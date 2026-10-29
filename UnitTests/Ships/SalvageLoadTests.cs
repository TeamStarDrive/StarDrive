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
/// Once its derelict is stripped empty, or the Salvage button's second click calls the order off, the platform turns
/// back into the salvage ship over two turns, shown as a build from start to end, and is gone, its salvage ship
/// carrying home what it stripped. The click gives up the claim at once, so the derelict can be ordered again while the
/// old platform turns back, a rival's claim included, and the old order's end leaves a new order's claim alone. A lost
/// platform ends its order with its claim; one in other hands while it turns back stays whole. A called-off order
/// turning back survives a save.
/// </summary>
[TestClass]
public class SalvageLoadTests : SalvageOrderTest
{
    // an earlier claim of the enemy's stripped all but 5
    void AllBut5Stripped(Derelict d) => UState.Derelicts.Strip(d, null, d.ProductionLeft - 5f);

    static string Status(SalvageDerelict order) => ShipInfoUIElement.SalvageStatus(order.Platform, order);

    [TestMethod]
    public void AnEmptiedDerelictsPlatformTurnsBackIntoTheShipOverTwoTurns()
    {
        Derelict d = Forms(Wreck);
        SalvageDerelict order = AtWork(d);
        Ship platform = order.Platform;
        AllBut5Stripped(d);

        order.Evaluate();
        Assert.IsTrue(order.Emptied && order.Dismantling && platform.Dismantling, "stripping the 5 left, it starts to turn back into the ship");
        AssertEqual(Localizer.Token(GameText.PlatformTurningBack), Status(order), "as its status says");
        AssertEqual(0f, platform.DismantlePercent, "shown as a build that starts");

        RunObjectsSim(UState.P.TurnTimer);
        AssertEqual(2f, 50f, platform.DismantlePercent, "half way after a turn");
        order.Evaluate();
        Assert.IsTrue(platform.Active, "it stands through the first turn");

        RunObjectsSim(UState.P.TurnTimer);
        AssertEqual(2f, 100f, platform.DismantlePercent, "and complete after two");
        order.Evaluate();
        Assert.IsFalse(platform.Active, "then it is gone");
        Assert.IsFalse(Player.AI.HasGoal(order), "its order done");
        AssertEqual(0.01f, 5f, Player.AI.FindGoals<DeliverRefund>().Single().Load, "its salvage ship carrying home the 5");
    }

    [TestMethod]
    public void ACalledOffPlatformFinishesThoughARivalClaimsTheDerelict()
    {
        Derelict d = Forms(Wreck);
        SalvageDerelict order = AtWork(d);
        order.Evaluate();
        SalvageDerelict.Cancel(Player, d);
        Assert.IsTrue(d.TryClaim(Enemy), "setup: a rival claims the derelict once the claim is given up");

        order.Evaluate();
        Assert.IsTrue(Player.AI.HasGoal(order), "the called-off order stands on");
        Assert.IsTrue(order.Dismantling, "its platform turning back");
        AssertEqual(0.01f, 12f, order.Load, "with what it stripped");
        AssertEqual(Enemy, d.Claimant, "leaving the rival's claim");
    }

    [TestMethod]
    public void APlatformInOtherHandsWhileTurningBackStaysWhole()
    {
        Derelict d = Forms(Wreck);
        SalvageDerelict order = AtWork(d);
        Ship platform = order.Platform;
        AllBut5Stripped(d);
        order.Evaluate();
        RunObjectsSim(UState.P.TurnTimer);
        Assert.IsTrue(platform.Dismantling && platform.DismantlePercent > 0f, "setup: the platform turns back");

        platform.LoyaltyChangeByGift(Enemy, addNotification: false);
        UState.Objects.UpdateLists();
        AssertEqual(Enemy, platform.Loyalty, "setup: the platform changes hands, as only the debug tools can make it");

        order.Evaluate();
        Assert.IsFalse(Player.AI.HasGoal(order), "its order ends");
        Assert.IsTrue(platform.Active, "its new owner keeps the platform");
        Assert.IsFalse(platform.Dismantling, "which no longer turns back");
        AssertEqual(0f, platform.DismantlePercent, "its dismantling undone");
    }

