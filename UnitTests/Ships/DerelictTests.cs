using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using SDGraphics;
using SDUtils;
using Ship_Game;
using Ship_Game.Gameplay;
using Ship_Game.Graphics.Particles;
using Ship_Game.Ships;
using Ship_Game.Utils;
using SynapseGaming.LightingSystem.Core;
using SynapseGaming.LightingSystem.Rendering;
using Matrix = SDGraphics.Matrix;
using Vector2 = SDGraphics.Vector2;
using BoundingFrustum = Microsoft.Xna.Framework.BoundingFrustum;
#pragma warning disable CA2213

namespace UnitTests.Ships;

/// <summary>
/// Some deaths of frigates and bigger military ships, and of stations on their own, leave a derelict: no main
/// explosion and no death blast. On screen it breaks up and drifts on with its lights out, tumbling and smoking,
/// then sinks away. Unseen, or past the cap of drifting derelicts, it leaves nothing, still with no blast.
/// </summary>
[TestClass]
public class DerelictTests : StarDriveTest
{
    readonly float DerelictChance = GlobalStats.Defaults.DerelictChance;
    readonly int MaxDriftingDerelicts = GlobalStats.Defaults.MaxDriftingDerelicts;

    public DerelictTests()
    {
        LoadStarterShips("Dreadnought mk1-a", "Corsair", "Hunter mk1-a", "Vulcan Scout", "Seeder Transport", "Shipyard",
                         "Platform Base mk1-a", "Subspace Projector", "Cordrazine Small Station Mk1",
                         "C-Research Station mk1-a", "C-Mining Station mk1-a");
        CreateUniverseAndPlayerEmpire();
    }

    public override void Cleanup()
    {
        GlobalStats.Defaults.DerelictChance = DerelictChance;
        GlobalStats.Defaults.MaxDriftingDerelicts = MaxDriftingDerelicts;
        UState.Derelicts.Clear();
        Universe.Particles?.Dispose();
        Universe.Particles = null;
        base.Cleanup();
    }

    static Vector2 Site(int i) => new(200_000f + i * 20_000f, 200_000f);

    void ViewEverything()
    {
        Universe.Particles = new ParticleManager(Content);
        Universe.Frustum = new BoundingFrustum(Matrix.CreateOrthographicOffCenter(0, 1_000_000, 1_000_000, 0, -10_000, 10_000));
        UState.ViewState = UniverseScreen.UnivScreenState.PlanetView;
    }

    static SceneObject GiveSceneObject(Ship ship)
    {
        var so = new SceneObject(ship.Name);
        typeof(Ship).GetField("ShipSO", BindingFlags.NonPublic | BindingFlags.Instance)!.SetValue(ship, so);
        return so;
    }

    // the scene changes queued since the last call
    static Array<PendingItem<ISceneObject>> TakeSceneChanges()
    {
        var pending = (ChangePendingListSafe<ISceneObject>)typeof(ScreenManager)
            .GetField("PendingObjects", BindingFlags.NonPublic | BindingFlags.Instance)!.GetValue(ScreenManager.Instance);
        return pending.MovePendingItems();
    }

    static bool LeftTheScene(Array<PendingItem<ISceneObject>> changes, SceneObject so)
    {
        return changes.Any(c => !c.Add && c.Item == so);
    }

    static bool HasExploded(Ship ship)
    {
        return (bool)typeof(Ship).GetField("HasExploded", BindingFlags.NonPublic | BindingFlags.Instance)!.GetValue(ship)!;
    }

    void MakeVisible(Ship ship)
    {
        ship.InFrustum = true;
        ship.KnownByEmpires.SetSeen(Player);
        Assert.IsTrue(ship.IsVisibleToPlayer, "setup: the ship is on screen");
    }

    static void DrainShields(Ship s)
    {
        foreach (ShipModule m in s.GetShields())
            m.DamageShield(m.ShieldPower, null, out float _);
    }

