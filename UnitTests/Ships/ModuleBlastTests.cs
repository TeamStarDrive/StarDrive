using System;
using System.Linq;
using System.Reflection;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Ship_Game;
using Ship_Game.Gameplay;
using Ship_Game.Ships;
using Vector2 = SDGraphics.Vector2;

namespace UnitTests.Ships;

[TestClass]
public class ModuleBlastTests : StarDriveTest
{
    public ModuleBlastTests()
    {
        LoadStarterShips("TEST_ShipShield");
        CreateUniverseAndPlayerEmpire();
    }

    // TEST_ShipShield 4x4 layout:
    //      x0        x1        x2         x3
    // y0   -         Amplifier Amplifier  -
    // y1   -         Cockpit   Cockpit    -
    // y2   Shield    Reactor   Reactor    Shield
    // y3   Reactor   Engine    WarpEngine Reactor

    static readonly FieldInfo ExplosionDamage = typeof(ShipModuleFlyweight).GetField(nameof(ShipModuleFlyweight.ExplosionDamage));
    static readonly FieldInfo ExplosionRadius = typeof(ShipModuleFlyweight).GetField(nameof(ShipModuleFlyweight.ExplosionRadius));

    // the test ship's reactors share one template, so its blast is set for the test and put back after; the ship outlives its reactors
    static void WithReactorBlast(int damage, int radius, Action test)
    {
        ShipModuleFlyweight reactor = ResourceManager.GetModuleTemplate("NuclearReactorSmall").Flyweight;
        object oldDamage = ExplosionDamage.GetValue(reactor);
        object oldRadius = ExplosionRadius.GetValue(reactor);
        float destroyThreshold = GlobalStats.Defaults.ShipDestroyThreshold;
        try
        {
            ExplosionDamage.SetValue(reactor, damage);
            ExplosionRadius.SetValue(reactor, radius);
            GlobalStats.Defaults.ShipDestroyThreshold = 0f;
            test();
        }
        finally
        {
            ExplosionDamage.SetValue(reactor, oldDamage);
            ExplosionRadius.SetValue(reactor, oldRadius);
            GlobalStats.Defaults.ShipDestroyThreshold = destroyThreshold;
        }
    }

    static void DrainShields(Ship ship)
    {
        foreach (ShipModule m in ship.GetShields())
            m.DamageShield(m.ShieldPower, null, out _);
    }

    static float TotalHealth(Ship ship) => ship.Modules.Sum(m => m.Health);

    float[] HealthAfterReactorBlast(Vector2 position, float rotation)
    {
        Ship ship = SpawnShip("TEST_ShipShield", Player, position);
        Ship attacker = SpawnShip("TEST_ShipShield", Enemy, position + new Vector2(1000, 0));
        ship.Rotation = rotation;
        ship.UpdateModulePositions(TestSimStep, forceUpdate: true);

        ShipModule reactor = ship.GetModuleAt(1, 2);
        Assert.IsTrue(reactor.Explodes, "setup: the module at (1,2) must be an exploding reactor");
        foreach (ShipModule m in ship.Modules)
            if (m.Explodes && m != reactor)
                m.Die(null, cleanupOnly: true); // no chain, so only this blast's reach decides what is hit
        float reactorHealth = reactor.Health;
        float before = TotalHealth(ship);
        reactor.Damage(attacker, reactorHealth + 10f, out _);
        Assert.IsFalse(reactor.Active, "setup: the reactor must be destroyed");
        AssertGreaterThan(before - reactorHealth - TotalHealth(ship), 0f, "setup: the blast must reach the modules around the reactor");
        return ship.Modules.Select(m => m.Health).ToArray();
    }

    [TestMethod]
    public void AModuleBlastHitsTheSameModulesAtAnyFacing()
    {
        // a radius at which a blast centered off its module reaches a second row on one side only
        WithReactorBlast(damage: 2400, radius: 23, () =>
        {
            float[] facingNorth = HealthAfterReactorBlast(new Vector2(0, 0), rotation: 0f);
            float[] facingSouth = HealthAfterReactorBlast(new Vector2(0, 20_000), rotation: MathF.PI);
            for (int i = 0; i < facingNorth.Length; ++i)
                AssertEqual(0.01f, facingNorth[i], facingSouth[i], $"module {i} must take the same blast at any facing");
        });
    }

    [TestMethod]
    public void ABlastDealsNoMoreThanItsDamage()
    {
        WithReactorBlast(damage: 400, radius: 40, () =>
        {
            Ship ship = SpawnShip("TEST_ShipShield", Player, new Vector2(0, 0));
            Ship attacker = SpawnShip("TEST_ShipShield", Enemy, new Vector2(1000, 0));
            DrainShields(ship);
            ShipModule reactor = ship.GetModuleAt(1, 2);
            float reactorHealth = reactor.Health;
            float before = TotalHealth(ship);
            reactor.Damage(attacker, reactorHealth + 10f, out _);
            float dealt = before - reactorHealth - TotalHealth(ship);
            AssertGreaterThan(dealt, 0f, "setup: the reactor's blast must reach other modules");
            Assert.IsTrue(dealt <= 400.01f, $"a 400 blast from a module must deal no more than 400, dealt {dealt}");

            Ship struck = SpawnShip("TEST_ShipShield", Player, new Vector2(0, 20_000));
            struck.UpdateModulePositions(TestSimStep, forceUpdate: true);
            DrainShields(struck);
            ShipModule cockpit = struck.GetModuleAt(1, 1);
            before = TotalHealth(struck);
            struck.DamageExplosive(attacker, 1000f, cockpit.Position, 40f, ignoreShields: true);
            dealt = before - TotalHealth(struck);
            Assert.IsFalse(cockpit.Active, "setup: the struck module must give way, so the rest of the blast spreads");
            Assert.IsTrue(dealt <= 1000.01f, $"a 1000 blast from outside must deal no more than 1000, dealt {dealt}");
        });
    }
}
