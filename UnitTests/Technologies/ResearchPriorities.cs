using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Ship_Game;
using Ship_Game.AI.Research;

namespace UnitTests.Technologies
{
    [TestClass]
    public class TestResearchPriorities : StarDriveTest
    {
        public TestResearchPriorities()
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

        [TestMethod]
        public void TestResearchPrioritiesTechCategories()
        {
            var researchMods = new ResearchOptions();
            researchMods.LoadResearchOptions(Enemy);
            var researchPriorities = new ResearchPriorities(Enemy, researchMods);
            int researchAreas      = researchPriorities.TechCategoryPrioritized.Split(':').Length;
            Assert.AreEqual(10, researchAreas, $"Unexpected number of tech areas: {researchPriorities.TechCategoryPrioritized}");
        }

        ResearchPriorities ResetPriorities(ResearchOptions.ResearchArea testArea)
        {
            var researchMods = new ResearchOptions();
            researchMods.LoadResearchOptions(Enemy);
            foreach (ResearchOptions.ResearchArea area in Enum.GetValues(typeof(ResearchOptions.ResearchArea)))
            {
                researchMods.ChangePriority(area, 1);
            }
            researchMods.ChangePriority(testArea, 10);
            var researchPriorities = new ResearchPriorities(Enemy, researchMods);
            return researchPriorities;
        }

        [TestMethod]
        public void TestResearchPrioritiesShips()
        {
            var area               = ResearchOptions.ResearchArea.ShipTech;
            var researchPriorities = ResetPriorities(area);
            string categories      = researchPriorities.TechCategoryPrioritized;
            bool first             = categories.StartsWith("TECH:Ship");
            Assert.IsTrue(first, $"{area} Was not the highest priority {categories}");
        }

        [TestMethod]
        public void TestResearchPrioritiesColonization()
        {
            var area               = ResearchOptions.ResearchArea.Colonization;
            var researchPriorities = ResetPriorities(area);
            string categories      = researchPriorities.TechCategoryPrioritized;
            bool first             = categories.StartsWith($"TECH:{area}");
            Assert.IsTrue(first, $"{area} Was not the highest priority {categories}");
        }

        [TestMethod]
        public void TestResearchPrioritiesEconomic()
        {
            var area               = ResearchOptions.ResearchArea.Economic;
            var researchPriorities = ResetPriorities(area);
            string categories      = researchPriorities.TechCategoryPrioritized;
            bool first             = categories.StartsWith($"TECH:{area}");
            Assert.IsTrue(first, $"{area} Was not the highest priority {categories}");
        }

        [TestMethod]
        public void TestResearchPrioritiesGeneral()
        {
            var area               = ResearchOptions.ResearchArea.General;
            var researchPriorities = ResetPriorities(area);
            string categories      = researchPriorities.TechCategoryPrioritized;
            bool first             = categories.StartsWith($"TECH:{area}");
            Assert.IsTrue(first, $"{area} Was not the highest priority {categories}");
        }

        [TestMethod]
        public void TestResearchPrioritiesGroundCombat()
        {
            var area               = ResearchOptions.ResearchArea.GroundCombat;
            var researchPriorities = ResetPriorities(area);
            string categories      = researchPriorities.TechCategoryPrioritized;
            bool first             = categories.StartsWith($"TECH:{area}");
            Assert.IsTrue(first, $"{area} Was not the highest priority {categories}");
        }

        [TestMethod]
        public void TestResearchPrioritiesIndustry()
        {
            var area               = ResearchOptions.ResearchArea.Industry;
            var researchPriorities = ResetPriorities(area);
            string categories      = researchPriorities.TechCategoryPrioritized;
            bool first             = categories.StartsWith($"TECH:{area}");
            Assert.IsTrue(first, $"{area} Was not the highest priority {categories}");
        }

        [TestMethod]
        public void TestResearchPrioritiesResearch()
        {
            var area               = ResearchOptions.ResearchArea.Research;
            Technology[] techs = ResourceManager.TechTree.Values.Filter(t => t.TechnologyTypes.Contains(TechnologyType.Research));
            foreach(Technology tech in techs)
            {
                foreach(Technology.LeadsToTech techName in tech.ComesFrom)
                {
                    Enemy.UnlockTech(techName.UID, TechUnlockType.Normal);
                }
            }
            var researchPriorities = ResetPriorities(area);
            string categories      = researchPriorities.TechCategoryPrioritized;
            bool first             = categories.StartsWith($"TECH:{area}");
            Assert.IsTrue(first, $"{area} Was not the highest priority {categories}");
        }
    }
}