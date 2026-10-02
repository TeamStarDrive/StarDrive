using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using SDGraphics;
using Ship_Game;
using Ship_Game.AI;
using Ship_Game.Commands.Goals;
using Ship_Game.GameScreens.LoadGame;
using Ship_Game.Ships;
using Vector2 = SDGraphics.Vector2;
#pragma warning disable CA2213

namespace UnitTests.Ships;

/// <summary>
/// A freighter lands to load and to unload: it glides into the colony's space port, the station drawn over the
/// planet, or lands on the planet when the colony has none, and glides into a research or mining station it
/// supplies. It takes off again from the same place. The goods move at touchdown, and the freighter keeps its
/// trade orders while it is down, so the lines drawn for a selected freighter still show where it is going, and
/// it still counts as a trading freighter. Trip estimates include the landings and the take-offs.
/// </summary>
[TestClass]
public class FreighterLandingTests : StarDriveTest
{
    readonly Planet Exporter;
    readonly Planet Importer;

    public FreighterLandingTests()
    {
        LoadStarterShips("Owlwok Freighter S", "Owlwok Freighter M", "Shipyard", "Basic Research Station");
        CreateUniverseAndPlayerEmpire();
        UState.Objects.EnableParallelUpdate = false;
        Exporter = AddHomeWorldToEmpire(new Vector2(200_000), Player, new Vector2(205_000), explored: true);
        Importer = AddDummyPlanet(new Vector2(300_000), 1f, 1f, 4f, new Vector2(305_000), explored: true);
        Importer.SetOwner(Player);
        Player.UpdateRallyPoints();

        Exporter.FS = Planet.GoodState.EXPORT;
        Exporter.FoodHere = Exporter.Storage.Max;
        Exporter.PS = Planet.GoodState.EXPORT;
        Exporter.ProdHere = Exporter.Storage.Max;
        Importer.FS = Planet.GoodState.IMPORT;
        Importer.FoodHere = 0;
    }

    Ship SpawnFreighter(Planet near, Vector2 offset)
    {
        Ship freighter = SpawnShip("Owlwok Freighter S", Player, near.Position + offset);
        Assert.IsTrue(freighter.IsFreighter && freighter.CargoSpaceMax > 0, "setup: the ship must be a freighter");
        freighter.TransportingFood = true;
        freighter.TransportingProduction = true;
        return freighter;
    }

    Ship SpawnResearchStation(Vector2 position)
    {
        Ship station = SpawnShip("Basic Research Station", Player, position);
        Assert.IsTrue(station.IsResearchStation && station.CargoSpaceMax > 5, "setup: the station must take production");
        return station;
    }

    static bool HasTradeGoal(Ship freighter, ShipAI.Plan plan, out ShipAI.ShipGoal goal)
    {
        goal = null;
        return freighter.AI.State == AIState.SystemTrader
               && freighter.AI.OrderQueue.TryPeekLast(out goal) && goal.Trade != null && goal.Plan == plan;
    }

    void RunUntilLanding(Ship freighter) => RunSimWhile((simTimeout: 120, fatal: true), () => !freighter.IsLanding);
    void RunUntilLanded(Ship freighter) => RunSimWhile((simTimeout: 60, fatal: true), () => freighter.IsLanding);

    void AssertTakeOffTakes(Ship freighter, Planet planet, string message)
        => AssertTakeOffTakes(freighter, freighter.GetTradeTakeOffTime(planet) * UState.P.TurnTimer, message);

    void AssertTakeOffTakes(Ship freighter, float seconds, string message)
    {
        double takeOff = RunSimWhile((simTimeout: 60, fatal: true), () => freighter.IsLaunching);
        AssertEqual(0.1f, seconds, (float)takeOff, message);
    }

    void AssertLandsOnStationAndUnloads(Ship freighter, Ship station)
    {
        float prodBefore = station.GetProduction();
        float cargo = freighter.GetCargo(Goods.Production);
        AssertGreaterThan(cargo, 0f, "setup: the freighter must carry production for the station");

        RunUntilLanding(freighter);
        float startDistance = freighter.Position.Distance(station.Position);
        float glideRange = LandShip.ShipyardLandingRange(freighter);
        AssertGreaterThan(startDistance, glideRange - 60f, "the landing on the station starts where a launch from it would end");
        AssertLessThan(startDistance, glideRange + 1f, "the landing on the station starts where a launch from it would end");
        Assert.IsTrue(HasTradeGoal(freighter, ShipAI.Plan.DropOffGoodsForStation, out _), "a landing freighter keeps its trade orders");

        RunSimWhile((simTimeout: 60, fatal: true), () => freighter.LandShip is { Done: false }, () =>
        {
            AssertEqual(prodBefore, station.GetProduction(), "nothing is unloaded before the freighter has landed");
        });

        Assert.IsTrue(freighter.IsLaunching, "a freighter takes off again once it has unloaded");
        Assert.IsNull(freighter.TakingOffFrom, "a freighter taking off from a station is not leaving a colony, so it cannot load there");
        AssertLessThan(freighter.Position.Distance(station.Position), LandShip.TouchdownRadius + 5f, "the freighter lands on and takes off from the station");
        AssertEqual(1f, prodBefore + cargo, station.GetProduction(), "the freighter unloads at touchdown");
        AssertEqual(0f, freighter.CargoSpaceUsed, "the freighter unloads at touchdown");
        Assert.IsFalse(freighter.AI.OrderQueue.TryPeekLast(out ShipAI.ShipGoal g) && g.Trade != null, "the delivery is done");
        AssertTakeOffTakes(freighter, LaunchShip.TakeOffSeconds(freighter, fromDock: true),
                           "the freighter takes off from the station like a ship leaving a shipyard");
    }

