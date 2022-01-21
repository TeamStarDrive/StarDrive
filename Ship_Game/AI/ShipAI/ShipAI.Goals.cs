using Microsoft.Xna.Framework;
using Ship_Game.Fleets;
using Ship_Game.Ships;
using System;
using System.Collections.Generic;
using Ship_Game.Ships.AI;

namespace Ship_Game.AI
{
    public sealed partial class ShipAI
    {
        void DequeueCurrentOrder(MoveOrder order)
        {
            if (order.IsSet(MoveOrder.DequeueWayPoint) && WayPoints.Count > 0)
                WayPoints.Dequeue();
            DequeueCurrentOrder();
        }

        void DequeueCurrentOrder()
        {
            if (OrderQueue.TryDequeue(out ShipGoal goal))
            {
                goal.Dispose();
                ShipGoal nextGoal = OrderQueue.PeekFirst;
                ChangeAIState(nextGoal?.WantedState ?? DefaultAIState);
            }
        }

        void DequeueOrdersUntilWayPointDequeued()
        {
            while (OrderQueue.TryDequeue(out ShipGoal goal))
            {
                goal.Dispose();
                ShipGoal nextGoal = OrderQueue.PeekFirst;
                ChangeAIState(nextGoal?.WantedState ?? DefaultAIState);
                if (goal.MoveOrder.IsSet(MoveOrder.DequeueWayPoint) && WayPoints.Count > 0)
                {
                    WayPoints.Dequeue();
                    break;
                }
            }
        }

        public void ClearOrders(AIState newState = AIState.AwaitingOrders, bool priority = false)
        {
            DisposeOrders();
            EscortTarget = null;
            PatrolTarget = null;
            OrbitTarget = null;
            SystemToDefend = null;
            IgnoreCombat = false;
            ExitCombatState();
            ChangeAIState(newState); // Must come after ExitCombatState since ExitCombatState change the AIstate to awaiting orders.
            if (ExplorationTarget != null)
            {
                Owner.Loyalty.GetEmpireAI().ExpansionAI.RemoveExplorationTargetFromList(ExplorationTarget);
                ExplorationTarget = null;
            }

            OrderQueue.Clear();
            SetPriorityOrder(priority);
        }

        public void ChangeAIState(AIState newState)
        {
            switch (State)
            {
                case AIState.Ferrying:
                    if (Owner.ShipData.Role == RoleName.supply)
                        EscortTarget?.Supply.ChangeIncomingSupply(SupplyType.Rearm, -Owner.Ordinance);
                    break;
            }
            State = newState;
        }

        public void SetPriorityOrder(bool priority)
        {
            HasPriorityOrder = priority;
        }

        public void ClearOrdersAndWayPoints(AIState newState = AIState.AwaitingOrders, bool priority = false)
        {
            ClearWayPoints();
            ClearOrders(newState, priority);
        }

        public void ClearPriorityOrderAndTarget()
        {
            SetPriorityOrder(false);
            Intercepting      = false;
            HasPriorityTarget = false;
        }

        void ResetPriorityOrderWithClear()
        {
            ResetPriorityOrder(true);
            ClearWayPoints();
        }

        // This will Reset The priority order along with Intercepting and Priority Target
        public void ResetPriorityOrder(bool clearOrders)
        {
            if (clearOrders)
                ClearOrders(State, true);

            SetPriorityOrder(true);
            Intercepting      = false;
            HasPriorityTarget = false;
        }

        public bool FindGoal(Plan plan, out ShipGoal goal)
        {
            foreach (ShipGoal g in OrderQueue)
            {
                if (g?.Plan == plan)
                {
                    goal = g;
                    return true;
                }
            }
            goal = null;
            return false;
        }

        public void AddGoalFromSave(SavedGame.ShipGoalSave sg, UniverseData data)
        {
            var goal = new ShipGoal(sg, data, Owner);
            EnqueueGoal(goal);
        }

