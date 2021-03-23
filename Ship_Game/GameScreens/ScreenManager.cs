using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Ship_Game.Audio;
using Ship_Game.GameScreens;
using Ship_Game.GameScreens.DiplomacyScreen;
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

        public LightRigIdentity LightRigIdentity = LightRigIdentity.Unknown;

        public LightingSystemManager LightSysManager;
        public LightingSystemEditor editor;
        public SceneEnvironment Environment;
        public InputState input;
        public AudioHandle Music = new AudioHandle();

        public GraphicsDeviceManager Graphics;
        public GraphicsDevice GraphicsDevice;
        public SpriteBatch SpriteBatch;

        // Thread safe screen queue
        readonly SafeQueue<GameScreen> PendingScreens = new SafeQueue<GameScreen>();

        // Thread safe input queue for running UI input on empire thread
        readonly SafeQueue<Action> PendingEmpireThreadActions = new SafeQueue<Action>();

        public Rectangle TitleSafeArea { get; private set; }
        public int NumScreens => GameScreens.Count;
        public GameScreen Current => GameScreens[GameScreens.Count-1];
        public IReadOnlyList<GameScreen> Screens => GameScreens;

        public Vector2 ScreenCenter => GameBase.ScreenCenter;

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
            input = new InputState();
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
            {
                LightRigIdentity = LightRigIdentity.Unknown;
                SceneInter.LightManager.Clear();
            }
        }

        public void AssignLightRig(LightRigIdentity identity, LightRig rig)
        {
            lock (InterfaceLock)
            {
                LightRigIdentity = identity;
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

        public void UpdateSceneObjects(float deltaTime)
        {
            lock (InterfaceLock)
                SceneInter.Update(deltaTime);
        }

        public void RenderSceneObjects()
        {
            lock (InterfaceLock)
                SceneInter.RenderManager.Render();
        }

        public void BeginFrameRendering(DrawTimes elapsed, ref Matrix view, ref Matrix projection)
        {
            lock (InterfaceLock)
            {
                GameSceneState.BeginFrameRendering(ref view, ref projection,
                                                   elapsed.RealTime.Seconds, Environment, true);
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

        /// <summary>
        /// The Draw loop works on visible real time between frames,
        /// since the delta time varies greatly between threads
        /// </summary>
        readonly DrawTimes DrawLoopTime = new DrawTimes();

        public void Draw()
        {
            DrawLoopTime.UpdateBeforeRendering(GameBase.Base.TotalGameTimeSeconds);

            SpriteBatch batch = SpriteBatch;
            for (int i = 0; i < GameScreens.Count; ++i)
            {
                GameScreen screen = GameScreens[i];
                if (screen.Visible && !screen.IsDisposed)
                {
                    try
                    {
                        screen.Draw(batch, DrawLoopTime);
                    }
                    catch (Exception e)
                    {
                        Log.Error(e, $"Draw Screen failed: {screen.GetType().GetTypeName()}");
                        try { batch.End(); } catch (Exception x)
                        {
                            Log.Error(x, "Fatal Loop in draw batch"); 
                            screen.Dispose();
                            GameScreens.Remove(screen);

                        }
                    }
                }
            }
            
            ToolTip.Draw(batch, DrawLoopTime);
        }

        public void ExitAll(bool clear3DObjects)
        {
            foreach (GameScreen screen in GameScreens.ToArray()/*grab an atomic copy*/)
            {
                screen.ExitScreen();
            }

            // forcefully remove, since some screens have transition effects
            foreach (GameScreen screen in GameScreens.ToArray())
                RemoveScreen(screen);

            if (clear3DObjects)
            {
                RemoveAllObjects();
                RemoveAllLights();
            }
        }

        public void ExitAllExcept(GameScreen except)
        {
            foreach (GameScreen screen in GameScreens.ToArray()/*grab an atomic copy*/)
            {
                if (screen != except)
                {
                    screen.ExitScreen();
                }
            }
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

        // @warning This unloads ALL game content
        // and is designed to be called only from ResourceManager!
        public void UnloadAllGameContent()
        {
            // @warning We only unload the content. And then reload later.
            foreach (GameScreen screen in GameScreens)
            {
                screen.UnloadContent();
            }

            UnloadSceneObjects();
            GameInstance.Content.Unload();
        }

        public void RemoveScreen(GameScreen screen)
        {
            if (GraphicsDeviceService?.GraphicsDevice != null)
            {
                screen.UnloadContent();
                screen.Dispose();
            }

            GameScreens.Remove(screen);
            screen.OnScreenRemoved();
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

        void PerformHotLoadTasks(UpdateTimes elapsed)
        {
            HotloadTimer += elapsed.RealTime.Seconds;
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

        public void Update(UpdateTimes elapsed)
        {
            PerformHotLoadTasks(elapsed);
            input.Update(elapsed); // analyze input state for this frame
            AddPendingScreens();

            bool otherScreenHasFocus = !StarDriveGame.Instance?.IsActive ?? false;
            bool coveredByOtherScreen = false;
            bool inputCaptured = false;

            // @note GameScreen could be removed during screen.Update, so [i] must always be bounds checked
            for (int i = GameScreens.Count - 1; i >= 0 && i < GameScreens.Count; --i)
            {
                GameScreen screen = GameScreens[i];
                if (screen == null) // FIX: threading or other removal issue,
                    continue;       // GameScreens was modified from another thread

                // 1. Handle Input
                if (!otherScreenHasFocus && !screen.IsExiting && !inputCaptured)
                {
                    inputCaptured = screen.HandleInput(input);
                    if (screen.IsDisposed)
                        continue; // HandleInput is allowed to Dispose this screen
                }

                // 2. Update the screen
                screen.Update(elapsed, otherScreenHasFocus, coveredByOtherScreen);

                // update visibility flags
                if (screen.ScreenState == ScreenState.TransitionOn ||
                    screen.ScreenState == ScreenState.Active)
                {
                    otherScreenHasFocus = true;

                    if (!screen.IsPopup)
                        coveredByOtherScreen = true;
                }
            }
        }

        /// <summary>
        /// Invokes all Pending actions. This should only be called from ProcessTurns !!!
        /// </summary>
        public void InvokePendingEmpireThreadActions()
        {
            while (PendingEmpireThreadActions.TryDequeue(out Action action))
                action();
        }

        /// <summary>
        /// Queues action to run on the Empire / Simulation thread, aka ProcessTurns thread.
        /// </summary>
        public void RunOnEmpireThread(Action action)
        {
            if (action != null)
            {
                PendingEmpireThreadActions.Enqueue(action);
            }
            else
            {
                Log.WarningWithCallStack("Null Action passed to RunOnEmpireThread method");
            }
        }

        public void Dispose()
        {
            Destroy();
            GC.SuppressFinalize(this);
        }

        ~ScreenManager() { Destroy(); }

        void Destroy()
        {
            SpriteBatch?.Dispose(ref SpriteBatch);
        }
    }
}