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
        Vector4[] vector4_0 = new Vector4[6];
        Matrix[] matrix_0 = new Matrix[6];
        Effect shadowFx;

        /// <summary>Effect used for shadow map rendering.</summary>
        public override Effect ShadowEffect => shadowFx;

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
        /// <param name="sceneState"></param>
        /// <param name="shadowGroup">Shadow group used as the source for the shadow map.</param>
        /// <param name="visibility"></param>
        /// <param name="shadowQuality">Shadow quality from 1.0 (highest) to 0.0 (lowest).</param>
        public override void Build(GraphicsDevice device, ISceneState sceneState, ShadowGroup shadowGroup, IShadowMapVisibility visibility, float shadowQuality)
        {
            base.Build(device, sceneState, shadowGroup, visibility, shadowQuality);
            if (shadowFx != null)
                return;
            shadowFx = CreateEffect();
        }

        /// <summary>Releases resources allocated by this object.</summary>
        public override void Dispose()
        {
            Disposable.Free(ref shadowFx);
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
            var vector4 = new Vector4(1f / shadowmap.Width, 1f / shadowmap.Height, 1f / shadowmap.Width, 1f / shadowmap.Height);
            for (int i = 0; i < Surfaces.Length; ++i)
            {
                Rectangle r = Surfaces[i].method_0(padding);
                vector4_0[i] = new Vector4(r.X, r.Y, r.Width, r.Height) * vector4;
            }
            return vector4_0;
        }

        /// <summary>
        /// Creates packed surface transforms used by the built-in ShadowEffect.
        /// </summary>
        /// <returns></returns>
        protected Matrix[] GetPackedSurfaceViewProjection()
        {
            for (int i = 0; i < Surfaces.Length; ++i)
                matrix_0[i] = Surfaces[i].WorldToSurfaceView * Surfaces[i].Projection;
            return matrix_0;
        }
    }
}
