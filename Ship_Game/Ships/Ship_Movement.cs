using System;
using SDGraphics;
using SDUtils;
using Ship_Game.AI;
using Vector2 = SDGraphics.Vector2;

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

        // maximum velocity magnitude, this is not updated very often
        // if ship is at warp, max velocity is FTL speed
        public float VelocityMax;
        public float RotationRadsPerSecond;
        public ShipEngines ShipEngines;

        const float DecelThrustPower = 0.5f; // Reverse thrusters work at 50% total engine thrust
        const float SASThrusterPower = 0.25f; // Stability Assist thrusters work at 25% total engine thrust

        // How much this ship should rotate this frame
        // +1.0: rotating towards Right
        // -1.0: rotating towards Left
        float RotationThisFrame;

        // assume Thrust or Mass just changed
        // this is called from StatusChange, which is not very often
        void UpdateVelocityMax() 
        {
            SetMaxFTLSpeed();
            SetMaxSTLSpeed();
            SetMaxVelocity(MaxSTLSpeed, MaxFTLSpeed, engineState == MoveState.Warp);
            RotationRadsPerSecond = Stats.GetTurnRadsPerSec(Level);
        }

        void SetMaxVelocity(float maxSTLSpeed, float maxFTLSpeed, bool atWarp)
        {
            float maxVel = atWarp ? maxFTLSpeed : maxSTLSpeed;
            if (atWarp) // minimum warp speed is LightSpeed
                maxVel = Math.Max(maxVel, LightSpeedConstant);
            VelocityMax = maxVel;
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
                    projectorBonus = Universe.P.FTLModifier;
                else if (!Universe.P.FTLInNeutralSystems || IsInHostileProjectorRange)
                    projectorBonus = Universe.P.EnemyFTLModifier;
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

        public void RotateToFacing(float angleDiff, in Vector2 wantedForward, in Vector2 currentForward)
        {
            float rotationDir = Vectors.RotationDirection(wantedForward, currentForward);
            RotationThisFrame = angleDiff * rotationDir;
        }

        void UpdateShipRotation(FixedSimTime timeStep)
        {
            float rotAmount = RotationThisFrame;
            RotationThisFrame = 0f;

            bool shouldBank = IsVisibleToPlayer && rotAmount != 0f && !AI.IsInOrbit;
            if (shouldBank)
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
            else
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

            if (rotAmount != 0f)
            {
                float maxRotation = timeStep.FixedTime * RotationRadsPerSecond;
                if (Math.Abs(rotAmount) > maxRotation)
                    rotAmount = Math.Sign(rotAmount) * maxRotation;

                Rotation = (Rotation + rotAmount).AsNormalizedRadians();
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

        public void SubLightAccelerate(float speedLimit = 0f, Thrust direction = Thrust.Forward)
        {
            if (engineState == MoveState.Warp)
                return; // Warp speed is updated in UpdateEnginesAndVelocity
            SetSpeedLimit(speedLimit);
            ThrustThisFrame = direction;
        }

        public void AllStop()
        {
            ThrustThisFrame = Thrust.AllStop;
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

            if (engineState == MoveState.Warp)
                ThrustThisFrame = Thrust.Forward; // in Warp, we can only thrust forward

            float maxVelocity = VelocityMax;
            if (SpeedLimit > 0f) // limit maxVel to speed limit
            {
                maxVelocity = Math.Min(SpeedLimit, maxVelocity);
                SpeedLimit = 0f; // always clear speed limit, to avoid ships getting stuck when leaving fleets
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

            SetMaxVelocity(maxSTLSpeed, maxFTLSpeed, atWarp);

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

            UpdateShipRotation(timeStep);

            if (atWarp && Velocity.Length() < SpeedLimit)
            {
                // enable full thrust, but don't touch the SpeedLimit
                // so that FormationWarp can work correctly
                ThrustThisFrame = Thrust.Forward;
            }

            UpdateVelocityAndPosition(timeStep);

            if (isWarpCapable && IsSpooling)
            {
                UpdateWarpSpooling(timeStep);
            }
        }

        public bool TryGetScoutFleeVector(out Vector2 escapePos) => GetEscapeJumpPosition(out escapePos, 100000, true);
        public bool TryGetEscapeVector(out Vector2 escapePos) => GetEscapeJumpPosition(out escapePos, 20000, false);

        /// <summary>
        /// Calculates an escape jump position to disengage from combat
        /// </summary>
        /// <param name="escapePos">Default is straight forward</param>
        /// <param name="desiredDistance"></param>
        /// <param name="ignoreNonCombat"></param>
        /// <returns>TRUE if an escape vector was found, FALSE if ship should go straight to resupply target</returns>
        public bool GetEscapeJumpPosition(out Vector2 escapePos, float desiredDistance, bool ignoreNonCombat)
        {
            Vector2 currentDir = Direction;
            escapePos = Position + currentDir * desiredDistance; // default position - straight through

            if (!InCombat && !ignoreNonCombat) // No need for escape position if not in combat - turn around
                return false;

            if (IsInFriendlyProjectorRange || Universe.P.GravityWellRange == 0f)
                return true; // Wont be inhibited - straight through

            if (System == null)
            {
                if (Inhibited) return false; // Ship is inhibited by something - turn around
                else           return true;  // Outer space - straight through
            }

            // ship is already inside a gravity well
            Planet gravityWell = System.IdentifyGravityWell(this);
            if (gravityWell != null)
            {
                Vector2 fromWellToShip = gravityWell.Position.DirectionToTarget(Position);
                Vector2 left = fromWellToShip.LeftVector();
                Vector2 right = fromWellToShip.RightVector();
                
                // escape left or right, whichever is closer
                float toLeft = Vectors.AngleDifference(left, currentDir);
                float toRight = Vectors.AngleDifference(right, currentDir);
                
                Vector2 offsetLeftOrRight = (toLeft < toRight) ? left : right;
                escapePos = Position + offsetLeftOrRight * desiredDistance;
                return true;
            }

            Array<Planet> potentialWells = new();
            foreach (Planet planet in System.PlanetList)
                if (Position.InRadius(planet.Position, desiredDistance + planet.GravityWellRadius))
                    potentialWells.Add(planet);

            if (potentialWells.IsEmpty)
                return true; // No wells nearby

            int leastWells = int.MaxValue;
            int leftOrRight = RandomMath.RollDie(2) == 1 ? 1 : -1;
            Vector2 bestDir = default;

            for (int i = 0; i < 12; ++i)
            {
                float rotation = Rotation + i * RadMath.Deg30AsRads*leftOrRight;
                Vector2 dirToCheck = rotation.RadiansToDirection();
                int wellHits = HitTestGravityWells(potentialWells, dirToCheck);
                if (wellHits == 0)
                {
                    bestDir = dirToCheck;
                    break; // Found direction with no wells
                }
                if (wellHits < leastWells) // try to get the path with least well hits
                {
                    leastWells = wellHits;
                    bestDir = dirToCheck;
                }
            }

            escapePos = Position + bestDir * desiredDistance;
            return true;
        }

        int HitTestGravityWells(Array<Planet> wells, Vector2 dir)
        {
            Vector2 end = Position + dir;
            int wellHits = 0;
            foreach (Planet planet in wells)
                if (planet.Position.RayHitTestCircle(planet.GravityWellRadius, Position, end, rayRadius:Radius))
                    wellHits += 1;
            return wellHits;
        }
    }
}
