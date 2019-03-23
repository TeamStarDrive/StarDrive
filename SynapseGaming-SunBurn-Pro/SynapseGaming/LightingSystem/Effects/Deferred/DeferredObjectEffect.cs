// Decompiled with JetBrains decompiler
// Type: SynapseGaming.LightingSystem.Effects.Deferred.DeferredObjectEffect
// Assembly: SynapseGaming-SunBurn-Pro, Version=1.3.2.8, Culture=neutral, PublicKeyToken=c23c60523565dbfd
// MVID: A5F03349-72AC-4BAA-AEEE-9AB9B77E0A39
// Assembly location: C:\Projects\BlackBox\StarDrive\SynapseGaming-SunBurn-Pro.dll

using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using ns3;
using EmbeddedResources;
using SynapseGaming.LightingSystem.Core;
using SynapseGaming.LightingSystem.Lights;

namespace SynapseGaming.LightingSystem.Effects.Deferred
{
    /// <summary>
    /// Provides SunBurn's built-in deferred object rendering. Supplies depth, diffuse,
    /// normal, specular, fresnel, and emissive information to the deferred buffers for
    /// later lighting, shadow, and material calculation.
    /// </summary>
    public class DeferredObjectEffect : BaseMaterialEffect, IShadowGenerateEffect, IDeferredObjectEffect
    {
        Vector3[] vector3_2 = new Vector3[3];
        Vector4 vector4_1;
        Vector4 vector4_2;
        Vector4 vector4_3;
        DeferredEffectOutput deferredEffectOutput_0;
        Texture2D texture2D_3;
        Texture2D texture2D_4;
        Vector2 vector2_0;
        bool bool_5;
        float float_7;
        float float_8;
        Vector3 vector3_0;
        Vector3 vector3_1;
        EffectParameter FxAmbientColor;
        EffectParameter FxAmbientDirection;
        EffectParameter FxTargetWidthHeight;
        EffectParameter FxTransClip;
        EffectParameter FxOffsetBiasDepthBiasTransClip;
        EffectParameter FxDirectionOrPosAndRadius;
        EffectParameter FxDiffuseMap;
        EffectParameter FxSpecularMap;
        EffectParameter FxFogStartDistEndDist;
        EffectParameter FxFogColor;

        /// <summary>Main property used to eliminate shadow artifacts.</summary>
        public float ShadowPrimaryBias
        {
            get => vector4_2.X;
            set => EffectHelper.Update(new Vector4(value, vector4_2.Y, vector4_2.Z, vector4_2.W), ref vector4_2, ref FxOffsetBiasDepthBiasTransClip);
        }

        /// <summary>
        /// Additional fine-tuned property used to eliminate shadow artifacts.
        /// </summary>
        public float ShadowSecondaryBias
        {
            get => vector4_2.Y;
            set => EffectHelper.Update(new Vector4(vector4_2.X, value, vector4_2.Z, vector4_2.W), ref vector4_2, ref FxOffsetBiasDepthBiasTransClip);
        }

        /// <summary>
        /// Bounding area of the shadow source, where the bounds center is the actual shadow source location,
        /// and the radius is either the source radius (for point sources) or the maximum view based casting
        /// distance (for directional sources).
        /// </summary>
        public BoundingSphere ShadowArea
        {
            set => EffectHelper.Update(new Vector4(value.Center, value.Radius), ref vector4_3, ref FxDirectionOrPosAndRadius);
        }

        /// <summary>
        /// Determines the type of shader output for the effects to generate.
        /// </summary>
        public DeferredEffectOutput DeferredEffectOutput
        {
            get => deferredEffectOutput_0;
            set
            {
                if (value == deferredEffectOutput_0)
                    return;
                deferredEffectOutput_0 = value;
                SetTechnique();
            }
        }

        /// <summary>
        /// Texture containing the screen-space lighting generated during deferred rendering (used during the Final rendering pass).
        /// </summary>
        public Texture2D SceneLightingDiffuseMap
        {
            get => texture2D_3;
            set
            {
                EffectHelper.Update(value, ref texture2D_3, FxDiffuseMap);
                if (FxTargetWidthHeight == null || texture2D_3 == null)
                    return;
                EffectHelper.Update(new Vector2(texture2D_3.Width, texture2D_3.Height), ref vector2_0, ref FxTargetWidthHeight);
            }
        }

        /// <summary>
        /// Texture containing the screen-space specular generated during deferred rendering (used during the Final rendering pass).
        /// </summary>
        public Texture2D SceneLightingSpecularMap
        {
            get => texture2D_4;
            set => EffectHelper.Update(value, ref texture2D_4, FxSpecularMap);
        }

