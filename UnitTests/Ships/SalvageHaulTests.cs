using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Ship_Game;
using Ship_Game.AI;
using Ship_Game.Commands.Goals;
using Ship_Game.GameScreens.LoadGame;
using Ship_Game.Gameplay;
using Ship_Game.Ships;
using Rectangle = SDGraphics.Rectangle;
using Vector2 = SDGraphics.Vector2;

namespace UnitTests.Ships;

/// <summary>
/// Freighters haul the production a salvage station holds to colonies. Each turn the player's station calls an idle
/// freighter for each average freighter load it holds beyond the room in the holds of those already on their way,
/// and for all it holds once it has no room for another platform load, with at most 3 on their way and none while
/// an armed hostile is in its sensor range; no freighter is built for it. A haul goes to the closest of the empire's
/// colonies importing production that a freighter reaches, then to the closest of its trade partners' with
/// inter-empire trade allowed, then to the closest of its colonies with room for production and open to trade, by
/// the quickest freighter there; a freighter holding other goods or with little room goes on none. Areas of
/// operation and trade routes rule as for research and mining stations, and with auto trade off only freighters set
/// to trade production haul. The freighter lands on the station, fills its hold whatever its colony's room, and
/// delivers like any freighter, taxed only for what the colony takes; outside the station's system it waits while
/// the station sees an armed hostile, and gives up as at a blockaded colony. Once nothing is left to salvage in its
/// system, no order of its empire's there and no derelict its empire knows of there that another empire does not
/// work from its station there, the station calls for all it has left, and once it holds less than 1 it is scrapped
/// with no refund, the player told unless salvage alerts are off; not while it is yet to be placed in its system or
/// the player scuttles it. Its status says how many freighters are on their way, that it waits for them while it
/// holds production, or that it salvages. AI empires' stations call none yet, and are not scrapped.
/// </summary>
[TestClass]
public class SalvageHaulTests : StarDriveTest
{
    static readonly Vector2 SystemSite = new(600_000f, 600_000f);
    static readonly Vector2 StationSite = SystemSite + new Vector2(30_000f, 0);
    static readonly Vector2 WreckSite = SystemSite + new Vector2(-30_000f, 0);
    readonly Planet Local; // in the station's system
    readonly IShipDesign Station;
    Derelict Work; // the derelict a station at work has left to salvage
    NotificationManager Notes;

    public SalvageHaulTests()
    {
        LoadStarterShips("Terran Basic Salvage Station", "Owlwok Freighter S", "Dreadnought mk1-a", ShipDesign.SalvagePlatformName);
        CreateUniverseAndPlayerEmpire();
        UState.Objects.EnableParallelUpdate = false;
        Local = AddDummyPlanet(SystemSite, 1f, 1f, 1f, SystemSite + new Vector2(0, 60_000f), explored: true);
        Local.Name = "Haul Local";
        Planet home = AddHomeWorldToEmpire(new Vector2(200_000f), Player, new Vector2(205_000f), explored: true);
        home.PS = Planet.GoodState.STORE; // the farthest colony, which imports nothing
        home.UpdateIncomingTradeGoods();
        Assert.IsTrue(ResourceManager.Ships.GetDesign("Terran Basic Salvage Station", out Station), "setup: the station is loaded");
        AssertEqual(20f, Player.AverageFreighterCargoCap, "setup: an average freighter load is 20 at game start");
    }

    [TestCleanup]
    public override void Cleanup()
    {
        UState.Derelicts.Clear();
        base.Cleanup();
    }

    // a salvageable derelict of the owner's ship, so the owner knows of it; the ship itself is gone
    Derelict Wreck(Vector2 at, Empire owner = null)
    {
        TestShip ship = SpawnShip("Dreadnought mk1-a", owner ?? Player, at);
        Assert.IsTrue(UState.Derelicts.TryAdd(ship, salvageable: true, seen: false), "setup: a salvageable derelict");
        Universe.InvokePendingSimThreadActions();
        ship.QueueTotalRemoval();
        Derelict d = UState.Derelicts.Current.Last();
        Assert.IsTrue(d.CanBeSalvaged && d.IsKnownBy(owner ?? Player), "setup: its owner knows of it");
        return d;
    }

    // the empire's station in the system, its order at work, with a derelict there it knows of unless it has nothing left to salvage, and placed in its system a step after it spawns unless asked
    Ship BuiltStation(Empire owner = null, bool atWork = true, bool placed = true)
    {
        owner ??= Player;
        if (atWork)
        {
            Work = Wreck(WreckSite, owner);
            AssertEqual(Local.System, Work.System, "setup: the derelict is in the station's system");
        }
        var order = new SalvageOps(owner, Local.System, StationSite, Station);
        owner.AI.AddGoal(order);
        order.AdvanceToNextStep(); // past the queueing, which needs a port
        order.TargetShip = SpawnShip(Station.Name, owner, StationSite);
        order.Evaluate();
        AssertEqual("Salvage", order.StepName, "setup: the station is built");
        if (placed)
            Place(order.TargetShip);
        return order.TargetShip;
    }

    void Place(Ship station)
    {
        RunObjectsSim(TestSimStep);
        RunObjectsSim(TestSimStep);
        AssertEqual(Local.System, station.System, "setup: the station is placed in its system");
    }

    // a station holding this much, with room for far more
    Ship StationHolding(float production, Empire owner = null, bool atWork = true)
    {
        Ship station = BuiltStation(owner, atWork);
        station.CargoSpaceMax = 1000f;
        AssertEqual(0.01f, production, station.LoadProduction(production), "setup: the station holds the production");
        return station;
    }

    static SalvageOps OrderOf(Ship station)
        => (SalvageOps)station.Loyalty.AI.FindGoal(g => g is SalvageOps && g.TargetShip == station);

    // a turn of the station's order
    static void Turn(Ship station) => OrderOf(station).Evaluate();

