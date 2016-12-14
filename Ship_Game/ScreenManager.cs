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
using System.Threading.Tasks;

namespace Ship_Game
{
	public sealed class ScreenManager : IDisposable
	{
		public List<GameScreen> screens = new List<GameScreen>();

		private List<GameScreen> screensToUpdate = new List<GameScreen>();

		private List<GameScreen> screensToDraw = new List<GameScreen>();

		public InputState input = new InputState();

		private IGraphicsDeviceService graphicsDeviceService;

	    private Texture2D blankTexture;

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

        public AudioCategory defaultCategory;
        public AudioCategory GlobalCategory;

		public Microsoft.Xna.Framework.Graphics.GraphicsDevice GraphicsDevice;

		public Microsoft.Xna.Framework.Graphics.SpriteBatch SpriteBatch;

        //adding for thread safe Dispose because class uses unmanaged resources 
        private bool disposed;

        public float exitScreenTimer = 0;

		public ContentManager Content { get; private set; }
	    public Rectangle TitleSafeArea { get; private set; }
	    public bool TraceEnabled { get; set; }

	    public ScreenManager(Game game, GraphicsDeviceManager graphics)
		{
			this.Content = new ContentManager(game.Services, "Content");
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
            foreach (GameScreen gs in screens)
			{
				if (gs is DiplomacyScreen)
				    return;
			}
			screen.ScreenManager = this;
			if (graphicsDeviceService?.GraphicsDevice != null)
				screen.LoadContent();
			screens.Add(screen);

		}

		public void AddScreenNoLoad(GameScreen screen)
		{
			foreach (GameScreen gs in screens)
			{
				if (gs is DiplomacyScreen)
			    	return;
			}
			screen.ScreenManager = this;
			screens.Add(screen);
		}

		public void Draw(GameTime gameTime)
		{
			screensToDraw.Clear();
			foreach (GameScreen screen in screens)
				screensToDraw.Add(screen);

			foreach (GameScreen screen in screensToDraw)
			{
			    if (screen.ScreenState != ScreenState.Hidden)
			        screen.Draw(gameTime);
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
		    foreach (GameScreen screen in screens)
		        screen.ExitScreen();
		}

		public void FadeBackBufferToBlack(int alpha, Microsoft.Xna.Framework.Graphics.SpriteBatch spriteBatch)
		{
			Viewport viewport = GraphicsDevice.Viewport;
			spriteBatch.Draw(blankTexture, new Rectangle(0, 0, viewport.Width, viewport.Height), new Color(0, 0, 0, (byte)alpha));
		}

		public void FadeBackBufferToBlack(int alpha)
		{
			Viewport viewport = GraphicsDevice.Viewport;
			SpriteBatch.Begin();
			SpriteBatch.Draw(blankTexture, new Rectangle(0, 0, viewport.Width, viewport.Height), new Color(0, 0, 0, (byte)alpha));
			SpriteBatch.End();
		}

		public GameScreen[] GetScreens()
		{
			return this.screens.ToArray();
		}

		public void LoadContent()
		{
			if (AudioManager.AudioEngine!= null)
			{
				this.musicCategory = AudioManager.AudioEngine.GetCategory("Music");
				this.racialMusic = AudioManager.AudioEngine.GetCategory("RacialMusic");
				this.combatMusic = AudioManager.AudioEngine.GetCategory("CombatMusic");
				this.weaponsCategory = AudioManager.AudioEngine.GetCategory("Weapons");
				this.weaponsCategory.SetVolume(0.5f);
                this.defaultCategory = AudioManager.AudioEngine.GetCategory("Default");
                this.GlobalCategory = AudioManager.AudioEngine.GetCategory("Global");
			}
			this.SpriteBatch = new Microsoft.Xna.Framework.Graphics.SpriteBatch(this.GraphicsDevice);
			this.blankTexture = this.Content.Load<Texture2D>("Textures/blank");
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
			this.TitleSafeArea = new Rectangle(num, num1, num2, (int)Math.Floor((double)((float)viewport3.Height * 0.9f)));
		}

		public void RemoveScreen(GameScreen screen)
		{
			if (this.graphicsDeviceService != null && this.graphicsDeviceService.GraphicsDevice != null)
			{
				screen.UnloadContent();
			}
			this.screens.Remove(screen);
			this.screensToUpdate.Remove(screen);
            this.exitScreenTimer = .025f;
            
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

        private void UnloadContent()
		{
			this.Content.Unload();
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
			if (this.TraceEnabled)
			{
				this.TraceScreens();
			}
		}
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        ~ScreenManager() { Dispose(false); }

        private void Dispose(bool disposing)
        {
            if (!disposed)
            {
                if (disposing)
                {
                    if (this.Content != null)
                        this.Content.Dispose();
                    if (this.SpriteBatch != null)
                        this.SpriteBatch.Dispose();

                }
                this.Content = null;
                this.SpriteBatch = null;
                this.disposed = true;
            }
        }
	}
}