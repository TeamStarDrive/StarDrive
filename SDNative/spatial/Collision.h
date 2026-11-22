#pragma once
#include "SpatialObject.h"
#include "SlabAllocator.h"
#include "SpatialObjectArray.h"
#include "Primitives.h"
#include "CellLoyalty.h"

namespace spatial
{
    /**
     * Collision mask which matches all object types
     */
    constexpr uint8_t CollisionMaskAll = 0xff;

    struct CollisionParams
    {
        // if TRUE, ignore objects of same loyalty
        bool ignoreSameLoyalty = false;

        // if TRUE, collision results are sorted by object Id-s, ascending
        bool sortCollisionsById = false;

        // if TRUE, collided objects are saved for debug
        bool showCollisions = false;
    };

    using CollisionPairs = Array<CollisionPair>;

    class Collider
    {
        SlabAllocator& Allocator;

        // pairs collided so far, open-addressing keys (a << 32 | b), so a pair found again in another cell is skipped
        uint64_t* CollidedPairs = nullptr;
        uint32_t PairSlotMask = 0; // capacity - 1, the capacity a power of two
        uint32_t NumCollided = 0;

        // a big cell's overlapping pairs, by their index in the cell
        struct CellPair
        {
            int i, j; // i < j
        };
        Array<CellPair> CellPairs {};

        // a big cell's objects as the sweep reads them, sorted by left edge
        struct SweepEntry
        {
            Rect rect;
            int index; // in the cell
            uint8_t collisionMask;
            uint8_t loyalty;
        };
        Array<SweepEntry> SweepEntries {};

        CollisionPairs Results {};

    public:

        explicit Collider(SlabAllocator& allocator);

        /**
         * @param arr Sorted array of spatial objects
         * @param loyalty Loyalty information of this cell
         * @param params Collision parameters
         */
        void collideObjects(SpatialObjectsView arr, CellLoyalty loyalty, const CollisionParams& params);

        /**
         * Get collision results and applies final modifications to the result array
         */
        CollisionPairs getResults(const CollisionParams& params);

    private:

        void collideBigCell(SpatialObjectsView arr, bool ignoreSame);
        void addPair(const SpatialObject& objectA, const SpatialObject& objectB);
        bool tryCollide(CollisionPair pair);
        void growCollidedPairs();
    };

}
