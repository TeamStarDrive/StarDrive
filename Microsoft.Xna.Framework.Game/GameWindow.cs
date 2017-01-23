// Decompiled with JetBrains decompiler
// Type: Microsoft.Xna.Framework.GameWindow
// Assembly: Microsoft.Xna.Framework.Game, Version=3.1.0.0, Culture=neutral, PublicKeyToken=6d5c3888ef60e27d
// MVID: E4BD910E-73ED-465E-A91E-14AAAB0CE109
// Assembly location: C:\WINDOWS\assembly\GAC_32\Microsoft.Xna.Framework.Game\3.1.0.0__6d5c3888ef60e27d\Microsoft.Xna.Framework.Game.dll

using System;

namespace Microsoft.Xna.Framework
{
  public abstract class GameWindow
  {
    internal static readonly int DefaultClientWidth = 800;
    internal static readonly int DefaultClientHeight = 600;
    private string title;

    public string Title
    {
      get
      {
        return this.title;
      }
      set
      {
        if (value == null)
          throw new ArgumentNullException("value", Resources.TitleCannotBeNull);
        if (!(this.title != value))
          return;
        this.title = value;
        this.SetTitle(this.title);
      }
    }

    public abstract IntPtr Handle { get; }

    public abstract bool AllowUserResizing { get; set; }

    internal abstract bool IsMouseVisible { get; set; }

    internal abstract bool IsMinimized { get; }

    public abstract Rectangle ClientBounds { get; }

    public abstract string ScreenDeviceName { get; }

    internal event EventHandler Activated;

    internal event EventHandler Deactivated;

    internal event EventHandler Paint;

    public event EventHandler ScreenDeviceNameChanged;

    public event EventHandler ClientSizeChanged;

    internal GameWindow()
    {
      this.title = string.Empty;
    }

    public abstract void BeginScreenDeviceChange(bool willBeFullScreen);

    public abstract void EndScreenDeviceChange(string screenDeviceName, int clientWidth, int clientHeight);

    public void EndScreenDeviceChange(string screenDeviceName)
    {
      this.EndScreenDeviceChange(screenDeviceName, this.ClientBounds.Width, this.ClientBounds.Height);
    }

    protected abstract void SetTitle(string title);

    protected void OnActivated()
    {
      if (this.Activated == null)
        return;
      this.Activated((object) this, EventArgs.Empty);
    }

    protected void OnDeactivated()
    {
      if (this.Deactivated == null)
        return;
      this.Deactivated((object) this, EventArgs.Empty);
    }

    protected void OnPaint()
    {
      if (this.Paint == null)
        return;
      this.Paint((object) this, EventArgs.Empty);
    }

    protected void OnScreenDeviceNameChanged()
    {
      if (this.ScreenDeviceNameChanged == null)
        return;
      this.ScreenDeviceNameChanged((object) this, EventArgs.Empty);
    }

    protected void OnClientSizeChanged()
    {
      if (this.ClientSizeChanged == null)
        return;
      this.ClientSizeChanged((object) this, EventArgs.Empty);
    }
  }
}
