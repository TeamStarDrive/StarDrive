#include "Collision.h"

namespace spatial
{
    void Collider::collideObjects(SpatialObject** objects, int size, void* user, CollisionFunc onCollide)
    {
        for (int i = 0; i < size; ++i)
        {
            const SpatialObject& objectA = *objects[i];
            if (!objectA.active)
                continue;

            for (int j = i + 1; j < size; ++j)
            {
                const SpatialObject& objectB = *objects[j];
                if (!objectB.active)
                    continue;
                //if (!objectA.overlaps(objectB))
                //    continue;
                float dx = objectA.x - objectB.x;
                float dy = objectA.y - objectB.y;
                float r2 = objectA.rx + objectB.rx;
                if ((dx*dx + dy*dy) <= (r2*r2))
                {
                    CollisionPair pair { objectA.objectId, objectB.objectId };
                    if (tryCollide(pair))
                    {
                        CollisionResult result = onCollide(user, pair.a, pair.b);
                    }
                }
            }
        }
    }

    bool Collider::tryCollide(CollisionPair pair)
    {
        if (collided.contains(pair))
            return false;
        collided.insert(pair);
        return true;
    }
}

