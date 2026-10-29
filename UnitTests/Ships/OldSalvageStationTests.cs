using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Ship_Game;
using Ship_Game.AI;
using Ship_Game.Commands.Goals;
using Ship_Game.GameScreens.LoadGame;
using Ship_Game.Ships;
using Vector2 = SDGraphics.Vector2;

namespace UnitTests.Ships;

/// <summary>
/// Salvage stations come only from saves of the release that had them. No empire builds one, though it can build its
/// hull and modules. As such a save loads, they leave the designs every empire can build; a freighter's haul from a
/// station is called off, one that loaded keeping its cargo for its empire's trade and one behind another order too,
/// while a colony's trade goes on; a station still on order is cancelled, its queue
/// item and a constructor on its way to build it scrapped, while other orbitals on order are built; and an AI empire
/// scraps the stations it holds, as any orbital, while the player and pirates keep theirs, idle. A station whose
/// design is gone loads from the save's own copy of it. The player can refit one to another station on its hull.
/// </summary>
[TestClass]
public class OldSalvageStationTests : StarDriveTest
{
    const string OldStation = "TEST_Old Salvage Station";
    const string PatrolStation = "Terran Small Patrol Station 1";
    const string ResearchStation = "Terran Small Research Station Mk1";
    const string Freighter = "Owlwok Freighter S";
    const string Constructor = "Terran Constructor";
    readonly Planet Exporter;
    readonly Planet Importer;

    public OldSalvageStationTests()
    {
        LoadStarterShips(OldStation, PatrolStation, ResearchStation, Freighter, Constructor);
        CreateUniverseAndPlayerEmpire();
        UState.Objects.EnableParallelUpdate = false;
        Exporter = AddHomeWorldToEmpire(new Vector2(200_000f), Player, new Vector2(205_000f), explored: true);
        Importer = AddDummyPlanet(new Vector2(300_000f), 1f, 1f, 4f, new Vector2(305_000f), explored: true);
        Importer.SetOwner(Player);
        Player.UpdateRallyPoints();
        AddDummyPlanetToEmpire(new Vector2(900_000f, 200_000f), Enemy); // the loader needs a colony of each major empire
    }

    static IShipDesign Design(string name)
    {
        Assert.IsTrue(ResourceManager.Ships.GetDesign(name, out IShipDesign design), $"setup: {name} is loaded");
        return design;
    }

    UniverseScreen SavedAndLoaded()
    {
        UState.StarDate = 1001f; // the loader needs a stardate past 1000
        SavedGame save = Universe.Save("UnitTest.OldSalvageStations", throwOnError: true);
        UniverseScreen game = LoadGame.Load(save.SaveFile, noErrorDialogs: true, startSimThread: false);
        Assert.IsNotNull(game, "setup: the save loads");
        game.InvokePendingSimThreadActions();
        return game;
    }

    [TestMethod]
    public void NoEmpireCanBuildASalvageStationThoughItCanBuildItsHullAndModules()
    {
        IShipDesign station = Design(OldStation);
        Assert.IsTrue(station.IsSalvageStation && station.IsValidDesign, "setup: a salvage station");
        UnlockAllTechsForShip(Player, OldStation);
        UnlockAllTechsForShip(Player, PatrolStation);
        Assert.IsTrue(Player.WeCanBuildThis(station), "setup: the player has its hull and modules");

        Player.UpdateShipsWeCanBuild();
        Assert.IsTrue(Player.CanBuildShip(PatrolStation), "setup: another station on its hull is buildable");
        Assert.IsFalse(Player.CanBuildShip(station), "but the salvage station is not");
        Assert.IsFalse(Player.CanBuildStation(station), "so the deep space build list leaves it out");
        Assert.IsFalse(station.CanBeAddedToBuildableShips(Enemy), "and no AI empire builds one either");
    }

    Ship SpawnFreighter(Vector2 offset)
    {
        Ship freighter = SpawnShip(Freighter, Player, Exporter.Position + offset);
        freighter.TransportingFood = true;
        freighter.TransportingProduction = true;
        return freighter;
    }

    // as a haul from a salvage station: no colony it loads at, and on its way to load, the station's own pickup plan
    static void AsStationHaul(Ship freighter, bool loading)
    {
        Assert.IsTrue(freighter.AI.OrderQueue.TryPeekFirst(out ShipAI.ShipGoal g) && g.Trade != null, "setup: a trade");
        g.Trade.ExportFrom.RemoveFromOutgoingFreighterList(freighter, g.Trade.Goods);
        typeof(ShipAI.TradePlan).GetField(nameof(ShipAI.TradePlan.ExportFrom)).SetValue(g.Trade, null);
        if (loading)
            typeof(ShipAI.ShipGoal).GetField(nameof(ShipAI.ShipGoal.Plan)).SetValue(g, ShipAI.Plan.PickupGoodsFromStation);
    }

