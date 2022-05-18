using System;
using System.Diagnostics.Contracts;
using SDUtils;

namespace SDGraphics;
using XnaVector2 = Microsoft.Xna.Framework.Vector2;
using XnaMatrix = Microsoft.Xna.Framework.Matrix;

public struct Vector2 : IEquatable<Vector2>
{
    public float X;
    public float Y;

    public Vector2(float x, float y)
    {
        X = x;
        Y = y;
    }

    public Vector2(double x, double y)
    {
        X = (float)x;
        Y = (float)y;
    }

    public Vector2(float xy)
    {
        X = xy;
        Y = xy;
    }

    public Vector2(XnaVector2 v)
    {
        X = v.X;
        Y = v.Y;
    }

    public Vector2(Point p)
    {
        X = p.X;
        Y = p.Y;
    }

    public static readonly Vector2 Zero = default;
    public static readonly Vector2 One = new(1f, 1f);

    public static readonly Vector2 UnitX = new(1f, 0f);
    public static readonly Vector2 UnitY = new(0f, 1f);

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
        return $"[{X:0.####}, {Y:0.####}]";
    }

    public string ToString(int precision)
    {
        return $"[{X.String(precision)}, {Y.String(precision)}]";
    }

    public static implicit operator XnaVector2(in Vector2 v)
    {
        return new XnaVector2(v.X, v.Y);
    }

    [Pure] public readonly Vector2 ToRounded() => new((float)Math.Round(X), (float)Math.Round(Y));
    [Pure] public readonly Vector2 ToFloored() => new((float)Math.Floor(X), (float)Math.Floor(Y));
    [Pure] public readonly float Length() => (float)Math.Sqrt(X*X + Y*Y);
    [Pure] public readonly float SqLen() => X*X + Y*Y;

    // Widens this Vector2 to a Vector3, the new Z component will have a value of 0f
    [Pure] public readonly Vector3 ToVec3() => new(X, Y, 0f);

    // Widens this Vector2 to a Vector3, the new Z component is provided as argument
    [Pure] public readonly Vector3 ToVec3(float z) => new(X, Y, z);
    [Pure] public readonly Vector3d ToVec3d(double z) => new(X, Y, z);

    [Pure] public readonly Vector2 Normalized()
    {
        double len = Math.Sqrt(X*X + Y*Y);
        return len > 0.000001 ? new Vector2((float)(X / len), (float)(Y / len)) : default;
    }
    
    [Pure] public readonly Vector2 Normalized(float newMagnitude)
    {
        double len = Math.Sqrt(X*X + Y*Y) / newMagnitude;
        return len > 0.000001 ? new Vector2((float)(X / len), (float)(Y / len)) : default;
    }

    [Pure] public readonly (Vector2, float) GetDirectionAndLength()
    {
        float l = (float)Math.Sqrt(X*X + Y*Y);
        return (l > 0.0000001f ? new Vector2(X / l, Y / l) : default, l);
    }

    // Equivalent to:
    // Vector2 direction = origin.DirectionToTarget(target);
    // float distance = origin.Distance(target);
    [Pure] public readonly (Vector2, float) GetDirectionAndLength(in Vector2 target)
    {
        double dx = target.X - X;
        double dy = target.Y - Y;
        double len = Math.Sqrt(dx*dx + dy*dy);
        if (len.AlmostZero())
            return (Zero, (float)len);
        return (new Vector2((dx / len), (dy / len)), (float)len);
    }

    // Gets the accurate distance from source point a to destination b
    // This is slower than Vector2.SqDist()
    [Pure] public readonly float Distance(in Vector2 b)
    {
        float dx = X - b.X;
        float dy = Y - b.Y;
        return (float)Math.Sqrt(dx*dx + dy*dy);
    }
    
    // Gets the Squared distance from source point a to destination b
    // This is faster than Vector2.Distance()
    [Pure] public readonly float SqDist(in Vector2 b)
    {
        float dx = X - b.X;
        float dy = Y - b.Y;
        return (dx*dx + dy*dy);
    }
    
    // True if this given position is within the radius of Circle [center,radius]
    [Pure] public readonly bool InRadius(Vector2 center, float radius)
    {
         return SqDist(center) <= radius*radius;
    }
    
    /// <summary>
    /// Geometric explanation of the Dot product
    /// When using two unit (direction) vectors a and b,
    /// the dot product will give [-1; +1] relation of these two vectors, where 
    /// +1: a and b are pointing in the same direction  --> -->
    /// 0: a and b are perpendicular, not specifying if left or right, |_ or _|
    /// -1: a and b are point in opposite directions --> <-- 
    /// </summary>
    [Pure]
    public readonly float Dot(Vector2 b) => X*b.X + Y*b.Y;

    [Pure] public readonly Vector2 DirectionToTarget(Vector2 target)
    {
        double dx = target.X - X;
        double dy = target.Y - Y;
        double len = Math.Sqrt(dx*dx + dy*dy);
        if (len.AlmostZero())
            return new Vector2(0.0f, -1.0f); // UP
        return new Vector2(dx / len, dy / len);
    }

    // result between [0, +2PI)
    [Pure] public readonly float RadiansToTarget(Vector2 target)
    {
        return (float)(Math.PI - Math.Atan2(target.X - X, target.Y - Y));
    }

    // Generates a new point on a circular radius from position
    // Input angle is given in degrees
    public readonly Vector2 PointFromAngle(float degrees, float circleRadius)
    {
        Vector2 offset = degrees.AngleToDirection() * circleRadius;
        return new Vector2(X + offset.X, Y + offset.Y);
    }

    // assuming this is a direction vector, gives the right side perpendicular vector
    // @note This assumes that +Y is DOWNWARDS on the screen
    [Pure] public readonly Vector2 LeftVector()
    {
        return new Vector2(Y, -X);
    }

    // assuming this is a direction vector, gives the left side perpendicular vector
    // @note This assumes that +Y is DOWNWARDS on the screen
    [Pure] public readonly Vector2 RightVector()
    {
        return new Vector2(-Y, X);
    }

    public static Vector2 Max(in Vector2 value1, in Vector2 value2)
    {
      Vector2 vector2;
      vector2.X = (double) value1.X > (double) value2.X ? value1.X : value2.X;
      vector2.Y = (double) value1.Y > (double) value2.Y ? value1.Y : value2.Y;
      return vector2;
    }

    [Pure]
    public readonly Vector2 Transform(in Matrix matrix)
    {
        return new Vector2((X * matrix.M11 + Y * matrix.M21) + matrix.M41,
                           (X * matrix.M12 + Y * matrix.M22) + matrix.M42);
    }

    [Pure]
    public readonly Vector2 Transform(in XnaMatrix matrix)
    {
        return new Vector2((X * matrix.M11 + Y * matrix.M21) + matrix.M41,
                           (X * matrix.M12 + Y * matrix.M22) + matrix.M42);
    }

    public static Vector2 operator -(in Vector2 a)
    {
        return new Vector2(-a.X, -a.Y);
    }

    public static Vector2 operator +(in Vector2 a, in Vector2 b)
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

    public static bool operator==(in Vector2 a, in Vector2 b)
    {
        return a.X == b.X && a.Y == b.Y;
    }

    public static bool operator!=(in Vector2 a, in Vector2 b)
    {
        return a.X != b.X || a.Y != b.Y;
    }

    public bool Equals(Vector2 other)
    {
        return X == other.X && Y == other.Y;
    }

    public override bool Equals(object obj)
    {
        return obj is Vector2 other && Equals(other);
    }

    public override int GetHashCode()
    {
        unchecked
        {
            return X.GetHashCode() + Y.GetHashCode();
        }
    }
}