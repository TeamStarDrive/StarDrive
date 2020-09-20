#pragma once
#include "QtreeConstants.h"
#include "QtreeRect.h"
#include "QtreeNode.h"
#include "QtreeAllocator.h"
#include <vector>

namespace tree
{
    /// <summary>
    /// Describes the result of the collision callback
    /// This gives greater flexibility
    /// </summary>
    enum class CollisionResult : int
    {
        NoSideEffects, // no visible side effect from collision in the quadtree (both objects still alive)
        ObjectAKilled, // objects collided and objectA was killed (no further collision possible)
        ObjectBKilled, // objects collided and objectB was killed (no further collision possible)
        BothKilled,    // both objects were killed during collision (no further collision possible)
    };

    /// <summary>
    /// Function for handling collisions between two objects
    /// @return CollisionResult Result of the collision
    /// @param user User defined pointer for passing context
    /// @param objectA ID of the first object colliding
    /// @param objectB 
    /// </summary>
    using CollisionFunc = CollisionResult (*)(void* user, int objectA, int objectB);

    /// <summary>
    /// Function type for final filtering of search results
    /// @return 0 Search filter failed and this object should be excluded
    /// @return 1 Search filter Succeeded and object should be included
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

    struct QtreeVisualizerOptions
    {
        QtreeRect visibleWorldRect; // this visible area in world coordinates that should be drawn
        bool objectBounds = true; // show bounding box around inserted objects
        bool objectToLeafLines = true; // show connections from Leaf node to object center
        bool objectText = false; // show text ontop of each object (very, very intensive)
        bool nodeText = true; // show text ontop of a leaf or branch node
        bool nodeBounds = true; // show edges of leaf and branch nodes
    };


    class TREE_API QuadTree
    {
        int Levels;
        int FullSize;
        int UniverseSize;

        // Since we're not able to modify the tree while it's being built
        // Defer the split threshold setting to `rebuild` method
        int PendingSplitThreshold = QuadDefaultLeafSplitThreshold; // pending until next `rebuild()`
        int CurrentSplitThreshold = QuadDefaultLeafSplitThreshold; // actual value used
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

        /**
         * Sets the LEAF node split threshold during next `rebuild()`
         */
        void setLeafSplitThreshold(int threshold) { PendingSplitThreshold = threshold; }

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

        void insertAt(int level, QtreeNode& root, const QtreeObject& o);
        void insertAtLeaf(int level, QtreeNode& leaf, const QtreeObject& o);
        void removeAt(QtreeNode* root, int objectId);

    public:

        /**
         * Collide all objects and call CollisionFunc for each collided pair
         * @note Once two objects have collided, they cannot collide anything else during collideAll
         * @param timeStep The fixed physics time step to ensure objects do not pass through when collision testing
         * @param user User defined pointer for passing application specific context
         * @param onCollide Collision resolution callback
         */
        void collideAll(float timeStep, void* user, CollisionFunc onCollide);

        template<class CollisionCallback>
        void collideAll(float timeStep, const CollisionCallback& callback)
        {
            this->collideAll(timeStep, (void*)&callback, [](void* user, int objectA, int objectB) -> CollisionResult
            {
                const CollisionCallback& callback = *reinterpret_cast<const CollisionCallback*>(user);
                return callback(objectA, objectB);
            });
        }

        int findNearby(int* outResults, const SearchOptions& opt);

        /**
         * Iterates through the Quadtree and submits draw calls to objects that overlap the visible rect
         * @param visible The visible area in World coordinates
         */
        void debugVisualize(const QtreeVisualizerOptions& opt, QtreeVisualizer& visualizer) const;

    private:
        void markForRemoval(int objectId, QtreeObject& o);
    };

    TREE_C_API QuadTree* __stdcall QtreeCreate(int universeSize, int smallestCell);
    TREE_C_API void __stdcall QtreeDestroy(QuadTree* tree);
    TREE_C_API void __stdcall QtreeClear(QuadTree* tree);
    TREE_C_API void __stdcall QtreeRebuild(QuadTree* tree);
    TREE_C_API int  __stdcall QtreeInsert(QuadTree* tree, const QtreeObject& o);
    TREE_C_API void __stdcall QtreeUpdate(QuadTree* tree, int objectId, int x, int y);
    TREE_C_API void __stdcall QtreeRemove(QuadTree* tree, int objectId);
    TREE_C_API void __stdcall QtreeCollideAll(QuadTree* tree, float timeStep, void* user, tree::CollisionFunc onCollide);
    TREE_C_API int __stdcall QtreeFindNearby(QuadTree* tree, int* outResults, const tree::SearchOptions& opt);
    TREE_C_API void __stdcall QtreeDebugVisualize(QuadTree* tree, const QtreeVisualizerOptions& opt, const QtreeVisualizerBridge& vis);
}
