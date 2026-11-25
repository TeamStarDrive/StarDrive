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
/// A major AI empire's war, defense or claim task finds its force in this order: the nearest idle fleet with the
/// strength on its own, the nearest idle fleet that reaches it with the nearest pool ships, a new fleet from the pool
/// while the empire has fewer fleets than its cap or defends its own system, two idle fleets joined into one with pool
/// ships; else it waits. A force short only of the troops or bombing the task needs, or outmatched at the target, leaves
/// the task to the next one, the task's needs as they were before each try; the task ends only when every force with the
/// troops and bombing is outmatched, and waits when any force fell short only of them. The cap is one fleet per ten
/// planet levels, one more at Normal and one more for each difficulty step above it, never below half the most the
/// empire has had, which is saved; a temporary force is no kept fleet. Idle fleets merge only half a step below the
/// line: the weakest joins the nearest, the two weighing their fights together and their fighting ships left to fight,
/// and fleets joining at the rally planet they orbit stay in orbit. A guard or exploration task takes an idle fleet only
/// of at most twice its need, else it forms a temporary force. A fleet that takes a task does it under the task's name,
/// drops the places held for ships away, and gets from the pool only the troops it lacks. A random fleet ratio rolls
/// the same every time, after a load too. A fleet freed by a defense task that ended its task goes to that defense; an
/// ended defense takes no fleet and ends no other task to free one.
/// </summary>
[TestClass]
public class KeptFleetForceTests : StarDriveTest
{
    const string Warship = "Terran-Prototype";
    const string AssaultTroopShip = "Proton Troopship  Mk1";
    const string Bomber = "Neutron Bomber Mk1";
    readonly Planet Rally;
    readonly Planet Target;
    static readonly Vector2 NearTarget = new(400_000f, 100_000f);
    static readonly Vector2 FarFromTarget = new(-300_000f, 0f);

    static readonly MethodInfo CreateTaskFleet = typeof(MilitaryTask).GetMethod("CreateTaskFleet", BindingFlags.Instance | BindingFlags.NonPublic)!;

    public KeptFleetForceTests()
    {
        LoadStarterShips(Warship, AssaultTroopShip, Bomber);
        CreateUniverseAndPlayerEmpire();
        UState.Objects.EnableParallelUpdate = false;
        Rally = AddDummyPlanetToEmpire(new Vector2(-400_000f), Enemy);
        Target = AddDummyPlanetToEmpire(new Vector2(400_000f), Player);
        Enemy.FleetStrEmpireMultiplier[Player.Id] = 1f;
    }

    MilitaryTask WarTask() => new(Target, Enemy, MilitaryTaskImportance.Normal);

    MilitaryTask ClaimTask() => MilitaryTask.CreateClaimTask(Enemy, AddDummyPlanet(new Vector2(400_000f, -100_000f)), 100f, null, fleetCount: 1);

    MilitaryTask GuardTask() => MilitaryTask.CreateGuardTask(Enemy, AddDummyPlanet(new Vector2(400_000f, -100_000f)));

    static Fleet FleetFor(MilitaryTask task, params Ship[] ships)
    {
        MethodInfo create = typeof(MilitaryTask).GetMethod("CreateFleet", BindingFlags.Instance | BindingFlags.NonPublic,
                                                           null, new[] { typeof(Array<Ship>), typeof(string) }, null);
        create!.Invoke(task, new object[] { new Array<Ship>(ships), "Test Fleet" });
        return task.Fleet;
    }

    Ship[] Ships(string design, int count, Vector2 at)
    {
        var ships = new Ship[count];
        for (int i = 0; i < count; ++i)
            ships[i] = SpawnShip(design, Enemy, at + new Vector2(0f, 1000f * i));
        return ships;
    }

    Ship[] Warships(int count, Vector2 at) => Ships(Warship, count, at);

    Fleet KeptFleet(params Ship[] ships)
    {
        MilitaryTask task = WarTask();
        Fleet fleet = FleetFor(task, ships);
        task.EndTask();
        Assert.IsTrue(fleet.IsIdle, "setup: the fleet is kept with no task");
        return fleet;
    }

    // the ships in no fleet, as the empire's turn gathers them
    FleetShips ReadyThePool(bool simulate = true)
    {
        if (simulate)
            UState.Objects.Update(TestSimStep);
        return Enemy.ShipsReadyForFleet = new FleetShips(Enemy, Enemy.AllFleetReadyShips());
    }

    void Requisition(MilitaryTask task, float strength, bool simulate = true)
    {
        ReadyThePool(simulate);
        task.MinimumTaskForceStrength = strength;
        CreateTaskFleet.Invoke(task, new object[] { 1f, false });
    }

    // every warship of the empire has the same strength
    static float One(Ship ship) => ship.GetStrength();

    [TestMethod]
    public void ATaskTakesTheNearestIdleFleetWithTheStrengthOnItsOwn()
    {
        Fleet near = KeptFleet(Warships(2, NearTarget));
        Fleet far = KeptFleet(Warships(2, FarFromTarget));
        Ship pooled = Warships(1, NearTarget + new Vector2(20_000f, 0f))[0];
        Assert.IsTrue(FleetShips.IsCombatShip(pooled), "setup: the warship fights in a fleet");
        float one = One(pooled);
        MilitaryTask task = WarTask();

        Requisition(task, 1.5f * one);

        Assert.AreEqual(near, task.Fleet, "the nearest idle fleet with the strength takes the task");
        Assert.AreEqual(task, near.FleetTask, "and holds it");
        AssertEqual(2, near.Ships.Count, "with no pool ship added");
        Assert.IsNull(pooled.Fleet, "the pool ship stays free");
        Assert.IsTrue(far.IsIdle, "the farther fleet stays idle");
        AssertEqual(0, near.TaskStep, "the task starts from its first step");
        AssertEqual("Invasion Fleet", near.Name, "under the task's name");
        Assert.IsNull(near.IdleRally, "the fleet no longer waits at a rally planet");
        Assert.IsTrue(near.Kept, "and stays kept");
    }

