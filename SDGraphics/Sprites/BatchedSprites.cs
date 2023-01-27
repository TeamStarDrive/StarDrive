using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;
using SDGraphics.Rendering;
using SDUtils;

namespace SDGraphics.Sprites;

/// <summary>
/// Enables batching multiple sprite draw calls into a pre-built list of draw operations.
///
/// This is useful when you always need to draw the same bunch of sprites
/// and want to do it really fast.
///
/// The batch is finalized when you call Compile(),
/// after which, no more items can be added.
/// </summary>
public sealed class BatchedSprites : IDisposable
{
    VertexBuffer VertexBuf;
    BatchCompiler Compiler = new();

    /// <summary>
    /// A single 3D Sprite billboard
    /// </summary>
    readonly record struct SpriteData(Quad3D Quad, Quad2D Coords, Color Color);

    Array<SpriteBatchSpan> Batches;

    public bool IsCompiled => Compiler == null;
    public bool IsDisposed => Compiler == null && VertexBuf == null;

    public BatchedSprites()
    {
    }

    public void Dispose()
    {
        Mem.Dispose(ref VertexBuf);
    }

    public void Add(Texture2D texture, in Quad3D quad, in Quad2D coords, Color color)
    {
        if (IsCompiled)
            throw new InvalidOperationException("Cannot add more sprites after Compile() was called");

        Array<SpriteData> sprites = Compiler.GetBatch(texture);
        sprites.Add(new() { Quad = quad, Coords = coords, Color = color });
    }

    public void Add(SubTexture subTex, in Quad3D quad)
    {
        Add(subTex.Texture, quad, subTex.UVCoords, Color.White);
    }

    public void Add(SubTexture subTex, in Quad3D quad, Color color)
    {
        Add(subTex.Texture, quad, subTex.UVCoords, color);
    }

    public void Draw(SpriteRenderer sr, Color color)
    {
        if (!IsCompiled)
            return; // nothing to do

        GraphicsDevice device = sr.Device;
        device.Vertices[0].SetSource(VertexBuf, 0, VertexCoordColor.SizeInBytes);
        device.Indices = sr.IndexBuf;
        device.VertexDeclaration = sr.VertexDeclaration;

        foreach (ref SpriteBatchSpan batch in Batches.AsSpan())
        {
            sr.ShaderBegin(batch.Texture, color);
            device.DrawIndexedPrimitives(PrimitiveType.TriangleList, 0,
                                         batch.StartIndex * 4, batch.Count * 4, // 4 points
                                         batch.StartIndex * 6, batch.Count * 2); // 2 triangles
            sr.ShaderEnd();
        }
    }

    public void Compile(GraphicsDevice g)
    {
        Batches = Compiler.Compile(g, out VertexBuf);
        Compiler = null;
    }

    // fills vertices of a quad at vertices[index*4] to vertices[index*4 + 3]
    static unsafe void FillQuad(VertexCoordColor* vertices, int index,
                                in Quad3D quad, in Quad2D coords, Color color)
    {
        int vertexOffset = index * 4;
        vertices[vertexOffset + 0] = new(quad.A, color, coords.A); // TopLeft
        vertices[vertexOffset + 1] = new(quad.B, color, coords.B); // TopRight
        vertices[vertexOffset + 2] = new(quad.C, color, coords.C); // BotRight
        vertices[vertexOffset + 3] = new(quad.D, color, coords.D); // BotLeft
    }

    /// <summary>
    /// Describes a range of sprites to draw, from startIndex, to startIndex+count
    /// </summary>
    readonly record struct SpriteBatchSpan(Texture2D Texture, int StartIndex, int Count);

    class BatchCompiler
    {
        Array<SpriteData> Untextured = new();
        Map<Texture2D, Array<SpriteData>> Textured = new();

        public unsafe Array<SpriteBatchSpan> Compile(GraphicsDevice g, out VertexBuffer vertexBuf)
        {
            Array<SpriteBatchSpan> batches = new();
            int numSprites = GetNumSprites();
            var vertices = new VertexCoordColor[numSprites * 4];
            int currentIndex = 0;

            fixed (VertexCoordColor* pVertices = vertices)
            {
                if (Untextured.NotEmpty)
                {
                    batches.Add(new(null, currentIndex, Untextured.Count));

                    foreach (ref SpriteData sd in Untextured.AsSpan())
                    {
                        FillQuad(pVertices, currentIndex, sd.Quad, sd.Coords, sd.Color);
                        ++currentIndex;
                    }
                }

                foreach (KeyValuePair<Texture2D, Array<SpriteData>> kv in Textured)
                {
                    batches.Add(new(kv.Key, currentIndex, kv.Value.Count));

                    foreach (ref SpriteData sd in kv.Value.AsSpan())
                    {
                        FillQuad(pVertices, currentIndex, sd.Quad, sd.Coords, sd.Color);
                        ++currentIndex;
                    }
                }
            }

            if (currentIndex != numSprites)
                throw new("Batched Sprite compilation failed");

            vertexBuf = new(g, VertexCoordColor.SizeInBytes*vertices.Length, BufferUsage.WriteOnly);
            vertexBuf.SetData(vertices);

            Untextured.Clear();
            Textured.Clear();
            Untextured = null;
            Textured = null;

            return batches;
        }

        int GetNumSprites()
        {
            int numSprites = Untextured.Count;
            foreach (var kv in Textured)
                numSprites += kv.Value.Count;
            return numSprites;
        }

        public Array<SpriteData> GetBatch(Texture2D texture)
        {
            if (texture == null)
                return Untextured;

            if (Textured.TryGetValue(texture, out Array<SpriteData> sprites))
                return sprites;

            sprites = new();
            Textured.Add(texture, sprites);
            return sprites;
        }
    }
}
