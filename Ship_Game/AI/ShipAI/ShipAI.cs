using Microsoft.Xna.Framework;
using Ship_Game.AI.ShipMovement;
using Ship_Game.Gameplay;
using Ship_Game.Ships;
using Ship_Game.Utils;
using System;
using System.Linq;
using System.Runtime.CompilerServices;
using Ship_Game.Ships.AI;

namespace Ship_Game.AI
{
    public sealed partial class ShipAI : IDisposable
    {
        Planet AwaitClosest;
        Planet PatrolTarget;
        float UtilityModuleCheckTimer;
        public FleetDataNode FleetNode;

        public Ship Owner;
        public AIState State = AIState.AwaitingOrders;
        public Planet ResupplyTarget;
        public SolarSystem SystemToDefend;
        public SolarSystem ExplorationTarget;
        public AIState DefaultAIState = AIState.AwaitingOrders;
        public SafeQueue<ShipGoal> OrderQueue  = new SafeQueue<ShipGoal>();
        public Array<ShipWeight>   NearByShips = new Array<ShipWeight>();

        // TODO: We should not keep these around, it increases memory usage by a lot
        DropOffGoods DropOffGoods;
        PickupGoods PickupGoods;
        OrbitPlan Orbit;

        public bool IsDisposed => Owner == null;

        [Flags]
        public enum Flags
        {
            None = 0,
            HasPriorityTarget = (1 << 0),
            HasPriorityOrder  = (1 << 1),
            HadPO             = (1 << 2),
            IgnoreCombat      = (1 << 3),
            IsNonCombatant    = (1 << 4),
            TargetProjectiles = (1 << 5),
            Intercepting      = (1 << 6),
            FiringAtMainTarget= (1 << 7),
            BadShipsNear      = (1 << 8),
            BadPlanetsNear    = (1 << 9),
            ReturnToHangarSoon = (1 << 10),
            BadShipsOrPlanetsNear = BadShipsNear | BadPlanetsNear,
        }

        public Flags StateBits;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        void Bit(Flags tag, bool value) => StateBits = value ? StateBits|tag : StateBits & ~tag;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        bool Bit(Flags tag) => (StateBits & tag) != 0;
        
        public bool HasPriorityTarget
        { [MethodImpl(MethodImplOptions.AggressiveInlining)] get => Bit(Flags.HasPriorityTarget);
          [MethodImpl(MethodImplOptions.AggressiveInlining)] set => Bit(Flags.HasPriorityTarget, value); }
        public bool HasPriorityOrder
        { [MethodImpl(MethodImplOptions.AggressiveInlining)] get => Bit(Flags.HasPriorityOrder);
          [MethodImpl(MethodImplOptions.AggressiveInlining)] private set => Bit(Flags.HasPriorityOrder, value); }
        public bool HadPO
        { [MethodImpl(MethodImplOptions.AggressiveInlining)] get => Bit(Flags.HadPO);
          [MethodImpl(MethodImplOptions.AggressiveInlining)] private set => Bit(Flags.HadPO, value); }
        public bool BadGuysNear
        { [MethodImpl(MethodImplOptions.AggressiveInlining)] get => Bit(Flags.BadShipsNear);
          [MethodImpl(MethodImplOptions.AggressiveInlining)] set => Bit(Flags.BadShipsNear, value); }
        public bool IgnoreCombat
        { [MethodImpl(MethodImplOptions.AggressiveInlining)] get => Bit(Flags.IgnoreCombat);
          [MethodImpl(MethodImplOptions.AggressiveInlining)] set => Bit(Flags.IgnoreCombat, value); }
        public bool Intercepting
        { [MethodImpl(MethodImplOptions.AggressiveInlining)] get => Bit(Flags.Intercepting);
          [MethodImpl(MethodImplOptions.AggressiveInlining)] set => Bit(Flags.Intercepting, value); }
        public bool TargetProjectiles
        { [MethodImpl(MethodImplOptions.AggressiveInlining)] get => Bit(Flags.TargetProjectiles);
          [MethodImpl(MethodImplOptions.AggressiveInlining)] set => Bit(Flags.TargetProjectiles, value); }
        public bool IsNonCombatant
        { [MethodImpl(MethodImplOptions.AggressiveInlining)] get => Bit(Flags.IsNonCombatant);
          [MethodImpl(MethodImplOptions.AggressiveInlining)] set => Bit(Flags.IsNonCombatant, value); }
        public bool IsFiringAtMainTarget
        { [MethodImpl(MethodImplOptions.AggressiveInlining)] get => Bit(Flags.FiringAtMainTarget); 
          [MethodImpl(MethodImplOptions.AggressiveInlining)] private set => Bit(Flags.FiringAtMainTarget, value); }
        bool ReturnToHangarSoon
        { [MethodImpl(MethodImplOptions.AggressiveInlining)] get => Bit(Flags.ReturnToHangarSoon); 
          [MethodImpl(MethodImplOptions.AggressiveInlining)] set => Bit(Flags.ReturnToHangarSoon, value); }

