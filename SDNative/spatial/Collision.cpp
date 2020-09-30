#include "Collision.h"
#include <algorithm>

namespace spatial
{
    Collider::Collider(SlabAllocator& allocator, int maxObjectId)
        : Allocator{allocator}, MaxObjectId{maxObjectId}
    {
        CollisionBits = allocator.allocArray<uint32_t>( (maxObjectId / 32) + 1);
        CollidedObjectsMap = allocator.allocArrayZeroed<CollisionChain*>(maxObjectId + 1);
    }

    void Collider::collideObjects(SpatialObjectsView arr, const CollisionParams& params)
    {
        bool ignoreSame = params.ignoreSameLoyalty;

        for (int i = 0; i < arr.size; ++i)
        {
            SpatialObject& objectA = *arr.objects[i];
            float ax = objectA.x;
            float ay = objectA.y;
            float ar = objectA.rx;
            uint8_t loyaltyA = objectA.loyalty;
            uint8_t collisionMaskA = objectA.collisionMask;

            for (int j = i + 1; j < arr.size; ++j)
            {
                SpatialObject& objectB = *arr.objects[j];
                if ((collisionMaskA & objectB.collisionMask) == 0)
                    continue;
                if (ignoreSame && objectB.loyalty == loyaltyA)
                    continue; // ignore same loyalty objects from collision

                float dx = ax - objectB.x;
                float dy = ay - objectB.y;
                float rr = ar + objectB.rx;
                if ((dx*dx + dy*dy) <= (rr*rr))
                {
                    CollisionPair pair { objectA.objectId, objectB.objectId };
                    if (tryCollide(pair))
                    {
                        Results.add(Allocator, pair, 128);
                    }
                }
            }
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

    bool Collider::tryCollide(CollisionPair pair)
    {
        if (CollisionChain* chain = CollidedObjectsMap[pair.a])
        {
            for (;;)
            {
                if (chain->b == pair.b)
                    return false; // already collided

                CollisionChain* next = chain->next;
                if (next == nullptr)
                    break; // end of chain
                chain = next;
            }

            // insert a new node to the end
            CollisionChain* newNode = Allocator.allocUninitialized<CollisionChain>();
            newNode->b = pair.b;
            newNode->next = nullptr;
        }
        else
        {
            // create a new chain entry
            CollisionChain* newNode = Allocator.allocUninitialized<CollisionChain>();
            newNode->b = pair.b;
            newNode->next = nullptr;
            CollidedObjectsMap[pair.a] = newNode;
        }
        return true; // we can collide
    }
}

