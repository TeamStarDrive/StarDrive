using System;
using System.Reflection;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using SDGraphics;
using Ship_Game;
using Ship_Game.Gameplay;
using Ship_Game.Ships;
using Vector2 = SDGraphics.Vector2;

namespace UnitTests.Planets;

[TestClass]
public class SpinAndOrbitSpeedTests : StarDriveTest
{
    static readonly MethodInfo UpdatePosition = typeof(SolarSystemBody).GetMethod("UpdatePosition", BindingFlags.Instance | BindingFlags.NonPublic);

    public SpinAndOrbitSpeedTests()
    {
        LoadStarterShips("Platform Base mk1-a");
        CreateUniverseAndPlayerEmpire();
        UState.Paused = false;
    }

    static float Turned(float before, float after, float fullTurn) => ((after - before) % fullTurn + fullTurn) % fullTurn;

    static float SpinInOneSecond(Ship platform, int stepsPerSecond)
    {
        float before = platform.Rotation;
        var step = new FixedSimTime(1f / stepsPerSecond);
        for (int i = 0; i < stepsPerSecond; ++i)
            platform.UpdateShipStatus(step);
        return Turned(before, platform.Rotation, RadMath.TwoPI);
    }

    static float OrbitInOneSecond(Planet planet, int stepsPerSecond)
    {
        planet.OrbitalAngle = 0f; // near 360 degrees a float's rounding swamps steps this small
        float before = planet.OrbitalAngle;
        var step = new FixedSimTime(1f / stepsPerSecond);
        for (int i = 0; i < stepsPerSecond; ++i)
            UpdatePosition.Invoke(planet, new object[] { step });
        return Turned(before, planet.OrbitalAngle, 360f);
    }

    static float OrbitInOneSecond(Moon moon, int stepsPerSecond)
    {
        float before = moon.OrbitalAngle;
        var step = new FixedSimTime(1f / stepsPerSecond);
        for (int i = 0; i < stepsPerSecond; ++i)
            moon.UpdateVisibleMoon(step);
        return Turned(before, moon.OrbitalAngle, 360f);
    }

    [TestMethod]
    public void APlatformSpinsByGameTimeWhateverTheSimulationRate()
    {
        Ship platform = SpawnShip("Platform Base mk1-a", Player, new Vector2(200_000f, 200_000f));
        AssertEqual(0f, platform.VelocityMax, "setup: a platform cannot move");

        float at60 = SpinInOneSecond(platform, 60);
        AssertEqual(0.0001f, 60 * (0.003f + platform.Id % 10 * 0.0001f), at60, "at 60 steps a second it spins as it did per step");
        AssertEqual(0.0001f, at60, SpinInOneSecond(platform, 30), "a lower simulation rate, as the game picks in big battles, must not slow it");
        AssertEqual(0.0001f, at60, SpinInOneSecond(platform, 120), "a higher simulation rate, as below 1x game speed, must not speed it up");
    }

    [TestMethod]
    public void APlatformSpinningSlowlyGoesOnPastAFullTurn()
    {
        Ship platform = SpawnShip("Platform Base mk1-a", Player, new Vector2(200_000f, 200_000f));
        float perStep = (0.003f + platform.Id % 10 * 0.0001f) / 4f; // at 0.25x game speed a step turns a quarter of a 1x step
        platform.Rotation = RadMath.TwoPI - 0.002f;
        var step = new FixedSimTime(1f / 240f);
        for (int i = 0; i < 10; ++i)
            platform.UpdateShipStatus(step);
        AssertEqual(0.00002f, 10 * perStep - 0.002f, platform.Rotation, "it passes a full turn and keeps turning");
    }

    [TestMethod]
    public void APlanetOnScreenOrbitsByGameTimeWhateverTheSimulationRate()
    {
        Planet planet = AddDummyPlanet(new Vector2(200_000f, 200_000f));
        planet.System.InFrustum = true;

        float at60 = OrbitInOneSecond(planet, 60);
        AssertEqual(0.0001f, 60 * (float)Math.Asin(15.0 / planet.OrbitalRadius), at60, "at 60 steps a second it orbits as it did per step");
        AssertEqual(0.0001f, at60, OrbitInOneSecond(planet, 30), "a lower simulation rate must not slow it");
        AssertEqual(0.0001f, at60, OrbitInOneSecond(planet, 120), "a higher simulation rate must not speed it up");
    }

    [TestMethod]
    public void APlanetOffScreenStillMovesOneStepEveryFiveSecondsWhateverTheSimulationRate()
    {
        foreach (int stepsPerSecond in new[] { 30, 120 })
        {
            Planet planet = AddDummyPlanet(new Vector2(200_000f + stepsPerSecond * 1000f, 200_000f));
            AssertFalse(planet.System.InFrustum, "setup: off screen");
            planet.OrbitalAngle = 0f;
            var step = new FixedSimTime(1f / stepsPerSecond);
            for (int i = 0; i < 10 * stepsPerSecond; ++i)
                UpdatePosition.Invoke(planet, new object[] { step });
            AssertEqual(0.0001f, 2 * (float)Math.Asin(15.0 / planet.OrbitalRadius), planet.OrbitalAngle,
                        $"in 10 seconds at {stepsPerSecond} steps a second, two steps, as before");
        }
    }

    [TestMethod]
    public void AMoonOnScreenOrbitsByGameTimeWhateverTheSimulationRate()
    {
        Planet planet = AddDummyPlanet(new Vector2(200_000f, 200_000f));
        var moon = new Moon(planet.System, planet, moon: 1, moonScale: 1f, orbitRadius: 2000f, orbitalAngle: 0f, planet.Position);

        float at60 = OrbitInOneSecond(moon, 60);
        AssertEqual(0.0001f, 60 * (float)Math.Asin(15f / 2000f), at60, "at 60 steps a second it orbits as it did per step");
        AssertEqual(0.0001f, at60, OrbitInOneSecond(moon, 30), "a lower simulation rate must not slow it");
        AssertEqual(0.0001f, at60, OrbitInOneSecond(moon, 120), "a higher simulation rate must not speed it up");
    }
}
