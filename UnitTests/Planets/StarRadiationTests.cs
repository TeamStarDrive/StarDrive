using System.Reflection;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using SDGraphics;
using Ship_Game;
using Ship_Game.AI;
using Ship_Game.AI.ExpansionAI;
using Ship_Game.Commands.Goals;
using Ship_Game.Ships;
using Ship_Game.Universe.SolarBodies;
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

    Planet AddPlanetAt(float distanceFromStar)
    {
        SolarSystem system = Colony.System;
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
