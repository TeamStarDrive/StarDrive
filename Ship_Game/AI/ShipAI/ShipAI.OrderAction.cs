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
            OrbitTarget = planetToAssault;
            ClearOrders(AIState.AssaultPlanet);
            AddShipGoal(Plan.LandTroop, planetToAssault);
        }

        public void OrderAllStop()
        {
            ClearWayPoints();
            ClearOrders(AIState.HoldPosition);
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
            ClearWayPoints();
            Target = toAttack;
            Owner.InCombatTimer = 15f;
            IgnoreCombat = false;
            //HACK. To fix this all the fleet tasks that use attackspecifictarget must be changed
            //if they also use hold position to keep ships from moving.
            if (!Owner.loyalty.isPlayer)
                CombatState = Owner.shipData.CombatState;
            TargetQueue.Add(toAttack);
            HasPriorityTarget = true;
            ClearOrders(AIState.AttackTarget);
            AddShipGoal(Plan.DoCombat);
        }

        public void OrderBombardPlanet(Planet toBombard)
        {
            Owner.InCombatTimer = 15f;
            ClearWayPoints();
            ClearOrders(AIState.Bombard, priority: true);
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
            ClearOrders(State, priority:true);
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
            ClearWayPoints();
            ClearOrders(AIState.Explore);
            AddShipGoal(Plan.Explore);
        }

        public void OrderExterminatePlanet(Planet toBombard)
        {
            ClearWayPoints();
            ClearOrders(AIState.Exterminate);
            AddShipGoal(Plan.Exterminate, toBombard);
            ExterminationTarget = toBombard;
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
            ClearWayPoints();
            ClearOrders();
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
            ClearWayPoints();
            ClearOrders(AIState.Intercept);
            Intercepting = true;
            Target = toIntercept;
            HasPriorityTarget = true;
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
                Owner.HyperspaceReturn(); // return from hyperspace if rotation needed

            if (clearOrders)
                ClearWayPoints();

            ClearOrders(AIState.MoveTo, Owner.loyalty == EmpireManager.Player);
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

            if (Owner.RotationNeededForTarget(position, 0.2f))
                Owner.HyperspaceReturn(); // return from hyperspace if rotation needed

            if (clearOrders)
                ClearWayPoints();

            ClearOrders(AIState.MoveTo, Owner.loyalty == EmpireManager.Player);
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

            if (clearOrders)
                ClearWayPoints();
            ClearOrders(AIState.MoveTo, Owner.loyalty == EmpireManager.Player);
            
            MovePosition = position;
            PlotCourseAsWayPoints(startPos: (WayPoints.Count > 0 ? WayPoints.PeekLast : Owner.Center), endPos: position);
            DesiredDirection = finalDirection;
            GenerateOrdersFromWayPoints(finalDirection, targetPlanet);
        }

        void GenerateOrdersFromWayPoints(Vector2 finalDirection, Planet targetPlanet)
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
            ClearWayPoints();

            Target = null;
            Intercepting = false;
            Owner.HyperspaceReturn();
            if (clearOrders)
                ClearOrders(State, HasPriorityOrder);

            Planet closest = Owner.loyalty.GetPlanets().FindMin(p => p.Center.SqDist(Owner.Center));
            if (closest != null)
            {
                ResupplyTarget = OrbitTarget = closest;
                State = AIState.Orbit;
                AddShipGoal(Plan.Orbit, OrbitTarget);
                return;
            }

            SolarSystem closestSystem = Owner.loyalty.GetOwnedSystems().FindMin(s => s.Position.SqDist(Owner.Center));
            if (closestSystem != null)
            {
                ResupplyTarget = OrbitTarget = closestSystem.PlanetList[0];
                State = AIState.Orbit;
                AddShipGoal(Plan.Orbit, OrbitTarget);
                return;
            }

            var emergencyPlanet = Empire.Universe.PlanetsDict.Values.ToArray().Filter(p => p.Owner == null);
            emergencyPlanet.Sort(p => p.Center.SqDist(Owner.Center));
            OrbitTarget = emergencyPlanet[0];
        }

        public void OrderFlee(bool clearOrders)
        {
            ClearWayPoints();

            Target = null;
            Intercepting = false;
            Owner.HyperspaceReturn();
            if (clearOrders)
                ClearOrders(State, HasPriorityOrder);

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
                AddShipGoal(Plan.Orbit, item);
                State = AIState.Flee;
            }
        }

        public void OrderOrbitPlanet(Planet p)
        {
            ClearWayPoints();

            Target = null;
            Intercepting = false;
            Owner.HyperspaceReturn();
            OrbitTarget = p;
            ResupplyTarget = p;
            ClearOrders(AIState.Orbit);
            AddShipGoal(Plan.Orbit, p);
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

                    ClearWayPoints();

                    State = AIState.AttackTarget;
                    TargetQueue.Add(toAttack);
                    HasPriorityTarget = true;
                    HasPriorityOrder = false;
                    return;
                }
                OrderInterceptShip(toAttack);
            }
        }

        public void OrderRebase(Planet p, bool clearOrders)
        {
            ClearWayPoints();
            if (clearOrders)
                ClearOrders();

            int troops = Owner.loyalty.GetShips()
                .Count(ship => ship.TroopList.Count > 0 && ship.AI.OrderQueue.Any(goal => goal.TargetPlanet != null && goal.TargetPlanet == p));
            if (troops >= p.GetGroundLandingSpots())
            {
                ClearOrders();
                return;
            }

            OrderMoveTowardsPosition(p.Center, Vectors.Up, false, p);
            IgnoreCombat = true;
            AddShipGoal(Plan.Rebase, p);
            State = AIState.Rebase;
            HasPriorityOrder = true;
        }

        public void OrderRebaseToNearest()
        {
            ClearWayPoints();

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

            AddShipGoal(Plan.Rebase, p);

            State = AIState.Rebase;
            HasPriorityOrder = true;
        }

        public void OrderRefitTo(string toRefit)
        {
            ClearWayPoints();
            IgnoreCombat = true;
            OrbitTarget = Owner.loyalty.RallyShipYardNearestTo(Owner.Center);
            if (OrbitTarget == null)
            {
                ClearOrders();
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

            Target       = null;
            OrbitTarget  = toOrbit;
            AwaitClosest = toOrbit;
            OrderMoveTowardsPosition(toOrbit.Center, Vectors.Up, clearOrders, toOrbit);
            State        = AIState.Resupply;
        }

        public void OrderReturnToHangar()
        {
            ClearOrders(AIState.ReturnToHangar, priority:true);
            AddShipGoal(Plan.ReturnToHangar);
        }

        public void OrderReturnHome()
        {
            ClearOrders(AIState.ReturnHome, priority:true);
            AddShipGoal(Plan.ReturnHome);
        }

        public void OrderScrapShip()
        {
            Owner.ClearFleet();
            Owner.loyalty.ForcePoolRemove(Owner);
            if (Owner.shipData.Role <= ShipData.RoleName.station && Owner.ScuttleTimer < 1)
            {
                Owner.ScuttleTimer = 1;
                ClearOrders(AIState.Scuttle, priority:true);
                Owner.QueueTotalRemoval(); // fbedard
                return;
            }

            ClearOrders();

            IgnoreCombat = true;
            OrbitTarget = Owner.loyalty.FindNearestRallyPoint(Owner.Center);
            if (OrbitTarget == null)
            {
                Owner.ScuttleTimer = 1;
                ClearOrders(AIState.Scuttle, priority:true);
                Owner.QueueTotalRemoval();
                return;
            }

            OrderMoveTowardsPosition(OrbitTarget.Center, Vectors.Up, true, OrbitTarget);
            AddShipGoal(Plan.Scrap, OrbitTarget);
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
                ClearOrders(State);
                OrbitTarget = null;
                if (SystemToDefend.PlanetList.Count > 0)
                {
                    var potentials = new Array<Planet>();
                    foreach (Planet p in SystemToDefend.PlanetList)
                    {
                        if (p.Owner == null || p.Owner != Owner.loyalty)
                            continue;
                        potentials.Add(p);
                    }
                    if (potentials.Count > 0)
                    {
                        AwaitClosest = potentials[UniverseRandom.InRange(potentials.Count)];
                        OrderMoveTowardsPosition(AwaitClosest.Center, Vectors.Up, true, null);
                        AddShipGoal(Plan.DefendSystem);
                        State = AIState.SystemDefender;
                    }
                    else
                        GoOrbitNearestPlanetAndResupply(true);
                }
            }
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
                ClearWayPoints();
                ClearOrders();
            }
            DesiredDirection = direction;
            OrderMoveTowardsPosition(position, direction, true, null);
        }

        public void OrderToOrbit(Planet toOrbit)
        {
            ClearWayPoints();
            ClearOrders(AIState.Orbit, priority:true);
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

                if (!Owner.loyalty.isFaction) // for empire find whatever is close. might add to this for better logic.
                {
                    home = Owner.loyalty.GetOwnedSystems()
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
                    home = Empire.Universe.SolarSystemDict.Values.ToArrayList()
                        .FindMinFiltered(o => o.OwnerList.Count == 0 && o.PlanetList.Count > 0,
                            ss => Owner.Center.SqDist(ss.Position));
                }
            }

            if (home != null)
            {
                AwaitClosest = home.PlanetList.FindMinFiltered(p =>
                {
                    if (Owner.loyalty.isFaction)
                        return p.Owner != null || p.Habitable; // for factions it just has to be habitable
                    return p.Owner == Owner.loyalty; // our empire owns this planet
                }, p => Owner.Center.SqDist(p.Center)); // pick closest
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
            ReverseThrustUntilStopped(elapsedTime);
        }


    }
}