using Microsoft.Xna.Framework;
using Ship_Game.Gameplay;
using Ship_Game.Ships;
using System.Linq;

namespace Ship_Game.AI
{
    public sealed partial class ShipAI
    {
        public void OrderAllStop()
        {
            ClearWayPoints();
            ClearOrders(AIState.HoldPosition);
            AddShipGoal(Plan.Stop);
        }

        public void OrderHoldPosition(Vector2 position, Vector2 direction)
        {
            AddShipGoal(Plan.HoldPosition, position, direction);
            HasPriorityOrder = true;
            IgnoreCombat     = true;
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
            ClearOrdersAndWayPoints();
            AddBombPlanetGoal(toBombard);
        }

        public void OrderColonization(Planet toColonize, Goal g = null)
        {
            if (toColonize == null)
                return;
            ColonizeTarget = toColonize;
            OrderMoveTo(toColonize.Center, Vectors.Up, true, toColonize);
            AddShipGoal(Plan.Colonize, toColonize.Center, Vectors.Up, toColonize, g);
            State = AIState.Colonize;
        }

        public void OrderDeepSpaceBuild(Goal goal)
        {
            ClearOrders(State, priority:true);
            Vector2 pos = goal.BuildPosition;
            Vector2 dir = Owner.Center.DirectionToTarget(pos);
            OrderMoveTo(pos, dir, true, goal.PlanetBuildingAt, goal);
            if (goal.type == GoalType.DeepSpaceConstruction) // deep space structures
                AddShipGoal(Plan.DeployStructure, pos, dir, goal, goal.ToBuildUID, 0f);
            else // orbitals for planet defense
                AddShipGoal(Plan.DeployOrbital, pos, dir, goal, goal.ToBuildUID, 0f);
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
            ClearOrdersAndWayPoints();
            AddExterminateGoal(toBombard);
        }

        public void OrderFindExterminationTarget()
        {
            if (ExterminationTarget?.Owner == null)
            {
                var plist = new Array<Planet>();
                foreach (var planetsDict in Empire.Universe.PlanetsDict)
                {
                    if (planetsDict.Value.Owner != null) plist.Add(planetsDict.Value);
                }

                Planet closest = plist.FindMin(p => Owner.Center.SqDist(p.Center));
                if (closest != null)
                    OrderExterminatePlanet(closest);
            }
            else if (ExterminationTarget != null && OrderQueue.IsEmpty)
            {
                OrderExterminatePlanet(ExterminationTarget);
            }
        }

        public void OrderFormationWarp(Vector2 destination, Vector2 direction)
        {
            float speedLimit = Owner.fleet?.SpeedLimit ?? 0f;
            OrderMoveDirectlyTo(destination, direction, true, speedLimit);
            State = AIState.FormationWarp;
        }

        public void OrderFormationWarpQ(Vector2 destination, Vector2 direction)
        {
            float speedLimit = Owner.fleet?.SpeedLimit ?? 0f;
            OrderMoveDirectlyTo(destination, direction, false, speedLimit);
            State = AIState.FormationWarp;
        }

        public void OrderInterceptShip(Ship toIntercept)
        {
            ClearWayPoints();
            ClearOrders(AIState.Intercept, priority: true);
            Intercepting = true;
            Target = toIntercept;
        }

        public void OrderLandAllTroops(Planet target)
        {
            SetPriorityOrderWithClear();
            if (Owner.Carrier.AnyAssaultOpsAvailable) // This deals also with single Troop Ships / Assault Shuttles
                AddLandTroopGoal(target);
        }

        public void OrderMoveDirectlyTo(Vector2 position, Vector2 finalDir, bool clearWayPoints,
                                        float speedLimit = 0f)
        {
            AddWayPoint(position, finalDir, clearWayPoints, speedLimit, targetPlanet: null);
        }

        public void OrderMoveTo(Vector2 position, Vector2 finalDir, bool clearWayPoints,
                                Planet targetPlanet, Goal goal = null)
        {
            AddWayPoint(position, finalDir, clearWayPoints, speedLimit:0f, targetPlanet, goal);
        }

        // Adds a WayPoint, optionally clears previous WayPoints
        // Then clears all existing ship orders and generates new move orders from WayPoints
        void AddWayPoint(Vector2 position, Vector2 finalDir, bool clearWayPoints,
                         float speedLimit, Planet targetPlanet, Goal goal = null)
        {
            if (!finalDir.IsUnitVector())
                Log.Error($"GenerateOrdersFromWayPoints finalDirection {finalDir} must be a direction unit vector!");

            Target = null;
            if (clearWayPoints)
                ClearWayPoints();
            ClearOrders(AIState.MoveTo, priority: (Owner.loyalty == EmpireManager.Player));

            WayPoints.Enqueue(position);
            MovePosition = position;

            // NOTE: please don't 'FIX' anything here without caution and testing.
            //   Checklist: single ship move & queued move,
            //              fleet move & queued move,
            //              ship group move & queued move,
            //              priority movement for all of the above while in combat
            Vector2[] wayPoints = WayPoints.ToArray();
            Vector2 wp = wayPoints[0];
            Vector2 firstDir = wayPoints.Length >= 2 ? wp.DirectionToTarget(wayPoints[1]) : finalDir;
            AddShipGoal(Plan.RotateToFaceMovePosition, wp, firstDir);
            AddShipGoal(Plan.MoveToWithin1000, wp, firstDir, speedLimit);

            for (int i = 1; i < wayPoints.Length - 1; ++i)
            {
                wp = wayPoints[i];
                Vector2 dir = wayPoints[i-1].DirectionToTarget(wp);
                AddShipGoal(Plan.MoveToWithin1000, wp, dir, speedLimit);
            }
            Vector2 lastWayPoint = WayPoints.PeekLast;
            AddShipGoal(Plan.MoveToWithin1000, lastWayPoint, finalDir, targetPlanet, speedLimit, goal);
            AddShipGoal(Plan.MakeFinalApproach, lastWayPoint, finalDir, targetPlanet, speedLimit, goal);
            AddShipGoal(Plan.RotateToDesiredFacing, lastWayPoint, finalDir, targetPlanet, goal);
        }

