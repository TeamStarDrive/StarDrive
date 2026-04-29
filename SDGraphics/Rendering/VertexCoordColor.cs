using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace SDGraphics.Rendering;

/// <summary>
/// A 3D vertex with Position, TexCoord and VertexColor
/// </summary>
public struct VertexCoordColor
{
    public Vector3 Position;
    public Color Color;
    public Vector2 Coords;

    public const int SizeInBytes = 24;

    public static readonly VertexElement[] VertexElements = new VertexElement[3]
    {
        new (0, VertexElementFormat.Vector3, VertexElementUsage.Position, 0),
        new (12, VertexElementFormat.Color, VertexElementUsage.Color, 0),
        new (16, VertexElementFormat.Vector2, VertexElementUsage.TextureCoordinate, 0)
    };

    public static readonly VertexDeclaration VertexDeclaration = new(VertexElements);

    public VertexCoordColor(in Vector3 pos, Color color, in Vector2 coords)
    {
        Position = pos;
        Color = color;
        Coords = coords;
    }
}