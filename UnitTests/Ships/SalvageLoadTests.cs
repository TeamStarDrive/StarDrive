using Microsoft.VisualStudio.TestTools.UnitTesting;
using Ship_Game;
using Ship_Game.Commands.Goals;
using Ship_Game.GameScreens.LoadGame;
using Ship_Game.Ships;
using Block = Ship_Game.Commands.Goals.SalvageDerelict.Block;
using Vector2 = SDGraphics.Vector2;

namespace UnitTests.Ships;

/// <summary>
/// The turn after its hold fills, a salvage platform sends its whole load to its salvage ship instead of stripping, as
/// much as the ship has room for; the rest stays aboard, and it strips again once its hold has room. With no room in
/// the ship, or the ship away from its site, the load waits, and the platform's status says which; a recovering
/// platform works on with a load it cannot send. The load's shuttles, one per 15 production and at most 20, fly one
/// way from the platform to the ship at 600 a second, and launch only where the player sees either end, and none
/// launches once its platform is gone. Once its derelict is stripped empty, or the Salvage button's second click calls
/// the order off, the platform sends the ship all it holds, full or not, then takes itself apart over two turns, shown
/// as a build from start to end, and is gone, freeing its unit; it waits to come apart until the ship has taken all of
/// it. The click gives up the claim at once, so the derelict can be ordered again while the old platform comes apart,
/// a rival's claim included, and the old order's end leaves a new order's claim alone. A platform is lost with its
/// ship, at work or coming apart; one in other hands while it comes apart stays whole. A called-off order coming apart
/// survives a save.
/// </summary>
[TestClass]
public class SalvageLoadTests : SalvageOrderTest
{
    // an earlier claim of the enemy's stripped all but 5
    void AllBut5Stripped(Derelict d) => UState.Derelicts.Strip(d, null, d.ProductionLeft - 5f);

    static string Status(SalvageDerelict order) => ShipInfoUIElement.SalvageStatus(order.Platform, order);

    static Ship ShipOf(SalvageDerelict order) => MissionOf(order).SalvageShip;

    [TestMethod]
    public void AFullHoldGoesToTheShipTheTurnAfterItFills()
    {
        Derelict d = Forms(Wreck);
        SalvageDerelict order = AtWork(d, hold: 24f);
        Ship ship = ShipOf(order);
        Ship platform = order.Platform;
        order.Evaluate();
        order.Evaluate();
        Assert.IsTrue(SalvageDerelict.HoldFull(platform), "setup: two turns fill its hold");
        float left = d.ProductionLeft;

        order.Evaluate();
        AssertEqual(0.01f, 24f, ship.GetProduction(), "the turn after, its whole load goes to the salvage ship");
        AssertEqual(0f, platform.GetProduction(), "leaving its hold empty");
        AssertEqual(0.01f, left, d.ProductionLeft, "and it strips nothing that turn");
        AssertEqual(0.01f, 24f, order.Delivered, "the order counts what it delivered");

        order.Evaluate();
        AssertEqual(0.01f, 12f, platform.GetProduction(), "then it strips again");
    }

    [TestMethod]
    public void TheShipTakesWhatFitsAndWithNoRoomTheLoadWaitsAboard()
    {
        Derelict d = Forms(Wreck);
        SalvageDerelict order = AtWork(d, hold: 24f);
        Ship ship = ShipOf(order);
        ship.LoadProduction(ship.CargoRoom - 10f);
        float stored = ship.GetProduction();
        Ship platform = order.Platform;
        order.Evaluate();
        order.Evaluate();
        Assert.IsTrue(SalvageDerelict.HoldFull(platform), "setup: two turns fill its hold");

        order.Evaluate();
        AssertEqual(0.01f, stored + 10f, ship.GetProduction(), "the ship takes the 10 it has room for");
        AssertEqual(0.01f, 14f, platform.GetProduction(), "and the rest stays aboard");

        order.Evaluate();
        AssertEqual(0.01f, 24f, platform.GetProduction(), "the platform strips again into the room it has");
        Assert.IsTrue(SalvageDerelict.HoldFull(platform) && SalvageDerelict.HoldFull(ship), "setup: both holds are full");

        order.Evaluate();
        AssertEqual(0.01f, 24f, platform.GetProduction(), "with no room in the ship its load waits");
        AssertEqual(Localizer.Token(GameText.SalvageShipHoldFull), Status(order), "and its status says so");
    }

