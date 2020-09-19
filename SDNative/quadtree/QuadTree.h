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
    using CollisionFunc = int (*)(int objectA, int objectB);

    /// <summary>
    /// Function type for final filtering of search results
    /// Return 0: failed, Return 1: passed
    /// </summary>
    using SearchFilterFunc = int (*)(int objectA);

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

    struct QtreeColor { uint8_t r, g, b, a; };

    struct QtreeVisualizer
    {
        virtual ~QtreeVisualizer() = default;
        virtual void drawRect  (int x1, int y1, int x2, int y2,  QtreeColor c) = 0;
        virtual void drawCircle(int x,  int y,  int radius,      QtreeColor c) = 0;
        virtual void drawLine  (int x1, int y1, int x2, int y2,  QtreeColor c) = 0;
        virtual void drawText  (int x,  int y, int size, const char* text, QtreeColor c) = 0;
    };

    struct QtreeVisualizerBridge
    {
        void (*DrawRect)  (int x1, int y1, int x2, int y2,  QtreeColor c);
        void (*DrawCircle)(int x,  int y,  int radius,      QtreeColor c);
        void (*DrawLine)  (int x1, int y1, int x2, int y2,  QtreeColor c);
        void (*DrawText)  (int x,  int y, int size, const char* text, QtreeColor c);
    };


    class TREE_API QuadTree
    {
        int Levels;
        int FullSize;
        int UniverseSize;
        int LeafSplitThreshold = QuadDefaultLeafSplitThreshold;

        QtreeNode* Root = nullptr;

        // NOTE: Cannot use std::unique_ptr here due to dll-interface
        QtreeAllocator* FrontAlloc = new QtreeAllocator{};
        QtreeAllocator* BackAlloc  = new QtreeAllocator{};

        std::vector<QtreeObject> Objects;

    public:

        explicit QuadTree(int universeSize, int smallestCell);
        ~QuadTree();
        
        QuadTree(QuadTree&&) = delete;
        QuadTree(const QuadTree&) = delete;
        QuadTree& operator=(QuadTree&&) = delete;
        QuadTree& operator=(const QuadTree&) = delete;

        int fullSize() const { return FullSize; }
        int universeSize() const { return UniverseSize; }

    private:
        QtreeNode* createRoot() const;

    public:

        /**
         * @return Number of QtreeObjects in this tree
         */
        int count() const { return (int)Objects.size(); }

        /**
         * @return Gets an object by its ObjectId
         */
        const QtreeObject& get(int objectId) const { return Objects[objectId]; }

        void setLeafSplitThreshold(int threshold) { LeafSplitThreshold = threshold; }

        /**
         * Clears all of the inserted objects and resets the root 
         */
        void clear();

        /**
         * Rebuilds the quadtree by inserting all current objects
         */
        void rebuild();

        /**
         * Inserts a new object into the Quadtree pending list
         * The object will be actually inserted after `rebuild()` is called
         * @return The unique ObjectId of this inserted object
         */
        int insert(const QtreeObject& o);

        /**
         * Updates position of the specified object
         * This is a deferred update.
         * The actual tree will be updated during `rebuild()`
         */
        void update(int objectId, int x, int y);

        /**
         * Removes an object from the object list and marks it for removal during next rebuild
         */
        void remove(int objectId);

    private:
        void insertAt(int level, QtreeNode* root, const QtreeObject& o);
        void insertAtLeaf(int level, QtreeNode* leaf, const QtreeObject& o);
        void removeAt(QtreeNode* root, int objectId);

    public:
        void collideAll(float timeStep, CollisionFunc onCollide);
        void collideAllRecursive(float timeStep, CollisionFunc onCollide);

        int findNearby(int* outResults, const SearchOptions& opt);

        /**
         * Iterates through the Quadtree and submits draw calls to objects that overlap the visible rect
         * @param visible The visible area in World coordinates
         */
        void debugVisualize(QtreeRect visible, QtreeVisualizer& visualizer) const;

    private:
        void markForRemoval(int objectId, QtreeObject& o);
    };

    TREE_C_API QuadTree* __stdcall QtreeCreate(int universeSize, int smallestCell);
    TREE_C_API void __stdcall QtreeDestroy(QuadTree* tree);
    TREE_C_API void __stdcall QtreeClear(QuadTree* tree);
    TREE_C_API void __stdcall QtreeRebuild(QuadTree* tree);
    TREE_C_API int  __stdcall QtreeInsert(QuadTree* tree, const QtreeObject& o);
    TREE_C_API void __stdcall QtreeUpdatePos(QuadTree* tree, int objectId, int x, int y);
    TREE_C_API void __stdcall QtreeRemove(QuadTree* tree, int objectId);
    TREE_C_API void __stdcall QtreeCollideAll(QuadTree* tree, float timeStep, 
                                             tree::CollisionFunc onCollide);
    TREE_C_API int __stdcall QtreeFindNearby(QuadTree* tree, int* outResults,
                                            const tree::SearchOptions& opt);

    TREE_C_API void __stdcall QtreeDebugVisualize(QuadTree* tree,
                                QtreeRect visible, const QtreeVisualizerBridge& visualizer);
}
