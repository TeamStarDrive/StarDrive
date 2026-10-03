using Ship_Game.Audio;
using Ship_Game.Gameplay;
using Ship_Game.Ships;
using System;
using System.Linq;
using SDGraphics;
using SDUtils;
using Ship_Game.ExtensionMethods;
using static Ship_Game.AI.CombatStanceType;
using Vector2 = SDGraphics.Vector2;
using Ship_Game.Commands.Goals;
using Ship_Game.Universe;

namespace Ship_Game.AI
{
    public sealed partial class ShipAI
    {
        StanceType CombatRangeType => ToStanceType(CombatState);

        // For per-tick handlers that orbit/thrust to a planet (Bombard / Exterminate / Orbit /
        // Flee-to-safe-planet / Resupply): if the goal still has unconsumed detours and we're
        // far from the target, head to the next detour instead of straight at the planet.
        // Returns true when we short-circuited; caller should skip its orbit/bomb step this tick.
        bool TryRouteToOrbitTarget(ShipGoal goal, FixedSimTime timeStep)
        {
            if (goal.Detours == null || goal.Detours.Length == 0 || goal.TargetPlanet == null)
                return false;

            Vector2 target = goal.TargetPlanet.Position;
            Vector2 thrust = goal.GetThrustTarget(target, Owner.Position);
            if (thrust == target)
                return false; // all detours consumed, fall through to normal orbit/thrust

            ThrustOrWarpToPos(thrust, timeStep);
            return true;
        }

        void DoOrbit(FixedSimTime timeStep, ShipGoal goal)
        {
            if (!TryRouteToOrbitTarget(goal, timeStep))
                Orbit.Orbit(goal.TargetPlanet, timeStep);
        }

        void DoBoardShip(FixedSimTime timeStep)
        {
            HasPriorityTarget = true;
            ChangeAIState(AIState.Boarding);
            var escortTarget = EscortTarget;
            if (Owner.TroopCount < 1 || escortTarget == null || escortTarget.IsDeadOrDying || escortTarget.IsLaunchingOrLanding
                || escortTarget.Loyalty == Owner.Loyalty)
            {
                ClearOrders(State);
                if (Owner.IsHangarShip)
                {
                    if (Owner.Mothership.Carrier.TroopsOut)
                        Owner.DoEscort(Owner.Mothership);
                    else
                        OrderReturnToHangar();
                }
                return;
            }

            ThrustOrWarpToPos(escortTarget.Position, timeStep);
            float distance = Owner.Position.Distance(escortTarget.Position);
            if (distance < LandShip.BoardingRange(escortTarget))
            {
                TryLandOnShip(escortTarget);
            }
            else if (distance > 10000f && Owner.Mothership?.AI.CombatState == CombatState.AssaultShip)
            {
                OrderReturnToHangar();
            }
        }

        public bool IsTargetValid(Ship ship)
        {
            if (ship == null)
                return false;

            if (Owner.IsResearchStation && !Owner.Loyalty.IsAtWarWith(ship.Loyalty))
                return false; // prevent neutral research station from shooting at eachother

            if (ship.Active && !ship.Dying && !ship.IsInWarp &&
                Owner.Loyalty.IsEmpireAttackable(ship.Loyalty, ship))
                return true;

            return Owner.Loyalty.isPlayer 
                   && HasPriorityTarget 
                   && ship.InPlayerSensorRange
                   && !Owner.Loyalty.IsAlliedWith(ship.Loyalty);
        }

        Ship UpdateCombatTarget()
        {
            if (IsTargetValid(Target))
                return Target;
            
            if (State == AIState.Pursue)
                DequeueCurrentOrder();

            Target = PotentialTargets.Find(t => IsTargetValid(t)
                                                && t.Position.InRadius(Owner.Position, Owner.SensorRange));
            return Target;
        }

        void DoCombat(FixedSimTime timeStep)
        {
            Ship target = UpdateCombatTarget();
            if (target == null)
            {
                // this check here, in-case exit auto-combat logic already triggered
                if (Owner.InCombat)
                {
                    ExitCombatState();
                    if (Owner.IsHangarShip)
                        BackToCarrier();
                }
                return;
            }

            AwaitClosest = null; // TODO: Why is this set here?

            if (!HasPriorityOrder && !HasPriorityTarget && Owner.Weapons.Count == 0 && !Owner.Carrier.HasActiveHangars)
                CombatState = CombatState.Evade;

            float distanceToTarget = target.Position.Distance(Owner.Position);

            // if ship has priority order, then move towards that order, EVEN when in combat
            if (HasPriorityOrder)
            {
                MoveToEngageTarget(target, timeStep);
            }
            // if we're outside of 7500 and also of desired combat range (which can be huge for carriers) then move towards target
            else if (distanceToTarget > 7500f && distanceToTarget > Owner.DesiredCombatRange)
            {
                MoveToEngageTarget(target, timeStep);
            }
            else // we are in range:
            {
                // Disengage from warp if we get close enough to the target
                // For big ships this can be 110% of DesiredCombatRange,
                // but also ensure we are far enough to prevent warping on top of enemy fleets
                //   which can be a detriment and also an exploit by players
                if (Owner.engineState == Ship.MoveState.Warp &&
                    (distanceToTarget < 7500f || distanceToTarget < Owner.DesiredCombatRange * 1.1f))
                {
                    Owner.HyperspaceReturn();
                }

                if (FleetNode != null && Owner.Fleet != null && HasPriorityOrder)
                {
                    // TODO: need to move this into fleet.
                    if (Owner.Fleet.FleetTask == null)
                    {
                        Vector2 nodePos = Owner.Fleet.AveragePosition() + FleetNode.RelativeFleetOffset;
                        if (target.Position.OutsideRadius(nodePos, FleetNode.OrdersRadius))
                        {
                            if (Owner.Position.OutsideRadius(nodePos, 1000f))
                            {
                                ThrustOrWarpToPos(nodePos, timeStep);
                            }
                            else
                            {
                                DoHoldPositionCombat(timeStep);
                            }
                            return;
                        }
                    }
                    else
                    {
                        var task = Owner.Fleet.FleetTask;
                        if (target.Position.OutsideRadius(task.AO, Math.Max(task.AORadius, FleetNode.OrdersRadius)))
                        {
                            DequeueCurrentOrder();
                        }
                    }
                }

                if (Intercepting && CombatRangeType == StanceType.RangedCombatMovement)
                {
                    // clamp the radius here so that it wont flounder if the ship has very long range weapons.
                    float radius = Owner.DesiredCombatRange * 3f;
                    if (Owner.Position.OutsideRadius(target.Position, radius))
                    {
                        ThrustOrWarpToPos(target.Position, timeStep);
                        return;
                    }
                    if (distanceToTarget < Owner.DesiredCombatRange)
                        Intercepting = false;
                }

                CombatAI.ExecuteCombatTactic(timeStep);
                Owner.Carrier.TryAssaultShipCombat();
            }
        }

