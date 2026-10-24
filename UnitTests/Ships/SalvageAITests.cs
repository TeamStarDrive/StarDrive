using System.Collections;
using System.Linq;
using System.Reflection;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Ship_Game;
using Ship_Game.AI;
using Ship_Game.AI.ExpansionAI;
using Ship_Game.Commands.Goals;
using Ship_Game.GameScreens.LoadGame;
using Ship_Game.Gameplay;
using Ship_Game.Ships;
using Ship_Game.Universe;
using Point = SDGraphics.Point;
using Vector2 = SDGraphics.Vector2;

namespace UnitTests.Ships;

/// <summary>
/// An AI empire's salvage station fills its free salvage units each turn, the derelict with the fewest turns left
/// first, while no armed hostile is in its sensor range, a rival's unarmed station holding off nothing, and one the
/// empire holds without being able to build one does too; the player's and the rebels' order nothing themselves, and
/// pirates' recovers every derelict ship it can but one of a design they spawn, with the constructor of the race whose
/// station it is, which keeps to its work when pirates are called home and is never drafted to defend a base; it
/// strips none, orders nothing with no such constructor, and regains a tenth of its ordnance a turn out of combat
/// unless it makes its own. A ship pirates recover heads for their nearest base, which keeps a ship of a Remnant design,
/// whoever's ship it was, where it takes in another small one, and their orbital raids go for a salvage station first. An AI empire's station recovers a ship stronger than the strongest design of its hull role
/// the empire can build, or of a role it can build none of, and strips the rest. Every few turns the planner orders an
/// AI empire that can build a salvage station one, the closest system first and one a run, in a system it has explored
/// with a derelict it knows of, near its colonies, outside enemy borders unless its own reach there, with no colony of a rival it is at peace
/// with and has no open borders or alliance with, the player counting as any empire, where the production left in the
/// derelicts it can work pays for the station, or one's design uses a tech it wants, or it is a ship it would recover;
/// where it knows of hostile strength it asks once for a fleet to clear the system instead. With Auto Build Salvage
/// Stations on, the player's stations order their derelicts and the planner orders the player stations by the same
/// rules, asking for no fleet, while the Salvage and Recover buttons only switch a standing order's work, its own
/// button still lit and calling nothing off, and their other tooltips say salvage is on automatic; turned off, it
/// leaves its orders standing as the player's. The switch
/// and the planner's timer are saved. An empire with no colony left orders nothing, and a derelict with less than a
/// fifth of its turns left is worth no station. The pick for a system takes the station with the most salvage units
/// up to the derelicts the empire would work there, those it knows of with a fifth of their turns left that no other
/// empire works from its station there, then the fewest units beyond them, and with no system the most units; the
/// planner orders the pick for its system, the derelicts' production weighed against that station's cost. Neither the AI's pick nor the player's takes a station with less cargo room
/// than a load for each of its salvage units.
/// </summary>
[TestClass]
public class SalvageAITests : StarDriveTest
{
    static readonly Vector2 SystemSite = new(600_000f, 600_000f);
    static readonly Vector2 StationSite = SystemSite + new Vector2(30_000f, 0);
    static readonly Vector2 WreckSite = SystemSite + new Vector2(-30_000f, 0);
    const string Older = "Dreadnought mk1-a";
    const string Newer = "Dreadnought mk2-c";
    readonly Planet Local; // in the derelicts' system
    readonly SolarSystem System;
    readonly IShipDesign Station;
    readonly float SalvageableDerelictCapMultiplier = GlobalStats.Defaults.SalvageableDerelictCapMultiplier;

    public SalvageAITests()
    {
        LoadStarterShips("Terran Basic Salvage Station", Older, Newer, ShipDesign.SalvagePlatformName, "Corsair Asteroid Base", "Platform Base mk1-a", "Ancient Frigate");
        CreateUniverseAndPlayerEmpire();
        UState.Objects.EnableParallelUpdate = false;
        Local = AddDummyPlanet(SystemSite, 1f, 1f, 1f, SystemSite + new Vector2(0, 60_000f), explored: true);
        System = Local.System;
        Assert.IsTrue(ResourceManager.Ships.GetDesign("Terran Basic Salvage Station", out Station), "setup: the station is loaded");
        Player.GetRelations(Enemy).AtWar = Enemy.GetRelations(Player).AtWar = false;
        GlobalStats.Defaults.SalvageableDerelictCapMultiplier = 10f; // room for more salvageable derelicts in this small galaxy, last as a failed constructor skips the cleanup
    }

    [TestCleanup]
    public override void Cleanup()
    {
        UState.Derelicts.Clear();
        GlobalStats.Defaults.SalvageableDerelictCapMultiplier = SalvageableDerelictCapMultiplier;
        ToolTip.Clear();
        base.Cleanup();
    }

    // a salvageable derelict of the owner's ship, so the owner knows of it, its turns run down by this many
    Derelict Wreck(string design, Vector2 at, Empire owner, int turnsGone = 0, SolarSystem system = null)
    {
        system ??= System;
        TestShip ship = SpawnShip(design, owner, at);
        Assert.IsTrue(UState.Derelicts.TryAdd(ship, salvageable: true, seen: false), "setup: a salvageable derelict");
        Universe.InvokePendingSimThreadActions();
        ship.QueueTotalRemoval();
        Derelict d = UState.Derelicts.Current.Last();
        for (int i = 0; i < turnsGone; ++i)
            d.UpdateTurn();
        Assert.IsTrue(d.CanBeSalvaged && d.System == system, "setup: the derelict can be salvaged in the system");
        return d;
    }

    // the empire's built station in the system, holding production so it stays with nothing left to salvage; a major empire gets a colony far off unless asked, as one with none orders nothing
    Ship StationOf(Empire owner, bool colony = true)
    {
        if (colony && !owner.IsFaction && owner.NumPlanets == 0)
            AddDummyPlanetToEmpire(ColonySite(owner), owner);
        var order = new SalvageOps(owner, System, StationSite, Station);
        owner.AI.AddGoal(order);
        order.AdvanceToNextStep(); // past the queueing, which needs a port
        order.TargetShip = SpawnShip(Station.Name, owner, StationSite);
        order.TargetShip.LoadProduction(5f);
        order.Evaluate();
        AssertEqual("Salvage", order.StepName, "setup: the station is built");
        AssertEqual(2, SalvageDerelict.LiveUnits(order.TargetShip), "setup: it has two salvage units");
        return order.TargetShip;
    }

    Vector2 ColonySite(Empire owner) => owner == Player ? new(200_000f, 900_000f) : new(900_000f, 200_000f);

    static void Turn(Ship station) => station.Loyalty.AI.FindGoal(g => g is SalvageOps && g.TargetShip == station).Evaluate();

