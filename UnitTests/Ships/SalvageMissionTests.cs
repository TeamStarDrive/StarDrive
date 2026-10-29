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
/// pick the best for the system, never one short of a load a unit. A shipyard lost, or turned unsafe while a safe one
/// is left, has the ship queued again at the nearest; taking it out of the queue, or calling off its orders, ends the
/// mission. The ship flies to its site taking no orders and no fights, paying no upkeep, launching nothing on its
/// way, parks there, and goes to its empire's nearest safe colony to unload when its hold is full, or to rearm when it
/// lacks the ordnance a waiting launch takes and its full load pays for, once no constructor of its is out; it unloads
/// once in orbit there, a colony turned unsafe on its way swapped for the nearest safe one, then comes back. With no
/// safe colony its hold is paid as credits. Its orders done, it brings home what it holds and waits in orbit at the
/// colony it last unloaded at, though another be nearer, or, never unloaded, at the nearest safe colony, a ship like
/// any other. A queued salvage ship refitted to another design ends its mission once built, untold. A mission flying
/// out survives a save, and an order saved with a salvage station's work ends on load. The derelicts window counts the
/// player's salvage ships at work and idle.
/// </summary>
[TestClass]
public class SalvageMissionTests : SalvageOrderTest
{
    NotificationManager Notes;

    void TrackNotifications() => Universe.NotificationManager = Notes = new NotificationManager(Universe.ScreenManager, Universe);

    static QueueItem QueuedFor(Planet shipyard, Goal mission) => shipyard.ConstructionQueue.FirstOrDefault(q => q.Goal == mission);

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
        Assert.IsFalse(ShipBuilder.IsUsableSalvageShip(shortHold), "setup: a design short of a load a unit");
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
}
