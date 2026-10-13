using System.Reflection;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Ship_Game;
using Ship_Game.AI;
using Ship_Game.AI.Tasks;
using Ship_Game.Fleets;
using Ship_Game.Gameplay;
using Ship_Game.Ships;
using Vector2 = SDGraphics.Vector2;

namespace UnitTests.Fleets;

[TestClass]
public class InvasionPlanningTests : StarDriveTest
{
    const string Warship = "Terran-Prototype";
    const string Platform = "Platform Base mk1-a";

    public InvasionPlanningTests()
    {
        LoadStarterShips(Warship, Platform);
        CreateUniverseAndPlayerEmpire();
        UState.Objects.EnableParallelUpdate = false;
        UState.Events.Disabled = true;
        Player.TotalScore = Enemy.TotalScore = 100;
        SetCanBuildBombers(Enemy, false);
        SetCanBuildBombers(Player, false);
    }

    static void SetCanBuildBombers(Empire empire, bool canBuild)
        => typeof(Empire).GetProperty(nameof(Empire.CanBuildBombers))!.SetValue(empire, canBuild);

    static void SetGroundGuns(Planet p, float offense)
        => typeof(Planet).GetProperty(nameof(Planet.BuildingGeodeticOffense))!.SetValue(p, offense);

    static object CallFleet(Fleet fleet, string method, params object[] args)
        => typeof(Fleet).GetMethod(method, BindingFlags.Instance | BindingFlags.NonPublic)!.Invoke(fleet, args);

    Fleet InvasionFleet(Planet target, out MilitaryTask task)
    {
        typeof(SolarSystemBody).GetProperty(nameof(SolarSystemBody.GravityWellRadius))!.SetValue(target, 10_000f);
        task = new MilitaryTask(target, Player, MilitaryTaskImportance.Normal);
        var fleet = new Fleet(UState, Player) { FleetTask = task };
        fleet.AddShip(SpawnShip(Warship, Player, target.Position + new Vector2(target.Radius + 1000f, 0f)));
        return fleet;
    }

    static bool TroopsHoldBack(Fleet fleet, MilitaryTask task, bool bombing)
        => (bool)CallFleet(fleet, "TroopsHoldBack", task, bombing);

    Ship OrbitalOf(Planet planet, Empire owner)
    {
        Ship platform = SpawnShip(Platform, owner, planet.Position + new Vector2(0f, planet.Radius + 2000f));
        platform.TetherToPlanet(planet);
        return platform;
    }

    Planet AddPlanetTo(SolarSystem system, Vector2 offset, Empire owner)
    {
        var p = new Planet(UState.CreateId(), system, system.Position + offset, 1f, 1f, 4f);
        system.PlanetList.Add(p);
        p.SetOwner(owner);
        return p;
    }

    [TestMethod]
    public void TroopsGiveTheBombersAFiveTurnLead()
    {
        Planet target = AddDummyPlanetToEmpire(new Vector2(400_000), Enemy);
        Fleet fleet = InvasionFleet(target, out MilitaryTask task);

        Assert.IsTrue(TroopsHoldBack(fleet, task, bombing: true), "troops hold back while the bombers start their run");
        UState.StarDate += 0.4f;
        Assert.IsTrue(TroopsHoldBack(fleet, task, bombing: true), "and for four turns after");
        Assert.IsFalse(TroopsHoldBack(fleet, task, bombing: false), "bombers that stop early let the troops go at once");
        UState.StarDate += 0.1f;
        Assert.IsFalse(TroopsHoldBack(fleet, task, bombing: true), "after five turns the troops follow the bombers in");
    }

    [TestMethod]
    public void ANewTargetGivesTheBombersANewLead()
    {
        Planet first = AddDummyPlanetToEmpire(new Vector2(400_000), Enemy);
        Planet second = AddDummyPlanetToEmpire(new Vector2(600_000), Enemy);
        Fleet fleet = InvasionFleet(first, out MilitaryTask task);
        TroopsHoldBack(fleet, task, bombing: true);
        UState.StarDate += 0.5f;
        Assert.IsFalse(TroopsHoldBack(fleet, task, bombing: true), "setup: the lead on the first target is over");

        task.SetTargetPlanet(second);

        Assert.IsTrue(TroopsHoldBack(fleet, task, bombing: true), "the bombers get a new lead on the next target");
    }

