#pragma once
#include <cstdint>
#include "Config.h"
#include "SpatialObject.h"
#include "Visualizer.h"
#include "Search.h"
#include "Collision.h"

namespace spatial
{
    /**
     * Describes a generic spatial collection which enables
     * fast query of objects
     */
    class SPATIAL_API Spatial
    {
    public:

        Spatial() = default;
        virtual ~Spatial() = default;

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
         * @return Total number of objects in this Spatial collection
         */
        virtual int count() const = 0;

        /**
         * @return Gets the SpatialObject by its ObjectId
         */
        virtual const SpatialObject& get(int objectId) const = 0;
        
        /**
         * @return Initial node capacity.
         * For Qtree this is the leaf node capacity
         * For Grid this is the initial
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
         * @return The unique ObjectId of this inserted object
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
         * @param timeStep The fixed physics time step to ensure objects do not pass through when collision testing
         * @param user User defined pointer for passing application specific context
         * @param onCollide Collision resolution callback
         */
        virtual void collideAll(float timeStep, void* user, CollisionFunc onCollide) = 0;

        template<class CollisionCallback>
        void collideAll(float timeStep, const CollisionCallback& callback)
        {
            this->collideAll(timeStep, (void*)&callback,
                [](void* user, int objectA, int objectB) -> CollisionResult
            {
                const CollisionCallback& callback = *static_cast<const CollisionCallback*>(user);
                return callback(objectA, objectB);
            });
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
}
