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
using Fasterflect;
using System.Linq;
using System.Threading;



namespace Ship_Game
{
	public sealed class Game1 : Game
	{
		public GraphicsDeviceManager Graphics;
		public static Game1 Instance;
		public ScreenManager ScreenManager;
		public WindowMode CurrentMode = WindowMode.Borderless;
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
			this.Graphics = new GraphicsDeviceManager(this)
			{
				MinimumPixelShaderProfile = ShaderProfile.PS_2_0,
				MinimumVertexShaderProfile = ShaderProfile. VS_2_0
			};
			string path = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
			Directory.CreateDirectory(string.Concat(path, "/StarDrive"));
			Directory.CreateDirectory(string.Concat(path, "/StarDrive/Saved Games"));
            Directory.CreateDirectory(string.Concat(path, "/StarDrive/Saved Races"));       // for saving custom races
            Directory.CreateDirectory(string.Concat(path, "/StarDrive/Saved Setups"));       // for saving new game setups
            Directory.CreateDirectory(string.Concat(path, "/StarDrive/Fleet Designs"));
			Directory.CreateDirectory(string.Concat(path, "/StarDrive/Saved Designs"));
			Directory.CreateDirectory(string.Concat(path, "/StarDrive/WIP"));
			Directory.CreateDirectory(string.Concat(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "/StarDrive/Saved Games/Headers"));
			Directory.CreateDirectory(string.Concat(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "/StarDrive/Saved Games/Fog Maps"));
			GlobalStats.Config = new Config();
            string asi = ConfigurationManager.AppSettings["AutoSaveFreq"];
            if (int.TryParse(asi, out int autosavefreq))
            {
                GlobalStats.AutoSaveFreq = autosavefreq;
            }
			string vol = ConfigurationManager.AppSettings["MusicVolume"];
            if (int.TryParse(vol, out int musicVol))
			{
				GlobalStats.Config.MusicVolume = (float)musicVol / 100f;
			}
			vol = ConfigurationManager.AppSettings["EffectsVolume"];
            if (int.TryParse(vol, out int fxVol))
			{
				GlobalStats.Config.EffectsVolume = fxVol / 100f;
			}
			GlobalStats.Config.Language = ConfigurationManager.AppSettings["Language"];
			if (GlobalStats.Config.Language != "English" && GlobalStats.Config.Language != "Spanish" && GlobalStats.Config.Language != "Polish" && GlobalStats.Config.Language != "German" && GlobalStats.Config.Language != "Russian" && GlobalStats.Config.Language != "French")
			{
				GlobalStats.Config.Language = "English";
			}
			GlobalStats.Config.RanOnce = ConfigurationManager.AppSettings["RanOnce"] != "false";
			GlobalStats.ForceFullSim   = ConfigurationManager.AppSettings["ForceFullSim"] != "false";
            if (int.TryParse(ConfigurationManager.AppSettings["WindowMode"], out int winmode))
			{
				GlobalStats.Config.WindowMode = winmode;
			}
			if (GlobalStats.Config.RanOnce)
			{
                if (int.TryParse(ConfigurationManager.AppSettings["XRES"], out int xres))
				{
					this.Graphics.PreferredBackBufferWidth = xres;
					GlobalStats.Config.XRES = xres;
				}
				if (int.TryParse(ConfigurationManager.AppSettings["YRES"], out int yres))
				{
					this.Graphics.PreferredBackBufferHeight = yres;
					GlobalStats.Config.YRES = yres;
				}
			}
			else
			{
				this.Graphics.PreferredBackBufferWidth = GraphicsAdapter.DefaultAdapter.CurrentDisplayMode.Width;
				this.Graphics.PreferredBackBufferHeight = GraphicsAdapter.DefaultAdapter.CurrentDisplayMode.Height;
				this.Graphics.IsFullScreen = true;
			}
			this.Graphics.PreferredDepthStencilFormat = DepthFormat.Depth24Stencil8;
			this.Graphics.PreferMultiSampling = true;
			this.Graphics.SynchronizeWithVerticalRetrace = true;
			this.Graphics.PreparingDeviceSettings += new EventHandler<PreparingDeviceSettingsEventArgs>(this.PrepareDeviceSettings);
			if (!GlobalStats.Config.RanOnce)
			{
				GlobalStats.Config.WindowMode = 0;
			}
			switch (GlobalStats.Config.WindowMode)
			{
				case 0:
				{
					this.SetWindowMode(Game1.WindowMode.Fullscreen, (!GlobalStats.Config.RanOnce ? GraphicsAdapter.DefaultAdapter.CurrentDisplayMode.Width : GlobalStats.Config.XRES), (!GlobalStats.Config.RanOnce ? GraphicsAdapter.DefaultAdapter.CurrentDisplayMode.Height : GlobalStats.Config.YRES));
					break;
				}
				case 1:
				{
					this.SetWindowMode(Game1.WindowMode.Windowed, GlobalStats.Config.XRES, GlobalStats.Config.YRES);
					break;
				}
				case 2:
				{
					this.SetWindowMode(Game1.WindowMode.Borderless, GlobalStats.Config.XRES, GlobalStats.Config.YRES);
					break;
				}
			}
			Bitmap cur = new Bitmap("Content/Cursors/Cursor.png", true);
			System.Drawing.Graphics.FromImage(cur);
			Cursor c = new Cursor(cur.GetHicon());
			Control.FromHandle(base.Window.Handle).Cursor = c;
			base.IsMouseVisible = true;
		}

