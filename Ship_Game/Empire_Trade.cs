using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Newtonsoft.Json;
using Ship_Game.AI;
using Ship_Game.AI.Budget;
using Ship_Game.Commands.Goals;
using Ship_Game.Debug;
using Ship_Game.Gameplay;
using Ship_Game.Ships;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Xml.Serialization;

namespace Ship_Game
{
    public partial class Empire
    {
        public bool AutoFreighters;
        public bool AutoPickBestFreighter;
        public float FastVsBigFreighterRatio      { get; private set; } = 0.5f;
        public float TradeMoneyAddedThisTurn      { get; private set; }
        public float TotalTradeMoneyAddedThisTurn { get; private set; }

        [XmlIgnore][JsonIgnore] public int FreighterCap         => OwnedPlanets.Count * 3 + ResearchStrategy.ExpansionPriority;
        [XmlIgnore][JsonIgnore] public int FreightersBeingBuilt => EmpireAI.Goals.Count(goal => goal is IncreaseFreighters);
        [XmlIgnore][JsonIgnore] public int MaxFreightersInQueue => 1 + ResearchStrategy.IndustryPriority;
        [XmlIgnore][JsonIgnore] public int TotalFreighters      => OwnedShips.Count(s => s.IsFreighter);
        [XmlIgnore][JsonIgnore] public Ship[] IdleFreighters    => OwnedShips.Filter(s => s.IsIdleFreighter);
        [XmlIgnore][JsonIgnore] public int AverageTradeIncome   => AllTimeTradeIncome / TurnCount;

        public float TotalAvgTradeIncome => TotalTradeTreatiesIncome() + AverageTradeIncome;

        [XmlIgnore][JsonIgnore]
        public Array<Empire> TradeTreaties
        {
            get
            {
                var tradeTreaties = new Array<Empire>();
                foreach (KeyValuePair<Empire, Relationship> kv in Relationships)
                    if (kv.Value.Treaty_Trade)
                        tradeTreaties.Add(kv.Key);
                return tradeTreaties;
            }
        }

        public BatchRemovalCollection<Planet> TradingEmpiresPlanetList()
        {
            var list = new BatchRemovalCollection<Planet>();
            foreach (Empire empire in TradeTreaties)
            {
                foreach (Planet planet in empire.OwnedPlanets)
                    list.Add(planet);
            }

            return list;
        }

        public void TaxGoods(float goods, Planet planet)
        {
            float taxedGoods = 0;
            if (this != planet.Owner) // Inter Empire Trade (very effective)
                taxedGoods += goods;

            taxedGoods              += MercantileTax(goods);
            TradeMoneyAddedThisTurn += taxedGoods;
            AllTimeTradeIncome      += (int)taxedGoods;
        }

        private float MercantileTax(float goods)
        {
            if (data.Traits.Mercantile.LessOrEqual(0))
                return 0;

            return goods * data.Traits.Mercantile * data.TaxRate;
        }

        void DispatchBuildAndScrapFreighters()
        {
            // Cybernetic factions never touch Food trade. Filthy Opteris are disgusted by protein-bugs. Ironic.
            if (NonCybernetic)
                DispatchOrBuildFreighters(Goods.Food, OwnedPlanets);

            DispatchOrBuildFreighters(Goods.Production, OwnedPlanets);
            DispatchOrBuildFreighters(Goods.Colonists, OwnedPlanets);

            var interTradePlanets = TradingEmpiresPlanetList();
            if (interTradePlanets.Count > 0)
            {
                // export stuff to Empires which have trade treaties with us
                if (NonCybernetic)
                    DispatchOrBuildFreighters(Goods.Food, interTradePlanets);

                DispatchOrBuildFreighters(Goods.Production, interTradePlanets);
            }

            UpdateFreighterTimersAndScrap();
        }

