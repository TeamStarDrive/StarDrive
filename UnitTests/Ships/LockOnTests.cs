using System;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using SDUtils;
using Ship_Game;
using Ship_Game.Gameplay;
using Ship_Game.Ships;
using Ship_Game.Spatial;
using Ship_Game.Universe;
using UnitTests.Serialization;
using Vector2 = SDGraphics.Vector2;
#pragma warning disable CA2213

namespace UnitTests.Ships;

[TestClass]
public class LockOnTests : StarDriveTest
{
    static readonly Vector2 Gun = new(300_000, 0);

    public LockOnTests()
    {
        LoadStarterShips("Rocket Scout", "Vulcan Scout");
        CreateUniverseAndPlayerEmpire();
    }

    static Weapon GuidedWeapon(Ship ship)
    {
        foreach (Weapon w in ship.Weapons)
            if (w.Tag_Guided)
                return w;
        return null;
    }

    Projectile Missile(Ship launcher, Ship target)
    {
        Weapon rocket = GuidedWeapon(launcher);
        Assert.IsNotNull(rocket, "setup: Rocket Scout must carry a guided weapon");
        return Projectile.Create(rocket, launcher, launcher.Position, Vectors.Up, target.Modules[0], playSound: false);
    }

    static Projectile Shell(Ship gunship, Vector2 at, Ship target)
    {
        Projectile shell = Projectile.Create(gunship.Weapons[0], gunship, at, Vectors.Up, target.Modules[0], playSound: false);
        Assert.IsNull(shell.MissileAI, "setup: Vulcan Scout must fire unguided shells");
        return shell;
    }

    unsafe int Collide(Projectile shot, SpatialObjectBase other)
    {
        var objects = new SpatialObjectBase[] { shot, other };
        CollisionPair* pairs = stackalloc CollisionPair[1];
        pairs[0] = new CollisionPair(0, 1);
        return NarrowPhase.Collide(TestSimStep, pairs, 1, objects);
    }

    // rolls the shot's lock again and again, counting how often it locks on to the target
    static int LocksOn(Projectile shot, Ship target, int rolls)
    {
        int locks = 0;
        for (int i = 0; i < rolls; ++i)
        {
            shot.RollLockOn(target.Modules[0]);
            if (shot.LockedOn == null)
                continue;
            Assert.AreEqual(target, shot.LockedOn, "a shot locks on only to the ship it is fired at");
            ++locks;
        }
        return locks;
    }

    [TestMethod]
    public void TheLockOnChanceFollowsTheTargetsSizeAndTheMissilesTurnRate()
    {
        AssertEqual(0.001f, 37.5f, Projectile.LockOnChance(0.5f, guided: true, 2f, smartMissiles: false), "2 rad/s is the even turn");
        AssertEqual(0.001f, 18.75f, Projectile.LockOnChance(0.5f, guided: true, 0f, smartMissiles: false), "no turn halves it");
        AssertEqual(0.001f, 56.25f, Projectile.LockOnChance(0.5f, guided: true, 4f, smartMissiles: false), "4 rad/s adds half");
        AssertEqual(0.001f, 56.25f, Projectile.LockOnChance(0.5f, guided: true, 6f, smartMissiles: false), "a turn past 4 rad/s counts as 4");
        AssertEqual(0.001f, 45f, Projectile.LockOnChance(0.5f, guided: true, 2f, smartMissiles: true), "smart missiles lock on a fifth more often");
        AssertEqual(0.001f, 95f, Projectile.LockOnChance(1f, guided: true, 4f, smartMissiles: true), "the chance stops at 95%");
    }

    [TestMethod]
    public void AnUnguidedShotsChanceIsAFifthOfTheSizeCurveWithNoTurnOrSmartBonus()
    {
        AssertEqual(0.001f, 7.5f, Projectile.LockOnChance(0.5f, guided: false, 4f, smartMissiles: true), "unguided");
    }

