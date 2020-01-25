using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Microsoft.Xna.Framework;
using Ship_Game.Gameplay;
using Ship_Game.Ships;

namespace UnitTests.Ships
{
    [TestClass]
    public class TestShields : StarDriveTest
    {
        public TestShields()
        {
            LoadStarterShips("TEST-ShipShield");
            CreateGameInstance();
            CreateUniverseAndPlayerEmpire(out _);
        }

        [TestMethod]
        public void AmplifierDestroyed()
        {
            Ship ship = SpawnShip("TEST-ShipShield", Player, Vector2.Zero);
            Assert.That.Equal(ship.shield_max, 1400);
            ShipModule amplifier = ship.TestGetModule("TEST-ModuleAmplifier");
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
