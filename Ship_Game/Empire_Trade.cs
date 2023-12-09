using System;
using Ship_Game.AI;
using Ship_Game.Commands.Goals;
using Ship_Game.Gameplay;
using Ship_Game.Ships;
using System.Collections.Generic;
using System.Linq;
using SDGraphics;
using SDUtils;
using Ship_Game.Data.Serialization;

namespace Ship_Game
{
    using static HelperFunctions;
    public partial class Empire
    {
        [StarData] public bool AutoFreighters;
        [StarData] public bool AutoPickBestFreighter;
        [StarData] public float FastVsBigFreighterRatio { get; private set; } = 0.5f;
        public float TradeMoneyAddedThisTurn { get; private set; }
        public float TotalTradeMoneyAddedThisTurn { get; private set; }
        [StarData] public float AverageFreighterCargoCap { get; private set; } = 20;
        [StarData] public int AverageFreighterFTLSpeed { get; private set; } = 20000;
        [StarData] public float TotalPlanetStorage { get; private set; }
        [StarData] public float AveragePlanetStorage { get; private set; } = 100;

        public int  TotalProdExportSlots { get; private set; }

        int FreighterCapUpperBound => OwnedPlanets.Count * (IsCybernetic ? 2 : 3);
        public int FreighterCap => (int)(AveragePlanetStorage / AverageFreighterCargoCap
            * OwnedPlanets.Count * (IsCybernetic ? 1.2f : 1.8f)).Clamped(1, FreighterCapUpperBound);
        public int FreightersBeingBuilt  => AI.CountGoals(goal => goal is IncreaseFreighters);
        public int MaxFreightersInQueue  => (int)Math.Ceiling((OwnedPlanets.Count / 5f)).Clamped(2, 5);
        public int TotalFreighters       => OwnedShips.Count(s => s?.IsFreighter == true);
        public int AverageTradeIncome    => AllTimeTradeIncome / TurnCount;
        public bool ManualTrade          => isPlayer && !AutoFreighters;
        public float TotalAvgTradeIncome => TotalTradeTreatiesIncome() + AverageTradeIncome;
        public int NumTradeTreaties      => TradeTreaties.Count;

        Array<Relationship> TradeTreaties = new();
        public IReadOnlyList<Relationship> TradeRelations => TradeTreaties;

        void UpdateTradeTreaties()
        {
            var tradeTreaties = new Array<Relationship>();
            foreach (Relationship r in ActiveRelations)
                if (r.Treaty_Trade) tradeTreaties.Add(r);

            TradeTreaties = tradeTreaties;
        }

        public Array<Planet> TradingEmpiresPlanetList()
        {
            var list = new Array<Planet>();
            foreach (Relationship rel in TradeTreaties)
            {
                list.AddRange(rel.Them.OwnedPlanets);
            }
            return list;
        }

        public void TaxGoods(float goods, Planet planet)
        {
            float taxedGoods = 0;
            float taxRate    = data.TaxRate;
            // First - tax the goods if the Mercantilism was unlocked
            if (data.Traits.TaxGoods)
                taxedGoods = goods * taxRate;

            // Then, add credits per goods if the race has the Mercantile trait
            taxedGoods += goods * data.Traits.Mercantile;

            // Finally, add Inter Empire Trade Tariff
            if (this != planet.Owner) 
                taxedGoods += goods * 2f;

            TradeMoneyAddedThisTurn += taxedGoods;
            AllTimeTradeIncome      += (int)taxedGoods;
        }

        // once per turn 
        void DispatchBuildAndScrapFreighters()
        {
            UpdateTradeTreaties();

            // Cybernetic factions never touch Food trade. Filthy Opteris are disgusted by protein-bugs. Ironic.
            if (NonCybernetic)
                DispatchOrBuildFreighters(Goods.Food, OwnedPlanets, false);

            float popRatio              = TotalPopBillion / MaxPopBillion;
            float productionFirstChance = popRatio * 100;
            if (Random.RollDice(productionFirstChance))
            {
                DispatchOrBuildFreighters(Goods.Production, OwnedPlanets, false);
                DispatchOrBuildFreighters(Goods.Colonists, OwnedPlanets, false);
            }
            else
            {
                DispatchOrBuildFreighters(Goods.Colonists, OwnedPlanets, false);
                DispatchOrBuildFreighters(Goods.Production, OwnedPlanets, false);
            }

            if (!isPlayer || Universe.P.AllowPlayerInterTrade)
            {
                var interTradePlanets = TradingEmpiresPlanetList();
                if (interTradePlanets.Count > 0)
                {
                    // export stuff to Empires which have trade treaties with us
                    if (NonCybernetic)
                        DispatchOrBuildFreighters(Goods.Food, interTradePlanets, true);

                    DispatchOrBuildFreighters(Goods.Production, interTradePlanets, true);
                }
            }

            UpdateFreighterTimersAndScrap();
        }

