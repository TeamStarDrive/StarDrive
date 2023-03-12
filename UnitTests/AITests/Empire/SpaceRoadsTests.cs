using Microsoft.VisualStudio.TestTools.UnitTesting;
using SDGraphics;
using SDUtils;
using Ship_Game;
using Ship_Game.AI;
using Ship_Game.Gameplay;
using Ship_Game.Ships;
using Vector2 = SDGraphics.Vector2;

namespace UnitTests.AITests.Empire
{
    [TestClass]
    public class SpaceRoadsTests : StarDriveTest
    {
        SolarSystem System1;
        SolarSystem System2;
        SpaceRoad Road;
        SpaceRoadsManager Manager;

        void CreateSystemsAndPlanets(float distance)
        {
            CreateUniverseAndPlayerEmpire("Cordrazine");
            AddHomeWorldToEmpire(new Vector2(1000), Player).System.SetExploredBy(Player);
            UState.Objects.UpdateLists();
            AddDummyPlanetToEmpire(new Vector2(1000, distance), Player);
            System1 = Player.GetOwnedSystems()[0];
            System2 = Player.GetOwnedSystems()[1];
            float radius = Player.GetProjectorRadius();
            AssertEqual(80000, radius);
            Manager = Player.AI.SpaceRoadsManager;
        }

        void CreateSystemsAndRoad() // 5 projectors
        {
            CreateSystemsAndPlanets(800000);
            int numProjectors = SpaceRoad.GetNeededNumProjectors(System1, System2, Player);
            string name = SpaceRoad.GetSpaceRoadName(System1, System2);
            Road = new(System1, System2, Player, numProjectors, name, 
                Player.AI.SpaceRoadsManager.GetNonDownEmpireRoadNodes());
        }

        void CreateProjectorsAtRoad(int skipProjectorNodeNumber = -1)
        {
            for (int i = 0; i < Road.RoadNodesList.Count; i++)
            {
                if (i == skipProjectorNodeNumber)
                    continue; // leaving a gap in the road;

                RoadNode node = Road.RoadNodesList[i];
                Ship projector = Ship.CreateShipAtPoint(Player.Universe, "Subspace Projector", Player, node.Position);
                Assert.IsNotNull(projector, "Expected to get a projector from CreateShipAtPoint, got null");
                Manager.AddProjectorToRoadList(projector, node.Position);
            }
        }

        [TestMethod]
        public void TestNumProjectorsVeryShort()
        {
            CreateSystemsAndPlanets(100000);
            int numProjectors = SpaceRoad.GetNeededNumProjectors(System1, System2, Player);
            AssertEqual(0, numProjectors);
        }

        [TestMethod]
        public void TestNumProjectorsShort()
        {
            CreateSystemsAndPlanets(300000);
            int numProjectors = SpaceRoad.GetNeededNumProjectors(System1, System2, Player);
            AssertEqual(2, numProjectors);
        }

        [TestMethod]
        public void TestNumProjectorsLong()
        {
            CreateSystemsAndPlanets(1000000);
            int numProjectors = SpaceRoad.GetNeededNumProjectors(System1, System2, Player);
            AssertEqual(7, numProjectors);
        }

        [TestMethod]
        public void TestSpaceRoadUpdateMaintenance()
        {
            CreateSystemsAndRoad();
            float maint = ResourceManager.GetShipTemplate("Subspace Projector").GetMaintCost(Player);
            float expectedMaint = maint * Road.NumProjectors;
            Assert.IsTrue(expectedMaint.AlmostEqual(Road.OperationalMaintenance),
                $"Expected Operational Maintenance of {expectedMaint}, got {Road.OperationalMaintenance}");

            Player.data.Traits.MaintMod = 2;
            Road.UpdateMaintenance();
            maint = ResourceManager.GetShipTemplate("Subspace Projector").GetMaintCost(Player);
            expectedMaint = maint * Road.NumProjectors;
            Assert.IsTrue(expectedMaint.AlmostEqual(Road.OperationalMaintenance),
                $"Expected Operational Maintenance of {expectedMaint}, got {Road.OperationalMaintenance}");
        }

