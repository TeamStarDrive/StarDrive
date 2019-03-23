// Decompiled with JetBrains decompiler
// Type: SynapseGaming.LightingSystem.Effects.Deferred.DeferredTerrainEffect
// Assembly: SynapseGaming-SunBurn-Pro, Version=1.3.2.8, Culture=neutral, PublicKeyToken=c23c60523565dbfd
// MVID: A5F03349-72AC-4BAA-AEEE-9AB9B77E0A39
// Assembly location: C:\Projects\BlackBox\StarDrive\SynapseGaming-SunBurn-Pro.dll

using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using ns3;
using SynapseGaming.LightingSystem.Lights;

namespace SynapseGaming.LightingSystem.Effects.Deferred
{
    /// <summary>
    /// Provides SunBurn's built-in deferred terrain rendering.
    /// </summary>
    public class DeferredTerrainEffect : BaseTerrainEffect, IShadowGenerateEffect, IDeferredObjectEffect
    {
        private Vector3[] vector3_3 = new Vector3[3];
        private Matrix CameraViewToWorld;
        private Vector4 vector4_0;
        private Vector4 vector4_1;
        private DeferredEffectOutput deferredEffectOutput_0;
        private Texture2D texture2D_13;
        private Texture2D texture2D_14;
        private Vector2 vector2_0;
        private bool bool_3;
        private float float_7;
        private float float_8;
        private Vector3 vector3_1;
        private Vector3 vector3_2;
        private EffectParameter effectParameter_31;
        private EffectParameter effectParameter_32;
        private EffectParameter FxCameraViewToWorld;
        private EffectParameter effectParameter_34;
        private EffectParameter effectParameter_35;
        private EffectParameter effectParameter_36;
        private EffectParameter effectParameter_37;
        private EffectParameter effectParameter_38;
        private EffectParameter effectParameter_39;
        private EffectParameter effectParameter_40;

        /// <summary>Main property used to eliminate shadow artifacts.</summary>
        public float ShadowPrimaryBias
        {
            get => this.vector4_0.X;
            set => EffectHelper.Update(new Vector4(value, this.vector4_0.Y, this.vector4_0.Z, this.vector4_0.W), ref this.vector4_0, ref this.effectParameter_35);
        }

        /// <summary>
        /// Additional fine-tuned property used to eliminate shadow artifacts.
        /// </summary>
        public float ShadowSecondaryBias
        {
            get => this.vector4_0.Y;
            set => EffectHelper.Update(new Vector4(this.vector4_0.X, value, this.vector4_0.Z, this.vector4_0.W), ref this.vector4_0, ref this.effectParameter_35);
        }

        /// <summary>
        /// Bounding area of the shadow source, where the bounds center is the actual shadow source location,
        /// and the radius is either the source radius (for point sources) or the maximum view based casting
        /// distance (for directional sources).
        /// </summary>
        public BoundingSphere ShadowArea
        {
            set => EffectHelper.Update(new Vector4(value.Center, value.Radius), ref this.vector4_1, ref this.effectParameter_36);
        }

        /// <summary>
        /// Determines if the effect is capable of generating shadow maps. Objects using effects unable to
        /// generate shadow maps automatically use the built-in shadow effect, however this puts
        /// heavy restrictions on how the effects handle rendering (only basic vertex transforms are supported).
        /// </summary>
        public bool SupportsShadowGeneration => true;

        /// <summary>
        /// Determines the type of shader output for the effects to generate.
        /// </summary>
        public DeferredEffectOutput DeferredEffectOutput
        {
            get => this.deferredEffectOutput_0;
            set
            {
                if (value == this.deferredEffectOutput_0)
                    return;
                this.deferredEffectOutput_0 = value;
                this.SetTechnique();
            }
        }

        /// <summary>
        /// Texture containing the screen-space lighting generated during deferred rendering (used during the Final rendering pass).
        /// </summary>
        public Texture2D SceneLightingDiffuseMap
        {
            get => this.texture2D_13;
            set
            {
                EffectHelper.Update(value, ref this.texture2D_13, this.effectParameter_37);
                if (this.effectParameter_34 == null || this.texture2D_13 == null)
                    return;
                EffectHelper.Update(new Vector2(this.texture2D_13.Width, this.texture2D_13.Height), ref this.vector2_0, ref this.effectParameter_34);
            }
        }

        /// <summary>
        /// Texture containing the screen-space specular generated during deferred rendering (used during the Final rendering pass).
        /// </summary>
        public Texture2D SceneLightingSpecularMap
        {
            get => this.texture2D_14;
            set => EffectHelper.Update(value, ref this.texture2D_14, this.effectParameter_38);
        }

        /// <summary>Enables scene fog.</summary>
        public bool FogEnabled
        {
            get => this.bool_3;
            set
            {
                this.bool_3 = value;
                this.SetTechnique();
            }
        }

