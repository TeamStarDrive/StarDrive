using System.Reflection;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using SDGraphics;
using Ship_Game;
using Ship_Game.AI;
using Ship_Game.AI.Tasks;
using Ship_Game.Fleets;
using Ship_Game.Ships;
using UnitTests.Ships;
using Vector2 = SDGraphics.Vector2;

namespace UnitTests.AITests.Ships;

[TestClass]
public class ChaseLimitTests : StarDriveTest
{
    const string FrigateName = "TEST_Spearhead mk1-a";
    const string ScoutName = "Vulcan Scout";
    const string CarrierName = "Light Carrier mk1-a";
    const string DreadnoughtName = "Dreadnought mk1-a";
    const string PlatformName = "Platform Base mk1-a";
    const string ShuttleName = "Assault Shuttle";
    static readonly FixedSimTime OneSecond = new(1f);
    static readonly FixedSimTime ToTheNextTargetPick = new(1.5f);
    static readonly Vector2 FleetPos = new(300_000f, 300_000f);

    SolarSystem System;
    Planet Guarded;

    public ChaseLimitTests()
    {
        LoadStarterShips(FrigateName, ScoutName, CarrierName, DreadnoughtName, PlatformName, ShuttleName);
        CreateUniverseAndPlayerEmpire();
        UState.Objects.EnableParallelUpdate = false;
    }

    static TestShip Quiet(TestShip ship)
    {
        ship.AI.SetCombatTriggerDelay(1000f);
        return ship;
    }

    TestShip Holding(string name, Ship_Game.Empire owner, Vector2 pos)
    {
        TestShip ship = Quiet(SpawnShip(name, owner, pos));
        ship.AI.OrderHoldPosition(MoveOrder.HoldPosition);
        return ship;
    }

    static void Place(TestShip ship, Vector2 pos)
    {
        ship.Position = pos;
        ship.AI.OrderHoldPosition(MoveOrder.HoldPosition);
    }

    static void Aims(TestShip hostile, Ship at)
    {
        hostile.AI.Target = at;
        hostile.AI.HasPriorityTarget = true;
    }

    Vector2 FromCenter(float distance) => System.Position + new Vector2(distance, 0f);

    // a 150,000 system with a planet the given distance out from its center
    void SystemWithPlanetAt(float planetDistance)
    {
        Vector2 sysPos = new(600_000f, 600_000f);
        Guarded = AddDummyPlanet(sysPos, 1f, 1f, 4f, sysPos + new Vector2(planetDistance, 0f), explored: true);
        System = Guarded.System;
        AssertEqual(150_000f, System.Radius, "setup: the test system's radius");
    }

    // a frigate orbiting a planet, the way Remnant guards orbit
    TestShip Guard(Ship_Game.Empire owner, float planetDistance = 140_000f)
    {
        SystemWithPlanetAt(planetDistance);
        TestShip guard = Quiet(SpawnShip(FrigateName, owner, Guarded.Position + new Vector2(0f, 300f)));
        guard.AI.OrderToOrbit(Guarded, clearOrders: true, MoveOrder.Aggressive);
        AssertGreaterThan(guard.SensorRange, 20_000f, "setup: the guard must see 16,000 out");
        return guard;
    }

    TestShip Carrier(Ship_Game.Empire owner, Vector2 pos, Vector2? fighterAt)
    {
        TestShip carrier = Holding(CarrierName, owner, pos);
        carrier.Carrier.DisableFighterLaunch = true;
        if (fighterAt is { } at)
            carrier.Carrier.AllFighterHangars[0].SetHangarShip(Holding(ScoutName, owner, at));
        return carrier;
    }

    [TestMethod]
    public void AGuardLeavesAShipOutsideItsSystemAlone()
    {
        TestShip guard = Guard(Enemy);
        Holding(ScoutName, Player, FromCenter(155_000f));
        RunObjectsSim(OneSecond);

        AssertEqual(1, guard.AI.PotentialTargets.Length, "setup: the guard sees the scout");
        Assert.IsNull(guard.AI.Target, "a scout outside the guarded system cannot lure the guard out");
        Assert.IsFalse(guard.InCombat);
    }

