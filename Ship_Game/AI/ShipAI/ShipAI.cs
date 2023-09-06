using Ship_Game.AI.ShipMovement;
using Ship_Game.Gameplay;
using Ship_Game.Ships;
using Ship_Game.Utils;
using System;
using System.Diagnostics.Contracts;
using System.Runtime.CompilerServices;
using SDUtils;
using Ship_Game.Data.Serialization;
using Ship_Game.Fleets;
using Vector2 = SDGraphics.Vector2;
using Ship_Game.Spatial;
#pragma warning disable CA2213

namespace Ship_Game.AI
{
    [StarDataType]
    public sealed partial class ShipAI : IDisposable
    {
        Planet AwaitClosest;
        Planet PatrolTarget;
        float UtilityModuleCheckTimer;
        [StarData] public FleetDataNode FleetNode;
        [StarData] public Ship Owner;
        [Pure] public RandomBase Random => Owner.Loyalty.Random;
        [StarData] public AIState State = AIState.AwaitingOrders;
        [StarData] public Planet ResupplyTarget;
        [StarData] public SolarSystem SystemToDefend; // FB - check if this is needed, since we are not using systemdefender for now
        [StarData] public SolarSystem ExplorationTarget;
        [StarData] public AIState DefaultAIState = AIState.AwaitingOrders;

        public SafeQueue<ShipGoal> OrderQueue  = new();
        public Array<ShipWeight>   NearByShips = new();

        [StarData] ShipGoal[] GoalsSave
        {
            get => OrderQueue.ToArray();
            set => OrderQueue.SetRange(value);
        }

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

        [StarData] public Flags StateBits;

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

        [StarDataConstructor]
        ShipAI() {}

        public ShipAI(Ship owner)
        {
            Owner = owner;
            CreateShipAIPlans();
            InitializeTargeting();
        }

        [StarDataDeserialized]
        void OnDeserialized()
        {
            CreateShipAIPlans();
        }

        void CreateShipAIPlans()
        {
            // TODO: THESE SHOULD BE REMOVED BECAUSE THEY EAT UP A LOT OF MEMORY
            if (Owner.IsFreighter)
            {
                DropOffGoods = new DropOffGoods(this);
                PickupGoods = new PickupGoods(this);
            }

            Orbit = new OrbitPlan(this);
            CombatAI = new CombatAI(this);
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

            Mem.Dispose(ref OrderQueue);
            NearByShips?.Clear();
            NearByShips = null;
            FriendliesNearby = Empty<Ship>.Array;
            PotentialTargets = Empty<Ship>.Array;
            TrackProjectiles = Empty<Projectile>.Array;

            Mem.Dispose(ref DropOffGoods);
            Mem.Dispose(ref PickupGoods);
            Mem.Dispose(ref Orbit);
            Mem.Dispose(ref CombatAI);
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
            WayPoints?.Dispose();
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
                    Vector2 pos = goal.TargetPlanet?.Position ?? goal.MovePosition;
                    if (pos.NotZero())
                        return pos;
                }
                return Target?.Position
                    ?? ExplorationTarget?.Position
                    ?? SystemToDefend?.Position
                    ?? ResupplyTarget?.Position
                    ?? ColonizeTarget?.Position
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


        void DoStandByColonize(FixedSimTime timeStep)
        {
            ReverseThrustUntilStopped(timeStep);
        }

        void DoColonize(ShipGoal shipGoal)
        {
            Planet targetPlanet = shipGoal.TargetPlanet;
            if (targetPlanet == null) // wtf? this happened when loading a savegame
            {
                Log.Error("Colonize: targetPlanet was null");
                DequeueCurrentOrder();
                return;
            }

            if (Owner.Position.OutsideRadius(targetPlanet.Position, 2000f))
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
            if (PatrolTarget != null && !PatrolTarget.IsExploredBy(Owner.Loyalty))
                return true;

            if (system.IsFullyExploredBy(Owner.Loyalty))
                return false;

            planet = system.PlanetList.FindMinFiltered(p => !p.IsExploredBy(Owner.Loyalty), p => Owner.Position.SqDist(p.Position));
            return planet != null;
        }

        void DoScrapShip(FixedSimTime timeStep, ShipGoal goal)
        {
            if (goal.TargetPlanet.Position.Distance(Owner.Position) >= goal.TargetPlanet.Radius * 3)
            {
                Orbit.Orbit(goal.TargetPlanet, timeStep);
                return;
            }

            if (goal.TargetPlanet.Position.Distance(Owner.Position) >= goal.TargetPlanet.Radius)
            {
                ThrustOrWarpToPos(goal.TargetPlanet.Position, timeStep, 200f);
                return;
            }

            // Waiting to be scrapped by Empire goal
            if (!Owner.Loyalty.AI.HasGoal(g => g.Type == GoalType.ScrapShip && g.OldShip == Owner))
                ClearOrders(); // Could not find empire scrap goal
        }

