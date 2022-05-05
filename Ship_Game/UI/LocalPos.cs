using System;
using SDUtils;
using SDGraphics;

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
    ///     Child LocalPos = 15,15  --> AbsPos = 115,115
    /// </summary>
    public struct LocalPos : IEquatable<LocalPos>
    {
        public float X;
        public float Y;

        public static LocalPos Zero = default;

        public LocalPos(float x, float y)
        {
            X = x;
            Y = y;
        }

        public LocalPos(Vector2 pos)
        {
            X = pos.X;
            Y = pos.Y;
        }

        public Vector2 ToVec2() => new Vector2(X, Y);

        public override string ToString() => $"LocalX:{X.String(2)} LocalY:{Y.String(2)}";

        public static LocalPos operator+(in LocalPos a, in LocalPos b)
        {
            return new LocalPos(a.X + b.X, a.Y + b.Y);
        }

        public LocalPos Add(float x, float y)
        {
            return new LocalPos(X + x, Y + y);
        }

        public static bool operator==(in LocalPos a, in LocalPos b)
        {
            return a.X == b.X && a.Y == b.Y;
        }

        public static bool operator!=(in LocalPos a, in LocalPos b)
        {
            return a.X != b.X || a.Y != b.Y;
        }

        public bool Equals(LocalPos other)
        {
            return X.Equals(other.X) && Y.Equals(other.Y);
        }

        public override bool Equals(object obj)
        {
            return obj is LocalPos other && Equals(other);
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
