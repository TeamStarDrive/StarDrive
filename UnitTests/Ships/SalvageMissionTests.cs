using System;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using SDUtils;
using Ship_Game;
using Ship_Game.AI;
using Ship_Game.Commands.Goals;
using Ship_Game.Fleets;
using Ship_Game.GameScreens.LoadGame;
using Ship_Game.Ships;
using Vector2 = SDGraphics.Vector2;

namespace UnitTests.Ships;

/// <summary>
/// A salvage order joins a salvage ship's mission in its derelict's system: one with a free unit, else the empire's
/// idle salvage ship nearest the system, one in no fleet orbiting, holding or awaiting orders, else a
/// new salvage ship, built first in the queue of the empire's safe shipyard nearest the mission's site, waiting for a
/// shipyard when there is none. The ship built is the one chosen in the Automation window, or the race's, or with auto
/// pick the best for the system, never one short of a load a unit or of the warp range the AI needs. A shipyard lost,
/// or turned unsafe while a safe one is left, has the ship queued again at the nearest; taking it out of the queue,
/// or calling off its orders, ends the mission. The ship flies to its site taking no orders and no fights, paying no
/// upkeep, launching nothing on its way, parks there, and goes to its empire's nearest safe colony to unload when its
/// hold is full, or to rearm when it lacks the ordnance a waiting launch takes and its full load pays for, once no
/// constructor of its is out; it unloads once in orbit there, a colony turned unsafe on its way swapped for the
/// nearest safe one, then comes back. With no safe colony its hold is paid as credits. Its orders done, it brings
/// home what it holds and waits in orbit at the colony it last unloaded at, though another be nearer, or, never
/// unloaded, at the nearest safe colony, a ship like any other. A queued salvage ship refitted to another design ends
/// its mission once built, untold. A mission flying out survives a save, and an order saved with a salvage station's
/// work ends on load. The derelicts window counts the player's salvage ships at work and idle, pauses the game, and
/// reads the goals as it opens and once a click in it has reached the sim thread. On its way to unload, the ship
/// takes no right-click order and joins no fleet. Flown out, it parks once its move has stopped it at its site;
/// pushed off its spot it flies back still fighting nothing, and on its way to unload an orbit order elsewhere is
/// undone. A mission waiting for its ship, or unloading, survives a save and load. A ship just reported built is its
/// mission's at once, and it flies out fighting nothing once its launch from the shipyard ends. Parked with a free
/// unit, the ship takes an order in its system still waiting for a salvage ship, then orders the salvage of the
/// derelict there soonest to sink that its empire could order with a fifth of its turns left, on Auto Salvage
/// recovering what Auto Salvage would, and never one whose order has a ship of its own; so its orders done it stays
/// for a derelict left there. With its hold full it takes only a recovery, waiting or of its own, launches no salvage
/// until it has room, launches a waiting recovery before it leaves unless no load of ordnance pays for it, and
/// unloaded comes back for a salvage left there. Its last order ended while it is away, it flies back for a derelict
/// left there, unless it has no live unit. On Auto Salvage it orders derelicts itself only where Auto Salvage would,
/// beside no rival's colony and with no hostile strength known there, salvaging the ships Auto Salvage would not
/// recover, and its own order joins its own mission though a queued ship there has a free unit. A derelict called off
/// it leaves alone.
/// </summary>
[TestClass]
public class SalvageMissionTests : SalvageOrderTest
{
    NotificationManager Notes;

    void TrackNotifications() => Universe.NotificationManager = Notes = new NotificationManager(Universe.ScreenManager, Universe);

    static QueueItem QueuedFor(Planet shipyard, Goal mission) => shipyard.ConstructionQueue.FirstOrDefault(q => q.Goal == mission);

    // runs the universe's objects while `running` holds, evaluating the mission once a simulated second, more often than its empire's 5 s turn
    void RunWithMission(SalvageMission mission, double seconds, Func<bool> running)
    {
        int frame = 0;
        RunSimWhile((seconds, fatal: true), running, () =>
        {
            if (++frame % 60 == 0 && mission.Owner.AI.HasGoal(mission))
                mission.Evaluate();
        });
    }

    // a salvageable derelict of the player's, its turns run down by this many
    Derelict Sinking(Vector2 at, int turnsGone)
    {
        Derelict d = Forms(at);
        for (int i = 0; i < turnsGone; ++i)
            d.UpdateTurn();
        return d;
    }

    // far from its site, on its way
    Vector2 OnItsWay => Homeworld.Position + new Vector2(2_000f, 0);

    // in orbit at the homeworld it was sent to, where its orbit keeps it, once the sim settles it there
    void InOrbitAtHomeworld(Ship ship)
    {
        ship.Position = Homeworld.Position + new Vector2(Homeworld.Radius * 2f + ship.Radius, 0);
        RunSimWhile((5.0, fatal: true), () => !ship.IsOrbiting(Homeworld));
    }

    [TestMethod]
    public void AnOrderWithNoIdleShipQueuesTheChosenSalvageShipFirstAtTheSafeShipyardNearestItsSite()
    {
        Planet near = AddHomeWorldToEmpire(SystemSite + new Vector2(-100_000f, 0), Player);
        Player.UpdateRallyPoints();
        near.Construction.Enqueue(ResourceManager.Ships.GetDesign(Constructor), QueueItemType.Freighter);
        AssertEqual(1, near.ConstructionQueue.Count, "setup: the nearer shipyard is building something");
        Derelict d = Forms(Wreck);

        SalvageDerelict order = Ordered(d);
        SalvageMission mission = MissionOf(order);
        AssertEqual("BuildShip", mission.StepName, "the order starts a mission");
        mission.Evaluate();
        AssertEqual("WaitForShip", mission.StepName, "which queues its ship on its first turn, then waits for it");
        AssertEqual("LaunchConstructor", order.StepName, "and the order for the ship at its site");
        QueueItem item = near.ConstructionQueue[0];
        Assert.AreSame(mission, item.Goal, "queued first at the shipyard nearest the site");
        AssertEqual("Terran Salvage Ship", item.ShipData.Name, "the race's salvage ship");
        Assert.AreSame(near, mission.PlanetBuildingAt);
        Assert.IsNull(QueuedFor(Homeworld, mission), "and not at the farther one");
        Assert.IsTrue(SalvageMission.IsValidSite(WreckSystem, mission.Site, Empty<Ship>.Array), "at a free site among the system's derelicts");
    }

    [TestMethod]
    public void WithNoShipyardTheMissionWaitsForOne()
    {
        Homeworld.HasSpacePort = false;
        Player.UpdateRallyPoints();
        SalvageMission mission = MissionOf(Ordered(Forms(Wreck)));
        AssertEqual("BuildShip", mission.StepName, "with no shipyard the mission is placed and waits");
        mission.Evaluate();
        AssertEqual("BuildShip", mission.StepName, "turn after turn");

        Homeworld.HasSpacePort = true;
        Player.UpdateRallyPoints();
        mission.Evaluate();
        Assert.IsNotNull(QueuedFor(Homeworld, mission), "then queues its ship at the first shipyard");
    }

    [TestMethod]
    public void AShipyardLostQueuesTheShipAgainAndTakingItOutOfTheQueueEndsTheMissionAndItsOrder()
    {
        Planet near = AddHomeWorldToEmpire(SystemSite + new Vector2(-100_000f, 0), Player);
        near.Name = "Near";
        Player.UpdateRallyPoints();
        SalvageDerelict order = Ordered(Forms(Wreck));
        SalvageMission mission = Queued(order);
        Assert.IsNotNull(QueuedFor(near, mission), "setup: queued at the nearer shipyard");

        near.Construction.ClearQueue();
        near.SetOwner(Enemy);
        Player.UpdateRallyPoints();
        mission.Evaluate();
        mission.Evaluate();
        Assert.IsNotNull(QueuedFor(Homeworld, mission), "its shipyard lost, the ship is queued again at the next");
        Assert.IsTrue(Player.AI.HasGoal(mission) && Player.AI.HasGoal(order), "and the mission and its order stand");

        Homeworld.Construction.Cancel(QueuedFor(Homeworld, mission));
        mission.Evaluate();
        order.Evaluate();
        Assert.IsFalse(Player.AI.HasGoal(mission) || Player.AI.HasGoal(order), "taken out of the queue, the mission ends with its order");
    }

