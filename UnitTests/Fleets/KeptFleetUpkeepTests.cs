using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using SDUtils;
using Ship_Game;
using Ship_Game.AI;
using Ship_Game.AI.Tasks;
using Ship_Game.Commands.Goals;
using Ship_Game.Fleets;
using Ship_Game.Ships;
using Vector2 = SDGraphics.Vector2;

namespace UnitTests.Fleets;

/// <summary>
/// Each AI turn, an idle fleet with fewer ships than half a fleet ratio, ships away refitting counted, breaks up into
/// the pool. An idle fleet in orbit of its rally planet sends its oldest designs that have a better one to refit, a
/// third of the fleet rounded up at a time, never its last ship; the check runs once it is in orbit with no enemies
/// near and the empire has a safe port, and
/// again when new tech comes in, which is saved; a ship in a fight is left to it, and the refitted ship comes back to
/// its place, or goes to the pool once the fleet is gone or the build is lost. Under the cap, an idle fleet in orbit
/// with no ship away refitting that holds more than all the empire's other fleets together splits, when each half
/// keeps a full fleet ratio: the new fleet takes half of each role from its repaired ships, damaged ones staying, while the old
/// one keeps the strength its tasks needed, the most of them, fading while idle and saved; the empire's only fleet
/// stays whole. The budget scrapper takes pool ships before the weakest idle fleet's, never a ship of a fleet on a
/// task, and below the money line where the AI stops building freighters it scraps down to the budget in one turn.
/// </summary>
[TestClass]
public class KeptFleetUpkeepTests : StarDriveTest
{
    const string Warship = "Terran-Prototype";
    const string Bomber = "Neutron Bomber Mk1";
    const string TroopShip = "Proton Troopship  Mk1";
    readonly Planet Rally;
    readonly Planet Target;

    public KeptFleetUpkeepTests()
    {
        LoadStarterShips(Warship, Bomber, TroopShip);
        CreateUniverseAndPlayerEmpire();
        UState.Objects.EnableParallelUpdate = false;
        Rally = AddHomeWorldToEmpire(new Vector2(-400_000f), Enemy);
        Target = AddDummyPlanetToEmpire(new Vector2(400_000f), Player);
        Enemy.FleetStrEmpireMultiplier[Player.Id] = 1f;
        Enemy.UpdateRallyPoints();
    }

    MilitaryTask WarTask() => new(Target, Enemy, MilitaryTaskImportance.Normal);

    static Fleet FleetFor(MilitaryTask task, params Ship[] ships)
    {
        MethodInfo create = typeof(MilitaryTask).GetMethod("CreateFleet", BindingFlags.Instance | BindingFlags.NonPublic,
                                                           null, new[] { typeof(Array<Ship>), typeof(string) }, null);
        create!.Invoke(task, new object[] { new Array<Ship>(ships), "Test Fleet" });
        return task.Fleet;
    }

    // a fleet kept after a task that needed the strength
    Fleet KeptFleet(float need, params Ship[] ships)
    {
        MilitaryTask task = WarTask();
        Fleet fleet = FleetFor(task, ships);
        task.MinimumTaskForceStrength = need;
        FleetShips pool = Enemy.ShipsReadyForFleet;
        Enemy.ShipsReadyForFleet = null; // with no pool yet, as before the empire's first turn, a small fleet is kept too
        task.EndTask();
        Enemy.ShipsReadyForFleet = pool;
        Assert.IsTrue(fleet.IsIdle, "setup: the fleet is kept with no task");
        return fleet;
    }

    int Spawned;

    Ship[] Ships(string design, int count, Vector2 at)
    {
        var ships = new Ship[count];
        for (int i = 0; i < count; ++i)
        {
            int n = Spawned++;
            ships[i] = SpawnShip(design, Enemy, at + new Vector2(600f * (n % 10), 600f * (n / 10)));
        }
        return ships;
    }

    // game ships, since a save with test ships in a fleet loads the fleet before them
    Ship[] GameShips(int count, Vector2 at)
    {
        var ships = new Ship[count];
        for (int i = 0; i < count; ++i)
            ships[i] = Ship.CreateShipAtPoint(UState, Warship, Enemy, at + new Vector2(600f * i, 0f));
        return ships;
    }

