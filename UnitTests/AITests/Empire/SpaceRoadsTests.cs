using System;
using System.Linq;
using System.Security.Cryptography;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Microsoft.Xna.Framework;
using SDGraphics;
using SDUtils;
using Ship_Game;
using Ship_Game.AI;
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

        void RemoveGoals()
        {
            for (int i = Player.AI.Goals.Count - 1; i >= 0; i--)
            {
                Goal g = Player.AI.Goals[i];
                Player.AI.RemoveGoal(g);
            }
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
                Player.AI.AddProjectorToRoadList(projector, node.Position);
            }
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
            Assert.AreEqual(name, $"{id1}-{id2}");

            name = SpaceRoad.GetSpaceRoadName(System2, System1);
            Assert.AreEqual(name, $"{id1}-{id2}");
        }

        [TestMethod]
        public void TestSpaceRoadCreation()
        {
            CreateSystemsAndRoad();
            Assert.AreEqual(Road.Status, SpaceRoad.SpaceRoadStatus.Down, 
                $"Road Status is {Road.Status}, while it should be Down");

            Assert.AreEqual(Road.Maintenance, 0, $"Maintenance should 0, since the Road is down, got {Road.Maintenance}");
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

            // Add 0.5, knowing the base is 2 (so total of 2.5)
            Road.AddHeat(extraHeat: 0.5f);
            Assert.AreEqual(Road.Heat, 4.5f, $"Expected Road heat of 4.5, got {Road.Heat}");
            Assert.IsFalse(Road.IsHot, "Road should not be hot");

            Road.AddHeat(extraHeat: 3.5f);
            Assert.AreEqual(Road.Heat, 10f, $"Expected Road heat of 10, got {Road.Heat}");
            Assert.IsTrue(Road.IsHot, "Road should be hot");

            Road.AddHeat(extraHeat: 100);
            Assert.AreEqual(Road.Heat, 15, $"Expected Road heat of 15, got {Road.Heat}");

            // Lower heat from 15 to -10
            for (int i = 1; i <= 25; i++)
                Road.CoolDown();

            Assert.IsTrue(Road.IsCold, "Road should be cold");
        }

        [TestMethod]
        public void TestSpaceRoadDeploy()
        {
            CreateSystemsAndRoad();
            Assert.AreEqual(Road.NumProjectors, 5, $"Expecting 5 projectors, got {Road.NumProjectors}");
            Road.DeployAllProjectors();
            var goals = Player.AI.Goals.Filter(g => g.Type == Ship_Game.AI.GoalType.DeepSpaceConstruction);
            Assert.AreEqual(Road.Status, SpaceRoad.SpaceRoadStatus.InProgress, 
                $"Road Status is {Road.Status}, while it should be In Progress");

            Assert.AreEqual(goals.Length, 5, $"Expected 5 deep construction goals, got {goals.Length}");

            foreach (RoadNode node in Road.RoadNodesList)
            {
                // check that at least 1 construction goal has the related build pos per node
                Assert.IsTrue(Player.AI.NodeAlreadyExistsAt(node.Position));
            }
        }

        [TestMethod]
        public void TestSpaceRoadFillGaps()
        {
            CreateSystemsAndRoad();
            Player.AI.SpaceRoads.Add(Road);
            Assert.AreEqual(Road.NumProjectors, 5, $"Expecting 5 projectors, got {Road.NumProjectors}");
            Road.DeployAllProjectors();
            Assert.AreEqual(Road.Status, SpaceRoad.SpaceRoadStatus.InProgress, 
                $"Road Status is {Road.Status}, while it should be In Progress");

            RemoveGoals();
            int skipNodeIndex = 2;
            CreateProjectorsAtRoad(skipProjectorNodeNumber: skipNodeIndex);
            Road.RecalculateStatus();
            Assert.AreEqual(Road.Status, SpaceRoad.SpaceRoadStatus.InProgress,
                $"Road Status is {Road.Status}, while it should be still In Progress");

            // Check that projectors were added
            for (int i = 0; i < Road.RoadNodesList.Count; i++)
            {
                if (i == skipNodeIndex)
                    continue; // gap in the third node;

                RoadNode node = Road.RoadNodesList[i];
                Assert.IsNotNull(node.Projector);
            }

            Road.FillGaps();
            Assert.AreEqual(Player.AI.Goals.Count, 1, $"Expected 1 goal, got {Player.AI.Goals.Count}");
            Goal constructionGoal = Player.AI.Goals[0];
            Assert.AreEqual(constructionGoal.Type, GoalType.DeepSpaceConstruction);
            Assert.IsTrue(Player.AI.NodeAlreadyExistsAt(Road.RoadNodesList[2].Position),
                $"Goal's build position is not near {Road.RoadNodesList[2].Position}");
        }

        [TestMethod]
        public void TestSpaceRoadScrap()
        {
            CreateSystemsAndRoad();
            Player.AutoBuildSpaceRoads = true;
            Player.CanBuildPlatforms = true;
            Player.AI.SpaceRoads.Add(Road);
            Player.AI.SSPBudget = 10;

            // Should not deploy projectors, as it was just created.
            Assert.AreEqual(Road.Status, SpaceRoad.SpaceRoadStatus.Down,
                $"Space Road status should be Down now, but it is {Road.Status}");

            CreateProjectorsAtRoad();
            // The road should be online now, since after projector creation we add them to the road.
            Assert.AreEqual(Road.Status, SpaceRoad.SpaceRoadStatus.Online, 
                $"Road Status is {Road.Status}, while it should be still Online");

            Ship projector = Road.RoadNodesList[0].Projector;
            Assert.IsNotNull(projector);
            projector.Die(null, cleanupOnly: true);

            // Node projector should be now null
            Assert.IsNull(Road.RoadNodesList[0].Projector, "Projector at node should be null, but is not");

            // Should now be in progress, since since there is a new gap
            Assert.AreEqual(Road.Status, SpaceRoad.SpaceRoadStatus.InProgress,
                $"Space Road status should be In Progress now, but it is {Road.Status}");

            RoadNode node = Road.RoadNodesList[1];
            projector = node.Projector;
            Assert.IsNotNull(projector);
            Road.RemoveProjectorAtNode(node);
            Assert.IsNull(node.Projector);
            Road.SetProjectorAtNode(node, projector);
            Assert.AreEqual(node.Projector, projector);

            Player.AI.TestRunInfrastructurePlanner();
            // Should still be in progress, since since there is a new gap
            // but now a new goal should be added to fill the gap
            Assert.AreEqual(Road.Status, SpaceRoad.SpaceRoadStatus.InProgress,
                $"Space Road status should be In Progress now, but it is {Road.Status}");

            Assert.AreEqual(Player.AI.Goals.Count, 1,
                $"There should be 1 goal, but found {Player.AI.Goals.Count}");

            Array<Goal> goals = Player.AI.Goals.ToArrayList();
            Road.Scrap(goals);
            foreach (RoadNode roadNode in Road.RoadNodesList)
                if (roadNode.Projector != null)
                    Assert.AreEqual(roadNode.Projector.ScuttleTimer, 1);

            Assert.AreEqual(Player.AI.Goals.Count, 0,
                $"There should be 0 goals, but found {Player.AI.Goals.Count}");

        }

        [TestMethod]
        public void TestInfraStructureLogic()
        {
            CreateSystemsAndRoad();
            Player.AutoBuildSpaceRoads = true;
            Player.CanBuildPlatforms= true;
            Player.AI.SpaceRoads.Add(Road);
            Player.AI.SSPBudget = 10;
            Player.AI.TestRunInfrastructurePlanner();

            // Should not deploy projectors, since the road is not hot
            Assert.AreEqual(Road.Status, SpaceRoad.SpaceRoadStatus.Down,
                $"Space Road status should be Down now, but it is {Road.Status}");

            // Heat up the road
            Road.AddHeat(10);
            Assert.IsTrue(Road.IsHot, "Road should be hot");
            Player.AI.SSPBudget = Road.OperationalMaintenance - 0.1f;
            Player.AI.TestRunInfrastructurePlanner();

            // Should not deploy projectors, although the road it hot, since there is not budget
            Assert.AreEqual(Road.Status, SpaceRoad.SpaceRoadStatus.Down,
                $"Space Road status should be Down now, but it is {Road.Status}");

            Player.AI.SSPBudget = Road.OperationalMaintenance + 1;
            Player.AI.TestRunInfrastructurePlanner();

            // Should now be deployed, since its hot and there is budget
            Assert.AreEqual(Road.Status, SpaceRoad.SpaceRoadStatus.InProgress,
                $"Space Road status should be In Progress now, but it is {Road.Status}");

            Player.AI.SSPBudget = Road.OperationalMaintenance - 0.2f;
            Player.AI.TestRunInfrastructurePlanner();

            // All projectors should be removed from the road and the road list should be empty since 
            Assert.AreEqual(Player.AI.Goals.Count, 0, 
                $"No goals should be found after scrap road, if it was ran, but found {Player.AI.Goals.Count}");
            Assert.AreEqual(Player.AI.SpaceRoads.Count, 0, 
                $"No roads should be found in roads list after scrap road, if it was ran, but found {Player.AI.SpaceRoads.Count}");
        }
    }
}