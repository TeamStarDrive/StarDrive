using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Windows.Forms;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using SDGraphics;
using SDUtils;
using Ship_Game;
using Ship_Game.AI;
using Ship_Game.Data;
using Ship_Game.Gameplay;
using Ship_Game.GameScreens.NewGame;
using Ship_Game.Ships;
using Ship_Game.Universe;
using Ship_Game.Universe.SolarBodies;
using Ship_Game.Utils;
using SynapseGaming.LightingSystem.Core;
using UnitTests.Ships;
using UnitTests.UI;
using ResourceManager = Ship_Game.ResourceManager;
using Vector2 = SDGraphics.Vector2;

namespace UnitTests;

/// <summary>
/// Automatic setup for StarDrive unit tests
/// </summary>
public partial class StarDriveTest : IDisposable
{
    // Game and Content is shared between all StarDriveTests
    public static TestGameDummy Game => StarDriveTestContext.Game;
    public static GameContentManager Content => StarDriveTestContext.Content;
    public static MockInputProvider MockInput => StarDriveTestContext.MockInput;

    // Universe and Player/Enemy empires are specific for each test instance
    public UniverseScreen Universe { get; private set; }
    public UniverseState UState { get; private set; }
    public Empire Player { get; private set; }
    public Empire Enemy { get; private set; }
    public Empire ThirdMajor { get; private set; }
    public Empire Faction { get; private set; }

    public const double TestSimStepD = 1.0 / 60.0;
    public readonly FixedSimTime TestSimStep = new((float)TestSimStepD);

    public StarDriveTest()
    {
    }

    public static void EnableMockInput(bool enabled)
    {
        StarDriveTestContext.EnableMockInput(enabled);
    }

    public void Dispose()
    {
        Dispose(true);
    }

    static void RequireGameInstance(string functionName)
    {
        if (Game == null)
            throw new Exception($"CreateGameInstance() must be called BEFORE {functionName}() !");
    }

    static void RequireStarterContentLoaded(string functionName)
    {
        if (ResourceManager.AllRaces.Count == 0)
            throw new Exception($"LoadStarterContent() or LoadStarterShips() must be called BEFORE {functionName}() !");
    }

    void SetEveryoneAsKnown(Empire us)
    {
        foreach (Empire them in UState.Empires)
        {
            if (them != us)
            {
                Empire.SetRelationsAsKnown(them, us);
                Empire.UpdateBilateralRelations(them, us);
            }
        }
    }

    public void CreateThirdMajorEmpire()
    {
        IEmpireData data = ResourceManager.MajorRaces.First(e => UState.GetEmpireByName(e.Name) == null);
        ThirdMajor = UState.CreateEmpire(data, isPlayer:false);
        SetEveryoneAsKnown(ThirdMajor);
    }

    public void CreateRebelFaction()
    {
        IEmpireData data = ResourceManager.MajorRaces.First(e => e.Name == Player.data.Name);
        Faction = UState.CreateRebelsFromEmpireData(data, Player);
        SetEveryoneAsKnown(ThirdMajor);
    }

    public void CreateAMinorFaction(string name)
    {
        IEmpireData data = ResourceManager.MinorRaces.First(e => e.Name.Contains(name) && UState.GetEmpireByName(e.Name) == null);
        Faction = UState.CreateEmpire(data, isPlayer: false);
        SetEveryoneAsKnown(Faction);
    }

    /// <param name="playerArchetype">for example "Human"</param>
    public void CreateUniverseAndPlayerEmpire(string playerArchetype = null,
        string enemyArchetype = null,
        float universeRadius = 2_000_000f,
        UniverseParams settings = null)
    {
        RequireGameInstance(nameof(CreateUniverseAndPlayerEmpire));
        RequireStarterContentLoaded(nameof(CreateUniverseAndPlayerEmpire));

        var sw = new Stopwatch();

        IEmpireData playerData = ResourceManager.MajorRaces[0];
        IEmpireData enemyData = ResourceManager.MajorRaces[1];
        if (playerArchetype != null)
        {
            playerData = ResourceManager.MajorRaces.FirstOrDefault(e => e.ArchetypeName.Contains(playerArchetype));
            if (playerData == null)
                throw new($"Could not find MajorRace archetype matching '{playerArchetype}'");
            enemyData = ResourceManager.MajorRaces.FirstOrDefault(e => e != playerData);
        }
        if (enemyArchetype != null)
        {
            enemyData = ResourceManager.MajorRaces.FirstOrDefault(e => e.ArchetypeName.Contains(enemyArchetype));
            if (enemyData == null)
                throw new($"Could not find MajorRace archetype matching '{enemyArchetype}'");
        }

        Universe = new UniverseScreen(settings ?? new UniverseParams(), universeRadius: universeRadius);
        UState = Universe.UState;
        UState.CanShowDiplomacyScreen = false;
        Player = UState.CreateEmpire(playerData, isPlayer:true);
        Enemy = UState.CreateEmpire(enemyData, isPlayer:false);
            
        Universe.viewState = UniverseScreen.UnivScreenState.PlanetView;
        Player.TestInitModifiers();
        Empire.SetRelationsAsKnown(Player, Enemy);
        Player.AI.DeclareWarOn(Enemy, WarType.BorderConflict);

        if (!Player.IsAtWarWith(Enemy) || !Enemy.IsAtWarWith(Player))
            throw new("Failed to declare war from Player to Enemy.");
            
        Log.Info($"CreateUniverseAndPlayerEmpire elapsed: {sw.Elapsed.TotalMilliseconds}ms");
    }

