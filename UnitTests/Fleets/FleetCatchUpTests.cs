using System.Collections.Generic;
using System.Reflection;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using SDUtils;
using Ship_Game;
using Ship_Game.Fleets;
using Ship_Game.Ships;
using Vector2 = SDGraphics.Vector2;

namespace UnitTests.Fleets;

[TestClass]
public class FleetCatchUpTests : StarDriveTest
{
    static readonly Vector2 BodyPos = new(200_000f, 200_000f);
    static readonly Vector2 FarPos = new(-1_200_000f, 200_000f);

    public FleetCatchUpTests()
    {
        LoadStarterShips("Terran-Prototype");
        CreateUniverseAndPlayerEmpire();
        UState.Events.Disabled = true;
    }

    Array<Ship> SpawnShips(int count, Vector2 pos)
    {
        var ships = new Array<Ship>();
        for (int i = 0; i < count; ++i)
            ships.Add(SpawnShip("Terran-Prototype", Player, pos + new Vector2(i * 300f, 0f)));
        return ships;
    }

    static void JoinFleet(Fleet fleet, IReadOnlyList<Ship> ships)
    {
        fleet.AddShipsJoiningFleet(ships);
        fleet.AutoArrange();
        fleet.Update(FixedSimTime.Zero);
    }

    Fleet FleetAt(Vector2 pos, out Array<Ship> ships)
    {
        ships = SpawnShips(10, pos);
        Fleet fleet = Player.CreateFleet(1, null);
        JoinFleet(fleet, ships);
        return fleet;
    }

    [TestMethod]
    public void AShipAddedFarFromItsFleetCatchesUpWithoutPullingTheFleetAway()
    {
        Fleet fleet = FleetAt(BodyPos, out Array<Ship> body);
        Vector2 centre = fleet.AveragePosition(force: true);

        Ship newcomer = SpawnShip("Terran-Prototype", Player, FarPos);
        JoinFleet(fleet, new[] { newcomer });

        Assert.IsTrue(newcomer.CatchingUpToFleet, "a ship added far from its fleet catches up on its own");
        Assert.IsTrue(fleet.AveragePosition(force: true).InRadius(centre, 5_000f),
                      $"the far ship must not drag the fleet's centre ({fleet.AveragePosition(force: true).Distance(centre):0} moved)");
        Assert.IsTrue(fleet.FinalPosition.InRadius(centre, 5_000f),
                      $"the fleet must not be sent toward the far ship ({fleet.FinalPosition.Distance(centre):0} away)");
        foreach (Ship ship in body)
            Assert.IsTrue(fleet.IsShipInFormation(ship, 30_000), "the fleet's own ships stay in formation, so they keep their speed");
    }

    [TestMethod]
    public void AFleetBuiltOneShipAtATimeFormsAroundItsMainGroup()
    {
        Ship farFirst = SpawnShip("Terran-Prototype", Player, FarPos);
        Fleet fleet = Player.CreateFleet(1, null);
        fleet.AddShipsJoiningFleet(new[] { farFirst });
        Array<Ship> body = SpawnShips(5, BodyPos);
        foreach (Ship ship in body)
            fleet.AddShipsJoiningFleet(new[] { ship });

        Assert.IsTrue(farFirst.CatchingUpToFleet, "the ship added first, far from the rest, catches up to them");
        foreach (Ship ship in body)
            Assert.IsFalse(ship.CatchingUpToFleet, "the main group is not catching up, though it joined after the far ship");
    }

    [TestMethod]
    public void AShipCatchingUpIsStillCatchingUpAfterALoad()
    {
        Fleet fleet = FleetAt(BodyPos, out Array<Ship> body);
        Ship newcomer = SpawnShip("Terran-Prototype", Player, FarPos);
        JoinFleet(fleet, new[] { newcomer });
        foreach (Ship ship in fleet.Ships)
            ship.CatchingUpToFleet = false;

        typeof(Fleet).GetMethod("OnDeserialized", BindingFlags.Instance | BindingFlags.NonPublic)!.Invoke(fleet, null);
        fleet.Update(FixedSimTime.Zero);

        Assert.IsTrue(newcomer.CatchingUpToFleet, "the far ship is still catching up after the load");
        foreach (Ship ship in body)
        {
            Assert.IsFalse(ship.CatchingUpToFleet, "the fleet's own ships are not");
            Assert.IsTrue(fleet.IsShipInFormation(ship, 30_000), "and stay in formation, so they keep their speed");
        }
    }
}
