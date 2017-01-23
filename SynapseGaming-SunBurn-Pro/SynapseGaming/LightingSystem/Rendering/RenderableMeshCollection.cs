// Decompiled with JetBrains decompiler
// Type: SynapseGaming.LightingSystem.Rendering.RenderableMeshCollection
// Assembly: SynapseGaming-SunBurn-Pro, Version=1.3.2.8, Culture=neutral, PublicKeyToken=c23c60523565dbfd
// MVID: A5F03349-72AC-4BAA-AEEE-9AB9B77E0A39
// Assembly location: C:\Projects\BlackBox\StarDrive\SynapseGaming-SunBurn-Pro.dll

using System.Collections.Generic;
using System.Collections.ObjectModel;

namespace SynapseGaming.LightingSystem.Rendering
{
  /// <summary>Represents a collection of RenderableMesh objects.</summary>
  public class RenderableMeshCollection : ReadOnlyCollection<RenderableMesh>
  {
    /// <summary>Creates a new RenderableMeshCollection instance.</summary>
    /// <param name="meshes">Source mesh list.</param>
    public RenderableMeshCollection(IList<RenderableMesh> meshes)
      : base(meshes)
    {
    }
  }
}
