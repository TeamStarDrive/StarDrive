using System;
using System.Reflection;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using SDGraphics;
using Ship_Game;
using Ship_Game.AI;
using Ship_Game.AI.Tasks;
using Ship_Game.Fleets;
using Ship_Game.GameScreens.LoadGame;
using Ship_Game.Ships;
using Matrix = SDGraphics.Matrix;
using Vector2 = SDGraphics.Vector2;
using BoundingFrustum = Microsoft.Xna.Framework.BoundingFrustum;
#pragma warning disable CA2213

namespace UnitTests.Ships;

/// <summary>
/// Troops reach a planet by landing. A troop ship lands on the planet, out of reach, and its troop lands at
/// touchdown, which spends the ship; at one of our colonies with a space port it lands on the port instead, and
/// cargo shuttles bring the troop down. An assault shuttle dives into the atmosphere for 2 seconds, runs on for 1,
/// drops its troop and climbs back out for 2; a carrier's shuttle then flies home, one without a carrier is spent.
/// A ship whose troop cannot land takes off again with it. A big troop ship at one of our colonies with a space
/// port still unloads at once, and cargo shuttles fly from the port to it and back.
/// </summary>
[TestClass]
public class TroopLandingTests : StarDriveTest
{
    readonly Planet Homeworld;
    readonly Planet Colony;
    readonly Planet EnemyPlanet;
    readonly Ship Carrier;

    public TroopLandingTests()
    {
        LoadStarterShips("TEST_Excalibur-Class Supercarrier", "Assault Shuttle", "Terran Assault Shuttle");
        CreateUniverseAndPlayerEmpire();
        LoadStarterShips(Player.data.DefaultTroopShip);
        UnlockAllShipsFor(Player);
        UState.Objects.EnableParallelUpdate = false;
        Universe.NotificationManager = new NotificationManager(Universe.ScreenManager, Universe);
        CreateThirdMajorEmpire();
        Player.GetRelations(Enemy).TurnsAtWar = 5;
        Homeworld = AddHomeWorldToEmpire(new Vector2(200_000), Player, new Vector2(205_000), explored: true);
        Colony = AddHomeWorldToEmpire(new Vector2(300_000), Player, new Vector2(305_000), explored: true);
        Colony.HasSpacePort = false;
        EnemyPlanet = AddHomeWorldToEmpire(new Vector2(400_000), Enemy, new Vector2(405_000), explored: true);
        EnemyPlanet.HasSpacePort = false;
        Carrier = SpawnShip("TEST_Excalibur-Class Supercarrier", Player, EnemyPlanet.Position + new Vector2(EnemyPlanet.Radius + 4000, 0));
        Carrier.Carrier.AllowBoardShip = false;
        Universe.Frustum = new BoundingFrustum(Matrix.CreateOrthographicOffCenter(0, 1_000_000, 1_000_000, 0, -10_000, 10_000));
        UState.ViewState = UniverseScreen.UnivScreenState.PlanetView;
        Assert.IsTrue(Homeworld.HasSpacePort, "setup: our homeworld has a space port");
    }

    double StepWhile(Func<bool> condition, double timeout = 30, Action body = null)
        => StepWhile(Universe, condition, timeout, body);

    double StepWhile(UniverseScreen universe, Func<bool> condition, double timeout = 30, Action body = null)
    {
        double elapsed = 0;
        for (; condition(); elapsed += TestSimStepD)
        {
            body?.Invoke();
            if (elapsed >= timeout)
                throw new TimeoutException("Timed out in StepWhile");
            universe.UState.Objects.Update(TestSimStep);
            universe.CargoShuttles.Update(universe, TestSimStep);
        }
        return elapsed;
    }

    double RunUntilLanding(Ship ship) => StepWhile(() => !ship.IsLanding, timeout: 60);

    double RunUntilTouchdown(Ship ship) => StepWhile(() => ship is { Active: true, IsLanding: true });

