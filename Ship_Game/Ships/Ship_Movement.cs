using System;
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
        MaxSpeedRev,
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
        public float RotationRadiansPerSecond;
        public ShipEngines ShipEngines;

        const float DecelThrustPower = 0.5f; // Reverse thrusters work at 50% total engine thrust
        const float SASThrusterPower = 0.25f; // Stability Assist thrusters work at 25% total engine thrust

        void UpdateMaxVelocity() // assume Thrust or Mass just changed
        {
            VelocityMaximum = Stats.UpdateVelocityMax();
            SetSpeedLimit(VelocityMaximum); // This is overwritten at the end of Update
            RotationRadiansPerSecond = Stats.GetTurnRadsPerSec(Level);
        }

        public void SetSpeedLimit(float value)
        {
            SpeedLimit = value;
        }

        float SetMaxFTLSpeed()
        {
            float projectorBonus = 1f;

            // Change FTL modifier for ship based on solar system
            if (System != null)
            {
                if (IsInFriendlyProjectorRange)
                    projectorBonus = Universe.FTLModifier;
                else if (!Universe.FTLInNeutralSystems || IsInHostileProjectorRange)
                    projectorBonus = Universe.EnemyFTLModifier;
            }

            FTLModifier = 1f;
            if (IsInFriendlyProjectorRange && Loyalty.data.Traits.InBordersSpeedBonus > 0)
                FTLModifier += Loyalty.data.Traits.InBordersSpeedBonus;
            FTLModifier *= projectorBonus;

            float maxFTLSpeed = Stats.MaxFTLSpeed * FTLModifier * WarpPercent;
            MaxFTLSpeed = maxFTLSpeed;
            return maxFTLSpeed;
        }

        void SetMaxSTLSpeed()
        {
            MaxSTLSpeed = Stats.MaxSTLSpeed;
        }

        public void RotateToFacing(FixedSimTime timeStep, float angleDiff, float rotationDir, float minDiff)
        {
            float rotAmount = rotationDir * timeStep.FixedTime * RotationRadiansPerSecond;
            ShouldBank = IsVisibleToPlayer && angleDiff > minDiff+0.2f; // slight threshold to start restoring y rotation

            if (ShouldBank)
            {
                if (rotAmount > 0f) // Y-bank:
                {
                    if (YRotation > -MaxBank)
                        YRotation -= GetYBankAmount(timeStep);
                }
                else if (rotAmount < 0f)
                {
                    if (YRotation < MaxBank)
                        YRotation += GetYBankAmount(timeStep);
                }
            }
            Rotation += rotAmount;
            Rotation = Rotation.AsNormalizedRadians();
        }

        public void RestoreYBankRotation(FixedSimTime timeStep)
        {
            if (YRotation > 0f)
            {
                YRotation -= GetYBankAmount(timeStep);
                if (YRotation < 0f)
                    YRotation = 0f;
            }
            else if (YRotation < 0f)
            {
                YRotation += GetYBankAmount(timeStep);
                if (YRotation > 0f)
                    YRotation = 0f;
            }
        }

        public float GetMinDecelerationDistance(float velocity)
        {
            // general formula for stopping distance:
            // https://www.johannes-strommer.com/diverses/pages-in-english/stopping-distance-acceleration-speed/#formel
            // s = v^2 / 2a
            float acc = GetMaxThrustAcceleration() * DecelThrustPower;
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
            UpdateVelocityAndPosition(timeStep.FixedTime, newAcc);
        }

        Vector2 GetNewAccelerationForThisFrame()
        {
            if ((TetheredTo == null && Stats.Thrust <= 0f) || (Mass <= 0f))
            {
                EnginesKnockedOut = true;
                if (engineState == MoveState.Warp)
                    HyperspaceReturn();
                // no magic stop or anything, we just stop acceleration
                return default;
            }

            EnginesKnockedOut = false;

            ThrustAcceleration = GetMaxThrustAcceleration();

            // Get the real speed limit
            float maxVelocity = SpeedLimit > 0f
                              ? Math.Min(SpeedLimit, VelocityMaximum)
                              : VelocityMaximum;

            // in Warp, we cannot go slower than LightSpeed
            if (engineState == MoveState.Warp)
            {
                maxVelocity = Math.Max(maxVelocity, LightSpeedConstant);
                ThrustThisFrame = Thrust.Forward; // in Warp, we can only thrust forward
            }

            // combine all different acceleration sources
            var a = new AccelerationState(Velocity, maxVelocity, Rotation, ThrustAcceleration, DecelThrustPower);
            Vector2 appliedForce = GetAppliedForceAcceleration();
            Vector2 sasAcc = GetSASThrusterAcceleration(a, SASThrusterPower);
            Vector2 thrustAcc = GetThrustAcceleration(a);
            return appliedForce + sasAcc + thrustAcc;
        }

        float GetMaxThrustAcceleration()
        {
            if (engineState == MoveState.Warp)
            {
                const float accelerationTime = 2f;
                return (MaxFTLSpeed / accelerationTime);
            }
            return (Stats.Thrust / Mass);
        }

        // these variables are only valid once per frame
        // and must be reset after every update
        void ResetFrameThrustState()
        {
            ResetForcesThisFrame();

            //if (!Carrier.RecallingShipsBeforeWarp)
            //    SetSpeedLimit(VelocityMaximum);

            //if (AI.State == AIState.FormationWarp)
            //    SetSpeedLimit(AI.FormationWarpSpeed(VelocityMaximum));
        }

        // Called from Ship.Update
        // @warning PERF This is called every simulation frame for every ship in the universe
        void UpdateEnginesAndVelocity(FixedSimTime timeStep)
        {
            float maxFTLSpeed = SetMaxFTLSpeed();
            float maxSTLSpeed = MaxSTLSpeed;
            bool isWarpCapable = maxFTLSpeed > maxSTLSpeed;
            bool atWarp = engineState == MoveState.Warp;

            VelocityMaximum = atWarp ? maxFTLSpeed : maxSTLSpeed;

            if (!atWarp && Velocity.Length() > maxSTLSpeed)
            {
                // feature: exit from hyperspace at ridiculous speeds (STL max)
                Velocity = Velocity.Normalized() * Math.Min(maxSTLSpeed, MaxSubLightSpeed);
            }

            if (isWarpCapable)
            {
                bool warpingOrSpooling = atWarp || IsSpooling;
                // check if ship is Inhibited by anything
                bool inhibited = UpdateHyperspaceInhibited(timeStep, warpingOrSpooling);
                // this causes warping ships to exit warp
                if (inhibited && warpingOrSpooling)
                {
                    HyperspaceReturn();
                }
            }

            if (!ShouldBank)
            {
                RestoreYBankRotation(timeStep);
            }

            if (atWarp && Velocity.Length() < SpeedLimit)
            {
                // enable full thrust, but don't touch the SpeedLimit
                // so that FormationWarp can work correctly
                ThrustThisFrame = Ships.Thrust.Forward;
            }

            UpdateVelocityAndPosition(timeStep);

            if (isWarpCapable && IsSpooling)
            {
                UpdateWarpSpooling(timeStep);
            }
        }

        public bool TryGetScoutFleeVector(out Vector2 escapePos) => GetEscapeVector(out escapePos, 100000, true);
        public bool TryGetEscapeVector(out Vector2 escapePos) => GetEscapeVector(out escapePos, 20000, false);

        public bool GetEscapeVector(out Vector2 escapePos, float desiredDistance, bool ignoreNonCombat)
        {
            escapePos = Position + Direction.Normalized() * desiredDistance; // default vector - straight through

            if (!InCombat && !ignoreNonCombat) // No need for escape vector if not in combat - turn around
                return false;

            if (IsInFriendlyProjectorRange || !Universe.GravityWells)
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