        void UpdateFreighterTimersAndScrap()
        {
            if (isPlayer && !AutoFreighters)
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

        private void DispatchOrBuildFreighters(Goods goods, BatchRemovalCollection<Planet> importPlanetList)
        {
            Planet[] importingPlanets = importPlanetList.Filter(p => p.FreeGoodsImportSlots(goods) > 0);
            if (importingPlanets.Length == 0)
                return;

            Planet[] exportingPlanets = OwnedPlanets.Filter(p => p.FreeGoodsExportSlots(goods) > 0);
            if (exportingPlanets.Length == 0)
                return;

            if (IdleFreighters.Length == 0)
            {
                if (FreightersBeingBuilt < MaxFreightersInQueue)
                    BuildFreighter();

                return;
            }

            foreach (Planet importPlanet in importingPlanets)
            {
                // check if the closest freighter has the goods we need
                Ship closestIdleFreighter = OpportunistFreighter(importPlanet, goods);
                if (closestIdleFreighter != null)
                {
                    closestIdleFreighter.AI.SetupFreighterPlan(importPlanet, goods);
                    continue;
                }

                // Check export planets
                Planet exportPlanet = exportingPlanets.FindClosestTo(importPlanet);
                if (exportPlanet == null) // no more exporting planets
                    break;

                closestIdleFreighter = FindClosestIdleFreighter(exportPlanet, goods);
                if (closestIdleFreighter == null) // no more available freighters
                    break;

                if (InterEmpireTradeDistanceOk(closestIdleFreighter, importPlanet, exportPlanet))
                    closestIdleFreighter.AI.SetupFreighterPlan(exportPlanet, importPlanet, goods);
            }
        }

        private bool InterEmpireTradeDistanceOk(Ship freighter, Planet importPlanet, Planet exportPlanet)
        {
            if (importPlanet.Owner == this)
                return true; // only for Inter-Empire Trade

            return freighter.GetAstrogateTimeBetween(importPlanet, exportPlanet) < 40;
        }

        private Ship FindClosestIdleFreighter(Planet planet, Goods goods)
        {
            if (!isPlayer || AutoFreighters)
                return IdleFreighters.FindClosestTo(planet);

            return ClosestIdleFreighterManual(planet, goods);
        }

        private Ship OpportunistFreighter(Planet planet, Goods goods)
        {
            Ship freighter = FindClosestIdleFreighter(planet, goods);
            if (freighter != null && freighter.GetCargo(goods) > 5f)
                return freighter;

            return null;
        }

        private Ship ClosestIdleFreighterManual(Planet planet, Goods goods)
        {
            switch (goods)
            {
                case Goods.Production: return IdleFreighters.FindClosestTo(planet, s => s.TransportingProduction);
                case Goods.Food:       return IdleFreighters.FindClosestTo(planet, s => s.TransportingFood);
                default:               return IdleFreighters.FindClosestTo(planet, s => s.TransportingColonists);
            }
        }

        private void BuildFreighter()
        {
            if (isPlayer && !AutoFreighters)
                return;

            if (FreighterCap > TotalFreighters + FreightersBeingBuilt && MaxFreightersInQueue > FreightersBeingBuilt)
                EmpireAI.Goals.Add(new IncreaseFreighters(this));
        }

        int NumFreightersTrading(Goods goods)
        {
            return OwnedShips.Count(s => s.IsFreighter && !s.IsIdleFreighter && s.AI.HasTradeGoal(goods));
        }

        // centralized method to deal with freighter priority ratio (fast or big)
        public void IncreaseFastVsBigFreighterRatio(FreighterPriority reason)
        {
            float ratioDiff = 0;
            switch (reason)
            {
                case FreighterPriority.TooSmall:         ratioDiff = -0.005f; break;
                case FreighterPriority.TooBig:           ratioDiff = +0.01f;  break;
                case FreighterPriority.TooSlow:          ratioDiff = +0.02f;  break;
                case FreighterPriority.ExcessCargoLeft:  ratioDiff = +0.02f;  break;
                case FreighterPriority.UnloadedAllCargo: ratioDiff = -0.005f; break;
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
            foreach (KeyValuePair<Empire, Relationship> kv in Relationships)
                if (kv.Value.Treaty_Trade) total += kv.Value.TradeIncome();
            return total;
        }

        void UpdateTradeIncome()
        {
            TotalTradeMoneyAddedThisTurn = TotalTradeTreatiesIncome() + TradeMoneyAddedThisTurn;
            TradeMoneyAddedThisTurn = 0; // Reset Trade Money for the next turn.
        }
    }
}
