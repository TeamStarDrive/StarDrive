// Decompiled with JetBrains decompiler
// Type: Microsoft.Xna.Framework.GameComponent
// Assembly: Microsoft.Xna.Framework.Game, Version=3.1.0.0, Culture=neutral, PublicKeyToken=6d5c3888ef60e27d
// MVID: E4BD910E-73ED-465E-A91E-14AAAB0CE109
// Assembly location: C:\WINDOWS\assembly\GAC_32\Microsoft.Xna.Framework.Game\3.1.0.0__6d5c3888ef60e27d\Microsoft.Xna.Framework.Game.dll

using System;

namespace Microsoft.Xna.Framework
{
  public class GameComponent : IGameComponent, IUpdateable, IDisposable
  {
    private bool enabled = true;
    private int updateOrder;
    private Game game;

    public bool Enabled
    {
      get
      {
        return this.enabled;
      }
      set
      {
        if (this.enabled == value)
          return;
        this.enabled = value;
        this.OnEnabledChanged((object) this, EventArgs.Empty);
      }
    }

    public int UpdateOrder
    {
      get
      {
        return this.updateOrder;
      }
      set
      {
        if (this.updateOrder == value)
          return;
        this.updateOrder = value;
        this.OnUpdateOrderChanged((object) this, EventArgs.Empty);
      }
    }

    public Game Game
    {
      get
      {
        return this.game;
      }
    }

    public event EventHandler EnabledChanged;

    public event EventHandler UpdateOrderChanged;

    public event EventHandler Disposed;

    public GameComponent(Game game)
    {
      this.game = game;
    }

    ~GameComponent()
    {
      this.Dispose(false);
    }

    public virtual void Initialize()
    {
    }

    public virtual void Update(GameTime gameTime)
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
        if (this.Game != null)
          this.Game.Components.Remove((IGameComponent) this);
        if (this.Disposed == null)
          return;
        this.Disposed((object) this, EventArgs.Empty);
      }
    }

    protected virtual void OnUpdateOrderChanged(object sender, EventArgs args)
    {
        UpdateOrderChanged?.Invoke((object) this, args);
    }

    protected virtual void OnEnabledChanged(object sender, EventArgs args)
    {
        EnabledChanged?.Invoke((object) this, args);
    }
  }
}
