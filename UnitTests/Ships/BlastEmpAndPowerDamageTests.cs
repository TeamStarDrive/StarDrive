using System.Collections.Generic;
using System.Reflection;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using SDGraphics;
using Ship_Game;
using Ship_Game.Gameplay;
using Ship_Game.Ships;
using Vector2 = SDGraphics.Vector2;
#pragma warning disable CA2213

namespace UnitTests.Ships;

[TestClass]
public class BlastEmpAndPowerDamageTests : StarDriveTest
{
    public BlastEmpAndPowerDamageTests()
    {
        LoadStarterShips("Dreadnought mk1-a", "Corsair");
        CreateUniverseAndPlayerEmpire();
    }

    static void DrainShields(Ship ship)
    {
        foreach (ShipModule m in ship.GetShields())
            m.DamageShield(m.ShieldPower, null, out float _);
    }

    Ship SpawnTarget()
    {
        Ship target = SpawnShip("Dreadnought mk1-a", Player, Vector2.Zero);
        DrainShields(target);
        return target;
    }

    Projectile Fire(string weaponUid, Ship target, out Weapon w)
    {
        Ship attacker = SpawnShip("Corsair", Enemy, new Vector2(0, -3000));
        w = ResourceManager.CreateWeapon(UState, weaponUid, attacker, null);
        Assert.IsFalse(w.IsBeam, $"{weaponUid} must be a projectile weapon");
        return Projectile.Create(w, attacker, attacker.Position,
                                 (target.Position - attacker.Position).Normalized(), target, playSound: false);
    }

    static Dictionary<ShipModule, float> HealthOf(Ship ship)
    {
        var health = new Dictionary<ShipModule, float>();
        foreach (ShipModule m in ship.Modules)
            health[m] = m.Health;
        return health;
    }

    static void AssertSeveralModulesDamaged(Ship ship, Dictionary<ShipModule, float> healthBefore)
    {
        int damaged = 0;
        foreach (ShipModule m in ship.Modules)
            if (m.Health < healthBefore[m])
                ++damaged;
        AssertGreaterThan(damaged, 1, "setup: the blast must damage more than one module of each ship");
    }

    static void BlastOnTheNose(Ship target, Projectile proj)
    {
        Dictionary<ShipModule, float> healthBefore = HealthOf(target);
        Vector2 outside = target.Position + new Vector2(0, -target.Radius - 100f);
        ShipModule entry = target.RayHitTestSingle(outside, target.Position, true);
        Assert.IsNotNull(entry, "setup: the ray must find a module on the target's nose");
        proj.Touch(entry, entry.Position);
        AssertSeveralModulesDamaged(target, healthBefore);
    }

    [TestMethod]
    public void ABlastAppliesItsEmpOncePerShip()
    {
        Ship target = SpawnTarget();
        Projectile proj = Fire("RemnantNuker", target, out Weapon nuker);
        Assert.IsTrue(proj.Explodes, "setup: RemnantNuker must explode");
        AssertGreaterThan(nuker.EMPDamage, 0f, "setup: RemnantNuker must carry EMP damage");
        AssertEqual(0f, target.EMPDamage, "setup: the target starts with no EMP");

        BlastOnTheNose(target, proj);

        AssertEqual(0.01f, nuker.EMPDamage, target.EMPDamage, "the blast must apply its EMP once, not once per module");
    }

    [TestMethod]
    public void ABlastDrainsPowerOncePerShip()
    {
        Ship target = SpawnTarget();
        Projectile proj = Fire("DarkMatterCannon_1x2", target, out Weapon gun);
        AssertGreaterThan(gun.PowerDamage, 0f, "setup: DarkMatterCannon_1x2 must carry power damage");
        proj.Explodes = true;
        proj.DamageRadius = 60f;
        AssertLessThan(gun.PowerDamage * 2f, target.PowerStoreMax, "setup: the store must hold more than two drains");
        target.PowerCurrent = target.PowerStoreMax;

        BlastOnTheNose(target, proj);

        AssertEqual(0.01f, target.PowerStoreMax - gun.PowerDamage, target.PowerCurrent,
            "the blast must drain power once, not once per module");
    }

    [TestMethod]
    public void ASpaceNukeAppliesItsEmpOnceToEveryShipItCatches()
    {
        Ship target = SpawnShip("Dreadnought mk1-a", Player, Vector2.Zero);
        Ship neighbour = SpawnShip("Dreadnought mk1-a", Player, new Vector2(target.Radius * 2f + 100f, 0));
        UState.Objects.Update(TestSimStep);
        DrainShields(target);
        DrainShields(neighbour);

        Projectile proj = Fire("RemnantNuker", target, out Weapon nuker);
        proj.DamageRadius = 1000f;
        proj.DamageAmount = 500000f;

        Dictionary<ShipModule, float> neighbourBefore = HealthOf(neighbour);
        BlastOnTheNose(target, proj);
        AssertSeveralModulesDamaged(neighbour, neighbourBefore);

        AssertEqual(0.01f, nuker.EMPDamage, target.EMPDamage, "the struck ship takes the blast's EMP once");
        AssertEqual(0.01f, nuker.EMPDamage, neighbour.EMPDamage, "every other ship the blast catches takes it once too");
    }

