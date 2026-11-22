using Microsoft.VisualStudio.TestTools.UnitTesting;
using Ship_Game;
using Ship_Game.AI;
using Ship_Game.Ships;
using Vector2 = SDGraphics.Vector2;

namespace UnitTests.AITests.Ships;

/// <summary>
/// Ships sent to one of our ships far away fly the route planner's route there, planned once for the trip,
/// and planned again where its last leg ends if that ship has moved on (#558).
/// </summary>
[TestClass]
public class RouteToShipTests : StarDriveTest
{
    static readonly Vector2 Start = new(200_000f, 300_000f);
    readonly Ship Carrier;

    public RouteToShipTests()
    {
        LoadStarterShips("TEST_Excalibur-Class Supercarrier", "Vulcan Scout");
        CreateUniverseAndPlayerEmpire();
        LoadStarterShips(Player.data.DefaultTroopShip);
        UState.Objects.EnableParallelUpdate = false;
        Carrier = SpawnShip("TEST_Excalibur-Class Supercarrier", Player, new Vector2(1_200_000f, 300_000f));
        Carrier.Carrier.AllowBoardShip = false;
        Carrier.AI.OrderHoldPosition(MoveOrder.HoldPosition);
    }

    Ship TroopShip()
    {
        Ship troopShip = SpawnShip(Player.data.DefaultTroopShip, Player, Start);
        ResourceManager.CreateTroop("Wyvern", Player).LandOnShip(troopShip);
        return troopShip;
    }

    SolarSystem EnemySystemAt(Vector2 pos)
    {
        SolarSystem system = AddDummyPlanetToEmpire(pos, Enemy).System;
        system.SetExploredBy(Player);
        return system;
    }

    void MoveCarrierTo(Vector2 pos)
    {
        Carrier.Position = pos;
        Carrier.AI.OrderHoldPosition(pos, Vectors.Up, MoveOrder.HoldPosition);
    }

    // the closest the ship came to the system on its way to our carrier, which it reached still under its order
    float FlyToTheCarrier(Ship ship, System.Action order, ShipAI.Plan plan, SolarSystem system, System.Action beforeEachStep = null)
    {
        bool wasOn = GlobalStats.RouteAroundGravityWells;
        float closest = float.MaxValue;
        try
        {
            GlobalStats.RouteAroundGravityWells = true;
            order();
            RunSimWhile((simTimeout: 300, fatal: false),
                        () => ship.Position.OutsideRadius(Carrier.Position, GravityWellRouter.DetourReachDistance),
                        () =>
                        {
                            beforeEachStep?.Invoke();
                            if (system != null)
                                closest = System.Math.Min(closest, ship.Position.Distance(system.Position));
                        });
        }
        finally
        {
            GlobalStats.RouteAroundGravityWells = wasOn;
        }

        AssertLessThan(ship.Position.Distance(Carrier.Position), GravityWellRouter.DetourReachDistance, "the ship reaches our carrier");
        Assert.IsTrue(ship.AI.OrderQueue.TryPeekFirst(out ShipAI.ShipGoal goal) && goal.Plan == plan, $"still on its {plan} order");
        return closest;
    }

    [TestMethod]
    public void TroopsSentToOurShipGoAroundAnEnemySystemOnTheWayOnARoutePlannedOnce()
    {
        SolarSystem enemy = EnemySystemAt(new Vector2(700_000f, 300_000f));
        Ship troopShip = TroopShip();

        float closest = FlyToTheCarrier(troopShip, () => troopShip.AI.OrderRebaseToShip(Carrier), ShipAI.Plan.RebaseToShip, enemy);

        AssertGreaterThan(closest, enemy.Radius, "the troop ship flies around the enemy system on the straight line to our ship");
        AssertEqual(1, troopShip.AI.ShipRoutesPlanned, "the route is planned once for the trip");
    }

    [TestMethod]
    public void TroopsPlanTheirRouteAgainWhereItsLastLegEndsIfOurShipMovedOn()
    {
        SolarSystem enemy = EnemySystemAt(new Vector2(1_200_000f, 800_000f));
        Ship troopShip = TroopShip();
        Vector2 legEnd = Carrier.Position;
        AssertLessThan(enemy.Radius * 2.2f, legEnd.Distance(enemy.Position), "setup: the system lies far from where the first leg ends, so the route passes around all of it");
        bool moved = false;

        float closest = FlyToTheCarrier(troopShip, () => troopShip.AI.OrderRebaseToShip(Carrier), ShipAI.Plan.RebaseToShip, enemy, () =>
        {
            if (moved || troopShip.Position.OutsideRadius(legEnd, 30_000f))
                return;
            moved = true;
            MoveCarrierTo(new Vector2(1_200_000f, 1_300_000f));
        });

        Assert.IsTrue(moved, "setup: our ship moved on as the troop ship neared it");
        AssertGreaterThan(closest, enemy.Radius, "the route from where the last leg ended to where our ship went passes around the enemy system");
        AssertEqual(2, troopShip.AI.ShipRoutesPlanned, "once for the trip and once where its last leg ended");
    }

