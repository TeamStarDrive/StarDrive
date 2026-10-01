using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using SDGraphics;
using Ship_Game;
using Ship_Game.AI;
using Ship_Game.GameScreens.LoadGame;
using Ship_Game.Ships;
using Vector2 = SDGraphics.Vector2;
#pragma warning disable CA2213

namespace UnitTests.Ships;

/// <summary>
/// A supply shuttle a colony sends to rearm a ship takes off from the colony's space port and, once it has
/// delivered, flies home and lands on it: a glide into the station drawn over the planet, like a shipyard
/// landing. A colony without a space port launches and lands its shuttles on the planet. Shipyards are not used.
/// </summary>
[TestClass]
public class SupplyShuttleLandingTests : StarDriveTest
{
    readonly Planet Homeworld;

    public SupplyShuttleLandingTests()
    {
        LoadStarterShips("Shipyard");
        CreateUniverseAndPlayerEmpire();
        UState.Objects.EnableParallelUpdate = false;
        Homeworld = AddHomeWorldToEmpire(new Vector2(200_000), Player, new Vector2(205_000), explored: true);
        Player.UpdateRallyPoints();
    }

    float PlanetRange => Homeworld.Radius + 300f;

    Ship AddShipyard(Vector2 offset)
    {
        Ship shipyard = SpawnShip("Shipyard", Player, Homeworld.Position + offset);
        shipyard.TetherToPlanet(Homeworld);
        return shipyard;
    }

    Goal SendShuttleToRearm(Vector2 targetOffset, out Ship target, out Ship shuttle)
    {
        target = SpawnShip("Rocket Scout", Player, Homeworld.Position + targetOffset);
        AssertGreaterThan(target.OrdinanceMax, 0f, "setup: the ship to rearm must use ordnance");
        target.ChangeOrdnance(-target.Ordinance * 0.5f);
        Ship inSystem = target;
        RunSimWhile((simTimeout: 5, fatal: false), () => inSystem.System == null);
        AssertEqual(Homeworld.System, target.System, "setup: the ship to rearm must be in the colony's system");

        Ship toRearm = target;
        Player.AI.AddPlanetaryRearmGoal(toRearm, Homeworld);
        Goal rearm = Player.AI.FindGoal(g => g.Type == GoalType.RearmShipFromPlanet && g.TargetShip == toRearm);
        Assert.IsNotNull(rearm, "setup: the colony must take on the rearm");
        rearm.Evaluate();
        shuttle = rearm.FinishedShip;
        Assert.IsTrue(shuttle is { IsSupplyShuttle: true, IsLaunching: true }, "setup: the colony must launch a supply shuttle");
        return rearm;
    }

    // the goal is evaluated every second, standing in for the empire turn
    void RunWithGoal(Goal goal, Func<bool> condition, Action body = null)
    {
        int frame = 0;
        RunSimWhile((simTimeout: 120, fatal: true), condition, () =>
        {
            if (++frame % 60 == 0)
                goal.Evaluate();
            body?.Invoke();
        });
    }

    bool HasGoal(Goal goal) => Player.AI.FindGoal(g => g == goal) != null;

