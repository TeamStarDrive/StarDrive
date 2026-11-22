#include "Collision.h"
#include <algorithm>
#include <cstring>

namespace spatial
{
    static constexpr uint64_t EmptyPairSlot = ~0ull;
    static constexpr uint32_t InitialPairSlots = 1024;

    Collider::Collider(SlabAllocator& allocator)
        : Allocator{allocator}
    {
        CollidedPairs = allocator.allocArray<uint64_t>(InitialPairSlots);
        memset(CollidedPairs, 0xFF, InitialPairSlots * sizeof(uint64_t));
        PairSlotMask = InitialPairSlots - 1;
    }

    void Collider::collideObjects(SpatialObjectsView arr, CellLoyalty loyalty, const CollisionParams& params)
    {
        bool ignoreSame = params.ignoreSameLoyalty;
        if (ignoreSame && loyalty.count <= 1)
            return; // definitely nothing to do here!

        if (arr.size > CollisionSweepThreshold)
        {
            collideBigCell(arr, ignoreSame);
            return;
        }

        for (int i = 0; i < arr.size; ++i)
        {
            SpatialObject& objectA = *arr.objects[i];
            uint8_t loyaltyA = objectA.loyalty;
            uint8_t collisionMaskA = objectA.collisionMask;
            Rect rectA = objectA.rect;

            for (int j = i + 1; j < arr.size; ++j)
            {
                SpatialObject& objectB = *arr.objects[j];
                if ((collisionMaskA & objectB.collisionMask) == 0)
                    continue;
                if (ignoreSame && objectB.loyalty == loyaltyA)
                    continue; // ignore same loyalty objects from collision

                if (rectA.overlaps(objectB.rect))
                {
                    addPair(objectA, objectB);
                }
            }
        }
    }

    // by left edge, each object checked against those starting at or before its right edge; pairs added in (i, j) order as above
    void Collider::collideBigCell(SpatialObjectsView arr, bool ignoreSame)
    {
        int size = arr.size;
        SweepEntries.size = 0;
        for (int i = 0; i < size; ++i)
        {
            const SpatialObject& o = *arr.objects[i];
            SweepEntries.add(Allocator, SweepEntry{o.rect, i, o.collisionMask, o.loyalty}, 256);
        }
        std::sort(SweepEntries.begin(), SweepEntries.end(), [](const SweepEntry& x, const SweepEntry& y)
        {
            return x.rect.x1 < y.rect.x1;
        });

        CellPairs.size = 0;
        const SweepEntry* entries = SweepEntries.data;
        for (int k = 0; k < size; ++k)
        {
            SweepEntry a = entries[k];
            for (int m = k + 1; m < size; ++m)
            {
                const SweepEntry& b = entries[m];
                if (b.rect.x1 > a.rect.x2)
                    break; // this one and all after it start right of A
                if ((a.collisionMask & b.collisionMask) == 0)
                    continue;
                if (ignoreSame && b.loyalty == a.loyalty)
                    continue;

                if (a.rect.overlaps(b.rect))
                {
                    CellPairs.add(Allocator, a.index < b.index ? CellPair{a.index, b.index} : CellPair{b.index, a.index}, 256);
                }
            }
        }

        std::sort(CellPairs.begin(), CellPairs.end(), [](CellPair x, CellPair y)
        {
            return x.i != y.i ? x.i < y.i : x.j < y.j;
        });
        for (const CellPair& p : CellPairs)
        {
            addPair(*arr.objects[p.i], *arr.objects[p.j]);
        }
    }

    void Collider::addPair(const SpatialObject& objectA, const SpatialObject& objectB)
    {
        CollisionPair pair { objectA.objectId, objectB.objectId };
        if (tryCollide(pair))
        {
            Results.add(Allocator, pair, 128);
        }
    }

    CollisionPairs Collider::getResults(const CollisionParams& params)
    {
        if (params.sortCollisionsById)
        {
            std::sort(Results.begin(), Results.end(),
            [](CollisionPair first, CollisionPair second)
            {
                return first.a < second.a && first.b < second.b;
            });
        }
        return Results;
    }

    SPATIAL_FINLINE static uint32_t pairSlot(uint64_t key)
    {
        return (uint32_t)((key * 0x9E3779B97F4A7C15ull) >> 32);
    }

    bool Collider::tryCollide(CollisionPair pair)
    {
        if ((NumCollided + 1) * 2 > PairSlotMask + 1)
            growCollidedPairs();

        uint64_t key = ((uint64_t)(uint32_t)pair.a << 32) | (uint32_t)pair.b;
        for (uint32_t slot = pairSlot(key) & PairSlotMask;; slot = (slot + 1) & PairSlotMask)
        {
            uint64_t found = CollidedPairs[slot];
            if (found == key)
                return false; // already collided

            if (found == EmptyPairSlot)
            {
                CollidedPairs[slot] = key;
                ++NumCollided;
                return true; // we can collide
            }
        }
    }

    void Collider::growCollidedPairs()
    {
        uint64_t* old = CollidedPairs;
        uint32_t oldCapacity = PairSlotMask + 1;
        uint32_t capacity = oldCapacity * 2;

        CollidedPairs = Allocator.allocArray<uint64_t>((int)capacity);
        memset(CollidedPairs, 0xFF, capacity * sizeof(uint64_t));
        PairSlotMask = capacity - 1;

        for (uint32_t i = 0; i < oldCapacity; ++i)
        {
            uint64_t key = old[i];
            if (key == EmptyPairSlot)
                continue;
            uint32_t slot = pairSlot(key) & PairSlotMask;
            while (CollidedPairs[slot] != EmptyPairSlot)
                slot = (slot + 1) & PairSlotMask;
            CollidedPairs[slot] = key;
        }
    }
}

