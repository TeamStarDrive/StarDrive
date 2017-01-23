// Decompiled with JetBrains decompiler
// Type: SynapseGaming.LightingSystem.Lights.ISpotSource
// Assembly: SynapseGaming-SunBurn-Pro, Version=1.3.2.8, Culture=neutral, PublicKeyToken=c23c60523565dbfd
// MVID: A5F03349-72AC-4BAA-AEEE-9AB9B77E0A39
// Assembly location: C:\Projects\BlackBox\StarDrive\SynapseGaming-SunBurn-Pro.dll

namespace SynapseGaming.LightingSystem.Lights
{
  /// <summary>
  /// Interface that provides spotlight lighting information.
  /// </summary>
  public interface ISpotSource : IPointSource, IDirectionalSource
  {
    /// <summary>Angle in degrees of the light's influence.</summary>
    float Angle { get; set; }

    /// <summary>Intensity of the light's 3D light beam.</summary>
    float Volume { get; set; }
  }
}