    [TestMethod]
    public void ATroopShipThatLosesOurShipAfterReachingItFollowsItInsteadOfGoingBack()
    {
        Ship troopShip = TroopShip();
        troopShip.AI.OrderRebaseToShip(Carrier);
        RunSimWhile((simTimeout: 100, fatal: true), () => troopShip.Position.X < 600_000f);
        AssertEqual(1, troopShip.AI.ShipRoutesPlanned, "setup: the troop ship is on its way to our ship");

        RunSimWhile((simTimeout: 10 * TestSimStepD, fatal: false), null,
                    () => MoveCarrierTo(troopShip.Position + new Vector2(0f, 5_000f)));
        AssertEqual(ShipAI.Plan.RebaseToShip, troopShip.AI.OrderQueue.PeekFirst.Plan, "setup: our ship came by within reach, but out of boarding range");

        float lostAt = troopShip.Position.X;
        float farthestEast = lostAt;
        MoveCarrierTo(new Vector2(100_000f, 300_000f));
        RunSimWhile((simTimeout: 100, fatal: true),
                    () => troopShip.Position.OutsideRadius(Carrier.Position, GravityWellRouter.DetourReachDistance),
                    () => farthestEast = System.Math.Max(farthestEast, troopShip.Position.X));

        AssertLessThan(farthestEast, lostAt + 200_000f, "the troop ship turns after our ship, not on to where it first flew");
        AssertEqual(2, troopShip.AI.ShipRoutesPlanned, "the trip ended when it reached our ship, so it planned a new one");
        AssertEqual(ShipAI.Plan.RebaseToShip, troopShip.AI.OrderQueue.PeekFirst.Plan, "still on its way to land on our ship");
    }

    [TestMethod]
    public void ATroopShipStartingNearOurShipFliesStraightToItWithNoRoute()
    {
        Ship troopShip = SpawnShip(Player.data.DefaultTroopShip, Player, Carrier.Position - new Vector2(12_000f, 0f));
        ResourceManager.CreateTroop("Wyvern", Player).LandOnShip(troopShip);

        FlyToTheCarrier(troopShip, () => troopShip.AI.OrderRebaseToShip(Carrier), ShipAI.Plan.RebaseToShip, null);

        AssertEqual(0, troopShip.AI.ShipRoutesPlanned, "within twice the router's reach the troop ship plans no route");
    }

    [TestMethod]
    public void WithRoutingOffATroopShipFollowsOurShipStraight()
    {
        Ship troopShip = TroopShip();
        bool wasOn = GlobalStats.RouteAroundGravityWells;
        try
        {
            GlobalStats.RouteAroundGravityWells = false;
            troopShip.AI.OrderRebaseToShip(Carrier);
            RunSimWhile((simTimeout: 300, fatal: true),
                        () => troopShip.Position.OutsideRadius(Carrier.Position, GravityWellRouter.DetourReachDistance));
        }
        finally
        {
            GlobalStats.RouteAroundGravityWells = wasOn;
        }

        AssertEqual(0, troopShip.AI.ShipRoutesPlanned, "with the Pathfinder option off no route is planned, as for every other move");
    }

    [TestMethod]
    [DataRow(ShipAI.Plan.Escort)]
    [DataRow(ShipAI.Plan.SupplyShip)]
    [DataRow(ShipAI.Plan.RearmShipFromPlanet)]
    [DataRow(ShipAI.Plan.BuildOrbital)]
    public void ShipsSentToOurFarShipGoAroundAnEnemySystemOnTheWay(ShipAI.Plan plan)
    {
        SolarSystem enemy = EnemySystemAt(new Vector2(700_000f, 300_000f));
        Planet home = AddDummyPlanetToEmpire(new Vector2(200_000f, -500_000f), Player);
        Ship ship = SpawnShip("Vulcan Scout", Player, Start);
        System.Action order = plan switch
        {
            ShipAI.Plan.Escort       => () => ship.AI.AddEscortGoal(Carrier),
            ShipAI.Plan.BuildOrbital => () => ship.AI.AddBuildOrbitalGoal(home, Carrier),
            _                        => () => ship.AI.AddSupplyShipGoal(Carrier, plan),
        };

        float closest = FlyToTheCarrier(ship, order, plan, enemy);

        AssertGreaterThan(closest, enemy.Radius, $"a ship on its {plan} order flies around the enemy system on the straight line to our ship");
        AssertEqual(1, ship.AI.ShipRoutesPlanned, "the route is planned once for the trip");
    }
}
