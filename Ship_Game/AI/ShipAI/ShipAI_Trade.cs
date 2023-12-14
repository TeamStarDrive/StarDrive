using Ship_Game.Ships;
using SDGraphics;

namespace Ship_Game.AI
{
    public sealed partial class ShipAI
    {
        public void DoPickupGoodsForStation(FixedSimTime timeStep, ShipGoal g)
        {
            Planet exportPlanet = g.Trade.ExportFrom;
            Ship targetStation = g.Trade.TargetStation;
            if (exportPlanet.Owner == null 
                || exportPlanet.Quarantine 
                || targetStation == null 
                || targetStation.Loyalty != Owner.Loyalty)
            {
                CancelTradePlan();
                return;
            }

            if (WaitForBlockadeRemoval(g, exportPlanet, timeStep))
                return;

            ThrustOrWarpToPos(exportPlanet.Position, timeStep);
            if (!Owner.Position.InRadius(exportPlanet.Position, exportPlanet.Radius + 300f))
                return;

            if (exportPlanet.Storage.GetGoodAmount(g.Trade.Goods) < 1) // other freighter took the goods, damn!
            {
                CancelTradePlan(exportPlanet);
                return;
            }

            float eta = Owner.GetAstrogateTimeBetween(exportPlanet, targetStation);
            switch (g.Trade.Goods)
            {
                case Goods.Food:
                    exportPlanet.ProdHere += Owner.UnloadProduction();
                    exportPlanet.Population += Owner.UnloadColonists();

                    // food amount estimated the import planet needs
                    float maxFoodLoad = exportPlanet.ExportableFood(exportPlanet, targetStation, eta);
                    exportPlanet.FoodHere -= Owner.LoadFood(maxFoodLoad);
                    break;
                case Goods.Production:
                    exportPlanet.FoodHere += Owner.UnloadFood();
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
            Planet exportPlanet = g.Trade.ExportFrom;
            Ship targetStation  = g.Trade.TargetStation;

            if (targetStation == null 
                || !targetStation.Active
                || targetStation.Loyalty != Owner.Loyalty
                || targetStation.Supply.InTradeBlockade)
            {
                CancelTradePlan(exportPlanet);
                return;
            }

            ThrustOrWarpToPos(targetStation.Position, timeStep);
            if (!Owner.Position.InRadius(targetStation.Position, targetStation.Radius))
                return;

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
    }
}
