// Decompiled with JetBrains decompiler
// Type: SynapseGaming.LightingSystem.Shadows.BaseShadowDirectionalMap
// Assembly: SynapseGaming-SunBurn-Pro, Version=1.3.2.8, Culture=neutral, PublicKeyToken=c23c60523565dbfd
// MVID: A5F03349-72AC-4BAA-AEEE-9AB9B77E0A39
// Assembly location: C:\Projects\BlackBox\StarDrive\SynapseGaming-SunBurn-Pro.dll

using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using ns3;
using ns6;
using SynapseGaming.LightingSystem.Core;
using SynapseGaming.LightingSystem.Effects;
using System;

namespace SynapseGaming.LightingSystem.Shadows
{
  /// <summary>
  /// Shadow map class that implements cascading level-of-detail
  /// directional shadows. Used for directional lights.
  /// </summary>
  public abstract class BaseShadowDirectionalMap : BaseShadowEffectShadowMap
  {
    private float[] float_0 = new float[4];
    private float float_1 = 250f;
    private float float_2 = 300f;
    private float float_3 = 300f;
    private ShadowMapSurface[] shadowMapSurface_0 = new ShadowMapSurface[3];
    private BoundingFrustum boundingFrustum_0 = new BoundingFrustum(Matrix.Identity);
    private Vector3[] vector3_0 = new Vector3[8];
    private const int int_0 = 3;
    private Vector4 vector4_1;

    /// <summary>Array of the level-of-detail surfaces.</summary>
    public override ShadowMapSurface[] Surfaces
    {
      get
      {
        return this.shadowMapSurface_0;
      }
    }

    /// <summary>
    /// Unused, this object supports render targets from the ShadowMapCache.
    /// </summary>
    public override RenderTarget CustomRenderTarget
    {
      get
      {
        return (RenderTarget) null;
      }
    }

    /// <summary>Creates a new ShadowDirectionalMap instance.</summary>
    public BaseShadowDirectionalMap()
    {
      for (int index = 0; index < this.shadowMapSurface_0.Length; ++index)
        this.shadowMapSurface_0[index] = new ShadowMapSurface();
    }

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
      this.float_1 = scenestate.Environment.ShadowFadeStartDistance;
      this.float_2 = scenestate.Environment.ShadowFadeEndDistance;
      this.float_3 = scenestate.Environment.ShadowCasterDistance;
      this.vector4_1.W = scenestate.Environment.ShadowFadeStartDistance;
      int num = Math.Min(this.float_0.Length - 1, shadowvisibility.ShadowLODRangeHints.Length);
      for (int index = 0; index < num; ++index)
        this.float_0[index + 1] = shadowvisibility.ShadowLODRangeHints[index];
      for (int index = 0; index < this.shadowMapSurface_0.Length; ++index)
        this.shadowMapSurface_0[index].LevelOfDetail = 1f;
    }

    private BoundingBox method_0(Vector3[] vector3_1, Matrix matrix_1)
    {
      if (vector3_1.Length < 1)
        return new BoundingBox();
      Vector3 vector3_2 = Vector3.Transform(vector3_1[0], matrix_1);
      BoundingBox boundingBox = new BoundingBox(vector3_2, vector3_2);
      for (int index = 1; index < vector3_1.Length; ++index)
      {
        Vector3 vector3_3 = Vector3.Transform(vector3_1[index], matrix_1);
        boundingBox.Min = Vector3.Min(boundingBox.Min, vector3_3);
        boundingBox.Max = Vector3.Max(boundingBox.Max, vector3_3);
      }
      return boundingBox;
    }

