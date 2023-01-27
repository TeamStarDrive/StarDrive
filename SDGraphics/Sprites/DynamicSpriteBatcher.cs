using System;
using Microsoft.Xna.Framework.Graphics;
using SDGraphics.Rendering;
using SDUtils;

namespace SDGraphics.Sprites;

/// <summary>
/// A single textured 3D Sprite billboard
/// </summary>
internal struct DynamicSpriteData
{
    public int TextureIndex;
    public Quad3D Quad;
    public Quad2D Coords;
    public Color Color;
}

/// <summary>
/// Performs batching and sorting operation for submitted sprites
/// </summary>
internal unsafe class DynamicSpriteBatcher : IDisposable
{
    readonly GraphicsDevice Device;
    DynamicSpriteData[] Sprites = new DynamicSpriteData[512];
    DynamicSpriteData*[] SortedSprites = new DynamicSpriteData*[512];
    int Count; // number of sprites this frame

    /// <summary>
    /// Describes a batch of sprites to draw, from startIndex, to startIndex+count
    /// </summary>
    readonly record struct Batch(SpriteVertexBuffer Buffer, Texture2D Texture, int StartIndex, int Count);

    struct BatchTexture
    {
        public Texture2D Texture;
        public int NumSprites; // how many sprites use this texture
        public int NextIndex; // after sorting, where does this batch of sprites start at?
    }

    readonly Array<Batch> Batches = new(16);
    readonly Array<BatchTexture> Textures = new(16);

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
        Reset();

        foreach (var buffer in AllBuffers)
            buffer.Dispose();
        AllBuffers.Clear();
        FreeBuffers.Clear();
        Array.Clear(Sprites, 0, Sprites.Length);
        Array.Clear(SortedSprites, 0, SortedSprites.Length);
    }

    public void Reset()
    {
        Count = 0;
        Batches.Clear();
        Textures.Clear();
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

    void Grow()
    {
        var sprites = new DynamicSpriteData[Sprites.Length * 2];
        for (int i = Sprites.Length; i < sprites.Length; ++i)
            sprites[i] = new();

        Sprites.CopyTo(sprites.AsSpan());
        Sprites = sprites;
        // for sorted sprites, just expand it, because we use it as a scratch space only
        SortedSprites = new DynamicSpriteData*[sprites.Length];
    }

    public void Add(Texture2D texture, in Quad3D quad, in Quad2D coords, Color color)
    {
        if (Sprites.Length == Count)
            Grow();

        // find the existing BatchTexture and increase its NumSprites
        // we assume here the statistical # of textures per batch is actually super low
        int textureIndex = -1;
        Span<BatchTexture> textures = Textures.AsSpan();
        for (int i = 0; i < textures.Length; ++i)
        {
            ref BatchTexture batchTexture = ref textures[i];
            if (batchTexture.Texture == texture)
            {
                ++batchTexture.NumSprites;
                textureIndex = i;
                break;
            }
        }

        if (textureIndex == -1)
        {
            textureIndex = textures.Length;
            Textures.Add(new(){ Texture = texture, NumSprites = 1});
        }

        ref DynamicSpriteData sprite = ref Sprites[Count++];
        sprite.TextureIndex = textureIndex;
        sprite.Quad = quad;
        sprite.Coords = coords;
        sprite.Color = color;
    }

    public void DrawBatches(SpriteRenderer sr)
    {
        int numSprites = Count;
        if (numSprites <= 0)
            return; // exceedingly rare

        // prepare to sort sprites by their TextureIndex
        Span<BatchTexture> textures = Textures.AsSpan();
        bool sortSprites = textures.Length > 1;
        if (sortSprites) // sort into SortedSprites
        {
            int startIndex = 0;
            for (int i = 0; i < textures.Length; ++i)
            {
                ref BatchTexture batchTexture = ref textures[i];
                batchTexture.NextIndex = startIndex;
                startIndex += batchTexture.NumSprites;
            }
        }

        fixed (DynamicSpriteData** pSorted = SortedSprites)
        {
            fixed (DynamicSpriteData* pUnsorted = Sprites)
            {
                if (sortSprites) // use the texture index to sort the Sprites
                {
                    for (int i = 0; i < numSprites; ++i)
                    {
                        DynamicSpriteData* s = &pUnsorted[i];
                        ref BatchTexture batchTexture = ref textures[s->TextureIndex];
                        pSorted[batchTexture.NextIndex++] = s;
                    }
                }
                else // simply copy the pointers
                {
                    for (int i = 0; i < numSprites; ++i)
                    {
                        pSorted[i] = &pUnsorted[i];
                    }
                }
            }

            int textureIndex = 0;
            int batchIndex = 0;
            int batchSize = 0;
            SpriteVertexBuffer last = GetBuffer();

            for (int i = 0; i < numSprites; ++i)
            {
                DynamicSpriteData* s = pSorted[i];

                if (textureIndex != s->TextureIndex)
                {
                    if (batchSize > 0)
                    {
                        Batches.Add(new(last, textures[textureIndex].Texture, batchIndex, batchSize));
                    }
                    textureIndex = s->TextureIndex;
                    batchIndex += batchSize;
                    batchSize = 0;
                }

                if (last.IsFull)
                {
                    if (batchSize > 0)
                    {
                        Batches.Add(new(last, textures[textureIndex].Texture, batchIndex, batchSize));
                    }
                    last = GetBuffer();
                    batchIndex = 0;
                    batchSize = 0;
                }

                int vertexOffset = last.Count * 4;
                ++last.Count;
                ++batchSize;

                fixed (VertexCoordColor* pQuads = last.Quads)
                {
                    VertexCoordColor* q0 = (pQuads + vertexOffset + 0);
                    VertexCoordColor* q1 = (pQuads + vertexOffset + 1);
                    VertexCoordColor* q2 = (pQuads + vertexOffset + 2);
                    VertexCoordColor* q3 = (pQuads + vertexOffset + 3);
                    q0->Position = s->Quad.A; q0->Color = s->Color; q0->Coords = s->Coords.A; // TopLeft
                    q1->Position = s->Quad.B; q1->Color = s->Color; q1->Coords = s->Coords.B; // TopRight
                    q2->Position = s->Quad.C; q2->Color = s->Color; q2->Coords = s->Coords.C; // BotRight
                    q3->Position = s->Quad.D; q3->Color = s->Color; q3->Coords = s->Coords.D; // BotLeft
                }
            }

            // submit any incomplete batches
            if (batchSize > 0)
            {
                Batches.Add(new(last, textures[textureIndex].Texture, batchIndex, batchSize));
            }
        }

        // set the index buffer and vertex layout
        Device.Indices = sr.IndexBuf;
        Device.VertexDeclaration = sr.VertexDeclaration;

        foreach (Batch batch in Batches.AsSpan())
        {
            batch.Buffer.Draw(sr, batch.Texture, batch.StartIndex, batch.Count);
        }
    }
}
