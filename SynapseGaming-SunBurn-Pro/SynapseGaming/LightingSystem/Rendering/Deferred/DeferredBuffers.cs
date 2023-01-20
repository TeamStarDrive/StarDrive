// Decompiled with JetBrains decompiler
// Type: SynapseGaming.LightingSystem.Rendering.Deferred.DeferredBuffers
// Assembly: SynapseGaming-SunBurn-Pro, Version=1.3.2.8, Culture=neutral, PublicKeyToken=c23c60523565dbfd
// MVID: A5F03349-72AC-4BAA-AEEE-9AB9B77E0A39
// Assembly location: C:\Projects\BlackBox\StarDrive\SynapseGaming-SunBurn-Pro.dll

using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework.Graphics;
using ns11;
using ns3;
using ns8;
using SynapseGaming.LightingSystem.Core;

namespace SynapseGaming.LightingSystem.Rendering.Deferred
{
  /// <summary>
  /// Manages deferred buffers (render targets) at a specific resolution (width and height)
  /// for optimal sharing across multiple deferred materials.
  /// </summary>
  public sealed class DeferredBuffers : IUnloadable, IDisposable
  {
    private SurfaceFormat surfaceFormat_0 = SurfaceFormat.Unknown;
    private List<int> list_0 = new List<int>(4);
      private DeferredBufferSizing deferredBufferSizing_0;
    private DetailPreference detailPreference_0;
    private DetailPreference detailPreference_1;
    private IGraphicsDeviceService igraphicsDeviceService_0;
    private GraphicsDeviceMonitor GraphicsDeviceMonitor0;
    private DepthStencilBuffer depthStencilBuffer_0;
    private RenderTarget2D renderTarget2D_0;
    private RenderTarget2D renderTarget2D_1;
    private RenderTarget2D renderTarget2D_2;
    private RenderTarget2D renderTarget2D_3;
    private FullFrameQuad fullFrameQuad_0;

    /// <summary>Current width of the deferred buffers.</summary>
    public int Width { get; private set; }

      /// <summary>Current height of the deferred buffers.</summary>
    public int Height { get; private set; }

      /// <summary>
    /// Depth stencil buffer properly sized and formatted for rendering with the deferred
    /// buffers (only valid in between calls to BeginFrameRendering and EndFrameRendering).
    /// </summary>
    public DepthStencilBuffer DepthStencilBuffer
    {
      get
      {
        if (this.depthStencilBuffer_0 == null)
          this.depthStencilBuffer_0 = new DepthStencilBuffer(this.igraphicsDeviceService_0.GraphicsDevice, this.Width, this.Height, DepthFormat.Depth24Stencil8);
        return this.depthStencilBuffer_0;
      }
    }

    /// <summary>
    /// Provides a full frame renderable quad sized specifically for the contained buffers.
    /// </summary>
    public FullFrameQuad FullFrameQuad
    {
      get
      {
        if (this.fullFrameQuad_0 == null)
          this.fullFrameQuad_0 = new FullFrameQuad(this.igraphicsDeviceService_0.GraphicsDevice, this.Width, this.Height);
        return this.fullFrameQuad_0;
      }
    }

    /// <summary>Creates a new DeferredBuffers instance.</summary>
    /// <param name="graphicsdevicemanager"></param>
    /// <param name="buffersize">Determines how deferred buffers are sized.</param>
    /// <param name="customwidth">Custom buffer width when buffersize is CustomSize.</param>
    /// <param name="customheight">Custom buffer height when buffersize is CustomSize.</param>
    /// <param name="precisionmode">Increases visual quality at the cost of performance.
    /// Generally used in visualizations, most games do not need this option.</param>
    /// <param name="lightingrange">Increases lighting quality at the cost of performance.
    /// Adds additional lighting range when using HDR.  Not supported on Xbox.</param>
    public DeferredBuffers(IGraphicsDeviceService graphicsdevicemanager, DeferredBufferSizing buffersize, int customwidth, int customheight, DetailPreference precisionmode, DetailPreference lightingrange)
    {
      this.igraphicsDeviceService_0 = graphicsdevicemanager;
      this.deferredBufferSizing_0 = buffersize;
      this.Width = customwidth;
      this.Height = customheight;
      this.detailPreference_0 = precisionmode;
      this.detailPreference_1 = lightingrange;
      this.GraphicsDeviceMonitor0 = new GraphicsDeviceMonitor(this.igraphicsDeviceService_0);
    }

    /// <summary>
    /// Gets one of the common (non-auxiliary) deferred buffers (only valid in between
    /// calls to BeginFrameRendering and EndFrameRendering, and after BuildDeferredPasses).
    /// </summary>
    /// <param name="buffertype"></param>
    /// <returns></returns>
    public RenderTarget2D GetDeferredBuffer(DeferredBufferType buffertype)
    {
      if (buffertype == DeferredBufferType.DepthAndSpecularPower)
        return this.renderTarget2D_0;
      if (buffertype == DeferredBufferType.NormalViewSpaceAndSpecular)
        return this.renderTarget2D_1;
      if (buffertype == DeferredBufferType.LightingDiffuse)
        return this.renderTarget2D_2;
      if (buffertype == DeferredBufferType.LightingSpecular)
        return this.renderTarget2D_3;
      return null;
    }

