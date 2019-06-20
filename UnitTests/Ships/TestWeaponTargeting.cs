using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Microsoft.Xna.Framework;
using Ship_Game;
using Ship_Game.Gameplay;
using Ship_Game.Ships;

namespace UnitTests.Ships
{
    [TestClass]
    public class TestWeaponTargeting : StarDriveTest
    {
        public TestWeaponTargeting()
        {
            ResourceManager.LoadStarterShipsForTesting(new[]{ "Vulcan Scout" });
        }

        void CreateTestEnv(out Empire empire, out Ship ship, out Weapon weapon)
        {
            empire = EmpireManager.CreateNewEmpire("TargetingEmpire");
            ship = Ship.CreateShipAtPoint("Vulcan Scout", empire, Vector2.Zero);
            weapon = ship.Weapons.Find(w => w.UID == "VulcanCannon");
        }

        static bool FireAtPos(Weapon weapon, float x, float y)
        {
            return weapon.ManualFireTowardsPos(new Vector2(x, y));
        }

        [TestMethod]
        public void FireSingleProjectile()
        {
            CreateTestEnv(out Empire empire, out Ship ship, out Weapon weapon);
            weapon.SalvoCount = 1;
            weapon.ProjectileCount = 1;

            Assert.AreEqual(62, ship.Ordinance, "ship.Ordinance");
            Assert.AreEqual(20, ship.PowerCurrent, "ship.PowerCurrent");
            Assert.IsTrue(FireAtPos(weapon, 0, -200), "Fire must be successful");
            Assert.AreEqual(1, ship.CopyProjectiles().Length, "Invalid projectile count");
            Assert.AreEqual(62-weapon.OrdinanceRequiredToFire, ship.Ordinance, "ship.Ordinance");
            Assert.AreEqual(20, ship.PowerCurrent, "ship.PowerCurrent");
        }
        
        [TestMethod]
        public void FireCooldownPreventsFiring()
        {
            CreateTestEnv(out Empire empire, out Ship ship, out Weapon weapon);
            weapon.SalvoCount = 1;
            weapon.ProjectileCount = 1;

            Assert.IsTrue(FireAtPos(weapon, 0, -200), "Fire must be successful");
            Assert.IsFalse(FireAtPos(weapon, 0, -200), "Respect weapon cooldown");
        }
    }
}