    public void CreateDeveloperSandboxUniverse(string playerPreference, int numOpponents, bool paused)
    {
        LoadAllGameData(); // we need all ships and stuff
        Universe = DeveloperUniverse.Create(playerPreference, numOpponents);
        UState = Universe.UState;
        UState.Paused = paused;
        Player = UState.Player;
        Enemy  = UState.NonPlayerEmpires[0];

        Universe.CreateSimThread = false;
        Universe.LoadContent();
    }

    public void CreateCustomUniverse(UniverseParams p)
    {
        Universe = new UniverseGenerator(p).Generate();
        UState = Universe.UState;
        Player = UState.Player;
        Enemy = UState.NonPlayerEmpires[0];
    }

    protected bool LoadedExtraData;

    // Temporarily loads all game data. It will be unloaded after the current TestMethod finishes.
    public void LoadAllGameData()
    {
        LoadedExtraData = true;
        Directory.CreateDirectory(SavedGame.DefaultSaveGameFolder);

        ScreenManager.Instance.UpdateGraphicsDevice(); // create SpriteBatch
        GlobalStats.AsteroidVisibility = ObjectVisibility.None; // dont create Asteroid SO's

        ResourceManager.UnloadAllData(ScreenManager.Instance);
        ResourceManager.LoadItAll(ScreenManager.Instance, null);
    }

    // this will clean up any extra data after a TestMethod finishes
    [TestCleanup]
    public virtual void Cleanup()
    {
        if (LoadedExtraData)
        {
            LoadedExtraData = false;
            ResourceManager.UnloadAllData(ScreenManager.Instance);
            StarDriveTestContext.LoadStarterContent();
        }
    }

    public void CreateCustomUniverseSandbox(int numOpponents, GalSize galSize, int numExtraShipsPerEmpire = 0)
    {
        LoadAllGameData();
        (int numStars, float starNumModifier) = RaceDesignScreen.GetNumStars(
            RaceDesignScreen.StarsAbundance.Abundant, galSize, numOpponents
        );

        EmpireData playerData = ResourceManager.FindEmpire("United").CreateInstance();
        playerData.DiplomaticPersonality = new DTrait();

        CreateCustomUniverse(new UniverseParams
        {
            PlayerData = playerData,
            Mode = RaceDesignScreen.GameMode.Sandbox,
            GalaxySize = galSize,
            NumSystems = numStars,
            NumOpponents = numOpponents,
            StarsModifier = starNumModifier,
            Pace = 1.0f,
            Difficulty = GameDifficulty.Normal,
        });
        Universe.CreateSimThread = false;
        Universe.LoadContent();

        if (numExtraShipsPerEmpire > 0)
        {
            UniverseState u = Universe.UState;
            foreach (Empire e in u.MajorEmpires)
            {
                for (int i = 0; i < numExtraShipsPerEmpire; ++i)
                {
                    Ship.CreateShipAt(UState, e.data.PrototypeShip, e, 
                        e.Capital, e.Capital.Position + u.Random.Vector2D(e.Capital.Radius * 3), true);
                }
            }
        }
    }

    protected virtual void Dispose(bool disposing)
    {
        Universe?.ExitScreen();
        Universe?.Dispose();
        Universe = null;
    }

    public void UnlockAllShipsFor(Empire empire)
    {
        foreach (IShipDesign design in ResourceManager.Ships.Designs)
            empire.AddBuildableShip(design);
    }

    public void UnlockAllTechsForShip(Empire empire, string shipName)
    {
        Ship ship = ResourceManager.GetShipTemplate(shipName);
        empire.UnlockedHullsDict[ship.ShipData.Hull] = true;

        // this populates `TechsNeeded`
        ShipDesignUtils.MarkDesignsUnlockable();

        foreach (var tech in ship.ShipData.TechsNeeded)
        {
            empire.UnlockTech(tech, TechUnlockType.Normal);
        }
    }

