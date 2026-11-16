using System.Reflection;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using SDGraphics;
using SDUtils;
using Ship_Game;
using Ship_Game.AI;
using Ship_Game.AI.ExpansionAI;
using Ship_Game.Commands.Goals;
using Ship_Game.Ships;
using Ship_Game.Universe.SolarBodies;
using Ship_Game.Utils;
using Vector2 = SDGraphics.Vector2;
#pragma warning disable CA2213

namespace UnitTests.Planets;

[TestClass]
public class StarRadiationTests : StarDriveTest
{
    const string Platform = "Platform Base mk1-a";
    const string Shipyard = "Shipyard";
    readonly Planet Colony;

    public StarRadiationTests()
    {
        LoadStarterShips(Platform, Shipyard);
        CreateUniverseAndPlayerEmpire();
        Player.ClearShipsWeCanBuild();
        UnlockAllTechsForShip(Player, Platform);
        UnlockAllTechsForShip(Player, Shipyard);
        Assert.IsNotNull(Player.BestPlatformWeCanBuild, "setup: the empire must be able to build a platform");

        Colony = AddHomeWorldToEmpire(new Vector2(200_000), Player);
        Colony.System.Sun = SunType.FindSun("star_neutron");
        Colony.HasSpacePort = true;
        Colony.CType = Planet.ColonyType.Core;
        Colony.GovOrbitals = true;
        Colony.ManualOrbitals = true;
        Colony.SetWantedPlatforms(12);
        Colony.SetWantedStations(0);
        Colony.SetWantedShipyards(0);
        Colony.SetManualSpaceDefBudget(1000);
    }

    int PlatformsOrdered => Colony.OrbitalsBeingBuilt(RoleName.platform);

    static float Integrity(Ship ship) => ship.Health + ship.ShieldPower;

    Planet AddPlanetAt(float distanceFromStar, SolarSystem system = null)
    {
        system ??= Colony.System;
        var p = new Planet(UState.CreateId(), system, system.Position + new Vector2(0f, distanceFromStar), fertility: 1f, minerals: 1f, maxPop: 4f);
        p.OrbitalAngle = p.Position.AngleToTarget(system.Position);
        p.TestSetOrbitalRadius(distanceFromStar);
        system.RingList.Add(new SolarSystem.Ring { Asteroids = false, OrbitalDistance = p.OrbitalRadius, Planet = p });
        system.PlanetList.Add(p);
        return p;
    }

    [TestMethod]
    public void AColonyInsideAStarsRadiationStillGetsOrbitalsFromItsGovernor()
    {
        Colony.TestSetOrbitalRadius(Colony.System.SunDangerRadius);

        Colony.DoGoverning();

        AssertEqual(1, PlatformsOrdered, "orbitals withstand the radiation, so the governor orders them as beside any star");
    }

    [TestMethod]
    public void AColonyInsideAStarsRadiationStillGetsAShipyardFromItsGovernor()
    {
        Assert.IsTrue(Player.CanBuildShip(Player.data.DefaultShipyard), "setup: the empire must be able to build its shipyard");
        Colony.SetWantedPlatforms(0);
        Colony.SetWantedShipyards(1);
        Colony.Population = Colony.MaxPopulation;
        Colony.UpdateIncomes();
        Colony.TestSetOrbitalRadius(Colony.System.SunDangerRadius);

        Colony.DoGoverning();

        AssertEqual(1, Colony.ShipyardsBeingBuilt(), "a shipyard withstands the radiation, so the governor orders one as beside any star");
    }

