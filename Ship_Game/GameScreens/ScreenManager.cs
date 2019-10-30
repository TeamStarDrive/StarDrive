using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Ship_Game.Audio;
using Ship_Game.Utils;
using SynapseGaming.LightingSystem.Core;
using SynapseGaming.LightingSystem.Editor;
using SynapseGaming.LightingSystem.Lights;
using SynapseGaming.LightingSystem.Rendering;

namespace Ship_Game
{
    public sealed class ScreenManager : IDisposable
    {
        readonly Array<GameScreen> GameScreens = new Array<GameScreen>();
        readonly IGraphicsDeviceService GraphicsDeviceService;
        readonly SceneState GameSceneState;
        readonly SceneInterface SceneInter;
        readonly object InterfaceLock = new object();
        readonly GameBase GameInstance;

        // Time elapsed between 2 frames
        // this can be used for rendering animations
        public float FrameDeltaTime { get; private set; }

        public LightingSystemManager LightSysManager;
        public LightingSystemEditor editor;
        public SceneEnvironment environment;
        public InputState input = new InputState();
        public AudioHandle Music = new AudioHandle();

        public GraphicsDeviceManager Graphics;
        public GraphicsDevice GraphicsDevice;
        public SpriteBatch SpriteBatch;

        // Thread safe screen queue
        readonly SafeQueue<GameScreen> PendingScreens = new SafeQueue<GameScreen>();

        public float ExitScreenTimer { get; set; }

        public Rectangle TitleSafeArea { get; private set; }
        public int NumScreens => GameScreens.Count;
        public GameScreen Current => GameScreens[GameScreens.Count-1];
        public IReadOnlyList<GameScreen> Screens => GameScreens;

        public static ScreenManager Instance { get; private set; }
        public static GameScreen CurrentScreen => Instance.Current;

        public ScreenManager(GameBase game, GraphicsDeviceManager graphics)
        {
            Instance = this;
            GameInstance = game;
            Graphics = graphics;
            GraphicsDevice = graphics.GraphicsDevice;
            GraphicsDeviceService = (IGraphicsDeviceService)game.Services.GetService(typeof(IGraphicsDeviceService));
            if (GraphicsDeviceService == null)
            {
                throw new InvalidOperationException("No graphics device service.");
            }
            LightSysManager = new LightingSystemManager(game.Services);
            GameSceneState = new SceneState();
            SceneInter = new SceneInterface(graphics);
            SceneInter.CreateDefaultManagers(false, false, true);
            editor = new LightingSystemEditor(game.Services, graphics, game)
            {
                UserHandledView = true
            };
            SceneInter.AddManager(editor);
        }

        public void UpdatePreferences(LightingSystemPreferences preferences)
        {
            SceneInter.ApplyPreferences(preferences);
        }

        public void UpdateViewports()
        {
            for (int i = 0; i < GameScreens.Count; ++i)
                GameScreens[i].UpdateViewport();
        }

        // @warning This is not thread safe!
        public void AddScreen(GameScreen screen)
        {
            if (GameBase.MainThreadId != Thread.CurrentThread.ManagedThreadId)
            {
                Log.Error("GameScreens can only be added on the main thread! Use AddScreenDeferred!");
                AddScreenDeferred(screen);
                return;
            }

            // @todo What is this hack doing here?
            foreach (GameScreen gs in GameScreens)
                if (gs is DiplomacyScreen)
                    return;

            GameScreens.Add(screen);

            // @note LoadContent is allowed to remove current screen as well
            screen.LoadContent();
        }

        // @note This is thread safe. Screen is added during next update of ScreenManager
        public void AddScreenDeferred(GameScreen screen)
        {
            PendingScreens.PushToFront(screen);
        }

        void AddPendingScreens()
        {
            while (PendingScreens.TryDequeue(out GameScreen screen))
                AddScreen(screen);
        }

        // exits all other screens and goes to specified screen
        public void GoToScreen(GameScreen screen, bool clear3DObjects)
        {
            ExitAll(clear3DObjects);
            AddScreen(screen);
        }

        public void AddScreenNoLoad(GameScreen screen)
        {
            GameScreens.Add(screen);
        }

        public bool IsShowing<T>() where T : GameScreen
        {
            foreach (GameScreen gs in GameScreens)
                if (gs is T) return true;
            return false;
        }

        ////////////////////////////////////////////////////////////////////////////////////

        public void AddObject(ISceneObject so)
        {
            if (so == null) return;
            lock (InterfaceLock)
                SceneInter.ObjectManager.Submit(so);
        }

        public void RemoveObject(ISceneObject so)
        {
            if (so == null) return;
            lock (InterfaceLock)
                SceneInter.ObjectManager.Remove(so);
        }

