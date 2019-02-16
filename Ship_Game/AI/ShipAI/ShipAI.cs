using Microsoft.Xna.Framework;
using Ship_Game.Gameplay;
using Ship_Game.Ships;
using Ship_Game.Ships.AI;
using System;
using System.Linq;

namespace Ship_Game.AI
{
    public sealed partial class ShipAI : IDisposable
    {
        Vector2 AINewDir;
        Goal ColonizeGoal;
        Planet AwaitClosest;
        Planet PatrolTarget;
        Vector2 OrbitPos;
        float FindNewPosTimer;
        float DistanceLast;
        float UtilityModuleCheckTimer;
        SolarSystem SystemToPatrol;
        readonly Array<Planet> PatrolRoute = new Array<Planet>();
        int StopNumber;
        public FleetDataNode FleetNode { get;  set; }
        
        public Ship Owner;
        public AIState State = AIState.AwaitingOrders;
        public Guid OrbitTargetGuid;
        public Planet ColonizeTarget;
        public Planet ResupplyTarget;
        public Guid SystemToDefendGuid;
        public SolarSystem SystemToDefend;
        public SolarSystem ExplorationTarget;
        public AIState DefaultAIState = AIState.AwaitingOrders;
        public SafeQueue<ShipGoal> OrderQueue = new SafeQueue<ShipGoal>();
        public Array<ShipWeight> NearByShips  = new Array<ShipWeight>();
        public BatchRemovalCollection<Ship> FriendliesNearby = new BatchRemovalCollection<Ship>();