    [TestMethod]
    public void AFartherIdleFleetWithTheStrengthComesBeforePoolShipsForANearerOne()
    {
        Fleet near = KeptFleet(Warships(1, NearTarget));
        Fleet far = KeptFleet(Warships(2, FarFromTarget));
        Ship pooled = Warships(1, NearTarget + new Vector2(20_000f, 0f))[0];
        float one = One(pooled);
        MilitaryTask task = WarTask();

        Requisition(task, 1.5f * one);

        Assert.AreEqual(far, task.Fleet, "a fleet with the strength on its own comes first");
        Assert.IsTrue(near.IsIdle, "the nearer, weaker fleet stays idle");
        Assert.IsNull(pooled.Fleet, "and the pool ship stays free");
    }

    [TestMethod]
    public void TheNearestIdleFleetThatReachesTheStrengthWithPoolShipsTakesThem()
    {
        Fleet near = KeptFleet(Warships(1, NearTarget));
        Fleet far = KeptFleet(Warships(1, FarFromTarget));
        Ship spare = Warships(1, FarFromTarget + new Vector2(0f, 100_000f))[0];
        Ship[] pooled = Warships(2, NearTarget + new Vector2(20_000f, 0f));
        float one = One(pooled[0]);
        MilitaryTask task = WarTask();

        Requisition(task, 2.5f * one);

        Assert.AreEqual(near, task.Fleet, "the nearest idle fleet takes the task");
        Assert.AreEqual(near, pooled[0].Fleet, "with pool ships for the strength it lacks");
        Assert.AreEqual(near, pooled[1].Fleet, "as many as that takes");
        Assert.IsNull(spare.Fleet, "the nearest ones, and no more");
        Assert.IsTrue(far.IsIdle && far.Ships.Count == 1, "the other fleet stays as it was");
    }

    [TestMethod]
    public void WithNoIdleFleetANewFleetFormsFromThePoolUnderTheCap()
    {
        float one = One(Warships(3, NearTarget)[0]);
        MilitaryTask task = WarTask();

        Requisition(task, 1.5f * one);

        Assert.IsNotNull(task.Fleet, "a new fleet forms from the pool");
        Assert.IsTrue(task.Fleet.CountsTowardFleetCap, "and counts toward the cap");
    }

    [TestMethod]
    public void AtTheCapNoNewFleetFormsAndTheTaskWaits()
    {
        FleetFor(WarTask(), Warships(1, FarFromTarget));
        Ship[] pooled = Warships(3, NearTarget);
        float one = One(pooled[0]);
        ReadyThePool();
        AssertEqual(1, Enemy.KeptFleetCap(), "setup: too few planet levels for a second fleet");
        AssertEqual(1, Enemy.KeptFleetCount(), "setup: the empire has one fleet, on a task");
        MilitaryTask task = WarTask();

        Requisition(task, 1.5f * one);

        Assert.IsNull(task.Fleet, "no new fleet forms over the cap, so the task waits");
        Assert.IsNull(pooled[0].Fleet, "and the pool ships stay free");
    }

    [TestMethod]
    public void TwoIdleFleetsJoinForATaskNeitherCanTakeAlone()
    {
        Fleet near = KeptFleet(Warships(1, NearTarget));
        Ship[] farShips = Warships(1, FarFromTarget);
        Fleet far = KeptFleet(farShips);
        float one = One(farShips[0]);
        MilitaryTask task = WarTask();

        Requisition(task, 1.5f * one);

        Assert.AreEqual(near, task.Fleet, "the nearer fleet takes the task");
        Assert.AreEqual(near, farShips[0].Fleet, "the other fleet's ships join it");
        Assert.IsTrue(far.Ships.IsEmpty, "so the task still has one fleet");
        AssertEqual(0, far.Key, "and the other one is gone");
    }

    [TestMethod]
    public void TheDefenseOfTheEmpiresOwnSystemFormsANewFleetOverTheCap()
    {
        FleetFor(WarTask(), Warships(1, FarFromTarget));
        float one = One(Warships(3, Rally.Position + new Vector2(20_000f, 0f))[0]);
        ReadyThePool();
        AssertEqual(1, Enemy.KeptFleetCap(), "setup: too few planet levels for a second fleet");
        AssertEqual(1, Enemy.KeptFleetCount(), "setup: the empire has one fleet, on a task");
        SolarSystem home = Rally.System;
        var defense = new MilitaryTask(MilitaryTask.TaskType.ClearAreaOfEnemies, Enemy, home.Position, home.Radius,
                                       home, 500f, MilitaryTaskImportance.Important);

        Requisition(defense, 1.5f * one);

        Assert.IsNotNull(defense.Fleet, "the defense of its own system does not wait for the cap");
    }

