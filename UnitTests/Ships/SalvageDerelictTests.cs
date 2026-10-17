using System.Linq;
using System.Reflection;
using System.Threading;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Ship_Game;
using Ship_Game.Commands.Goals;
using Ship_Game.GameScreens.LoadGame;
using Ship_Game.Ships;
using Block = Ship_Game.Commands.Goals.SalvageDerelict.Block;
using Vector2 = SDGraphics.Vector2;

namespace UnitTests.Ships;

/// <summary>
/// The Salvage button orders a derelict salvaged when the empire knows of it, salvage ops are on, and the empire has a
/// built salvage station in its system with a free salvage unit: each live unit works one order, and every order of
/// the empire's there takes one. Orders placed together reach the sim thread one by one, so the first ones hold the
/// units and a second for the same derelict ends; an order beyond the live units, as after a unit dies, ends, and so
/// does one whose station is lost, while an order ahead that is ending holds no unit. A station in other hands, its
/// takeover still queued, no longer counts for its old owner. A claim stops the derelict's timer and holds off other
/// empires, whose orders on it end and free their units. Whatever ends an order gives up its claim, so the timer runs
/// on: the button's second click, which calls off the order it finds on reaching the sim thread or gives up a claim
/// kept without one, the clearing of an empire's goals, the derelict that can no longer be salvaged, salvage ops off;
/// a duplicate's end leaves the claim of the order that stands. An order and its claim survive a save, which keeps
/// each derelict's id.
/// </summary>
[TestClass]
public class SalvageDerelictTests : StarDriveTest
{
    static readonly Vector2 SystemSite = new(600_000f, 600_000f);
    static readonly Vector2 InSystem = SystemSite + new Vector2(30_000f, 0);
    static readonly Vector2 BuildPos = SystemSite + new Vector2(40_000f, 0);
    readonly SolarSystem WreckSystem;
    readonly IShipDesign Station;

    public SalvageDerelictTests()
    {
        LoadStarterShips("Dreadnought mk1-a", "Terran Basic Salvage Station");
        CreateUniverseAndPlayerEmpire();
        WreckSystem = AddDummyPlanet(SystemSite, 1f, 1f, 1f, SystemSite + new Vector2(0, 60_000f), explored: true).System;
        AddHomeWorldToEmpire(new Vector2(200_000f), Player);
        Assert.IsTrue(ResourceManager.Ships.GetDesign("Terran Basic Salvage Station", out Station), "setup: the station is loaded");
        AssertEqual(2, (int)Station.NumSalvageModules, "setup: the station has two salvage units");
    }

    [TestCleanup]
    public override void Cleanup()
    {
        UState.Derelicts.Clear();
        base.Cleanup();
    }

    // a salvageable derelict of the player's, so the player knows of it, and its killer too
    Derelict Forms(Vector2 at, Empire killer = null)
    {
        TestShip ship = SpawnShip("Dreadnought mk1-a", Player, at);
        Assert.IsTrue(UState.Derelicts.TryAdd(ship, salvageable: true, seen: false, killer), "setup: a salvageable derelict");
        Universe.InvokePendingSimThreadActions();
        return UState.Derelicts.Current.Last();
    }

    Ship BuiltStation(Empire owner)
    {
        var order = new SalvageOps(owner, WreckSystem, BuildPos, Station);
        owner.AI.AddGoal(order);
        order.AdvanceToNextStep(); // past the queueing, which needs a port
        order.TargetShip = SpawnShip(Station.Name, owner, BuildPos);
        order.Evaluate();
        AssertEqual("Salvage", order.StepName, "setup: the station is built");
        return order.TargetShip;
    }

    static SalvageDerelict OrderOf(Empire empire, Derelict d) => SalvageDerelict.OrderOf(empire, d);

    void OffTheSimThread(System.Action action)
    {
        FieldInfo simThread = typeof(UniverseScreen).GetField("SimThread", BindingFlags.Instance | BindingFlags.NonPublic);
        simThread.SetValue(Universe, new Thread(() => { }));
        try
        {
            action();
        }
        finally
        {
            simThread.SetValue(Universe, null);
        }
    }

