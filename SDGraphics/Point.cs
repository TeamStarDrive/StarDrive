using System;
using System.Collections.Generic;
using System.Diagnostics.Contracts;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SDGraphics
{
    public struct Point : IEquatable<Point>
    {
        public int X;
        public int Y;

        public static readonly Point Zero = new Point();

        public Point(int x, int y)
        {
            X = x;
            Y = y;
        }

        public bool Equals(Point other) => X == other.X && Y == other.Y;

        public override bool Equals(object obj)
        {
            bool flag = false;
            if (obj is Point other)
                flag = Equals(other);
            return flag;
        }

        public override int GetHashCode() => X.GetHashCode() + Y.GetHashCode();

        public override string ToString()
        {
            CultureInfo c = CultureInfo.CurrentCulture;
            return string.Format(c, "{{X:{0} Y:{1}}}", X.ToString(c), Y.ToString(c));
        }

        public static bool operator ==(Point a, Point b) => a.Equals(b);
        public static bool operator !=(Point a, Point b) => a.X != b.X || a.Y != b.Y;

        [Pure]
        public readonly bool IsDiagonalTo(Point b)
        {
            return Math.Abs(b.X - X) > 0 && Math.Abs(b.Y - Y) > 0;
        }
    }
}
