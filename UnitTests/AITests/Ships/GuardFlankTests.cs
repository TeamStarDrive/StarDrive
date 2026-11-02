using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using SDGraphics;
using Ship_Game;
using Ship_Game.AI;
using Ship_Game.AI.CombatTactics;
using Ship_Game.Ships;
using Ship_Game.Universe;
using Ship_Game.Universe.SolarBodies;
using UnitTests.Ships;
using Vector2 = SDGraphics.Vector2;

namespace UnitTests.AITests.Ships;

[TestClass]
public class GuardFlankTests : StarDriveTest
{
    const string FrigateName = "TEST_Spearhead mk1-a";
    const float RivalsApart = 1500f;

    Planet Guarded;
    TestShip[] Guards;
    TestShip[] Rivals;
    Ship_Game.Empire RivalOwner;

    public GuardFlankTests()
    {
        LoadStarterShips(FrigateName, "Subspace Projector");
        CreateUniverseAndPlayerEmpire();
        CreateAMinorFaction("Remnant");
        UState.Objects.EnableParallelUpdate = false;
    }

    static TestShip Quiet(TestShip ship)
    {
        ship.AI.SetCombatTriggerDelay(1000f);
        return ship;
    }

    // guards orbiting a planet, spawned in id order, each seeing the others
    void GuardsAtPlanet(int count, GameDifficulty difficulty, Ship_Game.Empire owner = null, Vector2? fromTheSun = null)
    {
        UState.P.Difficulty = difficulty;
        Vector2 sysPos = new(600_000f, 600_000f);
        Guarded = AddDummyPlanet(sysPos, 1f, 1f, 4f, sysPos + (fromTheSun ?? new Vector2(20_000f, 0f)), explored: true);
        Guards = new TestShip[count];
        for (int i = 0; i < count; ++i)
        {
            Guards[i] = Quiet(SpawnShip(FrigateName, owner ?? Faction, Guarded.Position + new Vector2(i * 300f, 300f)));
            Guards[i].AI.OrderToOrbit(Guarded, clearOrders: true, MoveOrder.Aggressive);
        }
        RunObjectsSim(TestSimStep);
        RunObjectsSim(TestSimStep);
        foreach (TestShip guard in Guards)
            guard.AI.ScanForFriendlies(guard, guard.SensorRange);
        AssertEqual(count - 1, Guards[0].AI.FriendliesNearby.Length, "setup: the guards see each other");
        AssertLessThan(Guards[0].DesiredCombatRange, FlankGeometry.WarpExitRange, "setup: the guards keep the warp exit range");
    }

    Vector2 RivalsCenter(float distance, float sideways = 0f) => Guarded.Position + new Vector2(sideways, -distance);

    // three rivals in a row the given distance out from the planet, facing the given ways (180 faces the planet)
    void RivalsAt(float distance, params float[] facings) => RivalsSidewaysAt(distance, 0f, facings);

    void RivalsSidewaysAt(float distance, float sideways, params float[] facings)
    {
        Rivals = new TestShip[3];
        for (int i = 0; i < 3; ++i)
        {
            Rivals[i] = Quiet(SpawnShip(FrigateName, RivalOwner ?? Player, RivalsCenter(distance, sideways) + new Vector2((i - 1) * RivalsApart, 0f)));
            Rivals[i].RotationDegrees = facings.Length == 0 ? 180f : facings[i];
            Rivals[i].AI.OrderHoldPosition(MoveOrder.HoldPosition);
        }
        AssertGreaterThan(Guards[0].SensorRange, distance + 2000f, "setup: the guards see the rivals");
    }

    void GuardsTakeOnTheRivals()
    {
        RunSimWhile((10, fatal: true), () => Guards.Any(g => !g.InCombat));
        foreach (TestShip guard in Guards)
            Assert.IsTrue(Rivals.Contains(guard.AI.Target), "setup: each guard fights a rival");
    }

