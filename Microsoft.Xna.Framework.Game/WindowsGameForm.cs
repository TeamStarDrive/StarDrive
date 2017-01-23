// Decompiled with JetBrains decompiler
// Type: Microsoft.Xna.Framework.WindowsGameForm
// Assembly: Microsoft.Xna.Framework.Game, Version=3.1.0.0, Culture=neutral, PublicKeyToken=6d5c3888ef60e27d
// MVID: E4BD910E-73ED-465E-A91E-14AAAB0CE109
// Assembly location: C:\WINDOWS\assembly\GAC_32\Microsoft.Xna.Framework.Game\3.1.0.0__6d5c3888ef60e27d\Microsoft.Xna.Framework.Game.dll

using Microsoft.Xna.Framework.GamerServices;
using System;
using System.Drawing;
using System.Windows.Forms;

namespace Microsoft.Xna.Framework
{
  internal class WindowsGameForm : Form
  {
    private Size startResizeSize = Size.Empty;
    private bool centerScreen = true;
    private bool firstPaint = true;
    private bool freezeOurEvents;
    private Screen screen;
    private FormWindowState resizeWindowState;
    private bool hidMouse;
    private bool isMouseVisible;
    private bool allowUserResizing;
    private bool userResizing;
    private bool? deviceChangeWillBeFullScreen;
    private bool deviceChangeChangedVisible;
    private bool oldVisible;
    private Size oldClientSize;
    private bool isFullScreenMaximized;
    private FormBorderStyle savedFormBorderStyle;
    private FormWindowState savedWindowState;
    private System.Drawing.Rectangle savedBounds;
    private System.Drawing.Rectangle savedRestoreBounds;

    internal bool AllowUserResizing
    {
      get
      {
        return this.allowUserResizing;
      }
      set
      {
        if (this.allowUserResizing == value)
          return;
        this.allowUserResizing = value;
        this.UpdateBorderStyle();
      }
    }

    internal bool IsMouseVisible
    {
      get
      {
        return this.isMouseVisible;
      }
      set
      {
        if (this.isMouseVisible == value)
          return;
        this.isMouseVisible = value;
        if (this.isMouseVisible)
        {
          if (!this.hidMouse)
            return;
          Cursor.Show();
          this.hidMouse = false;
        }
        else
        {
          if (this.hidMouse)
            return;
          Cursor.Hide();
          this.hidMouse = true;
        }
      }
    }

    internal Screen DeviceScreen
    {
      get
      {
        return this.screen;
      }
    }

    internal Rectangle ClientBounds
    {
      get
      {
        System.Drawing.Point screen = this.PointToScreen(System.Drawing.Point.Empty);
        return new Rectangle(screen.X, screen.Y, this.ClientSize.Width, this.ClientSize.Height);
      }
    }

    internal bool IsMinimized
    {
      get
      {
        if (this.ClientSize.Width != 0)
          return this.ClientSize.Height == 0;
        return true;
      }
    }

    internal event EventHandler Suspend;

    internal event EventHandler Resume;

    internal event EventHandler ScreenChanged;

    internal event EventHandler UserResized;

    internal event EventHandler ApplicationActivated;

    internal event EventHandler ApplicationDeactivated;

    public WindowsGameForm()
    {
      this.SuspendLayout();
      this.AutoScaleDimensions = new SizeF(6f, 13f);
      this.AutoScaleMode = AutoScaleMode.Font;
      this.CausesValidation = false;
      this.ClientSize = new Size(292, 266);
      this.Name = "GameForm";
      this.Text = "GameForm";
      this.ResizeBegin += new EventHandler(this.Form_ResizeBegin);
      this.ClientSizeChanged += new EventHandler(this.Form_ClientSizeChanged);
      this.Resize += new EventHandler(this.Form_Resize);
      this.LocationChanged += new EventHandler(this.Form_LocationChanged);
      this.ResizeEnd += new EventHandler(this.Form_ResizeEnd);
      this.MouseEnter += new EventHandler(this.Form_MouseEnter);
      this.MouseLeave += new EventHandler(this.Form_MouseLeave);
      this.ResumeLayout(false);
      try
      {
        this.freezeOurEvents = true;
        this.resizeWindowState = this.WindowState;
        this.screen = WindowsGameWindow.ScreenFromHandle(this.Handle);
        this.SetStyle(ControlStyles.Opaque | ControlStyles.AllPaintingInWmPaint, false);
        this.ClientSize = new Size(GameWindow.DefaultClientWidth, GameWindow.DefaultClientHeight);
        this.UpdateBorderStyle();
      }
      finally
      {
        this.freezeOurEvents = false;
      }
    }

    private void UpdateBorderStyle()
    {
      if (!this.allowUserResizing)
      {
        this.MaximizeBox = false;
        if (this.isFullScreenMaximized)
          return;
        this.FormBorderStyle = FormBorderStyle.FixedSingle;
      }
      else
      {
        this.MaximizeBox = true;
        if (this.isFullScreenMaximized)
          return;
        this.FormBorderStyle = FormBorderStyle.Sizable;
      }
    }

