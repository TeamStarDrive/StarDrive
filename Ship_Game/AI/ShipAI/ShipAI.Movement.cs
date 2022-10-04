using Ship_Game.Gameplay;
using Ship_Game.Ships;
using Ship_Game.Ships.AI;
using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework.Graphics;
using SDGraphics;
using Ship_Game.Data.Serialization;
using Vector2 = SDGraphics.Vector2;

namespace Ship_Game.AI
{
    public sealed partial class ShipAI
    {
        // NOTE: This is the final move position
        //     For example, if you have several waypoints, this is the pos of the final waypoint
        //     And for other Ship AI Plans, this is used to store the current/default waypoint
        //     i.e. ExploreSystem sets MovePosition to next planet it likes
        [StarData] public Vector2 MovePosition;
        [StarData] public Planet OrbitTarget { get; private set; }

        WayPoints WayPoints = new();

        [StarData] WayPoint[] WayPointsSave
        {
            get => WayPoints.ToArray();
            set => WayPoints.Set(value);
        }

        public bool HasWayPoints => WayPoints.Count > 0;
        public WayPoint[] CopyWayPoints() => WayPoints.ToArray();

        public Vector2 DebugDrawPosition => Owner.Position + Owner.Velocity.Normalized() * Owner.Radius;

        public void ClearWayPoints()
        {
            WayPoints.Clear();
        }

        public void SetOrbitTarget(Planet target)
        {
            OrbitTarget = target;
        }

        public void SetWayPoints(IReadOnlyList<WayPoint> wayPoints)
        {
            ClearWayPoints();
            foreach (WayPoint wp in wayPoints)
                WayPoints.Enqueue(wp);
        }

        // Executes the active HoldPosition ShipGoal Plan while respecting MoveOrder stance
        void DoHoldPositionPlan(ShipGoal goal)
        {
            ChangeAIState(AIState.HoldPosition);

            // if HoldPosition is not the last goal, then we should stop holding position
            if (OrderQueue.PeekLast != goal)
            {
                DequeueCurrentOrder();
                return;
            }

            MoveOrder order = goal.MoveOrder;
            Vector2 holdAt = goal.MovePosition;

            // if our ship drifts too far from HoldPosition target,
            // order MoveTo with the original MoveOrder stance
            float distanceFromHoldAt = Owner.Position.Distance(holdAt);
            if (distanceFromHoldAt > 100f)
            {
                OrderMoveTo(holdAt, goal.Direction, AIState.HoldPosition, order);
                OrderHoldPosition(holdAt, goal.Direction, order);
            }
            else if (BadGuysNear && CanEnterCombatFromCurrentStance(order))
            {
                EnterCombatState(AIState.Combat);
            }
        }

        bool RotateToDirection(Vector2 wantedForward, FixedSimTime timeStep, float minDiff)
        {
            Vector2 currentForward = Owner.Rotation.RadiansToDirection();
            float angleDiff = AngleDifferenceToDirection(wantedForward, currentForward);
            if (angleDiff > minDiff)
            {
                Owner.RotateToFacing(angleDiff, wantedForward, currentForward);
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

            if (position.SqLen() < 4.0f)
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
                    DequeueCurrentOrder(goal.MoveOrder);
            }
            else if (distance <= 1000f)
            {
                DequeueCurrentOrder(goal.MoveOrder);
            }

            // does current ship stance require us to enter combat?
            if (BadGuysNear && CanEnterCombatFromCurrentStance(goal.MoveOrder))
            {
                // in Regular stance, distance from goal has to be small enough
                bool canStartEngagingTargets = !goal.HasRegularMoveOrder || distance <= 1500f;
                if (canStartEngagingTargets)
                {
                    EnterCombatState(AIState.Combat);
                }
            }
        }

