﻿using Microsoft.Xna.Framework.Graphics;
using SDGraphics.Rendering;

namespace SDGraphics;

public class SubTexture
{
    // name="sprite1" x="461" y="1317" width="28" height="41"
    public readonly string Name; // name of the sprite for name-based lookup
    public readonly int X;
    public readonly int Y;
    public readonly int Width;
    public readonly int Height;
    public readonly Texture2D Texture;

    /// <summary>
    /// path to the source of the texture
    /// this could be a stand-alone file before being packed to an atlas
    /// or it might be a pre-packed atlas file
    /// </summary>
    public readonly string TexturePath;

    /// <summary>
    /// Pre-calculated UV-Coordinates that can be used for sprite rendering
    /// </summary>
    public readonly Quad2D UVCoords;

    public Rectangle Rect => new(X, Y, Width, Height);
    public int Right  => X + Width;
    public int Bottom => Y + Height;

    public SubTexture(string name, int x, int y, int w, int h, Texture2D texture, string texturePath)
    {
        Name = name;
        X = x;
        Y = y;
        Width = w;
        Height = h;
        Texture = texture;
        TexturePath = texturePath;

        float tx = x / (float)texture.Width;
        float ty = y / (float)texture.Height;
        float tw = (w - 1) / (float)texture.Width;
        float th = (h - 1) / (float)texture.Height;
        UVCoords = new(tx, ty, tw, th);
    }

    // special case: SubTexture is a container for a full texture
    public SubTexture(string name, Texture2D fullTexture, string texturePath)
    {
        Name = name;
        Width = fullTexture.Width;
        Height = fullTexture.Height;
        Texture = fullTexture;
        TexturePath = texturePath;
        UVCoords = new(0.0f, 0.0f, 1.0f, 1.0f);
    }

    public Vector2 CenterF => new(Width/2f, Height/2f);
    public Vector2 SizeF => new(Width, Height);
    public Point Center => new(Width/2, Height/2);
    public int CenterX => Width/2;
    public int CenterY => Height/2;

    public float AspectRatio => Width / (float)Height;

    // TODO: document the aspect-fill stuff
    public float GetHeightFromWidthAspect(float wantedWidth) => GetHeightFromWidthAspect(Width, Height, wantedWidth);
    public float GetWidthFromHeightAspect(float wantedHeight) => GetWidthFromHeightAspect(Width, Height, wantedHeight);
    public Vector2 GetAspectFill(float minSize) => GetAspectFill(Width, Height, minSize);
    public Vector2d GetAspectFill(double minSize) => GetAspectFill(Width, Height, minSize);

    public static float GetHeightFromWidthAspect(int width, int height, float wantedWidth)
    {
        return wantedWidth * (height / (float)width);
    }

    public static float GetWidthFromHeightAspect(int width, int height, float wantedHeight)
    {
        return wantedHeight * (width / (float)height);
    }

    public static Vector2 GetAspectFill(int width, int height, float minSize)
    {
        if (width == height)
            return new Vector2(minSize);
        // if width is bigger, we enlarge size.X and set size.Y to minSize
        if (width > height)
            return new Vector2(minSize * (width / (float)height), minSize);
        // if height is bigger, we set size.X to minSize and enlarge size.Y
        return new Vector2(minSize, minSize * (height / (float)width));
    }

    public static Vector2d GetAspectFill(int width, int height, double minSize)
    {
        if (width == height)
            return new Vector2d(minSize);
        // if width is bigger, we enlarge size.X and set size.Y to minSize
        if (width > height)
            return new Vector2d(minSize * (width / (double)height), minSize);
        // if height is bigger, we set size.X to minSize and enlarge size.Y
        return new Vector2d(minSize, minSize * (height / (double)width));
    }

    public override string ToString()
        => $"sub-tex  {Name} {X},{Y} {Width}x{Height}  texture:{Texture.Width}x{Texture.Height}";
}