    [TestMethod]
    public void AFailedTryAtANewFleetLeavesTheBombingForTheIdleFleetsToCarry()
    {
        UState.P.Difficulty = GameDifficulty.Brutal; // room for more fleets, so a new one is tried first
        Ship nearBomber = Ships(Bomber, 1, NearTarget + new Vector2(3000f, 0f))[0];
        Fleet near = KeptFleet(Warships(1, NearTarget)[0], nearBomber);
        Ship farBomber = Ships(Bomber, 1, FarFromTarget + new Vector2(3000f, 0f))[0];
        Fleet far = KeptFleet(Warships(1, FarFromTarget)[0], farBomber);
        Ship troops = Ships(AssaultTroopShip, 1, NearTarget + new Vector2(20_000f, 0f))[0];
        UState.Objects.Update(TestSimStep);
        AssertEqual(RoleName.bomber, nearBomber.DesignRole, "setup: a bomber");
        AssertGreaterThan(near.ReadyBombSecs, 0, "setup: the fleet's bomber carries bombs");
        AssertGreaterThan(Enemy.KeptFleetCap(), Enemy.KeptFleetCount(), "setup: room for a new fleet, so one is tried first");
        MilitaryTask task = WarTask();
        task.TaskBombTimeNeeded = near.ReadyBombSecs + 1;
        task.NeededTroopStrength = (int)troops.Carrier.PlanetAssaultStrength;
        AssertGreaterThan(task.NeededTroopStrength, 0, "setup: the troop ship carries the troops the task needs");

        Requisition(task, 1.5f * near.ReadyStrength, simulate: false);

        Assert.AreEqual(near, task.Fleet, "the two idle fleets take the task with the bombing they carry together");
        Assert.AreEqual(near, farBomber.Fleet, "the other fleet's bomber joins");
        Assert.AreEqual(near, troops.Fleet, "and the troop ship from the pool");
    }

    [TestMethod]
    public void ANearerIdleFleetWithoutTheBombingTheTaskNeedsGivesWayToAFartherOneWithIt()
    {
        Fleet near = KeptFleet(Warships(2, NearTarget));
        Ship[] farShips = Warships(2, FarFromTarget);
        Ship farBomber = Ships(Bomber, 1, FarFromTarget + new Vector2(3000f, 0f))[0];
        Fleet far = KeptFleet(farShips[0], farShips[1], farBomber);
        UState.Objects.Update(TestSimStep);
        AssertEqual(0, near.ReadyBombSecs, "setup: the nearer fleet carries no bombs");
        AssertGreaterThan(far.ReadyBombSecs, 0, "setup: the farther fleet's bomber carries bombs");
        AssertEqual(0, ReadyThePool(simulate: false).BombSecsAvailable, "setup: no bomber is free in the pool");
        float one = One(farShips[0]);
        MilitaryTask task = WarTask();
        task.NeededTroopStrength = 0;
        task.TaskBombTimeNeeded = far.ReadyBombSecs;

        Requisition(task, 1.5f * one, simulate: false);

        Assert.AreEqual(far, task.Fleet, "the farther idle fleet, which carries the bombing, takes the task");
        Assert.IsTrue(near.IsIdle, "the nearer fleet stays idle");
    }

    [TestMethod]
    public void TwoIdleFleetsStrongEnoughAloneJoinWhenOnlyTheirBombersTogetherCoverTheTask()
    {
        Ship[] nearShips = Warships(2, NearTarget);
        Ship nearBomber = Ships(Bomber, 1, NearTarget + new Vector2(3000f, 0f))[0];
        Fleet near = KeptFleet(nearShips[0], nearShips[1], nearBomber);
        Ship[] farShips = Warships(2, FarFromTarget);
        Ship farBomber = Ships(Bomber, 1, FarFromTarget + new Vector2(3000f, 0f))[0];
        Fleet far = KeptFleet(farShips[0], farShips[1], farBomber);
        UState.Objects.Update(TestSimStep);
        float one = One(nearShips[0]);
        AssertGreaterThan(near.ReadyStrength, 1.5f * one, "setup: each fleet has the strength alone");
        AssertGreaterThan(far.ReadyStrength, 1.5f * one, "setup: each fleet has the strength alone");
        MilitaryTask task = WarTask();
        task.NeededTroopStrength = 0;
        task.TaskBombTimeNeeded = near.ReadyBombSecs + 1;
        AssertGreaterThan(near.ReadyBombSecs + far.ReadyBombSecs, task.TaskBombTimeNeeded, "setup: together their bombers cover the task");

        Requisition(task, 1.5f * one, simulate: false);

        Assert.AreEqual(near, task.Fleet, "the two idle fleets join and take the task");
        Assert.AreEqual(near, farBomber.Fleet, "with the other fleet's bomber");
    }

    [TestMethod]
    public void AnEndedDefenseTakesNoFleet()
    {
        Fleet idle = KeptFleet(Warships(2, Rally.Position + new Vector2(20_000f, 0f)));
        SolarSystem home = Rally.System;
        var defense = new MilitaryTask(MilitaryTask.TaskType.ClearAreaOfEnemies, Enemy, home.Position, home.Radius,
                                       home, 500f, MilitaryTaskImportance.Important);
        ReadyThePool();

        defense.Evaluate(Enemy);

        Assert.IsTrue(defense.QueuedForRemoval, "setup: with no threat left, the defense ends");
        Assert.IsTrue(idle.IsIdle, "and takes no fleet");
        Assert.IsNull(defense.Fleet, "for the task it ended");
    }

    [TestMethod]
    public void ATaskWaitsWhenNoForceHasTheStrength()
    {
        Fleet near = KeptFleet(Warships(1, NearTarget));
        Fleet far = KeptFleet(Warships(1, FarFromTarget));
        Ship pooled = Warships(1, NearTarget + new Vector2(20_000f, 0f))[0];
        float one = One(pooled);
        MilitaryTask task = WarTask();

        Requisition(task, 3.5f * one);

        Assert.IsNull(task.Fleet, "the task waits");
        Assert.IsTrue(near.IsIdle && far.IsIdle, "the fleets stay idle");
        AssertEqual(1, near.Ships.Count, "and as they were");
        Assert.IsNull(pooled.Fleet, "the pool ship stays free");
    }

