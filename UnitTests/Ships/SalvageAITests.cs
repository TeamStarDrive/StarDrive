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
/// it can build none of, and strips the rest. A pirate base under attack drafts no constructor. A ship pirates recover
/// heads for their nearest base, which keeps a ship of a Remnant design, whoever's ship it was, where it takes in
/// another small one, and their orbital raids go for a salvage platform first.
/// </summary>
[TestClass]
public class SalvageAITests : StarDriveTest
{
    static readonly Vector2 SystemSite = new(600_000f, 600_000f);
    static readonly Vector2 WreckSite = SystemSite + new Vector2(-30_000f, 0);
    const string Older = "Dreadnought mk1-a";
    const string Newer = "Dreadnought mk2-c";
    const string Station = "Terran Small Patrol Station 1";
    readonly Planet Local; // in the derelicts' system
    readonly SolarSystem System;
    readonly float SalvageableDerelictCapMultiplier = GlobalStats.Defaults.SalvageableDerelictCapMultiplier;

    public SalvageAITests()
    {
        LoadStarterShips(Station, Older, Newer, ShipDesign.SalvagePlatformName, "Corsair Asteroid Base", "Platform Base mk1-a", "Ancient Frigate", "Terran Salvage Ship");
        CreateUniverseAndPlayerEmpire();
        UState.Objects.EnableParallelUpdate = false;
        Local = AddDummyPlanet(SystemSite, 1f, 1f, 1f, SystemSite + new Vector2(0, 60_000f), explored: true);
        System = Local.System;
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
        Derelict hulk = Wreck(Station, WreckSite + new Vector2(0, -8_000f), Enemy);
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
        UState.Objects.UpdateLists();
        var raid = new PirateRaidOrbital(pirates, Enemy);
        Assert.IsTrue(raid.PickTarget(Ship_Game.Pirates.TargetType.Platform, out Ship target) && target == platform,
                      "with no salvage platform, the raid takes the orbital it rolled");

        Ship salvage = SpawnShip(ShipDesign.SalvagePlatformName, Enemy, WreckSite + new Vector2(0, 2_000f));
        UState.Objects.UpdateLists();
        Assert.IsTrue(raid.PickTarget(Ship_Game.Pirates.TargetType.Platform, out target), "setup: a target");
        AssertEqual(salvage, target, "a salvage platform comes first, whatever orbital it rolled");
    }
}
