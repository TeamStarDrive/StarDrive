using System;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using SDUtils;
using Ship_Game;
using Ship_Game.AI;
using Ship_Game.Commands.Goals;
using Ship_Game.GameScreens.LoadGame;
using Ship_Game.Gameplay;
using Ship_Game.Ships;
using Point = SDGraphics.Point;
using Vector2 = SDGraphics.Vector2;

namespace UnitTests.Ships;

/// <summary>
/// An empire orders a salvage station only in a system with a salvageable derelict it knows of, and holds one order
/// per system, so empires can each have one there; of two orders that reach the sim thread together, the first holds
/// the system. The order queues the station as an urgent orbital, the station its constructor builds becomes the
/// order's, untethered where it was ordered, and losing it, or its own construction before that, ends the order. An
/// abort before it is built takes the order and its constructor away, as salvage ops off does, and as the system
/// running out of derelicts does; a built station keeps its order. A site lies in the system, clear of its star and
/// planets' orbits and 2000 units from its derelicts and from salvage stations. An order survives a save. The picker
/// takes the station with the most salvage modules, then the most cargo room.
/// </summary>
[TestClass]
public class SalvageOpsTests : StarDriveTest
{
    static readonly Vector2 SystemSite = new(600_000f, 600_000f);
    static readonly Vector2 InSystem = SystemSite + new Vector2(30_000f, 0);
    static readonly Vector2 BuildPos = SystemSite + new Vector2(40_000f, 0);
    readonly SolarSystem WreckSystem;
    readonly IShipDesign Station;
    readonly IShipDesign ResearchStation;

    public SalvageOpsTests()
    {
        LoadStarterShips("Dreadnought mk1-a", "Terran Basic Salvage Station");
        CreateUniverseAndPlayerEmpire();
        WreckSystem = AddDummyPlanet(SystemSite, 1f, 1f, 1f, SystemSite + new Vector2(0, 60_000f), explored: true).System;
        AddHomeWorldToEmpire(new Vector2(200_000f), Player);
        Player.UpdateRallyPoints();
        Assert.IsTrue(ResourceManager.Ships.GetDesign("Terran Basic Salvage Station", out Station), "setup: the station is loaded");
        Assert.IsTrue(ResourceManager.Ships.GetDesign("Basic Research Station", out ResearchStation), "setup: a research station is loaded");
    }

    [TestCleanup]
    public override void Cleanup()
    {
        UState.Derelicts.Clear();
        base.Cleanup();
    }

    void Forms(Empire owner, Vector2 at)
    {
        TestShip ship = SpawnShip("Dreadnought mk1-a", owner, at);
        Assert.IsTrue(UState.Derelicts.TryAdd(ship, salvageable: true, seen: false), "setup: a salvageable derelict");
        Universe.InvokePendingSimThreadActions();
    }

    [TestMethod]
    public void AStationIsOrderedOnlyWhereTheEmpireKnowsOfASalvageableDerelictAndOncePerSystem()
    {
        Assert.IsFalse(SalvageOps.CanOrderIn(Player, WreckSystem), "no derelict there");
        Assert.IsFalse(SalvageOps.CanOrderIn(Player, null), "nor outside every system");

        Forms(Player, InSystem);
        Assert.IsTrue(SalvageOps.CanOrderIn(Player, WreckSystem), "a derelict the player knows of");
        Assert.IsFalse(SalvageOps.CanOrderIn(Enemy, WreckSystem), "one the other empire does not know of");

        Player.AI.AddGoal(new SalvageOps(Player, WreckSystem, BuildPos, Station));
        Assert.IsFalse(SalvageOps.CanOrderIn(Player, WreckSystem), "one order per empire per system");

        Forms(Enemy, InSystem + new Vector2(0, 10_000f));
        Assert.IsTrue(SalvageOps.CanOrderIn(Enemy, WreckSystem), "another empire that knows of one there can have its own");

        UState.P.DisableSalvageOps = true;
        Assert.IsFalse(SalvageOps.CanOrderIn(Enemy, WreckSystem), "no order with salvage ops off");
    }

