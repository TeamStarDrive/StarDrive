// Decompiled with JetBrains decompiler
// Type: SynapseGaming.LightingSystem.Rendering.MeshData
// Assembly: SynapseGaming-SunBurn-Pro, Version=1.3.2.8, Culture=neutral, PublicKeyToken=c23c60523565dbfd
// MVID: A5F03349-72AC-4BAA-AEEE-9AB9B77E0A39
// Assembly location: C:\Projects\BlackBox\StarDrive\SynapseGaming-SunBurn-Pro.dll

using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using ns11;

namespace SynapseGaming.LightingSystem.Rendering
{
  /// <summary>
  /// Represents geometry data that can be shared between multiple
  /// scene objects (similar to xna Model).
  /// 
  /// Generally loaded through the xna content manager.
  /// </summary>
  public class MeshData : IDisposable
  {
    private VertexDeclaration vertexDeclaration_0;
    private VertexBuffer vertexBuffer_0;
    private IndexBuffer indexBuffer_0;
    private Effect effect_0;

    /// <summary>Object space transform of the mesh.</summary>
    public Matrix MeshToObject { get; set; }

    /// <summary>
    /// Indicates the object bounding area spans the entire world and
    /// the object is always visible.
    /// </summary>
    public bool InfiniteBounds { get; set; }

    /// <summary>Number of primitives in the mesh geometry.</summary>
    public int PrimitiveCount { get; set; }

    /// <summary>
    /// Number of vertices in the vertex buffer range required to draw the mesh.
    /// For instance, a quad rendering vertices at indices (2, 5, 6, 9) requires
    /// a vertex buffer range of 8 vertices (vertices 2 – 9 inclusive).
    /// </summary>
    public int VertexCount { get; set; }

    /// <summary>Size in bytes of the elements in the vertex buffer.</summary>
    public int VertexStride { get; set; }

    /// <summary>
    /// Object-space bounding area that completely contains the mesh.
    /// </summary>
    public BoundingSphere ObjectSpaceBoundingSphere { get; set; }

    /// <summary>Describes the mesh vertex buffer contents.</summary>
    public VertexDeclaration VertexDeclaration
    {
      get => this.vertexDeclaration_0;
        set => this.vertexDeclaration_0 = value;
    }

    /// <summary>VertexBuffer that contains the mesh geometry.</summary>
    public VertexBuffer VertexBuffer
    {
      get => this.vertexBuffer_0;
        set => this.vertexBuffer_0 = value;
    }

    /// <summary>IndexBuffer that contains the mesh geometry.</summary>
    public IndexBuffer IndexBuffer
    {
      get => this.indexBuffer_0;
        set => this.indexBuffer_0 = value;
    }

    /// <summary>Effect applied to the mesh during rendering.</summary>
    public Effect Effect
    {
      get => this.effect_0;
        set => this.effect_0 = value;
    }

    /// <summary>Releases resources allocated by this object.</summary>
    public void Dispose()
    {
      Disposable.Free(ref this.vertexDeclaration_0);
      Disposable.Free(ref this.vertexBuffer_0);
      Disposable.Free(ref this.indexBuffer_0);
      Disposable.Free(ref this.effect_0);
    }
  }
}
