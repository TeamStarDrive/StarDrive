#include "QuadTree.h"
#include <algorithm>
#include <unordered_set>

namespace tree
{
    QuadTree::QuadTree(int universeSize, int smallestCell)
    {
        Levels = 0;
        FullSize = smallestCell;
        UniverseSize = universeSize;
        while (FullSize < universeSize)
        {
            FullSize *= 2;
            ++Levels;
        }
        Root = createRoot();
    }

    QuadTree::~QuadTree()
    {
        delete FrontAlloc;
        delete BackAlloc;
    }

    uint32_t QuadTree::totalMemory() const
    {
        uint32_t bytes = sizeof(QuadTree);
        bytes += FrontAlloc->totalBytes();
        bytes += BackAlloc->totalBytes();
        bytes += Objects.size() * sizeof(QtreeObject);
        return bytes;
    }

    QtreeNode* QuadTree::createRoot() const
    {
        QtreeNode* root = FrontAlloc->alloc<QtreeNode>();
        root->setCoords(0, 0, FullSize / 2);
        return root;
    }

    void QuadTree::clear()
    {
        Objects.clear();
        Root = createRoot();
    }

    void QuadTree::rebuild()
    {
        // swap the front and back-buffer
        // the front buffer will be reset and reused
        // while the back buffer will be untouched until next time
        std::swap(FrontAlloc, BackAlloc);
        FrontAlloc->reset();
        CurrentSplitThreshold = PendingSplitThreshold;

        const int numObjects = (int)Objects.size();
        const QtreeObject* objects = Objects.data();

        QtreeNode* root = createRoot();
        for (int i = 0; i < numObjects; ++i)
        {
            const QtreeObject& o = objects[i];
            insertAt(Levels, *root, o);
        }
        Root = root;
    }

    int QuadTree::insert(const QtreeObject& o)
    {
        int objectId = (int)Objects.size();
        QtreeObject& inserted = Objects.emplace_back(o);
        inserted.objectId = objectId;
        return objectId;
    }

    void QuadTree::update(int objectId, int x, int y)
    {
        QtreeObject& o = Objects[objectId];
        o.x = x;
        o.y = y;
    }