    static Vector2 Toward(Vector2 from, Vector2 to) => from.DirectionToTarget(to);

    static FlankGeometry.Keepout[] Keepouts(params Ship[] ships) => ships.Select(s => new FlankGeometry.Keepout(s.Position, 0f)).ToArray();

    void AssertKeepsTheRangeFromEveryRival(Vector2 point, string what)
    {
        foreach (TestShip rival in Rivals)
            AssertGreaterThan(point.Distance(rival.Position), FlankGeometry.WarpExitRange - 1f, $"{what} keeps the warp exit range from every rival");
    }

    [TestMethod]
    public void AtBrutalTheGuardsGoHeadOnLeftRightHeadOnAndRearInIdOrder()
    {
        GuardsAtPlanet(5, GameDifficulty.Brutal);
        RivalsAt(18_000f);
        GuardsTakeOnTheRivals();

        Assert.IsFalse(Guards[0].AI.IsFlanking, "the first comes in head-on");
        Assert.IsTrue(Guards[1].AI.IsFlanking && !Guards[1].AI.HasFlankWaypoint, "the second goes straight to a side");
        Assert.IsTrue(Guards[2].AI.IsFlanking && !Guards[2].AI.HasFlankWaypoint, "the third goes straight to the other side");
        Assert.IsFalse(Guards[3].AI.IsFlanking, "the fourth comes in head-on");
        Assert.IsTrue(Guards[4].AI.IsFlanking && Guards[4].AI.HasFlankWaypoint, "the fifth goes round to the rear");
    }

    [TestMethod]
    public void TheSidePointsLieAcrossTheWayTheRivalsFace()
    {
        GuardsAtPlanet(3, GameDifficulty.Brutal);
        RivalsAt(18_000f);
        GuardsTakeOnTheRivals();

        Vector2 center = RivalsCenter(18_000f);
        AssertGreaterThan(Toward(center, Guards[1].AI.FlankPoint).X, 0.99f, "facing the planet, their left is +X");
        AssertLessThan(Toward(center, Guards[2].AI.FlankPoint).X, -0.99f, "and their right -X");
        AssertKeepsTheRangeFromEveryRival(Guards[1].AI.FlankPoint, "the left point");
        AssertKeepsTheRangeFromEveryRival(Guards[2].AI.FlankPoint, "the right point");
    }

    [TestMethod]
    public void TheRearPointLiesBehindTheRivalsReachedRoundASide()
    {
        GuardsAtPlanet(5, GameDifficulty.Insane);
        RivalsSidewaysAt(18_000f, 6000f);
        GuardsTakeOnTheRivals();

        ShipAI rear = Guards[4].AI;
        Vector2 center = RivalsCenter(18_000f, sideways: 6000f);
        AssertLessThan(Toward(center, rear.FlankPoint).Y, -0.99f, "behind rivals facing the planet is -Y");
        AssertLessThan(Toward(center, rear.FlankWaypoint).X, -0.99f, "the way there goes round the side the guard is on, their right, -X");
        AssertKeepsTheRangeFromEveryRival(rear.FlankPoint, "the rear point");
        Assert.IsTrue(FlankGeometry.IsLegClear(rear.FlankWaypoint, rear.FlankPoint, Keepouts(Rivals), FlankGeometry.WarpExitRange - 1f),
                      "the leg from the side to the rear keeps the warp exit range");
    }

    [TestMethod]
    public void ARearFlankerPassesItsSideWaypointAndStopsFlankingBehindTheRivals()
    {
        GuardsAtPlanet(5, GameDifficulty.Brutal);
        RivalsSidewaysAt(18_000f, 6000f);
        GuardsTakeOnTheRivals();
        TestShip flanker = Guards[4];
        Vector2 waypoint = flanker.AI.FlankWaypoint;
        Vector2 point = flanker.AI.FlankPoint;
        Assert.IsTrue(flanker.AI.HasFlankWaypoint, "setup: the fifth guard goes round to the rear");

        float nearestToWaypoint = float.MaxValue;
        RunSimWhile((60, fatal: true), () => flanker.AI.IsFlanking,
                    () => nearestToWaypoint = System.Math.Min(nearestToWaypoint, flanker.Position.Distance(waypoint)));

        AssertLessThan(nearestToWaypoint, 1501f, "the guard passes its side waypoint");
        AssertLessThan(flanker.Position.Distance(point), 1501f, "and stops flanking behind the rivals");
    }

