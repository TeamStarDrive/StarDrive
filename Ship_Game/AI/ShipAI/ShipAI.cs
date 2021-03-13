using Microsoft.Xna.Framework;
using Ship_Game.AI.ShipMovement;
using Ship_Game.Gameplay;
using Ship_Game.Ships;
using Ship_Game.Utils;
using System;
using System.Linq;
using Ship_Game.Ships.AI;

namespace Ship_Game.AI
{
    public sealed partial class ShipAI : IDisposable
    {
        Planet AwaitClosest;
        Planet PatrolTarget;
        float UtilityModuleCheckTimer;
        int StopNumber;
        public FleetDataNode FleetNode;

        public readonly Ship Owner;
        public AIState State = AIState.AwaitingOrders;
        public Guid OrbitTargetGuid;
        public Planet ColonizeTarget;
        public Planet ResupplyTarget;
        public Guid SystemToDefendGuid;
        public SolarSystem SystemToDefend;
        public SolarSystem ExplorationTarget;
        public AIState DefaultAIState = AIState.AwaitingOrders;
        public SafeQueue<ShipGoal> OrderQueue  = new SafeQueue<ShipGoal>();
        public Array<ShipWeight>   NearByShips = new Array<ShipWeight>();
        public BatchRemovalCollection<Ship> FriendliesNearby = new BatchRemovalCollection<Ship>();

        readonly DropOffGoods DropOffGoods;
        readonly PickupGoods PickupGoods;

        readonly OrbitPlan Orbit;
        
        public ShipAI(Ship owner)
        {
            Owner = owner;
            DropOffGoods = new DropOffGoods(this);
            PickupGoods = new PickupGoods(this);
            Orbit = new OrbitPlan(this);
        }

        // Resets all important state of the AI
        public void Reset()
        {
            Target = null;
            ColonizeTarget = null;
            ResupplyTarget = null;
            EscortTarget = null;
            SystemToDefend = null;
            ExplorationTarget = null;

            ClearOrders();
            PotentialTargets.Clear();
            TrackProjectiles.Clear();
            NearByShips.Clear();
            FriendliesNearby.Clear();
        }

        public Vector2 GoalTarget
        {
            get
            {
                if (OrderQueue.NotEmpty)
                {
                    ShipGoal goal = OrderQueue.PeekFirst;
                    Vector2 pos = goal.TargetPlanet?.Center ?? goal.MovePosition;
                    if (pos.NotZero())
                        return pos;
                }
                return Target?.Position
                    ?? ExplorationTarget?.Position
                    ?? SystemToDefend?.Position
                    ?? ColonizeTarget?.Center
                    ?? ResupplyTarget?.Center
                    ?? Vector2.Zero;
            }
        }

        void Colonize(Planet targetPlanet, ShipGoal shipGoal)
        {
            if (Owner.Center.OutsideRadius(targetPlanet.Center, 2000f))
            {
                DequeueCurrentOrder();
                OrderColonization(targetPlanet, shipGoal.Goal);
                return;
            }

            if (targetPlanet.Owner != null || !targetPlanet.Habitable || targetPlanet.RecentCombat)
            {
                shipGoal.Goal?.NotifyMainGoalCompleted();
                ClearOrders();
                return;
            }

            ColonizeTarget = targetPlanet;
            ColonizeTarget.Colonize(Owner);
            Owner.QueueTotalRemoval();
        }

        bool TryGetClosestUnexploredPlanet(SolarSystem system, out Planet planet)
        {
            planet = PatrolTarget;
            if (PatrolTarget != null && !PatrolTarget.IsExploredBy(Owner.loyalty))
                return true;

            if (system.IsFullyExploredBy(Owner.loyalty))
                return false;

            planet = system.PlanetList.FindMinFiltered(p => !p.IsExploredBy(Owner.loyalty), p => Owner.Center.SqDist(p.Center));
            return planet != null;
        }

        void DoScrapShip(FixedSimTime timeStep, ShipGoal goal)
        {
            if (goal.TargetPlanet.Center.Distance(Owner.Center) >= goal.TargetPlanet.ObjectRadius * 3)
            {
                Orbit.Orbit(goal.TargetPlanet, timeStep);
                return;
            }

            if (goal.TargetPlanet.Center.Distance(Owner.Center) >= goal.TargetPlanet.ObjectRadius)
            {
                ThrustOrWarpToPos(goal.TargetPlanet.Center, timeStep, 200f);
                return;
            }

            // Waiting to be scrapped by Empire goal
            if (!Owner.loyalty.GetEmpireAI().Goals.Any(g => g.type == GoalType.ScrapShip && g.OldShip == Owner))
                ClearOrders(); // Could not find empire scrap goal 
        }

