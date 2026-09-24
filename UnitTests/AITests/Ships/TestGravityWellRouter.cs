using Microsoft.VisualStudio.TestTools.UnitTesting;
using Ship_Game;
using Ship_Game.AI;
using Ship_Game.Gameplay;
using Ship_Game.Ships;
using UnitTests.Ships;
using Vector2 = SDGraphics.Vector2;

namespace UnitTests.AITests.Ships;

// Unit tests for the order-time gravity-well router. Verifies the early-out
// branches (flag off, combat order, remnants) and a couple of happy-path
// detour cases. Geometry-detail tests live in the integration suite.
[TestClass]
public class TestGravityWellRouter : StarDriveTest
{
    readonly TestShip Scout;

    public TestGravityWellRouter()
    {
        LoadStarterShips("Vulcan Scout");
        CreateUniverseAndPlayerEmpire();
        Scout = SpawnShip("Vulcan Scout", Player, Vector2.Zero);
    }

    [TestMethod]
    public void Flag_Off_ProducesNoDetours()
    {
        // Plant a hostile planet right on the route so routing WOULD fire normally.
        Vector2 sysPos = new(300_000f, 0f);
        Planet enemyPlanet = AddDummyPlanetToEmpire(sysPos, Enemy);
        enemyPlanet.System.SetExploredBy(Player); // ensure near-system per-planet path is reachable

        bool wasOn = GlobalStats.RouteAroundGravityWells;
        try
        {
            GlobalStats.RouteAroundGravityWells = false;
            Vector2[] detours = GravityWellRouter.BuildDetours(
                Scout, Scout.Position, new Vector2(600_000f, 0f), MoveOrder.Regular);
            AssertEqual(0, detours.Length, "Flag off must produce 0 detours");
        }
        finally
        {
            GlobalStats.RouteAroundGravityWells = wasOn;
        }
    }

    [TestMethod]
    public void Flag_On_StraightShot_NoBlocker_NoDetours()
    {
        bool wasOn = GlobalStats.RouteAroundGravityWells;
        try
        {
            GlobalStats.RouteAroundGravityWells = true;
            // No planets along the route → straight line is clear → no detours
            Vector2[] detours = GravityWellRouter.BuildDetours(
                Scout, Scout.Position, new Vector2(1_000_000f, 0f), MoveOrder.Regular);
            AssertEqual(0, detours.Length, "Empty route must produce 0 detours");
        }
        finally
        {
            GlobalStats.RouteAroundGravityWells = wasOn;
        }
    }

    [TestMethod]
    public void AggressiveOrder_SkipsRouting_EvenWithBlocker()
    {
        Vector2 sysPos = new(300_000f, 0f);
        Planet enemyPlanet = AddDummyPlanetToEmpire(sysPos, Enemy);
        enemyPlanet.System.SetExploredBy(Player);

        bool wasOn = GlobalStats.RouteAroundGravityWells;
        try
        {
            GlobalStats.RouteAroundGravityWells = true;
            Vector2[] detours = GravityWellRouter.BuildDetours(
                Scout, Scout.Position, new Vector2(600_000f, 0f), MoveOrder.Aggressive);
            AssertEqual(0, detours.Length, "Aggressive moves must skip routing (player wants to engage in the well)");
        }
        finally
        {
            GlobalStats.RouteAroundGravityWells = wasOn;
        }
    }

    [TestMethod]
    public void DestinationInsideWell_SkipsThatWell()
    {
        // Put the destination inside the enemy planet's well — router must NOT try
        // to detour around it (futile: the final leg re-enters the well anyway).
        Vector2 sysPos = new(300_000f, 0f);
        Planet enemyPlanet = AddDummyPlanetToEmpire(sysPos, Enemy);
        enemyPlanet.System.SetExploredBy(Player);

        // Aim directly at the planet's center → destination is well inside the well
        bool wasOn = GlobalStats.RouteAroundGravityWells;
        try
        {
            GlobalStats.RouteAroundGravityWells = true;
            Vector2[] detours = GravityWellRouter.BuildDetours(
                Scout, Scout.Position, enemyPlanet.Position, MoveOrder.Regular);
            AssertEqual(0, detours.Length, "Destination inside the well must not produce a detour around it");
        }
        finally
        {
            GlobalStats.RouteAroundGravityWells = wasOn;
        }
    }

