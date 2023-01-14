using System;
using System.IO;
using System.Runtime;
using SDUtils;
using Ship_Game.Audio;
using Ship_Game.GameScreens;
using Color = Microsoft.Xna.Framework.Graphics.Color;
using Ship_Game.GameScreens.MainMenu;
using Ship_Game.Utils;

namespace Ship_Game
{
    // This class is created only once during Program start
    public sealed class StarDriveGame : GameBase
    {
        public static StarDriveGame Instance;
        public bool IsLoaded  { get; private set; }
        public bool IsExiting { get; private set; }
        bool GraphicsDeviceWasReset;

        public Func<bool> OnInitialize;

        public StarDriveGame()
        {
            // Configure and display the GC mode
            // LatencyMode is only available if ServerGC=False
            if (!GCSettings.IsServerGC)
            {
                // Batch : non-concurrent, block until all GC is done
                // Interactive : concurrent, most of the work is done in a background thread
                if (GCSettings.LatencyMode != GCLatencyMode.Batch)
                    GCSettings.LatencyMode = GCLatencyMode.Batch;
            }
            Log.Write(ConsoleColor.Yellow, $"User={Environment.UserName} NET={Environment.Version}");
            Log.Write(ConsoleColor.Yellow, $"GC Server={GCSettings.IsServerGC} LatencyMode={GCSettings.LatencyMode}");
            Log.Write(ConsoleColor.Yellow, $"PhysicalCores={Parallel.NumPhysicalCores} MaxParallelism={Parallel.MaxParallelism}");
            Log.Write(ConsoleColor.Yellow, $"GameDir={Directory.GetCurrentDirectory()}");

        #if STEAM
            if (SteamManager.SteamInitialize())
            {
                SteamManager.RequestCurrentStats();
                if (SteamManager.SetAchievement("Thanks"))
                    SteamManager.SaveAllStatAndAchievementChanges();
            }
        #endif

            Exiting += GameExiting;

            string appData = Dir.StarDriveAppData;
            Directory.CreateDirectory(appData + "/Saved Games");
            Directory.CreateDirectory(appData + "/Saved Races");  // for saving custom races
            Directory.CreateDirectory(appData + "/Saved Setups"); // for saving new game setups
            Directory.CreateDirectory(appData + "/Fleet Designs");
            Directory.CreateDirectory(appData + "/Saved Designs");
            Directory.CreateDirectory(appData + "/WIP"); // This is for unfinished Shipyard designs
            AutoPatcher.CleanupLegacyIncompatibleFiles();
            AutoPatcher.TryDeletePatchTemp();

            IsFixedTimeStep = true;
        }

        public void SetSteamAchievement(string name)
        {
        #if STEAM
            if (SteamManager.SteamInitialize())
            {
                if (SteamManager.SetAchievement(name))
                    SteamManager.SaveAllStatAndAchievementChanges();
            }
            else
            { Log.Warning("Steam not initialized"); }
        #endif
        }

        void GameExiting(object sender, EventArgs e)
        {
            IsExiting = true;
            ScreenManager.ExitAll(clear3DObjects: true);
            ResourceManager.WaitForExit();
        }

        protected override void Initialize()
        {
            Instance = this;
            Window.Title = "StarDrive BlackBox";
            ResourceManager.InitContentDir();
            ScreenManager = new(this, Graphics);
            InitializeAudio();
            ApplyGraphics(GraphicsSettings.FromGlobalStats());

            // run initialization handler which is able to cancel and exit the game
            if (OnInitialize != null && OnInitialize() == false)
            {
                Instance.Exit();
                return;
            }
            base.Initialize();
        }

        protected override void LoadContent()
        {
            if (IsLoaded)
                return;

            GameCursors.Initialize(this, GlobalStats.UseSoftwareCursor);

            // Quite rare, but brutal case for all graphic resource reload
            bool wasReset = GraphicsDeviceWasReset;
            if (wasReset)
            {
                Log.Warning("StarDriveGame GfxDevice Reset");
                GraphicsDeviceWasReset = false;
                ResourceManager.LoadGraphicsResources(ScreenManager);
            }

            ScreenManager.LoadContent(deviceWasReset:wasReset);
            IsLoaded = true;

            if (ScreenManager.NumScreens == 0)
            {
                ScreenManager.AddScreenAndLoadContent(new GameLoadingScreen(showSplash: true, resetResources: false));
            }
        }

        // This is called when the graphics device has been Disposed
        protected override void UnloadContent()
        {
            Log.Write("StarDriveGame UnloadContent");
            // This also unloads all screens
            if (ScreenManager != null)
                ResourceManager.UnloadGraphicsResources(ScreenManager);
            IsLoaded = false;
            GraphicsDeviceWasReset = true;
        }

        protected override void Update(float deltaTime)
        {
            GameAudio.Update();
            UpdateGame(deltaTime);

            if (IsLoaded && ScreenManager.NumScreens == 0)
            {
                Instance.Exit();
            }
        }

        protected override void Draw(float deltaTime)
        {
            if (IsDeviceGood)
            {
                GraphicsDevice.Clear(Color.Black);
                ScreenManager.Draw();
                base.Draw(deltaTime);
            }
        }

        protected override void Dispose(bool disposing)
        {
            base.Dispose(disposing);
            Instance = null;
        }
    }
}