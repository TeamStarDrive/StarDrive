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
        }

        void CreateTestEnv(out Ship ship, out Ship enemyShip)
        {
            CreateUniverseAndPlayerEmpire(out Empire empire);
            ship = Ship.CreateShipAtPoint("TEST_ShipShield", empire, Vector2.Zero);
            enemyShip = Ship.CreateShipAtPoint("TEST_ShipShield", Enemy, Vector2.Zero);
        }

        [TestMethod]
        public void ShipHealth()
        {
            CreateTestEnv(out Ship ship, out Ship enemyShip);
            Assert.IsNotNull(ship);

            Assert.That.Equal(ship.InternalSlotCount, 8);
            Assert.That.Equal(ship.HealthPercent, 1);
        }
    }
}