    [TestMethod]
    public void AGuardTaskTakesAnIdleFleetOfAtMostTwiceItsNeed()
    {
        Ship[] ships = Warships(1, NearTarget);
        Fleet idle = KeptFleet(ships);
        float one = One(ships[0]);
        MilitaryTask guard = GuardTask();

        Requisition(guard, 0.75f * one);

        Assert.AreEqual(idle, guard.Fleet, "an idle fleet of no more than twice the need takes the guard task");
        Assert.IsTrue(idle.Kept, "and stays kept for after it");
    }

    [TestMethod]
    public void AGuardTaskLeavesABiggerIdleFleetAndFormsATemporaryForce()
    {
        Fleet idle = KeptFleet(Warships(2, NearTarget));
        float one = One(Warships(2, NearTarget + new Vector2(20_000f, 0f))[0]);
        MilitaryTask guard = GuardTask();

        Requisition(guard, 0.75f * one);

        Assert.IsNotNull(guard.Fleet, "a force from the pool takes the guard task");
        Assert.AreNotEqual(idle, guard.Fleet, "not the idle fleet of more than twice the need");
        Assert.IsTrue(idle.IsIdle, "which stays idle");
        Assert.IsFalse(guard.Fleet.CountsTowardFleetCap, "a temporary force counts toward no cap");
    }

    [TestMethod]
    public void AClaimTakesTheNearestIdleFleetWithTheStrengthWhateverItsSize()
    {
        Fleet far = KeptFleet(Warships(3, FarFromTarget)); // formed first, so the nearer one is not first by order alone
        Fleet near = KeptFleet(Warships(3, NearTarget));
        Ship pooled = Warships(1, NearTarget + new Vector2(20_000f, 0f))[0];
        float one = One(pooled);
        MilitaryTask claim = ClaimTask();

        Requisition(claim, 0.75f * one);

        Assert.AreEqual(near, claim.Fleet, "the nearest idle fleet of more than twice the need takes the claim, as for a war task");
        Assert.IsTrue(far.IsIdle, "the farther fleet stays idle");
        Assert.IsNull(pooled.Fleet, "and the pool ship stays free");
    }

    [TestMethod]
    public void AClaimFormsANewFleetFromThePoolUnderTheCapAndCountsTowardIt()
    {
        float one = One(Warships(3, NearTarget)[0]);
        MilitaryTask claim = ClaimTask();

        Requisition(claim, 1.5f * one);

        Assert.IsNotNull(claim.Fleet, "a new fleet forms from the pool");
        Assert.IsTrue(claim.Fleet.CountsTowardFleetCap, "and counts toward the cap");
    }

    [TestMethod]
    public void AtTheCapAClaimFormsNoNewFleetAndWaits()
    {
        FleetFor(WarTask(), Warships(1, FarFromTarget));
        Ship[] pooled = Warships(3, NearTarget);
        float one = One(pooled[0]);
        ReadyThePool();
        AssertEqual(1, Enemy.KeptFleetCap(), "setup: too few planet levels for a second fleet");
        AssertEqual(1, Enemy.KeptFleetCount(), "setup: the empire has one fleet, on a task");
        MilitaryTask claim = ClaimTask();

        Requisition(claim, 1.5f * one);

        Assert.IsNull(claim.Fleet, "no new fleet forms over the cap, so the claim waits");
        Assert.IsFalse(claim.QueuedForRemoval, "with its task still open");
        Assert.IsNull(pooled[0].Fleet, "and the pool ships stay free");
    }

    static void SetLevel(Planet planet, int level) => typeof(Planet).GetProperty(nameof(Planet.Level))!.SetValue(planet, level);

    // more planets of the empire, each at the level
    Planet[] EnemyPlanets(int count, int level)
    {
        var planets = new Planet[count];
        for (int i = 0; i < count; ++i)
        {
            var at = new Vector2(-1_400_000f + 400_000f * (i % 8), i < 8 ? 1_200_000f : 1_600_000f);
            planets[i] = AddDummyPlanetToEmpire(at, Enemy);
            SetLevel(planets[i], level);
        }
        return planets;
    }

    [TestMethod]
    public void TheFleetCapIsOneFleetPerTenPlanetLevelsAndOneMorePerDifficultyStep()
    {
        SetLevel(Rally, 5);
        EnemyPlanets(3, level: 5);

        AssertEqual(3, Enemy.KeptFleetCap(), "twenty planet levels make two fleets, and Normal one more");

        UState.P.Difficulty = GameDifficulty.Brutal;
        AssertEqual(5, Enemy.KeptFleetCap(), "Brutal adds three");
    }

    [TestMethod]
    public void TheFleetCapNeverFallsBelowHalfTheMostItHasHad()
    {
        SetLevel(Rally, 5);
        Planet[] planets = EnemyPlanets(15, level: 5);
        AssertEqual(9, Enemy.KeptFleetCap(), "setup: eighty planet levels on Normal");
        typeof(EmpireAI).GetMethod("JoinIdleFleetsOverTheCap", BindingFlags.Instance | BindingFlags.NonPublic)!.Invoke(Enemy.AI, null); // the empire's turn notes the cap

        foreach (Planet planet in planets)
            planet.SetOwner(Player);

        AssertEqual(4, Enemy.KeptFleetCap(), "after losing all but one planet the cap stays at half the most it had");
    }

