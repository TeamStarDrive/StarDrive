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
	public class Game1 : Game
	{
		public GraphicsDeviceManager graphics;

		public static Game1 Instance;

		public ScreenManager screenManager;

		public bool IsLoaded;

		public Game1.WindowMode CurrentMode = Game1.WindowMode.Borderless;

		public Game1()
		{
			/*if (SteamManager.SteamInitialize())
			{
				SteamManager.RequestCurrentStats();
				if (SteamManager.SetAchievement("Thanks"))
				{
					SteamManager.SaveAllStatAndAchievementChanges();
				}
			}*/
            MethodUtil.ReplaceMethod(typeof(DevekSplash).GetMethod("Update2"), typeof(SplashScreen).GetMethod("Update"));
            foreach (var method in from type in Assembly.GetAssembly(typeof(SplashScreen)).GetTypes() where type.Name == "a" select type.GetMethods(BindingFlags.Static | BindingFlags.NonPublic) into methods from method in methods where method.Name == "k" select method)
            {
                MethodUtil.ReplaceMethod(typeof(DevekSplash).GetMethod("k2", BindingFlags.Static | BindingFlags.Public), method);
            }
			this.graphics = new GraphicsDeviceManager(this)
			{
				MinimumPixelShaderProfile = ShaderProfile.PS_2_0,
				MinimumVertexShaderProfile = ShaderProfile.VS_2_0
			};
			string path = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
			Directory.CreateDirectory(string.Concat(path, "/StarDrive"));
			Directory.CreateDirectory(string.Concat(path, "/StarDrive/Saved Games"));
			Directory.CreateDirectory(string.Concat(path, "/StarDrive/Fleet Designs"));
			Directory.CreateDirectory(string.Concat(path, "/StarDrive/Saved Designs"));
			Directory.CreateDirectory(string.Concat(path, "/StarDrive/WIP"));
			Directory.CreateDirectory(string.Concat(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "/StarDrive/Saved Games/Headers"));
			Directory.CreateDirectory(string.Concat(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "/StarDrive/Saved Games/Fog Maps"));
			GlobalStats.Config = new Config();
            string asi = ConfigurationSettings.AppSettings["AutoSaveInterval"];
            int autosaveinterval;
            if (int.TryParse(asi, out autosaveinterval))
            {
                GlobalStats.Config.AutoSaveInterval = (float)autosaveinterval;
            }
			string vol = ConfigurationSettings.AppSettings["MusicVolume"];
			int musicVol = 100;
			if (int.TryParse(vol, out musicVol))
			{
				GlobalStats.Config.MusicVolume = (float)musicVol / 100f;
			}
			vol = ConfigurationSettings.AppSettings["EffectsVolume"];
			int fxVol = 100;
			if (int.TryParse(vol, out fxVol))
			{
				GlobalStats.Config.EffectsVolume = (float)fxVol / 100f;
			}
			GlobalStats.Config.Language = ConfigurationSettings.AppSettings["Language"];
			if (GlobalStats.Config.Language != "English" && GlobalStats.Config.Language != "Spanish" && GlobalStats.Config.Language != "Polish" && GlobalStats.Config.Language != "German" && GlobalStats.Config.Language != "Russian" && GlobalStats.Config.Language != "French")
			{
				GlobalStats.Config.Language = "English";
			}
			GlobalStats.Config.RanOnce = (ConfigurationSettings.AppSettings["RanOnce"] == "false" ? false : true);
			GlobalStats.ForceFullSim = (ConfigurationSettings.AppSettings["ForceFullSim"] == "false" ? false : true);
			int winmode = 0;
			if (int.TryParse(ConfigurationSettings.AppSettings["WindowMode"], out winmode))
			{
				GlobalStats.Config.WindowMode = winmode;
			}
			if (GlobalStats.Config.RanOnce)
			{
				int xres = 1280;
				int yres = 720;
				if (int.TryParse(ConfigurationSettings.AppSettings["XRES"], out xres))
				{
					this.graphics.PreferredBackBufferWidth = xres;
					GlobalStats.Config.XRES = xres;
				}
				if (int.TryParse(ConfigurationSettings.AppSettings["YRES"], out yres))
				{
					this.graphics.PreferredBackBufferHeight = yres;
					GlobalStats.Config.YRES = yres;
				}
			}
			else
			{
				this.graphics.PreferredBackBufferWidth = GraphicsAdapter.DefaultAdapter.CurrentDisplayMode.Width;
				this.graphics.PreferredBackBufferHeight = GraphicsAdapter.DefaultAdapter.CurrentDisplayMode.Height;
				this.graphics.IsFullScreen = true;
			}
			this.graphics.PreferredDepthStencilFormat = DepthFormat.Depth24Stencil8;
			this.graphics.PreferMultiSampling = true;
			this.graphics.SynchronizeWithVerticalRetrace = true;
			this.graphics.PreparingDeviceSettings += new EventHandler<PreparingDeviceSettingsEventArgs>(this.PrepareDeviceSettings);
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
			Graphics.FromImage(cur);
			Cursor c = new Cursor(cur.GetHicon());
			Control.FromHandle(base.Window.Handle).Cursor = c;
			base.IsMouseVisible = true;
		}

		public void ApplySettings()
		{
			this.graphics.ApplyChanges();
		}

		protected override void Draw(GameTime gameTime)
		{
			if (base.GraphicsDevice.GraphicsDeviceStatus == GraphicsDeviceStatus.Normal)
			{
				base.GraphicsDevice.Clear(Microsoft.Xna.Framework.Graphics.Color.Black);
				if (!SplashScreen.DisplayComplete)
				{
					this.screenManager.splashScreenGameComponent.Draw(gameTime);
				}
				this.screenManager.Draw(gameTime);
				base.Draw(gameTime);
			}
		}

		protected override void Initialize()
		{
			base.Window.Title = "StarDrive";
			base.Content.RootDirectory = "Content";
			this.screenManager = new ScreenManager(this, this.graphics)
			{
				splashScreenGameComponent = new SplashScreenGameComponent(this, this.graphics)
			};
			base.Components.Add(this.screenManager.splashScreenGameComponent);
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
            //MethodUtil.ReplaceMethod(typeof(DevekSplash).GetMethod("Update2"), typeof(SplashScreen).GetMethod("Update"));
            //foreach (var method in from type in Assembly.GetAssembly(typeof(SplashScreen)).GetTypes() where type.Name == "a" select type.GetMethods(BindingFlags.Static | BindingFlags.NonPublic) into methods from method in methods where method.Name == "k" select method)
            //{
            //    MethodUtil.ReplaceMethod(typeof(DevekSplash).GetMethod("k2", BindingFlags.Static | BindingFlags.Public), method);
            //}
			this.screenManager.LoadContent();
			Fonts.LoadContent(base.Content);
			this.screenManager.AddScreen(new GameLoadingScreen());
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
				e.GraphicsDeviceInformation.PresentationParameters.MultiSampleType = MultiSampleType.TwoSamples;
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
            if (mode == WindowMode.Fullscreen)
                mode = WindowMode.Borderless;
            switch (mode)
			{
				case Game1.WindowMode.Fullscreen:
				{
					if (!this.graphics.IsFullScreen)
					{
						this.graphics.ToggleFullScreen();
					}
                    
					this.graphics.PreferredBackBufferWidth = width;
					this.graphics.PreferredBackBufferHeight = height;
					this.graphics.ApplyChanges();
					this.CurrentMode = Game1.WindowMode.Fullscreen;
					GlobalStats.Config.WindowMode = 0;
					return;
				}
				case Game1.WindowMode.Windowed:
				{
					if (this.graphics.IsFullScreen)
					{
						this.graphics.ToggleFullScreen();
					}
					this.graphics.PreferredBackBufferWidth = width;
					this.graphics.PreferredBackBufferHeight = height;
					this.graphics.ApplyChanges();
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
					if (this.graphics.IsFullScreen)
					{
						this.graphics.ToggleFullScreen();
					}
					this.graphics.PreferredBackBufferWidth = width;
					this.graphics.PreferredBackBufferHeight = height;
					this.graphics.ApplyChanges();
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
		}

		protected override void Update(GameTime gameTime)
		{
			this.screenManager.Update(gameTime);
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