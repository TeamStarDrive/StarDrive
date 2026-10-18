using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Ship_Game;
using Ship_Game.Commands.Goals;
using Ship_Game.GameScreens.LoadGame;
using Ship_Game.Ships;
using Block = Ship_Game.Commands.Goals.SalvageDerelict.Block;
using Vector2 = SDGraphics.Vector2;

namespace UnitTests.Ships;

/// <summary>
/// The turn after its hold fills, a salvage platform sends its whole load to its station instead of stripping, as
/// much as the station has room for; the rest stays aboard, and it strips again once its hold has room. With no room
/// in the station the load waits, and the platform's status says so. The load's shuttles, one per 10 production and
/// at most 20, fly one way from the platform to the station at 600 a second, and launch only where the player sees
/// either end, and none launches once its platform is gone. Once its derelict is stripped empty, or the Salvage
/// button's second click calls the order off, the platform sends its station all it holds, full or not, then takes
/// itself apart over two turns, shown as a build from start to end, and is gone, freeing its unit; it waits to come
/// apart until the station has taken all of it.
/// The click gives up the claim at once, so the derelict can be ordered again while the old platform comes apart, a
/// rival's claim included, and the old order's end leaves a new order's claim alone. A platform is lost with its
/// station, at work or coming apart; one boarded while it comes apart stays whole with its boarder. A called-off
/// order coming apart survives a save.
/// </summary>
[TestClass]
public class SalvageLoadTests : StarDriveTest
{
    static readonly Vector2 SystemSite = new(600_000f, 600_000f);
    static readonly Vector2 Wreck = SystemSite + new Vector2(30_000f, 0);
    static readonly Vector2 PlayerStation = SystemSite + new Vector2(33_000f, 0);
    readonly SolarSystem WreckSystem;
    readonly IShipDesign Station;

    public SalvageLoadTests()
    {
        LoadStarterShips("Dreadnought mk1-a", "Terran Basic Salvage Station", ShipDesign.SalvagePlatformName);
        CreateUniverseAndPlayerEmpire();
        UState.Objects.EnableParallelUpdate = false;
        WreckSystem = AddDummyPlanet(SystemSite, 1f, 1f, 1f, SystemSite + new Vector2(0, 60_000f), explored: true).System;
        AddHomeWorldToEmpire(new Vector2(200_000f), Player);
        Assert.IsTrue(ResourceManager.Ships.GetDesign("Terran Basic Salvage Station", out Station), "setup: the station is loaded");
        Player.data.CurrentConstructor = "Terran Constructor";
    }

    [TestCleanup]
    public override void Cleanup()
    {
        UState.Derelicts.Clear();
        base.Cleanup();
    }

    // a salvageable derelict of the player's, so the player knows of it; the ship itself is gone
    Derelict Forms(Vector2 at)
    {
        TestShip ship = SpawnShip("Dreadnought mk1-a", Player, at);
        Assert.IsTrue(UState.Derelicts.TryAdd(ship, salvageable: true, seen: false), "setup: a salvageable derelict");
        Universe.InvokePendingSimThreadActions();
        ship.QueueTotalRemoval();
        return UState.Derelicts.Current.Last();
    }

    Ship BuiltStation()
    {
        var order = new SalvageOps(Player, WreckSystem, PlayerStation, Station);
        Player.AI.AddGoal(order);
        order.AdvanceToNextStep(); // past the queueing, which needs a port
        order.TargetShip = SpawnShip(Station.Name, Player, PlayerStation);
        order.Evaluate();
        AssertEqual("Salvage", order.StepName, "setup: the station is built");
        return order.TargetShip;
    }

    // the player's order with its platform built beside the derelict, as its constructor leaves it, about to strip
    SalvageDerelict AtWork(Derelict d, float hold)
    {
        Assert.IsTrue(SalvageDerelict.Order(Player, d), "setup: ordered");
        SalvageDerelict order = SalvageDerelict.OrderOf(Player, d);
        Ship constructor = order.FinishedShip;
        Assert.IsNotNull(constructor, "setup: a constructor launches");
        Assert.IsTrue(d.TryClaim(Player), "setup: the constructor claims the derelict as it starts to build");

        Ship platform = SpawnShip(ShipDesign.SalvagePlatformName, Player, order.BuildPosition);
        platform.CargoSpaceMax = hold;
        constructor.QueueTotalRemoval();
        order.OnPlatformBuilt(platform, constructor);
        order.Evaluate();
        AssertEqual("Salvage", order.StepName, "setup: the platform is built");
        return order;
    }

