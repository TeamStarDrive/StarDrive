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
            CreateGameInstance();
        }

        void CreateTestEnv(out Ship ship)
        {
            CreateUniverseAndPlayerEmpire(out Empire empire);
            ship = Ship.CreateShipAtPoint("TEST_ShipShield", empire, Vector2.Zero);
        }

        [TestMethod]
        public void AmplifierDestroyed()
        {
            CreateTestEnv(out Ship ship);
            Assert.IsNotNull(ship);
            Assert.That.Equal(ship.shield_max, 1400);

            ShipModule amplifier = ship.TestGetModule("TEST_ModuleAmplifier");
            Assert.IsNotNull(amplifier);
            Assert.That.Equal(amplifier.AmplifyShields, 200);

            amplifier.Active = false;
            ship.ShipStatusChange();
            Assert.That.Equal(ship.shield_max, 1200);

            amplifier.Active = true;
            ship.ShipStatusChange();
            Assert.That.Equal(ship.shield_max, 1400);
        }
    }
}
