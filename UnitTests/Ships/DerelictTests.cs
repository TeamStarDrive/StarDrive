using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using SDGraphics;
using SDUtils;
using Ship_Game;
using Ship_Game.GameScreens.LoadGame;
using Ship_Game.Gameplay;
using Ship_Game.Graphics.Particles;
using Ship_Game.Ships;
using Ship_Game.Universe;
using Ship_Game.Universe.SolarBodies;
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
/// then sinks away. One that stops in a system outside the star's radiation, with salvage ops on, is salvageable:
/// it stays there, seen or not, until its turns run out. Unseen elsewhere, or past the cap of drifting derelicts,
/// it leaves nothing, still with no blast.
/// </summary>
[TestClass]
public class DerelictTests : StarDriveTest
{
    readonly float DerelictChance = GlobalStats.Defaults.DerelictChance;
    readonly int MaxDriftingDerelicts = GlobalStats.Defaults.MaxDriftingDerelicts;
    readonly int MaxSalvageableDerelicts = GlobalStats.Defaults.MaxSalvageableDerelicts;
    readonly bool EnableSalvageOps = GlobalStats.Defaults.EnableSalvageOps;

    public DerelictTests()
    {
        LoadStarterShips("Dreadnought mk1-a", "Corsair", "Hunter mk1-a", "Vulcan Scout", "Seeder Transport", "Shipyard",
                         "Platform Base mk1-a", "Subspace Projector", "Cordrazine Small Station Mk1",
                         "C-Research Station mk1-a", "C-Mining Station mk1-a");
        CreateUniverseAndPlayerEmpire();
    }

    [TestCleanup]
    public override void Cleanup()
    {
        GlobalStats.Defaults.DerelictChance = DerelictChance;
        GlobalStats.Defaults.MaxDriftingDerelicts = MaxDriftingDerelicts;
        GlobalStats.Defaults.MaxSalvageableDerelicts = MaxSalvageableDerelicts;
        GlobalStats.Defaults.EnableSalvageOps = EnableSalvageOps;
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

    static Derelict Drifting(Ship ship, SceneObject so) => new(ship, so, salvageable: false, seen: true);

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
        corvette.DebugDieAsDerelict(salvageable: false);
        AssertTrue(corvette.Active, "a corvette cannot become a derelict, so nothing happens to it");

        ship.DebugDieAsDerelict(salvageable: false);
        AssertFalse(ship.Active, "the dreadnought is destroyed");
        UState.Objects.Update(TestSimStep);
        AssertEqual(1, UState.Derelicts.Count, "as a derelict, with no roll");
    }

