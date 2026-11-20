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

            // SteamManager is currently a stub (returns false / no-op).
            // Calls remain unconditional so when Steamworks.NET is wired back
            // (see migration-plan-phase2 §"Deferred Final Step") only
            // SteamManager.cs needs to change.
            if (SteamManager.Initialize())
            {
                SteamManager.RequestStats();
                SteamManager.AchievementUnlocked("Thanks");
            }

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
            if (SteamManager.IsInitialized)
            {
                SteamManager.AchievementUnlocked(name);
            }
            else
            { Log.Warning("Steam not initialized"); }
        }

        void GameExiting(object sender, EventArgs e)
        {
            IsExiting = true;
            FrameTimeLogger.Stop();
            ScreenManager.ExitAll(clear3DObjects: true);
            ResourceManager.WaitForExit();
        }

        // Verifies the Media Foundation backend is usable on this machine.
        // Catches missing MF codec stack (Win10/11 N/KN editions) by attempting
        // to construct VideoPlayer and set Volume — both should succeed on a
        // working install. Sets GlobalStats.VideoDisabled if either throws so
        // GameLoadingScreen skips the splash and jumps straight to MainMenu.
        //
        // Phase 2.6.A: dropped the unconditional force-disable that worked
        // around MonoGame WindowsDX 3.8.0.1641's broken VideoPlayer
        // (Play/GetTexture both threw NRE). MonoGame 3.8.1+ fixes both.
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
            }
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
            // CWD at runtime is game/, so step up one to land alongside the rest of phase4-logs.
            // Disabled — §4.1 baseline already captured. Re-enable for the next perf pass
            // (e.g., §4.4 or anywhere we need fresh frame traces). All Begin/End/Stop calls
            // are no-ops when Init wasn't called, so no other edits are needed.
            // FrameTimeLogger.Init("../x64Migration/phase4-logs/perf-baseline/frames.csv");

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
            FrameTimeLogger.BeginUpdate();
            GameAudio.Update();
            UpdateGame(gameTime);
            FrameTimeLogger.EndUpdate();

            if (IsLoaded && ScreenManager.NumScreens == 0)
            {
                Instance.Exit();
            }
        }

        protected override void Draw(GameTime gameTime)
        {
            if (IsDeviceGood)
            {
                FrameTimeLogger.BeginDraw();
                try
                {
                    DrawToScreen(GraphicsDevice);
                    ScreenManager.ClearScreen(Color.Black);
                    ScreenManager.Draw();
                    base.Draw(gameTime);
                    StretchScreenTarget();
                }
                finally
                {
                    // Defensive: several render-to-texture paths (Bloom, Distortion, ShadowMap,
                    // FogMap ping-pong, DrawBorders RT, LightsTarget, MainTarget) bind a render
                    // target without try/finally cleanup. If any of them throws or skips its
                    // SetRenderTarget(null), MonoGame crashes Present with
                    // "Cannot call Present when a render target is active." Restore the back
                    // buffer unconditionally as a safety net. Use Game.GraphicsDevice (live),
                    // not ScreenManager's cached field which can be a disposed device after
                    // reset. Catch swallows cleanup throws so the original exception (if any)
                    // propagates instead of being masked.
                    try { GraphicsDevice?.SetRenderTarget(null); }
                    catch { }
                }
                string topScreen = ScreenManager.NumScreens > 0
                    ? ScreenManager.Current?.GetType().Name ?? ""
                    : "";
                FrameTimeLogger.EndFrame(topScreen);
            }
        }

        protected override void Dispose(bool disposing)
        {
            base.Dispose(disposing);
            Instance = null;
            SteamManager.Shutdown();
        }
    }
}