    UniverseScreen SaveAndLoad(string name)
    {
        UState.StarDate = 1001f; // the loader needs a stardate past 1000
        SavedGame save = Universe.Save(name, throwOnError: true);
        UniverseScreen loaded = LoadSave(save.SaveFile);
        Assert.IsNotNull(loaded, "setup: the save loads");
        return loaded;
    }

    FleetDataNode HoldPlaceForARefit(Fleet fleet)
    {
        var held = new FleetDataNode { ShipName = Warship };
        fleet.DataNodes.Add(held);
        fleet.AssignGoal(held, new RefitShip(Enemy));
        return held;
    }

    // the first ship's design is older than the second's while the action runs; the designs' own techs depend on what content is loaded
    static void WithOlderDesign(Ship older, Ship newer, Action action)
    {
        var olderDesign = (ShipDesign)older.ShipData;
        var newerDesign = (ShipDesign)newer.ShipData;
        HashSet<string> olderTechs = olderDesign.TechsNeeded;
        HashSet<string> newerTechs = newerDesign.TechsNeeded;
        olderDesign.TechsNeeded = new HashSet<string> { "OldTech" };
        newerDesign.TechsNeeded = new HashSet<string> { "OldTech", "NewTech" };
        try
        {
            action();
        }
        finally
        {
            olderDesign.TechsNeeded = olderTechs;
            newerDesign.TechsNeeded = newerTechs;
        }
    }

    // the ships still with the fleet leave it, as the scrapper or a fight can take them
    static void LoseTheShipsLeft(Fleet fleet)
    {
        foreach (Ship ship in fleet.Ships.ToArray())
            fleet.RemoveShip(ship, clearOrders: true);
    }

    static bool OrdersToOrbit(Ship ship, Planet planet) => ship.AI.FindGoal(ShipAI.Plan.Orbit, out ShipAI.ShipGoal orbit) && orbit.TargetPlanet == planet;

    Vector2 NearRally => Rally.Position + new Vector2(3000f, 3000f);
    static readonly Vector2 FarAway = new(300_000f, -300_000f);

    void InOrbitOfRally(Fleet fleet)
    {
        foreach (Ship ship in fleet.Ships)
            ship.OrderToOrbit(Rally, clearOrders: true);
        RunSimWhile((120, fatal: true), () => fleet.Ships.Any(s => !s.AI.IsOrbiting(Rally)));
    }

    FleetShips ReadyThePool() => Enemy.ShipsReadyForFleet = new FleetShips(Enemy, Enemy.AllFleetReadyShips());

    void Run(string method, params object[] args)
        => typeof(EmpireAI).GetMethod(method, BindingFlags.Instance | BindingFlags.NonPublic)!.Invoke(Enemy.AI, args);

    void Refit(Func<Ship, IShipDesign> betterDesign) => Run("RefitIdleFleetsAtRally", betterDesign);

    static IShipDesign Design(string name) => ResourceManager.Ships.GetDesign(name);

    // the ships one full fleet ratio holds, rounded up
    int FullRatio()
    {
        float ships = ReadyThePool().ShipsPerFullFleet;
        AssertGreaterThan(ships, 2f, "setup: a fleet ratio of more than two ships");
        return (int)Math.Ceiling(ships);
    }

    [TestMethod]
    public void AnIdleFleetInOrbitSendsItsOldestDesignsToRefitAThirdRoundedUp()
    {
        Ship[] older = Ships(Warship, 2, NearRally);
        Ship[] newer = Ships(Bomber, 2, NearRally);
        WithOlderDesign(older[0], newer[0], () =>
        {
            Fleet fleet = KeptFleet(0f, newer.Concat(older).ToArray());
            InOrbitOfRally(fleet);
            AssertGreaterThan(Enemy.SafeSpacePorts.Length, 0, "setup: a port to refit at");

            Refit(_ => Design(Warship));

            AssertEqual(2, fleet.ShipsAwayRefitting(), "a third of four ships, rounded up, goes to refit");
            Assert.IsTrue(older.All(s => s.Fleet == null && s.AI.State == AIState.Refit), "the oldest designs first");
            Assert.IsTrue(newer.All(s => s.Fleet == fleet), "while the newer ones stay");
        });
    }

