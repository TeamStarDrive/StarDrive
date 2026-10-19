using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Ship_Game;
using Ship_Game.Commands.Goals;
using Ship_Game.GameScreens.LoadGame;
using Ship_Game.Ships;
using Vector2 = SDGraphics.Vector2;

namespace UnitTests.Ships;

/// <summary>
/// The player is told when a salvage platform has sent its station its last load and starts to come apart, whether its
/// derelict was stripped empty or the order called off: the production its loads brought, then each tech with the
/// research they gave it, a tech whose points show as 0 left out. The player is also told when a salvage ends early:
/// its platform destroyed, its station lost (once for each order there, one still waiting to launch its constructor
/// too, under the derelict's icon when the order has not found its station since a load), a rival claiming the
/// derelict first, or the derelict sinking away before the claim. None of these pauses the game. Nothing is told of
/// an order called off before its platform is built, a platform lost while it comes apart, a station the player
/// scraps, salvage ops turned off, or another empire's salvage. The Salvage alerts switch silences them all, and the
/// Derelict alerts switch the notification of a new derelict. An order's production delivered and its derelict's name
/// and icon survive a save, as do both switches.
/// </summary>
[TestClass]
public class SalvageNotifyTests : StarDriveTest
{
    static readonly Vector2 SystemSite = new(600_000f, 600_000f);
    static readonly Vector2 Wreck = SystemSite + new Vector2(30_000f, 0);
    static readonly Vector2 PlayerStation = SystemSite + new Vector2(33_000f, 0);
    static readonly Vector2 EnemyStation = SystemSite + new Vector2(-33_000f, 0);
    readonly SolarSystem WreckSystem;
    readonly IShipDesign Station;
    NotificationManager Notes;

    public SalvageNotifyTests()
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

    // a salvageable derelict of the owner's ship, so the owner knows of it; the ship itself is gone
    Derelict Forms(Vector2 at, Empire owner = null)
    {
        TestShip ship = SpawnShip("Dreadnought mk1-a", owner ?? Player, at);
        Assert.IsTrue(UState.Derelicts.TryAdd(ship, salvageable: true, seen: false), "setup: a salvageable derelict");
        Universe.InvokePendingSimThreadActions();
        ship.QueueTotalRemoval();
        return UState.Derelicts.Current.Last();
    }

    Ship BuiltStation(Empire owner = null, Vector2? at = null)
    {
        owner ??= Player;
        var order = new SalvageOps(owner, WreckSystem, at ?? PlayerStation, Station);
        owner.AI.AddGoal(order);
        order.AdvanceToNextStep(); // past the queueing, which needs a port
        order.TargetShip = SpawnShip(Station.Name, owner, at ?? PlayerStation);
        order.Evaluate();
        AssertEqual("Salvage", order.StepName, "setup: the station is built");
        return order.TargetShip;
    }

    // the player's order with its platform built beside the derelict, about to strip
    SalvageDerelict AtWork(Derelict d, float hold)
    {
        Assert.IsTrue(SalvageDerelict.Order(Player, d), "setup: ordered");
        SalvageDerelict order = SalvageDerelict.OrderOf(Player, d);
        Ship constructor = order.FinishedShip;
        Assert.IsTrue(d.TryClaim(Player), "setup: the constructor claims the derelict as it starts to build");
        Ship platform = SpawnShip(ShipDesign.SalvagePlatformName, Player, order.BuildPosition);
        platform.CargoSpaceMax = hold;
        constructor.QueueTotalRemoval();
        order.OnPlatformBuilt(platform, constructor);
        order.Evaluate();
        AssertEqual("Salvage", order.StepName, "setup: the platform is built");
        return order;
    }

    SalvageDerelict Ordered(Empire empire, Derelict d)
    {
        Assert.IsTrue(SalvageDerelict.Order(empire, d), "setup: ordered");
        return SalvageDerelict.OrderOf(empire, d);
    }

    void TrackNotifications()
    {
        Universe.NotificationManager = Notes = new NotificationManager(Universe.ScreenManager, Universe);
    }

