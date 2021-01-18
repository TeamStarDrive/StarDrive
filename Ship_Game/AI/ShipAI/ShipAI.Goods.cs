using Ship_Game.Commands.Goals;
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
            if (exportPlanet.Owner == null 
                || importPlanet.Owner == null // colony was wiped out
                || importPlanet.Owner != Owner.loyalty && !importPlanet.Owner.IsTradeTreaty(Owner.loyalty)) 
            {
                AI.CancelTradePlan();
                return;
            }

            if (AI.WaitForBlockadeRemoval(g, exportPlanet, timeStep))
                return;

            AI.ThrustOrWarpToPos(exportPlanet.Center, timeStep);
            if (!Owner.Center.InRadius(exportPlanet.Center, exportPlanet.ObjectRadius + 300f))
                return;

            if (exportPlanet.Storage.GetGoodAmount(g.Trade.Goods) < 1) // other freighter took the goods, damn!
            {
                AI.CancelTradePlan(exportPlanet);
                return;
            }

            if (importPlanet.TradeBlocked) // We can't transport now to the importing planet
            {
                AI.CancelTradePlan(exportPlanet);
                return;
            }

            bool freighterTooSmall = false;
            float eta              = Owner.GetAstrogateTimeTo(importPlanet);
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

            Owner.loyalty.IncreaseFastVsBigFreighterRatio(freighterPriority);
            Owner.loyalty.AffectFastVsBigFreighterByEta(importPlanet, g.Trade.Goods, eta);
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
            Planet exportPlanet = g.Trade.ExportFrom;

            if (importPlanet.Owner == null  // colony was wiped out
                || importPlanet.Owner != Owner.loyalty && !importPlanet.Owner.IsTradeTreaty(Owner.loyalty)) 
            {
                AI.CancelTradePlan(exportPlanet);
                return;
            }

            if (AI.WaitForBlockadeRemoval(g, importPlanet, timeStep))
                return;

            AI.ThrustOrWarpToPos(importPlanet.Center, timeStep);
            if (!Owner.Center.InRadius(importPlanet.Center, importPlanet.ObjectRadius + 300f))
                return;

            bool fullBeforeUnload = Owner.CargoSpaceFree.AlmostZero();
            if (Owner.GetCargo(Goods.Colonists).AlmostZero())
                Owner.loyalty.TaxGoods(Owner.CargoSpaceUsed, importPlanet);

            importPlanet.FoodHere   += Owner.UnloadFood(importPlanet.Storage.Max - importPlanet.FoodHere);
            importPlanet.ProdHere   += Owner.UnloadProduction(importPlanet.Storage.Max - importPlanet.ProdHere);
            importPlanet.Population += Owner.UnloadColonists(importPlanet.MaxPopulation - importPlanet.Population);

            // If we did not unload all cargo, its better to build faster smaller cheaper freighters
            FreighterPriority freighterPriority = fullBeforeUnload && Owner.CargoSpaceUsed.AlmostZero()
                                                  ? FreighterPriority.UnloadedAllCargo
                                                  : FreighterPriority.ExcessCargoLeft;
                                                    
            Owner.loyalty.IncreaseFastVsBigFreighterRatio(freighterPriority);

            Planet toOrbit = importPlanet;
            if (toOrbit.TradeBlocked || Owner.loyalty != toOrbit.Owner)
                toOrbit = Owner.loyalty.FindNearestRallyPoint(Owner.Center); // get out of here!

            AI.CancelTradePlan(toOrbit);
            Owner.loyalty.CheckForRefitFreighter(Owner, 10);
        }
    }

    partial class ShipAI
    {
        public void SetupFreighterPlan(Planet exportPlanet, Planet importPlanet, Goods goods)
        {
            // if ship has this cargo type on board, proceed to drop it off at destination
            Plan plan = Owner.GetCargo(goods) / Owner.CargoSpaceMax > 0.5f
                        ? Plan.DropOffGoods 
                        : Plan.PickupGoods;

            SetTradePlan(plan, exportPlanet, importPlanet, goods);
            if (plan == Plan.DropOffGoods)
                Owner.loyalty.AffectFastVsBigFreighterByEta(importPlanet, goods, Owner.GetAstrogateTimeTo(importPlanet));
        }

        public void SetupFreighterPlan(Planet importPlanet, Goods goods)
        {
            Plan plan = Plan.DropOffGoods; // found close freighter with the goods we need so we don't need an export planet
            SetTradePlan(plan, importPlanet, importPlanet, goods); // this planet takes care of itself this trade
            Owner.loyalty.AffectFastVsBigFreighterByEta(importPlanet, goods, Owner.GetAstrogateTimeTo(importPlanet));
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