    Ship TroopShip(Planet near)
    {
        Ship troopShip = SpawnShip(Player.data.DefaultTroopShip, Player, near.Position + new Vector2(near.Radius + 3000, 0));
        ResourceManager.CreateTroop("Wyvern", Player).LandOnShip(troopShip);
        return troopShip;
    }

    Ship ShuttleWithoutCarrier(Planet near)
    {
        Ship shuttle = SpawnShip(Player.GetAssaultShuttleName(), Player, near.Position + new Vector2(near.Radius + 2000, 0));
        ResourceManager.CreateTroop("Wyvern", Player).LandOnShip(shuttle);
        return shuttle;
    }

    // launched without the take-off: one that ends with the carrier out of combat sends the shuttle straight home
    Ship CarrierShuttle(Planet near)
    {
        Carrier.Position = near.Position + new Vector2(near.Radius + 4000, 0);
        Assert.IsTrue(Carrier.GetOurFirstTroop(out Troop troop), "setup: the carrier must carry troops");
        UState.P.DebugDisableShipLaunch = true;
        Assert.IsTrue(Carrier.Carrier.TryScrambleSingleAssaultShuttle(troop, out Ship shuttle), "setup: the carrier must launch an assault shuttle");
        UState.P.DebugDisableShipLaunch = false;
        shuttle.Position = near.Position + new Vector2(near.Radius + 2000, 0);
        return shuttle;
    }

    int Troops(Planet planet) => planet.CountEmpireTroops(Player);

    [TestMethod]
    public void ATroopShipInvadingLandsOnThePlanetAndItsTroopLandsAtTouchdown()
    {
        Ship troopShip = TroopShip(EnemyPlanet);
        troopShip.AI.OrderLandAllTroops(EnemyPlanet, clearOrders: true);
        RunUntilLanding(troopShip);
        Assert.IsFalse(troopShip.LandShip.OnDock || troopShip.LandShip.Dives, "a troop ship lands on the planet itself");
        AssertEqual(1, troopShip.TroopCount, "its troop stays aboard while it lands");

        double seconds = RunUntilTouchdown(troopShip);
        AssertEqual(0.05f, LaunchShip.PlanetDuration(troopShip), (float)seconds, "the normal planet landing");
        AssertEqual(1, Troops(EnemyPlanet), "its troop lands at touchdown");
        Assert.IsTrue(troopShip is { Active: false, Dying: false }, "and the troop ship is spent");
    }

    [TestMethod]
    public void ATroopShipThatLostItsTroopIsSpentAtTouchdown()
    {
        Ship troopShip = TroopShip(EnemyPlanet);
        troopShip.AI.OrderLandAllTroops(EnemyPlanet, clearOrders: true);
        RunUntilLanding(troopShip);
        troopShip.KillOneOfOurTroops();

        RunUntilTouchdown(troopShip);
        Assert.IsTrue(troopShip is { Active: false, Dying: false }, "an empty troop ship is spent, it does not take off to land again");
    }

    [TestMethod]
    public void OurShipsLandingTroopsCountAsInvading()
    {
        Ship troopShip = TroopShip(EnemyPlanet);
        troopShip.AI.OrderLandAllTroops(EnemyPlanet, clearOrders: true);
        RunObjectsSim(TestSimStep);
        AssertEqual(0, EnemyPlanet.ShipsLandingTroopsHere(Player), "setup: nobody is landing yet");

        RunUntilLanding(troopShip);
        AssertEqual(1, EnemyPlanet.ShipsLandingTroopsHere(Player),
                    "a ship landing troops counts as invading, so a fleet does not send more than the free tiles take");
        RunUntilTouchdown(troopShip);
        AssertEqual(0, EnemyPlanet.ShipsLandingTroopsHere(Player), "until its troop holds the tile");
    }

