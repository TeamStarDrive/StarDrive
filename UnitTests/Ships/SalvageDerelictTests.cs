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
/// The Salvage button orders a derelict salvaged when the empire knows of it, salvage ops are on, and it can build a
/// salvage ship or has one with a live salvage unit; otherwise it says what is missing, the research while its race
/// names a salvage ship it cannot yet build, what a design needs while it builds salvage ships no pick takes, and a
/// salvage ship while its race names none. Each derelict takes one order, and an empire may order as many as it
/// likes. Orders placed together reach the sim thread one by one, so a second for the same derelict ends. A claim
/// stops the derelict's timer and holds off other empires, whose orders on it end. Whatever ends an order gives up
/// its claim, so the timer runs on: the button's second click, which calls off the order it finds on reaching the sim
/// thread or gives up a claim kept without one, the clearing of an empire's goals, the derelict that can no longer be
/// salvaged, salvage ops off; a duplicate's end leaves the claim of the order that stands. A claim kept without its
/// order, as a save can catch one, is let go at the turn, while one whose order stands holds. An order and its claim
/// survive a save, which keeps each derelict's id.
/// </summary>
[TestClass]
public class SalvageDerelictTests : SalvageOrderTest
{
    // a salvageable derelict of the player's, so the player knows of it, and its killer too
    Derelict FormsKilledBy(Vector2 at, Empire killer)
    {
        TestShip ship = SpawnShip("Dreadnought mk1-a", Player, at);
        Assert.IsTrue(UState.Derelicts.TryAdd(ship, salvageable: true, seen: false, killer), "setup: a salvageable derelict");
        Universe.InvokePendingSimThreadActions();
        return UState.Derelicts.Current.Last();
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
    public void TheButtonNeedsASalvageShipToBuildOrOneOfItsOwn()
    {
        Derelict first = Forms(Wreck);
        Derelict second = Forms(Wreck + new Vector2(0, 5_000f));
        Derelict third = Forms(Wreck + new Vector2(0, 10_000f));

        PropertyInfo canBuild = typeof(Empire).GetProperty(nameof(Empire.CanBuildSalvageShips))!;
        canBuild.SetValue(Player, false);
        AssertEqual(Block.NoUsableShip, SalvageDerelict.WhyNoOrder(Player, first), "with salvage ships to build and the flag off, as when none passes the checks, and none of its own");
        Assert.IsFalse(SalvageDerelict.Order(Player, first), "no order");
        IShipDesign[] salvageShips = Player.ShipsWeCanBuildSnapshot.Where(s => s.IsSalvageShip).ToArray();
        foreach (IShipDesign s in salvageShips)
            Player.RemoveBuildableShip(s);
        AssertEqual(Block.NoTech, SalvageDerelict.WhyNoOrder(Player, first), "with no salvage ship to build");
        Assert.IsFalse(SalvageDerelict.Order(Player, first), "nor then");

        string salvageShip = Player.data.DefaultSalvageShip;
        Player.data.DefaultSalvageShip = null;
        AssertEqual(Block.NoSalvageShip, SalvageDerelict.WhyNoOrder(Player, first), "nor for a race without a salvage ship");
        Assert.IsFalse(SalvageDerelict.Order(Player, first), "which cannot salvage");
        Ship own = IdleShip(Homeworld.Position + new Vector2(5_000f, 0));
        AssertEqual(Block.None, SalvageDerelict.WhyNoOrder(Player, first), "but a salvage ship of its own can take an order");
        foreach (ShipModule unit in own.Modules)
            if (unit.Salvage)
                unit.Die(null, cleanupOnly: true);
        AssertEqual(Block.NoSalvageShip, SalvageDerelict.WhyNoOrder(Player, first), "unless its salvage unit is destroyed");
        own.QueueTotalRemoval();
        Player.data.DefaultSalvageShip = salvageShip;
        foreach (IShipDesign s in salvageShips)
            Player.AddBuildableShip(s);
        canBuild.SetValue(Player, true);

        AssertEqual(Block.None, SalvageDerelict.WhyNoOrder(Player, first), "and one it can build");
        Assert.IsTrue(SalvageDerelict.Order(Player, first), "and the button orders it");
        AssertEqual(Block.Ordered, SalvageDerelict.WhyNoOrder(Player, first), "which then stands");
        AssertEqual(WreckSystem, OrderOf(Player, first).TargetSystem, "for the derelict's system");

        Assert.IsTrue(SalvageDerelict.Order(Player, second) && SalvageDerelict.Order(Player, third), "it may order as many as it likes");
        AssertEqual(3, Player.AI.CountGoals(g => g is SalvageDerelict), "each its own order");
    }

    [TestMethod]
    public void AnEmpireSalvagesOnlyADerelictItKnowsOfWithSalvageOpsOn()
    {
        Derelict d = Forms(Wreck);
        AssertEqual(Block.Gone, SalvageDerelict.WhyNoOrder(Enemy, d), "an empire that does not know of it");

        FormsKilledBy(Wreck + new Vector2(0, 5_000f), killer: Enemy);
        Assert.IsTrue(d.IsKnownBy(Enemy), "setup: learning of one there, it knows every derelict in the system");
        AssertEqual(Block.None, SalvageDerelict.WhyNoOrder(Enemy, d), "and can order it");

        UState.P.DisableSalvageOps = true;
        AssertEqual(Block.Gone, SalvageDerelict.WhyNoOrder(Enemy, d), "but not with salvage ops off");
    }

    [TestMethod]
    public void OrdersPlacedTogetherReachTheSimThreadOneByOneAndASecondForTheSameDerelictEnds()
    {
        Derelict a = Forms(Wreck);
        Derelict b = Forms(Wreck + new Vector2(0, 5_000f));

        OffTheSimThread(() =>
        {
            Assert.IsTrue(SalvageDerelict.Order(Player, a), "setup: placed from the UI");
            Assert.IsTrue(SalvageDerelict.Order(Player, a), "setup: twice, before the first reaches the sim thread");
            Assert.IsTrue(SalvageDerelict.Order(Player, b), "setup: then another");
        });
        AssertEqual(0, Player.AI.CountGoals(g => g is SalvageDerelict), "they wait for the sim thread");

        Universe.InvokePendingSimThreadActions();
        foreach (SalvageMission mission in Player.AI.FindGoals<SalvageMission>())
            mission.Evaluate();
        AssertEqual(1, Player.AI.CountGoals(g => g is SalvageDerelict s && s.DerelictId == a.Id), "a second order for the same derelict ends");
        AssertEqual(2, Homeworld.ConstructionQueue.Count(q => q.Goal is SalvageMission), "queueing a salvage ship for each order that stands, none for the second");
        Assert.IsNotNull(OrderOf(Player, b), "and the other stands");
    }

    [TestMethod]
    public void AClaimStopsTheTimerAndHoldsOffOtherEmpiresUntilItIsReleased()
    {
        Derelict d = Forms(Wreck);
        int turns = d.TurnsLeft;
        UState.Derelicts.UpdateTurn();
        AssertEqual(turns - 1, d.TurnsLeft, "setup: its timer runs");

        Assert.IsTrue(SalvageDerelict.Order(Player, d), "setup: ordered");
        Assert.IsTrue(d.TryClaim(Player), "an empire's order claims it");
        UState.Derelicts.UpdateTurn();
        AssertEqual(Player, d.Claimant, "which holds at the turn while its order stands");
        AssertEqual(turns - 1, d.TurnsLeft, "and its timer stands");
        Assert.IsFalse(d.TryClaim(Enemy), "another empire cannot claim it");
        AssertEqual(Player, d.Claimant, "it stays the first's");

        d.ReleaseClaim();
        UState.Derelicts.UpdateTurn();
        AssertEqual(turns - 2, d.TurnsLeft, "released, its timer runs on");
        Assert.IsTrue(d.TryClaim(Enemy), "and another empire can claim it");
    }

    [TestMethod]
    public void AnotherEmpiresClaimEndsAnOrder()
    {
        Derelict a = FormsKilledBy(Wreck, killer: Enemy);
        SalvageDerelict.Order(Player, a);

        Assert.IsTrue(a.TryClaim(Enemy), "setup: a rival claims a derelict the player ordered");
        AssertEqual(Block.ClaimedByOther, SalvageDerelict.WhyNoOrder(Player, a), "the button says so");
        OrderOf(Player, a).Evaluate();
        Assert.IsNull(OrderOf(Player, a), "the player's order on it ends");
        AssertEqual(Enemy, a.Claimant, "leaving the rival's claim");
    }

    [TestMethod]
    public void TheButtonsSecondClickCallsTheOrderOffOnTheSimThreadAndGivesUpItsClaim()
    {
        Derelict d = Forms(Wreck);
        SalvageDerelict.Order(Player, d);
        Assert.IsTrue(d.TryClaim(Player), "setup: the order holds its derelict");
        AssertEqual(Block.Ordered, SalvageDerelict.WhyNoOrder(Player, d), "the button is lit");

        OffTheSimThread(() => DerelictSalvageButton.Click(Player, d, DerelictSalvageButton.Kind.Salvage, Block.Ordered, lit: true));
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
        Derelict d = Forms(Wreck);
        SalvageDerelict.Order(Player, d);
        SalvageDerelict clicked = OrderOf(Player, d);
        OffTheSimThread(() => DerelictSalvageButton.Click(Player, d, DerelictSalvageButton.Kind.Salvage, Block.Ordered, lit: true));

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
        Derelict d = Forms(Wreck);
        Assert.IsTrue(d.TryClaim(Player), "setup: a claim with no order behind it, as a save can catch it");
        AssertEqual(Block.Ordered, SalvageDerelict.WhyNoOrder(Player, d), "the button is lit");

        DerelictSalvageButton.Click(Player, d, DerelictSalvageButton.Kind.Salvage, Block.Ordered, lit: true);
        Assert.IsNull(d.Claimant, "and its click gives the claim up");
    }

    [TestMethod]
    public void AClaimKeptWithoutItsOrderIsLetGoAtTheTurn()
    {
        Derelict d = Forms(Wreck);
        int turns = d.TurnsLeft;
        Assert.IsTrue(d.TryClaim(Enemy), "setup: a claim with no order behind it, as a save can catch it");

        UState.Derelicts.UpdateTurn();
        Assert.IsNull(d.Claimant, "the turn lets it go");
        AssertEqual(turns - 1, d.TurnsLeft, "so its timer runs on");
        Assert.IsTrue(d.TryClaim(Player), "and any empire can claim it");
    }

    [TestMethod]
    public void ADuplicateOrderEndingLeavesTheClaimOfTheOrderThatStands()
    {
        Derelict d = Forms(Wreck);
        SalvageDerelict.Order(Player, d);
        SalvageDerelict standing = OrderOf(Player, d);
        Assert.IsTrue(d.TryClaim(Player), "setup: the order holds its derelict");

        var duplicate = new SalvageDerelict(Player, d);
        Player.AI.AddGoal(duplicate);
        standing.Evaluate();
        Assert.IsTrue(Player.AI.HasGoal(g => g == standing), "a later duplicate does not end the order placed before it");
        duplicate.Evaluate();
        Assert.IsFalse(Player.AI.HasGoal(g => g == duplicate), "setup: the duplicate ends");
        Assert.IsTrue(Player.AI.HasGoal(g => g == standing), "the order stands");
        AssertEqual(Player, d.Claimant, "with its claim");
    }

    [TestMethod]
    public void AnEmpireWhoseGoalsAreClearedGivesUpItsClaims()
    {
        Derelict d = Forms(Wreck);
        SalvageDerelict.Order(Player, d);
        d.TryClaim(Player);

        Player.AI.ClearGoals(); // as a defeated or merged empire's are
        Assert.IsNull(d.Claimant, "its claim is given up");
    }

    [TestMethod]
    public void AnOrderEndsOnceItsDerelictCanNoLongerBeSalvagedOrWithSalvageOpsOff()
    {
        Derelict a = Forms(Wreck);
        Derelict b = Forms(Wreck + new Vector2(0, 5_000f));
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
        Derelict other = Forms(Wreck + new Vector2(0, 5_000f));
        Derelict d = Forms(Wreck);
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