        void MakeFinalApproach(FixedSimTime timeStep, ShipGoal goal)
        {
            Owner.HyperspaceReturn();

            if (goal.HasAggressiveMoveOrder || goal.HasRegularMoveOrder)
            {
                if (HasPriorityOrder) // Dont stuck in final approach if there are enemies (unless Standing Ground)
                {
                    HadPO = true;
                    ClearPriorityOrderAndTarget();
                }
            }

            Vector2 targetPos = goal.MovePosition;
            if (goal.Fleet != null && targetPos == Vector2.Zero) 
                targetPos = goal.Fleet.GetFinalPos(Owner);

            if (Owner.EnginesKnockedOut)
                return;

            bool debug = Owner.Universe.DebugMode == Debug.DebugModes.PathFinder;

            // to make the ship perfectly centered
            float distance = Owner.Position.Distance(targetPos);
            if (distance <= 75) // final stop, by this point our speed should be sufficiently
            {
                if (debug) Owner.Universe.DebugWin.DrawText(DebugDrawPosition, "STOP", Color.Red);
                if (ReverseThrustUntilStopped(timeStep))
                {
                    DequeueCurrentOrder(goal.MoveOrder);
                }
                return;
            }

            // edge case: we accidentally drifted way past final approach range, so move to 1000 again
            if (distance > 1250f)
            {
                PushGoalToFront(new ShipGoal(Plan.MoveToWithin1000, targetPos, goal.Direction, AIState.AwaitingOrders, goal.MoveOrder, 0, null));
            }

            Vector2 direction;
            if (distance > 75f)
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

        // face towards velocity direction
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
            
            float deceleration = (Owner.VelocityMax * timeStep.FixedTime);
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

            // Use predicted thrust position to enhance movement precision and cancel out lateral movement
            Vector2 predictedPos = PredictThrustPosition(pos);

            // use different angle errors depending if we are warping or not
            float maxAngleError = Owner.engineState == Ship.MoveState.Warp ? 0.001f : 0.02f;

            // do we need to rotate ourselves before thrusting?
            (Vector2 wantedForward, float distance) = Owner.Position.GetDirectionAndLength(predictedPos);
            Vector2 currentForward = Owner.Rotation.RadiansToDirection();
            float predictionDiff = Vectors.AngleDifference(wantedForward, currentForward);

            // this just checks if warp thrust is good
            if (UpdateWarpThrust(timeStep, predictionDiff, distance, wantedForward))
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

            if (predictionDiff > maxAngleError)
            {
                Owner.RotateToFacing(predictionDiff, wantedForward, currentForward);
                return; // don't SubLightAccelerate until we're faced correctly
            }

            // engage StarDrive if we're moderately far
            bool inFleet = Owner.Fleet != null && State == AIState.FormationMoveTo;
            if (inFleet) // FLEET MOVE
            {
                float distFromFleet = Owner.Fleet.AveragePosition().Distance(Owner.Position);
                if (distFromFleet > 15000f)
                {
                    // This ship is far away from the fleet
                    // Enter warp and continue next frame in UpdateWarpThrust()
                    Owner.EngageStarDrive();
                }
                else
                {
                    if (distance > 7500f) // Not near destination
                        EngageFormationWarp();
                    else
                        DisEngageFormationWarp();

                    speedLimit = GetFormationSpeed(speedLimit);
                    Owner.SubLightAccelerate(speedLimit);
                }
            }
            else // SINGLE SHIP MOVE
            {
                // only engage warp if we are facing towards thrust position
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
        }

        float EstimateMaxWarpTurn(float distance)
        {
            float timeToTarget = distance / Owner.Velocity.Length();
            float maxTurn = Owner.RotationRadsPerSecond * timeToTarget;
            const float minAngle = 15.0f * RadMath.DegreeToRadian;
            const float maxAngle = 65.0f * RadMath.DegreeToRadian;
            return maxTurn.Clamped(minAngle, maxAngle);
        }

        bool UpdateWarpThrust(FixedSimTime timeStep, float angleDiff, float distance, Vector2 wantedForward)
        {
            if (Owner.engineState != Ship.MoveState.Warp)
            {
                if (Owner.WarpPercent < 1f)
                    Owner.SetWarpPercent(timeStep, 1f); // back to normal
                return false;
            }

            (Vector2 velDir, float speed) = Owner.Velocity.GetDirectionAndLength();

            // ensure ship is already at light speed,
            // otherwise we could drop out of warp due to minor sideways drift when ship enters warp
            if (speed >= Ship.LightSpeedConstant)
            {
                // get the ship's velocity and intended heading vector relation (+1 same dir, -1 opposite dirs)
                float travel = velDir.Dot(wantedForward);
                if (travel < 0.2f) // the ship is drifting sideways
                {
                    //Log.Write(ConsoleColor.Red, $"Drifting!  travel: {travel.String(2)}  distance: {distance.String(0)}");
                    Owner.HyperspaceReturn();
                    return false;
                }
            }

            // if we are off even by little, aggressively reduce speed
            if (angleDiff > RadMath.Deg1AsRads*0.5f)
                Owner.SetWarpPercent(timeStep, 0.05f); // SLOW DOWN to % warp speed
            else if (Owner.WarpPercent < 1f)
                Owner.SetWarpPercent(timeStep, 1f); // back to normal

            float maxTurn = EstimateMaxWarpTurn(distance);
            //Log.Write($"angleDiff: {angleDiff.DegreeString()}  maxTurn: {maxTurn.DegreeString()}  travel: {travel.String(2)}  distance: {distance.String(0)}  ");

            if (angleDiff > maxTurn) // we can't make the turn
            {
                // just cut the corner to next WayPoint maybe?
                // this is not allowed for Fleets - to prevent individual ships cutting corners and getting ahead of the fleet
                if (Owner.Fleet == null && WayPoints.Count >= 2 && distance > Owner.Loyalty.GetProjectorRadius() * 0.5f)
                {
                    WayPoint next = WayPoints.ElementAt(1);
                    float nextDistance = Owner.Position.Distance(next.Position);
                    if (nextDistance < Owner.Loyalty.GetProjectorRadius() * 5f) // within cut range
                    {
                        float nextDiff = Owner.AngleDifferenceToPosition(next.Position);
                        float nextMaxTurn = EstimateMaxWarpTurn(nextDistance);

                        // Angle to next WayPoint is better than angle to this one
                        if (nextDiff < angleDiff && nextDiff < nextMaxTurn)
                        {
                            //Log.Write(ConsoleColor.Green, $"Shortcut!  nextDiff: {nextDiff.DegreeString()}  <  nextMaxTurn: {nextMaxTurn.DegreeString()}");
                            DequeueOrdersUntilWayPointDequeued(); // shortcut!
                            return true;
                        }
                    }
                }

                // we definitely can't correct this!
                float tooSharp = Math.Min(RadMath.Deg90AsRads, maxTurn * 1.25f);
                if (angleDiff > tooSharp)
                {
                    //Log.Write(ConsoleColor.Red, $"TooSharp! {angleDiff.DegreeString()}  distance: {distance.String(0)}");
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

        public float GetFormationSpeed(float currentSpeedLimit)
        {
            if (Owner.Fleet == null)
                return currentSpeedLimit;
            // always follow formation speed, completely ignore currentSpeedLimit because
            // formation status knows best whether ship should slow down or speed up
            return Owner.Fleet.GetFormationSpeedFor(Owner);
        }

        public bool IsOrbiting(Planet p) => OrbitTarget == p && Orbit.InOrbit;

        public bool IsInOrbit => Orbit.InOrbit;

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
            if (Owner.Fleet != null && Owner.Fleet.IsShipInFormation(Owner, 500))
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
                (Vector2 deflectionDir, float distance) = (ourPos - friendPos).GetDirectionAndLength();

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