    // an idle freighter with a hold of 20, set to trade production
    Ship Freighter(Vector2 at, Empire owner = null)
    {
        Ship freighter = SpawnShip("Owlwok Freighter S", owner ?? Player, at);
        Assert.IsTrue(freighter.IsIdleFreighter, "setup: an idle freighter");
        freighter.CargoSpaceMax = 20f;
        freighter.TransportingProduction = true;
        RunObjectsSim(TestSimStep); // a ship joins its empire's list of ships with the next step
        return freighter;
    }

    int Colonies;

    Planet Colony(Vector2 at, Empire owner = null)
    {
        Planet colony = AddDummyPlanet(at, 1f, 1f, 1f, at + new Vector2(5_000f), explored: true);
        colony.Name = $"Haul Colony {++Colonies}";
        colony.SetOwner(owner ?? Player);
        colony.PS = Planet.GoodState.STORE;
        colony.Storage.Max = 200f;
        colony.ProdHere = 0f;
        colony.UpdateIncomingTradeGoods();
        return colony;
    }

    Planet Importer(Vector2 at, Empire owner = null, int slots = 2)
    {
        Planet colony = Colony(at, owner);
        colony.PS = Planet.GoodState.IMPORT;
        colony.ManualProdImportSlots = slots;
        colony.UpdateIncomingTradeGoods();
        AssertGreaterThan(colony.FreeProdImportSlots, 0, "setup: the colony imports production");
        return colony;
    }

    static bool Hauls(Ship freighter, Ship station, out Planet colony)
    {
        colony = null;
        if (freighter.AI.OrderQueue.TryPeekFirst(out ShipAI.ShipGoal g) && g.Plan == ShipAI.Plan.PickupGoodsFromStation
            && g.Trade.FromStation == station)
        {
            colony = g.Trade.ImportTo;
            return true;
        }
        return false;
    }

    static bool Hauls(Ship freighter, Ship station) => Hauls(freighter, station, out _);

    static bool Delivers(Ship freighter, Planet colony)
        => freighter.AI.OrderQueue.TryPeekFirst(out ShipAI.ShipGoal g) && g.Plan == ShipAI.Plan.DropOffGoods && g.Trade.ImportTo == colony;

    [TestMethod]
    public void AStationCallsAFreighterOnceItHoldsAnAverageFreighterLoad()
    {
        Planet importer = Importer(SystemSite + new Vector2(0, 200_000f));
        Ship freighter = Freighter(StationSite + new Vector2(5_000f, 0));
        Ship station = StationHolding(19f);

        Turn(station);
        Assert.IsFalse(Hauls(freighter, station), "less than an average freighter load calls no freighter");

        station.LoadProduction(1f);
        Turn(station);
        Assert.IsTrue(Hauls(freighter, station, out Planet colony) && colony == importer, "a whole load calls one, for the importing colony");
        Assert.IsTrue(freighter.AI.State == AIState.SystemTrader && !freighter.IsIdleFreighter, "the freighter is trading now");
    }

    [TestMethod]
    public void AStationWithNoRoomForAnotherPlatformLoadCallsForAllItHolds()
    {
        Importer(SystemSite + new Vector2(0, 200_000f), slots: 10);
        typeof(Empire).GetProperty(nameof(Empire.AverageFreighterCargoCap)).SetValue(Player, 400f);
        AssertEqual(75f, ShipResupply.SalvageLoad, "setup: a platform load is 75");
        Ship freighter = Freighter(StationSite + new Vector2(5_000f, 0));
        Ship station = BuiltStation();
        station.CargoSpaceMax = 316f;
        station.LoadProduction(240f);

        Turn(station);
        Assert.IsFalse(Hauls(freighter, station), "240 of 316, with room for another load of 75 and less than an average load of 400, calls none");

        station.LoadProduction(10f);
        Turn(station);
        Assert.IsTrue(Hauls(freighter, station), "250, with room for only 66, calls one");
    }

    [TestMethod]
    public void ItCallsOneFreighterPerLoadBeyondTheHoldsOfThoseOnTheirWayAndAtMostThree()
    {
        Importer(SystemSite + new Vector2(0, 200_000f), slots: 10);
        Ship[] freighters = Enumerable.Range(0, 5).Select(i => Freighter(StationSite + new Vector2(5_000f + i * 1_000f, 0))).ToArray();
        Ship station = StationHolding(45f);

        Turn(station);
        AssertEqual(2, freighters.Count(f => Hauls(f, station)), "45 calls two with holds of 20, leaving 5 uncalled");
        AssertEqual(2, SalvageOps.HaulsComing(station, out float holds), "both are on their way");
        AssertEqual(0.01f, 40f, holds, "with 40 of room between them");

        Turn(station);
        AssertEqual(2, freighters.Count(f => Hauls(f, station)), "the next turn the holds on their way cover all but 5, so none more");

        station.LoadProduction(55f);
        Turn(station);
        AssertEqual(3, freighters.Count(f => Hauls(f, station)), "100 held calls only a third, as at most 3 are on their way");
    }

    [TestMethod]
    public void AHaulGoesToTheClosestImportingColonyByTheQuickestFreighter()
    {
        Planet far = Importer(SystemSite + new Vector2(0, 300_000f));
        Planet near = Importer(SystemSite + new Vector2(0, -200_000f));
        Ship slow = Freighter(StationSite + new Vector2(600_000f, 0));
        Ship quick = Freighter(StationSite + new Vector2(5_000f, 0));
        Ship station = StationHolding(20f);
        Assert.IsTrue(quick.TryGetHaulEta(station, near, out int quickEta) && slow.TryGetHaulEta(station, near, out int slowEta) && quickEta < slowEta,
                      "setup: one freighter is quicker to the station and on to the colony");

        Turn(station);
        Assert.IsTrue(Hauls(quick, station, out Planet colony), "the quickest freighter goes");
        AssertEqual(near.Name, colony.Name, "to the closest importing colony");
        Assert.IsFalse(Hauls(slow, station), "and the other stays");
        AssertGreaterThan(far.FreeProdImportSlots, 0, "though the far colony imports too");
    }

