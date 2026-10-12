using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Ship_Game;
using Ship_Game.AI;
using Ship_Game.Ships;
using UnitTests.Ships;
using Matrix = SDGraphics.Matrix;
using Vector2 = SDGraphics.Vector2;
using BoundingFrustum = Microsoft.Xna.Framework.BoundingFrustum;
#pragma warning disable CA2213

namespace UnitTests.AITests.Ships;

[TestClass]
public class OrbitCombatTests : StarDriveTest
{
    readonly Planet OurWorld;

    public OrbitCombatTests()
    {
        LoadStarterShips("Terran-Prototype");
        CreateUniverseAndPlayerEmpire();
        OurWorld = AddHomeWorldToEmpire(new Vector2(200_000), Player, new Vector2(205_000), explored: true);
    }

    TestShip OrbitOurWorld(Vector2 spawnAt)
    {
        TestShip ship = SpawnShip("Vulcan Scout", Player, spawnAt);
        ship.OrderToOrbit(OurWorld, clearOrders: true);
        return ship;
    }

    float RepairPerSecondAtHalfHealth(TestShip ship)
    {
        foreach (ShipModule m in ship.Modules)
            m.SetHealth(m.ActualMaxHealth * 0.5f, "Test");
        RunObjectsSim(3f);
        return ship.CurrentRepairPerSecond;
    }

    [TestMethod]
    public void AShipThatLeavesOrbitToFightIsNotRepairedByThePlanetUntilItIsBackInOrbit()
    {
        bool combatRepair = GlobalStats.Defaults.UseCombatRepair;
        GlobalStats.Defaults.UseCombatRepair = true; // the harness never advances TotalElapsed, so every ship reads as recently hit
        try
        {
            OurWorld.GeodeticManager.AffectNearbyShips();
            float planetRepair = OurWorld.GeodeticManager.RepairRatePerSecond;
            AssertGreaterThan(planetRepair, 0f, "setup: our world must repair ships");

            TestShip ship = OrbitOurWorld(OurWorld.Position + new Vector2(OurWorld.Radius + 500, 0));
            RunSimWhile((60, fatal: true), () => !ship.IsOrbiting(OurWorld));
            float orbitRepair = RepairPerSecondAtHalfHealth(ship);

            TestShip enemy = SpawnShipNoCombatHoldPos("Terran-Prototype", Enemy, ship.Position + new Vector2(0, 3000));
            enemy.AI.SetCombatTriggerDelay(1000f);
            RunSimWhile((10, fatal: true), () => !ship.InCombat);
            AssertFalse(ship.AI.IsInOrbit, "a ship that left orbit to fight must not count as in orbit");
            AssertEqual(1f, orbitRepair - planetRepair, RepairPerSecondAtHalfHealth(ship),
                        "the planet must not repair a ship that left orbit to fight");

            enemy.Die(null, cleanupOnly: true);
            RunSimWhile((60, fatal: true), () => ship.InCombat || !ship.IsOrbiting(OurWorld));
            AssertTrue(ship.AI.OrderQueue.TryPeekFirst(out ShipAI.ShipGoal goal) && goal.Plan == ShipAI.Plan.Orbit,
                       "the orbit order must outlast the fight");
            AssertEqual(1f, orbitRepair, RepairPerSecondAtHalfHealth(ship),
                        "back in orbit after the fight, the planet repairs it again");
        }
        finally
        {
            GlobalStats.Defaults.UseCombatRepair = combatRepair;
        }
    }

    [TestMethod]
    public void AGuardianThatChasesAFasterShipGoesBackToItsOrbitAfterwards()
    {
        CreateAMinorFaction("Remnant");
        Planet guarded = AddDummyPlanet(new Vector2(400_000));
        TestShip guardian = SpawnShip("Terran-Prototype", Faction, guarded.Position + new Vector2(guarded.Radius + 500, 0));
        guardian.OrderToOrbit(guarded, clearOrders: true, MoveOrder.Aggressive);
        RunSimWhile((60, fatal: true), () => !guardian.IsOrbiting(guarded));

        TestShip runner = SpawnShipNoCombatHoldPos("Vulcan Scout", Player, guardian.Position);
        runner.Position = guardian.Position + runner.Direction * 10_000f;
        runner.Velocity = runner.Direction * guardian.MaxSTLSpeed * 2f;
        guardian.Level = 95;
        guardian.AI.MoveToEngageTarget(runner, TestSimStep);
        guardian.Level = 0;
        AssertEqual(AIState.Pursue, guardian.AI.State, "setup: the guardian must give chase");

        runner.Die(null, cleanupOnly: true);
        RunSimWhile((120, fatal: true), () => !guardian.IsOrbiting(guarded));
        AssertEqual(AIState.Orbit, guardian.AI.State, "after the chase the guardian is back on its orbit order");
    }