    [TestMethod]
    public void AHullsSizeShareIsItsRadiusOverItsRadiusPlusThreeHundred()
    {
        foreach (string design in new[] { "Rocket Scout", "Vulcan Scout" })
        {
            Ship ship = SpawnShip(design, Player, Gun);
            AssertEqual(0.0001f, ship.Radius / (ship.Radius + 300f), ship.Grid.LockOnSizeShare, design);
        }
    }

    // the lock rate over many rolls lands within a tenth of the chance
    static void AssertLockRate(Projectile shot, Ship target, int rolls, float chance, string what)
    {
        float expected = rolls * chance;
        int locks = LocksOn(shot, target, rolls);
        Assert.IsTrue(locks >= expected * 0.9f && locks <= expected * 1.1f,
                      $"{what}: {locks} locks out of {rolls}, expected about {expected:0}");
    }

    // a missile turning at 1 rad/s, so its turn rate counts for less than the even 2 rad/s
    Projectile SlowMissileAt(Ship target)
    {
        Ship launcher = SpawnShip("Rocket Scout", Enemy, Gun);
        Projectile missile = Projectile.Create(ResourceManager.CreateWeapon(UState, "Missile", launcher, null), launcher,
                                               Gun, Vectors.Up, target.Modules[0], playSound: false);
        Assert.IsNotNull(missile.MissileAI, "setup: Missile must fire guided missiles");
        AssertEqual(0.001f, 1f, missile.RotationRadsPerSecond, "setup: Missile must turn at 1 rad/s");
        return missile;
    }

    [TestMethod]
    public void AMissileLocksOnAsOftenAsItsTargetsSizeAndItsTurnRateSay()
    {
        Enemy.data.Traits.SmartMissiles = false;
        Ship target = SpawnShip("Vulcan Scout", Player, Gun + new Vector2(0, -3000));
        float share = target.Radius / (target.Radius + 300f);
        AssertLockRate(SlowMissileAt(target), target, 20_000, 0.75f * share * 0.75f, "a missile at 1 rad/s");
    }

    [TestMethod]
    public void ASmartMissileLocksOnAFifthMoreOften()
    {
        Enemy.data.Traits.SmartMissiles = true;
        Ship target = SpawnShip("Vulcan Scout", Player, Gun + new Vector2(0, -3000));
        float share = target.Radius / (target.Radius + 300f);
        AssertLockRate(SlowMissileAt(target), target, 20_000, 0.75f * share * 0.75f * 1.2f, "a smart missile at 1 rad/s");
    }

    [TestMethod]
    public void AnUnguidedShotLocksOnAsOftenAsAFifthOfItsTargetsSizeSays()
    {
        Enemy.data.Traits.SmartMissiles = true;
        Ship gunship = SpawnShip("Vulcan Scout", Enemy, Gun);
        Ship target = SpawnShip("Vulcan Scout", Player, Gun + new Vector2(0, -3000));
        float share = target.Radius / (target.Radius + 300f);
        AssertLockRate(Shell(gunship, Gun, target), target, 100_000, 0.75f * share * 0.2f, "a shell, smart missiles or not");
    }

    [TestMethod]
    public void AShotFiredFromAShipRollsItsLock()
    {
        Ship gunship = SpawnShip("Vulcan Scout", Enemy, Gun);
        Ship target = SpawnShip("Vulcan Scout", Player, Gun + new Vector2(0, -3000));
        int locks = 0;
        for (int i = 0; i < 2000; ++i)
        {
            Projectile shell = Shell(gunship, Gun, target);
            if (shell.LockedOn != null)
                ++locks;
            shell.Die(null, cleanupOnly: true);
        }
        Assert.IsTrue(locks > 0, "a shell fired at a ship rolls to lock on to it");
    }

    [TestMethod]
    public void AShotFiredFromAPlanetRollsItsLock()
    {
        Planet planet = AddDummyPlanet(Gun);
        Ship target = SpawnShip("Vulcan Scout", Enemy, Gun + new Vector2(0, -3000));
        Weapon gun = ResourceManager.CreateWeapon(UState, "MassDriver", null, null);
        gun.PlanetOrigin = planet.Position;
        int locks = 0;
        for (int i = 0; i < 2000; ++i)
        {
            Projectile shell = Projectile.Create(gun, planet, Player, Vectors.Up, target.Modules[0]);
            if (shell.LockedOn != null)
                ++locks;
            shell.Die(null, cleanupOnly: true);
        }
        Assert.IsTrue(locks > 0, "a planet's shell fired at a ship rolls to lock on to it");
    }