    [TestMethod]
    public void AGuardTakesOnAShipInsideItsSystem()
    {
        TestShip guard = Guard(Enemy);
        TestShip scout = Holding(ScoutName, Player, FromCenter(130_000f));
        RunObjectsSim(OneSecond);

        Assert.AreSame(scout, guard.AI.Target, "a scout inside the guarded system is fair game");
    }

    [TestMethod]
    public void AGuardWhoseTargetDiesDoesNotTurnToAShipOutsideItsSystem()
    {
        TestShip guard = Guard(Enemy);
        TestShip inside = Holding(ScoutName, Player, FromCenter(130_000f));
        TestShip outside = Holding(ScoutName, Player, FromCenter(155_000f));
        RunObjectsSim(new FixedSimTime(0.5f));
        Assert.AreSame(inside, guard.AI.Target, "setup: the guard fights the scout inside");
        Assert.IsTrue(guard.InCombat, "setup: the guard is in combat");

        inside.Die(inside, cleanupOnly: true);
        RunObjectsSim(new FixedSimTime(0.05f));

        Assert.AreNotSame(outside, guard.AI.Target, "picking the next target mid-fight keeps to the system too");
    }

    // the guard of a planet 1,000 inside the system's edge, and a dreadnought outside the edge at the given part of its reach
    TestShip GuardAndHostileAt(float partOfReach, out TestShip hostile)
    {
        TestShip guard = Guard(Enemy, planetDistance: 149_000f);
        hostile = Holding(DreadnoughtName, Player, guard.Position);
        Place(hostile, guard.Position + new Vector2((hostile.WeaponsMaxRange + guard.Radius) * partOfReach, 0f));
        AssertGreaterThan(hostile.Position.Distance(System.Position), 150_000f, "setup: the dreadnought is outside the system");
        AssertGreaterThan(guard.SensorRange, hostile.Position.Distance(guard.Position) + 1000f, "setup: the guard sees the dreadnought");
        return guard;
    }

    [TestMethod]
    public void AGuardTakesOnAShipFiringOnItFromOutsideItsSystem()
    {
        TestShip guard = GuardAndHostileAt(0.6f, out TestShip sniper);
        Aims(sniper, guard);
        RunObjectsSim(OneSecond);

        Assert.AreSame(sniper, guard.AI.Target, "a ship shooting at the guard from outside the system gets an answer");
    }

    [TestMethod]
    public void AGuardLeavesAShipAimingAtItFromOutOfReachAlone()
    {
        TestShip guard = GuardAndHostileAt(1.2f, out TestShip sniper);
        Aims(sniper, guard);
        RunObjectsSim(OneSecond);

        Assert.IsNull(guard.AI.Target, "a ship outside the system that cannot reach the guard is no lure");
    }

    [TestMethod]
    public void AGuardLeavesAShipInReachAimingElsewhereAlone()
    {
        TestShip guard = GuardAndHostileAt(0.6f, out TestShip sniper);
        Aims(sniper, Holding(ScoutName, Enemy, sniper.Position + new Vector2(3000f, 0f)));
        RunObjectsSim(OneSecond);

        Assert.IsNull(guard.AI.Target, "a ship outside the system busy with another ship is no business of the guard");
    }

    [TestMethod]
    public void AGuardTakesOnACarrierWithFightersInItsSystem()
    {
        TestShip guard = Guard(Enemy);
        TestShip carrier = Carrier(Player, FromCenter(155_000f), fighterAt: FromCenter(-140_000f));
        RunObjectsSim(OneSecond);

        Assert.AreSame(carrier, guard.AI.Target, "a carrier sending fighters into the system from outside it gets an answer");
    }

