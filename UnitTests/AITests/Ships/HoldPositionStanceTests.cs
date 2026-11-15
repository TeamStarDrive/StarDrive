using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using SDGraphics;
using Ship_Game;
using Ship_Game.AI;
using Ship_Game.Empires;
using Ship_Game.Ships;
using UnitTests.Ships;
using Vector2 = SDGraphics.Vector2;

namespace UnitTests.AITests.Ships;

/// <summary>
/// A ship on the Hold Position stance enters combat with an enemy that comes close, even on an attack move, and in combat
/// it stays where it is, turning to face its target, unless the player picked that target.
/// </summary>
[TestClass]
public class HoldPositionStanceTests : StarDriveTest
{
    const string ShipName = "Vulcan Scout";
    static readonly Vector2 HoldAt = new(200_000, 200_000);
    TestShip Us;

    public HoldPositionStanceTests()
    {
        LoadStarterShips(ShipName);
        CreateUniverseAndPlayerEmpire();
        UState.Objects.EnableParallelUpdate = false;
    }

    TestShip SpawnEnemy(Vector2 at)
    {
        TestShip enemy = SpawnShipNoCombatHoldPos(ShipName, Enemy, at);
        enemy.AI.SetCombatTriggerDelay(1000f);
        return enemy;
    }

    void SpawnUsHolding()
    {
        Us = SpawnShip(ShipName, Player, HoldAt);
        Us.SetCombatStance(CombatState.HoldPosition);
        Us.AI.OrderHoldPosition();
        Us.AI.SetCombatTriggerDelay(1000f);
    }

    TestShip HoldUntilAnEnemyComesClose()
    {
        SpawnUsHolding();
        TestShip close = SpawnEnemy(HoldAt + new Vector2(0, 800));
        RunSimWhile((5, fatal: true), () => !Us.InCombat);
        return close;
    }

    // an enemy inside our sensors but farther than a ship fights from where it stands
    TestShip SpawnFarEnemy()
    {
        float distance = Us.SensorRange * 0.8f;
        Assert.IsTrue(distance > 7500f && distance > Us.DesiredCombatRange, "setup: the far enemy is out of reach from where our ship stands");
        TestShip far = SpawnEnemy(HoldAt + new Vector2(distance, 0));
        RunObjectsSim(EmpireConstants.EnemyScanInterval * 2f);
        return far;
    }

    // a ship destroyed in a fight stays in the universe for a while as it breaks up
    static void StartDying(TestShip ship)
    {
        ship.SetDieTimer(10f);
        ship.Dying = true;
    }

    float FarthestFromHoldOver(float seconds)
    {
        float farthest = 0f;
        RunSimWhile((seconds, fatal: false), body: () => farthest = Math.Max(farthest, Us.Position.Distance(HoldAt)));
        return farthest;
    }

    [TestMethod]
    public void InCombatItStaysWhenItsNextTargetIsFarAway()
    {
        TestShip close = HoldUntilAnEnemyComesClose();
        TestShip far = SpawnFarEnemy();
        StartDying(close);

        float farthest = FarthestFromHoldOver(15f);

        AssertEqual(far, Us.AI.Target, "setup: with the close enemy gone, our ship fights the far one");
        Assert.IsTrue(Us.InCombat, "setup: our ship is still in combat");
        Assert.IsTrue(farthest < 300f, $"our ship holds its position, but moved {farthest} toward the far enemy");
    }

    [TestMethod]
    public void InCombatAMoveOrderThatDidNotFinishDoesNotDrawItOut()
    {
        TestShip close = HoldUntilAnEnemyComesClose();
        TestShip far = SpawnFarEnemy();
        Us.AI.SetPriorityOrder(true); // as a move order that ran into a fight before its final turn leaves it
        StartDying(close);

        float farthest = FarthestFromHoldOver(15f);

        AssertEqual(far, Us.AI.Target, "setup: with the close enemy gone, our ship fights the far one");
        Assert.IsTrue(farthest < 300f, $"our ship holds its position, but moved {farthest} toward the far enemy");
    }

    [TestMethod]
    public void ATargetThePlayerPicksDrawsItOut()
    {
        SpawnUsHolding();
        TestShip far = SpawnFarEnemy();
        Us.AI.OrderAttackSpecificTarget(far);

        float farthest = FarthestFromHoldOver(10f);

        AssertEqual(CombatState.HoldPosition, Us.AI.CombatState, "setup: our ship keeps the Hold Position stance");
        Assert.IsTrue(farthest > 1000f, $"our ship flies to the enemy the player told it to attack, but moved only {farthest}");
    }

    [TestMethod]
    public void OnAnAttackMoveItFliesPastAnEnemyThatStaysAway()
    {
        SpawnUsHolding();
        SpawnEnemy(HoldAt + new Vector2(Us.SensorRange * 0.6f, 0));
        RunObjectsSim(EmpireConstants.EnemyScanInterval * 2f);
        Assert.IsTrue(Us.AI.BadGuysNear, "setup: our ship sees the enemy");
        Us.AI.OrderMoveTo(HoldAt + new Vector2(0, 100_000), Vectors.Up, AIState.MoveTo, MoveOrder.Aggressive);

        bool fought = false;
        float farthest = 0f;
        RunSimWhile((15, fatal: false), body: () =>
        {
            fought |= Us.InCombat;
            farthest = Math.Max(farthest, Us.Position.Distance(HoldAt));
        });

        Assert.IsFalse(fought, "on Hold Position an attack move fights only what comes close");
        Assert.IsTrue(farthest > 5000f, $"our ship keeps going, but moved only {farthest}");
    }

    [TestMethod]
    public void OnceThePickedTargetIsDestroyedItHoldsAgain()
    {
        TestShip close = HoldUntilAnEnemyComesClose();
        Us.AI.OrderAttackSpecificTarget(close);
        TestShip far = SpawnFarEnemy();
        StartDying(close);

        float farthest = FarthestFromHoldOver(15f);

        AssertEqual(far, Us.AI.Target, "setup: with the picked enemy destroyed, our ship fights the far one");
        Assert.IsFalse(Us.AI.HasPriorityTarget, "the far enemy is not one the player picked");
        Assert.IsTrue(farthest < 300f, $"our ship holds its position, but moved {farthest} toward the far enemy");
    }
}
