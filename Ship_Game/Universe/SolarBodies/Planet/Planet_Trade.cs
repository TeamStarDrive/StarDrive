using System;
using SDGraphics;
using SDUtils;
using Ship_Game.AI;
using Ship_Game.Data.Serialization;
using Ship_Game.Ships;

namespace Ship_Game
{
    public partial class Planet // Created by Fat Bastard
    {
        [StarData] public readonly Array<Ship> IncomingFreighters = new();
        [StarData] readonly Array<Ship> OutgoingFreighters = new();

        [StarData] public float AverageFoodImportTurns { get; protected set; } = 10; // EMA (90/10) time it took the traded to deliver food
        [StarData] public float AverageProdImportTurns { get; protected set; } = 10; // EMA (90/10) time it took the traded to deliver prod
        [StarData] public float AverageFoodExportTurns { get; protected set; } = 10; // Turns for the trader to arrive to the *export* planet to pick up food
        [StarData] public float AverageProdExportTurns { get; protected set; } = 10; // Turns for the trader to arrive to the *export* planet to pick up prod

        public float IncomingFood { get; protected set; }
        public float IncomingProd { get; protected set; }
        public float IncomingPop  { get; protected set; }

        public int NumIncomingFreighters   => IncomingFreighters.Count;
        public int NumOutgoingFreighters   => OutgoingFreighters.Count;

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

        public bool TradeBlocked => HasSpacePort && SpaceCombatNearPlanet || !HasSpacePort && !Safe || Quarantine;

        // These values can be set by the player. If 0, the code will determine the slots limit automatically
        [StarData] public int ManualFoodImportSlots { get; protected set; } 
        [StarData] public int ManualProdImportSlots { get; protected set; }
        [StarData] public int ManualColoImportSlots { get; protected set; }
        [StarData] public int ManualFoodExportSlots { get; protected set; }
        [StarData] public int ManualProdExportSlots { get; protected set; }
        [StarData] public int ManualColoExportSlots { get; protected set; }

        public int FoodExportSlots
        {
            get
            {
                if (TradeBlocked || !ExportFood)
                    return 0;

                if (ManualFoodExportSlots > 0 && Owner == Universe.Player)
                    return ManualFoodExportSlots;

                int min = Storage.FoodRatio > 0.75f ? 2 : 1;
                int maxSlots     = CType is ColonyType.Agricultural or ColonyType.Colony ? 14 : 10;
                int storageSlots = (int)(Storage.Food / Owner.AverageFreighterCargoCap);
                int outputSlots  = (int)(Food.NetIncome * AverageFoodExportTurns / Owner.AverageFreighterCargoCap);
                return (storageSlots + outputSlots).Clamped(min, maxSlots);
            }
        }

        public int ProdExportSlots
        {
            get
            {
                if (TradeBlocked || !ExportProd)
                    return 0;

                if (ManualProdExportSlots > 0 && Owner == Universe.Player)
                    return ManualProdExportSlots;

                int min = Storage.ProdRatio > 0.5f ? 2 : 1;
                int maxSlots = IsCybernetic ? 6 : 5;
                switch (CType)
                {
                    case ColonyType.Industrial: maxSlots += 7; break;
                    case ColonyType.Core:       maxSlots += 5; break;
                    case ColonyType.Research:   maxSlots += 3;  break;
                }

                int storageSlots = (int)(Storage.Prod / Owner.AverageFreighterCargoCap);
                int outputSlots  = (int)(Prod.NetIncome * AverageFoodExportTurns / Owner.AverageFreighterCargoCap);

                return (storageSlots + outputSlots).Clamped(min, maxSlots);
            }
        }