    [TestMethod]
    public void AFreighterLoadsOnTheSpacePortAndTakesOffFromIt()
    {
        Assert.IsTrue(Exporter.HasSpacePort, "setup: the exporting colony must have a space port");
        Ship shipyard = SpawnShip("Shipyard", Player, Exporter.Position + new Vector2(2500, 0));
        shipyard.TetherToPlanet(Exporter);
        Ship freighter = SpawnFreighter(Exporter, new Vector2(8000, 0));
        freighter.AI.SetupFreighterPlan(Exporter, Importer, Goods.Food);
        float foodBefore = Exporter.FoodHere;

        float speedBefore = 0f;
        RunSimWhile((simTimeout: 120, fatal: true), () => !freighter.IsLanding, () => speedBefore = freighter.CurrentVelocity);
        AssertLessThan(speedBefore, LaunchShip.ShipyardSpeed(freighter) + 10f, "the freighter comes in no faster than launch speed to glide on into the space port");
        AssertGreaterThan(speedBefore, LaunchShip.ShipyardSpeed(freighter) * 0.5f, "the freighter flies in to land on the space port");
        float startDistance = freighter.Position.Distance(Exporter.Position);
        float portRange = LandShip.ShipyardLandingRange(freighter);
        AssertGreaterThan(startDistance, portRange - 60f, "the landing on the space port starts where a launch from it would end");
        AssertLessThan(startDistance, portRange + 1f, "the landing on the space port starts where a launch from it would end");
        Assert.IsTrue(HasTradeGoal(freighter, ShipAI.Plan.PickupGoods, out _), "a landing freighter keeps its trade orders");

        RunSimWhile((simTimeout: 60, fatal: true), () => freighter.LandShip is { Done: false }, () =>
        {
            AssertEqual(0f, freighter.GetCargo(Goods.Food), "nothing is loaded before the freighter has landed");
            AssertEqual(foodBefore, Exporter.FoodHere, "nothing is loaded before the freighter has landed");
        });

        Assert.IsFalse(freighter.IsLanding, "a freighter takes off again at touchdown");
        Assert.IsTrue(freighter.IsLaunching, "a freighter takes off again at touchdown");
        AssertLessThan(freighter.Position.Distance(Exporter.Position), LandShip.TouchdownRadius + 5f, "the freighter lands on and takes off from the space port");
        float loaded = freighter.GetCargo(Goods.Food);
        AssertGreaterThan(loaded, 0f, "the freighter loads at touchdown");
        AssertEqual(1f, foodBefore - loaded, Exporter.FoodHere, "the goods loaded leave the colony's storage");
        Assert.IsTrue(HasTradeGoal(freighter, ShipAI.Plan.DropOffGoods, out ShipAI.ShipGoal goal) && goal.Trade.ImportTo == Importer,
                      "the freighter takes off bound for the importing colony, so its trade line still shows");
        AssertTakeOffTakes(freighter, Exporter, "the freighter takes off from the space port like a ship leaving a shipyard");
    }

    [TestMethod]
    public void AColonyWithoutASpacePortHasFreightersLandOnThePlanet()
    {
        Exporter.HasSpacePort = false;
        Ship freighter = SpawnFreighter(Exporter, new Vector2(8000, 0));
        freighter.AI.SetupFreighterPlan(Exporter, Importer, Goods.Food);

        float speedBefore = 0f;
        RunSimWhile((simTimeout: 120, fatal: true), () => !freighter.IsLanding, () => speedBefore = freighter.CurrentVelocity);
        AssertGreaterThan(freighter.MaxSTLSpeed * 0.9f, 250f, "setup: the freighter must be fast enough to tell a slowdown apart");
        AssertGreaterThan(speedBefore, freighter.MaxSTLSpeed * 0.9f, "the freighter does not slow down before it lands on a planet");
        float planetRange = Exporter.Radius + 300f;
        float startDistance = freighter.Position.Distance(Exporter.Position);
        AssertGreaterThan(startDistance, planetRange - 50f, "the landing starts as the freighter comes within the planet's radius + 300");
        AssertLessThan(startDistance, planetRange + 1f, "without a space port the freighter lands on the planet");

        RunUntilLanded(freighter);
        Assert.IsTrue(freighter.IsLaunching, "a freighter takes off again at touchdown");
        AssertLessThan(freighter.Position.Distance(Exporter.Position), LandShip.TouchdownRadius + 5f,
                       "a planet landing touches down somewhere on the planet");
        AssertGreaterThan(freighter.GetCargo(Goods.Food), 0f, "the freighter loads at touchdown");
        Assert.IsTrue(HasTradeGoal(freighter, ShipAI.Plan.DropOffGoods, out _), "the freighter takes off bound for the importing colony");
        Assert.IsFalse(freighter.ThrustersShown, "no thrusters are drawn while the freighter rises from the planet");
        AssertTakeOffTakes(freighter, Exporter, "without a space port the freighter takes off from the planet");
        Assert.IsTrue(freighter.ThrustersShown, "the thrusters show again once the freighter is up");
    }

    [TestMethod]
    public void AFreighterUnloadsAtTouchdownThenWaitsDockedForUpToATurn()
    {
        Assert.IsFalse(Importer.HasSpacePort, "setup: the importing colony must have no space port");
        AssertGreaterThan(Importer.Storage.Max, 5f, "setup: the importing colony must have room for the goods");
        Ship freighter = SpawnFreighter(Importer, new Vector2(8000, 0));
        freighter.LoadFood(5f);
        freighter.AI.SetupFreighterPlan(Importer, Importer, Goods.Food);
        Assert.IsTrue(HasTradeGoal(freighter, ShipAI.Plan.DropOffGoods, out _), "setup: a freighter with the goods on board goes straight to deliver");

        RunUntilLanding(freighter);
        Assert.IsTrue(HasTradeGoal(freighter, ShipAI.Plan.DropOffGoods, out _), "a landing freighter keeps its trade orders");
        RunSimWhile((simTimeout: 60, fatal: true), () => freighter.LandShip is { Done: false }, () =>
        {
            AssertEqual(0f, Importer.FoodHere, "nothing is unloaded before the freighter has landed");
        });

        AssertEqual(1f, 5f, Importer.FoodHere, "the freighter unloads at touchdown");
        AssertEqual(0f, freighter.CargoSpaceUsed, "the freighter unloads at touchdown");
        Assert.IsFalse(freighter.AI.HasTradePlan, "the delivery is done");
        Assert.IsTrue(freighter.LandShip is { Docked: true } && freighter.IsIdleFreighter,
                      "with no next job the freighter waits docked at its colony, free for the next job");
        AssertEqual(Importer, freighter.DockedOrTakingOffFrom, "the freighter is docked at the colony it unloaded at");

        double docked = RunSimWhile((simTimeout: 60, fatal: true), () => freighter.IsLanding);
        AssertEqual(0.1f, UState.P.TurnTimer, (float)docked, "with no job the freighter takes off after a turn");
        Assert.IsTrue(freighter.IsLaunching, "with no job the freighter takes off after a turn");
    }

