using System;
using System.Linq;
using System.Reflection;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using SDGraphics;
using SDUtils;
using Ship_Game;
using Ship_Game.AI;
using Ship_Game.AI.CombatTactics;
using Ship_Game.ExtensionMethods;
using Ship_Game.Gameplay;
using Ship_Game.GameScreens.LoadGame;
using Ship_Game.Ships;
using Vector2 = SDGraphics.Vector2;

namespace UnitTests.Ships;

[TestClass]
public class ProjectileInheritanceTests : StarDriveTest
{
    static readonly Vector2 Start = new(200_000, 200_000);
    static readonly Vector2 Up = new(0, -1);

    public ProjectileInheritanceTests()
    {
        LoadStarterShips("Dreadnought mk1-a", "Corsair", "Vulcan Scout");
        CreateUniverseAndPlayerEmpire();
    }

    TestShip SpawnShooter() => SpawnShip("Corsair", Enemy, Start, Up);
    TestShip SpawnTargetAhead(float distance) => SpawnShip("Dreadnought mk1-a", Player, Start + Up * distance);
    Weapon CreateWeapon(string uid, Ship owner) => ResourceManager.CreateWeapon(UState, uid, owner, null);

    void RunMissile(Projectile missile, float seconds)
    {
        for (float time = 0f; time < seconds; time += TestSimStep.FixedTime)
            missile.Update(TestSimStep);
    }

    [TestMethod]
    public void AShotCarriesItsShipsVelocityOnTopOfItsOwnSpeed()
    {
        TestShip shooter = SpawnShooter();
        var shipVelocity = new Vector2(shooter.MaxSTLSpeed * 0.9f, 0f);
        AssertGreaterThan(shipVelocity.Length(), 100f, "setup: the shooter must be able to move");
        shooter.Velocity = shipVelocity;

        Projectile shot = Projectile.Create(CreateWeapon("MassDriver", shooter), shooter, shooter.Position, Up,
                                            null, playSound: false);
        Vector2 launched = Up * shot.Speed + shipVelocity;
        AssertEqual(0.01f, launched, shot.Velocity, "a shot leaves at its own speed on top of its ship's");

        for (int i = 0; i < 60; ++i)
            shot.TestUpdatePhysics(TestSimStep);
        AssertEqual(0.01f, launched, shot.Velocity, "a shot keeps that speed in flight");
    }

    [TestMethod]
    public void AShotFromAShipFlyingSidewaysHitsACrossingTarget()
    {
        TestShip shooter = SpawnShooter();
        TestShip target = SpawnTargetAhead(4000f);
        shooter.Velocity = new Vector2(shooter.MaxSTLSpeed * 0.9f, 0f);
        target.Velocity = new Vector2(-300f, 0f);
        target.ECMValue = 0f;
        Weapon gun = CreateWeapon("MassDriver", shooter);
        gun.PlanetOrigin = shooter.Position;

        object[] args = { target, null };
        bool aimed = (bool)typeof(Weapon).GetMethod("ProjectedImpactPoint", BindingFlags.Instance | BindingFlags.NonPublic)!
                                         .Invoke(gun, args)!;
        Assert.IsTrue(aimed, "setup: the gun must find an aim point");
        Vector2 aim = (Vector2)args[1];
        Projectile shot = Projectile.Create(gun, shooter, shooter.Position, shooter.Position.DirectionToTarget(aim),
                                            target, playSound: false);

        float closest = float.MaxValue;
        float time = 0f;
        while (shot.Active && time < 10f)
        {
            shot.TestUpdatePhysics(TestSimStep);
            time += TestSimStep.FixedTime;
            closest = Math.Min(closest, shot.Position.Distance(target.Position + target.Velocity * time));
        }
        AssertLessThan(closest, 40f, "a shot that carries its ship's sideways speed must still meet the crossing target");
    }

    [TestMethod]
    public void AMissileKeepsItsLaunchSpeedUntilItsEngineLights()
    {
        TestShip shooter = SpawnShooter();
        TestShip target = SpawnTargetAhead(20_000f);
        Weapon launcher = CreateWeapon("Hellstorm", shooter);
        AssertGreaterThan(launcher.DelayedIgnition, 1f, "setup: Hellstorm must light its engine late");

        Projectile missile = Projectile.Create(launcher, shooter, shooter.Position, Up, target, playSound: false,
                                               inheritedVelocity: Up * (launcher.ProjectileSpeed + 300f));
        Vector2 launched = missile.Velocity;
        AssertGreaterThan(launched.Length(), missile.VelocityMax + 250f, "setup: it must leave faster than its engine flies");

        RunMissile(missile, launcher.DelayedIgnition - 0.5f);
        Assert.IsFalse(missile.TrailTurnedOn, "setup: the engine must still be off");
        AssertEqual(0.01f, launched, missile.Velocity, "an unlit missile drifts on at its launch speed");

        RunMissile(missile, 1.5f);
        Assert.IsTrue(missile.TrailTurnedOn, "setup: the engine must have lit");
        AssertLessThan(missile.Velocity.Length(), missile.VelocityMax * 1.1f, "once lit, its engine brings it to its own speed");
    }

