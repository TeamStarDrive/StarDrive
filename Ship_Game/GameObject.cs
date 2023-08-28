using Ship_Game.Gameplay;
using Ship_Game.Ships;
using System;
using System.Runtime.CompilerServices;
using Ship_Game.Data.Serialization;
using SDGraphics;
using Ship_Game.Spatial;

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
        SolarSystem = 16,
        SolarBody   = 32, // Asteroid, Moon,
        Planet      = 64,
        ThreatCluster = 128,
    }

    [StarDataType]
    public abstract class GameObject : SpatialObjectBase
    {
        [StarData] public readonly int Id;
        [StarData] public SolarSystem System;
        [StarData] public Vector2 Velocity;
        // important for hi-precision impact predictor and accurate Position integration
        [StarData] public Vector2 Acceleration;

        // Velocity magnitude (scalar), always absolute
        public float CurrentVelocity => Velocity.Length();

        // rotation in RADIANS
        // MUST be normalized to [0; +2PI]
        [StarData] public float Rotation;

        [StarData] public float Mass = 1f;
        [StarData] public float Health;

        public bool InFrustum; // Updated by UniverseObjectManager

        /// <summary>
        /// Current Rotation converted into a Direction unit vector
        /// </summary>
        public Vector2 Direction
        {
            get => Rotation.RadiansToDirection();
            set => Rotation = value.ToRadians(); // allow setting the rotation with a direction vector
        }
        public Vector3 Direction3D
        {
            get => Rotation.RadiansToDirection3D();
            set => Rotation = new Vector2(value.X, value.Y).ToRadians();
        }

        // Current direction of the Velocity vector, or Vector2.Zero if Velocity is Zero
        public Vector2 VelocityDirection => Velocity.Normalized();

        // gets/set the Rotation in Degrees; Properly normalizes input degrees to [0; +2PI]
        public float RotationDegrees
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

        [StarDataConstructor]
        protected GameObject(int id, GameObjectType type) : base(type)
        {
            Id = id;
        }

        public virtual IDamageModifier DamageMod => InternalDamageModifier.Instance;

        public virtual void Damage(GameObject source, float damageAmount, float beamModifier = 1f)
        {
        }

        public virtual void Die(GameObject source, bool cleanupOnly)
        {
            Active = false;
        }

        // in system view and inside frustum
        public bool IsInFrustum(UniverseScreen u) =>
            u.UState.IsSystemViewOrCloser && u.IsInFrustum(Position, 2000f);

        public string SystemName => System?.Name ?? "Deep Space";

        public void SetSystem(SolarSystem system)
        {
            System = system;
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

        public virtual float DodgeMultiplier()
        {
            return 1;
        }

        public virtual void OnDamageInflicted(ShipModule victim, float damage)
        {
        }


        // /////////////////////////////////////// //
        //    Velocity and Thrust manipulation     //
        // /////////////////////////////////////// //

        public void SetInitialVelocity(Vector2 velocity, bool rotateToVelocity = true)
        {
            Velocity = velocity;
            if (rotateToVelocity)
                Rotation = velocity.Normalized().ToRadians(); // used for drawing the projectile in correct direction
            Acceleration = Vector2.Zero;
        }

        // /////////////////////////////////////// //
        //  Velocity and Acceleration integration  //
        // /////////////////////////////////////// //
        
        // Automatically handles Velocity and Position integration for accurate results
        // Uses either Velocity Verlet integrator or Implicit Euler integrator if acceleration is Zero
        public void UpdateVelocityAndPosition(float dt, Vector2 newAcc, bool isZeroAcc)
        {
            // if there's any kind of newAcc or oldAcc, use Verlet:
            if (!isZeroAcc || Acceleration.X != 0f || Acceleration.Y != 0f) 
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