    [TestMethod]
    public void PointDefenseShellsAndRepairDronesNeverLockOn()
    {
        Ship gunship = SpawnShip("Vulcan Scout", Enemy, Gun);
        Ship target = SpawnShip("Vulcan Scout", Player, Gun + new Vector2(0, -3000));
        Assert.IsTrue(LocksOn(Shell(gunship, Gun, target), target, 2000) > 0, "setup: a plain shell at the same ship locks on now and then");

        Weapon flakGun = ResourceManager.CreateWeapon(UState, "FlakAA Cannon", gunship, null);
        Assert.IsTrue(flakGun.Tag_PD || flakGun.TruePD, "setup: FlakAA Cannon must be point defense");
        Projectile flak = Projectile.Create(flakGun, gunship, Gun, Vectors.Up, target.Modules[0], playSound: false);
        AssertEqual(0, LocksOn(flak, target, 2000), "a point defense shell never locks on");

        Weapon drones = ResourceManager.CreateWeapon(UState, "RepairDrone", gunship, null);
        Assert.IsTrue(drones.IsRepairDrone, "setup: RepairDrone must launch repair drones");
        Projectile drone = Projectile.Create(drones, gunship, Gun, Vectors.Up, null, playSound: false);
        AssertEqual(0, LocksOn(drone, target, 2000), "a repair drone never locks on");
    }

    [TestMethod]
    public void ALockedOnShotPassesAShipThatIsNotItsTarget()
    {
        Ship screen = SpawnShip("Vulcan Scout", Player, Gun);
        Ship target = SpawnShip("Vulcan Scout", Player, Gun + new Vector2(0, -3000));
        Ship gunship = SpawnShip("Vulcan Scout", Enemy, Gun + new Vector2(0, 3000));

        Projectile unlocked = Shell(gunship, Gun, target);
        unlocked.LockedOn = null;
        AssertEqual(1, Collide(unlocked, screen), "setup: a shell that is not locked on hits the ship in its way");

        Projectile locked = Shell(gunship, Gun, target);
        locked.LockedOn = target;
        AssertEqual(0, Collide(locked, screen), "a locked-on shell passes a ship that is not its target");
    }

    [TestMethod]
    public void ALockedOnShotHitsItsTarget()
    {
        Ship target = SpawnShip("Vulcan Scout", Player, Gun);
        Ship gunship = SpawnShip("Vulcan Scout", Enemy, Gun + new Vector2(0, 3000));
        Projectile shell = Shell(gunship, Gun, target);
        shell.LockedOn = target;
        AssertEqual(1, Collide(shell, target), "a locked-on shell hits the ship it is locked on to");
    }

    // the shell flies a step clear of every ship, then updates once more and sees it met none
    void StepClearOfShips(Projectile shell, Vector2 clear)
    {
        shell.Position = clear;
        shell.Update(TestSimStep);
        shell.Update(TestSimStep);
    }

    [TestMethod]
    public void AShotWhoseLockEndsInsideAShipPassesThroughItUntilItIsClear()
    {
        Ship screen = SpawnShip("Vulcan Scout", Player, Gun);
        Ship target = SpawnShip("Vulcan Scout", Player, Gun + new Vector2(0, -3000));
        Ship gunship = SpawnShip("Vulcan Scout", Enemy, Gun + new Vector2(0, 3000));
        Projectile shell = Shell(gunship, Gun, target);
        shell.LockedOn = target;
        AssertEqual(0, Collide(shell, screen), "setup: the locked-on shell passes the screen");

        shell.LockedOn = null;
        shell.Update(TestSimStep);
        shell.Position = Gun;
        AssertEqual(0, Collide(shell, screen), "a shell that loses its lock inside a ship does not hit it from the inside");

        StepClearOfShips(shell, Gun + new Vector2(screen.Radius * 3f, 0));
        shell.Position = Gun;
        AssertEqual(1, Collide(shell, screen), "once clear, a shell with no lock hits the ship again");
    }

