using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Ship_Game;
using Ship_Game.AI;
using Ship_Game.Commands.Goals;
using Ship_Game.GameScreens.LoadGame;
using Ship_Game.Gameplay;
using Ship_Game.Ships;
using Vector2 = SDGraphics.Vector2;

namespace UnitTests.Ships;

/// <summary>
/// A salvage order builds its race's salvage ship first in the queue of the empire's safe shipyard planet nearest the
/// derelict, and with no shipyard waits for one. A shipyard lost, or turned unsafe while a safe one is left, has the
/// ship queued again at the nearest; taking it out of the queue calls the order off. The built ship flies to the
/// platform's site taking no orders and no fights, then turns into the salvage platform over two turns, the derelict
/// its empire's from the start of the deploy, with the trust cost of an AI's colony in the system; the platform
/// stands where the ship was, and the empire sees the derelict's techs from then on. Its work over, the platform turns
/// back into the ship over two turns, and the ship carries home what it stripped as a refund is carried. Called off,
/// a ship still queued leaves the queue, one on its way or deploying flies back and lands at the nearest safe colony,
/// and a platform turns back into the ship. A queued salvage ship that Refit All turns into another design ends its
/// order once built, a ship like any other. A derelict taken by a rival or sunk before the deploy ends the order, the
/// ship flying home; a ship lost on its way, or bringing production home, is told. The salvage ship pays no upkeep and
/// the salvage unit is no hangar bay. An order saved with a salvage station's work ends on load, and one flying out
/// survives a save.
/// </summary>
[TestClass]
public class SalvageShipFlowTests : SalvageOrderTest
{
    NotificationManager Notes;

    void TrackNotifications() => Universe.NotificationManager = Notes = new NotificationManager(Universe.ScreenManager, Universe);

    // the item an order queued, or null
    static QueueItem QueuedFor(Planet shipyard, Goal order) => shipyard.ConstructionQueue.FirstOrDefault(q => q.Goal == order);

    DeliverRefund DeliveryOf(Ship ship) => Player.AI.FindGoal(g => g is DeliverRefund delivery && delivery.OldShip == ship) as DeliverRefund;

    [TestMethod]
    public void AnOrderQueuesItsRacesSalvageShipFirstAtTheSafeShipyardNearestTheDerelict()
    {
        Planet near = AddHomeWorldToEmpire(SystemSite + new Vector2(-100_000f, 0), Player);
        Player.UpdateRallyPoints();
        near.Construction.Enqueue(ResourceManager.Ships.GetDesign("Terran Constructor"), QueueItemType.Freighter);
        AssertEqual(1, near.ConstructionQueue.Count, "setup: the nearer shipyard is building something");
        Derelict d = Forms(Wreck);

        SalvageDerelict order = Ordered(d);
        AssertEqual("WaitForShip", order.StepName, "the order waits for its ship");
        QueueItem item = near.ConstructionQueue[0];
        Assert.AreSame(order, item.Goal, "queued first at the shipyard nearest the derelict");
        AssertEqual("Terran Salvage Ship", item.ShipData.Name, "the race's salvage ship");
        Assert.AreSame(near, order.PlanetBuildingAt);
        Assert.IsNull(QueuedFor(Homeworld, order), "and not at the farther one");
    }

    [TestMethod]
    public void WithNoShipyardTheOrderWaitsForOne()
    {
        Homeworld.HasSpacePort = false;
        Player.UpdateRallyPoints();
        Derelict d = Forms(Wreck);
        SalvageDerelict order = Ordered(d);
        AssertEqual("BuildShip", order.StepName, "with no shipyard the order is placed and waits");
        order.Evaluate();
        AssertEqual("BuildShip", order.StepName, "turn after turn");

        Homeworld.HasSpacePort = true;
        Player.UpdateRallyPoints();
        order.Evaluate();
        Assert.IsNotNull(QueuedFor(Homeworld, order), "then queues its ship at the first shipyard");
    }

