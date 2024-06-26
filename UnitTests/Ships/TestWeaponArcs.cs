﻿using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using SDGraphics;
using SDUtils;
using Ship_Game;
using Ship_Game.AI;
using Ship_Game.Gameplay;
using Ship_Game.Ships;
using Vector2 = SDGraphics.Vector2;

namespace UnitTests.Ships
{
    [TestClass]
    public class TestWeaponArcs : StarDriveTest
    {
        public TestWeaponArcs()
        {
            LoadStarterShips("Soldier mk2-c");
            CreateUniverseAndPlayerEmpire();
        }

        static readonly Projectile[] NoProjectiles = Empty<Projectile>.Array;
        static readonly Ship[] NoShips = Empty<Ship>.Array;

        void CreateShipWithFieldOfFire(float fieldOfFireDegrees, out Ship ship, out WeaponTestWrapper weapon)
        {
            ship = SpawnShip("TEST_Vulcan Scout", Player, Vector2.Zero);
            weapon = (WeaponTestWrapper)ship.Weapons[0];
            weapon.TestSalvoCount = 1;
            weapon.TestProjectileCount = 1;
            weapon.Module.FieldOfFire = fieldOfFireDegrees.ToRadians();
            weapon.Module.Flyweight.AccuracyPercent = 1f; // No targeting errors or jitters
        }

        [TestMethod]
        public void FiringInsideArc()
        {
            CreateShipWithFieldOfFire(45, out Ship ship, out WeaponTestWrapper weapon);
            Ship target = SpawnShip("TEST_Vulcan Scout", Enemy, new Vector2(0, -1000f)); // in front of us

            Assert.IsTrue(weapon.UpdateAndFireAtTarget(target, NoProjectiles, NoShips), "Fire at target must succeed");
            AssertEqual(1, GetProjectileCount(ship), "Invalid projectile count");
        }

        [TestMethod]
        public void FiringOutsideOfArc()
        {
            CreateShipWithFieldOfFire(45, out Ship ship, out WeaponTestWrapper weapon);
            Ship target = SpawnShip("TEST_Vulcan Scout", Enemy, new Vector2(0, 1000f)); // behind us

            Assert.IsFalse(weapon.UpdateAndFireAtTarget(target, NoProjectiles, NoShips), "Weapon cannot shoot behind its firing ARC!");
            AssertEqual(0, GetProjectileCount(ship), "No projectiles should launch");
        }

        [TestMethod]
        public void FiringTargetOfOpportunity()
        {
            CreateShipWithFieldOfFire(45, out Ship ship, out WeaponTestWrapper weapon);
            Ship main = SpawnShip("TEST_Vulcan Scout", Enemy, new Vector2(0, 1000f)); // behind us
            Ship opportune = SpawnShip("TEST_Vulcan Scout", Enemy, new Vector2(0, -1000f)); // in front of us

            var otherShips = new[]{ opportune };
            Assert.IsTrue(weapon.UpdateAndFireAtTarget(main, NoProjectiles, otherShips), "Fire at target must succeed");
            AssertEqual(1, GetProjectileCount(ship), "Invalid projectile count");
            AssertEqual(((ShipModule)weapon.FireTarget).GetParent(), opportune, "Weapon must have fired at target of opportunity");
        }

        [TestMethod]
        public void FiringPointDefenseAtMissiles()
        {
            CreateShipWithFieldOfFire(45, out Ship us, out WeaponTestWrapper weapon);
            weapon.TestTruePD = true;
            weapon.TestTag_PD = true;

            Vector2 inFrontOfUs = new Vector2(0, -1000f);
            Vector2 lookingAtUs = inFrontOfUs.DirectionToTarget(us.Position);
            Ship enemy = SpawnShip("Rocket Scout", Enemy, inFrontOfUs, lookingAtUs);

            Weapon rockets = enemy.Weapons.Find(w => w.UID == "Rocket");
            Assert.IsTrue(rockets.UpdateAndFireAtTarget(us, NoProjectiles, NoShips), "Fire at target must succeed");
            enemy.AI.OrderHoldPosition(enemy.Position, enemy.Direction);
            enemy.Update(new FixedSimTime(0.01f)); // update weapons & projectiles

            var projectiles = GetProjectiles(enemy).ToArr();
            Assert.IsTrue(weapon.UpdateAndFireAtTarget(enemy, projectiles, NoShips), "Fire PD at a projectile must succeed");
            AssertEqual(1, GetProjectileCount(us), "Invalid projectile count");
            AssertEqual(weapon.FireTarget.Type, GameObjectType.Proj, "TruePD must only fire at projectiles");
        }

        [TestMethod]
        [Ignore]
        public void FiringWithError()
        {
            Ship ship = SpawnShip("Soldier mk2-c", Player, Vector2.Zero);
            Weapon weapon = ship.Weapons.Find(w => w.UID == "LaserBeam");
            
            // TODO: This needs a major fix
            float error = weapon.BaseTargetError(-1);
            Assert.IsTrue(error > 112 & error < 114);
            // I am embarrassed by this unit test.
        }
    }
}