        public void Update(FixedSimTime timeStep)
        {
            if (State == AIState.AwaitingOrders && DefaultAIState == AIState.Exterminate )
                State = AIState.Exterminate;

            CheckTargetQueue();

            PrioritizePlayerCommands();
            if (HadPO && State != AIState.AwaitingOrders)
                HadPO = false;

            ResetStateFlee();

            ApplySensorScanResults();
            Owner.loyalty.data.Traits.ApplyTraitToShip(Owner);
            UpdateUtilityModuleAI(timeStep);
            ThrustTarget = Vector2.Zero;

            UpdateCombatStateAI(timeStep);

            if (UpdateOrderQueueAI(timeStep))
                return;

            AIStateRebase();
        }

        public void ApplySensorScanResults()
        {
            // scanning is done from the asyncdatacollector. 
            // scancomplete means the scan is done.
            if (ScanComplete && !ScanDataProcessed)
            {
                TrackProjectiles  = new Array<Projectile>(ScannedProjectiles);
                PotentialTargets  = new BatchRemovalCollection<Ship>(ScannedTargets);
                FriendliesNearby  = new BatchRemovalCollection<Ship>(ScannedFriendlies);
                NearByShips       = new Array<ShipWeight>(ScannedNearby);
                ScanTargetUpdated = true;
                
                ScannedProjectiles.Clear();
                ScannedTargets.Clear();
                ScannedFriendlies.Clear();
                ScannedNearby.Clear();
                ScanDataProcessed = true;
            }

            if (ScanTargetUpdated)
            {
                if (!HasPriorityOrder && (!HasPriorityTarget || Target?.Active == false))
                    Target = ScannedTarget;
                ScanTargetUpdated = false;
            }
        }

        public Ship NearBySupplyShip => 
            FriendliesNearby.FindMinFiltered(supply => supply.Carrier.HasSupplyBays && supply.SupplyShipCanSupply,
        supply => -supply.Center.SqDist(Owner.Center));

        public Ship NearByRepairShip => 
            FriendliesNearby.FindMinFiltered(supply => supply.hasRepairBeam || supply.HasRepairModule,
        supply => -supply.Center.SqDist(Owner.Center));
        public void ProcessResupply(ResupplyReason resupplyReason)
        {
            Planet nearestRallyPoint = null;
            switch (resupplyReason)
            {
                case ResupplyReason.LowOrdnanceCombat:
                case ResupplyReason.LowOrdnanceNonCombat:
                    if (Owner.IsPlatformOrStation) // Mostly for Orbitals in Deep Space
                    {
                        RequestResupplyFromPlanet();
                        return;
                    }

                    Ship supplyShip = NearBySupplyShip;
                    if (supplyShip != null)
                    {
                        SetUpSupplyEscort(supplyShip, supplyType: "Rearm");
                        return;
                    }

                    nearestRallyPoint = Owner.loyalty.FindNearestSafeRallyPoint(Owner.Center);
                    break;
                case ResupplyReason.RequestResupplyFromPlanet:
                    RequestResupplyFromPlanet();
                    return;
                case ResupplyReason.NoCommand:
                case ResupplyReason.LowHealth:
                    Ship repairShip = NearByRepairShip;
                    if (repairShip != null)
                        SetUpSupplyEscort(repairShip, supplyType: "Repair");
                    else
                        nearestRallyPoint = Owner.loyalty.FindNearestSafeRallyPoint(Owner.Center);
                    break;
                case ResupplyReason.LowTroops:
                    if (Owner.Carrier.SendTroopsToShip)
                    {
                        for (int i = 0; i < Owner.Carrier.MissingTroops - Owner.NumTroopsRebasingHere; ++i)
                        {
                            if (Owner.loyalty.GetTroopShipForRebase(out Ship troopShip, Owner))
                                troopShip.AI.OrderRebaseToShip(Owner);
                        }

                        return;
                    }

                    nearestRallyPoint = Owner.loyalty.SafeSpacePorts.FindMax(p => p.TroopsHere.Count);
                    break;
                case ResupplyReason.NotNeeded:
                    TerminateResupplyIfDone();
                    return;
            }

            if (Owner.IsPlatformOrStation)
                return;

            SetPriorityOrder(true);
            DecideWhereToResupply(nearestRallyPoint);
        }