    [TestMethod]
    public void OrbitalsTetheredToAPlanetConstructorsAndBuilderShipsTakeNoRadiationDamage()
    {
        Planet inner = AddPlanetAt(3_000f);
        Vector2 spot = inner.Position + new Vector2(2_000f, 0f);
        Ship tethered = SpawnShip(Platform, Player, spot);
        tethered.TetherToPlanet(inner);
        Ship loose = SpawnShip(Platform, Player, spot);
        Ship constructor = SpawnShipNoCombatHoldPos("Terran Constructor", Player, spot);
        Ship builder = SpawnShipNoCombatHoldPos("Supply Shuttle", Player, spot);
        float[] before = { Integrity(tethered), Integrity(loose), Integrity(constructor), Integrity(builder) };

        RunObjectsSim(3f);

        foreach (Ship ship in new[] { tethered, loose, constructor, builder })
        {
            Assert.IsTrue(Colony.System.ShipWithinRadiationRadius(ship), $"setup: {ship.Name} stays inside the radiation");
            Assert.IsTrue(ship.Active && !ship.IsLaunchingOrLanding, $"setup: {ship.Name} stays in space, where the radiation reaches it");
        }
        AssertGreaterThan(before[1], Integrity(loose), "setup: an orbital not tethered to a planet is worn down by the radiation");
        AssertEqual(0.01f, before[0], Integrity(tethered), "an orbital tethered to a planet withstands the radiation");
        AssertEqual(0.01f, before[2], Integrity(constructor), "a constructor withstands the radiation");
        AssertEqual(0.01f, before[3], Integrity(builder), "a builder ship withstands the radiation");
    }

    [TestMethod]
    public void APlanetInsideAStarsRadiationCannotBeColonized()
    {
        Planet inside = AddPlanetAt(10_000f);
        Planet outside = AddPlanetAt(Colony.System.SunDangerRadius + 30_000f);
        Assert.IsTrue(inside.Habitable && outside.Habitable, "setup: both planets are habitable");

        Assert.AreEqual(GoalStep.GoalFailed, new MarkForColonization(inside, Player, isManual: true).Evaluate(),
                        "an order to colonize a planet inside the radiation is refused");
        Assert.AreNotEqual(GoalStep.GoalFailed, new MarkForColonization(outside, Player, isManual: true).Evaluate(),
                           "setup: the same order for a planet outside it stands");
    }

    static void AdvanceSun(SolarSystem system, float seconds)
    {
        var step = new FixedSimTime(1f / 30f);
        for (int i = 0; i < (int)(seconds * 30f); ++i)
            foreach (SunLayerState layer in system.SunLayers)
                layer.Update(step);
    }

    [TestMethod]
    public void AFlareStarsRadiationComesOnlyWithItsFlares()
    {
        var system = new SolarSystem(UState, new Vector2(600_000)) { Sun = SunType.FindSun("star_flare_green") };
        UState.AddSolarSystem(system);
        Ship ship = SpawnShip(Platform, Player, system.Position + new Vector2(0, 3000));
        float whole = Integrity(ship);
        MethodInfo radiate = typeof(SolarSystem).GetMethod("ApplySolarRadiationDamage", BindingFlags.NonPublic | BindingFlags.Instance)!;

        AdvanceSun(system, 6f);
        for (int i = 0; i < 20; ++i)
            radiate.Invoke(system, new object[] { ship });
        AssertEqual(0.01f, whole, Integrity(ship), "between flares the star does not hurt a ship near it");

        AdvanceSun(system, 4f);
        for (int i = 0; i < 20; ++i)
            radiate.Invoke(system, new object[] { ship });
        AssertLessThan(Integrity(ship), whole, "at a flare's peak it does");
    }

    [TestMethod]
    public void ADysonSwarmIsNeverSetAtARadiatingStarEvenOneWithHabitablePlanets()
    {
        SunType flare = SunType.FindSun("star_flare_green");
        Assert.IsTrue(flare.RadiationDamage > 0, "setup: the green flare star radiates");
        SolarSystem AddSystemWithThreeHabitablePlanets(SolarSystem system)
        {
            for (int i = 0; i < 3; ++i)
                AddPlanetAt(30_000f + i * 15_000f, system);
            UState.AddSolarSystem(system);
            return system;
        }

        SolarSystem flareSystem = AddSystemWithThreeHabitablePlanets(new(UState, new Vector2(600_000, 600_000)) { Sun = flare });
        UState.GeneratePotentialDysonSwarms();
        Assert.AreEqual(0, (int)flareSystem.DysonSwarmType, "a radiating star gets no swarm, even as the only system that could hold one");

        SolarSystem calm = AddSystemWithThreeHabitablePlanets(SystemBesideAHarmlessStar(600_000f));
        UState.GeneratePotentialDysonSwarms();
        Assert.AreNotEqual(0, (int)calm.DysonSwarmType, "setup: the same system beside a harmless star gets one");
    }

