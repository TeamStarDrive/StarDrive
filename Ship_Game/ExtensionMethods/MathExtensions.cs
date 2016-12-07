using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Xna.Framework;

namespace Ship_Game
{
    internal static class MathExtensions
    {
        // Added by RedFox
        public static float SqDist(this Vector2 a, Vector2 b)
        {
            float dista = a.X-b.X;
            float distb = a.Y-b.Y;
            return dista*dista + distb*distb;
        }
    }
}
