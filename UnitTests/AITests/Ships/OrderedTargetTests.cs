using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using SDUtils;
using Ship_Game.Empires;
using Ship_Game.Ships;
using UnitTests.Ships;
using Vector2 = SDGraphics.Vector2;

namespace UnitTests.AITests.Ships;

/// <summary>
/// A ship told to attack takes the next queued target when its target falls, in the order they were queued, passing
/// over one it may not attack, and with none left picks its own, nearest first, as a target nobody gave it. A ship's
/// list of enemies runs nearest first.
/// </summary>
[TestClass]
public class OrderedTargetTests : StarDriveTest
{
    const string ShipName = "Vulcan Scout";
    const string ResearchStation = "Basic Research Station";
    static readonly Vector2 At = new(200_000, 200_000);
    readonly TestShip Us;

    public OrderedTargetTests()
    {
        LoadStarterShips(ShipName, ResearchStation);
        CreateUniverseAndPlayerEmpire();
        UState.Objects.EnableParallelUpdate = false;
        Us = SpawnShip(ShipName, Player, At);
        Us.AI.SetCombatTriggerDelay(1000f);
    }

    TestShip EnemyAt(Vector2 offset)
    {
        TestShip enemy = SpawnShipNoCombatHoldPos(ShipName, Enemy, At + offset);
        enemy.AI.SetCombatTriggerDelay(1000f);
        return enemy;
    }

    void Scanned()
    {
        RunObjectsSim(EmpireConstants.EnemyScanInterval * 1.5f);
        Assert.IsTrue(Us.AI.PotentialTargets.Length > 0, "setup: our ship has scanned the enemies");
    }

    [TestMethod]
    public void WhenItsTargetFallsTheShipTakesTheNextQueuedOneInOrder()
    {
        TestShip first = EnemyAt(new Vector2(2000, 0));
        TestShip second = EnemyAt(new Vector2(0, 4000));
        TestShip third = EnemyAt(new Vector2(-3000, 0));
        Scanned();
        Us.AI.OrderAttackSpecificTarget(first);
        Us.AI.OrderQueueSpecificTarget(second);
        Us.AI.OrderQueueSpecificTarget(third);
        RunObjectsSim(TestSimStep);
        Assert.AreSame(first, Us.AI.Target, "setup: the ship attacks the first target");

        first.Die(first, cleanupOnly: true);
        RunObjectsSim(0.1f);
        Assert.AreSame(second, Us.AI.Target, "it takes the next target queued, not the nearest enemy");
        Assert.IsTrue(Us.AI.HasPriorityTarget, "as one it was given");
        Assert.IsTrue(Us.AI.Intercepting, "and goes for it");

        second.Die(second, cleanupOnly: true);
        RunObjectsSim(0.1f);
        Assert.AreSame(third, Us.AI.Target, "and then the one after it, in the order they were queued");
    }

    [TestMethod]
    public void WhenItsTargetIsCapturedTheShipTakesTheNextQueuedOne()
    {
        TestShip first = EnemyAt(new Vector2(2000, 0));
        TestShip second = EnemyAt(new Vector2(0, 3000));
        Scanned();
        Us.AI.OrderAttackSpecificTarget(first);
        Us.AI.OrderQueueSpecificTarget(second);
        RunObjectsSim(TestSimStep);
        Assert.AreSame(first, Us.AI.Target, "setup: the ship attacks the first target");

        first.LoyaltyChangeFromBoarding(Player, addNotification: false);
        RunObjectsSim(EmpireConstants.EnemyScanInterval * 1.5f);

        Assert.AreSame(Player, first.Loyalty, "setup: the first target is now one of our ships");
        Assert.AreSame(second, Us.AI.Target, "the ship takes the next target queued");
        Assert.IsTrue(Us.AI.HasPriorityTarget, "as one it was given");
        Assert.IsFalse(Us.AI.TargetQueue.ContainsRef(first), "and drops the captured ship from its queue");
    }

    [TestMethod]
    public void AQueuedShipOfAnEmpireItMayNotAttackIsPassedOver()
    {
        CreateThirdMajorEmpire();
        TestShip first = EnemyAt(new Vector2(2000, 0));
        TestShip neutral = SpawnShipNoCombatHoldPos(ShipName, ThirdMajor, At + new Vector2(0, 2500));
        TestShip last = EnemyAt(new Vector2(-4000, 0));
        Scanned();
        Assert.IsFalse(Player.IsEmpireAttackable(ThirdMajor, neutral), "setup: we are not at war with them");
        Us.AI.OrderAttackSpecificTarget(first);
        Us.AI.OrderQueueSpecificTarget(neutral);
        Us.AI.OrderQueueSpecificTarget(last);
        RunObjectsSim(TestSimStep);
        Assert.AreSame(first, Us.AI.Target, "setup: the ship attacks the first target");

        first.Die(first, cleanupOnly: true);
        RunObjectsSim(0.1f);

        Assert.AreSame(last, Us.AI.Target, "it passes over a ship it may not attack and takes the next one queued");
        Assert.IsTrue(Us.AI.HasPriorityTarget, "as one it was given");
    }

