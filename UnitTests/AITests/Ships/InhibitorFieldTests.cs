using Microsoft.VisualStudio.TestTools.UnitTesting;
using Ship_Game.Ships;
using UnitTests.Ships;
using Vector2 = SDGraphics.Vector2;

namespace UnitTests.AITests.Ships;

/// <summary>
/// Only a live inhibitor still owned by the empire that lists it holds that empire's enemies out of warp.
/// </summary>
[TestClass]
public class InhibitorFieldTests : StarDriveTest
{
    const string DreadnoughtName = "Dreadnought mk1-a";
    const string SlaverName = "Corsair-Slaver";
    static readonly Vector2 ShipPos = new(300_000f, 300_000f);

    public InhibitorFieldTests()
    {
        LoadStarterShips(DreadnoughtName, SlaverName);
        CreateUniverseAndPlayerEmpire();
        UState.Objects.EnableParallelUpdate = false;
    }

    TestShip PlayerShip()
    {
        TestShip ship = SpawnShip(DreadnoughtName, Player, ShipPos);
        ship.AI.SetCombatTriggerDelay(1000f);
        return ship;
    }

    // an enemy slaver beside the ship, its inhibitor listed the way the enemy's turn lists it
    TestShip SlaverBeside(Ship ship)
    {
        TestShip slaver = SpawnShip(SlaverName, Enemy, ship.Position + new Vector2(ship.Radius + 2000f, 0f));
        slaver.AI.SetCombatTriggerDelay(1000f);
        AssertGreaterThan(slaver.InhibitionRadius, slaver.Position.Distance(ship.Position) + 3000f, "setup: the slaver's inhibitor reaches the ship");
        Enemy.Inhibitors.Add(slaver);
        Assert.IsTrue(ship.IsInhibitedFromEnemyShips(), "setup: the slaver holds the ship out of warp");
        return slaver;
    }

    [TestMethod]
    public void AKilledInhibitorLetsShipsGoBeforeItsEmpiresNextTurn()
    {
        TestShip ship = PlayerShip();
        TestShip slaver = SlaverBeside(ship);
        slaver.Die(slaver, cleanupOnly: true);

        Assert.IsTrue(Enemy.Inhibitors.Contains(slaver), "setup: the enemy lists the dead slaver until its next turn");
        Assert.IsFalse(ship.IsInhibitedFromEnemyShips(), "a dead slaver holds no one");
    }

    [TestMethod]
    public void ATumblingInhibitorLetsShipsGo()
    {
        TestShip ship = PlayerShip();
        TestShip slaver = SlaverBeside(ship);
        slaver.Dying = true;

        Assert.IsTrue(slaver.Active, "setup: a tumbling ship is still active");
        Assert.IsFalse(ship.IsInhibitedFromEnemyShips(), "a tumbling wreck holds no one");
    }

    [TestMethod]
    public void ACapturedInhibitorNoLongerHoldsShipsForItsOldOwner()
    {
        TestShip ship = PlayerShip();
        TestShip slaver = SlaverBeside(ship);
        slaver.LoyaltyChangeFromBoarding(Player, addNotification: false);
        RunObjectsSim(TestSimStep);

        Assert.IsTrue(slaver.Active && !slaver.Dying, "setup: the captured slaver is alive");
        AssertGreaterThan(slaver.InhibitionRadius, slaver.Position.Distance(ship.Position), "setup: the captured slaver's inhibitor still reaches the ship");
        Assert.AreSame(Player, slaver.Loyalty, "setup: the player took the slaver");
        Assert.IsTrue(Enemy.Inhibitors.Contains(slaver), "setup: the enemy lists the slaver until its next turn");
        Assert.IsFalse(ship.IsInhibitedFromEnemyShips(), "the captured slaver does not hold its new owner's ships");
    }
}
