using System;
using System.IO;
using System.Runtime.CompilerServices;
using Microsoft.Xna.Framework;
using Ship_Game.AI;

namespace Ship_Game.Ships
{
    public enum Thrust
    {
        Reverse,
        Coast,
        Forward,
        AllStop
    }

    // This is purely for DEBUGGING
    public enum ThrustStatus
    {
        None,
        AllStop,
        MaxSpeed,
        ThrustFwd,
        ThrustRev,
    }

    public partial class Ship
    {
        public float MaxFTLSpeed;
        public float MaxSTLSpeed;
        
        // reset at the end of each update
        public float SpeedLimit { get; private set; }
        public float VelocityMaximum; // maximum velocity magnitude
        public float Thrust;
        public float TurnThrust;
        public float RotationRadiansPerSecond;
        public ShipEngines ShipEngines;

        // Velocity magnitude (scalar), always absolute
        public float CurrentVelocity => Velocity.Length();

        // The desired thrust Mode during this frame
        // We don't apply velocity directly to avoid double-acceleration bugs
        // and to ensure coordinate integration is done properly
        public Thrust ThrustThisFrame;
        public ThrustStatus DebugThrustStatus;

        // this is an important variable for hi-precision impact predictor
        public Vector2 Acceleration { get; private set; }

        // Reset every frame. Not serialized.
        // This is the current thrust vector direction in radians (rads for easier vector manipulation)
        float ThrustVector;

        // Reset every frame. Not serialized.
        // Currently applied external force. All of the external force is converted into acceleration
        Vector2 AppliedExternalForce;

        const float DecelThrustPower = 0.5f; // Reverse thrusters work at 50% total engine thrust
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
            VelocityMaximum = Stats.GetVelocityMax(Thrust, Mass);
            SetSpeedLimit(VelocityMaximum); // This is overwritten at the end of Update
            RotationRadiansPerSecond = Stats.GetTurnRadsPerSec(TurnThrust, Mass, Level);
        }

        public void SetSpeedLimit(float value)
        {
            SpeedLimit = value;
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
            if (IsInFriendlyProjectorRange && loyalty.data.Traits.InBordersSpeedBonus > 0)
                FTLModifier += loyalty.data.Traits.InBordersSpeedBonus;
            FTLModifier *= projectorBonus;

            MaxFTLSpeed = Stats.GetFTLSpeed(WarpThrust, Mass, loyalty) * FTLModifier * WarpPercent;
        }

        void SetMaxSTLSpeed()
        {
            MaxSTLSpeed = Stats.GetSTLSpeed(Thrust, Mass, loyalty);
        }

        public void RotateToFacing(FixedSimTime timeStep, float angleDiff, float rotationDir)
        {
            float rotAmount = rotationDir * timeStep.FixedTime * RotationRadiansPerSecond;
            if (Math.Abs(rotAmount) > angleDiff)
            {
                rotAmount = rotAmount <= 0f ? -angleDiff : angleDiff;
                IsTurning = true;
            }
            else
            {
                IsTurning = false;
            }

            if (rotAmount > 0f) // Y-bank:
            {
                if (yRotation > -MaxBank)
                    yRotation -= GetYBankAmount(timeStep);
            }
            else if (rotAmount < 0f)
            {
                if (yRotation <  MaxBank)
                    yRotation += GetYBankAmount(timeStep);
            }

            Rotation += rotAmount;
            Rotation = Rotation.AsNormalizedRadians();
        }

        public void RestoreYBankRotation(FixedSimTime timeStep)
        {
            if (yRotation > 0f)
            {
                yRotation -= GetYBankAmount(timeStep);
                if (yRotation < 0f)
                    yRotation = 0f;
            }
            else if (yRotation < 0f)
            {
                yRotation += GetYBankAmount(timeStep);
                if (yRotation > 0f)
                    yRotation = 0f;
            }
            if (yRotation.AlmostZero()) IsTurning = false;
        }

        public float GetMinDecelerationDistance(float velocity)
        {
            // general formula for stopping distance:
            // https://www.johannes-strommer.com/diverses/pages-in-english/stopping-distance-acceleration-speed/#formel
            // s = v^2 / 2a
            float acc = GetThrustAcceleration() * DecelThrustPower;
            float distance = (velocity*velocity) / (2*acc);
            return distance;
        }

        public void SubLightAccelerate(float speedLimit = 0f, Thrust direction = Ships.Thrust.Forward)
        {
            if (engineState == MoveState.Warp)
                return; // Warp speed is updated in UpdateEnginesAndVelocity
            ApplyThrust(speedLimit, direction);
        }

        public void ApplyThrust(float speedLimit, Thrust direction)
        {
            SetSpeedLimit(speedLimit);
            ThrustThisFrame = direction;
        }

        public void AllStop()
        {
            ThrustThisFrame = Ships.Thrust.AllStop;
        }

