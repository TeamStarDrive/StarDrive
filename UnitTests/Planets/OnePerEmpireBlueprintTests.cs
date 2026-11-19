using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Ship_Game;
using Vector2 = SDGraphics.Vector2;
#pragma warning disable CA2213

namespace UnitTests.Planets;

[TestClass]
public class OnePerEmpireBlueprintTests : StarDriveTest
{
    readonly Planet Home;
    readonly Planet Other;
    readonly Building OnePerEmpire; // a template marked one-per-empire for the test, restored in Cleanup
    readonly Building Ordinary;

    public OnePerEmpireBlueprintTests()
    {
        CreateUniverseAndPlayerEmpire();
        Home = AddHomeWorldToEmpire(new Vector2(1000), Player);
        Other = AddHomeWorldToEmpire(new Vector2(400_000), Player);
        Building[] picks = ResourceManager.BuildingsDict.Values
            .Where(b => b.IsSuitableForBlueprints && !b.IsMilitary && b.EventOnBuild == null && !b.CanBuildAnywhere)
            .Take(2).ToArray();
        AssertEqual(2, picks.Length, "test needs two ordinary buildings");
        OnePerEmpire = picks[0];
        Ordinary = picks[1];
        Player.UnlockEmpireBuilding(OnePerEmpire.Name);
        Player.UnlockEmpireBuilding(Ordinary.Name);
        OnePerEmpire.BuildOnlyOnce = true; // last, so a failing constructor leaves the template as it was
    }

    [TestCleanup]
    public override void Cleanup()
    {
        OnePerEmpire.BuildOnlyOnce = false;
        base.Cleanup();
    }

    void Plan(Planet p, params string[] planned)
    {
        p.AddBlueprints(new BlueprintsTemplate("plan", false, null, new HashSet<string>(planned), Planet.ColonyType.Core), Player);
    }

    static void Build(Planet p, string name)
    {
        PlanetGridSquare tile = p.TilesList.Find(t => t.Habitable && t.NoBuildingOnTile);
        Assert.IsNotNull(tile, "test planet ran out of habitable tiles");
        tile.PlaceBuilding(ResourceManager.CreateBuilding(p, name), p);
    }

    [TestMethod]
    public void AOnePerEmpireBuildingCanBePlannedButIsNotOneAPlanScraps()
    {
        Assert.IsTrue(OnePerEmpire.CanBePlanned, "the blueprints editor offers it");
        Assert.IsFalse(OnePerEmpire.IsSuitableForBlueprints, "but a plan never scraps it to make room");
    }

    [TestMethod]
    public void APlanCountsItsOnePerEmpireBuildingOnceItStandsOnAnotherPlanet()
    {
        Plan(Home, OnePerEmpire.Name, Ordinary.Name);
        Build(Home, Ordinary.Name);
        AssertEqual(50, Home.Blueprints.PercentCompleted, "setup: half of the plan stands here");

        Build(Other, OnePerEmpire.Name);

        AssertEqual(100, Home.Blueprints.PercentCompleted, "the empire's one copy stands elsewhere, so this plan is complete");
        Assert.IsTrue(Home.Blueprints.IsAchievableCompleted, "and achieved");
    }

    [TestMethod]
    public void APlanStopsCountingItsOnePerEmpireBuildingWhenThePlanetHoldingItIsLost()
    {
        Plan(Home, OnePerEmpire.Name, Ordinary.Name);
        Build(Home, Ordinary.Name);
        Build(Other, OnePerEmpire.Name);
        AssertEqual(100, Home.Blueprints.PercentCompleted, "setup: the plan counts the copy on the other planet");

        Other.Construction.ClearQueue();
        Other.SetOwner(Enemy);

        AssertEqual(50, Home.Blueprints.PercentCompleted, "the empire no longer has it, so the plan is half done again");
    }

