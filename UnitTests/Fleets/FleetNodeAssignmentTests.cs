using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Ship_Game;
using Ship_Game.AI;
using Ship_Game.Commands.Goals;
using Ship_Game.Fleets;
using Ship_Game.Ships;
using Vector2 = SDGraphics.Vector2;

namespace UnitTests.Fleets;

[TestClass]
public class FleetNodeAssignmentTests : StarDriveTest
{
    const string Design = "Terran-Prototype";
    static readonly Vector2 FleetPos = new(200_000f, 200_000f);

    public FleetNodeAssignmentTests()
    {
        LoadStarterShips(Design);
        CreateUniverseAndPlayerEmpire();
        UState.Events.Disabled = true;
    }

    Fleet EmptyFleet()
    {
        Fleet fleet = Player.CreateFleet(1, null);
        fleet.FinalPosition = FleetPos;
        return fleet;
    }

    static FleetDataNode AddNode(Fleet fleet, Vector2 offset, Goal goal = null)
    {
        var node = new FleetDataNode { ShipName = Design, RelativeFleetOffset = offset, Goal = goal };
        fleet.DataNodes.Add(node);
        return node;
    }

    Ship IdleShip() => SpawnShip(Design, Player, FleetPos + new Vector2(20_000f, 0f));

    [TestMethod]
    public void AssignNowFillsTheNewNodeAndLeavesNodesUnderConstructionToTheirBuilds()
    {
        Fleet fleet = EmptyFleet();
        var build1 = new FleetRequisition(Design, Player, fleet, false);
        var build2 = new FleetRequisition(Design, Player, fleet, false);
        FleetDataNode building1 = AddNode(fleet, new Vector2(-500f, 0f), build1);
        FleetDataNode building2 = AddNode(fleet, new Vector2(500f, 0f), build2);
        FleetDataNode added = AddNode(fleet, new Vector2(0f, -1500f));
        Ship ship = IdleShip();

        AssertEqual(1, fleet.CountShipsThatFit(new[] { ship, IdleShip() }), "two idle ships, one node free of builds");
        AssertEqual(1, fleet.AssignShipsToEmptyNodes(new[] { ship }));

        Assert.AreSame(ship, added.Ship, "the idle ship fills the node that is not being built");
        Assert.IsNull(building1.Ship, "a node under construction waits for its own ship");
        Assert.IsNull(building2.Ship, "a node under construction waits for its own ship");
        Assert.AreSame(build1, building1.Goal, "its build goes on");
        Assert.AreSame(build2, building2.Goal, "its build goes on");
    }

    [TestMethod]
    public void AssignNowGivesEachShipOneNodeAndSendsItThere()
    {
        Fleet fleet = EmptyFleet();
        FleetDataNode first = AddNode(fleet, new Vector2(0f, -1500f));
        FleetDataNode second = AddNode(fleet, new Vector2(0f, 1500f));
        Ship ship = IdleShip();

        AssertEqual(1, fleet.AssignShipsToEmptyNodes(new[] { ship }));

        Assert.AreSame(ship, first.Ship, "the ship takes one node");
        Assert.IsNull(second.Ship, "and only one");
        Assert.IsTrue(ship.AI.OrderQueue.TryPeekFirst(out ShipAI.ShipGoal move), "the assigned ship has an order");
        AssertEqual(1f, fleet.GetFinalPos(ship), move.MovePosition, "it flies to its slot");
    }

    [TestMethod]
    public void AssignNowLeavesShipsOnTheirWayToARefitOrTheScrapyardAlone()
    {
        Fleet fleet = EmptyFleet();
        AddNode(fleet, new Vector2(0f, -1500f));
        Ship refitting = IdleShip();
        refitting.AI.State = AIState.Refit;
        Ship scrapping = IdleShip();
        scrapping.AI.State = AIState.Scrap;

        AssertEqual(0, fleet.CountShipsThatFit(new[] { refitting, scrapping }), "ships on their way to a refit or the scrapyard do not fit");
        AssertEqual(0, fleet.AssignShipsToEmptyNodes(new[] { refitting, scrapping }));
        AssertEqual(1, fleet.AssignShipsToEmptyNodes(new[] { IdleShip() }), "control: an idle ship of the same design does");
    }

    [TestMethod]
    public void AFinishedBuildNeverLandsOnANodeThatAlreadyHasAShip()
    {
        Fleet fleet = EmptyFleet();
        var build = new FleetRequisition(Design, Player, fleet, false);
        FleetDataNode taken = AddNode(fleet, new Vector2(0f, -1500f));
        FleetDataNode free = AddNode(fleet, new Vector2(0f, 1500f));
        Ship already = IdleShip();
        fleet.AddExistingShip(already, taken);
        taken.Goal = build;

        Ship built = IdleShip();
        build.ReportShipComplete(built);
        build.AdvanceToNextStep();
        build.Evaluate();

        Assert.AreSame(already, taken.Ship, "the node keeps the ship it had");
        Assert.AreSame(built, free.Ship, "the new ship takes a free node of its design");
        Assert.IsNull(taken.Goal, "and the build is done");
    }

    [TestMethod]
    public void AFinishedShipAssignedElsewhereMeanwhileIsNotAddedTwice()
    {
        Fleet fleet = EmptyFleet();
        var build = new FleetRequisition(Design, Player, fleet, false);
        FleetDataNode building = AddNode(fleet, new Vector2(0f, -1500f), build);
        FleetDataNode free = AddNode(fleet, new Vector2(0f, 1500f));
        Ship built = IdleShip();
        build.ReportShipComplete(built);
        AssertEqual(1, fleet.AssignShipsToEmptyNodes(new[] { built }), "setup: Assign Now took the new ship before its build goal ran");

        build.AdvanceToNextStep();
        build.Evaluate();

        Assert.AreSame(built, free.Ship, "the ship stays in the node it was assigned to");
        Assert.IsNull(building.Ship, "and is not put in its build's node as well");
        Assert.IsNull(building.Goal, "which is free again");
    }

    [TestMethod]
    public void AFleetBuildLostWithItsShipyardFreesItsNode()
    {
        Planet shipyard = AddHomeWorldToEmpire(new Vector2(400_000), Player, new Vector2(405_000), explored: true);
        Player.UpdateRallyPoints();
        Fleet fleet = EmptyFleet();
        var build = new FleetRequisition(Design, Player, fleet, false);
        FleetDataNode node = AddNode(fleet, new Vector2(0f, -1500f), build);
        Player.AI.AddGoalAndEvaluate(build);
        Assert.IsTrue(shipyard.ConstructionQueue.Any(q => q.Goal == build), "setup: the ship is being built");

        build.Evaluate();
        Assert.AreSame(build, node.Goal, "a build still in its shipyard's queue keeps its node");

        shipyard.Construction.ClearQueue();
        build.Evaluate();
        Assert.IsNull(node.Goal, "a build whose queue item is gone gives its node up");
        AssertEqual(1, fleet.AssignShipsToEmptyNodes(new[] { IdleShip() }), "so Assign Now can fill it");
    }
}
