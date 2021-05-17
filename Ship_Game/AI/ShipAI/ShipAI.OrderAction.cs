using System;
using Microsoft.Xna.Framework;
using Ship_Game.Gameplay;
using Ship_Game.Ships;
using System.Linq;
using Ship_Game.Ships.AI;

namespace Ship_Game.AI
{
    public sealed partial class ShipAI
    {
        public void OrderAllStop()
        {
            ClearWayPoints();
            ClearOrders();
            AddShipGoal(Plan.Stop, AIState.HoldPosition);
        }

        public void OrderHoldPosition(Vector2 position, Vector2 direction)
        {
            AddShipGoal(Plan.HoldPosition, position, direction, AIState.HoldPosition);
            SetPriorityOrder(true);
            IgnoreCombat = true;
        }

        public void OrderHoldPositionOffensive(Vector2 position, Vector2 direction)
        {
            AddShipGoal(Plan.HoldPositionOffensive, position, direction, AIState.HoldPosition);
            IgnoreCombat = false;
        }

        public void OrderAttackSpecificTarget(Ship toAttack)
        {
            TargetQueue.Clear();

            if (toAttack == null)
                return;

            if (!Owner.loyalty.IsEmpireAttackable(toAttack.loyalty, toAttack))
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
            ClearOrders();
            AddShipGoal(Plan.DoCombat, AIState.AttackTarget);
        }

        public void OrderBombardPlanet(Planet toBombard)
        {
            Owner.InCombatTimer = 15f;
            ClearOrdersAndWayPoints();
            AddBombPlanetGoal(toBombard);
        }

        public void OrderBombardWithPriority(Planet toBombard)
        {
            Owner.InCombatTimer = 15f;
            ClearOrdersAndWayPoints();
            AddPriorityBombPlanetGoal(toBombard);
        }

        public void OrderColonization(Planet toColonize, Goal g = null)
        {
            if (toColonize == null)
                return;

            ColonizeTarget = toColonize;
            OrderMoveAndColonize(toColonize, g);
        }

        public void OrderDeepSpaceBuild(Goal goal)
        {
            ClearOrders(State, priority:true);
            Vector2 pos = goal.BuildPosition;
            Vector2 dir = Owner.Center.DirectionToTarget(pos);
            OrderMoveTo(pos, dir, true, AIState.MoveTo, goal);
            if (goal.type == GoalType.DeepSpaceConstruction || goal.TetherTarget == Guid.Empty) // deep space structures
                AddShipGoal(Plan.DeployStructure, pos, dir, goal, goal.ToBuildUID, 0f, AIState.MoveTo);
            else // orbitals for planet defense
                AddShipGoal(Plan.DeployOrbital, pos, dir, goal, goal.ToBuildUID, 0f, AIState.MoveTo);
        }

        public void OrderScout(SolarSystem target, Goal g)
        {
            ClearWayPoints();
            ClearOrders();
            OrderMoveToNoStop(target.Position, Owner.Direction, true, AIState.Explore, g);
            ExplorationTarget = target;
        }

        public void OrderExplore()
        {
            if (State == AIState.Explore && ExplorationTarget != null)
                return;

            ClearWayPoints();
            ClearOrders();
            AddShipGoal(Plan.Explore, AIState.Explore);
        }

        public void OrderExterminatePlanet(Planet toBombard)
        {
            ClearOrdersAndWayPoints();
            AddExterminateGoal(toBombard);
        }