    [TestMethod]
    public void APlanCountsAOnePerEmpireBuildingOnAPlanetTheEmpireGains()
    {
        Planet taken = AddHomeWorldToEmpire(new Vector2(800_000), Enemy);
        Build(taken, OnePerEmpire.Name);
        Plan(Home, OnePerEmpire.Name, Ordinary.Name);
        Build(Home, Ordinary.Name);
        AssertEqual(50, Home.Blueprints.PercentCompleted, "setup: another empire's copy does not count");

        taken.Construction.ClearQueue();
        taken.SetOwner(Player);

        AssertEqual(100, Home.Blueprints.PercentCompleted, "the planet taken holds one, so the plan is complete");
    }

    [TestMethod]
    public void APlannedOnePerEmpireBuildingStandingNowhereIsNotCounted()
    {
        Plan(Home, OnePerEmpire.Name, Ordinary.Name);
        Build(Home, Ordinary.Name);
        Build(Other, Ordinary.Name);

        AssertEqual(50, Home.Blueprints.PercentCompleted, "other buildings elsewhere do not count");
        Assert.IsFalse(Home.Blueprints.IsAchievableCompleted, "the one-per-empire building is still to be built");
    }

    [TestMethod]
    public void APlannedOnePerEmpireBuildingQueuedElsewhereIsNotCountedYet()
    {
        Plan(Home, OnePerEmpire.Name, Ordinary.Name);
        Build(Home, Ordinary.Name);
        Assert.IsTrue(Other.Construction.Enqueue(ResourceManager.CreateBuilding(Other, OnePerEmpire.Name)), "setup: the other planet queues it");

        Home.Blueprints.UpdateCompletion();

        AssertEqual(50, Home.Blueprints.PercentCompleted, "it counts once it stands, when its completion updates every plan");
    }

    // both planets governed on the plan, with money for it
    void GovernBothOnThePlan()
    {
        foreach (Planet p in new[] { Home, Other })
        {
            Plan(p, OnePerEmpire.Name);
            p.SetManualCivBudget(1000);
        }
    }

    static int QueuedCopies(Planet p) => p.ConstructionQueue.Count(q => q.isBuilding && q.Building.BuildOnlyOnce);

    [TestMethod]
    public void OneGoverningPassQueuesAPlannedOnePerEmpireBuildingOnOnePlanetOnly()
    {
        GovernBothOnThePlan();

        Player.GovernPlanets();

        AssertEqual(1, QueuedCopies(Home), "setup: the first planet's governor queues it");
        AssertEqual(0, QueuedCopies(Other), "and the next planet in the same pass sees it is taken");
    }

    [TestMethod]
    public void AGoverningPassSeesAOnePerEmpireBuildingQueuedBeforeIt()
    {
        GovernBothOnThePlan();
        Assert.IsTrue(Other.Construction.Enqueue(ResourceManager.CreateBuilding(Other, OnePerEmpire.Name)), "setup: queued by hand on the other planet");

        Player.GovernPlanets();

        AssertEqual(0, QueuedCopies(Home), "a copy already queued elsewhere keeps it off this planet");
        AssertEqual(1, QueuedCopies(Other), "and the queued one stays");
    }

    [TestMethod]
    public void AGoverningPassSeesAOnePerEmpireBuildingStandingElsewhere()
    {
        GovernBothOnThePlan();
        Build(Other, OnePerEmpire.Name);

        Player.GovernPlanets();

        AssertEqual(0, QueuedCopies(Home) + QueuedCopies(Other), "the empire has its one already, so no planet queues another");
    }

    [TestMethod]
    public void TheGovernorBuildsAPlannedOnePerEmpireBuilding()
    {
        Plan(Home, OnePerEmpire.Name);
        object[] args = { new[] { OnePerEmpire }, 1000f, false, null };

        typeof(Planet).GetMethod("ChooseBestBuilding", BindingFlags.NonPublic | BindingFlags.Instance)!.Invoke(Home, args);

        Assert.AreSame(OnePerEmpire, args[3], "a player's governor builds a one-per-empire building its plan names");
    }
}
