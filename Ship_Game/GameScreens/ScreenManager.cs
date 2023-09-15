using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using Microsoft.Xna.Framework.Graphics;
using SDUtils;
using Ship_Game.Audio;
using Ship_Game.GameScreens;
using Ship_Game.GameScreens.DiplomacyScreen;
using Ship_Game.Utils;
using SynapseGaming.LightingSystem.Core;
using SynapseGaming.LightingSystem.Lights;
using SynapseGaming.LightingSystem.Rendering;
using Vector2 = SDGraphics.Vector2;
using Rectangle = SDGraphics.Rectangle;
using Matrix = SDGraphics.Matrix;
using XnaMatrix = Microsoft.Xna.Framework.Matrix;
using GraphicsDeviceManager = Microsoft.Xna.Framework.GraphicsDeviceManager;
using SDGraphics.Sprites;
#pragma warning disable CA2213

namespace Ship_Game
{
    public sealed class ScreenManager : IDisposable
    {
        readonly Array<GameScreen> GameScreens = new();
        readonly IGraphicsDeviceService GraphicsDeviceService;
        readonly SceneState GameSceneState;
        readonly SceneInterface SceneInter;
        readonly GameBase GameInstance;

        public LightRigIdentity LightRigIdentity = LightRigIdentity.Unknown;

        public LightingSystemManager LightSysManager;
        public SceneEnvironment Environment;
        public InputState input;
        public AudioHandle Music = new();

        public GraphicsDeviceManager Graphics;
        public GraphicsDevice GraphicsDevice;
        public SpriteBatch SpriteBatch;
        public SpriteRenderer SpriteRenderer;

        // Thread safe screen queue
        readonly SafeQueue<GameScreen> PendingScreens = new();

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
            GraphicsDeviceService = (IGraphicsDeviceService)game.Services.GetService(typeof(IGraphicsDeviceService))
                                  ?? throw new InvalidOperationException("No graphics device service.");

            GraphicsDevice.DeviceReset += GraphicsDeviceService_DeviceReset;
            GraphicsDevice.DeviceResetting += GraphicsDevice_DeviceResetting;
            GraphicsDevice.DeviceLost += GraphicsDevice_DeviceLost;
            GraphicsDevice.Disposing += GraphicsDevice_Disposing;

            input = new();
            LightSysManager = new(game.Services);
            GameSceneState = new();
            SceneInter = new(graphics);
            SceneInter.CreateDefaultManagers(useDeferredRendering:false, usePostProcessing:true);
            SceneInter.AddManager(new GameLightManager(graphics));
        }

        void GraphicsDeviceService_DeviceReset(object sender, EventArgs e)
        {
            Log.Write(ConsoleColor.Green, "GraphicsDevice Reset");
        }

        void GraphicsDevice_DeviceResetting(object sender, EventArgs e)
        {
            Log.Write(ConsoleColor.Green, "GraphicsDevice Resetting");
        }

        void GraphicsDevice_DeviceLost(object sender, EventArgs e)
        {
            Log.Write(ConsoleColor.Green, "GraphicsDevice Lost");
        }

        void GraphicsDevice_Disposing(object sender, EventArgs e)
        {
            Log.Write(ConsoleColor.Green, "GraphicsDevice Disposing");
        }

        class GameLightManager : LightManager
        {
            public GameLightManager(IGraphicsDeviceService device) : base(device) {}

            // TODO: maybe improve it here?
        }

