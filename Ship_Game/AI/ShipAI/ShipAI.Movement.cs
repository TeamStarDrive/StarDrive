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

        void MakeFinalApproach(float elapsedTime, ShipGoal Goal)
        {            
            if (WayPoints.Count <= 0)
            {
                Log.Error("Ship Movement: active way points was empty during final approach");
                ClearOrdersNext = true;
                return;
            }
            if (Goal.TargetPlanet != null)
                Goal.MovePosition = Goal.TargetPlanet.Center;

            Owner.HyperspaceReturn();
            Vector2 velocity = Owner.Velocity;
            if (Goal.TargetPlanet != null)
                velocity += Goal.TargetPlanet.Center;
            float timetostop = velocity.Length() / Goal.SpeedLimit;
            float distance = Owner.Center.Distance(Goal.MovePosition);
            if (distance / (Goal.SpeedLimit + 0.001f) <= timetostop)
            {
                OrderQueue.RemoveFirst();
            }
            else
            {
                if (DistanceLast.AlmostEqual(distance))
                    Goal.SpeedLimit++;
                ThrustTowardsPosition(Goal.MovePosition, elapsedTime, Goal.SpeedLimit);
            }
            DistanceLast = distance;
        }


        void MakeFinalApproachFleet(float elapsedTime, ShipGoal Goal)
        {
            float distance = Owner.Center.Distance(Goal.fleet.Position + Owner.FleetOffset);
            if (distance < 100f || DistanceLast > distance)
                OrderQueue.RemoveFirst();
            else
                MoveTowardsPosition(Goal.fleet.Position + Owner.FleetOffset, elapsedTime, Goal.fleet.Speed);
            DistanceLast = distance;
        }

        void MoveInDirection(Vector2 direction, float elapsedTime, float speedLimit = 0f)
        {
            if (Owner.EnginesKnockedOut) return;
            Owner.isThrusting = true;

            var angleDiff = Owner.AngleDiffTo(direction, out Vector2 right, out Vector2 forward);
            if (angleDiff > 0.22f) // start turning?
            {
                float facing = direction.Facing(right);
                Owner.isTurning = true;

                float rotAmount = Math.Min(angleDiff, facing * elapsedTime * Owner.rotationRadiansPerSecond);
                if (Math.Abs(rotAmount) > angleDiff)
                    rotAmount = rotAmount <= 0f ? -angleDiff : angleDiff;
                Owner.Rotation += rotAmount;

                if (rotAmount > 0f)
                {
                    if (Owner.yRotation > -Owner.MaxBank)
                        Owner.yRotation -= Owner.yBankAmount;
                }
                else if (rotAmount < 0f)
                {
                    if (Owner.yRotation < Owner.MaxBank)
                        Owner.yRotation += Owner.yBankAmount;
                }
            }
            else // not turning, slowly reset yRotation
            {
                if (Owner.yRotation > 0f)
                {
                    Owner.yRotation -= Owner.yBankAmount;
                    if (Owner.yRotation < 0f)
                        Owner.yRotation = 0f;
                }
                else if (Owner.yRotation < 0f)
                {
                    Owner.yRotation += Owner.yBankAmount;
                    if (Owner.yRotation > 0f)
                        Owner.yRotation = 0f;
                }
            }

            if (speedLimit <= 0f) speedLimit = Owner.velocityMaximum;

            //var angleReduction = Owner.isTurning  ? .25f : 1;
            //Owner.Velocity *= !Owner.isTurning ? 1 : 0.75f;
            Owner.Velocity += forward.Normalized() * (elapsedTime * Owner.Speed);
            if (Owner.Velocity.Length() > speedLimit)
                Owner.Velocity = Owner.Velocity.Normalized() * speedLimit;
        }

        void MoveTowardsPosition(Vector2 position, float elapsedTime)
        {
            if (Owner.Center.Distance(position) < 50f)
            {
                Owner.Velocity = Vector2.Zero; // whoaaaaa, super-brakes
                return;
            }
            position -= Owner.Velocity;
            if (Owner.EnginesKnockedOut)
                return;

            Owner.isThrusting = true;
            Vector2 wantedForward = Owner.Center.DirectionToTarget(position);
            var angleDiff = Owner.AngleDiffTo(wantedForward, out Vector2 right, out Vector2 forward);
            float facing = wantedForward.Facing(right);
            float distance = Vector2.Distance(position, Owner.Center);

            if (angleDiff > 0.02f)
            {
                float rotAmount = Math.Min(angleDiff, facing * elapsedTime * Owner.rotationRadiansPerSecond);
                if (rotAmount > 0f)
                {
                    if (Owner.yRotation > -Owner.MaxBank)
                    {
                        Owner.yRotation -= Owner.yBankAmount;
                    }
                }
                else if (rotAmount < 0f && Owner.yRotation < Owner.MaxBank)
                {
                    Owner.yRotation += Owner.yBankAmount;
                }
                Owner.isTurning = true;
                Owner.Rotation += rotAmount;
            }
            float speedLimit = Owner.Speed;
            if (Owner.isSpooling)
                speedLimit *= Owner.loyalty.data.FTLModifier;
            else if (distance < speedLimit)
                speedLimit = distance * 0.75f;

            Owner.Velocity += forward * (elapsedTime * speedLimit);
            if (Owner.Velocity.Length() > speedLimit)
                Owner.Velocity = Owner.Velocity.Normalized() * speedLimit;
        }

        void MoveTowardsPosition(Vector2 position, float elapsedTime, float speedLimit)
        {
            if (speedLimit < 1f)
                speedLimit = 200f;
            position = position - Owner.Velocity;
            if (!Owner.EnginesKnockedOut)
            {
                Owner.isThrusting = true;
                Vector2 wantedForward = Owner.Center.DirectionToTarget(position);
                var angleDiff = Owner.AngleDiffTo(wantedForward, out Vector2 right, out Vector2 forward);
                float facing = wantedForward.Facing(right);
                if (angleDiff > 0.02f)
                {
                    float RotAmount = Math.Min(angleDiff, facing * elapsedTime * Owner.rotationRadiansPerSecond);
                    if (RotAmount > 0f)
                    {
                        if (Owner.yRotation > -Owner.MaxBank)
                        {
                            Ship owner = Owner;
                            owner.yRotation = owner.yRotation - Owner.yBankAmount;
                        }
                    }
                    else if (RotAmount < 0f && Owner.yRotation < Owner.MaxBank)
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
                Owner.Velocity *= !Owner.isTurning ? 1 : .95f;
                Ship velocity = Owner;
                velocity.Velocity = velocity.Velocity + Vector2.Normalize(forward) * (elapsedTime * speedLimit);
                if (Owner.Velocity.Length() > speedLimit)
                    Owner.Velocity = Vector2.Normalize(Owner.Velocity) * speedLimit;
            }
        }

        void MoveToWithin1000(float elapsedTime, ShipGoal goal)
        {
            var distWaypt = 15000f; // fbedard
            if (WayPoints.Count > 1)
                distWaypt = Owner.loyalty.ProjectorRadius / 2f;

            if (goal.TargetPlanet != null && OrderQueue.Count > 2 &&
                OrderQueue[1].Plan != Plan.MoveToWithin1000)
            {
                goal.MovePosition = goal.TargetPlanet.Center;
            }

            float speedLimit = (int) Owner.Speed;
            float distance = Owner.Center.Distance(goal.MovePosition);
            if (WayPoints.Count <= 1)
                if (distance < Owner.Speed)
                    speedLimit = distance;

            ThrustTowardsPosition(goal.MovePosition, elapsedTime, speedLimit);

            if (WayPoints.Count <= 1)
            {
                if (distance > 1500f) return;

                if (WayPoints.Count > 1)
                    WayPoints.Dequeue();
                if (OrderQueue.NotEmpty)
                    OrderQueue.RemoveFirst();
            }
            else if (Owner.engineState == Ship.MoveState.Warp)
            {
                if (distance > distWaypt) return;

                WayPoints.Dequeue();
                if (OrderQueue.NotEmpty)
                    OrderQueue.RemoveFirst();
            }
            else if (distance <= 1500f)
            {
                WayPoints.Dequeue();
                if (OrderQueue.NotEmpty)
                    OrderQueue.RemoveFirst();
            }
        }

        void MoveToWithin1000Fleet(float elapsedTime, ShipGoal goal)
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

        bool PathCacheLookup(Point startp, Point endp, Vector2 startv, Vector2 endv)
        {
            if (!Owner.loyalty.PathCache.TryGetValue(startp, out Map<Point, Empire.PatchCacheEntry> pathstart)
                || !pathstart.TryGetValue(endp, out Empire.PatchCacheEntry pathend))
            {
                foreach (var paths in Owner.loyalty.PathCache.Values)
                {
                    if (!paths.TryGetValue(endp, out Empire.PatchCacheEntry path))
                        continue;
                    foreach(var wayPoint in path.Path)
                    {
                        if (wayPoint.X.AlmostEqual(startp.X,1000) && wayPoint.Y.AlmostEqual(startp.Y,1000))
                        {
                            Log.Info("could have used existing path");
                        }
                    }
                }

                return false;
            }

            if (pathend.Path.Count > 2)
            {
                int n = pathend.Path.Count - 2;
                for (var x = 1; x < n; ++x)
                {
                    Vector2 point = pathend.Path[x];
                    if (point != Vector2.Zero)
                        WayPoints.Enqueue(point);
                }
            }
            if (endv == Vector2.Zero)
                Log.Error("pathcache error. end = {0},{1}", endv.X.ToString(), endv.Y.ToString());
            WayPoints.Enqueue(endv);

            ++pathend.CacheHits;
            return true;
        }

        void PlotCourseToNew(Vector2 endPos, Vector2 startPos)
        {
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

                var pathpoints = path.FindPath(startp, endp);

                if (pathpoints == null)
                {
                    WayPoints.Enqueue(endPos);                        
                    return;
                }

                var cacheAdd = new Array<Vector2>();
                int y = pathpoints.Count - 1;
                for (int x = y; x >= 0; x -= 2)
                {
                    PathFinderNode pnode = pathpoints[x];

                    var worldPosition = new Vector2((pnode.X - granularity) * reducer,
                        (pnode.Y - granularity) * reducer);
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

        void RotateInLineWithVelocity(float elapsedTime, ShipGoal goal)
        {
            if (Owner.Velocity == Vector2.Zero)
            {
                OrderQueue.RemoveFirst();
                return;
            }

            Vector2 ownerDir = Owner.Velocity.Normalized();
            Vector2 forward = Owner.Rotation.RadiansToDirection();
            Vector2 right = forward.RightVector();
            float angleDiff = (float)Math.Acos(ownerDir.Dot(forward));
            float facing = ownerDir.Dot(right) > 0f ? 1f : -1f;
            if (angleDiff <= 0.2f)
            {
                OrderQueue.RemoveFirst();
                return;
            }
            RotateToFacing(elapsedTime, angleDiff, facing);
        }

        void RotateToDesiredFacing(float elapsedTime, ShipGoal goal)
        {
            Vector2 wantedForward = goal.DesiredDirection;
            Vector2 forward = Owner.Rotation.RadiansToDirection();
            Vector2 right = forward.RightVector();
            float angleDiff = (float) Math.Acos(wantedForward.Dot(forward));
            float facing = wantedForward.Dot(right) > 0f ? 1f : -1f;
            if (angleDiff <= 0.02f)
            {
                OrderQueue.RemoveFirst();
                return;
            }
            RotateToFacing(elapsedTime, angleDiff, facing);
        }

        bool RotateToFaceMovePosition(float elapsedTime, ShipGoal goal)
        {
            bool turned = false;
            Vector2 forward = Owner.Rotation.RadiansToDirection();
            Vector2 right = forward.RightVector();
            Vector2 dir = Owner.Center.DirectionToTarget(goal.MovePosition);
            float angleDiff = (float)Math.Acos(dir.Dot(forward));
            if (angleDiff > 0.2f)
            {
                Owner.HyperspaceReturn();
                RotateToFacing(elapsedTime, angleDiff, dir.Dot(right) > 0f ? 1f : -1f);
                turned = true;
            }
            else if (OrderQueue.NotEmpty)
            {
                OrderQueue.RemoveFirst();
            }
            return turned;
        }

        void RotateToFacing(float elapsedTime, float angleDiff, float rotationDir)
        {
            Owner.isTurning = true;
            float rotAmount = rotationDir * elapsedTime * Owner.rotationRadiansPerSecond;
            if (float.IsNaN(rotAmount))
            {
                Log.Error($"RotateToFacing: NaN! rotAmount:{rotAmount} angleDiff:{angleDiff}");
                rotAmount = rotationDir * 0.01f; // recover from critical failure
            }

            if (Math.Abs(rotAmount) > angleDiff)
                rotAmount = rotAmount <= 0f ? -angleDiff : angleDiff;

            if (rotAmount > 0f)
            {
                if (Owner.yRotation > -Owner.MaxBank)
                    Owner.yRotation -= Owner.yBankAmount;
            }
            else if (rotAmount < 0f)
            {
                if (Owner.yRotation <  Owner.MaxBank)
                    Owner.yRotation += Owner.yBankAmount;
            }

            Owner.Rotation += rotAmount;
        }

        // @return TRUE if fully stopped
        bool Stop(float elapsedTime)
        {
            Owner.HyperspaceReturn();
            if (Owner.Velocity == Vector2.Zero)
                return true;

            Vector2 oldVelocity = Owner.Velocity;

            // slowly break
            Owner.Velocity -= Owner.Direction * (elapsedTime * Owner.velocityMaximum);

            // we have negated our velocity? full stop.
            if (oldVelocity.IsOppositeOf(Owner.Velocity))
            {
                Owner.Velocity = Vector2.Zero;
                return true;
            }
            return false; // keep braking next update
        }

        void StopWithBackwardsThrust(float elapsedTime, ShipGoal Goal)
        {
            if (Goal.TargetPlanet != null && WayPoints.LastPointEquals(Goal.TargetPlanet.Center))
                Goal.MovePosition = Goal.TargetPlanet.Center;
                
            if (Owner.loyalty == EmpireManager.Player)
                HadPO = true;
            HasPriorityOrder = false;
            float distance = Vector2.Distance(Owner.Center, Goal.MovePosition);
            if (distance < 200f) //fbedard
            {
                WayPoints.Clear();
                
                Owner.Velocity = Vector2.Zero;
                if (Owner.loyalty == EmpireManager.Player)
                    HadPO = true;
                HasPriorityOrder = false;
            }
            Owner.HyperspaceReturn();
            //Vector2 forward2 = Quaternion
            //Quaternion.AngleAxis(_angle, Vector3.forward) * normalizedDirection1
            var forward = new Vector2((float) Math.Sin(Owner.Rotation),
                -(float) Math.Cos(Owner.Rotation));
            if (Owner.Velocity == Vector2.Zero ||
                Vector2.Distance(Owner.Center + Owner.Velocity * elapsedTime, Goal.MovePosition) >
                Vector2.Distance(Owner.Center, Goal.MovePosition))
            {
                Owner.Velocity = Vector2.Zero;
                OrderQueue.RemoveFirst();
                if (WayPoints.Count > 0)
                    WayPoints.Dequeue();
                    
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
                }
            }
        }

        void ThrustTowardsPosition(Vector2 Position, float elapsedTime, float speedLimit) //Gretman's Version
        {
            if (speedLimit.AlmostEqual(0f))
                speedLimit = Owner.velocityMaximum;
            float distance = Position.Distance(Owner.Center);
            if (Owner.engineState != Ship.MoveState.Warp) Position = Position - Owner.Velocity;
            if (Owner.EnginesKnockedOut) return;

            Owner.isThrusting = true;
            Vector2 wantedForward = Owner.Center.DirectionToTarget(Position);
            Vector2 forward = Owner.Rotation.RadiansToDirection();
            Vector2 right = new Vector2(-forward.Y, forward.X);
            float angleDiff = (float) Math.Acos(Vector2.Dot(wantedForward, forward));
            float facing = Vector2.Dot(wantedForward, right) > 0f ? 1f : -1f;

            float turnRate = Owner.TurnThrust / Owner.Mass / 700f;

            #region Warp

            if (angleDiff * 1.25f > turnRate && distance > 2500f &&
                Owner.engineState == Ship.MoveState.Warp) //Might be a turning issue
            {
                if (angleDiff > 1.0f)
                {
                    Owner.HyperspaceReturn(); //Too sharp of a turn. Drop out of warp
                }
                else
                {
                    float warpSpeed = (Owner.WarpThrust / Owner.Mass + 0.1f) * Owner.loyalty.data.FTLModifier;
                    if (Owner.inborders && Owner.loyalty.data.Traits.InBordersSpeedBonus > 0)
                        warpSpeed *= 1 + Owner.loyalty.data.Traits.InBordersSpeedBonus;

                    //if (Owner.VanityName == "MerCraft")
                    //    Log.Info("AngleDiff: " + angleDiff + "     TurnRate = " + turnRate + "     WarpSpeed = " +
                    //             warpSpeed + "     Distance = " + distance);
                    //AngleDiff: 1.500662     TurnRate = 0.2491764     WarpSpeed = 26286.67     Distance = 138328.4

                    if (WayPoints.Count >= 2 && distance > Owner.loyalty.ProjectorRadius / 2 &&
                        Vector2.Distance(Owner.Center, WayPoints.ElementAt(1)) < Owner.loyalty.ProjectorRadius * 5)

                    {

                        Vector2 wantedForwardNext = Owner.Center.DirectionToTarget(WayPoints.ElementAt(1));
                        float angleDiffNext = (float) Math.Acos(Vector2.Dot(wantedForwardNext, forward));
                        if (angleDiff > angleDiffNext || angleDiffNext < turnRate * 0.5)
                            //Angle to next waypoint is better than angle to this one, just cut the corner.

                        {
                            WayPoints.Dequeue();
                            if (OrderQueue.NotEmpty) OrderQueue.RemoveFirst();
                            return;
                        }
                    }
                    //                      Turn per tick         ticks left          Speed per tic
                    else if (angleDiff > turnRate / elapsedTime * (distance / (warpSpeed / elapsedTime)))
                        //Can we make the turn in the distance we have remaining?
                    {
                        Owner.WarpThrust -= Owner.NormalWarpThrust *
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
            else if (Owner.WarpThrust < Owner.NormalWarpThrust && angleDiff < turnRate)
                //Intentional allowance of the 25% added to angle diff in main if, so it wont accelerate too soon
            {
                Owner.WarpThrust += Owner.NormalWarpThrust * 0.01f; //Increase warpthrust back to normal 1 percent at a time
                if (Owner.WarpThrust > Owner.NormalWarpThrust)
                    Owner.WarpThrust = Owner.NormalWarpThrust; //Make sure we dont accidentally go over
            }

            #endregion

            if (HasPriorityTarget && distance < Owner.maxWeaponsRange * 0.85f)
                // If chasing something, and within weapons range
            {
                if (Owner.engineState == Ship.MoveState.Warp)
                    Owner.HyperspaceReturn();
            }
            else if (!HasPriorityOrder && !HasPriorityTarget && distance < 1000f && WayPoints.Count <= 1 &&

                     Owner.engineState == Ship.MoveState.Warp)
            {
                Owner.HyperspaceReturn();
            }

            if (angleDiff > 0.025f) //Stuff for the ship visually banking on the Y axis when turning
            {
                float rotAmount = Math.Min(angleDiff, facing * elapsedTime * Owner.rotationRadiansPerSecond);
                if      (rotAmount > 0f && Owner.yRotation > -Owner.MaxBank) Owner.yRotation -= Owner.yBankAmount;
                else if (rotAmount < 0f && Owner.yRotation <  Owner.MaxBank) Owner.yRotation += Owner.yBankAmount;
                Owner.isTurning = true;
                Owner.Rotation += (rotAmount > angleDiff ? angleDiff : rotAmount);
                //return; //I'm not sure about the return statement here. -Gretman
            }

            if (State != AIState.FormationWarp || Owner.fleet == null) //not in a fleet
            {
                if      (distance > 7500f && !Owner.InCombat && angleDiff < 0.25f) Owner.EngageStarDrive();
                else if (distance > 15000f && Owner.InCombat && angleDiff < 0.25f) Owner.EngageStarDrive();
            }
            else //In a fleet
            {
                speedLimit = FleetGrouping(distance);
            }


            speedLimit = speedLimit.Clamped(0, Owner.velocityMaximum);

            // @todo Need to figure out actual acceleration rates for ships
            //       Thrust to weight ratio or something?
            float acceleration = (speedLimit * 0.5f);
            float brakeWhenTurning = Owner.isTurning ? 0.75f : 1f;
            Owner.Velocity += forward * (elapsedTime * acceleration) * brakeWhenTurning;
            if (Owner.Velocity.Length() > speedLimit)
                Owner.Velocity = Owner.Velocity.Normalized() * speedLimit;
        }

        float FleetGrouping(float Distance)
        {
            float speedLimit = Owner.fleet.Speed;
            float distance = (Owner.Center + Owner.FleetOffset).Distance(Owner.fleet.Position + Owner.FleetOffset);
            if (distance > 7500) //Not near destination
            {
                float distanceFleetCenterToDistance = Owner.fleet.StoredFleetDistancetoMove - Owner.fleet.Position.Distance(Owner.fleet.Position + Owner.FleetOffset);
                speedLimit = Owner.fleet.Speed;

                #region FleetGrouping

#if true
                if (distance <= distanceFleetCenterToDistance)
                {
                    float speedreduction = distanceFleetCenterToDistance - Distance;
                    speedLimit = Owner.fleet.Speed - speedreduction;

                    if (speedLimit > Owner.fleet.Speed) speedLimit = Owner.fleet.Speed;
                }
                else if (distance > distanceFleetCenterToDistance)// && distance > 1000 )
                {
                    float speedIncrease = distance - distanceFleetCenterToDistance;
                    speedLimit = Owner.fleet.Speed + speedIncrease;
                }
#endif

                #endregion

                if (Owner.fleet.ReadyForWarp) Owner.EngageStarDrive(); //Fleet is ready to Go into warp
                else if (Owner.engineState == Ship.MoveState.Warp)
                    Owner.HyperspaceReturn(); //Fleet is not ready for warp
            }
            else if (Owner.engineState == Ship.MoveState.Warp)
            {
                Owner.HyperspaceReturn(); //Near Destination
                HasPriorityOrder = false;
            }

            return speedLimit;
        }


        //public class WayPoints
        //{
        //    public Planet planet { get; set; }
        //    public Ship ship { get; set; }
        //    public Vector2 location { get; set; }
        //}
    }
}