    [TestMethod]
    public void TheOrderQueuesAnUrgentOrbitalAndTakesTheStationItsConstructorBuilds()
    {
        Forms(Player, InSystem);
        var order = new SalvageOps(Player, WreckSystem, BuildPos, Station);
        Player.AI.AddGoalAndEvaluate(order);

        var build = (DeepSpaceBuildGoal)Player.AI.FindGoal(g => g.IsBuildingOrbitalFor(WreckSystem) && g.ToBuild == Station);
        Assert.IsNotNull(build, "the order queues its station");
        AssertEqual(BuildPos, build.BuildPosition, "at the place it was ordered");
        QueueItem item = build.PlanetBuildingAt.ConstructionQueue.First(q => q.Goal == build);
        AssertEqual(QueueItemType.OrbitalUrgent, item.QType, "as an urgent orbital, as research and mining stations are");

        Ship constructor = SpawnShip(item.ShipData.Name, Player, BuildPos);
        constructor.AI.OrderDeepSpaceBuild(build, Station.GetCost(Player), Station.Grid.Radius);
        RunSimWhile((60, fatal: true), () => order.TargetShip == null, () => constructor.Construction.AddConstructionFromBuilder());

        Ship station = order.TargetShip;
        Assert.IsTrue(station.IsSalvageStation, "the order holds the station its constructor built");
        AssertEqual(BuildPos, station.Position, "where it was ordered");
        Assert.IsNull(station.GetTether(), "orbiting no planet");

        order.Evaluate();
        AssertEqual("Salvage", order.StepName, "the order goes on once the station is built");
        Assert.IsFalse(SalvageOps.CanOrderIn(Player, WreckSystem), "and keeps the system's one order");

        RunOutDerelicts();
        order.Evaluate();
        Assert.IsTrue(Player.AI.HasGoal(g => g == order), "a built station keeps its order when the system's derelicts are gone");

        Forms(Player, InSystem + new Vector2(0, 15_000f));

        station.Die(null, cleanupOnly: true);
        order.Evaluate();
        Assert.IsFalse(Player.AI.HasGoal(g => g == order), "a lost station ends the order");
        Assert.IsTrue(SalvageOps.CanOrderIn(Player, WreckSystem), "so another can be ordered there");
    }

    [TestMethod]
    public void AnAbortBeforeTheStationIsBuiltRemovesTheOrderAndItsConstruction()
    {
        Forms(Player, InSystem);
        var order = new SalvageOps(Player, WreckSystem, BuildPos, Station);
        Player.AI.AddGoalAndEvaluate(order);
        Goal build = Player.AI.FindGoal(g => g.IsBuildingOrbitalFor(WreckSystem) && g.ToBuild == Station);
        QueueItem item = build.PlanetBuildingAt.ConstructionQueue.First(q => q.Goal == build);
        Assert.IsFalse(item.IsCancelled, "setup: the station is queued");

        Player.AI.CancelSalvageStation(WreckSystem);
        Assert.IsFalse(Player.AI.HasGoal(g => g == order || g == build), "the order and its construction are gone");
        Assert.IsTrue(item.IsCancelled, "and the queued constructor leaves the queue at its next update");
        Assert.IsTrue(SalvageOps.CanOrderIn(Player, WreckSystem), "so the system can be ordered again");
    }

    [TestMethod]
    public void WithSalvageOpsOffAnOrderEndsWithItsConstruction()
    {
        Forms(Player, InSystem);
        var order = new SalvageOps(Player, WreckSystem, BuildPos, Station);
        Player.AI.AddGoalAndEvaluate(order);
        Goal build = Player.AI.FindGoal(g => g.IsBuildingOrbitalFor(WreckSystem));
        QueueItem item = build.PlanetBuildingAt.ConstructionQueue.First(q => q.Goal == build);

        UState.P.DisableSalvageOps = true;
        order.Evaluate();
        Assert.IsFalse(Player.AI.HasGoal(g => g == order), "an order loaded under a mod without salvage content ends");
        Assert.IsFalse(Player.AI.HasGoal(g => g == build), "and so does its construction");
        Assert.IsTrue(item.IsCancelled, "whose constructor leaves the queue");
    }

    // the system's derelicts run out of turns and leave it, so it leaves every empire's list
    void RunOutDerelicts()
    {
        for (int turn = 0; turn < 10_000 && Player.DerelictSystems.Contains(WreckSystem); ++turn)
            UState.Derelicts.UpdateTurn();
        Assert.IsFalse(Player.DerelictSystems.Contains(WreckSystem), "setup: no derelict left in the system");
    }

