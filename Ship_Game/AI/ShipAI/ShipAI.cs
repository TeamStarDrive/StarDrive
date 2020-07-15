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
        SolarSystem SystemToPatrol;
        readonly Array<Planet> PatrolRoute = new Array<Planet>();
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

        bool TrySetupPatrolRoutes()
        {
            if (PatrolRoute.Count > 0)
                return true;

            foreach (Planet p in ExplorationTarget.PlanetList)
                PatrolRoute.Add(p);

            return PatrolRoute.Count > 0;
        }

        void ScrapShip(float elapsedTime, ShipGoal goal)
        {
            if (goal.TargetPlanet.Center.Distance(Owner.Center) >= goal.TargetPlanet.ObjectRadius * 3)
            {
                Orbit.Orbit(goal.TargetPlanet, elapsedTime);
                return;
            }

            if (goal.TargetPlanet.Center.Distance(Owner.Center) >= goal.TargetPlanet.ObjectRadius)
            {
                ThrustOrWarpToPos(goal.TargetPlanet.Center, elapsedTime, 200f);
                return;
            }
            ClearOrders(State);
            Owner.loyalty.RefundCreditsPostRemoval(Owner);
            goal.TargetPlanet.ProdHere += Owner.GetCost(Owner.loyalty) / 2f;
            Owner.loyalty.TryUnlockByScrap(Owner);
            Owner.QueueTotalRemoval();
            Owner.loyalty.GetEmpireAI().Recyclepool++;
        }

        public void Update(float elapsedTime)
        {
            if (State == AIState.AwaitingOrders && DefaultAIState == AIState.Exterminate )
                State = AIState.Exterminate;

            CheckTargetQueue();

            PrioritizePlayerCommands();
            if (HadPO && State != AIState.AwaitingOrders)
                HadPO = false;

            ResetStateFlee();
            ScanForThreat(elapsedTime);
            Owner.loyalty.data.Traits.ApplyTraitToShip(Owner);
            UpdateUtilityModuleAI(elapsedTime);
            ThrustTarget = Vector2.Zero;

            UpdateCombatStateAI(elapsedTime);

            if (UpdateOrderQueueAI(elapsedTime))
                return;

            AIStateRebase();
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
            Ship supplyShip = null;
            switch (resupplyReason)
            {
                case ResupplyReason.LowOrdnanceCombat:

                    supplyShip = NearBySupplyShip;
                    if (supplyShip != null)
                    {
                        SetUpSupplyEscort(supplyShip, supplyType: "Rearm");
                        return;
                    }

                    nearestRallyPoint = Owner.loyalty.RallyShipYardNearestTo(Owner.Center);
                    break;
                case ResupplyReason.LowOrdnanceNonCombat:
                    supplyShip = NearBySupplyShip;
                    if (supplyShip != null)
                    {
                        SetUpSupplyEscort(supplyShip, supplyType: "Rearm");
                        return;
                    }

                    nearestRallyPoint = Owner.loyalty.RallyShipYardNearestTo(Owner.Center);
                    break;
                case ResupplyReason.NoCommand:
                case ResupplyReason.LowHealth:
                    Ship repairShip = NearByRepairShip;
                    if (repairShip != null)
                        SetUpSupplyEscort(repairShip, supplyType: "Repair");
                    else
                        nearestRallyPoint = Owner.loyalty.RallyShipYardNearestTo(Owner.Center);
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

                    nearestRallyPoint = Owner.loyalty.RallyPoints.FindMax(p => p.TroopsHere.Count);
                    break;
                case ResupplyReason.NotNeeded:
                    TerminateResupplyIfDone();
                    return;
            }

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
                    Owner.fleet.FinalDirection, true, null, State);
        }

        void UpdateCombatStateAI(float elapsedTime)
        {
            TriggerDelay -= elapsedTime;
            FireOnMainTargetTime -= elapsedTime;
            if (TriggerDelay < 0)
            {
                TriggerDelay = elapsedTime * 2;
                FireOnTarget();
            }
            if (BadGuysNear && !IgnoreCombat && !HasPriorityOrder)
            {
                if (Owner.Weapons.Count > 0 || Owner.Carrier.HasActiveHangars || Owner.Carrier.HasTransporters)
                {
                    if (Target != null && !HasPriorityOrder && State != AIState.Resupply )
                    {
                        switch (OrderQueue.PeekFirst?.Plan)
                        {
                            case null:
                                OrderQueue.PushToFront(new ShipGoal(Plan.DoCombat, State));
                                break;
                            case Plan.DoCombat:
                            case Plan.Bombard:
                            case Plan.BoardShip:
                                break;
                            default:
                                OrderQueue.PushToFront(new ShipGoal(Plan.DoCombat, State));
                                break;
                        }
                    }
                }
            }
            else
            {
                int count = Owner.Weapons.Count;
                FireOnMainTargetTime = 0;
                Weapon[] items = Owner.Weapons.GetInternalArrayItems();
                for (int x = 0; x < count; x++)
                    items[x].ClearFireTarget();

                if (Owner.Carrier.HasHangars && Owner.loyalty != Empire.Universe.player)
                {
                    foreach (ShipModule hangar in Owner.Carrier.AllFighterHangars)
                    {
                        Ship hangarShip = hangar.GetHangarShip();
                        if (hangarShip != null && hangarShip.Active)
                            hangarShip.AI.OrderReturnToHangar();
                    }
                }
                else if (Owner.Carrier.HasHangars)
                {
                    foreach (ShipModule hangar in Owner.Carrier.AllFighterHangars)
                    {
                        Ship hangarShip = hangar.GetHangarShip();
                        if (hangarShip != null && hangarShip.AI.State != AIState.ReturnToHangar &&
                            !hangarShip.AI.HasPriorityTarget && !hangarShip.AI.HasPriorityOrder)
                        {
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
                CombatState = CombatState.Evade;
        }

        void AIStateRebase()
        {
            if (State != AIState.Rebase) return;
            if (OrderQueue.IsEmpty)
            {
                OrderRebaseToNearest();
                return;
            }
            for (int x = 0; x < OrderQueue.Count; x++)
            {
                ShipGoal goal = OrderQueue[x];
                if (goal.Plan != Plan.Rebase || goal.TargetPlanet == null || goal.TargetPlanet.Owner == Owner.loyalty)
                    continue;
                ClearOrders();
                return;
            }
        }

        bool UpdateOrderQueueAI(float elapsedTime)
        {
            if (OrderQueue.IsEmpty)
            {
                UpdateFromAIState(elapsedTime);
                return false;
            }
            return EvaluateNextOrderQueueItem(elapsedTime);
        }

        bool EvaluateNextOrderQueueItem(float elapsedTime)
        {
            ShipGoal goal = OrderQueue.PeekFirst;
            switch (goal.Plan)
            {
                case Plan.Stop:
                    if (ReverseThrustUntilStopped(elapsedTime)) { DequeueCurrentOrder(); }       break;
                case Plan.Bombard:                  return DoBombard(elapsedTime, goal);
                case Plan.Exterminate:              return DoExterminate(elapsedTime, goal);
                case Plan.Scrap:                    ScrapShip(elapsedTime, goal);                break;
                case Plan.RotateToFaceMovePosition: RotateToFaceMovePosition(elapsedTime, goal); break;
                case Plan.RotateToDesiredFacing:    RotateToDesiredFacing(elapsedTime, goal);    break;
                case Plan.MoveToWithin1000:         MoveToWithin1000(elapsedTime, goal);         break;
                case Plan.MakeFinalApproach:        MakeFinalApproach(elapsedTime, goal);        break;
                case Plan.RotateInlineWithVelocity: RotateInLineWithVelocity(elapsedTime);       break;
                case Plan.Orbit:                    Orbit.Orbit(goal.TargetPlanet, elapsedTime); break;
                case Plan.Colonize:                 Colonize(goal.TargetPlanet, goal);           break;
                case Plan.Explore:                  DoExplore(elapsedTime);                      break;
                case Plan.Rebase:                   DoLandTroop(elapsedTime, goal);              break;
                case Plan.DefendSystem:             DoSystemDefense(elapsedTime);                break;
                case Plan.DoCombat:                 DoCombat(elapsedTime);                       break;
                case Plan.DeployStructure:          DoDeploy(goal);                              break;
                case Plan.DeployOrbital:            DoDeployOrbital(goal);                       break;
                case Plan.PickupGoods:              PickupGoods.Execute(elapsedTime, goal);      break;
                case Plan.DropOffGoods:             DropOffGoods.Execute(elapsedTime, goal);     break;
                case Plan.ReturnToHangar:           DoReturnToHangar(elapsedTime);               break;
                case Plan.TroopToShip:              DoTroopToShip(elapsedTime, goal);            break;
                case Plan.BoardShip:                DoBoardShip(elapsedTime);                    break;
                case Plan.SupplyShip:               DoSupplyShip(elapsedTime);                   break;
                case Plan.Refit:                    DoRefit(goal);                               break;
                case Plan.LandTroop:                DoLandTroop(elapsedTime, goal);              break;
                case Plan.ResupplyEscort:           DoResupplyEscort(elapsedTime, goal);         break;
                case Plan.ReturnHome:               DoReturnHome(elapsedTime);                   break;
                case Plan.RebaseToShip:             DoRebaseToShip(elapsedTime);                 break;
                case Plan.HoldPosition:             HoldPosition();                              break;
                case Plan.HoldPositionOffensive:    HoldPositionOffensive();                     break;
                case Plan.Escort:                   AIStateEscort(elapsedTime);                  break;
            }

            return false;
        }

        void UpdateFromAIState(float elapsedTime)
        {
            if (Owner.fleet == null)
            {
                ClearWayPoints();
                switch (State)
                {
                    case AIState.DoNothing:      AwaitOrders(elapsedTime);           break;
                    case AIState.AwaitingOrders: AIStateAwaitingOrders(elapsedTime); break;
                    case AIState.Escort:         AIStateEscort(elapsedTime);         break;
                    case AIState.SystemDefender: AwaitOrders(elapsedTime); break;
                    case AIState.Resupply:       AwaitOrders(elapsedTime); break;
                    case AIState.ReturnToHangar: DoReturnToHangar(elapsedTime); break;
                    case AIState.AwaitingOffenseOrders: break;
                    case AIState.Exterminate:
                        OrderFindExterminationTarget(); break;
                    default:
                        if (Target != null)
                        {
                            Orbit.Orbit(Target, elapsedTime);
                        }
                        break;
                }
            }
            else
            {
                SetPriorityOrder(false);
                IdleFleetAI(elapsedTime);
            }
        }

        bool DoNearFleetOffset(float elapsedTime)
        {
            if (NearFleetPosition())
            {
                ReverseThrustUntilStopped(elapsedTime);
                RotateToDirection(Owner.fleet.FinalDirection, elapsedTime, 0.02f);
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

        void IdleFleetAI(float elapsedTime)
        {
            if (DoNearFleetOffset(elapsedTime))
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
                    SetPriorityOrder(true);
                    State = AIState.AwaitingOrders;
                    AddShipGoal(Plan.MakeFinalApproach,
                        Owner.fleet.GetFinalPos(Owner), Owner.fleet.FinalDirection, AIState.MoveTo);
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
                OrderMoveTo(Owner.fleet.GetFinalPos(Owner), Owner.fleet.FinalDirection, true, null, AIState.MoveTo);
            }
        }

        public bool HasTradeGoal(Goods goods)
        {
            return OrderQueue.Any(g => g.Trade?.Goods == goods);
        }

        public bool WaitForBlockadeRemoval(ShipGoal g, Planet planet, float elapsedTime)
        {
            if (planet.TradeBlocked && Owner.System != planet.ParentSystem)
            {
                g.Trade.BlockadeTimer -= elapsedTime;
                if (g.Trade.BlockadeTimer > 0f)
                {
                    ReverseThrustUntilStopped(elapsedTime);
                    return true;
                }

                // blockade is going on for too long, abort
                ClearOrders();
                State = AIState.AwaitingOrders;
                Planet fallback = Owner.loyalty.FindNearestRallyPoint(Owner.Center);
                if (fallback != planet)
                    AddOrbitPlanetGoal(fallback, AIState.AwaitingOrders);

                g.Trade.UnRegisterTrade(Owner);
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

        void UpdateUtilityModuleAI(float elapsedTime)
        {
            UtilityModuleCheckTimer -= elapsedTime;
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

        void ScanForThreat(float elapsedTime)
        {
            ScanForThreatTimer -= elapsedTime;
            if (ScanForThreatTimer <= 0f)
            {
                SetCombatStatus();
                ScanForThreatTimer = 2f;
            }
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

        void AIStateEscort(float elapsedTime)
        {
            Owner.AI.SetPriorityOrder(false);
            if (EscortTarget == null || !EscortTarget.Active)
            {
                EscortTarget = null;
                ClearOrders();
                if (Owner.Mothership != null && Owner.Mothership.Active)
                {
                    OrderReturnToHangar();
                    return;
                }

                State = AIState.AwaitingOrders; //fbedard
                if (Owner.loyalty.WeArePirates)
                    OrderPirateFleeHome();

                return;
            }
            if (Owner.GetStrength() <=0 
                || Owner.Mothership == null && EscortTarget.Center.InRadius(Owner.Center, Owner.SensorRange) 
                || Owner.Mothership == null 
                || !Owner.Mothership.AI.BadGuysNear 
                || EscortTarget != Owner.Mothership)
            {
                Orbit.Orbit(EscortTarget, elapsedTime);
                return;
            }
            // Doctor: This should make carrier-launched fighters scan for their own combat targets, except using the mothership's position
            // and a standard 30k around it instead of their own. This hopefully will prevent them flying off too much, as well as keeping them
            // in a carrier-based role while allowing them to pick appropriate target types depending on the fighter type.
            // gremlin Moved to setcombat status as target scan is expensive and did some of this already. this also shortcuts the UseSensorforTargets switch. Im not sure abuot the using the mothership target.
            // i thought i had added that in somewhere but i cant remember where. I think i made it so that in the scan it takes the motherships target list and adds it to its own.
            if(!Owner.InCombat )
            {
                Orbit.Orbit(EscortTarget, elapsedTime);
                return;
            }

            if (Owner.InCombat && Owner.Center.OutsideRadius(EscortTarget.Center, Owner.AI.CombatAI.PreferredEngagementDistance))
            {
                Owner.AI.SetPriorityOrder(true);
                Orbit.Orbit(EscortTarget, elapsedTime);
            }
        }

        void AIStateAwaitingOrders(float elapsedTime)
        {
            if (Owner.loyalty != Empire.Universe.player)
                AwaitOrders(elapsedTime);
            else
                AwaitOrdersPlayer(elapsedTime);
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