using Microsoft.Xna.Framework;
using Ship_Game.Audio;
using Ship_Game.Gameplay;
using Ship_Game.Ships;
using System;
using System.Linq;
using System.Text;

namespace Ship_Game.AI
{
    public sealed partial class ShipAI
    {
        void DoBoardShip(float elapsedTime)
        {
            HasPriorityTarget = true;
            State = AIState.Boarding;
            if ((!EscortTarget?.Active ?? true)
                || EscortTarget.loyalty == Owner.loyalty)
            {
                ClearOrders(State);
                if (Owner.Mothership != null)
                {
                    if (Owner.Mothership.TroopsOut)
                        Owner.DoEscort(Owner.Mothership);
                    else
                        OrderReturnToHangar();
                }
                return;
            }
            ThrustOrWarpToPos(EscortTarget.Center, elapsedTime);
            float distance = Owner.Center.Distance(EscortTarget.Center);
            if (distance < EscortTarget.Radius + 300f)
            {
                if (Owner.TroopList.Count > 0)
                    Owner.TroopList[0].LandOnShip(EscortTarget);
            }
            else if (distance > 10000f && Owner.Mothership?.AI.CombatState == CombatState.AssaultShip)
                OrderReturnToHangar();
        }

        Ship UpdateCombatTarget()
        {
            if (Target?.Active != true || Target.engineState != Ship.MoveState.Sublight
                                       || !Owner.loyalty.IsEmpireAttackable(Target.GetLoyalty(), Target))
            {
                Target = PotentialTargets.FirstOrDefault(t => t.Active && t.engineState != Ship.MoveState.Warp &&
                                                         t.Center.InRadius(Owner.Center, Owner.SensorRange));
            }
            return Target;
        }

        void DoCombat(float elapsedTime)
        {
            Ship target = UpdateCombatTarget();
            if (target == null)
            {
                DequeueCurrentOrder();
                State = DefaultAIState;
                Owner.InCombat = false;
                return;
            }

            AwaitClosest = null; // TODO: Why is this set here?
            State = AIState.Combat;
            Owner.InCombat = true;
            Owner.InCombatTimer = 15f;

            if (!HasPriorityOrder && !HasPriorityTarget && Owner.Weapons.Count == 0 && !Owner.Carrier.HasActiveHangars)
                CombatState = CombatState.Evade;

            if (Owner.System != null && Owner.Carrier.HasActiveTroopBays)
                CombatState = CombatState.AssaultShip;

            // in range:
            if (target.Center.InRadius(Owner.Center, 7500f))
            {
                if (Owner.engineState == Ship.MoveState.Warp)
                    Owner.HyperspaceReturn();
                if (Owner.Carrier.HasHangars && !Owner.ManualHangarOverride)
                    Owner.Carrier.ScrambleFighters();
            }
            else
            {
                // TODO: need to move this into fleet.
                if (FleetNode != null && Owner.fleet != null)
                {
                    if (Owner.fleet.FleetTask == null)
                    {
                        Vector2 nodePos = Owner.fleet.AveragePosition() + FleetNode.FleetOffset;
                        if (target.Center.OutsideRadius(nodePos, FleetNode.OrdersRadius))
                        {

                            if (Owner.Center.OutsideRadius(nodePos, 1000f))
                            {
                                ThrustOrWarpToPos(nodePos, elapsedTime);
                            }
                            else
                            {
                                DoHoldPositionCombat(elapsedTime);
                            }

                            return;
                        }
                    }
                    else
                    {
                        var task = Owner.fleet.FleetTask;
                        if (target.Center.OutsideRadius(task.AO, task.AORadius + FleetNode.OrdersRadius))
                        {
                            DequeueCurrentOrder();
                        }
                    }
                }
                if (CombatState != CombatState.HoldPosition && CombatState != CombatState.Evade)
                {
                    Vector2 prediction = target.Center;
                    Weapon fastestWeapon = Owner.FastestWeapon;
                    if (fastestWeapon != null) // if we have a weapon
                    {
                        prediction = fastestWeapon.ProjectedImpactPointNoError(target);
                    }
                    ThrustOrWarpToPos(prediction, elapsedTime);
                    return;
                }
            }

            if (Intercepting && CombatState != CombatState.HoldPosition && CombatState != CombatState.Evade
                && Owner.Center.OutsideRadius(target.Center, Owner.DesiredCombatRange * 3f))
            {
                ThrustOrWarpToPos(target.Center, elapsedTime);
                return;
            }

            CombatAI.ExecuteCombatTactic(elapsedTime);

            // Target was modified by one of the CombatStates (?)
            Owner.InCombat = Target != null;
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

            if (target != null && (target.Center + g.Goal.TetherOffset).Distance(Owner.Center) > 200f)
            {
                g.Goal.BuildPosition = target.Center + g.Goal.TetherOffset;
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
                planetToTether.OrbitalStations.Add(orbital.guid, orbital);
            }
            Owner.QueueTotalRemoval();
        }