        public int ColonistsExportSlots
        {
            get
            {
                if (TradeBlocked || ColonistsTradeState != GoodState.EXPORT)
                    return 0;

                if (ManualColoExportSlots > 0 && Owner == Universe.Player)
                    return ManualColoExportSlots;

                return (int)PopulationBillion;
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

                if (ManualFoodImportSlots > 0 && Owner == Universe.Player)
                    return ManualFoodImportSlots;

                float foodMissing = Storage.Max - FoodHere - IncomingFood;
                foodMissing      += (-Food.NetIncome * AverageFoodImportTurns).LowerBound(0);
                int maxSlots      = ((int)(Universe.GalaxySize) * 4).LowerBound(4) + Owner.NumTradeTreaties;
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

                if (ManualProdImportSlots > 0 && Owner == Universe.Player)
                    return ManualProdImportSlots;

                int maxSlots = ((int)(Universe.GalaxySize) * 4).LowerBound(4) + Owner.NumTradeTreaties;
                if (CType == ColonyType.Industrial
                    || CType == ColonyType.Core
                    || CType == ColonyType.Colony)
                {
                    maxSlots = (int)(maxSlots * 1.5f);
                }

                float averageFreighterCargoCap = Owner.AverageFreighterCargoCap * Owner.FastVsBigFreighterRatio;
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
                    totalProdSlots += ((int)(-Prod.NetIncome * AverageProdImportTurns / averageFreighterCargoCap)).LowerBound(0);

                return (int)totalProdSlots.Clamped(0, maxSlots);
            }
        }

        public int ColonistsImportSlots
        {
            get
            {
                if (TradeBlocked || ColonistsTradeState != GoodState.IMPORT)
                    return 0;

                if (ManualColoImportSlots > 0 && Owner == Universe.Player)
                    return ManualColoImportSlots;

                float slots = 2 / PopulationRatio.LowerBound(0.2f);
                return (int)slots.Clamped(1, 5);
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
            OutgoingFreighters.Remove(ship);
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
                else if (ship.Loyalty != Owner && !Owner.IsTradeTreaty(ship.Loyalty))
                {
                    // cancel trade plan and remove from list if trade treaty was canceled
                    freighters.RemoveAtSwapLast(i);
                    ship.AI.CancelTradePlan(ship.Loyalty.FindNearestRallyPoint(ship.Position));
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
                    maxProdLoad += importPlanet.Prod.NetIncome * eta.UpperBound(importPlanet.TurnsUntilQueueCompleted(1f));
                else
                    maxProdLoad = ProdHere.UpperBound(maxProdLoad);
            }

            return maxProdLoad.Clamped(0f, prodLoadLimit);
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
                if (freighter?.Active == true && freighter.AI.HasTradeGoal(goods))
                   numGoods += freighter.CheckExpectedGoods(goods);
            }

            return numGoods;
        }

        public float ExportGoodsLimit(Goods goods)
        {
            float limit = 0; // it is a multiplier
            switch (goods)
            {
                case Goods.Food:       limit = Storage.Max / NumOutgoingFreightersPickUp(OutgoingFreighters, goods).LowerBound(1); break;
                case Goods.Production: limit = Storage.Max / NumOutgoingFreightersPickUp(OutgoingFreighters, goods).LowerBound(1); break;
                case Goods.Colonists:  limit = Population * 0.2f;                                                                  break;
            }

            return limit;
        }

        public void UpdateAverageFreightTurns(Planet importPlanet, Planet exportPlanet, Goods goods, float startTime)
        {
            if (importPlanet == exportPlanet || startTime == 0)  // startTime is 0 in older saves
                return; // Express import, ignore

            float numTurns = (Universe.StarDate - startTime).LowerBound(0.5f); // not * 10 for turns since we are using 10% of value anyway
            switch (goods)
            {
                case Goods.Food       when importPlanet == this: AverageFoodImportTurns = AverageFoodImportTurns * 0.9f + numTurns; break;
                case Goods.Production when importPlanet == this: AverageProdImportTurns = AverageProdImportTurns * 0.9f + numTurns; break;
                case Goods.Food       when exportPlanet == this: AverageFoodExportTurns = AverageFoodExportTurns * 0.9f + numTurns; break;
                case Goods.Production when exportPlanet == this: AverageProdExportTurns = AverageProdExportTurns * 0.9f + numTurns; break;
            }
        }

        public void SetManualFoodImportSlots(int value)
        {
            ManualFoodImportSlots = value;
        }

        public void SetManualProdImportSlots(int value)
        {
            ManualProdImportSlots = value;
        }

        public void SetManualColoImportSlots(int value)
        {
            ManualColoImportSlots = value;
        }

        public void SetManualFoodExportSlots(int value)
        {
            ManualFoodExportSlots = value;
        }

        public void SetManualProdExportSlots(int value)
        {
            ManualProdExportSlots = value;
        }

        public void SetManualColoExportSlots(int value)
        {
            ManualColoExportSlots = value;
        }

    }
}
