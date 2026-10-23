using System;
using Ship_Game.Ships;
using SDGraphics;

namespace Ship_Game.AI
{
    public sealed partial class ShipAI
    {
        public void DoPickupGoodsForStation(FixedSimTime timeStep, ShipGoal g)
        {
            Planet exportPlanet = g.Trade.ExportFrom;
            if (!CanSupplyStation(exportPlanet, g.Trade.TargetStation))
            {
                CancelTradePlan();
                return;
            }

            if (WaitForBlockadeRemoval(g, exportPlanet, timeStep))
                return;

            if (Owner.TakingOffFrom == exportPlanet)
            {
                LoadGoodsForStation(g);
                return;
            }

            if (InTradeLandingRange(exportPlanet) && NothingToLoadForStation(g, exportPlanet))
            {
                CancelTradePlan(exportPlanet);
                return;
            }

            FlyInToTrade(timeStep, g, exportPlanet);
        }

        bool CanSupplyStation(Planet exportPlanet, Ship targetStation)
        {
            return exportPlanet.Owner == Owner.Loyalty
                   && !exportPlanet.Quarantine
                   && targetStation != null
                   && targetStation.Loyalty == Owner.Loyalty
                   && !targetStation.Dismantling;
        }

        static bool NothingToLoadForStation(ShipGoal g, Planet exportPlanet)
            => exportPlanet.Storage.GetGoodAmount(g.Trade.Goods) < 1; // other freighter took the goods, damn!

        void LoadGoodsForStation(ShipGoal g)
        {
            Planet exportPlanet = g.Trade.ExportFrom;
            Ship targetStation = g.Trade.TargetStation;
            if (!CanSupplyStation(exportPlanet, targetStation))
            {
                CancelTradePlan();
                return;
            }

            if (NothingToLoadForStation(g, exportPlanet))
            {
                CancelTradePlan(exportPlanet);
                return;
            }

            float eta = Owner.GetTradeTakeOffTime(exportPlanet) + Owner.GetAstrogateTimeBetween(exportPlanet, targetStation)
                        + Owner.GetStationLandingTime();
            switch (g.Trade.Goods)
            {
                case Goods.Food:
                    Owner.UnloadIntoColony(exportPlanet, Goods.Production);
                    exportPlanet.Population += Owner.UnloadColonists();

                    // food amount estimated the import planet needs
                    float maxFoodLoad = exportPlanet.ExportableFood(exportPlanet, targetStation, eta);
                    exportPlanet.FoodHere -= Owner.LoadFood(maxFoodLoad);
                    break;
                case Goods.Production:
                    Owner.UnloadIntoColony(exportPlanet, Goods.Food);
                    exportPlanet.Population += Owner.UnloadColonists();
                    float maxProdLoad = exportPlanet.ExportableProd(exportPlanet, targetStation, eta);
                    exportPlanet.ProdHere -= Owner.LoadProduction(maxProdLoad);
                    break;
                default:
                    CancelTradePlan(exportPlanet); // goods type not implemented
                    break;
            }

            SetTradePlan(Plan.DropOffGoodsForStation, exportPlanet, targetStation, g.Trade.Goods);
        }

        public void DoDropOffGoodsForStation(FixedSimTime timeStep, ShipGoal g)
        {
            Ship targetStation = g.Trade.TargetStation;
            if (!CanDeliverToStation(targetStation))
            {
                CancelTradePlan(g.Trade.ExportFrom);
                return;
            }

            FlyInToStation(timeStep, g, targetStation);
        }

        bool CanDeliverToStation(Ship targetStation)
        {
            return targetStation != null
                   && targetStation.Active
                   && targetStation.Loyalty == Owner.Loyalty
                   && !targetStation.Dismantling
                   && !targetStation.Supply.InTradeBlockade;
        }

        void UnloadGoodsForStation(ShipGoal g)
        {
            Ship targetStation = g.Trade.TargetStation;
            if (!CanDeliverToStation(targetStation))
            {
                CancelTradePlan(g.Trade.ExportFrom);
                return;
            }

            bool fullBeforeUnload = Owner.CargoSpaceFree.AlmostZero();
            float maxUnload = targetStation.IsMiningStation ? targetStation.MaxSupplyForMiningStation : targetStation.CargoSpaceFree;
            switch (g.Trade.Goods)
            {
                case Goods.Food:       targetStation.LoadFood(Owner.UnloadFood(maxUnload));             break;
                case Goods.Production: targetStation.LoadProduction(Owner.UnloadProduction(maxUnload)); break;
            }

            Owner.Loyalty.UpdateAverageFreightFTL(Owner.MaxFTLSpeed);
            Owner.Loyalty.UpdateAverageFreightCargoCap(Owner.CargoSpaceMax);
            // If we did not unload all cargo, its better to build faster smaller cheaper freighters
            FreighterPriority freighterPriority = fullBeforeUnload && Owner.CargoSpaceUsed < 1
                                                  ? FreighterPriority.UnloadedAllCargo
                                                  : FreighterPriority.ExcessCargoLeft;

            Owner.Loyalty.IncreaseFastVsBigFreighterRatio(freighterPriority);
            Planet toOrbit = Owner.Loyalty.FindNearestRallyPoint(Owner.Position);
            CancelTradePlan(toOrbit);
            Owner.Loyalty.CheckForRefitFreighter(Owner, 10);
        }

        // a haul flies to its station and loads as it lands there, unless other freighters took what the station held
        public void DoPickupGoodsFromStation(FixedSimTime timeStep, ShipGoal g)
        {
            Ship station = g.Trade.FromStation;
            if (!CanHaul(station, g.Trade.ImportTo))
            {
                CallOffTrade(g.Trade);
                return;
            }

            if (WaitForBlockadeRemoval(g, station, timeStep))
                return;

            if (Owner.Position.InRadius(station.Position, LandShip.ShipyardLandingRange(Owner)) && station.GetProduction() < 1f)
            {
                CallOffTrade(g.Trade);
                return;
            }

            FlyInToStation(timeStep, g, station);
        }

        // a trade called off flies back to its exporting colony, or a haul, with none, to the nearest rally point
        public void CallOffTrade(TradePlan trade)
            => CancelTradePlan(trade.ExportFrom ?? Owner.Loyalty.FindNearestRallyPoint(Owner.Position));

        bool CanHaul(Ship station, Planet importPlanet)
        {
            return station is { Active: true, Dying: false, Dismantling: false }
                   && station.Loyalty == Owner.Loyalty
                   && importPlanet.Owner != null // colony was wiped out
                   && !importPlanet.Quarantine
                   && (importPlanet.Owner == Owner.Loyalty || importPlanet.Owner.IsTradeTreaty(Owner.Loyalty));
        }

        // at touchdown a haul fills its hold, whatever its colony's room, to move the production out of the station
        void LoadGoodsFromStation(ShipGoal g)
        {
            Ship station = g.Trade.FromStation;
            Planet importPlanet = g.Trade.ImportTo;
            if (!CanHaul(station, importPlanet))
            {
                CallOffTrade(g.Trade);
                return;
            }

            float load = Math.Min(Owner.CargoRoom, station.GetProduction());
            if (load < 1f)
            {
                CallOffTrade(g.Trade);
                return;
            }

            station.UnloadProduction(Owner.LoadProduction(load));
            importPlanet.ChangeIncomingCargo(Goods.Production, Owner.GetCargo(Goods.Production));
            SetTradePlan(Plan.DropOffGoods, station, importPlanet, Goods.Production);
        }
    }
}
