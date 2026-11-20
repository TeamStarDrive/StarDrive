using System.Reflection;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using SDGraphics;
using Ship_Game;
using Ship_Game.AI;
using Ship_Game.Ships;
using UnitTests.Ships;
using Vector2 = SDGraphics.Vector2;

namespace UnitTests.AITests.Ships;

[TestClass]
public class CombatJumpTests : StarDriveTest
{
    const string Armed = "Fang Strafer";
    const string Scout = "Vulcan Scout";
    static readonly MethodInfo DoCombat = typeof(ShipAI).GetMethod("DoCombat", BindingFlags.Instance | BindingFlags.NonPublic)!;

    public CombatJumpTests()
    {
        LoadStarterShips(Armed, Scout);
        CreateUniverseAndPlayerEmpire();
    }

    // an AI ship after one thrust toward a point straight ahead of it at the given distance
    TestShip ThrustingAhead(float distance, bool inCombat, bool priorityOrder = false, AIState state = AIState.AwaitingOrders)
    {
        TestShip ship = SpawnShip(Scout, Enemy, new Vector2(100_000f, 100_000f));
        Assert.IsTrue(ship.MaxFTLSpeed > 0f && ship.WeaponsMaxRange < ShipAI.FormationWarpExitDistance,
                      $"setup: a ship that can warp, its guns reaching less than 7500 ({ship.WeaponsMaxRange:0})");
        ship.InCombat = inCombat;
        ship.AI.State = state;
        if (priorityOrder)
            ship.AI.SetPriorityOrder(true);
        ship.AI.ThrustOrWarpToPos(ship.Position + ship.Direction * distance, TestSimStep);
        return ship;
    }

    [TestMethod]
    public void AShipInCombatGoesAtSublightToAPointWithinTheCombatJumpDistance()
    {
        Assert.IsFalse(ThrustingAhead(12_000f, inCombat: true).IsSpoolingOrInWarp, "a jump this short would end at its target's reach as it began");
    }

    [TestMethod]
    public void AShipInCombatJumpsToAPointPastTheCombatJumpDistance()
    {
        Assert.IsTrue(ThrustingAhead(20_000f, inCombat: true).IsSpooling);
    }

    [TestMethod]
    public void AShipOutOfCombatOrOnAPriorityOrderJumpsPastTheWarpExitDistance()
    {
        Assert.IsTrue(ThrustingAhead(12_000f, inCombat: false).IsSpooling, "out of combat");
        Assert.IsTrue(ThrustingAhead(12_000f, inCombat: true, priorityOrder: true).IsSpooling, "on a priority order in combat");
    }

    [TestMethod]
    public void AShipInCombatFlyingToItsSupplyShipJumpsPastTheWarpExitDistance()
    {
        Assert.IsTrue(ThrustingAhead(12_000f, inCombat: true, state: AIState.ResupplyEscort).IsSpooling, "its jump ends beside the supply ship, not at a target's reach");
    }

    [TestMethod]
    public void AShipClosingInOnItsChosenTargetStartsNoJumpOnceTheTargetIsInReach()
    {
        TestShip chaser = SpawnShip(Armed, Enemy, new Vector2(100_000f, 100_000f));
        chaser.AI.CombatState = CombatState.ShortRange;
        typeof(Ship).GetProperty(nameof(Ship.DesiredCombatRange))!.SetValue(chaser, 2_000f);
        TestShip target = SpawnShip(Scout, Player, chaser.Position + new Vector2(7_400f, 0f));
        chaser.AI.Target = target;
        chaser.AI.HasPriorityTarget = true;
        chaser.AI.Intercepting = true;
        chaser.InCombat = true;
        Vector2 drift = new(0f, 1_300f); // sideways, so it aims past the target, beyond 7500
        Vector2 aim = new Ship_Game.Gameplay.ImpactPredictor(chaser.Position, drift, target.Position).PredictMovePos();
        float facing = chaser.Position.DirectionToTarget(aim).ToRadians();
        void Drifting()
        {
            chaser.Velocity = drift;
            chaser.Rotation = facing;
        }
        Drifting();
        chaser.AI.ThrustOrWarpToPos(target.Position, TestSimStep);
        Assert.IsTrue(chaser.IsSpooling && chaser.AI.IsTargetValid(target),
                      $"setup: aiming {chaser.Position.Distance(aim):0} off, the move toward a target it may fight starts a jump");
        chaser.HyperspaceReturn();
        Drifting();

        DoCombat.Invoke(chaser.AI, new object[] { TestSimStep });

        Assert.IsFalse(chaser.IsSpoolingOrInWarp, "with that target in reach, the jump ends before it spools a step");
    }