        void SetUpSupplyEscort(Ship supplyShip, string supplyType = "All")
        {
            IgnoreCombat = true;
            ClearOrders(AIState.ResupplyEscort);
            EscortTarget = supplyShip;

            float strafeOffset = Owner.Radius + supplyShip.Radius + UniverseRandom.RandomBetween(200, 1000);
            AddShipGoal(Plan.ResupplyEscort, Vector2.Zero, UniverseRandom.RandomDirection()
                , null, supplyType, strafeOffset, AIState.ResupplyEscort, pushToFront: true);
        }

        void DecideWhereToResupply(Planet nearestRallyPoint, bool cancelOrders = false)
        {
            if (nearestRallyPoint != null)
                OrderResupply(nearestRallyPoint, cancelOrders);
            else
            {
                nearestRallyPoint = Owner.loyalty.FindNearestRallyPoint(Owner.Center);
                if (nearestRallyPoint != null)
                    OrderResupply(nearestRallyPoint, cancelOrders);
                else if (Owner.loyalty.WeArePirates)
                    OrderPirateFleeHome();
                else
                    OrderFlee(true);
            }
        }

        public void TerminateResupplyIfDone(SupplyType supplyType = SupplyType.All)
        {
            if (Owner.AI.State != AIState.Resupply && Owner.AI.State != AIState.ResupplyEscort)
                return;

            if (!Owner.Supply.DoneResupplying(supplyType)) 
            {
                if (State != AIState.ResupplyEscort || EscortTarget?.SupplyShipCanSupply == true)
                    return;
            }

            DequeueCurrentOrder();
            Owner.AI.SetPriorityOrder(false);
            Owner.AI.IgnoreCombat = false;
            if (Owner.fleet != null)
                OrderMoveTo(Owner.fleet.FinalPosition + Owner.RelativeFleetOffset, 
                    Owner.fleet.FinalDirection, true, State);
        }

        void RequestResupplyFromPlanet()
        {
            if (Owner.GetTether()?.Owner == Owner.loyalty)
                return;

            EmpireAI ai = Owner.loyalty.GetEmpireAI();
            if (ai.Goals.Any(g => g.type == GoalType.RearmShipFromPlanet && g.TargetShip == Owner))
                return; // Supply ship is on the way

            var possiblePlanets = Owner.loyalty.GetPlanets().Filter(p => p.NumSupplyShuttlesCanLaunch() > 0);
            if (possiblePlanets.Length == 0)
                return;

            Planet planet = possiblePlanets.FindMin(p => p.Center.SqDist(Owner.Center));
            ai.AddPlanetaryRearmGoal(Owner, planet);
        }

        public void FireWeapons(FixedSimTime timeStep)
        {
            TriggerDelay -= timeStep.FixedTime;
            FireOnMainTargetTime -= timeStep.FixedTime;
            if (TriggerDelay < 0)
            {
                TriggerDelay = timeStep.FixedTime * 2;
                FireOnTarget();
            }
        }


        void UpdateCombatStateAI(FixedSimTime timeStep)
        {
            FireWeapons(timeStep);

            if (BadGuysNear && !IgnoreCombat && !HasPriorityOrder)
            {
                if (Owner.Weapons.Count > 0 || Owner.Carrier.HasActiveHangars || Owner.Carrier.HasTransporters)
                {
                    if (Target != null && !HasPriorityOrder && State != AIState.Resupply )
                    {
                        switch (OrderQueue.PeekFirst?.Plan)
                        {
                            default: OrderQueue.PushToFront(new ShipGoal(Plan.DoCombat, State)); break;
                            case Plan.DoCombat:
                            case Plan.Bombard:
                            case Plan.BoardShip: break;
                        }
                    }
                }
            }
            else if (!BadGuysNear)
            {
                int count            = Owner.Weapons.Count;
                FireOnMainTargetTime = 0;
                Weapon[] items       = Owner.Weapons.GetInternalArrayItems();
                for (int x = 0; x < count; x++)
                    items[x].ClearFireTarget();

                if (Owner.Carrier.HasHangars)
                {
                    foreach (ShipModule hangar in Owner.Carrier.AllFighterHangars)
                    {
                        if (hangar.TryGetHangarShip(out Ship hangarShip) 
                            && hangarShip.Active 
                            && hangarShip.AI.State != AIState.ReturnToHangar)
                        {
                            if (Owner.loyalty == Empire.Universe.player
                                && (hangarShip.AI.HasPriorityTarget || hangarShip.AI.HasPriorityOrder))
                            {
                                continue;
                            }

                            if (Owner.Carrier.FightersLaunched)
                                hangarShip.DoEscort(Owner);
                            else
                                hangarShip.AI.OrderReturnToHangar();
                        }
                    }
                }
            }

            // fbedard: civilian ships will evade combat (nice target practice)
            if (Owner.shipData.ShipCategory == ShipData.Category.Civilian && BadGuysNear)
            {
                if (Owner.WeaponsMaxRange <= 0 || PotentialTargets.Sum(o => o.GetStrength()) < Owner.GetStrength())
                {
                    CombatState = CombatState.Evade;
                }
                

            }
        }

