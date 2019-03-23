// Decompiled with JetBrains decompiler
// Type: ns5.Class38
// Assembly: SynapseGaming-SunBurn-Pro, Version=1.3.2.8, Culture=neutral, PublicKeyToken=c23c60523565dbfd
// MVID: A5F03349-72AC-4BAA-AEEE-9AB9B77E0A39
// Assembly location: C:\Projects\BlackBox\StarDrive\SynapseGaming-SunBurn-Pro.dll

using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using EmbeddedResources;
using SynapseGaming.LightingSystem.Core;
using SynapseGaming.LightingSystem.Effects;
using SynapseGaming.LightingSystem.Lights;
using SynapseGaming.LightingSystem.Shadows;

namespace ns5
{
    internal class Class38 : BaseRenderableEffect, Interface2, ILightingEffect, Interface3
    {
        private static Matrix[] matrix_10 = new Matrix[3];
        private static Vector4[] vector4_7 = new Vector4[3];
        private Vector4[] vector4_0 = new Vector4[10];
        private Vector4[] vector4_1 = new Vector4[10];
        private Vector4[] vector4_2 = new Vector4[10];
        private Vector4[] vector4_3 = new Vector4[10];
        private Matrix[] matrix_9 = new Matrix[3];
        private const int int_0 = 10;
        private Texture2D texture2D_0;
        private Texture2D texture2D_1;
        private Texture2D texture2D_2;
        private float float_1;
        private Vector2 vector2_0;
        private Vector4 vector4_4;
        private EffectParameter effectParameter_11;
        private EffectParameter effectParameter_12;
        private EffectParameter effectParameter_13;
        private EffectParameter effectParameter_14;
        private EffectParameter effectParameter_15;
        private EffectParameter effectParameter_16;
        private EffectParameter effectParameter_17;
        private EffectParameter effectParameter_18;
        private EffectParameter effectParameter_19;
        private EffectParameter effectParameter_20;
        private EffectParameter effectParameter_21;
        private Enum5 enum5_0;
        private DetailPreference detailPreference_1;
        private Texture2D texture2D_3;
        private TextureCube textureCube_0;
        private TextureCube textureCube_1;
        private float float_2;
        private Vector4 vector4_5;
        private EffectParameter effectParameter_22;
        private EffectParameter effectParameter_23;
        private EffectParameter effectParameter_24;
        private EffectParameter effectParameter_25;
        private EffectParameter effectParameter_26;
        private EffectParameter effectParameter_27;
        private Vector4 vector4_6;
        private EffectParameter effectParameter_28;
        private EffectParameter effectParameter_29;
        private EffectParameter effectParameter_30;
        private EffectParameter effectParameter_31;
        private EffectParameter effectParameter_32;
        private EffectParameter effectParameter_33;

        public Texture2D SceneDepthMap
        {
            get => texture2D_0;
            set
            {
                EffectHelper.Update(value, ref texture2D_0, effectParameter_13);
                if (effectParameter_12 == null || texture2D_0 == null)
                    return;
                EffectHelper.Update(new Vector2(texture2D_0.Width, texture2D_0.Height), ref vector2_0, ref effectParameter_12);
            }
        }

        public Texture2D SceneNormalSpecularMap
        {
            get => texture2D_1;
            set => EffectHelper.Update(value, ref texture2D_1, effectParameter_14);
        }

        public int MaxLightSources => 10;

        public List<ILight> LightSources
        {
            set
            {
                method_4(value);
                ++class46_0.lightingSystemStatistic_2.AccumulationValue;
            }
        }

        public float LightGroupDebugAmount
        {
            get => float_1;
            set => EffectHelper.Update(value, ref float_1, ref effectParameter_16);
        }

        public Texture2D MicrofacetTexture
        {
            get => texture2D_2;
            set => EffectHelper.Update(value, ref texture2D_2, effectParameter_15);
        }

