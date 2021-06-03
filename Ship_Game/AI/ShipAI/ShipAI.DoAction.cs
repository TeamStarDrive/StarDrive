using Microsoft.Xna.Framework;
using Ship_Game.Audio;
using Ship_Game.Gameplay;
using Ship_Game.Ships;
using System;
using System.Linq;
using static Ship_Game.AI.CombatStanceType;

namespace Ship_Game.AI
{
    public sealed partial class ShipAI
    {
        public bool IsFiringAtMainTarget => FireOnMainTargetTime > 0;
        float FireOnMainTargetTime;
        StanceType CombatRangeType => ToStanceType(CombatState);

        void DoBoardShip(FixedSimTime timeStep)
        {
            HasPriorityTarget = true;
            State             = AIState.Boarding;
            var escortTarget  = EscortTarget;
            if (escortTarget == null || !escortTarget.Active || escortTarget.loyalty == Owner.loyalty)
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

            ThrustOrWarpToPos(escortTarget.Center, timeStep);
            float distance = Owner.Center.Distance(escortTarget.Center);
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

            if (ship.Active && !ship.dying && !ship.IsInWarp && Owner.loyalty.IsEmpireAttackable(ship.GetLoyalty(), ship))
                return true;

            return Owner.loyalty.isPlayer 
                   && HasPriorityTarget 
                   && ship.inSensorRange // FB - this is only for the player, might be a bug
                   && !Owner.loyalty.IsAlliedWith(ship.loyalty);
        }

        Ship UpdateCombatTarget()
        {
            if (IsTargetValid(Target)) return Target;

            Target = PotentialTargets.FirstOrDefault(t => IsTargetValid(t) 
                                                          && t.Center.InRadius(Owner.Center, Owner.SensorRange));
            return Target;
        }