    [TestMethod]
    public void AShipyardLostQueuesTheShipAgainAndTakingItOutOfTheQueueCallsTheOrderOff()
    {
        Planet near = AddHomeWorldToEmpire(SystemSite + new Vector2(-100_000f, 0), Player);
        near.Name = "Near";
        Player.UpdateRallyPoints();
        Derelict d = Forms(Wreck);
        SalvageDerelict order = Ordered(d);
        Assert.IsNotNull(QueuedFor(near, order), "setup: queued at the nearer shipyard");

        near.Construction.ClearQueue();
        near.SetOwner(Enemy);
        Player.UpdateRallyPoints();
        order.Evaluate();
        order.Evaluate();
        Assert.IsNotNull(QueuedFor(Homeworld, order), "its shipyard lost, the ship is queued again at the next");
        Assert.IsTrue(Player.AI.HasGoal(order), "and the order stands");

        Homeworld.Construction.Cancel(QueuedFor(Homeworld, order));
        order.Evaluate();
        Assert.IsFalse(Player.AI.HasGoal(order), "taken out of the queue, the order is called off");
    }

    [TestMethod]
    public void AShipyardTurnedUnsafeHandsTheShipToASafeOne()
    {
        Planet near = AddHomeWorldToEmpire(SystemSite + new Vector2(-100_000f, 0), Player);
        near.Name = "Near";
        Player.UpdateRallyPoints();
        Derelict d = Forms(Wreck);
        SalvageDerelict order = Ordered(d);
        Assert.IsNotNull(QueuedFor(near, order), "setup: queued at the nearer shipyard");

        near.Quarantine = true;
        Player.UpdateRallyPoints();
        Assert.IsFalse(near.Safe, "setup: the nearer shipyard is no longer safe");
        order.Evaluate();
        order.Evaluate();
        Assert.IsNull(QueuedFor(near, order), "the unsafe one's item leaves its queue at once");
        Assert.IsNotNull(QueuedFor(Homeworld, order), "and the ship queued at the safe one");

        Homeworld.Quarantine = true;
        Player.UpdateRallyPoints();
        QueueItem second = QueuedFor(Homeworld, order);
        order.Evaluate();
        Assert.AreSame(second, QueuedFor(Homeworld, order), "with no safe shipyard left, it stays where it is");
    }

    [TestMethod]
    public void TheBuiltShipFliesToItsSiteTakingNoOrders()
    {
        Derelict d = Forms(Wreck);
        SalvageDerelict order = Ordered(d);
        Ship ship = ShipBuilt(order, at: Homeworld.Position + new Vector2(2_000f, 0));
        AssertEqual("FlyToDerelict", order.StepName, "setup: the queue reports it built");

        order.Evaluate();
        AssertEqual(AIState.MoveTo, ship.AI.State, "it flies out");
        Assert.IsTrue(ship.AI.IgnoreCombat, "fighting nothing on the way");
        Assert.IsTrue(ship.IsSalvageShip && !ship.CanBeScrapped && !ship.PlayerShipCanTakeFleetOrders(), "and takes no orders");
        AssertEqual(SalvageDerelict.Stage.ShipOnItsWay, SalvageDerelict.StageOf(Player, d, out _, out _), "its derelict reads that the ship is on its way");
        AssertEqual(string.Format(Localizer.Token(GameText.SalvageShipFlyingTo), d.Design.Name), ShipInfoUIElement.SalvageShipStatus(ship),
                    "and the ship that it flies to it");
        Assert.IsNull(d.Claimant, "the derelict is not claimed yet");
        AssertEqual(0f, ShipMaintenance.GetMaintenanceCost(ship, Player, 0), "it pays no upkeep");
    }

