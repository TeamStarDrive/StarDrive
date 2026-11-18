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
/// When an AI war, defense or claim task ends, its fleet stays together: single troop ships leave it and assault troop
/// ships stay, goals let go of it, every ship's task orders give way to the fleet's move or orbit while its fleet tactic
/// stays, a ship already orbiting the rally planet keeping its orbit, an outmatched ship that is fighting flees but stays
/// in it, a ship in a fight it can take is left to fight, and the rest wait at the planet a claim won, else head for the
/// nearest rally planet in a system the fleet can hold, where it goes into orbit once most of it is there, or at once
/// when it already is, headed for that planet either way; the rally planet it had before the task is forgotten. A guard
/// or exploration force, a fleet of fewer ships than half a fleet ratio, and a fleet whose empire has no planet left,
/// still break up. A kept fleet with no task is not disbanded; it leaves a rally planet the empire lost at
/// once and one in a system whose enemies outmatch it, counting its ships away resupplying, stays to fight where it
/// can win, holds a system with no enemies even with no strength, keeps its rally planet when it can hold no system,
/// and its ships keep to a guard's or a moving fleet's chase limits. It leaves the empire's fleets once it has no
/// ships and counts toward the strength the AI weighs before a war. Ending a task again leaves its old fleet to the
/// task it has moved on to.
/// </summary>
[TestClass]
public class KeptFleetTests : StarDriveTest
{
    const string Warship = "Terran-Prototype";
    const string AssaultTroopShip = "Proton Troopship  Mk1";
    static readonly Vector2 Away = new(200_000f);
    readonly Planet Rally;
    readonly Planet Target;

    public KeptFleetTests()
    {
        LoadStarterShips(Warship, AssaultTroopShip);
        CreateUniverseAndPlayerEmpire();
        LoadStarterShips(Enemy.data.DefaultTroopShip);
        UState.Objects.EnableParallelUpdate = false;
        Rally = AddDummyPlanetToEmpire(new Vector2(-400_000f), Enemy);
        Target = AddDummyPlanetToEmpire(new Vector2(400_000f), Player);
        Enemy.FleetStrEmpireMultiplier[Player.Id] = 1f;
    }

    MilitaryTask WarTask() => new(Target, Enemy, MilitaryTaskImportance.Normal);

    // the task's fleet, formed the way requisition forms one
    static Fleet FleetFor(MilitaryTask task, params Ship[] ships)
    {
        MethodInfo create = typeof(MilitaryTask).GetMethod("CreateFleet", BindingFlags.Instance | BindingFlags.NonPublic,
                                                           null, new[] { typeof(Array<Ship>), typeof(string) }, null);
        create!.Invoke(task, new object[] { new Array<Ship>(ships), "Test Fleet" });
        Assert.AreEqual(task, task.Fleet.FleetTask, "setup: the fleet holds its task");
        return task.Fleet;
    }

    Ship[] Warships(int count, Vector2 at)
    {
        var ships = new Ship[count];
        for (int i = 0; i < count; ++i)
            ships[i] = SpawnShip(Warship, Enemy, at + new Vector2(0f, 1000f * i));
        return ships;
    }

    // a war fleet whose task has ended
    Fleet KeptFleet(Ship[] ships)
    {
        MilitaryTask task = WarTask();
        Fleet fleet = FleetFor(task, ships);
        task.EndTask();
        Assert.IsTrue(fleet.IsIdle, "setup: the fleet is kept with no task");
        return fleet;
    }

    static bool IsListed(Empire empire, Fleet fleet)
    {
        foreach (Fleet f in empire.AllFleets)
            if (f == fleet)
                return true;
        return false;
    }

    static bool Orbits(Ship ship, Planet planet) => ship.AI.FindGoal(ShipAI.Plan.Orbit, out ShipAI.ShipGoal orbit) && orbit.TargetPlanet == planet;

