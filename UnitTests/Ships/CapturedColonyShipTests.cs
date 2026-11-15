using Microsoft.VisualStudio.TestTools.UnitTesting;
using Ship_Game;
using Ship_Game.AI;
using Ship_Game.Commands.Goals;
using Vector2 = SDGraphics.Vector2;

namespace UnitTests.Ships;

/// <summary>
/// A colony ship captured on its way is lost to the goal that sent it, as a destroyed one is: the old owner's goal no
/// longer orders it to colonize, to orbit its rally point or to a new target.
/// </summary>
[TestClass]
public class CapturedColonyShipTests : StarDriveTest
{
    readonly Planet Target;
    readonly Planet Other;

    public CapturedColonyShipTests()
    {
        CreateUniverseAndPlayerEmpire();
        UState.Objects.EnableParallelUpdate = false;
        Target = AddDummyPlanet(new Vector2(200_000), 1f, 1f, 4f, new Vector2(205_000), explored: true);
        Other = AddDummyPlanet(new Vector2(300_000), 1f, 1f, 4f, new Vector2(305_000), explored: true);
    }

    // a colony ship on its way to colonize for its empire, captured by another that orders it away
    TestShip CapturedOnItsWay(Ship_Game.Empire owner, Ship_Game.Empire captor, out MarkForColonization goal)
    {
        TestShip ship = SpawnShip("Colony Ship", owner, Target.Position + new Vector2(60_000, 0));
        goal = new MarkForColonization(ship, Target, owner);
        Assert.IsTrue(ship.AI.FindGoal(ShipAI.Plan.Colonize, out _), "setup: the ship is on its way to colonize");

        ship.LoyaltyChangeFromBoarding(captor, addNotification: false);
        RunObjectsSim(TestSimStep);
        Assert.AreSame(captor, ship.Loyalty, "setup: the ship was captured");
        ship.AI.OrderMoveTo(ship.Position + new Vector2(0, 50_000), new Vector2(0, 1));
        return ship;
    }

    static bool MovesOn(TestShip ship) => ship.AI.State == AIState.MoveTo && !ship.AI.FindGoal(ShipAI.Plan.Colonize, out _);

    [TestMethod]
    public void TheOldOwnersGoalNoLongerOrdersACapturedColonyShipToColonize()
    {
        TestShip ship = CapturedOnItsWay(Player, Enemy, out MarkForColonization goal);

        for (int i = 0; i < 8; ++i)
            goal.Evaluate();

        Assert.IsTrue(MovesOn(ship), "the captured ship keeps the move its new owner gave it");
        Assert.IsNull(goal.FinishedShip, "the goal lets it go, as it would a destroyed ship");
    }

    [TestMethod]
    public void AFailedGoalDoesNotSendACapturedColonyShipToItsOldOwnersRallyPoint()
    {
        AddHomeWorldToEmpire(new Vector2(-200_000), Enemy);
        TestShip ship = CapturedOnItsWay(Enemy, Player, out MarkForColonization goal);
        Target.SetOwner(Player);

        goal.Evaluate();

        Assert.IsTrue(MovesOn(ship), "the captured ship is not sent to orbit its old owner's rally point");
    }

    [TestMethod]
    public void AGoalThePlannerSwitchesToAnotherPlanetDoesNotOrderItsCapturedColonyShip()
    {
        TestShip ship = SpawnShip("Colony Ship", Player, Target.Position + new Vector2(0, 600_000));
        RunObjectsSim(TestSimStep);
        var goal = new MarkForColonization(Target, Player);
        Assert.AreSame(ship, goal.FinishedShip, "setup: the goal takes the idle colony ship");
        goal.Evaluate();
        Assert.IsTrue(ship.AI.FindGoal(ShipAI.Plan.Colonize, out _), "setup: and sends it to colonize");

        ship.LoyaltyChangeFromBoarding(Enemy, addNotification: false);
        RunObjectsSim(TestSimStep);
        Assert.AreSame(Enemy, ship.Loyalty, "setup: the ship was captured");
        ship.AI.OrderMoveTo(ship.Position + new Vector2(0, 50_000), new Vector2(0, 1));
        Assert.IsTrue(goal.CanSwitchTarget, "setup: the expansion planner may move this goal to another planet");

        goal.SwitchTargetPlanet(Other);

        Assert.IsTrue(MovesOn(ship), "the captured ship is not sent to colonize the new target");
        Assert.IsNull(goal.FinishedShip, "the goal lets it go");
    }
}