    [TestMethod]
    public void AtItsSiteTheShipTurnsIntoThePlatformOverTwoTurnsClaimingTheDerelictAsItStarts()
    {
        Derelict d = Forms(Wreck);
        WreckSystem.PlanetList[0].SetOwner(Enemy);
        Enemy.GetRelations(Player).AtWar = Player.GetRelations(Enemy).AtWar = false;
        Relationship aiToPlayer = Enemy.GetRelations(Player);
        float trust = aiToPlayer.Trust;
        SalvageDerelict order = Ordered(d);
        Ship ship = ShipBuilt(order);

        order.Evaluate();
        AssertEqual(SalvageDerelict.Stage.Deploying, SalvageDerelict.StageOf(Player, d, out _, out _), "at its site it deploys");
        AssertEqual(Player, d.Claimant, "the derelict is its empire's from the start");
        AssertEqual(5f, trust - aiToPlayer.Trust, "and the AI with a colony in the system trusts the player 5 less");
        Assert.IsTrue(ship.Dismantling, "the ship turns into the platform");
        Assert.IsFalse(d.ShowsTechsTo(Player), "setup: its techs are not shown yet");

        order.Evaluate();
        Assert.IsTrue(ship.Active && order.Platform == null, "it takes two turns");
        order.Evaluate();
        Ship platform = order.Platform;
        Assert.IsTrue(platform is { Active: true, IsSalvagePlatform: true }, "then the platform stands");
        AssertEqual(1f, ship.Position, platform.Position, "where the ship was");
        Assert.IsFalse(ship.Active, "and the ship is gone");
        Assert.IsTrue(d.ShowsTechsTo(Player), "the player sees the derelict's techs from then on");
        AssertEqual(5f, trust - aiToPlayer.Trust, "the trust cost came once");
    }

    [TestMethod]
    public void ItsWorkOverThePlatformTurnsBackIntoTheShipWhichBringsHomeWhatItStripped()
    {
        Derelict d = Forms(Wreck);
        SalvageDerelict order = AtWork(d);
        order.Evaluate();
        order.Evaluate();
        AssertEqual(0.01f, 24f, order.Load, "setup: two turns strip 24");
        Ship platform = order.Platform;

        SalvageDerelict.Cancel(Player, d);
        order.Evaluate();
        Assert.IsTrue(order.Dismantling && platform.Dismantling, "called off, the platform turns back into the ship");
        AssertEqual(Localizer.Token(GameText.PlatformTurningBack), ShipInfoUIElement.SalvageStatus(platform, order), "as its status reads");
        order.Evaluate();
        Assert.IsTrue(platform.Active, "over two turns");
        order.Evaluate();

        Assert.IsFalse(platform.Active || Player.AI.HasGoal(order), "then the platform and the order are gone");
        DeliverRefund delivery = Player.AI.FindGoals<DeliverRefund>().Single();
        Ship ship = delivery.OldShip;
        Assert.IsTrue(ship is { Active: true, IsSalvageShip: true }, "the salvage ship is back");
        AssertEqual(1f, platform.Position, ship.Position, "where the platform stood");
        AssertEqual(0.01f, 24f, delivery.Load, "carrying home the 24 it stripped");
        Assert.IsTrue(ship.DeliversRefund, "as a refund is carried, taking no orders");
    }

