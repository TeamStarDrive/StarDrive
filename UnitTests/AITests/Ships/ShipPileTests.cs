using System;
using System.Text;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using SDGraphics;
using Ship_Game;
using Ship_Game.AI;
using Ship_Game.Ships;
using UnitTests.Ships;
using Vector2 = SDGraphics.Vector2;

namespace UnitTests.AITests.Ships;

[TestClass]
public class ShipPileTests : StarDriveTest
{
    static readonly Vector2 PileAt = new(300_000, 300_000);

    public ShipPileTests()
    {
        LoadStarterShips("Terran-Prototype");
        CreateUniverseAndPlayerEmpire();
    }

    // our ships piled on one spot, each already holding where it sits
    TestShip[] PileUp(int count, CombatState stance, MoveOrder order)
    {
        var ships = new TestShip[count];
        for (int i = 0; i < count; ++i)
        {
            Vector2 spot = i == 0 ? PileAt : PileAt + (i * 1.1f).RadiansToDirection() * (5f * i);
            ships[i] = SpawnShip("Terran-Prototype", Player, spot, Vectors.Up);
            ships[i].SetCombatStance(stance);
            ships[i].AI.OrderHoldPosition(spot, Vectors.Up, order);
        }
        return ships;
    }

    TestShip EnemyAt(float distance)
    {
        TestShip enemy = SpawnShipNoCombatHoldPos("Terran-Prototype", Enemy, PileAt + new Vector2(0, -distance));
        enemy.AI.SetCombatTriggerDelay(1000f);
        return enemy;
    }

    // our ships keep shooting the enemy, so it is mended every frame to stay in sight
    static void Mend(TestShip enemy)
    {
        foreach (ShipModule m in enemy.Modules)
            if (m.Health < m.ActualMaxHealth)
                m.SetHealth(m.ActualMaxHealth, "Test");
    }

    void RunWithEnemy(float seconds, TestShip enemy, TestShip[] pile = null)
    {
        int frame = 0;
        RunObjectsSim(seconds, () =>
        {
            Mend(enemy);
            if (pile != null && ++frame % 300 == 0)
                Trace.Append($"\nt={frame / 60} closest={ClosestPair(pile).Ratio:0.00}{Describe(pile)}");
        });
    }

    readonly StringBuilder Trace = new();

    // the closest pair as a share of the distance at which their hulls touch
    static (float Ratio, string Pair) ClosestPair(TestShip[] ships)
    {
        float ratio = float.MaxValue;
        string pair = "";
        for (int a = 0; a < ships.Length; ++a)
            for (int b = a + 1; b < ships.Length; ++b)
            {
                float r = ships[a].Position.Distance(ships[b].Position) / (ships[a].Radius + ships[b].Radius);
                if (r < ratio) { ratio = r; pair = $"{a}-{b}"; }
            }
        return (ratio, pair);
    }

    static string Describe(TestShip[] ships)
    {
        var sb = new StringBuilder();
        for (int i = 0; i < ships.Length; ++i)
        {
            TestShip s = ships[i];
            float toTarget = s.AI.Target != null ? s.Position.Distance(s.AI.Target.Position) : -1f;
            sb.Append($"\n  {i}: off={s.Position.Distance(PileAt):0} v={s.CurrentVelocity:0.0} state={s.AI.State} " +
                      $"combat={s.InCombat} bad={s.AI.BadGuysNear} first={s.AI.OrderQueue.PeekFirst?.Plan} toTarget={toTarget:0}");
        }
        return sb.ToString();
    }

    void AssertApart(TestShip[] ships, string when)
    {
        (float ratio, string pair) = ClosestPair(ships);
        AssertGreaterThan(ratio, 0.95f, $"{when}: no two of our ships may still sit on each other (closest {pair} at {ratio:0.00} of touching){Describe(ships)}{Trace}");
    }

    [TestMethod]
    public void ShipsPiledOnHoldPositionEdgeApartWhileTheEnemyStaysOutsideTheStanceRange()
    {
        TestShip[] pile = PileUp(6, CombatState.HoldPosition, MoveOrder.Regular);
        TestShip enemy = EnemyAt(3000f);
        AssertLessThan(ClosestPair(pile).Ratio, 0.2f, "setup: the ships start piled on one spot");

        RunWithEnemy(20f, enemy, pile);
        AssertTrue(enemy.Active, "setup: the enemy is still there");
        AssertTrue(Array.TrueForAll(pile, s => !s.InCombat), "setup: an enemy 3000 away is outside the Hold Position stance's range");
        AssertApart(pile, "after 20 s with an enemy in sensor range");
    }

