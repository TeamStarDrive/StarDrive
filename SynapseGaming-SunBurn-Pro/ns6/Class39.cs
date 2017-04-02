// Decompiled with JetBrains decompiler
// Type: ns6.Class39
// Assembly: SynapseGaming-SunBurn-Pro, Version=1.3.2.8, Culture=neutral, PublicKeyToken=c23c60523565dbfd
// MVID: A5F03349-72AC-4BAA-AEEE-9AB9B77E0A39
// Assembly location: C:\Projects\BlackBox\StarDrive\SynapseGaming-SunBurn-Pro.dll

using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using SynapseGaming.LightingSystem.Effects;

namespace ns6
{
  internal class Class39 : BaseRenderableEffect
  {
    private Vector2 vector2_0;
    private Texture2D texture2D_0;
    private EffectParameter effectParameter_11;
    private EffectParameter effectParameter_12;

    public virtual Texture2D SourceTexture
    {
      get
      {
        return this.texture2D_0;
      }
      set
      {
        EffectHelper.smethod_8(value, ref this.texture2D_0, ref this.effectParameter_12);
        if (value == null)
          return;
        EffectHelper.smethod_7(new Vector2(1f / (float) value.Width, 1f / (float) value.Height), ref this.vector2_0, ref this.effectParameter_11);
      }
    }

    public Class39(GraphicsDevice graphicsdevice)
      : base(graphicsdevice, "HighDynamicRange")
    {
      this.CurrentTechnique = this.Techniques["HDRDownSampleTechnique"];
      this.effectParameter_11 = this.Parameters["_SourceTextureStride"];
      this.effectParameter_12 = this.Parameters["_SourceTexture"];
    }

    protected override void SetTechnique()
    {
    }

    protected override Effect Create(GraphicsDevice device)
    {
      return (Effect) new Class39(device);
    }
  }
}