    Notification[] Told() => Notes.GetNotificationsAtomic();

    // its order's next turn strips the 5 left
    void AllBut5Stripped(Derelict d) => UState.Derelicts.Strip(d, null, d.ProductionLeft - 5f);

    // the order's points for each tech, none for the rest, which its last load leaves as they are
    void HasGained(SalvageDerelict order, params float[] gained)
    {
        for (int i = 0; i < order.Techs.Length; ++i)
        {
            order.Gained[i] = i < gained.Length ? gained[i] : 0f;
            Player.GetTechEntry(order.Techs[i]).Unlocked = true;
        }
    }

    static string[] TechNames(Derelict d) => d.Techs.Select(t => ResourceManager.Tech(t).Name.Text).ToArray();

    void SinksAway(Derelict d)
    {
        while (d.TurnsLeft > 0)
            UState.Derelicts.UpdateTurn();
    }

    [TestMethod]
    public void APlatformThatSentItsLastLoadTellsTheProductionItsLoadsBroughtAndTheResearchEachTechGained()
    {
        Derelict d = Forms(Wreck);
        Ship station = BuiltStation();
        SalvageDerelict order = AtWork(d, hold: 24f);
        TrackNotifications();
        order.Evaluate();
        order.Evaluate();
        order.Evaluate();
        AssertEqual(0.01f, 24f, station.GetProduction(), "setup: a load of 24 reaches the station");
        AllBut5Stripped(d);
        order.Evaluate();
        Assert.IsTrue(order.Emptied, "setup: the platform strips the 5 left");
        AssertEqual(0, Told().Length, "nothing is told while the platform holds its last load");

        HasGained(order, 12_540.4f, 0.4f, 958f, 2_412f, 0f, 31.6f);
        order.Evaluate();
        Assert.IsTrue(order.Dismantling, "setup: the platform sent its last load and comes apart");
        string[] names = TechNames(d);
        Notification told = Told().Single();
        AssertEqual($"{WreckSystem.Name}: the Dreadnought mk1-a is stripped\n29 production salvaged\n{names[0]} +12,540\n{names[2]} +958\n{names[3]} +2,412\n{names[5]} +32",
                    told.Message, "the player is told the production its loads brought, and each tech's points but those that show as 0");
        AssertEqual(d.Design.BaseHull.IconPath, told.IconPath, "under the derelict's hull icon");
        AssertEqual("NewUI/icon_salvage_notification", told.SymbolPath, "with the research symbol");
        Assert.IsFalse(told.Pause, "it does not pause the game");
        AssertEqual("SnapToSystem", told.Action, "and a click shows the system");
        Assert.AreSame(WreckSystem, told.ReferencedItem1);

        RunObjectsSim(UState.P.TurnTimer);
        order.Evaluate();
        AssertEqual(1, Told().Length, "the platform coming apart tells no more");
    }

    [TestMethod]
    public void ACalledOffSalvageTellsTheSameOnceItsPlatformSentItsLastLoad()
    {
        Derelict d = Forms(Wreck);
        BuiltStation();
        SalvageDerelict order = AtWork(d, hold: 100f);
        order.Evaluate();
        TrackNotifications();
        SalvageDerelict.Cancel(Player, d);
        AssertEqual(0, Told().Length, "the click tells nothing");

        HasGained(order, 500f);
        order.Evaluate();
        Assert.IsTrue(order.Dismantling, "setup: the platform sent what it held and comes apart");
        AssertEqual($"{WreckSystem.Name}: salvage of the Dreadnought mk1-a called off\n12 production salvaged\n{TechNames(d)[0]} +500",
                    Told().Single().Message, "the player is told the salvage was called off, with what it brought");
    }

