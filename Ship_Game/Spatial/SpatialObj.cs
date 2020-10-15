using System;
using System.Runtime.InteropServices;
using Ship_Game.Spatial;

namespace Ship_Game
{
    [StructLayout(LayoutKind.Sequential, Pack=4)]
    public struct SpatialObj // sizeof: 36 bytes, neatly fits in one cache line
    {
        // NOTE: These are ordered by the order of access pattern
        public byte Active;  // 1 if this item is active, 0 if DEAD and pending removal
        public byte Loyalty;        // if loyalty == 0, then this is a STATIC world object !!!
        public GameObjectType Type; // GameObjectType : byte
        public byte CollisionMask; // mask which matches objects this object can collide with

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
            CollisionMask = go.DisableSpatialCollision ? (byte)0 : NativeSpatialObject.GetCollisionMask(Type);
            Obj     = go;
            CX      = Obj.Center.X;
            CY      = Obj.Center.Y;
            Radius  = Obj.Radius;
            AABB = new AABoundingBox2D(go);
        }
    }
}