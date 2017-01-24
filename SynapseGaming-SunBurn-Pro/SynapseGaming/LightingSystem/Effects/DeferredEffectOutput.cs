// Decompiled with JetBrains decompiler
// Type: SynapseGaming.LightingSystem.Effects.DeferredEffectOutput
// Assembly: SynapseGaming-SunBurn-Pro, Version=1.3.2.8, Culture=neutral, PublicKeyToken=c23c60523565dbfd
// MVID: A5F03349-72AC-4BAA-AEEE-9AB9B77E0A39
// Assembly location: C:\Projects\BlackBox\StarDrive\SynapseGaming-SunBurn-Pro.dll

namespace SynapseGaming.LightingSystem.Effects
{
  /// <summary>
  /// Determines the type of shader output deferred object rendering effects will generate.
  /// </summary>
  public enum DeferredEffectOutput
  {
    Depth,
    GBuffer,
    ShadowDepth,
    Final,
  }
}
