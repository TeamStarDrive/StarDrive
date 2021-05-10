using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Microsoft.Xna.Framework;
using Ship_Game;
using Ship_Game.Ships;

namespace UnitTests.Ships
{
    [TestClass]
    public class TestShipWarpStates : StarDriveTest
    {
        public TestShipWarpStates()
        {
            CreateGameInstance();
            LoadStarterShipVulcan();
            CreateUniverseAndPlayerEmpire(out _);
        }

        Ship CreateWarpTestShip()
        {
            Ship ship = SpawnShip("Vulcan Scout", Player, Vector2.Zero);
            ship.Stats.FTLSpoolTime = 3f;
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

            ship.Update(new FixedSimTime(2f)); // spooling not over yet
            Assert.IsTrue(ship.IsSpoolingOrInWarp);
            Assert.IsFalse(ship.IsInWarp);

            ship.Update(new FixedSimTime(2f)); // now it should enter warp
            Assert.IsTrue(ship.IsSpoolingOrInWarp);
            Assert.IsTrue(ship.IsInWarp);

            ship.Update(new FixedSimTime(10f)); // should still be in warp
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
            ship.Update(new FixedSimTime(2f));
            Assert.IsFalse(ship.IsSpoolingOrInWarp);
            Assert.IsFalse(ship.IsInWarp);
        }

        [TestMethod]
        public void InhibitedWhileSpoolingCancelsStarDrive()
        {
            Ship ship = CreateWarpTestShip();
            ship.EngageStarDrive();
            ship.Update(new FixedSimTime(2f));
            // inhibit while spooling
            ship.InhibitedTimer = 4f;
            ship.Update(new FixedSimTime(2f));
            Assert.IsFalse(ship.IsSpoolingOrInWarp);
            Assert.IsFalse(ship.IsInWarp);
        }
    }
}