        public void OrderAttackPriorityTarget(Ship target)
        {
            HasPriorityTarget = true;
            Target            = target;
            AddShipGoal(Plan.DoCombat, AIState.AttackTarget);
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

        public void OrderFormationWarp(Vector2 destination, Vector2 direction, bool offensiveMove)
        {
            float speedLimit = Owner.fleet?.GetSpeedLimitFor(Owner) ?? 0;
            OrderMoveDirectlyTo(destination, direction, true, AIState.FormationWarp, speedLimit, offensiveMove);
        }

        public void OrderFormationWarpQ(Vector2 destination, Vector2 direction, bool offensiveMove)
        {
            float speedLimit = Owner.fleet?.GetSpeedLimitFor(Owner) ?? 0;
            OrderMoveDirectlyTo(destination, direction, false, AIState.FormationWarp, speedLimit, offensiveMove);
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
            ResetPriorityOrderWithClear();
            // anyassaultops is broken and doesnt work with troop shuttles. 
            if (Owner.DesignRole == ShipData.RoleName.troop ||  Owner.Carrier.AnyAssaultOpsAvailable) // This deals also with single Troop Ships / Assault Shuttles
                AddLandTroopGoal(target);
        }

        public void OrderMoveDirectlyTo(Vector2 position, Vector2 finalDir, bool clearWayPoints,
                                        AIState wantedState, float speedLimit = 0f, bool offensiveMove = false)
        {
            AddWayPoint(position, finalDir, clearWayPoints, speedLimit, wantedState, offensiveMove, true);
        }

        public void OrderMoveTo(Vector2 position, Vector2 finalDir, bool clearWayPoints, AIState wantedState, Goal goal = null, bool offensiveMove = false)
        {
            AddWayPoint(position, finalDir, clearWayPoints, speedLimit:0f, wantedState, offensiveMove, true, goal);
        }

        public void OrderMoveToNoStop(Vector2 position, Vector2 finalDir, bool clearWayPoints, AIState wantedState, Goal goal = null, bool offensiveMove = false)
        {
            AddWayPoint(position, finalDir, clearWayPoints, speedLimit: 0f, wantedState, offensiveMove, false, goal);
        }

        public void OrderResupplyEscape(Vector2 position, Vector2 finalDir)
        {
            ShipGoal goal = new ShipGoal(Plan.MoveToWithin1000, position, finalDir, AIState.Resupply, MoveTypes.WayPoint, 0, null);
            OrderQueue.PushToFront(goal);
        }

        // Adds a WayPoint, optionally clears previous WayPoints
        // Then clears all existing ship orders and generates new move orders from WayPoints
        void AddWayPoint(Vector2 position, Vector2 finalDir, bool clearWayPoints,
                         float speedLimit, AIState wantedState, 
                         bool offensiveMove, bool stop, Goal goal = null)
        {
            if (!finalDir.IsUnitVector())
                Log.Error($"GenerateOrdersFromWayPoints finalDirection {finalDir} must be a direction unit vector!");

            Target = null;
            if (clearWayPoints)
                ClearWayPoints();

            // FB - if offensive move it true, ships will break and attack targets on the way to the destination
            ClearOrders(wantedState, priority: !offensiveMove);

            WayPoints.Enqueue(new WayPoint(position, finalDir));
            MovePosition = position;

            // NOTE: please don't 'FIX' anything here without caution and testing.
            //   Checklist: single ship move & queued move,
            //              fleet move & queued move,
            //              ship group move & queued move,
            //              priority movement for all of the above while in combat
            //              Verify ships can complete move to planet goals like colonization.
            WayPoint[] wayPoints = WayPoints.ToArray();
            WayPoint wp = wayPoints[0];

            AddMoveOrder(Plan.RotateToFaceMovePosition, wp, State,0, MoveTypes.FirstWayPoint);
            
            MoveTypes combatMidMove = offensiveMove ? MoveTypes.Combat : MoveTypes.None;

            MoveTypes combatEndMove = (goal?.IsPriorityMovement() ?? !offensiveMove) ? MoveTypes.None : MoveTypes.Combat;


            // set moveto1000 for each waypoint except for the last one. 
            // if only one waypoint skip this. 

            for (int i = 0; i < wayPoints.Length - 1; ++i)
            {
                AddMoveOrder(Plan.MoveToWithin1000, wayPoints[i], State, speedLimit, MoveTypes.WayPoint | combatMidMove);
            }
            // set final move position.
            // move to within 1000 of the position.
            // make a precision approach.
            // rotate to desired facing <= this needs to be fixed.
            // the position is always wrong unless it was forced in a ui move. 
            wp = wayPoints[wayPoints.Length - 1];

            // FB - do not remove priority order if the ship belongs to the player and not offensive move
            MoveTypes lastMove = Owner.loyalty.isPlayer && !offensiveMove ? combatEndMove = MoveTypes.None 
                                                                          : MoveTypes.LastWayPoint | combatEndMove;

            AddMoveOrder(Plan.MoveToWithin1000, wp, State, speedLimit, lastMove);

            // FB - Do not make final approach and stop, since the ship has more orders which do not
            // require stopping or rotating. 
            // If stopping, it will go the the set pos and not to the dynamic target planet center.
            if (stop)
            {
                AddMoveOrder(Plan.MakeFinalApproach, wp, State, 0, MoveTypes.SubLightApproach | combatEndMove, goal);
                AddMoveOrder(Plan.RotateToDesiredFacing, wp, State,0, MoveTypes.SubLightApproach | combatEndMove, goal);
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
                where !sys.DangerousForcesPresent(Owner.loyalty) && sys.Position.Distance(Owner.Position) > (sys.Radius*1.5f)
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

        public void OrderPirateFleeHome(bool signalRetreat = false)
        {

            if (Owner.loyalty.WeArePirates 
                && !Owner.IsPlatformOrStation 
                && Owner.loyalty.Pirates.GetBases(out Array<Ship> pirateBases))
            {
                Ship ship = pirateBases.FindMin(s => s.Center.Distance(Owner.Center));
                OrderMoveToPirateBase(ship);
            }
            else
            {
                ClearOrders();
            }

            if (signalRetreat)
                using (FriendliesNearby.AcquireReadLock())
                {
                    for (int i = 0; i < FriendliesNearby.Count; i++)
                        FriendliesNearby[i].AI.OrderPirateFleeHome();
                }
        }

        void OrderMoveToPirateBase(Ship pirateBase)
        {
            OrderMoveToNoStop(pirateBase.Center.GenerateRandomPointOnCircle(5000), Owner.Direction,
                true, AIState.MoveTo);

            AddEscortGoal(pirateBase, clearOrders: false); // Orders are cleared in OrderMoveTo
            Owner.AI.SetPriorityOrder(true);
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
            if (Owner.loyalty.GetRelations(toAttack.loyalty, out Relationship relations))
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
                    SetPriorityOrder(false);
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

            if (p.FreeTilesWithRebaseOnTheWay(Owner.loyalty) == 0)
                return;

            IgnoreCombat = true;
            OrderMoveAndRebase(p);
        }

        public void OrderRebaseToNearest()
        {
            ClearWayPoints();

            if (Owner.loyalty.WeArePirates)
            {
                OrderPirateFleeHome();
                return;
            }

            Planet planet = Owner.loyalty.GetPlanets().Filter(p => p.FreeTilesWithRebaseOnTheWay(Owner.loyalty) > 0)
                                                      .FindMin(p => Vector2.Distance(Owner.Center, p.Center));

            if (planet == null)
            {
                State = AIState.AwaitingOrders;
                Log.Info($"Could not find a planet to rebase for {Owner.Name}.");
                return;
            }

            IgnoreCombat = true;
            OrderMoveAndRebase(planet);
        }

        public void OrderRebaseToShip(Ship ship)
        {
            ClearOrders();
            EscortTarget = ship;
            IgnoreCombat = true;
            AddShipGoal(Plan.RebaseToShip, AIState.RebaseToShip);
        }

        public void OrderRefitTo(Planet refitPlanet, Goal refitGoal)
        {
            OrderMoveAndRefit(refitPlanet, refitGoal);
        }

        public void OrderResupply(Planet toOrbit, bool clearOrders)
        {
            ResetPriorityOrder(clearOrders);
            HadPO = clearOrders;
            ClearWayPoints();

            if (!Owner.loyalty.isPlayer)
                Owner.fleet?.RemoveShip(Owner); // Avoid lingering fleets for the AI

            Target       = null;
            OrbitTarget  = toOrbit;
            AwaitClosest = toOrbit;
            AddResupplyPlanetGoal(toOrbit);

            if (Owner.TryGetEscapeVector(out Vector2 escapePos))
                OrderResupplyEscape(escapePos, Owner.Direction);
        }

        public void OrderReturnToHangar()
        {
            ClearOrders(priority: true);
            AddShipGoal(Plan.ReturnToHangar, AIState.ReturnToHangar);
        }

        public void OrderReturnHome()
        {
            ClearOrders(priority:true);
            AddShipGoal(Plan.ReturnHome, AIState.ReturnHome);
        }

        public void OrderScrapShip()
        {
            Owner.loyalty.GetEmpireAI().AddScrapShipGoal(Owner);
        }

        public void AddSupplyShipGoal(Ship supplyTarget, Plan plan = Plan.SupplyShip)
        {
            ClearOrders();
            IgnoreCombat = true;
            //Clearorders wipes stored ordnance data if state is ferrying.
            EscortTarget = supplyTarget;
            AddShipGoal(plan, AIState.Ferrying);
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
                        OderMoveAndDefendSystem(AwaitClosest);
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
            OrderMoveTo(position, direction, true, AIState.MoveTo);
        }

        public void OrderToOrbit(Planet toOrbit, bool offensiveMove = false)
        {
            ClearWayPoints();
            ClearOrders();

            // FB - this will give priority order for the movement. if offensiveMove is false,
            // it means the player ordered this specifically wanting combat ships to engage targets
            // of opportunity, even dropping our of warp to engage them.
            if (!offensiveMove || Owner.shipData.ShipCategory == ShipData.Category.Civilian)
            {
                // only order to move if we are too far, no need to waste time here.
                float threshold = toOrbit.ObjectRadius + 1000 * toOrbit.Scale;
                if (Owner.Center.Distance(toOrbit.Center) > threshold)
                {
                    Vector2 finalDir = Owner.Position.DirectionToTarget(toOrbit.Center);
                    OrderMoveToNoStop(toOrbit.Center, finalDir, false, AIState.MoveTo);
                }
            }

            AddOrbitPlanetGoal(toOrbit);
        }

        bool SetAwaitClosestForSystemToDefend()
        {
            if (SystemToDefend == null) return false;
            AwaitClosest = SystemToDefend.PlanetList[0];
            return true;
        }

        bool SetAwaitClosestForFaction()
        {
            if (!Owner.loyalty.isFaction)
                return false;

            AwaitClosest = Owner.System?.PlanetList.FindMax(p => p.FindNearbyFriendlyShips().Length);

            if (AwaitClosest == null)
            {
                var ships = (Array<Ship>)Owner.loyalty.OwnedShips;
                var solarSystem = ships.FindMinFiltered(ship => ship.System != null,
                                     ship => Owner.Center.SqDist(ship.Center))?.System;

                AwaitClosest = solarSystem?.PlanetList.FindMax(p => p.FindNearbyFriendlyShips().Length);
                if (AwaitClosest == null)
                {
                    var system = Empire.Universe.SolarSystemDict.FindMinValue(ss =>
                                 Owner.Center.SqDist(ss.Position) * (ss.OwnerList.Count + 1));
                    AwaitClosest = system?.PlanetList.FindClosestTo(Owner);
                }
                if (AwaitClosest == null)
                {
                    AwaitClosest = Empire.Universe.PlanetsDict.FindMinValue(p =>
                        p.Center.SqDist(Owner.Center));
                }
            }
            return true;

        }

        bool SetAwaitClosestForPlayer()
        {
            if (!Owner.loyalty.isPlayer) return false;

            AwaitClosest = Owner.loyalty.GetPlanets().FindMin(p => Owner.Center.SqDist(p.Center));
            return true;
        }

        bool SetAwaitClosestForAIEmpire()
        {
            if (Owner.loyalty.isFaction || Owner.loyalty.isPlayer) return false;

            SolarSystem home = Owner.System?.OwnerList.Contains(Owner.loyalty) != true? null : Owner.System;
            if (home == null)
            {
                AwaitClosest = Owner.loyalty.GetBestNearbyPlanetToOrbitForAI(Owner);

                if (AwaitClosest == null) //Find any system with no owners and planets.
                {
                    var system = Empire.Universe.SolarSystemDict.FindMinValue(ss =>
                               Owner.Center.SqDist(ss.Position) * (ss.OwnerList.Count + 1));
                    AwaitClosest = system.PlanetList.FindClosestTo(Owner);
                }
            }
            else
            {
                AwaitClosest = home.PlanetList.FindMinFiltered(p => p.Owner == Owner.loyalty
                                                            , p => p.Center.SqDist(Owner.Center));
            }
            return true;
        }

        void AwaitOrders(FixedSimTime timeStep)
        {
            if (Owner.IsPlatformOrStation) 
                return;

            if (Owner.shipData.CarrierShip)
                return;

            if (State != AIState.Resupply)
                SetPriorityOrder(false);

            if (AwaitClosest != null)
            {
                if (SystemToDefend != null || Owner.loyalty.isPlayer)
                {
                    Orbit.Orbit(AwaitClosest, timeStep);
                    return;
                }
                if (AwaitClosest.ParentSystem.OwnerList.Count > 0)
                    AwaitClosest = null;
            }

            if (SetAwaitClosestForSystemToDefend() 
                || SetAwaitClosestForFaction()
                || SetAwaitClosestForPlayer()
                || SetAwaitClosestForAIEmpire() && AwaitClosest != null)
            {
                Orbit.Orbit(AwaitClosest, timeStep);
            }
            else
            {
                Log.Warning($"Could not find planet to idle at for {Owner}");
            }
        }

        void AwaitOrdersPlayer(FixedSimTime timeStep)
        {
            SetPriorityOrder(false);
            if (Owner.InCombatTimer > timeStep.FixedTime * -5 && ScanForThreatTimer < 2 - timeStep.FixedTime * 5)
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
                        Orbit.Orbit(p, timeStep);
                        AwaitClosest = p;
                    }
                    return;
                }

                if (AwaitClosest != null)
                {
                    Orbit.Orbit(AwaitClosest, timeStep);
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
            ReverseThrustUntilStopped(timeStep);
        }
    }
}