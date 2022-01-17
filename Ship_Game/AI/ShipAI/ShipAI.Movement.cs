using Microsoft.Xna.Framework;
using Ship_Game.Gameplay;
using Ship_Game.Ships;
using Ship_Game.Ships.AI;
using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework.Graphics;
using Ship_Game.Fleets;

namespace Ship_Game.AI
{
    public sealed partial class ShipAI
    {
        // NOTE: This is the final move position
        //     For example, if you have several waypoints, this is the pos of the final waypoint
        //     And for other Ship AI Plans, this is used to store the current/default waypoint
        //     i.e. ExploreSystem sets MovePosition to next planet it likes
        public Vector2 MovePosition;
        public Planet OrbitTarget;

        WayPoints WayPoints = new WayPoints();

        public bool HasWayPoints => WayPoints.Count > 0;
        public WayPoint[] CopyWayPoints() => WayPoints.ToArray();

        public Vector2 DebugDrawPosition => Owner.Position + Owner.Velocity.Normalized() * Owner.Radius;
        public void ClearWayPoints()
        {
            WayPoints.Clear();
        }

        public void SetWayPoints(IReadOnlyList<WayPoint> wayPoints)
        {
            ClearWayPoints();
            foreach (WayPoint wp in wayPoints)
                WayPoints.Enqueue(wp);
        }

        public void HoldPosition()
        {
            Owner.HyperspaceReturn();
            State = AIState.HoldPosition;
            CombatState = CombatState.HoldPosition;
        }

        public void HoldPositionOffensive()
        {
            ShipGoal goal = OrderQueue.PeekFirst;
            if (goal != null && Owner.Position.Distance(goal.MovePosition) > 100f)
            {
                // previous order is finished and now we want to return to original position
                OrderMoveTo(goal.MovePosition, goal.Direction, true, AIState.HoldPosition);
                OrderHoldPositionOffensive(goal.MovePosition, goal.Direction);
            }
        }

        bool RotateToDirection(Vector2 wantedForward, FixedSimTime timeStep, float minDiff)
        {
            Vector2 currentForward = Owner.Rotation.RadiansToDirection();
            float angleDiff = AngleDifferenceToDirection(wantedForward, currentForward);
            if (angleDiff > minDiff)
            {
                float rotationDir = wantedForward.Dot(currentForward.RightVector()) > 0f ? 1f : -1f;
                Owner.RotateToFacing(timeStep, angleDiff, rotationDir, minDiff);
                return true;
            }

            return false;
        }

        static float AngleDifferenceToDirection(Vector2 wantedForward, Vector2 currentForward)
        {
            if (wantedForward.AlmostZero() || !wantedForward.IsUnitVector())
                Log.Error($"RotateToDirection {wantedForward} not a unit vector! This is a bug!");

            return Vectors.AngleDifference(wantedForward, currentForward);
        }
        
        internal bool RotateTowardsPosition(Vector2 lookAt, FixedSimTime timeStep, float minDiff)
        {
            if (lookAt.AlmostZero())
                Log.Error($"RotateTowardsPosition {lookAt} was zero, is this a bug?");

            Vector2 wantedForward = Owner.Position.DirectionToTarget(lookAt);
            return RotateToDirection(wantedForward, timeStep, minDiff);
        }

        // @note This will constantly accelerate by design and
        //       will only slow down a little while turning too much
        internal void SubLightContinuousMoveInDirection(Vector2 direction, FixedSimTime timeStep, float speedLimit = 0f)
        {
            if (Owner.EnginesKnockedOut)
                return;

            if (RotateToDirection(direction, timeStep, 0.15f))
            {
                if (speedLimit <= 0) speedLimit = Owner.SpeedLimit;
                speedLimit *= 0.75f; // uh-oh we're going too fast
            }
            Owner.SubLightAccelerate(speedLimit);
        }

