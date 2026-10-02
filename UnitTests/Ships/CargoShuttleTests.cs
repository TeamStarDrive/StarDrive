using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Ship_Game;
using Ship_Game.AI;
using Ship_Game.GameScreens.LoadGame;
using Ship_Game.Ships;
using Color = Microsoft.Xna.Framework.Color;
using Matrix = SDGraphics.Matrix;
using Vector2 = SDGraphics.Vector2;
using BoundingFrustum = Microsoft.Xna.Framework.BoundingFrustum;
#pragma warning disable CA2213

namespace UnitTests.Ships;

/// <summary>
/// While a freighter lands on a space port to trade, small cargo shuttles fly up from the planet and dock in the
/// port before it does: one per 20 of its cargo space, up to 20. As it takes off, shuttles fly back down: the same
/// ones, now empty, after it loaded; one per 20 cargo unloaded after it unloaded. They are only drawn for a planet
/// on screen at planet view or closer.
/// </summary>
[TestClass]
public class CargoShuttleTests : StarDriveTest
{
    readonly Planet Exporter;
    readonly Planet Importer;

    public CargoShuttleTests()
    {
        LoadStarterShips("Owlwok Freighter L");
        CreateUniverseAndPlayerEmpire();
        UState.Objects.EnableParallelUpdate = false;
        Exporter = AddHomeWorldToEmpire(new Vector2(200_000), Player, new Vector2(205_000), explored: true);
        Importer = AddDummyPlanet(new Vector2(300_000), 1f, 1f, 4f, new Vector2(305_000), explored: true);
        Importer.SetOwner(Player);
        Player.UpdateRallyPoints();

        Exporter.FS = Planet.GoodState.EXPORT;
        Exporter.FoodHere = Exporter.Storage.Max;
        Importer.FS = Planet.GoodState.IMPORT;
        Importer.FoodHere = 0;
        ViewEverything(Universe);
    }

    static void ViewEverything(UniverseScreen universe)
    {
        universe.Frustum = new BoundingFrustum(Matrix.CreateOrthographicOffCenter(0, 1_000_000, 1_000_000, 0, -10_000, 10_000));
        universe.UState.ViewState = UniverseScreen.UnivScreenState.PlanetView;
    }

    void StepWhile(Func<bool> condition, Action body = null) => StepWhile(Universe, condition, body);

    void StepWhile(UniverseScreen universe, Func<bool> condition, Action body = null)
    {
        for (double elapsed = 0; condition(); elapsed += TestSimStepD)
        {
            body?.Invoke();
            if (elapsed >= 120)
                throw new TimeoutException("Timed out in StepWhile");
            universe.UState.Objects.Update(TestSimStep);
            universe.CargoShuttles.Update(universe, TestSimStep);
        }
    }

    Ship SpawnFreighter(Planet near)
    {
        Ship freighter = SpawnShip("Owlwok Freighter L", Player, near.Position + new Vector2(8000, 0));
        freighter.TransportingFood = true;
        freighter.TransportingProduction = true;
        AssertGreaterThan(CargoShuttles.ShuttlesFor(freighter.CargoSpaceMax), CargoShuttles.ShuttlesFor(25f),
                          "setup: the cargo space must take more shuttles than 25 cargo");
        return freighter;
    }

    int ShuttlesUp(Planet planet) => Universe.CargoShuttles.InFlight(planet, toPort: true);
    int ShuttlesDown(Planet planet) => Universe.CargoShuttles.InFlight(planet, toPort: false);

    Ship LandToLoadAtExporter()
    {
        Assert.IsTrue(Exporter.HasSpacePort, "setup: the exporting colony must have a space port");
        Ship freighter = SpawnFreighter(Exporter);
        freighter.AI.SetupFreighterPlan(Exporter, Importer, Goods.Food);
        StepWhile(() => !freighter.IsLanding);
        return freighter;
    }

    Ship LandToUnloadAtExporter(float production)
    {
        Exporter.PS = Planet.GoodState.IMPORT;
        Exporter.ProdHere = 0;
        Ship freighter = SpawnFreighter(Exporter);
        freighter.LoadProduction(production);
        freighter.AI.SetupFreighterPlan(Exporter, Exporter, Goods.Production);
        StepWhile(() => !freighter.IsLanding);
        return freighter;
    }

    Ship UnloadAndDockAtExporter(float production)
    {
        Ship freighter = LandToUnloadAtExporter(production);
        StepWhile(() => freighter.LandShip is not { Docked: true });
        Assert.IsTrue(freighter.LandShip.OnDock, "setup: the freighter must be docked on the space port");
        return freighter;
    }

    [TestMethod]
    public void EachShuttleCarriesUpTo20Cargo()
    {
        AssertEqual(1, CargoShuttles.ShuttlesFor(5f), "any cargo takes at least one shuttle");
        AssertEqual(1, CargoShuttles.ShuttlesFor(20f), "a shuttle carries 20 cargo");
        AssertEqual(2, CargoShuttles.ShuttlesFor(59f), "a shuttle carries 20 cargo");
        AssertEqual(20, CargoShuttles.ShuttlesFor(400f), "there are at most 20 shuttles");
        AssertEqual(20, CargoShuttles.ShuttlesFor(5000f), "there are at most 20 shuttles");
    }

