using System.Reflection;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using SDUtils;
using Ship_Game;
using Ship_Game.AI;
using Ship_Game.AI.Tasks;
using Ship_Game.Empires.Components;
using Ship_Game.Fleets;
using Ship_Game.Ships;
using Ship_Game.Utils;
using Vector2 = SDGraphics.Vector2;

namespace UnitTests.Fleets;

/// <summary>
/// An AI exploration force goes on to its own empire's colony while an event is left there, and its task ends at a
/// planet another empire owns, rebels included, but not at a planet of the Unknown empire. It never bombs its target,
/// and ending its task at its own colony leaves the empire's troops on the planet. When the AI knows less of a threat
/// at the planet than before, what the task asks falls at most to half of what it asked the time before.
/// </summary>
[TestClass]
public class ExplorationFleetTests : StarDriveTest
{
    const string Warship = "Terran-Prototype";
    const string TroopShip = "Proton Troopship  Mk1";
    const string Bomber = "Neutron Bomber Mk1";

    public ExplorationFleetTests()
    {
        LoadStarterShips(Warship, TroopShip, Bomber);
        CreateUniverseAndPlayerEmpire();
        LoadStarterShips(Enemy.data.DefaultTroopShip);
        UState.Objects.EnableParallelUpdate = false;
        UState.Events.Disabled = true;
    }

    // a planet with tiles, an event building on one of them unless asked otherwise, owned by the given empire or free
    Planet EventPlanet(Empire owner, Vector2 at, bool withEvent = true)
    {
        Planet p = AddDummyPlanet(at);
        p.GenerateNewFromPlanetType(new SeededRandom(1), ResourceManager.Planets.PlanetOrRandom(1), 1f);
        typeof(SolarSystemBody).GetProperty(nameof(SolarSystemBody.GravityWellRadius))!.SetValue(p, 10_000f);
        if (owner != null)
            p.SetOwner(owner);

        if (withEvent)
        {
            Building b = ResourceManager.CreateBuilding(p, Building.TerraformerId);
            b.EventTriggerUID = "TestEvent";
            foreach (PlanetGridSquare tile in p.TilesList)
            {
                if (tile.NoBuildingOnTile)
                {
                    tile.PlaceBuilding(b, p);
                    break;
                }
            }
        }

        Assert.AreEqual(withEvent, p.EventsOnTiles(), "setup: an event is left on the planet or not");
        Assert.AreEqual(owner, p.Owner, "setup: the planet's owner");
        return p;
    }

    Planet EventPlanet(Empire owner, bool withEvent = true) => EventPlanet(owner, new Vector2(400_000), withEvent);

    // the AI's exploration force for the planet at the given step of its task, just outside the planet's well
    Fleet ExplorationForce(Planet target, int taskStep, out MilitaryTask task, bool bomber = false)
    {
        task = MilitaryTask.CreateExploration(target, Enemy);
        var fleet = new Fleet(UState, Enemy) { FleetTask = task, TaskStep = taskStep };
        typeof(MilitaryTask).GetProperty(nameof(MilitaryTask.Fleet))!.SetValue(task, fleet);
        Vector2 at = target.Position + new Vector2(target.GravityWellRadius + 10_000f, 0f);
        fleet.AddShip(SpawnShip(Warship, Enemy, at));
        fleet.AddShip(SpawnShip(TroopShip, Enemy, at + new Vector2(0f, 1000f)));
        if (bomber)
            fleet.AddShip(SpawnShip(Bomber, Enemy, at + new Vector2(0f, -1000f)));
        return fleet;
    }

    static void ExplorePlanet(Fleet fleet, MilitaryTask task)
        => typeof(Fleet).GetMethod("DoExplorePlanet", BindingFlags.Instance | BindingFlags.NonPublic)!.Invoke(fleet, new object[] { task });

    void ReadyShipsForFleets() => Enemy.ShipsReadyForFleet = new FleetShips(Enemy, Enemy.AllFleetReadyShips());

    static Ship BomberOf(Fleet fleet) => fleet.Ships.Find(s => s.Name == Bomber);

    [TestMethod]
    public void AnExplorationForceGoesOnToItsOwnColonyWhileAnEventIsLeftThere()
    {
        Planet colony = EventPlanet(Enemy);
        Fleet fleet = ExplorationForce(colony, taskStep: 5, out MilitaryTask task);

        ExplorePlanet(fleet, task);

        Assert.AreEqual(task, fleet.FleetTask, "the force keeps its task at its own colony");
        Assert.IsFalse(task.QueuedForRemoval, "the task goes on");
    }