    // an earlier claim of the enemy's stripped all but 5
    void AllBut5Stripped(Derelict d) => UState.Derelicts.Strip(d, Enemy, null, d.ProductionLeft - 5f);

    static string Status(SalvageDerelict order) => ShipInfoUIElement.SalvageStatus(order.Platform, order);

    [TestMethod]
    public void AFullHoldGoesToTheStationTheTurnAfterItFills()
    {
        Derelict d = Forms(Wreck);
        Ship station = BuiltStation();
        SalvageDerelict order = AtWork(d, hold: 24f);
        Ship platform = order.Platform;
        order.Evaluate();
        order.Evaluate();
        Assert.IsTrue(SalvageDerelict.HoldFull(platform), "setup: two turns fill its hold");
        float left = d.ProductionLeft;

        order.Evaluate();
        AssertEqual(0.01f, 24f, station.GetProduction(), "the turn after, its whole load goes to the station");
        AssertEqual(0f, platform.GetProduction(), "leaving its hold empty");
        AssertEqual(0.01f, left, d.ProductionLeft, "and it strips nothing that turn");

        order.Evaluate();
        AssertEqual(0.01f, 12f, platform.GetProduction(), "then it strips again");
    }

    [TestMethod]
    public void AStationTakesWhatFitsAndWithNoRoomTheLoadWaitsAboard()
    {
        Derelict d = Forms(Wreck);
        Ship station = BuiltStation();
        station.LoadProduction(station.CargoRoom - 10f);
        float stored = station.GetProduction();
        SalvageDerelict order = AtWork(d, hold: 24f);
        Ship platform = order.Platform;
        order.Evaluate();
        order.Evaluate();
        Assert.IsTrue(SalvageDerelict.HoldFull(platform), "setup: two turns fill its hold");

        order.Evaluate();
        AssertEqual(0.01f, stored + 10f, station.GetProduction(), "the station takes the 10 it has room for");
        AssertEqual(0.01f, 14f, platform.GetProduction(), "and the rest stays aboard");

        order.Evaluate();
        AssertEqual(0.01f, 24f, platform.GetProduction(), "the platform strips again into the room it has");
        Assert.IsTrue(SalvageDerelict.HoldFull(platform) && SalvageDerelict.HoldFull(station), "setup: both holds are full");

        order.Evaluate();
        AssertEqual(0.01f, 24f, platform.GetProduction(), "with no room in the station its load waits");
        AssertEqual(Localizer.Token(GameText.SalvageStationHoldFull), Status(order), "and its status says so");
    }

    [TestMethod]
    public void ADamagedStationTakesOnlyWhatItsCargoRoomHolds()
    {
        Derelict d = Forms(Wreck);
        Ship station = BuiltStation();
        station.LoadProduction(0f); // the hold takes its limit from the cargo room of the moment, before a cargo module is lost
        station.CargoSpaceMax = 10f;
        SalvageDerelict order = AtWork(d, hold: 24f);
        order.Evaluate();
        order.Evaluate();
        Assert.IsTrue(SalvageDerelict.HoldFull(order.Platform), "setup: two turns fill its hold");

        order.Evaluate();
        AssertEqual(0.01f, 10f, station.GetProduction(), "the station takes only the 10 its cargo room holds");
        AssertEqual(0.01f, 14f, order.Platform.GetProduction(), "and the rest stays aboard");
    }

