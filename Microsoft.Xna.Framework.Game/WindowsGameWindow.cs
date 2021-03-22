// Decompiled with JetBrains decompiler
// Type: Microsoft.Xna.Framework.WindowsGameWindow
// Assembly: Microsoft.Xna.Framework.Game, Version=3.1.0.0, Culture=neutral, PublicKeyToken=6d5c3888ef60e27d
// MVID: E4BD910E-73ED-465E-A91E-14AAAB0CE109
// Assembly location: C:\WINDOWS\assembly\GAC_32\Microsoft.Xna.Framework.Game\3.1.0.0__6d5c3888ef60e27d\Microsoft.Xna.Framework.Game.dll

using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections;
using System.ComponentModel;
using System.Drawing;
using System.IO;
using System.Reflection;
using System.Resources;
using System.Windows.Forms;

namespace Microsoft.Xna.Framework
{
  internal class WindowsGameWindow : GameWindow
  {
    private WindowsGameForm mainForm;
    private bool isMouseVisible;
    private bool isGuideVisible;
    private bool inDeviceTransition;
    private Exception pendingException;

    public override IntPtr Handle
    {
      get
      {
        if (this.mainForm != null)
          return this.mainForm.Handle;
        return IntPtr.Zero;
      }
    }

    public override bool AllowUserResizing
    {
      get
      {
        if (this.mainForm != null)
          return this.mainForm.AllowUserResizing;
        return false;
      }
      set
      {
        if (this.mainForm == null)
          return;
        this.mainForm.AllowUserResizing = value;
      }
    }

    internal override bool IsMouseVisible
    {
      get
      {
        return this.isMouseVisible;
      }
      set
      {
        this.isMouseVisible = value;
        if (this.mainForm == null)
          return;
        this.mainForm.IsMouseVisible = this.isMouseVisible || this.isGuideVisible;
      }
    }

    internal bool IsGuideVisible
    {
      set
      {
        if (value == this.isGuideVisible)
          return;
        this.isGuideVisible = value;
        if (this.mainForm == null)
          return;
        this.mainForm.IsMouseVisible = this.isMouseVisible || this.isGuideVisible;
      }
    }

    public override Rectangle ClientBounds
    {
      get
      {
        return this.mainForm.ClientBounds;
      }
    }

    public override string ScreenDeviceName
    {
      get
      {
        if (this.mainForm == null || this.mainForm.DeviceScreen == null)
          return string.Empty;
        return WindowsGameWindow.DeviceNameFromScreen(this.mainForm.DeviceScreen);
      }
    }

    internal override bool IsMinimized
    {
      get
      {
        if (this.mainForm == null)
          return false;
        return this.mainForm.IsMinimized;
      }
    }

    internal Form Form
    {
      get
      {
        return (Form) this.mainForm;
      }
    }

    internal event EventHandler Suspend;

    internal event EventHandler Resume;

    [System.Runtime.InteropServices.DllImport("user32.dll")]
    internal static extern bool SetProcessDPIAware();

    public WindowsGameWindow()
    {
      SetProcessDPIAware();
      this.mainForm = new WindowsGameForm();
      Icon defaultIcon = WindowsGameWindow.GetDefaultIcon();
      if (defaultIcon != null)
        this.mainForm.Icon = defaultIcon;
      this.Title = WindowsGameWindow.GetDefaultTitleName();
      this.mainForm.Suspend += new EventHandler(this.mainForm_Suspend);
      this.mainForm.Resume += new EventHandler(this.mainForm_Resume);
      this.mainForm.ScreenChanged += new EventHandler(this.mainForm_ScreenChanged);
      this.mainForm.ApplicationActivated += new EventHandler(this.mainForm_ApplicationActivated);
      this.mainForm.ApplicationDeactivated += new EventHandler(this.mainForm_ApplicationDeactivated);
      this.mainForm.UserResized += new EventHandler(this.mainForm_UserResized);
      this.mainForm.Closing += new CancelEventHandler(this.mainForm_Closing);
      this.mainForm.Paint += new PaintEventHandler(this.mainForm_Paint);
    }

    internal void Close()
    {
      if (this.mainForm == null)
        return;
      this.mainForm.Close();
      this.mainForm = (WindowsGameForm) null;
    }

    public override void BeginScreenDeviceChange(bool willBeFullScreen)
    {
      this.mainForm.BeginScreenDeviceChange(willBeFullScreen);
      this.inDeviceTransition = true;
    }

    public override void EndScreenDeviceChange(string screenDeviceName, int clientWidth, int clientHeight)
    {
      try
      {
        this.mainForm.EndScreenDeviceChange(screenDeviceName, clientWidth, clientHeight);
      }
      finally
      {
        this.inDeviceTransition = false;
      }
    }

    protected override void SetTitle(string title)
    {
      if (this.mainForm == null)
        return;
      this.mainForm.Text = title;
    }

    protected void OnSuspend()
    {
      if (this.Suspend == null)
        return;
      this.Suspend((object) this, EventArgs.Empty);
    }

    protected void OnResume()
    {
      if (this.Resume == null)
        return;
      this.Resume((object) this, EventArgs.Empty);
    }

    private void mainForm_ApplicationActivated(object sender, EventArgs e)
    {
      this.OnActivated();
    }

    private void mainForm_ApplicationDeactivated(object sender, EventArgs e)
    {
      this.OnDeactivated();
    }