    [TestMethod]
    public void AGuardLeavesACarrierWithItsFightersOutsideTheSystemAlone()
    {
        TestShip guard = Guard(Enemy);
        Carrier(Player, FromCenter(155_000f), fighterAt: FromCenter(156_000f));
        RunObjectsSim(OneSecond);

        Assert.IsNull(guard.AI.Target, "fighters out but not in the guarded system are no attack on it");
    }

    [TestMethod]
    public void AGuardLeavesACarrierWithItsFightersAboardAlone()
    {
        TestShip guard = Guard(Enemy);
        Carrier(Player, FromCenter(155_000f), fighterAt: null);
        RunObjectsSim(OneSecond);

        Assert.IsNull(guard.AI.Target, "a carrier outside the system with no fighters out is just a ship outside the system");
    }

    [TestMethod]
    public void AShipWaitingInOrbitIsNoGuard()
    {
        TestShip guard = Guard(Enemy);
        guard.AI.CancelTradePlan(Guarded);
        TestShip scout = Holding(ScoutName, Player, FromCenter(155_000f));
        RunObjectsSim(OneSecond);

        Assert.AreSame(scout, guard.AI.Target, "an orbit that only parks the ship does not make it a guard");
    }

    [TestMethod]
    public void APlayerShipOrbitingAPlanetStillChasesBeyondItsSystem()
    {
        TestShip guard = Guard(Player);
        TestShip scout = Holding(ScoutName, Enemy, FromCenter(155_000f));
        RunObjectsSim(OneSecond);

        Assert.AreSame(scout, guard.AI.Target, "the player's own ships are left to the player");
    }

    // a dreadnought firing on the ship from half its reach away, so the ship is in combat there
    void UnderFire(TestShip ship)
    {
        TestShip sniper = Holding(DreadnoughtName, Player, ship.Position);
        Place(sniper, ship.Position + new Vector2(0f, sniper.WeaponsMaxRange * 0.5f));
        Aims(sniper, ship);
    }

    TestShip GuardFightingAt(float distanceFromCenter)
    {
        TestShip guard = Guard(Enemy);
        guard.Position = FromCenter(distanceFromCenter);
        UnderFire(guard);
        return guard;
    }

    [TestMethod]
    public void AGuardPastOneAndAHalfSystemRadiiGoesBackToItsPlanetLocked()
    {
        TestShip guard = GuardFightingAt(240_000f);
        RunObjectsSim(OneSecond);

        Assert.IsFalse(guard.InCombat, "the guard breaks off");
        Assert.IsTrue(guard.AI.HasPriorityOrder, "the way back takes no detours to fight");
        Assert.IsTrue(guard.AI.FindGoal(ShipAI.Plan.MoveToWithin1000, out _), "the guard is on its way back");
        Assert.IsTrue(guard.AI.FindGoal(ShipAI.Plan.Orbit, out ShipAI.ShipGoal orbit) && orbit.TargetPlanet == Guarded, "it orbits its planet again once there");
    }

    [TestMethod]
    public void AGuardWithinOneAndAHalfSystemRadiiKeepsFighting()
    {
        TestShip guard = GuardFightingAt(210_000f);
        RunObjectsSim(OneSecond);

        Assert.IsTrue(guard.InCombat, "the guard may chase a ship firing on it this far");
        Assert.IsFalse(guard.AI.HasPriorityOrder);
    }

