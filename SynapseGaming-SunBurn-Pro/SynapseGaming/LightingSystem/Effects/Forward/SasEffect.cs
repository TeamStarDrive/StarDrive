// Decompiled with JetBrains decompiler
// Type: SynapseGaming.LightingSystem.Effects.Forward.SasEffect
// Assembly: SynapseGaming-SunBurn-Pro, Version=1.3.2.8, Culture=neutral, PublicKeyToken=c23c60523565dbfd
// MVID: A5F03349-72AC-4BAA-AEEE-9AB9B77E0A39
// Assembly location: C:\Projects\BlackBox\StarDrive\SynapseGaming-SunBurn-Pro.dll

using Microsoft.Xna.Framework.Graphics;

namespace SynapseGaming.LightingSystem.Effects.Forward
{
  /// <summary>
  /// Effect class with full non-lighting support for, and binding of, FX Standard Annotations and Semantics (SAS).
  /// </summary>
  public class SasEffect : BaseSasEffect
  {
    /// <summary>
    /// Creates a new SasEffect instance from an effect containing an SAS shader
    /// (often loaded through the content pipeline or from disk).
    /// </summary>
    /// <param name="device"></param>
    /// <param name="effect">Source effect containing an SAS shader.</param>
    public SasEffect(GraphicsDevice device, Effect effect)
      : base(device, effect)
    {
    }

    internal SasEffect(GraphicsDevice device, Effect effect_0, bool bool_5)
      : base(device, effect_0, bool_5)
    {
    }

    /// <summary>
    /// Creates a new empty effect of the same class type and using the same effect file as this object.
    /// </summary>
    /// <param name="device"></param>
    /// <returns></returns>
    protected override Effect Create(GraphicsDevice device)
    {
      return new SasEffect(device, this);
    }

    /// <summary>
    /// Creates a new SasEffect instance from an effect containing an SAS shader
    /// (often loaded through the content pipeline or from disk). The returned
    /// effect is of SasLightingEffect type if the shader supports lighting.
    /// </summary>
    /// <param name="graphicsdevice"></param>
    /// <param name="effect">Source effect containing an SAS shader.</param>
    /// <returns></returns>
    public static SasEffect CreateBestSASEffectType(GraphicsDevice graphicsdevice, Effect effect)
    {
      return smethod_0(graphicsdevice, effect, true);
    }

    internal static SasEffect smethod_0(GraphicsDevice graphicsDevice_0, Effect effect_0, bool bool_5)
    {
      SasLightingEffect sasLightingEffect = new SasLightingEffect(graphicsDevice_0, effect_0, bool_5);
      if (sasLightingEffect.MaxLightSources > 0)
        return sasLightingEffect;
      sasLightingEffect.Dispose();
      return new SasEffect(graphicsDevice_0, effect_0, bool_5);
    }
  }
}