    [TestMethod]
    public void TheButtonNeedsABuiltStationOfTheEmpiresInTheSystemWithAFreeSalvageUnit()
    {
        Derelict first = Forms(InSystem);
        Derelict second = Forms(InSystem + new Vector2(0, 5_000f));
        Derelict third = Forms(InSystem + new Vector2(0, 10_000f));
        AssertEqual(Block.NoStation, SalvageDerelict.WhyNoOrder(Player, first), "no station there");
        Assert.IsFalse(SalvageDerelict.Order(Player, first), "so no order");

        var onOrder = new SalvageOps(Player, WreckSystem, BuildPos, Station);
        Player.AI.AddGoal(onOrder);
        AssertEqual(Block.NoStation, SalvageDerelict.WhyNoOrder(Player, first), "nor while it is on order");
        Player.AI.RemoveGoal(onOrder);

        BuiltStation(Player);
        AssertEqual(Block.None, SalvageDerelict.WhyNoOrder(Player, first), "a built one with a free unit");
        Assert.IsTrue(SalvageDerelict.Order(Player, first), "takes the order");
        AssertEqual(Block.Ordered, SalvageDerelict.WhyNoOrder(Player, first), "which then stands");
        AssertEqual(WreckSystem, OrderOf(Player, first).TargetSystem, "for the derelict's system");

        Assert.IsTrue(SalvageDerelict.Order(Player, second), "its second unit takes another");
        AssertEqual(Block.NoFreeUnit, SalvageDerelict.WhyNoOrder(Player, third), "and a third has no free unit");
        Assert.IsFalse(SalvageDerelict.Order(Player, third), "so no order");
        AssertEqual(2, Player.AI.CountGoals(g => g is SalvageDerelict), "two orders stand");
    }

    [TestMethod]
    public void AnEmpireSalvagesOnlyADerelictItKnowsOfWithSalvageOpsOn()
    {
        Derelict d = Forms(InSystem);
        BuiltStation(Enemy);
        AssertEqual(Block.Gone, SalvageDerelict.WhyNoOrder(Enemy, d), "an empire that does not know of it");

        Forms(InSystem + new Vector2(0, 5_000f), killer: Enemy);
        Assert.IsTrue(d.IsKnownBy(Enemy), "setup: learning of one there, it knows every derelict in the system");
        AssertEqual(Block.None, SalvageDerelict.WhyNoOrder(Enemy, d), "and can order it");

        UState.P.DisableSalvageOps = true;
        AssertEqual(Block.Gone, SalvageDerelict.WhyNoOrder(Enemy, d), "but not with salvage ops off");
    }

    [TestMethod]
    public void OrdersPlacedTogetherReachTheSimThreadOneByOneAndTheFirstOnesHoldTheUnits()
    {
        Derelict a = Forms(InSystem);
        Derelict b = Forms(InSystem + new Vector2(0, 5_000f));
        Derelict c = Forms(InSystem + new Vector2(0, 10_000f));
        BuiltStation(Player);

        OffTheSimThread(() =>
        {
            Assert.IsTrue(SalvageDerelict.Order(Player, a), "setup: placed from the UI");
            Assert.IsTrue(SalvageDerelict.Order(Player, a), "setup: twice, before the first reaches the sim thread");
            Assert.IsTrue(SalvageDerelict.Order(Player, b), "setup: then another");
            Assert.IsTrue(SalvageDerelict.Order(Player, c), "setup: and one more than the station has units");
        });
        AssertEqual(0, Player.AI.CountGoals(g => g is SalvageDerelict), "they wait for the sim thread");

        Universe.InvokePendingSimThreadActions();
        AssertEqual(1, Player.AI.CountGoals(g => g is SalvageDerelict s && s.DerelictId == a.Id), "a second order for the same derelict ends");
        Assert.IsNotNull(OrderOf(Player, b), "the first ones hold the units");
        Assert.IsNull(OrderOf(Player, c), "and the one beyond them ends");
    }

