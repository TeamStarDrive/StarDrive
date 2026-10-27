using System.Reflection;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using SDUtils;
using Ship_Game;
using Ship_Game.AI;
using Ship_Game.AI.Tasks;
using Ship_Game.Commands.Goals;
using Ship_Game.Fleets;
using Ship_Game.Gameplay;
using Ship_Game.Ships;
using Vector2 = SDGraphics.Vector2;

namespace UnitTests.Fleets;

/// <summary>
/// An AI war fleet keeps out of a fight between its enemy and a third empire hostile to it. The war's pick of
/// targets passes over a system where it knows such an empire's armed ships are. A fleet about to go in waits
/// outside while they are there, unless it is at war with them all and strong enough for everyone, and after two
/// minutes it moves on to the enemy's planet in a nearby system without them. A fleet that went in stays.
/// </summary>
[TestClass]
public class ThirdPartyTargetTests : StarDriveTest
{
    const string Warship = "Terran-Prototype";
    const string Bomber = "Neutron Bomber Mk1";
    const string ResearchStation = "Basic Research Station";
    const string Constructor = "Terran Constructor";
    readonly Planet Target;

    public ThirdPartyTargetTests()
    {
        LoadStarterShips(Warship, Bomber, ResearchStation, Constructor);
        CreateUniverseAndPlayerEmpire();
        CreateThirdMajorEmpire();
        LoadStarterShips(Enemy.data.DefaultTroopShip);
        Enemy.AddBuildableShip(ResourceManager.Ships.GetDesign(Constructor)); // a fleet heading for a new target sends a constructor for its projector
        UState.Objects.EnableParallelUpdate = false;
        UState.Events.Disabled = true;
        UState.P.TurnTimer = 5;
        Player.TotalScore = Enemy.TotalScore = 100;
        Enemy.ShipsReadyForFleet = new FleetShips(Enemy, Enemy.AllFleetReadyShips()); // set by the empire's turn in a game
        Target = AddDummyPlanetToEmpire(new Vector2(400_000), Player);
    }

    static object Call(object instance, string method, params object[] args)
        => instance.GetType().GetMethod(method, BindingFlags.Instance | BindingFlags.NonPublic)!.Invoke(instance, args);

    void HostileThirdEmpire()
    {
        Relationship rel = Enemy.GetRelations(ThirdMajor);
        rel.CanAttack = true;
        rel.IsHostile = true;
        Assert.IsTrue(Enemy.IsEmpireHostile(ThirdMajor) && !Enemy.IsAtWarWith(ThirdMajor), "setup: the third empire is hostile, not at war");
    }

    void AtWarWithThirdEmpire()
    {
        Enemy.AI.DeclareWarOn(ThirdMajor, WarType.BorderConflict);
        Assert.IsTrue(Enemy.IsEmpireHostile(ThirdMajor) && Enemy.IsAtWarWith(ThirdMajor), "setup: at war with the third empire");
    }

    // ships the attacking empire has seen, placed in the system around pos
    Ship[] SeenAt(Vector2 pos, SolarSystem system, Empire owner, int count = 1, string design = Warship)
    {
        var ships = new Ship[count];
        for (int i = 0; i < count; ++i)
        {
            ships[i] = SpawnShip(design, owner, pos + new Vector2(0f, 1000f * i));
            ships[i].SetSystem(system);
            Enemy.Threats.SetSeen(ships[i], fromBackgroundThread: false);
        }
        Enemy.Threats.Update(new FixedSimTime(2f));
        return ships;
    }

    Ship[] SeenBeside(Planet p, Empire owner, int count = 1, string design = Warship)
        => SeenAt(p.Position + new Vector2(5000f, 0f), p.System, owner, count, design);

    Vector2 Rally => Target.System.Position + new Vector2(Target.System.Radius * 2f, 0f);

    Fleet AttackFleet(int warships, Vector2 at, out MilitaryTask task, MilitaryTask.TaskType type = MilitaryTask.TaskType.AssaultPlanet,
                      string design = Warship)
    {
        var fleet = new Fleet(UState, Enemy);
        task = new MilitaryTask(Target, Enemy, fleet, MilitaryTaskImportance.Normal) { Type = type };
        fleet.FleetTask = task;
        fleet.AddShip(SpawnShip(Enemy.data.DefaultTroopShip, Enemy, at));
        for (int i = 0; i < warships; ++i)
            fleet.AddShip(SpawnShip(design, Enemy, at + new Vector2(0f, 1000f * (i + 1))));
        fleet.TaskStep = 5;
        return fleet;
    }

    bool CanBeatEveryoneThere(Fleet fleet)
        => Fleet.CanTakeThisFight(Enemy, fleet.GetStrength(), Enemy.Threats.GetHostileStrengthAt(Target.Position, 100_000f), Player);

    static void Assault(Fleet fleet, MilitaryTask task) => Call(fleet, "DoAssaultPlanet", task);