        // NOTE: do not call outside of unit tests or Ship.Update !
        public void UpdateVelocityAndPosition(FixedSimTime timeStep)
        {
            Vector2 newAcc = GetNewAccelerationForThisFrame();
            if (newAcc.AlmostZero())
                newAcc = default;

            IntegratePosVelocityVerlet(timeStep.FixedTime, newAcc);
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

        // apply thrust limit, so we don't cause oscillating SAS thrust
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        static void PrecisionAccelerate(ref Vector2 acc, in Vector2 dir,
                                        float maxThrust, float desiredThrust)
        {
            float precisionThrust = Math.Min(maxThrust*2f, desiredThrust);
            acc.X += dir.X * precisionThrust; // NOTE: intentional manual inlining
            acc.Y += dir.Y * precisionThrust;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        static void PrecisionDecelerate(ref Vector2 acc, in Vector2 dir,
                                        float maxThrust, float desiredThrust)
        {
            float precisionThrust = Math.Min(maxThrust*2f, desiredThrust);
            acc.X -= dir.X * precisionThrust; // NOTE: intentional manual inlining
            acc.Y -= dir.Y * precisionThrust;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        static void Accelerate(ref Vector2 acc, in Vector2 dir, float thrust)
        {
            acc.X += dir.X * thrust; // NOTE: intentional manual inlining
            acc.Y += dir.Y * thrust;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        static void Decelerate(ref Vector2 acc, in Vector2 dir, float thrust)
        {
            acc.X -= dir.X * thrust; // NOTE: intentional manual inlining
            acc.Y -= dir.Y * thrust;
        }

        void PrecisionStop(ref Vector2 acc, in Vector2 dir, float travel, float maxThrust, float thrust)
        {
            if (travel > 0.2f) // we are traveling forward, decelerate normally
            {
                PrecisionDecelerate(ref acc, dir, maxThrust, thrust * DecelThrustPower);
            }
            else if (travel < -0.2f) // we are traveling reverse, accelerate to slow down
            {
                PrecisionAccelerate(ref acc, dir, maxThrust, thrust);
            }
            else
            {
                ThrustThisFrame = Ships.Thrust.Coast; // turn off engine VFX
            }
            DebugThrustStatus = ThrustStatus.AllStop;
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

            float thrustAcc = GetThrustAcceleration();
            Vector2 acc = default;

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
                if (drift > 0f) // leftwards drift, decelerate LEFT (accelerate RIGHT)
                {
                    PrecisionDecelerate(ref acc, left, velocity, thrustAcc * SASThrusterPower);
                }
                else if (drift < 0f) // rightward drift, accelerate LEFT
                {
                    PrecisionAccelerate(ref acc, left, velocity, thrustAcc * SASThrusterPower);
                }
                else if (ThrustThisFrame == Ships.Thrust.Coast && // no thrust this frame?
                         travel < -0.5f && engineState != MoveState.Warp)
                {
                    // we are drifting reverse, accelerate forward!
                    ThrustThisFrame = Ships.Thrust.Forward;
                }
            }

            if (AppliedExternalForce.X != 0f || AppliedExternalForce.Y != 0f)
            {
                acc.X += AppliedExternalForce.X / Mass;
                acc.Y += AppliedExternalForce.Y / Mass;
                AppliedExternalForce = Vector2.Zero;
            }

            // Get the real speed limit
            float speedLimit = SpeedLimit > 0f
                             ? Math.Min(SpeedLimit, VelocityMaximum)
                             : VelocityMaximum;

            // in Warp, we cannot go slower than LightSpeed
            if (engineState == MoveState.Warp)
            {
                speedLimit = Math.Max(speedLimit, LightSpeedConstant);
                ThrustThisFrame = Ships.Thrust.Forward; // in Warp, we can only thrust forward
            }

            // get the current thrust dir and reset the vector for now
            // (todo: maybe add a delay to thrust vector direction change?)
            Vector2 thrustDir = (Rotation + ThrustVector).RadiansToDirection();
            ThrustVector = 0f;

            // Main ACCELERATE / DECELERATE
            if (ThrustThisFrame == Ships.Thrust.AllStop)
            {
                PrecisionStop(ref acc, thrustDir, travel, velocity, thrustAcc);
            }
            else if (velocity >= speedLimit) // we are at the speed limit already
            {
                float overLimit = (velocity - speedLimit);
                // in order to have direction control at max velocity limit
                // we spend half thrust to slow down and half thrust to speed up in wanted dir
                if (ThrustThisFrame == Ships.Thrust.Forward && overLimit < thrustAcc*0.01f)
                {
                    Decelerate(ref acc, velocityDir, thrustAcc);
                    Accelerate(ref acc, thrustDir,   thrustAcc);
                    DebugThrustStatus = ThrustStatus.MaxSpeed;
                }
                else
                {
                    PrecisionStop(ref acc, thrustDir, travel, velocity, thrustAcc);
                }
            }
            else if (ThrustThisFrame == Ships.Thrust.Forward)
            {
                Accelerate(ref acc, thrustDir, thrustAcc);
                DebugThrustStatus = ThrustStatus.ThrustFwd;
            }
            else if (ThrustThisFrame == Ships.Thrust.Reverse)
            {
                Decelerate(ref acc, thrustDir, thrustAcc * DecelThrustPower);
                DebugThrustStatus = ThrustStatus.ThrustRev;
            }
            else
            {
                DebugThrustStatus = ThrustStatus.None;
                if (ThrustThisFrame == Ships.Thrust.Coast && velocity < 0.25f)
                {
                    Velocity = Vector2.Zero; // magic stop
                    return Vector2.Zero;
                }
            }

            return acc;
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
            ThrustThisFrame = Ships.Thrust.Coast;
            if (!Carrier.RecallingShipsBeforeWarp)
                SetSpeedLimit(VelocityMaximum);

            if (AI.State == AIState.FormationWarp)
                SetSpeedLimit(AI.FormationWarpSpeed(VelocityMaximum));
        }

        // called from Ship.Update
        void UpdateEnginesAndVelocity(FixedSimTime timeStep)
        {
            if (engineState == MoveState.Sublight && CurrentVelocity > MaxSTLSpeed)
            {
                // feature: exit from hyperspace at ridiculous speeds
                Velocity = Velocity.Normalized() * Math.Min(MaxSTLSpeed, MaxSubLightSpeed);
            }

            UpdateHyperspaceInhibited(timeStep);
            SetMaxFTLSpeed();

            switch (engineState)
            {
                case MoveState.Sublight: VelocityMaximum = MaxSTLSpeed; break;
                case MoveState.Warp:     VelocityMaximum = MaxFTLSpeed; break;
            }

            if (!IsTurning)
            {
                RestoreYBankRotation(timeStep);
            }

            if (engineState == MoveState.Warp && Velocity.Length() < SpeedLimit)
            {
                // enable full thrust, but don't touch the SpeedLimit
                // so that FormationWarp can work correctly
                ThrustThisFrame = Ships.Thrust.Forward;
            }

            UpdateVelocityAndPosition(timeStep);

            if (IsSpooling && !Inhibited && MaxFTLSpeed >= LightSpeedConstant)
                UpdateWarpSpooling(timeStep);
        }

        public bool TryGetScoutFleeVector(out Vector2 escapePos) => GetEscapeVector(out escapePos, 100000, true);
        public bool TryGetEscapeVector(out Vector2 escapePos) => GetEscapeVector(out escapePos, 20000, false);

        public bool GetEscapeVector(out Vector2 escapePos, float desiredDistance, bool ignoreNonCombat)
        {
            escapePos = Position + Direction.Normalized() * desiredDistance; // default vector - straight through

            if (!InCombat && !ignoreNonCombat) // No need for escape vector if not in combat - turn around
                return false;

            if (IsInFriendlyProjectorRange || !Empire.Universe.GravityWells)
                return true; // Wont be inhibited - straight through

            switch (System)
            {
                case null when Inhibited: return false; // Ship Inhibitor - turn around
                case null:                return true;  // Outer space - straight through
            }

            Array<Planet> potentialWells = new Array<Planet>();
            foreach (Planet planet in System.PlanetList)
            {
                if (Position.InRadius(planet.Center, 20000 + planet.GravityWellRadius))
                    potentialWells.Add(planet);
            }

            if (potentialWells.Count == 0)
                return true; // No wells nearby

            int leastWells = int.MaxValue;
            int leftOrRight = RandomMath.RollDie(2) == 1 ? 1 : -1;
            for (int i = 0; i <= 11; i++ )
            {
                float rotation = Rotation + i * 0.52356f*leftOrRight; // 30 degrees
                Vector2 pathToCheck = rotation.RadiansToDirection();
                if (!WellsInPath(potentialWells, pathToCheck, 2000, out int wellHits))
                {
                    escapePos = Position + pathToCheck * desiredDistance;
                    break; // Found direction with no wells
                }

                if (wellHits < leastWells)
                {
                    leastWells = wellHits;
                    escapePos = Position +  pathToCheck * desiredDistance; // try to get the path with least well hits
                }
            }

            return true;
        }

        bool WellsInPath(Array<Planet> wells, Vector2 path, int pathResolution, out int wellHits)
        {
            wellHits = 0;
            foreach (Planet planet in wells)
            {
                for (int i = 1; i <= 10; i++)
                {
                    Vector2 posToCheck = Position + path * i * pathResolution;
                    if (posToCheck.InRadius(planet.Center, planet.GravityWellRadius))
                        wellHits += 1;
                }
            }

            return wellHits > 0;
        }
    }
}