        public ShipAI(Ship owner)
        {
            Owner = owner;
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

        void Colonize(Planet targetPlanet)
        {
            if (Owner.Center.OutsideRadius(targetPlanet.Center, 2000f))
            {
                DequeueCurrentOrder();
                OrderColonization(targetPlanet);
                State = AIState.Colonize;
                return;
            }

            if (targetPlanet.Owner != null || !targetPlanet.Habitable)
            {
                ColonizeGoal?.NotifyMainGoalCompleted();
                ClearOrders();
                return;
            }

            ColonizeTarget = targetPlanet;
            ColonizeTarget.Owner = Owner.loyalty;
            ColonizeTarget.ParentSystem.OwnerList.Add(Owner.loyalty);
            if (Owner.loyalty.isPlayer)
            {
                ColonizeTarget.colonyType = Owner.loyalty.AutoColonize
                    ? Owner.loyalty.AssessColonyNeeds(ColonizeTarget)
                    : Planet.ColonyType.Colony;
                Empire.Universe.NotificationManager.AddColonizedNotification(ColonizeTarget, EmpireManager.Player);
            }
            else
            {
                ColonizeTarget.colonyType = Owner.loyalty.AssessColonyNeeds(ColonizeTarget);
            }

            Owner.loyalty.AddPlanet(ColonizeTarget);
            ColonizeTarget.InitializeWorkerDistribution(Owner.loyalty);
            ColonizeTarget.SetExploredBy(Owner.loyalty);

            Owner.CreateColonizationBuildingFor(ColonizeTarget);

            ColonizeTarget.AddMaxFertility(Owner.loyalty.data.EmpireFertilityBonus);
            ColonizeTarget.CrippledTurns = 0;
            StatTracker.StatAddColony(ColonizeTarget, Owner.loyalty, Empire.Universe);

            Owner.loyalty.GetEmpireAI().RemoveGoal(GoalType.Colonize, g => g.ColonizationTarget == ColonizeTarget);

            if (ColonizeTarget.ParentSystem.OwnerList.Count > 1)
            {
                foreach (Planet p in ColonizeTarget.ParentSystem.PlanetList)
                {
                    if (p.Owner == ColonizeTarget.Owner || p.Owner == null)
                        continue;
                    if (p.Owner.TryGetRelations(Owner.loyalty, out Relationship rel) && !rel.Treaty_OpenBorders)
                        p.Owner.DamageRelationship(Owner.loyalty, "Colonized Owned System", 20f, p);
                }
            }

            Owner.UnloadColonizationResourcesAt(ColonizeTarget);

            bool troopsRemoved = false;
            bool playerTroopsRemoved = false;

            foreach (Troop t in targetPlanet.TroopsHere)
            {
                Empire owner = t?.Loyalty;
                if (owner != null && !owner.isFaction && owner.data.DefaultTroopShip != null && owner != ColonizeTarget.Owner &&
                    ColonizeTarget.Owner.TryGetRelations(owner, out Relationship rel) && !rel.AtWar)
                {
                    t.Launch();
                    troopsRemoved = true;
                    playerTroopsRemoved |= t.Loyalty.isPlayer;
                }
            }

            Owner.QueueTotalRemoval();

            if (troopsRemoved)
                OnTroopsRemoved(playerTroopsRemoved);
        }

        void OnTroopsRemoved(bool playerTroopsRemoved)
        {
            if (playerTroopsRemoved)
            {
                Empire.Universe.NotificationManager.AddTroopsRemovedNotification(ColonizeTarget);
            }
            else if (ColonizeTarget.Owner.isPlayer)
            {
                Empire.Universe.NotificationManager.AddForeignTroopsRemovedNotification(ColonizeTarget);
            }
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

        public float TimeToTarget(Planet target)
        {
            if (target == null) return 0;
            float test = Math.Max(1,Vector2.Distance(target.Center, Owner.Center) / Owner.GetmaxFTLSpeed);
            return test;
        }

        bool InsideAreaOfOperation(Planet planet)
        {
            if (Owner.AreaOfOperation.Count == 0)
                return true;
            foreach (Rectangle ao in Owner.AreaOfOperation)
                if (ao.HitTest(planet.Center))
                    return true;
            return false;
        }

        void ScrapShip(float elapsedTime, ShipGoal goal)
        {
            if (goal.TargetPlanet.Center.Distance(Owner.Center) >= goal.TargetPlanet.ObjectRadius * 3)
            {
                DoOrbit(goal.TargetPlanet, elapsedTime);
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
            if (ClearOrdersNext)
            {
                ClearOrders();
                AwaitClosest = null;
            }
            CheckTargetQueue();

            PrioritizePlayerCommands();
            if (HadPO && State != AIState.AwaitingOrders)
                HadPO = false;

            if (State == AIState.Resupply)
            {
                HasPriorityOrder = true;
                if (Owner.Ordinance >= Owner.OrdinanceMax && Owner.Health >= Owner.HealthMax) //fbedard: consider health also
                {
                    HasPriorityOrder = false;
                    State = AIState.AwaitingOrders;
                }
            }

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
            UpdateResupplyAI();

            if (!Owner.isTurning)
                Owner.RestoreYBankRotation();
        }

        void UpdateResupplyAI()
        {
            if (Owner.Health < 0.1f)
                return;

            ResupplyReason resupplyReason = Owner.Supply.Resupply();
            if (resupplyReason != ResupplyReason.NotNeeded && Owner.Mothership?.Active == true)
            {
                OrderReturnToHangar(); // dealing with hangar ships needing resupply
                return;
            }

            if (!Owner.loyalty.isFaction)
                ProcessResupply(resupplyReason);
        }

        void ProcessResupply(ResupplyReason resupplyReason)
        {
            Planet nearestRallyPoint = null;
            switch (resupplyReason)
            {
                case ResupplyReason.LowOrdnanceCombat:
                    if (FriendliesNearby.Any(supply => supply.SupplyShipCanSupply))
                    {
                        Ship supplyShip = FriendliesNearby.FindMinFiltered(supply => supply.Carrier.HasSupplyBays,
                                                                           supply => -supply.Center.SqDist(Owner.Center));

                        SetUpSupplyEscort(supplyShip, supplyType: "Rearm");
                        return;
                    }
                    nearestRallyPoint = Owner.loyalty.RallyShipYardNearestTo(Owner.Center);
                    break;
                case ResupplyReason.LowOrdnanceNonCombat:
                    if (FriendliesNearby.Any(supply => supply.SupplyShipCanSupply))
                    {
                        State = AIState.ResupplyEscort; // FB: this will signal supply carriers to dispatch a supply shuttle
                        return;
                    }
                    nearestRallyPoint = Owner.loyalty.RallyShipYardNearestTo(Owner.Center);
                    break;
                case ResupplyReason.NoCommand:
                case ResupplyReason.LowHealth:
                    if (Owner.fleet != null && Owner.fleet.HasRepair)
                    {
                        Ship supplyShip = Owner.fleet.Ships.First(supply => supply.hasRepairBeam || supply.HasRepairModule);
                        SetUpSupplyEscort(supplyShip, supplyType: "Repair");
                        return;
                    }
                    nearestRallyPoint = Owner.loyalty.RallyShipYardNearestTo(Owner.Center);
                    break;
                case ResupplyReason.LowTroops:
                    nearestRallyPoint = Owner.loyalty.RallyShipYards.FindMax(p => p.TroopsHere.Count);
                    break;
                case ResupplyReason.NotNeeded:
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
                OrderResupply(nearestRallyPoint, false);
            else
            {
                nearestRallyPoint = Owner.loyalty.FindNearestRallyPoint(Owner.Center);
                if (nearestRallyPoint != null)
                    OrderResupply(nearestRallyPoint, false);
                else
                    OrderFlee(true);
            }
        }

        public void TerminateResupplyIfDone(SupplyType supplyType = SupplyType.All)
        {
            if (Owner.AI.State != AIState.Resupply && Owner.AI.State != AIState.ResupplyEscort)
                return;

            if (!Owner.Supply.DoneResupplying(supplyType))
                return;

            Owner.AI.HasPriorityOrder = false;
            DequeueCurrentOrder();
            Owner.AI.State = AIState.AwaitingOrders;
            Owner.AI.IgnoreCombat = false;
        }

        void UpdateCombatStateAI(float elapsedTime)
        {
            TriggerDelay -= elapsedTime;
            if (BadGuysNear && !IgnoreCombat)
            {
                using (OrderQueue.AcquireWriteLock())
                {
                    ShipGoal firstgoal = OrderQueue.PeekFirst;
                    if (Owner.Weapons.Count > 0 || Owner.Carrier.HasActiveHangars || Owner.Carrier.HasTransporters)
                    {
                        if (Target != null && !HasPriorityOrder && State != AIState.Resupply &&
                            (OrderQueue.IsEmpty ||
                             firstgoal != null && firstgoal.Plan != Plan.DoCombat && firstgoal.Plan != Plan.Bombard &&
                             firstgoal.Plan != Plan.BoardShip))
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
            }
            else
            {
                for (int x = 0; x < Owner.Weapons.Count; x++)
                    Owner.Weapons[x].ClearFireTarget();


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
                        if (hangarShip == null
                            || hangarShip.AI.State == AIState.ReturnToHangar
                            || hangarShip.AI.HasPriorityTarget
                            || hangarShip.AI.HasPriorityOrder)
                                continue;

                        if (Owner.FightersLaunched)
                            hangarShip.DoEscort(Owner);
                        else
                            hangarShip.AI.OrderReturnToHangar();
                    }
                }
            }
            if (Owner.shipData.ShipCategory == ShipData.Category.Civilian && BadGuysNear) //fbedard: civilian will evade
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
                        <= planet.GetGroundStrength(Owner.loyalty))
                        //This will tilt the scale just enough so that if there are 0 troops, a planet can still be bombed.
                    {
                        //As far as I can tell, if there were 0 troops on the planet, then GetGroundStrengthOther and GetGroundStrength would both return 0,
                        //meaning that the planet could not be bombed since that part of the if statement would always be true (0 * 1.5 <= 0)
                        //Adding +1 to the result of GetGroundStrengthOther tilts the scale just enough so a planet with no troops at all can still be bombed
                        //but having even 1 allied troop will cause the bombine action to abort.
                        ClearOrders();
                        AddOrbitPlanetGoal(toEvaluate.TargetPlanet); // Stay in Orbit
                    }
                    DoOrbit(toEvaluate.TargetPlanet, elapsedTime);
                    float radius = toEvaluate.TargetPlanet.ObjectRadius + Owner.Radius + 1500;
                    if (toEvaluate.TargetPlanet.Owner == Owner.loyalty)
                    {
                        ClearOrders();
                        return true;
                    }
                    DropBombsAtGoal(toEvaluate, radius);
                    break;
                case Plan.Exterminate:
                    DoOrbit(planet, elapsedTime);
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
                case Plan.Orbit:        DoOrbit(planet, elapsedTime); break;
                case Plan.Colonize:     Colonize(planet);             break;
                case Plan.Explore:      DoExplore(elapsedTime);       break;
                case Plan.Rebase:       DoRebase(toEvaluate);         break;
                case Plan.DefendSystem: DoSystemDefense(elapsedTime); break;
                case Plan.DoCombat:     DoCombat(elapsedTime);        break;
                case Plan.DeployStructure:   DoDeploy(toEvaluate);                      break;
                case Plan.PickupGoods:       DoPickupGoods(elapsedTime, toEvaluate);    break;
                case Plan.DropOffGoods:      DoDropOffGoods(elapsedTime, toEvaluate);   break;
                case Plan.ReturnToHangar:    DoReturnToHangar(elapsedTime);             break;
                case Plan.TroopToShip:       DoTroopToShip(elapsedTime, toEvaluate);    break;
                case Plan.BoardShip:         DoBoardShip(elapsedTime);                  break;
                case Plan.SupplyShip:        DoSupplyShip(elapsedTime, toEvaluate);     break;
                case Plan.Refit:             DoRefit(elapsedTime, toEvaluate);          break;
                case Plan.LandTroop:         DoLandTroop(elapsedTime, toEvaluate);      break;
                case Plan.ResupplyEscort:    DoResupplyEscort(elapsedTime, toEvaluate); break;
                case Plan.ReturnHome:        DoReturnHome(elapsedTime);                 break;
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
                            OrbitShip(Target as Ship, elapsedTime, Orbit.Right);
                        }
                        break;
                }
            }
            else
            {
                HasPriorityOrder = false;
                using (Owner.fleet.Ships.AcquireReadLock())
                    IdleFleetAI(elapsedTime);
            }
        }

