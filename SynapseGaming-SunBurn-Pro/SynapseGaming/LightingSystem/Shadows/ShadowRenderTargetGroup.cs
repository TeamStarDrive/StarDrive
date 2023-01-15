// Decompiled with JetBrains decompiler
// Type: SynapseGaming.LightingSystem.Shadows.ShadowRenderTargetGroup
// Assembly: SynapseGaming-SunBurn-Pro, Version=1.3.2.8, Culture=neutral, PublicKeyToken=c23c60523565dbfd
// MVID: A5F03349-72AC-4BAA-AEEE-9AB9B77E0A39
// Assembly location: C:\Projects\BlackBox\StarDrive\SynapseGaming-SunBurn-Pro.dll

using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework.Graphics;
using ns11;
using SynapseGaming.LightingSystem.Core;
#pragma warning disable CA2213

namespace SynapseGaming.LightingSystem.Shadows
{
  /// <summary>
  /// Class that manages shadow groups sharing the same render target.
  /// </summary>
  public sealed class ShadowRenderTargetGroup : SafeSingletonBeginableObject, IDisposable
  {
      private GraphicsDevice graphicsDevice_0;
    private DepthStencilBuffer depthStencilBuffer_0;
      private Viewport viewport_0;
    private Viewport viewport_1;
    private RenderTarget renderTarget_1;
    private RenderTarget renderTarget_2;
    private RenderTarget renderTarget_3;
    private RenderTarget renderTarget_4;
    private DepthStencilBuffer depthStencilBuffer_1;
    private Texture2D texture2D_1;

    /// <summary>The current RenderTarget used by this object.</summary>
    public RenderTarget RenderTarget { get; private set; }

      /// <summary>
    /// Shadow map texture generated after completing a call to Begin and End.
    /// </summary>
    public Texture2D RenderTargetTexture { get; private set; }

      /// <summary>Viewport that encapsulates the entire render target.</summary>
    public Viewport Viewport => this.viewport_0;

      /// <summary>List of shadow groups managed by this object.</summary>
    public List<ShadowGroup> ShadowGroups { get; } = new List<ShadowGroup>(16);

      /// <summary>
    /// Used to determine if the render target contents are valid or if the contents need
    /// to be re-rendered.
    /// 
    /// The default SunBurn shadow mapping implementation renders shadow map contents
    /// every frame, however custom implementations can provide static shadow maps.
    /// 
    /// Please note: if shadow maps are static and the contents are valid DO NOT call
    /// ShadowRenderTargetGroup Begin() and End().  On the Xbox this will invalidate the
    /// render target data.
    /// 
    /// However skipping calls to Begin and End require calling
    /// ShadowRenderTargetGroup.UpdateRenderTargetTexture() to ensure the shadow texture
    /// is up to date.
    /// 
    /// When using the built-in render managers this is all handled automatically.
    /// </summary>
    public bool ContentsAreValid
    {
      get
      {
        foreach (ShadowGroup shadowGroup in this.ShadowGroups)
        {
          if (shadowGroup.Shadow is IShadowMap && !(shadowGroup.Shadow as IShadowMap).ContentsAreValid)
            return false;
        }
        return true;
      }
    }

    /// <summary>Determines if the render target group uses shadows.</summary>
    /// <returns></returns>
    public bool HasShadows()
    {
      return this.RenderTarget != null;
    }

    /// <summary>
    /// Used to update the RenderTargetTexture property when skipping calls to Begin()
    /// and End(), often during custom static shadow mapping implementations.
    /// 
    /// The default SunBurn shadow mapping implementation renders shadow map contents
    /// every frame, however custom implementations can provide static shadow maps.
    /// 
    /// Please note: if shadow maps are static and the contents are valid DO NOT call
    /// Begin() and End().  On the Xbox this will invalidate the render target data.
    /// 
    /// However skipping calls to Begin() and End() require calling UpdateRenderTargetTexture()
    /// to ensure the shadow texture is up to date.
    /// 
    /// When using the built-in render managers this is all handled automatically.
    /// </summary>
    public void UpdateRenderTargetTexture()
    {
      if (this.RenderTarget == null)
        return;
      this.RenderTargetTexture = ((RenderTarget2D) this.RenderTarget).GetTexture();
    }

