using Microsoft.Xna.Framework.Audio;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.GamerServices;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;
using System.Threading;

namespace Microsoft.Xna.Framework
{
    public class Game : IDisposable
    {
        const double MaximumElapsedTimeSeconds = 0.5;
        readonly List<IUpdateable> UpdateableComponents = new();
        readonly List<IUpdateable> CurrentlyUpdatingComponents = new();
        readonly List<IDrawable> DrawableComponents = new();
        readonly List<IDrawable> CurrentlyDrawingComponents = new();
        readonly List<IGameComponent> NotYetInitialized = new();
        IGraphicsDeviceManager GraphicsDeviceManager;
        IGraphicsDeviceService GraphicsDeviceService;
        GameHost Host;
        const int InactiveSleepMillis = 20;
        bool MouseVisible;
        bool InRun;
        readonly GameClock Clock;

        /// <summary>
        /// Total game time in seconds while the window has been active
        /// </summary>
        public double TotalGameTime { get; private set; }

        /// <summary>
        /// Elapsed game time since last update.
        /// If IsFixedTimeStep = true then this is locked to TargetTickInterval
        /// </summary>
        public double ElapsedFrameTime { get; private set; }

        /// <summary>
        /// This is the desired target interval between update ticks.
        /// Used when IsFixedTimeStep = true
        /// </summary>
        public double TargetTickInterval = 1.0 / 60.0; // the default is 60 fps

        /// <summary>
        /// If true, then `TargetTickInterval` is used to trigger updates
        /// in a fixed interval.
        /// Otherwise the game update loop will trigger as fast as possible.
        /// </summary>
        public bool IsFixedTimeStep { get; set; } = true;

        // accumulator for fixed time step logic
        double AccumulatedElapsedGameTime;

        bool DoneFirstUpdate;
        bool DoneFirstDraw;
        bool ForceElapsedTimeToZero;

        public GameComponentCollection Components { get; }
        public GameServiceContainer Services { get; } = new();

        public bool IsMouseVisible
        {
            get => MouseVisible;
            set
            {
                if (Window == null || MouseVisible == value)
                    return;
                MouseVisible = value;
                Window.IsMouseVisible = value;
            }
        }

        public GameWindow Window => Host?.Window;

        public bool IsActive
        {
            get
            {
                bool flag = false;
                if (GamerServicesDispatcher.IsInitialized)
                    flag = Guide.IsVisible;
                if (IsActiveIgnoringGuide)
                    return !flag;
                return false;
            }
        }

        public GraphicsDevice GraphicsDevice
        {
            get
            {
                if (GraphicsDeviceService == null)
                {
                    GraphicsDeviceService = Services.GetService(typeof(IGraphicsDeviceService)) as IGraphicsDeviceService;
                    if (GraphicsDeviceService == null)
                        throw new InvalidOperationException(Resources.NoGraphicsDeviceService);
                }
                return GraphicsDeviceService.GraphicsDevice;
            }
        }

        public ContentManager Content { get; set; }
        internal bool IsActiveIgnoringGuide { get; private set; }
        bool ShouldExit { get; set; }
        public bool GameOver => ShouldExit;

        public event EventHandler Activated;
        public event EventHandler Deactivated;
        public event EventHandler Exiting;
        public event EventHandler Disposed;

        public Game()
        {
            EnsureHost();
            Components = new();
            Components.ComponentAdded += GameComponentAdded;
            Components.ComponentRemoved += GameComponentRemoved;
            Content = new(Services);
            Clock = new();
        }

        ~Game()
        {
            Dispose(false);
        }

        public void CreateDevice()
        {
            GraphicsDeviceManager = Services.GetService(typeof(IGraphicsDeviceManager)) as IGraphicsDeviceManager;
            GraphicsDeviceManager?.CreateDevice();
            Initialize();
        }

        public void DoFirstUpdate()
        {
            ElapsedFrameTime = 0.0;
            Update(0f);
            DoneFirstUpdate = true;
        }

        public void Run()
        {
            try
            {
                CreateDevice();
                InRun = true;
                BeginRun();
                DoFirstUpdate();
                Host?.Run();
                EndRun();
            }
            catch (NoSuitableGraphicsDeviceException ex)
            {
                if (ShowMissingRequirementMessage(ex))
                    return;
                throw;
            }
            catch (NoAudioHardwareException ex)
            {
                if (ShowMissingRequirementMessage(ex))
                    return;
                throw;
            }
            finally
            {
                InRun = false;
            }
        }

