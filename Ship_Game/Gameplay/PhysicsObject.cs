using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Xna.Framework;
using Ship_Game.Ships;

namespace Ship_Game.Gameplay
{
    // A type of GameObject which can be manipulated using thrust forces
    // such as Ships or Projectiles
    //
    // Additional external forces can also be applied
    //
    public class PhysicsObject : GameplayObject
    {
        // The desired thrust Mode during this frame
        // We don't apply velocity directly to avoid double-acceleration bugs
        // and to ensure coordinate integration is done properly
        public Thrust ThrustThisFrame = Thrust.Coast;
        public ThrustStatus DebugThrustStatus;

        // How much Thrust to Apply in Newtons
        // acceleration = Force/mass
        protected float ThrustAcceleration;

        // Reset every frame. Not serialized.
        // This is the current thrust vector direction in radians (rads for easier vector manipulation)
        protected float ThrustVector;
        
        // Reset every frame. Not serialized.
        // Currently applied external force. All of the external force is converted into acceleration
        protected Vector2 AppliedExternalForce;

        public PhysicsObject(GameObjectType type) : base(type)
        {
        }

        protected struct AccelerationState
        {
            public float ThrustAcc; // maximum thrust acceleration a = m/s^2
            public float Velocity;
            public float MaxVelocity;
            public Vector2 VelocityDir;
            public Vector2 Forward; // object forward dir

            // compares velocity vector against where it is pointing
            // if +1 then obj is going forward as it points forward
            // if  0 then obj is drifting sideways
            // if -1 then obj is drifting reverse
            // velocityDir.Dot(objectForwardDir)
            public float Travel;
            public float DecelerationPower; // multiplier [0.0; 1.0]

            public AccelerationState(in Vector2 velocity, float maxVelocity, float rotation, float maxThrustAcc, float decelerationPower)
            {
                ThrustAcc = maxThrustAcc;

                // inline velocity magnitude and dir (speed optimization)
                float vx = velocity.X, vy = velocity.Y;
                Velocity = (float)Math.Sqrt(vx*vx + vy*vy);
                VelocityDir = Velocity > 0.0001f ? new Vector2(vx/Velocity, vy/Velocity) : default;

                Forward = rotation.RadiansToDirection();
                Travel = VelocityDir.Dot(Forward);

                MaxVelocity = maxVelocity;
                DecelerationPower = decelerationPower;
            }
        }

        // Resets any applied forces. Should be called after Position and Velocity integration is finished
        // These variables are only valid once per frame and must be set every update
        protected void ResetForcesThisFrame()
        {
            ThrustThisFrame = Thrust.Coast;
            ThrustAcceleration = 0f;
            ThrustVector = 0f;
            AppliedExternalForce = Vector2.Zero;
        }

        // Sets the thrust amount of the physics object this frame
        // This is used for Ships, Rockets, etc. Anything that is able to accelerate.
        public void SetThrustThisFrame(float thrustAcceleration, float thrustVector, Thrust status)
        {
            ThrustAcceleration = thrustAcceleration;
            ThrustVector = thrustVector;
            ThrustThisFrame = status;
        }

        // This sets the direction of forward thrust.
        // 0: ship accelerates forward
        // +PI/4: ship accelerates diagonally RIGHT
        // -PI/4: ship accelerates diagonally LEFT
        // @note Thrust direction is always clamped to [-PI/4; +PI/4]
        public void SetThrustVector(float thrustDirectionRadians)
        {
            const float quarterPi = RadMath.PI / 4;
            ThrustVector = thrustDirectionRadians.Clamped(-quarterPi, quarterPi);
        }

        // combining object Rotation(rads) and engine ThrustVector(rads)
        // return the engine nozzle's thrusting direction
        // if ThrustVector=0, this is equal to ship Forward Direction
        protected Vector2 GetThrustVector()
        {
            Vector2 thrustDir = (Rotation + ThrustVector).RadiansToDirection();
            return thrustDir;
        }

        // This applies (accumulates) a Force vector for the duration of this frame.
        // If you wish to transfer force over a larger time period, you must call this over several frames.
        // This is because our velocity/position integration recalculates acceleration every frame
        public void ApplyForce(Vector2 force)
        {
            AppliedExternalForce += force;
        }

        // turns any applied external force into acceleration
        // a = Force/mass
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        protected Vector2 GetAppliedForceAcceleration()
        {
            if (AppliedExternalForce == Vector2.Zero)
                return Vector2.Zero;
            return new Vector2(AppliedExternalForce.X / Mass, AppliedExternalForce.Y / Mass);
        }

