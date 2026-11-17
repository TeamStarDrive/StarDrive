using Microsoft.VisualStudio.TestTools.UnitTesting;
using SDGraphics;
using SDUtils;
using Ship_Game;
using Ship_Game.AI;
using Ship_Game.Empires;
using Ship_Game.Ships;
using UnitTests.Ships;
using Vector2 = SDGraphics.Vector2;

namespace UnitTests.AITests.Ships;

[TestClass]
public class BigShipTargetTests : StarDriveTest
{
    static readonly Vector2 Center = new(300_000, 300_000);
    readonly TestShip Us;

    public BigShipTargetTests()
    {
        LoadStarterShips("Dreadnought mk1-a", "Terran-Prototype", "Fang Strafer", "Maggot Bomber mk1-a", "Assault Shuttle");
        CreateUniverseAndPlayerEmpire();
        UState.Events.Disabled = true;
        UState.Objects.EnableParallelUpdate = false;
        Us = SpawnShip("Dreadnought mk1-a", Player, Center, Vectors.Up);
        Us.AI.SetCombatTriggerDelay(10f);
        Us.AI.OrderHoldPosition(MoveOrder.StandGround | MoveOrder.HoldPosition);
    }

    // an enemy held in place ahead of our ship
    TestShip SpawnAhead(string name, float distance) => SpawnShipNoCombatHoldPos(name, Enemy, Center + Vectors.Up * distance);

    // the target our ship picks once it has scanned the enemies, each of them targeting it
    Ship PickedTarget(params Ship[] enemies)
    {
        RunObjectsSim(new FixedSimTime(EmpireConstants.EnemyScanInterval));
        foreach (Ship enemy in enemies)
            enemy.AI.Target = Us;
        AssertEqual(enemies.Length, Us.AI.PotentialTargets.Length, "setup: our ship must see every enemy");
        return Us.AI.GetHighestPriorityTarget();
    }

    [TestMethod]
    public void ABigShipTakesAShipAtTheEdgeOfItsRangeOverAFighterInItsFace()
    {
        TestShip fighter = SpawnAhead("Fang Strafer", 800f);
        TestShip ship = SpawnAhead("Terran-Prototype", 8000f);

        AssertEqual(ship, PickedTarget(fighter, ship), "a big ship must not turn from a ship to fight a fighter in its face");
    }

    [TestMethod]
    public void ABigShipStillTakesAFighterWhenNothingElseIsNear()
    {
        TestShip fighter = SpawnAhead("Fang Strafer", 800f);

        AssertEqual(fighter, PickedTarget(fighter), "a fighter alone must still be a big ship's target");
    }

    [TestMethod]
    public void ABigShipStillTakesASmallBomberOverAShipOutOfItsRange()
    {
        TestShip bomber = SpawnAhead("Maggot Bomber mk1-a", 1200f);
        AssertTrue(bomber.HasBombs, "setup: the bomber must carry bombs");
        AssertLessThan(bomber.SurfaceArea * 30, Us.SurfaceArea, "setup: the bomber must be far smaller than our ship");
        TestShip ship = SpawnAhead("Terran-Prototype", 14_000f);

        AssertEqual(bomber, PickedTarget(bomber, ship), "a small ship carrying bombs must keep its place as a target");
    }

    [TestMethod]
    public void ABigShipStillTakesAShuttleCarryingTroopsOverAShipOutOfItsRange()
    {
        TestShip shuttle = SpawnAhead("Assault Shuttle", 800f);
        ResourceManager.CreateTroop("Wyvern", Enemy).LandOnShip(shuttle);
        AssertGreaterThan(shuttle.TroopCount, 0, "setup: the shuttle must carry troops");
        AssertLessThan(shuttle.SurfaceArea * 30, Us.SurfaceArea, "setup: the shuttle must be far smaller than our ship");
        TestShip ship = SpawnAhead("Terran-Prototype", 14_000f);

        AssertEqual(shuttle, PickedTarget(shuttle, ship), "a small ship carrying troops must keep its place as a target");
    }
}
