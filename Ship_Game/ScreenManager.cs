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
		public Array<GameScreen> screens = new Array<GameScreen>();
		private readonly Array<GameScreen> screensToUpdate = new Array<GameScreen>();
		private readonly Array<GameScreen> screensToDraw = new Array<GameScreen>();
		public InputState input = new InputState();
		private readonly IGraphicsDeviceService graphicsDeviceService;
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
		public AudioHandle Music;
		public GraphicsDevice GraphicsDevice;
		public SpriteBatch SpriteBatch;

        public float exitScreenTimer;

		//public GameContentManager Content { get; private set; }
	    public Rectangle TitleSafeArea { get; private set; }
	    public bool TraceEnabled { get; set; }

	    public ScreenManager(Game1 game, GraphicsDeviceManager graphics)
		{
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

		public void ExitAll()
		{
		    foreach (GameScreen screen in screens.ToArray())
		        screen.ExitScreen();
		}

		public void FadeBackBufferToBlack(int alpha, SpriteBatch spriteBatch)
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

        public int ScreenCount => screens.Count;

		public void LoadContent()
		{
			SpriteBatch = new SpriteBatch(GraphicsDevice);
            blankTexture = ResourceManager.LoadTexture("blank");
			foreach (GameScreen screen in screens)
			{
				screen.LoadContent();
			}

			Viewport viewport = GraphicsDevice.Viewport;
			TitleSafeArea = new Rectangle(
                (int)(viewport.X + viewport.Width  * 0.05f),
                (int)(viewport.Y + viewport.Height * 0.05f),
                (int)(viewport.Width  * 0.9f),
                (int)(viewport.Height * 0.9f));
		}

		public void RemoveScreen(GameScreen screen)
		{
			if (graphicsDeviceService?.GraphicsDevice != null)
				screen.UnloadContent();
			screens.Remove(screen);
			screensToUpdate.Remove(screen);
            exitScreenTimer = .025f;
		}

		public void SetupSunburn()
		{
		}

		private void TraceScreens()
		{
			var screenNames = new Array<string>();
			foreach (GameScreen screen in screens)
				screenNames.Add(screen.GetType().Name);
			Trace.WriteLine(string.Join(", ", screenNames.ToArray()));
		}

		public void Update(GameTime gameTime)
		{
			input.Update(gameTime);
			screensToUpdate.Clear();
			foreach (GameScreen screen in screens)
			{
				screensToUpdate.Add(screen);
			}
			bool otherScreenHasFocus = !Game1.Instance.IsActive;
			bool coveredByOtherScreen = false;
			while (screensToUpdate.Count > 0)
			{
				GameScreen screen = screensToUpdate[screensToUpdate.Count - 1];
				screensToUpdate.RemoveAt(screensToUpdate.Count - 1);
				screen.Update(gameTime, otherScreenHasFocus, coveredByOtherScreen);
				if (screen.ScreenState != ScreenState.TransitionOn && screen.ScreenState != ScreenState.Active)
					continue;
				if (!otherScreenHasFocus)
				{
					screen.HandleInput(input);
					otherScreenHasFocus = true;
				}
				if (screen.IsPopup)
					continue;
				coveredByOtherScreen = true;
			}
			if (TraceEnabled)
				TraceScreens();
		}
	    public bool UpdateExitTimeer(bool stopFurtherInput)
	    {
	        if (!stopFurtherInput)
	        {
	            exitScreenTimer -= .0016f;
	            if (exitScreenTimer > 0f)
	                return true;
	        }
	        else exitScreenTimer = .025f;
	        return false;
	    }
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        ~ScreenManager() { Dispose(false); }

        private void Dispose(bool disposing)
        {
            SpriteBatch?.Dispose(ref SpriteBatch);
        }
	}
}