        void AIStateRebase()
        {
            if (State != AIState.Rebase) 
                return;

            if (OrderQueue.IsEmpty)
            {
                OrderRebaseToNearest();
                return;
            }

            for (int x = 0; x < OrderQueue.Count; x++)
            {
                ShipGoal goal = OrderQueue[x];
                if (goal.Plan == Plan.Rebase 
                    && goal.TargetPlanet.Owner != Owner.loyalty
                    && !Owner.loyalty.isPlayer) // player rebase is not cancelled 
                {
                    ClearOrders();
                    return;
                }
            }
        }

        bool UpdateOrderQueueAI(FixedSimTime timeStep)
        {
            if (OrderQueue.IsEmpty)
            {
                UpdateFromAIState(timeStep);
                return false;
            }
            return EvaluateNextOrderQueueItem(timeStep);
        }

        bool EvaluateNextOrderQueueItem(FixedSimTime timeStep)
        {
            ShipGoal goal = OrderQueue.PeekFirst;
            switch (goal?.Plan)
            {
                case Plan.Stop:
                    if (ReverseThrustUntilStopped(timeStep)) { DequeueCurrentOrder(); }       break;
                case Plan.Bombard:                  return DoBombard(timeStep, goal);
                case Plan.Exterminate:              return DoExterminate(timeStep, goal);
                case Plan.Scrap:                    DoScrapShip(timeStep, goal);              break;
                case Plan.RotateToFaceMovePosition: RotateToFaceMovePosition(timeStep, goal); break;
                case Plan.RotateToDesiredFacing:    RotateToDesiredFacing(timeStep, goal);    break;
                case Plan.MoveToWithin1000:         MoveToWithin1000(timeStep, goal);         break;
                case Plan.MakeFinalApproach:        MakeFinalApproach(timeStep, goal);        break;
                case Plan.RotateInlineWithVelocity: RotateInLineWithVelocity(timeStep);       break;
                case Plan.Orbit:                    Orbit.Orbit(goal.TargetPlanet, timeStep); break;
                case Plan.Colonize:                 Colonize(goal.TargetPlanet, goal);        break;
                case Plan.Explore:                  DoExplore(timeStep);                      break;
                case Plan.Rebase:                   DoLandTroop(timeStep, goal);              break;
                case Plan.DefendSystem:             DoSystemDefense(timeStep);                break;
                case Plan.DoCombat:                 DoCombat(timeStep);                       break;
                case Plan.DeployStructure:          DoDeploy(goal);                           break;
                case Plan.DeployOrbital:            DoDeployOrbital(goal);                    break;
                case Plan.PickupGoods:              PickupGoods.Execute(timeStep, goal);      break;
                case Plan.DropOffGoods:             DropOffGoods.Execute(timeStep, goal);     break;
                case Plan.ReturnToHangar:           DoReturnToHangar(timeStep);               break;
                case Plan.TroopToShip:              DoTroopToShip(timeStep, goal);            break;
                case Plan.BoardShip:                DoBoardShip(timeStep);                    break;
                case Plan.SupplyShip:               DoSupplyShip(timeStep);                   break;
                case Plan.RearmShipFromPlanet:      DoRearmShip(timeStep);                    break;
                case Plan.Refit:                    DoRefit(goal);                            break;
                case Plan.LandTroop:                DoLandTroop(timeStep, goal);              break;
                case Plan.ResupplyEscort:           DoResupplyEscort(timeStep, goal);         break;
                case Plan.ReturnHome:               DoReturnHome(timeStep);                   break;
                case Plan.RebaseToShip:             DoRebaseToShip(timeStep);                 break;
                case Plan.HoldPosition:             HoldPosition();                           break;
                case Plan.HoldPositionOffensive:    HoldPositionOffensive();                  break;
                case Plan.Escort:                   AIStateEscort(timeStep);                  break;
                case Plan.Meteor:                   DoMeteor(timeStep, goal);                 break;
            }

            return false;
        }