        public TextureCube ShadowFaceMap
        {
            get => textureCube_0;
            set
            {
                if (effectParameter_25 == null || value == textureCube_0)
                    return;
                effectParameter_25.SetValue(value);
                textureCube_0 = value;
            }
        }

        public TextureCube ShadowCoordMap
        {
            get => textureCube_1;
            set
            {
                if (effectParameter_26 == null || value == textureCube_1)
                    return;
                effectParameter_26.SetValue(value);
                textureCube_1 = value;
            }
        }

        public Texture2D ShadowMap
        {
            get => texture2D_3;
            set => SetShadowMapAndType(value, enum5_0);
        }

        public Vector4 ShadowViewDistance
        {
            get => vector4_5;
            set
            {
                value.W = Math.Min(value.Z * 0.99f, value.W);
                EffectHelper.Update(value, ref vector4_5, ref effectParameter_27);
            }
        }

        public Vector4[] ShadowMapLocationAndSpan
        {
            set
            {
                Matrix matrix1 = new Matrix();
                matrix1.M11 = (float) ((value[0].X + (double) value[1].X) * 0.5);
                matrix1.M12 = (float) ((value[0].Y + (double) value[1].Y) * 0.5);
                matrix1.M13 = (float) ((value[0].Z + (double) value[1].Z) * 0.5);
                matrix1.M14 = (float) ((value[0].W + (double) value[1].W) * 0.5);
                matrix1.M21 = (float) ((value[2].X + (double) value[3].X) * 0.5);
                matrix1.M22 = (float) ((value[2].Y + (double) value[3].Y) * 0.5);
                matrix1.M23 = (float) ((value[2].Z + (double) value[3].Z) * 0.5);
                matrix1.M24 = (float) ((value[2].W + (double) value[3].W) * 0.5);
                matrix1.M31 = (float) ((value[4].X + (double) value[5].X) * 0.5);
                matrix1.M32 = (float) ((value[4].Y + (double) value[5].Y) * 0.5);
                matrix1.M33 = (float) ((value[4].Z + (double) value[5].Z) * 0.5);
                matrix1.M34 = (float) ((value[4].W + (double) value[5].W) * 0.5);
                Matrix matrix2 = new Matrix();
                matrix2.M11 = (float) ((value[0].X - (double) value[1].X) * 0.5);
                matrix2.M12 = (float) ((value[0].Y - (double) value[1].Y) * 0.5);
                matrix2.M13 = (float) ((value[0].Z - (double) value[1].Z) * 0.5);
                matrix2.M14 = (float) ((value[0].W - (double) value[1].W) * 0.5);
                matrix2.M21 = (float) ((value[2].X - (double) value[3].X) * 0.5);
                matrix2.M22 = (float) ((value[2].Y - (double) value[3].Y) * 0.5);
                matrix2.M23 = (float) ((value[2].Z - (double) value[3].Z) * 0.5);
                matrix2.M24 = (float) ((value[2].W - (double) value[3].W) * 0.5);
                matrix2.M31 = (float) ((value[4].X - (double) value[5].X) * 0.5);
                matrix2.M32 = (float) ((value[4].Y - (double) value[5].Y) * 0.5);
                matrix2.M33 = (float) ((value[4].Z - (double) value[5].Z) * 0.5);
                matrix2.M34 = (float) ((value[4].W - (double) value[5].W) * 0.5);
                if (effectParameter_31 != null)
                {
                    int num = Math.Min(value.Length, vector4_7.Length);
                    for (int index = 0; index < num; ++index)
                        vector4_7[index] = value[index];
                    effectParameter_31.SetArrayRange(0, 3);
                    effectParameter_31.SetValue(vector4_7);
                }
                if (effectParameter_32 == null || effectParameter_33 == null)
                    return;
                effectParameter_32.SetValue(matrix1);
                effectParameter_33.SetValue(matrix2);
            }
        }

        public BoundingSphere ShadowArea
        {
            set => method_5(new Vector4(value.Center, value.Radius), matrix_9);
        }

        public Matrix[] ShadowViewProjection
        {
            set => method_5(vector4_6, value);
        }

