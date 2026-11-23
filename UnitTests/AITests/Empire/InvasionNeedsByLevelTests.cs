using System.Linq;
using System.Reflection;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using SDUtils;
using Ship_Game;
using Ship_Game.AI;
using Ship_Game.AI.Tasks;
using Ship_Game.Commands.Goals;
using Ship_Game.Ships;
using Vector2 = SDGraphics.Vector2;

namespace UnitTests.AITests.Empire;

// the troops and bombing time an AI task asks for a planet grow with the planet's level, and how long it waits for bombers before troops make up for them
[TestClass]
public class InvasionNeedsByLevelTests : StarDriveTest
{
    const string Bomber = "Neutron Bomber Mk1";
    readonly Planet Target;

    public InvasionNeedsByLevelTests()
    {
        LoadStarterShips(Bomber);
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
        Requisition(task);
        AssertEqual(onHand, task.TaskBombTimeNeeded, "the bombing time on hand is asked for");
        AssertEqual(450, task.NeededTroopStrength, "the 40 missing seconds become 400 troop strength");
    }

    static MilitaryTask.RequisitionStatus Requisition(MilitaryTask task)
        => (MilitaryTask.RequisitionStatus)typeof(MilitaryTask).GetMethod("CreateTaskFleet", BindingFlags.Instance | BindingFlags.NonPublic)!
                                                                .Invoke(task, new object[] { 1f, false })!;

    // a war task for our planet asking this much bombing and 50 troop strength
    MilitaryTask TaskForBombing(bool canBuildBombers, bool planetHasAGun, int bombing)
    {
        typeof(Ship_Game.Empire).GetProperty(nameof(Ship_Game.Empire.CanBuildBombers))!.SetValue(Enemy, canBuildBombers);
        if (planetHasAGun)
        {
            Building template = ResourceManager.BuildingsDict.Values.First(b => b.IsWeapon);
            Target.TilesList.First(t => t.Habitable && t.NoBuildingOnTile).PlaceBuilding(ResourceManager.CreateBuilding(Target, template), Target);
        }
        Assert.AreEqual(planetHasAGun, Target.GunReach() > 0f, "setup: the planet's gun");

        var task = new MilitaryTask(Target, Enemy, MilitaryTaskImportance.Normal) { TaskBombTimeNeeded = bombing, NeededTroopStrength = 50 };
        task.Goal = new WarMission(Enemy, Player, Target);
        Assert.IsTrue(task.IsWarTask, "setup: a war task, which lasts the empire's war task life");
        return task;
    }

    [TestMethod]
    public void AnEmpireThatBuildsBombersWaitsForThemAgainstAPlanetWithAGunThroughHalfTheTasksLife()
    {
        AssertEqual(0, Enemy.ShipsReadyForFleet.BombSecsAvailable, "setup: no bomber is ready");
        MilitaryTask task = TaskForBombing(canBuildBombers: true, planetHasAGun: true, bombing: 40);

        Assert.AreEqual(MilitaryTask.RequisitionStatus.NotEnoughBomberStrength, Requisition(task), "the task waits for bombers");
        AssertEqual(50, task.NeededTroopStrength, "and asks no troops for the bombing");

        UState.StarDate += Enemy.PersonalityModifiers.WarTasksLifeTime * 0.4f;
        Requisition(task);
        AssertEqual(50, task.NeededTroopStrength, "it still waits before half its life");

        UState.StarDate += Enemy.PersonalityModifiers.WarTasksLifeTime * 0.15f;
        Requisition(task);
        AssertEqual(450, task.NeededTroopStrength, "past half its life, troops make up for the 40 missing seconds");
    }

    [TestMethod]
    public void AgainstAPlanetWithoutAGunItWaitsForHalfTheBombingAndTroopsMakeUpTheOtherHalf()
    {
        Enemy.ShipsReadyForFleet = ReadyForFleet(SpawnShip(Bomber, Enemy, new Vector2(-200_000)));
        int onHand = Enemy.ShipsReadyForFleet.BombSecsAvailable;
        Assert.IsTrue(onHand > 0, "setup: a bomber is ready");

        MilitaryTask moreThanTwice = TaskForBombing(canBuildBombers: true, planetHasAGun: false, bombing: onHand * 2 + 2);
        Assert.AreEqual(MilitaryTask.RequisitionStatus.NotEnoughBomberStrength, Requisition(moreThanTwice),
                        "with less than half the bombing ready, the task waits for bombers");
        AssertEqual(50, moreThanTwice.NeededTroopStrength, "and asks no troops for the bombing");

        MilitaryTask twice = TaskForBombing(canBuildBombers: true, planetHasAGun: false, bombing: onHand * 2);
        Requisition(twice);
        AssertEqual(50 + onHand * 10, twice.NeededTroopStrength, "with half of it ready, troops make up for the other half");
    }

    [TestMethod]
    public void AnEmpireThatCannotBuildBombersLetsTroopsMakeUpForAllTheBombingAtOnce()
    {
        AssertEqual(0, Enemy.ShipsReadyForFleet.BombSecsAvailable, "setup: no bomber is ready");
        MilitaryTask task = TaskForBombing(canBuildBombers: false, planetHasAGun: true, bombing: 40);
        Requisition(task);
        AssertEqual(450, task.NeededTroopStrength, "troops make up for the 40 missing seconds");
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