    static ShipModule ExplodingModule(Ship ship)
    {
        foreach (ShipModule m in ship.Modules)
            if (m.Explodes && m.ExplosionDamage > 0f)
                return m;
        Assert.Fail("setup: the target must carry a module that explodes");
        return null;
    }

    static void Destroy(Ship target, GameObject shot)
    {
        ShipModule module = ExplodingModule(target);
        Dictionary<ShipModule, float> healthBefore = HealthOf(target);
        module.Damage(shot, module.Health * 100f + 100f);
        Assert.IsFalse(module.Active, "setup: the shot must destroy the module");
        AssertSeveralModulesDamaged(target, healthBefore);
    }

    [TestMethod]
    public void AModuleAShotDestroysExplodesWithoutTheShotsEmp()
    {
        Ship target = SpawnTarget();
        Projectile shot = Fire("EmpCannon", target, out Weapon gun);
        Assert.IsFalse(shot.Explodes, "setup: EmpCannon must not explode");
        AssertGreaterThan(gun.EMPDamage, 0f, "setup: EmpCannon must carry EMP damage");

        Destroy(target, shot);

        AssertEqual(0.01f, gun.EMPDamage, target.EMPDamage, "only the module the shot struck takes its EMP");
    }

    [TestMethod]
    public void AModuleAShotDestroysExplodesWithoutTheShotsPowerDamage()
    {
        Ship target = SpawnTarget();
        Projectile shot = Fire("DarkMatterCannon_1x2", target, out Weapon gun);
        Assert.IsFalse(shot.Explodes, "setup: DarkMatterCannon_1x2 must not explode");
        AssertLessThan(gun.PowerDamage * 2f, target.PowerStoreMax, "setup: the store must hold more than two drains");
        target.PowerCurrent = target.PowerStoreMax;

        Destroy(target, shot);

        AssertEqual(0.01f, target.PowerStoreMax - gun.PowerDamage, target.PowerCurrent,
            "only the module the shot struck drains power");
    }

    [TestMethod]
    public void AModuleABeamDestroysExplodesWithoutTheBeamsPowerDamage()
    {
        Ship target = SpawnTarget();
        Ship attacker = SpawnShip("Corsair", Enemy, new Vector2(0, -3000));
        Weapon ionBeam = ResourceManager.CreateWeapon(UState, "IonBeam", attacker, null);
        AssertGreaterThan(ionBeam.PowerDamage, 0f, "setup: IonBeam must carry power damage");
        var beam = new Beam(UState.CreateId(), ionBeam, attacker.Position, target.Position, ExplodingModule(target));
        AssertLessThan(ionBeam.PowerDamage * 2f, target.PowerStoreMax, "setup: the store must hold more than two drains");
        target.PowerCurrent = target.PowerStoreMax;

        Destroy(target, beam);

        AssertEqual(0.01f, target.PowerStoreMax - ionBeam.PowerDamage, target.PowerCurrent,
            "only the module the beam struck drains power");
    }

    [TestMethod]
    public void AModuleAShotDestroysDoesNotBounceTheShotWithItsExplosion()
    {
        Ship target = SpawnTarget();
        Projectile shot = Fire("EmpCannon", target, out Weapon _);
        Assert.IsFalse(shot.Explodes, "setup: EmpCannon must not explode");
        ShipModule module = ExplodingModule(target);
        target.InFrustum = true;
        UState.ViewState = UniverseScreen.UnivScreenState.ShipView;
        Vector2 flight = shot.Velocity;

        FieldInfo deflection = typeof(ShipModuleFlyweight).GetField(nameof(ShipModuleFlyweight.Deflection));
        var saved = new Dictionary<ShipModuleFlyweight, float>();
        try
        {
            foreach (ShipModule m in target.Modules)
            {
                if (m.Flyweight != module.Flyweight && saved.TryAdd(m.Flyweight, m.Deflection))
                    deflection.SetValue(m.Flyweight, 1_000_000f);
            }

            module.Damage(shot, module.Health * 100f + 100f);
            Assert.IsFalse(module.Active, "setup: the shot must destroy the module");
        }
        finally
        {
            foreach (KeyValuePair<ShipModuleFlyweight, float> fw in saved)
                deflection.SetValue(fw.Key, fw.Value);
        }

        Assert.AreEqual(Enemy, shot.Loyalty, "the module's explosion turned the shot that set it off to the victim's side");
        AssertEqual(0.01f, flight, shot.Velocity, "the module's explosion bounced the shot that set it off");
    }
}
