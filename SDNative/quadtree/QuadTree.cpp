#include "QuadTree.h"
#include "QtreeConstants.h"

namespace tree
{
    QuadTree::QuadTree(float universeSize, float smallestCell)
    {
        Levels = 1;
        FullSize = smallestCell;
        while (FullSize < universeSize)
        {
            ++Levels;
            FullSize *= 2;
        }
        QuadToLinearSearchThreshold = FullSize * QuadToLinearRatio;
    }

    QuadTree::~QuadTree() = default;

    QtreeNode* QuadTree::createRoot()
    {
        // swap the front and back-buffer
        // the front buffer will be reset and reused
        // while the back buffer will be untouched until next time
        std::swap(FrontBuffer, BackBuffer);
        FrontBuffer->reset();
        
        float half = FullSize / 2;
        return FrontBuffer->newNode(Levels, -half, -half, +half, +half);
    }

    void QuadTree::subdivide(QtreeNode& node, int level)
    {
        float midX = (node.X1 + node.X2) / 2;
        float midY = (node.Y1 + node.Y2) / 2;

        int nextLevel = level - 1;
        node.NW = FrontBuffer->newNode(nextLevel, node.X1, node.Y1, midX,    midY);
        node.NE = FrontBuffer->newNode(nextLevel, midX,    node.Y1, node.X2, midY);
        node.SE = FrontBuffer->newNode(nextLevel, midX,    midY,    node.X2, node.Y2);
        node.SW = FrontBuffer->newNode(nextLevel, node.X1, midY,    midX,    node.Y2);

        int count = node.Count;
        if (count != 0)
        {
            SpatialObj* arr = node.Items;
            node.Count = 0;
            node.Capacity = 0;
            node.Items = nullptr;
            node.TotalTreeDepthCount -= count;

            // and now reinsert all items one by one
            for (int i = 0; i < count; ++i)
                insertAt(&node, level, arr[i]);
        }
    }

    QtreeNode* QuadTree::pickSubQuadrant(QtreeNode& node, const SpatialObj& obj)
    {
        float midX = (node.X1 + node.X2) / 2;
        float midY = (node.Y1 + node.Y2) / 2;

        if (obj.X < midX && obj.LastX < midX) // left
        {
            if (obj.Y <  midY && obj.LastY < midY) return node.NW; // top left
            if (obj.Y >= midY)                     return node.SW; // bot left
        }
        else if (obj.X >= midX) // right
        {
            if (obj.Y <  midY && obj.LastY < midY) return node.NE; // top right
            if (obj.Y >= midY)                     return node.SE; // bot right
        }
        return nullptr; // obj does not perfectly fit inside a quadrant
    }

    void QuadTree::insertAt(QtreeNode* node, int level, const SpatialObj& so)
    {
        for (;;)
        {
            if (level <= 1) // no more subdivisions possible
            {
                node->add(*FrontBuffer, so);
                return;
            }

            if (node->NW != nullptr)
            {
                if (QtreeNode* quad = pickSubQuadrant(*node, so))
                {
                    ++node->TotalTreeDepthCount;
                    node = quad; // go deeper!
                    --level;
                    continue;
                }
            }

            // item belongs to this node
            node->add(*FrontBuffer, so);

            // actually, are we maybe over Threshold and should Subdivide ?
            if (node->NW == nullptr && node->Count >= QuadCellThreshold)
            {
                subdivide(*node, level);
            }
            return;
        }
    }

    struct NodeStack
    {
        static constexpr int MAX = 1024;
        int next = -1;
        QtreeNode* stack[MAX];
        __forceinline void push(QtreeNode* node) { stack[++next] = node; }
        __forceinline QtreeNode* pop() { return stack[next--]; }
    };

    void QuadTree::removeAt(QtreeNode* root, int objectId)
    {
        NodeStack stack; stack.push(root);
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

    int QuadTree::findNearby(int* outResults, int maxResults,
                             float x, float y, float radius,
                             int typeFilter, int objectToIgnoreId, int loyaltyFilter)
    {
        return 0;
    }

    void QuadTree::markForRemoval(int objectId, SpatialObj& so)
    {
        so.PendingRemove = 1;
        so.ObjectId = -1;
    }


}