    Ship DockAfterUnloadingAtImporter()
    {
        Ship freighter = SpawnFreighter(Importer, new Vector2(8000, 0));
        freighter.LoadFood(5f);
        freighter.AI.SetupFreighterPlan(Importer, Importer, Goods.Food);
        RunSimWhile((simTimeout: 120, fatal: true), () => freighter.LandShip is not { Docked: true });
        return freighter;
    }

    void LetImporterExportProductionToExporter()
    {
        Importer.PS = Planet.GoodState.EXPORT;
        Importer.ProdHere = Importer.Storage.Max;
        Importer.ManualProdExportSlots = 1;
        Exporter.PS = Planet.GoodState.IMPORT;
        Exporter.ProdHere = 0;
        Exporter.ManualProdImportSlots = 1;
        Importer.UpdateIncomingTradeGoods();
        Exporter.UpdateIncomingTradeGoods();
    }

    void AssertLoadsAtImporterWithoutLanding(Ship freighter, string message)
    {
        AssertGreaterThan(freighter.GetCargo(Goods.Production), 0f, message);
        Assert.IsTrue(HasTradeGoal(freighter, ShipAI.Plan.DropOffGoods, out ShipAI.ShipGoal goal) && goal.Trade.ImportTo == Exporter,
                      "the freighter takes off bound for the importing colony");
        Assert.IsTrue(freighter.IsLaunching, "the freighter is taking off");
        RunSimWhile((simTimeout: 60, fatal: true), () => freighter.IsLaunching,
                    () => Assert.IsFalse(freighter.IsLanding, "the freighter does not land again to load"));
    }

    [TestMethod]
    public void ADockedFreighterLoadsAtItsColonyWithoutLandingAgain()
    {
        Ship freighter = DockAfterUnloadingAtImporter();
        LetImporterExportProductionToExporter();
        freighter.AI.SetupFreighterPlan(Importer, Exporter, Goods.Production);
        RunObjectsSim(TestSimStep);
        AssertLoadsAtImporterWithoutLanding(freighter, "a docked freighter given its colony's job loads on the spot");
    }

    [TestMethod]
    public void AFreighterTakingOffFromItsColonyLoadsThereWithoutLanding()
    {
        Ship freighter = DockAfterUnloadingAtImporter();
        RunSimWhile((simTimeout: 60, fatal: true), () => !freighter.IsLaunching);
        AssertEqual(Importer, freighter.TakingOffFrom, "setup: the freighter must be taking off from its colony");

        LetImporterExportProductionToExporter();
        freighter.AI.SetupFreighterPlan(Importer, Exporter, Goods.Production);
        RunObjectsSim(TestSimStep);
        AssertLoadsAtImporterWithoutLanding(freighter, "a job at the colony a freighter is taking off from is loaded at once");
    }

    [TestMethod]
    public void ANewFreighterTakingOffFromItsColonyLoadsThere()
    {
        Ship fromPlanet = Ship.CreateShipNearPlanet(UState, "Owlwok Freighter S", Player, Exporter, doOrbit: false);
        Ship fromShipyard = Ship.CreateShipAtShipyard(UState, "Owlwok Freighter S", Player, Exporter.Position, Exporter);
        foreach (Ship freighter in new[] { fromPlanet, fromShipyard })
        {
            freighter.TransportingFood = true;
            AssertEqual(Exporter, freighter.TakingOffFrom, "setup: a new freighter takes off from its colony");
            freighter.AI.SetupFreighterPlan(Exporter, Importer, Goods.Food);
        }

        RunObjectsSim(TestSimStep);
        foreach (Ship freighter in new[] { fromPlanet, fromShipyard })
        {
            AssertGreaterThan(freighter.GetCargo(Goods.Food), 0f, "a new freighter given its colony's job loads there without landing");
            Assert.IsTrue(freighter.IsLaunching, "the new freighter carries on with its take-off");
        }
    }

    [TestMethod]
    public void ADockedFreighterIsOfferedItsColonysExportsFirst()
    {
        Ship freighter = DockAfterUnloadingAtImporter();
        Player.LoadFreightersAtTheirColony();
        Assert.IsFalse(freighter.AI.HasTradePlan, "with nothing to export from its colony, the freighter stays free for other jobs");

        LetImporterExportProductionToExporter();
        Player.LoadFreightersAtTheirColony();
        Assert.IsTrue(HasTradeGoal(freighter, ShipAI.Plan.PickupGoods, out ShipAI.ShipGoal goal)
                      && goal.Trade.ExportFrom == Importer && goal.Trade.ImportTo == Exporter,
                      "a docked freighter is given its colony's exports first");
    }

    [TestMethod]
    public void TheShipListShowsADockedFreighterAsDocked()
    {
        Ship freighter = DockAfterUnloadingAtImporter();
        string docked = Localizer.Token(GameText.ShipListDocked);
        Assert.IsFalse(string.IsNullOrEmpty(docked), "setup: the docked text must exist");
        StringAssert.EndsWith(ShipListScreenItem.GetStatusText(freighter), docked, "a docked freighter's orders say it is docked");

        RunSimWhile((simTimeout: 60, fatal: true), () => !freighter.IsLaunching);
        Assert.IsFalse(ShipListScreenItem.GetStatusText(freighter).Contains(docked), "a freighter taking off is no longer docked");
    }

