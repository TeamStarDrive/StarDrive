﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using SDGraphics;
using SDUtils;
using Ship_Game.Gameplay;
using Ship_Game.Ships;
using Vector2 = SDGraphics.Vector2;

namespace Ship_Game.Spatial
{
    public struct CollisionPair
    {
        public static readonly CollisionPair Empty = new CollisionPair(-1, -1);
        public int A;
        public int B;
        public CollisionPair(int a, int b)
        {
            A = a;
            B = b;
        }
    }

    /// <summary>
    /// Narrow-Phase Collision is the fine-grained collision
    /// check done after we have confirmed that object AABB's are overlapping
    /// </summary>
    public unsafe class NarrowPhase
    {
        public struct BeamHitResult : IComparable<BeamHitResult>
        {
            public SpatialObjectBase Collided;
            public float Distance;
            public int CompareTo(BeamHitResult other)
            {
                return Distance.CompareTo(other.Distance);
            }
        }

        public static int Collide(FixedSimTime timeStep,
                                  CollisionPair* collisionPairs, int numPairs,
                                  SpatialObjectBase[] objects)
        {
            int numCollisions = 0;

            // handle the sorted collision pairs
            // for beam weapons, we need to gather all overlaps and find the nearest
            var beamHits = new Array<BeamHitResult>();

            for (int i = 0; i < numPairs; ++i)
            {
                CollisionPair pair = collisionPairs[i];
                if (pair.A == -1) // object removed by beam collision
                    continue;

                SpatialObjectBase objectA = objects[pair.A];
                SpatialObjectBase objectB = objects[pair.B];
                if (objectB == null)
                {
                    Log.Error($"CollideObjects objectB was null at {pair.B}");
                    continue;
                }
                if (!objectA.Active || !objectB.Active)
                    continue; // a collision participant already died

                // beam collision is a special case
                if (objectA.Type == GameObjectType.Beam ||
                    objectB.Type == GameObjectType.Beam)
                {
                    bool isBeamA = objectA.Type == GameObjectType.Beam;
                    int beamId = isBeamA ? pair.A : pair.B;
                    var beam = (Beam)(isBeamA ? objectA : objectB);
                    SpatialObjectBase victim = isBeamA ? objectB : objectA;

                    AddBeamHit(beamHits, beam, victim);

                    // gather and remove all other overlaps with this beam
                    for (int j = i+1; j < numPairs; ++j)
                    {
                        pair = collisionPairs[j];
                        if (pair.A == beamId)
                        {
                            AddBeamHit(beamHits, beam, objects[pair.B]);
                            collisionPairs[j] = CollisionPair.Empty; // remove
                        }
                        else if (pair.B == beamId)
                        {
                            AddBeamHit(beamHits, beam, objects[pair.A]);
                            collisionPairs[j] = CollisionPair.Empty; // remove
                        }
                    }

                    if (beamHits.Count > 0)
                    {
                        // for beams, it's important to only collide the CLOSEST object
                        // so we need to sort the hits by distance
                        // and then work from closest to farthest until we get a valid collision
                        // Some missiles/projectiles have special dodge features,
                        // so we need to check all touches.
                        if (beamHits.Count > 1)
                            beamHits.Sort();

                        for (int hitIndex = 0; hitIndex < beamHits.Count; ++hitIndex)
                        {
                            BeamHitResult hit = beamHits[hitIndex];
                            if (HandleBeamCollision(timeStep, beam, hit.Collided, hit.Distance))
                            {
                                ++numCollisions;
                                break; // and we're done
                            }
                        }
                        beamHits.Clear();
                    }
                }
                else if (objectA.Type == GameObjectType.Proj ||
                         objectB.Type == GameObjectType.Proj)
                {
                    bool isProjA = objectA.Type == GameObjectType.Proj;
                    var proj = (Projectile)(isProjA ? objectA : objectB);
                    SpatialObjectBase victim = isProjA ? objectB : objectA;

                    if (HitTestProj(timeStep.FixedTime, proj, victim, out ShipModule hitModule, out Vector2 hitPos))
                    {
                        if (proj.Touch((GameObject)(hitModule ?? victim), hitPos))
                            ++numCollisions;
                    }
                }
            }
            return numCollisions;
        }

