﻿// Decompiled with JetBrains decompiler
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
        private readonly TimeSpan MaximumElapsedTime = TimeSpan.FromMilliseconds(500.0);
        private readonly GameTime Time = new GameTime();
        private int UpdatesSinceRunningSlowly1 = int.MaxValue;
        private int UpdatesSinceRunningSlowly2 = int.MaxValue;
        private readonly List<IUpdateable> UpdateableComponents = new List<IUpdateable>();
        private readonly List<IUpdateable> CurrentlyUpdatingComponents = new List<IUpdateable>();
        private readonly List<IDrawable> DrawableComponents = new List<IDrawable>();
        private readonly List<IDrawable> CurrentlyDrawingComponents = new List<IDrawable>();
        private readonly List<IGameComponent> NotYetInitialized = new List<IGameComponent>();
        private IGraphicsDeviceManager GraphicsDeviceManager;
        private IGraphicsDeviceService GraphicsDeviceService;
        private GameHost Host;
        private TimeSpan InactiveSleep;
        private bool MouseVisible;
        private bool InRun;
        private readonly GameClock Clock;
        private TimeSpan LastFrameElapsedRealTime;
        private TimeSpan TotalGameTime;
        private TimeSpan TargetElapsedGameTime;
        private TimeSpan AccumulatedElapsedGameTime;
        private TimeSpan LastFrameElapsedGameTime;
        private bool DrawRunningSlowly;
        private bool DoneFirstUpdate;
        private bool DoneFirstDraw;
        private bool ForceElapsedTimeToZero;
        private bool DrawSuppressed;

        public GameComponentCollection Components { get; }
        public GameServiceContainer Services { get; } = new GameServiceContainer();

        public TimeSpan InactiveSleepTime
        {
            get => InactiveSleep;
            set
            {
                if (value < TimeSpan.Zero)
                    throw new ArgumentOutOfRangeException(nameof(value), Resources.InactiveSleepTimeCannotBeZero);
                InactiveSleep = value;
            }
        }

        public bool IsMouseVisible
        {
            get => MouseVisible;
            set
            {
                MouseVisible = value;
                if (Window == null)
                    return;
                Window.IsMouseVisible = value;
            }
        }

        public TimeSpan TargetElapsedTime
        {
            get => TargetElapsedGameTime;
            set
            {
                if (value <= TimeSpan.Zero)
                    throw new ArgumentOutOfRangeException(nameof(value), Resources.TargetElaspedCannotBeZero);
                TargetElapsedGameTime = value;
            }
        }

        public bool IsFixedTimeStep { get; set; } = true;

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
            Host.Window.Paint += Paint;
            Clock                      = new GameClock();
            TotalGameTime              = TimeSpan.Zero;
            AccumulatedElapsedGameTime = TimeSpan.Zero;
            LastFrameElapsedGameTime   = TimeSpan.Zero;
            TargetElapsedGameTime          = TimeSpan.FromTicks(166667L);
            InactiveSleep          = TimeSpan.FromMilliseconds(20.0);
        }

        ~Game()
        {
            Dispose(false);
        }

        public void Run()
        {
            try
            {
                GraphicsDeviceManager = Services.GetService(typeof(IGraphicsDeviceManager)) as IGraphicsDeviceManager;
                GraphicsDeviceManager?.CreateDevice();
                Initialize();
                InRun = true;
                BeginRun();
                Time.ElapsedGameTime = TimeSpan.Zero;
                Time.ElapsedRealTime = TimeSpan.Zero;
                Time.TotalGameTime = TotalGameTime;
                Time.TotalRealTime = Clock.CurrentTime;
                Time.IsRunningSlowly = false;
                Update(Time);
                DoneFirstUpdate = true;
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

        public void Tick()
        {
            if (ShouldExit)
                return;
            if (!IsActiveIgnoringGuide)
            {
                Thread.Sleep((int)InactiveSleep.TotalMilliseconds);
            }

            Clock.Step();
            bool flag = true;
            Time.TotalRealTime   = Clock.CurrentTime;
            Time.ElapsedRealTime = Clock.ElapsedTime;
            LastFrameElapsedRealTime += Clock.ElapsedTime;
            TimeSpan timeSpan1 = Clock.ElapsedAdjustedTime;
            if (timeSpan1 < TimeSpan.Zero)
                timeSpan1 = TimeSpan.Zero;
            if (ForceElapsedTimeToZero)
            {
                TimeSpan zero;
                timeSpan1 = zero = TimeSpan.Zero;
                LastFrameElapsedRealTime = zero;
                Time.ElapsedRealTime = zero;
                ForceElapsedTimeToZero = false;
            }
            if (timeSpan1 > MaximumElapsedTime)
                timeSpan1 = MaximumElapsedTime;
            if (IsFixedTimeStep)
            {
                if (Math.Abs(timeSpan1.Ticks - TargetElapsedGameTime.Ticks) < TargetElapsedGameTime.Ticks >> 6)
                    timeSpan1 = TargetElapsedGameTime;
                AccumulatedElapsedGameTime += timeSpan1;
                long num = AccumulatedElapsedGameTime.Ticks / TargetElapsedGameTime.Ticks;
                AccumulatedElapsedGameTime = new TimeSpan(AccumulatedElapsedGameTime.Ticks % TargetElapsedGameTime.Ticks);
                LastFrameElapsedGameTime = TimeSpan.Zero;
                if (num == 0L)
                    return;
                TimeSpan targetElapsed = TargetElapsedGameTime;
                if (num > 1L)
                {
                    UpdatesSinceRunningSlowly2 = UpdatesSinceRunningSlowly1;
                    UpdatesSinceRunningSlowly1 = 0;
                }
                else
                {
                    if (UpdatesSinceRunningSlowly1 < int.MaxValue) ++UpdatesSinceRunningSlowly1;
                    if (UpdatesSinceRunningSlowly2 < int.MaxValue) ++UpdatesSinceRunningSlowly2;
                }
                DrawRunningSlowly = UpdatesSinceRunningSlowly2 < 20;
                while (num > 0L && !ShouldExit)
                {
                    --num;
                    try
                    {
                        Time.ElapsedGameTime = targetElapsed;
                        Time.TotalGameTime   = TotalGameTime;
                        Time.IsRunningSlowly = DrawRunningSlowly;
                        Update(Time);
                        flag &= DrawSuppressed;
                        DrawSuppressed = false;
                    }
                    finally
                    {
                        LastFrameElapsedGameTime += targetElapsed;
                        TotalGameTime            += targetElapsed;
                    }
                }
            }
            else
            {
                DrawRunningSlowly = false;
                UpdatesSinceRunningSlowly1 = int.MaxValue;
                UpdatesSinceRunningSlowly2 = int.MaxValue;
                if (!ShouldExit)
                {
                    try
                    {
                        Time.ElapsedGameTime = LastFrameElapsedGameTime = timeSpan1;
                        Time.TotalGameTime   = TotalGameTime;
                        Time.IsRunningSlowly = false;
                        Update(Time);
                        flag &= DrawSuppressed;
                        DrawSuppressed = false;
                    }
                    finally
                    {
                        TotalGameTime += timeSpan1;
                    }
                }
            }
            if (flag)
                return;
            DrawFrame();
        }

        public void SuppressDraw()
        {
            DrawSuppressed = true;
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

        protected virtual void Update(GameTime time)
        {
            for (int i = 0; i < UpdateableComponents.Count; ++i)
            {
                CurrentlyUpdatingComponents.Add(UpdateableComponents[i]);
            }
            for (int i = 0; i < CurrentlyUpdatingComponents.Count; ++i)
            {
                IUpdateable updatingComponent = CurrentlyUpdatingComponents[i];
                if (updatingComponent.Enabled)
                    updatingComponent.Update(time);
            }
            CurrentlyUpdatingComponents.Clear();
            FrameworkDispatcher.Update();
            DoneFirstUpdate = true;
        }

        protected virtual bool BeginDraw()
        {
            return GraphicsDeviceManager == null || GraphicsDeviceManager.BeginDraw();
        }

        protected virtual void Draw(GameTime time)
        {
            for (int i = 0; i < DrawableComponents.Count; ++i)
            {
                CurrentlyDrawingComponents.Add(DrawableComponents[i]);
            }
            for (int i = 0; i < CurrentlyDrawingComponents.Count; ++i)
            {
                IDrawable drawingComponent = CurrentlyDrawingComponents[i];
                if (drawingComponent.Visible)
                    drawingComponent.Draw(time);
            }
            CurrentlyDrawingComponents.Clear();
        }

        protected virtual void EndDraw()
        {
            GraphicsDeviceManager?.EndDraw();
        }

        private void Paint(object sender, EventArgs e)
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
            DrawRunningSlowly = false;
            UpdatesSinceRunningSlowly1 = int.MaxValue;
            UpdatesSinceRunningSlowly2 = int.MaxValue;
        }
        public void EndingGame(bool start)
        {
            ShouldExit = start;
        }
        private void DrawFrame()
        {
            try
            {
                if (ShouldExit || !DoneFirstUpdate || (Window.IsMinimized || !BeginDraw()))
                    return;
                Time.TotalRealTime = Clock.CurrentTime;
                Time.ElapsedRealTime = LastFrameElapsedRealTime;
                Time.TotalGameTime = TotalGameTime;
                Time.ElapsedGameTime = LastFrameElapsedGameTime;
                Time.IsRunningSlowly = DrawRunningSlowly;
                Draw(Time);
                EndDraw();
                DoneFirstDraw = true;
            }
            finally
            {
                LastFrameElapsedRealTime = TimeSpan.Zero;
                LastFrameElapsedGameTime = TimeSpan.Zero;
            }
        }

        private void GameComponentRemoved(object sender, GameComponentCollectionEventArgs e)
        {
            if (!InRun)
                NotYetInitialized.Remove(e.GameComponent);
            if (e.GameComponent is IUpdateable updateable)
            {
                UpdateableComponents.Remove(updateable);
                updateable.UpdateOrderChanged -= UpdateableUpdateOrderChanged;
            }
            if (e.GameComponent is IDrawable drawable)
            {
                DrawableComponents.Remove(drawable);
                drawable.DrawOrderChanged -= DrawableDrawOrderChanged;
            }
        }

        private void GameComponentAdded(object sender, GameComponentCollectionEventArgs e)
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
                    updateable.UpdateOrderChanged += UpdateableUpdateOrderChanged;
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
                    drawable.DrawOrderChanged += DrawableDrawOrderChanged;
                }
            }
        }

        private void DrawableDrawOrderChanged(object sender, EventArgs e)
        {
            if (sender is IDrawable drawable)
            {
                DrawableComponents.Remove(drawable);
                int num = DrawableComponents.BinarySearch(drawable, DrawOrderComparer.Default);
                if (num < 0)
                {
                    int i = ~num;
                    while (i < DrawableComponents.Count && DrawableComponents[i].DrawOrder == drawable.DrawOrder)
                        ++i;
                    DrawableComponents.Insert(i, drawable);
                }
            }
        }

        private void UpdateableUpdateOrderChanged(object sender, EventArgs e)
        {
            if (sender is IUpdateable updateable)
            {
                UpdateableComponents.Remove(updateable);
                int num = UpdateableComponents.BinarySearch(updateable, UpdateOrderComparer.Default);
                if (num < 0)
                {
                    int i = ~num;
                    while (i < UpdateableComponents.Count && UpdateableComponents[i].UpdateOrder == updateable.UpdateOrder)
                        ++i;
                    UpdateableComponents.Insert(i, updateable);
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
            if (Host != null)
                return;
            Host = new WindowsGameHost(this);
            Host.Activated   += HostActivated;
            Host.Deactivated += HostDeactivated;
            Host.Suspend     += HostSuspend;
            Host.Resume      += HostResume;
            Host.Idle        += HostIdle;
            Host.Exiting     += HostExiting;
        }

        private void HostSuspend(object sender, EventArgs e)
        {
            Clock.Suspend();
        }

        private void HostResume(object sender, EventArgs e)
        {
            Clock.Resume();
        }

        private void HostExiting(object sender, EventArgs e)
        {
            OnExiting(this, EventArgs.Empty);
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
            GraphicsDeviceService = Services.GetService(typeof(IGraphicsDeviceService)) as IGraphicsDeviceService;
            if (GraphicsDeviceService == null)
                return;
            GraphicsDeviceService.DeviceCreated   += DeviceCreated;
            GraphicsDeviceService.DeviceResetting += DeviceResetting;
            GraphicsDeviceService.DeviceReset     += DeviceReset;
            GraphicsDeviceService.DeviceDisposing += DeviceDisposing;
        }

        private void UnhookDeviceEvents()
        {
            if (GraphicsDeviceService == null)
                return;
            GraphicsDeviceService.DeviceCreated   -= DeviceCreated;
            GraphicsDeviceService.DeviceResetting -= DeviceResetting;
            GraphicsDeviceService.DeviceReset     -= DeviceReset;
            GraphicsDeviceService.DeviceDisposing -= DeviceDisposing;
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
