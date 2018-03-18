// Decompiled with JetBrains decompiler
// Type: SynapseGaming.LightingSystem.Shadows.IShadowMapVisibility
// Assembly: SynapseGaming-SunBurn-Pro, Version=1.3.2.8, Culture=neutral, PublicKeyToken=c23c60523565dbfd
// MVID: A5F03349-72AC-4BAA-AEEE-9AB9B77E0A39
// Assembly location: C:\Projects\BlackBox\StarDrive\SynapseGaming-SunBurn-Pro.dll

namespace SynapseGaming.LightingSystem.Shadows
{
  /// <summary>
  /// Interface that provides support for determining shadow visibility and level-of-detail information.
  /// </summary>
  public interface IShadowMapVisibility
  {
    /// <summary>
    /// Determines the transition range of each shadow level-of-detail. The range is normalized relative
    /// to the environment ShadowFadeEndDistance, for instance a value of 1.0 transitions at the
    /// ShadowFadeEndDistance whereas a value of 0.25 transitions at (ShadowFadeEndDistance * 0.25).
    /// Index 0 is the highest level of detail.
    /// </summary>
    float[] ShadowLODRangeHints { get; }
  }
}
