using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using SDGraphics;
using Ship_Game;
using Ship_Game.AI;
using Ship_Game.GameScreens.LoadGame;
using Ship_Game.Ships;
using Vector2 = SDGraphics.Vector2;
#pragma warning disable CA2213

namespace UnitTests.Ships;

/// <summary>
/// A home defense ship with nothing left to fight flies home and lands on its planet, never on a shipyard,
/// since its hangar is on the surface. Once down, it is back in its hangar and its cost is refunded.
/// </summary>
[TestClass]
public class HomeDefenseLandingTests : StarDriveTest
{
    readonly Planet Homeworld;
    readonly Building Capital;

    public HomeDefenseLandingTests()
    {
        LoadStarterShips("Shipyard");
        CreateUniverseAndPlayerEmpire();
        UState.Objects.EnableParallelUpdate = false;
        Homeworld = AddHomeWorldToEmpire(new Vector2(200_000), Player, new Vector2(205_000), explored: true);
        Player.UpdateRallyPoints();
        foreach (Building b in Homeworld.Buildings)
            if (b.IsCapital) Capital = b;
        Assert.IsNotNull(Capital, "setup: the homeworld must have the capital and its defense hangar");
        Capital.UpdateCurrentDefenseShips(-Capital.DefenseShipsCapacity);
    }

    float PlanetRange => Homeworld.Radius + 300f;

    Ship AddShipyard(Vector2 offset)
    {
        Ship shipyard = SpawnShip("Shipyard", Player, Homeworld.Position + offset);
        shipyard.TetherToPlanet(Homeworld);
        return shipyard;
    }

    Ship LaunchDefenseShip(Vector2 offset, Action<Ship> whileLaunching = null)
    {
        Ship ship = Ship.CreateDefenseShip(UState, "Fang Strafer", Player, Homeworld.Position + offset, Homeworld);
        Assert.IsTrue(ship is { IsHomeDefense: true, DesignRole: RoleName.fighter }, "setup: a fighter defending the homeworld");
        Assert.IsTrue(ship.IsLaunching, "setup: a defense ship launches from its planet");
        RunSimWhile((simTimeout: 60, fatal: true), () => ship.IsLaunching, () => whileLaunching?.Invoke(ship));
        return ship;
    }

    void RunUntilLanding(Ship ship) => RunSimWhile((simTimeout: 120, fatal: true), () => !ship.IsLanding);

    [TestMethod]
    public void ADefenseShipComingHomeLandsOnThePlanetNotOnAShipyard()
    {
        Ship shipyard = AddShipyard(new Vector2(0, 2500));
        Ship ship = LaunchDefenseShip(new Vector2(6000, 2500));
        float money = Player.Money;

        RunUntilLanding(ship);
        AssertEqual(AIState.ReturnHome, ship.AI.State, "a landing defense ship is still on its way home");
        AssertLessThan(ship.Position.Distance(Homeworld.Position), PlanetRange + 1f, "the defense ship must land on the planet");
        AssertGreaterThan(ship.Position.Distance(Homeworld.Position), PlanetRange - 50f,
                          "the landing starts as the ship comes within the planet's radius + 300");
        AssertGreaterThan(ship.Position.Distance(shipyard.Position), LandShip.ShipyardLandingRange(ship),
                          "the defense ship must not land on the shipyard on its way");
        AssertEqual(0, Capital.CurrentNumDefenseShips, "the ship is not back in its hangar while it is on its way down");

        RunSimWhile((simTimeout: 60, fatal: true), () => ship.Active);
        Assert.IsFalse(ship.Dying, "a landed defense ship is removed, not destroyed");
        AssertEqual(1, Capital.CurrentNumDefenseShips, "the landed ship is back in its hangar");
        AssertGreaterThan(Player.Money, money, "the landed ship's cost is refunded");
    }

    [TestMethod]
    public void ADefenseShipWhosePlanetIsLostDuringTheLandingRefundsItsOwnEmpire()
    {
        Ship ship = LaunchDefenseShip(new Vector2(4000, 0));
        RunUntilLanding(ship);

        Homeworld.SetOwner(Enemy);
        float money = Player.Money;
        float enemyMoney = Enemy.Money;
        RunSimWhile((simTimeout: 60, fatal: true), () => ship.Active);
        AssertGreaterThan(Player.Money, money, "the ship's own empire gets the refund");
        AssertEqual(0.01f, enemyMoney, Enemy.Money, "the planet's new owner gets nothing for another empire's ship");
        AssertEqual(0, Capital.CurrentNumDefenseShips, "another empire's ship does not fill the new owner's hangar");
    }

    [TestMethod]
    public void ADefenseShipOnHighAlertWaitsBeforeItLands()
    {
        Ship ship = LaunchDefenseShip(new Vector2(Homeworld.Radius + 100f, 0), s => s.SetHighAlertStatus());
        RunSimWhile((simTimeout: 8, fatal: false), () => true, ship.SetHighAlertStatus);
        AssertEqual(AIState.ReturnHome, ship.AI.State, "setup: the ship must be on its way home");
        Assert.IsFalse(ship.IsLanding, "a defense ship on high alert must not land");

        RunUntilLanding(ship);
    }

    [TestMethod]
    public void ALandingDefenseShipCarriesOnAfterALoad()
    {
        UState.StarDate = 1042.5f;
        Ship ship = LaunchDefenseShip(new Vector2(4000, 0));
        RunUntilLanding(ship);

        SavedGame save = Universe.Save("UnitTest.HomeDefenseLanding", throwOnError: true);
        UniverseScreen loaded = LoadGame.Load(save.SaveFile, noErrorDialogs: true, startSimThread: false);
        Ship landing = loaded.UState.Objects.FindShip(ship.Id);
        Planet loadedHomeworld = loaded.UState.GetPlanet(Homeworld.Id);
        Building loadedCapital = null;
        foreach (Building b in loadedHomeworld.Buildings)
            if (b.IsCapital) loadedCapital = b;
        Assert.IsTrue(landing is { Active: true, IsLanding: true, AI.State: AIState.ReturnHome },
                      "a landing defense ship must still be landing after a load");

        for (double time = 0; landing.Active; time += TestSimStepD)
        {
            AssertLessThan(time, 60.0, "the loaded landing must finish");
            loaded.UState.Objects.Update(TestSimStep);
        }
        Assert.IsFalse(landing.Dying, "a landed defense ship is removed, not destroyed");
        AssertEqual(1, loadedCapital.CurrentNumDefenseShips, "the loaded landing still puts the ship back in its hangar");
    }
}