        void IdleFleetAI(float elapsedTime)
        {
            bool nearFleetOffSet = Owner.Center.InRadius(Owner.fleet.Position + Owner.FleetOffset, 75);
            if (nearFleetOffSet)
            {
                ReverseThrustUntilStopped(elapsedTime);
                RotateToDirection(Owner.fleet.Direction, elapsedTime, 0.02f);
            }
            else 
            if (State == AIState.FormationWarp || State == AIState.Orbit || State == AIState.AwaitingOrders ||
                    (!HasPriorityOrder && !HadPO && State != AIState.HoldPosition))
            {
                if (Owner.fleet.Position.InRadius(Owner.Center, 7500))
                {
                    ThrustOrWarpToPosCorrected(Owner.fleet.Position + Owner.FleetOffset, elapsedTime);
                }
                else
                {
                    ClearWayPoints();
                    WayPoints.Enqueue(Owner.fleet.Position + Owner.FleetOffset);
                    State = AIState.AwaitingOrders;
                    if (Owner.fleet?.GoalStack.Count > 0)
                        WayPoints.Enqueue(Owner.fleet.GoalStack.Peek().MovePosition + Owner.FleetOffset);
                    else
                        OrderMoveTowardsPosition(Owner.fleet.Position + Owner.FleetOffset, DesiredDirection, true, null);
                }
            }
        }

