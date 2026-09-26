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

        // PowerDamage used to be counted only in the IsBeam branch of CalculateOffense, so a
        // projectile weapon carrying it was rated as if the stat did nothing - which it did,
        // until projectiles started applying it.
        [TestMethod]
        public void ProjectilePowerDamageRaisesTheOffenseRating()
        {
            var t = (WeaponTemplate)ResourceManager.GetWeaponTemplate("DarkMatterCannon_1x2");
            Assert.IsFalse(t.IsBeam, "this test needs a projectile weapon");
            AssertGreaterThan(t.PowerDamage, 0f, "DarkMatterCannon_1x2 should carry power damage");

            float original = t.PowerDamage;
            try
            {
                float withStat = WeaponTemplate.CalculateOffense(null, t);
                t.PowerDamage = 0f;
                float withoutStat = WeaponTemplate.CalculateOffense(null, t);
                AssertGreaterThan(withStat, withoutStat,
                    "power damage must raise a projectile weapon's offense rating");
            }
            finally
            {
                t.PowerDamage = original;
            }
        }

        // Every projectile in a shot is paid for: a cannon that spawns 3 per trigger pull costs
        // 3x its listed price. The runtime used to charge once per shot however many it spawned,
        // so multi-projectile weapons fired far cheaper than the design screen said.
        [TestMethod]
        public void EveryProjectileInAShotIsPaidFor()
        {
            Weapon.TestProjectileCount = 3;
            Weapon.TestSalvoCount = 1;
            Weapon.TestPowerRequiredToFire = 5;
            Weapon.TestOrdinanceRequiredToFire = 5;
            Weapon.CooldownTimer = 0;

            Ship.PowerCurrent = Ship.PowerStoreMax;
            Ship.ChangeOrdnance(Ship.OrdinanceMax);
            float powerBefore = Ship.PowerCurrent;
            float ordnanceBefore = Ship.Ordinance;
            AssertGreaterThan(powerBefore, 15f, "test ship needs a store that covers all 3 projectiles");

            Assert.IsTrue(Weapon.ManualFireTowardsPos(new Vector2(0, -2000)), "weapon should have fired");

            AssertEqual(0.01f, 15f, powerBefore - Ship.PowerCurrent, "3 projectiles must cost 3x the power");
            AssertEqual(0.01f, 15f, ordnanceBefore - Ship.Ordinance, "3 projectiles must cost 3x the ordnance");
        }

        // The design screen reads the same rule: cost x projectiles x salvo.
        [TestMethod]
        public void TheDesignScreenChargesPerProjectileAndSalvo()
        {
            IWeaponTemplate flak = ResourceManager.GetWeaponTemplate("DualFlak");
            AssertGreaterThan(flak.ProjectileCount, 1, "DualFlak should fire several projectiles per shot");
            AssertGreaterThan(flak.OrdinanceRequiredToFire, 0f, "DualFlak should cost ordnance");
            AssertEqual(0.001f, flak.OrdinanceRequiredToFire * flak.ProjectileCount * flak.SalvoCount,
                flak.TotalOrdnanceUsagePerFire, "burst ordnance is per projectile, per salvo shot");

            IWeaponTemplate aegis = ResourceManager.GetWeaponTemplate("REAegis");
            AssertGreaterThan(aegis.ProjectileCount, 1, "REAegis should fire several projectiles per shot");
            AssertGreaterThan(aegis.PowerRequiredToFire, 0f, "REAegis should cost power");
            AssertEqual(0.001f,
                aegis.PowerRequiredToFire * aegis.ProjectileCount * aegis.SalvoCount / aegis.NetFireDelay,
                aegis.PowerFireUsagePerSecond, "weapon power drain is per projectile, per salvo shot");
        }

        // A shot it cannot pay for in full must not fire at all, or the store goes negative.
        [TestMethod]
        public void AShotIsRefusedWhenOnlyOneProjectileCanBePaidFor()
        {
            Weapon.TestProjectileCount = 3;
            Weapon.TestSalvoCount = 1;
            Weapon.TestPowerRequiredToFire = 5;
            Weapon.TestOrdinanceRequiredToFire = 0;
            Weapon.CooldownTimer = 0;

            Ship.PowerCurrent = 10; // covers one projectile at 5, not three at 15
            Assert.IsFalse(Weapon.ManualFireTowardsPos(new Vector2(0, -2000)),
                "the weapon cannot afford all 3 projectiles and must not fire");
            AssertEqual(0.01f, 10f, Ship.PowerCurrent, "a refused shot must not spend power");
        }
    }
}
