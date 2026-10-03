using Microsoft.VisualStudio.TestTools.UnitTesting;
using SDGraphics;
using SDUtils;
using Ship_Game;
using Ship_Game.AI;
using Ship_Game.Ships;
using Vector2 = SDGraphics.Vector2;
#pragma warning disable CA2213

namespace UnitTests.Ships;

/// <summary>
/// Troops reach another ship by landing on it: an assault shuttle or troop ship glides in from where boarding has
/// always started, the target's radius plus 300, onto the outer module nearest to it, and stays on that spot as the
/// ship turns. Its troop goes aboard only at touchdown, handed over on the sim thread. A carrier's shuttle then takes
/// off and flies home; a troop ship is spent. A shuttle whose target is lost before touchdown takes off with its troop.
/// </summary>
[TestClass]
public class BoardingLandingTests : StarDriveTest
{
    readonly Ship Carrier;
    readonly Ship Station;

    public BoardingLandingTests()
    {
        LoadStarterShips("TEST_Excalibur-Class Supercarrier", "Basic Mining Station", "Assault Shuttle", "Terran Assault Shuttle");
        CreateUniverseAndPlayerEmpire();
        LoadStarterShips(Player.data.DefaultTroopShip);
        UnlockAllShipsFor(Player);
        UState.Objects.EnableParallelUpdate = false;
        CreateThirdMajorEmpire();
        Carrier = SpawnShip("TEST_Excalibur-Class Supercarrier", Player, new Vector2(300_000));
        Carrier.Carrier.AllowBoardShip = false;
        Station = SpawnShip("Basic Mining Station", ThirdMajor, Carrier.Position + new Vector2(6000, 0));
        Assert.IsFalse(Player.IsAtWarWith(ThirdMajor), "setup: a target nobody fights, so only the boarding touches it");
    }

    // launched without the take-off: one that ends with the carrier out of combat sends the shuttle straight home
    Ship LaunchShuttle()
    {
        Assert.IsTrue(Carrier.GetOurFirstTroop(out Troop troop), "setup: the carrier must carry troops");
        UState.P.DebugDisableShipLaunch = true;
        Assert.IsTrue(Carrier.Carrier.TryScrambleSingleAssaultShuttle(troop, out Ship shuttle), "setup: the carrier must launch an assault shuttle");
        UState.P.DebugDisableShipLaunch = false;
        return shuttle;
    }

    Ship ShuttleSentToBoard(Ship target)
    {
        Ship shuttle = LaunchShuttle();
        shuttle.Position = target.Position - new Vector2(LandShip.BoardingRange(target) + 1000f, 0);
        shuttle.AI.OrderTroopToBoardShip(target);
        return shuttle;
    }

    Ship TroopShip(Empire owner, Vector2 position)
    {
        Ship troopShip = SpawnShip(owner.data.DefaultTroopShip, owner, position);
        ResourceManager.CreateTroop("Wyvern", owner).LandOnShip(troopShip);
        return troopShip;
    }

    void RunUntilLanding(Ship ship) => RunSimWhile((simTimeout: 30, fatal: true), () => !ship.IsLanding);

    void RunUntilTouchdown(Ship ship) => RunSimWhile((simTimeout: 10, fatal: true), () => ship.IsLanding);

    [TestMethod]
    public void AShuttleLandsOnTheNearSideOfTheShipItBoardsAndItsTroopGoesAboardAtTouchdown()
    {
        Ship shuttle = ShuttleSentToBoard(Station);
        RunUntilLanding(shuttle);

        AssertLessThan(shuttle.Position.Distance(Station.Position), LandShip.BoardingRange(Station) + 1f,
                       "the landing starts where boarding always started, within the target's radius plus 300");
        AssertEqual(1, shuttle.TroopCount, "the troop stays in the shuttle while it lands");
        Assert.IsFalse(Station.TroopsAreBoardingShip, "nobody is aboard the target before touchdown");

        RunUntilTouchdown(shuttle);
        AssertLessThan(shuttle.Position.Distance(Station.Position), Station.Radius, "the shuttle touches down on the target's hull");
        AssertLessThan(shuttle.Position.X, Station.Position.X, "on the side it came from");
        Assert.IsTrue(Station.TroopsAreBoardingShip, "its troop goes aboard at touchdown");
        AssertEqual(0, shuttle.TroopCount, "and leaves the shuttle");
        Assert.IsTrue(shuttle is { Active: true, IsLaunching: true }, "the carrier's shuttle takes off again");
        AssertEqual(AIState.ReturnToHangar, shuttle.AI.State, "to fly home");
    }