        internal void SubLightMoveTowardsPosition(Vector2 position, FixedSimTime timeStep, float speedLimit = 0f, bool predictPos = true, bool autoSlowDown = true)
        {
            if (Owner.EnginesKnockedOut)
                return;

            if (position.LengthSquared() < 4.0f)
                Log.Error($"SubLightMoveTowardsPosition: invalid position {position}");

            if (speedLimit <= 0f)
                speedLimit = Owner.SpeedLimit;

            if (autoSlowDown)
            {
                float distance = position.Distance(Owner.Position);
                if (distance < 50f)
                {
                    ReverseThrustUntilStopped(timeStep);
                    return;
                }
                if (distance < speedLimit)
                    speedLimit = distance*0.5f;
            }

            Vector2 predictedPoint;
            if (predictPos) // prediction to enhance movement precision
            {
                predictedPoint = PredictThrustPosition(position);
            }
            else
            {
                predictedPoint = position;
                ThrustTarget = position;
            }

            if (!RotateTowardsPosition(predictedPoint, timeStep, 0.02f))
                Owner.SubLightAccelerate(speedLimit);
        }

        // WayPoint move system
        void MoveToWithin1000(FixedSimTime timeStep, ShipGoal goal)
        {
            // we cannot give a speed limit here, because thrust will
            // engage warp drive and we would be limiting warp speed (baaaad)
            // FB - but, for a carrier which is waiting for fighters to board before
            // warp, we must give a speed limit. The limit is reset when all relevant
            // ships are recalled, so no issue with Warp
            float speedLimit = Owner.Carrier.RecallingShipsBeforeWarp ? Owner.SpeedLimit : 0;
            Vector2 movePos = goal.MovePosition; // dynamic move position
            ThrustOrWarpToPos(movePos, timeStep, speedLimit);

            float distance = Owner.Position.Distance(movePos);

            // we need to bail out way earlier when warping
            if (Owner.engineState == Ship.MoveState.Warp)
            {
                if (distance <= Owner.WarpOutDistance)
                    DequeueOrder(goal.HasCombatMove(distance));
            }
            else if (distance <= 1000f)
            {
                DequeueOrder(goal.HasCombatMove(distance));
            }

            if (Owner.AI.BadGuysNear && goal.HasCombatMove(distance))
            {
                if (Owner.Fleet != null && FleetNode != null)
                {
                    Ship closestShip = Owner.AI.Target;
                    if (closestShip == null)
                    {
                        float targetStrength = Owner.AI.PotentialTargets.Sum(s => s.GetStrength());
                        if (targetStrength > 0)
                            closestShip = Owner.AI.PotentialTargets.FindMin(s => s.Position.Distance(Owner.Position));
                    }

                    if (closestShip != null)
                    {
                        float fleetDistance = 0;
                        float actualDistance = Owner.Position.Distance(closestShip.Position);
                        switch (Owner.Fleet.Fcs)
                        {
                            case Fleet.FleetCombatStatus.Maintain:
                                fleetDistance = (Owner.Fleet.AveragePosition() + Owner.AI.FleetNode.FleetOffset).Distance(closestShip.Position);
                                if (actualDistance < Owner.AI.FleetNode.OrdersRadius && fleetDistance <= Owner.AI.FleetNode.OrdersRadius)
                                    SetPriorityOrder(false);
                                break;
                            case Fleet.FleetCombatStatus.Loose:
                                fleetDistance = Owner.Fleet.AveragePosition().Distance(closestShip.Position);
                                if (actualDistance < Owner.AI.FleetNode.OrdersRadius && fleetDistance <= Owner.AI.FleetNode.OrdersRadius)
                                    SetPriorityOrder(false);
                                break;
                            case Fleet.FleetCombatStatus.Free:
                                if (actualDistance <= Owner.SensorRange)
                                    SetPriorityOrder(false);
                                break;
                        }
                    }
                }
                else
                {
                    SetPriorityOrder(false);
                }
            }
        }

