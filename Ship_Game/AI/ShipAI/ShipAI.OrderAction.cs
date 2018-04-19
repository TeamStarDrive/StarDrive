using System;
using System.Linq;
using Microsoft.Xna.Framework;
using Ship_Game.Gameplay;
using Ship_Game.Ships;

namespace Ship_Game.AI {
    public sealed partial class ShipAI
    {
        public void OrderAssaultPlanet(Planet p)
        {
            State = AIState.AssaultPlanet;
            OrbitTarget = p;
            var shipGoal = new ShipGoal(Plan.LandTroop, Vector2.Zero, 0f, OrbitTarget);
            OrderQueue.Clear();
            OrderQueue.Enqueue(shipGoal);
        }

        public void OrderAllStop()
        {
            OrderQueue.Clear();
            lock (WayPointLocker)
            {
                ActiveWayPoints.Clear();
            }
            State = AIState.HoldPosition;
            HasPriorityOrder = false;
            var stop = new ShipGoal(Plan.Stop, Vector2.Zero, 0f);
            OrderQueue.Enqueue(stop);
        }

        public void OrderAttackSpecificTarget(Ship toAttack)
        {
            TargetQueue.Clear();

            if (toAttack == null)
                return;

            if (!Owner.loyalty.IsEmpireAttackable(toAttack.loyalty)) return;
            if (State == AIState.AttackTarget && Target == toAttack)
                return;
            if (State == AIState.SystemDefender && Target == toAttack)
                return;
            if (Owner.Weapons.Count == 0 || Owner.shipData.Role == ShipData.RoleName.troop)
            {
                OrderInterceptShip(toAttack);
                return;
            }
            Intercepting = true;
            lock (WayPointLocker)
            {
                ActiveWayPoints.Clear();
            }
            State = AIState.AttackTarget;
            Target = toAttack;
            Owner.InCombatTimer = 15f;
            OrderQueue.Clear();
            IgnoreCombat = false;
            //HACK. To fix this all the fleet tasks that use attackspecifictarget must be changed 
            //if they also use hold position to keep ships from moving. 
            CombatState = Owner.shipData.CombatState;
            TargetQueue.Add(toAttack);
            HasPriorityTarget = true;
            HasPriorityOrder = false;
            var combat = new ShipGoal(Plan.DoCombat, Vector2.Zero, 0f);
            OrderQueue.Enqueue(combat);
            return;
            OrderInterceptShip(toAttack);
        }

        public void OrderBombardPlanet(Planet toBombard)
        {
            lock (WayPointLocker)
            {
                ActiveWayPoints.Clear();
            }
            State = AIState.Bombard;
            Owner.InCombatTimer = 15f;
            OrderQueue.Clear();
            HasPriorityOrder = true;
            var combat = new ShipGoal(Plan.Bombard, Vector2.Zero, 0f)
            {
                TargetPlanet = toBombard
            };
            OrderQueue.Enqueue(combat);
        }

        public void OrderColonization(Planet toColonize)
        {
            if (toColonize == null)
                return;
            ColonizeTarget = toColonize;
            OrderMoveTowardsPosition(toColonize.Center, 0f, new Vector2(0f, -1f), true, toColonize);
            var colonize = new ShipGoal(Plan.Colonize, toColonize.Center, 0f)
            {
                TargetPlanet = ColonizeTarget
            };
            OrderQueue.Enqueue(colonize);
            State = AIState.Colonize;
        }

        public void OrderDeepSpaceBuild(Goal goal)
        {
            OrderQueue.Clear();
            var pos = goal.BuildPosition;
            OrderMoveTowardsPosition(pos, Owner.Center.RadiansToTarget(pos),
                Owner.Center.DirectionToTarget(pos), true, null);
            var Deploy = new ShipGoal(Plan.DeployStructure, pos,
                Owner.Center.RadiansToTarget(pos))
            {
                goal = goal,
                VariableString = goal.ToBuildUID
            };
            OrderQueue.Enqueue(Deploy);
        }

        public void OrderExplore()
        {
            if (State == AIState.Explore && ExplorationTarget != null)
                return;
            lock (WayPointLocker)
            {
                ActiveWayPoints.Clear();
            }
            OrderQueue.Clear();
            State = AIState.Explore;
            var Explore = new ShipGoal(Plan.Explore, Vector2.Zero, 0f);
            OrderQueue.Enqueue(Explore);
        }

        public void OrderExterminatePlanet(Planet toBombard)
        {
            lock (WayPointLocker)
            {
                ActiveWayPoints.Clear();
            }
            State = AIState.Exterminate;
            OrderQueue.Clear();
            var combat = new ShipGoal(Plan.Exterminate, Vector2.Zero, 0f)
            {
                TargetPlanet = toBombard
            };
            OrderQueue.Enqueue(combat);
        }

