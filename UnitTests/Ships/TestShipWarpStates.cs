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
            ship.InhibitedTimer = 3f;
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
            ship.InhibitedTimer = 4f;
            ship.Update(new FixedSimTime(2f));
            Assert.IsFalse(ship.IsSpoolingOrInWarp);
            Assert.IsFalse(ship.IsInWarp);
        }

        [TestMethod]
        public void TestInhibitedTimer()
        {
            TestShip ship = CreateWarpTestShip();
            ship.EngageStarDrive();
            ship.Update(new FixedSimTime(2f));
            // Test timer for accuracy
            ship.InhibitedTimer = 4f;
            ship.Update(TestSimStep);
            float inhibited = 0;
            while (ship.Inhibited)
            {
                ship.UpdateInhibitLogic(TestSimStep);
                inhibited += TestSimStep.FixedTime;
            }
            Assert.AreEqual(4, inhibited, 0.001d, "Inhibitor time was not equal to expect duration");
            Assert.IsTrue(ship.InhibitedTimer == 0, "Inhibitor timer reset failure");
        }

        [TestMethod]
        public void TestResetInhibitedByEnemyFlag()
        {
            TestShip ship = CreateWarpTestShip();
            ship.EngageStarDrive();
            ship.Update(new FixedSimTime(2f));
            // Test timer for accuracy
            ship.InhibitedTimer = 4f;
            ship.Update(TestSimStep);
            ship.SetInhibitedByEnemy(true);
            while (ship.Inhibited)
            {
                ship.UpdateInhibitLogic(TestSimStep);
            }
            Assert.IsFalse(ship.InhibitedByEnemy, "Failed to unset enemy inhibition status");
        }

        [TestMethod]
        public void TestInhibitedWarpFrequencyCheck()
        {
            TestShip ship = CreateWarpTestShip();
            ship.EngageStarDrive();
            ship.Update(new FixedSimTime(2f));
            ship.Update(new FixedSimTime(2f));
            //ship.Update(TestSimStep);
            //ship.Update(TestSimStep);
            float timeInhibited = ship.InhibitedTimer;
            while (timeInhibited > -Ship.InhibitedAtWarpCheckFrequency)
            {
                Assert.IsTrue(ship.engineState == Ship.MoveState.Warp);
                timeInhibited -= TestSimStep.FixedTime;
                ship.UpdateInhibitLogic(TestSimStep);
                if (timeInhibited <= -Ship.InhibitedAtWarpCheckFrequency)
                {
                    Assert.IsTrue(ship.InhibitedTimer == 0, "Failed to reset inhibitorTimer at InhibitedAtWarpFrequency");
                }
            }
        }
    }
}