    [TestMethod]
    public void ShipsAlreadyAwayRefittingCountTowardTheThird()
    {
        Fleet fleet = KeptFleet(0f, Ships(Warship, 4, NearRally));
        InOrbitOfRally(fleet);
        HoldPlaceForARefit(fleet);

        Refit(_ => Design(Warship));

        AssertEqual(2, fleet.ShipsAwayRefitting(), "with one of five away, one more goes: a third of five, rounded up, is two");
    }

    [TestMethod]
    public void AFleetKeepsOneShipFromRefit()
    {
        Ship only = Ships(Warship, 1, NearRally)[0];
        Fleet fleet = KeptFleet(0f, only);
        InOrbitOfRally(fleet);

        Refit(_ => Design(Warship));

        Assert.AreEqual(fleet, only.Fleet, "a fleet's last ship stays with it");
    }

    [TestMethod]
    public void WithEnemiesNearTheRefitCheckWaitsForThemToGo()
    {
        Ship[] ships = Ships(Warship, 3, NearRally);
        Fleet fleet = KeptFleet(0f, ships);
        InOrbitOfRally(fleet);
        ships[0].AI.BadGuysNear = true;
        int looks = 0;

        Refit(_ => { ++looks; return Design(Bomber); });
        AssertEqual(0, looks, "with enemies near the fleet, no check runs");

        ships[0].AI.BadGuysNear = false;
        Refit(_ => Design(Bomber));
        AssertEqual(1, fleet.ShipsAwayRefitting(), "and the check waits for them to go");
    }

    [TestMethod]
    public void ARefitLeavesAShipInAFightToIt()
    {
        Ship fighting = Ships(Warship, 1, NearRally)[0];
        Ship free = Ships(Bomber, 1, NearRally)[0];
        WithOlderDesign(fighting, free, () =>
        {
            Fleet fleet = KeptFleet(0f, fighting, free);
            InOrbitOfRally(fleet);
            fighting.InCombat = true;

            Refit(_ => Design(Warship));

            Assert.AreEqual(fleet, fighting.Fleet, "a ship in a fight is left to it, though it is the oldest design");
            Assert.IsNull(free.Fleet, "and the next one goes to refit instead");
        });
    }

    [TestMethod]
    public void ARefitWhoseBuildIsGoneFreesItsFleetPlace()
    {
        Fleet fleet = KeptFleet(0f, Ships(Warship, 3, NearRally));
        InOrbitOfRally(fleet);
        Refit(_ => Design(Warship));
        var refit = (RefitShip)Enemy.AI.FindGoal(g => g is RefitShip);
        AssertEqual(1, fleet.ShipsAwayRefitting(), "setup: a ship is away refitting");
        typeof(Goal).GetProperty(nameof(Goal.Step))!.SetValue(refit, 3);
        AssertEqual("WaitForRefitBuilt", refit.StepName, "setup: the refit waits for a build that is not in the queue");

        refit.Evaluate();

        AssertEqual(0, fleet.ShipsAwayRefitting(), "a refit whose build is gone frees the place it held in the fleet");
    }

    [TestMethod]
    public void TheRefitCheckIsSaved()
    {
        Fleet checkedFleet = KeptFleet(0f, GameShips(2, NearRally));
        InOrbitOfRally(checkedFleet);
        Refit(_ => null);
        Fleet dueFleet = KeptFleet(0f, GameShips(2, FarAway));

        Ship_Game.Empire enemy = SaveAndLoad("UnitTest.RefitCheckDue").UState.GetEmpireById(Enemy.Id);

        FieldInfo due = typeof(Fleet).GetField("RefitCheckDue", BindingFlags.Instance | BindingFlags.NonPublic)!;
        Assert.IsFalse((bool)due.GetValue(enemy.GetFleetOrNull(checkedFleet.Key)), "a refit check that ran stays run after a load");
        Assert.IsTrue((bool)due.GetValue(enemy.GetFleetOrNull(dueFleet.Key)), "and one due stays due");
    }