    private void UpdateScreen()
    {
      if (this.freezeOurEvents)
        return;
      Screen screen = Screen.FromHandle(this.Handle);
      if (this.screen != null && this.screen.Equals((object) screen))
        return;
      this.screen = screen;
      if (this.screen == null)
        return;
      this.OnScreenChanged();
    }

    private void OnSuspend()
    {
      if (this.Suspend == null)
        return;
      this.Suspend((object) this, EventArgs.Empty);
    }

    private void OnResume()
    {
      if (this.Resume == null)
        return;
      this.Resume((object) this, EventArgs.Empty);
    }

    private void OnScreenChanged()
    {
      if (this.ScreenChanged == null)
        return;
      this.ScreenChanged((object) this, EventArgs.Empty);
    }

    private void OnUserResized(bool forceEvent)
    {
      if (this.freezeOurEvents && !forceEvent || this.UserResized == null)
        return;
      this.UserResized((object) this, EventArgs.Empty);
    }

    private void OnActivateApp(bool active)
    {
      if (active)
      {
        this.firstPaint = true;
        this.freezeOurEvents = false;
        if (this.isFullScreenMaximized)
          this.TopMost = true;
        if (this.ApplicationActivated == null)
          return;
        this.ApplicationActivated((object) this, EventArgs.Empty);
      }
      else
      {
        if (this.ApplicationDeactivated != null)
          this.ApplicationDeactivated((object) this, EventArgs.Empty);
        this.freezeOurEvents = true;
      }
    }

    protected override void OnPaintBackground(PaintEventArgs e)
    {
      if (!this.firstPaint)
        return;
      base.OnPaintBackground(e);
      this.firstPaint = false;
    }

    protected override void WndProc(ref System.Windows.Forms.Message m)
    {
      if (m.Msg == 28)
        this.OnActivateApp(m.WParam != IntPtr.Zero);
      base.WndProc(ref m);
    }

    protected override bool ProcessDialogKey(Keys keyData)
    {
      Keys keys = keyData & Keys.KeyCode;
      if ((keyData & Keys.Alt) == Keys.Alt && (keys == Keys.F4 || keys == Keys.None))
        return base.ProcessDialogKey(keyData);
      if (GamerServicesDispatcher.IsInitialized && (keys == Keys.Home || Guide.IsVisible))
        return base.ProcessDialogKey(keyData);
      return true;
    }

    protected override void Dispose(bool disposing)
    {
      base.Dispose(disposing);
    }

    private void Form_ResizeBegin(object sender, EventArgs e)
    {
      this.startResizeSize = this.ClientSize;
      this.userResizing = true;
      this.OnSuspend();
    }

    private void Form_Resize(object sender, EventArgs e)
    {
      if (this.resizeWindowState != this.WindowState)
      {
        this.resizeWindowState = this.WindowState;
        this.firstPaint = true;
        this.OnUserResized(false);
        this.Invalidate();
      }
      if (!this.userResizing || !(this.ClientSize != this.startResizeSize))
        return;
      this.Invalidate();
    }

    private void Form_ResizeEnd(object sender, EventArgs e)
    {
      this.userResizing = false;
      if (this.ClientSize != this.startResizeSize)
      {
        this.centerScreen = false;
        this.OnUserResized(false);
      }
      this.firstPaint = true;
      this.OnResume();
    }

    private void Form_ClientSizeChanged(object sender, EventArgs e)
    {
      this.UpdateScreen();
    }

    private void Form_LocationChanged(object sender, EventArgs e)
    {
      if (this.userResizing)
        this.centerScreen = false;
      this.UpdateScreen();
    }

    private void Form_MouseEnter(object sender, EventArgs e)
    {
      if (this.isMouseVisible || this.hidMouse)
        return;
      Cursor.Hide();
      this.hidMouse = true;
    }

    private void Form_MouseLeave(object sender, EventArgs e)
    {
      if (!this.hidMouse)
        return;
      Cursor.Show();
      this.hidMouse = false;
    }