    [TestMethod]
    public void TheDebugKeysMakeADriftingOrInASystemASalvageableDerelict()
    {
        ViewEverything();
        GlobalStats.Defaults.DerelictChance = 0;
        AddSystem();
        Vector2 inSystem = SystemSite + new Vector2(30_000f, 0);

        Ship seen = SpawnPlainShip("Dreadnought mk1-a", inSystem);
        UState.Objects.Update(TestSimStep);
        GiveSceneObject(seen);
        MakeVisible(seen);
        seen.DebugDieAsDerelict(salvageable: false);
        UState.Objects.Update(TestSimStep);
        AssertEqual(1, UState.Derelicts.Count, "Ctrl+X: a seen death leaves a derelict, even in a system");
        AssertFalse(Newest().Salvageable, "a drifting one");

        Ship unseen = SpawnPlainShip("Dreadnought mk1-a", inSystem);
        AssertFalse(unseen.IsVisibleToPlayer, "setup: off screen");
        unseen.DebugDieAsDerelict(salvageable: false);
        AssertFalse(unseen.Active, "setup: destroyed");
        UState.Objects.Update(TestSimStep);
        AssertEqual(1, UState.Derelicts.Count, "unseen, it leaves nothing, like any unseen drifting one");

        Ship salvaged = SpawnPlainShip("Dreadnought mk1-a", inSystem);
        salvaged.DebugDieAsDerelict(salvageable: true);
        AssertFalse(salvaged.Active, "Ctrl+Shift+X destroys a ship in a system");
        UState.Objects.Update(TestSimStep);
        AssertEqual(2, UState.Derelicts.Count, "and leaves a derelict, seen or not");
        AssertTrue(Newest().CanBeSalvaged, "a salvageable one");

        Ship deepSpace = SpawnPlainShip("Dreadnought mk1-a", Site(0));
        deepSpace.DebugDieAsDerelict(salvageable: true);
        AssertTrue(deepSpace.Active, "outside every system it cannot be salvageable, so nothing happens to it");

        GlobalStats.Defaults.MaxSalvageableDerelicts = 1;
        Ship overLimit = SpawnPlainShip("Dreadnought mk1-a", inSystem);
        overLimit.DebugDieAsDerelict(salvageable: true);
        AssertTrue(overLimit.Active, "nor when the salvageable derelicts are all there");
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
        Derelict d = Drifting(ship, new SceneObject(ship.Name));
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
        var d = new Derelict(ship, new SceneObject(ship.Name), salvageable: true, seen: true);
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
        Derelict d = Drifting(ship, so);

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
        AssertFalse(UState.Derelicts.TryAdd(noHull, salvageable: false, seen: true), "a drifting one needs the ship's scene object");

        TestShip ship = SpawnShip("Dreadnought mk1-a", Player, Site(1));
        GiveSceneObject(ship);
        AssertTrue(UState.Derelicts.TryAdd(ship, salvageable: false, seen: true), "setup: a derelict");
        AssertFalse(ship.HasSO, "it took the hull");
        float lifetime = Derelict.MaxBreakupSeconds + Derelict.RemoveDepth / Derelict.SinkSpeed;
        for (int i = 0; i < (int)((lifetime + 1f) / TestSimStepD); ++i)
            UState.Derelicts.Update(TestSimStep);
        AssertEqual(0, UState.Derelicts.Count, "it sank away and left the list");

        GiveSceneObject(ship);
        UState.Derelicts.TryAdd(ship, salvageable: false, seen: true);
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
        Derelict d = Drifting(ship, so);
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

        Derelict d = Drifting(ship, new SceneObject(ship.Name));
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
        Derelict d = Drifting(ship, so);
        var lights = new HashSet<float>();
        Step(d, d.BreakupSeconds, () => lights.Add(so.EmissiveScale));
        AssertEqual(1, lights.Count, "no flicker");
        AssertEqual(0f, lights.First(), "the lights stay out");
    }

    // far from the sites in deep space, and its planet far from where ships die in it
    static readonly Vector2 SystemSite = new(600_000f, 600_000f);

    SolarSystem AddSystem()
    {
        Planet planet = AddDummyPlanet(SystemSite, 1f, 1f, 1f, SystemSite + new Vector2(0, 60_000f), explored: true);
        AssertGreaterThan(planet.System.Radius, 100_000f, "setup: the system reaches past the deaths in it");
        return planet.System;
    }

    Derelict Newest() => UState.Derelicts.ToArray().Last();

    // a dreadnought dies at the point, seen on screen or unseen, and its removal leaves a derelict or not
    Ship DieAt(Vector2 position, bool seen, Vector2 velocity = default)
    {
        Ship ship = SpawnPlainShip("Dreadnought mk1-a", position);
        UState.Objects.Update(TestSimStep); // sets its top speed
        if (seen)
        {
            GiveSceneObject(ship);
            MakeVisible(ship);
        }
        if (velocity.Length() > 0f)
            AssertGreaterThan(ship.MaxSTLSpeed, velocity.Length(), "setup: under the speed cap");
        ship.Velocity = velocity;
        ship.Die(null, cleanupOnly: false);
        UState.Objects.Update(TestSimStep);
        AssertFalse(ship.Active, "setup: the ship is dead");
        return ship;
    }