    [TestMethod]
    public void AGuardianThatOvertakesAFasterShipFightsOnBeforeGoingBackToItsOrbit()
    {
        CreateAMinorFaction("Remnant");
        Planet guarded = AddDummyPlanet(new Vector2(400_000));
        TestShip guardian = SpawnShip("Terran-Prototype", Faction, guarded.Position + new Vector2(guarded.Radius + 500, 0));
        guardian.OrderToOrbit(guarded, clearOrders: true, MoveOrder.Aggressive);
        RunSimWhile((60, fatal: true), () => !guardian.IsOrbiting(guarded));

        TestShip runner = SpawnShipNoCombatHoldPos("Vulcan Scout", Player, guardian.Position);
        runner.Position = guardian.Position + runner.Direction * 10_000f;
        runner.Velocity = runner.Direction * guardian.MaxSTLSpeed * 2f;
        guardian.Level = 95;
        guardian.AI.MoveToEngageTarget(runner, TestSimStep);
        guardian.Level = 0;
        AssertEqual(AIState.Pursue, guardian.AI.State, "setup: the guardian must give chase");

        RunSimWhile((60, fatal: true), () => guardian.AI.State == AIState.Pursue);
        AssertFalse(guardian.AI.HasPriorityOrder, "once the overtake is done the guardian must be free to fight");
        AssertTrue(guardian.AI.OrderQueue.TryPeekFirst(out ShipAI.ShipGoal goal) && goal.Plan == ShipAI.Plan.DoCombat,
                   $"after the overtake the guardian fights the ship it caught up with (first order: {goal?.Plan})");
        AssertTrue(guardian.AI.FindGoal(ShipAI.Plan.Orbit, out _), "its orbit order waits behind the fight");
    }

    [TestMethod]
    public void AnEscortThatChasesAFasterShipGoesBackToItsEscortAfterwards()
    {
        CreateAMinorFaction("Remnant");
        TestShip escorted = SpawnShip("Terran-Prototype", Faction, new Vector2(400_000));
        TestShip escort = SpawnShip("Terran-Prototype", Faction, escorted.Position + new Vector2(1_000, 0));
        escort.AI.AddEscortGoal(escorted);

        TestShip runner = SpawnShipNoCombatHoldPos("Vulcan Scout", Player, escort.Position);
        runner.Position = escort.Position + runner.Direction * 10_000f;
        runner.Velocity = runner.Direction * escort.MaxSTLSpeed * 2f;
        escort.Level = 95;
        escort.AI.MoveToEngageTarget(runner, TestSimStep);
        escort.Level = 0;
        AssertEqual(AIState.Pursue, escort.AI.State, "setup: the escort must give chase");

        runner.Die(null, cleanupOnly: true);
        RunSimWhile((120, fatal: true), () => escort.AI.State != AIState.Escort);
        Assert.AreSame(escorted, escort.AI.EscortTarget, "after the chase the escort goes back to the ship it escorts");
    }

    [TestMethod]
    public void ShipsSentToOrbitTogetherDoNotStackOnOneOrbitPoint()
    {
        Universe.Frustum = new BoundingFrustum(Matrix.CreateOrthographicOffCenter(0, 1_000_000, 1_000_000, 0, -10_000, 10_000));
        Vector2 start = OurWorld.Position + new Vector2(6000, 0);
        TestShip a = OrbitOurWorld(start);
        TestShip b = OrbitOurWorld(start);
        RunSimWhile((60, fatal: true), () => !a.IsOrbiting(OurWorld) || !b.IsOrbiting(OurWorld));
        AssertTrue(OurWorld.System.InFrustum, "setup: the ships must fly the visible orbit, not the off-screen stop");

        float closest = float.MaxValue;
        RunObjectsSim(10f, () => closest = Math.Min(closest, a.Position.Distance(b.Position)));
        AssertGreaterThan(closest, a.Radius + b.Radius, "two ships orbiting one planet must not sit on top of each other");
    }
}
