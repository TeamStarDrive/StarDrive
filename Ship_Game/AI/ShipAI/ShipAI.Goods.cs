using SDGraphics;
using Ship_Game.Ships;

namespace Ship_Game.AI
{
    internal sealed class PickupGoods : ShipAIPlan
    {
        public PickupGoods(ShipAI ai) : base(ai)
        {
        }

        public override void Execute(FixedSimTime timeStep, ShipAI.ShipGoal g)
        {
            Planet exportPlanet = g.Trade.ExportFrom;
            Planet importPlanet = g.Trade.ImportTo;
            if (!CanTrade(exportPlanet, importPlanet))
            {
                AI.CancelTradePlan();
                return;
            }

            if (AI.WaitForBlockadeRemoval(g, exportPlanet, timeStep))
                return;

            if (Owner.TakingOffFrom == exportPlanet)
            {
                LoadGoods(g);
                return;
            }

            if (AI.InTradeLandingRange(exportPlanet) && NothingToLoad(g, exportPlanet, importPlanet))
            {
                AI.CancelTradePlan(exportPlanet);
                return;
            }

            AI.FlyInToTrade(timeStep, g, exportPlanet);
        }

        bool CanTrade(Planet exportPlanet, Planet importPlanet)
        {
            return exportPlanet.Owner == Owner.Loyalty
                   && !exportPlanet.Quarantine
                   && !importPlanet.Quarantine
                   && importPlanet.Owner != null // colony was wiped out
                   && (importPlanet.Owner == Owner.Loyalty || importPlanet.Owner.IsTradeTreaty(Owner.Loyalty));
        }

        static bool NothingToLoad(ShipAI.ShipGoal g, Planet exportPlanet, Planet importPlanet)
        {
            return exportPlanet.Storage.GetGoodAmount(g.Trade.Goods) < 1 // other freighter took the goods, damn!
                   || importPlanet.TradeBlocked; // We can't transport now to the importing planet
        }

        public void LoadGoods(ShipAI.ShipGoal g)
        {
            Planet exportPlanet = g.Trade.ExportFrom;
            Planet importPlanet = g.Trade.ImportTo;
            if (!CanTrade(exportPlanet, importPlanet))
            {
                AI.CancelTradePlan();
                return;
            }

            if (NothingToLoad(g, exportPlanet, importPlanet))
            {
                AI.CancelTradePlan(exportPlanet);
                return;
            }

            bool freighterTooSmall = false;
            float eta              = Owner.GetTradeTakeOffTime(exportPlanet) + Owner.GetAstrogateTimeTo(importPlanet)
                                     + Owner.GetTradeLandingTime(importPlanet);
            exportPlanet.UpdateAverageFreightTurns(importPlanet, exportPlanet, g.Trade.Goods, g.Trade.StardateAdded);
            switch (g.Trade.Goods)
            {
                case Goods.Food:
                    exportPlanet.ProdHere   += Owner.UnloadProduction();
                    exportPlanet.Population += Owner.UnloadColonists();

                    // food amount estimated the import planet needs
                    float maxFoodLoad = exportPlanet.ExportableFood(exportPlanet, importPlanet, eta);
                    if (maxFoodLoad.Less(3f.UpperBound(Owner.CargoSpaceMax)))
                    {
                        AI.CancelTradePlan(exportPlanet); // import planet food is good by now
                        return;
                    }

                    exportPlanet.FoodHere -= Owner.LoadFood(maxFoodLoad);
                    freighterTooSmall      = Owner.CargoSpaceMax.Less(maxFoodLoad);
                    break;
                case Goods.Production:
                    exportPlanet.FoodHere   += Owner.UnloadFood();
                    exportPlanet.Population += Owner.UnloadColonists();
                    float maxProdLoad        = exportPlanet.ExportableProd(exportPlanet, importPlanet, eta);
                    if (maxProdLoad.Less(3f.UpperBound(Owner.CargoSpaceMax)))
                    {
                        AI.CancelTradePlan(exportPlanet); // there is nothing to load, wft?
                        return;
                    }

                    exportPlanet.ProdHere -= Owner.LoadProduction(maxProdLoad);
                    freighterTooSmall      = Owner.CargoSpaceMax.Less(maxProdLoad);
                    break;
                case Goods.Colonists:
                    exportPlanet.ProdHere += Owner.UnloadProduction();
                    exportPlanet.FoodHere += Owner.UnloadFood();

                    float maxPopLoad        = exportPlanet.ExportablePop(exportPlanet, importPlanet);
                    if (maxPopLoad.AlmostZero())
                    {
                        AI.CancelTradePlan(exportPlanet); // No pop to load
                        return;
                    }

                    exportPlanet.Population -= Owner.LoadColonists(maxPopLoad);
                    freighterTooSmall        = Owner.CargoSpaceMax.Less(maxPopLoad);
                    break;
            }

            FreighterPriority freighterPriority = freighterTooSmall 
                                                  ? FreighterPriority.TooSmall 
                                                  : FreighterPriority.TooBig;

            Owner.Loyalty.IncreaseFastVsBigFreighterRatio(freighterPriority);
            Owner.Loyalty.AffectFastVsBigFreighterByEta(importPlanet, g.Trade.Goods, eta);
            AI.SetTradePlan(ShipAI.Plan.DropOffGoods, exportPlanet, importPlanet, g.Trade.Goods);
        }
    }

    internal sealed class DropOffGoods : ShipAIPlan
    {
        public DropOffGoods(ShipAI ai) : base(ai)
        {
        }