    [TestMethod]
    public void AModTurnsSalvageOpsOnItselfAndTheRuleOptionTurnsThemOff()
    {
        AssertTrue(GlobalStats.VanillaDefaults.EnableSalvageOps, "vanilla has salvage content");
        var mod = new GamePlayGlobals();
        AssertFalse(mod.EnableSalvageOps, "a mod does not inherit the flag");
        AssertEqual(GlobalStats.VanillaDefaults.MaxSalvageableDerelicts, mod.MaxSalvageableDerelicts, "as it does the other vanilla values");

        var p = new UniverseParams();
        AssertFalse(p.DisableSalvageOps, "with salvage content the rule option starts unticked");
        AssertTrue(p.SalvageOps, "so salvage ops are on");
        p.DisableSalvageOps = true;
        AssertFalse(p.SalvageOps, "ticked, it turns them off");

        GlobalStats.Defaults.EnableSalvageOps = false;
        p = new UniverseParams();
        AssertTrue(p.DisableSalvageOps, "without salvage content it starts ticked");
        p.DisableSalvageOps = false;
        AssertFalse(p.SalvageOps, "and unticking it cannot turn them on");
    }

    [TestMethod]
    public void AnUnseenDeathInASystemLeavesASalvageableDerelictAlreadyAtItsStopPoint()
    {
        GlobalStats.Defaults.DerelictChance = 100;
        AddSystem();
        Ship ship = SpawnPlainShip("Dreadnought mk1-a", SystemSite + new Vector2(30_000f, 0));
        UState.Objects.Update(TestSimStep);
        SceneObject so = GiveSceneObject(ship); // a ship keeps its hull a while after it leaves the screen
        AssertFalse(ship.IsVisibleToPlayer, "setup: the death is unseen");
        ship.Velocity = new Vector2(ship.MaxSTLSpeed, 0f);
        Vector2 stop = ship.Position + ship.Velocity * (Derelict.StopSeconds / 2f);
        ship.Die(null, cleanupOnly: false);
        TakeSceneChanges();
        UState.Objects.Update(TestSimStep);

        AssertEqual(1, UState.Derelicts.Count, "unseen, it still leaves a derelict in a system");
        Derelict d = Newest();
        AssertTrue(d.CanBeSalvaged, "a salvageable one");
        AssertEqual(1f, stop, d.Position, "already where it would have stopped");
        AssertEqual(0.001f, 0f, d.VelocityNow.Length(), "and stopped");
        AssertFalse(d.InScene, "with no model, so a wreck the player may not know of is not drawn");
        AssertTrue(LeftTheScene(TakeSceneChanges(), so), "the ship's leftover hull goes with the ship");
        AssertFalse(HasExploded(ship), "and no explosion");
        AssertEqual(ship.ShipData, d.Design, "it keeps the ship's design");
        AssertEqual(Player, d.Owner, "and its owner");
    }

    [TestMethod]
    public void ASeenDeathInASystemBreaksUpWhereTheShipDiedAndStaysSalvageable()
    {
        ViewEverything();
        GlobalStats.Defaults.DerelictChance = 100;
        AddSystem();
        Vector2 site = SystemSite + new Vector2(30_000f, 0);
        Ship ship = DieAt(site, seen: true, velocity: new Vector2(80f, 0));

        Derelict d = Newest();
        AssertTrue(d.CanBeSalvaged, "a salvageable derelict");
        AssertTrue(d.InScene, "with the ship's hull");
        AssertEqual(1f, site, d.Position, "where the ship died, not where it will stop");
        AssertGreaterThan(d.VelocityNow.Length(), 0f, "still moving");
        AssertTrue(d.IsKnownBy(Player), "the player saw it");
        AssertFalse(HasExploded(ship), "with no explosion");
    }

