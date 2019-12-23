using Microsoft.Xna.Framework;
using Ship_Game.AI.ShipMovement;
using Ship_Game.Gameplay;
using Ship_Game.Ships;
using Ship_Game.Utils;
using System;
using System.Linq;

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

            PotentialTargets.Clear();
            TrackProjectiles.Clear();
            OrderQueue.Clear();
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

            if (targetPlanet.Owner != null || !targetPlanet.Habitable)
            {
                shipGoal.Goal?.NotifyMainGoalCompleted();
                ClearOrders();
                return;
            }

            ColonizeTarget = targetPlanet;
            ColonizeTarget.Colonize(Owner);
            Owner.QueueTotalRemoval();
        }

        bool ExploreEmptySystem(float elapsedTime, SolarSystem system)
        {
            if (system.IsExploredBy(Owner.loyalty))
                return true;
            MovePosition = system.Position;
            if (Owner.Center.InRadius(MovePosition, 75000f))
            {
                system.SetExploredBy(Owner.loyalty);
                return true;
            }
            ThrustOrWarpToPosCorrected(MovePosition, elapsedTime);
            return false;
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
                ThrustOrWarpToPosCorrected(goal.TargetPlanet.Center, elapsedTime, 200f);
                return;
            }
            ClearOrders(State);
            goal.TargetPlanet.ProdHere += Owner.GetCost(Owner.loyalty) / 2f;
            Owner.QueueTotalRemoval();
            Owner.loyalty.GetEmpireAI().Recyclepool++;
        }

        public void Update(float elapsedTime)
        {
            if (State == AIState.AwaitingOrders && DefaultAIState == AIState.Exterminate)
                State = AIState.Exterminate;

            CheckTargetQueue();

            PrioritizePlayerCommands();
            if (HadPO && State != AIState.AwaitingOrders)
                HadPO = false;

            //if (State == AIState.Resupply)
            //{
            //    HasPriorityOrder = true;
            //    if (Owner.Supply.DoneResupplying(SupplyType.All))
            //    if (Owner.Ordinance >= Owner.OrdinanceMax && Owner.Health >= Owner.HealthMax) //fbedard: consider health also
            //    {
            //        HasPriorityOrder = false;
            //        State = AIState.AwaitingOrders;
            //    }
            //}

            ResetStateFlee();
            ScanForThreat(elapsedTime);
            Owner.loyalty.data.Traits.ApplyTraitToShip(Owner);
            UpdateUtilityModuleAI(elapsedTime);

            if (State == AIState.ManualControl)
                return;

            Owner.isThrusting = false;
            Owner.isTurning = false;
            ThrustTarget = Vector2.Zero;

            if (UpdateOrderQueueAI(elapsedTime))
                return;

            AIStateRebase();
            UpdateCombatStateAI(elapsedTime);
            if (!Owner.isTurning)
                Owner.RestoreYBankRotation();
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
            HasPriorityOrder = true;
            DecideWhereToResupply(nearestRallyPoint);
        }

        void SetUpSupplyEscort(Ship supplyShip, string supplyType = "All")
        {
            EscortTarget = supplyShip;
            IgnoreCombat = true;
            ClearOrders(AIState.ResupplyEscort);

            float strafeOffset = Owner.Radius + supplyShip.Radius + UniverseRandom.RandomBetween(200, 1000);
            AddShipGoal(Plan.ResupplyEscort, Vector2.Zero, UniverseRandom.RandomDirection(), null, supplyType, strafeOffset);
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

            Owner.AI.HasPriorityOrder = false;            
            Owner.AI.State            = AIState.AwaitingOrders;
            Owner.AI.IgnoreCombat     = false;

            DequeueCurrentOrder();
            if (Owner.fleet != null)
                OrderMoveTowardsPosition(Owner.fleet.Position + Owner.RelativeFleetOffset, Owner.fleet.Direction, true, null);
        }

        void UpdateCombatStateAI(float elapsedTime)
        {
            TriggerDelay -= elapsedTime;
            if (BadGuysNear && !IgnoreCombat)
            {
                if (Owner.Weapons.Count > 0 || Owner.Carrier.HasActiveHangars || Owner.Carrier.HasTransporters)
                {
                    ShipGoal goal = OrderQueue.PeekFirst;
                    if (Target != null && !HasPriorityOrder && State != AIState.Resupply &&
                        (goal == null || goal.Plan != Plan.DoCombat
                                      && goal.Plan != Plan.Bombard
                                      && goal.Plan != Plan.BoardShip))
                    {
                        OrderQueue.PushToFront(new ShipGoal(Plan.DoCombat));
                    }

                    if (TriggerDelay < 0)
                    {
                        TriggerDelay = elapsedTime * 2;
                        FireOnTarget();
                    }
                }
            }
            else
            {
                int count = Owner.Weapons.Count;
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
                            if (Owner.FightersLaunched)
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
            ShipGoal toEvaluate = OrderQueue.PeekFirst;
            Planet planet = toEvaluate.TargetPlanet;
            switch (toEvaluate.Plan)
            {
                case Plan.HoldPosition: HoldPosition(); break;
                case Plan.Stop:
                    if (ReverseThrustUntilStopped(elapsedTime)) { DequeueCurrentOrder(); }
                    break;
                case Plan.Scrap: ScrapShip(elapsedTime, toEvaluate); break;
                case Plan.Bombard: //Modified by Gretman
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
                        //but having even 1 allied troop will cause the bombine action to abort.
                        ClearOrders();
                        AddOrbitPlanetGoal(toEvaluate.TargetPlanet); // Stay in Orbit
                    }
                    Orbit.Orbit(toEvaluate.TargetPlanet, elapsedTime);
                    float radius = toEvaluate.TargetPlanet.ObjectRadius + Owner.Radius + 1500;
                    if (toEvaluate.TargetPlanet.Owner == Owner.loyalty)
                    {
                        ClearOrders();
                        return true;
                    }
                    DropBombsAtGoal(toEvaluate, radius);
                    break;
                case Plan.Exterminate:
                    Orbit.Orbit(planet, elapsedTime);
                    radius = planet.ObjectRadius + Owner.Radius + 1500;
                    if (planet.Owner == Owner.loyalty || planet.Owner == null)
                    {
                        ClearOrders();
                        OrderFindExterminationTarget();
                        return true;
                    }
                    DropBombsAtGoal(toEvaluate, radius);
                    break;
                case Plan.RotateToFaceMovePosition: RotateToFaceMovePosition(elapsedTime, toEvaluate); break;
                case Plan.RotateToDesiredFacing:    RotateToDesiredFacing(elapsedTime, toEvaluate);    break;
                case Plan.MoveToWithin1000:         MoveToWithin1000(elapsedTime, toEvaluate);         break;
                case Plan.MakeFinalApproach:        MakeFinalApproach(elapsedTime, toEvaluate);        break;
                case Plan.RotateInlineWithVelocity: RotateInLineWithVelocity(elapsedTime);             break;
                case Plan.Orbit:                    Orbit.Orbit(planet, elapsedTime);                break;
                case Plan.Colonize:                 Colonize(planet, toEvaluate);                      break;
                case Plan.Explore:                  DoExplore(elapsedTime);                            break;
                case Plan.Rebase:                   DoRebase(toEvaluate);                              break;
                case Plan.DefendSystem:             DoSystemDefense(elapsedTime);                      break;
                case Plan.DoCombat:                 DoCombat(elapsedTime);                             break;
                case Plan.DeployStructure:          DoDeploy(toEvaluate);                              break;
                case Plan.DeployOrbital:            DoDeployOrbital(toEvaluate);                       break;
                case Plan.PickupGoods:              PickupGoods.Execute(elapsedTime, toEvaluate);      break;
                case Plan.DropOffGoods:             DropOffGoods.Execute(elapsedTime, toEvaluate);     break;
                case Plan.ReturnToHangar:           DoReturnToHangar(elapsedTime);                     break;
                case Plan.TroopToShip:              DoTroopToShip(elapsedTime, toEvaluate);            break;
                case Plan.BoardShip:                DoBoardShip(elapsedTime);                          break;
                case Plan.SupplyShip:               DoSupplyShip(elapsedTime);                         break;
                case Plan.Refit:                    DoRefit(toEvaluate);                               break;
                case Plan.LandTroop:                DoLandTroop(elapsedTime, toEvaluate);              break;
                case Plan.ResupplyEscort:           DoResupplyEscort(elapsedTime, toEvaluate);         break;
                case Plan.ReturnHome:               DoReturnHome(elapsedTime);                         break;
                case Plan.RebaseToShip:             DoRebaseToShip(elapsedTime);                       break;
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
                HasPriorityOrder = false;
                IdleFleetAI(elapsedTime);
            }
        }

        bool DoNearFleetOffset(float elapsedTime)
        {
            if (Owner.Center.InRadius(Owner.fleet.Position + Owner.FleetOffset, 75))
            {
                ReverseThrustUntilStopped(elapsedTime);
                RotateToDirection(Owner.fleet.Direction, elapsedTime, 0.02f);
                return true;
            }
            return false;
        }

        bool ShouldReturnToFleet()
        {
            //separated for clarity as this section can be very confusing.
            //we might need a toggle for the player action here.
            if (State == AIState.FormationWarp)
                return true;
            if (HasPriorityOrder || HadPO)
                return false;
            if (BadGuysNear)
                return false;
            if (State == AIState.Orbit || State == AIState.AwaitingOffenseOrders || State == AIState.AwaitingOrders)
                return true;
            return false;
        }

        void IdleFleetAI(float elapsedTime)
        {
            if (DoNearFleetOffset(elapsedTime))
                return;

            if (ShouldReturnToFleet())
            {
                //check if inside minimum warp jump range. If not do a full warp process.
                if (Owner.fleet.Position.InRadius(Owner.Center, 7500))
                    ThrustOrWarpToPosCorrected(Owner.fleet.Position + Owner.FleetOffset, elapsedTime);
                else
                    WarpToFleet();
            }
        }

        void WarpToFleet()
        {
            ClearWayPoints();
            WayPoints.Enqueue(Owner.fleet.Position + Owner.FleetOffset);
            State = AIState.AwaitingOrders;
            if (Owner.fleet?.GoalStack.Count > 0)
                WayPoints.Enqueue(Owner.fleet.GoalStack.Peek().MovePosition + Owner.FleetOffset);
            else
                OrderMoveTowardsPosition(Owner.fleet.Position + Owner.FleetOffset, Owner.fleet.Direction, true, null);
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
                    for (int x = 0; x < Owner.Carrier.AllTransporters.Length; x++) // FB:change to foreach
                    {
                        ShipModule module = Owner.Carrier.AllTransporters[x];
                        if (module.TransporterTimer > 0f || !module.Active || !module.Powered ||
                            module.TransporterPower >= Owner.PowerCurrent) continue;
                        if (FriendliesNearby.Count > 0 && module.TransporterOrdnance > 0 && Owner.Ordinance > 0)
                            DoOrdinanceTransporterLogic(module);
                        if (module.TransporterTroopAssault > 0 && Owner.TroopList.Any())
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
            EscortTarget = s;
            ClearOrders(State, priority: true);
            AddShipGoal(Plan.BoardShip);
        }

        public void OrderTroopToShip(Ship s)
        {
            EscortTarget = s;
            ClearOrders(State);
            AddShipGoal(Plan.TroopToShip);
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
                (State == AIState.MoveTo && Vector2.Distance(Owner.Center, MovePosition) > 100f 
                || State == AIState.Bombard 
                || State == AIState.AssaultPlanet 
                || State == AIState.BombardTroops 
                || State == AIState.Rebase 
                || State == AIState.Scrap 
                || State == AIState.Resupply 
                || State == AIState.Refit 
                || State == AIState.FormationWarp))
            {
                HasPriorityOrder = true;
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
            Owner.AI.HasPriorityOrder = false;
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
                return;
            }
            if (Owner.GetStrength() <=0 ||
                Owner.Mothership == null &&
                EscortTarget.Center.InRadius(Owner.Center, Owner.SensorRange) ||
                Owner.Mothership == null || !Owner.Mothership.AI.BadGuysNear ||
                EscortTarget != Owner.Mothership)
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
                Owner.AI.HasPriorityOrder = true;
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