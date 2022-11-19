using System;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Microsoft.Xna.Framework;
using SDGraphics;
using SDUtils;
using Ship_Game;
using Ship_Game.AI.Components;
using Ship_Game.Gameplay;
using Ship_Game.Ships;
using Ship_Game.Universe;
using Vector2 = SDGraphics.Vector2;

namespace UnitTests.AITests.Empire
{
    [TestClass]
    public class SpaceRoadsTests : StarDriveTest
    {
        SolarSystem System1;
        SolarSystem System2;
        SpaceRoad Road;

        void CreateSystemsAndPlanets(float distance)
        {
            CreateUniverseAndPlayerEmpire("Cordrazine");
            AddHomeWorldToEmpire(new Vector2(1000), Player).ParentSystem.SetExploredBy(Player);
            UState.Objects.UpdateLists();
            AddDummyPlanetToEmpire(new Vector2(1000, distance), Player);
            System1 = Player.GetOwnedSystems()[0];
            System2 = Player.GetOwnedSystems()[1];
        }

        void CreateSystemsAndRoad() // 5 projectors
        {
            CreateSystemsAndPlanets(800000);
            int numProjectors = SpaceRoad.GetNeededNumProjectors(System1, System2, Player);
            string name = SpaceRoad.GetSpaceRoadName(System1, System2);
            Road = new(System1, System2, Player, numProjectors, name);
        }


        [TestMethod]
        public void TestProjectorRadius()
        {
            CreateUniverseAndPlayerEmpire("Cordrazine");
            float radius = Player.GetProjectorRadius();
            Assert.AreEqual(radius, 80000, "expecting projector radius of 80,000 for SpaceRoad tests");
        }

        [TestMethod]
        public void TestNumProjectorsVeryShort()
        {
            CreateSystemsAndPlanets(100000);
            int numProjectors = SpaceRoad.GetNeededNumProjectors(System1, System2, Player);
            Assert.AreEqual(numProjectors, 0, $"Expecting 0 projectors, got {numProjectors}");
        }

        [TestMethod]
        public void TestNumProjectorsShort()
        {
            CreateSystemsAndPlanets(300000);
            int numProjectors = SpaceRoad.GetNeededNumProjectors(System1, System2, Player);
            Assert.AreEqual(numProjectors, 2, $"Expecting 2 projectors, got {numProjectors}");
        }

        [TestMethod]
        public void TestNumProjectorsLong()
        {
            CreateSystemsAndPlanets(1000000);
            int numProjectors = SpaceRoad.GetNeededNumProjectors(System1, System2, Player);
            Assert.AreEqual(numProjectors, 7, $"Expecting 7 projectors, got {numProjectors}");
        }

        [TestMethod]
        public void TestSpaceRoadNames()
        {
            CreateSystemsAndPlanets(100000);
            int id1 = System1.Id;
            int id2 = System2.Id;
            string name = SpaceRoad.GetSpaceRoadName(System1, System2);
            Assert.AreEqual(name, $"{id1}-{id2}");

            name = SpaceRoad.GetSpaceRoadName(System2, System1);
            Assert.AreEqual(name, $"{id1}-{id2}");
        }

        [TestMethod]
        public void TestSpaceRoadCreation()
        {
            CreateSystemsAndRoad();
            Assert.AreEqual(Road.Status, SpaceRoad.SpaceRoadStatus.Down, $"Road Status is {Road.Status}, while it should be Down");
            Assert.AreEqual(Road.Maintenance, 0, $"Maintenance should 0, since the Road is down, got {Road.Maintenance}");

            float maint = ResourceManager.GetShipTemplate("Subspace Projector").GetMaintCost(Player);
            float expectedMaint = maint * Road.NumProjectors;
            Assert.IsTrue(expectedMaint.AlmostEqual(Road.OperationalMaintenance),
                $"Expected Operational Maintenance of {expectedMaint}, got {Road.OperationalMaintenance}");

            Assert.AreEqual(Road.Heat, 2, $"Expected Road heat of 2, got {Road.Heat}");
        }

        [TestMethod]
        public void TestSpaceRoadHeat()
        {
            CreateSystemsAndRoad();
            Assert.AreEqual(Road.NumProjectors, 5, $"Expecting 5 projectors, got {Road.NumProjectors}");
            Assert.AreEqual(Road.Heat, 2, $"Expected Road head of 2, got {Road.Heat}");

            Road.CoolDown();
            Road.CoolDown();
            Assert.AreEqual(Road.Heat, 0, $"Expected Road heat of 0, got {Road.Heat}");

            Road.AddHeat();
            Assert.AreEqual(Road.Heat, 2, $"Expected Road heat of 2, got {Road.Heat}");

            Road.AddHeat(extraHeat: 0.5f);
            Assert.AreEqual(Road.Heat, 4.5f, $"Expected Road heat of 4.5, got {Road.Heat}");

            Road.AddHeat(extraHeat: 100);
            Assert.AreEqual(Road.Heat, 15, $"Expected Road heat of 15, got {Road.Heat}");

        }

        [TestMethod]
        public void TestSpaceRoadDeploy()
        {
            CreateSystemsAndRoad();
            Assert.AreEqual(Road.NumProjectors, 5, $"Expecting 5 projectors, got {Road.NumProjectors}");
            Road.DeployAllProjectors();
            var goals = Player.AI.Goals.Filter(g => g.Type == Ship_Game.AI.GoalType.DeepSpaceConstruction);
            Assert.AreEqual(goals.Length, 5, $"Expected 5 deep construction goals, got {goals.Length}");

            foreach (RoadNode node in Road.RoadNodesList)
            {
                // check that at least 1 construction goal has the related build pos per node
                Assert.IsTrue(Player.AI.NodeAlreadyExistsAt(node.Position));
            }
        }
    }
}