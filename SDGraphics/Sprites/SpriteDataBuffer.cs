using System;
using Microsoft.Xna.Framework.Graphics;
using SDGraphics.Rendering;
using SDUtils;

namespace SDGraphics.Sprites;

/// <summary>
/// A fixed-size reusable CPU+GPU buffer
/// </summary>
internal sealed class SpriteDataBuffer : IDisposable
{
    // the maximum # of particles per buffer,
    // this must be smaller than 10922 (which is ushort.MaxValue / 6)
    public const int Size = 4096;

    // A vertex buffer holding the quads. This contains the same data as
    // the quads array, but copied across to where the GPU can access it.
    DynamicVertexBuffer VertexBuffer;

    // all of the quads
    readonly VertexCoordColor[] Quads;
 
    // no more free quads in this buffer
    public bool IsFull => Count == Size;
    public int Count;
    bool Modified;

    public SpriteDataBuffer(GraphicsDevice device)
    {
        Quads = new VertexCoordColor[Size * 4];
        VertexBuffer = new(device, VertexCoordColor.SizeInBytes*Size*4, BufferUsage.WriteOnly);
    }

    ~SpriteDataBuffer()
    {
        Mem.Dispose(ref VertexBuffer);
    }

    public void Dispose()
    {
        Mem.Dispose(ref VertexBuffer);
        GC.SuppressFinalize(this);
    }

    // reuse this buffer
    public void Reset()
    {
        Count = 0;
        Modified = true;
    }

    public bool Add(in SpriteData data)
    {
        if (IsFull)
            return false;

        int vertexOffset = Count * 4;
        ++Count;
        Quads[vertexOffset + 0] = new(data.Quad.A, data.Color, data.Coords.A); // TopLeft
        Quads[vertexOffset + 1] = new(data.Quad.B, data.Color, data.Coords.B); // TopRight
        Quads[vertexOffset + 2] = new(data.Quad.C, data.Color, data.Coords.C); // BotRight
        Quads[vertexOffset + 3] = new(data.Quad.D, data.Color, data.Coords.D); // BotLeft
        Modified = true;
        return true;
    }

    void UploadIfNeeded()
    {
        if (!Modified)
            return;

        DynamicVertexBuffer vbo = VertexBuffer;

        // Restore the vertex buffer contents if the graphics device was lost.
        // Or just send all data if count == Size
        if (vbo.IsContentLost || Count == Size)
        {
            vbo.SetData(Quads);
        }
        else // upload quads to the GPU
        {
            try
            {
                const int stride = VertexCoordColor.SizeInBytes;
                vbo.SetData(0, Quads, 0, Count * 4, stride, SetDataOptions.NoOverwrite);
            }
            catch // if this fails for some reason, just send all data
            {
                vbo.SetData(Quads);
            }
        }

        Modified = false; // upload complete
    }

    public void Draw(SpriteRenderer sr, Texture2D texture, Color color, int startIndex, int drawCount)
    {
        DynamicVertexBuffer vbo = VertexBuffer;
        int count = Count;
        if (count <= 0 || vbo == null || drawCount <= 0)
            return;

        UploadIfNeeded();

        GraphicsDevice device = sr.Device;
        // Set the particle vertex and index buffer.
        device.Vertices[0].SetSource(vbo, 0, VertexCoordColor.SizeInBytes);
        device.Indices = sr.IndexBuf;
        device.VertexDeclaration = sr.VertexDeclaration;

        sr.ShaderBegin(texture, color);
        device.DrawIndexedPrimitives(PrimitiveType.TriangleList, 0,
                                     startIndex * 4, drawCount * 4, // 4 points
                                     startIndex * 6, drawCount * 2); // 2 triangles
        sr.ShaderEnd();
    }
}
