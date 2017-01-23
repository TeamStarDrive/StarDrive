// Decompiled with JetBrains decompiler
// Type: SynapseGaming.LightingSystem.Core.ILightingSystemPreferences
// Assembly: SynapseGaming-SunBurn-Pro, Version=1.3.2.8, Culture=neutral, PublicKeyToken=c23c60523565dbfd
// MVID: A5F03349-72AC-4BAA-AEEE-9AB9B77E0A39
// Assembly location: C:\Projects\BlackBox\StarDrive\SynapseGaming-SunBurn-Pro.dll

namespace SynapseGaming.LightingSystem.Core
{
  /// <summary>
  /// Interface that provides a base for lighting system user preferences.
  /// </summary>
  public interface ILightingSystemPreferences
  {
    /// <summary>
    /// Sets the user preferred balance of texture sampling quality and performance.
    /// </summary>
    SamplingPreference TextureSampling { get; }

    /// <summary>
    /// Sets the user preferred balance of texture resolution and performance.
    /// </summary>
    DetailPreference TextureQuality { get; }

    /// <summary>
    /// Sets the maximum anisotropy level when TextureSampling is set to Anisotropic.
    /// </summary>
    int MaxAnisotropy { get; }

    /// <summary>
    /// Sets the user preferred balance of shadow filtering quality and performance.
    /// </summary>
    DetailPreference ShadowDetail { get; }

    /// <summary>
    /// Sets the user preferred balance of shadow resolution and performance.
    /// </summary>
    float ShadowQuality { get; }

    /// <summary>
    /// Sets the user preferred balance of LightingEffect detail and performance.
    /// </summary>
    DetailPreference EffectDetail { get; }

    /// <summary>
    /// Sets the user preferred balance of post-processing effect detail and performance.
    /// </summary>
    DetailPreference PostProcessingDetail { get; }
  }
}