    [TestMethod]
    public void AGuardEndingAnOvertakePastItsLimitGoesBackAtOnce()
    {
        TestShip guard = GuardFightingAt(210_000f);
        RunObjectsSim(new FixedSimTime(0.2f));
        Assert.IsTrue(guard.InCombat, "setup: the guard fights within its limit");

        // an overtake has carried it past the limit and ends now, most of a second before the next once-a-second check
        guard.Position = FromCenter(240_000f);
        typeof(ShipAI).GetField("PullBackTimer", BindingFlags.NonPublic | BindingFlags.Instance)!.SetValue(guard.AI, 0.9f);
        guard.AI.OrderMoveToNoStop(guard.Position + guard.Direction * 500f, guard.Direction, AIState.Pursue, MoveOrder.Pursue);
        guard.AI.OrderToOrbit(Guarded, clearOrders: false, MoveOrder.Aggressive);
        RunObjectsSim(new FixedSimTime(0.1f));

        Assert.IsFalse(guard.InCombat, "the guard breaks off as soon as the overtake ends");
        Assert.IsTrue(guard.AI.HasPriorityOrder, "on its locked way back");
        AssertLessThan(guard.AI.MovePosition.Distance(Guarded.Position), 1000f, "heading for its planet, the overtake over");
    }

    // a frigate escorting a platform in the system, the way Remnant ships escort their portal
    TestShip Escort(out TestShip station, string escortedName = PlatformName, float stationDistance = 140_000f)
    {
        SystemWithPlanetAt(140_000f);
        station = Holding(escortedName, Enemy, FromCenter(stationDistance) + new Vector2(0f, 3000f));
        TestShip escort = Quiet(SpawnShip(FrigateName, Enemy, station.Position + new Vector2(0f, 300f)));
        escort.AI.AddEscortGoal(station);
        RunObjectsSim(TestSimStep);
        RunObjectsSim(TestSimStep);
        Assert.AreSame(System, station.System, "setup: the platform is placed in its system");
        return escort;
    }

    [TestMethod]
    public void AnEscortOfAStationLeavesAShipOutsideTheSystemAlone()
    {
        TestShip escort = Escort(out TestShip station);
        Assert.IsTrue(station.IsPlatformOrStation, "setup: the escorted ship is a platform");
        Holding(ScoutName, Player, FromCenter(155_000f) + new Vector2(0f, 3000f));
        RunObjectsSim(ToTheNextTargetPick);

        AssertEqual(1, escort.AI.PotentialTargets.Length, "setup: the escort sees the scout");
        Assert.IsNull(escort.AI.Target, "a scout outside the system cannot lure the station's escort out");
    }

    [TestMethod]
    public void AnEscortOfAStationTakesOnAShipInsideTheSystem()
    {
        TestShip escort = Escort(out _);
        TestShip scout = Holding(ScoutName, Player, FromCenter(130_000f) + new Vector2(0f, 3000f));
        RunObjectsSim(ToTheNextTargetPick);

        Assert.AreSame(scout, escort.AI.Target, "a scout inside the system is fair game for the escort");
    }

    // the escort of a platform 1,000 inside the system's edge, and a dreadnought outside the edge at the given part of its reach of the platform, aiming at it
    TestShip EscortAndStationUnderFire(float partOfReach, out TestShip hostile)
    {
        TestShip escort = Escort(out TestShip station, stationDistance: 149_000f);
        hostile = Holding(DreadnoughtName, Player, station.Position);
        Place(hostile, station.Position + new Vector2((hostile.WeaponsMaxRange + station.Radius) * partOfReach, 0f));
        Aims(hostile, station);
        AssertGreaterThan(hostile.Position.Distance(System.Position), 150_000f, "setup: the dreadnought is outside the system");
        AssertGreaterThan(escort.SensorRange, hostile.Position.Distance(escort.Position) + 1000f, "setup: the escort sees the dreadnought");
        return escort;
    }

    [TestMethod]
    public void AnEscortTakesOnAShipFiringOnItsStationFromOutsideTheSystem()
    {
        TestShip escort = EscortAndStationUnderFire(0.6f, out TestShip sniper);
        RunObjectsSim(ToTheNextTargetPick);

        Assert.AreSame(sniper, escort.AI.Target, "a ship shelling the station from outside the system gets an answer");
    }

    [TestMethod]
    public void AnEscortLeavesAShipAimingAtItsStationFromOutOfReachAlone()
    {
        TestShip escort = EscortAndStationUnderFire(1.2f, out _);
        RunObjectsSim(ToTheNextTargetPick);

        Assert.IsNull(escort.AI.Target, "a ship outside the system that cannot reach the station is no lure");
    }

