using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Ship_Game;
using Ship_Game.Gameplay;
using Ship_Game.Ships;
using Vector2 = SDGraphics.Vector2;

namespace UnitTests.Ships
{
    [TestClass]
    public class TestWeaponMunition : StarDriveTest
    {
        public TestWeaponMunition()
        {
            CreateUniverseAndPlayerEmpire();
        }

        static bool FireAtVisiblePoint(Weapon weapon)
        {
            return weapon.ManualFireTowardsPos(new Vector2(0, -200));
        }

        void CreateWeapon(out Ship ship, out WeaponTestWrapper weapon, float ordCost, float pwrCost)
        {
            ship = SpawnShip("TEST_Vulcan Scout", Player, Vector2.Zero);
            weapon = (WeaponTestWrapper)ship.Weapons[0];

            weapon.TestSalvoCount = 1;
            weapon.TestProjectileCount = 1;
            weapon.TestOrdinanceRequiredToFire = ordCost;
            weapon.TestPowerRequiredToFire = pwrCost;
        }

        [TestMethod]
        public void FireOrdnanceProjectile()
        {
            CreateWeapon(out Ship ship, out WeaponTestWrapper weapon, ordCost:1, pwrCost:0);

            Assert.AreEqual(24, ship.Ordinance, "ship.Ordinance");
            Assert.AreEqual(20, ship.PowerCurrent, "ship.PowerCurrent");
            Assert.IsTrue(FireAtVisiblePoint(weapon), "Fire must be successful");
            Assert.AreEqual(1, GetProjectileCount(ship), "Invalid projectile count");
            Assert.AreEqual(23, ship.Ordinance, "ship.Ordinance");
            Assert.AreEqual(20, ship.PowerCurrent, "ship.PowerCurrent");
        }
        
        [TestMethod]
        public void FirePowerProjectile()
        {
            CreateWeapon(out Ship ship, out WeaponTestWrapper weapon, ordCost:0, pwrCost:1);

            Assert.AreEqual(24, ship.Ordinance, "ship.Ordinance");
            Assert.AreEqual(20, ship.PowerCurrent, "ship.PowerCurrent");
            Assert.IsTrue(FireAtVisiblePoint(weapon), "Fire must be successful");
            Assert.AreEqual(1, GetProjectileCount(ship), "Invalid projectile count");
            Assert.AreEqual(24, ship.Ordinance, "ship.Ordinance");
            Assert.AreEqual(19, ship.PowerCurrent, "ship.PowerCurrent");
        }

        [TestMethod]
        public void FireCooldownPreventsFiring()
        {
            CreateWeapon(out Ship _, out WeaponTestWrapper weapon, ordCost:1, pwrCost:0);

            Assert.IsTrue(FireAtVisiblePoint(weapon), "Fire must be successful");
            Assert.IsTrue(weapon.CooldownTimer > 0f, "Weapon should be in cooldown");
            Assert.IsFalse(FireAtVisiblePoint(weapon), "Respect weapon cooldown");
            
            weapon.CooldownTimer = 0f;
            Assert.IsTrue(FireAtVisiblePoint(weapon), "Fire after cooldown reset");
        }
        
        [TestMethod]
        public void FireOrdinancePreventsFiring()
        {
            CreateWeapon(out Ship ship, out WeaponTestWrapper weapon, ordCost:1, pwrCost:0);

            ship.SetOrdnance(0.99f);
            Assert.IsFalse(FireAtVisiblePoint(weapon), "Weapon has no ordnance");
            Assert.AreEqual(0.99f, ship.Ordinance);
            
            ship.SetOrdnance(1f);
            Assert.IsTrue(FireAtVisiblePoint(weapon), "Weapon has exact ordnance");
            Assert.AreEqual(0f, ship.Ordinance);
        }
        
        [TestMethod]
        public void FireEnergyPreventsFiring()
        {
            CreateWeapon(out Ship ship, out WeaponTestWrapper weapon, ordCost:0, pwrCost:1);

            ship.PowerCurrent = 0.99f;
            Assert.IsFalse(FireAtVisiblePoint(weapon), "Weapon should not have enough power to fire");
            Assert.AreEqual(0.99f, ship.PowerCurrent, "ship.PowerCurrent");

            ship.PowerCurrent = 1f;
            Assert.IsTrue(FireAtVisiblePoint(weapon), "Weapon should have power to fire");
            Assert.AreEqual(0f, ship.PowerCurrent, "ship.PowerCurrent");
        }

        [TestMethod]
        public void FireWarpPreventsFiring()
        {
            CreateWeapon(out Ship ship, out WeaponTestWrapper weapon, ordCost:1, pwrCost:0);

            ship.engineState = Ship.MoveState.Warp;
            Assert.IsFalse(FireAtVisiblePoint(weapon), "Cannot fire in warp");

            ship.engineState = Ship.MoveState.Sublight;
            Assert.IsTrue(FireAtVisiblePoint(weapon), "Can fire in sub-light");
        }

        [TestMethod]
        public void FirePoweredOrDeathPreventsFiring()
        {
            CreateWeapon(out Ship ship, out WeaponTestWrapper weapon, ordCost:1, pwrCost:0);

            weapon.Module.SetHealth(0);
            Assert.IsFalse(FireAtVisiblePoint(weapon), "Cannot fire if dead");

            weapon.Module.SetHealth(weapon.Module.ActualMaxHealth);
            weapon.Module.Powered = false;
            Assert.IsFalse(FireAtVisiblePoint(weapon), "Cannot fire if not powered");

            weapon.Module.Powered = true;
            Assert.IsTrue(FireAtVisiblePoint(weapon), "Can fire if alive and powered");
        }

        [TestMethod]
        public void FireAtVisibleTarget()
        {
            CreateWeapon(out Ship ship, out WeaponTestWrapper weapon, ordCost: 1, pwrCost: 0);

            weapon.Module.SetHealth(0);
            Assert.IsFalse(FireAtVisiblePoint(weapon), "Cannot fire if dead");

            weapon.Module.SetHealth(weapon.Module.ActualMaxHealth);
            weapon.Module.Powered = false;
            Assert.IsFalse(FireAtVisiblePoint(weapon), "Cannot fire if not powered");

            weapon.Module.Powered = true;
            Assert.IsTrue(FireAtVisiblePoint(weapon), "Can fire if alive and powered");
        }
    }
}