        public void UpdatePreferences(LightingSystemPreferences preferences)
        {
            lock (SceneInter)
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
            if (screen == null)
            {
                Log.Error("GameScreen parameter cannot be null");
                return;
            }

            if (GameBase.MainThreadId != Thread.CurrentThread.ManagedThreadId)
            {
                Log.Error("GameScreens can only be added on the main thread! Use AddScreen!");
                AddScreen(screen);
                return;
            }
            
            // @todo What is this hack doing here? It appears to prohibit new popups while DiplomacyScreen is visible
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

        public void AddObject(ISceneObject so)
        {
            if (so != null)
                PendingObjects.Add(so);
        }
        
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

        public void RemoveAllLights(LightRigIdentity identity = LightRigIdentity.Unknown)
        {
            AssignLightRig(identity, null);
        }

        public void AssignLightRig(LightRigIdentity identity, LightRig rig)
        {
            lock (SceneInter)
            {
                LightRigIdentity = identity;
                SceneInter.LightManager.Clear();
                PendingLights.Clear();
                ActiveDynamicLights = 0;

                if (rig != null)
                    SceneInter.LightManager.Submit(rig);
            }
        }

        ////////////////////////////////////////////////////////////////////////////////////

        public void ClearScene()
        {
            lock (SceneInter)
            {
                PendingObjects.Clear();
                SceneInter.ObjectManager.Clear();
                RemoveAllLights();
            }
        }

        // This must be called when Graphics Device is reset!
        public void UnloadSceneObjects()
        {
            lock (SceneInter)
            {
                SceneInter.Unload();
                LightSysManager.Dispose(); // must be called on Graphics Device reset!
                ActiveDynamicLights = 0;
            }
        }

        ////////////////////////////////////////////////////////////////////////////////////

        public void UpdateSceneObjects(float deltaTime)
        {
            if (!IsMainThread)
                ErrorMustBeOnMainThread(nameof(UpdateSceneObjects));
            lock (SceneInter)
            {
                SceneInter.Update(deltaTime);
            }
        }

        public void RenderSceneObjects()
        {
            if (!IsMainThread)
                ErrorMustBeOnMainThread(nameof(RenderSceneObjects));
            lock (SceneInter)
            {
                SceneInter.RenderManager.Render();
            }
        }

        public void BeginFrameRendering(DrawTimes elapsed, ref Matrix view, ref Matrix projection)
        {
            if (!IsMainThread)
                ErrorMustBeOnMainThread(nameof(BeginFrameRendering));

            XnaMatrix xnaView = view;
            XnaMatrix xnaProj = projection;

            lock (SceneInter)
            {
                SubmitPendingObjects(SceneInter.ObjectManager, PendingObjects);
                SubmitPendingLights(SceneInter.LightManager, PendingLights);

                GameSceneState.BeginFrameRendering(ref xnaView, ref xnaProj, elapsed.RealTime.Seconds, Environment, true);
                SceneInter.BeginFrameRendering(GameSceneState);
            }
        }

        public void EndFrameRendering()
        {
            if (!IsMainThread)
                ErrorMustBeOnMainThread(nameof(EndFrameRendering));

            lock (SceneInter)
            {
                SceneInter.EndFrameRendering();
                GameSceneState.EndFrameRendering();
            }
        }

        ////////////////////////////////////////////////////////////////////////////////////

        /// <summary>
        /// The Draw loop works on visible real time between frames,
        /// since the delta time varies greatly between threads
        /// </summary>
        readonly DrawTimes DrawLoopTime = new();

        public void Draw()
        {
            DrawLoopTime.UpdateBeforeRendering(GameBase.Base.TotalElapsed);

            SpriteBatch batch = SpriteBatch;
            if (batch == null)
                return; // ScreenManager was disposed

            // the engine is still reloading graphics resources; wait a bit...
            if (ResourceManager.WhitePixel == null || ResourceManager.WhitePixel.IsDisposed)
                return;

            SpriteRenderer.RecycleBuffers();

            GameScreen[] screens = GameScreens.ToArray();
            for (int i = 0; i < screens.Length; ++i)
            {
                GameScreen screen = screens[i];
                if (screen.Visible && !screen.IsDisposed && screen.DidRunUpdate)
                {
                    try
                    {
                        screen.Draw(batch, DrawLoopTime);
                    }
                    catch (ObjectDisposedException e)
                    {
                        // When user device goes to sleep, graphics resources are lost
                        // So all screens need to be reloaded
                        Log.Error(e, "Draw Screen failed: Object Disposed, attempting to reload all screens");
                        return; // we can't continue this loop anyways
                    }
                    catch (Exception e)
                    {
                        Log.Error(e, $"Draw Screen failed: {screen.GetType().GetTypeName()}");
                        if (!batch.SafeEnd())
                        {
                            Log.Error("Draw Screen fatal error: batch end failed"); 
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
            var screens = GameScreens.ToArray(); /*grab an atomic copy*/

            // exit screens in reverse
            for (int i = screens.Length - 1; i >= 0; --i)
            {
                GameScreen screen = screens[i];
                if (!screen.IsExiting) // exit only if we're not already exiting
                    screen.ExitScreen();
            }

            // forcefully remove any screens that didn't get removed during ExitScreen,
            // since some screens have transition effects
            foreach (GameScreen screen in GameScreens.ToArray())
                RemoveScreen(screen);

            if (clear3DObjects)
            {
                ClearScene();
            }
        }

        public void FadeBackBufferToBlack(int alpha)
        {
            SpriteBatch.SafeBegin();
            SpriteBatch.Draw(ResourceManager.Blank, new Rectangle(0, 0, GameBase.ScreenWidth, GameBase.ScreenHeight), new Color(0, 0, 0, (byte)alpha));
            SpriteBatch.SafeEnd();
        }

        public void UpdateGraphicsDevice()
        {
            GraphicsDevice = Graphics.GraphicsDevice;
            if (SpriteBatch == null || SpriteBatch.GraphicsDevice != GraphicsDevice)
            {
                SpriteBatch?.Dispose();
                SpriteBatch = new(GraphicsDevice);
            }
            if (SpriteRenderer == null || SpriteRenderer.Device != GraphicsDevice)
            {
                SpriteRenderer?.Dispose();
                SpriteRenderer = new(GraphicsDevice);
            }
        }

        /// <summary>
        /// Clears the screen before rendering new content
        /// </summary>
        public void ClearScreen(Color color)
        {
            GraphicsDevice.Clear(color);
        }

        public void LoadContent(bool deviceWasReset)
        {
            Log.Write("ScreenManager.LoadContent");
            UpdateGraphicsDevice();

            Environment = ResourceManager.RootContent.Load<SceneEnvironment>("example/scene_environment");

            if (deviceWasReset) // recover
            {
                foreach (GameScreen screen in GameScreens)
                {
                    screen.ReloadContent();
                }
            }
            else // first time init
            {
                foreach (GameScreen screen in GameScreens)
                {
                    screen.InvokeLoadContent();
                }
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
        readonly Map<string, Hotloadable> HotLoadTargets = new();

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

            // OnModified/ReloadContent is allowed to modify HotLoadTargets
            // so to avoid collection modification issues, we iterate by KEY
            string[] keys = HotLoadTargets.Keys.ToArr();
            foreach (string targetKey in keys)
            {
                if (HotLoadTargets.TryGetValue(targetKey, out Hotloadable hot))
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
        }

        public void Update(UpdateTimes elapsed)
        {
            PerformHotLoadTasks(elapsed);
            input.Update(elapsed); // analyze input state for this frame
            AddPendingScreens();

            bool otherScreenHasFocus = !StarDriveGame.Instance?.IsActive ?? false;
            bool coveredByOtherScreen = false;
            bool inputCaptured = false;
            
            Array<GameScreen> frontToBack = new(); // valid screens ordered from topmost to back
            Array<GameScreen> backToFront = new();

            // since GameScreens are allowed to be removed randomly during their HandleInput,
            // we always create a copy of current screens, and double check if the screen still exists
            GameScreen[] activeScreens = GameScreens.ToArr();
            for (int i = activeScreens.Length - 1; i >= 0; --i)
            {
                GameScreen screen = activeScreens[i];
                if (!GameScreens.ContainsRef(screen))
                    continue; // this screen was removed while we were processing HandleInput events

                // 1. Handle Input
                if (!otherScreenHasFocus && !screen.IsExiting && !inputCaptured)
                {
                    inputCaptured = screen.HandleInput(input);
                    if (screen.IsDisposed)
                        continue; // HandleInput is allowed to Dispose this screen
                }

                // 2. Add the screen to Update list
                if (screen.PreUpdate(elapsed, otherScreenHasFocus, coveredByOtherScreen))
                {
                    frontToBack.Add(screen);
                    backToFront.Insert(0, screen);
                }

                // update visibility flags
                if (screen.ScreenState is ScreenState.TransitionOn or ScreenState.Active)
                {
                    otherScreenHasFocus = true;

                    if (!screen.IsPopup)
                        coveredByOtherScreen = true;
                }
            }

            // 4. trigger inactive in front-to-back order
            foreach (GameScreen screen in frontToBack)
            {
                if (!screen.Visible && screen.IsScreenActive)
                    screen.OnBecomeInActive();
            }
            
            // 5. trigger active in back-to-front order
            foreach (GameScreen screen in backToFront)
            {
                if (screen.Visible && !screen.IsScreenActive)
                    screen.OnBecomeActive();
            }
            
            // 6. update the screens in back-to-front order
            GameScreen topMost = frontToBack.Find(s => s.Visible);
            foreach (GameScreen screen in backToFront)
            {
                if (screen.Visible)
                    screen.Update(elapsed, isTopMost: screen == topMost);
            }
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        ~ScreenManager() { Dispose(false); }

        void Dispose(bool disposing)
        {
            Mem.Dispose(ref SpriteBatch);
            Mem.Dispose(ref SpriteRenderer);
            Mem.Dispose(ref LightSysManager);
            PendingScreens.Dispose();
        }
    }
}