    [TestMethod]
    public void AnEscortOfAShipThatIsNoStationIsNoGuard()
    {
        TestShip escort = Escort(out TestShip escorted, escortedName: FrigateName);
        Assert.IsFalse(escorted.IsPlatformOrStation, "setup: the escorted ship is a frigate");
        TestShip scout = Holding(ScoutName, Player, FromCenter(155_000f) + new Vector2(0f, 3000f));
        RunObjectsSim(ToTheNextTargetPick);

        Assert.AreSame(scout, escort.AI.Target, "a ship escorted on its way somewhere holds no system");
    }

    [TestMethod]
    public void AFighterEscortingItsStationIsNoGuard()
    {
        TestShip fighter = Escort(out TestShip station);
        fighter.Mothership = station;
        TestShip scout = Holding(ScoutName, Player, FromCenter(155_000f) + new Vector2(0f, 3000f));
        RunObjectsSim(ToTheNextTargetPick);

        Assert.AreSame(scout, fighter.AI.Target, "a station's own fighters are left to its hangars");
    }

    [TestMethod]
    public void AnEscortPastOneAndAHalfSystemRadiiGoesBackToItsStationLocked()
    {
        TestShip escort = Escort(out TestShip station);
        escort.Position = FromCenter(240_000f);
        UnderFire(escort);
        RunObjectsSim(ToTheNextTargetPick);

        Assert.IsFalse(escort.InCombat, "the escort breaks off");
        Assert.IsTrue(escort.AI.HasPriorityOrder, "the way back takes no detours to fight");
        Assert.IsTrue(escort.AI.FindGoal(ShipAI.Plan.MoveToWithin1000, out _), "the escort is on its way back");
        Assert.IsTrue(escort.AI.FindGoal(ShipAI.Plan.Escort, out ShipAI.ShipGoal escortGoal) && escortGoal.TargetShip == station, "it escorts its station again once there");
    }

    TestShip FleetShip(Vector2 pos) => Quiet(SpawnShip(DreadnoughtName, Enemy, pos));

    Fleet MovingFleet(params TestShip[] ships) => MovingFleetTo(new Vector2(-300_000f), ships);

    Fleet MovingFleetTo(Vector2 destination, params TestShip[] ships)
    {
        Fleet fleet = Enemy.CreateFleet(1, null);
        foreach (TestShip ship in ships)
            fleet.AddShip(ship);
        fleet.FleetTask = new MilitaryTask(AddDummyPlanet(new Vector2(-300_000f)), Enemy, MilitaryTaskImportance.Normal);
        fleet.MoveTo(destination, FleetPos.DirectionToTarget(destination), MoveOrder.Aggressive);
        return fleet;
    }

    // just past where the hostile's guns reach the fleet ship, inside the fleet ship's sensors
    static void JustOutOfReach(TestShip hostile, TestShip ship, float sideways = 0f)
    {
        float distance = hostile.WeaponsMaxRange + ship.Radius + 2000f;
        AssertGreaterThan(ship.SensorRange, distance + 1000f, "setup: the fleet ship sees the hostile");
        Place(hostile, ship.Position + new Vector2(sideways, distance));
    }

    static void Weak(TestShip hostile, TestShip ship)
    {
        AssertGreaterThan(ship.GetStrength() * 0.1f, hostile.GetStrength(), "setup: the hostile is under a tenth of the fleet ship");
    }

    TestShip WeakScout(TestShip ship)
    {
        TestShip scout = Holding(ScoutName, Player, ship.Position + new Vector2(0f, 5000f));
        Weak(scout, ship);
        AssertGreaterThan(scout.WeaponsMaxRange, 0f, "setup: the scout is armed");
        return scout;
    }