        void DoDeployOrbital(ShipGoal g)
        {
            if (g.Goal == null)
            {
                Log.Info($"There was no goal for Construction ship deploying orbital");
                OrderScrapShip();
                return;
            }

            Planet target = g.Goal.PlanetBuildingAt;
            if (target.Owner != Owner.loyalty) // FB - Planet owner has changed
            {
                OrderScrapShip();
                return;
            }

            Ship orbital = Ship.CreateShipAtPoint(g.Goal.ToBuildUID, Owner.loyalty, g.Goal.BuildPosition);
            if (orbital != null)
            {
                orbital.Center = g.Goal.BuildPosition;
                orbital.TetherToPlanet(target);
                target.OrbitalStations.Add(orbital.guid, orbital);
                Owner.QueueTotalRemoval();
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
                        StatTracker.StatAddRoad(node, Owner.loyalty);
                        return;
                    }
                }
            }
        }

        public void DoExplore(float elapsedTime)
        {
            HasPriorityOrder = true;
            IgnoreCombat = true;
            if (ExplorationTarget == null)
            {
                ExplorationTarget = Owner.loyalty.GetEmpireAI().AssignExplorationTarget(Owner);
                if (ExplorationTarget == null)
                {
                    ClearOrders();
                }
            }
            else if (DoExploreSystem(elapsedTime)) //@Notification
            {
                if (Owner.loyalty.isPlayer)
                {
                    //added by gremlin  add shamatts notification here
                    SolarSystem system = ExplorationTarget;
                    var message = new StringBuilder(system.Name); //@todo create global string builder
                    message.Append(" system explored.");

                    if (system.Sun.RadiationDamage > 0)
                        message.Append("\nThis Star emits radiation which will damage your ship's\nexternal modules or shields if they get close to it.");

                    var planetsTypesNumber = new Map<string, int>();
                    if (system.PlanetList.Count > 0)
                    {
                        foreach (Planet planet in system.PlanetList)
                        {
                            planetsTypesNumber.AddToValue(planet.CategoryName, 1);
                        }

                        foreach (var pair in planetsTypesNumber)
                            message.Append('\n').Append(pair.Value).Append(' ').Append(pair.Key);
                    }

                    foreach (Planet planet in system.PlanetList)
                    {
                        Building tile = planet.BuildingList.Find(t => t.IsCommodity);
                        if (tile != null)
                            message.Append('\n').Append(tile.Name).Append(" on ").Append(planet.Name);
                    }

                    if (system.HostileForcesPresent(Owner.loyalty))
                        message.Append("\nCombat in system!!!");

                    if (system.OwnerList.Count > 0 && !system.OwnerList.Contains(Owner.loyalty))
                        message.Append("\nContested system!!!");

                    Empire.Universe.NotificationManager.AddNotification(new Notification
                    {
                        Pause = false,
                        Message = message.ToString(),
                        ReferencedItem1 = system,
                        Icon = system.Sun.Icon,
                        Action = "SnapToExpandSystem"
                    }, "sd_ui_notification_warning");
                }
                ExplorationTarget = null;
            }
        }

        bool DoExploreSystem(float elapsedTime)
        {
            SystemToPatrol = ExplorationTarget;
            if (PatrolRoute.Count == 0)
            {
                foreach (Planet p in SystemToPatrol.PlanetList)
                    PatrolRoute.Add(p);

                if (SystemToPatrol.PlanetList.Count == 0)
                    return ExploreEmptySystem(elapsedTime, SystemToPatrol);
            }
            else
            {
                PatrolTarget = PatrolRoute[StopNumber];
                if (PatrolTarget.IsExploredBy(Owner.loyalty))
                {
                    StopNumber += 1;
                    if (StopNumber == PatrolRoute.Count)
                    {
                        StopNumber = 0;
                        PatrolRoute.Clear();
                        return true;
                    }
                }
                else
                {
                    MovePosition = PatrolTarget.Center;
                    float distanceToTarget = Owner.Center.Distance(MovePosition);
                    if (distanceToTarget < 75000f)
                        PatrolTarget.ParentSystem.SetExploredBy(Owner.loyalty);

                    if (distanceToTarget >= 5500f)
                    {
                        ThrustOrWarpToPos(MovePosition, elapsedTime, distanceToTarget);
                    }
                    else
                    {
                        ThrustOrWarpToPos(MovePosition, elapsedTime);
                        if (distanceToTarget < 500f)
                        {
                            PatrolTarget.SetExploredBy(Owner.loyalty);
                            StopNumber += 1;
                            if (StopNumber == PatrolRoute.Count)
                            {
                                StopNumber = 0;
                                PatrolRoute.Clear();
                                return true;
                            }
                        }
                    }
                }
            }
            return false;
        }

        void DoHoldPositionCombat(float elapsedTime)
        {
            if (Owner.CurrentVelocity > 0f)
            {
                ReverseThrustUntilStopped(elapsedTime);
                Vector2 interceptPoint = Owner.PredictImpact(Target);
                RotateTowardsPosition(interceptPoint, elapsedTime, 0.2f);
            }
            else
            {
                RotateTowardsPosition(Target.Center, elapsedTime, 0.2f);
            }
        }

        Vector2 LandingOffset;

        void DoLandTroop(float elapsedTime, ShipGoal goal)
        {
            Planet planet = goal.TargetPlanet;
            if (LandingOffset.AlmostZero())
                LandingOffset = RandomMath.Vector2D(planet.ObjectRadius);

            Vector2 landingSpot = planet.Center + LandingOffset;
            // force the ship out of warp if we get too close
            // this is a balance feature
            ThrustOrWarpToPos(landingSpot, elapsedTime, warpExitDistance: Owner.WarpOutDistance);
            if      (Owner.IsDefaultAssaultShuttle) LandTroopsViaSingleTransport(planet, landingSpot);
            else if (Owner.IsDefaultTroopShip)      LandTroopsViaSingleTransport(planet, landingSpot);
            else                                    LandTroopsViaTroopShip(elapsedTime, planet, landingSpot);
        }

        // Assault Shuttles will dump troops on the surface and return back to the troop ship to transport additional troops
        // Single Troop Ships can land from a longer distance, but the ship vanishes after landing its troop
        void LandTroopsViaSingleTransport(Planet planet, Vector2 landingSpot)
        {
            if (landingSpot.InRadius(Owner.Center, Owner.Radius + 40f))
            {
                Owner.LandAllTroopsAt(planet); // This will vanish default single Troop Ship
                DequeueCurrentOrder(); // make sure to clear this order, so we don't try to unload troops again

                // if it came from a mothership, return to hangar
                if (Owner.IsDefaultAssaultShuttle)
                    Owner.AI.OrderReturnToHangar();
            }
        }

        // Big Troop Ships will launch their own Assault Shuttles to land them on the planet
        void LandTroopsViaTroopShip(float elapsedTime, Planet planet, Vector2 landingSpot)
        {
            if (landingSpot.InRadius(Owner.Center, Owner.Radius + 100f))
            {
                if (planet.WeCanLandTroopsViaSpacePort(Owner.loyalty))
                    Owner.LandAllTroopsAt(planet); // We can land all our troops without assault bays since its our planet with space port
                else
                    Owner.Carrier.AssaultPlanet(planet); // Launch Assault shuttles or use Transporters (STSA)

                if (Owner.HasTroops)
                    Orbit.Orbit(planet, elapsedTime); // Doing orbit with AssaultPlanet state to continue landing troops if possible
                else
                    ClearOrders();
            }
        }

        void DoRebase(ShipGoal goal)
        {
            if (Owner.TroopList.IsEmpty)
                Owner.QueueTotalRemoval(); // troops not found, vanish the ship
            else if (!Owner.TroopList[0].TryLandTroop(goal.TargetPlanet))
                ClearOrders();
        }

        void DoRefit(ShipGoal goal)
        {
            if (goal.Goal == null) // empire goal was removed or planet was compromised
                ClearOrders();

            ClearOrders(AIState.HoldPosition);
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
            Ship repairMe = module.GetParent()
                    .loyalty.GetShips()
                    .FindMinFiltered(
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
                         && Owner.Center.Distance(s.Ship.Center) <= module.TransporterRange + 500f)
                .OrderBy(sw => Owner.Center.SqDist(sw.Ship.Center)).First();
            if (ship.Ship == null) return;

            int troopCount = 0;
            bool transported = false;
            for (int i = 0; i < Owner.TroopList.Count; ++i)
            {
                if (Owner.TroopList[i] == null)
                    continue;
                if (Owner.TroopList[i].Loyalty == Owner.loyalty)
                {
                    ship.Ship.TroopList.Add(Owner.TroopList[i]);
                    Owner.TroopList.Remove(Owner.TroopList[i]);
                    troopCount++;
                    transported = true;
                }
                if (troopCount == module.TransporterTroopAssault)
                    break;
            }
            if (transported) //@todo audio should not be here
            {
                module.TransporterTimer = module.TransporterTimerConstant;
                if (Owner.InFrustum)
                    GameAudio.PlaySfxAsync("transporter");
            }
        }

        void DoReturnToHangar(float elapsedTime)
        {
            if (Owner.Mothership == null || !Owner.Mothership.Active)
            {
                ClearOrders(State);
                if (Owner.shipData.Role == ShipData.RoleName.supply)
                    OrderScrapShip();
                else
                    GoOrbitNearestPlanetAndResupply(true);
                return;
            }
            ThrustOrWarpToPos(Owner.Mothership.Center, elapsedTime);

            if (Owner.Center.InRadius(Owner.Mothership.Center, Owner.Mothership.Radius + 300f))
            {
                if (Owner.TroopList.Count == 1)
                    Owner.Mothership.TroopList.Add(Owner.TroopList[0]);
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
                    if (hangar.GetHangarShip() != Owner)
                        continue;

                    hangar.SetHangarShip(null);
                    // FB - Here we are setting the hangar timer according to the R&R time. Cant be over the time to rebuild the ship
                    hangar.hangarTimer = (repairTime + rearmTime + shuttlePrepTime).Clamped(5, hangar.hangarTimerConstant);
                    hangar.HangarShipGuid = Guid.Empty;
                }
            }
        }

        void DoReturnHome(float elapsedTime)
        {
            if (Owner.HomePlanet.Owner != Owner.loyalty)
            {
                // find another friendly planet to land at
                Owner.UpdateHomePlanet(Owner.loyalty.RallyShipYardNearestTo(Owner.Center));
                if (Owner.HomePlanet == null)
                {
                    // Nowhere to land, bye bye.
                    Owner.ScuttleTimer = 1;
                    return;
                }
            }
            ThrustOrWarpToPos(Owner.HomePlanet.Center, elapsedTime);
            if (Owner.Center.InRadius(Owner.HomePlanet.Center, Owner.HomePlanet.ObjectRadius + 150f))
            {
                Owner.HomePlanet.LandDefenseShip(Owner.DesignRole, Owner.GetCost(Owner.loyalty), Owner.HealthPercent);
                Owner.QueueTotalRemoval();
            }
            if (Owner.InCombat)
            {
                ClearOrders();
            }
        }

        void DoRebaseToShip(float elapsedTime)
        {
            if (EscortTarget == null || !EscortTarget.Active
                                     || EscortTarget.AI.State == AIState.Scrap
                                     || EscortTarget.AI.State == AIState.Refit)
            {
                OrderRebaseToNearest();
                return;
            }

            ThrustOrWarpToPos(EscortTarget.Center, elapsedTime);
            if (Owner.Center.InRadius(EscortTarget.Center, EscortTarget.Radius + 300f))
            {
                if (EscortTarget.TroopCapacity == EscortTarget.TroopList.Count)
                {
                    OrderRebaseToNearest();
                    return;
                }

                Owner.TroopList[0].LandOnShip(EscortTarget);
            }
        }

        void DoSupplyShip(float elapsedTime)
        {
            if (EscortTarget == null || !EscortTarget.Active
                                     || EscortTarget.AI.State == AIState.Resupply
                                     || EscortTarget.AI.State == AIState.Scrap
                                     || EscortTarget.AI.State == AIState.Refit)
            {
                OrderReturnToHangar();
                return;
            }

            ThrustOrWarpToPos(EscortTarget.Center, elapsedTime);
            if (Owner.Center.InRadius(EscortTarget.Center, EscortTarget.Radius + 300f))
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

        void DoResupplyEscort(float elapsedTime, ShipGoal goal)
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

            ThrustOrWarpToPos(escortVector, elapsedTime, escortVelocity);

            switch (goal.VariableString)
            {
                default:       TerminateResupplyIfDone();                  break;
                case "Rearm":  TerminateResupplyIfDone(SupplyType.Rearm);  break;
                case "Repair": TerminateResupplyIfDone(SupplyType.Repair); break;
                case "Troops": TerminateResupplyIfDone(SupplyType.Troops); break;
            }
        }

        void DoSystemDefense(float elapsedTime)
        {
            SystemToDefend = SystemToDefend ?? Owner.System;
            if (SystemToDefend == null || AwaitClosest?.Owner == Owner.loyalty)
                AwaitOrders(elapsedTime);
            else
                OrderSystemDefense(SystemToDefend);
        }

        void DoTroopToShip(float elapsedTime, ShipGoal goal)
        {
            if (EscortTarget == null || !EscortTarget.Active)
            {
                ClearOrders();
                return;
            }
            SubLightMoveTowardsPosition(EscortTarget.Center, elapsedTime);
            if (Owner.Center.InRadius(EscortTarget.Center, EscortTarget.Radius + 300f))
            {
                if (EscortTarget.TroopCapacity > EscortTarget.TroopList.Count)
                {
                    EscortTarget.TroopList.Add(Owner.TroopList[0]);
                    Owner.QueueTotalRemoval();
                    return;
                }
                Orbit.Orbit(EscortTarget, elapsedTime);
            }
        }

        bool DoBombard(float elapsedTime, ShipGoal goal)
        {
            Planet planet = goal.TargetPlanet;
            if (Owner.Ordinance < 0.05 * Owner.OrdinanceMax //'Aint Got no bombs!
                || planet.TroopsHere.Count == 0 && planet.Population <= 0f //Everyone is dead
                || (planet.GetGroundStrengthOther(Owner.loyalty) + 1) * 1.5
                <= planet.GetGroundStrength(Owner.loyalty)
                || (planet.Owner != null && !Owner.loyalty.IsEmpireAttackable(planet.Owner))
                )
                //This will tilt the scale just enough so that if there are 0 troops, a planet can still be bombed.
            {
                //As far as I can tell, if there were 0 troops on the planet, then GetGroundStrengthOther and GetGroundStrength would both return 0,
                //meaning that the planet could not be bombed since that part of the if statement would always be true (0 * 1.5 <= 0)
                //Adding +1 to the result of GetGroundStrengthOther tilts the scale just enough so a planet with no troops at all can still be bombed
                //but having even 1 allied troop will cause the bombing action to abort.
                ClearOrders();
                AddOrbitPlanetGoal(planet); // Stay in Orbit
            }
            Orbit.Orbit(planet, elapsedTime);
            float radius = planet.ObjectRadius + Owner.Radius + 1500;
            if (planet.Owner == Owner.loyalty)
            {
                ClearOrders();
                return true; // skip combat rest of the update
            }
            DropBombsAtGoal(goal, radius);
            return false;
        }

        bool DoExterminate(float elapsedTime, ShipGoal goal)
        {
            Planet planet = goal.TargetPlanet;
            Orbit.Orbit(planet, elapsedTime);
            if (planet.Owner == Owner.loyalty || planet.Owner == null)
            {
                ClearOrders();
                OrderFindExterminationTarget();
                return true;
            }
            float radius = planet.ObjectRadius + Owner.Radius + 1500;
            DropBombsAtGoal(goal, radius);
            return false;
        }
    }
}