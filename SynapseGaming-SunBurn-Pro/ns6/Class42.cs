// Decompiled with JetBrains decompiler
// Type: ns6.Class42
// Assembly: SynapseGaming-SunBurn-Pro, Version=1.3.2.8, Culture=neutral, PublicKeyToken=c23c60523565dbfd
// MVID: A5F03349-72AC-4BAA-AEEE-9AB9B77E0A39
// Assembly location: C:\Projects\BlackBox\StarDrive\SynapseGaming-SunBurn-Pro.dll

using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using SynapseGaming.LightingSystem.Effects;

namespace EmbeddedResources
{
  internal class Class42 : Class39
  {
    private float float_1;
    private Texture2D texture2D_1;
    private EffectParameter effectParameter_13;
    private EffectParameter effectParameter_14;

    public float IntensityBlend
    {
      get => this.float_1;
        set
      {
        if (this.effectParameter_13 == null || value == (double) this.float_1)
          return;
        this.float_1 = value;
        this.effectParameter_13.SetValue(MathHelper.Clamp(this.float_1, 0.0f, 1f));
      }
    }

    public Texture2D IntensityTexture
    {
      get => this.texture2D_1;
        set => EffectHelper.Update(value, ref this.texture2D_1, this.effectParameter_14);
    }

    public Class42(GraphicsDevice graphicsdevice)
      : base(graphicsdevice)
    {
      this.CurrentTechnique = this.Techniques["HDRDownSampleBlendIntensityTechnique"];
      this.effectParameter_13 = this.Parameters["_IntensityBlend"];
      this.effectParameter_14 = this.Parameters["_IntensityTexture"];
    }
  }
}
