using System;
using System.Linq;
using System.Reflection;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Ship_Game;
using Ship_Game.Gameplay;
using Ship_Game.Ships;
using Vector2 = SDGraphics.Vector2;
#pragma warning disable CA2213

namespace UnitTests.Ships;

/// <summary>
/// Deflection meets a projectile's damage with its weapon's bonus or penalty against armor or shields, but before the
/// module's resistances: a resisted shot above the deflection still hits, an anti-armor weapon still gets through
/// deflection its raw damage would not, and a weapon weak against the target is still turned away.
/// </summary>
[TestClass]
public class DeflectionOrderTests : StarDriveTest
{
    static readonly FieldInfo Deflection = typeof(ShipModuleFlyweight).GetField(nameof(ShipModuleFlyweight.Deflection));
    static readonly FieldInfo ShieldDeflection = typeof(ShipModuleFlyweight).GetField(nameof(ShipModuleFlyweight.ShieldDeflection));
    static readonly FieldInfo KineticResist = typeof(ShipModuleFlyweight).GetField(nameof(ShipModuleFlyweight.KineticResist));
    static readonly FieldInfo ShieldKineticResist = typeof(ShipModuleFlyweight).GetField(nameof(ShipModuleFlyweight.ShieldKineticResist));
    static readonly FieldInfo ExplosiveResist = typeof(ShipModuleFlyweight).GetField(nameof(ShipModuleFlyweight.ExplosiveResist));

    readonly Ship Target;
    readonly Ship Attacker;

    public DeflectionOrderTests()
    {
        LoadStarterShips("Dreadnought mk1-a", "Corsair");
        CreateUniverseAndPlayerEmpire();
        Target = SpawnShip("Dreadnought mk1-a", Player, new Vector2(200_000f));
        Attacker = SpawnShip("Corsair", Enemy, new Vector2(-200_000f));
    }

    // a kinetic shell from a copy of the Railgun with the given bonuses against armor and shields
    Projectile Shell(float effectVsArmor, float effectVsShields)
    {
        Assert.IsTrue(ResourceManager.GetWeaponTemplate("Railgun", out IWeaponTemplate template), "setup: the weapon exists");
        var copy = (WeaponTemplate)typeof(object).GetMethod("MemberwiseClone", BindingFlags.NonPublic | BindingFlags.Instance)!
                                                 .Invoke(template, null);
        copy.EffectVsArmor = effectVsArmor;
        copy.EffectVsShields = effectVsShields;
        var weapon = new Weapon(UState, copy, Attacker, null);
        Assert.IsTrue(weapon.Tag_Kinetic && !weapon.Explodes, "setup: a kinetic shell that does not explode");
        return Projectile.Create(weapon, Attacker, Target.Position + new Vector2(0f, -Target.Radius - 500f),
                                 new Vector2(0f, 1f), Target, playSound: false);
    }

    // the module's template takes the deflection and kinetic resistance for the test and gets its own back after
    static void WithModule(ShipModule module, FieldInfo deflection, FieldInfo resist, float deflectionValue, float resistValue, Action test)
    {
        object savedDeflection = deflection.GetValue(module.Flyweight);
        object savedResist = resist.GetValue(module.Flyweight);
        try
        {
            deflection.SetValue(module.Flyweight, deflectionValue);
            resist.SetValue(module.Flyweight, resistValue);
            test();
        }
        finally
        {
            deflection.SetValue(module.Flyweight, savedDeflection);
            resist.SetValue(module.Flyweight, savedResist);
        }
    }

    [TestMethod]
    [DataRow(15f, 0.5f, 1f,   true)]  // resisted to 7.5, but 15 meets the deflection of 10
    [DataRow(6f,  0f,   2f,   true)]  // an anti-armor weapon: 6 raw, 12 against armor
    [DataRow(15f, 0f,   0.5f, false)] // a weapon weak against armor: 15 raw, 7.5 against armor
    public void ArmorDeflectionCountsTheWeaponsBonusButNotTheResistance(float damage, float kineticResist, float effectVsArmor, bool hits)
    {
        ShipModule armor = Target.Modules.First(m => m.Is(ShipModuleType.Armor));
        WithModule(armor, Deflection, KineticResist, 10f, kineticResist, () =>
        {
            Projectile shell = Shell(effectVsArmor, effectVsShields: 1f);
            float healthBefore = armor.Health;
            armor.Damage(shell, damage);

            if (hits)
                AssertEqual(0.01f, healthBefore - damage * effectVsArmor * (1f - kineticResist), armor.Health, "the shell gets past the deflection");
            else
                AssertEqual(0.01f, healthBefore, armor.Health, "the shell is deflected");
        });
    }

    [TestMethod]
    [DataRow(15f, 0.5f, 1f,   true)]
    [DataRow(15f, 0f,   0.5f, false)]
    public void ShieldDeflectionCountsTheWeaponsBonusButNotTheResistance(float damage, float kineticResist, float effectVsShields, bool hits)
    {
        ShipModule shield = Target.GetShields().First();
        Assert.IsTrue(shield.ShieldsAreActive, "setup: the shield is up");
        WithModule(shield, ShieldDeflection, ShieldKineticResist, 10f, kineticResist, () =>
        {
            Projectile shell = Shell(effectVsArmor: 1f, effectVsShields);
            float powerBefore = shield.ShieldPower;
            shield.Damage(shell, damage);

            if (hits)
                AssertEqual(0.01f, powerBefore - damage * effectVsShields * (1f - kineticResist), shield.ShieldPower, "the shell gets past the deflection");
            else
                AssertEqual(0.01f, powerBefore, shield.ShieldPower, "the shell is deflected");
        });
    }

    [TestMethod]
    [DataRow(8f,  false)] // the killer's bonus against armor would make it 16
    [DataRow(12f, true)]
    public void AModuleBlastMeetsDeflectionWithItsRawDamageWhateverWeaponSetItOff(float damage, bool hits)
    {
        ShipModule armor = Target.Modules.First(m => m.Is(ShipModuleType.Armor));
        WithModule(armor, Deflection, KineticResist, 10f, 0f, () =>
        {
            Projectile killer = Shell(effectVsArmor: 2f, effectVsShields: 1f);
            float healthBefore = armor.Health;
            armor.Damage(killer, damage, out float _, moduleExplosion: true);

            if (hits)
                AssertEqual(0.01f, healthBefore - damage * 2f, armor.Health, "the blast gets past the deflection, with the killer's bonus on its damage");
            else
                AssertEqual(0.01f, healthBefore, armor.Health, "the blast is deflected");
        });
    }

    [TestMethod]
    [DataRow(15f, true)]  // 7.5 after the explosive resistance, but 15 meets the deflection of 10
    [DataRow(8f,  false)]
    public void DamageFromNoWeaponMeetsDeflectionWithItsRawDamage(float damage, bool hits)
    {
        ShipModule armor = Target.Modules.First(m => m.Is(ShipModuleType.Armor));
        WithModule(armor, Deflection, ExplosiveResist, 10f, 0.5f, () =>
        {
            float healthBefore = armor.Health;
            armor.Damage(Attacker, damage); // a ship as the source, as its death blast is

            if (hits)
                AssertEqual(0.01f, healthBefore - damage * 0.5f, armor.Health, "the hit gets past the deflection");
            else
                AssertEqual(0.01f, healthBefore, armor.Health, "the hit is deflected");
        });
    }
}
