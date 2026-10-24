using System;
using System.Linq;
using System.Reflection;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using SDUtils;
using Ship_Game;
using Ship_Game.AI;
using Ship_Game.Commands.Goals;
using Ship_Game.Gameplay;
using Ship_Game.GameScreens.LoadGame;
using Ship_Game.Ships;
using UnitTests.UI;
using Vector2 = SDGraphics.Vector2;

namespace UnitTests.Ships;

/// <summary>
/// An orbital taken apart in deep space or at a planet not its empire's sends its refund, half of its cost by its
/// health and the production aboard, or a salvage station's whole cost as its order stored it when built and passed it
/// on a change of hands, in a builder ship that rises from where it stood, facing its first colony, and takes no
/// orders. The builder goes, as a scrapped ship does, to the nearest of the five safe colonies with the most room;
/// where only part fits it stops close by and sends that down, untaxed, and flies on, back to one it stopped at too
/// once its store has room again; where all of it fits, or at its last stop, at most ten or one for each colony, it lands and unloads all, the
/// excess paid as credits at the tax rate, all of it on a colony lost during the landing. With no colony left with
/// room it lands at the nearest; a colony turned unsafe on the way is swapped without counting a stop; with no safe
/// colony the refund is paid as credits at once. Destroyed or boarded, it loses its load; its empire's goals cleared,
/// as a merger or a defeat clears them, it is gone with its load. Loads on their way count against a colony's room
/// for builders and scrapped ships alike. A save keeps the delivery where it was.
/// </summary>
[TestClass]
public class DeliverRefundTests : StarDriveTest
{
    const string Platform = "Platform Base mk1-a";
    const string SalvageStation = "Terran Basic Salvage Station";
    readonly Planet Colony;

    public DeliverRefundTests()
    {
        LoadStarterShips(Platform, SalvageStation, "Rocket Scout", "Shipyard");
        CreateUniverseAndPlayerEmpire();
        UState.Objects.EnableParallelUpdate = false;
        UState.P.DebugDisableShipLaunch = true;
        Colony = AddHomeWorldToEmpire(new Vector2(200_000f), Player, new Vector2(205_000f), explored: true);
        SetRoom(Colony, 1000f);
        Player.data.TaxRate = 0.25f;
        Player.UpdateRallyPoints();
    }

    static void SetRoom(Planet colony, float room)
    {
        colony.Storage.Max = 1000f;
        colony.ProdHere = 1000f - room;
    }

    Planet AddColony(Vector2 system, float room)
    {
        Planet colony = AddDummyPlanet(system, 1f, 1f, 1f, system + new Vector2(5_000f), explored: true);
        colony.SetOwner(Player);
        SetRoom(colony, room);
        Player.UpdateRallyPoints();
        return colony;
    }

    // a platform 20000 from the colony, taken apart over its 3 turns
    Ship TakeApart(float health = 1f, float production = 0f, Planet tether = null)
    {
        Ship platform = SpawnShip(Platform, Player, (tether ?? Colony).Position + new Vector2(20_000f, 0));
        if (tether != null)
            platform.TetherToPlanet(tether);
        platform.Health = platform.HealthMax * health;
        if (production > 0f)
        {
            platform.CargoSpaceMax = 100f;
            platform.LoadProduction(production);
        }
        platform.AI.OrderScrapShip();
        var dismantling = (DismantleOrbital)Player.AI.FindGoal(g => g is DismantleOrbital && g.OldShip == platform);
        for (int i = 0; i < 3; ++i)
            dismantling.Evaluate();
        Assert.IsFalse(platform.Active, "setup: the platform is gone");
        return platform;
    }

    DeliverRefund Delivery => Player.AI.FindGoal(g => g is DeliverRefund) as DeliverRefund;

    static float Refund(Ship platform, float health, float production) => platform.GetCost(platform.Loyalty) * 0.5f * health + production;

    // the builder at rest within the planet's radius + 150
    static void Arrive(Ship builder, Planet colony)
    {
        builder.Position = colony.Position + new Vector2(colony.Radius + 150f, 0);
        builder.Velocity = Vector2.Zero;
    }

    // it lands, and its delivery unloads it
    void LandAndUnload(DeliverRefund delivery, Ship builder)
    {
        RunSimWhile((simTimeout: 10, fatal: true), () => !builder.IsLanding);
        delivery.Evaluate();
        RunSimWhile((simTimeout: 60, fatal: true), () => builder.LandShip is { Done: false });
        delivery.Evaluate();
    }