    void CreateClosedEnemyBorder(Vector2 position)
    {
        Player.GetRelations(Enemy).AtWar = false;
        Enemy.GetRelations(Player).AtWar = false;
        AddDummyPlanetToEmpire(position, Enemy);
        Enemy.UpdateContactsAndBorders(Universe, FixedSimTime.Zero);
    }

    [TestMethod]
    public void ClosedBorder_RoutesPeacefulShipAroundInfluence()
    {
        CreateClosedEnemyBorder(new Vector2(300_000f, 0f));
        bool wasOn = GlobalStats.RouteAroundGravityWells;
        try
        {
            // Border enforcement is independent of the optional gravity-well router.
            GlobalStats.RouteAroundGravityWells = false;
            Vector2[] detours = GravityWellRouter.BuildDetours(
                Scout, Scout.Position, new Vector2(600_000f, 0f), MoveOrder.Regular);
            Assert.IsTrue(detours.Length >= 2, "A closed border crossing must receive a detour");
        }
        finally
        {
            GlobalStats.RouteAroundGravityWells = wasOn;
        }
    }

    [TestMethod]
    public void NearbyPlanetBorders_FormOneContinuousTerritory()
    {
        CreateClosedEnemyBorder(new Vector2(300_000f, -100_000f));
        AddDummyPlanetToEmpire(new Vector2(300_000f, 100_000f), Enemy);
        foreach (Planet planet in Enemy.GetPlanets())
            planet.SetExploredBy(Player);
        Assert.IsTrue(System.Threading.SpinWait.SpinUntil(() =>
        {
            Enemy.UpdateContactsAndBorders(Universe, FixedSimTime.Zero);
            var scene = new Ship_Game.Universe.BorderScene(new[] { Enemy, Player });
            Enemy.BorderNodeCache.SetScene(scene, 0);
            Enemy.BorderNodeCache.Update(Enemy);
            return Enemy.BorderNodeCache.OutlineSegments.Length > 0;
        }, 10000), "The background territory and outline builds must finish");

        Assert.IsTrue(Enemy.BorderNodeCache.Connections.Count > 0,
            "Nearby colonies must be joined even when neither node is a projector ship");
        var roundedJoin = Enemy.BorderConnections[0];
        Assert.IsTrue(Enemy.GetBorderConnectionRadiusAt(roundedJoin, 0.5f)
                      > System.Math.Min(roundedJoin.Node1.Radius, roundedJoin.Node2.Radius),
            "A colony join must bulge through its midpoint rather than forming a U-shaped waist");
        Assert.IsTrue(Enemy.BorderNodeCache.OutlineSegments.Length > 0,
            "Joined influence must produce a single outer territory contour");

        bool wasOn = GlobalStats.RouteAroundGravityWells;
        try
        {
            GlobalStats.RouteAroundGravityWells = false;
            Vector2[] detours = GravityWellRouter.BuildDetours(
                Scout, Scout.Position, new Vector2(600_000f, 0f), MoveOrder.Regular);
            Assert.IsTrue(detours.Length >= 2,
                "The closed corridor between colonies must block straight transit");
        }
        finally
        {
            GlobalStats.RouteAroundGravityWells = wasOn;
        }
    }

    [TestMethod]
    public void ColonyPopulationExpandsSystemInfluence()
    {
        Planet colony = AddDummyPlanetToEmpire(new Vector2(300_000f, 0f), Enemy);
        colony.Population = 1_000f;
        float smallColonyRadius = Enemy.GetSystemInfluenceRadius(colony.System);

        colony.Population = 25_000f;
        float matureColonyRadius = Enemy.GetSystemInfluenceRadius(colony.System);

        Assert.IsTrue(matureColonyRadius > smallColonyRadius,
            "A mature colony must project a larger border than a small outpost");
        Assert.IsTrue(smallColonyRadius >= Enemy.GetProjectorRadius() * 3f,
            "Even a new colony must begin with 3x projector influence");
        Assert.IsTrue(matureColonyRadius <= Enemy.GetProjectorRadius() * 4.375f,
            "Population expansion must respect the configured 4.375x cap");
    }

