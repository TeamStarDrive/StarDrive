using System;
using System.IO;
using System.Runtime;
using Microsoft.Xna.Framework;
using SDUtils;
using Ship_Game.Audio;
using Ship_Game.GameScreens;
using Color = Microsoft.Xna.Framework.Color;
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
            if (SteamManager.Initialize())
            {
                SteamManager.RequestStats();
                SteamManager.AchievementUnlocked("Thanks");
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

            // TODO: enable this as an option in OptionsScreen
            IsFixedTimeStep = true;
        }

        public void SetSteamAchievement(string name)
        {
        #if STEAM
            if (SteamManager.IsInitialized)
            {
                SteamManager.AchievementUnlocked(name);
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

        // Verifies the Media Foundation backend is usable on this machine and that
        // VideoPlayer can actually deliver frames. Sets GlobalStats.VideoDisabled
        // when either check fails so GameLoadingScreen skips the splash entirely
        // and jumps straight to MainMenu instead of presenting a half-broken state.
        //
        // 1) Construction + Volume setter — catches missing MF codec stack
        //    (Win10/11 N/KN editions). IsLooped is known-unimplemented and
        //    intentionally not exercised; ScreenMediaPlayer just doesn't call it.
        // 2) Force-disabled below — MonoGame WindowsDX 3.8.0.1641 has a known
        //    VideoPlayer bug where Play() throws NullReferenceException and
        //    GetTexture() throws unconditionally (audio still decodes, but no
        //    frames reach the GPU; see project_phase2_backlog_runtime.md).
        //    The pin to 3.8.0.1641 is forced by net48 support; revisit when
        //    the framework target moves to net6+ (then bump to 3.8.1+ which
        //    fixes this) and remove the force-disable here.
        static void ProbeVideoBackend()
        {
            try
            {
                using var player = new Microsoft.Xna.Framework.Media.VideoPlayer();
                player.Volume = 0.5f;
            }
            catch (Exception ex)
            {
                GlobalStats.VideoDisabled = true;
                Log.Warning($"Media Foundation unavailable; videos disabled: {ex.GetType().Name}: {ex.Message}");
                return;
            }

            GlobalStats.VideoDisabled = true;
            Log.Info("VideoPlayer force-disabled (MonoGame WindowsDX 3.8.0.1641 GetTexture bug); splash and loading icon skipped.");
        }

        protected override void Initialize()
        {
            Instance = this;
            Window.Title = "StarDrive BlackBox";
            ResourceManager.InitContentDir();
            ScreenManager = new(this, Graphics);
            InitializeAudio();
            ApplyGraphics(GraphicsSettings.FromGlobalStats());
            ProbeVideoBackend();

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
            if (ScreenManager != null)
            {
                // This also unloads all screens
                // And also Unloads Sunburn lighting manager
                ResourceManager.UnloadGraphicsResources(ScreenManager);
            }
            IsLoaded = false;
            GraphicsDeviceWasReset = true;
        }

        protected override void Update(GameTime gameTime)
        {
            GameAudio.Update();
            UpdateGame(gameTime);

            if (IsLoaded && ScreenManager.NumScreens == 0)
            {
                Instance.Exit();
            }
        }

        protected override void Draw(GameTime gameTime)
        {
            if (IsDeviceGood)
            {
                ScreenManager.ClearScreen(Color.Black);
                ScreenManager.Draw();
                base.Draw(gameTime);
            }
        }

        protected override void Dispose(bool disposing)
        {
            base.Dispose(disposing);
            Instance = null;
            #if STEAM
                SteamManager.Shutdown();
            #endif
        }
    }
}