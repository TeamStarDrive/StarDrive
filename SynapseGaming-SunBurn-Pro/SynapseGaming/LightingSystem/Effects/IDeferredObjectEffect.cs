// Decompiled with JetBrains decompiler
// Type: SynapseGaming.LightingSystem.Effects.IDeferredObjectEffect
// Assembly: SynapseGaming-SunBurn-Pro, Version=1.3.2.8, Culture=neutral, PublicKeyToken=c23c60523565dbfd
// MVID: A5F03349-72AC-4BAA-AEEE-9AB9B77E0A39
// Assembly location: C:\Projects\BlackBox\StarDrive\SynapseGaming-SunBurn-Pro.dll

using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using SynapseGaming.LightingSystem.Core;
using SynapseGaming.LightingSystem.Lights;

namespace SynapseGaming.LightingSystem.Effects
{
  /// <summary>
  /// Interface that provides custom effects with deferred object rendering support.
  /// </summary>
  public interface IDeferredObjectEffect
  {
    /// <summary>
    /// Determines the type of shader output for the effects to generate.
    /// </summary>
    DeferredEffectOutput DeferredEffectOutput { get; set; }

    /// <summary>
    /// Texture containing the screen-space lighting generated during deferred rendering (used during the Final rendering pass).
    /// </summary>
    Texture2D SceneLightingDiffuseMap { get; set; }

    /// <summary>
    /// Texture containing the screen-space specular generated during deferred rendering (used during the Final rendering pass).
    /// </summary>
    Texture2D SceneLightingSpecularMap { get; set; }

    /// <summary>
    /// Applies the user's effect preference. This generally trades detail
    /// for performance based on the user's selection.
    /// </summary>
    DetailPreference EffectDetail { get; set; }

    /// <summary>Enables scene fog.</summary>
    bool FogEnabled { get; set; }

    /// <summary>
    /// Distance from the camera in world space that fog begins.
    /// </summary>
    float FogStartDistance { get; set; }

    /// <summary>
    /// Distance from the camera in world space that fog ends.
    /// </summary>
    float FogEndDistance { get; set; }

    /// <summary>Color applied to scene fog.</summary>
    Vector3 FogColor { get; set; }

    /// <summary>
    /// Sets scene ambient lighting (used during the Final rendering pass).
    /// </summary>
    void SetAmbientLighting(IAmbientSource light, Vector3 directionHint);
  }
}
