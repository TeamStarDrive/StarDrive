// Decompiled with JetBrains decompiler
// Type: SynapseGaming.LightingSystem.Core.SamplingPreference
// Assembly: SynapseGaming-SunBurn-Pro, Version=1.3.2.8, Culture=neutral, PublicKeyToken=c23c60523565dbfd
// MVID: A5F03349-72AC-4BAA-AEEE-9AB9B77E0A39
// Assembly location: C:\Projects\BlackBox\StarDrive\SynapseGaming-SunBurn-Pro.dll

using System;

namespace SynapseGaming.LightingSystem.Core
{
  /// <summary>
  /// Provides enumerated values for applying user sampling
  /// and performance preferences.
  /// </summary>
  [Serializable]
  public enum SamplingPreference
  {
    Bilinear,
    Trilinear,
    Anisotropic
  }
}