        void MoveToEngageTarget(Ship target, FixedSimTime timeStep)
        {
            if (!Owner.Loyalty.isPlayer && ShouldTryOvertakeTarget() && !Owner.IsInhibitedByUnfriendlyGravityWell)
            {
                Vector2 prediction = target.Position.GenerateRandomPointOnForwardHalfArc(target.Rotation, 2000, Owner.Loyalty.Random)
                    + (target.Direction * (target.CurrentVelocity * 4 + 7500 + Owner.Radius*5));

                if (Owner.Position.Distance(prediction) > 15_000)
                    Owner.AI.OrderMoveToNoStop(prediction, target.Direction, AIState.Pursue, MoveOrder.Pursue);

                return;
            }

            // TODO: ADD fleet formation warp logic here. 
            if (CombatRangeType == StanceType.RangedCombatMovement )
            {
                Vector2 prediction = target.Position;
                Weapon fastestWeapon = Owner.FastestWeapon;
                if (fastestWeapon != null && target.CurrentVelocity > 0) // if we have a weapon
                {
                    float distance = Owner.Position.Distance(target.Position);
                    if (distance < 7500)
                        prediction = fastestWeapon.ProjectedImpactPointNoError(target);
                }

                ThrustOrWarpToPos(prediction, timeStep);
            }
            else
            {
                ThrustOrWarpToPos(Target.Position, timeStep);
            }

            bool ShouldTryOvertakeTarget()
            {
                // A stationary ship (station/platform) can't overtake anything; issuing a Pursue
                // priority-order move only drops it out of combat (it never reaches the prediction
                // point). This is what left Remnant portals oscillating Combat<->Pursue instead of
                // fighting in place.
                if (Owner.IsInWarp || target.IsInWarp || Owner.Stats.IsStationary || Owner.Loyalty.Random.RollDice(95 - Owner.Level))
                    return false;

                float distance = Owner.Position.Distance(target.Position);
                return distance < 15_000 && distance > Owner.WeaponsMaxRange * 0.7f && Owner.MaxSTLSpeed < target.CurrentVelocity;
            }
        }

        void DoMinePlanet(FixedSimTime timeStep, ShipGoal g)
        {
            if (Owner.Mothership == null || !Owner.Mothership.Active)
            {
                OrderScuttleShip();
                return;
            }

            Planet planet = g.TargetPlanet;
            Vector2 wantedPos = g.MovePosition;
            if (wantedPos.OutsideRadius(Owner.Position, Owner.MaxSTLSpeed * 0.5f))
            {
                ThrustOrWarpToPos(wantedPos, timeStep, Owner.STLSpeedLimit);
                return;
            }

            ReverseThrustUntilStopped(timeStep);
            float miningRate = planet.Mining.Richness * timeStep.FixedTime 
                                                      * Owner.Loyalty.data.MiningSpeedMultiplier 
                                                      / Owner.Universe.P.TurnTimer;
            Owner.LoadCargo(planet.Mining.CargoId, miningRate);
            if (Owner.CargoSpaceFree == 0)
            {
                ClearOrders();
                Owner.InitLaunch(LaunchPlan.MinerReturn, Owner.Rotation.ToDegrees());
                OrderReturnToHangar();
            }
            else if (Owner.CargoSpaceFree > planet.Mining.Richness 
                && Owner.Position.OutsideRadius(planet.Position, planet.Mining.MaxMiningRadius*1.25f))
            {
                OrderMinePlanet(planet);
            }
        }