    [TestMethod]
    public void ADamagedShipTakesOnlyWhatItsCargoRoomHolds()
    {
        Derelict d = Forms(Wreck);
        SalvageDerelict order = AtWork(d, hold: 24f);
        Ship ship = ShipOf(order);
        ship.LoadProduction(0f); // the hold takes its limit from the cargo room of the moment, before a cargo module is lost
        ship.CargoSpaceMax = 10f;
        order.Evaluate();
        order.Evaluate();
        Assert.IsTrue(SalvageDerelict.HoldFull(order.Platform), "setup: two turns fill its hold");

        order.Evaluate();
        AssertEqual(0.01f, 10f, ship.GetProduction(), "the ship takes only the 10 its cargo room holds");
        AssertEqual(0.01f, 14f, order.Platform.GetProduction(), "and the rest stays aboard");
    }

    [TestMethod]
    public void WithTheShipAwayAFullPlatformWaitsAndARecoveringOneWorksOnWithItsLoad()
    {
        Derelict d = Forms(Wreck);
        SalvageDerelict order = AtWork(d, hold: 24f);
        Ship ship = ShipOf(order);
        Ship platform = order.Platform;
        ship.LoadProduction(ship.CargoRoom);
        MissionOf(order).Evaluate();
        Assert.IsFalse(MissionOf(order).Parked(out _), "setup: its hold full, the salvage ship leaves to unload");
        ship.UnloadProduction(ship.GetProduction()); // as at the colony, with the ship still away
        order.Evaluate();
        order.Evaluate();
        Assert.IsTrue(SalvageDerelict.HoldFull(platform), "setup: two turns fill its hold");
        float left = d.ProductionLeft;

        order.Evaluate();
        AssertEqual(0.01f, 24f, platform.GetProduction(), "with its ship away the load waits aboard");
        AssertEqual(0.01f, left, d.ProductionLeft, "and the platform strips no more");
        AssertEqual(Localizer.Token(GameText.WaitingForSalvageShip), Status(order), "its status saying so");

        SalvageDerelict.Switch(Player, d, recover: true);
        order.Evaluate();
        AssertEqual(1, order.BreakInDone, "turned to the recovery, it breaks in though it cannot send its load");
        AssertEqual(0.01f, 24f, platform.GetProduction(), "keeping the load aboard");
        Assert.AreNotEqual(Localizer.Token(GameText.WaitingForSalvageShip), Status(order), "and its status is the recovery's");
    }