    static SalvageDerelict OrderOf(Empire empire, Derelict d) => SalvageDerelict.OrderOf(empire, d);

    // a hostile, the player's unless given, the empire's station sees in the system
    void HostileThere(Empire empire, Empire hostile = null)
    {
        hostile ??= Player;
        empire.GetRelations(hostile).AtWar = hostile.GetRelations(empire).AtWar = true;
        SpawnShipNoCombatHoldPos("Vulcan Scout", hostile, SystemSite + new Vector2(0, -20_000f));
        SpawnShipNoCombatHoldPos("Vulcan Scout", empire, SystemSite + new Vector2(0, -21_000f));
        UState.Objects.Update(new(time: 2f));
        empire.Threats.Update(new(time: 2f));
        AssertGreaterThan(empire.KnownEnemyStrengthNoResearchStationsIn(System), 0f, "setup: the empire knows of hostile strength there");
    }

    [TestMethod]
    public void AnAIStationFillsItsFreeUnitsTheDerelictWithFewestTurnsLeftFirst()
    {
        Derelict oldest = Wreck(Older, WreckSite, Enemy, turnsGone: 3);
        Derelict newest = Wreck(Older, WreckSite + new Vector2(0, 8_000f), Enemy);
        Derelict older = Wreck(Older, WreckSite + new Vector2(0, 16_000f), Enemy, turnsGone: 1);
        Ship station = StationOf(Enemy);

        Turn(station);
        Assert.IsNotNull(OrderOf(Enemy, oldest), "the derelict with the fewest turns left is ordered");
        Assert.IsNotNull(OrderOf(Enemy, older), "and the next, for the second unit");
        Assert.IsNull(OrderOf(Enemy, newest), "but not the third, with no unit free");
    }

    [TestMethod]
    public void AnAIEmpireWithNoColonyLeftOrdersNothing()
    {
        Derelict d = Wreck(Older, WreckSite, Enemy);
        Ship station = StationOf(Enemy, colony: false);
        AssertEqual(0, Enemy.NumPlanets, "setup: the AI owns no colony, so it is defeated this turn");
        Turn(station);
        Assert.IsNull(OrderOf(Enemy, d), "its station orders nothing");

        AddDummyPlanetToEmpire(ColonySite(Enemy), Enemy);
        Turn(station);
        Assert.IsNotNull(OrderOf(Enemy, d), "with a colony it does");
    }

    [TestMethod]
    public void ItRecoversAShipStrongerThanTheBestOfItsHullRoleItCanBuildOrOfARoleItBuildsNoneOfAndStripsTheRest()
    {
        IShipDesign older = ResourceManager.Ships.GetDesign(Older);
        IShipDesign newer = ResourceManager.Ships.GetDesign(Newer);
        AssertEqual(older.HullRole, newer.HullRole, "setup: both are of one hull role");
        AssertGreaterThan(newer.GetStrength(Enemy), older.GetStrength(Enemy), "setup: the newer is the stronger");
        Assert.IsFalse(Enemy.ShipsWeCanBuildSnapshot.Any(s => s.HullRole == older.HullRole), "setup: the AI builds none of that role");

        Derelict d = Wreck(Older, WreckSite, Enemy, turnsGone: 1); // ordered before the hulk
        Assert.IsTrue(SalvageDerelict.WouldRecover(Enemy, d), "a ship of a role it can build none of is recovered");
        Derelict hulk = Wreck(Station.Name, WreckSite + new Vector2(0, -8_000f), Enemy);
        Assert.IsFalse(SalvageDerelict.WouldRecover(Enemy, hulk), "but a station's derelict, which cannot be, is stripped");

        Assert.IsTrue(Enemy.AddBuildableShip(older), "setup: it can build the same design");
        Assert.IsFalse(SalvageDerelict.WouldRecover(Enemy, d), "one no stronger than the best it can build is stripped");

        Derelict stronger = Wreck(Newer, WreckSite + new Vector2(0, 8_000f), Enemy, turnsGone: 1);
        Assert.IsTrue(SalvageDerelict.WouldRecover(Enemy, stronger), "a stronger one is recovered");

        Ship station = StationOf(Enemy);
        Turn(station);
        Assert.IsFalse(OrderOf(Enemy, d).Recovering, "so the station strips the one");
        Assert.IsTrue(OrderOf(Enemy, stronger).Recovering, "and recovers the other");
    }

    [TestMethod]
    public void AShipOfAHullRoleItBuildsNoneOfIsRecoveredThoughItBuildsStrongerShipsOfOthers()
    {
        IShipDesign capital = ResourceManager.Ships.GetDesign(Newer);
        IShipDesign cruiser = ResourceManager.Ships.GetDesign("Terran-Prototype");
        Assert.IsTrue(Enemy.AddBuildableShip(capital), "setup: the AI builds a capital");
        AssertGreaterThan(capital.GetStrength(Enemy), cruiser.GetStrength(Enemy), "setup: stronger than the cruiser");
        Assert.IsFalse(Enemy.ShipsWeCanBuildSnapshot.Any(s => s.HullRole == cruiser.HullRole), "setup: and builds no cruiser");

        Derelict d = Wreck(cruiser.Name, WreckSite, Enemy);
        Assert.IsTrue(SalvageDerelict.WouldRecover(Enemy, d), "the cruiser is weighed against cruisers only, of which it builds none");
    }

    [TestMethod]
    public void AnAIStationOrdersNothingWhileAnArmedHostileIsInItsSensorRange()
    {
        Ship station = StationOf(Enemy); // before the derelict, so it has nothing to order yet
        Enemy.GetRelations(Player).AtWar = Player.GetRelations(Enemy).AtWar = true;
        Ship hostile = SpawnShip(Older, Player, StationSite + new Vector2(0, 20_000f));
        RunObjectsSim(1.5f);
        Assert.IsTrue(SalvageOps.HaulsBlocked(station), "setup: the station sees an armed hostile");
        Derelict d = Wreck(Older, WreckSite, Enemy);
        Turn(station);
        Assert.IsNull(OrderOf(Enemy, d), "no order while it is there");

        hostile.QueueTotalRemoval();
        RunObjectsSim(1.5f);
        Turn(station);
        Assert.IsNotNull(OrderOf(Enemy, d), "and an order once it is gone");
    }

