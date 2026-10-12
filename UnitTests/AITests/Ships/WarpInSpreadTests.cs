using System;
using System.Collections.Generic;
using System.Reflection;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using SDUtils;
using Ship_Game;
using Ship_Game.AI;
using Ship_Game.AI.Tasks;
using Ship_Game.Fleets;
using Ship_Game.Ships;
using Ship_Game.Universe;
using UnitTests.Ships;
using Vector2 = SDGraphics.Vector2;
#pragma warning disable CA2213

namespace UnitTests.AITests.Ships;

[TestClass]
public class WarpInSpreadTests : StarDriveTest
{
    static readonly Vector2 TargetPos = new(300_000, 300_000);
    readonly TestShip Target;

    public WarpInSpreadTests()
    {
        LoadStarterShips("Terran-Prototype");
        CreateUniverseAndPlayerEmpire();
        UState.Events.Disabled = true;
        Target = SpawnShipNoCombatHoldPos("Vulcan Scout", Enemy, TargetPos);
        Target.AI.SetCombatTriggerDelay(1000f);
    }

    Array<Ship> SpawnLine(float distance, params float[] lateral) => SpawnLine("Vulcan Scout", distance, lateral);

    Array<Ship> SpawnLine(string shipName, float distance, params float[] lateral)
    {
        var ships = new Array<Ship>();
        foreach (float x in lateral)
            ships.Add(SpawnShip(shipName, Player, TargetPos + new Vector2(x, distance), Vectors.Up));
        return ships;
    }

    Vector2[] WarpInPositions(Array<Ship> ships)
    {
        var wasInWarp = new bool[ships.Count];
        var dropped = new bool[ships.Count];
        var droppedAt = new Vector2[ships.Count];
        RunSimWhile((60, fatal: false), () => Array.Exists(dropped, d => !d), body: () =>
        {
            for (int i = 0; i < ships.Count; ++i)
            {
                if (ships[i].IsInWarp)
                    wasInWarp[i] = true;
                else if (wasInWarp[i] && !dropped[i])
                {
                    dropped[i] = true;
                    droppedAt[i] = ships[i].Position;
                }
            }
        });

        for (int i = 0; i < ships.Count; ++i)
        {
            AssertTrue(dropped[i], $"ship {i} must warp in and drop out (was in warp: {wasInWarp[i]})");
            float distance = droppedAt[i].Distance(TargetPos);
            AssertTrue(distance is > 6000f and < 8000f, $"ship {i} must still drop out about 7500 from its target, not {distance:0}");
        }
        return droppedAt;
    }

    static float SpreadAcross(IEnumerable<Vector2> positions)
    {
        float min = float.MaxValue, max = float.MinValue;
        foreach (Vector2 p in positions)
        {
            min = Math.Min(min, p.X);
            max = Math.Max(max, p.X);
        }
        return max - min;
    }

    void AssertFleetWarpsInSpreadAcrossItsFormation(string shipName)
    {
        Array<Ship> ships = SpawnLine(shipName, 50_000, -100, 0, 100);
        Fleet fleet = Player.CreateFleet(1, null);
        fleet.AddShips(ships);
        ships[0].RelativeFleetOffset = new Vector2(-3000, 0);
        ships[1].RelativeFleetOffset = new Vector2(0, 0);
        ships[2].RelativeFleetOffset = new Vector2(3000, 0);

        foreach (Ship ship in ships)
            ship.AI.OrderAttackSpecificTarget(Target);

        AssertGreaterThan(SpreadAcross(WarpInPositions(ships)), 4000f, "the fleet must drop out spread as wide as its formation");
    }

    [TestMethod]
    public void AFleetWarpsInSpreadAcrossItsFormation()
    {
        AssertFleetWarpsInSpreadAcrossItsFormation("Vulcan Scout");
    }

    [TestMethod]
    public void AFleetOfSlowTurningShipsWarpsInSpreadAcrossItsFormation()
    {
        AssertFleetWarpsInSpreadAcrossItsFormation("Terran-Prototype");
    }

