using System;
using System.IO;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Ship_Game;
using Ship_Game.Gameplay;
using Microsoft.Xna.Framework;
using Ship_Game.Ships;

namespace UnitTests.Ships
{
    [TestClass]
    public class TestWeaponModifiers : StarDriveTest
    {
        Ship Ship;
        Weapon Weapon;
        Empire Empire;

        public TestWeaponModifiers()
        {
            CreateUniverseAndPlayerEmpire();
            Empire = EmpireManager.CreateNewEmpire("ModifierEmpire");
            Empire.TestInitModifiers();
            Ship = SpawnShip("Vulcan Scout", Empire, Vector2.Zero);
            Weapon = CreateWeapon(Ship);
        }

        Weapon CreateWeapon(Ship ship)
        {
            Weapon weapon = ship.Weapons[0];
            weapon.BaseRange = 1000;
            weapon.DamageAmount = 15;
            weapon.ProjectileSpeed = 1250;
            return weapon;
        }

        [TestMethod]
        public void GetActualWeaponRange()
        {
            Assert.That.Equal(1000, Weapon.GetActualRange());

            WeaponTagModifier m = Empire.WeaponBonuses(WeaponTag.Kinetic);
            m.Range = 1; // +100% increase
            Assert.That.Equal(2000, Weapon.GetActualRange());

            m.Range = 0.5f; // revert to +50%
            Assert.That.Equal(1500, Weapon.GetActualRange());
        }

        [TestMethod]
        public void ApplyModsToProjectile()
        {
            Weapon.HitPoints = 100;
            Weapon.ExplosionRadius = 10;

            Projectile p1 = Projectile.Create(Weapon, new Vector2(), Vectors.Up, null, false);
            Assert.That.Equal(2, p1.RotationRadsPerSecond);
            Assert.That.Equal(15, p1.DamageAmount);
            Assert.That.Equal(1000, p1.Range);
            Assert.That.Equal(1250, p1.Speed);
            Assert.That.Equal(100, p1.Health);
            Assert.That.Equal(10, p1.DamageRadius);
            Assert.That.Equal(0, p1.ArmorPiercing);
            Assert.That.Equal(0, p1.ArmorDamageBonus);
            Assert.That.Equal(0, p1.ShieldDamageBonus);
            Assert.AreEqual(false, p1.IgnoresShields);
            Assert.That.Equal(0.96f, p1.Duration);

            WeaponTagModifier m = Empire.WeaponBonuses(WeaponTag.Kinetic);
            m.Turn   = 1; // p.RotationRadsPerSecond
            m.Damage = 1; // p.DamageAmount
            m.Range  = 1; // p.Range
            m.Speed  = 1; // p.Speed
            m.Rate   = 1; // ??
            m.HitPoints         = 1; // p.Health
            m.ExplosionRadius   = 1; // p.ExplosionRadius
            m.ArmourPenetration = 10; // p.ArmorPiercing
            m.ArmorDamage       = 10; // p.ArmorDamageBonus
            m.ShieldDamage      = 10; // p.ShieldDamageBonus
            m.ShieldPenetration = 1; // p.IgnoresShields

            Projectile p2 = Projectile.Create(Weapon, new Vector2(), Vectors.Up, null, false);
            Assert.That.Equal(4, p2.RotationRadsPerSecond);
            Assert.That.Equal(30, p2.DamageAmount);
            Assert.That.Equal(2000, p2.Range);
            Assert.That.Equal(2500, p2.Speed);
            Assert.That.Equal(200, p2.Health);
            Assert.That.Equal(20, p2.DamageRadius);
            Assert.That.Equal(10, p2.ArmorPiercing);
            Assert.That.Equal(10, p2.ArmorDamageBonus);
            Assert.That.Equal(10, p2.ShieldDamageBonus);
            Assert.AreEqual(true, p2.IgnoresShields);
            Assert.That.Equal(0.96f, p2.Duration);
        }
    }
}