    [TestMethod]
    public void TheTouchdownSpotTurnsWithTheBoardedShip()
    {
        Ship shuttle = ShuttleSentToBoard(Station);
        RunUntilLanding(shuttle);

        Station.Rotation = (Station.Rotation + RadMath.PI).AsNormalizedRadians();
        RunUntilTouchdown(shuttle);
        AssertLessThan(shuttle.Position.Distance(Station.Position), Station.Radius, "the shuttle touches down on the target's hull");
        AssertGreaterThan(shuttle.Position.X, Station.Position.X, "on the spot it aimed for, now on the far side after a half turn");
    }

    [TestMethod]
    public void AShuttleWhoseTargetIsLostBeforeTouchdownTakesOffWithItsTroop()
    {
        Ship shuttle = ShuttleSentToBoard(Station);
        RunUntilLanding(shuttle);

        Station.QueueTotalRemoval();
        RunUntilTouchdown(shuttle);
        Assert.IsTrue(shuttle is { Active: true, IsLaunching: true }, "the shuttle takes off again");
        AssertEqual(1, shuttle.TroopCount, "with its troop, which never got aboard");

        RunSimWhile((simTimeout: 10, fatal: true), () => shuttle.IsLaunching);
        AssertEqual(AIState.ReturnToHangar, shuttle.AI.State, "and heads home");
    }

    [TestMethod]
    public void AShuttleThatBoardedFliesHomeEvenWhileItsCarrierHasFightersOut()
    {
        Carrier.Carrier.FightersOut = true;
        Ship shuttle = ShuttleSentToBoard(Station);
        RunUntilLanding(shuttle);
        RunUntilTouchdown(shuttle);

        RunSimWhile((simTimeout: 10, fatal: true), () => shuttle.IsLaunching);
        Assert.IsFalse(Carrier.InCombat, "setup: a carrier out of combat, whose launched ships escort it while its fighters are out");
        AssertEqual(AIState.ReturnToHangar, shuttle.AI.State, "a shuttle already sent home keeps flying home after its take-off");
    }

    [TestMethod]
    public void ATroopShipIsSpentWhenItsTroopGoesAboard()
    {
        Ship troopShip = TroopShip(Player, Station.Position - new Vector2(LandShip.BoardingRange(Station) + 1000f, 0));
        troopShip.AI.OrderTroopToBoardShip(Station);
        RunUntilLanding(troopShip);
        Assert.IsFalse(Station.TroopsAreBoardingShip, "nobody is aboard the target before touchdown");

        RunSimWhile((simTimeout: 10, fatal: true), () => troopShip.Active);
        Assert.IsFalse(troopShip.Dying, "the troop ship is spent, not destroyed");
        Assert.IsTrue(Station.TroopsAreBoardingShip, "its troop went aboard at touchdown");
    }

    [TestMethod]
    public void TroopsSentToOneOfOurShipsLandOnIt()
    {
        Carrier.KillOneOfOurTroops();
        int troops = Carrier.TroopCount;
        Ship troopShip = TroopShip(Player, Carrier.Position - new Vector2(LandShip.BoardingRange(Carrier) + 1000f, 0));
        troopShip.AI.OrderRebaseToShip(Carrier);
        RunUntilLanding(troopShip);
        AssertEqual(troops, Carrier.TroopCount, "the troop is not aboard before touchdown");

        RunSimWhile((simTimeout: 10, fatal: true), () => troopShip.Active);
        Assert.IsFalse(troopShip.Dying, "the troop ship is spent, not destroyed");
        AssertEqual(troops + 1, Carrier.TroopCount, "its troop went aboard at touchdown");
    }

    [TestMethod]
    public void ATroopShipWaitsForOurShipToFinishTakingOff()
    {
        Carrier.KillOneOfOurTroops();
        Carrier.InitLaunch(LaunchPlan.Planet, 0f);
        Ship troopShip = TroopShip(Player, Carrier.Position - new Vector2(LandShip.BoardingRange(Carrier) - 100f, 0));
        troopShip.AI.OrderRebaseToShip(Carrier);

        RunObjectsSim(1f);
        Assert.IsTrue(Carrier.IsLaunching, "setup: our ship is still taking off");
        Assert.IsFalse(troopShip.IsLanding, "a troop ship does not land on a ship that is taking off");
    }