    void Step(Derelict d, float seconds, Action body = null)
    {
        int steps = (int)Math.Round(seconds / TestSimStepD);
        for (int i = 0; i < steps; ++i)
        {
            d.Update(TestSimStep);
            body?.Invoke();
        }
    }

    // TestShip.Die always cleans up, which skips the roll, so the ships that die here are plain ships
    Ship SpawnPlainShip(string shipName, Vector2 position)
    {
        Ship ship = Ship.CreateShipAtPoint(UState, shipName, Player, position);
        ship.UpdateShipStatus(new FixedSimTime(0.01f));
        ship.UpdateModulePositions(new FixedSimTime(0.01f), forceUpdate: true);
        return ship;
    }

    // a dreadnought stacked on another, whose module nearest the blast is internal and takes the blast
    (Ship Dying, ShipModule Core) StackedPair(Vector2 site)
    {
        Ship dying = SpawnPlainShip("Dreadnought mk1-a", site);
        TestShip stacked = SpawnShip("Dreadnought mk1-a", Player, site + new Vector2(8f, 0));
        UState.Objects.Update(TestSimStep);
        DrainShields(stacked);
        return (dying, stacked.FindClosestModule(site));
    }

    [TestMethod]
    public void FrigatesAndBiggerWarshipsAndStationsOnTheirOwnCanBecomeDerelicts()
    {
        AssertTrue(SpawnShip("Dreadnought mk1-a", Player, Site(0)).CanBecomeDerelict, "a capital warship");
        TestShip frigate = SpawnShip("Corsair", Player, Site(1));
        AssertEqual(RoleName.frigate, frigate.ShipData.HullRole, "setup: the Corsair is a frigate");
        AssertTrue(frigate.CanBecomeDerelict, "a frigate warship");
        AssertFalse(SpawnShip("Hunter mk1-a", Player, Site(2)).CanBecomeDerelict, "a corvette");
        AssertFalse(SpawnShip("Vulcan Scout", Player, Site(3)).CanBecomeDerelict, "a fighter");
        AssertFalse(SpawnShip("Seeder Transport", Player, Site(4)).CanBecomeDerelict, "a freighter");
        AssertFalse(SpawnShip("Shipyard", Player, Site(5)).CanBecomeDerelict, "a shipyard");
        AssertFalse(SpawnShip("Platform Base mk1-a", Player, Site(6)).CanBecomeDerelict, "a platform");
        AssertFalse(SpawnShip("Subspace Projector", Player, Site(7)).CanBecomeDerelict, "a subspace projector");

        TestShip research = SpawnShip("C-Research Station mk1-a", Player, Site(8));
        TestShip mining = SpawnShip("C-Mining Station mk1-a", Player, Site(9));
        AssertTrue(research.IsResearchStation && mining.IsMiningStation, "setup: a research and a mining station");
        AssertFalse(research.CanBecomeDerelict, "a research station");
        AssertFalse(mining.CanBecomeDerelict, "a mining station");

        TestShip station = SpawnShip("Cordrazine Small Station Mk1", Player, Site(10));
        AssertTrue(station.IsStation, "setup: a station");
        AssertTrue(station.CanBecomeDerelict, "a station on its own");

        TestShip lander = SpawnShip("Dreadnought mk1-a", Player, Site(11));
        lander.InitLandingOnStation(station);
        AssertTrue(lander.IsLanding, "setup: the dreadnought is landing");
        AssertFalse(lander.CanBecomeDerelict, "a ship that is landing");

        frigate.InitLaunch(LaunchPlan.Planet, 0f);
        AssertTrue(frigate.IsLaunching, "setup: the frigate is launching");
        AssertFalse(frigate.CanBecomeDerelict, "a ship that is launching");

        station.TetherToPlanet(AddDummyPlanet(Site(12), 1f, 1f, 1f, Site(12) + new Vector2(5000f, 0), explored: true));
        AssertFalse(station.CanBecomeDerelict, "a station tethered to a planet");
    }

