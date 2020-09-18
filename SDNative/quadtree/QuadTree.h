#pragma once
#include "QtreeConstants.h"
#include "QtreeRect.h"
#include "QtreeNode.h"
#include "QtreeAllocator.h"
#include <vector>

namespace tree
{
    /// <summary>
    /// Function for handling collisions between two objects
    /// Return 0: no collision
    /// Return 1: collision happened
    /// </summary>
    using CollisionFunc = int (__stdcall*)(int objectA, int objectB);

    /// <summary>
    /// Function type for final filtering of search results
    /// Return 0: failed, Return 1: passed
    /// </summary>
    using SearchFilterFunc = int (__stdcall*)(int objectA);

    struct SearchOptions
    {
        /// <summary>
        /// The initial search origin X, Y coordinates
        /// </summary>
        int OriginX = 0;
        int OriginY = 0;

        /// <summary>
        /// Only objects that are within this radius are accepted
        /// </summary>
        int SearchRadius = 100;

        /// <summary>
        /// Maximum number of filtered final results until search is terminated
        /// Must be at least 1
        /// </summary>
        int MaxResults = 10;

        /// <summary>
        /// Filter search results by object type
        /// 0: disabled
        /// </summary>
        int FilterByType = 0;

        /// <summary>
        /// Filter search results by excluding this specific object
        /// -1: disabled
        /// </summary>
        int FilterExcludeObjectId = -1;

        /// <summary>
        /// Filter search results by excluding objects with this loyalty
        /// 0: disabled
        /// </summary>
        int FilterExcludeByLoyalty = 0;

        /// <summary>
        /// Filter search results by only matching objects with this loyalty
        /// 0: disabled
        /// </summary>
        int FilterIncludeOnlyByLoyalty = 0;

        /// <summary>
        /// Filter search results by passing the matched object through this function
        /// null: disabled
        /// Return 0: filter failed, object discarded
        /// Return 1: filter passed, object added to results
        /// </summary>
        SearchFilterFunc FilterFunction = nullptr;
    };

    struct QtreeVisualizer
    {
        virtual void drawRect  (int x1, int y1, int x2, int y2,  const float color[4]) = 0;
        virtual void drawCircle(int x,  int y,  int radius,      const float color[4]) = 0;
        virtual void drawLine  (int x1, int y1, int x2, int y2,  const float color[4]) = 0;
        virtual void drawText  (int x,  int y, const char* text, const float color[4]) = 0;
        virtual bool isVisible (int x1, int y1, int x2, int y2) const = 0;
    };

    class TREE_API QuadTree
    {
        int FullSize;
        int UniverseSize;

        QtreeBoundedNode Root { nullptr, 0, 0, 0, 0, 0, 0 };

        // NOTE: Cannot use std::unique_ptr here due to dll-interface
        QtreeAllocator* FrontAlloc = new QtreeAllocator{};
        QtreeAllocator* BackAlloc  = new QtreeAllocator{};


    public:

        explicit QuadTree(int universeSize, int smallestCell);
        ~QuadTree();
        
        QuadTree(QuadTree&&) = delete;
        QuadTree(const QuadTree&) = delete;
        QuadTree& operator=(QuadTree&&) = delete;
        QuadTree& operator=(const QuadTree&) = delete;

        int fullSize() const { return FullSize; }
        int universeSize() const { return UniverseSize; }

        QtreeBoundedNode createRoot();

        void updateAll(const std::vector<QtreeObject>& objects);

        void insertAt(QtreeBoundedNode node, const QtreeObject& o, QtreeRect target);

        void insert(const QtreeBoundedNode& root, const QtreeObject& o)
        {
            insertAt(root, o, o.bounds());
        }
        void removeAt(QtreeNode* root, int objectId);

        void collideAll(float timeStep, CollisionFunc onCollide);
        void collideAllRecursive(float timeStep, CollisionFunc onCollide);

        int findNearby(int* outResults, const SearchOptions& opt);

        void debugVisualize(QtreeVisualizer& visualizer) const;

    private:
        void markForRemoval(int objectId, QtreeObject& o);
        QtreeBoundedNode findEnclosingNode(const QtreeBoundedNode& node, const QtreeRect obj);
    };
}
