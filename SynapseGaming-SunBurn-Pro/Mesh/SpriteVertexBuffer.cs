using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace Mesh
{
    public struct SpriteVertex
    {
        public static readonly VertexElement[] ElementDescr =
        {
            new VertexElement(0, 0,  VertexElementFormat.Vector3, VertexElementMethod.Default, VertexElementUsage.Position, 0),
            new VertexElement(0, 12, VertexElementFormat.Vector3, VertexElementMethod.Default, VertexElementUsage.Normal, 0),
            new VertexElement(0, 24, VertexElementFormat.Vector2, VertexElementMethod.Default, VertexElementUsage.TextureCoordinate, 0),
            new VertexElement(0, 32, VertexElementFormat.Vector2, VertexElementMethod.Default, VertexElementUsage.Binormal, 0),
            new VertexElement(0, 36, VertexElementFormat.Vector2, VertexElementMethod.Default, VertexElementUsage.Tangent, 0)
        };
        public Vector3 Position;
        public Vector3 Normal;
        public Vector2 Coords;
        public Vector3 PackedBinormalTangent;

        public static readonly int SizeInBytes = 44;
    }

    public sealed class SpriteVertexBuffer : IDisposable
    {
        public BoundingBox ObjectBoundingBox { get; private set; }

        public int VertexCount { get; private set; }
        public IndexBuffer IndexBuffer { get; private set; }
        public VertexBuffer VertexBuffer { get; private set; }
        public VertexDeclaration VertexDeclaration { get; private set; }

        public unsafe void Create(GraphicsDevice device, SpriteVertex[] vertices, ushort[] indices, int faces)
        {
            if (VertexBuffer == null)
            {
                VertexDeclaration = new VertexDeclaration(device, SpriteVertex.ElementDescr);
                VertexBuffer      = new VertexBuffer(device, typeof(SpriteVertex), 4096, BufferUsage.None);
                IndexBuffer       = new IndexBuffer(device, typeof(ushort), 6144, BufferUsage.None);
            }

            BoundingBox bbox;
            fixed (SpriteVertex* pVerts = vertices)
            {
                int length = vertices.Length;
                bbox.Min.X = bbox.Max.X = pVerts->Position.X;
                bbox.Min.Y = bbox.Max.Y = pVerts->Position.Y;
                bbox.Max.Z = 1f;
                bbox.Min.Z = 0.0f;
                SpriteVertex* vert = pVerts + 1;
                for (int i = 1; i < length; ++i, ++vert)
                {
                    if      (vert->Position.X > bbox.Max.X) bbox.Max.X = vert->Position.X;
                    else if (vert->Position.X < bbox.Min.X) bbox.Min.X = vert->Position.X;
                    if      (vert->Position.Y > bbox.Max.Y) bbox.Max.Y = vert->Position.Y;
                    else if (vert->Position.Y < bbox.Min.Y) bbox.Min.Y = vert->Position.Y;
                }
            }

            ObjectBoundingBox = bbox;

            VertexCount = faces * 4;
            VertexBuffer.SetData(vertices, 0, VertexCount);
            IndexBuffer.SetData(indices, 0, faces * 6);
        }

        public void Dispose()
        {
            if (IndexBuffer != null)
            {
                IndexBuffer.Dispose();
                IndexBuffer = null;
            }
            if (VertexBuffer != null)
            {
                VertexBuffer.Dispose();
                VertexBuffer = null;
            }
            if (VertexDeclaration != null)
            {
                VertexDeclaration.Dispose();
                VertexDeclaration = null;
            }
        }
    }
}