        public void Update(FixedSimTime timeStep)
        {
            if (State == AIState.AwaitingOrders && DefaultAIState == AIState.Exterminate)
                ChangeAIState(AIState.Exterminate);

            CheckTargetQueue();
            PrioritizePlayerCommands();
            if (HadPO && State != AIState.AwaitingOrders)
                HadPO = false;

            ResetStateFlee();

            Owner.Loyalty.data.Traits.ApplyTraitToShip(Owner);

            UpdateUtilityModuleAI(timeStep);
            ThrustTarget = Vector2.Zero;

            // deferred return to hangar
            if (ReturnToHangarSoon)
            {
                ReturnToHangarSoon = false;
                OrderReturnToHangar();
            }

            UpdateCombatStateAI(timeStep);

            if (timeStep.FixedTime > 0f
                && GlobalStats.EnableShipFlocking
                && FriendliesNearby.Length != 0 
                && Owner.InCombat
                && !Orbit.InOrbit)
            {
                KeepDistanceUsingFlocking(timeStep);
            }

            // Evaluate ShipGoals
            if (OrderQueue.TryPeekFirst(out ShipGoal first))
            {
                EvaluateShipGoal(timeStep, first);
            }
            else
            {
                // LEGACY: there is no default goal right now
                //         so this tries to do "something" based on AIState
                // TODO: figure out a way to only use ShipGoal system which is less error prone
                UpdateFromAIState(timeStep);
            }
        }

        public Ship NearBySupplyShip => FriendliesNearby.FindMinFiltered(
            supply => supply.Carrier.HasSupplyBays && supply.SupplyShipCanSupply,
            supply => supply.Position.SqDist(Owner.Position));

        public Ship NearByRepairShip => FriendliesNearby.FindMinFiltered(
            supply => supply.HasRepairBeam || supply.HasRepairModule,
            supply => supply.Position.SqDist(Owner.Position));

        public void ProcessResupply(ResupplyReason resupplyReason)
        {
            Planet nearestRallyPoint = null;
            switch (resupplyReason)
            {
                case ResupplyReason.LowOrdnanceCombat:
                case ResupplyReason.LowOrdnanceNonCombat:
                    Ship supplyShip = NearBySupplyShip;
                    if (supplyShip != null && State != AIState.ResupplyEscort)
                    {
                        SetUpSupplyEscort(supplyShip, supplyType: "Rearm");
                        return;
                    }

                    nearestRallyPoint = Owner.Loyalty.FindNearestSafeRallyPoint(Owner.Position);
                    break;
                case ResupplyReason.RequestResupplyForOrbital:
                    RequestResupplyFromPlanetForOrbital();
                    return;
                case ResupplyReason.NoCommand:
                case ResupplyReason.LowHealth:
                    Ship repairShip = NearByRepairShip;
                    if (repairShip != null && State != AIState.ResupplyEscort)
                        SetUpSupplyEscort(repairShip, supplyType: "Repair");
                    else
                        nearestRallyPoint = Owner.Loyalty.FindNearestSafeRallyPoint(Owner.Position);
                    break;
                case ResupplyReason.LowTroops:
                    if (Owner.Carrier.SendTroopsToShip)
                    {
                        for (int i = 0; i < Owner.Carrier.MissingTroops - Owner.NumTroopsRebasingHere; ++i)
                        {
                            if (Owner.Loyalty.GetTroopShipForRebase(out Ship troopShip, Owner))
                                troopShip.AI.OrderRebaseToShip(Owner);
                        }

                        return;
                    }

                    nearestRallyPoint = Owner.Loyalty.SafeSpacePorts.FindMax(p => p.Troops.Count);
                    break;
                case ResupplyReason.NotNeeded:
                    TerminateResupplyIfDone(SupplyType.All, terminateIfEnemiesNear: false);
                    return;
            }

            if (Owner.IsPlatformOrStation)
                return;

            SetPriorityOrder(true);
            if (State != AIState.Resupply && State != AIState.ResupplyEscort)
            {
                DecideWhereToResupply(nearestRallyPoint);
            }
        }

