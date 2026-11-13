using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Ship_Game;
using Ship_Game.AI;
using Ship_Game.AI.ShipMovement;
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

    float HealthGainedAtHalfHealth(TestShip ship)
    {
        foreach (ShipModule m in ship.Modules)
            m.SetHealth(m.ActualMaxHealth * 0.5f, "Test");
        float before = ship.Health;
        RunObjectsSim(3f);
        return ship.Health - before;
    }

    [TestMethod]
    public void AShipThatLeavesOrbitToFightIsNotRepairedByThePlanetUntilItIsBackInOrbit()
    {
        OurWorld.GeodeticManager.AffectNearbyShips();
        AssertGreaterThan(OurWorld.GeodeticManager.RepairRatePerSecond, 0f, "setup: our world must repair ships");

        TestShip ship = OrbitOurWorld(OurWorld.Position + new Vector2(OurWorld.Radius + 500, 0));
        RunSimWhile((60, fatal: true), () => !ship.IsOrbiting(OurWorld));
        float orbitRepair = RepairPerSecondAtHalfHealth(ship);
        AssertTrue(ship.RepairPlanet == OurWorld, "setup: in orbit the planet is its repair planet");

        TestShip enemy = SpawnShipNoCombatHoldPos("Terran-Prototype", Enemy, ship.Position + new Vector2(0, 3000));
        enemy.AI.SetCombatTriggerDelay(1000f);
        RunSimWhile((10, fatal: true), () => !ship.InCombat);
        AssertFalse(ship.AI.IsInOrbit, "a ship that left orbit to fight must not count as in orbit");
        AssertTrue(ship.RepairPlanet == null, "so the planet is not its repair planet");
        AssertEqual(0.01f, 0f, HealthGainedAtHalfHealth(ship), "a ship fighting gets no repair, the planet's nor its own");

        enemy.Die(null, cleanupOnly: true);
        RunSimWhile((60, fatal: true), () => ship.InCombat || !ship.IsOrbiting(OurWorld));
        AssertTrue(ship.AI.OrderQueue.TryPeekFirst(out ShipAI.ShipGoal goal) && goal.Plan == ShipAI.Plan.Orbit,
                   "the orbit order must outlast the fight");
        AssertTrue(ship.RepairPlanet == OurWorld, "back in orbit the planet is its repair planet again");
        AssertEqual(1f, orbitRepair, RepairPerSecondAtHalfHealth(ship),
                    "back in orbit after the fight, the planet repairs it again");
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
        ViewOurSystem();
        Vector2 start = OurWorld.Position + new Vector2(6000, 0);
        TestShip a = OrbitOurWorld(start);
        TestShip b = OrbitOurWorld(start);
        RunSimWhile((60, fatal: true), () => !a.IsOrbiting(OurWorld) || !b.IsOrbiting(OurWorld));
        AssertTrue(OurWorld.System.InFrustum, "setup: the ships must fly the visible orbit, not the off-screen stop");

        float closest = float.MaxValue;
        RunObjectsSim(10f, () => closest = Math.Min(closest, a.Position.Distance(b.Position)));
        AssertGreaterThan(closest, a.Radius + b.Radius, "two ships orbiting one planet must not sit on top of each other");
    }

    // our world's system on screen; test universes start at planet view, close enough that ships fly their visible orbits
    void ViewOurSystem()
    {
        Universe.Frustum = new BoundingFrustum(Matrix.CreateOrthographicOffCenter(0, 1_000_000, 1_000_000, 0, -10_000, 10_000));
    }

    static Vector2 OrbitPosOf(TestShip ship)
    {
        var orbit = (OrbitPlan)typeof(ShipAI).GetField("Orbit", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)!.GetValue(ship.AI);
        return orbit!.OrbitPos;
    }

    // a warship sent to orbit our world, this far out across the planet from its orbit point, or with its orbit point off to its side
    TestShip SendToOrbitFromAcrossOurWorld(float distance, bool orbitPointToTheSide = false)
    {
        TestShip ship = SpawnShip("Terran-Prototype", Player, OurWorld.Position + new Vector2(distance, 0));
        AssertGreaterThan(ship.MaxFTLSpeed, 0f, "setup: a ship that can warp");
        ship.OrderToOrbit(OurWorld, clearOrders: true, MoveOrder.Aggressive);
        AssertTrue(ship.AI.OrderQueue.TryPeekFirst(out ShipAI.ShipGoal goal) && goal.Plan == ShipAI.Plan.Orbit,
                   "setup: a warship goes straight into the orbit plan, with no move to the planet first");
        RunObjectsSim(TestSimStep.FixedTime);
        Vector2 toOrbitPoint = (OrbitPosOf(ship) - OurWorld.Position).Normalized();
        ship.Position = OurWorld.Position + (orbitPointToTheSide ? new Vector2(-toOrbitPoint.Y, toOrbitPoint.X) : -toOrbitPoint) * distance;
        return ship;
    }

    // on its way into orbit: whether the ship spooled outside 7500 of the planet, the jumps it made, and the steps it spooled or warped within 7500
    (bool SpooledFar, int Jumps, int JumpStepsNear) FlyIntoOrbit(TestShip ship)
    {
        bool spooledFar = false, wasInWarp = false;
        int jumps = 0, jumpStepsNear = 0;
        RunSimWhile((240, fatal: true), () => !ship.IsOrbiting(OurWorld), () =>
        {
            AssertTrue(OurWorld.System.InFrustum && UState.IsSystemViewOrCloser, "setup: the ship flies the visible orbit, not the off-screen stop");
            if (ship.IsInWarp && !wasInWarp)
                ++jumps;
            wasInWarp = ship.IsInWarp;
            bool near = ship.Position.InRadius(OurWorld.Position, 7500f);
            spooledFar |= ship.IsSpooling && !near;
            if (ship.IsSpoolingOrInWarp && near)
                ++jumpStepsNear;
        });
        return (spooledFar, jumps, jumpStepsNear);
    }

    [TestMethod]
    public void AShipWarpingInToOrbitAPlanetOnScreenMakesNoJumpOnceNearIt()
    {
        ViewOurSystem();
        TestShip ship = SendToOrbitFromAcrossOurWorld(60_000f);

        (_, int jumps, int jumpStepsNear) = FlyIntoOrbit(ship);
        AssertEqual(1, jumps, "one jump in from afar");
        AssertEqual(0, jumpStepsNear, "out of warp near the planet, it closes in on its orbit at sublight");
    }

    [TestMethod]
    public void AShipSentToOrbitABigPlanetFromNearByJumpsInOnce()
    {
        ViewOurSystem();
        OurWorld.Radius = 2150f; // a big planet, its orbit point well off the line to its center
        TestShip ship = SendToOrbitFromAcrossOurWorld(12_000f, orbitPointToTheSide: true);

        (_, int jumps, int jumpStepsNear) = FlyIntoOrbit(ship);
        AssertEqual(1, jumps, "one jump for the planet, not one after another cut short as each turns for the orbit point");
        AssertEqual(0, jumpStepsNear, "then sublight to its orbit point");
    }

    [TestMethod]
    public void AJumpSpooledJustOutsideTheWarpExitDistanceIsCalledOffOnceInside()
    {
        ViewOurSystem();
        TestShip ship = SendToOrbitFromAcrossOurWorld(7550f);

        (bool spooledFar, _, int jumpStepsNear) = FlyIntoOrbit(ship);
        AssertTrue(spooledFar, "setup: just outside 7500 it spools for a jump for the planet");
        AssertEqual(0, jumpStepsNear, "once inside 7500 the jump is called off, and it closes in at sublight");
    }

    [TestMethod]
    public void AShipOnItsOrbitOrderedToOrbitAgainIsNotSentToThePlanetFirst()
    {
        ViewOurSystem();
        TestShip ship = SpawnShip("Terran-Prototype", Player, OurWorld.Position + new Vector2(50_000, 0));
        float orbit = OrbitPlan.OrbitRadius(OurWorld, ship);
        AssertGreaterThan(orbit, OurWorld.Radius + 1000 * OurWorld.Scale,
                          $"setup: the orbit of a ship of radius {ship.Radius} lies beyond three planet radii ({OurWorld.Radius})");
        ship.OrderToOrbit(OurWorld, clearOrders: true);
        AssertTrue(ship.AI.OrderQueue.TryPeekFirst(out ShipAI.ShipGoal far) && far.Plan != ShipAI.Plan.Orbit,
                   "setup: a ship far off still travels to the planet first");

        ship.Position = OurWorld.Position + new Vector2(orbit, 0);
        ship.OrderToOrbit(OurWorld, clearOrders: true);
        AssertTrue(ship.AI.OrderQueue.TryPeekFirst(out ShipAI.ShipGoal goal) && goal.Plan == ShipAI.Plan.Orbit,
                   $"a ship already on its orbit just orbits, with no trip to the planet's center first (first order: {goal?.Plan})");
    }

    [TestMethod]
    public void ABigShipOrbitsFartherOutAtItsOwnPlanetThanAtOneWhoseGunsWouldFireOnIt()
    {
        ViewOurSystem();
        AssertTrue(Player.IsEmpireAttackable(Enemy), "setup: our world's guns would fire on the enemy's ships");
        float OrbitPointDistance(Ship_Game.Empire loyalty)
        {
            TestShip ship = SpawnShip("Terran-Prototype", loyalty, OurWorld.Position + new Vector2(8000, 0));
            ship.AI.SetCombatTriggerDelay(1000f);
            ship.OrderToOrbit(OurWorld, clearOrders: true, MoveOrder.Aggressive);
            RunObjectsSim(TestSimStep.FixedTime);
            AssertTrue(ship.AI.OrderQueue.TryPeekFirst(out ShipAI.ShipGoal goal) && goal.Plan == ShipAI.Plan.Orbit, "setup: the ship flies its orbit");
            float distance = OrbitPosOf(ship).Distance(OurWorld.Position);
            ship.Die(null, cleanupOnly: true);
            return distance;
        }

        float shipRadius = SpawnShip("Terran-Prototype", Player, new Vector2(0, 0)).Radius;
        AssertEqual(1f, OurWorld.Radius * 2 + shipRadius * 4, OrbitPointDistance(Player),
                    "at its own planet a ship circles four of its sizes out from twice the planet's radius");
        AssertEqual(1f, OurWorld.Radius * 2 + shipRadius, OrbitPointDistance(Enemy),
                    "at a planet whose guns would fire on it a ship keeps the close orbit, within their reach");
    }

    [TestMethod]
    public void OffScreenAShipWhoseOrbitIsWiderThan6000StopsWhereItCircles()
    {
        Universe.Frustum = new BoundingFrustum(Matrix.CreateOrthographicOffCenter(5_000_000, 6_000_000, 6_000_000, 5_000_000, -10_000, 10_000));
        OurWorld.Radius = 2700f; // big enough that the ship's orbit lies beyond 6000
        TestShip ship = SpawnShip("Terran-Prototype", Player, OurWorld.Position);
        float orbit = OrbitPlan.OrbitRadius(OurWorld, ship);
        AssertTrue(orbit > 6500f && orbit < 7500f, $"setup: an orbit of {orbit} lies between the 6000 stop and the warp exit distance");
        ship.Position = OurWorld.Position + new Vector2(orbit - 100f, 0);
        ship.OrderToOrbit(OurWorld, clearOrders: true);

        RunObjectsSim(10f, () => AssertFalse(OurWorld.System.InFrustum && UState.IsSystemViewOrCloser, "setup: our world is off screen"));
        AssertTrue(ship.IsOrbiting(OurWorld), "off screen it counts as in orbit where it stopped");
        AssertEqual(50f, orbit - 100f, ship.Position.Distance(OurWorld.Position), "off screen it stops where it circles, not pulled in to 6000");
    }

    [TestMethod]
    public void AShipNearAPlanetOnScreenClosesInOnItsOrbitWithoutWarping()
    {
        ViewOurSystem();
        TestShip ship = SendToOrbitFromAcrossOurWorld(7000f);
        AssertGreaterThan(ship.Position.Distance(OrbitPosOf(ship)), 7500f, "setup: 7000 from the planet and more than 7500 from its orbit point");

        AssertEqual(0, FlyIntoOrbit(ship).JumpStepsNear, "too near the planet for a jump, it closes in at sublight");
    }
}