        void AddShipGoal(Plan plan, AIState wantedState, bool pushToFront = false)
        {
            EnqueueOrPush(new ShipGoal(plan, wantedState), pushToFront);
        }

        void AddShipGoal(Plan plan, Vector2 pos, Vector2 dir, AIState wantedState)
        {
            EnqueueOrPush(new ShipGoal(plan, pos, dir, null, null, 0f, "", 0f, wantedState, null));
        }

        void AddShipGoal(Plan plan, Vector2 pos, Vector2 dir, Goal theGoal,
                         string variableString, float variableNumber, AIState wantedState, bool pushToFront = false)
        {
            var goal = new ShipGoal(plan, pos, dir, null, theGoal, 0f, variableString, variableNumber, wantedState, null);
            EnqueueOrPush(goal, pushToFront);
        }

        void AddShipGoal(Plan plan, Ship targetShip, AIState wantedState)
        {
            EscortTarget = targetShip;
            var goal = new ShipGoal(plan, targetShip.Position, Vectors.Up, null, null, 0f, "", 0f, wantedState, targetShip);
            EnqueueOrPush(goal);
        }

        void AddShipGoal(Plan plan, Vector2 pos, Vector2 dir, Planet targetPlanet, Goal theGoal, AIState wantedState)
        {
            EnqueueOrPush(new ShipGoal(plan, pos, dir, targetPlanet, theGoal, 0f, "", 0f, wantedState, null));
        }

        internal void SetTradePlan(Plan plan, Planet exportPlanet, Planet importPlanet, Goods goodsType, float blockadeTimer = 120f)
        {
            ClearOrders(AIState.SystemTrader);
            var goal = new ShipGoal(plan, exportPlanet, importPlanet, goodsType, Owner, blockadeTimer, AIState.SystemTrader);
            EnqueueOrPush(goal);
        }

        bool AddShipGoal(Plan plan, Planet target, Goal theGoal, AIState wantedState, bool pushToFront = false)
        {
            if (target == null)
            {
                Log.Error($"AddShipGoal {plan}: planet was null! Goal discarded.");
                return false;
            }

            var goal = new ShipGoal(plan, target.Center, Vectors.Up, target, theGoal, 0f, "", 0f, wantedState, null);
            EnqueueOrPush(goal, pushToFront);
            return true;
        }

        void AddMoveOrder(Plan plan, WayPoint wayPoint, AIState state, float speedLimit, MoveOrder order, Goal goal = null)
        {
            EnqueueOrPush(new ShipGoal(plan, wayPoint.Position, wayPoint.Direction, state, order, speedLimit, goal));
        }

        void EnqueueOrPush(ShipGoal goal, bool pushToFront = false)
        {
            if (pushToFront)
                PushGoalToFront(goal);
            else
                EnqueueGoal(goal);

            ChangeAIState(goal.WantedState);
        }

        void EnqueueGoal(ShipGoal goal)
        {
            OrderQueue.Enqueue(goal);
        }

        void PushGoalToFront(ShipGoal goal)
        {
            OrderQueue.PushToFront(goal);
        }

        void AddPlanetGoal(Plan plan, Planet planet, AIState newState, bool priority = false, bool pushToFront = false)
        {
            if (AddShipGoal(plan, planet, null, newState, pushToFront))
            {
                OrbitTarget = planet;
                if (priority)
                    ResetPriorityOrder(false);
            }
        }

        public void AddMeteorGoal(Planet p, float rotation, Vector2 direction, float speed)
        {
            Owner.Rotation = rotation;
            PushGoalToFront(new ShipGoal(Plan.Meteor, p.Center, direction, p,
                                         null, speed, "", 0f, AIState.MoveTo, null));
        }

        void AddLandTroopGoal(Planet p)      => AddPlanetGoal(Plan.LandTroop, p, AIState.AssaultPlanet);
        void AddExterminateGoal(Planet p)    => AddPlanetGoal(Plan.Exterminate, p, AIState.Exterminate);
        void AddResupplyPlanetGoal(Planet p) => AddPlanetGoal(Plan.Orbit, p, AIState.Resupply, pushToFront: true);

