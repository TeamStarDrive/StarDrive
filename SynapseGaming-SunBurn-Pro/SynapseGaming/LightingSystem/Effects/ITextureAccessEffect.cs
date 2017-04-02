// Decompiled with JetBrains decompiler
// Type: SynapseGaming.LightingSystem.Effects.ITextureAccessEffect
// Assembly: SynapseGaming-SunBurn-Pro, Version=1.3.2.8, Culture=neutral, PublicKeyToken=c23c60523565dbfd
// MVID: A5F03349-72AC-4BAA-AEEE-9AB9B77E0A39
// Assembly location: C:\Projects\BlackBox\StarDrive\SynapseGaming-SunBurn-Pro.dll

using Microsoft.Xna.Framework.Graphics;

namespace SynapseGaming.LightingSystem.Effects
{
  /// <summary>
  /// Interface that provides custom effects with support for iterating through the effect's textures.
  /// </summary>
  public interface ITextureAccessEffect
  {
    /// <summary>Number of textures exposed by the effect.</summary>
    int TextureCount { get; }

    /// <summary>Returns the texture at a specific index.</summary>
    /// <param name="index"></param>
    /// <returns></returns>
    Texture GetTexture(int index);
  }
}