        void DoDeploy(ShipGoal g, FixedSimTime timeStep)
        {
            Goal goal = g.Goal;
            if (goal is not DeepSpaceBuildGoal bg)
                return;

            Planet target = g.TargetPlanet;
            if (target == null && bg.TetherPlanet != null)
            {
                target = bg.TetherPlanet;
                if (target == null 
                    || target.IsMineable && target.Mining.Owner != null && target.Mining.Owner != Owner.Loyalty) 
                {
                    OrderScrapShip();
                    return;
                }
            }

            if (goal is RefitOrbital && goal.OldShip?.Active == false)
            {
                OrderScrapShip();
                return;
            }

            if (goal.BuildPosition.OutsideRadius(Owner.Position, (Owner.CurrentVelocity *2).UpperBound(500)))
            {
                ThrustOrWarpToPos(bg.GetThrustTarget(Owner.Position), timeStep);
                return;
            }

            ReverseThrustUntilStopped(timeStep);
            Owner.Construction.AddConstructionEffects(bg.TetherPlanet);
            if (!Owner.Construction.ConsturctionCompleted)
                return;

            Owner.Construction.Complete();
            Ship orbital = Ship.CreateShipAtPoint(Owner.Universe, bg.ToBuild.Name, Owner.Loyalty, goal.BuildPosition);
            if (orbital == null)
                return;

            if (orbital.IsSubspaceProjector)
                Owner.Loyalty.AI.SpaceRoadsManager.AddProjectorToRoadList(orbital, goal.BuildPosition);

            if (orbital.IsDysonSwarmController)
            {
                if (!Owner.System.EmpireOwnsDysonSwarm(Owner.Loyalty) 
                    || !Owner.System.DysonSwarm.TryConnectControllerToGrid(orbital))
                {
                    orbital.QueueTotalRemoval();
                }
            }

            Owner.QueueTotalRemoval();
            if (goal.OldShip?.Active == true) // we are refitting something
            {
                goal.OldShip.TransferCargoUponRefit(orbital);
                goal.OldShip.QueueTotalRemoval();
            }

            if (bg.TetherPlanet != null)
            {
                Planet planetToTether = bg.TetherPlanet;
                orbital.TetherToPlanet(planetToTether);
                orbital.TetherOffset = bg.TetherOffset;
                UpdateResearchStationGoal(orbital, bg.TetherPlanet, goal.OldShip);
                UpdateMiningOpsGoal(orbital, bg.TetherPlanet, goal.OldShip);
                if (planetToTether.IsOverOrbitalsLimit(orbital.ShipData))
                    planetToTether.TryRemoveExcessOrbital(orbital);
            }
            else
            {
                UpdateResearchStationGoal(orbital, Owner.System, goal.OldShip);
            }
        }

        void DoDeployOrbital(ShipGoal g, FixedSimTime timeStep)
        {
            Goal goal = g.Goal;
            if (goal is not DeepSpaceBuildGoal bg)
            {
                Log.Info("There was no goal for Construction ship deploying orbital");
                OrderScrapShip();
                return;
            }

            Planet target = bg.TetherPlanet;
            if (target == null 
                || target.Owner != Owner.Loyalty
                || target.IsMineable && target.Mining.OpsOwnedBySomeoneElseThan(Owner.Loyalty))
            {
                OrderScrapShip(); // Planet or Mining ops owner was changed
                return;
            }

            if (goal is RefitOrbital && goal.OldShip?.Active == false)
            {
                OrderScrapShip();
                return;
            }

            if (goal.BuildPosition.OutsideRadius(Owner.Position, (Owner.CurrentVelocity * 2).UpperBound(500)))
            {
                ThrustOrWarpToPos(bg.GetThrustTarget(Owner.Position), timeStep);
                return;
            }

            ReverseThrustUntilStopped(timeStep);
            Owner.Construction.AddConstructionEffects(bg.TetherPlanet);
            if (!Owner.Construction.ConsturctionCompleted)
                return;

            Owner.Construction.Complete();
            Ship orbital = Ship.CreateShipAtPoint(Owner.Universe, bg.ToBuild.Name, Owner.Loyalty, goal.BuildPosition);
            if (orbital != null)
            {
                orbital.Position = goal.BuildPosition;
                orbital.TetherToPlanet(target);
                Owner.QueueTotalRemoval();
                if (goal.OldShip?.Active == true) // we are refitting something
                {
                    goal.OldShip.TransferCargoUponRefit(orbital);
                    goal.OldShip.QueueTotalRemoval();
                }
                else
                {
                    target.TryRemoveExcessOrbital(orbital);
                }

                UpdateResearchStationGoal(orbital, target, goal.OldShip);
                UpdateMiningOpsGoal(orbital, target, goal.OldShip);
            }
        }

        void UpdateResearchStationGoal(Ship orbital, ExplorableGameObject target, Ship oldShipToRefit)
        {
            if (!orbital.IsResearchStation)
                return;

            Goal goal = Owner.Loyalty.AI.FindGoal(g => g.IsResearchStationGoal(target)
                                                  && (oldShipToRefit == null
                                                      ? g.StepName == "WaitForConstructor"
                                                            && g.TargetShip == null
                                                            && g.ToBuild?.Name == orbital.Name
                                                      : g.TargetShip == oldShipToRefit));
            if (goal != null)
            {
                goal.TargetShip = orbital;
                Owner.Universe.AddEmpireToResearchableList(Owner.Loyalty, target);
            }
        }

        void UpdateMiningOpsGoal(Ship orbital, Planet planet, Ship oldShipToRefit)
        {
            if (!orbital.IsMiningStation)
                return;

            Goal goal = Owner.Loyalty.AI.FindGoal(g => g.IsMiningOpsGoal(planet)
                                                  && (oldShipToRefit == null
                                                      ? g.StepName == "WaitForConstructor" 
                                                            && g.TargetShip == null 
                                                            && g.ToBuild?.Name == orbital.Name
                                                      : g.TargetShip == oldShipToRefit));

            if (goal != null)
                goal.TargetShip = orbital;

            if (!planet.Mining.HasOpsOwner)
                planet.Mining.ChangeOwnershipIfNeeded(Owner.Loyalty);
        }

