using System;
using System.Linq;
using System.Reflection;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using SDGraphics;
using SDUtils;
using Ship_Game;
using Ship_Game.Gameplay;
using Ship_Game.Ships;
using Ship_Game.Universe;
using UnitTests.Serialization;
using Vector2 = SDGraphics.Vector2;
#pragma warning disable CA2213

namespace UnitTests.Ships;

/// <summary>
/// A shell that costs ordnance weighs 5 per ordnance point and, as it hits a ship, changes the ship's velocity by
/// its momentum relative to the ship over the ship's mass; a missile, rocket or torpedo (guided, or tagged missile or
/// torpedo) by half that, while a cannon shell pushes fully though it explodes. A MIRV warhead counts its share of the
/// ordnance its shot cost. Plasma and drones push nothing, nor does a hit on a raised shield, even one that breaks it,
/// unless the shell penetrates shields, or one that kills the ship, whose wreck keeps the motion it had. A shell that
/// armor deflects pushes along the path it struck on. A push never takes a ship past the greater of its top speed and
/// its speed now, and stations and ships at warp, launching, landing or crashing onto a planet are not moved.
/// </summary>
[TestClass]
public class ProjectilePushTests : StarDriveTest
{
    public ProjectilePushTests()
    {
        LoadStarterShips("Dreadnought mk1-a", "Corsair", "Vulcan Scout", "Platform Base mk1-a");
        CreateUniverseAndPlayerEmpire();
    }

    static Vector2 Site(int i) => new(200_000f + i * 20_000f, 200_000f);

    static void DrainShields(Ship ship)
    {
        foreach (ShipModule m in ship.GetShields())
            m.DamageShield(m.ShieldPower, null, out float _);
    }

    Ship SpawnTarget(string shipName, int site)
    {
        Ship target = SpawnShip(shipName, Player, Site(site));
        DrainShields(target);
        target.Velocity = Vector2.Zero;
        return target;
    }

    Weapon CreateWeapon(string weaponUid)
    {
        Ship attacker = SpawnShip("Corsair", Enemy, new Vector2(-200_000f));
        return ResourceManager.CreateWeapon(UState, weaponUid, attacker, null);
    }

    // a weapon made from a changed copy of the template
    Weapon CreateWeaponFromCopy(string weaponUid, Action<WeaponTemplate> change)
    {
        Ship attacker = SpawnShip("Corsair", Enemy, new Vector2(-200_000f));
        Assert.IsTrue(ResourceManager.GetWeaponTemplate(weaponUid, out IWeaponTemplate template), $"setup: {weaponUid} exists");
        var copy = (WeaponTemplate)typeof(object).GetMethod("MemberwiseClone", BindingFlags.NonPublic | BindingFlags.Instance)!
                                                 .Invoke(template, null);
        change(copy);
        return new Weapon(UState, copy, attacker, null);
    }

    // a shell flying at the given velocity strikes the target's first module, or the one given, where given
    static void Hit(Weapon w, Ship target, Vector2 shellVelocity, ShipModule module = null, float damage = 1f, Vector2? at = null,
                    bool penetratesShields = false)
    {
        Projectile shell = Projectile.Create(w, w.Owner, target.Position + new Vector2(0, -target.Radius - 500f),
                                             new Vector2(0, 1), target, playSound: false);
        shell.Velocity = shellVelocity;
        shell.DamageAmount = damage;
        shell.IgnoresShields = penetratesShields;
        module ??= target.Modules[0];
        Assert.IsTrue(shell.Touch(module, at ?? module.Position), "setup: the shell hits the ship");
    }

    static readonly FieldInfo HitRollSeconds = typeof(Ship).GetField("HitRollSeconds", BindingFlags.Instance | BindingFlags.NonPublic);

