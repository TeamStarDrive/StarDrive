using Microsoft.VisualStudio.TestTools.UnitTesting;
using SDGraphics;
using SDUtils;
using Ship_Game;
using Ship_Game.Fleets;
using Ship_Game.Ships;
using Vector2 = SDGraphics.Vector2;

namespace UnitTests.Fleets;

[TestClass]
public class FleetFacingTests : StarDriveTest
{
    const string Design = "Terran-Prototype";
    static readonly Vector2 FleetPos = new(200_000f, 200_000f);

    public FleetFacingTests()
    {
        LoadStarterShips(Design);
        CreateUniverseAndPlayerEmpire();
        UState.Events.Disabled = true;
    }

    Fleet EmptyFleetFacingRight()
    {
        Fleet fleet = Player.CreateFleet(1, null);
        fleet.FinalPosition = FleetPos;
        fleet.FinalDirection = Vectors.Right;
        return fleet;
    }

    [TestMethod]
    public void AShipJoiningLaterTakesItsSlotTurnedToTheFleetsFacing()
    {
        Fleet fleet = EmptyFleetFacingRight();
        var ahead = new FleetDataNode { ShipName = Design, RelativeFleetOffset = new Vector2(0f, -1500f) };
        fleet.DataNodes.Add(ahead);
        Ship ship = SpawnShip(Design, Player, FleetPos + new Vector2(20_000f, 0f));

        fleet.AddExistingShip(ship, ahead);

        AssertEqual(1f, new Vector2(1500f, 0f), ship.FleetOffset, "the slot ahead of a fleet facing right lies to its right");
    }

    [TestMethod]
    public void AutoArrangeKeepsTheFleetsFacing()
    {
        Fleet fleet = EmptyFleetFacingRight();
        var ships = new Array<Ship>();
        for (int i = 0; i < 6; ++i)
            ships.Add(SpawnShip(Design, Player, FleetPos + new Vector2(i * 300f, 0f)));
        fleet.AddShipsJoiningFleet(ships);

        fleet.AutoArrange();

        Assert.IsTrue(ships.Any(s => s.RelativeFleetOffset.Length() > 100f), "setup: the arranged fleet has ships off its centre");
        foreach (Ship s in ships)
        {
            Vector2 turnedRight = new(-s.RelativeFleetOffset.Y, s.RelativeFleetOffset.X);
            AssertEqual(1f, turnedRight, s.FleetOffset, "an arranged fleet facing right keeps its formation turned right");
        }
        AssertEqual(1f, Vectors.Right, fleet.FinalDirection, "and keeps facing right");
    }
}
