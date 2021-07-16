using System;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Ship_Game;
using Ship_Game.AI.Research;


namespace UnitTests.Technologies
{
    [TestClass]
    public class TestShipCostMods : StarDriveTest
    {
        ChooseTech TechChooser;
        public TestShipCostMods()
        {
            CreateGameInstance();
            LoadTechContent();
            LoadStarterShipVulcan();
            CreateUniverseAndPlayerEmpire();
            Player.ShipsWeCanBuild.Clear();
            Enemy.ShipsWeCanBuild.Clear();
            TechChooser = new ChooseTech(Enemy);
        }

        [TestMethod]
        public void LoadShipCostModifiers()
        {
            var researchMods = new ResearchOptions();
            researchMods.LoadResearchOptions(Player);
            Assert.IsTrue(researchMods.Count() > 0);
        }
    }
}