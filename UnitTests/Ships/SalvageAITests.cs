using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Ship_Game;
using Ship_Game.AI;
using Ship_Game.Commands.Goals;
using Ship_Game.Gameplay;
using Ship_Game.Ships;
using Point = SDGraphics.Point;
using Vector2 = SDGraphics.Vector2;

namespace UnitTests.Ships;

/// <summary>
/// An AI empire recovers a ship stronger than the strongest design of its hull role the empire can build, or of a role
/// it can build none of, and strips the rest. A pirate station regains a tenth of its ordnance a turn out of combat
/// unless it makes its own, and a pirate base under attack drafts no constructor. A ship pirates recover heads for their
/// nearest base, which keeps a ship of a Remnant design, whoever's ship it was, where it takes in another small one,
/// and their orbital raids go for a salvage platform first, a salvage station being an orbital like any other. The pick
/// for a system takes the station with the most salvage units up to the derelicts the empire would work there, those it
/// knows of with a fifth of their turns left that no other empire works there, then the fewest units beyond them, and
/// with no system the most units. Neither the AI's pick nor the player's takes a station with less cargo room than a
/// load for each of its salvage units.
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
        LoadStarterShips("Terran Basic Salvage Station", Older, Newer, ShipDesign.SalvagePlatformName, "Corsair Asteroid Base", "Platform Base mk1-a", "Ancient Frigate", "Terran Salvage Ship");
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
    Derelict Wreck(string design, Vector2 at, Empire owner, int turnsGone = 0)
    {
        TestShip ship = SpawnShip(design, owner, at);
        Assert.IsTrue(UState.Derelicts.TryAdd(ship, salvageable: true, seen: false), "setup: a salvageable derelict");
        Universe.InvokePendingSimThreadActions();
        ship.QueueTotalRemoval();
        Derelict d = UState.Derelicts.Current.Last();
        for (int i = 0; i < turnsGone; ++i)
            d.UpdateTurn();
        Assert.IsTrue(d.CanBeSalvaged && d.System == System, "setup: the derelict can be salvaged in the system");
        return d;
    }

    // the empire's built station in the system, holding production so it stays with nothing left to salvage; a major empire gets a colony far off
    Ship StationOf(Empire owner)
    {
        if (!owner.IsFaction && owner.NumPlanets == 0)
            AddDummyPlanetToEmpire(ColonySite(owner), owner);
        var order = new SalvageOps(owner, System, StationSite, Station);
        owner.AI.AddGoal(order);
        order.AdvanceToNextStep(); // past the queueing, which needs a port
        order.TargetShip = SpawnShip(Station.Name, owner, StationSite);
        order.TargetShip.LoadProduction(5f);
        order.Evaluate();
        AssertEqual("Salvage", order.StepName, "setup: the station is built");
        return order.TargetShip;
    }

    Vector2 ColonySite(Empire owner) => owner == Player ? new(200_000f, 900_000f) : new(900_000f, 200_000f);

    static void Turn(Ship station) => station.Loyalty.AI.FindGoal(g => g is SalvageOps && g.TargetShip == station).Evaluate();

    [TestMethod]
    public void ItRecoversAShipStrongerThanTheBestOfItsHullRoleItCanBuildOrOfARoleItBuildsNoneOfAndStripsTheRest()
    {
        IShipDesign older = ResourceManager.Ships.GetDesign(Older);
        IShipDesign newer = ResourceManager.Ships.GetDesign(Newer);
        AssertEqual(older.HullRole, newer.HullRole, "setup: both are of one hull role");
        AssertGreaterThan(newer.GetStrength(Enemy), older.GetStrength(Enemy), "setup: the newer is the stronger");
        Assert.IsFalse(Enemy.ShipsWeCanBuildSnapshot.Any(s => s.HullRole == older.HullRole), "setup: the AI builds none of that role");

        Derelict d = Wreck(Older, WreckSite, Enemy);
        Assert.IsTrue(SalvageDerelict.WouldRecover(Enemy, d), "a ship of a role it can build none of is recovered");
        Derelict hulk = Wreck(Station.Name, WreckSite + new Vector2(0, -8_000f), Enemy);
        Assert.IsFalse(SalvageDerelict.WouldRecover(Enemy, hulk), "but a station's derelict, which cannot be, is stripped");

        Assert.IsTrue(Enemy.AddBuildableShip(older), "setup: it can build the same design");
        Assert.IsFalse(SalvageDerelict.WouldRecover(Enemy, d), "one no stronger than the best it can build is stripped");

        Derelict stronger = Wreck(Newer, WreckSite + new Vector2(0, 8_000f), Enemy);
        Assert.IsTrue(SalvageDerelict.WouldRecover(Enemy, stronger), "a stronger one is recovered");
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

    Empire PirateFaction()
    {
        CreateAMinorFaction("Corsairs");
        Assert.IsTrue(Faction.WeArePirates, "setup: pirates");
        return Faction;
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
        station.OrdinanceMax = 100f; // a store, as a design with missiles has
        station.ChangeOrdnance(-station.Ordinance);
        Turn(station);
        AssertEqual(0.01f, 10f, station.Ordinance, "one pirates hold regains a tenth of its store a turn");

        station.ChangeOrdnance(-station.Ordinance);
        station.InCombat = true;
        Turn(station);
        AssertEqual(0.01f, 0f, station.Ordinance, "nor in combat, as a pirate base");

        station.InCombat = false;
        station.OrdAddedPerSecond = 1f; // as a design that makes its own
        Turn(station);
        AssertEqual(0.01f, 0f, station.Ordinance, "but not one that makes its own");

        Ship theirs = StationOf(Enemy);
        theirs.OrdinanceMax = 100f;
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
    public void APirateOrbitalRaidGoesForASalvagePlatformFirst()
    {
        Empire pirates = PirateFaction();
        Ship platform = SpawnShip("Platform Base mk1-a", Enemy, SystemSite + new Vector2(0, -40_000f));
        Ship station = StationOf(Enemy);
        UState.Objects.UpdateLists();
        Assert.IsFalse(station.IsPlatform, "setup: a salvage station is no platform");
        var raid = new PirateRaidOrbital(pirates, Enemy);
        Assert.IsTrue(raid.PickTarget(Ship_Game.Pirates.TargetType.Platform, out Ship target) && target == platform,
                      "with no salvage platform, the raid takes the orbital it rolled, though a salvage station stands");

        Ship salvage = SpawnShip(ShipDesign.SalvagePlatformName, Enemy, WreckSite + new Vector2(0, 2_000f));
        UState.Objects.UpdateLists();
        Assert.IsTrue(raid.PickTarget(Ship_Game.Pirates.TargetType.Platform, out target), "setup: a target");
        AssertEqual(salvage, target, "a salvage platform comes first, whatever orbital it rolled");
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
    public void NeitherPickTakesAStationWithLessCargoRoomThanALoadForEachOfItsSalvageUnits()
    {
        AssertEqual(75f, ShipResupply.SalvageLoad, "setup: a load of 75, the platform's hold");
        ShipDesign thirteen = WithUnits(13);
        Assert.IsTrue(thirteen.BaseCargoSpace >= 900f && thirteen.BaseCargoSpace < 975f, "setup: room for twelve loads but not thirteen");
        Assert.IsFalse(ShipBuilder.IsUsableSalvageStation(thirteen), "a station with thirteen units and room for twelve loads cannot be used");

        Enemy.ClearShipsWeCanBuild();
        Enemy.AddBuildableShip(Station);
        Enemy.AddBuildableShip(thirteen);
        AssertEqual(Station, ShipBuilder.PickSalvageStation(Enemy), "the AI's pick passes over it");

        Player.ClearShipsWeCanBuild();
        Player.AddBuildableShip(Station);
        Player.AddBuildableShip(thirteen);
        Player.data.CurrentSalvageStation = thirteen.Name;
        Assert.IsFalse(Player.AutoPickBestSalvageStation, "setup: the player's auto pick is off");
        AssertEqual(Station, ShipBuilder.PickSalvageStation(Player), "and the player's chosen station gives way to the race's default");
    }
}
