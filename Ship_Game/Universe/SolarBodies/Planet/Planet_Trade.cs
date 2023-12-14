using System;
using SDGraphics;
using SDUtils;
using Ship_Game.AI;
using Ship_Game.Data.Serialization;
using Ship_Game.Ships;

namespace Ship_Game
{
    using static HelperFunctions;

    public partial class Planet // Created by Fat Bastard
    {
        [StarData] readonly Array<Ship> IncomingFreighters = new();
        [StarData] readonly Array<Ship> OutgoingFreighters = new();

        [StarData] public float AverageFoodImportTurns { get; private set; } = 10; // EMA (90/10) time it took the traded to deliver food
        [StarData] public float AverageProdImportTurns { get; private set; } = 10; // EMA (90/10) time it took the traded to deliver prod
        [StarData] public float AverageFoodExportTurns { get; private set; } = 10; // Turns for the trader to arrive to the *export* planet to pick up food
        [StarData] public float AverageProdExportTurns { get; private set; } = 10; // Turns for the trader to arrive to the *export* planet to pick up prod

        // these are updated once per empire turn
        // TODO: this needs to be refactored into a TradeStatus class where each commodity has its own status
        //       since right now we have 3 sets of variables [Food, Prod, Pop], all doing the same thing
        [StarData] public float IncomingFood { get; private set; }
        [StarData] public float IncomingProd { get; private set; }
        [StarData] public float IncomingPop  { get; private set; }

        public int NumIncomingFreighters => IncomingFreighters.Count;
        public int NumOutgoingFreighters => OutgoingFreighters.Count;

        public int IncomingFoodFreighters { get; private set; }
        public int IncomingProdFreighters { get; private set; }
        public int IncomingColonistsFreighters { get; private set; }

        public int OutgoingFoodFreighters { get; private set; }
        public int OutgoingProdFreighters { get; private set; }
        public int OutGoingColonistsFreighters { get; private set; }

        public int FreeFoodExportSlots     => GetNumFreeSlots(FoodExportSlots, OutgoingFoodFreighters);
        public int FreeProdExportSlots     => GetNumFreeSlots(ProdExportSlots, OutgoingProdFreighters);
        public int FreeColonistExportSlots => GetNumFreeSlots(ColonistsExportSlots, OutGoingColonistsFreighters);

        public int FreeFoodImportSlots     => GetNumFreeSlots(FoodImportSlots, IncomingFoodFreighters);
        public int FreeProdImportSlots     => GetNumFreeSlots(ProdImportSlots, IncomingProdFreighters);
        public int FreeColonistImportSlots => GetNumFreeSlots(ColonistsImportSlots, IncomingColonistsFreighters);

        // # of free slots after deducting active freighter count
        static int GetNumFreeSlots(int totalSlots, int activeFreighters) => Math.Max(totalSlots - activeFreighters, 0);

        public bool TradeBlocked => HasSpacePort && SpaceCombatNearPlanet || !HasSpacePort && !Safe || Quarantine;

        // These values can be set by the player. If 0, the code will determine the slots limit automatically
        [StarData] public int ManualFoodImportSlots { get; set; } 
        [StarData] public int ManualProdImportSlots { get; set; }
        [StarData] public int ManualColoImportSlots { get; set; }
        [StarData] public int ManualFoodExportSlots { get; set; }
        [StarData] public int ManualProdExportSlots { get; set; }
        [StarData] public int ManualColoExportSlots { get; set; }

        public int FoodExportSlots { get; private set; }
        public int ProdExportSlots { get; private set; }
        public int ColonistsExportSlots { get; private set; }

        public int FoodImportSlots { get; private set; }
        public int ProdImportSlots { get; private set; }
        public int ColonistsImportSlots { get; private set; }

        public int NumFreightersPickingUpFood { get; private set; }
        public int NumFreightersPickingUpProd { get; private set; }

