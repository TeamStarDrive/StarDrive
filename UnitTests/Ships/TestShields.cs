﻿using Microsoft.VisualStudio.TestTools.UnitTesting;
using Ship_Game;
using Ship_Game.Ships;
using Vector2 = SDGraphics.Vector2;

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
            Ship ship = SpawnShip("TEST_ShipShield", Player, Vector2.Zero);;
            Assert.IsNotNull(ship);
            AssertEqual(ship.ShieldMax, 1400);

            ShipModule amplifier = ship.TestGetModule("TEST_ModuleAmplifier");
            Assert.IsNotNull(amplifier);
            AssertEqual(amplifier.AmplifyShields, 200);

            amplifier.Active = false;
            ship.ShipStatusChange();
            AssertEqual(ship.ShieldMax, 1200);

            amplifier.Active = true;
            ship.ShipStatusChange();
            AssertEqual(ship.ShieldMax, 1400);
        }
    }
}
