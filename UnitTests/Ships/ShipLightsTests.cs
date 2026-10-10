using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Ship_Game;
using Ship_Game.Gameplay;
using Ship_Game.Graphics.Particles;
using Ship_Game.Ships;
using SynapseGaming.LightingSystem.Rendering;
using Vector2 = SDGraphics.Vector2;
#pragma warning disable CA2213

namespace UnitTests.Ships;

/// <summary>
/// A ship's lights are its glow map. Without power they go dark and its thrusters vanish; they come back a second after
/// power returns. While the ship is EMP-disabled or dying, they stutter like a failing fluorescent tube.
/// </summary>
[TestClass]
public class ShipLightsTests : StarDriveTest
{
    public ShipLightsTests()
    {
        LoadStarterShips("Platform Base mk1-a", "Proton Troopship  Mk1");
        CreateUniverseAndPlayerEmpire();
    }

    public override void Cleanup()
    {
        Universe.Particles?.Dispose();
        Universe.Particles = null;
        base.Cleanup();
    }

    TestShip NewShip(float x = 1000f) => SpawnShip("Terran-Prototype", Player, new Vector2(x, 1000f));

    static void SetReactors(Ship ship, bool working)
    {
        foreach (ShipModule m in ship.Modules.Where(m => m.PowerFlowMax > 0f))
            m.SetHealth(working ? m.ActualMaxHealth : 0f, "Test");
    }

    void Run(Ship ship, float seconds, bool onScreen = true)
    {
        for (float time = 0f; time < seconds; time += TestSimStep.FixedTime)
        {
            ship.InFrustum = onScreen;
            ship.Update(TestSimStep);
        }
    }

    void CutPower(Ship ship)
    {
        SetReactors(ship, working: false);
        Run(ship, 1.5f);
        AssertEqual(0.001f, 0f, ship.PowerFlowMax, "setup: the reactors are gone");
    }

    static float[] LevelsOverAStutter(Ship ship)
    {
        return Enumerable.Range(0, 80).Select(i => ship.LightLevel(i * Ship.StutterSeconds / 80f)).ToArray();
    }

    static void AssertStutters(float[] levels, string what)
    {
        Assert.IsTrue(levels.Contains(0f) && levels.Contains(1f), $"{what}: the lights flick between dark and full");
        Assert.IsTrue(levels.Any(l => l is > 0f and < 1f), $"{what}: with half-lit steps between");
    }

    static SceneObject GiveSceneObject(Ship ship)
    {
        var so = new SceneObject(ship.Name);
        typeof(Ship).GetField("ShipSO", BindingFlags.NonPublic | BindingFlags.Instance)!.SetValue(ship, so);
        return so;
    }

    [TestMethod]
    public void AShipWithoutPowerGoesDarkAndLosesItsThrusters()
    {
        TestShip ship = NewShip();
        Run(ship, 1f);
        Assert.IsFalse(ship.LightsOut, "setup: a powered ship is lit");
        Assert.IsTrue(ship.ThrustersShown, "setup: and shows its thrusters");
        AssertEqual(0.001f, 1f, ship.LightLevel(5f), "full lights");

        CutPower(ship);
        Assert.IsTrue(ship.LightsOut, "no power, no lights");
        AssertEqual(0.001f, 0f, ship.LightLevel(5f), "dark");
        Assert.IsFalse(ship.ThrustersShown, "and no thrusters");
    }

    [TestMethod]
    public void TheLightsComeBackASecondAfterThePower()
    {
        TestShip ship = NewShip();
        CutPower(ship);
        SetReactors(ship, working: true);

        float powered = 0f;
        for (float time = 0f; ship.LightsOut && time < 5f; time += TestSimStep.FixedTime)
        {
            ship.InFrustum = true;
            bool storeHeldPower = ship.PowerCurrent > 0f && ship.PowerCurrent >= ship.CheapestEnergyShot;
            ship.Update(TestSimStep);
            if (ship.PowerFlowMax > 0f && storeHeldPower)
                powered += TestSimStep.FixedTime;
        }
        Assert.IsFalse(ship.LightsOut, "the lights come back");
        AssertEqual(TestSimStep.FixedTime * 1.5f, Ship.RelightSeconds, powered, "a second after the power");
    }