    internal void BeginScreenDeviceChange(bool willBeFullScreen)
    {
      this.oldClientSize = this.ClientSize;
      if (willBeFullScreen && !this.isFullScreenMaximized)
      {
        this.savedFormBorderStyle = this.FormBorderStyle;
        this.savedWindowState = this.WindowState;
        this.savedBounds = this.Bounds;
        if (this.WindowState == FormWindowState.Maximized)
          this.savedRestoreBounds = this.RestoreBounds;
      }
      if (willBeFullScreen != this.isFullScreenMaximized)
      {
        this.deviceChangeChangedVisible = true;
        this.oldVisible = this.Visible;
        this.Visible = false;
      }
      else
        this.deviceChangeChangedVisible = false;
      if (!willBeFullScreen && this.isFullScreenMaximized)
      {
        this.TopMost = false;
        this.FormBorderStyle = this.savedFormBorderStyle;
        if (this.savedWindowState == FormWindowState.Maximized)
          this.SetBoundsCore(this.screen.Bounds.X, this.screen.Bounds.Y, this.savedRestoreBounds.Width, this.savedRestoreBounds.Height, BoundsSpecified.Size);
        else
          this.SetBoundsCore(this.screen.Bounds.X, this.screen.Bounds.Y, this.savedBounds.Width, this.savedBounds.Height, BoundsSpecified.Size);
      }
      if (willBeFullScreen != this.isFullScreenMaximized)
        this.SendToBack();
      this.deviceChangeWillBeFullScreen = new bool?(willBeFullScreen);
    }

    internal void EndScreenDeviceChange(string screenDeviceName, int clientWidth, int clientHeight)
    {
      if (!this.deviceChangeWillBeFullScreen.HasValue)
        throw new InvalidOperationException(Resources.MustCallBeginDeviceChange);
      bool flag = false;
      if (this.deviceChangeWillBeFullScreen.Value)
      {
        Screen screen = WindowsGameWindow.ScreenFromDeviceName(screenDeviceName);
        System.Drawing.Rectangle bounds = Screen.GetBounds(new System.Drawing.Point(screen.Bounds.X, screen.Bounds.Y));
        if (!this.isFullScreenMaximized)
        {
          flag = true;
          this.TopMost = true;
          this.FormBorderStyle = FormBorderStyle.None;
          this.WindowState = FormWindowState.Normal;
          this.BringToFront();
        }
        this.Location = new System.Drawing.Point(bounds.X, bounds.Y);
        this.ClientSize = new Size(bounds.Width, bounds.Height);
        this.isFullScreenMaximized = true;
      }
      else
      {
        if (this.isFullScreenMaximized)
        {
          flag = true;
          this.BringToFront();
        }
        this.ResizeWindow(screenDeviceName, clientWidth, clientHeight, this.centerScreen);
      }
      if (this.deviceChangeChangedVisible)
        this.Visible = this.oldVisible;
      if (flag && this.oldClientSize != this.ClientSize)
        this.OnUserResized(true);
      this.deviceChangeWillBeFullScreen = new bool?();
    }

    private void ResizeWindow(string screenDeviceName, int clientWidth, int clientHeight, bool center)
    {
      Screen screen = WindowsGameWindow.ScreenFromDeviceName(screenDeviceName);
      System.Drawing.Rectangle bounds = Screen.GetBounds(new System.Drawing.Point(screen.Bounds.X, screen.Bounds.Y));
      int x1;
      int y1;
      if (screenDeviceName != WindowsGameWindow.DeviceNameFromScreen(this.DeviceScreen))
      {
        x1 = bounds.X;
        y1 = bounds.Y;
      }
      else
      {
        x1 = this.screen.Bounds.X;
        y1 = this.screen.Bounds.Y;
      }
      if (this.isFullScreenMaximized)
      {
        Size size = this.SizeFromClientSize(new Size(clientWidth, clientHeight));
        if (this.savedWindowState == FormWindowState.Maximized)
          this.SetBoundsCore(this.savedRestoreBounds.X - this.screen.Bounds.X + x1, this.savedRestoreBounds.Y - this.screen.Bounds.Y + y1, this.savedRestoreBounds.Width, this.savedRestoreBounds.Height, BoundsSpecified.All);
        else if (center)
          this.SetBoundsCore(x1 + bounds.Width / 2 - size.Width / 2, y1 + bounds.Height / 2 - size.Height / 2, size.Width, size.Height, BoundsSpecified.All);
        else
          this.SetBoundsCore(this.savedBounds.X - this.screen.Bounds.X + x1, this.savedBounds.Y - this.screen.Bounds.Y + y1, size.Width, size.Height, BoundsSpecified.All);
        this.WindowState = this.savedWindowState;
        this.isFullScreenMaximized = false;
      }
      else
      {
        if (this.WindowState != FormWindowState.Normal)
          return;
        int x2;
        int y2;
        if (center)
        {
          Size size = this.SizeFromClientSize(new Size(clientWidth, clientHeight));
          x2 = x1 + bounds.Width / 2 - size.Width / 2;
          y2 = y1 + bounds.Height / 2 - size.Height / 2;
        }
        else
        {
          x2 = x1 + this.Bounds.X - this.screen.Bounds.X;
          y2 = y1 + this.Bounds.Y - this.screen.Bounds.Y;
        }
        if (x2 != this.Location.X || y2 != this.Location.Y)
          this.Location = new System.Drawing.Point(x2, y2);
        if (this.ClientSize.Width == clientWidth && this.ClientSize.Height == clientHeight)
          return;
        this.ClientSize = new Size(clientWidth, clientHeight);
      }
    }
  }
}
