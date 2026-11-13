using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Ship_Game;
using Ship_Game.AI;
using Ship_Game.Commands.Goals;
using Ship_Game.Ships;
using Vector2 = SDGraphics.Vector2;

namespace UnitTests.Planets;

/// <summary>
/// On Hard and above, an AI colony that finishes the refit at the front of its queue spends what is left of the turn's
/// production on the refits right behind it: one more, and one more per shipyard.
/// </summary>
[TestClass]
public class AIRefitsFinishedTogetherTests : StarDriveTest
{
    Planet Home;
    IShipDesign Warship;

    void CreateColony(GameDifficulty difficulty, int shipyards = 0, bool playerOwned = false)
    {
        LoadStarterShips("Shipyard", "Rocket Scout");
        CreateUniverseAndPlayerEmpire();
        Empire owner = playerOwned ? Player : Enemy;
        UState.P.Difficulty = difficulty;
        UState.Objects.EnableParallelUpdate = false;
        Home = AddHomeWorldToEmpire(new Vector2(200_000), owner);
        Home.CType = Planet.ColonyType.Colony;
        Home.PS = Planet.GoodState.IMPORT; // a colony importing by hand spends all its stored production each turn
        Home.Food.Percent = 0f;
        Home.Res.Percent = 0f;
        Home.Prod.Percent = 1f;
        Home.UpdateIncomes();
        Home.ProdHere = 0;
        Warship = ResourceManager.Ships.GetDesign("Rocket Scout");
        for (int i = 0; i < shipyards; ++i)
            SpawnShip("Shipyard", owner, Home.Position + new Vector2(0, 2500 + 500 * i)).TetherToPlanet(Home);
        Assert.AreEqual(shipyards, Home.NumShipyards, "setup: the colony counts its shipyards");
    }

    QueueItem Refit(Goal goal = null) => new(Home)
    {
        isShip = true, ShipData = Warship, Cost = 6f, QType = QueueItemType.CombatShip, Goal = goal ?? new RefitShip(Home.Owner)
    };

    QueueItem Build() => new(Home) { isShip = true, ShipData = Warship, Cost = 6f, QType = QueueItemType.CombatShip };

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
    [DataRow(GameDifficulty.Hard,   0, 2)]
    [DataRow(GameDifficulty.Brutal, 1, 3)]
    [DataRow(GameDifficulty.Insane, 3, 5)]
    public void OnHardAndAboveAnAIColonyFinishesOneMoreRefitBehindTheFirstAndOneMorePerShipyard(GameDifficulty difficulty,
        int shipyards, int finished)
    {
        CreateColony(difficulty, shipyards);
        QueueItem[] refits = { Refit(), Refit(), Refit(), Refit(), Refit(), Refit() };
        QueueInOrder(refits);
        float cost = refits[0].ActualCost;
        SpendOneTurn(cost * (finished + 1)); // enough for one refit more than may finish

        for (int i = 0; i < refits.Length; ++i)
            Assert.AreEqual(i < finished, Finished(refits[i]), $"refit {i + 1} of {refits.Length} with {shipyards} shipyards");
        AssertEqual(0.01f, 0f, refits[finished].ProductionSpent, "the refit after the last one allowed gets nothing");
        AssertEqual(0.01f, cost, Home.ProdHere, "only the refits finished are paid for");
    }

    [TestMethod]
    public void TheLastRefitInTheChainTakesWhatIsLeftEvenIfItDoesNotFinish()
    {
        CreateColony(GameDifficulty.Hard, shipyards: 1);
        QueueItem first = Refit(), second = Refit();
        QueueInOrder(first, second);
        float cost = first.ActualCost;
        SpendOneTurn(cost * 1.5f);

        Assert.IsTrue(Finished(first), "the first refit finishes");
        AssertEqual(0.01f, cost * 0.5f, second.ProductionSpent, "the next refit takes what is left");
        Assert.IsFalse(Finished(second), "setup: what is left does not finish it");
    }

    [TestMethod]
    public void ACancelledRefitBehindTheFirstGetsNothing()
    {
        CreateColony(GameDifficulty.Hard, shipyards: 1);
        QueueItem first = Refit(), cancelled = Refit();
        QueueInOrder(first, cancelled);
        cancelled.IsCancelled = true;
        SpendOneTurn(first.ActualCost * 3);

        Assert.IsTrue(Finished(first), "setup: the first refit finishes");
        AssertEqual(0.01f, 0f, cancelled.ProductionSpent, "a cancelled refit takes no production, of which a cancel would refund only half");
    }

