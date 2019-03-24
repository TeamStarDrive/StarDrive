// Decompiled with JetBrains decompiler
// Type: Class36
// Assembly: SynapseGaming-SunBurn-Pro, Version=1.3.2.8, Culture=neutral, PublicKeyToken=c23c60523565dbfd
// MVID: A5F03349-72AC-4BAA-AEEE-9AB9B77E0A39
// Assembly location: C:\Projects\BlackBox\StarDrive\SynapseGaming-SunBurn-Pro.dll

using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using EmbeddedResources;
using SynapseGaming.LightingSystem.Core;
using SynapseGaming.LightingSystem.Effects;

internal class ShadowEffect : BaseSkinnedEffect, IAddressableEffect, ITransparentEffect, ITerrainEffect, IShadowEffect, IShadowGenerateEffect
{
    static Matrix[] matrix_12 = new Matrix[3];
    static Vector4[] vector4_2 = new Vector4[3];
    TextureCube textureCube_0;
    TextureCube textureCube_1;
    Texture2D texture2D_1;
    float ShadowMapSize;
    float float_3;
    float float_4;
    Vector4 vector4_0;
    Vector4 vector4_1;
    Matrix CameraViewToWorld;
    int int_0;
    float float_5;
    float float_6;
    Texture2D texture2D_2;
    EffectParameter TransparencyParam;
    EffectParameter effectParameter_13;
    EffectParameter ShadowMapSizeParam;
    EffectParameter ShadowMapParam;
    EffectParameter effectParameter_16;
    EffectParameter effectParameter_17;
    EffectParameter effectParameter_18;
    EffectParameter effectParameter_19;
    EffectParameter effectParameter_20;
    EffectParameter effectParameter_21;
    EffectParameter effectParameter_22;
    EffectParameter effectParameter_23;
    EffectParameter effectParameter_24;
    EffectParameter effectParameter_25;
    EffectParameter FxCameraViewToWorld;
    EffectParameter effectParameter_27;
    EffectParameter effectParameter_28;
    EffectParameter effectParameter_29;
    EffectParameter effectParameter_30;

    public TextureCube ShadowFaceMap
    {
        get => textureCube_0;
        set
        {
            if (effectParameter_16 == null || value == textureCube_0)
                return;
            effectParameter_16.SetValue(value);
            textureCube_0 = value;
        }
    }

    public TextureCube ShadowCoordMap
    {
        get => textureCube_1;
        set
        {
            if (effectParameter_17 == null || value == textureCube_1)
                return;
            effectParameter_17.SetValue(value);
            textureCube_1 = value;
        }
    }

    public Texture2D ShadowMap { get; private set; }

    public float ShadowPrimaryBias
    {
        get => float_4;
        set
        {
            if (effectParameter_20 == null || value == (double)float_4)
                return;
            effectParameter_20.SetValue(value);
            float_4 = value;
        }
    }

    public float ShadowSecondaryBias
    {
        get => float_3;
        set
        {
            if (effectParameter_19 == null || value == (double)float_3)
                return;
            effectParameter_19.SetValue(value);
            float_3 = value;
        }
    }

    public Vector4 ShadowViewDistance
    {
        get => vector4_1;
        set
        {
            value.W = Math.Min(value.Z * 0.99f, value.W);
            EffectHelper.Update(value, ref vector4_1, ref effectParameter_22);
        }
    }

