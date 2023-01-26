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

namespace Microsoft.Xna.Framework;

internal class WindowsGameHost : GameHost
{
    readonly Game TheGame;
    readonly WindowsGameWindow gameWindow;
    bool doneRun;
    bool exitRequested;
    bool onIdleRegistered;

    internal override GameWindow Window => (GameWindow) gameWindow;

    public WindowsGameHost(Game game)
    {
        TheGame = game;
        LockThreadToProcessor();
        gameWindow = new();
        Mouse.WindowHandle = gameWindow.Handle;
        gameWindow.IsMouseVisible = game.IsMouseVisible;
        gameWindow.Activated += GameWindowActivated;
        gameWindow.Deactivated += GameWindowDeactivated;
        gameWindow.Suspend += GameWindowSuspend;
        gameWindow.Resume += GameWindowResume;
    }

    void GameWindowSuspend(object sender, EventArgs e)
    {
        OnSuspend();
    }

    void GameWindowResume(object sender, EventArgs e)
    {
        OnResume();
    }

    void GameWindowDeactivated(object sender, EventArgs e)
    {
        OnDeactivated();
    }

    void GameWindowActivated(object sender, EventArgs e)
    {
        OnActivated();
    }

    void ApplicationIdle(object sender, EventArgs e)
    {
        // while there are no pending messages to process, call OnIdle()
        while (!NativeMethods.PeekMessage(out _, IntPtr.Zero, 0U, 0U, 0U))
        {
            if (exitRequested)
            {
                gameWindow.Close();
            }
            else
            {
                gameWindow.Tick();
                OnIdle(); // call the main game loop
                if (GamerServicesDispatcher.IsInitialized)
                    gameWindow.IsGuideVisible = Guide.IsVisible;
            }
        }
    }

    internal override void Run()
    {
        if (doneRun)
            throw new InvalidOperationException(Resources.NoMultipleRuns);
        try
        {
            Application.Idle += ApplicationIdle;
            Application.Run(gameWindow.Form);
        }
        finally
        {
            Application.Idle -= ApplicationIdle;
            doneRun = true;
            OnExiting();
        }
    }

    internal override void RunOne()
    {
        if (!onIdleRegistered)
        {
            Application.Idle += ApplicationIdle;
            onIdleRegistered = true;
        }
        Application.DoEvents();
    }

    internal override void Exit()
    {
        exitRequested = true;
    }

    static void LockThreadToProcessor()
    {
        if (!GetProcessAffinityMask(GetCurrentProcess(), out UIntPtr lpProcessAffinityMask, out UIntPtr _) || lpProcessAffinityMask == UIntPtr.Zero)
            return;
        var affinityMask = (UIntPtr)(lpProcessAffinityMask.ToUInt64() & (ulong)(~(long)lpProcessAffinityMask.ToUInt64() + 1L));
        SetThreadAffinityMask(GetCurrentThread(), affinityMask);
    }

    [DllImport("kernel32.dll")]
    static extern IntPtr GetCurrentThread();

    [DllImport("kernel32.dll")]
    static extern IntPtr GetCurrentProcess();

    [DllImport("kernel32.dll")]
    static extern UIntPtr SetThreadAffinityMask(IntPtr hThread, UIntPtr dwThreadAffinityMask);

    [DllImport("kernel32.dll", SetLastError = true)]
    static extern bool GetProcessAffinityMask(IntPtr hProcess, out UIntPtr lpProcessAffinityMask, out UIntPtr lpSystemAffinityMask);

    internal override bool ShowMissingRequirementMessage(Exception exception)
    {
        string text;
        if (exception is NoSuitableGraphicsDeviceException)
        {
            text = Resources.NoSuitableGraphicsDevice + "\n\n" + exception.Message;
            object obj1 = exception.Data[(object) "MinimumPixelShaderProfile"];
            object obj2 = exception.Data[(object) "MinimumVertexShaderProfile"];
            if (obj1 is ShaderProfile profile && obj2 is ShaderProfile shaderProfile)
            {
                string shaderProfileName1 = GetShaderProfileName(profile);
                string shaderProfileName2 = GetShaderProfileName(shaderProfile);
                text = text + "\n\n" + string.Format((IFormatProvider) CultureInfo.CurrentCulture, Resources.NoSuitableGraphicsDeviceDetails, new object[2]
                {
                    shaderProfileName1,
                    shaderProfileName2
                });
            }
        }
        else
        {
            if (exception is not NoAudioHardwareException)
                return base.ShowMissingRequirementMessage(exception);
            text = Resources.NoAudioHardware;
        }
        if (gameWindow.Form?.IsDisposed == false)
            MessageBox.Show(gameWindow.Form, text, gameWindow.Title, MessageBoxButtons.OK, System.Windows.Forms.MessageBoxIcon.Hand);
        return true;
    }

    static string GetShaderProfileName(ShaderProfile shaderProfile)
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