    static void AssertHolds(Fleet fleet, MilitaryTask task, int step, string message)
    {
        Assert.AreEqual(task, fleet.FleetTask, message + ": the fleet keeps its task");
        Assert.AreEqual(step, fleet.TaskStep, message + ": and does not go in");
        Assert.IsFalse(task.WentIn, message + ": and is not ordered in");
    }

    static void AssertGoesIn(Fleet fleet, MilitaryTask task, string message)
    {
        Assert.AreEqual(task, fleet.FleetTask, message + ": the fleet keeps its task");
        Assert.IsTrue(task.WentIn, message + ": and is ordered in");
    }

    Planet NearbyTarget(Vector2 pos)
    {
        Planet p = AddDummyPlanetToEmpire(pos, Player);
        Target.System.FiveClosestSystems = UState.GetFiveClosestSystems(Target.System);
        return p;
    }

    [TestMethod]
    public void AHostileThirdEmpiresWarshipInTheSystemIsAThirdParty()
    {
        HostileThirdEmpire();
        SeenBeside(Target, Player);
        Assert.IsFalse(Enemy.HasThirdPartyIn(Target.System, Player), "the ships of the empire we attack are no third party");

        SeenBeside(Target, ThirdMajor);
        Assert.IsTrue(Enemy.HasThirdPartyIn(Target.System, Player, out bool allAtWar), "a hostile third empire's warship in the system is");
        Assert.IsFalse(allAtWar, "and we are not at war with it");
    }

    [TestMethod]
    public void AThirdEmpireAtPeaceIsNoThirdParty()
    {
        Assert.IsFalse(Enemy.IsEmpireHostile(ThirdMajor), "setup: at peace with the third empire");
        SeenBeside(Target, ThirdMajor);

        Assert.IsFalse(Enemy.HasThirdPartyIn(Target.System, Player), "an empire that would not fight us is no third party");
    }

    [TestMethod]
    public void AThirdPartyMustBeInsideTheSystem()
    {
        HostileThirdEmpire();
        SolarSystem system = Target.System;
        SeenAt(system.Position + new Vector2(system.Radius + 800f, 0f), null, ThirdMajor);
        Assert.IsFalse(Enemy.HasThirdPartyIn(system, Player), "ships just past the system's edge, like a fleet waiting there, are outside it");

        SeenAt(system.Position + new Vector2(system.Radius - 800f, 0f), system, ThirdMajor);
        Assert.IsTrue(Enemy.HasThirdPartyIn(system, Player), "ships just inside the edge are in it");
    }

    [TestMethod]
    public void TheRemnantsAreNoThirdParty()
    {
        CreateAMinorFaction("Remnant");
        Assert.IsTrue(Faction.WeAreRemnants && Enemy.IsEmpireHostile(Faction), "setup: hostile Remnants");
        SeenBeside(Target, Faction);

        Assert.IsFalse(Enemy.HasThirdPartyIn(Target.System, Player), "the Remnants are left to their own rule");
    }

    [TestMethod]
    public void PiratesAreNoThirdParty()
    {
        CreateAMinorFaction("Corsairs");
        Assert.IsTrue(Faction.WeArePirates && Enemy.IsEmpireHostile(Faction), "setup: hostile pirates");
        SeenBeside(Target, Faction);

        Assert.IsFalse(Enemy.HasThirdPartyIn(Target.System, Player), "a pirate base in the system does not keep a war away from it");
    }

    [TestMethod]
    public void AResearchStationIsNoThirdParty()
    {
        AtWarWithThirdEmpire();
        Ship station = SeenBeside(Target, ThirdMajor, design: ResearchStation)[0];
        Assert.IsTrue(station.IsResearchStation && station.GetStrength() > 0f, "setup: an armed research station");
        Assert.IsFalse(Enemy.HasThirdPartyIn(Target.System, Player), "a research station is no third party");

        SeenBeside(Target, ThirdMajor);
        Assert.IsTrue(Enemy.HasThirdPartyIn(Target.System, Player, out bool allAtWar), "a warship beside it is");
        Assert.IsTrue(allAtWar, "and we are at war with its empire");
    }

    Planet WarPicks(out GoalStep step)
    {
        var war = Enemy.GetRelations(Player).ActiveWar;
        Assert.IsNotNull(war, "setup: the enemy is at war with the player");
        war.ChangeWarType(WarType.ImperialistWar);
        var manager = new WarManager(Enemy, Player, WarType.ImperialistWar);
        step = (GoalStep)Call(manager, "SelectTargetSystems");
        Goal[] missions = Enemy.AI.FindGoals(g => g is WarMission);
        return missions.Length > 0 ? missions[0].TargetPlanet : null;
    }

