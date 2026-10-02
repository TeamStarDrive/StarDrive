using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using SDGraphics;
using Ship_Game;
using Ship_Game.AI;
using Ship_Game.Commands.Goals;
using Ship_Game.Fleets;
using Ship_Game.GameScreens.LoadGame;
using Ship_Game.Ships;
using SDUtils;
using Vector2 = SDGraphics.Vector2;
#pragma warning disable CA2213

namespace UnitTests.Ships;

/// <summary>
/// A ship sent to be refitted lands like a ship sent to be scrapped: on the nearest of the empire's
/// shipyards at its port, or on the port itself. The refit is queued once it has landed, and the new
/// ship takes off from the same shipyard or planet. If the refit is called off, the old ship takes off again.
/// </summary>
[TestClass]
public class RefitLandingTests : StarDriveTest
{
    readonly Planet Homeworld;
    readonly IShipDesign RefitTo;
    TestShip Refitted;
    Goal Refit;

    public RefitLandingTests()
    {
        LoadStarterShips("Shipyard", "Rocket Scout");
        CreateUniverseAndPlayerEmpire();
        UState.Objects.EnableParallelUpdate = false;
        Homeworld = AddHomeWorldToEmpire(new Vector2(200_000), Player, new Vector2(205_000), explored: true);
        Player.UpdateRallyPoints();
        RefitTo = ResourceManager.Ships.GetDesign("Rocket Scout");
    }

    float PlanetRange => Homeworld.Radius + 300f;
    float ShipyardRange => LandShip.ShipyardLandingRange(Refitted);

    Ship AddShipyard(Vector2 offset)
    {
        Ship shipyard = SpawnShip("Shipyard", Player, Homeworld.Position + offset);
        shipyard.TetherToPlanet(Homeworld);
        return shipyard;
    }

    void OrderRefit(Vector2 from) => OrderRefit(SpawnShip("Vulcan Scout", Player, from));

    void OrderRefit(TestShip ship)
    {
        Refitted = ship;
        Refit = new RefitShip(ship, RefitTo, Player);
        Player.AI.AddGoalAndEvaluate(Refit);
        AssertEqual(Homeworld, Refit.PlanetBuildingAt, "setup: the ship must be sent to the homeworld");
        AssertEqual(AIState.Refit, Refitted.AI.State, "setup: the ship must be on its way to refit");
    }

    // the goal is evaluated every second, standing in for the empire turn
    void RunWithRefitGoal(Func<bool> condition, Action body = null)
    {
        int frame = 0;
        RunSimWhile((simTimeout: 120, fatal: true), condition, () =>
        {
            if (++frame % 60 == 0)
                Refit.Evaluate();
            body?.Invoke();
        });
    }

    void RunUntilLanding() => RunWithRefitGoal(() => !Refitted.IsLanding);

    QueueItem QueuedRefit(Planet planet, Goal goal)
    {
        foreach (QueueItem q in planet.ConstructionQueue)
            if (q.Goal == goal)
                return q;
        return null;
    }

    QueueItem RunUntilQueued()
    {
        RunWithRefitGoal(() => Refitted.Active);
        Assert.IsFalse(Refitted.Dying, "the landed ship is removed, not destroyed");
        QueueItem refit = QueuedRefit(Homeworld, Refit);
        Assert.IsNotNull(refit, "the refit is queued once the ship has landed");
        return refit;
    }

    static Ship FinishBuild(Planet planet, QueueItem refit, Goal goal)
    {
        int index = 0;
        while (planet.ConstructionQueue[index] != refit)
            ++index;
        refit.ProductionSpent = refit.ActualCost;
        planet.ProdHere = 10f;
        planet.Owner.AddMoney(1000f);
        planet.Construction.RushProduction(index, 1f, rushButton: true);
        Assert.IsNotNull(goal.FinishedShip, "setup: the refit must be built");
        return goal.FinishedShip;
    }

    static Vector2 AroundPlanet(float degrees, float distance) => Vector2.Zero.PointFromAngle(degrees, distance);

    [TestMethod]
    public void TheOldOwnersRefitGoalLeavesACapturedShipAlone()
    {
        OrderRefit(Homeworld.Position + new Vector2(3000, 0));
        RunUntilLanding();
        Refit.Evaluate(); // the goal moves on to waiting for the landing to finish

        Refitted.LoyaltyChangeFromBoarding(Enemy, addNotification: false);
        RunObjectsSim(TestSimStep);
        AssertEqual(Enemy, Refitted.Loyalty, "setup: the ship must have changed hands");
        Refitted.AI.ChangeAIState(AIState.HoldPosition); // a state the refit goal also takes as waiting for the refit

        RunWithRefitGoal(() => Refitted.Active && !Refitted.IsLaunching);
        for (int i = 0; i < 3; ++i)
            Refit.Evaluate();
        Assert.IsTrue(Refitted.Active, "a ship captured on its way down takes off for its new owner");
        Assert.IsNull(QueuedRefit(Homeworld, Refit), "the old owner refits nothing for a ship it lost");
    }

