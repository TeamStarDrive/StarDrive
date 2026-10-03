using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Ship_Game;
using Ship_Game.AI;
using Ship_Game.GameScreens.LoadGame;
using Ship_Game.Ships;
using Vector2 = SDGraphics.Vector2;
#pragma warning disable CA2213

namespace UnitTests.Ships;

/// <summary>
/// A pirate ship back at its base lands on it the way a freighter lands on a space port, and the pirates take it in
/// at touchdown: their own spawned ships and assault shuttles with no gain, captured freighters and small warships
/// as salvage. Big captured warships stay out to guard the base, as before.
/// </summary>
[TestClass]
public class PirateBaseLandingTests : StarDriveTest
{
    readonly Ship Base;
    readonly Goal BaseGoal;

    public PirateBaseLandingTests()
    {
        LoadStarterShips("Corsair Asteroid Base", "Corsair", "Corsair Flagship", "TEST_Excalibur-Class Supercarrier");
        CreateUniverseAndPlayerEmpire();
        CreateAMinorFaction("Corsairs");
        Universe.NotificationManager = new NotificationManager(Universe.ScreenManager, Universe);
        UState.Objects.EnableParallelUpdate = false;
        Base = SpawnShip("Corsair Asteroid Base", Faction, new Vector2(400_000));
        Faction.Pirates.AddGoalBase(Base, "Test");
        BaseGoal = Faction.AI.FindGoal(g => g.Type == GoalType.PirateBase && g.TargetShip == Base);
        Assert.IsNotNull(BaseGoal, "setup: the pirates must run their base");
    }

    Pirates Pirates => Faction.Pirates;

    // the base goal runs every second, standing in for the empire turn
    double RunWithBase(Func<bool> condition, double timeout = 60, bool fatal = true)
    {
        int frame = 0;
        return RunSimWhile((simTimeout: timeout, fatal: fatal), condition, () =>
        {
            if (frame++ % 60 == 0)
                BaseGoal.Evaluate();
        });
    }

    Ship PirateShipNearBase(string name)
    {
        Ship ship = SpawnShip(name, Faction, Base.Position + new Vector2(2000, 0));
        ship.AI.OrderHoldPosition(MoveOrder.HoldPosition);
        return ship;
    }

    bool OrderedToLand(Ship ship) => ship.AI.FindGoal(ShipAI.Plan.LandOnPirateBase, out _);

    [TestMethod]
    public void ACapturedWarshipLandsOnTheBaseAndIsSalvagedAtTouchdown()
    {
        Ship captured = PirateShipNearBase("Vulcan Scout");
        Assert.IsFalse(Pirates.ShipsWeCanSpawn.Contains(captured.Name), "setup: the pirates cannot spawn this design yet");
        RunWithBase(() => !captured.IsLanding);

        float range = LandShip.SpacePortLandingRange(captured);
        AssertLessThan(captured.Position.Distance(Base.Position), range + 1f, "the landing starts within the space port glide range");
        AssertLessThan(captured.Velocity.Length(), LaunchShip.ShipyardSpeed(captured) + 10f, "the ship comes in no faster than a space port glide");
        Assert.IsFalse(Pirates.ShipsWeCanSpawn.Contains(captured.Name), "nothing is salvaged before touchdown");

        double landing = RunWithBase(() => captured.Active, timeout: 20);
        AssertEqual(TestSimStepD * 2, LandShip.SpacePortLandingSeconds(captured), landing, "the landing takes as long as a space port landing");
        Assert.IsFalse(captured.Dying, "a landed ship is taken in, not destroyed");
        AssertLessThan(captured.Position.Distance(Base.Position), LandShip.TouchdownRadius + 5f, "it touches down on the base");
        Assert.IsTrue(Pirates.ShipsWeCanSpawn.Contains(captured.Name), "the pirates salvage the design at touchdown");
    }

    [TestMethod]
    public void TheirOwnSpawnedShipLandsAndIsTakenInWithNoGain()
    {
        Ship spawned = PirateShipNearBase("Corsair");
        Pirates.SpawnedShips.Add(spawned.Id);
        int spawnable = Pirates.ShipsWeCanSpawn.Count;
        RunWithBase(() => !spawned.IsLanding);
        Assert.IsTrue(Pirates.SpawnedShips.Contains(spawned.Id), "nothing happens before touchdown");

        RunWithBase(() => spawned.Active, timeout: 20);
        Assert.IsFalse(spawned.Dying, "a landed ship is taken in, not destroyed");
        Assert.IsFalse(Pirates.SpawnedShips.Contains(spawned.Id), "the base takes back its own ship");
        AssertEqual(spawnable, Pirates.ShipsWeCanSpawn.Count, "with no salvage");
    }

