#include "QuadTree.h"
#include "QtreeConstants.h"

namespace tree
{
    QuadTree::QuadTree(float universeSize, float smallestCell)
    {
        Levels = 1;
        FullSize = smallestCell;
        UniverseSize = universeSize;
        while (FullSize < universeSize)
        {
            ++Levels;
            FullSize *= 2;
        }
        QuadToLinearSearchThreshold = FullSize * QuadToLinearRatio;
    }

    QuadTree::~QuadTree()
    {
        delete FrontBuffer;
        delete BackBuffer;
    }

    QtreeBoundedNode QuadTree::createRoot()
    {
        // swap the front and back-buffer
        // the front buffer will be reset and reused
        // while the back buffer will be untouched until next time
        std::swap(FrontBuffer, BackBuffer);
        FrontBuffer->reset();
        
        float half = FullSize / 2;
        return QtreeBoundedNode{FrontBuffer->newNode(), 0.0f, 0.0f, -half, -half, +half, +half };
    }

    void QuadTree::updateAll(const std::vector<SpatialObj>& objects)
    {
        QtreeBoundedNode root = createRoot();
        for (const SpatialObj& so : objects)
        {
            insert(root, so);
        }
        Root = root;
    }

    QtreeBoundedNode QuadTree::findEnclosingNode(const QtreeBoundedNode& node, const QtreeRect obj)
    {
        int level = Levels;
        QtreeBoundedNode current = node;
        for (;;)
        {
            if (current.node->NW != nullptr)
            {
                if (obj.left < current.cx && obj.right < current.cx) // left
                {
                    if (obj.top < current.cy && obj.bottom < current.cy) // top left
                    {
                        current = current.nw();
                        if (--level > 1) // can we keep subdividing ?
                            continue;
                    }
                    if (obj.top >= current.cy) // bot left
                    {
                        current = current.sw();
                        if (--level > 1) // can we keep subdividing ?
                            continue;
                    }
                }
                else if (obj.left >= current.cx) // right
                {
                    if (obj.top < current.cy && obj.bottom < current.cy) // top right
                    {
                        current = current.ne();
                        if (--level > 1) // can we keep subdividing ?
                            continue;
                    }
                    if (obj.top >= current.cy) // bot right
                    {
                        current = current.se();
                        if (--level > 1) // can we keep subdividing ?
                            continue;
                    }
                }
            }
            // otherwise: target does not perfectly fit inside a quadrant anymore
            break;
        }
        return current;
    }

    void QuadTree::insertAt(const QtreeBoundedNode& node, int level, const SpatialObj& so)
    {
        QtreeBoundedNode current = node;
        QtreeRect target = so.Bounds;

        for (;;)
        {
            if (level <= 1) // no more subdivisions possible
            {
                current.node->add(*FrontBuffer, so);
                return;
            }

            // try to select a sub-quadrant, perhaps it's a better match
            if (current.node->NW != nullptr)
            {
                if (target.left < current.cx && target.right < current.cx) // left
                {
                    if (target.top < current.cy && target.bottom < current.cy) // top left
                    {
                        current = current.nw();
                        if (--level > 1) // can we keep subdividing ?
                            continue;
                    }
                    if (target.top >= current.cy) // bot left
                    {
                        current = current.sw();
                        if (--level > 1) // can we keep subdividing ?
                            continue;
                    }
                }
                else if (target.left >= current.cx) // right
                {
                    if (target.top < current.cy && target.bottom < current.cy) // top right
                    {
                        current = current.ne();
                        if (--level > 1) // can we keep subdividing ?
                            continue;
                    }
                    if (target.top >= current.cy) // bot right
                    {
                        current = current.se();
                        if (--level > 1) // can we keep subdividing ?
                            continue;
                    }
                }

                // target does not perfectly fit inside any sub-quadrants, so it belongs to current Node
            }

            // item belongs to this node
            current.node->add(*FrontBuffer, so);

            // actually, are we maybe over Threshold and should Subdivide ?
            if (current.node->NW == nullptr && current.node->Count >= QuadCellThreshold)
            {
                current.node->NW = FrontBuffer->newNode();
                current.node->NE = FrontBuffer->newNode();
                current.node->SE = FrontBuffer->newNode();
                current.node->SW = FrontBuffer->newNode();

                int count = current.node->Count;
                if (count != 0)
                {
                    SpatialObj* arr = current.node->Items;
                    current.node->Count = 0;
                    current.node->Capacity = 0;
                    current.node->Items = nullptr;

                    // and now reinsert all items one by one
                    for (int i = 0; i < count; ++i)
                        insertAt(current, level, arr[i]);
                }
            }
            return;
        }
    }

