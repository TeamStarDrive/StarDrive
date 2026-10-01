using System;
using System.Reflection;
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
/// A builder shuttle that has helped a constructor flies home and lands: on the nearest of the colony's
/// shipyards, or on the planet when it has none. Its launch slot at the colony is free once it has landed.
/// </summary>
[TestClass]
public class BuilderShuttleLandingTests : StarDriveTest
{
    static readonly FieldInfo BuildersAllowed = typeof(Planet).GetField("NumBuildShipsCanLaunch", BindingFlags.Instance | BindingFlags.NonPublic);

    readonly Planet Homeworld;

    public BuilderShuttleLandingTests()
    {
        LoadStarterShips("Shipyard");
        CreateUniverseAndPlayerEmpire();
        UState.Objects.EnableParallelUpdate = false;
        Homeworld = AddHomeWorldToEmpire(new Vector2(200_000), Player, new Vector2(205_000), explored: true);
        Player.UpdateRallyPoints();
        BuildersAllowed.SetValue(Homeworld, 5);
        AssertEqual(5, Homeworld.BuilderShipsLimit, "setup: the colony may have 5 builder ships out");
    }

    static int BuilderShipsOut(Planet planet) => planet.BuilderShipsOut;

    float PlanetRange => Homeworld.Radius + 300f;

    Ship AddShipyard(Vector2 offset)
    {
        Ship shipyard = SpawnShip("Shipyard", Player, Homeworld.Position + offset);
        shipyard.TetherToPlanet(Homeworld);
        return shipyard;
    }

    Ship AddConstructor(Vector2 offset)
    {
        Ship constructor = SpawnShip("Terran Constructor", Player, Homeworld.Position + offset);
        constructor.Construction = ConstructionShip.Create(constructor, 1000f, 100f);
        return constructor;
    }

    Ship LaunchShuttle(Ship constructor)
    {
        Homeworld.LaunchBuilderShip(constructor);
        RunObjectsSim(TestSimStep);
        Ship shuttle = Array.Find(UState.Objects.GetShips(), s => s.IsSupplyShuttle);
        Assert.IsNotNull(shuttle, "setup: the colony must launch a builder shuttle");
        AssertEqual(1, BuilderShipsOut(Homeworld), "setup: the shuttle takes one of the colony's builder slots");
        return shuttle;
    }

    void RunUntilLanding(Ship shuttle) => RunSimWhile((simTimeout: 120, fatal: true), () => !shuttle.IsLanding);

    [TestMethod]
    public void AShuttleComingHomeLandsOnTheNearestShipyardAndFreesItsSlotOnceDown()
    {
        Ship launchedFrom = AddShipyard(new Vector2(-2500, 0));
        Ship constructor = AddConstructor(new Vector2(8000, 2500));
        Ship shuttle = LaunchShuttle(constructor);
        AssertLessThan(shuttle.Position.Distance(launchedFrom.Position), 100f, "setup: the shuttle must launch from the only shipyard");
        Ship shipyard = AddShipyard(new Vector2(0, 2500));

        RunUntilLanding(shuttle);
        AssertGreaterThan(constructor.Construction.ConstructionAdded, 0f, "setup: the shuttle must help the constructor before it comes home");
        Assert.IsTrue(shuttle.Position.InRadius(shipyard.Position, LandShip.ShipyardLandingRange(shuttle)),
                      "the shuttle must land on the shipyard nearest to it, not the one it launched from");
        AssertEqual(1, BuilderShipsOut(Homeworld), "the slot stays taken while the shuttle is on its way down");

        RunSimWhile((simTimeout: 60, fatal: true), () => shuttle.Active);
        Assert.IsFalse(shuttle.Dying, "a landed shuttle is removed, not destroyed");
        AssertLessThan(shuttle.Position.Distance(shipyard.Position), LandShip.TouchdownRadius + 5f, "the shuttle must touch down on the shipyard");
        AssertEqual(0, BuilderShipsOut(Homeworld), "the slot is free once the shuttle has landed");
    }

    [TestMethod]
    public void AShuttleFromAColonyWithoutShipyardsLandsOnThePlanet()
    {
        Ship constructor = AddConstructor(new Vector2(6000, 0));
        Ship shuttle = LaunchShuttle(constructor);

        RunUntilLanding(shuttle);
        Assert.IsTrue(shuttle.Position.InRadius(Homeworld.Position, PlanetRange), "the shuttle must land on the planet");
        AssertGreaterThan(shuttle.Position.Distance(Homeworld.Position), PlanetRange - 50f,
                          "the landing starts as the shuttle comes within the planet's radius + 300");

        RunSimWhile((simTimeout: 60, fatal: true), () => shuttle.Active);
        Assert.IsFalse(shuttle.Dying, "a landed shuttle is removed, not destroyed");
        AssertEqual(0, BuilderShipsOut(Homeworld), "the slot is free once the shuttle has landed");
    }

