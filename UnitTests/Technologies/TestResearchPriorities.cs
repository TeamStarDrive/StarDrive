using System;
using System.Linq;
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
        public void ResearchPrioritiesTechCategories()
        {
            var researchMods = new ResearchOptions();
            researchMods.LoadResearchOptions(Enemy);
            var researchPriorities = new ResearchPriorities(Enemy, researchMods);
            int researchAreas = researchPriorities.TechCategoryPrioritized.Split(':').Length;
            AssertEqual(10, researchAreas, $"Unexpected number of tech areas: {researchPriorities.TechCategoryPrioritized}");
        }
        
        
        void ResetToFirstPriority(ResearchOptions.ResearchArea area, string expected)
        {
            var opts = new ResearchOptions();
            opts.LoadResearchOptions(Enemy);
            foreach (ResearchOptions.ResearchArea a in Enum.GetValues(typeof(ResearchOptions.ResearchArea)))
                opts.ChangePriority(a, 1);
            opts.ChangePriority(area, 10);

            var priorities = new ResearchPriorities(Enemy, opts, enableRandomizer: false);
            string firstPriority = priorities.TechCategoryPrioritized.Split(':')[1]; // skip "TECH:"
            AssertEqual(expected, firstPriority,
                            $"{area} Was not the highest priority: {priorities.TechCategoryPrioritized}");
        }

        [TestMethod]
        public void ResearchPriorityChangeToFirstPriority()
        {
            ResetToFirstPriority(ResearchOptions.ResearchArea.ShipTech, "ShipHull");
            ResetToFirstPriority(ResearchOptions.ResearchArea.Colonization, "Colonization");
            ResetToFirstPriority(ResearchOptions.ResearchArea.Economic, "Economic");
            ResetToFirstPriority(ResearchOptions.ResearchArea.General, "General");
            ResetToFirstPriority(ResearchOptions.ResearchArea.GroundCombat, "GroundCombat");
            ResetToFirstPriority(ResearchOptions.ResearchArea.Industry, "Industry");
        }


        [TestMethod]
        public void ResearchPrioritiesResearch()
        {
            Technology tech = ResourceManager.TechTree.Values.First(t => t.TechnologyTypes.First() == TechnologyType.Research);
            
            foreach (Technology.LeadsToTech techName in tech.ComesFrom)
            {
                Enemy.UnlockTech(techName.UID, TechUnlockType.Normal);
            }

            ResetToFirstPriority(ResearchOptions.ResearchArea.Research, "Research");
        }
    }
}