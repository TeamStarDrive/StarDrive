#pragma once
#include "QtreeNode.h"
#include "../Visualizer.h"
#include <vector>

namespace spatial
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

    /**
     * A fast QuadTree implementation
     *  -) Linear SLAB Allocators for cheap dynamic growth
     *  -) Bulk collision reaction function
     *  -) Fast search via findNearby
     */
    class TREE_API Qtree
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

        std::vector<SpatialObject> Objects;
        std::vector<SpatialObject> Pending;

    public:

        explicit Qtree(int universeSize, int smallestCell);
        ~Qtree();
        
        Qtree(Qtree&&) = delete;
        Qtree(const Qtree&) = delete;
        Qtree& operator=(Qtree&&) = delete;
        Qtree& operator=(const Qtree&) = delete;

        int fullSize() const { return FullSize; }
        int universeSize() const { return UniverseSize; }

        /**
         * @return Number of QtreeObjects in this tree
         */
        int count() const { return (int)Objects.size(); }

        /**
         * @return Gets an object by its ObjectId
         */
        const SpatialObject& get(int objectId) const { return Objects[objectId]; }

        /**
         * Sets the LEAF node split threshold during next `rebuild()`
         */
        void setLeafSplitThreshold(int threshold) { PendingSplitThreshold = threshold; }

        /**
         * @return Total number of bytes used entire Qtree, including all its auxiliary buffers
         */
        uint32_t totalMemory() const;

    private:

        QtreeNode* createRoot() const;

    public:

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
        int insert(const SpatialObject& o);

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

        void insertAt(int level, QtreeNode& root, SpatialObject* o);
        void insertAtLeaf(int level, QtreeNode& leaf, SpatialObject* o);
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
         * Iterates through the QuadTree and submits draw calls to objects that overlap the visible rect
         * @param opt Visualization options
         * @param visualizer Visualization interface for drawing primitives
         */
        void debugVisualize(const VisualizerOptions& opt, Visualizer& visualizer) const;

    private:
        void markForRemoval(int objectId, SpatialObject& o);
    };

    TREE_C_API Qtree* __stdcall QtreeCreate(int universeSize, int smallestCell);
    TREE_C_API void __stdcall QtreeDestroy(Qtree* tree);
    TREE_C_API void __stdcall QtreeClear(Qtree* tree);
    TREE_C_API void __stdcall QtreeRebuild(Qtree* tree);
    TREE_C_API int  __stdcall QtreeInsert(Qtree* tree, const SpatialObject& o);
    TREE_C_API void __stdcall QtreeUpdate(Qtree* tree, int objectId, int x, int y);
    TREE_C_API void __stdcall QtreeRemove(Qtree* tree, int objectId);
    TREE_C_API void __stdcall QtreeCollideAll(Qtree* tree, float timeStep, void* user, spatial::CollisionFunc onCollide);
    TREE_C_API int __stdcall QtreeFindNearby(Qtree* tree, int* outResults, const spatial::SearchOptions& opt);
    TREE_C_API void __stdcall QtreeDebugVisualize(Qtree* tree, const VisualizerOptions& opt, const VisualizerBridge& vis);
}
