using System;
using System.Reflection;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Ship_Game;
using Ship_Game.AI;
using Ship_Game.Ships;
using UnitTests.Ships;
using Vector2 = SDGraphics.Vector2;

namespace UnitTests.AITests.Ships;

[TestClass]
public class OvertakeTests : StarDriveTest
{
    const string Design = "Vulcan Scout";
    const string Armed = "Fang Strafer";
    const string CarrierDesign = "Heavy Carrier mk5-b";

    public OvertakeTests()
    {
        LoadStarterShips(Design, Armed, CarrierDesign);
        CreateUniverseAndPlayerEmpire();
    }

    [TestMethod]
    public void AnAIShipCuttingOffAFasterTargetAimsWhereTheTargetIsGoing()
    {
        TestShip chaser = SpawnShip(Design, Enemy, new Vector2(100_000f, 100_000f));
        TestShip target = SpawnShip(Design, Player, chaser.Position + new Vector2(8_000f, 0f));
        target.RotationDegrees = 270f;
        target.Velocity = new Vector2(chaser.MaxSTLSpeed + 200f, 0f);
        Assert.IsTrue(target.Direction.X < -0.99f, "setup: the target faces back toward the chaser while it flies away");

        for (int i = 0; i < 1000 && chaser.AI.State != AIState.Pursue; ++i)
            chaser.AI.MoveToEngageTarget(target, TestSimStep);

        Assert.AreEqual(AIState.Pursue, chaser.AI.State, "the chaser sets off to cut the target off");
        Assert.IsTrue(chaser.AI.MovePosition.X > target.Position.X + 5_000f,
                      $"it aims ahead of where the target flies, not where its nose points (aim {chaser.AI.MovePosition.X - target.Position.X:0} from the target)");
    }

    // a target 8,000 ahead flying straight away at the given share of the chaser's top speed
    bool CutsOffATargetAt(float speedShare, GameDifficulty difficulty)
    {
        UState.P.Difficulty = difficulty;
        TestShip chaser = SpawnShip(Design, Enemy, new Vector2(100_000f, 100_000f));
        TestShip target = SpawnShip(Design, Player, chaser.Position + new Vector2(8_000f, 0f));
        target.Velocity = new Vector2(chaser.MaxSTLSpeed * speedShare, 0f);

        for (int i = 0; i < 1000 && chaser.AI.State != AIState.Pursue; ++i)
            chaser.AI.MoveToEngageTarget(target, TestSimStep);
        return chaser.AI.State == AIState.Pursue;
    }

    [TestMethod]
    public void AboveNormalATargetAtFourFifthsOfTheChasersSpeedIsCutOff()
    {
        Assert.IsTrue(CutsOffATargetAt(0.8f, GameDifficulty.Hard), "on Hard");
    }

    [TestMethod]
    public void OnNormalATargetAtFourFifthsOfTheChasersSpeedIsChasedAsBefore()
    {
        Assert.IsFalse(CutsOffATargetAt(0.8f, GameDifficulty.Normal), "on Normal only a faster target is cut off");
    }

    [TestMethod]
    public void AboveNormalATargetBelowThreeQuartersOfTheChasersSpeedIsChasedAsBefore()
    {
        Assert.IsFalse(CutsOffATargetAt(0.7f, GameDifficulty.Insane), "below three quarters of its speed it is caught by chasing");
    }

    // on Hard and Short Range, a target just past the chaser's guns but in its reach, flying straight away from it or at it at four fifths of its top speed
    (TestShip Chaser, TestShip Target, float Distance) InReach(bool backingAway, float y = 100_000f)
    {
        UState.P.Difficulty = GameDifficulty.Hard;
        TestShip chaser = SpawnShip(Armed, Enemy, new Vector2(100_000f, y));
        chaser.AI.CombatState = CombatState.ShortRange;
        float distance = Math.Max(3_100f, Math.Max(chaser.WeaponsMaxRange, chaser.DesiredCombatRange) + 200f);
        TestShip target = SpawnShip(Design, Player, chaser.Position + new Vector2(distance, 0f));
        target.Velocity = new Vector2((backingAway ? 0.8f : -0.8f) * chaser.MaxSTLSpeed, 0f);
        chaser.AI.Target = target;
        Assert.IsTrue(chaser.Weapons.Count > 0 && distance < 7_500f && chaser.AI.IsTargetValid(target),
                      $"setup: an armed chaser with a target in its reach (range {chaser.WeaponsMaxRange:0}, desired {chaser.DesiredCombatRange:0}, target at {distance:0})");
        return (chaser, target, distance);
    }

