using System;
using Algorithms;
using Microsoft.Xna.Framework;
using Ship_Game.Gameplay;
using Ship_Game.Ships;
using Ship_Game.Ships.AI;

namespace Ship_Game.AI
{
    public sealed partial class ShipAI
    {
        public Vector2 MovePosition;
        Vector2 DesiredDirection;
        
        public Planet OrbitTarget;
        float OrbitalAngle = RandomMath.RandomBetween(0f, 360f);
        public WayPoints WayPoints;
        public void ClearWayPoints() => WayPoints.Clear();
    
        public void HoldPosition()
        {
            if (Owner.isSpooling || Owner.engineState == Ship.MoveState.Warp)
                Owner.HyperspaceReturn();
            State = AIState.HoldPosition;
            CombatState = CombatState.HoldPosition;
            Owner.isThrusting = false;
        }

        bool RotateToDirection(Vector2 wantedForward, float elapsedTime, float minDiff)
        {
            if (wantedForward.AlmostZero() || !wantedForward.IsUnitVector())
                Log.Warning($"RotateToDirection {wantedForward} not a unit vector!");

            Vector2 currentForward = Owner.Rotation.RadiansToDirection();
            float angleDiff = (float)Math.Acos(wantedForward.Dot(currentForward));
            if (angleDiff > minDiff)
            {
                float rotationDir = wantedForward.Dot(currentForward.RightVector()) > 0f ? 1f : -1f;
                Owner.RotateToFacing(elapsedTime, angleDiff, rotationDir);
                return true;
            }
            return false;
        }

        bool RotateTowardsPosition(Vector2 lookAt, float elapsedTime, float minDiff)
        {
            if (lookAt.AlmostZero())
                Log.Warning($"RotateTowardsPosition {lookAt} was zero, is this a bug?");

            Vector2 wantedForward = Owner.Position.DirectionToTarget(lookAt);
            return RotateToDirection(wantedForward, elapsedTime, minDiff);
        }

        void AccelerateToWarpPercent(float elapsedTime, float warpPercent = 1.0f)
        {
            float r = Owner.WarpThrust / Owner.NormalWarpThrust;
            if      (r < warpPercent) Owner.WarpThrust += Owner.NormalWarpThrust * elapsedTime;
            else if (r > warpPercent) Owner.WarpThrust -= Owner.NormalWarpThrust * elapsedTime;
            Owner.WarpThrust = Owner.WarpThrust.Clamped(0f, Owner.NormalWarpThrust);
        }

        void SubLightMoveInDirection(Vector2 direction, float elapsedTime, float speedLimit = 0f)
        {
            if (Owner.EnginesKnockedOut)
                return;

            RotateToDirection(direction, elapsedTime, 0.15f);
            Owner.SubLightAccelerate(elapsedTime, speedLimit);
        }

        void SubLightMoveTowardsPosition(Vector2 position, float elapsedTime)
        {
            float distance = position.Distance(Owner.Center);
            if (distance < 50f)
            {
                ReverseThrustUntilStopped(elapsedTime);
                return;
            }
            if (Owner.EnginesKnockedOut)
                return;

            
            float speedLimit = Owner.Speed;
            if (distance < speedLimit) speedLimit = distance * 0.75f;

            // prediction to enhance movement precision
            Vector2 predictedPoint = PredictThrustPosition(position);
            if (!RotateTowardsPosition(predictedPoint, elapsedTime, 0.02f))
            {
                Owner.SubLightAccelerate(elapsedTime, speedLimit);
            }
        }

        void MoveToWithin1000(float elapsedTime, ShipGoal goal)
        {
            // we cannot give a speed limit here, because thrust will
            // engage warp drive and we would be limiting warp speed (baaaad)
            ThrustOrWarpTowardsPosition(goal.MovePosition, elapsedTime);
            
            float distance = Owner.Center.Distance(goal.MovePosition);

            // during warp, we need to bail out way earlier
            if (Owner.engineState == Ship.MoveState.Warp)
            {
                if (distance <= Owner.WarpOutDistance)
                    DequeueWayPointAndOrder();
            }
            else if (distance <= 1000f)
            {
                DequeueWayPointAndOrder();
            }
        }
        