        void UpdateFromAIState(FixedSimTime timeStep)
        {
            if (Owner.fleet == null)
            {
                ClearWayPoints();
                switch (State)
                {
                    case AIState.DoNothing:      AwaitOrders(timeStep);           break;
                    case AIState.AwaitingOrders: AIStateAwaitingOrders(timeStep); break;
                    case AIState.Escort:         AIStateEscort(timeStep);         break;
                    case AIState.SystemDefender: AwaitOrders(timeStep); break;
                    case AIState.Resupply:       AwaitOrders(timeStep); break;
                    case AIState.ReturnToHangar: DoReturnToHangar(timeStep); break;
                    case AIState.AwaitingOffenseOrders: break;
                    case AIState.Exterminate:
                        OrderFindExterminationTarget(); break;
                    default:
                        if (Target != null)
                        {
                            Orbit.Orbit(Target, timeStep);
                        }
                        break;
                }
            }
            else
            {
                SetPriorityOrder(false);
                IdleFleetAI(timeStep);
            }
        }

        bool DoNearFleetOffset(FixedSimTime timeStep)
        {
            if (NearFleetPosition())
            {
                ReverseThrustUntilStopped(timeStep);
                RotateToDirection(Owner.fleet.FinalDirection, timeStep, 0.02f);
                return true;
            }
            return false;
        }

        bool NearFleetPosition() => Owner.Center.InRadius(Owner.fleet.GetFinalPos(Owner), 75f);

        bool ShouldReturnToFleet()
        {
            // separated for clarity as this section can be very confusing.
            // we might need a toggle for the player action here.
            if (State == AIState.FormationWarp && HasPriorityOrder || HadPO)
                return true;
            if (HasPriorityOrder || HadPO)
                return false;
            if (BadGuysNear)
                return false;
            if (!Owner.CanTakeFleetMoveOrders()) return false;
            if (State == AIState.Orbit ||
                State == AIState.AwaitingOffenseOrders ||
                State == AIState.AwaitingOrders)
                return true;
            return false;
        }

        void IdleFleetAI(FixedSimTime timeStep)
        {

            if (DoNearFleetOffset(timeStep))
            {
                if (State != AIState.HoldPosition && !Owner.fleet.HasFleetGoal && Owner.CanTakeFleetMoveOrders())
                    State = AIState.AwaitingOrders;
                return;
            }

            if (ShouldReturnToFleet())
            {
                // check if inside minimum warp jump range. If not do a full warp process.
                if (Owner.fleet.FinalPosition.InRadius(Owner.Center, 7500))
                {
                    SetPriorityOrder(true);  // FB this might cause serious issues that make orbiting ships stuck with PO and not available anymore for the AI.
                    State = AIState.AwaitingOrders;
                    AddShipGoal(Plan.MakeFinalApproach,
                        Owner.fleet.GetFormationPos(Owner), Owner.fleet.FinalDirection, AIState.MoveTo);
                }
                else
                {
                    WarpToFleet();
                }
            }
            else
            {
                if (State != AIState.HoldPosition && !Owner.fleet.HasFleetGoal && Owner.CanTakeFleetMoveOrders())
                    State = AIState.AwaitingOrders;
            }
        }

        void WarpToFleet()
        {
            ClearWayPoints();
            State = AIState.AwaitingOrders;
            if (Owner.fleet.HasFleetGoal)
            {
                // TODO: do we need this? Is this even correct?
                WayPoints.Enqueue(new WayPoint(Owner.fleet.NextGoalMovePosition + Owner.FleetOffset,
                                               Owner.fleet.FinalDirection));
            }
            else
            {
                OrderMoveTo(Owner.fleet.GetFinalPos(Owner), Owner.fleet.FinalDirection, true, AIState.MoveTo);
            }
        }

        public bool HasTradeGoal(Goods goods)
        {
            return OrderQueue.Any(g => g.Trade?.Goods == goods);
        }

