#pragma once
#include "QtreeConstants.h"
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
        float OriginX = 0.0f;
        float OriginY = 0.0f;

        /// <summary>
        /// Only objects that are within this radius are accepted
        /// </summary>
        float SearchRadius = 100.0f;

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

    class TREE_API QuadTree
    {
        int Levels;
        float FullSize;
        float UniverseSize;
        float QuadToLinearSearchThreshold;
        QtreeNode* Root;

        // NOTE: Cannot use std::unique_ptr here due to dll-interface
        QtreeAllocator* FrontBuffer = new QtreeAllocator{10000};
        QtreeAllocator* BackBuffer  = new QtreeAllocator{20000};

    public:

        explicit QuadTree(float universeSize, float smallestCell);
        ~QuadTree();
        
        QuadTree(QuadTree&&) = delete;
        QuadTree(const QuadTree&) = delete;
        QuadTree& operator=(QuadTree&&) = delete;
        QuadTree& operator=(const QuadTree&) = delete;

        int levels() const { return Levels; }
        float fullSize() const { return FullSize; }
        float universeSize() const { return UniverseSize; }


        QtreeNode* createRoot();

        void updateAll(const std::vector<SpatialObj>& objects);

        void insertAt(QtreeNode* node, int level, const SpatialObj& so);
        void insert(QtreeNode* root, const SpatialObj& so) { insertAt(root, Levels, so); }
        void removeAt(QtreeNode* root, int objectId);

        void collideAll(float timeStep, CollisionFunc onCollide);
        void collideAllRecursive(float timeStep, CollisionFunc onCollide);

        int findNearby(int* outResults, const SearchOptions& opt);

    private:
        void markForRemoval(int objectId, SpatialObj& so);
        void subdivide(QtreeNode& node, int level);
        static QtreeNode* pickSubQuadrant(QtreeNode& node, const SpatialObj& so);
        QtreeNode* findEnclosingNode(QtreeNode* node, const SpatialObj& so);
    };
}
