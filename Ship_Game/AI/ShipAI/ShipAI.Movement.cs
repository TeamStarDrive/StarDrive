using System;
using System.Collections.Generic;
using System.Linq;
using Algorithms;
using Microsoft.Xna.Framework;
using Ship_Game.Gameplay;

namespace Ship_Game.AI {
    public sealed partial class ShipAI
    {
        public Vector2 MovePosition;
        private float DesiredFacing;
        private Vector2 FinalFacingVector;
        public Queue<Vector2> ActiveWayPoints = new Queue<Vector2>();
        public bool ReadyToWarp = true;
        public Planet OrbitTarget;
        private float OrbitalAngle = RandomMath.RandomBetween(0f, 360f);

        public void GoTo(Vector2 movePos, Vector2 facing)
        {
            GotoStep = 0;
            if (Owner.loyalty == EmpireManager.Player)
                HasPriorityOrder = true;
            MovePosition.X = movePos.X;
            MovePosition.Y = movePos.Y;
            FinalFacingVector = facing;
            State = AIState.MoveTo;
        }

        public void HoldPosition()
        {
            if (Owner.isSpooling || Owner.engineState == Ship.MoveState.Warp)
                Owner.HyperspaceReturn();
            State = AIState.HoldPosition;
            Owner.isThrusting = false;
        }

        private void MakeFinalApproach(float elapsedTime, ShipGoal Goal)
        {
            if (Goal.TargetPlanet != null)
                lock (WayPointLocker)
                {
                    ActiveWayPoints.Last().Equals(Goal.TargetPlanet.Center);
                    Goal.MovePosition = Goal.TargetPlanet.Center;
                }
            Owner.HyperspaceReturn();
            Vector2 velocity = Owner.Velocity;
            if (Goal.TargetPlanet != null)
                velocity += Goal.TargetPlanet.Center;
            float timetostop = velocity.Length() / Goal.SpeedLimit;
            float Distance = Owner.Center.Distance(Goal.MovePosition);
            if (Distance / (Goal.SpeedLimit + 0.001f) <= timetostop)
            {
                OrderQueue.RemoveFirst();
            }
            else
            {
                if (DistanceLast == Distance)
                    Goal.SpeedLimit++;
                ThrustTowardsPosition(Goal.MovePosition, elapsedTime, Goal.SpeedLimit);
            }
            DistanceLast = Distance;
        }

        private void MakeFinalApproachDev(float elapsedTime, ShipGoal Goal)
        {
            float speedLimit = (int) Goal.SpeedLimit;

            Owner.HyperspaceReturn();
            Vector2 velocity = Owner.Velocity;
            float distance = Vector2.Distance(Owner.Center, Goal.MovePosition);

            float timetostop = velocity.Length() / speedLimit;

            if (distance / velocity.Length() <= timetostop)
            {
                OrderQueue.RemoveFirst();
            }
            else
            {
                Goal.SpeedLimit = speedLimit;

                ThrustTowardsPosition(Goal.MovePosition, elapsedTime, speedLimit);
            }
            DistanceLast = distance;
        }

        private void MakeFinalApproachFleet(float elapsedTime, ShipGoal Goal)
        {
            float distance = Owner.Center.Distance(Goal.fleet.Position + Owner.FleetOffset);
            if (distance < 100f || DistanceLast > distance)
                OrderQueue.RemoveFirst();
            else
                MoveTowardsPosition(Goal.fleet.Position + Owner.FleetOffset, elapsedTime, Goal.fleet.Speed);
            DistanceLast = distance;
        }

        private void MoveInDirection(Vector2 direction, float elapsedTime)
        {
            if (!Owner.EnginesKnockedOut)
            {
                Owner.isThrusting = true;
                Vector2 wantedForward = Vector2.Normalize(direction);
                var angleDiff = Owner.AngleDiffTo(wantedForward, out Vector2 right, out Vector2 forward);
                float facing = wantedForward.Facing(right);
                if (angleDiff > 0.22f)
                {
                    Owner.isTurning = true;
                    float rotAmount = Math.Min(angleDiff, facing * elapsedTime * Owner.rotationRadiansPerSecond);
                    if (Math.Abs(rotAmount) > angleDiff)
                        rotAmount = rotAmount <= 0f ? -angleDiff : angleDiff;
                    if (rotAmount > 0f)
                    {
                        if (Owner.yRotation > -Owner.maxBank)
                        {
                            Ship owner = Owner;
                            owner.yRotation = owner.yRotation - Owner.yBankAmount;
                        }
                    }
                    else if (rotAmount < 0f && Owner.yRotation < Owner.maxBank)
                    {
                        Ship ship = Owner;
                        ship.yRotation = ship.yRotation + Owner.yBankAmount;
                    }
                    Ship rotation = Owner;
                    rotation.Rotation = rotation.Rotation + rotAmount;
                }
                else if (Owner.yRotation > 0f)
                {
                    Ship owner1 = Owner;
                    owner1.yRotation = owner1.yRotation - Owner.yBankAmount;
                    if (Owner.yRotation < 0f)
                        Owner.yRotation = 0f;
                }
                else if (Owner.yRotation < 0f)
                {
                    Ship ship1 = Owner;
                    ship1.yRotation = ship1.yRotation + Owner.yBankAmount;
                    if (Owner.yRotation > 0f)
                        Owner.yRotation = 0f;
                }
                Ship velocity = Owner;
                velocity.Velocity = velocity.Velocity + Vector2.Normalize(forward) * (elapsedTime * Owner.speed);
                if (Owner.Velocity.Length() > Owner.velocityMaximum)
                    Owner.Velocity = Vector2.Normalize(Owner.Velocity) * Owner.velocityMaximum;
            }
        }

        private void MoveTowardsPosition(Vector2 Position, float elapsedTime)
        {
            if (Owner.Center.Distance(Position) < 50f)
            {
                Owner.Velocity = Vector2.Zero;
                return;
            }
            Position = Position - Owner.Velocity;
            if (!Owner.EnginesKnockedOut)
            {
                Owner.isThrusting = true;
                Vector2 wantedForward = Owner.Center.DirectionToTarget(Position);
                var angleDiff = Owner.AngleDiffTo(wantedForward, out Vector2 right, out Vector2 forward);
                float facing = wantedForward.Facing(right);
                float distance = Vector2.Distance(Position, Owner.Center);

                if (angleDiff > 0.02f)
                {
                    float RotAmount = Math.Min(angleDiff, facing * elapsedTime * Owner.rotationRadiansPerSecond);
                    if (RotAmount > 0f)
                    {
                        if (Owner.yRotation > -Owner.maxBank)
                        {
                            Ship owner = Owner;
                            owner.yRotation = owner.yRotation - Owner.yBankAmount;
                        }
                    }
                    else if (RotAmount < 0f && Owner.yRotation < Owner.maxBank)
                    {
                        Ship ship = Owner;
                        ship.yRotation = ship.yRotation + Owner.yBankAmount;
                    }
                    Owner.isTurning = true;
                    Ship rotation = Owner;
                    rotation.Rotation = rotation.Rotation + RotAmount;
                }
                float speedLimit = Owner.speed;
                if (Owner.isSpooling)
                    speedLimit = speedLimit * Owner.loyalty.data.FTLModifier;
                else if (distance < speedLimit)
                    speedLimit = distance * 0.75f;
                Ship velocity = Owner;
                velocity.Velocity = velocity.Velocity + Vector2.Normalize(forward) * (elapsedTime * speedLimit);
                if (Owner.Velocity.Length() > speedLimit)
                    Owner.Velocity = Vector2.Normalize(Owner.Velocity) * speedLimit;
            }
        }