    /// <summary>
    /// Sets the location in the shadow map render target the surface renders to.
    /// </summary>
    /// <param name="surface">Shadow map surface index.</param>
    /// <param name="location">Texel region used by the shadow map surface.</param>
    public override void SetSurfaceRenderTargetLocation(int surface, Rectangle location)
    {
      IShadowSource shadowSource = this.ShadowGroup.ShadowSource;
      ShadowMapSurface shadowMapSurface = this.shadowMapSurface_0[surface];
      shadowMapSurface.RenderTargetLocation = location;
      float d1 = this.float_2 * this.float_0[surface];
      float d2 = this.float_2 * this.float_0[surface + 1];
      if (surface == 0)
        this.vector4_1.X = d2;
      else if (surface == 1)
        this.vector4_1.Y = d2;
      else
        this.vector4_1.Z = d2;
      this.boundingFrustum_0.Matrix = this.SceneState.Projection;
      this.boundingFrustum_0.GetCorners(this.vector3_0);
      Plane plane_0_1 = new Plane(0.0f, 0.0f, 1f, d1);
      Plane plane_0_2 = new Plane(0.0f, 0.0f, 1f, d2);
      Vector3 vector3_3 = new Vector3();
      for (int index = 0; index < 4; ++index)
      {
        Vector3 vector3_1 = this.vector3_0[index];
        Vector3 vector3_2 = this.vector3_0[index + 4];
        if (Class13.smethod_10(vector3_1, vector3_2, plane_0_1, ref vector3_3))
          this.vector3_0[index] = vector3_3;
        if (Class13.smethod_10(vector3_1, vector3_2, plane_0_2, ref vector3_3))
          this.vector3_0[index + 4] = vector3_3;
      }
      Vector3 vector3_4 = this.vector3_0[0];
      for (int index = 1; index < 8; ++index)
        vector3_4 += this.vector3_0[index];
      Vector3 position1 = vector3_4 / 8f;
      Matrix viewToWorld = this.SceneState.ViewToWorld;
      Vector3 vector3_5 = Vector3.Transform(position1, viewToWorld);
      float float3 = this.float_3;
      Vector3 position2 = vector3_5 - shadowSource.World.Forward * float3;
      Matrix matrix1 = Matrix.Invert(Matrix.CreateTranslation(position2)) * Matrix.Invert(shadowSource.World);
      Matrix matrix2 = viewToWorld * matrix1;
      for (int index = 0; index < 8; ++index)
        this.vector3_0[index] = Vector3.Transform(this.vector3_0[index], matrix2);
      float num = Math.Max(Vector3.Distance(this.vector3_0[0], this.vector3_0[2]), Vector3.Distance(this.vector3_0[0], this.vector3_0[6]));
      Class13.smethod_11(this.vector3_0);
      shadowMapSurface.WorldToSurfaceView = matrix1;
      shadowMapSurface.Projection = Matrix.CreateOrthographic(num, num, float3 * 0.25f, float3 * 1.75f) * Matrix.CreateScale(-1f, 1f, 1f);
      int width = location.Width;
      Vector4 vector = (Vector4.Transform(new Vector4(position2, 1f), shadowMapSurface.Frustum.Matrix) + Vector4.One) * 0.5f * new Vector4((float) width);
      Vector4 vector4 = (Vector4.Transform(new Vector4(Vector3.Zero, 1f), shadowMapSurface.Frustum.Matrix) + Vector4.One) * 0.5f * new Vector4((float) width);
      vector.X += vector4.X % 1f;
      vector.Y += vector4.Y % 1f;
      vector /= new Vector4((float) width);
      vector = vector * 2f - Vector4.One;
      vector = Vector4.Transform(vector, Matrix.Invert(shadowMapSurface.Frustum.Matrix));
      Matrix matrix3 = Matrix.Invert(matrix1);
      matrix3.Translation = new Vector3(vector.X, vector.Y, vector.Z);
      shadowMapSurface.WorldToSurfaceView = Matrix.Invert(matrix3);
    }

    /// <summary>
    /// Determines if the shadow map surface is visible to the provided view frustum.
    /// </summary>
    /// <param name="surface">Shadow map surface index.</param>
    /// <param name="viewfrustum"></param>
    /// <returns></returns>
    public override bool IsSurfaceVisible(int surface, BoundingFrustum viewfrustum)
    {
      return true;
    }

    private int method_1(float float_4, int int_1, int int_2)
    {
      return (int) MathHelper.Clamp((float) ((double) float_4 * 0.5 + 0.5) * (float) int_1 + (float) int_2, 0.0f, (float) int_1);
    }

    /// <summary>
    /// Sets up the shadow map for rendering shadows to the scene.
    /// </summary>
    /// <param name="shadowmap"></param>
    public override void BeginRendering(Texture shadowmap)
    {
      this.BeginRendering(shadowmap, this.ShadowEffect);
    }

