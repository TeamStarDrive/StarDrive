#include "QuadTree.h"
#include <algorithm>

namespace tree
{
    QuadTree::QuadTree(float universeSize, float smallestCell)
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

    QtreeBoundedNode QuadTree::createRoot() const
    {
        float half = FullSize / 2;
        return QtreeBoundedNode{FrontAlloc->alloc<QtreeNode>(), 0, 0, -half, -half, +half, +half };
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

        const int numObjects = (int)Objects.size();
        const QtreeObject* objects = Objects.data();

        QtreeBoundedNode root = createRoot();
        for (int i = 0; i < numObjects; ++i)
        {
            const QtreeObject& o = objects[i];
            insertAt(Levels, root, o);
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

    void QuadTree::update(int objectId, float x, float y)
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
        _forceinline Overlaps(float quadCenterX, float quadCenterY,
                              float objectX, float objectY,
                              float objectRadiusX, float objectRadiusY)
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

    void QuadTree::insertAt(int level, const QtreeBoundedNode& root, const QtreeObject& o)
    {
        // QtreeBoundedNode node = findEnclosingNode(root, target);
        QtreeBoundedNode cur = root;
        for (;;)
        {
            // try to select a sub-quadrant, perhaps it's a better match
            if (cur.isBranch())
            {
                Overlaps over { cur.cx, cur.cy, o.x, o.y, o.rx, o.ry };

                // bitwise add booleans to get the number of overlaps
                int overlaps = over.NW + over.NE + over.SE + over.SW;

                // this is an optimal case, we only overlap 1 sub-quadrant, so we go deeper
                if (overlaps == 1)
                {
                    if      (over.NW) { cur = cur.nw(); }
                    else if (over.NE) { cur = cur.ne(); }
                    else if (over.SE) { cur = cur.se(); }
                    else if (over.SW) { cur = cur.sw(); }
                }
                else // target overlaps multiple quadrants, so it has to be inserted into several of them:
                {
                    if (over.NW) { insertAt(level-1, cur.nw(), o); }
                    if (over.NE) { insertAt(level-1, cur.ne(), o); }
                    if (over.SE) { insertAt(level-1, cur.se(), o); }
                    if (over.SW) { insertAt(level-1, cur.sw(), o); }
                    return;
                }
            }
            else // isLeaf
            {
                insertAtLeaf(level, cur, o);
                return;
            }
        }
    }

    void QuadTree::insertAtLeaf(int level, const QtreeBoundedNode& root, const QtreeObject& o)
    {
        QtreeNode& node = *root.node;

        if (node.size < QuadCellThreshold)
        {
            node.addObject(*FrontAlloc, o);
        }
        // are we maybe over Threshold and should Subdivide ?
        else if (level > 0)
        {
            const int size = node.size;
            QtreeObject* objects = node.objects;
            node.convertToBranch(*FrontAlloc);

            // and now reinsert all items one by one
            for (int i = 0; i < size; ++i)
            {
                insertAt(level-1, root, objects[i]);
            }

            // and now try to insert our object again
            insertAt(level-1, root, o);
        }
        else
        {
            node.addObjectUnbounded(*FrontAlloc, o);
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

    void QuadTree::collideAll(float timeStep, CollisionFunc onCollide)
    {
    }

    // finds all LEAF nodes that overlap [cx, cy, rx, ry]
    struct FoundLeaves
    {
        int numLeaves = 0; // number of found leaves
        int totalObjects = 0; // total number of potential objects
        SmallStack<QtreeBoundedNode> leaves;
    };

    static void findLeaves(FoundLeaves& found, const QtreeBoundedNode& root, float cx, float cy, float rx, float ry)
    {
        SmallStack<QtreeBoundedNode> stack { root };
        do
        {
            QtreeBoundedNode current = stack.pop_back();
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
                found.leaves.push_back(current);
                ++found.numLeaves;
                found.totalObjects += current.node->size;
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
        QtreeBoundedNode* leaves = found.leaves.items;

        // if total candidates is more than we can fit, we need to sort LEAF nodes by distance to Origin
        if (found.totalObjects > opt.MaxResults)
        {
            std::sort(leaves, leaves+found.numLeaves, [x,y](const QtreeBoundedNode& a, const QtreeBoundedNode& b) -> bool
            {
                float adx = x - a.cx;
                float ady = y - a.cy;
                float sqdist1 = adx*adx + ady*ady;
                float bdx = x - b.cx;
                float bdy = y - b.cy;
                float sqdist2 = bdx*bdx + bdy*bdy;
                return sqdist1 < sqdist2;
            });
        }

        int numResults = 0;
        for (int leafIndex = 0; leafIndex < found.numLeaves; ++leafIndex)
        {
            QtreeNode* leaf = found.leaves.items[leafIndex].node;
            const int size = leaf->size;
            const QtreeObject* items = leaf->objects;
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
    static const QtreeColor Blue   = { 95, 158, 160, 100 };
    static const QtreeColor Red    = { 255, 69,   0, 100 };
    static const QtreeColor Yellow = { 255, 255,  0, 200 };

    void QuadTree::debugVisualize(QtreeRect visible, QtreeVisualizer& visualizer) const
    {
        char text[128];

        float visibleX = (visible.left + visible.right) * 0.5f;
        float visibleY = (visible.top + visible.bottom) * 0.5f;
        float radiusX = (visible.right - visible.left) * 0.5f;
        float radiusY = (visible.bottom - visible.top) * 0.5f;

        visualizer.drawRect(-UniverseSize/2, -UniverseSize/2, +UniverseSize/2, +UniverseSize/2, Yellow);

        SmallStack<QtreeBoundedNode> stack;
        //SmallStack<QtreeBoundedNode> stack;
        //stack.push(Root);
        stack.push_back(Root);
        do
        {
            QtreeBoundedNode current = stack.pop_back();
            visualizer.drawRect(current.left, current.top, current.right, current.bottom, Brown);

            if (current.isBranch())
            {
                visualizer.drawText(current.cx, current.cy, current.width(), "BR", Yellow);

                Overlaps over { current.cx, current.cy, visibleX, visibleY, radiusX, radiusY };
                if (over.SW) stack.push_back(current.sw());
                if (over.SE) stack.push_back(current.se());
                if (over.NE) stack.push_back(current.ne());
                if (over.NW) stack.push_back(current.nw());
            }
            else
            {
                snprintf(text, sizeof(text), "LF n=%d", current.node->size);
                visualizer.drawText(current.cx, current.cy, current.width(), text, Yellow);

                int count = current.node->size;
                const QtreeObject* items = current.node->objects;
                for (int i = 0; i < count; ++i)
                {
                    const QtreeObject& o = items[i];
                    visualizer.drawRect(o.x-o.rx, o.y-o.ry, o.x+o.rx, o.y+o.ry, VioletBright);
                    visualizer.drawLine(current.cx, current.cy, o.x, o.y, VioletDim);
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

    TREE_C_API void __stdcall QtreeUpdate(QuadTree* tree, int objectId, float x, float y)
    {
        tree->update(objectId, x, y);
    }

    TREE_C_API void __stdcall QtreeRemove(QuadTree* tree, int objectId)
    {
        tree->remove(objectId);
    }

    TREE_C_API void __stdcall QtreeCollideAll(QuadTree* tree, float timeStep, 
                                              CollisionFunc onCollide)
    {
        tree->collideAll(timeStep, onCollide);
    }

    TREE_C_API int __stdcall QtreeFindNearby(QuadTree* tree, int* outResults,
                                             const SearchOptions& opt)
    {
        return tree->findNearby(outResults, opt);
    }

    
    TREE_C_API void __stdcall QtreeDebugVisualize(QuadTree* tree,
                                    QtreeRect visible, const QtreeVisualizerBridge& visualizer)
    {
        struct VisualizerBridge : QtreeVisualizer
        {
            QtreeVisualizerBridge vis;
            explicit VisualizerBridge(const QtreeVisualizerBridge& visualizer) : vis{visualizer} {}
            void drawRect(float x1, float y1, float x2, float y2, QtreeColor c) override
            { vis.DrawRect(x1, y1, x2, y2, c); }
            void drawCircle(float x, float y, float radius, QtreeColor c) override
            { vis.DrawCircle(x, y, radius, c); }
            void drawLine(float x1, float y1, float x2, float y2, QtreeColor c) override
            { vis.DrawLine(x1, y1, x2, y2, c); }
            void drawText(float x, float y, float size, const char* text, QtreeColor c) override
            { vis.DrawText(x, y, size, text, c); }
        };

        VisualizerBridge bridge { visualizer };
        tree->debugVisualize(visible, bridge);
    }
    /////////////////////////////////////////////////////////////////////////////////
}