    [TestMethod]
    public void FullShuttlesFlyUpToALoadingFreighterAndReturnEmptyAsItTakesOff()
    {
        Ship freighter = LandToLoadAtExporter();
        int shuttles = CargoShuttles.ShuttlesFor(freighter.CargoSpaceMax);
        AssertEqual(shuttles, ShuttlesUp(Exporter), "a freighter landing to load gets a shuttle for each 20 of its cargo space");

        StepWhile(() => freighter.LandShip is { Done: false });
        AssertEqual(0, ShuttlesUp(Exporter), "the shuttles have docked before the freighter touches down");
        Assert.IsTrue(freighter.IsLaunching, "setup: the loaded freighter must be taking off");
        AssertEqual(shuttles, ShuttlesDown(Exporter), "the same shuttles fly back down, empty, as the freighter takes off");
        StepWhile(() => freighter.IsLaunching);
        AssertEqual(0, ShuttlesDown(Exporter), "the shuttles have landed before the freighter has taken off");
    }

    [TestMethod]
    public void EmptyShuttlesFlyUpToAnUnloadingFreighterAndBringTheGoodsDownAsItTakesOff()
    {
        Ship freighter = LandToUnloadAtExporter(25f);
        AssertEqual(CargoShuttles.ShuttlesFor(freighter.CargoSpaceMax), ShuttlesUp(Exporter),
                    "a freighter landing to unload gets a shuttle for each 20 of its cargo space");
        StepWhile(() => freighter.LandShip is not { Docked: true });
        AssertEqual(0, ShuttlesUp(Exporter), "the shuttles have docked before the freighter touches down");

        StepWhile(() => freighter.IsLanding,
                  () => AssertEqual(0, ShuttlesDown(Exporter), "the shuttles wait for the docked freighter to take off"));
        Assert.IsTrue(freighter.IsLaunching, "setup: the freighter must be taking off");
        AssertEqual(CargoShuttles.ShuttlesFor(25f), ShuttlesDown(Exporter), "a shuttle flies down for each 20 cargo unloaded");
        StepWhile(() => freighter.IsLaunching);
        AssertEqual(0, ShuttlesDown(Exporter), "the shuttles have landed before the freighter has taken off");
    }

    [TestMethod]
    public void ADockedFreighterStillSendsItsShuttlesDownAfterALoad()
    {
        UState.StarDate = 1042.5f;
        Ship freighter = UnloadAndDockAtExporter(25f);
        SavedGame save = Universe.Save("UnitTest.CargoShuttles", throwOnError: true);
        UniverseScreen loaded = LoadGame.Load(save.SaveFile, noErrorDialogs: true, startSimThread: false);
        ViewEverything(loaded);
        Ship docked = loaded.UState.Objects.FindShip(freighter.Id);
        Planet colony = loaded.UState.GetPlanet(Exporter.Id);
        Assert.IsTrue(docked is { Active: true, LandShip.Docked: true }, "setup: the freighter must still be docked after the load");

        StepWhile(loaded, () => docked.IsLanding);
        AssertEqual(CargoShuttles.ShuttlesFor(25f), loaded.CargoShuttles.InFlight(colony, toPort: false),
                    "a freighter docked when the game was saved still sends its unloaded goods down");
    }

    [TestMethod]
    public void NoShuttlesWithoutASpacePort()
    {
        Exporter.HasSpacePort = false;
        Ship freighter = SpawnFreighter(Exporter);
        freighter.AI.SetupFreighterPlan(Exporter, Importer, Goods.Food);
        StepWhile(() => !freighter.IsLanding);
        AssertEqual(0, ShuttlesUp(Exporter), "a freighter landing on the planet itself needs no shuttles");
    }

    [TestMethod]
    public void NoShuttlesReturnToASpacePortBuiltWhileTheFreighterWasDownOnThePlanet()
    {
        Assert.IsFalse(Importer.HasSpacePort, "setup: the importing colony must have no space port");
        Ship freighter = SpawnFreighter(Importer);
        freighter.LoadFood(5f);
        freighter.AI.SetupFreighterPlan(Importer, Importer, Goods.Food);
        StepWhile(() => freighter.LandShip is not { Docked: true });
        Importer.HasSpacePort = true;

        StepWhile(() => freighter.IsLanding);
        Assert.IsTrue(freighter.IsLaunching, "setup: the freighter must be taking off");
        AssertEqual(0, ShuttlesDown(Importer), "a freighter that landed on the planet sends no shuttles down from a port built meanwhile");
    }

    [TestMethod]
    public void EachSimulationStepFliesTheShuttles()
    {
        AddDummyPlanet(new Vector2(500_000), 1f, 1f, 1f, new Vector2(505_000), explored: true).SetOwner(Enemy);
        UState.Paused = false;
        Universe.CargoShuttles.Send(Exporter, Color.White, 3, seconds: 5f, toPort: true);
        Universe.SingleSimulationStep(TestSimStep);
        AssertEqual(3, ShuttlesUp(Exporter), "a simulation step launches the shuttles sent since the last one");

        RunFullSimWhile((simTimeout: 5, fatal: false));
        AssertEqual(0, ShuttlesUp(Exporter), "the simulation flies them until they dock");
    }

    [TestMethod]
    public void NoShuttlesWhenZoomedOutPastPlanetView()
    {
        UState.ViewState = UniverseScreen.UnivScreenState.SystemView;
        LandToLoadAtExporter();
        AssertEqual(0, ShuttlesUp(Exporter), "the shuttles are too small to see past planet view");
    }

    [TestMethod]
    public void NoShuttlesForAColonyOffScreen()
    {
        Universe.Frustum = new BoundingFrustum(Matrix.CreateTranslation(1_000_000, 1_000_000, 0));
        LandToLoadAtExporter();
        AssertEqual(0, ShuttlesUp(Exporter), "no shuttles fly for a colony that is not on screen");
    }
}
