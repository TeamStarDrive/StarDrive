// Decompiled with JetBrains decompiler
// Type: SynapseGaming.LightingSystem.Effects.ISkinnedEffect
// Assembly: SynapseGaming-SunBurn-Pro, Version=1.3.2.8, Culture=neutral, PublicKeyToken=c23c60523565dbfd
// MVID: A5F03349-72AC-4BAA-AEEE-9AB9B77E0A39
// Assembly location: C:\Projects\BlackBox\StarDrive\SynapseGaming-SunBurn-Pro.dll

using Microsoft.Xna.Framework;

namespace SynapseGaming.LightingSystem.Effects
{
  /// <summary>
  /// Interface that provides custom effects with skinned animation support.
  /// </summary>
  public interface ISkinnedEffect
  {
    /// <summary>
    /// Array of bone transforms for the skeleton's current pose. The matrix index is the
    /// same as the bone order used in the model or vertex buffer.
    /// </summary>
    Matrix[] SkinBones { get; set; }

    /// <summary>
    /// Determines if the effect is currently rendering skinned objects.
    /// </summary>
    bool Skinned { get; set; }
  }
}