        public void RemoveAllObjects()
        {
            lock (InterfaceLock)
                SceneInter.ObjectManager.Clear();
        }

        ////////////////////////////////////////////////////////////////////////////////////

        public void AddLight(ILight light)
        {
            if (light == null) return;
            lock (InterfaceLock)
                SceneInter.LightManager.Submit(light);
        }

        public void RemoveLight(ILight light)
        {
            if (light == null) return;
            lock (InterfaceLock)
                SceneInter.LightManager.Remove(light);
        }

        public void RemoveAllLights()
        {
            lock (InterfaceLock)
                SceneInter.LightManager.Clear();
        }

        public void AssignLightRig(LightRig rig)
        {
            lock (InterfaceLock)
            {
                SceneInter.LightManager.Clear();
                SceneInter.LightManager.Submit(rig);
            }
        }

        ////////////////////////////////////////////////////////////////////////////////////

        public void ClearScene()
        {
            RemoveAllObjects();
            RemoveAllLights();
        }

        public void UnloadSceneObjects()
        {
            SceneInter.Unload();
            LightSysManager.Unload();
        }

        ////////////////////////////////////////////////////////////////////////////////////

        public void UpdateSceneObjects()
        {
            lock (InterfaceLock)
                SceneInter.Update(GameBase.Base.GameTime);
        }

        public void RenderSceneObjects()
        {
            lock (InterfaceLock)
                SceneInter.RenderManager.Render();
        }

        public void BeginFrameRendering(GameTime gameTime, ref Matrix view, ref Matrix projection)
        {
            lock (InterfaceLock)
            {
                GameSceneState.BeginFrameRendering(ref view, ref projection, gameTime, environment, true);
                editor.BeginFrameRendering(GameSceneState);
                SceneInter.BeginFrameRendering(GameSceneState);
            }
        }

        public void EndFrameRendering()
        {
            lock (InterfaceLock)
            {
                SceneInter.EndFrameRendering();
                editor.EndFrameRendering();
                GameSceneState.EndFrameRendering();
            }
        }

        ////////////////////////////////////////////////////////////////////////////////////

        public void Draw()
        {
            SpriteBatch batch = SpriteBatch;
            try
            {
                for (int i = 0; i < GameScreens.Count; ++i)
                {
                    if (GameScreens[i].Visible)
                    {
                        GameScreens[i].Draw(batch);
                    }
                }

                DrawToolTip(batch);
            }
            catch (Exception e)
            {
                Log.Error(e, "ScreenManager.Draw Crashed");
                try { batch.End(); } catch { }
            }
        }

        void DrawToolTip(SpriteBatch batch)
        {
            batch.Begin();
            ToolTip.Draw(batch, FrameDeltaTime);
            batch.End();
        }

        public void ExitAll(bool clear3DObjects)
        {
            foreach (GameScreen screen in GameScreens.ToArray()/*grab an atomic copy*/)
                screen.ExitScreen();

            GameScreens.Clear(); // forcefully clear, since some screens have transition effects

            if (clear3DObjects)
            {
                RemoveAllObjects();
                RemoveAllLights();
            }
        }

        public void ExitAllExcept(GameScreen except)
        {
            foreach (GameScreen screen in GameScreens.ToArray()/*grab an atomic copy*/)
                if (screen != except)
                    screen.ExitScreen();
        }

        public void FadeBackBufferToBlack(int alpha)
        {
            SpriteBatch.Begin();
            SpriteBatch.Draw(ResourceManager.Blank, new Rectangle(0, 0, GameBase.ScreenWidth, GameBase.ScreenHeight), new Color(0, 0, 0, (byte)alpha));
            SpriteBatch.End();
        }

        public void UpdateGraphicsDevice()
        {
            GraphicsDevice = Graphics.GraphicsDevice;
            if (SpriteBatch == null || SpriteBatch.GraphicsDevice != GraphicsDevice)
            {
                SpriteBatch = new SpriteBatch(GraphicsDevice);
            }
        }

        public void LoadContent()
        {
            UpdateGraphicsDevice();

            foreach (GameScreen screen in GameScreens)
            {
                screen.LoadContent();
            }

            Viewport viewport = GameBase.Viewport;
            TitleSafeArea = new Rectangle(
                (int)(viewport.X + viewport.Width  * 0.05f),
                (int)(viewport.Y + viewport.Height * 0.05f),
                (int)(viewport.Width  * 0.9f),
                (int)(viewport.Height * 0.9f));
        }

        // @warning This unloads ALL game content and is designed to be called only from ResourceManager!
        public void UnloadAllGameContent()
        {
            foreach (GameScreen screen in GameScreens)
                screen.UnloadContent();

            UnloadSceneObjects();
            GameInstance.Content.Unload();
        }

