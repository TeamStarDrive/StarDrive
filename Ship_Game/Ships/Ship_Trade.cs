using System;
using System.Collections.Generic;
using System.Windows.Forms;
using SDGraphics;
using SDUtils;
using Ship_Game.AI;
using Ship_Game.Data.Serialization;

namespace Ship_Game.Ships
{
    public partial class Ship
    {
        public float TradeTimer = 300;
        [StarData] public bool TransportingColonists  { get; set; }
        [StarData] public bool TransportingFood       { get; set; }
        [StarData] public bool TransportingProduction { get; set; }
        [StarData] public bool AllowInterEmpireTrade  { get; set; }
        [StarData] public Array<int> TradeRoutes      { get; private set; } = new();

        public bool IsCandidateForTradingBuild => ShipData.IsCandidateForTradingBuild;
        public bool IsFreighter => ShipData.IsFreighter && !IsMiningShip;

        public bool IsIdleFreighter => IsFreighter
                                       && AI != null
                                       && (!AI.HasPriorityOrder || !AI.HasWayPoints)
                                       && AI.State != AIState.SystemTrader
                                       && AI.State != AIState.Flee
                                       && AI.State != AIState.Refit
                                       && AI.State != AIState.Scrap
                                       && AI.State != AIState.Scuttle
                                       && AI.State != AIState.Resupply
                                       && AI.State != AIState.Mining;

        public bool AddTradeRoute(Planet planet)
        {
            if (planet.Owner == Loyalty
                || Loyalty.IsTradeTreaty(planet.Owner)
                || planet.IsMineable
                || planet.IsResearchable)
            {
                TradeRoutes.AddUnique(planet.Id);
                return true;
            }

            return false;
        }

        public bool CanTransportGoodsType(Goods goods)
        {
            if (!Loyalty.ManualTrade)
                return true;

            switch (goods)
            {
                default:
                case Goods.Food:       return TransportingFood;
                case Goods.Production: return TransportingProduction;
                case Goods.Colonists:  return TransportingColonists;
            }
        }

        public bool TryGetBestTradeRoute(Goods goods, Planet[] exportPlanets, Ship targetStation, out ExportPlanetAndEta exportAndEta)
        {
            exportAndEta = default;
            if (!CanTransportGoodsType(goods) || !InTradingZones(targetStation))
                return false;

            var potentialRoutes = new Map<int, Planet>();
            for (int i = 0; i < exportPlanets.Length; i++)
            {
                Planet exportPlanet = exportPlanets[i];
                int eta = (int)(GetAstrogateTimeTo(exportPlanet) + GetAstrogateTimeBetween(exportPlanet, targetStation));
                if (InTradingZones(exportPlanet) && !potentialRoutes.ContainsKey(eta))
                    potentialRoutes.Add(eta, exportPlanet);
            }

            if (potentialRoutes.Keys.Count == 0)
                return false;

            int fastest = potentialRoutes.FindMinKey(d => d);
            Planet bestExport = potentialRoutes[fastest];
            exportAndEta = new ExportPlanetAndEta(bestExport, fastest);
            return true;
        }

        public bool TryGetBestTradeRoute(Goods goods, Planet[] exportPlanets, Planet importPlanet, out ExportPlanetAndEta exportAndEta)
        {
            exportAndEta = default;
            if (!CanTransportGoodsType(goods) || !InTradingZones(importPlanet))
                return false;

            var potentialRoutes = new Map<int, Planet>();
            if (GetCargo(goods) >= CargoSpaceMax * 0.25f)
            {
                int eta = (int)GetAstrogateTimeTo(importPlanet);
                if (TradeDistanceOk(importPlanet, eta))
                    potentialRoutes.Add(eta, importPlanet); // import planet since there is not export planet.
            }

            for (int i = 0; i < exportPlanets.Length; i++)
            {
                Planet exportPlanet = exportPlanets[i];
                if (InTradingZones(exportPlanet))
                {
                    int eta = (int)(GetAstrogateTimeTo(exportPlanet) + GetAstrogateTimeBetween(exportPlanet, importPlanet));
                    if (!potentialRoutes.ContainsKey(eta) && TradeDistanceOk(importPlanet, eta))
                        potentialRoutes.Add(eta, exportPlanet);
                }
            }

            if (potentialRoutes.Keys.Count == 0)
                return false;

            int fastest       = potentialRoutes.FindMinKey(d => d);
            Planet bestExport = potentialRoutes[fastest];
            exportAndEta      = new ExportPlanetAndEta(bestExport, fastest);
            return true;
        }

        public struct ExportPlanetAndEta
        {
            public readonly Planet Planet;
            public readonly int Eta;

            public ExportPlanetAndEta(Planet exportPlanet, int eta)
            {
                Planet = exportPlanet;
                Eta    = eta;
            }
        }

        // limit eta for Inter Empire trade
        bool TradeDistanceOk(Planet importPlanet, int eta) =>  importPlanet.Owner == Loyalty || eta <= 30;

        public void RemoveTradeRoute(Planet planet)
        {
            TradeRoutes.Remove(planet.Id);
        }

        public void RefreshTradeRoutes()
        {
            if (!Loyalty.isPlayer)
                return; // Trade routes are available only for players

            foreach (int planetId in TradeRoutes)
            {
                Planet planet = Universe.GetPlanet(planetId);
                if (planet.Owner == Loyalty)
                    continue;

                if (planet.Owner == null || !Loyalty.IsTradeTreaty(planet.Owner))
                    RemoveTradeRoute(planet);
            }
        }

        public bool IsValidTradeRoute(Planet planet)
        {
            // need at least 2 routes or AO for it to filter
            return TradeRoutes.IsEmpty || TradeRoutes.Contains(planet.Id);
        }

        public bool InTradingZones(Ship targetStation)
        {
            if (!Loyalty.isPlayer)
                return true; // only player ships can have trade AO or trade routes

            bool tetheredInZones = false;
            if (targetStation.IsTethered)
            {
                Planet p = targetStation.GetTether();
                tetheredInZones = InTradingZones(p);
            }

            return tetheredInZones
                || AreaOfOperation.NotEmpty && InsideAreaOfOperation(targetStation.Position)
                || AreaOfOperation.IsEmpty && TradeRoutes?.Count == 0;
        }

        public bool InTradingZones(Planet planet)
        {
            if (!Loyalty.isPlayer)
                return true; // only player ships can have trade AO or trade routes

            if (TradeRoutes?.Count >= 2 && AreaOfOperation.NotEmpty) // ship has both AO trade routes, so check this or that.
                return IsValidTradeRoute(planet) || InsideAreaOfOperation(planet.Position);

            return IsValidTradeRoute(planet) && InsideAreaOfOperation(planet.Position);
        }

        public void DownloadTradeRoutes(Array<int> tradeRoutes)
        {
            TradeRoutes = tradeRoutes.Clone();
        }

        // # of goods that are being dropped off towards some destination
        // 0 if no drop-off goal is present
        public float GetGoodsBeingDroppedOff(Goods goods)
        {
            if (AI.FindGoal(ShipAI.Plan.DropOffGoods, out _))
                return GetCargo(goods);
            return 0;
        }

        public bool InTradeBlockade => IsResearchStation && HealthPercent < 0.8f;
    }
}
