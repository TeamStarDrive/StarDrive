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

            bool freighterTooSmall = false;
            float eta              = Owner.GetAstrograteTimeTo(importPlanet);
            switch (g.Trade.Goods)
            {
                case Goods.Food:
                    exportPlanet.ProdHere   += Owner.UnloadProduction();
                    exportPlanet.Population += Owner.UnloadColonists();

                    // food amount estimated the import planet needs
                    float maxFoodLoad   = importPlanet.Storage.Max - importPlanet.FoodHere;
                    float foodLoadLimit = Owner.loyalty.ManualTrade ? 1 : 0.5f; // should be set as an option for the player in future trade window.
                    maxFoodLoad         = (maxFoodLoad - importPlanet.Food.NetIncome * eta).Clamped(0, exportPlanet.Storage.Max * foodLoadLimit);
                    if (maxFoodLoad.AlmostZero())
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
                    float prodLoadLimit      = Owner.loyalty.ManualTrade ? 1 : 0.25f; // should be set as an option for the player in future trade window.
                    float maxProdLoad        = exportPlanet.ProdHere.Clamped(0f, exportPlanet.Storage.Max * prodLoadLimit);
                    exportPlanet.ProdHere   -= Owner.LoadProduction(maxProdLoad);
                    freighterTooSmall        = Owner.CargoSpaceMax.Less(maxProdLoad);
                    break;
                case Goods.Colonists:
                    exportPlanet.ProdHere += Owner.UnloadProduction();
                    exportPlanet.FoodHere += Owner.UnloadFood();

                    // load everyone we can :P
                    exportPlanet.Population -= Owner.LoadColonists(exportPlanet.Population * 0.2f);
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

        public override void Execute(float elapsedTime, ShipAI.ShipGoal g)
        {
            Planet importPlanet = g.Trade.ImportTo;
            if (AI.WaitForBlockadeRemoval(g, importPlanet, elapsedTime))
                return;

            AI.ThrustOrWarpToPosCorrected(importPlanet.Center, elapsedTime);
            if (!Owner.Center.InRadius(importPlanet.Center, importPlanet.ObjectRadius + 300f))
                return;

            bool fullBeforeUnload = Owner.CargoSpaceFree.AlmostZero();
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
            CheckAndScrap1To10();
        }

        // 1 out of 10 trades - check if there is better suited freighter model available and we have idle
        // freighters which can cover the scrap
        // Note that there are more scrap logic for freighters (idle timeout and idle ones when a new tech is researched
        void CheckAndScrap1To10() 
        {
            if (Owner.loyalty.ManualTrade || !RandomMath.RollDice(10))
                return;

            if (Owner.loyalty.IdleFreighters.Length == 0)
                return;

            Ship betterFreighter = ShipBuilder.PickFreighter(Owner.loyalty, Owner.loyalty.FastVsBigFreighterRatio);
            if (betterFreighter != null && betterFreighter.Name != Owner.Name)
                AI.OrderScrapShip();
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
                Owner.loyalty.AffectFastVsBigFreighterByEta(importPlanet, goods, Owner.GetAstrograteTimeTo(importPlanet));
        }

        public void SetupFreighterPlan(Planet importPlanet, Goods goods)
        {
            Plan plan = Plan.DropOffGoods; // found close freighter with the goods we need so we don't need an export planet
            SetTradePlan(plan, importPlanet, importPlanet, goods); // this planet takes care of itself this trade
            Owner.loyalty.AffectFastVsBigFreighterByEta(importPlanet, goods, Owner.GetAstrograteTimeTo(importPlanet));
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