    [TestMethod]
    public void WhereItStopsNotWhereItDiedDecidesTheKind()
    {
        ViewEverything();
        GlobalStats.Defaults.DerelictChance = 100;
        SolarSystem system = AddSystem();
        var speed = new Vector2(80f, 0);
        float drift = speed.X * Derelict.StopSeconds / 2f;
        Vector2 outside = SystemSite + new Vector2(system.Radius + drift / 2f, 0);
        Vector2 inside = SystemSite + new Vector2(system.Radius - drift / 2f, 0);

        DieAt(outside, seen: false, velocity: -speed);
        AssertEqual(1, UState.Derelicts.Count, "dying just outside the system but stopping in it leaves a salvageable derelict, unseen");
        AssertTrue(Newest().CanBeSalvaged, "a salvageable one");
        UState.Derelicts.Clear(); // it would lower the chance of the deaths below

        DieAt(inside, seen: false, velocity: speed);
        AssertEqual(0, UState.Derelicts.Count, "dying just inside but stopping outside leaves nothing unseen");
        DieAt(inside, seen: true, velocity: speed);
        AssertEqual(1, UState.Derelicts.Count, "and seen, a derelict");
        AssertFalse(Newest().Salvageable, "that drifts");
    }

    [TestMethod]
    public void OutsideASystemInRadiationOrWithSalvageOpsOffADerelictDriftsAndUnseenItIsNothing()
    {
        ViewEverything();
        GlobalStats.Defaults.DerelictChance = 100;
        SolarSystem system = AddSystem();
        system.Sun = SunType.FindSun("star_neutron");
        AssertGreaterThan(system.Sun.RadiationDamage, 0f, "setup: a star that radiates");

        Vector2 deepSpace = Site(0);
        Vector2 inRadiation = SystemSite + new Vector2(system.Sun.RadiationRadius * 0.5f, 0);
        Vector2 nearRadiation = SystemSite + new Vector2(system.Sun.RadiationRadius + 1000f, 0);
        AssertGreaterThan(system.Radius, system.Sun.RadiationRadius + 1000f, "setup: the margin lies inside the system");
        AssertTrue(UState.FindClosestSystem(deepSpace).Position.Distance(deepSpace) > system.Radius, "setup: deep space");
        foreach ((Vector2 site, string where) in new[] { (deepSpace, "outside every system"), (inRadiation, "inside the star's radiation"),
                                                         (nearRadiation, "within 2000 of the star's radiation") })
        {
            int before = UState.Derelicts.Count;
            DieAt(site, seen: true);
            AssertEqual(before + 1, UState.Derelicts.Count, $"{where}, a seen death leaves a derelict");
            AssertFalse(Newest().Salvageable, $"{where}, a drifting one");
            DieAt(site, seen: false);
            AssertEqual(before + 1, UState.Derelicts.Count, $"{where}, an unseen death leaves nothing");
        }

        UState.P.DisableSalvageOps = true;
        Vector2 inSystem = SystemSite + new Vector2(30_000f, 0);
        int count = UState.Derelicts.Count;
        DieAt(inSystem, seen: true);
        AssertEqual(count + 1, UState.Derelicts.Count, "with salvage ops off, a seen death in a system leaves a derelict");
        AssertFalse(Newest().Salvageable, "a drifting one");
        DieAt(inSystem, seen: false);
        AssertEqual(count + 1, UState.Derelicts.Count, "and an unseen one nothing");
    }

