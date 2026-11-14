using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Ship_Game;
using Ship_Game.AI;
using Ship_Game.Commands.Goals;
using Ship_Game.Ships;
using Vector2 = SDGraphics.Vector2;

namespace UnitTests.Planets;

/// <summary>
/// The AI keeps its refits together right behind the items it always builds first, oldest first.
/// </summary>
[TestClass]
public class AIRefitQueueOrderTests : StarDriveTest
{
    Planet Home;
    IShipDesign Warship;

    void CreateColony()
    {
        LoadStarterShips("Rocket Scout");
        CreateUniverseAndPlayerEmpire();
        UState.Objects.EnableParallelUpdate = false;
        Home = AddHomeWorldToEmpire(new Vector2(200_000), Enemy);
        Warship = ResourceManager.Ships.GetDesign("Rocket Scout");
    }

    QueueItem Refit(float cost) => new(Home)
    {
        isShip = true, ShipData = Warship, Cost = cost, QType = QueueItemType.CombatShip, Goal = new RefitShip(Enemy)
    };

    [TestMethod]
    [DataRow(false)]
    [DataRow(true)]
    public void AnAIColonyKeepsItsRefitsTogetherAtTheFrontOfItsQueue(bool colonyShipsGoFirst)
    {
        CreateColony();
        if (colonyShipsGoFirst)
        {
            UState.P.Difficulty = GameDifficulty.Hard;
            Enemy.TestInitModifiers();
            Enemy.AI.EndWarFromEvent(Player);
            Enemy.UpdateRelationships(takeTurn: false);
        }
        Assert.AreEqual(colonyShipsGoFirst, Enemy.ColonyShipsGoFirst, "setup: whether the AI's colony ships go first, which sorts its queue another way");
        QueueItem first = Refit(6f), second = Refit(6f);
        var build = new QueueItem(Home) { isShip = true, ShipData = Warship, Cost = 1000f, ProductionSpent = 500f, QType = QueueItemType.CombatShip };
        Home.Construction.EnqueueRefitShip(first);
        Home.Construction.EnqueueRefitShip(build); // half built, which the AI's own ranking puts ahead of a refit
        Home.Construction.EnqueueRefitShip(second);

        var queue = Home.Construction.GetConstructionQueue();
        Assert.IsTrue(queue[0].IsRefit && queue[1].IsRefit,
            $"the refits stay together at the front: {string.Join(", ", queue.Select(q => q.IsRefit ? "refit" : "build"))}");
    }

    [TestMethod]
    public void AnAIColonyWorksOnItsOldestRefitFirst()
    {
        CreateColony();
        QueueItem older = Refit(1000f), newer = Refit(1000f); // many turns from done, so nothing keeps a refit just queued behind the one ahead
        Home.Construction.EnqueueRefitShip(older);
        Home.Construction.EnqueueRefitShip(newer);

        var queue = Home.Construction.GetConstructionQueue();
        Assert.AreSame(older, queue[0], "the refit queued first stays first, rather than the newest jumping ahead of it");
        Assert.AreSame(newer, queue[1], "the newer refit waits right behind it");
    }
}
