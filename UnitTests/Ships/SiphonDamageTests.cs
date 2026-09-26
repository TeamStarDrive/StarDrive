using Microsoft.VisualStudio.TestTools.UnitTesting;
using SDGraphics;
using Ship_Game;
using Ship_Game.Gameplay;
using Ship_Game.Ships;
using Vector2 = SDGraphics.Vector2;

namespace UnitTests.Ships
{
    /// <summary>
    /// A siphon beam drains a shield and transfers what it drained into the firing ship.
    /// It used to credit the attacker the full siphon value no matter how little the shield
    /// actually held, so a beam could refill its own power store off an almost flat shield.
    /// </summary>
    [TestClass]
    public class SiphonDamageTests : StarDriveTest
    {
        public SiphonDamageTests()
        {
            LoadStarterShips("TEST_ShipShield");
            CreateUniverseAndPlayerEmpire();
        }

        // TEST_ShipShield carries a shield at grid (0,2), see ExplosionShieldTests
        ShipModule SpawnTargetShield(out Ship target)
        {
            target = SpawnShip("TEST_ShipShield", Player, Vector2.Zero);
            ShipModule shield = target.GetModuleAt(0, 2);
            Assert.IsTrue(shield.ShieldPowerMax > 0, "module at (0,2) should be a shield");
            return shield;
        }

        Beam FireSiphonAt(Ship attacker, Ship target, ShipModule shield, out Weapon siphon)
        {
            siphon = ResourceManager.CreateWeapon(UState, "SiphonBeam", attacker, null, null);
            AssertGreaterThan(siphon.SiphonDamage, 0f, "SiphonBeam should carry a siphon value");
            return new Beam(UState.CreateId(), siphon, attacker.Position, target.Position, shield);
        }

        [TestMethod]
        public void SiphonCreditsOnlyWhatTheShieldActuallyHeld()
        {
            ShipModule shield = SpawnTargetShield(out Ship target);
            Ship attacker = SpawnShip("TEST_ShipShield", Enemy, new Vector2(500, 0));

            Beam beam = FireSiphonAt(attacker, target, shield, out Weapon siphon);

            // leave the shield holding a quarter of one siphon tick, still above the
            // ShieldsAreActive threshold of 1 so the beam is allowed to land on it
            float held = siphon.SiphonDamage * 0.25f;
            AssertGreaterThan(held, 1f, "test needs a shield charge above the ShieldsAreActive threshold");
            shield.DamageShield(shield.ShieldPower - held, null, out _);
            AssertEqual(0.01f, held, shield.ShieldPower, "precondition: shield trimmed below one siphon tick");

            attacker.PowerCurrent = 0f;
            shield.Damage(beam, siphon.DamageAmount);

            AssertEqual(0.01f, 0f, shield.ShieldPower, "the siphon should have emptied the shield");
            AssertEqual(0.01f, held, attacker.PowerCurrent,
                "attacker may only gain the power the shield actually held, not the full siphon value");
        }

        // Beam.Touch scales each tick by timeStep.FixedTime * 60 so a beam does the same work
        // per second at any sim rate. That modifier used to be dropped between Damage and
        // TryDamageModule, so the special effects ran at full value every tick no matter how
        // long the step was - and the sim auto-throttles its rate during big battles.
        [TestMethod]
        public void SiphonScalesWithTheBeamTimeStepModifier()
        {
            ShipModule shield = SpawnTargetShield(out Ship target);
            Ship attacker = SpawnShip("TEST_ShipShield", Enemy, new Vector2(500, 0));
            Beam beam = FireSiphonAt(attacker, target, shield, out Weapon siphon);

            const float modifier = 0.5f;
            AssertGreaterThan(shield.ShieldPower, siphon.SiphonDamage,
                "test needs a shield holding more than one full siphon tick");

            float before = shield.ShieldPower;
            attacker.PowerCurrent = 0f;
            shield.Damage(beam, siphon.DamageAmount, modifier);

            AssertEqual(0.01f, siphon.SiphonDamage * modifier, before - shield.ShieldPower,
                "a half length sim step must siphon half as much");
            AssertEqual(0.01f, siphon.SiphonDamage * modifier, attacker.PowerCurrent,
                "the attacker gains the scaled amount, not the full siphon value");
        }

        Projectile FireProjectileAt(Ship attacker, Ship target, string weaponUid, out Weapon w)
        {
            w = ResourceManager.CreateWeapon(UState, weaponUid, attacker, null, null);
            AssertGreaterThan(w.PowerDamage, 0f, $"{weaponUid} should carry power damage");
            Assert.IsFalse(w.IsBeam, $"{weaponUid} must be a projectile weapon for this test");
            return Projectile.Create(w, attacker, attacker.Position,
                                     (target.Position - attacker.Position).Normalized(), target, playSound: false);
        }

