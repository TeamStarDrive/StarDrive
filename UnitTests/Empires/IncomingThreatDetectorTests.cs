using Microsoft.VisualStudio.TestTools.UnitTesting;
using SDUtils;
using Ship_Game.Fleets;
using Ship_Game.Ships;
using System;
using SDGraphics;
using Ship_Game;
using Ship_Game.Utils;

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
        LoadStarterShips("Fang Scout");
        CreateUniverseAndPlayerEmpire(enemyArchetype:"Kulrathi");
        
        // set up two solar systems
        PlayerPlanet = AddDummyPlanetToEmpire(new(200_000, 200_000), Player);
        EnemyPlanet = AddDummyPlanetToEmpire(new(-200_000, -200_000), Enemy);
        UState.P.GravityWellRange = 0;
        Player.InitEmpireFromSave(UState);
        Enemy.InitEmpireFromSave(UState);

        PlayerFleet = CreateTestFleet("TEST_Vulcan Scout", 12, Player, PlayerPlanet.Position);
        EnemyFleet = CreateTestFleet("TEST_Vulcan Scout", 12, Enemy, EnemyPlanet.Position);

        Player.SetFleet(1, PlayerFleet);
        Enemy.SetFleet(1, EnemyFleet);

        // unpause the universe to allow EmpireUpdate to run
        UState.Paused = false;
        UState.CanShowDiplomacyScreen = false;
        UState.Objects.EnableParallelUpdate = false;
        Enemy.AI.Disabled = true;
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

        var fleet = new Fleet(UState.CreateId(), owner);
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

    [TestMethod]
    public void PlayerEntersEnemySystem()
    {
        Universe.SingleSimulationStep(TestSimStep);
        MoveTo(PlayerFleet, EnemyPlanet.Position);

        RunFullSimWhile((simTimeout: 80.0, fatal: true),
            () => PlayerFleet.AveragePosition().OutsideRadius(EnemyPlanet.Position, 15_000));

        AssertEqual(0, Player.SystemsWithThreat.Length, "Player system should be safe");
        AssertEqual(1, Enemy.SystemsWithThreat.Length, "Enemy system should be under threat");

        // now kill all of our ships and wait a bit for SystemsWithThreat to reset:
        foreach (Ship s in PlayerFleet.Ships) s.InstantKill();
        RunFullSimWhile((simTimeout:20.0, fatal:false), () => Enemy.SystemsWithThreat.Length > 0);

        AssertEqual(0, Player.SystemsWithThreat.Length, "Player system should be safe");
        AssertEqual(0, Enemy.SystemsWithThreat.Length, "Enemy system should now be safe as well");
    }

    // Players have special conditions in the codebase, so we need to run another test from AI-s perspective
    [TestMethod]
    public void EnemyEntersPlayerSystem()
    {
        Universe.SingleSimulationStep(TestSimStep);
        MoveTo(EnemyFleet, PlayerPlanet.Position);

        RunFullSimWhile((simTimeout: 80.0, fatal: true),
            () => EnemyFleet.AveragePosition().OutsideRadius(PlayerPlanet.Position, 15_000));

        AssertEqual(0, Enemy.SystemsWithThreat.Length, "Enemy system should be safe");
        AssertEqual(1, Player.SystemsWithThreat.Length, "Player system should be under threat");

        // now kill all of our ships and wait a bit for SystemsWithThreat to reset:
        foreach (Ship s in EnemyFleet.Ships) s.InstantKill();
        RunFullSimWhile((simTimeout:20.0, fatal:false), () => Player.SystemsWithThreat.Length > 0);

        AssertEqual(0, Enemy.SystemsWithThreat.Length, "Enemy system should be safe");
        AssertEqual(0, Player.SystemsWithThreat.Length, "Player system should now be safe as well");
    }
}