    [TestMethod]
    public void TheLoadsShuttlesFlyOneWayToTheShipOnePerFifteenProduction()
    {
        AssertEqual(5, CargoShuttles.LoadShuttlesFor(75f), "a load of 75 takes 5 shuttles");
        AssertEqual(2, CargoShuttles.LoadShuttlesFor(16f), "16 takes 2, rounded up");
        AssertEqual(1, CargoShuttles.LoadShuttlesFor(15f), "15 takes one");
        AssertEqual(1, CargoShuttles.LoadShuttlesFor(0.5f), "and so does any amount");
        AssertEqual(20, CargoShuttles.LoadShuttlesFor(1000f), "at most 20");

        Derelict d = Forms(Wreck);
        SalvageDerelict order = AtWork(d, hold: 24f);
        Ship ship = ShipOf(order);
        Ship platform = order.Platform;
        UState.ViewState = UniverseScreen.UnivScreenState.PlanetView;
        RunSimWhile((simTimeout: 5, fatal: true), () => !platform.InPlayerSensorRange || !ship.InPlayerSensorRange);
        platform.InFrustum = ship.InFrustum = false;

        order.Evaluate();
        order.Evaluate();
        order.Evaluate();
        Universe.CargoShuttles.Update(Universe, TestSimStep);
        AssertEqual(0.01f, 24f, ship.GetProduction(), "setup: a load went to the ship");
        AssertEqual(0, Universe.CargoShuttles.InFlight(platform, ship), "with no shuttles where the player sees neither end");

        order.Evaluate();
        order.Evaluate();
        ship.InFrustum = true;
        order.Evaluate();
        Universe.CargoShuttles.Update(Universe, TestSimStep);
        AssertEqual(2, Universe.CargoShuttles.InFlight(platform, ship), "a load of 24 sends 2 shuttles where the player sees the ship");

        // a flight takes 0.42 to 0.6 of its route's time, launched within 0.35 of it, the route timed for 600 a second on average
        double seconds = platform.Position.Distance(ship.Position) / (600.0 * 0.51);
        AssertGreaterThan(seconds, 2.0, "setup: a trip of a few seconds");
        double time = TestSimStepD;
        for (; time < seconds * 0.42 - 0.05; time += TestSimStepD)
            Universe.CargoShuttles.Update(Universe, TestSimStep);
        AssertEqual(2, Universe.CargoShuttles.InFlight(platform, ship), "none is there before the quickest flight");
        for (; time < seconds * 0.95 + 0.05; time += TestSimStepD)
            Universe.CargoShuttles.Update(Universe, TestSimStep);
        AssertEqual(0, Universe.CargoShuttles.InFlight(platform, ship), "and all are there by the latest launch and the slowest flight, none coming back");
    }

    [TestMethod]
    public void ALoadsShuttlesNeverLaunchOnceTheirPlatformIsGone()
    {
        Derelict d = Forms(Wreck);
        SalvageDerelict order = AtWork(d, hold: 24f);
        Ship ship = ShipOf(order);
        Ship platform = order.Platform;
        UState.ViewState = UniverseScreen.UnivScreenState.PlanetView;
        RunSimWhile((simTimeout: 5, fatal: true), () => !platform.InPlayerSensorRange || !ship.InPlayerSensorRange);
        platform.InFrustum = false;
        ship.InFrustum = true;
        order.Evaluate();
        order.Evaluate();
        order.Evaluate();
        AssertEqual(0.01f, 24f, ship.GetProduction(), "setup: a load went to the ship");

        platform.QueueTotalRemoval();
        double seconds = platform.Position.Distance(ship.Position) / (600.0 * 0.51);
        for (double time = 0.0; time < seconds * 0.36; time += TestSimStepD)
            Universe.CargoShuttles.Update(Universe, TestSimStep);
        AssertEqual(0, Universe.CargoShuttles.InFlight(platform, ship), "its shuttles, all due to launch by now, never did");
    }

    [TestMethod]
    public void AnEmptiedDerelictsPlatformSendsAllItHoldsThenTakesItselfApartOverTwoTurns()
    {
        TwinShips();
        Derelict d = Forms(Wreck);
        Derelict other = Forms(Wreck + new Vector2(0, 5_000f));
        SalvageDerelict order = AtWork(d, hold: 100f);
        Ship ship = ShipOf(order);
        Ship platform = order.Platform;
        SalvageMission mission = MissionOf(order);
        Assert.AreSame(mission, MissionOf(Ordered(other)), "setup: the ship's other unit works another derelict");
        AssertEqual(0, mission.FreeUnits, "setup: both units are busy");
        AllBut5Stripped(d);

        order.Evaluate();
        Assert.IsTrue(order.Emptied && order.Finished, "setup: the platform strips the 5 left");
        AssertEqual(Localizer.Token(GameText.SalvageFinished), Status(order), "with its load aboard its salvage is finished");

        order.Evaluate();
        AssertEqual(0.01f, 5f, ship.GetProduction(), "the next turn it sends its ship all it holds, though its hold is not full");
        Assert.IsTrue(order.Dismantling && platform.Dismantling, "and starts to take itself apart");
        AssertEqual(Localizer.Token(GameText.PlatformDismantling), Status(order), "as its status says");
        AssertEqual(0f, platform.DismantlePercent, "shown as a build that starts");
        AssertEqual(0, mission.FreeUnits, "its unit still busy");

        RunObjectsSim(UState.P.TurnTimer);
        AssertEqual(2f, 50f, platform.DismantlePercent, "half way after a turn");
        order.Evaluate();
        Assert.IsTrue(platform.Active, "it stands through the first turn");

        RunObjectsSim(UState.P.TurnTimer);
        AssertEqual(2f, 100f, platform.DismantlePercent, "and complete after two");
        order.Evaluate();
        Assert.IsFalse(platform.Active, "then it is gone");
        Assert.IsFalse(Player.AI.HasGoal(order), "its order done");
        AssertEqual(1, mission.FreeUnits, "and its unit free");
    }