    static readonly FieldInfo StopsField = typeof(DeliverRefund).GetField("Stops", BindingFlags.Instance | BindingFlags.NonPublic);

    [TestMethod]
    public void ADeepSpaceOrbitalsRefundRisesInABuilderFacingItsColony()
    {
        UState.P.DebugDisableShipLaunch = false;
        Ship platform = TakeApart(health: 0.8f, production: 10f);

        DeliverRefund delivery = Delivery;
        Assert.IsNotNull(delivery, "its refund leaves in a delivery");
        Ship builder = delivery.OldShip;
        Assert.IsTrue(builder is { Active: true, IsSupplyShuttle: true }, "carried by a builder ship");
        Assert.IsTrue(builder.IsLaunching, "which rises");
        AssertLessThan(builder.Position.Distance(platform.Position), 1f, "from where the platform stood");
        RunObjectsSim(TestSimStep);
        float facing = platform.Position.AngleToTarget(Colony.Position);
        AssertLessThan(Math.Abs(((builder.RotationDegrees - facing) % 360f + 540f) % 360f - 180f), 11f, "facing its colony");
        AssertEqual(Colony, delivery.PlanetBuildingAt, "the colony it heads for");
        AssertEqual(0.01f, Refund(platform, 0.8f, 10f), delivery.Load, "with half its cost by its health and the production aboard");
    }

    [TestMethod]
    public void AtAPlanetNotTheEmpiresTheRefundLeavesInABuilderToo()
    {
        Planet other = AddDummyPlanet(new Vector2(400_000f), 1f, 1f, 1f, new Vector2(400_000f, 410_000f), explored: true);
        other.SetOwner(Enemy);
        float stored = other.ProdHere;
        Ship platform = TakeApart(tether: other);

        AssertEqual(stored, other.ProdHere, "the other empire's planet gets nothing");
        AssertEqual(0.01f, Refund(platform, 1f, 0f), Delivery?.Load ?? 0f, "the refund leaves in a builder");
        AssertEqual(Colony, Delivery.PlanetBuildingAt, "for the empire's colony");
    }

    [TestMethod]
    public void TheBuilderTakesNoOrdersAndPaysNoUpkeep()
    {
        TakeApart();
        Ship builder = Delivery.OldShip;
        Assert.IsTrue(builder.DeliversRefund, "setup: it carries the refund");
        Assert.IsFalse(builder.CanBeScrapped, "it cannot be scrapped");
        Assert.IsFalse(builder.CanBeRefitted, "nor refitted");
        AssertEqual(0f, builder.GetMaintCost(), "and pays no upkeep");

        builder.AI.OrderScrapShip();
        Assert.IsFalse(Player.AI.HasGoal(g => g is ScrapShip), "a scrap order is refused");
        Assert.IsTrue(builder.DeliversRefund, "and it carries on");

        builder.AI.ClearOrders();
        Delivery.Evaluate();
        Assert.IsTrue(builder.DeliversRefund, "an order lost is given back on its next turn");
    }

    [TestMethod]
    public void TheSelectionsOrderButtonsPassTheBuilderOver()
    {
        TakeApart();
        Ship builder = Delivery.OldShip;
        Ship scout = SpawnShip("Rocket Scout", Player, Colony.Position + new Vector2(30_000f, 0));
        var resupply = new OrdersButton(new Array<Ship> { builder, scout }, OrderType.OrderResupply, GameText.OrdersSelectedShipOrShips) { SimpleToggle = true };
        var mouse = new MockInputProvider { MousePos = new Vector2(10f, 10f) };
        var input = new InputState { Provider = mouse };
        input.Update(new UpdateTimes(1 / 60f, 0f));
        mouse.LeftMouse = SDGraphics.Input.ButtonState.Pressed; // a click is the press
        input.Update(new UpdateTimes(1 / 60f, 0f));

        Assert.IsTrue(resupply.HandleInput(input), "setup: the click reaches the selection's Resupply");
        AssertEqual(AIState.Resupply, scout.AI.State, "setup: the other ship goes to resupply");
        Assert.IsTrue(builder.DeliversRefund, "the builder carries on");
    }

    [TestMethod]
    public void ItsStatusNamesTheColonyItHeadsFor()
    {
        TakeApart();
        string status = ShipListScreenItem.GetStatusText(Delivery.OldShip);
        Assert.IsTrue(status.StartsWith("Delivering production to") && status.EndsWith(Colony.Name), status);
    }