        [TestMethod]
        public void TestSpaceRoadNames()
        {
            CreateSystemsAndPlanets(100000);
            int id1 = System1.Id;
            int id2 = System2.Id;
            string name = SpaceRoad.GetSpaceRoadName(System1, System2);
            AssertEqual($"{id1}-{id2}", name);

            name = SpaceRoad.GetSpaceRoadName(System2, System1);
            AssertEqual($"{id1}-{id2}", name);
        }

        [TestMethod]
        public void TestSpaceRoadCreation()
        {
            CreateSystemsAndRoad();
            AssertEqual(SpaceRoad.SpaceRoadStatus.Down, Road.Status);

            AssertEqual(0, Road.Maintenance);
            AssertEqual(2, Road.Heat);
        }

        [TestMethod]
        public void TestSpaceRoadHeat()
        {
            CreateSystemsAndRoad();
            AssertEqual(Road.NumProjectors, 5);
            AssertEqual(2, Road.Heat);

            Road.CoolDown();
            Road.CoolDown();
            AssertEqual(0, Road.Heat);

            Road.AddHeat();
            AssertEqual(2, Road.Heat);

            // Add 0.5, knowing the base is 2 (so total of 2.5)
            Road.AddHeat(extraHeat: 0.5f);
            AssertEqual(4.5f, Road.Heat);
            Assert.IsFalse(Road.IsHot, "Road should not be hot");

            Road.AddHeat(extraHeat: 3.5f);
            AssertEqual(10f, Road.Heat);
            Assert.IsTrue(Road.IsHot, "Road should be hot");

            Road.AddHeat(extraHeat: 100);
            AssertEqual(15, Road.Heat);

            // Lower heat from 15 to -7
            for (int i = 1; i <= 22; i++)
                Road.CoolDown();

            Assert.IsTrue(Road.IsCold, "Road should be cold");
        }

        [TestMethod]
        public void TestSpaceRoadDeploy()
        {
            CreateSystemsAndRoad();
            AssertEqual(Road.NumProjectors, 5);
            Road.DeployAllProjectors(Player.AI.SpaceRoadsManager.GetNonDownEmpireRoadNodes());
            var goals = Player.AI.Goals.Filter(g => g.Type == GoalType.DeepSpaceConstruction);
            AssertEqual(SpaceRoad.SpaceRoadStatus.InProgress, Road.Status);

            AssertEqual(5, goals.Length);

            foreach (RoadNode node in Road.RoadNodesList)
            {
                // check that at least 1 construction goal has the related build pos per node
                Assert.IsTrue(Manager.NodeGoalAlreadyExistsFor(node.Position));
            }
        }

        [TestMethod]
        public void TestSpaceRoadFillGaps()
        {
            CreateSystemsAndRoad();
            Manager.SpaceRoads.Add(Road);
            AssertEqual(5, Road.NumProjectors);
            Road.DeployAllProjectors(Player.AI.SpaceRoadsManager.GetNonDownEmpireRoadNodes());
            AssertEqual(SpaceRoad.SpaceRoadStatus.InProgress, Road.Status);

            Player.AI.ClearGoals();
            int skipNodeIndex = 2;
            CreateProjectorsAtRoad(skipProjectorNodeNumber: skipNodeIndex);
            AssertEqual(SpaceRoad.SpaceRoadStatus.InProgress, Road.Status);

            // Check that projectors were added
            for (int i = 0; i < Road.RoadNodesList.Count; i++)
            {
                if (i == skipNodeIndex)
                    continue; // gap in the third node;

                RoadNode node = Road.RoadNodesList[i];
                Assert.IsNotNull(node.Projector);
            }

            Road.FillProjectorGaps();
            AssertEqual(1, Player.AI.Goals.Count);
            Goal constructionGoal = Player.AI.Goals[0];
            AssertEqual(GoalType.DeepSpaceConstruction, constructionGoal.Type);
            Assert.IsTrue(Manager.NodeGoalAlreadyExistsFor(Road.RoadNodesList[2].Position),
                $"Goal's build position is not near {Road.RoadNodesList[2].Position}");
        }