    [TestMethod]
    public void AShipWhoseReactorsDieGoesDarkWithItsFuelCellsStillCharged()
    {
        TestShip ship = SpawnShip("Proton Troopship  Mk1", Player, new Vector2(3000f, 3000f));
        CutPower(ship);
        AssertGreaterThan(ship.PowerCurrent, 0f, "setup: with no module powered nothing draws, so the fuel cells keep their charge");
        Assert.IsTrue(ship.LightsOut, "no reactors, no power");
    }

    [TestMethod]
    public void AShipThatRunsItsStoreDryGoesDarkButKeepsItsThrusters()
    {
        TestShip ship = SpawnShip("Proton Troopship  Mk1", Player, new Vector2(3000f, 3000f));
        Run(ship, 0.1f);
        AssertGreaterThan(ship.PowerDraw, ship.PowerFlowMax, "setup: it draws more than its reactors make");
        Assert.IsFalse(ship.LightsOut, "setup: lit while its store lasts");
        float fullSpeed = ship.VelocityMax;

        ship.PowerCurrent = 1f;
        Run(ship, 1f);
        AssertEqual(0.001f, 0f, ship.PowerCurrent, "setup: the store ran dry");
        Assert.IsTrue(ship.LightsOut, "an empty store is no power, even with working reactors");
        Assert.IsTrue(ship.ThrustersShown, "the engines run off the reactors, so the thrusters stay");
        AssertGreaterThan(TrailParticles(ship, fullSpeed), 0, "and so does the engine trail");
    }

    [TestMethod]
    public void BeamsThatEmptyTheStoreKeepTheShipDarkUntilTheyStop()
    {
        TestShip ship = NewShip();
        Run(ship, 0.1f);
        AssertGreaterThan(ship.PowerFlowMax, ship.PowerDraw, "setup: its reactors outrun its own draw");

        for (int i = 0; i < 3; ++i) // a beam emptying the store every half second, as Beam.Update does
        {
            ship.PowerCurrent = 0f;
            Run(ship, 0.5f);
            AssertGreaterThan(ship.PowerCurrent, 0f, "setup: the reactors refill it between beams");
            Assert.IsTrue(ship.LightsOut, "an emptied store is no power, though the reactors refill it");
        }

        for (float time = 0f; ship.LightsOut && time < 5f; time += TestSimStep.FixedTime)
            Run(ship, TestSimStep.FixedTime);
        Assert.IsFalse(ship.LightsOut, "the lights come back once the beams stop emptying it");
    }

    [TestMethod]
    public void AnEmptiedStoreDarkensAShipWithoutEnergyWeapons()
    {
        TestShip scout = SpawnShip("Vulcan Scout", Player, new Vector2(5000f, 1000f));
        Run(scout, 0.1f);
        AssertEqual(0.001f, 0f, scout.CheapestEnergyShot, "setup: its guns need no power");
        Assert.IsFalse(scout.LightsOut, "setup: lit");

        scout.PowerCurrent = 0f; // as beams or draw would leave it
        Run(scout, TestSimStep.FixedTime);
        AssertGreaterThan(scout.PowerCurrent, 0f, "setup: its reactor refilled some in the same tick");
        Assert.IsTrue(scout.LightsOut, "the store was empty before the refill, so the ship has no power");
    }

    [TestMethod]
    public void AStoreTooLowForAnyEnergyShotIsEmpty()
    {
        TestShip ship = NewShip();
        Run(ship, 0.1f);
        float cheapest = ship.Weapons.Where(w => w.PowerPerShot > 0f).Min(w => w.PowerPerShot);
        AssertEqual(0.001f, cheapest, ship.CheapestEnergyShot, "setup: its cheapest energy shot");

        ship.PowerCurrent = cheapest * 1.1f;
        Run(ship, TestSimStep.FixedTime);
        Assert.IsFalse(ship.LightsOut, "enough for a shot is power");

        ship.PowerCurrent = cheapest * 0.9f;
        Run(ship, TestSimStep.FixedTime);
        Assert.IsTrue(ship.LightsOut, "not enough for a single shot is no power, though the bar is not quite empty");
    }

