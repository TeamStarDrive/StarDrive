#include "Qtree.h"
#include <algorithm>
#include <unordered_set>

namespace spatial
{
    Qtree::Qtree(int worldSize, int smallestCell)
    {
        WorldSize = worldSize;
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
        bytes += Pending.capacity() * sizeof(SpatialObject);
        bytes += Objects.capacity() * sizeof(SpatialObject);
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
        Levels = 0;
        FullSize = cellSize;
        while (FullSize < WorldSize)
        {
            FullSize *= 2;
            ++Levels;
        }
        rebuild();
    }

    void Qtree::clear()
    {
        Objects.clear();
        Pending.clear();
        Root = createRoot();
    }

    void Qtree::rebuild()
    {
        // swap the front and back-buffer
        // the front buffer will be reset and reused
        // while the back buffer will be untouched until next time
        std::swap(FrontAlloc, BackAlloc);
        FrontAlloc->reset();
        CurrentSplitThreshold = PendingSplitThreshold;

        if (!Pending.empty())
        {
            Objects.insert(Objects.end(), Pending.begin(), Pending.end());
            Pending.clear();
        }

        const int numObjects = (int)Objects.size();
        SpatialObject* objects = Objects.data();

        QtreeNode* root = createRoot();
        for (int i = 0; i < numObjects; ++i)
        {
            SpatialObject* o = &objects[i];
            insertAt(Levels, *root, o);
        }
        Root = root;
    }

    int Qtree::insert(const SpatialObject& o)
    {
        int objectId = (int)( Objects.size() + Pending.size() );
        SpatialObject& inserted = Pending.emplace_back(o);
        inserted.objectId = objectId;
        return objectId;
    }

    void Qtree::update(int objectId, int x, int y)
    {
        SpatialObject& o = Objects[objectId];
        o.x = x;
        o.y = y;
    }

    void Qtree::remove(int objectId)
    {
        // @todo This will be slow with large number of objects
        //       find a better lookup system, maybe a flatmap ?
        for (auto it = Objects.begin(), end = Objects.end(); it != end; ++it)
        {
            if (it->objectId == objectId)
            {
                Objects.erase(it);
                break;
            }
        }
    }

