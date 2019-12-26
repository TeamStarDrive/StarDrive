using System;
using Microsoft.Xna.Framework;
using Ship_Game.AI;

namespace Ship_Game.Ships
{
    public partial class Ship
    {
        public float MaxFTLSpeed;
        public float MaxSTLSpeed;
        
        public float SpeedLimit; // current speed limit; reset every update
        // Velocity magnitude (scalar), always absolute
        public float CurrentVelocity => Velocity.Length();
        public float VelocityMaximum; // maximum velocity magnitude
        public float Thrust;
        public float TurnThrust;
        public float RotationRadiansPerSecond;

        const float DecelerationRate = 0.5f; // Reverse thrusters work at 50% total engine thrust
        const float SASThrusterPower = 0.25f; // Stability Assist thrusters work at 25% total engine thrust

        void UpdateMaxVelocity()
        {
            VelocityMaximum = Thrust / Mass;
            RotationRadiansPerSecond = TurnThrust / Mass / 700f;
            SpeedLimit = VelocityMaximum; // This is overwritten inside Update
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
            isTurning = true;
            float rotAmount = rotationDir * elapsedTime * RotationRadiansPerSecond;
            if (Math.Abs(rotAmount) > angleDiff)
            {
                rotAmount = rotAmount <= 0f ? -angleDiff : angleDiff;
            }

            if (rotAmount > 0f) // Y-bank:
            {
                if (yRotation > -MaxBank)
                    yRotation -= yBankAmount;
            }
            else if (rotAmount < 0f)
            {
                if (yRotation <  MaxBank)
                    yRotation += yBankAmount;
            }

            Rotation += rotAmount;
            Rotation = Rotation.AsNormalizedRadians();
        }

        public void RestoreYBankRotation()
        {
            if (yRotation > 0f)
            {
                yRotation -= yBankAmount;
                if (yRotation < 0f)
                    yRotation = 0f;
            }
            else if (yRotation < 0f)
            {
                yRotation += yBankAmount;
                if (yRotation > 0f)
                    yRotation = 0f;
            }
        }

        float AdjustedSpeedLimit(float speedLimit = 0f)
        {
            float limit;
            // use [Speed], but still cap it to velocityMaximum
            if (speedLimit <= 0f || speedLimit > SpeedLimit)
                limit = Math.Min(SpeedLimit, VelocityMaximum);
            else
                limit = Math.Min(speedLimit, VelocityMaximum);

            // in Warp, we cannot go slower than LightSpeed
            if (engineState == MoveState.Warp)
                limit = Math.Max(limit, LightSpeedConstant);

            return limit;
        }

        float GetThrustAcceleration()
        {
            if (engineState == MoveState.Warp)
            {
                const float accelerationTime = 2f;
                return (MaxFTLSpeed / accelerationTime);
            }
            return (Thrust / Mass) * 0.5f;
        }

        public float GetMinDecelerationDistance(float velocity)
        {
            float a = GetThrustAcceleration() * DecelerationRate;

            // general formula for stopping distance:
            // https://www.johannes-strommer.com/diverses/pages-in-english/stopping-distance-acceleration-speed/#formel
            // s = v^2 / 2a
            float distance = (velocity*velocity) / (2*a);
            return distance;
        }

        public void SubLightAccelerate(float elapsedTime, float speedLimit = 0f, float direction = +1f)
        {
            if (engineState == MoveState.Warp)
                return; // Warp speed is updated in UpdateEnginesAndVelocity
            ApplyThrust(elapsedTime, speedLimit, direction);
        }

        void WarpAccelerate(float elapsedTime)
        {
            ApplyThrust(elapsedTime, MaxFTLSpeed, +1f);
        }


