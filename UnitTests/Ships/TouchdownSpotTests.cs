using System;
using System.Reflection;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using SDGraphics;
using SDUtils;
using Ship_Game;
using Ship_Game.Ships;
using Vector2 = SDGraphics.Vector2;
#pragma warning disable CA2213

namespace UnitTests.Ships;

/// <summary>
/// Landing ships touch down at random spots so they do not all go down at the same place: within 100 units of
/// the centre of the planet, shipyard, space port or station they land on. A planet landing follows the planet
/// along its orbit, as a shipyard landing does.
/// </summary>
[TestClass]
public class TouchdownSpotTests : StarDriveTest
{
    readonly Planet Homeworld;

    public TouchdownSpotTests()
    {
        LoadStarterShips("Vulcan Scout", "Shipyard", "Basic Research Station");
        CreateUniverseAndPlayerEmpire();
        UState.Objects.EnableParallelUpdate = false;
        Homeworld = AddHomeWorldToEmpire(new Vector2(200_000), Player, new Vector2(205_000), explored: true);
    }

    // lands 20 ships from the same spot and returns where each touched down, relative to the landing target;
    // a ship that took off again after touchdown is still where it touched down on the frame it stops landing
    Vector2[] TouchdownsRelativeTo(Func<Vector2> target, Vector2 from, Action<Ship> startLanding)
    {
        var ships = new Ship[20];
        for (int i = 0; i < ships.Length; ++i)
        {
            ships[i] = SpawnShip("Vulcan Scout", Player, from);
            startLanding(ships[i]);
        }

        var touchdowns = new Map<Ship, Vector2>();
        RunSimWhile((simTimeout: 60, fatal: true), () => touchdowns.Count < ships.Length, () =>
        {
            foreach (Ship ship in ships)
                if (!ship.IsLanding && !touchdowns.ContainsKey(ship))
                    touchdowns.Add(ship, ship.Position - target());
        });
        return touchdowns.Values.ToArr();
    }

    static float WidestGap(Vector2[] spots)
    {
        float widest = 0f;
        foreach (Vector2 a in spots)
            foreach (Vector2 b in spots)
                widest = widest.LowerBound(a.Distance(b));
        return widest;
    }

    void AssertSpreadAroundCentre(Vector2[] touchdowns, string landedOn)
    {
        foreach (Vector2 offset in touchdowns)
            AssertLessThan(offset.Length(), LandShip.TouchdownRadius + 2f, $"a ship touches down within 100 of the {landedOn}'s centre");
        AssertGreaterThan(WidestGap(touchdowns), LandShip.TouchdownRadius * 0.5f, $"touchdowns on the {landedOn} spread out");
    }

    [TestMethod]
    public void ShipsTouchDownWithin100OfAPlanetsCentre()
    {
        Vector2 approach = Homeworld.Position + new Vector2(Homeworld.Radius + 200f, 0);
        Vector2[] touchdowns = TouchdownsRelativeTo(() => Homeworld.Position, approach,
                                                    ship => ship.InitLanding(LandPlan.Refit, Homeworld)); // no refit goal: takes off again
        AssertSpreadAroundCentre(touchdowns, "planet");
    }

    [TestMethod]
    public void APlanetLandingFollowsThePlanetAlongItsOrbit()
    {
        Ship ship = SpawnShip("Vulcan Scout", Player, Homeworld.Position + new Vector2(Homeworld.Radius + 200f, 0));
        ship.InitLanding(LandPlan.Refit, Homeworld);
        RunObjectsSim(1f);
        Vector2 before = Homeworld.Position;
        Homeworld.OrbitalAngle += 3f;
        Homeworld.Position = Homeworld.System.Position.PointFromAngle(Homeworld.OrbitalAngle, Homeworld.OrbitalRadius);
        AssertGreaterThan(before.Distance(Homeworld.Position), LandShip.TouchdownRadius * 2f, "setup: the planet must move well past the touchdown spread");

        RunSimWhile((simTimeout: 60, fatal: true), () => ship.IsLanding);
        AssertLessThan(ship.Position.Distance(Homeworld.Position), LandShip.TouchdownRadius + 2f,
                       "the ship touches down near the planet's centre where the planet is now");
    }

    [TestMethod]
    public void APlanetLandingSavedWithoutItsPlanetFinishesWhereTheShipIs()
    {
        Vector2 start = Homeworld.Position + new Vector2(Homeworld.Radius + 200f, 0);
        Ship ship = SpawnShip("Vulcan Scout", Player, start);
        ship.InitLanding(LandPlan.Refit, Homeworld);

        // a landing saved before planet landings followed the planet loads with no planet
        var flags = BindingFlags.NonPublic | BindingFlags.Instance;
        FieldInfo landingField = typeof(LandShip).GetField("PlanetLanding", flags);
        object landing = landingField.GetValue(ship.LandShip);
        landing.GetType().GetField("Planet", flags).SetValue(landing, null);
        landingField.SetValue(ship.LandShip, landing);

        RunSimWhile((simTimeout: 60, fatal: true), () => ship.IsLanding);
        AssertLessThan(ship.Position.Distance(start), 1f, "the landing finishes where the ship is");
    }

    [TestMethod]
    public void ShipsTouchDownWithin100OfAShipyardsCentre()
    {
        Ship shipyard = SpawnShip("Shipyard", Player, Homeworld.Position + new Vector2(0, 2500));
        shipyard.TetherToPlanet(Homeworld);
        Vector2[] touchdowns = TouchdownsRelativeTo(() => shipyard.Position, shipyard.Position + new Vector2(300, 0),
                                                    ship => ship.InitLanding(LandPlan.Refit, Homeworld, shipyard));
        AssertSpreadAroundCentre(touchdowns, "shipyard");
    }

    [TestMethod]
    public void ShipsTouchDownWithin100OfAStationInOpenSpace()
    {
        Ship station = SpawnShip("Basic Research Station", Player, Homeworld.Position + new Vector2(30_000, 0));
        Vector2[] touchdowns = TouchdownsRelativeTo(() => station.Position, station.Position + new Vector2(300, 0),
                                                    ship => ship.InitLandingOnStation(station));
        AssertSpreadAroundCentre(touchdowns, "station");
    }
}
