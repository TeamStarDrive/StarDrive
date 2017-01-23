// Decompiled with JetBrains decompiler
// Type: SynapseGaming.LightingSystem.Effects.ISamplerEffect
// Assembly: SynapseGaming-SunBurn-Pro, Version=1.3.2.8, Culture=neutral, PublicKeyToken=c23c60523565dbfd
// MVID: A5F03349-72AC-4BAA-AEEE-9AB9B77E0A39
// Assembly location: C:\Projects\BlackBox\StarDrive\SynapseGaming-SunBurn-Pro.dll

namespace SynapseGaming.LightingSystem.Effects
{
  /// <summary>
  /// Interface that provides custom effects with optimized in-shader sampler support.
  /// </summary>
  public interface ISamplerEffect
  {
    /// <summary>
    /// Determines if the effect's shader alters sampler states during execution.
    /// </summary>
    bool AffectsSamplerStates { get; }
  }
}
