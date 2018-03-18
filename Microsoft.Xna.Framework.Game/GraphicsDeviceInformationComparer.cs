// Decompiled with JetBrains decompiler
// Type: Microsoft.Xna.Framework.GraphicsDeviceInformationComparer
// Assembly: Microsoft.Xna.Framework.Game, Version=3.1.0.0, Culture=neutral, PublicKeyToken=6d5c3888ef60e27d
// MVID: E4BD910E-73ED-465E-A91E-14AAAB0CE109
// Assembly location: C:\WINDOWS\assembly\GAC_32\Microsoft.Xna.Framework.Game\3.1.0.0__6d5c3888ef60e27d\Microsoft.Xna.Framework.Game.dll

using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;

namespace Microsoft.Xna.Framework
{
  internal class GraphicsDeviceInformationComparer : IComparer<GraphicsDeviceInformation>
  {
    private GraphicsDeviceManager graphics;

    public GraphicsDeviceInformationComparer(GraphicsDeviceManager graphicsComponent)
    {
      this.graphics = graphicsComponent;
    }

    public int Compare(GraphicsDeviceInformation d1, GraphicsDeviceInformation d2)
    {
      if (d1.DeviceType != d2.DeviceType)
        return d1.DeviceType >= d2.DeviceType ? 1 : -1;
      PresentationParameters presentationParameters1 = d1.PresentationParameters;
      PresentationParameters presentationParameters2 = d2.PresentationParameters;
      if (presentationParameters1.IsFullScreen != presentationParameters2.IsFullScreen)
        return this.graphics.IsFullScreen != presentationParameters1.IsFullScreen ? 1 : -1;
      int num1 = this.RankFormat(presentationParameters1.BackBufferFormat);
      int num2 = this.RankFormat(presentationParameters2.BackBufferFormat);
      if (num1 != num2)
        return num1 >= num2 ? 1 : -1;
      if (presentationParameters1.MultiSampleType != presentationParameters2.MultiSampleType)
        return (presentationParameters1.MultiSampleType == MultiSampleType.NonMaskable ? 17 : (int) presentationParameters1.MultiSampleType) <= (presentationParameters2.MultiSampleType == MultiSampleType.NonMaskable ? 17 : (int) presentationParameters2.MultiSampleType) ? 1 : -1;
      if (presentationParameters1.MultiSampleQuality != presentationParameters2.MultiSampleQuality)
        return presentationParameters1.MultiSampleQuality <= presentationParameters2.MultiSampleQuality ? 1 : -1;
      float num3 = this.graphics.PreferredBackBufferWidth == 0 || this.graphics.PreferredBackBufferHeight == 0 ? (float) GraphicsDeviceManager.DefaultBackBufferWidth / (float) GraphicsDeviceManager.DefaultBackBufferHeight : (float) this.graphics.PreferredBackBufferWidth / (float) this.graphics.PreferredBackBufferHeight;
      float num4 = (float) presentationParameters1.BackBufferWidth / (float) presentationParameters1.BackBufferHeight;
      float num5 = (float) presentationParameters2.BackBufferWidth / (float) presentationParameters2.BackBufferHeight;
      float num6 = Math.Abs(num4 - num3);
      float num7 = Math.Abs(num5 - num3);
      if ((double) Math.Abs(num6 - num7) > 0.200000002980232)
        return (double) num6 >= (double) num7 ? 1 : -1;
      int num8;
      int num9;
      if (this.graphics.IsFullScreen)
      {
        if (this.graphics.PreferredBackBufferWidth == 0 || this.graphics.PreferredBackBufferHeight == 0)
        {
          GraphicsAdapter adapter1 = d1.Adapter;
          num8 = adapter1.CurrentDisplayMode.Width * adapter1.CurrentDisplayMode.Height;
          GraphicsAdapter adapter2 = d2.Adapter;
          num9 = adapter2.CurrentDisplayMode.Width * adapter2.CurrentDisplayMode.Height;
        }
        else
          num8 = num9 = this.graphics.PreferredBackBufferWidth * this.graphics.PreferredBackBufferHeight;
      }
      else
        num8 = this.graphics.PreferredBackBufferWidth == 0 || this.graphics.PreferredBackBufferHeight == 0 ? (num9 = GraphicsDeviceManager.DefaultBackBufferWidth * GraphicsDeviceManager.DefaultBackBufferHeight) : (num9 = this.graphics.PreferredBackBufferWidth * this.graphics.PreferredBackBufferHeight);
      int num10 = Math.Abs(presentationParameters1.BackBufferWidth * presentationParameters1.BackBufferHeight - num8);
      int num11 = Math.Abs(presentationParameters2.BackBufferWidth * presentationParameters2.BackBufferHeight - num9);
      if (num10 != num11)
        return num10 >= num11 ? 1 : -1;
      if (this.graphics.IsFullScreen && presentationParameters1.FullScreenRefreshRateInHz != presentationParameters2.FullScreenRefreshRateInHz)
        return Math.Abs(d1.Adapter.CurrentDisplayMode.RefreshRate - presentationParameters1.FullScreenRefreshRateInHz) > Math.Abs(d2.Adapter.CurrentDisplayMode.RefreshRate - presentationParameters2.FullScreenRefreshRateInHz) ? 1 : -1;
      if (d1.Adapter != d2.Adapter)
      {
        if (d1.Adapter.IsDefaultAdapter)
          return -1;
        if (d2.Adapter.IsDefaultAdapter)
          return 1;
      }
      return 0;
    }

    private int RankFormat(SurfaceFormat format)
    {
      int num1 = Array.IndexOf<SurfaceFormat>(GraphicsDeviceManager.ValidBackBufferFormats, format);
      if (num1 == -1)
        return int.MaxValue;
      int num2 = Array.IndexOf<SurfaceFormat>(GraphicsDeviceManager.ValidBackBufferFormats, this.graphics.PreferredBackBufferFormat);
      if (num2 == -1)
        return GraphicsDeviceManager.ValidBackBufferFormats.Length - num1;
      if (num1 >= num2)
        return num1 - num2;
      return int.MaxValue;
    }
  }
}
