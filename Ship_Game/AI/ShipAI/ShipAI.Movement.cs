using System;
using Algorithms;
using Microsoft.Xna.Framework;
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

        void MakeFinalApproach(float elapsedTime, ShipGoal goal)
        {    
            Owner.HyperspaceReturn();

            if (goal.TargetPlanet != null)
                goal.MovePosition = goal.TargetPlanet.Center;

            float distance = Owner.Center.Distance(goal.MovePosition);

            float timeToStop = distance / goal.SpeedLimit;
            if (distance / (goal.SpeedLimit + 0.001f) <= timeToStop)
            {
                DequeueCurrentOrder();
            }
            else
            {
                if (DistanceLast.AlmostEqual(distance))
                    goal.SpeedLimit += 1.0f;
                ThrustOrWarpTowardsPosition(goal.MovePosition, elapsedTime, goal.SpeedLimit);
            }
            DistanceLast = distance;
        }


        void MakeFinalApproachFleet(float elapsedTime, ShipGoal goal)
        {
            float distance = Owner.Center.Distance(goal.fleet.Position + Owner.FleetOffset);
            if (distance < 100f || DistanceLast > distance)
                DequeueCurrentOrder();
            else
                SubLightMoveTowardsPosition(goal.fleet.Position + Owner.FleetOffset, elapsedTime, goal.fleet.Speed);
            DistanceLast = distance;
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

        void WarpAccelerate(float elapsedTime, float relativeAcceleration = 0.50f)
        {
            float a = Owner.NormalWarpThrust * relativeAcceleration * elapsedTime;
            Owner.WarpThrust = (Owner.WarpThrust + a).Clamped(0f, Owner.NormalWarpThrust);
        }

        void SubLightMoveInDirection(Vector2 direction, float elapsedTime, float speedLimit = 0f)
        {
            if (Owner.EnginesKnockedOut)
                return;

            RotateToDirection(direction, elapsedTime, 0.22f);
            Owner.SubLightAccelerate(elapsedTime, speedLimit);
        }

        void SubLightMoveTowardsPosition(Vector2 position, float elapsedTime)
        {
            if (Owner.Center.Distance(position) < 50f)
            {
                ReverseThrustUntilStopped(elapsedTime);
                return;
            }
            if (Owner.EnginesKnockedOut)
                return;

            RotateTowardsPosition(position, elapsedTime, 0.02f);

            float distance = position.Distance(Owner.Center);
            float speedLimit = Owner.Speed;
            if      (Owner.isSpooling)      speedLimit *= Owner.loyalty.data.FTLModifier;
            else if (distance < speedLimit) speedLimit  = distance * 0.75f;
            
            Owner.SubLightAccelerate(elapsedTime, speedLimit);
        }

        // @note Used for fleets?
        void SubLightMoveTowardsPosition(Vector2 position, float elapsedTime, float speedLimit)
        {   
            if (Owner.EnginesKnockedOut)
                return;
            if (speedLimit < 1f) // @todo this is probably a hack to prevent fleets standing still; find out the real bug
                speedLimit = 300f;

            RotateTowardsPosition(position, elapsedTime, 0.02f);
            Owner.SubLightAccelerate(elapsedTime, speedLimit);
        }

        void MoveToWithin1000(float elapsedTime, ShipGoal goal)
        {
            if (goal.TargetPlanet != null && OrderQueue.Count > 2 &&
                OrderQueue[1].Plan != Plan.MoveToWithin1000)
            {
                goal.MovePosition = goal.TargetPlanet.Center;
            }

            // we cannot give a speed limit here, because thrust will
            // engage warp drive and we would be limiting warp speed (baaaad)
            ThrustOrWarpTowardsPosition(goal.MovePosition, elapsedTime);

            float distance = Owner.Center.Distance(goal.MovePosition);

            // during warp, we need to bail out way earlier
            if (Owner.engineState == Ship.MoveState.Warp)
            {
                float warpOutDistance = 1000f + Owner.GetSTLSpeed();
                if (distance <= warpOutDistance)
                    DequeueWayPointAndOrder();
            }
            else if (distance <= 1000f)
            {
                DequeueWayPointAndOrder();
            }
        }

        void MoveToWithin1000Fleet(float elapsedTime, ShipGoal goal)
        {
            float distance = Owner.Center.Distance(goal.fleet.Position + Owner.FleetOffset);
            float speedLimit = goal.SpeedLimit;
            if (Owner.velocityMaximum >= distance)
                speedLimit = distance;

            if (distance > 10000f)
            {
                Owner.EngageStarDrive();
            }
            else if (distance < 1000f)
            {
                Owner.HyperspaceReturn();
                DequeueCurrentOrder();
                return;
            }
            SubLightMoveTowardsPosition(goal.fleet.Position + Owner.FleetOffset, elapsedTime, speedLimit);
        }

        bool PathCacheLookup(Point startP, Point endP, Vector2 startV, Vector2 endV)
        {
            if (!Owner.loyalty.PathCache.TryGetValue(startP, out Map<Point, Empire.PatchCacheEntry> pathStart)
                || !pathStart.TryGetValue(endP, out Empire.PatchCacheEntry pathEnd))
            {
                foreach (var paths in Owner.loyalty.PathCache.Values)
                {
                    if (!paths.TryGetValue(endP, out Empire.PatchCacheEntry path))
                        continue;
                    foreach(var wayPoint in path.Path)
                    {
                        if (wayPoint.X.AlmostEqual(startP.X,1000) && wayPoint.Y.AlmostEqual(startP.Y,1000))
                            Log.Info("could have used existing path");
                    }
                }

                return false;
            }

            if (pathEnd.Path.Count > 2)
            {
                int n = pathEnd.Path.Count - 2;
                for (var x = 1; x < n; ++x)
                {
                    Vector2 point = pathEnd.Path[x];
                    if (point != Vector2.Zero)
                        WayPoints.Enqueue(point);
                }
            }
            if (endV == Vector2.Zero)
                Log.Error("pathcache error. end = {0},{1}", endV.X.ToString(), endV.Y.ToString());
            WayPoints.Enqueue(endV);

            ++pathEnd.CacheHits;
            return true;
        }

        // @todo Disabled because current path finding is complete shite
        void PlotCourseAsWayPoints(Vector2 startPos, Vector2 endPos)
        {
            // @todo Write a new pathfinder
            WayPoints.Enqueue(endPos);
            return;

            if (Owner.loyalty.grid != null && Vector2.Distance(startPos, endPos) > Owner.loyalty.ProjectorRadius * 2)
            {
                int reducer = Empire.Universe.PathMapReducer;
                int granularity = Owner.loyalty.granularity;

                var startp = new Point((int) startPos.X, (int) startPos.Y);
                startp.X /= reducer;
                startp.Y /= reducer;
                startp.X += granularity;
                startp.Y += granularity;
                startp.X = startp.X < 0 ? 0 : startp.X;
                startp.Y = startp.Y < 0 ? 0 : startp.Y;
                startp.X = startp.X > granularity * 2 ? granularity * 2 : startp.X;
                startp.Y = startp.Y > granularity * 2 ? granularity * 2 : startp.Y;
                var endp = new Point((int) endPos.X, (int) endPos.Y);
                endp.X /= reducer;
                endp.Y /= reducer;
                endp.Y += granularity;
                endp.X += granularity;
                endp.X = endp.X < 0 ? 0 : endp.X;
                endp.Y = endp.Y < 0 ? 0 : endp.Y;
                endp.X = endp.X > granularity * 2 ? granularity * 2 : endp.X;
                endp.Y = endp.Y > granularity * 2 ? granularity * 2 : endp.Y;
                //@Bug Add sanity correct to prevent start and end from getting posistions off the map
                using (Owner.loyalty.LockPatchCache.AcquireReadLock())
                    if (PathCacheLookup(startp, endp, startPos, endPos))
                        return;

                var path = new PathFinderFast(Owner.loyalty.grid)
                {
                    Diagonals = true,
                    HeavyDiagonals = false,
                    PunishChangeDirection = true,
                    Formula = HeuristicFormula.EuclideanNoSQR, // try with HeuristicFormula.MaxDXDY?
                    HeuristicEstimate = 1, // try with 2?
                    SearchLimit = 999999
                };

                Array<PathFinderNode> pathPoints = path.FindPath(startp, endp);
                if (pathPoints == null)
                {
                    WayPoints.Enqueue(endPos);                        
                    return;
                }

                var cacheAdd = new Array<Vector2>();
                int y = pathPoints.Count - 1;
                for (int x = y; x >= 0; x -= 2)
                {
                    PathFinderNode pNode = pathPoints[x];

                    var worldPosition = new Vector2((pNode.X - granularity) * reducer,
                                                    (pNode.Y - granularity) * reducer);
                    if (worldPosition == Vector2.Zero)
                        continue;
                    cacheAdd.Add(worldPosition);

                    if (Vector2.Distance(worldPosition, endPos) > Owner.loyalty.ProjectorRadius * 2
                        && Vector2.Distance(worldPosition, startPos) > Owner.loyalty.ProjectorRadius * 2)
                        WayPoints.Enqueue(worldPosition);
                }

                var cache = Owner.loyalty.PathCache;
                if (!cache.ContainsKey(startp))
                {
                    using (Owner.loyalty.LockPatchCache.AcquireWriteLock())
                    {
                        var endValue = new Empire.PatchCacheEntry(cacheAdd);
                        cache[startp] = new Map<Point, Empire.PatchCacheEntry> {{endp, endValue}};
                        Owner.loyalty.pathcacheMiss++;
                    }
                }
                else if (!cache[startp].ContainsKey(endp))
                {
                    using (Owner.loyalty.LockPatchCache.AcquireWriteLock())
                    {
                        var endValue = new Empire.PatchCacheEntry(cacheAdd);
                        cache[startp].Add(endp, endValue);
                        Owner.loyalty.pathcacheMiss++;
                    }
                }
                else
                {
                    using (Owner.loyalty.LockPatchCache.AcquireReadLock())
                    {
                        PathCacheLookup(startp, endp, startPos, endPos);
                    }
                }

                WayPoints.Enqueue(endPos);
                return;
            }
            WayPoints.Enqueue(endPos);
        }

        void RotateInLineWithVelocity(float elapsedTime)
        {
            if (Owner.Velocity == Vector2.Zero)
            {
                DequeueCurrentOrder();
                return;
            }

            if (!RotateToDirection(Owner.Velocity.Normalized(), elapsedTime, 0.2f))
                DequeueCurrentOrder(); // rotation complete
        }

        // this is used when we arrive at final position
        void RotateToDesiredFacing(float elapsedTime, ShipGoal goal)
        {
            if (!RotateToDirection(goal.DesiredDirection, elapsedTime, 0.02f))
                DequeueCurrentOrder(); // rotation complete
        }

        // @note This is done just before thrusting and warping to targets
        void RotateToFaceMovePosition(float elapsedTime, ShipGoal goal)
        {
            Vector2 dir = Owner.Position.DirectionToTarget(goal.MovePosition);
            // we need high precision here, otherwise our jumps are inaccurate
            if (RotateToDirection(dir, elapsedTime, 0.02f))
                Owner.HyperspaceReturn();
            else
                DequeueCurrentOrder(); // rotation complete
        }

        // @return TRUE if fully stopped
        bool ReverseThrustUntilStopped(float elapsedTime)
        {
            Owner.HyperspaceReturn();
            if (Owner.Velocity.AlmostZero())
                return true;

            Vector2 oldVelocity = Owner.Velocity;
            
            Owner.Velocity -= Owner.Direction * (elapsedTime * Owner.velocityMaximum); // slowly break

            if (oldVelocity.IsOppositeOf(Owner.Velocity)) // we have negated our velocity? full stop.
            {
                Owner.Velocity = Vector2.Zero;
                return true;
            }
            return false; // keep braking next update
        }

        // attempt to stop exactly at goal.MovePosition
        void StopWithBackwardsThrust(float elapsedTime, ShipGoal goal)
        {
            if (goal.TargetPlanet != null && WayPoints.LastPointEquals(goal.TargetPlanet.Center))
                goal.MovePosition = goal.TargetPlanet.Center;
                
            if (Owner.loyalty == EmpireManager.Player)
                HadPO = true;

            HasPriorityOrder = false;

            Owner.HyperspaceReturn();
            float distance = Owner.Center.Distance(goal.MovePosition);

            // precision move, continuously reduce max velocity until we are close enough
            // for full reverse thrust
            if (distance > 30f)
            {
                float breakingTime = distance / Owner.velocityMaximum;
                float precisionSpeed = breakingTime * Owner.velocityMaximum;
                ThrustOrWarpTowardsPosition(goal.MovePosition, elapsedTime, precisionSpeed);
            }
            else // we are close enough come to full stop
            {
                if (ReverseThrustUntilStopped(elapsedTime)) // continuous braking
                    DequeueWayPointAndOrder(); // done!
            }
        }

        // thrusts towards a position and engages StarDrive if needed
        // speedLimit can control the max movement speed (it even caps FTL speed)
        // if speedLimit == 0f, then Ship.velocityMaximum is used
        //     during Warp, velocityMaximum is set to FTLMax
        void ThrustOrWarpTowardsPosition(Vector2 position, float elapsedTime, float speedLimit = 0f)
        {
            if (Owner.EnginesKnockedOut)
                return;

            Owner.RotationNeededForTarget(position, 0f, out float angleDiff, out float rotationDir);
            
            float distance = position.Distance(Owner.Center);
            if (TurnWhileWarping(elapsedTime, angleDiff, distance))
                return;

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

            if (angleDiff > 0.025f)
            {
                Owner.RotateToFacing(elapsedTime, angleDiff, rotationDir);
                return; // I'm not sure about the return statement here. -Gretman
            }

            // engage StarDrive if we're moderately far
            if (State != AIState.FormationWarp || Owner.fleet == null) // not in a fleet
            {
                if (angleDiff < 0.25f) // kinda towards
                {
                    if      (distance > 7500f && !Owner.InCombat) Owner.EngageStarDrive();
                    else if (distance > 15000f && Owner.InCombat) Owner.EngageStarDrive();
                }
            }
            else // In a fleet
            {
                speedLimit = FleetGrouping(distance);
            }
            
            Owner.SubLightAccelerate(elapsedTime, speedLimit);
        }

        bool TurnWhileWarping(float elapsedTime, float angleDiff, float distance)
        {
            float turnRate = Owner.TurnThrust / Owner.Mass / 700f;

            if (angleDiff * 1.25f > turnRate && distance > 2500f &&
                Owner.engineState == Ship.MoveState.Warp) // Might be a turning issue
            {
                if (angleDiff > 1.0f) // 1.0 rad == ~57 degrees
                {
                    Owner.HyperspaceReturn(); // Too sharp of a turn. Drop out of warp
                }
                else
                {
                    float warpSpeed = (Owner.WarpThrust / Owner.Mass + 0.1f) * Owner.loyalty.data.FTLModifier;
                    if (Owner.inborders && Owner.loyalty.data.Traits.InBordersSpeedBonus > 0)
                        warpSpeed *= 1 + Owner.loyalty.data.Traits.InBordersSpeedBonus;

                    if (WayPoints.Count >= 2 && distance > Owner.loyalty.ProjectorRadius / 2 &&
                        Owner.Center.Distance(WayPoints.ElementAt(1)) < Owner.loyalty.ProjectorRadius * 5f)
                    {
                        float angleDiffNext = Owner.AngleDifferenceToPosition(WayPoints.ElementAt(1));
                        if (angleDiff > angleDiffNext || angleDiffNext < turnRate * 0.5)
                            // Angle to next waypoint is better than angle to this one, just cut the corner.
                        {
                            DequeueWayPointAndOrder();
                            return true;
                        }
                    }
                    //                      Turn per tick         ticks left          Speed per tic
                    else if (angleDiff > turnRate / elapsedTime * (distance / (warpSpeed / elapsedTime)))
                        // Can we make the turn in the distance we have remaining?
                    {
                        // Reduce warp thrust by 50% per second until this is an achievable turn
                        WarpAccelerate(elapsedTime, -0.75f);
                    }
                    else if (Owner.WarpThrust < Owner.NormalWarpThrust)
                    {
                        WarpAccelerate(elapsedTime, +0.25f); // Increase warp thrust back to normal by % per second
                    }
                }
            }
            else if (Owner.WarpThrust < Owner.NormalWarpThrust && angleDiff < turnRate)
                // Intentional allowance of the 25% added to angle diff in main if, so it wont accelerate too soon
            {
                WarpAccelerate(elapsedTime, +0.25f); // Increase warp thrust back to normal by % per second
            }

            return false;
        }

        float FleetGrouping(float Distance)
        {
            float speedLimit = Owner.fleet.Speed;
            float distance = (Owner.Center + Owner.FleetOffset).Distance(Owner.fleet.Position + Owner.FleetOffset);
            if (distance > 7500f) // Not near destination
            {
                float distanceFleetCenterToDistance = Owner.fleet.StoredFleetDistancetoMove - Owner.fleet.Position.Distance(Owner.fleet.Position + Owner.FleetOffset);
                speedLimit = Owner.fleet.Speed;

                if (distance <= distanceFleetCenterToDistance)
                {
                    float reduction = distanceFleetCenterToDistance - Distance;
                    speedLimit = Owner.fleet.Speed - reduction;
                    if (speedLimit > Owner.fleet.Speed)
                        speedLimit = Owner.fleet.Speed;
                }
                else if (distance > distanceFleetCenterToDistance)
                {
                    float speedIncrease = distance - distanceFleetCenterToDistance;
                    speedLimit = Owner.fleet.Speed + speedIncrease;
                }

                if (Owner.fleet.ReadyForWarp)
                {
                    Owner.EngageStarDrive(); //Fleet is ready to Go into warp
                }
                else if (Owner.engineState == Ship.MoveState.Warp)
                {
                    Owner.HyperspaceReturn(); //Fleet is not ready for warp
                }
            }
            else if (Owner.engineState == Ship.MoveState.Warp)
            {
                Owner.HyperspaceReturn(); // Near Destination
                HasPriorityOrder = false;
            }
            return speedLimit;
        }

    }
}