    [TestMethod]
    public void AHaulTakesAnImportSlotOfItsColony()
    {
        Planet near = Importer(SystemSite + new Vector2(0, -200_000f), slots: 1);
        Planet far = Importer(SystemSite + new Vector2(0, 300_000f));
        Ship first = Freighter(StationSite + new Vector2(5_000f, 0));
        Ship second = Freighter(StationSite + new Vector2(6_000f, 0));
        Ship station = StationHolding(40f);

        Turn(station);
        bool firstHauls = Hauls(first, station, out Planet a);
        Assert.IsTrue(Hauls(second, station, out Planet b) && firstHauls, "setup: two are called");
        AssertEqual(0, near.FreeProdImportSlots, "the first haul takes the near colony's one import slot");
        AssertEqual(new[] { near.Name, far.Name }, new[] { a.Name, b.Name }.OrderBy(n => n == near.Name ? 0 : 1).ToArray(),
                    "so the second goes to the far one");
    }

    void TradeWith(Empire partner)
    {
        Player.SignTreatyWith(partner, TreatyType.Trade);
        Assert.IsTrue(Player.IsTradeTreaty(partner), "setup: a trade treaty, signed after the last trade turn");
        UState.P.AllowPlayerInterTrade = true;
    }

    [TestMethod]
    public void WithNoImportingColonyAHaulGoesToATradePartnersOneWithInterTradeAllowed()
    {
        CreateThirdMajorEmpire();
        Planet partners = Importer(SystemSite + new Vector2(0, 200_000f), ThirdMajor);
        Planet mine = Colony(SystemSite + new Vector2(0, -250_000f));
        TradeWith(ThirdMajor);
        Ship freighter = Freighter(StationSite + new Vector2(5_000f, 0));
        freighter.AllowInterEmpireTrade = true;
        Ship station = StationHolding(20f);

        Turn(station);
        Assert.IsTrue(Hauls(freighter, station, out Planet colony), "a freighter is called");
        AssertEqual(partners.Name, colony.Name, "for the partner's importing colony, not a colony of the player's with room");
        AssertEqual(1, partners.NumIncomingFreighters, "which counts it on its way");
        AssertGreaterThan(mine.ProdStoreRoom, 3f, "setup: the player's colony has room");
    }

    [TestMethod]
    public void APartnersColonyGetsNoHaulWithoutInterTradeOrFromAFreighterNotAllowedIt()
    {
        CreateThirdMajorEmpire();
        Planet partners = Importer(SystemSite + new Vector2(0, 200_000f), ThirdMajor);
        Planet mine = Colony(SystemSite + new Vector2(0, -250_000f));
        TradeWith(ThirdMajor);
        Ship freighter = Freighter(StationSite + new Vector2(5_000f, 0));
        Ship station = StationHolding(20f);

        Turn(station);
        Assert.IsTrue(Hauls(freighter, station, out Planet colony) && colony == mine, "a freighter not allowed inter-empire trade hauls to the player's colony");
        freighter.AI.ClearOrders();

        freighter.AllowInterEmpireTrade = true;
        UState.P.AllowPlayerInterTrade = false;
        Turn(station);
        Assert.IsTrue(Hauls(freighter, station, out colony) && colony == mine, "and so does one allowed it, with inter-empire trade off");
        AssertEqual(0, partners.NumIncomingFreighters, "the partner's colony gets none");
    }

    [TestMethod]
    public void WithNoImporterOrPartnerAHaulGoesToTheClosestColonyWithRoom()
    {
        Planet full = Colony(SystemSite + new Vector2(0, -150_000f));
        full.ProdHere = full.Storage.Max - 2f;
        Planet roomy = Colony(SystemSite + new Vector2(0, 250_000f));
        Planet farther = Colony(SystemSite + new Vector2(0, 350_000f));
        full.UpdateIncomingTradeGoods();
        Ship freighter = Freighter(StationSite + new Vector2(5_000f, 0));
        Ship station = StationHolding(20f);

        Turn(station);
        Assert.IsTrue(Hauls(freighter, station, out Planet colony), "a freighter is called");
        AssertEqual(roomy.Name, colony.Name, "for the closest colony with room for 3 or more, past a closer one with room for 2");
        AssertGreaterThan(farther.ProdStoreRoom, 3f, "setup: a farther colony has room too");
    }

    [TestMethod]
    public void AColonyWhoseTradeIsBlockedGetsNoHaulThoughItHasRoom()
    {
        Planet quarantined = Colony(SystemSite + new Vector2(0, -150_000f));
        quarantined.Quarantine = true;
        Planet roomy = Colony(SystemSite + new Vector2(0, 250_000f));
        Assert.IsTrue(quarantined.TradeBlocked && quarantined.ProdStoreRoom >= 3f, "setup: the closer colony has room but its trade is blocked");
        Ship freighter = Freighter(StationSite + new Vector2(5_000f, 0));
        Ship station = StationHolding(20f);

        Turn(station);
        Assert.IsTrue(Hauls(freighter, station, out Planet colony), "a freighter is called");
        AssertEqual(roomy.Name, colony.Name, "for the farther colony, not the quarantined one");
    }

