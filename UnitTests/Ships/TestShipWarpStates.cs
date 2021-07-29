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
            CreateUniverseAndPlayerEmpire();
        }

        TestShip CreateWarpTestShip()
        {
            TestShip ship = SpawnShip("Vulcan Scout", Player, Vector2.Zero);
            ship.Stats.FTLSpoolTime = 3f;
            Assert.IsFalse(ship.IsSpoolingOrInWarp);
            Assert.IsFalse(ship.IsInWarp);
            return ship;
        }

        [TestMethod]
        public void EngageStarDrive()
        {
            var ship = CreateWarpTestShip();
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
            var ship = CreateWarpTestShip();
            // inhibit while spooling
            ship.SetWarpInhibitedState(false, 3f);
            ship.EngageStarDrive();
            ship.Update(new FixedSimTime(2f));
            Assert.IsFalse(ship.IsSpoolingOrInWarp);
            Assert.IsFalse(ship.IsInWarp);
        }

        [TestMethod]
        public void InhibitedWhileSpoolingCancelsStarDrive()
        {
            var ship = CreateWarpTestShip();
            ship.EngageStarDrive();
            ship.Update(new FixedSimTime(2f));
            // inhibit while spooling
            ship.SetWarpInhibitedState(false, 4f);
            ship.Update(new FixedSimTime(2f));
            Assert.IsFalse(ship.IsSpoolingOrInWarp);
            Assert.IsFalse(ship.IsInWarp);
        }

        [TestMethod]
        public void InhibitedTimerIsAccurate()
        {
            TestShip ship = CreateWarpTestShip();
            ship.EngageStarDrive();
            ship.Update(new FixedSimTime(2f));
            // Test timer for accuracy
            ship.SetWarpInhibitedState(false, 4f);
            ship.Update(TestSimStep);
            float inhibited = 0;
            LoopWhile((5d, true), () => ship.Inhibited, () =>
             {
                 ship.UpdateInhibitLogic(TestSimStep);
                 inhibited += TestSimStep.FixedTime;
             });
            Assert.AreEqual(4, inhibited, 0.001d, "Inhibitor time was not equal to expect duration");
            Assert.IsTrue(ship.WarpInhibitionCheckTimer == ship.Stats.FTLSpoolTime, "Inhibitor timer reset failure");
        }

        [TestMethod]
        public void InhibitedByEnemyFlagIsSetToFalse()
        {
            TestShip ship = CreateWarpTestShip();
            ship.EngageStarDrive();
            ship.Update(new FixedSimTime(2f));
            // Test timer for accuracy
            ship.SetWarpInhibitedState(true, 4f);
            ship.Update(TestSimStep);

            LoopWhile((5d, true), () => ship.Inhibited, () => ship.UpdateInhibitLogic(TestSimStep));

            Assert.IsFalse(ship.InhibitedByEnemy, "Failed to unset enemy inhibition status");
        }

        [TestMethod]
        public void InhibitionChecksAtWarpHappenAtCorrectIntervals()
        {
            TestShip ship = CreateWarpTestShip();
            ship.EngageStarDrive();
            ship.Update(new FixedSimTime(2f));
            ship.Update(new FixedSimTime(2f));
            Assert.IsTrue(ship.engineState == Ship.MoveState.Warp, "Ship at warp sanity test fail");
            ship.WarpInhibitionCheckTimer = 0;
            ship.UpdateInhibitLogic(TestSimStep);
            Assert.IsTrue(ship.WarpInhibitionCheckTimer == Ship.InhibitedAtWarpCheckFrequency, "Failed to reset inhibitorTimer to InhibitedAtWarpFrequency");
        }
    }
}