    [TestMethod]
    public void AWarRivalsUnarmedStationThereHoldsOffNoOrder()
    {
        Ship station = StationOf(Enemy);
        Enemy.GetRelations(Player).AtWar = Player.GetRelations(Enemy).AtWar = true;
        Ship rivals = SpawnShip(Station.Name, Player, StationSite + new Vector2(0, -10_000f));
        RunObjectsSim(1.5f);
        UState.Objects.Update(new(time: 2f));
        Enemy.Threats.Update(new(time: 2f));
        Assert.IsTrue(station.AI.PotentialTargets.Contains(rivals) && rivals.TotalDps <= 0f, "setup: the station sees the rival's unarmed station");
        AssertGreaterThan(Enemy.KnownEnemyStrengthNoResearchStationsIn(System), 0f, "setup: which counts as hostile strength there");
        Derelict d = Wreck(Older, WreckSite, Enemy);
        Turn(station);
        Assert.IsNotNull(OrderOf(Enemy, d), "the station orders all the same");
    }

    [TestMethod]
    public void AStationTheAIHoldsWithoutBeingAbleToBuildOneStillOrders()
    {
        Assert.IsFalse(Enemy.CanBuildSalvageStations, "setup: the AI cannot build a salvage station");
        Derelict d = Wreck(Older, WreckSite, Enemy);
        Ship station = StationOf(Enemy);
        Turn(station);
        Assert.IsNotNull(OrderOf(Enemy, d), "its station still fills its units");
    }

    [TestMethod]
    public void ThePlayersAndTheRebelsStationsOrderNothingThemselves()
    {
        Derelict mine = Wreck(Older, WreckSite, Player);
        Turn(StationOf(Player));
        Assert.IsNull(OrderOf(Player, mine), "with Auto Build Salvage Stations off, the player orders salvage by hand");

        Empire rebels = UState.CreateRebelsFromEmpireData(Player.data, Player);
        Derelict theirs = Wreck(Older, WreckSite + new Vector2(0, 8_000f), rebels);
        Ship rebelStation = StationOf(rebels);
        Turn(rebelStation);
        Assert.IsNull(OrderOf(rebels, theirs), "rebels only finish what they inherit");
    }

    Empire PirateFaction()
    {
        CreateAMinorFaction("Corsairs");
        Assert.IsTrue(Faction.WeArePirates, "setup: pirates");
        return Faction;
    }

    [TestMethod]
    public void APirateStationRecoversEveryDerelictShipItCanWithTheConstructorOfItsRaceAndStripsNone()
    {
        Empire pirates = PirateFaction();
        Derelict hulk = Wreck(Station.Name, WreckSite + new Vector2(0, -8_000f), pirates, turnsGone: 3);
        Derelict weaker = Wreck(Older, WreckSite, pirates, turnsGone: 1);
        Derelict stronger = Wreck(Newer, WreckSite + new Vector2(0, 8_000f), pirates);
        Assert.IsTrue(pirates.AddBuildableShip(ResourceManager.Ships.GetDesign(Newer)), "setup: the pirates hold a stronger design of the role");
        Assert.IsFalse(SalvageDerelict.WouldRecover(pirates, weaker), "setup: so an AI empire would strip the weaker ship");
        Assert.IsTrue(ResourceManager.Ships.GetDesign("Terran Constructor", out _), "setup: the station's race has a constructor");

        Ship station = StationOf(pirates);
        Turn(station);
        Assert.IsNull(OrderOf(pirates, hulk), "a station's derelict, which can only be stripped, is left alone");
        Assert.IsTrue(OrderOf(pirates, weaker) is { Recovering: true } && OrderOf(pirates, stronger) is { Recovering: true },
                      "and its units recover both ships, the weaker too");
        Ship constructor = OrderOf(pirates, weaker).FinishedShip;
        AssertEqual("Terran Constructor", constructor?.Name, "with the constructor of the race whose station it is, as pirates build none");

        AIState working = constructor.AI.State;
        constructor.AI.OrderPirateFleeHome();
        AssertEqual(working, constructor.AI.State, "which keeps to its work when a raid's recall sends the pirates there home");
    }

    [TestMethod]
    public void APirateStationPassesOverAShipOfADesignTheySpawn()
    {
        Empire pirates = PirateFaction();
        Derelict theirs = Wreck(Older, WreckSite, pirates, turnsGone: 1);
        Derelict other = Wreck(Newer, WreckSite + new Vector2(0, 8_000f), pirates);
        pirates.Pirates.ShipsWeCanSpawn.AddUnique(Older); // as their own frigates are
        Turn(StationOf(pirates));
        Assert.IsNull(OrderOf(pirates, theirs), "a ship of a design they spawn, which their base would take in for nothing, is passed over");
        Assert.IsNotNull(OrderOf(pirates, other), "while another is recovered");
    }

    [TestMethod]
    public void APirateBaseKeepsAShipOfARemnantDesignWhereItTakesInAnotherSmallShip()
    {
        Empire pirates = PirateFaction();
        CreateAMinorFaction("Remnant");
        Assert.IsTrue(Faction.WeAreRemnants && UState.Remnants == Faction, "setup: the Remnants are in the game");
        Ship pirateBase = SpawnShip("Corsair Asteroid Base", pirates, SystemSite + new Vector2(0, -40_000f));
        Ship scout = SpawnShip("Vulcan Scout", pirates, SystemSite + new Vector2(0, -38_000f));
        Ship remnant = SpawnShip("Ancient Frigate", pirates, SystemSite + new Vector2(0, -36_000f));
        AssertEqual(RoleName.frigate, remnant.ShipData.HullRole, "setup: a small Remnant ship, which pirates hold as one they recovered does");
        Assert.IsFalse(pirates.Pirates.ShipsWeCanSpawn.Contains(remnant.Name), "setup: of a design they cannot spawn");

        pirates.Pirates.ProcessShip(scout, pirateBase);
        pirates.Pirates.ProcessShip(remnant, pirateBase);
        Assert.IsTrue(scout.AI.FindGoal(ShipAI.Plan.LandOnPirateBase, out _), "the base takes in a small ship");
        Assert.IsFalse(remnant.AI.FindGoal(ShipAI.Plan.LandOnPirateBase, out _), "but keeps the Remnant one");
        Assert.IsTrue(remnant.AI.FindGoal(ShipAI.Plan.Escort, out ShipAI.ShipGoal escort) && escort.TargetShip == pirateBase, "to guard it");
    }

    [TestMethod]
    public void APirateStationWithNoConstructorOfItsRaceForItsBaysOrdersNothing()
    {
        Empire pirates = PirateFaction();
        Ship platform = SpawnShip("Platform Base mk1-a", pirates, SystemSite + new Vector2(0, -40_000f));
        Assert.IsFalse(UState.Empires.Any(e => e.data.Traits.ShipType == platform.ShipData.ShipStyle), "setup: a design of no race in the game");
        AssertEqual("", SalvageDerelict.ConstructorName(pirates, platform), "pirates' bays on a design of no race launch nothing");

        Derelict d = Wreck(Older, WreckSite, pirates);
        Ship station = StationOf(pirates);
        Player.data.DefaultConstructor = "No Such Constructor"; // the station's race, the player's
        Turn(station);
        Assert.IsNull(OrderOf(pirates, d), "and a station whose race has no constructor orders nothing");
    }