    [TestMethod]
    public void AJumpStillSpoolingIsCalledOffOnceItsTargetIsWithin7500()
    {
        TestShip chaser = SpawnShip(Armed, Enemy, new Vector2(100_000f, 100_000f));
        chaser.AI.CombatState = CombatState.ShortRange;
        typeof(Ship).GetProperty(nameof(Ship.DesiredCombatRange))!.SetValue(chaser, 2_000f);
        TestShip target = SpawnShip(Scout, Player, chaser.Position + new Vector2(7_000f, 0f));
        chaser.AI.Target = target;
        chaser.AI.Intercepting = true; // closing in on the target, which ends the step before its stance moves it
        chaser.InCombat = true;
        chaser.EngageStarDrive();
        Assert.IsTrue(chaser.IsSpooling && chaser.AI.IsTargetValid(target) && chaser.DesiredCombatRange * 3f < 7_000f,
                      $"setup: the chaser spools for a jump at a target past three times its chosen range ({chaser.DesiredCombatRange:0})");

        DoCombat.Invoke(chaser.AI, new object[] { TestSimStep });

        Assert.IsFalse(chaser.IsSpoolingOrInWarp, "with its target in reach the chaser closes in at sublight, with no jump of a step");
    }

    // a ship that keeps its targets 20,000 off, in a fight with a target the given distance straight ahead
    TestShip LongRangeShipInAFightAt(float targetAt, bool spooling)
    {
        TestShip chaser = SpawnShip(Armed, Enemy, new Vector2(100_000f, 100_000f + targetAt * 4f));
        chaser.AI.CombatState = CombatState.ShortRange;
        typeof(Ship).GetProperty(nameof(Ship.DesiredCombatRange))!.SetValue(chaser, 20_000f);
        TestShip target = SpawnShip(Scout, Player, chaser.Position + chaser.Direction * targetAt);
        chaser.AI.Target = target;
        chaser.InCombat = true;
        if (spooling)
            chaser.EngageStarDrive();
        Assert.IsTrue(chaser.IsSpooling == spooling && chaser.AI.IsTargetValid(target),
                      $"setup: a valid target {targetAt:0} off (sensors {chaser.SensorRange:0}), spooling {spooling}");
        DoCombat.Invoke(chaser.AI, new object[] { TestSimStep });
        return chaser;
    }

    [TestMethod]
    public void ALongRangeShipCallsOffAJumpThatWouldDropOutWithin7500AtItsChosenRange()
    {
        Assert.IsFalse(LongRangeShipInAFightAt(21_000f, spooling: true).IsSpoolingOrInWarp, "just past its chosen range");
        Assert.IsFalse(LongRangeShipInAFightAt(26_000f, spooling: false).IsSpoolingOrInWarp, "6,000 past it, a jump started and called off at once");
        Assert.IsTrue(LongRangeShipInAFightAt(28_000f, spooling: false).IsSpooling, "setup: more than 7,500 past it, it jumps");
    }

    [TestMethod]
    public void AMoveInAFightThatIsNotAtTheTargetStillJumpsWithTheTargetNear()
    {
        TestShip bomber = SpawnShip(Scout, Enemy, new Vector2(100_000f, 100_000f));
        bomber.AI.Target = SpawnShip(Scout, Player, bomber.Position + new Vector2(5_000f, 0f));
        bomber.InCombat = true; // as a bombing run puts it

        bomber.AI.ThrustOrWarpToPos(bomber.Position + bomber.Direction * 40_000f, TestSimStep);

        Assert.IsTrue(bomber.IsSpooling, "a move to a planet 40,000 off jumps, whatever its target nearby");
    }

    [TestMethod]
    public void AStanceMoveWithItsTargetInReachStartsNoJump()
    {
        TestShip chaser = SpawnShip(Armed, Enemy, new Vector2(100_000f, 100_000f));
        TestShip target = SpawnShip(Scout, Player, chaser.Position + new Vector2(2_000f, 0f));
        chaser.AI.CombatState = CombatState.OrbitLeft;
        typeof(Ship).GetProperty(nameof(Ship.DesiredCombatRange))!.SetValue(chaser, 30_000f); // a long chosen range, as a carrier's can be
        chaser.AI.Target = target;
        chaser.InCombat = true;
        chaser.AI.CombatAI.SetCombatTactics(CombatState.Artillery);
        chaser.AI.CombatAI.SetCombatTactics(CombatState.OrbitLeft);

        chaser.AI.CombatAI.ExecuteCombatTactic(TestSimStep);
        object orbit = typeof(CombatAI).GetField("CombatTactic", BindingFlags.Instance | BindingFlags.NonPublic)!.GetValue(chaser.AI.CombatAI)!;
        Vector2 orbitPos = (Vector2)orbit.GetType().GetProperty("OrbitPos")!.GetValue(orbit)!;
        chaser.Rotation = chaser.Position.DirectionToTarget(orbitPos).ToRadians();
        chaser.AI.CombatAI.ExecuteCombatTactic(TestSimStep);
        Assert.IsTrue(chaser.IsSpooling, $"setup: the orbit stance asks for a jump to its orbit point {chaser.Position.Distance(orbitPos):0} away");
        chaser.HyperspaceReturn();

        DoCombat.Invoke(chaser.AI, new object[] { TestSimStep });

        Assert.IsFalse(chaser.IsSpoolingOrInWarp, "with its target in reach, the jump its stance asks for ends before it spools a step");
    }
}
