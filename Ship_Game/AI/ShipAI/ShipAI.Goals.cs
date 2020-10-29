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
        public bool HasPriorityOrder { get; private set;}
        public bool HadPO;

        void DequeueWayPointAndOrder()
        {
            if (WayPoints.Count > 0)
                WayPoints.Dequeue();
            DequeueCurrentOrder();
        }

        void DequeueCurrentOrderAndPriority()
        {
            DequeueCurrentOrder();
            ShipGoal goal = OrderQueue.PeekFirst;
            // remove priority order only if there are no way points
            if (goal == null || goal.Plan != Plan.MoveToWithin1000)
                SetPriorityOrder(false);
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

        public void ClearOrders(AIState newState = AIState.AwaitingOrders, bool priority = false)
        {
            for (int i = 0; i < OrderQueue.Count; i++)
            {
                ShipGoal g = OrderQueue[i];
                g?.Dispose();
            }

            ChangeAIState(newState);
            EscortTarget = null;
            if (ExplorationTarget != null)
            {
                Owner.loyalty.GetEmpireAI().ExpansionAI.RemoveExplorationTargetFromList(ExplorationTarget);
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
                    if (Owner.shipData.Role == ShipData.RoleName.supply)
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
            OrderQueue.Enqueue(new ShipGoal(sg, data, Owner));
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
            ShipGoal goal = new ShipGoal(plan, pos, dir, null, theGoal, 0f, variableString, variableNumber, wantedState, null);
            EnqueueOrPush(goal, pushToFront);
        }

        void AddShipGoal(Plan plan, Vector2 pos, Vector2 dir, Planet targetPlanet, float speedLimit, Goal empireGoal, AIState wantedState)
        {
            EnqueueOrPush(new ShipGoal(plan, pos, dir, targetPlanet, empireGoal, speedLimit, "", 0f, wantedState, null));
        }

        void AddShipGoal(Plan plan, Ship targetShip, AIState wantedState)
        {
            EscortTarget = targetShip;
            EnqueueOrPush(new ShipGoal(plan, targetShip.Position, Vectors.Up, null, null
                , 0f, "", 0f, wantedState, targetShip));
        }

        void AddShipGoal(Plan plan, Vector2 pos, Vector2 dir, float speedLimit, AIState wantedState)
        {
            EnqueueOrPush(new ShipGoal(plan, pos, dir, null, null, speedLimit, "", 0f, wantedState, null));
        }

        void AddShipGoal(Plan plan, Vector2 pos, Vector2 dir, Planet targetPlanet, Goal theGoal, AIState wantedState)
        {
            EnqueueOrPush(new ShipGoal(plan, pos, dir, targetPlanet, theGoal, 0f, "", 0f, wantedState, null));
        }

        internal void SetTradePlan(Plan plan, Planet exportPlanet, Planet importPlanet, Goods goodsType, float blockadeTimer = 120f)
        {
            ClearOrders(AIState.SystemTrader);
            OrderQueue.Enqueue(new ShipGoal(plan, exportPlanet, importPlanet, goodsType, Owner, blockadeTimer, AIState.SystemTrader));
        }

        bool AddShipGoal(Plan plan, Planet target, Goal theGoal, AIState wantedState, bool pushToFront = false)
        {
            if (target == null)
            {
                Log.Error($"AddShipGoal {plan}: planet was null! Goal discarded.");
                return false;
            }

            ShipGoal goal = new ShipGoal(plan, target.Center, Vectors.Up, target, theGoal, 0f, "", 0f, wantedState, null);
            EnqueueOrPush(goal, pushToFront);
            return true;
        }

        void AddMoveOrder(Plan plan, WayPoint wayPoint, AIState state, float speedLimit, MoveTypes move, Goal goal = null)
        {
            EnqueueOrPush(new ShipGoal(plan, wayPoint.Position, wayPoint.Direction, state, move, speedLimit, goal));
        }

        void EnqueueOrPush(ShipGoal goal, bool pushToFront = false)
        {
            if (pushToFront)
                OrderQueue.PushToFront(goal);
            else
                OrderQueue.Enqueue(goal);

            ChangeAIState(goal.WantedState);
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

        void AddPriorityBombPlanetGoal(Planet p) => AddPlanetGoal(Plan.Bombard, p
                                                  , AIState.Bombard, true, true);

        void AddLandTroopGoal(Planet p)      => AddPlanetGoal(Plan.LandTroop, p, AIState.AssaultPlanet);
        void AddBombPlanetGoal(Planet p)     => AddPlanetGoal(Plan.Bombard, p, AIState.Bombard);
        void AddExterminateGoal(Planet p)    => AddPlanetGoal(Plan.Exterminate, p, AIState.Exterminate);
        void AddResupplyPlanetGoal(Planet p) => AddPlanetGoal(Plan.Orbit, p, AIState.Resupply, pushToFront: true);

        void AddOrbitPlanetGoal(Planet p, AIState newState = AIState.Orbit) => AddPlanetGoal(Plan.Orbit, p, newState);

        public void OrderMoveAndColonize(Planet planet, Goal g)
        {
            OrderMoveTo(GetPositionOnPlanet(planet), Vectors.Up, true, AIState.Colonize);
            AddShipGoal(Plan.Colonize, planet.Center, Vectors.Up, planet, g, AIState.Colonize);
        }

        public void OrderMoveAndRebase(Planet p)
        {
            Vector2 direction = Owner.Center.DirectionToTarget(p.Center);
            OrderMoveToNoStop(GetPositionOnPlanet(p), direction, false, AIState.Rebase);
            AddPlanetGoal(Plan.Rebase, p, AIState.Rebase, priority: true);
        }

        public void OrderSupplyShipLand(Planet p)
        {
            Vector2 direction = Owner.Center.DirectionToTarget(p.Center);
            OrderMoveToNoStop(GetPositionOnPlanet(p), direction, false, AIState.SupplyReturnHome);
            IgnoreCombat = true;
            EscortTarget = null;
            SetPriorityOrder(true);
        }

        public void OrderMoveAndRefit(Planet planet, Goal g)
        {
            if (!Owner.IsPlatformOrStation)
            {
                OrderMoveTo(GetPositionOnPlanet(planet), Vectors.Up, true, AIState.Refit);
                IgnoreCombat = true;
                ResetPriorityOrder(clearOrders: false);
            }

            AddShipGoal(Plan.Refit, planet, g, AIState.Refit);
        }

        public void OrderMoveAndScrap(Planet p)
        {
            Vector2 direction = Owner.Center.DirectionToTarget(p.Center);
            OrbitTarget = p;
            OrderMoveTo(GetPositionOnPlanet(p), direction, true, AIState.Scrap);
            AddPlanetGoal(Plan.Scrap, p, AIState.Scrap);
        }

        public void OderMoveAndDefendSystem(Planet p)
        {
            OrderMoveTo(GetPositionOnPlanet(p), Vectors.Up, true, AIState.SystemDefender);
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

        [Flags]
        public enum MoveTypes
        {
            None             = 0,
            Combat           = 1,
            WayPoint         = 2,
            Positioning      = 4,
            Begin            = 8,
            End              = 16,
            FirstWayPoint    = Begin | WayPoint,
            PrepareForWarp   = Begin | Positioning,
            LastWayPoint     = End | WayPoint,
            SubLightApproach = End | Positioning,
            CombatWayPoint   = Combat | WayPoint,
            CombatApproach   = Combat | SubLightApproach

        }

        public class ShipGoal : IDisposable
        {
            bool IsDisposed;
            // ship goal variables are read-only by design, do not allow writes!
            public readonly Plan Plan;
            private Vector2 StaticMovePosition;
            public Vector2 MovePosition
            {
                get
                {
                    if (Goal != null) return Goal.MovePosition;
                    if (TargetPlanet != null) return TargetPlanet.Center;
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
            public readonly MoveTypes MoveType;

            public float GetSpeedLimitFor(Ship ship) => ship.fleet?.GetSpeedLimitFor(ship) ?? SpeedLimit;

            public override string ToString() => $"{Plan} pos:{MovePosition} dir:{Direction}";

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

            public ShipGoal(Plan plan, Planet exportPlanet, Planet importPlanet, Goods goods, Ship freighter, float blockadeTimer, AIState wantedState)
            {
                Plan        = plan;
                Trade       = new TradePlan(exportPlanet, importPlanet, goods, freighter, blockadeTimer);
                WantedState = wantedState;
            }

            public ShipGoal(Plan plan, Vector2 waypoint, Vector2 direction, AIState state, MoveTypes moveType, float speedLimit, Goal goal)
            {
                Plan         = plan;
                MovePosition = waypoint;
                Direction    = direction;
                WantedState  = state;
                MoveType     = moveType;
                Goal         = goal;
            }

            //public ShipGoal(Plan plan, Vector2 waypoint, AIState state, MoveTypes moveType, float speedLimit)
            //    : this(plan, waypoint, new Vector2(1, 0), state, moveType, speedLimit) { }
        

            public ShipGoal(SavedGame.ShipGoalSave sg, UniverseData data, Ship ship)
            {
                Plan         = sg.Plan;
                MovePosition = sg.MovePosition;
                Direction    = sg.Direction;
                WantedState  = sg.WantedState;

                TargetPlanet = data.FindPlanetOrNull(sg.TargetPlanetGuid);
                TargetShip   = data.FindShipOrNull(sg.TargetShipGuid);

                VariableString = sg.VariableString;
                SpeedLimit     = sg.SpeedLimit;
                Empire loyalty = ship.loyalty;

                if (sg.fleetGuid != Guid.Empty)
                {
                    foreach (KeyValuePair<int, Fleet> empireFleet in loyalty.GetFleetsDict())
                        if (empireFleet.Value.Guid == sg.fleetGuid)
                            Fleet = empireFleet.Value;
                }

                if (sg.goalGuid != Guid.Empty)
                {
                    Array<Goal> goals = loyalty.GetEmpireAI().Goals;
                    foreach (Goal empireGoal in goals)
                    {
                        if (sg.goalGuid == empireGoal.guid)
                        {
                            Goal = empireGoal;
                            break;
                        }
                    }
                    if (Goal == null)
                        Log.Warning($"ShipGoalSave {sg.Plan}: failed to find Empire.Goal {sg.goalGuid}");
                }

                if (sg.Trade != null)
                    Trade = new TradePlan(sg.Trade, data, ship);

                if (Plan == Plan.SupplyShip)
                    ship.AI.EscortTarget?.Supply.ChangeIncomingSupply(SupplyType.Rearm, ship.Ordinance);
                MoveType = sg.MoveType;
            }

            public bool HasCombatMove(float distance)
            {
                bool combat       = MoveType.HasFlag(MoveTypes.Combat);
                bool lastWayPoint = MoveType.HasFlag(MoveTypes.LastWayPoint);

                if (!lastWayPoint && distance > 0)   return combat;
                if (distance > 10000)                return combat;
                if (distance < 1000 && Goal == null) return true;

                return Goal?.IsPriorityMovement() ?? combat;
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
            public float BlockadeTimer; // indicates how much time to wait with freight when trade is blocked

            public TradePlan(Planet exportPlanet, Planet importPlanet, Goods goodsType, Ship freighter, float blockadeTimer)
            {
                ExportFrom    = exportPlanet;
                ImportTo      = importPlanet;
                Goods         = goodsType;
                BlockadeTimer = blockadeTimer;
                Freighter      = freighter;

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
            HoldPositionOffensive,
            Escort,
            RearmShipFromPlanet
        }
    }
}