        private void GameExiting(object sender, EventArgs e)
        {
            ScreenManager.ExitAll();
        }

        public void ApplySettings()
		{
			this.Graphics.ApplyChanges();
		}

		protected override void Draw(GameTime gameTime)
		{
			if (GraphicsDevice.GraphicsDeviceStatus == GraphicsDeviceStatus.Normal)
			{
				GraphicsDevice.Clear(Microsoft.Xna.Framework.Graphics.Color.Black);
				if (!SplashScreen.DisplayComplete)
				{
					ScreenManager.splashScreenGameComponent.Draw(gameTime);
				}
				ScreenManager.Draw(gameTime);
				base.Draw(gameTime);
			}
		}

		protected override void Initialize()
		{
			base.Window.Title = "StarDrive";
			base.Content.RootDirectory = "Content";
			this.ScreenManager = new ScreenManager(this, this.Graphics)
			{
				splashScreenGameComponent = new SplashScreenGameComponent(this, this.Graphics)
			};
			base.Components.Add(this.ScreenManager.splashScreenGameComponent);
			AudioManager.Initialize(this, "Content/Audio/ShipGameProject.xgs", "Content/Audio/Wave Bank.xwb", "Content/Audio/Sound Bank.xsb");
            
			Game1.Instance = this;
			base.Initialize();
		}

		protected override void LoadContent()
		{
			if (this.IsLoaded)
			{

				return;
            }

            this.ScreenManager.LoadContent();
			Fonts.LoadContent(base.Content);
			this.ScreenManager.AddScreen(new GameLoadingScreen());
			this.IsLoaded = true;
		}