        void ApplyThrust(float elapsedTime, float speedLimit, float thrustDirection)
        {
            float actualSpeedLimit = AdjustedSpeedLimit(speedLimit);
            float acceleration = elapsedTime * GetThrustAcceleration();
            isThrusting = true;

            // we are already over max vel? forget about accelerating, slow down!
            // this if check is needed to retain high velocities and visually decelerate
            if (Velocity.Length() > actualSpeedLimit)
            {
                // we don't know which direction we were thrusting before, so simply negate the velocity vector
                Velocity -= Velocity.Normalized() * acceleration * DecelerationRate;
            }
            else
            {
                if (thrustDirection >= 0f) // accelerating
                {
                    Velocity += Direction * acceleration;
                }
                else // decelerating
                {
                    Velocity -= Direction * acceleration * DecelerationRate; 
                }
                
                // cap the speed immediately so we never go past the speed limit
                if (Velocity.Length() > actualSpeedLimit)
                {
                    Velocity = Velocity.Normalized() * actualSpeedLimit;
                }
            }
        }

        public void Decelerate(float elapsedTime)
        {
            float acceleration = elapsedTime * GetThrustAcceleration();
            // we don't know which direction we were thrusting before, so simply negate the velocity vector
            Velocity -= Velocity.Normalized() * acceleration * DecelerationRate;
        }

        // simulates navigational thrusting to remove sideways or reverse travel
        void RemoveVelocityDriftAndApplyVelocityLimit(float elapsedTime)
        {
            // compare ship velocity vector against where it is pointing
            // if +1 then ship is going forward as intended
            // if  0 then ship is drifting sideways
            // if -1 then ship is drifting reverse
            float velocity = Velocity.Length();
            if (velocity.AlmostZero())
            {
                Velocity = Vector2.Zero;
                return;
            }

            float actualSpeedLimit = AdjustedSpeedLimit();
            float acceleration = elapsedTime * GetThrustAcceleration();

            Vector2 forward = Direction;
            Vector2 velocityDir = Velocity.Normalized();
            float travel = velocityDir.Dot(forward);
            if (travel <= 0.99f)
            {

                // remove sideways drift
                Vector2 left = forward.LeftVector();
                float drift = velocityDir.Dot(left);
                if (drift > 0f) // leftwards drift
                {
                    Velocity -= left * acceleration * SASThrusterPower;
                }
                else if (drift < 0f) // rightward drift
                {
                    Velocity += left * acceleration * SASThrusterPower;
                }
                else if (travel < -0.5f && engineState != MoveState.Warp)
                {
                    // we are drifting reverse, accelerate forward!
                    isThrusting = true;
                    Velocity += forward * acceleration * SASThrusterPower;
                }
            }

            // we are already over max vel? forget about accelerating, slow down!
            if (Velocity.Length() > actualSpeedLimit)
            {
                // we don't know which direction we were thrusting before, so simply negate the velocity vector
                Velocity -= Velocity.Normalized() * acceleration * DecelerationRate;
            }
        }

        // called from Ship.Update
        void UpdateEnginesAndVelocity(float elapsedTime)
        {
            UpdateHyperspaceInhibited(elapsedTime);
            SetMaxFTLSpeed();

            switch (engineState)
            {
                case MoveState.Sublight: VelocityMaximum = MaxSTLSpeed; break;
                case MoveState.Warp:     VelocityMaximum = MaxFTLSpeed; break;
            }

            SpeedLimit = AI.State == AIState.FormationWarp
                ? AI.FormationWarpSpeed(VelocityMaximum) : VelocityMaximum;

            RotationRadiansPerSecond = TurnThrust / Mass / 700f;
            RotationRadiansPerSecond += RotationRadiansPerSecond * Level * 0.05f;
            yBankAmount = GetyBankAmount(RotationRadiansPerSecond * elapsedTime);

            if (engineState == MoveState.Warp)
            {
                WarpAccelerate(elapsedTime);
            }

            if ((Thrust <= 0f || Mass <= 0f) && !IsTethered)
            {
                EnginesKnockedOut = true;
                VelocityMaximum = Velocity.Length();
                Velocity -= Velocity * (elapsedTime * 0.1f);
                if (engineState == MoveState.Warp)
                    HyperspaceReturn();
            }
            else
            {
                EnginesKnockedOut = false;
            }

            RemoveVelocityDriftAndApplyVelocityLimit(elapsedTime);

            Acceleration = PreviousVelocity.Acceleration(Velocity, elapsedTime);
            PreviousVelocity = Velocity;

            if (IsSpooling && !Inhibited && MaxFTLSpeed >= LightSpeedConstant)
                UpdateWarpSpooling(elapsedTime);
        }

    }
}