    [TestMethod]
    public void APirateBaseUnderAttackDraftsNoConstructor()
    {
        Empire pirates = PirateFaction();
        Ship pirateBase = SpawnShip(pirates.data.PirateBaseBasic, pirates, SystemSite + new Vector2(-80_000f, 0));
        Ship constructor = SpawnShip("Terran Constructor", pirates, SystemSite + new Vector2(-200_000f, 0));
        pirates.GetRelations(Enemy).AtWar = Enemy.GetRelations(pirates).AtWar = true;
        SpawnShipNoCombatHoldPos(Newer, Enemy, pirateBase.Position + new Vector2(3_000f, 0));
        UState.Objects.Update(new(time: 2f));
        AssertGreaterThan(pirateBase.AI.PotentialTargets.Sum(s => s.BaseStrength), pirateBase.AI.FriendliesNearby.Sum(s => s.BaseStrength),
                          "setup: the base is outgunned");
        constructor.BaseStrength = 10f; // as a mod's constructor with armor; vanilla's have none, and no strength

        pirateBase.InCombat = true;
        new PirateDefendBase(pirates, pirateBase).Evaluate();
        Assert.AreNotEqual(pirateBase, constructor.AI.EscortTarget, "a constructor is not drafted to escort the base");
    }

    [TestMethod]
    public void APirateStationThatMakesNoOrdnanceRegainsATenthOfItsStoreATurn()
    {
        Empire pirates = PirateFaction();
        Ship station = StationOf(pirates);
        AssertEqual(0.001f, 0f, station.OrdAddedPerSecond, "setup: the station makes no ordnance");
        station.ChangeOrdnance(-station.Ordinance);
        Turn(station);
        AssertEqual(0.01f, station.OrdinanceMax * 0.1f, station.Ordinance, "one pirates hold regains a tenth of its store a turn");

        station.ChangeOrdnance(-station.Ordinance);
        station.InCombat = true;
        Turn(station);
        AssertEqual(0.01f, 0f, station.Ordinance, "nor in combat, as a pirate base");

        station.InCombat = false;
        station.OrdAddedPerSecond = 1f; // as a design that makes its own
        Turn(station);
        AssertEqual(0.01f, 0f, station.Ordinance, "but not one that makes its own");

        Ship theirs = StationOf(Enemy);
        theirs.ChangeOrdnance(-theirs.Ordinance);
        Turn(theirs);
        AssertEqual(0.01f, 0f, theirs.Ordinance, "nor an AI empire's");
    }

    [TestMethod]
    public void AShipPiratesRecoverHeadsForTheirNearestBase()
    {
        Empire pirates = PirateFaction();
        SpawnShip(pirates.data.PirateBaseBasic, pirates, SystemSite + new Vector2(300_000f, 0));
        Ship near = SpawnShip(pirates.data.PirateBaseBasic, pirates, SystemSite + new Vector2(-80_000f, 0));
        UState.Objects.UpdateLists();
        Assert.IsTrue(pirates.Pirates.GetBases(out var bases) && bases.Count == 2, "setup: two pirate bases");

        Derelict d = Wreck(Older, WreckSite, pirates);
        Ship ship = Ship.CreateRecoveredShip(UState, d, pirates, model: null);
        AssertEqual(near, ship.AI.EscortTarget, "the ship heads for the pirates' nearest base, which takes in a small one and keeps a large one");
    }

    [TestMethod]
    public void APirateOrbitalRaidGoesForASalvageStationFirst()
    {
        Empire pirates = PirateFaction();
        Ship platform = SpawnShip("Platform Base mk1-a", Enemy, SystemSite + new Vector2(0, -40_000f));
        UState.Objects.UpdateLists();
        var raid = new PirateRaidOrbital(pirates, Enemy);
        Assert.IsTrue(raid.PickTarget(Ship_Game.Pirates.TargetType.Platform, out Ship target) && target == platform,
                      "setup: with no salvage station, the raid takes the orbital it rolled");

        Ship station = StationOf(Enemy);
        UState.Objects.UpdateLists();
        Assert.IsTrue(raid.PickTarget(Ship_Game.Pirates.TargetType.Platform, out target), "setup: a target");
        AssertEqual(station, target, "a salvage station comes first, whatever orbital it rolled");
    }

    // the AI able to build the station, with the system explored unless asked
    SalvageOpsPlanner Planner(bool explored = true)
    {
        if (explored)
            System.SetExploredBy(Enemy);
        Enemy.AddBuildableShip(Station);
        Enemy.MarkShipRolesUsableForEmpire(Station);
        Assert.IsTrue(Enemy.CanBuildSalvageStations && ShipBuilder.PickSalvageStation(Enemy) == Station, "setup: and picks it");
        Assert.IsTrue(Enemy.DerelictSystems.Contains(System), "setup: it knows of a derelict in the system");
        SalvageOpsPlanner planner = Enemy.AI.SalvageOpsAI ??= new SalvageOpsPlanner(Enemy);
        return planner;
    }

    // the AI's planner, or the owner's given
    bool Plans(SalvageOpsPlanner planner, bool ignoreDistance = true, Empire owner = null)
    {
        planner.TurnTimer = 1;
        planner.RunSalvageOpsPlanner(ignoreDistance);
        return (owner ?? Enemy).AI.HasGoal(g => g.IsSalvageOpsGoal(System));
    }

    [TestMethod]
    public void ThePlannerOrdersAStationWhereTheDerelictsPayForItButNotWithoutTheTechOrWithSalvageOpsOff()
    {
        Derelict d = Wreck(Older, WreckSite, Enemy);
        AssertGreaterThan(d.ProductionLeft, Station.GetCost(Enemy), "setup: the derelict's production pays for the station");
        System.SetExploredBy(Enemy);
        Assert.IsTrue(Enemy.AddBuildableShip(Station) && ShipBuilder.PickSalvageStation(Enemy) == Station, "setup: it has the station's design");
        Assert.IsFalse(Enemy.CanBuildSalvageStations, "setup: but cannot build a salvage station yet");
        SalvageOpsPlanner planner = Enemy.AI.SalvageOpsAI;
        Assert.IsNotNull(planner, "setup: its planner comes with its AI");
        Assert.IsFalse(Plans(planner), "not while the AI cannot build a salvage station");

        planner = Planner();
        UState.P.DisableSalvageOps = true;
        Assert.IsFalse(Plans(planner), "nor with salvage ops off");

        UState.P.DisableSalvageOps = false;
        planner.TurnTimer = 2;
        planner.RunSalvageOpsPlanner(ignoreDistance: true);
        Assert.IsFalse(Enemy.AI.HasGoal(g => g.IsSalvageOpsGoal(System)), "nor before its turn");
        Assert.IsTrue(Plans(planner), "but on its turn it orders one");
        var order = (SalvageOps)Enemy.AI.FindGoal(g => g.IsSalvageOpsGoal(System));
        Assert.IsTrue(SalvageOps.IsValidSite(System, order.StaticBuildPos, SalvageOps.StationsSeenBy(Enemy, UState.Objects.GetShips())),
                      "at a site the derelict panel's button would pick");
    }