    [TestMethod]
    public void TroopsWaitUntilThePlanetsOrbitalsAreDestroyed()
    {
        Planet target = AddDummyPlanetToEmpire(new Vector2(400_000), Enemy);
        Fleet fleet = InvasionFleet(target, out MilitaryTask task);
        Ship platform = OrbitalOf(target, Enemy);

        Assert.IsTrue(TroopsHoldBack(fleet, task, bombing: false), "troops wait while the planet's platform still fights");
        platform.Die(null, cleanupOnly: true);
        Assert.IsFalse(TroopsHoldBack(fleet, task, bombing: false), "and go in once it is destroyed");
    }

    [TestMethod]
    public void AnotherEmpiresOrbitalDoesNotHoldTheTroopsBack()
    {
        CreateThirdMajorEmpire();
        Player.AI.DeclareWarOn(ThirdMajor, WarType.BorderConflict);
        Assert.IsTrue(Player.IsEmpireAttackable(ThirdMajor), "setup: we are at war with the third empire too");
        Planet target = AddDummyPlanetToEmpire(new Vector2(400_000), Enemy);
        Fleet fleet = InvasionFleet(target, out MilitaryTask task);
        OrbitalOf(target, ThirdMajor);

        Assert.IsFalse(TroopsHoldBack(fleet, task, bombing: false), "only the planet owner's orbitals hold the troops back");
    }

    [TestMethod]
    public void TroopsWaitUntilThePlanetsDefenseShipsAreDestroyed()
    {
        Planet target = AddDummyPlanetToEmpire(new Vector2(400_000), Enemy);
        Fleet fleet = InvasionFleet(target, out MilitaryTask task);
        Ship defender = SpawnShip(Warship, Enemy, target.Position + new Vector2(0f, target.Radius + 3000f));
        defender.UpdateHomePlanet(target);
        RunSimWhile((simTimeout: 5, fatal: true), () => !target.System.ShipList.Contains(defender));

        Assert.IsTrue(TroopsHoldBack(fleet, task, bombing: false), "troops wait while the planet's defense ship still fights");
        defender.Die(null, cleanupOnly: true);
        Assert.IsFalse(TroopsHoldBack(fleet, task, bombing: false), "and go in once it is destroyed");
    }

    [TestMethod]
    public void WhileTheTroopsHoldBackTheFleetStaysOnTheAssault()
    {
        Planet target = AddDummyPlanetToEmpire(new Vector2(400_000), Enemy);
        Fleet fleet = InvasionFleet(target, out MilitaryTask task);
        OrbitalOf(target, Enemy);

        var status = (Status)CallFleet(fleet, "StatusOfPlanetAssault", task);

        Assert.AreEqual(Status.Good, status,
            "a fleet holding its troops back keeps fighting at the planet instead of going back to forming up for the attack");
    }

    [TestMethod]
    public void WithoutBombersTheCapitalIsLeftOffTheTargetList()
    {
        Planet capital = AddHomeWorldToEmpire(new Vector2(400_000), Player);
        Planet colony = AddDummyPlanetToEmpire(new Vector2(600_000), Player);
        Assert.IsTrue(capital.HasCapital && !colony.HasCapital, "setup: one capital, one colony");

        Assert.IsTrue(Enemy.GetPotentialTargetPlanets(Player, WarType.ImperialistWar, out Planet[] targets), "setup: there are targets");
        CollectionAssert.Contains(targets, colony, "the colony is a target");
        CollectionAssert.DoesNotContain(targets, capital, "an empire that cannot build bombers leaves the capital off its list");

        SetCanBuildBombers(Enemy, true);
        Enemy.GetPotentialTargetPlanets(Player, WarType.ImperialistWar, out targets);
        CollectionAssert.Contains(targets, capital, "once it can build bombers the capital is a target again");
    }

    [TestMethod]
    public void WithoutBombersACapitalIsStillATargetWhenItIsAllThatIsLeft()
    {
        Planet capital = AddHomeWorldToEmpire(new Vector2(400_000), Player);

        Assert.IsTrue(Enemy.GetPotentialTargetPlanets(Player, WarType.ImperialistWar, out Planet[] targets),
            "a capital that is its empire's last planet is still a target");
        CollectionAssert.Contains(targets, capital);
    }

    [TestMethod]
    public void WithoutBombersGunnedPlanetsComeLast()
    {
        Enemy.WeightedCenter = Vector2.Zero;
        Planet gunned = AddDummyPlanetToEmpire(new Vector2(100_000), Player);
        Planet bare = AddDummyPlanetToEmpire(new Vector2(300_000), Player);
        SetGroundGuns(gunned, 50f);

        Planet[] sorted = Enemy.SortPlanetTargets(new[] { gunned, bare }, WarType.DefensiveWar, Player);
        Assert.AreEqual(bare, sorted[0], "without bombers a planet without ground guns comes before a nearer one with them");

        SetCanBuildBombers(Enemy, true);
        sorted = Enemy.SortPlanetTargets(new[] { gunned, bare }, WarType.DefensiveWar, Player);
        Assert.AreEqual(gunned, sorted[0], "with bombers the nearer planet comes first, guns or not");
    }