    [TestMethod]
    public void ALostPlatformEndsItsOrderAndGivesUpItsClaim()
    {
        Derelict d = Forms(Wreck);
        SalvageDerelict order = AtWork(d);

        order.Platform.Die(null, cleanupOnly: true);
        order.Evaluate();
        Assert.IsFalse(Player.AI.HasGoal(order), "a lost platform ends its order");
        Assert.IsNull(d.Claimant, "and its claim");
    }

    [TestMethod]
    public void TheSecondClickTurnsThePlatformBackWhileTheDerelictCanBeOrderedAgain()
    {
        Derelict d = Forms(Wreck);
        SalvageDerelict order = AtWork(d);
        Ship platform = order.Platform;
        order.Evaluate();
        float left = d.ProductionLeft;
        AssertEqual(Block.Ordered, SalvageDerelict.WhyNoOrder(Player, d), "setup: the button is lit");

        DerelictSalvageButton.Click(Player, d, DerelictSalvageButton.Kind.Salvage, Block.Ordered, lit: true);
        Assert.IsTrue(order.CalledOff && Player.AI.HasGoal(order), "the click calls the order off, which winds down");
        Assert.IsNull(d.Claimant, "giving up the claim at once");
        Assert.IsTrue(platform.Active, "with its platform");
        AssertEqual(Block.None, SalvageDerelict.WhyNoOrder(Player, d), "so the derelict can be ordered again");
        AssertEqual(Localizer.Token(GameText.SalvageFinished), Status(order), "and the platform's salvage is finished");

        order.Evaluate();
        AssertEqual(0.01f, left, d.ProductionLeft, "the platform strips no more");
        Assert.IsTrue(order.Dismantling, "and turns back into the ship");

        SalvageDerelict again = Ordered(d);
        Assert.AreNotSame(order, again, "a new order is the derelict's now");
        Assert.IsTrue(d.TryClaim(Player), "setup: its salvage ship starts to deploy");
        SalvageDerelict.Cancel(Player, d);
        Assert.IsFalse(Player.AI.HasGoal(again), "a new order called off before its platform stands ends");
        Assert.IsNull(d.Claimant, "with its claim, though the old order still winds down");

        Ordered(d);
        Assert.IsTrue(d.TryClaim(Player), "setup: ordered once more, and claimed");
        order.Evaluate();
        order.Evaluate();
        Assert.IsFalse(Player.AI.HasGoal(order), "the old order ends with its platform");
        AssertEqual(Player, d.Claimant, "leaving the new order's claim");
    }

    [TestMethod]
    public void ACalledOffOrderTurningBackSurvivesSaveAndLoad()
    {
        Derelict d = Forms(Wreck);
        SalvageDerelict order = AtWork(d);
        order.Evaluate();
        SalvageDerelict.Cancel(Player, d);
        order.Evaluate();
        RunObjectsSim(UState.P.TurnTimer);
        order.Evaluate();
        float percent = order.Platform.DismantlePercent;
        AssertEqual(2f, 50f, percent, "setup: half way through turning back");

        UState.StarDate = 1001f; // the loader needs a stardate past 1000
        SavedGame save = Universe.Save("UnitTest.SalvageLoad", throwOnError: true);
        UniverseScreen loaded = LoadGame.Load(save.SaveFile, noErrorDialogs: true, startSimThread: false);
        Assert.IsNotNull(loaded, "setup: the save loads");

        var loadedOrder = (SalvageDerelict)loaded.UState.Player.AI.FindGoal(g => g is SalvageDerelict);
        Assert.IsTrue(loadedOrder.CalledOff && loadedOrder.Dismantling, "the order is called off and turning back");
        AssertEqual(0.01f, 12f, loadedOrder.Load, "with what it stripped");
        Ship platform = loadedOrder.Platform;
        Assert.IsTrue(platform.Dismantling, "and so is its platform");
        AssertEqual(0.01f, percent, platform.DismantlePercent, "as far along");
        loadedOrder.Evaluate();
        Assert.IsFalse(platform.Active, "its last turn takes it away");
        Assert.IsTrue(loaded.UState.Player.AI.HasGoal(g => g is DeliverRefund), "its salvage ship bringing the 12 home");
    }
}
