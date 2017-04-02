// Decompiled with JetBrains decompiler
// Type: Microsoft.Xna.Framework.Game
// Assembly: Microsoft.Xna.Framework.Game, Version=3.1.0.0, Culture=neutral, PublicKeyToken=6d5c3888ef60e27d
// MVID: E4BD910E-73ED-465E-A91E-14AAAB0CE109
// Assembly location: C:\WINDOWS\assembly\GAC_32\Microsoft.Xna.Framework.Game\3.1.0.0__6d5c3888ef60e27d\Microsoft.Xna.Framework.Game.dll

using Microsoft.Xna.Framework.Audio;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.GamerServices;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Threading;

namespace Microsoft.Xna.Framework
{
    public class Game : IDisposable
    {
        private readonly TimeSpan maximumElapsedTime = TimeSpan.FromMilliseconds(500.0);
        private GameTime gameTime = new GameTime();
        private int updatesSinceRunningSlowly1 = int.MaxValue;
        private int updatesSinceRunningSlowly2 = int.MaxValue;
        private readonly List<IUpdateable> updateableComponents = new List<IUpdateable>();
        private readonly List<IUpdateable> currentlyUpdatingComponents = new List<IUpdateable>();
        private readonly List<IDrawable> drawableComponents = new List<IDrawable>();
        private readonly List<IDrawable> currentlyDrawingComponents = new List<IDrawable>();
        private readonly List<IGameComponent> notYetInitialized = new List<IGameComponent>();
        private IGraphicsDeviceManager graphicsDeviceManager;
        private IGraphicsDeviceService graphicsDeviceService;
        private GameHost host;
        private TimeSpan inactiveSleepTime;
        private bool isMouseVisible;
        private bool inRun;
        private readonly GameClock clock;
        private TimeSpan lastFrameElapsedRealTime;
        private TimeSpan totalGameTime;
        private TimeSpan targetElapsedTime;
        private TimeSpan accumulatedElapsedGameTime;
        private TimeSpan lastFrameElapsedGameTime;
        private bool drawRunningSlowly;
        private bool doneFirstUpdate;
        private bool doneFirstDraw;
        private bool forceElapsedTimeToZero;
        private bool suppressDraw;

        public GameComponentCollection Components { get; }
        public GameServiceContainer Services { get; } = new GameServiceContainer();

        public TimeSpan InactiveSleepTime
        {
            get
            {
                return inactiveSleepTime;
            }
            set
            {
                if (value < TimeSpan.Zero)
                    throw new ArgumentOutOfRangeException(nameof(value), Resources.InactiveSleepTimeCannotBeZero);
                inactiveSleepTime = value;
            }
        }

        public bool IsMouseVisible
        {
            get
            {
                return isMouseVisible;
            }
            set
            {
                isMouseVisible = value;
                if (Window == null)
                    return;
                Window.IsMouseVisible = value;
            }
        }

        public TimeSpan TargetElapsedTime
        {
            get
            {
                return targetElapsedTime;
            }
            set
            {
                if (value <= TimeSpan.Zero)
                    throw new ArgumentOutOfRangeException(nameof(value), Resources.TargetElaspedCannotBeZero);
                targetElapsedTime = value;
            }
        }

        public bool IsFixedTimeStep { get; set; } = true;