    [TestMethod]
    public void AShipyardTurnedUnsafeHandsTheShipToASafeOne()
    {
        Planet near = AddHomeWorldToEmpire(SystemSite + new Vector2(-100_000f, 0), Player);
        near.Name = "Near";
        Player.UpdateRallyPoints();
        SalvageMission mission = Queued(Ordered(Forms(Wreck)));
        Assert.IsNotNull(QueuedFor(near, mission), "setup: queued at the nearer shipyard");

        near.Quarantine = true;
        Player.UpdateRallyPoints();
        Assert.IsFalse(near.Safe, "setup: the nearer shipyard is no longer safe");
        mission.Evaluate();
        mission.Evaluate();
        Assert.IsNull(QueuedFor(near, mission), "the unsafe one's item leaves its queue at once");
        Assert.IsNotNull(QueuedFor(Homeworld, mission), "and the ship is queued at the safe one");

        Homeworld.Quarantine = true;
        Player.UpdateRallyPoints();
        QueueItem second = QueuedFor(Homeworld, mission);
        mission.Evaluate();
        Assert.AreSame(second, QueuedFor(Homeworld, mission), "with no safe shipyard left, it stays where it is");
    }

    [TestMethod]
    public void CallingOffAQueuedShipsOrdersTakesItOutOfTheQueue()
    {
        Derelict d = Forms(Wreck);
        SalvageDerelict order = Ordered(d);
        SalvageMission mission = Queued(order);
        Assert.IsNotNull(QueuedFor(Homeworld, mission), "setup: its ship is queued");

        SalvageDerelict.Cancel(Player, d);
        Assert.IsFalse(Player.AI.HasGoal(order), "a queued order called off ends");
        mission.Evaluate();
        Assert.IsFalse(Player.AI.HasGoal(mission), "and so does its mission, with no order left");
        Assert.IsNull(QueuedFor(Homeworld, mission), "its ship leaving the queue at once");
    }

    [TestMethod]
    public void TheEmpiresIdleSalvageShipNearestTheSystemTakesTheOrderSoNoneIsBuilt()
    {
        Ship far = IdleShip(Homeworld.Position + new Vector2(-20_000f, 0));
        Ship near = IdleShip(Homeworld.Position + new Vector2(20_000f, 20_000f));
        AssertLessThan(near.Position.Distance(WreckSystem.Position), far.Position.Distance(WreckSystem.Position), "setup: one is nearer the system");

        SalvageMission first = MissionOf(Ordered(Forms(Wreck)));
        Assert.AreSame(near, first.SalvageShip, "the nearer idle ship takes the order");
        AssertEqual("FlyToSite", first.StepName, "flying out at once");
        Assert.IsNull(QueuedFor(Homeworld, first), "so none is built");
        Assert.IsFalse(SalvageMission.IsIdle(near, Player), "and it is idle no more");

        SalvageMission second = MissionOf(Ordered(Forms(Wreck + new Vector2(0, 5_000f))));
        Assert.AreSame(far, second.SalvageShip, "the next order, its one unit busy, takes the other");
        SalvageMission third = Queued(Ordered(Forms(Wreck + new Vector2(0, -5_000f))));
        Assert.IsNull(third.SalvageShip, "and with none idle left");
        Assert.IsNotNull(QueuedFor(Homeworld, third), "one is built");
    }

    [TestMethod]
    public void OnlyAShipInNoFleetOrbitingHoldingOrAwaitingOrdersIsIdle()
    {
        Ship ship = IdleShip(Homeworld.Position + new Vector2(5_000f, 0));
        ship.AI.OrderMoveTo(SystemSite, Vector2.UnitX);
        Assert.IsFalse(SalvageMission.IsIdle(ship, Player), "a ship on the move is not idle");
        ship.AI.ClearOrders();
        Assert.IsTrue(SalvageMission.IsIdle(ship, Player), "setup: awaiting orders again");
        ship.AI.OrderScrapShip();
        Assert.IsFalse(SalvageMission.IsIdle(ship, Player), "one ordered scrapped is not");

        ship.AI.ClearOrders();
        Assert.IsTrue(SalvageMission.IsIdle(ship, Player), "setup: awaiting orders again");
        Fleet fleet = Player.CreateFleet(1, null);
        Assert.IsTrue(fleet.AddShip(ship), "setup: in a fleet");
        Assert.IsFalse(SalvageMission.IsIdle(ship, Player), "nor is one in a fleet");
    }

    [TestMethod]
    public void AMissionWithAFreeUnitTakesTheNextOrderInItsSystemOnly()
    {
        TwinShips();
        Vector2 elsewhere = new(-600_000f, 600_000f);
        SolarSystem other = AddDummyPlanet(elsewhere, 1f, 1f, 1f, elsewhere + new Vector2(0, 60_000f), explored: true).System;
        SalvageMission mission = MissionOf(Ordered(Forms(Wreck)));
        AssertEqual(1, mission.FreeUnits, "setup: the twin-unit ship it waits for has a free unit");

        SalvageMission there = MissionOf(Ordered(Forms(elsewhere + new Vector2(30_000f, 0))));
        Assert.AreNotSame(mission, there, "an order in another system starts its own");
        AssertEqual(other, there.TargetSystem);
        Assert.AreSame(mission, MissionOf(Ordered(Forms(Wreck + new Vector2(0, 5_000f)))), "one in its system takes the free unit");
        AssertEqual(0, mission.FreeUnits, "which leaves none");
        Assert.AreNotSame(mission, MissionOf(Ordered(Forms(Wreck + new Vector2(0, -5_000f)))), "so the next starts another");
    }

    [TestMethod]
    public void ParkedWithAFreeUnitTheShipOrdersTheSalvageOfTheDerelictInItsSystemSoonestToSinkThatItCould()
    {
        TwinShips();
        SalvageDerelict order = Ordered(Forms(Wreck));
        SalvageMission mission = MissionOf(order);
        Vector2 elsewhere = new(-600_000f, 600_000f);
        AddDummyPlanet(elsewhere, 1f, 1f, 1f, elsewhere + new Vector2(0, 60_000f), explored: true);
        Derelict otherSystem = Sinking(elsewhere + new Vector2(30_000f, 0), turnsGone: 130);
        Derelict sinking = Sinking(Wreck + new Vector2(5_000f, 0), turnsGone: 161);
        Derelict theirs = Sinking(Wreck + new Vector2(-5_000f, 0), turnsGone: 120);
        Assert.IsTrue(theirs.TryClaim(Enemy), "setup: another empire salvages one");
        Derelict later = Forms(Wreck + new Vector2(0, 5_000f));
        Derelict sooner = Sinking(Wreck + new Vector2(0, -5_000f), turnsGone: 100);
        AssertEqual(39, sinking.TurnsLeft, "setup: 39 of its 200 turns left");
        ShipBuilt(order);
        Parked(order);

        mission.Evaluate();
        SalvageDerelict taken = SalvageDerelict.OrderOf(Player, sooner);
        Assert.IsNotNull(taken, "its free unit orders the derelict there soonest to sink");
        Assert.AreSame(mission, taken.Mission, "which joins the ship's mission");
        Assert.IsTrue(!taken.Recovering && SalvageDerelict.WouldRecover(Player, sooner), "salvaged, though Auto Salvage would recover it");
        Assert.IsNull(SalvageDerelict.OrderOf(Player, sinking), "not one with less than a fifth of its turns left");
        Assert.IsTrue(SalvageDerelict.OrderOf(Player, theirs) == null && theirs.Claimant == Enemy, "nor one another empire salvages");
        Assert.IsNull(SalvageDerelict.OrderOf(Player, otherSystem), "nor one in another system");
        Assert.IsNull(SalvageDerelict.OrderOf(Player, later), "and no more than its free units take");
    }

