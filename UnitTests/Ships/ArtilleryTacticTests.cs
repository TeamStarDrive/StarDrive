using Microsoft.VisualStudio.TestTools.UnitTesting;
using SDGraphics;
using Ship_Game;
using Ship_Game.AI;
using Ship_Game.AI.CombatTactics;
using Ship_Game.Ships;
using Vector2 = SDGraphics.Vector2;

namespace UnitTests.Ships;

[TestClass]
public class ArtilleryTacticTests : StarDriveTest
{
    const string Design = "Terran-Prototype";
    const float Speed = 500f;
    readonly TestShip Gunship;
    readonly TestShip Target;

    public ArtilleryTacticTests()
    {
        LoadStarterShips(Design);
        CreateUniverseAndPlayerEmpire();
        Gunship = SpawnShip(Design, Player, new Vector2(100_000f, 100_000f));
        Gunship.AI.CombatState = CombatState.Artillery;
        Target = SpawnShip(Design, Enemy, Gunship.Position + new Vector2(50_000f, 0f));
        Gunship.RotationDegrees = Target.RotationDegrees = 90f;
        Gunship.AI.Target = Target;
    }

    float StopDistanceAtOwnSpeed => Gunship.GetMinDecelerationDistance(Speed) * 1.2f;

    void PlaceTarget(float shortOfStandoff, float theirSpeed, float ourSpeed = Speed)
    {
        float standoff = Gunship.DesiredCombatRange - ((int)Gunship.Radius).RoundUpToMultipleOf(10);
        Target.Position = Gunship.Position + new Vector2(standoff + shortOfStandoff - 0.5f * Target.Radius, 0f);
        Gunship.Velocity = new Vector2(ourSpeed, 0f);
        Target.Velocity = new Vector2(theirSpeed, 0f);
    }

    Thrust ThrustOnApproach()
    {
        Gunship.ThrustThisFrame = Thrust.Coast;
        new Artillery(Gunship.AI).Execute(TestSimStep, null);
        return Gunship.ThrustThisFrame;
    }

    [TestMethod]
    public void AnArtilleryShipKeepsClosingOnATargetFleeingAlmostAsFast()
    {
        PlaceTarget(shortOfStandoff: StopDistanceAtOwnSpeed * 0.5f, theirSpeed: Speed * 0.9f);

        Assert.AreNotEqual(Thrust.Reverse, ThrustOnApproach(), "closing at a tenth of its own speed, it is far from needing to brake");
    }

    [TestMethod]
    public void AnArtilleryShipBrakesEarlierForATargetComingAtIt()
    {
        PlaceTarget(shortOfStandoff: StopDistanceAtOwnSpeed * 2f, theirSpeed: -Speed);

        Assert.AreEqual(Thrust.Reverse, ThrustOnApproach(), "closing at twice its own speed, it brakes before its own speed alone would call for it");
    }

    [TestMethod]
    public void ABigArtilleryShipHasRoomToHoldItsRange()
    {
        Gunship.Radius = 0.25f * Gunship.DesiredCombatRange;
        float standoff = Gunship.DesiredCombatRange - ((int)Gunship.Radius).RoundUpToMultipleOf(10);
        Assert.IsTrue(standoff < 0.9f * Gunship.DesiredCombatRange, "setup: the hull is too big for the usual band between 90% of range and range less radius");
        Target.Position = Gunship.Position + new Vector2(standoff - 50f - 0.5f * Target.Radius, 0f);
        Gunship.Velocity = Target.Velocity = Vector2.Zero;

        Assert.AreNotEqual(Thrust.Reverse, ThrustOnApproach(), "just inside its range a still ship holds, instead of backing off and coming in again");
    }

    [TestMethod]
    public void AnArtilleryShipDriftingAwayFromItsTargetDoesNotBrake()
    {
        PlaceTarget(shortOfStandoff: StopDistanceAtOwnSpeed * 0.5f, theirSpeed: 0f, ourSpeed: -Speed);

        Assert.AreNotEqual(Thrust.Reverse, ThrustOnApproach(), "moving away from a target out of range, it has nothing to brake for");
    }

    [TestMethod]
    public void AnArtilleryShipWaitingForATargetComingAtItDoesNotBackAway()
    {
        PlaceTarget(shortOfStandoff: StopDistanceAtOwnSpeed * 0.5f, theirSpeed: -Speed, ourSpeed: 0f);

        Assert.AreEqual(Thrust.Coast, ThrustOnApproach(), "still and out of range, it lets the target come to it instead of backing away");
    }

    [TestMethod]
    public void AnArtilleryShipDriftingAwayFromATargetComingAtItStops()
    {
        PlaceTarget(shortOfStandoff: StopDistanceAtOwnSpeed * 0.5f, theirSpeed: -Speed, ourSpeed: -Speed * 0.2f);

        Assert.AreEqual(Thrust.AllStop, ThrustOnApproach(), "out of range and already moving away, it stops instead of speeding away");
    }
}