        void SetUpSupplyEscort(Ship supplyShip, string supplyType = "All")
        {
            IgnoreCombat = true;
            ChangeAIState(AIState.ResupplyEscort);
            EscortTarget = supplyShip;

            float strafeOffset = Owner.Radius + supplyShip.Radius + Random.Float(200, 1000);
            AddShipGoal(Plan.ResupplyEscort, Vector2.Zero, Random.Direction2D(), null, 
                        supplyType, strafeOffset, AIState.ResupplyEscort, pushToFront: true);
        }

        void DecideWhereToResupply(Planet nearestRallyPoint, bool cancelOrders = false)
        {
            if (nearestRallyPoint != null)
                OrderResupply(nearestRallyPoint, cancelOrders);
            else
            {
                nearestRallyPoint = Owner.Loyalty.FindNearestRallyPoint(Owner.Position);

                if      (nearestRallyPoint != null)   OrderResupply(nearestRallyPoint, cancelOrders);
                else if (Owner.Loyalty.WeArePirates)  OrderPirateFleeHome();
                else if (Owner.Loyalty.WeAreRemnants) OrderRemnantFlee(Owner.Loyalty.Remnants);
                else                                  OrderFlee();
            }
        }

        public void TerminateResupplyIfDone(SupplyType supplyType, bool terminateIfEnemiesNear)
        {
            if (Owner.AI.State != AIState.Resupply && Owner.AI.State != AIState.ResupplyEscort)
                return;

            if (Owner.Supply.DoneResupplying(supplyType) || terminateIfEnemiesNear && Owner.AI.BadGuysNear)
            {
                DequeueCurrentOrder();
                ExitCombatState();
                Owner.AI.SetPriorityOrder(false);
                Owner.AI.IgnoreCombat = false;
                if (Owner.Fleet != null)
                    OrderMoveTo(Owner.Fleet.GetFinalPos(Owner), Owner.Fleet.FinalDirection, State);
                else if (ShouldNotReturnToLastPos())
                    ClearOrders();

                Owner.Supply.ResetIncomingOrdnance(supplyType);

                bool ShouldNotReturnToLastPos()
                {
                    if (OrderQueue.TryPeekFirst(out ShipGoal nextGoal) && nextGoal.MovePosition != Vector2.Zero)
                    {
                        Vector2 movePos = nextGoal.MovePosition;
                        return Owner.Universe.Influence.GetInfluenceStatus(Owner.Loyalty, MovePosition) == Universe.InfluenceStatus.Enemy
                                || Owner.Loyalty.AI.ThreatMatrix.GetHostileStrengthAt(movePos, Owner.SensorRange * 2) > Owner.GetStrength();
                    }

                    return false;
                }
            }
        }