    [TestMethod]
    public void TheRefitCheckRunsInOrbitAndAgainWhenNewTechComesIn()
    {
        Fleet fleet = KeptFleet(0f, Ships(Warship, 3, NearRally));
        IShipDesign better = null;
        int looks = 0;
        Func<Ship, IShipDesign> pick = _ => { ++looks; return better; };

        Refit(pick);
        AssertEqual(0, looks, "no check before the fleet is in orbit of its rally planet");

        InOrbitOfRally(fleet);
        Refit(pick);
        AssertEqual(1, looks, "in orbit it checks, one look per design");

        better = Design(Bomber);
        Refit(pick);
        AssertEqual(0, fleet.ShipsAwayRefitting(), "once, on reaching its rally planet");

        Enemy.AI.TriggerRefit(); // new tech
        Refit(pick);
        AssertEqual(1, fleet.ShipsAwayRefitting(), "and again when new tech comes in");
    }

    [TestMethod]
    public void WithNoSafePortTheRefitCheckWaitsForOne()
    {
        Fleet fleet = KeptFleet(0f, Ships(Warship, 3, NearRally));
        InOrbitOfRally(fleet);
        Planet[] ports = Enemy.SafeSpacePorts;
        Enemy.SafeSpacePorts = Empty<Planet>.Array;
        int looks = 0;

        Refit(_ => { ++looks; return Design(Bomber); });
        AssertEqual(0, looks, "with no safe port to refit at, no check runs");

        Enemy.SafeSpacePorts = ports;
        Refit(_ => Design(Bomber));
        AssertEqual(1, fleet.ShipsAwayRefitting(), "and the check waits for one");
    }

    [TestMethod]
    public void ARefittedShipComesBackToItsIdleFleet()
    {
        Fleet fleet = KeptFleet(0f, Ships(Warship, 3, NearRally));
        InOrbitOfRally(fleet);
        Refit(_ => Design(Warship));
        var refit = (RefitShip)Enemy.AI.FindGoal(g => g is RefitShip);
        Assert.IsNotNull(refit, "setup: a ship is away refitting");

        Ship refitted = SpawnShip(Warship, Enemy, Rally.Position + new Vector2(2000f, 0f));
        refit.FinishedShip = refitted;
        typeof(RefitShip).GetMethod("AddShipDataAndFleet", BindingFlags.Instance | BindingFlags.NonPublic)!.Invoke(refit, null);

        Assert.AreEqual(fleet, refitted.Fleet, "the refitted ship takes its place in the fleet");
        AssertEqual(3, fleet.Ships.Count, "which is whole again");
    }

    [TestMethod]
    public void TheRefitsOfAFleetThatIsGoneGoToThePool()
    {
        Fleet dropped = KeptFleet(0f, Ships(Warship, 2, NearRally));
        Fleet replaced = KeptFleet(0f, Ships(Warship, 2, NearRally + new Vector2(0f, 5000f)));
        InOrbitOfRally(dropped);
        InOrbitOfRally(replaced);
        Refit(_ => Design(Warship));
        RefitShip[] refits = Enemy.AI.FindGoals(g => g is RefitShip).Cast<RefitShip>().ToArray();
        RefitShip ofDropped = refits.Single(r => r.Fleet == dropped);
        RefitShip ofReplaced = refits.Single(r => r.Fleet == replaced);
        LoseTheShipsLeft(dropped);
        LoseTheShipsLeft(replaced);
        Assert.IsTrue(dropped.Ships.IsEmpty && replaced.Ships.IsEmpty, "setup: each fleet's last ship is away refitting");

        Enemy.CreateFleet(replaced.Key, "New Fleet");
        Assert.IsNull(ofReplaced.Fleet, "a kept fleet whose place another fleet takes lets go of its refits, which go to the pool");
        Assert.AreEqual(dropped, ofDropped.Fleet, "setup: the other one still holds its refit");

        Enemy.UpdateFleets(TestSimStep);
        Assert.IsNull(Enemy.GetFleetOrNull(dropped.Key), "setup: the kept fleet with no ship left is dropped");
        Assert.IsNull(ofDropped.Fleet, "and lets go of its refits too");
    }

