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

        static Array<Projectile> NoProjectiles = new Array<Projectile>();
        static Array<Ship> NoShips = new Array<Ship>();

        static bool FireAtTarget(Weapon weapon, Ship target)
        {
            return weapon.UpdateAndFireAtTarget(target, NoProjectiles, NoShips);
        }

        void CreateShipWithFieldOfFire(float fieldOfFireDegrees, out Ship ship, out Weapon weapon)
        {
            ship = Ship.CreateShipAtPoint("Vulcan Scout", Player, Vector2.Zero);
            ship.UpdateShipStatus(0f); // update module pos
            weapon = ship.Weapons.Find(w => w.UID == "VulcanCannon");
            weapon.SalvoCount = 1;
            weapon.ProjectileCount = 1;
            weapon.Module.FieldOfFire = fieldOfFireDegrees.ToRadians();
        }

        void SpawnEnemy(out Ship target, Vector2 position)
        {
            target = Ship.CreateShipAtPoint("Vulcan Scout", Enemy, position);
            target.UpdateShipStatus(0f); // update module pos
        }

        [TestMethod]
        public void TestFiringInFrontOfUs()
        {
            CreateShipWithFieldOfFire(45, out Ship ship, out Weapon weapon);
            SpawnEnemy(out Ship target, new Vector2(0, -200f));
            
            Assert.IsTrue(FireAtTarget(weapon, target), "Fire at target must succeed");
            Assert.AreEqual(1, ship.CopyProjectiles().Length, "Invalid projectile count");
        }
    }
}