    [TestMethod]
    public void ParkedWithAFreeUnitTheShipFirstTakesAnOrderInItsSystemStillWaitingForASalvageShip()
    {
        TwinShips();
        SalvageDerelict order = Ordered(Forms(Wreck));
        SalvageMission mission = MissionOf(order);
        Vector2 elsewhere = new(-600_000f, 600_000f);
        AddDummyPlanet(elsewhere, 1f, 1f, 1f, elsewhere + new Vector2(0, 60_000f), explored: true);
        var waitingElsewhere = new SalvageDerelict(Player, Forms(elsewhere + new Vector2(30_000f, 0)));
        var waiting = new SalvageDerelict(Player, Forms(Wreck + new Vector2(0, 5_000f)));
        Player.AI.AddGoal(waitingElsewhere); // as orders that have found no salvage ship
        Player.AI.AddGoal(waiting);
        Derelict sooner = Sinking(Wreck + new Vector2(0, -5_000f), turnsGone: 100);
        ShipBuilt(order);
        Parked(order);

        mission.Evaluate();
        Assert.AreSame(mission, waiting.Mission, "its free unit takes the order there that waits for a salvage ship");
        Assert.IsNull(SalvageDerelict.OrderOf(Player, sooner), "before it orders a derelict itself");
        Assert.IsNull(waitingElsewhere.Mission, "and leaves one waiting in another system");
    }

    [TestMethod]
    public void OnAutoSalvageTheShipRecoversTheShipsAutoSalvageWould()
    {
        TwinShips();
        SalvageDerelict order = Ordered(Forms(Wreck));
        Derelict other = Forms(Wreck + new Vector2(0, 5_000f));
        Assert.IsTrue(SalvageDerelict.WouldRecover(Player, other), "setup: the player builds no ship of its role as strong");
        ShipBuilt(order);
        Parked(order);

        Player.AutoSalvage = true;
        MissionOf(order).Evaluate();
        Assert.IsTrue(SalvageDerelict.OrderOf(Player, other) is { Recovering: true }, "on Auto Salvage the ship's own order recovers it");
    }

    [TestMethod]
    public void ItsOrdersDoneWithItsHoldFullTheShipTakesNoSalvageButUnloadedComesBackForTheDerelictLeft()
    {
        Derelict first = Forms(Wreck);
        SalvageDerelict order = Ordered(first);
        SalvageMission mission = MissionOf(order);
        SalvageDerelict ownShip = Ordered(Forms(Wreck + new Vector2(0, 5_000f)));
        SalvageMission itsOwn = MissionOf(ownShip);
        Assert.AreNotSame(mission, itsOwn, "setup: the one-unit ship has no unit for a second order, which gets a ship of its own");
        Derelict left = Forms(Wreck + new Vector2(0, -5_000f));
        Ship ship = ShipBuilt(order);
        Parked(order);
        mission.Evaluate();
        Assert.IsNull(SalvageDerelict.OrderOf(Player, left), "setup: with no free unit, it orders nothing more");

        UState.Derelicts.Strip(first, null, first.ProductionLeft);
        Player.AI.RemoveGoal(order);
        ship.LoadProduction(ship.CargoRoom);
        mission.Evaluate();
        Assert.IsTrue(SalvageDerelict.OrderOf(Player, left) == null && mission.Unloading, "its order done and its hold full, it takes no salvage and goes home");

        InOrbitAtHomeworld(ship);
        mission.Evaluate();
        Assert.IsTrue(ship.GetProduction() == 0f && mission.FlyingOut, "unloaded, it comes back");
        ship.Position = mission.Site;
        ship.AI.ClearOrders(); // its move there over
        mission.Evaluate();
        mission.Evaluate();
        Assert.AreSame(mission, SalvageDerelict.OrderOf(Player, left)?.Mission, "and with room in its hold its unit takes the derelict left to no salvage ship");
        Assert.AreSame(itsOwn, ownShip.Mission, "not the one whose order has a ship of its own");
    }

    [TestMethod]
    public void WithItsHoldFullTheShipOrdersNoSalvageItselfButARecoveryAutoSalvagePicks()
    {
        TwinShips();
        SalvageDerelict order = Ordered(Forms(Wreck));
        SalvageMission mission = MissionOf(order);
        Ship ship = ShipBuilt(order);
        Launched(order);
        Derelict other = Forms(Wreck + new Vector2(0, 5_000f));
        ship.LoadProduction(ship.CargoRoom);

        mission.Evaluate();
        Assert.IsTrue(SalvageDerelict.OrderOf(Player, other) == null && mission.Parked(out _), "with its hold full and its constructor out, the ship stays and orders no salvage");
        Player.AutoSalvage = true;
        IShipDesign dreadnought = ResourceManager.Ships.GetDesign("Dreadnought mk1-a");
        Assert.IsTrue(Player.AddBuildableShip(dreadnought) && !SalvageDerelict.WouldRecover(Player, other), "setup: building the same design, Auto Salvage would salvage it");
        mission.Evaluate();
        Assert.IsNull(SalvageDerelict.OrderOf(Player, other), "nor a salvage Auto Salvage picks");
        Assert.IsTrue(Player.RemoveBuildableShip(dreadnought) && SalvageDerelict.WouldRecover(Player, other), "setup: Auto Salvage would recover it");
        mission.Evaluate();
        Assert.IsTrue(SalvageDerelict.OrderOf(Player, other) is { Recovering: true }, "but on Auto Salvage it orders the recovery");
    }

    [TestMethod]
    public void WithItsHoldFullTheShipTakesAWaitingRecoveryButNoWaitingSalvage()
    {
        TwinShips();
        SalvageDerelict order = Ordered(Forms(Wreck));
        SalvageMission mission = MissionOf(order);
        Ship ship = ShipBuilt(order);
        Launched(order);
        var salvage = new SalvageDerelict(Player, Forms(Wreck + new Vector2(0, 5_000f)));
        var recovery = new SalvageDerelict(Player, Forms(Wreck + new Vector2(0, -5_000f)), recover: true);
        Player.AI.AddGoal(salvage); // as orders that have found no salvage ship
        Player.AI.AddGoal(recovery);
        ship.LoadProduction(ship.CargoRoom);

        mission.Evaluate();
        Assert.IsNull(salvage.Mission, "with its hold full the ship leaves a waiting salvage");
        Assert.AreSame(mission, recovery.Mission, "but takes a waiting recovery");
    }

    [TestMethod]
    public void WithItsHoldFullTheShipLaunchesAWaitingRecoveryItTakesOverBeforeItLeaves()
    {
        TwinShips();
        SalvageDerelict order = Ordered(Forms(Wreck));
        SalvageMission mission = MissionOf(order);
        Ship ship = ShipBuilt(order);
        Parked(order);
        var recovery = new SalvageDerelict(Player, Forms(Wreck + new Vector2(0, 5_000f)), recover: true);
        Player.AI.AddGoal(recovery); // as an order that has found no salvage ship
        ship.LoadProduction(ship.CargoRoom);

        mission.Evaluate();
        Assert.IsTrue(recovery.Mission == mission && recovery.Launching, "with its hold full the ship takes over the waiting recovery, which moves on to its launch");
        Assert.IsTrue(mission.Parked(out _), "so the ship waits to launch it");
        recovery.Evaluate();
        Assert.IsNotNull(recovery.FinishedShip, "which it then does");
    }

    [TestMethod]
    public void WithItsHoldFullTheShipLaunchesNoSalvageItHoldsUntilItHasRoom()
    {
        TwinShips();
        SalvageDerelict order = Ordered(Forms(Wreck));
        Ship ship = ShipBuilt(order);
        Launched(order);
        ship.LoadProduction(ship.CargoRoom);
        SalvageDerelict clicked = Ordered(Forms(Wreck + new Vector2(0, 5_000f)));
        Assert.AreSame(MissionOf(order), clicked.Mission, "setup: a salvage clicked in its system joins the full ship's free unit");

        clicked.Evaluate();
        Assert.IsTrue(clicked.FinishedShip == null && clicked.Launching, "with its hold full, the ship launches no constructor for it");
        ship.UnloadProduction(ship.GetProduction());
        clicked.Evaluate();
        Assert.IsNotNull(clicked.FinishedShip, "once its hold has room, it does");
    }