    [TestMethod]
    public void AtHardOnlyTheSidesAreFlanked()
    {
        GuardsAtPlanet(5, GameDifficulty.Hard);
        RivalsAt(18_000f);
        GuardsTakeOnTheRivals();

        Assert.IsTrue(Guards[1].AI.IsFlanking && Guards[2].AI.IsFlanking, "the sides from Hard");
        Assert.IsFalse(Guards[4].AI.IsFlanking, "the rear only from Brutal: the fifth comes in head-on");
    }

    [TestMethod]
    public void AtNormalEveryGuardComesInHeadOn()
    {
        GuardsAtPlanet(3, GameDifficulty.Normal);
        RivalsAt(18_000f);
        GuardsTakeOnTheRivals();

        Assert.IsFalse(Guards.Any(g => g.AI.IsFlanking), "no flanking at Normal");
    }

    [TestMethod]
    public void AnAIRacesGuardsFlankToo()
    {
        GuardsAtPlanet(3, GameDifficulty.Brutal, owner: Enemy);
        RivalsAt(18_000f);
        GuardsTakeOnTheRivals();

        Assert.IsTrue(Guards[1].AI.IsFlanking && Guards[2].AI.IsFlanking, "an AI race's guards outside fleets flank like the Remnants");
    }

    [TestMethod]
    public void ThePlayersGuardsComeInHeadOn()
    {
        RivalOwner = Enemy;
        GuardsAtPlanet(3, GameDifficulty.Brutal, owner: Player);
        RivalsAt(18_000f);
        GuardsTakeOnTheRivals();

        Assert.IsFalse(Guards.Any(g => g.AI.IsFlanking), "the player's ships are left to the player");
    }

    [TestMethod]
    public void PiratesGuardsComeInHeadOn()
    {
        CreateAMinorFaction("Corsairs");
        Assert.IsTrue(Faction.WeArePirates, "setup: the faction is pirates");
        GuardsAtPlanet(3, GameDifficulty.Brutal);
        RivalsAt(18_000f);
        GuardsTakeOnTheRivals();

        Assert.IsFalse(Guards.Any(g => g.AI.IsFlanking), "only the Remnants among the factions flank");
    }

    // a gravity well round the guarded planet, by default reaching past every flank point
    void GravityWellOverTheFlank(float radius = 50_000f)
    {
        AssertGreaterThan(UState.P.GravityWellRange, 0f, "setup: gravity wells are on");
        typeof(SolarSystemBody).GetProperty(nameof(SolarSystemBody.GravityWellRadius))!.SetValue(Guarded, radius);
    }

    [TestMethod]
    public void AnAIRacesRearGuardDoesNotGoRoundASideInAGravityWell()
    {
        GuardsAtPlanet(5, GameDifficulty.Brutal, owner: Enemy);
        GravityWellOverTheFlank(25_000f);
        RivalsSidewaysAt(18_000f, 6000f);
        AssertGreaterThan(Guarded.Position.Distance(RivalsCenter(18_000f, sideways: 6000f) + new Vector2(0f, -9000f)), 25_000f,
                          "setup: the points behind the rivals lie outside the well, the side ones toward the planet inside it");
        GuardsTakeOnTheRivals();

        Assert.IsFalse(Guards[4].AI.IsFlanking, "the way round to the rear would leave warp in the well, so it comes in head-on");
    }