    [TestMethod]
    public void ACheapDerelictIsWorthAStationOnlyForATechItsDesignUsesThatTheAIWantsOrAShipItWouldRecover()
    {
        Derelict d = Wreck(Older, WreckSite, Enemy);
        UState.Derelicts.Strip(d, null, d.ProductionLeft - 10f);
        AssertLessThan(d.ProductionLeft, Station.GetCost(Enemy), "setup: what is left does not pay for the station");
        Assert.IsTrue(Enemy.AddBuildableShip(ResourceManager.Ships.GetDesign(Newer)), "setup: the AI builds a stronger ship of its role");
        Assert.IsFalse(SalvageDerelict.WouldRecover(Enemy, d), "setup: so it would strip it");
        string[] designTechs = Derelict.TechsOf(d.Design).ToArray();
        foreach (string tech in designTechs)
            Enemy.GetTechEntry(tech).shipDesignsCanuseThis = false; // as no design of its race uses them
        SalvageOpsPlanner planner = Planner();
        Assert.IsFalse(Plans(planner), "with no tech it wants, a cheap derelict is not worth a station");

        TechEntry wanted = designTechs.Except(d.Techs).Select(Enemy.GetTechEntry).First(t => SalvageDerelict.CanLearn(Enemy, t));
        wanted.shipDesignsCanuseThis = true; // one it can still learn that the derelict does not hold
        Assert.IsTrue(Plans(planner), "one whose design uses a tech it wants is, whichever few techs it holds");

        Enemy.AI.RemoveGoal(Enemy.AI.FindGoal(g => g.IsSalvageOpsGoal(System)));
        Enemy.UnlockTech(wanted.UID, TechUnlockType.Normal);
        Assert.IsFalse(Plans(planner), "but not once it has researched that tech");

        Assert.IsTrue(Enemy.RemoveBuildableShip(ResourceManager.Ships.GetDesign(Newer)), "setup: it builds none of that role again");
        Assert.IsTrue(SalvageDerelict.WouldRecover(Enemy, d), "setup: so it would recover the derelict");
        Assert.IsTrue(Plans(planner), "and a ship it would recover is worth one");
    }

    // the basic station with this many salvage units, in a strip below its grid, and all its cargo holds
    ShipDesign WithUnits(int units)
    {
        ShipModule unit = ResourceManager.GetModuleTemplate("SalvageUnit");
        ShipDesign design = ((ShipDesign)Station).GetClone($"Station Of {units} Units");
        ShipGridInfo info = design.GridInfo;
        var size = new Point(unit.XSize, unit.YSize);
        DesignSlot[] slots = Station.GetOrLoadDesignSlots().Where(s => !ResourceManager.GetModuleTemplate(s.ModuleUID).Salvage)
            .Concat(Enumerable.Range(0, units).Select(i => new DesignSlot(new Point(i * size.X, info.Size.Y), unit.UID, size, 0, ModuleOrientation.Normal, null)))
            .ToArray();
        info.Size = new Point(units * size.X > info.Size.X ? units * size.X : info.Size.X, info.Size.Y + size.Y);
        design.GridInfo = info;
        design.SetDesignSlots(slots, updateRole: false);
        AssertEqual(units, design.NumSalvageModules, $"setup: a station with {units} salvage units");
        return design;
    }

    [TestMethod]
    public void ThePickForASystemTakesTheMostSalvageUnitsUpToItsDerelictsThenTheFewestBeyondThem()
    {
        ShipDesign three = WithUnits(3);
        ShipDesign four = WithUnits(4);
        AssertEqual(2, Station.NumSalvageModules, "setup: the basic station has two salvage units");
        Assert.IsTrue(ShipBuilder.IsUsableSalvageStation(three) && ShipBuilder.IsUsableSalvageStation(four), "setup: each holds a load for each of its units");
        Enemy.ClearShipsWeCanBuild();
        Enemy.AddBuildableShip(Station);
        Enemy.AddBuildableShip(four);
        Enemy.AddBuildableShip(three);
        AssertEqual(four, ShipBuilder.PickSalvageStation(Enemy), "with no system, the most salvage units");

        Wreck(Older, WreckSite, Enemy);
        AssertEqual(Station, ShipBuilder.PickSalvageStation(Enemy, System), "for one derelict, the fewest units");
        Wreck(Older, WreckSite + new Vector2(0, 8_000f), Enemy);
        Wreck(Older, WreckSite + new Vector2(0, 16_000f), Enemy);
        AssertEqual(three, ShipBuilder.PickSalvageStation(Enemy, System), "for three, as many units as derelicts and no more");
        Wreck(Older, WreckSite + new Vector2(0, 24_000f), Enemy);
        AssertEqual(four, ShipBuilder.PickSalvageStation(Enemy, System), "for four, four");
        Wreck(Older, WreckSite + new Vector2(0, 32_000f), Enemy);
        AssertEqual(four, ShipBuilder.PickSalvageStation(Enemy, System), "and for more derelicts than any station has units, the most units");
    }

    [TestMethod]
    public void ThePickForASystemCountsOnlyTheDerelictsTheEmpireWouldWorkThere()
    {
        ShipDesign three = WithUnits(3);
        Enemy.ClearShipsWeCanBuild();
        Enemy.AddBuildableShip(Station);
        Enemy.AddBuildableShip(three);
        Wreck(Older, WreckSite, Enemy);
        Wreck(Older, WreckSite + new Vector2(0, 8_000f), Enemy);
        AssertEqual(2, SalvageOps.DerelictsToWorkIn(Enemy, System), "setup: two derelicts it knows of there");
        AssertEqual(Station, ShipBuilder.PickSalvageStation(Enemy, System), "setup: two units for two derelicts");

        Derelict sinking = Wreck(Older, WreckSite + new Vector2(0, 16_000f), Enemy, turnsGone: 161);
        AssertEqual(39, sinking.TurnsLeft, "setup: 39 of its 200 turns left");
        AssertEqual(Station, ShipBuilder.PickSalvageStation(Enemy, System), "not one with less than a fifth of its turns left");

        Derelict worked = Wreck(Older, WreckSite + new Vector2(0, 24_000f), Player);
        Assert.IsTrue(worked.IsKnownBy(Enemy), "setup: the empire knows of every derelict in the system");
        StationOf(Player);
        Assert.IsTrue(worked.TryClaim(Player), "setup: the player's station works it");
        AssertEqual(Station, ShipBuilder.PickSalvageStation(Enemy, System), "nor one another empire works from its station there");

        Wreck(Older, WreckSite + new Vector2(0, 32_000f), Enemy);
        AssertEqual(three, ShipBuilder.PickSalvageStation(Enemy, System), "while a third it would work counts");
    }