    internal SurfaceFormat method_0()
    {
      if (this.surfaceFormat_0 != SurfaceFormat.Unknown)
        return this.surfaceFormat_0;
      GraphicsDeviceSupport.FormatSupport formatSupport = LightingSystemManager.Instance.GetGraphicsDeviceSupport(this.igraphicsDeviceService_0.GraphicsDevice).SurfaceFormat[SurfaceFormat.Rgba1010102];
      this.surfaceFormat_0 = !formatSupport.RenderTarget || !formatSupport.Blending || !formatSupport.Filtering ? (this.detailPreference_1 != DetailPreference.Medium ? SurfaceFormat.Color : SurfaceFormat.HalfVector4) : SurfaceFormat.Rgba1010102;
      return this.surfaceFormat_0;
    }

    internal void method_1(Class59 class59_0)
    {
      bool flag1 = false;
      bool flag2 = false;
      bool flag3 = false;
      bool flag4 = false;
      GraphicsDevice graphicsDevice = this.igraphicsDeviceService_0.GraphicsDevice;
      this.list_0.Clear();
      foreach (Class58 channel in class59_0.Channels)
      {
        if (channel != null)
        {
          if (channel.Buffer == DeferredBufferType.DepthAndSpecularPower)
          {
            if (flag1)
              this.method_3("DepthAndSpecularPower");
            flag1 = true;
            if (this.detailPreference_0 == DetailPreference.High)
              this.method_2(channel, ref this.renderTarget2D_0, SurfaceFormat.Vector2);
            else
              this.method_2(channel, ref this.renderTarget2D_0, SurfaceFormat.HalfVector2);
          }
          else if (channel.Buffer == DeferredBufferType.NormalViewSpaceAndSpecular)
          {
            if (flag2)
              this.method_3("NormalViewSpaceAndSpecular");
            flag2 = true;
            if (this.detailPreference_0 == DetailPreference.High)
              this.method_2(channel, ref this.renderTarget2D_1, SurfaceFormat.HalfVector4);
            else
              this.method_2(channel, ref this.renderTarget2D_1, SurfaceFormat.Color);
          }
          else if (channel.Buffer == DeferredBufferType.LightingDiffuse)
          {
            if (flag3)
              this.method_3("LightingDiffuse");
            flag3 = true;
            if (this.detailPreference_1 == DetailPreference.High)
              this.method_2(channel, ref this.renderTarget2D_2, SurfaceFormat.HalfVector4);
            else
              this.method_2(channel, ref this.renderTarget2D_2, this.method_0());
          }
          else
          {
            if (channel.Buffer != DeferredBufferType.LightingSpecular)
              throw new Exception("Invalid deferred buffer type.");
            if (flag4)
              this.method_3("LightingSpecular");
            flag4 = true;
            if (this.detailPreference_1 == DetailPreference.High)
              this.method_2(channel, ref this.renderTarget2D_3, SurfaceFormat.HalfVector4);
            else
              this.method_2(channel, ref this.renderTarget2D_3, this.method_0());
          }
        }
      }
    }

    private void method_2(Class58 class58_0, ref RenderTarget2D renderTarget2D_4, SurfaceFormat surfaceFormat_1)
    {
      if (renderTarget2D_4 == null)
        renderTarget2D_4 = new RenderTarget2D(this.igraphicsDeviceService_0.GraphicsDevice, this.Width, this.Height, 1, surfaceFormat_1, MultiSampleType.None, 0, RenderTargetUsage.PlatformContents);
      class58_0.Format = renderTarget2D_4.Format;
    }

    private void method_3(string string_0)
    {
      throw new Exception("Deferred buffer type '" + string_0 + "' already assigned - reassignment will overwrite previous data.");
    }

    /// <summary>Sets up the object prior to rendering.</summary>
    /// <param name="scenestate"></param>
    public void BeginFrameRendering(ISceneState scenestate)
    {
      if (this.GraphicsDeviceMonitor0.Changed)
        this.Unload();
      if (this.deferredBufferSizing_0 != DeferredBufferSizing.ResizeToBackBuffer)
        return;
      PresentationParameters presentationParameters = this.igraphicsDeviceService_0.GraphicsDevice.PresentationParameters;
      if (this.Width == presentationParameters.BackBufferWidth && this.Height == presentationParameters.BackBufferHeight)
        return;
      this.Width = presentationParameters.BackBufferWidth;
      this.Height = presentationParameters.BackBufferHeight;
      this.Unload();
    }

    /// <summary>Finalizes rendering.</summary>
    public void EndFrameRendering()
    {
    }

    public void Dispose()
    {
      Disposable.Dispose(ref this.depthStencilBuffer_0);
      Disposable.Dispose(ref this.fullFrameQuad_0);
      Disposable.Dispose(ref this.renderTarget2D_0);
      Disposable.Dispose(ref this.renderTarget2D_1);
      Disposable.Dispose(ref this.renderTarget2D_2);
      Disposable.Dispose(ref this.renderTarget2D_3);
    }

    /// <summary>
    /// Disposes any graphics resource used internally by this object, and removes
    /// scene resources managed by this object. Commonly used during Game.UnloadContent.
    /// </summary>
    public void Unload()
    {
      Dispose();
    }
  }
}