    [TestMethod]
    public void AnOrderNotYetBuiltIsDroppedOnceItsSystemHasNoDerelictLeft()
    {
        Forms(Player, InSystem);
        var order = new SalvageOps(Player, WreckSystem, BuildPos, Station);
        Player.AI.AddGoalAndEvaluate(order);
        Goal build = Player.AI.FindGoal(g => g.IsBuildingOrbitalFor(WreckSystem));
        QueueItem item = build.PlanetBuildingAt.ConstructionQueue.First(q => q.Goal == build);

        RunOutDerelicts();
        order.Evaluate();
        Assert.IsFalse(Player.AI.HasGoal(g => g == order || g == build), "the order and its construction end");
        Assert.IsTrue(item.IsCancelled, "and its constructor leaves the queue");
    }

    [TestMethod]
    public void AStationSiteIsInTheSystemAndClearOfItsStarPlanetsDerelictsAndSalvageStations()
    {
        Forms(Player, InSystem);
        Planet planet = WreckSystem.PlanetList[0];
        Ship rival = SpawnShip(Station.Name, Enemy, SystemSite + new Vector2(0, -40_000f));
        Ship other = SpawnShip("Dreadnought mk1-a", Enemy, SystemSite + new Vector2(0, 40_000f));
        Ship[] ships = { rival, other };
        bool Valid(Vector2 pos) => SalvageOps.IsValidSite(WreckSystem, pos, ships);

        Assert.IsTrue(Valid(BuildPos), "in the system, clear of everything");
        float near = SalvageOps.Clearance - 100f, clear = SalvageOps.Clearance + 100f;
        Assert.IsFalse(Valid(InSystem + new Vector2(near, 0)), "too near a salvageable derelict");
        Assert.IsTrue(Valid(InSystem + new Vector2(clear, 0)), "just clear of it");
        Assert.IsFalse(Valid(rival.Position + new Vector2(near, 0)), "too near a salvage station, any empire's");
        Assert.IsTrue(Valid(rival.Position + new Vector2(clear, 0)), "just clear of it");
        Assert.IsTrue(Valid(other.Position + new Vector2(near, 0)), "other ships do not count");
        Assert.IsFalse(Valid(planet.Position + new Vector2(DeepSpaceBuildingWindow.TetherRadius(planet) - 100f, 0)), "in a planet's orbit");
        Assert.IsFalse(Valid(SystemSite + new Vector2(DeepSpaceBuildingWindow.MinimumBuildDistanceFromSun - 100f, 0)), "too near the star");
        Assert.IsFalse(Valid(SystemSite + new Vector2(WreckSystem.Radius * DeepSpaceBuildingWindow.SystemPlacementRadius + 100f, 0)),
                       "outside the system's placement area");
    }

    [TestMethod]
    public void AnOrderEndsWhenItsOwnConstructionGoesAwayWhateverElseIsBuiltInTheSystem()
    {
        Forms(Player, InSystem);
        var order = new SalvageOps(Player, WreckSystem, BuildPos, Station);
        Player.AI.AddGoalAndEvaluate(order);
        Goal salvageBuild = Player.AI.FindGoal(g => g.IsBuildingOrbitalFor(WreckSystem));
        Planet port = salvageBuild.PlanetBuildingAt;

        var research = new ProcessResearchStation(Player, WreckSystem, SystemSite + new Vector2(20_000f, 0), ResearchStation);
        Player.AI.AddGoalAndEvaluate(research);
        Goal researchBuild = Player.AI.FindGoal(g => g.IsBuildingOrbitalFor(WreckSystem) && g.ToBuild == ResearchStation);
        Assert.IsTrue(port != null && researchBuild != null, "setup: a research station is on its way to the same system");

        Player.AI.RemoveGoal(salvageBuild);
        order.Evaluate();
        Assert.IsFalse(Player.AI.HasGoal(g => g == order), "the salvage order ends with its own construction, not the research one's");

        Player.AI.AddGoalAndEvaluate(new SalvageOps(Player, WreckSystem, BuildPos, Station));
        Player.AI.RemoveGoal(researchBuild);
        research.Evaluate();
        Assert.IsFalse(Player.AI.HasGoal(g => g == research), "and a research order at the star ends with its own, not a salvage one's");
    }

    [TestMethod]
    public void OfTwoOrdersThatReachTheSimThreadTogetherTheFirstHoldsTheSystem()
    {
        Forms(Player, InSystem);
        var first = new SalvageOps(Player, WreckSystem, BuildPos, Station);
        var second = new SalvageOps(Player, WreckSystem, BuildPos + new Vector2(5_000f, 0), Station);
        Player.AI.AddGoal(first);
        Player.AI.AddGoal(second);

        second.Evaluate();
        first.Evaluate();
        Assert.IsFalse(Player.AI.HasGoal(g => g == second), "the later order ends");
        Assert.IsTrue(Player.AI.HasGoal(g => g == first), "the first stays");
        AssertEqual(1, Player.AI.FindGoals(g => g.IsBuildingOrbitalFor(WreckSystem)).Length, "and only it queues a station");
    }

