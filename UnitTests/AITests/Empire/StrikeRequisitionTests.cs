using System.Reflection;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Ship_Game;
using Ship_Game.AI;
using Ship_Game.AI.Tasks;
using Vector2 = SDGraphics.Vector2;

namespace UnitTests.AITests.Empire
{
    // a strike is not formed when the fleet it gathers cannot take on the enemy around its target whole;
    // the AI's scout watching the target is in a fight there, so it stays out of the strike
    [TestClass]
    public class StrikeRequisitionTests : StarDriveTest
    {
        const string Warship = "Dreadnought mk1-a";
        const string Fighter = "Fang Strafer"; // the test empire's fleet ratios want fighters only
        const string Troopship = "Neutron Troopship Mk2";
        readonly Planet Target;
        readonly Planet EnemyHome;

        public StrikeRequisitionTests()
        {
            LoadStarterShips(Warship, Fighter, Troopship);
            CreateUniverseAndPlayerEmpire();
            Target = AddDummyPlanetToEmpire(new Vector2(1000), Player, 1f, 1f, 1f);
            EnemyHome = AddHomeWorldToEmpire(new Vector2(600_000), Enemy);
            SpawnShip("Vulcan Scout", Enemy, Target.Position + new Vector2(32_000, 0)); // sees the target's surroundings
        }

        void AIWarships(int count)
        {
            for (int i = 0; i < count; ++i)
                SpawnShip(Fighter, Enemy, EnemyHome.Position + new Vector2(10_000, i * 600));
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

            Assert.IsTrue(task.QueuedForRemoval, $"the strike ends instead of forming a fleet against a force it cannot take on whole ({Status(task)})");
            Assert.IsNull(task.Fleet, "no fleet is formed");
        }

        [TestMethod]
        public void AStrikeBeforeEitherEmpiresBudgetHasRunAsksForAStrengthItCanMeasure()
        {
            AIWarships(6);
            PlayerShipsNearTarget("Vulcan Scout", 1);
            Assert.IsTrue(Enemy.AI.BuildCapacity == 0f && Player.AI.BuildCapacity == 0f, "setup: neither empire's budget has run");

            MilitaryTask task = EvaluatedStrike();

            Assert.IsFalse(float.IsNaN(task.MinimumTaskForceStrength), $"the strike asks for a strength it can measure a fleet against ({Status(task)})");
        }

        static void SetBuildCapacity(Ship_Game.Empire empire, float capacity)
            => typeof(EmpireAI).GetProperty(nameof(EmpireAI.BuildCapacity))!.GetSetMethod(nonPublic: true)!.Invoke(empire.AI, new object[] { capacity });

        [TestMethod]
        [DataRow(0f, 500f, 1f, DisplayName = "the AI's budget has not run yet")]
        [DataRow(500f, 0f, 1f, DisplayName = "its target's budget has not run yet")]
        [DataRow(1000f, 500f, 0.5f, DisplayName = "both have run, the target building half as much")]
        public void AStrikesStrengthIsCappedByBuildCapacityOnlyOnceBothBudgetsHaveRun(float ours, float theirs, float cap)
        {
            SetBuildCapacity(Enemy, ours);
            SetBuildCapacity(Player, theirs);
            var task = new MilitaryTask(Target, Enemy, MilitaryTaskImportance.Normal) { TargetEmpire = Player };

            float multiplier = (float)typeof(MilitaryTask).GetMethod("GetBuildCapacityMultiplier", BindingFlags.Instance | BindingFlags.NonPublic)!.Invoke(task, null);

            AssertEqual(0.0001f, cap, multiplier, "a capacity not yet set leaves the strength asked for as it is");
        }

        [TestMethod]
        public void AStrikeOnRebelsAsksForTheStrengthItFindsWhateverTheirBudget()
        {
            Ship_Game.Empire rebels = UState.CreateRebelsFromEmpireData(Player.data, Player);
            Assert.IsTrue(rebels.data.IsRebelFaction && rebels.IsFaction, "setup: rebels are a faction");
            SetBuildCapacity(Enemy, 1000f);
            SetBuildCapacity(rebels, 5f); // as after a load gives them one step of a budget they never use
            var task = new MilitaryTask(Target, Enemy, MilitaryTaskImportance.Normal) { TargetEmpire = rebels };

            float multiplier = (float)typeof(MilitaryTask).GetMethod("GetBuildCapacityMultiplier", BindingFlags.Instance | BindingFlags.NonPublic)!.Invoke(task, null);

            AssertEqual(0.0001f, 1f, multiplier, "as for any faction, the rebels' capacity, saved or not, does not cap the strength asked for");
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