    [TestMethod]
    public void AnInvasionFleetSendsNoTroopShipForATileATroopIsAlreadyLandingOn()
    {
        for (int i = 0; i < 100 && EnemyPlanet.GetFreeTiles(Player) > 1; ++i)
            ResourceManager.CreateTroop("Wyvern", Player).TryLandTroop(EnemyPlanet);
        AssertEqual(1, EnemyPlanet.GetFreeTiles(Player), "setup: one tile is left to land on");
        Ship landing = TroopShip(EnemyPlanet);
        landing.AI.OrderLandAllTroops(EnemyPlanet, clearOrders: true);
        RunUntilLanding(landing);

        Ship reserve = TroopShip(EnemyPlanet);
        var task = new MilitaryTask(EnemyPlanet, Player, MilitaryTaskImportance.Normal);
        var fleet = new Fleet(UState, Player) { FleetTask = task };
        typeof(Fleet).GetMethod("OrderShipsToInvade", BindingFlags.Instance | BindingFlags.NonPublic)!
                     .Invoke(fleet, new object[] { new[] { reserve }, task, true });
        Assert.AreNotEqual(AIState.AssaultPlanet, reserve.AI.State,
                           "the troop already landing takes the last tile, so the fleet keeps its reserve troop ship back");
    }

    [TestMethod]
    public void AShipMadeWhilePausedJoinsOurShipsOnTheNextPausedFrame()
    {
        Ship troopShip = TroopShip(Colony);
        UState.Objects.Update(FixedSimTime.Zero);

        bool owned = false;
        foreach (Ship ship in Player.OwnedShips)
            owned |= ship == troopShip;
        Assert.IsTrue(owned, "a ship made while the game is paused is in our ship list on the next paused frame, so Call Troops counts it");
    }

    [TestMethod]
    public void EachLandingOrderPicksAFreshDropSpot()
    {
        Ship shuttle = ShuttleWithoutCarrier(EnemyPlanet);
        shuttle.AI.LandingOffset = new Vector2(5000, 0);
        shuttle.AI.OrderLandAllTroops(EnemyPlanet, clearOrders: true);
        AssertEqual(Vector2.Zero, shuttle.AI.LandingOffset, "a spot picked for another planet is not reused");
        shuttle.AI.LandingOffset = new Vector2(5000, 0);
        shuttle.AI.OrderRebase(Colony, clearOrders: true);
        AssertEqual(Vector2.Zero, shuttle.AI.LandingOffset, "nor for a rebase");
    }

    [TestMethod]
    public void ATroopShipNeverLandsOnAnEnemySpacePort()
    {
        EnemyPlanet.HasSpacePort = true;
        Ship troopShip = TroopShip(EnemyPlanet);
        troopShip.AI.OrderLandAllTroops(EnemyPlanet, clearOrders: true);
        RunUntilLanding(troopShip);
        Assert.IsFalse(troopShip.LandShip.OnDock, "an invading troop ship lands on the planet, not on the enemy's space port");
    }

    [TestMethod]
    public void ATroopShipFindingThePlanetAtPeaceAtTouchdownTakesOffWithItsTroop()
    {
        Ship troopShip = TroopShip(EnemyPlanet);
        troopShip.AI.OrderLandAllTroops(EnemyPlanet, clearOrders: true);
        RunUntilLanding(troopShip);
        EnemyPlanet.SetOwner(ThirdMajor);
        Assert.IsFalse(Player.IsAtWarWith(ThirdMajor), "setup: the planet changes hands to an empire we are at peace with");

        RunUntilTouchdown(troopShip);
        Assert.IsTrue(troopShip is { Active: true, IsLaunching: true }, "the troop ship takes off again");
        AssertEqual(1, troopShip.TroopCount, "with its troop");
        AssertEqual(0, EnemyPlanet.CountEmpireTroops(Player), "which never landed");
        AssertEqual(AIState.Rebase, troopShip.AI.State, "and, with nothing else to do, rebases to one of our colonies");
    }