        public ShipAI(Ship owner)
        {
            Owner = owner;
            DropOffGoods = new DropOffGoods(this);
            PickupGoods = new PickupGoods(this);
            Orbit = new OrbitPlan(this);
            CombatAI = new CombatAI(this);
            InitializeTargeting();
        }

        void DisposeOrders()
        {
            var orders = OrderQueue.TakeAll();
            for (int i = 0; i < orders.Length; i++)
            {
                ShipGoal g = orders[i];
                g?.Dispose();
            }
        }

        public void Dispose()
        {
            if (IsDisposed)
                return;

            // Absolutely necessary to dispose all of these manually
            // because C# is not always able to break bugged cyclic references
            Owner = null;
            DisposeOrders(); // dispose any active goals
            
            StateBits = Flags.None;
            AwaitClosest = null;
            PatrolTarget = null;
            FleetNode = null;
            ResupplyTarget = null;
            SystemToDefend = null;
            ExplorationTarget = null;

            OrderQueue?.Dispose(ref OrderQueue);
            NearByShips?.Clear();
            NearByShips = null;
            FriendliesNearby = Empty<Ship>.Array;
            PotentialTargets = Empty<Ship>.Array;
            TrackProjectiles = Empty<Projectile>.Array;

            DropOffGoods?.Dispose(ref DropOffGoods);
            PickupGoods?.Dispose(ref PickupGoods);
            Orbit?.Dispose(ref Orbit);

            CombatAI = null;
            EscortTarget = null;
            ExterminationTarget = null;
            Target = null;
            TargetQueue?.Clear();
            TargetQueue = null;

            ScannedTargets.Clear();
            ScannedTargets = null;
            ScannedFriendlies.Clear();
            ScannedFriendlies = null;
            ScannedProjectiles.Clear();
            ScannedProjectiles = null;

            OrbitTarget = null;
            WayPoints?.Clear();
            WayPoints = null;
        }

        // Resets all important state of the AI
        public void Reset()
        {
            if (IsDisposed)
                return;

            Target = null;
            ResupplyTarget = null;
            EscortTarget = null;
            SystemToDefend = null;
            ExplorationTarget = null;

            PotentialTargets = Empty<Ship>.Array;
            FriendliesNearby = Empty<Ship>.Array;
            TrackProjectiles = Empty<Projectile>.Array;
            NearByShips.Clear();
            ClearOrders();
        }

