// Decompiled with JetBrains decompiler
// Type: Microsoft.Xna.Framework.GraphicsDeviceInformation
// Assembly: Microsoft.Xna.Framework.Game, Version=3.1.0.0, Culture=neutral, PublicKeyToken=6d5c3888ef60e27d
// MVID: E4BD910E-73ED-465E-A91E-14AAAB0CE109
// Assembly location: C:\WINDOWS\assembly\GAC_32\Microsoft.Xna.Framework.Game\3.1.0.0__6d5c3888ef60e27d\Microsoft.Xna.Framework.Game.dll

using Microsoft.Xna.Framework.Graphics;
using System;

namespace Microsoft.Xna.Framework
{
  public class GraphicsDeviceInformation
  {
    private PresentationParameters presentationParameters = new PresentationParameters();
    private GraphicsAdapter adapter = GraphicsAdapter.DefaultAdapter;
    private DeviceType deviceType = DeviceType.Hardware;

    public GraphicsAdapter Adapter
    {
      get
      {
        return this.adapter;
      }
      set
      {
        if (this.adapter == (GraphicsAdapter) null)
          throw new ArgumentNullException("value", Resources.NoNullUseDefaultAdapter);
        this.adapter = value;
      }
    }

    public DeviceType DeviceType
    {
      get
      {
        return this.deviceType;
      }
      set
      {
        this.deviceType = value;
      }
    }

    public PresentationParameters PresentationParameters
    {
      get
      {
        return this.presentationParameters;
      }
      set
      {
        this.presentationParameters = value;
      }
    }

    public override bool Equals(object obj)
    {
      GraphicsDeviceInformation deviceInformation = obj as GraphicsDeviceInformation;
      return deviceInformation != null && deviceInformation.adapter.Equals((object) this.adapter) && (deviceInformation.deviceType.Equals((object) this.deviceType) && deviceInformation.PresentationParameters.AutoDepthStencilFormat == this.PresentationParameters.AutoDepthStencilFormat) && (deviceInformation.PresentationParameters.BackBufferCount == this.PresentationParameters.BackBufferCount && deviceInformation.PresentationParameters.BackBufferFormat == this.PresentationParameters.BackBufferFormat && (deviceInformation.PresentationParameters.BackBufferHeight == this.PresentationParameters.BackBufferHeight && deviceInformation.PresentationParameters.BackBufferWidth == this.PresentationParameters.BackBufferWidth)) && (!(deviceInformation.PresentationParameters.DeviceWindowHandle != this.PresentationParameters.DeviceWindowHandle) && deviceInformation.PresentationParameters.EnableAutoDepthStencil == this.PresentationParameters.EnableAutoDepthStencil && (deviceInformation.PresentationParameters.FullScreenRefreshRateInHz == this.PresentationParameters.FullScreenRefreshRateInHz && deviceInformation.PresentationParameters.IsFullScreen == this.PresentationParameters.IsFullScreen) && (deviceInformation.PresentationParameters.MultiSampleQuality == this.PresentationParameters.MultiSampleQuality && deviceInformation.PresentationParameters.MultiSampleType == this.PresentationParameters.MultiSampleType && (deviceInformation.PresentationParameters.PresentationInterval == this.PresentationParameters.PresentationInterval && deviceInformation.PresentationParameters.PresentOptions == this.PresentationParameters.PresentOptions))) && deviceInformation.PresentationParameters.SwapEffect == this.PresentationParameters.SwapEffect;
    }

    public override int GetHashCode()
    {
      return this.deviceType.GetHashCode() ^ this.adapter.GetHashCode() ^ this.presentationParameters.GetHashCode();
    }

    public GraphicsDeviceInformation Clone()
    {
      return new GraphicsDeviceInformation()
      {
        presentationParameters = this.presentationParameters.Clone(),
        adapter = this.adapter,
        deviceType = this.deviceType
      };
    }
  }
}