    [TestMethod]
    public void AMovingFleetShipLeavesALoneWeakShipOutOfItsReachAlone()
    {
        TestShip ship = FleetShip(FleetPos);
        MovingFleet(ship);
        JustOutOfReach(WeakScout(ship), ship);
        RunObjectsSim(OneSecond);

        AssertEqual(1, ship.AI.PotentialTargets.Length, "setup: the fleet ship sees the scout");
        Assert.IsNull(ship.AI.Target, "a lone scout that cannot reach the fleet does not turn it from its task");
        Assert.IsFalse(ship.InCombat);
    }

    [TestMethod]
    public void AFleetShipOnAnAttackMoveOfItsOwnTakesOnALoneWeakShip()
    {
        TestShip ship = FleetShip(FleetPos);
        MovingFleet(ship);
        ship.AI.OrderAttackMoveTo(FleetPos + new Vector2(0f, -50_000f));
        TestShip scout = WeakScout(ship);
        JustOutOfReach(scout, ship);
        RunObjectsSim(OneSecond);

        Assert.AreSame(scout, ship.AI.Target, "a ship the fleet sent at a cluster in its task area clears even a lone weak ship there");
    }

    [TestMethod]
    public void AShipOfAFleetWithoutATaskTakesOnALoneWeakShip()
    {
        TestShip ship = FleetShip(FleetPos);
        Fleet fleet = Enemy.CreateFleet(1, null);
        fleet.AddShip(ship);
        fleet.MoveTo(new Vector2(-300_000f), Vectors.Up, MoveOrder.Aggressive);
        TestShip scout = WeakScout(ship);
        JustOutOfReach(scout, ship);
        RunObjectsSim(OneSecond);

        Assert.AreSame(scout, ship.AI.Target, "only fleets on a task keep to their move");
    }

    [TestMethod]
    public void AMovingFleetShipTakesOnAWeakShipThatIsNotAlone()
    {
        TestShip ship = FleetShip(FleetPos);
        MovingFleet(ship);
        JustOutOfReach(WeakScout(ship), ship);
        JustOutOfReach(WeakScout(ship), ship, sideways: 500f);
        RunObjectsSim(OneSecond);

        Assert.IsNotNull(ship.AI.Target, "two hostiles are not a lone ship");
    }

    [TestMethod]
    public void AMovingFleetShipTakesOnALoneShipStrongEnoughToMatter()
    {
        TestShip ship = FleetShip(FleetPos);
        MovingFleet(ship);
        TestShip rival = Holding(DreadnoughtName, Player, ship.Position + new Vector2(0f, 5000f));
        JustOutOfReach(rival, ship);
        RunObjectsSim(OneSecond);

        Assert.AreSame(rival, ship.AI.Target, "a lone ship as strong as the fleet ship is worth the fight");
    }

    [TestMethod]
    public void AMovingFleetShipTakesOnALoneWeakShipThatCanReachIt()
    {
        TestShip ship = FleetShip(FleetPos);
        MovingFleet(ship);
        TestShip scout = WeakScout(ship);
        Place(scout, ship.Position + new Vector2(0f, scout.WeaponsMaxRange * 0.5f));
        RunObjectsSim(OneSecond);

        Assert.AreSame(scout, ship.AI.Target, "a scout close enough to shoot at the fleet gets shot back");
    }

    [TestMethod]
    public void AMovingFleetShipTakesOnALoneWeakShipAttackingAFleetMate()
    {
        TestShip ship = FleetShip(FleetPos);
        TestShip mate = FleetShip(FleetPos + new Vector2(3000f, 0f));
        MovingFleet(ship, mate);
        TestShip scout = WeakScout(ship);
        JustOutOfReach(scout, ship);
        Aims(scout, mate);
        RunObjectsSim(OneSecond);

        Assert.AreSame(scout, ship.AI.Target, "a weak ship attacking the fleet is the fleet's business");
    }