    [TestMethod]
    public void AFullStoreIsPowerEvenIfItCannotPayForAShot()
    {
        TestShip ship = NewShip();
        Run(ship, 0.1f);
        ship.PowerStoreMax = ship.CheapestEnergyShot * 0.5f; // a design whose weapons outsize its store
        ship.PowerCurrent = ship.PowerStoreMax;
        Run(ship, TestSimStep.FixedTime);
        AssertEqual(0.001f, ship.CheapestEnergyShot * 0.5f, ship.PowerStoreMax, "setup: the small store held for the tick");
        Assert.IsFalse(ship.LightsOut, "a full store is power, or such a ship would never light up");
    }

    [TestMethod]
    public void UnpoweredEnergyWeaponsNeedNoPower()
    {
        TestShip ship = NewShip();
        Run(ship, 0.1f);
        AssertGreaterThan(ship.CheapestEnergyShot, 0f, "setup: it carries energy weapons");

        foreach (Weapon w in ship.Weapons.Where(w => w.PowerPerShot > 0f))
            w.Module.Powered = false;
        ship.ShipStatusChange();
        AssertEqual(0.001f, 0f, ship.CheapestEnergyShot, "an unpowered weapon cannot fire, so it sets no minimum");
    }

    [TestMethod]
    public void DestroyedEnergyWeaponsNeedNoPower()
    {
        TestShip ship = NewShip();
        Run(ship, 0.1f);
        AssertGreaterThan(ship.CheapestEnergyShot, 0f, "setup: it carries energy weapons");

        foreach (Weapon w in ship.Weapons.Where(w => w.PowerPerShot > 0f))
            w.Module.SetHealth(0f, "Test");
        Run(ship, 1.1f);
        AssertEqual(0.001f, 0f, ship.CheapestEnergyShot, "a destroyed weapon fires nothing, so it sets no minimum");
    }

    [TestMethod]
    public void AShipWithoutReactorsLeavesNoEngineTrail()
    {
        TestShip ship = NewShip();
        Run(ship, 0.1f);
        float fullSpeed = ship.VelocityMax;
        AssertGreaterThan(TrailParticles(ship, fullSpeed), 0, "setup: a ship at full speed leaves an engine trail");

        CutPower(ship);
        Assert.AreEqual(0, TrailParticles(ship, fullSpeed), "a ship drifting without reactors leaves no engine trail");
    }

    MiningStationVisualsTests.CountingParticle Exhaust;

    // engine trail particles from one frame of the ship flying at this speed
    int TrailParticles(Ship ship, float speed)
    {
        bool trails = GlobalStats.EnableEngineTrails;
        try
        {
            GlobalStats.EnableEngineTrails = true;
            if (Exhaust == null)
            {
                Exhaust = new();
                Universe.Particles = new ParticleManager(Content) { ThrustEffect = Exhaust, EngineTrail = Exhaust };
            }
            Exhaust.Added = 0;
            ship.Velocity = ship.Direction * speed;
            ship.UpdateThrusters(TestSimStep);
            return Exhaust.Added;
        }
        finally
        {
            GlobalStats.EnableEngineTrails = trails;
        }
    }

    [TestMethod]
    public void OnlyShipsOnScreenFollowTheirPower()
    {
        TestShip ship = NewShip();
        SetReactors(ship, working: false);
        ship.PowerCurrent = 0f;
        Run(ship, 1.5f, onScreen: false);
        AssertEqual(0.001f, 0f, ship.PowerFlowMax, "setup: the reactors are gone");
        Assert.IsFalse(ship.LightsOut, "nobody sees an off-screen ship's lights, so they are not updated");

        Run(ship, TestSimStep.FixedTime);
        Assert.IsTrue(ship.LightsOut, "on screen they go out at once");
    }