    /// <summary>
    /// Builds the render target group information based on the
    /// provided render target and depth buffer.
    /// </summary>
    /// <param name="device"></param>
    /// <param name="shadowmaprendertarget"></param>
    /// <param name="shadowmapdepthbuffer"></param>
    public void Build(GraphicsDevice device, RenderTarget shadowmaprendertarget, DepthStencilBuffer shadowmapdepthbuffer)
    {
      this.graphicsDevice_0 = device;
      this.RenderTarget = shadowmaprendertarget;
      this.depthStencilBuffer_0 = shadowmapdepthbuffer;
      if (shadowmaprendertarget != null)
      {
        this.viewport_0.X = 0;
        this.viewport_0.Y = 0;
        this.viewport_0.Width = shadowmaprendertarget.Width;
        this.viewport_0.Height = shadowmaprendertarget.Height;
        this.viewport_0.MinDepth = 0.0f;
        this.viewport_0.MaxDepth = 1f;
      }
      else
        this.viewport_0 = new Viewport();
    }

    /// <summary>Releases resources allocated by this object.</summary>
    public void Dispose()
    {
      this.graphicsDevice_0 = null;
      this.depthStencilBuffer_0 = null;
      this.RenderTarget = null;
      this.ShadowGroups.Clear();
      this.RenderTargetTexture = null;
      this.renderTarget_1 = null;
      this.renderTarget_2 = null;
      this.renderTarget_3 = null;
      this.renderTarget_4 = null;
      this.depthStencilBuffer_1 = null;
      Disposable.Dispose(ref this.texture2D_1);
    }

    /// <summary>
    /// Sets up the render target group for generating the shadow maps.
    /// </summary>
    public override void Begin()
    {
      base.Begin();
      if (this.RenderTarget == null)
        throw new Exception("Render target is null. This group dosn't contain shadows, begin cannot be called.");
      if (!(this.RenderTarget is RenderTarget2D))
        throw new Exception("Unsupported render target type. Must be RenderTarget2D.");
      this.viewport_1 = this.graphicsDevice_0.Viewport;
      this.renderTarget_1 = this.graphicsDevice_0.GetRenderTarget(0);
      this.renderTarget_2 = this.graphicsDevice_0.GetRenderTarget(1);
      this.renderTarget_3 = this.graphicsDevice_0.GetRenderTarget(2);
      this.renderTarget_4 = this.graphicsDevice_0.GetRenderTarget(3);
      this.depthStencilBuffer_1 = this.graphicsDevice_0.DepthStencilBuffer;
      this.graphicsDevice_0.SetRenderTarget(0, (RenderTarget2D) this.RenderTarget);
      this.graphicsDevice_0.SetRenderTarget(1, null);
      this.graphicsDevice_0.SetRenderTarget(2, null);
      this.graphicsDevice_0.SetRenderTarget(3, null);
      this.graphicsDevice_0.DepthStencilBuffer = this.depthStencilBuffer_0;
      this.graphicsDevice_0.Viewport = this.viewport_0;
    }

    /// <summary>Finalizes rendering.</summary>
    public override void End()
    {
      base.End();
      if (this.RenderTarget == null)
        return;
      this.graphicsDevice_0.SetRenderTarget(0, (RenderTarget2D) this.renderTarget_1);
      this.graphicsDevice_0.SetRenderTarget(1, (RenderTarget2D) this.renderTarget_2);
      this.graphicsDevice_0.SetRenderTarget(2, (RenderTarget2D) this.renderTarget_3);
      this.graphicsDevice_0.SetRenderTarget(3, (RenderTarget2D) this.renderTarget_4);
      this.graphicsDevice_0.DepthStencilBuffer = this.depthStencilBuffer_1;
      this.graphicsDevice_0.Viewport = this.viewport_1;
      this.RenderTargetTexture = ((RenderTarget2D) this.RenderTarget).GetTexture();
    }
  }
}