    template<class T> struct SmallStack
    {
        static constexpr int MAX = 1024;
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
            int count = node.Count;
            SpatialObj* items = node.Items;
            for (int i = 0; i < count; ++i)
            {
                SpatialObj& so = items[i];
                if (so.ObjectId == objectId)
                {
                    markForRemoval(objectId, so);
                    return;
                }
            }
            if (node.NW != nullptr)
            {
                stack.push(node.SW);
                stack.push(node.SE);
                stack.push(node.NE);
                stack.push(node.NW);
            }
        } while (stack.next >= 0);
    }

    void QuadTree::collideAll(float timeStep, CollisionFunc onCollide)
    {
    }

    void QuadTree::collideAllRecursive(float timeStep, CollisionFunc onCollide)
    {
    }

    int QuadTree::findNearby(int* outResults, const SearchOptions& opt)
    {
        QtreeRect enclosingRect = QtreeRect::fromPointRadius(opt.OriginX, opt.OriginY, opt.SearchRadius);

        // find the deepest enclosing node
        QtreeBoundedNode enclosing = findEnclosingNode(Root, enclosingRect);

        // If enclosing object is the Root object and radius is huge,
        // switch to linear search because we need to traverse the ENTIRE universe anyway
        // TODO -- Implement this in native side
        //if (enclosing == root && radius > QuadToLinearSearchThreshold)
        //{
        //    return FindLinear(worldPos, radius, filter, toIgnore, loyaltyFilter);
        //}

        SmallStack<QtreeBoundedNode> stack;
        stack.push(enclosing);

        // NOTE: to avoid a few branches, we used pre-calculated masks, 0xff will pass any
        int exclLoyaltyMask = (opt.FilterExcludeByLoyalty == 0)     ? 0xffffffff : ~opt.FilterExcludeByLoyalty;
        int onlyLoyaltyMask = (opt.FilterIncludeOnlyByLoyalty == 0) ? 0xffffffff : opt.FilterIncludeOnlyByLoyalty;
        int filterMask      = (opt.FilterByType == 0)               ? 0xffffffff : opt.FilterByType;
        int objectMask      = (opt.FilterExcludeObjectId == -1)     ? 0xffffffff : ~opt.FilterExcludeObjectId;
        float x = opt.OriginX;
        float y = opt.OriginY;
        float radius = opt.SearchRadius;
        SearchFilterFunc filterFunc = opt.FilterFunction;

        int maxResults = opt.MaxResults;
        int numResults = 0;
        do
        {
            QtreeBoundedNode current = stack.pop();

            int count = current.node->Count;
            const SpatialObj* items = current.node->Items;
            for (int i = 0; i < count; ++i)
            {
                const SpatialObj& so = items[i];
                if (so.Active
                    && (so.Loyalty & exclLoyaltyMask)
                    && (so.Loyalty & onlyLoyaltyMask)
                    && (so.Type     & filterMask)
                    && (so.ObjectId & objectMask))
                {
                    // check if inside radius, inlined for perf
                    float dx = x - so.CX;
                    float dy = y - so.CY;
                    float r2 = radius + so.Radius;
                    if ((dx*dx + dy*dy) <= (r2*r2))
                    {
                        if (!filterFunc || filterFunc(so.ObjectId) != 0)
                        {
                            outResults[numResults++] = so.ObjectId;
                            if (numResults >= maxResults)
                                return numResults; // we are done !
                        }
                    }
                }
            }

            if (current.node->NW != nullptr)
            {
                QtreeBoundedNode sw = current.sw();
                if (sw.overlaps(enclosingRect))
                    stack.push(sw);

                QtreeBoundedNode se = current.se();
                if (se.overlaps(enclosingRect))
                    stack.push(se);
                
                QtreeBoundedNode ne = current.ne();
                if (ne.overlaps(enclosingRect))
                    stack.push(ne);
                
                QtreeBoundedNode nw = current.nw();
                if (nw.overlaps(enclosingRect))
                    stack.push(nw);
            }
        }
        while (stack.next >= 0);
        
        return numResults;
    }
    
    static const float Brown[4]  = { 139, 69,  19, 150 };
    static const float Violet[4] = { 199, 21, 133, 100 };
    static const float Blue[4]   = { 95, 158, 160, 100 };
    static const float Red[4]    = { 255, 69,   0, 100 };
    static const float Yellow[4] = { 255, 255,  0, 100 };

    void QuadTree::debugVisualize(QtreeVisualizer& visualizer) const
    {
        SmallStack<QtreeBoundedNode> stack;
        stack.push(Root);
        do
        {
            QtreeBoundedNode current = stack.pop();
            visualizer.drawRect(current.left, current.top, current.right, current.bottom, Brown);
            //char text[64];
            //snprintf(text, sizeof(text), "{%d,%d}", (int)current.cx, (int)current.cy);
            //visualizer.drawText(current.cx, current.cy, text, Red);

            int count = current.node->Count;
            const SpatialObj* items = current.node->Items;
            for (int i = 0; i < count; ++i)
            {
                const SpatialObj& so = items[i];
                visualizer.drawRect(so.Bounds.left, so.Bounds.top, so.Bounds.right, so.Bounds.bottom, Violet);
                //visualizer.drawCircle(so.CX, so.CY, so.Radius, Violet);
                visualizer.drawLine(current.cx, current.cy, so.CX, so.CY, Violet);
            }

            if (current.node->NW != nullptr)
            {
                QtreeBoundedNode sw = current.sw();
                if (visualizer.isVisible(sw.left, sw.top, sw.right, sw.bottom))
                    stack.push(sw);

                QtreeBoundedNode se = current.se();
                if (visualizer.isVisible(se.left, se.top, se.right, se.bottom))
                    stack.push(se);
                
                QtreeBoundedNode ne = current.ne();
                if (visualizer.isVisible(ne.left, ne.top, ne.right, ne.bottom))
                    stack.push(ne);
                
                QtreeBoundedNode nw = current.nw();
                if (visualizer.isVisible(nw.left, nw.top, nw.right, nw.bottom))
                    stack.push(nw);
            }
        }
        while (stack.next >= 0);
    }

    void QuadTree::markForRemoval(int objectId, SpatialObj& so)
    {
        so.Active = 0;
        so.ObjectId = -1;
    }


}