    [TestMethod]
    public void AShellPushesTheShipByFiveTimesItsOrdnanceCostTimesItsVelocityOverTheShipsMass()
    {
        Weapon massDriver = CreateWeapon("MassDriver");
        AssertEqual(13f, massDriver.OrdinanceRequiredToFire, "setup: a mass driver shot costs 13 ordnance");
        Assert.IsTrue(massDriver.Explodes, "setup: a mass driver shell explodes on the hit");
        Ship target = SpawnTarget("Dreadnought mk1-a", 0);

        Hit(massDriver, target, new Vector2(900f, 1200f));

        Vector2 expected = 13f * 5f * new Vector2(900f, 1200f) / target.Mass;
        AssertLessThan(expected.Length(), target.MaxSTLSpeed, "setup: the push stays under the ship's top speed");
        AssertEqual(0.0001f, expected, target.Velocity,
                    "the ship moves along the shell's path, by its full momentum over the ship's mass, though the shell explodes");
    }

    [TestMethod]
    public void ThePushCountsTheShellsVelocityRelativeToTheShip()
    {
        Weapon massDriver = CreateWeapon("MassDriver");
        Ship fleeing = SpawnTarget("Dreadnought mk1-a", 0);
        Ship charging = SpawnTarget("Dreadnought mk1-a", 1);
        float speed = fleeing.MaxSTLSpeed * 0.25f;
        fleeing.Velocity = new Vector2(0f, speed);
        charging.Velocity = new Vector2(0f, -speed);

        Hit(massDriver, fleeing, new Vector2(0f, 1600f));
        Hit(massDriver, charging, new Vector2(0f, 1600f));

        AssertEqual(0.0001f, speed + 13f * 5f * (1600f - speed) / fleeing.Mass, fleeing.Velocity.Y,
                    "a ship flying away from the shell is pushed by the speed it closes at");
        AssertEqual(0.0001f, -speed + 13f * 5f * (1600f + speed) / charging.Mass, charging.Velocity.Y,
                    "and one flying into it by its own speed added");
        AssertEqual(0f, fleeing.Velocity.X, "and only along the shell's path");
    }

    [TestMethod]
    public void APushNeverTakesAShipPastTheGreaterOfItsTopSpeedAndItsSpeedNow()
    {
        Weapon massDriver = CreateWeapon("MassDriver");
        Ship atTopSpeed = SpawnTarget("Vulcan Scout", 0);
        Ship overTopSpeed = SpawnTarget("Vulcan Scout", 1);
        float top = atTopSpeed.MaxSTLSpeed;
        float push = 13f * 5f * 1600f / atTopSpeed.Mass;
        AssertGreaterThan(push, top, "setup: a mass driver shell alone would throw a scout past its top speed");
        atTopSpeed.Velocity = new Vector2(top, 0f);
        overTopSpeed.Velocity = new Vector2(top * 2f, 0f);

        Hit(massDriver, atTopSpeed, new Vector2(0f, 1600f));
        Hit(massDriver, overTopSpeed, new Vector2(0f, 1600f));

        Vector2 pushedAtTop = new Vector2(top, 0f) + 13f * 5f * new Vector2(-top, 1600f) / atTopSpeed.Mass;
        Vector2 pushedOverTop = new Vector2(top * 2f, 0f) + 13f * 5f * new Vector2(-top * 2f, 1600f) / overTopSpeed.Mass;
        AssertGreaterThan(pushedAtTop.Length(), top, "setup: uncapped, the push would take the first past its top speed");
        AssertGreaterThan(pushedOverTop.Length(), top * 2f, "setup: and the second past its speed now");
        AssertEqual(0.01f, top, atTopSpeed.Velocity.Length(), "a ship at its top speed stays at it");
        AssertEqual(0.0001f, pushedAtTop.Normalized(), atTopSpeed.Velocity.Normalized(), "with its course bent by the push");
        AssertEqual(0.01f, top * 2f, overTopSpeed.Velocity.Length(), "a ship over its top speed is neither slowed nor sped up");
        AssertEqual(0.0001f, pushedOverTop.Normalized(), overTopSpeed.Velocity.Normalized(), "and is bent too");
    }