        // Gravity-well routing for the explore legs. The explore loop thrusts directly at the
        // target system/planet, so without this it beelines straight THROUGH intervening known
        // systems and gets warp-inhibited crossing them (e.g. a scout cutting through Aphronn on
        // the way to a distant target). Mirrors the goal/trade detour pattern: compute a detour
        // chain once per leg (keyed on the target object so orbiting planets don't force a
        // recompute every tick), then walk it via GravityWellRouter.GetThrustTarget.
        // Deliberately not [StarData]: a mid-flight save/load just clears the cache and the
        // next tick recomputes a fresh chain from the current position.
        object ExploreDetourTarget;
        Vector2[] ExploreDetours;
        int ExploreDetourIndex;
        Vector2 ExploreThrustTarget(object legTarget, Vector2 dest)
        {
            if (!ReferenceEquals(ExploreDetourTarget, legTarget))
            {
                ExploreDetourTarget = legTarget;
                ExploreDetours = GravityWellRouter.BuildDetours(Owner, Owner.Position, dest, MoveOrder.Regular);
                ExploreDetourIndex = 0;
            }
            return GravityWellRouter.GetThrustTarget(ExploreDetours, ref ExploreDetourIndex, dest, Owner.Position);
        }

        // Test seam for the explore gravity-well routing wrapper (see TestGravityWellRouter).
        public Vector2 TestExploreThrustTarget(object legTarget, Vector2 dest) => ExploreThrustTarget(legTarget, dest);

        public void DoExplore(FixedSimTime timeStep)
        {
            SetPriorityOrder(true);
            IgnoreCombat = true;
            if (ExplorationTarget == null)
            {
                if (!Owner.Loyalty.AI.ExpansionAI.AssignExplorationTargetSystem(Owner, out ExplorationTarget))
                    ClearOrders(); // FB - could not find a new system to explore
            }
            else if (DoExploreSystem(timeStep))
            {
                Owner.Loyalty.AI.ExpansionAI.RemoveExplorationTargetFromList(ExplorationTarget);
                ExplorationTarget.AddSystemExploreSuccessMessage(Owner.Loyalty);
                ExplorationTarget = null;
            }
        }

        bool DoExploreSystem(FixedSimTime timeStep)
        {
            if (Owner.System != null 
                && BadGuysNear 
                && Owner.System.ShipList.Any(s => s.AI.Target == Owner)
                && !Owner.IsInWarp)
            {
                ClearOrders();
                if (Owner.TryGetScoutFleeVector(out Vector2 escapePos))
                {
                    OrderMoveTo(escapePos, Owner.Direction.DirectionToTarget(escapePos), AIState.Flee, MoveOrder.NoStop);
                    Vector2 secondaryPos = escapePos.GenerateRandomPointOnCircle(20_000, Random);  
                    OrderMoveTo(secondaryPos, escapePos.DirectionToTarget(secondaryPos), AIState.Flee, MoveOrder.NoStop | MoveOrder.AddWayPoint);
                    AddShipGoal(Plan.Explore, AIState.Explore); // Add a new exploration order to the queue to fall back to after flee is done
                }
                else
                {
                    OrderFlee();
                }

                return false;
            }

            if (!ExplorationTarget.IsExploredBy(Owner.Loyalty))
            {
                // First we explore the star, since we don't know what is in the system yet.
                DoExploreUnknownSystem(timeStep, ExplorationTarget);
                if (!ExplorationTarget.IsExploredBy(Owner.Loyalty))
                    return false;
            }

            // We explored the Star, now we can proceed to the planets
            if (!TryGetClosestUnexploredPlanet(ExplorationTarget, out PatrolTarget))
            {
                ExplorationTarget.UpdateFullyExploredBy(Owner.Loyalty);
                return true; // All planets explored
            }

            MovePosition = PatrolTarget.Position;
            {
                ThrustOrWarpToPos(ExploreThrustTarget(PatrolTarget, MovePosition), timeStep);
                if (Owner.Position.InRadius(MovePosition, Owner.ExplorePlanetDistance))
                {
                    PatrolTarget.SetExploredBy(Owner.Loyalty);
                    Owner.AddExoticFoundNotification(PatrolTarget);
                }
            }

            return false;
        }

        void DoExploreUnknownSystem(FixedSimTime timeStep, SolarSystem system)
        {
            MovePosition = system.Position;
            // We are doing faster updates that the 1 per second Explore of normal ships
            // Since we are now actively scouting the system
            if (Owner.Position.InRadius(MovePosition, Owner.ExploreSystemDistance))
            {
                if (Owner.Loyalty.isPlayer && system.IsResearchable && !system.IsExploredBy(Owner.Loyalty))
                    Owner.Universe.Screen.NotificationManager?.AddReseachableStar(system);

                system.SetExploredBy(Owner.Loyalty);
                return;
            }

            ThrustOrWarpToPos(ExploreThrustTarget(system, MovePosition), timeStep);
        }