    [TestMethod]
    public void ACivilianShipOnAFrigateHullNeverBecomesADerelict()
    {
        ShipDesign freighter = CivilianUpkeepTests.CargoDesign("Terran/Gunboat", ShipCategory.Civilian);
        TestShip ship = SpawnShip(Ship.CreateNewShipTemplate(Empire.Void, freighter), Player, Site(0));
        AssertEqual(RoleName.frigate, ship.ShipData.HullRole, "setup: a frigate hull");
        AssertEqual(RoleType.Civilian, ship.DesignRoleType, "setup: a civilian design");
        AssertFalse(ship.CanBecomeDerelict, "only military designs become derelicts, whatever their hull");
    }

    [TestMethod]
    public void ADerelictDeathGivesNoBlast()
    {
        GlobalStats.Defaults.DerelictChance = 0;
        (Ship control, ShipModule controlCore) = StackedPair(Site(0));
        control.Die(null, cleanupOnly: false);
        AssertGreaterThan(controlCore.ActualMaxHealth, controlCore.Health, "control: an ordinary death blasts");

        GlobalStats.Defaults.DerelictChance = 100;
        (Ship dying, ShipModule core) = StackedPair(Site(1));
        GiveSceneObject(dying);
        dying.Die(null, cleanupOnly: false);
        AssertFalse(dying.Active, "the ship is dead");
        AssertEqual(0.001f, core.ActualMaxHealth, core.Health, "a derelict death gives no blast");

        UState.Objects.Update(TestSimStep);
        AssertEqual(0, UState.Derelicts.Count, "unseen, it leaves nothing");
    }

    [TestMethod]
    public void AnInstantKillCanBecomeADerelictToo()
    {
        TestShip shooter = SpawnShip("Corsair", Enemy, Site(5));
        Projectile shell = Projectile.Create(shooter.Weapons[0], shooter, shooter.Position, Vectors.Up, null, playSound: false);
        shell.Explodes = true;
        shell.DamageAmount = 1_000_000f; // far over the instant kill line

        GlobalStats.Defaults.DerelictChance = 100;
        (Ship dying, ShipModule core) = StackedPair(Site(0));
        dying.Die(shell, cleanupOnly: false);
        AssertFalse(dying.Active, "the ship is dead");
        AssertEqual(0.001f, core.ActualMaxHealth, core.Health, "the roll comes before the instant kill, so no blast");
    }

    [TestMethod]
    public void AShipThatCrashesOnAPlanetDoesNotBecomeADerelict()
    {
        GlobalStats.Defaults.DerelictChance = 100;
        Planet planet = AddDummyPlanet(Site(0), 1f, 1f, 1f, Site(0) + new Vector2(5000f, 0), explored: true);
        typeof(SolarSystemBody).GetProperty(nameof(SolarSystemBody.GravityWellRadius))!.SetValue(planet, 10_000f);
        int crashes = 0;
        for (int trial = 0; trial < 40; ++trial)
        {
            Ship ship = SpawnPlainShip("Dreadnought mk1-a", planet.Position + new Vector2(1000f, 0));
            AssertTrue(ship.CanBecomeDerelict, "setup: the ship can become a derelict");
            ship.System = planet.System;
            ship.Die(null, cleanupOnly: false);
            if (ship.Active)
            {
                AssertTrue(ship.Dying && ship.PlanetCrash != null, $"trial {trial}: a ship that lives on is crashing");
                ++crashes;
            }
        }
        AssertGreaterThan(crashes, 0, "35% of the deaths near a planet still crash on it, derelict roll or not");
    }

    [TestMethod]
    public void AtFullChanceAShipAwayFromPlanetsNeverTumbles()
    {
        GlobalStats.Defaults.DerelictChance = 100;
        for (int trial = 0; trial < 20; ++trial)
        {
            Ship ship = SpawnPlainShip("Dreadnought mk1-a", Site(trial));
            ship.InFrustum = true; // on screen, a ship that does not become a derelict tumbles 35% of the time
            ship.Die(null, cleanupOnly: false);
            AssertFalse(ship.Dying, $"trial {trial}: it became a derelict, never a tumbling wreck");
            AssertFalse(ship.Active, $"trial {trial}: dead at once");
        }
    }

