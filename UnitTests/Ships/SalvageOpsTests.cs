using System;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using SDUtils;
using Ship_Game;
using Ship_Game.AI;
using Ship_Game.Commands.Goals;
using Ship_Game.GameScreens.LoadGame;
using Ship_Game.Gameplay;
using Ship_Game.Ships;
using Ship_Game.Utils;
using Color = Microsoft.Xna.Framework.Color;
using Point = SDGraphics.Point;
using Vector2 = SDGraphics.Vector2;

namespace UnitTests.Ships;

/// <summary>
/// An empire orders a salvage station only in a system with a salvageable derelict it knows of that no other empire
/// works from its station there, and holds one order per system, so empires can each have one there; of two orders that reach the sim thread together, the first
/// holds the system. The order queues the station as an urgent orbital, the station its constructor builds becomes the
/// order's, untethered where it was ordered, and losing it, or its own construction before that, ends the order. An
/// abort before it is built takes the order and its constructor away, as salvage ops off does, and as the system
/// running out of derelicts for it does; a built station keeps its order while it holds production. A site lies in the
/// system, clear of its star and planets' orbits and 2000 units from its derelicts and from salvage stations. An order
/// survives a save. The picker with no system in mind takes the station with the most salvage modules, then the most
/// cargo room. The player's picker, with auto pick off as it starts, takes the station chosen, or the race's default
/// when the choice cannot be built, and the best pick when neither can. Neither picker takes a station with less cargo
/// room than a load, a salvage platform's hold, for each of its salvage units. The derelicts window pauses the
/// game and reads the goals as it opens and once a click in it has reached the sim thread. A built station costs the builder trust, once, with each AI that has a colony in its system and knows it, unless the
/// two have open borders, an alliance or a war: the AI personality's value, 5 for all, times a difficulty multiplier
/// when the builder is the player. The value survives a save, 0 included. A boarded station works for its boarder as it
/// changes hands, past the build, with no built notice and no trust cost, and takes the place of the boarder's order
/// there; where the boarder has a station already, its troops leave and it is evacuated, and the player is told, though
/// one just lost does not count. Pirates keep one they board. An absorbed empire's station works for the absorber once
/// it changes hands a step later, or is removed with its troops where the absorber has one, with no one told, and
/// either way an evacuated or removed one leaves its owner's ships. One boarded from an empire that then merges stays
/// with its boarder, a defeated empire's works for its rebels, and one taken over survives a save. A change of hands
/// applied off the sim thread is taken over on it, unless the station is gone or has changed hands again by then; an
/// order that finds its station in other hands hands it on, once.
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
        LoadStarterShips("Dreadnought mk1-a", "Terran Basic Salvage Station", "Assault Shuttle", ShipDesign.SalvagePlatformName);
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

        station.LoadProduction(5f); // an empty one would be scrapped with nothing left to salvage
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
    public void NoStationIsOrderedWhereOtherEmpiresWorkEveryDerelictFromTheirStationsThere()
    {
        Forms(Player, InSystem);
        Player.AddBuildableShip(Station);
        Derelict d = UState.Derelicts.Current.Last();
        Assert.IsTrue(d.TryClaim(Enemy), "setup: another empire claims the derelict");
        AssertEqual(SalvageOps.OrderBlock.None, SalvageOps.WhyNoOrderIn(Player, WreckSystem), "with no station of its own there its claim is ending, so the player can order");

        BuiltStation(Enemy, BuildPos);
        Assert.IsFalse(SalvageOps.CanOrderIn(Player, WreckSystem), "with its station there, the player cannot");
        AssertEqual(SalvageOps.OrderBlock.AllWorked, SalvageOps.WhyNoOrderIn(Player, WreckSystem), "since every derelict there is worked");

        Forms(Player, InSystem + new Vector2(0, 10_000f));
        AssertEqual(SalvageOps.OrderBlock.None, SalvageOps.WhyNoOrderIn(Player, WreckSystem), "a second derelict no one works lets it order again");
    }

    [TestMethod]
    public void AnOrderNotYetBuiltIsDroppedOnceOtherEmpiresWorkEveryDerelictThere()
    {
        Forms(Player, InSystem);
        Derelict d = UState.Derelicts.Current.Last();
        var order = new SalvageOps(Player, WreckSystem, BuildPos, Station);
        Player.AI.AddGoalAndEvaluate(order);
        Goal build = Player.AI.FindGoal(g => g.IsBuildingOrbitalFor(WreckSystem));
        Assert.IsNotNull(build, "setup: the station is queued");

        Assert.IsTrue(d.TryClaim(Enemy), "setup: another empire claims the derelict");
        order.Evaluate();
        Assert.IsTrue(Player.AI.HasGoal(g => g == order), "while that empire has no station there, the order stands");

        BuiltStation(Enemy, BuildPos + new Vector2(0, -10_000f));
        order.Evaluate();
        Assert.IsFalse(Player.AI.HasGoal(g => g == order || g == build), "with its station there, the order and its construction end");
    }

    [TestMethod]
    public void AStationSiteIsInTheSystemAndClearOfItsStarPlanetsDerelictsAndSalvageStations()
    {
        Forms(Player, InSystem);
        Planet planet = WreckSystem.PlanetList[0];
        Ship rival = SpawnShip(Station.Name, Enemy, SystemSite + new Vector2(0, -40_000f));
        Ship other = SpawnShip("Dreadnought mk1-a", Enemy, SystemSite + new Vector2(0, 40_000f));
        Ship[] ships = { rival, other };
        bool Valid(Vector2 pos) => SalvageMission.IsValidSite(WreckSystem, pos, ships);

        Assert.IsTrue(Valid(BuildPos), "in the system, clear of everything");
        float near = SalvageMission.Clearance - 100f, clear = SalvageMission.Clearance + 100f;
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
    public void TheSiteSearchStartsWithin1000OfTheDerelictsAverageSpot()
    {
        Forms(Player, InSystem);
        Forms(Player, InSystem + new Vector2(0, 10_000f));
        Vector2 average = InSystem + new Vector2(0, 5_000f);

        Assert.IsTrue(SalvageMission.FindSite(WreckSystem, Empty<Ship>.Array, new SeededRandom(1), out Vector2 site), "a site is found");
        Assert.IsTrue(site.InRadius(average, SalvageMission.FirstSearchRadius), "within 1000 of the derelicts' average spot, where it is clear");
    }

    [TestMethod]
    public void TheSiteSearchWidensUntilAPointIsClear()
    {
        Forms(Player, InSystem);
        Ship rival = SpawnShip(Station.Name, Enemy, InSystem + new Vector2(3_000f, 0));
        for (int seed = 1; seed <= 20; ++seed)
        {
            Assert.IsTrue(SalvageMission.FindSite(WreckSystem, new[] { rival }, new SeededRandom(seed), out Vector2 site), $"seed {seed}: a site is found");
            Assert.IsTrue(SalvageMission.IsValidSite(WreckSystem, site, new[] { rival }), $"seed {seed}: and it is a valid one");
            Assert.IsTrue(site.OutsideRadius(InSystem, SalvageMission.Clearance), $"seed {seed}: clear of the derelict, which sits on the average spot");
        }

        SolarSystem empty = AddDummyPlanet(SystemSite + new Vector2(0, 400_000f), 1f, 1f, 1f, SystemSite + new Vector2(0, 460_000f), explored: true).System;
        Assert.IsFalse(SalvageMission.FindSite(empty, Empty<Ship>.Array, new SeededRandom(1), out _), "no site in a system without derelicts");
    }

    [TestMethod]
    public void TheSiteSearchGivesUpWhenNoPointInTheSystemIsClear()
    {
        Forms(Player, InSystem);
        WreckSystem.Radius = DeepSpaceBuildingWindow.MinimumBuildDistanceFromSun / DeepSpaceBuildingWindow.SystemPlacementRadius;
        Assert.IsFalse(SalvageMission.FindSite(WreckSystem, Empty<Ship>.Array, new SeededRandom(1), out _),
                       "a system whose placement area lies within the star's 20000 has no site, and the search ends");
    }

    [TestMethod]
    public void ThePanelButtonKeepsClearOfTheSalvageStationsThePlayerSeesOnly()
    {
        Forms(Player, InSystem);
        Player.AddBuildableShip(Station);
        Assert.IsTrue(SalvageMission.FindSite(WreckSystem, Empty<Ship>.Array, new SeededRandom(7), out Vector2 free), "setup: the site with nothing else there");

        Ship unseen = SpawnShip(Station.Name, Enemy, free);
        Assert.IsTrue(SalvageOps.OrderNearDerelicts(Player, WreckSystem, new[] { unseen }, new SeededRandom(7)), "an order");
        AssertEqual(free, ((SalvageOps)Player.AI.FindGoal(g => g is SalvageOps)).StaticBuildPos, "a station the player does not see changes nothing");

        Player.AI.CancelSalvageStation(WreckSystem);
        unseen.KnownByEmpires.SetSeen(Player);
        Assert.IsTrue(SalvageOps.OrderNearDerelicts(Player, WreckSystem, new[] { unseen }, new SeededRandom(7)), "another order");
        Vector2 site = ((SalvageOps)Player.AI.FindGoal(g => g is SalvageOps)).StaticBuildPos;
        Assert.IsTrue(site.OutsideRadius(free, SalvageMission.Clearance), "keeps clear of one the player sees");
    }

    [TestMethod]
    public void ThePanelButtonOrdersThePickedStationAtAFreeSiteNearTheDerelicts()
    {
        Forms(Player, InSystem);
        Player.ClearShipsWeCanBuild();
        AssertEqual(SalvageOps.OrderBlock.NoDesign, SalvageOps.WhyNoOrderIn(Player, WreckSystem), "greyed until a salvage station can be built");
        Assert.IsFalse(SalvageOps.OrderNearDerelicts(Player, WreckSystem, Empty<Ship>.Array, new SeededRandom(1)), "so it orders nothing");

        Player.AddBuildableShip(Station);
        AssertEqual(SalvageOps.OrderBlock.None, SalvageOps.WhyNoOrderIn(Player, WreckSystem), "then it can order");
        Assert.IsTrue(SalvageOps.OrderNearDerelicts(Player, WreckSystem, Empty<Ship>.Array, new SeededRandom(1)), "and does");
        var order = (SalvageOps)Player.AI.FindGoal(g => g is SalvageOps);
        AssertEqual(Station, order.ToBuild, "the picked station");
        AssertEqual(WreckSystem, order.TargetSystem, "in the derelict's system");
        Assert.IsTrue(SalvageMission.IsValidSite(WreckSystem, order.StaticBuildPos, Empty<Ship>.Array), "at a free site");
        Assert.IsTrue(order.StaticBuildPos.InRadius(InSystem, 10_000f), "near the derelict");

        AssertEqual(SalvageOps.OrderBlock.StationThere, SalvageOps.WhyNoOrderIn(Player, WreckSystem), "greyed once a station is on order");
        Assert.IsFalse(SalvageOps.OrderNearDerelicts(Player, WreckSystem, Empty<Ship>.Array, new SeededRandom(2)), "and orders no second one");
        AssertEqual(SalvageOps.OrderBlock.NotHere, SalvageOps.WhyNoOrderIn(Enemy, WreckSystem), "an empire that knows of no derelict there cannot order");
    }

    [TestMethod]
    public void AStationStillThereIsSeenWhenItIsOursOrInOurSensors()
    {
        Ship ours = SpawnShip(Station.Name, Player, SystemSite + new Vector2(0, -40_000f));
        Ship unseen = SpawnShip(Station.Name, Enemy, SystemSite + new Vector2(0, -50_000f));
        Ship seen = SpawnShip(Station.Name, Enemy, SystemSite + new Vector2(0, -60_000f));
        Ship warship = SpawnShip("Dreadnought mk1-a", Player, SystemSite + new Vector2(0, -70_000f));
        seen.KnownByEmpires.SetSeen(Player);

        Ship[] stations = SalvageMission.SitesSeenBy(Player, new[] { ours, unseen, seen, warship });
        Assert.IsTrue(stations.Contains(ours) && stations.Contains(seen), "our own station and one in our sensors");
        Assert.IsFalse(stations.Contains(unseen), "not one we do not see");
        Assert.IsFalse(stations.Contains(warship), "and no other ship");

        ours.Die(null, cleanupOnly: true);
        Assert.IsFalse(SalvageMission.SitesSeenBy(Player, new[] { ours, seen }).Contains(ours), "nor one that is gone");
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
        Player.AutoPickBestSalvageStation = true;
        Player.ClearShipsWeCanBuild();
        Assert.IsNull(ShipBuilder.PickSalvageStation(Player), "none until a salvage station can be built");

        Player.AddBuildableShip(Station);
        AssertEqual(Station, ShipBuilder.PickSalvageStation(Player), "setup: the basic station");

        ShipDesign fewer = Variant("Fewer Units Station", dropOneSalvageUnit: true, extraHolds: 9);
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

    [TestMethod]
    public void WithAutoPickOffThePanelButtonOrdersTheStationChosenInTheAutomationWindow()
    {
        Assert.IsFalse(Player.AutoPickBestSalvageStation, "auto pick starts off");
        AssertEqual(Station.Name, Player.data.DefaultSalvageStation, "setup: the basic station is the race's default");
        ShipDesign fewer = Variant("Fewer Units Station", dropOneSalvageUnit: true, extraHolds: 9);
        ShipDesign roomier = Variant("Roomier Station", dropOneSalvageUnit: false, extraHolds: 1);
        Player.ClearShipsWeCanBuild();
        Player.AddBuildableShip(Station);
        Player.AddBuildableShip(fewer);
        Player.AddBuildableShip(roomier);
        AssertEqual(Station, ShipBuilder.PickSalvageStation(Player), "with nothing chosen, the race's default, though a better one can be built");

        Player.data.CurrentSalvageStation = fewer.Name;
        AssertEqual(fewer, ShipBuilder.PickSalvageStation(Player), "the station chosen in the Automation window");
        Forms(Player, InSystem);
        ResourceManager.AddShipTemplate(fewer, playerDesign: false); // the order queues its station by name
        try
        {
            Assert.IsTrue(SalvageOps.OrderNearDerelicts(Player, WreckSystem, Empty<Ship>.Array, new SeededRandom(1)), "setup: the panel button orders");
            AssertEqual(fewer, Player.AI.FindGoal(g => g is SalvageOps).ToBuild, "the chosen station");
        }
        finally
        {
            ResourceManager.Ships.Delete(fewer.Name);
        }

        Player.AutoPickBestSalvageStation = true;
        AssertEqual(roomier, ShipBuilder.PickSalvageStation(Player), "auto pick takes the best, whatever was chosen");
    }

    [TestMethod]
    public void AChoiceThatCannotBeBuiltGivesWayToTheRaceDefaultThenToTheBestPick()
    {
        ShipDesign roomier = Variant("Roomier Station", dropOneSalvageUnit: false, extraHolds: 1);
        Player.ClearShipsWeCanBuild();
        Player.AddBuildableShip(Station);
        Player.AddBuildableShip(roomier);
        Player.AddBuildableShip(ResearchStation);

        Player.data.CurrentSalvageStation = "No Such Station Design";
        AssertEqual(Station, ShipBuilder.PickSalvageStation(Player), "a deleted design gives way to the race's default");
        Player.data.CurrentSalvageStation = ResearchStation.Name;
        AssertEqual(Station, ShipBuilder.PickSalvageStation(Player), "as does a design that is no salvage station");
        Player.data.CurrentSalvageStation = roomier.Name;
        Player.RemoveBuildableShip(roomier);
        AssertEqual(Station, ShipBuilder.PickSalvageStation(Player), "and one the player can no longer build");

        Player.AddBuildableShip(roomier);
        Player.data.CurrentSalvageStation = "";
        Player.RemoveBuildableShip(Station);
        AssertEqual(roomier, ShipBuilder.PickSalvageStation(Player), "a default the player cannot build gives way to the best pick");
    }

    [TestMethod]
    public void NeitherPickerTakesAStationWithLessCargoRoomThanALoadForEachOfItsSalvageUnits()
    {
        ShipDesign holdless = Variant("Holdless Station", dropOneSalvageUnit: false, extraHolds: 0, dropHolds: true);
        ShipDesign fewer = Variant("Fewer Units Station", dropOneSalvageUnit: true, extraHolds: 9);
        AssertLessThan(holdless.BaseCargoSpace, ShipResupply.SalvageCargoNeeded(holdless), "setup: a station without room for a load for each of its units");
        AssertGreaterThan(holdless.NumSalvageModules, fewer.NumSalvageModules, "setup: with more salvage units than the other");
        Player.ClearShipsWeCanBuild();
        Player.AddBuildableShip(holdless);
        Player.AddBuildableShip(fewer);

        Player.AutoPickBestSalvageStation = true;
        AssertEqual(fewer, ShipBuilder.PickSalvageStation(Player), "auto pick passes over a station without room for a load a unit");
        Player.AutoPickBestSalvageStation = false;
        Player.data.CurrentSalvageStation = holdless.Name;
        Player.data.DefaultSalvageStation = holdless.Name;
        AssertEqual(fewer, ShipBuilder.PickSalvageStation(Player), "and so does the player's pick, chosen station and race default alike");

        Player.ClearShipsWeCanBuild();
        Player.AddBuildableShip(holdless);
        Assert.IsNull(ShipBuilder.PickSalvageStation(Player), "none when no station has room for a load a unit");
    }

    [TestMethod]
    public void TheDerelictsWindowPausesTheGameAndReadsTheGoalsAsItOpensAndOnceAClickHasReachedTheSimThread()
    {
        Forms(Player, InSystem);
        Derelict d = UState.Derelicts.Current.Last();
        UState.Paused = false;
        var window = new SystemDerelictsScreen(Universe, WreckSystem);
        window.LoadContent();
        Assert.IsTrue(UState.Paused, "the window pauses the game");
        AssertEqual(SalvageDerelict.Stage.None, window.StageShown(d), "setup: nobody salvages the derelict");

        Assert.IsTrue(d.TryClaim(Enemy), "setup: a rival claims the derelict");
        window.Update(0f);
        AssertEqual(SalvageDerelict.Stage.None, window.StageShown(d), "the window does not read the goals again while nothing in it is clicked");

        // as a row's Salvage or Recover button does after its click
        typeof(SystemDerelictsScreen).GetMethod("ReadAfterClick", BindingFlags.Instance | BindingFlags.NonPublic)!.Invoke(window, null);
        window.Update(0f);
        AssertEqual(SalvageDerelict.Stage.None, window.StageShown(d), "nor before the click reaches the sim thread");
        Universe.InvokePendingSimThreadActions();
        window.Update(0f);
        AssertEqual(SalvageDerelict.Stage.Salvaging, window.StageShown(d), "but reads them once it has");
    }

    [TestMethod]
    public void ABuiltStationCostsTheBuilderTrustWithAnAIThatHasAColonyInItsSystem()
    {
        Forms(Player, InSystem);
        WreckSystem.PlanetList[0].SetOwner(Enemy);
        Assert.IsTrue(WreckSystem.HasPlanetsOwnedBy(Enemy) && Enemy.IsKnown(Player), "setup: an AI with a colony there, that knows the player");
        Enemy.GetRelations(Player).AtWar = Player.GetRelations(Enemy).AtWar = false;
        Relationship aiToPlayer = Enemy.GetRelations(Player);
        var order = new SalvageOps(Player, WreckSystem, BuildPos, Station);
        Player.AI.AddGoalAndEvaluate(order);
        float trust = aiToPlayer.Trust;

        order.TargetShip = SpawnShip(Station.Name, Player, BuildPos);
        order.Evaluate();
        AssertEqual("Salvage", order.StepName, "setup: the station is built");
        AssertEqual(5f, trust - aiToPlayer.Trust, "the AI trusts the player 5 less, its personality's default at Normal difficulty");

        order.Evaluate();
        AssertEqual(5f, trust - aiToPlayer.Trust, "once for each station");
    }

    [TestMethod]
    public void OnlyAnAIThatKnowsTheBuilderWithNoOpenBordersAllianceOrWarLosesTrust()
    {
        Assert.IsTrue(ResourceManager.DiplomaticTraits.DiplomaticTraitsList.All(t => t.SalvageStationTrustLoss == 5f),
                      "every personality loses 5, as none sets its own");
        CreateThirdMajorEmpire();
        WreckSystem.PlanetList[0].SetOwner(Enemy);
        Relationship aiToPlayer = Enemy.GetRelations(Player), aiToAI = Enemy.GetRelations(ThirdMajor);
        aiToPlayer.AtWar = Player.GetRelations(Enemy).AtWar = false;
        aiToAI.AtWar = ThirdMajor.GetRelations(Enemy).AtWar = false;
        float Loss(Empire builder)
        {
            Relationship rel = Enemy.GetRelations(builder);
            float before = rel.Trust;
            SalvageMission.CostTrustOfColonyOwners(builder, WreckSystem);
            return before - rel.Trust;
        }

        UState.P.Difficulty = GameDifficulty.Insane;
        Enemy.TestInitModifiers();
        AssertEqual(5f * 1.75f, Loss(Player), "the player loses more at a harder difficulty");
        AssertEqual(5f, Loss(ThirdMajor), "another AI does not");

        Empire stranger = UState.CreateEmpire(ResourceManager.MajorRaces.First(e => UState.GetEmpireByName(e.Name) == null), isPlayer: false);
        Enemy.GetRelations(stranger).AtWar = false;
        Assert.IsFalse(Enemy.IsKnown(stranger), "setup: an empire the AI has not met");
        AssertEqual(0f, Loss(stranger), "none from an empire the AI has not met");

        aiToPlayer.Treaty_OpenBorders = true;
        AssertEqual(0f, Loss(Player), "none with open borders");
        aiToPlayer.Treaty_OpenBorders = false;
        aiToPlayer.Treaty_Alliance = true;
        AssertEqual(0f, Loss(Player), "nor with an alliance");
        aiToPlayer.Treaty_Alliance = false;
        aiToPlayer.AtWar = true;
        AssertEqual(0f, Loss(Player), "nor at war");
        aiToPlayer.AtWar = false;

        GlobalStats.RestrictAIPlayerInteraction = true;
        try
        {
            AssertEqual(0f, Loss(Player), "nor with the AI kept away from the player");
        }
        finally
        {
            GlobalStats.RestrictAIPlayerInteraction = false;
        }

        WreckSystem.PlanetList[0].SetOwner(ThirdMajor);
        AssertEqual(0f, Loss(Player), "and none once the AI has no colony there");

        WreckSystem.PlanetList[0].SetOwner(Player);
        Relationship playerToAI = Player.GetRelations(ThirdMajor);
        float playerTrust = playerToAI.Trust;
        SalvageMission.CostTrustOfColonyOwners(ThirdMajor, WreckSystem);
        AssertEqual(playerTrust, playerToAI.Trust, "and a player's colony costs nothing, as the player keeps no trust");
    }

    [TestMethod]
    public void InALoadedGameAnAIBuilderPassesOverItsOwnColonyAndATrustLossOf0Holds()
    {
        CreateThirdMajorEmpire();
        WreckSystem.PlanetList[0].SetOwner(ThirdMajor);
        Enemy.TestSetPersonality("Cunning");
        Enemy.data.DiplomaticPersonality.SalvageStationTrustLoss = 0; // as a mod might set it
        UState.StarDate = 1001f; // the loader needs a stardate past 1000
        SavedGame save = Universe.Save("UnitTest.SalvageTrust", throwOnError: true);
        UniverseScreen loaded = LoadGame.Load(save.SaveFile, noErrorDialogs: true, startSimThread: false);
        Assert.IsNotNull(loaded, "setup: the save loads");

        Empire builder = loaded.UState.GetEmpireByName(ThirdMajor.Name);
        SolarSystem system = loaded.UState.Systems.First(s => s.Position == WreckSystem.Position);
        Assert.IsTrue(builder.IsKnown(builder) && system.HasPlanetsOwnedBy(builder), "setup: a loaded empire knows itself, and has a colony there");
        SalvageMission.CostTrustOfColonyOwners(builder, system); // it has no relationship with itself to ask about
        AssertEqual(0f, loaded.UState.GetEmpireByName(Enemy.Name).data.DiplomaticPersonality.SalvageStationTrustLoss,
                    "and a personality's loss of 0 survives the save");
    }

    SalvageOps BuiltStation(Empire owner, Vector2 at)
    {
        var order = new SalvageOps(owner, WreckSystem, at, Station);
        owner.AI.AddGoal(order);
        order.AdvanceToNextStep(); // past the queueing, which needs a port
        order.TargetShip = SpawnShip(Station.Name, owner, at);
        order.Evaluate();
        AssertEqual("Salvage", order.StepName, "setup: the station is built");
        return order;
    }

    void Board(Ship station, Empire boarder)
    {
        station.KillAllTroops();
        ResourceManager.CreateTroop("Wyvern", boarder).LandOnShip(station);
        station.LoyaltyChangeFromBoarding(boarder, addNotification: false);
        RunObjectsSim(TestSimStep);
        AssertEqual(boarder, station.Loyalty, "setup: the boarder holds the station");
    }

    NotificationManager TrackNotifications()
    {
        Universe.NotificationManager = new NotificationManager(Universe.ScreenManager, Universe);
        return Universe.NotificationManager;
    }

    Goal StationGoalOf(Empire empire) => empire.AI.FindGoal(g => g.IsSalvageOpsGoal(WreckSystem));

    [TestMethod]
    public void ABoardedStationWorksForItsBoarderWithNoBuiltNoticeNorTrustCost()
    {
        CreateThirdMajorEmpire();
        Forms(Enemy, InSystem);
        SalvageOps theirs = BuiltStation(Enemy, BuildPos);
        Ship station = theirs.TargetShip;
        WreckSystem.PlanetList[0].SetOwner(ThirdMajor);
        ThirdMajor.GetRelations(Player).AtWar = Player.GetRelations(ThirdMajor).AtWar = false;
        Relationship aiToPlayer = ThirdMajor.GetRelations(Player);
        float trust = aiToPlayer.Trust;
        NotificationManager notifications = TrackNotifications();

        Board(station, Player);
        Assert.IsFalse(Enemy.AI.HasGoal(g => g == theirs), "the old owner's order ends as the station changes hands");
        Assert.IsTrue(SalvageOps.CanOrderIn(Enemy, WreckSystem), "so it can order another there");
        Goal ours = StationGoalOf(Player);
        AssertEqual(station, ours?.TargetShip, "the station works for its boarder");
        AssertEqual("Salvage", ours.StepName, "past the build");
        AssertEqual(trust, aiToPlayer.Trust, "taking one over costs no trust, which is for building one");
        AssertEqual(0, notifications.NumberOfNotifications, "and tells of no station built");

        RunObjectsSim(TestSimStep); // placed in its system, the station shows the boarder the derelict there
        ours.Evaluate();
        Assert.IsTrue(Player.AI.HasGoal(g => g == ours), "the boarder keeps it");
    }

    [TestMethod]
    public void AStationTakenOverIsNotScrappedBeforeItsNewOwnerLearnsOfTheDerelictsThere()
    {
        Forms(Enemy, InSystem);
        Derelict d = UState.Derelicts.Current.Last();
        Ship station = BuiltStation(Enemy, BuildPos).TargetShip;
        Settle(station);
        Assert.IsFalse(d.IsKnownBy(Player), "setup: the boarder knows of no derelict there");

        Board(station, Player);
        Assert.IsTrue(station.Active && d.IsKnownBy(Player), "the empty station is not scrapped as it changes hands, which shows the boarder the derelict there");
        StationGoalOf(Player).Evaluate();
        Assert.IsTrue(station.Active, "and with it to salvage, the station works for the boarder");
    }

    [TestMethod]
    public void AStationTakenOverBeforeItIsPlacedInItsSystemIsNotScrappedUntilItIs()
    {
        Forms(Enemy, InSystem);
        Derelict d = UState.Derelicts.Current.Last();
        Ship station = BuiltStation(Enemy, BuildPos).TargetShip;
        Board(station, Player);
        Assert.IsTrue(station.System == null && !d.IsKnownBy(Player), "setup: it changed hands before it was placed in its system, the boarder knowing of no derelict there");
        StationGoalOf(Player).Evaluate();
        Assert.IsTrue(station.Active, "its order's turn does not scrap the empty station");

        RunObjectsSim(TestSimStep);
        Assert.IsTrue(d.IsKnownBy(Player), "setup: once it is placed there, the boarder learns of the derelict");
        StationGoalOf(Player).Evaluate();
        Assert.IsTrue(station.Active, "and with it to salvage, the station works for the boarder");
    }

    [TestMethod]
    public void ABoardedStationTakesThePlaceOfTheBoardersOrderNotYetBuilt()
    {
        Forms(Enemy, InSystem);
        Forms(Player, InSystem + new Vector2(0, 10_000f));
        Ship station = BuiltStation(Enemy, BuildPos).TargetShip;
        var order = new SalvageOps(Player, WreckSystem, BuildPos + new Vector2(0, -10_000f), Station);
        Player.AI.AddGoalAndEvaluate(order);
        Assert.IsTrue(Player.AI.HasGoal(g => g.IsBuildingOrbitalFor(WreckSystem)), "setup: the player's station is queued");

        Board(station, Player);
        Assert.IsFalse(Player.AI.HasGoal(g => g == order), "the player's order is dropped");
        Assert.IsFalse(Player.AI.HasGoal(g => g.IsBuildingOrbitalFor(WreckSystem)), "with its construction");
        AssertEqual(station, StationGoalOf(Player)?.TargetShip, "for the station taken");
        Assert.IsTrue(station.Active, "which stays");
    }

    [TestMethod]
    public void ABoardedStationIsEvacuatedWhereTheBoarderHasOneItsTroopsLeavingFirst()
    {
        Forms(Enemy, InSystem);
        Forms(Player, InSystem + new Vector2(0, 10_000f));
        SalvageOps theirs = BuiltStation(Enemy, BuildPos);
        SalvageOps ours = BuiltStation(Player, BuildPos + new Vector2(0, -10_000f));
        Ship station = theirs.TargetShip;
        NotificationManager notifications = TrackNotifications();

        Board(station, Player);
        Assert.IsFalse(station.Active, "the boarded station is evacuated");
        Assert.IsFalse(Enemy.AI.HasGoal(g => g == theirs), "the old owner's order ends");
        AssertEqual(ours, StationGoalOf(Player), "the player keeps its own station there");
        AssertEqual(1, Player.AI.CountGoals(g => g.IsSalvageOpsGoal(WreckSystem)), "and only that one");
        Assert.IsTrue(notifications.IsNotificationPresent(Localizer.Token(GameText.RemoveExcessSalvageStation)), "and is told");

        RunObjectsSim(TestSimStep);
        Assert.IsFalse(Player.OwnedShips.Contains(station) || UState.Objects.GetShips().Contains(station), "it leaves the player's ships and the universe");
        Ship[] shuttles = UState.Objects.GetShips().Filter(s => s.Loyalty == Player && s.IsDefaultAssaultShuttle);
        AssertEqual(1, shuttles.Length, "the boarder's troop leaves in an assault shuttle first");
        AssertEqual(1, shuttles[0].TroopCount, "with the troop on board");
    }

    [TestMethod]
    public void AStationBoardedBeforeTheOrderOfOneJustLostEndsWorksForTheBoarder()
    {
        Forms(Enemy, InSystem);
        Forms(Player, InSystem + new Vector2(0, 10_000f));
        Ship station = BuiltStation(Enemy, BuildPos).TargetShip;
        SalvageOps lost = BuiltStation(Player, BuildPos + new Vector2(0, -10_000f));
        lost.TargetShip.Die(null, cleanupOnly: true);

        Board(station, Player);
        Assert.IsTrue(station.Active, "a station just lost does not count as one there");
        Assert.IsTrue(Player.AI.HasGoal(g => g.IsSalvageOpsGoal(WreckSystem) && g.TargetShip == station), "so the boarded one works for the player");

        lost.Evaluate();
        AssertEqual(1, Player.AI.CountGoals(g => g.IsSalvageOpsGoal(WreckSystem)), "and is the one left once the lost one's order ends");
    }

    [TestMethod]
    public void PiratesKeepASalvageStationTheyBoard()
    {
        CreateAMinorFaction("Corsairs");
        Empire raiders = Faction;
        Assert.IsTrue(raiders.WeArePirates, "setup: pirates");
        Forms(Player, InSystem);
        Ship station = BuiltStation(Player, BuildPos).TargetShip;

        Board(station, raiders);
        AssertLessThan(station.ScuttleTimer, 0f, "the pirates do not scuttle it");
        AssertEqual(station, StationGoalOf(raiders)?.TargetShip, "it works for them as for any boarder");
        AssertEqual(1, station.TroopCount, "and their troop holds it");
    }

    // two steps of the objects: the first puts new ships in their empires' lists and in the spatial index, from which the second finds their systems
    void Settle(Ship station)
    {
        RunObjectsSim(TestSimStep);
        RunObjectsSim(TestSimStep);
        AssertEqual(WreckSystem, station.System, "setup: the station is in its system");
    }

    [TestMethod]
    public void AnAbsorbedEmpiresStationWorksForTheAbsorberOnceItChangesHands()
    {
        Forms(Enemy, InSystem);
        Forms(Player, InSystem + new Vector2(0, 10_000f));
        Ship station = BuiltStation(Enemy, BuildPos).TargetShip;
        var order = new SalvageOps(Player, WreckSystem, BuildPos + new Vector2(0, -10_000f), Station);
        Player.AI.AddGoalAndEvaluate(order);
        UState.Objects.UpdateLists();
        Assert.IsTrue(Enemy.OwnedShips.Contains(station) && station.System == null, "setup: a station among its empire's ships, not yet placed in its system");

        Player.AbsorbEmpire(Enemy);
        RunObjectsSim(TestSimStep);
        AssertEqual(Player, station.Loyalty, "setup: the station changes hands a step after the merger");
        Assert.IsFalse(Player.AI.HasGoal(g => g == order), "the absorber's order there is dropped");
        Assert.IsFalse(Player.AI.HasGoal(g => g.IsBuildingOrbitalFor(WreckSystem)), "with its construction");
        Goal ours = StationGoalOf(Player);
        AssertEqual(station, ours?.TargetShip, "for the absorbed station");
        AssertEqual("Salvage", ours.StepName, "past the build");
    }

    [TestMethod]
    public void AnAbsorbedEmpiresStationIsRemovedWithItsTroopsWhereTheAbsorberHasOne()
    {
        Forms(Enemy, InSystem);
        Forms(Player, InSystem + new Vector2(0, 10_000f));
        Ship station = BuiltStation(Enemy, BuildPos).TargetShip;
        SalvageOps ours = BuiltStation(Player, BuildPos + new Vector2(0, -10_000f));
        ResourceManager.CreateTroop("Wyvern", Enemy).LandOnShip(station);
        Settle(station);
        NotificationManager notifications = TrackNotifications();

        Player.AbsorbEmpire(Enemy);
        RunObjectsSim(TestSimStep);
        Assert.IsFalse(station.Active, "the absorbed station is removed");
        AssertEqual(ours, StationGoalOf(Player), "the absorber keeps its own");
        AssertEqual(1, Player.AI.CountGoals(g => g.IsSalvageOpsGoal(WreckSystem)), "and only that one");
        Assert.IsFalse(notifications.IsNotificationPresent(Localizer.Token(GameText.RemoveExcessSalvageStation)), "no one is told");

        RunObjectsSim(TestSimStep);
        Assert.IsFalse(Player.OwnedShips.Contains(station) || UState.Objects.GetShips().Contains(station), "it leaves the absorber's ships and the universe");
        AssertEqual(0, UState.Objects.GetShips().Count(s => s.IsDefaultAssaultShuttle), "and no troop leaves it in a shuttle");
    }

    [TestMethod]
    public void AStationBoardedFromAnEmpireThatThenMergesStaysWithItsBoarder()
    {
        CreateThirdMajorEmpire();
        Forms(Enemy, InSystem);
        Forms(Player, InSystem + new Vector2(0, 10_000f));
        Ship station = BuiltStation(Enemy, BuildPos).TargetShip;
        SalvageOps ours = BuiltStation(Player, BuildPos + new Vector2(0, -10_000f));
        Settle(station);

        Board(station, ThirdMajor);
        Player.AbsorbEmpire(Enemy);
        RunObjectsSim(TestSimStep);
        Assert.IsTrue(station.Active && station.Loyalty == ThirdMajor, "the boarder keeps the station");
        AssertEqual(station, StationGoalOf(ThirdMajor)?.TargetShip, "which works for it");
        AssertEqual(ours, StationGoalOf(Player), "and the absorber keeps its own");
    }

    [TestMethod]
    public void ADefeatedEmpiresStationWorksForItsRebels()
    {
        Forms(Enemy, InSystem);
        Ship station = BuiltStation(Enemy, BuildPos).TargetShip;
        Settle(station);

        Enemy.SetAsDefeated();
        RunObjectsSim(TestSimStep);
        Empire rebels = station.Loyalty;
        Assert.IsTrue(station.Active && rebels != Enemy, "setup: the station goes to the defeated empire's rebels");
        AssertEqual(station, StationGoalOf(rebels)?.TargetShip, "and works for them, so whoever boards it takes it over");
    }

    // the player's boarders hold the station, and the change of hands is applied off the sim thread, as a save or the UI does
    void BoardOffTheSimThread(Ship station)
    {
        station.KillAllTroops();
        ResourceManager.CreateTroop("Wyvern", Player).LandOnShip(station);
        station.LoyaltyChangeFromBoarding(Player, addNotification: false);
        FieldInfo simThread = typeof(UniverseScreen).GetField("SimThread", BindingFlags.Instance | BindingFlags.NonPublic);
        simThread.SetValue(Universe, new Thread(() => { }));
        try
        {
            UState.Objects.UpdateLists();
        }
        finally
        {
            simThread.SetValue(Universe, null);
        }
        AssertEqual(Player, station.Loyalty, "setup: the station changes hands");
    }

    [TestMethod]
    public void AChangeOfHandsOffTheSimThreadIsTakenOverOnIt()
    {
        Forms(Enemy, InSystem);
        SalvageOps theirs = BuiltStation(Enemy, BuildPos);
        Ship station = theirs.TargetShip;

        BoardOffTheSimThread(station);
        Assert.IsTrue(Enemy.AI.HasGoal(g => g == theirs) && StationGoalOf(Player) == null, "the takeover waits for the sim thread");

        Universe.InvokePendingSimThreadActions();
        Assert.IsFalse(Enemy.AI.HasGoal(g => g == theirs), "where the old owner's order ends");
        AssertEqual(station, StationGoalOf(Player)?.TargetShip, "and the station works for its boarder");
    }

    [TestMethod]
    public void AStationLostBeforeItsTakeoverReachesTheSimThreadIsNotTakenOver()
    {
        Forms(Enemy, InSystem);
        Forms(Player, InSystem + new Vector2(0, 10_000f));
        Ship station = BuiltStation(Enemy, BuildPos).TargetShip;
        var order = new SalvageOps(Player, WreckSystem, BuildPos + new Vector2(0, -10_000f), Station);
        Player.AI.AddGoalAndEvaluate(order);

        BoardOffTheSimThread(station);
        station.Die(null, cleanupOnly: true);
        Universe.InvokePendingSimThreadActions();
        AssertEqual(order, StationGoalOf(Player), "a station gone by then is not taken over, and the player's order there stands");
        Assert.IsTrue(Player.AI.HasGoal(g => g.IsBuildingOrbitalFor(WreckSystem)), "with its construction");
    }

    [TestMethod]
    public void AQueuedTakeoverOvertakenByAnotherChangeOfHandsDoesNothing()
    {
        CreateThirdMajorEmpire();
        Forms(Enemy, InSystem);
        Forms(Player, InSystem + new Vector2(0, 10_000f));
        Ship station = BuiltStation(Enemy, BuildPos).TargetShip;
        SalvageOps ours = BuiltStation(Player, BuildPos + new Vector2(0, -10_000f));

        BoardOffTheSimThread(station);
        Board(station, ThirdMajor);
        Universe.InvokePendingSimThreadActions();
        Assert.IsTrue(station.Active && station.Loyalty == ThirdMajor, "the player's queued takeover leaves the station its last boarder took");
        AssertEqual(station, StationGoalOf(ThirdMajor)?.TargetShip, "which works for it");
        AssertEqual(ours, StationGoalOf(Player), "and the player keeps its own");
    }

    [TestMethod]
    public void AnOrderWhoseStationChangedHandsWithoutItHandsItOnOnce()
    {
        Forms(Enemy, InSystem);
        SalvageOps theirs = BuiltStation(Enemy, BuildPos);
        Ship station = theirs.TargetShip;

        BoardOffTheSimThread(station);
        theirs.Evaluate();
        Assert.IsFalse(Enemy.AI.HasGoal(g => g == theirs), "an order that finds its station in other hands ends");
        Goal ours = StationGoalOf(Player);
        AssertEqual(station, ours?.TargetShip, "handing it to its new owner");

        Universe.InvokePendingSimThreadActions();
        Assert.IsTrue(station.Active, "the queued takeover, overtaken, does not evacuate the station");
        AssertEqual(1, Player.AI.CountGoals(g => g.IsSalvageOpsGoal(WreckSystem)), "nor gives it a second order");
        AssertEqual(ours, StationGoalOf(Player), "the first one holds it");
    }

    // as the game saves: started on the sim thread, written by a worker, then loaded
    UniverseScreen SaveAsTheGameDoesAndLoad(string name)
    {
        FieldInfo simThread = typeof(UniverseScreen).GetField("SimThread", BindingFlags.Instance | BindingFlags.NonPublic);
        simThread.SetValue(Universe, Thread.CurrentThread);
        try
        {
            UState.StarDate = 1001f; // the loader needs a stardate past 1000
            Universe.SaveAsync(name);
            for (int waited = 0; Universe.IsSaving && waited < 30_000; waited += 10)
                Thread.Sleep(10);
            Assert.IsFalse(Universe.IsSaving, "setup: the save is written");
        }
        finally
        {
            simThread.SetValue(Universe, null);
        }
        Universe.InvokePendingSimThreadActions();
        UniverseScreen loaded = LoadGame.Load(new FileInfo($"{SavedGame.DefaultSaveGameFolder}{name}.sav"), noErrorDialogs: true, startSimThread: false);
        Assert.IsNotNull(loaded, "setup: the save loads");
        return loaded;
    }

    [TestMethod]
    public void AMergedEmpiresStationStillChangingHandsWhenASaveStartsLoadsWithItsOrder()
    {
        Forms(Enemy, InSystem);
        Ship station = BuiltStation(Enemy, BuildPos).TargetShip;
        Settle(station);

        Player.AbsorbEmpire(Enemy);
        UniverseScreen loaded = SaveAsTheGameDoesAndLoad("UnitTest.SalvageMergerSave");
        Empire player = loaded.UState.Player;
        Goal ours = player.AI.FindGoal(g => g is SalvageOps);
        Assert.IsTrue(ours?.TargetShip is { Active: true, IsSalvageStation: true } && ours.TargetShip.Loyalty == player,
                      "the station the merger brought loads with the absorber's order for it");
    }

    [TestMethod]
    public void AStationBoardedWhereTheBoarderHasOneAsASaveStartsIsEvacuatedBeforeItIsWritten()
    {
        Forms(Enemy, InSystem);
        Forms(Player, InSystem + new Vector2(0, 10_000f));
        Ship station = BuiltStation(Enemy, BuildPos).TargetShip;
        BuiltStation(Player, BuildPos + new Vector2(0, -10_000f));
        Settle(station);
        station.KillAllTroops();
        ResourceManager.CreateTroop("Wyvern", Player).LandOnShip(station);
        station.LoyaltyChangeFromBoarding(Player, addNotification: false);

        UniverseScreen loaded = SaveAsTheGameDoesAndLoad("UnitTest.SalvageBoardingSave");
        Empire player = loaded.UState.Player;
        Ship[] ships = loaded.UState.Objects.GetShips();
        AssertEqual(1, ships.Count(s => s.IsSalvageStation && s.Loyalty == player), "the boarded station was evacuated before the save");
        AssertEqual(1, player.AI.CountGoals(g => g is SalvageOps), "the player's own station keeps the one order");
        Assert.IsTrue(ships.Any(s => s.Loyalty == player && s.IsDefaultAssaultShuttle && s.TroopCount == 1), "and the boarder's troop left it in a shuttle");
    }

    [TestMethod]
    public void AStationTakenOverSurvivesSaveAndLoad()
    {
        Forms(Enemy, InSystem);
        Ship station = BuiltStation(Enemy, BuildPos).TargetShip;
        Settle(station); // so the boarder learns of the derelict there as it takes the station
        Board(station, Player);

        UState.StarDate = 1001f; // the loader needs a stardate past 1000
        SavedGame save = Universe.Save("UnitTest.SalvageTakeOver", throwOnError: true);
        UniverseScreen loaded = LoadGame.Load(save.SaveFile, noErrorDialogs: true, startSimThread: false);
        Assert.IsNotNull(loaded, "setup: the save loads");

        Empire player = loaded.UState.Player;
        var ours = (SalvageOps)player.AI.FindGoal(g => g is SalvageOps);
        AssertEqual(WreckSystem.Name, ours?.TargetSystem?.Name, "the station's order loads with its system");
        AssertEqual("Salvage", ours.StepName, "past the build");
        Assert.IsTrue(ours.TargetShip is { Active: true, IsSalvageStation: true } && ours.TargetShip.Loyalty == player, "holding the station");

        ours.Evaluate();
        Assert.IsTrue(player.AI.HasGoal(g => g == ours), "and keeps it in the loaded game");
    }

    // the basic station, less one salvage unit or every cargo hold if asked, plus medium cargo holds in a strip below its grid
    ShipDesign Variant(string name, bool dropOneSalvageUnit, int extraHolds, bool dropHolds = false)
    {
        ShipModule hold = ResourceManager.GetModuleTemplate("MediumCargoHold");
        ShipDesign design = ((ShipDesign)Station).GetClone(name);
        var slots = new Array<DesignSlot>(Station.GetOrLoadDesignSlots());
        if (dropOneSalvageUnit)
            slots.Remove(slots.First(s => ResourceManager.GetModuleTemplate(s.ModuleUID).Salvage));
        if (dropHolds)
            slots.RemoveAll(s => ResourceManager.GetModuleTemplate(s.ModuleUID) is { CargoCapacity: > 0f, Salvage: false });

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
