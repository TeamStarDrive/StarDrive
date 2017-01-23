// Decompiled with JetBrains decompiler
// Type: SynapseGaming.LightingSystem.Effects.Forward.XSIEffect
// Assembly: SynapseGaming-SunBurn-Pro, Version=1.3.2.8, Culture=neutral, PublicKeyToken=c23c60523565dbfd
// MVID: A5F03349-72AC-4BAA-AEEE-9AB9B77E0A39
// Assembly location: C:\Projects\BlackBox\StarDrive\SynapseGaming-SunBurn-Pro.dll

using Microsoft.Xna.Framework.Graphics;
using System;

namespace SynapseGaming.LightingSystem.Effects.Forward
{
  /// <summary>Provides support for XSI shaders.</summary>
  public class XSIEffect : SasLightingEffect
  {
    /// <summary>
    /// Creates a new XSIEffect instance from an effect containing an XSI shader
    /// (often loaded through the content pipeline or from disk).
    /// </summary>
    /// <param name="graphicsdevice"></param>
    /// <param name="effect">Source effect containing an XSI shader.</param>
    public XSIEffect(GraphicsDevice graphicsdevice, Effect effect)
      : base(graphicsdevice, effect, false)
    {
      this.BindBySasAddress(this.FindByName("AmbientColor"), BaseSasBindEffect.SASAddress_AmbientLight_Color[0]);
      this.SkinBonesEffectParameter = this.FindByName("Bones");
      int length = BaseSasBindEffect.SASAddress_PointLight_Position.Length;
      for (int index = 0; index < length; ++index)
      {
        this.BindBySasAddress(this.FindBySasAddress("Sas.PointLights[" + (object) index + "].Position"), BaseSasBindEffect.SASAddress_PointLight_Position[index]);
        this.BindBySasAddress(this.FindBySasAddress("Sas.PointLights[" + (object) index + "].Color"), BaseSasBindEffect.SASAddress_PointLight_Color[index]);
      }
      this.FindMaxLightCount();
      this.SetMaxLightCount(Math.Min(this.MaxLightSources, 3));
    }

    /// <summary>
    /// Sets the effect technique based on its current property values.
    /// </summary>
    protected override void SetTechnique()
    {
      ++this.class47_0.lightingSystemStatistic_0.AccumulationValue;
      if (this.Skinned)
        this.SetTechnique("Skinned");
      else
        this.SetTechnique("Static");
    }
  }
}