    [TestMethod]
    public void SalvageableDerelictsLowerTheChanceToNoneAtTheirLimitThenDrift()
    {
        GlobalStats.Defaults.DerelictChance = 40;
        GlobalStats.Defaults.MaxSalvageableDerelicts = 4;
        GlobalStats.Defaults.MaxDriftingDerelicts = 2;
        AssertEqual(0.001f, 40f, UState.Derelicts.Chance, "with none, the full chance");

        AssertTrue(UState.Derelicts.TryAdd(SpawnShip("Corsair", Player, Site(0)), salvageable: true, seen: false), "setup: a salvageable derelict");
        AssertEqual(0.001f, 30f, UState.Derelicts.Chance, "a quarter of the limit takes a quarter of the chance");

        TestShip drifter = SpawnShip("Corsair", Player, Site(1));
        GiveSceneObject(drifter);
        AssertTrue(UState.Derelicts.TryAdd(drifter, salvageable: false, seen: true), "setup: a drifting derelict");
        AssertEqual(0.001f, 30f, UState.Derelicts.Chance, "drifting ones do not count");

        for (int i = 2; i < 5; ++i)
            UState.Derelicts.TryAdd(SpawnShip("Corsair", Player, Site(i)), salvageable: true, seen: false);
        AssertEqual(4, UState.Derelicts.NumSalvageable, "setup: the limit is reached");
        AssertEqual(0.001f, 0f, UState.Derelicts.Chance, "and no death rolls a derelict");

        AssertFalse(UState.Derelicts.TryAdd(SpawnShip("Corsair", Player, Site(5)), salvageable: true, seen: false),
                    "over the limit an unseen one is nothing");
        TestShip seen = SpawnShip("Corsair", Player, Site(6));
        GiveSceneObject(seen);
        AssertTrue(UState.Derelicts.TryAdd(seen, salvageable: true, seen: true), "and a seen one, as the salvageable ones fill no drifting place");
        AssertFalse(Newest().Salvageable, "drifts instead");
        AssertEqual(4, UState.Derelicts.NumSalvageable, "so the limit holds");
    }

    [TestMethod]
    public void AtTheLimitOfSalvageableDerelictsADeathBlastsAsUsual()
    {
        GlobalStats.Defaults.DerelictChance = 100;
        GlobalStats.Defaults.MaxSalvageableDerelicts = 1;
        UState.Derelicts.TryAdd(SpawnShip("Corsair", Player, Site(5)), salvageable: true, seen: false);
        AssertEqual(1, UState.Derelicts.NumSalvageable, "setup: the limit is reached");

        (Ship dying, ShipModule core) = StackedPair(Site(0));
        dying.Die(null, cleanupOnly: false);
        AssertFalse(dying.Active, "the ship is dead");
        AssertGreaterThan(core.ActualMaxHealth, core.Health, "it rolled no derelict, so it blasts");
    }

    [TestMethod]
    public void ASalvageableDerelictLastsAHundredTurnsTimesThePaceThenSinksAway()
    {
        UState.P.Pace = 3f;
        AssertEqual(0.001f, 2f, UState.ProductionPace, "setup: a slower pace");
        TestShip ship = SpawnShip("Dreadnought mk1-a", Player, Site(0));
        UState.Derelicts.TryAdd(ship, salvageable: true, seen: false);
        Derelict d = Newest();
        AssertEqual(200, d.TurnsLeft, "100 turns times the production pace");

        for (int turn = 1; turn < 200; ++turn)
            UState.Derelicts.UpdateTurn();
        UState.Derelicts.Update(TestSimStep);
        AssertEqual(1, d.TurnsLeft, "one turn later");
        AssertTrue(d.CanBeSalvaged, "it is still there to salvage");
        AssertEqual(0.001f, 0f, d.Z, "on the plane");

        AssertEqual(0.001f, GlobalStats.Defaults.DerelictChance * (1f - 1f / GlobalStats.Defaults.MaxSalvageableDerelicts),
                    UState.Derelicts.Chance, "setup: it lowers the chance");
        UState.Derelicts.UpdateTurn();
        AssertTrue(d.Sinking, "its time is up");
        AssertFalse(d.CanBeSalvaged, "and it can no longer be salvaged");
        AssertEqual(0.001f, GlobalStats.Defaults.DerelictChance, UState.Derelicts.Chance, "nor lowers the chance");
        UState.Derelicts.Update(TestSimStep);
        AssertEqual(0, UState.Derelicts.Count, "with no model, it is gone at once");

        TestShip seen = SpawnShip("Dreadnought mk1-a", Player, Site(1));
        GiveSceneObject(seen);
        UState.Derelicts.TryAdd(seen, salvageable: true, seen: true);
        d = Newest();
        while (d.TurnsLeft > 0)
            UState.Derelicts.UpdateTurn();
        float seconds = 0f;
        while (UState.Derelicts.Count > 0)
        {
            UState.Derelicts.Update(TestSimStep);
            seconds += TestSimStep.FixedTime;
            Assert.IsTrue(seconds < 200f, "the derelict never left");
        }
        AssertEqual(0.5f, Derelict.RemoveDepth / Derelict.SinkSpeed, seconds, "with a model, it sinks away like a drifting one");
    }