    [TestMethod]
    public void AnOrderBeyondTheLiveUnitsEndsAndSoDoesOneWhoseStationIsLost()
    {
        Derelict a = Forms(InSystem);
        Derelict b = Forms(InSystem + new Vector2(0, 5_000f));
        Ship station = BuiltStation(Player);
        SalvageDerelict.Order(Player, a);
        SalvageDerelict.Order(Player, b);
        SalvageDerelict first = OrderOf(Player, a), second = OrderOf(Player, b);

        station.Modules.First(m => m.Salvage).Die(null, cleanupOnly: true);
        AssertEqual(1, SalvageDerelict.LiveUnits(station), "setup: a salvage unit dies");
        second.Evaluate(); // the goals are evaluated last to first
        first.Evaluate();
        Assert.IsNull(OrderOf(Player, b), "the later order ends");
        Assert.IsNotNull(OrderOf(Player, a), "the first keeps the unit left");

        station.Die(null, cleanupOnly: true);
        first.Evaluate();
        Assert.IsNull(OrderOf(Player, a), "and ends once the station is lost");
    }

    [TestMethod]
    public void AStationWhoseChangeOfHandsWaitsForTheSimThreadTakesNoOrderOfItsOldOwners()
    {
        Derelict d = Forms(InSystem);
        Ship station = BuiltStation(Player);
        station.KillAllTroops();
        ResourceManager.CreateTroop("Wyvern", Enemy).LandOnShip(station);
        station.LoyaltyChangeFromBoarding(Enemy, addNotification: false);
        OffTheSimThread(() => UState.Objects.UpdateLists());
        AssertEqual(Enemy, station.Loyalty, "setup: the station changes hands off the sim thread");
        Assert.IsNotNull(Player.AI.FindGoal(g => g.IsSalvageOpsGoal(WreckSystem)), "setup: its takeover is queued, so the old order stands");
        AssertEqual(Block.NoStation, SalvageDerelict.WhyNoOrder(Player, d), "the button counts no station of the player's there");
    }

    [TestMethod]
    public void AClaimStopsTheTimerAndHoldsOffOtherEmpiresUntilItIsReleased()
    {
        Derelict d = Forms(InSystem);
        int turns = d.TurnsLeft;
        UState.Derelicts.UpdateTurn();
        AssertEqual(turns - 1, d.TurnsLeft, "setup: its timer runs");

        Assert.IsTrue(d.TryClaim(Player), "an empire claims it");
        UState.Derelicts.UpdateTurn();
        AssertEqual(turns - 1, d.TurnsLeft, "and its timer stands");
        Assert.IsFalse(d.TryClaim(Enemy), "another empire cannot claim it");
        AssertEqual(Player, d.Claimant, "it stays the first's");

        d.ReleaseClaim();
        UState.Derelicts.UpdateTurn();
        AssertEqual(turns - 2, d.TurnsLeft, "released, its timer runs on");
        Assert.IsTrue(d.TryClaim(Enemy), "and another empire can claim it");
    }

    [TestMethod]
    public void AnotherEmpiresClaimEndsAnOrderAndFreesItsUnit()
    {
        Derelict a = Forms(InSystem, killer: Enemy);
        Derelict b = Forms(InSystem + new Vector2(0, 5_000f));
        Derelict c = Forms(InSystem + new Vector2(0, 10_000f));
        BuiltStation(Player);
        SalvageDerelict.Order(Player, a);
        SalvageDerelict.Order(Player, b);
        AssertEqual(Block.NoFreeUnit, SalvageDerelict.WhyNoOrder(Player, c), "setup: both units are busy");

        Assert.IsTrue(a.TryClaim(Enemy), "setup: a rival claims a derelict the player ordered");
        AssertEqual(Block.ClaimedByOther, SalvageDerelict.WhyNoOrder(Player, a), "the button says so");
        OrderOf(Player, a).Evaluate();
        Assert.IsNull(OrderOf(Player, a), "the player's order on it ends");
        AssertEqual(Enemy, a.Claimant, "leaving the rival's claim");
        AssertEqual(Block.None, SalvageDerelict.WhyNoOrder(Player, c), "and its unit is free again");
    }

