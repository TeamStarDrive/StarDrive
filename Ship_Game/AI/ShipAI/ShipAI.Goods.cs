using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Ship_Game.Ships;

namespace Ship_Game.AI
{
    internal sealed class PickupGoods : ShipAIPlan
    {
        public PickupGoods(ShipAI ai) : base(ai)
        {
        }

        public override void Execute(float elapsedTime, ShipAI.ShipGoal g)
        {
            Planet exportPlanet = g.Trade.ExportFrom;
            Planet importPlanet = g.Trade.ImportTo;
            if (AI.WaitForBlockadeRemoval(g, exportPlanet, elapsedTime))
                return;

            AI.ThrustOrWarpToPosCorrected(exportPlanet.Center, elapsedTime);
            if (!Owner.Center.InRadius(exportPlanet.Center, exportPlanet.ObjectRadius + 300f))
                return;

            if (exportPlanet.Storage.GetGoodAmount(g.Trade.Goods) < 1) // other freighter took the goods, damn!
            {
                AI.CancelTradePlan(exportPlanet);
                return;
            }

            switch (g.Trade.Goods)
            {
                case Goods.Food:
                    exportPlanet.ProdHere   += Owner.UnloadProduction();
                    exportPlanet.Population += Owner.UnloadColonists();

                    // food amount estimated the import planet needs
                    float maxFoodLoad = importPlanet.Storage.Max - importPlanet.FoodHere;
                    maxFoodLoad = (maxFoodLoad - importPlanet.Food.NetIncome * 25)
                                .Clamped(0, exportPlanet.Storage.Max * 0.5f);
                    if (maxFoodLoad.AlmostZero())
                    {
                        AI.CancelTradePlan(exportPlanet); // import planet food is good by now
                        return;
                    }

                    exportPlanet.FoodHere -= Owner.LoadFood(maxFoodLoad);
                    break;
                case Goods.Production:
                    exportPlanet.FoodHere   += Owner.UnloadFood();
                    exportPlanet.Population += Owner.UnloadColonists();
                    float maxProdLoad = exportPlanet.ProdHere.Clamped(0f, exportPlanet.Storage.Max * 0.25f);
                    exportPlanet.ProdHere   -= Owner.LoadProduction(maxProdLoad);
                    break;
                case Goods.Colonists:
                    exportPlanet.ProdHere += Owner.UnloadProduction();
                    exportPlanet.FoodHere += Owner.UnloadFood();

                    // load everyone we can :P
                    exportPlanet.Population -= Owner.LoadColonists(exportPlanet.Population * 0.2f);
                    break;
            }
            AI.SetTradePlan(ShipAI.Plan.DropOffGoods, exportPlanet, importPlanet, g.Trade.Goods);
        }
    }

    
    internal sealed class DropOffGoods : ShipAIPlan
    {
        public DropOffGoods(ShipAI ai) : base(ai)
        {
        }

        public override void Execute(float elapsedTime, ShipAI.ShipGoal g)
        {
            Planet importPlanet = g.Trade.ImportTo;
            if (AI.WaitForBlockadeRemoval(g, importPlanet, elapsedTime))
                return;

            AI.ThrustOrWarpToPosCorrected(importPlanet.Center, elapsedTime);
            if (!Owner.Center.InRadius(importPlanet.Center, importPlanet.ObjectRadius + 300f))
                return;

            Owner.loyalty.TaxGoodsIfMercantile(Owner.CargoSpaceUsed);
            importPlanet.FoodHere   += Owner.UnloadFood(importPlanet.Storage.Max - importPlanet.FoodHere);
            importPlanet.ProdHere   += Owner.UnloadProduction(importPlanet.Storage.Max - importPlanet.ProdHere);
            importPlanet.Population += Owner.UnloadColonists(importPlanet.MaxPopulation - importPlanet.Population);

            // If we did not unload all cargo, its better to build faster freighters
            float fasterFreighters     = Owner.CargoSpaceUsed.NotZero() ? 0.01f : -0.01f;
            Owner.loyalty.IncreaseFastVsBigFreighterRatio(fasterFreighters);
            CheckAndScrap1To10();
            AI.CancelTradePlan(importPlanet);
        }

        // 1 out of 10 trades - check if there is better suited freighter model available
        // Note that there are more scrap logic for freighters (idle timeout and idle ones where a new tech is researched
        void CheckAndScrap1To10() 
        {
            if (!RandomMath.RollDice(10))
                return;

            Ship betterFreighter = ShipBuilder.PickFreighter(Owner.loyalty, Owner.loyalty.FastVsBigFreighterRatio);
            if (betterFreighter != null && betterFreighter.Name != Owner.Name)
                AI.OrderScrapShip();
        }
    }
}
