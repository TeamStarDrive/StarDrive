using System;
using SDUtils;
using SDGraphics;

namespace Ship_Game.UI
{
    /// <summary>
    /// Represents a relative UI position which
    /// depends on the Parent element's Pos AND Size (!)
    ///
    /// These coordinates are in RELATIVE values
    ///
    /// Example:
    ///   Parent AbsPos = 100,100  AbsSize = 40,40
    ///     Child RelPos = 0.25,0.25  --> AbsPos = 110,110
    /// </summary>
    public struct RelPos : IEquatable<RelPos>
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

        public Vector2 ToVec2() => new Vector2(X, Y);

        public override string ToString() => $"RelX:{X.String(2)} RelY:{Y.String(2)}";

        public static RelPos operator+(in RelPos a, in RelPos b)
        {
            return new RelPos(a.X + b.X, a.Y + b.Y);
        }

        public RelPos Add(float x, float y)
        {
            return new RelPos(X + x, Y + y);
        }

        public static bool operator==(in RelPos a, in RelPos b)
        {
            return a.X == b.X && a.Y == b.Y;
        }

        public static bool operator!=(in RelPos a, in RelPos b)
        {
            return a.X != b.X || a.Y != b.Y;
        }

        public bool Equals(RelPos other)
        {
            return X.Equals(other.X) && Y.Equals(other.Y);
        }

        public override bool Equals(object obj)
        {
            return obj is RelPos other && Equals(other);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                return (X.GetHashCode() * 397) ^ Y.GetHashCode();
            }
        }
    }
}
