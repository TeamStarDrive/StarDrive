using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using SDUtils;
using Ship_Game;
using Ship_Game.AI;
using Ship_Game.Fleets;
using Ship_Game.Ships;
using UnitTests.Ships;
using Vector2 = SDGraphics.Vector2;

namespace UnitTests.AITests.Ships;

[TestClass]
public class WarpPastEnemyTests : StarDriveTest
{
    static readonly Vector2 EnemyPos = new(300_000, 300_000);
    static readonly Vector2 Heading = Vectors.Up;
    readonly TestShip EnemyShip;

    public WarpPastEnemyTests()
    {
        CreateUniverseAndPlayerEmpire();
        UState.Events.Disabled = true;
        EnemyShip = SpawnShipNoCombatHoldPos("Vulcan Scout", Enemy, EnemyPos);
        EnemyShip.AI.SetCombatTriggerDelay(1000f);
    }

    Ship SpawnApproaching(float lateral)
    {
        Ship ship = SpawnShip("Vulcan Scout", Player, EnemyPos - Heading * 60_000 + new Vector2(lateral, 0), Heading);
        ship.SensorRange = 40_000;
        return ship;
    }

    float MinWarpExitRoom(Ship ship) => ship.WarpExitBrakingDistance + ship.MaxFTLSpeed * TestSimStep.FixedTime * 2f;

    // how far past the enemy each ship was, along the heading, when it left warp
    float[] PastEnemyAtWarpExit(Array<Ship> ships, Fleet fleet = null, Action<Ship> inWarp = null)
    {
        var wasInWarp = new bool[ships.Count];
        var dropped = new bool[ships.Count];
        var past = new float[ships.Count];
        RunSimWhile((60, fatal: false), () => Array.Exists(dropped, d => !d), body: () =>
        {
            fleet?.Update(TestSimStep);
            for (int i = 0; i < ships.Count; ++i)
            {
                if (ships[i].IsInWarp)
                {
                    wasInWarp[i] = true;
                    inWarp?.Invoke(ships[i]);
                }
                else if (wasInWarp[i] && !dropped[i])
                {
                    dropped[i] = true;
                    past[i] = (ships[i].Position - EnemyPos).Dot(Heading);
                }
            }
        });
        for (int i = 0; i < ships.Count; ++i)
            AssertTrue(dropped[i], $"ship {i} must warp and leave warp (was in warp: {wasInWarp[i]})");
        return past;
    }

    float UsualWarpExit(Ship ship) => ship.WarpOutDistance - ship.MaxFTLSpeed * TestSimStep.FixedTime * 2f - 100f;

    // how far short of a point the ship was when its leg to that point stopped being its current order
    float LegEndShortOf(Ship ship, Vector2 point)
    {
        bool wasOnLeg = false;
        float shortOf = -1f;
        bool inWarp = false;
        RunSimWhile((60, fatal: false), () => shortOf < 0f, body: () =>
        {
            bool onLeg = ship.AI.OrderQueue.TryPeekFirst(out ShipAI.ShipGoal g) && g.Plan == ShipAI.Plan.MoveToWithin1000
                         && g.MovePosition.InRadius(point, 1f);
            if (onLeg)
                wasOnLeg = true;
            else if (wasOnLeg)
            {
                shortOf = ship.Position.Distance(point);
                inWarp = ship.IsInWarp;
            }
        });
        AssertTrue(wasOnLeg && shortOf >= 0f, "the ship must fly its leg to the point and end it");
        AssertTrue(inWarp, "the leg must end in warp");
        return shortOf;
    }

    [TestMethod]
    public void AShipStoppingBeyondAnEnemyLeavesWarpPastIt()
    {
        Ship ship = SpawnApproaching(0);
        float beyond = ship.WarpOutDistance - 1000f;
        AssertGreaterThan(beyond, MinWarpExitRoom(ship) + 500f, "setup: the stop must leave room to brake past the enemy");
        Vector2 destination = EnemyPos + Heading * beyond;

        ship.AI.OrderMoveTo(destination, Heading, AIState.AwaitingOrders);
        float past = PastEnemyAtWarpExit(new Array<Ship> { ship })[0];

        AssertGreaterThan(past, 0f, "the ship must leave warp past the enemy, not in front of it");
        AssertGreaterThan(beyond - past, ship.WarpExitBrakingDistance, "the ship must still leave warp with room to brake before its stop");
        AssertLessThan(beyond - past, MinWarpExitRoom(ship), "the ship must keep its warp until it has just room left to brake");
    }