    [TestMethod]
    public unsafe void AShotWhoseLockEndsAmongOverlappingShipsPassesThemAll()
    {
        Ship first = SpawnShip("Vulcan Scout", Player, Gun);
        Ship second = SpawnShip("Vulcan Scout", Player, Gun);
        Ship target = SpawnShip("Vulcan Scout", Player, Gun + new Vector2(0, -3000));
        Ship gunship = SpawnShip("Vulcan Scout", Enemy, Gun + new Vector2(0, 3000));
        Projectile shell = Shell(gunship, Gun, target);
        shell.LockedOn = target;

        var objects = new SpatialObjectBase[] { shell, first, second };
        CollisionPair* pairs = stackalloc CollisionPair[2];
        pairs[0] = new CollisionPair(0, 1);
        pairs[1] = new CollisionPair(0, 2);
        AssertEqual(0, NarrowPhase.Collide(TestSimStep, pairs, 2, objects), "setup: a locked-on shell passes both ships");

        shell.LockedOn = null;
        foreach (bool firstListedFirst in new[] { true, false })
        {
            shell.Update(TestSimStep);
            shell.Position = Gun;
            pairs[0] = new CollisionPair(0, firstListedFirst ? 1 : 2);
            pairs[1] = new CollisionPair(0, firstListedFirst ? 2 : 1);
            AssertEqual(0, NarrowPhase.Collide(TestSimStep, pairs, 2, objects),
                        $"a shell that loses its lock among overlapping ships hits neither ({(firstListedFirst ? "first" : "second")} listed first)");
        }
    }

    static Projectile LoadedShot(UniverseState loaded, Projectile shot)
    {
        Projectile found = loaded.Objects.GetAllProjectilesAndBeams().FirstOrDefault(p => p.Id == shot.Id);
        Assert.IsNotNull(found, "setup: the shot must be in the save");
        return found;
    }

    [TestMethod]
    public void AShotSavedAmongShipsItWasPassingStillPassesThemWhenLoaded()
    {
        Ship screen = SpawnShip("Vulcan Scout", Player, Gun);
        Ship target = SpawnShip("Vulcan Scout", Player, Gun + new Vector2(0, -3000));
        Ship gunship = SpawnShip("Vulcan Scout", Enemy, Gun + new Vector2(0, 3000));
        Projectile shell = Shell(gunship, Gun, target);
        shell.LockedOn = target;
        AssertEqual(0, Collide(shell, screen), "setup: the locked-on shell passes the screen");
        shell.LockedOn = null;
        RunObjectsSim(TestSimStep);

        UniverseState loaded = BinarySerializerTests.SerDes(UState);
        Projectile loadedShell = LoadedShot(loaded, shell);
        Ship loadedScreen = loaded.Objects.FindShip(screen.Id);
        Assert.IsNull(loadedShell.LockedOn, "setup: the shell was saved with no lock");
        loadedShell.Update(TestSimStep);
        loadedShell.Position = loadedScreen.Position;

        AssertEqual(0, Collide(loadedShell, loadedScreen), "a shell loaded among ships it was passing still passes them");
    }

    [TestMethod]
    public unsafe void ALockedOnMissileStillFallsToPointDefense()
    {
        Ship launcher = SpawnShip("Rocket Scout", Enemy, Gun + new Vector2(0, 5000));
        Ship target = SpawnShip("Vulcan Scout", Player, Gun + new Vector2(0, -3000));
        Ship defender = SpawnShip("Vulcan Scout", Player, Gun);
        Projectile missile = Projectile.Create(ResourceManager.CreateWeapon(UState, "Missile", launcher, null), launcher,
                                               Gun + new Vector2(0, 2000), Vectors.Up, target.Modules[0], playSound: false);
        missile.LockedOn = target;
        Projectile flak = Projectile.Create(ResourceManager.CreateWeapon(UState, "FlakAA Cannon", defender, null), defender,
                                            missile.Position, Vectors.Up, null, playSound: false);

        AssertEqual(1, Collide(missile, flak), "point defense hits a locked-on missile");
        AssertLessThanOrEqual(missile.Health, 0f, "the missile is shot down");
    }