    [TestMethod]
    public void AShuttleTakesOffFromAndLandsOnTheSpacePortNotAShipyard()
    {
        Assert.IsTrue(Homeworld.HasSpacePort, "setup: the homeworld must have a space port");
        Ship shipyard = AddShipyard(new Vector2(0, 2500));
        Goal rearm = SendShuttleToRearm(new Vector2(6000, 2500), out Ship target, out Ship shuttle);
        AssertLessThan(shuttle.Position.Distance(Homeworld.Position), 51f, "the shuttle takes off from the space port over the planet");
        int slotsWhileOut = Homeworld.NumSupplyShuttlesCanLaunch();

        float ordnanceBefore = target.Ordinance;
        float speedBefore = 0f;
        RunWithGoal(rearm, () => !shuttle.IsLanding, () => speedBefore = shuttle.CurrentVelocity);
        AssertGreaterThan(target.Ordinance, ordnanceBefore, "setup: the shuttle must rearm the ship before it comes home");
        AssertEqual(10f, LaunchShip.ShipyardSpeed(shuttle), speedBefore, "the shuttle comes in at launch speed to glide on into the space port");
        float portRange = LandShip.ShipyardLandingRange(shuttle);
        float startDistance = shuttle.Position.Distance(Homeworld.Position);
        AssertGreaterThan(startDistance, portRange - 60f, "the landing on the space port starts where a launch from it would end");
        AssertLessThan(startDistance, portRange + 1f, "the landing on the space port starts where a launch from it would end");
        AssertEqual(AIState.SupplyReturnHome, shuttle.AI.State, "a landing shuttle is still on its way home");

        rearm.Evaluate();
        AssertEqual(slotsWhileOut, Homeworld.NumSupplyShuttlesCanLaunch(), "the shuttle holds its slot at the colony while it is on its way down");

        RunSimWhile((simTimeout: 60, fatal: true), () => shuttle.Active);
        Assert.IsFalse(shuttle.Dying, "a landed shuttle is removed, not destroyed");
        rearm.Evaluate();
        Assert.IsFalse(HasGoal(rearm), "the rearm is done once the shuttle has landed");
        AssertEqual(slotsWhileOut + 1, Homeworld.NumSupplyShuttlesCanLaunch(), "the colony gets the slot back once the shuttle has landed");
        AssertLessThan(shuttle.Position.Distance(Homeworld.Position), LandShip.TouchdownRadius + 5f, "the shuttle must glide into the space port, not the shipyard");
        AssertGreaterThan(shuttle.Position.Distance(shipyard.Position), 2000f, "the shuttle must not land on the shipyard");
    }

    [TestMethod]
    public void AColonyWithoutASpacePortUsesThePlanet()
    {
        Homeworld.HasSpacePort = false;
        Goal rearm = SendShuttleToRearm(new Vector2(6000, 0), out Ship target, out Ship shuttle);
        float launchDistance = shuttle.Position.Distance(Homeworld.Position);
        AssertGreaterThan(launchDistance, Homeworld.Radius - 101f, "without a space port the shuttle takes off from the planet");
        AssertLessThan(launchDistance, Homeworld.Radius + 101f, "without a space port the shuttle takes off from the planet");

        float ordnanceBefore = target.Ordinance;
        RunWithGoal(rearm, () => !shuttle.IsLanding);
        AssertGreaterThan(target.Ordinance, ordnanceBefore, "setup: the shuttle must rearm the ship before it comes home");
        float startDistance = shuttle.Position.Distance(Homeworld.Position);
        AssertGreaterThan(startDistance, PlanetRange - 50f, "the landing starts as the shuttle comes within the planet's radius + 300");
        AssertLessThan(startDistance, PlanetRange + 1f, "without a space port the shuttle lands on the planet");

        RunSimWhile((simTimeout: 60, fatal: true), () => shuttle.Active);
        Assert.IsFalse(shuttle.Dying, "a landed shuttle is removed, not destroyed");
        Assert.IsFalse(Homeworld.HasSpacePort, "setup: the colony must still have no space port");
    }

    [TestMethod]
    public void TheColonyTurnCountsTheSupplyShuttlesOutForTheColonyScreen()
    {
        SendShuttleToRearm(new Vector2(6000, 0), out _, out _);
        AssertEqual(0, Homeworld.SupplyShuttlesOut, "setup: the count shown is refreshed by the colony's turn");

        Player.UpdateEmpirePlanets();
        int shuttlesOut = Player.AI.CountGoals(g => g.Type == GoalType.RearmShipFromPlanet && g.PlanetBuildingAt == Homeworld);
        AssertGreaterThan(shuttlesOut, 0, "setup: the colony must have a shuttle out");
        AssertEqual(shuttlesOut, Homeworld.SupplyShuttlesOut, "the colony's turn counts the supply shuttles it has out");
        AssertEqual((int)Homeworld.InfraStructure, Homeworld.SupplyShuttlesLimit, "the colony can have as many out as its infrastructure");
        AssertEqual(Homeworld.SupplyShuttlesLimit - shuttlesOut, Homeworld.NumSupplyShuttlesCanLaunch(), "the free slots are the limit less the shuttles out");

        Homeworld.SetOwner(Enemy);
        AssertEqual(0, Homeworld.SupplyShuttlesOut, "a captured colony shows none of its old owner's shuttles");
    }