    [TestMethod]
    public void ShipsPiledOnHoldPositionEdgeApartInAFight()
    {
        TestShip[] pile = PileUp(6, CombatState.HoldPosition, MoveOrder.Regular);
        TestShip enemy = EnemyAt(700f);

        RunWithEnemy(20f, enemy, pile);
        AssertTrue(enemy.Active, "setup: the enemy is still there");
        AssertTrue(Array.TrueForAll(pile, s => s.InCombat), "setup: an enemy 700 away brings the stance into the fight");
        AssertApart(pile, "after 20 s of fighting");

        enemy.Die(null, cleanupOnly: true);
        RunObjectsSim(20f);
        AssertApart(pile, "20 s after the fight, back on their hold orders");
        foreach (TestShip s in pile)
            AssertLessThan(s.Position.Distance(s.AI.OrderQueue.PeekLast.MovePosition), 100f,
                           $"each holds where the push left it, not back on the pile{Describe(pile)}");
    }

    [TestMethod]
    public void ShipsPiledOnAStandGroundHoldEdgeApartAndStayApart()
    {
        TestShip[] pile = PileUp(6, CombatState.AttackRuns, MoveOrder.StandGround);
        TestShip enemy = EnemyAt(3000f);
        AssertEqual(CombatState.HoldPosition, pile[0].AI.CombatState, "setup: a stand-ground hold puts our ships on Hold Position");

        RunWithEnemy(20f, enemy, pile);
        AssertTrue(enemy.Active, "setup: the enemy is still there");
        AssertApart(pile, "after 20 s with an enemy in sensor range");

        RunWithEnemy(20f, enemy);
        AssertApart(pile, "20 s later");
        foreach (TestShip s in pile)
        {
            AssertLessThan(s.Position.Distance(s.AI.OrderQueue.PeekLast.MovePosition), 100f, $"each holds where the push left it{Describe(pile)}");
            AssertEqual(0f, s.CurrentVelocity, $"and stops there{Describe(pile)}");
        }
    }

    [TestMethod]
    public void ShipsPiledOnMaximumRangeEdgeApartAtTheirFiringDistance()
    {
        TestShip[] pile = PileUp(6, CombatState.Artillery, MoveOrder.Regular);
        TestShip probe = pile[0];
        probe.ShipStatusChange(); // its firing distance for the new stance
        float farthest = probe.DesiredCombatRange - ((int)probe.Radius).RoundUpToMultipleOf(10);
        float nearest = Math.Min(Math.Max(probe.DesiredCombatRange - 500f, probe.DesiredCombatRange * 0.9f), farthest - 150f);
        TestShip enemy = EnemyAt(0.5f * (nearest + farthest) - 0.5f * probe.Radius); // mid-way in the band where the stance stops

        RunWithEnemy(60f, enemy, pile);
        AssertTrue(enemy.Active, "setup: the enemy is still there");
        AssertTrue(Array.TrueForAll(pile, s => s.InCombat), "setup: the ships fight the enemy");
        AssertApart(pile, "after 60 s of fighting at their firing distance");
    }

    [TestMethod]
    public void ShipsOnOtherStancesGoBackToTheirMovePointAfterAFight()
    {
        TestShip[] pile = PileUp(3, CombatState.AttackRuns, MoveOrder.Regular);
        Vector2[] spots = Array.ConvertAll(pile, s => s.Position);
        TestShip enemy = EnemyAt(3000f);

        RunWithEnemy(15f, enemy);
        AssertTrue(Array.TrueForAll(pile, s => s.InCombat), "setup: the ships fight the enemy");
        enemy.Die(null, cleanupOnly: true);
        RunObjectsSim(60f);
        for (int i = 0; i < pile.Length; ++i)
        {
            AssertLessThan(pile[i].AI.OrderQueue.PeekLast.MovePosition.Distance(spots[i]), 50f, $"ship {i} keeps the point it was moved to{Describe(pile)}");
            AssertLessThan(pile[i].Position.Distance(spots[i]), 100f, $"and goes back to it after the fight{Describe(pile)}");
        }
    }

