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
        static void CollideShipAtNode(float simTimeStep, QtreeNode node, ref SpatialObj ship)
        {
            for (int i = 0; i < node.Count; ++i)
            {
                ref SpatialObj proj = ref node.Items[i]; // potential projectile ?
                if (proj.Loyalty != ship.Loyalty      && // friendlies don't collide
                    (proj.Type & GameObjectType.Proj) != 0 && // only collide with projectiles
                    (proj.Type & GameObjectType.Beam) == 0 && // forbid obj-beam tests; beam-obj is handled by CollideBeamAtNode
                    proj.HitTestProj(simTimeStep, ref ship, out ShipModule hitModule))
                {
                    var projectile = proj.Obj as Projectile;
                    if (!HandleProjCollision(projectile, hitModule ?? ship.Obj))
                        continue; // there was no collision

                    if (projectile.DieNextFrame) FastRemoval(projectile, node, ref i);
                }
            }
            if (node.NW == null) return;
            CollideShipAtNode(simTimeStep, node.NW, ref ship);
            CollideShipAtNode(simTimeStep, node.NE, ref ship);
            CollideShipAtNode(simTimeStep, node.SE, ref ship);
            CollideShipAtNode(simTimeStep, node.SW, ref ship);
        }

        //@HACK sometime Obj is null and crash the game. added if null mark dienextframe false. 
        //This is surely a bug but the hack might need to be true?
        static bool ProjectileIsDying(ref SpatialObj obj)
            => (obj.Type & GameObjectType.Proj) != 0 && ((obj.Obj as Projectile)?.DieNextFrame ?? false);


        // projectile collision, return the first match because the projectile destroys itself anyway
        static bool CollideProjAtNode(float simTimeStep, QtreeNode node, ref SpatialObj proj)
        {
            for (int i = 0; i < node.Count; ++i)
            {
                ref SpatialObj item = ref node.Items[i];
                if (item.Loyalty != proj.Loyalty &&           // friendlies don't collide, also ignores self
                    (item.Type & GameObjectType.Beam) == 0 && // forbid obj-beam tests; beam-obj is handled by CollideBeamAtNode
                    proj.HitTestProj(simTimeStep, ref item, out ShipModule hitModule))
                {
                    if (!HandleProjCollision(proj.Obj as Projectile, hitModule ?? item.Obj)) // module OR projectile
                        continue; // there was no collision

                    if ((item.Type & GameObjectType.Proj) != 0 && (item.Obj as Projectile).DieNextFrame)
                        FastRemoval(item.Obj, node, ref i);
                    return true;
                }
            }
            if (node.NW == null) return false;
            return CollideProjAtNode(simTimeStep, node.NW, ref proj)
                || CollideProjAtNode(simTimeStep, node.NE, ref proj)
                || CollideProjAtNode(simTimeStep, node.SE, ref proj)
                || CollideProjAtNode(simTimeStep, node.SW, ref proj);
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
                if (item.Loyalty != beam.Loyalty &&        // friendlies don't collide
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

        static void CollideAllAt(float simTimeStep, QtreeNode node, Array<BeamHitResult> beamHitCache)
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
                GameplayObject go = so.Obj;
                if (go == null) // FIX: concurrency issue, someone already removed this item
                    continue;
                if (go.Active == false)
                    continue; // already collided inside this loop

                // each collision instigator type has a very specific recursive handler
                if ((so.Type & GameObjectType.Beam) != 0)
                {
                    CollideBeamAtNode(node, ref so, beamHitCache);
                }
                else if ((so.Type & GameObjectType.Proj) != 0)
                {
                    if (CollideProjAtNode(simTimeStep, node, ref so) && ProjectileIsDying(ref so))
                        FastRemoval(go, node, ref i);
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

        static bool HandleProjCollision(Projectile projectile, GameplayObject victim)
        {
            return projectile.Touch(victim);
        }
    }
}