    [TestMethod]
    public void WithItsHoldFullTheShipLaunchesARecoveryBeforeItLeavesAndGoesOnceItsConstructorIsDone()
    {
        TwinShips();
        SalvageDerelict order = Ordered(Forms(Wreck));
        SalvageMission mission = MissionOf(order);
        Ship ship = ShipBuilt(order);
        Parked(order);
        ship.LoadProduction(ship.CargoRoom);
        SalvageDerelict recovery = Ordered(Forms(Wreck + new Vector2(0, 5_000f)), recover: true);
        Assert.AreSame(mission, recovery.Mission, "setup: a recovery clicked in its system joins the full ship");

        mission.Evaluate();
        Assert.IsTrue(mission.Parked(out _), "with its hold full and no constructor out, the ship waits to launch the recovery");
        recovery.Evaluate();
        order.Evaluate();
        Assert.IsTrue(recovery.FinishedShip != null && order.FinishedShip == null, "which launches, the salvage waiting for room");
        mission.Evaluate();
        Assert.IsTrue(mission.Parked(out _), "it stays while the recovery's constructor is out");
        PlatformBuilt(recovery);
        mission.Evaluate();
        Assert.IsTrue(mission.Unloading, "and once it is done, goes to unload");
    }

    [TestMethod]
    public void WithItsHoldFullAShipWhoseFullLoadPaysForNoLaunchLeavesWithoutWaitingForARecovery()
    {
        TwinShips();
        SalvageDerelict order = Ordered(Forms(Wreck));
        SalvageMission mission = MissionOf(order);
        Ship ship = ShipBuilt(order);
        Parked(order);
        ship.ChangeOrdnance(-ship.Ordinance);
        ship.OrdinanceMax = 1f; // as with a constructor whose launch costs more than the ship holds
        ship.LoadProduction(ship.CargoRoom);
        Ordered(Forms(Wreck + new Vector2(0, 5_000f)), recover: true);

        mission.Evaluate();
        Assert.IsTrue(mission.Unloading, "with its hold full it goes to unload, as no load of ordnance pays for the recovery's launch");
    }

    [TestMethod]
    public void ItsLastOrderEndingOnItsTripHomeTheShipFliesBackForADerelictLeftInItsSystemUnlessItHasNoLiveUnit()
    {
        SalvageDerelict order = AtWork(Forms(Wreck));
        SalvageMission mission = MissionOf(order);
        Ship ship = mission.SalvageShip;
        Derelict left = Forms(Wreck + new Vector2(0, 5_000f));
        ship.LoadProduction(ship.CargoRoom);
        mission.Evaluate();
        Assert.IsTrue(mission.Unloading && SalvageDerelict.OrderOf(Player, left) == null, "setup: the full ship leaves to unload, its one unit still held");
        order.Platform.Die(null, cleanupOnly: true);
        order.Evaluate();
        Assert.IsFalse(Player.AI.HasGoal(order), "setup: on its way its platform is lost, and its last order with it");

        InOrbitAtHomeworld(ship);
        mission.Evaluate();
        Assert.IsTrue(Player.AI.HasGoal(mission) && mission.FlyingOut, "unloaded, it flies back for the derelict left in its system");
        mission.Evaluate();
        Assert.IsTrue(Player.AI.HasGoal(mission) && ship.AI.State == AIState.MoveTo, "on its way with no order yet");

        foreach (ShipModule unit in ship.Modules.Where(m => m.Salvage))
            unit.SetHealth(0f, "Test");
        mission.Evaluate();
        Assert.IsFalse(Player.AI.HasGoal(mission), "but with no live unit its mission ends");
    }

    [TestMethod]
    public void OnAutoSalvageTheShipSalvagesAShipAutoSalvageWouldNotRecover()
    {
        TwinShips();
        SalvageDerelict order = Ordered(Forms(Wreck));
        Derelict other = Forms(Wreck + new Vector2(0, 5_000f));
        Assert.IsTrue(Player.AddBuildableShip(ResourceManager.Ships.GetDesign("Dreadnought mk1-a")), "setup: the player builds the same design");
        Assert.IsFalse(SalvageDerelict.WouldRecover(Player, other), "setup: so Auto Salvage would not recover it");
        ShipBuilt(order);
        Parked(order);

        Player.AutoSalvage = true;
        MissionOf(order).Evaluate();
        Assert.IsTrue(SalvageDerelict.OrderOf(Player, other) is { Recovering: false }, "on Auto Salvage the ship's own order salvages it");
    }

    [TestMethod]
    public void OnAutoSalvageTheShipsOwnOrderJoinsItsMissionThoughAQueuedShipThereHasAFreeUnit()
    {
        Player.AutoSalvage = true;
        Derelict queuedFor = Forms(Wreck + new Vector2(0, 5_000f));
        SalvageDerelict first = Ordered(queuedFor);
        SalvageMission queued = MissionOf(first);
        Ship twin = IdleShip(SystemSite + new Vector2(0, 20_000f), design: TwinShip);
        SalvageDerelict order = Ordered(Forms(Wreck));
        SalvageMission mission = MissionOf(order);
        Assert.IsTrue(mission != queued && mission.SalvageShip == twin, "setup: with the queued ship's one unit held, the idle twin-unit ship takes the next order");
        Player.AI.RemoveGoal(first);
        Assert.IsTrue(Player.AI.HasGoal(queued) && queued.FreeUnits == 1, "setup: on automatic the queued ship waits, its unit free");
        twin.Position = mission.Site;
        twin.AI.ClearOrders(); // its move there over
        Parked(order);

        mission.Evaluate();
        Assert.AreSame(mission, SalvageDerelict.OrderOf(Player, queuedFor)?.Mission, "the parked ship's own order on the derelict left joins its own mission");
    }

    [TestMethod]
    public void OnAutoSalvageTheShipOrdersNothingItselfBesideARivalsColonyButOffItDoes()
    {
        TwinShips();
        SalvageDerelict order = Ordered(Forms(Wreck));
        Derelict other = Forms(Wreck + new Vector2(0, 5_000f));
        ShipBuilt(order);
        Parked(order);
        Player.GetRelations(Enemy).AtWar = Enemy.GetRelations(Player).AtWar = false;
        WreckSystem.PlanetList[0].SetOwner(Enemy);
        WreckSystem.UpdateOwnerList();

        Player.AutoSalvage = true;
        MissionOf(order).Evaluate();
        Assert.IsNull(SalvageDerelict.OrderOf(Player, other), "on Auto Salvage the colony of a rival with no treaty keeps the ship's own orders out, as it keeps Auto Salvage out");
        Player.AutoSalvage = false;
        MissionOf(order).Evaluate();
        Assert.IsNotNull(SalvageDerelict.OrderOf(Player, other), "off it, the ship orders the derelict as a click would");
    }

    [TestMethod]
    public void OnAutoSalvageTheShipOrdersNothingItselfWhereItKnowsOfHostileStrength()
    {
        TwinShips();
        Player.AutoSalvage = true;
        SalvageDerelict order = Ordered(Forms(Wreck));
        Derelict other = Forms(Wreck + new Vector2(0, 5_000f));
        ShipBuilt(order);
        Parked(order);
        Player.GetRelations(Enemy).AtWar = Enemy.GetRelations(Player).AtWar = true;
        SpawnShipNoCombatHoldPos("Vulcan Scout", Enemy, SystemSite + new Vector2(0, -20_000f));
        SpawnShipNoCombatHoldPos("Vulcan Scout", Player, SystemSite + new Vector2(0, -21_000f));
        UState.Objects.Update(new(time: 2f));
        Player.Threats.Update(new(time: 2f));
        AssertGreaterThan(Player.KnownEnemyStrengthNoResearchStationsIn(WreckSystem), 0f, "setup: the player knows of hostile strength there");

        MissionOf(order).Evaluate();
        Assert.IsNull(SalvageDerelict.OrderOf(Player, other), "on Auto Salvage, hostile strength it knows of keeps the ship's own orders out");
    }