    [TestMethod]
    public void AnAIRacesGuardDoesNotFlankToAPointInAGravityWell()
    {
        GuardsAtPlanet(2, GameDifficulty.Brutal, owner: Enemy);
        GravityWellOverTheFlank();
        RivalsAt(18_000f);
        GuardsTakeOnTheRivals();

        Assert.IsFalse(Guards[1].AI.IsFlanking, "a ship could not leave warp where the well holds it, so it comes in head-on");
    }

    [TestMethod]
    public void AnAIRacesGuardFlanksToAPointInAGravityWellInsideItsBorders()
    {
        GuardsAtPlanet(2, GameDifficulty.Brutal, owner: Enemy);
        GravityWellOverTheFlank();
        RivalsAt(18_000f);
        SpawnShip("Subspace Projector", Enemy, RivalsCenter(18_000f) + new Vector2(12_000f, 0f));
        GuardsTakeOnTheRivals();
        AssertEqual(InfluenceStatus.Friendly, UState.Influence.GetInfluenceStatus(Enemy, Guards[1].AI.FlankPoint), "setup: its projector's borders cover the point");

        Assert.IsTrue(Guards[1].AI.IsFlanking, "an unowned planet's well holds no ship inside its own borders");
    }

    [TestMethod]
    public void ARemnantGuardFlanksToAPointInAGravityWell()
    {
        GuardsAtPlanet(2, GameDifficulty.Brutal);
        GravityWellOverTheFlank();
        RivalsAt(18_000f);
        GuardsTakeOnTheRivals();

        Assert.IsTrue(Guards[1].AI.IsFlanking, "the Remnants pay gravity wells no heed");
    }

    [TestMethod]
    public void ARemnantShipEscortingAShipThatIsNoStationComesInHeadOn()
    {
        GuardsAtPlanet(2, GameDifficulty.Brutal);
        Guards[1].AI.AddEscortGoal(Guards[0]);
        RivalsAt(18_000f);
        GuardsTakeOnTheRivals();

        Assert.IsFalse(Guards[1].AI.IsFlanking, "only guards flank, and an escort of a ship on the move guards nothing");
    }

    [TestMethod]
    public void TheClearDistanceIsTheNearestPointOutAlongTheWayThatKeepsTheRangeFromEveryShip()
    {
        Vector2 center = new(300_000f, 300_000f);
        FlankGeometry.Keepout onTheWay = new(center + new Vector2(1500f, 0f), 0f);
        FlankGeometry.Keepout beside = new(center + new Vector2(0f, 3000f), 0f);
        FlankGeometry.Keepout behind = new(center + new Vector2(-9000f, 0f), 0f);
        Vector2 right = new(1f, 0f);

        AssertEqual(0.5f, 9000f, FlankGeometry.ClearDistance(center, right, new[] { onTheWay }, 7500f), "7,500 past a ship 1,500 out on the way");
        AssertEqual(0.5f, 6873.9f, FlankGeometry.ClearDistance(center, right, new[] { beside }, 7500f), "where the way leaves the circle round a ship 3,000 beside it");
        AssertEqual(0.5f, 0f, FlankGeometry.ClearDistance(center, right, new[] { behind }, 7500f), "a ship behind the center pushes nothing out");
        AssertEqual(0.5f, 9000f, FlankGeometry.ClearDistance(center, right, new[] { onTheWay, beside, behind }, 7500f), "past ranges that overlap");

        FlankGeometry.Keepout nextOut = new(center + new Vector2(12_000f, 0f), 0f);
        FlankGeometry.Keepout farOut = new(center + new Vector2(30_000f, 0f), 0f);
        AssertEqual(0.5f, 19_500f, FlankGeometry.ClearDistance(center, right, new[] { onTheWay, nextOut }, 7500f), "a range overlapping the first pushes on past it");
        AssertEqual(0.5f, 9000f, FlankGeometry.ClearDistance(center, right, new[] { farOut, onTheWay }, 7500f), "a ship far out on the way leaves the gap before it");

        FlankGeometry.Keepout spreadGroup = new(center + new Vector2(1500f, 0f), 2000f);
        AssertEqual(0.5f, 11_000f, FlankGeometry.ClearDistance(center, right, new[] { spreadGroup }, 7500f), "a group's spread adds to the range");
    }

