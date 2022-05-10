using System;
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
        new (0, 0, VertexElementFormat.Vector3, VertexElementMethod.Default, VertexElementUsage.Position, 0),
        new (0, 12, VertexElementFormat.Color, VertexElementMethod.Default, VertexElementUsage.Color, 0),
        new (0, 16, VertexElementFormat.Vector2, VertexElementMethod.Default, VertexElementUsage.TextureCoordinate, 0)
    };

    public VertexCoordColor(in Vector3 pos, Color color, in Vector2 coords)
    {
        Position = pos;
        Color = color;
        Coords = coords;
    }
}