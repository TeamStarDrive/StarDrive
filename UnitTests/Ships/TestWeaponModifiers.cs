using System;
using System.IO;
using System.Reflection;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using SDUtils;
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

        static readonly string[] CloneUids = { "ZzLauncher", "ZzWarhead", "ZzGuided", "ZzUnguided" };

        static Map<string, IWeaponTemplate> Weapons => (Map<string, IWeaponTemplate>)typeof(ResourceManager)
            .GetField("WeaponsDict", BindingFlags.NonPublic | BindingFlags.Static)!.GetValue(null);

        static WeaponTemplate CloneWeapon(string uid, string cloneUid)
        {
            Assert.IsTrue(ResourceManager.GetWeaponTemplate(uid, out IWeaponTemplate t), $"setup: no weapon {uid}");
            var clone = (WeaponTemplate)typeof(object).GetMethod("MemberwiseClone", BindingFlags.NonPublic | BindingFlags.Instance)!
                                                      .Invoke(t, null);
            clone.UID = cloneUid;
            Weapons[cloneUid] = clone;
            return clone;
        }

        static (WeaponTemplate Launcher, WeaponTemplate Warhead) CloneClusterMissile()
        {
            WeaponTemplate warhead = CloneWeapon("ClusterMirv", "ZzWarhead");
            WeaponTemplate launcher = CloneWeapon("ClusterMissiles", "ZzLauncher");
            launcher.MirvWeapon = warhead.UID;
            launcher.SalvoDuration = 0f;
            return (launcher, warhead);
        }

        [TestCleanup]
        public override void Cleanup()
        {
            foreach (string uid in CloneUids)
                Weapons.Remove(uid);
            base.Cleanup();
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
            AssertEqual(false, p1.IgnoresShields);
            AssertEqual(0.96f, p1.Duration);

            WeaponTagModifier m = Empire.WeaponBonuses(WeaponTag.Kinetic);
            m.Turn   = 1; // p.RotationRadsPerSecond
            m.Damage = 1; // p.DamageAmount
            m.Range  = 1; // p.Range
            m.Speed  = 1; // p.Speed
            m.HitPoints         = 1; // p.Health
            m.ExplosionRadius   = 1; // p.ExplosionRadius
            m.ShieldPenetration = 1; // p.IgnoresShields

            Projectile p2 = Projectile.Create(Weapon, Ship, new Vector2(), Vectors.Up, null, false);
            AssertEqual(4, p2.RotationRadsPerSecond);
            AssertEqual(30, p2.DamageAmount);
            AssertEqual(2000, p2.Range);
            AssertEqual(2500, p2.Speed);
            AssertEqual(200, p2.Health);
            AssertEqual(20, p2.DamageRadius);
            AssertEqual(0, p2.ArmorPiercing);
            AssertEqual(true, p2.IgnoresShields);
            AssertEqual(0.96f, p2.Duration);
        }

        [TestMethod]
        public void ShieldPenetrationBonusesAddUp()
        {
            Assert.IsTrue(Weapon.Tag_Kinetic, "setup: the test weapon must be kinetic");
            Empire.data.ShieldPenBonusChance = 0.6f;
            Empire.WeaponBonuses(WeaponTag.Kinetic).ShieldPenetration = 0.6f;

            float chance = WeaponTemplate.ActualShieldPenChance(Weapon, Empire.data);
            AssertEqual(0.001f, Weapon.ShieldPenChance + 120f, chance, "the design screen adds the empire bonus, the weapon's own chance and every tag bonus");

            for (int i = 0; i < 50; ++i)
            {
                Projectile p = Projectile.Create(Weapon, Ship, new Vector2(), Vectors.Up, null, false);
                Assert.IsTrue(p.IgnoresShields, "combat must add the bonuses as the screen does; 120% always passes shields");
            }

            IWeaponTemplate darkMatter = ResourceManager.GetWeaponTemplate("DarkMatterCannon_1x2");
            Assert.IsFalse(darkMatter.Tag_Kinetic, "setup: the second weapon must not carry the kinetic bonus");
            Empire.data.ShieldPenBonusChance = 0.1f;
            AssertEqual(0.001f, darkMatter.ShieldPenChance + 10f, WeaponTemplate.ActualShieldPenChance(darkMatter, Empire.data),
                        "the empire bonus adds to a weapon's own chance instead of acting as a minimum");
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

        [TestMethod]
        public void TheBlastRadiusDoesNotScaleTheEmpRating()
        {
            var t = (WeaponTemplate)ResourceManager.GetWeaponTemplate("RemnantNuker");
            AssertGreaterThan(t.EMPDamage, 0f, "setup: RemnantNuker should carry EMP damage");
            AssertGreaterThan(t.ExplosionRadius, 16f, "setup: RemnantNuker should have a blast radius");

            float emp = t.EMPDamage;
            float radius = t.ExplosionRadius;
            float EmpRating(float blastRadius)
            {
                t.ExplosionRadius = blastRadius;
                t.EMPDamage = emp;
                float withEmp = WeaponTemplate.CalculateOffense(null, t);
                t.EMPDamage = 0f;
                return withEmp - WeaponTemplate.CalculateOffense(null, t);
            }
            try
            {
                float atRadius = EmpRating(radius);
                AssertGreaterThan(atRadius, 0f, "EMP damage must raise the offense rating");
                AssertEqual(atRadius * 0.001f, atRadius, EmpRating(radius * 2f),
                    "a blast applies its EMP once per ship, so a bigger blast must not rate it higher");
            }
            finally
            {
                t.EMPDamage = emp;
                t.ExplosionRadius = radius;
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

        // Resupply triggers below the ordnance of the costliest full shot, not of one projectile.
        [TestMethod]
        public void OrdnanceMinIsTheCostOfAFullShot()
        {
            LoadStarterShips("Alliance-Class Mk Ia Hvy Assault");
            Ship ship = SpawnShip("Alliance-Class Mk Ia Hvy Assault", Player, Vector2.Zero);

            float perShot = 0f, perProjectile = 0f;
            foreach (Weapon w in ship.Weapons)
            {
                if (w.TruePD || w.Tag_PD) continue;
                perShot = Math.Max(perShot, w.OrdnancePerShot);
                perProjectile = Math.Max(perProjectile, w.OrdinanceRequiredToFire);
            }
            AssertGreaterThan(perShot, perProjectile, "precondition: the design's costliest shot is a multi-projectile one");
            AssertEqual(0.001f, perShot, ship.OrdnanceMin);
        }

        // MIRV warheads come out of the launcher's shots, so they are rated at its fire rate and salvo
        [TestMethod]
        public void AMirvWarheadIsRatedAtItsLaunchersRate()
        {
            (WeaponTemplate launcher, WeaponTemplate warhead) = CloneClusterMissile();
            float rating = WeaponTemplate.CalculateOffense(null, launcher);
            AssertGreaterThan(rating, 0f, "setup: the launcher must have a rating");

            warhead.FireDelay = launcher.FireDelay * 0.1f;
            warhead.SalvoCount = 5;
            AssertEqual(rating * 0.0001f, rating, WeaponTemplate.CalculateOffense(null, launcher),
                        "a warhead's own fire delay and salvo are never used, so they must not change the rating");

            launcher.SalvoCount *= 2;
            AssertEqual(rating * 0.0001f, rating * 2f, WeaponTemplate.CalculateOffense(null, launcher),
                        "twice the launcher's salvo must double its warheads' rating along with its own");

            launcher.ProjectileCount *= 2;
            AssertEqual(rating * 0.0001f, rating * 4f, WeaponTemplate.CalculateOffense(null, launcher),
                        "twice the launcher's projectiles must double its warheads' rating along with its own");

            launcher.FireDelay *= 4f;
            AssertEqual(rating * 0.0001f, rating, WeaponTemplate.CalculateOffense(null, launcher),
                        "a quarter of the launcher's fire rate must quarter its warheads' rating along with its own");
        }

        static float RatingWithoutWarheads(WeaponTemplate launcher)
        {
            string warhead = launcher.MirvWeapon;
            launcher.MirvWeapon = null;
            float rating = WeaponTemplate.CalculateOffense(null, launcher);
            launcher.MirvWeapon = warhead;
            return rating;
        }

        [TestMethod]
        public void AMirvLaunchersOwnDamageCountsAQuarter()
        {
            (WeaponTemplate launcher, WeaponTemplate warhead) = CloneClusterMissile();
            float alone = RatingWithoutWarheads(launcher);
            AssertGreaterThan(alone, 0f, "setup: the launcher must have a rating of its own");

            warhead.DamageAmount = 0f;
            warhead.EMPDamage = 0f;
            warhead.PowerDamage = 0f;
            AssertEqual(alone * 0.0001f, alone * 0.25f, WeaponTemplate.CalculateOffense(null, launcher),
                        "a launcher that splits lands its own damage only if it hits first, so it counts a quarter");
        }

        [TestMethod]
        public void AMirvWarheadsOwnOffenseModifierScalesItsShare()
        {
            (WeaponTemplate launcher, WeaponTemplate warhead) = CloneClusterMissile();
            float total = WeaponTemplate.CalculateOffense(null, launcher);
            float launcherShare = RatingWithoutWarheads(launcher) * 0.25f;
            AssertGreaterThan(total, launcherShare, "setup: the warheads must add to the launcher's rating");

            warhead.OffPowerMod *= 2f;
            float warheadShare = total - launcherShare;
            AssertEqual(total * 0.0001f, launcherShare + warheadShare * 2f, WeaponTemplate.CalculateOffense(null, launcher),
                        "the warhead's offense modifier must scale its own share of the rating");
        }

        // The launcher's flight and then each warhead's own leg can be cut short by interception and a delayed ignition
        [TestMethod]
        public void TheLaunchersFlightAndTheWarheadsOwnLegBothCount()
        {
            (WeaponTemplate launcher, WeaponTemplate warhead) = CloneClusterMissile();
            launcher.DelayedIgnition = 0f;
            warhead.DelayedIgnition = 0f;
            warhead.Tag_Intercept = false;
            float total = WeaponTemplate.CalculateOffense(null, launcher);
            float launcherShare = RatingWithoutWarheads(launcher) * 0.25f;
            float warheadShare = total - launcherShare;
            AssertGreaterThan(warheadShare, 0f, "setup: the warheads must add to the launcher's rating");

            warhead.DelayedIgnition = 5f;
            AssertEqual(total * 0.0001f, launcherShare + warheadShare * 0.5f, WeaponTemplate.CalculateOffense(null, launcher),
                        "a warhead's own delayed ignition must lower its share");

            warhead.Tag_Intercept = true;
            warhead.Tag_Missile = true;
            warhead.RotationRadsPerSecond = 2f;
            warhead.HitPoints = 20f;
            warhead.ProjectileRadius = 4f;
            AssertEqual(total * 0.0001f, launcherShare + warheadShare * 0.5f * 0.8f * 1.1f, WeaponTemplate.CalculateOffense(null, launcher),
                        "a warhead that can be shot down on its own leg must rate by its interception and hit points");

            float withWarheadLeg = WeaponTemplate.CalculateOffense(null, launcher);
            launcher.DelayedIgnition = 5f;
            AssertEqual(total * 0.0001f, withWarheadLeg * 0.5f, WeaponTemplate.CalculateOffense(null, launcher),
                        "the launcher's delayed ignition must lower both its own share and its warheads'");
        }

        [TestMethod]
        public void AMirvWarheadIsRatedOverItsLaunchersReach()
        {
            (WeaponTemplate launcher, WeaponTemplate warhead) = CloneClusterMissile();
            float rating = WeaponTemplate.CalculateOffense(null, launcher);
            AssertGreaterThan(rating, 0f, "setup: the launcher must have a rating");

            warhead.BaseRange *= 3f;
            AssertEqual(rating * 0.0001f, rating, WeaponTemplate.CalculateOffense(null, launcher),
                        "warheads split off near the target, so their own range must not change the rating");

            warhead.ProjectileSpeed *= 2f;
            AssertEqual(rating * 0.0001f, rating, WeaponTemplate.CalculateOffense(null, launcher),
                        "warheads split off near the target, so their own speed must not change the rating");
        }

        [TestMethod]
        public void AModulesArcAndTurretBonusesCountOnceForAMirvWeapon()
        {
            (WeaponTemplate launcher, _) = CloneClusterMissile();
            ShipModule module = ResourceManager.GetModuleTemplate("FlakTurret");
            Assert.IsFalse(launcher.Tag_Kinetic, "setup: the launcher must not take the kinetic power penalties");
            Assert.AreEqual(ShipModuleType.Turret, module.ModuleType, "setup: the module must be a turret");
            float arcBonus = Math.Clamp(module.FieldOfFire / (MathF.PI * 0.34f), 1f, 4f);
            AssertGreaterThan(arcBonus, 1f, "setup: the module needs a fire arc wide enough to earn a bonus");

            float bare = WeaponTemplate.CalculateOffense(null, launcher);
            AssertEqual(bare * 0.0001f, bare * arcBonus * 1.25f, WeaponTemplate.CalculateOffense(module, launcher),
                        "the module's bonuses apply once, to the launcher and its warheads together");
        }

        // Range counts for a guided weapon by its square root, less steeply than for an unguided one
        [TestMethod]
        public void RangeRaisesAGuidedWeaponsRatingLessSteeplyThanAnUnguidedOne()
        {
            WeaponTemplate missile = CloneWeapon("Missile", "ZzGuided");
            WeaponTemplate gun = CloneWeapon("MassDriver", "ZzUnguided");
            Assert.IsTrue(missile.Tag_Guided && !gun.Tag_Guided, "setup: one guided and one unguided weapon");
            gun.FireImprecisionAngle = 0;

            float RatingAt(WeaponTemplate t, float range)
            {
                t.BaseRange = range;
                return WeaponTemplate.CalculateOffense(null, t);
            }
            float missileAt4K = RatingAt(missile, 4000f);
            float gunAt4K = RatingAt(gun, 4000f);
            AssertEqual(missileAt4K * 0.0001f, missileAt4K * 2f, RatingAt(missile, 16000f),
                        "4x the range must rate a guided weapon 2x");
            AssertEqual(gunAt4K * 0.0001f, gunAt4K * 4f, RatingAt(gun, 16000f),
                        "4x the range must rate an unguided weapon 4x");
        }
    }
}
