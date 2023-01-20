using System;
using System.IO;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Ship_Game;
using Ship_Game.Gameplay;
using Ship_Game.Ships;
using Vector2 = SDGraphics.Vector2;
#pragma warning disable CA2213

namespace UnitTests.Ships
{
    [TestClass]
    public class TestWeaponModifiers : StarDriveTest
    {
        Ship Ship;
        WeaponTestWrapper Weapon;
        Empire Empire;

        public TestWeaponModifiers()
        {
            CreateUniverseAndPlayerEmpire();
            Empire = UState.CreateTestEmpire("ModifierEmpire");
            Empire.TestInitModifiers();
            Ship = SpawnShip("Vulcan Scout", Empire, Vector2.Zero);
            Weapon = CreateWeapon(Ship);
        }

        WeaponTestWrapper CreateWeapon(Ship ship)
        {
            WeaponTestWrapper weapon = (WeaponTestWrapper)ship.Weapons[0];
            weapon.TestBaseRange = 1000;
            weapon.TestDamageAmount = 15;
            weapon.TestProjectileSpeed = 1250;
            return weapon;
        }

        [TestMethod]
        public void GetActualWeaponRange()
        {
            AssertEqual(1000, Weapon.GetActualRange(Ship.Loyalty));

            WeaponTagModifier m = Empire.WeaponBonuses(WeaponTag.Kinetic);
            m.Range = 1; // +100% increase
            AssertEqual(2000, Weapon.GetActualRange(Ship.Loyalty));

            m.Range = 0.5f; // revert to +50%
            AssertEqual(1500, Weapon.GetActualRange(Ship.Loyalty));
        }

        [TestMethod]
        public void ApplyModsToProjectile()
        {
            Weapon.TestHitPoints = 100;
            Weapon.TestExplosionRadius = 10;

            Projectile p1 = Projectile.Create(Weapon, Ship, new Vector2(), Vectors.Up, null, false);
            AssertEqual(2, p1.RotationRadsPerSecond);
            AssertEqual(15, p1.DamageAmount);
            AssertEqual(1000, p1.Range);
            AssertEqual(1250, p1.Speed);
            AssertEqual(100, p1.Health);
            AssertEqual(10, p1.DamageRadius);
            AssertEqual(0, p1.ArmorPiercing);
            AssertEqual(0, p1.ArmorDamageBonus);
            AssertEqual(0, p1.ShieldDamageBonus);
            AssertEqual(false, p1.IgnoresShields);
            AssertEqual(0.96f, p1.Duration);

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

            Projectile p2 = Projectile.Create(Weapon, Ship, new Vector2(), Vectors.Up, null, false);
            AssertEqual(4, p2.RotationRadsPerSecond);
            AssertEqual(30, p2.DamageAmount);
            AssertEqual(2000, p2.Range);
            AssertEqual(2500, p2.Speed);
            AssertEqual(200, p2.Health);
            AssertEqual(20, p2.DamageRadius);
            AssertEqual(10, p2.ArmorPiercing);
            AssertEqual(10, p2.ArmorDamageBonus);
            AssertEqual(10, p2.ShieldDamageBonus);
            AssertEqual(true, p2.IgnoresShields);
            AssertEqual(0.96f, p2.Duration);
        }
    }
}
