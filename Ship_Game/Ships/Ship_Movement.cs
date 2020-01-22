﻿using System;
using Microsoft.Xna.Framework;
using Ship_Game.AI;

namespace Ship_Game.Ships
{
    public partial class Ship
    {
        public float MaxFTLSpeed;
        public float MaxSTLSpeed;
        
        // reset at the end of each update
        public float SpeedLimit;
        public float VelocityMaximum; // maximum velocity magnitude
        public float Thrust;
        public float TurnThrust;
        public float RotationRadiansPerSecond;

        // Velocity magnitude (scalar), always absolute
        public float CurrentVelocity => Velocity.Length();

        // we need to store the applied thrust for correct
        // VelocityVerlet integration
        // > 0: forward/acceleration
        // < 0: reverse/deceleration
        int ThrustThisFrame;

        // this is an important variable for hi-precision impact predictor
        public Vector2 Acceleration { get; private set; }

        // Reset every frame. Not serialized.
        // This is the current thrust vector direction in radians (rads for easier vector manipulation)
        float ThrustVector;

        // Reset every frame. Not serialized.
        // Currently applied external force. All of the external force is converted into acceleration
        Vector2 AppliedExternalForce;

        const float DecelerationRate = 0.5f; // Reverse thrusters work at 50% total engine thrust
        const float SASThrusterPower = 0.25f; // Stability Assist thrusters work at 25% total engine thrust

        // This applies (accumulates) a Force vector for the duration of this frame.
        // If you wish to transfer force over a larger time period, you must call this over several frames.
        // This is because our velocity/position integration recalculates acceleration every frame
        public void ApplyForce(Vector2 force)
        {
            AppliedExternalForce += force;
        }

        // This sets the direction of forward thrust.
        // 0: ship accelerates forward
        // +PI/4: ship accelerates diagonally RIGHT
        // -PI/4: ship accelerates diagonally LEFT
        // @note Thrust direction is always clamped to [-PI/4; +PI/4]
        public void SetThrustDirection(float thrustDirectionRadians)
        {
            const float quarterPi = RadMath.PI / 4;
            ThrustVector = thrustDirectionRadians.Clamped(-quarterPi, quarterPi);
        }

        void UpdateMaxVelocity()
        {
            VelocityMaximum = Thrust / Mass;
            RotationRadiansPerSecond = TurnThrust / Mass / 700f;
            RotationRadiansPerSecond += RotationRadiansPerSecond * Level * 0.05f;
            SpeedLimit = VelocityMaximum; // This is overwritten at the end of Update
        }

        void SetMaxFTLSpeed()
        {
            float projectorBonus = 1f;

            // Change FTL modifier for ship based on solar system
            if (System != null)
            {
                if (IsInFriendlyProjectorRange)
                    projectorBonus = Empire.Universe.FTLModifier;
                else if (!Empire.Universe.FTLInNuetralSystems || IsInHostileProjectorRange)
                    projectorBonus = Empire.Universe.EnemyFTLModifier;
            }

            FTLModifier = 1f;
            if (inborders && loyalty.data.Traits.InBordersSpeedBonus > 0)
                FTLModifier += loyalty.data.Traits.InBordersSpeedBonus;
            FTLModifier *= projectorBonus;
            FTLModifier *= loyalty.data.FTLModifier;

            MaxFTLSpeed = (WarpThrust / Mass) * FTLModifier;
        }

        void SetMaxSTLSpeed()
        {
            float thrustWeightRatio = Thrust / Mass;
            float speed = thrustWeightRatio * loyalty.data.SubLightModifier;
            MaxSTLSpeed = Math.Min(speed, LightSpeedConstant);
        }

        public void RotateToFacing(float elapsedTime, float angleDiff, float rotationDir)
        {
            IsTurning = true;
            float rotAmount = rotationDir * elapsedTime * RotationRadiansPerSecond;
            if (Math.Abs(rotAmount) > angleDiff)
            {
                rotAmount = rotAmount <= 0f ? -angleDiff : angleDiff;
            }

            if (rotAmount > 0f) // Y-bank:
            {
                if (yRotation > -MaxBank)
                    yRotation -= GetYBankAmount(elapsedTime);
            }
            else if (rotAmount < 0f)
            {
                if (yRotation <  MaxBank)
                    yRotation += GetYBankAmount(elapsedTime);
            }

            Rotation += rotAmount;
            Rotation = Rotation.AsNormalizedRadians();
        }

