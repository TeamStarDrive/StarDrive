using System.Diagnostics.Contracts;

using Newtonsoft.Json;
using Ship_Game.Gameplay;
using Ship_Game.Ships;
using System.Xml.Serialization;
using System;
using System.Runtime.CompilerServices;
using Ship_Game.Data.Serialization;
using Ship_Game.ExtensionMethods;
using SDGraphics;

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
        SolarSystem = 64,
        Planet = 128,
    }

    [StarDataType]
    public abstract class GameObject
    {
        /**
         *  @note Careful! Any property/variable that doesn't have [XmlIgnore][JsonIgnore]
         *        will be accidentally serialized!
         */
        
        [StarData] public readonly int Id;
        [XmlIgnore][JsonIgnore] public bool Active = true;
        [XmlIgnore][JsonIgnore] public SolarSystem System { get; private set; }
        
        [StarData] public Vector2 Position;
        [StarData] public Vector2 Velocity;

        // Velocity magnitude (scalar), always absolute
        [XmlIgnore][JsonIgnore] public float CurrentVelocity => Velocity.Length();

        // important for hi-precision impact predictor and accurate Position integration
        [StarData] public Vector2 Acceleration;

        // rotation in RADIANS
        // MUST be normalized to [0; +2PI]
        [StarData] public float Rotation;

        [StarData] public float Radius = 1f;
        [StarData] public float Mass = 1f;
        [StarData] public float Health;

        [StarData] public readonly GameObjectType Type;

        [XmlIgnore][JsonIgnore] public GameObject LastDamagedBy;

        [XmlIgnore][JsonIgnore] public int SpatialIndex = -1;
        [XmlIgnore][JsonIgnore] public bool DisableSpatialCollision = false; // if true, object is never added to spatial manager
        [XmlIgnore][JsonIgnore] public bool ReinsertSpatial = false; // if true, this object should be reinserted to spatial manager
        [XmlIgnore][JsonIgnore] public bool InFrustum; // Updated by UniverseObjectManager

        /// <summary>
        /// Current Rotation converted into a Direction unit vector
        /// </summary>
        [XmlIgnore][JsonIgnore] public Vector2 Direction
        {
            get => Rotation.RadiansToDirection();
            set => Rotation = value.ToRadians(); // allow setting the rotation with a direction vector
        }
        [XmlIgnore][JsonIgnore] public Vector3 Direction3D
        {
            get => Rotation.RadiansToDirection3D();
            set => Rotation = new Vector2(value.X, value.Y).ToRadians();
        }

        // Current direction of the Velocity vector, or Vector2.Zero if Velocity is Zero
        [XmlIgnore][JsonIgnore] public Vector2 VelocityDirection => Velocity.Normalized();

        // gets/set the Rotation in Degrees; Properly normalizes input degrees to [0; +2PI]
        [XmlIgnore][JsonIgnore] public float RotationDegrees
        {
            get => Rotation.ToDegrees();
            set => Rotation = value.ToRadians();
        }

        // @return Distance from `this` to `target`
        public float Distance(GameObject target) => Position.Distance(target.Position);

        // @return True if `this` is overlapping with `target`
        public bool InRadius(GameObject target)
        {
            float dx = Position.X - target.Position.X;
            float dy = Position.Y - target.Position.Y;
            float r2 = Radius + target.Radius;
            return (dx*dx + dy*dy) <= (r2*r2);
        }

        // @return True if `this` is overlapping with `target`
        public bool InRadius(GameObject target, float extraRadius)
        {
            float dx = Position.X - target.Position.X;
            float dy = Position.Y - target.Position.Y;
            float r2 = Radius + target.Radius + extraRadius;
            return (dx*dx + dy*dy) <= (r2*r2);
        }

        public override string ToString() => $"GameObj Id={Id} Pos={Position}";

        protected GameObject(int id, GameObjectType type)
        {
            Id = id;
            Type = type;
        }

        [XmlIgnore][JsonIgnore] public virtual IDamageModifier DamageMod => InternalDamageModifier.Instance;

        public virtual void Damage(GameObject source, float damageAmount)
        {
        }

        //public virtual void Initialize()
        //{
        //}

        public virtual void Die(GameObject source, bool cleanupOnly)
        {
            Active = false;
        }

        public virtual void RemoveFromUniverseUnsafe()
        {
        }

        // in system view and inside frustum
        public bool IsInFrustum(UniverseScreen u) =>
            u.IsSystemViewOrCloser && u.Frustum.Contains(Position, 2000f);

        [XmlIgnore][JsonIgnore]
        public string SystemName => System?.Name ?? "Deep Space";

        public void SetSystem(SolarSystem system)
        {
            System = system;
        }

        [Pure] public int GetLoyaltyId()
        {
            if (Type == GameObjectType.Proj) return ((Projectile)this).Loyalty?.Id ?? 0;
            if (Type == GameObjectType.Beam) return ((Beam)this).Loyalty?.Id ?? 0;
            if (Type == GameObjectType.Ship) return ((Ship)this).Loyalty.Id;
            return 0;
        }

        [Pure] public Empire GetLoyalty()
        {
            if (Type == GameObjectType.Proj) return ((Projectile)this).Loyalty;
            if (Type == GameObjectType.Beam) return ((Beam)this).Loyalty;
            if (Type == GameObjectType.Ship) return ((Ship)this).Loyalty;
            if (Type == GameObjectType.ShipModule) return ((ShipModule)this).GetParent().Loyalty;
            return null;
        }

        public virtual bool IsAttackable(Empire attacker, Relationship attackerRelationThis)
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


        /// /////////////////////////////////////// ///
        ///    Velocity and Thrust manipulation     ///
        /// /////////////////////////////////////// ///

        public void SetInitialVelocity(Vector2 velocity, bool rotateToVelocity = true)
        {
            Velocity = velocity;
            if (rotateToVelocity)
                Rotation = velocity.Normalized().ToRadians(); // used for drawing the projectile in correct direction
            Acceleration = Vector2.Zero;
        }

        /// /////////////////////////////////////// ///
        ///  Velocity and Acceleration integration  ///
        /// /////////////////////////////////////// ///
        
        // Automatically handles Velocity and Position integration for accurate results
        // Uses either Velocity Verlet integrator or Implicit Euler integrator if acceleration is Zero
        public void UpdateVelocityAndPosition(float dt, Vector2 newAcc)
        {
            bool zeroAcc = newAcc.AlmostZero();
            if (zeroAcc) newAcc = default;

            // if there's any kind of newAcc or oldAcc, use Verlet:
            if (!zeroAcc || Acceleration != Vector2.Zero) 
            {
                IntegratePosVelocityVerlet(dt, newAcc);
            }
            else
            {

                // no acceleration, we can use implicit euler
                IntegrateExplicitEulerConstantVelocity(dt);
            }
        }

        // Velocity Verlet integration method
        // significantly more stable and accurate than ExplicitEuler or SemiImplicitEuler
        // 1. Get the new acceleration for this frame
        // 2. Update the current position
        //     -- using previous frame's acceleration
        //     -- using previous frames' velocity
        // 3. Update the velocity using old and new acceleration
        // @param dt Delta Time for the Simulation
        public void IntegratePosVelocityVerlet(float dt, Vector2 newAcc)
        {
            Vector2 pos = Position;
            Vector2 vel = Velocity;
            Vector2 oldAcc = Acceleration;
            
            // integrate position using Velocity Verlet method:
            // x' = x + v*dt + (a*dt^2)/2
            float dt2 = dt*dt*0.5f;
            pos.X += (vel.X*dt + oldAcc.X*dt2);
            pos.Y += (vel.Y*dt + oldAcc.Y*dt2);

            // integrate velocity using Velocity Verlet method:
            // v' = v + (a0+a1)*0.5*dt
            vel.X += (oldAcc.X+newAcc.X)*0.5f*dt;
            vel.Y += (oldAcc.Y+newAcc.Y)*0.5f*dt;

            Position = pos;
            Velocity = vel;
            Acceleration = newAcc;
        }

        // Minimal Explicit Euler method, which assumes constant velocity
        // Use with care, because if velocity is not constant the errors are going to be huge.
        // For 1000 units, the position error can be even 100 units with variable velocity
        public void IntegrateExplicitEulerConstantVelocity(float dt)
        {
            // new position: x' = x + v'*dt
            Vector2 pos = Position;
            Vector2 vel = Velocity;
            pos.X += vel.X*dt;
            pos.Y += vel.Y*dt;
            Position = pos;
        }
        
        // Implicit Euler Method (aka Backward Euler Method)
        // is a more accurate form of the classic pos' = pos + vel*dt
        // https://www.gafferongames.com/post/integration_basics/
        // but not as good as Velocity Verlet
        public void IntegratePosImplicitEuler(float dt, Vector2 newAcc)
        {
            Vector2 pos = Position;
            Vector2 vel = Velocity;

            // new velocity: v' = v + a*dt
            vel.X += newAcc.X*dt; // vel updated first with `New Acceleration`
            vel.Y += newAcc.Y*dt;

            // new position: x' = x + v'*dt
            pos.X += vel.X*dt; // and use new vel
            pos.Y += vel.Y*dt;
            
            Position = pos;
            Velocity = vel;
            Acceleration = newAcc;
        }

        // Classic Explicit Euler Method (aka Forward Euler Method)
        // https://www.gafferongames.com/post/integration_basics/
        // Explicit Euler suffers from precision issues if object has Acceleration
        // however it's perfectly alright with constant Velocity
        public void IntegrateExplicitEuler(float dt)
        {
            Vector2 pos = Position;
            Vector2 vel = Velocity;
            Vector2 oldAcc = Acceleration;

            // new position: x' = x + v'*dt
            pos.X += vel.X*dt; // pos is 1 step behind, use old Velocity
            pos.Y += vel.Y*dt;

            // new velocity: v' = v + a*dt
            vel.X += oldAcc.X*dt;
            vel.Y += oldAcc.Y*dt;

            Position = pos;
            Velocity = vel;
        }

        // apply thrust limit, so we don't cause oscillating SAS thrust
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        protected static void PrecisionAccelerate(ref Vector2 acc, in Vector2 thrustDir,
                                                  float maxThrust, float desiredThrust)
        {
            float precisionThrust = Math.Min(maxThrust*2f, desiredThrust);
            acc.X += thrustDir.X * precisionThrust; // NOTE: intentional manual inlining
            acc.Y += thrustDir.Y * precisionThrust;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        protected static void PrecisionDecelerate(ref Vector2 acc, in Vector2 thrustDir,
                                                  float maxThrust, float desiredThrust)
        {
            float precisionThrust = Math.Min(maxThrust*2f, desiredThrust);
            acc.X -= thrustDir.X * precisionThrust; // NOTE: intentional manual inlining
            acc.Y -= thrustDir.Y * precisionThrust;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        protected static void Accelerate(ref Vector2 acc, in Vector2 dir, float thrust)
        {
            acc.X += dir.X * thrust; // NOTE: intentional manual inlining
            acc.Y += dir.Y * thrust;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        protected static void Decelerate(ref Vector2 acc, in Vector2 dir, float thrust)
        {
            acc.X -= dir.X * thrust; // NOTE: intentional manual inlining
            acc.Y -= dir.Y * thrust;
        }
    }
}