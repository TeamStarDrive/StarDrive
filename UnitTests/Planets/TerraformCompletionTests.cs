using System.Reflection;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Ship_Game;
using Ship_Game.Utils;
using Vector2 = SDGraphics.Vector2;
#pragma warning disable CA2213

namespace UnitTests.Planets;

/// <summary>
/// Once nothing is left to terraform at a colony's terraform level, the job is done: every terraformer goes, an event
/// terraformer such as a World Seed too, the player is told once, and the message names the level the job used.
/// </summary>
[TestClass]
public class TerraformCompletionTests : StarDriveTest
{
    readonly Planet Home;

    public TerraformCompletionTests()
    {
        CreateUniverseAndPlayerEmpire();
        Universe.NotificationManager = new NotificationManager(Universe.ScreenManager, Universe);
        Home = AddHomeWorldToEmpire(new Vector2(1000), Player);
        foreach (PlanetGridSquare tile in Home.TilesList)
            tile.Terraformable = false; // nothing left to terraform
    }

    void Place(string building)
    {
        PlanetGridSquare tile = Home.TilesList.Find(t => t.Habitable && t.NoBuildingOnTile);
        tile.PlaceBuilding(ResourceManager.CreateBuilding(Home, building), Home);
        Home.UpdatePlanetStatsByRecalculation();
    }

    static string TerraformerName => ResourceManager.GetBuildingTemplate(Building.TerraformerId).Name;

    void ApplyTerraforming()
        => typeof(Planet).GetMethod("ApplyTerraforming", BindingFlags.NonPublic | BindingFlags.Instance)!.Invoke(Home, new object[] { new SeededRandom(1) });

    // turns of terraforming until nothing terraforms here any more
    void TerraformToTheEnd()
    {
        for (int turn = 0; turn < 5000 && Home.TerraformingHere; ++turn)
            ApplyTerraforming();
        Assert.IsFalse(Home.TerraformingHere, "the job ends");
        ApplyTerraforming(); // and one more turn, which must change nothing
    }

    Notification[] Notifications() => Universe.NotificationManager.GetNotificationsAtomic();

    string LastNotification()
    {
        Notification[] all = Notifications();
        return all.Length > 0 ? all[all.Length - 1].Message : "";
    }

    [TestMethod]
    public void AFinishedJobScrapsTheTerraformersAndTheWorldSeed()
    {
        Place("WorldSeed");
        Place(TerraformerName);
        Assert.IsTrue(Home.TerraformingHere && Home.ContainsEventTerraformers, "setup: a World Seed and a terraformer stand on the colony");
        Assert.IsFalse(Home.Terraformable, "setup: nothing is left to terraform");

        ApplyTerraforming();

        Assert.IsFalse(Home.HasBuilding(b => b.IsTerraformer), "the terraformer is scrapped");
        Assert.IsFalse(Home.ContainsEventTerraformers, "and so is the World Seed");
        Assert.IsFalse(Home.TerraformingHere, "so the colony is no longer terraforming");
        StringAssert.Contains(LastNotification(), " 3:", "the message names the level the World Seed worked at");
    }

    [TestMethod]
    public void AnIdleWorldSeedIsScrappedWhenThereIsNothingToTerraform()
    {
        Place("WorldSeed");
        Assert.IsFalse(Home.Terraformable, "setup: nothing is left to terraform");

        ApplyTerraforming();

        Assert.IsFalse(Home.ContainsEventTerraformers, "the World Seed goes");
        Assert.IsFalse(Home.TerraformingHere, "and the colony is not terraforming");
    }

    [TestMethod]
    public void TheMessageNamesTheEmpiresLevelWithNoEventTerraformer()
    {
        Player.data.Traits.TerraformingLevel = 1;
        Place(TerraformerName);
        Assert.IsFalse(Home.ContainsEventTerraformers, "setup: only a terraformer");

        ApplyTerraforming();

        Assert.IsFalse(Home.HasBuilding(b => b.IsTerraformer), "the terraformer is scrapped");
        StringAssert.Contains(LastNotification(), " 1:", "the message names the empire's level");
    }

    [TestMethod]
    public void TheLastTileTerraformedWithAWorldSeedEndsTheJobWithOneMessage()
    {
        PlanetGridSquare dead = Home.TilesList.Find(t => !t.Habitable && t.NoBuildingOnTile);
        dead.Terraformable = true;
        Place("WorldSeed");
        Place(TerraformerName);
        Assert.IsTrue(Home.Terraformable && Home.HasTilesToTerraform, "setup: one tile is left to terraform");

        TerraformToTheEnd();

        Assert.IsTrue(dead.Habitable, "the tile is terraformed");
        Assert.IsFalse(Home.HasBuilding(b => b.IsTerraformer) || Home.ContainsEventTerraformers, "the terraformer and the World Seed are gone");
        AssertEqual(1, Notifications().Length, "the player is told once");
    }

    [TestMethod]
    public void TheLastVolcanoTerraformedWithAWorldSeedEndsTheJobWithOneMessage()
    {
        PlanetGridSquare volcano = Home.TilesList.Find(t => t.Habitable && t.NoBuildingOnTile);
        volcano.CreateVolcano(Home);
        volcano.Terraformable = false; // the volcano's tile is left as it is, so the volcano is the last job
        Place("WorldSeed");
        Place(TerraformerName);
        Assert.IsTrue(volcano.VolcanoHere && Home.HasTerrainToTerraform && !Home.HasTilesToTerraform, "setup: one volcano is left to terraform");
        int told = Notifications().Length;

        TerraformToTheEnd();

        Assert.IsFalse(volcano.VolcanoHere, "the volcano is gone");
        Assert.IsFalse(Home.HasBuilding(b => b.IsTerraformer) || Home.ContainsEventTerraformers, "the terraformer and the World Seed are gone");
        AssertEqual(told + 1, Notifications().Length, "the player is told once");
    }

    [TestMethod]
    public void AFertilityJobRunsToTheEndAndScrapsTheTerraformersAndTheWorldSeed()
    {
        float target = Home.TerraformedMaxFertility;
        Home.SetBaseFertility(target * 0.5f, target * 0.5f);
        Place("WorldSeed");
        Place(TerraformerName);
        Assert.IsTrue(Home.Terraformable && !Home.HasTilesToTerraform, "setup: only the planet's fertility is left to raise");

        TerraformToTheEnd();

        Assert.IsTrue(Home.BaseMaxFertility >= target - 0.001f, $"the fertility reaches the target ({Home.BaseMaxFertility} of {target})");
        Assert.IsFalse(Home.HasBuilding(b => b.IsTerraformer) || Home.ContainsEventTerraformers, "the terraformer and the World Seed are gone");
        AssertEqual(1, Notifications().Length, "the player is told once");
    }
}