		private void PrepareDeviceSettings(object sender, PreparingDeviceSettingsEventArgs e)
		{
			int quality = 0;
			GraphicsAdapter adapter = e.GraphicsDeviceInformation.Adapter;
			if (!adapter.CheckDeviceMultiSampleType(DeviceType.Hardware, adapter.CurrentDisplayMode.Format, false, MultiSampleType.TwoSamples, out quality))
			{
				e.GraphicsDeviceInformation.PresentationParameters.MultiSampleType = MultiSampleType.None;
				e.GraphicsDeviceInformation.PresentationParameters.MultiSampleQuality = 0;
			}
			else
			{
				e.GraphicsDeviceInformation.PresentationParameters.MultiSampleQuality = (quality == 1 ? 0 : 1);
                // added by gremlin video
                e.GraphicsDeviceInformation.PresentationParameters.MultiSampleType = MultiSampleType.FourSamples;
              
			}

            if (bool.Parse(ConfigurationManager.AppSettings["8XAntiAliasing"]) && adapter.CheckDeviceMultiSampleType(DeviceType.Hardware, adapter.CurrentDisplayMode.Format, false, MultiSampleType.EightSamples, out quality))
            {
                // even if a greater quality is returned, we only want quality 0
               e.GraphicsDeviceInformation.PresentationParameters.MultiSampleQuality = 0;
                e.GraphicsDeviceInformation.PresentationParameters.MultiSampleType = MultiSampleType.EightSamples;
                    
            }

			e.GraphicsDeviceInformation.PresentationParameters.RenderTargetUsage = RenderTargetUsage.PlatformContents;
		}

		public void SetWindowMode(Game1.WindowMode mode, int width, int height)
		{
            if (width <= 0 || height <= 0)
            {
                width = 800;
                height = 600;
            }
			Form form = (Form)Control.FromHandle(base.Window.Handle);
#if DEBUG
            if (mode == WindowMode.Fullscreen)
                mode = WindowMode.Borderless; 
#endif
            switch (mode)
			{
				case Game1.WindowMode.Fullscreen:
				{
					if (!this.Graphics.IsFullScreen)
					{
						this.Graphics.ToggleFullScreen();
					}
                    
					this.Graphics.PreferredBackBufferWidth = width;
					this.Graphics.PreferredBackBufferHeight = height;
					this.Graphics.ApplyChanges();
					this.CurrentMode = Game1.WindowMode.Fullscreen;
					GlobalStats.Config.WindowMode = 0;
					return;
				}
				case Game1.WindowMode.Windowed:
				{
					if (this.Graphics.IsFullScreen)
					{
						this.Graphics.ToggleFullScreen();
					}
					this.Graphics.PreferredBackBufferWidth = width;
					this.Graphics.PreferredBackBufferHeight = height;
					this.Graphics.ApplyChanges();
					form.WindowState = FormWindowState.Normal;
					form.FormBorderStyle = FormBorderStyle.Fixed3D;
					form.ClientSize = new Size(width, height);
					Size size = Screen.PrimaryScreen.WorkingArea.Size;
					int num = size.Width / 2 - width / 2;
					Size size1 = Screen.PrimaryScreen.WorkingArea.Size;
					form.Location = new System.Drawing.Point(num, size1.Height / 2 - height / 2);
					this.CurrentMode = Game1.WindowMode.Windowed;
					GlobalStats.Config.WindowMode = 1;
					return;
				}
				case Game1.WindowMode.Borderless:
				{
					if (this.Graphics.IsFullScreen)
					{
						this.Graphics.ToggleFullScreen();
					}
					this.Graphics.PreferredBackBufferWidth = width;
					this.Graphics.PreferredBackBufferHeight = height;
					this.Graphics.ApplyChanges();
					form.FormBorderStyle = FormBorderStyle.None;
					form.WindowState = FormWindowState.Normal;
					form.ClientSize = new Size(width, height);
					Size size2 = Screen.PrimaryScreen.WorkingArea.Size;
					int num1 = size2.Width / 2 - width / 2;
					Size size3 = Screen.PrimaryScreen.WorkingArea.Size;
					form.Location = new System.Drawing.Point(num1, size3.Height / 2 - height / 2);
					this.CurrentMode = Game1.WindowMode.Borderless;
					GlobalStats.Config.WindowMode = 2;
					return;
				}
				default:
				{
					return;
				}
			}
		}

		protected override void UnloadContent()
		{
            return;
		}

		protected override void Update(GameTime gameTime)
		{
			this.ScreenManager.Update(gameTime);
			base.Update(gameTime);
		}

		public enum WindowMode
		{
			Fullscreen,
			Windowed,
			Borderless
		}
	}
}