        void DoHoldPositionCombat(FixedSimTime timeStep)
        {
            if (Owner.CurrentVelocity > 0f)
            {
                ReverseThrustUntilStopped(timeStep);
                Vector2 interceptPoint = Owner.PredictImpact(Target);
                RotateTowardsPosition(interceptPoint, timeStep, 0.2f);
            }
            else
            {
                RotateTowardsPosition(Target.Position, timeStep, 0.2f);
            }
        }

        void DoRebase(FixedSimTime timeStep, ShipGoal goal)
        {
            DoLandTroop(timeStep, goal);
        }

        internal Vector2 LandingOffset;

        void DoLandTroop(FixedSimTime timeStep, ShipGoal goal)
        {
            Planet planet = goal.TargetPlanet;
            if (planet.Owner != null 
                && planet.Owner != Owner.Loyalty 
                && !Owner.Loyalty.IsAtWarWith(planet.Owner))
            {
                AbortLandNoFleet(planet);
                return;
            }

            if (LandingOffset.AlmostZero())
                LandingOffset = FindLandingOffset(planet);

            Vector2 landingSpot = planet.Position + LandingOffset;
            if (Owner.IsDefaultAssaultShuttle || Owner.IsDefaultTroopShip)
            {
                if (!Owner.IsHangarShip && LandShip.LandsOnSpacePort(LandPlan.Troops, planet, Owner.Loyalty))
                {
                    FlyInToLand(timeStep, goal, planet, LandPlan.Troops);
                    return;
                }

                // force the ship out of warp if we get too close
                // this is a balance feature
                ThrustOrWarpToPos(goal.GetThrustTarget(landingSpot, Owner.Position), timeStep, warpExitDistance: Owner.WarpOutDistance);
                LandTroopsViaSingleTransport(planet, landingSpot);
            }
            else
            {
                LaunchShuttlesFromTroopShip(timeStep, planet, landingSpot, goal);
            }
        }

        // Assault Shuttles dive to drop their troop and climb back, a carrier's shuttle then flies back to its hangar
        // Single Troop Ships land on the planet, and the ship is spent once its troop lands
        void LandTroopsViaSingleTransport(Planet planet, Vector2 landingSpot)
        {
            bool dives = Owner.IsDefaultAssaultShuttle || Owner.IsHangarShip;
            float range = Owner.Radius + 40f;
            if (dives && Owner.Direction.Dot(Owner.Position.DirectionToTarget(landingSpot)) > 0.98f)
                range += LandShip.DiveDistance(Owner);

            if (CanStartLanding(landingSpot, range))
                Owner.InitLanding(dives ? LandPlan.AssaultDive : LandPlan.Troops, planet);
        }

        public void FlyOnAfterTroopLanding(Planet planet)
        {
            if (Owner.IsHangarShip && Owner.Mothership.Active)
            {
                OrderReturnToHangar();
                return;
            }

            if (OrderQueue.TryPeekFirst(out ShipGoal goal) && goal.Plan is Plan.LandTroop or Plan.Rebase && goal.TargetPlanet == planet)
                DequeueCurrentOrder();

            if (OrderQueue.NotEmpty)
                return;

            if (planet.Owner != null && planet.Owner != Owner.Loyalty && !Owner.Loyalty.IsAtWarWith(planet.Owner))
                AbortLandNoFleet(planet);
            else
                OrderRebaseToNearest();
        }

        // Big Troop Ships will launch their own Assault Shuttles to land them on the planet
        void LaunchShuttlesFromTroopShip(FixedSimTime timeStep, Planet planet, Vector2 launchPos, ShipGoal goal)
        {
            if (!Orbit.InOrbit && Owner.Position.InRadius(planet.Position, planet.Radius *1.4f))
                ThrustOrWarpToPos(launchPos, timeStep, warpExitDistance: Owner.WarpOutDistance);
            else if (!TryRouteToOrbitTarget(goal, timeStep)) // Doing orbit with AssaultPlanet state to continue landing troops if possible
                Orbit.Orbit(planet, timeStep);

            if (Orbit.InOrbit)
            {
                if (planet.WeCanLandTroopsViaSpacePort(Owner.Loyalty))
                {
                    // We can land all our troops without assault bays since its our planet with space port
                    if (Owner.LandTroopsOnPlanet(planet) > 0)
                        Owner.SendTroopShuttlesToShip(planet);
                }
                else
                {
                    Owner.Carrier.AssaultPlanet(planet); // Launch Assault shuttles or use Transporters (STSA)
                }

                if (!Owner.HasOurTroops)
                {
                    OrderOrbitPlanet(planet, clearOrders: true);
                }
            }
        }

        Vector2 FindLandingOffset(Planet planet)
        {
            Vector2 pos;
            if (Owner.IsSingleTroopShip || Owner.IsDefaultAssaultShuttle)
                pos = Vector2.Zero.GenerateRandomPointInsideCircle(planet.Radius, planet.Random);
            else
                pos = planet.Position - planet.Position.GenerateRandomPointOnCircle(planet.Radius * 1.5f, planet.Random);

            return pos;
        }

        void DoRefit(FixedSimTime timeStep, ShipGoal goal)
        {
            if (Owner.IsPlatformOrStation)
            {
                ClearOrders(AIState.Refit); // orbitals wait in place for the empire goal
                return;
            }

            if (!Owner.Loyalty.AI.HasGoal(GoalType.Refit, Owner))
            {
                ClearOrders(); // Could not find empire refit goal
                return;
            }

            if (goal.TargetPlanet != null && goal.TargetPlanet.Owner != Owner.Loyalty)
            {
                ReverseThrustUntilStopped(timeStep); // the empire goal picks another port
                return;
            }

            IgnoreCombat = true;
            FlyInToLand(timeStep, goal, goal.TargetPlanet, LandPlan.Refit);
        }