    [TestMethod]
    public void ShipsMovedOntoOneSpotWithEnemiesNearArriveAndEdgeApart()
    {
        TestShip enemy = EnemyAt(3000f);
        var ships = new TestShip[2];
        for (int i = 0; i < ships.Length; ++i)
        {
            ships[i] = SpawnShip("Terran-Prototype", Player, PileAt + new Vector2(i == 0 ? -2000f : 2000f, 0f), Vectors.Up);
            ships[i].AI.OrderMoveTo(PileAt, Vectors.Up, MoveOrder.StandGround);
        }

        RunWithEnemy(60f, enemy);
        foreach (TestShip s in ships)
            AssertEqual(ShipAI.Plan.HoldPosition, s.AI.OrderQueue.PeekFirst?.Plan, $"each finishes its move and holds{Describe(ships)}");
        AssertApart(ships, "60 s after both were moved onto one spot");
    }

    [TestMethod]
    public void ShipsMovedOntoOneSpotHoldWhereAFightThatStartsAsTheyTurnLeavesThem()
    {
        var ships = new TestShip[4];
        for (int i = 0; i < ships.Length; ++i)
        {
            Vector2 spot = PileAt + new Vector2(5f * i, 0f);
            ships[i] = SpawnShip("Terran-Prototype", Player, spot + new Vector2(0f, 400f), Vectors.Up);
            ships[i].SetCombatStance(CombatState.HoldPosition);
            ships[i].AI.OrderMoveTo(spot, Vectors.Down);
        }
        RunSimWhile((30, fatal: true), () => !Array.Exists(ships, s => s.AI.OrderQueue.PeekFirst?.Plan == ShipAI.Plan.RotateToDesiredFacing));
        TestShip enemy = EnemyAt(700f);
        bool turnBehindFight = false;
        RunObjectsSim(20f, () =>
        {
            Mend(enemy);
            foreach (TestShip s in ships)
                turnBehindFight |= s.AI.OrderQueue.PeekFirst?.Plan == ShipAI.Plan.DoCombat && s.AI.FindGoal(ShipAI.Plan.RotateToDesiredFacing, out _);
        });
        AssertTrue(turnBehindFight, "setup: a fight starts while a ship still has its final turn queued");
        AssertApart(ships, "after 20 s of fighting");

        enemy.Die(null, cleanupOnly: true);
        RunObjectsSim(30f);
        AssertApart(ships, "30 s after the fight, with their final turns done");
        foreach (TestShip s in ships)
            AssertLessThan(s.Position.Distance(s.AI.OrderQueue.PeekLast.MovePosition), 100f, $"each holds where the push left it{Describe(ships)}");
    }

    [TestMethod]
    public void TwoShipsOnExactlyOneSpotPartAtOnceAndStopCloseBy()
    {
        TestShip[] pair = PileUp(2, CombatState.HoldPosition, MoveOrder.Regular);
        pair[1].Position = pair[0].Position;
        TestShip enemy = EnemyAt(3000f);

        RunWithEnemy(2f, enemy);
        float gap = pair[0].Position.Distance(pair[1].Position);
        AssertGreaterThan(gap, 100f, "about a second after they see the enemy, the two part along their own fixed directions");

        RunWithEnemy(10f, enemy);
        gap = pair[0].Position.Distance(pair[1].Position);
        float touching = pair[0].Radius + pair[1].Radius;
        AssertGreaterThan(gap, touching, "they end clear of each other");
        AssertLessThan(gap, touching + 110f, "and close by, each stopping within about 50 of where it came clear");
    }

    [TestMethod]
    public void ALoneShipOnHoldPositionStillBrakesToAStopInAFight()
    {
        TestShip ship = PileUp(1, CombatState.HoldPosition, MoveOrder.Regular)[0];
        TestShip enemy = EnemyAt(700f);
        RunSimWhile((5, fatal: true), () => !ship.InCombat);
        ship.Velocity = ship.Direction * 100f;

        RunWithEnemy(5f, enemy);
        AssertTrue(ship.InCombat, "setup: still fighting");
        AssertEqual(0f, ship.CurrentVelocity, "nothing pushes it, so it brakes to a stop");
    }

    [TestMethod]
    public void AShipKnockedOffItsHoldPointStopsInsteadOfDrifting()
    {
        TestShip ship = PileUp(1, CombatState.AttackRuns, MoveOrder.StandGround)[0];
        Vector2 holdAt = ship.Position;
        RunObjectsSim(1f);
        ship.Velocity = ship.Direction * 50f; // pushed along its facing, as its stability thrusters already stop a sideways drift

        float farthest = 0f;
        RunObjectsSim(10f, () => farthest = Math.Max(farthest, ship.Position.Distance(holdAt)));
        AssertEqual(0f, ship.CurrentVelocity, "it brakes to a stop");
        AssertLessThan(farthest, 100f, "before drifting off its hold point");
    }
}
