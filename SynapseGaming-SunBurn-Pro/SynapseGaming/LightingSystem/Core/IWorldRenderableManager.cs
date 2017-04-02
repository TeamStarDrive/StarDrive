// Decompiled with JetBrains decompiler
// Type: SynapseGaming.LightingSystem.Core.IWorldRenderableManager
// Assembly: SynapseGaming-SunBurn-Pro, Version=1.3.2.8, Culture=neutral, PublicKeyToken=c23c60523565dbfd
// MVID: A5F03349-72AC-4BAA-AEEE-9AB9B77E0A39
// Assembly location: C:\Projects\BlackBox\StarDrive\SynapseGaming-SunBurn-Pro.dll

using Microsoft.Xna.Framework;

namespace SynapseGaming.LightingSystem.Core
{
  /// <summary>
  /// Interface used by objects that manage rendering, scene resources, and have a
  /// containment volume. Commonly used for scenegraphs, oct-trees, and BSP-trees.
  /// </summary>
  public interface IWorldRenderableManager : IUnloadable, IManager, IRenderableManager
  {
    /// <summary>The current containment volume for this object.</summary>
    BoundingBox WorldBoundingBox { get; }

    /// <summary>Resizes the tree used to store contained objects.</summary>
    /// <param name="worldboundingbox">The smallest bounding area that completely
    /// contains the scene. Helps the scenegraph build an optimal scene tree.</param>
    /// <param name="worldtreemaxdepth">Maximum depth for entries in the scene tree. Small
    /// scenes with few objects see better performance with shallow trees. Large complex
    /// scenes often need deeper trees.</param>
    void Resize(BoundingBox worldboundingbox, int worldtreemaxdepth);

    /// <summary>Optimizes the tree used to store contained objects.</summary>
    void Optimize();
  }
}