        public void OrderFindExterminationTarget(bool ClearOrders)
        {
            if (ExterminationTarget == null || ExterminationTarget.Owner == null)
            {
                var plist = new Array<Planet>();
                foreach (var planetsDict in UniverseScreen.PlanetsDict)
                {
                    if (planetsDict.Value.Owner == null)
                        continue;
                    plist.Add(planetsDict.Value);
                }
                var sortedList =
                    from planet in plist
                    orderby Vector2.Distance(Owner.Center, planet.Center)
                    select planet;
                if (sortedList.Any())
                {
                    ExterminationTarget = sortedList.First<Planet>();
                    OrderExterminatePlanet(ExterminationTarget);
                    return;
                }
            }
            else if (ExterminationTarget != null && OrderQueue.IsEmpty)
            {
                OrderExterminatePlanet(ExterminationTarget);
            }
        }

        public void OrderFormationWarp(Vector2 destination, float facing, Vector2 fvec)
        {
            lock (WayPointLocker)
            {
                ActiveWayPoints.Clear();
            }
            OrderQueue.Clear();
            OrderMoveDirectlyTowardsPosition(destination, facing, fvec, true, Owner.fleet.Speed);
            State = AIState.FormationWarp;
        }

        public void OrderFormationWarpQ(Vector2 destination, float facing, Vector2 fvec)
        {
            OrderMoveDirectlyTowardsPosition(destination, facing, fvec, false, Owner.fleet.Speed);
            State = AIState.FormationWarp;
        }

        public void OrderInterceptShip(Ship toIntercept)
        {
            Intercepting = true;
            lock (WayPointLocker)
            {
                ActiveWayPoints.Clear();
            }
            State = AIState.Intercept;
            Target = toIntercept;
            HasPriorityTarget = true;
            HasPriorityOrder = false;
            OrderQueue.Clear();
        }

        public void OrderLandAllTroops(Planet target)
        {
            if ((Owner.shipData.Role == ShipData.RoleName.troop || Owner.HasTroopBay || Owner.hasTransporter) &&
                Owner.TroopList.Count > 0 && target.GetGroundLandingSpots() > 0)
            {
                HasPriorityOrder = true;
                State = AIState.AssaultPlanet;
                OrbitTarget = target;
                OrderQueue.Clear();
                lock (ActiveWayPoints)
                {
                    ActiveWayPoints.Clear();
                }
                var goal = new ShipGoal(Plan.LandTroop, Vector2.Zero, 0f)
                {
                    TargetPlanet = target
                };
                OrderQueue.Enqueue(goal);
            }
            //else if (this.Owner.BombBays.Count > 0 && target.GetGroundStrength(this.Owner.loyalty) ==0)  //universeScreen.player == this.Owner.loyalty && 
            //{
            //    this.State = AIState.Bombard;
            //    this.OrderBombardTroops(target);
            //}
        }

        public void OrderMoveDirectlyTowardsPosition(Vector2 position, float desiredFacing,
            bool clearOrders) => OrderMoveDirectlyTowardsPosition(position, desiredFacing, Vector2.Zero, clearOrders);

        public void OrderMoveDirectlyTowardsPosition(Vector2 position, float desiredFacing, Vector2 fVec,
            bool ClearOrders)
        {
            Target = null;
            HasPriorityTarget = false;
            Vector2 wantedForward = Owner.Center.DirectionToTarget(position);
            var forward = new Vector2((float) Math.Sin((double) Owner.Rotation),
                -(float) Math.Cos((double) Owner.Rotation));
            var right = new Vector2(-forward.Y, forward.X);
            var angleDiff = (float) Math.Acos((double) Vector2.Dot(wantedForward, forward));
            Vector2.Dot(wantedForward, right);
            if (angleDiff > 0.2f)
                Owner.HyperspaceReturn();
            OrderQueue.Clear();
            if (ClearOrders)
                lock (WayPointLocker)
                {
                    ActiveWayPoints.Clear();
                }
            if (Owner.loyalty == EmpireManager.Player)
                HasPriorityOrder = true;
            State = AIState.MoveTo;
            MovePosition = position;
            lock (WayPointLocker)
            {
                ActiveWayPoints.Enqueue(position);
            }
            //FinalFacingVector = fVec;
            DesiredFacing = desiredFacing;
            lock (WayPointLocker)
            {
                for (var i = 0; i < ActiveWayPoints.Count; i++)
                {
                    Vector2 waypoint = ActiveWayPoints.ToArray()[i];
                    if (i != 0)
                    {
                        var to1k = new ShipGoal(Plan.MoveToWithin1000, waypoint, desiredFacing)
                        {
                            SpeedLimit = Owner.Speed
                        };
                        OrderQueue.Enqueue(to1k);
                    }
                    else
                    {
                        AddShipGoal(Plan.RotateToFaceMovePosition, waypoint, 0f);
                        var to1k = new ShipGoal(Plan.MoveToWithin1000, waypoint, desiredFacing)
                        {
                            SpeedLimit = Owner.Speed
                        };
                        OrderQueue.Enqueue(to1k);
                    }
                    if (i == ActiveWayPoints.Count - 1)
                    {
                        var finalApproach = new ShipGoal(Plan.MakeFinalApproach, waypoint, desiredFacing)
                        {
                            SpeedLimit = Owner.Speed
                        };
                        OrderQueue.Enqueue(finalApproach);
                        var slow = new ShipGoal(Plan.StopWithBackThrust, waypoint, 0f)
                        {
                            SpeedLimit = Owner.Speed
                        };
                        OrderQueue.Enqueue(slow);
                        AddShipGoal(Plan.RotateToDesiredFacing, waypoint, desiredFacing);
                    }
                }
            }
        }