        void DoCombat(FixedSimTime timeStep)
        {
            Ship target = UpdateCombatTarget();
            if (target == null)
            {
                DequeueCurrentOrder();
                Owner.InCombat = false;
                return;
            }

            AwaitClosest = null; // TODO: Why is this set here?
            State = AIState.Combat;
            Owner.InCombat = true;
            Owner.InCombatTimer = 15f;

            if (!HasPriorityOrder && !HasPriorityTarget && Owner.Weapons.Count == 0 && !Owner.Carrier.HasActiveHangars)
                CombatState = CombatState.Evade;

            // in range:
            float distanceToTarget = target.Center.Distance(Owner.Center);
            if (HasPriorityOrder || distanceToTarget > Owner.DesiredCombatRange && distanceToTarget > 7500f)
            {
                if (Owner.fleet == null || Owner.ShipEngines.ReadyForFormationWarp == Status.Good)
                MoveToEngageTarget(target, timeStep);
            }
            else
            {

                if (Owner.engineState == Ship.MoveState.Warp)
                    Owner.HyperspaceReturn();

                if (FleetNode != null && Owner.fleet != null && HasPriorityOrder)
                {
                    // TODO: need to move this into fleet.
                    if (Owner.fleet.FleetTask == null)
                    {
                        Vector2 nodePos = Owner.fleet.AveragePosition() + FleetNode.FleetOffset;
                        if (target.Center.OutsideRadius(nodePos, FleetNode.OrdersRadius))
                        {

                            if (Owner.Center.OutsideRadius(nodePos, 1000f))
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
                        var task = Owner.fleet.FleetTask;
                        if (target.Center.OutsideRadius(task.AO, Math.Max(task.AORadius, FleetNode.OrdersRadius)))
                        {
                            DequeueCurrentOrder();
                        }
                    }
                }

                if (Owner.Carrier.IsInHangarLaunchRange(distanceToTarget))
                    Owner.Carrier.ScrambleFighters();

                if (Intercepting && CombatRangeType == StanceType.RangedCombatMovement)
                {
                    // clamp the radius here so that it wont flounder if the ship has very long range weapons.
                    float radius = Owner.DesiredCombatRange * 3f;
                    if (Owner.Center.OutsideRadius(target.Center, radius))
                    {
                        ThrustOrWarpToPos(target.Center, timeStep);
                        return;
                    }
                    else if (distanceToTarget < Owner.DesiredCombatRange)
                        Intercepting = false;
                }

                CombatAI.ExecuteCombatTactic(timeStep);
            }
            
            // Target was modified by one of the CombatStates (?)
            Owner.InCombat = Target != null;
        }

        void MoveToEngageTarget(Ship target, FixedSimTime timeStep)
        {
            if (CombatRangeType == StanceType.RangedCombatMovement)
            {
                Vector2 prediction = target.Center;
                Weapon fastestWeapon = Owner.FastestWeapon;
                if (fastestWeapon != null && target.CurrentVelocity > 0) // if we have a weapon
                {
                    prediction = fastestWeapon.ProjectedImpactPointNoError(target);
                }

                ThrustOrWarpToPos(prediction, timeStep);
            }
            else
            {
                ThrustOrWarpToPos(Target.Center, timeStep);
            }
        }

        void DoDeploy(ShipGoal g)
        {
            if (g.Goal == null)
                return;

            Planet target = g.TargetPlanet;
            if (target == null && g.Goal.TetherTarget != Guid.Empty)
            {
                target = Empire.Universe.GetPlanet(g.Goal.TetherTarget);
            }

            if (target != null && (g.Goal.BuildPosition).Distance(Owner.Center) > 200f)
            {
                OrderDeepSpaceBuild(g.Goal);
                return;
            }

            Ship orbital = Ship.CreateShipAtPoint(g.Goal.ToBuildUID, Owner.loyalty, g.Goal.BuildPosition);
            if (orbital == null)
                return;

            AddStructureToRoadsList(g, orbital);

            if (g.Goal.TetherTarget != Guid.Empty)
            {
                Planet planetToTether = Empire.Universe.GetPlanet(g.Goal.TetherTarget);
                orbital.TetherToPlanet(planetToTether);
                orbital.TetherOffset = g.Goal.TetherOffset;
                planetToTether.OrbitalStations.Add(orbital);
                if (planetToTether.IsOverOrbitalsLimit(orbital))
                    planetToTether.TryRemoveExcessOrbital(orbital);
            }

            Owner.QueueTotalRemoval();
            if (g.Goal.OldShip?.Active == true) // we are refitting something
                g.Goal.OldShip.QueueTotalRemoval();
        }

        void DoDeployOrbital(ShipGoal g)
        {
            if (g.Goal == null)
            {
                Log.Info($"There was no goal for Construction ship deploying orbital");
                OrderScrapShip();
                return;
            }

            Planet target = Empire.Universe.GetPlanet(g.Goal.TetherTarget);
            if (target == null || target.Owner != Owner.loyalty) // FB - Planet owner has changed
            {
                OrderScrapShip();
                return;
            }

            if (g.Goal.BuildPosition.Distance(Owner.Center) > 200f) // correct build position after long travel
            {
                OrderDeepSpaceBuild(g.Goal);
                return;
            }

            Ship orbital = Ship.CreateShipAtPoint(g.Goal.ToBuildUID, Owner.loyalty, g.Goal.BuildPosition);
            if (orbital != null)
            {
                orbital.Center = g.Goal.BuildPosition;
                orbital.TetherToPlanet(target);
                target.OrbitalStations.Add(orbital);
                Owner.QueueTotalRemoval();
                if (g.Goal.OldShip?.Active == true) // we are refitting something
                    g.Goal.OldShip.QueueTotalRemoval();
                else 
                    target.TryRemoveExcessOrbital(orbital);
            }

            OrderScrapShip();
        }

        void AddStructureToRoadsList(ShipGoal g, Ship platform)
        {
            foreach (SpaceRoad road in Owner.loyalty.SpaceRoadsList)
            {
                foreach (RoadNode node in road.RoadNodesList)
                {
                    if (node.Position == g.Goal.BuildPosition)
                    {
                        node.Platform = platform;
                        StatTracker.StatAddRoad(Empire.Universe.StarDate, node, Owner.loyalty);
                        return;
                    }
                }
            }
        }

        public void DoExplore(FixedSimTime timeStep)
        {
            SetPriorityOrder(true);
            IgnoreCombat = true;
            if (ExplorationTarget == null)
            {
                if (!Owner.loyalty.GetEmpireAI().ExpansionAI.AssignExplorationTargetSystem(Owner, out ExplorationTarget))
                    ClearOrders(); // FB - could not find a new system to explore
            }
            else if (DoExploreSystem(timeStep))
            {
                Owner.loyalty.GetEmpireAI().ExpansionAI.RemoveExplorationTargetFromList(ExplorationTarget);
                ExplorationTarget.AddSystemExploreSuccessMessage(Owner.loyalty);
                ExplorationTarget = null;
            }
        }

        bool DoExploreSystem(FixedSimTime timeStep)
        {
            if (Owner.System == ExplorationTarget 
                && BadGuysNear 
                && ExplorationTarget.ShipList.Any(s => s.AI.Target == Owner))
            {
                ClearOrders();
                if (Owner.TryGetScoutFleeVector(out Vector2 escapePos))
                    OrderMoveToNoStop(escapePos, Owner.Direction.DirectionToTarget(escapePos), true, AIState.Flee);
                else
                    OrderFlee(true);

                return false;
            }

            if (ExplorationTarget.PlanetList.Count == 0)
                return DoExploreEmptySystem(timeStep, ExplorationTarget); // Lone Star

            if (!TryGetClosestUnexploredPlanet(ExplorationTarget, out PatrolTarget))
            {
                ExplorationTarget.UpdateFullyExploredBy(Owner.loyalty);
                return true; // All planets explored
            }

            MovePosition           = PatrolTarget.Center;
            float distanceToTarget = Owner.Center.Distance(MovePosition);
            if (distanceToTarget < 75000f)
                PatrolTarget.ParentSystem.SetExploredBy(Owner.loyalty);

            if (distanceToTarget >= 5500f)
            {
                ThrustOrWarpToPos(MovePosition, timeStep, distanceToTarget);
            }
            else
            {
                ThrustOrWarpToPos(MovePosition, timeStep);
                if (distanceToTarget < 500f)
                    PatrolTarget.SetExploredBy(Owner.loyalty);
            }

            return false;
        }

        bool DoExploreEmptySystem(FixedSimTime timeStep, SolarSystem system)
        {
            if (system.IsExploredBy(Owner.loyalty))
                return true;

            MovePosition = system.Position;
            if (Owner.Center.InRadius(MovePosition, 75000f))
            {
                system.SetExploredBy(Owner.loyalty);
                return true;
            }

            ThrustOrWarpToPos(MovePosition, timeStep);
            return false;
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
                RotateTowardsPosition(Target.Center, timeStep, 0.2f);
            }
        }