        void DoRepairDroneLogic(Weapon w)
        {
            Ship repairMe = FriendliesNearby.FindMinFiltered(
                filter: ship => ShipNeedsRepair(ship, ShipResupply.RepairDroneRange),
                selector: ship => ship.InternalSlotsHealthPercent);

            if (repairMe == null) return;
            Vector2 target = w.Origin.DirectionToTarget(repairMe.Position);
            target.Y = target.Y * -1f;
            w.FireDrone(target);
        }

        bool ShipNeedsRepair(Ship target, float maxDistance)
        {
            return target.Active
                    && target.HealthPercent < ShipResupply.RepairDroneThreshold
                    && Owner.Position.Distance(target.Position) <= maxDistance;
        }

        void DoOrdinanceTransporterLogic(ShipModule module)
        {
            var ships = (Array<Ship>)module.GetParent().Loyalty.OwnedShips;
            Ship repairMe = ships.FindMinFiltered(
                        filter: ship => Owner.Position.Distance(ship.Position) <= module.TransporterRange + 500f
                                        && ship.Ordinance < ship.OrdinanceMax && !ship.Carrier.HasOrdnanceTransporters,
                        selector: ship => ship.Ordinance);
            if (repairMe == null)
                return;

            module.TransporterTimer = module.TransporterTimerConstant;

            float transferAmount    = module.TransporterOrdnance > module.GetParent().Ordinance
                ? module.GetParent().Ordinance : module.TransporterOrdnance;
            float ordnanceLeft = repairMe.ChangeOrdnance(transferAmount);
            module.GetParent().ChangeOrdnance(ordnanceLeft - transferAmount);
            module.GetParent().AddPower(module.TransporterPower * ((ordnanceLeft - transferAmount) / module.TransporterOrdnance));

            if (Owner.InFrustum)
                GameAudio.PlaySfxAsync("transporter", module.GetParent().SoundEmitter);
        }

        void DoAssaultTransporterLogic(ShipModule module)
        {
            if (NearByShips.IsEmpty)
                return;

            ShipWeight ship = NearByShips.Where(
                    s => s.Ship.Loyalty != null && s.Ship.Loyalty != Owner.Loyalty && s.Ship.ShieldPower <= 0
                         && s.Ship.Position.InRadius(Owner.Position, module.TransporterRange + 500f))
                .OrderBy(sw => Owner.Position.SqDist(sw.Ship.Position))
                .FirstOrDefault();
            
            if (ship.Ship == null)
                return;

            int landed = Owner.LandTroopsOnShip(ship.Ship, module.TransporterTroopAssault);
            if (landed > 0)
            {
                module.TransporterTimer = module.TransporterTimerConstant;
                if (Owner.InFrustum) // @todo audio should not be here
                    GameAudio.PlaySfxAsync("transporter");
            }
        }

        void DoReturnToHangar(FixedSimTime timeStep)
        {
            if (!Owner.IsHangarShip || !Owner.Mothership.Active)
            {
                ClearOrders(State);
                if      (Owner.IsMiningShip)                     Owner.Die(null, true);
                else if (Owner.ShipData.Role == RoleName.supply) OrderScrapShip();
                else                                             GoOrbitNearestPlanetAndResupply(true);
                return;
            }

            // scrap drones which fall outside of Mothership's control radius
            if (Owner.DesignRole == RoleName.drone && 
                !Owner.InRadius(Owner.Mothership.Position, Owner.Mothership.SensorRange))
            {
                Owner.Die(null, true);
                return;
            }

            FlyInToHangar(timeStep, Owner.Mothership);
        }

        public void LandTroopsAfterTouchdown(Ship target)
        {
            if (target.IsDeadOrDying || target.IsLaunchingOrLanding)
                return;

            if (target.Loyalty != Owner.Loyalty)
            {
                Owner.TryLandSingleTroopOnShip(target);
                Owner.Loyalty.ResetTargetsForShipsTargetingAfterBoarding(target);
                if (Owner.Active)
                    OrderReturnToHangar();
                return;
            }

            int freeRoom = target.TroopCapacity - target.TroopCount;
            for (int i = 0; i < freeRoom && Owner.GetOurFirstTroop(out Troop troop); ++i)
                troop.LandOnShip(target);

            if (Owner.Active && !Owner.HasOurTroops)
                OrderReturnToHangar();
        }

