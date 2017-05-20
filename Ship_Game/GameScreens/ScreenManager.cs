using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using SynapseGaming.LightingSystem.Core;
using SynapseGaming.LightingSystem.Editor;
using SynapseGaming.LightingSystem.Rendering;
using System;
using SynapseGaming.LightingSystem.Lights;

namespace Ship_Game
{
    public sealed class ScreenManager : IDisposable
    {
        private readonly Array<GameScreen> Screens = new Array<GameScreen>();
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

        public Rectangle TitleSafeArea { get; private set; }

        public int NumScreens => Screens.Count;

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

        public void UpdateViewports()
        {
            for (int i = 0; i < Screens.Count; ++i)
                Screens[i].UpdateViewport();
        }

        public void AddScreen(GameScreen screen)
        {
            foreach (GameScreen gs in Screens)
                if (gs is DiplomacyScreen)
                    return;
            if (graphicsDeviceService?.GraphicsDevice != null)
                screen.LoadContent();
            Screens.Add(screen);
        }

        public void AddScreenNoLoad(GameScreen screen)
        {
            foreach (GameScreen gs in Screens)
                if (gs is DiplomacyScreen)
                    return;
            Screens.Add(screen);
        }

        public void Submit(ISceneObject so) => inter.ObjectManager.Submit(so);
        public void Remove(ISceneObject so) => inter.ObjectManager.Remove(so);
        public void Submit(ILight light) => inter.LightManager.Submit(light);
        public void Remove(ILight light) => inter.LightManager.Remove(light);
        public void RemoveAllLights() => inter.LightManager.Clear();

        public void Draw(GameTime gameTime)
        {
            try
            {
                for (int i = 0; i < Screens.Count; ++i)
                {
                    GameScreen screen = Screens[i];
                    if (screen.ScreenState != ScreenState.Hidden)
                        screen.Draw(gameTime);
                }
            }
            catch(Exception e)
            {
                Log.Warning("DrawLoop Crashed : {0}", e.InnerException);
                SpriteBatch.End();
            }
        }

        public void ExitAll()
        {
            foreach (GameScreen screen in Screens.ToArray())
                screen.ExitScreen();
        }

        public void ExitAllExcept(GameScreen except)
        {
            foreach (GameScreen screen in Screens.ToArray())
                if (screen != except)
                    screen.ExitScreen();
        }

        public void FadeBackBufferToBlack(int alpha, SpriteBatch spriteBatch)
        {
            Viewport viewport = Game1.Instance.Viewport;
            spriteBatch.Draw(blankTexture, new Rectangle(0, 0, viewport.Width, viewport.Height), new Color(0, 0, 0, (byte)alpha));
        }

        public void FadeBackBufferToBlack(int alpha)
        {
            Viewport viewport = Game1.Instance.Viewport;
            SpriteBatch.Begin();
            SpriteBatch.Draw(blankTexture, new Rectangle(0, 0, viewport.Width, viewport.Height), new Color(0, 0, 0, (byte)alpha));
            SpriteBatch.End();
        }

        public int ScreenCount => Screens.Count;

        public void LoadContent()
        {
            SpriteBatch = new SpriteBatch(GraphicsDevice);
            blankTexture = ResourceManager.LoadTexture("blank");
            foreach (GameScreen screen in Screens)
            {
                screen.LoadContent();
            }

            Viewport viewport = Game1.Instance.Viewport;
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
            Screens.Remove(screen);
            exitScreenTimer = .025f;
        }

        public void Update(GameTime gameTime)
        {
            input.Update(gameTime);

            bool otherScreenHasFocus = !Game1.Instance.IsActive;
            bool coveredByOtherScreen = false;

            for (int i = Screens.Count-1; i >= 0; --i)
            {
                GameScreen screen = Screens[i];
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