    [TestMethod]
    public void AFinishedPlatformComesApartOnlyOnceItsShipTookAllItHolds()
    {
        Derelict d = Forms(Wreck);
        SalvageDerelict order = AtWork(d, hold: 100f);
        Ship ship = ShipOf(order);
        ship.LoadProduction(ship.CargoRoom - 2f);
        AllBut5Stripped(d);
        order.Evaluate();
        Assert.IsTrue(order.Emptied, "setup: the platform strips the 5 left");

        order.Evaluate();
        AssertEqual(0.01f, 3f, order.Platform.GetProduction(), "the ship takes the 2 it has room for");
        Assert.IsFalse(order.Dismantling || order.Platform.Dismantling, "so the platform keeps the rest and stands");
        AssertEqual(Localizer.Token(GameText.SalvageShipHoldFull), Status(order), "its status saying why");

        ship.UnloadProduction(10f);
        order.Evaluate();
        AssertEqual(0f, order.Platform.GetProduction(), "once the ship has room it takes the rest");
        Assert.IsTrue(order.Dismantling, "and the platform comes apart");
    }

    [TestMethod]
    public void ACalledOffPlatformFinishesThoughARivalClaimsTheDerelict()
    {
        Derelict d = Forms(Wreck);
        SalvageDerelict order = AtWork(d, hold: 100f);
        Ship ship = ShipOf(order);
        order.Evaluate();
        SalvageDerelict.Cancel(Player, d);
        Assert.IsTrue(d.TryClaim(Enemy), "setup: a rival claims the derelict once the claim is given up");

        order.Evaluate();
        Assert.IsTrue(Player.AI.HasGoal(order), "the called-off order stands on");
        AssertEqual(0.01f, 12f, ship.GetProduction(), "its platform sends what it holds");
        Assert.IsTrue(order.Dismantling, "and comes apart");
        AssertEqual(Enemy, d.Claimant, "leaving the rival's claim");
    }

    [TestMethod]
    public void APlatformInOtherHandsWhileComingApartStaysWhole()
    {
        Derelict d = Forms(Wreck);
        SalvageDerelict order = AtWork(d, hold: 100f);
        Ship platform = order.Platform;
        AllBut5Stripped(d);
        order.Evaluate();
        order.Evaluate();
        RunObjectsSim(UState.P.TurnTimer);
        Assert.IsTrue(platform.Dismantling && platform.DismantlePercent > 0f, "setup: the platform comes apart");

        platform.LoyaltyChangeByGift(Enemy, addNotification: false);
        UState.Objects.UpdateLists();
        AssertEqual(Enemy, platform.Loyalty, "setup: the platform changes hands, as only the debug tools can make it");

        order.Evaluate();
        Assert.IsFalse(Player.AI.HasGoal(order), "its order ends");
        Assert.IsTrue(platform.Active, "its new owner keeps the platform");
        Assert.IsFalse(platform.Dismantling, "which no longer comes apart");
        AssertEqual(0f, platform.DismantlePercent, "its dismantling undone");
    }