        void UpdateIncomingTradeGoods()
        {
            // synchronization point
            // TODO: this might not be needed if Empires are updated after Ships parallel update
            Ship[] incomingFreighters;
            Ship[] outgoingFreighters;
            lock (IncomingFreighters) incomingFreighters = IncomingFreighters.ToArr();
            lock (OutgoingFreighters) outgoingFreighters = OutgoingFreighters.ToArr();

            // these need to be updated before slots update
            IncomingFood = GetTotalCargo(incomingFreighters, Goods.Food);
            IncomingProd = GetTotalCargo(incomingFreighters, Goods.Production);
            IncomingPop  = GetTotalCargo(incomingFreighters, Goods.Colonists);

            IncomingFoodFreighters      = GetActiveFreighterCount(incomingFreighters, Goods.Food);
            IncomingProdFreighters      = GetActiveFreighterCount(incomingFreighters, Goods.Production);
            IncomingColonistsFreighters = GetActiveFreighterCount(incomingFreighters, Goods.Colonists);

            OutgoingFoodFreighters      = GetActiveFreighterCount(outgoingFreighters, Goods.Food);
            OutgoingProdFreighters      = GetActiveFreighterCount(outgoingFreighters, Goods.Production);
            OutGoingColonistsFreighters = GetActiveFreighterCount(outgoingFreighters, Goods.Colonists);

            FoodExportSlots = GetFoodExportSlots();
            ProdExportSlots = GetProdExportSlots();
            ColonistsExportSlots = GetColonistsExportSlots();

            FoodImportSlots = GetFoodImportSlots();
            ProdImportSlots = GetProdImportSlots();
            ColonistsImportSlots = GetColonistsImportSlots();

            NumFreightersPickingUpFood = NumOutgoingFreightersPickUp(outgoingFreighters, Goods.Food);
            NumFreightersPickingUpProd = NumOutgoingFreightersPickUp(outgoingFreighters, Goods.Production);
        }

        void IncreaseOutgoingFreighters(Goods goods)
        {
            switch (goods) 
            {
                case Goods.Food:       OutgoingFoodFreighters++;      break;
                case Goods.Production: OutgoingProdFreighters++;      break;
                case Goods.Colonists:  OutGoingColonistsFreighters++; break;
            }
        }

        int GetFoodExportSlots()
        {
            if (TradeBlocked || !ExportFood)
                return 0;

            if (ManualFoodExportSlots > 0 && Owner == Universe.Player)
                return ManualFoodExportSlots;

            int min = Storage.FoodRatio > 0.75f ? 2 : 1;
            int maxSlots = CType is ColonyType.Agricultural or ColonyType.Colony ? 14 : 10;
            int storageSlots = (int)(Storage.Food / Owner.AverageFreighterCargoCap);
            int outputSlots  = (int)(Food.NetIncome * AverageFoodExportTurns / Owner.AverageFreighterCargoCap);
            return (storageSlots + outputSlots).Clamped(min, maxSlots);
        }

        int GetProdExportSlots()
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

        int GetColonistsExportSlots()
        {
            if (TradeBlocked || ColonistsTradeState != GoodState.EXPORT)
                return 0;

            if (ManualColoExportSlots > 0 && Owner == Universe.Player)
                return ManualColoExportSlots;

            return (int)PopulationBillion;
        }

        int GetFoodImportSlots()
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

            float averageFreighterCargoCap = Owner.AverageFreighterCargoCap;
            int maxSlots = ((int)(Storage.Max / averageFreighterCargoCap)).LowerBound(1);
            float foodMissing = Storage.Max - FoodHere - IncomingFood;

            foodMissing += (-Food.NetIncome * AverageFoodImportTurns).LowerBound(0);
            int foodSlots = foodMissing < 5 ? 0 : (foodMissing / Owner.AverageFreighterCargoCap).RoundUpTo(1) + 1;

            return foodSlots.Clamped(0, maxSlots);
        }