        public void OrderMoveDirectlyTowardsPosition(Vector2 position, float desiredFacing, Vector2 fVec,
            bool ClearOrders, float speedLimit)
        {
            Target = null;
            HasPriorityTarget = false;
            Vector2 wantedForward = Owner.Center.DirectionToTarget(position);
            var forward = new Vector2((float) Math.Sin((double) Owner.Rotation),
                -(float) Math.Cos((double) Owner.Rotation));
            var right = new Vector2(-forward.Y, forward.X);
            var angleDiff = (float) Math.Acos((double) Vector2.Dot(wantedForward, forward));
            Vector2.Dot(wantedForward, right);
            if (angleDiff > 0.2f)
                Owner.HyperspaceReturn();
            OrderQueue.Clear();
            if (ClearOrders)
                lock (WayPointLocker)
                {
                    ActiveWayPoints.Clear();
                }
            if (Owner.loyalty == EmpireManager.Player)
                HasPriorityOrder = true;
            State = AIState.MoveTo;
            MovePosition = position;
            lock (WayPointLocker)
            {
                ActiveWayPoints.Enqueue(position);
            }
            FinalFacingVector = fVec;
            DesiredFacing = desiredFacing;

            Vector2[] waypoints;
            lock (WayPointLocker) waypoints = ActiveWayPoints.ToArray();

            for (int i = 0; i < waypoints.Length; i++)
            {
                Vector2 waypoint = waypoints[i];
                if (i != 0)
                {
                    var to1K = new ShipGoal(Plan.MoveToWithin1000, waypoint, desiredFacing)
                    {
                        SpeedLimit = speedLimit
                    };
                    OrderQueue.Enqueue(to1K);
                }
                else
                {
                    AddShipGoal(Plan.RotateToFaceMovePosition, waypoint, 0f);
                    var to1K = new ShipGoal(Plan.MoveToWithin1000, waypoint, desiredFacing)
                    {
                        SpeedLimit = speedLimit
                    };
                    OrderQueue.Enqueue(to1K);
                }
                if (i == waypoints.Length - 1)
                {
                    var finalApproach = new ShipGoal(Plan.MakeFinalApproach, waypoint, desiredFacing)
                    {
                        SpeedLimit = speedLimit
                    };
                    OrderQueue.Enqueue(finalApproach);
                    var slow = new ShipGoal(Plan.StopWithBackThrust, waypoint, 0f)
                    {
                        SpeedLimit = speedLimit
                    };
                    OrderQueue.Enqueue(slow);
                    AddShipGoal(Plan.RotateToDesiredFacing, waypoint, desiredFacing);
                }
            }
        }

        public void OrderMoveToFleetPosition(Vector2 position, float desiredFacing, Vector2 fVec, bool ClearOrders,
            float SpeedLimit, Fleet fleet)
        {
            SpeedLimit = Owner.Speed;
            if (ClearOrders)
            {
                OrderQueue.Clear();
                lock (WayPointLocker)
                {
                    ActiveWayPoints.Clear();
                }
            }
            State = AIState.MoveTo;
            MovePosition = position;
            FinalFacingVector = fVec;
            DesiredFacing = desiredFacing;
            bool inCombat = Owner.InCombat;
            AddShipGoal(Plan.RotateToFaceMovePosition, MovePosition, 0f);
            var to1k = new ShipGoal(Plan.MoveToWithin1000, MovePosition, desiredFacing)
            {
                SpeedLimit = SpeedLimit,
                fleet = fleet
            };
            OrderQueue.Enqueue(to1k);
            var finalApproach = new ShipGoal(Plan.MakeFinalApproachFleet, MovePosition, desiredFacing)
            {
                SpeedLimit = SpeedLimit,
                fleet = fleet
            };
            OrderQueue.Enqueue(finalApproach);
            AddShipGoal(Plan.RotateInlineWithVelocity, Vector2.Zero, 0f);
            var slow = new ShipGoal(Plan.StopWithBackThrust, position, 0f)
            {
                SpeedLimit = Owner.Speed
            };
            OrderQueue.Enqueue(slow);
            AddShipGoal(Plan.RotateToDesiredFacing, MovePosition, desiredFacing);
        }

