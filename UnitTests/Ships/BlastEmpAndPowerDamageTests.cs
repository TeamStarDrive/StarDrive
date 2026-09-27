using System.Collections.Generic;
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
}
