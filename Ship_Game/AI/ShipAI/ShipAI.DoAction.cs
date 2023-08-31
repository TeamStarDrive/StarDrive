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

        void DoBoardShip(FixedSimTime timeStep)
        {
            HasPriorityTarget = true;
            ChangeAIState(AIState.Boarding);
            var escortTarget = EscortTarget;
            if (Owner.TroopCount < 1 || escortTarget == null || !escortTarget.Active || escortTarget.Loyalty == Owner.Loyalty)
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
            if (distance < escortTarget.Radius + 300f)
            {
                Owner.TryLandSingleTroopOnShip(escortTarget);
                OrderReturnToHangar();
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
                // For big ships this can be 70% of DesiredCombatRange,
                // but also ensure we are far enough to prevent warping on top of enemy fleets
                //   which can be a detriment and also an exploit by players
                if (Owner.engineState == Ship.MoveState.Warp &&
                    (distanceToTarget < 5000f || distanceToTarget < Owner.DesiredCombatRange*0.7f))
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
                if (target == null) 
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

            if (goal.BuildPosition.OutsideRadius(Owner.Position, Owner.CurrentVelocity*2))
            {
                ThrustOrWarpToPos(goal.BuildPosition, timeStep);
                return;
            }

            ReverseThrustUntilStopped(timeStep);
            if (!Owner.Construction.ConsturctionCompleted)
                return;

            Ship orbital = Ship.CreateShipAtPoint(Owner.Universe, bg.ToBuild.Name, Owner.Loyalty, goal.BuildPosition);
            if (orbital == null)
                return;

            if (orbital.IsSubspaceProjector)
                Owner.Loyalty.AI.SpaceRoadsManager.AddProjectorToRoadList(orbital, goal.BuildPosition);

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
                UpdateResearchStationGoal(orbital, bg.TetherPlanet);
                planetToTether.OrbitalStations.Add(orbital);
                if (planetToTether.IsOverOrbitalsLimit(orbital.ShipData))
                    planetToTether.TryRemoveExcessOrbital(orbital);
            }
            else
            {
                UpdateResearchStationGoal(orbital, Owner.System);
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
            if (target == null || target.Owner != Owner.Loyalty) // FB - Planet owner has changed
            {
                OrderScrapShip();
                return;
            }

            if (goal is RefitOrbital && goal.OldShip?.Active == false)
            {
                OrderScrapShip();
                return;
            }

            if (goal.BuildPosition.OutsideRadius(Owner.Position, Owner.CurrentVelocity * 2))
            {
                ThrustOrWarpToPos(goal.BuildPosition, timeStep);
                return;
            }

            ReverseThrustUntilStopped(timeStep);
            if (!Owner.Construction.ConsturctionCompleted)
                return;


            Ship orbital = Ship.CreateShipAtPoint(Owner.Universe, bg.ToBuild.Name, Owner.Loyalty, goal.BuildPosition);
            if (orbital != null)
            {
                orbital.Position = goal.BuildPosition;
                orbital.TetherToPlanet(target);
                target.OrbitalStations.Add(orbital);
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

                UpdateResearchStationGoal(orbital, target);
            }
        }

        void UpdateResearchStationGoal(Ship orbital, ExplorableGameObject target)
        {
            if (!orbital.IsResearchStation)
                return;

            Goal goal = Owner.Loyalty.AI.FindGoal(g => g.IsResearchStationGoal(target));
            if (goal != null)
            {
                goal.TargetShip = orbital;
                Owner.Universe.AddEmpireToResearchableList(Owner.Loyalty, target);
            }
        }

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
                ThrustOrWarpToPos(MovePosition, timeStep);
                if (Owner.Position.InRadius(MovePosition, Owner.ExplorePlanetDistance))
                {
                    if (PatrolTarget.IsResearchable && Owner.Loyalty.isPlayer)
                        Owner.Universe.Screen.NotificationManager?.AddReseachablePlanet(PatrolTarget);

                    PatrolTarget.SetExploredBy(Owner.Loyalty);
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

            ThrustOrWarpToPos(MovePosition, timeStep);
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

        Vector2 LandingOffset;

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
                // force the ship out of warp if we get too close
                // this is a balance feature
                ThrustOrWarpToPos(landingSpot, timeStep, warpExitDistance: Owner.WarpOutDistance);
                LandTroopsViaSingleTransport(planet, landingSpot, timeStep);
            }
            else
            {
                LaunchShuttlesFromTroopShip(timeStep, planet, landingSpot);
            }
        }

        // Assault Shuttles will dump troops on the surface and return back to the troop ship to transport additional troops
        // Single Troop Ships can land from a longer distance, but the ship vanishes after landing its troop
        void LandTroopsViaSingleTransport(Planet planet, Vector2 landingSpot, FixedSimTime timeStep)
        {
            if (landingSpot.InRadius(Owner.Position, Owner.Radius + 40f))
            {
                // This will vanish default single Troop Ship or order Assault shuttle to return to hangar
                Owner.LandTroopsOnPlanet(planet); 
                DequeueCurrentOrder(); // make sure to clear this order, so we don't try to unload troops again
                if (Owner.IsHangarShip && Owner.Mothership.Active)
                    OrderReturnToHangar();
                else
                    Owner.QueueTotalRemoval();
            }
        }

        // Big Troop Ships will launch their own Assault Shuttles to land them on the planet
        void LaunchShuttlesFromTroopShip(FixedSimTime timeStep, Planet planet, Vector2 launchPos)
        {
            if (!Orbit.InOrbit && Owner.Position.InRadius(planet.Position, planet.Radius *1.4f))
                ThrustOrWarpToPos(launchPos, timeStep, warpExitDistance: Owner.WarpOutDistance);
            else // Doing orbit with AssaultPlanet state to continue landing troops if possible
                Orbit.Orbit(planet, timeStep); 

            if (Orbit.InOrbit)
            {
                if (planet.WeCanLandTroopsViaSpacePort(Owner.Loyalty))
                    Owner.LandTroopsOnPlanet(planet); // We can land all our troops without assault bays since its our planet with space port
                else
                    Owner.Carrier.AssaultPlanet(planet); // Launch Assault shuttles or use Transporters (STSA)

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
                pos = planet.Random.Vector2D(planet.Radius);
            else
                pos = planet.Position - planet.Position.GenerateRandomPointOnCircle(planet.Radius * 1.5f, planet.Random);

            return pos;
        }

        void DoRefit(ShipGoal goal)
        {
            if (goal.Goal == null) // empire goal was removed or planet was compromised
                ClearOrders();

            // stick around until the empire goal picks the ship for refit
            if (!Owner.IsPlatformOrStation)
            {
                ClearOrders(AIState.HoldPosition);
                SetPriorityOrder(true); // Especially for freighters manually refitted by the player, so they wont be taken to trade again
            }

            ClearOrders(AIState.Refit);  // For orbitals
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

        void DoRepairBeamLogic(Weapon w)
        {
            Ship repairMe = FriendliesNearby.FindMinFiltered(
                    filter: ship => ShipNeedsRepair(ship, w.BaseRange + 500f, Owner),
                    selector: ship => ship.InternalSlotsHealthPercent);

            if (repairMe != null) w.FireTargetedBeam(repairMe);
        }

        bool ShipNeedsRepair(Ship target, float maxDistance, Ship doNotHealSelf = null)
        {
            return target.Active && target != doNotHealSelf
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
                if (Owner.ShipData.Role == RoleName.supply)
                    OrderScrapShip();
                else
                    GoOrbitNearestPlanetAndResupply(true);
                return;
            }

            // scrap drones which fall outside of Mothership's control radius
            if (Owner.DesignRole == RoleName.drone && 
                !Owner.InRadius(Owner.Mothership.Position, Owner.Mothership.SensorRange))
            {
                Owner.Die(null, true);
                return;
            }

            ThrustOrWarpToPos(Owner.Mothership.Position, timeStep);

            // recover the ship
            if (Owner.Position.InRadius(Owner.Mothership.Position, Owner.Mothership.Radius))
            {
                if (Owner.IsDefaultTroopTransport)
                    Owner.LandTroopsOnShip(Owner.Mothership);

                if (Owner.IsSupplyShuttle) // fbedard: Supply ship return with Ordinance
                    Owner.Mothership.ChangeOrdnance(Owner.Ordinance);

                Owner.Mothership.ChangeOrdnance(Owner.ShipRetrievalOrd); // Get back the ordnance it took to launch the ship
                Owner.QueueTotalRemoval();
                
                // find which hangar is the owner of this ship
                ShipModule owningHangar = Owner.Mothership.Carrier.AllActiveHangars.Find(
                                        h => h.TryGetHangarShip(out Ship hs) && hs == Owner);
                if (owningHangar != null)
                {
                    owningHangar.SetHangarShip(null);

                    // Set up repair and rearm times
                    float missingHealth   = Owner.HealthMax - Owner.Health;
                    float missingOrdnance = Owner.OrdinanceMax - Owner.Ordinance;
                    float repairTime      = missingHealth / (Owner.Mothership.RepairRate + Owner.RepairRate + Owner.Mothership.Level * 10);
                    float rearmTime       = missingOrdnance / (2 + Owner.Mothership.Level);
                    float shuttlePrepTime = Owner.IsDefaultAssaultShuttle ? 5 : 0;
                    // FB - Here we are setting the hangar timer according to the R&R time. Cant be over the time to rebuild the ship
                    owningHangar.HangarTimer = (repairTime + rearmTime + shuttlePrepTime).Clamped(5, owningHangar.HangarTimerConstant);

                    Owner.Mothership.OnShipReturned(Owner); // EVT: returned to base
                }
            }
        }

        void DoReturnHome(FixedSimTime timeStep)
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

            ThrustOrWarpToPos(Owner.HomePlanet.Position, timeStep);
            if (Owner.SecondsAlive > 5
                && !Owner.OnHighAlert
                && Owner.Position.InRadius(Owner.HomePlanet.Position, Owner.HomePlanet.Radius + 150f))
            {
                Owner.HomePlanet.LandDefenseShip(Owner);
                Owner.QueueTotalRemoval();
            }
        }

        void DoRebaseToShip(FixedSimTime timeStep)
        {
            if (EscortTarget == null || !EscortTarget.Active
                                     || EscortTarget.AI.State == AIState.Scrap
                                     || EscortTarget.AI.State == AIState.Refit)
            {
                OrderRebaseToNearest();
                return;
            }

            ThrustOrWarpToPos(EscortTarget.Position, timeStep);
            if (Owner.Position.InRadius(EscortTarget.Position, EscortTarget.Radius + 300f))
            {
                if (EscortTarget.TroopCapacity == EscortTarget.TroopCount)
                {
                    OrderRebaseToNearest();
                    return;
                }

                Owner.TryLandSingleTroopOnShip(EscortTarget);
            }
        }

        void DoBuildOrbital(FixedSimTime timeStep, ShipGoal goal)
        {
            if (EscortTarget == null || !EscortTarget.Active)
            {
                OrderSupplyShipLand(goal.TargetPlanet);
                return;
            }

            if (!Owner.Position.InRadius(EscortTarget.Position, EscortTarget.Radius + ConstructionShip.ConstructingDistance))
            {
                ThrustOrWarpToPos(EscortTarget.Position, timeStep);
            }
            else
            {
                EscortTarget.Construction.AddConstruction(Owner.OrdinanceMax);
                OrderSupplyShipLand(goal.TargetPlanet);
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

        void DoSystemDefense(FixedSimTime timeStep)
        {
            SystemToDefend = SystemToDefend ?? Owner.System;
            if (SystemToDefend == null || AwaitClosest?.Owner == Owner.Loyalty)
                AwaitOrdersAIControlled(timeStep);
            else
                OrderSystemDefense(SystemToDefend);
        }

        void DoTroopToShip(FixedSimTime timeStep, ShipGoal goal)
        {
            if (EscortTarget == null || !EscortTarget.Active)
            {
                ClearOrders();
                return;
            }
            SubLightMoveTowardsPosition(EscortTarget.Position, timeStep);
            if (Owner.Position.InRadius(EscortTarget.Position, EscortTarget.Radius + 300f))
            {
                if (EscortTarget.TroopCapacity > EscortTarget.TroopCount)
                {
                    Owner.TryLandSingleTroopOnShip(EscortTarget);
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