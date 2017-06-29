// Decompiled with JetBrains decompiler
// Type: SynapseGaming.LightingSystem.Shadows.BaseShadowEffectShadowMap
// Assembly: SynapseGaming-SunBurn-Pro, Version=1.3.2.8, Culture=neutral, PublicKeyToken=c23c60523565dbfd
// MVID: A5F03349-72AC-4BAA-AEEE-9AB9B77E0A39
// Assembly location: C:\Projects\BlackBox\StarDrive\SynapseGaming-SunBurn-Pro.dll

using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using ns11;
using SynapseGaming.LightingSystem.Core;

namespace SynapseGaming.LightingSystem.Shadows
{
  /// <summary>
  /// Base shadow map class that provides support for the built-in ShadowEffect.
  /// </summary>
  public abstract class BaseShadowEffectShadowMap : BaseShadowMap
  {
    private Vector4[] vector4_0 = new Vector4[6];
    private Matrix[] matrix_0 = new Matrix[6];
    private Effect effect_0;

    /// <summary>Effect used for shadow map rendering.</summary>
    public override Effect ShadowEffect => this.effect_0;

      /// <summary>
    /// Creates a new effect that performs rendering specific to the shadow
    /// mapping implementation used by this object.
    /// </summary>
    /// <returns></returns>
    protected abstract Effect CreateEffect();

    /// <summary>
    /// Builds the shadow map information based on the provided scene state and shadow
    /// group, visibility, and quality.
    /// </summary>
    /// <param name="device"></param>
    /// <param name="scenestate"></param>
    /// <param name="shadowgroup">Shadow group used as the source for the shadow map.</param>
    /// <param name="shadowvisibility"></param>
    /// <param name="shadowquality">Shadow quality from 1.0 (highest) to 0.0 (lowest).</param>
    public override void Build(GraphicsDevice device, ISceneState scenestate, ShadowGroup shadowgroup, IShadowMapVisibility shadowvisibility, float shadowquality)
    {
      base.Build(device, scenestate, shadowgroup, shadowvisibility, shadowquality);
      if (this.effect_0 != null)
        return;
      this.effect_0 = this.CreateEffect();
    }

    /// <summary>Releases resources allocated by this object.</summary>
    public override void Dispose()
    {
      Disposable.Free(ref this.effect_0);
      base.Dispose();
    }

    /// <summary>
    /// Creates packed surface information used by the built-in ShadowEffect.
    /// </summary>
    /// <param name="shadowmap"></param>
    /// <param name="padding">Width of pixel padding used to avoid edge artifacts.</param>
    /// <returns></returns>
    protected Vector4[] GetPackedRenderTargetLocationAndSpan(Texture2D shadowmap, int padding)
    {
      Vector4 vector4 = new Vector4(1f / shadowmap.Width, 1f / shadowmap.Height, 1f / shadowmap.Width, 1f / shadowmap.Height);
      for (int index = 0; index < this.Surfaces.Length; ++index)
      {
        Rectangle rectangle = this.Surfaces[index].method_0(padding);
        this.vector4_0[index] = new Vector4(rectangle.X, rectangle.Y, rectangle.Width, rectangle.Height) * vector4;
      }
      return this.vector4_0;
    }

    /// <summary>
    /// Creates packed surface transforms used by the built-in ShadowEffect.
    /// </summary>
    /// <returns></returns>
    protected Matrix[] GetPackedSurfaceViewProjection()
    {
      for (int index = 0; index < this.Surfaces.Length; ++index)
        this.matrix_0[index] = this.Surfaces[index].WorldToSurfaceView * this.Surfaces[index].Projection;
      return this.matrix_0;
    }
  }
}