    [TestMethod]
    public void ThePlannerOrdersThePickForTheSystem()
    {
        Wreck(Older, WreckSite, Enemy);
        SalvageOpsPlanner planner = Planner();
        ShipDesign three = WithUnits(3);
        Enemy.AddBuildableShip(three);
        AssertEqual(three, ShipBuilder.PickSalvageStation(Enemy), "setup: with no system, the station with more units");
        Assert.IsTrue(Plans(planner), "setup: the planner orders a station");
        AssertEqual(Station, Enemy.AI.FindGoal(g => g.IsSalvageOpsGoal(System)).ToBuild, "the one whose units fit the system's one derelict");
    }

    [TestMethod]
    public void ThePlannerWeighsTheDerelictsAgainstTheCostOfThePickForTheSystem()
    {
        Derelict d = Wreck(Older, WreckSite, Enemy);
        IShipDesign three = WithUnits(3);
        float basicCost = Station.GetCost(Enemy);
        float threeCost = three.GetCost(Enemy);
        AssertGreaterThan(threeCost, basicCost, "setup: a third unit costs more");
        UState.Derelicts.Strip(d, null, d.ProductionLeft - (basicCost + threeCost) / 2f);
        Assert.IsTrue(d.ProductionLeft > basicCost && d.ProductionLeft < threeCost, "setup: what is left pays for the basic station but not the one with three units");
        Assert.IsTrue(Enemy.AddBuildableShip(ResourceManager.Ships.GetDesign(Newer)), "setup: the AI builds a stronger ship of its role");
        Assert.IsFalse(SalvageDerelict.WouldRecover(Enemy, d), "setup: so it would strip it");
        foreach (string tech in Derelict.TechsOf(d.Design))
            Enemy.GetTechEntry(tech).shipDesignsCanuseThis = false; // as no design of its race uses them

        SalvageOpsPlanner planner = Planner();
        Enemy.AddBuildableShip(three);
        AssertEqual(three, ShipBuilder.PickSalvageStation(Enemy), "setup: with no system, the station with more units");
        Assert.IsTrue(Plans(planner), "the derelict pays for the station whose units fit it");
        AssertEqual(Station, Enemy.AI.FindGoal(g => g.IsSalvageOpsGoal(System)).ToBuild, "which is the one ordered");
    }

    [TestMethod]
    public void NeitherPickTakesAStationWithLessCargoRoomThanALoadForEachOfItsSalvageUnits()
    {
        AssertEqual(75f, ShipResupply.SalvageLoad, "setup: a load of 75, the platform's hold");
        ShipDesign five = WithUnits(5);
        Assert.IsTrue(five.BaseCargoSpace >= 300f && five.BaseCargoSpace < 375f, "setup: room for four loads but not five");
        Assert.IsFalse(ShipBuilder.IsUsableSalvageStation(five), "a station with five units and room for four loads cannot be used");

        Enemy.ClearShipsWeCanBuild();
        Enemy.AddBuildableShip(Station);
        Enemy.AddBuildableShip(five);
        AssertEqual(Station, ShipBuilder.PickSalvageStation(Enemy), "the AI's pick passes over it");

        Player.ClearShipsWeCanBuild();
        Player.AddBuildableShip(Station);
        Player.AddBuildableShip(five);
        Player.data.CurrentSalvageStation = five.Name;
        Assert.IsFalse(Player.AutoPickBestSalvageStation, "setup: the player's auto pick is off");
        AssertEqual(Station, ShipBuilder.PickSalvageStation(Player), "and the player's chosen station gives way to the race's default");
    }

    [TestMethod]
    public void ARivalsColonyKeepsThePlannerOutUnlessItHasOpenBordersOrAnAllianceWithThem()
    {
        Wreck(Older, WreckSite, Enemy);
        Local.SetOwner(Player);
        System.UpdateOwnerList();
        Assert.IsTrue(System.OwnerList.Contains(Player), "setup: the player has a colony there");
        SalvageOpsPlanner planner = Planner();
        Assert.IsFalse(Plans(planner), "the player's colony keeps out an AI at peace with it and with no treaty");

        Enemy.SignTreatyWith(Player, TreatyType.OpenBorders);
        Assert.IsTrue(Enemy.IsOpenBordersTreaty(Player), "setup: open borders");
        Assert.IsTrue(Plans(planner), "with open borders it orders one");
    }

    [TestMethod]
    public void AColonyOfAnEmpireItIsAtWarWithKeepsThePlannerOutByItsBorders()
    {
        Wreck(Older, WreckSite, Enemy);
        Local.SetOwner(Player);
        System.UpdateOwnerList();
        Enemy.GetRelations(Player).AtWar = Player.GetRelations(Enemy).AtWar = true;
        SalvageOpsPlanner planner = Planner();
        AssertEqual(InfluenceStatus.Enemy, UState.Influence.GetInfluenceStatus(Enemy, System.Position), "setup: the system lies in an enemy's borders");
        Assert.IsFalse(Plans(planner), "no station inside an enemy's borders");
    }

    [TestMethod]
    public void AColonyOfAnEmpireItIsAtWarWithLetsThePlannerInWhereItsOwnBordersReach()
    {
        Wreck(Older, WreckSite, Enemy);
        Local.SetOwner(Player);
        System.UpdateOwnerList();
        Enemy.GetRelations(Player).AtWar = Player.GetRelations(Enemy).AtWar = true;
        SpawnShip("Subspace Projector", Enemy, SystemSite + new Vector2(10_000f, 0));
        RunObjectsSim(TestSimStep);
        SalvageOpsPlanner planner = Planner();
        AssertEqual(InfluenceStatus.Friendly, UState.Influence.GetInfluenceStatus(Enemy, System.Position), "setup: its own projector's borders come first");
        Assert.IsTrue(Plans(planner), "an enemy's colony is no rival's");
    }