    // player ships the AI has seen around a point
    void PlayerShipsSeen(Vector2 at, int count, SolarSystem system = null)
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
    public void AFleetOfFewerShipsThanHalfAFleetRatioBreaksUpWhenItsTaskEnds()
    {
        MilitaryTask task = WarTask();
        Ship[] ships = Warships(1, Away);
        Fleet fleet = FleetFor(task, ships);
        FleetShips pool = Enemy.ShipsReadyForFleet = new FleetShips(Enemy, Enemy.AllFleetReadyShips()); // as the empire's turn does
        AssertGreaterThan(pool.ShipsPerFullFleet / 2f, 1f, "setup: one ship is fewer than half a fleet ratio");

        task.EndTask();

        Assert.IsFalse(fleet.Kept, "the fleet is not kept for a turn only to break up then");
        Assert.IsNull(ships[0].Fleet, "its ship is free at once");
    }

    [TestMethod]
    public void AClaimFleetWaitsAtThePlanetItsClaimWon()
    {
        Planet claimed = AddDummyPlanet(new Vector2(0f, 400_000f));
        MilitaryTask claim = MilitaryTask.CreateClaimTask(Enemy, claimed, 100f, null, fleetCount: 1);
        Ship[] ships = Warships(2, Rally.Position + new Vector2(150_000f));
        Fleet fleet = FleetFor(claim, ships);
        Assert.IsTrue(fleet.AveragePosition(force: true).SqDist(Rally.Position) < fleet.AveragePosition(force: true).SqDist(claimed.Position),
                      "setup: another planet of the empire is nearer the fleet");
        claimed.SetOwner(Enemy);

        claim.EndTask();

        Assert.AreEqual(claimed, fleet.IdleRally, "the fleet waits at the planet its claim won");
    }

    [TestMethod]
    public void AFleetAlreadyInOrbitOfItsRallyPlanetStaysInOrbitWhenItsTaskEnds()
    {
        MilitaryTask task = WarTask();
        Ship[] ships = Warships(3, Rally.Position + new Vector2(5000f, 0f));
        Fleet fleet = FleetFor(task, ships);
        foreach (Ship ship in ships)
            ship.OrderToOrbit(Rally, clearOrders: true);
        RunSimWhile((120, fatal: true), () => ships.Any(s => !s.AI.IsOrbiting(Rally)));
        Assert.IsFalse(fleet.FinalPosition.InRadius(Rally.Position, 1f), "setup: the fleet's task left it headed elsewhere");

        task.EndTask();

        Assert.AreEqual(Rally, fleet.IdleRally, "setup: the fleet waits at the planet it orbits");
        Assert.IsFalse(ships.Any(s => s.AI.State != AIState.Orbit || !Orbits(s, Rally)), "it stays in orbit instead of forming up beside the planet");
        Assert.IsTrue(fleet.FinalPosition.InRadius(Rally.Position, 1f), "and is headed for the planet, not where its task last sent it");
    }

    [TestMethod]
    public void AWarFleetStaysTogetherWhenItsTaskEnds()
    {
        MilitaryTask task = WarTask();
        Fleet fleet = FleetFor(task, Warships(2, Away));

        task.EndTask();

        Assert.IsTrue(task.QueuedForRemoval, "setup: the task ended");
        Assert.IsTrue(fleet.Kept, "the fleet is kept");
        Assert.IsNull(fleet.FleetTask, "with no task");
        AssertEqual(2, fleet.Ships.Count, "its ships stay in it");
        Assert.AreNotEqual(0, fleet.Key, "it keeps its key");
        Assert.IsTrue(IsListed(Enemy, fleet), "and stays among the empire's fleets");
    }

    [TestMethod]
    public void AClaimFleetStaysTogetherWhenItsTaskEnds()
    {
        MilitaryTask task = MilitaryTask.CreateClaimTask(Enemy, AddDummyPlanet(new Vector2(0f, 400_000f)), 100f, null, fleetCount: 1);
        Fleet fleet = FleetFor(task, Warships(2, Away));

        task.EndTask();

        Assert.IsTrue(fleet.IsIdle, "a claim fleet is kept with no task");
        AssertEqual(2, fleet.Ships.Count, "its ships stay in it");
        Assert.AreEqual(Rally, fleet.IdleRally, "and it heads for a rally planet");
    }

