// Decompiled with JetBrains decompiler
// Type: SynapseGaming.LightingSystem.Effects.ITerrainEffect
// Assembly: SynapseGaming-SunBurn-Pro, Version=1.3.2.8, Culture=neutral, PublicKeyToken=c23c60523565dbfd
// MVID: A5F03349-72AC-4BAA-AEEE-9AB9B77E0A39
// Assembly location: C:\Projects\BlackBox\StarDrive\SynapseGaming-SunBurn-Pro.dll

using Microsoft.Xna.Framework.Graphics;

namespace SynapseGaming.LightingSystem.Effects
{
  /// <summary>
  /// Interface that provides effects with terrain rendering support.
  /// </summary>
  public interface ITerrainEffect
  {
    /// <summary>
    /// Texture containing height values used to displace a terrain mesh. Also used
    /// for low frequency lighting.
    /// </summary>
    Texture2D HeightMapTexture { get; set; }

    /// <summary>Adjusts the terrain displacement magnitude.</summary>
    float HeightScale { get; set; }

    /// <summary>
    /// Adjusts the number of times the height map tiles across a terrain's
    /// mesh. Similar to uv scale when texture mapping.
    /// </summary>
    float Tiling { get; set; }

    /// <summary>Density or tessellation of the terrain mesh.</summary>
    int MeshSegments { get; set; }
  }
}
