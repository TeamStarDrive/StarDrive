// Decompiled with JetBrains decompiler
// Type: SynapseGaming.LightingSystem.Rendering.HighDynamicRangePostProcessor
// Assembly: SynapseGaming-SunBurn-Pro, Version=1.3.2.8, Culture=neutral, PublicKeyToken=c23c60523565dbfd
// MVID: A5F03349-72AC-4BAA-AEEE-9AB9B77E0A39
// Assembly location: C:\Projects\BlackBox\StarDrive\SynapseGaming-SunBurn-Pro.dll

using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework.Graphics;
using ns11;
using EmbeddedResources;
using SynapseGaming.LightingSystem.Core;

namespace SynapseGaming.LightingSystem.Rendering
{
  /// <summary>
  /// Adds high dynamic range (HDR) lighting to the scene. Includes HDR, tone mapping, and bloom.
  /// </summary>
  public class HighDynamicRangePostProcessor : BaseRenderTargetPostProcessor
  {
    private List<Class60> list_0 = new List<Class60>(16);
    private RenderTarget2D[] renderTarget2D_3 = new RenderTarget2D[2];
    private SurfaceFormat[] surfaceFormat_0 = new SurfaceFormat[3]{ SurfaceFormat.HalfVector4, SurfaceFormat.Rgba64, SurfaceFormat.Vector4 };
    private DetailPreference detailPreference_0;
    private FullFrameQuad fullFrameQuad_0;
    private FullFrameQuad fullFrameQuad_1;
    private Class44 class44_0;
    private Class40 class40_0;
    private Class39 class39_0;
    private Class43 class43_0;
    private Class41 class41_0;
    private Class42 class42_0;
    private RenderTarget2D renderTarget2D_2;
    private RenderTarget2D renderTarget2D_4;
    private RenderTarget2D renderTarget2D_5;

    /// <summary>
    /// Render target formats supported by the post processor.
    /// </summary>
    public override SurfaceFormat[] SupportedTargetFormats => this.surfaceFormat_0;

      /// <summary>
    /// Source texture formats supported by the post processor. Source textures are
    /// provided by the previous post processor in the processing chain.
    /// </summary>
    public override SurfaceFormat[] SupportedSourceFormats => this.surfaceFormat_0;

      /// <summary>Creates a HighDynamicRangePostProcessor instance.</summary>
    /// <param name="graphicsdevicemanager"></param>
    /// <param name="uselowdynamicrange">Determines if the post processor uses high range floating point render
    /// targets or faster lower range integer targets.  Xbox 360 does not support blended floating point targets
    /// and *always* uses lower range targets. When developing games for both Windows and Xbox 360 set the range
    /// to low during development to match the visuals on both platforms. It's then safe to set the range to high
    /// for released games to ensure the best visual quality on Windows.</param>
    public HighDynamicRangePostProcessor(IGraphicsDeviceService graphicsdevicemanager, bool uselowdynamicrange)
      : base(graphicsdevicemanager)
    {
      if (!uselowdynamicrange)
        return;
      this.surfaceFormat_0 = new SurfaceFormat[1]
      {
        SurfaceFormat.Color
      };
    }

    /// <summary>
    /// Use to apply user quality and performance preferences to the resources managed by this object.
    /// </summary>
    /// <param name="preferences"></param>
    public override void ApplyPreferences(ILightingSystemPreferences preferences)
    {
      this.detailPreference_0 = preferences.PostProcessingDetail;
    }