    [TestMethod]
    public void AKeptFleetWithNoShipLeftInASaveLetsGoOfItsRefitsOnLoad()
    {
        Fleet fleet = KeptFleet(0f, GameShips(2, NearRally));
        InOrbitOfRally(fleet);
        Refit(_ => Design(Warship));
        LoseTheShipsLeft(fleet);
        Assert.IsTrue(fleet.Ships.IsEmpty && fleet.ShipsAwayRefitting() == 1, "setup: the fleet's last ship is away refitting");

        Ship_Game.Empire enemy = SaveAndLoad("UnitTest.KeptFleetRefitAway").UState.GetEmpireById(Enemy.Id);

        Assert.IsNull(enemy.GetFleetOrNull(fleet.Key), "the kept fleet with no ship left is dropped on load");
        var refit = (RefitShip)enemy.AI.FindGoal(g => g is RefitShip);
        Assert.IsNotNull(refit, "setup: the refit is saved");
        Assert.IsNull(refit.Fleet, "and lets go of the fleet, so the refitted ship goes to the pool");
    }

    [TestMethod]
    public void AnIdleFleetOfFewerShipsThanHalfARatioBreaksUpIntoThePool()
    {
        int ratio = FullRatio();
        int fewer = (ratio + 1) / 2 - 1; // under half a ratio
        Fleet small = KeptFleet(0f, Ships(Warship, fewer, NearRally));
        Fleet half = KeptFleet(0f, Ships(Warship, fewer + 1, FarAway));
        Fleet onTask = FleetFor(WarTask(), Ships(Warship, fewer, FarAway + new Vector2(0f, 100_000f)));

        Run("DissolveSmallIdleFleets");

        Assert.IsTrue(small.Ships.IsEmpty, "the fleet of fewer ships than half a ratio breaks up");
        Assert.IsTrue(half.IsIdle && half.Ships.Count == fewer + 1, "the one of half a ratio stays");
        AssertEqual(fewer, onTask.Ships.Count, "and a fleet on a task is left to it");
    }

    [TestMethod]
    public void ShipsAwayRefittingCountTowardKeepingAnIdleFleet()
    {
        int ratio = FullRatio();
        int fewer = (ratio + 1) / 2 - 1;
        Fleet fleet = KeptFleet(0f, Ships(Warship, fewer, NearRally));
        HoldPlaceForARefit(fleet);

        Run("DissolveSmallIdleFleets");

        AssertEqual(fewer, fleet.Ships.Count, "with the ship away refitting, the fleet holds half a ratio and stays");
    }

    void SetLevel(Planet planet, int level) => typeof(Planet).GetProperty(nameof(Planet.Level))!.SetValue(planet, level);

    // a fleet in orbit of the rally planet, and a small one far away, under the cap
    Fleet LargestOfTwo(float need, Ship[] ships)
    {
        UState.P.Difficulty = GameDifficulty.Brutal;
        SetLevel(Rally, 0);
        Fleet largest = KeptFleet(need, ships);
        KeptFleet(0f, Ships(Warship, 1, FarAway));
        InOrbitOfRally(largest);
        ReadyThePool();
        AssertGreaterThan(Enemy.KeptFleetCap(), Enemy.KeptFleetCount(), "setup: under the cap");
        return largest;
    }

    Fleet NewFleet(params Fleet[] old) => Enemy.ActiveFleets.ToArrayList().Find(f => !old.Contains(f) && f.Ships.NotEmpty);