    [TestMethod]
    public void WhereAllOfItFitsItFliesThereLandsAndUnloadsAllUntaxed()
    {
        Ship platform = TakeApart(production: 10f);
        DeliverRefund delivery = Delivery;
        Ship builder = delivery.OldShip;
        float load = delivery.Load;
        float stored = Colony.ProdHere;
        float excess = Player.ExcessCargoMoney;
        AssertEqual(0.01f, Refund(platform, 1f, 10f), load, "setup: it carries the refund");
        Assert.IsTrue(delivery.Lands, "all of it fits, so it will land");

        int frame = 0;
        RunSimWhile((simTimeout: 120, fatal: true), () => Player.AI.HasGoal(delivery), () =>
        {
            if (++frame % 60 == 0)
                delivery.Evaluate();
        });
        Assert.IsFalse(builder.Active, "landed, the builder is gone");
        AssertEqual(0.01f, stored + load, Colony.ProdHere, "the colony gets all of it, untaxed");
        AssertEqual(excess, Player.ExcessCargoMoney, "with nothing paid as credits");
    }

    [TestMethod]
    public void FlyingInItComesToRestCloseByNotOverThePlanet()
    {
        AddColony(new Vector2(300_000f, 200_000f), room: 1000f);
        SetRoom(Colony, 5f);
        TakeApart();
        Ship builder = Delivery.OldShip;
        Assert.IsFalse(Delivery.Lands, "setup: it will stop, not land");
        builder.Position = Colony.Position + new Vector2(0, 150_000f); // far enough that it must warp, as sublight it would take over 500 s

        bool wasInWarp = false;
        float farthestWarpExit = 0f;
        RunSimWhile((simTimeout: 60, fatal: true), () => builder.Position.Distance(Colony.Position) > Colony.Radius + 300f || builder.CurrentVelocity > 1f, () =>
        {
            if (wasInWarp && !builder.IsInWarp)
                farthestWarpExit = Math.Max(farthestWarpExit, builder.Position.Distance(Colony.Position));
            wasInWarp = builder.IsInWarp;
        });
        AssertGreaterThan(farthestWarpExit, 0f, "setup: it warps on the way");
        AssertLessThan(farthestWarpExit, 7_500f, "it leaves warp only as it nears the colony");
        RunSimWhile((simTimeout: 3, fatal: false), () => true);
        float distance = builder.Position.Distance(Colony.Position);
        AssertLessThan(distance, Colony.Radius + 300f, "it stays close by");
        AssertGreaterThan(distance, Colony.Radius, "at rest outside the planet's disc");
    }

    [TestMethod]
    public void WhereOnlyPartFitsItStopsSendsThatDownAndFliesOn()
    {
        Planet far = AddColony(new Vector2(300_000f, 200_000f), room: 1000f);
        SetRoom(Colony, 5f);
        TakeApart();
        DeliverRefund delivery = Delivery;
        Ship builder = delivery.OldShip;
        float load = delivery.Load;
        AssertEqual(Colony, delivery.PlanetBuildingAt, "setup: the nearest colony first");
        Assert.IsFalse(delivery.Lands, "setup: with room for only part, it will stop");

        Arrive(builder, Colony);
        delivery.Evaluate();
        AssertEqual(0.01f, 1000f, Colony.ProdHere, "close by, it sends down what the store has room for");
        AssertEqual(0.01f, load - 5f, delivery.Load, "and carries the rest");
        AssertEqual(far, delivery.PlanetBuildingAt, "on to the next colony");
        Assert.IsTrue(delivery.Lands && !builder.IsLanding, "where all of it fits, not landing where it stopped");

        Arrive(builder, far);
        LandAndUnload(delivery, builder);
        AssertEqual(0.01f, load - 5f, far.ProdHere, "the next colony gets the rest");
        Assert.IsFalse(builder.Active, "and the builder is gone");
    }

