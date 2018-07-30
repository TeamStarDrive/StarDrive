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

internal class ShadowEffect : BaseSkinnedEffect, IAddressableEffect, ITransparentEffect, ITerrainEffect, Interface3, IShadowGenerateEffect
{
    private static Matrix[] matrix_12 = new Matrix[3];
    private static Vector4[] vector4_2 = new Vector4[3];
    private TextureCube textureCube_0;
    private TextureCube textureCube_1;
    private Texture2D texture2D_1;
    private float ShadowMapSize;
    private float float_3;
    private float float_4;
    private Vector4 vector4_0;
    private Vector4 vector4_1;
    private Matrix matrix_11;
    private int int_0;
    private float float_5;
    private float float_6;
    private Texture2D texture2D_2;
    private EffectParameter TransparencyParam;
    private EffectParameter effectParameter_13;
    private EffectParameter ShadowMapSizeParam;
    private EffectParameter ShadowMapParam;
    private EffectParameter effectParameter_16;
    private EffectParameter effectParameter_17;
    private EffectParameter effectParameter_18;
    private EffectParameter effectParameter_19;
    private EffectParameter effectParameter_20;
    private EffectParameter effectParameter_21;
    private EffectParameter effectParameter_22;
    private EffectParameter effectParameter_23;
    private EffectParameter effectParameter_24;
    private EffectParameter effectParameter_25;
    private EffectParameter effectParameter_26;
    private EffectParameter effectParameter_27;
    private EffectParameter effectParameter_28;
    private EffectParameter effectParameter_29;
    private EffectParameter effectParameter_30;

    public TextureCube ShadowFaceMap
    {
        get => this.textureCube_0;
        set
        {
            if (this.effectParameter_16 == null || value == this.textureCube_0)
                return;
            this.effectParameter_16.SetValue(value);
            this.textureCube_0 = value;
        }
    }

    public TextureCube ShadowCoordMap
    {
        get => this.textureCube_1;
        set
        {
            if (this.effectParameter_17 == null || value == this.textureCube_1)
                return;
            this.effectParameter_17.SetValue(value);
            this.textureCube_1 = value;
        }
    }

    public Texture2D ShadowMap { get; private set; }

    public float ShadowPrimaryBias
    {
        get => this.float_4;
        set
        {
            if (this.effectParameter_20 == null || value == (double)this.float_4)
                return;
            this.effectParameter_20.SetValue(value);
            this.float_4 = value;
        }
    }

    public float ShadowSecondaryBias
    {
        get => this.float_3;
        set
        {
            if (this.effectParameter_19 == null || value == (double)this.float_3)
                return;
            this.effectParameter_19.SetValue(value);
            this.float_3 = value;
        }
    }

    public Vector4 ShadowViewDistance
    {
        get => this.vector4_1;
        set
        {
            value.W = Math.Min(value.Z * 0.99f, value.W);
            EffectHelper.smethod_3(value, ref this.vector4_1, ref this.effectParameter_22);
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
            if (this.effectParameter_23 != null)
            {
                int num = Math.Min(value.Length, vector4_2.Length);
                for (int index = 0; index < num; ++index)
                    vector4_2[index] = value[index];
                this.effectParameter_23.SetArrayRange(0, 3);
                this.effectParameter_23.SetValue(vector4_2);
            }
            if (this.effectParameter_24 == null || this.effectParameter_25 == null)
                return;
            this.effectParameter_24.SetValue(matrix1);
            this.effectParameter_25.SetValue(matrix2);
        }
    }

    public BoundingSphere ShadowArea
    {
        set => EffectHelper.smethod_3(new Vector4(value.Center, value.Radius), ref this.vector4_0, ref this.effectParameter_13);
    }

    public Matrix[] ShadowViewProjection
    {
        set
        {
            if (this.effectParameter_21 == null)
                return;
            int num = Math.Min(value.Length, matrix_12.Length);
            for (int index = 0; index < num; ++index)
                matrix_12[index] = value[index];
            this.effectParameter_21.SetArrayRange(0, 3);
            this.effectParameter_21.SetValue(matrix_12);
        }
    }

    public Enum5 ShadowSourceType { get; private set; }

    public bool SupportsShadowGeneration => true;

    public TransparencyMode TransparencyMode { get; private set; }

    public float Transparency { get; private set; } = -1f;

    public Texture TransparencyMap => this.texture2D_1;

    public TextureAddressMode AddressModeU { get; set; } = TextureAddressMode.Wrap;

    public TextureAddressMode AddressModeV { get; set; } = TextureAddressMode.Wrap;

    public TextureAddressMode AddressModeW { get; set; } = TextureAddressMode.Wrap;

    public Texture2D HeightMapTexture
    {
        get => this.texture2D_2;
        set
        {
            if (value == this.texture2D_2)
                return;
            EffectHelper.SetParam(value, ref this.texture2D_2, this.effectParameter_30);
            this.SetTechnique();
        }
    }

    public float HeightScale
    {
        get => this.float_5;
        set => EffectHelper.smethod_6(value, ref this.float_5, ref this.effectParameter_28);
    }

    public float Tiling
    {
        get => this.float_6;
        set => EffectHelper.smethod_6(value, ref this.float_6, ref this.effectParameter_29);
    }

    public int MeshSegments
    {
        get => this.int_0;
        set => EffectHelper.smethod_5(value, ref this.int_0, ref this.effectParameter_27);
    }