    void FollowFromTheShipList(Ship freighter)
    {
        Universe.SelectedShip = freighter; // what clicking its row in the ship list does
        Universe.ShipToView = freighter;
        Universe.ViewingShip = true;
        Universe.UpdateSelectedShips();
        Assert.IsNull(Universe.SelectedShip, "a docked freighter cannot stay selected");
    }

    [TestMethod]
    public void AFollowedFreighterIsSelectedAgainWhenItTakesOff()
    {
        Ship freighter = DockAfterUnloadingAtImporter();
        FollowFromTheShipList(freighter);
        Assert.IsNull(Universe.TakeFollowedShipThatTookOff(), "it is not selected again while it is docked");

        RunSimWhile((simTimeout: 60, fatal: true), () => !freighter.IsLaunching);
        AssertEqual(freighter, Universe.TakeFollowedShipThatTookOff(), "the freighter the camera follows is selected again once it takes off");
        Assert.IsNull(Universe.TakeFollowedShipThatTookOff(), "it is selected again only once");
    }

    [TestMethod]
    public void AFreighterNoLongerFollowedIsNotSelectedAgain()
    {
        Ship freighter = DockAfterUnloadingAtImporter();
        FollowFromTheShipList(freighter);
        Universe.ViewingShip = false; // the player panned away
        RunSimWhile((simTimeout: 60, fatal: true), () => !freighter.IsLaunching);
        Assert.IsNull(Universe.TakeFollowedShipThatTookOff(), "a freighter the camera stopped following is not selected again");
    }

    [TestMethod]
    public void ARefitOrderMakesADockedFreighterTakeOffAtOnce()
    {
        Ship freighter = DockAfterUnloadingAtImporter();
        Player.AI.AddGoalAndEvaluate(new RefitShip(freighter, ResourceManager.Ships.GetDesign("Owlwok Freighter M"), Player));
        Assert.IsTrue(freighter.IsLaunching, "a docked freighter takes off at once to be refitted");
        AssertEqual(AIState.Refit, freighter.AI.State, "a docked freighter goes straight to its refit");
        Assert.IsFalse(freighter.IsIdleFreighter, "so it cannot be handed a trade job and lose the cargo at the refit");
    }

    [TestMethod]
    public void AScrapOrderMakesADockedFreighterTakeOffAtOnce()
    {
        Ship freighter = DockAfterUnloadingAtImporter();
        freighter.AI.OrderScrapShip();
        Assert.IsTrue(freighter.IsLaunching, "a docked freighter takes off at once to be scrapped");
        AssertEqual(AIState.Scrap, freighter.AI.State, "a docked freighter goes straight to be scrapped");
        Assert.IsFalse(freighter.IsIdleFreighter, "so it cannot be handed a trade job and lose the cargo when scrapped");
    }

    [TestMethod]
    public void AFreighterBuiltAtAColonyIsGivenItsColonysJobAsItTakesOff()
    {
        Ship shipyard = SpawnShip("Shipyard", Player, Exporter.Position + new Vector2(2500, 0));
        shipyard.TetherToPlanet(Exporter);
        Exporter.ManualFoodExportSlots = 1;
        Importer.ManualFoodImportSlots = 1;
        Exporter.UpdateIncomingTradeGoods();
        Importer.UpdateIncomingTradeGoods();

        UState.Debug = true;
        Exporter.Construction.Enqueue(ResourceManager.Ships.GetDesign("Owlwok Freighter S"), QueueItemType.Freighter);
        AssertEqual(1, Exporter.ConstructionQueue.Count, "setup: the colony must queue the freighter");
        Exporter.Construction.RushProduction(0, 10000, rushButton: true);
        RunObjectsSim(TestSimStep);

        Ship built = null;
        foreach (Ship ship in Player.OwnedShips)
            if (ship.IsFreighter)
                built = ship;
        Assert.IsNotNull(built, "setup: the colony must build the freighter");
        AssertEqual(Exporter, built.TakingOffFrom, "a freighter built at a shipyard takes off from its colony");
        AssertGreaterThan(built.GetCargo(Goods.Food), 0f, "a new freighter is given its colony's job when it is built and loads as it takes off");
        Assert.IsTrue(HasTradeGoal(built, ShipAI.Plan.DropOffGoods, out ShipAI.ShipGoal goal) && goal.Trade.ImportTo == Importer,
                      "the new freighter takes off bound for the importing colony");
    }

    static void MoveAlongOrbit(Planet planet, float degrees)
    {
        planet.OrbitalAngle += degrees;
        planet.Position = planet.System.Position.PointFromAngle(planet.OrbitalAngle, planet.OrbitalRadius);
    }

    [TestMethod]
    public void ADockedFreighterStaysWithItsColonyAsItMoves()
    {
        Ship freighter = DockAfterUnloadingAtImporter();
        MoveAlongOrbit(Importer, 3f);
        RunObjectsSim(TestSimStep);
        AssertLessThan(freighter.Position.Distance(Importer.Position), LandShip.TouchdownRadius + 5f,
                       "a docked freighter moves with its colony along the orbit");
    }

    Ship DockOnExportersSpacePort()
    {
        Exporter.PS = Planet.GoodState.IMPORT;
        Exporter.ProdHere = 0;
        Ship freighter = SpawnFreighter(Exporter, new Vector2(8000, 0));
        freighter.LoadProduction(5f);
        freighter.AI.SetupFreighterPlan(Exporter, Exporter, Goods.Production);
        RunSimWhile((simTimeout: 120, fatal: true), () => freighter.LandShip is not { Docked: true });
        Assert.IsTrue(freighter.LandShip.OnDock, "setup: the freighter must be docked on the space port");
        return freighter;
    }

