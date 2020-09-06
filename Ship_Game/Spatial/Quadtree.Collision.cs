using System;
using Microsoft.Xna.Framework;
using Ship_Game.Gameplay;
using Ship_Game.Ships;

namespace Ship_Game
{
    public sealed partial class Quadtree
    {
        // ship collision; this can collide with multiple projectiles..
        // beams are ignored because they may intersect multiple objects and thus require special CollideBeamAtNode
        void CollideShipAtNode(float simTimeStep, QtreeNode node, ref SpatialObj ship)
        {
            for (int i = 0; i < node.Count; ++i)
            {
                ref SpatialObj proj = ref node.Items[i]; // potential projectile ?
                if (proj.PendingRemove == 0 &&                // not pending remove
                    proj.Loyalty != ship.Loyalty &&           // friendlies don't collide
                    (proj.Type & GameObjectType.Proj) != 0 && // only collide with projectiles
                    (proj.Type & GameObjectType.Beam) == 0 && // forbid obj-beam tests; beam-obj is handled by CollideBeamAtNode
                    proj.HitTestProj(simTimeStep, ref ship, out ShipModule hitModule))
                {
                    GameplayObject victim = hitModule ?? ship.Obj;
                    var projectile = (Projectile)proj.Obj;

                    if (IsObjectDead(victim))
                    {
                        Log.Warning($"Ship dead but still in Quadtree: {ship.Obj}");
                        MarkForRemoval(ship.Obj, ref ship);
                    }
                    else if (projectile.Touch(victim) && IsObjectDead(projectile))
                    {
                        MarkForRemoval(projectile, ref proj);
                    }
                }
            }
            if (node.NW == null) return;
            CollideShipAtNode(simTimeStep, node.NW, ref ship);
            CollideShipAtNode(simTimeStep, node.NE, ref ship);
            CollideShipAtNode(simTimeStep, node.SE, ref ship);
            CollideShipAtNode(simTimeStep, node.SW, ref ship);
        }

        // projectile collision, return the first match because the projectile destroys itself anyway
        bool CollideProjAtNode(float simTimeStep, QtreeNode node, Projectile theProj, ref SpatialObj proj)
        {
            for (int i = 0; i < node.Count; ++i)
            {
                ref SpatialObj item = ref node.Items[i];
                if (item.PendingRemove == 0 &&                // not pending remove
                    item.Loyalty != proj.Loyalty &&           // friendlies don't collide, also ignores self
                    (item.Type & GameObjectType.Beam) == 0 && // forbid obj-beam tests; beam-obj is handled by CollideBeamAtNode
                    proj.HitTestProj(simTimeStep, ref item, out ShipModule hitModule))
                {
                    // module OR projectile
                    GameplayObject victim = hitModule ?? item.Obj;
                    if (IsObjectDead(victim))
                    {
                        Log.Warning("Victim dead but still in Quadtree");
                        MarkForRemoval(item.Obj, ref item);
                    }
                    else if (theProj.Touch(victim))
                    {
                        if (IsObjectDead(item.Obj))
                        {
                            MarkForRemoval(item.Obj, ref item);
                        }
                        return true;
                    }
                }
            }
            if (node.NW == null) return false;
            return CollideProjAtNode(simTimeStep, node.NW, theProj, ref proj)
                || CollideProjAtNode(simTimeStep, node.NE, theProj, ref proj)
                || CollideProjAtNode(simTimeStep, node.SE, theProj, ref proj)
                || CollideProjAtNode(simTimeStep, node.SW, theProj, ref proj);
        }

        struct BeamHitResult : IComparable<BeamHitResult>
        {
            public GameplayObject Collided;
            public float Distance;
            public int CompareTo(BeamHitResult other)
            {
                return Distance.CompareTo(other.Distance);
            }
        }

        // we keep this list as a cache to reduce memory pressure
        readonly Array<BeamHitResult> BeamHitCache = new Array<BeamHitResult>();