    [TestMethod]
    public void AnOrderWaitingForItsConstructorSurvivesSaveAndLoad()
    {
        Forms(Player, InSystem);
        Player.AI.AddGoalAndEvaluate(new SalvageOps(Player, WreckSystem, BuildPos, Station));

        UState.StarDate = 1001f; // the loader needs a stardate past 1000
        SavedGame save = Universe.Save("UnitTest.SalvageOps", throwOnError: true);
        UniverseScreen loaded = LoadGame.Load(save.SaveFile, noErrorDialogs: true, startSimThread: false);
        Assert.IsNotNull(loaded, "setup: the save loads");

        Empire player = loaded.UState.Player;
        var order = (SalvageOps)player.AI.FindGoal(g => g is SalvageOps);
        Assert.IsNotNull(order, "the order loads");
        AssertEqual(WreckSystem.Name, order.TargetSystem?.Name, "for its system");
        AssertEqual(BuildPos, order.StaticBuildPos, "and its place");
        AssertEqual(Station.Name, order.ToBuild?.Name, "and its station");
        AssertEqual("WaitForConstructor", order.StepName, "waiting for its constructor");
        Assert.IsTrue(player.AI.HasGoal(g => g.IsBuildingOrbitalFor(order.TargetSystem) && g.ToBuild?.IsSalvageStation == true),
                      "which is still queued");
        Assert.IsFalse(SalvageOps.CanOrderIn(player, order.TargetSystem), "and it still holds the system");

        order.Evaluate();
        Assert.IsTrue(player.AI.HasGoal(g => g == order), "it goes on waiting in the loaded game, whose derelicts it still knows of");
    }

    [TestMethod]
    public void ThePickerTakesTheMostSalvageModulesThenTheMostCargoRoom()
    {
        Player.ClearShipsWeCanBuild();
        Assert.IsNull(ShipBuilder.PickSalvageStation(Player), "none until a salvage station can be built");

        Player.AddBuildableShip(Station);
        AssertEqual(Station, ShipBuilder.PickSalvageStation(Player), "setup: the basic station");

        ShipDesign fewer = Variant("Fewer Units Station", dropOneSalvageUnit: true, extraHolds: 3);
        Assert.IsTrue(fewer.NumSalvageModules < Station.NumSalvageModules && fewer.BaseCargoSpace > Station.BaseCargoSpace,
                      "setup: one salvage unit fewer, more cargo room");
        Player.AddBuildableShip(fewer);
        AssertEqual(Station, ShipBuilder.PickSalvageStation(Player), "more salvage units win over more cargo room");

        ShipDesign roomier = Variant("Roomier Station", dropOneSalvageUnit: false, extraHolds: 1);
        Assert.IsTrue(roomier.NumSalvageModules == Station.NumSalvageModules && roomier.BaseCargoSpace > Station.BaseCargoSpace,
                      "setup: as many salvage units, more cargo room");
        Player.AddBuildableShip(roomier);
        AssertEqual(roomier, ShipBuilder.PickSalvageStation(Player), "with as many, more cargo room wins");
    }

    // the basic station, less one salvage unit if asked, plus medium cargo holds in a strip below its grid
    ShipDesign Variant(string name, bool dropOneSalvageUnit, int extraHolds)
    {
        ShipModule hold = ResourceManager.GetModuleTemplate("MediumCargoHold");
        ShipDesign design = ((ShipDesign)Station).GetClone(name);
        var slots = new Array<DesignSlot>(Station.GetOrLoadDesignSlots());
        if (dropOneSalvageUnit)
            slots.Remove(slots.First(s => ResourceManager.GetModuleTemplate(s.ModuleUID).Salvage));

        ShipGridInfo info = design.GridInfo;
        var size = new Point(hold.XSize, hold.YSize);
        for (int i = 0; i < extraHolds; ++i)
            slots.Add(new DesignSlot(new Point(i * size.X, info.Size.Y), hold.UID, size, 0, ModuleOrientation.Normal, null));
        info.Size = new Point(Math.Max(info.Size.X, extraHolds * size.X), info.Size.Y + size.Y);
        design.GridInfo = info;
        design.SetDesignSlots(slots.ToArray(), updateRole: false);
        return design;
    }
}
