using Microsoft.VisualStudio.TestTools.UnitTesting;
using Microsoft.Xna.Framework;
using Ship_Game;
using Ship_Game.Ships;

namespace UnitTests.Ships
{
    [TestClass]
    public class TestShields : StarDriveTest
    {
        public TestShields()
        {
            LoadStarterShips("TEST_ShipShield");
            CreateUniverseAndPlayerEmpire();
        }

        [TestMethod]
        public void AmplifierDestroyed()
        {
            Ship ship = Ship.CreateShipAtPoint("TEST_ShipShield", Player, Vector2.Zero);;
            Assert.IsNotNull(ship);
            Assert.That.Equal(ship.ShieldMax, 1400);

            ShipModule amplifier = ship.TestGetModule("TEST_ModuleAmplifier");
            Assert.IsNotNull(amplifier);
            Assert.That.Equal(amplifier.AmplifyShields, 200);

            amplifier.Active = false;
            ship.ShipStatusChange();
            Assert.That.Equal(ship.ShieldMax, 1200);

            amplifier.Active = true;
            ship.ShipStatusChange();
            Assert.That.Equal(ship.ShieldMax, 1400);
        }
    }
}