    [TestMethod]
    public void CalledOffAShipQueuedLeavesTheQueueAndOneOnItsWayOrDeployingLandsAtTheNearestSafeColony()
    {
        Derelict queued = Forms(Wreck);
        Derelict flying = Forms(Wreck + new Vector2(0, 5_000f));
        Derelict deploying = Forms(Wreck + new Vector2(0, -5_000f));

        SalvageDerelict waiting = Ordered(queued);
        Assert.IsNotNull(QueuedFor(Homeworld, waiting), "setup: its ship is queued");
        SalvageDerelict.Cancel(Player, queued);
        Assert.IsFalse(Player.AI.HasGoal(waiting), "a queued order called off ends");
        Assert.IsNull(QueuedFor(Homeworld, waiting), "and its ship leaves the queue at once");

        SalvageDerelict outbound = Ordered(flying);
        Ship ship = ShipBuilt(outbound, at: Homeworld.Position + new Vector2(2_000f, 0));
        outbound.Evaluate();
        SalvageDerelict.Cancel(Player, flying);
        Assert.IsFalse(Player.AI.HasGoal(outbound), "one on its way called off ends");
        DeliverRefund home = DeliveryOf(ship);
        Assert.IsNotNull(home, "its ship flies back");
        Assert.IsTrue(home.Lands && home.Load == 0f, "with nothing aboard, to land");
        Assert.AreSame(Homeworld, home.PlanetBuildingAt, "at the nearest safe colony");
        AssertEqual($"{Localizer.Token(GameText.ReturningTo)} {Homeworld.Name}", ShipListScreenItem.GetStatusText(ship), "returning to it");

        SalvageDerelict deploy = Ordered(deploying);
        Ship deployer = ShipBuilt(deploy);
        deploy.Evaluate();
        Assert.IsTrue(deployer.Dismantling && deploying.Claimant == Player, "setup: the ship deploys");
        SalvageDerelict.Cancel(Player, deploying);
        Assert.IsFalse(deployer.Dismantling, "called off, it stops deploying");
        Assert.IsNotNull(DeliveryOf(deployer), "and flies back");
        Assert.IsNull(deploying.Claimant, "giving up the claim");
    }

    [TestMethod]
    public void AQueuedSalvageShipRefittedToAnotherDesignEndsItsOrderOnceBuiltAndIsAShipLikeAnyOther()
    {
        Derelict d = Forms(Wreck);
        SalvageDerelict order = Ordered(d);
        Ship salvageShip = SpawnShip("Terran Salvage Ship", Player, Homeworld.Position + new Vector2(5_000f, 0));
        IShipDesign other = ResourceManager.Ships.GetDesign("Dreadnought mk1-a");
        Homeworld.Construction.RefitShipsBeingBuilt(salvageShip, other); // as Refit All does to the ships being built
        Assert.AreSame(other, QueuedFor(Homeworld, order).ShipData, "setup: Refit All turns the queued salvage ship into another design");

        Ship built = SpawnShip(other.Name, Player, Homeworld.Position + new Vector2(2_000f, 0));
        TrackNotifications();
        order.ReportShipComplete(built);
        order.Evaluate();
        Assert.IsFalse(Player.AI.HasGoal(order), "built, it ends the order");
        AssertEqual(0, Notes.GetNotificationsAtomic().Length, "telling of no salvage ship lost");
        Assert.IsTrue(built.Active && built.CanBeScrapped && !built.WorksForSalvage, "and is a ship like any other");
        Assert.IsNull(DeliveryOf(built), "which is not sent home");
        Assert.IsNull(d.Claimant, "the derelict left unclaimed");
    }

    [TestMethod]
    public void ADerelictTakenOrSunkBeforeTheDeployEndsTheOrderAndTheShipFliesHome()
    {
        Derelict taken = Forms(Wreck);
        SalvageDerelict order = Ordered(taken);
        Ship ship = ShipBuilt(order, at: Homeworld.Position + new Vector2(2_000f, 0));
        order.Evaluate();
        Assert.IsTrue(taken.TryClaim(Enemy), "setup: a rival claims it first");

        order.Evaluate();
        Assert.IsFalse(Player.AI.HasGoal(order), "the order ends");
        Assert.IsNotNull(DeliveryOf(ship), "and its ship flies home");
    }

