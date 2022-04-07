using System;
using Microsoft.Xna.Framework;
using Ship_Game.Gameplay;
using Ship_Game.Ships;
using Ship_Game.Ships.AI;

namespace Ship_Game.AI
{
    [Flags]
    public enum MoveOrder
    {
        // The default move stance
        // No drop from warp
        // Depends heavily on the unit Combat Stance,
        //      If CombatStance=AttackRuns, the ship will hunt anything in the solar system
        Regular = (1 << 1),

        // Kill anything on the way, drop out of warp, resume to destination after enemy destroyed
        Aggressive = (1 << 2),

        // No drop from warp, don't chase enemies, only shoot at targets of opportunity
        StandGround = (1 << 3),

        // Whether to QUEUE this as the next move WayPoint
        AddWayPoint = (1 << 4),

        // Ships will not perform stopping actions after WayPoint completion
        NoStop = (1 << 5),

        // Forces the Fleet or ShipGroup to Reassemble individual ship offsets
        ForceReassembly = (1 << 6),

        // Forces Fleet ships to reform between WayPoints
        ReformAtWayPoint = (1 << 7),

        // Flag for WayPoint movement system to Dequeue WayPoints
        DequeueWayPoint = (1 << 8),
    }

    public sealed partial class ShipAI
    {
        public void OrderAllStop()
        {
            ClearWayPoints();
            ClearOrders();
            AddShipGoal(Plan.Stop, AIState.HoldPosition);
        }

        // Forces the ship to HoldPosition at its current location
        // MoveOrder parameter controls how the ship responds to threats
        // if MoveOrder.StandGround, then ship will have priority HoldPosition
        public void OrderHoldPosition(MoveOrder order = MoveOrder.Regular)
        {
            OrderHoldPosition(Owner.Position, Owner.Direction, order);
        }
        
        // Forces the ship to HoldPosition at `position`, facing `direction`
        // MoveOrder parameter controls how the ship responds to threats
        // if MoveOrder.StandGround, then ship will have priority HoldPosition
        public void OrderHoldPosition(Vector2 position, Vector2 direction, MoveOrder order = MoveOrder.Regular)
        {
            AddMoveOrder(Plan.HoldPosition, new WayPoint(position, direction), AIState.HoldPosition, 0f, order);
            IgnoreCombat = false;

            if (order.IsSet(MoveOrder.StandGround))
            {
                if (Owner.Loyalty.isPlayer)
                {
                    // for players, StandGround is a hardline HoldPosition
                    CombatState = CombatState.HoldPosition;
                    SetPriorityOrder(true);
                }
                else
                {
                    // for AI, StandGround is more of a suggestion
                    CombatState = CombatState.GuardMode;
                }
            }
        }

        public void OrderAttackSpecificTarget(Ship toAttack)
        {
            TargetQueue.Clear();

            if (toAttack == null)
                return;

            if (!Owner.Loyalty.IsEmpireAttackable(toAttack.Loyalty, toAttack))
                return;
            if (State == AIState.AttackTarget && Target == toAttack)
                return;
            if (State == AIState.SystemDefender && Target == toAttack)
                return;

            if (Owner.Weapons.Count == 0 || Owner.ShipData.Role == RoleName.troop)
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
            if (!Owner.Loyalty.isPlayer)
                CombatState = Owner.ShipData.DefaultCombatState;
            TargetQueue.Add(toAttack);
            HasPriorityTarget = true;

            ClearOrders();
            EnterCombatState(AIState.AttackTarget);
        }

        public void OrderBombardPlanet(Planet toBombard, bool clearOrders)
        {
            if (clearOrders)
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
            if (goal.type == GoalType.DeepSpaceConstruction || goal.TetherPlanetId == 0) // deep space structures
                AddShipGoal(Plan.DeployStructure, pos, dir, goal, goal.ToBuildUID, 0f, AIState.MoveTo);
            else // orbitals for planet defense
                AddShipGoal(Plan.DeployOrbital, pos, dir, goal, goal.ToBuildUID, 0f, AIState.MoveTo);
        }