    [TestMethod]
    public void WithoutBombersAStrikeFleetMovesOnToAColonyRatherThanTheCapital()
    {
        Planet capital = AddHomeWorldToEmpire(new Vector2(400_000), Player);
        Planet colony = AddDummyPlanetToEmpire(new Vector2(700_000), Player);
        var fleet = new Fleet(UState, Enemy);

        var task = new MilitaryTask(capital, Enemy, MilitaryTaskImportance.Normal);
        var pick = (Planet)CallFleet(fleet, "TryGetNewTargetPlanetStrike", capital.System, Player, task);

        Assert.AreEqual(colony, pick, "a strike fleet without bombers passes the capital next to it for a colony further away");
    }

    [TestMethod]
    public void WithoutBombersAFleetDoesNotTurnOnTheCapitalBesideItsConquest()
    {
        Planet capital = AddHomeWorldToEmpire(new Vector2(400_000), Player);
        AddDummyPlanetToEmpire(new Vector2(700_000), Player);
        Planet conquered = AddPlanetTo(capital.System, new Vector2(30_000f, 0f), Enemy);
        var task = new MilitaryTask(conquered, Enemy, MilitaryTaskImportance.Normal);
        var fleet = new Fleet(UState, Enemy) { FleetTask = task };

        object[] args = { task, null };
        CallFleet(fleet, "TryGetNewTargetPlanet", args);
        Assert.AreNotEqual(capital, args[1], "without bombers the fleet does not turn on the capital in the system it just took");

        SetCanBuildBombers(Enemy, true);
        args = new object[] { task, null };
        CallFleet(fleet, "TryGetNewTargetPlanet", args);
        Assert.AreEqual(capital, args[1], "with bombers it goes for the capital as before");
    }

    static Planet NextTarget(Fleet fleet, MilitaryTask task)
    {
        object[] args = { task, null };
        CallFleet(fleet, "TryGetNewTargetPlanet", args);
        return (Planet)args[1];
    }

    Planet NextTargetAfterAWave(Planet target, bool sent, bool landed)
    {
        var task = new MilitaryTask(target, Enemy, MilitaryTaskImportance.Normal) { TroopsSent = sent, TroopsLanded = landed };
        return NextTarget(new Fleet(UState, Enemy) { FleetTask = task }, task);
    }

    [TestMethod]
    public void AFleetDoesNotGoBackToAPlanetThatBeatAWave()
    {
        Planet first = AddDummyPlanetToEmpire(new Vector2(400_000), Player);
        Planet second = AddPlanetTo(first.System, new Vector2(30_000f, 0f), Player);
        var task = new MilitaryTask(first, Enemy, MilitaryTaskImportance.Normal) { TroopsSent = true };
        var fleet = new Fleet(UState, Enemy) { FleetTask = task };
        Assert.AreEqual(second, NextTarget(fleet, task), "setup: beaten at the first planet, the fleet moves on to the second");

        task.SetTargetPlanet(second);
        task.TroopsSent = true;

        Assert.IsNull(NextTarget(fleet, task), "beaten at the second too, it does not go back to the first");
    }

    [TestMethod]
    public void AStrikeFleetWhoseWaveNeverLandedMovesOnToThePlanetBeyond()
    {
        Planet target = AddDummyPlanetToEmpire(new Vector2(400_000), Player);
        Planet beyond = AddDummyPlanetToEmpire(new Vector2(700_000), Player);
        var task = new MilitaryTask(target, Enemy, MilitaryTaskImportance.Normal) { TroopsSent = true };
        task.Type = MilitaryTask.TaskType.StrikeForce;

        Assert.AreEqual(beyond, NextTarget(new Fleet(UState, Enemy) { FleetTask = task }, task),
            "a strike fleet passes over the closest planet when that is the one that beat its wave");
    }

    [TestMethod]
    public void APlanetTakenAfterAWaveThatSeemedNotToLandIsStillHeld()
    {
        Planet target = AddDummyPlanetToEmpire(new Vector2(400_000), Enemy);
        var fleet = new Fleet(UState, Enemy);
        var task = new MilitaryTask(target, Enemy, fleet, MilitaryTaskImportance.Normal) { TroopsSent = true };
        fleet.FleetTask = task;
        fleet.AddShip(SpawnShip(Warship, Enemy, target.Position + new Vector2(target.Radius + 5000f, 0f)));
        fleet.TaskStep = 8;

        CallFleet(fleet, "DoAssaultPlanet", task);

        Assert.IsNotNull(fleet.FleetTask, "a fleet whose planet is ours stays to guard it, whatever its records say");
    }

