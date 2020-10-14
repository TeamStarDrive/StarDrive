using System;
using System.Runtime.InteropServices;
using Microsoft.Xna.Framework;
using Ship_Game.Gameplay;
using Ship_Game.Ships;

namespace Ship_Game
{
    [StructLayout(LayoutKind.Sequential, Pack=4)]
    public struct SpatialObj // sizeof: 36 bytes, neatly fits in one cache line
    {
        // NOTE: These are ordered by the order of access pattern
        public byte Active;  // 1 if this item is active, 0 if DEAD and pending removal
        public byte Loyalty;        // if loyalty == 0, then this is a STATIC world object !!!
        public GameObjectType Type; // GameObjectType : byte

        public GameplayObject Obj;

        public float CX, CY; // Center x y
        public float Radius;
        public AABoundingBox2D AABB;

        public override string ToString() => Obj.ToString();

        public SpatialObj(GameplayObject go)
        {
            Active = 1;
            Loyalty = (byte)go.GetLoyaltyId();
            Type    = go.Type;
            Obj     = go;
            CX      = Obj.Center.X;
            CY      = Obj.Center.Y;
            Radius  = Obj.Radius;
            AABB = new AABoundingBox2D(go);
        }

        public static bool HitTestBeam(Beam beam, ref SpatialObj target, out ShipModule hitModule, out float distanceToHit)
        {
            ++GlobalStats.BeamTests;

            Vector2 beamStart = beam.Source;
            Vector2 beamEnd   = beam.Destination;

            if (target.Type == GameObjectType.Ship) // beam-ship is special collision
            {
                var ship = (Ship)target.Obj;
                hitModule = ship.RayHitTestSingle(beamStart, beamEnd, 8f, beam.IgnoresShields);
                if (hitModule == null)
                {
                    distanceToHit = float.NaN;
                    return false;
                }
                return hitModule.RayHitTest(beamStart, beamEnd, 8f, out distanceToHit);
            }

            hitModule = null;
            if (target.Type == GameObjectType.Proj)
            {
                var proj = (Projectile)target.Obj;
                if (!proj.Weapon.Tag_Intercept) // for projectiles, make sure they are physical and can be killed
                {
                    distanceToHit = float.NaN;
                    return false;
                }
            }

            // intersect projectiles or anything else that can collide
            var center = new Vector2(target.CX, target.CY);
            return center.RayCircleIntersect(target.Radius, beamStart, beamEnd, out distanceToHit);
        }

        // assumes THIS is a projectile
        public bool HitTestProj(float simTimeStep, ref SpatialObj target, out ShipModule hitModule)
        {
            hitModule = null;
            float dx = CX - target.CX;
            float dy = CY - target.CY;
            float r2 = Radius + target.Radius;
            if ((dx*dx + dy*dy) > (r2*r2)) // filter out by target Ship or target Projectile radius
                return false;
            // NOTE: this is for Projectile<->Projectile collision!
            if (target.Type != GameObjectType.Ship) // target not a ship, collision success
                return true;

            // ship collision, target modules instead
            var proj = (Projectile)Obj;
            var ship = (Ship)target.Obj;
            if (ship == null) { Log.Warning("HitTestProj had a null ship."); return false; }

            float velocity = proj.Velocity.Length();
            float maxDistPerFrame = velocity * simTimeStep;

            // if this projectile will move more than 15 units (1 module grid = 16x16) within one simulation step
            // we have to use ray-casting to avoid projectiles clipping through objects
            if (maxDistPerFrame > 15f)
            {
                Vector2 dir = proj.Velocity / velocity;
                var prevPos = new Vector2(CX - dir.X*maxDistPerFrame, CY - dir.Y*maxDistPerFrame);
                var center = new Vector2(CX, CY);
                hitModule = ship.RayHitTestSingle(prevPos, center, Radius, proj.IgnoresShields);
            }
            else
            {
                hitModule = ship.HitTestSingle(proj.Center, proj.Radius, proj.IgnoresShields);
            }
            return hitModule != null;
        }
    }
}