using Microsoft.VisualStudio.TestTools.UnitTesting;
using Ship_Game;
using Ship_Game.AI;
using Ship_Game.Commands.Goals;
using Ship_Game.Ships;
using Vector2 = SDGraphics.Vector2;

namespace UnitTests.Planets;

[TestClass]
public class AIColonyShipQueueTests : StarDriveTest
{
    [TestMethod]
    [DataRow(GameDifficulty.Hard,   false, QueueItemType.ColonyShip,      false, false, true)]
    [DataRow(GameDifficulty.Brutal, false, QueueItemType.ColonyShip,      false, false, true)]
    [DataRow(GameDifficulty.Insane, false, QueueItemType.ColonyShip,      false, false, true)]
    [DataRow(GameDifficulty.Hard,   false, QueueItemType.ColonyShipClaim, false, false, true)]
    [DataRow(GameDifficulty.Hard,   false, QueueItemType.ColonyShip,      false, true,  true)]
    [DataRow(GameDifficulty.Normal, false, QueueItemType.ColonyShip,      false, false, false)]
    [DataRow(GameDifficulty.Hard,   true,  QueueItemType.ColonyShip,      false, false, false)]
    [DataRow(GameDifficulty.Hard,   false, QueueItemType.ColonyShipClaim, true,  false, false)]
    public void OnHardAndAboveAnAIColonyShipForAChosenPlanetGoesFirstInItsQueue(GameDifficulty difficulty, bool atWar,
        QueueItemType colonyShipType, bool standbyShip, bool outpostQueued, bool goesFirst)
    {
        CreateUniverseAndPlayerEmpire();
        UState.P.Difficulty = difficulty;
        Enemy.TestInitModifiers();
        if (!atWar)
            Enemy.AI.EndWarFromEvent(Player);
        Enemy.UpdateRelationships(takeTurn: false);
        Assert.AreEqual(atWar, Enemy.IsAtWarWithMajorEmpire, "setup: Enemy's war state");

        Planet home = AddHomeWorldToEmpire(new Vector2(200_000), Enemy);
        home.ProdHere = 0;
        Assert.AreEqual(0, home.Construction.Count, "setup: the queue starts empty");
        IShipDesign warship = ResourceManager.GetShipTemplate("Rocket Scout").ShipData;
        IShipDesign colonyShip = ResourceManager.GetShipTemplate("Colony Ship").ShipData;

        // a warship well under way, which the AI's own scoring keeps ahead of the colony ship
        home.Construction.Enqueue(warship, QueueItemType.CombatShip);
        QueueItem underWay = home.Construction.GetConstructionQueue()[0];
        underWay.Cost = 1_000_000;
        underWay.ProductionSpent = 500_000;

        if (outpostQueued)
            Assert.IsTrue(home.Construction.Enqueue(ResourceManager.CreateBuilding(home, Building.OutpostId)), "setup: an Outpost is queued");

        Goal goal = standbyShip ? new StandbyColonyShip(Enemy) : new MarkForColonization(Enemy) { TargetPlanet = home };
        goal.PlanetBuildingAt = home;
        home.Construction.Enqueue(colonyShip, colonyShipType, goal);
        // many turns from done, so a refit never slots in behind it
        FindItem(home, goal).Cost = 1_000_000;
        home.Construction.Enqueue(warship, QueueItemType.CombatShip);

        var refit = new QueueItem(home)
        {
            isShip   = true,
            ShipData = warship,
            Cost     = 50f,
            QType    = QueueItemType.CombatShip,
            Goal     = new RefitShip(Enemy),
        };
        home.Construction.EnqueueRefitShip(refit);

        var queue = home.Construction.GetConstructionQueue();
        int ahead = outpostQueued ? 1 : 0;
        Assert.AreEqual(4 + ahead, queue.Count);
        if (goesFirst)
        {
            if (outpostQueued)
                Assert.IsTrue(queue[0].isBuilding && queue[0].Building.IsOutpost, "an Outpost goes ahead of the colony ship");
            Assert.AreSame(goal, queue[ahead].Goal, "the colony ship goes ahead of a warship queued before it and one queued after it");
            Assert.AreSame(refit, queue[ahead + 1], "a refit goes right behind the colony ship");
        }
        else
        {
            Assert.AreSame(refit, queue[0], "a refit goes first");
            Assert.AreSame(underWay, queue[1], "the colony ship stays behind the warship the AI's scoring puts ahead of it");
        }
    }

    static QueueItem FindItem(Planet planet, Goal goal)
    {
        foreach (QueueItem q in planet.Construction.GetConstructionQueue())
            if (q.Goal == goal)
                return q;
        throw new AssertFailedException("setup: the goal's item is not in the queue");
    }

    [TestMethod]
    public void APortIsTimedForALeadingColonyShipByTheItemsKeptFirstOnly()
    {
        CreateUniverseAndPlayerEmpire();
        UState.P.Difficulty = GameDifficulty.Hard;
        Enemy.TestInitModifiers();
        Enemy.AI.EndWarFromEvent(Player);
        Enemy.UpdateRelationships(takeTurn: false);
        Assert.IsTrue(Enemy.ColonyShipsGoFirst, "setup: Enemy's colony ships go first");

        Planet home = AddHomeWorldToEmpire(new Vector2(200_000), Enemy);
        IShipDesign warship = ResourceManager.GetShipTemplate("Rocket Scout").ShipData;
        IShipDesign colonyShip = ResourceManager.GetShipTemplate("Colony Ship").ShipData;
        float cost = colonyShip.GetCost(Enemy);
        int onAnEmptyPort = home.TurnsUntilQueueComplete(cost, 1f, colonyShip, goesFirst: true);
        Assert.IsTrue(onAnEmptyPort < 9999, "setup: the port can build the colony ship");

        home.Construction.Enqueue(warship, QueueItemType.CombatShip);
        home.Construction.GetConstructionQueue()[0].Cost = 1_000_000;
        Assert.AreEqual(onAnEmptyPort, home.TurnsUntilQueueComplete(cost, 1f, colonyShip, goesFirst: true),
                        "a leading colony ship does not wait for the warship");
        Assert.IsTrue(home.TurnsUntilQueueComplete(cost, 1f, colonyShip) > onAnEmptyPort, "any other ship waits for the warship");

        var goal = new MarkForColonization(Enemy) { TargetPlanet = home, PlanetBuildingAt = home };
        home.Construction.Enqueue(colonyShip, QueueItemType.ColonyShip, goal);
        QueueItem leading = home.Construction.GetConstructionQueue()[0];
        Assert.AreSame(goal, leading.Goal, "setup: the queued colony ship leads");
        leading.Cost = 1_000_000;
        Assert.IsTrue(home.TurnsUntilQueueComplete(cost, 1f, colonyShip, goesFirst: true) > onAnEmptyPort,
                      "a leading colony ship waits for the colony ship ahead of it");
    }
}
