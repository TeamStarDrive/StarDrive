using Microsoft.VisualStudio.TestTools.UnitTesting;
using Ship_Game;
using Ship_Game.AI;
using Ship_Game.Commands.Goals;
using Ship_Game.Fleets;
using Vector2 = SDGraphics.Vector2;

namespace UnitTests.Ships;

/// <summary>
/// A ship captured while a goal of its old owner holds it is lost to that goal, as a destroyed one is: the goal
/// gives it no more orders, does not scuttle it and does not take it into its old owner's fleet.
/// </summary>
[TestClass]
public class CapturedShipGoalTests : StarDriveTest
{
    const string Platform = "Platform Base mk1-a";
    const string Constructor = "Terran Constructor";
    const string Warship = "Vulcan Scout";

    public CapturedShipGoalTests()
    {
        LoadStarterShips(Platform, Constructor, Warship);
        CreateUniverseAndPlayerEmpire();
        UState.Objects.EnableParallelUpdate = false;
    }

    // the player captures the ship and orders it away
    void CapturedByPlayer(TestShip ship)
    {
        ship.LoyaltyChangeFromBoarding(Player, addNotification: false);
        RunObjectsSim(TestSimStep);
        Assert.AreSame(Player, ship.Loyalty, "setup: the ship was captured");
        ship.AI.OrderMoveTo(ship.Position + new Vector2(0, 50_000), new Vector2(0, 1));
    }

    static bool MovesOn(TestShip ship) => ship.AI.State == AIState.MoveTo;

    [TestMethod]
    public void AStandbyColonyShipCapturedIsReplacedNotKeptForever()
    {
        TestShip ship = SpawnShip("Colony Ship", Enemy, new Vector2(100_000));
        var goal = new StandbyColonyShip(Enemy);
        goal.ReportShipComplete(ship);
        goal.Evaluate();
        Assert.AreEqual("KeepOnStandBy", goal.StepName, "setup: the colony ship is on standby");

        CapturedByPlayer(ship);
        for (int i = 0; i < 8; ++i)
            goal.Evaluate();

        Assert.IsNull(goal.FinishedShip, "the goal lets the captured ship go");
        Assert.AreNotEqual("KeepOnStandBy", goal.StepName, "and goes back to getting a standby ship of its own");
        Assert.IsTrue(MovesOn(ship), "the captured ship keeps its new owner's orders");
    }

    [TestMethod]
    public void AScoutCapturedIsNotSentScoutingByItsOldOwner()
    {
        TestShip scout = SpawnShip(Warship, Enemy, new Vector2(100_000));
        var goal = new ScoutSystem(Enemy);
        goal.ReportShipComplete(scout);

        CapturedByPlayer(scout);
        for (int i = 0; i < 8; ++i)
            goal.Evaluate();

        Assert.IsNull(goal.FinishedShip, "the goal lets the captured scout go");
        Assert.IsTrue(MovesOn(scout), "the captured scout keeps its new owner's orders");
    }

    [TestMethod]
    public void AConstructorCapturedIsNotScuttledWhenItsOldOwnerLosesTheTetherPlanet()
    {
        Planet home = AddHomeWorldToEmpire(new Vector2(-200_000), Enemy);
        Enemy.AddBuildableShip(ResourceManager.Ships.GetDesign(Constructor));
        var goal = new BuildOrbital(home, Platform, Enemy);
        TestShip constructor = SpawnShip(Constructor, Enemy, home.Position + new Vector2(20_000, 0));
        goal.ReportShipComplete(constructor);

        CapturedByPlayer(constructor);
        home.SetOwner(Player);
        goal.Evaluate();

        AssertLessThan(constructor.ScuttleTimer, 0f, "the captured constructor is not ordered to scuttle");
        Assert.IsTrue(MovesOn(constructor), "and keeps its new owner's orders");
    }