    [TestMethod]
    public void AMissileLaunchedFasterThanItFliesStillTurnsOntoATargetOffToItsSide()
    {
        TestShip shooter = SpawnShooter();
        TestShip target = SpawnTargetAhead(5000f);
        AssertLessThan(target.Radius, 1000f, "setup: the target must be small next to the 2700+ a stuck missile misses by");
        for (int launch = 0; launch < 5; ++launch)
        {
            Projectile missile = Projectile.Create(CreateWeapon("LRM2", shooter), shooter, shooter.Position, Up, target,
                                                   playSound: false, inheritedVelocity: new Vector2(1600f, 0f));
            AssertGreaterThan(missile.Velocity.Length(), missile.VelocityMax, "setup: it must leave faster than its engine flies");

            float closest = float.MaxValue;
            for (float time = 0f; time < 15f && missile.Active; time += TestSimStep.FixedTime)
            {
                missile.Update(TestSimStep);
                closest = Math.Min(closest, missile.Position.Distance(target.Position));
            }
            AssertLessThan(closest, target.Radius, $"launch {launch}: a missile sliding sideways past its top speed must still turn onto its target");
        }
    }

    [TestMethod]
    public void AMissileInFlightKeepsItsIgnitionWhenLoaded()
    {
        UState.StarDate = 1042.5f; // past the start, so loading does not hand out starting ships
        TestShip shooter = SpawnShooter();
        TestShip target = SpawnTargetAhead(20_000f);
        RunObjectsSim(TestSimStep);

        Projectile unlit = Projectile.Create(CreateWeapon("Hellstorm", shooter), shooter, shooter.Position, Up,
                                             target, playSound: false);
        Projectile lit = Projectile.Create(CreateWeapon("Missile", shooter), shooter, shooter.Position, Up,
                                           target, playSound: false);
        RunMissile(unlit, 0.5f);
        RunMissile(lit, 0.5f);
        RunObjectsSim(TestSimStep);
        float delayLeft = unlit.IgnitionDelay;
        AssertGreaterThan(delayLeft, 0.5f, "setup: the Hellstorm must still be waiting to light");
        AssertLessThan(delayLeft, unlit.Weapon.DelayedIgnition - 0.4f, "setup: part of its delay must have passed");
        Assert.IsTrue(lit.TrailTurnedOn, "setup: the plain missile must be lit");

        SavedGame save = Universe.Save("UnitTest.MissileIgnition", throwOnError: true);
        UniverseScreen loaded = LoadGame.Load(save.SaveFile, noErrorDialogs: true, startSimThread: false);
        Assert.IsNotNull(loaded, "the save must load");
        Projectile[] projectiles = loaded.UState.Objects.GetAllProjectilesAndBeams();
        Projectile loadedUnlit = projectiles.FirstOrDefault(p => p.Id == unlit.Id);
        Projectile loadedLit = projectiles.FirstOrDefault(p => p.Id == lit.Id);
        Assert.IsNotNull(loadedUnlit, "setup: the Hellstorm must survive the round trip");
        Assert.IsNotNull(loadedLit, "setup: the plain missile must survive the round trip");

        AssertEqual(0.001f, delayLeft, loadedUnlit.IgnitionDelay, "an unlit missile keeps only what is left of its delay");
        Assert.IsFalse(loadedUnlit.TrailTurnedOn, "an unlit missile stays unlit");
        Assert.IsTrue(loadedLit.TrailTurnedOn, "a lit missile stays lit");
    }

    [TestMethod]
    public void ClusterWarheadsLeaveTheirMissileAtTheirOwnSpeed()
    {
        TestShip shooter = SpawnShooter();
        TestShip target = SpawnTargetAhead(20_000f);
        Weapon launcher = CreateWeapon("ClusterMissiles", shooter);
        Projectile missile = Projectile.Create(launcher, shooter, shooter.Position, Up, target, playSound: false);
        RunMissile(missile, 1f);
        Vector2 missileVelocity = missile.Velocity;
        AssertGreaterThan(missileVelocity.Length(), 500f, "setup: the missile must be under way");

        missile.CreateMirv(target);

        Projectile[] warheads = GetProjectiles(shooter).Filter(p => p.Active && p.Weapon.UID == launcher.MirvWeapon);
        Assert.IsTrue(warheads.Length > 0, "setup: the missile must split into warheads");
        Assert.IsFalse(warheads[0].Weapon.Tag_Guided, "setup: cluster warheads must be unguided");
        foreach (Projectile warhead in warheads)
            AssertEqual(1f, warhead.Speed, (warhead.Velocity - missileVelocity).Length(),
                        "a warhead leaves at its own speed on top of its missile's");
    }

