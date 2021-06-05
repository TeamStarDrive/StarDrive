using Microsoft.VisualStudio.TestTools.UnitTesting;
using Microsoft.Xna.Framework;
using Ship_Game;
using Ship_Game.Ships;

namespace UnitTests.Ships
{
    [TestClass]
    public class TestHealth : StarDriveTest
    {
        public TestHealth()
        {
            CreateGameInstance();
            LoadStarterShips("TEST_ShipShield");
            CreateUniverseAndPlayerEmpire();
        }

        [TestMethod]
        public void ShipHealth()
        {
            Ship ship = Ship.CreateShipAtPoint("TEST_ShipShield", Player, Vector2.Zero);
            Ship enemyShip = Ship.CreateShipAtPoint("TEST_ShipShield", Enemy, Vector2.Zero);
            Assert.IsNotNull(ship);

            Assert.That.Equal(ship.InternalSlotCount, 8);
            Assert.That.Equal(ship.HealthPercent, 1);
        }
    }
}