    [TestMethod]
    public void EmpiresKnowADerelictFromItsDeathOrTheirSensorsAndKeepKnowingIt()
    {
        TestShip ship = SpawnShip("Dreadnought mk1-a", Player, Site(0));
        ship.KnownByEmpires.SetSeen(Player);
        UState.Derelicts.TryAdd(ship, salvageable: true, seen: false);
        Derelict d = Newest();
        AssertTrue(d.IsKnownBy(Player), "an empire that had the ship in its sensors at its death knows of it");
        AssertFalse(d.IsKnownBy(Enemy), "one that did not, does not");

        Enemy.SensorNodes = new[] { new Empire.InfluenceNode { Position = d.Position + new Vector2(11_000f, 0), Radius = 10_000f } };
        UState.Derelicts.UpdateTurn();
        AssertFalse(d.IsKnownBy(Enemy), "a sensor that does not reach it tells nothing");

        Enemy.SensorNodes = new[] { new Empire.InfluenceNode { Position = d.Position + new Vector2(9000f, 0), Radius = 10_000f } };
        UState.Derelicts.UpdateTurn();
        AssertTrue(d.IsKnownBy(Enemy), "a sensor over it on a turn tells an empire of it");

        Enemy.SensorNodes = Empty<Empire.InfluenceNode>.Array;
        UState.Derelicts.UpdateTurn();
        AssertTrue(d.IsKnownBy(Enemy), "and it stays known");
    }

    static Empire.InfluenceNode[] SensorOver(Derelict d) => new[] { new Empire.InfluenceNode { Position = d.Position, Radius = 10_000f } };

    // a death in the system, with more than six modules dead so it smokes from more than six places
    Derelict SalvageableWithSmoke(bool seen = false, Vector2 velocity = default)
    {
        GlobalStats.Defaults.DerelictChance = 100;
        AddSystem();
        Ship ship = SpawnPlainShip("Dreadnought mk1-a", SystemSite + new Vector2(30_000f, 0));
        UState.Objects.Update(TestSimStep); // sets its top speed
        foreach (ShipModule m in ship.Modules.Where(m => m.CanVisualizeDamage && !m.HasInternalRestrictions
                                                         && m.ModuleType != ShipModuleType.PowerConduit).Take(10))
            m.SetHealth(0f, "Test");
        AssertTrue(ship.Active, "setup: the ship lives on without them");
        if (seen)
        {
            GiveSceneObject(ship);
            MakeVisible(ship);
        }
        ship.Velocity = velocity;
        ship.Die(null, cleanupOnly: false);
        UState.Objects.Update(TestSimStep);
        Derelict d = Newest();
        AssertTrue(d.CanBeSalvaged, "setup: a salvageable derelict");
        AssertGreaterThan(d.NumSmokeSources, Derelict.MinSmokeSources, "setup: it smokes from more than six places");
        return d;
    }

    static Derelict.SmokeSource[] SmokeOf(Derelict d)
    {
        return (Derelict.SmokeSource[])typeof(Derelict).GetField("Smoke", BindingFlags.NonPublic | BindingFlags.Instance)!.GetValue(d);
    }