    [TestMethod]
    public void OnLoadAHaulFromASalvageStationIsCalledOffAFreighterThatLoadedKeepingItsCargo()
    {
        Ship coming = SpawnFreighter(new Vector2(5_000f, 0));
        coming.AI.SetTradePlan(ShipAI.Plan.PickupGoods, Exporter, Importer, Goods.Production);
        AsStationHaul(coming, loading: true);
        Ship loaded = SpawnFreighter(new Vector2(6_000f, 0));
        loaded.LoadProduction(10f);
        loaded.AI.SetTradePlan(ShipAI.Plan.DropOffGoods, Exporter, Importer, Goods.Production);
        AsStationHaul(loaded, loading: false);
        Ship trader = SpawnFreighter(new Vector2(7_000f, 0));
        trader.LoadFood(5f);
        trader.AI.SetTradePlan(ShipAI.Plan.DropOffGoods, Exporter, Importer, Goods.Food);

        UniverseScreen game = SavedAndLoaded();
        Ship onItsWay = game.UState.Objects.FindShip(coming.Id);
        Ship withLoad = game.UState.Objects.FindShip(loaded.Id);
        Assert.IsFalse(onItsWay.AI.HasTradePlan, "a freighter on its way to load at the station gives up its haul");
        Assert.IsFalse(withLoad.AI.HasTradePlan, "and so does one with the station's production aboard");
        AssertEqual(10f, withLoad.GetProduction(), "which keeps it for its empire's trade to deliver");
        Assert.IsTrue(game.UState.Objects.FindShip(trader.Id).AI.HasTradePlan, "while a colony's trade goes on");
        Planet importer = game.UState.Player.GetPlanets().First(p => p.Name == Importer.Name);
        AssertEqual(1, importer.NumIncomingFreighters, "and the colony awaits that one only");
    }

    [TestMethod]
    public void OnLoadAHaulFromASalvageStationBehindAnotherOrderIsCalledOffToo()
    {
        Ship freighter = SpawnFreighter(new Vector2(5_000f, 0));
        freighter.LoadProduction(10f);
        freighter.AI.SetTradePlan(ShipAI.Plan.DropOffGoods, Exporter, Importer, Goods.Production);
        AsStationHaul(freighter, loading: false);
        freighter.AI.OrderResupply(Exporter, clearOrders: false);
        Assert.IsTrue(freighter.AI.OrderQueue.TryPeekFirst(out ShipAI.ShipGoal first) && first.Trade == null
                      && freighter.AI.OrderQueue.Any(g => g.Trade != null), "setup: the haul waits behind its resupply");

        Ship loaded = SavedAndLoaded().UState.Objects.FindShip(freighter.Id);
        Assert.IsFalse(loaded.AI.OrderQueue.Any(g => g.Trade != null), "a haul behind another order is called off too");
        AssertEqual(10f, loaded.GetProduction(), "its cargo kept");
    }

    [TestMethod]
    public void OnLoadNoEmpireCanBuildASalvageStationItCouldBuildInTheSave()
    {
        IShipDesign station = Design(OldStation);
        Assert.IsTrue(Player.AddBuildableShip(station) && Enemy.AddBuildableShip(station), "setup: both empires can build it, as in a save from when they could");
        Assert.IsTrue(Player.CanBuildStation(station), "setup: among the player's deep space builds");
        Player.AddBuildableShip(Design(PatrolStation));

        UniverseScreen game = SavedAndLoaded();
        Empire player = game.UState.Player;
        Assert.IsTrue(player.CanBuildShip(PatrolStation), "setup: the player still builds what it built before");
        Assert.IsFalse(player.CanBuildShip(OldStation), "but not the salvage station");
        Assert.IsFalse(player.CanBuildStation(Design(OldStation)), "nor from the deep space build list");
        Assert.IsFalse(game.UState.GetEmpireByName(Enemy.Name).CanBuildShip(OldStation), "and no AI empire builds one either");
    }

