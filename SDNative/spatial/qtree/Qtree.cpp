#include "Qtree.h"
#include <algorithm>
#include <unordered_set>

namespace spatial
{
    Qtree::Qtree(int worldSize, int smallestCell) : Spatial{worldSize}
    {
        smallestCellSize(smallestCell);
    }

    Qtree::~Qtree()
    {
        delete FrontAlloc;
        delete BackAlloc;
    }

    uint32_t Qtree::totalMemory() const
    {
        uint32_t bytes = sizeof(Qtree);
        bytes += FrontAlloc->totalBytes();
        bytes += BackAlloc->totalBytes();
        bytes += Objects.totalMemory();
        return bytes;
    }

    QtreeNode* Qtree::createRoot() const
    {
        QtreeNode* root = FrontAlloc->alloc<QtreeNode>();
        root->setCoords(0, 0, FullSize / 2);
        return root;
    }

    void Qtree::smallestCellSize(int cellSize)
    {
        SmallestCell = cellSize;
        FullSize = cellSize;
        while (FullSize < WorldSize)
        {
            FullSize *= 2;
        }

        Levels = 0;
        int currentSize = FullSize;
        while (currentSize > SmallestCell*2)
        {
            currentSize /= 2;
            ++Levels;
        }
        rebuild();
    }

    void Qtree::clear()
    {
        Objects.clear();
        Root = createRoot();
        Dbg.clear();
    }

    void Qtree::rebuild()
    {
        // swap the front and back-buffer
        // the front buffer will be reset and reused
        // while the back buffer will be untouched until next time
        std::swap(FrontAlloc, BackAlloc);
        FrontAlloc->reset();
        CurrentSplitThreshold = PendingSplitThreshold;

        Objects.submitPending();
        QtreeNode* root = createRoot();

        int topLevel = Levels - 1;
        for (SpatialObject& o : Objects)
        {
            if (o.active)
            {
                insertAt(topLevel, *root, &o);
            }
        }
        Root = root;
    }

    struct OverlapsRect
    {
        bool NW, NE, SE, SW;
        SPATIAL_FINLINE OverlapsRect(int quadCenterX, int quadCenterY, Rect rect)
        {
            // +---------+   The target rectangle overlaps Left quadrants (NW, SW)
            // | x--|    |
            // |-|--+----|
            // | x--|    |
            // +---------+
            bool overlaps_Left = (rect.x1 < quadCenterX);
            // +---------+   The target rectangle overlaps Right quadrants (NE, SE)
            // |    |--x |
            // |----+--|-|
            // |    |--x |
            // +---------+
            bool overlaps_Right = (rect.x2 >= quadCenterX);
            // +---------+   The target rectangle overlaps Top quadrants (NW, NE)
            // | x--|-x  |
            // |----+----|
            // |    |    |
            // +---------+
            bool overlaps_Top = (rect.y1 < quadCenterY);
            // +---------+   The target rectangle overlaps Bottom quadrants (SW, SE)
            // |    |    |
            // |----+----|
            // | x--|-x  |
            // +---------+
            bool overlaps_Bottom = (rect.y2 >= quadCenterY);

            // bitwise combine to get which quadrants we overlap: NW, NE, SE, SW
            NW = overlaps_Top & overlaps_Left;
            NE = overlaps_Top & overlaps_Right;
            SE = overlaps_Bottom & overlaps_Right;
            SW = overlaps_Bottom & overlaps_Left;
        }
    };

    void Qtree::insertAt(int level, QtreeNode& root, SpatialObject* o)
    {
        QtreeNode* cur = &root;
        Rect oRect = o->rect;
        for (;;)
        {
            // try to select a sub-quadrant, perhaps it's a better match
            if (cur->isBranch())
            {
                OverlapsRect over { cur->cx, cur->cy, oRect };

                // bitwise add booleans to get the number of overlaps
                int overlaps = over.NW + over.NE + over.SE + over.SW;

                // this is an optimal case, we only overlap 1 sub-quadrant, so we go deeper
                if (overlaps == 1)
                {
                    if      (over.NW) { cur = cur->nw(); --level; }
                    else if (over.NE) { cur = cur->ne(); --level; }
                    else if (over.SE) { cur = cur->se(); --level; }
                    else if (over.SW) { cur = cur->sw(); --level; }
                }
                else // target overlaps multiple quadrants, so it has to be inserted into several of them:
                {
                    if (over.NW) { insertAt(level-1, *cur->nw(), o); }
                    if (over.NE) { insertAt(level-1, *cur->ne(), o); }
                    if (over.SE) { insertAt(level-1, *cur->se(), o); }
                    if (over.SW) { insertAt(level-1, *cur->sw(), o); }
                    return;
                }
            }
            else // isLeaf
            {
                insertAtLeaf(level, *cur, o);
                return;
            }
        }
    }

    void Qtree::insertAtLeaf(int level, QtreeNode& leaf, SpatialObject* o)
    {
        // are we maybe over Threshold and should Subdivide ?
        if (level > 0 && leaf.size >= CurrentSplitThreshold)
        {
            SpatialObject** objects = leaf.objects;
            const int size = leaf.size;
            leaf.convertToBranch(*FrontAlloc);

            // and now reinsert all items one by one
            for (int i = 0; i < size; ++i)
            {
                insertAt(level, leaf, objects[i]);
            }

            // we can reuse this array later
            FrontAlloc->reuseArray(objects, size);

            // and now try to insert our object again
            insertAt(level, leaf, o);
        }
        else // expand LEAF
        {
            leaf.addObject(*FrontAlloc, o, CurrentSplitThreshold);
        }
    }

