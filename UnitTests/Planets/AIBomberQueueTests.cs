using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Ship_Game;
using Ship_Game.Ships;
using Vector2 = SDGraphics.Vector2;

namespace UnitTests.Planets;

/// <summary>
/// At war with a major empire, the AI's new bomber order goes ahead of the warship orders already waiting in its
/// colony's queue, which would otherwise keep it behind them all.
/// </summary>
[TestClass]
public class AIBomberQueueTests : StarDriveTest
{
    const string Warship = "Rocket Scout";
    const string Bomber = "Neutron Bomber Mk1";
    readonly Planet Home;

    public AIBomberQueueTests()
    {
        LoadStarterShips(Warship, Bomber);
        CreateUniverseAndPlayerEmpire();
        Home = AddHomeWorldToEmpire(new Vector2(200_000), Enemy);
        Enemy.UpdateRelationships(takeTurn: false); // lists the war with the player among its active wars
        Assert.AreEqual(RoleName.bomber, ResourceManager.Ships.GetDesign(Bomber).Role, "setup: a bomber");
        Assert.AreNotEqual(RoleName.bomber, ResourceManager.Ships.GetDesign(Warship).Role, "setup: a warship that is no bomber");
    }

    // four warships queued one by one, each sort adding to the bonus of those already waiting, then a bomber
    int BomberPlaceAfterFourWarships()
    {
        for (int i = 0; i < 4; ++i)
            Home.Construction.Enqueue(ResourceManager.Ships.GetDesign(Warship), QueueItemType.CombatShip);
        Home.Construction.Enqueue(ResourceManager.Ships.GetDesign(Bomber), QueueItemType.CombatShip);
        var queue = Home.Construction.GetConstructionQueue().ToArray();
        AssertEqual(5, queue.Length, "setup: the five orders");
        return System.Array.FindIndex(queue, q => q.ShipData.Role == RoleName.bomber);
    }

    [TestMethod]
    public void AtWarWithAMajorEmpireTheAIsNewBomberGoesAheadOfTheWarshipsWaiting()
    {
        Assert.IsTrue(Enemy.IsAtWarWithMajorEmpire, "setup: at war with the player");
        AssertEqual(0, BomberPlaceAfterFourWarships(), "the bomber is built first");
    }

    [TestMethod]
    public void TheAIsNewBomberStaysBehindAWarshipBeingBuiltAndAnEarlierBomber()
    {
        Assert.IsTrue(Enemy.IsAtWarWithMajorEmpire, "setup: at war with the player");
        IShipDesign warship = ResourceManager.Ships.GetDesign(Warship), bomber = ResourceManager.Ships.GetDesign(Bomber);
        Home.Construction.Enqueue(warship, QueueItemType.CombatShip);
        QueueItem started = Home.Construction.GetConstructionQueue()[0];
        started.ProductionSpent = 1f; // under way, though far from done
        Home.Construction.Enqueue(warship, QueueItemType.CombatShip);
        Home.Construction.Enqueue(warship, QueueItemType.CombatShip);
        Home.Construction.Enqueue(bomber, QueueItemType.CombatShip);
        Home.Construction.Enqueue(warship, QueueItemType.CombatShip);
        Home.Construction.Enqueue(bomber, QueueItemType.CombatShip);

        QueueItem[] queue = Home.Construction.GetConstructionQueue().ToArray();
        string order = string.Join(", ", queue.Select(q => q == started ? "started" : q.ShipData.Role == RoleName.bomber ? "bomber" : "warship"));
        Assert.AreEqual("started, bomber, bomber, warship, warship, warship", order,
                        "the warship under way first, then the bombers in the order they came, then the warships waiting");
    }

    [TestMethod]
    public void AtPeaceTheAIsNewBomberWaitsBehindTheWarships()
    {
        Enemy.AI.EndWarFromEvent(Player);
        Enemy.UpdateRelationships(takeTurn: false);
        Assert.IsFalse(Enemy.IsAtWarWithMajorEmpire, "setup: at peace");
        AssertEqual(4, BomberPlaceAfterFourWarships(), "the bomber waits behind the warships queued before it");
    }
}