    [TestMethod]
    public void APlanetTakenMeanwhileIsStillHeldWhenTheNextTargetFindsNoTroops()
    {
        Planet taken = AddDummyPlanetToEmpire(new Vector2(400_000), Enemy);
        Planet next = AddPlanetTo(taken.System, new Vector2(30_000f, 0f), Player);
        Enemy.ShipsReadyForFleet = new FleetShips(Enemy, Enemy.AllFleetReadyShips());
        var fleet = new Fleet(UState, Enemy);
        var task = new MilitaryTask(taken, Enemy, fleet, MilitaryTaskImportance.Normal) { TroopsSent = true };
        fleet.FleetTask = task;
        fleet.AddShip(SpawnShip(Warship, Enemy, taken.Position + new Vector2(taken.Radius + 5000f, 0f)));
        fleet.TaskStep = 8;
        Assert.IsFalse(new MilitaryTask(next, Enemy, MilitaryTaskImportance.Normal).GetMoreTroops(next, out _),
            "setup: there are no troops for the next planet");

        CallFleet(fleet, "DoAssaultPlanet", task);

        Assert.AreEqual(taken, task.TargetPlanet, "the task keeps the planet it fought for when it finds no troops for the next one");
        Assert.IsNotNull(fleet.FleetTask, "and the fleet stays to guard it");
    }

    [TestMethod]
    public void AssaultShuttlesFlyingInAreAWaveStillOnItsWay()
    {
        const string troopCarrier = "Neutron Troopship Mk2";
        LoadStarterShips(troopCarrier, Player.data.DefaultAssaultShuttle);
        Player.GetRelations(Enemy).TurnsAtWar = 5;
        Planet target = AddHomeWorldToEmpire(new Vector2(400_000), Enemy);
        var task = new MilitaryTask(target, Player, MilitaryTaskImportance.Normal);
        var fleet = new Fleet(UState, Player) { FleetTask = task };
        Ship carrier = SpawnShip(troopCarrier, Player, target.Position + new Vector2(target.Radius + 3000f, 0f));
        carrier.KillAllTroops();
        ShipModule[] bays = carrier.Carrier.AllTroopBays;
        for (int i = 0; i < bays.Length; ++i)
        {
            bays[i].HangarTimer = 0f;
            ResourceManager.CreateTroop("Wyvern", Player).LandOnShip(carrier);
        }
        fleet.AddShip(carrier);

        carrier.Carrier.AssaultPlanet(target);
        Assert.IsTrue(bays[0].TryGetHangarShipActive(out Ship shuttle) && shuttle.HasOurTroops && !shuttle.IsLanding,
            "setup: a shuttle carries troops toward the planet");
        Assert.IsFalse(carrier.HasOurTroops, "setup: the carrier has launched all its troops");

        bool invading = (bool)CallFleet(fleet, "OrderShipsToInvade", new[] { carrier }, task, false);
        Assert.IsTrue(invading, "troops still flying in keep the invasion going, so the wave is not judged lost before it lands");

        Planet elsewhere = AddDummyPlanetToEmpire(new Vector2(700_000), Enemy);
        var otherTask = new MilitaryTask(elsewhere, Player, MilitaryTaskImportance.Normal);
        bool invadingElsewhere = (bool)CallFleet(fleet, "OrderShipsToInvade", new[] { carrier }, otherTask, false);
        Assert.IsFalse(invadingElsewhere, "control: shuttles flying to another planet are no wave on this one");
    }

    [TestMethod]
    public void TroopsReachingTheGroundAreRecordedAsTheyLand()
    {
        LoadStarterShips(Player.data.DefaultTroopShip);
        Planet target = AddHomeWorldToEmpire(new Vector2(400_000), Enemy);
        var task = new MilitaryTask(target, Player, MilitaryTaskImportance.Normal);
        var fleet = new Fleet(UState, Player) { FleetTask = task };
        Ship troopShip = SpawnShip(Player.data.DefaultTroopShip, Player, target.Position + new Vector2(target.Radius + 3000f, 0f));
        ResourceManager.CreateTroop("Wyvern", Player).LandOnShip(troopShip);
        fleet.AddShip(troopShip);

        AssertEqual(1, troopShip.LandTroopsOnPlanet(target), "setup: the troop lands");

        Assert.IsTrue(task.TroopsLanded, "the task learns of the landing as it happens, not at the fleet's next check");
    }

