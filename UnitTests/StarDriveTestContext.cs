﻿using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Microsoft.Xna.Framework;
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
            Log.Info($"CreateGameInstance elapsed: {s1.Elapsed.TotalMilliseconds}ms");

            var s2 = Stopwatch.StartNew();
            LoadStarterContent();
            Log.Info($"LoadStarterContent elapsed: {s2.Elapsed.TotalMilliseconds}ms");
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
            Log.Initialize(enableSentry: false);
            Log.VerboseLogging = true;

            // This allows us to completely load UniverseScreen inside UnitTests
            GlobalStats.DrawStarfield = false;
            GlobalStats.DrawNebulas = false;

            // @note: This is slow! It can take 500-1000ms
            //        Which is why we only do it ONCE
            Game = new TestGameDummy(new AutoResetEvent(false), 800, 800, show:false);
            Game.Create();
            Content = Game.Content;
            Game.Manager.input.Provider = MockInput = new MockInputProvider();
        }

        static void LoadStarterContent()
        {
            ResourceManager.LoadContentForTesting();
            ReloadStarterShips();
        }

        public static void ReloadStarterShips()
        {
            // some basic ships that we always use
            string[] starterShips = { "Vulcan Scout", "Rocket Scout", "Colony Ship", 
                                      "Small Transport", "Supply Shuttle", "Subspace Projector" };
            string[] savedDesigns = { "Prototype Frigate" };
            ResourceManager.LoadStarterShipsForTesting(starterShips, savedDesigns, clearAll: true);
        }

        public static void Cleanup()
        {
            Ship_Game.Parallel.ClearPool(); // Dispose all thread pool Threads
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
            
            Directory.SetCurrentDirectory("../../../stardrive");
            StarDriveAbsolutePath = Directory.GetCurrentDirectory();
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

            try
            {
                Thread.CurrentThread.Name = "StarDriveTest";
            }
            catch {}
        }

    }
}