        public void ReturnToMothership(Ship mothership)
        {
            if (Owner.IsDefaultTroopTransport)
                Owner.LandTroopsOnShip(mothership);

            if (Owner.IsSupplyShuttle) // fbedard: Supply ship return with Ordinance
                mothership.ChangeOrdnance(Owner.Ordinance);

            if (Owner.IsMiningShip)
            {
                string cargoId = mothership.GetTether()?.Mining.CargoId ?? "";
                if (cargoId.NotEmpty())
                {
                    float maxToload = (mothership.MiningStationCargoSpaceMax - mothership.GetOtherCargo(cargoId)).LowerBound(0);
                    mothership.LoadCargo(cargoId, Owner.GetOtherCargo(cargoId).UpperBound(maxToload));
                }
            }
            Owner.Carrier.ScuttleHangarShips();
            mothership.ChangeOrdnance(Owner.ShipRetrievalOrd); // Get back the ordnance it took to launch the ship

            // find which hangar is the owner of this ship
            ShipModule owningHangar = mothership.Carrier.AllHangars.Find(
                                    h => h.TryGetHangarShip(out Ship hs) && hs == Owner);
            if (owningHangar != null)
            {
                // Set up repair and rearm times
                float missingHealth   = Owner.HealthMax - Owner.Health;
                float missingOrdnance = Owner.OrdinanceMax - Owner.Ordinance;
                float repairTime      = missingHealth / (mothership.RepairRate + Owner.RepairRate + mothership.Level * 10);
                float rearmTime       = missingOrdnance / (2 + mothership.Level);
                float shuttlePrepTime = Owner.IsDefaultAssaultShuttle ? 5 : 0;
                // FB - Here we are setting the hangar timer according to the R&R time. Cant be over the time to rebuild the ship
                owningHangar.HangarTimer = (repairTime + rearmTime + shuttlePrepTime).Clamped(5, owningHangar.HangarTimerConstant);
                owningHangar.SetHangarShip(null);

                mothership.OnShipReturned(Owner); // EVT: returned to base
            }
        }

        void DoReturnHome(FixedSimTime timeStep, ShipGoal goal)
        {
            if (Owner.HomePlanet?.Owner != Owner.Loyalty)
            {
                // find another friendly planet to land at
                Owner.UpdateHomePlanet(Owner.Loyalty.FindNearestSpacePort(Owner.Position));
                if (!Owner.IsHomeDefense // new home planet not found
                    || Owner.HomePlanet.System != Owner.System && !Owner.BaseCanWarp) // Cannot warp and its in another system
                {
                    // Nowhere to land, bye bye.
                    ClearOrders(AIState.Scuttle);
                    Owner.ScuttleTimer = 1;
                    return;
                }
            }

            if (Owner.InCombat)
                ClearOrders();

            if (Owner.SecondsAlive <= 5 || Owner.OnHighAlert)
                ThrustOrWarpToPos(Owner.HomePlanet.Position, timeStep);
            else
                FlyInToLand(timeStep, goal, Owner.HomePlanet, LandPlan.HomeDefense);
        }

        void DoBuilderReturnHome(FixedSimTime timeStep, ShipGoal goal)
        {
            if (goal.TargetPlanet.Owner != Owner.Loyalty)
            {
                // Nowhere to land, bye bye.
                ClearOrders(AIState.Scuttle);
                Owner.ScuttleTimer = 1;
                return;
            }

            FlyInToLand(timeStep, goal, goal.TargetPlanet, LandPlan.Builder);
        }

        void DoSupplyReturnHome(FixedSimTime timeStep, ShipGoal goal)
        {
            if (goal.TargetPlanet.Owner != Owner.Loyalty)
            {
                ClearOrders(AIState.Scuttle);
                Owner.ScuttleTimer = 1;
                return;
            }

            FlyInToLand(timeStep, goal, goal.TargetPlanet, LandPlan.Supply);
        }

        void DoRebaseToShip(FixedSimTime timeStep)
        {
            if (EscortTarget == null || !EscortTarget.Active || EscortTarget.IsLanding
                                     || EscortTarget.AI.State == AIState.Scrap
                                     || EscortTarget.AI.State == AIState.Refit)
            {
                OrderRebaseToNearest();
                return;
            }

            ThrustOrWarpToPos(EscortTarget.Position, timeStep);
            if (Owner.Position.InRadius(EscortTarget.Position, LandShip.BoardingRange(EscortTarget)))
            {
                if (EscortTarget.TroopCount >= EscortTarget.TroopCapacity)
                {
                    OrderRebaseToNearest();
                    return;
                }

                TryLandOnShip(EscortTarget);
            }
        }

        void DoBuildOrbital(FixedSimTime timeStep, ShipGoal goal)
        {
            Ship constructor = goal.TargetShip;
            if (constructor == null 
                || !constructor.Active 
                || constructor.Loyalty != Owner.Loyalty && !Owner.Loyalty.IsAlliedWith(constructor.Loyalty))
            {
                OrderBuilderReturnHome(goal.TargetPlanet);
                return;
            }

            if (!Owner.Position.InRadius(constructor.Position, constructor.Radius + ConstructionShip.ConstructingDistance))
            {
                ThrustOrWarpToPos(constructor.Position, timeStep);
            }
            else
            {
                constructor.Construction.AddConstructionFromBuilder();
                OrderBuilderReturnHome(goal.TargetPlanet);
            }
        }

        void DoRearmShip(FixedSimTime timeStep)
        {
            if (EscortTarget == null || !EscortTarget.Active)
            {
                ClearOrders();
                return;
            }

            if (!Owner.Position.InRadius(EscortTarget.Position, EscortTarget.Radius + 300f))
                ThrustOrWarpToPos(EscortTarget.Position, timeStep);
            else
                ReverseThrustUntilStopped(timeStep); // the empire goal takes care of the rearm
        }