    [TestMethod]
    public void AMovingFleetShipTakesOnALoneShuttleBoardingAFleetMate()
    {
        TestShip ship = FleetShip(FleetPos);
        TestShip mate = FleetShip(FleetPos + new Vector2(3000f, 0f));
        MovingFleet(ship, mate);
        TestShip shuttle = Quiet(SpawnShip(ShuttleName, Player, ship.Position + new Vector2(0f, 5000f)));
        ResourceManager.CreateTroop("Wyvern", Player).LandOnShip(shuttle);
        shuttle.AI.OrderTroopToBoardShip(mate);
        shuttle.AI.ChangeAIState(AIState.Boarding);
        Weak(shuttle, ship);
        RunObjectsSim(OneSecond);

        Assert.AreSame(shuttle, ship.AI.Target, "a shuttle about to board a fleet ship is stopped, unarmed as it is");
    }

    [TestMethod]
    public void AMovingFleetShipTakesOnALoneWeakShipAttackingAFleetCarriersFighter()
    {
        TestShip ship = FleetShip(FleetPos);
        TestShip carrier = FleetShip(FleetPos + new Vector2(3000f, 0f));
        MovingFleet(ship, carrier);
        TestShip fighter = Quiet(SpawnShip(ScoutName, Enemy, FleetPos + new Vector2(3000f, 2000f)));
        fighter.Mothership = carrier;
        TestShip scout = WeakScout(ship);
        JustOutOfReach(scout, ship);
        Aims(scout, fighter);
        RunObjectsSim(OneSecond);

        Assert.AreSame(scout, ship.AI.Target, "a carrier's fighters fly with its fleet");
    }

    // a fleet ship that wins every overtake roll, and a dreadnought 10,000 off speeding away faster than it can follow
    TestShip FleetShipAfterAFasterRival(bool onTask, out TestShip rival)
    {
        TestShip ship = FleetShip(FleetPos);
        ship.Level = 95;
        if (onTask)
        {
            MovingFleet(ship);
        }
        else
        {
            Fleet fleet = Enemy.CreateFleet(1, null);
            fleet.AddShip(ship);
            fleet.MoveTo(new Vector2(-300_000f), Vectors.Up, MoveOrder.Aggressive);
        }
        rival = Holding(DreadnoughtName, Player, ship.Position + new Vector2(0f, 10_000f));
        RunObjectsSim(TestSimStep);
        rival.Velocity = new Vector2(0f, ship.MaxSTLSpeed * 10f);
        return ship;
    }

    [TestMethod]
    public void AMovingFleetShipDoesNotOvertakeOutOfItsFleetsMove()
    {
        TestShip ship = FleetShipAfterAFasterRival(onTask: true, out TestShip rival);
        ship.AI.MoveToEngageTarget(rival, TestSimStep);

        Assert.AreNotEqual(AIState.Pursue, ship.AI.State, "no overtake");
        Assert.IsTrue(ship.AI.FindGoal(ShipAI.Plan.MoveToWithin1000, out ShipAI.ShipGoal leg) && leg.WantedState == AIState.FormationMoveTo,
                      "the fleet's move, and so its limits, stay with the ship");
    }

    [TestMethod]
    public void AShipOfAFleetWithoutATaskStillOvertakes()
    {
        TestShip ship = FleetShipAfterAFasterRival(onTask: false, out TestShip rival);
        ship.AI.MoveToEngageTarget(rival, TestSimStep);

        AssertEqual(AIState.Pursue, ship.AI.State, "a ship with no fleet move to keep overtakes as before");
    }

    // two fleet ships together and a third out at the given part of their sensor range, fighting a rival there
    TestShip FleetShipFightingAway(float sensorRanges, bool headingPastTheAwayShip = false)
    {
        TestShip first = FleetShip(FleetPos);
        TestShip second = FleetShip(FleetPos + new Vector2(500f, 0f));
        TestShip away = FleetShip(FleetPos + new Vector2(0f, first.SensorRange * sensorRanges));
        if (headingPastTheAwayShip)
            MovingFleetTo(away.Position + new Vector2(0f, first.SensorRange * 0.5f), first, second, away);
        else
            MovingFleet(first, second, away);
        Holding(DreadnoughtName, Player, away.Position + new Vector2(0f, 3000f));
        return away;
    }