    /// <summary>
    /// Should be run after empires are created.
    /// * fucks up ship templates and hulls by corrupting the data
    /// populates all shipdata techs
    /// unlocks all non shiptech. Locks all shiptechs.
    /// Sets all ships to belong to Enemy Empire
    /// Clears Ships WeCanBuild
    /// </summary>
    public void PrepareShipAndEmpireForShipTechTests()
    {
        ShipDesignUtils.MarkDesignsUnlockable();

        Player.ClearShipsWeCanBuild();
        Enemy.ClearShipsWeCanBuild();
        var techs = Enemy.TechEntries;
        foreach (var tech in techs)
        {
            if (tech.IsRoot)
                continue;

            if (tech.ContainsShipTech())
                tech.Unlocked = false;
            else
                tech.Unlocked = true;
        }
    }

    /// <summary>
    /// Since some Unit Tests have side-effects to global ships list
    /// This can be used to reset starter ships
    /// </summary>
    public static void ReloadStarterShips()
    {
        StarDriveTestContext.ReloadStarterShips();
    }

    // Loads additional ships into ResourceManager
    // If the ship is already loaded this will be a No-Op
    public static void LoadStarterShips(params string[] shipList)
    {
        ResourceManager.LoadStarterShipsForTesting(shipList);
    }

    public static void OnlyLoadShips(params string[] shipList)
    {
        ResourceManager.LoadStarterShipsForTesting(shipList, clearAll: true);
    }

    public static void ReloadTechTree()
    {
        ResourceManager.LoadTechTree();
    }

    public TestShip SpawnShip(string shipName, Empire empire, Vector2 position, Vector2 shipDirection = default)
    {
        if (!ResourceManager.GetShipTemplate(shipName, out Ship template))
            throw new Exception($"Failed to find ship template: {shipName} (did you call LoadStarterShips?)");

        return SpawnShip(template, empire, position, shipDirection);
    }
        
    public TestShip SpawnShip(Ship template, Empire empire, Vector2 position, Vector2 shipDirection = default)
    {
        var ship = new TestShip(UState, template, empire, position);
        if (!ship.HasModules)
            throw new Exception($"Failed to create ship modules: {template.Name} (did you load modules?)");

        UState?.AddShip(ship);
        ship.Rotation = shipDirection.Normalized().ToRadians();
        ship.UpdateShipStatus(new FixedSimTime(0.01f)); // update module pos
        ship.UpdateModulePositions(new FixedSimTime(0.01f), true, forceUpdate: true);
        ship.System = null;
        AssertTrue(ship.Active, "Spawned ship is Inactive! This is a bug in Status update!");

        return ship;
    }

    public TestShip SpawnShipNoCombatHoldPos(string shipName, Empire empire, Vector2 position)
    {
        TestShip s = SpawnShip(shipName, empire, position);
        s.AI.IgnoreCombat = true;
        s.AI.OrderHoldPosition(MoveOrder.StandGround|MoveOrder.HoldPosition);
        return s;
    }

    SolarSystem CreateRandomSolarSystem(Vector2 sysPos)
    {
        if (sysPos == Vector2.Zero)
            throw new ArgumentException("SolarSystem position must not be Zero");
        return new SolarSystem(UState, sysPos)
        {
            Sun = SunType.RandomHabitableSun(UState.Random)
        };
    }
        
    void AddPlanetToSolarSystem(SolarSystem s, Planet p)
    {
        p.OrbitalAngle = p.Position.AngleToTarget(s.Position);
        p.TestSetOrbitalRadius(p.Position.Distance(s.Position) + p.Radius);

        s.RingList.Add(new SolarSystem.Ring { Asteroids = false, OrbitalDistance = p.OrbitalRadius, Planet = p });
        s.PlanetList.Add(p);
        UState.AddSolarSystem(s);
    }

    public Planet AddDummyPlanet(Vector2 sysPos, float fertility=1f, float minerals=1f, float pop=4f)
    {
        return AddDummyPlanet(sysPos, fertility, minerals, pop, sysPos+new Vector2(5000), explored:false);
    }

    public Planet AddDummyPlanet(Vector2 sysPos, float fertility, float minerals, float pop, Vector2 pos, bool explored)
    {
        SolarSystem s = CreateRandomSolarSystem(sysPos);
        var p = new Planet(UState.CreateId(), s, pos, fertility, minerals, pop);
        AddPlanetToSolarSystem(s, p);

        if (explored)
        {
            s.SetExploredBy(Player);
            p.SetExploredBy(Player);
        }
        return p;
    }

