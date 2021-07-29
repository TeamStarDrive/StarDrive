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

            ship.SetWarpInhibitedState(sourceEnemyShip:false, 3f);
            ship.EngageStarDrive();
            Assert.IsFalse(ship.IsSpooling);
            Assert.IsFalse(ship.IsInWarp);

            ship.Update(new FixedSimTime(2f));
            Assert.IsFalse(ship.IsSpooling);
            Assert.IsFalse(ship.IsInWarp);
        }

        [TestMethod]
        public void InhibitedWhileSpoolingCancelsStarDrive()
        {
            var ship = CreateWarpTestShip();

            ship.EngageStarDrive();
            ship.Update(new FixedSimTime(2f));
            Assert.IsTrue(ship.IsSpooling, "Ship should be spooling (and not in warp)");

            // inhibit while spooling
            ship.SetWarpInhibitedState(sourceEnemyShip:false, 4f);
            ship.Update(new FixedSimTime(2f));
            Assert.IsFalse(ship.IsSpooling);
            Assert.IsFalse(ship.IsInWarp);
        }

        [TestMethod]
        public void InhibitedTimerIsAccurate()
        {
            TestShip ship = CreateWarpTestShip();
            ship.EngageStarDrive(); // start spooling
            ship.Update(new FixedSimTime(2f)); // not enough time to engage warp yet
            Assert.IsTrue(ship.IsSpooling, "Ship should be spooling (and not in warp)");

            ship.SetWarpInhibitedState(sourceEnemyShip:false, 4f);
            Assert.IsFalse(ship.InhibitedByEnemy, "SetWarpInhibited InhibitedByEnemy should be false");

            // Test timer for accuracy
            float timeInhibited = 0;
            LoopWhile((5, true), () => ship.Inhibited, () =>
            {
                ship.Update(TestSimStep);
                if (ship.Inhibited)
                    timeInhibited += TestSimStep.FixedTime;
            });

            Assert.AreEqual(4f, timeInhibited, 0.001f, "Ship was not Inhibited for expected duration");
            Assert.AreEqual(ship.Stats.FTLSpoolTime, ship.InhibitedCheckTimer, 0.001f,
                            "InhibitedCheckTimer must be FTLSpoolTime when in STL");
        }

        [TestMethod]
        public void InhibitedByEnemyFlagIsSetToFalse()
        {
            TestShip ship = CreateWarpTestShip();
            ship.EngageStarDrive();
            ship.Update(new FixedSimTime(2f));

            ship.SetWarpInhibitedState(sourceEnemyShip:true, 4f);
            Assert.IsTrue(ship.InhibitedByEnemy, "SetWarpInhibited InhibitedByEnemy should be true");

            LoopWhile((5, true), () => ship.Inhibited, () => ship.Update(TestSimStep));
            Assert.IsFalse(ship.InhibitedByEnemy, "Inhibit failed to clear InhibitedByEnemy flag");
        }

        [TestMethod]
        public void InhibitionChecksAtWarpHappenAtCorrectIntervals()
        {
            TestShip ship = CreateWarpTestShip();
            ship.EngageStarDrive();
            ship.Update(new FixedSimTime(2f));
            ship.Update(new FixedSimTime(2f));
            Assert.IsFalse(ship.IsSpooling, "Ship should no longer be spooling");
            Assert.IsTrue(ship.engineState == Ship.MoveState.Warp, "Ship should be at warp");

            // force immediate inhibition check:
            ship.InhibitedCheckTimer = 0;
            ship.Update(TestSimStep);
            Assert.IsTrue(ship.InhibitedCheckTimer == 0f, "InhibitedCheckTimer must be 0 when warping");
        }
    }
}