    [TestMethod]
    public void TheDebugKeyMakesADerelictOnlyOfAShipThatCanBeOne()
    {
        ViewEverything();
        GlobalStats.Defaults.DerelictChance = 0;
        Ship ship = SpawnPlainShip("Dreadnought mk1-a", Site(0));
        Ship corvette = SpawnPlainShip("Hunter mk1-a", Site(1));
        UState.Objects.Update(TestSimStep);
        GiveSceneObject(ship);
        MakeVisible(ship);

        corvette.InFrustum = false; // off screen, an ordinary death never tumbles, so it would show as dead
        corvette.DebugDieAsDerelict();
        AssertTrue(corvette.Active, "a corvette cannot become a derelict, so nothing happens to it");

        ship.DebugDieAsDerelict();
        AssertFalse(ship.Active, "the dreadnought is destroyed");
        UState.Objects.Update(TestSimStep);
        AssertEqual(1, UState.Derelicts.Count, "as a derelict, with no roll");
    }

    [TestMethod]
    public void AVisibleDerelictDeathKeepsItsHullInTheScene()
    {
        ViewEverything();
        GlobalStats.Defaults.DerelictChance = 100;
        Ship ship = SpawnPlainShip("Dreadnought mk1-a", Site(0));
        UState.Objects.Update(TestSimStep);
        SceneObject so = GiveSceneObject(ship);
        MakeVisible(ship);

        ship.Die(null, cleanupOnly: false);
        AssertEqual(0, UState.Derelicts.Count, "the derelict waits for the ship's removal on the sim thread");
        AssertTrue(ship.HasSO, "until then the hull is the ship's");

        TakeSceneChanges();
        UState.Objects.Update(TestSimStep);
        AssertEqual(1, UState.Derelicts.Count, "a visible derelict death leaves a derelict");
        Derelict d = UState.Derelicts.ToArray()[0];
        AssertFalse(LeftTheScene(TakeSceneChanges(), so), "removing the ship leaves its hull in the scene");
        AssertTrue(d.InScene, "with the derelict");
        AssertEqual(0.001f, ship.Position.X, d.Position.X, "where the ship died");
        AssertFalse(HasExploded(ship), "with no explosion");

        GiveSceneObject(ship);
        ship.LeaveDerelict(); // a save or the UI removing the ship too, having seen its hull
        AssertEqual(1, UState.Derelicts.Count, "a ship leaves one derelict");
        AssertFalse(HasExploded(ship), "and no explosion over it");
    }

    [TestMethod]
    public void PastTheCapADerelictDeathExplodesAsUsualStillWithNoBlast()
    {
        ViewEverything();
        GlobalStats.Defaults.DerelictChance = 100;
        GlobalStats.Defaults.MaxDriftingDerelicts = 1;
        Ship first = SpawnPlainShip("Dreadnought mk1-a", Site(0));
        (Ship dying, ShipModule core) = StackedPair(Site(1));
        GiveSceneObject(first);
        SceneObject so = GiveSceneObject(dying);
        MakeVisible(first);
        first.Die(null, cleanupOnly: false);
        UState.Objects.Update(TestSimStep);
        AssertEqual(1, UState.Derelicts.Count, "setup: the first one fills the cap");

        MakeVisible(dying);
        dying.Die(null, cleanupOnly: false);
        AssertFalse(HasExploded(dying), "setup: a seen derelict death waits for the removal to explode or not");
        TakeSceneChanges();
        UState.Objects.Update(TestSimStep);
        AssertFalse(dying.Active, "the ship is dead");
        AssertEqual(1, UState.Derelicts.Count, "no derelict past the cap");
        AssertTrue(HasExploded(dying), "it explodes as usual");
        AssertTrue(LeftTheScene(TakeSceneChanges(), so), "its hull leaves the scene like any dead ship's");
        AssertEqual(0.001f, core.ActualMaxHealth, core.Health, "with no blast");
    }