    // a target 8,000 ahead of an AI ship, fleeing at four fifths of its top speed
    TestShip Fleeing(TestShip chaser)
    {
        TestShip fleeing = SpawnShip(Design, Player, chaser.Position + new Vector2(8_000f, 0f));
        fleeing.Velocity = new Vector2(chaser.MaxSTLSpeed * 0.8f, 0f);
        return fleeing;
    }

    bool CutsOffFromFar(TestShip chaser, TestShip fleeing)
    {
        for (int i = 0; i < 1000 && chaser.AI.State != AIState.Pursue; ++i)
            chaser.AI.MoveToEngageTarget(fleeing, TestSimStep);
        return chaser.AI.State == AIState.Pursue;
    }

    bool CutsOffInItsFight(TestShip chaser)
    {
        MethodInfo doCombat = typeof(ShipAI).GetMethod("DoCombat", BindingFlags.Instance | BindingFlags.NonPublic)!;
        for (int i = 0; i < 6000 && chaser.AI.State != AIState.Pursue; ++i)
            doCombat.Invoke(chaser.AI, new object[] { TestSimStep });
        return chaser.AI.State == AIState.Pursue;
    }

    static int CutOffs(TestShip chaser, TestShip target, float distance, int tries = 40_000)
    {
        int cutOffs = 0;
        for (int i = 0; i < tries; ++i)
            if (chaser.AI.TryOvertakeTargetInReach(target, distance))
                ++cutOffs;
        return cutOffs;
    }

    [TestMethod]
    public void AnAIShipInReachOfATargetBackingAwayCutsItOff()
    {
        (TestShip chaser, TestShip target, _) = InReach(backingAway: true);
        float farthestPoint = chaser.Position.Distance(target.Position) + target.CurrentVelocity * 4 + 7500 + chaser.Radius * 5 + 2000;
        Assert.IsTrue(farthestPoint < 15_000f, $"setup: the point ahead lies closer than a far chase would go for ({farthestPoint:0})");

        Assert.IsTrue(CutsOffInItsFight(chaser), "the chaser sets off to cut the target off");
        Assert.IsTrue(chaser.AI.MovePosition.X > target.Position.X + 5_000f, "ahead of where the target flies");
    }

    [TestMethod]
    public void AnAIShipInReachOfATargetComingAtItFightsOn()
    {
        (TestShip chaser, _, _) = InReach(backingAway: false);

        Assert.IsFalse(CutsOffInItsFight(chaser), "a target closing in is fought where it is");
    }

    [TestMethod]
    public void InReachAShortRangeOrAttackRunsShipCutsOffFiveTimesAsOftenAsOthers()
    {
        (TestShip chaser, TestShip target, float distance) = InReach(backingAway: true);
        int CutOffsOn(CombatState stance)
        {
            chaser.AI.CombatState = stance;
            return CutOffs(chaser, target, distance);
        }

        int shortRange = CutOffsOn(CombatState.ShortRange);
        int attackRuns = CutOffsOn(CombatState.AttackRuns);
        int artillery = CutOffsOn(CombatState.Artillery);

        Assert.IsTrue(shortRange is > 140 and < 260, $"about one step in two hundred on Short Range ({shortRange} of 40000)");
        Assert.IsTrue(attackRuns is > 140 and < 260, $"about one step in two hundred on Attack Runs ({attackRuns} of 40000)");
        Assert.IsTrue(artillery is > 10 and < 80, $"about one step in a thousand on Artillery ({artillery} of 40000)");
    }

