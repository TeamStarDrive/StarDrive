using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Ship_Game.Ships;

namespace Ship_Game.AI 
{
    public sealed partial class ShipAI
    {
        public bool HasPriorityOrder;
        public bool HadPO;

        void DequeueWayPointAndOrder()
        {
            if (WayPoints.Count > 0)
                WayPoints.Dequeue();
            DequeueCurrentOrder();
        }

        void DequeueCurrentOrder()
        {
            if (OrderQueue.TryDequeue(out ShipGoal goal))
                goal.Dispose();
        }

        public void ClearOrders(AIState newState = AIState.AwaitingOrders, bool priority = false)
        {
            if (Empire.Universe is DeveloperSandbox.DeveloperUniverse)
                Log.Info(ConsoleColor.Blue, $"ClearOrders new_state:{newState} priority:{priority}");

            foreach (ShipGoal g in OrderQueue)
                g.Dispose();

            OrderQueue.Clear();
            State = newState;
            HasPriorityOrder = priority;
        }

        public void ClearOrdersAndWayPoints(AIState newState = AIState.AwaitingOrders, bool priority = false)
        {
            ClearWayPoints();
            ClearOrders(newState, priority);
        }

        public void ClearPriorityOrder()
        {
            HasPriorityOrder  = false;
            Intercepting      = false;
            HasPriorityTarget = false;
        }

        void SetPriorityOrderWithClear()
        {
            SetPriorityOrder(true);
            ClearWayPoints();
        }

        public void SetPriorityOrder(bool clearOrders)
        {
            if (clearOrders)
                ClearOrders(State, true);
            else
                HasPriorityOrder = true;
            Intercepting      = false;
            HasPriorityTarget = false;
        }