    [TestMethod]
    public void TheSidesFollowTheWayMostOfTheRivalsFace()
    {
        GuardsAtPlanet(3, GameDifficulty.Brutal);
        RivalsAt(18_000f, 90f, 90f, 90f);
        GuardsTakeOnTheRivals();

        AssertGreaterThan(Toward(RivalsCenter(18_000f), Guards[2].AI.FlankPoint).Y, 0.99f, "rivals facing +X have their right at +Y");
    }

    [TestMethod]
    public void RivalsFacingEveryWayHaveTheirFrontTowardTheGuard()
    {
        GuardsAtPlanet(3, GameDifficulty.Brutal);
        RivalsAt(18_000f, 90f, 270f, 0f);
        GuardsTakeOnTheRivals();

        AssertLessThan(Toward(RivalsCenter(18_000f), Guards[2].AI.FlankPoint).X, -0.9f, "facing the guard, the right is -X");
    }

    [TestMethod]
    public void GuardsTooCloseForAClearWayToTheSideOrRearComeInHeadOn()
    {
        GuardsAtPlanet(5, GameDifficulty.Brutal);
        RivalsAt(8_000f);
        var flankedOnEntry = new bool?[5];
        RunSimWhile((10, fatal: true), () => flankedOnEntry[1] == null || flankedOnEntry[4] == null, () =>
        {
            foreach (int i in new[] { 1, 4 })
            {
                if (flankedOnEntry[i] != null || !Guards[i].InCombat)
                    continue;
                AssertGreaterThan(Guards[i].Position.Distance(Guards[i].AI.Target.Position), FlankGeometry.WarpExitRange, "setup: the guard enters combat out of reach");
                flankedOnEntry[i] = Guards[i].AI.IsFlanking;
            }
        });

        Assert.IsFalse(flankedOnEntry[1]!.Value, "every way to the side passes within the warp exit range");
        Assert.IsFalse(flankedOnEntry[4]!.Value, "and every way round to the rear too");
    }

    [TestMethod]
    public void FightersDoNotPushTheFlankPointOut()
    {
        GuardsAtPlanet(2, GameDifficulty.Brutal);
        RivalsAt(18_000f);
        TestShip fighter = Quiet(SpawnShip(FrigateName, Player, RivalsCenter(18_000f) + new Vector2(10_000f, 0f)));
        fighter.AI.OrderHoldPosition(MoveOrder.HoldPosition);
        fighter.Mothership = Rivals[0];
        RunSimWhile((10, fatal: true), () => Guards.Any(g => !g.InCombat));
        Assert.IsTrue(Guards[1].AI.PotentialTargets.Contains(fighter), "setup: the guard sees the fighter");

        Guards[1].AI.Target = Rivals[1];
        Guards[1].AI.PlanFlank();

        Assert.IsTrue(Guards[1].AI.IsFlanking, "setup: the guard flanks to the left, +X");
        AssertLessThan(Guards[1].AI.FlankPoint.Distance(fighter.Position), FlankGeometry.WarpExitRange, "the fighter at the left side is left out");
    }

    // the second of two guards, planning again against the middle rival with one more rival at the given offset from the row
    TestShip LeftFlankerWithAnotherRivalAt(Vector2 offset, out TestShip other)
    {
        GuardsAtPlanet(2, GameDifficulty.Brutal);
        RivalsAt(18_000f);
        other = Quiet(SpawnShip(FrigateName, Player, RivalsCenter(18_000f) + offset));
        other.RotationDegrees = 180f;
        other.AI.OrderHoldPosition(MoveOrder.HoldPosition);
        RunSimWhile((10, fatal: true), () => Guards.Any(g => !g.InCombat));
        Assert.IsTrue(Guards[1].AI.PotentialTargets.Contains(other), "setup: the guard sees the other rival");
        Guards[1].AI.Target = Rivals[1];
        return Guards[1];
    }