        public GameWindow Window => host?.Window;

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
                if (graphicsDeviceService == null)
                {
                    graphicsDeviceService = Services.GetService(typeof(IGraphicsDeviceService)) as IGraphicsDeviceService;
                    if (graphicsDeviceService == null)
                        throw new InvalidOperationException(Resources.NoGraphicsDeviceService);
                }
                return graphicsDeviceService.GraphicsDevice;
            }
        }

        public ContentManager Content { get; set; }
        internal bool IsActiveIgnoringGuide { get; private set; }
        private bool ShouldExit { get; set; }

        public event EventHandler Activated;
        public event EventHandler Deactivated;
        public event EventHandler Exiting;
        public event EventHandler Disposed;

        public Game()
        {
            EnsureHost();
            Components             = new GameComponentCollection();
            Components.ComponentAdded += GameComponentAdded;
            Components.ComponentRemoved += GameComponentRemoved;
            Content                    = new ContentManager(Services);
            host.Window.Paint += Paint;
            clock                      = new GameClock();
            totalGameTime              = TimeSpan.Zero;
            accumulatedElapsedGameTime = TimeSpan.Zero;
            lastFrameElapsedGameTime   = TimeSpan.Zero;
            targetElapsedTime          = TimeSpan.FromTicks(166667L);
            inactiveSleepTime          = TimeSpan.FromMilliseconds(20.0);
        }

        ~Game()
        {
            Dispose(false);
        }

        public void Run()
        {
            try
            {
                graphicsDeviceManager = Services.GetService(typeof(IGraphicsDeviceManager)) as IGraphicsDeviceManager;
                graphicsDeviceManager?.CreateDevice();
                Initialize();
                inRun = true;
                BeginRun();
                gameTime.ElapsedGameTime = TimeSpan.Zero;
                gameTime.ElapsedRealTime = TimeSpan.Zero;
                gameTime.TotalGameTime = totalGameTime;
                gameTime.TotalRealTime = clock.CurrentTime;
                gameTime.IsRunningSlowly = false;
                Update(gameTime);
                doneFirstUpdate = true;
                host?.Run();
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
                inRun = false;
            }
        }

        public void Tick()
        {
            if (ShouldExit)
                return;
            if (!IsActiveIgnoringGuide)
            {
                Thread.Sleep((int)inactiveSleepTime.TotalMilliseconds);
            }

            clock.Step();
            bool flag = true;
            gameTime.TotalRealTime   = clock.CurrentTime;
            gameTime.ElapsedRealTime = clock.ElapsedTime;
            lastFrameElapsedRealTime += clock.ElapsedTime;
            TimeSpan timeSpan1 = clock.ElapsedAdjustedTime;
            if (timeSpan1 < TimeSpan.Zero)
                timeSpan1 = TimeSpan.Zero;
            if (forceElapsedTimeToZero)
            {
                TimeSpan zero;
                timeSpan1 = zero = TimeSpan.Zero;
                lastFrameElapsedRealTime = zero;
                gameTime.ElapsedRealTime = zero;
                forceElapsedTimeToZero = false;
            }
            if (timeSpan1 > maximumElapsedTime)
                timeSpan1 = maximumElapsedTime;
            if (IsFixedTimeStep)
            {
                if (Math.Abs(timeSpan1.Ticks - targetElapsedTime.Ticks) < targetElapsedTime.Ticks >> 6)
                    timeSpan1 = targetElapsedTime;
                accumulatedElapsedGameTime += timeSpan1;
                long num = accumulatedElapsedGameTime.Ticks / targetElapsedTime.Ticks;
                accumulatedElapsedGameTime = new TimeSpan(accumulatedElapsedGameTime.Ticks % targetElapsedTime.Ticks);
                lastFrameElapsedGameTime = TimeSpan.Zero;
                if (num == 0L)
                    return;
                TimeSpan targetElapsed = targetElapsedTime;
                if (num > 1L)
                {
                    updatesSinceRunningSlowly2 = updatesSinceRunningSlowly1;
                    updatesSinceRunningSlowly1 = 0;
                }
                else
                {
                    if (updatesSinceRunningSlowly1 < int.MaxValue) ++updatesSinceRunningSlowly1;
                    if (updatesSinceRunningSlowly2 < int.MaxValue) ++updatesSinceRunningSlowly2;
                }
                drawRunningSlowly = updatesSinceRunningSlowly2 < 20;
                while (num > 0L && !ShouldExit)
                {
                    --num;
                    try
                    {
                        gameTime.ElapsedGameTime = targetElapsed;
                        gameTime.TotalGameTime   = totalGameTime;
                        gameTime.IsRunningSlowly = drawRunningSlowly;
                        Update(gameTime);
                        flag &= suppressDraw;
                        suppressDraw = false;
                    }
                    finally
                    {
                        lastFrameElapsedGameTime += targetElapsed;
                        totalGameTime            += targetElapsed;
                    }
                }
            }
            else
            {
                drawRunningSlowly = false;
                updatesSinceRunningSlowly1 = int.MaxValue;
                updatesSinceRunningSlowly2 = int.MaxValue;
                if (!ShouldExit)
                {
                    try
                    {
                        gameTime.ElapsedGameTime = lastFrameElapsedGameTime = timeSpan1;
                        gameTime.TotalGameTime   = totalGameTime;
                        gameTime.IsRunningSlowly = false;
                        Update(gameTime);
                        flag &= suppressDraw;
                        suppressDraw = false;
                    }
                    finally
                    {
                        totalGameTime += timeSpan1;
                    }
                }
            }
            if (flag)
                return;
            DrawFrame();
        }

        public void SuppressDraw()
        {
            suppressDraw = true;
        }

        public void Exit()
        {
            ShouldExit = true;
            host.Exit();
        }

        protected virtual void BeginRun()
        {
        }

        protected virtual void EndRun()
        {
        }

        protected virtual void Update(GameTime time)
        {
            for (int i = 0; i < updateableComponents.Count; ++i)
            {
                currentlyUpdatingComponents.Add(updateableComponents[i]);
            }
            for (int i = 0; i < currentlyUpdatingComponents.Count; ++i)
            {
                IUpdateable updatingComponent = currentlyUpdatingComponents[i];
                if (updatingComponent.Enabled)
                    updatingComponent.Update(time);
            }
            currentlyUpdatingComponents.Clear();
            FrameworkDispatcher.Update();
            doneFirstUpdate = true;
        }

        protected virtual bool BeginDraw()
        {
            return graphicsDeviceManager == null || graphicsDeviceManager.BeginDraw();
        }

        protected virtual void Draw(GameTime time)
        {
            for (int i = 0; i < drawableComponents.Count; ++i)
            {
                currentlyDrawingComponents.Add(drawableComponents[i]);
            }
            for (int i = 0; i < currentlyDrawingComponents.Count; ++i)
            {
                IDrawable drawingComponent = currentlyDrawingComponents[i];
                if (drawingComponent.Visible)
                    drawingComponent.Draw(time);
            }
            currentlyDrawingComponents.Clear();
        }

        protected virtual void EndDraw()
        {
            graphicsDeviceManager?.EndDraw();
        }

        private void Paint(object sender, EventArgs e)
        {
            if (!doneFirstDraw)
                return;
            DrawFrame();
        }

        protected virtual void Initialize()
        {
            HookDeviceEvents();
            while (notYetInitialized.Count != 0)
            {
                notYetInitialized[0].Initialize();
                notYetInitialized.RemoveAt(0);
            }
            if (graphicsDeviceService?.GraphicsDevice == null)
                return;
            LoadContent();
        }

        public void ResetElapsedTime()
        {
            forceElapsedTimeToZero = true;
            drawRunningSlowly = false;
            updatesSinceRunningSlowly1 = int.MaxValue;
            updatesSinceRunningSlowly2 = int.MaxValue;
        }

        private void DrawFrame()
        {
            try
            {
                if (ShouldExit || !doneFirstUpdate || (Window.IsMinimized || !BeginDraw()))
                    return;
                gameTime.TotalRealTime = clock.CurrentTime;
                gameTime.ElapsedRealTime = lastFrameElapsedRealTime;
                gameTime.TotalGameTime = totalGameTime;
                gameTime.ElapsedGameTime = lastFrameElapsedGameTime;
                gameTime.IsRunningSlowly = drawRunningSlowly;
                Draw(gameTime);
                EndDraw();
                doneFirstDraw = true;
            }
            finally
            {
                lastFrameElapsedRealTime = TimeSpan.Zero;
                lastFrameElapsedGameTime = TimeSpan.Zero;
            }
        }

        private void GameComponentRemoved(object sender, GameComponentCollectionEventArgs e)
        {
            if (!inRun)
                notYetInitialized.Remove(e.GameComponent);
            if (e.GameComponent is IUpdateable updateable)
            {
                updateableComponents.Remove(updateable);
                updateable.UpdateOrderChanged -= UpdateableUpdateOrderChanged;
            }
            if (e.GameComponent is IDrawable drawable)
            {
                drawableComponents.Remove(drawable);
                drawable.DrawOrderChanged -= DrawableDrawOrderChanged;
            }
        }

        private void GameComponentAdded(object sender, GameComponentCollectionEventArgs e)
        {
            if (inRun)
                e.GameComponent.Initialize();
            else
                notYetInitialized.Add(e.GameComponent);

            if (e.GameComponent is IUpdateable updateable)
            {
                int num = updateableComponents.BinarySearch(updateable, UpdateOrderComparer.Default);
                if (num < 0)
                {
                    int i = ~num;
                    while (i < updateableComponents.Count && updateableComponents[i].UpdateOrder == updateable.UpdateOrder)
                        ++i;
                    updateableComponents.Insert(i, updateable);
                    updateable.UpdateOrderChanged += UpdateableUpdateOrderChanged;
                }
            }
            if (e.GameComponent is IDrawable drawable)
            {
                int num1 = drawableComponents.BinarySearch(drawable, DrawOrderComparer.Default);
                if (num1 < 0)
                {
                    int i = ~num1;
                    while (i < drawableComponents.Count && drawableComponents[i].DrawOrder == drawable.DrawOrder)
                        ++i;
                    drawableComponents.Insert(i, drawable);
                    drawable.DrawOrderChanged += DrawableDrawOrderChanged;
                }
            }
        }

        private void DrawableDrawOrderChanged(object sender, EventArgs e)
        {
            if (sender is IDrawable drawable)
            {
                drawableComponents.Remove(drawable);
                int num = drawableComponents.BinarySearch(drawable, DrawOrderComparer.Default);
                if (num < 0)
                {
                    int i = ~num;
                    while (i < drawableComponents.Count && drawableComponents[i].DrawOrder == drawable.DrawOrder)
                        ++i;
                    drawableComponents.Insert(i, drawable);
                }
            }
        }

        private void UpdateableUpdateOrderChanged(object sender, EventArgs e)
        {
            if (sender is IUpdateable updateable)
            {
                updateableComponents.Remove(updateable);
                int num = updateableComponents.BinarySearch(updateable, UpdateOrderComparer.Default);
                if (num < 0)
                {
                    int i = ~num;
                    while (i < updateableComponents.Count && updateableComponents[i].UpdateOrder == updateable.UpdateOrder)
                        ++i;
                    updateableComponents.Insert(i, updateable);
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

        private void EnsureHost()
        {
            if (host != null)
                return;
            host = new WindowsGameHost(this);
            host.Activated   += HostActivated;
            host.Deactivated += HostDeactivated;
            host.Suspend     += HostSuspend;
            host.Resume      += HostResume;
            host.Idle        += HostIdle;
            host.Exiting     += HostExiting;
        }

        private void HostSuspend(object sender, EventArgs e)
        {
            clock.Suspend();
        }

        private void HostResume(object sender, EventArgs e)
        {
            clock.Resume();
        }

        private void HostExiting(object sender, EventArgs e)
        {
            OnExiting((object)this, EventArgs.Empty);
        }

        private void HostIdle(object sender, EventArgs e)
        {
            Tick();
        }

        private void HostDeactivated(object sender, EventArgs e)
        {
            if (!IsActiveIgnoringGuide)
                return;
            IsActiveIgnoringGuide = false;
            OnDeactivated(this, EventArgs.Empty);
        }

        private void HostActivated(object sender, EventArgs e)
        {
            if (IsActiveIgnoringGuide)
                return;
            IsActiveIgnoringGuide = true;
            OnActivated(this, EventArgs.Empty);
        }

        private void HookDeviceEvents()
        {
            graphicsDeviceService = Services.GetService(typeof(IGraphicsDeviceService)) as IGraphicsDeviceService;
            if (graphicsDeviceService == null)
                return;
            graphicsDeviceService.DeviceCreated   += DeviceCreated;
            graphicsDeviceService.DeviceResetting += DeviceResetting;
            graphicsDeviceService.DeviceReset     += DeviceReset;
            graphicsDeviceService.DeviceDisposing += DeviceDisposing;
        }

        private void UnhookDeviceEvents()
        {
            if (graphicsDeviceService == null)
                return;
            graphicsDeviceService.DeviceCreated   -= DeviceCreated;
            graphicsDeviceService.DeviceResetting -= DeviceResetting;
            graphicsDeviceService.DeviceReset     -= DeviceReset;
            graphicsDeviceService.DeviceDisposing -= DeviceDisposing;
        }

        private void DeviceResetting(object sender, EventArgs e)
        {
        }

        private void DeviceReset(object sender, EventArgs e)
        {
        }

        private void DeviceCreated(object sender, EventArgs e)
        {
            LoadContent();
        }

        private void DeviceDisposing(object sender, EventArgs e)
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
                (graphicsDeviceManager as IDisposable)?.Dispose();
                UnhookDeviceEvents();
                Disposed?.Invoke(this, EventArgs.Empty);
            }
        }

        protected virtual bool ShowMissingRequirementMessage(Exception exception)
        {
            return host != null && host.ShowMissingRequirementMessage(exception);
        }
    }
}