        public void OrderMoveTowardsPosition(Vector2 position, float desiredFacing, bool clearOrders, Planet targetPlanet)
            => OrderMoveTowardsPosition(position, desiredFacing, Vector2.Zero, clearOrders, targetPlanet);

        public void OrderMoveTowardsPosition(Vector2 position, float desiredFacing, Vector2 fVec, bool clearOrders,
            Planet targetPlanet)
        {
            DistanceLast = 0f;
            Target = null;
            HasPriorityTarget = false;
            //   Vector2 wantedForward = Owner.Center.FindVectorToTarget(position);
            //         Vector2 forward       = Owner.Rotation.RotationToForwardVec();
            //float angleDiff = (float)Math.Acos(Vector2.Dot(wantedForward, forward));

            //if (angleDiff > 0.2f)
            //    Owner.HyperspaceReturn();
            OrderQueue.Clear();
            if (clearOrders)
                lock (WayPointLocker)
                {
                    ActiveWayPoints.Clear();
                }
            if (UniverseScreen != null && Owner.loyalty == EmpireManager.Player)
                HasPriorityOrder = true;
            State = AIState.MoveTo;
            MovePosition = position;

            PlotCourseToNew(position, ActiveWayPoints.Count > 0 ? ActiveWayPoints.Last() : Owner.Center);
            
            DesiredFacing = desiredFacing;

            Vector2[] waypoints;
            lock (WayPointLocker) waypoints = ActiveWayPoints.ToArray();

            for (int i = 0; i < waypoints.Length; ++i)
            {
                Vector2 waypoint = waypoints[i];
                bool isLast = waypoints.Length - 1 == i;
                Planet p = isLast ? targetPlanet : null;

                if (i != 0)
                {
                    AddShipGoal(Plan.MoveToWithin1000, waypoint, desiredFacing, p, Owner.Speed);
                }
                else
                {
                    AddShipGoal(Plan.RotateToFaceMovePosition, waypoint, 0f);
                    AddShipGoal(Plan.MoveToWithin1000, waypoint, desiredFacing, p, Owner.Speed);
                }
                if (isLast)
                {
                    AddShipGoal(Plan.MakeFinalApproach, waypoint, desiredFacing, p, Owner.Speed);
                    AddShipGoal(Plan.StopWithBackThrust, waypoint, 0f, targetPlanet, Owner.Speed);
                    AddShipGoal(Plan.RotateToDesiredFacing, waypoint, desiredFacing);
                }
            }
        }

        public void OrderOrbitNearest(bool ClearOrders)
        {
            lock (WayPointLocker)
            {
                ActiveWayPoints.Clear();
            }
            Target = null;
            Intercepting = false;
            Owner.HyperspaceReturn();
            if (ClearOrders)
                OrderQueue.Clear();
            var sortedList =
                from toOrbit in Owner.loyalty.GetPlanets()
                orderby Vector2.Distance(Owner.Center, toOrbit.Center)
                select toOrbit;
            if (sortedList.Any())
            {
                var planet = sortedList.First<Planet>();
                OrbitTarget = planet;
                var orbit = new ShipGoal(Plan.Orbit, Vector2.Zero, 0f)
                {
                    TargetPlanet = planet
                };
                ResupplyTarget = planet;
                OrderQueue.Enqueue(orbit);
                State = AIState.Orbit;
                return;
            }

            if (Owner.loyalty.GetOwnedSystems().Any())
            {
                var systemList = from solarsystem in Owner.loyalty.GetOwnedSystems()
                    orderby Owner.Center.SqDist(solarsystem.Position)
                    select solarsystem;
                Planet item = systemList.First().PlanetList[0];
                OrbitTarget = item;
                var orbit = new ShipGoal(Plan.Orbit, Vector2.Zero, 0f)
                {
                    TargetPlanet = item
                };
                ResupplyTarget = item;
                OrderQueue.Enqueue(orbit);
                State = AIState.Orbit;
            }
        }

