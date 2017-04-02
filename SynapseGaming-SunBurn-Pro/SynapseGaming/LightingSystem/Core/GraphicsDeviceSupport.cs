// Decompiled with JetBrains decompiler
// Type: SynapseGaming.LightingSystem.Core.GraphicsDeviceSupport
// Assembly: SynapseGaming-SunBurn-Pro, Version=1.3.2.8, Culture=neutral, PublicKeyToken=c23c60523565dbfd
// MVID: A5F03349-72AC-4BAA-AEEE-9AB9B77E0A39
// Assembly location: C:\Projects\BlackBox\StarDrive\SynapseGaming-SunBurn-Pro.dll

using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;

namespace SynapseGaming.LightingSystem.Core
{
  /// <summary>
  /// Provides information on the device capabilities supported by the current hardware
  /// and if those capabilities are allowed by the current configuration.  This allows
  /// users to select various system specific configurations and for developers to test
  /// a wide variety of configurations on a single machine.
  /// 
  /// Check a capability property to see if it's supported and allowed.  Setting a
  /// capability property to true will allow it only if the current hardware supports it.
  /// </summary>
  public class GraphicsDeviceSupport
  {
    private bool bool_0 = true;
    private int int_2 = 1;
    private int int_3 = 1;
    private Dictionary<Microsoft.Xna.Framework.Graphics.SurfaceFormat, GraphicsDeviceSupport.FormatSupport> dictionary_0 = new Dictionary<Microsoft.Xna.Framework.Graphics.SurfaceFormat, GraphicsDeviceSupport.FormatSupport>(16);
    private bool bool_1;
    private int int_0;
    private int int_1;
    private GraphicsDevice graphicsDevice_0;

    /// <summary>
    /// Checks if two sided stencil rendering is supported by the hardware and allowed by the current configuration.
    /// </summary>
    public bool StencilTwoSided
    {
      get
      {
        return GraphicsDeviceSupport.smethod_0(this.bool_0, this.bool_1);
      }
      set
      {
        this.bool_0 = value;
      }
    }

    /// <summary>The maximum texture size supported by the hardware.</summary>
    public int MaxTextureSize
    {
      get
      {
        return this.int_0;
      }
    }

    /// <summary>
    /// The maximum anisotropy value supported by the hardware.
    /// </summary>
    public int MaxAnisotropy
    {
      get
      {
        return this.int_1;
      }
    }

    /// <summary>
    /// The maximum pixel shader version supported by the hardware.
    /// </summary>
    public int PixelShaderMajorVersion
    {
      get
      {
        return this.int_2;
      }
    }

    /// <summary>
    /// The maximum pixel shader version supported by the hardware.
    /// </summary>
    public int PixelShaderMinorVersion
    {
      get
      {
        return this.int_3;
      }
    }

    /// <summary>List of surface formats and related hardware support.</summary>
    public Dictionary<Microsoft.Xna.Framework.Graphics.SurfaceFormat, GraphicsDeviceSupport.FormatSupport> SurfaceFormat
    {
      get
      {
        return this.dictionary_0;
      }
    }

    internal GraphicsDeviceSupport(GraphicsDevice graphicsDevice_1)
    {
      this.graphicsDevice_0 = graphicsDevice_1;
      GraphicsDeviceCapabilities deviceCapabilities = graphicsDevice_1.GraphicsDeviceCapabilities;
      this.bool_1 = deviceCapabilities.StencilCapabilities.SupportsTwoSided;
      this.int_0 = deviceCapabilities.MaxTextureWidth;
      this.int_1 = deviceCapabilities.MaxAnisotropy;
      this.int_2 = deviceCapabilities.PixelShaderVersion.Major;
      this.int_3 = deviceCapabilities.PixelShaderVersion.Minor;
      foreach (Microsoft.Xna.Framework.Graphics.SurfaceFormat surfaceFormat in Enum.GetValues(typeof (Microsoft.Xna.Framework.Graphics.SurfaceFormat)))
      {
        bool bool_10 = GraphicsAdapter.DefaultAdapter.CheckDeviceFormat(DeviceType.Hardware, graphicsDevice_1.DisplayMode.Format, TextureUsage.None, QueryUsages.None, ResourceType.DepthStencilBuffer, surfaceFormat);
        bool bool_11 = GraphicsAdapter.DefaultAdapter.CheckDeviceFormat(DeviceType.Hardware, graphicsDevice_1.DisplayMode.Format, TextureUsage.None, QueryUsages.None, ResourceType.Texture2D, surfaceFormat);
        bool bool_12 = GraphicsAdapter.DefaultAdapter.CheckDeviceFormat(DeviceType.Hardware, graphicsDevice_1.DisplayMode.Format, TextureUsage.None, QueryUsages.None, ResourceType.RenderTarget, surfaceFormat);
        bool bool_14 = GraphicsAdapter.DefaultAdapter.CheckDeviceFormat(DeviceType.Hardware, graphicsDevice_1.DisplayMode.Format, TextureUsage.None, QueryUsages.Filter, ResourceType.Texture2D, surfaceFormat);
        bool bool_13 = GraphicsAdapter.DefaultAdapter.CheckDeviceFormat(DeviceType.Hardware, graphicsDevice_1.DisplayMode.Format, TextureUsage.None, QueryUsages.PostPixelShaderBlending, ResourceType.RenderTarget, surfaceFormat);
        this.dictionary_0.Add(surfaceFormat, new GraphicsDeviceSupport.FormatSupport(bool_10, bool_11, bool_12, bool_13, bool_14));
      }
    }

