using System;
using Ship_Game.AI;
using Ship_Game.Ships;

namespace Ship_Game
{
    public partial class Planet // Created by Fat Bastard
    {
        public readonly Array<Ship> IncomingFreighters = new Array<Ship>();
        readonly Array<Ship> OutgoingFreighters = new Array<Ship>();
        private float AverageImportTurns;

        public float IncomingFood { get; protected set; }
        public float IncomingProd { get; protected set; }
        public float IncomingPop { get; protected set; }

        public int NumIncomingFreighters   => IncomingFreighters.Count;
        public int NumOutgoingFreighters   => OutgoingFreighters.Count;
        public Guid[] IncomingFreighterIds => IncomingFreighters.Select(s => s.guid);
        public Guid[] OutgoingFreighterIds => OutgoingFreighters.Select(s => s.guid);

        public int IncomingFoodFreighters      => FreighterTraffic(IncomingFreighters, Goods.Food);
        public int IncomingProdFreighters      => FreighterTraffic(IncomingFreighters, Goods.Production);
        public int IncomingColonistsFreighters => FreighterTraffic(IncomingFreighters, Goods.Colonists);

        public int OutgoingFoodFreighters      => FreighterTraffic(OutgoingFreighters, Goods.Food);
        public int OutgoingProdFreighters      => FreighterTraffic(OutgoingFreighters, Goods.Production);
        public int OutGoingColonistsFreighters => FreighterTraffic(OutgoingFreighters, Goods.Colonists);

        public int FreeFoodExportSlots     => FreeFreighterSlots(FoodExportSlots, OutgoingFoodFreighters);
        public int FreeProdExportSlots     => FreeFreighterSlots(ProdExportSlots, OutgoingProdFreighters);
        public int FreeColonistExportSlots => FreeFreighterSlots(ColonistsExportSlots, OutGoingColonistsFreighters);

        public int FreeFoodImportSlots     => FreeFreighterSlots(FoodImportSlots, IncomingFoodFreighters);
        public int FreeProdImportSlots     => FreeFreighterSlots(ProdImportSlots, IncomingProdFreighters);
        public int FreeColonistImportSlots => FreeFreighterSlots(ColonistsImportSlots, IncomingColonistsFreighters);

        public bool TradeBlocked => !Safe;

        public int FoodExportSlots
        {
            get
            {
                if (TradeBlocked || !ExportFood)
                    return 0;

                int min = Storage.FoodRatio > 0.75f ? 1 : 0;
                return ((int)(Food.NetIncome / 2 + Storage.Food / 25)).Clamped(min, 10);
            }
        }

        public int ProdExportSlots
        {
            get
            {
                if (TradeBlocked || !ExportProd)
                    return 0;

                int min = Storage.ProdRatio > 0.5f ? 1 : 0;
                return ((int)(Prod.NetIncome / 2 + Storage.Prod / 50)).Clamped(min, 5);
            }
        }

        public int ColonistsExportSlots
        {
            get
            {
                if (TradeBlocked || ColonistsTradeState != GoodState.EXPORT)
                    return 0;

                return (int)(PopulationBillion / 3);
            }
        }

        public int FoodImportSlots
        {
            get
            {
                if (TradeBlocked || !ImportFood)
                    return 0;

                if (NoGovernorAndNotTradeHub)
                {
                    // for players with no governor or with trade hub - only 90% storage or less will open slots
                    if (Storage.FoodRatio > 0.9f)
                        return 0;  
                }

                float foodMissing = Storage.Max - FoodHere - IncomingFood;
                foodMissing      += (-Food.NetIncome * AverageImportTurns).LowerBound(0);
                int maxSlots      = ((int)(CurrentGame.GalaxySize) * 5).LowerBound(5) + Owner.NumTradeTreaties;
                int foodSlots     = foodMissing < 5 ? 0 : (foodMissing / Owner.AverageFreighterCargoCap).RoundUpTo(1);
                return foodSlots.Clamped(0, maxSlots);
            }
        }

        public int ProdImportSlots
        {
            get
            {
                if (TradeBlocked || !ImportProd)
                    return 0;

                float averageFreighterCargoCap = Owner.AverageFreighterCargoCap;
                if (NonCybernetic)
                {
                    switch (ConstructionQueue.Count)
                    {
                        // No construction queue cases for non cybernetics
                        case 0 when Storage.ProdRatio.AlmostEqual(1): return 0;
                        case 0: return ((int)((Storage.Max - ProdHere - IncomingProd) / averageFreighterCargoCap) + 1).Clamped(0, 6);
                    }
                }

                // We have items in construction
                float prodForStorage  = Storage.Max - ProdHere;
                float totalProdNeeded = prodForStorage + TotalProdNeededInQueue() - IncomingProd;
                float totalProdSlots  = (totalProdNeeded / averageFreighterCargoCap).LowerBound(0);

                if (IsCybernetic) // They need prod as food
                    totalProdSlots += ((int)(-Prod.NetIncome * AverageImportTurns / averageFreighterCargoCap)).LowerBound(0);

                int maxSlots = ((int)(CurrentGame.GalaxySize) * 5).LowerBound(5) + Owner.NumTradeTreaties;
                return (int)(totalProdSlots).Clamped(0, maxSlots);
            }
        }

        public int ColonistsImportSlots
        {
            get
            {
                if (TradeBlocked || ColonistsTradeState != GoodState.IMPORT)
                    return 0;

                return (int)(8 - PopulationBillion).Clamped(0, 5);
            }
        }

        public int FreeFreighterSlots(int slots, int freighters)
        {
            return Math.Max(slots - freighters, 0);
        }

