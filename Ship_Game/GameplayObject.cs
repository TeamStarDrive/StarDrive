using System.Diagnostics.Contracts;
using Microsoft.Xna.Framework;
using Newtonsoft.Json;
using Ship_Game.Gameplay;
using Ship_Game.Ships;
using System.Xml.Serialization;
using System;

namespace Ship_Game
{
    [Flags]
    public enum GameObjectType : byte
    {
        // Can be used as a search filter to match all object types
        Any        = 0,
        Ship       = 1,
        ShipModule = 2,
        Proj       = 4, // this is a projectile, NOT a beam
        Beam       = 8, // this is a BEAM, not a projectile
        Asteroid   = 16,
        Moon       = 32,
    }

    public abstract class GameplayObject
    {
        /**
         *  @note Careful! Any property/variable that doesn't have [XmlIgnore][JsonIgnore]
         *        will be accidentally serialized!
         */

        [XmlIgnore][JsonIgnore] public bool Active = true;
        [XmlIgnore][JsonIgnore] public SolarSystem System { get; private set; }
        [XmlIgnore] [JsonIgnore] public SolarSystem SystemBackBuffer { get; private set; }

        // TODO: Position and Center are duplicates. One of them should be removed eventually.
        [Serialize(0)] public Vector2 Position;
        [Serialize(1)] public Vector2 Center;
        [Serialize(2)] public Vector2 Velocity;

        // rotation in RADIANS
        // MUST be normalized to [0; +2PI]
        [Serialize(3)] public float Rotation;

        [Serialize(4)] public float Radius = 1f;
        [Serialize(5)] public float Mass = 1f;
        [Serialize(6)] public float Health;

        [Serialize(7)] public readonly GameObjectType Type;

        [XmlIgnore][JsonIgnore] public GameplayObject LastDamagedBy;

        [XmlIgnore][JsonIgnore] public int SpatialIndex = -1;
        [XmlIgnore][JsonIgnore] public bool DisableSpatialCollision = false; // if true, object is never added to spatial manager
        [XmlIgnore][JsonIgnore] public bool ReinsertSpatial = false; // if true, this object should be reinserted to spatial manager
        [XmlIgnore][JsonIgnore] public bool InFrustum; // Updated by UniverseObjectManager

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

        protected GameplayObject(GameObjectType type)
        {
            Type = type;
        }

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
        }

        public virtual void RemoveFromUniverseUnsafe()
        {
        }

        [XmlIgnore][JsonIgnore]
        public bool IsInFrustum =>
            Empire.Universe.IsSystemViewOrCloser &&
            Empire.Universe.Frustum.Contains(Center, 2000f);

        [XmlIgnore][JsonIgnore]
        public string SystemName => System?.Name ?? "Deep Space";

        public void SetSystem(SolarSystem system)
        {
            System = system;
        }
        
        public void SetSystemBackBuffer(SolarSystem system)
        {
            SystemBackBuffer = system;
        }

        public void SetSystemFromBackBuffer()
        {
            System           = SystemBackBuffer;
            SystemBackBuffer = null;
        }

        public void ChangeLoyalty(Empire changeTo, bool notification = true)
        {
            // TODO: Should we allow projectiles to change loyalty? They are short lived anyway
            if (Type == GameObjectType.Proj)
            {
                ((Projectile) this).Loyalty = changeTo;
            }
            else if (Type == GameObjectType.Beam)
            {
                ((Beam)this).Loyalty = changeTo;
            }
            else if (Type == GameObjectType.Ship)
            {
                var ship = (Ship) this;
                Empire oldLoyalty = ship.loyalty;
                oldLoyalty.TheyKilledOurShip(changeTo, ship);
                changeTo.WeKilledTheirShip(oldLoyalty, ship);
                ship.ClearFleet();
                ship.AI.ClearOrders();
                oldLoyalty.RemoveShip(ship);

                oldLoyalty.GetEmpireAI().ThreatMatrix.RemovePin(ship);
                changeTo.AddShip(ship);
                ship.shipStatusChanged = true;
                ship.loyalty = changeTo;

                ship.SwitchTroopLoyalty(oldLoyalty, ship.loyalty);
                ship.ReCalculateTroopsAfterBoard();
                ship.ScuttleTimer = -1f; // Cancel any active self destruct 
                ship.PiratePostChangeLoyalty();
                ship.IsGuardian = changeTo.WeAreRemnants;
                
                if (notification)
                {
                    changeTo.AddBoardSuccessNotification(ship);
                    oldLoyalty.AddBoardedNotification(ship);
                }
            }
            ReinsertSpatial = true;
        }

        public int GetLoyaltyId()
        {
            if (Type == GameObjectType.Proj) return ((Projectile)this).Loyalty?.Id ?? 0;
            if (Type == GameObjectType.Beam) return ((Beam)this).Loyalty?.Id ?? 0;
            if (Type == GameObjectType.Ship) return ((Ship)this).loyalty.Id;
            return 0;
        }

        [Pure]
        public Empire GetLoyalty()
        {
            if (Type == GameObjectType.Proj) return ((Projectile)this).Loyalty;
            if (Type == GameObjectType.Beam) return ((Beam)this).Loyalty;
            if (Type == GameObjectType.Ship) return ((Ship)this).loyalty;
            if (Type == GameObjectType.ShipModule) return ((ShipModule)this).GetParent().loyalty;
            return null;
        }

        public virtual bool IsAttackable(Empire attacker, Relationship attackerRelationThis)
        {
            return false;
        }
        public virtual bool ParentIsThis(Ship ship) 
        {
            return false;
        }

        public virtual void Update(FixedSimTime timeStep)
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