    [TestMethod]
    public void ThePickTakesTheChosenShipOrTheRacesOrWithAutoPickTheBestForTheSystemButNoneShortOfALoadAUnit()
    {
        CanBuild(Player, TwinShip);
        Forms(Wreck);
        AssertEqual("Terran Salvage Ship", ShipBuilder.PickSalvageShip(Player, WreckSystem)?.Name, "with no choice, the race's");
        Player.data.CurrentSalvageShip = TwinShip;
        AssertEqual(TwinShip, ShipBuilder.PickSalvageShip(Player, WreckSystem)?.Name, "the Automation window's choice");

        ShipDesign shortHold = ((ShipDesign)ResourceManager.Ships.GetDesign(TwinShip)).GetClone("Short Twin Ship");
        shortHold.SetDesignSlots(ResourceManager.Ships.GetDesign(TwinShip).GetOrLoadDesignSlots()
                                 .Filter(s => ResourceManager.GetModuleTemplate(s.ModuleUID) is { CargoCapacity: <= 0f } or { Salvage: true }),
                                 updateRole: false);
        Assert.IsFalse(ShipBuilder.IsUsableSalvageShip(shortHold, Player, 0f), "setup: a design short of a load a unit");
        Player.AddBuildableShip(shortHold);
        Player.data.CurrentSalvageShip = shortHold.Name;
        AssertEqual("Terran Salvage Ship", ShipBuilder.PickSalvageShip(Player, WreckSystem)?.Name, "a choice short of a load a unit gives way to the race's");

        Player.AutoPickBestSalvageShip = true;
        AssertEqual("Terran Salvage Ship", ShipBuilder.PickSalvageShip(Player, WreckSystem)?.Name, "auto pick takes one unit for one derelict");
        Forms(Wreck + new Vector2(0, 5_000f));
        AssertEqual(TwinShip, ShipBuilder.PickSalvageShip(Player, WreckSystem)?.Name, "and two for two");
        AssertEqual(TwinShip, ShipBuilder.PickSalvageShip(Player)?.Name, "and the most units with no system");

        Assert.IsTrue(Player.RemoveBuildableShip(ResourceManager.Ships.GetDesign(TwinShip)), "setup: the twin ship no longer buildable");
        AssertEqual("Terran Salvage Ship", ShipBuilder.PickSalvageShip(Player, WreckSystem)?.Name,
                    "auto pick never takes one short of a load a unit, though it has the units for the system");
    }

    [TestMethod]
    public void NoPickTakesAShipShortOfTheWarpRangeTheAINeedsThoughThePlayerCanBuildIt()
    {
        Forms(Wreck);
        Forms(Wreck + new Vector2(0, 5_000f));
        ShipDesign noPower = ((ShipDesign)ResourceManager.Ships.GetDesign(TwinShip)).GetClone("Powerless Twin Ship");
        noPower.SetDesignSlots(ResourceManager.Ships.GetDesign(TwinShip).GetOrLoadDesignSlots()
                               .Filter(s => ResourceManager.GetModuleTemplate(s.ModuleUID) is not { PowerFlowMax: > 0f }),
                               updateRole: false);
        Player.AddBuildableShip(noPower);
        Assert.IsTrue(Player.ShipsWeCanBuildSnapshot.Contains(noPower), "setup: the player can build it");
        Assert.IsTrue(ShipResupply.HasRoomForSalvageLoads(noPower) && noPower.BaseOrdnanceMax >= SalvageDerelict.ConstructorLaunchCost(Player),
                      "setup: it holds a load a unit and the ordnance a launch takes");
        Assert.IsFalse(noPower.IsWarpRangeGood(GlobalStats.Defaults.MinAcceptableShipWarpRange, ShipStats.GetFTLSpeed(noPower, Player)),
                       "setup: with nothing making power it falls short of the warp range");

        Player.data.CurrentSalvageShip = noPower.Name;
        AssertEqual("Terran Salvage Ship", ShipBuilder.PickSalvageShip(Player, WreckSystem)?.Name, "the Automation window's choice gives way to the race's");
        Player.AutoPickBestSalvageShip = true;
        AssertEqual("Terran Salvage Ship", ShipBuilder.PickSalvageShip(Player, WreckSystem)?.Name,
                    "auto pick passes it over, though it has the units for the system's two derelicts");
    }

    [TestMethod]
    public void TheShipFliesToItsSiteTakingNoOrdersNoFightsAndNoUpkeep()
    {
        Derelict d = Forms(Wreck);
        SalvageDerelict order = Ordered(d);
        Ship ship = ShipBuilt(order, at: OnItsWay);
        SalvageMission mission = MissionOf(order);
        AssertEqual("FlyToSite", mission.StepName, "setup: the queue reports it built");

        mission.Evaluate();
        AssertEqual(AIState.MoveTo, ship.AI.State, "it flies out");
        Assert.IsTrue(ship.AI.IgnoreCombat, "fighting nothing on the way");
        order.Evaluate();
        Assert.IsNull(order.FinishedShip, "launching nothing on the way");
        Assert.IsTrue(ship.WorksForSalvage && !ship.CanBeScrapped && !ship.PlayerShipCanTakeFleetOrders(), "and takes no orders");
        AssertEqual(SalvageDerelict.Stage.ShipOnItsWay, SalvageDerelict.StageOf(Player, d, out _, out _), "its derelict reads that the ship is on its way");
        AssertEqual(string.Format(Localizer.Token(GameText.SalvageShipFlyingTo), WreckSystem.Name), ShipListScreenItem.GetStatusText(ship),
                    "and the ship that it flies to the system");
        AssertEqual(0f, ShipMaintenance.GetMaintenanceCost(ship, Player, 0), "it pays no upkeep");
    }

    [TestMethod]
    public void AtItsSiteTheShipParksWhereItsStatusReadsSalvaging()
    {
        SalvageDerelict order = Ordered(Forms(Wreck));
        Ship ship = ShipBuilt(order);
        Parked(order);
        AssertEqual(AIState.HoldPosition, ship.AI.State, "the ship holds at its site");
        Assert.IsTrue(ship.AI.IgnoreCombat && ship.AI.HasPriorityOrder, "fighting nothing, and nothing orders it off");
        AssertEqual(string.Format(Localizer.Token(GameText.SalvagingIn), WreckSystem.Name), ShipListScreenItem.GetStatusText(ship),
                    "its status reads that it salvages in the system");
        ship.LoadProduction(ship.CargoRoom);
        AssertEqual(Localizer.Token(GameText.SalvageHoldFull), ShipListScreenItem.GetStatusText(ship), "or that its hold is full");
    }

    [TestMethod]
    public void WithItsHoldFullTheShipUnloadsAtTheNearestSafeColonyOnceItsConstructorIsDoneAndComesBack()
    {
        SalvageDerelict order = Ordered(Forms(Wreck));
        Ship ship = ShipBuilt(order);
        SalvageMission mission = MissionOf(order);
        Launched(order);
        ship.LoadProduction(ship.CargoRoom);
        float load = ship.GetProduction();

        mission.Evaluate();
        Assert.IsTrue(mission.Parked(out _), "while its constructor is out, the full ship stays");
        PlatformBuilt(order);
        mission.Evaluate();
        Assert.IsTrue(mission.Unloading && mission.Colony == Homeworld, "once it is done, the ship leaves for its empire's nearest safe colony");
        AssertEqual(AIState.Orbit, ship.AI.State, "to orbit it");
        AssertEqual($"{Localizer.Token(GameText.DeliveringProductionTo)} {Homeworld.Name}", ShipListScreenItem.GetStatusText(ship), "delivering production there");
        mission.Evaluate();
        AssertEqual(0.01f, load, ship.GetProduction(), "unloading nothing on its way");

        Homeworld.ProdHere = 0f;
        InOrbitAtHomeworld(ship);
        mission.Evaluate();
        AssertEqual(0.01f, load, Homeworld.ProdHere, "there it unloads all into the colony's store");
        AssertEqual(0f, ship.GetProduction(), "emptying its hold");
        Assert.IsTrue(mission.FlyingOut, "and flies back to its site");
        mission.Evaluate();
        AssertEqual(AIState.MoveTo, ship.AI.State, "where it heads");
    }

