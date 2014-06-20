using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Audio;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;
using SynapseGaming.LightingSystem.Core;
using SynapseGaming.LightingSystem.Editor;
using SynapseGaming.LightingSystem.Rendering;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Reflection;

namespace Ship_Game
{
	public class ScreenManager
	{
		public List<GameScreen> screens = new List<GameScreen>();

		private List<GameScreen> screensToUpdate = new List<GameScreen>();

		private List<GameScreen> screensToDraw = new List<GameScreen>();

		public InputState input = new InputState();

		private IGraphicsDeviceService graphicsDeviceService;

		private ContentManager content;

		private Texture2D blankTexture;

		private Rectangle titleSafeArea;

		private bool traceEnabled;

		public LightingSystemManager lightingSystemManager;

		public LightingSystemEditor editor;

		public SceneState sceneState;

		public SceneEnvironment environment;

		public LightingSystemPreferences preferences;

		public SplashScreenGameComponent splashScreenGameComponent;

		public SceneInterface inter;

		public SceneInterface buffer1;

		public SceneInterface buffer2;

		public SceneInterface renderBuffer;

		public Cue Music;

		public AudioCategory musicCategory;

		public AudioCategory racialMusic;

		public AudioCategory combatMusic;

		public AudioCategory weaponsCategory;

		public Microsoft.Xna.Framework.Graphics.GraphicsDevice GraphicsDevice;

		public Microsoft.Xna.Framework.Graphics.SpriteBatch SpriteBatch;

		public ContentManager Content
		{
			get
			{
				return this.content;
			}
		}

		public Rectangle TitleSafeArea
		{
			get
			{
				return this.titleSafeArea;
			}
		}

		public bool TraceEnabled
		{
			get
			{
				return this.traceEnabled;
			}
			set
			{
				this.traceEnabled = value;
			}
		}

		public ScreenManager(Game game, GraphicsDeviceManager graphics)
		{
			this.content = new ContentManager(game.Services, "Content");
			this.GraphicsDevice = graphics.GraphicsDevice;
			this.graphicsDeviceService = (IGraphicsDeviceService)game.Services.GetService(typeof(IGraphicsDeviceService));
			if (this.graphicsDeviceService == null)
			{
				throw new InvalidOperationException("No graphics device service.");
			}
			this.lightingSystemManager = new LightingSystemManager(game.Services);
			this.sceneState = new SceneState();
			this.inter = new SceneInterface(graphics);
			this.inter.CreateDefaultManagers(false, false, true);
			this.editor = new LightingSystemEditor(game.Services, graphics, game)
			{
				UserHandledView = true
			};
			this.inter.AddManager(this.editor);
		}

		public void AddScreen(GameScreen screen)
		{
			foreach (GameScreen gs in this.screens)
			{
				if (!(gs is DiplomacyScreen))
				{
					continue;
				}
				return;
			}
			screen.ScreenManager = this;
			if (this.graphicsDeviceService != null && this.graphicsDeviceService.GraphicsDevice != null)
			{
				screen.LoadContent();
			}
			this.screens.Add(screen);
		}

		public void AddScreenNoLoad(GameScreen screen)
		{
			foreach (GameScreen gs in this.screens)
			{
				if (!(gs is DiplomacyScreen))
				{
					continue;
				}
				return;
			}
			screen.ScreenManager = this;
			this.screens.Add(screen);
		}

		public void Draw(GameTime gameTime)
		{
			this.screensToDraw.Clear();
			foreach (GameScreen screen in this.screens)
			{
				this.screensToDraw.Add(screen);
			}
			for (int i = 0; i < this.screensToDraw.Count; i++)
			{
				GameScreen screen = this.screensToDraw[i];
				if (screen.ScreenState != ScreenState.Hidden)
				{
					screen.Draw(gameTime);
				}
			}
		}

		public void DrawRectangle(Rectangle rectangle, Color color)
		{
			this.SpriteBatch.Begin();
			this.SpriteBatch.Draw(this.blankTexture, rectangle, color);
			this.SpriteBatch.End();
		}

		public void ExitAll()
		{
			for (int i = 0; i < this.screens.Count; i++)
			{
				this.screens[i].ExitScreen();
			}
		}

