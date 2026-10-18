using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using SDGraphics;
using SDUtils;
using Ship_Game;
using Ship_Game.Commands.Goals;
using Ship_Game.GameScreens.LoadGame;
using Ship_Game.Ships;
using Block = Ship_Game.Commands.Goals.SalvageDerelict.Block;
using Vector2 = SDGraphics.Vector2;

namespace UnitTests.Ships;

/// <summary>
/// A salvage platform strips its derelict a turn at a time: 12 production, the Globals' SalvageSpeed, or what its hold
/// still takes, or what is left, into its hold as production cargo, counted to its empire's share on the derelict, a
/// share kept apart from other empires' earlier claims, and only what its hold took. With its hold full it strips no
/// more. Its shuttles fly from it to the hull and back, one for every 3 production stripped, launched 0.2 to
/// 2 s apart and 2 to 2.9 s each way, turning back at once, and only where the player sees the platform, as the
/// derelict's flashes do. A derelict stripped empty can no longer be salvaged: it leaves its system's and the empires'
/// lists at once and sinks away at twice the speed, while the order stands on with the platform. The platform's
/// panel reads Salvaging and the derelict's design, or Salvage hold full. The shares, and an order whose platform
/// stripped its derelict empty, survive a save.
/// </summary>
[TestClass]
public class SalvageWorkTests : StarDriveTest
{
    static readonly Vector2 SystemSite = new(600_000f, 600_000f);
    static readonly Vector2 Wreck = SystemSite + new Vector2(30_000f, 0);
    static readonly Vector2 PlayerStation = SystemSite + new Vector2(33_000f, 0);
    readonly SolarSystem WreckSystem;
    readonly IShipDesign Station;