    [TestMethod]
    public void TheRoomForAHaulCountsTheHoldsOfTheHaulsAlreadyGoingThere()
    {
        Planet roomy = Colony(SystemSite + new Vector2(0, 250_000f));
        roomy.ProdHere = roomy.Storage.Max - 22f;
        Planet farther = Colony(SystemSite + new Vector2(0, 350_000f));
        roomy.UpdateIncomingTradeGoods();
        Ship first = Freighter(StationSite + new Vector2(5_000f, 0));
        Ship second = Freighter(StationSite + new Vector2(6_000f, 0));
        Ship station = StationHolding(40f);

        Turn(station);
        bool firstHauls = Hauls(first, station, out Planet a);
        Assert.IsTrue(Hauls(second, station, out Planet b) && firstHauls, "setup: two are called");
        AssertEqual(new[] { roomy.Name, farther.Name }, new[] { a.Name, b.Name }.OrderBy(n => n == roomy.Name ? 0 : 1).ToArray(),
                    "the first hold of 20 leaves the closer colony room for 2, so the second goes on to the farther one");
    }

    [TestMethod]
    public void NoColonyWithRoomCallsNoFreighter()
    {
        Planet full = Colony(SystemSite + new Vector2(0, -150_000f));
        full.ProdHere = full.Storage.Max - 2f;
        full.UpdateIncomingTradeGoods();
        foreach (Planet p in Player.GetPlanets())
        {
            if (p == full)
                continue;
            p.ProdHere = p.Storage.Max;
            p.UpdateIncomingTradeGoods();
        }
        Ship freighter = Freighter(StationSite + new Vector2(5_000f, 0));
        Ship station = StationHolding(20f);

        Turn(station);
        Assert.IsFalse(Hauls(freighter, station), "with no colony to take it, the station keeps its production");
    }

    [TestMethod]
    public void AFreighterGoesOnlyWhereItsAreasAndRoutesLetItAsForResearchAndMiningStations()
    {
        Planet importer = Importer(SystemSite + new Vector2(0, 200_000f));
        Ship station = StationHolding(20f);
        Ship freighter = Freighter(StationSite + new Vector2(5_000f, 0));
        var aroundColony = new Rectangle((int)importer.Position.X - 10_000, (int)importer.Position.Y - 10_000, 20_000, 20_000);
        var aroundStation = new Rectangle((int)StationSite.X - 10_000, (int)StationSite.Y - 10_000, 20_000, 20_000);

        freighter.AreaOfOperation.Add(aroundStation);
        Turn(station);
        Assert.IsFalse(Hauls(freighter, station), "an area around the station alone leaves the colony out");

        freighter.AreaOfOperation.Clear();
        freighter.AreaOfOperation.Add(aroundColony);
        Turn(station);
        Assert.IsFalse(Hauls(freighter, station), "an area around the colony alone leaves the station out");

        freighter.AreaOfOperation.Add(aroundStation);
        Turn(station);
        Assert.IsTrue(Hauls(freighter, station), "an area around the station too lets it go");
        freighter.AI.ClearOrders();

        freighter.AreaOfOperation.Clear();
        Assert.IsTrue(freighter.AddTradeRoute(importer), "setup: a route to the colony");
        Turn(station);
        Assert.IsFalse(Hauls(freighter, station), "a route list alone never reaches a station");
    }

    [TestMethod]
    public void AFreighterHoldingOtherGoodsOrWithLittleRoomIsNotCalled()
    {
        Importer(SystemSite + new Vector2(0, 200_000f));
        Ship withFood = Freighter(StationSite + new Vector2(5_000f, 0));
        withFood.LoadFood(5f);
        Ship nearlyFull = Freighter(StationSite + new Vector2(6_000f, 0));
        nearlyFull.LoadProduction(18f);
        Ship halfFull = Freighter(StationSite + new Vector2(40_000f, 0));
        halfFull.LoadProduction(10f);
        Ship station = StationHolding(20f);

        Turn(station);
        Assert.IsFalse(Hauls(withFood, station), "a freighter holding food stays, as a hold carries one kind of good");
        Assert.IsFalse(Hauls(nearlyFull, station), "and so does one with room for 2");
        Assert.IsTrue(Hauls(halfFull, station), "a farther one holding production with room for 10 goes");
    }

    [TestMethod]
    public void WithAutoTradeOffOnlyFreightersSetToTradeProductionHaul()
    {
        Importer(SystemSite + new Vector2(0, 200_000f));
        Player.AutoFreighters = false;
        Ship freighter = Freighter(StationSite + new Vector2(5_000f, 0));
        freighter.TransportingProduction = false;
        Ship station = StationHolding(20f);

        Turn(station);
        Assert.IsFalse(Hauls(freighter, station), "a freighter not set to trade production stays");

        freighter.TransportingProduction = true;
        Turn(station);
        Assert.IsTrue(Hauls(freighter, station), "one set to it goes");
    }

    [TestMethod]
    public void NoFreighterIsCalledWhileAnArmedHostileIsInTheStationsSensorRange()
    {
        Importer(SystemSite + new Vector2(0, 200_000f));
        Ship freighter = Freighter(StationSite + new Vector2(5_000f, 0));
        Ship station = StationHolding(20f);
        Assert.IsTrue(Player.IsAtWarWith(Enemy), "setup: at war");
        Ship hostile = SpawnShip("Dreadnought mk1-a", Enemy, StationSite + new Vector2(0, 20_000f));
        RunObjectsSim(1.5f);
        Assert.IsTrue(hostile.Position.InRadius(station.Position, station.SensorRange), "setup: the warship is in the station's sensor range");

        Turn(station);
        Assert.IsFalse(Hauls(freighter, station), "the station calls no freighter while it is there");

        hostile.QueueTotalRemoval();
        RunObjectsSim(1.5f);
        Turn(station);
        Assert.IsTrue(Hauls(freighter, station), "and calls one once it is gone");
    }