    struct Overlaps
    {
        bool NW, NE, SE, SW;
        SPATIAL_FINLINE Overlaps(int quadCenterX, int quadCenterY, int objectX, int objectY,
                              int objectRadiusX, int objectRadiusY)
        {
            // +---------+   The target rectangle overlaps Left quadrants (NW, SW)
            // | x--|    |
            // |-|--+----|
            // | x--|    |
            // +---------+
            bool overlaps_Left = (objectX-objectRadiusX < quadCenterX);
            // +---------+   The target rectangle overlaps Right quadrants (NE, SE)
            // |    |--x |
            // |----+--|-|
            // |    |--x |
            // +---------+
            bool overlaps_Right = (objectX+objectRadiusX >= quadCenterX);
            // +---------+   The target rectangle overlaps Top quadrants (NW, NE)
            // | x--|-x  |
            // |----+----|
            // |    |    |
            // +---------+
            bool overlaps_Top = (objectY-objectRadiusY < quadCenterY);
            // +---------+   The target rectangle overlaps Bottom quadrants (SW, SE)
            // |    |    |
            // |----+----|
            // | x--|-x  |
            // +---------+
            bool overlaps_Bottom = (objectY+objectRadiusY >= quadCenterY);

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
        int ox = o->x, oy = o->y, rx = o->rx, ry = o->ry;
        for (;;)
        {
            // try to select a sub-quadrant, perhaps it's a better match
            if (cur->isBranch())
            {
                Overlaps over { cur->cx, cur->cy, ox, oy, rx, ry };

                // bitwise add booleans to get the number of overlaps
                int overlaps = over.NW + over.NE + over.SE + over.SW;

                // this is an optimal case, we only overlap 1 sub-quadrant, so we go deeper
                if (overlaps == 1)
                {
                    if      (over.NW) { cur = cur->nw(); }
                    else if (over.NE) { cur = cur->ne(); }
                    else if (over.SE) { cur = cur->se(); }
                    else if (over.SW) { cur = cur->sw(); }
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
        if (leaf.size < CurrentSplitThreshold)
        {
            leaf.addObject(*FrontAlloc, o, CurrentSplitThreshold);
        }
        // are we maybe over Threshold and should Subdivide ?
        else if (level > 0)
        {
            const int size = leaf.size;
            SpatialObject** objects = leaf.objects;
            leaf.convertToBranch(*FrontAlloc);

            // and now reinsert all items one by one
            for (int i = 0; i < size; ++i)
            {
                insertAt(level-1, leaf, objects[i]);
            }

            // and now try to insert our object again
            insertAt(level-1, leaf, o);
        }
        else
        {
            // final edge case: if number of objects overwhelms the tree,
            // keep dynamically expanding the objects array
            leaf.addObjectUnbounded(*FrontAlloc, o, CurrentSplitThreshold);
        }
    }

    template<class T> struct SmallStack
    {
        static constexpr int MAX = 2048;
        int next = -1;
        T items[MAX];
        SmallStack() = default;
        explicit SmallStack(const T& node) : next{0} { items[0] = node; }
        SPATIAL_FINLINE void push_back(const T& item) { items[++next] = item; }
        SPATIAL_FINLINE T pop_back() { return items[next--]; }
    };

    void Qtree::removeAt(QtreeNode* root, int objectId)
    {
        SmallStack<QtreeNode*> stack; stack.push_back(root);
        do
        {
            QtreeNode& node = *stack.pop_back();
            if (node.isBranch())
            {
                stack.push_back(node.sw());
                stack.push_back(node.se());
                stack.push_back(node.ne());
                stack.push_back(node.nw());
            }
            else
            {
                int size = node.size;
                SpatialObject** items = node.objects;
                for (int i = 0; i < size; ++i)
                {
                    SpatialObject& so = *items[i];
                    if (so.objectId == objectId)
                    {
                        markForRemoval(objectId, so);
                        return;
                    }
                }
            }
        }
        while (stack.next >= 0);
    }

    void Qtree::collideAll(float timeStep, void* user, CollisionFunc onCollide)
    {
        Collider collider;
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
                if (int size = current.size)
                    collider.collideObjects(current.objects, size, user, onCollide);
            }
        }
        while (stack.next >= 0);
    }

    #pragma warning( disable : 6262 )
    int Qtree::findNearby(int* outResults, const SearchOptions& opt) const
    {
        FoundNodes found;

        SmallStack<const QtreeNode*> stack { Root };
        int cx = opt.OriginX;
        int cy = opt.OriginY;
        int rx = opt.SearchRadius;
        int ry = opt.SearchRadius;
        do
        {
            const QtreeNode& current = *stack.pop_back();
            if (current.isBranch())
            {
                Overlaps over { current.cx, current.cy, cx, cy, rx, ry };
                if (over.SW) stack.push_back(current.sw());
                if (over.SE) stack.push_back(current.se());
                if (over.NE) stack.push_back(current.ne());
                if (over.NW) stack.push_back(current.nw());
            }
            else
            {
                found.add(current.objects, current.size, current.cx, current.cy);
            }
        } while (stack.next >= 0);

        return spatial::findNearby(outResults, opt, found);
    }
    
    static const Color Brown  = { 139, 69,  19, 150 };
    static const Color VioletDim = { 199, 21, 133, 100 };
    static const Color VioletBright = { 199, 21, 133, 150 };
    static const Color Blue   = { 95, 158, 160, 200 };
    static const Color Yellow = { 255, 255,  0, 200 };

    void Qtree::debugVisualize(const VisualizerOptions& opt, Visualizer& visualizer) const
    {
        char text[128];
        int visibleX = opt.visibleWorldRect.centerX();
        int visibleY = opt.visibleWorldRect.centerY();
        int radiusX  = opt.visibleWorldRect.width() / 2;
        int radiusY  = opt.visibleWorldRect.height() / 2;
        visualizer.drawRect(Root->left(), Root->top(), Root->right(), Root->bottom(), Yellow);

        SmallStack<const QtreeNode*> stack { Root };
        do
        {
            const QtreeNode& current = *stack.pop_back();
            visualizer.drawRect(current.left(), current.top(), current.right(), current.bottom(), Brown);

            if (current.isBranch())
            {
                if (opt.nodeText)
                    visualizer.drawText(current.cx, current.cy, current.width(), "BR", Yellow);

                Overlaps over { current.cx, current.cy, visibleX, visibleY, radiusX, radiusY };
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
                    visualizer.drawText(current.cx, current.cy, current.width(), text, Yellow);
                }
                int count = current.size;
                SpatialObject** const items = current.objects;
                for (int i = 0; i < count; ++i)
                {
                    const SpatialObject& o = *items[i];
                    if (opt.objectBounds)
                        visualizer.drawRect(o.x-o.rx, o.y-o.ry, o.x+o.rx, o.y+o.ry, VioletBright);
                    if (opt.objectToLeafLines)
                        visualizer.drawLine(current.cx, current.cy, o.x, o.y, VioletDim);
                    if (opt.objectText)
                    {
                        snprintf(text, sizeof(text), "o=%d", o.objectId);
                        visualizer.drawText(o.x, o.y, o.rx*2, text, Blue);
                    }
                }
            }
        }
        while (stack.next >= 0);
    }

