using System;
using System.Runtime.InteropServices;
using Microsoft.Xna.Framework;

namespace Ship_Game.Spatial
{
    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    public struct NativeSpatialObject
    {
        public byte Active;  //1 if this item is active, 0 if this item is DEAD and REMOVED from world
        public byte Loyalty; // if loyalty == 0xff, then this is a STATIC world object !!!
        public byte Type;    // object type used in filtering findNearby queries
        public byte CollisionMask; // mask which matches objects this object can collide with
        
        public int ObjectId; // the object

        public int X, Y; // Center x y
        public int RadiusX; // radius for collision test
        public int RadiusY; // radius for collision test

        public NativeSpatialObject(GameplayObject go)
        {
            Active   = 1;
            Loyalty  = (byte)go.GetLoyaltyId();
            GameObjectType type = go.Type;
            Type     = (byte)type;
            CollisionMask = go.DisableSpatialCollision ? (byte)0 : GetCollisionMask(type);
            ObjectId = -1; // ObjectId will be assigned by Native Spatial system

            X = (int)go.Center.X;
            Y = (int)go.Center.Y;

            if (Type == (byte)GameObjectType.Beam)
            {
                var beam = (Beam)go;
                RadiusX = beam.RadiusX;
                RadiusY = beam.RadiusY;
            }
            else
            {
                RadiusX = RadiusY = (int)go.Radius;
            }
        }

        // ships collide with: projectiles, beams
        const byte ShipMask = (byte)(GameObjectType.Proj | GameObjectType.Beam);

        // projectiles collide with: projectiles, beams, ships
        const byte ProjMask = (byte)(GameObjectType.Proj | GameObjectType.Beam | GameObjectType.Ship);
        
        // beams collide with: projectiles, ships
        const byte BeamMask = (byte)(GameObjectType.Proj | GameObjectType.Ship);

        public static byte GetCollisionMask(GameObjectType type)
        {
            switch (type)
            {
                case GameObjectType.Any: return 0xff;
                case GameObjectType.Ship: return ShipMask;
                case GameObjectType.ShipModule: return 0;
                case GameObjectType.Proj: return ProjMask;
                case GameObjectType.Beam: return BeamMask;
                case GameObjectType.Asteroid: return 0;
                case GameObjectType.Moon: return 0;
                default: return 0;
            }
        }
    }
}