        public void OrderOrbitNearest(bool clearOrders)
        {
            ClearWayPoints();

            Target = null;
            Intercepting = false;
            Owner.HyperspaceReturn();
            if (clearOrders)
                ClearOrders(State, HasPriorityOrder);

            Planet closest = Owner.loyalty.RallyShipYardNearestTo(Owner.Center);
            if (closest != null)
            {
                ResupplyTarget = closest;
                AddOrbitPlanetGoal(closest);
                return;
            }

            SolarSystem closestSystem = Owner.loyalty.GetOwnedSystems().FindMin(s => s.Position.SqDist(Owner.Center));
            if (closestSystem != null)
            {
                ResupplyTarget = closestSystem.PlanetList[0];
                AddOrbitPlanetGoal(ResupplyTarget);
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
                where !sys.HostileForcesPresent(Owner.loyalty) && sys.Position.Distance(Owner.Position) > (sys.Radius*1.5f)
                orderby Owner.Center.Distance(sys.Position)
                select sys).ToArray();

            if (systemList.Length > 0)
            {
                ResupplyTarget = systemList[0].PlanetList[0];
                AddOrbitPlanetGoal(ResupplyTarget, AIState.Flee);
            }
        }

        public void OrderOrbitPlanet(Planet p)
        {
            ClearOrdersAndWayPoints();

            Target = null;
            Intercepting = false;
            Owner.HyperspaceReturn();
            ResupplyTarget = p;
            AddOrbitPlanetGoal(p);
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

            // targeting relation
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
            if (clearOrders)
                ClearWayPoints();

            ClearOrders();

            if (p.FreeTilesWithRebaseOnTheWay == 0)
                return;

            OrderMoveTo(p.Center, Vectors.Up, false, p);
            IgnoreCombat = true;
            AddRebaseGoal(p);
        }

        public void OrderRebaseToNearest()
        {
            ClearWayPoints();
            Planet planet = Owner.loyalty.GetPlanets().Filter(p => p.FreeTilesWithRebaseOnTheWay > 0)
                                                      .FindMin(p => Vector2.Distance(Owner.Center, p.Center));

            if (planet == null)
            {
                State = AIState.AwaitingOrders;
                Log.Info($"Could not find a planet to rebase for {Owner.Name}.");
                return;
            }

            OrderMoveTo(planet.Center, Vectors.Up, false, planet);
            IgnoreCombat = true;

            AddRebaseGoal(planet);
        }

        public void OrderRebaseToShip(Ship ship)
        {
            EscortTarget = ship;
            IgnoreCombat = true;
            ClearOrders();
            State = AIState.RebaseToShip;
            AddShipGoal(Plan.RebaseToShip);
        }

        public void OrderRefitTo(Planet refitPlanet, Goal refitGoal)
        {
            OrderMoveTo(refitPlanet.Center, Vectors.Up, true, refitPlanet);
            AddShipGoal(Plan.Refit, refitPlanet, refitGoal);
            IgnoreCombat = true;
            State        = AIState.Refit;
            SetPriorityOrder(clearOrders: false);
        }

        public void OrderResupply(Planet toOrbit, bool clearOrders)
        {
            SetPriorityOrder(clearOrders);
            HadPO = clearOrders;
            ClearWayPoints();

            Target       = null;
            OrbitTarget  = toOrbit;
            AwaitClosest = toOrbit;
            OrderMoveTo(toOrbit.Center, Vectors.Up, clearOrders, toOrbit);
            State = AIState.Resupply;
        }

        public void OrderReturnToHangar()
        {
            ClearOrders(AIState.ReturnToHangar, priority: true);
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

            OrderMoveTo(OrbitTarget.Center, Vectors.Up, true, OrbitTarget);
            AddScrapGoal(OrbitTarget);
        }
        public void AddSupplyShipGoal(Ship supplyTarget)
        {
            IgnoreCombat = true;
            ClearOrders(AIState.Ferrying);
            //Clearorders wipes stored ordnance data if state is ferrying.
            EscortTarget = supplyTarget;
            AddShipGoal(Plan.SupplyShip);
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
                        OrderMoveTo(AwaitClosest.Center, Vectors.Up, true, null);
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
            OrderMoveTo(position, direction, true, null);
        }

        public void OrderToOrbit(Planet toOrbit)
        {
            ClearWayPoints();
            ClearOrders();

            // fbedard: civilian ship will use projectors
            if (Owner.shipData.ShipCategory == ShipData.Category.Civilian)
                OrderMoveTo(toOrbit.Center, Vectors.Up, false, toOrbit);

            AddOrbitPlanetGoal(toOrbit);
        }

        void AwaitOrders(float elapsedTime)
        {
            if (State != AIState.Resupply)
                HasPriorityOrder = false;
            if (AwaitClosest != null)
            {
                Orbit.Orbit(AwaitClosest, elapsedTime);
                return;
            }
            SolarSystem home = Owner.System;
            if (home == null)
            {
                if (SystemToDefend != null)
                {
                    Orbit.Orbit(SystemToDefend.PlanetList[0], elapsedTime);
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
                    if (p != null)
                    {
                        Orbit.Orbit(p, elapsedTime);
                        AwaitClosest = p;
                    }
                    return;
                }

                if (AwaitClosest != null)
                {
                    Orbit.Orbit(AwaitClosest, elapsedTime);
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