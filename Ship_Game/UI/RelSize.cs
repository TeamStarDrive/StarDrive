using System;
using Microsoft.Xna.Framework;

namespace Ship_Game.UI
{
    /// <summary>
    /// Represents a relative UI size which
    /// depends on the Parent element's size
    ///
    /// These sizes are in relative values from [0.0 ... 1.0]
    /// which corresponds to [0% ... 100%]
    ///
    /// Example:
    ///   Parent AbsSize = 100,100
    ///     Child RelSize = 0.25,0.25  --> AbsSize = 25,25
    /// </summary>
    public struct RelSize : IEquatable<RelSize>
    {
        public float W;
        public float H;

        public RelSize(float w, float h)
        {
            W = w;
            H = h;
        }

        public RelSize(Vector2 size)
        {
            W = size.X;
            H = size.Y;
        }

        public Vector2 ToVec2() => new Vector2(W, H);

        public override string ToString() => $"RelW:{W.String(2)} RelH:{H.String(2)}";

        public static bool operator==(in RelSize a, in RelSize b)
        {
            return a.W == b.W && a.H == b.H;
        }

        public static bool operator!=(in RelSize a, in RelSize b)
        {
            return a.W != b.W || a.H != b.H;
        }

        public bool Equals(RelSize other)
        {
            return W.Equals(other.W) && H.Equals(other.H);
        }

        public override bool Equals(object obj)
        {
            return obj is RelSize other && Equals(other);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                return (W.GetHashCode() * 397) ^ H.GetHashCode();
            }
        }
    }
}