    [TestMethod]
    public void AGuardForceStillBreaksUpWhenItsTaskEnds()
    {
        MilitaryTask task = MilitaryTask.CreateGuardTask(Enemy, AddDummyPlanet(new Vector2(0f, 400_000f)));
        Fleet fleet = FleetFor(task, Warships(2, Away));

        task.EndTask();

        Assert.IsFalse(fleet.Kept, "a guard force is not kept");
        Assert.IsTrue(fleet.Ships.IsEmpty, "its ships leave it");
        AssertEqual(0, fleet.Key, "and it is reset");
    }

    [TestMethod]
    public void AnExplorationForceStillBreaksUpWhenItsTaskEnds()
    {
        MilitaryTask task = MilitaryTask.CreateExploration(AddDummyPlanet(new Vector2(0f, 400_000f)), Enemy);
        Fleet fleet = FleetFor(task, Warships(2, Away));

        task.EndTask();

        Assert.IsFalse(fleet.Kept, "an exploration force is not kept");
        Assert.IsTrue(fleet.Ships.IsEmpty, "its ships leave it");
        AssertEqual(0, fleet.Key, "and it is reset");
    }

    [TestMethod]
    public void SingleTroopShipsLeaveAKeptFleetAndAssaultTroopShipsStay()
    {
        Ship single = SpawnShip(Enemy.data.DefaultTroopShip, Enemy, Away + new Vector2(3000f, 0f));
        Ship assault = SpawnShip(AssaultTroopShip, Enemy, Away + new Vector2(5000f, 0f));
        AssertEqual(RoleName.troop, single.DesignRole, "setup: a single troop ship");
        AssertEqual(RoleName.troopShip, assault.DesignRole, "setup: an assault troop ship");
        MilitaryTask task = WarTask();
        Fleet fleet = FleetFor(task, Warships(1, Away)[0], single, assault);

        task.EndTask();

        Assert.IsNull(single.Fleet, "the single troop ship leaves the fleet when its task ends");
        Assert.AreEqual(fleet, assault.Fleet, "the assault troop ship stays, for a system the fleet may have to retake");
    }

    [TestMethod]
    public void GoalsLetGoOfAKeptFleet()
    {
        MilitaryTask task = WarTask();
        Fleet fleet = FleetFor(task, Warships(1, Away));
        var goal = new WarMission(Enemy) { Fleet = fleet, Task = task };
        Enemy.AI.AddGoal(goal);

        task.EndTask();

        Assert.IsNull(goal.Task, "setup: the goal lets go of the ended task");
        Assert.IsNull(goal.Fleet, "and of the kept fleet, which is free for another task");
    }

    [TestMethod]
    public void AKeptFleetHeadsForASafeRallyPlanetAndIsNotDisbanded()
    {
        Fleet fleet = KeptFleet(Warships(2, Away));

        Assert.AreEqual(Rally, fleet.IdleRally, "the fleet heads for the empire's rally planet");
        Assert.IsTrue(fleet.FinalPosition.InRadius(Rally.Position, 1f), "its move ends there");

        fleet.UpdateAI(TestSimStep);

        Assert.IsTrue(fleet.IsIdle, "its next update keeps it, with no task");
        AssertEqual(2, fleet.Ships.Count, "and with its ships");
        Assert.AreNotEqual(0, fleet.Key, "and its key");
    }

