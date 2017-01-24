// Decompiled with JetBrains decompiler
// Type: ns6.Class44
// Assembly: SynapseGaming-SunBurn-Pro, Version=1.3.2.8, Culture=neutral, PublicKeyToken=c23c60523565dbfd
// MVID: A5F03349-72AC-4BAA-AEEE-9AB9B77E0A39
// Assembly location: C:\Projects\BlackBox\StarDrive\SynapseGaming-SunBurn-Pro.dll

using Microsoft.Xna.Framework.Graphics;
using SynapseGaming.LightingSystem.Effects;

namespace ns6
{
  internal class Class44 : Class43
  {
    private Texture2D texture2D_2;
    private EffectParameter effectParameter_17;

    public Texture2D BloomTexture
    {
      get
      {
        return this.texture2D_2;
      }
      set
      {
        EffectHelper.smethod_8(value, ref this.texture2D_2, ref this.effectParameter_17);
      }
    }

    public float BloomAmount
    {
      get
      {
        return this._BloomAmount_None_Threshold_Burn.X;
      }
      set
      {
        this.SetBloomData(value, this._BloomAmount_None_Threshold_Burn.Y, this._BloomAmount_None_Threshold_Burn.Z, this._BloomAmount_None_Threshold_Burn.W);
      }
    }

    public Class44(GraphicsDevice graphicsdevice)
      : base(graphicsdevice)
    {
      this.CurrentTechnique = this.Techniques["HDRBlendTechnique"];
      this.effectParameter_17 = this.Parameters["_BloomTexture"];
      this.BloomAmount = 2f;
    }
  }
}