    [TestMethod]
    public void APlatformDestroyedAtWorkIsToldAndAClickSelectsTheDerelict()
    {
        Derelict d = Forms(Wreck);
        BuiltStation();
        SalvageDerelict order = AtWork(d, hold: 100f);
        TrackNotifications();
        Ship platform = order.Platform;
        platform.Die(null, cleanupOnly: true);

        order.Evaluate();
        Assert.IsFalse(Player.AI.HasGoal(order), "setup: the order ends");
        Notification told = Told().Single();
        AssertEqual($"{WreckSystem.Name}: your salvage platform at the Dreadnought mk1-a was destroyed", told.Message, "the player is told");
        AssertEqual(platform.ShipData.IconPath, told.IconPath, "under the platform's icon");
        Assert.IsFalse(told.Pause, "it does not pause the game");
        AssertEqual("SnapToDerelict", told.Action, "a click selects the derelict");
        Assert.AreSame(d, told.ReferencedItem1);
        Assert.AreSame(WreckSystem, told.ReferencedItem2, "or shows its system once it can no longer be salvaged");
    }

    [TestMethod]
    public void ARivalThatClaimedTheDerelictFirstIsToldUnderItsFlag()
    {
        Derelict d = Forms(Wreck);
        BuiltStation();
        SalvageDerelict order = Ordered(Player, d);
        TrackNotifications();
        Assert.IsTrue(d.TryClaim(Enemy), "setup: a rival's constructor starts to build first");

        order.Evaluate();
        Assert.IsFalse(Player.AI.HasGoal(order), "setup: the order ends");
        Notification told = Told().Single();
        AssertEqual($"{WreckSystem.Name}: the {Enemy.Name} started salvaging the Dreadnought mk1-a first", told.Message, "the player is told who");
        Assert.AreSame(Enemy, told.RelevantEmpire, "under the rival's flag");
        AssertEqual(d.Design.BaseHull.IconPath, told.IconPath, "over the derelict's hull icon");
        Assert.IsFalse(told.Pause, "it does not pause the game");
        AssertEqual("SnapToDerelict", told.Action, "a click selects the derelict");
        Assert.AreSame(d, told.ReferencedItem1);
    }

    [TestMethod]
    public void AStationLostIsToldOnceForEachOrderItEnds()
    {
        Derelict working = Forms(Wreck);
        Derelict waiting = Forms(Wreck + new Vector2(0, 5_000f));
        Ship station = BuiltStation();
        SalvageDerelict atWork = AtWork(working, hold: 100f);
        SalvageDerelict building = Ordered(Player, waiting);
        TrackNotifications();
        station.Die(null, cleanupOnly: true);

        atWork.Evaluate();
        for (int i = 0; i < 3 && Player.AI.HasGoal(building); ++i)
            building.Evaluate();
        Assert.IsFalse(Player.AI.HasGoal(atWork) || Player.AI.HasGoal(building), "setup: both orders end");
        Notification[] told = Told();
        AssertEqual(2, told.Length, "the player is told once for each");
        foreach (Notification n in told)
        {
            AssertEqual($"{WreckSystem.Name}: your salvage station was lost, and your salvage of the Dreadnought mk1-a with it", n.Message);
            AssertEqual(station.ShipData.IconPath, n.IconPath, "under the station's icon");
            Assert.IsFalse(n.Pause, "it does not pause the game");
        }
        Assert.AreSame(working, told[0].ReferencedItem1, "a click selects each one's derelict");
        Assert.AreSame(waiting, told[1].ReferencedItem1);
    }

    [TestMethod]
    public void AnOrderWaitingToLaunchItsConstructorIsToldOfItsStationsLossToo()
    {
        Derelict d = Forms(Wreck);
        Ship station = BuiltStation();
        station.ChangeOrdnance(-station.Ordinance);
        SalvageDerelict waiting = Ordered(Player, d);
        AssertEqual("LaunchConstructor", waiting.StepName, "setup: short of ordnance, its unit waits to launch the constructor");
        TrackNotifications();
        station.Die(null, cleanupOnly: true);

        waiting.Evaluate();
        Assert.IsFalse(Player.AI.HasGoal(waiting), "setup: the order ends");
        Notification told = Told().Single();
        AssertEqual($"{WreckSystem.Name}: your salvage station was lost, and your salvage of the Dreadnought mk1-a with it", told.Message, "the player is told");
        AssertEqual(station.ShipData.IconPath, told.IconPath, "under the station's icon");
    }