    [TestMethod]
    public void AQueuedResearchStationOfAnEmpireWeAreNotAtWarWithIsPassedOver()
    {
        CreateThirdMajorEmpire();
        Player.GetRelations(ThirdMajor).CanAttack = true; // angry enough to fire on its ships, not at war
        TestShip first = EnemyAt(new Vector2(2000, 0));
        TestShip station = SpawnShip(ResearchStation, ThirdMajor, At + new Vector2(0, 2500));
        TestShip last = EnemyAt(new Vector2(-4000, 0));
        Scanned();
        Assert.IsTrue(station.IsResearchStation && !Player.IsAtWarWith(ThirdMajor), "setup: their research station, and no war");
        Us.AI.OrderAttackSpecificTarget(first);
        Us.AI.OrderQueueSpecificTarget(station);
        Us.AI.OrderQueueSpecificTarget(last);
        RunObjectsSim(TestSimStep);
        Assert.AreSame(first, Us.AI.Target, "setup: the ship attacks the first target");

        first.Die(first, cleanupOnly: true);
        RunObjectsSim(0.1f);

        Assert.AreSame(last, Us.AI.Target, "a research station is spared outside war, so the ship takes the next one queued");
    }

    [TestMethod]
    public void AQueuedEnemyShipTakingOffIsStillTheNextTarget()
    {
        TestShip first = EnemyAt(new Vector2(2000, 0));
        TestShip second = EnemyAt(new Vector2(0, 3000));
        Scanned();
        Us.AI.OrderAttackSpecificTarget(first);
        Us.AI.OrderQueueSpecificTarget(second);
        RunObjectsSim(TestSimStep);
        Assert.AreSame(first, Us.AI.Target, "setup: the ship attacks the first target");

        second.InitLaunch(LaunchPlan.Planet, 0f);
        Assert.IsTrue(second.IsLaunching, "setup: the queued enemy is taking off");
        first.Die(first, cleanupOnly: true);
        RunObjectsSim(0.1f);

        Assert.AreSame(second, Us.AI.Target, "an enemy ship taking off is still the next target");
        Assert.IsTrue(Us.AI.HasPriorityTarget, "as one it was given");
    }

    [TestMethod]
    public void WhenItsLastTargetFallsTheShipPicksItsOwn()
    {
        TestShip near = EnemyAt(new Vector2(1500, 0));
        TestShip ordered = EnemyAt(new Vector2(0, 2500));
        EnemyAt(new Vector2(-9000, 0));
        Scanned();
        Us.AI.OrderAttackSpecificTarget(ordered);
        RunObjectsSim(TestSimStep);
        Assert.AreSame(ordered, Us.AI.Target, "setup: the ship attacks the target it was given");

        ordered.Die(ordered, cleanupOnly: true);
        RunObjectsSim(0.1f);

        Assert.IsFalse(Us.AI.HasPriorityTarget, "its next target is its own pick, not one it was given");
        Assert.IsFalse(Us.AI.Intercepting, "so it does not fly straight at it");
        Assert.AreSame(near, Us.AI.Target, "it takes on the enemy nearest to it");
    }

    [TestMethod]
    public void AShipsListOfEnemiesRunsNearestFirst()
    {
        float[] distances = { 9000, 1500, 6000, 3000, 12000, 4500, 7500, 2000 };
        Vector2[] directions = { new(1, 0), new(0, 1), new(-1, 0), new(0, -1), new(0.7f, 0.7f), new(-0.7f, 0.7f), new(-0.7f, -0.7f), new(0.7f, -0.7f) };
        for (int i = 0; i < distances.Length; ++i)
            EnemyAt(directions[i] * distances[i]);
        AssertGreaterThan(Us.SensorRange, 13000f, "setup: our ship sees them all");
        RunObjectsSim(TestSimStep);

        Us.AI.ScanForEnemies(Us, Us.SensorRange);

        float[] found = Us.AI.PotentialTargets.Select(t => t.Position.Distance(Us.Position)).ToArray();
        AssertEqual(distances.Length, found.Length, "every enemy is on the list");
        for (int i = 1; i < found.Length; ++i)
            AssertLessThan(found[i - 1], found[i], "nearest first, so a weapon that tracks only the first few looks at the closest");
    }
}
