using Microsoft.VisualStudio.TestTools.UnitTesting;
using Vector2 = SDGraphics.Vector2;
#pragma warning disable CA2213

namespace UnitTests.Ships;

[TestClass]
public class ExternalForceTests : StarDriveTest
{
    public ExternalForceTests()
    {
        CreateUniverseAndPlayerEmpire();
    }

    [TestMethod]
    public void APushAlongEitherAxisAloneMovesTheShip()
    {
        TestShip alongX = SpawnShip("Vulcan Scout", Player, new Vector2(200_000));
        alongX.ApplyForce(new Vector2(1_000_000f, 0f));
        alongX.UpdateVelocityAndPosition(TestSimStep);
        AssertGreaterThan(alongX.Velocity.X, 0f, "a push along X must move the ship");

        TestShip alongY = SpawnShip("Vulcan Scout", Player, new Vector2(-200_000));
        alongY.ApplyForce(new Vector2(0f, 1_000_000f));
        alongY.UpdateVelocityAndPosition(TestSimStep);
        AssertGreaterThan(alongY.Velocity.Y, 0f, "a push along Y alone must move the ship");
    }
}
