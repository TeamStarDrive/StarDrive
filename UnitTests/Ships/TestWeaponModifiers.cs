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
    public class TestWeaponModifiers
    {
        public TestWeaponModifiers()
        {
            Directory.SetCurrentDirectory("/Projects/BlackBox/StarDrive");
            ResourceManager.LoadStarterShipsForTesting();
        }

        [TestMethod]
        public void ApplyModsToProjectile()
        {
            Empire empire = EmpireManager.CreateNewEmpire("ModifierEmpire");
            Ship ship = Ship.CreateShipAtPoint("Vulcan Scout", empire, Vector2.Zero);
            Weapon vulcan = ship.Weapons.Find(w => w.UID == "VulcanCannon");
            vulcan.HitPoints = 100;

            Projectile p1 = Projectile.Create(vulcan, new Vector2(), Vectors.Up, null, false);
            Assert.That.Equal(2, p1.RotationRadsPerSecond);
            Assert.That.Equal(15, p1.DamageAmount);
            Assert.That.Equal(1000, p1.Range);
            Assert.That.Equal(1250, p1.Speed);
            Assert.That.Equal(100, p1.Health);
            Assert.That.Equal(0, p1.DamageRadius);
            Assert.That.Equal(0, p1.ArmorPiercing);
            Assert.That.Equal(0, p1.ArmorDamageBonus);
            Assert.That.Equal(0, p1.ShieldDamageBonus);
            Assert.AreEqual(false, p1.IgnoresShields);
            Assert.That.Equal(0.96f, p1.Duration);

            WeaponTagModifier m = empire.WeaponBonuses(WeaponTag.Kinetic);
            m.Turn   = 1; // p.RotationRadsPerSecond
            m.Damage = 1; // p.DamageAmount
            m.Range  = 1; // p.Range
            m.Speed  = 1; // p.Speed
            m.Rate   = 1; // ??
            m.HitPoints         = 1; // p.Health
            m.ExplosionRadius   = 1; // p.DamageRadius
            m.ArmourPenetration = 10; // p.ArmorPiercing
            m.ArmorDamage       = 10; // p.ArmorDamageBonus
            m.ShieldDamage      = 10; // p.ShieldDamageBonus
            m.ShieldPenetration = 1; // p.IgnoresShields

            Projectile p2 = Projectile.Create(vulcan, new Vector2(), Vectors.Up, null, false);
            Assert.That.Equal(4, p2.RotationRadsPerSecond);
            Assert.That.Equal(30, p2.DamageAmount);
            Assert.That.Equal(2000, p2.Range);
            Assert.That.Equal(2500, p2.Speed);
            Assert.That.Equal(200, p2.Health);
            Assert.That.Equal(0, p2.DamageRadius);
            Assert.That.Equal(10, p2.ArmorPiercing);
            Assert.That.Equal(10, p2.ArmorDamageBonus);
            Assert.That.Equal(10, p2.ShieldDamageBonus);
            Assert.AreEqual(true, p2.IgnoresShields);
            Assert.That.Equal(0.96f, p2.Duration);
        }
    }
}
