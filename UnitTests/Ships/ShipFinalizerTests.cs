using System.Reflection;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Ship_Game.Ships;
using Vector2 = SDGraphics.Vector2;

namespace UnitTests.Ships;

[TestClass]
public class ShipFinalizerTests : StarDriveTest
{
    public ShipFinalizerTests()
    {
        CreateUniverseAndPlayerEmpire();
    }

    // what ~Ship() runs on the GC thread
    static void DisposeAsFinalizer(Ship ship)
    {
        typeof(Ship).GetMethod("Dispose", BindingFlags.NonPublic | BindingFlags.Instance, null, new[] { typeof(bool) }, null)
            .Invoke(ship, new object[] { false });
    }

    [TestMethod]
    public void TheFinalizerLeavesTheShipAlone()
    {
        Ship ship = SpawnShip("Rocket Scout", Player, Vector2.Zero);
        int modules = ship.Modules.Length;

        DisposeAsFinalizer(ship);

        Assert.AreEqual(modules, ship.Modules.Length, "the finalizer path tore down the ship's modules");
        Assert.IsNotNull(ship.AI, "the finalizer path disposed the ship's AI");
    }
}