        protected Vector2 GetThrustAcceleration(in AccelerationState a)
        {
            // get the current thrust dir and reset the vector for now
            Vector2 thrustDir = GetThrustVector();
            Vector2 acc = default;

            if (ThrustThisFrame == Thrust.AllStop)
            {
                PrecisionStop(ref acc, thrustDir, a.Velocity, a);
            }
            else if (a.Velocity >= a.MaxVelocity) // we are at the speed limit already
            {
                // in order to have direction control at max velocity limit
                // we spend use thrust to slow down in velocity dir and thrust to speed up in wanted dir
                // if we are already facing wanted direction, then this is a no-op
                if (ThrustThisFrame == Thrust.Forward && a.Travel > 0.2f/*we are going forward*/)
                {
                    // if we're over speed limit, then accelerate a bit less than we decelerate
                    float overLimit = (a.Velocity - a.MaxVelocity);
                    float accelerate = overLimit > 1f ? a.ThrustAcc * 0.5f : a.ThrustAcc;
                    Decelerate(ref acc, a.VelocityDir, a.ThrustAcc);
                    Accelerate(ref acc, thrustDir, accelerate);
                    DebugThrustStatus = ThrustStatus.MaxSpeed;
                }
                else
                {
                    // if we're not going forward, use precision stop to slow down correctly (decel or accel)
                    PrecisionStop(ref acc, thrustDir, a.Velocity, a);
                    DebugThrustStatus = ThrustStatus.MaxSpeedRev;
                }
            }
            else if (ThrustThisFrame == Thrust.Forward)
            {
                Accelerate(ref acc, thrustDir, a.ThrustAcc);
                DebugThrustStatus = ThrustStatus.ThrustFwd;
            }
            else if (ThrustThisFrame == Thrust.Reverse)
            {
                Decelerate(ref acc, thrustDir, a.ThrustAcc * a.DecelerationPower);
                DebugThrustStatus = ThrustStatus.ThrustRev;
            }
            else
            {
                DebugThrustStatus = ThrustStatus.None;
                if (ThrustThisFrame == Thrust.Coast && a.Velocity < 0.25f)
                {
                    Velocity = Vector2.Zero; // magic stop
                    return Vector2.Zero;
                }
            }
            return acc;
        }


        // Get Stability Assist System thruster's acceleration
        // SAS Thrusters can be used to remove sideways drift from your Ships to stabilize them
        // Simulates navigational thrusting to remove sideways or reverse travel
        protected Vector2 GetSASThrusterAcceleration(in AccelerationState a, float sasThrustPower)
        {
            Vector2 acc = default;
            if (a.Velocity > 0.0001f && a.Travel <= 0.99f)
            {
                // remove sideways drift
                Vector2 left = a.Forward.LeftVector();
                float drift = a.VelocityDir.Dot(left);
                if (drift > 0f) // leftwards drift, decelerate LEFT (accelerate RIGHT)
                {
                    PrecisionDecelerate(ref acc, left, a.Velocity, a.ThrustAcc * sasThrustPower);
                }
                else if (drift < 0f) // rightward drift, accelerate LEFT
                {
                    PrecisionAccelerate(ref acc, left, a.Velocity, a.ThrustAcc * sasThrustPower);
                }
                // no thrust this frame and we're drifting backwards? accelerate forward!
                else if (ThrustThisFrame == Thrust.Coast && a.Travel < -0.5f)
                {
                    ThrustThisFrame = Thrust.Forward;
                }
            }
            return acc;
        }

        // performs a precise stop, depending on the direction of drift (travel)
        protected void PrecisionStop(ref Vector2 acc, in Vector2 thrustDir, float maxThrust, in AccelerationState a)
        {
            if (a.Travel > 0.2f) // we are traveling forward, decelerate normally
            {
                PrecisionDecelerate(ref acc, thrustDir, maxThrust, a.ThrustAcc * a.DecelerationPower);
            }
            else if (a.Travel < -0.2f) // we are traveling reverse, accelerate to slow down
            {
                PrecisionAccelerate(ref acc, thrustDir, maxThrust, a.ThrustAcc);
            }
            else // we are stopped?
            {
                ThrustThisFrame = Thrust.Coast; // turn off engine VFX
            }
            DebugThrustStatus = ThrustStatus.AllStop;
        }

    }
}
