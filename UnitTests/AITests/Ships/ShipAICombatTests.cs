﻿using System;
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
using UnitTests.Ships;

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

        static void InjectSteroids(Ship s)
        {
            // inject some steroids into our vulcan cannons 
            foreach (var w in s.Weapons)
            {
                w.DamageAmount = 150f;
                w.OrdinanceRequiredToFire = 0f;
                w.fireDelay = 0.5f;
            }

            s.AI.SetCombatTriggerDelay(0f);
            s.AI.CombatState = CombatState.ShortRange;
        }

        void DebugPrintKillFailure(TestShip colonyShip)
        {
            Log.Write($"Failed to kill colony ship!: {colonyShip}");
            Log.Write($"OurShip: {OurShip}");
            Log.Write($"OurShip.Target = {OurShip.AI.Target}  CombatState = {OurShip.AI.CombatState} InCombat = {OurShip.InCombat}");
            Log.Write($"ColonyShip.CombatState = {colonyShip.AI.CombatState}  InCombat = {colonyShip.InCombat}");
            Log.Write($"ColonyShip.health = {colonyShip.Health}  percent = {colonyShip.HealthPercent}");
            foreach (var m in colonyShip.Modules)
                Log.Write($"  ColonyShip.Module {m}");
        }

        [TestMethod]
        public void InCombatAutoEnterAndExitWhenColonyShipDestroyed()
        {
            TestShip colonyShip = SpawnShip("Colony Ship", Enemy, new Vector2(0,-500));
            colonyShip.AI.HoldPosition();
            colonyShip.shipData.ShipCategory = ShipData.Category.Kamikaze;
            Update(EnemyScanInterval);
            Assert.IsTrue(OurShip.InCombat, "ship should be in combat");
            
            InjectSteroids(OurShip);

            LoopWhile((timeout:5, fatal:false), () => colonyShip.Active, () =>
            {
                Assert.IsTrue(OurShip.InCombat, "ship must stay in combat until target destroyed");
                colonyShip.Velocity = Vector2.Zero; // BUG: there is a strange drift effect in sim
                Update(TestSimStep);
            });

            if (colonyShip.Active)
            {
                DebugPrintKillFailure(colonyShip);
                Assert.Fail("Failed to kill colony ship");
            }

            Update(EnemyScanInterval);
            Assert.IsFalse(OurShip.InCombat, "ship must exit combat after target destroyed");
        }

        [TestMethod]
        public void InCombatAutoEnterWithCombatMoveShouldKillColonyShip()
        {
            TestShip colonyShip = SpawnShip("Colony Ship", Enemy, new Vector2(0,-500));
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
            LoopWhile((timeout:5, fatal:false), () => colonyShip.Active, () =>
            {
                Assert.IsTrue(OurShip.InCombat, "ship must stay in combat until target destroyed");
                colonyShip.Velocity = Vector2.Zero; // BUG: there is a strange drift effect in sim
                Update(TestSimStep);
            });

            if (colonyShip.Active)
            {
                DebugPrintKillFailure(colonyShip);
                Assert.Fail("Failed to kill colony ship");
            }

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
