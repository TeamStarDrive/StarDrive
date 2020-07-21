﻿using System;
using Ship_Game.AI;
using Ship_Game.Ships;

namespace Ship_Game
{
    public partial class Planet // Created by Fat Bastard
    {
        readonly Array<Ship> IncomingFreighters = new Array<Ship>();
        readonly Array<Ship> OutgoingFreighters = new Array<Ship>();

        public float IncomingFood { get; protected set; }
        public float IncomingProd { get; protected set; }

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
                return ((int)(Food.NetIncome / 2 + Storage.Food / 50)).Clamped(min, 7);
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

                int foodIncomeSlots  = (int)(1 - Food.NetIncome);
                int foodStorageRatio = (int)((1 - Storage.FoodRatio) * 3);
                return (foodIncomeSlots + foodStorageRatio).Clamped(0, 5 + Owner.NumTradeTreaties);
            }
        }

        public int ProdImportSlots
        {
            get
            {
                if (TradeBlocked || !ImportProd)
                    return 0;

                if (NonCybernetic)
                {
                    switch (ConstructionQueue.Count)
                    {
                        // No construction queue cases for non cybernetics
                        case 0 when Storage.ProdRatio.AlmostEqual(1): return 0;
                        case 0: return ((int)((Storage.Max - Storage.Prod) / 50) + 1).Clamped(0, 6);
                    }
                }

                // We have items in construction
                float totalProdNeeded = TotalProdNeededInQueue() - ProdHere - IncomingProd;
                float totalProdSlots  = (totalProdNeeded / Owner.AverageFreighterCargoCap).LowerBound(0);

                if (IsCybernetic) // They need prod as food
                    totalProdSlots += (int)((1 - Storage.ProdRatio) * 3);

                int maxSlots = ((int)(CurrentGame.GalaxySize) * 5).LowerBound(5);
                return (int)(totalProdSlots).Clamped(0, maxSlots + Owner.NumTradeTreaties);
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
            return freighterList.Count(s => s.AI.HasTradeGoal(goods));
        }

        public int FreeGoodsImportSlots(Goods goods)
        {
            switch (goods)
            {
                case Goods.Food: return FreeFoodImportSlots;
                case Goods.Production: return FreeProdImportSlots;
                case Goods.Colonists: return FreeColonistImportSlots;
                default: return 0;
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
            OutgoingFreighters.Remove(ship);
        }

        private void RemoveInvalidFreighters(Array<Ship> freighters)
        {
            for (int i = freighters.Count - 1; i >= 0; --i)
            {
                Ship ship = freighters[i];
                if (!ship.Active || ship.AI.State != AIState.SystemTrader)
                {
                    freighters.RemoveAtSwapLast(i);
                }
                else if (ship.loyalty != Owner && !Owner.GetRelations(ship.loyalty).Treaty_Trade)
                {
                    // cancel trade plan and remove from list if trade treaty was canceled
                    freighters.RemoveAtSwapLast(i);
                    ship.AI.CancelTradePlan(ship.loyalty.FindNearestRallyPoint(ship.Center));
                }
            }
        }

        public float ExportableFood(Planet importPlanet, float eta)
        {
            float maxFoodLoad   = importPlanet.Storage.Max - importPlanet.FoodHere;
            float foodLoadLimit = Owner?.GoodsLimits(Goods.Food) ?? 0;
            maxFoodLoad         = (maxFoodLoad - importPlanet.Food.NetIncome * eta).Clamped(0, Storage.Max * foodLoadLimit);
            return maxFoodLoad;
        }

        public float ExportableProd(Planet importPlanet)
        {
            float prodLoadLimit = Owner?.GoodsLimits(Goods.Production) ?? 0;
            float maxProdLoad   = ProdHere.Clamped(0f, Storage.Max * prodLoadLimit);
            return maxProdLoad;
        }

        void CalcIncomingGoods()
        {
            IncomingFood = CalcIncomingGoods(Goods.Food);
            IncomingProd = CalcIncomingGoods(Goods.Production);
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
    }
}