    [TestMethod]
    public void AnIdleFleetHoldingMoreThanTheOthersSplitsHalfOfEachRole()
    {
        int ratio = FullRatio();
        Ship[] warships = Ships(Warship, 2 * ratio + 1, NearRally);
        Ship[] bombers = Ships(Bomber, 2, NearRally);
        Ship[] fightingTroops = Ships(TroopShip, 2, NearRally);
        Fleet largest = LargestOfTwo(0f, warships.Concat(bombers).Concat(fightingTroops).ToArray());
        Ship damaged = warships[0];
        foreach (Ship ship in new[] { damaged, bombers[0], bombers[1] })
        {
            foreach (ShipModule module in ship.Modules)
                module.SetHealth(module.ActualMaxHealth * 0.5f, "Test");
            AssertLessThan(ship.HealthPercent, ShipResupply.RepairDoneThreshold, "setup: a damaged ship");
        }
        foreach (Ship ship in fightingTroops)
            ship.InCombat = true;
        Fleet other = Enemy.ActiveFleets.ToArrayList().Find(f => f != largest);

        Run("SplitTheLargestIdleFleet");

        Fleet split = NewFleet(largest, other);
        Assert.IsNotNull(split, "the fleet splits");
        AssertEqual(ratio, split.Ships.Count(s => s.Name == Warship), "the new fleet takes half of the warships, rounded down");
        Assert.AreEqual(largest, damaged.Fleet, "while the damaged warship stays");
        AssertEqual(0, split.Ships.Count(s => s.Name == Bomber), "as do damaged bombers, even when half of the role is due to leave");
        AssertEqual(0, split.Ships.Count(s => s.Name == TroopShip), "and ships in a fight");
        Assert.IsTrue(split.IsIdle && split.IdleRally == Rally, "the new fleet waits at the same rally planet");
        Assert.IsTrue(largest.Ships.Where(s => !s.InCombat).All(s => OrdersToOrbit(s, Rally)), "the staying ships keep their orbit");
    }

    [TestMethod]
    public void ASplitTakesShipsInTurnsAcrossTheRoles()
    {
        int ratio = FullRatio();
        Ship[] warships = Ships(Warship, 2 * ratio, NearRally);
        Ship[] bombers = Ships(Bomber, 2 * ratio, NearRally);
        bool warshipsFirst = warships[0].DesignRole < bombers[0].DesignRole; // turn by turn, the roles go in this order
        Ship[] first = warshipsFirst ? warships : bombers;
        Ship[] second = warshipsFirst ? bombers : warships;
        int fromFirst = (ratio + 1) / 2, fromSecond = ratio / 2; // the first full ratio of ships, taken in turns
        float leaving = fromFirst * first[0].GetStrength() + fromSecond * second[0].GetStrength();
        float ready = warships.Concat(bombers).Sum(s => s.GetStrength());
        float smallest = Math.Min(first[0].GetStrength(), second[0].GetStrength());
        Fleet largest = LargestOfTwo(ready - leaving - smallest / 2f, warships.Concat(bombers).ToArray());
        Fleet other = Enemy.ActiveFleets.ToArrayList().Find(f => f != largest);

        Run("SplitTheLargestIdleFleet");

        Fleet split = NewFleet(largest, other);
        Assert.IsNotNull(split, "the fleet splits");
        AssertEqual(fromFirst, split.Ships.Count(s => first.Contains(s)), "the roles take turns, so neither leaves whole");
        AssertEqual(fromSecond, split.Ships.Count(s => second.Contains(s)), "before the strength to keep stops it");
    }

    [TestMethod]
    public void ASplitLeavesTheFleetTheStrengthItsTasksNeeded()
    {
        int ratio = FullRatio();
        Ship[] warships = Ships(Warship, 4 * ratio, NearRally);
        float one = warships[0].GetStrength();
        Fleet largest = LargestOfTwo((3 * ratio - 0.5f) * one, warships);
        Fleet other = Enemy.ActiveFleets.ToArrayList().Find(f => f != largest);

        Run("SplitTheLargestIdleFleet");

        Fleet split = NewFleet(largest, other);
        Assert.IsNotNull(split, "the fleet splits");
        AssertEqual(ratio, split.Ships.Count, "only as many ships leave as keep the strength its task needed");
        AssertEqual(3 * ratio, largest.Ships.Count, "the rest stay");
    }

    [TestMethod]
    public void AFleetTooSmallForTwoFullRatiosStaysWhole()
    {
        int ratio = FullRatio();
        Fleet largest = LargestOfTwo(0f, Ships(Warship, 2 * ratio - 1, NearRally));

        Run("SplitTheLargestIdleFleet");

        AssertEqual(2 * ratio - 1, largest.Ships.Count, "a half would hold less than a full fleet ratio");
    }