    [TestMethod]
    public void TheWarsPickPassesOverASystemWithAThirdParty()
    {
        Planet other = AddDummyPlanetToEmpire(new Vector2(-600_000), Player);
        Enemy.WeightedCenter = Vector2.Zero;
        Assert.IsTrue(Enemy.GetPotentialTargetPlanets(Player, WarType.ImperialistWar, out Planet[] targets), "setup: there are targets");
        Planet first = Enemy.SortPlanetTargets(targets, WarType.ImperialistWar, Player)[0];
        Planet second = first == Target ? other : Target;
        HostileThirdEmpire();
        SeenBeside(first, ThirdMajor);

        Assert.AreEqual(second, WarPicks(out _), "the war picks the planet in a system without a third party over the one it would have picked");
    }

    [TestMethod]
    public void WithOnlyATargetWithAThirdPartyLeftTheWarWaits()
    {
        HostileThirdEmpire();
        SeenBeside(Target, ThirdMajor);

        Assert.IsNull(WarPicks(out GoalStep step), "no mission goes to the only target, where a third party is");
        Assert.AreEqual(GoalStep.GoToNextStep, step, "the war goes on to wait and pick again");
        Assert.IsTrue(Enemy.GetPotentialTargetPlanets(Player, WarType.ImperialistWar, out Planet[] targets), "the war still has targets");
        CollectionAssert.Contains(targets, Target, "the planet is still one of them, so the war is not seen as out of targets");
    }

    [TestMethod]
    public void AFleetAboutToGoInWaitsOutsideWhileAHostileThirdEmpireIsThere()
    {
        Fleet control = AttackFleet(6, Rally, out MilitaryTask controlTask);
        Assault(control, controlTask);
        AssertGoesIn(control, controlTask, "control: with no third party");

        HostileThirdEmpire();
        SeenBeside(Target, ThirdMajor);
        Fleet fleet = AttackFleet(6, Rally, out MilitaryTask task);
        Assert.IsTrue(CanBeatEveryoneThere(fleet), "setup: the fleet could beat everyone there");

        Assault(fleet, task);
        AssertHolds(fleet, task, 5, "not at war with the third empire, the fleet waits however strong it is");
    }

    [TestMethod]
    public void AtWarWithTheThirdEmpireAFleetStrongEnoughForEveryoneGoesIn()
    {
        AtWarWithThirdEmpire();
        SeenBeside(Target, ThirdMajor);
        Fleet fleet = AttackFleet(6, Rally, out MilitaryTask task);
        Assert.IsTrue(CanBeatEveryoneThere(fleet), "setup: the fleet can beat everyone there");

        Assault(fleet, task);
        AssertGoesIn(fleet, task, "at war with the third empire and strong enough");
    }

    [TestMethod]
    public void AtWarWithTheThirdEmpireAFleetTooWeakForEveryoneWaits()
    {
        AtWarWithThirdEmpire();
        SeenBeside(Target, ThirdMajor, count: 4);
        Fleet fleet = AttackFleet(1, Rally, out MilitaryTask task);
        Assert.IsFalse(CanBeatEveryoneThere(fleet), "setup: the fleet cannot beat everyone there");

        Assault(fleet, task);
        AssertHolds(fleet, task, 5, "at war but too weak for everyone");
    }

    [TestMethod]
    public void AfterTwoMinutesAFleetStillFacingTheThirdEmpireMovesOnToAnotherSystem()
    {
        Planet next = NearbyTarget(new Vector2(400_000, 900_000));
        HostileThirdEmpire();
        SeenBeside(Target, ThirdMajor);
        Fleet fleet = AttackFleet(6, Rally, out MilitaryTask task);

        Assault(fleet, task);
        UState.StarDate += 2.3f;
        Assault(fleet, task);
        AssertHolds(fleet, task, 5, "short of two minutes, at five seconds a turn");
        Assert.AreEqual(Target, task.TargetPlanet, "and keeps its target");

        UState.StarDate += 0.2f;
        Assault(fleet, task);
        Assert.AreEqual(task, fleet.FleetTask, "after two minutes the fleet keeps its task");
        Assert.AreEqual(next, task.TargetPlanet, "and turns to the enemy's planet in a nearby system");
        Assert.AreEqual(next.Position, task.AO, "with its area of operation");
        Assert.AreEqual(2, fleet.TaskStep, "and heads there");
    }

    [TestMethod]
    public void TheNewTargetIsInASystemWithoutAThirdParty()
    {
        Planet closer = NearbyTarget(new Vector2(400_000, 800_000));
        Planet farther = NearbyTarget(new Vector2(400_000, -200_000));
        HostileThirdEmpire();
        SeenBeside(Target, ThirdMajor);
        SeenBeside(closer, ThirdMajor);
        SeenBeside(farther, Player, count: 4);
        Assert.IsTrue(Enemy.KnownEnemyStrengthIn(closer.System) < Enemy.KnownEnemyStrengthIn(farther.System),
            "setup: by known enemy strength alone the closer system would be picked");
        Fleet fleet = AttackFleet(6, Rally, out MilitaryTask task);

        Assault(fleet, task);
        UState.StarDate += 2.5f;
        Assault(fleet, task);

        Assert.AreEqual(farther, task.TargetPlanet, "the fleet passes over a nearby system where the third empire is too");
    }