    UniverseScreen SaveAndLoad()
    {
        UState.StarDate = 1001f; // the loader needs a stardate past 1000
        SavedGame save = Universe.Save("UnitTest.Derelicts", throwOnError: true);
        UniverseScreen loaded = LoadGame.Load(save.SaveFile, noErrorDialogs: true, startSimThread: false);
        AssertTrue(loaded != null, "setup: the save loads");
        return loaded;
    }

    [TestMethod]
    public void ASaveKeepsASalvageableDerelictWithItsStateKnowledgeAndLastSixSmokePoints()
    {
        ViewEverything();
        CreateThirdMajorEmpire();
        Derelict d = SalvageableWithSmoke(seen: true, velocity: new Vector2(80f, 0));
        for (int i = 0; i < 60; ++i)
            UState.Derelicts.Update(TestSimStep);
        AssertGreaterThan(d.VelocityNow.Length(), 0f, "setup: it is still slowing to a stop");
        Enemy.SensorNodes = SensorOver(d);
        for (int turn = 0; turn < 3; ++turn)
            UState.Derelicts.UpdateTurn();
        AssertTrue(d.IsKnownBy(Enemy) && d.IsKnownBy(Player), "setup: the enemy and the player know of it");
        AssertFalse(d.IsKnownBy(ThirdMajor), "setup: the third empire does not");

        Ship drifter = SpawnPlainShip("Dreadnought mk1-a", Site(0));
        UState.Objects.Update(TestSimStep);
        GiveSceneObject(drifter);
        MakeVisible(drifter);
        drifter.DebugDieAsDerelict(salvageable: false);
        UState.Objects.Update(TestSimStep);
        AssertEqual(2, UState.Derelicts.Count, "setup: and a drifting derelict");

        UniverseScreen loaded = SaveAndLoad();
        Derelict[] after = loaded.UState.Derelicts.ToArray();
        AssertEqual(1, after.Length, "the salvageable derelict comes back; the drifting one was only a visual");
        Derelict back = after[0];
        AssertTrue(back.CanBeSalvaged, "still salvageable");
        AssertEqual(0.01f, d.Position, back.Position, "where it was");
        AssertEqual(0.001f, d.Age, back.Age, "as old");
        AssertEqual(0.01f, d.VelocityNow, back.VelocityNow, "slowing down as it was");
        AssertEqual(0.001f, new Vector3(d.RotationX, d.RotationY, d.RotationZ),
                    new Vector3(back.RotationX, back.RotationY, back.RotationZ), "turned as it was");
        AssertEqual(0.01f, d.Radius, back.Radius, "as big");
        AssertEqual(d.TurnsLeft, back.TurnsLeft, "with the turns it had left");
        Assert.AreSame(ResourceManager.Ships.GetDesign(d.Design.Name), back.Design, "of the loaded game's own design");
        AssertEqual(Player.Id, back.Owner.Id, "and owner");
        AssertTrue(back.IsKnownBy(loaded.UState.GetEmpireById(Enemy.Id)), "the enemy still knows of it");
        AssertTrue(back.IsKnownBy(loaded.UState.Player), "and so does the player");
        AssertFalse(back.IsKnownBy(loaded.UState.GetEmpireById(ThirdMajor.Id)), "the third empire still does not");
        AssertEqual(Derelict.MinSmokeSources, back.NumSmokeSources, "it smokes from six places");
        Derelict.SmokeSource[] before = SmokeOf(d), loadedSmoke = SmokeOf(back);
        for (int i = 0; i < loadedSmoke.Length; ++i)
            AssertEqual(0.001f, before[i].Local, loadedSmoke[i].Local, $"the ones left after the thin-out: point {i}");
        AssertFalse(back.InScene, "with no model until the player sees it");
    }