		public void FadeBackBufferToBlack(int alpha, Microsoft.Xna.Framework.Graphics.SpriteBatch spriteBatch)
		{
			Viewport viewport = this.GraphicsDevice.Viewport;
			spriteBatch.Draw(this.blankTexture, new Rectangle(0, 0, viewport.Width, viewport.Height), new Color(0, 0, 0, (byte)alpha));
		}

		public void FadeBackBufferToBlack(int alpha)
		{
			Viewport viewport = this.GraphicsDevice.Viewport;
			this.SpriteBatch.Begin();
			this.SpriteBatch.Draw(this.blankTexture, new Rectangle(0, 0, viewport.Width, viewport.Height), new Color(0, 0, 0, (byte)alpha));
			this.SpriteBatch.End();
		}

		public GameScreen[] GetScreens()
		{
			return this.screens.ToArray();
		}

		public void LoadContent()
		{
			if (AudioManager.getAudioEngine() != null)
			{
				this.musicCategory = AudioManager.getAudioEngine().GetCategory("Music");
				this.racialMusic = AudioManager.getAudioEngine().GetCategory("RacialMusic");
				this.combatMusic = AudioManager.getAudioEngine().GetCategory("CombatMusic");
				this.weaponsCategory = AudioManager.getAudioEngine().GetCategory("Weapons");
				this.weaponsCategory.SetVolume(0.5f);
			}
			this.SpriteBatch = new Microsoft.Xna.Framework.Graphics.SpriteBatch(this.GraphicsDevice);
			this.blankTexture = this.content.Load<Texture2D>("Textures/blank");
			foreach (GameScreen screen in this.screens)
			{
				screen.LoadContent();
			}
			float x = (float)this.GraphicsDevice.Viewport.X;
			Viewport viewport = this.GraphicsDevice.Viewport;
			int num = (int)Math.Floor((double)(x + (float)viewport.Width * 0.05f));
			float y = (float)this.GraphicsDevice.Viewport.Y;
			Viewport viewport1 = this.GraphicsDevice.Viewport;
			int num1 = (int)Math.Floor((double)(y + (float)viewport1.Height * 0.05f));
			Viewport viewport2 = this.GraphicsDevice.Viewport;
			int num2 = (int)Math.Floor((double)((float)viewport2.Width * 0.9f));
			Viewport viewport3 = this.GraphicsDevice.Viewport;
			this.titleSafeArea = new Rectangle(num, num1, num2, (int)Math.Floor((double)((float)viewport3.Height * 0.9f)));
		}

		public void RemoveScreen(GameScreen screen)
		{
			if (this.graphicsDeviceService != null && this.graphicsDeviceService.GraphicsDevice != null)
			{
				screen.UnloadContent();
			}
			this.screens.Remove(screen);
			this.screensToUpdate.Remove(screen);
		}

		public void SetupSunburn()
		{
		}

		private void TraceScreens()
		{
			List<string> screenNames = new List<string>();
			foreach (GameScreen screen in this.screens)
			{
				screenNames.Add(screen.GetType().Name);
			}
			Trace.WriteLine(string.Join(", ", screenNames.ToArray()));
		}

		protected void UnloadContent()
		{
			this.content.Unload();
			foreach (GameScreen screen in this.screens)
			{
				screen.UnloadContent();
			}
		}

		public void Update(GameTime gameTime)
		{
			this.input.Update(gameTime);
			this.screensToUpdate.Clear();
			foreach (GameScreen screen in this.screens)
			{
				this.screensToUpdate.Add(screen);
			}
			bool otherScreenHasFocus = !Game1.Instance.IsActive;
			bool coveredByOtherScreen = false;
			while (this.screensToUpdate.Count > 0)
			{
				GameScreen screen = this.screensToUpdate[this.screensToUpdate.Count - 1];
				this.screensToUpdate.RemoveAt(this.screensToUpdate.Count - 1);
				screen.Update(gameTime, otherScreenHasFocus, coveredByOtherScreen);
				if (screen.ScreenState != ScreenState.TransitionOn && screen.ScreenState != ScreenState.Active)
				{
					continue;
				}
				if (!otherScreenHasFocus)
				{
					screen.HandleInput(this.input);
					otherScreenHasFocus = true;
				}
				if (screen.IsPopup)
				{
					continue;
				}
				coveredByOtherScreen = true;
			}
			if (this.traceEnabled)
			{
				this.TraceScreens();
			}
		}
	}
}