    [TestMethod]
    public void AHitOnARaisedShieldNeitherPushesNorRollsTheShipEvenWhenItBreaksTheShield()
    {
        Weapon massDriver = CreateWeapon("MassDriver");
        Ship target = SpawnShip("Dreadnought mk1-a", Player, Site(0));
        target.Velocity = Vector2.Zero;
        ShipModule shield = target.GetShields().First();
        Assert.IsTrue(shield.ShieldsAreActive, "setup: the shield is up");
        Vector2 offCenter = target.Position + new Vector2(target.Radius * 0.5f, 0f);

        Hit(massDriver, target, new Vector2(0f, 1600f), shield, damage: shield.ShieldPower + 500f, at: offCenter);

        Assert.IsFalse(shield.ShieldsAreActive, "setup: the shell breaks the shield");
        AssertEqual(Vector2.Zero, target.Velocity, "a hit on a raised shield pushes nothing");
        AssertEqual(0f, (float)HitRollSeconds.GetValue(target), "nor rolls the ship");

        DrainShields(target);
        Hit(massDriver, target, new Vector2(0f, 1600f), target.Modules[0], at: offCenter);
        AssertEqual(0.0001f, 13f * 5f * 1600f / target.Mass, target.Velocity.Y, "while a shell that meets no raised shield pushes");
        AssertGreaterThan((float)HitRollSeconds.GetValue(target), 0f, "and rolls it");
    }

    [TestMethod]
    public void AShellThatPenetratesShieldsPushesThoughItLandsOnARaisedOne()
    {
        Weapon massDriver = CreateWeapon("MassDriver");
        Ship target = SpawnShip("Dreadnought mk1-a", Player, Site(0));
        target.Velocity = Vector2.Zero;
        ShipModule shield = target.GetShields().First();
        Assert.IsTrue(shield.ShieldsAreActive, "setup: the shield is up");

        Hit(massDriver, target, new Vector2(0f, 1600f), shield, penetratesShields: true);

        AssertEqual(0.0001f, 13f * 5f * 1600f / target.Mass, target.Velocity.Y, "a shell that rolled its shield penetration pushes through the bubble");
    }

    [TestMethod]
    public void ADeflectedShellPushesAlongThePathItStruckOn()
    {
        Weapon railgun = CreateWeapon("Railgun");
        Assert.IsFalse(railgun.Explodes, "setup: a railgun shell does not explode, so armor can deflect it");
        Ship target = SpawnTarget("Dreadnought mk1-a", 0);
        ShipModule armor = target.Modules.First(m => m.Is(ShipModuleType.Armor));
        FieldInfo deflection = typeof(ShipModuleFlyweight).GetField(nameof(ShipModuleFlyweight.Deflection));
        float saved = armor.Deflection;
        target.InFrustum = true;
        UState.ViewState = UniverseScreen.UnivScreenState.ShipView; // a shell is deflected only on screen, at ship view or closer
        try
        {
            deflection.SetValue(armor.Flyweight, 100f);
            Projectile shell = Projectile.Create(railgun, railgun.Owner, target.Position + new Vector2(0, -target.Radius - 500f),
                                                 new Vector2(0, 1), target, playSound: false);
            shell.Velocity = new Vector2(0f, 1600f);
            shell.DamageAmount = 1f;
            shell.ArmorPiercing = 0; // so it strikes the armor rather than passing through it
            AssertEqual(100f, armor.Deflection, "setup: the armor deflects weak shots");
            Assert.IsTrue(target.InFrustum && UState.IsShipViewOrCloser, "setup: on screen");

            Assert.IsTrue(shell.Touch(armor, armor.Position), "setup: the shell hits the ship");

            Assert.AreEqual(target.Loyalty, shell.Loyalty, "setup: the armor deflects the shell, which turns away");
            AssertEqual(0.0001f, new Vector2(0f, 2.8f * 5f * 1600f / target.Mass), target.Velocity,
                        "the ship is pushed along the path the shell struck on, not the one it turned to");
        }
        finally
        {
            deflection.SetValue(armor.Flyweight, saved);
        }
    }

