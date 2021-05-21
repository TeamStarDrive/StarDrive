using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Drawing;
using System.IO;
using System.Runtime;
using System.Windows.Forms;
using Ship_Game.Audio;
using Color = Microsoft.Xna.Framework.Graphics.Color;

namespace Ship_Game
{
    // This class is created only once during Program start
    public sealed class StarDriveGame : GameBase
    {
        public static StarDriveGame Instance;
        public bool IsLoaded  { get; private set; }
        public bool IsExiting { get; private set; }
        bool GraphicsDeviceWasReset;

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
            Directory.CreateDirectory(appData + "/WIP"); // huh????? @todo What's this for? CG:unfinished designs
            Directory.CreateDirectory(appData + "/Saved Games/Headers");
            Directory.CreateDirectory(appData + "/Saved Games/Fog Maps");

            IsFixedTimeStep = true;
        }

        public void SetGameCursor()
        {
            FileInfo file = ResourceManager.GetModOrVanillaFile("Cursors/Cursor.png");
            if (file != null)
            {
                var cursor = new Bitmap(file.FullName, true);
                Form.Cursor = new Cursor(cursor.GetHicon());
            }
            else
            {
                Form.Cursor = Cursors.Default;
            }
            IsMouseVisible = true;
        }

        public void SetCinematicCursor()
        {
            FileInfo file = ResourceManager.GetModOrVanillaFile("Cursors/CinematicCursor.png");
            if (file != null)
            {
                var cursor = new Bitmap(file.FullName, true);
                Form.Cursor = new Cursor(cursor.GetHicon());
            }
            else
            {
                SetGameCursor();
            }
        }

        public void SetSteamAchievement(string name)
        {
            if (SteamManager.SteamInitialize())
            {
                if (SteamManager.SetAchievement(name))
                    SteamManager.SaveAllStatAndAchievementChanges();
            }
            else
            { Log.Warning("Steam not initialized"); }
        }

        void GameExiting(object sender, EventArgs e)
        {
            IsExiting = true;
            ScreenManager.ExitAll(clear3DObjects: true);
            ResourceManager.WaitForExit();
        }

        protected override void Initialize()
        {
            Window.Title = "StarDrive BlackBox";
            ScreenManager = new ScreenManager(this, Graphics);
            InitializeAudio();
            ApplyGraphics(GraphicsSettings.FromGlobalStats());

            Instance = this;
            base.Initialize();
        }

        protected override void LoadContent()
        {
            if (IsLoaded)
                return;
            
            ResourceManager.InitContentDir();
            SetGameCursor();

            // Quite rare, but brutal case for all graphic resource reload
            if (GraphicsDeviceWasReset)
            {
                Log.Warning("StarDriveGame GfxDevice Reset");
                GraphicsDeviceWasReset = false;
                ResourceManager.LoadGraphicsResources(ScreenManager);
            }
            
            ScreenManager.LoadContent();
            IsLoaded = true;

            if (ScreenManager.NumScreens == 0)
            {
                ScreenManager.AddScreenAndLoadContent(new GameLoadingScreen(showSplash: true, resetResources: false));
            }
        }

        // This is called when the graphics device has been Disposed
        protected override void UnloadContent()
        {
            Log.Warning("StarDriveGame UnloadContent");
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
                Log.Info("ScreenManager GameScreens+PendingScreens == 0, Exiting Game");
                Instance.Exit();
            }
        }

        protected override void Draw(float deltaTime)
        {
            if (GraphicsDevice.GraphicsDeviceStatus != GraphicsDeviceStatus.Normal)
                return;

            GraphicsDevice.Clear(Color.Black);
            ScreenManager.Draw();
            base.Draw(deltaTime);
        }

        protected override void Dispose(bool disposing)
        {
            base.Dispose(disposing);
            Instance = null;
        }
    }
}