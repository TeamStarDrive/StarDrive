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
        static bool bool_0;
        static Matrix[] matrix_1 = new Matrix[6];
        static Plane[] plane_1 = new Plane[6];
        ShadowMapSurface[] shadowMapSurface_0 = new ShadowMapSurface[6];
        Plane[] plane_0 = new Plane[6];

        /// <summary>Array of the cube-map surfaces.</summary>
        public override ShadowMapSurface[] Surfaces => shadowMapSurface_0;

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
            for (int index = 0; index < shadowMapSurface_0.Length; ++index)
                shadowMapSurface_0[index] = new ShadowMapSurface();
            shadowMapSurface_0[0].WorldToSurfaceView = matrix_1[0];
            shadowMapSurface_0[1].WorldToSurfaceView = matrix_1[1];
            shadowMapSurface_0[2].WorldToSurfaceView = matrix_1[2];
            shadowMapSurface_0[3].WorldToSurfaceView = matrix_1[3];
            shadowMapSurface_0[4].WorldToSurfaceView = matrix_1[4];
            shadowMapSurface_0[5].WorldToSurfaceView = matrix_1[5];
            for (int index = 0; index < shadowMapSurface_0.Length; ++index)
                plane_0[index] = plane_1[index];
        }

        /// <summary>
        /// Builds the shadow map information based on the provided scene state and shadow
        /// group, visibility, and quality.
        /// </summary>
        /// <param name="device"></param>
        /// <param name="sceneState"></param>
        /// <param name="shadowGroup">Shadow group used as the source for the shadow map.</param>
        /// <param name="visibility"></param>
        /// <param name="shadowQuality">Shadow quality from 1.0 (highest) to 0.0 (lowest).</param>
        public override void Build(GraphicsDevice device, ISceneState sceneState, ShadowGroup shadowGroup, IShadowMapVisibility visibility, float shadowQuality)
        {
            base.Build(device, sceneState, shadowGroup, visibility, shadowQuality);
            IShadowSource source = shadowGroup.ShadowSource;
            BoundingSphere boundingSphereCentered = shadowGroup.BoundingSphereCentered;
            Vector3 pos = shadowGroup.ShadowSource.ShadowPosition;
            float radius = boundingSphereCentered.Radius;
            shadowMapSurface_0[0].method_1(new Vector3(-pos.Z, -pos.Y,  pos.X));
            shadowMapSurface_0[1].method_1(new Vector3( pos.Z, -pos.Y, -pos.X));
            shadowMapSurface_0[2].method_1(new Vector3(-pos.X, -pos.Z,  pos.Y));
            shadowMapSurface_0[3].method_1(new Vector3( pos.X, -pos.Z, -pos.Y));
            shadowMapSurface_0[4].method_1(new Vector3( pos.X, -pos.Y,  pos.Z));
            shadowMapSurface_0[5].method_1(new Vector3(-pos.X, -pos.Y, -pos.Z));
            plane_0[0].D =  pos.X + radius;
            plane_0[1].D = -pos.X + radius;
            plane_0[2].D =  pos.Y + radius;
            plane_0[3].D = -pos.Y + radius;
            plane_0[4].D =  pos.Z + radius;
            plane_0[5].D = -pos.Z + radius;
            Vector3 translation = SceneState.ViewToWorld.Translation;
            float val1 = 0.0f;
            for (int index1 = 0; index1 < shadowMapSurface_0.Length; ++index1)
            {
                ShadowMapSurface shadowMapSurface = shadowMapSurface_0[index1];
                if (!shadowMapSurface.Enabled)
                {
                    shadowMapSurface.LevelOfDetail = 0.0f;
                }
                else
                {
                    Plane plane1 = plane_0[index1];
                    float num1 = plane1.DotCoordinate(translation);
                    Vector3 vector3 = translation - plane1.Normal * num1;
                    for (int index2 = 0; index2 < shadowMapSurface_0.Length; ++index2)
                    {
                        Plane plane2 = plane_0[index2];
                        float num2 = plane2.DotCoordinate(vector3);
                        if (num2 < 0.0)
                            vector3 -= plane2.Normal * num2;
                    }
                    float float_3 = (vector3 - translation).Length();
                    float num3 = CoreUtils.smethod_22(radius, float_3, SceneState.Projection);
                    shadowMapSurface.LevelOfDetail = MathHelper.Clamp(num3, 0.0f, 1f);
                    val1 = Math.Max(val1, shadowMapSurface.LevelOfDetail);
                }
            }
            if (!source.ShadowPerSurfaceLOD)
            {
                foreach (ShadowMapSurface shadowMapSurface in shadowMapSurface_0)
                    shadowMapSurface.LevelOfDetail = val1;
            }
            if (ShadowEffect is IRenderableEffect render)
                render.World = Matrix.Identity;
            if (ShadowEffect is IShadowGenerateEffect generate)
                generate.ShadowArea = shadowGroup.BoundingSphereCentered;
        }

        /// <summary>
        /// Sets the location in the shadow map render target the surface renders to.
        /// </summary>
        /// <param name="surface">Shadow map surface index.</param>
        /// <param name="location">Texel region used by the shadow map surface.</param>
        public override void SetSurfaceRenderTargetLocation(int surface, Rectangle location)
        {
            ShadowMapSurface shadowMapSurface = shadowMapSurface_0[surface];
            shadowMapSurface.RenderTargetLocation = location;
            float num1 = location.Width * 0.5f;
            float num2 = shadowMapSurface.method_0(8).Width * 0.5f;
            float fieldOfView = num2 <= 0.0 ? MathHelper.ToRadians(90f) : (float)Math.Atan(num1 / num2) * 2f;
            float farPlaneDistance = 10000f;
            if (ShadowGroup.ShadowSource is IPointSource)
                farPlaneDistance = ShadowGroup.BoundingSphereCentered.Radius;
            if (farPlaneDistance <= 0.0)
                farPlaneDistance = 1E-05f;
            float nearPlaneDistance = farPlaneDistance * 1E-05f;
            Matrix perspectiveFieldOfView = Matrix.CreatePerspectiveFieldOfView(fieldOfView, 1f, nearPlaneDistance, farPlaneDistance);
            perspectiveFieldOfView.M11 *= -1f;
            shadowMapSurface_0[surface].Projection = perspectiveFieldOfView;
        }

        /// <summary>
        /// Determines if the shadow map surface is visible to the provided view frustum.
        /// </summary>
        /// <param name="surface">Shadow map surface index.</param>
        /// <param name="viewfrustum"></param>
        /// <returns></returns>
        public override bool IsSurfaceVisible(int surface, BoundingFrustum viewfrustum)
        {
            return shadowMapSurface_0[surface].Enabled;
        }

        /// <summary>
        /// Sets up the shadow map for rendering shadows to the scene.
        /// </summary>
        /// <param name="shadowMap"></param>
        public override void BeginRendering(Texture shadowMap)
        {
            BeginRendering(shadowMap, ShadowEffect);
        }

        /// <summary>
        /// Sets up the shadow map for rendering shadows to the scene.
        /// </summary>
        /// <param name="shadowMap"></param>
        /// <param name="shadowFx">Custom shadow effect used in rendering.</param>
        public override void BeginRendering(Texture shadowMap, Effect shadowFx)
        {
            var shadow = (IShadowEffect)shadowFx;
            if (shadowMap is Texture2D shadowTex)
            {
                var render = shadowFx as IRenderableEffect;
                var generate = shadowFx as IShadowGenerateEffect;
                shadow.SetShadowMapAndType(shadowTex, Enum5.const_0);
                render?.SetViewAndProjection(SceneState.View, SceneState.ViewToWorld, SceneState.Projection, SceneState.ProjectionToView);
                if (generate != null)
                {
                    generate.ShadowPrimaryBias = ShadowGroup.ShadowSource.ShadowPrimaryBias;
                    generate.ShadowSecondaryBias = ShadowGroup.ShadowSource.ShadowSecondaryBias;
                }

                shadow.ShadowArea = ShadowGroup.BoundingSphereCentered;
                shadow.ShadowMapLocationAndSpan = GetPackedRenderTargetLocationAndSpan(shadowTex, 8);
            }
            else
            {
                shadow.SetShadowMapAndType(null, Enum5.const_0);
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
            BeginSurfaceRendering(surface, ShadowEffect);
        }

        /// <summary>
        /// Sets up the shadow map surface for generating the shadow map depth buffer.
        /// </summary>
        /// <param name="surface">Shadow map surface index.</param>
        /// <param name="shadowFx">Custom shadow effect used in rendering.</param>
        public override void BeginSurfaceRendering(int surface, Effect shadowFx)
        {
            ShadowMapSurface shadowMapSurface = shadowMapSurface_0[surface];
            var render = shadowFx as IRenderableEffect;
            var shadow = shadowFx as IShadowEffect;
            var generate = shadowFx as IShadowGenerateEffect;
            shadow?.SetShadowMapAndType(null, Enum5.const_0);
            render?.SetViewAndProjection(shadowMapSurface.WorldToSurfaceView, Matrix.Identity, shadowMapSurface.Projection, SceneState.ProjectionToView);
            if (generate != null)
            {
                generate.ShadowPrimaryBias = ShadowGroup.ShadowSource.ShadowPrimaryBias;
                generate.ShadowSecondaryBias = ShadowGroup.ShadowSource.ShadowSecondaryBias;
                generate.ShadowArea = ShadowGroup.BoundingSphereCentered;
                generate.SetCameraView(SceneState.View, SceneState.ViewToWorld);
            }
            else if (shadow != null)
                shadow.ShadowArea = ShadowGroup.BoundingSphereCentered;
            Device.Viewport = shadowMapSurface_0[surface].Viewport;
            Device.Clear(ClearOptions.Target | ClearOptions.DepthBuffer | ClearOptions.Stencil, Color.White, 1f, 0);
        }

        /// <summary>Finalizes rendering.</summary>
        public override void EndSurfaceRendering()
        {
        }
    }
}