    [TestMethod]
    public void AnArmedHostileBeyondTheStationsSensorsOrAnUnarmedStationInRangeHoldsOffNoHaul()
    {
        Importer(SystemSite + new Vector2(0, 200_000f), slots: 10);
        Ship first = Freighter(StationSite + new Vector2(5_000f, 0));
        Ship second = Freighter(StationSite + new Vector2(6_000f, 0));
        Ship station = StationHolding(20f);
        Ship far = SpawnShip("Dreadnought mk1-a", Enemy, StationSite + new Vector2(0, station.SensorRange + 10_000f));
        Ship unarmed = SpawnShip("Terran Basic Salvage Station", Enemy, StationSite + new Vector2(0, -10_000f));
        RunObjectsSim(1.5f);
        Assert.IsTrue(far.System == station.System && unarmed.Position.InRadius(station.Position, station.SensorRange),
                      "setup: a warship in the system beyond the station's sensors, and an unarmed station within them");
        Assert.IsTrue(unarmed.TotalDps <= 0f && unarmed.BaseStrength > 0f && far.TotalDps > 0f,
                      "setup: the station has strength but no weapons, the warship has weapons");
        Assert.IsTrue(station.AI.PotentialTargets.Contains(unarmed), "setup: the station sees the unarmed one as a hostile");

        Turn(station);
        Assert.IsTrue(Hauls(first, station) || Hauls(second, station), "the station calls a freighter");
    }

    [TestMethod]
    public void AHaulOutsideTheSystemWaitsWhileHostilesAreThereAndGivesUpAfterAWhile()
    {
        Planet importer = Importer(SystemSite + new Vector2(0, 200_000f));
        Ship station = StationHolding(20f);
        Ship freighter = Freighter(StationSite + new Vector2(200_000f, 0));
        Ship hostile = SpawnShip("Dreadnought mk1-a", Enemy, StationSite + new Vector2(0, 20_000f));
        RunObjectsSim(1.5f);
        Assert.IsTrue(freighter.System == null && SalvageOps.HaulsBlocked(station), "setup: the freighter is outside the system, and a hostile is at the station");
        freighter.AI.SetupFreighterPlan(station, importer, Goods.Production);

        float distance = freighter.Position.Distance(station.Position);
        RunObjectsSim(5f);
        Assert.IsTrue(Hauls(freighter, station), "it keeps its haul");
        AssertLessThan(distance - freighter.Position.Distance(station.Position), 2_000f, "and waits rather than flying on");
        freighter.AI.OrderQueue.TryPeekFirst(out ShipAI.ShipGoal g);
        AssertLessThan(g.Trade.BlockadeTimer, 116f, "its blockade timer runs");

        g.Trade.BlockadeTimer = 0.5f;
        RunObjectsSim(1f);
        Assert.IsFalse(Hauls(freighter, station), "once the timer runs out it gives up the haul");

        Ship inside = Freighter(StationSite + new Vector2(200f, 0));
        RunObjectsSim(0.2f);
        Assert.IsTrue(inside.System == station.System, "setup: a freighter inside the system");
        inside.AI.SetupFreighterPlan(station, importer, Goods.Production);
        RunObjectsSim(0.1f);
        Assert.IsTrue(inside.IsLanding && Hauls(inside, station), "one inside the system goes on and lands");
        hostile.QueueTotalRemoval();
    }

    [TestMethod]
    public void AnAIEmpiresStationCallsNoFreighterYet()
    {
        Planet importer = Importer(SystemSite + new Vector2(0, 200_000f), Enemy);
        Ship freighter = Freighter(StationSite + new Vector2(5_000f, 0), Enemy);
        Ship station = StationHolding(20f, Enemy);
        AssertGreaterThan(importer.FreeProdImportSlots, 0, "setup: the AI's colony imports");

        Turn(station);
        Assert.IsFalse(Hauls(freighter, station), "an AI empire's station calls none of its freighters");
    }

    // its haul's freighter as it lands on the station
    Ship LandingToHaul(Ship station, Planet importer)
    {
        Ship freighter = Freighter(StationSite + new Vector2(5_000f, 0));
        freighter.AI.SetupFreighterPlan(station, importer, Goods.Production);
        Assert.IsTrue(Hauls(freighter, station), "setup: the freighter hauls");
        return freighter;
    }

    [TestMethod]
    public void AHaulFillsItsHoldWhateverItsColonysRoom()
    {
        Planet importer = Importer(SystemSite + new Vector2(0, 200_000f));
        Ship station = StationHolding(50f);

        importer.ProdHere = importer.Storage.Max - 2f;
        Ship first = LandingToHaul(station, importer);
        first.AI.TradeAfterLanding(null, station);
        AssertEqual(0.01f, 20f, first.GetProduction(), "it takes its hold of 20, though its colony has room for 2");
        AssertEqual(0.01f, 30f, station.GetProduction(), "from the station");
        Assert.IsTrue(Delivers(first, importer), "and delivers it");
        AssertEqual(0.01f, 20f, importer.IncomingProd, "which the colony counts on its way");

        station.UnloadProduction(28f);
        Ship second = LandingToHaul(station, importer);
        second.AI.TradeAfterLanding(null, station);
        AssertEqual(0.01f, 2f, second.GetProduction(), "it takes the last 2 a station holds");
        Assert.IsTrue(Delivers(second, importer), "and delivers them");
    }

    [TestMethod]
    public void OnlyWhatAColonyTakesOfADeliveryIsTaxed()
    {
        Planet importer = Importer(SystemSite + new Vector2(0, 200_000f));
        Ship station = StationHolding(40f);
        Player.data.Traits.TaxGoods = false;
        Player.data.Traits.Mercantile = 1f; // a credit for each unit delivered

        importer.ProdHere = importer.Storage.Max - 2f;
        Ship first = LandingToHaul(station, importer);
        first.AI.TradeAfterLanding(null, station);
        float before = Player.TradeMoneyAddedThisTurn;
        first.AI.TradeAfterLanding(importer, null);
        AssertEqual(0.01f, 18f, first.GetProduction(), "setup: the colony takes 2 of 20, the rest stays aboard");
        AssertEqual(0.01f, 2f, Player.TradeMoneyAddedThisTurn - before, "only the 2 the colony takes are taxed");

        importer.ProdHere = 0f;
        Ship second = LandingToHaul(station, importer);
        second.AI.TradeAfterLanding(null, station);
        before = Player.TradeMoneyAddedThisTurn;
        second.AI.TradeAfterLanding(importer, null);
        AssertEqual(0.01f, 20f, Player.TradeMoneyAddedThisTurn - before, "a whole load delivered is taxed whole");
    }

