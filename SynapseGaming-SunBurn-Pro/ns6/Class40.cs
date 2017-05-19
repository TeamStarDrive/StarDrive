// Decompiled with JetBrains decompiler
// Type: ns6.Class40
// Assembly: SynapseGaming-SunBurn-Pro, Version=1.3.2.8, Culture=neutral, PublicKeyToken=c23c60523565dbfd
// MVID: A5F03349-72AC-4BAA-AEEE-9AB9B77E0A39
// Assembly location: C:\Projects\BlackBox\StarDrive\SynapseGaming-SunBurn-Pro.dll

using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using SynapseGaming.LightingSystem.Core;

namespace ns6
{
  internal class Class40 : Class39
  {
    public static Interface4 interface4_0 = new Class49();
    public static Interface4 interface4_1 = new Class50();
    private static Vector2[] vector2_1 = new Vector2[32];
    private static float[] float_1 = new float[32];
    private static Vector4[] vector4_0 = new Vector4[16];
    private static Vector2[] vector2_2 = new Vector2[16];
    private const int int_0 = 32;
    private Interface4 interface4_2;
    private DetailPreference detailPreference_1;
    private EffectParameter effectParameter_13;
    private EffectParameter effectParameter_14;

    public Interface4 BlurKernel
    {
      get => this.interface4_2;
        set
      {
        this.interface4_2 = value;
        this.method_2();
      }
    }

    public DetailPreference BloomDetail
    {
      get => this.detailPreference_1;
        set
      {
        this.detailPreference_1 = value;
        this.method_2();
        this.SetTechnique();
      }
    }

    public override Texture2D SourceTexture
    {
      get => base.SourceTexture;
        set
      {
        base.SourceTexture = value;
        this.method_2();
      }
    }

    public Class40(GraphicsDevice graphicsdevice)
      : base(graphicsdevice)
    {
      this.CurrentTechnique = this.Techniques["HDRBloomBlurHighTechnique"];
      this.effectParameter_13 = this.Parameters["_GaussianCoords"];
      this.effectParameter_14 = this.Parameters["_GaussianWeights"];
      this.BloomDetail = DetailPreference.High;
    }

    private void method_2()
    {
      if (this.interface4_2 == null || this.SourceTexture == null)
        return;
      this.interface4_2.GenerateSampleData(this.SourceTexture.Width, this.SourceTexture.Height, this.detailPreference_1 != DetailPreference.High ? (this.detailPreference_1 != DetailPreference.Medium ? 8 : 16) : 32, ref vector2_1, ref float_1);
      int index1 = 0;
      for (int index2 = 0; index2 < vector4_0.Length; ++index2)
      {
        Vector2 vector2_1 = Class40.vector2_1[index1];
        float[] float1_1 = float_1;
        int index3 = index1;
        int num1 = 1;
        int index4 = index3 + num1;
        float x = float1_1[index3];
        Vector2 vector2_2 = Class40.vector2_1[index4];
        float[] float1_2 = float_1;
        int index5 = index4;
        int num2 = 1;
        index1 = index5 + num2;
        float y = float1_2[index5];
        vector4_0[index2] = new Vector4(vector2_1, vector2_2.X, vector2_2.Y);
        Class40.vector2_2[index2] = new Vector2(x, y);
      }
      if (this.effectParameter_13 != null)
        this.effectParameter_13.SetValue(vector4_0);
      if (this.effectParameter_14 == null)
        return;
      this.effectParameter_14.SetValue(vector2_2);
    }

    protected override void SetTechnique()
    {
      ++this.class46_0.lightingSystemStatistic_0.AccumulationValue;
      if (this.detailPreference_1 == DetailPreference.High)
        this.CurrentTechnique = this.Techniques["HDRBloomBlurHighTechnique"];
      else if (this.detailPreference_1 == DetailPreference.Medium)
        this.CurrentTechnique = this.Techniques["HDRBloomBlurMediumTechnique"];
      else
        this.CurrentTechnique = this.Techniques["HDRBloomBlurLowTechnique"];
    }

    public interface Interface4
    {
      void GenerateSampleData(int texturewidth, int textureheight, int samplecount, ref Vector2[] generateduvs, ref float[] generatedweights);
    }

    public class Class49 : Interface4
    {
      private const float float_0 = 8.5f;

      public virtual void GenerateSampleData(int texturewidth, int textureheight, int samplecount, ref Vector2[] generateduvs, ref float[] generatedweights)
      {
        float texelstride = 1f / texturewidth;
        float gaussianBase = this.GenerateGaussianBase();
        float gaussianExp = this.GenerateGaussianExp();
        for (int index = 0; index < samplecount; ++index)
        {
          generateduvs[index] = new Vector2(this.GenerateUVOffset(index, samplecount, texelstride), 0.0f);
          generatedweights[index] = this.GenerateWeight(index, samplecount, gaussianBase, gaussianExp);
        }
        this.NormalizeKernel(ref generatedweights);
      }

      protected void NormalizeKernel(ref float[] generatedweights)
      {
        float num1 = 0.0f;
        foreach (float num2 in generatedweights)
          num1 += num2;
        if (num1 == 0.0)
          return;
        float num3 = 1f / num1;
        for (int index = 0; index < generatedweights.Length; ++index)
          generatedweights[index] = generatedweights[index] * num3;
      }

      protected float GenerateGaussianBase()
      {
        return 1f / (float) Math.Sqrt(17.0 * Math.PI);
      }

      protected float GenerateGaussianExp()
      {
        return -0.006920415f;
      }

      protected float GenerateUVOffset(int index, int count, float texelstride)
      {
        float num = (float) (count * 0.5 - 0.5);
        return (index - num) * texelstride;
      }

      protected float GenerateWeight(int index, int count, float gaussbase, float gaussexp)
      {
        float num1 = (float) (count * 0.5 - 0.5);
        float num2 = index - num1;
        return gaussbase * (float) Math.Exp(num2 * (double) num2 * gaussexp);
      }
    }

    public class Class50 : Class49
    {
      public override void GenerateSampleData(int texturewidth, int textureheight, int samplecount, ref Vector2[] generateduvs, ref float[] generatedweights)
      {
        float texelstride = 1f / textureheight;
        float gaussianBase = this.GenerateGaussianBase();
        float gaussianExp = this.GenerateGaussianExp();
        for (int index = 0; index < samplecount; ++index)
        {
          generateduvs[index] = new Vector2(0.0f, this.GenerateUVOffset(index, samplecount, texelstride));
          generatedweights[index] = this.GenerateWeight(index, samplecount, gaussianBase, gaussianExp);
        }
        this.NormalizeKernel(ref generatedweights);
      }
    }
  }
}