    public ShadowEffect(GraphicsDevice graphicsdevice)
      : base(graphicsdevice, "ShadowEffect")
    {
        this.TransparencyParam = this.Parameters["_TransparencyClipReference"];
        this.effectParameter_13 = this.Parameters["_Direction_Or_Position_And_Radius"];
        this.ShadowMapSizeParam = this.Parameters["_ShadowBufferPageSize"];
        this.ShadowMapParam = this.Parameters["_ShadowMap"];
        this.effectParameter_16 = this.Parameters["_FaceMap"];
        this.effectParameter_17 = this.Parameters["_CoordMap"];
        this.effectParameter_18 = this.Parameters["_TransparencyMap"];
        this.effectParameter_19 = this.Parameters["_DepthBias"];
        this.effectParameter_20 = this.Parameters["_OffsetBias"];
        this.effectParameter_21 = this.Parameters["_ShadowViewProjection"];
        this.effectParameter_22 = this.Parameters["_ShadowViewDistance"];
        this.effectParameter_23 = this.Parameters["_RenderTargetLocation_And_Span"];
        this.effectParameter_24 = this.Parameters["_RenderTargetLocation_Offset"];
        this.effectParameter_25 = this.Parameters["_RenderTargetLocation_Difference"];
        this.effectParameter_26 = this.Parameters["_CameraViewToWorld"];
        this.effectParameter_30 = this.Parameters["HeightMapTexture"];
        this.effectParameter_27 = this.Parameters["MeshSegments"];
        this.effectParameter_28 = this.Parameters["HeightScale"];
        this.effectParameter_29 = this.Parameters["Tiling"];
        this.ShadowFaceMap = LightingSystemManager.Instance.method_5(graphicsdevice);
        this.ShadowCoordMap = LightingSystemManager.Instance.method_6(graphicsdevice);
        this.ShadowPrimaryBias = 1f;
        this.ShadowSecondaryBias = 0.2f;
        this.SetTechnique();
    }

    public void SetCameraView(Matrix view, Matrix viewtoworld)
    {
        EffectHelper.smethod_0(viewtoworld, ref this.matrix_11, ref this.effectParameter_26);
    }

    public void SetTransparencyModeAndMap(TransparencyMode mode, float transparency, Texture map)
    {
        bool flag = false;
        if (mode != this.TransparencyMode)
        {
            this.TransparencyMode = mode;
            flag = true;
        }
        if (this.TransparencyParam != null && transparency != (double)this.Transparency)
        {
            this.Transparency = transparency;
            this.TransparencyParam.SetValue(this.Transparency);
            flag = true;
        }
        Texture2D texture2D = map as Texture2D;
        if (this.effectParameter_18 != null && texture2D != this.texture2D_1)
        {
            this.texture2D_1 = texture2D;
            this.effectParameter_18.SetValue(this.texture2D_1);
            flag = true;
        }
        if (!flag)
            return;
        this.SetTechnique();
    }

    protected override void SetTechnique()
    {
        ++this.class46_0.lightingSystemStatistic_0.AccumulationValue;
        bool bool_3 = this.texture2D_2 != null;
        TechniquNames.Enum3 enum3_0;
        TechniquNames.Enum4 enum4_0;
        if (this.ShadowMap == null)
        {
            enum3_0 = TechniquNames.Enum3.ShadowGen;
            enum4_0 = this.ShadowSourceType != Enum5.const_1 ? TechniquNames.Enum4.Point : TechniquNames.Enum4.Directional;
        }
        else if (this.EffectDetail == DetailPreference.High)
        {
            enum3_0 = TechniquNames.Enum3.Shadow;
            enum4_0 = this.ShadowSourceType != Enum5.const_1 ? TechniquNames.Enum4.Point4 : TechniquNames.Enum4.Directional4;
        }
        else if (this.EffectDetail == DetailPreference.Medium)
        {
            enum3_0 = TechniquNames.Enum3.Shadow;
            enum4_0 = this.ShadowSourceType != Enum5.const_1 ? TechniquNames.Enum4.Point3 : TechniquNames.Enum4.Directional3;
        }
        else
        {
            enum3_0 = TechniquNames.Enum3.Shadow;
            enum4_0 = this.ShadowSourceType != Enum5.const_1 ? TechniquNames.Enum4.Point : TechniquNames.Enum4.Directional;
        }
        this.CurrentTechnique = this.Techniques[TechniquNames.Get(enum3_0, enum4_0, 0, false, this.texture2D_1 != null && this.TransparencyMode != TransparencyMode.None, this.Skinned, bool_3)];
    }

    public void SetShadowMapAndType(Texture2D shadowmap, Enum5 type)
    {
        bool flag = false;
        if (type != this.ShadowSourceType)
        {
            this.ShadowSourceType = type;
            flag = true;
        }
        if (this.ShadowMapSizeParam != null && shadowmap != null && shadowmap.Width != (double)this.ShadowMapSize)
        {
            this.ShadowMapSize = shadowmap.Width;
            this.ShadowMapSizeParam.SetValue(this.ShadowMapSize);
            flag = true;
        }
        if (this.ShadowMapParam != null && shadowmap != this.ShadowMap)
        {
            this.ShadowMap = shadowmap;
            this.ShadowMapParam.SetValue(this.ShadowMap);
            flag = true;
        }
        if (!flag)
            return;
        this.SetTechnique();
    }

    protected override Effect Create(GraphicsDevice device)
    {
        return new ShadowEffect(device);
    }
}