    [TestMethod]
    public void AHaulTouchingDownOnAStationTheOthersEmptiedIsCalledOff()
    {
        Planet importer = Importer(SystemSite + new Vector2(0, 200_000f));
        Ship station = StationHolding(20f);
        Ship first = LandingToHaul(station, importer);
        Ship second = LandingToHaul(station, importer);
        first.AI.TradeAfterLanding(null, station);
        AssertEqual(0.01f, 0f, station.GetProduction(), "setup: the first takes it all");

        second.AI.TradeAfterLanding(null, station);
        AssertEqual(0f, second.GetProduction(), "the second takes nothing");
        Assert.IsFalse(Hauls(second, station) || Delivers(second, importer), "and its haul is called off");
    }

    [TestMethod]
    public void AHaulIsCalledOffWithItsStationLostButALoadAboardIsStillDelivered()
    {
        Planet importer = Importer(SystemSite + new Vector2(0, 200_000f));
        Ship station = StationHolding(40f);
        Ship loaded = LandingToHaul(station, importer);
        loaded.AI.TradeAfterLanding(null, station);
        Ship coming = LandingToHaul(station, importer);

        station.QueueTotalRemoval();
        RunObjectsSim(TestSimStep);
        Assert.IsFalse(Hauls(coming, station), "a freighter on its way gives up its haul");
        Assert.IsTrue(Delivers(loaded, importer), "one with the load aboard still delivers it");
    }

    [TestMethod]
    public void ADyingStationCallsNoFreighterAndGivesNoLoad()
    {
        Planet importer = Importer(SystemSite + new Vector2(0, 200_000f));
        Ship station = StationHolding(40f);
        Ship freighter = LandingToHaul(station, importer);
        Ship idle = Freighter(StationSite + new Vector2(6_000f, 0));
        station.Dying = true;

        freighter.AI.TradeAfterLanding(null, station);
        AssertEqual(0f, freighter.GetProduction(), "a freighter touching down on a dying station takes nothing");
        Turn(station);
        Assert.IsFalse(Hauls(idle, station), "and the station calls no freighter");
    }

    [TestMethod]
    public void AHaulInItsStationsOwnSystemOrdersNoProjectorBridge()
    {
        Planet importer = Importer(SystemSite + new Vector2(0, 200_000f));
        Ship station = StationHolding(20f);
        Player.AutoBuildSpaceRoads = true;
        Ship freighter = Freighter(StationSite + new Vector2(5_000f, 0));
        RunObjectsSim(0.2f);
        freighter.AI.SetupFreighterPlan(station, importer, Goods.Production);
        Assert.IsTrue(freighter.System != null && freighter.System == station.System && !station.System.HasPlanetsOwnedBy(Player),
                      "setup: the freighter is in the station's system, where the player has no colony");

        Player.AI.SpaceRoadsManager.SetupProjectorBridgeIfNeeded(freighter, ProjectorBridgeEndCondition.NoHostiles);
        Assert.IsFalse(Player.AI.HasGoal(g => g is ProjectorBridge), "no projector bridge is ordered from the system to itself");
    }

    [TestMethod]
    public void AHaulReachingAStationTheOthersEmptiedIsCalledOff()
    {
        Planet importer = Importer(SystemSite + new Vector2(0, 200_000f));
        Ship station = StationHolding(0.5f);
        Ship freighter = Freighter(StationSite + new Vector2(200f, 0));
        AssertLessThan(200f, LandShip.ShipyardLandingRange(freighter), "setup: the freighter is in landing range of the station");
        freighter.AI.SetupFreighterPlan(station, importer, Goods.Production);

        RunObjectsSim(0.1f);
        Assert.IsFalse(Hauls(freighter, station) || freighter.IsLanding, "with less than 1 left there, it does not land");

        station.LoadProduction(10f);
        Ship next = Freighter(freighter.Position + new Vector2(0, 10f));
        next.AI.SetupFreighterPlan(station, importer, Goods.Production);
        RunObjectsSim(0.1f);
        Assert.IsTrue(next.IsLanding && Hauls(next, station), "with 10 there, it lands");
    }

    [TestMethod]
    public void AScrapOrderCallsOffAHaulLandingOnItsStation()
    {
        Planet importer = Importer(SystemSite + new Vector2(0, 200_000f));
        Ship station = StationHolding(20f);
        Ship freighter = LandingToHaul(station, importer);
        freighter.InitLandingOnStation(station);
        Assert.IsTrue(freighter.IsLanding, "setup: it is landing");

        freighter.AI.CancelPickup(AIState.Scrap);
        freighter.AI.TradeAfterLanding(null, station);
        AssertEqual(0f, freighter.GetProduction(), "a pickup called off takes nothing at touchdown");
        AssertEqual(0.01f, 20f, station.GetProduction(), "the station keeps it");
    }

