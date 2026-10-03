using System.Linq;
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
/// Fighters, drones and supply shuttles land on their carrier the way they launch from it, played backwards,
/// as mining ships do on their station. A ship at full health may barrel-roll in, as it may roll out at launch.
/// The carrier takes it back, with its ordnance, and frees its hangar only once it has touched down.
/// </summary>
[TestClass]
public class HangarLandingTests : StarDriveTest
{
    readonly Ship Carrier;

    public HangarLandingTests()
    {
        LoadStarterShips("TEST_Heavy Carrier mk1", "TEST_Excalibur-Class Supercarrier", "Fang Strafer",
                         "Unarmed Scout", "X-Ving mk1", "Assault Shuttle", "Terran Assault Shuttle");
        CreateUniverseAndPlayerEmpire();
        UnlockAllShipsFor(Player);
        UState.Objects.EnableParallelUpdate = false;
        Carrier = SpawnShip("TEST_Heavy Carrier mk1", Player, new Vector2(300_000));
    }

    static ShipModule HangarOf(Ship carrier, Ship hangarShip) => carrier.Carrier.AllHangars.Find(h => h.HangarShip == hangarShip);

    // dynamic hangars pick the strongest design loaded, so the fighter is placed out of its hangar by hand
    Ship FighterOut()
    {
        Ship fighter = SpawnShip("Fang Strafer", Player, Carrier.Position + new Vector2(0, 800));
        Carrier.OnShipLaunched(fighter, Carrier.Carrier.AllFighterHangars.First());
        Assert.IsTrue(fighter is { DesignRole: RoleName.fighter, IsHangarShip: true }, "setup: a fighter out of its carrier");
        return fighter;
    }

    void RunUntilLanding(Ship ship) => RunSimWhile((simTimeout: 30, fatal: true), () => !ship.IsLanding);

    [TestMethod]
    public void AFighterLandsOnItsCarrierTheWayItLaunched()
    {
        Ship fighter = FighterOut();
        ShipModule hangar = HangarOf(Carrier, fighter);
        fighter.Position = Carrier.Position + new Vector2(3000, 0);
        fighter.AI.OrderReturnToHangar();
        float inSpace = Carrier.Carrier.OrdnanceInSpace;
        RunUntilLanding(fighter);

        AssertEqual(420f, LandShip.HangarLandingRange(fighter), "the Fang Strafer glides in at its launch speed, 300, for the launch's 1.4 seconds");
        AssertLessThan(fighter.Position.Distance(Carrier.Position), LandShip.HangarLandingRange(fighter) + 1f, "the landing starts within the glide range");
        AssertEqual(AIState.ReturnToHangar, fighter.AI.State, "a landing fighter is still returning to its hangar");
        Assert.AreSame(fighter, hangar.HangarShip, "the hangar still holds its fighter while it lands, so it launches no other");
        AssertEqual(inSpace, Carrier.Carrier.OrdnanceInSpace, "the carrier takes nothing back before touchdown");

        double landing = RunSimWhile((simTimeout: 10, fatal: true), () => fighter.Active);
        AssertEqual(TestSimStepD * 2, LaunchShip.HangarDuration(fighter), landing, "the landing takes as long as the hangar launch");
        Assert.IsFalse(fighter.Dying, "a landed fighter is taken in, not destroyed");
        AssertLessThan(fighter.Position.Distance(Carrier.Position), 1f, "the fighter touches down at the carrier's centre");
        AssertEqual(0.01f, -31.5f, fighter.XRotation.ToDegrees(), "it ends nose down, as the hangar launch starts nose up");
        Assert.IsNull(hangar.HangarShip, "the hangar is free again");
        AssertEqual(inSpace - fighter.ShipOrdLaunchCost, Carrier.Carrier.OrdnanceInSpace, "the carrier takes the fighter back at touchdown");
        AssertGreaterThan(hangar.HangarTimer, 4.9f, "the hangar readies the next fighter as before, at least 5 seconds");
    }

    [TestMethod]
    public void AFighterAtFullHealthMayBarrelRollInAsItMayRollOutAtLaunch()
    {
        Ship fighter = FighterOut();
        AssertEqual(1f, fighter.HealthPercent, "setup: an undamaged fighter");
        int rolls = CountRollingLandings(fighter, landings: 100);
        AssertGreaterThan(rolls, 20, "a fighter rolls on about half its landings, the odds of its launch roll");
        AssertLessThan(rolls, 80, "but not on every landing");

        ShipModule module = fighter.Modules.First();
        module.SetHealth(module.ActualMaxHealth * 0.5f, "Test");
        AssertLessThan(fighter.HealthPercent, 1f, "setup: a damaged fighter");
        AssertEqual(0, CountRollingLandings(fighter, landings: 100), "a damaged fighter never rolls in");
    }

