using Microsoft.VisualStudio.TestTools.UnitTesting;
using Ship_Game;
using Ship_Game.Ships;
using Ship_Game.Universe.SolarBodies;
using Vector2 = SDGraphics.Vector2;
#pragma warning disable CA2213

namespace UnitTests.Planets;

[TestClass]
public class GovernorOrbitalRadiationTests : StarDriveTest
{
    const string Platform = "Platform Base mk1-a";
    const string Shipyard = "Shipyard";
    readonly Planet Colony;

    public GovernorOrbitalRadiationTests()
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

    void AddPlatforms(int count)
    {
        for (int i = 0; i < count; ++i)
            Colony.OrbitalStations.Add(SpawnShip(Platform, Player, Colony.Position + new Vector2(3000 + i * 50)));
    }

    [TestMethod]
    public void AColonyInsideAStarsRadiationGetsNoOrbitalsFromItsGovernor()
    {
        Colony.TestSetOrbitalRadius(Colony.System.SunDangerRadius);
        AssertEqual(0, Colony.OrbitalRingsClearOfRadiation, "setup: every ring must pass through the radiation");

        Colony.DoGoverning();
        AssertEqual(0, PlatformsOrdered, "the governor must not order an orbital the star would wear down");

        Colony.System.Sun = SunType.FindSun("star_yellow");
        Colony.DoGoverning();
        AssertEqual(1, PlatformsOrdered, "the same governor orders one beside a harmless star");
    }

    [TestMethod]
    public void AColonyInsideAStarsRadiationGetsNoShipyardFromItsGovernor()
    {
        Assert.IsTrue(Player.CanBuildShip(Player.data.DefaultShipyard), "setup: the empire must be able to build its shipyard");
        Colony.SetWantedPlatforms(0);
        Colony.SetWantedShipyards(1);
        Colony.Population = Colony.MaxPopulation;
        Colony.UpdateIncomes();
        Colony.TestSetOrbitalRadius(Colony.System.SunDangerRadius);

        Colony.DoGoverning();
        AssertEqual(0, Colony.ShipyardsBeingBuilt(), "the governor must not order a shipyard the star would wear down");

        Colony.System.Sun = SunType.FindSun("star_yellow");
        Colony.DoGoverning();
        AssertEqual(1, Colony.ShipyardsBeingBuilt(), "the same governor orders one beside a harmless star");
    }

    [TestMethod]
    public void TheGovernorStopsWhenTheRingsClearOfRadiationAreFull()
    {
        Colony.TestSetOrbitalRadius(Colony.System.SunDangerRadius + (Colony.OrbitalRingRadius(0) + Colony.OrbitalRingRadius(1)) / 2);
        AssertEqual(1, Colony.OrbitalRingsClearOfRadiation, "setup: only the nearest ring must stay clear all the way round");

        AddPlatforms(Planet.OrbitalsPerRing);
        Colony.DoGoverning();
        AssertEqual(0, PlatformsOrdered, "a full clear ring must not overflow into the radiation");

        Colony.OrbitalStations.RemoveAt(Colony.OrbitalStations.Count - 1);
        Colony.DoGoverning();
        AssertEqual(1, PlatformsOrdered, "a free place in the clear ring is filled");
    }
}