        public void OrderScout(SolarSystem target, Goal g)
        {
            ClearWayPoints();
            ClearOrders();
            OrderMoveToNoStop(target.Position, Owner.Direction, AIState.Explore, MoveOrder.Regular, g);
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
                Planet closest = Owner.Universe.Planets.Filter(p => p.Owner != null)
                                                       .FindMin(p => Owner.Position.SqDist(p.Center));
                if (closest != null)
                    OrderExterminatePlanet(closest);
            }
            else if (ExterminationTarget != null && OrderQueue.IsEmpty)
            {
                OrderExterminatePlanet(ExterminationTarget);
            }
        }

        public void OrderInterceptShip(Ship toIntercept)
        {
            ClearWayPoints();
            ClearOrders(AIState.Intercept, priority: true);
            Intercepting = true;
            Target = toIntercept;
        }

        public void OrderLandAllTroops(Planet target, bool clearOrders)
        {
            if (clearOrders)
                ResetPriorityOrderWithClear();

            // anyassaultops is broken and doesnt work with troop shuttles. 
            if (Owner.IsSingleTroopShip || Owner.IsDefaultAssaultShuttle ||  Owner.Carrier.AnyAssaultOpsAvailable) // This deals also with single Troop Ships / Assault Shuttles
                AddLandTroopGoal(target);
        }

        public void OrderMoveTo(Vector2 position, Vector2 finalDir, MoveOrder order = MoveOrder.Regular)
        {
            AddWayPoint(position, finalDir, AIState.MoveTo, order, 0f, null);
        }

        public void OrderMoveTo(Vector2 position, Vector2 finalDir, AIState wantedState, MoveOrder order, float speedLimit)
        {
            AddWayPoint(position, finalDir, wantedState, order, speedLimit, null);
        }

        // DO NOT ADD MORE ARGUMENTS. USE `MoveOrder` FLAGS INSTEAD.
        public void OrderMoveTo(Vector2 position, Vector2 finalDir, AIState wantedState,
                                MoveOrder order = MoveOrder.Regular, Goal goal = null)
        {
            AddWayPoint(position, finalDir, wantedState, order, 0f, goal);
        }
        
        // DO NOT ADD MORE ARGUMENTS. USE `MoveOrder` FLAGS INSTEAD.
        public void OrderMoveToNoStop(Vector2 position, Vector2 finalDir, AIState wantedState,
                                      MoveOrder order = MoveOrder.Regular, Goal goal = null)
        {
            AddWayPoint(position, finalDir, wantedState, order|MoveOrder.NoStop, 0f, goal);
        }

        public void OrderResupplyEscape(Vector2 position, Vector2 finalDir)
        {
            var goal = new ShipGoal(Plan.MoveToWithin1000, position, finalDir, AIState.Resupply, MoveOrder.Regular, 0, null);
            PushGoalToFront(goal);
        }

