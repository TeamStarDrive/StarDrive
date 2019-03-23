// Decompiled with JetBrains decompiler
// Type: SynapseGaming.LightingSystem.Effects.IShadowGenerateEffect
// Assembly: SynapseGaming-SunBurn-Pro, Version=1.3.2.8, Culture=neutral, PublicKeyToken=c23c60523565dbfd
// MVID: A5F03349-72AC-4BAA-AEEE-9AB9B77E0A39
// Assembly location: C:\Projects\BlackBox\StarDrive\SynapseGaming-SunBurn-Pro.dll

using Microsoft.Xna.Framework;

namespace SynapseGaming.LightingSystem.Effects
{
  /// <summary>
  /// Interface that provides custom effects with shadow map generation support.
  /// </summary>
  public interface IShadowGenerateEffect
  {
    /// <summary>
    /// Determines if the effect is capable of generating shadow maps. Objects using effects unable to
    /// generate shadow maps automatically use the built-in shadow effect, however this puts
    /// heavy restrictions on how the effects handle rendering (only basic vertex transforms are supported).
    /// </summary>
    bool SupportsShadowGeneration { get; }

    /// <summary>Main property used to eliminate shadow artifacts.</summary>
    float ShadowPrimaryBias { get; set; }

    /// <summary>
    /// Additional fine-tuned property used to eliminate shadow artifacts.
    /// </summary>
    float ShadowSecondaryBias { get; set; }

    /// <summary>
    /// Bounding area of the shadow source, where the bounds center is the actual shadow source location,
    /// and the radius is either the source radius (for point sources) or the maximum view based casting
    /// distance (for directional sources).
    /// </summary>
    BoundingSphere ShadowArea { set; }

    /// <summary>
    /// Sets the camera view and inverse camera view matrices. These are the matrices used in the final
    /// on-screen render from the camera / player point of view.
    /// 
    /// These matrices will differ from the standard view and inverse view matrices when rendering from
    /// an alternate point of view (for instance during shadow map and cube map generation).
    /// </summary>
    /// <param name="view">Camera view matrix applied to geometry using this effect.</param>
    /// <param name="viewToWorld">Camera inverse view matrix applied to geometry using this effect.</param>
    void SetCameraView(in Matrix view, in Matrix viewToWorld);
  }
}