    [TestMethod]
    public void ARivalInWarpDoesNotPushTheFlankPointOut()
    {
        TestShip flanker = LeftFlankerWithAnotherRivalAt(new Vector2(10_000f, 0f), out TestShip warping);
        warping.engineState = Ship.MoveState.Warp;
        flanker.AI.PlanFlank();

        Assert.IsTrue(flanker.AI.IsFlanking, "setup: the guard flanks to the left, +X");
        AssertLessThan(flanker.AI.FlankPoint.Distance(warping.Position), FlankGeometry.WarpExitRange, "a ship passing in warp is left out");
    }

    [TestMethod]
    public void ARivalAwayFromTheTargetIsNoPartOfItsGroup()
    {
        TestShip flanker = LeftFlankerWithAnotherRivalAt(new Vector2(-8000f, 8000f), out _);
        flanker.AI.PlanFlank();

        Assert.IsTrue(flanker.AI.IsFlanking, "setup: the guard flanks to the left, +X");
        AssertGreaterThan(Toward(RivalsCenter(18_000f), flanker.AI.FlankPoint).X, 0.99f, "the point lies straight out from the row round the target");
    }

    static void TurnToward(TestShip[] ships, Vector2 position)
    {
        foreach (TestShip ship in ships)
        {
            ship.Rotation = ship.Position.RadiansToTarget(position);
            ship.AI.OrderHoldPosition(MoveOrder.HoldPosition);
        }
    }

    [TestMethod]
    public void AFlankerThatArrivedClosesInOnANewTargetOfTheSameGroup()
    {
        GuardsAtPlanet(2, GameDifficulty.Brutal);
        RivalsAt(18_000f);
        GuardsTakeOnTheRivals();
        TestShip flanker = Guards[1];
        RunSimWhile((60, fatal: true), () => flanker.AI.IsFlanking);
        TurnToward(Rivals, flanker.Position);

        Ship lost = flanker.AI.Target;
        lost.Die(lost, cleanupOnly: true);
        RunObjectsSim(TestSimStep);

        Assert.IsTrue(Rivals.Contains(flanker.AI.Target) && flanker.AI.Target != lost, "setup: the guard turns to another rival of the row");
        Assert.IsFalse(flanker.AI.IsFlanking, "facing it now, the row's left has moved round, but the guard closes in instead of circling");
    }

    [TestMethod]
    public void AFlankerWhoseTargetDiesOnTheWayKeepsItsLeg()
    {
        GuardsAtPlanet(2, GameDifficulty.Brutal);
        RivalsAt(18_000f);
        GuardsTakeOnTheRivals();
        TestShip flanker = Guards[1];
        Vector2 point = flanker.AI.FlankPoint;
        TurnToward(Rivals, Rivals[1].Position + new Vector2(10_000f, 0f));

        Ship lost = flanker.AI.Target;
        lost.Die(lost, cleanupOnly: true);
        RunObjectsSim(TestSimStep);

        Assert.IsTrue(Rivals.Contains(flanker.AI.Target) && flanker.AI.Target != lost, "setup: the guard turns to another rival of the row");
        Assert.IsTrue(flanker.AI.IsFlanking, "the leg goes on");
        AssertEqual(1f, point, flanker.AI.FlankPoint, "to the same point, though the row now faces another way");
    }

