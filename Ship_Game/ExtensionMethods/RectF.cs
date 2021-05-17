using System;
using System.Diagnostics.Contracts;
using System.Runtime.CompilerServices;
using Microsoft.Xna.Framework;

namespace Ship_Game
{
    public struct RectF
    {
        public float X, Y, W, H;

        public float Left => X;
        public float Top => Y;
        public float Right => X + W;
        public float Bottom => Y + H;
        
        public Vector2 Center => new Vector2(X + W*0.5f, Y + H*0.5f);
        public Vector2 Size => new Vector2(W, H);

        public Vector2 TopLeft => new Vector2(X, Y);
        public Vector2 BotRight => new Vector2(X + W, Y + H);

        public override string ToString() => $"{{X:{X} Y:{Y} W:{W} H:{H}}}";

        public RectF(float x, float y, float w, float h)
        {
            X = x;
            Y = y;
            W = w;
            H = h;
        }

        public RectF(Vector2 pos, Vector2 size)
        {
            X = pos.X;
            Y = pos.Y;
            W = size.X;
            H = size.Y;
        }

        public RectF(in Rectangle r)
        {
            X = r.X;
            Y = r.Y;
            W = r.Width;
            H = r.Height;
        }

        /// <summary>
        /// This creates a rectangle from points X1Y1 X2Y2, instead of TopLeftXY+WidthHeight
        /// o------o- y1
        /// |      |
        /// |      |
        /// o------o- y2
        /// '      '
        /// x1     x2
        /// </summary>
        public static RectF FromPoints(float x1, float x2, float y1, float y2)
        {
            float w = x2 - x1;
            float h = y2 - y1;
            return new RectF(x1, y1, w, h);
        }

        public static implicit operator Rectangle(in RectF r)
        {
            return new Rectangle((int)r.X, (int)r.Y, (int)r.W, (int)r.H);
        }

        [Pure][MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool Overlaps(in RectF r)
        {
            return Left <= r.Right && Right > r.Left
                && Top <= r.Bottom && Bottom > r.Top;
        }
    }
}