    [TestMethod]
    public void AFleetWhoseTroopsNeverLandedMovesOnToAnotherPlanet()
    {
        Planet target = AddDummyPlanetToEmpire(new Vector2(400_000), Player);
        Planet other = AddPlanetTo(target.System, new Vector2(30_000f, 0f), Player);

        Assert.AreEqual(target, NextTargetAfterAWave(target, sent: false, landed: false),
            "a fleet that has sent no troops yet tries the planet again with more");
        Assert.AreEqual(target, NextTargetAfterAWave(target, sent: true, landed: true),
            "so does one whose troops reached the ground");
        Assert.AreEqual(other, NextTargetAfterAWave(target, sent: true, landed: false),
            "one whose whole wave was lost before landing moves on to another planet");
    }

    [TestMethod]
    public void AFleetWhoseTroopsNeverLandedGoesHomeWhenNoOtherTargetIsLeft()
    {
        Planet target = AddDummyPlanetToEmpire(new Vector2(400_000), Player);
        var fleet = new Fleet(UState, Enemy);
        var task = new MilitaryTask(target, Enemy, fleet, MilitaryTaskImportance.Normal) { TroopsSent = true };
        fleet.FleetTask = task;
        fleet.AddShip(SpawnShip(Warship, Enemy, target.Position + new Vector2(target.Radius + 5000f, 0f)));
        fleet.TaskStep = 8;

        CallFleet(fleet, "DoAssaultPlanet", task);

        Assert.IsNull(fleet.FleetTask, "the fleet ends its invasion instead of staying to guard a planet it did not take");
    }

    [TestMethod]
    public void TheTaskKnowsWhetherItsTroopsWereSentAndWhetherTheyLanded()
    {
        LoadStarterShips(Player.data.DefaultTroopShip);
        Planet target = AddHomeWorldToEmpire(new Vector2(400_000), Enemy);
        var task = new MilitaryTask(target, Player, MilitaryTaskImportance.Normal);
        var fleet = new Fleet(UState, Player) { FleetTask = task };
        Ship troopShip = SpawnShip(Player.data.DefaultTroopShip, Player, target.Position + new Vector2(target.Radius + 3000f, 0f));
        ResourceManager.CreateTroop("Wyvern", Player).LandOnShip(troopShip);

        CallFleet(fleet, "OrderShipsToInvade", new[] { troopShip }, task, true);
        Assert.IsFalse(task.TroopsSent, "a landing refused while the war is too young to invade sends no wave");

        Player.GetRelations(Enemy).TurnsAtWar = 5;
        CallFleet(fleet, "OrderShipsToInvade", new[] { troopShip }, task, true);
        Assert.IsTrue(task.TroopsSent && !task.TroopsLanded, "the task knows a wave is on its way and has not landed");

        Assert.IsTrue(ResourceManager.CreateTroop("Wyvern", Player).TryLandTroop(target), "setup: a troop reaches the ground");
        CallFleet(fleet, "OrderShipsToInvade", new Ship[0], task, true);
        Assert.IsTrue(task.TroopsLanded, "and that troops reached the ground");

        task.SetTargetPlanet(target);
        Assert.IsFalse(task.TroopsSent || task.TroopsLanded, "a target set again starts with a clean record");
    }

    [TestMethod]
    public void TheAIAsksForHalfAgainMoreTroopsToInvadeACapital()
    {
        Planet capital = AddHomeWorldToEmpire(new Vector2(400_000), Player);
        for (int i = 0; i < 3; ++i)
            ResourceManager.CreateTroop("Wyvern", Player).TryLandTroop(capital);
        float ground = capital.GetGroundStrengthOther(Enemy);
        Assert.IsTrue(ground > 40f, $"setup: the capital is defended, ground strength {ground}");

        var task = new MilitaryTask(capital, Enemy, MilitaryTaskImportance.Normal);
        var planned = (int)typeof(MilitaryTask).GetMethod("GetTargetPlanetGroundStrength", BindingFlags.Instance | BindingFlags.NonPublic)!
                                              .Invoke(task, new object[] { 40 });
        AssertEqual((int)(ground * 1.5f), planned, "the troops the AI gathers are planned for half again the capital's ground strength");

        var fleet = new Fleet(UState, Enemy);
        var atLanding = (float)CallFleet(fleet, "GetGroundStrOfPlanet", capital);
        AssertEqual(0.01f, ground * 1.5f, atLanding, "and so are the landings");
    }
}
