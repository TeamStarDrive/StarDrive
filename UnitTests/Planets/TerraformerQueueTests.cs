using System.Linq;
using System.Reflection;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Ship_Game;
using Vector2 = SDGraphics.Vector2;

namespace UnitTests.Planets;

[TestClass]
public class TerraformerQueueTests : StarDriveTest
{
    readonly Planet Home;

    public TerraformerQueueTests()
    {
        LoadStarterShips("Vulcan Scout");
        CreateUniverseAndPlayerEmpire();
        Home = AddHomeWorldToEmpire(new Vector2(1000), Player);
        Player.data.Traits.TerraformingLevel = 2;
        foreach (PlanetGridSquare tile in Home.TilesList)
            tile.Terraformable = false;
        foreach (PlanetGridSquare tile in Home.TilesList.Where(t => !t.Habitable).Take(6))
            tile.Terraformable = true;
        Assert.IsTrue(Home.Terraformable, "setup: tiles here can be terraformed");
    }

    QueueItem[] QueuedTerraformers() => Home.ConstructionQueue.Where(q => q.isBuilding && q.Building.IsTerraformer).ToArray();

    void QueueTerraformers(int count)
    {
        string name = ResourceManager.GetBuildingTemplate(Building.TerraformerId).Name;
        for (int i = 0; i < count; ++i)
            Assert.IsTrue(Home.Construction.Enqueue(ResourceManager.CreateBuilding(Home, name), playerAdded: true), "setup: a terraformer is queued");
    }

    // a turn's pass over the queue, with no production stored to finish anything; true when production was applied
    bool ApplyAProductionTurn(float stored = 0f, float spend = 1f)
    {
        Home.ProdHere = stored;
        return (bool)Home.Construction.GetType().GetMethod("ApplyProductionToQueue", BindingFlags.NonPublic | BindingFlags.Instance)!
            .Invoke(Home.Construction, new object[] { spend, 0, false, false, 0 })!;
    }

    // the planet holds all the terraformers it takes
    void FillTerraformerRoom()
    {
        string name = ResourceManager.GetBuildingTemplate(Building.TerraformerId).Name;
        int limit = Home.TerraformerLimit;
        foreach (PlanetGridSquare tile in Home.TilesList.Where(t => t.Habitable && t.NoBuildingOnTile).Take(limit).ToArray())
            tile.PlaceBuilding(ResourceManager.CreateBuilding(Home, name), Home);
        Home.UpdatePlanetStatsByRecalculation();
        Assert.AreEqual(limit, Home.TerraformersHere, "setup: the planet holds all the terraformers it takes");
    }

    [TestMethod]
    public void TheTurnsProductionGoesToTheItemBehindATerraformerPastTheLimit()
    {
        FillTerraformerRoom();
        QueueTerraformers(1);
        Home.Construction.Enqueue(ResourceManager.Ships.GetDesign("Vulcan Scout"), QueueItemType.CombatShip);
        QueueItem ship = Home.ConstructionQueue.Last(q => q.isShip);
        Assert.IsTrue(Home.ConstructionQueue[0].isBuilding && Home.ConstructionQueue[0].Building.IsTerraformer, "setup: the terraformer heads the queue");

        Home.ProdHere = 100f;
        Home.Construction.AutoApplyProduction(0f);

        Assert.AreEqual(0, QueuedTerraformers().Length, "the terraformer is dropped");
        Assert.IsTrue(ship.ProductionSpent > 0f, "and the ship behind it takes the turn's production");
    }

    [TestMethod]
    public void ATerraformerPastTheLimitAtTheHeadOfTheQueueGetsNoProductionBeforeItIsDropped()
    {
        FillTerraformerRoom();
        QueueTerraformers(1);
        Assert.AreEqual(1, Home.ConstructionQueue.Count, "setup: one more terraformer heads the queue");

        bool applied = ApplyAProductionTurn(stored: 100f, spend: 50f);

        Assert.AreEqual(0, QueuedTerraformers().Length, "it is dropped");
        Assert.AreEqual(100f, Home.ProdHere, 0.01f, "before any of the stored production is spent on it");
        Assert.IsFalse(applied, "and a rush on it reports that nothing was applied");
    }

    [TestMethod]
    public void QueuedTerraformersPastThePlanetsLimitAreDropped()
    {
        int limit = Home.TerraformerLimit;
        QueueTerraformers(limit + 2);
        QueueItem[] queued = QueuedTerraformers();
        Assert.AreEqual(limit + 2, queued.Length, "setup: two more terraformers are queued than the planet takes");

        ApplyAProductionTurn();

        QueueItem[] left = QueuedTerraformers();
        Assert.AreEqual(limit, left.Length, "the queue keeps as many terraformers as the planet takes");
        Assert.IsTrue(left.SequenceEqual(queued.Take(limit)), "the newest ones are the ones dropped");
    }

    [TestMethod]
    public void QueuedTerraformersAreDroppedOnceNothingIsLeftToTerraform()
    {
        QueueTerraformers(2);
        foreach (PlanetGridSquare tile in Home.TilesList)
            tile.Terraformable = false;
        Assert.IsFalse(Home.Terraformable, "setup: nothing is left to terraform");

        ApplyAProductionTurn();

        Assert.AreEqual(0, QueuedTerraformers().Length, "a terraformer still queued is dropped instead of being built and scrapped");
    }
}
