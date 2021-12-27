using System;
using System.Diagnostics.Contracts;
using Microsoft.Xna.Framework;

namespace Ship_Game
{
    // Double precision Vector2
    public struct Vector2d
    {
        public double X;
        public double Y;

        public static readonly Vector2d Zero = default;

        public Vector2d(double x, double y)
        {
            X = x;
            Y = y;
        }

        public Vector2d(in Vector2 v)
        {
            X = v.X;
            Y = v.Y;
        }

        public Vector2d(double xy)
        {
            X = xy;
            Y = xy;
        }

        public override string ToString()
        {
            return string.Format($"{{X:{X:0.###} Y:{Y:0.###}}}");
        }

        [Pure] public Vector2 ToVec2f() => new Vector2((float)X, (float)Y);
        [Pure] public Vector2 ToVec2fRounded() => new Vector2((float)Math.Round(X), (float)Math.Round(Y));
        [Pure] public double Length() => Math.Sqrt(X*X + Y*Y);

        [Pure] public Vector2d Normalized()
        {
            double len = Math.Sqrt(X*X + Y*Y);
            return len > 0.000001f ? new Vector2d(X / len, Y / len) : default;
        }

        [Pure] public double Distance(in Vector2d b)
        {
            double dx = X - b.X;
            double dy = Y - b.Y;
            return Math.Sqrt(dx*dx + dy*dy);
        }

        [Pure] public Vector2d DirectionToTarget(Vector2d target)
        {
            double dx = target.X - X;
            double dy = target.Y - Y;
            double len = Math.Sqrt(dx*dx + dy*dy);
            if (len.AlmostZero())
                return new Vector2d(0.0, -1.0); // UP
            return new Vector2d(dx / len, dy / len);
        }

        // result between [0, +2PI)
        [Pure] public float RadiansToTarget(Vector2d target)
        {
            return (float)(Math.PI - Math.Atan2(target.X - X, target.Y - Y));
        }

        // Generates a new point on a circular radius from position
        // Input angle is given in degrees
        public Vector2d PointFromAngle(double degrees, double circleRadius)
        {
            Vector2d offset = degrees.AngleToDirection() * circleRadius;
            return new Vector2d(X + offset.X, Y + offset.Y);
        }

        // assuming this is a direction vector, gives the right side perpendicular vector
        // @note This assumes that +Y is DOWNWARDS on the screen
        [Pure] public Vector2d LeftVector()
        {
            return new Vector2d(Y, -X);
        }

        // assuming this is a direction vector, gives the left side perpendicular vector
        // @note This assumes that +Y is DOWNWARDS on the screen
        [Pure] public Vector2d RightVector()
        {
            return new Vector2d(-Y, X);
        }

        public static Vector2d operator+(in Vector2d a, in Vector2d b)
        {
            return new Vector2d(a.X + b.X, a.Y + b.Y);
        }
        public static Vector2d operator-(in Vector2d a, in Vector2d b)
        {
            return new Vector2d(a.X - b.X, a.Y - b.Y);
        }
        public static Vector2d operator*(in Vector2d a, in Vector2d b)
        {
            return new Vector2d(a.X * b.X, a.Y * b.Y);
        }
        public static Vector2d operator/(in Vector2d a, in Vector2d b)
        {
            return new Vector2d(a.X / b.X, a.Y / b.Y);
        }

        public static Vector2d operator+(in Vector2d a, double f)
        {
            return new Vector2d(a.X + f, a.Y + f);
        }
        public static Vector2d operator-(in Vector2d a, double f)
        {
            return new Vector2d(a.X - f, a.Y - f);
        }
        public static Vector2d operator*(in Vector2d a, double f)
        {
            return new Vector2d(a.X * f, a.Y * f);
        }
        public static Vector2d operator/(in Vector2d a, double f)
        {
            return new Vector2d(a.X / f, a.Y / f);
        }
    }
}
