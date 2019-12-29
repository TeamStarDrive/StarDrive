using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Microsoft.Xna.Framework;
using Ship_Game.Ships;

namespace UnitTests.Ships
{
    [TestClass]
    public class TestShipWarpStates : StarDriveTest
    {
        public TestShipWarpStates()
        {
            LoadStarterShipVulcan();
            CreateGameInstance();
            CreateUniverseAndPlayerEmpire(out _);
        }

        Ship CreateWarpTestShip()
        {
            Ship ship = SpawnShip("Vulcan Scout", Player, Vector2.Zero);
            ship.FTLSpoolTime = 3f;
            Assert.IsFalse(ship.IsSpoolingOrInWarp);
            Assert.IsFalse(ship.IsInWarp);
            return ship;
        }

        [TestMethod]
        public void EngageStarDrive()
        {
            Ship ship = CreateWarpTestShip();
            ship.EngageStarDrive();
            Assert.IsTrue(ship.IsSpoolingOrInWarp);
            Assert.IsFalse(ship.IsInWarp);

            ship.Update(2f); // spooling not over yet
            Assert.IsTrue(ship.IsSpoolingOrInWarp);
            Assert.IsFalse(ship.IsInWarp);

            ship.Update(2f); // now it should enter warp
            Assert.IsTrue(ship.IsSpoolingOrInWarp);
            Assert.IsTrue(ship.IsInWarp);

            ship.Update(10f); // should still be in warp
            Assert.IsTrue(ship.IsSpoolingOrInWarp);
            Assert.IsTrue(ship.IsInWarp);

            // immediate hyperspace return
            ship.HyperspaceReturn();
            Assert.IsFalse(ship.IsSpoolingOrInWarp);
            Assert.IsFalse(ship.IsInWarp);
        }

        [TestMethod]
        public void InhibitedCannotEngageStarDrive()
        {
            Ship ship = CreateWarpTestShip();
            // inhibit while spooling
            ship.InhibitedTimer = 3f;
            ship.EngageStarDrive();
            ship.Update(2f);
            Assert.IsFalse(ship.IsSpoolingOrInWarp);
            Assert.IsFalse(ship.IsInWarp);
        }

        [TestMethod]
        public void InhibitedWhileSpoolingCancelsStarDrive()
        {
            Ship ship = CreateWarpTestShip();
            ship.EngageStarDrive();
            ship.Update(2f);
            // inhibit while spooling
            ship.InhibitedTimer = 4f;
            ship.Update(2f);
            Assert.IsFalse(ship.IsSpoolingOrInWarp);
            Assert.IsFalse(ship.IsInWarp);
        }
    }
}