    [TestMethod]
    public void IdleFleetsMergeOnlyHalfAStepBelowTheLine()
    {
        SetLevel(Rally, 5);
        Planet other = EnemyPlanets(1, level: 5)[0];
        Fleet a = KeptFleet(Warships(2, Rally.Position + new Vector2(500f, 0f)));
        Fleet b = KeptFleet(Warships(1, Rally.Position + new Vector2(3000f, 0f)));
        AssertEqual(2, Enemy.KeptFleetCap(), "setup: ten planet levels on Normal");
        MethodInfo join = typeof(EmpireAI).GetMethod("JoinIdleFleetsOverTheCap", BindingFlags.Instance | BindingFlags.NonPublic)!;

        SetLevel(other, 4);
        AssertEqual(1, Enemy.KeptFleetCap(), "setup: a planet one level down takes the cap under the line");
        join.Invoke(Enemy.AI, null);
        Assert.IsTrue(a.Ships.Count == 2 && b.Ships.Count == 1, "a planet changing level at the line merges no fleets");

        SetLevel(Rally, 1);
        join.Invoke(Enemy.AI, null);
        Assert.IsTrue(a.Ships.Count == 2 && b.Ships.Count == 1, "nor do five levels under the line");

        SetLevel(Rally, 0);
        join.Invoke(Enemy.AI, null);
        AssertEqual(3, a.Ships.Count, "six levels under the line, the weaker fleet joins the other");
    }

    [TestMethod]
    public void TheMostFleetsTheEmpireHasHadIsSaved()
    {
        typeof(Ship_Game.Empire).GetField("PeakFleetCap", BindingFlags.Instance | BindingFlags.NonPublic)!.SetValue(Enemy, 8);
        UState.StarDate = 1001f; // the loader needs a stardate past 1000
        SavedGame save = Universe.Save("UnitTest.PeakFleetCap", throwOnError: true);
        UniverseScreen loaded = LoadSave(save.SaveFile);
        Assert.IsNotNull(loaded, "setup: the save loads");

        AssertEqual(4, loaded.UState.GetEmpireById(Enemy.Id).KeptFleetCap(), "after a load the cap still stays at half the most it had");
    }

    [TestMethod]
    public void OverTheCapTheWeakestIdleFleetJoinsTheNearest()
    {
        Ship[] aShips = Warships(2, new Vector2(0f, -300_000f));
        Fleet a = KeptFleet(aShips);
        Fleet b = KeptFleet(Warships(2, new Vector2(300_000f, 300_000f)));
        Fleet c = KeptFleet(Warships(1, new Vector2(0f, -250_000f)));
        ReadyThePool();
        AssertEqual(1, Enemy.KeptFleetCap(), "setup: too few planet levels for more than one fleet");

        typeof(EmpireAI).GetMethod("JoinIdleFleetsOverTheCap", BindingFlags.Instance | BindingFlags.NonPublic)!.Invoke(Enemy.AI, null);

        AssertEqual(5, a.Ships.Count, "the weakest fleet joins the nearest, then the next weakest joins the rest");
        Assert.IsTrue(b.Ships.IsEmpty && c.Ships.IsEmpty, "until the empire is back at its cap");
        Assert.IsTrue(a.FinalPosition.InRadius(Rally.Position, 1f), "the joined fleet gathers at its rally planet");
    }

    [TestMethod]
    public void IdleFleetsJoiningAtTheRallyPlanetTheyOrbitStayInOrbit()
    {
        Ship[] aShips = Warships(2, Rally.Position + new Vector2(5000f, 0f));
        Ship[] bShips = Warships(2, Rally.Position + new Vector2(-5000f, 0f));
        Fleet a = KeptFleet(aShips);
        Fleet b = KeptFleet(bShips);
        Ship[] all = { aShips[0], aShips[1], bShips[0], bShips[1] };
        foreach (Ship ship in all)
            ship.OrderToOrbit(Rally, clearOrders: true);
        RunSimWhile((120, fatal: true), () => all.Any(s => !s.AI.IsOrbiting(Rally)));
        ReadyThePool(simulate: false);
        AssertEqual(1, Enemy.KeptFleetCap(), "setup: room for one fleet only");
        Assert.IsTrue(a.IdleRally == Rally && b.IdleRally == Rally, "setup: both fleets wait at the planet they orbit");

        typeof(EmpireAI).GetMethod("JoinIdleFleetsOverTheCap", BindingFlags.Instance | BindingFlags.NonPublic)!.Invoke(Enemy.AI, null);

        AssertEqual(4, (a.Ships.Count == 4 ? a : b).Ships.Count, "one fleet joins the other");
        Assert.IsFalse(all.Any(s => s.AI.State != AIState.Orbit), "the joined fleet stays in orbit instead of forming up beside the planet");
    }

    [TestMethod]
    public void AShipFightingWhenItsIdleFleetJoinsAnotherIsLeftToFight()
    {
        Ship[] aShips = Warships(2, new Vector2(0f, -300_000f));
        Fleet a = KeptFleet(aShips);
        Ship fighting = Warships(1, new Vector2(0f, -250_000f))[0];
        KeptFleet(fighting);
        ReadyThePool();
        fighting.InCombat = true;

        typeof(EmpireAI).GetMethod("JoinIdleFleetsOverTheCap", BindingFlags.Instance | BindingFlags.NonPublic)!.Invoke(Enemy.AI, null);

        AssertEqual(3, a.Ships.Count, "setup: the weaker fleet joins the other");
        Assert.IsFalse(fighting.AI.HasPriorityOrder, "a ship in a fight it can take is left to fight");
        Assert.IsTrue(aShips[0].AI.HasPriorityOrder, "while the rest take the fleet's move to its rally planet");
    }

