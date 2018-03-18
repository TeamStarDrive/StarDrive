// Decompiled with JetBrains decompiler
// Type: SynapseGaming.LightingSystem.Lights.ILight
// Assembly: SynapseGaming-SunBurn-Pro, Version=1.3.2.8, Culture=neutral, PublicKeyToken=c23c60523565dbfd
// MVID: A5F03349-72AC-4BAA-AEEE-9AB9B77E0A39
// Assembly location: C:\Projects\BlackBox\StarDrive\SynapseGaming-SunBurn-Pro.dll

using Microsoft.Xna.Framework;
using SynapseGaming.LightingSystem.Core;
using SynapseGaming.LightingSystem.Shadows;

namespace SynapseGaming.LightingSystem.Lights
{
  /// <summary>
  /// Interface that provides basic lighting information for all lights.
  /// </summary>
  public interface ILight : IMovableObject
  {
    /// <summary>
    /// Turns illumination on and off without removing the light from the scene.
    /// </summary>
    bool Enabled { get; set; }

    /// <summary>Direct lighting color given off by the light.</summary>
    Vector3 DiffuseColor { get; set; }

    /// <summary>Intensity of the light.</summary>
    float Intensity { get; set; }

    /// <summary>
    /// Provides softer indirect-like illumination without "hot-spots".
    /// </summary>
    bool FillLight { get; set; }

    /// <summary>
    /// Controls how quickly lighting falls off over distance (only available in deferred rendering).
    /// Value ranges from 0.0f to 1.0f.
    /// </summary>
    float FalloffStrength { get; set; }

    /// <summary>
    /// The combined light color and intensity (provided for convenience).
    /// </summary>
    Vector3 CompositeColorAndIntensity { get; }

    /// <summary>
    /// Shadow source the light's shadows are generated from.
    /// Allows sharing shadows between point light sources.
    /// </summary>
    IShadowSource ShadowSource { get; set; }

    /// <summary>Bounding area of the light's influence.</summary>
    BoundingSphere WorldBoundingSphere { get; }
  }
}