        public void OrderFlee(bool ClearOrders)
        {
            lock (WayPointLocker)
            {
                ActiveWayPoints.Clear();
            }
            Target = null;
            Intercepting = false;
            Owner.HyperspaceReturn();
            if (ClearOrders)
                OrderQueue.Clear();

            var systemList =
                from solarsystem in Owner.loyalty.GetOwnedSystems()
                where solarsystem.combatTimer <= 0f && Vector2.Distance(solarsystem.Position, Owner.Position) > 200000f
                orderby Vector2.Distance(Owner.Center, solarsystem.Position)
                select solarsystem;
            if (systemList.Any())
            {
                Planet item = systemList.First<SolarSystem>().PlanetList[0];
                OrbitTarget = item;
                var orbit = new ShipGoal(Plan.Orbit, Vector2.Zero, 0f)
                {
                    TargetPlanet = item
                };
                ResupplyTarget = item;
                OrderQueue.Enqueue(orbit);
                State = AIState.Flee;
            }
        }

        public void OrderOrbitPlanet(Planet p)
        {
            lock (WayPointLocker)
            {
                ActiveWayPoints.Clear();
            }
            Target = null;
            Intercepting = false;
            Owner.HyperspaceReturn();
            OrbitTarget = p;
            OrderQueue.Clear();
            var orbit = new ShipGoal(Plan.Orbit, Vector2.Zero, 0f)
            {
                TargetPlanet = p
            };
            ResupplyTarget = p;
            OrderQueue.Enqueue(orbit);
            State = AIState.Orbit;
        }

        public void OrderQueueSpecificTarget(Ship toAttack)
        {
            if (TargetQueue.Count == 0 && Target != null && Target.Active && Target != toAttack)
            {
                OrderAttackSpecificTarget(Target as Ship);
                TargetQueue.Add(Target as Ship);
            }
            if (TargetQueue.Count == 0)
            {
                OrderAttackSpecificTarget(toAttack);
                return;
            }
            if (toAttack == null)
                return;
            //targetting relation
            if (Owner.loyalty.TryGetRelations(toAttack.loyalty, out Relationship relations))
            {
                if (!relations.Treaty_Peace)
                {
                    if (State == AIState.AttackTarget && Target == toAttack)
                        return;
                    if (State == AIState.SystemDefender && Target == toAttack)
                        return;
                    if (Owner.Weapons.Count == 0 || Owner.shipData.Role == ShipData.RoleName.troop)
                    {
                        OrderInterceptShip(toAttack);
                        return;
                    }
                    Intercepting = true;
                    lock (WayPointLocker)
                    {
                        ActiveWayPoints.Clear();
                    }
                    State = AIState.AttackTarget;
                    TargetQueue.Add(toAttack);
                    HasPriorityTarget = true;
                    HasPriorityOrder = false;
                    return;
                }
                OrderInterceptShip(toAttack);
            }
        }

        public void OrderRebase(Planet p, bool ClearOrders)
        {
            lock (WayPointLocker)
            {
                ActiveWayPoints.Clear();
            }
            if (ClearOrders)
                OrderQueue.Clear();
            int troops = Owner.loyalty
                .GetShips()
                .Where(troop => troop.TroopList.Count > 0)
                .Count(
                    troopAi => troopAi.AI.OrderQueue.Any(goal => goal.TargetPlanet != null && goal.TargetPlanet == p));

            if (troops >= p.GetGroundLandingSpots())
            {
                OrderQueue.Clear();
                State = AIState.AwaitingOrders;
                return;
            }

            OrderMoveTowardsPosition(p.Center, 0f, new Vector2(0f, -1f), false, p);
            IgnoreCombat = true;
            var rebase = new ShipGoal(Plan.Rebase, Vector2.Zero, 0f)
            {
                TargetPlanet = p
            };
            OrderQueue.Enqueue(rebase);
            State = AIState.Rebase;
            HasPriorityOrder = true;
        }

        public void OrderRebaseToNearest()
        {
            ////added by gremlin if rebasing dont rebase.
            //if (this.State == AIState.Rebase && this.OrbitTarget.Owner == this.Owner.loyalty)
            //    return;
            lock (WayPointLocker)
            {
                ActiveWayPoints.Clear();
            }

            var sortedList =
                from planet in Owner.loyalty.GetPlanets()
                //added by gremlin if the planet is full of troops dont rebase there. RERC2 I dont think the about looking at incoming troops works.
                where Owner.loyalty.GetShips()
                          .Where(troop => troop.TroopList.Count > 0)
                          .Count(troopAi => troopAi.AI.OrderQueue
                              .Any(goal => goal.TargetPlanet != null && goal.TargetPlanet == planet)) <=
                      planet.GetGroundLandingSpots()
                /*where planet.TroopsHere.Count + this.Owner.loyalty.GetShips()
                .Where(troop => troop.Role == ShipData.RoleName.troop 
                    
                    && troop.GetAI().State == AIState.Rebase 
                    && troop.GetAI().OrbitTarget == planet).Count() < planet.TilesList.Sum(space => space.number_allowed_troops)*/
                orderby Vector2.Distance(Owner.Center, planet.Center)
                select planet;


            if (!sortedList.Any())
            {
                State = AIState.AwaitingOrders;
                return;
            }
            var p = sortedList.First();
            OrderMoveTowardsPosition(p.Center, 0f, new Vector2(0f, -1f), false, p);
            IgnoreCombat = true;
            var rebase = new ShipGoal(Plan.Rebase, Vector2.Zero, 0f)
            {
                TargetPlanet = p
            };


            OrderQueue.Enqueue(rebase);

            State = AIState.Rebase;
            HasPriorityOrder = true;
        }

