using Microsoft.VisualStudio.TestTools.UnitTesting;
using Ship_Game;
using Ship_Game.AI;
using Ship_Game.Ships;
using Ship_Game.Universe.SolarBodies;
using Vector2 = SDGraphics.Vector2;
#pragma warning disable CA2213

namespace UnitTests.Planets;

// Wanted numbers from older saves, or orbitals the player built, can take a colony past the orbitals
// cap, so the governor's own orders must obey it too, without giving up on upgrading what it has
[TestClass]
public class GovernorOrbitalCapTests : StarDriveTest
{
    const string Platform = "Platform Base mk1-a";
    const string Shipyard = "Shipyard";
    readonly Planet Colony;

    public GovernorOrbitalCapTests()
    {
        LoadStarterShips(Platform, Shipyard);
        CreateUniverseAndPlayerEmpire();
        Player.ClearShipsWeCanBuild();
        UnlockAllTechsForShip(Player, Platform);
        UnlockAllTechsForShip(Player, Shipyard);
        Assert.IsNotNull(Player.BestPlatformWeCanBuild, "setup: the empire must be able to build a platform");

        Colony = AddHomeWorldToEmpire(new Vector2(200_000), Player);
        Colony.System.Sun = SunType.FindSun("star_yellow");
        Colony.HasSpacePort = true;
        Colony.CType = Planet.ColonyType.Core;
        Colony.GovOrbitals = true;
        Colony.ManualOrbitals = true;
        Colony.SetWantedStations(0);
        Colony.SetManualSpaceDefBudget(1000);
        Colony.Population = Colony.MaxPopulation;
        Colony.UpdateIncomes();

        for (int i = 0; i < ShipBuilder.OrbitalsLimit; ++i)
            SpawnShip(Platform, Player, Colony.Position + new Vector2(3000 + i * 50)).TetherToPlanet(Colony);
    }

    [TestMethod]
    public void TheGovernorOrdersNoPlatformPastTheCap()
    {
        Colony.SetWantedPlatforms(ShipBuilder.OrbitalsLimit + 1);
        Colony.SetWantedShipyards(0);

        Colony.DoGoverning();
        AssertEqual(0, Colony.OrbitalsBeingBuilt(RoleName.platform), "the governor ordered a platform past the orbitals cap");

        Colony.OrbitalStations.RemoveAt(Colony.OrbitalStations.Count - 1);
        Colony.DoGoverning();
        AssertEqual(1, Colony.OrbitalsBeingBuilt(RoleName.platform), "the same governor fills a free orbital slot");
    }

    [TestMethod]
    public void AtTheCapTheGovernorStillUpgradesItsOrbitals()
    {
        Colony.SetWantedPlatforms(ShipBuilder.OrbitalsLimit + 1);
        Colony.SetWantedShipyards(0);
        Ship weakest = Colony.OrbitalStations[0];
        weakest.BaseStrength = Player.BestPlatformWeCanBuild.BaseStrength / 2;
        Player.UpdateRallyPoints(); // the refit needs a safe space port to be built at

        Colony.DoGoverning();
        Assert.IsTrue(Player.AI.HasGoal(g => g.Type == GoalType.RefitOrbital && g.OldShip == weakest),
            "at the orbitals cap the governor stopped upgrading its orbitals");
    }

    [TestMethod]
    public void TheGovernorOrdersNoShipyardPastTheCap()
    {
        Assert.IsTrue(Player.CanBuildShip(Player.data.DefaultShipyard), "setup: the empire must be able to build its shipyard");
        Colony.SetWantedPlatforms(ShipBuilder.OrbitalsLimit);
        Colony.SetWantedShipyards(1);

        Colony.DoGoverning();
        AssertEqual(0, Colony.ShipyardsBeingBuilt(), "the governor ordered a shipyard past the orbitals cap");

        Colony.OrbitalStations.RemoveAt(Colony.OrbitalStations.Count - 1);
        Colony.SetWantedPlatforms(ShipBuilder.OrbitalsLimit - 1);
        Colony.DoGoverning();
        AssertEqual(1, Colony.ShipyardsBeingBuilt(), "the same governor puts a shipyard in a free orbital slot");
    }
}