    [TestMethod]
    public void TheLoadsShuttlesFlyOneWayToTheStationOnePerTenProduction()
    {
        AssertEqual(3, CargoShuttles.LoadShuttlesFor(24f), "a load of 24 takes 3 shuttles, rounded up");
        AssertEqual(1, CargoShuttles.LoadShuttlesFor(10f), "10 takes one");
        AssertEqual(1, CargoShuttles.LoadShuttlesFor(0.5f), "and so does any amount");
        AssertEqual(20, CargoShuttles.LoadShuttlesFor(1000f), "at most 20");

        Derelict d = Forms(Wreck);
        Ship station = BuiltStation();
        SalvageDerelict order = AtWork(d, hold: 24f);
        Ship platform = order.Platform;
        UState.ViewState = UniverseScreen.UnivScreenState.PlanetView;
        RunSimWhile((simTimeout: 5, fatal: true), () => !platform.InPlayerSensorRange || !station.InPlayerSensorRange);
        platform.InFrustum = station.InFrustum = false;

        order.Evaluate();
        order.Evaluate();
        order.Evaluate();
        Universe.CargoShuttles.Update(Universe, TestSimStep);
        AssertEqual(0.01f, 24f, station.GetProduction(), "setup: a load went to the station");
        AssertEqual(0, Universe.CargoShuttles.InFlight(platform, station), "with no shuttles where the player sees neither end");

        order.Evaluate();
        order.Evaluate();
        station.InFrustum = true;
        order.Evaluate();
        Universe.CargoShuttles.Update(Universe, TestSimStep);
        AssertEqual(3, Universe.CargoShuttles.InFlight(platform, station), "a load of 24 sends 3 shuttles where the player sees the station");

        // a flight takes 0.42 to 0.6 of its route's time, launched within 0.35 of it, the route timed for 600 a second on average
        double seconds = platform.Position.Distance(station.Position) / (600.0 * 0.51);
        AssertGreaterThan(seconds, 2.0, "setup: a trip of a few seconds");
        double time = TestSimStepD;
        for (; time < seconds * 0.42 - 0.05; time += TestSimStepD)
            Universe.CargoShuttles.Update(Universe, TestSimStep);
        AssertEqual(3, Universe.CargoShuttles.InFlight(platform, station), "none is there before the quickest flight");
        for (; time < seconds * 0.95 + 0.05; time += TestSimStepD)
            Universe.CargoShuttles.Update(Universe, TestSimStep);
        AssertEqual(0, Universe.CargoShuttles.InFlight(platform, station), "and all are there by the latest launch and the slowest flight, none coming back");
    }

    [TestMethod]
    public void ALoadsShuttlesNeverLaunchOnceTheirPlatformIsGone()
    {
        Derelict d = Forms(Wreck);
        Ship station = BuiltStation();
        SalvageDerelict order = AtWork(d, hold: 24f);
        Ship platform = order.Platform;
        UState.ViewState = UniverseScreen.UnivScreenState.PlanetView;
        RunSimWhile((simTimeout: 5, fatal: true), () => !platform.InPlayerSensorRange || !station.InPlayerSensorRange);
        platform.InFrustum = false;
        station.InFrustum = true;
        order.Evaluate();
        order.Evaluate();
        order.Evaluate();
        AssertEqual(0.01f, 24f, station.GetProduction(), "setup: a load went to the station");

        platform.QueueTotalRemoval();
        double seconds = platform.Position.Distance(station.Position) / (600.0 * 0.51);
        for (double time = 0.0; time < seconds * 0.36; time += TestSimStepD)
            Universe.CargoShuttles.Update(Universe, TestSimStep);
        AssertEqual(0, Universe.CargoShuttles.InFlight(platform, station), "its shuttles, all due to launch by now, never did");
    }

    [TestMethod]
    public void AnEmptiedDerelictsPlatformSendsAllItHoldsThenTakesItselfApartOverTwoTurns()
    {
        Derelict d = Forms(Wreck);
        Derelict other = Forms(Wreck + new Vector2(0, 5_000f));
        Derelict next = Forms(Wreck + new Vector2(0, -5_000f));
        Ship station = BuiltStation();
        SalvageDerelict order = AtWork(d, hold: 100f);
        Ship platform = order.Platform;
        Assert.IsTrue(SalvageDerelict.Order(Player, other), "setup: the station's other unit works another derelict");
        AssertEqual(Block.NoFreeUnit, SalvageDerelict.WhyNoOrder(Player, next), "setup: both units are busy");
        AllBut5Stripped(d);

        order.Evaluate();
        Assert.IsTrue(order.Emptied && order.Finished, "setup: the platform strips the 5 left");
        AssertEqual(Localizer.Token(GameText.SalvageFinished), Status(order), "with its load aboard its salvage is finished");

        order.Evaluate();
        AssertEqual(0.01f, 5f, station.GetProduction(), "the next turn it sends its station all it holds, though its hold is not full");
        Assert.IsTrue(order.Dismantling && platform.Dismantling, "and starts to take itself apart");
        AssertEqual(Localizer.Token(GameText.PlatformDismantling), Status(order), "as its status says");
        AssertEqual(0f, platform.DismantlePercent, "shown as a build that starts");
        AssertEqual(Block.NoFreeUnit, SalvageDerelict.WhyNoOrder(Player, next), "its unit still busy");

        RunObjectsSim(UState.P.TurnTimer);
        AssertEqual(2f, 50f, platform.DismantlePercent, "half way after a turn");
        order.Evaluate();
        Assert.IsTrue(platform.Active, "it stands through the first turn");

        RunObjectsSim(UState.P.TurnTimer);
        AssertEqual(2f, 100f, platform.DismantlePercent, "and complete after two");
        order.Evaluate();
        Assert.IsFalse(platform.Active, "then it is gone");
        Assert.IsFalse(Player.AI.HasGoal(order), "its order done");
        AssertEqual(Block.None, SalvageDerelict.WhyNoOrder(Player, next), "and its unit free");
    }