    [TestMethod]
    public void ASmartMissileRollsAgainForTheShipItTurnsTo()
    {
        Enemy.data.Traits.SmartMissiles = true;
        Ship launcher = SpawnShip("Rocket Scout", Enemy, Gun);
        Ship first = SpawnShip("Vulcan Scout", Player, Gun + new Vector2(0, -3000));
        Ship next = SpawnShip("Vulcan Scout", Player, Gun + new Vector2(1000, -3000));
        launcher.AI.Target = next;
        Projectile missile = Missile(launcher, first);
        first.InitLaunch(LaunchPlan.Planet, 0f); // a target that starts launching is dropped for another
        Assert.IsTrue(first.IsLaunching, "setup: the first target must be launching");

        int locksOnNext = 0;
        for (int i = 0; i < 200; ++i)
        {
            missile.MissileAI.Target = first.Modules[0];
            missile.LockedOn = first;
            missile.MissileAI.Think(TestSimStep);
            Assert.AreNotEqual(first, missile.LockedOn, "the lock on the ship it turned away from ends");
            if (missile.LockedOn == next)
                ++locksOnNext;
        }
        Assert.IsTrue(locksOnNext > 0, "it rolls to lock on to the ship it turned to");
    }

    [TestMethod]
    public void AMissilePassingThroughAShipRollsNoLockWhenItTurnsToThatShip()
    {
        Enemy.data.Traits.SmartMissiles = true;
        Ship launcher = SpawnShip("Rocket Scout", Enemy, Gun + new Vector2(0, 5000));
        Ship first = SpawnShip("Vulcan Scout", Player, Gun + new Vector2(0, -3000));
        Ship screen = SpawnShip("Vulcan Scout", Player, Gun);
        launcher.AI.Target = screen;
        Projectile missile = Projectile.Create(GuidedWeapon(launcher), launcher, Gun, Vectors.Up, first.Modules[0], playSound: false);
        missile.LockedOn = first;
        AssertEqual(0, Collide(missile, screen), "setup: the missile locked on the first ship passes through the screen");
        first.InitLaunch(LaunchPlan.Planet, 0f); // a target that starts launching is dropped for another
        Assert.IsTrue(first.IsLaunching, "setup: the first target must be launching");

        for (int i = 0; i < 200; ++i)
        {
            missile.MissileAI.Target = first.Modules[0];
            missile.LockedOn = first;
            missile.MissileAI.Think(TestSimStep);
            Assert.AreEqual(screen, (missile.MissileAI.Target as ShipModule)?.GetParent(), "setup: the missile turns to the screen");
            Assert.AreNotEqual(screen, missile.LockedOn, "a missile inside the ship it turns to does not lock on to it");
        }
        AssertEqual(0, Collide(missile, screen), "so it does not hit that ship from the inside");
    }

    [TestMethod]
    public void ADeflectedShotLosesItsLock()
    {
        Ship screen = SpawnShip("Vulcan Scout", Player, Gun);
        Ship target = SpawnShip("Vulcan Scout", Player, Gun + new Vector2(0, -3000));
        Ship gunship = SpawnShip("Vulcan Scout", Enemy, Gun + new Vector2(0, 3000));
        Ship other = SpawnShip("Vulcan Scout", Enemy, Gun);
        Projectile shell = Shell(gunship, Gun, target);
        shell.LockedOn = target;
        AssertEqual(0, Collide(shell, screen), "setup: the locked-on shell passes the screen");

        shell.Deflect(Player, new SDGraphics.Vector3(Gun, 0f));

        Assert.IsNull(shell.LockedOn, "a deflected shot flies off with no lock");
        AssertEqual(1, Collide(shell, other), "and hits the next ship of another empire it meets");
    }

