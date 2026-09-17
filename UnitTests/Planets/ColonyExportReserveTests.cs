using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Ship_Game;
using Vector2 = SDGraphics.Vector2;
#pragma warning disable CA2213

namespace UnitTests.Planets
{
    /// <summary>
    /// Issue #314: a freshly colonised world has tiny storage, so it reads as "full", exports
    /// its entire food buffer, and then starves as it grows into its own max population.
    /// </summary>
    [TestClass]
    public class ColonyExportReserveTests : StarDriveTest
    {
        readonly Planet Colony;
        readonly Planet Homeworld;

        public ColonyExportReserveTests()
        {
            CreateUniverseAndPlayerEmpire();
            Homeworld = AddHomeWorldToEmpire(new Vector2(2000), Player);
            // note: the last arg is population PER TILE, not max population
            Colony = AddDummyPlanetToEmpire(new Vector2(1000), Player, fertility: 0.5f, minerals: 1f, maxPop: 2000f);
            Colony.UpdateIncomes();
        }

        [TestMethod]
        public void FoodReserveIsSizedOnTheTargetPopulationNotTodays()
        {
            Assert.IsTrue(Colony.MaxPopulationBillion > Colony.PopulationBillion,
                "test needs a colony below its max population");

            // today's headcount barely eats anything; the colony it is growing into does
            float todaysNeed = Colony.Consumption * Colony.AverageFoodExportTurns;
            Assert.IsTrue(Colony.FoodExportReserve > todaysNeed,
                $"reserve {Colony.FoodExportReserve} must exceed today's need {todaysNeed}");
        }

        [TestMethod]
        public void ReserveBindsTheFreighterLoadNotJustTheSlotCount()
        {
            float reserve = Colony.FoodExportReserve;
            Assert.IsTrue(reserve > 0f, "test needs a colony with a real food reserve");

            // the whole stockpile sits inside the colony's own reserve
            Colony.Storage.Max = 1000;
            Colony.FoodHere = reserve * 0.5f;

            // this is the guarantee: even if a slot is granted, there is nothing to lift
            Assert.AreEqual(0f, Colony.ExportGoodsLimit(Goods.Food), 0.001f,
                "a freighter must not be allowed to load the colony's own food buffer");
        }

        [TestMethod]
        public void SurplusAboveTheReserveIsStillExportable()
        {
            Colony.Storage.Max = 1000;
            Colony.FoodHere = Colony.FoodExportReserve + 200f;

            Assert.IsTrue(Colony.ExportGoodsLimit(Goods.Food) > 0f,
                "food above the reserve must remain exportable, otherwise the empire starves");
        }

        [TestMethod]
        public void ABuildQueueDoesNotReserveProductionForNonCyberneticRaces()
        {
            Assert.IsFalse(Homeworld.IsCybernetic, "test needs a race that eats food, not production");

            Building nanoMine = ResourceManager.GetBuildingTemplate("Nano Mine");
            Assert.IsNotNull(nanoMine, "Nano Mine template missing from ResourceManager");
            Assert.IsTrue(Homeworld.Construction.Enqueue(nanoMine), "failed to enqueue the test building");

            // a slower build is not worth holding every shipyard's exports back
            Assert.AreEqual(0f, Homeworld.ProdExportReserve, 0.001f,
                "only cybernetic colonies, which eat production, keep a production reserve");
        }

        [TestMethod]
        public void SlotsAreNotOfferedWhenThePickupWouldFindNothingAboveTheReserve()
        {
            // a full homeworld farming flat out: real food income, but nowhere near its own
            // reserve by the time a freighter could arrive
            Homeworld.Storage.Max = 1000;
            Homeworld.FoodHere = 0;
            Homeworld.FS = Planet.GoodState.EXPORT;
            Homeworld.Food.Percent = 1;
            Homeworld.Prod.Percent = 0;
            Homeworld.Res.Percent = 0;
            Homeworld.UpdateIncomes();

            float atPickup = Homeworld.FoodHere + Homeworld.Food.NetIncome * Homeworld.AverageFoodExportTurns;
            Assert.IsTrue(Homeworld.Food.NetIncome > 0, "test needs real food income, or income grants no slots either way");
            Assert.IsTrue(atPickup < Homeworld.FoodExportReserve,
                $"test needs a colony still below its reserve at pickup: {atPickup} vs {Homeworld.FoodExportReserve}");

            Homeworld.UpdateIncomingTradeGoods();

            // income alone used to grant slots here, so freighters crossed the map to load nothing
            Assert.AreEqual(0, Homeworld.FoodExportSlots,
                "a slot promises a pickup, so it must not be offered when the load would be zero");
        }

        [TestMethod]
        public void SlotsSetByHandIgnoreTheReserve()
        {
            float reserve = Colony.FoodExportReserve;
            Assert.IsTrue(reserve > 0f, "test needs a colony with a real food reserve");

            Colony.Storage.Max = 1000;
            Colony.FoodHere = reserve * 0.5f;
            Colony.ManualFoodExportSlots = 2;

            // the player asked for this planet to be emptied; the governor's caution does not override that
            Assert.IsTrue(Colony.ExportGoodsLimit(Goods.Food) > 0f,
                "manual export slots must be able to load, otherwise freighters arrive and find nothing");
        }

    }
}
