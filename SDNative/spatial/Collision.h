#pragma once
#include "SpatialObject.h"
#include "SlabAllocator.h"
#include "SpatialObjectArray.h"
#include "Primitives.h"

namespace spatial
{
    /**
     * Collision mask which matches all object types
     */
    constexpr uint8_t CollisionMaskAll = 0xff;

    struct CollisionParams
    {
        bool ignoreSameLoyalty = false; // if TRUE, ignore objects of same loyalty
        bool sortCollisionsById = true; // if TRUE, collision results are sorted by object Id-s, ascending
        bool showCollisions = false; // if TRUE, collided objects are saved for debug
    };

    using CollisionPairs = Array<CollisionPair>;

    class Collider
    {
        SlabAllocator& Allocator;
        int MaxObjectId; // for estimating bit-array size

        // Maps ObjectA ID to a chain of collided pairs
        // @note The map of chains is faster than a map of arrays
        //       and 20x faster than a simple array of collisions
        struct CollisionChain
        {
            int b; // object B
            CollisionChain* next;
        };
        CollisionChain** CollidedObjectsMap;

        CollisionPairs Results {};

    public:

        explicit Collider(SlabAllocator& allocator, int maxObjectId);

        /**
         * @param arr Sorted array of spatial objects
         * @param params Collision parameters
         */
        void collideObjects(SpatialObjectsView arr, const CollisionParams& params);

        /**
         * Get collision results and applies final modifications to the result array
         */
        CollisionPairs getResults(const CollisionParams& params);

    private:

        bool tryCollide(CollisionPair pair);
    };

}