    [TestMethod]
    public void WithFewerThanTenColoniesItsLastStopIsItsLastColony()
    {
        Planet far = AddColony(new Vector2(300_000f, 200_000f), room: 5f);
        SetRoom(Colony, 5f);
        TakeApart();
        DeliverRefund delivery = Delivery;
        Ship builder = delivery.OldShip;
        float load = delivery.Load;
        float excess = Player.ExcessCargoMoney;

        Arrive(builder, Colony);
        delivery.Evaluate();
        AssertEqual(far, delivery.PlanetBuildingAt, "setup: on to the second of its two colonies");
        Assert.IsTrue(delivery.Lands, "its last stop, where it lands though not all of it fits");

        Arrive(builder, far);
        LandAndUnload(delivery, builder);
        AssertEqual(0.01f, 1000f, far.ProdHere, "the store fills");
        AssertEqual(0.01f, excess + (load - 10f) * 0.25f, Player.ExcessCargoMoney, "and the rest is paid as credits at the tax rate");
    }

    [TestMethod]
    public void WithElevenColoniesItsTenthStopIsItsLast()
    {
        SetRoom(Colony, 1f);
        for (int i = 1; i <= 10; ++i)
            AddColony(new Vector2(200_000f + i * 100_000f, 200_000f), room: 1f);
        TakeApart();
        DeliverRefund delivery = Delivery;
        Ship builder = delivery.OldShip;
        StopsField.SetValue(delivery, 8);

        Arrive(builder, delivery.PlanetBuildingAt);
        delivery.Evaluate();
        Assert.IsFalse(delivery.Lands, "after its eighth stop, the ninth is not its last");
        Arrive(builder, delivery.PlanetBuildingAt);
        delivery.Evaluate();
        Assert.IsTrue(delivery.Lands, "after its ninth, the tenth is");
    }

    [TestMethod]
    public void WithNoColonyWithRoomLeftItLandsAtTheNearest()
    {
        AddColony(new Vector2(300_000f, 200_000f), room: 0f);
        AddColony(new Vector2(400_000f, 200_000f), room: 0f);
        SetRoom(Colony, 5f);
        TakeApart();
        DeliverRefund delivery = Delivery;
        Ship builder = delivery.OldShip;
        float load = delivery.Load;
        float excess = Player.ExcessCargoMoney;

        Arrive(builder, Colony);
        delivery.Evaluate();
        AssertEqual(Colony, delivery.PlanetBuildingAt, "with the others full, it goes to the nearest, the one it stopped at");
        Assert.IsTrue(delivery.Lands, "and lands there");
        LandAndUnload(delivery, builder);
        AssertEqual(0.01f, excess + (load - 5f) * 0.25f, Player.ExcessCargoMoney, "all it carried on paid as credits");
    }

    [TestMethod]
    public void AColonyTurnedUnsafeIsSwappedWithoutCountingAStop()
    {
        Planet far = AddColony(new Vector2(300_000f, 200_000f), room: 5f);
        SetRoom(Colony, 5f);
        TakeApart();
        DeliverRefund delivery = Delivery;
        AssertEqual(Colony, delivery.PlanetBuildingAt, "setup: heading for the nearest");

        Colony.Quarantine = true;
        Player.UpdateRallyPoints();
        delivery.Evaluate();
        AssertEqual(far, delivery.PlanetBuildingAt, "turned unsafe, it is swapped for the other");
        Assert.IsFalse(delivery.Lands, "which, the swap not counted, is not its last stop");
        Assert.IsTrue(delivery.OldShip.AI.OrderQueue.PeekFirst?.TargetPlanet == far, "and the builder flies there");
    }

    [TestMethod]
    public void AColonyLostOnTheWayIsSwapped()
    {
        Planet far = AddColony(new Vector2(300_000f, 200_000f), room: 5f);
        TakeApart();
        DeliverRefund delivery = Delivery;
        AssertEqual(Colony, delivery.PlanetBuildingAt, "setup: heading for the nearest");

        Colony.Construction.ClearQueue();
        Colony.SetOwner(Enemy);
        Player.UpdateRallyPoints();
        delivery.Evaluate();
        AssertEqual(far, delivery.PlanetBuildingAt, "lost, it is swapped for the other");
    }

    [TestMethod]
    public void WithNoSafeColonyTheRefundIsPaidAsCreditsAtOnce()
    {
        Colony.Quarantine = true;
        Player.UpdateRallyPoints();
        float excess = Player.ExcessCargoMoney;
        Ship platform = TakeApart();
        Assert.IsNull(Delivery, "no builder rises");
        AssertEqual(0.01f, excess + Refund(platform, 1f, 0f) * 0.25f, Player.ExcessCargoMoney, "the refund is paid as credits at the tax rate");
    }

