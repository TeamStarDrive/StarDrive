using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Xna.Framework;
using Ship_Game;
using Ship_Game.Data;
using Ship_Game.Gameplay;
using Ship_Game.Ships;
using UnitTests.UI;

namespace UnitTests
{
    /// <summary>
    /// Automatic setup for StarDrive unit tests
    /// </summary>
    public class StarDriveTest : IDisposable
    {
        public static string StarDriveAbsolutePath { get; private set; }

        static StarDriveTest()
        {
            AppDomain.CurrentDomain.ProcessExit += CurrentDomain_ProcessExit;
            
            SetGameDirectory();
            try
            {
                var xna2 = Assembly.LoadFile(
                    $"{StarDriveAbsolutePath}\\Microsoft.Xna.Framework.dll");
                Console.WriteLine($"XNA Path: {xna2.Location}");
            }
            catch (FileNotFoundException e)
            {
                Console.WriteLine($"XNA Load Failed: {e.Message}\n{e.FileName}\n{e.FusionLog}");
                throw;
            }
            catch (Exception e)
            {
                Console.WriteLine($"XNA Load Failed: {e.Message}\n");
                throw;
            }
        }

        static void CurrentDomain_ProcessExit(object sender, EventArgs e)
        {
            Cleanup();
        }

        public static void SetGameDirectory()
        {
            Directory.SetCurrentDirectory("../../../stardrive");
            StarDriveAbsolutePath = Directory.GetCurrentDirectory();
        }

        public TestGameDummy Game { get; private set; }
        public GameContentManager Content { get; private set; }
        public MockInputProvider MockInput { get; private set; }

        public UniverseScreen Universe { get; private set; }
        public Empire Player { get; private set; }
        public Empire Enemy { get; private set; }
        public Empire Faction { get; private set; }

        public FixedSimTime TestSimStep { get; private set; } = new FixedSimTime(1f / 60f);

        public StarDriveTest()
        {
            Log.Initialize();
            Log.VerboseLogging = true;
        }

        static void Cleanup()
        {
            Ship_Game.Parallel.ClearPool(); // Dispose all thread pool Threads
            Log.StopLogThread();
            Log.FlushAllLogs();
        }

        // @note: This is slow! It can take 500-1000ms
        // So don't create it if you don't need a valid GraphicsDevice
        // Cases where you need this:
        //  -- You need to create a new Universe
        //  -- You need to load textures
        //  -- You need to test any kind of GameScreen instance
        //  -- You want to test a Ship
        public void CreateGameInstance(int width=800, int height=600,
                                       bool show=false, bool mockInput=true)
        {
            var sw = Stopwatch.StartNew();
            Game = new TestGameDummy(new AutoResetEvent(false), width, height, show);
            Game.Create();
            Content = Game.Content;
            if (mockInput)
                Game.Manager.input.Provider = MockInput = new MockInputProvider();
            Log.Info($"CreateGameInstance elapsed: {sw.Elapsed.TotalMilliseconds}ms");
        }

        public void Dispose()
        {
            Empire.Universe?.ExitScreen();
            Game?.Dispose();
            Empire.Universe = Universe = null;
            Game = null;
            Cleanup();
        }

        void RequireGameInstance(string functionName)
        {
            if (Game == null)
                throw new Exception($"CreateGameInstance() must be called BEFORE {functionName}() !");

        }

        public void CreateUniverseAndPlayerEmpire(out Empire player)
        {
            RequireGameInstance(nameof(CreateUniverseAndPlayerEmpire));

            var data = new UniverseData();
            Player = player = data.CreateEmpire(ResourceManager.MajorRaces[0]);
            Player.isPlayer = true;
            Empire.Universe = Universe = new UniverseScreen(data, player);
            Universe.player = player;
            Enemy = EmpireManager.CreateRebelsFromEmpireData(ResourceManager.MajorRaces[0], Player);
            player.TestInitDifficultyModifiers();
        }

        public void LoadStarterShips(params string[] shipList)
        {
            RequireGameInstance(nameof(LoadStarterShips));
            ResourceManager.LoadStarterShipsForTesting(shipList.Length == 0 ? null : shipList);
        }

        public void LoadStarterShipVulcan()
        {
            LoadStarterShips(new[]
            {
                "Vulcan Scout",
                "Rocket Scout"
            });
        }
        
        public Ship SpawnShip(string shipName, Empire empire, Vector2 position, Vector2 shipDirection = default)
        {
            var target = Ship.CreateShipAtPoint(shipName, empire, position);
            target.Rotation = shipDirection.Normalized().ToRadians();
            target.UpdateShipStatus(new FixedSimTime(0.01f)); // update module pos
            target.UpdateModulePositions(new FixedSimTime(0.01f), true, forceUpdate: true);
            target.SetSystem(null);
            return target;
        }

        public void LoadPlanetContent()
        {
            RequireGameInstance(nameof(LoadPlanetContent));
            ResourceManager.LoadPlanetContentForTesting();
        }

        public void LoadTechContent()
        {
            RequireGameInstance(nameof(LoadPlanetContent));
            ResourceManager.LoadTechContentForTesting();
        }

        static void AddDummyPlanet(out Planet p)
        {
            p = new Planet();
            var s = new SolarSystem();
            s.PlanetList.Add(p);
            p.ParentSystem = s;
        }

        public static void AddDummyPlanet(float fertility, float minerals, float pop, out Planet p)
        {
            p = new Planet(fertility, minerals, pop);
            var s = new SolarSystem();
            s.PlanetList.Add(p);
            p.ParentSystem = s;
        }

        public static void AddDummyPlanetToEmpire(Empire empire) => 
            AddDummyPlanetToEmpire(empire,0, 0, 0);
        public static void AddDummyPlanetToEmpire(Empire empire, float fertility, float minerals, float maxPop)
        {
            AddDummyPlanet(fertility, minerals, maxPop, out Planet p);
            empire?.AddPlanet(p);
            p.Type = ResourceManager.PlanetOrRandom(0);
        }

        public static void AddHomeWorldToEmpire(Empire empire, out Planet p)
        {
            AddDummyPlanet(out p);
            p.GenerateNewHomeWorld(empire);
        }

        public Array<Projectile> GetProjectiles(Ship ship)
        {
            return Universe.Objects.GetProjectiles(ship);
        }
        public int GetProjectileCount(Ship ship)
        {
            return GetProjectiles(ship).Count;
        }
    }
}