        public Planet ColonizeTarget => FindGoal(Plan.Colonize, out ShipGoal g)
                                      ? g.TargetPlanet : null;

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
                    ?? ResupplyTarget?.Center
                    ?? ColonizeTarget?.Center
                    ?? Vector2.Zero;
            }
        }

        /// <summary>
        /// Check if a ship has an exploration order
        ///
        /// A ship can have Evade/Escape order during exploration, hack workaround is to iterate orders queue
        /// </summary>
        public bool IsExploring => State == AIState.Explore
                                || ExplorationTarget != null
                                || OrderQueue.Any(g => g.Plan == Plan.Explore);

        void Colonize(ShipGoal shipGoal)
        {
            Planet targetPlanet = shipGoal.TargetPlanet;
            if (Owner.Position.OutsideRadius(targetPlanet.Center, 2000f))
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

            targetPlanet.Colonize(Owner);
            Owner.QueueTotalRemoval();
        }

        bool TryGetClosestUnexploredPlanet(SolarSystem system, out Planet planet)
        {
            planet = PatrolTarget;
            if (PatrolTarget != null && !PatrolTarget.IsExploredBy(Owner.loyalty))
                return true;

            if (system.IsFullyExploredBy(Owner.loyalty))
                return false;

            planet = system.PlanetList.FindMinFiltered(p => !p.IsExploredBy(Owner.loyalty), p => Owner.Position.SqDist(p.Center));
            return planet != null;
        }

        void DoScrapShip(FixedSimTime timeStep, ShipGoal goal)
        {
            if (goal.TargetPlanet.Center.Distance(Owner.Position) >= goal.TargetPlanet.ObjectRadius * 3)
            {
                Orbit.Orbit(goal.TargetPlanet, timeStep);
                return;
            }

            if (goal.TargetPlanet.Center.Distance(Owner.Position) >= goal.TargetPlanet.ObjectRadius)
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

            Owner.loyalty.data.Traits.ApplyTraitToShip(Owner);

            UpdateUtilityModuleAI(timeStep);
            ThrustTarget = Vector2.Zero;

            // deferred return to hangar
            if (ReturnToHangarSoon)
            {
                ReturnToHangarSoon = false;
                OrderReturnToHangar();
            }

            UpdateCombatStateAI(timeStep);

            if (UpdateOrderQueueAI(timeStep))
                return;

            AIStateRebase();
        }

        public Ship NearBySupplyShip => FriendliesNearby.FindMinFiltered(
            supply => supply.Carrier.HasSupplyBays && supply.SupplyShipCanSupply,
            supply => supply.Position.SqDist(Owner.Position));

        public Ship NearByRepairShip => FriendliesNearby.FindMinFiltered(
            supply => supply.hasRepairBeam || supply.HasRepairModule,
            supply => supply.Position.SqDist(Owner.Position));

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

                    nearestRallyPoint = Owner.loyalty.FindNearestSafeRallyPoint(Owner.Position);
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
                        nearestRallyPoint = Owner.loyalty.FindNearestSafeRallyPoint(Owner.Position);
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
                nearestRallyPoint = Owner.loyalty.FindNearestRallyPoint(Owner.Position);

                if      (nearestRallyPoint != null)   OrderResupply(nearestRallyPoint, cancelOrders);
                else if (Owner.loyalty.WeArePirates)  OrderPirateFleeHome();
                else if (Owner.loyalty.WeAreRemnants) OrderRemnantFlee();
                else                                  OrderFlee();
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

            Planet planet = possiblePlanets.FindMin(p => p.Center.SqDist(Owner.Position));
            ai.AddPlanetaryRearmGoal(Owner, planet);
        }

        public bool FireWeapons(FixedSimTime timeStep)
        {
            TriggerDelay -= timeStep.FixedTime;
            if (TriggerDelay <= 0f)
            {
                TriggerDelay = timeStep.FixedTime * 2;
                return FireOnTarget();
            }
            return false;
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
                case Plan.Colonize:                 Colonize(goal);                           break;
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

        void AbortLandNoFleet(Planet planet)
        {
            if (Owner.fleet == null) // AI fleets cancel this in their eval process
            {
                OrderRebaseToNearest();
                if (Owner.loyalty.isPlayer)
                    Empire.Universe.NotificationManager.AddAbortLandNotification(planet, Owner);
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

        bool NearFleetPosition() => Owner.Position.InRadius(Owner.fleet.GetFinalPos(Owner), 75f);

        bool ShouldReturnToFleet()
        {
            if (Owner.Position.InRadius(Owner.fleet.GetFormationPos(Owner), 400))
                return false;
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
                if (Owner.fleet.FinalPosition.InRadius(Owner.Position, 7500))
                {
                    SetPriorityOrder(true);  // FB this might cause serious issues that make orbiting ships stuck with PO and not available anymore for the AI.
                    State = AIState.AwaitingOrders;
                    AddShipGoal(Plan.MakeFinalApproach,
                        Owner.fleet.GetFormationPos(Owner), Owner.fleet.FinalDirection, AIState.MoveTo);

                    AddShipGoal(Plan.RotateToDesiredFacing,
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
                Planet fallback = Owner.loyalty.FindNearestRallyPoint(Owner.Position);
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
                        if (FriendliesNearby.Length > 0 && module.TransporterOrdnance > 0 && Owner.Ordinance > 0)
                            DoOrdinanceTransporterLogic(module);
                        if (module.TransporterTroopAssault > 0 && Owner.HasOurTroops)
                            DoAssaultTransporterLogic(module);
                    }

                //Do repair check if friendly ships around
                if (FriendliesNearby.Length == 0)
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
            if (Owner.SecondsAlive > 1 && Owner.System == null)
                Owner.Die(null, true);

            // constant velocity, no acceleration
            // Ship Sim should take care of the rest
            Owner.Velocity = g.Direction * g.SpeedLimit;
            Owner.MaxSTLSpeed = g.SpeedLimit;

            if (Owner.Position.InRadius(g.TargetPlanet.Center, g.TargetPlanet.GravityWellRadius * 0.5f))
            {
                Owner.PlanetCrash = new PlanetCrash(g.TargetPlanet, Owner, g.SpeedLimit*0.85f);
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
                if (!target.Active)
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
                || !Owner.IsHangarShip && escortTarget.Position.InRadius(Owner.Position, Owner.SensorRange)
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

            if (Owner.InCombat && Owner.Position.OutsideRadius(escortTarget.Position, Owner.DesiredCombatRange))
            {
                Owner.AI.SetPriorityOrder(true);
                Orbit.Orbit(escortTarget, timeStep);
            }
        }

        void AIStateAwaitingOrders(FixedSimTime timeStep)
        {
            if (!Owner.loyalty.isPlayer)
                AwaitOrders(timeStep);
            else
                AwaitOrdersPlayer(timeStep);
        }

        // For Unit tests
        public Planet TestGetPatrolTarget() => PatrolTarget;
    }
}