    [TestMethod]
    public void AShipWhoseEnginesAreOutSetsOffToCutNoTargetOff()
    {
        (TestShip chaser, TestShip target, float distance) = InReach(backingAway: true);
        chaser.EnginesKnockedOut = true;
        AssertEqual(0, CutOffs(chaser, target, distance), "in reach");

        TestShip far = SpawnShip(Design, Enemy, new Vector2(300_000f, 100_000f));
        TestShip fleeing = SpawnShip(Design, Player, far.Position + new Vector2(8_000f, 0f));
        fleeing.Velocity = new Vector2(far.MaxSTLSpeed * 0.8f, 0f);
        far.EnginesKnockedOut = true;
        for (int i = 0; i < 1000; ++i)
            far.AI.MoveToEngageTarget(fleeing, TestSimStep);
        Assert.AreNotEqual(AIState.Pursue, far.AI.State, "nor farther out");
    }

    [TestMethod]
    public void AnEvadingShipOrAHangarShipSetsOffToCutNoTargetOff()
    {
        (TestShip chaser, TestShip target, float distance) = InReach(backingAway: true);
        chaser.AI.CombatState = CombatState.Evade;
        AssertEqual(0, CutOffs(chaser, target, distance), "a ship evading keeps away");

        chaser.AI.CombatState = CombatState.ShortRange;
        chaser.Mothership = SpawnShip(Design, Enemy, chaser.Position + new Vector2(0f, 2_000f));
        Assert.IsTrue(chaser.IsHangarShip, "setup: the chaser flies from a carrier");
        AssertEqual(0, CutOffs(chaser, target, distance), "a ship from a carrier stays with it");
    }

    [TestMethod]
    public void AShipAtTheRangeItWantsToFightAtCutsNoTargetOff()
    {
        UState.P.Difficulty = GameDifficulty.Hard;
        TestShip carrier = SpawnShip(CarrierDesign, Enemy, new Vector2(100_000f, 100_000f));
        float distance = Math.Min(carrier.DesiredCombatRange - 100f, 7_400f);
        TestShip target = SpawnShip(Design, Player, carrier.Position + new Vector2(distance, 0f));
        target.Velocity = new Vector2(0.8f * carrier.MaxSTLSpeed, 0f);
        carrier.AI.Target = target;
        Assert.IsTrue(carrier.AI.CombatState != CombatState.Evade && distance > Math.Max(3_000f, carrier.WeaponsMaxRange * 0.7f),
                      $"setup: a carrier whose chosen range lies past seven tenths of its reach (stance {carrier.AI.CombatState}, range {carrier.WeaponsMaxRange:0}, desired {carrier.DesiredCombatRange:0})");

        AssertEqual(0, CutOffs(carrier, target, distance), "a carrier within the range it chose keeps its distance");
        Assert.IsTrue(carrier.DesiredCombatRange + 100f < 7_500f, $"setup: the carrier's chosen range leaves room in its reach ({carrier.DesiredCombatRange:0})");
        AssertGreaterThan(CutOffs(carrier, target, carrier.DesiredCombatRange + 100f), 0, "setup: just past that range the carrier cuts it off");
    }