        void DoSupplyShip(FixedSimTime timeStep)
        {
            var escortTarget = EscortTarget;
            if (EscortTarget == null || !escortTarget.Active
                                     || escortTarget.AI.State == AIState.Resupply
                                     || escortTarget.AI.State == AIState.Scrap
                                     || escortTarget.AI.State == AIState.Refit)
            {
                OrderReturnToHangar();
                return;
            }

            
            ThrustOrWarpToPos(EscortTarget.Position, timeStep);
            if (Owner.Position.InRadius(escortTarget.Position, escortTarget.Radius + 300f))
            {
                // remove amount from incoming supply (we counted full ordnance so remove it now)
                EscortTarget.Supply.ChangeIncomingOrdnance(-Owner.Ordinance);
                // how much the target did not take.
                float leftOverOrdnance = EscortTarget.ChangeOrdnance(Owner.Ordinance);
                // how much the target did take.
                float ordnanceDelivered = Owner.Ordinance - leftOverOrdnance;
                Owner.ChangeOrdnance(-ordnanceDelivered);
                Owner.SendOrdnanceShuttles(EscortTarget, ordnanceDelivered);
                EscortTarget.AI.TerminateResupplyIfDone(SupplyType.Rearm, terminateIfEnemiesNear: true);
                DequeueCurrentOrder();
                if (Owner.Ordinance < 1)
                    OrderReturnToHangar();
                else
                    ChangeAIState(AIState.AwaitingOrders);
            }
        }

        void DoResupplyEscort(FixedSimTime timeStep, ShipGoal goal)
        {
            if (EscortTarget == null || !EscortTarget.Active
                                     || !EscortTarget.SupplyShipCanSupply)
            {
                DequeueCurrentOrder();
                ChangeAIState(AIState.AwaitingOrders);
                Owner.Supply.ResetIncomingOrdnance(SupplyType.Rearm);
                ExitCombatState();
                Owner.AI.SetPriorityOrder(false);
                Owner.AI.IgnoreCombat = false;
                return;
            }

            var escortVector = EscortTarget.FindStrafeVectorFromTarget(goal.VariableNumber, goal.Direction);
            float distanceToEscortSpot = Owner.Position.Distance(escortVector);
            float supplyShipVelocity   = EscortTarget.CurrentVelocity;
            float escortVelocity       = Owner.VelocityMax;
            if (distanceToEscortSpot < 50)
                escortVelocity = distanceToEscortSpot;
            else if (distanceToEscortSpot < 2000) // ease up thrust on approach to escort spot
                escortVelocity = distanceToEscortSpot / 2000 * Owner.VelocityMax + supplyShipVelocity + 25;

            bool terminateIfEnemiesNear = distanceToEscortSpot < 2000;
            ThrustOrWarpToPos(escortVector, timeStep, escortVelocity);

            switch (goal.VariableString)
            {
                default:       TerminateResupplyIfDone(SupplyType.All, terminateIfEnemiesNear);    break;
                case "Rearm":  TerminateResupplyIfDone(SupplyType.Rearm, terminateIfEnemiesNear);  break;
                case "Repair": TerminateResupplyIfDone(SupplyType.Repair, terminateIfEnemiesNear); break;
                case "Troops": TerminateResupplyIfDone(SupplyType.Troops, terminateIfEnemiesNear); break;
            }
        }

        void DoTroopToShip(FixedSimTime timeStep, ShipGoal goal)
        {
            if (EscortTarget == null || !EscortTarget.Active || EscortTarget.IsLanding)
            {
                ClearOrders();
                return;
            }
            SubLightMoveTowardsPosition(EscortTarget.Position, timeStep);
            if (Owner.Position.InRadius(EscortTarget.Position, LandShip.BoardingRange(EscortTarget)))
            {
                if (EscortTarget.TroopCapacity > EscortTarget.TroopCount)
                {
                    TryLandOnShip(EscortTarget);
                    return;
                }
                Orbit.Orbit(EscortTarget, timeStep);
            }
        }

        void DoStop(FixedSimTime timeStep, ShipGoal goal)
        {
            if (ReverseThrustUntilStopped(timeStep))
            {
                DequeueCurrentOrder();
            }
        }

        bool DoBombard(FixedSimTime timeStep, ShipGoal goal)
        {
            Planet planet = goal.TargetPlanet;
            if (planet == null) // wtf? this happened when loading a savegame
            {
                Log.Error("DoBombard: targetPlant was null");
                DequeueCurrentOrder();
                return false;
            }

            if (!planet.TroopsHereAreEnemies(Owner.Loyalty) && planet.Population <= 0f // Everyone is dead
                || planet.Owner != null && !Owner.Loyalty.IsEmpireAttackable(planet.Owner))
            {
                ClearOrders();
                AddOrbitPlanetGoal(planet); // Stay in Orbit
            }

            if (TryRouteToOrbitTarget(goal, timeStep))
                return false; // still routing around wells, no bombing yet

            Orbit.Orbit(planet, timeStep);
            if (planet.Owner == Owner.Loyalty)
            {
                ClearOrders();
                return true; // skip combat rest of the update
            }

            DropBombsAtGoal(goal, Orbit.InOrbit);
            return false;
        }

        void DoExterminate(FixedSimTime timeStep, ShipGoal goal)
        {
            Planet planet = goal.TargetPlanet;
            if (planet == null)
            {
                DoFindExterminationTarget(timeStep, goal);
                return;
            }

            if (TryRouteToOrbitTarget(goal, timeStep))
                return; // still routing around wells, no bombing yet

            Orbit.Orbit(planet, timeStep);

            // have we exterminated it?
            if (planet.Owner == Owner.Loyalty || planet.Owner == null)
            {
                ClearOrders();
                DoFindExterminationTarget(timeStep, goal);
            }
            else
            {
                // keep bombing it
                DropBombsAtGoal(goal, Orbit.InOrbit);
            }
        }
    }
}