    [TestMethod]
    public void TheNewTargetIsInTheSystemWithTheLeastKnownEnemyStrength()
    {
        Planet closer = NearbyTarget(new Vector2(400_000, 800_000));
        Planet farther = NearbyTarget(new Vector2(400_000, -200_000));
        SeenBeside(closer, Player, count: 4);
        HostileThirdEmpire();
        SeenBeside(Target, ThirdMajor);
        Fleet fleet = AttackFleet(6, Rally, out MilitaryTask task);

        Assault(fleet, task);
        UState.StarDate += 2.5f;
        Assault(fleet, task);

        Assert.AreEqual(farther, task.TargetPlanet, "of two nearby systems the fleet turns to the one with fewer known defenders");
    }

    [TestMethod]
    public void TheNewTargetIsTheSameEnemysPlanet()
    {
        Planet othersPlanet = AddDummyPlanetToEmpire(new Vector2(400_000, 800_000), ThirdMajor);
        Planet enemysPlanet = NearbyTarget(new Vector2(400_000, -200_000));
        HostileThirdEmpire();
        SeenBeside(Target, ThirdMajor);
        Fleet fleet = AttackFleet(6, Rally, out MilitaryTask task);
        CollectionAssert.Contains(Target.System.FiveClosestSystems, othersPlanet.System, "setup: the third empire's planet is nearby");

        Assault(fleet, task);
        UState.StarDate += 2.5f;
        Assault(fleet, task);

        Assert.AreEqual(enemysPlanet, task.TargetPlanet, "the fleet turns to a planet of the empire it is at war with, not to another empire's");
    }

    Ship LoadedTroopShip(Vector2 at)
    {
        Ship ship = SpawnShip(Enemy.data.DefaultTroopShip, Enemy, at);
        ResourceManager.CreateTroop("Wyvern", Enemy).LandOnShip(ship);
        return ship;
    }

    // troop ships with their troops aboard, waiting at home for a fleet
    Ship[] SpareTroopShips(int count)
    {
        var spare = new Ship[count];
        for (int i = 0; i < count; ++i)
            spare[i] = LoadedTroopShip(new Vector2(-800_000f, 1000f * i));
        var pool = new Array<Ship>();
        pool.AddRange(spare);
        Enemy.ShipsReadyForFleet = new FleetShips(Enemy, pool);
        return spare;
    }

    void TroopsAboard(Fleet fleet, int ships)
    {
        for (int i = 0; i < ships; ++i)
            fleet.AddShip(LoadedTroopShip(Rally - new Vector2(1000f * (i + 1), 0f)));
    }

    static float TroopStrengthOf(Fleet fleet)
    {
        float strength = 0f;
        foreach (Ship s in fleet.Ships)
            strength += s.Carrier.PlanetAssaultStrength;
        return strength;
    }

    // ships join their empire's ship list on the next game update, which these tests do not run; free troop ships are called up from it
    void OwnedByTheEnemy(params Ship[] ships)
    {
        foreach (Ship s in ships)
            Enemy.EmpireShips.Add(s);
        Enemy.EmpireShips.UpdatePublicLists();
        Assert.AreEqual(ships.Length, Enemy.OwnedShips.Count, "setup: the free troop ships are on the empire's list");
    }

    static int JoinedFleet(Ship[] ships, Fleet fleet)
    {
        int joined = 0;
        foreach (Ship s in ships)
            if (s.Fleet == fleet)
                ++joined;
        return joined;
    }

    // holds outside a target with a third party for two minutes, then turns to the next
    void WaitAndTurn(Fleet fleet, MilitaryTask task)
    {
        Assault(fleet, task);
        UState.StarDate += 2.5f;
        Assault(fleet, task);
    }

    [TestMethod]
    public void AFleetTurningToANewTargetTakesMoreTroopsWhenItsEmpireHasThem()
    {
        Planet next = NearbyTarget(new Vector2(400_000, 900_000));
        HostileThirdEmpire();
        SeenBeside(Target, ThirdMajor);
        Fleet fleet = AttackFleet(6, Rally, out MilitaryTask task);
        Ship[] spare = SpareTroopShips(10);
        Assert.IsTrue(Enemy.ShipsReadyForFleet.InvasionTroopStrength > 60f, "setup: troops enough for a planet with no defenders wait at home");

        WaitAndTurn(fleet, task);

        Assert.AreEqual(next, task.TargetPlanet, "setup: the fleet turns to another planet");
        Assert.IsTrue(JoinedFleet(spare, fleet) > 0, "and takes troops waiting at home for it, having none aboard");
    }