    [TestMethod]
    public void AScuttledShipNeverBecomesADerelict()
    {
        ViewEverything();
        GlobalStats.Defaults.DerelictChance = 100;
        (Ship scuttled, ShipModule core) = StackedPair(Site(0));
        GiveSceneObject(scuttled);
        MakeVisible(scuttled);

        scuttled.ScuttleTimer = 0.01f; // a scuttle order about to run out
        scuttled.Update(TestSimStep);
        AssertFalse(scuttled.Active, "the scuttled ship is dead");
        UState.Objects.Update(TestSimStep);
        AssertEqual(0, UState.Derelicts.Count, "scuttling skips the derelict roll");
        AssertGreaterThan(core.ActualMaxHealth, core.Health, "so it blasts like an ordinary death");
    }

    [TestMethod]
    public void ADriftingDerelictKeepsItsSpeedThenSinksAway()
    {
        TestShip ship = SpawnShip("Dreadnought mk1-a", Player, Site(0));
        UState.Objects.Update(TestSimStep);
        AssertGreaterThan(ship.MaxSTLSpeed, 0f, "setup: the ship has a top sublight speed");
        ship.Velocity = new Vector2(ship.MaxSTLSpeed * 10f, 0f); // it died at warp
        var d = new Derelict(ship, new SceneObject(ship.Name));
        float startX = d.Position.X;

        d.Update(FixedSimTime.Zero);
        AssertEqual(0.001f, startX, d.Position.X, "paused, it stays put");

        Step(d, d.BreakupSeconds);
        float distance = ship.MaxSTLSpeed * d.BreakupSeconds;
        AssertEqual(distance * 0.01f, distance, d.Position.X - startX, "it drifts on at top sublight speed at most");
        AssertEqual(0.01f, ship.MaxSTLSpeed, d.VelocityNow.Length(), "without slowing down");
        AssertEqual(1f, 0f, d.Z, "on the plane");

        Step(d, 10f);
        float later = ship.MaxSTLSpeed * (d.BreakupSeconds + 10f);
        AssertEqual(later * 0.01f, later, d.Position.X - startX, "it keeps drifting");
        AssertEqual(2f, 10f * Derelict.SinkSpeed, d.Z, "and sinks at 50 a second");

        float seconds = d.BreakupSeconds + 10f;
        while (d.Update(TestSimStep))
        {
            seconds += TestSimStep.FixedTime;
            Assert.IsTrue(seconds < 200f, "the derelict never left");
        }
        AssertEqual(0.5f, d.BreakupSeconds + Derelict.RemoveDepth / Derelict.SinkSpeed, seconds,
                    "it sinks after its breakup and leaves at depth 5000, 100 s later");
        AssertFalse(d.InScene, "and takes its hull out of the scene");
    }

    [TestMethod]
    public void ASalvageableDerelictSlowsToAStopInTenSeconds()
    {
        TestShip ship = SpawnShip("Dreadnought mk1-a", Player, Site(0));
        UState.Objects.Update(TestSimStep);
        ship.Velocity = new Vector2(ship.MaxSTLSpeed, 0f);
        var d = new Derelict(ship, new SceneObject(ship.Name), salvageable: true);
        float startX = d.Position.X;

        Step(d, Derelict.StopSeconds);
        float stopDistance = ship.MaxSTLSpeed * Derelict.StopSeconds / 2f;
        AssertEqual(stopDistance * 0.01f, stopDistance, d.Position.X - startX, "it slows to a stop over 10 s");
        AssertEqual(0.001f, 0f, d.VelocityNow.Length(), "and stops");

        float stopX = d.Position.X;
        Step(d, 10f);
        AssertEqual(0.001f, stopX, d.Position.X, "it stays stopped");
        AssertEqual(0.001f, 0f, d.Z, "on the plane, with no sinking");
    }

    // the hull's size in its world matrix: rotation keeps a row's length, so the row is as long as the scale
    static float DrawnScale(SceneObject so) => new Vector3(so.World.M11, so.World.M12, so.World.M13).Length();

