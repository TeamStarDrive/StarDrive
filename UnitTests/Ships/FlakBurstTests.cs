using System;
using System.Linq;
using System.Reflection;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using SDUtils;
using Ship_Game;
using Ship_Game.Gameplay;
using Ship_Game.Ships;
using Ship_Game.Universe;
using UnitTests.Serialization;
using Vector2 = SDGraphics.Vector2;
#pragma warning disable CA2213

namespace UnitTests.Ships;

[TestClass]
public class FlakBurstTests : StarDriveTest
{
    const string ShellUid = "ZzFlakShell";
    const string FragmentUid = "ZzFlakFragment";
    const string BoltUid = "ZzInterceptableBolt";
    const string MisspeltShellUid = "ZzMisspeltFlakShell";
    const float Separation = 300f;
    const int Fragments = 5;

    static readonly Vector2 Gun = new(200_000, 200_000);

    public FlakBurstTests()
    {
        LoadStarterShips("Rocket Scout", "Vulcan Scout");
        CreateUniverseAndPlayerEmpire();

        WeaponTemplate shell = CloneWeapon("FlakAA Cannon", ShellUid);
        shell.MirvWeapon = FragmentUid;
        shell.MirvSeparationDistance = Separation;

        WeaponTemplate fragment = CloneWeapon("FlakAA Cannon", FragmentUid);
        fragment.ProjectileCount = Fragments;
        fragment.FireDispersionArc = 40;
        fragment.ProjectileRadius = 30;
        fragment.ProjectileSpeed = 200;
        fragment.BaseRange = 300;

        WeaponTemplate bolt = CloneWeapon("MassDriver", BoltUid);
        bolt.Tag_Intercept = true;
        bolt.HitPoints = 10;
        bolt.ProjectileSpeed = 800;

        WeaponTemplate misspelt = CloneWeapon("FlakAA Cannon", MisspeltShellUid);
        misspelt.MirvWeapon = "ZzNoSuchWarhead";
    }

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

    public override void Cleanup()
    {
        Weapons.Remove(ShellUid);
        Weapons.Remove(FragmentUid);
        Weapons.Remove(BoltUid);
        Weapons.Remove(MisspeltShellUid);
        base.Cleanup();
    }

    Projectile FireShell(Ship defender, Vector2 direction, GameObject target)
    {
        Weapon gun = ResourceManager.CreateWeapon(UState, ShellUid, defender, null);
        return Projectile.Create(gun, defender, Gun, direction, target, playSound: false, inheritedVelocity: Vector2.Zero);
    }

    static Projectile[] FragmentsOf(Projectile[] projectiles)
        => projectiles.Filter(p => p.Weapon.UID == FragmentUid);

    [TestMethod]
    public void AFlakShellSplitsShortOfTheBoltItWasFiredAtAndItsFragmentsShootItDown()
    {
        Ship defender = SpawnShip("Vulcan Scout", Player, Gun);
        Ship attacker = SpawnShip("Vulcan Scout", Enemy, Gun + new Vector2(0, -50_000));
        Weapon boltGun = ResourceManager.CreateWeapon(UState, BoltUid, attacker, null);
        Projectile bolt = Projectile.Create(boltGun, attacker, Gun + new Vector2(0, -3000), Vectors.Down, null,
                                            playSound: false, inheritedVelocity: Vector2.Zero);
        Projectile shell = FireShell(defender, Vectors.Up, bolt);
        Assert.IsTrue(shell.HasMirvFuse, "setup: an unguided shell that names a MIRV warhead must get a fuse");

        float separationAtBurst = 0f;
        RunSimWhile((2, fatal: true), () => shell.Active, body: () =>
        {
            Assert.IsTrue(bolt.Active, "the bolt must not be hit before the shell splits");
            separationAtBurst = shell.Position.Distance(bolt.Position);
        });

        AssertGreaterThan(separationAtBurst, Separation - 5f, "the shell must split before it closes inside its separation distance");
        AssertLessThan(separationAtBurst, Separation + 60f, "the shell must split no more than a frame before its separation distance");
        AssertEqual(Fragments, FragmentsOf(GetProjectiles(defender)).Length, "the shell must split into every warhead");

        RunSimWhile((1, fatal: true), () => bolt.Active);
        AssertLessThanOrEqual(bolt.Health, 0f, "the fragments must shoot the bolt down");
    }

    [TestMethod]
    public void AFlakShellWithNoTargetSplitsAtTheEndOfItsFlight()
    {
        Ship defender = SpawnShip("Vulcan Scout", Player, Gun);
        Projectile shell = FireShell(defender, Vectors.Up, null);

        Vector2 lastPosition = shell.Position;
        RunSimWhile((5, fatal: true), () => shell.Active, body: () => lastPosition = shell.Position);

        Projectile[] fragments = FragmentsOf(GetProjectiles(defender));
        AssertEqual(Fragments, fragments.Length, "a shell with no target must split when its flight ends");
        float flight = shell.Weapon.ProjectileSpeed * (shell.Weapon.BaseRange / shell.Weapon.ProjectileSpeed * 1.2f);
        AssertEqual(40f, flight, Gun.Distance(lastPosition), "it must split at the end of its flight, not before");
    }

