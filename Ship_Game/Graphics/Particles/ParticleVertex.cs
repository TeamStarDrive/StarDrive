using Microsoft.Xna.Framework.Graphics;
using Color = Microsoft.Xna.Framework.Color;
using Vector2 = Microsoft.Xna.Framework.Vector2;
using SDGraphics;

namespace Ship_Game;

public struct ParticleVertex
{
    // Stores which corner of the particle quad this vertex represents.
    // Phase 3.5: switched from Short2 to Vector2 (full float). MonoGame's
    // DirectX_11 backend reads VertexElementFormat.Short2 as if SNORM-decoded
    // (Short2(-1) → -1/32767 ≈ 0), collapsing every 4-vertex quad into a
    // degenerate point. The +4 bytes/vertex is negligible and the explicit
    // float layout is unambiguous across backends.
    public Vector2 Corner;
    // Stores the starting position of the particle.
    public SDGraphics.Vector3 Position;
    // Stores the starting velocity of the particle.
    public SDGraphics.Vector3 Velocity;
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
        new VertexElement(0,  VertexElementFormat.Vector2, VertexElementUsage.Position, 0),
        new VertexElement(8,  VertexElementFormat.Vector3, VertexElementUsage.Position, 1),
        new VertexElement(20, VertexElementFormat.Vector3, VertexElementUsage.Normal, 0),
        new VertexElement(32, VertexElementFormat.Color,   VertexElementUsage.Color, 0),
        new VertexElement(36, VertexElementFormat.Color,   VertexElementUsage.Color, 1),
        new VertexElement(40, VertexElementFormat.Single,  VertexElementUsage.TextureCoordinate, 0),
        new VertexElement(44, VertexElementFormat.Single,  VertexElementUsage.TextureCoordinate, 1)
    };

    public const int SizeInBytes = 48;
}