        public void RunOne()
        {
            try
            {
                bool firstUpdate = false;
                if (GraphicsDeviceManager == null)
                {
                    CreateDevice();
                    firstUpdate = true;
                }
                InRun = true;
                if (firstUpdate)
                {
                    BeginRun();
                    DoFirstUpdate();
                }
                Host?.RunOne();
                //EndRun();
            }
            catch (NoSuitableGraphicsDeviceException ex)
            {
                if (ShowMissingRequirementMessage(ex))
                    return;
                throw;
            }
            catch (NoAudioHardwareException ex)
            {
                if (ShowMissingRequirementMessage(ex))
                    return;
                throw;
            }
            finally
            {
                InRun = false;
            }
        }

        public void Tick()
        {
            if (ShouldExit)
                return;

            if (!IsActiveIgnoringGuide)
            {
                Thread.Sleep(InactiveSleepMillis);
            }

            double elapsed = Clock.Step();

            if (ForceElapsedTimeToZero)
            {
                elapsed = 0.0;
                ForceElapsedTimeToZero = false;
            }

            // discard huge time deltas
            if (elapsed > MaximumElapsedTimeSeconds)
                elapsed = MaximumElapsedTimeSeconds;

            // fixed timestep ticks logic
            if (IsFixedTimeStep)
            {
                AccumulatedElapsedGameTime += elapsed;
                int numElapsedSteps = (int)(AccumulatedElapsedGameTime / TargetTickInterval);
                AccumulatedElapsedGameTime -= numElapsedSteps * TargetTickInterval;

                if (numElapsedSteps > 0)
                {
                    while (numElapsedSteps > 0 && !ShouldExit)
                    {
                        --numElapsedSteps;
                        try
                        {
                            ElapsedFrameTime = TargetTickInterval;
                            Update((float)TargetTickInterval);
                        }
                        finally
                        {
                            TotalGameTime += TargetTickInterval;
                        }
                    }

                    DrawFrame();
                }
            }
            else if (!ShouldExit)
            {
                try
                {
                    ElapsedFrameTime = elapsed;
                    Update((float)elapsed);
                }
                finally
                {
                    TotalGameTime += elapsed;
                }

                DrawFrame();
            }
        }

        public void Exit()
        {
            ShouldExit = true;
            Host.Exit();
        }

        protected virtual void BeginRun()
        {
        }

        protected virtual void EndRun()
        {
        }

        protected virtual void Update(float deltaTime)
        {
            for (int i = 0; i < UpdateableComponents.Count; ++i)
            {
                CurrentlyUpdatingComponents.Add(UpdateableComponents[i]);
            }
            for (int i = 0; i < CurrentlyUpdatingComponents.Count; ++i)
            {
                IUpdateable updatingComponent = CurrentlyUpdatingComponents[i];
                if (updatingComponent.Enabled)
                    updatingComponent.Update(deltaTime);
            }
            CurrentlyUpdatingComponents.Clear();
            FrameworkDispatcher.Update();
            DoneFirstUpdate = true;
        }

        protected virtual bool BeginDraw()
        {
            return GraphicsDeviceManager == null || GraphicsDeviceManager.BeginDraw();
        }

        protected virtual void Draw()
        {
            for (int i = 0; i < DrawableComponents.Count; ++i)
            {
                CurrentlyDrawingComponents.Add(DrawableComponents[i]);
            }
            for (int i = 0; i < CurrentlyDrawingComponents.Count; ++i)
            {
                IDrawable drawingComponent = CurrentlyDrawingComponents[i];
                if (drawingComponent.Visible)
                    drawingComponent.Draw();
            }
            CurrentlyDrawingComponents.Clear();
        }

        protected virtual void EndDraw()
        {
            GraphicsDeviceManager?.EndDraw();
        }

        void Paint(object sender, EventArgs e)
        {
            if (!DoneFirstDraw)
                return;
            DrawFrame();
        }

        protected virtual void Initialize()
        {
            HookDeviceEvents();
            while (NotYetInitialized.Count != 0)
            {
                NotYetInitialized[0].Initialize();
                NotYetInitialized.RemoveAt(0);
            }
            if (GraphicsDeviceService?.GraphicsDevice == null)
                return;
            LoadContent();
        }

        public void ResetElapsedTime()
        {
            ForceElapsedTimeToZero = true;
        }

        public void EndingGame(bool start)
        {
            ShouldExit = start;
        }

        void DrawFrame()
        {
            try
            {
                if (ShouldExit || !DoneFirstUpdate || (Window.IsMinimized || !BeginDraw()))
                    return;

                Draw();
                EndDraw();
                DoneFirstDraw = true;
            }
            catch
            {
            }
        }

        void GameComponentRemoved(object sender, GameComponentCollectionEventArgs e)
        {
            if (!InRun)
                NotYetInitialized.Remove(e.GameComponent);
            if (e.GameComponent is IUpdateable updateable)
            {
                UpdateableComponents.Remove(updateable);
            }
            if (e.GameComponent is IDrawable drawable)
            {
                DrawableComponents.Remove(drawable);
            }
        }