    internal static bool smethod_0(bool bool_2, bool bool_3)
    {
      if (!bool_2)
        return false;
      return bool_3;
    }

    /// <summary>
    /// Finds the first supported surface format in a list of requested formats. Always sort the
    /// requested format list in order of preference to ensure the supported and returned format is the best possible match.
    /// </summary>
    /// <param name="requestedformats">List of requested formats.</param>
    /// <param name="texture">Returned format must support use as a texture.</param>
    /// <param name="rendertarget">Returned format must support use as a render target.</param>
    /// <param name="blending">Returned format must support render target blending.</param>
    /// <param name="filtering">Returned format must support texture filtering.</param>
    /// <returns></returns>
    public Microsoft.Xna.Framework.Graphics.SurfaceFormat FindSupportedFormat(Microsoft.Xna.Framework.Graphics.SurfaceFormat[] requestedformats, bool texture, bool rendertarget, bool blending, bool filtering)
    {
      for (int index = 0; index < requestedformats.Length; ++index)
      {
        Microsoft.Xna.Framework.Graphics.SurfaceFormat requestedformat = requestedformats[index];
        if ((!texture || this.SurfaceFormat[requestedformat].Texture) && (!rendertarget || this.SurfaceFormat[requestedformat].RenderTarget) && ((!blending || this.SurfaceFormat[requestedformat].Blending) && (!filtering || this.SurfaceFormat[requestedformat].Filtering)))
          return requestedformat;
      }
      return Microsoft.Xna.Framework.Graphics.SurfaceFormat.Unknown;
    }

    /// <summary>
    /// Finds the first supported depth format in a list of requested formats. Always sort the requested
    /// format list in order of preference to ensure the supported and returned format is the best possible match.
    /// </summary>
    /// <param name="requestedformats">List of requested formats.</param>
    /// <param name="rendertargetformat">Format of the render target or back buffer the requested format will be paired with.</param>
    /// <returns></returns>
    public DepthFormat FindSupportedFormat(DepthFormat[] requestedformats, Microsoft.Xna.Framework.Graphics.SurfaceFormat rendertargetformat)
    {
      for (int index = 0; index < requestedformats.Length; ++index)
      {
        DepthFormat requestedformat = requestedformats[index];
        if (this.SurfaceFormat[(Microsoft.Xna.Framework.Graphics.SurfaceFormat) requestedformat].DepthBuffer && GraphicsAdapter.DefaultAdapter.CheckDepthStencilMatch(DeviceType.Hardware, this.graphicsDevice_0.DisplayMode.Format, rendertargetformat, requestedformat))
          return requestedformat;
      }
      return DepthFormat.Unknown;
    }

    /// <summary>
    /// Describes the hardware support for a specific surface format.
    /// </summary>
    public class FormatSupport
    {
      private bool bool_0 = true;
      private bool bool_1 = true;
      private bool bool_2 = true;
      private bool bool_3 = true;
      private bool bool_4 = true;
      private bool bool_5;
      private bool bool_6;
      private bool bool_7;
      private bool bool_8;
      private bool bool_9;

      /// <summary>
      /// Hardware supports using the format as a depth texture.
      /// </summary>
      public bool DepthBuffer
      {
        get
        {
          return GraphicsDeviceSupport.smethod_0(this.bool_0, this.bool_5);
        }
        set
        {
          this.bool_0 = value;
        }
      }

      /// <summary>Hardware supports using the format as a texture.</summary>
      public bool Texture
      {
        get
        {
          return GraphicsDeviceSupport.smethod_0(this.bool_1, this.bool_6);
        }
        set
        {
          this.bool_1 = value;
        }
      }

      /// <summary>
      /// Hardware supports using the format as a render target.
      /// </summary>
      public bool RenderTarget
      {
        get
        {
          return GraphicsDeviceSupport.smethod_0(this.bool_2, this.bool_7);
        }
        set
        {
          this.bool_2 = value;
        }
      }

      /// <summary>
      /// Hardware supports blending when using the format as a render target.
      /// </summary>
      public bool Blending
      {
        get
        {
          return GraphicsDeviceSupport.smethod_0(this.bool_3, this.bool_8);
        }
        set
        {
          this.bool_3 = value;
        }
      }

      /// <summary>
      /// Hardware supports filtering when using the format as a texture.
      /// </summary>
      public bool Filtering
      {
        get
        {
          return GraphicsDeviceSupport.smethod_0(this.bool_4, this.bool_9);
        }
        set
        {
          this.bool_4 = value;
        }
      }

      internal FormatSupport(bool bool_10, bool bool_11, bool bool_12, bool bool_13, bool bool_14)
      {
        this.bool_5 = bool_10;
        this.bool_6 = bool_11;
        this.bool_7 = bool_12;
        this.bool_8 = bool_13;
        this.bool_9 = bool_14;
      }
    }
  }
}
