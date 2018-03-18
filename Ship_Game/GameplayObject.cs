using System;
using System.Runtime.CompilerServices;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System.Xml.Serialization;
using Newtonsoft.Json;
using Ship_Game.Gameplay;
using Ship_Game.Ships;

namespace Ship_Game
{
    [Flags]
    public enum GameObjectType : byte
    {
        None       = 0,
        Ship       = 1,
        ShipModule = 2,
        Proj       = 4,
        Beam       = 8,
        Asteroid   = 16,
        Moon       = 32,
    }

    public abstract class GameplayObject
    {
        public static GraphicsDevice device;

        /**
         *  @note Careful! Any property/variable that doesn't have [XmlIgnore][JsonIgnore]
         *        will be accidentally serialized! 
         */

        [XmlIgnore][JsonIgnore] public bool Active = true;
        [XmlIgnore][JsonIgnore] protected AudioHandle DeathSfx;
        [XmlIgnore][JsonIgnore] public SolarSystem System { get; private set; }

        [Serialize(0)] public Vector2 Position;
        [Serialize(1)] public Vector2 Center;
        [Serialize(2)] public Vector2 Velocity;
        [Serialize(3)] public float Rotation;

        [Serialize(4)] public Vector2 Dimensions;
        [Serialize(5)] public float Radius = 1f;
        [Serialize(6)] public float Mass = 1f;
        [Serialize(7)] public float Health;

        [Serialize(8)] public GameObjectType Type;

        [XmlIgnore][JsonIgnore] public GameplayObject LastDamagedBy;

        // -2: pending, -1: not in spatial, >= 0: in spatial
        [XmlIgnore][JsonIgnore] public int SpatialIndex = -1;
        [XmlIgnore][JsonIgnore] public bool NotInSpatial   => SpatialIndex == -1;
        [XmlIgnore][JsonIgnore] public bool InSpatial      => SpatialIndex != -1;
        [XmlIgnore][JsonIgnore] public bool SpatialPending => SpatialIndex == -2;

        [XmlIgnore][JsonIgnore] public bool InDeepSpace => System == null;
        [XmlIgnore][JsonIgnore] public bool DisableSpatialCollision = false; // if true, object is never added to spatial manager

        private static int GameObjIds;
        [XmlIgnore][JsonIgnore] public int Id = ++GameObjIds;
        
        protected GameplayObject(GameObjectType typeFlags)
        {
            Type = typeFlags;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool Is(GameObjectType flags) => (Type & flags) != 0;

        public virtual bool Damage(GameplayObject source, float damageAmount)
        {
            return false;
        }

        public virtual void Initialize()
        {
        }

        public virtual void Die(GameplayObject source, bool cleanupOnly)
        {            
            Active = false;            
            Empire.Universe.QueueGameplayObjectRemoval(this);

        }

        public virtual void RemoveFromUniverseUnsafe()
        {
            if (InSpatial)
                UniverseScreen.SpaceManager.Remove(this);
        }

        [XmlIgnore][JsonIgnore] 
        public string SystemName => System?.Name ?? "Deep Space";

        public void SetSystem(SolarSystem system)
        {
            // SetSystem means this GameplayObject is used somewhere in the universe
            // Regardless whether the system itself is null, we insert self to SpaceManager
            if (!DisableSpatialCollision && Active && NotInSpatial)
                UniverseScreen.SpaceManager.Add(this);

            if (System == system)
                return;

            if (this is Ship ship)
            {
                System?.ShipList.RemoveSwapLast(ship);
                system?.ShipList.AddUnique(ship);
            }
            System = system;
        }

        public void ChangeLoyalty(Empire changeTo)
        {
            if (InSpatial)
                UniverseScreen.SpaceManager.Remove(this);
            if ((Type & GameObjectType.Proj) != 0) ((Projectile)this).Loyalty = changeTo;
            if ((Type & GameObjectType.Ship) != 0) ((Ship)this).loyalty = changeTo;
            if (!DisableSpatialCollision && Active && NotInSpatial)
                UniverseScreen.SpaceManager.Add(this);
        }

        public int GetLoyaltyId()
        {
            if ((Type & GameObjectType.Proj) != 0) return ((Projectile)this).Loyalty?.Id ?? 0;
            if ((Type & GameObjectType.Ship) != 0) return ((Ship)this).loyalty?.Id ?? 0;
            return 0;
        }

        public Empire GetLoyalty()
        {
            if ((Type & GameObjectType.Proj) != 0) return ((Projectile)this).Loyalty;
            if ((Type & GameObjectType.Ship) != 0) return ((Ship)this).loyalty;
            if ((Type & GameObjectType.ShipModule) != 0) return ((ShipModule)this).GetParent().loyalty;
            return null;
        }

        public virtual bool Touch(GameplayObject target)
        {
            return false; // by default, objects can't be touched
        }

        public virtual bool IsAttackable(Empire attacker, Relationship attackerRelationThis)
        {
            return false;
        }

        public virtual void Update(float elapsedTime)
        {
        }

        public virtual Vector2 JitterPosition()
        {
            return Vector2.Zero;            
        }

        public override string ToString() => $"GameObj Id={Id} Pos={Position}";
    }
}