using System;
using Microsoft.Xna.Framework.Graphics;
using SDGraphics.Rendering;
using SDUtils;

namespace SDGraphics.Sprites;

/// <summary>
/// A single textured 3D Sprite billboard
/// </summary>
internal class DynamicSpriteData : IComparable<DynamicSpriteData>
{
    public Texture2D Texture;
    public int SortKey;
    public Quad3D Quad;
    public Quad2D Coords;
    public Color Color;

    public int CompareTo(DynamicSpriteData other)
    {
        if (SortKey < other.SortKey)
            return -1;
        return SortKey > other.SortKey ? 1 : 0;
    }
}

/// <summary>
/// Performs batching and sorting operation for submitted sprites
/// </summary>
internal class DynamicSpriteBatcher : IDisposable
{
    readonly GraphicsDevice Device;
    DynamicSpriteData[] Sprites = new DynamicSpriteData[512];
    int Count; // number of sprites this frame

    /// <summary>
    /// Describes a batch of sprites to draw, from startIndex, to startIndex+count
    /// </summary>
    readonly record struct Batch(SpriteVertexBuffer Buffer, Texture2D Texture, int StartIndex, int Count);

    readonly Map<Texture2D, int> TextureIDs = new();
    readonly Array<Batch> Batches = new(16);

    readonly Array<SpriteVertexBuffer> AllBuffers = new();
    readonly Array<SpriteVertexBuffer> FreeBuffers = new();

    public DynamicSpriteBatcher(GraphicsDevice device)
    {
        Device = device;
        for (int i = 0; i < Sprites.Length; ++i)
            Sprites[i] = new();
    }

    public void Dispose()
    {
        TextureIDs.Clear();
        foreach (var buffer in AllBuffers)
            buffer.Dispose();
        AllBuffers.Clear();
        FreeBuffers.Clear();
    }

    public void Reset()
    {
        Count = 0;
        TextureIDs.Clear();
        Batches.Clear();
    }

    public void RecycleBuffers()
    {
        FreeBuffers.Assign(AllBuffers);
    }

    SpriteVertexBuffer GetBuffer()
    {
        if (FreeBuffers.NotEmpty)
        {
            SpriteVertexBuffer last = FreeBuffers.PopLast();
            last.Reset();
            return last;
        }
        SpriteVertexBuffer b = new(Device);
        AllBuffers.Add(b);
        return b;
    }

    int GetSortKey(Texture2D texture)
    {
        if (TextureIDs.TryGetValue(texture, out int sortKey))
            return sortKey;
        sortKey = TextureIDs.Count + 1;
        TextureIDs.Add(texture, sortKey);
        return sortKey;
    }

    void Grow()
    {
        var sprites = new DynamicSpriteData[Sprites.Length * 2];
        for (int i = Sprites.Length; i < sprites.Length; ++i)
            sprites[i] = new();

        Sprites.CopyTo(sprites.AsSpan());
        Sprites = sprites;
    }

    public void Add(Texture2D texture, in Quad3D quad, in Quad2D coords, Color color)
    {
        if (Sprites.Length == Count)
            Grow();

        DynamicSpriteData sprite = Sprites[Count++];
        sprite.Texture = texture;
        sprite.SortKey = texture == null ? 0 : GetSortKey(texture);
        sprite.Quad = quad;
        sprite.Coords = coords;
        sprite.Color = color;
    }

    public void DrawBatches(SpriteRenderer sr)
    {
        if (Count <= 0)
            return; // exceedingly rare

        // sort by SortKey
        Array.Sort(Sprites, 0, Count);

        Texture2D texture = null;
        int batchIndex = 0;
        int batchSize = 0;
        SpriteVertexBuffer last = GetBuffer();

        var sprites = Sprites.AsSpan(0, Count);

        foreach (DynamicSpriteData sprite in sprites)
        {
            if (!ReferenceEquals(texture, sprite.Texture))
            {
                if (batchSize > 0)
                {
                    Batches.Add(new(last, texture, batchIndex, batchSize));
                }
                texture = sprite.Texture;
                batchIndex += batchSize;
                batchSize = 0;
            }

            if (last.IsFull)
            {
                if (batchSize > 0)
                {
                    Batches.Add(new(last, texture, batchIndex, batchSize));
                }
                last = GetBuffer();
                batchIndex = 0;
                batchSize = 0;
            }

            last.Add(sprite.Quad, sprite.Coords, sprite.Color);
            sprite.Texture = null;
            ++batchSize;
        }

        // set the index buffer and vertex layout
        Device.Indices = sr.IndexBuf;
        Device.VertexDeclaration = sr.VertexDeclaration;

        foreach (var batch in Batches.AsSpan())
        {
            batch.Buffer.Draw(sr, batch.Texture, batch.StartIndex, batch.Count);
        }
    }
}
