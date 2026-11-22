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
        // @note RedFox's design until 2026-11 mapped ObjectA ID to a chain of collided pairs, with his note:
        //       "The map of chains is faster than a map of arrays and 20x faster than a simple array of collisions".
        //       His SDNativeTests scene of 10,000 ships (SimParams defaults) makes about 400 pairs: most ships meet
        //       no other, and the most crowded meet about 6.
        // @note 2026-11, replaying object snapshots from a Combined Arms battle (600 battleships and capitals a side,
        //       a 500-ship pile): a big ship overlapped by thousands of shots got a chain thousands long, and every
        //       overlap found walked it, about 1,100-1,900 steps each, 67-86% of the whole search. With this hash set
        //       the heaviest step took 38 ms instead of 257, with the same pairs in the same order, and that
        //       SDNativeTests scene 0.77 ms instead of 0.88.
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