    private void mainForm_ScreenChanged(object sender, EventArgs e)
    {
      this.OnScreenDeviceNameChanged();
    }

    private void mainForm_UserResized(object sender, EventArgs e)
    {
      this.OnClientSizeChanged();
    }

    private void mainForm_Paint(object sender, PaintEventArgs e)
    {
      if (this.inDeviceTransition)
        return;
      try
      {
        this.OnPaint();
      }
      catch (Exception ex)
      {
        this.pendingException = (Exception) new InvalidOperationException(Microsoft.Xna.Framework.Resources.PreviousDrawThrew, ex);
      }
    }

    private void mainForm_Closing(object sender, CancelEventArgs e)
    {
      this.OnDeactivated();
    }

    private void mainForm_Resume(object sender, EventArgs e)
    {
      this.OnResume();
    }

    private void mainForm_Suspend(object sender, EventArgs e)
    {
      this.OnSuspend();
    }

    internal void Tick()
    {
      if (this.pendingException != null)
      {
        Exception pendingException = this.pendingException;
        this.pendingException = (Exception) null;
        throw pendingException;
      }
    }

    internal static Screen ScreenFromHandle(IntPtr windowHandle)
    {
      int num1 = 0;
      Screen screen = (Screen) null;
      NativeMethods.RECT rect;
      NativeMethods.GetWindowRect(windowHandle, out rect);
      System.Drawing.Rectangle rectangle1 = new System.Drawing.Rectangle(rect.Left, rect.Top, rect.Right - rect.Left, rect.Bottom - rect.Top);
      foreach (Screen allScreen in Screen.AllScreens)
      {
        System.Drawing.Rectangle rectangle2 = rectangle1;
        rectangle2.Intersect(allScreen.Bounds);
        int num2 = rectangle2.Width * rectangle2.Height;
        if (num2 > num1)
        {
          num1 = num2;
          screen = allScreen;
        }
      }
      if (screen == null)
        screen = Screen.AllScreens[0];
      return screen;
    }

    internal static string DeviceNameFromScreen(Screen screen)
    {
      string str = screen.DeviceName;
      int length = screen.DeviceName.IndexOf(char.MinValue);
      if (length != -1)
        str = screen.DeviceName.Substring(0, length);
      return str;
    }

    internal static Screen ScreenFromDeviceName(string screenDeviceName)
    {
      if (string.IsNullOrEmpty(screenDeviceName))
        throw new ArgumentException(Microsoft.Xna.Framework.Resources.NullOrEmptyScreenDeviceName);
      foreach (Screen allScreen in Screen.AllScreens)
      {
        if (WindowsGameWindow.DeviceNameFromScreen(allScreen) == screenDeviceName)
          return allScreen;
      }
      throw new ArgumentException(Microsoft.Xna.Framework.Resources.InvalidScreenDeviceName, "screenDeviceName");
    }

    internal static Screen ScreenFromAdapter(GraphicsAdapter adapter)
    {
      foreach (Screen allScreen in Screen.AllScreens)
      {
        if (WindowsGameWindow.DeviceNameFromScreen(allScreen) == adapter.DeviceName)
          return allScreen;
      }
      throw new ArgumentException(Microsoft.Xna.Framework.Resources.InvalidScreenAdapter, "adapter");
    }

    private static string GetAssemblyTitle(Assembly assembly)
    {
      if (assembly == null)
        return (string) null;
      AssemblyTitleAttribute[] customAttributes = (AssemblyTitleAttribute[]) assembly.GetCustomAttributes(typeof (AssemblyTitleAttribute), true);
      if (customAttributes != null && customAttributes.Length > 0)
        return customAttributes[0].Title;
      return (string) null;
    }

    private static string GetDefaultTitleName()
    {
      string assemblyTitle = WindowsGameWindow.GetAssemblyTitle(Assembly.GetEntryAssembly());
      if (!string.IsNullOrEmpty(assemblyTitle))
        return assemblyTitle;
      try
      {
        return Path.GetFileNameWithoutExtension(new Uri(Application.ExecutablePath).LocalPath);
      }
      catch (ArgumentNullException)
      {
      }
      catch (UriFormatException)
      {
      }
      return Microsoft.Xna.Framework.Resources.DefaultTitleName;
    }

    private static Icon FindFirstIcon(Assembly assembly)
    {
      if (assembly == null)
        return (Icon) null;
      foreach (string manifestResourceName in assembly.GetManifestResourceNames())
      {
        try
        {
          return new Icon(assembly.GetManifestResourceStream(manifestResourceName));
        }
        catch
        {
          try
          {
            IDictionaryEnumerator enumerator = new ResourceReader(assembly.GetManifestResourceStream(manifestResourceName)).GetEnumerator();
            while (enumerator.MoveNext())
            {
              Icon icon = enumerator.Value as Icon;
              if (icon != null)
                return icon;
            }
          }
          catch
          {
          }
        }
      }
      return (Icon) null;
    }

    private static Icon GetDefaultIcon()
    {
      Assembly entryAssembly = Assembly.GetEntryAssembly();
      if (entryAssembly != null)
      {
        try
        {
          Icon associatedIcon = Icon.ExtractAssociatedIcon(entryAssembly.Location);
          if (associatedIcon != null)
            return associatedIcon;
        }
        catch
        {
        }
      }
      return WindowsGameWindow.FindFirstIcon(entryAssembly) ?? new Icon(typeof (Game), "Game.ico");
    }
  }
}
