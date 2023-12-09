using System;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using System.Threading;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Ship_Game;
using Ship_Game.Data;
using UnitTests.UI;

namespace UnitTests
{
    /// <summary>
    /// Shared global state between all StarDrive tests
    ///
    /// This sets up the shared Game instance which is the most expensive part of game tests
    /// </summary>
    [TestClass] // -- required for AssemblyInitialize to be detected
    public static class StarDriveTestContext
    {
        public static TestGameDummy Game { get; private set; }
        public static GameContentManager Content { get; private set; }
        public static MockInputProvider MockInput { get; private set; }
        
        public static string StarDriveAbsolutePath { get; private set; }

        [AssemblyInitialize]
        public static void InitializeStarDriveTests(TestContext context)
        {
            ConfigureAssembly();

            var s1 = Stopwatch.StartNew();
            CreateGameInstance();
            Log.Info($"CreateGameInstance elapsed: {s1.Elapsed.TotalMilliseconds:0.##}ms");

            var s2 = Stopwatch.StartNew();
            LoadStarterContent();
            Log.Info($"LoadStarterContent elapsed: {s2.Elapsed.TotalMilliseconds:0.##}ms");
        }

        [AssemblyCleanup]
        public static void TeardownStarDriveTests()
        {
            Game?.Dispose();
            Game = null;
            Cleanup();
        }
        
        static void CreateGameInstance()
        {
            GlobalStats.LoadConfig();
            Log.Initialize(enableSentry: false, showHeader: false);
            Log.VerboseLogging = true;

            // This allows us to completely load UniverseScreen inside UnitTests
            GlobalStats.DrawStarfield = false;
            GlobalStats.DrawNebulas = false;

            // @note: This is slow! It can take 500-1000ms
            //        Which is why we only do it ONCE
            Game = new TestGameDummy(new AutoResetEvent(false), 1024, 1024, show:false);
            Game.Create();
            Content = Game.Content;
            Game.Manager.input.Provider = MockInput = new MockInputProvider();
        }

        // loads all testing content
        public static void LoadStarterContent()
        {
            GlobalStats.IsUnitTest = true;
            ResourceManager.LoadContentForTesting();
            ReloadStarterShips();
        }

        public static void ReloadStarterShips()
        {
            // some basic ships that we always use
            string[] designs = { "TEST_Vulcan Scout", "Vulcan Scout", "Rocket Scout", "Vingscout",
                                 "Fang Strafer", "Terran-Prototype", "Colony Ship",
                                 "Small Transport", "Supply Shuttle", "Subspace Projector", "Basic Research Station",
                                 "Terran Constructor", "Wisp Scout", "Basic Mining Station"};
            ResourceManager.LoadStarterShipsForTesting(designs, clearAll: true);
        }

        public static void Cleanup()
        {
            Parallel.ClearPool(); // Dispose all thread pool Threads
            Log.Close();
        }

        public static void EnableMockInput(bool enabled)
        {
            if (enabled)
                Game.Manager.input.Provider = MockInput = new MockInputProvider();
            else
                Game.Manager.input.Provider = new DefaultInputProvider();
        }

        static void ConfigureAssembly()
        {
            AppDomain.CurrentDomain.ProcessExit += (sender, e) => Cleanup();
            
            Directory.SetCurrentDirectory("../../../game");
            StarDriveAbsolutePath = Directory.GetCurrentDirectory();
            ResourceManager.InitContentDir();
            try
            {
                var xnaFramework = Assembly.LoadFile(
                    $"{StarDriveAbsolutePath}\\Microsoft.Xna.Framework.dll");
                Console.WriteLine($"XNAFramework Path: {xnaFramework.Location}");

                var xnAnimation = Assembly.LoadFile(
                    $"{StarDriveAbsolutePath}\\XNAnimation.dll");
                Console.WriteLine($"XNAnimation Path: {xnAnimation.Location}");
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

            try
            {
                Thread.CurrentThread.Name = "StarDriveTest";
            }
            catch {}
        }

    }
}