    [TestMethod]
    public void NoSunDrawnForOrdinarySystemsRadiates()
    {
        Assert.IsTrue(SunType.HabitableSuns.Length > 10 && SunType.MultiSuns.Length > 0, "setup: the suns drawn for ordinary systems are loaded");
        foreach (SunType[] pool in new[] { SunType.HabitableSuns, SunType.MultiSuns })
            foreach (SunType sun in pool)
                AssertEqual(0.001f, 0f, sun.DangerRadius, $"{sun.Id} is drawn for ordinary systems, whose planets and colonies sit close to their star, so it must not radiate");
    }

    SolarSystem SystemBesideAHarmlessStar(float x)
        => new(UState, new Vector2(x, -1_000_000f)) { Sun = SunType.FindSun("star_yellow") };

    Planet AddGasGiantAt(float distanceFromStar, SolarSystem system)
    {
        Planet p = AddPlanetAt(distanceFromStar, system);
        p.GenerateNewFromPlanetType(new SeededRandom(), ResourceManager.Planets.RandomPlanet(PlanetCategory.GasGiant), scale: 1.5f, preDefinedPop: 0);
        Assert.IsFalse(p.Habitable, "setup: a gas giant is not habitable");
        return p;
    }

    [TestMethod]
    public void ANewSystemWithAHabitablePlanetInsideTheRadiationIsNotGivenAHazardousStar()
    {
        SunType neutron = SunType.FindSun("star_neutron");
        SolarSystem near = SystemBesideAHarmlessStar(0f);
        Assert.IsTrue(AddPlanetAt(10_000f, near).Habitable, "setup: the planet is habitable");

        Assert.IsFalse(near.TrySetHazardousSun(neutron), "a habitable planet inside the radiation keeps the hazardous star away");
        Assert.AreEqual("star_yellow", near.Sun.Id, "the system keeps its star");

        SolarSystem far = SystemBesideAHarmlessStar(1_000_000f);
        AddPlanetAt(neutron.DangerRadius + 10_000f, far);
        Assert.IsTrue(far.TrySetHazardousSun(neutron), "setup: a habitable planet outside the radiation does not");
    }

    [TestMethod]
    public void ANewHazardousStarLeavesNoExoticPlanetInsideItsRadiation()
    {
        SunType neutron = SunType.FindSun("star_neutron");
        SolarSystem system = SystemBesideAHarmlessStar(0f);
        Planet inside = AddGasGiantAt(10_000f, system);
        Planet outside = AddGasGiantAt(neutron.DangerRadius + 10_000f, system);
        foreach (Planet p in new[] { inside, outside })
        {
            p.SetResearchable(true, UState);
            p.Mining = new(p);
            p.MineralRichness = p.Mining.Richness;
        }

        Assert.IsTrue(system.TrySetHazardousSun(neutron), "setup: no habitable planet keeps the hazardous star away");
        Assert.IsFalse(inside.IsResearchable || inside.IsMineable, "a planet inside the radiation is neither researchable nor mineable");
        Assert.IsFalse(UState.MineablePlanets.ContainsRef(inside) || UState.ResearchableSolarBodies.ContainsKey(inside),
                       "and the universe no longer lists it as exotic");
        AssertEqual(0f, 0f, inside.MineralRichness, "nor does it show the richness of a resource it no longer has");
        Assert.IsTrue(outside.IsResearchable && outside.IsMineable, "a planet outside the radiation keeps its exotic resources");
    }

    [TestMethod]
    public void TheAIDoesNotPickAPlanetInsideAStarsRadiationToColonize()
    {
        Planet inside = AddPlanetAt(10_000f);
        Planet outside = AddPlanetAt(Colony.System.SunDangerRadius + 30_000f);
        inside.SetExploredBy(Enemy);
        outside.SetExploredBy(Enemy);
        MethodInfo canBeColonized = typeof(ExpansionPlanner).GetMethod("CanBeColonized", BindingFlags.Instance | BindingFlags.NonPublic,
                                                                        null, new[] { typeof(Planet) }, null)!;

        Assert.IsTrue((bool)canBeColonized.Invoke(Enemy.AI.ExpansionAI, new object[] { outside }), "setup: the AI may colonize the planet outside");
        Assert.IsFalse((bool)canBeColonized.Invoke(Enemy.AI.ExpansionAI, new object[] { inside }), "the AI must not pick the planet inside the radiation");
    }
}
