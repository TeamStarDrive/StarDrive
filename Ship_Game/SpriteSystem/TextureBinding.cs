using System;
using System.IO;
using System.Threading;
using Microsoft.Xna.Framework.Graphics;
using SDGraphics;

namespace Ship_Game.SpriteSystem;

/// <summary>
/// This enables Lazy-Loading for the textures
/// Contains all necessary information to load non-packed Textures or packed Atlas SubTextures
/// </summary>
public class TextureBinding
{
    SubTexture SubTex;
    public Texture2D Texture; // if set, this binding holds ownership of Texture
    readonly string UnpackedPath; // if set, this texture does not reference the main atlas
    public readonly string Name;
    readonly int X, Y;
    public readonly int Width, Height;
    readonly TextureAtlas Atlas; // always non-null
    readonly string SourcePath;

    public TextureBinding(TextureAtlas atlas, TextureInfo t)
    {
        SubTex = null;
        Name = t.Name;
        UnpackedPath = t.UnpackedPath;
        X = t.X;
        Y = t.Y;
        Width = t.Width;
        Height = t.Height;
        Atlas = atlas;
        SourcePath = t.SourcePath;
    }

    public SubTexture GetOrLoadTexture()
    {
        if (SubTex != null)
            return SubTex;

        if (UnpackedPath == null) // texture is packed into atlas
        {
            lock (Atlas)
            {
                if (UnpackedPath == null) // double-check locking
                {
                    Thread.MemoryBarrier();
                    SubTex = new(Name, X, Y, Width, Height, Atlas.GetAtlasTexture(), SourcePath);
                }
            }
        }
        // load the unpacked texture if we already didn't
        else if (Texture == null)
        {
            lock (Atlas)
            {
                if (Texture == null) // double-check locking
                {
                    Thread.MemoryBarrier();
                    (Texture, SubTex) = LoadTextureUnsafe();
                }
            }
        }
        return SubTex;
    }

    (Texture2D texture, SubTexture subTex) LoadTextureUnsafe()
    {
        var file = new FileInfo(UnpackedPath);
        if (!file.Exists)
        {
            Log.Warning(ConsoleColor.Red, $"NonPacked Texture does not exist: {UnpackedPath}");
        }
        var texture = ResourceManager.RootContent.LoadUncachedTexture(file, Atlas.Name);
        SubTexture subTex = new(Name, texture, SourcePath);
        return (texture, subTex);
    }
}
