using Microsoft.VisualStudio.TestTools.UnitTesting;
using Ship_Game.Ships;
using Vector2 = SDGraphics.Vector2;
#pragma warning disable CA2213

namespace UnitTests.Ships;

[TestClass]
public class ShipPositionPrecisionTests : StarDriveTest
{
    public ShipPositionPrecisionTests()
    {
        LoadStarterShips("Vulcan Scout");
        CreateUniverseAndPlayerEmpire();
    }

    [TestMethod]
    [DataRow(true, DisplayName = "constant velocity")]
    [DataRow(false, DisplayName = "velocity verlet")]
    public void ASlowShipFarFromTheCentreMovesAtHalfGameSpeed(bool isZeroAcc)
    {
        var start = new Vector2(-891685.3f, -17109896f);
        var velocity = new Vector2(-3.54f, -118f);
        Ship ship = SpawnShip("Vulcan Scout", Player, Vector2.Zero);
        ship.Position = start;
        ship.Velocity = velocity;
        ship.Acceleration = Vector2.Zero;

        const float halfSpeedStep = 1f / 120f;
        for (int i = 0; i < 120; ++i)
            ship.UpdateVelocityAndPosition(halfSpeedStep, Vector2.Zero, isZeroAcc);

        AssertEqual(0.0625f, velocity.X, ship.Position.X - start.X, "one second of travel on X");
        AssertEqual(2f, velocity.Y, ship.Position.Y - start.Y, "one second of travel on Y");
    }

    [TestMethod]
    public void AShipRecoversFromABadStepOnceItsPositionIsReset()
    {
        Ship ship = SpawnShip("Vulcan Scout", Player, Vector2.Zero);
        ship.Acceleration = Vector2.Zero;
        ship.Velocity = new Vector2(float.NaN, float.NaN);
        ship.UpdateVelocityAndPosition(TestSimStep.FixedTime, Vector2.Zero, isZeroAcc: true);

        var start = new Vector2(1000f, 1000f);
        var velocity = new Vector2(60f, -30f);
        ship.Position = start;
        ship.Velocity = velocity;
        ship.UpdateVelocityAndPosition(1f, Vector2.Zero, isZeroAcc: true);

        AssertEqual(0.01f, start + velocity, ship.Position, "a reset ship must move on from where it was put");
    }
}