    [TestMethod]
    public void ARefittedShipCapturedDoesNotJoinItsOldOwnersFleet()
    {
        TestShip member = SpawnShip(Warship, Enemy, new Vector2(100_000));
        Fleet fleet = Enemy.CreateFleet(1, null);
        fleet.AddShip(member);
        var goal = new RefitShip(Enemy) { Fleet = fleet };
        fleet.AssignGoal(fleet.DataNodes[0], goal);
        for (int i = 0; i < 3; ++i)
            goal.AdvanceToNextStep();
        TestShip refitted = SpawnShip(Warship, Enemy, new Vector2(110_000));
        goal.ReportShipComplete(refitted);
        Assert.AreEqual("AddShipDataAndFleet", goal.StepName, "setup: the refitted ship is about to join its fleet");

        CapturedByPlayer(refitted);
        goal.Evaluate();

        Assert.IsNull(refitted.Fleet, "the captured ship does not join its old owner's fleet");
        Assert.IsTrue(MovesOn(refitted), "and keeps its new owner's orders");
    }

    [TestMethod]
    public void AFleetShipCapturedBeforeJoiningDoesNotJoinItsOldOwnersFleet()
    {
        Fleet fleet = Enemy.CreateFleet(1, null);
        var build = new FleetRequisition(Warship, Enemy, fleet, false);
        var node = new FleetDataNode { ShipName = Warship, Goal = build };
        fleet.DataNodes.Add(node);
        TestShip built = SpawnShip(Warship, Enemy, new Vector2(100_000));
        build.ReportShipComplete(built);
        build.AdvanceToNextStep();
        Assert.AreEqual("AddShipToFleetAndMoveToPosition", build.StepName, "setup: the new ship is about to join its fleet");

        CapturedByPlayer(built);
        build.Evaluate();

        Assert.IsNull(built.Fleet, "the captured ship does not join its old owner's fleet");
        Assert.IsNull(node.Goal, "its slot is free for another build");
        Assert.IsTrue(MovesOn(built), "and the ship keeps its new owner's orders");
    }

    [TestMethod]
    public void AnOrbitalCapturedIsNotReplacedByItsOldOwnersRefit()
    {
        AddHomeWorldToEmpire(new Vector2(-200_000), Enemy);
        TestShip platform = SpawnShip(Platform, Enemy, new Vector2(100_000));
        var refit = new RefitOrbital(platform, ResourceManager.Ships.GetDesign(Platform), Enemy);
        for (int i = 0; i < 4; ++i)
            refit.AdvanceToNextStep();
        TestShip constructor = SpawnShip(Constructor, Enemy, platform.Position);
        refit.FinishedShip = constructor;
        constructor.AI.OrderDeepSpaceBuild(refit, 1f, platform.Radius);
        Assert.AreEqual("WaitForDeployment", refit.StepName, "setup: the constructor is at the orbital to build its refit");

        platform.LoyaltyChangeFromBoarding(Player, addNotification: false);
        RunObjectsSim(TestSimStep);
        Assert.AreSame(Player, platform.Loyalty, "setup: the orbital was captured");
        GoalStep result = GoalStep.TryAgain;
        for (int i = 0; i < 5 && result == GoalStep.TryAgain; ++i)
        {
            result = refit.Evaluate();
            RunObjectsSim(TestSimStep);
        }
        RunObjectsSim(1f);

        Assert.AreEqual(GoalStep.GoalFailed, result, "the old owner's refit is called off");
        Assert.IsTrue(platform.Active, "the captured orbital is not torn down for its old owner's refit");
        AssertEqual(AIState.Scrap, constructor.AI.State, "and the old owner's constructor scraps itself");
    }