        void UpdateFreighterTimersAndScrap()
        {
            if (ManualTrade)
                return;

            Ship[] ownedFreighters = OwnedShips.Filter(s => s.IsFreighter);
            for (int i = 0; i < ownedFreighters.Length; ++i)
            {
                Ship freighter = ownedFreighters[i];
                if (freighter.IsIdleFreighter)
                {
                    freighter.TradeTimer -= Universe.P.TurnTimer;
                    if (freighter.TradeTimer < 0)
                    {
                        freighter.AI.OrderScrapShip();
                        freighter.TradeTimer = Universe.P.TurnTimer * 60;
                    }
                }
                else
                {
                    freighter.TradeTimer = Universe.P.TurnTimer * 60;
                }
            }
        }

        public bool TryDispatchGoodsSupplyToStation(Goods goods, Ship targetStation, out ExportPlanetAndFreighter exportAndFreighter)
        {
            exportAndFreighter = default;
            // TODO: maybe use IEnumerable generators for these?
            Planet[] exportingPlanets = OwnedPlanets.Filter(p => p.FreeGoodsExportSlots(goods) > 0);
            if (exportingPlanets.Length == 0)
                return false;

            Ship[] idleFreighters = GetIdleFreighters(interTrade: false);
            if (idleFreighters.Length == 0) // Need trade for auto trade but no freighters found
                return false;

            return GetTradeParameters(goods, idleFreighters, targetStation, exportingPlanets, out exportAndFreighter);
        }

        void DispatchOrBuildFreighters(Goods goods, Array<Planet> importPlanetList, bool interTrade)
        {
            // Order importing planets to balance freighters distribution
            Planet[] importingPlanets = importPlanetList.Filter(p => p.FreeGoodsImportSlots(goods) > 0);
            if (importingPlanets.Length == 0)
                return;

            // TODO: maybe use IEnumerable generators for these?
            Planet[] exportingPlanets = OwnedPlanets.Filter(p => p.FreeGoodsExportSlots(goods) > 0);
            if (exportingPlanets.Length == 0)
                return;

            Ship[] idleFreighters = GetIdleFreighters(interTrade);
            if (idleFreighters.Length == 0) // Need trade for auto trade but no freighters found
            {
                if (!interTrade) 
                    BuildFreighter();

                return;
            }

            importingPlanets.Sort(p => p.GetCachedIncomingCargo(goods));

            for (int i = 0; i < importingPlanets.Length; i++)
            {
                Planet importPlanet = importingPlanets[i];
                // Check export planets
                if (GetTradeParameters(goods, idleFreighters, importPlanet, exportingPlanets, out ExportPlanetAndFreighter exportAndFreighter))
                {
                    Planet exportPlanet = exportAndFreighter.Planet;
                    Ship freighter      = exportAndFreighter.Freighter;
                    freighter.RefreshTradeRoutes();
                    freighter.AI.SetupFreighterPlan(exportPlanet, importPlanet, goods);
                    idleFreighters.Remove(freighter, out idleFreighters);

                    // Remove the export planet from the exporting list if no more export slots left
                    if (exportPlanet.FreeGoodsExportSlots(goods) == 0)
                        exportingPlanets.Remove(exportPlanet, out exportingPlanets);
                }
            }
        }

        Ship[] GetIdleFreighters(bool interTrade)
        {
            return interTrade ? OwnedShips.Filter(s => s.IsIdleFreighter && s.AllowInterEmpireTrade)
                              : OwnedShips.Filter(s => s.IsIdleFreighter); 
        }

        bool GetTradeParameters(Goods goods, Ship[] freighterList, GameObject target, 
            Planet[] exportPlanets,  out ExportPlanetAndFreighter exportAndFreighter)
        {
            var potentialRoutes = new Map<int, ExportPlanetAndFreighter>();
            for (int i = 0; i < freighterList.Length; i++)
            {
                Ship freighter = freighterList[i];
                if ((target is Planet importPlanet && freighter.TryGetBestTradeRoute(goods, exportPlanets, importPlanet, out Ship.ExportPlanetAndEta exportAndEta)
                    || target is Ship targetShip && freighter.TryGetBestTradeRoute(goods, exportPlanets, targetShip, out exportAndEta))
                    && !potentialRoutes.ContainsKey(exportAndEta.Eta))
                {
                    potentialRoutes.Add(exportAndEta.Eta, new ExportPlanetAndFreighter(exportAndEta.Planet, freighter));
                }
            }

            exportAndFreighter = default;
            if (potentialRoutes.Count == 0)
                return false;

            int shortest       = potentialRoutes.FindMinKey(e => e);
            exportAndFreighter = potentialRoutes[shortest];
            return true;
        }

        public struct ExportPlanetAndFreighter
        {
            public readonly Planet Planet;
            public readonly Ship Freighter;

            public ExportPlanetAndFreighter(Planet exportPlanet, Ship freighter)
            {
                Planet    = exportPlanet;
                Freighter = freighter;
            }
        }

