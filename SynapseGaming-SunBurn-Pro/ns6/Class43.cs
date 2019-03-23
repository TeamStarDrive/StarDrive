// Decompiled with JetBrains decompiler
// Type: ns6.Class43
// Assembly: SynapseGaming-SunBurn-Pro, Version=1.3.2.8, Culture=neutral, PublicKeyToken=c23c60523565dbfd
// MVID: A5F03349-72AC-4BAA-AEEE-9AB9B77E0A39
// Assembly location: C:\Projects\BlackBox\StarDrive\SynapseGaming-SunBurn-Pro.dll

using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using SynapseGaming.LightingSystem.Effects;

namespace EmbeddedResources
{
  internal class Class43 : Class42
  {
    protected Vector4 _BloomAmount_None_Threshold_Burn;
    protected Vector4 _ExposureAmount_TransitionMax_TransitionMin;
    private EffectParameter effectParameter_15;
    private EffectParameter effectParameter_16;

    public float BloomThreshold
    {
      get => this._BloomAmount_None_Threshold_Burn.Z;
        set => this.SetBloomData(this._BloomAmount_None_Threshold_Burn.X, this._BloomAmount_None_Threshold_Burn.Y, value, this._BloomAmount_None_Threshold_Burn.W);
    }

    public float ExposureAmount
    {
      get => this._ExposureAmount_TransitionMax_TransitionMin.X;
        set => this.SetExposureTransitionData(value, this._ExposureAmount_TransitionMax_TransitionMin.Y, this._ExposureAmount_TransitionMax_TransitionMin.Z);
    }

    public float TransitionMaxScale
    {
      get => this._ExposureAmount_TransitionMax_TransitionMin.Y;
        set => this.SetExposureTransitionData(this._ExposureAmount_TransitionMax_TransitionMin.X, value, this._ExposureAmount_TransitionMax_TransitionMin.Z);
    }

    public float TransitionMinScale
    {
      get => this._ExposureAmount_TransitionMax_TransitionMin.Z;
        set => this.SetExposureTransitionData(this._ExposureAmount_TransitionMax_TransitionMin.X, this._ExposureAmount_TransitionMax_TransitionMin.Y, value);
    }

    public Class43(GraphicsDevice graphicsdevice)
      : base(graphicsdevice)
    {
      this.CurrentTechnique = this.Techniques["HDRDownSampleBloomTechnique"];
      this.effectParameter_15 = this.Parameters["_BloomAmount_None_Threshold_Burn"];
      this.effectParameter_16 = this.Parameters["_ExposureAmount_TransitionMax_TransitionMin"];
    }

    protected void SetBloomData(float sharp, float soft, float threshold, float colorburn)
    {
      EffectHelper.Update(new Vector4(sharp, soft, threshold, colorburn), ref this._BloomAmount_None_Threshold_Burn, ref this.effectParameter_15);
    }

    protected void SetExposureTransitionData(float exposure, float maxscale, float minscale)
    {
      EffectHelper.Update(new Vector4(exposure, maxscale, minscale, 0.0f), ref this._ExposureAmount_TransitionMax_TransitionMin, ref this.effectParameter_16);
    }
  }
}