    [TestMethod]
    public void ASmartMissileKeepsItsLockWhenItTurnsToAnotherPartOfTheSameShip()
    {
        Enemy.data.Traits.SmartMissiles = true;
        Ship launcher = SpawnShip("Rocket Scout", Enemy, Gun);
        Ship target = SpawnShip("Vulcan Scout", Player, Gun + new Vector2(0, -3000));
        launcher.AI.Target = target;
        Projectile missile = Missile(launcher, target);
        ShipModule wrecked = target.Modules[0];

        for (int retargets = 0; retargets < 30; ++retargets)
        {
            wrecked.Active = false; // the part it aimed at is gone, the ship is not
            missile.MissileAI.Target = wrecked;
            missile.LockedOn = target;
            for (int i = 0; i < 200 && missile.MissileAI.Target == wrecked; ++i)
                missile.MissileAI.Think(TestSimStep);

            Assert.AreNotEqual(wrecked, missile.MissileAI.Target, "setup: the missile must turn to another part");
            Assert.AreEqual(target, missile.LockedOn, "turning to another part of the same ship keeps the lock");
        }
    }

    [TestMethod]
    public void ATorpedoKeepsItsLockWhileItsShipLives()
    {
        Ship launcher = SpawnShip("Rocket Scout", Enemy, Gun);
        Ship target = SpawnShip("Vulcan Scout", Player, Gun + new Vector2(0, -3000));
        Weapon tubes = ResourceManager.CreateWeapon(UState, "BasicTorpedo", launcher, null);
        Projectile torpedo = Projectile.Create(tubes, launcher, Gun, Vectors.Up, target.Modules[0], playSound: false);
        Assert.IsTrue(torpedo.Weapon.Tag_Torpedo, "setup: BasicTorpedo must fire torpedoes");
        torpedo.LockedOn = target;

        target.Modules[0].Active = false;
        for (int i = 0; i < 100 && torpedo.MissileAI.Target != null; ++i)
            torpedo.MissileAI.Think(TestSimStep);

        Assert.IsNull(torpedo.MissileAI.Target, "setup: a torpedo whose aim point is gone flies straight on");
        Assert.AreEqual(target, torpedo.LockedOn, "it stays locked on to its ship while the ship lives");
    }

    [TestMethod]
    public void MirvWarheadsTakeTheirMissilesLockWithoutARollOfTheirOwn()
    {
        Ship launcher = SpawnShip("Rocket Scout", Enemy, Gun);
        Ship target = SpawnShip("Vulcan Scout", Player, Gun + new Vector2(0, -3000));
        Weapon rack = ResourceManager.CreateWeapon(UState, "ClusterMissiles", launcher, null);
        int warheads = ResourceManager.GetWeaponTemplate("ClusterMirv").ProjectileCount;

        Projectile locked = Projectile.Create(rack, launcher, Gun, Vectors.Up, target.Modules[0], playSound: false);
        locked.LockedOn = target;
        locked.CreateMirv(target.Modules[0]);
        Projectile[] lockedWarheads = GetProjectiles(launcher).Filter(p => p.Weapon.UID == "ClusterMirv");
        AssertEqual(warheads, lockedWarheads.Length, "setup: the missile splits into every warhead");
        foreach (Projectile p in lockedWarheads)
            Assert.AreEqual(target, p.LockedOn, "a warhead takes its missile's lock");

        Projectile unlocked = Projectile.Create(rack, launcher, Gun, Vectors.Up, target.Modules[0], playSound: false);
        unlocked.LockedOn = null;
        unlocked.CreateMirv(target.Modules[0]);
        Projectile[] all = GetProjectiles(launcher).Filter(p => p.Weapon.UID == "ClusterMirv");
        AssertEqual(warheads * 2, all.Length, "setup: the second missile splits too");
        foreach (Projectile p in all)
            if (Array.IndexOf(lockedWarheads, p) < 0)
                Assert.IsNull(p.LockedOn, "a warhead of a missile with no lock has none");
    }

