using Microsoft.VisualStudio.TestTools.UnitTesting;
using SDUtils;
using Ship_Game;
using Ship_Game.AI;
using Ship_Game.Fleets;
using Ship_Game.Gameplay;
using Ship_Game.Ships;
using Ship_Game.Ships.AI;
using Ship_Game.Universe;
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

    [TestMethod]
    public void ASystemAtTheEndOfTheTripWithKnownRemnantsIsPassedAroundWhole()
    {
        AssertKnownRemnantsAtTheEndOfTheTripArePassedAroundWhole(explored: true);
    }

    [TestMethod]
    public void AnUnexploredSystemAtTheEndOfTheTripWithKnownRemnantsIsPassedAroundWhole()
    {
        AssertKnownRemnantsAtTheEndOfTheTripArePassedAroundWhole(explored: false);
    }

    [TestMethod]
    public void ExploringASystemOnTheWayKeepsTheOrdersQueuedAfterTheMove()
    {
        Planet far = AddDummyPlanet(new Vector2(600_000f, 0f));
        Scout.AI.OrderToOrbit(far, clearOrders: true);
        Assert.IsTrue(Scout.AI.FindGoal(ShipAI.Plan.Orbit, out _), "setup: the scout flies there to orbit");

        Scout.AI.OnSystemNewlyExplored(far.System);

        Assert.IsTrue(Scout.AI.FindGoal(ShipAI.Plan.Orbit, out ShipAI.ShipGoal orbit) && orbit.TargetPlanet == far,
                      "the orbit order waiting behind the move survives the system being explored");
    }

    [TestMethod]
    public void ExploringASystemOnTheWayKeepsTheQueuedWayPoints()
    {
        Scout.AI.OrderMoveTo(new Vector2(300_000f, 0f), Vectors.Up);
        Scout.AI.OrderMoveTo(new Vector2(600_000f, 0f), Vectors.Up, AIState.MoveTo, MoveOrder.Regular | MoveOrder.AddWayPoint);
        AssertEqual(2, Scout.AI.CopyWayPoints().Length, "setup: the scout has two waypoints");

        Scout.AI.OnSystemNewlyExplored(null);

        AssertEqual(2, Scout.AI.CopyWayPoints().Length, "the first waypoint is not skipped when a system on the way is explored");
    }

    [TestMethod]
    public void ExploringASystemOnTheWayStillReroutesAPlainMove()
    {
        Scout.AI.OrderMoveTo(new Vector2(600_000f, 0f), Vectors.Up);
        ShipAI.ShipGoal before = Scout.AI.OrderQueue.PeekFirst;

        Scout.AI.OnSystemNewlyExplored(null);

        Assert.AreNotSame(before, Scout.AI.OrderQueue.PeekFirst, "a plain move is issued again so the router sees the explored system");
    }

    [TestMethod]
    public void ExploringASystemOnTheWayKeepsTheSystemAScoutWasSentToExplore()
    {
        SolarSystem target = AddDummyPlanet(new Vector2(600_000f, 0f)).System;
        Scout.AI.OrderScout(target, null);

        Scout.AI.OnSystemNewlyExplored(null);

        Assert.AreSame(target, Scout.AI.ExplorationTarget, "the scout keeps the system it was sent to explore");
    }

    [TestMethod]
    public void ExploringASystemOnTheWayKeepsAGroupShipsPlaceAcrossItsGroup()
    {
        Scout.AI.OrderMoveTo(new Vector2(600_000f, 0f), Vectors.Up);
        Scout.AI.GroupLateralOffset = 2_000f;

        Scout.AI.OnSystemNewlyExplored(null);

        AssertEqual(1f, 2_000f, Scout.AI.GroupLateralOffset, "a move issued again keeps the ship's place across its group");
    }

    [TestMethod]
    public void ATradePartnersSystemOnTheWayIsCrossedStraight()
    {
        CreateThirdMajorEmpire();
        AddDummyPlanetToEmpire(new Vector2(500_000f, 0f), ThirdMajor);
        Vector2 dest = new(1_000_000f, 0f);
        Assert.IsTrue(Detours(Scout.Position, dest).Length > 0, "setup: a system held by an empire without a treaty is passed around");

        Player.SignTreatyWith(ThirdMajor, TreatyType.Trade);

        AssertEqual(0, Detours(Scout.Position, dest).Length, "a trade partner's territory is friendly for the route, as it is for the ship");
    }

    [TestMethod]
    public void AnAtWarWellInATradePartnersSystemOnTheWayIsPassedAround()
    {
        CreateThirdMajorEmpire();
        Player.SignTreatyWith(ThirdMajor, TreatyType.Trade);
        SolarSystem system = AddDummyPlanetToEmpire(new Vector2(500_000f, 0f), ThirdMajor).System;
        system.SetExploredBy(Player);
        Planet enemy = AddPlanetTo(system, system.Position + new Vector2(0f, 3_000f), Enemy);
        SetWell(enemy, 10_000f);
        Assert.AreEqual(InfluenceStatus.Friendly, UState.Influence.GetInfluenceStatus(Player, system.Position),
                        "setup: the system lies in a trade partner's territory");

        Vector2 dest = new(1_000_000f, 0f);
        Vector2[] detours = Detours(Scout.Position, dest);

        Assert.IsTrue(detours.Length > 0, "the well of a planet at war with us holds our ships even in friendly territory");
        AssertRouteStaysOutOf(Scout.Position, detours, dest, enemy, 10_000f);
    }

    [TestMethod]
    public void AnAtWarWellInOurOwnSystemOnTheWayIsPassedAround()
    {
        SolarSystem system = AddDummyPlanetToEmpire(new Vector2(500_000f, 0f), Player).System;
        system.SetExploredBy(Player);
        Planet enemy = AddPlanetTo(system, system.Position + new Vector2(0f, 3_000f), Enemy);
        SetWell(enemy, 10_000f);
        Assert.IsTrue(UState.Influence.IsInInfluenceOf(Player, system.Position), "setup: the system lies in our territory");

        Vector2 dest = new(1_000_000f, 0f);
        Vector2[] detours = Detours(Scout.Position, dest);

        Assert.IsTrue(detours.Length > 0, "the well of a planet at war with us holds our ships in our own territory");
        AssertRouteStaysOutOf(Scout.Position, detours, dest, enemy, 10_000f);
    }

    [TestMethod]
    public void AKnownRemnantPortalInATradePartnersSystemOnTheWayIsPassedAround()
    {
        CreateThirdMajorEmpire();
        Player.SignTreatyWith(ThirdMajor, TreatyType.Trade);
        SolarSystem system = AddDummyPlanetToEmpire(new Vector2(500_000f, 0f), ThirdMajor).System;
        system.SetExploredBy(Player);
        Vector2 dest = new(1_000_000f, 0f);
        AssertEqual(0, Detours(Scout.Position, dest).Length, "setup: a trade partner's system is crossed");

        UState.RegisterRemnantPortal(system);

        Assert.IsTrue(Detours(Scout.Position, dest).Length > 0, "a known portal's inhibitor holds ships in friendly territory too");
    }

    [TestMethod]
    public void AnAllysWellNearTheEndOfTheTripIsCrossed()
    {
        CreateThirdMajorEmpire();
        Planet allied = AddDummyPlanetToEmpire(new Vector2(300_000f, 0f), ThirdMajor);
        allied.System.SetExploredBy(Player);
        Assert.IsTrue(DetoursPast(allied, 0.5f).Length > 0, "setup: the well of an empire without a treaty is passed around");

        Player.SignTreatyWith(ThirdMajor, TreatyType.Alliance);

        AssertEqual(0, DetoursPast(allied, 0.5f).Length, "an ally's well inside its territory does not hold our ships, so the route crosses it");
    }

    [TestMethod]
    public void AnAtWarWellOutsideFriendlyTerritoryKeepsItsFullSizeWithTheCounterInhibitionTrait()
    {
        Player.data.Traits.EnemyPlanetInhibitionPercentCounter = 0.5f;
        Planet enemy = AddDummyPlanetToEmpire(new Vector2(300_000f, 0f), Enemy);
        enemy.System.SetExploredBy(Player);

        Assert.IsTrue(DetoursPast(enemy, 0.75f).Length > 0, "outside friendly territory the trait does not shrink the well, as in the game");
    }

    [TestMethod]
    public void AnAtWarWellInsideOurTerritoryShrinksByTheCounterInhibitionTrait()
    {
        Planet enemy = AddDummyPlanetToEmpire(new Vector2(300_000f, 0f), Enemy);
        enemy.System.SetExploredBy(Player);
        AddDummyPlanetToEmpire(enemy.System.Position + new Vector2(0f, -20_000f), Player);
        Assert.IsTrue(UState.Influence.IsInInfluenceOf(Player, enemy.Position), "setup: our territory covers the enemy planet");
        Assert.IsTrue(DetoursPast(enemy, 0.75f).Length > 0, "setup: without the trait the route passes around the well");

        Player.data.Traits.EnemyPlanetInhibitionPercentCounter = 0.5f;

        AssertEqual(0, DetoursPast(enemy, 0.75f).Length, "inside our territory the trait halves the enemy well, so the route crosses its outer part");
    }

    [TestMethod]
    public void AFleetShipWhoseShiftedRouteCutsASystemRoutesOnItsOwn()
    {
        SolarSystem system = AddDummyPlanetToEmpire(new Vector2(500_000f, -1_000f), Enemy).System;
        float flank = system.Radius * 0.1f;
        Fleet fleet = Player.CreateFleet(1, null);
        fleet.FinalDirection = Vectors.Right;
        var ships = new Array<Ship>();
        foreach (float across in new[] { -flank, 0f, flank })
        {
            var node = new FleetDataNode { ShipName = "Vulcan Scout", RelativeFleetOffset = new Vector2(across, 0f) };
            fleet.DataNodes.Add(node);
            Ship ship = SpawnShip("Vulcan Scout", Player, new Vector2(10_000f, across));
            fleet.AddExistingShip(ship, node);
            ships.Add(ship);
        }

        WithRouting(() => fleet.MoveTo(new Vector2(1_000_000f, 0f), Vectors.Right));

        Assert.IsTrue(ships[1].AI.CopyWayPoints().Length > 1, "setup: the fleet's route turns around the system");
        foreach (Ship ship in ships)
        {
            Vector2 a = ship.Position;
            foreach (WayPoint wp in ship.AI.CopyWayPoints())
            {
                Assert.IsTrue(ClosestApproach(a, wp.Position, system.Position) > system.Radius,
                              $"the ship {ship.FleetOffset.Y:0} across the fleet stays outside the system on its way to {wp.Position}");
                a = wp.Position;
            }
        }
    }

    static void SetWell(Planet p, float radius)
    {
        typeof(SolarSystemBody).GetProperty(nameof(SolarSystemBody.GravityWellRadius))!.SetValue(p, radius);
    }

    Planet AddPlanetTo(SolarSystem system, Vector2 pos, Ship_Game.Empire owner)
    {
        var p = new Planet(UState.CreateId(), system, pos, fertility: 1f, minerals: 1f, maxPop: 4f);
        p.OrbitalAngle = p.Position.AngleToTarget(system.Position);
        p.TestSetOrbitalRadius(p.Position.Distance(system.Position) + p.Radius);
        system.RingList.Add(new SolarSystem.Ring { Asteroids = false, OrbitalDistance = p.OrbitalRadius, Planet = p });
        system.PlanetList.Add(p);
        p.SetOwner(owner);
        return p;
    }

    Vector2[] DetoursPast(Planet p, float across)
    {
        const float r = 10_000f;
        SetWell(p, r);
        return Detours(p.Position + new Vector2(-3f * r, across * r), p.Position + new Vector2(3f * r, across * r));
    }

    Vector2[] Detours(Vector2 from, Vector2 to)
    {
        Vector2[] detours = null;
        WithRouting(() => detours = GravityWellRouter.BuildDetours(Scout, from, to, MoveOrder.Regular));
        return detours;
    }

    static void WithRouting(System.Action action)
    {
        bool wasOn = GlobalStats.RouteAroundGravityWells;
        try
        {
            GlobalStats.RouteAroundGravityWells = true;
            action();
        }
        finally
        {
            GlobalStats.RouteAroundGravityWells = wasOn;
        }
    }

    static void AssertRouteStaysOutOf(Vector2 from, Vector2[] detours, Vector2 to, Planet well, float radius)
    {
        Vector2 a = from;
        for (int i = 0; i <= detours.Length; i++)
        {
            Vector2 b = i < detours.Length ? detours[i] : to;
            Assert.IsTrue(ClosestApproach(a, b, well.Position) > radius, $"the leg from {a} to {b} stays out of the enemy well");
            a = b;
        }
    }

    static float ClosestApproach(Vector2 a, Vector2 b, Vector2 c)
    {
        Vector2 ab = b - a;
        float t = ab.SqLen() < 1f ? 0f : System.Math.Clamp((c - a).Dot(ab) / ab.SqLen(), 0f, 1f);
        return (a + ab * t).Distance(c);
    }

    void AssertKnownRemnantsAtTheEndOfTheTripArePassedAroundWhole(bool explored)
    {
        CreateAMinorFaction("Remnant");
        UState.Remnants = Faction;
        Vector2 sysPos = new(300_000f, 0f);
        SolarSystem system = AddDummyPlanet(sysPos, 1f, 1f, 4f, sysPos + new Vector2(0f, 2_000f), explored).System;
        Vector2 dest = sysPos + new Vector2(system.Radius + 10_000f, 0f);

        bool wasOn = GlobalStats.RouteAroundGravityWells;
        try
        {
            GlobalStats.RouteAroundGravityWells = true;
            AssertEqual(0, GravityWellRouter.BuildDetours(Scout, Scout.Position, dest, MoveOrder.Regular).Length,
                        "setup: with no known Remnants the route crosses the system between its wells");

            SpawnShip("Vulcan Scout", Faction, sysPos + new Vector2(0f, 2_000f - system.Radius));
            Ship observer = SpawnShip("Vulcan Scout", Player, sysPos + new Vector2(0f, -2_000f - system.Radius));
            UState.Objects.Update(new(time: 2f));
            UState.Objects.Update(new(time: 2f));
            Player.Threats.Update(new(time: 2f));
            observer.QueueTotalRemoval();
            UState.Objects.Update(new(time: 2f));
            Assert.IsTrue(Player.Threats.GetHostileStrengthAt(Faction, sysPos, system.Radius) > 0f, "setup: the player knows of Remnant ships there");
            AssertEqual(explored, system.IsExploredBy(Player), "setup: whether the player explored the system");

            Vector2[] detours = GravityWellRouter.BuildDetours(Scout, Scout.Position, dest, MoveOrder.Regular);
            Assert.IsTrue(detours.Length > 0, "the route must turn off the straight line through the Remnants' system");
            foreach (Vector2 detour in detours)
                Assert.IsTrue(detour.OutsideRadius(sysPos, system.Radius),
                              $"every turn of the route stays outside the system (detour {detour.Distance(sysPos):0} from its centre, radius {system.Radius:0})");
        }
        finally
        {
            GlobalStats.RouteAroundGravityWells = wasOn;
        }
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