        public bool WaitForBlockadeRemoval(ShipGoal g, Planet planet, float elapsedTime)
        {
            if (planet.TradeBlocked)
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

                g.Trade.UnregisterTrade(Owner);
                return true;
            }
            g.Trade.BlockadeTimer = 120f; // blockade was removed, continue as planned
            return false;
        }

        public void SetupFreighterPlan(Planet exportPlanet, Planet importPlanet, Goods goods)
        {
            ClearOrders();
            State = AIState.SystemTrader;

            // if ship has this cargo type on board, proceed to drop it off at destination
            Plan plan = Owner.GetCargo(goods) / Owner.CargoSpaceMax > 0.5f ? Plan.DropOffGoods : Plan.PickupGoods;
            AddTradePlan(plan, exportPlanet, importPlanet, goods, Owner);
        }

        public bool ClearOrderIfCombat() => ClearOrdersConditional(Plan.DoCombat);
        public bool ClearOrdersConditional(Plan plan)
        {
            bool clearOrders = false;
            foreach (ShipGoal order in OrderQueue)
            {
                if (order.Plan == plan)
                {
                    clearOrders = true;
                    break;
                }
            }
            if (clearOrders)
                ClearOrders();
            return clearOrders;
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
            if (!(ScanForThreatTimer < 0f)) return;
            SetCombatStatus(elapsedTime);
            ScanForThreatTimer = 2f;
        }