    [TestMethod]
    public void AMiningShipNeverRollsIn()
    {
        Player.data.DefaultMiningShip = "Fang Strafer";
        Ship miner = FighterOut();
        Assert.IsTrue(miner.IsMiningShip, "setup: a mining ship design of fighter role, as a mod may have");
        AssertEqual(0, CountRollingLandings(miner, landings: 100), "mining ships never roll out at launch, so they never roll in");
    }

    // a quarter of the way in, a rolling ship has three quarters of its turn left: the launch's roll, reversed
    int CountRollingLandings(Ship fighter, int landings)
    {
        int rolls = 0;
        for (int i = 0; i < landings; ++i)
        {
            fighter.InitLandingInHangar(Carrier);
            fighter.LandShip.Update(visibleToPlayer: false, new FixedSimTime(LaunchShip.HangarDuration(fighter) * 0.25f));
            if (fighter.YRotation.ToDegrees().AlmostEqual(270f, 0.1f))
                ++rolls;
            else
                AssertEqual(0.001f, 0f, fighter.YRotation, "a ship that does not roll comes in level");
        }
        return rolls;
    }

    [TestMethod]
    public void AFighterCatchesUpWithACarrierFlyingFasterThanItsGlide()
    {
        Ship carrier = SpawnShip("Unarmed Scout", Player, new Vector2(300_000, 310_000));
        carrier.AI.OrderMoveTo(carrier.Position + new Vector2(6000, 0), Vector2.Right, AIState.AwaitingOrders);
        RunSimWhile((simTimeout: 10, fatal: true), () => carrier.CurrentVelocity < 400f);

        Ship fighter = SpawnShip("X-Ving mk1", Player, carrier.Position - new Vector2(1500, 0));
        Assert.IsTrue(fighter.MaxSTLSpeed > carrier.MaxSTLSpeed && carrier.MaxSTLSpeed > LaunchShip.HangarSpeed(fighter),
                      "setup: a fighter faster than its carrier, which flies faster than the 300 glide speed");
        fighter.Mothership = carrier;
        fighter.AI.OrderReturnToHangar();
        RunUntilLanding(fighter);

        AssertGreaterThan(carrier.CurrentVelocity, 400f, "the fighter starts landing while its carrier is still at full speed");
        AssertLessThan(fighter.CurrentVelocity, LaunchShip.HangarSpeed(fighter) + carrier.CurrentVelocity + 10f,
                       "it comes in at its glide speed on top of the carrier's");

        Vector2 offset = fighter.Position - carrier.Position;
        float closing = (fighter.Velocity - carrier.Velocity).Dot(-offset.Normalized());
        RunObjectsSim(TestSimStep);
        float glideIn = (offset.Length() - fighter.Position.Distance(carrier.Position)) / TestSimStep.FixedTime;
        AssertEqual(closing * 0.1f, closing, glideIn, "the glide starts at the speed it was closing in on the carrier, so it does not jolt");

        RunSimWhile((simTimeout: 10, fatal: true), () => fighter.Active);
        Assert.IsFalse(fighter.Dying, "a landed fighter is taken in, not destroyed");
        AssertLessThan(fighter.Position.Distance(carrier.Position), 1f, "it touches down on the moving carrier");
    }

    [TestMethod]
    public void AFighterMeetingItsCarrierHeadOnComesInAtItsGlideSpeed()
    {
        Ship carrier = SpawnShip("Unarmed Scout", Player, new Vector2(300_000, 310_000));
        carrier.AI.OrderMoveTo(carrier.Position + new Vector2(6000, 0), Vector2.Right, AIState.AwaitingOrders);
        RunSimWhile((simTimeout: 10, fatal: true), () => carrier.CurrentVelocity < 400f);

        Ship fighter = SpawnShip("X-Ving mk1", Player, carrier.Position + new Vector2(2500, 0));
        fighter.Mothership = carrier;
        fighter.AI.OrderReturnToHangar();
        RunUntilLanding(fighter);

        AssertGreaterThan(carrier.CurrentVelocity, 400f, "setup: the carrier is still flying toward the fighter");
        AssertLessThan(fighter.CurrentVelocity, LaunchShip.HangarSpeed(fighter) + 10f,
                       "a carrier coming toward the fighter adds nothing to its glide speed");
    }