    [TestMethod]
    public void WithNoSafeColonyLeftOnTheWayWhatItCarriesIsPaidAsCredits()
    {
        TakeApart();
        DeliverRefund delivery = Delivery;
        Ship builder = delivery.OldShip;
        float load = delivery.Load;
        float excess = Player.ExcessCargoMoney;

        Colony.Quarantine = true;
        Player.UpdateRallyPoints();
        delivery.Evaluate();
        Assert.IsFalse(Player.AI.HasGoal(delivery), "the delivery ends");
        Assert.IsFalse(builder.Active, "the builder removed");
        AssertEqual(0.01f, excess + load * 0.25f, Player.ExcessCargoMoney, "and its load paid as credits at the tax rate");
    }

    [TestMethod]
    public void DestroyedItLosesItsLoad()
    {
        TakeApart();
        DeliverRefund delivery = Delivery;
        float stored = Colony.ProdHere;
        float excess = Player.ExcessCargoMoney;

        delivery.OldShip.Die(null, cleanupOnly: true);
        delivery.Evaluate();
        Assert.IsFalse(Player.AI.HasGoal(delivery), "the delivery ends");
        AssertEqual(stored, Colony.ProdHere, "the colony gets nothing");
        AssertEqual(excess, Player.ExcessCargoMoney, "nor is anything paid");
    }

    [TestMethod]
    public void DyingItLosesItsLoadAndIsLeftToDie()
    {
        TakeApart();
        DeliverRefund delivery = Delivery;
        Ship builder = delivery.OldShip;
        float excess = Player.ExcessCargoMoney;

        builder.Dying = true;
        delivery.Evaluate();
        Assert.IsFalse(Player.AI.HasGoal(delivery), "the delivery ends");
        Assert.IsTrue(builder.Active, "the dying builder is not taken away");
        AssertEqual(excess, Player.ExcessCargoMoney, "and its load is lost");
    }

    [TestMethod]
    public void BoardedItLosesItsLoadAndIsTheCaptorsPlainShuttle()
    {
        TakeApart();
        DeliverRefund delivery = Delivery;
        Ship builder = delivery.OldShip;
        float excess = Player.ExcessCargoMoney;

        builder.LoyaltyChangeFromBoarding(Enemy, addNotification: false);
        RunObjectsSim(TestSimStep);
        delivery.Evaluate();
        Assert.IsFalse(Player.AI.HasGoal(delivery), "the delivery ends");
        Assert.IsTrue(builder.Active && builder.Loyalty == Enemy, "the builder is the captor's");
        Assert.IsFalse(builder.DeliversRefund, "carrying nothing");
        AssertEqual(excess, Player.ExcessCargoMoney, "and its load is lost");
    }

    [TestMethod]
    public void BoardedJustBeforeItsTurnItIsNotPaidOutOrRemoved()
    {
        TakeApart();
        DeliverRefund delivery = Delivery;
        Ship builder = delivery.OldShip;
        float excess = Player.ExcessCargoMoney;
        Colony.Quarantine = true;
        Player.UpdateRallyPoints();

        builder.LoyaltyChangeFromBoarding(Enemy, addNotification: false);
        delivery.Evaluate();
        Assert.IsTrue(builder.Active && Player.AI.HasGoal(delivery), "about to change hands, it waits");
        AssertEqual(excess, Player.ExcessCargoMoney, "unpaid though no colony is safe");

        RunObjectsSim(TestSimStep);
        delivery.Evaluate();
        Assert.IsTrue(builder.Active && builder.Loyalty == Enemy, "then it is the captor's");
        Assert.IsFalse(Player.AI.HasGoal(delivery), "the delivery over");
        AssertEqual(excess, Player.ExcessCargoMoney, "and its load lost");
    }

    [TestMethod]
    public void AColonyWhoseStoreFilledOnTheWayStillCountsAsAStop()
    {
        Planet second = AddColony(new Vector2(300_000f, 200_000f), room: 5f);
        AddColony(new Vector2(400_000f, 200_000f), room: 5f);
        SetRoom(Colony, 5f);
        TakeApart();
        DeliverRefund delivery = Delivery;
        Ship builder = delivery.OldShip;
        float load = delivery.Load;
        AssertEqual(Colony, delivery.PlanetBuildingAt, "setup: heading for the nearest");

        SetRoom(Colony, 0f);
        Arrive(builder, Colony);
        delivery.Evaluate();
        AssertEqual(0.01f, load, delivery.Load, "nothing fits, so it sends nothing");
        AssertEqual(2, (int)StopsField.GetValue(delivery), "though the stop counts");
        AssertEqual(second, delivery.PlanetBuildingAt, "and it flies on to the next");
    }