    [TestMethod]
    public void OnLoadASalvageStationStillOnOrderIsCancelledAndAConstructorOnItsWayScrapped()
    {
        Vector2 site = new(630_000f, 600_000f);
        SolarSystem system = AddDummyPlanet(new Vector2(600_000f), 1f, 1f, 1f, new Vector2(600_000f, 660_000f), explored: true).System;
        var queued = new BuildOrbital(Exporter, system, OldStation, Player, site);
        Player.AI.AddGoal(queued);
        var built = new BuildOrbital(Exporter, system, OldStation, Player, site + new Vector2(0, 10_000f));
        Player.AI.AddGoal(built);
        Ship constructor = SpawnShip(Constructor, Player, Exporter.Position + new Vector2(5_000f, 0));
        built.ReportShipComplete(constructor); // as the queue does once the constructor is built
        var patrol = new BuildOrbital(Exporter, system, PatrolStation, Player, site + new Vector2(0, -10_000f));
        Player.AI.AddGoal(patrol);

        UniverseScreen game = SavedAndLoaded();
        Empire player = game.UState.Player;
        Planet colony = player.GetPlanets().First(p => p.Name == Exporter.Name);
        Assert.IsFalse(player.AI.HasGoal(g => g is BuildOrbital && g.ToBuild?.Name == OldStation), "a salvage station on order is never built");
        QueueItem[] items = colony.ConstructionQueue.Where(q => q.Goal is BuildOrbital b && b.ToBuild?.Name == OldStation).ToArray();
        Assert.IsTrue(items.Length > 0 && items.All(q => q.IsCancelled), "its queue item leaves the queue at its next update");
        Ship onItsWay = game.UState.Objects.FindShip(constructor.Id);
        Assert.IsTrue(player.AI.HasGoal(g => g is ScrapShip && g.OldShip == onItsWay), "and a constructor on its way to build one is scrapped");
        Assert.IsTrue(player.AI.HasGoal(g => g is BuildOrbital && g.ToBuild?.Name == PatrolStation), "while another orbital on order is built");
        Assert.IsFalse(colony.ConstructionQueue.First(q => q.Goal is BuildOrbital b && b.ToBuild?.Name == PatrolStation).IsCancelled, "and stays queued");
    }

    [TestMethod]
    public void OnLoadAnAIEmpireScrapsItsSalvageStationsWhileThePlayerAndPiratesKeepTheirs()
    {
        CreateAMinorFaction("Corsairs");
        Empire pirates = Faction;
        Assert.IsTrue(pirates.WeArePirates, "setup: pirates");
        Ship ours = SpawnShip(OldStation, Player, new Vector2(600_000f));
        Ship theirs = SpawnShip(OldStation, Enemy, new Vector2(640_000f, 600_000f));
        Ship pirate = SpawnShip(OldStation, pirates, new Vector2(680_000f, 600_000f));

        UniverseScreen game = SavedAndLoaded();
        Empire ai = game.UState.GetEmpireByName(Enemy.Name);
        Ship aiStation = game.UState.Objects.FindShip(theirs.Id);
        Assert.IsTrue(ai.AI.HasGoal(g => g is DismantleOrbital && g.OldShip == aiStation), "an AI empire scraps the salvage station it holds, as any orbital");
        Ship playerStation = game.UState.Objects.FindShip(ours.Id);
        Assert.IsFalse(game.UState.Player.AI.HasGoal(g => g is DismantleOrbital && g.OldShip == playerStation), "the player keeps theirs");
        Ship pirateStation = game.UState.Objects.FindShip(pirate.Id);
        Assert.IsFalse(game.UState.GetEmpireByName(pirates.Name).AI.HasGoal(g => g is DismantleOrbital && g.OldShip == pirateStation), "and so do pirates");
    }

    [TestMethod]
    public void AStationWhoseDesignIsGoneLoadsFromTheSavesOwnCopyOfIt()
    {
        Ship station = SpawnShip(OldStation, Player, new Vector2(600_000f));
        station.LoadProduction(30f);
        UState.StarDate = 1001f; // the loader needs a stardate past 1000
        SavedGame save = Universe.Save("UnitTest.OldSalvageStationDesign", throwOnError: true);
        ResourceManager.Ships.Delete(OldStation);
        try
        {
            Assert.IsFalse(ResourceManager.Ships.GetDesign(OldStation, out _), "setup: no content has the design");
            UniverseScreen game = LoadGame.Load(save.SaveFile, noErrorDialogs: true, startSimThread: false);
            Assert.IsNotNull(game, "setup: the save loads");
            Ship loaded = game.UState.Objects.FindShip(station.Id);
            Assert.IsNotNull(loaded, "the station loads");
            Assert.IsTrue(loaded.IsSalvageStation && loaded.ShipData.IsFromSave, "on the save's copy of its design");
            AssertEqual(30f, loaded.GetProduction(), "with what it holds");
            Assert.IsFalse(game.UState.Player.AI.HasGoal(g => g.TargetShip == loaded || g.OldShip == loaded), "and no order works it");
        }
        finally
        {
            ResourceManager.Ships.Delete(OldStation);
            LoadStarterShips(OldStation);
        }
    }

    [TestMethod]
    public void ThePlayerCanRefitASalvageStationToAnotherStationOnItsHull()
    {
        Ship station = SpawnShip(OldStation, Player, new Vector2(600_000f));
        IShipDesign patrol = Design(PatrolStation);
        IShipDesign research = Design(ResearchStation);
        AssertEqual(station.ShipData.Hull, patrol.Hull, "setup: a patrol station on its hull");
        AssertEqual(station.ShipData.Hull, research.Hull, "setup: and a research station");

        Assert.IsTrue(RefitToWindow.CanRefitTo(station, patrol), "the refit list offers the patrol station");
        Assert.IsFalse(RefitToWindow.CanRefitTo(station, research), "but no research station, as for any station that is none");
        Assert.IsFalse(RefitToWindow.CanRefitTo(station, station.ShipData), "nor its own design");
    }
}
