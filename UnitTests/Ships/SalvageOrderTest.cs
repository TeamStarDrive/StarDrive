using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Ship_Game;
using Ship_Game.Commands.Goals;
using Ship_Game.Ships;
using Vector2 = SDGraphics.Vector2;

namespace UnitTests.Ships;

// a system of derelicts, the player's homeworld with a shipyard, the Salvage Ships technology for the player and the enemy, and orders run as the game runs them: an order queues its salvage ship, the queue reports it built at the platform's site, and two turns of deploying put the platform up
public abstract class SalvageOrderTest : StarDriveTest
{
    protected static readonly Vector2 SystemSite = new(600_000f, 600_000f);
    protected static readonly Vector2 Wreck = SystemSite + new Vector2(30_000f, 0);
    protected readonly SolarSystem WreckSystem;
    protected readonly Planet Homeworld;

    protected SalvageOrderTest()
    {
        LoadStarterShips("Dreadnought mk1-a", ShipDesign.SalvagePlatformName, "Terran Salvage Ship", "Cordrazine Salvage Ship");
        CreateUniverseAndPlayerEmpire();
        UState.Objects.EnableParallelUpdate = false;
        WreckSystem = AddDummyPlanet(SystemSite, 1f, 1f, 1f, SystemSite + new Vector2(0, 60_000f), explored: true).System;
        Homeworld = AddHomeWorldToEmpire(new Vector2(200_000f), Player);
        AddRemoteSystems(10); // room under the cap for a few salvageable derelicts
        Player.UpdateRallyPoints();
        Player.data.AllowSalvageShip = true;
        Enemy.data.AllowSalvageShip = true;
    }

    [TestCleanup]
    public override void Cleanup()
    {
        UState.Derelicts.Clear();
        base.Cleanup();
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

    // the queue reports the order's salvage ship built, at the platform's site unless placed elsewhere
    protected Ship ShipBuilt(SalvageDerelict order, Vector2? at = null)
    {
        if (order.StepName == "BuildShip")
            order.AdvanceToNextStep(); // past the queueing, which needs a shipyard
        Ship ship = SpawnShip(order.Owner.SalvageShipDesign.Name, order.Owner, at ?? order.BuildPosition);
        order.ReportShipComplete(ship);
        return ship;
    }

    // the empire's order with its platform standing beside the derelict, as its salvage ship leaves it after deploying, about to work
    protected SalvageDerelict AtWork(Derelict d, bool recover = false, Empire empire = null)
    {
        SalvageDerelict order = Ordered(d, recover, empire);
        ShipBuilt(order);
        for (int turn = 0; turn < 1 + SalvageDerelict.DeployTurns; ++turn) // it starts to deploy at once, at its site
            order.Evaluate();
        AssertEqual("Salvage", order.StepName, "setup: the platform stands");
        Assert.IsNotNull(order.Platform, "setup: with its platform");
        return order;
    }
}
