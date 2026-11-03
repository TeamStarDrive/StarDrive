using System.Reflection;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using SDUtils;
using Ship_Game;
using Ship_Game.AI;
using Ship_Game.AI.Tasks;
using Ship_Game.Ships;
using Vector2 = SDGraphics.Vector2;

namespace UnitTests.AITests.Empire;

// the troops and bombing time an AI task asks for a planet grow with the planet's level
[TestClass]
public class InvasionNeedsByLevelTests : StarDriveTest
{
    const string Bomber = "Neutron Bomber Mk1";
    const string Fighter = "TEST_Vulcan Scout";
    readonly Planet Target;

    public InvasionNeedsByLevelTests()
    {
        LoadStarterShips(Bomber, Fighter);
        CreateUniverseAndPlayerEmpire();
        Target = AddHomeWorldToEmpire(new Vector2(200_000), Player);
        for (int i = 0; i < 6; ++i)
            Assert.IsTrue(ResourceManager.CreateTroop("Wyvern", Player).TryLandTroop(Target), "test setup: a defending troop lands");
        Enemy.ShipsReadyForFleet = new FleetShips(Enemy, Enemy.AllFleetReadyShips());
    }

    void SetLevel(int level) => typeof(Planet).GetProperty(nameof(Planet.Level))!.SetValue(Target, level);

    int TroopsAskedAtLevel(int level)
    {
        SetLevel(level);
        var task = new MilitaryTask(Target, Enemy, MilitaryTaskImportance.Normal);
        task.GetMoreTroops(Target, out _);
        return task.NeededTroopStrength;
    }

    int BombTimeAtLevel(string method, int level)
    {
        SetLevel(level);
        typeof(Planet).GetProperty(nameof(Planet.TotalDefensiveStrength))!.SetValue(Target, 400);
        var task = new MilitaryTask(Target, Enemy, MilitaryTaskImportance.Normal);
        return (int)typeof(MilitaryTask).GetMethod(method, BindingFlags.Instance | BindingFlags.NonPublic)!.Invoke(task, null)!;
    }

    [TestMethod]
    public void TheTroopsAskedForAPlanetGrowWithItsLevel()
    {
        Assert.IsTrue(Target.GetGroundStrengthOther(Enemy) * Target.InvasionResistance > 40f,
                      "test setup: the planet's ground strength is above the least a task asks for");
        int atLevel1 = TroopsAskedAtLevel(1);
        int atLevel4 = TroopsAskedAtLevel(4);
        AssertEqual(0.05f, 4f, atLevel4 / (float)atLevel1, $"level 1 asks {atLevel1}, level 4 asks {atLevel4}");
    }

    [TestMethod]
    public void APlanetWithNoLevelYetIsAskedForAsLevelOne()
    {
        Assert.IsTrue(Target.GetGroundStrengthOther(Enemy) * Target.InvasionResistance > 40f,
                      "test setup: the planet's ground strength is above the least a task asks for");
        AssertEqual(TroopsAskedAtLevel(1), TroopsAskedAtLevel(0), "a planet nobody has owned yet");
    }

    // ships ready for a fleet, though the test ships' warp range is short
    FleetShips ReadyForFleet(params Ship[] ships)
    {
        float minWarpRange = GlobalStats.Defaults.MinAcceptableShipWarpRange;
        try
        {
            GlobalStats.Defaults.MinAcceptableShipWarpRange = 0;
            var ready = new Array<Ship>();
            foreach (Ship s in ships)
                ready.Add(s);
            return new FleetShips(Enemy, ready);
        }
        finally
        {
            GlobalStats.Defaults.MinAcceptableShipWarpRange = minWarpRange;
        }
    }

    [TestMethod]
    public void ATaskShortOfBombersTakesThoseOnHandAndTroopsMakeUpTheRest()
    {
        Enemy.ShipsReadyForFleet = ReadyForFleet(SpawnShip(Bomber, Enemy, new Vector2(-200_000)));
        int onHand = Enemy.ShipsReadyForFleet.BombSecsAvailable;
        Assert.IsTrue(onHand > 0, "test setup: the bomber is ready for a fleet");

        var task = new MilitaryTask(Target, Enemy, MilitaryTaskImportance.Normal) { TaskBombTimeNeeded = onHand + 40, NeededTroopStrength = 50 };
        typeof(MilitaryTask).GetMethod("CreateTaskFleet", BindingFlags.Instance | BindingFlags.NonPublic)!.Invoke(task, new object[] { 1f, false });
        AssertEqual(onHand, task.TaskBombTimeNeeded, "the bombing time on hand is asked for");
        AssertEqual(450, task.NeededTroopStrength, "the 40 missing seconds become 400 troop strength");
    }

    [TestMethod]
    public void TheBombingAFleetsLimitOnBombersLeavesHomeIsMadeUpWithTroops()
    {
        Ship bomber = SpawnShip(Bomber, Enemy, new Vector2(-201_000));
        FleetShips ready = ReadyForFleet(SpawnShip(Fighter, Enemy, new Vector2(-200_000)), bomber,
                                         SpawnShip(Bomber, Enemy, new Vector2(-202_000)));
        FieldInfo ratiosField = typeof(FleetShips).GetField("Ratios", BindingFlags.Instance | BindingFlags.NonPublic)!;
        object ratios = ratiosField.GetValue(ready)!;
        typeof(FleetRatios).GetProperty(nameof(FleetRatios.MinBombers))!.SetValue(ratios, 1f); // one bomber a fleet
        ratiosField.SetValue(ready, ratios);

        var task = new MilitaryTask(Target, Enemy, MilitaryTaskImportance.Normal) { TaskBombTimeNeeded = ready.BombSecsAvailable, NeededTroopStrength = 50 };
        ready.ExtractShipSet(0f, new Array<Troop>(), 1, task, out _, out _);
        AssertEqual(bomber.BombsGoodFor60Secs, task.TaskBombTimeNeeded, "the fleet takes one of the two bombers");
        AssertEqual(50 + bomber.BombsGoodFor60Secs * 10, task.NeededTroopStrength, "troops make up the bomber left home");
    }

    [TestMethod]
    public void TheBombingTimeAskedForAPlanetGrowsWithItsLevel()
    {
        AssertEqual(60, BombTimeAtLevel("BombTimeNeeded", 4) - BombTimeAtLevel("BombTimeNeeded", 1),
                    "400 ground defense: 20 at level 1, 80 at level 4");
    }

    [TestMethod]
    public void TheGlassingTimeAskedForAPlanetGrowsWithItsLevel()
    {
        AssertEqual(240, BombTimeAtLevel("GlassBombTimeNeeded", 4) - BombTimeAtLevel("GlassBombTimeNeeded", 1),
                    "400 ground defense: 80 at level 1, 320 at level 4");
    }
}
