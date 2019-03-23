// Decompiled with JetBrains decompiler
// Type: SynapseGaming.LightingSystem.Effects.Deferred.DeferredSasEffect
// Assembly: SynapseGaming-SunBurn-Pro, Version=1.3.2.8, Culture=neutral, PublicKeyToken=c23c60523565dbfd
// MVID: A5F03349-72AC-4BAA-AEEE-9AB9B77E0A39
// Assembly location: C:\Projects\BlackBox\StarDrive\SynapseGaming-SunBurn-Pro.dll

using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using SynapseGaming.LightingSystem.Core;
using SynapseGaming.LightingSystem.Lights;

namespace SynapseGaming.LightingSystem.Effects.Deferred
{
    /// <summary>
    /// Provides custom deferred object rendering from user FX shaders, with full support for, and binding of, FX Standard
    /// Annotations and Semantics (SAS). Potentially supplying depth, diffuse, normal, specular,
    /// fresnel, and emissive information to the deferred buffers for later lighting, shadow, and
    /// material calculation.
    /// </summary>
    public class DeferredSasEffect : BaseSasEffect, IShadowGenerateEffect, IDeferredObjectEffect
    {
        DeferredEffectOutput deferredEffectOutput = DeferredEffectOutput.GBuffer;
        bool bool_5;
        float FogStart;
        float FogEnd;
        Vector3 fogColor;
        float float_3;
        Vector4 ShadowInfo;
        Vector4 RenderInfo;
        Vector4 vector4_2;
        Vector2 vector2_0;
        Texture2D texture2D_0;
        Texture2D texture2D_1;
        EffectParameter FxFogInfo;
        EffectParameter FxFogColor;
        EffectParameter FxFarClipDist;
        EffectParameter FxShadowInfo;
        EffectParameter FxRenderInfo;
        EffectParameter FxShadowSource;
        EffectParameter FxTargetInfo;
        EffectParameter FxDiffuseMap;
        EffectParameter FxSpecularMap;

        /// <summary>Main property used to eliminate shadow artifacts.</summary>
        public float ShadowPrimaryBias
        {
            get => ShadowInfo.X;
            set => EffectHelper.Update(new Vector4(value, ShadowInfo.Y, ShadowInfo.Z, ShadowInfo.W), ref ShadowInfo, ref FxShadowInfo);
        }

        /// <summary>
        /// Additional fine-tuned property used to eliminate shadow artifacts.
        /// </summary>
        public float ShadowSecondaryBias
        {
            get => ShadowInfo.Y;
            set => EffectHelper.Update(new Vector4(ShadowInfo.X, value, ShadowInfo.Z, ShadowInfo.W), ref ShadowInfo, ref FxShadowInfo);
        }

        /// <summary>
        /// Bounding area of the shadow source, where the bounds center is the actual shadow source location,
        /// and the radius is either the source radius (for point sources) or the maximum view based casting
        /// distance (for directional sources).
        /// </summary>
        public BoundingSphere ShadowArea
        {
            set => EffectHelper.Update(new Vector4(value.Center, value.Radius), ref vector4_2, ref FxShadowSource);
        }

        /// <summary>
        /// Determines if the effect is capable of generating shadow maps. Objects using effects unable to
        /// generate shadow maps automatically use the built-in shadow effect, however this puts
        /// heavy restrictions on how the effects handle rendering (only basic vertex transforms are supported).
        /// </summary>
        public bool SupportsShadowGeneration
        {
            get
            {
                if (!Properties.ContainsKey("ShadowGenerationTechnique"))
                    return false;
                return Techniques[(string)Properties["ShadowGenerationTechnique"]] != null;
            }
        }

        /// <summary>
        /// Determines the type of shader output for the effects to generate.
        /// </summary>
        public DeferredEffectOutput DeferredEffectOutput
        {
            get => deferredEffectOutput;
            set
            {
                deferredEffectOutput = value;
                SetTechnique();
            }
        }

        /// <summary>
        /// Texture containing the screen-space lighting generated during deferred rendering (used during the Final rendering pass).
        /// </summary>
        public Texture2D SceneLightingDiffuseMap
        {
            get => texture2D_0;
            set
            {
                EffectHelper.Update(value, ref texture2D_0, FxDiffuseMap);
                if (FxTargetInfo == null || texture2D_0 == null)
                    return;
                EffectHelper.Update(new Vector2(texture2D_0.Width, texture2D_0.Height), ref vector2_0, ref FxTargetInfo);
            }
        }

        /// <summary>
        /// Texture containing the screen-space specular generated during deferred rendering (used during the Final rendering pass).
        /// </summary>
        public Texture2D SceneLightingSpecularMap
        {
            get => texture2D_1;
            set => EffectHelper.Update(value, ref texture2D_1, FxSpecularMap);
        }

        /// <summary>Enables scene fog.</summary>
        public bool FogEnabled
        {
            get => bool_5;
            set
            {
                bool_5 = value;
                UpdateFogRange(float.MaxValue, float.MaxValue);
            }
        }

        /// <summary>
        /// Distance from the camera in world space that fog begins.
        /// </summary>
        public float FogStartDistance
        {
            get => FogStart;
            set => UpdateFogRange(value, FogEnd);
        }

        /// <summary>
        /// Distance from the camera in world space that fog ends.
        /// </summary>
        public float FogEndDistance
        {
            get => FogEnd;
            set => UpdateFogRange(FogStart, value);
        }

        /// <summary>Color applied to scene fog.</summary>
        public Vector3 FogColor
        {
            get => fogColor;
            set => EffectHelper.Update(value, ref fogColor, ref FxFogColor);
        }