    [TestMethod]
    public void AShuttleWhoseColonyIsLostIsScuttledAndDoesNotLand()
    {
        Vector2 inRange = Homeworld.Position + new Vector2(300f, 0);
        Ship control = SpawnShip("Supply Shuttle", Player, inRange);
        control.AI.OrderSupplyShipLand(Homeworld);
        RunObjectsSim(TestSimStep);
        Assert.IsTrue(control.IsLanding, "setup: a shuttle this close to home starts down at once");

        Ship shuttle = SpawnShip("Supply Shuttle", Player, inRange);
        shuttle.AI.OrderSupplyShipLand(Homeworld);
        Homeworld.SetOwner(Enemy);
        RunObjectsSim(TestSimStep);
        Assert.IsFalse(shuttle.IsLanding, "a shuttle must not land on a colony that is no longer its empire's");
        AssertEqual(AIState.Scuttle, shuttle.AI.State, "a shuttle with no home to land on is scuttled");
    }

    [TestMethod]
    public void AShuttleOnAnOldReturnOrderIsGivenTheLandingOrder()
    {
        Goal rearm = SendShuttleToRearm(new Vector2(6000, 0), out _, out Ship shuttle);
        RunWithGoal(rearm, () => !shuttle.AI.FindGoal(ShipAI.Plan.SupplyReturnHome, out _));

        Vector2 toPlanet = shuttle.Position.DirectionToTarget(Homeworld.Position);
        shuttle.AI.OrderMoveToNoStop(Homeworld.Position + new Vector2(Homeworld.Radius, 0), toPlanet, AIState.SupplyReturnHome);
        Assert.IsFalse(shuttle.AI.FindGoal(ShipAI.Plan.SupplyReturnHome, out _), "setup: the shuttle must be on a plain move home, as older saves have it");

        rearm.Evaluate();
        Assert.IsTrue(shuttle.AI.FindGoal(ShipAI.Plan.SupplyReturnHome, out _), "the rearm goal must give a shuttle on a plain move home its landing order");
        RunWithGoal(rearm, () => shuttle.Active);
        Assert.IsFalse(shuttle.Dying, "the shuttle must land and be removed");
    }

    [TestMethod]
    public void ASpacePortLandingCarriesOnAfterALoad()
    {
        UState.StarDate = 1042.5f;
        Goal rearm = SendShuttleToRearm(new Vector2(6000, 0), out _, out Ship shuttle);
        RunWithGoal(rearm, () => !shuttle.IsLanding);

        SavedGame save = Universe.Save("UnitTest.SupplyLanding", throwOnError: true);
        UniverseScreen loaded = LoadGame.Load(save.SaveFile, noErrorDialogs: true, startSimThread: false);
        Ship landing = loaded.UState.Objects.FindShip(shuttle.Id);
        Planet loadedHomeworld = loaded.UState.GetPlanet(Homeworld.Id);
        Assert.IsTrue(landing is { Active: true, IsLanding: true, AI.State: AIState.SupplyReturnHome },
                      "a landing shuttle must still be landing after a load");

        for (double time = 0; landing.Active; time += TestSimStepD)
        {
            AssertLessThan(time, 60.0, "the loaded landing must finish");
            loaded.UState.Objects.Update(TestSimStep);
        }
        Assert.IsFalse(landing.Dying, "a landed shuttle is removed, not destroyed");
        AssertLessThan(landing.Position.Distance(loadedHomeworld.Position), LandShip.TouchdownRadius + 5f, "the loaded landing must still glide into the space port");
    }
}
