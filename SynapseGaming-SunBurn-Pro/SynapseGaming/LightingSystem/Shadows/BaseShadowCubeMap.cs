// Decompiled with JetBrains decompiler
// Type: SynapseGaming.LightingSystem.Shadows.BaseShadowCubeMap
// Assembly: SynapseGaming-SunBurn-Pro, Version=1.3.2.8, Culture=neutral, PublicKeyToken=c23c60523565dbfd
// MVID: A5F03349-72AC-4BAA-AEEE-9AB9B77E0A39
// Assembly location: C:\Projects\BlackBox\StarDrive\SynapseGaming-SunBurn-Pro.dll

using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using ns3;
using EmbeddedResources;
using SynapseGaming.LightingSystem.Core;
using SynapseGaming.LightingSystem.Effects;
using SynapseGaming.LightingSystem.Lights;

namespace SynapseGaming.LightingSystem.Shadows
{
  /// <summary>
  /// Shadow map class that implements cube-mapped shadows with
  /// per surface level-of-detail. Used for point based lights.
  /// </summary>
  public abstract class BaseShadowCubeMap : BaseShadowEffectShadowMap
  {
    private static bool bool_0;
    private static Matrix[] matrix_1 = new Matrix[6];
    private static Plane[] plane_1 = new Plane[6];
    private ShadowMapSurface[] shadowMapSurface_0 = new ShadowMapSurface[6];
    private Plane[] plane_0 = new Plane[6];
    private const int int_0 = 6;
    private const int int_1 = 8;

    /// <summary>Array of the cube-map surfaces.</summary>
    public override ShadowMapSurface[] Surfaces => this.shadowMapSurface_0;

      /// <summary>
    /// Unused, this object supports render targets from the ShadowMapCache.
    /// </summary>
    public override RenderTarget CustomRenderTarget => null;

      /// <summary>Creates a new ShadowCubeMap instance.</summary>
    public BaseShadowCubeMap()
    {
      if (!bool_0)
      {
        matrix_1[0] = Matrix.CreateLookAt(Vector3.Zero, Vector3.UnitX, Vector3.UnitY);
        matrix_1[1] = Matrix.CreateLookAt(Vector3.Zero, -Vector3.UnitX, Vector3.UnitY);
        matrix_1[2] = Matrix.CreateLookAt(Vector3.Zero, Vector3.UnitY, Vector3.UnitZ);
        matrix_1[3] = Matrix.CreateLookAt(Vector3.Zero, -Vector3.UnitY, Vector3.UnitZ);
        matrix_1[4] = Matrix.CreateLookAt(Vector3.Zero, Vector3.UnitZ, Vector3.UnitY);
        matrix_1[5] = Matrix.CreateLookAt(Vector3.Zero, -Vector3.UnitZ, Vector3.UnitY);
        for (int index = 0; index < plane_1.Length; ++index)
          plane_1[index] = Plane.Transform(new Plane(0.0f, 0.0f, 1f, 1f), Matrix.Invert(matrix_1[index]));
        bool_0 = true;
      }
      for (int index = 0; index < this.shadowMapSurface_0.Length; ++index)
        this.shadowMapSurface_0[index] = new ShadowMapSurface();
      this.shadowMapSurface_0[0].WorldToSurfaceView = matrix_1[0];
      this.shadowMapSurface_0[1].WorldToSurfaceView = matrix_1[1];
      this.shadowMapSurface_0[2].WorldToSurfaceView = matrix_1[2];
      this.shadowMapSurface_0[3].WorldToSurfaceView = matrix_1[3];
      this.shadowMapSurface_0[4].WorldToSurfaceView = matrix_1[4];
      this.shadowMapSurface_0[5].WorldToSurfaceView = matrix_1[5];
      for (int index = 0; index < this.shadowMapSurface_0.Length; ++index)
        this.plane_0[index] = plane_1[index];
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
      IShadowSource shadowSource = shadowgroup.ShadowSource;
      BoundingSphere boundingSphereCentered = shadowgroup.BoundingSphereCentered;
      Vector3 shadowPosition = shadowgroup.ShadowSource.ShadowPosition;
      float radius = boundingSphereCentered.Radius;
      this.shadowMapSurface_0[0].method_1(new Vector3(-shadowPosition.Z, -shadowPosition.Y, shadowPosition.X));
      this.shadowMapSurface_0[1].method_1(new Vector3(shadowPosition.Z, -shadowPosition.Y, -shadowPosition.X));
      this.shadowMapSurface_0[2].method_1(new Vector3(-shadowPosition.X, -shadowPosition.Z, shadowPosition.Y));
      this.shadowMapSurface_0[3].method_1(new Vector3(shadowPosition.X, -shadowPosition.Z, -shadowPosition.Y));
      this.shadowMapSurface_0[4].method_1(new Vector3(shadowPosition.X, -shadowPosition.Y, shadowPosition.Z));
      this.shadowMapSurface_0[5].method_1(new Vector3(-shadowPosition.X, -shadowPosition.Y, -shadowPosition.Z));
      this.plane_0[0].D = shadowPosition.X + radius;
      this.plane_0[1].D = -shadowPosition.X + radius;
      this.plane_0[2].D = shadowPosition.Y + radius;
      this.plane_0[3].D = -shadowPosition.Y + radius;
      this.plane_0[4].D = shadowPosition.Z + radius;
      this.plane_0[5].D = -shadowPosition.Z + radius;
      Vector3 translation = this.SceneState.ViewToWorld.Translation;
      float val1 = 0.0f;
      for (int index1 = 0; index1 < this.shadowMapSurface_0.Length; ++index1)
      {
        ShadowMapSurface shadowMapSurface = this.shadowMapSurface_0[index1];
        if (!shadowMapSurface.Enabled)
        {
          shadowMapSurface.LevelOfDetail = 0.0f;
        }
        else
        {
          Plane plane1 = this.plane_0[index1];
          float num1 = plane1.DotCoordinate(translation);
          Vector3 vector3 = translation - plane1.Normal * num1;
          for (int index2 = 0; index2 < this.shadowMapSurface_0.Length; ++index2)
          {
            Plane plane2 = this.plane_0[index2];
            float num2 = plane2.DotCoordinate(vector3);
            if (num2 < 0.0)
              vector3 -= plane2.Normal * num2;
          }
          float float_3 = (vector3 - translation).Length();
          float num3 = CoreUtils.smethod_22(radius, float_3, this.SceneState.Projection);
          shadowMapSurface.LevelOfDetail = MathHelper.Clamp(num3, 0.0f, 1f);
          val1 = Math.Max(val1, shadowMapSurface.LevelOfDetail);
        }
      }
      if (!shadowSource.ShadowPerSurfaceLOD)
      {
        foreach (ShadowMapSurface shadowMapSurface in this.shadowMapSurface_0)
          shadowMapSurface.LevelOfDetail = val1;
      }
      if (this.ShadowEffect is IRenderableEffect)
        (this.ShadowEffect as IRenderableEffect).World = Matrix.Identity;
      if (!(this.ShadowEffect is IShadowGenerateEffect))
        return;
      (this.ShadowEffect as IShadowGenerateEffect).ShadowArea = shadowgroup.BoundingSphereCentered;
    }

