using System;
using System.Runtime.InteropServices;

namespace Ship_Game.Spatial
{
    [StructLayout(LayoutKind.Sequential, Pack=4)]
    public struct SpatialObj // sizeof: 36 bytes, neatly fits in one cache line
    {
        // NOTE: These are ordered by the order of access pattern
        public byte Active;  // 1 if this item is active, 0 if DEAD and pending removal
        public GameObjectType Type; // GameObjectType : byte
        public byte CollisionMask; // mask which matches objects this object can collide with
        public byte Loyalty; // Loyalty ID
        public uint LoyaltyMask; // mask for matching loyalty, see GetLoyaltyMask
        public int ObjectId;
        public AABoundingBox2D AABB;

        public SpatialObj(GameplayObject go, int objectId)
        {
            Active = 1;
            Type = go.Type;
            CollisionMask = go.DisableSpatialCollision ? (byte)0 : NativeSpatialObject.GetCollisionMask(Type);
            int loyaltyId = go.GetLoyaltyId();
            Loyalty = (byte)loyaltyId;
            LoyaltyMask = NativeSpatialObject.GetLoyaltyMask(loyaltyId);
            ObjectId = objectId;
            AABB = new AABoundingBox2D(go);
        }
    }
}