using Microsoft.VisualStudio.TestTools.UnitTesting;
using Ship_Game;
using Ship_Game.AI;
using Ship_Game.Commands.Goals;
using Ship_Game.Ships;
using Vector2 = SDGraphics.Vector2;
#pragma warning disable CA2213

namespace UnitTests.Ships;

/// <summary>
/// An orbital refit chooses its port on one turn and orders the build there on the next. A port lost in between,
/// say wiped out by the Remnants, has no owner to price the build for, which threw every turn and kept the
/// empire's turn from ever finishing. The refit now takes another port, or gives up when there is none.
/// An orbital takes one refit at a time, so a refit that gives up never ends another one's wait.
/// </summary>
[TestClass]
public class RefitOrbitalLostPortTests : StarDriveTest
{
    const string Platform = "Platform Base mk1-a";
    const string Constructor = "Terran Constructor";

    public RefitOrbitalLostPortTests()
    {
        LoadStarterShips(Platform, Constructor);
        CreateUniverseAndPlayerEmpire();
        Universe.NotificationManager = new NotificationManager(Universe.ScreenManager, Universe);
        Enemy.AddBuildableShip(ResourceManager.Ships.GetDesign(Constructor));
    }

    Planet Port(Vector2 position)
    {
        Planet port = AddHomeWorldToEmpire(position, Enemy);
        port.HasSpacePort = true;
        return port;
    }

    // an orbital refit that chose its port this turn and orders the build on the next
    (RefitOrbital refit, Ship platform) RefitThatChoseItsPort()
    {
        Enemy.UpdateRallyPoints();
        Ship platform = SpawnShip(Platform, Enemy, new Vector2(100_000));
        var refit = new RefitOrbital(platform, ResourceManager.Ships.GetDesign(Platform), Enemy);
        refit.Evaluate();
        Assert.AreEqual("BuildNewShip", refit.StepName, "setup: the refit chose its port");
        Assert.IsNotNull(refit.PlanetBuildingAt, "setup: and holds it");
        return (refit, platform);
    }

    static bool Queues(Planet port, Goal goal)
    {
        foreach (QueueItem item in port.ConstructionQueue)
            if (item.Goal == goal)
                return true;
        return false;
    }

    [TestMethod]
    public void ARefitWhosePortWasLostBuildsAtAnotherPort()
    {
        Planet first = Port(new Vector2(-200_000));
        Planet second = Port(new Vector2(400_000));
        (RefitOrbital refit, _) = RefitThatChoseItsPort();
        Planet lost = refit.PlanetBuildingAt;
        Planet other = lost == first ? second : first;

        lost.WipeOutColony(Enemy);
        Assert.IsNull(lost.Owner, "setup: the chosen port was wiped out");
        refit.Evaluate();

        Assert.AreSame(other, refit.PlanetBuildingAt, "the refit takes the port its empire still has");
        Assert.IsTrue(Queues(other, refit), "and orders its build there");
        Assert.AreEqual("WaitForRefitBuilt", refit.StepName, "then waits for the build as usual");
    }

    [TestMethod]
    public void AnOrbitalWhoseQueuedRefitWasWipedOutStopsWaitingForIt()
    {
        Port(new Vector2(-200_000));
        (RefitOrbital refit, Ship platform) = RefitThatChoseItsPort();
        Planet port = refit.PlanetBuildingAt;
        refit.Evaluate();
        Assert.IsTrue(Queues(port, refit), "setup: the refit's build is queued at its port");
        AssertEqual(AIState.Refit, platform.AI.State, "setup: the orbital waits for its refit");

        port.WipeOutColony(Enemy);
        GoalStep result = refit.Evaluate();

        AssertEqual(GoalStep.GoalFailed, result, "the build gone with its port, the refit is called off");
        Assert.AreNotEqual(AIState.Refit, platform.AI.State, "and the orbital no longer waits for it");
    }

    [TestMethod]
    public void AnOrbitalAlreadyRefittingTakesNoSecondRefit()
    {
        Port(new Vector2(-200_000));
        Enemy.UpdateRallyPoints();
        Ship platform = SpawnShip(Platform, Enemy, new Vector2(100_000));
        var first = new RefitOrbital(platform, ResourceManager.Ships.GetDesign(Platform), Enemy);
        Enemy.AI.AddGoalAndEvaluate(first);
        Assert.AreEqual("BuildNewShip", first.StepName, "setup: the first refit chose its port");

        var second = new RefitOrbital(platform, ResourceManager.Ships.GetDesign(Platform), Enemy);
        Enemy.AI.AddGoalAndEvaluate(second);

        Assert.IsFalse(Enemy.AI.HasGoal(second), "the second refit is turned down");
        Assert.IsTrue(Enemy.AI.HasGoal(first), "the first one goes on");
        AssertEqual(AIState.Refit, platform.AI.State, "and the orbital still waits for it");
    }

    [TestMethod]
    public void ACancelledRefitMakesWayForANewOneAtOnce()
    {
        Planet port = Port(new Vector2(-200_000));
        Enemy.UpdateRallyPoints();
        Ship platform = SpawnShip(Platform, Enemy, new Vector2(100_000));
        var first = new RefitOrbital(platform, ResourceManager.Ships.GetDesign(Platform), Enemy);
        Enemy.AI.AddGoalAndEvaluate(first);
        first.Evaluate();
        QueueItem queued = null;
        foreach (QueueItem item in port.ConstructionQueue)
            if (item.Goal == first)
                queued = item;
        Assert.IsNotNull(queued, "setup: the first refit's build is queued");

        port.Construction.Cancel(queued);
        var second = new RefitOrbital(platform, ResourceManager.Ships.GetDesign(Platform), Enemy);
        Enemy.AI.AddGoalAndEvaluate(second);

        Assert.IsFalse(Enemy.AI.HasGoal(first), "the cancelled refit is gone");
        Assert.IsTrue(Enemy.AI.HasGoal(second), "and a new refit of the orbital goes ahead");
    }

    [TestMethod]
    public void ARefitWhoseOnlyPortWasLostGivesUp()
    {
        Port(new Vector2(-200_000));
        (RefitOrbital refit, Ship platform) = RefitThatChoseItsPort();
        AssertEqual(AIState.Refit, platform.AI.State, "setup: the orbital waits for its refit");

        refit.PlanetBuildingAt.WipeOutColony(Enemy);
        GoalStep result = refit.Evaluate();

        AssertEqual(GoalStep.GoalFailed, result, "with no port left the refit is called off");
        Assert.AreNotEqual(AIState.Refit, platform.AI.State, "and the orbital no longer waits for it");
    }
}
