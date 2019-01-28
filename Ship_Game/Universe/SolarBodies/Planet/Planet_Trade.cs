using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Ship_Game.AI;
using Ship_Game.Debug;
using Ship_Game.Ships;
using Ship_Game.Universe.SolarBodies.AI;

namespace Ship_Game
{
    public partial class Planet
    {
        public TradeAI TradeAI => Storage.Trade;

        public float IncomingFood;
        public float IncomingProduction;
        public float IncomingColonists;

        void CalculateIncomingTrade()
        {
            if (Owner == null || Owner.isFaction)
                return;

            IncomingProduction = 0;
            IncomingFood = 0;
            TradeAI.ClearHistory();
            using (Owner.GetShips().AcquireReadLock())
            {
                foreach (var ship in Owner.GetShips())
                {
                    if (ship.DesignRole != ShipData.RoleName.freighter) continue;
                    if (ship.AI.State != AIState.SystemTrader && ship.AI.State != AIState.PassengerTransport) continue;
                    if (ship.AI.OrderQueue.IsEmpty) continue;
                    TradeAI.AddTrade(ship);
                }
            }
            TradeAI.ComputeAverages();
            IncomingFood       = TradeAI.AvgTradingFood;
            IncomingProduction = TradeAI.AvgTradingProduction;
            IncomingColonists  = TradeAI.AvgTradingColonists;
        }

        
        public bool ShortOnFood()
        {
            if (Owner?.isFaction ?? true)
                return false;
            float incoming = IsCybernetic ? IncomingProduction : IncomingFood;
            float food = Storage.RaceFood + incoming;
            return (food / Storage.Max) < 0.1f;
        }

        void DebugImportFood(float predictedFood, string text) =>
            Empire.Universe?.DebugWin?.DebugLogText($"IFOOD PREDFD:{predictedFood:0.#} {text} {this}", DebugModes.Trade);

        void DebugImportProd(float predictedFood, string text) =>
            Empire.Universe?.DebugWin?.DebugLogText($"IPROD PREDFD:{predictedFood:0.#} {text} {this}", DebugModes.Trade);

        public Goods ImportPriority()
        {
            // Is this an Import-Export type of planet?
            if (ImportFood && ExportProd) return Goods.Food;
            if (ImportProd && ExportFood) return Goods.Production;

            const int lookahead = 30; // 1 turn ~~ 5 second, 12 turns ~~ 1min, 60 turns ~~ 5min

            // only calc food for organic races
            float predictedFood = NonCybernetic ? ProjectedFood(lookahead) : 0f;
            if (predictedFood < 0f) // we will starve!
            {
                if (!FindConstructionBuilding(Goods.Food, out QueueItem item))
                {
                    // we will definitely starve without food, so plz send food!
                    DebugImportFood(predictedFood, "(no food buildings)");
                    return Goods.Food;
                }

                // will the building complete in reasonable time?
                int buildTurns = NumberOfTurnsUntilCompleted(item);
                int starveTurns = TurnsUntilOutOfFood();
                if (buildTurns > (starveTurns + 30))
                {
                    DebugImportFood(predictedFood, $"(build {buildTurns} > starve {starveTurns + 30})");
                    return Goods.Food; // No! We will seriously starve even if this alleviates starving
                }

                float foodProduced = item.Building.FoodProduced(this);
                if (Food.NetIncome + foodProduced >= 0f) // this building will solve starving
                {
                    DebugImportProd(predictedFood, $"(build {buildTurns})");
                    return Goods.Production; // send production to finish it faster!
                }

                // we can't wait until building is finished, import food!
                DebugImportFood(predictedFood, "(build has not enough food)");
                return Goods.Food;
            }

            // We are not starving and we're constructing stuff
            if (ConstructionQueue.Count > 0)
            {
                // this is taking too long! import production to speed it up
                int totalTurns = NumberOfTurnsUntilCompleted(ConstructionQueue.Last);
                if (totalTurns >= 60)
                {
                    DebugImportProd(predictedFood, "(construct >= 60 turns)");
                    return Goods.Production;
                }

                // only import if we're constructing more than we're producing
                float projectedProd = ProjectedProduction(totalTurns);
                if (projectedProd <= 25f)
                {
                    DebugImportProd(predictedFood, $"(projected {projectedProd:0.#} <= 25)");
                    return Goods.Production;
                }
            }
            
            // we have enough food incoming, so focus on production instead
            float predictedProduction = ProjectedProduction(lookahead);

            // we are not starving and we are not constructing anything
            // just pick which stockpile is smaller
            return predictedFood < predictedProduction ? Goods.Food : Goods.Production;
        }

        const int NEVER = 10000;

        int TurnsUntilOutOfFood()
        {
            if (IsCybernetic)
                return NEVER;

            float avg = Food.NetIncome + IncomingFood;
            if (avg > 0f) return NEVER;
            return (int)Math.Floor(FoodHere / Math.Abs(avg));
        }

        float ProjectedFood(int turns)
        {
            float incomingAvg = Food.NetIncome + IncomingFood;
            return FoodHere + incomingAvg * turns;
        }

        float ProjectedProduction(int turns)
        {
            float incomingAvg = Prod.NetIncome + IncomingProduction;

            int totalTurns = NumberOfTurnsUntilCompleted(ConstructionQueue.Last);
            int turnsOfIdleProd = turns - totalTurns;
            
            float total = ProdHere + incomingAvg * turns;
            if (turnsOfIdleProd > 0)
            {
                total += incomingAvg * turnsOfIdleProd;
            }
            return total;
        }
    }
}