    [TestMethod]
    public void AKeptFleetGoesIntoOrbitOnceMostOfItIsAtItsRallyPlanet()
    {
        Fleet far = KeptFleet(Warships(2, Away));
        Fleet near = KeptFleet(Warships(2, Rally.Position + new Vector2(500f, 0f)));

        far.UpdateAI(TestSimStep);
        near.UpdateAI(TestSimStep);

        Assert.IsFalse(far.Ships.Any(s => Orbits(s, Rally)), "a fleet still on its way does not go into orbit");
        Assert.IsFalse(near.Ships.Any(s => !Orbits(s, Rally)), "a fleet that is there goes into orbit of its rally planet");
        AssertEqual(2, near.Ships.Count, "setup: with both its ships");
    }

    [TestMethod]
    public void AnOutmatchedShipFleesButStaysInItsKeptFleet()
    {
        Ship[] ships = Warships(2, Away);
        MilitaryTask task = WarTask();
        Fleet fleet = FleetFor(task, ships);
        ships[0].InCombat = true;
        PlayerShipsSeen(Away, 12);

        task.EndTask();

        AssertEqual(AIState.Flee, ships[0].AI.State, "a ship fighting a force its fleet cannot beat flees");
        Assert.AreEqual(fleet, ships[0].Fleet, "and stays in the fleet");
        Assert.AreNotEqual(AIState.Flee, ships[1].AI.State, "a ship that is not fighting does not flee");
    }

    [TestMethod]
    public void AShipInAFightItCanTakeIsLeftToFightWhileTheRestHeadForTheRallyPlanet()
    {
        Ship[] ships = Warships(2, Away);
        MilitaryTask task = WarTask();
        Fleet fleet = FleetFor(task, ships);
        ships[0].InCombat = true;
        PlayerShipsSeen(Away, 1);

        task.EndTask();

        Assert.AreNotEqual(AIState.Flee, ships[0].AI.State, "a ship fighting a weaker force does not flee");
        Assert.IsFalse(ships[0].AI.HasPriorityOrder, "it is left to fight");
        Assert.IsTrue(ships[1].AI.HasPriorityOrder, "while the ship not fighting takes the fleet's move to the rally planet");
        Assert.AreEqual(fleet, ships[0].Fleet, "setup: both stay in the fleet");
    }

    [TestMethod]
    public void AnIdleFleetLeavesARallyPlanetTheEmpireLostAtOnceEvenWhileAShipFights()
    {
        Planet farther = AddDummyPlanetToEmpire(new Vector2(-800_000f), Enemy);
        Ship[] ships = Warships(2, Rally.Position + new Vector2(500f, 0f));
        Fleet fleet = KeptFleet(ships);
        Assert.AreEqual(Rally, fleet.IdleRally, "setup: the fleet waits at the nearer rally planet");
        Rally.SetOwner(Player);
        ships[0].InCombat = true;

        fleet.UpdateAI(TestSimStep);

        Assert.AreEqual(farther, fleet.IdleRally, "a lost rally planet is left at once, even while a ship of the fleet fights");
        Assert.IsTrue(fleet.FinalPosition.InRadius(farther.Position, 1f), "the fleet's move ends at the new rally planet");
    }

    [TestMethod]
    public void AnIdleFleetWhoseEmpireLostItsLastPlanetBreaksUp()
    {
        Ship[] ships = Warships(2, Rally.Position + new Vector2(500f, 0f));
        Fleet fleet = KeptFleet(ships);
        Rally.SetOwner(Player);

        fleet.UpdateAI(TestSimStep);

        Assert.IsTrue(fleet.Ships.IsEmpty, "with no planet left to wait at, the fleet breaks up");
        Assert.IsNull(ships[0].Fleet, "and its ships go their own way");
    }