    [TestMethod]
    public void AFreighterDockedOnASpacePortLoadsThereWithoutLandingAgain()
    {
        Ship freighter = DockOnExportersSpacePort();
        freighter.AI.SetupFreighterPlan(Exporter, Importer, Goods.Food);
        RunObjectsSim(TestSimStep);

        AssertEqual(Exporter, freighter.TakingOffFrom, "a freighter leaving a space port is leaving its colony");
        AssertGreaterThan(freighter.GetCargo(Goods.Food), 0f, "it loads its colony's goods as it takes off from the space port");
        RunSimWhile((simTimeout: 60, fatal: true), () => freighter.IsLaunching,
                    () => Assert.IsFalse(freighter.IsLanding, "the freighter does not land again to load"));
    }

    [TestMethod]
    public void ADockedFreighterStaysWithItsSpacePortAsTheColonyMoves()
    {
        Ship freighter = DockOnExportersSpacePort();
        MoveAlongOrbit(Exporter, 3f);
        RunObjectsSim(TestSimStep);
        AssertLessThan(freighter.Position.Distance(Exporter.Position), LandShip.TouchdownRadius + 5f,
                       "a freighter docked on a space port moves with its colony along the orbit");
    }

    [TestMethod]
    public void TheTurnsJobHandOutOffersADockedFreighterItsColonysJobFirst()
    {
        Assert.IsTrue(Player.NonCybernetic, "setup: food must be handed out before production");
        Ship freighter = DockAfterUnloadingAtImporter();
        LetImporterExportProductionToExporter();
        Exporter.ManualFoodExportSlots = 1;
        Importer.ManualFoodImportSlots = 1;
        Exporter.UpdateIncomingTradeGoods();
        Importer.UpdateIncomingTradeGoods();

        Player.DispatchBuildAndScrapFreighters();
        Assert.IsTrue(HasTradeGoal(freighter, ShipAI.Plan.PickupGoods, out ShipAI.ShipGoal goal) && goal.Trade.ExportFrom == Importer,
                      "the turn's hand-out gives a docked freighter its own colony's job before food from elsewhere");
    }

    [TestMethod]
    public void ADockedFreighterTakesOffWhenItsColonyIsLost()
    {
        Ship freighter = DockAfterUnloadingAtImporter();
        Importer.SetOwner(Enemy);
        RunObjectsSim(TestSimStep);
        Assert.IsTrue(freighter.IsLaunching, "a freighter does not stay docked at a colony its empire has lost");
    }

    [TestMethod]
    public void TripEstimatesOfADockedFreighterStartWithItsTakeOff()
    {
        Ship freighter = DockAfterUnloadingAtImporter();
        LetImporterExportProductionToExporter();
        float takeOff = freighter.GetTradeTakeOffTime(Importer);

        float toColony = freighter.GetAstrogateTimeTo(Importer);
        float colonyToExporter = freighter.GetAstrogateTimeBetween(Importer, Exporter);
        Assert.IsTrue(freighter.TryGetBestTradeRoute(Goods.Production, new[] { Importer }, Exporter, out Ship.ExportPlanetAndEta route),
                      "setup: the docked freighter must find the route from its colony");
        AssertEqual((int)(takeOff + toColony + 0f + colonyToExporter + freighter.GetTradeLandingTime(Exporter)), route.Eta,
                    "a job at the colony it is docked at costs the take-off, the flight and the landing, but no landing to load");

        float toExporter = freighter.GetAstrogateTimeTo(Exporter);
        float stopAtExporter = freighter.GetTradeLandingTime(Exporter) + freighter.GetTradeTakeOffTime(Exporter);
        float exporterToColony = freighter.GetAstrogateTimeBetween(Exporter, Importer);
        Assert.IsTrue(freighter.TryGetBestTradeRoute(Goods.Food, new[] { Exporter }, Importer, out route),
                      "setup: the docked freighter must find a route from another colony");
        AssertEqual((int)(takeOff + toExporter + stopAtExporter + exporterToColony + freighter.GetTradeLandingTime(Importer)), route.Eta,
                    "a job elsewhere starts with taking off from the colony it is docked at");
    }

    [TestMethod]
    public void ADockedFreighterIsStillDockedAfterALoad()
    {
        UState.StarDate = 1042.5f;
        Ship freighter = DockAfterUnloadingAtImporter();
        RunObjectsSim(UState.P.TurnTimer * 0.5f);
        Assert.IsTrue(freighter.LandShip is { Docked: true }, "setup: the freighter must still be docked halfway through its wait");
        SavedGame save = Universe.Save("UnitTest.FreighterDocked", throwOnError: true);
        UniverseScreen loaded = LoadGame.Load(save.SaveFile, noErrorDialogs: true, startSimThread: false);
        Ship docked = loaded.UState.Objects.FindShip(freighter.Id);
        Assert.IsTrue(docked is { Active: true, LandShip.Docked: true }, "a docked freighter is still docked after a load");

        double waited = 0;
        for (; docked.IsLanding; waited += TestSimStepD)
        {
            AssertLessThan(waited, loaded.UState.P.TurnTimer + 1.0, "the loaded freighter takes off when its wait is up");
            loaded.UState.Objects.Update(TestSimStep);
        }
        Assert.IsTrue(docked.IsLaunching, "the loaded freighter takes off when its wait is up");
        AssertEqual(0.2f, loaded.UState.P.TurnTimer * 0.5f, (float)waited, "the loaded freighter waits out the rest of its turn");
    }

    [TestMethod]
    public void AFreighterTakingOffFromItsColonyLoadsForAStationThere()
    {
        Ship station = SpawnResearchStation(Exporter.Position + new Vector2(30_000, 0));
        Ship freighter = Ship.CreateShipNearPlanet(UState, "Owlwok Freighter S", Player, Exporter, doOrbit: false);
        freighter.TransportingProduction = true;
        freighter.AI.SetupFreighterPlan(Exporter, station, Goods.Production);
        RunObjectsSim(TestSimStep);
        AssertGreaterThan(freighter.GetCargo(Goods.Production), 0f, "a station's supplies are loaded at the colony the freighter is taking off from");
        Assert.IsTrue(HasTradeGoal(freighter, ShipAI.Plan.DropOffGoodsForStation, out _), "the freighter takes off bound for the station");
    }

