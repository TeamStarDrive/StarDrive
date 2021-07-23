using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Microsoft.Xna.Framework;
using Ship_Game;
using Ship_Game.AI;
using Ship_Game.Empires;
using Ship_Game.Gameplay;
using Ship_Game.Ships;

namespace UnitTests.AITests.Ships
{
    [TestClass]
    public class ShipAICombatTests : StarDriveTest
    {
        Ship OurShip, TheirShip, ThirdShip;

        FixedSimTime EnemyScanInterval = new FixedSimTime(EmpireConstants.EnemyScanInterval);

        public ShipAICombatTests()
        {
            CreateUniverseAndPlayerEmpire();

            OurShip = SpawnShip("Vulcan Scout", Player, new Vector2(0, 0));
            OurShip.AI.SetCombatTriggerDelay(10f); // disable firing weapons
            OurShip.AI.TargetProjectiles = true;
        }

        void SpawnEnemyShips()
        {
            TheirShip = SpawnShip("Rocket Scout", Enemy, new Vector2(0, -400));
            ThirdShip = SpawnShip("Vulcan Scout", Enemy, new Vector2(0, -600));
            // Rotate to LOOK AT our ship
            TheirShip.Rotation = TheirShip.Position.DirectionToTarget(OurShip.Position).ToRadians();
            ThirdShip.Rotation = ThirdShip.Position.DirectionToTarget(OurShip.Position).ToRadians();

            TheirShip.AI.SetCombatTriggerDelay(10f); // disable firing weapons
            ThirdShip.AI.SetCombatTriggerDelay(10f); // disable firing weapons

            TheirShip.AI.TargetProjectiles = true;
            ThirdShip.AI.TargetProjectiles = true;
        }

        void SpawnEnemyPlanet()
        {
            AddDummyPlanetToEmpire(Enemy);
        }

        void Update(FixedSimTime timeStep)
        {
            // 1. Updates all Ships
            // 2. Updates all Sensors and Scans for Targets (!)
            // 3. Updates all Ship AI-s (!)
            Universe.Objects.Update(timeStep);
        }

        [TestMethod]
        public void EnemyShipsDetectedOnScan()
        {
            SpawnEnemyShips();
            Update(TestSimStep);
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
            SpawnEnemyShips();
            Update(TestSimStep);
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
            SpawnEnemyShips();
            Update(TestSimStep);
            Assert.AreEqual(2, OurShip.AI.PotentialTargets.Length);
            Assert.AreEqual(0, OurShip.AI.TrackProjectiles.Length);
            
            TheirShip.AI.SetCombatTriggerDelay(0f); // enable weapons
            Assert.IsNotNull(TheirShip.AI.Target, "No Target! BUG in ScanForCombatTargets()");
            bool didFireWeapons = TheirShip.AI.FireWeapons(TestSimStep);
            Assert.IsTrue(didFireWeapons, "Rocket Scout couldn't fire on our Ship. BUG!!");

            var minTime = new FixedSimTime(EmpireConstants.ProjectileScanInterval);
            Update(minTime);
            Assert.AreEqual(2, OurShip.AI.PotentialTargets.Length);
            Assert.AreEqual(3, OurShip.AI.TrackProjectiles.Length, "Rockets weren't detected! BUG!");
        }

        [TestMethod]
        public void InCombatAutoEnterWithEnemyShips()
        {
            Update(TestSimStep);
            Assert.IsFalse(OurShip.InCombat, "ship should not be in combat yet");

            SpawnEnemyShips();
            Update(EnemyScanInterval);
            Assert.IsTrue(OurShip.InCombat, "ship should be in combat");
        }

        void InjectSteroids(Ship s)
        {
            // inject some steroids into our vulcan cannons 
            foreach (var w in OurShip.Weapons)
            {
                w.DamageAmount = 150f;
                w.OrdinanceRequiredToFire = 0f;
            }
            OurShip.AI.CombatState = CombatState.ShortRange;
        }

        [TestMethod]
        public void InCombatAutoEnterAndExitWhenColonyShipDestroyed()
        {
            var colonyShip = SpawnShip("Colony Ship", Enemy, new Vector2(500,500));
            colonyShip.AI.HoldPosition();
            colonyShip.shipData.ShipCategory = ShipData.Category.Kamikaze;
            Update(EnemyScanInterval);
            Assert.IsTrue(OurShip.InCombat, "ship should be in combat");

            InjectSteroids(OurShip);

            LoopWhile(5, () => colonyShip.Active, () =>
            {
                Assert.IsTrue(OurShip.InCombat, "ship must stay in combat until target destroyed");
                Update(TestSimStep);
            });

            Update(EnemyScanInterval);
            Assert.IsFalse(OurShip.InCombat, "ship must exit combat after target destroyed");
        }

        [TestMethod]
        public void InCombatAutoEnterWithCombatMoveShouldKillColonyShip()
        {
            var colonyShip = SpawnShip("Colony Ship", Enemy, new Vector2(500,500));
            colonyShip.AI.HoldPosition();
            Update(EnemyScanInterval);
            Assert.IsTrue(OurShip.InCombat, "ship should be in combat");

            InjectSteroids(OurShip);

            // now assign offensive move order
            OurShip.AI.OrderMoveTo(colonyShip.Position, Vectors.Up, true, AIState.AwaitingOrders,
                                   offensiveMove: true);

            Assert.IsFalse(OurShip.InCombat, "ship must exit combat after giving a move order, since giving a move order clears orders");
            // Let the ship reacquire the target since giving an order caused an exit
            // combat (as it should, otherwise the ship will not enter combat again). 
            Update(EnemyScanInterval); 

            // our ship must remain in combat the whole time until enemy ship is destroyed
            LoopWhile(5, () => colonyShip.Active, () =>
            {
                Assert.IsTrue(OurShip.InCombat, "ship must stay in combat until target destroyed");
                Update(TestSimStep);
            });

            Update(EnemyScanInterval);
            Assert.IsFalse(OurShip.InCombat, "ship must exit combat after target destroyed");
        }

        [TestMethod]
        public void InCombatAutoExitWhenEnemyShipsDie()
        {
            Update(TestSimStep);
            SpawnEnemyShips();
            Update(EnemyScanInterval);
            Assert.IsTrue(OurShip.InCombat, "ship should be in combat");

            TheirShip.Die(TheirShip, cleanupOnly: true);
            ThirdShip.Die(ThirdShip, cleanupOnly: true);

            Update(EnemyScanInterval);
            Assert.IsFalse(OurShip.InCombat, "ship should have exited combat");
        }

        [TestMethod]
        public void InCombatAutoExitWhenEnemyShipsWarpAway()
        {
            Update(TestSimStep);
            SpawnEnemyShips();
            Update(EnemyScanInterval);
            Assert.IsTrue(OurShip.InCombat, "ship should be in combat");

            // move the ships to the middle of nowhere (outside sensor range)
            TheirShip.Position = new Vector2(200000, 200000);
            ThirdShip.Position = new Vector2(200000, 200000);

            Update(EnemyScanInterval);
            Assert.IsFalse(OurShip.InCombat, "ship should have exited combat");
        }

        [TestMethod]
        public void NotInCombatWithHostilePlanets()
        {
            Update(TestSimStep);
            Assert.IsFalse(OurShip.InCombat, "ship should not be in combat yet");
            
            SpawnEnemyPlanet();
            Update(EnemyScanInterval);
            Assert.IsFalse(OurShip.InCombat, "ship should not be in combat with a planet");
        }
    }
}