    // player ships the AI has seen around a point
    void PlayerShipsSeen(Vector2 at, int count, SolarSystem system)
    {
        for (int i = 0; i < count; ++i)
        {
            Ship enemy = SpawnShip(Warship, Player, at + new Vector2(8000f, 1000f * i));
            enemy.SetSystem(system);
            Enemy.Threats.SetSeen(enemy, fromBackgroundThread: false);
        }
        Enemy.Threats.Update(new FixedSimTime(2f));
    }

    [TestMethod]
    public void AnIdleFleetOutmatchedAtTheTargetLeavesTheTaskToEnd()
    {
        Ship[] ships = Warships(1, NearTarget);
        Fleet idle = KeptFleet(ships);
        Assert.IsTrue(Enemy.IsEmpireAttackable(Player), "setup: the target's owner may be attacked");
        PlayerShipsSeen(Target.Position, 12, Target.System);
        MilitaryTask task = WarTask();

        Requisition(task, 0.5f * One(ships[0]));

        Assert.IsTrue(task.QueuedForRemoval, "a task whose force cannot take on the enemy at the target whole ends");
        Assert.IsTrue(idle.IsIdle, "and the idle fleet stays idle");
    }

    // a defense of the empire's own system, the player's ships seen there
    MilitaryTask DefenseWithPlayerShipsSeen(int count)
    {
        SolarSystem home = Rally.System;
        PlayerShipsSeen(Rally.Position, count, home);
        return new MilitaryTask(MilitaryTask.TaskType.ClearAreaOfEnemies, Enemy, home.Position, home.Radius * 1.5f,
                                home, 500f, MilitaryTaskImportance.Important);
    }

    [TestMethod]
    public void ADefenseThatWouldGiveUpItsFightAtOnceFormsNoFleet()
    {
        Ship[] ships = Warships(1, Rally.Position + new Vector2(20_000f, 0f));
        Fleet idle = KeptFleet(ships);
        float one = One(ships[0]);
        float playerShip = SpawnShip(Warship, Player, new Vector2(900_000f)).GetStrength();
        int count = (int)(one * Enemy.PersonalityModifiers.CanWeTakeThisFightMultiplier / (playerShip * Fleet.ClearAreaEnemyShare)) + 1;
        MilitaryTask defense = DefenseWithPlayerShipsSeen(count);
        float hostiles = Enemy.Threats.GetWeightedHostileStrengthAt(defense.AO, defense.AORadius);
        Assert.IsFalse(Fleet.CanTakeWeightedFight(Enemy, one, hostiles * Fleet.ClearAreaEnemyShare), "setup: the idle fleet would give up the fight there");

        Requisition(defense, 0.5f * one, simulate: false);

        Assert.IsNull(defense.Fleet, "a defense fleet that would give up its fight on its first update is not formed");
        Assert.IsTrue(idle.IsIdle, "and the idle fleet stays idle");
    }

    [TestMethod]
    public void ADefenseThatCanTakeItsFightFormsItsFleet()
    {
        Ship[] ships = Warships(1, Rally.Position + new Vector2(20_000f, 0f));
        Fleet idle = KeptFleet(ships);
        MilitaryTask defense = DefenseWithPlayerShipsSeen(1);

        Requisition(defense, 0.5f * One(ships[0]), simulate: false);

        Assert.AreEqual(idle, defense.Fleet, "the idle fleet takes a defense it can see through");
    }

    [TestMethod]
    public void AWeakerFleetOutmatchedAtTheTargetLeavesTheTaskToTwoFleetsJoinedInsteadOfEndingIt()
    {
        Ship[] nearShips = Warships(4, NearTarget);
        Fleet near = KeptFleet(nearShips[0], nearShips[1], nearShips[2], nearShips[3]);
        Ship farWarship = Warships(1, FarFromTarget)[0];
        Ship farTroops = Ships(AssaultTroopShip, 1, FarFromTarget + new Vector2(3000f, 0f))[0];
        Fleet far = KeptFleet(farWarship, farTroops);
        UState.Objects.Update(TestSimStep);
        AssertGreaterThan(far.ReadyTroopStrength, 0f, "setup: the farther fleet keeps its assault troops");
        AssertEqual(0f, near.ReadyTroopStrength, "setup: the nearer fleet carries none");
        MilitaryTask task = WarTask();
        task.NeededTroopStrength = (int)far.ReadyTroopStrength;
        task.TaskBombTimeNeeded = 0;
        float playerShip = SpawnShip(Warship, Player, new Vector2(900_000f)).GetStrength();
        int count = (int)(far.ReadyStrength * Enemy.PersonalityModifiers.CanWeTakeThisFightMultiplier / playerShip) + 1;
        PlayerShipsSeen(Target.Position, count, Target.System);
        Assert.IsTrue(task.ForceTooWeakToGoIn(far.ReadyStrength), "setup: the farther fleet alone is outmatched at the target");
        Assert.IsFalse(task.ForceTooWeakToGoIn(near.ReadyStrength + far.ReadyStrength), "setup: both fleets together are not");

        Requisition(task, 0.5f * One(farWarship), simulate: false);

        Assert.IsFalse(task.QueuedForRemoval, "the task does not end because the weaker fleet is outmatched");
        Assert.AreEqual(near, task.Fleet, "the two fleets join and take it");
        Assert.AreEqual(near, farTroops.Fleet, "with the other fleet's troops");
    }