        public Enum5 ShadowSourceType
        {
            get => enum5_0;
            set => SetShadowMapAndType(texture2D_3, value);
        }

        public DetailPreference ShadowDetail
        {
            get => detailPreference_1;
            set
            {
                detailPreference_1 = value;
                method_3();
            }
        }

        public BoundingSphere WorldClippingSphere
        {
            set => EffectHelper.Update(new Vector4(value.Center, value.Radius), ref vector4_4, ref effectParameter_11);
        }

        internal static int MaxLightSourcesInternal => 10;

        public Class38(GraphicsDevice graphicsdevice) : base(graphicsdevice, "DeferredLightingEffect")
        {
            effectParameter_13 = Parameters["_SceneDepthMap"];
            effectParameter_14 = Parameters["_SceneNormalSpecularMap"];
            effectParameter_15 = Parameters["_MicrofacetMap"];
            effectParameter_16 = Parameters["_DebugAmount"];
            effectParameter_17 = Parameters["_LightCount"];
            effectParameter_18 = Parameters["_LightPos_Radius"];
            effectParameter_19 = Parameters["_LightColor"];
            effectParameter_20 = Parameters["_LightSpotDir"];
            effectParameter_21 = Parameters["_LightModel_Fill_SpotAng_InvSpotAng"];
            effectParameter_12 = Parameters["_TargetWidthHeight"];
            effectParameter_11 = Parameters["_WorldSphere"];
            effectParameter_22 = Parameters["_ShadowType"];
            effectParameter_29 = Parameters["_Shadow_Direction_Or_Position_And_Radius"];
            effectParameter_23 = Parameters["_ShadowBufferPageSize"];
            effectParameter_24 = Parameters["_ShadowMap"];
            effectParameter_25 = Parameters["_FaceMap"];
            effectParameter_26 = Parameters["_CoordMap"];
            effectParameter_28 = Parameters["_ViewToCameraWorld"];
            effectParameter_30 = Parameters["_CameraWorldToShadowViewProjection"];
            effectParameter_27 = Parameters["_ShadowViewDistance"];
            effectParameter_31 = Parameters["_RenderTargetLocation_And_Span"];
            effectParameter_32 = Parameters["_RenderTargetLocation_Offset"];
            effectParameter_33 = Parameters["_RenderTargetLocation_Difference"];
            LightGroupDebugAmount = 0.0f;
            MicrofacetTexture = LightingSystemManager.Instance.method_7(graphicsdevice);
            ShadowFaceMap = LightingSystemManager.Instance.method_5(graphicsdevice);
            ShadowCoordMap = LightingSystemManager.Instance.method_6(graphicsdevice);
            SetTechnique();
        }

        public void SetShadowMapAndType(Texture2D shadowmap, Enum5 type)
        {
            if (effectParameter_23 != null && shadowmap != null && shadowmap.Width != (double)float_2)
            {
                effectParameter_23.SetValue(shadowmap.Width);
                float_2 = shadowmap.Width;
            }
            EffectHelper.Update(shadowmap, ref texture2D_3, effectParameter_24);
            enum5_0 = type;
            method_3();
        }

        private void method_3()
        {
            if (effectParameter_22 == null)
                return;
            if (enum5_0 == Enum5.const_1)
            {
                if (texture2D_3 == null)
                    effectParameter_22.SetValue(1);
                else if (detailPreference_1 == DetailPreference.High)
                    effectParameter_22.SetValue(7);
                else if (detailPreference_1 == DetailPreference.Medium)
                    effectParameter_22.SetValue(5);
                else
                    effectParameter_22.SetValue(3);
            }
            else
            {
                if (enum5_0 != Enum5.const_0)
                    return;
                if (texture2D_3 == null)
                    effectParameter_22.SetValue(0);
                else if (detailPreference_1 == DetailPreference.High)
                    effectParameter_22.SetValue(6);
                else if (detailPreference_1 == DetailPreference.Medium)
                    effectParameter_22.SetValue(4);
                else
                    effectParameter_22.SetValue(2);
            }
        }