        void AddOrbitPlanetGoal(Planet p, AIState newState = AIState.Orbit) => AddPlanetGoal(Plan.Orbit, p, newState);

        public void OrderMoveAndColonize(Planet planet, Goal g)
        {
            OrderMoveTo(GetPositionOnPlanet(planet), Vectors.Up, AIState.Colonize);
            AddShipGoal(Plan.Colonize, planet.Center, Vectors.Up, planet, g, AIState.Colonize);
        }

        public void OrderMoveAndRebase(Planet p)
        {
            Vector2 direction = Owner.Position.DirectionToTarget(p.Center);
            OrderMoveToNoStop(GetPositionOnPlanet(p), direction, AIState.Rebase, MoveOrder.AddWayPoint);
            AddPlanetGoal(Plan.Rebase, p, AIState.Rebase, priority: true);
        }

        public void OrderSupplyShipLand(Planet p)
        {
            Vector2 direction = Owner.Position.DirectionToTarget(p.Center);
            OrderMoveToNoStop(GetPositionOnPlanet(p), direction, AIState.SupplyReturnHome, MoveOrder.AddWayPoint);
            IgnoreCombat = true;
            EscortTarget = null;
            SetPriorityOrder(true);
        }

        public void OrderMoveAndRefit(Planet planet, Goal g)
        {
            if (!Owner.IsPlatformOrStation)
            {
                OrderMoveTo(GetPositionOnPlanet(planet), Vectors.Up, AIState.Refit);
                IgnoreCombat = true;
                ResetPriorityOrder(clearOrders: false);
            }

            // refitting ships must not be in any pools or fleets
            Owner.RemoveFromPoolAndFleet(clearOrders: false);

            AddShipGoal(Plan.Refit, planet, g, AIState.Refit);
        }

        public void OrderMoveAndScrap(Planet p)
        {
            Vector2 direction = Owner.Position.DirectionToTarget(p.Center);
            OrbitTarget = p;
            OrderMoveTo(GetPositionOnPlanet(p), direction, AIState.Scrap);
            AddPlanetGoal(Plan.Scrap, p, AIState.Scrap);
        }

        public void OderMoveAndDefendSystem(Planet p)
        {
            OrderMoveTo(GetPositionOnPlanet(p), Vectors.Up, AIState.SystemDefender);
            AddShipGoal(Plan.DefendSystem, AIState.SystemDefender);
        }

        public void AddEscortGoal(Ship targetShip, bool clearOrders = true)
        {
            if (clearOrders)
                ClearOrders();

            AddShipGoal(Plan.Escort, targetShip, AIState.Escort);
        }

        Vector2 GetPositionOnPlanet(Planet p)
        {
            return MathExt.RandomOffsetAndDistance(p.Center, p.ObjectRadius);
        }

        public class ShipGoal : IDisposable
        {
            bool IsDisposed;
            // ship goal variables are read-only by design, do not allow writes!
            public readonly Plan Plan;

            Vector2 StaticMovePosition;

            public Vector2 MovePosition
            {
                get
                {
                    if (Goal != null)
                        return Goal.MovePosition;

                    // for Orbit plans we don't use Planet.Center
                    // TODO: There is a mismatch here after save load
                    if (TargetPlanet != null && Plan != Plan.Orbit)
                        return TargetPlanet.Center;

                    return StaticMovePosition;
                }
                set => StaticMovePosition = value;
            }

            public readonly Vector2 Direction; // direction param for this goal, can have multiple meanings
            public readonly Planet TargetPlanet;
            public readonly Ship TargetShip;
            public readonly Goal Goal; // Empire AI Goal
            public readonly Fleet Fleet;
            public readonly float SpeedLimit;
            public readonly string VariableString;
            public readonly float VariableNumber;
            public readonly AIState WantedState; 
            public TradePlan Trade;
            public readonly MoveOrder MoveOrder;