    /// <summary>
    /// Sets up the shadow map for rendering shadows to the scene.
    /// </summary>
    /// <param name="shadowmap"></param>
    /// <param name="shadoweffect">Custom shadow effect used in rendering.</param>
    public override void BeginRendering(Texture shadowmap, Effect shadoweffect)
    {
      if (!(shadowmap is Texture2D))
      {
        (shadoweffect as Interface3).SetShadowMapAndType((Texture2D) null, Enum5.const_1);
      }
      else
      {
        Texture2D shadowmap1 = shadowmap as Texture2D;
        IRenderableEffect renderableEffect = shadoweffect as IRenderableEffect;
        Interface3 nterface3 = shadoweffect as Interface3;
        IShadowGenerateEffect shadowGenerateEffect = shadoweffect as IShadowGenerateEffect;
        nterface3.SetShadowMapAndType(shadowmap1, Enum5.const_1);
        nterface3.ShadowViewDistance = this.vector4_1;
        if (renderableEffect != null)
          renderableEffect.SetViewAndProjection(this.SceneState.View, this.SceneState.ViewToWorld, this.SceneState.Projection, this.SceneState.ProjectionToView);
        if (shadowGenerateEffect != null)
        {
          shadowGenerateEffect.ShadowPrimaryBias = this.ShadowGroup.ShadowSource.ShadowPrimaryBias;
          shadowGenerateEffect.ShadowSecondaryBias = this.ShadowGroup.ShadowSource.ShadowSecondaryBias;
        }
        nterface3.ShadowArea = this.ShadowGroup.BoundingSphereCentered;
        nterface3.ShadowMapLocationAndSpan = this.GetPackedRenderTargetLocationAndSpan(shadowmap1, 0);
        nterface3.ShadowViewProjection = this.GetPackedSurfaceViewProjection();
      }
    }

    /// <summary>Finalizes rendering.</summary>
    public override void EndRendering()
    {
    }

    /// <summary>
    /// Sets up the shadow map surface for generating the shadow map depth buffer.
    /// </summary>
    /// <param name="surface">Shadow map surface index.</param>
    public override void BeginSurfaceRendering(int surface)
    {
      this.BeginSurfaceRendering(surface, this.ShadowEffect);
    }

    /// <summary>
    /// Sets up the shadow map surface for generating the shadow map depth buffer.
    /// </summary>
    /// <param name="surface">Shadow map surface index.</param>
    /// <param name="shadoweffect">Custom shadow effect used in rendering.</param>
    public override void BeginSurfaceRendering(int surface, Effect shadoweffect)
    {
      ShadowMapSurface shadowMapSurface = this.shadowMapSurface_0[surface];
      IRenderableEffect renderableEffect = shadoweffect as IRenderableEffect;
      Interface3 nterface3 = shadoweffect as Interface3;
      IShadowGenerateEffect shadowGenerateEffect = shadoweffect as IShadowGenerateEffect;
      if (nterface3 != null)
        nterface3.SetShadowMapAndType((Texture2D) null, Enum5.const_1);
      if (renderableEffect != null)
        renderableEffect.SetViewAndProjection(shadowMapSurface.WorldToSurfaceView, Matrix.Identity, shadowMapSurface.Projection, this.SceneState.ProjectionToView);
      if (shadowGenerateEffect != null)
      {
        shadowGenerateEffect.ShadowPrimaryBias = this.ShadowGroup.ShadowSource.ShadowPrimaryBias;
        shadowGenerateEffect.ShadowSecondaryBias = this.ShadowGroup.ShadowSource.ShadowSecondaryBias;
        shadowGenerateEffect.ShadowArea = this.ShadowGroup.BoundingSphereCentered;
        shadowGenerateEffect.SetCameraView(this.SceneState.View, this.SceneState.ViewToWorld);
      }
      else if (nterface3 != null)
        nterface3.ShadowArea = this.ShadowGroup.BoundingSphereCentered;
      this.Device.Viewport = this.shadowMapSurface_0[surface].Viewport;
      this.Device.Clear(ClearOptions.Target | ClearOptions.DepthBuffer | ClearOptions.Stencil, Color.White, 1f, 0);
    }

    /// <summary>Finalizes rendering.</summary>
    public override void EndSurfaceRendering()
    {
    }
  }
}
