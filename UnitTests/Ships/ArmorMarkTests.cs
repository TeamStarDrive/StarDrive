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
/// A module whose AP resistance is set below zero, such as a turret built into an armor plate, counts as armor: weapons'
/// effect against armor applies to it and armor piercing can pass through it. Its AP resistance counts as zero, both for
/// the armor piercing a shot uses up on it and in what the module reports.
/// </summary>
[TestClass]
public class ArmorMarkTests : StarDriveTest
{
    static readonly FieldInfo APResist = typeof(ShipModuleFlyweight).GetField(nameof(ShipModuleFlyweight.APResist));
    static readonly MethodInfo TryPhaseThrough = typeof(Projectile).GetMethod("TryPhaseThroughModule", BindingFlags.NonPublic | BindingFlags.Instance);

    readonly ShipModule Turret;
    readonly Weapon AntiArmor;

    public ArmorMarkTests()
    {
        LoadStarterShips("Dreadnought mk1-a", "Corsair");
        CreateUniverseAndPlayerEmpire();
        Ship target = SpawnShip("Dreadnought mk1-a", Player, new Vector2(200_000f));
        Turret = target.Modules.First(m => m.ModuleType == ShipModuleType.Turret);
        AssertEqual(0, Turret.Flyweight.APResist, "setup: the turret has no AP resistance");

        Ship attacker = SpawnShip("Corsair", Enemy, new Vector2(-200_000f));
        Assert.IsTrue(ResourceManager.GetWeaponTemplate("2x2LaserTurret", out IWeaponTemplate template), "setup: the weapon exists");
        var copy = (WeaponTemplate)typeof(object).GetMethod("MemberwiseClone", BindingFlags.NonPublic | BindingFlags.Instance)!
                                                 .Invoke(template, null);
        copy.EffectVsArmor = 2f;
        AntiArmor = new Weapon(UState, copy, attacker, null);
    }

    // the turret's template is marked for the test and put back after
    void WithTurretAPResist(int value, System.Action test)
    {
        object saved = APResist.GetValue(Turret.Flyweight);
        try
        {
            APResist.SetValue(Turret.Flyweight, value);
            test();
        }
        finally
        {
            APResist.SetValue(Turret.Flyweight, saved);
        }
    }

    // whether a shot with the given armor piercing passes through the turret
    bool PhasesThroughTurret(int armorPiercing)
    {
        Projectile shot = Projectile.Create(AntiArmor, AntiArmor.Owner, Turret.Position + new Vector2(0f, -500f),
                                            new Vector2(0f, 1f), Turret.GetParent(), playSound: false);
        shot.ArmorPiercing = armorPiercing;
        return (bool)TryPhaseThrough.Invoke(shot, new object[] { Turret });
    }

    [TestMethod]
    public void ATurretMarkedAsArmorTakesTheWeaponsEffectAgainstArmor()
    {
        float plain = AntiArmor.GetArmorDamageMod(Turret);
        Assert.IsFalse(Turret.Is(ShipModuleType.Armor), "setup: a plain turret is not armor");

        WithTurretAPResist(-1, () =>
        {
            Assert.IsTrue(Turret.Is(ShipModuleType.Armor), "a turret marked below zero counts as armor");
            AssertEqual(0.0001f, plain * 2f, AntiArmor.GetArmorDamageMod(Turret), "a weapon's effect against armor applies to it");
        });
    }

    [TestMethod]
    public void ATurretMarkedAsArmorResistsArmorPiercingAsIfItHadNone()
    {
        int width = Turret.XSize;
        Assert.IsFalse(PhasesThroughTurret(width), "setup: armor piercing does not pass through a plain turret");

        WithTurretAPResist(-1, () =>
        {
            AssertEqual(0, Turret.APResist, "the module reports no AP resistance");
            Assert.IsTrue(PhasesThroughTurret(width), "armor piercing of the turret's width passes through it, as through armor");
            Assert.IsFalse(PhasesThroughTurret(width - 1), "but less does not: the mark gives a shot no extra piercing");
        });
    }
}
