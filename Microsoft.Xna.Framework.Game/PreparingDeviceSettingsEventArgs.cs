﻿// Decompiled with JetBrains decompiler
// Type: Microsoft.Xna.Framework.PreparingDeviceSettingsEventArgs
// Assembly: Microsoft.Xna.Framework.Game, Version=3.1.0.0, Culture=neutral, PublicKeyToken=6d5c3888ef60e27d
// MVID: E4BD910E-73ED-465E-A91E-14AAAB0CE109
// Assembly location: C:\WINDOWS\assembly\GAC_32\Microsoft.Xna.Framework.Game\3.1.0.0__6d5c3888ef60e27d\Microsoft.Xna.Framework.Game.dll

using System;

namespace Microsoft.Xna.Framework
{
  public class PreparingDeviceSettingsEventArgs : EventArgs
  {
    private GraphicsDeviceInformation graphicsDeviceInformation;

    public GraphicsDeviceInformation GraphicsDeviceInformation
    {
      get
      {
        return this.graphicsDeviceInformation;
      }
    }

    public PreparingDeviceSettingsEventArgs(GraphicsDeviceInformation graphicsDeviceInformation)
    {
      this.graphicsDeviceInformation = graphicsDeviceInformation;
    }
  }
}
