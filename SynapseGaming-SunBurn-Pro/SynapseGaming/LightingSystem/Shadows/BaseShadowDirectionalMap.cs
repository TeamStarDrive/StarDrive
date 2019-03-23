// Decompiled with JetBrains decompiler
// Type: SynapseGaming.LightingSystem.Shadows.BaseShadowDirectionalMap
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

namespace SynapseGaming.LightingSystem.Shadows
{
    /// <summary>
    /// Shadow map class that implements cascading level-of-detail
    /// directional shadows. Used for directional lights.
    /// </summary>
    public abstract class BaseShadowDirectionalMap : BaseShadowEffectShadowMap
    {
        float[] float_0 = new float[4];
        float fadeStartDist = 250f;
        float fadeEndDist = 300f;
        float casterDist = 300f;
        ShadowMapSurface[] shadowSurfaces = new ShadowMapSurface[3];
        BoundingFrustum boundingFrustum_0 = new BoundingFrustum(Matrix.Identity);
        Vector3[] vector3_0 = new Vector3[8];
        Vector4 vector4_1;

        /// <summary>Array of the level-of-detail surfaces.</summary>
        public override ShadowMapSurface[] Surfaces => shadowSurfaces;

        /// <summary>
        /// Unused, this object supports render targets from the ShadowMapCache.
        /// </summary>
        public override RenderTarget CustomRenderTarget => null;

        /// <summary>Creates a new ShadowDirectionalMap instance.</summary>
        protected BaseShadowDirectionalMap()
        {
            for (int index = 0; index < shadowSurfaces.Length; ++index)
                shadowSurfaces[index] = new ShadowMapSurface();
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
            fadeStartDist = sceneState.Environment.ShadowFadeStartDistance;
            fadeEndDist = sceneState.Environment.ShadowFadeEndDistance;
            casterDist = sceneState.Environment.ShadowCasterDistance;
            vector4_1.W = sceneState.Environment.ShadowFadeStartDistance;
            int num = Math.Min(float_0.Length - 1, visibility.ShadowLODRangeHints.Length);
            for (int i = 0; i < num; ++i)
                float_0[i + 1] = visibility.ShadowLODRangeHints[i];
            for (int index = 0; index < shadowSurfaces.Length; ++index)
                shadowSurfaces[index].LevelOfDetail = 1f;
        }

