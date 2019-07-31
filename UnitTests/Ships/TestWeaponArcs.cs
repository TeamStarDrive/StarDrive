using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Microsoft.Xna.Framework;
using Ship_Game;
using Ship_Game.Gameplay;
using Ship_Game.Ships;

namespace UnitTests.Ships
{
    [TestClass]
    public class TestWeaponArcs : StarDriveTest
    {
        public TestWeaponArcs()
        {
            LoadStarterShipVulcan();
            CreateGameInstance();
            CreateUniverseAndPlayerEmpire(out _);
        }

        static readonly Array<Projectile> NoProjectiles = new Array<Projectile>();
        static readonly Array<Ship> NoShips = new Array<Ship>();

        void CreateShipWithFieldOfFire(float fieldOfFireDegrees, out Ship ship, out Weapon weapon)
        {
            ship = SpawnShip("Vulcan Scout", Player, Vector2.Zero);
            weapon = ship.Weapons.Find(w => w.UID == "VulcanCannon");
            weapon.SalvoCount = 1;
            weapon.ProjectileCount = 1;
            weapon.Module.FieldOfFire = fieldOfFireDegrees.ToRadians();
            weapon.Module.AccuracyPercent = 1f; // No targeting errors or jitters
        }


        [TestMethod]
        public void TestFiringInsideArc()
        {
            CreateShipWithFieldOfFire(45, out Ship ship, out Weapon weapon);
            Ship target = SpawnShip("Vulcan Scout", Enemy, new Vector2(0, -1000f)); // in front of us
            
            Assert.IsTrue(weapon.UpdateAndFireAtTarget(target, NoProjectiles, NoShips), "Fire at target must succeed");
            Assert.AreEqual(1, ship.CopyProjectiles.Length, "Invalid projectile count");
        }

        [TestMethod]
        public void TestFiringOutsideOfArc()
        {
            CreateShipWithFieldOfFire(45, out Ship ship, out Weapon weapon);
            Ship target = SpawnShip("Vulcan Scout", Enemy, new Vector2(0, 1000f)); // behind us
            
            Assert.IsFalse(weapon.UpdateAndFireAtTarget(target, NoProjectiles, NoShips), "Weapon cannot shoot behind its firing ARC!");
            Assert.AreEqual(0, ship.CopyProjectiles.Length, "No projectiles should launch");
        }

        [TestMethod]
        public void TestFiringTargetOfOpportunity()
        {
            CreateShipWithFieldOfFire(45, out Ship ship, out Weapon weapon);
            Ship main = SpawnShip("Vulcan Scout", Enemy, new Vector2(0, 1000f)); // behind us
            Ship opportune = SpawnShip("Vulcan Scout", Enemy, new Vector2(0, -1000f)); // in front of us

            var otherShips = new Array<Ship>(new[]{ opportune });
            Assert.IsTrue(weapon.UpdateAndFireAtTarget(main, NoProjectiles, otherShips), "Fire at target must succeed");
            Assert.AreEqual(1, ship.CopyProjectiles.Length, "Invalid projectile count");
            Assert.AreEqual(((ShipModule)weapon.FireTarget).GetParent(), opportune, "Weapon must have fired at target of opportunity");
        }

        [TestMethod]
        public void TestFiringPointDefenseAtMissiles()
        {
            CreateShipWithFieldOfFire(45, out Ship us, out Weapon weapon);
            weapon.TruePD = true;
            weapon.Tag_PD = true;

            Vector2 inFrontOfUs = new Vector2(0, -1000f);
            Vector2 lookingAtUs = inFrontOfUs.DirectionToTarget(us.Center);
            Ship enemy = SpawnShip("Rocket Scout", Enemy, inFrontOfUs, lookingAtUs.ToRadians());

            Weapon rockets = enemy.Weapons.Find(w => w.UID == "Rocket");
            Assert.IsTrue(rockets.UpdateAndFireAtTarget(us, NoProjectiles, NoShips), "Fire at target must succeed");
            enemy.Update(0.01f); // update weapons & projectiles

            var projectiles = new Array<Projectile>(enemy.CopyProjectiles);
            Assert.IsTrue(weapon.UpdateAndFireAtTarget(enemy, projectiles, NoShips), "Fire PD at a projectile must succeed");
            Assert.AreEqual(1, us.CopyProjectiles.Length, "Invalid projectile count");
            Assert.AreEqual(weapon.FireTarget.Type, GameObjectType.Proj, "TruePD must only fire at projectiles");
        }
    }
}