    [TestMethod]
    public void ALockOnASunkShipEnds()
    {
        Ship target = SpawnShip("Vulcan Scout", Player, Gun + new Vector2(0, -3000));
        Ship gunship = SpawnShip("Vulcan Scout", Enemy, Gun);
        Projectile shell = Shell(gunship, Gun, target);
        shell.LockedOn = target;

        target.Die(null, cleanupOnly: true);
        shell.Update(TestSimStep);

        Assert.IsNull(shell.LockedOn, "a shot locked on to a ship that is gone has no lock left to save");
    }

    [TestMethod]
    public void ALockOnAShipThatJustDiedIsNotSaved()
    {
        Ship target = SpawnShip("Vulcan Scout", Player, Gun + new Vector2(0, -3000));
        Ship gunship = SpawnShip("Vulcan Scout", Enemy, Gun);
        Projectile shell = Shell(gunship, Gun + new Vector2(20_000, 0), target);
        shell.LockedOn = target;
        RunObjectsSim(TestSimStep);

        target.Die(null, cleanupOnly: true); // killed in the collision step, saved before the shell's next update
        Projectile loadedShell = LoadedShot(BinarySerializerTests.SerDes(UState), shell);

        Assert.IsNull(loadedShell.LockedOn, "a save holds no lock on a ship that is gone");
    }

    [TestMethod]
    public void AShotLockedOnAShipThatJustDiedHitsTheNextShipItMeets()
    {
        Ship screen = SpawnShip("Vulcan Scout", Player, Gun);
        Ship target = SpawnShip("Vulcan Scout", Player, Gun + new Vector2(0, -3000));
        Ship gunship = SpawnShip("Vulcan Scout", Enemy, Gun + new Vector2(0, 3000));
        Projectile shell = Shell(gunship, Gun, target);
        shell.LockedOn = target;

        target.Die(null, cleanupOnly: true);

        AssertEqual(1, Collide(shell, screen), "a shot whose target died this step has no lock left to pass a ship with");
    }

    [TestMethod]
    public void MirvWarheadsOfAMissileAmongShipsItWasPassingPassThemToo()
    {
        Ship screen = SpawnShip("Vulcan Scout", Player, Gun);
        Ship target = SpawnShip("Vulcan Scout", Player, Gun + new Vector2(0, -3000));
        Ship launcher = SpawnShip("Rocket Scout", Enemy, Gun + new Vector2(0, 3000));
        Weapon rack = ResourceManager.CreateWeapon(UState, "ClusterMissiles", launcher, null);
        Projectile missile = Projectile.Create(rack, launcher, Gun, Vectors.Up, target.Modules[0], playSound: false);
        missile.LockedOn = target;
        AssertEqual(0, Collide(missile, screen), "setup: the locked-on missile passes the screen");
        missile.LockedOn = null;

        missile.CreateMirv(target.Modules[0]);
        Projectile[] warheads = GetProjectiles(launcher).Filter(p => p.Weapon.UID == "ClusterMirv");
        Assert.IsTrue(warheads.Length > 0, "setup: the missile splits");
        Projectile warhead = warheads[0];
        warhead.Update(TestSimStep);
        warhead.Position = Gun;

        AssertEqual(0, Collide(warhead, screen), "a warhead born among the ships its missile was passing passes them too");
    }

    [TestMethod]
    public void AShotKeepsItsLockThroughASave()
    {
        Ship target = SpawnShip("Vulcan Scout", Player, Gun + new Vector2(0, -3000));
        Ship gunship = SpawnShip("Vulcan Scout", Enemy, Gun);
        Projectile shell = Shell(gunship, Gun + new Vector2(20_000, 0), target);
        shell.LockedOn = target;
        RunObjectsSim(TestSimStep);

        Projectile loadedShell = LoadedShot(BinarySerializerTests.SerDes(UState), shell);

        AssertEqual(target.Id, loadedShell.LockedOn?.Id ?? -1, "a loaded shot keeps the lock it was saved with");
    }
}
