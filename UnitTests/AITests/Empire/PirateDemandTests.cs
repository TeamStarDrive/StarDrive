using System.Reflection;
using System.Xml;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Ship_Game;
using Ship_Game.Commands.Goals;

namespace UnitTests.AITests.Empire;

[TestClass]
public class PirateDemandTests : StarDriveTest
{
    public PirateDemandTests()
    {
        CreateUniverseAndPlayerEmpire();
        CreateAMinorFaction("Corsairs");
        UState.P.Pace = 10f;
        AssertGreaterThan(UState.ProductionPace, 1f, "setup: a slower pace raises production costs");
        Assert.IsNotNull(Faction.Pirates, "setup: the Corsairs are pirates");
        Faction.Pirates.Init();
    }

    Pirates Pirates => Faction.Pirates;
    int Period => Faction.data.PiratePaymentPeriodTurns;
    int PacedPeriod => (int)(Period * UState.ProductionPace);

    [TestMethod]
    public void ThePiratesPeriodComesFromTheirRaceFile()
    {
        var race = new XmlDocument();
        race.Load(ResourceManager.GetModOrVanillaFile("Races/Corsairs.xml").FullName);
        XmlNode period = race.SelectSingleNode("/EmpireData/PiratePaymentPeriodTurns");
        Assert.IsNotNull(period, "the race file sets the period under the field's own name");
        AssertEqual(int.Parse(period.InnerText), Period, "so the game uses it");
    }

    [TestMethod]
    public void TributeIsDemandedOnTheSameTurnsAtEveryPace()
    {
        AssertEqual(Period, Pirates.PaymentTimerFor(Player), "the timer starts at the race's own period");

        Pirates.DecreasePaymentTimerFor(Player);
        Faction.AI.EndWarFromEvent(Player); // the player pays in the pirates' dialog
        AssertEqual(Period, Pirates.PaymentTimerFor(Player), "a payment sets it back to that period");

        Pirates.DecreasePaymentTimerFor(Enemy);
        Enemy.AI.EndWarFromEvent(Faction); // an AI empire pays
        AssertEqual(Period, Pirates.PaymentTimerFor(Enemy), "and so does an AI empire's payment");
    }

    [TestMethod]
    public void ARefusalWaitsThePacedPeriodSoTheThreatItAddsKeepsThePace()
    {
        Faction.AI.DeclareWarFromEvent(Player, WarType.SkirmishWar); // the player refuses in the pirates' dialog
        AssertEqual(PacedPeriod, Pirates.PaymentTimerFor(Player), "refusing to pay starts the paced period");
        Enemy.AI.DeclareWarFromEvent(Faction, WarType.SkirmishWar); // an AI empire refuses
        AssertEqual(PacedPeriod, Pirates.PaymentTimerFor(Enemy), "and so does an AI empire's refusal");

        Pirates.IncreaseThreatLevelFor(Player); // the first demand was made
        int threat = Pirates.ThreatLevelFor(Player);
        Pirates.PaymentTimers[Player.Id] = 0;
        var goal = new PirateDirectorPayment(Faction, Player);
        bool demanded = (bool)typeof(PirateDirectorPayment).GetMethod("RequestPayment", BindingFlags.NonPublic | BindingFlags.Instance)!
                                                          .Invoke(goal, null)!;

        Assert.IsFalse(demanded, "a player still refusing gets no new demand");
        AssertEqual(threat + 1, Pirates.ThreatLevelFor(Player), "the threat rises");
        AssertEqual(PacedPeriod, Pirates.PaymentTimerFor(Player), "and the next rise is a paced period away");
    }
}
