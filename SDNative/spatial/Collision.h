#pragma once
#include "Config.h"
#include "SpatialObject.h"
#include "SlabAllocator.h"
#include "SpatialObjectArray.h"

namespace spatial
{
    /**
     * Describes the result of the collision callback.
     * This allows the collider to ignore objects which
     * no longer participate in further collisions
     */
    enum class CollisionResult : int
    {
        NoSideEffects, // no visible side effect from collision (both objects still alive)
        ObjectAKilled, // objects collided and objectA was killed (no further collision possible)
        ObjectBKilled, // objects collided and objectB was killed (no further collision possible)
        BothKilled,    // both objects were killed during collision (no further collision possible)
    };

    /**
     * Function for handling collisions between two objects
     * @param user User defined pointer for passing context
     * @param objectA ID of the first object colliding
     * @param objectA ID of the second object colliding
     * @return CollisionResult Result of the collision
     */
    using CollisionFunc = CollisionResult (SPATIAL_CC*)(void* user, int objectA, int objectB);

    /**
     * Collision mask which matches all object types
     */
    constexpr uint8_t CollisionMaskAll = 0xff;

    struct CollisionParams
    {
        void* user = nullptr; // user pointer passed to onCollide
        CollisionFunc onCollide = nullptr; // collide reaction callback
        bool ignoreSameLoyalty = false; // if TRUE, ignore objects of same loyalty
        bool showCollisions = false; // if TRUE, collided objects are saved for debug
    };

    struct CollisionPair
    {
        int a, b;
    };

    class Collider
    {
        SlabAllocator& Allocator;
        int MaxObjectId; // for estimating bit-array size

        // bit set to flag which object-id's have already collided
        uint32_t* CollisionBits;

        // Maps ObjectA ID to a chain of collided pairs
        // @note The map of chains is faster than a map of arrays
        //       and 20x faster than a simple array of collisions
        struct CollisionChain
        {
            int b; // object B
            CollisionChain* next;
        };
        CollisionChain** CollidedObjectsMap;

    public:
        SpatialIdArray Collisions;

        explicit Collider(SlabAllocator& allocator, int maxObjectId);

        /**
         * @param arr Sorted array of spatial objects
         * @param params Collision parameters
         */
        int collideObjects(SpatialObjectArray arr, const CollisionParams& params);

    private:

        bool tryCollide(CollisionPair pair);
    };

}