        [TestMethod]
        public void TestSpaceRoadScrap()
        {
            CreateSystemsAndRoad();
            Player.AutoBuildSpaceRoads = true;
            Player.CanBuildPlatforms = true;
            Manager.SpaceRoads.Add(Road);
            Player.AI.SSPBudget = 10;

            // Should not deploy projectors, as it was just created.
            AssertEqual(SpaceRoad.SpaceRoadStatus.Down, Road.Status);

            CreateProjectorsAtRoad();
            // The road should be online now, since after projector creation we add them to the road.
            AssertEqual(SpaceRoad.SpaceRoadStatus.Online, Road.Status);

            Ship projector = Road.RoadNodesList[0].Projector;
            Assert.IsNotNull(projector);
            projector.Die(null, cleanupOnly: true);

            // Node projector should be now null
            Assert.IsNull(Road.RoadNodesList[0].Projector, "Projector at node should be null, but is not");

            // Should now be in progress, since since there is a new gap
            AssertEqual(SpaceRoad.SpaceRoadStatus.InProgress, Road.Status);

            RoadNode node = Road.RoadNodesList[1];
            projector = node.Projector;
            Assert.IsNotNull(projector);
            Road.RemoveProjectorRef(projector);
            Assert.IsNull(node.Projector);
            Road.AddProjector(projector, node.Position);
            AssertEqual(node.Projector, projector);

            Manager.Update();
            Assert.IsFalse(Road.IsHot, "Road should not be hot now but it is hot");
            // Should still be in progress, since since there is a new gap
            // and no new goal should be added to fill the gap since the road is not hot
            AssertEqual(SpaceRoad.SpaceRoadStatus.InProgress, Road.Status);
            AssertEqual(0, Player.AI.Goals.Count);

            Road.AddHeat(extraHeat: 1000);
            Assert.IsTrue(Road.IsHot, "Road should be hot now but it is not hot enough");
            Manager.Update();
            // Should still be in progress, since since there is a new gap
            // but now a new goal should be added to fill the gap since the road is hot
            AssertEqual(SpaceRoad.SpaceRoadStatus.InProgress, Road.Status);
            AssertEqual(1, Player.AI.Goals.Count);

            Road.Scrap();
            foreach (RoadNode roadNode in Road.RoadNodesList)
                if (roadNode.Projector != null)
                    AssertEqual(roadNode.Projector.ScuttleTimer, 1);

            AssertEqual(Player.AI.Goals.Count, 0);
        }

        [TestMethod]
        public void TestInfraStructureLogic()
        {
            CreateSystemsAndRoad();
            Player.AutoBuildSpaceRoads = true;
            Player.CanBuildPlatforms= true;
            Manager.SpaceRoads.Add(Road);
            Player.AI.SSPBudget = 10;
            Manager.Update();

            // Should not deploy projectors, since the road is not hot
            AssertEqual(SpaceRoad.SpaceRoadStatus.Down, Road.Status);

            // Heat up the road
            Road.AddHeat(10);
            Assert.IsTrue(Road.IsHot, "Road should be hot");
            Player.AI.SSPBudget = Road.OperationalMaintenance - 0.1f;
            Manager.Update();

            // Should not deploy projectors, although the road it hot, since there is not budget
            AssertEqual(SpaceRoad.SpaceRoadStatus.Down, Road.Status);

            Player.AI.SSPBudget = Road.OperationalMaintenance + 1;
            Manager.Update();

            // Should now be deployed, since its hot and there is budget
            AssertEqual(SpaceRoad.SpaceRoadStatus.InProgress, Road.Status);

            Player.AI.SSPBudget = Road.OperationalMaintenance - 0.2f;
            Manager.Update();

            // All projectors should be removed from the road and the road list should be empty since 
            AssertEqual(0, Player.AI.Goals.Count);
            AssertEqual(0, Manager.SpaceRoads.Count);
        }
    }
}