    /// <summary>
    /// Sets up the post processor and tries to find supported formats for its visual processing.
    /// </summary>
    /// <param name="availableformats">List of formats available based on support by all previous
    /// post processor in the processing chain.</param>
    /// <returns>Returns true if the post processor was properly initialized.</returns>
    public override bool Initialize(List<SurfaceFormat> availableformats)
    {
      this.Unload();
      if (!base.Initialize(availableformats))
        return false;
      GraphicsDevice graphicsDevice = this.GraphicsDeviceManager.GraphicsDevice;
      int width = this.ProcessorRenderTarget.Width;
      int height = this.ProcessorRenderTarget.Height;
      SurfaceFormat format = this.ProcessorRenderTarget.Format;
      MultiSampleType multiSampleType = this.ProcessorRenderTarget.MultiSampleType;
      int multiSampleQuality = this.ProcessorRenderTarget.MultiSampleQuality;
      this.fullFrameQuad_0 = new FullFrameQuad(graphicsDevice, width, height);
      this.class44_0 = new Class44(graphicsDevice);
      this.class40_0 = new Class40(graphicsDevice);
      this.class39_0 = new Class39(graphicsDevice);
      this.class43_0 = new Class43(graphicsDevice);
      this.class41_0 = new Class41(graphicsDevice);
      this.class42_0 = new Class42(graphicsDevice);
      this.fullFrameQuad_1 = new FullFrameQuad(graphicsDevice, 1, 1);
      this.renderTarget2D_3[0] = new RenderTarget2D(graphicsDevice, 1, 1, 1, format, multiSampleType, multiSampleQuality, LightingSystemManager.Instance.GetBestRenderTargetUsage());
      this.renderTarget2D_3[1] = new RenderTarget2D(graphicsDevice, 1, 1, 1, format, multiSampleType, multiSampleQuality, LightingSystemManager.Instance.GetBestRenderTargetUsage());
      this.renderTarget2D_4 = this.renderTarget2D_3[0];
      this.renderTarget2D_5 = this.renderTarget2D_3[1];
      while (width > 1 || height > 1)
      {
        width = Math.Max(width / 2, 1);
        height = Math.Max(height / 2, 1);
        if (width > 1 || height > 1)
          this.list_0.Add(new Class60
          {
            fullFrameQuad_0 = new FullFrameQuad(graphicsDevice, width, height),
            renderTarget2D_0 = new RenderTarget2D(graphicsDevice, width, height, 1, format, multiSampleType, multiSampleQuality, LightingSystemManager.Instance.GetBestRenderTargetUsage())
          });
        else
          break;
      }
      if (this.list_0.Count > 0)
      {
        RenderTarget2D renderTarget2D0 = this.list_0[0].renderTarget2D_0;
        this.renderTarget2D_2 = new RenderTarget2D(graphicsDevice, renderTarget2D0.Width, renderTarget2D0.Height, 1, format, multiSampleType, multiSampleQuality, LightingSystemManager.Instance.GetBestRenderTargetUsage());
      }
      RenderTarget2D renderTarget = (RenderTarget2D) graphicsDevice.GetRenderTarget(0);
      graphicsDevice.SetRenderTarget(0, this.renderTarget2D_5);
      graphicsDevice.Clear(Color.Gray);
      graphicsDevice.SetRenderTarget(0, renderTarget);
      return true;
    }

    /// <summary>
    /// Disposes any graphics resources used internally by this object.
    /// </summary>
    public override void Unload()
    {
      this.renderTarget2D_4 = null;
      this.renderTarget2D_5 = null;
      Disposable.Free(ref this.fullFrameQuad_1);
      Disposable.Free(ref this.renderTarget2D_3[0]);
      Disposable.Free(ref this.renderTarget2D_3[1]);
      Disposable.Free(ref this.renderTarget2D_2);
      Disposable.Free(ref this.fullFrameQuad_0);
      Disposable.Free(ref this.class44_0);
      Disposable.Free(ref this.class40_0);
      Disposable.Free(ref this.class39_0);
      Disposable.Free(ref this.class43_0);
      Disposable.Free(ref this.class41_0);
      Disposable.Free(ref this.class42_0);
      for (int index = 0; index < this.list_0.Count; ++index)
      {
        this.list_0[index].fullFrameQuad_0.Dispose();
        this.list_0[index].renderTarget2D_0.Dispose();
      }
      this.list_0.Clear();
      base.Unload();
    }

