using System;
using Microsoft.Xna.Framework.Graphics;
using SDGraphics.Rendering;
using SDUtils;

namespace SDGraphics.Sprites;

/// <summary>
/// A fixed-size reusable CPU+GPU buffer
/// </summary>
internal sealed class SpriteVertexBuffer : IDisposable
{
    // the maximum # of vertices per buffer,
    // this must be smaller than 10922 (which is ushort.MaxValue / 6)
    public const int Size = 4096;

    // A vertex buffer holding the quads. This contains the same data as
    // the quads array, but copied across to where the GPU can access it.
    DynamicVertexBuffer VertexBuffer;

    // all of the quads
    public readonly VertexCoordColor[] Quads;

    // no more free quads in this buffer
    public bool IsFull => Count == Size;
    public int Count;

    // index of first vertex pending upload
    int FirstPending;

    public SpriteVertexBuffer(GraphicsDevice device)
    {
        Quads = new VertexCoordColor[Size * 4];
        VertexBuffer = new(device, VertexCoordColor.SizeInBytes*Size*4, BufferUsage.WriteOnly);
    }

    ~SpriteVertexBuffer()
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
        FirstPending = 0;
    }

    public unsafe void Add(in Quad3D quad, in Quad2D coords, Color color)
    {
        int vertexOffset = Count * 4;
        ++Count;

        fixed (VertexCoordColor* quads = Quads)
        {
            VertexCoordColor* q0 = (quads + vertexOffset);
            VertexCoordColor* q1 = (quads + vertexOffset + 1);
            VertexCoordColor* q2 = (quads + vertexOffset + 2);
            VertexCoordColor* q3 = (quads + vertexOffset + 3);
            q0->Position = quad.A; q0->Color = color; q0->Coords = coords.A; // TopLeft
            q1->Position = quad.B; q1->Color = color; q1->Coords = coords.B; // TopRight
            q2->Position = quad.C; q2->Color = color; q2->Coords = coords.C; // BotRight
            q3->Position = quad.D; q3->Color = color; q3->Coords = coords.D; // BotLeft
        }
    }

    public void Draw(SpriteRenderer sr, Texture2D texture, int startIndex, int drawCount)
    {
        DynamicVertexBuffer vbo = VertexBuffer;

        if (FirstPending != Count)
        {
            // Restore the vertex buffer contents if the graphics device was lost.
            // Or just send all data if numPending == Size
            int numPending = Count - FirstPending;
            if (vbo.IsContentLost || numPending == Size)
            {
                vbo.SetData(Quads);
            }
            else // upload quads to the GPU
            {
                try
                {
                    const int stride = VertexCoordColor.SizeInBytes;
                    vbo.SetData(0, Quads, FirstPending, numPending * 4, stride, SetDataOptions.NoOverwrite);
                }
                catch // if this fails for some reason, just send all data
                {
                    vbo.SetData(Quads);
                }
            }
            FirstPending = Count; // upload complete
        }

        GraphicsDevice device = sr.Device;
        // set the vertex buffer
        device.Vertices[0].SetSource(vbo, 0, VertexCoordColor.SizeInBytes);

        sr.ShaderBegin(texture, Color.White);
        device.DrawIndexedPrimitives(PrimitiveType.TriangleList, 0,
                                     startIndex * 4, drawCount * 4, // 4 points
                                     startIndex * 6, drawCount * 2); // 2 triangles
        sr.ShaderEnd();
    }
}
