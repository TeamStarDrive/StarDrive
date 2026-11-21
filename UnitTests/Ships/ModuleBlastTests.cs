using System;
using System.Linq;
using System.Reflection;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Ship_Game;
using Ship_Game.Gameplay;
using Ship_Game.Ships;
using Point = SDGraphics.Point;
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
    static readonly FieldInfo ShieldRadius = typeof(ShipModuleFlyweight).GetField(nameof(ShipModuleFlyweight.ShieldRadius));

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

    // the modules at a grid cell as a hit sees them, walked as a foreach walks them
    static ShipModule[] ModulesAt(Ship ship, Point cell, bool checkShields)
    {
        var found = new System.Collections.Generic.List<ShipModule>(8);
        foreach (ShipModule m in ship.Grid.GetModulesAt(ship.Modules, cell, checkShields))
            found.Add(m);
        return found.ToArray();
    }

    [TestMethod]
    public void AGridCellGivesItsActiveShieldsFirstThenItsModule()
    {
        Ship ship = SpawnShip("TEST_ShipShield", Player, Vector2.Zero);
        var cell = new Point(0, 2);
        ShipModule onCell = ship.GetModuleAt(0, 2);

        ShipModule[] withShields = ModulesAt(ship, cell, checkShields: true);
        Assert.IsTrue(withShields.Length >= 2, $"setup: the shield's bubble covers its own cell, then the shield itself is the module there: {withShields.Length}");
        for (int i = 0; i < withShields.Length - 1; ++i)
            Assert.IsTrue(withShields[i].ShieldsAreActive, $"the bubbles come first: {withShields[i].UID}");
        Assert.AreEqual(onCell, withShields[withShields.Length - 1], "and the module on the cell last");
        CollectionAssert.AreEqual(new[] { onCell }, ModulesAt(ship, cell, checkShields: false), "without shields, only the module");

        DrainShields(ship);
        CollectionAssert.AreEqual(new[] { onCell }, ModulesAt(ship, cell, checkShields: true), "a bubble that is down is no hit");
        onCell.Die(null, cleanupOnly: true);
        Assert.AreEqual(0, ModulesAt(ship, cell, checkShields: false).Length, "nor is a dead module");
    }

    [TestMethod]
    public void WalkingAGridCellTakesNothingOffTheHeap()
    {
        Ship ship = SpawnShip("TEST_ShipShield", Player, Vector2.Zero);
        var cell = new Point(0, 2);
        int count = 0;
        foreach (ShipModule _ in ship.Grid.GetModulesAt(ship.Modules, cell, checkShields: true))
            ++count;

        long before = GC.GetAllocatedBytesForCurrentThread();
        foreach (ShipModule _ in ship.Grid.GetModulesAt(ship.Modules, cell, checkShields: true))
            --count;
        Assert.AreEqual(0L, GC.GetAllocatedBytesForCurrentThread() - before, "a hit test walks the grid without an enumerator on the heap");
        Assert.AreEqual(0, count, "and finds the same modules each time");
    }

    [TestMethod]
    public void TheClosestModuleIsTheNearestActiveOneAndItsSearchAllocatesNothing()
    {
        Ship ship = SpawnShip("TEST_ShipShield", Player, Vector2.Zero);
        var points = new[] { new Vector2(-300, 0), new Vector2(0, 250), new Vector2(17, -9), new Vector2(5000, 4000), Vector2.Zero };
        foreach (Vector2 p in points)
        {
            float nearest = ship.Modules.Where(m => m.Active).Min(m => m.Position.Distance(p));
            AssertEqual(0.001f, nearest, ship.FindClosestModule(p).Position.Distance(p), $"the nearest module to {p}");
        }

        ShipModule closest = ship.FindClosestModule(points[0]);
        closest.Die(null, cleanupOnly: true);
        Assert.AreNotEqual(closest, ship.FindClosestModule(points[0]), "a dead module is never the closest");

        ship.FindClosestModule(points[1]);
        long before = GC.GetAllocatedBytesForCurrentThread();
        ship.FindClosestModule(points[1]);
        Assert.AreEqual(0L, GC.GetAllocatedBytesForCurrentThread() - before, "the search walks the modules, not an enumerator over the grid");
    }

    static void DrainShields(Ship ship)
    {
        foreach (ShipModule m in ship.GetShields())
            m.DamageShield(m.ShieldPower, null, out _);
    }

    static float TotalHealth(Ship ship) => ship.Modules.Sum(m => m.Health);

    // no chain, so only this blast decides what is hit
    static void QuietlyKillOtherExploders(Ship ship, ShipModule reactor)
    {
        Assert.IsTrue(reactor.Explodes, "setup: the module at (1,2) must be an exploding reactor");
        foreach (ShipModule m in ship.Modules)
            if (m.Explodes && m != reactor)
                m.Die(null, cleanupOnly: true);
    }

    float[] HealthAfterReactorBlast(Vector2 position, float rotation)
    {
        Ship ship = SpawnShip("TEST_ShipShield", Player, position);
        Ship attacker = SpawnShip("TEST_ShipShield", Enemy, position + new Vector2(1000, 0));
        ship.Rotation = rotation;
        ship.UpdateModulePositions(TestSimStep, forceUpdate: true);

        ShipModule reactor = ship.GetModuleAt(1, 2);
        QuietlyKillOtherExploders(ship, reactor);
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
        WithReactorBlast(damage: 2400, radius: 40, () =>
        {
            Ship ship = SpawnShip("TEST_ShipShield", Player, new Vector2(0, 0));
            Ship attacker = SpawnShip("TEST_ShipShield", Enemy, new Vector2(1000, 0));
            DrainShields(ship);
            ShipModule reactor = ship.GetModuleAt(1, 2);
            QuietlyKillOtherExploders(ship, reactor);
            ShipModule[] northwest = { ship.GetModuleAt(1, 1), ship.GetModuleAt(0, 2), ship.GetModuleAt(1, 0) };
            float northwestBefore = northwest.Sum(m => m.Health);
            float reactorHealth = reactor.Health;
            float before = TotalHealth(ship);
            reactor.Damage(attacker, reactorHealth + 10f, out _);
            float dealt = before - reactorHealth - TotalHealth(ship);
            AssertGreaterThan(dealt, 0f, "setup: the reactor's blast must reach other modules");
            Assert.IsTrue(dealt <= 2400.01f, $"a 2400 blast from a module must deal no more than 2400, dealt {dealt}");
            float northwestDealt = northwestBefore - northwest.Sum(m => m.Health);
            Assert.IsTrue(northwestDealt <= 600.01f, $"the northwest corner must take no more than its quarter of 600, took {northwestDealt}");

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

    [TestMethod]
    public void AShieldBubbleTakesNoMoreOfABlastOnceItsShieldIsDown()
    {
        // a small bubble, so a blast can center outside it and still reach cells inside it
        ShipModuleFlyweight shieldTemplate = ResourceManager.GetModuleTemplate("TEST_ModuleShield").Flyweight;
        object oldRadius = ShieldRadius.GetValue(shieldTemplate);
        float destroyThreshold = GlobalStats.Defaults.ShipDestroyThreshold;
        try
        {
            ShieldRadius.SetValue(shieldTemplate, 20f);
            GlobalStats.Defaults.ShipDestroyThreshold = 0f;
            Ship ship = SpawnShip("TEST_ShipShield", Player, new Vector2(0, 0));
            Ship attacker = SpawnShip("TEST_ShipShield", Enemy, new Vector2(1000, 0));
            ship.UpdateModulePositions(TestSimStep, forceUpdate: true);

            ShipModule shield = ship.GetModuleAt(0, 2);
            ShipModule farShield = ship.GetModuleAt(3, 2);
            ShipModule amplifier = ship.GetModuleAt(2, 0);
            farShield.DamageShield(farShield.ShieldPower, null, out _);
            shield.DamageShield(shield.ShieldPower - 50f, null, out _);
            Assert.IsTrue(shield.ShieldsAreActive, "setup: the shield at (0,2) must be up");
            Assert.IsFalse(shield.HitTestShield(new Point(2, 0), 8f), "setup: its bubble must not cover the cell the blast centers on");
            Assert.IsTrue(shield.HitTestShield(new Point(1, 0), 8f) && shield.HitTestShield(new Point(2, 1), 8f),
                          "setup: its bubble must cover cells in two corners of the blast");
            float health = shield.Health;

            // centered on the amplifier at (2,0), reaching columns 1 to 3 only, so the generator at (0,2) is out of reach
            ship.DamageExplosive(attacker, 1000f, amplifier.Position, 20f, ignoreShields: false);

            Assert.IsFalse(shield.ShieldsAreActive, "setup: the blast must take the shield down through its bubble");
            AssertEqual(0.01f, health, shield.Health, "a generator out of the blast's reach must not take it on its health through its bubble");
        }
        finally
        {
            ShieldRadius.SetValue(shieldTemplate, oldRadius);
            GlobalStats.Defaults.ShipDestroyThreshold = destroyThreshold;
        }
    }
}