    [TestMethod]
    public void OnItsWayToUnloadTheShipTakesNoRightClickOrdersSoItStillUnloadsAtItsColony()
    {
        SalvageDerelict order = Ordered(Forms(Wreck));
        Ship ship = ShipBuilt(order);
        SalvageMission mission = MissionOf(order);
        Launched(order);
        ship.LoadProduction(ship.CargoRoom);
        PlatformBuilt(order);
        mission.Evaluate();
        Assert.IsTrue(mission.Unloading && ship.AI.OrbitTarget == Homeworld, "setup: the full ship leaves to unload at the homeworld");

        var commands = new Ship_Game.Universe.ShipMoveCommands(Universe);
        Planet other = AddHomeWorldToEmpire(Homeworld.Position + new Vector2(0, 60_000f), Player);
        commands.RightClickOnPlanet(ship, other);
        AssertEqual(Homeworld, ship.AI.OrbitTarget, "a right-click on another planet orders it nowhere");
        commands.MoveShipToLocation(Empty<Ship>.Array, ship.Position + new Vector2(5_000f, 0), new Vector2(0, -1), ship);
        Assert.IsTrue(ship.AI.State == AIState.Orbit && ship.AI.OrbitTarget == Homeworld, "nor does one on open space");
        Ship enemy = SpawnShip("Vulcan Scout", Enemy, ship.Position + new Vector2(5_000f, 0));
        Assert.IsFalse(commands.AttackSpecificShip(ship, enemy), "and one on an enemy ship is refused");
        Assert.IsFalse(ship.CanBeAddedToFleets(), "nor can it join a fleet");

        InOrbitAtHomeworld(ship);
        mission.Evaluate();
        Assert.IsTrue(ship.GetProduction() == 0f && mission.FlyingOut, "so it unloads at its colony and flies back");
    }

    [TestMethod]
    public void WithoutTheOrdnanceALaunchTakesTheShipRearmsAtAColonyAndComesBack()
    {
        SalvageDerelict order = Ordered(Forms(Wreck));
        Ship ship = ShipBuilt(order);
        SalvageMission mission = MissionOf(order);
        ship.ChangeOrdnance(-ship.Ordinance);
        Parked(order);
        order.Evaluate();
        Assert.IsNull(order.FinishedShip, "setup: with no ordnance, its unit launches nothing");

        mission.Evaluate();
        Assert.IsTrue(mission.Unloading && mission.Colony == Homeworld, "the ship leaves to rearm at its empire's nearest safe colony");
        AssertEqual($"{Localizer.Token(GameText.RearmingAt)} {Homeworld.Name}", ShipListScreenItem.GetStatusText(ship), "as its status says");
        InOrbitAtHomeworld(ship);
        mission.Evaluate();
        Assert.IsTrue(mission.Unloading, "there it waits while it rearms");

        ship.ChangeOrdnance(ship.OrdinanceMax);
        mission.Evaluate();
        Assert.IsTrue(mission.FlyingOut, "and once rearmed it flies back to its site");
    }

    [TestMethod]
    public void AShipWhoseFullLoadPaysForNoLaunchNeverLeavesToRearm()
    {
        SalvageDerelict order = Ordered(Forms(Wreck));
        Ship ship = ShipBuilt(order);
        SalvageMission mission = MissionOf(order);
        ship.ChangeOrdnance(-ship.Ordinance);
        ship.OrdinanceMax = 1f; // as with a constructor whose launch costs more than the ship holds
        Parked(order);
        order.Evaluate();
        Assert.IsNull(order.FinishedShip, "setup: its unit launches nothing");

        mission.Evaluate();
        Assert.IsTrue(mission.Parked(out _), "the ship stays at its site, as no rearming pays for the launch");
    }

    [TestMethod]
    public void WithEveryOrderLaunchedAShipOutOfOrdnanceStaysAtItsSite()
    {
        SalvageMission mission = MissionOf(AtWork(Forms(Wreck)));
        Ship ship = mission.SalvageShip;
        ship.ChangeOrdnance(-ship.Ordinance);

        mission.Evaluate();
        Assert.IsTrue(mission.Parked(out _), "with no launch waiting, a ship out of ordnance does not leave to rearm");
    }

    [TestMethod]
    public void AColonyTurnedUnsafeOnTheWayIsSwappedForTheNearestSafeOne()
    {
        Planet near = AddHomeWorldToEmpire(SystemSite + new Vector2(-100_000f, 0), Player);
        near.Name = "Near";
        Player.UpdateRallyPoints();
        SalvageDerelict order = Ordered(Forms(Wreck));
        Ship ship = ShipBuilt(order);
        SalvageMission mission = MissionOf(order);
        Parked(order);
        ship.LoadProduction(ship.CargoRoom);
        mission.Evaluate();
        Assert.IsTrue(mission.Unloading && mission.Colony == near, "setup: the full ship leaves for its empire's nearest safe colony");

        near.Quarantine = true;
        Player.UpdateRallyPoints();
        mission.Evaluate();
        Assert.IsTrue(mission.Unloading && mission.Colony == Homeworld, "turned unsafe on the way, it is swapped for the nearest safe one");
        Assert.AreSame(Homeworld, ship.AI.OrbitTarget, "which the ship heads for");
    }

    [TestMethod]
    public void WithNoSafeColonyAFullHoldIsPaidAsCreditsAtTheTaxRate()
    {
        SalvageDerelict order = Ordered(Forms(Wreck));
        Ship ship = ShipBuilt(order);
        SalvageMission mission = MissionOf(order);
        Parked(order);
        ship.LoadProduction(ship.CargoRoom);
        float load = ship.GetProduction();
        Homeworld.Quarantine = true;
        Player.UpdateRallyPoints();
        Assert.IsFalse(Homeworld.Safe, "setup: the player has no safe colony");
        float credits = Player.ExcessCargoMoney;

        mission.Evaluate();
        AssertEqual(0f, ship.GetProduction(), "its hold is emptied");
        AssertEqual(0.01f, credits + load * Player.data.TaxRate, Player.ExcessCargoMoney, "and paid as credits at the tax rate");
        Assert.IsTrue(mission.Parked(out _), "while it stays at its site");
    }

    [TestMethod]
    public void ItsOrdersDoneTheShipBringsHomeWhatItHoldsAndWaitsInOrbitThereAShipLikeAnyOther()
    {
        Derelict d = Forms(Wreck);
        SalvageDerelict order = Ordered(d);
        Ship ship = ShipBuilt(order);
        SalvageMission mission = MissionOf(order);
        Parked(order);
        ship.LoadProduction(30f);
        SalvageDerelict.Cancel(Player, d);
        Assert.IsFalse(Player.AI.HasGoal(order), "setup: its last order ends");

        mission.Evaluate();
        Assert.IsTrue(mission.Unloading, "the ship brings home what it holds");
        Homeworld.ProdHere = 0f;
        InOrbitAtHomeworld(ship);
        mission.Evaluate();
        AssertEqual(0.01f, 30f, Homeworld.ProdHere, "unloading it");
        Assert.IsFalse(Player.AI.HasGoal(mission), "and its mission is done");
        Assert.IsTrue(ship.AI.State == AIState.Orbit && ship.AI.OrbitTarget == Homeworld, "it waits in orbit there");
        Assert.IsTrue(!ship.WorksForSalvage && ship.CanBeScrapped && SalvageMission.IsIdle(ship, Player), "a ship like any other, idle for its next work");
        AssertGreaterThan(ShipMaintenance.GetMaintenanceCost(ship, Player, 0), 0f, "that pays its upkeep");
    }

    [TestMethod]
    public void AnEmptyShipNeverUnloadedWaitsAtTheNearestSafeColony()
    {
        Derelict d = Forms(Wreck);
        SalvageDerelict order = Ordered(d);
        Ship ship = ShipBuilt(order, at: OnItsWay);
        SalvageMission mission = MissionOf(order);
        mission.Evaluate();
        SalvageDerelict.Cancel(Player, d);

        mission.Evaluate();
        Assert.IsFalse(Player.AI.HasGoal(mission), "with nothing aboard its mission ends at once");
        Assert.IsTrue(ship.AI.State == AIState.Orbit && ship.AI.OrbitTarget == Homeworld, "and it goes to orbit the nearest safe colony");
    }

    [TestMethod]
    public void AShipReleasedAtItsSiteWaitsAtTheColonyItLastUnloadedAtThoughAnotherIsNearer()
    {
        Planet near = AddHomeWorldToEmpire(SystemSite + new Vector2(-100_000f, 0), Player);
        near.Quarantine = true;
        Player.UpdateRallyPoints();
        Derelict d = Forms(Wreck);
        SalvageDerelict order = Ordered(d);
        Ship ship = ShipBuilt(order);
        SalvageMission mission = MissionOf(order);
        Parked(order);
        ship.LoadProduction(ship.CargoRoom);
        mission.Evaluate();
        Assert.IsTrue(mission.Unloading && mission.Colony == Homeworld, "setup: with the nearer colony unsafe, the ship unloads at the homeworld");
        InOrbitAtHomeworld(ship);
        mission.Evaluate();
        Assert.IsTrue(mission.FlyingOut, "setup: and flies back");

        near.Quarantine = false;
        Player.UpdateRallyPoints();
        ship.Position = mission.Site;
        SalvageDerelict.Cancel(Player, d);
        mission.Evaluate();
        Assert.IsFalse(Player.AI.HasGoal(mission), "its orders done, the mission ends");
        Assert.AreSame(Homeworld, ship.AI.OrbitTarget, "and the ship waits at the colony it last unloaded at, not the nearer one");
    }

