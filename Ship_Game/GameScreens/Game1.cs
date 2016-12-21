#if DEBUG
#define UNSAFE
#endif
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;
using SynapseGaming.LightingSystem.Rendering;
using System;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.Configuration;
using System.Drawing;
using System.IO;
using System.Windows.Forms;
using System.Reflection;
using System.Linq;

namespace Ship_Game
{
    // This class is created only once during Program start
	public sealed class Game1 : Game
	{
		public GraphicsDeviceManager Graphics;
		public static Game1 Instance;
		public ScreenManager ScreenManager;
		public bool IsLoaded;

		public Game1()
		{
        #if STEAM
            if (SteamManager.SteamInitialize())
			{
                SteamManager.RequestCurrentStats();
                if (SteamManager.SetAchievement("Thanks"))
                    SteamManager.SaveAllStatAndAchievementChanges();
			}
        #endif

            Exiting += GameExiting;

        #if UNSAFE && DEBUG
            MethodUtil.ReplaceMethod(typeof(DevekSplash).GetMethod("Update2"), typeof(SplashScreen).GetMethod("Update"));
            foreach (var method in from type in Assembly.GetAssembly(typeof(SplashScreen)).GetTypes() where type.Name == "a" select type.GetMethods(BindingFlags.Static | BindingFlags.NonPublic) into methods from method in methods where method.Name == "k" select method)
            {
                MethodUtil.ReplaceMethod(typeof(DevekSplash).GetMethod("k2", BindingFlags.Static | BindingFlags.Public), method);
            }
        #endif

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
			Directory.CreateDirectory(appData + "/StarDrive/WIP");
			Directory.CreateDirectory(appData + "/StarDrive/Saved Games/Headers");
			Directory.CreateDirectory(appData + "/StarDrive/Saved Games/Fog Maps");

			if (GlobalStats.RanOnce)
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
			Graphics.PreferMultiSampling = true;
			Graphics.SynchronizeWithVerticalRetrace = true;
			Graphics.PreparingDeviceSettings += PrepareDeviceSettings;

            int width  = GlobalStats.XRES;
            int height = GlobalStats.YRES;
            if (!GlobalStats.RanOnce)
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
		}

		protected override void Draw(GameTime gameTime)
		{
		    if (GraphicsDevice.GraphicsDeviceStatus != GraphicsDeviceStatus.Normal)
                return;

		    GraphicsDevice.Clear(Microsoft.Xna.Framework.Graphics.Color.Black);
		    if (!SplashScreen.DisplayComplete)
		    {
		        ScreenManager.splashScreenGameComponent.Draw(gameTime);
		    }
		    ScreenManager.Draw(gameTime);
		    base.Draw(gameTime);
		}

		protected override void Initialize()
		{
			Window.Title = "StarDrive";
			Content.RootDirectory = "Content";
			ScreenManager = new ScreenManager(this, Graphics)
			{
				splashScreenGameComponent = new SplashScreenGameComponent(this, Graphics)
			};
			Components.Add(ScreenManager.splashScreenGameComponent);
			AudioManager.Initialize(this, "Content/Audio/ShipGameProject.xgs", "Content/Audio/Wave Bank.xwb", "Content/Audio/Sound Bank.xsb");
            
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

		private void PrepareDeviceSettings(object sender, PreparingDeviceSettingsEventArgs e)
		{
            GraphicsAdapter a = e.GraphicsDeviceInformation.Adapter;
            var p = e.GraphicsDeviceInformation.PresentationParameters;

			if (a.CheckDeviceMultiSampleType(DeviceType.Hardware, a.CurrentDisplayMode.Format, 
                                                   false, MultiSampleType.TwoSamples, out int quality))
			{
                p.MultiSampleQuality = (quality == 1 ? 0 : 1);
                p.MultiSampleType    = MultiSampleType.FourSamples;
			}
			else
			{
                p.MultiSampleType    = MultiSampleType.None;
                p.MultiSampleQuality = 0;
            }

            if (GlobalStats.AntiAlias8XOverride && a.CheckDeviceMultiSampleType(DeviceType.Hardware, a.CurrentDisplayMode.Format, false, MultiSampleType.EightSamples, out quality))
            {
                // even if a greater quality is returned, we only want quality 0
                p.MultiSampleQuality = 0;
                p.MultiSampleType = MultiSampleType.EightSamples;
                    
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
			Form form = (Form)Control.FromHandle(Window.Handle);
        #if DEBUG
            if (mode == WindowMode.Fullscreen)
                mode = WindowMode.Borderless;
        #endif
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
            Graphics.ApplyChanges();

            if (mode != WindowMode.Fullscreen)
            {
                form.WindowState = FormWindowState.Normal;
                form.ClientSize = new Size(width, height);

                // set form to the center of the primary screen
                Size size = Screen.PrimaryScreen.Bounds.Size;
                form.Location = new System.Drawing.Point(size.Width / 2 - width / 2, size.Height / 2 - height / 2);
            }
		}

		protected override void UnloadContent()
		{
		}

		protected override void Update(GameTime gameTime)
		{
			ScreenManager.Update(gameTime);
			base.Update(gameTime);
		}
	}
}