    [TestMethod]
    public void AFleetHoldingLessThanTheOthersTogetherStaysWhole()
    {
        UState.P.Difficulty = GameDifficulty.Brutal;
        SetLevel(Rally, 10);
        int ratio = FullRatio();
        Fleet largest = KeptFleet(0f, Ships(Warship, 4 * ratio, NearRally));
        KeptFleet(0f, Ships(Warship, 2 * ratio + 1, FarAway));
        KeptFleet(0f, Ships(Warship, 2 * ratio, FarAway + new Vector2(0f, 100_000f)));
        InOrbitOfRally(largest);
        ReadyThePool();
        AssertGreaterThan(Enemy.KeptFleetCap(), Enemy.KeptFleetCount(), "setup: under the cap");

        Run("SplitTheLargestIdleFleet");

        AssertEqual(4 * ratio, largest.Ships.Count, "the largest fleet holds less than the other two together");
    }

    [TestMethod]
    public void AFleetSplitsOnlyInOrbitWithNoShipAwayRefitting()
    {
        UState.P.Difficulty = GameDifficulty.Brutal;
        SetLevel(Rally, 0);
        int ratio = FullRatio();
        Fleet largest = KeptFleet(0f, Ships(Warship, 4 * ratio, NearRally));
        KeptFleet(0f, Ships(Warship, 1, FarAway));
        ReadyThePool();

        Run("SplitTheLargestIdleFleet");
        AssertEqual(4 * ratio, largest.Ships.Count, "no split before the fleet is in orbit of its rally planet");

        InOrbitOfRally(largest);
        FleetDataNode held = HoldPlaceForARefit(largest);
        Run("SplitTheLargestIdleFleet");
        AssertEqual(4 * ratio, largest.Ships.Count, "nor with a ship away refitting");

        largest.AssignGoal(held, null);
        Run("SplitTheLargestIdleFleet");
        AssertLessThan(largest.Ships.Count, 4 * ratio, "in orbit with no ship away, it splits");
    }

    [TestMethod]
    public void TheEmpiresOnlyFleetStaysWhole()
    {
        UState.P.Difficulty = GameDifficulty.Brutal;
        SetLevel(Rally, 0);
        int ratio = FullRatio();
        Fleet only = KeptFleet(0f, Ships(Warship, 4 * ratio, NearRally));
        InOrbitOfRally(only);
        ReadyThePool();

        Run("SplitTheLargestIdleFleet");

        AssertEqual(4 * ratio, only.Ships.Count, "the only fleet does not split");
    }

    [TestMethod]
    public void AtTheCapNoFleetSplits()
    {
        SetLevel(Rally, 10);
        int ratio = FullRatio();
        Fleet largest = KeptFleet(0f, Ships(Warship, 4 * ratio, NearRally));
        KeptFleet(0f, Ships(Warship, 1, FarAway));
        InOrbitOfRally(largest);
        ReadyThePool();
        AssertEqual(Enemy.KeptFleetCap(), Enemy.KeptFleetCount(), "setup: at the cap");

        Run("SplitTheLargestIdleFleet");

        AssertEqual(4 * ratio, largest.Ships.Count, "no fleet splits at the cap");
    }

    [TestMethod]
    public void TheStrengthToKeepIsTheMostTheTasksNeededAndFadesWhileIdle()
    {
        Fleet fleet = KeptFleet(100f, Ships(Warship, FullRatio(), NearRally)); // a full ratio, so it does not break up
        Fleet onTask = KeptFleet(100f, Ships(Warship, FullRatio(), FarAway));

        MilitaryTask small = WarTask();
        small.MinimumTaskForceStrength = 10f;
        fleet.FleetTask = small;
        fleet.KeepAfterTask();
        AssertEqual(0.001f, 100f, fleet.StrengthToKeep, "a smaller task leaves the most the tasks needed");

        onTask.FleetTask = WarTask();
        ReadyThePool();
        Run("UpdateIdleFleets");
        AssertEqual(0.001f, 99.5f, fleet.StrengthToKeep, "it fades by half a percent a turn while the fleet is idle");
        AssertEqual(0.001f, 100f, onTask.StrengthToKeep, "and only then");
    }