    [TestMethod]
    public void ARefittingShipLandsOnThePlanetAndIsQueuedOnceItHasLanded()
    {
        OrderRefit(Homeworld.Position + new Vector2(5000, 0));
        RunUntilLanding();
        Assert.IsTrue(Refitted.Position.InRadius(Homeworld.Position, PlanetRange), "the ship lands on the planet");
        AssertGreaterThan(Refitted.Position.Distance(Homeworld.Position), PlanetRange - 50f,
                          "the landing starts as the ship comes within the planet's radius + 300");

        bool queuedWhileLanding = false;
        RunWithRefitGoal(() => Refitted.Active, () =>
        {
            if (Refitted.LandShip is { Done: false } && QueuedRefit(Homeworld, Refit) != null)
                queuedWhileLanding = true;
        });
        Assert.IsFalse(queuedWhileLanding, "the refit must not be queued while the ship is on its way down");
        Assert.IsNotNull(QueuedRefit(Homeworld, Refit), "the refit is queued once the ship has landed");
    }

    [TestMethod]
    public void AShipThatLandedOnThePlanetIsRefittedAndTakesOffFromThePlanet()
    {
        OrderRefit(Homeworld.Position + new Vector2(3000, 0));
        RunUntilLanding();
        QueueItem refit = RunUntilQueued();

        AddShipyard(new Vector2(0, Homeworld.Radius + 1000f));
        Ship newShip = FinishBuild(Homeworld, refit, Refit);
        Assert.IsTrue(newShip.IsLaunching, "the new ship takes off");
        AssertEqual(101f, Homeworld.Radius, newShip.Position.Distance(Homeworld.Position),
                    "the new ship must take off from the planet its old ship landed on, not from a shipyard");
    }

    [TestMethod]
    public void AShipThatLandedOnAShipyardIsRefittedAndTakesOffFromThatShipyard()
    {
        Ship landedOn = AddShipyard(AroundPlanet(0f, 2500f));
        for (int i = 1; i < 6; ++i)
            AddShipyard(AroundPlanet(i * 60f, 2500f));
        OrderRefit(Homeworld.Position + AroundPlanet(0f, 6000f));
        RunUntilLanding();
        Assert.IsTrue(Refitted.Position.InRadius(landedOn.Position, ShipyardRange), "setup: the ship must land on the nearest shipyard");
        RunWithRefitGoal(() => !Refitted.LandShip.Done);
        AssertLessThan(Refitted.Position.Distance(landedOn.Position), LandShip.TouchdownRadius + 5f, "the ship lands on the shipyard");

        QueueItem refit = RunUntilQueued();
        AssertEqual(landedOn, refit.LaunchShipyard, "the refit must remember the shipyard the old ship landed on");
        Ship newShip = FinishBuild(Homeworld, refit, Refit);
        Assert.IsTrue(newShip.IsLaunching, "the new ship takes off");
        AssertLessThan(newShip.Position.Distance(landedOn.Position), 51f,
                       "the new ship must take off from the shipyard its old ship landed on");
    }

    [TestMethod]
    public void ARefittingShipFliesStraightIntoItsShipyardLanding()
    {
        Ship shipyard = AddShipyard(new Vector2(0, 2500));
        OrderRefit(Homeworld.Position + new Vector2(5000, 2500));

        bool moving = false, stopped = false;
        RunSimWhile((simTimeout: 120, fatal: true), () => !Refitted.IsLanding, () =>
        {
            if (Refitted.CurrentVelocity > 100f) moving = true;
            if (moving && Refitted.CurrentVelocity < 10f) stopped = true;
        });
        Assert.IsTrue(moving, "setup: the ship must fly to the shipyard");
        Assert.IsFalse(stopped, "the ship must fly into its landing, not stop at the port first");
        AssertGreaterThan(Refitted.Position.Distance(shipyard.Position), ShipyardRange - 60f,
                          "the landing must start where a shipyard launch would end");
    }

