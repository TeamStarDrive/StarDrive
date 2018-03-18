// Decompiled with JetBrains decompiler
// Type: SynapseGaming.LightingSystem.Lights.IAmbientSource
// Assembly: SynapseGaming-SunBurn-Pro, Version=1.3.2.8, Culture=neutral, PublicKeyToken=c23c60523565dbfd
// MVID: A5F03349-72AC-4BAA-AEEE-9AB9B77E0A39
// Assembly location: C:\Projects\BlackBox\StarDrive\SynapseGaming-SunBurn-Pro.dll

namespace SynapseGaming.LightingSystem.Lights
{
  /// <summary>Interface that provides ambient lighting information.</summary>
  public interface IAmbientSource
  {
    /// <summary>
    /// Increases the detail of normal mapped surfaces during the ambient lighting pass (deferred rendering only).
    /// </summary>
    float Depth { get; }
  }
}
