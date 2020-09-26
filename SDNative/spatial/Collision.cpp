#include "Collision.h"

namespace spatial
{
    Collider::Collider(SlabAllocator& allocator, int maxObjectId)
        : Allocator{allocator}, MaxObjectId{maxObjectId}
    {
        CollisionBits = allocator.allocArray<uint32_t>( (maxObjectId / 32) + 1);
        CollidedObjectsMap = allocator.allocArrayZeroed<CollisionChain*>(maxObjectId + 1);
    }

    int Collider::collideObjects(SpatialObjectArray arr, const CollisionParams& params)
    {
        int numCollision = 0;
        bool ignoreSame = params.ignoreSameLoyalty;
        bool showCollisions = params.showCollisions;

        for (int i = 0; i < arr.size; ++i)
        {
            SpatialObject& objectA = *arr.objects[i];
            if (!objectA.active)
                continue;

            float ax = objectA.x;
            float ay = objectA.y;
            float ar = objectA.rx;
            uint8_t loyaltyA = objectA.loyalty;
            uint8_t collisionMaskA = objectA.collisionMask;

            for (int j = i + 1; j < arr.size; ++j)
            {
                SpatialObject& objectB = *arr.objects[j];
                if (!objectB.active || (collisionMaskA & objectB.collisionMask) == 0)
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
                        ++numCollision;
                        if (showCollisions)
                        {
                            Collisions.addId(Allocator, pair.a, 128);
                            Collisions.addId(Allocator, pair.b, 128);
                        }

                        CollisionResult result = params.onCollide(params.user, pair.a, pair.b);
                        switch (result)
                        {
                            case CollisionResult::NoSideEffects:
                                break;
                            case CollisionResult::ObjectAKilled:
                                objectA.active = 0;
                                goto nextObjectA;
                            case CollisionResult::ObjectBKilled:
                                objectB.active = 0;
                                break;
                            case CollisionResult::BothKilled:
                                objectA.active = 0;
                                objectB.active = 0;
                                goto nextObjectA;
                        }
                    }
                }
            }
            nextObjectA:continue;
        }
        return numCollision;
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