        /// <summary>
        /// Creates a new DeferredSasEffect instance from an effect containing an SAS shader
        /// (often loaded through the content pipeline or from disk).
        /// </summary>
        /// <param name="device"></param>
        /// <param name="effect">Source effect containing an SAS shader.</param>
        public DeferredSasEffect(GraphicsDevice device, Effect effect) : base(device, effect)
        {
            method_9();
        }

        /// <summary>
        /// Sets scene ambient lighting (used during the Final rendering pass).
        /// </summary>
        public void SetAmbientLighting(IAmbientSource light, Vector3 directionHint)
        {
            if (light is ILight iLight)
            {
                var colorAndIntensity = new Vector4(iLight.CompositeColorAndIntensity, 1f);
                EffectHelper.Update(SasAutoBindTable.method_1(SASAddress_AmbientLight_Color[0]), colorAndIntensity);
            }
        }

        /// <summary>
        /// Sets the camera view and inverse camera view matrices. These are the matrices used in the final
        /// on-screen render from the camera / player point of view.
        /// 
        /// These matrices will differ from the standard view and inverse view matrices when rendering from
        /// an alternate point of view (for instance during shadow map and cube map generation).
        /// </summary>
        /// <param name="view">Camera view matrix applied to geometry using this effect.</param>
        /// <param name="viewToWorld">Camera inverse view matrix applied to geometry using this effect.</param>
        public void SetCameraView(in Matrix view, in Matrix viewToWorld)
        {
        }

        void method_9()
        {
            FxFarClipDist = FindBySemantic("FARCLIPDIST");
            FxShadowInfo = FindBySemantic("SHADOWINFO");
            FxShadowSource = FindBySemantic("SHADOWSOURCE");
            FxTargetInfo = FindBySemantic("TARGETINFO");
            FxRenderInfo = FindBySemantic("RENDERINFO");
            FxFogInfo = FindBySemantic("FOGINFO");
            FxFogColor = FindBySemantic("FOGCOLOR");
            FxDiffuseMap = FindBySemantic("SCENELIGHTINGDIFFUSEMAP");
            FxSpecularMap = FindBySemantic("SCENELIGHTINGSPECULARMAP");
            FogEnabled = false;
        }

        /// <summary>
        /// Creates a new empty effect of the same class type and using the same effect file as this object.
        /// </summary>
        /// <param name="device"></param>
        /// <returns></returns>
        protected override Effect Create(GraphicsDevice device)
        {
            return new DeferredSasEffect(device, this);
        }

        /// <summary>
        /// Sets the effect technique based on its current property values.
        /// </summary>
        protected override void SetTechnique()
        {
            ++class47_0.lightingSystemStatistic_0.AccumulationValue;
            EffectTechnique technique = null;
            switch (deferredEffectOutput)
            {
                case DeferredEffectOutput.Depth:
                    if (Properties.ContainsKey("DepthTechnique"))
                        technique = Techniques[(string)Properties["DepthTechnique"]];
                    break;
                case DeferredEffectOutput.GBuffer:
                    if (Properties.ContainsKey("GBufferTechnique"))
                        technique = Techniques[(string)Properties["GBufferTechnique"]];
                    break;
                case DeferredEffectOutput.ShadowDepth:
                    if (Properties.ContainsKey("ShadowGenerationTechnique"))
                        technique = Techniques[(string)Properties["ShadowGenerationTechnique"]];
                    break;
                case DeferredEffectOutput.Final:
                    if (Properties.ContainsKey("FinalTechnique"))
                        technique = Techniques[(string)Properties["FinalTechnique"]];
                    break;
            }
            if (technique != null)
                CurrentTechnique = technique;
        }

        void UpdateFogRange(float start, float end)
        {
            if (FxFogInfo != null && (FogStart != start || FogEnd != end))
            {
                FogStart = Math.Max(start, 0.0f);
                FogEnd = Math.Max(FogStart * 1.01f, end);
                float distance = FogEnd - FogStart;
                if (distance != 0f)
                    distance = 1f / distance;
                FxFogInfo.SetValue(new Vector2(FogStart, distance));
            }
        }

        /// <summary>
        /// Applies the current transform information to the bound effect parameters.
        /// </summary>
        protected override void SyncTransformEffectData()
        {
            base.SyncTransformEffectData();
            if (FxFarClipDist != null)
            {
                Vector4 vector4 = Vector4.Transform(new Vector4(0.0f, 0.0f, 1f, 1f), ProjectionToView);
                float num = 0.0f;
                if (vector4.W != 0.0)
                    num = Math.Abs(vector4.Z / vector4.W);
                if (float_3 != (double) num)
                {
                    float_3 = num;
                    FxFarClipDist.SetValue(num);
                }
            }
        }

        /// <summary>
        /// Applies the object's transparency information to its effect parameters.
        /// </summary>
        protected override void SyncTransparency()
        {
            Vector4 shadowInfo = ShadowInfo;
            Vector4 renderInfo = RenderInfo;
            if (TransparencyMode == TransparencyMode.Clip)
            {
                shadowInfo.Z = Transparency;
                renderInfo.X = Transparency;
            }
            else
            {
                shadowInfo.Z = 0.0f;
                renderInfo.X = 0.0f;
            }
            EffectHelper.Update(shadowInfo, ref ShadowInfo, ref FxShadowInfo);
            EffectHelper.Update(renderInfo, ref RenderInfo, ref FxRenderInfo);
        }
    }
}