        /// <summary>
        /// Distance from the camera in world space that fog begins.
        /// </summary>
        public float FogStartDistance
        {
            get => this.float_7;
            set => this.method_6(value, this.float_8);
        }

        /// <summary>
        /// Distance from the camera in world space that fog ends.
        /// </summary>
        public float FogEndDistance
        {
            get => this.float_8;
            set => this.method_6(this.float_7, value);
        }

        /// <summary>Color applied to scene fog.</summary>
        public Vector3 FogColor
        {
            get => this.vector3_1;
            set => EffectHelper.Update(value, ref this.vector3_1, ref this.effectParameter_40);
        }

        /// <summary>Creates a new DeferredTerrainEffect instance.</summary>
        /// <param name="graphicsdevice"></param>
        public DeferredTerrainEffect(GraphicsDevice graphicsdevice)
          : base(graphicsdevice, "DeferredTerrainEffect")
        {
            this.method_5();
        }

        internal DeferredTerrainEffect(GraphicsDevice device, bool bool_4)
          : base(device, "DeferredTerrainEffect", bool_4)
        {
            this.method_5();
        }

        /// <summary>
        /// Sets scene ambient lighting (used during the Final rendering pass).
        /// </summary>
        public void SetAmbientLighting(IAmbientSource light, Vector3 directionHint)
        {
            if (this.effectParameter_31 == null || this.effectParameter_32 == null || !(light is ILight))
                return;
            Vector3 vector3_2;
            Vector3 vector3_3;
            CoreUtils.smethod_1((light as ILight).CompositeColorAndIntensity, light.Depth, 0.65f, out vector3_2, out vector3_3);
            this.vector3_3[0] = vector3_2;
            this.vector3_3[1] = vector3_3;
            this.vector3_3[2] = (vector3_2 + vector3_3) * 0.2f;
            this.effectParameter_31.SetValue(this.vector3_3);
            EffectHelper.Update(directionHint, ref this.vector3_2, ref this.effectParameter_32);
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
            EffectHelper.UpdateViewToWorld(viewToWorld, ref CameraViewToWorld, ref FxCameraViewToWorld);
        }

        /// <summary>
        /// Sets the effect technique based on its current property values.
        /// </summary>
        protected override void SetTechnique()
        {
            switch (this.deferredEffectOutput_0)
            {
                case DeferredEffectOutput.Depth:
                    this.CurrentTechnique = this.Techniques["Terrain_Depth_Technique"];
                    break;
                case DeferredEffectOutput.GBuffer:
                    this.CurrentTechnique = this.Techniques["Terrain_GBuffer_Technique"];
                    break;
                case DeferredEffectOutput.ShadowDepth:
                    this.CurrentTechnique = this.Techniques["Terrain_Shadow_Technique"];
                    break;
                case DeferredEffectOutput.Final:
                    if (this.bool_3)
                    {
                        this.CurrentTechnique = this.Techniques["Terrain_FinalFog_Technique"];
                        break;
                    }
                    this.CurrentTechnique = this.Techniques["Terrain_Final_Technique"];
                    break;
            }
        }

        private void method_5()
        {
            this.FxCameraViewToWorld = this.Parameters["_CameraViewToWorld"];
            this.effectParameter_31 = this.Parameters["_AmbientColor"];
            this.effectParameter_32 = this.Parameters["_AmbientDirection"];
            this.effectParameter_37 = this.Parameters["_SceneLightingDiffuseMap"];
            this.effectParameter_38 = this.Parameters["_SceneLightingSpecularMap"];
            this.effectParameter_34 = this.Parameters["_TargetWidthHeight"];
            this.effectParameter_35 = this.Parameters["_OffsetBias_DepthBias_TransClipRef"];
            this.effectParameter_36 = this.Parameters["_Direction_Or_Position_And_Radius"];
            this.effectParameter_39 = this.Parameters["_FogStartDist_EndDistInv"];
            this.effectParameter_40 = this.Parameters["_FogColor"];
        }

        /// <summary>
        /// Creates a new empty effect of the same class type and using the same effect file as this object.
        /// </summary>
        /// <param name="device"></param>
        /// <returns></returns>
        protected override Effect Create(GraphicsDevice device)
        {
            return new DeferredTerrainEffect(device);
        }

        private void method_6(float float_9, float float_10)
        {
            if (this.effectParameter_39 == null || this.float_7 == (double)float_9 && this.float_8 == (double)float_10)
                return;
            this.float_7 = Math.Max(float_9, 0.0f);
            this.float_8 = Math.Max(this.float_7 * 1.01f, float_10);
            float y = this.float_8 - this.float_7;
            if (y != 0.0)
                y = 1f / y;
            this.effectParameter_39.SetValue(new Vector2(this.float_7, y));
        }
    }
}