    [TestMethod]
    public void AMovingFleetShipFarFromItsPlaceRejoinsTheFleetsMoveLocked()
    {
        TestShip away = FleetShipFightingAway(sensorRanges: 2f);
        RunObjectsSim(OneSecond);

        Assert.IsFalse(away.InCombat, "the ship breaks off");
        Assert.IsTrue(away.AI.HasPriorityOrder, "the way back takes no detours to fight");
        AssertEqual(AIState.FormationMoveTo, away.AI.State, "it rejoins the fleet's move");
        Assert.IsTrue(away.AI.FindGoal(ShipAI.Plan.MoveToWithin1000, out _));
    }

    [TestMethod]
    public void AMovingFleetShipNearItsPlaceKeepsFighting()
    {
        TestShip away = FleetShipFightingAway(sensorRanges: 0.6f);
        RunObjectsSim(OneSecond);

        Assert.IsTrue(away.InCombat, "a ship still within its fleet's sensor range of its place may fight");
        Assert.IsFalse(away.AI.HasPriorityOrder);
    }

    [TestMethod]
    public void AFleetShipAlreadyAtItsSpotAtTheDestinationKeepsFighting()
    {
        TestShip away = FleetShipFightingAway(sensorRanges: 2f, headingPastTheAwayShip: true);
        RunObjectsSim(OneSecond);

        Assert.IsTrue(away.AI.FindGoal(ShipAI.Plan.MoveToWithin1000, out _), "setup: the ship is still on its way to its spot");
        Assert.IsTrue(away.InCombat, "a ship far ahead of its fleet but near its spot at the destination has nothing to rejoin");
        Assert.IsFalse(away.AI.HasPriorityOrder);
    }

    [TestMethod]
    public void AFleetShipOnAnAttackMoveOfItsOwnFarFromItsPlaceKeepsFighting()
    {
        TestShip away = FleetShipFightingAway(sensorRanges: 2f);
        away.AI.OrderAttackMoveTo(away.Position + new Vector2(0f, 3000f));
        RunObjectsSim(OneSecond);

        Assert.IsTrue(away.InCombat, "a ship the fleet sent at a cluster of its own may fight there");
        Assert.IsFalse(away.AI.HasPriorityOrder);
    }

    [TestMethod]
    public void AFleetLooksUpItsLongestSensorRangeAgainOnceThatShipLeaves()
    {
        TestShip near = FleetShip(FleetPos);
        TestShip far = FleetShip(FleetPos + new Vector2(500f, 0f));
        near.SensorRange = 30_000f;
        far.SensorRange = 50_000f;
        Fleet fleet = Enemy.CreateFleet(1, null);
        fleet.AddShip(near);
        fleet.AddShip(far);
        Assert.AreEqual(50_000f, fleet.LongestSensorRange);

        fleet.RemoveShip(far, clearOrders: true);
        Assert.AreEqual(30_000f, fleet.LongestSensorRange, "the ship that left no longer counts");
    }

    [TestMethod]
    public void AFleetLooksUpItsLongestSensorRangeAgainOnceThatShipDies()
    {
        TestShip near = FleetShip(FleetPos);
        TestShip far = FleetShip(FleetPos + new Vector2(500f, 0f));
        near.SensorRange = 30_000f;
        far.SensorRange = 50_000f;
        Fleet fleet = Enemy.CreateFleet(1, null);
        fleet.AddShip(near);
        fleet.AddShip(far);
        Assert.AreEqual(50_000f, fleet.LongestSensorRange);

        far.Die(far, cleanupOnly: true);
        Assert.AreEqual(30_000f, fleet.LongestSensorRange, "a dead ship still listed in the fleet no longer counts");
    }
}
