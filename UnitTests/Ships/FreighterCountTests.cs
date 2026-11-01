using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Ship_Game;
using Ship_Game.AI;
using Ship_Game.GameScreens.LoadGame;
using Ship_Game.Ships;
using Vector2 = SDGraphics.Vector2;
#pragma warning disable CA2213

namespace UnitTests.Ships;

[TestClass]
public class FreighterCountTests : StarDriveTest
{
    readonly Planet Exporter;
    readonly Planet Importer;

    public FreighterCountTests()
    {
        LoadStarterShips("Owlwok Freighter S");
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
    }

    Ship FreighterSentForFood()
    {
        Ship freighter = SpawnShip("Owlwok Freighter S", Player, Exporter.Position + new Vector2(3000f, 0f));
        freighter.TransportingFood = true;
        freighter.AI.SetupFreighterPlan(Exporter, Importer, Goods.Food);
        Exporter.UpdateIncomingTradeGoods();
        Importer.UpdateIncomingTradeGoods();
        return freighter;
    }

    static bool CarriesFoodIn(Ship freighter) => freighter.AI.FindGoal(ShipAI.Plan.DropOffGoods, out _);

    [TestMethod]
    public void AColonysFreighterCountsAreBackRightAfterALoad()
    {
        Ship freighter = FreighterSentForFood();
        AssertEqual(1, Importer.IncomingFoodFreighters, "setup: one freighter is on its way");
        AssertEqual(1, Exporter.OutgoingFoodFreighters, "setup: one freighter is on its way");

        UState.StarDate = 1042.5f;
        SavedGame save = Universe.Save("UnitTest.FreighterCounts", throwOnError: true);
        UniverseScreen loaded = LoadSave(save.SaveFile);
        Planet importer = loaded.UState.Player.GetPlanets().FirstOrDefault(p => p.Id == Importer.Id);
        Planet exporter = loaded.UState.Player.GetPlanets().FirstOrDefault(p => p.Id == Exporter.Id);
        Assert.IsNotNull(loaded.UState.Objects.FindShip(freighter.Id), "setup: the freighter is in the save");

        AssertEqual(1, importer.IncomingFoodFreighters, "the colony still counts the freighter coming in");
        AssertEqual(1, exporter.OutgoingFoodFreighters, "the exporter still counts the freighter it sent");
    }

    [TestMethod]
    public void TheIncomingAmountFollowsTheCargoAsItIsLoadedAndDelivered()
    {
        Ship freighter = FreighterSentForFood();
        AssertEqual(0f, Importer.IncomingFood, "setup: nothing is loaded yet");

        RunSimWhile((simTimeout: 120, fatal: true), () => !CarriesFoodIn(freighter));
        float cargo = freighter.GetCargo(Goods.Food);
        AssertGreaterThan(cargo, 0f, "setup: the freighter loaded food");
        AssertEqual(0.01f, cargo, Importer.IncomingFood, "the colony counts the food as soon as it is loaded");

        Importer.UpdateIncomingTradeGoods();
        AssertEqual(0.01f, cargo, Importer.IncomingFood, "setup: the turn's recount agrees");

        RunSimWhile((simTimeout: 120, fatal: true), () => CarriesFoodIn(freighter));
        AssertEqual(0f, Importer.IncomingFood, "once the food is delivered it is no longer counted as coming in");
    }

    [TestMethod]
    public void FoodAlreadyAboardIsCountedFromTheMomentItIsSent()
    {
        Ship freighter = SpawnShip("Owlwok Freighter S", Player, Exporter.Position + new Vector2(3000f, 0f));
        freighter.TransportingFood = true;
        float cargo = freighter.LoadFood(10f);
        AssertGreaterThan(cargo, 0f, "setup: the freighter carries food");

        freighter.AI.SetupFreighterPlan(Importer, Importer, Goods.Food);
        AssertEqual(0.01f, cargo, Importer.IncomingFood, "food already aboard counts as coming in as soon as the freighter is sent");

        RunSimWhile((simTimeout: 120, fatal: true), () => CarriesFoodIn(freighter));
        AssertEqual(0f, Importer.IncomingFood, "and stops counting once delivered");
    }

    Ship FreighterSentForFoodWithoutARecount()
    {
        Ship freighter = SpawnShip("Owlwok Freighter S", Player, Exporter.Position + new Vector2(3000f, 0f));
        freighter.TransportingFood = true;
        freighter.AI.SetupFreighterPlan(Exporter, Importer, Goods.Food);
        return freighter;
    }

    [TestMethod]
    public void AFreighterCountsOnceFromTheMomentItIsSentUntilItDelivers()
    {
        Ship freighter = FreighterSentForFoodWithoutARecount();
        AssertEqual(1, Importer.IncomingFoodFreighters, "the colony counts the freighter as soon as it is sent");
        AssertEqual(1, Exporter.OutgoingFoodFreighters, "and so does the colony it loads at");

        RunSimWhile((simTimeout: 120, fatal: true), () => !CarriesFoodIn(freighter));
        AssertEqual(1, Importer.IncomingFoodFreighters, "a loaded freighter is still one freighter coming in");
        AssertEqual(1, Exporter.OutgoingFoodFreighters, "and one going out, not two");

        RunSimWhile((simTimeout: 120, fatal: true), () => CarriesFoodIn(freighter));
        AssertEqual(0, Importer.IncomingFoodFreighters, "once it has delivered it no longer counts as coming in");
        AssertEqual(0, Exporter.OutgoingFoodFreighters, "or as going out");
    }

    [TestMethod]
    public void AFreighterThatGivesUpNoLongerCounts()
    {
        Ship freighter = FreighterSentForFoodWithoutARecount();
        AssertEqual(1, Exporter.OutgoingFoodFreighters, "setup: the freighter is counted");

        freighter.AI.CancelTradePlan();

        AssertEqual(0, Importer.IncomingFoodFreighters, "a freighter that gives up no longer counts as coming in");
        AssertEqual(0, Exporter.OutgoingFoodFreighters, "or as going out");
    }
}