    void QuadTree::remove(int objectId)
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
        TREE_FINLINE Overlaps(int quadCenterX, int quadCenterY, int objectX, int objectY,
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

    void QuadTree::insertAt(int level, QtreeNode& root, const QtreeObject& o)
    {
        QtreeNode* cur = &root;
        for (;;)
        {
            // try to select a sub-quadrant, perhaps it's a better match
            if (cur->isBranch())
            {
                Overlaps over { cur->cx, cur->cy, o.x, o.y, o.rx, o.ry };

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

    void QuadTree::insertAtLeaf(int level, QtreeNode& leaf, const QtreeObject& o)
    {
        if (leaf.size < CurrentSplitThreshold)
        {
            leaf.addObject(*FrontAlloc, o, CurrentSplitThreshold);
        }
        // are we maybe over Threshold and should Subdivide ?
        else if (level > 0)
        {
            const int size = leaf.size;
            QtreeObject* objects = leaf.objects;
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
        __forceinline void push_back(const T& node) { items[++next] = node; }
        __forceinline T pop_back() { return items[next--]; }
    };

    void QuadTree::removeAt(QtreeNode* root, int objectId)
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
                QtreeObject* items = node.objects;
                for (int i = 0; i < size; ++i)
                {
                    QtreeObject& so = items[i];
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

    struct CollisionPair
    {
        int ObjectA;
        int ObjectB;
        bool operator==(const CollisionPair& o) const
        {
            return (ObjectA == o.ObjectA && ObjectB == o.ObjectB)
                || (ObjectB == o.ObjectA && ObjectA == o.ObjectB);
        }
    };

    struct CollisionPairHash
    {
        std::size_t operator()(const CollisionPair& p) const
        {
            return std::hash<int>()(p.ObjectA) ^ std::hash<int>()(p.ObjectB);
        }
    };

    void QuadTree::collideAll(float timeStep, void* user, CollisionFunc onCollide)
    {
        std::unordered_set<CollisionPair, CollisionPairHash> collided;

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
                const int size = current.size;
                const QtreeObject* items = current.objects;
                for (int i = 0; i < size; ++i)
                {
                    const QtreeObject& objectA = items[i];
                    if (!objectA.active)
                        continue;

                    for (int j = i + 1; j < size; ++j)
                    {
                        const QtreeObject& objectB = items[j];
                        if (!objectB.active)
                            continue;
                        //if (!objectA.overlaps(objectB))
                        //    continue;
                        float dx = objectA.x - objectB.x;
                        float dy = objectA.y - objectB.y;
                        float r2 = objectA.rx + objectB.rx;
                        if ((dx*dx + dy*dy) <= (r2*r2))
                        {
                            CollisionPair collisionPair { objectA.objectId, objectB.objectId };
                            if (!collided.contains(collisionPair))
                            {
                                collided.insert(collisionPair);
                                CollisionResult result = onCollide(user, objectA.objectId, objectB.objectId);
                            }
                        }
                    }
                }
            }
        }
        while (stack.next >= 0);
    }

    // finds all LEAF nodes that overlap [cx, cy, rx, ry]
    struct FoundLeaves
    {
        int numLeaves = 0; // number of found leaves
        int totalObjects = 0; // total number of potential objects
        SmallStack<const QtreeNode*> leaves;
    };

    static void findLeaves(FoundLeaves& found, const QtreeNode* root, int cx, int cy, int rx, int ry)
    {
        SmallStack<const QtreeNode*> stack { root };
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
                found.leaves.push_back(&current);
                ++found.numLeaves;
                found.totalObjects += current.size;
            }
        } while (stack.next >= 0);
    }

    #pragma warning( disable : 6262 )
    int QuadTree::findNearby(int* outResults, const SearchOptions& opt)
    {
        FoundLeaves found;
        findLeaves(found, Root, opt.OriginX, opt.OriginY, opt.SearchRadius, opt.SearchRadius);

        // NOTE: to avoid a few branches, we used pre-calculated masks, 0xff will pass any
        int exclLoyaltyMask = (opt.FilterExcludeByLoyalty == 0)     ? 0xffffffff : ~opt.FilterExcludeByLoyalty;
        int onlyLoyaltyMask = (opt.FilterIncludeOnlyByLoyalty == 0) ? 0xffffffff : opt.FilterIncludeOnlyByLoyalty;
        int filterMask      = (opt.FilterByType == 0)               ? 0xffffffff : opt.FilterByType;
        int objectMask      = (opt.FilterExcludeObjectId == -1)     ? 0xffffffff : ~(opt.FilterExcludeObjectId+1);
        float x = opt.OriginX;
        float y = opt.OriginY;
        float radius = opt.SearchRadius;
        SearchFilterFunc filterFunc = opt.FilterFunction;
        int maxResults = opt.MaxResults;
        const QtreeNode** leaves = found.leaves.items;

        // if total candidates is more than we can fit, we need to sort LEAF nodes by distance to Origin
        if (found.totalObjects > opt.MaxResults)
        {
            std::sort(leaves, leaves+found.numLeaves, [x,y](const QtreeNode* a, const QtreeNode* b) -> bool
            {
                float adx = x - a->cx;
                float ady = y - a->cy;
                float sqDist1 = adx*adx + ady*ady;
                float bdx = x - b->cx;
                float bdy = y - b->cy;
                float sqDist2 = bdx*bdx + bdy*bdy;
                return sqDist1 < sqDist2;
            });
        }

        int numResults = 0;
        for (int leafIndex = 0; leafIndex < found.numLeaves; ++leafIndex)
        {
            const QtreeNode& leaf = *leaves[leafIndex];
            const int size = leaf.size;
            const QtreeObject* items = leaf.objects;
            for (int i = 0; i < size; ++i)
            {
                const QtreeObject& o = items[i];
                if (o.active
                    && (o.loyalty & exclLoyaltyMask)
                    && (o.loyalty & onlyLoyaltyMask)
                    && (o.type     & filterMask)
                    && ((o.objectId+1) & objectMask))
                {
                    // check if inside radius, inlined for perf
                    float dx = x - o.x;
                    float dy = y - o.y;
                    float r2 = radius + o.rx;
                    if ((dx*dx + dy*dy) <= (r2*r2))
                    {
                        if (!filterFunc || filterFunc(o.objectId) != 0)
                        {
                            outResults[numResults++] = o.objectId;
                            if (numResults >= maxResults)
                                return numResults; // we are done !
                        }
                    }
                }
            }
        }
        return numResults;
    }
    
    static const QtreeColor Brown  = { 139, 69,  19, 150 };
    static const QtreeColor VioletDim = { 199, 21, 133, 100 };
    static const QtreeColor VioletBright = { 199, 21, 133, 150 };
    static const QtreeColor Blue   = { 95, 158, 160, 200 };
    static const QtreeColor Red    = { 255, 69,   0, 100 };
    static const QtreeColor Yellow = { 255, 255,  0, 200 };

    void QuadTree::debugVisualize(const QtreeVisualizerOptions& opt, QtreeVisualizer& visualizer) const
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
                const QtreeObject* items = current.objects;
                for (int i = 0; i < count; ++i)
                {
                    const QtreeObject& o = items[i];
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

    void QuadTree::markForRemoval(int objectId, QtreeObject& o)
    {
        o.active = 0;
        o.objectId = -1;
    }

    /////////////////////////////////////////////////////////////////////////////////

    TREE_C_API QuadTree* __stdcall QtreeCreate(int universeSize, int smallestCell)
    {
        return new QuadTree(universeSize, smallestCell);
    }
    TREE_C_API void __stdcall QtreeDestroy(QuadTree* tree)
    {
        delete tree;
    }
    TREE_C_API void __stdcall QtreeClear(QuadTree* tree)
    {
        tree->clear();
    }
    TREE_C_API void __stdcall QtreeRebuild(QuadTree* tree)
    {
        tree->rebuild();
    }
    TREE_C_API int __stdcall QtreeInsert(QuadTree* tree, const QtreeObject& o)
    {
        return tree->insert(o);
    }
    TREE_C_API void __stdcall QtreeUpdate(QuadTree* tree, int objectId, int x, int y)
    {
        tree->update(objectId, x, y);
    }
    TREE_C_API void __stdcall QtreeRemove(QuadTree* tree, int objectId)
    {
        tree->remove(objectId);
    }
    TREE_C_API void __stdcall QtreeCollideAll(QuadTree* tree, float timeStep, void* user, CollisionFunc onCollide)
    {
        tree->collideAll(timeStep, user, onCollide);
    }
    TREE_C_API int __stdcall QtreeFindNearby(QuadTree* tree, int* outResults, const SearchOptions& opt)
    {
        return tree->findNearby(outResults, opt);
    }
    TREE_C_API void __stdcall QtreeDebugVisualize(QuadTree* tree, const QtreeVisualizerOptions& opt, const QtreeVisualizerBridge& vis)
    {
        struct VisualizerBridge : QtreeVisualizer
        {
            QtreeVisualizerBridge vis;
            explicit VisualizerBridge(const QtreeVisualizerBridge& visualizer) : vis{visualizer} {}
            void drawRect(int x1, int y1, int x2, int y2, QtreeColor c) override
            { vis.DrawRect(x1, y1, x2, y2, c); }
            void drawCircle(int x, int y, int radius, QtreeColor c) override
            { vis.DrawCircle(x, y, radius, c); }
            void drawLine(int x1, int y1, int x2, int y2, QtreeColor c) override
            { vis.DrawLine(x1, y1, x2, y2, c); }
            void drawText(int x, int y, int size, const char* text, QtreeColor c) override
            { vis.DrawText(x, y, size, text, c); }
        };

        VisualizerBridge bridge { vis };
        tree->debugVisualize(opt, bridge);
    }
    /////////////////////////////////////////////////////////////////////////////////
}
