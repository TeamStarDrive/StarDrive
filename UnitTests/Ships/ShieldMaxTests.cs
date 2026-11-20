using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Ship_Game;
using Ship_Game.Ships;
using Vector2 = SDGraphics.Vector2;

namespace UnitTests.Ships;

[TestClass]
public class ShieldMaxTests : StarDriveTest
{
    const string ShieldShip = "TEST_ShipShield"; // two shields, amplified past their base power

    public ShieldMaxTests()
    {
        CreateUniverseAndPlayerEmpire();
        LoadStarterShips(ShieldShip);
        UState.Objects.EnableParallelUpdate = false;
    }

    // a ship with full shields that just lost an amplifier
    Ship ShipThatLostAnAmplifier() => ShipThatLostAnAmplifier(out _);

    Ship ShipThatLostAnAmplifier(out float excess)
    {
        Ship ship = SpawnShip(ShieldShip, Player, new Vector2(10_000f));
        ShipModule[] shields = ship.GetShields().ToArray();
        float fullMax = shields[0].ActualShieldPowerMax;
        foreach (ShipModule shield in shields)
            AssertEqual(0.01f, shield.ActualShieldPowerMax, shield.ShieldPower, "setup: the shields are full");

        ship.GetAmplifiers().First().SetHealth(0, "Test");
        ship.SetLastDamagedBy(null); // the hit that took it keeps the ship from repairing it at once
        ship.ShipStatusChange();

        AssertLessThan(shields[0].ActualShieldPowerMax, fullMax, "setup: the shields' max dropped with the amplifier");
        AssertGreaterThan(shields[0].ShieldPower, shields[0].ActualShieldPowerMax, "setup: the shield still holds the power of its old max");
        excess = shields[0].ShieldPower - shields[0].ActualShieldPowerMax;
        return ship;
    }

    // modules, shields among them, update once a second
    void RunAShieldUpdate()
    {
        RunObjectsSim(FixedSimTime.One);
        RunObjectsSim(FixedSimTime.One);
    }

    [TestMethod]
    public void AShieldWhoseMaxDroppedKeepsNoMoreThanItsNewMax()
    {
        Ship ship = ShipThatLostAnAmplifier();

        RunAShieldUpdate();

        foreach (ShipModule shield in ship.GetShields())
            AssertEqual(0.01f, shield.ActualShieldPowerMax, shield.ShieldPower, "no power is kept above the new max");
    }

    [TestMethod]
    public void AHitOnAShieldWhoseMaxDroppedShowsAtOnce()
    {
        Ship ship = ShipThatLostAnAmplifier(out float excess);
        RunAShieldUpdate();

        ship.GetShields().First().DamageShield(excess / 2f, null, out _); // less than the excess the old max left
        ship.UpdateShields();

        AssertLessThan(ship.ShieldPower, ship.ShieldMax, "the hit takes from the shield shown, not from power hidden above the max");
    }
}