    [TestMethod]
    public void AHauledLoadFliesFromTheStationToItsColonyAndLandsThere()
    {
        Local.SetOwner(Player);
        Local.Storage.Max = 200f;
        Local.PS = Planet.GoodState.IMPORT;
        Local.ManualProdImportSlots = 2;
        Local.ProdHere = 0f;
        Local.UpdateIncomingTradeGoods();
        Ship station = BuiltStation();
        Ship freighter = SpawnShip("Owlwok Freighter S", Player, StationSite + new Vector2(8_000f, 0));
        freighter.TransportingProduction = true;
        RunObjectsSim(TestSimStep);
        float hold = freighter.CargoSpaceMax;
        float held = System.Math.Max(hold + 1f, 20f);
        AssertGreaterThan(hold, 3f, "setup: the freighter has a hold");
        AssertGreaterThan(station.CargoSpaceMax, held, "setup: the station holds more than a freighter load");
        AssertGreaterThan(Local.Storage.Max, hold, "setup: the colony has room for it");
        station.LoadProduction(held);
        Turn(station);
        Assert.IsTrue(Hauls(freighter, station), "setup: the freighter is called");

        RunSimWhile((simTimeout: 300, fatal: true), () => Hauls(freighter, station));
        Assert.IsTrue(Delivers(freighter, Local), "it loads at the station and turns to its colony");
        AssertEqual(0.01f, hold, freighter.GetProduction(), "with a full hold");
        AssertEqual(0.01f, held - hold, station.GetProduction(), "taken from the station");

        RunSimWhile((simTimeout: 300, fatal: true), () => Delivers(freighter, Local));
        AssertEqual(0.01f, hold, Local.ProdHere, "and the colony has it once the freighter has landed");
    }

    [TestMethod]
    public void TheShipListSaysWhereAHaulPicksUpAndDelivers()
    {
        Planet importer = Importer(SystemSite + new Vector2(0, 200_000f));
        Ship station = StationHolding(20f);
        Ship freighter = LandingToHaul(station, importer);
        string picking = Localizer.Token(GameText.PickingUp);
        Assert.IsFalse(string.IsNullOrEmpty(picking), "setup: the text exists");
        StringAssert.StartsWith(ShipListScreenItem.GetStatusText(freighter), $"{picking} Production from {station.Name} to {importer.Name}");

        freighter.AI.TradeAfterLanding(null, station);
        StringAssert.StartsWith(ShipListScreenItem.GetStatusText(freighter),
                                $"{Localizer.Token(GameText.Delivering)} Production from {station.Name} to {importer.Name}");
    }

    [TestMethod]
    public void AHaulSurvivesSaveAndLoad()
    {
        Planet importer = Importer(SystemSite + new Vector2(0, 200_000f));
        Ship station = StationHolding(40f);
        Ship coming = LandingToHaul(station, importer);
        Ship loaded = LandingToHaul(station, importer);
        loaded.AI.TradeAfterLanding(null, station);

        AddDummyPlanetToEmpire(new Vector2(900_000f, 200_000f), Enemy); // the loader needs a colony of each major empire
        UState.StarDate = 1001f; // the loader needs a stardate past 1000
        SavedGame save = Universe.Save("UnitTest.SalvageHaul", throwOnError: true);
        UniverseScreen game = LoadGame.Load(save.SaveFile, noErrorDialogs: true, startSimThread: false);
        Assert.IsNotNull(game, "setup: the save loads");

        Ship loadedStation = game.UState.Objects.FindShip(station.Id);
        Planet loadedImporter = game.UState.Player.GetPlanets().FirstOrDefault(p => p.Name == importer.Name);
        Assert.IsTrue(Hauls(game.UState.Objects.FindShip(coming.Id), loadedStation, out Planet colony) && colony == loadedImporter,
                      "a freighter on its way to the station still hauls for the colony");
        Assert.IsTrue(Delivers(game.UState.Objects.FindShip(loaded.Id), loadedImporter), "one with its load aboard still delivers it");
        AssertEqual(1, SalvageOps.HaulsComing(loadedStation, out _), "the station counts the one on its way");
        AssertEqual(2, loadedImporter.NumIncomingFreighters, "and the colony both");
    }

    void TrackNotifications()
    {
        Universe.NotificationManager = Notes = new NotificationManager(Universe.ScreenManager, Universe);
    }

    Notification[] Told() => Notes.GetNotificationsAtomic();

    static bool Scrapped(Ship station) => !station.Active && OrderOf(station) == null;

    [TestMethod]
    public void WithNothingLeftToSalvageTheStationCallsForWhatItHasLeftBelowAnAverageLoad()
    {
        Importer(SystemSite + new Vector2(0, 200_000f));
        Ship freighter = Freighter(StationSite + new Vector2(5_000f, 0));
        Ship station = StationHolding(5f);

        Turn(station);
        Assert.IsFalse(Hauls(freighter, station), "5 with a derelict left to salvage calls none");

        UState.Derelicts.Strip(Work, null, Work.ProductionLeft);
        Assert.IsFalse(Work.CanBeSalvaged, "setup: the derelict is stripped");
        Turn(station);
        Assert.IsTrue(Hauls(freighter, station), "with nothing left to salvage the 5 call a freighter");
        Assert.IsTrue(station.Active, "and the station stays while it holds them");
    }

    [TestMethod]
    public void ADerelictAnotherEmpireWorksFromItsStationThereLeavesNothingToSalvage()
    {
        Ship station = StationHolding(0f);
        Assert.IsTrue(Work.TryClaim(Player), "setup: the player claims the derelict, with no order of its own");
        Turn(station);
        Assert.IsTrue(station.Active, "a derelict the player claimed keeps the station");

        Work.ReleaseClaim();
        Assert.IsTrue(Work.TryClaim(Enemy), "setup: another empire claims it, with no station there, as one whose station was just taken");
        Turn(station);
        Assert.IsTrue(station.Active, "a claim ending with its empire's station keeps the station too");

        BuiltStation(Enemy, atWork: false);
        Turn(station);
        Assert.IsTrue(Scrapped(station), "one another empire works from its station there leaves nothing to salvage, and the empty station is scrapped");
    }