        public bool WaitForBlockadeRemoval(ShipGoal g, Planet planet, FixedSimTime timeStep)
        {
            if (planet.TradeBlocked && Owner.System != planet.ParentSystem)
            {
                g.Trade.BlockadeTimer -= timeStep.FixedTime;
                if (g.Trade.BlockadeTimer > 0f && !planet.Quarantine)
                {
                    ReverseThrustUntilStopped(timeStep);
                    return true;
                }

                // blockade is going on for too long or manual quarantine, abort
                ClearOrders();
                State = AIState.AwaitingOrders;
                Planet fallback = Owner.loyalty.FindNearestRallyPoint(Owner.Center);
                if (fallback != planet)
                    AddOrbitPlanetGoal(fallback, AIState.AwaitingOrders);

                return true;
            }

            g.Trade.BlockadeTimer = 120f; // blockade was removed, continue as planned
            return false;
        }

        // @note This is only called via user interaction, so not performance critical
        public bool ClearOrdersIfCombat()
        {
            bool clearOrders = OrderQueue.Any(goal => goal.Plan == Plan.DoCombat);
            if (clearOrders)
                ClearOrders();
            return clearOrders;
        }

        public void CancelTradePlan(Planet orbitPlanet = null)
        {
            ClearOrders();
            if (orbitPlanet != null)
                AddOrbitPlanetGoal(orbitPlanet, AIState.AwaitingOrders);
            else
                State = AIState.AwaitingOrders;
        }

        void UpdateUtilityModuleAI(FixedSimTime timeStep)
        {
            UtilityModuleCheckTimer -= timeStep.FixedTime;
            if (Owner.engineState != Ship.MoveState.Warp && UtilityModuleCheckTimer <= 0f)
            {
                UtilityModuleCheckTimer = 1f;
                //Added by McShooterz: logic for transporter modules
                if (Owner.Carrier.HasTransporters)
                    for (int x = 0; x < Owner.Carrier.AllTransporters.Length; x++)
                    {
                        ShipModule module = Owner.Carrier.AllTransporters[x];
                        if (module.TransporterTimer > 0f || !module.Active || !module.Powered ||
                            module.TransporterPower >= Owner.PowerCurrent) continue;
                        if (FriendliesNearby.Count > 0 && module.TransporterOrdnance > 0 && Owner.Ordinance > 0)
                            DoOrdinanceTransporterLogic(module);
                        if (module.TransporterTroopAssault > 0 && Owner.HasOurTroops)
                            DoAssaultTransporterLogic(module);
                    }

                //Do repair check if friendly ships around
                if (FriendliesNearby.Count <= 0)
                    return;
                //Added by McShooterz: logic for repair beams
                if (Owner.hasRepairBeam)
                    for (int x = 0; x < Owner.RepairBeams.Count; x++)
                    {
                        ShipModule module = Owner.RepairBeams[x];
                        if (module.InstalledWeapon.CooldownTimer <= 0f &&
                            module.InstalledWeapon.Module.Powered &&
                            Owner.Ordinance >= module.InstalledWeapon.OrdinanceRequiredToFire &&
                            Owner.PowerCurrent >= module.InstalledWeapon.PowerRequiredToFire)
                            DoRepairBeamLogic(module.InstalledWeapon);
                    }

                if (!Owner.HasRepairModule) return;
                for (int x = 0; x < Owner.Weapons.Count; x++)
                {
                    Weapon weapon = Owner.Weapons[x];
                    if (weapon.CooldownTimer > 0f || !weapon.Module.Powered ||
                        Owner.Ordinance < weapon.OrdinanceRequiredToFire ||
                        Owner.PowerCurrent < weapon.PowerRequiredToFire || !weapon.IsRepairDrone)
                    {
                        //Gretman -- Added this so repair drones would cooldown outside combat (+15s)
                        if (weapon.CooldownTimer > 0f)
                            weapon.CooldownTimer = MathHelper.Max(weapon.CooldownTimer - 1, 0f);
                        continue;
                    }

                    DoRepairDroneLogic(weapon);
                }
            }
        }

        void DoMeteor(FixedSimTime timeStep, ShipGoal g)
        {
            if (Owner.SecondsAlive > 1 &&  Owner.System == null)
                Owner.Die(null, true);

            Owner.Position += g.Direction.Normalized() * g.SpeedLimit * timeStep.FixedTime;
            if (Owner.Position.InRadius(g.TargetPlanet.Center, g.TargetPlanet.GravityWellRadius/2))
            {
                Owner.PlanetCrash = new PlanetCrash(g.TargetPlanet, Owner, g.SpeedLimit, true);
                Owner.dying       = true;
            }
        }

