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
     *
     */
    struct CollisionRule
    {
        uint8_t typeA; // first type of the collision rule
        uint8_t typeB; // second type of the collision rule
        uint8_t ignoreSameLoyalty; // should two friendly objects ignore each other? 
    };

    /**
     * Implements collision rule set to pre-filter objects
     */
    struct CollisionRuleSet
    {
        // 256 x 256 bit-matrix
        uint32_t* CollisionMatrix;

        CollisionRuleSet();
        ~CollisionRuleSet();

        // no copy, no move
        CollisionRuleSet(const CollisionRuleSet&) = delete;
        CollisionRuleSet(CollisionRuleSet&&) = delete;
        CollisionRuleSet& operator=(const CollisionRuleSet&) = delete;
        CollisionRuleSet& operator=(CollisionRuleSet&&) = delete;
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

        explicit Collider(SlabAllocator& allocator, int maxObjectId);

        /**
         * @param arr Sorted array of spatial objects
         * @param user User pointer for onCollide
         * @param onCollide collision response callback
         */
        void collideObjects(SpatialObjectArray arr, void* user, CollisionFunc onCollide);

    private:

        bool tryCollide(CollisionPair pair);
    };

}
