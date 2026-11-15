using System.Reflection;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Ship_Game;
using Ship_Game.AI;
using Ship_Game.AI.Tasks;
using Ship_Game.Commands.Goals;
using Ship_Game.Fleets;
using Ship_Game.Ships;
using Vector2 = SDGraphics.Vector2;

namespace UnitTests.Fleets;

/// <summary>
/// An AI fleet claiming a planet for colonization that forms inside the planet's system goes straight to the planet
/// instead of gathering at the system's edge, and the colonization goal gives up on its claim fleet only when the fleet
/// is neither moving in nor in the system.
/// </summary>
[TestClass]
public class ClaimFleetTests : StarDriveTest
{
    const string Warship = "Terran-Prototype";
    const string Constructor = "Terran Constructor";
    const string ColonyShip = "Colony Ship";
    readonly Planet Target;

    public ClaimFleetTests()
    {
        LoadStarterShips(Warship, Constructor, ColonyShip);
        CreateUniverseAndPlayerEmpire();
        Enemy.AddBuildableShip(ResourceManager.Ships.GetDesign(Constructor)); // a claim fleet sends a constructor for its projector
        UState.Objects.EnableParallelUpdate = false;
        Target = AddDummyPlanet(new Vector2(400_000));
    }

    Vector2 InSystem => Target.Position + new Vector2(20_000f, 0f);
    Vector2 OutsideSystem => Target.System.Position + new Vector2(Target.System.Radius * 3f, 0f);

    // the AI's claim fleet for the planet, at the given step of its task
    Fleet ClaimFleet(Vector2 at, int taskStep, out MilitaryTask task)
    {
        task = MilitaryTask.CreateClaimTask(Enemy, Target, 100f, Player, fleetCount: 1);
        var fleet = new Fleet(UState, Enemy) { FleetTask = task, TaskStep = taskStep };
        typeof(MilitaryTask).GetProperty(nameof(MilitaryTask.Fleet))!.SetValue(task, fleet);
        for (int i = 0; i < 4; ++i)
            fleet.AddShip(SpawnShip(Warship, Enemy, at + new Vector2(0f, 1000f * i)));
        Assert.AreEqual(at.InRadius(Target.System.Position, Target.System.Radius), fleet.IsInSystem(Target.System),
                        "setup: the fleet is where it was put");
        return fleet;
    }

    static void ClaimDefense(Fleet fleet, MilitaryTask task)
        => typeof(Fleet).GetMethod("DoClaimDefense", BindingFlags.Instance | BindingFlags.NonPublic)!.Invoke(fleet, new object[] { task });

    [TestMethod]
    public void AClaimFleetFormedInsideTheSystemGoesStraightToThePlanet()
    {
        Fleet fleet = ClaimFleet(InSystem, taskStep: 0, out MilitaryTask task);

        ClaimDefense(fleet, task);
        ClaimDefense(fleet, task);

        Assert.AreEqual(5, fleet.TaskStep, "the fleet skips gathering at the system's edge");
        AssertLessThan(fleet.FinalPosition.Distance(Target.Position), Target.GravityWellRadius * 1.5f + 1f, "and moves to the planet");
    }

    [TestMethod]
    public void AClaimFleetOutsideTheSystemGathersBeforeItGoesIn()
    {
        Fleet fleet = ClaimFleet(OutsideSystem, taskStep: 0, out MilitaryTask task);

        ClaimDefense(fleet, task);
        ClaimDefense(fleet, task);

        AssertLessThan(fleet.TaskStep, 4, "a fleet coming from outside still gathers before it goes in");
    }

    [TestMethod]
    [DataRow(5, true,  false)]
    [DataRow(5, false, false)]
    [DataRow(2, true,  false)]
    [DataRow(2, false, true)]
    public void AColonizationGoalGivesUpOnItsClaimFleetOnlyWhenItIsNeitherMovingInNorThere(int taskStep, bool inSystem, bool givesUp)
    {
        Ship enemy = SpawnShip(Warship, Player, Target.Position + new Vector2(10_000f, 0f));
        enemy.SetSystem(Target.System);
        Enemy.Threats.SetSeen(enemy, fromBackgroundThread: false);
        Enemy.Threats.Update(new FixedSimTime(2f));
        AssertGreaterThan(Enemy.KnownEnemyStrengthIn(Target.System), 10f, "setup: the AI knows of enemies in the system");

        ClaimFleet(inSystem ? InSystem : OutsideSystem, taskStep, out MilitaryTask task);
        var goal = new MarkForColonization(Enemy) { TargetPlanet = Target, Task = task };
        Ship colonyShip = SpawnShip(ColonyShip, Enemy, Target.Position + new Vector2(5000f, 0f));
        goal.FinishedShip = colonyShip;
        colonyShip.AI.OrderColonization(Target, goal);
        for (int i = 0; i < 5; ++i)
            goal.AdvanceToNextStep();
        Assert.AreEqual("WaitForColonizationComplete", goal.StepName, "setup: the colony ship is on its way to settle");

        GoalStep result = goal.Evaluate();

        Assert.AreEqual(givesUp, result == GoalStep.GoalFailed,
                        givesUp ? "a claim fleet neither sent in nor there is no cover, and the goal gives up"
                                : "a claim fleet moving in or already in the system still covers the colony ship");
    }
}
