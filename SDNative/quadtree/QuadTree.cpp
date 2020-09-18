#include "QuadTree.h"
#include "QtreeConstants.h"

namespace tree
{
    QuadTree::QuadTree(int universeSize, int smallestCell)
    {
        FullSize = smallestCell;
        UniverseSize = universeSize;
        while (FullSize < universeSize)
        {
            FullSize *= 2;
        }
        Root = createRoot();
    }

    QuadTree::~QuadTree()
    {
        delete FrontAlloc;
        delete BackAlloc;
    }

    QtreeBoundedNode QuadTree::createRoot()
    {
        // swap the front and back-buffer
        // the front buffer will be reset and reused
        // while the back buffer will be untouched until next time
        std::swap(FrontAlloc, BackAlloc);
        FrontAlloc->reset();

        int half = FullSize / 2;
        return QtreeBoundedNode{FrontAlloc->allocArrayZeroed<QtreeNode>(4), {}, 0, 0, -half, -half, +half, +half };
    }

    void QuadTree::updateAll(const std::vector<QtreeObject>& objects)
    {
        QtreeBoundedNode root = createRoot();
        for (const QtreeObject& so : objects)
        {
            insert(root, so);
        }
        Root = root;
    }

    QtreeBoundedNode QuadTree::findEnclosingNode(const QtreeBoundedNode& node, const QtreeRect obj)
    {
        QtreeBoundedNode current = node;
        for (;;)
        {
            if (current.nodes != nullptr)
            {
                if (obj.left < current.cx && obj.right < current.cx) // left
                {
                    if (obj.top < current.cy && obj.bottom < current.cy) // top left
                    {
                        current = current.nw();
                        continue;
                    }
                    if (obj.top >= current.cy) // bot left
                    {
                        current = current.sw();
                        continue;
                    }
                }
                else if (obj.left >= current.cx) // right
                {
                    if (obj.top < current.cy && obj.bottom < current.cy) // top right
                    {
                        current = current.ne();
                        continue;
                    }
                    if (obj.top >= current.cy) // bot right
                    {
                        current = current.se();
                        continue;
                    }
                }
            }
            // otherwise: target does not perfectly fit inside a quadrant anymore
            break;
        }
        return current;
    }

    void QuadTree::insertAt(QtreeBoundedNode node, const QtreeObject& o, QtreeRect target)
    {
        for (;;)
        {
            // try to select a sub-quadrant, perhaps it's a better match
            if (node.nodes != nullptr)
            {
                if (target.left < node.cx && target.right < node.cx) // left
                {
                    if (target.top < node.cy && target.bottom < node.cy) // top left
                    {
                        node = node.nw();
                        continue;
                    }
                    if (target.top >= node.cy) // bot left
                    {
                        node = node.sw();
                        continue;
                    }
                }
                else if (target.left >= node.cx) // right
                {
                    if (target.top < node.cy && target.bottom < node.cy) // top right
                    {
                        node = node.ne();
                        continue;
                    }
                    if (target.top >= node.cy) // bot right
                    {
                        node = node.se();
                        continue;
                    }
                }

                // target does not perfectly fit inside any sub-quadrants, so it belongs to current Node
            }

            // item belongs to this node
            node.objects.push_back(*FrontAlloc, o);

            // actually, are we maybe over Threshold and should Subdivide ?
            if (node.nodes == nullptr && node.objects.Count >= QuadCellThreshold)
            {
                node.nodes = FrontAlloc->allocArrayZeroed<QtreeNode>(4);

                int count = node.objects.Count;
                if (count != 0)
                {
                    QtreeObject* items = node.objects.Items;
                    node.objects.clear();

                    // and now reinsert all items one by one
                    for (int i = 0; i < count; ++i)
                        insertAt(node, items[i], target);
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
            int count = node.objects.Count;
            QtreeObject* items = node.objects.Items;
            for (int i = 0; i < count; ++i)
            {
                QtreeObject& so = items[i];
                if (so.objectId == objectId)
                {
                    markForRemoval(objectId, so);
                    return;
                }
            }
            if (node.nodes != nullptr)
            {
                stack.push( &node.sw() );
                stack.push( &node.se() );
                stack.push( &node.ne() );
                stack.push( &node.nw() );
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
        int x = opt.OriginX;
        int y = opt.OriginY;
        int radius = opt.SearchRadius;
        SearchFilterFunc filterFunc = opt.FilterFunction;

        int maxResults = opt.MaxResults;
        int numResults = 0;
        do
        {
            QtreeBoundedNode current = stack.pop();

            int count = current.objects.Count;
            const QtreeObject* items = current.objects.Items;
            for (int i = 0; i < count; ++i)
            {
                const QtreeObject& o = items[i];
                if (o.Active
                    && (o.Loyalty & exclLoyaltyMask)
                    && (o.Loyalty & onlyLoyaltyMask)
                    && (o.type     & filterMask)
                    && (o.objectId & objectMask))
                {
                    // check if inside radius, inlined for perf
                    int dx = x - o.x;
                    int dy = y - o.y;
                    int r2 = radius + o.radius;
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

            if (current.nodes != nullptr)
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

            int count = current.objects.Count;
            const QtreeObject* items = current.objects.Items;
            for (int i = 0; i < count; ++i)
            {
                const QtreeObject& o = items[i];
                QtreeRect bounds = o.bounds();

                visualizer.drawRect(bounds.left, bounds.top, bounds.right, bounds.bottom, Violet);
                //visualizer.drawCircle(so.CX, so.CY, so.Radius, Violet);
                visualizer.drawLine(current.cx, current.cy, o.x, o.y, Violet);
            }

            if (current.nodes != nullptr)
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

    void QuadTree::markForRemoval(int objectId, QtreeObject& o)
    {
        o.Active = 0;
        o.objectId = -1;
    }


}
