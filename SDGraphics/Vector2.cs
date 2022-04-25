using System;
using System.Diagnostics.Contracts;
using SDUtils;

namespace SDGraphics;

public struct Vector2
{
    public float X;
    public float Y;

    public Vector2(float x, float y)
    {
        X = x;
        Y = y;
    }

    public Vector2(float xy)
    {
        X = xy;
        Y = xy;
    }

    public static readonly Vector2 Zero = default;

    // Up in the world is -Y, down is +Y
    public static readonly Vector2 Up = new(0, -1);
    public static readonly Vector2 Down = new(0, 1);
    public static readonly Vector2 Left = new(-1, 0);
    public static readonly Vector2 Right = new(1, 0);
    public static readonly Vector2 TopLeft = new(-1, -1);
    public static readonly Vector2 TopRight = new(+1, -1);
    public static readonly Vector2 BotLeft = new(-1, +1);
    public static readonly Vector2 BotRight = new(+1, +1);

    public override string ToString()
    {
        return $"[{X:0.###}, {Y:0.###}]";
    }

    public string ToString(int precision)
    {
        return $"[{X.String(precision)}, {Y.String(precision)}]";
    }

    public static implicit operator Microsoft.Xna.Framework.Vector2(in Vector2 v)
    {
        return new Microsoft.Xna.Framework.Vector2(v.X, v.Y);
    }

    [Pure] public Vector2 ToRounded() => new((float)Math.Round(X), (float)Math.Round(Y));
    [Pure] public float Length() => (float)Math.Sqrt(X*X + Y*Y);

    [Pure] public Vector2 Normalized()
    {
        double len = Math.Sqrt(X*X + Y*Y);
        return len > 0.000001 ? new Vector2((float)(X / len), (float)(Y / len)) : default;
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

    public static Vector2 operator+(in Vector2 a, in Vector2 b)
    {
        return new Vector2(a.X + b.X, a.Y + b.Y);
    }
    public static Vector2 operator -(in Vector2 a, in Vector2 b)
    {
        return new Vector2(a.X - b.X, a.Y - b.Y);
    }
    public static Vector2 operator *(in Vector2 a, in Vector2 b)
    {
        return new Vector2(a.X * b.X, a.Y * b.Y);
    }
    public static Vector2 operator /(in Vector2 a, in Vector2 b)
    {
        return new Vector2(a.X / b.X, a.Y / b.Y);
    }

    public static Vector2 operator +(in Vector2 a, float f)
    {
        return new Vector2(a.X + f, a.Y + f);
    }
    public static Vector2 operator -(in Vector2 a, float f)
    {
        return new Vector2(a.X - f, a.Y - f);
    }
    public static Vector2 operator *(in Vector2 a, float f)
    {
        return new Vector2(a.X * f, a.Y * f);
    }
    public static Vector2 operator /(in Vector2 a, float f)
    {
        return new Vector2(a.X / f, a.Y / f);
    }

    public static Vector2 operator +(float f, in Vector2 b)
    {
        return new Vector2(f + b.X, f + b.Y);
    }
    public static Vector2 operator -(float f, in Vector2 b)
    {
        return new Vector2(f - b.X, f - b.Y);
    }
    public static Vector2 operator *(float f, in Vector2 b)
    {
        return new Vector2(f * b.X, f * b.Y);
    }
    public static Vector2 operator /(float f, in Vector2 b)
    {
        return new Vector2(f / b.X, f / b.Y);
    }
}