    public SalvageWorkTests()
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
        AssertEqual(0f, platform.GetProduction(), "setup: with an empty hold");
        return order;
    }

    [TestMethod]
    public void APlatformStripsTwelveATurnIntoItsHoldForItsEmpiresShare()
    {
        AssertEqual(12f, GlobalStats.Defaults.SalvageSpeed, "setup: the default salvage speed");
        Derelict d = Forms(Wreck);
        BuiltStation();
        SalvageDerelict order = AtWork(d, hold: 100f);
        Ship platform = order.Platform;
        float left = d.ProductionLeft;

        order.Evaluate();
        AssertEqual(0.01f, 12f, platform.GetProduction(), "a turn's work is 12 production in the platform's hold");
        AssertEqual(0.01f, left - 12f, d.ProductionLeft, "taken from the derelict");
        AssertEqual(0.01f, 12f, d.ShareOf(Player), "and counted to the empire's share");
        AssertEqual(0f, d.ShareOf(Enemy), "and to no other's");

        order.Evaluate();
        AssertEqual(0.01f, 24f, platform.GetProduction(), "turn after turn");
        AssertEqual(0.01f, 24f, d.ShareOf(Player), "turn after turn");
    }

    [TestMethod]
    public void APlatformStripsOnlyWhatItsHoldTakes()
    {
        Derelict d = Forms(Wreck);
        BuiltStation();
        SalvageDerelict order = AtWork(d, hold: 30f);
        Ship platform = order.Platform;
        float left = d.ProductionLeft;
        AssertGreaterThan(left, 30f, "setup: the derelict holds more than the platform's hold takes");

        order.Evaluate();
        order.Evaluate();
        order.Evaluate();
        AssertEqual(0.01f, 30f, platform.GetProduction(), "12, 12, then the 6 its hold still takes");
        Assert.IsTrue(SalvageDerelict.HoldFull(platform), "its hold is full");

        order.Evaluate();
        AssertEqual(0.01f, left - 30f, d.ProductionLeft, "so it strips no more");
    }

    [TestMethod]
    public void APlatformStripsOnlyWhatItsHoldTookWhenTheHoldLagsBehindItsCargoRoom()
    {
        Derelict d = Forms(Wreck);
        BuiltStation();
        SalvageDerelict order = AtWork(d, hold: 18f);
        Ship platform = order.Platform;
        platform.LoadProduction(0f); // the hold takes its limit from the cargo room of the moment, before a damaged cargo module is repaired
        platform.CargoSpaceMax = 24f;
        float left = d.ProductionLeft;

        order.Evaluate();
        order.Evaluate();
        AssertEqual(0.01f, 18f, platform.GetProduction(), "the hold takes 18");
        AssertEqual(0.01f, left - 18f, d.ProductionLeft, "and only that much leaves the derelict");
        AssertEqual(0.01f, 18f, d.ShareOf(Player), "or counts to the empire's share");
        Assert.IsTrue(SalvageDerelict.HoldFull(platform), "then the hold is full");
        AssertEqual(Localizer.Token(GameText.SalvageHoldFull), ShipInfoUIElement.SalvageStatus(platform, order), "and its status says so");
    }

    [TestMethod]
    public void APlatformInOtherHandsEndsItsOrderAndItsClaim()
    {
        Derelict d = Forms(Wreck);
        BuiltStation();
        SalvageDerelict order = AtWork(d, hold: 100f);
        Ship platform = order.Platform;
        order.Evaluate();
        float left = d.ProductionLeft;

        platform.KillAllTroops();
        ResourceManager.CreateTroop("Wyvern", Enemy).LandOnShip(platform);
        platform.LoyaltyChangeFromBoarding(Enemy, addNotification: false);
        UState.Objects.UpdateLists();
        AssertEqual(Enemy, platform.Loyalty, "setup: the platform is boarded");

        order.Evaluate();
        Assert.IsFalse(Player.AI.HasGoal(order), "its order ends, as with a lost platform");
        Assert.IsNull(d.Claimant, "with its claim");
        AssertEqual(left, d.ProductionLeft, "and strips no more");
        Assert.IsTrue(platform.Active, "the boarders keep the platform");
        AssertEqual(0.01f, 12f, platform.GetProduction(), "with what it holds");
    }

    [TestMethod]
    public void ADerelictStrippedEmptyCanNoLongerBeSalvagedAndSinksAwayAtTwiceTheSpeed()
    {
        Derelict d = Forms(Wreck);
        Ship station = BuiltStation();
        SalvageDerelict order = AtWork(d, hold: 100f);
        float earlier = d.ProductionLeft - 5f;
        AssertEqual(0.01f, earlier, UState.Derelicts.Strip(d, Enemy, null, earlier), "setup: an earlier claim of the enemy's stripped all but 5");
        Assert.IsTrue(WreckSystem.Derelicts.Contains(d) && Player.DerelictSystems.Contains(WreckSystem), "setup: the derelict is listed");

        order.Evaluate();
        AssertEqual(0.01f, 5f, order.Platform.GetProduction(), "the platform strips the 5 left");
        AssertEqual(0.01f, 5f, d.ShareOf(Player), "for the empire's share");
        AssertEqual(0.01f, earlier, d.ShareOf(Enemy), "apart from the enemy's earlier one");
        Assert.IsTrue(d.Stripped && !d.CanBeSalvaged && !d.SelectableByPlayer, "the derelict is stripped empty and can no longer be salvaged or selected");
        Assert.IsFalse(WreckSystem.Derelicts.Contains(d), "it leaves its system's list at once");
        Assert.IsFalse(Player.DerelictSystems.Contains(WreckSystem), "and the system, with none left, leaves the empire's");
        AssertEqual(Block.Gone, SalvageDerelict.WhyNoOrder(Player, d), "nobody can order it salvaged");

        order.Evaluate();
        Assert.IsTrue(Player.AI.HasGoal(order) && order.Emptied, "the order stands on");
        Assert.IsTrue(order.Platform.Active, "with its platform");
        AssertEqual(0.01f, 5f, station.GetProduction(), "which sends its station its load");

        Assert.IsTrue(d.Sinking, "the hull sinks away");
        float z = d.Z;
        UState.Derelicts.Update(new FixedSimTime(1f));
        AssertEqual(0.01f, z + 100f, d.Z, "at 100 a second, twice a derelict whose turns ran out");
    }

    [TestMethod]
    public void ItsShuttlesFlyToTheHullAndBackOnePerThreeProductionStripped()
    {
        AssertEqual(4, CargoShuttles.SalvageShuttlesFor(12f), "12 stripped takes 4 shuttles");
        AssertEqual(2, CargoShuttles.SalvageShuttlesFor(5f), "5 takes 2, rounded up");
        AssertEqual(1, CargoShuttles.SalvageShuttlesFor(0.5f), "any amount takes one");

        Derelict d = Forms(Wreck);
        BuiltStation();
        SalvageDerelict order = AtWork(d, hold: 100f);
        Ship platform = order.Platform;
        UState.ViewState = UniverseScreen.UnivScreenState.PlanetView;
        RunSimWhile((simTimeout: 5, fatal: true), () => !platform.InPlayerSensorRange);

        platform.InFrustum = false;
        order.Evaluate();
        Universe.CargoShuttles.Update(Universe, TestSimStep);
        AssertEqual(0, Universe.CargoShuttles.InFlight(null, platform), "none for a platform off screen");

        platform.InFrustum = true;
        order.Evaluate();
        Universe.CargoShuttles.Update(Universe, TestSimStep);
        AssertEqual(4, Universe.CargoShuttles.InFlight(null, platform), "a turn's 12 sends 4 shuttles from the platform on screen");
        AssertEqual(0, Universe.CargoShuttles.InFlightTowards(platform), "out to the hull first");

        AssertEqual(0, Universe.CargoShuttles.Launched(platform), "none launches at once");

        int comingBack = 0;
        double time = TestSimStepD;
        for (; time < 0.19; time += TestSimStepD)
            Universe.CargoShuttles.Update(Universe, TestSimStep);
        AssertEqual(0, Universe.CargoShuttles.Launched(platform), "nor before 0.2 s");
        for (; time < 2.05; time += TestSimStepD)
            Universe.CargoShuttles.Update(Universe, TestSimStep);
        AssertEqual(4, Universe.CargoShuttles.Launched(platform), "and all have by 2 s");
        for (; time < 4.2; time += TestSimStepD)
            Universe.CargoShuttles.Update(Universe, TestSimStep);
        AssertEqual(4, Universe.CargoShuttles.InFlight(null, platform), "launched 0.2 to 2 s apart and 2 to 2.9 s each way, none is back within 4.2 s");
        for (; time < 7.9; time += TestSimStepD)
        {
            Universe.CargoShuttles.Update(Universe, TestSimStep);
            comingBack = comingBack.LowerBound(Universe.CargoShuttles.InFlightTowards(platform));
        }
        AssertGreaterThan(comingBack, 0, "they turn back at the hull");
        AssertEqual(0, Universe.CargoShuttles.InFlight(null, platform), "and are all back within 7.8 s");
    }

    [TestMethod]
    public void ADerelictFlashesOnlyWhileThePlayerSeesThePlatformStrippingIt()
    {
        Derelict mine = Forms(Wreck);
        Derelict theirs = Forms(Wreck + new Vector2(0, 5_000f));
        BuiltStation();
        SalvageDerelict order = AtWork(mine, hold: 100f);
        Ship platform = order.Platform;
        Ship unseen = SpawnShip(ShipDesign.SalvagePlatformName, Enemy, new Vector2(900_000f, 200_000f));
        RunSimWhile((simTimeout: 5, fatal: true), () => !platform.InPlayerSensorRange);
        Assert.IsFalse(unseen.InPlayerSensorRange, "setup: the rival's platform is out of the player's sight");
        Assert.IsFalse(mine.StripEffectsShown, "setup: no flashes before it is stripped");

        order.Evaluate();
        Assert.IsTrue(mine.StripEffectsShown, "the player's platform at work makes its derelict flash");
        UState.Derelicts.Strip(theirs, Enemy, unseen, 12f);
        Assert.IsFalse(theirs.StripEffectsShown, "a rival's platform out of the player's sight makes no flashes");
    }

    [TestMethod]
    public void ThePlatformsStatusReadsSalvagingTheDerelictOrSalvageHoldFull()
    {
        Derelict d = Forms(Wreck);
        BuiltStation();
        SalvageDerelict order = AtWork(d, hold: 24f);
        Ship platform = order.Platform;
        Assert.AreSame(order, SalvageDerelict.OfPlatform(platform), "the platform's order is found from it");
        AssertEqual(string.Format(Localizer.Token(GameText.SalvagingDerelict), d.Design.Name), ShipInfoUIElement.SalvageStatus(platform, order),
                    "while it strips, the status names the derelict's design");

        order.Evaluate();
        order.Evaluate();
        Assert.IsTrue(SalvageDerelict.HoldFull(platform), "setup: two turns fill its hold");
        AssertEqual(Localizer.Token(GameText.SalvageHoldFull), ShipInfoUIElement.SalvageStatus(platform, order), "and then says its hold is full");
    }

    [TestMethod]
    public void TheSharesAndAnOrderThatStrippedItsDerelictEmptySurviveSaveAndLoad()
    {
        Derelict partly = Forms(Wreck);
        Derelict emptied = Forms(Wreck + new Vector2(0, 5_000f));
        BuiltStation();
        SalvageDerelict working = AtWork(partly, hold: 100f);
        working.Evaluate();
        SalvageDerelict done = AtWork(emptied, hold: 100f);
        UState.Derelicts.Strip(emptied, Enemy, null, emptied.ProductionLeft - 5f);
        done.Evaluate();
        Assert.IsTrue(done.Emptied, "setup: the second platform stripped its derelict empty");

        UState.StarDate = 1001f; // the loader needs a stardate past 1000
        SavedGame save = Universe.Save("UnitTest.SalvageWork", throwOnError: true);
        UniverseScreen loaded = LoadGame.Load(save.SaveFile, noErrorDialogs: true, startSimThread: false);
        Assert.IsNotNull(loaded, "setup: the save loads");

        Empire player = loaded.UState.Player;
        Derelict loadedPartly = loaded.UState.Derelicts.Find(partly.Id);
        AssertEqual(0.01f, 12f, loadedPartly.ShareOf(player), "a derelict keeps each empire's share");
        AssertEqual(0.01f, partly.ProductionLeft, loadedPartly.ProductionLeft, "and the production left in it");

        SalvageDerelict[] orders = player.AI.FindGoals<SalvageDerelict>();
        SalvageDerelict loadedWorking = orders.First(o => o.DerelictId == partly.Id);
        AssertEqual(0.01f, 12f, loadedWorking.Platform.GetProduction(), "a working platform keeps its load");
        SalvageDerelict loadedDone = orders.First(o => o.DerelictId == emptied.Id);
        Assert.IsTrue(loadedDone.Emptied, "an order whose platform stripped its derelict empty stays so");
        AssertEqual(0.01f, 5f, loadedDone.Platform.GetProduction(), "with its platform's load");
        loadedDone.Evaluate();
        Assert.IsTrue(player.AI.HasGoal(loadedDone), "and stands on");
    }
}
