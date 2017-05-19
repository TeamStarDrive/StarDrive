// Decompiled with JetBrains decompiler
// Type: ns9.Class65
// Assembly: SynapseGaming-SunBurn-Pro, Version=1.3.2.8, Culture=neutral, PublicKeyToken=c23c60523565dbfd
// MVID: A5F03349-72AC-4BAA-AEEE-9AB9B77E0A39
// Assembly location: C:\Projects\BlackBox\StarDrive\SynapseGaming-SunBurn-Pro.dll

using Microsoft.Xna.Framework.Graphics;
using SynapseGaming.LightingSystem.Core;

namespace ns9
{
  internal class Class65
  {
    protected Class66 Statistics = new Class66();
    private bool bool_0 = true;
    private TextureAddressMode textureAddressMode_0;
    private TextureAddressMode textureAddressMode_1;
    private TextureAddressMode textureAddressMode_2;
    private TextureFilter textureFilter_0;
    private TextureFilter textureFilter_1;
    private TextureFilter textureFilter_2;
    private float float_0;

    public void method_0()
    {
      this.bool_0 = true;
    }

    public void method_1(GraphicsDevice graphicsDevice_0, TextureAddressMode textureAddressMode_3, TextureAddressMode textureAddressMode_4, TextureAddressMode textureAddressMode_5, TextureFilter textureFilter_3, TextureFilter textureFilter_4, TextureFilter textureFilter_5, float float_1)
    {
      bool flag1 = this.bool_0;
      bool flag2 = this.bool_0;
      if (!flag1)
        flag1 = this.textureAddressMode_0 != textureAddressMode_3 || this.textureAddressMode_1 != textureAddressMode_4;
      if (!flag2)
        flag2 = this.textureFilter_0 != textureFilter_3 || this.textureFilter_1 != textureFilter_4 || this.textureFilter_2 != textureFilter_5 || this.float_0 != (double) float_1;
      this.textureAddressMode_0 = textureAddressMode_3;
      this.textureAddressMode_1 = textureAddressMode_4;
      this.textureAddressMode_2 = textureAddressMode_5;
      if (flag2)
      {
        this.textureFilter_0 = textureFilter_3;
        this.textureFilter_1 = textureFilter_4;
        this.textureFilter_2 = textureFilter_5;
        this.float_0 = float_1;
      }
      for (int index = 0; index < 8; ++index)
      {
        SamplerState samplerState = graphicsDevice_0.SamplerStates[index];
        if (index < 2 || flag1)
        {
          samplerState.AddressU = this.textureAddressMode_0;
          samplerState.AddressV = this.textureAddressMode_1;
        }
        if (flag2)
        {
          samplerState.AddressW = this.textureAddressMode_2;
          samplerState.MagFilter = this.textureFilter_0;
          samplerState.MinFilter = this.textureFilter_1;
          samplerState.MipFilter = this.textureFilter_2;
          samplerState.MipMapLevelOfDetailBias = this.float_0;
        }
      }
      ++this.Statistics.lightingSystemStatistic_0.AccumulationValue;
      if (flag2)
        ++this.Statistics.lightingSystemStatistic_1.AccumulationValue;
      this.bool_0 = false;
    }

    protected class Class66
    {
      public LightingSystemStatistic lightingSystemStatistic_0 = LightingSystemStatistics.GetStatistic("Renderer_BatchSamplerUVChanges", LightingSystemStatisticCategory.Rendering);
      public LightingSystemStatistic lightingSystemStatistic_1 = LightingSystemStatistics.GetStatistic("Renderer_BatchSamplerFullChanges", LightingSystemStatisticCategory.Rendering);
    }
  }
}
