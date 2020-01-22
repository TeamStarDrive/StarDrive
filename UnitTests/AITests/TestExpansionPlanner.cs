using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Microsoft.Xna.Framework;
using Ship_Game;

namespace UnitTests.AITests
{
    [TestClass]
    public class TestExpansionPlanner : StarDriveTest
    {
        public TestExpansionPlanner()
        {
            LoadPlanetContent();
            CreateGameInstance();
            CreateTestEnv();
        }

        private Planet P;
        private Empire TestEmpire;

        public void AddPlanetToUniverse(Planet p, bool explored, Vector2 pos)
        {
            var s1         = new SolarSystem {Position = pos};
            p.Center       = pos + Vector2.One;
            p.ParentSystem = s1;
            s1.PlanetList.Add(p);
            if (explored)
                s1.SetExploredBy(TestEmpire);
            Empire.Universe.PlanetsDict.Add(Guid.NewGuid(), p);
            UniverseScreen.SolarSystemList.Add(s1);
        }
        public void AddPlanetToUniverse(float fertility, float minerals, float pop, bool explored, Vector2 pos)
        {
            AddDummyPlanet(fertility, minerals, pop, out Planet p);
            p.Center = pos;
            AddPlanetToUniverse(p, explored, pos);
        }

        void CreateTestEnv()
        {
            CreateUniverseAndPlayerEmpire(out TestEmpire);
            AddPlanetToUniverse(2, 2, 40000, true,Vector2.One);
            AddPlanetToUniverse(1.9f, 1.9f, 40000, true, new Vector2(5000));
            AddPlanetToUniverse(1.7f, 1.7f, 40000, true, new Vector2(-5000));
            for (int x = 0; x < 50; x++)
                AddPlanetToUniverse(0.1f, 0.1f, 1000, true, Vector2.One);
            AddHomeWorldToEmpire(TestEmpire, out P);
            AddPlanetToUniverse(P,true, Vector2.Zero);
            AddHomeWorldToEmpire(Enemy, out P);
            AddPlanetToUniverse(P, true, new Vector2(2000));
        }

        [TestMethod]
        public void TestExpansionPlannerColonize()
        {
            var expansionAI = TestEmpire.GetEmpireAI().ExpansionAI;
            TestEmpire.AutoColonize = true;
            expansionAI.RunExpansionPlanner();
            Assert.AreEqual(0, expansionAI.ColonizationTargets.Length,
                "All targets should have a colony goal");
            Assert.AreEqual(4, expansionAI.DesiredPlanets.Length,
                "There should be 4 planets that we want");
            Assert.AreEqual(5, expansionAI.RankedPlanets.Length,
                "unfiltered colonization targets should be 5");

            
            var markedPlanet = expansionAI.GetMarkedPlanets();
            Assert.AreEqual(3, markedPlanet.Length, "expected 3 colony goals ");

            //mock colonization success
            expansionAI.DesiredPlanets[0].Owner = TestEmpire;
            expansionAI.RunExpansionPlanner();
            Assert.AreEqual(3, expansionAI.DesiredPlanets.Length);
            Assert.AreEqual(5, expansionAI.RankedPlanets.Length);

        }
    }
}