        /// <summary>
        /// Sets the location in the shadow map render target the surface renders to.
        /// </summary>
        /// <param name="surface">Shadow map surface index.</param>
        /// <param name="location">Texel region used by the shadow map surface.</param>
        public override void SetSurfaceRenderTargetLocation(int surface, Rectangle location)
        {
            IShadowSource shadowSource = ShadowGroup.ShadowSource;
            ShadowMapSurface shadowMapSurface = shadowSurfaces[surface];
            shadowMapSurface.RenderTargetLocation = location;
            float d1 = fadeEndDist * float_0[surface];
            float d2 = fadeEndDist * float_0[surface + 1];
            if (surface == 0)
                vector4_1.X = d2;
            else if (surface == 1)
                vector4_1.Y = d2;
            else
                vector4_1.Z = d2;
            boundingFrustum_0.Matrix = SceneState.Projection;
            boundingFrustum_0.GetCorners(vector3_0);
            Plane plane_0_1 = new Plane(0.0f, 0.0f, 1f, d1);
            Plane plane_0_2 = new Plane(0.0f, 0.0f, 1f, d2);
            Vector3 vector3_3 = new Vector3();
            for (int index = 0; index < 4; ++index)
            {
                Vector3 vector3_1 = vector3_0[index];
                Vector3 vector3_2 = vector3_0[index + 4];
                if (CoreUtils.smethod_10(vector3_1, vector3_2, plane_0_1, ref vector3_3))
                    vector3_0[index] = vector3_3;
                if (CoreUtils.smethod_10(vector3_1, vector3_2, plane_0_2, ref vector3_3))
                    vector3_0[index + 4] = vector3_3;
            }
            Vector3 vector3_4 = vector3_0[0];
            for (int index = 1; index < 8; ++index)
                vector3_4 += vector3_0[index];
            Vector3 position1 = vector3_4 / 8f;
            Matrix viewToWorld = SceneState.ViewToWorld;
            Vector3 vector3_5 = Vector3.Transform(position1, viewToWorld);
            float float3 = casterDist;
            Vector3 position2 = vector3_5 - shadowSource.World.Forward * float3;
            Matrix matrix1 = Matrix.Invert(Matrix.CreateTranslation(position2)) * Matrix.Invert(shadowSource.World);
            Matrix matrix2 = viewToWorld * matrix1;
            for (int index = 0; index < 8; ++index)
                vector3_0[index] = Vector3.Transform(vector3_0[index], matrix2);
            float num = Math.Max(Vector3.Distance(vector3_0[0], vector3_0[2]), Vector3.Distance(vector3_0[0], vector3_0[6]));
            CoreUtils.smethod_11(vector3_0);
            shadowMapSurface.WorldToSurfaceView = matrix1;
            shadowMapSurface.Projection = Matrix.CreateOrthographic(num, num, float3 * 0.25f, float3 * 1.75f) * Matrix.CreateScale(-1f, 1f, 1f);
            int width = location.Width;
            Vector4 vector = (Vector4.Transform(new Vector4(position2, 1f), shadowMapSurface.Frustum.Matrix) + Vector4.One) * 0.5f * new Vector4(width);
            Vector4 vector4 = (Vector4.Transform(new Vector4(Vector3.Zero, 1f), shadowMapSurface.Frustum.Matrix) + Vector4.One) * 0.5f * new Vector4(width);
            vector.X += vector4.X % 1f;
            vector.Y += vector4.Y % 1f;
            vector /= new Vector4(width);
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
                shadow.SetShadowMapAndType(shadowTex, Enum5.const_1);
                shadow.ShadowViewDistance = vector4_1;
                render?.SetViewAndProjection(SceneState.View, SceneState.ViewToWorld, SceneState.Projection, SceneState.ProjectionToView);
                if (generate != null)
                {
                    generate.ShadowPrimaryBias = ShadowGroup.ShadowSource.ShadowPrimaryBias;
                    generate.ShadowSecondaryBias = ShadowGroup.ShadowSource.ShadowSecondaryBias;
                }

                shadow.ShadowArea = ShadowGroup.BoundingSphereCentered;
                shadow.ShadowMapLocationAndSpan = GetPackedRenderTargetLocationAndSpan(shadowTex, 0);
                shadow.ShadowViewProjection = GetPackedSurfaceViewProjection();
            }
            else
            {
                shadow.SetShadowMapAndType(null, Enum5.const_1);
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
            ShadowMapSurface mapSurface = shadowSurfaces[surface];
            var render = shadowFx as IRenderableEffect;
            var shadow = shadowFx as IShadowEffect;
            var generate = shadowFx as IShadowGenerateEffect;
            shadow?.SetShadowMapAndType(null, Enum5.const_1);
            render?.SetViewAndProjection(mapSurface.WorldToSurfaceView, Matrix.Identity, mapSurface.Projection, SceneState.ProjectionToView);
            if (generate != null)
            {
                generate.ShadowPrimaryBias = ShadowGroup.ShadowSource.ShadowPrimaryBias;
                generate.ShadowSecondaryBias = ShadowGroup.ShadowSource.ShadowSecondaryBias;
                generate.ShadowArea = ShadowGroup.BoundingSphereCentered;
                generate.SetCameraView(SceneState.View, SceneState.ViewToWorld);
            }
            else if (shadow != null)
            {
                shadow.ShadowArea = ShadowGroup.BoundingSphereCentered;
            }
            Device.Viewport = shadowSurfaces[surface].Viewport;
            Device.Clear(ClearOptions.Target | ClearOptions.DepthBuffer | ClearOptions.Stencil, Color.White, 1f, 0);
        }

        /// <summary>Finalizes rendering.</summary>
        public override void EndSurfaceRendering()
        {
        }
    }
}
