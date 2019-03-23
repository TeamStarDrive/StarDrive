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
        VertexDeclaration VertexDeclr;
        VertexBuffer VertexBuf;
        IndexBuffer IndexBuf;
        Effect ShaderEffect;

        public string Name { get; set; }

        /// <summary>Object space transform of the mesh.</summary>
        public Matrix MeshToObject { get; set; } = Matrix.Identity;

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
            get => VertexDeclr;
            set => VertexDeclr = value;
        }

        /// <summary>VertexBuffer that contains the mesh geometry.</summary>
        public VertexBuffer VertexBuffer
        {
            get => VertexBuf;
            set => VertexBuf = value;
        }

        /// <summary>IndexBuffer that contains the mesh geometry.</summary>
        public IndexBuffer IndexBuffer
        {
            get => IndexBuf;
            set => IndexBuf = value;
        }

        /// <summary>Effect applied to the mesh during rendering.</summary>
        public Effect Effect
        {
            get => ShaderEffect;
            set => ShaderEffect = value;
        }

        /// <summary>Releases resources allocated by this object.</summary>
        public void Dispose()
        {
            Disposable.Free(ref VertexDeclr);
            Disposable.Free(ref VertexBuf);
            Disposable.Free(ref IndexBuf);
            Disposable.Free(ref ShaderEffect);
        }
    }
}