    [TestMethod]
    public void AFinishedPlatformComesApartOnlyOnceItsStationTookAllItHolds()
    {
        Derelict d = Forms(Wreck);
        Ship station = BuiltStation();
        station.LoadProduction(station.CargoRoom - 2f);
        SalvageDerelict order = AtWork(d, hold: 100f);
        AllBut5Stripped(d);
        order.Evaluate();
        Assert.IsTrue(order.Emptied, "setup: the platform strips the 5 left");

        order.Evaluate();
        AssertEqual(0.01f, 3f, order.Platform.GetProduction(), "the station takes the 2 it has room for");
        Assert.IsFalse(order.Dismantling || order.Platform.Dismantling, "so the platform keeps the rest and stands");
        AssertEqual(Localizer.Token(GameText.SalvageStationHoldFull), Status(order), "its status saying why");

        station.UnloadProduction(10f);
        order.Evaluate();
        AssertEqual(0f, order.Platform.GetProduction(), "once the station has room it takes the rest");
        Assert.IsTrue(order.Dismantling, "and the platform comes apart");
    }

    [TestMethod]
    public void ACalledOffPlatformFinishesThoughARivalClaimsTheDerelict()
    {
        Derelict d = Forms(Wreck);
        Ship station = BuiltStation();
        SalvageDerelict order = AtWork(d, hold: 100f);
        order.Evaluate();
        SalvageDerelict.Cancel(Player, d);
        Assert.IsTrue(d.TryClaim(Enemy), "setup: a rival claims the derelict once the claim is given up");

        order.Evaluate();
        Assert.IsTrue(Player.AI.HasGoal(order), "the called-off order stands on");
        AssertEqual(0.01f, 12f, station.GetProduction(), "its platform sends what it holds");
        Assert.IsTrue(order.Dismantling, "and comes apart");
        AssertEqual(Enemy, d.Claimant, "leaving the rival's claim");
    }

    [TestMethod]
    public void APlatformBoardedWhileComingApartStaysWholeWithItsBoarder()
    {
        Derelict d = Forms(Wreck);
        BuiltStation();
        SalvageDerelict order = AtWork(d, hold: 100f);
        Ship platform = order.Platform;
        AllBut5Stripped(d);
        order.Evaluate();
        order.Evaluate();
        RunObjectsSim(UState.P.TurnTimer);
        Assert.IsTrue(platform.Dismantling && platform.DismantlePercent > 0f, "setup: the platform comes apart");

        platform.KillAllTroops();
        ResourceManager.CreateTroop("Wyvern", Enemy).LandOnShip(platform);
        platform.LoyaltyChangeFromBoarding(Enemy, addNotification: false);
        UState.Objects.UpdateLists();
        AssertEqual(Enemy, platform.Loyalty, "setup: the platform is boarded");

        order.Evaluate();
        Assert.IsFalse(Player.AI.HasGoal(order), "its order ends");
        Assert.IsTrue(platform.Active, "the boarders keep the platform");
        Assert.IsFalse(platform.Dismantling, "which no longer comes apart");
        AssertEqual(0f, platform.DismantlePercent, "its dismantling undone");
    }

    [TestMethod]
    public void APlatformIsLostWithItsStationAtWorkOrComingApart()
    {
        Derelict d = Forms(Wreck);
        Derelict emptied = Forms(Wreck + new Vector2(0, 5_000f));
        Ship station = BuiltStation();
        SalvageDerelict working = AtWork(d, hold: 100f);
        working.Evaluate();
        SalvageDerelict apart = AtWork(emptied, hold: 100f);
        AllBut5Stripped(emptied);
        apart.Evaluate();
        apart.Evaluate();
        Assert.IsTrue(apart.Dismantling, "setup: one platform comes apart");
        AssertEqual(0.01f, 12f, working.Platform.GetProduction(), "setup: while the other holds a load");

        station.QueueTotalRemoval();
        working.Evaluate();
        apart.Evaluate();
        Assert.IsFalse(working.Platform.Active, "a platform at work is gone with its station, its load with it");
        Assert.IsFalse(Player.AI.HasGoal(working), "its order ends");
        Assert.IsNull(d.Claimant, "with its claim");
        Assert.IsFalse(apart.Platform.Active, "and so is one coming apart");
        Assert.IsFalse(Player.AI.HasGoal(apart), "with its order");
    }