    [TestMethod]
    public void AKnownDerelictNotYetOrderedKeepsAnEmptyStation()
    {
        Ship station = StationHolding(0f);
        Turn(station);
        Turn(station);
        Assert.IsTrue(station.Active && OrderOf(station) != null, "a derelict there not yet ordered keeps the station at work");
    }

    [TestMethod]
    public void AnOrderInTheSystemKeepsTheStationButOneElsewhereDoesNot()
    {
        Vector2 elsewhere = new(600_000f, 1_200_000f);
        AddDummyPlanet(elsewhere, 1f, 1f, 1f, elsewhere + new Vector2(0, 60_000f), explored: true);
        Derelict away = Wreck(elsewhere + new Vector2(-30_000f, 0));
        Assert.IsTrue(away.System != null && away.System != Local.System, "setup: a derelict in another system");
        Ship station = StationHolding(0f);
        var here = new SalvageDerelict(Player, Work);
        Player.AI.AddGoal(here);
        Player.AI.AddGoal(new SalvageDerelict(Player, away));
        UState.Derelicts.Strip(Work, null, Work.ProductionLeft);

        Turn(station);
        Assert.IsTrue(station.Active, "an order of the player's in the system keeps the station, though its derelict is stripped");

        Player.AI.RemoveGoal(here);
        Turn(station);
        Assert.IsTrue(Scrapped(station), "with it gone, an order and a derelict in another system keep nothing here");
    }

    [TestMethod]
    public void WithNothingLeftToSalvageAnEmptyStationIsScrappedWithNoRefundAndThePlayerIsTold()
    {
        Ship station = StationHolding(0.5f, atWork: false);
        TrackNotifications();
        float money = Player.Money;

        Turn(station);
        Assert.IsTrue(Scrapped(station), "holding less than 1 with nothing left to salvage, the station is scrapped");
        AssertEqual(money, Player.Money, "with no refund");
        Assert.IsTrue(station.ScuttleTimer > 0f, "as a station scrapped is, its scuttle timer set");
        Notification[] told = Told();
        AssertEqual(1, told.Length, "the player is told once");
        AssertEqual(string.Format(Localizer.Token(GameText.SalvageStationScrappedNotify), Local.System.Name), told[0].Message,
                    "that the station in the system was scrapped");
        Assert.IsTrue(told[0].ReferencedItem1 == Local.System && !told[0].Pause, "a click shows the system, and the game goes on");
        StringAssert.StartsWith(Localizer.Token(GameText.SalvageStationScrappedNotify), "{0}: with nothing left to salvage");
    }

    [TestMethod]
    public void SalvageAlertsOffTellNothingOfAScrappedStation()
    {
        Ship station = StationHolding(0f, atWork: false);
        TrackNotifications();
        UState.P.DisableSalvageWarning = true;

        Turn(station);
        Assert.IsTrue(Scrapped(station), "the station is scrapped");
        AssertEqual(0, Told().Length, "and nothing is told");
    }

    [TestMethod]
    public void AStationWithNothingLeftToSalvageIsScrappedOnceItsProductionIsHauledAway()
    {
        Planet importer = Importer(SystemSite + new Vector2(0, 200_000f));
        Ship station = StationHolding(5f, atWork: false);
        Ship freighter = LandingToHaul(station, importer);

        Turn(station);
        Assert.IsTrue(station.Active, "holding 5, it is not scrapped");

        freighter.AI.TradeAfterLanding(null, station);
        Turn(station);
        Assert.IsTrue(Scrapped(station), "once the freighter has taken them it is");
        Assert.IsTrue(Delivers(freighter, importer), "and the freighter still delivers them");
    }

    [TestMethod]
    public void AnAIEmpiresOrADyingStationIsNotScrapped()
    {
        Ship theirs = StationHolding(0f, Enemy, atWork: false);
        Turn(theirs);
        Assert.IsTrue(theirs.Active && OrderOf(theirs) != null, "an AI empire's station with nothing left to salvage stays");

        Ship dying = BuiltStation(atWork: false);
        TrackNotifications();
        dying.Dying = true;
        Turn(dying);
        Assert.IsTrue(dying.Active && OrderOf(dying) != null, "a dying station is not scrapped");
        AssertEqual(0, Told().Length, "and nothing is told");
    }

    [TestMethod]
    public void AStationNotYetPlacedInItsSystemOrBeingScuttledIsNotScrapped()
    {
        Ship station = BuiltStation(atWork: false, placed: false);
        TrackNotifications();
        Assert.IsNull(station.System, "setup: not yet placed in its system");
        Turn(station);
        Assert.IsTrue(station.Active, "a station yet to show the player the derelicts in its system is not scrapped");

        Place(station);
        station.ScuttleTimer = 10f;
        Turn(station);
        Assert.IsTrue(station.Active && OrderOf(station) != null, "nor is one the player is scuttling");
        AssertEqual(0, Told().Length, "and nothing is told");
    }

    [TestMethod]
    public void TheStationsStatusSaysWhetherFreightersAreOnTheirWayOrAwaitedOrItSalvages()
    {
        Planet importer = Importer(SystemSite + new Vector2(0, 200_000f), slots: 10);
        Ship station = StationHolding(0.5f);
        AssertEqual("Salvaging", ShipInfoUIElement.SalvageStationStatus(station), "holding less than 1, it salvages");

        station.LoadProduction(0.5f);
        AssertEqual("Waiting for freighters", ShipInfoUIElement.SalvageStationStatus(station), "holding 1 with none coming, it waits");

        LandingToHaul(station, importer);
        LandingToHaul(station, importer);
        AssertEqual("Freighters on their way: 2", ShipInfoUIElement.SalvageStationStatus(station), "with two coming, it counts them");

        Ship theirs = StationHolding(40f, Enemy, atWork: false);
        AssertEqual(ShipListScreenItem.GetStatusText(theirs), ShipInfoUIElement.SalvageStationStatus(theirs),
                    "another empire's shows its orders");
    }
}
