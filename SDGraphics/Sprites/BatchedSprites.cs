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
    VertexDeclaration VD;
    VertexBuffer VertexBuf;
    IndexBuffer IndexBuf;
    BatchCompiler Compiler = new();
    Array<SpriteBatchSpan> Batches;

    public bool IsCompiled => Compiler == null;
    public bool IsDisposed => VD == null;

    public BatchedSprites(SpriteRenderer sr)
    {
        VD = sr.VertexDeclaration;
    }

    public void Dispose()
    {
        VD = null; // managed by SpriteRenderer
        Mem.Dispose(ref VertexBuf);
        Mem.Dispose(ref IndexBuf);
    }

    public void Add(Texture2D texture, in Quad3D quad, in Quad2D coords, Color color)
    {
        if (IsCompiled)
            throw new InvalidOperationException("Cannot add more sprites after Compile() was called");

        Array<SpriteData> sprites = Compiler.GetBatch(texture);
        sprites.Add(new() { Quad = quad, Coords = coords, Color = color });
    }

    public void Add(SubTexture subTex, in Quad3D quad, Color color)
    {
        Texture2D tex = subTex.Texture;
        float tx = subTex.X / (float)tex.Width;
        float ty = subTex.Y / (float)tex.Height;
        float tw = (subTex.Width - 1) / (float)tex.Width;
        float th = (subTex.Height - 1) / (float)tex.Height;
        Quad2D coords = new(tx, ty, tw, th);

        Add(tex, quad, coords, color);
    }

    public void Draw(SpriteRenderer sr)
    {
        if (!IsCompiled)
            return; // nothing to do

        GraphicsDevice device = sr.Device;
        device.Vertices[0].SetSource(VertexBuf, 0, VertexCoordColor.SizeInBytes);
        device.Indices = IndexBuf;
        device.VertexDeclaration = VD;

        foreach (ref SpriteBatchSpan batch in Batches.AsSpan())
        {
            sr.ShaderBegin(batch.Texture);
            device.DrawIndexedPrimitives(PrimitiveType.TriangleList, 0,
                                         batch.StartIndex * 4, batch.Count * 4, // 4 points
                                         batch.StartIndex * 6, batch.Count * 2); // 2 triangles
            sr.ShaderEnd();
        }
    }

    public void Compile()
    {
        Batches = Compiler.Compile(VD, out VertexBuf, out IndexBuf);
        Compiler = null;
    }

    readonly record struct SpriteData(Quad3D Quad, Quad2D Coords, Color Color);
    readonly record struct SpriteBatchSpan(Texture2D Texture, int StartIndex, int Count);

    class BatchCompiler
    {
        Array<SpriteData> Untextured = new();
        Map<Texture2D, Array<SpriteData>> Textured = new();

        public unsafe Array<SpriteBatchSpan> Compile(VertexDeclaration vd, out VertexBuffer vertexBuf, out IndexBuffer indexBuf)
        {
            Array<SpriteBatchSpan> batches = new();
            int numSprites = GetNumSprites();
            var vertices = new VertexCoordColor[numSprites * 4];
            ushort[] indices = new ushort[numSprites * 6];

            int currentIndex = 0;

            fixed (VertexCoordColor* pVertices = vertices)
            {
                fixed (ushort* pIndices = indices)
                {
                    if (Untextured.NotEmpty)
                    {
                        batches.Add(new(null, currentIndex, Untextured.Count));

                        foreach (ref SpriteData sd in Untextured.AsSpan())
                        {
                            SpriteRenderer.FillVertexData(pVertices, pIndices, currentIndex, sd.Quad, sd.Coords, sd.Color);
                            ++currentIndex;
                        }
                    }

                    foreach (KeyValuePair<Texture2D, Array<SpriteData>> kv in Textured)
                    {
                        batches.Add(new(kv.Key, currentIndex, kv.Value.Count));

                        foreach (ref SpriteData sd in kv.Value.AsSpan())
                        {
                            SpriteRenderer.FillVertexData(pVertices, pIndices, currentIndex, sd.Quad, sd.Coords, sd.Color);
                            ++currentIndex;
                        }
                    }
                }
            }

            if (currentIndex != numSprites)
                throw new("Batched Sprite compilation failed");

            vertexBuf = new(vd.GraphicsDevice, VertexCoordColor.SizeInBytes*vertices.Length, BufferUsage.WriteOnly);
            vertexBuf.SetData(vertices);

            indexBuf = new(vd.GraphicsDevice, typeof(ushort), indices.Length, BufferUsage.WriteOnly);
            indexBuf.SetData(indices);

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