    // a neutron star whose radiation covers every left flank point of rivals 18,000 out from a planet beside it
    void NeutronStarOverTheLeftFlank(Ship_Game.Empire owner)
    {
        GuardsAtPlanet(2, GameDifficulty.Brutal, owner, fromTheSun: new Vector2(-22_500f, 10_000f));
        Guarded.System.Sun = SunType.FindSun("star_neutron");
        RivalsAt(18_000f);
        SolarSystem system = Guarded.System;
        Assert.IsFalse(system.InSafeDistanceFromRadiation(RivalsCenter(18_000f) + new Vector2(9000f, 0f))
                       || system.InSafeDistanceFromRadiation(RivalsCenter(18_000f) + new Vector2(18_000f, 0f)), "setup: the radiation reaches the left flank points");
        Assert.IsTrue(system.InSafeDistanceFromRadiation(Guarded.Position) && system.InSafeDistanceFromRadiation(RivalsCenter(18_000f)), "setup: but not the guards or the rivals");
    }

    [TestMethod]
    public void AnAIRacesGuardDoesNotFlankIntoAStarsRadiation()
    {
        NeutronStarOverTheLeftFlank(Enemy);
        GuardsTakeOnTheRivals();

        Assert.IsFalse(Guards[1].AI.IsFlanking, "the radiation would hurt it there, so it comes in head-on");
    }

    [TestMethod]
    public void AGuardImmuneToRadiationFlanksIntoAStarsRadiation()
    {
        NeutronStarOverTheLeftFlank(Faction);
        foreach (TestShip guard in Guards)
            guard.IsGuardian = true;
        GuardsTakeOnTheRivals();

        Assert.IsTrue(Guards[1].IsImmuneToStarRadiation, "setup: Remnant guardians are immune");
        Assert.IsTrue(Guards[1].AI.IsFlanking, "an immune guard takes the left flank all the same");
    }

    [TestMethod]
    public void AFlankerFliesToItsPointAndStopsFlankingThere()
    {
        GuardsAtPlanet(2, GameDifficulty.Brutal);
        RivalsAt(18_000f);
        GuardsTakeOnTheRivals();
        TestShip flanker = Guards[1];
        Vector2 point = flanker.AI.FlankPoint;

        RunSimWhile((60, fatal: true), () => flanker.AI.IsFlanking);

        AssertLessThan(flanker.Position.Distance(point), 1501f, "the flank ends on reaching its point");
        Assert.IsTrue(flanker.InCombat, "and the guard fights on from there");
    }

    [TestMethod]
    public void AFlankerInReachOfItsTargetOnTheWayStopsFlanking()
    {
        GuardsAtPlanet(2, GameDifficulty.Brutal);
        RivalsAt(18_000f);
        GuardsTakeOnTheRivals();
        TestShip flanker = Guards[1];
        Assert.IsTrue(flanker.AI.IsFlanking, "setup: the guard flanks");

        flanker.Position = flanker.AI.Target.Position + new Vector2(0f, 5000f);
        RunObjectsSim(TestSimStep);

        Assert.IsFalse(flanker.AI.IsFlanking, "a flanker in reach of its target is in the fight");
    }

    // the second of two guards flanking a lone rival out east, with a row of rivals nearer it to the north
    TestShip FlankerOfALoneRival(out TestShip lone)
    {
        GuardsAtPlanet(2, GameDifficulty.Brutal);
        RivalsAt(12_000f);
        lone = Quiet(SpawnShip(FrigateName, Player, Guarded.Position + new Vector2(17_000f, 0f)));
        lone.RotationDegrees = 270f;
        lone.AI.OrderHoldPosition(MoveOrder.HoldPosition);
        RunSimWhile((10, fatal: true), () => Guards.Any(g => !g.InCombat));
        TestShip flanker = Guards[1];
        Assert.IsTrue(flanker.AI.PotentialTargets.Contains(lone), "setup: the guard sees the lone rival");

        flanker.AI.Target = lone;
        flanker.AI.PlanFlank();
        Assert.IsTrue(flanker.AI.IsFlanking, "setup: the guard flanks the lone rival");
        AssertGreaterThan(Toward(lone.Position, flanker.AI.FlankPoint).Y, 0.99f, "setup: on the lone rival's left, facing -X its left is +Y");
        return flanker;
    }

