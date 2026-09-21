using Microsoft.VisualStudio.TestTools.UnitTesting;
using Ship_Game;
using Ship_Game.AI;
using Ship_Game.GameScreens.Universe.Debug;
using Ship_Game.Ships;
#pragma warning disable CA2213

namespace UnitTests.ExoticSystems
{
    [TestClass]
    public class StationDesignPicks : StarDriveTest
    {
        public StationDesignPicks()
        {
            CreateUniverseAndPlayerEmpire();
            ResearchDebugUnlocks.UnlockAllResearch(Player, unlockBonuses: false);
            Player.AutoPickBestResearchStation = false;
            Player.AutoPickBestMiningStation = false;
            Assert.IsTrue(Player.CanBuildResearchStations && Player.CanBuildMiningStations, "test needs station tech");
        }

        [TestMethod]
        public void AnUnsetDropdownResolvesToTheRacialDefault()
        {
            Player.data.CurrentMiningStation = "";
            Player.data.CurrentResearchStation = "";

            AssertEqual(Player.data.DefaultMiningStation, Player.data.MiningStation);
            AssertEqual(Player.data.DefaultResearchStation, Player.data.ResearchStation);
            Assert.IsNotNull(ShipBuilder.StationDesignOrNull(Player.data.MiningStation), "the default mining station must be a real design");
            Assert.IsNotNull(ShipBuilder.StationDesignOrNull(Player.data.ResearchStation), "the default research station must be a real design");
        }

        [TestMethod]
        public void ANameThatNoLongerResolvesIsNullNotAThrow()
        {
            Assert.IsNull(ShipBuilder.StationDesignOrNull("No Such Station Design"));
            Assert.IsNull(ShipBuilder.StationDesignOrNull(""));
            Assert.IsNull(ShipBuilder.StationDesignOrNull(null));

            Player.data.CurrentResearchStation = "No Such Station Design";
            AssertEqual("No Such Station Design", Player.data.ResearchStation, "the property passes a set name through unchanged");
            Assert.IsNull(ShipBuilder.StationDesignOrNull(Player.data.ResearchStation), "so the goal sees nothing to build, and no exception");
        }

        [TestMethod]
        public void ARealNameResolvesToItsDesign()
        {
            string name = Player.data.MiningStation;
            IShipDesign design = ShipBuilder.StationDesignOrNull(name);
            Assert.IsNotNull(design, $"'{name}' should be a shipped design");
            AssertEqual(name, design.Name);
        }
    }
}
