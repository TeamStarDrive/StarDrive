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
        public GameplayObject Obj;

        public GameObjectType Type; // GameObjectType : byte
        public byte Loyalty;        // if loyalty == 0, then this is a STATIC world object !!!
        public byte OverlapsQuads;  // does it overlap multiple quads?
        public byte LastUpdate;

        public float CX, CY; // Center x y
        public float Radius;
        public float X, Y, LastX, LastY;

        public override string ToString() => Obj.ToString();

        public SpatialObj(GameplayObject go)
        {
            Obj           = go;
            Type          = go.Type;
            Loyalty       = (byte)go.GetLoyaltyId();
            OverlapsQuads = 0;
            LastUpdate    = 0;
            if ((Type & GameObjectType.Beam) != 0)
            {
                var beam = (Beam)go;
                Vector2 source = beam.Source;
                Vector2 target = beam.Destination;
                X     = Math.Min(source.X, target.X);
                Y     = Math.Min(source.Y, target.Y);
                LastX = Math.Max(source.X, target.X);
                LastY = Math.Max(source.Y, target.Y);
                CX = 0f;
                CY = 0f;
                Radius = 0f;
            }
            else
            {
                CX       = Obj.Center.X;
                CY       = Obj.Center.Y;
                Radius   = Obj.Radius;
                X        = CX - Radius;
                Y        = CY - Radius;
                LastX    = CX + Radius;
                LastY    = CY + Radius;
            }
        }

        public SpatialObj(Vector2 center, float radius)
        {
            Obj           = null;
            Type          = GameObjectType.Any;
            Loyalty       = 0;
            OverlapsQuads = 0;
            LastUpdate    = 0;
            CX            = center.X;
            CY            = center.Y;
            Radius        = radius;
            X             = CX - radius;
            Y             = CY - radius;
            LastX         = CX + radius;
            LastY         = CY + radius;
        }

        public void UpdateBounds() // Update SpatialObj bounding box
        {
            if ((Type & GameObjectType.Beam) != 0)
            {
                var beam = (Beam)Obj;
                Vector2 source = beam.Source;
                Vector2 target = beam.Destination;
                X     = Math.Min(source.X, target.X);
                Y     = Math.Min(source.Y, target.Y);
                LastX = Math.Max(source.X, target.X);
                LastY = Math.Max(source.Y, target.Y);
            }
            else
            {
                CX   = Obj.Center.X;
                CY   = Obj.Center.Y;
                Radius   = Obj.Radius;
                X        = CX - Radius;
                Y        = CY - Radius;
                LastX    = CX + Radius;
                LastY    = CY + Radius;
            }
        }

        public bool HitTestBeam(ref SpatialObj target, out ShipModule hitModule, out float distanceToHit)
        {
            var beam = (Beam)Obj;
            ++GlobalStats.BeamTests;

            Vector2 beamStart = beam.Source;
            Vector2 beamEnd   = beam.Destination;

            if ((target.Type & GameObjectType.Ship) != 0) // beam-ship is special collision
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
            if ((target.Type & GameObjectType.Proj) != 0)
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
            if ((target.Type & GameObjectType.Ship) == 0) // target not a ship, collision success
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