    [TestMethod]
    public void ADerelictTakenBeforeTheLaunchEndsTheOrderAndTheShipGoesHome()
    {
        Derelict taken = Forms(Wreck);
        SalvageDerelict order = Ordered(taken);
        Ship ship = ShipBuilt(order, at: OnItsWay);
        SalvageMission mission = MissionOf(order);
        mission.Evaluate();
        Assert.IsTrue(taken.TryClaim(Enemy), "setup: a rival claims it first");

        order.Evaluate();
        Assert.IsFalse(Player.AI.HasGoal(order), "the order ends");
        mission.Evaluate();
        Assert.IsFalse(Player.AI.HasGoal(mission), "and so does its mission");
        AssertEqual(AIState.Orbit, ship.AI.State, "its ship going home");
    }

    [TestMethod]
    public void AQueuedSalvageShipRefittedToAnotherDesignEndsItsMissionOnceBuiltUntold()
    {
        Derelict d = Forms(Wreck);
        SalvageDerelict order = Ordered(d);
        SalvageMission mission = Queued(order);
        Ship salvageShip = SpawnShip("Terran Salvage Ship", Player, Homeworld.Position + new Vector2(5_000f, 0));
        IShipDesign other = ResourceManager.Ships.GetDesign("Dreadnought mk1-a");
        Homeworld.Construction.RefitShipsBeingBuilt(salvageShip, other); // as Refit All does to the ships being built
        Assert.AreSame(other, QueuedFor(Homeworld, mission).ShipData, "setup: Refit All turns the queued salvage ship into another design");

        Ship built = SpawnShip(other.Name, Player, OnItsWay);
        TrackNotifications();
        mission.ReportShipComplete(built);
        mission.Evaluate();
        order.Evaluate();
        Assert.IsFalse(Player.AI.HasGoal(mission) || Player.AI.HasGoal(order), "built, it ends the mission and its order");
        AssertEqual(0, Notes.GetNotificationsAtomic().Length, "telling of no salvage ship lost");
        Assert.IsTrue(built.Active && built.CanBeScrapped && !built.WorksForSalvage, "and is a ship like any other");
        Assert.IsNull(d.Claimant, "the derelict left unclaimed");
    }

    [TestMethod]
    public void AnOrderSavedWithAStationsWorkEndsOnLoad()
    {
        Derelict d = Forms(Wreck);
        Ship constructor = SpawnShip(Constructor, Player, Wreck + new Vector2(5_000f, 0));
        Player.AI.AddGoal(new SalvageDerelict(Player) { DerelictId = d.Id, FinishedShip = constructor }); // as one saved before salvage ships
        RunObjectsSim(TestSimStep); // a ship joins its empire's list, which the save writes, on an objects update

        UState.StarDate = 1001f; // the loader needs a stardate past 1000
        SavedGame save = Universe.Save("UnitTest.SalvageMission", throwOnError: true);
        UniverseScreen loaded = LoadGame.Load(save.SaveFile, noErrorDialogs: true, startSimThread: false);
        Assert.IsNotNull(loaded, "setup: the save loads");

        Empire player = loaded.UState.Player;
        SalvageDerelict order = player.AI.FindGoals<SalvageDerelict>().Single();
        Ship loadedConstructor = loaded.UState.Objects.FindShip(constructor.Id);
        Assert.IsTrue(order.Derelict != null && loadedConstructor != null, "setup: with its derelict still there to work, and its constructor");
        order.Evaluate();
        Assert.IsFalse(player.AI.HasGoal(order), "it ends");
        Assert.IsTrue(loadedConstructor.ScuttleTimer > 0f && !loadedConstructor.Active, "and scuttles its constructor at once");
    }

    [TestMethod]
    public void AMissionFlyingOutSurvivesSaveAndLoad()
    {
        Derelict d = Forms(Wreck);
        SalvageDerelict order = Ordered(d, recover: true);
        Ship ship = ShipBuilt(order, at: OnItsWay);
        MissionOf(order).Evaluate();
        RunObjectsSim(TestSimStep); // a ship joins its empire's list, which the save writes, on an objects update

        UState.StarDate = 1001f; // the loader needs a stardate past 1000
        SavedGame save = Universe.Save("UnitTest.SalvageMission", throwOnError: true);
        UniverseScreen loaded = LoadGame.Load(save.SaveFile, noErrorDialogs: true, startSimThread: false);
        Assert.IsNotNull(loaded, "setup: the save loads");

        Empire player = loaded.UState.Player;
        SalvageMission mission = player.AI.FindGoals<SalvageMission>().Single();
        Assert.IsTrue(mission.FlyingOut, "the mission still flies out");
        AssertEqual(ship.Id, mission.SalvageShip?.Id ?? 0, "with its ship");
        SalvageDerelict loadedOrder = player.AI.FindGoals<SalvageDerelict>().Single();
        Assert.AreSame(mission, loadedOrder.Mission, "holding its order");
        Assert.IsTrue(loadedOrder.Recovering, "to recover the derelict");
        Assert.IsTrue(mission.SalvageShip.WorksForSalvage, "its ship at work");
        mission.Evaluate();
        loadedOrder.Evaluate();
        Assert.IsTrue(player.AI.HasGoal(mission) && player.AI.HasGoal(loadedOrder), "and they stand on");
    }

    [TestMethod]
    public void FlownOutTheShipParksOnceItHasStoppedAtItsSiteAndHoldsThereFightingNothing()
    {
        SalvageDerelict order = Ordered(Forms(Wreck));
        SalvageMission mission = MissionOf(order);
        Ship ship = ShipBuilt(order, at: mission.Site + new Vector2(30_000f, 0));
        RunWithMission(mission, 120, () => mission.FlyingOut);
        Assert.IsTrue(mission.Parked(out _), "setup: it flew to its site and parked");

        float farthest = 0f;
        for (int second = 0; second < 20; ++second)
        {
            RunSimWhile((1, fatal: false));
            mission.Evaluate();
            farthest = Math.Max(farthest, ship.Position.Distance(mission.Site));
        }
        AssertLessThan(farthest, 300f, "stopped before it parked, it never drifts off its site");
        Assert.IsTrue(ship.AI.State == AIState.HoldPosition && ship.AI.IgnoreCombat && ship.AI.HasPriorityOrder,
                      "where it holds, fighting nothing, and nothing orders it off");
    }

    [TestMethod]
    public void PushedOffItsSpotTheParkedShipFliesBackStillFightingNothing()
    {
        SalvageDerelict order = Ordered(Forms(Wreck));
        Ship ship = ShipBuilt(order);
        SalvageMission mission = MissionOf(order);
        Parked(order);
        Vector2 spot = ship.Position;
        ship.Position = spot + new Vector2(600f, 0);
        RunSimWhile((1, fatal: false));
        Assert.IsFalse(ship.AI.IgnoreCombat, "setup: its hold flies it back to its spot, which drops its order to fight nothing");

        mission.Evaluate();
        Assert.IsTrue(ship.AI.IgnoreCombat && ship.AI.HasPriorityOrder, "its next turn keeps it from fights and other orders");
        for (int second = 0; second < 30 && ship.Position.OutsideRadius(spot, 100f); ++second)
        {
            RunSimWhile((1, fatal: false));
            mission.Evaluate();
        }
        AssertLessThan(ship.Position.Distance(spot), 100f, "it flies back to its spot");
        Assert.IsTrue(mission.Parked(out _) && ship.AI.IgnoreCombat && ship.AI.HasPriorityOrder, "parked there still, fighting nothing");
    }