    [TestMethod]
    public void AFreighterDoesNotLandOnAColonyItsEmpireHasLost()
    {
        Ship control = SpawnFreighter(Exporter, new Vector2(Exporter.Radius + 200f, 0));
        control.AI.SetupFreighterPlan(Exporter, Importer, Goods.Food);
        RunObjectsSim(TestSimStep);
        Assert.IsTrue(control.IsLanding, "setup: a freighter this close to the colony starts down at once");

        Ship freighter = SpawnFreighter(Exporter, new Vector2(-Exporter.Radius - 200f, 0));
        freighter.AI.SetupFreighterPlan(Exporter, Importer, Goods.Food);
        Exporter.SetOwner(Enemy);
        RunObjectsSim(TestSimStep);
        Assert.IsFalse(freighter.IsLanding, "a freighter must not land to load at a colony that is no longer its empire's");
        Assert.IsFalse(freighter.AI.OrderQueue.TryPeekLast(out ShipAI.ShipGoal g) && g.Trade != null, "the trade is called off");
    }

    [TestMethod]
    public void AFreighterDoesNotLandWhenTheGoodsAreGone()
    {
        Exporter.FoodHere = 0;
        Ship freighter = SpawnFreighter(Exporter, new Vector2(Exporter.Radius + 200f, 0));
        freighter.AI.SetupFreighterPlan(Exporter, Importer, Goods.Food);
        RunObjectsSim(TestSimStep);
        Assert.IsFalse(freighter.IsLanding, "a freighter must not land to load goods that are no longer there");
        Assert.IsFalse(freighter.AI.OrderQueue.TryPeekLast(out ShipAI.ShipGoal g) && g.Trade != null, "the trade is called off");
    }

    [TestMethod]
    public void NothingIsLoadedFromAColonyLostWhileTheFreighterIsDown()
    {
        Ship freighter = SpawnFreighter(Exporter, new Vector2(8000, 0));
        freighter.AI.SetupFreighterPlan(Exporter, Importer, Goods.Food);
        RunUntilLanding(freighter);
        Exporter.SetOwner(Enemy);
        float foodBefore = Exporter.FoodHere;

        RunUntilLanded(freighter);
        Assert.IsTrue(freighter.IsLaunching, "the freighter takes off again");
        AssertEqual(0f, freighter.GetCargo(Goods.Food), "nothing is loaded from a colony its empire has lost");
        AssertEqual(foodBefore, Exporter.FoodHere, "nothing is loaded from a colony its empire has lost");
        Assert.IsFalse(freighter.AI.OrderQueue.TryPeekLast(out ShipAI.ShipGoal g) && g.Trade != null, "the trade is called off");
    }

    [TestMethod]
    public void NothingIsUnloadedOnAColonyLostWhileTheFreighterIsDown()
    {
        Ship freighter = SpawnFreighter(Importer, new Vector2(8000, 0));
        freighter.LoadFood(5f);
        freighter.AI.SetupFreighterPlan(Importer, Importer, Goods.Food);
        RunUntilLanding(freighter);
        Importer.SetOwner(Enemy);
        float foodBefore = Importer.FoodHere;

        RunUntilLanded(freighter);
        Assert.IsTrue(freighter.IsLaunching, "the freighter takes off again");
        AssertEqual(foodBefore, Importer.FoodHere, "nothing is unloaded on a colony its empire has lost");
        AssertEqual(1f, 5f, freighter.GetCargo(Goods.Food), "the freighter keeps its cargo");
    }

    [TestMethod]
    public void TripEstimatesIncludeLandingAndTakingOff()
    {
        Ship freighter = SpawnFreighter(Exporter, new Vector2(30_000, 0));
        float turn = UState.P.TurnTimer;
        float portLanding = LaunchShip.ShipyardDuration(freighter, LaunchShip.ShipyardRotationDegX(freighter));
        AssertEqual(0.001f, portLanding / turn, freighter.GetTradeLandingTime(Exporter), "a space port landing takes a shipyard glide");
        AssertEqual(0.001f, portLanding * 0.9f / turn, freighter.GetTradeTakeOffTime(Exporter), "a space port take-off is a shipyard launch");
        AssertEqual(0.001f, LaunchShip.PlanetDuration(freighter) / turn, freighter.GetTradeLandingTime(Importer), "without a space port it lands on the planet");
        AssertEqual(0.001f, LaunchShip.PlanetDuration(freighter) / turn, freighter.GetTradeTakeOffTime(Importer), "without a space port it takes off from the planet");

        float toExporter = freighter.GetAstrogateTimeTo(Exporter);
        float stopToLoad = freighter.GetTradeLandingTime(Exporter) + freighter.GetTradeTakeOffTime(Exporter);
        float toImporter = freighter.GetAstrogateTimeBetween(Exporter, Importer);
        float landToUnload = freighter.GetTradeLandingTime(Importer);
        AssertGreaterThan(stopToLoad + landToUnload, 1f, "setup: the landings must add at least a turn");
        Assert.IsTrue(freighter.TryGetBestTradeRoute(Goods.Food, new[] { Exporter }, Importer, out Ship.ExportPlanetAndEta route),
                      "setup: the freighter must find the route");
        AssertEqual((int)(toExporter + stopToLoad + toImporter + landToUnload), route.Eta,
                    "the trip estimate adds landing to load, taking off and landing to unload");

        freighter.LoadFood(freighter.CargoSpaceMax);
        Assert.IsTrue(freighter.TryGetBestTradeRoute(Goods.Food, Array.Empty<Planet>(), Importer, out route),
                      "setup: a loaded freighter must find the direct route");
        AssertEqual((int)(freighter.GetAstrogateTimeTo(Importer) + freighter.GetTradeLandingTime(Importer)), route.Eta,
                    "a freighter with the goods on board adds only the landing to unload");
    }

