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
            ship.SetInhibitedState(3f, false);
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
            ship.SetInhibitedState(4f, false);
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
            ship.SetInhibitedState(4f, false);
            ship.Update(TestSimStep);
            float inhibited = 0;
            LoopWhile((5d, true), () => ship.Inhibited, () =>
             {
                 ship.UpdateInhibitLogic(TestSimStep);
                 inhibited += TestSimStep.FixedTime;
             });
            Assert.AreEqual(4, inhibited, 0.001d, "Inhibitor time was not equal to expect duration");
            Assert.IsTrue(ship.InhibitedTimer == 0, "Inhibitor timer reset failure");
        }

        [TestMethod]
        public void InhibitedByEnemyFlagIsSetToFalse()
        {
            TestShip ship = CreateWarpTestShip();
            ship.EngageStarDrive();
            ship.Update(new FixedSimTime(2f));
            // Test timer for accuracy
            ship.SetInhibitedState(duration: 4f, inhibitedByShip: true);
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
            float timeInhibited = ship.InhibitedTimer;
            bool wasTested = false;

            LoopWhile((2d, true), () => timeInhibited > -Ship.InhibitedAtWarpCheckFrequency, () =>
            {
                Assert.IsTrue(ship.engineState == Ship.MoveState.Warp);
                timeInhibited -= TestSimStep.FixedTime;
                ship.UpdateInhibitLogic(TestSimStep);
                if (timeInhibited <= -Ship.InhibitedAtWarpCheckFrequency)
                {
                    Assert.IsTrue(ship.InhibitedTimer == 0, "Failed to reset inhibitorTimer at InhibitedAtWarpFrequency");
                    wasTested = true;
                }
            });
            Assert.IsTrue(wasTested, $"Time inhibited did not fall below Frequency threshold!!!! Debug");
        }
    }
}
