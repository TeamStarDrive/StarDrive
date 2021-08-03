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
            IgnoreCombat = false;

            //HACK. To fix this all the fleet tasks that use attackspecifictarget must be changed
            //if they also use hold position to keep ships from moving.
            if (!Owner.loyalty.isPlayer)
                CombatState = Owner.shipData.DefaultCombatState;
            TargetQueue.Add(toAttack);
            HasPriorityTarget = true;

            ClearOrders();
            EnterCombatState(AIState.AttackTarget);
        }

        public void OrderBombardPlanet(Planet toBombard)
        {
            ClearOrdersAndWayPoints();
            AddPlanetGoal(Plan.Bombard, toBombard, AIState.Bombard);
            EnterCombatState(AIState.Bombard);
        }

        public void OrderColonization(Planet toColonize, Goal g = null)
        {
            if (toColonize == null)
                return;

            OrderMoveAndColonize(toColonize, g);
        }

        public void OrderDeepSpaceBuild(Goal goal)
        {
            ClearOrders(State, priority:true);
            Vector2 pos = goal.BuildPosition;
            Vector2 dir = Owner.Position.DirectionToTarget(pos);
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
            Target = target;
            EnterCombatState(AIState.AttackTarget);
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

                Planet closest = plist.FindMin(p => Owner.Position.SqDist(p.Center));
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
            if (Owner.IsSingleTroopShip || Owner.IsDefaultAssaultShuttle ||  Owner.Carrier.AnyAssaultOpsAvailable) // This deals also with single Troop Ships / Assault Shuttles
                AddLandTroopGoal(target);
        }

        public void OrderMoveDirectlyTo(Vector2 position, Vector2 finalDir, bool clearWayPoints,
                                        AIState wantedState, float speedLimit = 0f, bool offensiveMove = false, bool pinPoint = false)
        {
            AddWayPoint(position, finalDir, clearWayPoints, speedLimit, wantedState, offensiveMove, stop:true, pinPoint);
        }

        public void OrderMoveTo(Vector2 position, Vector2 finalDir, bool clearWayPoints, 
                                        AIState wantedState, Goal goal = null, bool offensiveMove = false, bool pinPoint = false)
        {
            AddWayPoint(position, finalDir, clearWayPoints, speedLimit:0f, wantedState, offensiveMove, stop:true, pinPoint, goal);
        }

        public void OrderMoveToNoStop(Vector2 position, Vector2 finalDir, bool clearWayPoints,
                                        AIState wantedState, Goal goal = null, bool offensiveMove = false, bool pinPoint = false)
        {
            AddWayPoint(position, finalDir, clearWayPoints, speedLimit: 0f, wantedState, offensiveMove, stop:false, pinPoint, goal);
        }

        public void OrderResupplyEscape(Vector2 position, Vector2 finalDir)
        {
            var goal = new ShipGoal(Plan.MoveToWithin1000, position, finalDir, AIState.Resupply, MoveTypes.WayPoint, 0, null);
            PushGoalToFront(goal);
        }

        // Adds a WayPoint, optionally clears previous WayPoints
        // Then clears all existing ship orders and generates new move orders from WayPoints
        void AddWayPoint(Vector2 position, Vector2 finalDir, bool clearWayPoints,
                         float speedLimit, AIState wantedState,
                         bool offensiveMove, bool stop, bool pinPoint, Goal goal = null)
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
            MoveTypes lastMove = Owner.loyalty.isPlayer && !offensiveMove && pinPoint 
                ? combatEndMove = MoveTypes.None  // Ships will move to the exact location  before engaging combat (secondary fire will apply)
                : MoveTypes.LastWayPoint | combatEndMove; // Allow ships to engage combat if within 1000 of the move target

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

        public void OrderAwaitOrders(bool clearPriorityOrder = true)
        {
            State = AIState.AwaitingOrders;
            if (clearPriorityOrder)
                SetPriorityOrder(false);
        }

        public void OrderOrbitNearest(bool clearOrders)
        {
            ClearWayPoints();

            Target = null;
            Intercepting = false;
            Owner.HyperspaceReturn();
            if (clearOrders)
                ClearOrders(State, HasPriorityOrder);

            Planet closest = Owner.loyalty.RallyShipYardNearestTo(Owner.Position);
            if (closest != null)
            {
                ResupplyTarget = closest;
                AddOrbitPlanetGoal(closest);
                return;
            }

            SolarSystem closestSystem = Owner.loyalty.GetOwnedSystems().FindMin(s => s.Position.SqDist(Owner.Position));
            if (closestSystem != null)
            {
                ResupplyTarget = closestSystem.PlanetList[0];
                AddOrbitPlanetGoal(ResupplyTarget);
                return;
            }

            var emergencyPlanet = Empire.Universe.PlanetsDict.Values.ToArray().Filter(p => p.Owner == null);
            emergencyPlanet.Sort(p => p.Center.SqDist(Owner.Position));
            OrbitTarget = emergencyPlanet[0];
        }

        public void OrderFlee()
        {
            ClearWayPoints();

            Target = null;
            Intercepting = false;
            Owner.HyperspaceReturn();
            ClearOrders(State, HasPriorityOrder);

                ResupplyTarget = Owner.loyalty.GetPlanets().FindClosestTo(Owner, IsSafePlanet)
                             // fallback to any safe planet - this is a very rare case where no alternatives were found
                             ?? Empire.Universe.PlanetsDict.Values.ToArray().FindClosestTo(Owner, IsSafePlanet);

            if (ResupplyTarget != null)
                AddOrbitPlanetGoal(ResupplyTarget, AIState.Flee);
            else if (Owner.TryGetScoutFleeVector(out Vector2 pos)) // just get out of here
                OrderMoveToNoStop(pos, Owner.Direction.DirectionToTarget(pos), true, AIState.Flee);
            else
                ClearOrders(); // give up and resume combat

            // Local method
            bool IsSafePlanet(Planet p) => !p.ParentSystem.DangerousForcesPresent(Owner.loyalty);
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
                Ship ship = pirateBases.FindClosestTo(Owner);
                OrderMoveToPirateBase(ship);
            }
            else
            {
                OrderFlee();
            }

            if (signalRetreat)
            {
                Ship[] friends = FriendliesNearby;
                for (int i = 0; i < friends.Length; i++)
                    friends[i].AI.OrderPirateFleeHome();
            }
        }

        public void OrderRemnantFlee()
        {
            if (Owner.loyalty.WeAreRemnants
                && !Owner.IsPlatformOrStation
                && Owner.loyalty.Remnants.GetClosestPortal(Owner.Position, out Ship portal))
            {
                OrderMoveToNoStop(portal.Position.GenerateRandomPointOnCircle(5000), Owner.Direction,
                    true, AIState.MoveTo);

                AddEscortGoal(portal, clearOrders: false); // Orders are cleared in OrderMoveTo
            }
            else
            {
                OrderFlee();
            }
        }

        void OrderMoveToPirateBase(Ship pirateBase)
        {
            OrderMoveToNoStop(pirateBase.Position.GenerateRandomPointOnCircle(5000), Owner.Direction,
                true, AIState.MoveTo);

            AddEscortGoal(pirateBase, clearOrders: false); // Orders are cleared in OrderMoveTo
        }

        public void OrderQueueSpecificTarget(Ship toAttack)
        {
            if (TargetQueue.Count == 0 && Target != null && Target.Active && Target != toAttack)
            {
                OrderAttackSpecificTarget(Target);
                TargetQueue.Add(Target);
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
                                                      .FindMin(p => Vector2.Distance(Owner.Position, p.Center));

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
            HadPO = HasPriorityOrder;
            ResetPriorityOrder(clearOrders);
            IgnoreCombat = true;
            ClearWayPoints();

            if (!Owner.loyalty.isPlayer)
                Owner.fleet?.RemoveShip(Owner, returnToEmpireAI: false); // Avoid lingering fleets for the AI

            Target       = null;
            OrbitTarget  = toOrbit;
            AwaitClosest = toOrbit;
            AddResupplyPlanetGoal(toOrbit);

            if (Owner.TryGetEscapeVector(out Vector2 escapePos))
                OrderResupplyEscape(escapePos, Owner.Direction);
        }

        // Thread-Safe: orders the ship to return to Hangar during next ship AI update
        public void OrderReturnToHangarDeferred()
        {
            ReturnToHangarSoon = true;
        }

        // @warning This is NOT thread-safe! Do not call externally from other Ships!
        public void OrderReturnToHangar()
        {
            ClearOrders(AIState.ReturnToHangar, priority: true);
            AddShipGoal(Plan.ReturnToHangar, AIState.ReturnToHangar);
        }

        public void OrderReturnHome()
        {
            ClearOrders(priority:true);
            AddShipGoal(Plan.ReturnHome, AIState.ReturnHome);
        }

        // Move to closest colony and get back some resources
        public void OrderScrapShip()
        {
            Owner.loyalty.GetEmpireAI().AddScrapShipGoal(Owner, immediateScuttle:false);
        }

        // Immediately self-destruct
        public void OrderScuttleShip()
        {
            Owner.loyalty.GetEmpireAI().AddScrapShipGoal(Owner, immediateScuttle:true);
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

            if (SystemToDefend != system || AwaitClosest?.Owner == null ||
                AwaitClosest.Owner != Owner.loyalty || Owner.System != system &&
                goal != null && OrderQueue.PeekLast.Plan != Plan.DefendSystem)
            {
                ClearOrders(State);

                SystemToDefend = system;
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
            if (Owner.shipData.HullRole == ShipData.RoleName.drone)
            {
                Owner.Die(null, true);
                return; // Drones never go to resupply, using hull role in case someone makes a drone module which changes the DesignRole
            }

            Planet nearestRallyPoint = Owner.loyalty.RallyShipYardNearestTo(Owner.Position);
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
                if (Owner.Position.Distance(toOrbit.Center) > threshold)
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
                var ships = Owner.loyalty.OwnedShips;
                var solarSystem = ships.FindMinFiltered(ships.Count, ship => ship.System != null,
                                     ship => Owner.Position.SqDist(ship.Position))?.System;               

                AwaitClosest = solarSystem?.PlanetList.FindMax(p => p.FindNearbyFriendlyShips().Length);
                if (AwaitClosest == null)
                {
                    var system = Empire.Universe.SolarSystemDict.FindMinValue(ss =>
                                 Owner.Position.SqDist(ss.Position) * (ss.OwnerList.Count + 1));
                    AwaitClosest = system?.PlanetList.FindClosestTo(Owner);
                }
                if (AwaitClosest == null)
                {
                    AwaitClosest = Empire.Universe.PlanetsDict.FindMinValue(p =>
                        p.Center.SqDist(Owner.Position));
                }
            }
            return AwaitClosest != null;

        }

        bool SetAwaitClosestForPlayer()
        {
            if (!Owner.loyalty.isPlayer) return false;

            AwaitClosest = Owner.loyalty.GetPlanets().FindMin(p => Owner.Position.SqDist(p.Center));
            return AwaitClosest != null;
        }

        bool SetAwaitClosestForAIEmpire()
        {
            if (Owner.loyalty.isFaction || Owner.loyalty.isPlayer)
                return false;

            SolarSystem home = Owner.System?.OwnerList.Contains(Owner.loyalty) != true? null : Owner.System;
            if (home == null)
            {
                AwaitClosest = Owner.loyalty.GetBestNearbyPlanetToOrbitForAI(Owner);

                if (AwaitClosest == null) //Find any system with no owners and planets.
                {
                    var system = Empire.Universe.SolarSystemDict.FindMinValue(ss =>
                               Owner.Position.SqDist(ss.Position) * (ss.OwnerList.Count + 1));
                    if (system == null)
                        return false;

                    AwaitClosest = system.PlanetList.FindClosestTo(Owner);
                }
            }
            else
            {
                AwaitClosest = home.PlanetList.FindMinFiltered(p => p.Owner == Owner.loyalty
                                                            , p => p.Center.SqDist(Owner.Position));
            }
            return AwaitClosest != null;
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

            if (AwaitClosest != null
                || SetAwaitClosestForSystemToDefend() 
                || SetAwaitClosestForFaction()
                || SetAwaitClosestForPlayer()
                || SetAwaitClosestForAIEmpire()) 
            {
                Orbit.Orbit(AwaitClosest, timeStep);
            }
        }

        void AwaitOrdersPlayer(FixedSimTime timeStep)
        {
            SetPriorityOrder(false);

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
                    .FindMin(p => p.Center.SqDist(Owner.Position) + (Owner.loyalty != p.Owner ? 300000 : 0));
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