    [TestMethod]
    public void ATroopShipRebasingToOurColonyWithASpacePortLandsOnThePortAndShuttlesBringItsTroopDown()
    {
        int troops = Troops(Homeworld);
        Ship troopShip = TroopShip(Homeworld);
        troopShip.AI.OrderRebase(Homeworld, clearOrders: true);
        RunUntilLanding(troopShip);
        Assert.IsTrue(troopShip.LandShip.OnSpacePort, "a troop ship rebasing to our colony lands on its space port, like a freighter");
        AssertEqual(troops, Troops(Homeworld), "its troop is not on the planet before touchdown");

        double seconds = RunUntilTouchdown(troopShip);
        AssertEqual(0.05f, LandShip.SpacePortLandingSeconds(troopShip), (float)seconds, "the space port landing");
        AssertEqual(troops + 1, Troops(Homeworld), "its troop goes down to the planet at touchdown");
        Assert.IsTrue(troopShip is { Active: false, Dying: false }, "and the troop ship is spent");
        AssertEqual(CargoShuttles.TroopShuttles, Universe.CargoShuttles.InFlight(Homeworld, rising: false),
                    "cargo shuttles fly from the port down to the planet");
    }

    [TestMethod]
    public void ATroopShipRebasingToOurColonyWithoutASpacePortLandsOnThePlanet()
    {
        Ship troopShip = TroopShip(Colony);
        troopShip.AI.OrderRebase(Colony, clearOrders: true);
        RunUntilLanding(troopShip);
        Assert.IsFalse(troopShip.LandShip.OnDock || troopShip.LandShip.Dives, "a troop ship lands on a colony with no space port");

        RunUntilTouchdown(troopShip);
        AssertEqual(1, Troops(Colony), "its troop lands at touchdown");
        Assert.IsTrue(troopShip is { Active: false, Dying: false }, "and the troop ship is spent");
    }

    [TestMethod]
    public void ATroopShipOrShuttleLandingOnOurColonyStillTakesUpATile()
    {
        int free = Colony.FreeTilesWithRebaseOnTheWay(Player);
        Ship troopShip = TroopShip(Colony);
        troopShip.AI.OrderRebase(Colony, clearOrders: true);
        Ship shuttle = ShuttleWithoutCarrier(Colony);
        shuttle.AI.OrderRebase(Colony, clearOrders: true);
        RunObjectsSim(TestSimStep);
        AssertEqual(free - 2, Colony.FreeTilesWithRebaseOnTheWay(Player), "setup: each troop on its way takes up a tile");

        StepWhile(() => !shuttle.IsLanding, timeout: 60);
        Assert.IsTrue(shuttle.LandShip.Dives, "setup: the shuttle dives");
        AssertEqual(free - 2, Colony.FreeTilesWithRebaseOnTheWay(Player),
                    "a diving shuttle keeps its tile, so no other troop is sent for it");

        StepWhile(() => !troopShip.IsLanding, timeout: 60);
        AssertEqual(free - 2, Colony.FreeTilesWithRebaseOnTheWay(Player),
                    "and so does a landing troop ship");
    }

    [TestMethod]
    public void ATroopShipFindingOurColonyFullAtTouchdownTakesOffAndRebasesElsewhere()
    {
        Ship troopShip = TroopShip(Colony);
        troopShip.AI.OrderRebase(Colony, clearOrders: true);
        RunUntilLanding(troopShip);
        while (Colony.GetFreeTiles(Player) > 0)
            Assert.IsTrue(ResourceManager.CreateTroop("Wyvern", Player).TryLandTroop(Colony), "setup: fill the colony with troops");

        RunUntilTouchdown(troopShip);
        Assert.IsTrue(troopShip is { Active: true, IsLaunching: true }, "the troop ship takes off again");
        AssertEqual(1, troopShip.TroopCount, "with its troop");
        Assert.IsTrue(troopShip.AI.FindGoal(ShipAI.Plan.Rebase, out ShipAI.ShipGoal rebase) && rebase.TargetPlanet == Homeworld,
                      "and rebases to a colony that has room, instead of landing on the full one again");
    }