        public void RestoreYBankRotation(float elapsedTime)
        {
            if (yRotation > 0f)
            {
                yRotation -= GetYBankAmount(elapsedTime);
                if (yRotation < 0f)
                    yRotation = 0f;
            }
            else if (yRotation < 0f)
            {
                yRotation += GetYBankAmount(elapsedTime);
                if (yRotation > 0f)
                    yRotation = 0f;
            }
        }

        public float GetMinDecelerationDistance(float velocity)
        {
            // general formula for stopping distance:
            // https://www.johannes-strommer.com/diverses/pages-in-english/stopping-distance-acceleration-speed/#formel
            // s = v^2 / 2a
            float acc = GetThrustAcceleration() * DecelerationRate;
            float distance = (velocity*velocity) / (2*acc);
            return distance;
        }

        public void SubLightAccelerate(float speedLimit = 0f, int direction = +1)
        {
            if (engineState == MoveState.Warp)
                return; // Warp speed is updated in UpdateEnginesAndVelocity
            ApplyThrust(speedLimit, direction);
        }

        void ApplyThrust(float speedLimit, int direction)
        {
            SpeedLimit = speedLimit;
            ThrustThisFrame = direction;
        }

        public void Decelerate()
        {
            ThrustThisFrame = -1;
        }

        void UpdateVelocityAndPosition(float elapsedTime)
        {
            Vector2 newAcc = GetNewAccelerationForThisFrame();
            if (newAcc.AlmostZero())
                newAcc = default;

            IntegratePosVelocityVerlet(elapsedTime, newAcc);
        }

        // Velocity Verlet integration method
        // significantly more stable and accurate than ExplicitEuler or SemiImplicitEuler
        // 1. Get the new acceleration for this frame
        // 2. Update the current position
        //     -- using previous frame's acceleration
        //     -- using previous frames' velocity
        // 3. Update the velocity using old and new acceleration
        // @param dt Delta Time for the Simulation
        void IntegratePosVelocityVerlet(float dt, Vector2 newAcc)
        {
            // integrate position using Velocity Verlet method:
            // x' = x + v*dt + (a*dt^2)/2
            Vector2 oldAcc = Acceleration;
            float dt2 = dt*dt*0.5f;
            Position.X += (Velocity.X*dt + oldAcc.X*dt2);
            Position.Y += (Velocity.Y*dt + oldAcc.Y*dt2);
            Center = Position;

            // integrate velocity using Velocity Verlet method:
            // v' = v + (a0+a1)*0.5*dt
            Velocity.X += (oldAcc.X+newAcc.X)*0.5f*dt;
            Velocity.Y += (oldAcc.Y+newAcc.Y)*0.5f*dt;

            Acceleration = newAcc;
        }

