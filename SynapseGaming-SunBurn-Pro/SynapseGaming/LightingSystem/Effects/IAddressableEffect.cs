// Decompiled with JetBrains decompiler
// Type: SynapseGaming.LightingSystem.Effects.IAddressableEffect
// Assembly: SynapseGaming-SunBurn-Pro, Version=1.3.2.8, Culture=neutral, PublicKeyToken=c23c60523565dbfd
// MVID: A5F03349-72AC-4BAA-AEEE-9AB9B77E0A39
// Assembly location: C:\Projects\BlackBox\StarDrive\SynapseGaming-SunBurn-Pro.dll

using Microsoft.Xna.Framework.Graphics;

namespace SynapseGaming.LightingSystem.Effects
{
  /// <summary>
  /// Interface that provides custom effects with texture addressing support.
  /// </summary>
  public interface IAddressableEffect
  {
    /// <summary>
    /// Determines the effect's texture address mode in the U texture-space direction.
    /// </summary>
    TextureAddressMode AddressModeU { get; set; }

    /// <summary>
    /// Determines the effect's texture address mode in the V texture-space direction.
    /// </summary>
    TextureAddressMode AddressModeV { get; set; }

    /// <summary>
    /// Determines the effect's texture address mode in the W texture-space direction.
    /// </summary>
    TextureAddressMode AddressModeW { get; set; }
  }
}