    [TestMethod]
    public void AHitThatKillsTheShipPushesNothingSoItsWreckKeepsTheMotionItHad()
    {
        Weapon massDriver = CreateWeapon("MassDriver");
        Ship target = SpawnTarget("Vulcan Scout", 0);
        var motion = new Vector2(target.MaxSTLSpeed * 0.25f, 0f);
        target.Velocity = motion;

        Hit(massDriver, target, new Vector2(0f, 1600f), damage: 10_000_000f);

        Assert.IsFalse(target.Active, "setup: the hit kills the ship");
        AssertEqual(motion, target.Velocity, "a killing hit pushes nothing, so the wreck keeps the ship's motion");
    }

    [TestMethod]
    public void AMissileRocketOrTorpedoPushesWithHalfItsMomentum()
    {
        Weapon missile = CreateWeapon("Missile");
        AssertEqual(10f, missile.OrdinanceRequiredToFire, "setup: a missile costs 10 ordnance");
        Weapon lrm = CreateWeapon("LRM1");
        Assert.IsTrue(lrm.Tag_Guided && !lrm.Tag_Missile && !lrm.Tag_Torpedo, "setup: an LRM is guided, with no missile or torpedo tag");
        AssertEqual(11f, lrm.OrdinanceRequiredToFire, "setup: an LRM costs 11 ordnance");
        Weapon rocket = CreateWeaponFromCopy("Railgun", t => t.Tag_Missile = true);
        Weapon torpedo = CreateWeaponFromCopy("Railgun", t => t.Tag_Torpedo = true);
        Weapon railgun = CreateWeapon("Railgun");
        Assert.IsTrue(!railgun.Tag_Guided && !railgun.Tag_Missile && !railgun.Tag_Torpedo, "setup: a railgun shell is none of them");
        AssertEqual(2.8f, railgun.OrdinanceRequiredToFire, "setup: a railgun shot costs 2.8 ordnance");
        Ship[] targets = { SpawnTarget("Dreadnought mk1-a", 0), SpawnTarget("Dreadnought mk1-a", 1),
                           SpawnTarget("Dreadnought mk1-a", 2), SpawnTarget("Dreadnought mk1-a", 3), SpawnTarget("Dreadnought mk1-a", 4) };
        float mass = targets[0].Mass;

        Hit(missile, targets[0], new Vector2(0f, 1100f));
        Hit(lrm, targets[1], new Vector2(0f, 1100f));
        Hit(rocket, targets[2], new Vector2(0f, 1100f));
        Hit(torpedo, targets[3], new Vector2(0f, 1100f));
        Hit(railgun, targets[4], new Vector2(0f, 1100f));

        AssertEqual(0.0001f, 10f * 5f * 1100f * 0.5f / mass, targets[0].Velocity.Y, "a missile pushes with half its momentum");
        AssertEqual(0.0001f, 11f * 5f * 1100f * 0.5f / mass, targets[1].Velocity.Y, "so does any guided shell");
        AssertEqual(0.0001f, 2.8f * 5f * 1100f * 0.5f / mass, targets[2].Velocity.Y, "and any shell tagged missile, guided or not");
        AssertEqual(0.0001f, 2.8f * 5f * 1100f * 0.5f / mass, targets[3].Velocity.Y, "or torpedo");
        AssertEqual(0.0001f, 2.8f * 5f * 1100f / mass, targets[4].Velocity.Y, "while the same shell untagged pushes fully");
    }