    [TestMethod]
    public void AJoinedFleetKeepsTheGreaterStrengthToKeep()
    {
        Fleet weaker = KeptFleet(200f, Ships(Warship, 1, NearRally));
        Fleet stronger = KeptFleet(50f, Ships(Warship, 2, NearRally + new Vector2(5000f, 0f)));
        SetLevel(Rally, 0);
        AssertEqual(1, Enemy.KeptFleetCap(), "setup: room for one fleet");

        Run("JoinIdleFleetsOverTheCap");

        Assert.IsTrue(weaker.Ships.IsEmpty, "setup: the weaker fleet joins the other");
        AssertEqual(0.001f, 200f, stronger.StrengthToKeep, "which keeps the greater strength to keep");
    }

    [TestMethod]
    public void TheStrengthToKeepIsSaved()
    {
        Fleet fleet = KeptFleet(123f, GameShips(2, NearRally));

        Fleet after = SaveAndLoad("UnitTest.StrengthToKeep").UState.GetEmpireById(Enemy.Id).GetFleetOrNull(fleet.Key);
        AssertEqual(0.001f, 123f, after.StrengthToKeep, "the strength to keep is saved");
    }

    // money against the empire's money goal, which sets its credit rating
    void CreditRating(float money)
    {
        typeof(EmpireAI).GetProperty(nameof(EmpireAI.ProjectedMoney))!.SetValue(Enemy.AI, 1000f);
        Enemy.Money = money;
    }

    // the build planner's turn with no budget, which scraps; new ships join the empire's lists first
    void ScrapTurn()
    {
        UState.Objects.UpdateLists();
        _ = new EmpireAI.RoleBuildInfo(0f, Enemy.AI, ignoreDebt: false);
    }

    static bool Scrapped(Ship ship) => ship.AI.State == AIState.Scrap;

    [TestMethod]
    public void ScrappingTakesPoolShipsFirstThenTheWeakestIdleFleetsNeverAFleetOnATask()
    {
        CreditRating(1000f);
        Assert.IsTrue(Enemy.EconomicSafeToBuildFreighter, "setup: above the money line");
        Ship[] onTask = Ships(Bomber, 3, FarAway);
        FleetFor(WarTask(), onTask);
        Ship strong = Ships(Bomber, 1, NearRally)[0];
        KeptFleet(0f, new[] { strong }.Concat(Ships(Warship, 2, NearRally)).ToArray());
        Ship weak = Ships(Bomber, 1, NearRally + new Vector2(5000f, 0f))[0];
        KeptFleet(0f, weak);
        Ship pooled = Ships(Bomber, 1, NearRally + new Vector2(10_000f, 0f))[0];

        ScrapTurn();
        Assert.IsTrue(Scrapped(pooled) && !Scrapped(weak) && !Scrapped(strong), "a ship in no fleet goes first, one a turn");

        ScrapTurn();
        Assert.IsTrue(Scrapped(weak) && !Scrapped(strong), "then the weakest idle fleet's");

        ScrapTurn();
        Assert.IsTrue(Scrapped(strong), "then the next idle fleet's");

        CreditRating(0f); // over budget and scrapping all it takes
        ScrapTurn();
        Assert.IsFalse(onTask.Any(Scrapped), "never a ship of a fleet on a task");
    }

    [TestMethod]
    public void BelowTheMoneyLineScrappingGoesDownToTheBudgetInOneTurn()
    {
        Ship[] pooled = Ships(Bomber, 4, NearRally);
        CreditRating(1000f);
        ScrapTurn();
        AssertEqual(1, pooled.Count(Scrapped), "above the money line, one ship a turn");

        CreditRating(0f);
        Assert.IsFalse(Enemy.EconomicSafeToBuildFreighter, "setup: below the money line");
        ScrapTurn();
        AssertEqual(3, pooled.Count(Scrapped), "below it, all it takes to be within budget in one turn");
    }
}