        public int FreighterTraffic(Array<Ship> freighterList, Goods goods)
        {
            return freighterList.Count(s => s?.AI.HasTradeGoal(goods) == true);
        }

        // Freighters on the way to pick up from us
        public int NumOutgoingFreightersPickUp(Array<Ship> freighterList, Goods goods)
        {
            int numFreighters = 0;
            for (int i = 0; i < freighterList.Count; i++)
            {
                Ship freighter = freighterList[i];
                if (freighter?.Active == true
                    && freighter.AI.FindGoal(ShipAI.Plan.PickupGoods, out ShipAI.ShipGoal goal)
                    && goal.Trade.Goods == goods)
                {
                    numFreighters += 1;
                }
            }

            return numFreighters;
        }

        public int FreeGoodsImportSlots(Goods goods)
        {
            switch (goods)
            {
                case Goods.Food:       return FreeFoodImportSlots;
                case Goods.Production: return FreeProdImportSlots;
                case Goods.Colonists:  return FreeColonistImportSlots;
                default:               return 0;
            }
        }

        public int FreeGoodsExportSlots(Goods goods)
        {
            switch (goods)
            {
                case Goods.Food: return FreeFoodExportSlots;
                case Goods.Production: return FreeProdExportSlots;
                case Goods.Colonists: return FreeColonistExportSlots;
                default: return 0;
            }
        }

        public void AddToIncomingFreighterList(Ship ship)
        {
            IncomingFreighters.AddUniqueRef(ship);
        }

        public void AddToOutgoingFreighterList(Ship ship)
        {
            OutgoingFreighters.AddUniqueRef(ship);
        }

        public void RemoveFromIncomingFreighterList(Ship ship)
        {
            IncomingFreighters.Remove(ship);
        }

        public void RemoveFromOutgoingFreighterList(Ship ship)
        {
            for (int i = 0; i < OutgoingFreighters.Count; i++)
            {
                var freight = OutgoingFreighters[i];
                if (freight == ship)
                    OutgoingFreighters.RemoveAt(i);
            }
        }

        private void RemoveInvalidFreighters(Array<Ship> freighters)
        {
            for (int i = freighters.Count - 1; i >= 0; --i)
            {
                Ship ship = freighters[i];
                if (ship == null || !ship.Active || ship.AI.State != AIState.SystemTrader)
                {
                    freighters.RemoveAtSwapLast(i);
                }
                else if (ship.loyalty != Owner && !Owner.IsTradeTreaty(ship.loyalty))
                {
                    // cancel trade plan and remove from list if trade treaty was canceled
                    freighters.RemoveAtSwapLast(i);
                    ship.AI.CancelTradePlan(ship.loyalty.FindNearestRallyPoint(ship.Center));
                }
            }
        }

        public float ExportableFood(Planet exportPlanet, Planet importPlanet, float eta)
        {
            if (!ExportFood || !importPlanet.ImportFood)
                return 0;

            float maxFoodLoad   = importPlanet.Storage.Max - importPlanet.FoodHere - importPlanet.IncomingFood;
            float foodLoadLimit = exportPlanet.ExportGoodsLimit(Goods.Food);
            maxFoodLoad        -= importPlanet.Food.NetIncome * eta;
            return maxFoodLoad.Clamped(0, foodLoadLimit);
        }

        public float ExportableProd(Planet exportPlanet, Planet importPlanet, float eta)
        {
            if (!ExportProd)
                return 0;

            float maxProdLoad   = importPlanet.Storage.Max - importPlanet.ProdHere - importPlanet.IncomingProd;
            float prodLoadLimit = exportPlanet.ExportGoodsLimit(Goods.Production);
            if (importPlanet.Prod.NetIncome < 0) // Cybernetics can have negative production
            {
                maxProdLoad -= importPlanet.Prod.NetIncome * eta;
            }
            else
            {
                if (importPlanet.ConstructionQueue.Count > 0)
                    maxProdLoad += importPlanet.Prod.NetIncome * eta.UpperBound(importPlanet.TurnsUntilQueueCompleted);
                else
                    maxProdLoad = ProdHere.UpperBound(maxProdLoad);
            }

            return maxProdLoad.Clamped(0f, prodLoadLimit); ;
        }

        public float ExportablePop(Planet exportPlanet, Planet importPlanet)
        {
            if (importPlanet.IsStarving)
                return 0;

            float maxPopLoad = importPlanet.MaxPopulation - importPlanet.Population - importPlanet.IncomingPop;
            return maxPopLoad.Clamped(0, exportPlanet.MaxPopulation * 0.2f);
        }

        void CalcIncomingGoods()
        {
            IncomingFood = CalcIncomingGoods(Goods.Food);
            IncomingProd = CalcIncomingGoods(Goods.Production);
            IncomingPop  = CalcIncomingGoods(Goods.Colonists);
        }

        float CalcIncomingGoods(Goods goods)
        {
            float numGoods = 0;
            for (int i = 0; i < IncomingFreighters.Count; i++)
            {
                Ship freighter = IncomingFreighters[i];
                if (freighter.Active && freighter.AI.HasTradeGoal(goods))
                   numGoods += freighter.CheckExpectedGoods(goods);
            }

            return numGoods;
        }

        public float ExportGoodsLimit(Goods goods)
        {
            float limit = 0; // it is a multiplier
            switch (goods)
            {
                case Goods.Food:       limit = FoodHere / NumOutgoingFreightersPickUp(OutgoingFreighters, goods).LowerBound(1); break;
                case Goods.Production: limit = ProdHere / NumOutgoingFreightersPickUp(OutgoingFreighters, goods).LowerBound(1); break;
                case Goods.Colonists:  limit = Population * 0.2f;                                                               break;
            }
            return limit;
        }
    }
}