    [TestMethod]
    public void ReactorsWithoutBatteriesKeepTheLightsOn()
    {
        TestShip platform = SpawnShip("Platform Base mk1-a", Player, new Vector2(5000f, 5000f));
        Run(platform, 1f);
        AssertEqual(0.001f, 0f, platform.PowerStoreMax, "setup: no batteries");
        AssertGreaterThan(platform.PowerFlowMax, 0f, "setup: but working reactors");
        Assert.IsFalse(platform.LightsOut, "a ship running on its reactors alone has power");
    }

    [TestMethod]
    public void AnEMPDisabledShipStuttersUntilTheEMPWearsOff()
    {
        TestShip ship = NewShip();
        Run(ship, 1f);
        ship.CauseEmpDamage(ship.EmpTolerance * 10f);
        Universe.Particles = new ParticleManager(Content); // an EMP-disabled ship throws lightning
        Run(ship, 0.2f);
        Assert.IsTrue(ship.EMPDisabled, "setup: still EMP-disabled");
        Assert.IsFalse(ship.LightsOut, "an EMP leaves the reactors running");
        AssertStutters(LevelsOverAStutter(ship), "under EMP");

        ship.CauseEmpDamage(-ship.EmpTolerance * 20f);
        Assert.IsFalse(ship.EMPDisabled, "setup: the EMP wore off");
        AssertEqual(0.001f, 1f, ship.LightLevel(5f), "steady lights again");
    }

    [TestMethod]
    public void ADyingShipStutters()
    {
        TestShip ship = NewShip();
        Run(ship, 1f);
        ship.Dying = true;
        AssertStutters(LevelsOverAStutter(ship), "dying");
    }

    [TestMethod]
    public void AShipWithoutPowerStaysDarkUnderEMP()
    {
        TestShip ship = NewShip();
        CutPower(ship);
        ship.CauseEmpDamage(ship.EmpTolerance * 2f);
        Assert.IsTrue(LevelsOverAStutter(ship).All(l => l == 0f), "nothing left to stutter");
    }

    [TestMethod]
    public void ShipsDoNotStutterInStep()
    {
        TestShip a = NewShip(1000f);
        TestShip b = NewShip(3000f);
        a.CauseEmpDamage(a.EmpTolerance * 2f);
        b.CauseEmpDamage(b.EmpTolerance * 2f);
        Assert.IsFalse(LevelsOverAStutter(a).SequenceEqual(LevelsOverAStutter(b)), "each ship flickers in its own time");
    }

    [TestMethod]
    public void AShipDrawsItsLightsAtItsLightLevel()
    {
        TestShip ship = NewShip();
        SceneObject so = GiveSceneObject(ship);
        Run(ship, 0.1f);
        AssertEqual(0.001f, 1f, so.EmissiveScale, "a powered ship draws its lights");

        CutPower(ship);
        AssertEqual(0.001f, 0f, so.EmissiveScale, "a ship without power draws them dark");
    }

    [TestMethod]
    public void ADyingShipDrawsItsLightsStuttering()
    {
        Universe.Particles = new ParticleManager(Content); // a dying ship on screen throws sparks
        UState.ViewState = UniverseScreen.UnivScreenState.PlanetView;
        TestShip ship = NewShip();
        Run(ship, 0.1f);
        SceneObject so = GiveSceneObject(ship);
        ship.Dying = true;
        ship.SetDieTimer(10f);

        var drawn = new HashSet<float>();
        for (float time = 0f; time < Ship.StutterSeconds; time += TestSimStep.FixedTime)
        {
            ship.InFrustum = true;
            ship.KnownByEmpires.SetSeen(Player);
            Universe.CurrentSimTime += TestSimStep.FixedTime;
            ship.Update(TestSimStep);
            drawn.Add(so.EmissiveScale);
        }
        Assert.IsTrue(ship.IsVisibleToPlayer, "setup: the ship is on screen");
        AssertStutters(drawn.ToArray(), "drawn while dying");
    }
}