    [TestMethod]
    public void ACarriersAssaultShuttleDivesRunsDropsItsTroopAndClimbsBackOutToFlyHome()
    {
        Ship shuttle = CarrierShuttle(EnemyPlanet);
        shuttle.AI.OrderLandAllTroops(EnemyPlanet, clearOrders: true);
        RunUntilLanding(shuttle);
        Assert.IsTrue(shuttle.LandShip.Dives, "an assault shuttle dives into the atmosphere");
        Vector2 diveStart = shuttle.Position;

        double seconds = RunUntilTouchdown(shuttle);
        AssertEqual(0.05f, LandShip.DiveSeconds + LandShip.RunSeconds, (float)seconds, "it dives for 2 seconds and runs on for 1");
        AssertGreaterThan(shuttle.Position.Distance(diveStart), 50f, "it keeps moving through the dive and the run");
        AssertLessThan(shuttle.Position.Distance(EnemyPlanet.Position + shuttle.AI.LandingOffset), 200f,
                       "it starts diving early enough to drop its troop over the spot it picked on the planet");
        AssertEqual(1, Troops(EnemyPlanet), "then its troop lands");
        AssertEqual(0, shuttle.TroopCount, "and leaves the shuttle");
        AssertEqual(CargoShuttles.TroopShuttles, Universe.CargoShuttles.InFlight(EnemyPlanet, rising: false),
                    "cargo shuttles fly down from the shuttle to the planet");

        Assert.IsTrue(shuttle is { Active: true, IsLaunching: true }, "the carrier's shuttle climbs back out");
        seconds = StepWhile(() => shuttle.IsLaunching);
        AssertEqual(0.05f, LaunchShip.AssaultClimbSeconds, (float)seconds, "for 2 seconds");
        AssertEqual(AIState.ReturnToHangar, shuttle.AI.State, "and flies home");

        RunUntilLanding(shuttle);
        Assert.IsTrue(shuttle.LandShip.InHangar, "to land on its carrier");
    }

    [TestMethod]
    public void ADroppingShuttleIsOutOfReachUntilItIsBackUp()
    {
        Ship shuttle = CarrierShuttle(EnemyPlanet);
        shuttle.AI.OrderLandAllTroops(EnemyPlanet, clearOrders: true);
        RunUntilLanding(shuttle);

        double outOfReach = LandShip.DiveSeconds + LandShip.RunSeconds + LaunchShip.AssaultClimbSeconds - 0.05;
        for (double time = 0; time < outOfReach; time += TestSimStepD)
        {
            Assert.IsTrue(shuttle.IsLaunchingOrLanding, $"the shuttle is out of reach through the dive, the run and the climb ({time:0.00} s)");
            UState.Objects.Update(TestSimStep);
        }
        StepWhile(() => shuttle.IsLaunchingOrLanding, timeout: 1);
        Assert.IsTrue(shuttle is { Active: true, IsLaunchingOrLanding: false }, "and back in play once it is up");
    }

    [TestMethod]
    public void ACarriersAssaultShuttleDeliveringToOurColonyDivesToo()
    {
        Ship shuttle = CarrierShuttle(Homeworld);
        int troops = Troops(Homeworld);
        shuttle.AI.OrderLandAllTroops(Homeworld, clearOrders: true);
        RunUntilLanding(shuttle);
        Assert.IsTrue(shuttle.LandShip.Dives, "a carrier's shuttle dives even at our colony with a space port");

        RunUntilTouchdown(shuttle);
        AssertEqual(troops + 1, Troops(Homeworld), "its troop lands at the end of the run");
        Assert.IsTrue(shuttle is { Active: true, IsLaunching: true }, "and the shuttle climbs back out");
    }

    [TestMethod]
    public void AnAssaultShuttleWithoutACarrierIsSpentAtTheDrop()
    {
        Ship shuttle = ShuttleWithoutCarrier(EnemyPlanet);
        shuttle.AI.OrderLandAllTroops(EnemyPlanet, clearOrders: true);
        RunUntilLanding(shuttle);
        Assert.IsTrue(shuttle.LandShip.Dives, "an assault shuttle dives into the atmosphere");

        RunUntilTouchdown(shuttle);
        AssertEqual(1, Troops(EnemyPlanet), "its troop lands at the end of the run");
        Assert.IsTrue(shuttle is { Active: false, Dying: false }, "and a shuttle with no carrier is spent");
    }