    public Vector4[] ShadowMapLocationAndSpan
    {
        set
        {
            Matrix matrix1 = new Matrix();
            matrix1.M11 = (float)((value[0].X + (double)value[1].X) * 0.5);
            matrix1.M12 = (float)((value[0].Y + (double)value[1].Y) * 0.5);
            matrix1.M13 = (float)((value[0].Z + (double)value[1].Z) * 0.5);
            matrix1.M14 = (float)((value[0].W + (double)value[1].W) * 0.5);
            matrix1.M21 = (float)((value[2].X + (double)value[3].X) * 0.5);
            matrix1.M22 = (float)((value[2].Y + (double)value[3].Y) * 0.5);
            matrix1.M23 = (float)((value[2].Z + (double)value[3].Z) * 0.5);
            matrix1.M24 = (float)((value[2].W + (double)value[3].W) * 0.5);
            matrix1.M31 = (float)((value[4].X + (double)value[5].X) * 0.5);
            matrix1.M32 = (float)((value[4].Y + (double)value[5].Y) * 0.5);
            matrix1.M33 = (float)((value[4].Z + (double)value[5].Z) * 0.5);
            matrix1.M34 = (float)((value[4].W + (double)value[5].W) * 0.5);
            Matrix matrix2 = new Matrix();
            matrix2.M11 = (float)((value[0].X - (double)value[1].X) * 0.5);
            matrix2.M12 = (float)((value[0].Y - (double)value[1].Y) * 0.5);
            matrix2.M13 = (float)((value[0].Z - (double)value[1].Z) * 0.5);
            matrix2.M14 = (float)((value[0].W - (double)value[1].W) * 0.5);
            matrix2.M21 = (float)((value[2].X - (double)value[3].X) * 0.5);
            matrix2.M22 = (float)((value[2].Y - (double)value[3].Y) * 0.5);
            matrix2.M23 = (float)((value[2].Z - (double)value[3].Z) * 0.5);
            matrix2.M24 = (float)((value[2].W - (double)value[3].W) * 0.5);
            matrix2.M31 = (float)((value[4].X - (double)value[5].X) * 0.5);
            matrix2.M32 = (float)((value[4].Y - (double)value[5].Y) * 0.5);
            matrix2.M33 = (float)((value[4].Z - (double)value[5].Z) * 0.5);
            matrix2.M34 = (float)((value[4].W - (double)value[5].W) * 0.5);
            if (effectParameter_23 != null)
            {
                int num = Math.Min(value.Length, vector4_2.Length);
                for (int index = 0; index < num; ++index)
                    vector4_2[index] = value[index];
                effectParameter_23.SetArrayRange(0, 3);
                effectParameter_23.SetValue(vector4_2);
            }
            if (effectParameter_24 == null || effectParameter_25 == null)
                return;
            effectParameter_24.SetValue(matrix1);
            effectParameter_25.SetValue(matrix2);
        }
    }

    public BoundingSphere ShadowArea
    {
        set => EffectHelper.Update(new Vector4(value.Center, value.Radius), ref vector4_0, ref effectParameter_13);
    }

    public Matrix[] ShadowViewProjection
    {
        set
        {
            if (effectParameter_21 == null)
                return;
            int num = Math.Min(value.Length, matrix_12.Length);
            for (int index = 0; index < num; ++index)
                matrix_12[index] = value[index];
            effectParameter_21.SetArrayRange(0, 3);
            effectParameter_21.SetValue(matrix_12);
        }
    }

    public Enum5 ShadowSourceType { get; private set; }

    public bool SupportsShadowGeneration => true;

    public TransparencyMode TransparencyMode { get; private set; }

    public float Transparency { get; private set; } = -1f;

    public Texture TransparencyMap => texture2D_1;

    public TextureAddressMode AddressModeU { get; set; } = TextureAddressMode.Wrap;

    public TextureAddressMode AddressModeV { get; set; } = TextureAddressMode.Wrap;

    public TextureAddressMode AddressModeW { get; set; } = TextureAddressMode.Wrap;

    public Texture2D HeightMapTexture
    {
        get => texture2D_2;
        set
        {
            if (value == texture2D_2)
                return;
            EffectHelper.Update(value, ref texture2D_2, effectParameter_30);
            SetTechnique();
        }
    }

    public float HeightScale
    {
        get => float_5;
        set => EffectHelper.Update(value, ref float_5, ref effectParameter_28);
    }

    public float Tiling
    {
        get => float_6;
        set => EffectHelper.Update(value, ref float_6, ref effectParameter_29);
    }

    public int MeshSegments
    {
        get => int_0;
        set => EffectHelper.Update(value, ref int_0, ref effectParameter_27);
    }