        // Adds a WayPoint, optionally clears previous WayPoints
        // Then clears all existing ship orders and generates new move orders from WayPoints
        void AddWayPoint(Vector2 position, Vector2 finalDir, AIState wantedState, MoveOrder order, float speedLimit, Goal goal)
        {
            if (!finalDir.IsUnitVector())
                Log.Error($"AddWayPoint finalDir {finalDir} must be a direction unit vector!");

            Target = null;
            bool queueNewWayPoint = order.IsSet(MoveOrder.AddWayPoint);
            if (!queueNewWayPoint)
            {
                ClearWayPoints();
            }

            // clean up the move order so we only pass forward essentialy information
            MoveOrder o = default;
            if      (order.IsSet(MoveOrder.Aggressive))  o |= MoveOrder.Aggressive;
            else if (order.IsSet(MoveOrder.Regular))     o |= MoveOrder.Regular;
            else if (order.IsSet(MoveOrder.StandGround)) o |= MoveOrder.StandGround;

            // FB - if offensive move is true, ships will break and attack targets on the way to the destination
            bool offensiveMove = order.IsSet(MoveOrder.Aggressive);
            ClearOrders(wantedState, priority: !offensiveMove);

            WayPoints.Enqueue(new WayPoint(position, finalDir));
            MovePosition = position;

            // WARNING: please don't 'FIX' anything here without caution and testing.
            //   Checklist: single ship move & queued move,
            //              fleet move & queued move,
            //              ship group move & queued move,
            //              priority movement for all of the above while in combat
            //              Verify ships can complete move to planet goals like colonization.
            WayPoint[] wayPoints = WayPoints.ToArray();

               /////////////////////////////////////////////////////////////////
              ////// --               FINAL WARNING                   -- //////
             ////// -- DO NOT MODIFY ANY OF THIS WITHOUT CODE REVIEW -- //////
            ////// --    IT'S INCREDIBLY EASY TO FUBAR THIS CODE!   -- //////
            ////// --                                               -- //////
             ////// --  IF AND WHEN YOU FUBAR THIS ANYWAY INCREASE   -- //////
              ////// --       THIS COUNTER AS WARNING TO OTHERS       -- //////
               ////// --        total_hours_wasted_here = 112          -- //////
                /////////////////////////////////////////////////////////////////
            AddMoveOrder(Plan.RotateToFaceMovePosition, wayPoints[0], State, speedLimit, o);

            // this allows fleets to keep their cohesion between waypoints
            // it makes fleet warps slower, but keeps the fleet together which is more important
            bool assembleBetweenWayPoints = order.IsSet(MoveOrder.ReformAtWayPoint);

            // Set all WayPoints except the last one
            WayPoint wp;
            for (int i = 0; i < wayPoints.Length - 1; ++i)
            {
                wp = wayPoints[i];
                if (assembleBetweenWayPoints)
                {
                    AddMoveOrder(Plan.MoveToWithin1000, wp, State, speedLimit, o|MoveOrder.DequeueWayPoint);
                    AddMoveOrder(Plan.MakeFinalApproach, wp, State, speedLimit, o, goal);
                    (Vector2 dirToNext, float dist) = wp.Position.GetDirectionAndLength(wayPoints[i + 1].Position);
                    Vector2 nextPos = wp.Position + dirToNext*Math.Min(1000f, dist*0.25f);
                    AddMoveOrder(Plan.RotateToFaceMovePosition, new WayPoint(nextPos, dirToNext), State, speedLimit, o, goal);
                }
                else
                {
                    AddMoveOrder(Plan.MoveToWithin1000, wp, State, speedLimit, o|MoveOrder.DequeueWayPoint);
                }
            }

            wp = wayPoints[wayPoints.Length - 1];
            AddMoveOrder(Plan.MoveToWithin1000, wp, State, speedLimit, o|MoveOrder.DequeueWayPoint);

            // FB - Do not make final approach and stop, since the ship has more orders which don't
            // require stopping or rotating. Otherwise go to the set pos and not to the dynamic target planet center.
            if (!order.IsSet(MoveOrder.NoStop))
            {
                AddMoveOrder(Plan.MakeFinalApproach, wp, State, speedLimit, o, goal);
                AddMoveOrder(Plan.RotateToDesiredFacing, wp, State, 0, o, goal);
                OrderHoldPosition(position, finalDir, o);
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

            Planet closest = Owner.Loyalty.RallyShipYardNearestTo(Owner.Position);
            if (closest != null)
            {
                ResupplyTarget = closest;
                AddOrbitPlanetGoal(closest);
                return;
            }

            SolarSystem closestSystem = Owner.Loyalty.GetOwnedSystems().FindMin(s => s.Position.SqDist(Owner.Position));
            if (closestSystem != null)
            {
                ResupplyTarget = closestSystem.PlanetList[0];
                AddOrbitPlanetGoal(ResupplyTarget);
                return;
            }

            var emergencyPlanet = Owner.Universe.Planets.Filter(p => p.Owner == null);
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

                ResupplyTarget = Owner.Loyalty.GetPlanets().FindClosestTo(Owner, IsSafePlanet)
                             // fallback to any safe planet - this is a very rare case where no alternatives were found
                             ?? Owner.Universe.Planets.FindClosestTo(Owner, IsSafePlanet);

            if (ResupplyTarget != null)
                AddOrbitPlanetGoal(ResupplyTarget, AIState.Flee);
            else if (Owner.TryGetScoutFleeVector(out Vector2 pos)) // just get out of here
                OrderMoveToNoStop(pos, Owner.Direction.DirectionToTarget(pos), AIState.Flee);
            else
                ClearOrders(); // give up and resume combat

            // Local method
            bool IsSafePlanet(Planet p) => !p.ParentSystem.DangerousForcesPresent(Owner.Loyalty);
        }

        public void OrderOrbitPlanet(Planet p, bool clearOrders)
        {
            if (clearOrders)
            {
                ClearOrdersAndWayPoints();
            }

            Target = null;
            Intercepting = false;
            Owner.HyperspaceReturn();
            ResupplyTarget = p;
            AddOrbitPlanetGoal(p);
        }

        public void OrderPirateFleeHome(bool signalRetreat = false)
        {
            if (Owner.Loyalty.WeArePirates 
                && !Owner.IsPlatformOrStation 
                && Owner.Loyalty.Pirates.GetBases(out Array<Ship> pirateBases))
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
            if (Owner.Loyalty.WeAreRemnants
                && !Owner.IsPlatformOrStation
                && Owner.Loyalty.Remnants.GetClosestPortal(Owner.Position, out Ship portal))
            {
                OrderMoveToNoStop(portal.Position.GenerateRandomPointOnCircle(5000), Owner.Direction, AIState.MoveTo);
                AddEscortGoal(portal, clearOrders: false); // Orders are cleared in OrderMoveTo
            }
            else
            {
                OrderFlee();
            }
        }

        void OrderMoveToPirateBase(Ship pirateBase)
        {
            OrderMoveToNoStop(pirateBase.Position.GenerateRandomPointOnCircle(5000), Owner.Direction, AIState.MoveTo);
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
            if (Owner.Loyalty.GetRelations(toAttack.Loyalty, out Relationship relations))
            {
                if (!relations.Treaty_Peace)
                {
                    if (State == AIState.AttackTarget && Target == toAttack)
                        return;
                    if (State == AIState.SystemDefender && Target == toAttack)
                        return;
                    if (Owner.Weapons.Count == 0 || Owner.ShipData.Role == RoleName.troop)
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

            if (p.FreeTilesWithRebaseOnTheWay(Owner.Loyalty) == 0)
                return;

            IgnoreCombat = true;
            OrderMoveAndRebase(p);
        }

        public void OrderRebaseToNearest()
        {
            ClearWayPoints();

            if (Owner.Loyalty.WeArePirates)
            {
                OrderPirateFleeHome();
                return;
            }

            Planet planet = Owner.Loyalty.GetPlanets().Filter(p => p.FreeTilesWithRebaseOnTheWay(Owner.Loyalty) > 0)
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

            if (!Owner.Loyalty.isPlayer)
            {
                // bugfix: Avoid lingering fleets for the AI
                Owner.Fleet?.RemoveShip(Owner, returnToEmpireAI: false, clearOrders: clearOrders);
            }

            Target = null;
            OrbitTarget = toOrbit;
            AwaitClosest = toOrbit;
            AddResupplyPlanetGoal(toOrbit);

            if (Owner.TryGetEscapeVector(out Vector2 escapePos))
            {
                OrderResupplyEscape(escapePos, Owner.Direction);
            }
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
            Owner.Loyalty.GetEmpireAI().AddScrapShipGoal(Owner, immediateScuttle:false);
        }

        // Immediately self-destruct
        public void OrderScuttleShip()
        {
            Owner.Loyalty.GetEmpireAI().AddScrapShipGoal(Owner, immediateScuttle:true);
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
                AwaitClosest.Owner != Owner.Loyalty || Owner.System != system &&
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
                        if (p.Owner == null || p.Owner != Owner.Loyalty)
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
            if (Owner.ShipData.HullRole == RoleName.drone)
            {
                Owner.Die(null, true);
                return; // Drones never go to resupply, using hull role in case someone makes a drone module which changes the DesignRole
            }

            Planet nearestRallyPoint = Owner.Loyalty.RallyShipYardNearestTo(Owner.Position);
            DecideWhereToResupply(nearestRallyPoint, cancelOrders: cancelOrders);
        }

        public void OrderThrustTowardsPosition(Vector2 position, Vector2 direction, bool clearOrders)
        {
            if (clearOrders)
            {
                ClearWayPoints();
                ClearOrders();
            }
            OrderMoveTo(position, direction, AIState.MoveTo);
        }

        public void OrderToOrbit(Planet toOrbit, bool clearOrders, MoveOrder order = MoveOrder.Regular)
        {
            if (clearOrders)
            {
                ClearOrdersAndWayPoints();
            }

            // FB - this will give priority order for the movement. if offensiveMove is false,
            // it means the player ordered this specifically wanting combat ships to engage targets
            // of opportunity, even dropping our of warp to engage them.
            if (!order.IsSet(MoveOrder.Aggressive) || Owner.ShipData.ShipCategory == ShipCategory.Civilian)
            {
                // only order to move if we are too far, no need to waste time here.
                float threshold = toOrbit.ObjectRadius + 1000 * toOrbit.Scale;
                if (Owner.Position.Distance(toOrbit.Center) > threshold)
                {
                    Vector2 finalDir = Owner.Position.DirectionToTarget(toOrbit.Center);
                    OrderMoveToNoStop(toOrbit.Center, finalDir, AIState.MoveTo, order|MoveOrder.AddWayPoint);
                }
            }

            AddOrbitPlanetGoal(toOrbit);
        }

        bool SetAwaitClosestForSystemToDefend()
        {
            if (SystemToDefend == null)
                return false;
            AwaitClosest = SystemToDefend.PlanetList[0];
            return true;
        }

        bool SetAwaitClosestForFaction()
        {
            if (!Owner.Loyalty.isFaction)
                return false;

            AwaitClosest = Owner.System?.PlanetList.FindMax(p => p.FindNearbyFriendlyShips().Length);

            if (AwaitClosest == null)
            {
                var ships = Owner.Loyalty.OwnedShips;
                var solarSystem = ships.FindMinFiltered(ships.Count, ship => ship.System != null,
                                     ship => Owner.Position.SqDist(ship.Position))?.System;               

                AwaitClosest = solarSystem?.PlanetList.FindMax(p => p.FindNearbyFriendlyShips().Length);
                if (AwaitClosest == null)
                {
                    var system = Owner.Universe.Systems.FindMin(ss =>
                                 Owner.Position.SqDist(ss.Position) * (ss.OwnerList.Count + 1));
                    AwaitClosest = system?.PlanetList.FindClosestTo(Owner);
                }
                if (AwaitClosest == null)
                {
                    AwaitClosest = Owner.Universe.Planets.FindMin(p =>
                        p.Center.SqDist(Owner.Position));
                }
            }
            return AwaitClosest != null;

        }

        bool SetAwaitClosestForPlayer()
        {
            if (!Owner.Loyalty.isPlayer)
                return false;

            AwaitClosest = Owner.Loyalty.GetPlanets().FindMin(p => Owner.Position.SqDist(p.Center));
            return AwaitClosest != null;
        }

        bool SetAwaitClosestForAIEmpire()
        {
            if (Owner.Loyalty.isFaction || Owner.Loyalty.isPlayer)
                return false;

            SolarSystem home = Owner.System?.OwnerList.Contains(Owner.Loyalty) != true? null : Owner.System;
            if (home == null)
            {
                AwaitClosest = Owner.Loyalty.GetBestNearbyPlanetToOrbitForAI(Owner);

                if (AwaitClosest == null) //Find any system with no owners and planets.
                {
                    var system = Owner.Universe.Systems.FindMin(ss =>
                               Owner.Position.SqDist(ss.Position) * (ss.OwnerList.Count + 1));
                    if (system == null)
                        return false;

                    AwaitClosest = system.PlanetList.FindClosestTo(Owner);
                }
            }
            else
            {
                AwaitClosest = home.PlanetList.FindMinFiltered(p => p.Owner == Owner.Loyalty
                                                            , p => p.Center.SqDist(Owner.Position));
            }
            return AwaitClosest != null;
        }

        void AwaitOrders(FixedSimTime timeStep)
        {
            if (Owner.IsPlatformOrStation) 
                return;

            if (Owner.ShipData.IsCarrierOnly)
                return;

            if (State != AIState.Resupply)
                SetPriorityOrder(false);

            if (AwaitClosest != null)
            {
                if (SystemToDefend != null || Owner.Loyalty.isPlayer)
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
                    Planet p = Owner.Loyalty.GetEmpireAI().DefensiveCoordinator.AssignIdleShips(Owner);
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
                AwaitClosest = Owner.Loyalty.GetEmpireAI().GetKnownPlanets(Owner.Universe)
                    .FindMin(p => p.Center.SqDist(Owner.Position) + (Owner.Loyalty != p.Owner ? 300000 : 0));
                return;
            }
            if (Owner.System?.OwnerList.Contains(Owner.Loyalty) ?? false)
            {
                HadPO = false;
                return;
            }
            ReverseThrustUntilStopped(timeStep);
        }
    }
}