        void RequestResupplyFromPlanetForOrbital()
        {
            if (Owner.GetTether()?.Owner == Owner.Loyalty)
                return;

            EmpireAI ai = Owner.Loyalty.AI;
            if (ai.HasGoal(g => g.Type == GoalType.RearmShipFromPlanet && g.TargetShip == Owner))
                return; // Supply ship is on the way

            var possiblePlanets = Owner.Loyalty.GetPlanets().Filter(p => p.NumSupplyShuttlesCanLaunch() > 0);
            if (possiblePlanets.Length == 0)
                return;

            Planet planet = possiblePlanets.FindMin(p => p.Position.SqDist(Owner.Position));
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

        void EvaluateShipGoal(FixedSimTime timeStep, ShipGoal goal)
        {
            switch (goal.Plan)
            {
                case Plan.AwaitOrders:              DoAwaitOrders(timeStep, goal);            break;
                case Plan.AwaitOrdersAIManaged:     AwaitOrdersAIControlled(timeStep);        break;
                case Plan.Stop:                     DoStop(timeStep, goal);                   break;
                case Plan.Bombard:                  DoBombard(timeStep, goal);                break;
                case Plan.FindExterminationTarget:  DoFindExterminationTarget(timeStep, goal);break;
                case Plan.Exterminate:              DoExterminate(timeStep, goal);            break;
                case Plan.Scrap:                    DoScrapShip(timeStep, goal);              break;
                case Plan.RotateToFaceMovePosition: RotateToFaceMovePosition(timeStep, goal); break;
                case Plan.RotateToDesiredFacing:    RotateToDesiredFacing(timeStep, goal);    break;
                case Plan.MoveToWithin1000:         MoveToWithin1000(timeStep, goal);         break;
                case Plan.MakeFinalApproach:        MakeFinalApproach(timeStep, goal);        break;
                case Plan.RotateInlineWithVelocity: RotateInLineWithVelocity(timeStep);       break;
                case Plan.Orbit:                    Orbit.Orbit(goal.TargetPlanet, timeStep); break;
                case Plan.Colonize:                 DoColonize(goal);                         break;
                case Plan.StandByColonize:          DoStandByColonize(timeStep);              break;
                case Plan.Explore:                  DoExplore(timeStep);                      break;
                case Plan.Rebase:                   DoRebase(timeStep, goal);                 break;
                case Plan.DefendSystem:             DoSystemDefense(timeStep);                break;
                case Plan.DoCombat:                 DoCombat(timeStep);                       break;
                case Plan.DeployStructure:          DoDeploy(goal, timeStep);                 break;
                case Plan.DeployOrbital:            DoDeployOrbital(goal, timeStep);          break;
                case Plan.PickupGoods:              PickupGoods.Execute(timeStep, goal);      break;
                case Plan.DropOffGoods:             DropOffGoods.Execute(timeStep, goal);     break;
                case Plan.PickupGoodsForStation:    DoPickupGoodsForStation(timeStep, goal);  break;
                case Plan.DropOffGoodsForStation:   DoDropOffGoodsForStation(timeStep, goal); break;
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
                case Plan.HoldPosition:             DoHoldPositionPlan(goal);                 break;
                case Plan.Escort:                   AIStateEscort(timeStep);                  break;
                case Plan.Meteor:                   DoMeteor(goal);                           break;
            }
        }

        void UpdateFromAIState(FixedSimTime timeStep)
        {
            SetPriorityOrder(false);

            Fleet fleet = Owner.Fleet;
            if (fleet == null)
            {
                ClearWayPoints();
                Plan correspondingPlan = GetMatchingShipGoalPlan(State);
                AddShipGoal(correspondingPlan, State);
            }
            else
            {
                IdleFleetAI(timeStep, fleet);
            }
        }

        // figure out which ShipGoal Plan corresponds to AIState
        static Plan GetMatchingShipGoalPlan(AIState state)
        {
            switch (state)
            {
                default:
                case AIState.AwaitingOrders: return Plan.AwaitOrders;
                case AIState.SystemDefender:
                case AIState.Resupply:       return Plan.AwaitOrdersAIManaged; // @see Ship.UpdateResupply()
                case AIState.Escort:         return Plan.Escort;
                case AIState.ReturnToHangar: return Plan.ReturnToHangar;
                case AIState.Exterminate:    return Plan.FindExterminationTarget;
            }
        }

        void AbortLandNoFleet(Planet planet)
        {
            if (Owner.Fleet == null) // AI fleets cancel this in their eval process
            {
                OrderRebaseToNearest();
                if (Owner.Loyalty.isPlayer)
                    Owner.Universe.Notifications.AddAbortLandNotification(planet, Owner);
            }
        }

        bool DoNearFleetOffset(FixedSimTime timeStep, Fleet fleet)
        {
            if (Owner.Position.InRadius(fleet.GetFinalPos(Owner), 75f))
            {
                ReverseThrustUntilStopped(timeStep);
                RotateToDirection(fleet.FinalDirection, timeStep, 0.02f);
                return true;
            }
            return false;
        }

        bool ShouldReturnToFleet(Fleet fleet)
        {
            if (Owner.Position.InRadius(fleet.GetFormationPos(Owner), 400))
                return false;
            // separated for clarity as this section can be very confusing.
            // we might need a toggle for the player action here.
            if (State == AIState.FormationMoveTo && HasPriorityOrder || HadPO)
                return true;
            if (HasPriorityOrder || HadPO)
                return false;
            if (BadGuysNear)
                return false;
            if (!Owner.CanTakeFleetMoveOrders())
                return false;
            if (State == AIState.Orbit ||
                State == AIState.AwaitingOrders)
                return true;

            return false;
        }

        void IdleFleetAI(FixedSimTime timeStep, Fleet fleet)
        {
            if (DoNearFleetOffset(timeStep, fleet))
            {
                if (State != AIState.HoldPosition && Owner.CanTakeFleetMoveOrders())
                    ChangeAIState(AIState.AwaitingOrders);
                return;
            }

            if (ShouldReturnToFleet(fleet))
            {
                OrderMoveTo(fleet.GetFinalPos(Owner), fleet.FinalDirection, AIState.MoveTo);
            }
            else
            {
                if (State != AIState.HoldPosition && Owner.CanTakeFleetMoveOrders())
                    OrderAwaitOrders(false);
            }
        }

        public bool HasTradeGoal(Goods goods)
        {
            return OrderQueue.Any(g => g.Trade?.Goods == goods);
        }

        public bool WaitForBlockadeRemoval(ShipGoal g, Planet planet, FixedSimTime timeStep)
        {
            if (planet.TradeBlocked && Owner.System != planet.System)
            {
                g.Trade.BlockadeTimer -= timeStep.FixedTime;
                if (g.Trade.BlockadeTimer > 0f && !planet.Quarantine)
                {
                    ReverseThrustUntilStopped(timeStep);
                    return true;
                }

                // blockade is going on for too long or manual quarantine, abort
                ClearOrders();
                ChangeAIState(AIState.AwaitingOrders);
                Planet fallback = Owner.Loyalty.FindNearestRallyPoint(Owner.Position);
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
                ChangeAIState(AIState.AwaitingOrders);
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
                var repairBeams = Owner.RepairBeams;
                if (repairBeams != null)
                {
                    for (int i = 0; i < repairBeams.Count; i++)
                    {
                        ShipModule m = repairBeams[i];
                        if (m.InstalledWeapon.CooldownTimer <= 0f &&
                            m.InstalledWeapon.Module.Powered &&
                            Owner.Ordinance >= m.InstalledWeapon.OrdinanceRequiredToFire &&
                            Owner.PowerCurrent >= m.InstalledWeapon.PowerRequiredToFire)
                        {
                            DoRepairBeamLogic(m.InstalledWeapon);
                        }
                    }
                }

                if (!Owner.HasRepairModule)
                    return;

                var weapons = Owner.Weapons;
                for (int i = 0; i < weapons.Count; i++)
                {
                    Weapon w = weapons[i];
                    if (w.CooldownTimer > 0f || !w.Module.Powered ||
                        Owner.Ordinance < w.OrdinanceRequiredToFire ||
                        Owner.PowerCurrent < w.PowerRequiredToFire || !w.IsRepairDrone)
                    {
                        //Gretman -- Added this so repair drones would cooldown outside combat (+15s)
                        if (w.CooldownTimer > 0f)
                            w.CooldownTimer = Math.Max(w.CooldownTimer - 1, 0f);
                        continue;
                    }

                    DoRepairDroneLogic(w);
                }
            }
        }

        public void UpdateRebase()
        {
            if (State == AIState.Rebase && !Owner.Loyalty.isPlayer)
            {
                // if our Rebase troop ships order is targeting a conquered planet, cancel orders
                if (FindGoal(Plan.Rebase, out ShipGoal rebase) && rebase.TargetPlanet.Owner != Owner.Loyalty)
                    OrderRebaseToNearest();
            }
        }

        void DoMeteor(ShipGoal g)
        {
            if (Owner.SecondsAlive > 1 && Owner.System == null)
                Owner.Die(null, true);

            // constant velocity, no acceleration
            // Ship Sim should take care of the rest
            Owner.Velocity = g.Direction * g.SpeedLimit;
            Owner.MaxSTLSpeed = g.SpeedLimit;

            if (Owner.Position.InRadius(g.TargetPlanet.Position, g.TargetPlanet.GravityWellRadius * 0.5f))
            {
                Owner.PlanetCrash = new PlanetCrash(g.TargetPlanet, Owner);
                Owner.Dying       = true;
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
            if (Owner.Loyalty == Owner.Universe.Player &&
                (State is AIState.Bombard or AIState.AssaultPlanet 
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
                
                ChangeAIState(AIState.AwaitingOrders);
                if (Owner.Loyalty.WeArePirates)
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
            if (!Owner.InCombat)
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


        // This is done for player projectors to visualize empires in projection radius
        public void ProjectorScan(float sensorRadius, float time)
        {
            Ship projector = Owner;
            Empire us = projector.Loyalty;
            var findEnemies = new SearchOptions(projector.Position, sensorRadius, GameObjectType.Ship)
            {
                MaxResults = 32,
                Exclude = projector,
                ExcludeLoyalty = us,
            };

            SpatialObjectBase[] enemies = projector.Universe.Spatial.FindNearby(ref findEnemies);
            for (int i = 0; i < enemies.Length; ++i)
            {
                var enemy = (Ship)enemies[i];
                if (!enemy.Active || enemy.Dying || enemy.BaseStrength == 0 || enemy.IsFreighter)
                    continue;

                projector.PlayerProjectorHasSeenEmpires.SetSeen(enemy.Loyalty, time);
            }
        }

        // For Unit tests
        public Planet TestGetPatrolTarget() => PatrolTarget;
    }
}