    [TestMethod]
    public void ATaskWhoseStrongerFleetLacksTroopsWaitsWhenOnlyAnOutmatchedFleetHasThem()
    {
        Ship nearWarship = Warships(1, NearTarget)[0];
        Ship nearTroops = Ships(AssaultTroopShip, 1, NearTarget + new Vector2(3000f, 0f))[0];
        Fleet near = KeptFleet(nearWarship, nearTroops);
        Ship[] farShips = Warships(3, FarFromTarget);
        Fleet far = KeptFleet(farShips[0], farShips[1], farShips[2]);
        UState.Objects.Update(TestSimStep);
        AssertEqual(0f, far.ReadyTroopStrength, "setup: the farther fleet carries no troops");
        AssertGreaterThan(far.ReadyStrength, near.ReadyStrength, "setup: the farther fleet is the stronger one");
        MilitaryTask task = WarTask();
        task.NeededTroopStrength = (int)near.ReadyTroopStrength;
        task.TaskBombTimeNeeded = 0;
        float playerShip = SpawnShip(Warship, Player, new Vector2(900_000f)).GetStrength();
        float both = near.ReadyStrength + far.ReadyStrength;
        int count = (int)(both * Enemy.PersonalityModifiers.CanWeTakeThisFightMultiplier / playerShip) + 1;
        PlayerShipsSeen(Target.Position, count, Target.System);
        Assert.IsTrue(task.ForceTooWeakToGoIn(both), "setup: even both fleets together are outmatched at the target");
        ReadyThePool(simulate: false);
        task.MinimumTaskForceStrength = 0.5f * One(nearWarship);

        var status = (MilitaryTask.RequisitionStatus)CreateTaskFleet.Invoke(task, new object[] { 1f, false })!;

        AssertEqual(MilitaryTask.RequisitionStatus.NotEnoughTroopStrength, status, "the nearer fleet with the troops is outmatched first, and the stronger one lacks troops");
        Assert.IsFalse(task.QueuedForRemoval, "a fleet short only of troops keeps the task waiting instead of ending it");
        Assert.IsNull(task.Fleet, "with no fleet taken");
        Assert.IsTrue(near.IsIdle && far.IsIdle, "both fleets stay idle");
    }

    [TestMethod]
    public void IdleFleetsJoiningWeighTheirFightsTogether()
    {
        Ship[] aShips = Warships(2, Rally.Position + new Vector2(500f, 0f));
        KeptFleet(aShips);
        Ship joining = Warships(1, Rally.Position + new Vector2(3000f, 0f))[0];
        KeptFleet(joining);
        ReadyThePool();
        foreach (Ship ship in new[] { aShips[0], aShips[1], joining })
            ship.SetSystem(Rally.System);
        // the races differ, so the hostiles one ship cannot beat but three can are worked out from measured strengths
        float one = joining.GetStrength();
        float playerShip = SpawnShip(Warship, Player, new Vector2(900_000f)).GetStrength();
        int count = (int)(one * Enemy.PersonalityModifiers.CanWeTakeThisFightMultiplier / playerShip) + 1;
        PlayerShipsSeen(Rally.Position, count, Rally.System);
        float hostiles = Enemy.Threats.GetWeightedHostileStrengthAt(Rally.System.Position, Rally.System.Radius);
        Assert.IsFalse(Fleet.CanTakeWeightedFight(Enemy, one, hostiles), "setup: the joining fleet cannot take the fight alone");
        Assert.IsTrue(Fleet.CanTakeWeightedFight(Enemy, 3f * one, hostiles), "setup: both fleets together can");
        Assert.AreEqual(Rally.System, joining.System, "setup: the fight is in the system they both wait in");
        joining.InCombat = true;
        aShips[0].InCombat = true;

        typeof(EmpireAI).GetMethod("JoinIdleFleetsOverTheCap", BindingFlags.Instance | BindingFlags.NonPublic)!.Invoke(Enemy.AI, null);

        Assert.AreNotEqual(AIState.Flee, joining.AI.State, "together they can take the fight, so the ship stays in it");
        Assert.IsFalse(joining.AI.HasPriorityOrder, "left to fight");
    }

    [TestMethod]
    public void TakingATaskDropsThePlacesHeldForShipsAway()
    {
        Ship[] ships = Warships(2, NearTarget);
        Fleet fleet = KeptFleet(ships);
        float one = One(ships[0]);
        var refit = new RefitShip(Enemy);
        var held = new FleetDataNode { ShipName = Warship };
        fleet.DataNodes.Add(held);
        fleet.AssignGoal(held, refit);
        Assert.IsTrue(fleet.FindNodeWithGoal(refit, out _), "setup: a place held for a ship away refitting");

        Requisition(WarTask(), one);

        Assert.IsFalse(fleet.FindNodeWithGoal(refit, out _), "the place goes, so the refitted ship goes to the pool when done");
        AssertEqual(2, fleet.DataNodes.Count, "the places of the fleet's ships stay");
    }

    [TestMethod]
    public void AKeptFleetsOwnTroopShipsCountTowardTheTroopsATaskNeeds()
    {
        Ship warship = Warships(1, NearTarget)[0];
        Ship troops = Ships(AssaultTroopShip, 1, NearTarget + new Vector2(3000f, 0f))[0];
        Fleet fleet = KeptFleet(warship, troops);
        float one = One(warship);
        Ship pooledTroops = Ships(AssaultTroopShip, 1, NearTarget + new Vector2(20_000f, 0f))[0];
        AssertGreaterThan(fleet.ReadyTroopStrength, 0f, "setup: the fleet carries troops");
        MilitaryTask task = WarTask();
        task.NeededTroopStrength = (int)fleet.ReadyTroopStrength;

        Requisition(task, one);

        Assert.AreEqual(fleet, task.Fleet, "the fleet takes the task");
        Assert.IsNull(pooledTroops.Fleet, "with the troops it carries, no troop ship comes from the pool");
    }