    template<class T> struct SmallStack
    {
        static constexpr int MAX = 2048;
        int next = -1;
        T items[MAX];
        #pragma warning(disable:26495)
        SmallStack() = default;
        explicit SmallStack(const T& node) : next{0} { items[0] = node; }
        SPATIAL_FINLINE void push_back(const T& item) { items[++next] = item; }
        SPATIAL_FINLINE T pop_back() { return items[next--]; }
    };

    CollisionPairs Qtree::collideAll(const CollisionParams& params)
    {
        Collider collider { *FrontAlloc, Objects.maxObjects() };

        SmallStack<QtreeNode*> stack { Root };
        do
        {
            const QtreeNode& current = *stack.pop_back();
            if (current.isBranch())
            {
                stack.push_back(current.sw());
                stack.push_back(current.se());
                stack.push_back(current.ne());
                stack.push_back(current.nw());
            }
            else
            {
                if (current.size > 1)
                {
                    collider.collideObjects({current.objects, current.size}, current.loyalty, params);
                }
            }
        }
        while (stack.next >= 0);

        CollisionPairs results = collider.getResults(params);
        if (params.showCollisions)
        {
            Dbg.setCollisions(results);
        }
        return results;
    }

    #pragma warning( disable : 6262 )
    int Qtree::findNearby(int* outResults, const SearchOptions& opt) const
    {
        FoundNodes found;
        SmallStack<const QtreeNode*> stack { Root };
        Rect searchRect = opt.SearchRect;
        uint32_t loyaltyMask = getLoyaltyMask(opt);
        do
        {
            const QtreeNode& current = *stack.pop_back();
            int cx = current.cx, cy = current.cy, cr = current.radius;

            if (current.isBranch())
            {
                OverlapsRect over { cx, cy, searchRect };
                if (over.SW) stack.push_back(current.sw());
                if (over.SE) stack.push_back(current.se());
                if (over.NE) stack.push_back(current.ne());
                if (over.NW) stack.push_back(current.nw());
            }
            else
            {
                if (current.loyalty.mask & loyaltyMask) // empty cell mask is 0
                {
                    found.add(current.objects, current.size, {cx,cy}, cr);
                }
            }
        } while (stack.next >= 0 && found.count != found.MAX);

        int numResults = 0;
        if (found.count)
            numResults = spatial::findNearby(outResults, Objects.maxObjects(), opt, found);

        if (opt.DebugId)
        {
            DebugFindNearby dfn;
            dfn.SearchArea = opt.SearchRect;
            dfn.RadialFilter = opt.RadialFilter;
            dfn.addCells(found);
            dfn.addResults(outResults, numResults);
            Dbg.setFindNearby(opt.DebugId, std::move(dfn));
        }
        return numResults;
    }

    void Qtree::debugVisualize(const VisualizerOptions& opt, Visualizer& visualizer) const
    {
        char text[128];
        Rect visibleRect = opt.visibleWorldRect;
        visualizer.drawRect(Root->rect(), Yellow);

        SmallStack<const QtreeNode*> stack { Root };
        do
        {
            const QtreeNode& current = *stack.pop_back();
            if (opt.nodeBounds)
            {
                auto color = current.loyalty.count > 1 ? Brown : BrownDim;
                visualizer.drawRect(current.rect(), color);
            }

            int cx = current.cx, cy = current.cy;
            if (current.isBranch())
            {
                if (opt.nodeText)
                    visualizer.drawText({cx,cy}, current.width(), "BR", Yellow);

                OverlapsRect over { cx, cy, visibleRect };
                if (over.SW) stack.push_back(current.sw());
                if (over.SE) stack.push_back(current.se());
                if (over.NE) stack.push_back(current.ne());
                if (over.NW) stack.push_back(current.nw());
            }
            else
            {
                if (opt.nodeText)
                {
                    snprintf(text, sizeof(text), "LF n=%d", current.size);
                    visualizer.drawText({cx,cy}, current.width(), text, Yellow);
                }

                int count = current.size;
                SpatialObject** const items = current.objects;
                for (int i = 0; i < count; ++i)
                {
                    const SpatialObject& o = *items[i];

                    if (opt.objectBounds)
                    {
                        auto color = (o.loyalty % 2 == 0) ? VioletBright : Purple;
                        visualizer.drawRect(o.rect, color);
                    }
                    if (opt.objectToLeaf)
                    {
                        auto color = (o.loyalty % 2 == 0) ? VioletDim : Purple;
                        visualizer.drawLine({cx,cy}, o.rect.center(), color);
                    }
                    if (opt.objectText)
                    {
                        snprintf(text, sizeof(text), "o=%d", o.objectId);
                        visualizer.drawText(o.rect.center(), o.rect.width(), text, Blue);
                    }
                }
            }
        } while (stack.next >= 0);

        if (opt.searchDebug)
        {
            Dbg.draw(visualizer, opt, Objects.data());
        }
    }
}
