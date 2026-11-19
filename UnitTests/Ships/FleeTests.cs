using Microsoft.VisualStudio.TestTools.UnitTesting;
using SDGraphics;
using Ship_Game;
using Ship_Game.AI;
using Ship_Game.Ships;
using Vector2 = SDGraphics.Vector2;

namespace UnitTests.Ships;

[TestClass]
public class FleeTests : StarDriveTest
{
    public FleeTests()
    {
        LoadStarterShips("Terran-Prototype", "Owlwok Freighter S");
        CreateUniverseAndPlayerEmpire();
    }

    // an AI ship facing a player ship 8000 to its right, with a safe planet of its own at the given point
    Ship FacingAnEnemy(Vector2 safePlanetAt)
    {
        AddDummyPlanetToEmpire(safePlanetAt, Enemy);
        Ship fleeing = SpawnShip("Terran-Prototype", Enemy, Vector2.Zero);
        Ship enemy = SpawnShip("Terran-Prototype", Player, new Vector2(8000f, 0f));
        fleeing.Rotation = new Vector2(1f, 0f).ToRadians();
        fleeing.AI.PotentialTargets = new[] { enemy };
        fleeing.AI.BadGuysNear = true;
        return fleeing;
    }

    [TestMethod]
    public void AShipFleesStraightToASafePlanetThatLiesAwayFromTheEnemy()
    {
        Ship fleeing = FacingAnEnemy(new Vector2(-400_000f, 0f));

        fleeing.AI.OrderFlee();

        ShipAI.ShipGoal first = fleeing.AI.OrderQueue.PeekFirst;
        Assert.AreEqual(ShipAI.Plan.Orbit, first?.Plan, "it does not first run on toward the enemy it faces");
    }

    [TestMethod]
    public void AShipWhoseSafePlanetLiesPastTheEnemyFirstBreaksAwayFromIt()
    {
        Ship fleeing = FacingAnEnemy(new Vector2(400_000f, 0f));

        fleeing.AI.OrderFlee();

        ShipAI.ShipGoal first = fleeing.AI.OrderQueue.PeekFirst;
        Assert.AreNotEqual(ShipAI.Plan.Orbit, first?.Plan, "setup: it first breaks away");
        Assert.IsTrue(first.MovePosition.X < -50_000f, $"away from the enemy, not through it ({first.MovePosition})");
    }

    [TestMethod]
    public void AShipFleesFromTheEnemyWarshipNotFromTheFreightersAround()
    {
        Ship fleeing = FacingAnEnemy(new Vector2(0f, 400_000f));
        var near = new Ship[4];
        near[0] = fleeing.AI.PotentialTargets[0];
        for (int i = 1; i < near.Length; ++i)
            near[i] = SpawnShip("Owlwok Freighter S", Player, new Vector2(-8000f, 1000f * i));
        Assert.AreEqual(0f, near[1].GetStrength(), "setup: a freighter cannot fight");
        fleeing.AI.PotentialTargets = near;

        fleeing.AI.OrderFlee();

        ShipAI.ShipGoal first = fleeing.AI.OrderQueue.PeekFirst;
        Assert.AreNotEqual(ShipAI.Plan.Orbit, first?.Plan, "setup: it first breaks away");
        Assert.IsTrue(first.MovePosition.X < -50_000f, $"away from the warship, though three freighters sit on the other side ({first.MovePosition})");
    }
}