    [TestMethod]
    public void AShipGroupOnAnAttackMoveWarpsInSpreadAcrossItsLine()
    {
        Array<Ship> ships = SpawnLine(20_000, -200, -100, 0, 100, 200);
        Vector2 center = TargetPos + new Vector2(0, 20_000);
        var group = new ShipGroup(ships, center - new Vector2(3000, 0), center + new Vector2(3000, 0), Vectors.Up, Player);
        group.MoveTo(TargetPos, Vectors.Up, MoveOrder.Aggressive);

        AssertGreaterThan(SpreadAcross(WarpInPositions(ships)), 1000f, "the group must drop out spread as wide as its line");
    }

    [TestMethod]
    public void AShipGroupOrderedToAttackAShipWarpsInSpreadAcrossItsLine()
    {
        Array<Ship> ships = SpawnLine(50_000, -3000, 0, 3000);
        foreach (Ship ship in ships)
            ship.AI.OrderAttackSpecificTarget(Target);
        ShipMoveCommands.SpreadGroupAttack(ships, Target);

        AssertGreaterThan(SpreadAcross(WarpInPositions(ships)), 4000f, "the group must drop out spread as wide as it stood");
    }

    [TestMethod]
    public void AShipOrderedAloneAfterAGroupMoveWarpsStraightAtItsTarget()
    {
        Array<Ship> ships = SpawnLine(50_000, -3000, 0, 3000);
        Vector2 center = TargetPos + new Vector2(0, 50_000);
        var group = new ShipGroup(ships, center - new Vector2(3000, 0), center + new Vector2(3000, 0), Vectors.Up, Player);
        group.MoveTo(center + new Vector2(0, -500), Vectors.Up);
        Ship alone = ships[0];
        Assert.AreNotEqual(0f, alone.AI.GroupLateralOffset, "setup: the group move must give the ship a place in the line");

        alone.AI.OrderAttackSpecificTarget(Target);
        Vector2 straight = TargetPos + TargetPos.DirectionToTarget(alone.Position) * 7500f;
        Vector2 droppedAt = WarpInPositions(new Array<Ship> { alone })[0];
        AssertLessThan(droppedAt.Distance(straight), 300f, "a ship ordered alone must warp straight at its target");
    }

    [TestMethod]
    public void AnAIFleetAttackingAClumpSendsEachShipToItsOwnFormationPlace()
    {
        Vector2 clump = new(600_000, 300_000);
        SpawnShipNoCombatHoldPos("Terran-Prototype", Player, clump).AI.SetCombatTriggerDelay(1000f);
        SpawnShipNoCombatHoldPos("Vulcan Scout", Enemy, clump + new Vector2(0, 5000)).AI.SetCombatTriggerDelay(1000f);

        var ships = new Array<Ship>();
        for (int i = 0; i < 3; ++i)
            ships.Add(SpawnShip("Vulcan Scout", Enemy, clump + new Vector2(i * 100, 40_000), Vectors.Up));
        Fleet fleet = Enemy.CreateFleet(1, null);
        fleet.AddShips(ships);
        ships[0].RelativeFleetOffset = new Vector2(-3000, 0);
        ships[1].RelativeFleetOffset = new Vector2(0, 0);
        ships[2].RelativeFleetOffset = new Vector2(3000, 0);

        UState.Objects.Update(new FixedSimTime(2f));
        Enemy.Threats.Update(new FixedSimTime(2f));

        var task = new MilitaryTask(AddDummyPlanet(new Vector2(-300_000)), Enemy, MilitaryTaskImportance.Normal)
        {
            AO = clump,
            AORadius = 20_000,
        };
        MethodInfo attack = typeof(Fleet).GetMethod("AttackEnemyStrengthClumpsInAO", BindingFlags.Instance | BindingFlags.NonPublic,
                                                    null, new[] { typeof(MilitaryTask), typeof(IReadOnlyList<Ship>) }, null);
        AssertTrue((bool)attack.Invoke(fleet, new object[] { task, ships }), "setup: the fleet must find the clump to attack");

        var moveTo = new Array<Vector2>();
        foreach (Ship ship in ships)
        {
            AssertTrue(ship.AI.MovePosition.Distance(clump) < 5000f, $"each ship must attack the clump, not go to {ship.AI.MovePosition}");
            moveTo.Add(ship.AI.MovePosition);
        }
        AssertGreaterThan(SpreadAcross(moveTo), 4000f, "each ship must head for its own place in the formation around the clump");
    }
}
