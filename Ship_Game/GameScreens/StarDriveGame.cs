using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using SynapseGaming.LightingSystem.Core;
using System;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Runtime;
using System.Windows.Forms;
using Ship_Game.Audio;
using Color = Microsoft.Xna.Framework.Graphics.Color;
using Point = System.Drawing.Point;

namespace Ship_Game
{
    // This class is created only once during Program start
    public sealed class StarDriveGame : GameBase
    {
        public static StarDriveGame Instance;
        public bool IsLoaded  { get; private set; }
        public bool IsExiting { get; private set; }

        // Time elapsed between 2 frames
        // this can be used for rendering animations
        public float FrameDeltaTime { get; private set; }
        public GameTime GameTime;
        public float TotalElapsed => (float)GameTime.TotalGameTime.TotalSeconds;
        public int FrameId { get; private set; }

        public StarDriveGame()
        {
            // @note This will override and initialize global system settings
            GlobalStats.LoadConfig();

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

            var cursor = new Bitmap("Content/Cursors/Cursor.png", true);
            System.Drawing.Graphics.FromImage(cursor);
            Control.FromHandle(Window.Handle).Cursor = new Cursor(cursor.GetHicon());
            IsMouseVisible = true;
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

            ScreenManager.LoadContent();
            Fonts.LoadContent(Content);
            ScreenManager.AddScreen(new GameLoadingScreen());
            IsLoaded = true;
        }

        protected override void UnloadContent()
        {
        }

        protected override void Update(GameTime gameTime)
        {
            ++FrameId;
            GameTime = gameTime;
            
            FrameDeltaTime = (float)gameTime.ElapsedGameTime.TotalSeconds;
            if (FrameDeltaTime > 0.4f) // @note Probably we were loading something heavy
                FrameDeltaTime = 1f / 60f;

            GameAudio.Update();
            ScreenManager.Update(gameTime);
            base.Update(gameTime); // Update XNA components

            if (ScreenManager.NumScreens == 0)
                Instance.Exit();
        }


        protected override void Draw(GameTime gameTime)
        {
            if (GraphicsDevice.GraphicsDeviceStatus != GraphicsDeviceStatus.Normal)
                return;

            GraphicsDevice.Clear(Color.Black);
            ScreenManager.Draw();
            base.Draw(gameTime);
        }


        protected override void Dispose(bool disposing)
        {
            base.Dispose(disposing);
            GameAudio.Destroy();
            Instance = null;
            Log.Write("Exiting: Game Instance Disposed");
        }
    }
}