        public override void Execute(FixedSimTime timeStep, ShipAI.ShipGoal g)
        {
            Planet importPlanet = g.Trade.ImportTo;
            if (!CanDeliver(importPlanet))
            {
                AI.CancelTradePlan(g.Trade.ExportFrom);
                return;
            }

            if (AI.WaitForBlockadeRemoval(g, importPlanet, timeStep))
                return;

            AI.FlyInToTrade(timeStep, g, importPlanet);
        }

        bool CanDeliver(Planet importPlanet)
        {
            return importPlanet.Owner != null // colony was wiped out
                   && !importPlanet.Quarantine
                   && (importPlanet.Owner == Owner.Loyalty || importPlanet.Owner.IsTradeTreaty(Owner.Loyalty));
        }

        public float UnloadGoods(ShipAI.ShipGoal g)
        {
            Planet importPlanet = g.Trade.ImportTo;
            Planet exportPlanet = g.Trade.ExportFrom;
            if (!CanDeliver(importPlanet))
            {
                AI.CancelTradePlan(exportPlanet);
                return 0f;
            }

            float cargoBefore = Owner.CargoSpaceUsed;
            bool fullBeforeUnload = Owner.CargoSpaceFree.AlmostZero();
            if (Owner.GetCargo(Goods.Colonists).AlmostZero())
                Owner.Loyalty.TaxGoods(Owner.CargoSpaceUsed, importPlanet);

            importPlanet.FoodHere   += Owner.UnloadFood(importPlanet.Storage.Max - importPlanet.FoodHere);
            importPlanet.ProdHere   += Owner.UnloadProduction(importPlanet.Storage.Max - importPlanet.ProdHere);
            importPlanet.Population += Owner.UnloadColonists(importPlanet.MaxPopulation - importPlanet.Population);
            float unloaded = cargoBefore - Owner.CargoSpaceUsed;

            importPlanet.UpdateAverageFreightTurns(importPlanet, exportPlanet, g.Trade.Goods, g.Trade.StardateAdded);
            Owner.Loyalty.UpdateAverageFreightFTL(Owner.MaxFTLSpeed);
            Owner.Loyalty.UpdateAverageFreightCargoCap(Owner.CargoSpaceMax);
            // If we did not unload all cargo, its better to build faster smaller cheaper freighters
            FreighterPriority freighterPriority = fullBeforeUnload && Owner.CargoSpaceUsed < 1
                                                  ? FreighterPriority.UnloadedAllCargo
                                                  : FreighterPriority.ExcessCargoLeft;
                                                    
            Owner.Loyalty.IncreaseFastVsBigFreighterRatio(freighterPriority);
            importPlanet.Mend(2); // Helping with planet repair/heal troops/buildings

            Planet toOrbit = importPlanet;
            if (toOrbit.TradeBlocked || Owner.Loyalty != toOrbit.Owner)
                toOrbit = Owner.Loyalty.FindNearestRallyPoint(Owner.Position); // get out of here!

            AI.CancelTradePlan(toOrbit);
            Owner.Loyalty.CheckForRefitFreighter(Owner, 10);
            return unloaded;
        }
    }

    partial class ShipAI
    {
        public bool HasTradePlan => OrderQueue.TryPeekFirst(out ShipGoal g) && g.Trade != null;

        public float TradeAfterLanding(Planet planet, Ship station)
        {
            if (!OrderQueue.TryPeekFirst(out ShipGoal g) || g.Trade == null)
                return 0f;

            if (station != null)
            {
                if (g.Plan == Plan.DropOffGoodsForStation && station == g.Trade.TargetStation)
                    UnloadGoodsForStation(g);
                return 0f;
            }

            switch (g.Plan)
            {
                case Plan.PickupGoods when planet == g.Trade.ExportFrom:           PickupGoods.LoadGoods(g);          break;
                case Plan.DropOffGoods when planet == g.Trade.ImportTo:            return DropOffGoods.UnloadGoods(g);
                case Plan.PickupGoodsForStation when planet == g.Trade.ExportFrom: LoadGoodsForStation(g);            break;
            }
            return 0f;
        }

        public void SetupFreighterPlan(Planet exportPlanet, Planet importPlanet, Goods goods)
        {
            Plan plan = Plan.PickupGoods;
            if (importPlanet == exportPlanet)
            {
                plan = Plan.DropOffGoods;  // fast track since needed cargo was already on board
                float eta = Owner.GetAstrogateTimeTo(importPlanet) + Owner.GetTradeLandingTime(importPlanet);
                Owner.Loyalty.AffectFastVsBigFreighterByEta(importPlanet, goods, eta);
            }
            else if (Owner.Loyalty == importPlanet.Owner || Owner.Loyalty.IsAlliedWith(importPlanet.Owner))
            {
                Owner.Loyalty.AI.SpaceRoadsManager.AddSpaceRoadHeat(exportPlanet.System,
                    importPlanet.System, Owner.CargoSpaceMax * 0.02f);
            }

            SetTradePlan(plan, exportPlanet, importPlanet, goods);
        }

        public void SetupFreighterPlan(Planet exportPlanet, Ship targetStation, Goods goods)
        {
            if (targetStation.System != null)
            {
                Owner.Loyalty.AI.SpaceRoadsManager.AddSpaceRoadHeat(exportPlanet.System, 
                    targetStation.System, Owner.CargoSpaceMax * 1f);
            }
            Plan plan = Plan.PickupGoodsForStation;
            SetTradePlan(plan, exportPlanet, targetStation, goods);
        }
    }

    public enum FreighterPriority
    {
        TooSmall,
        TooBig,
        TooSlow,
        ExcessCargoLeft,
        UnloadedAllCargo
    }
}
