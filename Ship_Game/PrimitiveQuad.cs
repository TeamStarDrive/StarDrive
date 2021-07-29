using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace Ship_Game
{
    public sealed class PrimitiveQuad
    {
        public int X;
        public int Y;
        public int W;
        public int H;

        public Vector2 Position => new Vector2(X, Y);
        public Rectangle Rect => new Rectangle(X, Y, W, H);

        public PrimitiveQuad(float x, float y, float w, float h)
        {
            X = (int)x;
            Y = (int)y;
            W = (int)w;
            H = (int)h;
        }

        public bool Contains(Vector2 pos)
        {
            return pos.X > X && pos.X < (X + W)
                && pos.Y > Y && pos.Y < (Y + H);
        }
    }
}