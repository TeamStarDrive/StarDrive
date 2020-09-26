#pragma once
#include <cstdint>
#include <memory>
#include "Config.h"
#include "SpatialObject.h"
#include "Visualizer.h"
#include "Search.h"
#include "Collision.h"

namespace spatial
{
    enum class SpatialType : int
    {
        Grid, // spatial::Grid
        QuadTree, // spatial::QuadTree
    };

    /**
     * Describes a generic spatial collection which enables
     * fast query of objects
     */
    class SPATIAL_API Spatial
    {
    public:

        /**
         * Virtual factory method for creating a new spacial instance based on the SpatialType
         * @param type Type of spatial collection: Grid, QuadTree, etc.
         * @param universeSize The Width and Height of the simulation world
         * @param cellSize For Grid: size of a single Cell. For Qtree: smallest allowed Qtree node.
         */
        static std::shared_ptr<Spatial> create(SpatialType type, int universeSize, int cellSize);

        Spatial() = default;
        virtual ~Spatial() = default;

        /**
         * Type id of the Spatial collection
         */
        virtual SpatialType type() const = 0;

        /**
         * Debug friendly name of this Spatial collection
         */
        virtual const char* name() const = 0;

        /**
         * @return Total number of bytes used, including all its auxiliary buffers
         */
        virtual uint32_t totalMemory() const = 0;

        /**
         * @return Full Width and Height of the spatial collection. 
         *         This is usually bigger than world size
         */
        virtual int fullSize() const = 0;

        /**
         * @return Original size of the simulation world
         */
        virtual int worldSize() const = 0;

        /**
         * @return Total number of Active objects in this Spatial collection
         * @warning Spatial Id-s are mapped by a FlatMap,
         *          so MaxId will not match NumActive
         *          Ex: There are 1000 reserved Id-s, but only 100 active objects
         */
        virtual int numActive() const = 0;

        /**
         * @return Gets the SpatialObject by its ObjectId
         * @warning These Id-s map to a FlatMap,
         *          so some entries in the middle of the FlatMap can be inactive 
         */
        virtual const SpatialObject& get(int objectId) const = 0;
        
        /**
         * @return Initial node capacity.
         * For Qtree this is the leaf node capacity
         * For Grid this is the initial Cell capacity when a new item is inserted
         * This can control many different aspects of spatial node storage
         */
        virtual int nodeCapacity() const = 0;

        /**
         * Sets a new node capacity.
         * This may control many different aspects of spatial node storage
         */
        virtual void nodeCapacity(int capacity) = 0;

        /**
         * @return The smallest possible Cell size in this Spatial collection
         */
        virtual int smallestCellSize() const = 0;

        /**
         * @return Sets the new smallest cell size.
         * This is allowed to trigger full rebuild if needed.
         */
        virtual void smallestCellSize(int cellSize) = 0;

        /**
         * Clears all of the inserted objects and resets the Spatial collection
         */
        virtual void clear() = 0;

        /**
         * Performs necessary actions to refresh the Spatial collection
         * in a THREAD SAFE manner.
         * This means inserting pending objects, removing objects that are pending removal
         * Handling moved objects and rebuilding the collection as necessary
         */
        virtual void rebuild() = 0;

        /**
         * Inserts a new object into the Spatial collection THREAD SAFELY
         * The object will be visible after the next `update()`
         *
         * @return The unique ObjectId of this inserted object
         *         Valid Object ID-s range from [0 ... maxCount)
         *         And are mapped into a sparse flat map
         */
        virtual int insert(const SpatialObject& o) = 0;

        /**
         * Updates position of the specified object THREAD SAFELY
         * The changes will be visible after next `update()`
         */
        virtual void update(int objectId, int x, int y) = 0;

        /**
         * Removes an object from the object list
         * and marks it for removal during next rebuild
         */
        virtual void remove(int objectId) = 0;

        /**
         * Collide all objects and call CollisionFunc for each collided pair
         * @note Once two objects have collided, they cannot collide anything else during collideAll
         * @param params Collision parameters
         */
        virtual int collideAll(const CollisionParams& params) = 0;

        template<class CollisionCallback>
        int collideAll(CollisionParams params, const CollisionCallback& callback)
        {
            params.user = (void*)&callback;
            params.onCollide = [](void* user, int objectA, int objectB) -> CollisionResult
            {
                const CollisionCallback& callback = *static_cast<const CollisionCallback*>(user);
                return callback(objectA, objectB);
            };
            return this->collideAll(params);
        }

        /**
         * Finds multiple nearby objects. This is THREAD SAFE
         * @param outResults Buffer which will store the object id-s
         * @param opt SearchOptions defining the search constraints and MaxResults
         * @return Number of objects found
         */
        virtual int findNearby(int* outResults, const SearchOptions& opt) const = 0;

        /**
         * Iterates the spatial collection and submits draw calls
         * to objects that overlap the visible rect.
         * @param opt Visualization options
         * @param visualizer Visualization interface for drawing primitives
         */
        virtual void debugVisualize(const VisualizerOptions& opt, Visualizer& visualizer) const = 0;


        // NO COPY, NO MOVE
        Spatial(Spatial&&) = delete;
        Spatial(const Spatial&) = delete;
        Spatial& operator=(Spatial&&) = delete;
        Spatial& operator=(const Spatial&) = delete;
    };

    ///////////////////////////////////////////////////////////////////////////////////////////////

    SPATIAL_C_API Spatial* SPATIAL_CC SpatialCreate(SpatialType type, int universeSize, int cellSize);
    SPATIAL_C_API void SPATIAL_CC SpatialDestroy(Spatial* spatial);

    SPATIAL_C_API SpatialType SPATIAL_CC SpatialGetType(Spatial* spatial);
    SPATIAL_C_API int SPATIAL_CC SpatialWorldSize(Spatial* spatial);
    SPATIAL_C_API int SPATIAL_CC SpatialFullSize(Spatial* spatial);
    SPATIAL_C_API int SPATIAL_CC SpatialNumActive(Spatial* spatial);

    SPATIAL_C_API void SPATIAL_CC SpatialClear(Spatial* spatial);
    SPATIAL_C_API void SPATIAL_CC SpatialRebuild(Spatial* spatial);

    SPATIAL_C_API int SPATIAL_CC  SpatialInsert(Spatial* spatial, const SpatialObject* o);
    SPATIAL_C_API void SPATIAL_CC SpatialUpdate(Spatial* spatial, int objectId, int x, int y);
    SPATIAL_C_API void SPATIAL_CC SpatialRemove(Spatial* spatial, int objectId);

    SPATIAL_C_API void SPATIAL_CC SpatialCollideAll(Spatial* spatial, const CollisionParams* params);
    SPATIAL_C_API int SPATIAL_CC SpatialFindNearby(Spatial* spatial, int* outResults, const SearchOptions* opt);
    SPATIAL_C_API void SPATIAL_CC SpatialDebugVisualize(Spatial* spatial, const VisualizerOptions* opt, const VisualizerBridge* vis);

    ///////////////////////////////////////////////////////////////////////////////////////////////
}