    // a cluster missile split into its warheads far from any ship
    Projectile[] SplitClusterMissile(out Ship attacker)
    {
        Weapon cluster = CreateWeapon("ClusterMissiles");
        attacker = cluster.Owner;
        AssertEqual(1f, cluster.OrdinanceRequiredToFire, "setup: a cluster missile costs 1 ordnance");
        Assert.AreEqual("ClusterMirv", cluster.MirvWeapon, "setup: it splits into cluster warheads");
        Projectile shot = Projectile.Create(cluster, attacker, attacker.Position, new Vector2(0, 1), null, playSound: false);
        shot.CreateMirv(null);
        Projectile[] warheads = UState.Objects.GetProjectiles(attacker).Filter(p => p.Weapon.UID == "ClusterMirv");
        AssertEqual(45, warheads.Length, "setup: into 45 warheads");
        return warheads;
    }

    [TestMethod]
    public void EachMirvWarheadPushesWithItsShareOfTheOrdnanceItsShotCost()
    {
        Projectile warhead = SplitClusterMissile(out _)[0];
        AssertGreaterThan(warhead.Weapon.OrdinanceRequiredToFire, 0.5f, "setup: a warhead's own template costs far more than its share");
        Assert.IsFalse(warhead.Explodes, "setup: a cluster warhead does not explode");
        Ship target = SpawnTarget("Vulcan Scout", 0);
        warhead.Velocity = new Vector2(0f, 1200f);
        warhead.DamageAmount = 1f;
        ShipModule module = target.Modules[0];

        Assert.IsTrue(warhead.Touch(module, module.Position), "setup: the warhead hits the ship");

        AssertEqual(0.0001f, 1f / 45f * 5f * 1200f / target.Mass, target.Velocity.Y,
                    "a warhead pushes with a 45th of the shot's 1 ordnance, not its own template's cost");
    }

    [TestMethod]
    public void AWarheadsShareSurvivesASaveAndOtherShellsLoadWithNone()
    {
        Projectile[] warheads = SplitClusterMissile(out Ship attacker);
        Projectile emptyWarhead = warheads[1];
        typeof(Projectile).GetProperty(nameof(Projectile.WarheadOrdnance))!.SetValue(emptyWarhead, 0f);
        Projectile shell = Projectile.Create(CreateWeapon("MassDriver"), attacker, attacker.Position, new Vector2(0, 1), null, playSound: false);
        AssertEqual(-1f, shell.WarheadOrdnance, "setup: a shell that is no warhead has no share");
        RunObjectsSim(TestSimStep);

        UniverseState loaded = BinarySerializerTests.SerDes(UState);
        Projectile[] all = loaded.Objects.GetAllProjectilesAndBeams();

        AssertEqual(0.0001f, 1f / 45f, all.First(p => p.Id == warheads[0].Id).WarheadOrdnance, "a loaded warhead keeps its share");
        AssertEqual(0f, all.First(p => p.Id == emptyWarhead.Id).WarheadOrdnance, "a share of 0 stays 0");
        AssertEqual(-1f, all.First(p => p.Id == shell.Id).WarheadOrdnance, "a shell that is no warhead loads with none");
    }

    [TestMethod]
    public void ARepairDroneThatFliesIntoAShipPushesNothing()
    {
        Weapon drone = CreateWeapon("RepairDrone");
        Assert.IsTrue(drone.IsRepairDrone, "setup: a repair drone");
        AssertGreaterThan(drone.OrdinanceRequiredToFire, 0f, "setup: that costs ordnance");
        Ship target = SpawnTarget("Vulcan Scout", 0);

        Hit(drone, target, new Vector2(0f, 650f));

        AssertEqual(Vector2.Zero, target.Velocity, "a drone pushes nothing");
    }