        static void CollideBeamRecursive(QtreeNode node, ref SpatialObj beam, Array<BeamHitResult> outHitResults)
        {
            for (int i = 0; i < node.Count; ++i)
            {
                ref SpatialObj item = ref node.Items[i];
                if (item.PendingRemove == 0 &&             // not pending remove
                    item.Loyalty != beam.Loyalty &&        // friendlies don't collide
                   (item.Type & GameObjectType.Beam) == 0) // forbid beam-beam collision            
                {
                    if (beam.HitTestBeam(ref item, out ShipModule hitModule, out float dist))
                    {
                        outHitResults.Add(new BeamHitResult
                        {
                            Distance = dist,
                            Collided = hitModule ?? item.Obj
                        });
                    }
                }
            }
            if (node.NW == null) return;
            CollideBeamRecursive(node.NW, ref beam, outHitResults);
            CollideBeamRecursive(node.NE, ref beam, outHitResults);
            CollideBeamRecursive(node.SE, ref beam, outHitResults);
            CollideBeamRecursive(node.SW, ref beam, outHitResults);
        }

        static void CollideBeamAtNode(QtreeNode node, ref SpatialObj beam, Array<BeamHitResult> beamHitCache)
        {
            CollideBeamRecursive(node, ref beam, beamHitCache);

            if (beamHitCache.Count > 0)
            {
                // for beams it's important to only collide the CLOSEST object
                // so we need to sort the hits by distance
                // and then work from closest to farthest until we get a valid collision
                // 
                // Some missiles/projectiles have special dodge features, so we need to check all touches.
                if (beamHitCache.Count > 1)
                    beamHitCache.Sort();

                for (int i = 0; i < beamHitCache.Count; ++i)
                {
                    BeamHitResult hit = beamHitCache[i];
                    if (HandleBeamCollision(beam.Obj as Beam, hit.Collided, hit.Distance))
                        break; // and we're done
                }
                beamHitCache.Clear();
            }
        }

        public void CollideAll()
        {
            CollideAllAt(SimulationStep.FixedTime, Root, BeamHitCache);
        }

        void CollideAllAt(float simTimeStep, QtreeNode node, Array<BeamHitResult> beamHitCache)
        {
            if (node.NW != null) // depth first approach, to early filter LastCollided
            {
                CollideAllAt(simTimeStep, node.NW, beamHitCache);
                CollideAllAt(simTimeStep, node.NE, beamHitCache);
                CollideAllAt(simTimeStep, node.SE, beamHitCache);
                CollideAllAt(simTimeStep, node.SW, beamHitCache);
            }
            for (int i = 0; i < node.Count; ++i)
            {
                ref SpatialObj so = ref node.Items[i];
                if (so.PendingRemove != 0)
                    continue; // already collided inside this loop

                // each collision instigator type has a very specific recursive handler
                if ((so.Type & GameObjectType.Beam) != 0)
                {
                    CollideBeamAtNode(node, ref so, beamHitCache);
                }
                else if ((so.Type & GameObjectType.Proj) != 0)
                {
                    var projectile = (Projectile)so.Obj;
                    if (CollideProjAtNode(simTimeStep, node, projectile, ref so) && projectile.DieNextFrame)
                        MarkForRemoval(so.Obj, ref so);
                }
                else if ((so.Type & GameObjectType.Ship) != 0)
                {
                    CollideShipAtNode(simTimeStep, node, ref so);
                }
            }
        }

        static bool HandleBeamCollision(Beam beam, GameplayObject victim, float hitDistance)
        {
            if (!beam.Touch(victim))
                return false;

            Vector2 beamStart = beam.Source;
            Vector2 beamEnd   = beam.Destination;
            Vector2 hitPos;
            if (hitDistance > 0f)
                hitPos = beamStart + (beamEnd - beamStart).Normalized()*hitDistance;
            else // the beam probably glanced the module from side, so just get the closest point:
                hitPos = victim.Center.FindClosestPointOnLine(beamStart, beamEnd);

            beam.BeamCollidedThisFrame = true;
            beam.ActualHitDestination = hitPos;
            return true;
        }
    }
}