    [TestMethod]
    public void AnExplorationForceEndsItsTaskOnceNoEventIsLeftOnItsOwnColony()
    {
        Planet colony = EventPlanet(Enemy, withEvent: false);
        Fleet fleet = ExplorationForce(colony, taskStep: 5, out MilitaryTask task);

        ExplorePlanet(fleet, task);

        Assert.IsNull(fleet.FleetTask, "nothing is left to explore");
        Assert.IsTrue(task.QueuedForRemoval, "the task ends");
    }

    [TestMethod]
    public void AnExplorationForceEndsItsTaskAtAPlanetAnotherEmpireOwnsButNotAtOneOfTheUnknownEmpire()
    {
        CreateAMinorFaction("Corsairs");
        UState.Unknown = Faction;
        Empire rebels = UState.CreateRebelsFromEmpireData(Player.data, Player);
        Fleet player = ExplorationForce(EventPlanet(Player, new Vector2(400_000)), taskStep: 5, out MilitaryTask playerTask);
        Fleet rebel = ExplorationForce(EventPlanet(rebels, new Vector2(800_000)), taskStep: 5, out MilitaryTask rebelTask);
        Fleet unknown = ExplorationForce(EventPlanet(Faction, new Vector2(1_200_000)), taskStep: 5, out MilitaryTask unknownTask);

        ExplorePlanet(player, playerTask);
        ExplorePlanet(rebel, rebelTask);
        ExplorePlanet(unknown, unknownTask);

        Assert.IsTrue(playerTask.QueuedForRemoval, "the task ends at a planet of another empire");
        Assert.IsTrue(rebelTask.QueuedForRemoval, "and at a planet of rebels");
        Assert.IsFalse(unknownTask.QueuedForRemoval, "the force goes on to invade a planet of the Unknown empire");
        Assert.AreEqual(unknownTask, unknown.FleetTask, "and keeps its task there");
    }

    [TestMethod]
    public void AnExplorationTaskEndsForAPlanetOfRebelsButGoesOnForItsOwnColonyAndTheUnknownEmpire()
    {
        CreateAMinorFaction("Corsairs");
        UState.Unknown = Faction;
        Empire rebels = UState.CreateRebelsFromEmpireData(Player.data, Player);
        ReadyShipsForFleets();
        MilitaryTask rebel = MilitaryTask.CreateExploration(EventPlanet(rebels, new Vector2(400_000)), Enemy);
        MilitaryTask own = MilitaryTask.CreateExploration(EventPlanet(Enemy, new Vector2(800_000)), Enemy);
        MilitaryTask unknown = MilitaryTask.CreateExploration(EventPlanet(Faction, new Vector2(1_200_000)), Enemy);

        rebel.Evaluate(Enemy);
        own.Evaluate(Enemy);
        unknown.Evaluate(Enemy);

        Assert.IsTrue(rebel.QueuedForRemoval, "a task for a planet rebels hold ends before it forms a force");
        Assert.IsFalse(own.QueuedForRemoval, "a task for the empire's own colony with an event left goes on");
        Assert.IsFalse(unknown.QueuedForRemoval, "a task for a planet of the Unknown empire goes on");
    }

    [TestMethod]
    public void AnExplorationForceNeverBombsItsTarget()
    {
        CreateAMinorFaction("Corsairs");
        UState.Unknown = Faction;
        Fleet own = ExplorationForce(EventPlanet(Enemy, new Vector2(400_000)), taskStep: 5, out MilitaryTask ownTask, bomber: true);
        Fleet unknown = ExplorationForce(EventPlanet(Faction, new Vector2(800_000)), taskStep: 5, out MilitaryTask unknownTask, bomber: true);

        ExplorePlanet(own, ownTask);
        ExplorePlanet(unknown, unknownTask);

        Assert.AreNotEqual(AIState.Bombard, BomberOf(own).AI.State, "no bomber is sent to bomb the empire's own colony");
        Assert.AreNotEqual(AIState.Bombard, BomberOf(unknown).AI.State, "nor a planet of the Unknown empire, where bombs could wreck the event");
        Assert.AreEqual(ownTask, own.FleetTask, "setup: the force keeps its task at its own colony");
        Assert.AreEqual(unknownTask, unknown.FleetTask, "setup: and at the Unknown empire's planet");

        typeof(Fleet).GetMethod("StartBombing", BindingFlags.Instance | BindingFlags.NonPublic)!.Invoke(unknown, new object[] { unknownTask.TargetPlanet });
        Assert.AreEqual(AIState.Bombard, BomberOf(unknown).AI.State, "setup: the bomber does bomb the planet when it is sent");
    }

