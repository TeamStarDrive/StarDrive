using System.Reflection;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Ship_Game;
using Ship_Game.AI;
using Ship_Game.AI.Tasks;
using Vector2 = SDGraphics.Vector2;

namespace UnitTests.AITests.Empire
{
    // a strike is not formed when the fleet it gathers would call itself off at once against the enemy around its target
    [TestClass]
    public class StrikeRequisitionTests : StarDriveTest
    {
        const string Warship = "Dreadnought mk1-a";
        const string Troopship = "Neutron Troopship Mk2";
        readonly Planet Target;
        readonly Planet EnemyHome;

        public StrikeRequisitionTests()
        {
            LoadStarterShips(Warship, Troopship);
            CreateUniverseAndPlayerEmpire();
            Target = AddDummyPlanetToEmpire(new Vector2(1000), Player, 1f, 1f, 1f);
            EnemyHome = AddHomeWorldToEmpire(new Vector2(600_000), Enemy);
            SpawnShip("Vulcan Scout", Enemy, Target.Position + new Vector2(32_000, 0)); // sees the target's surroundings
        }

        void AIWarships(int count)
        {
            for (int i = 0; i < count; ++i)
                SpawnShip(Warship, Enemy, EnemyHome.Position + new Vector2(10_000, i * 600));
            for (int i = 0; i < 2; ++i)
                SpawnShip(Troopship, Enemy, EnemyHome.Position + new Vector2(-10_000, i * 600));
        }

        void PlayerShipsNearTarget(string shipName, int count)
        {
            for (int i = 0; i < count; ++i)
                SpawnShip(shipName, Player, Target.Position + new Vector2(30_000, i * 600));
        }

        string Status(MilitaryTask task)
            => typeof(MilitaryTask).GetField("ReqStatus", BindingFlags.Instance | BindingFlags.NonPublic)?.GetValue(task)
               + $" troopsNeeded={task.NeededTroopStrength} troopsHave={Enemy.ShipsReadyForFleet.InvasionTroopStrength}"
               + $" ships={Enemy.ShipsReadyForFleet.TotalShips}";

        MilitaryTask EvaluatedStrike()
        {
            UState.Objects.Update(new(time: 2f));
            Enemy.Threats.Update(new(time: 2f));
            float minWarpRange = GlobalStats.Defaults.MinAcceptableShipWarpRange;
            try
            {
                GlobalStats.Defaults.MinAcceptableShipWarpRange = 0; // the test troopships' range is short
                Enemy.ShipsReadyForFleet = new FleetShips(Enemy, Enemy.AllFleetReadyShips()); // as the empire's update does
                Enemy.CreateWarTask(Target, Player, null, out MilitaryTask task);
                Assert.AreEqual(MilitaryTask.TaskType.StrikeForce, task.Type, "setup: the first war task is a strike");
                Assert.IsTrue(Enemy.Threats.GetHostileStrengthAt(Target.Position, Ship_Game.Fleets.Fleet.AssaultEnemyScanRadius) > 0,
                              "setup: the AI knows the player's ships near the target");
                Assert.AreEqual(0f, Enemy.Threats.GetHostileStrengthAt(Target.Position, task.AORadius),
                                "setup: the strike's own look at the planet finds none of them");
                task.Evaluate(Enemy);
                return task;
            }
            finally
            {
                GlobalStats.Defaults.MinAcceptableShipWarpRange = minWarpRange;
            }
        }

        [TestMethod]
        public void AStrikeOutmatchedAroundItsTargetIsNotFormed()
        {
            AIWarships(1);
            PlayerShipsNearTarget(Warship, 6);

            MilitaryTask task = EvaluatedStrike();

            Assert.IsTrue(task.QueuedForRemoval, $"the strike ends instead of forming a fleet that would call itself off ({Status(task)})");
            Assert.IsNull(task.Fleet, "no fleet is formed");
        }

        [TestMethod]
        public void AStrikeStrongerThanTheEnemyAroundItsTargetIsFormed()
        {
            AIWarships(6);
            PlayerShipsNearTarget("Vulcan Scout", 1);

            MilitaryTask task = EvaluatedStrike();

            Assert.IsFalse(task.QueuedForRemoval, $"the fleet it gathers can take the fight, so the strike goes on ({Status(task)})");
            Assert.IsNotNull(task.Fleet, $"setup: the strike formed its fleet ({Status(task)})");
        }
    }
}
