using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using SDGraphics;
using Ship_Game.Ships;
using Vector2 = SDGraphics.Vector2;

namespace Ship_Game.Gameplay
{
    // A type of GameObject which can be manipulated using thrust forces
    // such as Ships or Projectiles
    //
    // Additional external forces can also be applied
    //
    public class PhysicsObject : GameObject
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

        public PhysicsObject(int id, GameObjectType type) : base(id, type)
        {
        }

        protected readonly struct AccelerationState
        {
            public readonly float ThrustAcc; // maximum thrust acceleration a = m/s^2
            public readonly float Velocity;
            public readonly float MaxVelocity;
            public readonly Vector2 VelocityDir;
            public readonly Vector2 Forward; // object forward dir

            // compares velocity vector against where it is pointing
            // if +1 then obj is going forward as it points forward
            // if  0 then obj is drifting sideways
            // if -1 then obj is drifting reverse
            // velocityDir.Dot(objectForwardDir)
            public readonly float Travel;
            public readonly float DecelerationPower; // multiplier [0.0; 1.0]

            public AccelerationState(in Vector2 velocity, float maxVelocity, float rotation, float maxThrustAcc, float decelerationPower)
            {
                ThrustAcc = maxThrustAcc;

                // inline velocity magnitude and dir (speed optimization)
                float vx = velocity.X, vy = velocity.Y;
                Velocity = (float)Math.Sqrt(vx*vx + vy*vy);
                VelocityDir = Velocity > 0.0001f ? new Vector2(vx/Velocity, vy/Velocity) : default;

                // inline RadiansToDirection (speed optimization)
                float s = (float)Math.Sin(rotation);
                float c = (float)Math.Cos(rotation);
                Forward = new(s, -c);
                //Forward = rotation.RadiansToDirection();
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

        // This applies (accumulates) a Force vector for the duration of this frame.
        // If you wish to transfer force over a larger time period, you must call this over several frames.
        // This is because our velocity/position integration recalculates acceleration every frame
        public void ApplyForce(Vector2 force)
        {
            AppliedExternalForce += force;
        }

        protected bool GetThrustAcceleration(ref Vector2 acc, in AccelerationState a)
        {
            // combining object Rotation(rads) and engine ThrustVector(rads)
            // return the engine nozzle's thrusting direction
            // if ThrustVector=0, this is equal to ship Forward Direction
            float rotation = Rotation + ThrustVector;
            Vector2 thrustDir;
            thrustDir.X = (float)Math.Sin(rotation); // inline RadiansToDirection (speed optimization)
            thrustDir.Y = -(float)Math.Cos(rotation);

            // If ship is traveling backwards or sideways, limit max velocity
            float maxVelocity = a.Travel < -0.15f ? a.MaxVelocity * 0.75f : a.MaxVelocity;

            if (ThrustThisFrame == Thrust.AllStop)
            {
                PrecisionStop(ref acc, thrustDir, a.Velocity, a);
                return true;
            }
            else if (a.Velocity >= maxVelocity) // we are at the speed limit already
            {
                // in order to have direction control at max velocity limit
                // we spend use thrust to slow down in velocity dir and thrust to speed up in wanted dir
                // if we are already facing wanted direction, then this is a no-op
                if (ThrustThisFrame == Thrust.Forward && a.Travel > 0.2f/*we are going forward*/)
                {
                    // if we're over speed limit, then accelerate a bit less than we decelerate
                    float overLimit = (a.Velocity - maxVelocity);
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
                return true;
            }
            else if (ThrustThisFrame == Thrust.Forward)
            {
                Accelerate(ref acc, thrustDir, a.ThrustAcc);
                DebugThrustStatus = ThrustStatus.ThrustFwd;
                return true;
            }
            else if (ThrustThisFrame == Thrust.Reverse)
            {
                Decelerate(ref acc, thrustDir, a.ThrustAcc * a.DecelerationPower);
                DebugThrustStatus = ThrustStatus.ThrustRev;
                return true;
            }
            else
            {
                DebugThrustStatus = ThrustStatus.None;
                if (ThrustThisFrame == Thrust.Coast && a.Velocity < 0.25f)
                {
                    Velocity = Vector2.Zero; // magic stop
                }
            }
            return false;
        }

        // Get Stability Assist System thruster's acceleration
        // SAS Thrusters can be used to remove sideways drift from your Ships to stabilize them
        // Simulates navigational thrusting to remove sideways or reverse travel
        protected bool GetSASThrusterAcceleration(ref Vector2 acc, in AccelerationState a, float sasThrustPower)
        {
            if (a.Velocity > 0.0001f && a.Travel <= 0.99f)
            {
                // remove sideways drift
                Vector2 left = a.Forward.LeftVector();
                float drift = a.VelocityDir.Dot(left);
                if (drift > 0f) // leftwards drift, decelerate LEFT (accelerate RIGHT)
                {
                    PrecisionDecelerate(ref acc, left, a.Velocity, a.ThrustAcc * sasThrustPower);
                    return true;
                }
                else if (drift < 0f) // rightward drift, accelerate LEFT
                {
                    PrecisionAccelerate(ref acc, left, a.Velocity, a.ThrustAcc * sasThrustPower);
                    return true;
                }
                // no thrust this frame and we're drifting backwards? accelerate forward!
                else if (ThrustThisFrame == Thrust.Coast && a.Travel < -0.5f)
                {
                    ThrustThisFrame = Thrust.Forward;
                }
            }
            return false;
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
