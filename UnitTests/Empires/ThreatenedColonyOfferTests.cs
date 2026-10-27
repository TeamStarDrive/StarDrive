using System.Linq;
using System.Reflection;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using SDUtils;
using Ship_Game;
using Ship_Game.Empires.Components;
using Ship_Game.Fleets;
using Ship_Game.Ships;
using Vector2 = SDGraphics.Vector2;

namespace UnitTests.Empires;

/// <summary>
/// The player's colony in a system under threat, with a hostile fleet the player knows of heading there or hostile
/// warships the player sees in it, is offered in diplomacy only to the empire behind the threat, and to no one while
/// more than one empire threatens it.
/// </summary>
[TestClass]
public class ThreatenedColonyOfferTests : StarDriveTest
{
    readonly Planet Threatened;
    readonly Planet Safe;

    public ThreatenedColonyOfferTests()
    {
        CreateUniverseAndPlayerEmpire();
        CreateThirdMajorEmpire();
        Threatened = AddDummyPlanetToEmpire(new Vector2(200_000), Player);
        Safe = AddDummyPlanetToEmpire(new Vector2(-200_000), Player);
    }

    void FleetsHeadFor(Planet p, params Empire[] owners)
    {
        Fleet[] fleets = owners.Select(o => new Fleet(UState, o)).ToArray();
        Player.ThreatDetector.SystemsWithThreat = new[] { new IncomingThreat(Player, p.System, fleets) };
    }

    Ship WarshipBeside(Planet p, Empire owner)
    {
        Ship warship = SpawnShip("Fang Strafer", owner, p.Position + new Vector2(5000f, 0f));
        p.System.ShipList.Add(warship);
        Assert.IsTrue(warship.BaseStrength > 0f, "setup: an armed ship");
        return warship;
    }

    void PlayerTakesATurn() => Player.ThreatDetector.AssessHostilePresenceForPlayerWarnings(Player);

    [TestMethod]
    public void AColonyAHostileFleetIsHeadingForGoesOnlyToThatFleetsEmpire()
    {
        FleetsHeadFor(Threatened, Enemy);

        Assert.IsFalse(Player.CanOfferColonyTo(Threatened, ThirdMajor), "a colony under threat cannot be given to another empire");
        Assert.IsTrue(Player.CanOfferColonyTo(Threatened, Enemy), "it can still be handed to the empire threatening it");
        Assert.IsTrue(Player.CanOfferColonyTo(Safe, ThirdMajor), "and a colony elsewhere is offered as before");

        FleetsHeadFor(Threatened, Enemy, ThirdMajor);
        Assert.IsFalse(Player.CanOfferColonyTo(Threatened, Enemy), "with two empires threatening it, it goes to neither");
    }

    [TestMethod]
    public void AThreatWhoseFleetsLeftHoldsTheColonyBackUntilItTimesOut()
    {
        FleetsHeadFor(Threatened, Enemy);
        IncomingThreat threat = Player.ThreatDetector.SystemsWithThreat[0];
        threat.UpdateThreats(Empty<Fleet>.Array);
        Assert.IsFalse(threat.ThreatTimedOut, "setup: the threat outlasts its fleets for a while");

        Assert.IsFalse(Player.CanOfferColonyTo(Threatened, Enemy), "with no fleet behind the threat, the colony goes to no one");

        threat.Update(new FixedSimTime(11f));
        Assert.IsTrue(threat.ThreatTimedOut, "setup: the threat has timed out");
        Assert.IsTrue(Player.CanOfferColonyTo(Threatened, ThirdMajor), "once it times out the colony is offered again");
    }

    [TestMethod]
    public void AColonyWithHostileWarshipsInItsSystemGoesOnlyToTheirEmpire()
    {
        WarshipBeside(Threatened, Enemy).KnownByEmpires.SetSeen(Player);
        PlayerTakesATurn();

        Assert.IsFalse(Player.CanOfferColonyTo(Threatened, ThirdMajor), "a colony with hostile warships beside it cannot be given to another empire");
        Assert.IsTrue(Player.CanOfferColonyTo(Threatened, Enemy), "it can still be handed to the warships' empire");
        Assert.IsTrue(Player.CanOfferColonyTo(Safe, ThirdMajor), "and a colony elsewhere is offered as before");
    }

    [TestMethod]
    public void AColonyWithHostileWarshipsBesideItIsHeldBackWhileTheProjectionIsDisrupted()
    {
        WarshipBeside(Threatened, Enemy).KnownByEmpires.SetSeen(Player);
        PlayerTakesATurn();

        typeof(Empire).GetMethod("SetInfluenceActive", BindingFlags.Instance | BindingFlags.NonPublic)!.Invoke(Player, new object[] { false });

        Assert.IsFalse(Player.CanOfferColonyTo(Threatened, ThirdMajor), "a disrupted subspace projection does not hide the warships the player sees");
    }

    [TestMethod]
    public void AHostileWarshipThePlayerCannotSeeHidesNothing()
    {
        Ship warship = WarshipBeside(Threatened, Enemy);
        Assert.IsFalse(warship.InPlayerSensorRange, "setup: the player cannot see the warship");
        PlayerTakesATurn();

        Assert.IsTrue(Player.CanOfferColonyTo(Threatened, ThirdMajor), "a warship the player cannot see gives nothing away");
    }

    [TestMethod]
    public void AWarshipOfAnEmpireAtPeaceHidesNothing()
    {
        Assert.IsFalse(Player.GetRelations(ThirdMajor).IsHostile, "setup: the player is at peace with the third empire");
        WarshipBeside(Threatened, ThirdMajor).KnownByEmpires.SetSeen(Player);
        PlayerTakesATurn();

        Assert.IsTrue(Player.CanOfferColonyTo(Threatened, Enemy), "a warship at peace with the player is no threat");
    }
}