    [TestMethod]
    public void AFreighterLandsOnAResearchStationToUnload()
    {
        Ship station = SpawnResearchStation(Exporter.Position + new Vector2(30_000, 0));
        Assert.IsNull(station.GetTether(), "setup: the station must sit in open space");
        Ship freighter = SpawnFreighter(Exporter, new Vector2(42_000, 0));
        freighter.LoadProduction(5f);
        freighter.AI.SetTradePlan(ShipAI.Plan.DropOffGoodsForStation, Exporter, station, Goods.Production);
        AssertLandsOnStationAndUnloads(freighter, station);
    }

    [TestMethod]
    public void AFreighterLandsOnAStationOrbitingAPlanet()
    {
        Ship station = SpawnResearchStation(Importer.Position + new Vector2(2000, 0));
        station.TetherToPlanet(Importer);
        Ship freighter = SpawnFreighter(Importer, new Vector2(12_000, 0));
        freighter.LoadProduction(5f);
        freighter.AI.SetTradePlan(ShipAI.Plan.DropOffGoodsForStation, Exporter, station, Goods.Production);
        AssertLandsOnStationAndUnloads(freighter, station);
    }

    [TestMethod]
    public void NothingIsUnloadedOnAStationLostWhileTheFreighterIsDown()
    {
        Ship station = SpawnResearchStation(Exporter.Position + new Vector2(30_000, 0));
        Ship freighter = SpawnFreighter(Exporter, new Vector2(42_000, 0));
        freighter.LoadProduction(5f);
        freighter.AI.SetTradePlan(ShipAI.Plan.DropOffGoodsForStation, Exporter, station, Goods.Production);
        RunUntilLanding(freighter);
        station.QueueTotalRemoval();
        float prodBefore = station.GetProduction();

        RunUntilLanded(freighter);
        Assert.IsTrue(freighter.IsLaunching, "the freighter takes off again");
        AssertEqual(prodBefore, station.GetProduction(), "nothing is unloaded on a station that is gone");
        AssertEqual(1f, 5f, freighter.GetCargo(Goods.Production), "the freighter keeps its cargo");
    }

    [TestMethod]
    public void AFreighterSupplyingAStationLoadsAtTheColonyAndUnloadsOnTheStation()
    {
        Ship station = SpawnResearchStation(Exporter.Position + new Vector2(30_000, 0));
        Ship freighter = SpawnFreighter(Exporter, new Vector2(8000, 0));
        freighter.AI.SetupFreighterPlan(Exporter, station, Goods.Production);

        RunUntilLanding(freighter);
        Assert.IsTrue(HasTradeGoal(freighter, ShipAI.Plan.PickupGoodsForStation, out _), "a landing freighter keeps its trade orders");
        AssertEqual(0f, freighter.GetCargo(Goods.Production), "nothing is loaded before the freighter has landed");
        RunUntilLanded(freighter);
        AssertLessThan(freighter.Position.Distance(Exporter.Position), LandShip.TouchdownRadius + 5f, "the freighter loads on the colony's space port");
        AssertGreaterThan(freighter.GetCargo(Goods.Production), 0f, "the freighter loads at touchdown");
        Assert.IsTrue(HasTradeGoal(freighter, ShipAI.Plan.DropOffGoodsForStation, out ShipAI.ShipGoal goal) && goal.Trade.TargetStation == station,
                      "the freighter takes off bound for the station");

        AssertLandsOnStationAndUnloads(freighter, station);
    }

    [TestMethod]
    public void TripEstimatesToAStationIncludeLandingOnIt()
    {
        Ship station = SpawnResearchStation(Exporter.Position + new Vector2(30_000, 0));
        Ship freighter = SpawnFreighter(Exporter, new Vector2(-30_000, 0));
        AssertEqual(0.001f, LandShip.DockLandingSeconds(freighter) / UState.P.TurnTimer, freighter.GetStationLandingTime(),
                    "landing on a station takes a shipyard glide");

        float toExporter = freighter.GetAstrogateTimeTo(Exporter);
        float stopToLoad = freighter.GetTradeLandingTime(Exporter) + freighter.GetTradeTakeOffTime(Exporter);
        float toStation = freighter.GetAstrogateTimeBetween(Exporter, station);
        float landOnStation = freighter.GetStationLandingTime();
        Assert.IsTrue(freighter.TryGetBestTradeRoute(Goods.Production, new[] { Exporter }, station, out Ship.ExportPlanetAndEta route),
                      "setup: the freighter must find the route");
        AssertEqual((int)(toExporter + stopToLoad + toStation + landOnStation), route.Eta,
                    "the trip estimate adds landing to load, taking off and landing on the station");
    }

    [TestMethod]
    public void ALandingFreighterStillCountsAndStaysOnTheShipList()
    {
        Ship freighter = SpawnFreighter(Exporter, new Vector2(8000, 0));
        freighter.AI.SetupFreighterPlan(Exporter, Importer, Goods.Food);
        RunObjectsSim(TestSimStep);
        int freighters = Player.TotalFreighters;
        AssertGreaterThan(freighters, 0, "setup: the empire must own the freighter");

        bool landed = false, tookOff = false;
        RunSimWhile((simTimeout: 180, fatal: true), () => !tookOff || freighter.IsLaunching, () =>
        {
            landed |= freighter.IsLanding;
            tookOff |= landed && freighter.IsLaunching;
            AssertEqual(freighters, Player.TotalFreighters, "landing and taking off never change the number of freighters");
            Assert.IsTrue(freighter.AI.State == AIState.SystemTrader && freighter.AI.HasTradeGoal(Goods.Food),
                          "a freighter landing or taking off on a delivery counts as a trading freighter");
            Assert.IsFalse(ShipListScreen.IsLeftOffTheList(freighter), "a landing or launching freighter stays on the ship list");
        });

        Ship scrapped = SpawnFreighter(Exporter, new Vector2(Exporter.Radius + 200f, 0));
        scrapped.InitLanding(LandPlan.Scrap, Exporter);
        Assert.IsTrue(ShipListScreen.IsLeftOffTheList(scrapped), "other landing ships are left off the ship list");
    }

