using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Ship_Game;
using Ship_Game.AI;
using Ship_Game.Ships;
using Vector2 = SDGraphics.Vector2;
#pragma warning disable CA2213

namespace UnitTests.Planets;

[TestClass]
public class PlanetTroopLoadingTests : StarDriveTest
{
    const string Carrier = "TEST_Excalibur-Class Supercarrier";
    readonly Planet Homeworld;
    readonly Troop[] Troops = new Troop[3];

    public PlanetTroopLoadingTests()
    {
        CreateUniverseAndPlayerEmpire();
        LoadStarterShips(Player.data.DefaultTroopShip, Carrier);
        UState.Objects.EnableParallelUpdate = false;
        Homeworld = AddHomeWorldToEmpire(new Vector2(200_000), Player, new Vector2(205_000), explored: true);
        Homeworld.AutoBuildTroops = true; // no garrison held back
        for (int i = 0; i < Troops.Length; ++i)
        {
            Troops[i] = ResourceManager.CreateTroop("Wyvern", Player);
            Assert.IsTrue(Troops[i].TryLandTroop(Homeworld), "setup: the planet must hold troops");
            Troops[i].UpdateMoveActions(Troops[i].MaxStoredActions);
        }
    }

    Ship EmptyTroopShip(float distance)
    {
        Ship ship = SpawnShip(Player.data.DefaultTroopShip, Player, Homeworld.Position + new Vector2(Homeworld.Radius + distance, 0));
        ship.AI.OrderHoldPosition(MoveOrder.HoldPosition);
        return ship;
    }

    Ship[] Launched() => Troops.Select(t => t.HostShip).Where(s => s != null).ToArray();

    [TestMethod]
    public void APlanetSendsEachTroopShipOnlyTheTroopsItHasRoomFor()
    {
        Ship first = EmptyTroopShip(1000);
        Ship second = EmptyTroopShip(2000);
        RunObjectsSim(TestSimStep);
        RunObjectsSim(TestSimStep);
        Assert.IsTrue(Homeworld.System.ShipList.Contains(first) && Homeworld.System.ShipList.Contains(second), "setup: both troop ships are in the planet's system");
        AssertEqual(0, first.TroopCount + second.TroopCount, "setup: both troop ships are empty");
        AssertEqual(1, first.TroopCapacity, "setup: a troop ship holds one troop");
        AssertEqual(3, Homeworld.NumTroopsCanLaunch, "setup: the planet can launch its three troops");
        AssertEqual(0, Launched().Length, "setup: no troop has launched yet");

        Homeworld.GeodeticManager.AffectNearbyShips();

        Ship[] launched = Launched();
        AssertEqual(2, launched.Length, "the planet launches a troop for each empty troop ship and keeps the third");
        AssertEqual(1, launched.Count(s => s.AI.EscortTarget == first), "one troop flies to the first troop ship");
        AssertEqual(1, launched.Count(s => s.AI.EscortTarget == second), "and one to the second");

        RunObjectsSim(TestSimStep);
        Homeworld.GeodeticManager.AffectNearbyShips();
        AssertEqual(2, Launched().Length, "the troops already flying to the ships count, so the next turn sends none");
    }

    [TestMethod]
    public void APlanetFillsACarriersRoomAndKeepsTheRest()
    {
        Ship carrier = SpawnShip(Carrier, Player, Homeworld.Position + new Vector2(Homeworld.Radius + 1000, 0));
        carrier.AI.OrderHoldPosition(MoveOrder.HoldPosition);
        carrier.KillAllTroops();
        for (int i = 0; i < carrier.TroopCapacity - 2; ++i)
            ResourceManager.CreateTroop("Wyvern", Player).LandOnShip(carrier);
        RunObjectsSim(TestSimStep);
        RunObjectsSim(TestSimStep);
        Assert.IsTrue(Homeworld.System.ShipList.Contains(carrier), "setup: the carrier is in the planet's system");
        AssertEqual(2, carrier.TroopCapacity - carrier.Carrier.NumTroopsInShipAndInSpace, "setup: the carrier has room for two troops");
        AssertEqual(3, Homeworld.NumTroopsCanLaunch, "setup: the planet can launch its three troops");

        Homeworld.GeodeticManager.AffectNearbyShips();

        Ship[] launched = Launched();
        AssertEqual(2, launched.Length, "the planet launches two troops and keeps the third");
        AssertEqual(2, launched.Count(s => s.AI.EscortTarget == carrier), "both fly to the carrier");
    }
}