        public void RemoveScreen(GameScreen screen)
        {
            if (GraphicsDeviceService?.GraphicsDevice != null)
                screen.UnloadContent();
            GameScreens.Remove(screen);
            ExitScreenTimer = 0.25f;
        }

        float HotloadTimer;
        const float HotloadInterval = 1.0f;

        class Hotloadable
        {
            public string File;
            public DateTime LastModified;
            public Action<FileInfo> OnModified;
            public GameScreen Screen;
        }
        readonly Map<string, Hotloadable> HotLoadTargets = new Map<string, Hotloadable>();

        public void ResetHotLoadTargets()
        {
            HotLoadTargets.Clear();
        }

        // HotLoading allows for modifying game content while the game is running
        // Different content managers are triggered through `OnModified`
        // which will reload appropriate subsystems
        // @param key Unique key to categorize the hot load target
        // @param file File to check
        // @param onModified Event to trigger if File was changed
        public void AddHotLoadTarget(GameScreen screen, string key, string file, Action<FileInfo> onModified)
        {
            HotLoadTargets[key] = new Hotloadable {
                File = file,
                LastModified = File.GetLastWriteTimeUtc(file),
                OnModified = onModified
            };
            if (screen != null)
            {
                screen.OnExit += () =>
                {
                    HotLoadTargets.Remove(key);
                };
            }
        }

        // Adds a GameScreen as a HotLoad target
        // @note HotLoad is removed when screen exits
        // @note GameScreen.ReloadContent() is called when file is modified
        public void AddHotLoadTarget(GameScreen screen, string key, string file)
        {
            HotLoadTargets[key] = new Hotloadable {
                File = file,
                LastModified = File.GetLastWriteTimeUtc(file),
                Screen = screen
            };
            screen.OnExit += () =>
            {
                HotLoadTargets.Remove(key);
            };
        }

        void PerformHotLoadTasks()
        {
            HotloadTimer += FrameDeltaTime;
            if (HotloadTimer < HotloadInterval) return;

            HotloadTimer = 0f;
            foreach (Hotloadable hot in HotLoadTargets.Values)
            {
                var info = new FileInfo(hot.File);
                if (info.LastWriteTimeUtc != hot.LastModified)
                {
                    Log.Write(ConsoleColor.Magenta, $"HotLoading content: {info.Name}...");
                    hot.LastModified = info.LastWriteTimeUtc; // update
                    hot.OnModified?.Invoke(info);
                    hot.Screen?.ReloadContent();
                    return;
                }
            }
        }

        public void Update(GameTime gameTime)
        {
            float elapsedTime = (float)gameTime.ElapsedGameTime.TotalSeconds;

            FrameDeltaTime = (float)gameTime.ElapsedGameTime.TotalSeconds;
            if (FrameDeltaTime > 0.4f) // @note Probably we were loading something heavy
                FrameDeltaTime = 1f/60f;

            PerformHotLoadTasks();
            input.Update(elapsedTime); // analyze input state for this frame
            AddPendingScreens();

            if (ExitScreenTimer >= 0)
                ExitScreenTimer -= elapsedTime;

            bool otherScreenHasFocus = !StarDriveGame.Instance?.IsActive ?? false;
            bool coveredByOtherScreen = false;
            bool inputCaptured = false;

            // @note GameScreen could be removed during screen.Update, so [i] must always be bounds checked
            for (int i = GameScreens.Count - 1; i >= 0 && i < GameScreens.Count; --i)
            {
                GameScreen screen = GameScreens[i];

                // 1. Handle Input
                if (!otherScreenHasFocus && !screen.IsExiting && !inputCaptured)
                {
                    inputCaptured = screen.HandleInput(input);
                }

                // 2. Update the screen
                screen.Update(gameTime, otherScreenHasFocus, coveredByOtherScreen);

                // update visibility flags
                if (screen.ScreenState == ScreenState.TransitionOn ||
                    screen.ScreenState == ScreenState.Active)
                {
                    if (!otherScreenHasFocus && ExitScreenTimer <= 0f)
                        otherScreenHasFocus = true;

                    if (!screen.IsPopup)
                        coveredByOtherScreen = true;
                }
            }
        }

        public bool UpdateExitTimer(bool stopFurtherInput)
        {
            if (!stopFurtherInput)
            {
                if (ExitScreenTimer > 0f)
                    return true;
            }
            return false;
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        ~ScreenManager() { Dispose(false); }

        void Dispose(bool disposing)
        {
            SpriteBatch?.Dispose(ref SpriteBatch);
        }
    }
}