    public ShadowEffect(GraphicsDevice device) : base(device, "ShadowEffect")
    {
        TransparencyParam = Parameters["_TransparencyClipReference"];
        effectParameter_13 = Parameters["_Direction_Or_Position_And_Radius"];
        ShadowMapSizeParam = Parameters["_ShadowBufferPageSize"];
        ShadowMapParam = Parameters["_ShadowMap"];
        effectParameter_16 = Parameters["_FaceMap"];
        effectParameter_17 = Parameters["_CoordMap"];
        effectParameter_18 = Parameters["_TransparencyMap"];
        effectParameter_19 = Parameters["_DepthBias"];
        effectParameter_20 = Parameters["_OffsetBias"];
        effectParameter_21 = Parameters["_ShadowViewProjection"];
        effectParameter_22 = Parameters["_ShadowViewDistance"];
        effectParameter_23 = Parameters["_RenderTargetLocation_And_Span"];
        effectParameter_24 = Parameters["_RenderTargetLocation_Offset"];
        effectParameter_25 = Parameters["_RenderTargetLocation_Difference"];
        FxCameraViewToWorld = Parameters["_CameraViewToWorld"];
        effectParameter_30 = Parameters["HeightMapTexture"];
        effectParameter_27 = Parameters["MeshSegments"];
        effectParameter_28 = Parameters["HeightScale"];
        effectParameter_29 = Parameters["Tiling"];
        ShadowFaceMap = LightingSystemManager.Instance.method_5(device);
        ShadowCoordMap = LightingSystemManager.Instance.method_6(device);
        ShadowPrimaryBias = 1f;
        ShadowSecondaryBias = 0.2f;
        SetTechnique();
    }

    public void SetCameraView(in Matrix view, in Matrix viewToWorld)
    {
        EffectHelper.UpdateViewToWorld(viewToWorld, ref CameraViewToWorld, ref FxCameraViewToWorld);
    }

    public void SetTransparencyModeAndMap(TransparencyMode mode, float transparency, Texture map)
    {
        bool flag = false;
        if (mode != TransparencyMode)
        {
            TransparencyMode = mode;
            flag = true;
        }
        if (TransparencyParam != null && transparency != (double)Transparency)
        {
            Transparency = transparency;
            TransparencyParam.SetValue(Transparency);
            flag = true;
        }
        Texture2D texture2D = map as Texture2D;
        if (effectParameter_18 != null && texture2D != texture2D_1)
        {
            texture2D_1 = texture2D;
            effectParameter_18.SetValue(texture2D_1);
            flag = true;
        }
        if (!flag)
            return;
        SetTechnique();
    }

    protected override void SetTechnique()
    {
        ++class46_0.lightingSystemStatistic_0.AccumulationValue;
        bool bool_3 = texture2D_2 != null;
        TechniquNames.Enum3 enum3_0;
        TechniquNames.Enum4 enum4_0;
        if (ShadowMap == null)
        {
            enum3_0 = TechniquNames.Enum3.ShadowGen;
            enum4_0 = ShadowSourceType != Enum5.const_1 ? TechniquNames.Enum4.Point : TechniquNames.Enum4.Directional;
        }
        else if (EffectDetail == DetailPreference.High)
        {
            enum3_0 = TechniquNames.Enum3.Shadow;
            enum4_0 = ShadowSourceType != Enum5.const_1 ? TechniquNames.Enum4.Point4 : TechniquNames.Enum4.Directional4;
        }
        else if (EffectDetail == DetailPreference.Medium)
        {
            enum3_0 = TechniquNames.Enum3.Shadow;
            enum4_0 = ShadowSourceType != Enum5.const_1 ? TechniquNames.Enum4.Point3 : TechniquNames.Enum4.Directional3;
        }
        else
        {
            enum3_0 = TechniquNames.Enum3.Shadow;
            enum4_0 = ShadowSourceType != Enum5.const_1 ? TechniquNames.Enum4.Point : TechniquNames.Enum4.Directional;
        }
        CurrentTechnique = Techniques[TechniquNames.Get(enum3_0, enum4_0, 0, false, texture2D_1 != null && TransparencyMode != TransparencyMode.None, Skinned, bool_3)];
    }

    public void SetShadowMapAndType(Texture2D shadowmap, Enum5 type)
    {
        bool flag = false;
        if (type != ShadowSourceType)
        {
            ShadowSourceType = type;
            flag = true;
        }
        if (ShadowMapSizeParam != null && shadowmap != null && shadowmap.Width != (double)ShadowMapSize)
        {
            ShadowMapSize = shadowmap.Width;
            ShadowMapSizeParam.SetValue(ShadowMapSize);
            flag = true;
        }
        if (ShadowMapParam != null && shadowmap != ShadowMap)
        {
            ShadowMap = shadowmap;
            ShadowMapParam.SetValue(ShadowMap);
            flag = true;
        }
        if (!flag)
            return;
        SetTechnique();
    }

    protected override Effect Create(GraphicsDevice device)
    {
        return new ShadowEffect(device);
    }
}