    [TestMethod]
    public void AFleetWhoseOwnTroopsCoverTheNewPlanetTakesNoMore()
    {
        Planet next = NearbyTarget(new Vector2(400_000, 900_000));
        HostileThirdEmpire();
        SeenBeside(Target, ThirdMajor);
        Fleet fleet = AttackFleet(6, Rally, out MilitaryTask task);
        TroopsAboard(fleet, 10);
        Ship[] spare = SpareTroopShips(10);

        WaitAndTurn(fleet, task);

        Assert.AreEqual(next, task.TargetPlanet, "setup: the fleet turns to another planet");
        Assert.IsTrue(TroopStrengthOf(fleet) >= task.NeededTroopStrength, "setup: its own troops cover the new planet's need");
        Assert.AreEqual(0, JoinedFleet(spare, fleet), "a fleet carrying troops enough takes no more from home");
    }

    [TestMethod]
    public void AFleetShortOfTroopsTakesFewerThanOneWithNoneAboard()
    {
        NearbyTarget(new Vector2(400_000, 900_000));
        HostileThirdEmpire();
        SeenBeside(Target, ThirdMajor);
        Fleet empty = AttackFleet(6, Rally, out MilitaryTask emptyTask);
        Ship[] firstPool = SpareTroopShips(10);
        WaitAndTurn(empty, emptyTask);
        int takenWithNoneAboard = JoinedFleet(firstPool, empty);

        Fleet partly = AttackFleet(6, Rally, out MilitaryTask task);
        TroopsAboard(partly, 2);
        Ship[] secondPool = SpareTroopShips(10);
        WaitAndTurn(partly, task);
        int taken = JoinedFleet(secondPool, partly);

        Assert.IsTrue(taken > 0, "setup: a fleet with two troop ships aboard is short and takes troops");
        Assert.IsTrue(taken < takenWithNoneAboard, $"it takes fewer ({taken}) than a fleet with none aboard ({takenWithNoneAboard})");
    }

    [TestMethod]
    public void AFleetsOwnTroopsCountTowardTheNewPlanetsNeed()
    {
        NearbyTarget(new Vector2(400_000, 900_000));
        HostileThirdEmpire();
        SeenBeside(Target, ThirdMajor);
        Fleet probe = AttackFleet(6, Rally, out MilitaryTask probeTask);
        SpareTroopShips(0);
        WaitAndTurn(probe, probeTask);
        int need = probeTask.NeededTroopStrength;
        Ship idle = LoadedTroopShip(new Vector2(-900_000f, 0f)); // a free troop ship outside the pool, which the call-up for more could take
        float perShip = idle.Carrier.PlanetAssaultStrength;
        int aboard = (int)System.Math.Ceiling(need / perShip) - 1;
        Assert.IsTrue(aboard >= 1 && perShip < need, $"setup: the new planet needs {need}, more than one troop ship's {perShip}");

        Fleet fleet = AttackFleet(6, Rally, out MilitaryTask task);
        TroopsAboard(fleet, aboard);
        Ship[] spare = SpareTroopShips(1);
        OwnedByTheEnemy(idle);
        WaitAndTurn(fleet, task);

        Assert.AreEqual(1, JoinedFleet(spare, fleet),
            "one troop ship at home makes up what the fleet's own troops lack, though alone it is far short of the planet's need");
        Assert.AreNotEqual(fleet, idle.Fleet, "and no free troop ship is called up beyond that");
    }

    [TestMethod]
    public void TroopsCalledUpToMakeUpAShortfallCountEachShipOnce()
    {
        Planet next = NearbyTarget(new Vector2(400_000, 900_000));
        HostileThirdEmpire();
        SeenBeside(Target, ThirdMajor);
        Fleet probe = AttackFleet(6, Rally, out MilitaryTask probeTask);
        SpareTroopShips(0);
        WaitAndTurn(probe, probeTask);
        int need = probeTask.NeededTroopStrength;

        Ship full = LoadedTroopShip(new Vector2(-800_000f, 0f));
        float perShip = full.Carrier.PlanetAssaultStrength;
        Fleet fleet = AttackFleet(6, Rally, out MilitaryTask task);
        TroopsAboard(fleet, (int)((need - 2) / perShip));
        int lacking = need - (int)TroopStrengthOf(fleet);
        Assert.IsTrue(lacking >= 2 && lacking < Enemy.GetTypicalTroopStrength(),
            $"setup: the fleet lacks {lacking} of {need}, less than a typical troop, so one troop ship is taken from home");

        Ship weakened = SpawnShip(Enemy.data.DefaultTroopShip, Enemy, next.Position + new Vector2(5000f, 0f));
        Troop hurt = ResourceManager.CreateTroop("Wyvern", Enemy);
        hurt.LandOnShip(weakened);
        hurt.Strength = lacking - 1; // a troop worn down in battle, one short of what the fleet lacks
        var pool = new Array<Ship>();
        pool.Add(weakened);
        pool.Add(full);
        Enemy.ShipsReadyForFleet = new FleetShips(Enemy, pool);
        OwnedByTheEnemy(weakened, full);

        WaitAndTurn(fleet, task);

        Assert.AreEqual(next, task.TargetPlanet, "setup: the fleet turns to the next planet");
        Assert.AreEqual(fleet, weakened.Fleet, "setup: the weakened troop ship is taken first");
        Assert.IsTrue(TroopStrengthOf(fleet) >= task.NeededTroopStrength,
            $"the free troop ship called up to make up the rest is another one, not the weakened ship counted twice (has {TroopStrengthOf(fleet)} of {task.NeededTroopStrength})");
    }

