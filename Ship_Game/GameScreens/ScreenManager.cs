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
        readonly GameBase GameInstance;

        public LightRigIdentity LightRigIdentity = LightRigIdentity.Unknown;

        public LightingSystemManager LightSysManager;
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
        public int NumScreens => GameScreens.Count + PendingScreens.Count;
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
            SceneInter.AddManager(new GameLightManager(graphics));
        }

        class GameLightManager : LightManager
        {
            public GameLightManager(IGraphicsDeviceService device) : base(device) {}

            // TODO: maybe improve it here?
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
        // @warning Using this WILL cause screens to be Drawn before they are UPDATED!
        // @warning THIS SHOULD NOT BE USED IN MOST CASES
        public void AddScreenAndLoadContent(GameScreen screen)
        {
            if (GameBase.MainThreadId != Thread.CurrentThread.ManagedThreadId)
            {
                Log.Error("GameScreens can only be added on the main thread! Use AddScreen!");
                AddScreen(screen);
                return;
            }

            // @todo What is this hack doing here?
            foreach (GameScreen gs in GameScreens)
                if (gs is DiplomacyScreen)
                    return;

            GameScreens.Add(screen);

            // @note LoadContent is allowed to remove current screen as well
            screen.InvokeLoadContent();
        }

        // @note This is thread safe. Screen is added during next update of ScreenManager
        public void AddScreen(GameScreen screen)
        {
            PendingScreens.PushToFront(screen);
        }

        void AddPendingScreens()
        {
            while (PendingScreens.TryDequeue(out GameScreen screen))
                AddScreenAndLoadContent(screen);
        }

        // exits all other screens and goes to specified screen
        public void GoToScreen(GameScreen screen, bool clear3DObjects)
        {
            ExitAll(clear3DObjects);
            AddScreenAndLoadContent(screen);
        }

        public void AddScreenNoLoad(GameScreen screen)
        {
            GameScreens.Add(screen);
        }

        public bool IsShowing<T>() where T : GameScreen
        {
            return FindScreen<T>() != null;
        }

        public T FindScreen<T>() where T : GameScreen
        {
            foreach (GameScreen gs in GameScreens)
                if (gs is T gt) return gt;
            return null;
        }

        public bool IsMainThread => Thread.CurrentThread.ManagedThreadId == GameBase.MainThreadId;

        void ErrorMustBeOnMainThread(string functionName)
        {
            Log.Error($"{functionName}() must only be called on main draw thread!");
        }

        ////////////////////////////////////////////////////////////////////////////////////
        
        readonly ChangePendingListSafe<ISceneObject> PendingObjects = new ChangePendingListSafe<ISceneObject>();
        readonly ChangePendingListSafe<(ILight,bool)> PendingLights = new ChangePendingListSafe<(ILight,bool)>();
        public int ActiveDynamicLights;

        void SubmitPendingObjects(ISubmit<ISceneObject> manager, ChangePendingListSafe<ISceneObject> pendingList)
        {
            Array<PendingItem<ISceneObject>> pending = pendingList.MovePendingItems();
            for (int i = 0; i < pending.Count; ++i)
            {
                PendingItem<ISceneObject> p = pending[i];
                if (p.Add)
                    manager.Submit(p.Item);
                else
                    manager.Remove(p.Item);
            }
        }
        void SubmitPendingLights(ISubmit<ILight> manager, ChangePendingListSafe<(ILight,bool)> pendingList)
        {
            Array<PendingItem<(ILight,bool)>> pending = pendingList.MovePendingItems();
            for (int i = 0; i < pending.Count; ++i)
            {
                PendingItem<(ILight,bool)> p = pending[i];
                bool dynamic = p.Item.Item2;
                if (p.Add)
                {
                    if (dynamic)
                    {
                        if (ActiveDynamicLights >= GlobalStats.MaxDynamicLightSources)
                            continue; // don't add more
                        ++ActiveDynamicLights;
                    }
                    manager.Submit(p.Item.Item1);
                }
                else
                {
                    if (manager.Remove(p.Item.Item1))
                    {
                        if (dynamic)
                            --ActiveDynamicLights;
                    }
                }
            }
        }

        public void AddObject(ISceneObject so) => PendingObjects.Add(so);
        public void RemoveObject(ISceneObject so)
        {
            if (so != null)
                PendingObjects.Remove(so);
        }
        
        public void AddLight(ILight light, bool dynamic)
        {
            if (light != null)
                PendingLights.Add((light,dynamic));
        }
        public void RemoveLight(ILight light, bool dynamic)
        {
            if (light != null)
            {
                PendingLights.Remove((light,dynamic));
            }
        }

        public void RemoveAllObjects()
        {
            PendingObjects.Clear();
            SceneInter.ObjectManager.Clear();
        }

        public void RemoveAllLights()
        {
            AssignLightRig(LightRigIdentity.Unknown, null);
        }

        public void AssignLightRig(LightRigIdentity identity, LightRig rig)
        {
            LightRigIdentity = identity;
            SceneInter.LightManager.Clear();
            PendingLights.Clear();
            ActiveDynamicLights = 0;

            if (rig != null)
                SceneInter.LightManager.Submit(rig);
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
            ActiveDynamicLights = 0;
        }

        ////////////////////////////////////////////////////////////////////////////////////

        public void UpdateSceneObjects(float deltaTime)
        {
            if (!IsMainThread)
                ErrorMustBeOnMainThread(nameof(UpdateSceneObjects));
            SceneInter.Update(deltaTime);
        }

        public void RenderSceneObjects()
        {
            if (!IsMainThread)
                ErrorMustBeOnMainThread(nameof(RenderSceneObjects));
            SceneInter.RenderManager.Render();
        }

        public void BeginFrameRendering(DrawTimes elapsed, ref Matrix view, ref Matrix projection)
        {
            if (!IsMainThread)
                ErrorMustBeOnMainThread(nameof(BeginFrameRendering));

            SubmitPendingObjects(SceneInter.ObjectManager, PendingObjects);
            SubmitPendingLights(SceneInter.LightManager, PendingLights);

            GameSceneState.BeginFrameRendering(ref view, ref projection,
                                               elapsed.RealTime.Seconds, Environment, true);
            SceneInter.BeginFrameRendering(GameSceneState);
        }

        public void EndFrameRendering()
        {
            if (!IsMainThread)
                ErrorMustBeOnMainThread(nameof(EndFrameRendering));
            SceneInter.EndFrameRendering();
            GameSceneState.EndFrameRendering();
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

            // draw software cursor / or update OS cursor
            // don't use software cursor in loading screens
            bool software = GlobalStats.UseSoftwareCursor && !IsShowing<GameLoadingScreen>();
            GameCursors.Draw(GameInstance, batch, input.CursorPosition, software);
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
            Log.Info("ScreenManager.LoadContent");
            UpdateGraphicsDevice();

            foreach (GameScreen screen in GameScreens)
            {
                screen.InvokeLoadContent();
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
        public void UnsafeUnloadAllGameContent()
        {
            Log.Info("ScreenManager.UnloadAllGameContent");

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
        // @param relativePath File to check
        // @param onModified Event to trigger if File was changed
        public FileInfo AddHotLoadTarget(GameScreen screen, string relativePath, Action<FileInfo> onModified)
        {
            FileInfo file = ResourceManager.GetModOrVanillaFile(relativePath);
            if (file == null)
                throw new FileNotFoundException($"No such file: {relativePath}");

            AddHotLoadTarget(screen, file, onModified);
            return file;
        }

        // HotLoading allows for modifying game content while the game is running
        // Different content managers are triggered through `OnModified`
        // which will reload appropriate subsystems
        // @param file File to check
        // @param onModified Event to trigger if File was changed
        public void AddHotLoadTarget(GameScreen screen, FileInfo file, Action<FileInfo> onModified)
        {
            string key = file.Name;
            HotLoadTargets[key] = new Hotloadable {
                File = file.FullName,
                LastModified = file.LastWriteTimeUtc,
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
        public FileInfo AddHotLoadTarget(GameScreen screen, string relativePath)
        {
            FileInfo file = ResourceManager.GetModOrVanillaFile(relativePath);
            if (file == null)
                throw new FileNotFoundException($"No such file: {relativePath}");

            HotLoadTargets[relativePath] = new Hotloadable {
                File = file.FullName,
                LastModified = file.LastWriteTimeUtc,
                Screen = screen
            };
            screen.OnExit += () =>
            {
                HotLoadTargets.Remove(relativePath);
            };
            return file;
        }

        /// <summary>
        /// Adds a generic HotLoad target.
        /// This is the newest and most streamlined overload, where loading is
        /// always done via `onModified` callback.
        /// 
        /// HotLoading allows for modifying game content while the game is running.
        /// Different content managers are triggered through `OnModified`
        /// which will reload appropriate subsystems.
        /// 
        /// </summary>
        /// <param name="relativePath">File to check</param>
        /// <param name="onModified">Event to trigger if File was changed</param>
        public void LoadAndEnableHotLoad(string relativePath, Action<FileInfo> onModified)
        {
            FileInfo file = ResourceManager.GetModOrVanillaFile(relativePath);
            if (file == null)
                throw new FileNotFoundException($"No such file: {relativePath}");

            AddHotLoadTarget(null, file, onModified);
            onModified(file); // Load It!
        }

        // Remove an entry from registered hot load targets
        public void RemoveHotLoadTarget(string key)
        {
            HotLoadTargets.Remove(key);
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