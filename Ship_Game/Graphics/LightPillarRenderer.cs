using System;
using Microsoft.Xna.Framework.Graphics;
using SDGraphics;
using SDUtils;
using Ship_Game.Ships;
using Color = Microsoft.Xna.Framework.Color;
using Matrix = SDGraphics.Matrix;
using Vector2 = SDGraphics.Vector2;
using XnaVector2 = Microsoft.Xna.Framework.Vector2;
using XnaVector3 = Microsoft.Xna.Framework.Vector3;

namespace Ship_Game.Graphics;

public sealed class LightPillarRenderer : IDisposable
{
    const float BaseGlowScale = 1.6f;

    BasicEffect Fx;
    Texture2D ColumnGlow;
    Texture2D BaseGlow;
    readonly Array<LightPillar> Pillars = new();
    readonly VertexPositionColorTexture[] Vertices = new VertexPositionColorTexture[4];
    static readonly short[] Indices = { 0, 1, 2, 2, 1, 3 };

    public LightPillarRenderer(GraphicsDevice device)
    {
        ColumnGlow = CreateColumnGlow(device);
        BaseGlow = CreateBaseGlow(device);
        Fx = new BasicEffect(device) { TextureEnabled = true, VertexColorEnabled = true };
    }

    public void Dispose()
    {
        Mem.Dispose(ref Fx);
        Mem.Dispose(ref ColumnGlow);
        Mem.Dispose(ref BaseGlow);
    }

    public void Draw(GraphicsDevice device, Ship[] ships, float simTime, Vector2 cameraPos, in Matrix view, in Matrix projection)
    {
        Pillars.Clear();
        foreach (Ship ship in ships)
        {
            if (ship.IsMiningStation && ship.Active && ship.InFrustum && ship.InPlayerSensorRange)
                ship.Carrier.MiningBays?.AddLightPillars(simTime, Pillars);
        }

        if (Pillars.IsEmpty)
            return;

        RenderStates.BasicBlendMode(device, additive: true, depthWrite: false);
        Fx.View = view;
        Fx.Projection = projection;

        Fx.Texture = ColumnGlow;
        foreach (LightPillar pillar in Pillars)
        {
            Vector2 toCamera = cameraPos - pillar.Position;
            Vector2 across = toCamera.Length() > 1f ? toCamera.Normalized().LeftVector() : new Vector2(1f, 0f);
            Vector2 left = pillar.Position - across * pillar.HalfWidth;
            Vector2 right = pillar.Position + across * pillar.HalfWidth;
            SetQuad(new(left.X, left.Y, pillar.TopZ), new(right.X, right.Y, pillar.TopZ),
                    new(left.X, left.Y, pillar.BottomZ), new(right.X, right.Y, pillar.BottomZ), pillar.Color);
            DrawQuad(device);
        }

        Fx.Texture = BaseGlow;
        foreach (LightPillar pillar in Pillars)
        {
            float r = pillar.HalfWidth * BaseGlowScale;
            Vector2 p = pillar.Position;
            SetQuad(new(p.X - r, p.Y - r, 0f), new(p.X + r, p.Y - r, 0f),
                    new(p.X - r, p.Y + r, 0f), new(p.X + r, p.Y + r, 0f), pillar.Color);
            DrawQuad(device);
        }
    }

    void SetQuad(in XnaVector3 topLeft, in XnaVector3 topRight, in XnaVector3 bottomLeft, in XnaVector3 bottomRight, Color color)
    {
        Vertices[0] = new(topLeft, color, new XnaVector2(0f, 0f));
        Vertices[1] = new(topRight, color, new XnaVector2(1f, 0f));
        Vertices[2] = new(bottomLeft, color, new XnaVector2(0f, 1f));
        Vertices[3] = new(bottomRight, color, new XnaVector2(1f, 1f));
    }

    void DrawQuad(GraphicsDevice device)
    {
        foreach (EffectPass pass in Fx.CurrentTechnique.Passes)
        {
            pass.Apply();
            device.DrawUserIndexedPrimitives(PrimitiveType.TriangleList, Vertices, 0, 4, Indices, 0, 2);
        }
    }

    static Texture2D CreateColumnGlow(GraphicsDevice device)
    {
        const int width = 32, height = 128;
        var pixels = new Color[width * height];
        for (int y = 0; y < height; ++y)
        {
            float v = (y + 0.5f) / height;
            float along = SmoothEdge(v) * SmoothEdge(1f - v);
            for (int x = 0; x < width; ++x)
            {
                float fromCenter = Math.Abs((x + 0.5f) / width - 0.5f) * 2f;
                float across = (float)Math.Exp(-fromCenter * fromCenter * 5f);
                pixels[y * width + x] = new Color(1f, 1f, 1f, across * along);
            }
        }
        return CreateTexture(device, width, height, pixels);
    }

    static Texture2D CreateBaseGlow(GraphicsDevice device)
    {
        const int size = 64;
        var pixels = new Color[size * size];
        for (int y = 0; y < size; ++y)
        {
            for (int x = 0; x < size; ++x)
            {
                float dx = (x + 0.5f) / size * 2f - 1f;
                float dy = (y + 0.5f) / size * 2f - 1f;
                float distance = (float)Math.Sqrt(dx * dx + dy * dy);
                float glow = distance >= 1f ? 0f : (float)Math.Exp(-distance * distance * 4f) * (1f - distance);
                pixels[y * size + x] = new Color(1f, 1f, 1f, glow);
            }
        }
        return CreateTexture(device, size, size, pixels);
    }

    static Texture2D CreateTexture(GraphicsDevice device, int width, int height, Color[] pixels)
    {
        var texture = new Texture2D(device, width, height);
        texture.SetData(pixels);
        return texture;
    }

    static float SmoothEdge(float distanceFromEnd)
    {
        float t = (distanceFromEnd / 0.2f).Clamped(0f, 1f);
        return t * t * (3f - 2f * t);
    }
}