    [TestMethod]
    public void AGlassingFleetTurningToANewTargetTakesNoTroops()
    {
        Planet next = NearbyTarget(new Vector2(400_000, 900_000));
        HostileThirdEmpire();
        SeenBeside(Target, ThirdMajor);
        Fleet fleet = AttackFleet(4, Rally, out MilitaryTask task, MilitaryTask.TaskType.GlassPlanet, Bomber);
        fleet.TaskStep = 4;
        Ship[] spare = SpareTroopShips(10);

        Call(fleet, "DoGlassPlanet", task);
        UState.StarDate += 2.5f;
        Call(fleet, "DoGlassPlanet", task);

        Assert.AreEqual(next, task.TargetPlanet, "setup: the glassing fleet turns to another planet");
        Assert.AreEqual(0, JoinedFleet(spare, fleet), "a fleet that bombs its target takes no troops along");
    }

    [TestMethod]
    public void WithNoOtherTargetNearbyTheFleetGivesUp()
    {
        HostileThirdEmpire();
        SeenBeside(Target, ThirdMajor);
        Fleet fleet = AttackFleet(6, Rally, out MilitaryTask task);

        Assault(fleet, task);
        UState.StarDate += 2.5f;
        Assault(fleet, task);

        Assert.IsNull(fleet.FleetTask, "with nowhere else to go the fleet ends its task");
    }

    [TestMethod]
    public void AReclaimFleetGivesUpRatherThanTurningElsewhere()
    {
        NearbyTarget(new Vector2(400_000, 900_000));
        HostileThirdEmpire();
        SeenBeside(Target, ThirdMajor);
        Fleet fleet = AttackFleet(6, Rally, out MilitaryTask task, MilitaryTask.TaskType.ReclaimPlanet);

        Assault(fleet, task);
        UState.StarDate += 2.5f;
        Assault(fleet, task);

        Assert.IsNull(fleet.FleetTask, "a fleet reclaiming its planet does not turn to another one");
    }

    void ThirdPartyLeaves(Ship[] thirdParty, Ship[] stillSeen)
    {
        foreach (Ship s in thirdParty)
            s.Die(null, cleanupOnly: true);
        SpawnShip(Warship, Enemy, thirdParty[0].Position); // a ship of ours sees the space they left
        foreach (Ship s in stillSeen)
            Enemy.Threats.SetSeen(s, fromBackgroundThread: false);
        Enemy.Threats.Update(new FixedSimTime(2f));
        Assert.IsFalse(Enemy.HasThirdPartyIn(Target.System, Player), "setup: the third party is gone");
    }

    [TestMethod]
    public void OnceTheThirdEmpireIsGoneTheFleetGoesIn()
    {
        HostileThirdEmpire();
        Ship[] thirdParty = SeenBeside(Target, ThirdMajor);
        Fleet fleet = AttackFleet(6, Rally, out MilitaryTask task);
        Assault(fleet, task);
        AssertHolds(fleet, task, 5, "setup: the fleet waits");

        ThirdPartyLeaves(thirdParty, new Ship[0]);
        Assault(fleet, task);

        AssertGoesIn(fleet, task, "the third empire gone, the fleet goes in");
        Assert.AreEqual(0f, task.ThirdPartyHoldEndsAt, "and its wait is over");
    }

    [TestMethod]
    public void OnceTheThirdEmpireIsGoneAFleetTooWeakForWhatIsLeftGivesUp()
    {
        Ship[] defenders = SeenAt(Target.Position - new Vector2(30_000f, 0f), Target.System, Player, count: 4);
        HostileThirdEmpire();
        Ship[] thirdParty = SeenBeside(Target, ThirdMajor);
        Fleet fleet = AttackFleet(1, Rally, out MilitaryTask task);
        Assault(fleet, task);
        AssertHolds(fleet, task, 5, "setup: the fleet waits");

        ThirdPartyLeaves(thirdParty, defenders);
        Assert.IsFalse(CanBeatEveryoneThere(fleet), "setup: the planet's own defenders are too strong");
        Assault(fleet, task);

        Assert.IsFalse(task.WentIn, "after its wait the fleet goes in only if it can beat what is left");
        Assert.IsNull(fleet.FleetTask, "and ends its task when it cannot");
    }