        /// <summary>
        /// movement to posistion
        /// </summary>
        /// <param name="Position"></param>
        /// <param name="elapsedTime"></param>
        /// <param name="speedLimit"></param>
        private void MoveTowardsPosition(Vector2 Position, float elapsedTime, float speedLimit)
        {
            if (speedLimit < 1f)
                speedLimit = 200f;
            Position = Position - Owner.Velocity;
            if (!Owner.EnginesKnockedOut)
            {
                Owner.isThrusting = true;
                Vector2 wantedForward = Owner.Center.DirectionToTarget(Position);
                var angleDiff = Owner.AngleDiffTo(wantedForward, out Vector2 right, out Vector2 forward);
                float facing = wantedForward.Facing(right);
                if (angleDiff > 0.02f)
                {
                    float RotAmount = Math.Min(angleDiff, facing * elapsedTime * Owner.rotationRadiansPerSecond);
                    if (RotAmount > 0f)
                    {
                        if (Owner.yRotation > -Owner.maxBank)
                        {
                            Ship owner = Owner;
                            owner.yRotation = owner.yRotation - Owner.yBankAmount;
                        }
                    }
                    else if (RotAmount < 0f && Owner.yRotation < Owner.maxBank)
                    {
                        Ship ship = Owner;
                        ship.yRotation = ship.yRotation + Owner.yBankAmount;
                    }
                    Owner.isTurning = true;
                    Ship rotation = Owner;
                    rotation.Rotation = rotation.Rotation + RotAmount;
                }
                if (Owner.isSpooling)
                    speedLimit = speedLimit * Owner.loyalty.data.FTLModifier;
                Ship velocity = Owner;
                velocity.Velocity = velocity.Velocity + Vector2.Normalize(forward) * (elapsedTime * speedLimit);
                if (Owner.Velocity.Length() > speedLimit)
                    Owner.Velocity = Vector2.Normalize(Owner.Velocity) * speedLimit;
            }
        }

        private void MoveToWithin1000(float elapsedTime, ShipGoal goal)
        {
            var distWaypt = 15000f; //fbedard
            if (ActiveWayPoints.Count > 1)
                distWaypt = Empire.ProjectorRadius / 2f;

            if (OrderQueue.NotEmpty && OrderQueue[1].Plan != Plan.MoveToWithin1000 && goal.TargetPlanet != null)
                lock (WayPointLocker)
                {
                    ActiveWayPoints.Last().Equals(goal.TargetPlanet.Center);
                    goal.MovePosition = goal.TargetPlanet.Center;
                }
            float speedLimit = (int) Owner.speed;
            float distance = Owner.Center.Distance(goal.MovePosition);
            if (ActiveWayPoints.Count <= 1)
                if (distance < Owner.speed)
                    speedLimit = distance;
            ThrustTowardsPosition(goal.MovePosition, elapsedTime, speedLimit);
            if (ActiveWayPoints.Count <= 1)
            {
                if (distance <= 1500f)
                    lock (WayPointLocker)
                    {
                        if (ActiveWayPoints.Count > 1)
                            ActiveWayPoints.Dequeue();
                        if (OrderQueue.NotEmpty)
                            OrderQueue.RemoveFirst();
                    }
            }
            else if (Owner.engineState == Ship.MoveState.Warp)
            {
                if (distance <= distWaypt)
                    lock (WayPointLocker)
                    {
                        ActiveWayPoints.Dequeue();
                        if (OrderQueue.NotEmpty)
                            OrderQueue.RemoveFirst();
                    }
            }
            else if (distance <= 1500f)
            {
                lock (WayPointLocker)
                {
                    ActiveWayPoints.Dequeue();
                    if (OrderQueue.NotEmpty)
                        OrderQueue.RemoveFirst();
                }
            }
        }

        private void MoveToWithin1000Fleet(float elapsedTime, ShipGoal goal)
        {
            float Distance = Vector2.Distance(Owner.Center, goal.fleet.Position + Owner.FleetOffset);
            float speedLimit = goal.SpeedLimit;
            if (Owner.velocityMaximum >= Distance)
                speedLimit = Distance;

            if (Distance > 10000f)
            {
                Owner.EngageStarDrive();
            }
            else if (Distance < 1000f)
            {
                Owner.HyperspaceReturn();
                OrderQueue.RemoveFirst();
                return;
            }
            MoveTowardsPosition(goal.fleet.Position + Owner.FleetOffset, elapsedTime, speedLimit);
        }

        private void OrbitShip(Ship ship, float elapsedTime)
        {
            OrbitPos = ship.Center.PointOnCircle(OrbitalAngle, 1500f);
            if (Vector2.Distance(OrbitPos, Owner.Center) < 1500f)
            {
                ShipAI orbitalAngle = this;
                orbitalAngle.OrbitalAngle = orbitalAngle.OrbitalAngle + 15f;
                if (OrbitalAngle >= 360f)
                {
                    ShipAI artificialIntelligence = this;
                    artificialIntelligence.OrbitalAngle = artificialIntelligence.OrbitalAngle - 360f;
                }
                OrbitPos = ship.Position.PointOnCircle(OrbitalAngle, 2500f);
            }
            ThrustTowardsPosition(OrbitPos, elapsedTime, Owner.speed);
        }

        private void OrbitShipLeft(Ship ship, float elapsedTime)
        {
            OrbitPos = ship.Center.PointOnCircle(OrbitalAngle, 1500f);
            if (Vector2.Distance(OrbitPos, Owner.Center) < 1500f)
            {
                ShipAI orbitalAngle = this;
                orbitalAngle.OrbitalAngle = orbitalAngle.OrbitalAngle - 15f;
                if (OrbitalAngle >= 360f)
                {
                    ShipAI artificialIntelligence = this;
                    artificialIntelligence.OrbitalAngle = artificialIntelligence.OrbitalAngle - 360f;
                }
                OrbitPos = ship.Position.PointOnCircle(OrbitalAngle, 2500f);
            }
            ThrustTowardsPosition(OrbitPos, elapsedTime, Owner.speed);
        }

        private bool PathCacheLookup(Point startp, Point endp, Vector2 startv, Vector2 endv)
        {
            if (!Owner.loyalty.PathCache.TryGetValue(startp, out Map<Point, Empire.PatchCacheEntry> pathstart)
                || !pathstart.TryGetValue(endp, out Empire.PatchCacheEntry pathend))
                return false;

            lock (WayPointLocker)
            {
                if (pathend.Path.Count > 2)
                {
                    int n = pathend.Path.Count - 2;
                    for (var x = 1; x < n; ++x)
                    {
                        Vector2 point = pathend.Path[x];
                        if (point != Vector2.Zero)
                            ActiveWayPoints.Enqueue(point);
                    }
                }
                if (endv == Vector2.Zero)
                    Log.Error("pathcache error. end = {0},{1}", endv.X.ToString(), endv.Y.ToString());
                ActiveWayPoints.Enqueue(endv);
            }
            ++pathend.CacheHits;
            return true;
        }

