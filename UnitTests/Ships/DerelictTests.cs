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
using UnitTests.UI;
using Matrix = SDGraphics.Matrix;
using Vector2 = SDGraphics.Vector2;
using BoundingFrustum = Microsoft.Xna.Framework.BoundingFrustum;
#pragma warning disable CA2213

namespace UnitTests.Ships;

/// <summary>
/// Some deaths of frigates and bigger military ships, and of stations on their own, leave a derelict: no main
/// explosion and no death blast. On screen it breaks up and drifts to a stop with its lights out, tumbling and smoking,
/// then sinks away. One that stops in a system outside the star's radiation, with salvage ops on, is salvageable
/// unless its hull's roll makes it drift: it stays there, seen or not, until its turns run out. A drifting one is left
/// seen or not, but outside every system only if the player owned it, destroyed it or had it in its own sensors; past
/// the cap of drifting derelicts a seen death takes the place of the oldest one with no model, and with none it leaves
/// nothing, still with no blast.
/// </summary>
[TestClass]
public class DerelictTests : StarDriveTest
{
    readonly float DerelictChance = GlobalStats.Defaults.DerelictChance;
    readonly float DriftingDerelictCapMultiplier = GlobalStats.Defaults.DriftingDerelictCapMultiplier;
    readonly float SalvageableDerelictCapMultiplier = GlobalStats.Defaults.SalvageableDerelictCapMultiplier;
    readonly bool EnableSalvageOps = GlobalStats.Defaults.EnableSalvageOps;

    public DerelictTests()
    {
        LoadStarterShips("Dreadnought mk1-a", "Corsair", "Hunter mk1-a", "Vulcan Scout", "Seeder Transport", "Shipyard",
                         "Platform Base mk1-a", "Subspace Projector", "Cordrazine Small Station Mk1",
                         "C-Research Station mk1-a", "C-Mining Station mk1-a", "Cordrazine Basic Salvage Station");
        CreateUniverseAndPlayerEmpire();
        UState.Derelicts.TestDriftRoll = false; // a death drifts by its hull's roll only in the tests of that roll
    }

