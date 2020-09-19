#include "QuadTree.h"

#include <stdexcept>

#include "QtreeConstants.h"

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

    QtreeBoundedNode QuadTree::createRoot() const
    {
        int half = FullSize / 2;
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

    void QuadTree::rebuild(const std::vector<QtreeObject>& objects)
    {
        Objects.assign(objects.data(), objects.data() + objects.size());
        rebuild();
    }

    void QuadTree::rebuild(const QtreeObject* objects, int numObjects)
    {
        Objects.assign(objects, objects + numObjects);
        rebuild();
    }

    void QuadTree::insert(const QtreeObject& o)
    {
        Objects.push_back(o);
    }

    void QuadTree::insert(const std::vector<QtreeObject>& objects)
    {
        Objects.insert(Objects.end(), objects.begin(), objects.end());
    }

    void QuadTree::insert(const QtreeObject* objects, int numObjects)
    {
        Objects.insert(Objects.end(), objects, objects+numObjects);
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
        _forceinline Overlaps(int quadCenterX, int quadCenterY, int objectX, int objectY,
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
        T stack[MAX];
        __forceinline void push(const T& node) { stack[++next] = node; }
        __forceinline T pop() { return stack[next--]; }
    };

    void QuadTree::removeAt(QtreeNode* root, int objectId)
    {
        SmallStack<QtreeNode*> stack; stack.push(root);
        do
        {
            QtreeNode& node = *stack.pop();
            if (node.isBranch())
            {
                stack.push(node.sw());
                stack.push(node.se());
                stack.push(node.ne());
                stack.push(node.nw());
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

    int QuadTree::findNearby(int* outResults, const SearchOptions& opt)
    {
        int searchRX = opt.SearchRadius;
        int searchRY = opt.SearchRadius;

        // find the deepest enclosing node
        //QtreeBoundedNode enclosing = findEnclosingNode(Root, enclosingRect);

        // If enclosing object is the Root object and radius is huge,
        // switch to linear search because we need to traverse the ENTIRE universe anyway
        // TODO -- Implement this in native side
        //if (enclosing == root && radius > QuadToLinearSearchThreshold)
        //{
        //    return FindLinear(worldPos, radius, filter, toIgnore, loyaltyFilter);
        //}

        SmallStack<QtreeBoundedNode> stack;
        stack.push(Root);

        // NOTE: to avoid a few branches, we used pre-calculated masks, 0xff will pass any
        int exclLoyaltyMask = (opt.FilterExcludeByLoyalty == 0)     ? 0xffffffff : ~opt.FilterExcludeByLoyalty;
        int onlyLoyaltyMask = (opt.FilterIncludeOnlyByLoyalty == 0) ? 0xffffffff : opt.FilterIncludeOnlyByLoyalty;
        int filterMask      = (opt.FilterByType == 0)               ? 0xffffffff : opt.FilterByType;
        int objectMask      = (opt.FilterExcludeObjectId == -1)     ? 0xffffffff : ~opt.FilterExcludeObjectId;
        int x = opt.OriginX;
        int y = opt.OriginY;
        int radius = opt.SearchRadius;
        SearchFilterFunc filterFunc = opt.FilterFunction;

        int maxResults = opt.MaxResults;
        int numResults = 0;
        do
        {
            QtreeBoundedNode current = stack.pop();
            if (current.isBranch())
            {
                Overlaps over { current.cx, current.cy, x, y, radius, radius };
                if (over.SW) stack.push(current.sw());
                if (over.SE) stack.push(current.se());
                if (over.NE) stack.push(current.ne());
                if (over.NW) stack.push(current.nw());
            }
            else
            {
                int size = current.node->size;
                const QtreeObject* items = current.node->objects;
                for (int i = 0; i < size; ++i)
                {
                    const QtreeObject& o = items[i];
                    if (o.active
                        && (o.loyalty & exclLoyaltyMask)
                        && (o.loyalty & onlyLoyaltyMask)
                        && (o.type     & filterMask)
                        && (o.objectId & objectMask))
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
        }
        while (stack.next >= 0);
        
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

        int visibleX = (visible.left + visible.right) / 2;
        int visibleY = (visible.top + visible.bottom) / 2;
        int radiusX = (visible.right - visible.left) / 2;
        int radiusY = (visible.bottom - visible.top) / 2;

        visualizer.drawRect(-UniverseSize/2, -UniverseSize/2, +UniverseSize/2, +UniverseSize/2, Yellow);

        std::vector<QtreeBoundedNode> stack;
        stack.reserve(128);
        //SmallStack<QtreeBoundedNode> stack;
        //stack.push(Root);
        stack.push_back(Root);
        do
        {
            QtreeBoundedNode current = stack.back();
            stack.pop_back();
            visualizer.drawRect(current.left, current.top, current.right, current.bottom, Brown);

            if (current.isBranch())
            {
                snprintf(text, sizeof(text), "BR{%d,%d}", (int)current.cx, (int)current.cy);
                visualizer.drawText(current.cx, current.cy, current.width(), text, Yellow);

                Overlaps over { current.cx, current.cy, visibleX, visibleY, radiusX, radiusY };
                if (over.SW) stack.push_back(current.sw());
                if (over.SE) stack.push_back(current.se());
                if (over.NE) stack.push_back(current.ne());
                if (over.NW) stack.push_back(current.nw());
            }
            else
            {
                snprintf(text, sizeof(text), "LF{%d,%d} size=%d", (int)current.cx, (int)current.cy, current.node->size);
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
        while (!stack.empty());
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

    TREE_C_API void __stdcall QtreeRebuildObjects(QuadTree* tree, const QtreeObject* objects, int numObjects)
    {
        tree->rebuild(objects, numObjects);
    }

    TREE_C_API void __stdcall QtreeInsert(QuadTree* tree, const QtreeObject& o)
    {
        tree->insert(o);
    }

    TREE_C_API void __stdcall QtreeInsertObjects(QuadTree* tree, const QtreeObject* objects, int numObjects)
    {
        tree->insert(objects, numObjects);
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
            void drawRect(int x1, int y1, int x2, int y2, QtreeColor c) override
            { vis.DrawRect(x1, y1, x2, y2, c); }
            void drawCircle(int x, int y, int radius, QtreeColor c) override
            { vis.DrawCircle(x, y, radius, c); }
            void drawLine(int x1, int y1, int x2, int y2, QtreeColor c) override
            { vis.DrawLine(x1, y1, x2, y2, c); }
            void drawText(int x, int y, int size, const char* text, QtreeColor c) override
            { vis.DrawText(x, y, size, text, c); }
        };

        VisualizerBridge bridge { visualizer };
        tree->debugVisualize(visible, bridge);
    }
    /////////////////////////////////////////////////////////////////////////////////
}