    [TestMethod]
    public void UnderAModWithoutSalvageContentASavedSalvageableDerelictIsGone()
    {
        SalvageableWithSmoke();
        AssertEqual(1, UState.Derelicts.GetSaves().Length, "setup: the save keeps it");
        GlobalStats.Defaults.EnableSalvageOps = false;
        UniverseScreen loaded = SaveAndLoad();
        AssertEqual(0, loaded.UState.Derelicts.Count, "the mod's switch wins, and with no model it would sink away at once");
    }

    [TestMethod]
    public void AContentReloadKeepsTheSalvageableDerelictsWithoutTheirModels()
    {
        ViewEverything();
        Derelict salvageable = SalvageableWithSmoke(seen: true);
        Ship drifter = SpawnPlainShip("Dreadnought mk1-a", Site(0));
        UState.Objects.Update(TestSimStep);
        GiveSceneObject(drifter);
        MakeVisible(drifter);
        drifter.DebugDieAsDerelict(salvageable: false);
        UState.Objects.Update(TestSimStep);
        Derelict drifting = Newest();
        AssertTrue(salvageable.InScene && drifting.InScene, "setup: both have models");

        UState.RemoveSceneObjects(); // as the options screen's Apply reloads the content
        Derelict[] after = UState.Derelicts.ToArray();
        AssertEqual(1, after.Length, "only the salvageable one stays");
        AssertTrue(after[0] == salvageable, "the same derelict, with its turns and knowledge");
        AssertFalse(salvageable.InScene, "its model is gone");
        AssertTrue(salvageable.WantsModel, "and it gets one again once on screen");
    }

    [TestMethod]
    public void ADerelictGetsAModelOnlyOnceThePlayerKnowsOfItAndSeesIt()
    {
        ViewEverything();
        TestShip ship = SpawnShip("Dreadnought mk1-a", Enemy, Site(0));
        UState.Derelicts.TryAdd(ship, salvageable: true, seen: false);
        Derelict d = Newest();
        AssertFalse(d.IsKnownBy(Player), "setup: the player does not know of it");
        AssertFalse(d.WantsModel, "a wreck the player does not know of gets no model");

        Player.SensorNodes = SensorOver(d);
        UState.Derelicts.UpdateTurn();
        AssertTrue(d.WantsModel, "once known, on screen at planet view, it wants one");
        AssertEqual(1, UState.Derelicts.WantingModels().Length, "and the list asks for it");
        UState.ViewState = UniverseScreen.UnivScreenState.SectorView;
        AssertFalse(d.WantsModel, "zoomed out it can wait");
        UState.ViewState = UniverseScreen.UnivScreenState.PlanetView;

        var so = new SceneObject("derelict");
        TakeSceneChanges();
        AssertTrue(UState.Derelicts.TryAttachModel(d, so), "the loaded model is attached");
        AssertTrue(d.InScene, "it is in the scene");
        AssertTrue(TakeSceneChanges().Any(c => c.Add && c.Item == so), "added to it");
        AssertEqual(0.001f, 0f, so.EmissiveScale, "with its lights out");
        AssertFalse(d.WantsModel, "and keeps it");
        AssertFalse(UState.Derelicts.TryAttachModel(d, new SceneObject("second")), "it never takes a second one");

        TestShip late = SpawnShip("Dreadnought mk1-a", Enemy, Site(2));
        AssertTrue(UState.Derelicts.TryAdd(late, salvageable: true, seen: false), "setup: another salvageable derelict");
        Derelict expired = Newest();
        while (expired.TurnsLeft > 0)
            UState.Derelicts.UpdateTurn();
        AssertFalse(UState.Derelicts.TryAttachModel(expired, new SceneObject("expired")), "nor does one whose turns ran out while it loaded");

        TestShip other = SpawnShip("Dreadnought mk1-a", Enemy, Site(1));
        UState.Derelicts.TryAdd(other, salvageable: true, seen: false);
        Derelict gone = Newest();
        UState.Derelicts.Clear();
        AssertFalse(UState.Derelicts.TryAttachModel(gone, new SceneObject("late")), "one that left while its model loaded gets none");
    }
}