    [TestMethod]
    public void ALandingFreighterKeepsPayingUpkeep()
    {
        Ship freighter = SpawnFreighter(Exporter, new Vector2(8000, 0));
        float upkeep = freighter.GetMaintCost();
        AssertGreaterThan(upkeep, 0f, "setup: the freighter must cost upkeep");
        freighter.AI.SetupFreighterPlan(Exporter, Importer, Goods.Food);
        RunUntilLanding(freighter);
        AssertEqual(upkeep, freighter.GetMaintCost(), "a freighter landing to trade still pays its upkeep");
    }

    [TestMethod]
    public void ARefitOrderedWhileAFreighterIsDownWaitsUntilItHasTakenOff()
    {
        Ship freighter = SpawnFreighter(Exporter, new Vector2(8000, 0));
        freighter.AI.SetupFreighterPlan(Exporter, Importer, Goods.Food);
        RunUntilLanding(freighter);

        var refit = new RefitShip(freighter, ResourceManager.Ships.GetDesign("Owlwok Freighter M"), Player);
        Player.AI.AddGoalAndEvaluate(refit);
        Assert.IsTrue(Player.AI.HasGoal(g => g == refit), "a refit ordered while the freighter is down waits for it");
        Assert.IsTrue(HasTradeGoal(freighter, ShipAI.Plan.PickupGoods, out _), "the freighter carries on with its landing");

        RunUntilLanded(freighter);
        refit.Evaluate();
        AssertEqual(AIState.Refit, freighter.AI.State, "once the freighter is up, it goes to be refitted");
    }

    [TestMethod]
    public void AScrapOrderedWhileAFreighterIsDownWaitsUntilItHasTakenOff()
    {
        Ship freighter = SpawnFreighter(Exporter, new Vector2(8000, 0));
        freighter.AI.SetupFreighterPlan(Exporter, Importer, Goods.Food);
        RunUntilLanding(freighter);

        freighter.AI.OrderScrapShip();
        Goal scrap = Player.AI.FindGoal(g => g.Type == GoalType.ScrapShip && g.OldShip == freighter);
        Assert.IsNotNull(scrap, "a scrap ordered while the freighter is down waits for it");
        Assert.IsTrue(HasTradeGoal(freighter, ShipAI.Plan.PickupGoods, out _), "the freighter carries on with its landing");

        RunUntilLanded(freighter);
        scrap.Evaluate();
        AssertEqual(AIState.Scrap, freighter.AI.State, "once the freighter is up, it goes to be scrapped");
    }

    [TestMethod]
    public void AFreighterLandingOnAStationStillUnloadsAfterALoad()
    {
        UState.StarDate = 1042.5f;
        Ship station = SpawnResearchStation(Exporter.Position + new Vector2(30_000, 0));
        Ship freighter = SpawnFreighter(Exporter, new Vector2(42_000, 0));
        freighter.LoadProduction(5f);
        freighter.AI.SetTradePlan(ShipAI.Plan.DropOffGoodsForStation, Exporter, station, Goods.Production);
        float prodBefore = station.GetProduction();
        RunUntilLanding(freighter);

        SavedGame save = Universe.Save("UnitTest.FreighterStationLanding", throwOnError: true);
        UniverseScreen loaded = LoadGame.Load(save.SaveFile, noErrorDialogs: true, startSimThread: false);
        Ship landing = loaded.UState.Objects.FindShip(freighter.Id);
        Ship loadedStation = loaded.UState.Objects.FindShip(station.Id);
        Assert.IsTrue(landing is { Active: true, IsLanding: true } && landing.LandShip.Station == loadedStation,
                      "a freighter landing on a station must still be landing on it after a load");

        for (double time = 0; landing.IsLanding; time += TestSimStepD)
        {
            AssertLessThan(time, 60.0, "the loaded landing must finish");
            loaded.UState.Objects.Update(TestSimStep);
        }

        AssertEqual(1f, prodBefore + 5f, loadedStation.GetProduction(), "the loaded freighter unloads on the station at touchdown");
        AssertLessThan(landing.Position.Distance(loadedStation.Position), LandShip.TouchdownRadius + 5f, "the loaded landing must still glide into the station");
    }

    [TestMethod]
    public void ALandingFreighterStillTradesAfterALoad()
    {
        UState.StarDate = 1042.5f;
        Ship freighter = SpawnFreighter(Exporter, new Vector2(8000, 0));
        freighter.AI.SetupFreighterPlan(Exporter, Importer, Goods.Food);
        RunUntilLanding(freighter);

        SavedGame save = Universe.Save("UnitTest.FreighterLanding", throwOnError: true);
        UniverseScreen loaded = LoadGame.Load(save.SaveFile, noErrorDialogs: true, startSimThread: false);
        Ship landing = loaded.UState.Objects.FindShip(freighter.Id);
        Planet loadedExporter = loaded.UState.GetPlanet(Exporter.Id);
        Assert.IsTrue(landing is { Active: true, IsLanding: true }, "a landing freighter must still be landing after a load");
        Assert.IsTrue(HasTradeGoal(landing, ShipAI.Plan.PickupGoods, out _), "a landing freighter keeps its trade orders after a load");

        for (double time = 0; landing.IsLanding; time += TestSimStepD)
        {
            AssertLessThan(time, 60.0, "the loaded landing must finish");
            loaded.UState.Objects.Update(TestSimStep);
        }

        AssertGreaterThan(landing.GetCargo(Goods.Food), 0f, "the loaded freighter loads at touchdown");
        AssertLessThan(landing.Position.Distance(loadedExporter.Position), LandShip.TouchdownRadius + 5f, "the loaded landing must still glide into the space port");
        Assert.IsTrue(HasTradeGoal(landing, ShipAI.Plan.DropOffGoods, out _), "the loaded freighter takes off bound for the importing colony");
    }
}