    [TestMethod]
    public void AsItSinksADerelictShrinksToATenthOfItsSize()
    {
        ViewEverything();
        TestShip ship = SpawnShip("Dreadnought mk1-a", Player, Site(0));
        UState.Objects.Update(TestSimStep);
        var so = new SceneObject(ship.Name);
        var d = new Derelict(ship, so);

        Step(d, d.BreakupSeconds);
        AssertEqual(GlobalStats.ShipVisibility, so.Visibility, "setup: on screen, so it is drawn");
        AssertEqual(0.01f, 1f, DrawnScale(so), "full size while it drifts on the plane");

        Step(d, Derelict.RemoveDepth / 2f / Derelict.SinkSpeed);
        AssertEqual(0.02f, (1f + Derelict.ScaleAtRemoval) / 2f, DrawnScale(so), "half way down, half way to a tenth");

        float last = DrawnScale(so);
        while (d.Update(TestSimStep))
            last = DrawnScale(so);
        AssertEqual(0.02f, Derelict.ScaleAtRemoval, last, "a tenth of its size when it leaves");
    }

    [TestMethod]
    public void TheListDropsDerelictsThatSankAwayAndClearsTheRest()
    {
        TestShip noHull = SpawnShip("Dreadnought mk1-a", Player, Site(0));
        AssertFalse(UState.Derelicts.TryAdd(noHull), "a ship with no scene object leaves no derelict");

        TestShip ship = SpawnShip("Dreadnought mk1-a", Player, Site(1));
        GiveSceneObject(ship);
        AssertTrue(UState.Derelicts.TryAdd(ship), "setup: a derelict");
        AssertFalse(ship.HasSO, "it took the hull");
        float lifetime = Derelict.MaxBreakupSeconds + Derelict.RemoveDepth / Derelict.SinkSpeed;
        for (int i = 0; i < (int)((lifetime + 1f) / TestSimStepD); ++i)
            UState.Derelicts.Update(TestSimStep);
        AssertEqual(0, UState.Derelicts.Count, "it sank away and left the list");

        GiveSceneObject(ship);
        UState.Derelicts.TryAdd(ship);
        Derelict d = UState.Derelicts.ToArray()[0];
        UState.Derelicts.Clear();
        AssertEqual(0, UState.Derelicts.Count, "cleared");
        AssertFalse(d.InScene, "and out of the scene");
    }

    [TestMethod]
    public void OnScreenADerelictTumblesAndSmokesWhileItsLightsGoOut()
    {
        ViewEverything();
        TestShip ship = SpawnShip("Dreadnought mk1-a", Player, Site(0));
        UState.Objects.Update(TestSimStep);
        ShipModule[] smoky = ship.Modules.Where(m => m.CanVisualizeDamage && !m.HasInternalRestrictions
                                                     && m.ModuleType != ShipModuleType.PowerConduit).Take(8).ToArray();
        AssertEqual(8, smoky.Length, "setup: modules that can smoke");
        foreach (ShipModule m in smoky)
            m.SetHealth(0f, "Test");
        AssertTrue(ship.Active, "setup: the ship lives on without them");

        var so = new SceneObject(ship.Name);
        var d = new Derelict(ship, so);
        AssertGreaterThan(ship.Modules.Length * Derelict.SmokeSourcesPerModule, smoky.Length, "setup: a fifth of its modules is more");
        AssertEqual(smoky.Length, d.NumSmokeSources, "each dead module smokes, as they are fewer than a fifth of its modules");

        var rotation = (d.RotationX, d.RotationY, d.RotationZ);
        var lights = new HashSet<float>();
        Step(d, d.BreakupSeconds, () => lights.Add(so.EmissiveScale));
        AssertEqual(GlobalStats.ShipVisibility, so.Visibility, "on screen, it is drawn");
        Assert.AreNotEqual(rotation, (d.RotationX, d.RotationY, d.RotationZ), "and tumbles");
        Assert.IsTrue(lights.Any(l => l > 0f) && lights.Contains(0f), "its lights flicker while it breaks up");

        Step(d, 1f);
        AssertEqual(0.001f, 0f, so.EmissiveScale, "then they are out");

        UState.ViewState = UniverseScreen.UnivScreenState.SectorView;
        rotation = (d.RotationX, d.RotationY, d.RotationZ);
        Step(d, 1f);
        AssertEqual(ObjectVisibility.None, so.Visibility, "zoomed out, it is hidden");
        AssertEqual(rotation, (d.RotationX, d.RotationY, d.RotationZ), "and stops tumbling");
    }

