using Microsoft.Xna.Framework.Graphics;
using SDGraphics;
using SDGraphics.Rendering;
using SDGraphics.Sprites;

namespace Ship_Game;

public sealed class BackgroundItem
{
    public readonly SubTexture SubTex;
    public readonly RectF Rect;
    public readonly float Z;

    public BackgroundItem(SubTexture subTex, in RectF rect, float z)
    {
        SubTex = subTex;
        Rect = rect;
        Z = z;
    }

    public void Draw(SpriteRenderer renderer, Color color)
    {
        renderer.Draw(SubTex.Texture, new Quad3D(Rect, Z), SubTex.UVCoords, color);
    }
}
