using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using SDGraphics;
using Ship_Game;
using Ship_Game.AI;
using Ship_Game.Ships;
using UnitTests.Ships;
using Vector2 = SDGraphics.Vector2;

namespace UnitTests.AITests.Ships;

/// <summary>
/// Troops rebase only to a planet that is not a war zone; with none, rebase to nearest waits just outside
/// the nearest system the empire holds.
/// </summary>
[TestClass]
public class SafeRebaseTests : StarDriveTest
{
    const string DreadnoughtName = "Dreadnought mk1-a";

    public SafeRebaseTests()
    {
        LoadStarterShips(DreadnoughtName);
        CreateUniverseAndPlayerEmpire();
        LoadStarterShips(Player.data.DefaultTroopShip, Enemy.data.DefaultTroopShip);
        UState.Objects.EnableParallelUpdate = false;
    }

    Planet HomeWorld(Ship_Game.Empire owner, Vector2 sysPos, string name)
    {
        Planet p = AddHomeWorldToEmpire(sysPos, owner, sysPos + new Vector2(5000f), explored: true);
        p.Name = name;
        p.SetExploredBy(owner);
        p.System.SetExploredBy(owner);
        return p;
    }

    // an enemy warship holds position beside the planet, which makes the planet a war zone
    void EnemyBeside(Planet p, Ship_Game.Empire enemy)
    {
        TestShip warship = SpawnShip(DreadnoughtName, enemy, p.Position + new Vector2(0f, 6000f));
        warship.AI.SetCombatTriggerDelay(1000f);
        warship.AI.OrderHoldPosition(MoveOrder.HoldPosition);
        RunObjectsSim(4f);
        Assert.IsTrue(p.MightBeAWarZone(p.Owner), "setup: the enemy beside the planet makes it a war zone");
    }

    Ship TroopShip(Ship_Game.Empire owner, Vector2 pos)
    {
        Ship ship = SpawnShip(owner.data.DefaultTroopShip, owner, pos);
        ResourceManager.CreateTroop("Wyvern", owner).LandOnShip(ship);
        RunObjectsSim(TestSimStep);
        return ship;
    }

    static Planet RebaseTarget(Ship ship) => ship.AI.FindGoal(ShipAI.Plan.Rebase, out ShipAI.ShipGoal goal) ? goal.TargetPlanet : null;

    [TestMethod]
    public void RebaseToNearestSkipsAPlanetUnderAttack()
    {
        Planet near = HomeWorld(Player, new Vector2(300_000f), "Near");
        Planet far = HomeWorld(Player, new Vector2(700_000f), "Far");
        EnemyBeside(near, Enemy);
        Ship troops = TroopShip(Player, near.System.Position + new Vector2(-100_000f, 0f));
        Assert.IsTrue(near.FreeTilesWithRebaseOnTheWay(Player) > 0 && far.FreeTilesWithRebaseOnTheWay(Player) > 0, "setup: both planets have room");

        troops.AI.OrderRebaseToNearest();

        AssertEqual(far, RebaseTarget(troops), "the troops pass the nearer planet under attack for the one that is not");
    }

    [TestMethod]
    public void RebaseToNearestWithEveryColonyUnderAttackWaitsJustOutsideTheNearestSystemWeHold()
    {
        Planet home = HomeWorld(Player, new Vector2(300_000f), "Home");
        EnemyBeside(home, Enemy);
        SolarSystem system = home.System;
        Ship troops = TroopShip(Player, system.Position + new Vector2(system.Radius * 3f, 0f));

        troops.AI.OrderRebaseToNearest();

        Assert.IsNull(RebaseTarget(troops), "no planet is safe to land on");
        ShipAI.ShipGoal last = troops.AI.OrderQueue.ToArray().LastOrDefault(g => g.Plan == ShipAI.Plan.MakeFinalApproach);
        Assert.IsNotNull(last, "the troops move off to wait");
        AssertEqual(1f, system.Radius * 1.05f, last.MovePosition.Distance(system.Position), "they wait just outside the system");
        AssertGreaterThan(system.Position.DirectionToTarget(last.MovePosition).Dot(system.Position.DirectionToTarget(troops.Position)), 0.99f,
                          "on their side of it");
    }

    [TestMethod]
    public void TheAITroopDistributionSkipsAPlanetUnderAttack()
    {
        Planet near = HomeWorld(Enemy, new Vector2(300_000f), "Near");
        Planet far = HomeWorld(Enemy, new Vector2(700_000f), "Far");
        EnemyBeside(near, Player);
        Ship troops = TroopShip(Enemy, near.System.Position + new Vector2(-100_000f, 0f));

        Enemy.AI.DefensiveCoordinator.ManageForcePool();

        AssertEqual(far, RebaseTarget(troops), "the AI sends its idle troops to the planet that is not under attack");
    }
}
