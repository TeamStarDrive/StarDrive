// Decompiled with JetBrains decompiler
// Type: SynapseGaming.LightingSystem.Lights.IPointSource
// Assembly: SynapseGaming-SunBurn-Pro, Version=1.3.2.8, Culture=neutral, PublicKeyToken=c23c60523565dbfd
// MVID: A5F03349-72AC-4BAA-AEEE-9AB9B77E0A39
// Assembly location: C:\Projects\BlackBox\StarDrive\SynapseGaming-SunBurn-Pro.dll

using Microsoft.Xna.Framework;

namespace SynapseGaming.LightingSystem.Lights
{
  /// <summary>Interface that provides point light information.</summary>
  public interface IPointSource
  {
    /// <summary>Position in world space of the light.</summary>
    Vector3 Position { get; set; }

    /// <summary>
    /// Maximum distance in world space of the light's influence.
    /// </summary>
    float Radius { get; set; }
  }
}
