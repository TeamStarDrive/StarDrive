using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Graphics.PackedVector;
using SDGraphics;

namespace Ship_Game;

public struct ParticleVertex
{
    // Stores which corner of the particle quad this vertex represents.
    public Short2 Corner;
    // Stores the starting position of the particle.
    public Vector3 Position;
    // Stores the starting velocity of the particle.
    public Vector3 Velocity;
    // Overriding multiplicative color value for this particle
    public Color Color;
    // Four random values, used to make each particle look slightly different.
    public Color Random;
    // Extra scaling multiplier added to the particle
    public float Scale;
    // The time (in seconds) at which this particle was created.
    public float Time;

    public static readonly VertexElement[] VertexElements =
    {
        new VertexElement(0, 0,  VertexElementFormat.Short2,  VertexElementMethod.Default, VertexElementUsage.Position, 0),
        new VertexElement(0, 4,  VertexElementFormat.Vector3, VertexElementMethod.Default, VertexElementUsage.Position, 1),
        new VertexElement(0, 16, VertexElementFormat.Vector3, VertexElementMethod.Default, VertexElementUsage.Normal, 0),
        new VertexElement(0, 28, VertexElementFormat.Color,   VertexElementMethod.Default, VertexElementUsage.Color, 0),
        new VertexElement(0, 32, VertexElementFormat.Color,   VertexElementMethod.Default, VertexElementUsage.Color, 1),
        new VertexElement(0, 36, VertexElementFormat.Single,  VertexElementMethod.Default, VertexElementUsage.TextureCoordinate, 0),
        new VertexElement(0, 40, VertexElementFormat.Single,  VertexElementMethod.Default, VertexElementUsage.TextureCoordinate, 1)
    };

    public const int SizeInBytes = 44;
}