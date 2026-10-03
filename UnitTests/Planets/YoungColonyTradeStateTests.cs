using Microsoft.VisualStudio.TestTools.UnitTesting;
using Ship_Game;
using Vector2 = SDGraphics.Vector2;
#pragma warning disable CA2213

namespace UnitTests.Planets
{
    /// <summary>
    /// Issue #314: a freshly colonised world has storage so small that its starting food and
    /// production read as "full", so the governor puts both straight onto export. One freighter
    /// lifts the entire buffer while colonists are still arriving, and the colony starves.
    /// </summary>
    [TestClass]
    public class YoungColonyTradeStateTests : StarDriveTest
    {
        readonly Planet Colony;

        public YoungColonyTradeStateTests()
        {
            CreateUniverseAndPlayerEmpire();
            AddHomeWorldToEmpire(new Vector2(2000), Player); // a solo planet always STOREs, so the empire needs two
            Colony = AddDummyPlanetToEmpire(new Vector2(1000), Player, fertility: 0.5f, minerals: 1f, maxPop: 2000f);
            Colony.UpdateIncomes();
        }

        void FillAFreshColonysStores()
        {
            Colony.CType = Planet.ColonyType.Core; // a Colony-type planet never reaches the trade states at all
            Assert.IsTrue(Colony.Storage.Max < Player.AverageFreighterCargoCap * 2,
                $"test needs a colony whose storage is under two freighter loads: {Colony.Storage.Max}");
            Assert.IsTrue(Colony.PopulationRatio < 0.1f, $"test needs a barely settled colony: {Colony.PopulationRatio}");

            // full stores - which on this colony means one freighter load, not a surplus
            Colony.FoodHere = Colony.Storage.Max;
            Colony.ProdHere = Colony.Storage.Max;
            Colony.DoGoverning();
        }

        [TestMethod]
        public void AFreshColonyDoesNotExportItsFood()
        {
            FillAFreshColonysStores();
            Assert.AreEqual(Planet.GoodState.STORE, Colony.FS, "a young colony must keep the food its colonists are landing to eat");
        }

        [TestMethod]
        public void AFreshColonyDoesNotExportItsProduction()
        {
            FillAFreshColonysStores();
            Assert.AreEqual(Planet.GoodState.STORE, Colony.PS, "a young colony must keep the production that starts its own buildings");
        }

        [TestMethod]
        public void AGrownColonyStillExportsItsSurplus()
        {
            Colony.CType = Planet.ColonyType.Core;
            Colony.Storage.Max = 1000; // storage grows with the colony, so the young-colony rule lets go
            Colony.FoodHere = Colony.Storage.Max;
            Colony.ProdHere = Colony.Storage.Max;
            Colony.DoGoverning();

            Assert.AreEqual(Planet.GoodState.EXPORT, Colony.PS, "a colony past the young-colony rule must still feed the empire");
            Assert.AreEqual(Planet.GoodState.EXPORT, Colony.FS, "the food side must let go of the rule as well");
        }
    }
}
