using System;
using SDUtils;
using Ship_Game.Commands.Goals;
using Ship_Game.ExtensionMethods;
using Ship_Game.Gameplay;
using Ship_Game.Ships;
using Ship_Game.Ships.AI;
using static Ship_Game.AI.ShipAI;
using Vector2 = SDGraphics.Vector2;

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

        // Forces ships to HoldPosition
        // WARNING: DO NOT USE THIS OUTSIDE OF TESTS, IT WILL CRIPPLE THE AI
        HoldPosition = (1 << 9),
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
        // if MoveOrder.StandGround, then PLAYER ships will have priority HoldPosition
        // if MoveOrder.HoldPosition, then ALL ships will have priority HoldPosition
        public void OrderHoldPosition(Vector2 position, Vector2 direction, MoveOrder order = MoveOrder.Regular)
        {
            AddMoveOrder(Plan.HoldPosition, new WayPoint(position, direction), AIState.HoldPosition, 0f, order);
            IgnoreCombat = false;

            if (order.IsSet(MoveOrder.StandGround) || order.IsSet(MoveOrder.HoldPosition))
            {
                // for players, StandGround is a hardline HoldPosition
                if (Owner.Loyalty.isPlayer || order.IsSet(MoveOrder.HoldPosition))
                {
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

        public void OrderAttackMoveTo(Vector2 to)
        {
            Vector2 finalDir = Owner.Position.DirectionToTarget(to);
            OrderMoveTo(to, finalDir, MoveOrder.Aggressive);
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

        public void OrderDeepSpaceBuild(DeepSpaceBuildGoal bg, float constructionNeeded, float buildRadius)
        {
            ClearOrders(State, priority:true);
            Vector2 pos = bg.BuildPosition;
            Vector2 dir = Owner.Position.DirectionToTarget(pos);
            Owner.Construction = ConstructionShip.Create(Owner, constructionNeeded, buildRadius);
            if (bg.IsBuildingOrbitalFor(bg.TargetPlanet)) // orbitals for planet defense or research stations
                AddShipGoal(Plan.DeployOrbital, pos, dir, bg, bg.ToBuild.Name, 0f, AIState.MoveTo);
            else // deep space structures
                AddShipGoal(Plan.DeployStructure, pos, dir, bg, bg.ToBuild.Name, 0f, AIState.MoveTo);
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

        public void OrderAttackPriorityTarget(Ship target)
        {
            HasPriorityTarget = true;
            Target = target;
            EnterCombatState(AIState.AttackTarget);
        }

        void DoFindExterminationTarget(FixedSimTime timeStep, ShipGoal goal)
        {
            Planet toExterminate = null;
            if (ExterminationTarget != null)
            {
                if (ExterminationTarget.Owner == null)
                {
                    toExterminate = Owner.Universe.Planets.FindClosestTo(Owner.Position, p => p.Owner != null);
                }
                else if (OrderQueue.Count <= 1)
                {
                    toExterminate = ExterminationTarget;
                }
            }

            if (toExterminate != null)
            {
                ClearOrdersAndWayPoints();
                AddPlanetGoal(Plan.Exterminate, toExterminate, AIState.Exterminate);
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
            if (Owner.IsSingleTroopShip || Owner.IsDefaultAssaultShuttle ||  Owner.Carrier.AnyAssaultOpsAvailable)
            {
                // This deals also with single Troop Ships / Assault Shuttles
                AddPlanetGoal(Plan.LandTroop, target, AIState.AssaultPlanet);
            }
        }

        public void OrderMoveTo(Vector2 position, Vector2 finalDir, MoveOrder order = MoveOrder.Regular)
        {
            AddWayPoint(position, finalDir, AIState.MoveTo, order, 0f, null);
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
            else if (order.IsSet(MoveOrder.StandGround)) o |= MoveOrder.StandGround;
            else if (order.IsSet(MoveOrder.Regular))     o |= MoveOrder.Regular;

            // FB - if offensive move is true, ships will break and attack targets on the way to the destination
            bool offensiveMove = order.IsSet(MoveOrder.Aggressive);
            ClearOrders(wantedState, priority: !offensiveMove);

            MovePosition = position;

            // WARNING: please don't 'FIX' anything here without caution and testing.
            //   Checklist: single ship move & queued move,
            //              fleet move & queued move,
            //              ship group move & queued move,
            //              priority movement for all of the above while in combat
            //              Verify ships can complete move to planet goals like colonization.
            WayPoint[] wayPoints = WayPoints.EnqueueAndToArray(new WayPoint(position, finalDir));

               /////////////////////////////////////////////////////////////////
              ////// --               FINAL WARNING                   -- //////
             ////// -- DO NOT MODIFY ANY OF THIS WITHOUT CODE REVIEW -- //////
            ////// --    IT'S INCREDIBLY EASY TO FUBAR THIS CODE!   -- //////
            ////// --                                               -- //////
             ////// --  IF AND WHEN YOU FUBAR THIS ANYWAY INCREASE   -- //////
              ////// --       THIS COUNTER AS WARNING TO OTHERS       -- //////
               ////// --        total_hours_wasted_here = 114          -- //////
                /////////////////////////////////////////////////////////////////
            AddMoveOrder(Plan.RotateToFaceMovePosition, wayPoints[0], wantedState, speedLimit, o);

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
                    AddMoveOrder(Plan.MoveToWithin1000, wp, wantedState, speedLimit, o|MoveOrder.DequeueWayPoint);
                    AddMoveOrder(Plan.MakeFinalApproach, wp, wantedState, speedLimit, o, goal);
                    (Vector2 dirToNext, float dist) = wp.Position.GetDirectionAndLength(wayPoints[i + 1].Position);
                    Vector2 nextPos = wp.Position + dirToNext*Math.Min(1000f, dist*0.25f);
                    AddMoveOrder(Plan.RotateToFaceMovePosition, new WayPoint(nextPos, dirToNext), wantedState, speedLimit, o, goal);
                }
                else
                {
                    AddMoveOrder(Plan.MoveToWithin1000, wp, wantedState, speedLimit, o|MoveOrder.DequeueWayPoint);
                }
            }

            wp = wayPoints[wayPoints.Length - 1];
            AddMoveOrder(Plan.MoveToWithin1000, wp, wantedState, speedLimit, o|MoveOrder.DequeueWayPoint);

            // FB - Do not make final approach and stop, since the ship has more orders which don't
            // require stopping or rotating. Otherwise go to the set pos and not to the dynamic target planet center.
            if (!order.IsSet(MoveOrder.NoStop))
            {
                AddMoveOrder(Plan.MakeFinalApproach, wp, wantedState, speedLimit, o, goal);
                AddMoveOrder(Plan.RotateToDesiredFacing, wp, wantedState, 0, o, goal);
                OrderHoldPosition(position, finalDir, o);
            }

            // finally, we need to set the current AIState, because everything else modified it -_-
            ChangeAIState(wantedState);
        }

        public void OrderAwaitOrders(bool clearPriorityOrder = true)
        {
            AddShipGoal(Plan.AwaitOrders, AIState.AwaitingOrders);
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

            Planet closest = Owner.Loyalty.FindNearestSpacePort(Owner.Position);
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
            emergencyPlanet.Sort(p => p.Position.SqDist(Owner.Position));
            SetOrbitTarget(emergencyPlanet[0]);
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
            bool IsSafePlanet(Planet p) => !p.System.DangerousForcesPresent(Owner.Loyalty);
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
                OrderMoveToPirateBase(Owner.Loyalty.Pirates, ship);
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

        public void OrderRemnantFlee(Remnants remnants)
        {
            if (!Owner.IsPlatformOrStation
                && remnants.GetClosestPortal(Owner.Position, out Ship portal))
            {
                OrderMoveToNoStop(portal.Position.GenerateRandomPointOnCircle(5000, remnants.Random), Owner.Direction, AIState.MoveTo);
                AddEscortGoal(portal, clearOrders: false); // Orders are cleared in OrderMoveTo
            }
            else
            {
                OrderFlee();
            }
        }

        void OrderMoveToPirateBase(Pirates pirates, Ship pirateBase)
        {
            OrderMoveToNoStop(pirateBase.Position.GenerateRandomPointOnCircle(5000, pirates.Random), Owner.Direction, AIState.MoveTo);
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
                    
                    ChangeAIState(AIState.AttackTarget);
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

            var planets = Owner.Loyalty.GetPlanets();
            if (planets.Count == 0)
            {
                if (Owner.Loyalty.IsFaction)
                {
                    OrderScuttleShip();
                    return;
                }

                planets = Owner.Loyalty.Universe.Planets;
            }

            Planet planet = planets.FindClosestTo(Owner.Position, p => p.FreeTilesWithRebaseOnTheWay(Owner.Loyalty) > 0);
            if (planet == null)
            {
                ChangeAIState(AIState.AwaitingOrders);
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

            /* // FB - traying again without it after code changes
            if (!Owner.Loyalty.isPlayer)
            {
                // bugfix: Avoid lingering fleets for the AI
                Owner.Fleet?.RemoveShip(Owner, returnToEmpireAI: false, clearOrders: clearOrders);
            }*/

            Target = null;
            SetOrbitTarget(toOrbit);
            AwaitClosest = toOrbit;
            AddPlanetGoal(Plan.Orbit, toOrbit, AIState.Resupply, pushToFront: true);

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

        public void OrderBuilderReturnHome(Planet planet)
        {
            ClearOrders(priority: true);
            AddShipGoal(Plan.BuilderReturnHome, AIState.SupplyReturnHome, 
                planet.GetBuilderShipTargetVector(launch: false, out _), planet, true);
        }

        // Move to closest colony and get back some resources
        public void OrderScrapShip()
        {
            Owner.Loyalty.AI.AddScrapShipGoal(Owner, immediateScuttle:false);
        }

        // Immediately self-destruct
        public void OrderScuttleShip()
        {
            Owner.Loyalty.AI.AddScrapShipGoal(Owner, immediateScuttle:true);
        }

        public void AddSupplyShipGoal(Ship supplyTarget, Plan plan = Plan.SupplyShip)
        {
            ClearOrders();
            IgnoreCombat = true;
            //Clearorders wipes stored ordnance data if state is ferrying.
            EscortTarget = supplyTarget;
            AddShipGoal(plan, AIState.Ferrying);
        }

        public void AddBuildOrbitalGoal(Planet targetPlanet, Ship targetConstructor)
        {
            ClearOrders();
            IgnoreCombat = true;
            EscortTarget = targetConstructor;
            AddShipGoal(Plan.BuildOrbital, targetPlanet, AIState.Ferrying, targetConstructor);
        }

        public void OrderMinePlanet(Planet targetPlanet)
        {
            ClearOrders();
            IgnoreCombat = true;
            float distance = targetPlanet.Random.Float(targetPlanet.Mining.MinMiningRadius,
                                                       targetPlanet.Mining.MaxMiningRadius);

            Vector2 pos = targetPlanet.Position.GenerateRandomPointOnCircle(distance, targetPlanet.Random);
            AddShipGoal(Plan.MinePlanet, AIState.Mining, pos, targetPlanet, true);
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
                        AwaitClosest = Random.Item(potentials);
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

            Planet nearestRallyPoint = Owner.Loyalty.FindNearestSpacePort(Owner.Position);
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
                float threshold = toOrbit.Radius + 1000 * toOrbit.Scale;
                if (Owner.Position.Distance(toOrbit.Position) > threshold)
                {
                    Vector2 finalDir = Owner.Position.DirectionToTarget(toOrbit.Position);
                    OrderMoveToNoStop(toOrbit.Position, finalDir, AIState.MoveTo, order|MoveOrder.AddWayPoint);
                }
            }

            AddOrbitPlanetGoal(toOrbit);
        }

        Planet GetAwaitClosest()
        {
            Planet closest = null;
            if (SystemToDefend != null && SystemToDefend.PlanetList.NotEmpty)
            {
                closest = SystemToDefend.PlanetList[0];
                if (closest != null)
                    return closest;
            }

            Empire e = Owner.Loyalty;
            
            if (!e.IsFaction && !e.isPlayer)
            {
                SolarSystem currentFriendlySys = Owner.System?.OwnerList.Contains(e) != true ? null : Owner.System;
                if (currentFriendlySys != null)
                    closest = e.FindNearestRallyPlanetInSystem(Owner.Position, currentFriendlySys);

                if (closest != null)
                    return closest;
            }

            // Empire.UpdateRallyPoints() handles the logic for setting up rally points
            closest = e.FindNearestRallyPoint(Owner.Position);

            // this should never be null! if this happens, then UpdateRallyPoints() has a regression
            if (closest == null)
                Log.Error($"GetAwaitClosest returned null, Empire={e.Name}");

            return closest;
        }

        void DoAwaitOrders(FixedSimTime timeStep, ShipGoal goal)
        {
            if (Owner.Loyalty.isPlayer)
                AwaitOrdersPlayer(timeStep);
            else
                AwaitOrdersAIControlled(timeStep);
        }

        void AwaitOrdersAIControlled(FixedSimTime timeStep)
        {
            if (Owner.IsPlatformOrStation || Owner.IsSubspaceProjector) 
                return;

            if (Owner.ShipData.IsCarrierOnly)
            {
                // BUG Drifting Ships with AwaitOrders and no ShipGoals
                // FIX: if mothership is alive, return to base,
                //      otherwise resume AwaitOrders logic
                if (Owner.Mothership?.Active == true)
                {
                    OrderReturnToHangar();
                    return;
                }
            }

            if (State != AIState.Resupply)
                SetPriorityOrder(false);

            if (AwaitClosest != null)
            {
                if (SystemToDefend != null || Owner.Loyalty.isPlayer)
                {
                    Orbit.Orbit(AwaitClosest, timeStep);
                    return;
                }
                if (AwaitClosest.System.OwnerList.Count > 0)
                    AwaitClosest = null;
            }

            AwaitClosest ??= GetAwaitClosest();
            if (AwaitClosest != null) 
            {
                Orbit.Orbit(AwaitClosest, timeStep);
            }
        }

        // TODO: refactor this and check overlap with `AwaitOrdersAIControlled`
        void AwaitOrdersPlayer(FixedSimTime timeStep)
        {
            SetPriorityOrder(false);

            if (EscortTarget != null)
            {
                ChangeAIState(AIState.Escort);
                return;
            }

            if (!HadPO)
            {
                if (AwaitClosest != null)
                {
                    Orbit.Orbit(AwaitClosest, timeStep);
                    return;
                }
                AwaitClosest = Owner.Loyalty.AI.GetKnownPlanets(Owner.Universe)
                    .FindMin(p => p.Position.SqDist(Owner.Position) + (Owner.Loyalty != p.Owner ? 300000 : 0));
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