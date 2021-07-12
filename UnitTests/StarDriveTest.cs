using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Xna.Framework;
using Ship_Game;
using Ship_Game.Data;
using Ship_Game.Gameplay;
using Ship_Game.GameScreens.NewGame;
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
            ResourceManager.InitContentDir();
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
        public Empire ThirdMajor { get; private set; }
        public Empire Faction { get; private set; }

        public FixedSimTime TestSimStep { get; private set; } = new FixedSimTime(1f / 60f);
        public VariableFrameTime TestVarTime { get; private set; } = new VariableFrameTime(1f / 60f);

        public StarDriveTest()
        {
            GlobalStats.LoadConfig();
            Log.Initialize(enableSentry: false);
            Log.VerboseLogging = true;

            // This allows us to completely load UniverseScreen inside UnitTests
            GlobalStats.DrawStarfield = false;
            GlobalStats.DrawNebulas = false;
        }

        static void Cleanup()
        {
            Ship_Game.Parallel.ClearPool(); // Dispose all thread pool Threads
            Log.Close();
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
            if (Game != null)
                throw new InvalidOperationException("Game instance already created");

            // Try to collect all memory before we continue, otherwise we can run out of memory
            // in the unit tests because it doesn't collect memory by default
            GCSettings.LargeObjectHeapCompactionMode = GCLargeObjectHeapCompactionMode.CompactOnce;
            GC.Collect(GC.MaxGeneration, GCCollectionMode.Forced, true);

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
            DestroyUniverse();
            Game?.Dispose();
            Game = null;
            Cleanup();
        }

        void RequireGameInstance(string functionName)
        {
            if (Game == null)
                throw new Exception($"CreateGameInstance() must be called BEFORE {functionName}() !");
        }

        static void RequireStarterContentLoaded(string functionName)
        {
            if (ResourceManager.AllRaces.Count == 0)
                throw new Exception($"LoadStarterContent() or LoadStarterShips() must be called BEFORE {functionName}() !");
        }

        public void CreateUniverseAndPlayerEmpire()
        {
            RequireGameInstance(nameof(CreateUniverseAndPlayerEmpire));
            RequireStarterContentLoaded(nameof(CreateUniverseAndPlayerEmpire));

            var data = new UniverseData();
            Player = data.CreateEmpire(ResourceManager.MajorRaces[0], isPlayer:true);
            Enemy = data.CreateEmpire(ResourceManager.MajorRaces[1], isPlayer:false);
            Empire.Universe = Universe = new UniverseScreen(data, Player);
            Player.TestInitModifiers();

            Player.SetRelationsAsKnown(Enemy);
            Player.GetEmpireAI().DeclareWarOn(Enemy, WarType.BorderConflict);
            Empire.UpdateBilateralRelations(Player, Enemy);
        }

        public void CreateThirdMajorEmpire()
        {
            ThirdMajor = EmpireManager.CreateEmpireFromEmpireData(ResourceManager.MajorRaces[2], isPlayer:false);
            EmpireManager.Add(ThirdMajor);

            Player.SetRelationsAsKnown(ThirdMajor);
            Enemy.SetRelationsAsKnown(ThirdMajor);
            Empire.UpdateBilateralRelations(Player, ThirdMajor);
            Empire.UpdateBilateralRelations(Enemy, ThirdMajor);
        }

        public void CreateRebelFaction(bool atWarWithPlayer = true)
        {
            Faction = EmpireManager.CreateRebelsFromEmpireData(ResourceManager.MajorRaces[0], Player);
        }

        public void UnlockAllShipsFor(Empire empire)
        {
            foreach (string uid in ResourceManager.GetShipTemplateIds())
                empire.ShipsWeCanBuild.Add(uid);
        }
        
        public void UnlockAllTechsForShip(Empire empire, string shipName)
        {
            Ship ship = ResourceManager.GetShipTemplate(shipName);
            foreach (var tech in ship.shipData.TechsNeeded)
            {
                empire.UnlockTech(tech, TechUnlockType.Normal);
            }
            ShipDesignUtils.MarkDesignsUnlockable(new ProgressCounter());
        }

        public void CreateDeveloperSandboxUniverse(string playerPreference, int numOpponents, bool paused)
        {
            var data = DeveloperUniverse.Create(playerPreference, numOpponents);
            SetUniverse(new DeveloperUniverse(data, data.EmpireList.First, paused));
        }

        public void SetUniverse(UniverseScreen us)
        {
            Empire.Universe = Universe = us;
            Player = EmpireManager.Player;
            Enemy  = EmpireManager.NonPlayerEmpires[0];
        }

        public void DestroyUniverse()
        {
            Empire.Universe?.ExitScreen();
            Empire.Universe?.Dispose();
            Empire.Universe = Universe = null;
        }

        public void LoadGameContent(ResourceManager.TestOptions options = ResourceManager.TestOptions.None)
        {
            RequireGameInstance(nameof(LoadGameContent));
            ResourceManager.LoadContentForTesting(options);
        }

        public void LoadStarterShips(params string[] shipList)
        {
            LoadStarterShips(ResourceManager.TestOptions.None, shipList);
        }

        public void LoadStarterShips(ResourceManager.TestOptions options, params string[] shipList)
        {
            RequireGameInstance(nameof(LoadStarterShips));
            if (shipList == null)
                throw new NullReferenceException(nameof(shipList));
            ResourceManager.LoadStarterShipsForTesting(shipsList:shipList.Length == 0 ? null : shipList,
                                                       savedDesigns:null, options);
        }

        public void LoadStarterShips(string[] starterShips, string[] savedDesigns,
                                     ResourceManager.TestOptions options = ResourceManager.TestOptions.None)
        {
            RequireGameInstance(nameof(LoadStarterShips));
            ResourceManager.LoadStarterShipsForTesting(starterShips, savedDesigns, options);
        }

        public void LoadStarterShipVulcan(ResourceManager.TestOptions options = ResourceManager.TestOptions.None)
        {
            LoadStarterShips(options, "Vulcan Scout", "Rocket Scout", "Laserclaw");
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
        public Beam[] GetBeams(Ship ship)
        {
            return Universe.Objects.GetBeams(ship);
        }
    }
}