    [TestMethod]
    public void ItMayGoBackToAColonyItStoppedAtOnceItHasRoomAgain()
    {
        Planet second = AddColony(new Vector2(250_000f, 200_000f), room: 5f);
        AddColony(new Vector2(600_000f, 200_000f), room: 1000f);
        SetRoom(Colony, 5f);
        TakeApart();
        DeliverRefund delivery = Delivery;
        Ship builder = delivery.OldShip;
        Arrive(builder, Colony);
        delivery.Evaluate();
        AssertEqual(second, delivery.PlanetBuildingAt, "setup: on from the homeworld to the next nearest");

        SetRoom(Colony, 1000f);
        Arrive(builder, second);
        delivery.Evaluate();
        AssertEqual(Colony, delivery.PlanetBuildingAt, "the homeworld it stopped at, with room again, is the nearest with room");
    }

    [TestMethod]
    public void ItLandsOnlyWhileAllOfItStillFits()
    {
        AddColony(new Vector2(300_000f, 200_000f), room: 1000f);
        TakeApart();
        DeliverRefund delivery = Delivery;
        Assert.IsTrue(delivery.Lands, "setup: all of it fits, so it would land");

        SetRoom(Colony, 5f);
        delivery.Evaluate();
        Assert.IsFalse(delivery.Lands, "the store filled on the way, it will stop instead");
        AssertEqual(Colony, delivery.PlanetBuildingAt, "at the same colony");
    }

    [TestMethod]
    public void ItLandsOnAShipyardOfItsColony()
    {
        Ship shipyard = SpawnShip("Shipyard", Player, Colony.Position + new Vector2(0, Colony.Radius + 1_000f));
        shipyard.TetherToPlanet(Colony);
        TakeApart();
        DeliverRefund delivery = Delivery;
        Ship builder = delivery.OldShip;

        Arrive(builder, Colony);
        RunSimWhile((simTimeout: 30, fatal: true), () => !builder.IsLanding);
        AssertEqual(shipyard, builder.LandShip.Shipyard, "it lands on the colony's shipyard");
    }

    [TestMethod]
    public void AnEmpireAbsorbedLosesItsBuilderAndItsLoad()
    {
        Planet enemyColony = AddDummyPlanet(new Vector2(600_000f, 200_000f), 1f, 1f, 1f, new Vector2(605_000f, 200_000f), explored: true);
        enemyColony.SetOwner(Enemy);
        SetRoom(enemyColony, 1000f);
        Enemy.UpdateRallyPoints();
        Ship platform = SpawnShip(Platform, Enemy, enemyColony.Position + new Vector2(20_000f, 0));
        platform.AI.OrderScrapShip();
        var dismantling = (DismantleOrbital)Enemy.AI.FindGoal(g => g is DismantleOrbital);
        for (int i = 0; i < 3; ++i)
            dismantling.Evaluate();
        Ship builder = (Enemy.AI.FindGoal(g => g is DeliverRefund) as DeliverRefund)?.OldShip;
        Assert.IsNotNull(builder, "setup: the enemy's builder is on its way");
        UState.Objects.UpdateLists();
        Assert.IsTrue(Enemy.OwnedShips.Contains(builder), "setup: among its empire's ships");
        float excess = Player.ExcessCargoMoney;

        Player.AbsorbEmpire(Enemy);
        Assert.IsFalse(builder.Active, "absorbed, its empire's builder is gone");
        Assert.IsFalse(Player.AI.HasGoal(g => g is DeliverRefund), "its delivery passes to no one");
        AssertEqual(excess, Player.ExcessCargoMoney, "and its load is lost");
    }

    [TestMethod]
    public void ItUnloadsOnlyOnceItHasTouchedDown()
    {
        TakeApart();
        DeliverRefund delivery = Delivery;
        Ship builder = delivery.OldShip;
        float stored = Colony.ProdHere;

        Arrive(builder, Colony);
        RunSimWhile((simTimeout: 10, fatal: true), () => !builder.IsLanding);
        delivery.Evaluate();
        delivery.Evaluate();
        Assert.IsTrue(builder.LandShip is { Done: false }, "setup: still on its way down");
        AssertEqual(stored, Colony.ProdHere, "nothing is unloaded yet");
        Assert.IsTrue(builder.Active && Player.AI.HasGoal(delivery), "the builder and its delivery wait for touchdown");
    }

