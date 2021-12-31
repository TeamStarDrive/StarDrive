using Microsoft.VisualStudio.TestTools.UnitTesting;
using Microsoft.Xna.Framework;
using Ship_Game;
using Ship_Game.Ships;

namespace UnitTests.AITests.Ships
{
    [TestClass]
    public class TestShipExploration : StarDriveTest
    {
        readonly SolarSystem CloseSystem;
        readonly SolarSystem FarSystem;

        public TestShipExploration()
        {
            CreateUniverseAndPlayerEmpire();
            GlobalStats.ExtraPlanets     = 1; // Ensures there is at least one planet to explore
            GlobalStats.DisableAsteroids = true; // Ensures no asteroids will be created instead of a planet

            CloseSystem = new SolarSystem();
            CloseSystem.Position = new Vector2(300000, 0); 
            CloseSystem.GenerateRandomSystem("Close System", 1);
            Universe.AddSolarSystem(CloseSystem);

            FarSystem = new SolarSystem();
            FarSystem.Position = new Vector2(600000, 0);
            FarSystem.GenerateRandomSystem("Far System", 1);
            Universe.AddSolarSystem(FarSystem);

            foreach (Planet planet in CloseSystem.PlanetList)
                planet.RecreateSceneObject(); // needed for object update

            foreach (Planet planet in FarSystem.PlanetList)
                planet.RecreateSceneObject(); // needed for object update
        }

        [TestMethod]
        public void TestScoutShipExploringClosestSystem()
        {
            Assert.IsFalse(CloseSystem.IsExploredBy(Player), "Close system should not be already explored.");
            Assert.IsFalse(FarSystem.IsExploredBy(Player), "Far system should not be already explored.");
            Ship scout = SpawnShip("Vulcan Scout", Player, Vector2.Zero);

            scout.DoExplore();
            Assert.IsTrue(scout.AI.State == Ship_Game.AI.AIState.Explore, "Scout is not in explore AI state.");
            Ship_Game.AI.ShipAI.Plan plan = scout.AI.OrderQueue.PeekFirst.Plan;
            Assert.IsTrue(plan == Ship_Game.AI.ShipAI.Plan.Explore, $"Plan should be Explore after ordering to explore and it is {plan}");
            Assert.IsNull(scout.AI.ExplorationTarget, "Exploration system should not be selected at this stage");

            scout.AI.DoExplore(TestSimStep);
            Assert.IsTrue(scout.AI.HasPriorityOrder, "Priority Order is not True after executing exploration.");
            Assert.IsTrue(scout.AI.IgnoreCombat, "Ignore Combat is not True after executing exploration and it must be.");
            Assert.IsTrue(scout.AI.ExplorationTarget == CloseSystem, $"Scout set to explore {scout.AI.ExplorationTarget}" +
                                                                    $" but it should explore {CloseSystem.Name}");

            scout.AI.DoExplore(TestSimStep);
            Assert.IsFalse(CloseSystem.IsExploredBy(Player), $"{CloseSystem.Name} is set as explored but it should not be explored" +
                                                             " by the ship since it is over 75000");
            Assert.IsNotNull(scout.AI.TestGetPatrolTarget(), "Patrol target planet is not set, but it should at this stage");

            Planet closestPlanet   = CloseSystem.PlanetList.FindMin(p => p.Center.SqDist(scout.Position));
            Planet planetToExplore = scout.AI.TestGetPatrolTarget();
            Assert.AreSame(closestPlanet, planetToExplore, "Scout did not target the closest planet for exploration.");

            // Move the scout into the system below 75000 from the explored planet so it should mark the system as
            // explored (not fully explored, though).
            scout.Position = planetToExplore.Center.GenerateRandomPointInsideCircle(70000); 
            Universe.Objects.Update(TestSimStep);
            scout.AI.DoExplore(TestSimStep);
            Assert.IsTrue(CloseSystem.IsExploredBy(Player), $"{CloseSystem.Name} is not set as explored but it should be explored" +
                                                             " as the ship is within 75000 of system center");
        }

        [TestMethod]
        public void TestScoutShipRunAwayFromEnemy()
        {
            Ship scout = SpawnShip("Vulcan Scout", Player, CloseSystem.Position);
            Ship enemy = SpawnShip("Rocket Scout", Enemy, new Vector2(CloseSystem.Position.X + 5000, CloseSystem.Position.Y));

            scout.DoExplore();
            scout.AI.DoExplore(TestSimStep); // First get the system to explore

            Universe.Objects.Update(TestSimStep);
            CloseSystem.ShipList.Add(enemy);
            CloseSystem.ShipList.Add(scout);
            scout.SetSystem(CloseSystem);
            Assert.IsTrue(enemy.AI.Target == scout, "Enemy's target is not the scout ship");

            // Scout should now detect it is being targeted, find an escape vector and
            // a final move position out of the system, and then add a new exploration order
            // to the queue in order to resume exploring
            scout.AI.DoExplore(TestSimStep); 
            Assert.AreEqual(3, scout.AI.OrderQueue.Count, "Scout should have 1 rotate order for the escape vector," +
                                                          " 1 move order and 1 explore order in the end of the queue.");
            Assert.IsTrue(scout.AI.OrderQueue.PeekLast.Plan == Ship_Game.AI.ShipAI.Plan.Explore, "Scout last order should be exploration but it is not.");
        }
    }
}