        private void method_4(List<ILight> list_0)
        {
            if (effectParameter_17 == null || effectParameter_18 == null || (effectParameter_19 == null || effectParameter_20 == null) || effectParameter_21 == null)
                return;
            if (list_0.Count > 10)
                throw new Exception("Too many light sources provided for effect.");
            int index = 0;
            foreach (ILight light in list_0)
            {
                vector4_1[index] = new Vector4(light.CompositeColorAndIntensity, 0.0f);
                vector4_3[index] = !light.FillLight ? new Vector4(0.0f, 1f, 0.0f, 0.0f) : new Vector4(0.0f, 0.0f, 0.0f, 0.0f);
                if (light is ISpotSource)
                {
                    ISpotSource spotSource = light as ISpotSource;
                    float num1 = (float) Math.Cos(MathHelper.ToRadians(MathHelper.Clamp(spotSource.Angle * 0.5f, 0.01f, 89.99f)));
                    float num2 = (float) (1.0 / (1.0 - num1));
                    vector4_3[index].X = light.FalloffStrength;
                    vector4_3[index].Z = num1;
                    vector4_3[index].W = num2;
                    vector4_2[index] = new Vector4(Vector3.TransformNormal(spotSource.Direction, View), 0.0f);
                    vector4_0[index] = new Vector4(Vector3.Transform(spotSource.Position, View), spotSource.Radius);
                }
                else if (light is IPointSource)
                {
                    IPointSource pointSource = light as IPointSource;
                    vector4_3[index].X = light.FalloffStrength;
                    vector4_0[index] = new Vector4(Vector3.Transform(pointSource.Position, View), pointSource.Radius);
                }
                else if (light is IShadowSource)
                {
                    IShadowSource shadowSource = light as IShadowSource;
                    vector4_0[index] = new Vector4(Vector3.Transform(shadowSource.ShadowPosition, View), 1E+09f);
                }
                ++index;
            }
            effectParameter_17.SetValue(index);
            if (index < 1)
                return;
            effectParameter_18.SetValue(vector4_0);
            effectParameter_19.SetValue(vector4_1);
            effectParameter_20.SetValue(vector4_2);
            effectParameter_21.SetValue(vector4_3);
        }

        private void method_5(Vector4 vector4_8, Matrix[] matrix_11)
        {
            vector4_6 = vector4_8;
            int num = Math.Min(matrix_11.Length, matrix_9.Length);
            for (int index = 0; index < num; ++index)
                matrix_9[index] = matrix_11[index];
            Matrix translation = Matrix.CreateTranslation(ViewToWorld.Translation);
            Matrix viewToWorld = ViewToWorld;
            viewToWorld.Translation = Vector3.Zero;
            if (effectParameter_28 != null)
                effectParameter_28.SetValue(viewToWorld);
            if (effectParameter_29 != null)
                effectParameter_29.SetValue(new Vector4(Vector3.Transform(new Vector3(vector4_8.X, vector4_8.Y, vector4_8.Z), Matrix.Invert(translation)), vector4_8.W));
            if (effectParameter_30 == null)
                return;
            for (int index = 0; index < num; ++index)
                matrix_10[index] = translation * matrix_11[index];
            effectParameter_30.SetArrayRange(0, 3);
            effectParameter_30.SetValue(matrix_10);
        }

        protected override void SetTechnique()
        {
            if (EffectDetail == DetailPreference.High)
                CurrentTechnique = Techniques["Lighting_High_Technique"];
            else if (EffectDetail == DetailPreference.Medium)
                CurrentTechnique = Techniques["Lighting_Medium_Technique"];
            else if (EffectDetail == DetailPreference.Low)
                CurrentTechnique = Techniques["Lighting_Low_Technique"];
            else
                CurrentTechnique = Techniques["Lighting_Off_Technique"];
        }

        protected override Effect Create(GraphicsDevice device)
        {
            return new Class38(device);
        }
    }
}
