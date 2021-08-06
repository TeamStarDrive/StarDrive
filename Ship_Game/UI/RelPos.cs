using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Xna.Framework;

namespace Ship_Game.UI
{
    /// <summary>
    /// Represents a relative UI position which
    /// depends on the Parent element's position
    ///
    /// These coordinates are in pixel values
    ///
    /// Example:
    ///   Parent AbsPos = 100,100
    ///     Child RelPos = 15,15  --> AbsPos = 115,115
    /// </summary>
    public struct RelPos
    {
        public float X;
        public float Y;

        public RelPos(float x, float y)
        {
            X = x;
            Y = y;
        }

        public RelPos(Vector2 pos)
        {
            X = pos.X;
            Y = pos.Y;
        }

        public override string ToString() => $"RelX:{X.String(2)} RelY:{Y.String(2)}";

        public static RelPos operator+(in RelPos a, in RelPos b)
        {
            return new RelPos(a.X + b.X, a.Y + b.Y);
        }

        public RelPos Add(float x, float y)
        {
            return new RelPos(X + x, Y + y);
        }
    }
}