    public Planet AddDummyPlanetToEmpire(Vector2 sysPos, Empire empire)
    {
        return AddDummyPlanetToEmpire(sysPos, empire, 0, 0, 0);
    }

    public Planet AddDummyPlanetToEmpire(Vector2 sysPos, Empire empire, float fertility, float minerals, float maxPop)
    {
        Planet p = AddDummyPlanet(sysPos, fertility, minerals, maxPop, sysPos+new Vector2(5000), explored:false);
        p.SetOwner(empire);
        return p;
    }

    public Planet AddHomeWorldToEmpire(Vector2 sysPos, Empire empire)
    {
        return AddHomeWorldToEmpire(sysPos, empire, sysPos + new Vector2(5000));
    }

    public Planet AddHomeWorldToEmpire(Vector2 sysPos, Empire empire, Vector2 pos, bool explored = false)
    {
        SolarSystem s = CreateRandomSolarSystem(sysPos);

        var random = new SeededRandom();
        var data = ResourceManager.LoadSolarSystemData(empire.data.Traits.HomeSystemName);
        var ring = data.RingList.Find(r => r.HomePlanet);
        float orbitalAngle = pos.AngleToTarget(s.Position);
        float orbitalRadius = pos.Distance(s.Position);
        var p = new Planet(UState.CreateId(), random, s, orbitalAngle, orbitalRadius,
            null, 100_000, empire, ring);
        AssertTrue(p.IsHomeworld, "Created Planet must be a homeworld!");
        AddPlanetToSolarSystem(s, p);

        if (explored)
        {
            s.SetExploredBy(Player);
            p.SetExploredBy(Player);
        }
        return p;
    }

    public Projectile[] GetProjectiles(Ship ship)
    {
        return UState.Objects.GetProjectiles(ship);
    }

    public int GetProjectileCount(Ship ship)
    {
        return GetProjectiles(ship).Length;
    }

    /// <summary>
    /// Runs object simulation for up to `simTimeout` simulation seconds
    /// Each step is taken using TestSimTimeStep (1/60)
    /// if `fatal` == true, throws exception if timeout is reached and the test should fail
    /// </summary>
    /// <returns>Elapsed Simulation Time</returns>
    public double RunSimWhile((double simTimeout, bool fatal) timeout, Func<bool> condition = null, Action body = null)
    {
        double elapsedSimTime = 0.0;
        while (condition == null || condition())
        {
            body?.Invoke();

            // update after invoking body, to avoid side-effects in condition()
            // if we update universe before body(), then body() CAN observe condition() == false
            UState.Objects.Update(TestSimStep);

            elapsedSimTime += TestSimStepD;
            if (elapsedSimTime >= timeout.simTimeout)
            {
                if (timeout.fatal)
                    throw new TimeoutException("Timed out in RunSimWhile");
                return elapsedSimTime; // timed out
            }
        }
        return elapsedSimTime;
    }
        
    /// <summary>
    /// Runs full universe simulation for up to `simTimeout` simulation seconds
    /// Each step is taken using TestSimTimeStep (1/60)
    /// if `fatal` == true, throws exception if timeout is reached and the test should fail
    /// </summary>
    /// <returns>Elapsed Simulation Time</returns>
    public double RunFullSimWhile((double simTimeout, bool fatal) timeout, Func<bool> condition = null, Action body = null)
    {
        double elapsedSimTime = 0.0;
        while (condition == null || condition())
        {
            body?.Invoke();

            // update after invoking body, to avoid side-effects in condition()
            // if we update universe before body(), then body() CAN observe condition() == false
            Universe.SingleSimulationStep(TestSimStep);

            elapsedSimTime += TestSimStepD;
            if (elapsedSimTime >= timeout.simTimeout)
            {
                if (timeout.fatal)
                    throw new TimeoutException("Timed out in RunSimWhile");
                return elapsedSimTime; // timed out
            }
        }
        return elapsedSimTime;
    }
    /// <summary>
    /// Update Universe.Objects simulation for `totalSimSeconds`
    /// </summary>
    public float RunObjectsSim(float totalSimSeconds, Action body = null)
    {
        // we run multiple iterations in order to allow the universe to properly simulate in fixed steps
        float time = 0f;
        for (; time < totalSimSeconds; time += TestSimStep.FixedTime)
        {
            UState.Objects.Update(TestSimStep);
            body?.Invoke();
        }
        return time;
    }

    public float RunObjectsSim(FixedSimTime totalTime, Action body = null)
    {
        return RunObjectsSim(totalTime.FixedTime, body);
    }
}