        void BuildFreighter()
        {
            if (ManualTrade)
                return;

            int beingBuilt = FreightersBeingBuilt;
            if (beingBuilt < MaxFreightersInQueue && (TotalFreighters + beingBuilt) < FreighterCap)
                AI.AddGoal(new IncreaseFreighters(this));
        }

        int NumFreightersTrading(Goods goods)
        {
            return OwnedShips.Count(s => s?.IsFreighter == true && !s.IsIdleFreighter && s.AI.HasTradeGoal(goods));
        }

        // centralized method to deal with freighter priority ratio (fast or big)
        public void IncreaseFastVsBigFreighterRatio(FreighterPriority reason)
        {
            float ratioDiff = 0;
            switch (reason)
            {
                case FreighterPriority.TooSmall:         ratioDiff = -0.01f;  break;
                case FreighterPriority.TooBig:           ratioDiff = +0.01f;  break;
                case FreighterPriority.TooSlow:          ratioDiff = +0.02f;  break;
                case FreighterPriority.ExcessCargoLeft:  ratioDiff = +0.005f; break;
                case FreighterPriority.UnloadedAllCargo: ratioDiff = -0.02f;  break;
            }

            IncreaseFastVsBigFreighterRatio(ratioDiff);
        }

        public void AffectFastVsBigFreighterByEta(Planet importPlanet, Goods goods, float eta)
        {
            bool freighterTooSlow;
            switch (goods)
            {
                case Goods.Food: freighterTooSlow = (importPlanet.FoodHere - importPlanet.Food.NetIncome * eta) < 0; break;
                default: freighterTooSlow         = eta > 50;                                                        break;
            }

            if (freighterTooSlow)
                IncreaseFastVsBigFreighterRatio(FreighterPriority.TooSlow);
        }

        public void IncreaseFastVsBigFreighterRatio(float amount)
        {
            FastVsBigFreighterRatio = (FastVsBigFreighterRatio + amount).Clamped(0.1f, 1);
        }

        public float TotalTradeTreatiesIncome()
        {
            float total = 0f;
            foreach (Relationship rel in ActiveRelations)
                if (rel.Treaty_Trade) total += rel.TradeIncome(this);
            return total;
        }

        void UpdateTradeIncome()
        {
            TotalTradeMoneyAddedThisTurn = TotalTradeTreatiesIncome() + TradeMoneyAddedThisTurn;
            TradeMoneyAddedThisTurn = 0; // Reset Trade Money for the next turn.
        }

        // FB - Refit some idle freighters to better ones, if unlocked
        public void TriggerFreightersRefit()
        {
            if (ManualTrade || TotalFreighters / (float)FreighterCap <= 0.75f)
                return;

            IShipDesign betterFreighter = ShipBuilder.PickFreighter(this, FastVsBigFreighterRatio);
            if (betterFreighter == null)
                return;

            var ships = GetIdleFreighters(false);
            for (int i = 0; i < ships.Length; i++)
            {
                Ship idleFreighter = ships[i];
                CheckForRefitFreighter(idleFreighter, 25, betterFreighter);
            }
        }

        // Percentage to check if there is better suited freighter model available
        public void CheckForRefitFreighter(Ship freighter, int percentage, IShipDesign betterFreighter = null)
        {
            if (!ManualTrade && Random.RollDice(percentage) && TotalFreighters / (float)FreighterCap > 0.75f)
            {
                if (betterFreighter == null)
                    betterFreighter = ShipBuilder.PickFreighter(this, FastVsBigFreighterRatio);

                if (betterFreighter != null && betterFreighter.Name != freighter.Name)
                    AI.AddGoalAndEvaluate(new RefitShip(freighter, betterFreighter, this));
            }
        }

        public void UpdateAverageFreightFTL(float value)
        {
            AverageFreighterFTLSpeed = (int)ExponentialMovingAverage(AverageFreighterFTLSpeed, value);
        }

        public void UpdateAverageFreightCargoCap(float value)
        {
            AverageFreighterCargoCap = ExponentialMovingAverage(AverageFreighterCargoCap, value).RoundToFractionOf10();
        }

        void UpdatePlanetStorageStats()
        {
            UpdateTotalPlanetStorage();
            UpdateAveragePlanetStorage(TotalPlanetStorage);

        }

        void UpdateTotalPlanetStorage()
        {
            TotalPlanetStorage = OwnedPlanets.Sum(p => p.Storage.Max);
        }

        void UpdateAveragePlanetStorage(float totalStorage)
        {
            AveragePlanetStorage = OwnedPlanets.Count > 0 
                ? ExponentialMovingAverage(AveragePlanetStorage, totalStorage / OwnedPlanets.Count) 
                : 0;
        }

        public float TotalPlanetsTradeValue => OwnedPlanets.Sum(p => p.Level).LowerBound(1);
    }
}