    [TestMethod]
    public void AnIdleFleetOutmatchedEverywhereKeepsItsRallyPlanet()
    {
        Planet farther = AddDummyPlanetToEmpire(new Vector2(-800_000f), Enemy);
        Fleet fleet = KeptFleet(Warships(2, Rally.Position + new Vector2(500f, 0f)));
        typeof(Planet).GetProperty(nameof(Planet.SpaceCombatNearPlanet))!.SetValue(Rally, true);
        Enemy.UpdateRallyPoints();
        PlayerShipsSeen(Rally.Position, 12, Rally.System);
        PlayerShipsSeen(farther.Position, 12, farther.System);
        Assert.IsFalse(Rally.Safe, "setup: the rally planet is fought over, the other one is not");

        fleet.UpdateAI(TestSimStep);

        Assert.AreEqual(Rally, fleet.IdleRally, "with no system it can hold, the fleet makes no trip to another planet it cannot hold either");
        Assert.IsTrue(fleet.IsIdle && fleet.Ships.Count == 2, "and it stays together");
    }

    [TestMethod]
    public void AnOutmatchedIdleFleetLeavesARallyPlanetThatStillLooksSafe()
    {
        Planet farther = AddDummyPlanetToEmpire(new Vector2(-800_000f), Enemy);
        Fleet fleet = KeptFleet(Warships(2, Rally.Position + new Vector2(500f, 0f)));
        PlayerShipsSeen(Rally.System.Position + new Vector2(60_000f, 0f), 12, Rally.System);
        Enemy.UpdateRallyPoints();
        Assert.IsTrue(Rally.Safe, "setup: the enemy fleet is in the system but too far from the planet to mark it unsafe");

        fleet.UpdateAI(TestSimStep);

        Assert.AreEqual(farther, fleet.IdleRally, "the fleet leaves for a planet in a system it can hold");
    }

    [TestMethod]
    public void AnIdleFleetStaysToFightAtItsRallyPlanetUnlessTheEnemiesThereOutmatchIt()
    {
        Planet farther = AddDummyPlanetToEmpire(new Vector2(-800_000f), Enemy);
        Fleet fleet = KeptFleet(Warships(2, Rally.Position + new Vector2(500f, 0f)));
        typeof(Planet).GetProperty(nameof(Planet.SpaceCombatNearPlanet))!.SetValue(Rally, true);
        Enemy.UpdateRallyPoints();
        Assert.IsFalse(Rally.Safe, "setup: there is fighting at the rally planet");
        PlayerShipsSeen(Rally.Position, 1, Rally.System);

        fleet.UpdateAI(TestSimStep);
        Assert.AreEqual(Rally, fleet.IdleRally, "a fleet that can beat the enemies at its rally planet stays to fight");

        PlayerShipsSeen(Rally.Position, 12, Rally.System);
        fleet.UpdateAI(TestSimStep);
        Assert.AreEqual(farther, fleet.IdleRally, "one they outmatch leaves for another rally planet");
    }

    [TestMethod]
    public void AnIdleFleetCountsItsShipsAwayResupplyingWhenItWeighsTheEnemiesAtItsRallyPlanet()
    {
        AddDummyPlanetToEmpire(new Vector2(-800_000f), Enemy);
        Ship[] ships = Warships(4, Rally.Position + new Vector2(500f, 0f));
        Fleet fleet = KeptFleet(ships);
        for (int i = 1; i < ships.Length; ++i)
            ships[i].AI.ChangeAIState(AIState.Resupply);
        // the races differ, so the count of player ships that outweighs the one ship not resupplying is worked out
        float playerShip = SpawnShip(Warship, Player, new Vector2(900_000f)).GetStrength();
        int count = (int)(fleet.ReadyStrength * Enemy.PersonalityModifiers.CanWeTakeThisFightMultiplier / playerShip) + 1;
        PlayerShipsSeen(Rally.Position, count, Rally.System);
        float hostiles = Enemy.Threats.GetWeightedHostileStrengthAt(Rally.System.Position, Rally.System.Radius);
        Assert.IsFalse(Fleet.CanTakeWeightedFight(Enemy, fleet.ReadyStrength, hostiles), "setup: the ship not resupplying cannot beat the enemies there alone");
        Assert.IsTrue(Fleet.CanTakeWeightedFight(Enemy, fleet.GetStrength(), hostiles), "setup: the whole fleet can");

        fleet.UpdateAI(TestSimStep);

        Assert.AreEqual(Rally, fleet.IdleRally, "the fleet stays at its rally planet");
        AssertEqual(AIState.Resupply, ships[1].AI.State, "and calls no ship back from resupplying");
        Assert.IsTrue(fleet.IsIdle && fleet.Ships.Count == 4, "it stays together");
    }