    [TestMethod]
    public void PlasmaAndShotsThatCostNoOrdnancePushNothing()
    {
        Weapon plasma = CreateWeapon("PlasmaThrower");
        Assert.IsTrue(plasma.Tag_Plasma, "setup: a plasma thrower is plasma");
        AssertGreaterThan(plasma.OrdinanceRequiredToFire, 0f, "setup: that costs ordnance");
        Ship plasmaTarget = SpawnTarget("Vulcan Scout", 0);
        Hit(plasma, plasmaTarget, new Vector2(0f, 1700f));
        AssertEqual(Vector2.Zero, plasmaTarget.Velocity, "plasma pushes nothing");

        Weapon notPlasma = CreateWeaponFromCopy("PlasmaThrower", t => t.Tag_Plasma = false);
        Ship control = SpawnTarget("Vulcan Scout", 1);
        Hit(notPlasma, control, new Vector2(0f, 1700f));
        AssertEqual(0.0001f, notPlasma.OrdinanceRequiredToFire * 5f * 1700f / control.Mass, control.Velocity.Y,
                    "the same shot without its plasma tag pushes by its ordnance cost");

        Weapon laser = CreateWeapon("2x2LaserTurret");
        AssertEqual(0f, laser.OrdinanceRequiredToFire, "setup: a laser bolt costs no ordnance");
        Ship laserTarget = SpawnTarget("Vulcan Scout", 2);
        Hit(laser, laserTarget, new Vector2(0f, 1700f));
        AssertEqual(Vector2.Zero, laserTarget.Velocity, "a shot that costs no ordnance has no mass to push with");
    }

    [TestMethod]
    public void StationsAndShipsAtWarpLaunchingLandingOrCrashingAreNotMovedButADyingShipIs()
    {
        var impulse = new Vector2(0f, 100_000f);
        Planet planet = AddDummyPlanet(new Vector2(400_000f));

        Ship control = SpawnTarget("Dreadnought mk1-a", 0);
        control.ApplyImpulse(impulse);
        AssertGreaterThan(control.Velocity.Y, 0f, "setup: the push moves a ship in play");

        Ship station = SpawnTarget("Platform Base mk1-a", 1);
        Assert.IsTrue(station.Stats.IsStationary, "setup: a platform is stationary");
        station.MaxSTLSpeed = control.MaxSTLSpeed; // so that only the station rule holds it
        station.ApplyImpulse(impulse);
        AssertEqual(Vector2.Zero, station.Velocity, "a station is not moved");

        Ship warping = SpawnTarget("Dreadnought mk1-a", 2);
        warping.engineState = Ship.MoveState.Warp;
        warping.ApplyImpulse(impulse);
        AssertEqual(Vector2.Zero, warping.Velocity, "nor a ship at warp");

        Ship launching = SpawnTarget("Dreadnought mk1-a", 3);
        launching.InitLaunch(LaunchPlan.Planet, 0f);
        Assert.IsTrue(launching.IsLaunching, "setup: the ship is launching");
        launching.Velocity = Vector2.Zero;
        launching.ApplyImpulse(impulse);
        AssertEqual(Vector2.Zero, launching.Velocity, "nor a launching ship");

        Ship landing = SpawnTarget("Dreadnought mk1-a", 4);
        landing.InitLanding(LandPlan.Scrap, planet);
        Assert.IsTrue(landing.IsLanding, "setup: the ship is landing");
        landing.Velocity = Vector2.Zero;
        landing.ApplyImpulse(impulse);
        AssertEqual(Vector2.Zero, landing.Velocity, "nor a landing ship");

        Ship crashing = SpawnTarget("Dreadnought mk1-a", 5);
        crashing.PlanetCrash = new PlanetCrash(planet, crashing);
        crashing.Dying = true;
        crashing.ApplyImpulse(impulse);
        AssertEqual(Vector2.Zero, crashing.Velocity, "nor one crashing onto a planet");

        Ship dying = SpawnTarget("Dreadnought mk1-a", 6);
        dying.Dying = true;
        dying.SetDieTimer(10f);
        dying.ApplyImpulse(impulse);
        AssertEqual(0.0001f, control.Velocity, dying.Velocity, "a dying ship is moved like one in play");
    }
}
