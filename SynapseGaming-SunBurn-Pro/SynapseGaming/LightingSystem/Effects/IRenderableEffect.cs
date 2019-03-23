// Decompiled with JetBrains decompiler
// Type: SynapseGaming.LightingSystem.Effects.IRenderableEffect
// Assembly: SynapseGaming-SunBurn-Pro, Version=1.3.2.8, Culture=neutral, PublicKeyToken=c23c60523565dbfd
// MVID: A5F03349-72AC-4BAA-AEEE-9AB9B77E0A39
// Assembly location: C:\Projects\BlackBox\StarDrive\SynapseGaming-SunBurn-Pro.dll

using Microsoft.Xna.Framework;
using SynapseGaming.LightingSystem.Core;

namespace SynapseGaming.LightingSystem.Effects
{
  /// <summary>
  /// Interface that provides custom effects with basic RenderManager compatibility.
  /// </summary>
  public interface IRenderableEffect
  {
    /// <summary>World matrix applied to geometry using this effect.</summary>
    Matrix World { get; set; }

    /// <summary>View matrix applied to geometry using this effect.</summary>
    Matrix View { get; set; }

    /// <summary>
    /// Projection matrix applied to geometry using this effect.
    /// </summary>
    Matrix Projection { get; set; }

    /// <summary>
    /// Determines if the effect's vertex transform differs from the built-in
    /// effects, this will cause z-fighting that must be accounted for. If
    /// the value is false (meaning it varies and is different from the built-in
    /// effects) a depth adjustment technique like depth-offset needs to be applied.
    /// </summary>
    bool Invariant { get; }

    /// <summary>
    /// Surfaces rendered with the effect should be visible from both sides.
    /// </summary>
    bool DoubleSided { get; set; }

    /// <summary>
    /// Applies the user's effect preference. This generally trades detail
    /// for performance based on the user's selection.
    /// </summary>
    DetailPreference EffectDetail { get; set; }

    /// <summary>
    /// Sets both the world and inverse world matrices.  Used to improve
    /// performance in effects that automatically generate an inverse
    /// world matrix when the world matrix is set, by providing a cached
    /// or precalculated inverse matrix with the world matrix.
    /// </summary>
    /// <param name="world">World matrix applied to geometry using this effect.</param>
    /// <param name="worldtoobj">Inverse world matrix applied to geometry using this effect.</param>
    void SetWorldAndWorldToObject(Matrix world, in Matrix worldtoobj);

    /// <summary>
    /// Used internally by SunBurn - not recommended for external use.
    /// 
    /// Quickly sets the world and inverse world matrices during an effect
    /// Begin / End block.  Values applied using this method do not persist
    /// after the Begin / End block.
    /// 
    /// This method is highly context sensitive.  Built-in effects that derive from
    /// BaseRenderableEffect fully support this method, however other effects merely
    /// call the non-transposed overload.
    /// </summary>
    /// <param name="world">World matrix applied to geometry using this effect.</param>
    /// <param name="worldtranspose">Transposed world matrix applied to geometry using this effect.</param>
    /// <param name="worldtoobj">Inverse world matrix applied to geometry using this effect.</param>
    /// <param name="worldtoobjtranspose">Transposed inverse world matrix applied to geometry using this effect.</param>
    void SetWorldAndWorldToObject(ref Matrix world, ref Matrix worldtranspose, ref Matrix worldtoobj, ref Matrix worldtoobjtranspose);

    /// <summary>
    /// Sets both the view, projection, and their inverse matrices.  Used to improve
    /// performance in effects that automatically generate an inverse
    /// matrix when the view and project are set, by providing a cached
    /// or precalculated inverse matrix with the view and project matrices.
    /// </summary>
    /// <param name="view">View matrix applied to geometry using this effect.</param>
    /// <param name="viewtoworld">Inverse view matrix applied to geometry using this effect.</param>
    /// <param name="projection">Projection matrix applied to geometry using this effect.</param>
    /// <param name="projectiontoview">Inverse projection matrix applied to geometry using this effect.</param>
    void SetViewAndProjection(Matrix view, Matrix viewtoworld, Matrix projection, Matrix projectiontoview);
  }
}