    [TestMethod]
    public void WithoutAThirdPartyAFleetAtTheAttackStepGoesInAsBefore()
    {
        SeenAt(Target.Position - new Vector2(30_000f, 0f), Target.System, Player, count: 4);
        Fleet fleet = AttackFleet(1, Rally, out MilitaryTask task);
        Assert.IsFalse(CanBeatEveryoneThere(fleet), "setup: the planet's defenders outmatch the fleet");

        Assault(fleet, task);

        AssertGoesIn(fleet, task, "with no third party the step that sends the fleet in is unchanged");
    }

    [TestMethod]
    public void AFleetThatWentInStaysInItsFight()
    {
        HostileThirdEmpire();
        SeenBeside(Target, ThirdMajor);
        Fleet fleet = AttackFleet(6, Rally, out MilitaryTask task);
        task.WentIn = true;

        Assault(fleet, task);

        Assert.AreEqual(task, fleet.FleetTask, "a fleet already ordered in keeps its task");
        Assert.AreEqual(6, fleet.TaskStep, "and goes on with its attack when a third party shows up");
        Assert.AreEqual(0f, task.ThirdPartyHoldEndsAt, "without a wait");
    }

    [TestMethod]
    public void AFleetAlreadyInTheTargetSystemDoesNotTurnBack()
    {
        HostileThirdEmpire();
        SeenBeside(Target, ThirdMajor);
        Fleet fleet = AttackFleet(6, Target.Position + new Vector2(20_000f, 0f), out MilitaryTask task);

        Assault(fleet, task);

        AssertGoesIn(fleet, task, "a fleet inside the system goes on with its attack");
    }

    [TestMethod]
    public void AFleetThatTurnedToAnotherPlanetInItsSystemStaysInItsFight()
    {
        HostileThirdEmpire();
        SeenBeside(Target, ThirdMajor);
        Fleet fleet = AttackFleet(6, Target.Position + new Vector2(20_000f, 0f), out MilitaryTask task);
        fleet.TaskStep = 6; // as after an invasion moves on to a planet it can invade at once, which clears its record

        Assault(fleet, task);
        Assert.IsTrue(task.WentIn, "a fleet attacking inside the system is in its fight");

        foreach (Ship s in fleet.Ships)
            s.Position = Rally;
        Assault(fleet, task);
        Assert.AreEqual(0f, task.ThirdPartyHoldEndsAt, "and does not wait outside when the fight takes it past the system's edge");
    }

    [TestMethod]
    public void AFleetSentInWhenItsRearShipsComeUnderFireStaysInItsFight()
    {
        HostileThirdEmpire();
        Ship hostile = SeenBeside(Target, ThirdMajor)[0];
        Fleet fleet = AttackFleet(6, Rally, out MilitaryTask task);
        fleet.AutoArrange();
        Ship troops = fleet.Ships.Find(s => s.IsTroopShip);
        troops.Position = Target.Position + new Vector2(10_000f, 0f);
        hostile.AI.Target = troops;
        troops.AI.PotentialTargets = new[] { hostile };
        fleet.TaskStep = 3;

        Assault(fleet, task);
        Assert.AreEqual(6, fleet.TaskStep, "setup: a rear ship under fire near the planet sends the fleet in");

        Assault(fleet, task);
        Assert.AreEqual(0f, task.ThirdPartyHoldEndsAt, "a fleet sent in that way does not then wait outside");
    }

    [TestMethod]
    public void AScatteredFleetWithAThirdPartyAheadGathersAgainOutsideTheSystem()
    {
        Vector2 farFromItsShips = Rally + new Vector2(0f, 200_000f);
        Fleet control = AttackFleet(6, Rally, out MilitaryTask controlTask);
        control.TaskStep = 4;
        control.FinalPosition = farFromItsShips;
        Assault(control, controlTask);
        Assert.AreEqual(3, control.TaskStep, "setup: a scattered fleet gathers again");
        Assert.IsTrue(control.FinalPosition.InRadius(Target.System.Position, Target.System.Radius),
            "control: with no third party it gathers again inside the system, near the planet");

        HostileThirdEmpire();
        SeenBeside(Target, ThirdMajor);
        Fleet fleet = AttackFleet(6, Rally, out MilitaryTask task);
        fleet.TaskStep = 4;
        fleet.FinalPosition = farFromItsShips;

        Assault(fleet, task);
        Assert.AreEqual(3, fleet.TaskStep, "setup: the scattered fleet gathers again");
        Assert.IsFalse(fleet.FinalPosition.InRadius(Target.System.Position, Target.System.Radius),
            "with a third party ahead it gathers outside the system, where it waits");
    }

