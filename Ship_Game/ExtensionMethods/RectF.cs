using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Xna.Framework;

namespace Ship_Game
{
    public struct RectF
    {
        public float X, Y, W, H;

        public RectF(float x, float y, float w, float h)
        {
            X = x; Y = y; W = w; H = h;
        }

        public RectF(Vector2 pos, Vector2 size)
        {
            X = pos.X; Y = pos.Y; W = size.X; H = size.Y;
        }

        public RectF(in Rectangle r)
        {
            X = r.X; Y = r.Y; W = r.Width; H = r.Height;
        }

        public static implicit operator Rectangle(in RectF r)
        {
            return new Rectangle((int)r.X, (int)r.Y, (int)r.W, (int)r.H);
        }
    }
}