    // a refit whose construction its goal finished on its last turn, the constructor about to deploy
    (TestShip platform, TestShip constructor) RefitAboutToDeploy()
    {
        AddHomeWorldToEmpire(new Vector2(-200_000), Enemy);
        TestShip platform = SpawnShip(Platform, Enemy, new Vector2(100_000));
        var refit = new RefitOrbital(platform, ResourceManager.Ships.GetDesign(Platform), Enemy);
        TestShip constructor = SpawnShip(Constructor, Enemy, platform.Position);
        refit.FinishedShip = constructor;
        constructor.AI.OrderDeepSpaceBuild(refit, 1f, platform.Radius);
        constructor.Construction.TryConstruct(refit.BuildPosition);
        Assert.IsTrue(constructor.Construction.ConsturctionCompleted, "setup: the refit's last turn finished the construction");
        return (platform, constructor);
    }

    [TestMethod]
    public void ARefitConstructorDoesNotDeployOverAnOrbitalCapturedBeforeItsGoalRunsAgain()
    {
        (TestShip platform, TestShip constructor) = RefitAboutToDeploy();

        platform.LoyaltyChangeFromBoarding(Player, addNotification: false);
        RunObjectsSim(1f);

        Assert.IsTrue(platform.Active, "the constructor does not tear down the captured orbital");
        Assert.AreSame(Player, platform.Loyalty, "which its new owner keeps");
        AssertEqual(AIState.Scrap, constructor.AI.State, "it scraps itself instead");
    }

    [TestMethod]
    public void ARefitConstructorDoesNotDeployOverAnOrbitalBeingCapturedThisFrame()
    {
        (TestShip platform, TestShip constructor) = RefitAboutToDeploy();

        platform.LoyaltyChangeFromBoarding(Player, addNotification: false);
        constructor.AI.Update(TestSimStep);
        RunObjectsSim(1f);

        Assert.IsTrue(platform.Active, "the constructor does not tear down an orbital changing hands");
        Assert.AreSame(Player, platform.Loyalty, "which its new owner keeps");
        AssertEqual(AIState.Scrap, constructor.AI.State, "it scraps itself instead");
    }

    [TestMethod]
    public void AProjectorConstructorCapturedIsNotScrappedByItsOldOwnersBridge()
    {
        Planet home = AddHomeWorldToEmpire(new Vector2(-200_000), Enemy);
        var bridge = new ProjectorBridge(home.System, home.Position + new Vector2(50_000, 0), Enemy, ProjectorBridgeEndCondition.NoHostiles);
        bridge.AdvanceToNextStep();
        bridge.AdvanceToNextStep();
        TestShip constructor = SpawnShip(Constructor, Enemy, home.Position + new Vector2(20_000, 0));
        bridge.FinishedShip = constructor;
        Assert.AreEqual("RemoveProjectorWhenSafe", bridge.StepName, "setup: the bridge is done with its constructor");

        CapturedByPlayer(constructor);
        bridge.Evaluate();

        AssertLessThan(constructor.ScuttleTimer, 0f, "the captured constructor is not set to self-destruct");
        Assert.IsTrue(MovesOn(constructor), "and keeps its new owner's orders");
    }

    [TestMethod]
    public void AFleetProjectorCapturedIsNotScuttledByItsOldOwnersFleetGoal()
    {
        Planet home = AddHomeWorldToEmpire(new Vector2(-200_000), Enemy);
        Fleet fleet = Enemy.CreateFleet(1, null);
        var goal = new DeployFleetProjector(fleet, home, Enemy);
        for (int i = 0; i < 3; ++i)
            goal.AdvanceToNextStep();
        TestShip projector = SpawnShip("Subspace Projector", Enemy, home.Position + new Vector2(20_000, 0));
        goal.FinishedShip = projector;
        Assert.AreEqual("RemoveProjectorWhenCompleted", goal.StepName, "setup: the fleet's projector is up and its task is over");

        projector.LoyaltyChangeFromBoarding(Player, addNotification: false);
        RunObjectsSim(TestSimStep);
        Assert.AreSame(Player, projector.Loyalty, "setup: the projector was captured");
        goal.Evaluate();

        AssertLessThan(projector.ScuttleTimer, 0f, "the captured projector is not set to self-destruct");
    }
}
