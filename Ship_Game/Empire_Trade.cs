﻿using System;
using Ship_Game.AI;
using Ship_Game.Commands.Goals;
using Ship_Game.Gameplay;
using Ship_Game.Ships;
using System.Collections.Generic;
using System.Linq;

namespace Ship_Game
{
    public partial class Empire
    {
        public bool AutoFreighters;
        public bool AutoPickBestFreighter;
        public float FastVsBigFreighterRatio      { get; private set; } = 0.5f;
        public float TradeMoneyAddedThisTurn      { get; private set; }
        public float TotalTradeMoneyAddedThisTurn { get; private set; }
        public float AverageFreighterCargoCap     { get; private set; } = 10;
        public int AverageFreighterFTLSpeed       { get; private set; } = 20000;
        public int  TotalProdExportSlots          { get; private set; }

        public int FreighterCap          => OwnedPlanets.Count * 3 + Research.Strategy.ExpansionPriority;
        public int FreightersBeingBuilt  => EmpireAI.Goals.Count(goal => goal is IncreaseFreighters);
        public int MaxFreightersInQueue => (int)(Math.Ceiling(2 * Research.Strategy.IndustryRatio));
        public int TotalFreighters       => OwnedShips.Count(s => s?.IsFreighter == true);
        public int AverageTradeIncome    => AllTimeTradeIncome / TurnCount;
        public bool ManualTrade          => isPlayer && !AutoFreighters;
        public float TotalAvgTradeIncome => TotalTradeTreatiesIncome() + AverageTradeIncome;
        public int NumTradeTreaties      => TradeTreaties.Count;

        Array<OurRelationsToThem> TradeTreaties = new Array<OurRelationsToThem>();
        public IReadOnlyList<OurRelationsToThem> TradeRelations => TradeTreaties;

        void UpdateTradeTreaties()
        {
            var tradeTreaties = new Array<OurRelationsToThem>();
            foreach (OurRelationsToThem r in ActiveRelations)
                if (r.Rel.Treaty_Trade)
                    tradeTreaties.Add(r);

            TradeTreaties = tradeTreaties;
        }

        public BatchRemovalCollection<Planet> TradingEmpiresPlanetList()
        {
            var list = new BatchRemovalCollection<Planet>();
            foreach ((Empire them, Relationship rel) in TradeTreaties)
            {
                list.AddRange(them.OwnedPlanets);
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
                taxedGoods += goods * 0.5f;

            TradeMoneyAddedThisTurn += taxedGoods;
            AllTimeTradeIncome      += (int)taxedGoods;
        }

        void DispatchBuildAndScrapFreighters()
        {
            UpdateTradeTreaties();

            // Cybernetic factions never touch Food trade. Filthy Opteris are disgusted by protein-bugs. Ironic.
            if (NonCybernetic)
                DispatchOrBuildFreighters(Goods.Food, OwnedPlanets, false);

            float popRatio              = TotalPopBillion / MaxPopBillion;
            float productionFirstChance = popRatio * 100;
            if (RandomMath.RollDice(productionFirstChance))
            {
                DispatchOrBuildFreighters(Goods.Production, OwnedPlanets, false);
                DispatchOrBuildFreighters(Goods.Colonists, OwnedPlanets, false);
            }
            else
            {
                DispatchOrBuildFreighters(Goods.Colonists, OwnedPlanets, false);
                DispatchOrBuildFreighters(Goods.Production, OwnedPlanets, false);
            }

            var interTradePlanets = TradingEmpiresPlanetList();
            if (interTradePlanets.Count > 0)
            {
                // export stuff to Empires which have trade treaties with us
                if (NonCybernetic)
                    DispatchOrBuildFreighters(Goods.Food, interTradePlanets, true);

                DispatchOrBuildFreighters(Goods.Production, interTradePlanets, true);
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
                    freighter.TradeTimer -= GlobalStats.TurnTimer;
                    if (freighter.TradeTimer < 0)
                    {
                        freighter.AI.OrderScrapShip();
                        freighter.TradeTimer = GlobalStats.TurnTimer * 60;
                    }
                }
                else
                {
                    freighter.TradeTimer = GlobalStats.TurnTimer * 60;
                }
            }
        }

        void DispatchOrBuildFreighters(Goods goods, BatchRemovalCollection<Planet> importPlanetList, bool interTrade)
        {
            // Order importing planets to balance freighters distribution
            Planet[] importingPlanets = importPlanetList.Filter(p => p.FreeGoodsImportSlots(goods) > 0)
                                                        .OrderBy(p => p.FreighterTraffic(p.IncomingFreighters, goods)).ToArray();
            if (importingPlanets.Length == 0)
                return;

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

        bool GetTradeParameters(Goods goods, Ship[] freighterList, Planet importPlanet, 
            Planet[] exportPlanets,  out ExportPlanetAndFreighter exportAndFreighter)
        {
            var potentialRoutes = new Map<int, ExportPlanetAndFreighter>();
            for (int i = 0; i < freighterList.Length; i++)
            {
                Ship freighter = freighterList[i];
                if (freighter.TryGetBestTradeRoute(goods, exportPlanets, importPlanet, out Ship.ExportPlanetAndEta exportAndEta)
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

        struct ExportPlanetAndFreighter
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
            if (ManualTrade || FreightersBeingBuilt >= MaxFreightersInQueue)
                return;

            if (FreighterCap > TotalFreighters + FreightersBeingBuilt && MaxFreightersInQueue >= FreightersBeingBuilt)
                EmpireAI.Goals.Add(new IncreaseFreighters(this));
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
            foreach ((Empire them, Relationship rel) in ActiveRelations)
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
            if (ManualTrade)
                return;

            Ship betterFreighter = ShipBuilder.PickFreighter(this, FastVsBigFreighterRatio);
            if (betterFreighter == null)
                return;

            foreach (Ship idleFreighter in GetIdleFreighters(false))
                CheckForRefitFreighter(idleFreighter, 20, betterFreighter);
        }

        // Percentage to check if there is better suited freighter model available
        public void CheckForRefitFreighter(Ship freighter, int percentage, Ship betterFreighter = null)
        {
            if (ManualTrade || !RandomMath.RollDice(percentage))
                return;

            if (betterFreighter == null)
                 betterFreighter = ShipBuilder.PickFreighter(this, FastVsBigFreighterRatio);

            if (betterFreighter != null && betterFreighter.Name != freighter.Name)
                GetEmpireAI().Goals.Add(new RefitShip(freighter, betterFreighter.Name, this));
        }

        public void UpdateAverageFreightFTL(float value)
        {
            AverageFreighterFTLSpeed = (int)(AverageFreighterFTLSpeed * 0.9f + value * 0.1f);
        }

        public void UpdateAverageFreightCargoCap(float value)
        {
            AverageFreighterCargoCap = (AverageFreighterCargoCap * 0.9f + value * 0.1f).RoundToFractionOf10();
        }

        public void SetAverageFreighterCargoCap(float value)
        {
            AverageFreighterCargoCap = value.LowerBound(10);
        }

        public void SetAverageFreighterFTLSpeed(int value)
        {
            AverageFreighterFTLSpeed = value.LowerBound(5000);
        }

        public float TotalPlanetsTradeValue => OwnedPlanets.Sum(p => p.Level).LowerBound(1);
    }
}