    [TestMethod]
    public void AShipJustPulledOutOfWarpLendsItsShotsNoMoreThanItsTopSublightSpeed()
    {
        TestShip shooter = SpawnShooter();
        TestShip target = SpawnTargetAhead(20_000f);
        shooter.engineState = Ship.MoveState.Warp;
        shooter.Velocity = Up * 40_000f;
        shooter.HyperspaceReturn();
        Assert.AreEqual(Ship.MoveState.Sublight, shooter.engineState, "setup: the ship must be out of warp");
        AssertGreaterThan(shooter.Velocity.Length(), 30_000f, "setup: the ship must still carry its warp speed");

        Projectile shot = Projectile.Create(CreateWeapon("MassDriver", shooter), shooter, shooter.Position, Up,
                                            target, playSound: false);
        Projectile missile = Projectile.Create(CreateWeapon("Missile", shooter), shooter, shooter.Position, Up,
                                               target, playSound: false);

        AssertLessThan((shot.Velocity - Up * shot.Speed).Length(), shooter.MaxSTLSpeed + 0.5f,
                       "a shot takes no more than the ship's top sublight speed");
        AssertLessThan(missile.Velocity.Length(), shooter.MaxSTLSpeed + 0.5f,
                       "a missile takes no more than the ship's top sublight speed");
    }

    void AssertASlidingShipTurnsToWhereItsShotsWillHit(Func<TestShip, Action> makeStanceStep, float turnSlack)
    {
        TestShip shooter = SpawnShip("Vulcan Scout", Enemy, Start, Up);
        TestShip target = SpawnTargetAhead(Math.Min(4000f, shooter.DesiredCombatRange * 0.6f));
        var shipVelocity = new Vector2(shooter.MaxSTLSpeed * 0.9f, 0f);
        shooter.Velocity = shipVelocity;
        shooter.AI.Target = target;

        Vector2 aimDir = shooter.Position.DirectionToTarget(shooter.PredictAim(target));
        Vector2 leadDir = shooter.Position.DirectionToTarget(shooter.PredictImpact(target));
        AssertGreaterThan(Vectors.AngleDifference(aimDir, leadDir), 0.15f,
                          "setup: the shots' aim must differ clearly from the plain lead");

        float leadAngle = leadDir.ToRadians();
        float beyondLead = turnSlack * 0.75f;
        float oneSide = leadAngle + beyondLead, otherSide = leadAngle - beyondLead;
        bool oneSideIsFurther = Vectors.AngleDifference(oneSide.RadiansToDirection(), aimDir)
                              > Vectors.AngleDifference(otherSide.RadiansToDirection(), aimDir);
        shooter.Rotation = (oneSideIsFurther ? oneSide : otherSide).AsNormalizedRadians();
        float startOff = Vectors.AngleDifference(shooter.Direction, aimDir);

        Action stanceStep = makeStanceStep(shooter);
        for (int i = 0; i < 60; ++i)
        {
            shooter.Velocity = shipVelocity;
            stanceStep();
            shooter.Update(TestSimStep);
        }

        Vector2 aimNow = shooter.Position.DirectionToTarget(shooter.PredictAim(target));
        float endOff = Vectors.AngleDifference(shooter.Direction, aimNow);
        AssertLessThan(endOff, turnSlack + 0.03f, "the ship turns until its guns point where its shots will meet the target");
        AssertGreaterThan(startOff - endOff, 0.1f, "setup: the ship must have turned towards the aim");
    }

    [TestMethod]
    public void AShipHoldingPositionTurnsToWhereItsShotsWillHit()
    {
        AssertASlidingShipTurnsToWhereItsShotsWillHit(ship =>
        {
            var plan = new HoldPosition(ship.AI);
            return () => plan.Execute(TestSimStep, null);
        }, turnSlack: 0.2f);
    }

    [TestMethod]
    public void AShipFightingInHoldPositionTurnsToWhereItsShotsWillHit()
    {
        MethodInfo holdCombat = typeof(ShipAI).GetMethod("DoHoldPositionCombat", BindingFlags.Instance | BindingFlags.NonPublic);
        Assert.IsNotNull(holdCombat, "setup: ShipAI.DoHoldPositionCombat must exist");
        AssertASlidingShipTurnsToWhereItsShotsWillHit(ship => () => holdCombat.Invoke(ship.AI, new object[] { TestSimStep }),
                                                      turnSlack: 0.2f);
    }

    [TestMethod]
    public void AnArtilleryShipTurnsToWhereItsShotsWillHit()
    {
        AssertASlidingShipTurnsToWhereItsShotsWillHit(ship =>
        {
            var stance = new Artillery(ship.AI);
            return () => stance.Execute(TestSimStep, null);
        }, turnSlack: 0.05f);
    }
}