    [TestMethod]
    public void AnAssaultShuttleWithoutACarrierRebasingToOurColonyWithASpacePortLandsOnThePort()
    {
        Ship shuttle = ShuttleWithoutCarrier(Homeworld);
        shuttle.AI.OrderRebase(Homeworld, clearOrders: true);
        RunUntilLanding(shuttle);
        Assert.IsTrue(shuttle.LandShip.OnSpacePort, "a shuttle with no carrier rebasing to our colony lands on its space port");
    }

    [TestMethod]
    public void AnAssaultShuttleWhoseTroopCannotLandClimbsBackOutWithIt()
    {
        Ship shuttle = CarrierShuttle(EnemyPlanet);
        shuttle.AI.OrderLandAllTroops(EnemyPlanet, clearOrders: true);
        RunUntilLanding(shuttle);
        EnemyPlanet.SetOwner(ThirdMajor);

        RunUntilTouchdown(shuttle);
        Assert.IsTrue(shuttle is { Active: true, IsLaunching: true }, "the shuttle climbs back out");
        AssertEqual(1, shuttle.TroopCount, "with its troop");
        AssertEqual(AIState.ReturnToHangar, shuttle.AI.State, "to bring it home");
    }

    [TestMethod]
    public void ABigTroopShipUnloadsThroughOurSpacePortAtOnceWhileShuttlesFlyToItAndBack()
    {
        Carrier.Position = Homeworld.Position + new Vector2(Homeworld.Radius + 1500, 0);
        int carried = Carrier.TroopCount;
        int troops = Troops(Homeworld);
        Carrier.AI.OrderLandAllTroops(Homeworld, clearOrders: true);
        StepWhile(() => Carrier.HasOurTroops, timeout: 60);
        AssertEqual(troops + carried, Troops(Homeworld), "every troop lands through the space port at once");
        AssertEqual(CargoShuttles.TroopShuttles, Universe.CargoShuttles.InFlight(Homeworld, rising: true),
                    "cargo shuttles fly up from the port to the troop ship");
        StepWhile(() => Universe.CargoShuttles.InFlight(Homeworld, rising: false) == 0, timeout: 10);
        AssertEqual(CargoShuttles.TroopShuttles, Universe.CargoShuttles.InFlight(Homeworld), "and come back down to the port");
        StepWhile(() => Universe.CargoShuttles.InFlight(Homeworld) > 0, timeout: 10);
    }

    [TestMethod]
    public void ADiveSavedHalfwayEndsWithTheDropAfterLoading()
    {
        Ship shuttle = CarrierShuttle(EnemyPlanet);
        shuttle.AI.OrderLandAllTroops(EnemyPlanet, clearOrders: true);
        RunUntilLanding(shuttle);
        RunObjectsSim(1.5f);
        Vector2 offset = shuttle.Position - EnemyPlanet.Position;
        UState.StarDate = 1042.5f;
        SavedGame save = Universe.Save("UnitTest.TroopLanding", throwOnError: true);
        UniverseScreen loaded = LoadGame.Load(save.SaveFile, noErrorDialogs: true, startSimThread: false);
        Ship diving = loaded.UState.Objects.FindShip(shuttle.Id);
        Planet planet = loaded.UState.GetPlanet(EnemyPlanet.Id);
        Assert.IsTrue(diving is { Active: true, IsLanding: true }, "setup: the shuttle is still diving after the load");

        loaded.UState.Objects.Update(TestSimStep);
        AssertLessThan(diving.Position.Distance(planet.Position + offset), 20f, "it carries on from where it was");
        double seconds = StepWhile(loaded, () => diving is { Active: true, IsLanding: true });
        AssertLessThan((float)seconds, 2f, "it finishes the dive it had started");
        AssertEqual(1, planet.CountEmpireTroops(loaded.UState.Player), "and drops its troop");
        Assert.IsTrue(diving.IsLaunching, "then climbs back out");
    }
}
