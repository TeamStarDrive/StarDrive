using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Ship_Game;
using Ship_Game.Commands.Goals;
using Ship_Game.GameScreens.LoadGame;
using Ship_Game.Ships;
using Vector2 = SDGraphics.Vector2;

namespace UnitTests.Ships;

/// <summary>
/// The player is told when a salvage platform has sent its salvage ship its last load and starts to come apart,
/// whether its derelict was stripped empty or the order called off: the production its loads brought, then each tech
/// with the research they gave it, a tech whose points show as 0 left out. The player is also told when a salvage ends
/// early: its platform destroyed, a rival claiming the derelict first, or the derelict sinking away before the claim;
/// and when its salvage ship is lost, once for each order of its mission, one still waiting to launch its constructor
/// too, under the ship's icon, and once more for the production it had aboard. None of these pauses the game. Nothing
/// is told of an order called off before its platform is built, a platform lost while it comes apart, salvage ops
/// turned off, or another empire's salvage. The Salvage alerts switch silences them all, and the Derelict alerts
/// switch the notification of a new derelict. An order's production delivered and its derelict's name and icon
/// survive a save, as do both switches.
/// </summary>
[TestClass]
public class SalvageNotifyTests : SalvageOrderTest
{
    NotificationManager Notes;

    void TrackNotifications()
    {
        Universe.NotificationManager = Notes = new NotificationManager(Universe.ScreenManager, Universe);
    }

    Notification[] Told() => Notes.GetNotificationsAtomic();

    static Ship ShipOf(SalvageDerelict order) => MissionOf(order).SalvageShip;

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
        SalvageDerelict order = AtWork(d, hold: 24f);
        TrackNotifications();
        order.Evaluate();
        order.Evaluate();
        order.Evaluate();
        AssertEqual(0.01f, 24f, ShipOf(order).GetProduction(), "setup: a load of 24 reaches the salvage ship");
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
        SalvageDerelict order = Ordered(d);
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
    public void ASalvageShipLostIsToldOnceForEachOfItsOrdersAndOnceForTheProductionAboard()
    {
        TwinShips();
        Derelict working = Forms(Wreck);
        Derelict waiting = Forms(Wreck + new Vector2(0, 5_000f));
        SalvageDerelict atWork = AtWork(working, hold: 24f);
        Ship ship = ShipOf(atWork);
        SalvageDerelict launching = Ordered(waiting);
        Assert.AreSame(MissionOf(atWork), MissionOf(launching), "setup: the ship's other unit is to launch for the second order");
        ship.LoadProduction(30f);
        Ship platform = atWork.Platform;
        TrackNotifications();
        ship.Die(null, cleanupOnly: true);

        MissionOf(atWork).Evaluate();
        atWork.Evaluate();
        launching.Evaluate();
        Assert.IsTrue(Player.AI.HasGoal(atWork) && Player.AI.HasGoal(launching), "setup: both orders start again");
        Notification[] told = Told();
        AssertEqual(3, told.Length, "the player is told once for each order and once for the production aboard");
        AssertEqual("Your salvage ship was lost with 30 production aboard", told[0].Message, "the production aboard");
        foreach (Notification n in told)
        {
            AssertEqual(ship.ShipData.IconPath, n.IconPath, "under the ship's icon");
            Assert.IsFalse(n.Pause, "it does not pause the game");
        }
        foreach (Notification n in told.Skip(1))
            AssertEqual($"{WreckSystem.Name}: your salvage ship for the Dreadnought mk1-a was lost", n.Message);
        Assert.AreSame(working, told[1].ReferencedItem1, "a click selects each one's derelict");
        Assert.AreSame(waiting, told[2].ReferencedItem1);
        Assert.IsFalse(platform.Active, "and the platform is gone with the ship");
    }

    [TestMethod]
    public void ADerelictThatSankBeforeTheClaimIsTold()
    {
        Derelict d = Forms(Wreck);
        SalvageDerelict order = Ordered(d);
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
        TrackNotifications();

        SalvageDerelict called = Ordered(unbuilt);
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

        SalvageDerelict stopped = Ordered(unbuilt);
        UState.P.DisableSalvageOps = true;
        stopped.Evaluate();
        UState.P.DisableSalvageOps = false;
        Assert.IsFalse(Player.AI.HasGoal(stopped), "setup: salvage ops off ends an order");
        AssertEqual(1, Told().Length, "and tells nothing");

        SalvageDerelict rival = Ordered(theirs, empire: Enemy);
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
        SalvageDerelict order = AtWork(stripped, hold: 100f);
        TrackNotifications();
        UState.P.DisableSalvageWarning = true;

        AllBut5Stripped(stripped);
        order.Evaluate();
        order.Evaluate();
        Assert.IsTrue(order.Dismantling, "setup: a platform sent its last load");
        SalvageDerelict lost = Ordered(claimed);
        Assert.IsTrue(claimed.TryClaim(Enemy), "setup: a rival claims a derelict first");
        lost.Evaluate();
        Assert.IsFalse(Player.AI.HasGoal(lost), "setup: that order ends");
        SalvageDerelict sinks = Ordered(sinking);
        SinksAway(sinking);
        sinks.Evaluate();
        Assert.IsFalse(Player.AI.HasGoal(sinks), "setup: an order's derelict sank");
        Ship ship = ShipOf(order);
        ship.LoadProduction(10f);
        ship.Die(null, cleanupOnly: true);
        MissionOf(order).Evaluate();
        order.Evaluate();
        Assert.IsFalse(Player.AI.HasGoal(order), "setup: a salvage ship with production aboard is lost");
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
        SalvageDerelict order = AtWork(d, hold: 24f);
        order.Evaluate();
        order.Evaluate();
        order.Evaluate();
        AssertEqual(0.01f, 24f, ShipOf(order).GetProduction(), "setup: a load of 24 reaches the salvage ship");
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