    [TestMethod]
    [DataRow(false)]
    [DataRow(true)]
    public void AnAIColonyKeepsItsRefitsTogetherAtTheFrontOfItsQueue(bool colonyShipsGoFirst)
    {
        CreateColony(GameDifficulty.Hard);
        if (colonyShipsGoFirst)
        {
            Enemy.TestInitModifiers();
            Enemy.AI.EndWarFromEvent(Player);
            Enemy.UpdateRelationships(takeTurn: false);
        }
        Assert.AreEqual(colonyShipsGoFirst, Enemy.ColonyShipsGoFirst, "setup: whether the AI's colony ships go first, which sorts its queue another way");
        QueueItem first = Refit(), second = Refit(), build = Build();
        build.Cost = 1000f;
        build.ProductionSpent = 500f; // half built, which the AI's own ranking puts ahead of a refit
        Home.Construction.EnqueueRefitShip(first);
        Home.Construction.EnqueueRefitShip(build);
        Home.Construction.EnqueueRefitShip(second);
        var queue = Home.Construction.GetConstructionQueue();
        Assert.IsTrue(queue[0].IsRefit && queue[1].IsRefit,
            $"the refits stay together at the front: {string.Join(", ", queue.Select(q => q.IsRefit ? "refit" : "build"))}");

        SpendOneTurn(first.ActualCost * 2);
        Assert.IsTrue(Finished(first) && Finished(second), "so both finish in one turn");
    }

    [TestMethod]
    public void AnAIColonyWorksOnItsOldestRefitFirst()
    {
        CreateColony(GameDifficulty.Hard);
        QueueItem older = Refit(), newer = Refit();
        older.Cost = newer.Cost = 1000f; // many turns from done, so nothing keeps a refit just queued behind the one ahead
        Home.Construction.EnqueueRefitShip(older);
        Home.Construction.EnqueueRefitShip(newer);
        var queue = Home.Construction.GetConstructionQueue();
        Assert.AreSame(older, queue[0], "the refit queued first stays first, rather than the newest jumping ahead of it");
        Assert.AreSame(newer, queue[1], "the newer refit waits right behind it");
    }

    [TestMethod]
    public void ABuildBehindTheFirstRefitStopsTheChain()
    {
        CreateColony(GameDifficulty.Hard, shipyards: 3);
        QueueItem refit = Refit(), build = Build(), laterRefit = Refit();
        QueueInOrder(refit, build, laterRefit);
        SpendOneTurn(refit.ActualCost * 3);

        Assert.IsTrue(Finished(refit), "setup: the refit finishes");
        AssertEqual(0.01f, 0f, build.ProductionSpent, "a build that is no refit gets nothing");
        AssertEqual(0.01f, 0f, laterRefit.ProductionSpent, "nor does a refit behind that build");
    }

    [TestMethod]
    public void NothingIsPassedOnWhenTheFirstItemIsNoRefit()
    {
        CreateColony(GameDifficulty.Hard, shipyards: 3);
        QueueItem build = Build(), refit = Refit();
        QueueInOrder(build, refit);
        SpendOneTurn(build.ActualCost * 3);

        Assert.IsTrue(Finished(build), "setup: the build finishes");
        AssertEqual(0.01f, 0f, refit.ProductionSpent, "a refit behind a finished build gets nothing");
    }

    [TestMethod]
    public void AnOrbitalRefitCountsAsARefit()
    {
        CreateColony(GameDifficulty.Hard);
        QueueItem first = Refit(new RefitOrbital(Enemy)), second = Refit(new RefitOrbital(Enemy));
        QueueInOrder(first, second);
        SpendOneTurn(first.ActualCost * 3);

        Assert.IsTrue(Finished(first), "setup: the first orbital refit finishes");
        Assert.IsTrue(Finished(second), "an orbital refit passes what is left to the orbital refit behind it");
    }

    [TestMethod]
    [DataRow(GameDifficulty.Normal, false)]
    [DataRow(GameDifficulty.Insane, true)]
    public void ThePlayerAndAnAIOnNormalPassNothingOn(GameDifficulty difficulty, bool player)
    {
        CreateColony(difficulty, shipyards: 3, playerOwned: player);
        QueueItem first = Refit(), second = Refit();
        QueueInOrder(first, second);
        SpendOneTurn(first.ActualCost * 3);

        Assert.IsTrue(Finished(first), "setup: the first refit finishes");
        AssertEqual(0.01f, 0f, second.ProductionSpent, "the next refit gets nothing");
    }
}