        void MakeFinalApproach(float elapsedTime, ShipGoal goal)
        {    
            Owner.HyperspaceReturn();
            Vector2 targetPos = goal.MovePosition;
            if (goal.fleet != null) targetPos = goal.fleet.Position + Owner.FleetOffset;

            if (Owner.EnginesKnockedOut)
                return;

            // to make the ship perfectly centered
            Vector2 direction = Owner.Direction;
            float distance = Owner.Center.Distance(targetPos);
            if (distance <= 75f)
            {
                if (ReverseThrustUntilStopped(elapsedTime))
                {
                    if (Owner.loyalty == EmpireManager.Player)
                        HadPO = true;
                    HasPriorityOrder = false;
                    DequeueCurrentOrder();
                }
                return;
            }
            
            float speedLimit = goal.SpeedLimit.Clamped(5f, distance);
            if (distance > Owner.Radius)
            {
                // prediction to enhance movement precision
                Vector2 predictedPoint = PredictThrustPosition(targetPos);
                direction = Owner.Center.DirectionToTarget(predictedPoint);
            }

            if (!RotateToDirection(direction, elapsedTime, 0.05f))
            {
                Owner.SubLightAccelerate(elapsedTime, speedLimit);
            }
        }

        void RotateInLineWithVelocity(float elapsedTime)
        {
            if (Owner.Velocity.AlmostZero())
            {
                DequeueCurrentOrder();
                return;
            }

            if (!RotateToDirection(Owner.Velocity.Normalized(), elapsedTime, 0.1f))
            {
                DequeueCurrentOrder(); // rotation complete
            }
        }

        // this is used when we arrive at final position
        void RotateToDesiredFacing(float elapsedTime, ShipGoal goal)
        {
            if (!RotateToDirection(goal.DesiredDirection, elapsedTime, 0.02f))
            {
                DequeueCurrentOrder(); // rotation complete
            }
        }

        // @note This is done just before thrusting and warping to targets
        void RotateToFaceMovePosition(float elapsedTime, ShipGoal goal)
        {
            Vector2 dir = Owner.Position.DirectionToTarget(goal.MovePosition);
            // we need high precision here, otherwise our jumps are inaccurate
            if (RotateToDirection(dir, elapsedTime, 0.05f))
            {
                Owner.HyperspaceReturn();
            }
            else
            {
                DequeueCurrentOrder(); // rotation complete
            }
        }

        // @return TRUE if fully stopped
        bool ReverseThrustUntilStopped(float elapsedTime)
        {
            Owner.HyperspaceReturn();
            if (Owner.Velocity.AlmostZero())
                return true;
            
            float deceleration = Owner.velocityMaximum * elapsedTime;
            if (Owner.Velocity.Length() < deceleration)
            {
                Owner.Velocity = Vector2.Zero;
                return true; // stopped
            }

            // continue breaking velocity
            Owner.Velocity -= Owner.Velocity.Normalized() * deceleration;
            return false;
        }

        // thrust offset used by ThrustOrWarpTowardsPosition
        public Vector2 ThrustTarget { get; private set; }

        Vector2 PredictThrustPosition(Vector2 targetPos)
        {
            // because or ship is actively moving, it needs to correct its thrusting direction
            // this reduces drift and prevents stupidly missing targets with naive "thrust toward target"
            float speedLimit = Owner.Speed;
            Vector2 prediction = new ImpactPredictor(Owner.Center, Owner.Velocity, 
                                                     speedLimit, targetPos).Predict();
            ThrustTarget = prediction;
            return prediction;
        }

        // thrusts towards a position and engages StarDrive if needed
        // speedLimit can control the max movement speed (it even caps FTL speed)
        // if speedLimit == 0f, then Ship.velocityMaximum is used
        //     during Warp, velocityMaximum is set to FTLMax
        void ThrustOrWarpTowardsPosition(Vector2 position, float elapsedTime, float speedLimit = 0f)
        {
            if (Owner.EnginesKnockedOut)
                return;
            
            // this just checks if warp thrust is good
            float actualDiff = Owner.AngleDifferenceToPosition(position);
            float distance = position.Distance(Owner.Center);
            if (UpdateWarpThrust(elapsedTime, actualDiff, distance))
                return; // WayPoint short-cut

            // prediction to enhance movement precision
            Vector2 predictedPoint = PredictThrustPosition(position);
            Owner.RotationNeededForTarget(predictedPoint, 0f, out float predictionDiff, out float rotationDir);

            // If chasing something, and within weapons range
            if (HasPriorityTarget && distance < Owner.maxWeaponsRange * 0.85f)
            {
                if (Owner.engineState == Ship.MoveState.Warp)
                    Owner.HyperspaceReturn();
            }
            else if (!HasPriorityOrder && !HasPriorityTarget && distance < 1000f &&
                    WayPoints.Count <= 1 && Owner.engineState == Ship.MoveState.Warp)
            {
                Owner.HyperspaceReturn();
            }

            if (predictionDiff > 0.025f)
            {
                Owner.RotateToFacing(elapsedTime, predictionDiff, rotationDir);
                return; // don't accelerate until we're faced correctly
            }

            // engage StarDrive if we're moderately far
            if (State != AIState.FormationWarp || Owner.fleet == null) // not in a fleet
            {
                // only warp towards actual warp pos
                if (actualDiff < 0.05f)
                {
                    if      (distance > 7500f && !Owner.InCombat) Owner.EngageStarDrive();
                    else if (distance > 15000f && Owner.InCombat) Owner.EngageStarDrive();
                }
                Owner.SubLightAccelerate(elapsedTime, speedLimit);
            }
            else // In a fleet
            {
                FleetGroupMove(elapsedTime, distance, speedLimit);
            }
        }