    /// <summary>
    /// Applies post processing effects based on the source textures.
    /// </summary>
    /// <param name="mastersource">Texture containing the original scene without any visual processing applied.</param>
    /// <param name="lastprocessorsource">Texture containing the scene with visual processing applied by each
    /// previous post processor in the processing chain.</param>
    /// <returns>Returns a texture containing the post processor's output image.</returns>
    public override Texture2D EndFrameRendering(Texture2D mastersource, Texture2D lastprocessorsource)
    {
      GraphicsDevice graphicsDevice = this.GraphicsDeviceManager.GraphicsDevice;
      if (this.list_0.Count < 1)
        return base.EndFrameRendering(mastersource, lastprocessorsource);
      if (this.renderTarget2D_5 == this.renderTarget2D_4)
        throw new Exception("Intensity render targets reference the same object.");
      Texture2D texture = this.renderTarget2D_5.GetTexture();
      graphicsDevice.RenderState.CullMode = CullMode.None;
      graphicsDevice.RenderState.DepthBufferEnable = false;
      graphicsDevice.RenderState.DepthBufferWriteEnable = false;
      graphicsDevice.RenderState.AlphaBlendEnable = false;
      graphicsDevice.RenderState.SourceBlend = Blend.One;
      graphicsDevice.RenderState.DestinationBlend = Blend.Zero;
      this.class43_0.IntensityTexture = texture;
      this.class43_0.BloomThreshold = this.SceneState.Environment.BloomThreshold;
      this.class43_0.ExposureAmount = this.SceneState.Environment.ExposureAmount;
      this.class43_0.TransitionMaxScale = this.SceneState.Environment.DynamicRangeTransitionMaxScale;
      this.class43_0.TransitionMinScale = this.SceneState.Environment.DynamicRangeTransitionMinScale;
      Texture2D texture2D1 = null;
      RenderTarget2D renderTarget2D = this.ProcessorRenderTarget;
      for (int index = 0; index < this.list_0.Count; ++index)
      {
        Class60 class60 = this.list_0[index];
        graphicsDevice.SetRenderTarget(0, class60.renderTarget2D_0);
        Class39 class39 = index != 0 ? (index != 1 ? this.class39_0 : this.class41_0) : this.class43_0;
        if (texture2D1 == null)
        {
          texture2D1 = renderTarget2D.GetTexture();
          class39.SourceTexture = texture2D1;
        }
        else
          class39.SourceTexture = renderTarget2D.GetTexture();
        class60.fullFrameQuad_0.Render(class39);
        graphicsDevice.SetRenderTarget(0, null);
        renderTarget2D = this.list_0[index].renderTarget2D_0;
      }
      Texture2D texture2D2 = null;
      if (this.detailPreference_0 != DetailPreference.Off)
      {
        Class60 class60 = this.list_0[0];
        graphicsDevice.SetRenderTarget(0, this.renderTarget2D_2);
        this.class40_0.BloomDetail = this.detailPreference_0;
        this.class40_0.SourceTexture = class60.renderTarget2D_0.GetTexture();
        this.class40_0.BlurKernel = Class40.interface4_0;
        class60.fullFrameQuad_0.Render(this.class40_0);
        graphicsDevice.SetRenderTarget(0, class60.renderTarget2D_0);
        this.class40_0.SourceTexture = this.renderTarget2D_2.GetTexture();
        this.class40_0.BlurKernel = Class40.interface4_1;
        class60.fullFrameQuad_0.Render(this.class40_0);
        graphicsDevice.SetRenderTarget(0, this.renderTarget2D_4);
        texture2D2 = class60.renderTarget2D_0.GetTexture();
      }
      else
        graphicsDevice.SetRenderTarget(0, this.renderTarget2D_4);
      this.class42_0.IntensityBlend = SceneState.ElapsedTime / SceneState.Environment.DynamicRangeTransitionTime;
      this.class42_0.IntensityTexture = texture;
      this.class42_0.SourceTexture = renderTarget2D.GetTexture();
      this.fullFrameQuad_1.Render(this.class42_0);
      graphicsDevice.SetRenderTarget(0, this.PreviousRenderTarget);
      graphicsDevice.Viewport = this.PreviousViewport;
      this.class44_0.BloomAmount = this.SceneState.Environment.BloomAmount;
      this.class44_0.SourceTexture = texture2D1;
      this.class44_0.IntensityTexture = texture;
      this.class44_0.BloomTexture = texture2D2;
      this.class44_0.ExposureAmount = this.SceneState.Environment.ExposureAmount;
      this.class44_0.TransitionMaxScale = this.SceneState.Environment.DynamicRangeTransitionMaxScale;
      this.class44_0.TransitionMinScale = this.SceneState.Environment.DynamicRangeTransitionMinScale;
      this.fullFrameQuad_0.Render(this.class44_0);
      RenderTarget2D renderTarget2D4 = this.renderTarget2D_4;
      this.renderTarget2D_4 = this.renderTarget2D_5;
      this.renderTarget2D_5 = renderTarget2D4;
      graphicsDevice.RenderState.CullMode = CullMode.CullCounterClockwiseFace;
      graphicsDevice.RenderState.DepthBufferEnable = true;
      graphicsDevice.RenderState.DepthBufferWriteEnable = true;
      return texture2D1;
    }

    private class Class60
    {
      public FullFrameQuad fullFrameQuad_0;
      public RenderTarget2D renderTarget2D_0;
    }
  }
}
