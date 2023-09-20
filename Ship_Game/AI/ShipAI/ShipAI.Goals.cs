using Ship_Game.Fleets;
using Ship_Game.Ships;
using System;
using System.Collections.Generic;
using SDGraphics;
using SDUtils;
using Ship_Game.Data.Serialization;
using Ship_Game.ExtensionMethods;
using Ship_Game.Ships.AI;
using Ship_Game.Universe;
using static Ship_Game.AI.ShipAI;

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
                if (OrderQueue.TryPeekFirst(out ShipGoal nextGoal))
                    ChangeAIState(nextGoal.WantedState);
                else
                    ChangeAIState(DefaultAIState);
            }
        }

        void DequeueOrdersUntilWayPointDequeued()
        {
            while (OrderQueue.TryDequeue(out ShipGoal goal))
            {
                goal.Dispose();
                if (OrderQueue.TryPeekFirst(out ShipGoal nextGoal))
                {
                    ChangeAIState(nextGoal.WantedState);
                    if (goal.MoveOrder.IsSet(MoveOrder.DequeueWayPoint) && WayPoints.Count > 0)
                    {
                        WayPoints.Dequeue();
                        break;
                    }
                }
                else
                {
                    ChangeAIState(DefaultAIState);
                }
            }
        }

        public void ClearOrders(AIState newState = AIState.AwaitingOrders, bool priority = false)
        {
            DisposeOrders();
            EscortTarget = null;
            PatrolTarget = null;
            Orbit?.ExitOrbit();
            SystemToDefend = null;
            IgnoreCombat = false;
            ExitCombatState();
            ChangeAIState(newState); // Must come after ExitCombatState since ExitCombatState change the AIstate to awaiting orders.
            if (ExplorationTarget != null)
            {
                Owner.Loyalty.AI.ExpansionAI.RemoveExplorationTargetFromList(ExplorationTarget);
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
                        EscortTarget?.Supply.ChangeIncomingOrdnance(-Owner.Ordinance);
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

        void AddShipGoal(Plan plan, AIState wantedState, bool pushToFront = false)
        {
            EnqueueOrPush(new ShipGoal(plan, wantedState), pushToFront);
        }

        void AddShipGoal(Plan plan, Planet targetPlanet, AIState wantedState, Ship targetShip)
        {
            EnqueueOrPush(new ShipGoal(plan, targetPlanet, wantedState, targetShip), pushToFront: true);
        }

        void AddShipGoal(Plan plan, AIState wantedState, Vector2 pos, Planet planet, bool pushToFront = false)
        {
            var goal = new ShipGoal(plan, pos, Vectors.Up, planet, null, 0f, "", 0f, wantedState, null);
            EnqueueOrPush(goal, pushToFront);
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

        internal void SetTradePlan(Plan plan, Planet exportPlanet, Ship targetStation, Goods goodsType, float blockadeTimer = 120f)
        {
            ClearOrders(AIState.SystemTrader);
            var goal = new ShipGoal(plan, exportPlanet, targetStation, goodsType, Owner, blockadeTimer, AIState.SystemTrader);
            EnqueueOrPush(goal);
        }

        bool AddShipGoal(Plan plan, Planet target, Goal theGoal, AIState wantedState, bool pushToFront = false)
        {
            if (target == null)
            {
                Log.Error($"AddShipGoal {plan}: planet was null! Goal discarded.");
                return false;
            }

            var goal = new ShipGoal(plan, target.Position, Vectors.Up, target, theGoal, 0f, "", 0f, wantedState, null);
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
                SetOrbitTarget(planet);
                if (priority)
                    ResetPriorityOrder(clearOrders: false);
            }
        }

        public void AddMeteorGoal(Planet p, float rotation, Vector2 direction, float speed)
        {
            Owner.Rotation = rotation;
            PushGoalToFront(new ShipGoal(Plan.Meteor, p.Position, direction, p,
                                         null, speed, "", 0f, AIState.MoveTo, null));
        }

        void AddOrbitPlanetGoal(Planet p, AIState newState = AIState.Orbit)
        {
            AddPlanetGoal(Plan.Orbit, p, newState);
        }

        public void OrderMoveAndColonize(Planet planet, Goal g)
        {
            OrderMoveTo(GetPositionOnPlanet(planet), Vectors.Up, AIState.Colonize);
            AddShipGoal(Plan.Colonize, planet.Position, Vectors.Up, planet, g, AIState.Colonize);
        }

        public void OrderMoveAndStandByColonize(Planet planet, Goal g)
        {
            float distanceToWait = Owner.Loyalty.GetProjectorRadius() * 1.2f;
            Vector2 dir = planet.Position.DirectionToTarget(Owner.Position);
            Vector2 pos = planet.Position + dir * distanceToWait;
            OrderMoveToNoStop(pos, -dir, AIState.Colonize, MoveOrder.AddWayPoint, goal: g);
            AddShipGoal(Plan.StandByColonize, AIState.Colonize);
        }

        public void OrderMoveAndRebase(Planet p)
        {
            Vector2 direction = Owner.Position.DirectionToTarget(p.Position);
            OrderMoveToNoStop(GetPositionOnPlanet(p), direction, AIState.Rebase, MoveOrder.AddWayPoint);
            AddPlanetGoal(Plan.Rebase, p, AIState.Rebase, priority: true);
        }

        public void OrderSupplyShipLand(Planet p)
        {
            Vector2 direction = Owner.Position.DirectionToTarget(p.Position);
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
            Vector2 direction = Owner.Position.DirectionToTarget(p.Position);
            SetOrbitTarget(p);
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

        public void AddResearchStationPlan(Plan plan)
        {
            ClearOrders();
            AddShipGoal(plan, AIState.Research);
        }

        public void AddMiningStationPlan(Plan plan)
        {
            ClearOrders();
            AddShipGoal(plan, AIState.Mining);
        }

        Vector2 GetPositionOnPlanet(Planet p)
        {
            return NewMathExt.RandomOffsetAndDistance(p.Position, p.Radius, p.Random);
        }

        [StarDataType]
        public sealed class ShipGoal : IDisposable
        {
            bool IsDisposed;
            // ship goal variables are read-only by design, do not allow writes!
            [StarData] public readonly Plan Plan;

            Vector2 StaticMovePosition;

            [StarData] public Vector2 MovePosition
            {
                get
                {
                    if (Goal != null)
                        return Goal.MovePosition;

                    // for Orbit plans we don't use Planet.Position
                    // TODO: There is a mismatch here after save load
                    if (TargetPlanet != null && Plan is not Plan.Orbit and not Plan.BuilderReturnHome)
                        return TargetPlanet.Position;

                    return StaticMovePosition;
                }
                set
                {
                    // This is here to catch math bugs
                    if (value.IsNaN())
                    {
                        Log.Error($"NaN ShipGoal.MovePosition={value}. Discarding value.");
                    }
                    else
                    {
                        StaticMovePosition = value;
                    }
                }
            }

            [StarData] public readonly Vector2 Direction; // direction param for this goal, can have multiple meanings
            [StarData] public readonly Planet TargetPlanet;
            [StarData] public readonly Ship TargetShip;
            [StarData] public readonly Goal Goal; // Empire AI Goal
            [StarData] public readonly Fleet Fleet;
            [StarData] public readonly float SpeedLimit;
            [StarData] public readonly string VariableString;
            [StarData] public readonly float VariableNumber;
            [StarData] public readonly AIState WantedState; 
            [StarData] public TradePlan Trade;
            [StarData] public readonly MoveOrder MoveOrder = MoveOrder.Regular;

            // If this is a Move Order, is it an Aggressive move?
            public bool HasAggressiveMoveOrder => (MoveOrder & MoveOrder.Aggressive) != 0;

            // If this a Move Order, is it just a plain old Regular move? (default)
            public bool HasRegularMoveOrder => (MoveOrder & MoveOrder.Regular) != 0;

            public float GetSTLSpeedLimitFor(Ship ship) => ship.Fleet?.GetSTLSpeedLimitFor(ship) ?? SpeedLimit;

            public override string ToString() => $"{Plan} {MoveOrder} pos:{MovePosition} dir:{Direction}";

            [StarDataConstructor]
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

            public ShipGoal(Plan plan, Planet targetPlanet, AIState wantedState, Ship targetShip)
            {
                Plan         = plan;
                TargetPlanet = targetPlanet;
                WantedState  = wantedState;
                TargetShip   = targetShip;
            }

            public ShipGoal(Plan plan, Planet exportPlanet, Planet importPlanet, Goods goods, 
                            Ship freighter, float blockadeTimer, AIState wantedState)
            {
                Plan        = plan;
                Trade       = new TradePlan(exportPlanet, importPlanet, goods, freighter, blockadeTimer);
                WantedState = wantedState;
            }

            public ShipGoal(Plan plan, Planet exportPlanet, Ship targetStation, Goods goods,
                            Ship freighter, float blockadeTimer, AIState wantedState)
            {
                Plan        = plan;
                Trade       = new TradePlan(exportPlanet, targetStation, goods, freighter, blockadeTimer);
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
            [StarDataDeserialized]
            void OnDeserialized()
            {
                if (Plan is Plan.SupplyShip or Plan.RearmShipFromPlanet)
                    TargetShip?.AI.EscortTarget?.Supply.ChangeIncomingOrdnance(TargetShip.Ordinance);
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

        [StarDataType]
        public class TradePlan
        {
            [StarData] public readonly Goods Goods;
            [StarData] public readonly Planet ExportFrom;
            [StarData] public readonly Planet ImportTo;
            [StarData] public readonly Ship Freighter;
            [StarData] public readonly float StardateAdded;
            [StarData] public readonly Ship TargetStation;
            [StarData] public float BlockadeTimer; // Indicates how much time to wait with freight when trade is blocked

            bool SupplyingPlanet => ImportTo != null;

            TradePlan() { }

            public TradePlan(Planet exportPlanet, Ship targetStation, Goods goodsType, Ship freighter, float blockadeTimer)
            {
                ExportFrom    = exportPlanet;
                Goods         = goodsType;
                BlockadeTimer = blockadeTimer;
                Freighter     = freighter;
                StardateAdded = exportPlanet.Universe.StarDate;
                TargetStation = targetStation;

                ExportFrom.AddToOutgoingFreighterList(freighter, goodsType);
            }

            public TradePlan(Planet exportPlanet, Planet importPlanet, Goods goodsType, Ship freighter, float blockadeTimer)
            {
                ExportFrom    = exportPlanet;
                ImportTo      = importPlanet;
                Goods         = goodsType;
                BlockadeTimer = blockadeTimer;
                Freighter     = freighter;
                StardateAdded = exportPlanet.Universe.StarDate;

                ExportFrom.AddToOutgoingFreighterList(freighter, goodsType);
                ImportTo.AddToIncomingFreighterList(freighter);
            }

            public void UnRegisterTrade(Ship freighter)
            {
                ExportFrom.RemoveFromOutgoingFreighterList(freighter);
                if (SupplyingPlanet)
                    ImportTo.RemoveFromIncomingFreighterList(freighter);
            }
        }

        // WARNING: Enums are serialized as integers, so don't change the order
        //          because it will break savegames. Add new entries to the end.
        //          At some point we will add stable enum mapping.
        public enum Plan
        {
            // using explicit integers to support deleting entries
            Stop = 0,
            Scrap = 1,
            HoldPosition = 2,
            Bombard = 3,
            Exterminate = 4, // exterminate a specific planet
            RotateToFaceMovePosition = 5,
            RotateToDesiredFacing = 6,
            MoveToWithin1000 = 7,
            MakeFinalApproach = 8,
            RotateInlineWithVelocity = 9,
            Orbit = 10,
            Colonize = 11,
            Explore = 12,
            Rebase = 13,
            DoCombat = 14,
            Trade = 15,
            DefendSystem = 16,
            DeployStructure = 17,
            PickupGoods = 18,
            DropOffGoods = 19,
            ReturnToHangar = 20,
            TroopToShip = 21,
            BoardShip = 22,
            SupplyShip = 23,
            Refit = 24,
            LandTroop = 25,
            ResupplyEscort = 26,
            RebaseToShip = 27,
            ReturnHome = 28,
            DeployOrbital = 29,
            Escort = 30,
            RearmShipFromPlanet = 31,
            Meteor = 32,
            AwaitOrders = 33,
            AwaitOrdersAIManaged = 34, // different from AwaitOrders, gives the ship over to AI management
            FindExterminationTarget = 35, // find a target to exterminate
            PickupGoodsForStation = 36,
            DropOffGoodsForStation = 37,
            ResearchStationResearching = 38, // for shipUIinfo display only
            ResearchStationIdle = 39, // for shipUIinfo display only
            ExoticStationNoSupply = 40, // for shipUIinfo display only
            StandByColonize = 41,
            BuildOrbital = 42,
            BuilderReturnHome = 43,
            MiningStationIdle = 44, // for shipUIinfo display only
            MiningStationRefining = 45 // for shipUIinfo display only
        }
    }
}
