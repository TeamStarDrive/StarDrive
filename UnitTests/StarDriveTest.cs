using System;
using System.Diagnostics;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Microsoft.Xna.Framework;
using Ship_Game;
using Ship_Game.Data;
using Ship_Game.Gameplay;
using Ship_Game.GameScreens.NewGame;
using Ship_Game.Ships;
using Ship_Game.Universe;
using Ship_Game.Universe.SolarBodies;
using UnitTests.Ships;
using UnitTests.UI;

namespace UnitTests
{
    /// <summary>
    /// Automatic setup for StarDrive unit tests
    /// </summary>
    public class StarDriveTest : IDisposable
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

        public readonly double TestSimStepD = 1.0 / 60.0;
        public readonly FixedSimTime TestSimStep = new FixedSimTime((float)(1.0 / 60.0));

        public StarDriveTest()
        {
        }

        public static void EnableMockInput(bool enabled)
        {
            StarDriveTestContext.EnableMockInput(enabled);
        }

        public void Dispose()
        {
            DestroyUniverse();
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

        /// <param name="playerArchetype">for example "Human"</param>
        public void CreateUniverseAndPlayerEmpire(string playerArchetype = null)
        {
            RequireGameInstance(nameof(CreateUniverseAndPlayerEmpire));
            RequireStarterContentLoaded(nameof(CreateUniverseAndPlayerEmpire));

            var sw = new Stopwatch();

            EmpireManager.Clear();

            IEmpireData playerData = ResourceManager.MajorRaces[0];
            IEmpireData enemyData = ResourceManager.MajorRaces[1];
            if (playerArchetype != null)
            {
                playerData = ResourceManager.MajorRaces.FirstOrDefault(e => e.ArchetypeName.Contains(playerArchetype));
                if (playerData == null)
                    throw new Exception($"Could not find MajorRace archetype matching '{playerArchetype}'");
                enemyData = ResourceManager.MajorRaces.FirstOrDefault(e => e != playerData);
            }

            Universe = new UniverseScreen(2_000_000f);
            UState = Universe.UState;
            Player = UState.CreateEmpire(playerData, isPlayer:true);
            Enemy = UState.CreateEmpire(enemyData, isPlayer:false);
            
            Universe.viewState = UniverseScreen.UnivScreenState.PlanetView;
            Player.TestInitModifiers();
            Player.SetRelationsAsKnown(Enemy);
            Player.GetEmpireAI().DeclareWarOn(Enemy, WarType.BorderConflict);
            Empire.UpdateBilateralRelations(Player, Enemy);
            
            Log.Info($"CreateUniverseAndPlayerEmpire elapsed: {sw.Elapsed.TotalMilliseconds}ms");
        }

        public void CreateDeveloperSandboxUniverse(string playerPreference, int numOpponents, bool paused)
        {
            Universe = DeveloperUniverse.Create(playerPreference, numOpponents);
            Player = Universe.Player;
            Enemy  = EmpireManager.NonPlayerEmpires[0];
        }

        public void CreateCustomUniverse(UniverseGenerator.Params p)
        {
            Universe = new UniverseGenerator(p).Generate();
            Player = Universe.Player;
            Enemy = EmpireManager.NonPlayerEmpires[0];
        }

        public void DestroyUniverse()
        {
            Universe?.ExitScreen();
            Universe?.Dispose();
            Universe = null;
        }

        public void CreateThirdMajorEmpire()
        {
            ThirdMajor = EmpireManager.CreateEmpireFromEmpireData(UState, ResourceManager.MajorRaces[2], isPlayer:false);
            UState.AddEmpire(ThirdMajor);

            Player.SetRelationsAsKnown(ThirdMajor);
            Enemy.SetRelationsAsKnown(ThirdMajor);
            Empire.UpdateBilateralRelations(Player, ThirdMajor);
            Empire.UpdateBilateralRelations(Enemy, ThirdMajor);
        }

        public void CreateRebelFaction()
        {
            IEmpireData data = ResourceManager.MajorRaces.FirstOrDefault(e => e.Name == Player.data.Name);
            Faction = EmpireManager.CreateRebelsFromEmpireData(data, Player);
        }

        public void UnlockAllShipsFor(Empire empire)
        {
            foreach (string uid in ResourceManager.ShipTemplateIds)
                empire.ShipsWeCanBuild.Add(uid);
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

            Player.ShipsWeCanBuild.Clear();
            Enemy.ShipsWeCanBuild.Clear();
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

            var ship = new TestShip(UState, template, empire, position);
            if (!ship.HasModules)
                throw new Exception($"Failed to create ship modules: {shipName} (did you load modules?)");

            UState?.Objects.Add(ship);
            ship.Rotation = shipDirection.Normalized().ToRadians();
            ship.UpdateShipStatus(new FixedSimTime(0.01f)); // update module pos
            ship.UpdateModulePositions(new FixedSimTime(0.01f), true, forceUpdate: true);
            ship.SetSystem(null);
            Assert.IsTrue(ship.Active, "Spawned ship is Inactive! This is a bug in Status update!");

            return ship;
        }

        public SolarSystem CreateNewSolarSystemWithPlanet(Planet p)
        {
            var s = new SolarSystem(UState)
            {
                Sun = SunType.RandomBarrenSun()
            };
            AddPlanetToSolarSystem(s, p);
            return s;
        }

        Planet AddDummyPlanet()
        {
            var p = new Planet(UState.CreateId());
            CreateNewSolarSystemWithPlanet(p);
            return p;
        }

        public Planet AddDummyPlanet(float fertility, float minerals, float pop)
        {
            var p = new Planet(UState.CreateId(), fertility, minerals, pop);
            CreateNewSolarSystemWithPlanet(p);
            return p;
        }

        public Planet AddDummyPlanet(float fertility, float minerals, float pop, Vector2 pos, bool explored)
        {
            var p = new Planet(UState.CreateId(), fertility, minerals, pop) { Center = pos };
            var s = CreateNewSolarSystemWithPlanet(p);
            if (explored) s.SetExploredBy(Player);
            return p;
        }

        public Planet AddDummyPlanetToEmpire(Empire empire)
        {
            return AddDummyPlanetToEmpire(empire, 0, 0, 0);
        }

        public Planet AddDummyPlanetToEmpire(Empire empire, float fertility, float minerals, float maxPop)
        {
            var p = AddDummyPlanet(fertility, minerals, maxPop);
            empire.AddPlanet(p);
            p.Owner = empire;
            p.Type = ResourceManager.Planets.PlanetOrRandom(0);
            p.ParentSystem.OwnerList.Add(empire);
            return p;
        }

        public Planet AddHomeWorldToEmpire(Empire empire)
        {
            var p = AddDummyPlanet();
            p.GenerateNewHomeWorld(empire);
            return p;
        }

        public Planet AddHomeWorldToEmpire(Empire empire, Vector2 pos, bool explored = false)
        {
            var p = AddDummyPlanet(0, 0, 0, pos, explored);
            p.GenerateNewHomeWorld(empire);
            return p;
        }

        public void AddPlanetToSolarSystem(SolarSystem s, Planet p)
        {
            float distance = p.Center.Distance(s.Position);
            var r = new SolarSystem.Ring { Asteroids = false, OrbitalDistance = distance, planet = p };
            s.RingList.Add(r);
            s.PlanetList.Add(p);
            UState.AddSolarSystem(s);
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
        /// Update Universe.Objects simulation for `totalSimSeconds`
        /// </summary>
        public float RunObjectsSim(float totalSimSeconds)
        {
            // we run multiple iterations in order to allow the universe to properly simulate in fixed steps
            float time = 0f;
            for (; time < totalSimSeconds; time += TestSimStep.FixedTime)
            {
                UState.Objects.Update(TestSimStep);
            }
            return time;
        }

        public float RunObjectsSim(FixedSimTime totalTime)
        {
            return RunObjectsSim(totalTime.FixedTime);
        }
    }
}
