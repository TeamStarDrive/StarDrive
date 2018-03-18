// Decompiled with JetBrains decompiler
// Type: SynapseGaming.LightingSystem.Lights.CompositeLighting
// Assembly: SynapseGaming-SunBurn-Pro, Version=1.3.2.8, Culture=neutral, PublicKeyToken=c23c60523565dbfd
// MVID: A5F03349-72AC-4BAA-AEEE-9AB9B77E0A39
// Assembly location: C:\Projects\BlackBox\StarDrive\SynapseGaming-SunBurn-Pro.dll

using Microsoft.Xna.Framework;

namespace SynapseGaming.LightingSystem.Lights
{
  /// <summary>
  /// Represents approximate lighting packed into a single directional and ambient light for
  /// fast single-pass lighting.
  /// </summary>
  public struct CompositeLighting
  {
    /// <summary>Direction in world space of the light's influence.</summary>
    public Vector3 Direction { get; set; }

    /// <summary>Directional lighting color given off by the light.</summary>
    public Vector3 DiffuseColor { get; set; }

    /// <summary>Ambient lighting color given off by the light.</summary>
    public Vector3 AmbientColor { get; set; }
  }
}