    [TestMethod]
    public void AShipThatCannotWarpNowSetsOffToCutNoTargetOff()
    {
        (TestShip held, TestShip target, float distance) = InReach(backingAway: true);
        HoldOutOfWarp(held);
        AssertEqual(0, CutOffs(held, target, distance), "in reach, held out of warp by an enemy ship");

        (TestShip driveLost, TestShip target2, float distance2) = InReach(backingAway: true, y: 125_000f);
        LoseWarpDrive(driveLost);
        AssertEqual(0, CutOffs(driveLost, target2, distance2), "in reach, its warp drive lost");

        UState.P.Difficulty = GameDifficulty.Hard; // a target at four fifths of the chaser's speed is cut off from far only above Normal
        TestShip farHeld = SpawnShip(Design, Enemy, new Vector2(300_000f, 100_000f));
        HoldOutOfWarp(farHeld);
        Assert.IsFalse(CutsOffFromFar(farHeld, Fleeing(farHeld)), "farther out, held out of warp");

        TestShip farDriveLost = SpawnShip(Design, Enemy, new Vector2(300_000f, 125_000f));
        LoseWarpDrive(farDriveLost);
        Assert.IsFalse(CutsOffFromFar(farDriveLost, Fleeing(farDriveLost)), "farther out, its warp drive lost");

        TestShip farFree = SpawnShip(Design, Enemy, new Vector2(300_000f, 200_000f));
        Assert.IsTrue(CutsOffFromFar(farFree, Fleeing(farFree)), "setup: one that can warp cuts it off from there");

        static void HoldOutOfWarp(Ship ship)
        {
            ship.SetWarpInhibited(Ship.InhibitionType.EnemyShip, secondsToInhibit: 60f);
            ship.ShipEngines.Update(ship); // as each frame's status update does
        }

        static void LoseWarpDrive(Ship ship)
        {
            ship.MaxFTLSpeed = 0f; // its warp modules destroyed, its sublight engines working
            ship.ShipEngines.Update(ship);
            Assert.IsTrue(!ship.EnginesKnockedOut && !ship.Inhibited, "setup: only the warp drive is lost");
        }
    }

    [TestMethod]
    public void AShipHeldByATractorBeamSetsOffToCutNoTargetOff()
    {
        (TestShip chaser, TestShip target, float distance) = InReach(backingAway: true);
        chaser.CauseTractorDamage(chaser.Mass * 0.5f, hittingShields: false);
        Assert.IsTrue(chaser.TractorDamage > 0f && !chaser.EnginesKnockedOut, "setup: held by a tractor beam, its engines running");
        AssertEqual(0, CutOffs(chaser, target, distance), "in reach");

        TestShip far = SpawnShip(Design, Enemy, new Vector2(300_000f, 100_000f));
        TestShip fleeing = SpawnShip(Design, Player, far.Position + new Vector2(8_000f, 0f));
        fleeing.Velocity = new Vector2(far.MaxSTLSpeed * 0.8f, 0f);
        far.CauseTractorDamage(far.Mass * 0.5f, hittingShields: false);
        for (int i = 0; i < 1000; ++i)
            far.AI.MoveToEngageTarget(fleeing, TestSimStep);
        Assert.AreNotEqual(AIState.Pursue, far.AI.State, "nor farther out");
    }

    [TestMethod]
    public void ATargetCloseInAndUnderTheChasersGunsIsFoughtWhereItIs()
    {
        (TestShip chaser, TestShip target, _) = InReach(backingAway: true);
        float distance = Math.Max(chaser.WeaponsMaxRange * 0.7f, chaser.DesiredCombatRange) + 50f;
        Assert.IsTrue(distance < 3_000f && distance <= chaser.WeaponsMaxRange,
                      $"setup: a target closer than 3,000 within the chaser's guns ({distance:0}, range {chaser.WeaponsMaxRange:0})");

        AssertEqual(0, CutOffs(chaser, target, distance), "close in and under its guns");
        AssertGreaterThan(CutOffs(chaser, target, 3_100f), 0, "setup: farther out the same chaser cuts it off");
    }

    [TestMethod]
    public void TheSpreadAheadOfATargetLiesAheadOfIt()
    {
        var random = new Ship_Game.Utils.SeededRandom(1);
        Vector2 center = new(1_000f, 1_000f);
        for (int i = 0; i < 50; ++i)
        {
            Vector2 p = Ship_Game.ExtensionMethods.NewMathExt.GenerateRandomPointOnForwardHalfArc(center, 0f, 2_000f, random);
            Assert.IsTrue(p.Y <= center.Y + 0.01f, $"rotation 0 faces up, so the forward half lies above the center ({p})");
        }
    }
}