    [TestMethod]
    public void AStationLostBeforeTheOrderFoundItShowsTheDerelictsIcon()
    {
        Derelict d = Forms(Wreck);
        Ship station = BuiltStation();
        var order = new SalvageDerelict(Player, d);
        Player.AI.AddGoal(order); // as a save loads it, before its first turn
        TrackNotifications();
        station.Die(null, cleanupOnly: true);

        order.Evaluate();
        Assert.IsFalse(Player.AI.HasGoal(order), "setup: the order ends");
        AssertEqual(d.Design.BaseHull.IconPath, Told().Single().IconPath, "the player is told under the derelict's hull icon");
    }

    [TestMethod]
    public void AStationThePlayerScrapsTellsNothing()
    {
        Derelict working = Forms(Wreck);
        Derelict waiting = Forms(Wreck + new Vector2(0, 5_000f));
        Ship station = BuiltStation();
        SalvageDerelict atWork = AtWork(working, hold: 100f);
        SalvageDerelict building = Ordered(Player, waiting);
        TrackNotifications();
        station.AI.OrderScrapShip();
        Assert.IsFalse(station.Active, "setup: the player scraps the station, which scuttles at once");

        atWork.Evaluate();
        for (int i = 0; i < 3 && Player.AI.HasGoal(building); ++i)
            building.Evaluate();
        Assert.IsFalse(Player.AI.HasGoal(atWork) || Player.AI.HasGoal(building), "setup: both orders end");
        AssertEqual(0, Told().Length, "and the player, who scrapped it, is told nothing");
    }

    [TestMethod]
    public void ADerelictThatSankBeforeTheClaimIsTold()
    {
        Derelict d = Forms(Wreck);
        BuiltStation();
        SalvageDerelict order = Ordered(Player, d);
        TrackNotifications();
        SinksAway(d);

        order.Evaluate();
        Assert.IsFalse(Player.AI.HasGoal(order), "setup: the order ends");
        Notification told = Told().Single();
        AssertEqual($"{WreckSystem.Name}: the Dreadnought mk1-a sank away before your platform was built", told.Message, "the player is told");
        AssertEqual(d.Design.BaseHull.IconPath, told.IconPath, "under the derelict's hull icon");
        AssertEqual("NewUI/icon_derelict_notification", told.SymbolPath, "with the hourglass");
        Assert.IsFalse(told.Pause, "it does not pause the game");
        AssertEqual("SnapToSystem", told.Action, "a click shows the system");
        Assert.AreSame(WreckSystem, told.ReferencedItem1);
    }

    [TestMethod]
    public void NothingIsToldOfACallOffBeforeThePlatformAPlatformLostComingApartSalvageOpsTurnedOffOrAnotherEmpiresSalvage()
    {
        Derelict unbuilt = Forms(Wreck);
        Derelict apart = Forms(Wreck + new Vector2(0, 5_000f));
        Derelict theirs = Forms(Wreck + new Vector2(0, -5_000f), owner: Enemy);
        BuiltStation();
        BuiltStation(Enemy, EnemyStation);
        TrackNotifications();

        SalvageDerelict called = Ordered(Player, unbuilt);
        SalvageDerelict.Cancel(Player, unbuilt);
        Assert.IsFalse(Player.AI.HasGoal(called), "setup: an order called off before its platform is built ends");
        AssertEqual(0, Told().Length, "and tells nothing");

        SalvageDerelict order = AtWork(apart, hold: 100f);
        AllBut5Stripped(apart);
        order.Evaluate();
        order.Evaluate();
        Assert.IsTrue(order.Dismantling, "setup: a platform comes apart");
        AssertEqual(1, Told().Length, "setup: told of its last load");
        order.Platform.Die(null, cleanupOnly: true);
        order.Evaluate();
        Assert.IsFalse(Player.AI.HasGoal(order), "setup: its order ends");
        AssertEqual(1, Told().Length, "a platform lost while it comes apart tells nothing more");

        SalvageDerelict stopped = Ordered(Player, unbuilt);
        UState.P.DisableSalvageOps = true;
        stopped.Evaluate();
        UState.P.DisableSalvageOps = false;
        Assert.IsFalse(Player.AI.HasGoal(stopped), "setup: salvage ops off ends an order");
        AssertEqual(1, Told().Length, "and tells nothing");

        SalvageDerelict rival = Ordered(Enemy, theirs);
        SinksAway(theirs);
        rival.Evaluate();
        Assert.IsFalse(Enemy.AI.HasGoal(rival), "setup: another empire's order ends with its derelict sunk");
        AssertEqual(1, Told().Length, "and tells the player nothing");
    }