    [TestMethod]
    public void TheButtonsSecondClickCallsTheOrderOffOnTheSimThreadAndGivesUpItsClaim()
    {
        Derelict d = Forms(InSystem);
        BuiltStation(Player);
        SalvageDerelict.Order(Player, d);
        Assert.IsTrue(d.TryClaim(Player), "setup: the order holds its derelict");
        AssertEqual(Block.Ordered, SalvageDerelict.WhyNoOrder(Player, d), "the button is lit");

        OffTheSimThread(() => DerelictSalvageButton.Click(Player, d, Block.Ordered));
        Assert.IsNotNull(OrderOf(Player, d), "the click waits for the sim thread");
        AssertEqual(Player, d.Claimant, "and so does the claim");

        Universe.InvokePendingSimThreadActions();
        Assert.IsNull(OrderOf(Player, d), "where the order ends");
        Assert.IsNull(d.Claimant, "with its claim");
        AssertEqual(Block.None, SalvageDerelict.WhyNoOrder(Player, d), "so it can be ordered again");
        int turns = d.TurnsLeft;
        UState.Derelicts.UpdateTurn();
        AssertEqual(turns - 1, d.TurnsLeft, "and its timer runs on");
    }

    [TestMethod]
    public void TheSecondClickCallsOffTheOrderStandingWhenItReachesTheSimThread()
    {
        Derelict d = Forms(InSystem);
        BuiltStation(Player);
        SalvageDerelict.Order(Player, d);
        SalvageDerelict clicked = OrderOf(Player, d);
        OffTheSimThread(() => DerelictSalvageButton.Click(Player, d, Block.Ordered));

        Player.AI.RemoveGoal(clicked);
        SalvageDerelict.Order(Player, d);
        Assert.IsTrue(d.TryClaim(Player), "setup: before the click arrives, a new order stands and holds the derelict");

        Universe.InvokePendingSimThreadActions();
        Assert.IsNull(OrderOf(Player, d), "the click calls off the order it finds there");
        Assert.IsNull(d.Claimant, "and gives up that order's claim with it");
    }

    [TestMethod]
    public void TheButtonGivesUpAClaimKeptWithoutItsOrder()
    {
        Derelict d = Forms(InSystem);
        Assert.IsTrue(d.TryClaim(Player), "setup: a claim with no order behind it, as a save can catch it");
        AssertEqual(Block.Ordered, SalvageDerelict.WhyNoOrder(Player, d), "the button is lit");

        DerelictSalvageButton.Click(Player, d, Block.Ordered);
        Assert.IsNull(d.Claimant, "and its click gives the claim up");
    }

    [TestMethod]
    public void ADuplicateOrderEndingLeavesTheClaimOfTheOrderThatStands()
    {
        Derelict d = Forms(InSystem);
        BuiltStation(Player);
        SalvageDerelict.Order(Player, d);
        SalvageDerelict standing = OrderOf(Player, d);
        Assert.IsTrue(d.TryClaim(Player), "setup: the order holds its derelict");

        var duplicate = new SalvageDerelict(Player, d);
        Player.AI.AddGoal(duplicate);
        duplicate.Evaluate();
        Assert.IsFalse(Player.AI.HasGoal(g => g == duplicate), "setup: the duplicate ends");
        Assert.IsTrue(Player.AI.HasGoal(g => g == standing), "the order stands");
        AssertEqual(Player, d.Claimant, "with its claim");
    }