        public void OrderRefitTo(string toRefit)
        {
            lock (WayPointLocker)
            {
                ActiveWayPoints.Clear();
            }
            HasPriorityOrder = true;
            IgnoreCombat = true;

            OrderQueue.Clear();


            OrbitTarget = Owner.loyalty.FindNearestRallyPoint(Owner.Center);
         
            if (OrbitTarget == null)
            {
                State = AIState.AwaitingOrders;
                return;
            }
            OrderMoveTowardsPosition(OrbitTarget.Center, 0f, Vector2.One, true, OrbitTarget);
            var refit = new ShipGoal(Plan.Refit, Vector2.Zero, 0f)
            {
                TargetPlanet = OrbitTarget,
                VariableString = toRefit
            };
            OrderQueue.Enqueue(refit);
            State = AIState.Refit;
        }

        public void OrderResupply(Planet toOrbit, bool ClearOrders)
        {
            if (ClearOrders)
            {
                OrderQueue.Clear();
                HadPO = true;
            }
            else
            {
                HadPO = false;
            }
            lock (WayPointLocker)
            {
                ActiveWayPoints.Clear();
            }
            Target = null;
            OrbitTarget = toOrbit;
            AwaitClosest = toOrbit;
            OrderMoveTowardsPosition(toOrbit.Center, 0f, Vector2.One, ClearOrders, toOrbit);
            State = AIState.Resupply;
            HasPriorityOrder = true;
        }

        public void OrderResupplyNearest(bool ClearOrders)
        {
            if (Owner.Mothership != null && Owner.Mothership.Active && (Owner.shipData.Role != ShipData.RoleName.supply
                                                                        || Owner.Ordinance > 0 ||
                                                                        Owner.Health / Owner.HealthMax <
                                                                        DmgLevel[(int) Owner.shipData.ShipCategory]))
            {
                OrderReturnToHangar();
                return;
            }
            var shipyards = new Array<Planet>();
            if (Owner.loyalty.isFaction)
                return;
            foreach (Planet planet in Owner.loyalty.GetPlanets())
            {
                if (!planet.HasShipyard || Owner.InCombat && Vector2.Distance(Owner.Center, planet.Center) < 15000f)
                    continue;
                shipyards.Add(planet);
            }
            IOrderedEnumerable<Planet> sortedList = null;
            if (Owner.NeedResupplyTroops)
                sortedList =
                    from p in shipyards
                    orderby p.TroopsHere.Count > Owner.TroopCapacity,
                    Vector2.Distance(Owner.Center, p.Center)
                    select p;
            else
                sortedList =
                    from p in shipyards
                    orderby Vector2.Distance(Owner.Center, p.Center)
                    select p;
            if (sortedList.Count<Planet>() > 0)
                OrderResupply(sortedList.First<Planet>(), ClearOrders);
            else
                OrderFlee(true);
        }

        public void OrderReturnToHangar()
        {
            var g = new ShipGoal(Plan.ReturnToHangar, Vector2.Zero, 0f);

            OrderQueue.Clear();
            OrderQueue.Enqueue(g);

            HasPriorityOrder = true;
            State = AIState.ReturnToHangar;
        }

        public void OrderScrapShip()
        {
#if SHOWSCRUB
//Log.Info(string.Concat(this.Owner.loyalty.PortraitName, " : ", this.Owner.Role)); 
#endif            
            Owner.ClearFleet();
            Owner.loyalty.ForcePoolRemove(Owner);
            if (Owner.shipData.Role <= ShipData.RoleName.station && Owner.ScuttleTimer < 1)
            {
                Owner.ScuttleTimer = 1;
                State = AIState.Scuttle;
                HasPriorityOrder = true;
                Owner.QueueTotalRemoval(); //fbedard
                return;
            }
            lock (WayPointLocker)
            {
                ActiveWayPoints.Clear();
            }
            
            HasPriorityOrder = true;
            IgnoreCombat = true;
            OrderQueue.Clear();
            OrbitTarget = Owner.loyalty.FindNearestRallyPoint(Owner.Center);

            if (OrbitTarget == null)
            {
                Owner.ScuttleTimer = 1;
                State = AIState.Scuttle;
                HasPriorityOrder = true;
                Owner.QueueTotalRemoval();
                return;
            }


            OrderMoveTowardsPosition(OrbitTarget.Center, 0f, Vector2.One, true, OrbitTarget);
            var scrap = new ShipGoal(Plan.Scrap, Vector2.Zero, 0f)
            {
                TargetPlanet = OrbitTarget
            };
            OrderQueue.Enqueue(scrap);
            State = AIState.Scrap;
        }

