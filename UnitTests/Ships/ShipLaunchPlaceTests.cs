using Microsoft.VisualStudio.TestTools.UnitTesting;
using SDGraphics;
using Ship_Game;
using Ship_Game.Commands.Goals;
using Ship_Game.Ships;
using Vector2 = SDGraphics.Vector2;
#pragma warning disable CA2213

namespace UnitTests.Ships;

/// <summary>
/// A planet with shipyards launches its new ships from a random one of them; only a planet without
/// shipyards launches them from the planet itself.
/// </summary>
[TestClass]
public class ShipLaunchPlaceTests : StarDriveTest
{
    readonly Planet Homeworld;

    public ShipLaunchPlaceTests()
    {
        LoadStarterShips("Shipyard", "Vulcan Scout");
        CreateUniverseAndPlayerEmpire();
        Homeworld = AddHomeWorldToEmpire(new Vector2(200_000), Player, new Vector2(205_000), explored: true);
    }

    Ship AddShipyard(Vector2 offset, Empire owner = null)
    {
        Ship shipyard = SpawnShip("Shipyard", owner ?? Player, Homeworld.Position + offset);
        shipyard.TetherToPlanet(Homeworld);
        return shipyard;
    }

    [TestMethod]
    public void APlanetWithShipyardsLaunchesOnlyFromThem()
    {
        Ship north = AddShipyard(new Vector2(0, -2500));
        Ship south = AddShipyard(new Vector2(0, 2500));
        bool usedNorth = false, usedSouth = false;
        for (int i = 0; i < 200; ++i)
        {
            Vector2 at = Homeworld.GetBuilderShipTargetVector(launch: true, out bool fromShipyard);
            Assert.IsTrue(fromShipyard, "a planet with shipyards must never launch from the planet");
            if (at.InRadius(north.Position, 51f)) usedNorth = true;
            else if (at.InRadius(south.Position, 51f)) usedSouth = true;
            else Assert.Fail("a launch must start at one of the planet's shipyards");
        }
        Assert.IsTrue(usedNorth && usedSouth, "every shipyard must get its share of the launches");

        Vector2 returnTo = Homeworld.GetBuilderShipTargetVector(launch: false, out _);
        Assert.IsTrue(returnTo == north.Position || returnTo == south.Position, "a builder shuttle returns to a shipyard");
    }

    [TestMethod]
    public void APlanetWithoutShipyardsLaunchesFromThePlanet()
    {
        Vector2 at = Homeworld.GetBuilderShipTargetVector(launch: true, out bool fromShipyard);
        Assert.IsFalse(fromShipyard, "a planet without shipyards launches from the planet");
        AssertEqual(0.01f, Homeworld.Position, at, "a planet without shipyards launches from the planet");
    }

    [TestMethod]
    public void AShipyardThatIsDyingOrNotOursIsNeverUsed()
    {
        Ship dying = AddShipyard(new Vector2(0, -2500));
        dying.Dying = true;
        AddShipyard(new Vector2(0, 2500), Enemy);

        for (int i = 0; i < 50; ++i)
        {
            Homeworld.GetBuilderShipTargetVector(launch: true, out bool fromShipyard);
            Assert.IsFalse(fromShipyard, "a dying shipyard or one that is not ours must not launch our ships");
        }
    }

    [TestMethod]
    public void ANewlyBuiltShipLaunchesFromAShipyard()
    {
        Ship shipyard = AddShipyard(new Vector2(0, 2500));
        var goal = new RefitShip(Player);
        var build = new QueueItem(Homeworld)
        {
            isShip = true,
            ShipData = ResourceManager.Ships.GetDesign("Vulcan Scout"),
            Cost = 1f,
            Goal = goal,
            QType = QueueItemType.CombatShip,
        };
        Homeworld.Construction.EnqueueRefitShip(build);
        build.ProductionSpent = build.ActualCost;
        Homeworld.ProdHere = 10f;
        Player.AddMoney(1000f);
        Homeworld.Construction.RushProduction(0, 1f, rushButton: true);

        Assert.IsNotNull(goal.FinishedShip, "setup: the ship must be built");
        Assert.IsTrue(goal.FinishedShip.IsLaunching, "the new ship launches");
        AssertLessThan(goal.FinishedShip.Position.Distance(shipyard.Position), 51f, "the new ship launches from the shipyard");
    }
}