    [TestMethod]
    public void AnOrderAheadThatIsEndingHoldsNoUnit()
    {
        Derelict a = Forms(InSystem);
        Derelict b = Forms(InSystem + new Vector2(0, 5_000f));
        Ship station = BuiltStation(Player);
        SalvageDerelict.Order(Player, a);
        SalvageDerelict.Order(Player, b);
        SalvageDerelict first = OrderOf(Player, a), second = OrderOf(Player, b);

        station.Modules.First(m => m.Salvage).Die(null, cleanupOnly: true);
        Assert.IsTrue(a.TryClaim(Enemy), "setup: in the same turn a unit dies and a rival claims the first order's derelict");
        second.Evaluate(); // the goals are evaluated last to first
        first.Evaluate();
        Assert.IsNull(OrderOf(Player, a), "the first order ends");
        Assert.IsNotNull(OrderOf(Player, b), "and the later one keeps the unit left");
    }

    [TestMethod]
    public void AnEmpireWhoseGoalsAreClearedGivesUpItsClaims()
    {
        Derelict d = Forms(InSystem);
        BuiltStation(Player);
        SalvageDerelict.Order(Player, d);
        d.TryClaim(Player);

        Player.AI.ClearGoals(); // as a defeated or merged empire's are
        Assert.IsNull(d.Claimant, "its claim is given up");
    }

    [TestMethod]
    public void AnOrderEndsOnceItsDerelictCanNoLongerBeSalvagedOrWithSalvageOpsOff()
    {
        Derelict a = Forms(InSystem);
        Derelict b = Forms(InSystem + new Vector2(0, 5_000f));
        BuiltStation(Player);
        SalvageDerelict.Order(Player, a);
        SalvageDerelict.Order(Player, b);
        SalvageDerelict first = OrderOf(Player, a), second = OrderOf(Player, b);

        UState.P.DisableSalvageOps = true;
        first.Evaluate();
        Assert.IsNull(OrderOf(Player, a), "salvage ops off ends an order");
        UState.P.DisableSalvageOps = false;

        for (int turn = 0; turn < 10_000 && b.CanBeSalvaged; ++turn)
            UState.Derelicts.UpdateTurn();
        Assert.IsFalse(b.CanBeSalvaged, "setup: its turns run out");
        second.Evaluate();
        Assert.IsNull(OrderOf(Player, b), "and so does a derelict that can no longer be salvaged");
    }

    [TestMethod]
    public void AnOrderAndItsClaimSurviveSaveAndLoad()
    {
        Derelict other = Forms(InSystem + new Vector2(0, 5_000f));
        Derelict d = Forms(InSystem);
        BuiltStation(Player);
        SalvageDerelict.Order(Player, d);
        d.TryClaim(Player);
        Assert.AreNotEqual(d.Id, other.Id, "setup: each derelict has its own id");
        Assert.AreSame(d, UState.Derelicts.Current[1], "setup: the ordered one formed second, so finding it takes its id");

        UState.StarDate = 1001f; // the loader needs a stardate past 1000
        SavedGame save = Universe.Save("UnitTest.SalvageDerelict", throwOnError: true);
        UniverseScreen loaded = LoadGame.Load(save.SaveFile, noErrorDialogs: true, startSimThread: false);
        Assert.IsNotNull(loaded, "setup: the save loads");

        Empire player = loaded.UState.Player;
        var order = (SalvageDerelict)player.AI.FindGoal(g => g is SalvageDerelict);
        Assert.IsNotNull(order, "the order loads");
        Derelict target = order.Derelict;
        Assert.IsNotNull(target, "and finds its derelict by its id");
        AssertEqual(d.Id, target.Id, "the same one");
        AssertEqual(d.Position, target.Position, "where it lay");
        AssertEqual(player, target.Claimant, "still claimed");
        AssertEqual(Block.Ordered, SalvageDerelict.WhyNoOrder(player, target), "with the button lit");

        int turns = target.TurnsLeft;
        loaded.UState.Derelicts.UpdateTurn();
        AssertEqual(turns, target.TurnsLeft, "and its timer standing");
        order.Evaluate();
        Assert.IsNotNull(SalvageDerelict.OrderOf(player, target), "the order goes on in the loaded game");
    }
}
