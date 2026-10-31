using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Ship_Game;
using Ship_Game.Commands.Goals;
using Ship_Game.Ships;
using Vector2 = SDGraphics.Vector2;

namespace UnitTests.Ships;

// a system of derelicts, the player's homeworld with a shipyard, the player and the enemy able to build their race's salvage ship, and orders run as the game runs them: an order joins a salvage ship's mission, the queue reports the ship built at the mission's site, where it parks, its unit launches the order's constructor, and the platform goes up as the constructor builds it
public abstract class SalvageOrderTest : StarDriveTest
{
    protected static readonly Vector2 SystemSite = new(600_000f, 600_000f);
    protected static readonly Vector2 Wreck = SystemSite + new Vector2(30_000f, 0);
    protected const string Constructor = "Terran Constructor";
    protected const string TwinShip = "TEST_Twin Salvage Ship";
    protected readonly SolarSystem WreckSystem;
    protected readonly Planet Homeworld;

    protected SalvageOrderTest()
    {
        LoadStarterShips("Dreadnought mk1-a", ShipDesign.SalvagePlatformName, "Terran Salvage Ship", "Cordrazine Salvage Ship", TwinShip);
        CreateUniverseAndPlayerEmpire();
        UState.Objects.EnableParallelUpdate = false;
        WreckSystem = AddDummyPlanet(SystemSite, 1f, 1f, 1f, SystemSite + new Vector2(0, 60_000f), explored: true).System;
        Homeworld = AddHomeWorldToEmpire(new Vector2(200_000f), Player);
        AddRemoteSystems(10); // room under the cap for a few salvageable derelicts
        Player.UpdateRallyPoints();
        Player.data.CurrentConstructor = Constructor;
        Enemy.AddBuildableShip(ResourceManager.Ships.GetDesign(Constructor));
        CanBuild(Player, "Terran Salvage Ship");
        CanBuild(Enemy, "Cordrazine Salvage Ship");
    }

    [TestCleanup]
    public override void Cleanup()
    {
        UState.Derelicts.Clear();
        base.Cleanup();
    }

    // the empire can build the design, as its research would let it
    protected static void CanBuild(Empire empire, string design)
    {
        IShipDesign ship = ResourceManager.Ships.GetDesign(design);
        empire.AddBuildableShip(ship);
        empire.MarkShipRolesUsableForEmpire(ship);
    }

    // a salvageable derelict of the owner's, the player's unless given, so that empire knows of it; the ship itself is gone
    protected Derelict Forms(Vector2 at, Empire owner = null)
    {
        TestShip ship = SpawnShip("Dreadnought mk1-a", owner ?? Player, at);
        Assert.IsTrue(UState.Derelicts.TryAdd(ship, salvageable: true, seen: false), "setup: a salvageable derelict");
        Universe.InvokePendingSimThreadActions();
        ship.QueueTotalRemoval();
        return UState.Derelicts.Current.Last();
    }

    // the empire's order on the derelict, the player's unless given
    protected SalvageDerelict Ordered(Derelict d, bool recover = false, Empire empire = null)
    {
        empire ??= Player;
        Assert.IsTrue(SalvageDerelict.Order(empire, d, recover), "setup: ordered");
        return SalvageDerelict.OrderOf(empire, d);
    }

    // the mission the order joined as it was placed
    protected static SalvageMission MissionOf(SalvageDerelict order)
    {
        Assert.IsNotNull(order.Mission, "setup: the order joined a salvage ship's mission");
        return order.Mission;
    }

    // the queue reports the mission's salvage ship built, at the mission's site unless placed elsewhere
    protected Ship ShipBuilt(SalvageDerelict order, Vector2? at = null)
    {
        SalvageMission mission = MissionOf(order);
        if (mission.StepName == "BuildShip")
            mission.AdvanceToNextStep(); // past the queueing, which needs a shipyard
        Ship ship = SpawnShip(mission.Design.Name, order.Owner, at ?? mission.Site);
        mission.ReportShipComplete(ship);
        return ship;
    }

    // the player's salvage orders build the twin-unit salvage ship
    protected void TwinShips()
    {
        CanBuild(Player, TwinShip);
        Player.data.CurrentSalvageShip = TwinShip;
    }

    // a salvage ship of the empire's awaiting orders among its ships, the Terran salvage ship unless given
    protected Ship IdleShip(Vector2 at, Empire empire = null, string design = "Terran Salvage Ship")
    {
        empire ??= Player;
        Ship ship = SpawnShip(design, empire, at);
        UState.Objects.UpdateLists();
        Assert.IsTrue(SalvageMission.IsIdle(ship, empire) && empire.OwnedShips.Contains(ship), "setup: an idle salvage ship of the empire's");
        return ship;
    }

    // the mission the order joined, which queues its ship on its first turn
    protected static SalvageMission Queued(SalvageDerelict order)
    {
        SalvageMission mission = MissionOf(order);
        mission.Evaluate();
        return mission;
    }

    // the mission's ship, at its site, stops there, where its units launch
    protected static Ship Parked(SalvageDerelict order)
    {
        SalvageMission mission = MissionOf(order);
        if (!mission.Parked(out _))
            mission.Evaluate();
        Assert.IsTrue(mission.Parked(out Ship ship), "setup: the salvage ship is parked at its site");
        return ship;
    }

    // the order's constructor, launched from its parked ship's unit
    protected static Ship Launched(SalvageDerelict order)
    {
        Parked(order);
        order.Evaluate();
        Assert.IsNotNull(order.FinishedShip, "setup: a constructor launches");
        AssertEqual("BuildPlatform", order.StepName, "setup: the order waits for its platform");
        return order.FinishedShip;
    }

    // the platform its constructor builds on the order's site, which the constructor claimed the derelict for as it started; the platform's hold as given
    protected Ship PlatformBuilt(SalvageDerelict order, float? hold = null)
    {
        Ship constructor = order.FinishedShip;
        Assert.IsTrue(order.Derelict.TryClaim(order.Owner), "setup: the constructor claims the derelict as it starts to build");
        Ship platform = SpawnShip(ShipDesign.SalvagePlatformName, order.Owner, order.BuildPosition);
        if (hold != null)
            platform.CargoSpaceMax = hold.Value;
        constructor.QueueTotalRemoval();
        order.OnPlatformBuilt(platform, constructor);
        order.Evaluate();
        AssertEqual("Salvage", order.StepName, "setup: the platform stands");
        return platform;
    }

    // the empire's order with its platform standing beside the derelict, its salvage ship parked at its site, about to work
    protected SalvageDerelict AtWork(Derelict d, bool recover = false, Empire empire = null, float? hold = null)
    {
        SalvageDerelict order = Ordered(d, recover, empire);
        ShipBuilt(order);
        Launched(order);
        PlatformBuilt(order, hold);
        return order;
    }
}
