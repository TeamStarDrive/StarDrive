using System.Linq;
using System.Reflection;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Ship_Game;
using Ship_Game.AI;
using Ship_Game.Ships;
using Vector2 = SDGraphics.Vector2;

namespace UnitTests.Planets;

[TestClass]
public class ProductionImportOrderTests : StarDriveTest
{
    const string Freighter = "Owlwok Freighter S";
    readonly Planet Exporter;

    public ProductionImportOrderTests()
    {
        LoadStarterShips(Freighter);
        CreateUniverseAndPlayerEmpire();
        UState.Objects.EnableParallelUpdate = false;
        Exporter = AddHomeWorldToEmpire(new Vector2(200_000), Player, new Vector2(205_000), explored: true);
        Player.UpdateRallyPoints();
        Exporter.FS = Planet.GoodState.STORE;
        Exporter.PS = Planet.GoodState.EXPORT;
        Exporter.ProdHere = Exporter.Storage.Max;
    }

    Planet AddImporter(Vector2 sysPos, float prodHere, bool somethingToBuild = true, float maxPop = 4f)
    {
        Planet p = AddDummyPlanet(sysPos, 1f, 1f, maxPop, sysPos + new Vector2(5000), explored: true);
        p.SetOwner(Player);
        p.FS = Planet.GoodState.STORE;
        p.PS = Planet.GoodState.IMPORT;
        p.Storage.Max = 1000;
        p.ProdHere = prodHere;
        if (somethingToBuild)
        {
            IShipDesign design = ResourceManager.Ships.GetDesign(Freighter);
            p.Construction.EnqueueRefitShip(new QueueItem(p) { isShip = true, ShipData = design, Cost = 500f, QType = QueueItemType.CombatShip });
        }
        p.UpdateIncomingTradeGoods();
        return p;
    }

    Ship SpawnFreighter()
    {
        Ship freighter = SpawnShip(Freighter, Player, Exporter.Position + new Vector2(3000f, 0f));
        freighter.TransportingProduction = true;
        return freighter;
    }

    static float ServingRank(Planet p) => p.GetCachedIncomingCargoPriority(Goods.Production);

    Planet AddCyberneticColonyEatingItsStore(Vector2 sysPos, float prodHere)
    {
        Player.data.Traits.Cybernetic = 1;
        Planet p = AddImporter(sysPos, prodHere, somethingToBuild: false, maxPop: 20000f);
        p.Prod.Percent = 0;
        p.Population = p.MaxPopulation;
        p.UpdateIncomes();
        Assert.IsTrue(p.IsCybernetic && p.Prod.NetIncome < -1f, $"setup: the colony eats production, net {p.Prod.NetIncome}");
        return p;
    }

    [TestMethod]
    public void ProductionGoesFirstToTheColonyWhoseStoreRunsOutSoonest()
    {
        Planet stocked = AddImporter(new Vector2(300_000, 200_000), prodHere: 400f);
        Planet empty = AddImporter(new Vector2(200_000, 300_000), prodHere: 0f);
        empty.ChangeIncomingCargo(Goods.Production, 10f);
        Exporter.UpdateIncomingTradeGoods();
        AssertGreaterThan(stocked.FreeProdImportSlots, 0, "setup: the stocked colony takes production");
        AssertGreaterThan(empty.FreeProdImportSlots, 0, "setup: the empty colony takes production");
        AssertGreaterThan(Exporter.FreeProdExportSlots, 0, "setup: the home world sends production");
        Ship freighter = SpawnFreighter();
        RunObjectsSim(TestSimStep);
        Assert.IsTrue(freighter.IsIdleFreighter && Player.OwnedShips.Contains(freighter), "setup: the empire has an idle freighter");

        Player.DispatchBuildAndScrapFreighters();

        Assert.IsTrue(freighter.AI.FindGoal(ShipAI.Plan.PickupGoods, out ShipAI.ShipGoal goal), "setup: the freighter gets a job");
        Assert.AreEqual(empty, goal.Trade.ImportTo,
            "a colony with nothing in store and 10 production on its way is served before one with 400 in store and nothing coming");
    }

    [TestMethod]
    public void AColonyAFreighterIsAlreadyFetchingForWaitsBehindOneWithNothingComing()
    {
        Planet first = AddImporter(new Vector2(300_000, 200_000), prodHere: 0f);
        Planet second = AddImporter(new Vector2(200_000, 300_000), prodHere: 0f);
        Exporter.UpdateIncomingTradeGoods();

        SpawnFreighter().AI.SetupFreighterPlan(Exporter, first, Goods.Production);
        first.UpdateIncomingTradeGoods();

        AssertGreaterThan(first.ProdBeingFetched, 0f, "setup: the freighter's load counts as on its way");
        AssertGreaterThan(ServingRank(first), ServingRank(second),
            "a colony a freighter is already on its way to load for is served after one with nothing coming");
    }

    [TestMethod]
    public void AColonyWithNothingToBuildIsServedLast()
    {
        Planet nothingToBuild = AddImporter(new Vector2(300_000, 200_000), prodHere: 0f, somethingToBuild: false);
        Planet building = AddImporter(new Vector2(200_000, 300_000), prodHere: 400f);

        AssertGreaterThan(ServingRank(nothingToBuild), ServingRank(building),
            "an empty colony with nothing to build draws nothing from its store, so it waits behind one that is building");
    }

    [TestMethod]
    public void ProductionAlreadyOnItsWayCountsAsStored()
    {
        Planet supplied = AddImporter(new Vector2(300_000, 200_000), prodHere: 0f);
        supplied.ChangeIncomingCargo(Goods.Production, 300f);
        Planet stocked = AddImporter(new Vector2(200_000, 300_000), prodHere: 100f);

        AssertGreaterThan(ServingRank(supplied), ServingRank(stocked),
            "a colony with 300 production on its way is served after one with 100 in store and nothing coming");
    }

    [TestMethod]
    public void ACyberneticColonyEatingItsStoreIsServedBeforeOneWithALongerSupply()
    {
        Planet eating = AddCyberneticColonyEatingItsStore(new Vector2(300_000, 200_000), prodHere: 100f);
        Planet building = AddImporter(new Vector2(200_000, 300_000), prodHere: 1000f);

        AssertLessThan(ServingRank(eating), ServingRank(building),
            "a cybernetic colony with nothing to build still eats its store, so it is served before one with a longer supply");
    }

    [TestMethod]
    public void AStarvingCyberneticColonyIsServedFirst()
    {
        Planet starving = AddCyberneticColonyEatingItsStore(new Vector2(300_000, 200_000), prodHere: 0f);
        typeof(Planet).GetMethod("ApplyResources", BindingFlags.NonPublic | BindingFlags.Instance)!.Invoke(starving, null);
        Assert.IsTrue(starving.IsStarving, "setup: the colony starves");
        starving.ChangeIncomingCargo(Goods.Production, 1000f);
        Planet empty = AddImporter(new Vector2(200_000, 300_000), prodHere: 0f);

        AssertLessThan(ServingRank(starving), ServingRank(empty),
            "a starving cybernetic colony is served first, even with production on its way");
    }
}