        /// <summary>
        /// Determines if the effect is capable of generating shadow maps. Objects using effects unable to
        /// generate shadow maps automatically use the built-in shadow effect, however this puts
        /// heavy restrictions on how the effects handle rendering (only basic vertex transforms are supported).
        /// </summary>
        public bool SupportsShadowGeneration => false;

        /// <summary>Enables scene fog.</summary>
        public bool FogEnabled
        {
            get => bool_5;
            set
            {
                bool_5 = value;
                SetTechnique();
            }
        }

        /// <summary>
        /// Distance from the camera in world space that fog begins.
        /// </summary>
        public float FogStartDistance
        {
            get => float_7;
            set => method_6(value, float_8);
        }

        /// <summary>
        /// Distance from the camera in world space that fog ends.
        /// </summary>
        public float FogEndDistance
        {
            get => float_8;
            set => method_6(float_7, value);
        }

        /// <summary>Color applied to scene fog.</summary>
        public Vector3 FogColor
        {
            get => vector3_0;
            set => EffectHelper.Update(value, ref vector3_0, ref FxFogColor);
        }

        /// <summary>Creates a new DeferredObjectEffect instance.</summary>
        /// <param name="device"></param>
        public DeferredObjectEffect(GraphicsDevice device) : base(device, "DeferredObjectEffect")
        {
            LoadParameters(device);
        }

        internal DeferredObjectEffect(GraphicsDevice device, bool bool_6) : base(device, "DeferredObjectEffect", bool_6)
        {
            LoadParameters(device);
        }