        float EstimateMaxTurn(float distance)
        {
            float timeToTarget = distance / (Owner.maxFTLSpeed);
            float maxTurn = Owner.rotationRadiansPerSecond * timeToTarget;
            maxTurn *= 0.4f; // ships can't really turn as numbers would predict...
            // and we don't allow over certain degrees either
            return maxTurn.Clamped(5f.ToRadians(), 60f.ToRadians());
        }

        bool UpdateWarpThrust(float elapsedTime, float angleDiff, float distance)
        {
            if (Owner.engineState != Ship.MoveState.Warp)
            {
                if (Owner.WarpThrust < Owner.NormalWarpThrust)
                    AccelerateToWarpPercent(elapsedTime, 1.0f); // back to normal
                return false;
            }

            if (angleDiff > 0.05f)
                AccelerateToWarpPercent(elapsedTime, 0.05f); // SLOW DOWN to % warp speed
            else if (Owner.WarpThrust < Owner.NormalWarpThrust)
                AccelerateToWarpPercent(elapsedTime, 1.0f); // back to normal

            float maxTurn = EstimateMaxTurn(distance);
            if (angleDiff > maxTurn) // we can't make the turn
            {
                // ok, just cut the corner to next WayPoint maybe?
                if (WayPoints.Count >= 2 && distance > Owner.loyalty.ProjectorRadius * 0.5f)
                {
                    Vector2 next = WayPoints.ElementAt(1);
                    float nextDistance = Owner.Center.Distance(next);
                    if (nextDistance < Owner.loyalty.ProjectorRadius * 5f) // within cut range
                    {
                        float nextDiff = Owner.AngleDifferenceToPosition(next);
                        float nextMaxTurn = EstimateMaxTurn(nextDistance);

                        // Angle to next WayPoint is better than angle to this one
                        if (angleDiff > nextDiff || nextDiff < nextMaxTurn)
                        {
                            DequeueWayPointAndOrder(); // shortcut!
                            return true;
                        }
                    }
                }
                // we definitely can't correct this!
                float tooSharp = Math.Max(1f, maxTurn*1.25f);
                if (angleDiff > tooSharp || angleDiff > (float)Math.PI)
                {
                    Log.Info(ConsoleColor.Red, $"TurnWhileWarping TOO SHARP: {angleDiff}rads {angleDiff.ToDegrees()}° Exit Warp.");
                    Owner.HyperspaceReturn(); // Too sharp of a turn. Drop out of warp
                    return false;
                }
            }
            return false;
        }

        void FleetGroupMove(float elapsedTime, float distance, float speedLimit)
        {
            float fleetSpeed = Owner.fleet.Speed;
            if (distance > 7500f) // Not near destination
            {
                float distanceFleetCenterToDistance = Owner.fleet.StoredFleetDistancetoMove
                    - Owner.fleet.Position.Distance(Owner.fleet.Position + Owner.FleetOffset);

                if (distance <= distanceFleetCenterToDistance)
                {
                    float reduction = distanceFleetCenterToDistance - distance;
                    fleetSpeed = Owner.fleet.Speed - reduction;
                    if (fleetSpeed > Owner.fleet.Speed)
                        fleetSpeed = Owner.fleet.Speed;
                }
                else if (distance > distanceFleetCenterToDistance)
                {
                    float speedIncrease = distance - distanceFleetCenterToDistance;
                    fleetSpeed = Owner.fleet.Speed + speedIncrease;
                }

                if (Owner.fleet.ReadyForWarp)
                {
                    Owner.EngageStarDrive(); // Fleet is ready to Go into warp
                }
                else if (Owner.engineState == Ship.MoveState.Warp)
                {
                    Owner.HyperspaceReturn(); // Fleet is not ready for warp
                }
            }
            else if (Owner.engineState == Ship.MoveState.Warp)
            {
                Owner.HyperspaceReturn(); // Near Destination
                HasPriorityOrder = false;
            }

            speedLimit = Math.Min(fleetSpeed, speedLimit);
            Owner.SubLightAccelerate(elapsedTime, speedLimit);
        }

    }
}