        private void OrderSupplyShip(Ship tosupply, float ord_amt)
        {
            var g = new ShipGoal(Plan.SupplyShip, Vector2.Zero, 0f);
            EscortTarget = tosupply;
            g.VariableNumber = ord_amt;
            IgnoreCombat = true;
            OrderQueue.Clear();
            OrderQueue.Enqueue(g);
            State = AIState.Ferrying;
        }

        /// <summary>
        /// sysdefense order defend system
        /// </summary>
        /// <param name="system"></param>
        public void OrderSystemDefense(SolarSystem system)
        {
            //if (this.State == AIState.Intercept || this.Owner.InCombatTimer > 0)
            //    return;
            //bool inSystem = true;
            //if (this.Owner.BaseCanWarp && Vector2.Distance(system.Position, this.Owner.Position) / this.Owner.velocityMaximum > 11)
            //    inSystem = false;
            //else 
            //    inSystem = this.Owner.GetSystem() == this.SystemToDefend;
            //if (this.SystemToDefend == null)
            //{
            //    this.HasPriorityOrder = false;
            //    this.SystemToDefend = system;
            //    this.OrderQueue.Clear();
            //}
            //else

            ShipGoal goal = OrderQueue.PeekLast;

            if (SystemToDefend == null || SystemToDefend != system || AwaitClosest == null ||
                AwaitClosest.Owner == null || AwaitClosest.Owner != Owner.loyalty || Owner.System != system &&
                goal != null && OrderQueue.PeekLast.Plan != Plan.DefendSystem)
            {
#if SHOWSCRUB
                if (this.Target != null && (this.Target as Ship).Name == "Subspace Projector")
                    Log.Info(string.Concat("Scrubbed", (this.Target as Ship).Name)); 
#endif
                SystemToDefend = system;
                HasPriorityOrder = false;
                SystemToDefend = system;
                OrderQueue.Clear();
                OrbitTarget = (Planet) null;
                if (SystemToDefend.PlanetList.Count > 0)
                {
                    var Potentials = new Array<Planet>();
                    foreach (Planet p in SystemToDefend.PlanetList)
                    {
                        if (p.Owner == null || p.Owner != Owner.loyalty)
                            continue;
                        Potentials.Add(p);
                    }
                    //if (Potentials.Count == 0)
                    //    foreach (Planet p in this.SystemToDefend.PlanetList)
                    //        if (p.Owner == null)
                    //            Potentials.Add(p);

                    if (Potentials.Count > 0)
                    {
                        AwaitClosest = Potentials[UniverseRandom.InRange(Potentials.Count)];
                        OrderMoveTowardsPosition(AwaitClosest.Center, 0f, Vector2.One, true, null);
                        AddShipGoal(Plan.DefendSystem, Vector2.Zero, 0f);
                        State = AIState.SystemDefender;
                    }
                    else
                    {
                        OrderResupplyNearest(true);
                    }
                }
                //this.OrderQueue.AddLast(new ArtificialIntelligence.ShipGoal(ArtificialIntelligence.Plan.DefendSystem, Vector2.Zero, 0f));
            }

            //this.State = AIState.SystemDefender;                   
        }

        public void OrderThrustTowardsPosition(Vector2 position, float desiredFacing, Vector2 fVec, bool ClearOrders)
        {
            if (ClearOrders)
            {
                OrderQueue.Clear();
                lock (WayPointLocker)
                {
                    ActiveWayPoints.Clear();
                }
            }
            FinalFacingVector = fVec;
            DesiredFacing = desiredFacing;
            lock (WayPointLocker)
            {
                for (var i = 0; i < ActiveWayPoints.Count; i++)
                {
                    Vector2 waypoint = ActiveWayPoints.ToArray()[i];
                    if (i == 0)
                    {
                        AddShipGoal(Plan.RotateInlineWithVelocity, Vector2.Zero, 0f);
                        var stop = new ShipGoal(Plan.Stop, Vector2.Zero, 0f);
                        OrderQueue.Enqueue(stop);
                        AddShipGoal(Plan.RotateToFaceMovePosition, waypoint, 0f);
                        var to1k = new ShipGoal(Plan.MoveToWithin1000, waypoint, desiredFacing)
                        {
                            SpeedLimit = Owner.Speed
                        };
                        OrderQueue.Enqueue(to1k);
                    }
                }
            }
        }