        public void OrderTroopToBoardShip(Ship s)
        {
            ClearOrders(priority: true);
            EscortTarget = s;
            AddShipGoal(Plan.BoardShip, State);
        }

        public void OrderTroopToShip(Ship s)
        {
            ClearOrders();
            EscortTarget = s;
            AddShipGoal(Plan.TroopToShip, State);
        }
        
        public void StartSensorScan(FixedSimTime timeStep)
        {
            ScanDataProcessed = false;
            ScanComplete = false;
            float maxContactTimer = timeStep.FixedTime;
            ScanForThreatTimer = maxContactTimer;
            ScanForTargets();
        }

        public void DoManualSensorScan(FixedSimTime timeStep)
        {
            StartSensorScan(timeStep);
            ApplySensorScanResults();
        }

        void ResetStateFlee()
        {
            if (State != AIState.Flee || BadGuysNear || State == AIState.Resupply || HasPriorityOrder) return;
            if (OrderQueue.NotEmpty)
                OrderQueue.RemoveLast();
        }


        void PrioritizePlayerCommands()
        {
            if (Owner.loyalty == EmpireManager.Player &&
                (State == AIState.Bombard 
                || State == AIState.AssaultPlanet 
                || State == AIState.Rebase 
                || State == AIState.Scrap 
                || State == AIState.Resupply 
                || State == AIState.Refit))
            {
                SetPriorityOrder(true);
                HadPO = false;
                EscortTarget = null;
            }
        }

        void CheckTargetQueue()
        {
            if (!HasPriorityTarget)
                TargetQueue.Clear();
            for (int x = TargetQueue.Count - 1; x >= 0; x--)
            {
                Ship target = TargetQueue[x];
                if (target.Active)
                    continue;
                TargetQueue.RemoveAtSwapLast(x);
            }
        }

        void AIStateEscort(FixedSimTime timeStep)
        {
            Owner.AI.SetPriorityOrder(false);
            var escortTarget = EscortTarget;
            if (escortTarget == null || !escortTarget.Active)
            {
                EscortTarget = null;
                ClearOrders();
                if (Owner.IsHangarShip && Owner.Mothership.Active)
                {
                    OrderReturnToHangar();
                    return;
                }

                State = AIState.AwaitingOrders; //fbedard
                if (Owner.loyalty.WeArePirates)
                    OrderPirateFleeHome();

                return;
            }

            if (Owner.GetStrength() <= 0 
                || !Owner.IsHangarShip && escortTarget.Center.InRadius(Owner.Center, Owner.SensorRange) 
                || !Owner.IsHangarShip
                || !Owner.Mothership.AI.BadGuysNear 
                || escortTarget != Owner.Mothership)
            {
                Orbit.Orbit(escortTarget, timeStep);
                return;
            }
            // Doctor: This should make carrier-launched fighters scan for their own combat targets, except using the mothership's position
            // and a standard 30k around it instead of their own. This hopefully will prevent them flying off too much, as well as keeping them
            // in a carrier-based role while allowing them to pick appropriate target types depending on the fighter type.
            // gremlin Moved to setcombat status as target scan is expensive and did some of this already. this also shortcuts the UseSensorforTargets switch. Im not sure abuot the using the mothership target.
            // i thought i had added that in somewhere but i cant remember where. I think i made it so that in the scan it takes the motherships target list and adds it to its own.
            if(!Owner.InCombat )
            {
                Orbit.Orbit(escortTarget, timeStep);
                return;
            }

            if (Owner.InCombat && Owner.Center.OutsideRadius(escortTarget.Center, Owner.DesiredCombatRange))
            {
                Owner.AI.SetPriorityOrder(true);
                Orbit.Orbit(escortTarget, timeStep);
            }
        }

        void AIStateAwaitingOrders(FixedSimTime timeStep)
        {
            if (Owner.loyalty != Empire.Universe.player)
                AwaitOrders(timeStep);
            else
                AwaitOrdersPlayer(timeStep);
        }

        public void Dispose()
        {
            ClearOrders(); // dispose any active goals
            NearByShips = null;
            FriendliesNearby?.Dispose(ref FriendliesNearby);
            PotentialTargets?.Dispose(ref PotentialTargets);
        }
    }
}