    [TestMethod]
    public void AShellNamingAMissingWarheadFliesOnWithoutSplitting()
    {
        Ship defender = SpawnShip("Vulcan Scout", Player, Gun);
        Weapon gun = ResourceManager.CreateWeapon(UState, MisspeltShellUid, defender, null);
        Projectile shell = Projectile.Create(gun, defender, Gun, Vectors.Up, null, playSound: false, inheritedVelocity: Vector2.Zero);

        RunSimWhile((5, fatal: true), () => shell.Active);

        AssertEqual(0, GetProjectileCount(defender), "a shell whose warhead does not exist must expire like a plain shell");
    }

    [TestMethod]
    public void AFlakShellFiredFromAPlanetSplitsIntoThePlanetsWarheads()
    {
        Planet planet = AddDummyPlanet(new Vector2(300_000, 300_000));
        Weapon gun = ResourceManager.CreateWeapon(UState, ShellUid, null, null);
        gun.PlanetOrigin = planet.Position;
        Projectile shell = Projectile.Create(gun, planet, Player, Vectors.Up, null);

        RunSimWhile((12, fatal: true), () => shell.Active);

        Projectile[] fragments = FragmentsOf(GetProjectiles(null));
        AssertEqual(Fragments, fragments.Length, "a planet's shell must split into every warhead");
        foreach (Projectile p in fragments)
        {
            Assert.AreEqual(planet, p.Planet, "a planet's warheads belong to the planet");
            Assert.AreEqual(Player, p.Loyalty, "a planet's warheads keep its owner");
        }
    }

    [TestMethod]
    public void AFlakShellKeepsItsFuseThroughASave()
    {
        Ship defender = SpawnShip("Vulcan Scout", Player, Gun);
        Ship target = SpawnShip("Vulcan Scout", Enemy, Gun + new Vector2(0, -3000));
        Projectile shell = FireShell(defender, Vectors.Up, target);
        RunObjectsSim(TestSimStep);
        float fuse = shell.MirvFuse;
        AssertGreaterThan(fuse, 0.5f, "setup: a shell fired at a target must split before the end of its flight");

        UniverseState loaded = BinarySerializerTests.SerDes(UState);
        Projectile loadedShell = loaded.Objects.GetAllProjectilesAndBeams().FirstOrDefault(p => p.Id == shell.Id);

        Assert.IsNotNull(loadedShell, "setup: the shell must be in the save");
        Assert.IsTrue(loadedShell.HasMirvFuse, "a loaded shell must still split");
        AssertEqual(0.001f, fuse, loadedShell.MirvFuse, "a loaded shell must split where it would have");
    }

    [TestMethod]
    public void AMirvWeaponsDamagePerSecondCountsTheDamageOfItsWarheads()
    {
        var shell = (WeaponTemplate)ResourceManager.GetWeaponTemplate(ShellUid);
        var fragment = (WeaponTemplate)ResourceManager.GetWeaponTemplate(FragmentUid);
        shell.DamageAmount = 10f;
        fragment.DamageAmount = 40f;

        shell.InitializeTemplate();

        float expected = Math.Max(1, shell.SalvoCount) / shell.NetFireDelay * shell.ProjectileCount * 40f * Fragments;
        AssertEqual(0.01f, expected, shell.DamagePerSecond, "a MIRV weapon's damage per second is what its warheads deal");
    }

    [TestMethod]
    public void AClusterMissileStillSplitsIntoItsUnguidedWarheads()
    {
        Ship launcher = SpawnShip("Rocket Scout", Player, Gun);
        Ship target = SpawnShip("Vulcan Scout", Enemy, Gun + new Vector2(0, -3000));
        Weapon rack = ResourceManager.CreateWeapon(UState, "ClusterMissiles", launcher, null);
        Projectile missile = Projectile.Create(rack, launcher, Gun, Vectors.Up, target.Modules[0], playSound: false);
        int warheads = ResourceManager.GetWeaponTemplate("ClusterMirv").ProjectileCount;

        missile.CreateMirv(target.Modules[0]);

        Projectile[] spawned = GetProjectiles(launcher).Filter(p => p.Weapon.UID == "ClusterMirv");
        AssertEqual(warheads, spawned.Length, "a cluster missile must split into every warhead");
        foreach (Projectile p in spawned)
            Assert.AreEqual(Player, p.Loyalty, "the warheads keep the missile's owner");
        Assert.IsFalse(missile.Active, "the missile is gone once it splits");
    }
}