    [TestMethod]
    public void TheTroopsAKeptFleetLacksComeFromThePool()
    {
        Ship warship = Warships(1, NearTarget)[0];
        Ship troops = Ships(AssaultTroopShip, 1, NearTarget + new Vector2(3000f, 0f))[0];
        Fleet fleet = KeptFleet(warship, troops);
        float one = One(warship);
        Ship pooledTroops = Ships(AssaultTroopShip, 1, NearTarget + new Vector2(20_000f, 0f))[0];
        MilitaryTask task = WarTask();
        task.NeededTroopStrength = (int)fleet.ReadyTroopStrength + 1;

        Requisition(task, one);

        Assert.AreEqual(fleet, task.Fleet, "the fleet takes the task");
        Assert.AreEqual(fleet, pooledTroops.Fleet, "and a troop ship from the pool joins it for the troops it lacks");
    }

    static void AssertSameRatio(FleetRatios expected, FleetRatios actual, string message)
    {
        AssertEqual(expected.MinFighters, actual.MinFighters, message);
        AssertEqual(expected.MinCorvettes, actual.MinCorvettes, message);
        AssertEqual(expected.MinFrigates, actual.MinFrigates, message);
        AssertEqual(expected.MinCruisers, actual.MinCruisers, message);
        AssertEqual(expected.MinBattleships, actual.MinBattleships, message);
        AssertEqual(expected.MinCapitals, actual.MinCapitals, message);
        AssertEqual(expected.MinBombers, actual.MinBombers, message);
        AssertEqual(expected.MinCarriers, actual.MinCarriers, message);
        AssertEqual(expected.MinSupport, actual.MinSupport, message);
    }

    [TestMethod]
    public void ARandomFleetRatioRollsTheSameEveryTimeAndAfterALoad()
    {
        UState.P.EnableRandomizedAIFleetSizes = true;
        var first = new FleetRatios(Enemy);

        for (int i = 0; i < 5; ++i)
            AssertSameRatio(first, new FleetRatios(Enemy), "the empire's ratio rolls the same every time");

        UState.StarDate = 1001f; // the loader needs a stardate past 1000
        SavedGame save = Universe.Save("UnitTest.FleetRatioSeed", throwOnError: true);
        UniverseScreen loaded = LoadSave(save.SaveFile);
        Assert.IsNotNull(loaded, "setup: the save loads");
        Ship_Game.Empire enemy = loaded.UState.GetEmpireById(Enemy.Id);

        AssertEqual(Enemy.FleetRatioSeed, enemy.FleetRatioSeed, "the seed is saved");
        AssertSameRatio(first, new FleetRatios(enemy), "and the ratio rolls the same after a load");
    }

    [TestMethod]
    public void ADefenseTaskTakesTheFleetOfATaskItEndedToFreeShips()
    {
        Planet invaded = AddDummyPlanetToEmpire(new Vector2(0f, 400_000f), Enemy);
        Ship[] ships = Warships(2, invaded.Position + new Vector2(5000f, 0f));
        Fleet fleet = FleetFor(WarTask(), ships);
        float one = One(ships[0]);
        MilitaryTask postInvasion = MilitaryTask.CreatePostInvasion(invaded, fleet, Enemy);
        fleet.FleetTask = postInvasion;
        SolarSystem threatened = Rally.System;
        var defense = new MilitaryTask(MilitaryTask.TaskType.ClearAreaOfEnemies, Enemy, threatened.Position, threatened.Radius,
                                       threatened, 500f, MilitaryTaskImportance.Important);
        Enemy.AI.AddPendingTask(postInvasion);
        Enemy.AI.AddPendingTask(defense);
        Enemy.AI.ApplyPendingChanges();

        typeof(EmpireDefense).GetMethod("AssessDefense", BindingFlags.Instance | BindingFlags.NonPublic)!.Invoke(new EmpireDefense(Enemy), null);
        Assert.IsTrue(postInvasion.QueuedForRemoval, "setup: the defense ends the post-invasion task to free its fleet");

        Requisition(defense, one);

        Assert.AreEqual(fleet, defense.Fleet, "the defense task takes the fleet it freed");
    }

    [TestMethod]
    public void AnEndedDefenseEndsNoOtherTaskToFreeShips()
    {
        Planet invaded = AddDummyPlanetToEmpire(new Vector2(0f, 400_000f), Enemy);
        Fleet fleet = FleetFor(WarTask(), Warships(2, invaded.Position + new Vector2(5000f, 0f)));
        MilitaryTask postInvasion = MilitaryTask.CreatePostInvasion(invaded, fleet, Enemy);
        fleet.FleetTask = postInvasion;
        SolarSystem threatened = Rally.System;
        var defense = new MilitaryTask(MilitaryTask.TaskType.ClearAreaOfEnemies, Enemy, threatened.Position, threatened.Radius,
                                       threatened, 500f, MilitaryTaskImportance.Important);
        Enemy.AI.AddPendingTask(postInvasion);
        Enemy.AI.AddPendingTask(defense);
        Enemy.AI.ApplyPendingChanges();
        defense.EndTask();
        Assert.IsTrue(defense.QueuedForRemoval && Enemy.AI.GetDefendSystemTasks().Contains(defense),
                      "setup: the defense has ended and is still listed until the planner's next pass");

        typeof(EmpireDefense).GetMethod("AssessDefense", BindingFlags.Instance | BindingFlags.NonPublic)!.Invoke(new EmpireDefense(Enemy), null);

        Assert.IsFalse(postInvasion.QueuedForRemoval, "an ended defense calls off no other task to free ships for it");
    }
}
