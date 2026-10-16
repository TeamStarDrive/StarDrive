using Microsoft.VisualStudio.TestTools.UnitTesting;
using SDUtils;
using Ship_Game;
using Ship_Game.AI;
using Ship_Game.Commands.Goals;
using Ship_Game.Ships;
using Vector2 = SDGraphics.Vector2;
#pragma warning disable CA2213

namespace UnitTests.Ships;

/// <summary>
/// The pirates scuttle a projector, research station or mining station they capture, 20 seconds later. Their troops
/// leave it in assault shuttles for home first, and live on after it blows. A projector raid counts the scuttled
/// projector as a success even when it is gone before the raid looks, and pirates paid to protect a station who take
/// it back from the raiders hold it with their troops until they hand it back, instead of scuttling it.
/// </summary>
[TestClass]
public class PirateCaptureScuttleTests : StarDriveTest
{
    readonly Empire Raiders;
    readonly Ship Base;

    public PirateCaptureScuttleTests()
    {
        LoadStarterShips("Corsair Asteroid Base", "Assault Shuttle");
        CreateUniverseAndPlayerEmpire();
        CreateAMinorFaction("Corsairs");
        Raiders = Faction;
        Universe.NotificationManager = new NotificationManager(Universe.ScreenManager, Universe);
        UState.Objects.EnableParallelUpdate = false;
        Base = SpawnShip("Corsair Asteroid Base", Raiders, new Vector2(400_000));
        RunObjectsSim(TestSimStep);
    }

    Ship StationWithBoarders(string design, Empire pirates, int pirateTroops)
    {
        Ship station = SpawnShip(design, Player, new Vector2(20_000));
        station.SetSystem(AddDummyPlanet(station.Position).System);
        RunObjectsSim(TestSimStep);
        station.KillAllTroops();
        LandTroops(station, pirates, pirateTroops);
        return station;
    }

    static void LandTroops(Ship station, Empire pirates, int troops)
    {
        for (int i = 0; i < troops; ++i)
            ResourceManager.CreateTroop("Wyvern", pirates).LandOnShip(station);
    }

    void Capture(Ship station, Empire pirates)
    {
        station.LoyaltyChangeFromBoarding(pirates, addNotification: false);
        RunObjectsSim(TestSimStep);
        AssertEqual(pirates, station.Loyalty, "setup: the pirates must hold the station");
    }

    Ship[] Shuttles(Empire pirates)
    {
        return UState.Objects.GetShips().Filter(s => s.Loyalty == pirates && s.Name == Empire.DefaultBoardingShuttleName);
    }

    void RunUntilGone(Ship station) => RunSimWhile((simTimeout: 40, fatal: true), () => station.Active);

    [DataTestMethod]
    [DataRow("Subspace Projector")]
    [DataRow("Basic Research Station")]
    [DataRow("Basic Mining Station")]
    public void TheTroopsLeaveACapturedStationBeforeThePiratesScuttleIt(string design)
    {
        Ship station = StationWithBoarders(design, Raiders, pirateTroops: 2);
        Capture(station, Raiders);
        AssertEqual(0.1f, 20f, station.ScuttleTimer, "the pirates scuttle the station 20 seconds after they take it");
        AssertEqual(0, station.TroopCount, "no troop stays aboard");

        Ship[] shuttles = Shuttles(Raiders);
        AssertEqual(2, shuttles.Length, "each troop leaves in its own assault shuttle");
        foreach (Ship shuttle in shuttles)
        {
            AssertEqual(1, shuttle.TroopCount, "with its troop on board");
            Assert.IsTrue(shuttle.AI.FindGoal(ShipAI.Plan.Escort, out ShipAI.ShipGoal home) && home.TargetShip == Base,
                          "and flies home to the pirate base");
        }

        RunUntilGone(station);
        foreach (Ship shuttle in shuttles)
            Assert.IsTrue(shuttle.Active && shuttle.TroopCount == 1, "the troops live on after the station blows");
    }

    [DataTestMethod]
    [DataRow(false, DisplayName = "the raid looks only after the blast")]
    [DataRow(true, DisplayName = "the raid sees the capture, then the blast")]
    public void AProjectorRaidSucceedsWhenTheCapturedProjectorIsScuttled(bool raidSeesTheCapture)
    {
        Ship projector = StationWithBoarders("Subspace Projector", Raiders, pirateTroops: 1);
        var raid = new PirateRaidProjector(Raiders, Player) { TargetShip = projector };
        Raiders.AI.AddGoal(raid);
        raid.AdvanceToNextStep(); // the boarders are out, waiting for the capture

        Capture(projector, Raiders);
        if (raidSeesTheCapture)
            AssertEqual(GoalStep.GoToNextStep, raid.Evaluate(), "setup: the raid sees the projector taken");

        RunUntilGone(projector);
        AssertEqual(GoalStep.GoalComplete, raid.Evaluate(), "the scuttled projector counts as a success, which can level the pirates up");
    }

    [TestMethod]
    public void PiratesPaidToProtectAStationTakeItBackFromTheRaidersAndHandItBack()
    {
        CreateAMinorFaction("Draugar");
        Empire protectors = Faction;
        Ship station = StationWithBoarders("Basic Research Station", Raiders, pirateTroops: 2);
        var protection = new PirateProtection(protectors, Player, station);
        protectors.AI.AddGoal(protection);
        protection.AdvanceToNextStep(); // the rescuers are out, waiting for the raiders to take it

        Capture(station, Raiders);
        AssertGreaterThan(station.ScuttleTimer, 0f, "setup: the raiders scuttle the station they took");

        LandTroops(station, protectors, 2);
        Capture(station, protectors);
        AssertLessThan(station.ScuttleTimer, 0f, "taking it back stops the scuttle, and the rescuers start none of their own");
        AssertEqual(2, station.TroopCount, "their troops hold it until the hand-back");

        AssertEqual(GoalStep.GoToNextStep, protection.Evaluate(), "setup: the rescuers see the station taken back");
        RunObjectsSim(10f);
        AssertEqual(GoalStep.GoalComplete, protection.Evaluate(), "the rescuers hand the station back");
        RunObjectsSim(25f);
        Assert.IsTrue(station.Active, "the station survives");
        AssertEqual(Player, station.Loyalty, "and is the owner's again");
        AssertEqual(2, Shuttles(protectors).Length, "the rescuers' troops leave it at the hand-back");
    }
}
