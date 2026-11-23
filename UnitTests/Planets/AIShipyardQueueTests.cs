using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Ship_Game;
using Ship_Game.AI;
using Ship_Game.Commands.Goals;
using Ship_Game.Ships;
using Vector2 = SDGraphics.Vector2;

namespace UnitTests.Planets;

/// <summary>
/// An AI colony builds a shipyard it orders ahead of its refits and every other order, however long those have waited,
/// behind only the items it always builds first, such as an Outpost or Capital.
/// </summary>
[TestClass]
public class AIShipyardQueueTests : StarDriveTest
{
    const string Shipyard = "Shipyard";
    const string Constructor = "Terran Constructor";
    const string Warship = "Rocket Scout";
    readonly Planet Home;

    public AIShipyardQueueTests()
    {
        LoadStarterShips(Shipyard, Constructor, Warship);
        CreateUniverseAndPlayerEmpire();
        Home = AddHomeWorldToEmpire(new Vector2(200_000), Enemy);
        Enemy.AddBuildableShip(ResourceManager.Ships.GetDesign(Constructor));
    }

    [TestMethod]
    public void AnAIColonyBuildsTheShipyardItOrdersAheadOfItsRefitsAndOtherOrders()
    {
        IShipDesign warship = ResourceManager.Ships.GetDesign(Warship);
        for (int i = 0; i < 6; ++i) // each sort adds to the bonus of the orders already waiting
            Home.Construction.Enqueue(warship, QueueItemType.CombatShip);
        var refit = new QueueItem(Home) { isShip = true, ShipData = warship, Cost = 100f, QType = QueueItemType.CombatShip, Goal = new RefitShip(Enemy) };
        Home.Construction.EnqueueRefitShip(refit);
        var shipyard = new BuildOrbital(Home, Shipyard, Enemy);
        for (int i = 0; i < 6; ++i)
            Home.Construction.Enqueue(warship, QueueItemType.CombatShip);

        QueueItem[] queue = Home.Construction.GetConstructionQueue().ToArray();
        string order = string.Join(", ", queue.Select(q => q.Goal == shipyard ? "shipyard" : q == refit ? "refit" : "warship"));
        Assert.IsTrue(order.StartsWith("shipyard, refit, warship"), $"the shipyard first, then the refit, then the warships: {order}");
    }

    [TestMethod]
    public void AnAIShipyardOrderGoesAheadOfTheWarshipsOrderedBeforeIt()
    {
        IShipDesign warship = ResourceManager.Ships.GetDesign(Warship);
        for (int i = 0; i < 6; ++i)
            Home.Construction.Enqueue(warship, QueueItemType.CombatShip);
        var shipyard = new BuildOrbital(Home, Shipyard, Enemy);

        Assert.AreSame(shipyard, Home.Construction.GetConstructionQueue()[0].Goal, "the shipyard goes first, though the warships have gained bonus on every sort");
    }
}
