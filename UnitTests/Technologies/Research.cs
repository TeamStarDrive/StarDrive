using Microsoft.VisualStudio.TestTools.UnitTesting;
using Ship_Game;
using Ship_Game.AI.Research;

namespace UnitTests.Technologies
{
    [TestClass]
    public class TestResearch : StarDriveTest
    {
        public TestResearch()
        {
            CreateGameInstance();
            LoadTechContent();
            CreateUniverseAndPlayerEmpire();
        }

        [TestMethod]
        public void LoadResearchPriorities()
        {
            var researchMods = new ResearchOptions();
            researchMods.LoadResearchOptions(Player);
            Assert.IsTrue(researchMods.Count() > 0);
        }
    }
}