    [TestMethod]
    public void ADerelictSmokesFromAFifthOfItsModulesThinningOutToSix()
    {
        ViewEverything();
        var smoke = new MiningStationVisualsTests.CountingParticle();
        Universe.Particles = new ParticleManager(Content) { ExplosionSmoke = smoke };
        TestShip ship = SpawnShip("Dreadnought mk1-a", Player, Site(0));
        UState.Objects.Update(TestSimStep);
        ShipModule[] smoky = ship.Modules.Where(m => m.CanVisualizeDamage && !m.HasInternalRestrictions
                                                     && m.ModuleType != ShipModuleType.PowerConduit).ToArray();
        int fifth = (int)(ship.Modules.Length * Derelict.SmokeSourcesPerModule + 0.5f);
        AssertGreaterThan(smoky.Length, fifth, "setup: more dead modules than a fifth of them");
        AssertGreaterThan(fifth, 2 * Derelict.MinSmokeSources, "setup: a fifth is well over six");
        foreach (ShipModule m in smoky)
            m.SetHealth(0f, "Test");
        AssertTrue(ship.Active, "setup: the ship lives on without them");

        var d = new Derelict(ship, new SceneObject(ship.Name));
        AssertEqual(fifth, d.NumSmokeSources, "a fifth of its modules smoke");
        AssertEqual(fifth, d.NumSmokingNow, "all of them at first");

        smoke.Added = 0;
        Step(d, 1f);
        int atFirst = smoke.Added;
        AssertGreaterThan(atFirst, 0, "setup: it smokes on screen");

        Step(d, Derelict.SmokeThinSeconds / 2f - 1f);
        int halfway = d.NumSmokingNow;
        Assert.IsTrue(halfway > Derelict.MinSmokeSources && halfway < fifth, $"halfway, some are out: {halfway} of {fifth}");

        Step(d, Derelict.SmokeThinSeconds / 2f + 0.5f);
        AssertEqual(Derelict.MinSmokeSources, d.NumSmokingNow, "after 60 s, six are left");
        smoke.Added = 0;
        Step(d, 1f);
        Assert.IsTrue(smoke.Added < atFirst * 0.8f, $"the six give less smoke than all of them: {smoke.Added} vs {atFirst}");
        smoke.Added = 0;
        Step(d, 30f);
        AssertEqual(Derelict.MinSmokeSources, d.NumSmokingNow, "and they keep smoking");
        AssertGreaterThan(smoke.Added, 0, "smoke still comes out of them");
    }

    [TestMethod]
    public void AShipThatWasDarkStaysDarkAsADerelict()
    {
        ViewEverything();
        TestShip ship = SpawnShip("Terran-Prototype", Player, Site(0));
        UState.Objects.Update(TestSimStep);
        foreach (ShipModule m in ship.Modules.Where(m => m.PowerFlowMax > 0f))
            m.SetHealth(0f, "Test");
        for (int i = 0; i < 90; ++i)
        {
            ship.InFrustum = true;
            ship.Update(TestSimStep);
        }
        AssertTrue(ship.LightsOut, "setup: the reactors are gone and the lights are out");

        var so = new SceneObject(ship.Name);
        var d = new Derelict(ship, so);
        var lights = new HashSet<float>();
        Step(d, d.BreakupSeconds, () => lights.Add(so.EmissiveScale));
        AssertEqual(1, lights.Count, "no flicker");
        AssertEqual(0f, lights.First(), "the lights stay out");
    }
}
