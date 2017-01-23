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
    private bool isFixedTimeStep = true;
    private int updatesSinceRunningSlowly1 = int.MaxValue;
    private int updatesSinceRunningSlowly2 = int.MaxValue;
    private List<IUpdateable> updateableComponents = new List<IUpdateable>();
    private List<IUpdateable> currentlyUpdatingComponents = new List<IUpdateable>();
    private List<IDrawable> drawableComponents = new List<IDrawable>();
    private List<IDrawable> currentlyDrawingComponents = new List<IDrawable>();
    private List<IGameComponent> notYetInitialized = new List<IGameComponent>();
    private GameServiceContainer gameServices = new GameServiceContainer();
    private IGraphicsDeviceManager graphicsDeviceManager;
    private IGraphicsDeviceService graphicsDeviceService;
    private GameHost host;
    private bool isActive;
    private bool exitRequested;
    private TimeSpan inactiveSleepTime;
    private bool isMouseVisible;
    private bool inRun;
    private GameClock clock;
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
    private GameComponentCollection gameComponents;
    private ContentManager content;

    public GameComponentCollection Components
    {
      get
      {
        return this.gameComponents;
      }
    }

    public GameServiceContainer Services
    {
      get
      {
        return this.gameServices;
      }
    }

    public TimeSpan InactiveSleepTime
    {
      get
      {
        return this.inactiveSleepTime;
      }
      set
      {
        if (value < TimeSpan.Zero)
          throw new ArgumentOutOfRangeException("value", Resources.InactiveSleepTimeCannotBeZero);
        this.inactiveSleepTime = value;
      }
    }

    public bool IsMouseVisible
    {
      get
      {
        return this.isMouseVisible;
      }
      set
      {
        this.isMouseVisible = value;
        if (this.Window == null)
          return;
        this.Window.IsMouseVisible = value;
      }
    }

    public TimeSpan TargetElapsedTime
    {
      get
      {
        return this.targetElapsedTime;
      }
      set
      {
        if (value <= TimeSpan.Zero)
          throw new ArgumentOutOfRangeException("value", Resources.TargetElaspedCannotBeZero);
        this.targetElapsedTime = value;
      }
    }

    public bool IsFixedTimeStep
    {
      get
      {
        return this.isFixedTimeStep;
      }
      set
      {
        this.isFixedTimeStep = value;
      }
    }

    public GameWindow Window
    {
      get
      {
        if (this.host != null)
          return this.host.Window;
        return (GameWindow) null;
      }
    }

    public bool IsActive
    {
      get
      {
        bool flag = false;
        if (GamerServicesDispatcher.IsInitialized)
          flag = Guide.IsVisible;
        if (this.isActive)
          return !flag;
        return false;
      }
    }

    public GraphicsDevice GraphicsDevice
    {
      get
      {
        IGraphicsDeviceService graphicsDeviceService = this.graphicsDeviceService;
        if (graphicsDeviceService == null)
        {
          graphicsDeviceService = this.Services.GetService(typeof (IGraphicsDeviceService)) as IGraphicsDeviceService;
          if (graphicsDeviceService == null)
            throw new InvalidOperationException(Resources.NoGraphicsDeviceService);
        }
        return graphicsDeviceService.GraphicsDevice;
      }
    }

    public ContentManager Content
    {
      get
      {
        return this.content;
      }
      set
      {
        if (value == null)
          throw new ArgumentNullException();
        this.content = value;
      }
    }

    internal bool IsActiveIgnoringGuide
    {
      get
      {
        return this.isActive;
      }
    }

    private bool ShouldExit
    {
      get
      {
        return this.exitRequested;
      }
    }

    public event EventHandler Activated;

    public event EventHandler Deactivated;

    public event EventHandler Exiting;

    public event EventHandler Disposed;

    public Game()
    {
      this.EnsureHost();
      this.gameComponents = new GameComponentCollection();
      this.gameComponents.ComponentAdded += new EventHandler<GameComponentCollectionEventArgs>(this.GameComponentAdded);
      this.gameComponents.ComponentRemoved += new EventHandler<GameComponentCollectionEventArgs>(this.GameComponentRemoved);
      this.content = new ContentManager((IServiceProvider) this.gameServices);
      this.host.Window.Paint += new EventHandler(this.Paint);
      this.clock = new GameClock();
      this.totalGameTime = TimeSpan.Zero;
      this.accumulatedElapsedGameTime = TimeSpan.Zero;
      this.lastFrameElapsedGameTime = TimeSpan.Zero;
      this.targetElapsedTime = TimeSpan.FromTicks(166667L);
      this.inactiveSleepTime = TimeSpan.FromMilliseconds(20.0);
    }

    ~Game()
    {
      this.Dispose(false);
    }

    public void Run()
    {
      try
      {
        this.graphicsDeviceManager = this.Services.GetService(typeof (IGraphicsDeviceManager)) as IGraphicsDeviceManager;
        if (this.graphicsDeviceManager != null)
          this.graphicsDeviceManager.CreateDevice();
        this.Initialize();
        this.inRun = true;
        this.BeginRun();
        this.gameTime.ElapsedGameTime = TimeSpan.Zero;
        this.gameTime.ElapsedRealTime = TimeSpan.Zero;
        this.gameTime.TotalGameTime = this.totalGameTime;
        this.gameTime.TotalRealTime = this.clock.CurrentTime;
        this.gameTime.IsRunningSlowly = false;
        this.Update(this.gameTime);
        this.doneFirstUpdate = true;
        if (this.host != null)
          this.host.Run();
        this.EndRun();
      }
      catch (NoSuitableGraphicsDeviceException ex)
      {
        if (this.ShowMissingRequirementMessage((Exception) ex))
          return;
        throw;
      }
      catch (NoAudioHardwareException ex)
      {
        if (this.ShowMissingRequirementMessage((Exception) ex))
          return;
        throw;
      }
      finally
      {
        this.inRun = false;
      }
    }

    public void Tick()
    {
      if (this.ShouldExit)
        return;
      if (!this.isActive)
        Thread.Sleep((int) this.inactiveSleepTime.TotalMilliseconds);
      this.clock.Step();
      bool flag = true;
      this.gameTime.TotalRealTime = this.clock.CurrentTime;
      this.gameTime.ElapsedRealTime = this.clock.ElapsedTime;
      this.lastFrameElapsedRealTime += this.clock.ElapsedTime;
      TimeSpan timeSpan1 = this.clock.ElapsedAdjustedTime;
      if (timeSpan1 < TimeSpan.Zero)
        timeSpan1 = TimeSpan.Zero;
      if (this.forceElapsedTimeToZero)
      {
        GameTime gameTime = this.gameTime;
        TimeSpan zero;
        timeSpan1 = zero = TimeSpan.Zero;
        TimeSpan timeSpan2 = zero;
        this.lastFrameElapsedRealTime = zero;
        TimeSpan timeSpan3 = timeSpan2;
        gameTime.ElapsedRealTime = timeSpan3;
        this.forceElapsedTimeToZero = false;
      }
      if (timeSpan1 > this.maximumElapsedTime)
        timeSpan1 = this.maximumElapsedTime;
      if (this.isFixedTimeStep)
      {
        if (Math.Abs(timeSpan1.Ticks - this.targetElapsedTime.Ticks) < this.targetElapsedTime.Ticks >> 6)
          timeSpan1 = this.targetElapsedTime;
        this.accumulatedElapsedGameTime += timeSpan1;
        long num = this.accumulatedElapsedGameTime.Ticks / this.targetElapsedTime.Ticks;
        this.accumulatedElapsedGameTime = TimeSpan.FromTicks(this.accumulatedElapsedGameTime.Ticks % this.targetElapsedTime.Ticks);
        this.lastFrameElapsedGameTime = TimeSpan.Zero;
        if (num == 0L)
          return;
        TimeSpan targetElapsedTime = this.targetElapsedTime;
        if (num > 1L)
        {
          this.updatesSinceRunningSlowly2 = this.updatesSinceRunningSlowly1;
          this.updatesSinceRunningSlowly1 = 0;
        }
        else
        {
          if (this.updatesSinceRunningSlowly1 < int.MaxValue)
            ++this.updatesSinceRunningSlowly1;
          if (this.updatesSinceRunningSlowly2 < int.MaxValue)
            ++this.updatesSinceRunningSlowly2;
        }
        this.drawRunningSlowly = this.updatesSinceRunningSlowly2 < 20;
        while (num > 0L && !this.ShouldExit)
        {
          --num;
          try
          {
            this.gameTime.ElapsedGameTime = targetElapsedTime;
            this.gameTime.TotalGameTime = this.totalGameTime;
            this.gameTime.IsRunningSlowly = this.drawRunningSlowly;
            this.Update(this.gameTime);
            flag &= this.suppressDraw;
            this.suppressDraw = false;
          }
          finally
          {
            this.lastFrameElapsedGameTime += targetElapsedTime;
            this.totalGameTime += targetElapsedTime;
          }
        }
      }
      else
      {
        TimeSpan timeSpan2 = timeSpan1;
        this.drawRunningSlowly = false;
        this.updatesSinceRunningSlowly1 = int.MaxValue;
        this.updatesSinceRunningSlowly2 = int.MaxValue;
        if (!this.ShouldExit)
        {
          try
          {
            this.gameTime.ElapsedGameTime = this.lastFrameElapsedGameTime = timeSpan2;
            this.gameTime.TotalGameTime = this.totalGameTime;
            this.gameTime.IsRunningSlowly = false;
            this.Update(this.gameTime);
            flag &= this.suppressDraw;
            this.suppressDraw = false;
          }
          finally
          {
            this.totalGameTime += timeSpan2;
          }
        }
      }
      if (flag)
        return;
      this.DrawFrame();
    }

    public void SuppressDraw()
    {
      this.suppressDraw = true;
    }

    public void Exit()
    {
      this.exitRequested = true;
      this.host.Exit();
    }

    protected virtual void BeginRun()
    {
    }

    protected virtual void EndRun()
    {
    }

    protected virtual void Update(GameTime gameTime)
    {
      for (int index = 0; index < this.updateableComponents.Count; ++index)
        this.currentlyUpdatingComponents.Add(this.updateableComponents[index]);
      for (int index = 0; index < this.currentlyUpdatingComponents.Count; ++index)
      {
        IUpdateable updatingComponent = this.currentlyUpdatingComponents[index];
        if (updatingComponent.Enabled)
          updatingComponent.Update(gameTime);
      }
      this.currentlyUpdatingComponents.Clear();
      FrameworkDispatcher.Update();
      this.doneFirstUpdate = true;
    }

    protected virtual bool BeginDraw()
    {
      return this.graphicsDeviceManager == null || this.graphicsDeviceManager.BeginDraw();
    }

    protected virtual void Draw(GameTime gameTime)
    {
      for (int index = 0; index < this.drawableComponents.Count; ++index)
        this.currentlyDrawingComponents.Add(this.drawableComponents[index]);
      for (int index = 0; index < this.currentlyDrawingComponents.Count; ++index)
      {
        IDrawable drawingComponent = this.currentlyDrawingComponents[index];
        if (drawingComponent.Visible)
          drawingComponent.Draw(gameTime);
      }
      this.currentlyDrawingComponents.Clear();
    }

    protected virtual void EndDraw()
    {
      if (this.graphicsDeviceManager == null)
        return;
      this.graphicsDeviceManager.EndDraw();
    }

    private void Paint(object sender, EventArgs e)
    {
      if (!this.doneFirstDraw)
        return;
      this.DrawFrame();
    }

    protected virtual void Initialize()
    {
      this.HookDeviceEvents();
      while (this.notYetInitialized.Count != 0)
      {
        this.notYetInitialized[0].Initialize();
        this.notYetInitialized.RemoveAt(0);
      }
      if (this.graphicsDeviceService == null || this.graphicsDeviceService.GraphicsDevice == null)
        return;
      this.LoadGraphicsContent(true);
      this.LoadContent();
    }

    public void ResetElapsedTime()
    {
      this.forceElapsedTimeToZero = true;
      this.drawRunningSlowly = false;
      this.updatesSinceRunningSlowly1 = int.MaxValue;
      this.updatesSinceRunningSlowly2 = int.MaxValue;
    }

    private void DrawFrame()
    {
      try
      {
        if (this.ShouldExit || !this.doneFirstUpdate || (this.Window.IsMinimized || !this.BeginDraw()))
          return;
        this.gameTime.TotalRealTime = this.clock.CurrentTime;
        this.gameTime.ElapsedRealTime = this.lastFrameElapsedRealTime;
        this.gameTime.TotalGameTime = this.totalGameTime;
        this.gameTime.ElapsedGameTime = this.lastFrameElapsedGameTime;
        this.gameTime.IsRunningSlowly = this.drawRunningSlowly;
        this.Draw(this.gameTime);
        this.EndDraw();
        this.doneFirstDraw = true;
      }
      finally
      {
        this.lastFrameElapsedRealTime = TimeSpan.Zero;
        this.lastFrameElapsedGameTime = TimeSpan.Zero;
      }
    }

    private void GameComponentRemoved(object sender, GameComponentCollectionEventArgs e)
    {
      if (!this.inRun)
        this.notYetInitialized.Remove(e.GameComponent);
      IUpdateable gameComponent1 = e.GameComponent as IUpdateable;
      if (gameComponent1 != null)
      {
        this.updateableComponents.Remove(gameComponent1);
        gameComponent1.UpdateOrderChanged -= new EventHandler(this.UpdateableUpdateOrderChanged);
      }
      IDrawable gameComponent2 = e.GameComponent as IDrawable;
      if (gameComponent2 == null)
        return;
      this.drawableComponents.Remove(gameComponent2);
      gameComponent2.DrawOrderChanged -= new EventHandler(this.DrawableDrawOrderChanged);
    }

    private void GameComponentAdded(object sender, GameComponentCollectionEventArgs e)
    {
      if (this.inRun)
        e.GameComponent.Initialize();
      else
        this.notYetInitialized.Add(e.GameComponent);
      IUpdateable gameComponent1 = e.GameComponent as IUpdateable;
      if (gameComponent1 != null)
      {
        int num = this.updateableComponents.BinarySearch(gameComponent1, (IComparer<IUpdateable>) UpdateOrderComparer.Default);
        if (num < 0)
        {
          int index = ~num;
          while (index < this.updateableComponents.Count && this.updateableComponents[index].UpdateOrder == gameComponent1.UpdateOrder)
            ++index;
          this.updateableComponents.Insert(index, gameComponent1);
          gameComponent1.UpdateOrderChanged += new EventHandler(this.UpdateableUpdateOrderChanged);
        }
      }
      IDrawable gameComponent2 = e.GameComponent as IDrawable;
      if (gameComponent2 == null)
        return;
      int num1 = this.drawableComponents.BinarySearch(gameComponent2, (IComparer<IDrawable>) DrawOrderComparer.Default);
      if (num1 >= 0)
        return;
      int index1 = ~num1;
      while (index1 < this.drawableComponents.Count && this.drawableComponents[index1].DrawOrder == gameComponent2.DrawOrder)
        ++index1;
      this.drawableComponents.Insert(index1, gameComponent2);
      gameComponent2.DrawOrderChanged += new EventHandler(this.DrawableDrawOrderChanged);
    }

    private void DrawableDrawOrderChanged(object sender, EventArgs e)
    {
      IDrawable drawable = sender as IDrawable;
      this.drawableComponents.Remove(drawable);
      int num = this.drawableComponents.BinarySearch(drawable, (IComparer<IDrawable>) DrawOrderComparer.Default);
      if (num >= 0)
        return;
      int index = ~num;
      while (index < this.drawableComponents.Count && this.drawableComponents[index].DrawOrder == drawable.DrawOrder)
        ++index;
      this.drawableComponents.Insert(index, drawable);
    }

    private void UpdateableUpdateOrderChanged(object sender, EventArgs e)
    {
      IUpdateable updateable = sender as IUpdateable;
      this.updateableComponents.Remove(updateable);
      int num = this.updateableComponents.BinarySearch(updateable, (IComparer<IUpdateable>) UpdateOrderComparer.Default);
      if (num >= 0)
        return;
      int index = ~num;
      while (index < this.updateableComponents.Count && this.updateableComponents[index].UpdateOrder == updateable.UpdateOrder)
        ++index;
      this.updateableComponents.Insert(index, updateable);
    }

    protected virtual void OnActivated(object sender, EventArgs args)
    {
      if (this.Activated == null)
        return;
      this.Activated((object) this, args);
    }

    protected virtual void OnDeactivated(object sender, EventArgs args)
    {
      if (this.Deactivated == null)
        return;
      this.Deactivated((object) this, args);
    }

    protected virtual void OnExiting(object sender, EventArgs args)
    {
      if (this.Exiting == null)
        return;
      this.Exiting((object) null, args);
    }

    private void EnsureHost()
    {
      if (this.host != null)
        return;
      this.host = (GameHost) new WindowsGameHost(this);
      this.host.Activated += new EventHandler(this.HostActivated);
      this.host.Deactivated += new EventHandler(this.HostDeactivated);
      this.host.Suspend += new EventHandler(this.HostSuspend);
      this.host.Resume += new EventHandler(this.HostResume);
      this.host.Idle += new EventHandler(this.HostIdle);
      this.host.Exiting += new EventHandler(this.HostExiting);
    }

    private void HostSuspend(object sender, EventArgs e)
    {
      this.clock.Suspend();
    }

    private void HostResume(object sender, EventArgs e)
    {
      this.clock.Resume();
    }

    private void HostExiting(object sender, EventArgs e)
    {
      this.OnExiting((object) this, EventArgs.Empty);
    }

    private void HostIdle(object sender, EventArgs e)
    {
      this.Tick();
    }

    private void HostDeactivated(object sender, EventArgs e)
    {
      if (!this.isActive)
        return;
      this.isActive = false;
      this.OnDeactivated((object) this, EventArgs.Empty);
    }

    private void HostActivated(object sender, EventArgs e)
    {
      if (this.isActive)
        return;
      this.isActive = true;
      this.OnActivated((object) this, EventArgs.Empty);
    }

    private void HookDeviceEvents()
    {
      this.graphicsDeviceService = this.Services.GetService(typeof (IGraphicsDeviceService)) as IGraphicsDeviceService;
      if (this.graphicsDeviceService == null)
        return;
      this.graphicsDeviceService.DeviceCreated += new EventHandler(this.DeviceCreated);
      this.graphicsDeviceService.DeviceResetting += new EventHandler(this.DeviceResetting);
      this.graphicsDeviceService.DeviceReset += new EventHandler(this.DeviceReset);
      this.graphicsDeviceService.DeviceDisposing += new EventHandler(this.DeviceDisposing);
    }

    private void UnhookDeviceEvents()
    {
      if (this.graphicsDeviceService == null)
        return;
      this.graphicsDeviceService.DeviceCreated -= new EventHandler(this.DeviceCreated);
      this.graphicsDeviceService.DeviceResetting -= new EventHandler(this.DeviceResetting);
      this.graphicsDeviceService.DeviceReset -= new EventHandler(this.DeviceReset);
      this.graphicsDeviceService.DeviceDisposing -= new EventHandler(this.DeviceDisposing);
    }

    private void DeviceResetting(object sender, EventArgs e)
    {
      this.UnloadGraphicsContent(false);
    }

    private void DeviceReset(object sender, EventArgs e)
    {
      this.LoadGraphicsContent(false);
    }

    private void DeviceCreated(object sender, EventArgs e)
    {
      this.LoadGraphicsContent(true);
      this.LoadContent();
    }

    private void DeviceDisposing(object sender, EventArgs e)
    {
      this.content.Unload();
      this.UnloadGraphicsContent(true);
      this.UnloadContent();
    }

    [Obsolete("The LoadGraphicsContent method is obsolete and will be removed in the future.  Use the LoadContent method instead.")]
    [EditorBrowsable(EditorBrowsableState.Never)]
    protected virtual void LoadGraphicsContent(bool loadAllContent)
    {
    }

    [Obsolete("The UnloadGraphicsContent method is obsolete and will be removed in the future.  Use the UnloadContent method instead.")]
    [EditorBrowsable(EditorBrowsableState.Never)]
    protected virtual void UnloadGraphicsContent(bool unloadAllContent)
    {
    }

    protected virtual void LoadContent()
    {
    }

    protected virtual void UnloadContent()
    {
    }

    public void Dispose()
    {
      this.Dispose(true);
      GC.SuppressFinalize((object) this);
    }

    protected virtual void Dispose(bool disposing)
    {
      if (!disposing)
        return;
      lock (this)
      {
        IGameComponent[] local_0 = new IGameComponent[this.gameComponents.Count];
        this.gameComponents.CopyTo(local_0, 0);
        for (int local_1 = 0; local_1 < local_0.Length; ++local_1)
        {
          IDisposable local_2 = local_0[local_1] as IDisposable;
          if (local_2 != null)
            local_2.Dispose();
        }
        IDisposable local_3 = this.graphicsDeviceManager as IDisposable;
        if (local_3 != null)
          local_3.Dispose();
        this.UnhookDeviceEvents();
        if (this.Disposed == null)
          return;
        this.Disposed((object) this, EventArgs.Empty);
      }
    }

    protected virtual bool ShowMissingRequirementMessage(Exception exception)
    {
      if (this.host != null)
        return this.host.ShowMissingRequirementMessage(exception);
      return false;
    }
  }
}