    [TestCleanup]
    public override void Cleanup()
    {
        GlobalStats.Defaults.DerelictChance = DerelictChance;
        GlobalStats.Defaults.DriftingDerelictCapMultiplier = DriftingDerelictCapMultiplier;
        GlobalStats.Defaults.SalvageableDerelictCapMultiplier = SalvageableDerelictCapMultiplier;
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
        TestShip salvage = SpawnShip("Cordrazine Basic Salvage Station", Player, Site(13));
        AssertTrue(research.IsResearchStation && mining.IsMiningStation && salvage.IsSalvageStation,
                   "setup: a research, a mining and a salvage station");
        AssertFalse(research.CanBecomeDerelict, "a research station");
        AssertFalse(mining.CanBecomeDerelict, "a mining station");
        AssertFalse(salvage.CanBecomeDerelict, "a salvage station");

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
        AssertEqual(1, UState.Derelicts.Count, "unseen outside every system, a wreck of the player's own still drifts");
        AssertFalse(Newest().InScene, "with no model, as nobody saw it die");
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
        AddRemoteSystems(9);
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
        AssertEqual(2, UState.Derelicts.Count, "unseen, it leaves one too");
        AssertFalse(Newest().Salvageable || Newest().InScene, "drifting, with no model");

        Ship salvaged = SpawnPlainShip("Dreadnought mk1-a", inSystem);
        salvaged.DebugDieAsDerelict(salvageable: true);
        AssertFalse(salvaged.Active, "Ctrl+Shift+X destroys a ship in a system");
        UState.Objects.Update(TestSimStep);
        AssertEqual(3, UState.Derelicts.Count, "and leaves a derelict, seen or not");
        AssertTrue(Newest().CanBeSalvaged, "a salvageable one");

        Ship deepSpace = SpawnPlainShip("Dreadnought mk1-a", Site(0));
        deepSpace.DebugDieAsDerelict(salvageable: true);
        AssertTrue(deepSpace.Active, "outside every system it cannot be salvageable, so nothing happens to it");

        GlobalStats.Defaults.SalvageableDerelictCapMultiplier = 0.1f; // of its 10 systems, one salvageable place, and the drifting cap stays at 10
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
        GlobalStats.Defaults.DriftingDerelictCapMultiplier = 0.1f; // a cap of 1 in a galaxy of up to 10 systems
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
    public void TheDriftingCapCountsEveryWreckPastSalvageAndASeenDeathTakesThePlaceOfTheOldestDriftingOneWithNoModel()
    {
        GlobalStats.Defaults.DriftingDerelictCapMultiplier = 0.1f; // a cap of 1 in a galaxy of up to 10 systems
        UState.Derelicts.TryAdd(SpawnShip("Dreadnought mk1-a", Player, Site(0)), salvageable: true, seen: false);
        AssertTrue(Newest().CanBeSalvaged, "setup: a salvageable one");

        AssertTrue(UState.Derelicts.TryAdd(SpawnShip("Dreadnought mk1-a", Player, Site(1)), salvageable: false, seen: false),
                   "it takes no drifting place, so an unseen wreck drifts");
        Derelict oldest = Newest();
        AssertFalse(oldest.InScene, "setup: with no model");
        AssertFalse(UState.Derelicts.TryAdd(SpawnShip("Dreadnought mk1-a", Player, Site(2)), salvageable: false, seen: false),
                    "one with no model fills the cap for another unseen one");

        TestShip seen = SpawnShip("Dreadnought mk1-a", Player, Site(3));
        GiveSceneObject(seen);
        AssertTrue(UState.Derelicts.TryAdd(seen, salvageable: false, seen: true), "a seen one takes its place");
        Derelict[] now = UState.Derelicts.ToArray();
        AssertEqual(-1, Array.IndexOf(now, oldest), "the one with no model is gone");
        AssertEqual(2, now.Length, "and the salvageable one stays");
        TestShip another = SpawnShip("Dreadnought mk1-a", Player, Site(4));
        GiveSceneObject(another);
        AssertFalse(UState.Derelicts.TryAdd(another, salvageable: false, seen: true), "with the drifting place held by a model, a seen one finds none");

        UState.Derelicts.Clear();
        UState.Derelicts.TryAdd(SpawnShip("Dreadnought mk1-a", Player, Site(5)), salvageable: true, seen: false);
        while (Newest().TurnsLeft > 0)
            UState.Derelicts.UpdateTurn();
        AssertTrue(Newest().Sinking && !Newest().InScene, "setup: a salvageable one sinking with no model");
        AssertFalse(UState.Derelicts.TryAdd(SpawnShip("Dreadnought mk1-a", Player, Site(6)), salvageable: false, seen: false),
                    "it takes the drifting place too");
        TestShip seenLast = SpawnShip("Dreadnought mk1-a", Player, Site(7));
        GiveSceneObject(seenLast);
        AssertFalse(UState.Derelicts.TryAdd(seenLast, salvageable: false, seen: true), "and only a drifting one gives its place up to a seen death");
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
    public void ADriftingDerelictSlowsToAStopOverTenSecondsAndSinksAwayAfterItsBreakup()
    {
        TestShip ship = SpawnShip("Dreadnought mk1-a", Player, Site(0));
        UState.Objects.Update(TestSimStep);
        AssertGreaterThan(ship.MaxSTLSpeed, 0f, "setup: the ship has a top sublight speed");
        ship.Velocity = new Vector2(ship.MaxSTLSpeed * 10f, 0f); // it died at warp
        Derelict d = Drifting(ship, new SceneObject(ship.Name));
        float startX = d.Position.X;

        d.Update(FixedSimTime.Zero);
        AssertEqual(0.001f, startX, d.Position.X, "paused, it stays put");
        AssertEqual(0.01f, ship.MaxSTLSpeed, d.VelocityNow.Length(), "it drifts at top sublight speed at most");
        AssertEqual(1f, 0f, d.Z, "on the plane");

        Step(d, 5f);
        AssertEqual(ship.MaxSTLSpeed * 0.02f, ship.MaxSTLSpeed * 0.5f, d.VelocityNow.Length(), "at half that 5 seconds on");
        Step(d, 5f);
        float stop = ship.MaxSTLSpeed * 10f * 0.5f;
        AssertEqual(stop * 0.01f, stop, d.Position.X - startX, "10 seconds on it has stopped, half its speed times 10 away");
        AssertEqual(0.001f, 0f, d.VelocityNow.Length(), "and moves no more");
        Step(d, 5f);
        AssertEqual(stop * 0.01f, stop, d.Position.X - startX, "where it stays");
        AssertEqual(2f, (15f - d.BreakupSeconds) * Derelict.SinkSpeed, d.Z, "sinking at 50 a second since its breakup");

        float seconds = 15f;
        while (d.Update(TestSimStep))
        {
            seconds += TestSimStep.FixedTime;
            Assert.IsTrue(seconds < 200f, "the derelict never left");
        }
        AssertEqual(0.5f, d.BreakupSeconds + Derelict.RemoveDepth / Derelict.SinkSpeed, seconds,
                    "it leaves at depth 5000, 100 s after its breakup");
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
        AssertEqual(1f, Math.Min(25f * Derelict.StopSeconds, d.RestDepth), d.Z, "settling meanwhile at 25 a second");

        float stopX = d.Position.X;
        Step(d, 30f);
        AssertEqual(0.001f, stopX, d.Position.X, "it stays stopped");
        AssertEqual(0.001f, Math.Min(d.Radius, 500f), d.RestDepth, "its rest depth is its radius, at most 500");
        AssertEqual(1f, d.RestDepth, d.Z, "there it stays, a little below the plane, not sinking away");
        AssertEqual(0.001f, 1f, d.Scale, "and at its full size");
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
        AddRemoteSystems(10);
        TestShip noHull = SpawnShip("Dreadnought mk1-a", Player, Site(0));
        AssertTrue(UState.Derelicts.TryAdd(noHull, salvageable: false, seen: true), "seen with no scene object to hand over, it drifts with no model");
        AssertFalse(Newest().InScene, "setup: with no model");

        TestShip ship = SpawnShip("Dreadnought mk1-a", Player, Site(1));
        GiveSceneObject(ship);
        AssertTrue(UState.Derelicts.TryAdd(ship, salvageable: false, seen: true), "setup: a derelict");
        AssertFalse(ship.HasSO, "it took the hull");
        float lifetime = Derelict.MaxBreakupSeconds + Derelict.RemoveDepth / Derelict.SinkSpeed;
        for (int i = 0; i < (int)((lifetime + 1f) / TestSimStepD); ++i)
            UState.Derelicts.Update(TestSimStep);
        AssertEqual(0, UState.Derelicts.Count, "both sank away and left the list");

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

    SolarSystem AddSystem() => AddSystem(SystemSite);

    SolarSystem AddSystem(Vector2 site)
    {
        Planet planet = AddDummyPlanet(site, 1f, 1f, 1f, site + new Vector2(0, 60_000f), explored: true);
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
        AssertEqual(GlobalStats.VanillaDefaults.SalvageableDerelictCapMultiplier, mod.SalvageableDerelictCapMultiplier, "as it does the other vanilla values");

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
        AddRemoteSystems(10);
        var speed = new Vector2(80f, 0);
        float drift = speed.X * Derelict.StopSeconds / 2f;
        Vector2 outside = SystemSite + new Vector2(system.Radius + drift / 2f, 0);
        Vector2 inside = SystemSite + new Vector2(system.Radius - drift / 2f, 0);

        DieAt(outside, seen: false, velocity: -speed);
        AssertEqual(1, UState.Derelicts.Count, "dying just outside the system but stopping in it leaves a salvageable derelict, unseen");
        AssertTrue(Newest().CanBeSalvaged, "a salvageable one");
        UState.Derelicts.Clear(); // it would lower the chance of the deaths below

        DieAt(inside, seen: false, velocity: speed);
        AssertEqual(1, UState.Derelicts.Count, "dying just inside but stopping outside, a wreck of the player's own drifts");
        AssertFalse(Newest().Salvageable, "unseen");
        DieAt(inside, seen: true, velocity: speed);
        AssertEqual(2, UState.Derelicts.Count, "and seen");
        AssertFalse(Newest().Salvageable, "too");
    }

    [TestMethod]
    public void OutsideASystemInRadiationOrWithSalvageOpsOffADerelictDriftsSeenOrNot()
    {
        AddRemoteSystems(10);
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
            AssertEqual(before + 2, UState.Derelicts.Count, $"{where}, so does an unseen one of the player's own");
            AssertFalse(Newest().Salvageable || Newest().InScene, $"{where}, drifting with no model");
        }

        UState.P.DisableSalvageOps = true;
        UState.Derelicts.TestDriftRoll = true;
        Vector2 inSystem = SystemSite + new Vector2(30_000f, 0);
        int count = UState.Derelicts.Count;
        DieAt(inSystem, seen: true);
        AssertEqual(count + 1, UState.Derelicts.Count, "with salvage ops off, a seen death in a system that rolls drifting leaves a derelict");
        AssertFalse(Newest().Salvageable, "a drifting one");
        DieAt(inSystem, seen: false);
        AssertEqual(count + 2, UState.Derelicts.Count, "and so does an unseen one");
    }

    [TestMethod]
    public void SalvageableDerelictsLowerTheChanceToNoneAtTheirLimitThenDrift()
    {
        for (int i = 0; i < 4; ++i)
            AddDummyPlanet(new Vector2(-400_000f + i * 100_000f, -400_000f));
        AssertEqual(0.0001f, 1f, GlobalStats.Defaults.SalvageableDerelictCapMultiplier, "setup: the shipped default, which leaves one place per system");
        GlobalStats.Defaults.DerelictChance = 40;
        GlobalStats.Defaults.DriftingDerelictCapMultiplier = 0.75f;
        AssertEqual(0.001f, 40f, UState.Derelicts.Chance(UState), "with none, the full chance");

        AssertTrue(UState.Derelicts.TryAdd(SpawnShip("Corsair", Player, Site(0)), salvageable: true, seen: false), "setup: a salvageable derelict");
        AssertEqual(0.001f, 30f, UState.Derelicts.Chance(UState), "a quarter of the limit takes a quarter of the chance");

        TestShip drifter = SpawnShip("Corsair", Player, Site(1));
        GiveSceneObject(drifter);
        AssertTrue(UState.Derelicts.TryAdd(drifter, salvageable: false, seen: true), "setup: a drifting derelict");
        AssertEqual(0.001f, 30f, UState.Derelicts.Chance(UState), "drifting ones do not count");

        for (int i = 2; i < 5; ++i)
            UState.Derelicts.TryAdd(SpawnShip("Corsair", Player, Site(i)), salvageable: true, seen: false);
        AssertEqual(4, UState.Derelicts.NumSalvageable, "setup: the limit is reached");
        AssertEqual(0.001f, 0f, UState.Derelicts.Chance(UState), "and no death rolls a derelict");

        AssertTrue(UState.Derelicts.TryAdd(SpawnShip("Corsair", Player, Site(5)), salvageable: true, seen: false),
                   "over the limit an unseen one drifts instead");
        AssertFalse(Newest().Salvageable, "setup: drifting");
        TestShip seen = SpawnShip("Corsair", Player, Site(6));
        GiveSceneObject(seen);
        AssertTrue(UState.Derelicts.TryAdd(seen, salvageable: true, seen: true), "and a seen one, as the salvageable ones fill no drifting place");
        AssertFalse(Newest().Salvageable, "drifts instead");
        AssertEqual(4, UState.Derelicts.NumSalvageable, "so the limit holds");
    }

    [TestMethod]
    public void AFrigateDriftsThreeTimesInFourAndACapitalOnceInTen()
    {
        AssertEqual(0.001f, 75f, Derelict.DriftingChance(RoleName.frigate), "a frigate");
        AssertEqual(0.001f, 70f, Derelict.DriftingChance(RoleName.destroyer), "a destroyer, as any hull role not named");
        AssertEqual(0.001f, 50f, Derelict.DriftingChance(RoleName.cruiser), "a cruiser");
        AssertEqual(0.001f, 25f, Derelict.DriftingChance(RoleName.battleship), "a battleship");
        AssertEqual(0.001f, 10f, Derelict.DriftingChance(RoleName.capital), "a capital");
        AssertEqual(0.001f, 70f, Derelict.DriftingChance(RoleName.station), "an untethered station");

        UState.Derelicts.TestDriftRoll = null;
        var random = new SeededRandom(7);
        int frigates = 0, capitals = 0;
        for (int i = 0; i < 1000; ++i)
        {
            if (UState.Derelicts.RollsDrifting(RoleName.frigate, random))
                ++frigates;
            if (UState.Derelicts.RollsDrifting(RoleName.capital, random))
                ++capitals;
        }
        AssertTrue(frigates is > 700 and < 800, $"the roll draws at the hull's chance: {frigates} frigates of 1000");
        AssertTrue(capitals is > 60 and < 140, $"and {capitals} capitals");
    }

    [TestMethod]
    public void ADeathWhoseHullRollsDriftingDriftsEvenWhereItCouldBeSalvaged()
    {
        ViewEverything();
        GlobalStats.Defaults.DerelictChance = 100;
        SolarSystem system = AddSystem();
        AddRemoteSystems(10);
        Vector2 inSystem = SystemSite + new Vector2(30_000f, 0);
        DieAt(inSystem, seen: false);
        AssertTrue(Newest().CanBeSalvaged, "control: with the hull's roll against drifting, it lies to be salvaged");

        UState.Derelicts.TestDriftRoll = true;
        GlobalStats.Defaults.DerelictChance = 200; // the first one's share lowers the chance below 100, so this keeps the death's roll certain
        DieAt(inSystem, seen: false);
        AssertEqual(2, UState.Derelicts.Count, "setup: the second death leaves a derelict too");
        Derelict d = Newest();
        AssertFalse(d.Salvageable, "with it for drifting, it drifts, unseen too");
        Assert.AreSame(system, d.System, "in the system it lies in");
        AssertEqual(1, UState.Derelicts.NumSalvageable, "and only the first lowers the chance");
    }

    [TestMethod]
    public void WithSalvageOpsOffOnlyADeathWhoseHullRollsDriftingLeavesADerelict()
    {
        GlobalStats.Defaults.DerelictChance = 100;
        UState.P.DisableSalvageOps = true;
        (Ship ordinary, ShipModule core) = StackedPair(Site(0));
        ordinary.Die(null, cleanupOnly: false);
        AssertFalse(ordinary.Active, "setup: the ship is dead");
        AssertGreaterThan(core.ActualMaxHealth, core.Health, "with the hull's roll against drifting, it dies as usual, with a blast");
        UState.Objects.Update(TestSimStep);
        AssertEqual(0, UState.Derelicts.Count, "and leaves no derelict");

        UState.Derelicts.TestDriftRoll = true;
        (Ship drifter, ShipModule drifterCore) = StackedPair(Site(1));
        drifter.Die(null, cleanupOnly: false);
        AssertEqual(0.001f, drifterCore.ActualMaxHealth, drifterCore.Health, "with it for drifting, no blast");
        UState.Objects.Update(TestSimStep);
        AssertEqual(1, UState.Derelicts.Count, "and a derelict");
        AssertFalse(Newest().Salvageable, "a drifting one");
    }

    [TestMethod]
    public void OutsideEverySystemADriftingWreckIsLeftOnlyIfThePlayerOwnedItDestroyedItOrHadItInItsOwnSensorRange()
    {
        AddRemoteSystems(10);
        CreateThirdMajorEmpire();
        Player.SignAllianceWith(ThirdMajor);
        AssertTrue(Player.IsAlliedWith(ThirdMajor), "setup: an ally");
        Vector2 allysWreck = new(200_000f, 400_000f), allysSight = new(200_000f, 600_000f), playersSight = new(200_000f, 800_000f);
        TestShip allys = SpawnShip("Dreadnought mk1-a", ThirdMajor, allysWreck);
        SpawnShip("Vulcan Scout", ThirdMajor, allysSight);
        SpawnShip("Vulcan Scout", Player, playersSight);
        UState.Objects.Update(new FixedSimTime(1f)); // the sensors' nodes
        UState.Objects.Update(TestSimStep); // and the ally's ships shown to the player
        AssertTrue(allys.KnownByEmpires.KnownBy(Player), "setup: the game shows the player its ally's ships");

        AssertFalse(UState.Derelicts.TryAdd(SpawnShip("Dreadnought mk1-a", Enemy, Site(0)), salvageable: false, seen: false),
                    "a stranger's wreck the player did not destroy or sense is nothing");
        AssertFalse(UState.Derelicts.TryAdd(allys, salvageable: false, seen: false), "nor does an ally's count, though the player is shown it");
        TestShip allysKill = SpawnShip("Dreadnought mk1-a", Enemy, allysSight + new Vector2(5_000f, 0));
        allysKill.KnownByEmpires.SetSeen(Player); // as the game marks what an ally's ships scan
        AssertFalse(UState.Derelicts.TryAdd(allysKill, salvageable: false, seen: false, ThirdMajor),
                    "nor one an ally destroyed in its own sensors");
        AssertEqual(0, UState.Derelicts.Count, "setup: none left");

        AssertTrue(UState.Derelicts.TryAdd(SpawnShip("Dreadnought mk1-a", Player, Site(3)), salvageable: false, seen: false),
                   "the player's own is left");
        AssertTrue(Newest().KnownByPlayer, "known to the player");
        AssertTrue(UState.Derelicts.TryAdd(SpawnShip("Dreadnought mk1-a", Enemy, Site(4)), salvageable: false, seen: false, Player),
                   "so is one the player destroyed");
        AssertTrue(Newest().KnownByPlayer, "known to the player too");
        TestShip sensed = SpawnShip("Dreadnought mk1-a", Enemy, playersSight + new Vector2(5_000f, 0));
        AssertTrue(UState.Derelicts.TryAdd(sensed, salvageable: false, seen: false), "and one in the sensor range of the player's own ships");
        AssertTrue(Newest().KnownByPlayer, "known to the player as well");
    }

    [TestMethod]
    public void TheOldestDriftingWreckWithNoModelGivesUpItsPlaceToASeenDeath()
    {
        AddRemoteSystems(2); // a drifting cap of 2
        AssertTrue(UState.Derelicts.TryAdd(SpawnShip("Dreadnought mk1-a", Player, Site(0)), salvageable: false, seen: false),
                   "setup: an unseen drifting wreck");
        Derelict oldest = Newest();
        AssertTrue(UState.Derelicts.TryAdd(SpawnShip("Dreadnought mk1-a", Player, Site(1)), salvageable: false, seen: false),
                   "setup: and a newer one, which fills the cap");
        Derelict newer = Newest();
        TestShip seen = SpawnShip("Dreadnought mk1-a", Player, Site(2));
        GiveSceneObject(seen);
        AssertTrue(UState.Derelicts.TryAdd(seen, salvageable: false, seen: true), "a seen death takes a place");
        Derelict[] now = UState.Derelicts.ToArray();
        AssertEqual(-1, Array.IndexOf(now, oldest), "the oldest's");
        AssertGreaterThan(Array.IndexOf(now, newer), -1, "while the newer one stays");
    }

    [TestMethod]
    public void ASystemThePlayerListsShowsItTheDriftingWrecksThere()
    {
        SolarSystem system = AddSystem();
        AddRemoteSystems(10);
        Vector2 inSystem = SystemSite + new Vector2(30_000f, 0);
        AssertTrue(UState.Derelicts.TryAdd(SpawnShip("Dreadnought mk1-a", Enemy, inSystem), salvageable: false, seen: false),
                   "setup: a stranger's drifting wreck there");
        Universe.InvokePendingSimThreadActions();
        Derelict drifting = Newest();
        AssertFalse(drifting.KnownByPlayer, "setup: unknown to the player");

        Forms(Enemy, inSystem + new Vector2(10_000f, 0), seenByPlayer: true);
        AssertTrue(Player.DerelictSystems.Contains(system), "setup: the player learns of a salvageable one there and lists the system");
        AssertTrue(drifting.KnownByPlayer, "so it knows of the drifting one too");

        AssertTrue(UState.Derelicts.TryAdd(SpawnShip("Dreadnought mk1-a", Enemy, inSystem - new Vector2(10_000f, 0)), salvageable: false, seen: false),
                   "setup: another drifting wreck there");
        Universe.InvokePendingSimThreadActions();
        AssertTrue(Newest().KnownByPlayer, "and of one that forms there while it lists the system");
    }

    [TestMethod]
    public void AnUnseenDriftingWreckInASystemSinksOnForThePlayerToComeUponAndGetsItsModelOnceItKnowsOfIt()
    {
        ViewEverything();
        SolarSystem system = AddSystem();
        AddRemoteSystems(10);
        Vector2 inSystem = SystemSite + new Vector2(30_000f, 0);
        TestShip stranger = SpawnShip("Dreadnought mk1-a", Enemy, inSystem);
        stranger.Velocity = new Vector2(80f, 0);
        AssertTrue(UState.Derelicts.TryAdd(stranger, salvageable: false, seen: false), "a stranger's unseen wreck in a system is left");
        Universe.InvokePendingSimThreadActions();
        Derelict d = Newest();
        Assert.AreSame(system, d.System, "setup: in the system");
        AssertEqual(1f, inSystem, d.Position, "where the ship died, not where it will stop");
        AssertEqual(0.01f, 80f, d.VelocityNow.Length(), "going on from there as a seen one would");
        AssertEqual(0.001f, 0f, d.Z, "on the plane");
        AssertFalse(d.KnownByPlayer, "with nothing of the player's there, the player does not know of it");
        AssertFalse(d.WantsModel, "so it gets no model on screen");

        StepAll(UState.Derelicts, d.BreakupSeconds + 20f);
        AssertTrue(d.Sinking, "setup: sinking");
        AssertEqual(1, UState.Derelicts.Count, "unknown and with no model, it sinks on");

        SpawnShip("Vulcan Scout", Player, inSystem + new Vector2(10_000f, 0));
        UpdateObjectsTwice();
        AssertTrue(d.KnownByPlayer, "a ship of the player's entering the system shows it the wreck");
        AssertTrue(d.WantsModel, "which gets its model on screen");

        AssertTrue(UState.Derelicts.TryAdd(SpawnShip("Dreadnought mk1-a", Enemy, inSystem), salvageable: false, seen: false),
                   "setup: another one there");
        Universe.InvokePendingSimThreadActions();
        AssertTrue(Newest().KnownByPlayer, "with the player's ship there, the player knows of it as it forms");
    }

    [TestMethod]
    public void EachCapIsTheGalaxysStarSystemsTimesItsMultiplierRoundedUp()
    {
        AssertEqual(1, Derelicts.SalvageableCap(UState), "a galaxy with no systems still has room for one");
        for (int i = 0; i < 25; ++i)
            AddDummyPlanet(new Vector2(-400_000f + i * 50_000f, -400_000f));
        AssertEqual(25, Derelicts.SalvageableCap(UState), "one salvageable derelict per system by default");
        AssertEqual(25, Derelicts.DriftingCap(UState), "and one drifting");

        GlobalStats.Defaults.SalvageableDerelictCapMultiplier = 1.2f;
        AssertEqual(30, Derelicts.SalvageableCap(UState), "the float 30.000002 of 25 times 1.2 does not round it up");
        GlobalStats.Defaults.SalvageableDerelictCapMultiplier = 0.33f;
        AssertEqual(9, Derelicts.SalvageableCap(UState), "part of a derelict rounds up");
        GlobalStats.Defaults.DriftingDerelictCapMultiplier = 0.01f;
        AssertEqual(3, Derelicts.DriftingCap(UState), "a multiplier is never below 0.1");
        GlobalStats.Defaults.DriftingDerelictCapMultiplier = 20f;
        AssertEqual(250, Derelicts.DriftingCap(UState), "nor above 10");
    }

    [TestMethod]
    public void AtTheLimitOfSalvageableDerelictsADeathBlastsAsUsual()
    {
        GlobalStats.Defaults.DerelictChance = 100;
        GlobalStats.Defaults.SalvageableDerelictCapMultiplier = 0.1f; // a cap of 1 in a galaxy of up to 10 systems
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
        AssertFalse(d.KnownByPlayer, "setup: the player does not know of it");
        AssertEqual(200, d.TurnsLeft, "100 turns times the production pace");

        for (int turn = 1; turn < 200; ++turn)
            UState.Derelicts.UpdateTurn();
        UState.Derelicts.Update(TestSimStep);
        AssertEqual(1, d.TurnsLeft, "one turn later");
        AssertTrue(d.CanBeSalvaged, "it is still there to salvage");
        AssertEqual(0.001f, d.RestDepth, d.Z, "at its rest depth, where an unseen one starts");

        AssertEqual(0.001f, 0f, UState.Derelicts.Chance(UState), "setup: it takes the one place of a galaxy with no systems, so it leaves no chance");
        UState.Derelicts.UpdateTurn();
        AssertTrue(d.Sinking, "its time is up");
        AssertFalse(d.CanBeSalvaged, "and it can no longer be salvaged");
        AssertEqual(0.001f, GlobalStats.Defaults.DerelictChance, UState.Derelicts.Chance(UState), "nor lowers the chance");
        UState.Derelicts.Update(TestSimStep);
        AssertEqual(0, UState.Derelicts.Count, "with no model and unknown to the player, it is gone at once");

        TestShip seen = SpawnShip("Dreadnought mk1-a", Player, Site(1));
        GiveSceneObject(seen);
        UState.Derelicts.TryAdd(seen, salvageable: true, seen: true);
        AssertEqual(0.5f, Derelict.RemoveDepth / Derelict.SinkSpeed, SecondsToSinkAway(Newest()),
                    "with a model, it sinks away like a drifting one");

        TestShip known = SpawnShip("Dreadnought mk1-a", Player, Site(2));
        known.KnownByEmpires.SetSeen(Player);
        UState.Derelicts.TryAdd(known, salvageable: true, seen: false);
        AssertTrue(Newest().KnownByPlayer && !Newest().InScene, "setup: one the player knows of, with no model");
        AssertEqual(0.5f, (Derelict.RemoveDepth - Newest().RestDepth) / Derelict.SinkSpeed, SecondsToSinkAway(Newest()),
                    "with no model, one the player knows of sinks away too, from its rest depth, to show if the player looks");
    }

    // its turns run out, then the seconds until it has sunk away and left the list
    float SecondsToSinkAway(Derelict d)
    {
        while (d.TurnsLeft > 0)
            UState.Derelicts.UpdateTurn();
        float seconds = 0f;
        while (UState.Derelicts.Count > 0)
        {
            UState.Derelicts.Update(TestSimStep);
            seconds += TestSimStep.FixedTime;
            Assert.IsTrue(seconds < 200f, "the derelict never left");
        }
        return seconds;
    }

    [TestMethod]
    public void AShipEnteringASystemShowsItsEmpireTheDerelictsThere()
    {
        NotificationManager notifications = TrackNotifications();
        SolarSystem system = AddSystem();
        Derelict d = Forms(Enemy, SystemSite + new Vector2(30_000f, 0));
        AssertFalse(Player.DerelictSystems.Contains(system) || d.KnownByPlayer, "setup: the player does not know of it");

        TestShip scout = SpawnShip("Vulcan Scout", Player, SystemSite + new Vector2(system.Radius + 20_000f, 0));
        UpdateObjectsTwice();
        AssertTrue(scout.System == null, "setup: the scout waits outside the system");
        AssertFalse(Player.DerelictSystems.Contains(system), "a ship outside the system learns nothing");

        MoveAndUpdate(scout, SystemSite + new Vector2(system.Radius - 20_000f, 0));
        AssertTrue(scout.System == system, "setup: the scout entered the system");
        AssertTrue(Player.DerelictSystems.Contains(system) && d.KnownByPlayer, "its empire lists the system and knows the derelict");
        AssertTrue(notifications.IsNotificationPresent(OneNew(system, d)), "and the player is told");

        SpawnShip("Vulcan Scout", Player, SystemSite + new Vector2(0, 30_000f));
        UpdateObjectsTwice();
        AssertEqual(1, notifications.NumberOfNotifications, "another ship entering does not tell again");

        CreateThirdMajorEmpire();
        SpawnShip("Vulcan Scout", ThirdMajor, SystemSite + new Vector2(0, -30_000f));
        UpdateObjectsTwice();
        AssertTrue(ThirdMajor.DerelictSystems.Contains(system), "another empire's ship shows that empire the system");
        AssertEqual(1, notifications.NumberOfNotifications, "and tells the player nothing");
        var entered = (Array<Ship>)typeof(UniverseObjectManager)
            .GetField("EnteredDerelictSystems", BindingFlags.NonPublic | BindingFlags.Instance)!.GetValue(UState.Objects);
        AssertEqual(0, entered.Count, "each step's arrivals are handled once");

        SolarSystem empty = AddSystem(SystemSite + new Vector2(0, 400_000f));
        TestShip visitor = SpawnShip("Vulcan Scout", Player, empty.Position + new Vector2(30_000f, 0));
        UpdateObjectsTwice();
        AssertTrue(visitor.System == empty, "setup: a scout in a system with no derelicts");
        AssertFalse(Player.DerelictSystems.Contains(empty), "which is not listed");
    }

    [TestMethod]
    public void AJoinThatComesAfterTheDerelictLeftChangesNothing()
    {
        SolarSystem system = AddSystem();
        UState.Derelicts.TryAdd(SpawnShip("Dreadnought mk1-a", Player, SystemSite + new Vector2(30_000f, 0)), salvageable: true, seen: false);
        Derelict d = Newest();
        while (d.TurnsLeft > 0)
            UState.Derelicts.UpdateTurn();
        Universe.InvokePendingSimThreadActions();
        AssertFalse(system.Derelicts.Count > 0 || Player.DerelictSystems.Contains(system), "one whose turns ran out before it joined does not join");

        UState.Derelicts.TryAdd(SpawnShip("Dreadnought mk1-a", Player, SystemSite + new Vector2(-30_000f, 0)), salvageable: true, seen: false);
        UState.Derelicts.Clear();
        Universe.InvokePendingSimThreadActions();
        AssertFalse(system.Derelicts.Count > 0 || Player.DerelictSystems.Contains(system), "nor one cleared away before it joined");
    }

    [TestMethod]
    public void OnLoadAnEmpireKnowsEveryDerelictInASystemItLists()
    {
        AddRemoteSystems(10);
        SolarSystem system = AddSystem();
        TestShip seen = SpawnShip("Dreadnought mk1-a", Enemy, SystemSite + new Vector2(30_000f, 0));
        seen.KnownByEmpires.SetSeen(Player);
        UState.Derelicts.TryAdd(seen, salvageable: true, seen: false);
        UState.Derelicts.TryAdd(SpawnShip("Dreadnought mk1-a", Enemy, SystemSite + new Vector2(-30_000f, 0)), salvageable: true, seen: false);
        Derelict unseen = Newest();
        AssertFalse(unseen.KnownByPlayer, "setup: saved before they joined their system, the player knows only the one it saw die");

        UniverseScreen loaded = SaveAndLoad();
        Derelict[] after = loaded.UState.Derelicts.ToArray();
        AssertTrue(loaded.UState.Player.DerelictSystems.Contains(after[0].System), "the player lists their system");
        AssertTrue(after[1].KnownByPlayer, "and so knows the other one there too");
    }

    // a death in the system, with more than six modules dead so it smokes from more than six places
    Derelict SalvageableWithSmoke(bool seen = false, Vector2 velocity = default)
    {
        AddSystem();
        Derelict d = DieWithSmoke(SystemSite + new Vector2(30_000f, 0), seen, velocity);
        AssertTrue(d.CanBeSalvaged, "setup: a salvageable derelict");
        return d;
    }

    Derelict DieWithSmoke(Vector2 position, bool seen, Vector2 velocity = default, bool drifting = false)
    {
        GlobalStats.Defaults.DerelictChance = 100;
        Ship ship = SpawnPlainShip("Dreadnought mk1-a", position);
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
        if (drifting)
            ship.DebugDieAsDerelict(salvageable: false);
        else
            ship.Die(null, cleanupOnly: false);
        UState.Objects.Update(TestSimStep);
        Derelict d = Newest();
        AssertGreaterThan(d.NumSmokeSources, Derelict.MinSmokeSources, "setup: it smokes from more than six places");
        return d;
    }

    // seen, by the debug key, which skips the roll: the chance falls once a salvageable derelict exists
    Derelict DriftingWithSmoke(Vector2 velocity = default)
    {
        Derelict d = DieWithSmoke(Site(0), seen: true, velocity, drifting: true);
        AssertFalse(d.Salvageable, "setup: a drifting derelict");
        return d;
    }

    void StepAll(Derelicts derelicts, float seconds)
    {
        int steps = (int)Math.Round(seconds / TestSimStepD);
        for (int i = 0; i < steps; ++i)
            derelicts.Update(TestSimStep);
    }

    static void AssertSameSmoke(Derelict before, Derelict after)
    {
        Derelict.SmokeSource[] a = SmokeOf(before), b = SmokeOf(after);
        for (int i = 0; i < b.Length; ++i)
        {
            AssertEqual(0.001f, a[i].Local, b[i].Local, $"smoke point {i} in the same place");
            AssertEqual(0.001f, a[i].Area, b[i].Area, $"smoke point {i} as big");
            AssertEqual(a[i].Type, b[i].Type, $"smoke point {i} of the same module type");
        }
    }

    static Derelict.SmokeSource[] SmokeOf(Derelict d)
    {
        return (Derelict.SmokeSource[])typeof(Derelict).GetField("Smoke", BindingFlags.NonPublic | BindingFlags.Instance)!.GetValue(d);
    }

    static Vector2 MeshOffsetOf(Derelict d)
    {
        return (Vector2)typeof(Derelict).GetField("MeshOffset", BindingFlags.NonPublic | BindingFlags.Instance)!.GetValue(d);
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
    public void ASaveKeepsASalvageableDerelictWithItsStateKnowledgeAndSmokePoints()
    {
        ViewEverything();
        CreateThirdMajorEmpire();
        Derelict d = SalvageableWithSmoke(seen: true, velocity: new Vector2(80f, 0));
        Universe.InvokePendingSimThreadActions(); // it joins its system
        StepAll(UState.Derelicts, 1f);
        AssertGreaterThan(d.VelocityNow.Length(), 0f, "setup: it is still slowing to a stop");
        SpawnShip("Vulcan Scout", Enemy, SystemSite + new Vector2(0, 30_000f));
        UpdateObjectsTwice(); // an enemy scout enters the system
        for (int turn = 0; turn < 3; ++turn)
            UState.Derelicts.UpdateTurn();
        AssertTrue(d.IsKnownBy(Enemy) && d.IsKnownBy(Player), "setup: the enemy and the player know of it");
        AssertFalse(d.IsKnownBy(ThirdMajor), "setup: the third empire does not");
        AssertGreaterThan(d.Production, 0f, "setup: it has production to salvage");
        typeof(Derelict).GetProperty(nameof(Derelict.ProductionLeft))!.SetValue(d, d.Production * 0.5f); // partly salvaged

        UniverseScreen loaded = SaveAndLoad();
        Derelict[] after = loaded.UState.Derelicts.ToArray();
        AssertEqual(1, after.Length, "the salvageable derelict comes back");
        Derelict back = after[0];
        AssertTrue(back.CanBeSalvaged, "still salvageable");
        AssertEqual(0.01f, d.Position, back.Position, "where it was");
        AssertEqual(0.001f, d.Age, back.Age, "as old");
        AssertEqual(0.01f, d.VelocityNow, back.VelocityNow, "slowing down as it was");
        AssertEqual(0.001f, new Vector3(d.RotationX, d.RotationY, d.RotationZ),
                    new Vector3(back.RotationX, back.RotationY, back.RotationZ), "turned as it was");
        AssertEqual(0.01f, d.Radius, back.Radius, "as big");
        AssertEqual(d.TurnsLeft, back.TurnsLeft, "with the turns it had left");
        AssertEqual(0.01f, d.Production, back.Production, "the production it started with");
        AssertEqual(0.01f, d.ProductionLeft, back.ProductionLeft, "and what was left of it");
        Assert.AreSame(ResourceManager.Ships.GetDesign(d.Design.Name), back.Design, "of the loaded game's own design");
        AssertEqual(new Vector2(0f, -16f), MeshOffsetOf(back), "its model offset on its hull, as the Dreadnought hull sets it");
        AssertEqual(Player.Id, back.Owner.Id, "and owner");
        AssertTrue(back.IsKnownBy(loaded.UState.GetEmpireById(Enemy.Id)), "the enemy still knows of it");
        AssertTrue(back.IsKnownBy(loaded.UState.Player), "and so does the player");
        AssertFalse(back.IsKnownBy(loaded.UState.GetEmpireById(ThirdMajor.Id)), "the third empire still does not");
        AssertEqual(d.NumSmokeSources, back.NumSmokeSources, "still thinning out, it smokes from all its places");
        AssertSameSmoke(d, back);
        AssertFalse(back.InScene, "with no model until the player sees it");
        SolarSystem system = loaded.UState.Systems.First(s => s.Id == d.System.Id);
        Assert.AreSame(system, back.System, "in the loaded game's own system");
        AssertTrue(system.Derelicts.SequenceEqual(new[] { back }), "which lists it again");
        AssertTrue(loaded.UState.Player.DerelictSystems.Contains(system), "as the player, who knows of it, lists the system again");
        AssertTrue(loaded.UState.GetEmpireById(Enemy.Id).DerelictSystems.Contains(system), "and so does the enemy");
        AssertFalse(loaded.UState.GetEmpireById(ThirdMajor.Id).DerelictSystems.Contains(system), "and the third empire does not");
    }

    [TestMethod]
    public void ASaveKeepsADriftingDerelictWhichSinksOnWithNoModelUntilThePlayerSeesIt()
    {
        ViewEverything();
        Derelict d = DriftingWithSmoke(new Vector2(80f, 0));
        StepAll(UState.Derelicts, d.BreakupSeconds + 10f);
        AssertTrue(d.Sinking && d.Z > 0f, "setup: it is sinking");

        UniverseScreen loaded = SaveAndLoad();
        Derelict[] after = loaded.UState.Derelicts.ToArray();
        AssertEqual(1, after.Length, "it comes back");
        Derelict back = after[0];
        AssertFalse(back.Salvageable, "still a drifting one");
        AssertTrue(back.Sinking, "and sinking");
        AssertEqual(0.01f, d.Z, back.Z, "as deep as it was");
        AssertEqual(0.01f, d.Position, back.Position, "where it was");
        AssertEqual(0.01f, d.VelocityNow, back.VelocityNow, "at rest as it was");
        AssertEqual(0.001f, d.Age, back.Age, "as old");
        AssertTrue(back.KnownByPlayer, "known to the player, who saw it die");
        AssertEqual(d.NumSmokeSources, back.NumSmokeSources, "still thinning out, it smokes from all its places");
        AssertSameSmoke(d, back);
        AssertFalse(back.InScene, "with no model");

        float z = back.Z, x = back.Position.X;
        StepAll(loaded.UState.Derelicts, 1f);
        AssertEqual(1, loaded.UState.Derelicts.Count, "it does not vanish for want of a model");
        AssertEqual(1f, z + Derelict.SinkSpeed, back.Z, "it sinks on");
        AssertEqual(1f, x, back.Position.X, "where it came to rest");
        loaded.Frustum = Universe.Frustum;
        loaded.UState.ViewState = UniverseScreen.UnivScreenState.PlanetView;
        AssertTrue(back.WantsModel, "on screen it gets a model");
    }

    [TestMethod]
    public void PastTheThinOutASaveKeepsOnlyTheSixSmokePointsLeft()
    {
        Derelict d = DriftingWithSmoke();
        StepAll(UState.Derelicts, Derelict.SmokeThinSeconds + 1f);
        AssertEqual(1, UState.Derelicts.Count, "setup: it has not sunk away yet");
        AssertGreaterThan(d.NumSmokeSources, Derelict.MinSmokeSources, "setup: it holds more than the six still smoking");
        Derelict back = SaveAndLoad().UState.Derelicts.Find(d.Id);
        Assert.IsNotNull(back, "setup: it comes back from the save");
        Derelict.SmokeSource[] saved = SmokeOf(back), smoke = SmokeOf(d);
        AssertEqual(Derelict.MinSmokeSources, saved.Length, "the six left");
        for (int i = 0; i < saved.Length; ++i)
            AssertEqual(0.001f, smoke[i].Local, saved[i].Local, $"the ones still smoking: point {i}");
    }

    [TestMethod]
    public void UnderAModWithoutSalvageContentASavedSalvageableDerelictSinksAway()
    {
        AddRemoteSystems(10);
        Derelict known = SalvageableWithSmoke(seen: true);
        UState.Derelicts.TryAdd(SpawnShip("Dreadnought mk1-a", Enemy, SystemSite + new Vector2(0, 30_000f)), salvageable: true, seen: false);
        Derelict unknown = Newest();
        AssertTrue(known.KnownByPlayer && !unknown.KnownByPlayer, "setup: one the player knows of, and one the player does not");
        DriftingWithSmoke();

        GlobalStats.Defaults.EnableSalvageOps = false;
        UniverseScreen loaded = SaveAndLoad();
        Derelict[] after = loaded.UState.Derelicts.ToArray();
        AssertEqual(3, after.Length, "all three load");
        AssertTrue(after[0].Sinking && after[1].Sinking, "the mod's switch wins: the salvageable ones sink");
        AssertEqual(0, loaded.UState.Derelicts.NumSalvageable, "and none can be salvaged");
        AssertFalse(after[2].Salvageable, "the drifting one is only a visual, so it loads whatever the switch says");
        AssertLessThan(after[2].Age, Derelict.MinBreakupSeconds, "setup: it was saved during its breakup");
        AssertEqual(0, loaded.UState.Player.DerelictSystems.Count, "and no system is listed for ones that cannot be salvaged");

        loaded.UState.Derelicts.Update(TestSimStep);
        Derelict[] left = loaded.UState.Derelicts.ToArray();
        AssertEqual(2, left.Length, "with no model, the one the player does not know of is gone at once");
        AssertTrue(left[0] == after[0] && left[1] == after[2], "the known one and the drifting one sink on");
        AssertGreaterThan(after[0].Z, 0f, "the known one sinks");
        AssertGreaterThan(after[2].Z, 0f, "saved during its breakup, the drifting one sinks at once");
    }

    [TestMethod]
    public void AContentReloadKeepsEveryDerelictWithoutItsModel()
    {
        ViewEverything();
        Derelict salvageable = SalvageableWithSmoke(seen: true);
        Derelict drifting = DriftingWithSmoke();
        AssertTrue(salvageable.InScene && drifting.InScene, "setup: both have models");

        UState.RemoveSceneObjects(); // as the options screen's Apply reloads the content
        Derelict[] after = UState.Derelicts.ToArray();
        AssertEqual(2, after.Length, "both stay");
        AssertTrue(after[0] == salvageable && after[1] == drifting, "the same derelicts, with their turns and knowledge");
        AssertFalse(salvageable.InScene || drifting.InScene, "their models are gone");
        AssertTrue(salvageable.WantsModel && drifting.WantsModel, "and they get them again once on screen");
    }

    [TestMethod]
    public void ADerelictGetsAModelOnlyOnceThePlayerKnowsOfItAndSeesIt()
    {
        AddRemoteSystems(10);
        ViewEverything();
        AddSystem();
        Derelict d = Forms(Enemy, SystemSite + new Vector2(30_000f, 0));
        AssertFalse(d.IsKnownBy(Player), "setup: the player does not know of it");
        AssertFalse(d.WantsModel, "a wreck the player does not know of gets no model");

        SpawnShip("Vulcan Scout", Player, SystemSite + new Vector2(0, 30_000f));
        UpdateObjectsTwice(); // the scout enters its system
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

        Derelict expired = Forms(Enemy, SystemSite + new Vector2(-30_000f, 0));
        AssertTrue(expired.WantsModel, "setup: the player knows of it and it is on screen");
        while (expired.TurnsLeft > 0)
            UState.Derelicts.UpdateTurn();
        AssertTrue(expired.WantsModel, "its turns ran out: it still wants one, to sink away on screen");
        AssertTrue(UState.Derelicts.TryAttachModel(expired, new SceneObject("expired")),
                   "one whose turns ran out while its model loaded still takes it, to sink away on screen");

        TestShip other = SpawnShip("Dreadnought mk1-a", Enemy, Site(1));
        UState.Derelicts.TryAdd(other, salvageable: true, seen: false);
        Derelict gone = Newest();
        UState.Derelicts.Clear();
        AssertFalse(UState.Derelicts.TryAttachModel(gone, new SceneObject("late")), "one that left while its model loaded gets none");
    }

    Derelict SalvageableOf(Empire owner, Vector2 at, bool knownByPlayer)
    {
        TestShip ship = SpawnShip("Dreadnought mk1-a", owner, at);
        if (knownByPlayer)
            ship.KnownByEmpires.SetSeen(Player);
        AssertTrue(UState.Derelicts.TryAdd(ship, salvageable: true, seen: false), "setup: a salvageable derelict");
        return Newest();
    }

    [TestMethod]
    public void ASalvageableDerelictHoldsHalfTheHealthItHadLeftTimesItsCostBeforeBonuses()
    {
        AddRemoteSystems(10);
        UState.P.Pace = 3f;
        AssertEqual(0.001f, 2f, UState.ProductionPace, "setup: a slower pace, which the cost includes");
        Enemy.data.Traits.ShipCostMod = 0.5f;
        TestShip ship = SpawnShip("Dreadnought mk1-a", Enemy, Site(0));
        foreach (ShipModule m in ship.Modules.Where(m => m.CanVisualizeDamage && !m.HasInternalRestrictions).Take(10))
            m.SetHealth(0f, "Test");
        float health = ship.HealthPercent;
        AssertTrue(health is > 0f and < 0.99f, "setup: the ship is damaged");
        float cost = (int)(ship.ShipData.BaseCost * 2f);
        AssertLessThan(cost, ship.GetCost(Enemy), "setup: its cost trait made it cost its owner more");

        UState.Derelicts.TryAdd(ship, salvageable: true, seen: false);
        Derelict d = Newest();
        AssertEqual(0.5f, health * 0.5f * cost, d.Production, "half the share of health it had left, times its cost before any empire's bonus");
        AssertEqual(0.01f, d.Production, d.ProductionLeft, "none of it salvaged yet");

        TestShip station = SpawnShip("Cordrazine Small Station Mk1", Enemy, Site(1));
        float stationValue = 0.5f * (int)(station.ShipData.BaseCost * 2f * 0.7f);
        AssertTrue(station.ShipData.IsPlatformOrStation && station.HealthPercent > 0.99f && stationValue > 100f,
                   "setup: an undamaged station worth more than the floor");
        UState.Derelicts.TryAdd(station, salvageable: true, seen: false);
        AssertEqual(0.5f, stationValue, Newest().Production, "a platform or station costs 30% less");

        TestShip wreck = SpawnShip("Corsair", Enemy, Site(2));
        foreach (ShipModule m in wreck.Modules) // its internal modules intact, as a ship still alive has them
            m.SetHealth(m.HasInternalRestrictions ? 1f : 0f, "Test");
        AssertLessThan(wreck.HealthPercent * 0.5f * (int)(wreck.ShipData.BaseCost * 2f), 100f, "setup: a cheap wreck worth less than the floor");
        UState.Derelicts.TryAdd(wreck, salvageable: true, seen: false);
        AssertEqual(0.01f, 50f * 2f, Newest().Production, "still holds 50, times the production pace");

        Derelict drifting = DriftingWithSmoke();
        AssertEqual(0.01f, 0f, drifting.Production, "a drifting one has nothing to salvage");
    }

    [TestMethod]
    public void ThePlayerSelectsOnlyTheSalvageableDerelictsItKnowsOf()
    {
        AddRemoteSystems(10);
        Derelict known = SalvageableOf(Enemy, Site(1), knownByPlayer: true);
        Derelict unknown = SalvageableOf(Enemy, Site(2), knownByPlayer: false);
        Derelict drifting = DriftingWithSmoke();
        AssertTrue(known.SelectableByPlayer, "a salvageable derelict the player knows of can be selected");
        AssertFalse(unknown.SelectableByPlayer, "one the player does not know of cannot");
        AssertTrue(drifting.KnownByPlayer && !drifting.SelectableByPlayer, "nor can a drifting one, though the player saw it die");

        while (known.TurnsLeft > 0)
            UState.Derelicts.UpdateTurn();
        AssertFalse(known.SelectableByPlayer, "nor one whose turns ran out");
    }

    [TestMethod]
    public void TheUIThreadSeesEveryChangeToTheList()
    {
        AddRemoteSystems(10);
        AssertEqual(0, UState.Derelicts.Current.Length, "setup: none yet");
        Derelict stays = SalvageableOf(Enemy, Site(0), knownByPlayer: true);
        Derelict leaves = SalvageableOf(Enemy, Site(1), knownByPlayer: false);
        AssertTrue(UState.Derelicts.Current.SequenceEqual(new[] { stays, leaves }), "the added ones");

        while (leaves.TurnsLeft > 0)
            UState.Derelicts.UpdateTurn();
        UState.Derelicts.Update(TestSimStep);
        AssertEqual(1, UState.Derelicts.Count, "setup: with no model, the one the player does not know of is gone");
        AssertTrue(UState.Derelicts.Current.SequenceEqual(new[] { stays }), "and gone from what the UI thread sees");

        UniverseScreen loaded = SaveAndLoad();
        AssertEqual(1, loaded.UState.Derelicts.Current.Length, "a loaded one");

        UState.Derelicts.Clear();
        AssertEqual(0, UState.Derelicts.Current.Length, "and none once cleared");
    }

    void LookDownAt(Vector2 position, double height)
    {
        Universe.CamPos = new Vector3d(position.X, position.Y, height);
        Universe.SetViewPerspective(Matrices.CreateLookAtDown(position.X, position.Y, -height), maxDistance: UniverseScreen.CAM_MAX);
    }

    [TestMethod]
    public void AClickOnItsIconOrItsHullFindsADerelictThePlayerCanSelect()
    {
        AddRemoteSystems(10);
        Derelict d = SalvageableOf(Enemy, Site(0), knownByPlayer: true);
        Derelict unknown = SalvageableOf(Enemy, Site(5), knownByPlayer: false);

        LookDownAt(d.Position, 2_000_000);
        Vector2 icon = Universe.ProjectToScreenPosition(d.Position, d.Z).ToVec2f();
        AssertLessThan(Universe.ProjectToScreenSize(d.Radius), 2.0, "setup: zoomed out it is only an icon");
        Assert.AreSame(d, Universe.FindClickedDerelict(icon), "a click on its icon");
        Assert.AreSame(d, Universe.FindClickedDerelict(icon + new Vector2(10f, 0)), "or at its edge");
        Assert.IsNull(Universe.FindClickedDerelict(icon + new Vector2(20f, 0)), "but not past it");
        Assert.IsNull(Universe.FindClickedDerelict(Universe.ProjectToScreenPosition(unknown.Position, unknown.Z).ToVec2f()),
                      "nor on one the player does not know of");

        LookDownAt(d.Position, 5000);
        Vector2 center = Universe.ProjectToScreenPosition(d.Position, d.Z).ToVec2f();
        float hull = (float)Universe.ProjectToScreenSize(d.Radius);
        AssertGreaterThan(hull, 50f, "setup: up close its hull is bigger than its icon");
        Assert.AreSame(d, Universe.FindClickedDerelict(center + new Vector2(hull * 0.9f, 0)), "up close a click on its hull");
    }

    [TestMethod]
    public void SelectingADerelictClearsTheOtherSelectionsAndOneThatSinksDropsOut()
    {
        Universe.ShipInfoUIElement ??= new(new Rectangle(0, 0, 407, 242), Universe.ScreenManager, Universe);
        Derelict d = SalvageableOf(Enemy, Site(0), knownByPlayer: true);
        TestShip ship = SpawnShip("Vulcan Scout", Player, Site(3));

        Universe.SetSelectedShip(ship);
        Universe.SetSelectedDerelict(d);
        Assert.AreSame(d, Universe.SelectedDerelict, "the derelict is selected");
        AssertTrue(Universe.SelectedShip == null && Universe.SelectedShips.Count == 0, "in place of the ship");
        AssertTrue(Universe.HasSelectedItem, "so a right click on a ship selects nothing, as with a planet");

        Universe.SetSelectedShip(ship);
        Assert.IsNull(Universe.SelectedDerelict, "selecting a ship deselects it");

        Universe.SetSelectedDerelict(d);
        Universe.UpdateSelectedDerelict();
        Assert.AreSame(d, Universe.SelectedDerelict, "it stays selected while it can be salvaged");
        while (d.TurnsLeft > 0)
            UState.Derelicts.UpdateTurn();
        Universe.UpdateSelectedDerelict();
        Assert.IsNull(Universe.SelectedDerelict, "once it starts to sink it is no longer selected");
    }

    NotificationManager TrackNotifications()
    {
        Universe.NotificationManager = new NotificationManager(Universe.ScreenManager, Universe);
        return Universe.NotificationManager;
    }

    static string OneNew(SolarSystem system, Derelict d)
    {
        return string.Format(Localizer.Token(GameText.NewSalvageableDerelict), system.Name, d.Design.Name);
    }

    // a salvageable derelict of the owner's ship, killed by the killer, which joins its system's lists on the sim thread
    Derelict Forms(Empire owner, Vector2 at, Empire killer = null, bool seenByPlayer = false)
    {
        TestShip ship = SpawnShip("Dreadnought mk1-a", owner, at);
        if (seenByPlayer)
            ship.KnownByEmpires.SetSeen(Player);
        AssertTrue(UState.Derelicts.TryAdd(ship, salvageable: true, seen: false, killer), "setup: a salvageable derelict");
        Universe.InvokePendingSimThreadActions();
        return Newest();
    }

    // a system finds its ships where the spatial index put them the step before, so an arrival counts on the second step
    void UpdateObjectsTwice()
    {
        UState.Objects.Update(TestSimStep);
        UState.Objects.Update(TestSimStep);
    }

    void MoveAndUpdate(Ship ship, Vector2 to)
    {
        ship.Position = to;
        UpdateObjectsTwice();
    }

    // whether the player lists the system of a derelict of the enemy's ship, killed by the third empire, formed while the rule holds
    bool PlayerLearnsOfOneFormingIn(SolarSystem system, Action rule, Action undo, bool seenByPlayer = false)
    {
        rule();
        Derelict d = Forms(Enemy, SystemSite + new Vector2(30_000f, 0), ThirdMajor, seenByPlayer);
        AssertTrue(d.System == system, "setup: it stopped in the system");
        bool learned = Player.DerelictSystems.Contains(system);
        AssertEqual(learned, d.KnownByPlayer, "an empire that lists the system knows of the derelict");
        UState.Derelicts.Clear();
        undo();
        return learned;
    }

    [TestMethod]
    public void EmpiresThatLearnOfAFormingDerelictListItsSystem()
    {
        CreateThirdMajorEmpire();
        SolarSystem system = AddSystem();
        Derelict d = Forms(Enemy, SystemSite + new Vector2(30_000f, 0), ThirdMajor);
        AssertTrue(system.Derelicts.SequenceEqual(new[] { d }), "it joins its system");
        AssertTrue(Enemy.DerelictSystems.Contains(system), "its owner lists the system");
        AssertTrue(ThirdMajor.DerelictSystems.Contains(system), "and so does its killer");
        AssertFalse(Player.DerelictSystems.Contains(system) || d.KnownByPlayer, "an empire no rule names does not, and does not know of it");
        UState.Derelicts.Clear();

        void Nothing() { }
        AssertFalse(PlayerLearnsOfOneFormingIn(system, Nothing, Nothing), "setup: no rule names the player");
        AssertTrue(PlayerLearnsOfOneFormingIn(system, Nothing, Nothing, seenByPlayer: true), "the player had the ship in its sensors at its death");
        AssertTrue(PlayerLearnsOfOneFormingIn(system, () => Player.SignTreatyWith(Enemy, TreatyType.Alliance),
                                              () => Player.BreakTreatyWith(Enemy, TreatyType.Alliance)), "the player is allied with its owner");
        AssertTrue(PlayerLearnsOfOneFormingIn(system, () => Player.SignTreatyWith(ThirdMajor, TreatyType.Alliance),
                                              () => Player.BreakTreatyWith(ThirdMajor, TreatyType.Alliance)), "or with its killer");
        Espionage onOwner = Player.GetRelations(Enemy).Espionage, onKiller = Player.GetRelations(ThirdMajor).Espionage;
        AssertFalse(PlayerLearnsOfOneFormingIn(system, () => onOwner.Level = Espionage.MaxLevel - 1, () => onOwner.Level = 0),
                    "an infiltration below the top level is not enough");
        AssertTrue(PlayerLearnsOfOneFormingIn(system, () => onOwner.Level = Espionage.MaxLevel, () => onOwner.Level = 0),
                   "the player has infiltrated its owner to the top level");
        AssertTrue(PlayerLearnsOfOneFormingIn(system, () => onKiller.Level = Espionage.MaxLevel, () => onKiller.Level = 0),
                   "or its killer");
        AssertTrue(PlayerLearnsOfOneFormingIn(system, () => Player.data.MoleList.Add(new Mole { PlanetId = system.PlanetList[0].Id }),
                                              () => Player.data.MoleList.Clear()), "the player has a mole on a planet there");
        Planet elsewhere = AddSystem(SystemSite + new Vector2(0, 400_000f)).PlanetList[0];
        AssertFalse(PlayerLearnsOfOneFormingIn(system, () => Player.data.MoleList.Add(new Mole { PlanetId = elsewhere.Id }),
                                               () => Player.data.MoleList.Clear()), "a mole in another system tells nothing");
        TestShip scout = SpawnShip("Vulcan Scout", Player, Site(5));
        AssertTrue(PlayerLearnsOfOneFormingIn(system, () => MoveAndUpdate(scout, SystemSite + new Vector2(0, -90_000f)),
                                              () => MoveAndUpdate(scout, Site(5))), "the player has a ship there");
        AssertTrue(system.ShipList.Any(s => s.Loyalty == Enemy), "setup: the enemy's ships are still there");
        AssertFalse(PlayerLearnsOfOneFormingIn(system, Nothing, Nothing), "another empire's ship there tells the player nothing");
        AssertTrue(PlayerLearnsOfOneFormingIn(system, () => system.PlanetList[0].SetOwner(Player), Nothing), "the player holds a colony there");

        Derelict own = Forms(Player, SystemSite + new Vector2(-30_000f, 0));
        AssertTrue(own.System == system && Player.DerelictSystems.Contains(system), "the player's own lost ship");
        UState.Derelicts.Clear();
        Forms(Enemy, SystemSite + new Vector2(-30_000f, 0), killer: Player);
        AssertTrue(Player.DerelictSystems.Contains(system), "and one the player killed");
    }

    [TestMethod]
    public void AnEmpireThatListsASystemKnowsEveryDerelictInItAndASystemLeavesTheListsWithItsLastSalvageableOne()
    {
        AddRemoteSystems(10);
        SolarSystem system = AddSystem();
        Derelict deepSpace = Forms(Enemy, Site(0));
        AssertTrue(deepSpace.System == null && Enemy.DerelictSystems.Count == 0, "one outside every system joins no list");

        Derelict first = Forms(Player, SystemSite + new Vector2(30_000f, 0));
        UState.Derelicts.UpdateTurn();
        Derelict second = Forms(Enemy, SystemSite + new Vector2(-30_000f, 0));
        AssertTrue(second.KnownByPlayer, "the player, who lists the system, knows of a new one there that no rule names");
        AssertTrue(system.Derelicts.SequenceEqual(new[] { first, second }), "the system holds both");
        AssertTrue(Enemy.DerelictSystems.Contains(system), "setup: the enemy lists it too");

        while (first.TurnsLeft > 0)
            UState.Derelicts.UpdateTurn();
        AssertTrue(system.Derelicts.SequenceEqual(new[] { second }), "one that can no longer be salvaged leaves its system at once");
        AssertTrue(Player.DerelictSystems.Contains(system) && Enemy.DerelictSystems.Contains(system), "which stays listed while another is left");

        while (second.TurnsLeft > 0)
            UState.Derelicts.UpdateTurn();
        AssertEqual(0, system.Derelicts.Count, "the last one leaves too");
        AssertFalse(Player.DerelictSystems.Contains(system) || Enemy.DerelictSystems.Contains(system), "and the system leaves every list");
        AssertEqual(3, UState.Derelicts.Count, "though they are still there to sink away");

        Forms(Player, SystemSite + new Vector2(0, 30_000f));
        AssertTrue(Player.DerelictSystems.Contains(system), "setup: a new one lists it again");
        UState.Derelicts.Clear();
        AssertFalse(Player.DerelictSystems.Contains(system) || system.Derelicts.Count > 0, "clearing the derelicts clears the lists");
    }

    [TestMethod]
    public void TheEmpireWhoseFireKilledTheShipListsTheSystemOfItsDerelict()
    {
        GlobalStats.Defaults.DerelictChance = 100;
        SolarSystem system = AddSystem();
        Ship ship = SpawnPlainShip("Dreadnought mk1-a", SystemSite + new Vector2(30_000f, 0));
        UState.Objects.Update(TestSimStep);
        TestShip shooter = SpawnShip("Vulcan Scout", Enemy, Site(5));
        AssertFalse(ship.KnownByEmpires.KnownBy(Enemy), "setup: the enemy has no sensors on it");
        ship.SetLastDamagedBy(shooter); // as every hit does
        ship.Die(ship.LastDamagedBy, cleanupOnly: false);
        UState.Objects.Update(TestSimStep);
        Universe.InvokePendingSimThreadActions();
        AssertTrue(Newest().System == system, "setup: a salvageable derelict in the system");
        AssertTrue(Enemy.DerelictSystems.Contains(system), "the enemy, whose fire killed it, lists its system");
    }

    [TestMethod]
    public void ThePlayerIsToldWhenASystemJoinsItsList()
    {
        AddRemoteSystems(10);
        NotificationManager notifications = TrackNotifications();
        SolarSystem system = AddSystem();
        SolarSystem other = AddSystem(SystemSite + new Vector2(0, 400_000f));
        AssertFalse(system.HasPlanetsOwnedBy(Player), "setup: the player holds no colony there");
        Derelict d = Forms(Player, SystemSite + new Vector2(30_000f, 0));
        AssertEqual(1, notifications.NumberOfNotifications, "a system joins the player's list");
        AssertTrue(notifications.IsNotificationPresent(OneNew(system, d)), "telling of the derelict that brought it");

        Forms(Enemy, SystemSite + new Vector2(-30_000f, 0), seenByPlayer: true);
        AssertEqual(1, notifications.NumberOfNotifications, "a new one in a system already on the list is not news");
        Forms(Enemy, other.Position + new Vector2(30_000f, 0));
        AssertEqual(1, notifications.NumberOfNotifications, "nor is one in a system the player does not learn of");
    }

    void ClickTopNotification(NotificationManager notifications)
    {
        var mouse = new MockInputProvider { MousePos = new Vector2(GameBase.ScreenWidth - 40, 100) };
        var input = new InputState { Provider = mouse };
        mouse.LeftMouse = SDGraphics.Input.ButtonState.Pressed;
        input.Update(new UpdateTimes(1 / 60f, 0f));
        mouse.LeftMouse = SDGraphics.Input.ButtonState.Released;
        input.Update(new UpdateTimes(1 / 60f, 0f));
        AssertTrue(notifications.HandleInput(input), "setup: the click reaches the notification");
    }

    [TestMethod]
    public void ClickingTheNotificationSelectsTheDerelictOrShowsItsSystemOnceItCanNoLongerBeSalvaged()
    {
        Universe.SystemInfoOverlay ??= new(new Rectangle(0, 0, 407, 242), Universe.ScreenManager, Universe);
        ViewEverything();
        GlobalStats.Defaults.DerelictChance = 100;
        NotificationManager notifications = TrackNotifications();
        SolarSystem system = AddSystem();
        Vector2 site = SystemSite + new Vector2(30_000f, 0), speed = new(80f, 0);
        DieAt(site, seen: true, velocity: speed);
        Derelict d = Newest();
        Universe.InvokePendingSimThreadActions();
        StepAll(UState.Derelicts, 4f);
        AssertGreaterThan(d.VelocityNow.Length(), 0f, "setup: seen, it is still slowing to a stop");
        ClickTopNotification(notifications);
        Assert.AreSame(d, Universe.SelectedDerelict, "the derelict is selected");
        AssertEqual(1f, (site + speed * (Derelict.StopSeconds / 2f)).X, (float)Universe.CamDestination.X,
                    "and the camera goes to where it comes to rest");

        while (d.TurnsLeft > 0)
            UState.Derelicts.UpdateTurn();
        Derelict sank = Forms(Enemy, SystemSite + new Vector2(-30_000f, 0), seenByPlayer: true);
        AssertEqual(1, notifications.NumberOfNotifications, "setup: told of the next one there");
        while (sank.TurnsLeft > 0)
            UState.Derelicts.UpdateTurn();
        ClickTopNotification(notifications);
        Assert.IsNull(Universe.SelectedDerelict, "one whose turns ran out before the click cannot be selected");
        Assert.AreSame(system, Universe.SelectedSystem, "so its system is shown");
    }
}