        [TestMethod]
        public void ProjectilePowerDamageDrainsTheTargetOnceShieldsAreDown()
        {
            ShipModule shield = SpawnTargetShield(out Ship target);
            Ship attacker = SpawnShip("TEST_ShipShield", Enemy, new Vector2(500, 0));
            Projectile proj = FireProjectileAt(attacker, target, "DarkMatterCannon_1x2", out Weapon gun);

            // an active shield absorbs the shot, so nothing reaches the power store
            Assert.IsTrue(shield.ShieldsAreActive, "precondition: shield is up");
            target.PowerCurrent = target.PowerStoreMax;
            shield.Damage(proj, gun.DamageAmount);
            AssertEqual(0.01f, target.PowerStoreMax, target.PowerCurrent,
                "power damage must not apply while the module has an active shield");

            // with the shield flattened the same hit drains the store
            shield.DamageShield(shield.ShieldPower, null, out _);
            Assert.IsFalse(shield.ShieldsAreActive, "precondition: shield is down");
            shield.Damage(proj, gun.DamageAmount);
            AssertEqual(0.01f, (target.PowerStoreMax - gun.PowerDamage).LowerBound(0), target.PowerCurrent,
                "power damage should drain the store once the shields are down");
        }

        // Deliberate design decision: power damage is applied before the deflection return,
        // so a shot too weak to hurt the module still drains its ship.
        [TestMethod]
        public void ProjectilePowerDamageDrainsEvenWhenTheShotIsDeflected()
        {
            ShipModule shield = SpawnTargetShield(out Ship target);
            Ship attacker = SpawnShip("TEST_ShipShield", Enemy, new Vector2(500, 0));
            Projectile proj = FireProjectileAt(attacker, target, "DarkMatterCannon_1x2", out Weapon gun);

            ShipModule hull = target.GetModuleAt(1, 1);
            Assert.IsFalse(hull.ShieldPowerMax > 0, "expected a non shield module at (1,1)");

            float healthBefore = hull.Health;
            target.PowerCurrent = target.PowerStoreMax;
            // zero damage is always below the module deflection, so the shot is deflected
            hull.Damage(proj, 0f);

            AssertEqual(0.01f, healthBefore, hull.Health, "a deflected shot must not damage the module");
            AssertEqual(0.01f, (target.PowerStoreMax - gun.PowerDamage).LowerBound(0), target.PowerCurrent,
                "a deflected shot still drains the power store");
        }

        [TestMethod]
        public void SiphonStaysBeamOnlyForProjectileWeapons()
        {
            ShipModule shield = SpawnTargetShield(out Ship target);
            Ship attacker = SpawnShip("TEST_ShipShield", Enemy, new Vector2(500, 0));

            Weapon emp = ResourceManager.CreateWeapon(UState, "EmpCannon", attacker, null, null);
            AssertGreaterThan(emp.SiphonDamage, 0f, "EmpCannon should carry a siphon value");
            Assert.IsFalse(emp.IsBeam, "EmpCannon must be a projectile weapon for this test");

            Projectile proj = Projectile.Create(emp, attacker, attacker.Position,
                (target.Position - attacker.Position).Normalized(), target, playSound: false);

            attacker.PowerCurrent = 0f;
            float shieldBefore = shield.ShieldPower;
            shield.Damage(proj, emp.DamageAmount);

            AssertEqual(0.01f, 0f, attacker.PowerCurrent, "only beams siphon - a projectile must gain nothing");
            Assert.IsTrue(shield.ShieldPower >= shieldBefore - emp.DamageAmount - 0.01f,
                "a projectile must not siphon the shield");
        }

        [TestMethod]
        public void SiphonCreditsTheFullValueWhenTheShieldCanPayIt()
        {
            ShipModule shield = SpawnTargetShield(out Ship target);
            Ship attacker = SpawnShip("TEST_ShipShield", Enemy, new Vector2(500, 0));

            Beam beam = FireSiphonAt(attacker, target, shield, out Weapon siphon);
            AssertGreaterThan(shield.ShieldPower, siphon.SiphonDamage,
                "test needs a shield holding more than one siphon tick");

            float before = shield.ShieldPower;
            attacker.PowerCurrent = 0f;
            shield.Damage(beam, siphon.DamageAmount);

            AssertEqual(0.01f, siphon.SiphonDamage, before - shield.ShieldPower, "shield should lose the full siphon");
            AssertEqual(0.01f, siphon.SiphonDamage, attacker.PowerCurrent, "attacker should gain the full siphon");
        }
    }
}