    [TestMethod]
    public void AShipLeftWaitingAtItsPortIsSentOnToLand()
    {
        TestShip ship = SpawnShip("Vulcan Scout", Player, Homeworld.Position);
        Ship shipyard = AddShipyard(new Vector2(0, LandShip.ShipyardLandingRange(ship) + 1000f));
        OrderRefit(ship);
        Refitted.AI.ClearOrders(AIState.Refit); // how an older save left a ship that had reached its port

        RunUntilLanding();
        Assert.IsTrue(Refitted.Position.InRadius(shipyard.Position, ShipyardRange),
                      "a ship left waiting at its port must be sent on to land on the shipyard");
    }

    [TestMethod]
    public void TheGoalStartsTheLandingOfAShipAlreadyInRange()
    {
        Ship shipyard = AddShipyard(new Vector2(0, 2500));
        OrderRefit(shipyard.Position + new Vector2(500, 0));
        Assert.IsFalse(Refitted.IsLanding, "setup: the refit order alone must not land the ship");

        Refit.Evaluate();
        Assert.IsTrue(Refitted.IsLanding, "the goal must start the landing of a ship it finds in range");
    }

    [TestMethod]
    public void AShipWhoseRefitIsCalledOffOnTheWayIsFreed()
    {
        OrderRefit(Homeworld.Position + new Vector2(8000, 0));
        RunObjectsSim(1f);
        Player.AI.RemoveGoal(Refit);

        RunObjectsSim(1f);
        Assert.AreNotEqual(AIState.Refit, Refitted.AI.State, "a ship whose refit goal is gone stops refitting");
        RunObjectsSim(10f);
        Assert.IsFalse(Refitted.IsLanding, "a ship whose refit goal is gone must not land");
    }

    [TestMethod]
    public void AShipWhoseShipyardIsGoneBeforeTheRefitIsBuiltTakesOffAsUsual()
    {
        Ship landedOn = AddShipyard(new Vector2(0, 2500));
        OrderRefit(Homeworld.Position + new Vector2(0, 6000));
        RunUntilLanding();
        QueueItem refit = RunUntilQueued();
        Vector2 shipyardWasAt = landedOn.Position;
        landedOn.QueueTotalRemoval();

        Ship newShip = FinishBuild(Homeworld, refit, Refit);
        Assert.IsTrue(newShip.IsLaunching, "the new ship still takes off");
        AssertGreaterThan(newShip.Position.Distance(shipyardWasAt), 500f, "the new ship must not take off from a shipyard that is gone");
        AssertEqual(101f, Homeworld.Radius, newShip.Position.Distance(Homeworld.Position),
                    "with no shipyard left, the new ship takes off from the planet as usual");
    }

    [TestMethod]
    public void AShipWhosePortIsLostWhileItLandsTakesOffAndDropsTheRefitIfNoPortIsLeft()
    {
        OrderRefit(Homeworld.Position + new Vector2(3000, 0));
        RunUntilLanding();
        Homeworld.SetOwner(Enemy);

        RunWithRefitGoal(() => !Refitted.IsLaunching);
        Assert.IsTrue(Refitted.Active, "a ship whose refit was called off must not be removed");
        Assert.IsFalse(Refitted.IsLanding, "a ship whose refit was called off takes off again");
        Assert.IsFalse(Player.AI.HasGoal(g => g == Refit), "with no port left, the refit is dropped");
        Assert.IsNull(QueuedRefit(Homeworld, Refit), "a lost port must not queue the refit");

        RunSimWhile((simTimeout: 60, fatal: true), () => Refitted.IsLaunching);
        Assert.AreNotEqual(AIState.Refit, Refitted.AI.State, "the ship is back in play once it has taken off");
        Assert.IsFalse(Refitted.AI.IgnoreCombat, "the ship fights again once it has taken off");
    }

    Planet AddSecondPort()
    {
        Planet port = AddHomeWorldToEmpire(new Vector2(400_000), Player, new Vector2(405_000), explored: true);
        Player.UpdateRallyPoints();
        return port;
    }

    void AssertSentTo(Planet port, string reason)
    {
        AssertEqual(port, Refit.PlanetBuildingAt, reason);
        Assert.IsTrue(Refitted.AI.FindGoal(ShipAI.Plan.Refit, out ShipAI.ShipGoal order) && order.TargetPlanet == port, reason);
        AssertEqual(AIState.Refit, Refitted.AI.State, reason);
    }

    [TestMethod]
    public void AShipWhosePortIsLostOnTheWayDoesNotLandThereAndGoesToAnotherPort()
    {
        Planet otherPort = AddSecondPort();
        OrderRefit(Homeworld.Position + new Vector2(PlanetRange + 800f, 0));
        Homeworld.SetOwner(Enemy);

        RunObjectsSim(10f);
        Assert.IsFalse(Refitted.IsLanding, "a ship must not land on a port that is no longer ours");

        Refit.Evaluate();
        AssertSentTo(otherPort, "a ship whose port is lost on the way must be sent to another port");
    }