    [TestMethod]
    public void TheSalvageAlertsSwitchSilencesTheSalvageNotificationsAndTheDerelictAlertsSwitchTheNewDerelicts()
    {
        Derelict stripped = Forms(Wreck);
        Derelict sinking = Forms(Wreck + new Vector2(0, 5_000f));
        Derelict claimed = Forms(Wreck + new Vector2(0, -5_000f));
        BuiltStation();
        SalvageDerelict order = AtWork(stripped, hold: 100f);
        TrackNotifications();
        UState.P.DisableSalvageWarning = true;

        AllBut5Stripped(stripped);
        order.Evaluate();
        order.Evaluate();
        Assert.IsTrue(order.Dismantling, "setup: a platform sent its last load");
        SalvageDerelict lost = Ordered(Player, claimed);
        Assert.IsTrue(claimed.TryClaim(Enemy), "setup: a rival claims a derelict first");
        lost.Evaluate();
        Assert.IsFalse(Player.AI.HasGoal(lost), "setup: that order ends");
        SalvageDerelict sinks = Ordered(Player, sinking);
        SinksAway(sinking);
        sinks.Evaluate();
        Assert.IsFalse(Player.AI.HasGoal(sinks), "setup: an order's derelict sank");
        AssertEqual(0, Told().Length, "with Salvage alerts off, none is told");

        UState.P.DisableDerelictWarning = true;
        Notes.AddNewDerelictNotification(claimed);
        AssertEqual(0, Told().Length, "with Derelict alerts off, no new derelict is told");
        UState.P.DisableDerelictWarning = false;
        Notes.AddNewDerelictNotification(claimed);
        AssertEqual(1, Told().Length, "and with them on it is");
    }

    [TestMethod]
    public void AnOrdersProductionDeliveredItsDerelictsNameAndIconAndTheAlertSwitchesSurviveSaveAndLoad()
    {
        Derelict d = Forms(Wreck);
        Ship station = BuiltStation();
        SalvageDerelict order = AtWork(d, hold: 24f);
        order.Evaluate();
        order.Evaluate();
        order.Evaluate();
        AssertEqual(0.01f, 24f, station.GetProduction(), "setup: a load of 24 reaches the station");
        UState.P.DisableSalvageWarning = true;
        UState.P.DisableDerelictWarning = true;

        UState.StarDate = 1001f; // the loader needs a stardate past 1000
        SavedGame save = Universe.Save("UnitTest.SalvageNotify", throwOnError: true);
        UniverseScreen loaded = LoadGame.Load(save.SaveFile, noErrorDialogs: true, startSimThread: false);
        Assert.IsNotNull(loaded, "setup: the save loads");

        SalvageDerelict loadedOrder = loaded.UState.Player.AI.FindGoals<SalvageDerelict>().Single();
        AssertEqual(0.01f, 24f, loadedOrder.Delivered, "an order keeps the production its loads brought");
        AssertEqual("Dreadnought mk1-a", loadedOrder.DerelictName, "and its derelict's name");
        AssertEqual(d.Design.BaseHull.IconPath, loadedOrder.DerelictIcon, "and hull icon");
        Assert.IsTrue(loaded.UState.P.DisableSalvageWarning, "the Salvage alerts switch stays off");
        Assert.IsTrue(loaded.UState.P.DisableDerelictWarning, "and so does the Derelict alerts switch");
    }
}
