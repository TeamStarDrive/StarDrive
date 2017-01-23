// Decompiled with JetBrains decompiler
// Type: SynapseGaming.LightingSystem.Shadows.IShadowSource
// Assembly: SynapseGaming-SunBurn-Pro, Version=1.3.2.8, Culture=neutral, PublicKeyToken=c23c60523565dbfd
// MVID: A5F03349-72AC-4BAA-AEEE-9AB9B77E0A39
// Assembly location: C:\Projects\BlackBox\StarDrive\SynapseGaming-SunBurn-Pro.dll

using Microsoft.Xna.Framework;

namespace SynapseGaming.LightingSystem.Shadows
{
  /// <summary>
  /// Interface that provides all basic source information for all shadow casters.
  /// </summary>
  public interface IShadowSource
  {
    /// <summary>
    /// Defines the type of objects that cast shadows from the light.
    /// Does not affect an object's ability to receive shadows.
    /// </summary>
    ShadowType ShadowType { get; set; }

    /// <summary>Position in world space of the shadow source.</summary>
    Vector3 ShadowPosition { get; }

    /// <summary>Adjusts the visual quality of casts shadows.</summary>
    float ShadowQuality { get; set; }

    /// <summary>Main property used to eliminate shadow artifacts.</summary>
    float ShadowPrimaryBias { get; set; }

    /// <summary>
    /// Additional fine-tuned property used to eliminate shadow artifacts.
    /// </summary>
    float ShadowSecondaryBias { get; set; }

    /// <summary>
    /// Enables independent level-of-detail per cubemap face on point-based lights.
    /// </summary>
    bool ShadowPerSurfaceLOD { get; set; }

    /// <summary>
    /// Requests that all lights contained within the shadow source are rendered in one
    /// pass (this is only a performance hint - support depends on the rendering implementation).
    /// </summary>
    bool ShadowRenderLightsTogether { get; }

    /// <summary>World space transform of the shadow source.</summary>
    Matrix World { get; set; }

    /// <summary>
    /// Returns a hash code that uniquely identifies the shadow source
    /// and its current state.  Changes to ShadowPosition affects the
    /// hash code, which is used to trigger updates on related shadows.
    /// </summary>
    /// <returns>Shadow hash code.</returns>
    int GetShadowSourceHashCode();
  }
}
