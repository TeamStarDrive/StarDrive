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
        public void OrderAssaultPlanet(Planet planetToAssault)
        {
            State = AIState.AssaultPlanet;
            OrbitTarget = planetToAssault;
            OrderQueue.Clear();
            AddShipGoal(Plan.LandTroop, planetToAssault);
        }

        public void OrderAllStop()
        {
            OrderQueue.Clear();
            WayPoints.Clear();
            State = AIState.HoldPosition;
            HasPriorityOrder = false;
            AddShipGoal(Plan.Stop);
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
            AddShipGoal(Plan.DoCombat);
        }

        public void OrderBombardPlanet(Planet toBombard)
        {
            WayPoints.Clear();
            State = AIState.Bombard;
            Owner.InCombatTimer = 15f;
            OrderQueue.Clear();
            HasPriorityOrder = true;
            AddShipGoal(Plan.Bombard, toBombard);
        }

        public void OrderColonization(Planet toColonize)
        {
            if (toColonize == null)
                return;
            ColonizeTarget = toColonize;
            OrderMoveTowardsPosition(toColonize.Center, Vectors.Up, true, toColonize);
            AddShipGoal(Plan.Colonize, toColonize.Center, toColonize);
            State = AIState.Colonize;
        }

        public void OrderDeepSpaceBuild(Goal goal)
        {
            OrderQueue.Clear();
            Vector2 pos = goal.BuildPosition;
            Vector2 dir = Owner.Center.DirectionToTarget(pos);
            OrderMoveTowardsPosition(pos, dir, true, null);
            AddToOrderQueue(new ShipGoal(Plan.DeployStructure, pos, dir)
            {
                goal = goal,
                VariableString = goal.ToBuildUID
            });
        }

        public void OrderExplore()
        {
            if (State == AIState.Explore && ExplorationTarget != null)
                return;
            WayPoints.Clear();
            OrderQueue.Clear();
            State = AIState.Explore;
            AddShipGoal(Plan.Explore);
        }

        public void OrderExterminatePlanet(Planet toBombard)
        {
            WayPoints.Clear();
            OrderQueue.Clear();
            ExterminationTarget = toBombard;
            State = AIState.Exterminate;
            AddShipGoal(Plan.Exterminate, toBombard);
        }

        public void OrderFindExterminationTarget()
        {
            if (ExterminationTarget?.Owner == null)
            {
                var plist = new Array<Planet>();
                foreach (var planetsDict in Empire.Universe.PlanetsDict)
                {
                    if (planetsDict.Value.Owner == null)
                        continue;
                    plist.Add(planetsDict.Value);
                }

                Planet closest = plist.FindMin(p => Owner.Center.SqDist(p.Center));
                if (closest != null)
                {
                    OrderExterminatePlanet(closest);
                }
            }
            else if (ExterminationTarget != null && OrderQueue.IsEmpty)
            {
                OrderExterminatePlanet(ExterminationTarget);
            }
        }

        public void OrderFormationWarp(Vector2 destination, Vector2 fvec)
        {
            WayPoints.Clear();
            OrderQueue.Clear();
            OrderMoveDirectlyTowardsPosition(destination, fvec, true, Owner.fleet.Speed);
            State = AIState.FormationWarp;
        }

        public void OrderFormationWarpQ(Vector2 destination, Vector2 fvec)
        {
            OrderMoveDirectlyTowardsPosition(destination, fvec, false, Owner.fleet.Speed);
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
                State = AIState.AssaultPlanet;
                OrbitTarget = target;
                AddToOrderQueue(ShipGoal.CreateLandTroopGoal(target));
            }
        }

        public void OrderMoveDirectlyTowardsPosition(Vector2 position, float desiredFacing, bool clearOrders)
        {
            OrderMoveDirectlyTowardsPosition(position, desiredFacing.RadiansToDirection(), clearOrders);
        }

        public void OrderMoveDirectlyTowardsPosition(Vector2 position, Vector2 fVec, bool clearOrders)
        {
            Target = null;
            HasPriorityTarget = false;

            if (Owner.RotationNeededForTarget(position, 0.2f))
            {
                Owner.HyperspaceReturn();
            }

            OrderQueue.Clear();
            if (clearOrders)
                WayPoints.Clear();
            if (Owner.loyalty == EmpireManager.Player)
                HasPriorityOrder = true;
            State = AIState.MoveTo;
            MovePosition = position;
            DesiredDirection = fVec;
            WayPoints.Enqueue(position);

            Vector2[] wayPoints = WayPoints.ToArray();
            for (int i = 0; i < wayPoints.Length; i++)
            {
                Vector2 wp = wayPoints[i];
                if (i == 0)
                {
                    AddShipGoal(Plan.RotateToFaceMovePosition, wp, Vectors.Up);
                }

                AddShipGoal(Plan.MoveToWithin1000, wp, fVec, null, Owner.Speed);

                if (i == WayPoints.Count - 1)
                {
                    AddShipGoal(Plan.MakeFinalApproach, wp, fVec, null, Owner.Speed);
                    AddShipGoal(Plan.StopWithBackThrust, wp, fVec, null, Owner.Speed);
                    AddShipGoal(Plan.RotateToDesiredFacing, wp, fVec);
                }
            }
        }

        // @todo Completely refactor this
        public void OrderMoveDirectlyTowardsPosition(Vector2 position, Vector2 fVec, bool clearOrders, float speedLimit)
        {
            Target = null;
            HasPriorityTarget = false;
            float angleDiff = Owner.AngleDifferenceToPosition(position);
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
            DesiredDirection = fVec;

            Vector2[] wayPoints = WayPoints.ToArray();

            for (int i = 0; i < wayPoints.Length; i++)
            {
                Vector2 pos = wayPoints[i];
                if (i == 0)
                    AddShipGoal(Plan.RotateToFaceMovePosition, pos, Vectors.Up);
                AddShipGoal(Plan.MoveToWithin1000, pos, fVec, null, speedLimit);

                if (i == wayPoints.Length - 1)
                {
                    AddShipGoal(Plan.MakeFinalApproach, pos, fVec, null, speedLimit);
                    AddShipGoal(Plan.StopWithBackThrust, pos, fVec, null, speedLimit);
                    AddShipGoal(Plan.RotateToDesiredFacing, pos, fVec);
                }
            }
        }

        public void OrderMoveTowardsPosition(Vector2 position, Vector2 finalDirection, bool clearOrders, Planet targetPlanet)
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

            PlotCourseToNew(position, WayPoints.Count > 0 ? WayPoints.PeekLast : Owner.Center);
            DesiredDirection = finalDirection;
            CreateFullMovementGoals(finalDirection, targetPlanet);
        }

        void CreateFullMovementGoals(Vector2 finalDirection, Planet targetPlanet)
        {
            Vector2[] wayPoints = WayPoints.ToArray();

            for (int i = 0; i < wayPoints.Length; ++i)
            {
                Vector2 wp = wayPoints[i];
                bool isLast = wayPoints.Length - 1 == i;
                Planet p = isLast ? targetPlanet : null;

                if (i == 0)
                    AddShipGoal(Plan.RotateToFaceMovePosition, wp, Vector2.Zero);
                AddShipGoal(Plan.MoveToWithin1000, wp, finalDirection, p, Owner.Speed);

                if (isLast)
                {
                    AddShipGoal(Plan.MakeFinalApproach, wp, finalDirection, p, Owner.Speed);
                    AddShipGoal(Plan.StopWithBackThrust, wp, Vector2.Zero, targetPlanet, Owner.Speed);
                    AddShipGoal(Plan.RotateToDesiredFacing, wp, finalDirection);
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
                OrderQueue.Enqueue(new ShipGoal(Plan.Orbit) { TargetPlanet = OrbitTarget });
                return;
            }

            SolarSystem closestSystem = Owner.loyalty.GetOwnedSystems().FindMin(s => s.Position.SqDist(Owner.Center));
            if (closestSystem != null)
            {
                ResupplyTarget = OrbitTarget = closestSystem.PlanetList[0];
                State = AIState.Orbit;
                OrderQueue.Enqueue(new ShipGoal(Plan.Orbit) { TargetPlanet = OrbitTarget });
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
                ResupplyTarget = item;
                OrderQueue.Enqueue(new ShipGoal(Plan.Orbit)
                {
                    TargetPlanet = item
                });
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
            var orbit = new ShipGoal(Plan.Orbit)
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

            OrderMoveTowardsPosition(p.Center, Vectors.Up, false, p);
            IgnoreCombat = true;
            OrderQueue.Enqueue(new ShipGoal(Plan.Rebase)
            {
                TargetPlanet = p
            });
            State = AIState.Rebase;
            HasPriorityOrder = true;
        }

        public void OrderRebaseToNearest()
        {
            WayPoints.Clear();

            var sortedList =
                from planet in Owner.loyalty.GetPlanets()
                // added by gremlin if the planet is full of troops dont rebase there.
                // RERC2 I dont think the about looking at incoming troops works.
                where Owner.loyalty.GetShips()
                          .Where(troop => troop.TroopList.Count > 0)
                          .Count(troopAi => troopAi.AI.OrderQueue
                              .Any(goal => goal.TargetPlanet != null && goal.TargetPlanet == planet)) <=
                      planet.GetGroundLandingSpots()
                orderby Vector2.Distance(Owner.Center, planet.Center)
                select planet;

            Planet p = sortedList.FirstOrDefault();
            if (p == null)
            {
                State = AIState.AwaitingOrders;
                return;
            }
            OrderMoveTowardsPosition(p.Center, Vectors.Up, false, p);
            IgnoreCombat = true;
            var rebase = new ShipGoal(Plan.Rebase)
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
            OrderMoveTowardsPosition(OrbitTarget.Center, Vectors.Up, true, OrbitTarget);
            OrderQueue.Enqueue(new ShipGoal(Plan.Refit)
            {
                TargetPlanet = OrbitTarget,
                VariableString = toRefit
            });
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
            OrderMoveTowardsPosition(toOrbit.Center, Vectors.Up, clearOrders, toOrbit);
            State            = AIState.Resupply;
        }

        public void OrderReturnToHangar()
        {
            var g = new ShipGoal(Plan.ReturnToHangar);

            OrderQueue.Clear();
            OrderQueue.Enqueue(g);

            HasPriorityOrder = true;
            State = AIState.ReturnToHangar;
        }

        public void OrderReturnHome()
        {
            var g = new ShipGoal(Plan.ReturnHome);

            OrderQueue.Clear();
            OrderQueue.Enqueue(g);

            HasPriorityOrder = true;
            State = AIState.ReturnHome;
        }

        public void OrderScrapShip()
        {
            Owner.ClearFleet();
            Owner.loyalty.ForcePoolRemove(Owner);
            if (Owner.shipData.Role <= ShipData.RoleName.station && Owner.ScuttleTimer < 1)
            {
                Owner.ScuttleTimer = 1;
                State = AIState.Scuttle;
                HasPriorityOrder = true;
                Owner.QueueTotalRemoval(); // fbedard
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

            OrderMoveTowardsPosition(OrbitTarget.Center, Vectors.Up, true, OrbitTarget);
            OrderQueue.Enqueue(new ShipGoal(Plan.Scrap)
            {
                TargetPlanet = OrbitTarget
            });
            State = AIState.Scrap;
        }

        public void OrderSystemDefense(SolarSystem system)
        {
            ShipGoal goal = OrderQueue.PeekLast;

            if (SystemToDefend == null || SystemToDefend != system || AwaitClosest == null ||
                AwaitClosest.Owner == null || AwaitClosest.Owner != Owner.loyalty || Owner.System != system &&
                goal != null && OrderQueue.PeekLast.Plan != Plan.DefendSystem)
            {
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
                        OrderMoveTowardsPosition(AwaitClosest.Center, Vectors.Up, true, null);
                        AddShipGoal(Plan.DefendSystem);
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

        public void OrderThrustTowardsPosition(Vector2 position, Vector2 direction, bool clearOrders)
        {
            if (clearOrders)
            {
                State = AIState.AwaitingOrders;
                OrderQueue.Clear();
                WayPoints.Clear();
            }
            DesiredDirection = direction;
            OrderMoveTowardsPosition(position, direction, true, null);
        }

        public void OrderToOrbit(Planet toOrbit, bool clearOrders)
        {
            if (clearOrders)
                OrderQueue.Clear();
            HasPriorityOrder = true;
            WayPoints.Clear();

            State = AIState.Orbit;
            OrbitTarget = toOrbit;

            // fbedard: civilian ship will use projectors
            if (Owner.shipData.ShipCategory == ShipData.Category.Civilian)
                OrderMoveTowardsPosition(toOrbit.Center, Vectors.Up, false, toOrbit);

            AddShipGoal(Plan.Orbit, toOrbit);
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
                AwaitClosest = Owner.loyalty.GetEmpireAI().GetKnownPlanets()
                    .FindMin(p => p.Center.SqDist(Owner.Center) + (Owner.loyalty != p.Owner ? 300000 : 0));
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
        public void SetPriorityOrderWithClear()
        {
            SetPriorityOrder(true);
            ClearWayPoints();
        }
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