    [TestMethod]
    public void OpenBordersAndWar_AllowStraightTransit()
    {
        CreateClosedEnemyBorder(new Vector2(300_000f, 0f));
        bool wasOn = GlobalStats.RouteAroundGravityWells;
        try
        {
            GlobalStats.RouteAroundGravityWells = false;
            Vector2 destination = new(600_000f, 0f);

            Player.SignTreatyWith(Enemy, TreatyType.OpenBorders);
            AssertEqual(0, GravityWellRouter.BuildDetours(
                Scout, Scout.Position, destination, MoveOrder.Regular).Length,
                "Open borders must allow straight transit");

            Player.BreakTreatyWith(Enemy, TreatyType.OpenBorders);
            Player.GetRelations(Enemy).AtWar = true;
            Enemy.GetRelations(Player).AtWar = true;
            AssertEqual(0, GravityWellRouter.BuildDetours(
                Scout, Scout.Position, destination, MoveOrder.Regular).Length,
                "Declared war must allow entry into enemy territory");
        }
        finally
        {
            GlobalStats.RouteAroundGravityWells = wasOn;
        }
    }

    [TestMethod]
    public void DestinationInsideClosedBorder_StopsAtEdge_ButShipInsideCanExit()
    {
        Vector2 borderCenter = new(300_000f, 0f);
        CreateClosedEnemyBorder(borderCenter);
        Ship_Game.Empire.InfluenceNode border = Enemy.BorderNodes[0];

        Assert.IsTrue(GravityWellRouter.IsInsideClosedBorderClaim(Player, borderCenter),
            "Automatic expansion must reject a target covered by a closed raw border claim");

        Vector2 clamped = GravityWellRouter.ClampToAccessibleBorders(Scout, Scout.Position, borderCenter);
        Assert.AreNotEqual(borderCenter, clamped, "A destination inside closed territory must be clamped");
        Assert.IsTrue(clamped.OutsideRadius(border.Position, border.Radius),
            "The clamped destination must remain outside the authoritative border");

        Vector2 exit = borderCenter + new Vector2(border.Radius * 2f, 0f);
        AssertEqual(0.01f, exit,
            GravityWellRouter.ClampToAccessibleBorders(Scout, borderCenter, exit),
            "A ship already inside closed territory must be allowed to leave");
    }

    [TestMethod]
    public void LegalDestinationBeyondClosedBorder_IsRoutedInsteadOfClamped()
    {
        Vector2 borderCenter = new(300_000f, 0f);
        CreateClosedEnemyBorder(borderCenter);
        float radius = Enemy.BorderNodes[0].Radius;
        Vector2 from = borderCenter - new Vector2(radius * 2f, 0f);
        Vector2 destination = borderCenter + new Vector2(radius * 2f, 0f);

        Assert.IsTrue(GravityWellRouter.IsDestinationAccessible(Scout, destination));
        AssertEqual(0.01f, destination,
            GravityWellRouter.ClampToAccessibleBorders(Scout, from, destination),
            "A legal destination beyond a closed border must not be replaced by the frontier");
        Assert.IsTrue(GravityWellRouter.BuildDetours(
            Scout, from, destination, MoveOrder.Regular).Length >= 2,
            "Transit to a legal destination must route around the closed territory");
    }

    [TestMethod]
    public void DirectPhysicsMovement_CannotCrossClosedBorder()
    {
        Vector2 borderCenter = new(300_000f, 0f);
        CreateClosedEnemyBorder(borderCenter);
        Ship_Game.Empire.InfluenceNode border = Enemy.BorderNodes[0];
        Vector2 from = borderCenter - new Vector2(border.Radius * 2f, 0f);

        Assert.IsTrue(GravityWellRouter.ClampBorderCrossing(
            Scout, from, borderCenter, out Vector2 legalPosition));
        Assert.IsTrue(legalPosition.OutsideRadius(borderCenter, border.Radius),
            "Direct-thrust AI must stop outside a closed border");

        Player.GetRelations(Enemy).AtWar = true;
        Enemy.GetRelations(Player).AtWar = true;
        Assert.IsFalse(GravityWellRouter.ClampBorderCrossing(
            Scout, from, borderCenter, out _),
            "A declared war must permit direct movement across the frontier");
    }