        void ResetStateFlee()
        {
            if (State != AIState.Flee || BadGuysNear || State == AIState.Resupply || HasPriorityOrder) return;
            if (OrderQueue.NotEmpty)
                OrderQueue.RemoveLast();
        }

        void PrioritizePlayerCommands()
        {
            if (Owner.loyalty == Empire.Universe.player &&
                (State == AIState.MoveTo && Vector2.Distance(Owner.Center, MovePosition) > 100f || State == AIState.Orbit ||
                 State == AIState.Bombard || State == AIState.AssaultPlanet || State == AIState.BombardTroops ||
                 State == AIState.Rebase || State == AIState.Scrap || State == AIState.Resupply || State == AIState.Refit ||
                 State == AIState.FormationWarp))
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
                OrbitShip(EscortTarget, elapsedTime, Orbit.Right);
                return;
            }
            // Doctor: This should make carrier-launched fighters scan for their own combat targets, except using the mothership's position
            // and a standard 30k around it instead of their own. This hopefully will prevent them flying off too much, as well as keeping them
            // in a carrier-based role while allowing them to pick appropriate target types depending on the fighter type.
            // gremlin Moved to setcombat status as target scan is expensive and did some of this already. this also shortcuts the UseSensorforTargets switch. Im not sure abuot the using the mothership target.
            // i thought i had added that in somewhere but i cant remember where. I think i made it so that in the scan it takes the motherships target list and adds it to its own.
            if(!Owner.InCombat )
            {
                OrbitShip(EscortTarget, elapsedTime, Orbit.Right);
                return;
            }

            if (Owner.InCombat && Owner.Center.OutsideRadius(EscortTarget.Center, Owner.AI.CombatAI.PreferredEngagementDistance))
            {
                Owner.AI.HasPriorityOrder = true;
                OrbitShip(EscortTarget, elapsedTime, Orbit.Right);
            }
        }

        void AIStateAwaitingOrders(float elapsedTime)
        {
            if (Owner.loyalty != Empire.Universe.player)
                AwaitOrders(elapsedTime);
            else
                AwaitOrdersPlayer(elapsedTime);
            if (Owner.loyalty.isFaction)
                return;
            if (Owner.OrdnanceStatus > ShipStatus.Average)
                return;
            if (FriendliesNearby.Any(supply => supply.Carrier.HasSupplyBays && supply.OrdnanceStatus > ShipStatus.Poor))
                return;
            var resupplyPlanet = Owner.loyalty.RallyShipYardNearestTo(Owner.Center);
            if (resupplyPlanet == null)
                return;
            OrderResupply(resupplyPlanet, true);
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        ~ShipAI() { Dispose(false); }

        void Dispose(bool disposing)
        {
            NearByShips = null;
            FriendliesNearby?.Dispose(ref FriendliesNearby);
            OrderQueue?.Dispose(ref OrderQueue);
            PotentialTargets?.Dispose(ref PotentialTargets);
        }
    }
}