        Vector2 LandingOffset;

        void DoLandTroop(FixedSimTime timeStep, ShipGoal goal)
        {
            Planet planet = goal.TargetPlanet;
            if (planet.Owner != null 
                && planet.Owner != Owner.loyalty 
                && !Owner.loyalty.IsAtWarWith(planet.Owner))
            {
                AbortLandNoFleet(planet);
                return;
            }

            if (LandingOffset.AlmostZero())
                LandingOffset = RandomMath.Vector2D(planet.ObjectRadius);

            Vector2 landingSpot = planet.Center + LandingOffset;
            // force the ship out of warp if we get too close
            // this is a balance feature
            ThrustOrWarpToPos(landingSpot, timeStep, warpExitDistance: Owner.WarpOutDistance);
            if (Owner.IsDefaultAssaultShuttle)      LandTroopsViaSingleTransport(planet, landingSpot, timeStep);
            else if (Owner.IsDefaultTroopShip)      LandTroopsViaSingleTransport(planet, landingSpot, timeStep);
            else                                    LandTroopsViaTroopShip(timeStep, planet);
        }

        // Assault Shuttles will dump troops on the surface and return back to the troop ship to transport additional troops
        // Single Troop Ships can land from a longer distance, but the ship vanishes after landing its troop
        void LandTroopsViaSingleTransport(Planet planet, Vector2 landingSpot, FixedSimTime timeStep)
        {
            if (landingSpot.InRadius(Owner.Center, Owner.Radius + 40f))
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
        void LandTroopsViaTroopShip(FixedSimTime timeStep, Planet planet)
        {
            if (Owner.Center.InRadius(planet.Center, Owner.Radius + planet.ObjectRadius * 2))
            {
                if (planet.WeCanLandTroopsViaSpacePort(Owner.loyalty))
                    Owner.LandTroopsOnPlanet(planet); // We can land all our troops without assault bays since its our planet with space port
                else
                    Owner.Carrier.AssaultPlanet(planet); // Launch Assault shuttles or use Transporters (STSA)

                if (Owner.HasOurTroops)
                    Orbit.Orbit(planet, timeStep); // Doing orbit with AssaultPlanet state to continue landing troops if possible
                else
                    ClearOrders();
            }
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
            using (FriendliesNearby.AcquireReadLock())
            {
                Ship repairMe = FriendliesNearby.FindMinFiltered(
                    filter: ship => ShipNeedsRepair(ship, ShipResupply.RepairDroneRange),
                    selector: ship => ship.InternalSlotsHealthPercent);

                if (repairMe == null) return;
                Vector2 target = w.Origin.DirectionToTarget(repairMe.Center);
                target.Y = target.Y * -1f;
                w.FireDrone(target);
            }
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
                    && Owner.Center.Distance(target.Center) <= maxDistance;
        }

        void DoOrdinanceTransporterLogic(ShipModule module)
        {
            var ships = (Array<Ship>)module.GetParent().loyalty.OwnedShips;
            Ship repairMe = ships.FindMinFiltered(
                        filter: ship => Owner.Center.Distance(ship.Center) <= module.TransporterRange + 500f
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
            ShipWeight ship = NearByShips.Where(
                    s => s.Ship.loyalty != null && s.Ship.loyalty != Owner.loyalty && s.Ship.shield_power <= 0
                         && s.Ship.Center.InRadius(Owner.Center, module.TransporterRange + 500f))
                .OrderBy(sw => Owner.Center.SqDist(sw.Ship.Center))
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
                if (Owner.shipData.Role == ShipData.RoleName.supply)
                    OrderScrapShip();
                else
                    GoOrbitNearestPlanetAndResupply(true);
                return;
            }
            ThrustOrWarpToPos(Owner.Mothership.Center, timeStep);

            if (Owner.Center.InRadius(Owner.Mothership.Center, Owner.Mothership.Radius + 300f))
            {
                Owner.LandTroopsOnShip(Owner.Mothership);

                if (Owner.shipData.Role == ShipData.RoleName.supply) // fbedard: Supply ship return with Ordinance
                    Owner.Mothership.ChangeOrdnance(Owner.Ordinance);

                Owner.Mothership.ChangeOrdnance(Owner.ShipRetrievalOrd); // Get back the ordnance it took to launch the ship
                // Set up repair and rearm times
                float missingHealth   = Owner.HealthMax - Owner.Health;
                float missingOrdnance = Owner.OrdinanceMax - Owner.Ordinance;
                float repairTime      = missingHealth / (Owner.Mothership.RepairRate + Owner.RepairRate + Owner.Mothership.Level * 10);
                float rearmTime       = missingOrdnance / (2 + Owner.Mothership.Level);
                float shuttlePrepTime = Owner.IsDefaultAssaultShuttle ? 5 : 0;
                Owner.QueueTotalRemoval();
                foreach (ShipModule hangar in Owner.Mothership.Carrier.AllActiveHangars)
                {
                    if (hangar.TryGetHangarShip(out Ship hangarShip) && hangarShip != Owner)
                        continue;

                    hangar.SetHangarShip(null);
                    // FB - Here we are setting the hangar timer according to the R&R time. Cant be over the time to rebuild the ship
                    hangar.hangarTimer = (repairTime + rearmTime + shuttlePrepTime).Clamped(5, hangar.hangarTimerConstant);
                    hangar.HangarShipGuid = Guid.Empty;
                }
            }
        }

        void DoReturnHome(FixedSimTime timeStep)
        {
            if (Owner.HomePlanet?.Owner != Owner.loyalty)
            {
                // find another friendly planet to land at
                Owner.UpdateHomePlanet(Owner.loyalty.RallyShipYardNearestTo(Owner.Center));
                if (!Owner.IsHomeDefense // new home planet not found
                    || Owner.HomePlanet.ParentSystem != Owner.System && !Owner.BaseCanWarp) // Cannot warp and its in another system
                {
                    // Nowhere to land, bye bye.
                    ClearOrders(AIState.Scuttle);
                    Owner.ScuttleTimer = 1;
                    return;
                }
            }

            ThrustOrWarpToPos(Owner.HomePlanet.Center, timeStep);
            if (Owner.Center.InRadius(Owner.HomePlanet.Center, Owner.HomePlanet.ObjectRadius + 150f))
            {
                Owner.HomePlanet.LandDefenseShip(Owner);
                Owner.QueueTotalRemoval();
            }

            if (Owner.InCombat)
                ClearOrders();
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

            ThrustOrWarpToPos(EscortTarget.Center, timeStep);
            if (Owner.Center.InRadius(EscortTarget.Center, EscortTarget.Radius + 300f))
            {
                if (EscortTarget.TroopCapacity == EscortTarget.TroopCount)
                {
                    OrderRebaseToNearest();
                    return;
                }

                Owner.TryLandSingleTroopOnShip(EscortTarget);
            }
        }

        void DoRearmShip(FixedSimTime timeStep)
        {
            if (EscortTarget == null)
            {
                ClearOrders();
                return;
            }

            if (!Owner.Center.InRadius(EscortTarget.Center, EscortTarget.Radius + 300f))
                ThrustOrWarpToPos(EscortTarget.Center, timeStep);
            else
                ReverseThrustUntilStopped(timeStep);
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

            
            ThrustOrWarpToPos(EscortTarget.Center, timeStep);
            if (Owner.Center.InRadius(escortTarget.Center, escortTarget.Radius + 300f))
            {
                // how much the target did not take.
                float leftOverOrdnance = EscortTarget.ChangeOrdnance(Owner.Ordinance);
                // how much the target did take.
                float ordnanceDelivered = Owner.Ordinance - leftOverOrdnance;
                // remove amount from incoming supply
                EscortTarget.Supply.ChangeIncomingSupply(SupplyType.Rearm, -ordnanceDelivered);
                Owner.ChangeOrdnance(-ordnanceDelivered);
                EscortTarget.AI.TerminateResupplyIfDone();
                OrderReturnToHangar();
            }
        }

        void DoResupplyEscort(FixedSimTime timeStep, ShipGoal goal)
        {
            if (EscortTarget == null || !EscortTarget.Active
                                     || EscortTarget.AI.State == AIState.Resupply
                                     || EscortTarget.AI.State == AIState.Scrap
                                     || EscortTarget.AI.State == AIState.Refit
                                     || !EscortTarget.SupplyShipCanSupply)
            {
                State = AIState.AwaitingOrders;
                IgnoreCombat = false;
                return;
            }

            var escortVector = EscortTarget.FindStrafeVectorFromTarget(goal.VariableNumber, goal.Direction);
            float distanceToEscortSpot = Owner.Center.Distance(escortVector);
            float supplyShipVelocity   = EscortTarget.CurrentVelocity;
            float escortVelocity       = Owner.VelocityMaximum;
            if (distanceToEscortSpot < 50)
                escortVelocity = distanceToEscortSpot;
            else if (distanceToEscortSpot < 2000) // ease up thrust on approach to escort spot
                escortVelocity = distanceToEscortSpot / 2000 * Owner.VelocityMaximum + supplyShipVelocity + 25;

            ThrustOrWarpToPos(escortVector, timeStep, escortVelocity);

            switch (goal.VariableString)
            {
                default:       TerminateResupplyIfDone();                  break;
                case "Rearm":  TerminateResupplyIfDone(SupplyType.Rearm);  break;
                case "Repair": TerminateResupplyIfDone(SupplyType.Repair); break;
                case "Troops": TerminateResupplyIfDone(SupplyType.Troops); break;
            }
        }

        void DoSystemDefense(FixedSimTime timeStep)
        {
            SystemToDefend = SystemToDefend ?? Owner.System;
            if (SystemToDefend == null || AwaitClosest?.Owner == Owner.loyalty)
                AwaitOrders(timeStep);
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
            SubLightMoveTowardsPosition(EscortTarget.Center, timeStep);
            if (Owner.Center.InRadius(EscortTarget.Center, EscortTarget.Radius + 300f))
            {
                if (EscortTarget.TroopCapacity > EscortTarget.TroopCount)
                {
                    Owner.TryLandSingleTroopOnShip(EscortTarget);
                    return;
                }
                Orbit.Orbit(EscortTarget, timeStep);
            }
        }

        bool DoBombard(FixedSimTime timeStep, ShipGoal goal)
        {
            Planet planet = goal.TargetPlanet;
            if (!planet.TroopsHereAreEnemies(Owner.loyalty) && planet.Population <= 0f // Everyone is dead
                || planet.Owner != null && !Owner.loyalty.IsEmpireAttackable(planet.Owner))
            {
                ClearOrders();
                AddOrbitPlanetGoal(planet); // Stay in Orbit
            }

            Orbit.Orbit(planet, timeStep);
            if (planet.Owner == Owner.loyalty)
            {
                ClearOrders();
                return true; // skip combat rest of the update
            }

            DropBombsAtGoal(goal, Orbit.InOrbit);
            return false;
        }

        bool DoExterminate(FixedSimTime timeStep, ShipGoal goal)
        {
            Planet planet = goal.TargetPlanet;
            Orbit.Orbit(planet, timeStep);
            if (planet.Owner == Owner.loyalty || planet.Owner == null)
            {
                ClearOrders();
                OrderFindExterminationTarget();
                return true;
            }

            DropBombsAtGoal(goal, Orbit.InOrbit);
            return false;
        }
    }
}