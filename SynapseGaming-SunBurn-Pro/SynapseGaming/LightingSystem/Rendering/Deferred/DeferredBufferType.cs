// Decompiled with JetBrains decompiler
// Type: SynapseGaming.LightingSystem.Rendering.Deferred.DeferredBufferType
// Assembly: SynapseGaming-SunBurn-Pro, Version=1.3.2.8, Culture=neutral, PublicKeyToken=c23c60523565dbfd
// MVID: A5F03349-72AC-4BAA-AEEE-9AB9B77E0A39
// Assembly location: C:\Projects\BlackBox\StarDrive\SynapseGaming-SunBurn-Pro.dll

namespace SynapseGaming.LightingSystem.Rendering.Deferred
{
  /// <summary>
  /// Provides a list of both common and auxiliary buffer used for deferred rendering.
  /// </summary>
  public enum DeferredBufferType
  {
    DepthAndSpecularPower,
    NormalViewSpaceAndSpecular,
    LightingDiffuse,
    LightingSpecular,
    None,
  }
}