    void AssertScuttledWhenItsColonyIsLost(Action loseColony, Empire newOwner)
    {
        Vector2 inRange = Homeworld.Position + new Vector2(Homeworld.Radius + 100f, 0);
        Ship control = SpawnShip("Supply Shuttle", Player, inRange);
        control.AI.OrderBuilderReturnHome(Homeworld);
        RunObjectsSim(TestSimStep);
        Assert.IsTrue(control.IsLanding, "setup: a shuttle this close to home starts down at once");

        Ship shuttle = SpawnShip("Supply Shuttle", Player, inRange);
        shuttle.AI.OrderBuilderReturnHome(Homeworld);
        loseColony();
        AssertEqual(newOwner, Homeworld.Owner, "setup: the colony must be lost");
        RunObjectsSim(TestSimStep);
        Assert.IsFalse(shuttle.IsLanding, "a shuttle must not land on a colony that is no longer its empire's");
        AssertEqual(AIState.Scuttle, shuttle.AI.State, "a shuttle with no home to land on is scuttled");
    }

    [TestMethod]
    public void AShuttleWhoseColonyIsTakenIsScuttledAndDoesNotLand()
    {
        AssertScuttledWhenItsColonyIsLost(() => Homeworld.SetOwner(Enemy), Enemy);
    }

    [TestMethod]
    public void AShuttleWhoseColonyIsWipedOutIsScuttledAndDoesNotLand()
    {
        Universe.NotificationManager = new NotificationManager(Universe.ScreenManager, Universe);
        AssertScuttledWhenItsColonyIsLost(() => Homeworld.WipeOutColony(Player), null);
    }

    [TestMethod]
    public void ACapturedColonyCountsOnlyItsNewOwnersShuttles()
    {
        Ship constructor = AddConstructor(new Vector2(6000, 0));
        Ship shuttle = LaunchShuttle(constructor);
        RunUntilLanding(shuttle);

        Homeworld.SetOwner(Enemy);
        AssertEqual(0, BuilderShipsOut(Homeworld), "the new owner of a colony starts with none of its builder slots taken");

        Homeworld.UpdateBuilderShipLaunched(1);
        RunSimWhile((simTimeout: 60, fatal: true), () => shuttle.Active);
        AssertEqual(1, BuilderShipsOut(Homeworld), "the old owner's shuttle landing must not free a slot the new owner's shuttle holds");
    }

    [TestMethod]
    public void ALandingShuttleCarriesOnAfterALoad()
    {
        UState.StarDate = 1042.5f;
        Ship shipyard = AddShipyard(new Vector2(0, 2500));
        Ship constructor = AddConstructor(new Vector2(3000, 2500));
        Ship shuttle = LaunchShuttle(constructor);
        RunUntilLanding(shuttle);

        SavedGame save = Universe.Save("UnitTest.BuilderLanding", throwOnError: true);
        UniverseScreen loaded = LoadGame.Load(save.SaveFile, noErrorDialogs: true, startSimThread: false);
        Ship landing = loaded.UState.Objects.FindShip(shuttle.Id);
        Ship loadedShipyard = loaded.UState.Objects.FindShip(shipyard.Id);
        Planet loadedHomeworld = loaded.UState.GetPlanet(Homeworld.Id);
        Assert.IsTrue(landing is { Active: true, IsLanding: true }, "a landing shuttle must still be landing after a load");
        AssertEqual(1, BuilderShipsOut(loadedHomeworld), "setup: the loaded colony still counts the shuttle as out");

        for (double time = 0; landing.Active; time += TestSimStepD)
        {
            AssertLessThan(time, 60.0, "the loaded landing must finish");
            loaded.UState.Objects.Update(TestSimStep);
        }
        Assert.IsFalse(landing.Dying, "a landed shuttle is removed, not destroyed");
        AssertLessThan(landing.Position.Distance(loadedShipyard.Position), LandShip.TouchdownRadius + 5f, "the loaded landing must still end on the shipyard");
        AssertEqual(0, BuilderShipsOut(loadedHomeworld), "the loaded landing still frees the colony's slot");
    }
}
