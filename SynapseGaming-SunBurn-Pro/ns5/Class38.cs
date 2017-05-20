// Decompiled with JetBrains decompiler
// Type: ns5.Class38
// Assembly: SynapseGaming-SunBurn-Pro, Version=1.3.2.8, Culture=neutral, PublicKeyToken=c23c60523565dbfd
// MVID: A5F03349-72AC-4BAA-AEEE-9AB9B77E0A39
// Assembly location: C:\Projects\BlackBox\StarDrive\SynapseGaming-SunBurn-Pro.dll

using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using ns6;
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
      get => this.texture2D_0;
        set
      {
        EffectHelper.smethod_8(value, ref this.texture2D_0, ref this.effectParameter_13);
        if (this.effectParameter_12 == null || this.texture2D_0 == null)
          return;
        EffectHelper.smethod_7(new Vector2(this.texture2D_0.Width, this.texture2D_0.Height), ref this.vector2_0, ref this.effectParameter_12);
      }
    }

    public Texture2D SceneNormalSpecularMap
    {
      get => this.texture2D_1;
        set => EffectHelper.smethod_8(value, ref this.texture2D_1, ref this.effectParameter_14);
    }

    public int MaxLightSources => 10;

      public List<ILight> LightSources
    {
      set
      {
        this.method_4(value);
        ++this.class46_0.lightingSystemStatistic_2.AccumulationValue;
      }
    }

    public float LightGroupDebugAmount
    {
      get => this.float_1;
        set => EffectHelper.smethod_6(value, ref this.float_1, ref this.effectParameter_16);
    }

    public Texture2D MicrofacetTexture
    {
      get => this.texture2D_2;
        set => EffectHelper.smethod_8(value, ref this.texture2D_2, ref this.effectParameter_15);
    }

    public TextureCube ShadowFaceMap
    {
      get => this.textureCube_0;
        set
      {
        if (this.effectParameter_25 == null || value == this.textureCube_0)
          return;
        this.effectParameter_25.SetValue(value);
        this.textureCube_0 = value;
      }
    }

    public TextureCube ShadowCoordMap
    {
      get => this.textureCube_1;
        set
      {
        if (this.effectParameter_26 == null || value == this.textureCube_1)
          return;
        this.effectParameter_26.SetValue(value);
        this.textureCube_1 = value;
      }
    }

    public Texture2D ShadowMap
    {
      get => this.texture2D_3;
        set => this.SetShadowMapAndType(value, this.enum5_0);
    }

    public Vector4 ShadowViewDistance
    {
      get => this.vector4_5;
        set
      {
        value.W = Math.Min(value.Z * 0.99f, value.W);
        EffectHelper.smethod_3(value, ref this.vector4_5, ref this.effectParameter_27);
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
        if (this.effectParameter_31 != null)
        {
          int num = Math.Min(value.Length, vector4_7.Length);
          for (int index = 0; index < num; ++index)
            vector4_7[index] = value[index];
          this.effectParameter_31.SetArrayRange(0, 3);
          this.effectParameter_31.SetValue(vector4_7);
        }
        if (this.effectParameter_32 == null || this.effectParameter_33 == null)
          return;
        this.effectParameter_32.SetValue(matrix1);
        this.effectParameter_33.SetValue(matrix2);
      }
    }

    public BoundingSphere ShadowArea
    {
      set => this.method_5(new Vector4(value.Center, value.Radius), this.matrix_9);
    }

    public Matrix[] ShadowViewProjection
    {
      set => this.method_5(this.vector4_6, value);
    }

    public Enum5 ShadowSourceType
    {
      get => this.enum5_0;
        set => this.SetShadowMapAndType(this.texture2D_3, value);
    }

    public DetailPreference ShadowDetail
    {
      get => this.detailPreference_1;
        set
      {
        this.detailPreference_1 = value;
        this.method_3();
      }
    }

    public BoundingSphere WorldClippingSphere
    {
      set => EffectHelper.smethod_3(new Vector4(value.Center, value.Radius), ref this.vector4_4, ref this.effectParameter_11);
    }

    internal static int MaxLightSourcesInternal => 10;

      public Class38(GraphicsDevice graphicsdevice)
      : base(graphicsdevice, "DeferredLightingEffect")
    {
      this.effectParameter_13 = this.Parameters["_SceneDepthMap"];
      this.effectParameter_14 = this.Parameters["_SceneNormalSpecularMap"];
      this.effectParameter_15 = this.Parameters["_MicrofacetMap"];
      this.effectParameter_16 = this.Parameters["_DebugAmount"];
      this.effectParameter_17 = this.Parameters["_LightCount"];
      this.effectParameter_18 = this.Parameters["_LightPos_Radius"];
      this.effectParameter_19 = this.Parameters["_LightColor"];
      this.effectParameter_20 = this.Parameters["_LightSpotDir"];
      this.effectParameter_21 = this.Parameters["_LightModel_Fill_SpotAng_InvSpotAng"];
      this.effectParameter_12 = this.Parameters["_TargetWidthHeight"];
      this.effectParameter_11 = this.Parameters["_WorldSphere"];
      this.effectParameter_22 = this.Parameters["_ShadowType"];
      this.effectParameter_29 = this.Parameters["_Shadow_Direction_Or_Position_And_Radius"];
      this.effectParameter_23 = this.Parameters["_ShadowBufferPageSize"];
      this.effectParameter_24 = this.Parameters["_ShadowMap"];
      this.effectParameter_25 = this.Parameters["_FaceMap"];
      this.effectParameter_26 = this.Parameters["_CoordMap"];
      this.effectParameter_28 = this.Parameters["_ViewToCameraWorld"];
      this.effectParameter_30 = this.Parameters["_CameraWorldToShadowViewProjection"];
      this.effectParameter_27 = this.Parameters["_ShadowViewDistance"];
      this.effectParameter_31 = this.Parameters["_RenderTargetLocation_And_Span"];
      this.effectParameter_32 = this.Parameters["_RenderTargetLocation_Offset"];
      this.effectParameter_33 = this.Parameters["_RenderTargetLocation_Difference"];
      this.LightGroupDebugAmount = 0.0f;
      this.MicrofacetTexture = LightingSystemManager.Instance.method_7(graphicsdevice);
      this.ShadowFaceMap = LightingSystemManager.Instance.method_5(graphicsdevice);
      this.ShadowCoordMap = LightingSystemManager.Instance.method_6(graphicsdevice);
      this.SetTechnique();
    }

    public void SetShadowMapAndType(Texture2D shadowmap, Enum5 type)
    {
      if (this.effectParameter_23 != null && shadowmap != null && shadowmap.Width != (double) this.float_2)
      {
        this.effectParameter_23.SetValue(shadowmap.Width);
        this.float_2 = shadowmap.Width;
      }
      EffectHelper.smethod_8(shadowmap, ref this.texture2D_3, ref this.effectParameter_24);
      this.enum5_0 = type;
      this.method_3();
    }

    private void method_3()
    {
      if (this.effectParameter_22 == null)
        return;
      if (this.enum5_0 == Enum5.const_1)
      {
        if (this.texture2D_3 == null)
          this.effectParameter_22.SetValue(1);
        else if (this.detailPreference_1 == DetailPreference.High)
          this.effectParameter_22.SetValue(7);
        else if (this.detailPreference_1 == DetailPreference.Medium)
          this.effectParameter_22.SetValue(5);
        else
          this.effectParameter_22.SetValue(3);
      }
      else
      {
        if (this.enum5_0 != Enum5.const_0)
          return;
        if (this.texture2D_3 == null)
          this.effectParameter_22.SetValue(0);
        else if (this.detailPreference_1 == DetailPreference.High)
          this.effectParameter_22.SetValue(6);
        else if (this.detailPreference_1 == DetailPreference.Medium)
          this.effectParameter_22.SetValue(4);
        else
          this.effectParameter_22.SetValue(2);
      }
    }

    private void method_4(List<ILight> list_0)
    {
      if (this.effectParameter_17 == null || this.effectParameter_18 == null || (this.effectParameter_19 == null || this.effectParameter_20 == null) || this.effectParameter_21 == null)
        return;
      if (list_0.Count > 10)
        throw new Exception("Too many light sources provided for effect.");
      int index = 0;
      foreach (ILight light in list_0)
      {
        this.vector4_1[index] = new Vector4(light.CompositeColorAndIntensity, 0.0f);
        this.vector4_3[index] = !light.FillLight ? new Vector4(0.0f, 1f, 0.0f, 0.0f) : new Vector4(0.0f, 0.0f, 0.0f, 0.0f);
        if (light is ISpotSource)
        {
          ISpotSource spotSource = light as ISpotSource;
          float num1 = (float) Math.Cos(MathHelper.ToRadians(MathHelper.Clamp(spotSource.Angle * 0.5f, 0.01f, 89.99f)));
          float num2 = (float) (1.0 / (1.0 - num1));
          this.vector4_3[index].X = light.FalloffStrength;
          this.vector4_3[index].Z = num1;
          this.vector4_3[index].W = num2;
          this.vector4_2[index] = new Vector4(Vector3.TransformNormal(spotSource.Direction, this.View), 0.0f);
          this.vector4_0[index] = new Vector4(Vector3.Transform(spotSource.Position, this.View), spotSource.Radius);
        }
        else if (light is IPointSource)
        {
          IPointSource pointSource = light as IPointSource;
          this.vector4_3[index].X = light.FalloffStrength;
          this.vector4_0[index] = new Vector4(Vector3.Transform(pointSource.Position, this.View), pointSource.Radius);
        }
        else if (light is IShadowSource)
        {
          IShadowSource shadowSource = light as IShadowSource;
          this.vector4_0[index] = new Vector4(Vector3.Transform(shadowSource.ShadowPosition, this.View), 1E+09f);
        }
        ++index;
      }
      this.effectParameter_17.SetValue(index);
      if (index < 1)
        return;
      this.effectParameter_18.SetValue(this.vector4_0);
      this.effectParameter_19.SetValue(this.vector4_1);
      this.effectParameter_20.SetValue(this.vector4_2);
      this.effectParameter_21.SetValue(this.vector4_3);
    }

    private void method_5(Vector4 vector4_8, Matrix[] matrix_11)
    {
      this.vector4_6 = vector4_8;
      int num = Math.Min(matrix_11.Length, this.matrix_9.Length);
      for (int index = 0; index < num; ++index)
        this.matrix_9[index] = matrix_11[index];
      Matrix translation = Matrix.CreateTranslation(this.ViewToWorld.Translation);
      Matrix viewToWorld = this.ViewToWorld;
      viewToWorld.Translation = Vector3.Zero;
      if (this.effectParameter_28 != null)
        this.effectParameter_28.SetValue(viewToWorld);
      if (this.effectParameter_29 != null)
        this.effectParameter_29.SetValue(new Vector4(Vector3.Transform(new Vector3(vector4_8.X, vector4_8.Y, vector4_8.Z), Matrix.Invert(translation)), vector4_8.W));
      if (this.effectParameter_30 == null)
        return;
      for (int index = 0; index < num; ++index)
        matrix_10[index] = translation * matrix_11[index];
      this.effectParameter_30.SetArrayRange(0, 3);
      this.effectParameter_30.SetValue(matrix_10);
    }

    protected override void SetTechnique()
    {
      if (this.EffectDetail == DetailPreference.High)
        this.CurrentTechnique = this.Techniques["Lighting_High_Technique"];
      else if (this.EffectDetail == DetailPreference.Medium)
        this.CurrentTechnique = this.Techniques["Lighting_Medium_Technique"];
      else if (this.EffectDetail == DetailPreference.Low)
        this.CurrentTechnique = this.Techniques["Lighting_Low_Technique"];
      else
        this.CurrentTechnique = this.Techniques["Lighting_Off_Technique"];
    }

    protected override Effect Create(GraphicsDevice device)
    {
      return new Class38(device);
    }
  }
}