        void DequeueOrder(bool combat)
        {
            if (combat)
            {
                DequeueCurrentOrderAndPriority();
            }
            else
            {
                DequeueCurrentOrder();
            }
        }

        void MakeFinalApproach(FixedSimTime timeStep, ShipGoal goal)
        {
            Owner.HyperspaceReturn();

            if (goal.HasCombatMove(0))
            {
                if (HasPriorityOrder && !Owner.Loyalty.isPlayer) // For AI fleets doing priority order
                {
                    HadPO = true;
                    ClearPriorityOrderAndTarget();
                }
            }

            Vector2 targetPos = goal.MovePosition;
            if (goal.Fleet != null && targetPos.AlmostZero()) 
                targetPos = goal.Fleet.FinalPosition + Owner.FleetOffset;

            if (Owner.EnginesKnockedOut)
                return;

            bool debug = Owner.Universe.Debug && Owner.Universe.DebugWin != null
                                               && Debug.DebugInfoScreen.Mode == Debug.DebugModes.PathFinder;

            // to make the ship perfectly centered
            Vector2 direction = Owner.Direction;
            float distance = Owner.Position.Distance(targetPos);
            if (distance <= 75) // final stop, by this point our speed should be sufficiently
            {
                if (debug) Owner.Universe.DebugWin.DrawText(DebugDrawPosition, "STOP", Color.Red);
                if (ReverseThrustUntilStopped(timeStep))
                {
                    DequeueCurrentOrder();
                }
                return;
            }

            if (distance > 1000)
            {
                PushGoalToFront(new ShipGoal(Plan.MoveToWithin1000, targetPos, goal.Direction, AIState.AwaitingOrders, MoveTypes.LastWayPoint, 0, null));
            }

            if (distance > 75)
            {
                // prediction to enhance movement precision
                Vector2 predictedPoint = PredictThrustPosition(targetPos);
                direction = Owner.Position.DirectionToTarget(predictedPoint);
            }
            else
            {
                direction = Owner.Position.DirectionToTarget(targetPos);
            }

            bool isFacingTarget = !RotateToDirection(direction, timeStep, 0.05f);

            float vel = Owner.CurrentVelocity;
            float stoppingDistance = Owner.GetMinDecelerationDistance(vel);

            if (distance <= stoppingDistance)
            {
                ReverseThrustUntilStopped(timeStep);
                if (debug) Owner.Universe.DebugWin.DrawText(DebugDrawPosition, $"REV {distance:0} <= {stoppingDistance:0} ", Color.Red);
            }
            else if (isFacingTarget)
            {
                // make sure to not get at stupid slow speeds but try not to accelerate while slowing down.
                const float minimumSpeed = 25f;
                if (vel < Math.Max(distance, stoppingDistance).LowerBound(minimumSpeed))
                { 
                    float speedLimit = distance;
                    if (goal.SpeedLimit > 0f)
                        speedLimit = Math.Max(speedLimit, goal.GetSpeedLimitFor(Owner));
                    speedLimit = Math.Max(speedLimit, minimumSpeed);

                    Owner.SubLightAccelerate(speedLimit);
                    if (debug)
                        Owner.Universe.DebugWin.DrawText(DebugDrawPosition, $"ACC {distance:0}  {speedLimit:0} ", Color.Red);
                }
            }
        }

        void RotateInLineWithVelocity(FixedSimTime timeStep)
        {
            if (Owner.Velocity.AlmostZero())
            {
                DequeueCurrentOrder();
                return;
            }

            if (!RotateToDirection(Owner.VelocityDirection, timeStep, 0.1f))
            {
                DequeueCurrentOrder(); // rotation complete
            }
        }

        // this is used when we arrive at final position
        void RotateToDesiredFacing(FixedSimTime timeStep, ShipGoal goal)
        {
            SetPriorityOrder(false);
            if (!RotateToDirection(goal.Direction, timeStep, 0.02f))
            {
                DequeueCurrentOrder(); // rotation complete
            }
        }