    [TestMethod]
    public void AShipHoldingMoreTroopsThanItsCapacityIsFull()
    {
        ResourceManager.CreateTroop("Wyvern", Player).LandOnShip(Carrier);
        AssertGreaterThan(Carrier.TroopCount, Carrier.TroopCapacity, "setup: returning shuttles can overfill a carrier");
        Ship troopShip = TroopShip(Player, Carrier.Position - new Vector2(LandShip.BoardingRange(Carrier) - 100f, 0));
        troopShip.AI.OrderRebaseToShip(Carrier);

        for (double time = 0; time < 3.0; time += TestSimStepD)
        {
            Assert.IsFalse(troopShip.IsLanding, "a troop ship does not land on a ship that is already over full");
            RunObjectsSim(TestSimStep);
        }
        AssertEqual(1, troopShip.TroopCount, "it keeps its troop");
    }

    Ship EmptyTroopShipNearATroopShipSentToIt(out Ship troopShip)
    {
        Ship target = SpawnShip(Player.data.DefaultTroopShip, Player, new Vector2(300_000, 310_000));
        AssertGreaterThan(target.TroopCapacity, target.TroopCount, "setup: a ship with room for a troop");
        troopShip = TroopShip(Player, target.Position - new Vector2(LandShip.BoardingRange(target) - 50f, 0));
        troopShip.AI.OrderRebaseToShip(target);
        return target;
    }

    [TestMethod]
    public void NothingLandsOnADyingShip()
    {
        Ship target = EmptyTroopShipNearATroopShipSentToIt(out Ship troopShip);
        target.Dying = true;

        troopShip.AI.Update(TestSimStep);
        Assert.IsTrue(target is { Active: true, Dying: true }, "setup: the ship is still breaking up");
        Assert.IsFalse(troopShip.IsLanding, "a troop ship does not land on a dying ship");
    }

    [TestMethod]
    public void NothingLandsOnAShipAboutToJump()
    {
        Ship target = EmptyTroopShipNearATroopShipSentToIt(out Ship troopShip);
        target.EngageStarDrive();
        Assert.IsTrue(target.IsSpoolingOrInWarp, "setup: the ship is spooling its warp drive");

        RunObjectsSim(TestSimStep);
        Assert.IsTrue(target.IsSpoolingOrInWarp, "setup: the ship is still about to jump");
        Assert.IsFalse(troopShip.IsLanding, "a troop ship does not land on a ship about to jump, which would carry it off at warp");
    }

    [TestMethod]
    public void ATroopShipLandingOnOurShipIsNotIdle()
    {
        Carrier.KillOneOfOurTroops();
        Carrier.Carrier.SetSendTroopsToShip(false);
        Ship troopShip = TroopShip(Player, Carrier.Position - new Vector2(LandShip.BoardingRange(Carrier) + 1000f, 0));
        troopShip.AI.OrderTroopToShip(Carrier);
        Assert.IsTrue(troopShip.IsIdleSingleTroopship, "setup: a troop ship moving troops to our ship counts as idle on its way");

        RunUntilLanding(troopShip);
        AssertEqual(AIState.AwaitingOrders, troopShip.AI.State, "setup: nobody sent it elsewhere on its way");
        Assert.IsFalse(troopShip.IsIdleSingleTroopship, "but it is not idle while it lands, so no one can send it elsewhere mid-landing");
    }

    [TestMethod]
    public void ATroopShipFindingOurShipFullAtTouchdownTakesOffWithItsTroop()
    {
        Carrier.KillOneOfOurTroops();
        Ship troopShip = TroopShip(Player, Carrier.Position - new Vector2(LandShip.BoardingRange(Carrier) + 1000f, 0));
        troopShip.AI.OrderRebaseToShip(Carrier);
        RunUntilLanding(troopShip);

        ResourceManager.CreateTroop("Wyvern", Player).LandOnShip(Carrier);
        AssertEqual(Carrier.TroopCapacity, Carrier.TroopCount, "setup: the ship fills up while the troop ship lands");
        RunUntilTouchdown(troopShip);
        Assert.IsTrue(troopShip is { Active: true, IsLaunching: true }, "the troop ship takes off again");
        AssertEqual(1, troopShip.TroopCount, "with its troop");
    }
}