    [TestMethod]
    public void AStrikeFromAStagedFleetWaitsToo()
    {
        HostileThirdEmpire();
        SeenBeside(Target, ThirdMajor);
        Fleet fleet = AttackFleet(6, Rally, out MilitaryTask task, MilitaryTask.TaskType.StrikeForce);
        fleet.TaskStep = 6;

        Assault(fleet, task);
        AssertHolds(fleet, task, 6, "a strike made from a fleet staged outside the system starts at the attack step");

        task.WentIn = true;
        Assault(fleet, task);
        Assert.AreEqual(5, fleet.TaskStep, "control: a fleet that went in goes on through its attack steps");
    }

    [TestMethod]
    public void ATravelingFleetIsNotCalledOffByAThirdPartyItWillWaitOutAtTheRally()
    {
        HostileThirdEmpire();
        SeenBeside(Target, ThirdMajor, count: 4);
        Fleet fleet = AttackFleet(1, Rally, out MilitaryTask task);
        fleet.TaskStep = 3;
        Assert.IsFalse(CanBeatEveryoneThere(fleet), "setup: the fleet cannot beat everyone there");

        Assault(fleet, task);

        Assert.AreEqual(task, fleet.FleetTask, "a fleet on its way keeps its task, for the wait before going in decides");
    }

    [TestMethod]
    public void ATravelingFleetIsStillCalledOffByTheEnemyItsTooWeakFor()
    {
        SeenBeside(Target, Player, count: 4);
        Fleet fleet = AttackFleet(1, Rally, out MilitaryTask task);
        fleet.TaskStep = 3;
        Assert.IsFalse(CanBeatEveryoneThere(fleet), "setup: the fleet cannot beat the planet's defenders");

        Assault(fleet, task);

        Assert.IsNull(fleet.FleetTask, "without a third party, a fleet too weak for its target ends its task as before");
    }

    [TestMethod]
    public void AGlassingFleetWaitsOutsideToo()
    {
        Fleet control = AttackFleet(4, Rally, out MilitaryTask controlTask, MilitaryTask.TaskType.GlassPlanet, Bomber);
        control.TaskStep = 4;
        Call(control, "DoGlassPlanet", controlTask);
        Assert.IsTrue(controlTask.WentIn, "control: with no third party the bombers go in");

        HostileThirdEmpire();
        SeenBeside(Target, ThirdMajor);
        Fleet fleet = AttackFleet(4, Rally, out MilitaryTask task, MilitaryTask.TaskType.GlassPlanet, Bomber);
        fleet.TaskStep = 4;

        Call(fleet, "DoGlassPlanet", task);
        AssertHolds(fleet, task, 4, "a glassing fleet waits outside like the others");
    }

    [TestMethod]
    public void AGlassingFleetThatWentInIsStillCalledOffWhenOutmatched()
    {
        HostileThirdEmpire();
        SeenBeside(Target, ThirdMajor, count: 4);
        Fleet fleet = AttackFleet(1, Rally, out MilitaryTask task, MilitaryTask.TaskType.GlassPlanet, Bomber);
        fleet.TaskStep = 5;
        task.WentIn = true;

        Call(fleet, "DoGlassPlanet", task);

        Assert.IsNull(fleet.FleetTask, "a glassing fleet that went in and is outmatched ends its task as before");
    }

    [TestMethod]
    public void ATravelingGlassingFleetIsNotCalledOffByAThirdParty()
    {
        HostileThirdEmpire();
        SeenBeside(Target, ThirdMajor, count: 4);
        Fleet fleet = AttackFleet(1, Rally, out MilitaryTask task, MilitaryTask.TaskType.GlassPlanet, Bomber);
        fleet.TaskStep = 3;
        Assert.IsFalse(CanBeatEveryoneThere(fleet), "setup: the fleet cannot beat everyone there");

        Call(fleet, "DoGlassPlanet", task);

        Assert.AreEqual(task, fleet.FleetTask, "a glassing fleet on its way keeps its task, for the wait before going in decides");
    }

    [TestMethod]
    public void ATravelingGlassingFleetIsStillCalledOffByTheEnemyItsTooWeakFor()
    {
        SeenBeside(Target, Player, count: 4);
        Fleet fleet = AttackFleet(1, Rally, out MilitaryTask task, MilitaryTask.TaskType.GlassPlanet, Bomber);
        fleet.TaskStep = 3;

        Call(fleet, "DoGlassPlanet", task);

        Assert.IsNull(fleet.FleetTask, "without a third party, a glassing fleet too weak for its target ends its task as before");
    }

    [TestMethod]
    public void ANewTargetStartsWithNoWaitAndNoOrderIn()
    {
        Planet other = AddDummyPlanetToEmpire(new Vector2(-400_000), Player);
        var task = new MilitaryTask(Target, Enemy, MilitaryTaskImportance.Normal) { WentIn = true, ThirdPartyHoldEndsAt = 1002f };

        task.SetTargetPlanet(other);

        Assert.IsFalse(task.WentIn, "the fleet has not gone in at its new target");
        Assert.AreEqual(0f, task.ThirdPartyHoldEndsAt, "nor started a wait there");
    }
}