    [TestMethod]
    public void AnIdleFleetWithNoStrengthStillHoldsASystemWithNoEnemies()
    {
        Planet farther = AddDummyPlanetToEmpire(new Vector2(-800_000f), Enemy);
        Ship[] ships = Warships(2, Rally.Position + new Vector2(500f, 0f));
        Fleet fleet = KeptFleet(ships);
        foreach (Ship ship in ships)
            typeof(Ship).GetField("CurrentStrength", BindingFlags.Instance | BindingFlags.NonPublic)!.SetValue(ship, 0f);
        AssertEqual(0f, fleet.GetStrength(), "setup: a fleet with no strength, like one left with only unarmed ships");
        PlayerShipsSeen(Rally.Position, 1, Rally.System);

        fleet.UpdateAI(TestSimStep);

        Assert.AreEqual(farther, fleet.IdleRally, "it leaves the enemies at its rally planet for a system with none");
    }

    [TestMethod]
    public void AFleetEndingANewTaskForgetsTheRallyPlanetItHadBefore()
    {
        Planet farther = AddDummyPlanetToEmpire(new Vector2(-800_000f), Enemy);
        Fleet fleet = KeptFleet(Warships(2, farther.Position + new Vector2(500f, 0f)));
        typeof(Fleet).GetProperty(nameof(Fleet.IdleRally))!.SetValue(fleet, Rally); // where it waited before its next task
        MilitaryTask next = WarTask();
        fleet.FleetTask = next;
        typeof(MilitaryTask).GetProperty(nameof(MilitaryTask.Fleet))!.SetValue(next, fleet);
        PlayerShipsSeen(Rally.Position, 12, Rally.System);
        PlayerShipsSeen(farther.Position, 12, farther.System);

        next.EndTask();

        Assert.AreEqual(farther, fleet.IdleRally, "outmatched everywhere, it waits at the planet nearest to where its task ended, not at its old rally planet");
    }

    static void UpdateChaseLimits(Ship ship)
        => typeof(ShipAI).GetMethod("UpdateChaseLimits", BindingFlags.Instance | BindingFlags.NonPublic)!.Invoke(ship.AI, null);

    static object ChaseLimit(Ship ship, string field)
        => typeof(ShipAI).GetField(field, BindingFlags.Instance | BindingFlags.NonPublic)!.GetValue(ship.AI);

    [TestMethod]
    public void AShipOfAnIdleFleetInOrbitGuardsTheSystemOfItsRallyPlanet()
    {
        Fleet fleet = KeptFleet(Warships(2, Rally.Position + new Vector2(500f, 0f)));
        fleet.UpdateAI(TestSimStep);
        Ship ship = fleet.Ships[0];
        Assert.IsTrue(Orbits(ship, Rally), "setup: the ship orbits the rally planet");

        UpdateChaseLimits(ship);

        Assert.AreEqual(Rally.System, ChaseLimit(ship, "GuardedSystem"), "it guards that system and chases no farther than a guard outside any fleet");
    }

    [TestMethod]
    public void AShipOfAnIdleFleetOnItsWayToTheRallyPlanetKeepsNearItsFleet()
    {
        Fleet fleet = KeptFleet(Warships(2, Away));
        Ship ship = fleet.Ships[0];

        UpdateChaseLimits(ship);

        Assert.IsNull(ChaseLimit(ship, "GuardedSystem"), "a ship on its way guards no system");
        Assert.AreEqual(true, ChaseLimit(ship, "OnFleetMove"), "it keeps to the limits of a ship on its fleet's move");
    }

