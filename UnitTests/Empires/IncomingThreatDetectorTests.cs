using Microsoft.VisualStudio.TestTools.UnitTesting;
using SDUtils;
using Ship_Game.Fleets;
using Ship_Game.Ships;
using System;
using SDGraphics;
using Ship_Game;
using Ship_Game.Utils;
using System.Linq;
#pragma warning disable CA2213

namespace UnitTests.Empires;

[TestClass]
public class IncomingThreatDetectorTests : StarDriveTest
{
    Planet PlayerPlanet;
    Planet EnemyPlanet;
    Fleet PlayerFleet;
    Fleet EnemyFleet;

    public IncomingThreatDetectorTests()
    {
        // load required ships for Kulrathi
        LoadStarterShips("Fang Scout", "Unarmed Scout");
        CreateUniverseAndPlayerEmpire(enemyArchetype:"Kulrathi");
        
        // set up two solar systems
        PlayerPlanet = AddDummyPlanetToEmpire(new(200_000, 200_000), Player);
        EnemyPlanet = AddDummyPlanetToEmpire(new(-200_000, -200_000), Enemy);
        UState.P.GravityWellRange = 0;
        Player.InitEmpireFromSave(UState);
        Enemy.InitEmpireFromSave(UState);

        PlayerFleet = CreateTestFleet("Unarmed Scout", 12, Player, PlayerPlanet.Position);
        EnemyFleet = CreateTestFleet("Unarmed Scout", 12, Enemy, EnemyPlanet.Position);

        Player.SetFleet(1, PlayerFleet);
        Enemy.SetFleet(1, EnemyFleet);

        // unpause the universe to allow EmpireUpdate to run
        UState.Paused = false;
        UState.CanShowDiplomacyScreen = false;
        UState.Objects.EnableParallelUpdate = false;
        Enemy.AI.Disabled = true;
        UState.Events.Disabled = true;

        // required for full sim
        IEmpireData data = ResourceManager.AllRaces.First(e => e.Name == "Unknown");
        UState.CreateEmpire(data, isPlayer:false);
    }

    Fleet CreateTestFleet(string shipName, int numberWanted, Empire owner, Vector2 pos)
    {
        Array<Ship> ships = new();
        var random = new SeededRandom(1337);
        for (int i = 0; i < numberWanted; i++)
        {
            Ship ship = SpawnShip(shipName, owner, pos+random.Vector2D(1000));
            ships.Add(ship);
            if (ship.MaxFTLSpeed < Ship.LightSpeedConstant)
                throw new InvalidOperationException($"Invalid ship: '{ship.Name}' CANNOT WARP! MaxFTLSpeed: {ship.MaxFTLSpeed:0}");
        }

        Fleet fleet = owner.CreateFleet(1, null);
        fleet.AddShips(ships);
        fleet.AutoArrange();
        return fleet;
    }

    [TestMethod]
    public void NoThreatsLoggedByDefault()
    {
        Universe.SingleSimulationStep(TestSimStep);
        AssertEqual(0, Player.SystemsWithThreat.Length, "No systems should be under threat");
        AssertEqual(0, Enemy.SystemsWithThreat.Length, "No systems should be under threat");
    }

    void MoveTo(Fleet f, Vector2 pos) => f.MoveTo(pos, f.AveragePosition().DirectionToTarget(pos));

    void PrintFleetStatus(string name, Fleet f)
    {
        Log.Info($"{name} Fleet={f.Name} AvgPos={f.AveragePosition()} Status={f.FleetMoveStatus(UState.P.GravityWellRange)}");
        foreach (Ship s in f.Ships)
            Log.Info($"  Ship Dist={s.Position.Distance(f.FinalPosition).GetNumberString()} Spd={s.CurrentVelocity.GetNumberString()} InWarp={s.IsInWarp} AI={s.AI.State} Plan={s.AI.OrderQueue.PeekFirst?.Plan} FormationWarp:{s.ShipEngines.ReadyForFormationWarp} HP={s.HealthPercent*100:0.0}%");
    }

    bool HasFleetArrivedAt(Fleet f, Vector2 at)
    {
        return f.Ships.Any(s => s.Position.InRadius(at, 10_000));
    }

    [TestMethod]
    public void PlayerEntersEnemySystem()
    {
        Universe.SingleSimulationStep(TestSimStep);
        MoveTo(PlayerFleet, EnemyPlanet.Position);

        try
        {
            RunFullSimWhile((simTimeout:80, fatal:true),
                () => !HasFleetArrivedAt(PlayerFleet, PlayerFleet.FinalPosition));
        }
        finally
        {
            PrintFleetStatus("PLAYER", PlayerFleet);
        }

        AssertEqual(0, Player.SystemsWithThreat.Length, "Player system should be safe");
        AssertEqual(1, Enemy.SystemsWithThreat.Length, "Enemy system should be under threat");

        // now kill all of our ships and wait a bit for SystemsWithThreat to reset:
        foreach (Ship s in PlayerFleet.Ships) s.InstantKill();
        RunFullSimWhile((simTimeout:20, fatal:false), () => Enemy.SystemsWithThreat.Length > 0);

        AssertEqual(0, Player.SystemsWithThreat.Length, "Player system should be safe");
        AssertEqual(0, Enemy.SystemsWithThreat.Length, "Enemy system should now be safe as well");
    }

    // Players have special conditions in the codebase,
    // so we need to run another test from AI-s perspective
    [TestMethod]
    public void EnemyEntersPlayerSystem()
    {
        Universe.SingleSimulationStep(TestSimStep);
        MoveTo(EnemyFleet, PlayerPlanet.Position);

        try
        {
            RunFullSimWhile((simTimeout:80, fatal:true),
                () => !HasFleetArrivedAt(EnemyFleet, EnemyFleet.FinalPosition));
        }
        finally
        {
            PrintFleetStatus("ENEMY", EnemyFleet);
        }

        AssertEqual(0, Enemy.SystemsWithThreat.Length, "Enemy system should be safe");
        AssertEqual(1, Player.SystemsWithThreat.Length, "Player system should be under threat");

        // now kill all of our ships and wait a bit for SystemsWithThreat to reset:
        foreach (Ship s in EnemyFleet.Ships) s.InstantKill();
        RunFullSimWhile((simTimeout:20, fatal:false), () => Player.SystemsWithThreat.Length > 0);

        AssertEqual(0, Enemy.SystemsWithThreat.Length, "Enemy system should be safe");
        AssertEqual(0, Player.SystemsWithThreat.Length, "Player system should now be safe as well");
    }
}
