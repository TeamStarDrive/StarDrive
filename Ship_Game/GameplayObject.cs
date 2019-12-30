using Microsoft.Xna.Framework;
using Newtonsoft.Json;
using Ship_Game.Audio;
using Ship_Game.Gameplay;
using Ship_Game.Ships;
using System;
using System.Runtime.CompilerServices;
using System.Xml.Serialization;

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
        Moon       = 32
    }

    public abstract class GameplayObject
    {
        /**
         *  @note Careful! Any property/variable that doesn't have [XmlIgnore][JsonIgnore]
         *        will be accidentally serialized!
         */

        [XmlIgnore][JsonIgnore] public bool Active = true;
        [XmlIgnore][JsonIgnore] protected AudioHandle DeathSfx = new AudioHandle();
        [XmlIgnore][JsonIgnore] public SolarSystem System { get; private set; }

        // TODO: Position and Center are duplicates. One of them should be removed eventually.
        [Serialize(0)] public Vector2 Position;
        [Serialize(1)] public Vector2 Center;
        [Serialize(2)] public Vector2 Velocity;

        // rotation in RADIANS
        // MUST be normalized to [0; +2PI]
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

        // current rotation converted into a direction vector
        [XmlIgnore][JsonIgnore] public Vector2 Direction   => Rotation.RadiansToDirection();
        [XmlIgnore][JsonIgnore] public Vector3 Direction3D => Rotation.RadiansToDirection3D();

        // Current direction of the Velocity vector, or Vector2.Zero if Velocity is Zero
        [XmlIgnore][JsonIgnore] public Vector2 VelocityDirection => Velocity.Normalized();

        // gets/set the Rotation in Degrees; Properly normalizes input degrees to [0; +2PI]
        [XmlIgnore][JsonIgnore] public float RotationDegrees
        {
            get => Rotation.ToDegrees();
            set => Rotation = value.ToRadians();
        }

        private static int GameObjIds;
        [XmlIgnore][JsonIgnore] public int Id = ++GameObjIds;

        protected GameplayObject(GameObjectType typeFlags)
        {
            Type = typeFlags;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool Is(GameObjectType flags) => (Type & flags) != 0;

        [XmlIgnore][JsonIgnore] public virtual IDamageModifier DamageMod => InternalDamageModifier.Instance;

        public virtual void Damage(GameplayObject source, float damageAmount)
        {
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
            {
                UniverseScreen.SpaceManager.Remove(this);
            }
        }

        [XmlIgnore][JsonIgnore]
        public bool IsInFrustum =>
            Empire.Universe.viewState <= UniverseScreen.UnivScreenState.SystemView &&
            Empire.Universe.Frustum.Contains(Center, 2000f);

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
            // spatial collisions are filtered by loyalty,
            // so we need to remove and re-insert after the loyalty change
            if (InSpatial)
            {
                UniverseScreen.SpaceManager.Remove(this);
            }

            if ((Type & GameObjectType.Proj) != 0)
            {
                ((Projectile)this).Loyalty = changeTo;
            }
            else if ((Type & GameObjectType.Ship) != 0)
            {
                var ship = (Ship)this;
                Empire oldLoyalty = ship.loyalty;
                oldLoyalty.TheyKilledOurShip(changeTo, ship);
                changeTo.WeKilledTheirShip(oldLoyalty, ship);
                ship.ClearFleet();
                oldLoyalty.GetShips().QueuePendingRemoval(ship);
                oldLoyalty.RemoveShip(ship);

                oldLoyalty.GetEmpireAI().ThreatMatrix.RemovePin(ship);
                changeTo.AddShipNextFrame(ship);
                ship.shipStatusChanged = true;
                ship.loyalty = changeTo;
            }

            // this resets the spatial management
            SetSystem(null);
        }

        public int GetLoyaltyId()
        {
            if ((Type & GameObjectType.Proj) != 0) return ((Projectile)this).Loyalty?.Id ?? 0;
            if ((Type & GameObjectType.Ship) != 0) return ((Ship)this).loyalty.Id;
            return 0;
        }

        public Empire GetLoyalty()
        {
            if ((Type & GameObjectType.Proj) != 0) return ((Projectile)this).Loyalty;
            if ((Type & GameObjectType.Ship) != 0) return ((Ship)this).loyalty;
            if ((Type & GameObjectType.ShipModule) != 0) return ((ShipModule)this).GetParent().loyalty;
            return null;
        }

        public virtual bool IsAttackable(Empire attacker, Relationship attackerRelationThis)
        {
            return false;
        }

        public virtual void Update(float elapsedTime)
        {
        }

        // certain modules/ships have jamming/ecm properties which make
        // it harder for missiles to hit them and for weapons to predict our future trajectory
        // @return Error offset or Vector2.Zero if no jamming error
        public virtual Vector2 JammingError()
        {
            return Vector2.Zero;
        }

        public virtual void OnDamageInflicted(ShipModule victim, float damage)
        {
        }

        public override string ToString() => $"GameObj Id={Id} Pos={Position}";
    }
}