using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Microsoft.Xna.Framework;
using Ship_Game;
using Ship_Game.AI;
using Ship_Game.Gameplay;
using Ship_Game.Ships;

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

        void CreateShipWithFieldOfFire(float fieldOfFireDegrees, out Ship ship, out Weapon weapon)
        {
            ship = SpawnShip("Vulcan Scout", Player, Vector2.Zero);
            weapon = ship.Weapons[0];
            weapon.SalvoCount = 1;
            weapon.ProjectileCount = 1;
            weapon.Module.FieldOfFire = fieldOfFireDegrees.ToRadians();
            weapon.Module.Flyweight.AccuracyPercent = 1f; // No targeting errors or jitters
        }

        [TestMethod]
        public void FiringInsideArc()
        {
            CreateShipWithFieldOfFire(45, out Ship ship, out Weapon weapon);
            Ship target = SpawnShip("Vulcan Scout", Enemy, new Vector2(0, -1000f)); // in front of us

            Assert.IsTrue(weapon.UpdateAndFireAtTarget(target, NoProjectiles, NoShips), "Fire at target must succeed");
            Assert.AreEqual(1, GetProjectileCount(ship), "Invalid projectile count");
        }

        [TestMethod]
        public void FiringOutsideOfArc()
        {
            CreateShipWithFieldOfFire(45, out Ship ship, out Weapon weapon);
            Ship target = SpawnShip("Vulcan Scout", Enemy, new Vector2(0, 1000f)); // behind us

            Assert.IsFalse(weapon.UpdateAndFireAtTarget(target, NoProjectiles, NoShips), "Weapon cannot shoot behind its firing ARC!");
            Assert.AreEqual(0, GetProjectileCount(ship), "No projectiles should launch");
        }

        [TestMethod]
        public void FiringTargetOfOpportunity()
        {
            CreateShipWithFieldOfFire(45, out Ship ship, out Weapon weapon);
            Ship main = SpawnShip("Vulcan Scout", Enemy, new Vector2(0, 1000f)); // behind us
            Ship opportune = SpawnShip("Vulcan Scout", Enemy, new Vector2(0, -1000f)); // in front of us

            var otherShips = new[]{ opportune };
            Assert.IsTrue(weapon.UpdateAndFireAtTarget(main, NoProjectiles, otherShips), "Fire at target must succeed");
            Assert.AreEqual(1, GetProjectileCount(ship), "Invalid projectile count");
            Assert.AreEqual(((ShipModule)weapon.FireTarget).GetParent(), opportune, "Weapon must have fired at target of opportunity");
        }

        [TestMethod]
        public void FiringPointDefenseAtMissiles()
        {
            CreateShipWithFieldOfFire(45, out Ship us, out Weapon weapon);
            weapon.TruePD = true;
            weapon.Tag_PD = true;

            Vector2 inFrontOfUs = new Vector2(0, -1000f);
            Vector2 lookingAtUs = inFrontOfUs.DirectionToTarget(us.Position);
            Ship enemy = SpawnShip("Rocket Scout", Enemy, inFrontOfUs, lookingAtUs);

            Weapon rockets = enemy.Weapons.Find(w => w.UID == "Rocket");
            Assert.IsTrue(rockets.UpdateAndFireAtTarget(us, NoProjectiles, NoShips), "Fire at target must succeed");
            enemy.AI.OrderHoldPosition(enemy.Position, enemy.Direction);
            enemy.Update(new FixedSimTime(0.01f)); // update weapons & projectiles

            var projectiles = GetProjectiles(enemy).ToArray();
            Assert.IsTrue(weapon.UpdateAndFireAtTarget(enemy, projectiles, NoShips), "Fire PD at a projectile must succeed");
            Assert.AreEqual(1, GetProjectileCount(us), "Invalid projectile count");
            Assert.AreEqual(weapon.FireTarget.Type, GameObjectType.Proj, "TruePD must only fire at projectiles");
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
