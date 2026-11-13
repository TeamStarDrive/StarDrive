using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Ship_Game;
using Ship_Game.AI;
using Ship_Game.Commands.Goals;
using Ship_Game.Ships;
using Vector2 = SDGraphics.Vector2;

namespace UnitTests.Planets;

/// <summary>
/// A colony that finishes the first item in its queue spends what is left of the turn's production on the items right
/// behind it, up to its infrastructure in items per turn, whatever the items are and whoever owns the colony.
/// </summary>
[TestClass]
public class ItemsFinishedPerTurnTests : StarDriveTest
{
    Planet Home;
    IShipDesign Warship;

    void CreateColony(float infrastructure, bool playerOwned = false)
    {
        LoadStarterShips("Rocket Scout");
        CreateUniverseAndPlayerEmpire();
        UState.Objects.EnableParallelUpdate = false;
        Home = AddHomeWorldToEmpire(new Vector2(200_000), playerOwned ? Player : Enemy);
        Home.CType = Planet.ColonyType.Colony;
        Home.PS = Planet.GoodState.IMPORT; // a colony importing by hand spends all its stored production each turn
        Home.Food.Percent = 0f;
        Home.Res.Percent = 0f;
        Home.Prod.Percent = 1f;
        Home.UpdateIncomes();
        Home.ProdHere = 0;
        typeof(Planet).GetProperty(nameof(Planet.InfraStructure))!.SetValue(Home, infrastructure); // as its buildings would give it
        AssertEqual(0.001f, infrastructure, Home.InfraStructure, "setup: the colony's infrastructure");
        Warship = ResourceManager.Ships.GetDesign("Rocket Scout");
    }

    QueueItem Build(float cost = 2f) => new(Home) { isShip = true, ShipData = Warship, Cost = cost, QType = QueueItemType.CombatShip };

    QueueItem Refit() => new(Home)
    {
        isShip = true, ShipData = Warship, Cost = 2f, QType = QueueItemType.CombatShip, Goal = new RefitShip(Home.Owner)
    };

    QueueItem Troop() => new(Home) { isTroop = true, TroopType = "Test Troop", Cost = 2f, QType = QueueItemType.Troop };

    void QueueInOrder(params QueueItem[] items)
    {
        foreach (QueueItem item in items)
            Home.Construction.EnqueueRefitShip(item);
        for (int i = 0; i < items.Length; ++i)
            Home.Construction.Reorder(items[i], i);
        var queue = Home.Construction.GetConstructionQueue();
        for (int i = 0; i < items.Length; ++i)
            Assert.AreSame(items[i], queue[i], "setup: the queue is in the order given");
    }

    void SpendOneTurn(float production)
    {
        Home.Storage.Max = 10_000f;
        Home.ProdHere = production;
        AssertEqual(0.01f, production, Home.ProdHere, "setup: the colony holds the production");
        float budget = Home.LimitedProductionExpenditure(Home.CurrentProductionToQueue);
        AssertEqual(0.01f, production, budget, "setup: the colony spends all it holds this turn");
        Home.Construction.AutoApplyProduction(0f);
    }

    bool Finished(QueueItem item) => item.IsComplete && !Home.Construction.GetConstructionQueue().Contains(item);

    [TestMethod]
    [DataRow(1f,   false, 1)]
    [DataRow(2.5f, true,  2)]
    [DataRow(5f,   false, 5)]
    public void AColonyFinishesUpToItsInfrastructureInItemsEachTurn(float infrastructure, bool playerOwned, int finished)
    {
        CreateColony(infrastructure, playerOwned);
        QueueItem[] items = { Build(), Refit(), Build(), Refit(), Build(), Refit() };
        QueueInOrder(items);
        float cost = items[0].ActualCost;
        SpendOneTurn(cost * (finished + 1)); // enough for one item more than may finish

        for (int i = 0; i < items.Length; ++i)
            Assert.AreEqual(i < finished, Finished(items[i]), $"item {i + 1} of {items.Length} with infrastructure {infrastructure}");
        AssertEqual(0.01f, 0f, items[finished].ProductionSpent, "the item after the last one allowed gets nothing");
        AssertEqual(0.01f, cost, Home.ProdHere, "only the items finished are paid for");
    }

    [TestMethod]
    public void TheLastItemInTheChainTakesWhatIsLeftEvenIfItDoesNotFinish()
    {
        CreateColony(infrastructure: 3f);
        QueueItem first = Build(), second = Build();
        QueueInOrder(first, second);
        float cost = first.ActualCost;
        SpendOneTurn(cost * 1.5f);

        Assert.IsTrue(Finished(first), "the first item finishes");
        AssertEqual(0.01f, cost * 0.5f, second.ProductionSpent, "the next item takes what is left");
        Assert.IsFalse(Finished(second), "setup: what is left does not finish it");
    }

    [TestMethod]
    public void ACancelledItemBehindTheFirstGetsNothing()
    {
        CreateColony(infrastructure: 3f);
        QueueItem first = Build(), cancelled = Build();
        QueueInOrder(first, cancelled);
        cancelled.IsCancelled = true;
        SpendOneTurn(first.ActualCost * 3);

        Assert.IsTrue(Finished(first), "setup: the first item finishes");
        AssertEqual(0.01f, 0f, cancelled.ProductionSpent, "a cancelled item takes no production, of which a cancel would refund only half");
    }

    [TestMethod]
    public void ATroopTheColonyHasNoRoomForGetsNothing()
    {
        CreateColony(infrastructure: 3f);
        foreach (var tile in Home.TilesList)
            tile.MaxAllowedTroops = 0;
        QueueItem build = Build(), troop = Troop();
        QueueInOrder(build, troop);
        SpendOneTurn(build.ActualCost * 3);

        Assert.IsTrue(Finished(build), "setup: the build finishes");
        AssertEqual(0.01f, 0f, troop.ProductionSpent, "a troop with no room for it is cancelled, so it takes no production, of which the cancel would refund only half");
    }

    [TestMethod]
    [DataRow(false)]
    [DataRow(true)]
    public void WithRoomForOneTroopTheTurnPaysForOneTroopOnly(bool troopFirst)
    {
        CreateColony(infrastructure: 4f);
        foreach (var tile in Home.TilesList)
            tile.MaxAllowedTroops = tile.TroopsHere.Count;
        var open = Home.TilesList.First(t => t.Building == null || t.Building.CombatStrength == 0);
        open.MaxAllowedTroops = open.TroopsHere.Count + 1;
        QueueItem first = Troop(), second = Troop();
        if (troopFirst)
            QueueInOrder(first, second);
        else
            QueueInOrder(Build(), first, second);
        SpendOneTurn(first.ActualCost * 4);

        AssertEqual(0.01f, first.ActualCost, first.ProductionSpent, "setup: the first troop is paid for");
        AssertEqual(0.01f, 0f, second.ProductionSpent, "the first troop takes the only room, so the second takes no production, of which its cancel would refund only half");
    }

    [TestMethod]
    public void ARushPassesNothingOnToTheItemBehind()
    {
        CreateColony(infrastructure: 3f, playerOwned: true);
        QueueItem first = Build(), second = Build();
        QueueInOrder(first, second);
        Home.Storage.Max = 10_000f;
        Home.ProdHere = first.ActualCost * 3;
        Player.AddMoney(10_000f);
        Assert.IsTrue(Home.Construction.RushProduction(0, first.ActualCost * 3), "setup: the rush goes through");

        Assert.IsTrue(Finished(first), "setup: the rushed item finishes");
        AssertEqual(0.01f, 0f, second.ProductionSpent, "a rush passes nothing on to the item behind");
    }
}