    // ends the task of a force that landed a troop of the empire on the planet
    int TroopsLeftAfterTheTaskEnds(Planet target)
    {
        Fleet fleet = ExplorationForce(target, taskStep: 5, out MilitaryTask task);
        Troop troop = ResourceManager.CreateTroop("Wyvern", Enemy);
        Assert.IsTrue(troop.TryLandTroop(target), "setup: a troop of the empire is on the planet");
        troop.UpdateMoveActions(troop.MaxStoredActions);
        Assert.IsTrue(troop.CanLaunch, "setup: the troop can launch");

        task.EndTask();

        Assert.IsNull(fleet.FleetTask, "setup: the task ended");
        return target.Troops.NumTroopsHere(Enemy);
    }

    [TestMethod]
    public void EndingAnExplorationTaskAtItsOwnColonyLeavesTheEmpiresTroopsThere()
    {
        Assert.AreEqual(0, TroopsLeftAfterTheTaskEnds(EventPlanet(null, new Vector2(400_000))),
                        "setup: at a free planet the troops are taken off when the task ends");
        Assert.AreEqual(1, TroopsLeftAfterTheTaskEnds(EventPlanet(Enemy, new Vector2(800_000))),
                        "at the empire's own colony its troops stay");
    }

    // a player warship at the planet that the AI sees
    void SeenAtThePlanet(Planet target)
    {
        Ship guard = SpawnShip(Warship, Player, target.Position + new Vector2(5000f, 0f));
        guard.SetSystem(target.System);
        Enemy.Threats.SetSeen(guard, fromBackgroundThread: false);
        Enemy.Threats.Update(new FixedSimTime(2f));
    }

    [TestMethod]
    public void AThreatNoLongerKnownStillAsksHalfOfWhatTheTaskAskedEachTime()
    {
        Planet target = EventPlanet(null);
        ReadyShipsForFleets();
        MilitaryTask task = MilitaryTask.CreateExploration(target, Enemy);
        task.EnemyStrength = 4000f;

        task.Evaluate(Enemy);
        Assert.IsNull(task.Fleet, "setup: no force is formed");
        AssertEqual(0.01f, 2000f, task.EnemyStrength, "with nothing known there the task asks half of what it asked");
        task.Evaluate(Enemy);
        AssertEqual(0.01f, 1000f, task.EnemyStrength, "and half again the next time");

        task.EnemyStrength = 150f;
        task.Evaluate(Enemy);
        AssertEqual(0.01f, 100f, task.EnemyStrength, "down to the least a task asks");

        SeenAtThePlanet(target);
        float known = Enemy.Threats.GetHostileStrengthAt(target.Position, target.System.Radius);
        AssertGreaterThan(known, 100f, "setup: the AI now knows a threat at the planet");
        task.Evaluate(Enemy);
        AssertEqual(0.01f, known, task.EnemyStrength, "once a threat is known again the task asks what is known");
        Assert.IsFalse(task.QueuedForRemoval, "setup: the task goes on all along");
    }

    [TestMethod]
    public void AThreatPartlyForgottenDropsWhatTheTaskAsksAtMostToHalf()
    {
        Planet target = EventPlanet(null);
        ReadyShipsForFleets();
        MilitaryTask task = MilitaryTask.CreateExploration(target, Enemy);
        SeenAtThePlanet(target);
        float known = Enemy.Threats.GetHostileStrengthAt(target.Position, target.System.Radius);
        AssertGreaterThan(known, 100f, "setup: the AI knows a threat at the planet");

        task.EnemyStrength = known * 4f;
        task.Evaluate(Enemy);
        Assert.IsNull(task.Fleet, "setup: no force is formed");
        AssertEqual(0.01f, known * 2f, task.EnemyStrength, "less known than half of what the task asked: it asks half");

        task.EnemyStrength = known * 1.5f;
        task.Evaluate(Enemy);
        AssertEqual(0.01f, known, task.EnemyStrength, "more known than half of what it asked: it asks what is known");
        Assert.IsFalse(task.QueuedForRemoval, "setup: the task goes on all along");
    }
}
