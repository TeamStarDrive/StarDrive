using Microsoft.VisualStudio.TestTools.UnitTesting;
using Ship_Game;
using Vector2 = SDGraphics.Vector2;
#pragma warning disable CA2213

namespace UnitTests.Ships;

[TestClass]
public class TractorBeamTests : StarDriveTest
{
    public TractorBeamTests()
    {
        CreateUniverseAndPlayerEmpire();
    }

    [TestMethod]
    public void AShipHeldPastItsMassStandsStillInsteadOfShaking()
    {
        TestShip ship = SpawnShip("Vulcan Scout", Enemy, new Vector2(200_000));
        ship.CauseTractorDamage(ship.Mass * 3f, hittingShields: false);
        ship.ShipStatusChange();

        AssertEqual(0f, ship.MaxSTLSpeed, "a ship held past its mass has no speed left");
        AssertEqual(0f, ship.RotationRadsPerSecond, "a ship held past its mass cannot turn");

        ship.Velocity = new Vector2(100f, 0f);
        ship.Update(TestSimStep);
        AssertLessThan(ship.Velocity.Length(), 0.001f, "the held ship stops, it does not fly backwards");
        ship.Update(TestSimStep);
        AssertLessThan(ship.Velocity.Length(), 0.001f, "and it stays put, so shooters aim at where it is");
    }

    [TestMethod]
    public void AShipIsReleasedAtTheFirstStatusUpdateAfterTheBeamLetsGo()
    {
        TestShip free = SpawnShip("Vulcan Scout", Enemy, new Vector2(200_000));
        TestShip held = SpawnShip("Vulcan Scout", Enemy, new Vector2(-200_000));
        held.CauseTractorDamage(held.Mass * 0.5f, hittingShields: false);
        held.ShipStatusChange();
        AssertEqual(0.01f, free.MaxSTLSpeed * 0.5f, held.MaxSTLSpeed, "setup: half the ship's mass in pull halves its speed");

        free.Update(FixedSimTime.One);
        held.Update(FixedSimTime.One);
        AssertEqual(0.01f, free.MaxSTLSpeed * 0.5f, held.MaxSTLSpeed, "the beam hit since the last status update, so the ship is still held");

        free.Update(FixedSimTime.One);
        held.Update(FixedSimTime.One);
        AssertEqual(0.01f, free.MaxSTLSpeed, held.MaxSTLSpeed, "no hit for a whole second: the ship gets its speed back at once");
    }
}