    [TestMethod]
    public void EveryShipDropsItsTaskOrdersForTheMoveToTheRallyPlanet()
    {
        Ship[] ships = Warships(2, Away);
        MilitaryTask task = WarTask();
        FleetFor(task, ships);
        ships[0].AI.OrderToOrbit(Target, clearOrders: true);

        task.EndTask();

        Assert.IsFalse(ships[0].AI.FindGoal(ShipAI.Plan.Orbit, out _), "the ship's order from the task is replaced by the fleet's move");
    }

    [TestMethod]
    public void AShipKeepsTheTacticItsFleetGaveItWhenTheFleetIsKept()
    {
        Ship[] ships = Warships(2, Away);
        MilitaryTask task = WarTask();
        FleetFor(task, ships);
        CombatState tactic = ships[0].ShipData.DefaultCombatState == CombatState.Artillery ? CombatState.ShortRange : CombatState.Artillery;
        ships[0].AI.CombatState = tactic;

        task.EndTask();

        AssertEqual(tactic, ships[0].AI.CombatState, "the ship keeps its tactic for the fleet's next fight");
    }

    [TestMethod]
    public void AFleetWithNoPlanetLeftToWaitAtBreaksUpWhenItsTaskEnds()
    {
        Ship[] ships = Warships(2, Away);
        MilitaryTask task = WarTask();
        Fleet fleet = FleetFor(task, ships);
        Rally.SetOwner(Player);
        Assert.IsNull(Enemy.FindNearestSafeRallyPoint(Away), "setup: the empire has no planet left");

        task.EndTask();

        Assert.IsTrue(fleet.Ships.IsEmpty, "the fleet breaks up");
        AssertEqual(0, fleet.Key, "and is reset");
        Assert.IsNull(ships[0].Fleet, "its ships go their own way");
    }

    [TestMethod]
    public void AKeptFleetLeavesTheEmpiresFleetsOnceItHasNoShips()
    {
        Ship[] ships = Warships(2, Away);
        Fleet fleet = KeptFleet(ships);
        foreach (Ship ship in ships)
            fleet.RemoveShip(ship, clearOrders: false);

        Enemy.UpdateFleets(TestSimStep);

        Assert.IsFalse(IsListed(Enemy, fleet), "an idle fleet with no ships left is dropped");
    }

    [TestMethod]
    public void TheStrengthFreeForANewTaskCountsIdleFleetsButNotFleetsOnATask()
    {
        SpawnShip(Warship, Enemy, Away + new Vector2(0f, -20_000f));
        Fleet idle = KeptFleet(Warships(2, Away));
        Fleet busy = FleetFor(WarTask(), Warships(1, Away + new Vector2(20_000f, 0f)));
        RunObjectsSim(TestSimStep);
        Enemy.ShipsReadyForFleet = new FleetShips(Enemy, Enemy.AllFleetReadyShips());
        AssertGreaterThan(idle.ReadyStrength, 0f, "setup: the idle fleet has strength");
        AssertGreaterThan(busy.GetStrength(), 0f, "setup: so does the fleet on a task");

        float expected = Enemy.ShipsReadyForFleet.AccumulatedStrength + idle.ReadyStrength;
        AssertEqual(1f, expected, Enemy.StrengthReadyForTasks(), "warships in no fleet and idle fleets are free, a fleet on a task is not");
    }

    [TestMethod]
    public void EndingATaskAgainLeavesItsOldFleetToTheTaskItMovedOnTo()
    {
        MilitaryTask first = WarTask();
        Fleet fleet = FleetFor(first, Warships(2, Away));
        first.EndTask();
        MilitaryTask next = WarTask();
        fleet.FleetTask = next;
        typeof(MilitaryTask).GetProperty(nameof(MilitaryTask.Fleet))!.SetValue(next, fleet);

        first.EndTask();

        Assert.AreEqual(next, fleet.FleetTask, "the fleet keeps the task it moved on to");
    }
}