        // @note This is done just before thrusting and warping to targets
        void RotateToFaceMovePosition(FixedSimTime timeStep, ShipGoal goal)
        {
            Vector2 dir = Owner.Position.DirectionToTarget(goal.MovePosition);
            // we need high precision here, otherwise our jumps are inaccurate
            if (RotateToDirection(dir, timeStep, 0.05f))
            {
                Owner.HyperspaceReturn();
            }
            else
            {
                DequeueCurrentOrder(); // rotation complete
            }
        }

        // @return TRUE if fully stopped
        internal bool ReverseThrustUntilStopped(FixedSimTime timeStep)
        {
            Owner.HyperspaceReturn();
            if (Owner.Velocity.AlmostZero())
            {
                Owner.Velocity = Vector2.Zero;
                return true;
            }
            
            float deceleration = (Owner.VelocityMaximum * timeStep.FixedTime);
            if (Owner.CurrentVelocity.LessOrEqual(deceleration)) // we are almost at zero, lets stop.
            {
                Owner.Velocity = Vector2.Zero;
                return true; // stopped
            }

            Owner.AllStop();
            return false;
        }

        // thrust offset used by ThrustOrWarpTowardsPosition
        public Vector2 ThrustTarget { get; private set; }

        internal Vector2 PredictThrustPosition(Vector2 targetPos)
        {
            // because or ship is actively moving, it needs to correct its thrusting direction
            // this reduces drift and prevents stupidly missing targets with naive "thrust toward target"
            Vector2 prediction = new ImpactPredictor(Owner.Position, Owner.Velocity, targetPos).PredictMovePos();
            ThrustTarget = prediction;
            return prediction;
        }

        /**
         * Thrusts towards a position and engages StarDrive if needed
         * Thrust direction will be adjusted according to current velocity,
         * towards predicted interception point
         * @param pos Target position where we want to arrive at
         * @param timeStep Fixed time step for physics simulation
         * @param speedLimit Can control the max movement speed (it even caps FTL speed)
         *                   if speedLimit == 0f, then Ship.velocityMaximum is used
         *                   during Warp, velocityMaximum is set to FTLMax
         * @param warpExitDistance [0] If set to nonzero, ships will exit warp at this distance
         *                   but only if this is the last WayPoint
         */
        internal void ThrustOrWarpToPos(Vector2 pos, FixedSimTime timeStep, float speedLimit = 0f, float warpExitDistance = 0f)
        {
            if (Owner.EnginesKnockedOut)
                return;

            // this just checks if warp thrust is good
            // we don't need to use predicted position here, since warp is more linear
            // and prediction errors can cause warp to disengage due to sharp angle
            float actualDiff = Owner.AngleDifferenceToPosition(pos);
            float distance = pos.Distance(Owner.Position);
            if (UpdateWarpThrust(timeStep, actualDiff, distance))
            {
                return; // WayPoint short-cut
            }

            if (Owner.engineState == Ship.MoveState.Warp)
            {
                // if chasing something, and within weapons range
                if (HasPriorityTarget && distance < Owner.DesiredCombatRange * 0.25f)
                {
                    Owner.HyperspaceReturn();
                }
                // we are overshooting the target!!
                else if (!HasPriorityOrder && !HasPriorityTarget && distance < 1500f &&
                         WayPoints.Count <= 1)
                {
                    Owner.HyperspaceReturn();
                }
                // warpExitDistance is set and we are closing in on the target
                else if (warpExitDistance > 0f && distance < warpExitDistance)
                {
                    Owner.HyperspaceReturn();
                }
            }

            // prediction to enhance movement precision
            // not at warp though. 
            // because we are warping to the actual point but changing direction to the predicted point. 
            //Vector2 predictedPoint = distance > 30000 ?  PredictThrustPosition(pos) : pos;
            Vector2 predictedPoint = PredictThrustPosition(pos);
            Owner.RotationNeededForTarget(predictedPoint, 0f, out float predictionDiff, out float rotationDir);

            float angleCheck = Owner.engineState != Ship.MoveState.Warp ? 0.02f : 0.0001f;

            if (predictionDiff > angleCheck) // do we need to rotate ourselves before thrusting?
            {
                Owner.RotateToFacing(timeStep, predictionDiff, rotationDir, angleCheck);
                return; // don't accelerate until we're faced correctly
            }

            // engage StarDrive if we're moderately far
            if (State != AIState.FormationWarp || Owner.Fleet == null) // not in a fleet
            {
                // only warp towards actual warp pos
                if (predictionDiff < 0.05f && Owner.MaxFTLSpeed > 0)
                {
                    // NOTE: PriorityOrder must ignore the combat flag
                    if (distance > 7500f)
                    {
                        if (HasPriorityOrder || HasPriorityTarget || !Owner.InCombat)
                            Owner.EngageStarDrive();
                        else if (distance > Owner.WeaponsMaxRange && Owner.InCombat)
                            Owner.EngageStarDrive();
                    }
                }
                Owner.SubLightAccelerate(speedLimit);
            }
            else // In a fleet
            {
                if (Owner.Fleet.AveragePosition().Distance(Owner.Position) > 15000)
                {
                    // This ship is far away from the fleet
                    Owner.EngageStarDrive();
                }
                else
                {
                    if (distance > 7500f) // Not near destination
                    {
                        EngageFormationWarp();
                    }
                    else
                    {
                        DisEngageFormationWarp();
                    }

                    speedLimit = FormationWarpSpeed(speedLimit);
                    Owner.SubLightAccelerate(speedLimit);
                }
            }
        }

