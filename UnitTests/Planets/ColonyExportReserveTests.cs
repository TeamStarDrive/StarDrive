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
        public void ProdReserveCoversTheColonysOwnBuildQueue()
        {
            float before = Homeworld.ProdExportReserve;

            Building nanoMine = ResourceManager.GetBuildingTemplate("Nano Mine");
            Assert.IsNotNull(nanoMine, "Nano Mine template missing from ResourceManager");
            Assert.IsTrue(Homeworld.Construction.Enqueue(nanoMine), "failed to enqueue the test building");

            Assert.IsTrue(Homeworld.ProdExportReserve > before,
                $"a queued building must reserve the production still owed on it: {before} -> {Homeworld.ProdExportReserve}");
        }
    }
}