    [TestMethod]
    public void AFleetStoppingBeyondAnEnemyLeavesWarpPastIt()
    {
        var ships = new Array<Ship> { SpawnApproaching(-500), SpawnApproaching(500) };
        Fleet fleet = Player.CreateFleet(1, null);
        fleet.AddShips(ships);
        ships[0].RelativeFleetOffset = new Vector2(-500, 0);
        ships[1].RelativeFleetOffset = new Vector2(500, 0);
        float beyond = 4000f;
        AssertGreaterThan(beyond, MinWarpExitRoom(ships[0]) + 500f, "setup: the stop must leave room to brake past the enemy");

        fleet.MoveTo(EnemyPos + Heading * beyond, Heading, MoveOrder.Regular);
        float[] past = PastEnemyAtWarpExit(ships, fleet);

        for (int i = 0; i < ships.Count; ++i)
            AssertGreaterThan(past[i], 0f, $"fleet ship {i} must leave warp past the enemy, not in front of it");
    }

    [TestMethod]
    public void AShipStoppingAtAnEnemyLeavesWarpWhereItAlwaysDid()
    {
        Ship ship = SpawnApproaching(0);
        ship.AI.OrderMoveTo(EnemyPos, Heading, AIState.AwaitingOrders);
        float past = PastEnemyAtWarpExit(new Array<Ship> { ship })[0];

        AssertGreaterThan(-past, UsualWarpExit(ship), "a ship stopping at an enemy must leave warp as far short of it as before");
    }

    [TestMethod]
    public void AShipFasterThanItsTopWarpSpeedStillLeavesWarpWithRoomToBrake()
    {
        Ship ship = SpawnApproaching(0);
        float beyond = ship.WarpOutDistance - 1000f;
        AssertGreaterThan(beyond, MinWarpExitRoom(ship) + 500f, "setup: the stop must leave room to brake past the enemy");
        Vector2 destination = EnemyPos + Heading * beyond;
        float topSpeed = ship.Stats.MaxFTLSpeed;

        // its top warp speed drops near the stop, as in an enemy's borders, and its speed bleeds off slowly
        ship.AI.OrderMoveTo(destination, Heading, AIState.AwaitingOrders);
        float past = PastEnemyAtWarpExit(new Array<Ship> { ship }, inWarp: s =>
        {
            if (s.Position.InRadius(destination, 20_000))
                s.Stats.MaxFTLSpeed = topSpeed * 0.25f;
        })[0];

        AssertGreaterThan(past, 0f, "a ship faster than its top warp speed must still leave warp past the enemy");
        AssertGreaterThan(beyond - past, ship.WarpExitBrakingDistance, "a ship faster than its top warp speed must still leave warp with room to brake");
    }

    [TestMethod]
    public void AnEnemyWellOffThePathLeavesTheWarpAlone()
    {
        var aside = new Vector2(6000, 0);
        Ship ship = SpawnApproaching(aside.X);
        float beyond = ship.WarpOutDistance - 1000f;
        ship.AI.OrderMoveTo(EnemyPos + aside + Heading * beyond, Heading, AIState.AwaitingOrders);
        float past = PastEnemyAtWarpExit(new Array<Ship> { ship })[0];

        AssertGreaterThan(beyond - past, UsualWarpExit(ship), "an enemy beyond reach of the path must not keep the ship in warp");
    }

    [TestMethod]
    public void AShipLeavingAnEnemyBehindLeavesWarpWhereItAlwaysDid()
    {
        Ship ship = SpawnShip("Vulcan Scout", Player, EnemyPos + Heading * 10_000, Heading);
        ship.SensorRange = 40_000;
        const float stop = 40_000f;
        ship.AI.OrderMoveTo(EnemyPos + Heading * stop, Heading, AIState.AwaitingOrders);
        float past = PastEnemyAtWarpExit(new Array<Ship> { ship })[0];

        AssertGreaterThan(stop - past, UsualWarpExit(ship), "an enemy left far behind must not keep the ship in warp");
    }

    [TestMethod]
    public void AMoveWithoutAStopPastAnEnemyEndsItsLegWhereItAlwaysDid()
    {
        Ship ship = SpawnApproaching(0);
        Vector2 point = EnemyPos + Heading * (ship.WarpOutDistance - 1000f);
        ship.AI.OrderMoveToNoStop(point, Heading, AIState.AwaitingOrders);

        AssertGreaterThan(LegEndShortOf(ship, point), UsualWarpExit(ship), "a move that does not stop there must not keep its warp past the enemy");
    }

    [TestMethod]
    public void AShipPassingAnEnemyOnItsWayToALaterWaypointEndsThatLegWhereItAlwaysDid()
    {
        Ship ship = SpawnApproaching(0);
        Vector2 first = EnemyPos + Heading * (ship.WarpOutDistance - 1000f);
        ship.AI.OrderMoveTo(first, Heading, AIState.AwaitingOrders);
        ship.AI.OrderMoveTo(first + Heading * 30_000, Heading, AIState.AwaitingOrders, MoveOrder.Regular | MoveOrder.AddWayPoint);

        AssertGreaterThan(LegEndShortOf(ship, first), UsualWarpExit(ship), "a waypoint the ship flies on from must not keep its warp past the enemy");
    }
}