    [TestMethod]
    public void AShipLostOnItsWayOrBringingProductionHomeIsTold()
    {
        Derelict d = Forms(Wreck);
        SalvageDerelict order = Ordered(d);
        Ship ship = ShipBuilt(order, at: Homeworld.Position + new Vector2(2_000f, 0));
        order.Evaluate();
        TrackNotifications();
        ship.Die(null, cleanupOnly: true);

        order.Evaluate();
        Assert.IsFalse(Player.AI.HasGoal(order), "a ship lost on its way ends the order");
        Notification told = Notes.GetNotificationsAtomic().Single();
        AssertEqual($"{WreckSystem.Name}: your salvage ship for the Dreadnought mk1-a was lost", told.Message, "the player is told");
        AssertEqual(ship.ShipData.IconPath, told.IconPath, "under the salvage ship's icon");
        Assert.AreSame(d, told.ReferencedItem1, "and a click selects the derelict");
        Assert.IsNull(d.Claimant, "the derelict was never claimed");

        Derelict other = Forms(Wreck + new Vector2(0, 5_000f));
        SalvageDerelict work = AtWork(other);
        work.Evaluate();
        SalvageDerelict.Cancel(Player, other);
        for (int turn = 0; turn < 1 + SalvageDerelict.DismantleTurns; ++turn)
            work.Evaluate();
        DeliverRefund delivery = Player.AI.FindGoals<DeliverRefund>().Single();
        delivery.OldShip.Die(null, cleanupOnly: true);
        delivery.Evaluate();
        AssertEqual("Your salvage ship bringing home 12 production was lost", Notes.GetNotificationsAtomic().Last().Message,
                    "the player is told of a ship lost with production aboard");
    }

    [TestMethod]
    public void TheSalvageUnitIsNoHangarBay()
    {
        ShipModule unit = ResourceManager.GetModuleTemplate("SalvageUnit");
        Assert.IsTrue(unit.Salvage, "setup: the salvage module");
        Assert.IsFalse(unit.Is(ShipModuleType.Hangar), "it is no hangar, launching nothing");
        AssertEqual(0, ResourceManager.Ships.GetDesign("Terran Salvage Ship").Hangars.Length, "so a salvage ship has no bays");
    }

    [TestMethod]
    public void AnOrderSavedWithAStationsWorkEndsOnLoad()
    {
        Ship constructor = SpawnShip("Terran Constructor", Player, Wreck + new Vector2(5_000f, 0));
        var stationOrder = new SalvageDerelict(Player) { FinishedShip = constructor }; // as one saved before salvage ships loads
        Player.AI.AddGoal(stationOrder);

        stationOrder.Evaluate();
        Assert.IsFalse(Player.AI.HasGoal(stationOrder), "it ends");
        Assert.IsTrue(constructor.ScuttleTimer > 0f && !constructor.Active, "and scuttles its constructor at once");
    }

    [TestMethod]
    public void AnOrderWhoseShipFliesOutSurvivesSaveAndLoad()
    {
        Derelict d = Forms(Wreck);
        SalvageDerelict order = Ordered(d, recover: true);
        Ship ship = ShipBuilt(order, at: Homeworld.Position + new Vector2(2_000f, 0));
        order.Evaluate();
        AssertEqual("FlyToDerelict", order.StepName, "setup: its ship flies out");
        RunObjectsSim(TestSimStep); // a ship joins its empire's list, which the save writes, on an objects update

        UState.StarDate = 1001f; // the loader needs a stardate past 1000
        SavedGame save = Universe.Save("UnitTest.SalvageShipFlow", throwOnError: true);
        UniverseScreen loaded = LoadGame.Load(save.SaveFile, noErrorDialogs: true, startSimThread: false);
        Assert.IsNotNull(loaded, "setup: the save loads");

        SalvageDerelict loadedOrder = loaded.UState.Player.AI.FindGoals<SalvageDerelict>().Single();
        AssertEqual("FlyToDerelict", loadedOrder.StepName, "the order still flies out");
        AssertEqual(ship.Id, loadedOrder.SalvageShip?.Id ?? 0, "with its ship");
        Assert.IsTrue(loadedOrder.Recovering, "to recover the derelict");
        loadedOrder.Evaluate();
        Assert.IsTrue(loaded.UState.Player.AI.HasGoal(loadedOrder), "and it stands on");
    }
}