        float EstimateMaxTurn(float distance)
        {
            float timeToTarget = distance / Owner.MaxFTLSpeed;
            float maxTurn = Owner.RotationRadiansPerSecond * timeToTarget;
            maxTurn *= 0.4f; // ships can't really turn as numbers would predict...
            // and we don't allow over certain degrees either
            const float minAngle = 5f  * RadMath.DegreeToRadian;
            const float maxAngle = 60f * RadMath.DegreeToRadian;
            return maxTurn.Clamped(minAngle, maxAngle);
        }

        bool UpdateWarpThrust(FixedSimTime timeStep, float angleDiff, float distance)
        {
            if (Owner.engineState != Ship.MoveState.Warp)
            {
                if (Owner.WarpPercent < 1f)
                    Owner.SetWarpPercent(timeStep, 1f); // back to normal
                return false;
            }

            if (angleDiff > 0.04f)
                Owner.SetWarpPercent(timeStep, 0.05f); // SLOW DOWN to % warp speed
            else if (Owner.WarpPercent < 1f)
                Owner.SetWarpPercent(timeStep, 1f); // back to normal

            float maxTurn = EstimateMaxTurn(distance);
            if (angleDiff > maxTurn) // we can't make the turn
            {
                // ok, just cut the corner to next WayPoint maybe?
                if (WayPoints.Count >= 2 && distance > Owner.Loyalty.GetProjectorRadius() * 0.5f)
                {
                    WayPoint next = WayPoints.ElementAt(1);
                    float nextDistance = Owner.Position.Distance(next.Position);
                    if (nextDistance < Owner.Loyalty.GetProjectorRadius() * 5f) // within cut range
                    {
                        float nextDiff = Owner.AngleDifferenceToPosition(next.Position);
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
                    Owner.HyperspaceReturn(); // Too sharp of a turn. Drop out of warp
                    return false;
                }
            }
            return false;
        }

        public void EngageFormationWarp()
        {
            if (Owner.Fleet.ReadyForWarp)
            {
                if (Owner.engineState == Ship.MoveState.Sublight)
                    Owner.EngageStarDrive();
            }
            else
            {
                if (Owner.engineState == Ship.MoveState.Warp && 
                    Owner.ShipEngines.ReadyForFormationWarp == WarpStatus.WaitingOrRecalling)
                {
                    Owner.HyperspaceReturn();
                }
            }
        }

        public void DisEngageFormationWarp()
        {
            if (Owner.IsInWarp)
            {
                if (!Owner.Loyalty.isPlayer) 
                    SetPriorityOrder(false);

                Owner.HyperspaceReturn();
            }
        }

        public float FormationWarpSpeed(float currentSpeedLimit)
        {
            if (Owner.Fleet == null)
                return currentSpeedLimit;
            return Math.Min(Owner.Fleet.FormationWarpSpeed(Owner), currentSpeedLimit);
        }

        public bool IsOrbiting(Planet p) => OrbitTarget == p && Orbit.InOrbit;

        // Minimum desired distance between two ships bounding Radius
        // This can be negative as well, to tweak the overlaps
        public const float FlockingSeparation = 0f;

        // Minimum distance where ships start checking for rotation alignment
        // This needs to be a bit farther away so ships start turning before they collide
        public const float FlockingMinSteeringDistance = 128f;

        // Size of the arc that the ship checks for steering Left or Right
        public const float FlockingSteeringArc = RadMath.Deg90AsRads;

        // Currently friends are not sorted by distance, so this limit should be very high
        // If we can optimize FriendliesNearby so that sorting becomes feasible, this could be reduced
        public const int FlockingMaxNeighbors = 100;

        // Very simple implementation for basic flocking behaviour
        // It's not designed to be perfect, but rather work with performance constraints
        // And give at least some separation to ships, instead of 100-ship death stacks
        //
        // This also improves collision performance, because stacked ships don't benefit from Quadtree subdivision
        // It's a bit long
        public void KeepDistanceUsingFlocking(FixedSimTime timeStep)
        {
            // special case for fleets: if ship is already at its final position
            // ignore all flocking rules and stay put - other ships that are not in place
            // will do their own thing
            if (Owner.Fleet != null && Owner.Fleet.IsShipAtFinalPosition(Owner, 200))
            {
                return;
            }

            // all deflection vectors summed by scaled direction vectors
            Vector2 meanDeflect = Vector2.Zero;

            // max amount of deflection to apply in the final direction
            float maxDeflectionPower = 0f;

            // rotation change needed to avoid colliding with 
            float rotationChange = 0f;

            Vector2 ourPos = Owner.Position;
            var u = Owner.Universe;

            for (int i = 0; i < FriendliesNearby.Length && i < FlockingMaxNeighbors; ++i)
            {
                Ship friend = FriendliesNearby[i];

                // we want to deflect away from friend, so get vector facing towards us
                Vector2 friendPos = friend.Position;
                (ourPos - friendPos).GetDirectionAndLength(out Vector2 deflectionDir, out float distance);

                float combinedRadius = Owner.Radius + friend.Radius;
                float separationDist = combinedRadius + FlockingSeparation;
                if (distance < separationDist)
                {
                    if (u.Debug && u.DebugWin != null)
                    {
                        u.DebugWin.DrawLine(Debug.DebugModes.Normal, ourPos, friendPos, 0.8f, Color.Orange, 0f);
                    }

                    // edge case, two ships are on top of each other, so assign a random direction
                    if (distance < 16f)
                    {
                        meanDeflect += RandomMath.RandomDirection();
                        maxDeflectionPower = 1.0f; // fully deflect away

                        // we don't need to get any more friends, this single random direction is enough
                        // and we also don't want to apply any more rotation changes
                        break;
                    }
                    else
                    {
                        // relative distance to desired separation,
                        // 0.01 if ships are on top of each other; 0.99 if ship is almost out of range
                        // thanks to previous checks, this can never be 0.0 or 1.0
                        float relativeDist = distance / separationDist;

                        // weight deflection direction by the distance
                        // if relativeDistance = 0.01 power is 0.99
                        // if relativeDistance = 0.99 power is 0.01
                        float deflectionPower = 1.0f - relativeDist; // guaranteed (0.0 - 1.0)

                        meanDeflect += deflectionDir * deflectionPower;
                        maxDeflectionPower = Math.Max(maxDeflectionPower, deflectionPower);
                    }
                }

                // TODO: requires new rotation logic to prevent 2 rotations from cancelling out ship rotation
                if (false && rotationChange == 0f)
                {
                    float rotationDist = combinedRadius + FlockingMinSteeringDistance;
                    if (distance < rotationDist)
                    {
                        if (u.Debug && u.DebugWin != null)
                        {
                            u.DebugWin.DrawLine(Debug.DebugModes.Normal, ourPos, friendPos, 0.8f, Color.Green, 0f);
                            u.DebugWin.DrawCircle(Debug.DebugModes.Normal, friendPos, 64, Color.Green);

                            Vector2 left  = (Owner.Rotation - FlockingSteeringArc * 0.5f).RadiansToDirection();
                            Vector2 right = (Owner.Rotation + FlockingSteeringArc * 0.5f).RadiansToDirection();
                            u.DebugWin.DrawLine(Debug.DebugModes.Normal, ourPos, ourPos + left * distance, 0.8f, Color.Green, 0f);
                            u.DebugWin.DrawLine(Debug.DebugModes.Normal, ourPos, ourPos + right * distance, 0.8f, Color.Green, 0f);
                        }

                        // simply check if predicted impact point is ahead of us and inside distance check
                        if (RadMath.IsTargetInsideArc(ourPos, friendPos, Owner.Rotation, FlockingSteeringArc))
                        {
                            // is target on the left side?
                            bool left = RadMath.IsTargetInsideArc(ourPos, friendPos,
                                                                  Owner.Rotation - FlockingSteeringArc, FlockingSteeringArc);
                            // then rotation will be opposite of that: +1 to right, -1 to left
                            rotationChange = left ? +1f : -1f;
                        }
                    }
                }
            }

            ApplyFlocking(timeStep, meanDeflect, maxDeflectionPower, rotationChange);
        }

        void ApplyFlocking(FixedSimTime timeStep, Vector2 meanDeflect, float maxDeflectionPower, float rotationChange)
        {
            if (maxDeflectionPower > 0f)
            {
                // Ship acceleration already makes use of Thrust Force
                // so just use that, and ship velocity update takes care of the rest
                float thrustForce = Owner.Stats.Thrust;
                Vector2 force = meanDeflect.Normalized() * maxDeflectionPower * thrustForce;
                Owner.ApplyForce(force);

                var u = Owner.Universe;
                if (u.Debug && u.DebugWin != null)
                {
                    u.DebugWin.DrawCircle(Debug.DebugModes.Normal, Owner.Position, Owner.Radius+FlockingSeparation, Color.Brown);
                    u.DebugWin.DrawArrow(Debug.DebugModes.Normal, Owner.Position, Owner.Position + force * 0.01f, 0.8f, Color.Red, 0f);
                }
            }

            if (rotationChange != 0f)
            {
                Vector2 wantedDir = (Owner.Rotation + rotationChange).RadiansToDirection();
                RotateToDirection(wantedDir, timeStep, 0.05f);
                //Owner.RotateToFacing(timeStep, rotationChange, rotationChange, 0.05f);

                var u = Owner.Universe;
                if (u.Debug && u.DebugWin != null)
                {
                    var ahead = Owner.Position + Owner.Direction*Owner.Radius;
                    var right = Owner.Direction.RightVector();
                    var offset = right*rotationChange*64f;
                    u.DebugWin.DrawCircle(Debug.DebugModes.Normal, Owner.Position, Owner.Radius+FlockingMinSteeringDistance, Color.LightBlue);
                    u.DebugWin.DrawLine(Debug.DebugModes.Normal, Owner.Position, ahead, 0.8f, Color.Blue, 0f);
                    u.DebugWin.DrawArrow(Debug.DebugModes.Normal, ahead, ahead+offset, 0.8f, Color.Blue, 0f);
                }
            }
        }
    }
}