    [TestMethod]
    public void ABigCapturedWarshipStaysOutToGuardTheBase()
    {
        Ship big = PirateShipNearBase("TEST_Excalibur-Class Supercarrier");
        Assert.IsTrue(big.ShipData.HullRole is RoleName.capital or RoleName.battleship or RoleName.cruiser, "setup: a big hull");
        RunWithBase(() => big.AI.State != AIState.Escort, timeout: 10);
        RunWithBase(() => true, timeout: 5, fatal: false);
        Assert.IsFalse(big.IsLanding || OrderedToLand(big), "a big captured warship is kept to escort the base, never landed");
        Assert.IsTrue(big.Active, "and stays in play");
    }

    [TestMethod]
    public void AShipLandingOnABaseThatIsLostIsLostWithIt()
    {
        Ship captured = PirateShipNearBase("Vulcan Scout");
        RunWithBase(() => !captured.IsLanding);

        Base.QueueTotalRemoval();
        RunSimWhile((simTimeout: 20, fatal: true), () => captured.Active);
        Assert.IsFalse(Pirates.ShipsWeCanSpawn.Contains(captured.Name), "a lost base salvages nothing");
    }

    [TestMethod]
    public void AShipLandingOnADyingBaseIsLostWithIt()
    {
        Ship captured = PirateShipNearBase("Vulcan Scout");
        RunWithBase(() => !captured.IsLanding);

        Base.SetDieTimer(60f);
        Base.Dying = true;
        RunSimWhile((simTimeout: 20, fatal: true), () => captured.Active);
        Assert.IsTrue(Base.Active, "setup: the base must still be dying when the ship touches down");
        Assert.IsFalse(Pirates.ShipsWeCanSpawn.Contains(captured.Name), "a dying base salvages nothing");
    }

    [TestMethod]
    public void TheFlagshipStaysOutToGuardTheBase()
    {
        Ship flagship = PirateShipNearBase(Faction.data.PirateFlagShip);
        Pirates.SpawnedShips.Add(flagship.Id);
        RunWithBase(() => flagship.AI.State != AIState.Escort, timeout: 10);
        RunWithBase(() => true, timeout: 5, fatal: false);
        Assert.IsFalse(flagship.IsLanding || OrderedToLand(flagship), "the flagship, spawned at the base, is kept to escort it, never landed");
        Assert.IsTrue(flagship.Active, "and stays in play");
    }

    [TestMethod]
    public void AShipCalledInWhenTheBaseIsLostFliesHomeElsewhere()
    {
        Ship captured = PirateShipNearBase("Vulcan Scout");
        RunWithBase(() => !OrderedToLand(captured), timeout: 5);
        Assert.IsFalse(captured.IsLanding, "setup: the ship must not be landing yet");

        Base.QueueTotalRemoval();
        RunSimWhile((simTimeout: 2, fatal: false));
        Assert.IsTrue(captured.Active, "the ship is not lost with a base it has not landed on");
        Assert.IsFalse(captured.IsLanding || OrderedToLand(captured), "and no longer tries to land on it");
    }

    [TestMethod]
    public void ALandingPirateShipIsTakenInAfterALoad()
    {
        UState.StarDate = 1042.5f;
        Ship captured = PirateShipNearBase("Vulcan Scout");
        RunWithBase(() => !captured.IsLanding);

        SavedGame save = Universe.Save("UnitTest.PirateBaseLanding", throwOnError: true);
        UniverseScreen loaded = LoadGame.Load(save.SaveFile, noErrorDialogs: true, startSimThread: false);
        Ship landing = loaded.UState.Objects.FindShip(captured.Id);
        Empire pirates = loaded.UState.GetEmpireById(Faction.Id);
        Assert.IsTrue(landing is { Active: true, IsLanding: true }, "a landing pirate ship must still be landing after a load");

        for (double time = 0; landing.Active; time += TestSimStepD)
        {
            AssertLessThan(time, 20.0, "the loaded landing must finish");
            loaded.UState.Objects.Update(TestSimStep);
        }
        Assert.IsFalse(landing.Dying, "a landed ship is taken in, not destroyed");
        Assert.IsTrue(pirates.Pirates.ShipsWeCanSpawn.Contains(captured.Name), "the loaded landing still salvages at touchdown");
    }
}