        int GetProdImportSlots()
        {
            if (TradeBlocked || !ImportProd)
                return 0;

            if (ManualProdImportSlots > 0 && Owner == Universe.Player)
                return ManualProdImportSlots;

            float averageFreighterCargoCap = Owner.AverageFreighterCargoCap;
            int maxSlots = ((int)(Storage.Max / averageFreighterCargoCap)).LowerBound(1);
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

        int GetColonistsImportSlots()
        {
            if (TradeBlocked || ColonistsTradeState != GoodState.IMPORT)
                return 0;

            if (ManualColoImportSlots > 0 && Owner == Universe.Player)
                return ManualColoImportSlots;

            float slots = 2 / PopulationRatio.LowerBound(0.2f);
            return (int)slots.Clamped(1, 5);
        }

        // # of Freighters on the way to pick up from us
        int NumOutgoingFreightersPickUp(Ship[] outgoing, Goods goods)
        {
            int numFreighters = 0;
            for (int i = 0; i < outgoing.Length; i++)
            {
                Ship freighter = outgoing[i];
                if (freighter.Active &&
                    freighter.AI.FindGoal(ShipAI.Plan.PickupGoods, out ShipAI.ShipGoal goal) &&
                    goal.Trade.Goods == goods)
                    ++numFreighters;
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
            lock (IncomingFreighters)
                IncomingFreighters.AddUniqueRef(ship);
        }

        public void AddToOutgoingFreighterList(Ship ship, Goods goods)
        {
            IncreaseOutgoingFreighters(goods);
            lock (OutgoingFreighters)
                OutgoingFreighters.AddUniqueRef(ship);
        }

        public void RemoveFromIncomingFreighterList(Ship ship)
        {
            lock (IncomingFreighters)
                IncomingFreighters.RemoveRef(ship);
        }

        public void RemoveFromOutgoingFreighterList(Ship ship)
        {
            lock (OutgoingFreighters)
                OutgoingFreighters.RemoveRef(ship);
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
        public float ExportableFood(Planet exportPlanet, Ship targetStation, float eta)
        {
            if (!ExportFood)
                return 0;

            float foodConsumption = targetStation.TotalRefining * GlobalStats.Defaults.MiningStationFoodPerOneRefining;
            float foodLeftUntilEta = (targetStation.GetFood() - foodConsumption*eta).LowerBound(0);
            float maxFoodLoad = (targetStation.IsMiningStation ? targetStation.MiningShipCargoSpaceMax : targetStation.CargoSpaceMax) - foodLeftUntilEta;
            float foodLoadLimit = exportPlanet.ExportGoodsLimit(Goods.Food);

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

        public float ExportableProd(Planet exportPlanet, Ship targetStation, float eta)
        {
            if (!ExportProd)
                return 0;

            float prodConsumpsion = targetStation.IsResearchStation
                ? targetStation.ResearchPerTurn * GlobalStats.Defaults.ResearchStationProductionPerResearch
                : targetStation.TotalRefining * GlobalStats.Defaults.MiningStationFoodPerOneRefining;

            float prodLeftUntilEta = (targetStation.GetProduction() - prodConsumpsion*eta).LowerBound(0);
            float maxProdLoad = (targetStation.IsMiningStation ? targetStation.MiningShipCargoSpaceMax : targetStation.CargoSpaceMax) - prodLeftUntilEta;
            float prodLoadLimit = exportPlanet.ExportGoodsLimit(Goods.Production);
            return maxProdLoad.Clamped(0f, prodLoadLimit);
        }

        public float ExportablePop(Planet exportPlanet, Planet importPlanet)
        {
            if (importPlanet.IsStarving)
                return 0;

            float maxPopLoad = importPlanet.MaxPopulation - importPlanet.Population - importPlanet.IncomingPop;
            return maxPopLoad.Clamped(0, exportPlanet.MaxPopulation * 0.2f);
        }

        // # of active freighters trading specified goods
        static int GetActiveFreighterCount(Ship[] freighterList, Goods goods)
        {
            // using raw loop for perf, TODO: we need a way to do these without looping
            int numActiveFreighters = 0;
            for (int i = 0; i < freighterList.Length; ++i)
                if (freighterList[i].AI.HasTradeGoal(goods))
                    ++numActiveFreighters;
            return numActiveFreighters;
        }

        // total (cached) amount of cargo of specified type incoming via freighters
        public float GetCachedIncomingCargo(Goods goods)
        {
            if (goods == Goods.Food) return IncomingFood;
            if (goods == Goods.Production) return IncomingProd;
            if (goods == Goods.Colonists) return IncomingPop;
            return 0f;
        }

        float GetTotalCargo(Ship[] freighterList, Goods goods)
        {
            float totalCargo = 0f;
            // using raw loop for perf, TODO: we need a way to do these without looping
            for (int i = 0; i < freighterList.Length; ++i)
            {
                Ship freighter = freighterList[i];
                if (freighter.Active)
                    totalCargo += freighter.GetGoodsBeingDroppedOff(goods);
            }
            return totalCargo;
        }

        public float ExportGoodsLimit(Goods goods)
        {
            float limit = 0; // it is a multiplier
            switch (goods)
            {
                case Goods.Food:       limit = (Storage.Max / NumFreightersPickingUpFood.LowerBound(1)).UpperBound(FoodHere); break;
                case Goods.Production: limit = (Storage.Max / NumFreightersPickingUpProd.LowerBound(1)).UpperBound(ProdHere); break;
                case Goods.Colonists:  limit = Population * 0.2f;                                                             break;
            }

            return limit;
        }

        public void UpdateAverageFreightTurns(Planet importPlanet, Planet exportPlanet, Goods goods, float startTime)
        {
            if (importPlanet == exportPlanet || startTime == 0)  // startTime is 0 in older saves
                return; // Express import, ignore

            float numTurns = (Universe.StarDate - startTime).LowerBound(0.5f) * 10;
            switch (goods)
            {
                case Goods.Food       when importPlanet == this: AverageFoodImportTurns = ExponentialMovingAverage(AverageFoodImportTurns, numTurns); break;
                case Goods.Production when importPlanet == this: AverageProdImportTurns = ExponentialMovingAverage(AverageProdImportTurns, numTurns); break;
                case Goods.Food       when exportPlanet == this: AverageFoodExportTurns = ExponentialMovingAverage(AverageFoodExportTurns, numTurns); break;
                case Goods.Production when exportPlanet == this: AverageProdExportTurns = ExponentialMovingAverage(AverageProdExportTurns, numTurns); break;
            }
        }
    }
}