    [TestMethod]
    public void ThePlannerSkipsASystemTheAIHasNotExploredAndOrdersNothingForThePlayer()
    {
        Wreck(Older, WreckSite, Enemy);
        SalvageOpsPlanner planner = Planner(explored: false);
        Assert.IsFalse(Plans(planner), "not in a system it has not explored");
        System.SetExploredBy(Enemy);
        Assert.IsTrue(Plans(planner), "but once it has");

        Wreck(Older, WreckSite + new Vector2(0, 8_000f), Player);
        Assert.IsTrue(Player.AddBuildableShip(Station), "setup: the player can build the station");
        Player.MarkShipRolesUsableForEmpire(Station);
        Assert.IsTrue(Player.CanBuildSalvageStations && Player.DerelictSystems.Contains(System), "setup: and knows of a derelict in the system");
        Assert.IsFalse(Plans(new SalvageOpsPlanner(Player), owner: Player), "the planner orders nothing for the player with auto build off");
    }

    [TestMethod]
    public void WithAutoBuildOnThePlayersStationOrdersItsDerelictsAndTurningItOffLeavesTheOrdersAsThePlayers()
    {
        Derelict d = Wreck(Older, WreckSite, Player);
        Ship station = StationOf(Player);
        Player.AutoBuildSalvageStations = true;
        Turn(station);
        SalvageDerelict order = OrderOf(Player, d);
        Assert.IsNotNull(order, "with auto build on, the player's station orders its derelicts");

        Player.AutoBuildSalvageStations = false;
        Derelict later = Wreck(Older, WreckSite + new Vector2(0, 8_000f), Player);
        Turn(station);
        Assert.IsNull(OrderOf(Player, later), "turned off, it orders no more");
        AssertEqual(order, OrderOf(Player, d), "and leaves its order standing as the player's");
    }

    [TestMethod]
    public void WithAutoBuildOnThePlannerOrdersThePlayerAStationButAsksForNoFleetWhereItKnowsOfHostiles()
    {
        Wreck(Older, WreckSite, Player);
        Assert.IsTrue(Player.AddBuildableShip(Station), "setup: the player can build the station");
        Player.MarkShipRolesUsableForEmpire(Station);
        Assert.IsTrue(Player.CanBuildSalvageStations && Player.DerelictSystems.Contains(System), "setup: and knows of a derelict in the system");
        var planner = new SalvageOpsPlanner(Player);
        Player.AutoBuildSalvageStations = true;
        Assert.IsTrue(Plans(planner, owner: Player), "with auto build on, the planner orders the player a station");

        Player.AI.RemoveGoal(Player.AI.FindGoal(g => g.IsSalvageOpsGoal(System)));
        HostileThere(Player, hostile: Enemy);
        Assert.IsFalse(Plans(planner, owner: Player), "but none where the player knows of hostile strength");
        Assert.IsFalse(Player.AI.HasGoal(g => g is DefendSystem), "and asks for no fleet");
    }

    [TestMethod]
    public void WithSalvageOnAutomaticOnlyAStandingOrdersOtherButtonClicksToSwitchItsWork()
    {
        Derelict d = Wreck(Older, WreckSite, Player);
        Derelict other = Wreck(Older, WreckSite + new Vector2(0, 8_000f), Player);
        StationOf(Player);
        Assert.IsTrue(SalvageDerelict.Order(Player, d), "setup: an order placed by hand");
        Player.AutoBuildSalvageStations = true;
        AssertEqual(SalvageDerelict.Block.None, SalvageDerelict.WhyNoOrder(Player, other), "setup: a unit is free for the other");

        Assert.IsFalse(DerelictSalvageButton.Clickable(Player, SalvageDerelict.Block.None, lit: false), "with salvage on automatic a derelict with no order lights no button under the cursor");
        DerelictSalvageButton.Click(Player, other, DerelictSalvageButton.Kind.Salvage, SalvageDerelict.Block.None, lit: false);
        DerelictSalvageButton.Click(Player, other, DerelictSalvageButton.Kind.Recover, SalvageDerelict.Block.None, lit: false);
        Assert.IsNull(OrderOf(Player, other), "and no click orders");

        SalvageDerelict order = OrderOf(Player, d);
        Assert.IsTrue(DerelictSalvageButton.Lit(DerelictSalvageButton.Kind.Salvage, SalvageDerelict.Block.Ordered, order), "the standing order's button stays lit");
        Assert.IsFalse(DerelictSalvageButton.Clickable(Player, SalvageDerelict.Block.Ordered, lit: true), "but does not light up under the cursor");
        DerelictSalvageButton.Click(Player, d, DerelictSalvageButton.Kind.Salvage, SalvageDerelict.Block.Ordered, lit: true);
        Assert.IsTrue(Player.AI.HasGoal(order) && !order.CalledOff, "and its second click calls nothing off");

        Assert.IsTrue(DerelictSalvageButton.Clickable(Player, SalvageDerelict.Block.Ordered, lit: false), "the other button lights up under the cursor");
        DerelictSalvageButton.Click(Player, d, DerelictSalvageButton.Kind.Recover, SalvageDerelict.Block.Ordered, lit: false);
        Assert.IsTrue(Player.AI.HasGoal(order) && order.Recovering, "and its click turns the order to recovery");

        Player.AutoBuildSalvageStations = false;
        Assert.IsTrue(DerelictSalvageButton.Clickable(Player, SalvageDerelict.Block.None, lit: false), "turned off, the buttons light again");
    }

    [TestMethod]
    public void WithSalvageOnAutomaticTheButtonsTooltipsSaySoButTheSwitchsOwn()
    {
        Derelict d = Wreck(Older, WreckSite, Player);
        Derelict theirs = Wreck(Older, WreckSite + new Vector2(0, 8_000f), Player);
        StationOf(Player);
        Assert.IsTrue(SalvageDerelict.Order(Player, d), "setup: an order");
        SalvageDerelict order = OrderOf(Player, d);
        Assert.IsTrue(theirs.TryClaim(Enemy), "setup: the enemy's claim on the other");
        AssertEqual(string.Format(Localizer.Token(GameText.DerelictSalvagedBy), Enemy.Name),
                    Tip(DerelictSalvageButton.Kind.Salvage, theirs, SalvageDerelict.Block.ClaimedByOther, false, null), "setup: a claim's tooltip names its empire");
        AssertEqual("derelicts_salvaging", ToolTip.GetActiveCodexUid(), "linked to Salvaging");

        Player.AutoBuildSalvageStations = true;
        AssertEqual(Localizer.Token(GameText.SalvageAutomaticTip),
                    Tip(DerelictSalvageButton.Kind.Salvage, theirs, SalvageDerelict.Block.ClaimedByOther, false, null), "with salvage on automatic a claim's button says so");
        AssertEqual(Localizer.Token(GameText.SalvageAutomaticOrderTip),
                    Tip(DerelictSalvageButton.Kind.Salvage, d, SalvageDerelict.Block.Ordered, true, order), "as the standing order's own button does, offering the switch");
        AssertEqual("derelicts_salvaging", ToolTip.GetActiveCodexUid(), "linked to Salvaging");
        StringAssert.StartsWith(Tip(DerelictSalvageButton.Kind.Recover, d, SalvageDerelict.Block.Ordered, false, order),
                                "Recover this ship instead", "while the other button gives its switch");
    }

