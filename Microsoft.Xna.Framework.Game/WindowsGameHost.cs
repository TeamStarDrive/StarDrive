// Decompiled with JetBrains decompiler
// Type: Microsoft.Xna.Framework.WindowsGameHost
// Assembly: Microsoft.Xna.Framework.Game, Version=3.1.0.0, Culture=neutral, PublicKeyToken=6d5c3888ef60e27d
// MVID: E4BD910E-73ED-465E-A91E-14AAAB0CE109
// Assembly location: C:\WINDOWS\assembly\GAC_32\Microsoft.Xna.Framework.Game\3.1.0.0__6d5c3888ef60e27d\Microsoft.Xna.Framework.Game.dll

using Microsoft.Xna.Framework.Audio;
using Microsoft.Xna.Framework.GamerServices;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using System;
using System.Globalization;
using System.Runtime.InteropServices;
using System.Windows.Forms;

namespace Microsoft.Xna.Framework
{
  internal class WindowsGameHost : GameHost
  {
    private Game game;
    private WindowsGameWindow gameWindow;
    private bool doneRun;
    private bool exitRequested;
    private bool onIdleRegistered;

    internal override GameWindow Window
    {
      get
      {
        return (GameWindow) this.gameWindow;
      }
    }

    public WindowsGameHost(Game game)
    {
      this.game = game;
      this.LockThreadToProcessor();
      this.gameWindow = new WindowsGameWindow();
      Mouse.WindowHandle = this.gameWindow.Handle;
      this.gameWindow.IsMouseVisible = game.IsMouseVisible;
      this.gameWindow.Activated += new EventHandler(this.GameWindowActivated);
      this.gameWindow.Deactivated += new EventHandler(this.GameWindowDeactivated);
      this.gameWindow.Suspend += new EventHandler(this.GameWindowSuspend);
      this.gameWindow.Resume += new EventHandler(this.GameWindowResume);
    }

    private void GameWindowSuspend(object sender, EventArgs e)
    {
      this.OnSuspend();
    }

    private void GameWindowResume(object sender, EventArgs e)
    {
      this.OnResume();
    }

    private void GameWindowDeactivated(object sender, EventArgs e)
    {
      this.OnDeactivated();
    }

    private void GameWindowActivated(object sender, EventArgs e)
    {
      this.OnActivated();
    }

    private void ApplicationIdle(object sender, EventArgs e)
    {
      NativeMethods.Message msg;
      while (!NativeMethods.PeekMessage(out msg, IntPtr.Zero, 0U, 0U, 0U))
      {
        if (this.exitRequested)
        {
          this.gameWindow.Close();
        }
        else
        {
          this.gameWindow.Tick();
          this.OnIdle();
          if (GamerServicesDispatcher.IsInitialized)
            this.gameWindow.IsGuideVisible = Guide.IsVisible;
        }
      }
    }

    internal override void Run()
    {
      if (this.doneRun)
        throw new InvalidOperationException(Resources.NoMultipleRuns);
      try
      {
        Application.Idle += this.ApplicationIdle;
        Application.Run(this.gameWindow.Form);
      }
      finally
      {
        Application.Idle -= this.ApplicationIdle;
        this.doneRun = true;
        this.OnExiting();
      }
    }
    
    internal override void RunOne()
    {
        if (!onIdleRegistered)
        {
            Application.Idle += this.ApplicationIdle;
            onIdleRegistered = true;
        }
        Application.DoEvents();
    }

    internal override void Exit()
    {
      this.exitRequested = true;
    }

    private void LockThreadToProcessor()
    {
      UIntPtr lpProcessAffinityMask = UIntPtr.Zero;
      UIntPtr lpSystemAffinityMask = UIntPtr.Zero;
      if (!GetProcessAffinityMask(GetCurrentProcess(), out lpProcessAffinityMask, out lpSystemAffinityMask) || !(lpProcessAffinityMask != UIntPtr.Zero))
        return;
      var affinityMask = (UIntPtr)(lpProcessAffinityMask.ToUInt64() & (ulong)(~(long)lpProcessAffinityMask.ToUInt64() + 1L));
      SetThreadAffinityMask(GetCurrentThread(), affinityMask);
    }

    [DllImport("kernel32.dll")]
    private static extern IntPtr GetCurrentThread();

    [DllImport("kernel32.dll")]
    private static extern IntPtr GetCurrentProcess();

    [DllImport("kernel32.dll")]
    private static extern UIntPtr SetThreadAffinityMask(IntPtr hThread, UIntPtr dwThreadAffinityMask);

    [DllImport("kernel32.dll", SetLastError = true)]
    private static extern bool GetProcessAffinityMask(IntPtr hProcess, out UIntPtr lpProcessAffinityMask, out UIntPtr lpSystemAffinityMask);

    internal override bool ShowMissingRequirementMessage(Exception exception)
    {
      string text;
      if (exception is NoSuitableGraphicsDeviceException)
      {
        text = Resources.NoSuitableGraphicsDevice + "\n\n" + exception.Message;
        object obj1 = exception.Data[(object) "MinimumPixelShaderProfile"];
        object obj2 = exception.Data[(object) "MinimumVertexShaderProfile"];
        if (obj1 is ShaderProfile && obj2 is ShaderProfile)
        {
          string shaderProfileName1 = WindowsGameHost.GetShaderProfileName((ShaderProfile) obj1);
          string shaderProfileName2 = WindowsGameHost.GetShaderProfileName((ShaderProfile) obj2);
          text = text + "\n\n" + string.Format((IFormatProvider) CultureInfo.CurrentCulture, Resources.NoSuitableGraphicsDeviceDetails, new object[2]
          {
            (object) shaderProfileName1,
            (object) shaderProfileName2
          });
        }
      }
      else
      {
        if (!(exception is NoAudioHardwareException))
          return base.ShowMissingRequirementMessage(exception);
        text = Resources.NoAudioHardware;
      }
      int num = (int) MessageBox.Show((IWin32Window) this.gameWindow.Form, text, this.gameWindow.Title, MessageBoxButtons.OK, System.Windows.Forms.MessageBoxIcon.Hand);
      return true;
    }

    private static string GetShaderProfileName(ShaderProfile shaderProfile)
    {
      switch (shaderProfile)
      {
        case ShaderProfile.PS_1_1:
          return "1.1";
        case ShaderProfile.PS_1_2:
          return "1.2";
        case ShaderProfile.PS_1_3:
          return "1.3";
        case ShaderProfile.PS_1_4:
          return "1.4";
        case ShaderProfile.PS_2_0:
          return "2.0";
        case ShaderProfile.PS_2_A:
          return "2.0a";
        case ShaderProfile.PS_2_B:
          return "2.0b";
        case ShaderProfile.PS_2_SW:
          return "2.0sw";
        case ShaderProfile.PS_3_0:
          return "3.0";
        case ShaderProfile.VS_1_1:
          return "1.1";
        case ShaderProfile.VS_2_0:
          return "2.0";
        case ShaderProfile.VS_2_A:
          return "2.0a";
        case ShaderProfile.VS_2_SW:
          return "2.0sw";
        case ShaderProfile.VS_3_0:
          return "3.0";
        default:
          return shaderProfile.ToString();
      }
    }
  }
}