            /// If this is a Move Order, is it an Aggressive move?
            public bool HasAggressiveMoveOrder => (MoveOrder & MoveOrder.Aggressive) != 0;

            /// If this a Move Order, is it just a plain old Regular move? (default)
            public bool HasRegularMoveOrder => (MoveOrder & MoveOrder.Regular) != 0;

            public float GetSpeedLimitFor(Ship ship) => ship.Fleet?.GetSpeedLimitFor(ship) ?? SpeedLimit;

            public override string ToString() => $"{Plan} {MoveOrder} pos:{MovePosition} dir:{Direction}";

            public ShipGoal(Plan plan, AIState wantedState)
            {
                Plan        = plan;
                WantedState = wantedState;
            }

            public ShipGoal(Plan plan, Vector2 pos, Vector2 dir, Planet targetPlanet, Goal theGoal,
                            float speedLimit, string variableString, float variableNumber, AIState wantedState, Ship targetShip)
            {
                Plan           = plan;
                MovePosition   = pos;
                Direction      = dir;
                TargetPlanet   = targetPlanet;
                Goal           = theGoal;
                SpeedLimit     = speedLimit;
                VariableString = variableString;
                VariableNumber = variableNumber;
                WantedState    = wantedState;
                TargetShip     = targetShip;
            }

            public ShipGoal(Plan plan, Planet exportPlanet, Planet importPlanet, Goods goods, 
                            Ship freighter, float blockadeTimer, AIState wantedState)
            {
                Plan        = plan;
                Trade       = new TradePlan(exportPlanet, importPlanet, goods, freighter, blockadeTimer);
                WantedState = wantedState;
            }

            public ShipGoal(Plan plan, Vector2 waypoint, Vector2 direction, AIState state, 
                            MoveOrder order, float speedLimit, Goal goal)
            {
                Plan         = plan;
                MovePosition = waypoint;
                Direction    = direction;
                WantedState  = state;
                Goal         = goal;
                MoveOrder    = order;
                SpeedLimit   = speedLimit;
            }

            // restore from SaveGame
            public ShipGoal(SavedGame.ShipGoalSave sg, UniverseData data, Ship ship)
            {
                Plan         = sg.Plan;
                MovePosition = sg.MovePosition;
                Direction    = sg.Direction;
                WantedState  = sg.WantedState;

                TargetPlanet = data.FindPlanetOrNull(sg.TargetPlanetGuid);
                TargetShip   = data.FindShipOrNull(sg.TargetShipGuid);

                VariableString = sg.VariableString;
                VariableNumber = sg.VariableNumber;
                SpeedLimit = sg.SpeedLimit;
                MoveOrder  = sg.MoveOrder;

                Empire loyalty = ship.Loyalty;

                if (sg.fleetGuid != Guid.Empty)
                {
                    foreach (KeyValuePair<int, Fleet> empireFleet in loyalty.GetFleetsDict())
                        if (empireFleet.Value.Guid == sg.fleetGuid)
                            Fleet = empireFleet.Value;
                }

                if (sg.GoalGuid != Guid.Empty)
                {
                    Array<Goal> goals = loyalty.GetEmpireAI().Goals;
                    foreach (Goal empireGoal in goals)
                    {
                        if (sg.GoalGuid == empireGoal.guid)
                        {
                            Goal = empireGoal;
                            break;
                        }
                    }
                    if (Goal == null)
                        Log.Warning($"ShipGoalSave {sg.Plan}: failed to find Empire.Goal {sg.GoalGuid}");
                }

                if (sg.Trade != null)
                    Trade = new TradePlan(sg.Trade, data, ship);

                if (Plan == Plan.SupplyShip)
                    ship.AI.EscortTarget?.Supply.ChangeIncomingSupply(SupplyType.Rearm, ship.Ordinance);
            }

