using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework.Graphics;
using SDUtils;

namespace SDGraphics.Rendering;

/// <summary>
/// An abstraction for storing multiple Quads in VertexBuffer + IndexBuffer
/// QuadBuffer.Builder can be used to generate a buffer.
///
/// Once a buffer is generated, it's not efficient to add new elements,
/// but it is relatively simple to edit existing Quads.
/// </summary>
public class QuadBuffer : IDisposable
{
    VertexBuffer VBO;
    IndexBuffer IBO;
    VertexDeclaration VD;
    public readonly int NumQuads;

    public class Builder
    {
        readonly List<VertexCoordColor> Vertices;

        public Builder(int capacity = 256)
        {
            Vertices = new List<VertexCoordColor>(capacity);
        }

        public void Add(in Quad3D quad, in Quad2D coords, Color color)
        {
            Vertices.Add(new VertexCoordColor(quad.A, color, coords.A));
            Vertices.Add(new VertexCoordColor(quad.B, color, coords.B));
            Vertices.Add(new VertexCoordColor(quad.C, color, coords.C));
            Vertices.Add(new VertexCoordColor(quad.D, color, coords.D));
        }

        public QuadBuffer ToBuffer(GraphicsDevice device)
        {
            int numVertices = Vertices.Count;
            int numQuads = numVertices / 4;

            VertexCoordColor[] vertices = Vertices.ToArray();

            if (numVertices <= ushort.MaxValue)
            {
                ushort[] indices = new ushort[numQuads * 6];

                for (int quadIdx = 0; quadIdx < numQuads; ++quadIdx)
                {
                    int vertexOffset = quadIdx * 4;
                    int indexOffset  = quadIdx * 6;
                    indices[indexOffset + 0] = (ushort)(vertexOffset + 0);
                    indices[indexOffset + 1] = (ushort)(vertexOffset + 1);
                    indices[indexOffset + 2] = (ushort)(vertexOffset + 2);
                    indices[indexOffset + 3] = (ushort)(vertexOffset + 0);
                    indices[indexOffset + 4] = (ushort)(vertexOffset + 2);
                    indices[indexOffset + 5] = (ushort)(vertexOffset + 3);
                }

                return new QuadBuffer(device, vertices, indices);
            }
            else
            {
                uint[] indices = new uint[numQuads * 6];

                for (int quadIdx = 0; quadIdx < numQuads; ++quadIdx)
                {
                    int vertexOffset = quadIdx * 4;
                    int indexOffset  = quadIdx * 6;
                    indices[indexOffset + 0] = (uint)(vertexOffset + 0);
                    indices[indexOffset + 1] = (uint)(vertexOffset + 1);
                    indices[indexOffset + 2] = (uint)(vertexOffset + 2);
                    indices[indexOffset + 3] = (uint)(vertexOffset + 0);
                    indices[indexOffset + 4] = (uint)(vertexOffset + 2);
                    indices[indexOffset + 5] = (uint)(vertexOffset + 3);
                }

                return new QuadBuffer(device, vertices, indices);
            }
        }
    }

    public QuadBuffer(GraphicsDevice device, VertexCoordColor[] vertices, ushort[] indices)
    {
        NumQuads = vertices.Length / 4;
        Create(device, vertices, indices);
    }

    public QuadBuffer(GraphicsDevice device, VertexCoordColor[] vertices, uint[] indices)
    {
        NumQuads = vertices.Length / 4;
        Create(device, vertices, indices);
    }

    void Create<T>(GraphicsDevice device, VertexCoordColor[] vertices, T[] indices) where T : struct
    {
        VD = new VertexDeclaration(device, VertexCoordColor.VertexElements);
        VBO = new VertexBuffer(device, typeof(VertexCoordColor), vertices.Length, BufferUsage.WriteOnly);
        IBO = new IndexBuffer(device, typeof(T), indices.Length, BufferUsage.WriteOnly);
        VBO.SetData(vertices);
        IBO.SetData(indices);
    }

    public void SetQuad(int index, in Quad3D quad, in Quad2D coords, Color color)
    {
        if (index >= (uint)NumQuads)
            throw new IndexOutOfRangeException($"QuadBuffer index={index} out of range={NumQuads}");

        var vertices = new VertexCoordColor[]
        {
            new(quad.A, color, coords.A),
            new(quad.B, color, coords.B),
            new(quad.C, color, coords.C),
            new(quad.D, color, coords.D)
        };
        VBO.SetData(vertices, index*4, 4);
    }

    void SetVertexSource(GraphicsDevice device)
    {
        // Set the vertex and index buffers
        device.Vertices[0].SetSource(VBO, 0, VertexCoordColor.SizeInBytes);
        device.Indices = IBO;
        device.VertexDeclaration = VD;
    }

    public void Draw(GraphicsDevice device)
    {
        SetVertexSource(device);
        // draw all indexed primitives
        device.DrawIndexedPrimitives(PrimitiveType.TriangleList, 0,
                                     0, NumQuads * 4,
                                     0, NumQuads * 2);
    }

    public void Draw(GraphicsDevice device, int startIndex, int numQuads)
    {
        if (startIndex > NumQuads)
            return; // just don't render anything

        // if we try to draw more than possible, clamp the excess
        if ((startIndex + numQuads) > NumQuads)
            numQuads = NumQuads - startIndex;

        SetVertexSource(device);
        device.DrawIndexedPrimitives(PrimitiveType.TriangleList, 0,
                                     startIndex * 4, numQuads * 4,
                                     startIndex * 6, numQuads * 2);
    }

    ~QuadBuffer() { Destroy(); }

    void Destroy()
    {
        Mem.Dispose(ref VBO);
        Mem.Dispose(ref IBO);
        Mem.Dispose(ref VD);
    }

    public void Dispose()
    {
        Destroy();
        GC.SuppressFinalize(this);
    }
}