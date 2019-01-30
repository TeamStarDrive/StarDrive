using Microsoft.Xna.Framework;
using Ship_Game.Gameplay;
using Ship_Game.Ships;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Ship_Game.AI
{
    public sealed partial class ShipAI
    {
        public void AddToOrderQueue(ShipGoal goal)
        {
            using (OrderQueue.AcquireWriteLock())
            {
                OrderQueue.Enqueue(goal);
            }
        }

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
            WayPoints.Clear();
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

            if (!Owner.loyalty.IsEmpireAttackable(toAttack.loyalty))
                return;
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
            WayPoints.Clear();
            State = AIState.AttackTarget;
            Target = toAttack;
            Owner.InCombatTimer = 15f;
            OrderQueue.Clear();
            IgnoreCombat = false;
            //HACK. To fix this all the fleet tasks that use attackspecifictarget must be changed
            //if they also use hold position to keep ships from moving.
            if (!Owner.loyalty.isPlayer)
                CombatState = Owner.shipData.CombatState;
            TargetQueue.Add(toAttack);
            HasPriorityTarget = true;
            HasPriorityOrder = false;
            var combat = new ShipGoal(Plan.DoCombat, Vector2.Zero, 0f);
            OrderQueue.Enqueue(combat);
            //return;
            //OrderInterceptShip(toAttack);
        }

        public void OrderBombardPlanet(Planet toBombard)
        {
            WayPoints.Clear();
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
            WayPoints.Clear();
            OrderQueue.Clear();
            State = AIState.Explore;
            var Explore = new ShipGoal(Plan.Explore, Vector2.Zero, 0f);
            OrderQueue.Enqueue(Explore);
        }

        public void OrderExterminatePlanet(Planet toBombard)
        {
            WayPoints.Clear();
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
                foreach (var planetsDict in Empire.Universe.PlanetsDict)
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
                    ExterminationTarget = sortedList.First();
                    OrderExterminatePlanet(ExterminationTarget);
                }
            }
            else if (ExterminationTarget != null && OrderQueue.IsEmpty)
            {
                OrderExterminatePlanet(ExterminationTarget);
            }
        }

        public void OrderFormationWarp(Vector2 destination, float facing, Vector2 fvec)
        {
            WayPoints.Clear();
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
            WayPoints.Clear();
            State = AIState.Intercept;
            Target = toIntercept;
            HasPriorityTarget = true;
            HasPriorityOrder = false;
            OrderQueue.Clear();
        }

        public void OrderLandAllTroops(Planet target)
        {
            if (Owner.Carrier.AnyPlanetAssaultAvailable)
            {
                SetPriorityOrderWithClear();
                ClearWayPoints();
                State = AIState.AssaultPlanet;
                OrbitTarget = target;
                AddToOrderQueue(ShipGoal.CreateLandTroopGoal(target));
            }
        }

        public void OrderMoveDirectlyTowardsPosition(Vector2 position, float desiredFacing,
            bool clearOrders) => OrderMoveDirectlyTowardsPosition(position, desiredFacing, Vector2.Zero, clearOrders);

        public void OrderMoveDirectlyTowardsPosition(Vector2 position, float desiredFacing, Vector2 fVec,
            bool clearOrders)
        {
            Target = null;
            HasPriorityTarget = false;
            Vector2 wantedForward = Owner.Center.DirectionToTarget(position);
            var forward = new Vector2((float) Math.Sin(Owner.Rotation),
                -(float) Math.Cos(Owner.Rotation));
            var right = new Vector2(-forward.Y, forward.X);
            var angleDiff = (float) Math.Acos(Vector2.Dot(wantedForward, forward));
            Vector2.Dot(wantedForward, right);
            if (angleDiff > 0.2f)
                Owner.HyperspaceReturn();
            OrderQueue.Clear();
            if (clearOrders)
                WayPoints.Clear();
            if (Owner.loyalty == EmpireManager.Player)
                HasPriorityOrder = true;
            State = AIState.MoveTo;
            MovePosition = position;
            WayPoints.Enqueue(position);
            //FinalFacingVector = fVec;
            DesiredFacing = desiredFacing;
            lock (WayPoints.WayPointLocker)
            {
                IReadOnlyList<Vector2> waypoints = WayPoints.ToArray();
                for (var i = 0; i < WayPoints.Count(); i++)
                {
                    var waypoint = waypoints[i];

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

                    if (i == WayPoints.Count() - 1)
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
            bool clearOrders, float speedLimit)
        {
            Target = null;
            HasPriorityTarget = false;
            Vector2 wantedForward = Owner.Center.DirectionToTarget(position);
            var forward = new Vector2((float) Math.Sin(Owner.Rotation),
                -(float) Math.Cos(Owner.Rotation));
            var right = new Vector2(-forward.Y, forward.X);
            var angleDiff = (float) Math.Acos(Vector2.Dot(wantedForward, forward));
            Vector2.Dot(wantedForward, right);
            if (angleDiff > 0.2f)
                Owner.HyperspaceReturn();
            OrderQueue.Clear();
            if (clearOrders)
                WayPoints.Clear();

            if (Owner.loyalty == EmpireManager.Player)
                HasPriorityOrder = true;
            State = AIState.MoveTo;
            MovePosition = position;
            WayPoints.Enqueue(position);
            DesiredFacing = desiredFacing;

            var waypoints = WayPoints.ToArray();

            for (int i = 0; i < waypoints.Count; i++)
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

                if (i != waypoints.Count - 1) continue;

                var finalApproach = new ShipGoal(Plan.MakeFinalApproach, waypoint, desiredFacing)
                {
                    SpeedLimit = speedLimit
                };
                OrderQueue.Enqueue(finalApproach);
                ShipGoal slow = new ShipGoal(Plan.StopWithBackThrust, waypoint, 0f)
                {
                    SpeedLimit = speedLimit
                };
                OrderQueue.Enqueue(slow);
                AddShipGoal(Plan.RotateToDesiredFacing, waypoint, desiredFacing);
            }
        }

        public void OrderMoveTowardsPosition(Vector2 position, float desiredFacing, bool clearOrders, Planet targetPlanet)
            => OrderMoveTowardsPosition(position, desiredFacing, Vector2.Zero, clearOrders, targetPlanet);

        public void OrderMoveTowardsPosition(Vector2 position, float desiredFacing, Vector2 fVec, bool clearOrders,
            Planet targetPlanet)
        {
            DistanceLast = 0f;
            Target = null;
            HasPriorityTarget = false;
            OrderQueue.Clear();
            if (clearOrders)
                WayPoints.Clear();

            if (Empire.Universe != null && Owner.loyalty == EmpireManager.Player)
                HasPriorityOrder = true;
            State = AIState.MoveTo;
            MovePosition = position;

            PlotCourseToNew(position, WayPoints.Count() > 0 ? WayPoints.Last() : Owner.Center);
            DesiredFacing = desiredFacing;
            CreateFullMovementGoals(desiredFacing, targetPlanet);
        }

        void CreateFullMovementGoals(float desiredFacing, Planet targetPlanet)
        {
            var waypoints = WayPoints.ToArray();

            for (int i = 0; i < waypoints.Count; ++i)
            {
                Vector2 wp = waypoints[i];
                bool isLast = waypoints.Count - 1 == i;
                Planet p = isLast ? targetPlanet : null;

                if (i != 0)
                {
                    AddShipGoal(Plan.MoveToWithin1000, wp, desiredFacing, p, Owner.Speed);
                }
                else
                {
                    AddShipGoal(Plan.RotateToFaceMovePosition, wp, 0f);
                    AddShipGoal(Plan.MoveToWithin1000, wp, desiredFacing, p, Owner.Speed);
                }

                if (isLast)
                {
                    AddShipGoal(Plan.MakeFinalApproach, wp, desiredFacing, p, Owner.Speed);
                    AddShipGoal(Plan.StopWithBackThrust, wp, 0f, targetPlanet, Owner.Speed);
                    AddShipGoal(Plan.RotateToDesiredFacing, wp, desiredFacing);
                }
            }
        }

        public void OrderOrbitNearest(bool clearOrders)
        {
            WayPoints.Clear();

            Target = null;
            Intercepting = false;
            Owner.HyperspaceReturn();
            if (clearOrders)
                OrderQueue.Clear();

            Planet closest = Owner.loyalty.GetPlanets().FindMin(p => p.Center.SqDist(Owner.Center));
            if (closest != null)
            {
                ResupplyTarget = OrbitTarget = closest;
                State = AIState.Orbit;
                OrderQueue.Enqueue(new ShipGoal(Plan.Orbit, Vector2.Zero, 0f) { TargetPlanet = OrbitTarget });
                return;
            }

            SolarSystem closestSystem = Owner.loyalty.GetOwnedSystems().FindMin(s => s.Position.SqDist(Owner.Center));
            if (closestSystem != null)
            {
                ResupplyTarget = OrbitTarget = closestSystem.PlanetList[0];
                State = AIState.Orbit;
                OrderQueue.Enqueue(new ShipGoal(Plan.Orbit, Vector2.Zero, 0f) { TargetPlanet = OrbitTarget });
                return;
            }

            var emergencyPlanet = Empire.Universe.PlanetsDict.Values.ToArray().Filter(p => p.Owner == null);
            emergencyPlanet.Sort(p => p.Center.SqDist(Owner.Center));
            OrbitTarget = emergencyPlanet[0];
        }

        public void OrderFlee(bool clearOrders)
        {
            WayPoints.Clear();

            Target = null;
            Intercepting = false;
            Owner.HyperspaceReturn();
            if (clearOrders)
                OrderQueue.Clear();

            var systemList = (
                from sys in Owner.loyalty.GetOwnedSystems()
                where sys.combatTimer <= 0f && sys.Position.Distance(Owner.Position) > (sys.Radius*1.5f)
                orderby Owner.Center.Distance(sys.Position)
                select sys).ToArray();

            if (systemList.Length > 0)
            {
                Planet item = systemList[0].PlanetList[0];
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
            WayPoints.Clear();

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

                    WayPoints.Clear();

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

            WayPoints.Clear();

            if (ClearOrders)
                OrderQueue.Clear();
            int troops = Owner.loyalty
                .GetShips()
                //.Where(troop => troop.TroopList.Count > 0)
                .Count(
                    troopAi => troopAi.TroopList.Count > 0  && troopAi.AI.OrderQueue.Any(goal => goal.TargetPlanet != null && goal.TargetPlanet == p));
            int ptCount = p.GetGroundLandingSpots();
            if (troops >= ptCount )
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
            WayPoints.Clear();

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

            WayPoints.Clear();

            HasPriorityOrder = true;
            IgnoreCombat = true;

            OrderQueue.Clear();


            OrbitTarget = Owner.loyalty.RallyShipYardNearestTo(Owner.Center);

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

        public void OrderResupply(Planet toOrbit, bool clearOrders)
        {
            SetPriorityOrder(clearOrders);
            HadPO = clearOrders;
            ClearWayPoints();

            Target           = null;
            OrbitTarget      = toOrbit;
            AwaitClosest     = toOrbit;
            OrderMoveTowardsPosition(toOrbit.Center, 0f, Vector2.One, clearOrders, toOrbit);
            State            = AIState.Resupply;
        }

        public void OrderReturnToHangar()
        {
            var g = new ShipGoal(Plan.ReturnToHangar, Vector2.Zero, 0f);

            OrderQueue.Clear();
            OrderQueue.Enqueue(g);

            HasPriorityOrder = true;
            State = AIState.ReturnToHangar;
        }

        public void OrderReturnHome()
        {
            var g = new ShipGoal(Plan.ReturnHome, Vector2.Zero, 0f);

            OrderQueue.Clear();
            OrderQueue.Enqueue(g);

            HasPriorityOrder = true;
            State = AIState.ReturnHome;
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

            WayPoints.Clear();

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
                OrbitTarget = null;
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
                        GoOrbitNearestPlanetAndResupply(true);
                }
                //this.OrderQueue.AddLast(new ArtificialIntelligence.ShipGoal(ArtificialIntelligence.Plan.DefendSystem, Vector2.Zero, 0f));
            }

            //this.State = AIState.SystemDefender;
        }

        public void GoOrbitNearestPlanetAndResupply(bool cancelOrders)
        {
            Planet nearestRallyPoint = Owner.loyalty.RallyShipYardNearestTo(Owner.Center);
            DecideWhereToResupply(nearestRallyPoint, cancelOrders: cancelOrders);
        }

        public void OrderThrustTowardsPosition(Vector2 position, float desiredFacing, Vector2 fVec, bool ClearOrders)
        {
            if (ClearOrders)
            {
                State = AIState.AwaitingOrders;
                OrderQueue.Clear();
                WayPoints.Clear();
            }
            DesiredFacing = desiredFacing;
            OrderMoveTowardsPosition(position, desiredFacing, true, null);
        }

        public void OrderToOrbit(Planet toOrbit, bool ClearOrders)
        {
            if (ClearOrders)
                OrderQueue.Clear();
            HasPriorityOrder = true;

            WayPoints.Clear();

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

        void AwaitOrders(float elapsedTime)
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

        void AwaitOrdersPlayer(float elapsedTime)
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
                    Planet p = Owner.loyalty.GetEmpireAI().DefensiveCoordinator.AssignIdleShips(Owner);
                    DoOrbit(p, elapsedTime);
                    AwaitClosest = p;
                    return;
                }

                if (AwaitClosest != null)
                {
                    DoOrbit(AwaitClosest, elapsedTime);
                    return;
                }
                AwaitClosest =
                    Owner.loyalty.GetEmpireAI()
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
        public void ClearPriorityOrder()
        {
            HasPriorityOrder = false;
            Intercepting = false;
            HasPriorityTarget = false;
        }
        public void SetPriorityOrderWithClear() => SetPriorityOrder(true);
        public void SetPriorityOrder(bool clearOrders)
        {
            if (clearOrders)
                OrderQueue.Clear();
            HasPriorityOrder = true;
            Intercepting = false;
            HasPriorityTarget = false;
        }
    }
}