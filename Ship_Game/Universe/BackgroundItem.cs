using System;
using Microsoft.Xna.Framework.Graphics;
using SDGraphics;
using SDGraphics.Sprites;

namespace Ship_Game
{
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
            Texture2D tex = SubTex.Texture;
            float tx = SubTex.X / (float)tex.Width;
            float ty = SubTex.Y / (float)tex.Height;
            float tw = (SubTex.Width - 1) / (float)tex.Width;
            float th = (SubTex.Height - 1) / (float)tex.Height;
            var coords = new RectF(tx, ty, tw, th);

            renderer.Draw(tex, Rect, Z, coords, color);
        }
    }
}