    [TestMethod]
    public void ASupplyShuttleLandsOnItsCarrierAndHandsBackItsOrdnanceAtTouchdown()
    {
        Ship carrier = SpawnShip("TEST_Excalibur-Class Supercarrier", Player, new Vector2(300_000, 290_000));
        carrier.ChangeOrdnance(-carrier.OrdinanceMax * 0.5f);
        ShipModule bay = carrier.Carrier.AllSupplyBays.First();
        Ship shuttle = Ship.CreateShipFromHangar(UState, bay, Player, carrier.Position, carrier);
        carrier.OnShipLaunched(shuttle, bay);
        Assert.IsTrue(shuttle is { IsSupplyShuttle: true, IsLaunching: true }, "setup: the carrier must launch a supply shuttle");
        RunSimWhile((simTimeout: 10, fatal: true), () => shuttle.IsLaunching);

        shuttle.Position = carrier.Position + new Vector2(0, 2500);
        shuttle.AI.OrderReturnToHangar();
        RunUntilLanding(shuttle);
        Assert.AreSame(shuttle, bay.HangarShip, "the bay still holds its shuttle while it lands");

        float before = carrier.Ordinance, broughtBack = 0f;
        for (double time = 0; shuttle.Active; time += TestSimStepD)
        {
            AssertLessThan(time, 10.0, "the landing must finish");
            before = carrier.Ordinance;
            broughtBack = shuttle.Ordinance + shuttle.ShipRetrievalOrd;
            RunObjectsSim(TestSimStep);
        }
        AssertGreaterThan(broughtBack, 0f, "setup: the shuttle carries ordnance home");
        float gained = carrier.Ordinance - before;
        AssertGreaterThan(gained, broughtBack - 0.01f, "the shuttle's ordnance goes back into the carrier at touchdown");
        AssertLessThan(gained, broughtBack + carrier.OrdAddedPerSecond + 0.01f, "and nothing more, besides the carrier's own production");
        Assert.IsNull(bay.HangarShip, "the bay is free again");
    }

    [TestMethod]
    public void ACarrierLeavesALandingFighterAlone()
    {
        Ship fighter = FighterOut();
        ShipModule hangar = HangarOf(Carrier, fighter);
        fighter.AI.OrderReturnToHangar();
        RunUntilLanding(fighter);

        fighter.HasCommand = false;
        fighter.AI.OrderReturnToHangar();
        Assert.IsFalse(fighter.AI.IgnoreCombat, "setup: a fighter with no command asks for its hangar every second, which clears its ignore-combat flag");
        Carrier.Carrier.ScrambleFighters();
        AssertEqual(AIState.ReturnToHangar, fighter.AI.State, "the carrier does not send a landing fighter out to escort it");
        Assert.AreSame(fighter, hangar.HangarShip, "nor launch another from its hangar");

        RunSimWhile((simTimeout: 10, fatal: true), () => fighter.Active);
        Assert.IsNull(hangar.HangarShip, "the fighter lands and is taken in");
    }

    [TestMethod]
    public void AFighterWhoseCarrierIsLostWhileItLandsIsLostWithIt()
    {
        Ship fighter = FighterOut();
        fighter.AI.OrderReturnToHangar();
        RunUntilLanding(fighter);

        Carrier.QueueTotalRemoval();
        float ordnance = Carrier.Ordinance;
        RunSimWhile((simTimeout: 10, fatal: true), () => fighter.Active);
        Assert.IsFalse(fighter.Dying, "the fighter finishes its landing on the spot the carrier left, and is removed");
        AssertEqual(ordnance, Carrier.Ordinance, "a lost carrier takes nothing back");
    }

    [TestMethod]
    public void AnAssaultShuttleLandsOnItsCarrierAndItsTroopGoesBackAboardAtTouchdown()
    {
        Ship carrier = SpawnShip("TEST_Excalibur-Class Supercarrier", Player, new Vector2(300_000, 290_000));
        int troops = carrier.TroopCount;
        Assert.IsTrue(carrier.GetOurFirstTroop(out Troop troop), "setup: the carrier must carry troops");
        Assert.IsTrue(carrier.Carrier.TryScrambleSingleAssaultShuttle(troop, out Ship shuttle), "setup: the carrier must launch an assault shuttle");
        Assert.IsTrue(shuttle.IsDefaultTroopTransport, "setup: an assault shuttle");
        RunSimWhile((simTimeout: 10, fatal: true), () => shuttle.IsLaunching);

        shuttle.Position = carrier.Position + new Vector2(0, 2500);
        shuttle.AI.OrderReturnToHangar();
        RunUntilLanding(shuttle);
        AssertEqual(troops - 1, carrier.TroopCount, "the troop stays in the shuttle while it lands");

        RunSimWhile((simTimeout: 10, fatal: true), () => shuttle.Active);
        Assert.IsFalse(shuttle.Dying, "a landed shuttle is taken in, not destroyed");
        AssertEqual(troops, carrier.TroopCount, "its troop is back on the carrier at touchdown");
    }
}