        /// <summary>
        /// Sets scene ambient lighting (used during the Final rendering pass).
        /// </summary>
        public void SetAmbientLighting(IAmbientSource light, Vector3 directionHint)
        {
            if (FxAmbientColor == null || FxAmbientDirection == null || !(light is ILight))
                return;
            Vector3 vector3_2;
            Vector3 vector3_3;
            CoreUtils.smethod_1((light as ILight).CompositeColorAndIntensity, light.Depth, 0.65f, out vector3_2, out vector3_3);
            this.vector3_2[0] = vector3_2;
            this.vector3_2[1] = vector3_3;
            this.vector3_2[2] = (vector3_2 + vector3_3) * 0.2f;
            FxAmbientColor.SetValue(this.vector3_2);
            EffectHelper.Update(directionHint, ref vector3_1, ref FxAmbientDirection);
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

        /// <summary>
        /// Creates a new empty effect of the same class type and using the same effect file as this object.
        /// </summary>
        /// <param name="device"></param>
        /// <returns></returns>
        protected override Effect Create(GraphicsDevice device)
        {
            return new DeferredObjectEffect(device);
        }

        void LoadParameters(GraphicsDevice graphicsDevice_0)
        {
            FxTransClip = Parameters["_TransClipRef"];
            FxOffsetBiasDepthBiasTransClip = Parameters["_OffsetBias_DepthBias_TransClipRef"];
            FxDirectionOrPosAndRadius = Parameters["_Direction_Or_Position_And_Radius"];
            FxTargetWidthHeight = Parameters["_TargetWidthHeight"];
            FxAmbientColor = Parameters["_AmbientColor"];
            FxAmbientDirection = Parameters["_AmbientDirection"];
            FxDiffuseMap = Parameters["_SceneLightingDiffuseMap"];
            FxSpecularMap = Parameters["_SceneLightingSpecularMap"];
            FxFogStartDistEndDist = Parameters["_FogStartDist_EndDistInv"];
            FxFogColor = Parameters["_FogColor"];
            ShadowPrimaryBias = 1f;
            ShadowSecondaryBias = 0.2f;
            SetTechnique();
        }

        /// <summary>
        /// Sets the effect technique based on its current property values.
        /// </summary>
        protected override void SetTechnique()
        {
            ++class46_0.lightingSystemStatistic_0.AccumulationValue;
            bool bool_1 = TransparencyMap != null && TransparencyMode != TransparencyMode.None;
            if (deferredEffectOutput_0 == DeferredEffectOutput.ShadowDepth)
            {
                CurrentTechnique = Techniques[TechniquNames.Get(TechniquNames.Enum3.ShadowGen, TechniquNames.Enum4.Point, 0, false, bool_1, Skinned, false)];
            }
            else
            {
                bool flag1 = EffectDetail <= DetailPreference.Medium && TransparencyMode == TransparencyMode.None;
                bool flag2 = _EmissiveMapTexture != null;
                if (deferredEffectOutput_0 == DeferredEffectOutput.GBuffer)
                    CurrentTechnique = Techniques[TechniquNames.Get(TechniquNames.Enum3.DeferredGBuffer, !flag1 || _ParallaxMapTexture == null ? TechniquNames.Enum4.DiffuseBump : TechniquNames.Enum4.DiffuseParallax, 0, DoubleSided, bool_1, Skinned, false)];
                else if (deferredEffectOutput_0 == DeferredEffectOutput.Depth)
                {
                    TechniquNames.Enum4 enum4_0 = TechniquNames.Enum4.DiffuseBump;
                    if (bool_1 && flag1 && _ParallaxMapTexture != null)
                        enum4_0 = TechniquNames.Enum4.DiffuseParallax;
                    CurrentTechnique = Techniques[TechniquNames.Get(TechniquNames.Enum3.DeferredDepth, enum4_0, 0, false, bool_1, Skinned, false)];
                }
                else
                {
                    if (deferredEffectOutput_0 != DeferredEffectOutput.Final)
                        return;
                    TechniquNames.Enum4 enum4_0 = !flag1 || _ParallaxMapTexture == null ? (!flag2 ? TechniquNames.Enum4.DiffuseBump : TechniquNames.Enum4.DiffuseBumpEmissive) : (!flag2 ? TechniquNames.Enum4.DiffuseParallax : TechniquNames.Enum4.DiffuseParallaxEmissive);
                    if (bool_5)
                        CurrentTechnique = Techniques[TechniquNames.Get(TechniquNames.Enum3.DeferredFinalFog, enum4_0, 0, false, bool_1, Skinned, false)];
                    else
                        CurrentTechnique = Techniques[TechniquNames.Get(TechniquNames.Enum3.DeferredFinal, enum4_0, 0, false, bool_1, Skinned, false)];
                }
            }
        }

        void method_6(float float_9, float float_10)
        {
            if (FxFogStartDistEndDist == null || float_7 == (double)float_9 && float_8 == (double)float_10)
                return;
            float_7 = Math.Max(float_9, 0.0f);
            float_8 = Math.Max(float_7 * 1.01f, float_10);
            float y = float_8 - float_7;
            if (y != 0.0)
                y = 1f / y;
            FxFogStartDistEndDist.SetValue(new Vector2(float_7, y));
        }

        /// <summary>
        /// Applies the object's transparency information to its effect parameters.
        /// </summary>
        protected override void SyncTransparency(bool changedmode)
        {
            Vector4 vector42 = vector4_2;
            Vector4 vector41 = vector4_1;
            if (TransparencyMode == TransparencyMode.Clip)
            {
                vector42.Z = Transparency;
                vector41.X = Transparency;
            }
            else
            {
                vector42.Z = 0.0f;
                vector41.X = 0.0f;
            }
            EffectHelper.Update(vector42, ref vector4_2, ref FxOffsetBiasDepthBiasTransClip);
            EffectHelper.Update(vector41, ref vector4_1, ref FxTransClip);
            if (!changedmode)
                return;
            SetTechnique();
        }

        /// <summary>
        /// Applies the provided diffuse information to the object and its effect parameters.
        /// </summary>
        /// <param name="diffusecolor"></param>
        /// <param name="diffusemap"></param>
        /// <param name="normalmap"></param>
        protected override void SyncDiffuseAndNormalData(Vector4 diffusecolor, Texture2D diffusemap, Texture2D normalmap)
        {
            _DiffuseColorOriginal = diffusecolor;
            if (diffusemap != null && diffusemap != _DefaultDiffuseMapTexture)
            {
                EffectHelper.Update(diffusemap, ref _DiffuseMapTexture, _DiffuseMapTextureIndirectParam);
                EffectHelper.Update(Vector4.One, ref _DiffuseColorCached, ref _DiffuseColorIndirectParam);
            }
            else
            {
                EffectHelper.Update(_DefaultDiffuseMapTexture, ref _DiffuseMapTexture, _DiffuseMapTextureIndirectParam);
                EffectHelper.Update(diffusecolor, ref _DiffuseColorCached, ref _DiffuseColorIndirectParam);
            }
            if (normalmap != null && normalmap != _DefaultNormalMapTexture)
                EffectHelper.Update(normalmap, ref _NormalMapTexture, _NormalMapTextureIndirectParam);
            else
                EffectHelper.Update(_DefaultNormalMapTexture, ref _NormalMapTexture, _NormalMapTextureIndirectParam);
        }
    }
}