        // @note This function is a bit longer than normal because it's very math
        //       heavy and we want to avoid too many function call overheads,
        //       hoping for better CLR optimization
        Vector2 GetNewAccelerationForThisFrame()
        {
            if (TetheredTo == null && (Thrust <= 0f || Mass <= 0f))
            {
                EnginesKnockedOut = true;
                if (engineState == MoveState.Warp)
                    HyperspaceReturn();
                // no magic stop or anything, we just stop acceleration
                return default;
            }

            EnginesKnockedOut = false;

            float thrustAcceleration = GetThrustAcceleration();
            Vector2 newAcc = default;

            // inline velocity magnitude and dir (speed optimization)
            float vx = Velocity.X, vy = Velocity.Y;
            float velocity = (float)Math.Sqrt(vx*vx + vy*vy);
            Vector2 velocityDir = velocity > 0.0001f
                                ? new Vector2(vx/velocity, vy/velocity)
                                : default;

            // simulates navigational thrusting to remove sideways or reverse travel
            // compare ship velocity vector against where it is pointing
            // if +1 then ship is going forward as intended
            // if  0 then ship is drifting sideways
            // if -1 then ship is drifting reverse
            Vector2 shipForward = Rotation.RadiansToDirection();
            float travel = velocityDir.Dot(shipForward);
            if (velocity > 0.0001f && travel <= 0.99f)
            {
                // remove sideways drift
                Vector2 left = shipForward.LeftVector();
                float drift = velocityDir.Dot(left);
                if (drift > 0f) // leftwards drift
                {
                    newAcc -= left * thrustAcceleration * SASThrusterPower;
                }
                else if (drift < 0f) // rightward drift
                {
                    newAcc += left * thrustAcceleration * SASThrusterPower;
                }
                else if (ThrustThisFrame == 0 && // no thrust this frame?
                         travel < -0.5f && engineState != MoveState.Warp)
                {
                    // we are drifting reverse, accelerate forward!
                    ApplyThrust(0f, direction: +1);
                }
            }

            if (AppliedExternalForce != Vector2.Zero)
            {
                newAcc += (AppliedExternalForce / Mass);
                AppliedExternalForce = Vector2.Zero;
            }

            // Get the real speed limit
            float speedLimit = SpeedLimit > 0f
                             ? Math.Min(SpeedLimit, VelocityMaximum)
                             : VelocityMaximum;

            // in Warp, we cannot go slower than LightSpeed
            if (engineState == MoveState.Warp)
                speedLimit = Math.Max(speedLimit, LightSpeedConstant);

            // get the current thrust dir and reset the vector for now
            // (todo: maybe add a delay to thrust vector direction change?)
            Vector2 thrustDirection = (Rotation + ThrustVector).RadiansToDirection();
            ThrustVector = 0f;

            // Main ACCELERATE / DECELERATE
            // we are pretty much at the speed limit already, don't do anything
            if (velocity.AlmostEqual(speedLimit))
            {
                ThrustThisFrame = 0; // turn off engine VFX
            }
            // we are already over max vel? forget about accelerating, slow down!
            else if (velocity > speedLimit)
            {
                if (travel > 0.2f) // we are traveling forward, decelerate normally
                {
                    newAcc -= thrustDirection * thrustAcceleration * DecelerationRate;
                }
                else if (travel < -0.2f) // we are traveling reverse, accelerate to slow down
                {
                    newAcc += thrustDirection * thrustAcceleration;
                }
                // else: we aren't facing drift direction, so thrusting is pointless
            }
            else if (ThrustThisFrame > 0)
            {
                newAcc += thrustDirection * thrustAcceleration;
            }
            else if (ThrustThisFrame < 0)
            {
                newAcc -= thrustDirection * thrustAcceleration * DecelerationRate;
            }
            return newAcc;
        }

        float GetThrustAcceleration()
        {
            if (engineState == MoveState.Warp)
            {
                const float accelerationTime = 2f;
                return (MaxFTLSpeed / accelerationTime);
            }
            return (Thrust / Mass);
        }

        // these variables are only valid once per frame
        // and must be reset after every update
        void ResetFrameThrustState()
        {
            ThrustThisFrame = 0;
            SpeedLimit = VelocityMaximum;
            if (AI.State == AIState.FormationWarp)
                SpeedLimit = AI.FormationWarpSpeed(VelocityMaximum);
        }

        // called from Ship.Update
        void UpdateEnginesAndVelocity(float elapsedTime)
        {
            UpdateHyperspaceInhibited(elapsedTime);
            SetMaxFTLSpeed();

            switch (engineState)
            {
                case MoveState.Sublight: VelocityMaximum = MaxSTLSpeed; break;
                case MoveState.Warp: VelocityMaximum = MaxFTLSpeed; break;
            }

            if (!IsTurning)
            {
                RestoreYBankRotation(elapsedTime);
            }
            IsTurning = false;

            if ((engineState == MoveState.Warp || ThrustThisFrame > 0) && Velocity.Length() < SpeedLimit)
            {
                // enable full thrust, but don't touch the SpeedLimit
                // so that FormationWarp can work correctly
                ThrustThisFrame = +1;
            }

            UpdateVelocityAndPosition(elapsedTime);

            if (IsSpooling && !Inhibited && MaxFTLSpeed >= LightSpeedConstant)
                UpdateWarpSpooling(elapsedTime);
        }
    }
}