    [TestMethod]
    public void WithSalvageOnAutomaticAStationsLitButtonOffersNoSwitch()
    {
        Derelict d = Wreck("Terran Basic Salvage Station", WreckSite, Player);
        StationOf(Player);
        Assert.IsTrue(SalvageDerelict.Order(Player, d), "setup: a station's derelict ordered");
        Player.AutoBuildSalvageStations = true;
        AssertEqual(Localizer.Token(GameText.SalvageAutomaticTip),
                    Tip(DerelictSalvageButton.Kind.Salvage, d, SalvageDerelict.Block.Ordered, true, OrderOf(Player, d)),
                    "its lit button offers no switch a station's derelict cannot take");
    }

    // the raw text of the button's tooltip, the only one shown
    string Tip(DerelictSalvageButton.Kind kind, Derelict d, SalvageDerelict.Block block, bool lit, SalvageDerelict order)
    {
        ToolTip.Clear();
        DerelictSalvageButton.Tooltip(Player, d, kind, block, lit, order);
        var tips = (IEnumerable)typeof(ToolTip).GetField("ActiveTips", BindingFlags.NonPublic | BindingFlags.Static)!.GetValue(null);
        object tip = tips!.Cast<object>().Single();
        return (string)tip.GetType().GetField("RawText")!.GetValue(tip);
    }

    [TestMethod]
    public void AnAllianceLetsThePlannerInBesideAnAllysColony()
    {
        Wreck(Older, WreckSite, Enemy);
        Local.SetOwner(Player);
        System.UpdateOwnerList();
        Enemy.SignTreatyWith(Player, TreatyType.Alliance);
        Assert.IsTrue(Enemy.IsAlliedWith(Player) && !Enemy.IsOpenBordersTreaty(Player), "setup: an alliance with no open borders");
        Assert.IsTrue(Plans(Planner()), "an ally's colony does not keep it out");
    }

    [TestMethod]
    public void WhereTheAIKnowsOfHostileStrengthThePlannerAsksForAFleetInsteadOfAStation()
    {
        HostileThere(Enemy);
        Wreck(Older, WreckSite, Enemy);
        SalvageOpsPlanner planner = Planner();
        Assert.IsFalse(Plans(planner), "no station where it knows of hostile strength");
        Assert.IsTrue(Enemy.AI.HasGoal(g => g is DefendSystem d && d.TargetSystem == System), "it asks for a fleet to clear the system");
        Plans(planner);
        AssertEqual(1, Enemy.AI.CountGoals(g => g is DefendSystem), "and asks once, though the fleet's task is not raised yet");
    }

    [TestMethod]
    public void ADerelictWithLessThanAFifthOfItsTurnsLeftIsWorthNoStation()
    {
        Derelict d = Wreck(Older, WreckSite, Enemy, turnsGone: 161);
        AssertEqual(39, d.TurnsLeft, "setup: 39 of its 200 turns left");
        AssertGreaterThan(d.ProductionLeft, Station.GetCost(Enemy), "setup: its production pays for the station");
        SalvageOpsPlanner planner = Planner();
        Assert.IsFalse(Plans(planner), "with less than a fifth of its turns left, it is worth no station");

        Derelict later = Wreck(Older, WreckSite + new Vector2(0, 8_000f), Enemy, turnsGone: 160);
        AssertEqual(40, later.TurnsLeft, "setup: 40 of its 200 turns left");
        Assert.IsTrue(Plans(planner), "one with a fifth left is");
    }

    [TestMethod]
    public void ThePlannerOrdersOneStationARunTheSystemClosestToTheEmpireFirst()
    {
        Vector2 farSite = SystemSite * 1.5f;
        SolarSystem far = AddDummyPlanet(farSite, 1f, 1f, 1f, farSite + new Vector2(0, 60_000f), explored: false).System;
        far.SetExploredBy(Enemy);
        Wreck(Older, farSite + new Vector2(-30_000f, 0), Enemy, system: far);
        Wreck(Older, WreckSite, Enemy);
        Enemy.WeightedCenter = Vector2.Zero;
        AssertLessThan(System.Position.SqDist(Enemy.WeightedCenter), far.Position.SqDist(Enemy.WeightedCenter), "setup: one system is closer");

        SalvageOpsPlanner planner = Planner();
        Assert.IsTrue(Plans(planner), "the closer system is ordered first");
        Assert.IsFalse(Enemy.AI.HasGoal(g => g.IsSalvageOpsGoal(far)), "and only it, one a run");
        Plans(planner);
        Assert.IsTrue(Enemy.AI.HasGoal(g => g.IsSalvageOpsGoal(far)), "the farther one on the next run");
    }

    [TestMethod]
    public void ThePlannerKeepsToSystemsNearTheEmpire()
    {
        Wreck(Older, WreckSite, Enemy);
        SalvageOpsPlanner planner = Planner();
        Assert.IsFalse(Plans(planner, ignoreDistance: false), "not in a system far from the empire's colonies");

        Local.SetOwner(Enemy);
        System.UpdateOwnerList();
        Assert.IsTrue(System.HasPlanetsOwnedBy(Enemy), "setup: the AI has a colony there");
        Assert.IsTrue(Plans(planner, ignoreDistance: false), "but in one where it has a colony");
    }

    [TestMethod]
    public void TheAutoBuildSwitchAndThePlannersTimerSurviveSaveAndLoad()
    {
        Player.AutoBuildSalvageStations = true;
        Enemy.AI.SalvageOpsAI.TurnTimer = 7;
        AddDummyPlanetToEmpire(ColonySite(Player), Player); // the loader needs a colony of each major empire
        AddDummyPlanetToEmpire(ColonySite(Enemy), Enemy);
        UState.StarDate = 1001f; // the loader needs a stardate past 1000
        SavedGame save = Universe.Save("UnitTest.SalvageAI", throwOnError: true);
        UniverseScreen game = LoadGame.Load(save.SaveFile, noErrorDialogs: true, startSimThread: false);
        Assert.IsNotNull(game, "setup: the save loads");

        Assert.IsTrue(game.UState.Player.AutoBuildSalvageStations, "Auto Build Salvage Stations stays on");
        AssertEqual(7, game.UState.GetEmpireById(Enemy.Id).AI.SalvageOpsAI.TurnTimer, "and the planner keeps its count of turns");
    }
}
