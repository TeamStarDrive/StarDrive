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

        public int FreighterCap => (int)(AveragePlanetStorage / AverageFreighterCargoCap * OwnedPlanets.Count).Clamped(1, OwnedPlanets.Count*10);
        public int FreightersBeingBuilt  => AI.CountGoals(goal => goal is IncreaseFreighters);
        public int MaxFreightersInQueue  => (int)Math.Ceiling((OwnedPlanets.Count / 5f)).Clamped(2, 5);
        public int TotalFreighters       => OwnedShips.Count(s => s?.IsFreighter == true);
        public int AverageTradeIncome    => AllTimeTradeIncome / TurnCount;
        public bool ManualTrade          => isPlayer && !AutoFreighters;
        public float TotalAvgTradeIncome => TotalTradeTreatiesIncome() + AverageTradeIncome;
        public bool EconomicSafeToBuildFreighter => AI.CreditRating >= 0.4;
        public int TotalLevelsOfPirateFactionsAtWar => Universe.PirateFactions.Sum(e => IsAtWarWith(e) ? e.Pirates.Level : 0);

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

        // once per turn with 3 passes if possible
        void DispatchBuildAndScrapFreighters()
        {
            UpdateTradeTreaties();
            TradeState tradeState = new(this, false);
            for (int i = 1; i <= 3; i++)
            {
                if (tradeState.NoFreeFreighters)
                    break;

                if (NonCybernetic)
                    DispatchOrBuildFreighters(Goods.Food, OwnedPlanets, false, ref tradeState);

                float popRatio = TotalPopBillion / MaxPopBillion;
                float productionFirstChance = popRatio * (NonCybernetic ? 200 : 300);
                if (Random.RollDice(productionFirstChance))
                {
                    DispatchOrBuildFreighters(Goods.Production, OwnedPlanets, false, ref tradeState);
                    DispatchOrBuildFreighters(Goods.Colonists, OwnedPlanets, false, ref tradeState);
                }
                else
                {
                    DispatchOrBuildFreighters(Goods.Colonists, OwnedPlanets, false, ref tradeState);
                    DispatchOrBuildFreighters(Goods.Production, OwnedPlanets, false, ref tradeState);
                }

                tradeState.UpdatePlanetsTradeGoods();
            }

            // Cybernetic factions never touch Food trade. Filthy Opteris are disgusted by protein-bugs. Ironic.
            if (tradeState.HasFreeFreighters && (!isPlayer || Universe.P.AllowPlayerInterTrade))
            {
                Ship[] idleFrieghters = tradeState.IdleFreighters.ToArr();
                tradeState = new(this, true);
                tradeState.SetIdleFreighters(idleFrieghters);
                var interTradePlanets = TradingEmpiresPlanetList();
                if (interTradePlanets.Count > 0)
                {
                    // export stuff to Empires which have trade treaties with us
                    if (NonCybernetic)
                        DispatchOrBuildFreighters(Goods.Food, interTradePlanets, true, ref tradeState);

                    DispatchOrBuildFreighters(Goods.Production, interTradePlanets, true, ref tradeState);
                }
            }

            UpdateFreighterTimersAndScrap();
        }

        struct TradeState
        {
            readonly bool InterTrade;
            public Ship[] IdleFreighters {get; private set; }
            public EmpireIdleFreighters State { get; private set; }
            bool BuildFreighterRequested;
            readonly Empire Owner;
            HashSet<Planet> PlanetsNeedUpdate = new();
            public bool HasImportingFoodPlanets { get; private set; } = true;
            public bool HasImportingProductionPlanets { get; private set; } = true;
            public bool HasImportingColonistsPlanets { get; private set; } = true;
            public bool HasExportingFoodPlanets { get; private set; } = true;
            public bool HasExportingProductionPlanets { get; private set; } = true;
            public bool HasExportingColonistsPlanets { get; private set; } = true;

            public TradeState(Empire owner,  bool interTrade)
            {
                IdleFreighters = new Ship[0];
                State = EmpireIdleFreighters.Fetch;
                InterTrade = interTrade;
                Owner = owner;
            }

            public Ship[] FetchIdleFreightersOrBuild()
            {
                if (State == EmpireIdleFreighters.Fetch)
                {
                    IdleFreighters = Owner.GetIdleFreighters(InterTrade);
                    SetIdleFreightesState();
                    if (IdleFreighters.Length == 0)
                        BuildFreighter();
                }

                return IdleFreighters;
            }

            public void SetIdleFreighters(Ship[] freighters)
            {
                IdleFreighters = freighters;
                SetIdleFreightesState();
            }

            public void SetIdleFreightesState()
            {
                if (IdleFreighters.Length > 0)
                    State = EmpireIdleFreighters.SomeIdle;
                else
                    State = EmpireIdleFreighters.None;
            }

            public void BuildFreighter()
            {
                if (!BuildFreighterRequested && !InterTrade)
                {
                    BuildFreighterRequested = true;
                    Owner.BuildFreighter();
                }
            }

            public void AddToPlanetsNeedUpdate(Planet import, Planet export)
            {
                PlanetsNeedUpdate.Add(import);
                PlanetsNeedUpdate.Add(export);
            }

            public void UpdatePlanetsTradeGoods()
            {
                foreach (Planet p in PlanetsNeedUpdate)
                    p.UpdateIncomingTradeGoods();

                PlanetsNeedUpdate = new();
            }

            public void SetNoImportPlanetOf(Goods goods)
            {
                switch (goods)
                {
                    case Goods.Food:       HasImportingFoodPlanets       = false; break;
                    case Goods.Production: HasImportingProductionPlanets = false; break;
                    case Goods.Colonists:  HasImportingColonistsPlanets  = false; break;
                }
            }

            public void SetNoExportPlanetOf(Goods goods)
            {
                switch (goods)
                {
                    case Goods.Food:       HasExportingFoodPlanets       = false; break;
                    case Goods.Production: HasExportingProductionPlanets = false; break;
                    case Goods.Colonists:  HasExportingColonistsPlanets  = false; break;
                }
            }

            public bool HasImportPlanetOf(Goods goods)
            {
                return goods switch
                {
                    Goods.Food       => HasImportingFoodPlanets,
                    Goods.Production => HasImportingProductionPlanets,
                    Goods.Colonists  => HasImportingColonistsPlanets,
                };
            }

            public bool HasExportPlanetOf(Goods goods)
            {
                return goods switch
                {
                    Goods.Food       => HasExportingFoodPlanets,
                    Goods.Production => HasExportingProductionPlanets,
                    Goods.Colonists  => HasExportingColonistsPlanets,
                };
            }

            public bool NoFreeFreighters => State == EmpireIdleFreighters.None;
            public bool HasFreeFreighters => State == EmpireIdleFreighters.SomeIdle;
            public bool ShouldFetchIdleFreighters => State == EmpireIdleFreighters.Fetch;
        }

        enum EmpireIdleFreighters
        {
            Fetch,
            SomeIdle,
            None,
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
                        ResetTradeTimer(freighter);
                    }
                }
                else
                {
                    ResetTradeTimer(freighter);
                }
            }

            void ResetTradeTimer(Ship freighter)
            {
                freighter.TradeTimer = Universe.P.TurnTimer * 20;
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

        void DispatchOrBuildFreighters(Goods goods, Array<Planet> importPlanetList, bool interTrade, ref TradeState state)
        {
            if (state.NoFreeFreighters)
                return;

            // Order importing planets to balance freighters distribution
            Planet[] importingPlanets = new Planet[0]; 
            if (state.HasImportPlanetOf(goods))
            {
                importingPlanets = importPlanetList.Filter(p => p.FreeGoodsImportSlots(goods) > 0);
                if (importingPlanets.Length == 0)
                {
                    state.SetNoImportPlanetOf(goods);
                    return;
                }
            }
            else
            {
                return;
            }

            Planet[] exportingPlanets = new Planet[0];
            if (state.HasExportPlanetOf(goods))
            {
                // TODO: maybe use IEnumerable generators for these?
                exportingPlanets = OwnedPlanets.Filter(p => p.FreeGoodsExportSlots(goods) > 0);
                if (exportingPlanets.Length == 0)
                {
                    state.SetNoExportPlanetOf(goods);
                    return;
                }
            }
            else
            {
                return;
            }

            Ship[] idleFreighters = state.FetchIdleFreightersOrBuild();
            if (state.NoFreeFreighters) // Need trade for auto trade but no freighters found
                return;

            importingPlanets.Sort(p => p.GetCachedIncomingCargoPriority(goods));

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

                    state.AddToPlanetsNeedUpdate(importPlanet, exportPlanet);
                }
            }

            state.SetIdleFreighters(idleFreighters);
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
            if (ManualTrade || !EconomicSafeToBuildFreighter)
                return;

            int beingBuilt = FreightersBeingBuilt;
            if (beingBuilt < MaxFreightersInQueue && (TotalFreighters + beingBuilt) < FreighterCap)
                AI.AddGoalAndEvaluate(new IncreaseFreighters(this));
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
                case FreighterPriority.TooSlow:          ratioDiff = +0.01f;  break;
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
            // 1.0f = all fast, 0.1f = all big
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

            IShipDesign betterFreighter = ShipBuilder.PickFreighter(this);
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
            if (!ManualTrade && Random.RollDice(percentage) && TotalFreighters / (float)FreighterCap > 0.5f)
            {
                if (betterFreighter == null)
                    betterFreighter = ShipBuilder.PickFreighter(this);

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
