using Ship_Game.Gameplay;
using Ship_Game.Ships;
using Ship_Game.Spatial;

namespace Ship_Game.Spatial
{
    public sealed partial class Qtree
    {
        unsafe class Collider
        {
            // Maps ObjectA ID to a chain of collided pairs
            // @note The map of chains is faster than a map of arrays
            //       and 20x faster than a simple array of collisions
            class CollisionChain
            {
                public int B;
                public CollisionChain Next;
            }
            CollisionChain[] CollidedObjectsMap;

            public Array<CollisionPair> Results = new Array<CollisionPair>();

            public Collider(int maxObjectId)
            {
                CollidedObjectsMap = new CollisionChain[maxObjectId + 1];
            }

            public void CollideObjects(SpatialObj*[] objects, int count)
            {
                for (int i = 0; i < count; ++i)
                {
                    SpatialObj* objectA = objects[i];
                    byte loyaltyA = objectA->Loyalty;
                    byte collisionMaskA = objectA->CollisionMask;
                    AABoundingBox2D rectA = objectA->AABB;

                    for (int j = i + 1; j < count; ++j)
                    {
                        SpatialObj* objectB = objects[j];
                        if ((collisionMaskA & objectB->CollisionMask) == 0)
                            continue;
                        if (objectB->Loyalty == loyaltyA)
                            continue; // ignore same loyalty objects from collision

                        if (objectB->AABB.Overlaps(rectA))
                        {
                            var pair = new CollisionPair(objectA->ObjectId, objectB->ObjectId);
                            if (TryCollide(pair))
                            {
                                Results.Add(pair);
                            }
                        }
                    }
                }
            }

            bool TryCollide(CollisionPair pair)
            {
                CollisionChain chain = CollidedObjectsMap[pair.A];
                if (chain != null)
                {
                    for (;;)
                    {
                        if (chain.B == pair.B)
                            return false; // already collided

                        CollisionChain next = chain.Next;
                        if (next == null)
                            break; // end of chain
                        chain = next;
                    }

                    // insert a new node to the end
                    chain.Next = new CollisionChain { B = pair.B };
                }
                else
                {
                    // create the first chain entry
                    CollidedObjectsMap[pair.A] = new CollisionChain { B = pair.B };
                }
                return true; // we can collide
            }
        }

        public unsafe int CollideAll(FixedSimTime timeStep)
        {
            var collider = new Collider(SpatialObjects.Length);
            FindResultBuffer buffer = GetThreadLocalTraversalBuffer(Root);

            do
            {
                QtreeNode current = buffer.Pop();
                if (current.NW != null) // isBranch
                {
                    buffer.PushBack(current.SW);
                    buffer.PushBack(current.SE);
                    buffer.PushBack(current.NE);
                    buffer.PushBack(current.NW);
                }
                else // isLeaf
                {
                    if (current.Count > 1 && current.LoyaltyCount > 1)
                    {
                        collider.CollideObjects(current.Items, current.Count);
                    }
                }
            }
            while (buffer.NextNode >= 0);

            CollisionPair[] candidates = collider.Results.GetInternalArrayItems();
            int numCandidates = collider.Results.Count;
            if (numCandidates == 0)
                return 0;

            fixed (CollisionPair* candidatesPtr = candidates)
            {
                return NarrowPhase.Collide(timeStep, 
                    candidatesPtr, numCandidates, Objects);
            }
        }
    }
}