    [TestMethod]
    public void SlowMovement_CannotCreepDeeperIntoClosedBorder()
    {
        Vector2 borderCenter = new(300_000f, 0f);
        CreateClosedEnemyBorder(borderCenter);
        float radius = Enemy.BorderNodes[0].Radius;
        Vector2 inside = borderCenter + new Vector2(radius * 0.5f, 0f);
        Vector2 tinyStepInward = inside - new Vector2(1f, 0f);

        Assert.IsTrue(GravityWellRouter.ClampBorderCrossing(
            Scout, inside, tinyStepInward, out Vector2 stopped));
        AssertEqual(0.01f, inside, stopped,
            "Tiny per-frame movement must not accumulate into inward border creep");
    }

    [TestMethod]
    public void BlockedShip_ReplacesImpossibleOrderWithNearestExit()
    {
        Vector2 borderCenter = new(300_000f, 0f);
        CreateClosedEnemyBorder(borderCenter);
        float radius = Enemy.BorderNodes[0].Radius;
        Scout.Position = borderCenter + new Vector2(radius * 0.5f, 0f);
        Scout.AI.OrderMoveTo(borderCenter, Vectors.Up);
        Assert.IsTrue(Scout.AI.HasWayPoints);

        Scout.AI.OnClosedBorderBlocked();

        Assert.IsTrue(Scout.AI.HasWayPoints,
            "A trapped ship must receive an evacuation order rather than retrying inward movement");
        var evacuation = Scout.AI.CopyWayPoints();
        Assert.IsTrue(GravityWellRouter.IsDestinationAccessible(Scout, evacuation[^1].Position),
            "The automatic evacuation destination must be outside every closed border");
    }

    [TestMethod]
    public void RepeatedMoveOrders_CannotPushShipThroughClosedBorder()
    {
        Vector2 borderCenter = new(300_000f, 0f);
        CreateClosedEnemyBorder(borderCenter);

        Vector2 firstStop = GravityWellRouter.ClampToAccessibleBorders(
            Scout, Scout.Position, borderCenter);
        Vector2 secondStop = GravityWellRouter.ClampToAccessibleBorders(
            Scout, firstStop, borderCenter);

        AssertEqual(0.01f, firstStop, secondStop,
            "Clicking repeatedly must not advance a ship through the frontier");
        Assert.IsTrue(GravityWellRouter.ClampBorderCrossing(
            Scout, firstStop, borderCenter, out Vector2 physicsStop));
        AssertEqual(0.01f, firstStop, physicsStop,
            "The runtime guard must also reject inward movement from the frontier");
    }

    // Covers the explore-leg wrapper (ShipAI.ExploreThrustTarget): it must feed the order-time
    // router with the right ship/origin/destination/order and walk the chain via GetThrustTarget,
    // and it must reuse the cached chain while the leg target is unchanged. The detour GEOMETRY
    // itself is the router's concern (the cases above); here we pin the wrapper's delegation.
    [TestMethod]
    public void ExploreThrustTarget_MatchesRouter_AndIsStablePerLeg()
    {
        Vector2 dest = new(600_000f, 0f);
        AddDummyPlanetToEmpire(new Vector2(300_000f, 0f), Enemy).System.SetExploredBy(Player);

        bool wasOn = GlobalStats.RouteAroundGravityWells;
        try
        {
            foreach (bool routing in new[] { false, true })
            {
                GlobalStats.RouteAroundGravityWells = routing;

                // What the order-time router would return for this leg (Regular = non-combat move).
                Vector2[] detours = GravityWellRouter.BuildDetours(Scout, Scout.Position, dest, MoveOrder.Regular);
                int idx = 0;
                Vector2 expected = GravityWellRouter.GetThrustTarget(detours, ref idx, dest, Scout.Position);

                var leg = new object();
                AssertEqual(0.01f, expected, Scout.AI.TestExploreThrustTarget(leg, dest),
                    $"Explore thrust must match the gravity-well router (routing={routing})");
                // Same leg object → cached chain → identical, stable result (no per-tick jitter).
                AssertEqual(0.01f, expected, Scout.AI.TestExploreThrustTarget(leg, dest),
                    $"Re-querying the same explore leg must be stable (routing={routing})");
            }
        }
        finally
        {
            GlobalStats.RouteAroundGravityWells = wasOn;
        }
    }
}