        public void OrderToOrbit(Planet toOrbit, bool ClearOrders)
        {
            if (ClearOrders)
                OrderQueue.Clear();
            HasPriorityOrder = true;
            lock (WayPointLocker)
            {
                ActiveWayPoints.Clear();
            }
            State = AIState.Orbit;
            OrbitTarget = toOrbit;
            if (Owner.shipData.ShipCategory == ShipData.Category.Civilian) //fbedard: civilian ship will use projectors
                OrderMoveTowardsPosition(toOrbit.Center, 0f, new Vector2(0f, -1f), false, toOrbit);
            var orbit = new ShipGoal(Plan.Orbit, Vector2.Zero, 0f)
            {
                TargetPlanet = toOrbit
            };

            OrderQueue.Enqueue(orbit);
        }

        private void AwaitOrders(float elapsedTime)
        {
            if (State != AIState.Resupply)
                HasPriorityOrder = false;
            if (AwaitClosest != null)
            {
                DoOrbit(AwaitClosest, elapsedTime);
                return;
            }
            SolarSystem home = Owner.System;
            if (home == null)
            {
                if (SystemToDefend != null)
                {
                    DoOrbit(SystemToDefend.PlanetList[0], elapsedTime);
                    AwaitClosest = SystemToDefend.PlanetList[0];
                    return;
                }

                if (!Owner.loyalty.isFaction) //for empire find whatever is close. might add to this for better logic. 
                {
                    home = (Owner.loyalty.GetOwnedSystems() as Array<SolarSystem>)
                        .FindMin(s => Owner.Center.SqDist(s.Position));
                }
                else //for factions look for ships in a system so they group up. 
                {
                    home = Owner.loyalty.GetShips()
                        .FindMinFiltered(inSystem => inSystem.System != null,
                            inSystem => Owner.Center.SqDist(inSystem.Center))?.System;
                }

                if (home == null) //Find any system with no owners and planets.
                {
                    home =
                        Empire.Universe.SolarSystemDict.Values.ToArrayList()
                            .FindMinFiltered(o => o.OwnerList.Count == 0 && o.PlanetList.Count > 0,
                                ss => Owner.Center.SqDist(ss.Position));
                }
            }

            if (home != null)
            {
                var closestD = float.MaxValue;
                var closestOurs = false;
                float distance;
                foreach (Planet p in home.PlanetList)
                {
                    if (AwaitClosest == null) AwaitClosest = p;
                    var ours = false;
                    if (Owner.loyalty.isFaction)
                        ours = p.Owner != null || p.Habitable; //for factions it just has to be habitable

                    else ours = p.Owner == Owner.loyalty;

                    if (closestOurs && !ours) // if we already have an owned planet and the current isnt. forget it. 
                        continue;
                    distance = Owner.Center.SqDist(p.Center);

                    if (ours && closestOurs)
                        if (distance >= closestD)
                            continue;

                    closestOurs = true;
                    closestD = distance;
                    AwaitClosest = p;
                }
            }
        }

        private void AwaitOrdersPlayer(float elapsedTime)
        {
            HasPriorityOrder = false;
            if (Owner.InCombatTimer > elapsedTime * -5 && ScanForThreatTimer < 2 - elapsedTime * 5)
                ScanForThreatTimer = 0;
            if (EscortTarget != null)
            {
                State = AIState.Escort;
                return;
            }
            if (!HadPO)
            {
                if (SystemToDefend != null)
                {
                    Planet p = Owner.loyalty.GetGSAI().DefensiveCoordinator.AssignIdleShips(Owner);
                    DoOrbit(p, elapsedTime);
                    AwaitClosest = p;
                    return;
                }
                else
                if (AwaitClosest != null)
                {
                    DoOrbit(AwaitClosest, elapsedTime);
                    return;
                }
                AwaitClosest =
                    Owner.loyalty.GetGSAI()
                        .GetKnownPlanets()
                        .FindMin(
                            planet => planet.Center.SqDist(Owner.Center) + (Owner.loyalty != planet.Owner ? 300000 : 0));
                return;
            }	        
            if (Owner.System?.OwnerList.Contains(Owner.loyalty) ?? false)
            {
                HadPO = false;
                return;
            }
            Stop(elapsedTime);
        }

        public bool ClearOrdersNext;
        public bool HasPriorityOrder;
        public bool HadPO;

        public void SetPriorityOrder()
        {
            OrderQueue.Clear();
            HasPriorityOrder = true;
            Intercepting = false;
            HasPriorityTarget = false;
        }
    }
}