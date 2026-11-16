using Microsoft.VisualStudio.TestTools.UnitTesting;
using Ship_Game;
using Ship_Game.AI;
using Ship_Game.Graphics.Particles;
using Ship_Game.Ships;
using Vector2 = SDGraphics.Vector2;
#pragma warning disable CA2213

namespace UnitTests.Ships;

/// <summary>
/// A ship circling a planet or in warp shows only its thruster glow, with no engine flame or trail behind it. A ship
/// that flies on to an order it had before it went to resupply is out of orbit, and its trail is back.
/// </summary>
[TestClass]
public class OrbitEngineTrailTests : StarDriveTest
{
    readonly Planet OurWorld;
    readonly MiningStationVisualsTests.CountingParticle Flame = new();
    readonly MiningStationVisualsTests.CountingParticle Trail = new();

    public OrbitEngineTrailTests()
    {
        LoadStarterShips("Terran-Prototype");
        CreateUniverseAndPlayerEmpire();
        OurWorld = AddHomeWorldToEmpire(new Vector2(200_000), Player, new Vector2(205_000), explored: true);
        Universe.Particles = new ParticleManager(Content) { ThrustEffect = Flame, EngineTrail = Trail };
    }

    [TestCleanup]
    public override void Cleanup()
    {
        Universe.Particles?.Dispose();
        Universe.Particles = null;
        base.Cleanup();
    }

    TestShip NearOurWorld() => SpawnShip("Terran-Prototype", Player, OurWorld.Position + new Vector2(OurWorld.Radius + 500, 0));

    TestShip InOrbit()
    {
        TestShip ship = NearOurWorld();
        ship.OrderToOrbit(OurWorld, clearOrders: true);
        RunSimWhile((60, fatal: true), () => !ship.IsOrbiting(OurWorld));
        return ship;
    }

    // the engine flame and trail particles of one frame of the ship at full speed
    (int Flame, int Trail) Exhaust(Ship ship)
    {
        bool trails = GlobalStats.EnableEngineTrails;
        try
        {
            GlobalStats.EnableEngineTrails = true;
            Flame.Added = Trail.Added = 0;
            ship.Velocity = ship.Direction * ship.VelocityMax;
            ship.UpdateThrusters(TestSimStep);
            return (Flame.Added, Trail.Added);
        }
        finally
        {
            GlobalStats.EnableEngineTrails = trails;
        }
    }

    [TestMethod]
    public void AShipInOrbitShowsNoEngineFlameOrTrail()
    {
        TestShip ship = InOrbit();

        Assert.AreEqual((0, 0), Exhaust(ship), "a ship circling a planet shows only its thruster glow");
    }

    [TestMethod]
    public void AShipInWarpShowsNoEngineFlameOrTrail()
    {
        TestShip ship = NearOurWorld();
        (int flame, int trail) = Exhaust(ship);
        Assert.IsTrue(flame > 0 && trail > 0, "setup: a ship at full sublight speed shows its engine flame and trail");

        ship.engineState = Ship.MoveState.Warp;

        Assert.AreEqual((0, 0), Exhaust(ship), "a ship in warp shows only its thruster glow");
    }

    [TestMethod]
    public void AShipThatFliesOnToItsOrdersAfterResupplyIsNoLongerInOrbit()
    {
        TestShip ship = NearOurWorld();
        ship.AI.OrderMoveTo(OurWorld.Position + new Vector2(60_000f, 0f), Vectors.Up);
        foreach (ShipModule m in ship.Modules)
            m.SetHealth(m.ActualMaxHealth * 0.5f, "Test");
        ship.AI.OrderResupply(OurWorld, clearOrders: false);
        RunSimWhile((60, fatal: true), () => !ship.IsOrbiting(OurWorld));
        Assert.AreEqual(OurWorld, ship.RepairPlanet, "setup: resupplying in orbit, the planet repairs it");

        foreach (ShipModule m in ship.Modules)
            m.SetHealth(m.ActualMaxHealth, "Test");
        ship.AI.TerminateResupplyIfDone(SupplyType.All, terminateIfEnemiesNear: true);
        Assert.IsTrue(ship.AI.OrderQueue.TryPeekFirst(out ShipAI.ShipGoal goal) && goal.Plan != ShipAI.Plan.Orbit,
                      "setup: done resupplying, it flies on to its earlier order");
        RunObjectsSim(1f);

        Assert.IsFalse(ship.AI.IsInOrbit, "a ship flying on to its orders is no longer in orbit");
        Assert.IsNull(ship.RepairPlanet, "so the planet no longer repairs it");
        AssertGreaterThan(Exhaust(ship).Trail, 0, "and it shows its engine trail again");
    }
}