    [TestMethod]
    public void TheSecondClickSendsTheLoadThenTakesThePlatformApartWhileTheDerelictCanBeOrderedAgain()
    {
        Derelict d = Forms(Wreck);
        Derelict other = Forms(Wreck + new Vector2(0, 5_000f));
        Ship station = BuiltStation();
        SalvageDerelict order = AtWork(d, hold: 100f);
        order.Evaluate();
        float left = d.ProductionLeft;
        AssertEqual(Block.Ordered, SalvageDerelict.WhyNoOrder(Player, d), "setup: the button is lit");

        SalvageDerelict.Cancel(Player, d);
        Assert.IsTrue(order.CalledOff && Player.AI.HasGoal(order), "the click calls the order off, which winds down");
        Assert.IsNull(d.Claimant, "giving up the claim at once");
        AssertEqual(Block.None, SalvageDerelict.WhyNoOrder(Player, d), "so the derelict can be ordered again with the station's other unit");
        AssertEqual(Localizer.Token(GameText.SalvageFinished), Status(order), "and the platform's salvage is finished");

        order.Evaluate();
        AssertEqual(0.01f, left, d.ProductionLeft, "the platform strips no more");
        AssertEqual(0.01f, 12f, station.GetProduction(), "and sends what it holds");
        Assert.IsTrue(order.Dismantling, "then takes itself apart");

        Assert.IsTrue(SalvageDerelict.Order(Player, d), "a new order on the derelict");
        SalvageDerelict again = SalvageDerelict.OrderOf(Player, d);
        Assert.AreNotSame(order, again, "is the derelict's order now");
        Assert.IsNotNull(again.FinishedShip, "and launches a constructor from the free unit");
        AssertEqual(Block.NoFreeUnit, SalvageDerelict.WhyNoOrder(Player, other), "while the old platform still holds its unit");
        Assert.IsTrue(d.TryClaim(Player), "setup: the new constructor claims the derelict");

        SalvageDerelict.Cancel(Player, d);
        Assert.IsFalse(Player.AI.HasGoal(again), "a new order called off before its platform is built ends");
        Assert.IsNull(d.Claimant, "with its claim, though the old order still winds down");

        Assert.IsTrue(SalvageDerelict.Order(Player, d), "setup: ordered once more");
        Assert.IsTrue(d.TryClaim(Player), "setup: and claimed");
        order.Evaluate();
        order.Evaluate();
        Assert.IsFalse(Player.AI.HasGoal(order), "the old order ends with its platform");
        AssertEqual(Player, d.Claimant, "leaving the new order's claim");
    }

    [TestMethod]
    public void ACalledOffOrderComingApartSurvivesSaveAndLoad()
    {
        Derelict d = Forms(Wreck);
        BuiltStation();
        SalvageDerelict order = AtWork(d, hold: 100f);
        order.Evaluate();
        SalvageDerelict.Cancel(Player, d);
        order.Evaluate();
        RunObjectsSim(UState.P.TurnTimer);
        order.Evaluate();
        float percent = order.Platform.DismantlePercent;
        AssertEqual(2f, 50f, percent, "setup: half way through its dismantling");

        UState.StarDate = 1001f; // the loader needs a stardate past 1000
        SavedGame save = Universe.Save("UnitTest.SalvageLoad", throwOnError: true);
        UniverseScreen loaded = LoadGame.Load(save.SaveFile, noErrorDialogs: true, startSimThread: false);
        Assert.IsNotNull(loaded, "setup: the save loads");

        var loadedOrder = (SalvageDerelict)loaded.UState.Player.AI.FindGoal(g => g is SalvageDerelict);
        Assert.IsTrue(loadedOrder.CalledOff && loadedOrder.Dismantling, "the order is called off and coming apart");
        Ship platform = loadedOrder.Platform;
        Assert.IsTrue(platform.Dismantling, "and so is its platform");
        AssertEqual(0.01f, percent, platform.DismantlePercent, "as far along");
        loadedOrder.Evaluate();
        Assert.IsFalse(platform.Active, "its last turn takes it away");
    }
}