        public bool FindGoal(Plan plan, out ShipGoal goal)
        {
            foreach (ShipGoal g in OrderQueue)
            {
                if (g.Plan == plan)
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

        void AddShipGoal(Plan plan)
        {
            OrderQueue.Enqueue(new ShipGoal(plan));
        }

        void AddShipGoal(Plan plan, Vector2 pos, Vector2 dir)
        {
            OrderQueue.Enqueue(new ShipGoal(plan, pos, dir, null, null, 0f, "", 0f));
        }

        void AddShipGoal(Plan plan, Vector2 pos, Vector2 dir, Goal theGoal, 
                         string variableString, float variableNumber)
        {
            OrderQueue.Enqueue(new ShipGoal(plan, pos, dir, null, theGoal, 
                                            0f, variableString, variableNumber));
        }

        void AddShipGoal(Plan plan, Vector2 pos, Vector2 dir, Planet targetPlanet, float speedLimit)
        {
            OrderQueue.Enqueue(new ShipGoal(plan, pos, dir, targetPlanet, null, speedLimit, "", 0f));
        }

        void SetTradePlan(Plan plan, Planet exportPlanet, Planet importPlanet, Goods goodsType, float blockadeTimer = 120f)
        {
            ClearOrders(AIState.SystemTrader);
            OrderQueue.Enqueue(new ShipGoal(plan, exportPlanet, importPlanet, goodsType, Owner, blockadeTimer));
        }

        bool AddShipGoal(Plan plan, Planet target, string variableString = "")
        {
            if (target == null)
            {
                Log.Error($"AddShipGoal {plan}: planet was null! Goal discarded.");
                return false;
            }

            OrderQueue.Enqueue(new ShipGoal(plan, target.Center, Vectors.Up, 
                target, null, 0f, variableString, 0f));
            return true;
        }

        void AddPlanetGoal(Plan plan, Planet planet, AIState newState, bool priority = false)
        {
            if (AddShipGoal(plan, planet))
            {
                State = newState;
                OrbitTarget = planet;
                if (priority)
                    SetPriorityOrder(false);
            }
        }

        void AddOrbitPlanetGoal(Planet planet, AIState newState = AIState.Orbit)
        {
            AddPlanetGoal(Plan.Orbit, planet, newState);
        }

        public class ShipGoal : IDisposable
        {
            bool IsDisposed;
            // ship goal variables are read-only by design, do not allow writes!
            public readonly Plan Plan;
            public readonly Vector2 MovePosition;
            public readonly Vector2 Direction; // direction param for this goal, can have multiple meanings
            public readonly Planet TargetPlanet;
            public readonly Goal Goal;
            public readonly Fleet Fleet;
            public readonly float SpeedLimit;
            public readonly string VariableString;
            public readonly float VariableNumber;
            public TradePlan Trade;

            public override string ToString() => $"{Plan} pos:{MovePosition} dir:{Direction}";

            public ShipGoal(Plan plan)
            {
                Plan = plan;
            }

            public ShipGoal(Plan plan, Vector2 pos, Vector2 dir, Planet targetPlanet, Goal theGoal,
                            float speedLimit, string variableString, float variableNumber)
            {
                Plan         = plan;
                MovePosition = pos;
                Direction    = dir;
                TargetPlanet = targetPlanet;
                Goal         = theGoal;
                SpeedLimit   = speedLimit;
                VariableString = variableString;
                VariableNumber = variableNumber;
            }

            public ShipGoal(Plan plan, Planet exportPlanet, Planet importPlanet, Goods goods, Ship freighter, float blockadeTimer)
            {
                Plan = plan;
                Trade = new TradePlan(exportPlanet, importPlanet, goods, freighter, blockadeTimer);
            }

            public ShipGoal(SavedGame.ShipGoalSave sg, UniverseData data, Ship ship)
            {
                Plan = sg.Plan;
                MovePosition = sg.MovePosition;
                Direction = sg.DesiredFacing.RadiansToDirection();

                if (sg.TargetPlanetGuid != Guid.Empty)
                    TargetPlanet = data.FindPlanet(sg.TargetPlanetGuid);

                VariableString = sg.VariableString;
                SpeedLimit = sg.SpeedLimit;

                Empire loyalty = ship.loyalty;
                
                if (sg.fleetGuid != Guid.Empty)
                {
                    foreach (KeyValuePair<int, Fleet> empireFleet in loyalty.GetFleetsDict())
                        if (empireFleet.Value.Guid == sg.fleetGuid)
                            Fleet = empireFleet.Value;
                }

                if (sg.goalGuid != Guid.Empty)
                {
                    foreach (Goal empireGoal in loyalty.GetEmpireAI().Goals)
                    {
                        if (sg.goalGuid == empireGoal.guid)
                            Goal = empireGoal;
                    }
                }

                if (sg.Trade != null)
                    Trade = new TradePlan(sg.Trade, data, ship);
            }

            ~ShipGoal() { Destroy(); } // finalizer
            public void Dispose()
            {
                Destroy();
                GC.SuppressFinalize(this);
            }

            private void Destroy()
            {
                if (IsDisposed) return;
                IsDisposed = true;
                Trade?.UnregisterTrade(Trade.Freighter);
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

                RegisterTrade(freighter);
            }
            
            public void RegisterTrade(Ship freighter)
            {
                ExportFrom.AddToOutgoingFreighterList(freighter);
                ImportTo.AddToIncomingFreighterList(freighter);
            }

            public void UnregisterTrade(Ship freighter)
            {
                ExportFrom.RemoveFromOutgoingFreighterList(freighter);
                ImportTo.RemoveFromIncomingFreighterList(freighter);
            }

            public TradePlan(SavedGame.TradePlanSave save, UniverseData data, Ship freighter)
            {
                Goods         = save.Goods;
                ExportFrom    = data.FindPlanet(save.ExportFrom);
                ImportTo      = data.FindPlanet(save.ImportTo);
                BlockadeTimer = save.BlockadeTimer;
                Freighter     = freighter;
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
            ReturnHome
        }
    }
}