    [TestMethod]
    public void AShipWhosePortIsLostWhileItLandsTakesOffForAnotherPort()
    {
        Planet otherPort = AddSecondPort();
        OrderRefit(Homeworld.Position + new Vector2(3000, 0));
        RunUntilLanding();
        Homeworld.SetOwner(Enemy);

        RunWithRefitGoal(() => !Refitted.IsLaunching);
        Assert.IsTrue(Refitted.Active, "a ship whose port was lost must not be removed");
        Assert.IsNull(QueuedRefit(Homeworld, Refit), "a lost port must not queue the refit");
        AssertSentTo(otherPort, "a ship whose port is lost while it lands must take off for another port");

        RunSimWhile((simTimeout: 60, fatal: true), () => Refitted.IsLaunching);
        RunObjectsSim(1f);
        AssertSentTo(otherPort, "the ship keeps heading for the other port once it has taken off");
        Assert.IsTrue(Refitted.AI.IgnoreCombat, "a ship on its way to refit still ignores combat after taking off");
    }

    [TestMethod]
    public void RefittingALandingShipAgainKeepsItsRefitAndFleetSlot()
    {
        Fleet fleet = Player.CreateFleet(1, null);
        TestShip ship = SpawnShip("Vulcan Scout", Player, Homeworld.Position + new Vector2(3000, 0));
        fleet.AddShips(new Array<Ship> { ship });
        OrderRefit(ship);
        Assert.IsTrue(fleet.FindNodeWithGoal(Refit, out _), "setup: the fleet slot must wait for the refit");
        RunUntilLanding();

        Player.AI.AddGoalAndEvaluate(new RefitShip(Refitted, RefitTo, Player));
        Assert.IsTrue(Player.AI.HasGoal(g => g == Refit), "a second refit order must not cancel the refit of a landing ship");
        Assert.IsTrue(fleet.FindNodeWithGoal(Refit, out _), "a second refit order must not take the fleet slot of a landing ship");
    }

    void LosePortWithItsQueue()
    {
        Homeworld.Construction.ClearQueue(); // as an invasion does when the planet changes hands
        Homeworld.SetOwner(Enemy);
    }

    [TestMethod]
    public void ARefitWhosePortIsLostWhileItIsBuiltStartsOverAtAnotherPort()
    {
        Planet otherPort = AddSecondPort();
        OrderRefit(Homeworld.Position + new Vector2(3000, 0));
        RunUntilLanding();
        QueueItem refit = RunUntilQueued();
        refit.ProductionSpent = 1f;
        LosePortWithItsQueue();

        Refit.Evaluate();
        AssertEqual(otherPort, Refit.PlanetBuildingAt, "a refit whose port is lost while it is built moves to another port");
        Assert.AreSame(refit, QueuedRefit(otherPort, Refit), "the refit must be queued again at the other port");
        AssertEqual(0f, refit.ProductionSpent, "the production spent at the lost port is lost with it");

        Ship otherShipyard = SpawnShip("Shipyard", Player, otherPort.Position + new Vector2(0, 2500));
        otherShipyard.TetherToPlanet(otherPort);
        Ship newShip = FinishBuild(otherPort, refit, Refit);
        AssertLessThan(newShip.Position.Distance(otherShipyard.Position), 51f,
                       "the new ship takes off as usual at the new port, not from a planet only the old port had");
    }

    [TestMethod]
    public void ARefitWhosePortIsLostWhileItIsBuiltIsDroppedIfNoPortIsLeft()
    {
        Fleet fleet = Player.CreateFleet(1, null);
        TestShip ship = SpawnShip("Vulcan Scout", Player, Homeworld.Position + new Vector2(3000, 0));
        fleet.AddShips(new Array<Ship> { ship });
        OrderRefit(ship);
        RunUntilLanding();
        RunUntilQueued();
        LosePortWithItsQueue();

        Refit.Evaluate();
        Assert.IsFalse(Player.AI.HasGoal(g => g == Refit), "with no port left, the refit is dropped");
        Assert.IsFalse(fleet.FindNodeWithGoal(Refit, out _), "a dropped refit must not leave its fleet slot waiting for it");
    }