    // the row faces the planet, so its left is +X
    void AssertFlanksTheRow(TestShip flanker)
    {
        Assert.IsTrue(flanker.AI.IsFlanking, "the guard flanks again");
        AssertGreaterThan(Toward(RivalsCenter(12_000f), flanker.AI.FlankPoint).X, 0.99f, "now on the row's left");
    }

    [TestMethod]
    public void AFlankerPickingANewTargetPlansAroundIt()
    {
        TestShip flanker = FlankerOfALoneRival(out TestShip lone);

        RunSimWhile((3, fatal: true), () => flanker.AI.Target == lone);

        Assert.IsTrue(Rivals.Contains(flanker.AI.Target), "setup: the next pick is a nearer rival in the row");
        AssertFlanksTheRow(flanker);
    }

    [TestMethod]
    public void AFlankerLosingItsTargetPlansAroundTheNextOne()
    {
        TestShip flanker = FlankerOfALoneRival(out TestShip lone);
        typeof(ShipAI).GetField("TargetSelectTimer", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic)!
                      .SetValue(flanker.AI, 10f);

        lone.Die(lone, cleanupOnly: true);
        RunObjectsSim(TestSimStep);

        Assert.IsTrue(Rivals.Contains(flanker.AI.Target), "setup: the guard turns to a rival in the row at once");
        AssertFlanksTheRow(flanker);
    }

    [TestMethod]
    public void ARearFlankerAlreadyRoundTheSideGoesStraightBehind()
    {
        GuardsAtPlanet(5, GameDifficulty.Brutal);
        RivalsSidewaysAt(18_000f, 6000f);
        GuardsTakeOnTheRivals();
        TestShip rear = Guards[4];
        Assert.IsTrue(rear.AI.HasFlankWaypoint, "setup: from in front, the rear is reached round a side");

        rear.Position = rear.AI.FlankWaypoint;
        rear.AI.PlanFlank();

        Assert.IsTrue(rear.AI.IsFlanking && !rear.AI.HasFlankWaypoint, "from the side the way behind is clear, so it goes straight");
        AssertLessThan(Toward(RivalsCenter(18_000f, sideways: 6000f), rear.AI.FlankPoint).Y, -0.99f, "to the point behind the rivals");
    }

    [TestMethod]
    public void AFlankerDoesNotOvertake()
    {
        GuardsAtPlanet(2, GameDifficulty.Brutal);
        RivalsAt(18_000f);
        RunSimWhile((10, fatal: true), () => Guards.Any(g => !g.InCombat));
        Assert.IsTrue(Guards.All(g => !g.IsInWarp), "setup: neither guard is in warp yet");

        Assert.IsTrue(Overtakes(Guards[0]), "setup: the head-on guard cuts a fast target off");
        Assert.IsFalse(Overtakes(Guards[1]), "the flanker keeps to its way to the flank");
        Assert.IsTrue(Guards[1].AI.IsFlanking);
    }

    // a target 12,000 off at twice the guard's speed, against a guard that wins every overtake roll
    bool Overtakes(TestShip guard)
    {
        guard.Level = 95;
        Ship target = guard.AI.Target;
        target.Position = guard.Position + new Vector2(0f, -12_000f);
        target.Velocity = new Vector2(0f, -guard.MaxSTLSpeed * 2f);
        guard.AI.MoveToEngageTarget(target, TestSimStep);
        return guard.AI.State == AIState.Pursue;
    }

    [TestMethod]
    public void AGuardLeavingCombatDropsItsFlank()
    {
        GuardsAtPlanet(2, GameDifficulty.Brutal);
        RivalsAt(18_000f);
        GuardsTakeOnTheRivals();
        Assert.IsTrue(Guards[1].AI.IsFlanking, "setup: the guard flanks");

        Guards[1].AI.ExitCombatState();

        Assert.IsFalse(Guards[1].AI.IsFlanking);
    }
}