    [TestMethod]
    public void OnItsWayToUnloadAnOrbitOrderElsewhereIsUndoneOnItsNextTurn()
    {
        SalvageDerelict order = Ordered(Forms(Wreck));
        Ship ship = ShipBuilt(order);
        SalvageMission mission = MissionOf(order);
        Parked(order);
        ship.LoadProduction(ship.CargoRoom);
        mission.Evaluate();
        Assert.IsTrue(mission.Unloading && ship.AI.OrbitTarget == Homeworld, "setup: the full ship leaves to unload at the homeworld");

        Planet other = AddHomeWorldToEmpire(Homeworld.Position + new Vector2(0, 60_000f), Player);
        ship.AI.OrderOrbitPlanet(other, clearOrders: true); // as an order no guard refuses would
        mission.Evaluate();
        Assert.IsTrue(ship.AI.State == AIState.Orbit && ship.AI.OrbitTarget == Homeworld, "its next turn sends it back to orbit its colony");
    }

    [TestMethod]
    public void AMissionWaitingForItsShipSurvivesSaveAndLoad()
    {
        SalvageDerelict order = Ordered(Forms(Wreck));
        SalvageMission mission = Queued(order);
        AssertEqual("WaitForShip", mission.StepName, "setup: its ship is queued");

        UState.StarDate = 1001f; // the loader needs a stardate past 1000
        SavedGame save = Universe.Save("UnitTest.SalvageMission", throwOnError: true);
        UniverseScreen loaded = LoadGame.Load(save.SaveFile, noErrorDialogs: true, startSimThread: false);
        Assert.IsNotNull(loaded, "setup: the save loads");

        Empire player = loaded.UState.Player;
        SalvageMission loadedMission = player.AI.FindGoals<SalvageMission>().Single();
        AssertEqual("WaitForShip", loadedMission.StepName, "the mission still waits for its ship");
        Planet shipyard = loadedMission.PlanetBuildingAt;
        AssertEqual(Homeworld.Id, shipyard?.Id ?? 0, "at its shipyard");
        Assert.IsTrue(shipyard.ConstructionQueue.Any(q => q.Goal == loadedMission), "whose queue still builds the ship for it");
        Assert.AreSame(loadedMission, player.AI.FindGoals<SalvageDerelict>().Single().Mission, "holding its order");
        loadedMission.Evaluate();
        Assert.IsTrue(player.AI.HasGoal(loadedMission) && loadedMission.StepName == "WaitForShip", "and it waits on");
    }

    [TestMethod]
    public void AMissionUnloadingSurvivesSaveAndLoad()
    {
        SalvageDerelict order = Ordered(Forms(Wreck));
        Ship ship = ShipBuilt(order);
        SalvageMission mission = MissionOf(order);
        Parked(order);
        ship.LoadProduction(ship.CargoRoom);
        float load = ship.GetProduction();
        mission.Evaluate();
        Assert.IsTrue(mission.Unloading, "setup: the full ship leaves to unload at the homeworld");
        RunObjectsSim(TestSimStep); // a ship joins its empire's list, which the save writes, on an objects update

        UState.StarDate = 1001f; // the loader needs a stardate past 1000
        SavedGame save = Universe.Save("UnitTest.SalvageMission", throwOnError: true);
        UniverseScreen loaded = LoadGame.Load(save.SaveFile, noErrorDialogs: true, startSimThread: false);
        Assert.IsNotNull(loaded, "setup: the save loads");

        Empire player = loaded.UState.Player;
        SalvageMission loadedMission = player.AI.FindGoals<SalvageMission>().Single();
        Assert.IsTrue(loadedMission.Unloading, "the mission still unloads");
        AssertEqual(Homeworld.Id, loadedMission.Colony?.Id ?? 0, "at its colony");
        Ship loadedShip = loadedMission.SalvageShip;
        AssertEqual(ship.Id, loadedShip?.Id ?? 0, "with its ship");
        AssertEqual(0.01f, load, loadedShip.GetProduction(), "and its load");
        Assert.IsTrue(loadedShip.AI.OrbitTarget == loadedMission.Colony && loadedShip.AI.IgnoreCombat, "on its way there, fighting nothing");
        loadedMission.Evaluate();
        Assert.IsTrue(player.AI.HasGoal(loadedMission) && loadedMission.Unloading, "and it unloads on");
    }

    [TestMethod]
    public void AShipJustReportedBuiltIsItsMissionsAtOnceSoAnOrderElsewhereDoesNotTakeIt()
    {
        SalvageDerelict order = Ordered(Forms(Wreck));
        Ship ship = ShipBuilt(order, at: OnItsWay);
        UState.Objects.UpdateLists(); // it joins its empire's ships
        SalvageMission mission = MissionOf(order);
        Assert.IsTrue(mission.FlyingOut && mission.SalvageShip == null, "setup: the queue reported it built, before the mission's turn takes it");
        Assert.IsTrue(ship.WorksForSalvage && !SalvageMission.IsIdle(ship, Player), "it is its mission's at once");

        Vector2 elsewhere = new(-600_000f, 600_000f);
        AddDummyPlanet(elsewhere, 1f, 1f, 1f, elsewhere + new Vector2(0, 60_000f), explored: true);
        SalvageMission there = MissionOf(Ordered(Forms(elsewhere + new Vector2(30_000f, 0))));
        Assert.IsTrue(there != mission && there.SalvageShip == null, "so an order elsewhere does not take it");
        mission.Evaluate();
        Assert.AreSame(ship, mission.SalvageShip, "and its mission's turn takes it");
    }

    [TestMethod]
    public void LaunchedFromItsShipyardTheShipFliesOutFightingNothingOnceItsLaunchEnds()
    {
        SalvageDerelict order = Ordered(Forms(Wreck));
        Ship ship = ShipBuilt(order, at: OnItsWay);
        SalvageMission mission = MissionOf(order);
        ship.InitLaunch(LaunchPlan.Planet, 0f); // as its shipyard launches it
        mission.Evaluate();
        Assert.IsTrue(ship.IsLaunching && ship.AI.State == AIState.MoveTo && ship.AI.IgnoreCombat, "setup: ordered out as it launches, fighting nothing");
        RunSimWhile((simTimeout: 20, fatal: true), () => ship.IsLaunching);
        Assert.IsFalse(ship.AI.IgnoreCombat, "setup: the launch's end lets it fight");

        mission.Evaluate();
        Assert.IsTrue(ship.AI.State == AIState.MoveTo && ship.AI.IgnoreCombat, "its next turn keeps it from fights on its way");
    }

    [TestMethod]
    public void TheDerelictsWindowCountsThePlayersSalvageShipsAtWorkAndIdle()
    {
        AtWork(Forms(Wreck));
        IdleShip(Homeworld.Position + new Vector2(5_000f, 0));
        IdleShip(Homeworld.Position + new Vector2(8_000f, 0));
        SpawnShip("Cordrazine Salvage Ship", Enemy, new Vector2(900_000f, 200_000f));
        RunObjectsSim(TestSimStep); // ships join their empires' lists on an objects update

        var window = new SystemDerelictsScreen(Universe, WreckSystem);
        window.LoadContent();
        AssertEqual("Salvage ships: 1 working, 2 idle", window.ShipsCount.Text.Text, "the player's, not another empire's");
    }

    [TestMethod]
    public void TheDerelictsWindowPausesTheGameAndReadsTheGoalsAsItOpensAndOnceAClickHasReachedTheSimThread()
    {
        Derelict d = Forms(Wreck);
        UState.Paused = false;
        var window = new SystemDerelictsScreen(Universe, WreckSystem);
        window.LoadContent();
        Assert.IsTrue(UState.Paused, "the window pauses the game");
        AssertEqual(SalvageDerelict.Stage.None, window.StageShown(d), "setup: nobody salvages the derelict");

        Assert.IsTrue(d.TryClaim(Enemy), "setup: a rival claims the derelict");
        window.Update(0f);
        AssertEqual(SalvageDerelict.Stage.None, window.StageShown(d), "the window does not read the goals again while nothing in it is clicked");

        // as a row's Salvage or Recover button does after its click
        typeof(SystemDerelictsScreen).GetMethod("ReadAfterClick", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic)!.Invoke(window, null);
        window.Update(0f);
        AssertEqual(SalvageDerelict.Stage.None, window.StageShown(d), "nor before the click reaches the sim thread");
        Universe.InvokePendingSimThreadActions();
        window.Update(0f);
        AssertEqual(SalvageDerelict.Stage.Salvaging, window.StageShown(d), "but reads them once it has");
    }
}