    [TestMethod]
    public void AColonyLostWhileItLandsTurnsTheLoadIntoCredits()
    {
        TakeApart();
        DeliverRefund delivery = Delivery;
        Ship builder = delivery.OldShip;
        float load = delivery.Load;
        float excess = Player.ExcessCargoMoney;

        Arrive(builder, Colony);
        RunSimWhile((simTimeout: 10, fatal: true), () => !builder.IsLanding);
        Colony.Construction.ClearQueue();
        Colony.SetOwner(Enemy);
        float stored = Colony.ProdHere;
        delivery.Evaluate();
        RunSimWhile((simTimeout: 60, fatal: true), () => builder.LandShip is { Done: false });
        delivery.Evaluate();
        AssertEqual(stored, Colony.ProdHere, "the lost colony gets nothing");
        AssertEqual(0.01f, excess + load * 0.25f, Player.ExcessCargoMoney, "all of it is paid as credits at the tax rate");
    }

    [TestMethod]
    public void ItsEmpiresGoalsClearedTheBuilderGoesWithItsLoad()
    {
        TakeApart();
        Ship builder = Delivery.OldShip;
        float excess = Player.ExcessCargoMoney;

        Player.AI.ClearGoals(); // as a merger or a defeat clears them
        Assert.IsFalse(builder.Active, "the builder is gone");
        AssertEqual(excess, Player.ExcessCargoMoney, "with its load");
    }

    [TestMethod]
    public void LoadsOnTheirWayCountAgainstAColonysRoom()
    {
        TakeApart();
        float load = Delivery.Load;
        Goal[] coming = Player.AI.FindGoals(g => g is DeliverRefund);
        AssertEqual(0.01f, 1000f - load, Colony.MissingProdHereForScrap(coming), "a builder's load counts against the room it heads for");
    }

    // five more colonies with little room, and a builder on its way to the homeworld with as much as it has room for
    float FillTheHomeworldByABuilder()
    {
        for (int i = 0; i < 5; ++i)
            AddColony(new Vector2(200_000f, 300_000f + i * 100_000f), room: 1f);
        TakeApart();
        DeliverRefund delivery = Delivery;
        AssertEqual(Colony, delivery.PlanetBuildingAt, "setup: the builder heads for the homeworld");
        SetRoom(Colony, delivery.Load);
        return delivery.Load;
    }

    [TestMethod]
    public void AScrappedShipPassesOverAColonyABuilderWillFill()
    {
        FillTheHomeworldByABuilder();
        Ship scout = SpawnShip("Rocket Scout", Player, Colony.Position + new Vector2(25_000f, 0));
        Assert.IsTrue(Player.FindPlanetToScrapIn(scout, out Planet scrapAt), "setup: the scout has somewhere to go");
        Assert.AreNotEqual(Colony, scrapAt, "a scrapped ship passes over the colony the builder will fill");
    }

    [TestMethod]
    public void ANextBuilderPassesOverAColonyABuilderWillFill()
    {
        FillTheHomeworldByABuilder();
        TakeApart();
        DeliverRefund second = Player.AI.FindGoals(g => g is DeliverRefund).Cast<DeliverRefund>().Last();
        Assert.AreNotEqual(Colony, second.PlanetBuildingAt, "the next builder passes over it");
    }

    [TestMethod]
    public void TheShuttlesAreOnePerFiftyProductionRoundedUpAtMostTwenty()
    {
        AssertEqual(1, CargoShuttles.RefundShuttlesFor(10f), "10 production, one shuttle");
        AssertEqual(3, CargoShuttles.RefundShuttlesFor(120f), "120, three");
        AssertEqual(20, CargoShuttles.RefundShuttlesFor(5000f), "never more than 20");
    }

    // a built salvage station in a system of its own, its order on its salvage step
    SalvageOps BuiltSalvageStation(Empire owner, out Ship station)
    {
        Vector2 site = new(600_000f, 600_000f);
        SolarSystem system = AddDummyPlanet(site, 1f, 1f, 1f, site + new Vector2(0, 60_000f), explored: true).System;
        Assert.IsTrue(ResourceManager.Ships.GetDesign(SalvageStation, out IShipDesign design), "setup: the station is loaded");
        var order = new SalvageOps(owner, system, site + new Vector2(30_000f, 0), design);
        owner.AI.AddGoal(order);
        order.AdvanceToNextStep();
        order.TargetShip = station = SpawnShip(SalvageStation, owner, order.StaticBuildPos);
        order.Evaluate();
        AssertEqual("Salvage", order.StepName, "setup: the station is built");
        return order;
    }