    [TestMethod]
    public void ARefitOrderThatFailsDoesNotStrandTheFleetSlot()
    {
        Fleet fleet = Player.CreateFleet(1, null);
        TestShip ship = SpawnShip("Vulcan Scout", Player, Homeworld.Position + new Vector2(PlanetRange - 100f, 0));
        fleet.AddShips(new Array<Ship> { ship });
        OrderRefit(ship);

        var second = new RefitShip(Refitted, RefitTo, Player);
        Assert.IsTrue(fleet.FindNodeWithGoal(second, out _), "setup: the second refit order must take over the fleet slot");
        Assert.IsTrue(Refitted.AI.TryLand(LandPlan.Refit, Homeworld, null), "setup: the ship starts landing before the order is evaluated");

        Player.AI.AddGoalAndEvaluate(second);
        Assert.IsFalse(fleet.FindNodeWithGoal(second, out _), "a refit order that fails must not leave the fleet slot waiting for it");
    }

    [TestMethod]
    public void ARefitNeverTakesOffFromAShipyardAtAnotherPlanet()
    {
        Planet otherPort = AddSecondPort();
        Ship elsewhere = SpawnShip("Shipyard", Player, otherPort.Position + new Vector2(0, 2500));
        elsewhere.TetherToPlanet(otherPort);
        OrderRefit(Homeworld.Position + new Vector2(3000, 0));
        RunUntilLanding();
        QueueItem refit = RunUntilQueued();

        refit.LaunchShipyard = elsewhere;
        refit.LaunchFromPlanet = false;
        Ship newShip = FinishBuild(Homeworld, refit, Refit);
        AssertEqual(101f, Homeworld.Radius, newShip.Position.Distance(Homeworld.Position),
                    "a refit must take off at the planet that built it, never from a shipyard at another planet");
    }

    [TestMethod]
    public void AShipLandingForRefitIsStillRefittingAndOutOfPlay()
    {
        OrderRefit(Homeworld.Position + new Vector2(3000, 0));
        RunUntilLanding();

        AssertEqual(AIState.Refit, Refitted.AI.State, "a ship landing for refit stays in the refit state");
        Assert.IsTrue(Refitted.AI.HasPriorityOrder, "a landing ship keeps its priority order");
        Assert.IsFalse(Refitted.IsIdleScout(), "a scout landing for refit is not free to explore");
        Assert.IsFalse(Player.AllFleetReadyShips().Contains(Refitted), "a ship landing for refit is not free for a fleet");
        Assert.IsFalse(Refitted.CanBeRefitted, "a landing ship cannot be refitted or scrapped again");
        Assert.IsFalse(Enemy.IsEmpireAttackable(Player, Refitted), "enemies must not target a landing ship");
    }

    [TestMethod]
    public void APlatformRefittedAtItsPlanetDoesNotLand()
    {
        TestShip platform = (TestShip)AddShipyard(new Vector2(0, Homeworld.Radius + 100f));
        OrderRefit(platform);
        AssertEqual(GoalStep.GoToNextStep, Refit.Evaluate(), "setup: the platform is at its port");
        Assert.IsFalse(platform.IsLanding, "a platform is refitted where it is, not landed");

        RunObjectsSim(10 * TestSimStep.FixedTime);
        Assert.IsFalse(platform.IsLanding, "a platform waiting for its refit must not land either");
    }

    [TestMethod]
    public void AQueuedRefitStillTakesOffFromItsShipyardAfterALoad()
    {
        UState.StarDate = 1042.5f;
        Ship landedOn = AddShipyard(new Vector2(0, 2500));
        OrderRefit(Homeworld.Position + new Vector2(0, 6000));
        RunUntilLanding();
        RunUntilQueued();

        SavedGame save = Universe.Save("UnitTest.RefitLanding", throwOnError: true);
        UniverseScreen loaded = LoadGame.Load(save.SaveFile, noErrorDialogs: true, startSimThread: false);
        Goal loadedRefit = loaded.UState.Player.AI.FindGoal(g => g.Type == GoalType.Refit);
        Assert.IsNotNull(loadedRefit, "setup: the refit goal must survive the load");
        QueueItem refit = QueuedRefit(loadedRefit.PlanetBuildingAt, loadedRefit);
        Assert.IsNotNull(refit, "setup: the queued refit must survive the load");
        Assert.IsNotNull(refit.LaunchShipyard, "the queued refit must still know its shipyard after a load");
        AssertEqual(landedOn.Id, refit.LaunchShipyard.Id, "the queued refit must still know its shipyard after a load");

        Ship newShip = FinishBuild(loadedRefit.PlanetBuildingAt, refit, loadedRefit);
        AssertLessThan(newShip.Position.Distance(refit.LaunchShipyard.Position), 51f,
                       "the loaded refit must take off from the shipyard its old ship landed on");
    }
}
