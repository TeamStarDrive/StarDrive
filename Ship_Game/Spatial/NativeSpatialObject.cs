using System;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace Ship_Game.Spatial
{
    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    public struct NativeSpatialObject
    {
        public byte Active;  //1 if this item is active, 0 if this item is DEAD and REMOVED from world
        public byte Type;    // object type used in filtering findNearby queries
        public byte CollisionMask; // mask which matches objects this object can collide with
        public byte Loyalty; // Loyalty ID
        public uint LoyaltyMask; // mask for matching loyalty, see GetLoyaltyMask
        public int ObjectId; // the object ID
        public AABoundingBox2Di AABB;

        public NativeSpatialObject(GameplayObject go)
        {
            Active = 1;
            GameObjectType type = go.Type;
            Type = (byte)type;
            CollisionMask = go.DisableSpatialCollision ? (byte)0 : GetCollisionMask(type);
            int loyaltyId = go.GetLoyaltyId();
            Loyalty = (byte)loyaltyId;
            LoyaltyMask = GetLoyaltyMask(loyaltyId);
            ObjectId = -1; // ObjectId will be assigned by Native Spatial system
            AABB = new AABoundingBox2Di(go);
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

        /// <summary>
        /// This Loyalty Mask will match with everything
        /// </summary>
        public const uint MatchAll = 0xffff_ffff;

        // Calculates an appropriate bitmask from loyaltyId [1..32]
        // If loyalty is out of range, then MatchAll is returned
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static uint GetLoyaltyMask(int loyaltyId)
        {
            byte id = (byte)(loyaltyId - 1);
            return id < 32 ? (uint)(1 << id) : MatchAll;
        }

        // Gets loyalty mask from search options
        public static uint GetLoyaltyMask(in SearchOptions opt)
        {
            uint loyaltyMask = MatchAll;
            if (opt.OnlyLoyalty != null)
                loyaltyMask = GetLoyaltyMask(opt.OnlyLoyalty.Id);
            if (opt.ExcludeLoyalty != null)
                loyaltyMask = ~GetLoyaltyMask(opt.ExcludeLoyalty.Id);
            return loyaltyMask;
        }
    }
}