        void GameComponentAdded(object sender, GameComponentCollectionEventArgs e)
        {
            if (InRun)
                e.GameComponent.Initialize();
            else
                NotYetInitialized.Add(e.GameComponent);

            if (e.GameComponent is IUpdateable updateable)
            {
                int num = UpdateableComponents.BinarySearch(updateable, UpdateOrderComparer.Default);
                if (num < 0)
                {
                    int i = ~num;
                    while (i < UpdateableComponents.Count && UpdateableComponents[i].UpdateOrder == updateable.UpdateOrder)
                        ++i;
                    UpdateableComponents.Insert(i, updateable);
                }
            }
            if (e.GameComponent is IDrawable drawable)
            {
                int num1 = DrawableComponents.BinarySearch(drawable, DrawOrderComparer.Default);
                if (num1 < 0)
                {
                    int i = ~num1;
                    while (i < DrawableComponents.Count && DrawableComponents[i].DrawOrder == drawable.DrawOrder)
                        ++i;
                    DrawableComponents.Insert(i, drawable);
                }
            }
        }

        protected virtual void OnActivated(object sender, EventArgs args)
        {
            Activated?.Invoke(this, args);
        }

        protected virtual void OnDeactivated(object sender, EventArgs args)
        {
            Deactivated?.Invoke(this, args);
        }

        protected virtual void OnExiting(object sender, EventArgs args)
        {
            Exiting?.Invoke(null, args);
        }

        public void CreateWindow()
        {
            Host = new WindowsGameHost(this);
            Host.Activated   += HostActivated;
            Host.Deactivated += HostDeactivated;
            Host.Suspend     += HostSuspend;
            Host.Resume      += HostResume;
            Host.Idle        += HostIdle;
            Host.Exiting     += HostExiting;
            Host.Window.Paint += Paint;
        }

        void EnsureHost()
        {
            if (Host == null)
                CreateWindow();
        }

        void HostSuspend(object sender, EventArgs e)
        {
            Clock.Suspend();
        }

        void HostResume(object sender, EventArgs e)
        {
            Clock.Resume();
        }

        void HostExiting(object sender, EventArgs e)
        {
            OnExiting(this, EventArgs.Empty);
        }

        void HostIdle(object sender, EventArgs e)
        {
            Tick();
        }

        void HostDeactivated(object sender, EventArgs e)
        {
            if (!IsActiveIgnoringGuide)
                return;
            IsActiveIgnoringGuide = false;
            OnDeactivated(this, EventArgs.Empty);
        }

        void HostActivated(object sender, EventArgs e)
        {
            if (IsActiveIgnoringGuide)
                return;
            IsActiveIgnoringGuide = true;
            OnActivated(this, EventArgs.Empty);
        }

        void HookDeviceEvents()
        {
            GraphicsDeviceService = Services.GetService(typeof(IGraphicsDeviceService)) as IGraphicsDeviceService;
            if (GraphicsDeviceService == null)
                return;
            GraphicsDeviceService.DeviceCreated   += DeviceCreated;
            GraphicsDeviceService.DeviceResetting += DeviceResetting;
            GraphicsDeviceService.DeviceReset     += DeviceReset;
            GraphicsDeviceService.DeviceDisposing += DeviceDisposing;
        }

        void UnhookDeviceEvents()
        {
            if (GraphicsDeviceService == null)
                return;
            GraphicsDeviceService.DeviceCreated   -= DeviceCreated;
            GraphicsDeviceService.DeviceResetting -= DeviceResetting;
            GraphicsDeviceService.DeviceReset     -= DeviceReset;
            GraphicsDeviceService.DeviceDisposing -= DeviceDisposing;
        }

        void DeviceResetting(object sender, EventArgs e)
        {
        }

        void DeviceReset(object sender, EventArgs e)
        {
        }

        void DeviceCreated(object sender, EventArgs e)
        {
            LoadContent();
        }

        void DeviceDisposing(object sender, EventArgs e)
        {
            Content?.Unload();
            UnloadContent();
        }

        protected virtual void LoadContent()
        {
        }

        protected virtual void UnloadContent()
        {
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (!disposing)
                return;
            lock (this)
            {
                var components = new IGameComponent[Components.Count];
                Components.CopyTo(components, 0);
                for (int i = 0; i < components.Length; ++i)
                {
                    (components[i] as IDisposable)?.Dispose();
                }
                (GraphicsDeviceManager as IDisposable)?.Dispose();
                UnhookDeviceEvents();
                Disposed?.Invoke(this, EventArgs.Empty);
            }
        }

        protected virtual bool ShowMissingRequirementMessage(Exception exception)
        {
            return Host != null && Host.ShowMissingRequirementMessage(exception);
        }
    }
}