        private void PlotCourseToNew(Vector2 endPos, Vector2 startPos)
        {
            if (Owner.loyalty.grid != null && Vector2.Distance(startPos, endPos) > Empire.ProjectorRadius * 2)
            {
                int reducer = Empire.Universe.reducer; //  (int)(Empire.ProjectorRadius );
                int granularity = Owner.loyalty.granularity; // (int)Empire.ProjectorRadius / 2;

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

                var pathpoints = path.FindPath(startp, endp);
                lock (WayPointLocker)
                {
                    if (pathpoints != null)
                    {
                        var cacheAdd = new Array<Vector2>();
                        //byte lastValue =0;
                        int y = pathpoints.Count() - 1;
                        for (int x = y; x >= 0; x -= 2)
                        {
                            PathFinderNode pnode = pathpoints[x];
                            //var value = this.Owner.loyalty.grid[pnode.X, pnode.Y];
                            //if (value != 1 && lastValue >1)
                            //{
                            //    lastValue--;
                            //    continue;
                            //}
                            //lastValue = value ==1 ?(byte)1 : (byte)2;
                            var translated = new Vector2((pnode.X - granularity) * reducer,
                                (pnode.Y - granularity) * reducer);
                            if (translated == Vector2.Zero)
                                continue;
                            cacheAdd.Add(translated);

                            if (Vector2.Distance(translated, endPos) > Empire.ProjectorRadius * 2
                                && Vector2.Distance(translated, startPos) > Empire.ProjectorRadius * 2)
                                ActiveWayPoints.Enqueue(translated);
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
                    }
                    ActiveWayPoints.Enqueue(endPos);
                    return;
                }
            }
            ActiveWayPoints.Enqueue(endPos);

#if false
                Array<Vector2> goodpoints = new Array<Vector2>();
                //Grid path = new Grid(this.Owner.loyalty, 36, 10f);
                if (Empire.Universe != null && this.Owner.loyalty.SensorNodes.Count != 0)
                    goodpoints = this.Owner.loyalty.pathhMap.Pathfind(startPos, endPos, false);
                if (goodpoints != null && goodpoints.Count > 0)
                {
                    lock (this.WayPointLocker)
                    {
                        foreach (Vector2 wayp in goodpoints.Skip(1))
                        {

                            this.ActiveWayPoints.Enqueue(wayp);
                        }
                        //this.ActiveWayPoints.Enqueue(endPos);
                    }
                    //this.Owner.loyalty.lockPatchCache.EnterWriteLock();
                    //int cache;
                    //if (!this.Owner.loyalty.pathcache.TryGetValue(goodpoints, out cache))
                    //{

                    //    this.Owner.loyalty.pathcache.Add(goodpoints, 0);

                    //}
                    //cache++;
                    this.Owner.loyalty.lockPatchCache.ExitWriteLock();

                }
                else
                {
                    if (startPos != Vector2.Zero && endPos != Vector2.Zero)
                    {
                        // this.ActiveWayPoints.Enqueue(startPos);
                        this.ActiveWayPoints.Enqueue(endPos);
                    }
                    else
                        this.ActiveWayPoints.Clear();
                }
            #endif
        }

        private Array<Vector2> GoodRoad(Vector2 endPos, Vector2 startPos)
        {
            SpaceRoad targetRoad = null;
            var StartRoads = new Array<SpaceRoad>();
            var endRoads = new Array<SpaceRoad>();
            var nodePos = new Array<Vector2>();
            foreach (SpaceRoad road in Owner.loyalty.SpaceRoadsList)
            {
                Vector2 start = road.GetOrigin().Position;
                Vector2 end = road.GetDestination().Position;
                if (Vector2.Distance(start, startPos) < Empire.ProjectorRadius)
                    if (Vector2.Distance(end, endPos) < Empire.ProjectorRadius)
                        targetRoad = road;
                    else
                        StartRoads.Add(road);
                else if (Vector2.Distance(end, startPos) < Empire.ProjectorRadius)
                    if (Vector2.Distance(start, endPos) < Empire.ProjectorRadius)
                        targetRoad = road;
                    else
                        endRoads.Add(road);

                if (targetRoad != null)
                    break;
            }


            if (targetRoad != null)
            {
                foreach (RoadNode node in targetRoad.RoadNodesList)
                    nodePos.Add(node.Position);
                nodePos.Add(endPos);
                nodePos.Add(targetRoad.GetDestination().Position);
                nodePos.Add(targetRoad.GetOrigin().Position);
            }
            return nodePos;
        }

        private Array<Vector2> PlotCourseToNewViaRoad(Vector2 endPos, Vector2 startPos)
        {
            //return null;
            var goodPoints = new Array<Vector2>();
            var potentialEndRoads = new Array<SpaceRoad>();
            var potentialStartRoads = new Array<SpaceRoad>();
            RoadNode nearestNode = null;
            var distanceToNearestNode = 0f;
            foreach (SpaceRoad road in Owner.loyalty.SpaceRoadsList)
            {
                if (Vector2.Distance(road.GetOrigin().Position, endPos) < 300000f ||
                    Vector2.Distance(road.GetDestination().Position, endPos) < 300000f)
                    potentialEndRoads.Add(road);
                foreach (RoadNode projector in road.RoadNodesList)
                    if (nearestNode == null || Vector2.Distance(projector.Position, startPos) < distanceToNearestNode)
                    {
                        potentialStartRoads.Add(road);
                        nearestNode = projector;
                        distanceToNearestNode = Vector2.Distance(projector.Position, startPos);
                    }
            }

            var targetRoads = potentialStartRoads.Intersect(potentialEndRoads).ToList();
            if (targetRoads.Count == 1)
            {
                SpaceRoad targetRoad = targetRoads[0];
                bool startAtOrgin = Vector2.Distance(endPos, targetRoad.GetOrigin().Position) >
                                    Vector2.Distance(endPos, targetRoad.GetDestination().Position);
                var foundstart = false;
                if (startAtOrgin)
                    foreach (RoadNode node in targetRoad.RoadNodesList)
                    {
                        if (!foundstart && node != nearestNode)
                            continue;
                        else if (!foundstart)
                            foundstart = true;
                        goodPoints.Add(node.Position);
                        goodPoints.Add(targetRoad.GetDestination().Position);
                        goodPoints.Add(targetRoad.GetOrigin().Position);
                    }
                else
                    foreach (RoadNode node in targetRoad.RoadNodesList.Reverse<RoadNode>())
                    {
                        if (!foundstart && node != nearestNode)
                            continue;
                        else if (!foundstart)
                            foundstart = true;
                        goodPoints.Add(node.Position);
                        goodPoints.Add(targetRoad.GetDestination().Position);
                        goodPoints.Add(targetRoad.GetOrigin().Position);
                    }
            }
            else if (true)
            {
                while (potentialStartRoads.Intersect(potentialEndRoads).Count() == 0)
                {
                    var test = false;
                    foreach (SpaceRoad road in Owner.loyalty.SpaceRoadsList)
                    {
                        var flag = false;

                        if (!potentialStartRoads.Contains(road))
                            foreach (SpaceRoad proad in potentialStartRoads)
                                if (proad.GetDestination() == road.GetOrigin() ||
                                    proad.GetOrigin() == road.GetDestination())
                                    flag = true;
                        if (flag)
                        {
                            potentialStartRoads.Add(road);
                            test = true;
                        }
                    }
                    if (!test)
                    {
                        Log.Info("failed to find road path for {0}", Owner.loyalty.PortraitName);
                        return new Array<Vector2>();
                    }
                }
                while (!potentialEndRoads.Contains(potentialStartRoads[0]))
                {
                    var test = false;
                    foreach (SpaceRoad road in potentialStartRoads)
                    {
                        var flag = false;

                        if (!potentialEndRoads.Contains(road))
                            foreach (SpaceRoad proad in potentialEndRoads)
                                if (proad.GetDestination() == road.GetOrigin() ||
                                    proad.GetOrigin() == road.GetDestination())
                                    flag = true;
                        if (flag)
                        {
                            test = true;
                            potentialEndRoads.Add(road);
                        }
                    }
                    if (!test)
                    {
                        Log.Info("failed to find road path for {0}", Owner.loyalty.PortraitName);
                        return new Array<Vector2>();
                    }
                }
                targetRoads = potentialStartRoads.Intersect(potentialEndRoads).ToList();
                if (targetRoads.Count > 0)
                {
                    SpaceRoad targetRoad = null;
                    RoadNode targetnode = null;
                    float distance = -1f;
                    foreach (SpaceRoad road in targetRoads)
                    foreach (RoadNode node in road.RoadNodesList)
                        if (distance == -1f || Vector2.Distance(node.Position, startPos) < distance)
                        {
                            targetRoad = road;
                            targetnode = node;
                            distance = Vector2.Distance(node.Position, startPos);
                        }
                    var orgin = false;
                    var startnode = false;
                    foreach (SpaceRoad road in targetRoads)
                        if (road.GetDestination() == targetRoad.GetDestination() ||
                            road.GetDestination() == targetRoad.GetOrigin())
                            orgin = true;
                    if (orgin)
                        foreach (RoadNode node in targetRoad.RoadNodesList)
                            if (!startnode || node != targetnode)
                            {
                                continue;
                            }
                            else
                            {
                                startnode = true;
                                goodPoints.Add(node.Position);
                                goodPoints.Add(targetRoad.GetDestination().Position);
                                goodPoints.Add(targetRoad.GetOrigin().Position);
                            }
                    else
                        foreach (RoadNode node in targetRoad.RoadNodesList.Reverse<RoadNode>())
                            if (!startnode || node != targetnode)
                            {
                                continue;
                            }
                            else
                            {
                                startnode = true;
                                goodPoints.Add(node.Position);
                                goodPoints.Add(targetRoad.GetDestination().Position);
                                goodPoints.Add(targetRoad.GetOrigin().Position);
                            }
                    while (Vector2.Distance(targetRoad.GetOrigin().Position, endPos) > 300000
                           && Vector2.Distance(targetRoad.GetDestination().Position, endPos) > 300000)
                    {
                        targetRoads.Remove(targetRoad);
                        if (orgin)
                        {
                            var test = false;
                            foreach (SpaceRoad road in targetRoads)
                                if (road.GetOrigin() == targetRoad.GetDestination())
                                {
                                    foreach (RoadNode node in road.RoadNodesList)
                                    {
                                        goodPoints.Add(node.Position);
                                        goodPoints.Add(targetRoad.GetDestination().Position);
                                        goodPoints.Add(targetRoad.GetOrigin().Position);
                                    }
                                    targetRoad = road;
                                    test = true;
                                    break;
                                }
                                else if (road.GetDestination() == targetRoad.GetDestination())
                                {
                                    orgin = false;
                                    if (road.GetOrigin() == targetRoad.GetDestination())
                                        foreach (RoadNode node in road.RoadNodesList.Reverse<RoadNode>())
                                        {
                                            goodPoints.Add(node.Position);
                                            goodPoints.Add(targetRoad.GetDestination().Position);
                                            goodPoints.Add(targetRoad.GetOrigin().Position);
                                        }
                                    test = true;
                                    targetRoad = road;
                                    break;
                                }
                            if (!test)
                                orgin = false;
                        }
                        else
                        {
                            var test = false;
                            foreach (SpaceRoad road in targetRoads)
                                if (road.GetOrigin() == targetRoad.GetOrigin())
                                {
                                    foreach (RoadNode node in road.RoadNodesList)
                                    {
                                        goodPoints.Add(node.Position);
                                        goodPoints.Add(targetRoad.GetDestination().Position);
                                        goodPoints.Add(targetRoad.GetOrigin().Position);
                                    }
                                    targetRoad = road;
                                    test = true;
                                    break;
                                }
                                else if (road.GetDestination() == targetRoad.GetOrigin())
                                {
                                    orgin = true;
                                    if (road.GetOrigin() == targetRoad.GetDestination())
                                        foreach (RoadNode node in road.RoadNodesList.Reverse<RoadNode>())
                                        {
                                            goodPoints.Add(node.Position);
                                            goodPoints.Add(targetRoad.GetDestination().Position);
                                            goodPoints.Add(targetRoad.GetOrigin().Position);
                                        }
                                    targetRoad = road;
                                    test = true;
                                    break;
                                }
                            if (!test)
                                break;
                        }
                    }
                }
            }
            return goodPoints;
        }

        private void RotateInLineWithVelocity(float elapsedTime, ShipGoal Goal)
        {
            if (Owner.Velocity == Vector2.Zero)
            {
                OrderQueue.RemoveFirst();
                return;
            }
            var forward = new Vector2((float) Math.Sin((double) Owner.Rotation),
                -(float) Math.Cos((double) Owner.Rotation));
            var right = new Vector2(-forward.Y, forward.X);
            var angleDiff = (float) Math.Acos((double) Vector2.Dot(Vector2.Normalize(Owner.Velocity), forward));
            float facing = Vector2.Dot(Vector2.Normalize(Owner.Velocity), right) > 0f ? 1f : -1f;
            if (angleDiff <= 0.2f)
            {
                OrderQueue.RemoveFirst();
                return;
            }
            RotateToFacing(elapsedTime, angleDiff, facing);
        }

        private void RotateToDesiredFacing(float elapsedTime, ShipGoal goal)
        {
            Vector2 p = MathExt.PointFromRadians(Vector2.Zero, goal.DesiredFacing, 1f);
            Vector2 fvec = Vector2.Zero.DirectionToTarget(p);
            Vector2 wantedForward = Vector2.Normalize(fvec);
            var forward = new Vector2((float) Math.Sin((double) Owner.Rotation),
                -(float) Math.Cos((double) Owner.Rotation));
            var right = new Vector2(-forward.Y, forward.X);
            var angleDiff = (float) Math.Acos((double) Vector2.Dot(wantedForward, forward));
            float facing = Vector2.Dot(wantedForward, right) > 0f ? 1f : -1f;
            if (angleDiff <= 0.02f)
            {
                OrderQueue.RemoveFirst();
                return;
            }
            RotateToFacing(elapsedTime, angleDiff, facing);
        }

        private bool RotateToFaceMovePosition(float elapsedTime, ShipGoal goal)
        {
            var turned = false;
            var forward = new Vector2((float) Math.Sin((double) Owner.Rotation),
                -(float) Math.Cos((double) Owner.Rotation));
            var right = new Vector2(-forward.Y, forward.X);
            Vector2 VectorToTarget = Owner.Center.DirectionToTarget(goal.MovePosition);
            var angleDiff = (float) Math.Acos((double) Vector2.Dot(VectorToTarget, forward));
            if (angleDiff > 0.2f)
            {
                Owner.HyperspaceReturn();
                RotateToFacing(elapsedTime, angleDiff, Vector2.Dot(VectorToTarget, right) > 0f ? 1f : -1f);
                turned = true;
            }
            else if (OrderQueue.NotEmpty)
            {
                OrderQueue.RemoveFirst();
            }
            return turned;
        }

        private bool RotateToFaceMovePosition(float elapsedTime, Vector2 MovePosition)
        {
            var turned = false;
            var forward = new Vector2((float) Math.Sin((double) Owner.Rotation),
                -(float) Math.Cos((double) Owner.Rotation));
            var right = new Vector2(-forward.Y, forward.X);
            Vector2 VectorToTarget = Owner.Center.DirectionToTarget(MovePosition);
            var angleDiff = (float) Math.Acos((double) Vector2.Dot(VectorToTarget, forward));
            if (angleDiff > Owner.rotationRadiansPerSecond * elapsedTime)
            {
                Owner.HyperspaceReturn();
                RotateToFacing(elapsedTime, angleDiff, Vector2.Dot(VectorToTarget, right) > 0f ? 1f : -1f);
                turned = true;
            }

            return turned;
        }

        private void RotateToFacing(float elapsedTime, float angleDiff, float facing)
        {
            Owner.isTurning = true;
            float RotAmount = Math.Min(angleDiff, facing * elapsedTime * Owner.rotationRadiansPerSecond);
            if (Math.Abs(RotAmount) > angleDiff)
                RotAmount = RotAmount <= 0f ? -angleDiff : angleDiff;
            if (RotAmount > 0f)
            {
                if (Owner.yRotation > -Owner.maxBank)
                {
                    Ship owner = Owner;
                    owner.yRotation = owner.yRotation - Owner.yBankAmount;
                }
            }
            else if (RotAmount < 0f && Owner.yRotation < Owner.maxBank)
            {
                Ship ship = Owner;
                ship.yRotation = ship.yRotation + Owner.yBankAmount;
            }
            if (!float.IsNaN(RotAmount))
            {
                Ship rotation = Owner;
                rotation.Rotation = rotation.Rotation + RotAmount;
            }
        }

        private void Stop(float elapsedTime)
        {
            Owner.HyperspaceReturn();
            if (Owner.Velocity == Vector2.Zero || Owner.Velocity.Length() > Owner.VelocityLast.Length())
            {
                Owner.Velocity = Vector2.Zero;
                return;
            }
            var forward = new Vector2((float) Math.Sin((double) Owner.Rotation),
                -(float) Math.Cos((double) Owner.Rotation));
            if (Owner.Velocity.Length() / Owner.velocityMaximum <= elapsedTime ||
                (forward.X <= 0f || Owner.Velocity.X <= 0f) && (forward.X >= 0f || Owner.Velocity.X >= 0f))
            {
                Owner.Velocity = Vector2.Zero;
                return;
            }
            Ship owner = Owner;
            owner.Velocity = owner.Velocity + Vector2.Normalize(-forward) * (elapsedTime * Owner.velocityMaximum);
        }

        private void Stop(float elapsedTime, ShipGoal Goal)
        {
            Owner.HyperspaceReturn();
            if (Owner.Velocity == Vector2.Zero || Owner.Velocity.Length() > Owner.VelocityLast.Length())
            {
                Owner.Velocity = Vector2.Zero;
                OrderQueue.RemoveFirst();
                return;
            }
            var forward = new Vector2((float) Math.Sin((double) Owner.Rotation),
                -(float) Math.Cos((double) Owner.Rotation));
            if (Owner.Velocity.Length() / Owner.velocityMaximum <= elapsedTime ||
                (forward.X <= 0f || Owner.Velocity.X <= 0f) && (forward.X >= 0f || Owner.Velocity.X >= 0f))
            {
                Owner.Velocity = Vector2.Zero;
                return;
            }
            Ship owner = Owner;
            owner.Velocity = owner.Velocity + Vector2.Normalize(-forward) * (elapsedTime * Owner.velocityMaximum);
        }

        private void StopWithBackwardsThrust(float elapsedTime, ShipGoal Goal)
        {
            if (Goal.TargetPlanet != null)
                lock (WayPointLocker)
                {
                    ActiveWayPoints.Last().Equals(Goal.TargetPlanet.Center);
                    Goal.MovePosition = Goal.TargetPlanet.Center;
                }
            if (Owner.loyalty == EmpireManager.Player)
                HadPO = true;
            HasPriorityOrder = false;
            float Distance = Vector2.Distance(Owner.Center, Goal.MovePosition);
            //if (Distance < 100f && Distance < 25f)
            if (Distance < 200f) //fbedard
            {
                OrderQueue.RemoveFirst();
                lock (WayPointLocker)
                {
                    ActiveWayPoints.Clear();
                }
                Owner.Velocity = Vector2.Zero;
                if (Owner.loyalty == EmpireManager.Player)
                    HadPO = true;
                HasPriorityOrder = false;
            }
            Owner.HyperspaceReturn();
            //Vector2 forward2 = Quaternion
            //Quaternion.AngleAxis(_angle, Vector3.forward) * normalizedDirection1
            var forward = new Vector2((float) Math.Sin((double) Owner.Rotation),
                -(float) Math.Cos((double) Owner.Rotation));
            if (Owner.Velocity == Vector2.Zero ||
                Vector2.Distance(Owner.Center + Owner.Velocity * elapsedTime, Goal.MovePosition) >
                Vector2.Distance(Owner.Center, Goal.MovePosition))
            {
                Owner.Velocity = Vector2.Zero;
                OrderQueue.RemoveFirst();
                if (ActiveWayPoints.Count > 0)
                    lock (WayPointLocker)
                    {
                        ActiveWayPoints.Dequeue();
                    }
                return;
            }
            Vector2 velocity = Owner.Velocity;
            float timetostop = velocity.Length() / Goal.SpeedLimit;
            //added by gremlin devekmod timetostopfix
            if (Vector2.Distance(Owner.Center, Goal.MovePosition) / Goal.SpeedLimit <= timetostop + .005)
                //if (Vector2.Distance(this.Owner.Center, Goal.MovePosition) / (this.Owner.Velocity.Length() + 0.001f) <= timetostop)
            {
                Ship owner = Owner;
                owner.Velocity = owner.Velocity + Vector2.Normalize(forward) * (elapsedTime * Goal.SpeedLimit);
                if (Owner.Velocity.Length() > Goal.SpeedLimit)
                    Owner.Velocity = Vector2.Normalize(Owner.Velocity) * Goal.SpeedLimit;
            }
            else
            {
                Ship ship = Owner;
                ship.Velocity = ship.Velocity + Vector2.Normalize(forward) * (elapsedTime * Goal.SpeedLimit);
                if (Owner.Velocity.Length() > Goal.SpeedLimit)
                {
                    Owner.Velocity = Vector2.Normalize(Owner.Velocity) * Goal.SpeedLimit;
                    return;
                }
            }
        }

        private void StopWithBackwardsThrustbroke(float elapsedTime, ShipGoal Goal)
        {
            if (Owner.loyalty == EmpireManager.Player)
                HadPO = true;
            HasPriorityOrder = false;
            float Distance = Vector2.Distance(Owner.Center, Goal.MovePosition);
            if (Distance < 200) //&& Distance > 25f)
            {
                OrderQueue.RemoveFirst();
                lock (WayPointLocker)
                {
                    ActiveWayPoints.Clear();
                }
                Owner.Velocity = Vector2.Zero;
                if (Owner.loyalty == EmpireManager.Player)
                    HadPO = true;
                HasPriorityOrder = false;
            }
            Owner.HyperspaceReturn();
            var forward = new Vector2((float) Math.Sin((double) Owner.Rotation),
                -(float) Math.Cos((double) Owner.Rotation));
            if (Owner.Velocity == Vector2.Zero ||
                Vector2.Distance(Owner.Center + Owner.Velocity * elapsedTime, Goal.MovePosition) >
                Vector2.Distance(Owner.Center, Goal.MovePosition))
            {
                Owner.Velocity = Vector2.Zero;
                OrderQueue.RemoveFirst();
                if (ActiveWayPoints.Count > 0)
                    lock (WayPointLocker)
                    {
                        ActiveWayPoints.Dequeue();
                    }
                return;
            }
            Vector2 velocity = Owner.Velocity;
            float timetostop = (int) velocity.Length() / Goal.SpeedLimit;
            if (Vector2.Distance(Owner.Center, Goal.MovePosition) / Goal.SpeedLimit <=
                timetostop + .005) //(this.Owner.Velocity.Length() + 1)
                if (Math.Abs((int) (DistanceLast - Distance)) < 10)
                {
                    var to1K = new ShipGoal(Plan.MakeFinalApproach, Goal.MovePosition, 0f)
                    {
                        SpeedLimit = Owner.speed > Distance ? Distance : Owner.GetSTLSpeed()
                    };
                    lock (WayPointLocker)
                    {
                        OrderQueue.PushToFront(to1K);
                    }
                    DistanceLast = Distance;
                    return;
                }
            if (Vector2.Distance(Owner.Center, Goal.MovePosition) / (Owner.Velocity.Length() + 0.001f) <= timetostop)
            {
                Ship owner = Owner;
                owner.Velocity = owner.Velocity + Vector2.Normalize(-forward) * (elapsedTime * Goal.SpeedLimit);
                if (Owner.Velocity.Length() > Goal.SpeedLimit)
                    Owner.Velocity = Vector2.Normalize(Owner.Velocity) * Goal.SpeedLimit;
            }
            else
            {
                Ship ship = Owner;
                ship.Velocity = ship.Velocity + Vector2.Normalize(forward) * (elapsedTime * Goal.SpeedLimit);
                if (Owner.Velocity.Length() > Goal.SpeedLimit)
                {
                    Owner.Velocity = Vector2.Normalize(Owner.Velocity) * Goal.SpeedLimit;
                    return;
                }
            }

            DistanceLast = Distance;
        }

        private void ThrustTowardsPosition(Vector2 Position, float elapsedTime, float speedLimit) //Gretman's Version
        {
            if (speedLimit == 0f) speedLimit = Owner.speed;
            float Distance = Vector2.Distance(Position, Owner.Center);
            if (Owner.engineState != Ship.MoveState.Warp) Position = Position - Owner.Velocity;
            if (Owner.EnginesKnockedOut) return;

            Owner.isThrusting = true;
            Vector2 wantedForward = Owner.Center.DirectionToTarget(Position);
            var forward = new Vector2((float) Math.Sin((double) Owner.Rotation),
                -(float) Math.Cos((double) Owner.Rotation));
            var right = new Vector2(-forward.Y, forward.X);
            var angleDiff = (float) Math.Acos((double) Vector2.Dot(wantedForward, forward));
            float facing = Vector2.Dot(wantedForward, right) > 0f ? 1f : -1f;

            float TurnRate = Owner.TurnThrust / Owner.Mass / 700f;

            #region Warp

            if (angleDiff * 1.25f > TurnRate && Distance > 2500f &&
                Owner.engineState == Ship.MoveState.Warp) //Might be a turning issue
            {
                if (angleDiff > 1.0f)
                {
                    Owner.HyperspaceReturn(); //Too sharp of a turn. Drop out of warp
                }
                else
                {
                    float WarpSpeed = (Owner.WarpThrust / Owner.Mass + 0.1f) * Owner.loyalty.data.FTLModifier;
                    if (Owner.inborders && Owner.loyalty.data.Traits.InBordersSpeedBonus > 0)
                        WarpSpeed *= 1 + Owner.loyalty.data.Traits.InBordersSpeedBonus;

                    if (Owner.VanityName == "MerCraft")
                        Log.Info("AngleDiff: " + angleDiff + "     TurnRate = " + TurnRate + "     WarpSpeed = " +
                                 WarpSpeed + "     Distance = " + Distance);
                    //AngleDiff: 1.500662     TurnRate = 0.2491764     WarpSpeed = 26286.67     Distance = 138328.4

                    if (ActiveWayPoints.Count >= 2 && Distance > Empire.ProjectorRadius / 2 &&
                        Vector2.Distance(Owner.Center, ActiveWayPoints.ElementAt(1)) < Empire.ProjectorRadius * 5)
                    {
                        Vector2 wantedForwardNext = Owner.Center.DirectionToTarget(ActiveWayPoints.ElementAt(1));
                        var angleDiffNext = (float) Math.Acos((double) Vector2.Dot(wantedForwardNext, forward));
                        if (angleDiff > angleDiffNext || angleDiffNext < TurnRate * 0.5
                        ) //Angle to next waypoint is better than angle to this one, just cut the corner.
                        {
                            lock (WayPointLocker)
                            {
                                ActiveWayPoints.Dequeue();
                            }
                            if (OrderQueue.NotEmpty) OrderQueue.RemoveFirst();
                            return;
                        }
                    }
                    //                          Turn per tick         ticks left          Speed per tic
                    else if (angleDiff > TurnRate / elapsedTime *
                             (Distance / (WarpSpeed / elapsedTime))
                    ) //Can we make the turn in the distance we have remaining?
                    {
                        Owner.WarpThrust -=
                            Owner.NormalWarpThrust *
                            0.02f; //Reduce warpthrust by 2 percent every frame until this is an acheivable turn
                    }
                    else if (Owner.WarpThrust < Owner.NormalWarpThrust)
                    {
                        Owner.WarpThrust +=
                            Owner.NormalWarpThrust * 0.01f; //Increase warpthrust back to normal 1 percent at a time
                        if (Owner.WarpThrust > Owner.NormalWarpThrust)
                            Owner.WarpThrust = Owner.NormalWarpThrust; //Make sure we dont accidentally go over
                    }
                }
            }
            else if (Owner.WarpThrust < Owner.NormalWarpThrust && angleDiff < TurnRate
            ) //Intentional allowance of the 25% added to angle diff in main if, so it wont accelerate too soon
            {
                Owner.WarpThrust +=
                    Owner.NormalWarpThrust * 0.01f; //Increase warpthrust back to normal 1 percent at a time
                if (Owner.WarpThrust > Owner.NormalWarpThrust)
                    Owner.WarpThrust = Owner.NormalWarpThrust; //Make sure we dont accidentally go over
            }

            #endregion

            if (HasPriorityTarget &&
                Distance < Owner.maxWeaponsRange * 0.85f) //If chasing something, and within weapons range
            {
                if (Owner.engineState == Ship.MoveState.Warp) Owner.HyperspaceReturn();
            }
            else if (!HasPriorityOrder && !HasPriorityTarget && Distance < 1000f && ActiveWayPoints.Count <= 1 &&
                     Owner.engineState == Ship.MoveState.Warp)
            {
                Owner.HyperspaceReturn();
            }

            if (angleDiff > 0.025f) //Stuff for the ship visually banking on the Y axis when turning
            {
                float RotAmount = Math.Min(angleDiff, facing * elapsedTime * Owner.rotationRadiansPerSecond);
                if (RotAmount > 0f && Owner.yRotation > -Owner.maxBank)
                    Owner.yRotation = Owner.yRotation - Owner.yBankAmount;
                else if (RotAmount < 0f && Owner.yRotation < Owner.maxBank)
                    Owner.yRotation = Owner.yRotation + Owner.yBankAmount;
                Owner.isTurning = true;
                Owner.Rotation = Owner.Rotation + (RotAmount > angleDiff ? angleDiff : RotAmount);
                return; //I'm not sure about the return statement here. -Gretman
            }

            if (State != AIState.FormationWarp || Owner.fleet == null) //not in a fleet
            {
                if (Distance > 7500f && !Owner.InCombat && angleDiff < 0.25f) Owner.EngageStarDrive();
                else if (Distance > 15000f && Owner.InCombat && angleDiff < 0.25f) Owner.EngageStarDrive();
            }
            else //In a fleet
            {
                if (Distance > 7500f) //Not near destination
                {
                    var fleetReady = true;

                    using (Owner.fleet.Ships.AcquireReadLock())
                    {
                        foreach (Ship ship in Owner.fleet.Ships)
                        {
                            if (ship.AI.State != AIState.FormationWarp) continue;
                            if (ship.AI.ReadyToWarp && (ship.PowerCurrent / (ship.PowerStoreMax + 0.01f) >= 0.2f ||
                                                        ship.isSpooling))
                            {
                                if (Owner.FightersOut) Owner.RecoverFighters(); //Recall Fighters
                                continue;
                            }
                            fleetReady = false;
                            break;
                        }
                    }

                    float distanceFleetCenterToDistance = Owner.fleet.StoredFleetDistancetoMove;
                    speedLimit = Owner.fleet.Speed;

                    #region FleetGrouping

#if true
                    if (Distance <= distanceFleetCenterToDistance)
                    {
                        float speedreduction = distanceFleetCenterToDistance - Distance;
                        speedLimit = Owner.fleet.Speed - speedreduction;

                        if (speedLimit > Owner.fleet.Speed) speedLimit = Owner.fleet.Speed;
                    }
                    else if (Distance > distanceFleetCenterToDistance && Distance > Owner.speed)
                    {
                        float speedIncrease = Distance - distanceFleetCenterToDistance;
                        speedLimit = Owner.fleet.Speed + speedIncrease;
                    }
#endif

                    #endregion

                    if (fleetReady) Owner.EngageStarDrive(); //Fleet is ready to Go into warp
                    else if (Owner.engineState == Ship.MoveState.Warp)
                        Owner.HyperspaceReturn(); //Fleet is not ready for warp
                }
                else if (Owner.engineState == Ship.MoveState.Warp)
                {
                    Owner.HyperspaceReturn(); //Near Destination
                }
            }

            if (speedLimit > Owner.velocityMaximum) speedLimit = Owner.velocityMaximum;
            else if (speedLimit < 0) speedLimit = 0;

            Owner.Velocity = Owner.Velocity + Vector2.Normalize(forward) * (elapsedTime * speedLimit);
            if (Owner.Velocity.Length() > speedLimit) Owner.Velocity = Vector2.Normalize(Owner.Velocity) * speedLimit;
        }

        private void ThrustTowardsPositionOld(Vector2 Position, float elapsedTime, float speedLimit)
        {
            if (speedLimit == 0f)
                speedLimit = Owner.speed;
            float Ownerspeed = Owner.speed;
            if (Ownerspeed > speedLimit)
                Ownerspeed = speedLimit;
            float Distance = Position.Distance(Owner.Center);

            if (Owner.engineState != Ship.MoveState.Warp)
                Position = Position - Owner.Velocity;
            if (!Owner.EnginesKnockedOut)
            {
                Owner.isThrusting = true;

                Vector2 wantedForward = Vector2.Normalize(Owner.Center.DirectionToTarget(Position));
                var forward = new Vector2((float) Math.Sin((double) Owner.Rotation),
                    -(float) Math.Cos((double) Owner.Rotation));
                var right = new Vector2(-forward.Y, forward.X);
                double angleDiff = Math.Acos(Vector2.Dot(wantedForward, forward));
                double facing = Vector2.Dot(wantedForward, right) > 0f ? 1f : -1f;

                #region warp

                if (angleDiff > 0.25f && Owner.engineState == Ship.MoveState.Warp)
                {
                    if (Owner.VanityName == "MerCraftA") Log.Info("angleDiff: " + angleDiff);
                    if (ActiveWayPoints.Count > 1)
                    {
                        if (angleDiff > 1.0f)
                        {
                            Owner.HyperspaceReturn();
                            if (Owner.VanityName == "MerCraft")
                                Log.Info("Dropped out of warp:  Master Angle too large for warp."
                                         + "   angleDiff: " + angleDiff);
                        }
                        if (Distance <= Empire.ProjectorRadius / 2f)
                            if (angleDiff > 0.25f
                            ) //Gretman tinkering with fbedard's 2nd attempt to smooth movement around waypoints
                            {
                                if (Owner.VanityName == "MerCraft")
                                    Log.Info("Pre Dequeue Queue size:  " + ActiveWayPoints.Count);
                                lock (WayPointLocker)
                                {
                                    ActiveWayPoints.Dequeue();
                                }
                                if (Owner.VanityName == "MerCraft")
                                    Log.Info("Post Dequeue Pre Remove 1st Queue size:  " + ActiveWayPoints.Count);
                                if (OrderQueue.NotEmpty)
                                    OrderQueue.RemoveFirst();
                                if (Owner.VanityName == "MerCraft")
                                    Log.Info("Post Remove 1st Queue size:  " + ActiveWayPoints.Count);
                                Position = ActiveWayPoints.First();
                                Distance = Vector2.Distance(Position, Owner.Center);
                                wantedForward = Owner.Center.DirectionToTarget(Position);
                                forward = new Vector2((float) Math.Sin((double) Owner.Rotation),
                                    -(float) Math.Cos((double) Owner.Rotation));
                                angleDiff = Math.Acos((double) Vector2.Dot(wantedForward, forward));

                                speedLimit = speedLimit * 0.75f;
                                if (Owner.VanityName == "MerCraft")
                                    Log.Info("Rounded Corner:  Slowed down.   angleDiff: {0}", angleDiff);
                            }
                            else
                            {
                                if (Owner.VanityName == "MerCraft")
                                    Log.Info("Pre Dequeue Queue size:  " + ActiveWayPoints.Count);
                                lock (WayPointLocker)
                                {
                                    ActiveWayPoints.Dequeue();
                                }
                                if (Owner.VanityName == "MerCraft")
                                    Log.Info("Post Dequeue Pre Remove 1st Queue size:  " + ActiveWayPoints.Count);
                                if (OrderQueue.NotEmpty)
                                    OrderQueue.RemoveFirst();
                                if (Owner.VanityName == "MerCraft")
                                    Log.Info("Post Remove 1st Queue size:  " + ActiveWayPoints.Count);
                                Position = ActiveWayPoints.First();
                                Distance = Vector2.Distance(Position, Owner.Center);
                                wantedForward = Owner.Center.DirectionToTarget(Position);
                                forward = new Vector2((float) Math.Sin(Owner.Rotation),
                                    -(float) Math.Cos(Owner.Rotation));
                                angleDiff = Math.Acos(Vector2.Dot(wantedForward, forward));
                                if (Owner.VanityName == "MerCraft")
                                    Log.Info("Rounded Corner:  Did not slow down." + "   angleDiff: " + angleDiff);
                            }
                    }
                    else if (Target != null)
                    {
                        float d = Vector2.Distance(Target.Center, Owner.Center);
                        if (angleDiff > 0.400000005960464f)
                            Owner.HyperspaceReturn();
                        else if (d > 25000f)
                            Owner.HyperspaceReturn();
                    }
                    else if (State != AIState.Bombard && State != AIState.AssaultPlanet &&
                             State != AIState.BombardTroops && !IgnoreCombat || OrderQueue.IsEmpty)
                    {
                        Owner.HyperspaceReturn();
                    }
                    else if (OrderQueue.PeekLast.TargetPlanet != null)
                    {
                        float d = OrderQueue.PeekLast.TargetPlanet.Center.Distance(Owner.Center);
                        wantedForward = Owner.Center.DirectionToTarget(OrderQueue.PeekLast.TargetPlanet.Center);
                        angleDiff = (float) Math.Acos((double) Vector2.Dot(wantedForward, forward));
                        if (angleDiff > 0.400000005960464f)
                            Owner.HyperspaceReturn();
                        else if (d > 25000f)
                            Owner.HyperspaceReturn();
                    }
                    else if (angleDiff > .25)
                    {
                        Owner.HyperspaceReturn();
                    }
                }

                #endregion

                if (HasPriorityTarget && Distance < Owner.maxWeaponsRange)
                {
                    if (Owner.engineState == Ship.MoveState.Warp)
                        Owner.HyperspaceReturn();
                }
                else if (!HasPriorityOrder && !HasPriorityTarget && Distance < 1000f && ActiveWayPoints.Count <= 1 &&
                         Owner.engineState == Ship.MoveState.Warp)
                {
                    Owner.HyperspaceReturn();
                }
                float TurnSpeed = 1;
                if (angleDiff > Owner.yBankAmount * .1)
                {
                    double RotAmount = Math.Min(angleDiff, facing * Owner.yBankAmount);
                    if (RotAmount > 0f)
                    {
                        if (Owner.yRotation > -Owner.maxBank)
                        {
                            Ship owner = Owner;
                            owner.yRotation = owner.yRotation - Owner.yBankAmount;
                        }
                    }
                    else if (RotAmount < 0f && Owner.yRotation < Owner.maxBank)
                    {
                        Ship owner1 = Owner;
                        owner1.yRotation = owner1.yRotation + Owner.yBankAmount;
                    }
                    Owner.isTurning = true;
                    Ship rotation = Owner;
                    rotation.Rotation = rotation.Rotation +
                                        (RotAmount > angleDiff ? (float) angleDiff : (float) RotAmount);
                    {
                        float nimble = Owner.rotationRadiansPerSecond;
                        if (angleDiff < nimble)
                            TurnSpeed = (float) ((nimble * 1.5 - angleDiff) / (nimble * 1.5));
                    }
                }
                if (State != AIState.FormationWarp || Owner.fleet == null)
                {
                    if (Distance > 7500f && !Owner.InCombat && angleDiff < 0.25f)
                        Owner.EngageStarDrive();
                    else if (Distance > 15000f && Owner.InCombat && angleDiff < 0.25f)
                        Owner.EngageStarDrive();
                    if (Owner.engineState == Ship.MoveState.Warp)
                        if (angleDiff > .1f)
                            speedLimit = Ownerspeed;
                        else
                            speedLimit = (int) Owner.velocityMaximum;
                    else if (Distance > Ownerspeed * 10f)
                        speedLimit = Ownerspeed;
                    speedLimit *= TurnSpeed;
                    Ship velocity = Owner;
                    velocity.Velocity = velocity.Velocity + Vector2.Normalize(forward) * (elapsedTime * speedLimit);
                    if (Owner.Velocity.Length() > speedLimit)
                        Owner.Velocity = Vector2.Normalize(Owner.Velocity) * speedLimit;
                }
                else
                {
                    if (Distance > 7500f)
                    {
                        var fleetReady = true;
                        using (Owner.fleet.Ships.AcquireReadLock())
                        {
                            foreach (Ship ship in Owner.fleet.Ships)
                            {
                                if (ship.AI.State != AIState.FormationWarp)
                                    continue;
                                if (ship.AI.ReadyToWarp
                                    && (ship.PowerCurrent / (ship.PowerStoreMax + 0.01f) >= 0.2f || ship.isSpooling)
                                )
                                {
                                    if (Owner.FightersOut)
                                        Owner.RecoverFighters();
                                    continue;
                                }
                                fleetReady = false;
                                break;
                            }
                        }

                        float distanceFleetCenterToDistance = Owner.fleet.StoredFleetDistancetoMove; //
                        speedLimit = Owner.fleet.Speed;

                        #region FleetGrouping

                        float fleetPosistionDistance = Distance;
                        if (fleetPosistionDistance <= distanceFleetCenterToDistance)
                        {
                            float speedreduction = distanceFleetCenterToDistance - Distance;
                            speedLimit = (int) (Owner.fleet.Speed - speedreduction);
                            if (speedLimit < 0)
                                speedLimit = 0;
                            else if (speedLimit > Owner.fleet.Speed)
                                speedLimit = (int) Owner.fleet.Speed;
                        }
                        else if (fleetPosistionDistance > distanceFleetCenterToDistance && Distance > Ownerspeed)
                        {
                            float speedIncrease = Distance - distanceFleetCenterToDistance;
                            speedLimit = (int) (Owner.fleet.Speed + speedIncrease);
                        }

                        #endregion

                        if (fleetReady)
                            Owner.EngageStarDrive();
                        else if (Owner.engineState == Ship.MoveState.Warp)
                            Owner.HyperspaceReturn();
                    }
                    else if (Owner.engineState == Ship.MoveState.Warp)
                    {
                        Owner.HyperspaceReturn();
                    }

                    if (speedLimit > Owner.velocityMaximum)
                        speedLimit = Owner.velocityMaximum;
                    else if (speedLimit < 0)
                        speedLimit = 0;
                    Ship velocity1 = Owner;
                    velocity1.Velocity = velocity1.Velocity + Vector2.Normalize(forward) * (elapsedTime * speedLimit);
                    if (Owner.Velocity.Length() > speedLimit)
                    {
                        Owner.Velocity = Vector2.Normalize(Owner.Velocity) * speedLimit;
                        return;
                    }
                }
            }
        }

        public class WayPoints
        {
            public Planet planet { get; set; }
            public Ship ship { get; set; }
            public Vector2 location { get; set; }
        }
    }
}