    /// <summary>
    /// Sets the location in the shadow map render target the surface renders to.
    /// </summary>
    /// <param name="surface">Shadow map surface index.</param>
    /// <param name="location">Texel region used by the shadow map surface.</param>
    public override void SetSurfaceRenderTargetLocation(int surface, Rectangle location)
    {
      ShadowMapSurface shadowMapSurface = this.shadowMapSurface_0[surface];
      shadowMapSurface.RenderTargetLocation = location;
      float num1 = location.Width * 0.5f;
      float num2 = shadowMapSurface.method_0(8).Width * 0.5f;
      float fieldOfView = (double) num2 <= 0.0 ? MathHelper.ToRadians(90f) : (float) Math.Atan(num1 / (double) num2) * 2f;
      float farPlaneDistance = 10000f;
      if (this.ShadowGroup.ShadowSource is IPointSource)
        farPlaneDistance = this.ShadowGroup.BoundingSphereCentered.Radius;
      if (farPlaneDistance <= 0.0)
        farPlaneDistance = 1E-05f;
      float nearPlaneDistance = farPlaneDistance * 1E-05f;
      Matrix perspectiveFieldOfView = Matrix.CreatePerspectiveFieldOfView(fieldOfView, 1f, nearPlaneDistance, farPlaneDistance);
      perspectiveFieldOfView.M11 *= -1f;
      this.shadowMapSurface_0[surface].Projection = perspectiveFieldOfView;
    }

    /// <summary>
    /// Determines if the shadow map surface is visible to the provided view frustum.
    /// </summary>
    /// <param name="surface">Shadow map surface index.</param>
    /// <param name="viewfrustum"></param>
    /// <returns></returns>
    public override bool IsSurfaceVisible(int surface, BoundingFrustum viewfrustum)
    {
      return this.shadowMapSurface_0[surface].Enabled;
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
        (shadoweffect as Interface3).SetShadowMapAndType(null, Enum5.const_0);
      }
      else
      {
        Texture2D shadowmap1 = shadowmap as Texture2D;
        IRenderableEffect renderableEffect = shadoweffect as IRenderableEffect;
        Interface3 nterface3 = shadoweffect as Interface3;
        IShadowGenerateEffect shadowGenerateEffect = shadoweffect as IShadowGenerateEffect;
        nterface3.SetShadowMapAndType(shadowmap1, Enum5.const_0);
        if (renderableEffect != null)
          renderableEffect.SetViewAndProjection(this.SceneState.View, this.SceneState.ViewToWorld, this.SceneState.Projection, this.SceneState.ProjectionToView);
        if (shadowGenerateEffect != null)
        {
          shadowGenerateEffect.ShadowPrimaryBias = this.ShadowGroup.ShadowSource.ShadowPrimaryBias;
          shadowGenerateEffect.ShadowSecondaryBias = this.ShadowGroup.ShadowSource.ShadowSecondaryBias;
        }
        nterface3.ShadowArea = this.ShadowGroup.BoundingSphereCentered;
        nterface3.ShadowMapLocationAndSpan = this.GetPackedRenderTargetLocationAndSpan(shadowmap1, 8);
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
        nterface3.SetShadowMapAndType(null, Enum5.const_0);
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
