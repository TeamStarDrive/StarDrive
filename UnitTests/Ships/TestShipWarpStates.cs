using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Ship_Game;
using Ship_Game.Ships;
using Vector2 = SDGraphics.Vector2;

namespace UnitTests.Ships
{
    [TestClass]
    public class TestShipWarpStates : StarDriveTest
    {
        public TestShipWarpStates()
        {
            CreateUniverseAndPlayerEmpire();
            UState.Events.Disabled = true;
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

            ship.SetWarpInhibited(source: Ship.InhibitionType.GravityWell, 3f);
            ship.EngageStarDrive();
            Assert.IsFalse(ship.IsSpooling);
            Assert.IsFalse(ship.IsInWarp);

            ship.Update(new FixedSimTime(2f));
            Assert.IsFalse(ship.IsInWarp, "Ship should not be in warp while Inhibited");
            Assert.IsFalse(ship.IsSpooling, "Ship should not be spooling while Inhibited");
        }

        [TestMethod]
        public void InhibitedWhileSpoolingCancelsStarDrive()
        {
            var ship = CreateWarpTestShip();

            ship.EngageStarDrive();
            ship.Update(new FixedSimTime(2f));
            Assert.IsTrue(ship.IsSpooling, "Ship should be spooling (and not in warp)");

            // inhibit while spooling
            ship.SetWarpInhibited(source: Ship.InhibitionType.GravityWell, 4f);
            ship.Update(new FixedSimTime(2f));
            Assert.IsFalse(ship.IsInWarp, "Ship should not be in warp while Inhibited");
            Assert.IsFalse(ship.IsSpooling, "Ship should not be spooling while Inhibited");
        }

        [TestMethod]
        public void InhibitedWhileAtWarpCancelsStarDrive()
        {
            var ship = CreateWarpTestShip();

            ship.EngageStarDrive();
            ship.Update(new FixedSimTime(4f));
            Assert.IsTrue(ship.IsInWarp, "Ship should be in warp");

            // inhibit while warping
            ship.SetWarpInhibited(source: Ship.InhibitionType.GravityWell, 4f);
            ship.Update(TestSimStep);
            Assert.IsFalse(ship.IsInWarp, "Ship should not be in warp while Inhibited");
            Assert.IsFalse(ship.IsSpooling, "Ship should not be spooling while Inhibited");
        }

        [TestMethod]
        public void InhibitedTimerIsAccurate()
        {
            TestShip ship = CreateWarpTestShip();
            ship.EngageStarDrive(); // start spooling
            ship.Update(new FixedSimTime(2f)); // not enough time to engage warp yet
            Assert.IsTrue(ship.IsSpooling, "Ship should be spooling");

            ship.SetWarpInhibited(source: Ship.InhibitionType.GravityWell, 4f);
            AssertEqual(Ship.InhibitionType.GravityWell, ship.InhibitionSource, "Inhibited Source should be gravitywell");

            // Test timer for accuracy
            double timeInhibited = RunSimWhile((simTimeout:15, true), () => ship.Inhibited);

            AssertEqual(0.001, 4.0 + TestSimStepD, timeInhibited, "Ship was not Inhibited for expected duration");
            AssertEqual(0.001f, ship.Stats.FTLSpoolTime, ship.InhibitedCheckTimer,
                                "InhibitedCheckTimer must be FTLSpoolTime when in STL");
            AssertEqual(Ship.InhibitionType.None, ship.InhibitionSource, "Source should be none");
        }

        [TestMethod]
        public void InhibitedByEnemyFlagIsSetToFalse()
        {
            TestShip ship = CreateWarpTestShip();
            ship.EngageStarDrive();
            ship.Update(new FixedSimTime(2f));

            ship.SetWarpInhibited(source: Ship.InhibitionType.EnemyShip, 4f);
            AssertEqual(Ship.InhibitionType.EnemyShip, ship.InhibitionSource, "Source should be EnemyShip");

            RunSimWhile((5, true), () => ship.Inhibited);
            Assert.AreNotEqual(Ship.InhibitionType.EnemyShip, ship.InhibitionSource, "Inhibit failed to clear InhibitedByEnemy flag");
            AssertEqual(Ship.InhibitionType.None, ship.InhibitionSource, "Source should be none");
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
