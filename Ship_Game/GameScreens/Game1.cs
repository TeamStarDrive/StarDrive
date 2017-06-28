using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using SynapseGaming.LightingSystem.Rendering;
using System;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Windows.Forms;

namespace Ship_Game
{
    // This class is created only once during Program start
    public sealed class Game1 : Game
    {
        public GraphicsDeviceManager Graphics;
        public static Game1 Instance;
        public ScreenManager ScreenManager;
        public Viewport Viewport { get; private set; }
        public bool IsLoaded { get; private set; }

        public new GameContentManager Content { get; }
        public static GameContentManager GameContent => Instance.Content;

        public Game1()
        {
            // need to set base Content, to ensure proper content disposal
            base.Content = this.Content = new GameContentManager(Services, "Game");

            GlobalStats.LoadConfig();

        #if STEAM
            if (SteamManager.SteamInitialize())
            {
                SteamManager.RequestCurrentStats();
                if (SteamManager.SetAchievement("Thanks"))
                    SteamManager.SaveAllStatAndAchievementChanges();
            }
        #endif

            Exiting += GameExiting;

            Graphics = new GraphicsDeviceManager(this)
            {
                MinimumPixelShaderProfile  = ShaderProfile.PS_2_0,
                MinimumVertexShaderProfile = ShaderProfile.VS_2_0
            };
            string appData = Dir.ApplicationData;
            Directory.CreateDirectory(appData + "/StarDrive/Saved Games");
            Directory.CreateDirectory(appData + "/StarDrive/Saved Races");  // for saving custom races
            Directory.CreateDirectory(appData + "/StarDrive/Saved Setups"); // for saving new game setups
            Directory.CreateDirectory(appData + "/StarDrive/Fleet Designs");
            Directory.CreateDirectory(appData + "/StarDrive/Saved Designs");
            Directory.CreateDirectory(appData + "/StarDrive/WIP"); // huh????? @todo What's this for?
            Directory.CreateDirectory(appData + "/StarDrive/Saved Games/Headers");
            Directory.CreateDirectory(appData + "/StarDrive/Saved Games/Fog Maps");

            if (GlobalStats.IsFirstRun)
            {
                Graphics.PreferredBackBufferWidth  = GlobalStats.XRES;
                Graphics.PreferredBackBufferHeight = GlobalStats.YRES;
            }
            else
            {
                Graphics.PreferredBackBufferWidth  = GraphicsAdapter.DefaultAdapter.CurrentDisplayMode.Width;
                Graphics.PreferredBackBufferHeight = GraphicsAdapter.DefaultAdapter.CurrentDisplayMode.Height;
                Graphics.IsFullScreen = true;
            }
            Graphics.PreferredDepthStencilFormat = DepthFormat.Depth24Stencil8;
            Graphics.PreferMultiSampling = false; //true
            Graphics.SynchronizeWithVerticalRetrace = true;
            Graphics.PreparingDeviceSettings += PrepareDeviceSettings;

            int width  = GlobalStats.XRES;
            int height = GlobalStats.YRES;
            if (!GlobalStats.IsFirstRun)
            {
                width  = GraphicsAdapter.DefaultAdapter.CurrentDisplayMode.Width;
                height = GraphicsAdapter.DefaultAdapter.CurrentDisplayMode.Height;
            }
            SetWindowMode(GlobalStats.WindowMode, width, height);

            var cursor = new Bitmap("Content/Cursors/Cursor.png", true);
            System.Drawing.Graphics.FromImage(cursor);
            Control.FromHandle(Window.Handle).Cursor = new Cursor(cursor.GetHicon());
            IsMouseVisible = true;
        }

        private void GameExiting(object sender, EventArgs e)
        {
            ScreenManager.ExitAll();
        }

        public void ApplySettings()
        {
            Graphics.ApplyChanges();
            Viewport = GraphicsDevice.Viewport;
            ScreenManager?.UpdateViewports();
        }

        protected override void Draw(GameTime gameTime)
        {
            if (GraphicsDevice.GraphicsDeviceStatus != GraphicsDeviceStatus.Normal)
                return;

            GraphicsDevice.Clear(Microsoft.Xna.Framework.Graphics.Color.Black);
            ScreenManager.Draw(gameTime);
            base.Draw(gameTime);
        }

        protected override void Initialize()
        {
            Window.Title = "StarDrive";
            ScreenManager = new ScreenManager(this, Graphics);
            GameAudio.Initialize("Content/Audio/ShipGameProject.xgs", "Content/Audio/Wave Bank.xwb", "Content/Audio/Sound Bank.xsb");

            ResourceManager.ScreenManager = ScreenManager;

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

        private static void PrepareDeviceSettings(object sender, PreparingDeviceSettingsEventArgs e)
        {
            GraphicsAdapter a = e.GraphicsDeviceInformation.Adapter;
            PresentationParameters p = e.GraphicsDeviceInformation.PresentationParameters;

            MultiSampleType samples = (MultiSampleType)GlobalStats.AntiAlias;
            if (a.CheckDeviceMultiSampleType(DeviceType.Hardware, a.CurrentDisplayMode.Format, 
                                                   false, samples, out int quality))
            {
                p.MultiSampleQuality = (quality == 1 ? 0 : 1);
                p.MultiSampleType    = samples;
            }
            else
            {
                p.MultiSampleType    = MultiSampleType.None;
                p.MultiSampleQuality = 0;
            }

            e.GraphicsDeviceInformation.PresentationParameters.RenderTargetUsage = RenderTargetUsage.PlatformContents;
        }

        public void SetWindowMode(WindowMode mode, int width, int height)
        {
            if (width <= 0 || height <= 0)
            {
                width  = 800;
                height = 600;
            }
            var form = (Form)Control.FromHandle(Window.Handle);        
            if (Debugger.IsAttached && mode == WindowMode.Fullscreen)
                mode = WindowMode.Borderless;
            GlobalStats.WindowMode = mode;
            Graphics.PreferredBackBufferWidth  = width;
            Graphics.PreferredBackBufferHeight = height;
            switch (mode)
            {
                case WindowMode.Windowed:   form.FormBorderStyle = FormBorderStyle.Fixed3D; break;
                case WindowMode.Borderless: form.FormBorderStyle = FormBorderStyle.None;    break;
            }
            if (mode != WindowMode.Fullscreen && Graphics.IsFullScreen || 
                mode == WindowMode.Fullscreen && !Graphics.IsFullScreen)
            {
                Graphics.ToggleFullScreen();
            }

            ApplySettings();

            if (mode != WindowMode.Fullscreen)
            {
                form.WindowState = FormWindowState.Normal;
                form.ClientSize = new Size(width, height);

                // set form to the center of the primary screen
                var size = Screen.PrimaryScreen.Bounds.Size;
                form.Location = new System.Drawing.Point(size.Width / 2 - width / 2, size.Height / 2 - height / 2);
            }
        }

        protected override void UnloadContent()
        {
        }

        protected override void Update(GameTime gameTime)
        {
            GameAudio.Update();
            ScreenManager.Update(gameTime);
            base.Update(gameTime);
        }

        protected override void Dispose(bool disposing)
        {
            base.Dispose(disposing);
            GameAudio.Destroy();
            Instance = null;
            Log.Info("Game Instance Disposed");
        }
    }
}