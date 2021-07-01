using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Microsoft.Xna.Framework;
using Ship_Game;
using Ship_Game.AI;
using Ship_Game.Ships;

namespace UnitTests.AITests.Ships
{
    [TestClass]
    public class ShipAICombatTests : StarDriveTest
    {
        Ship OurShip, TheirShip;

        public ShipAICombatTests()
        {
            CreateGameInstance();
            LoadStarterShipVulcan();
            CreateUniverseAndPlayerEmpire();

            OurShip = SpawnShip("Vulcan Scout", Player, new Vector2(0, 0));
            TheirShip = SpawnShip("Rocket Scout", Enemy, new Vector2(0, -200));
            SpawnShip("Vulcan Scout", Enemy, new Vector2(0, -400));
            // Rotate to LOOK AT our ship
            TheirShip.Rotation = TheirShip.Center.DirectionToTarget(OurShip.Center).ToRadians();

            OurShip.AI.SetCombatTriggerDelay(10f);
            TheirShip.AI.SetCombatTriggerDelay(10f);
            OurShip.AI.CanTrackProjectiles = true;
            TheirShip.AI.CanTrackProjectiles = true;
        }

        void Update(int iterations = 1)
        {
            for (int i = 0; i < iterations; ++i)
            {
                // 1. first update universe object states
                Universe.Objects.Update(TestSimStep);
                // 2. then run the scans
                foreach (Ship s in Universe.Objects.Ships)
                    s.AI.ScanForTargets(TestSimStep);
            }
        }

        [TestMethod]
        public void EnemyShipsDetectedOnScan()
        {
            Update(iterations:1);
            Assert.AreEqual(2, OurShip.AI.PotentialTargets.Length);
            Assert.AreEqual(0, OurShip.AI.FriendliesNearby.Length);
            Assert.AreEqual(0, OurShip.AI.TrackProjectiles.Length);

            Assert.AreEqual(1, TheirShip.AI.PotentialTargets.Length);
            Assert.AreEqual(1, TheirShip.AI.FriendliesNearby.Length);
            Assert.AreEqual(0, TheirShip.AI.TrackProjectiles.Length);
        }

        [TestMethod]
        public void ClosestEnemyShipSelectedAsTarget()
        {
            Update(iterations:1);
            Assert.AreEqual(2, OurShip.AI.PotentialTargets.Length);
            Assert.AreEqual(1, TheirShip.AI.PotentialTargets.Length);

            Assert.IsNotNull(TheirShip.AI.Target, "No Target! BUG in ScanForCombatTargets()");
            Assert.AreEqual(OurShip, TheirShip.AI.Target);

            Assert.IsNotNull(OurShip.AI.Target, "No Target! BUG in ScanForCombatTargets()");
            Assert.AreEqual(TheirShip, OurShip.AI.Target);
        }

        [TestMethod]
        public void WeCanDetectEnemyRockets()
        {
            Update(iterations:1);
            Assert.AreEqual(2, OurShip.AI.PotentialTargets.Length);
            Assert.AreEqual(0, OurShip.AI.TrackProjectiles.Length);
            
            TheirShip.AI.SetCombatTriggerDelay(0f);
            Assert.IsNotNull(TheirShip.AI.Target, "No Target! BUG in ScanForCombatTargets()");
            bool didFireWeapons = TheirShip.AI.FireWeapons(TestSimStep);
            Assert.IsTrue(didFireWeapons, "Rocket Scout couldn't fire on our Ship. BUG!!");

            Update(iterations:1);
            Assert.AreEqual(2, OurShip.AI.PotentialTargets.Length);
            Assert.AreEqual(3, OurShip.AI.TrackProjectiles.Length, "Rockets weren't detected! BUG!");
        }
    }
}