        static bool HandleBeamCollision(FixedSimTime timeStep, Beam beam, SpatialObjectBase victim, float hitDistance)
        {
            if (!beam.Touch(timeStep, (GameObject)victim))
                return false;

            Vector2 beamStart = beam.Source;
            Vector2 beamEnd   = beam.Destination;
            Vector2 hitPos;
            if (hitDistance > 0f)
                hitPos = beamStart + (beamEnd - beamStart).Normalized()*Math.Min(beam.Range,hitDistance);
            else // the beam probably glanced the module from side, so just get the closest point:
                hitPos = victim.Position.FindClosestPointOnLine(beamStart, beamEnd);

            beam.BeamCollidedThisFrame = true;
            beam.SetActualHitDestination(hitPos);
            return true;
        }

        static void AddBeamHit(Array<BeamHitResult> beamHits, Beam beam, SpatialObjectBase victim)
        {
            if (HitTestBeam(beam, victim, out ShipModule hitModule, out float dist))
            {
                beamHits.Add(new BeamHitResult
                {
                    Distance = dist,
                    Collided = hitModule ?? victim
                });
            }
        }

        static bool HitTestBeam(Beam beam, SpatialObjectBase victim, out ShipModule hitModule, out float distanceToHit)
        {
            Vector2 beamStart = beam.Source;
            Vector2 beamEnd   = beam.Destination;

            if (victim.Type == GameObjectType.Ship) // beam-ship is special collision
            {
                var ship = (Ship)victim;
                hitModule = ship.RayHitTestSingle(beamStart, beamEnd, beam.IgnoresShields);
                if (hitModule != null)
                    return hitModule.RayHitTest(beamStart, beamEnd, 8f, out distanceToHit);
                distanceToHit = float.NaN;
                return false;
            }

            hitModule = null;
            if (victim.Type == GameObjectType.Proj)
            {
                var proj = (Projectile)victim;
                if (!proj.Weapon.Tag_Intercept) // for projectiles, make sure they are physical and can be killed
                {
                    distanceToHit = float.NaN;
                    return false;
                }
            }

            // intersect projectiles or anything else that can collide
            return victim.Position.RayCircleIntersect(victim.Radius, beamStart, beamEnd, out distanceToHit);
        }

        static bool HitTestProj(float simTimeStep, Projectile proj, SpatialObjectBase victim,
                                out ShipModule hitModule, out Vector2 hitPos)
        {
            hitPos = proj.Position;
            // NOTE: this is for Projectile<->Projectile collision!
            if (victim.Type != GameObjectType.Ship) // target not a ship, collision success
            {
                hitModule = null;
                return true;
            }

            // ship collision, target modules instead
            var ship = (Ship)victim;
            float velocity = proj.Velocity.Length();
            float maxDistPerFrame = velocity * simTimeStep;
            Vector2 center = proj.Position;

            // if this projectile will move more than 15 units (1 module grid = 16x16) within one simulation step
            // we have to use ray-casting to avoid projectiles clipping through objects
            if (maxDistPerFrame > 15f)
            {
                Vector2 dir = proj.Velocity / velocity;
                var prevPos = new Vector2(center.X - dir.X*maxDistPerFrame, center.Y - dir.Y*maxDistPerFrame);
                hitModule = ship.RayHitTestSingle(prevPos, center, proj.IgnoresShields);
                hitPos = prevPos;
            }
            else
            {
                hitModule = ship.HitTestSingle(center, proj.Radius, proj.IgnoresShields);
            }

            return hitModule != null;
        }

    }
}