    void Qtree::markForRemoval(int objectId, SpatialObject& o)
    {
        o.active = 0;
        o.objectId = -1;
    }

    /////////////////////////////////////////////////////////////////////////////////

    SPATIAL_C_API Qtree* __stdcall QtreeCreate(int universeSize, int smallestCell)
    {
        return new Qtree(universeSize, smallestCell);
    }
    SPATIAL_C_API void __stdcall QtreeDestroy(Qtree* tree)
    {
        delete tree;
    }
    SPATIAL_C_API void __stdcall QtreeClear(Qtree* tree)
    {
        tree->clear();
    }
    SPATIAL_C_API void __stdcall QtreeRebuild(Qtree* tree)
    {
        tree->rebuild();
    }
    SPATIAL_C_API int __stdcall QtreeInsert(Qtree* tree, const SpatialObject& o)
    {
        return tree->insert(o);
    }
    SPATIAL_C_API void __stdcall QtreeUpdate(Qtree* tree, int objectId, int x, int y)
    {
        tree->update(objectId, x, y);
    }
    SPATIAL_C_API void __stdcall QtreeRemove(Qtree* tree, int objectId)
    {
        tree->remove(objectId);
    }
    SPATIAL_C_API void __stdcall QtreeCollideAll(Qtree* tree, float timeStep, void* user, CollisionFunc onCollide)
    {
        tree->collideAll(timeStep, user, onCollide);
    }
    SPATIAL_C_API int __stdcall QtreeFindNearby(Qtree* tree, int* outResults, const SearchOptions& opt)
    {
        return tree->findNearby(outResults, opt);
    }
    SPATIAL_C_API void __stdcall QtreeDebugVisualize(Qtree* tree, const VisualizerOptions& opt, const VisualizerBridge& vis)
    {
        struct CppToCBridge : Visualizer
        {
            VisualizerBridge vis;
            explicit CppToCBridge(const VisualizerBridge& visualizer) : vis{visualizer} {}
            void drawRect(int x1, int y1, int x2, int y2, Color c) override
            { vis.drawRect(x1, y1, x2, y2, c); }
            void drawCircle(int x, int y, int radius, Color c) override
            { vis.drawCircle(x, y, radius, c); }
            void drawLine(int x1, int y1, int x2, int y2, Color c) override
            { vis.drawLine(x1, y1, x2, y2, c); }
            void drawText(int x, int y, int size, const char* text, Color c) override
            { vis.drawText(x, y, size, text, c); }
        };

        CppToCBridge bridge { vis };
        tree->debugVisualize(opt, bridge);
    }
    /////////////////////////////////////////////////////////////////////////////////
}
