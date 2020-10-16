#include "Collision.h"
#include <algorithm>

namespace spatial
{
    Collider::Collider(SlabAllocator& allocator, int maxObjectId)
        : Allocator{allocator}, MaxObjectId{maxObjectId}
    {
        CollidedObjectsMap = allocator.allocArrayZeroed<CollisionChain*>(maxObjectId + 1);
    }

    void Collider::collideObjects(SpatialObjectsView arr, CellLoyalty loyalty, const CollisionParams& params)
    {
        bool ignoreSame = params.ignoreSameLoyalty;
        if (ignoreSame && loyalty.count <= 1)
            return; // definitely nothing to do here!

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
            chain->next = newNode;
        }
        else
        {
            // create the first chain entry
            CollisionChain* newNode = Allocator.allocUninitialized<CollisionChain>();
            newNode->b = pair.b;
            newNode->next = nullptr;
            CollidedObjectsMap[pair.a] = newNode;
        }
        return true; // we can collide
    }
}

