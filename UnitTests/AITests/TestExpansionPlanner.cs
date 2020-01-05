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
            Assert.IsTrue(expansionAI.ColonizationTargets.Length == 0);
            Assert.IsTrue(expansionAI.DesiredPlanets.Length == 4);
            Assert.IsTrue(expansionAI.RankedPlanets.Length == 5);
            var markedPlanet = expansionAI.GetMarkedPlanets();
            Assert.IsTrue(markedPlanet.Length == 3);
            expansionAI.DesiredPlanets[0].Owner = TestEmpire;
            expansionAI.RunExpansionPlanner();
            Assert.IsTrue(expansionAI.DesiredPlanets.Length == 3);
            Assert.IsTrue(expansionAI.RankedPlanets.Length == 5);

        }
    }
}