    [TestMethod]
    public void APlatformIsLostWithItsShipAtWorkOrComingApart()
    {
        TwinShips();
        Derelict d = Forms(Wreck);
        Derelict emptied = Forms(Wreck + new Vector2(0, 5_000f));
        SalvageDerelict working = AtWork(d, hold: 100f);
        Ship ship = ShipOf(working);
        working.Evaluate();
        SalvageDerelict apart = Ordered(emptied);
        Assert.AreSame(MissionOf(working), MissionOf(apart), "setup: the ship's other unit works the other derelict");
        Launched(apart);
        PlatformBuilt(apart, hold: 100f);
        AllBut5Stripped(emptied);
        apart.Evaluate();
        apart.Evaluate();
        Assert.IsTrue(apart.Dismantling, "setup: one platform comes apart");
        AssertEqual(0.01f, 12f, working.Platform.GetProduction(), "setup: while the other holds a load");

        ship.QueueTotalRemoval();
        MissionOf(working).Evaluate();
        working.Evaluate();
        apart.Evaluate();
        Assert.IsFalse(working.Platform.Active, "a platform at work is gone with its ship, its load with it");
        Assert.IsFalse(Player.AI.HasGoal(working), "its order ends");
        Assert.IsNull(d.Claimant, "with its claim");
        Assert.IsFalse(apart.Platform.Active, "and so is one coming apart");
        Assert.IsFalse(Player.AI.HasGoal(apart), "with its order");
    }

    [TestMethod]
    public void TheSecondClickSendsTheLoadThenTakesThePlatformApartWhileTheDerelictCanBeOrderedAgain()
    {
        TwinShips();
        Derelict d = Forms(Wreck);
        SalvageDerelict order = AtWork(d, hold: 100f);
        Ship ship = ShipOf(order);
        SalvageMission mission = MissionOf(order);
        order.Evaluate();
        float left = d.ProductionLeft;
        AssertEqual(Block.Ordered, SalvageDerelict.WhyNoOrder(Player, d), "setup: the button is lit");

        SalvageDerelict.Cancel(Player, d);
        Assert.IsTrue(order.CalledOff && Player.AI.HasGoal(order), "the click calls the order off, which winds down");
        Assert.IsNull(d.Claimant, "giving up the claim at once");
        AssertEqual(Block.None, SalvageDerelict.WhyNoOrder(Player, d), "so the derelict can be ordered again");
        AssertEqual(Localizer.Token(GameText.SalvageFinished), Status(order), "and the platform's salvage is finished");

        order.Evaluate();
        AssertEqual(0.01f, left, d.ProductionLeft, "the platform strips no more");
        AssertEqual(0.01f, 12f, ship.GetProduction(), "and sends what it holds");
        Assert.IsTrue(order.Dismantling, "then takes itself apart");

        SalvageDerelict again = Ordered(d);
        Assert.AreNotSame(order, again, "a new order on the derelict is the derelict's order now");
        Assert.AreSame(mission, MissionOf(again), "joining the ship's other unit");
        again.Evaluate();
        Assert.IsNotNull(again.FinishedShip, "which launches a constructor");
        AssertEqual(0, mission.FreeUnits, "while the old platform still holds its unit");
        Assert.IsTrue(d.TryClaim(Player), "setup: the new constructor claims the derelict");

        SalvageDerelict.Cancel(Player, d);
        Assert.IsFalse(Player.AI.HasGoal(again), "a new order called off before its platform is built ends");
        Assert.IsNull(d.Claimant, "with its claim, though the old order still winds down");

        Ordered(d);
        Assert.IsTrue(d.TryClaim(Player), "setup: ordered once more, and claimed");
        order.Evaluate();
        order.Evaluate();
        Assert.IsFalse(Player.AI.HasGoal(order), "the old order ends with its platform");
        AssertEqual(Player, d.Claimant, "leaving the new order's claim");
    }

    [TestMethod]
    public void ACalledOffOrderComingApartSurvivesSaveAndLoad()
    {
        Derelict d = Forms(Wreck);
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