            // Convert this ShipGoal into a ShipGoalSave
            public SavedGame.ShipGoalSave ToSaveData()
            {
                var s = new SavedGame.ShipGoalSave
                {
                    Plan             = Plan,
                    Direction        = Direction,
                    VariableString   = VariableString,
                    SpeedLimit       = SpeedLimit,
                    MovePosition     = MovePosition,
                    fleetGuid        = Fleet?.Guid ?? Guid.Empty,
                    GoalGuid         = Goal?.guid ?? Guid.Empty,
                    TargetPlanetGuid = TargetPlanet?.Guid ?? Guid.Empty,
                    TargetShipGuid   = TargetShip?.Guid ?? Guid.Empty,
                    MoveOrder        = MoveOrder,
                    VariableNumber   = VariableNumber,
                    WantedState      = WantedState
                };

                if (Trade != null)
                {
                    s.Trade = new SavedGame.TradePlanSave
                    {
                        Goods         = Trade.Goods,
                        ExportFrom    = Trade.ExportFrom?.Guid ?? Guid.Empty,
                        ImportTo      = Trade.ImportTo?.Guid ?? Guid.Empty,
                        BlockadeTimer = Trade.BlockadeTimer,
                        StardateAdded = Trade.StardateAdded
                    };
                }

                return s;
            }

            ~ShipGoal() { Destroy(); } // finalizer
            public void Dispose()
            {
                Destroy();
                GC.SuppressFinalize(this);
            }

            void Destroy()
            {
                if (IsDisposed) return;
                IsDisposed = true;
                Trade?.UnRegisterTrade(Trade.Freighter);
            }
        }

        public class TradePlan
        {
            public readonly Goods Goods;
            public readonly Planet ExportFrom;
            public readonly Planet ImportTo;
            public readonly Ship Freighter;
            public readonly float StardateAdded;
            public float BlockadeTimer; // Indicates how much time to wait with freight when trade is blocked

            public TradePlan(Planet exportPlanet, Planet importPlanet, Goods goodsType, Ship freighter, float blockadeTimer)
            {
                ExportFrom    = exportPlanet;
                ImportTo      = importPlanet;
                Goods         = goodsType;
                BlockadeTimer = blockadeTimer;
                Freighter     = freighter;
                StardateAdded = exportPlanet.Universe.StarDate;

                ExportFrom.AddToOutgoingFreighterList(freighter);
                ImportTo.AddToIncomingFreighterList(freighter);
            }

            public TradePlan(SavedGame.TradePlanSave save, UniverseData data, Ship freighter)
            {
                Goods         = save.Goods;
                ExportFrom    = data.FindPlanetOrNull(save.ExportFrom);
                ImportTo      = data.FindPlanetOrNull(save.ImportTo);
                BlockadeTimer = save.BlockadeTimer;
                Freighter     = freighter;
                StardateAdded = save.StardateAdded;
            }

            public void UnRegisterTrade(Ship freighter)
            {
                ExportFrom.RemoveFromOutgoingFreighterList(freighter);
                ImportTo.RemoveFromIncomingFreighterList(freighter);
            }
        }

        public enum Plan
        {
            Stop,
            Scrap,
            HoldPosition,
            Bombard,
            Exterminate,
            RotateToFaceMovePosition,
            RotateToDesiredFacing,
            MoveToWithin1000,
            MakeFinalApproach,
            FleetReformAtWayPoint,
            RotateInlineWithVelocity,
            Orbit,
            Colonize,
            Explore,
            Rebase,
            DoCombat,
            Trade,
            DefendSystem,
            DeployStructure,
            PickupGoods,
            DropOffGoods,
            ReturnToHangar,
            TroopToShip,
            BoardShip,
            SupplyShip,
            Refit,
            LandTroop,
            ResupplyEscort,
            RebaseToShip,
            ReturnHome,
            DeployOrbital,
            Escort,
            RearmShipFromPlanet,
            Meteor
        }
    }
}