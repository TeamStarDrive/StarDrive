using Microsoft.VisualStudio.TestTools.UnitTesting;
using Ship_Game;
using UnitTests.Ships;
using Vector2 = SDGraphics.Vector2;

namespace UnitTests.Planets;

/// <summary>
/// A ship of ours scans a planet's surface within its own sensor range, however far that reaches, and without the extra
/// reach a ship has for spotting ships inside friendly projector influence.
/// </summary>
[TestClass]
public class PlanetSurfaceScanTests : StarDriveTest
{
    Planet Home;

    public PlanetSurfaceScanTests()
    {
        LoadStarterShips("TEST_Vulcan Scout");
        CreateUniverseAndPlayerEmpire();
        UState.Objects.EnableParallelUpdate = false;
        Home = AddHomeWorldToEmpire(new Vector2(200_000), Player);
    }

    TestShip Scout(Vector2 at)
    {
        TestShip scout = SpawnShipNoCombatHoldPos("TEST_Vulcan Scout", Player, at);
        UState.Objects.Update(new(time: 1f));
        return scout;
    }

    Planet PlanetAt(Vector2 at) => AddDummyPlanet(at, 1f, 1f, 4f, at, explored: true);

    [TestMethod]
    public void AShipScansAPlanetWithinItsSensorRange()
    {
        TestShip scout = Scout(new Vector2(-600_000));
        Planet target = PlanetAt(scout.Position + new Vector2(0, scout.SensorRange - 5000));

        Assert.IsTrue(target.OurShipsCanScanSurface(Player), "a planet within the scout's sensor range is scanned");
    }

    [TestMethod]
    public void InOurBordersAShipScansAPlanetOnlyWithinItsSensorRangeThoughItSpotsShipsFarther()
    {
        TestShip scout = Scout(Home.Position);
        Assert.IsTrue(scout.IsInFriendlyProjectorRange, "setup: the scout is in our borders");
        float beyondSensors = scout.SensorRange + 5000;
        Assert.IsTrue(scout.AI.GetSensorRadius() > beyondSensors, "setup: there it spots ships beyond its sensor range");
        Planet target = PlanetAt(scout.Position + new Vector2(0, beyondSensors));

        Assert.IsFalse(target.OurShipsCanScanSurface(Player), "a planet beyond the scout's own sensor range is not scanned");
    }

    [TestMethod]
    public void AShipWithVeryLongSensorsScansAPlanetFarAway()
    {
        TestShip scout = Scout(new Vector2(-600_000));
        scout.SensorRange = 400_000;
        Planet target = PlanetAt(scout.Position + new Vector2(0, 300_000));

        Assert.IsTrue(target.OurShipsCanScanSurface(Player), "no search radius cuts off a ship whose sensors reach the planet");
    }
}