    [TestMethod]
    public void ASalvageStationsOrderStoresItsCostAsBuilt()
    {
        SalvageOps order = BuiltSalvageStation(Player, out Ship station);
        AssertEqual(0.01f, station.GetCost(Player), order.StationCost, "its order keeps what the station cost as it was built");
    }

    [TestMethod]
    public void ASalvageStationRefundsAllOfTheCostItsOrderStored()
    {
        SalvageOps order = BuiltSalvageStation(Player, out Ship station);
        order.StationCost = 777f;
        station.Health = station.HealthMax * 0.8f;
        station.CargoSpaceMax = 100f;
        station.LoadProduction(10f);

        station.AI.OrderScrapShip();
        var dismantling = (DismantleOrbital)Player.AI.FindGoal(g => g is DismantleOrbital && g.OldShip == station);
        for (int i = 0; i < 20 && Player.AI.HasGoal(dismantling); ++i)
            dismantling.Evaluate();
        Assert.IsFalse(station.Active, "setup: the station is gone");
        AssertEqual(0.01f, 777f * 0.8f + 10f, Delivery?.Load ?? 0f, "all of the cost its order stored, by its health, and its hold in full");
    }

    [TestMethod]
    public void ASalvageStationWithNoCostStoredRefundsAllOfItsCostNow()
    {
        SalvageOps order = BuiltSalvageStation(Player, out Ship station);
        order.StationCost = 0f;
        float cost = station.GetCost(Player);

        station.AI.OrderScrapShip();
        var dismantling = (DismantleOrbital)Player.AI.FindGoal(g => g is DismantleOrbital && g.OldShip == station);
        for (int i = 0; i < 20 && Player.AI.HasGoal(dismantling); ++i)
            dismantling.Evaluate();
        Assert.IsFalse(station.Active, "setup: the station is gone");
        AssertEqual(0.01f, cost, Delivery?.Load ?? 0f, "all of what it costs");
    }

    [TestMethod]
    public void ASalvageStationsCostPassesToItsNewOwner()
    {
        SalvageOps order = BuiltSalvageStation(Player, out Ship station);
        order.StationCost = 777f;

        station.LoyaltyChangeFromBoarding(Enemy, addNotification: false);
        RunObjectsSim(TestSimStep);
        Universe.InvokePendingSimThreadActions();
        var theirs = Enemy.AI.FindGoal(g => g is SalvageOps && g.TargetShip == station) as SalvageOps;
        Assert.IsNotNull(theirs, "setup: the station works for its new owner");
        AssertEqual(777f, theirs.StationCost, "with the cost it was built for");
    }

    [TestMethod]
    public void ASaveKeepsTheDeliveryWhereItWas()
    {
        Planet far = AddColony(new Vector2(300_000f, 200_000f), room: 1000f);
        SetRoom(Colony, 5f);
        TakeApart();
        DeliverRefund delivery = Delivery;
        Arrive(delivery.OldShip, Colony);
        delivery.Evaluate();
        float load = delivery.Load;
        AssertEqual(far, delivery.PlanetBuildingAt, "setup: one stop made, on its way to the next");

        UState.StarDate = 1001f; // the loader needs a stardate past 1000
        SavedGame save = Universe.Save("UnitTest.DeliverRefund", throwOnError: true);
        UniverseScreen loaded = LoadGame.Load(save.SaveFile, noErrorDialogs: true, startSimThread: false);
        Assert.IsNotNull(loaded, "setup: the save loads");

        var loadedDelivery = loaded.UState.Player.AI.FindGoal(g => g is DeliverRefund) as DeliverRefund;
        Assert.IsNotNull(loadedDelivery, "the delivery is kept");
        Planet loadedFar = loaded.UState.Player.GetPlanets().First(p => p.Name == far.Name);
        AssertEqual(0.01f, load, loadedDelivery.Load, "with its load");
        AssertEqual(loadedFar, loadedDelivery.PlanetBuildingAt, "and its colony");
        Assert.IsTrue(loadedDelivery.Lands, "where it lands, its last stop");
        Assert.IsTrue(loadedDelivery.OldShip is { Active: true, DeliversRefund: true }, "its builder on its way");
        AssertEqual(2, (int)StopsField.GetValue(loadedDelivery), "its stops counted");
    }
}
