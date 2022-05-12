using System;
using System.Diagnostics.Contracts;
using SDUtils;

namespace SDGraphics;
using XnaVector3 = Microsoft.Xna.Framework.Vector3;
using XnaMatrix = Microsoft.Xna.Framework.Matrix;

public struct Vector3 : IEquatable<Vector3>
{
    public float X;
    public float Y;
    public float Z;

    public Vector3(float xyz)
    {
        X = xyz;
        Y = xyz;
        Z = xyz;
    }

    public Vector3(float x, float y, float z)
    {
        X = x;
        Y = y;
        Z = z;
    }

    public Vector3(in Vector2 v, float z)
    {
        X = v.X;
        Y = v.Y;
        Z = z;
    }

    public Vector3(in Vector2 v)
    {
        X = v.X;
        Y = v.Y;
        Z = 0f;
    }

    public Vector3(in XnaVector3 v)
    {
        X = v.X;
        Y = v.Y;
        Z = v.Z;
    }

    public static readonly Vector3 Zero = default;
    public static readonly Vector3 One = new(1f, 1f, 1f);

    public static readonly Vector3 UnitX = new(1f, 0f, 0f);
    public static readonly Vector3 UnitY = new(0f, 1f, 0f);
    public static readonly Vector3 UnitZ = new(0f, 0f, 1f);

    public static readonly Vector3 Up = new(0f, 1f, 0f);
    public static readonly Vector3 Down = new(0f, -1f, 0f);
    public static readonly Vector3 Right = new(1f, 0f, 0f);
    public static readonly Vector3 Left = new(-1f, 0f, 0f);
    public static readonly Vector3 Forward = new(0f, 0f, -1f);
    public static readonly Vector3 Backward = new(0f, 0f, 1f);

    public override string ToString()
    {
        return $"[{X:0.###}, {Y:0.###}, {Z:0.###}]";
    }

    public string ToString(int precision)
    {
        return $"[{X.String(precision)}, {Y.String(precision)}, {Z.String(precision)}]";
    }

    public static implicit operator XnaVector3(in Vector3 v)
    {
        return new XnaVector3(v.X, v.Y, v.Z);
    }

    public static explicit operator Vector3(in XnaVector3 v)
    {
        return new Vector3(v.X, v.Y, v.Z);
    }

    [Pure] public readonly float Length() => (float)Math.Sqrt(X * X + Y * Y + Z * Z);
    
    // Narrows this Vector3 to a Vector2, the Z component is truncated
    [Pure] public readonly Vector2 ToVec2() => new(X, Y);

    [Pure]
    public readonly Vector3 Normalized(float newMagnitude)
    {
        float len = (float)Math.Sqrt(X * X + Y * Y + Z * Z) / newMagnitude;
        return len > 0.0000001
            ? new Vector3((X / len), (Y / len), (Z / len))
            : default;
    }

    [Pure]
    public readonly Vector3 Normalized()
    {
        float len = (float)Math.Sqrt(X * X + Y * Y + Z * Z);
        return len > 0.0000001
            ? new Vector3((X / len), (Y / len), (Z / len))
            : default;
    }
    
    // Gets the accurate distance from source point a to destination b
    // This is slower than Vector3.SqDist()
    [Pure]
    public readonly float Distance(in Vector3 b)
    {
        float dx = X - b.X;
        float dy = Y - b.Y;
        float dz = Z - b.Z;
        return (float)Math.Sqrt(dx * dx + dy * dy + dz * dz);
    }

    // Squared distance between two Vector3's
    [Pure] public readonly float SqDist(in Vector3 b)
    {
        float dx = X - b.X;
        float dy = Y - b.Y;
        float dz = Z - b.Z;
        return dx*dx + dy*dy + dz*dz;
    }

    [Pure]
    public readonly Vector3 DirectionToTarget(in Vector3 target)
    {
        double dx = target.X - X;
        double dy = target.Y - Y;
        double dz = target.Z - Z;
        double len = Math.Sqrt(dx*dx + dy*dy + dz*dz);
        if (len.AlmostZero())
            return new Vector3(0f, -1f, 0f); // UP
        return new Vector3((float)(dx / len), (float)(dy / len), (float)(dz / len));
    }

    /// <summary>
    /// 3D Version of the Dot product, +1 same dir, 0 perpendicular in some axis, -1 opposite dirs
    /// </summary>
    [Pure]
    public readonly float Dot(in Vector3 b) => X*b.X + Y*b.Y + Z*b.Z;

    [Pure]
    public readonly Vector3 Transform(in Matrix matrix)
    {
        float x = (X * matrix.M11 + Y * matrix.M21 + Z * matrix.M31) + matrix.M41;
        float y = (X * matrix.M12 + Y * matrix.M22 + Z * matrix.M32) + matrix.M42;
        float z = (X * matrix.M13 + Y * matrix.M23 + Z * matrix.M33) + matrix.M43;
        return new Vector3(x, y, z);
    }

    [Pure]
    public readonly Vector3 Transform(in XnaMatrix matrix)
    {
        float x = (X * matrix.M11 + Y * matrix.M21 + Z * matrix.M31) + matrix.M41;
        float y = (X * matrix.M12 + Y * matrix.M22 + Z * matrix.M32) + matrix.M42;
        float z = (X * matrix.M13 + Y * matrix.M23 + Z * matrix.M33) + matrix.M43;
        return new Vector3(x, y, z);
    }

    public static Vector3 Cross(in Vector3 a, in Vector3 b)
    {
        Vector3 v;
        v.X = (a.Y * b.Z - a.Z * b.Y);
        v.Y = (a.Z * b.X - a.X * b.Z);
        v.Z = (a.X * b.Y - a.Y * b.X);
        return v;
    }

    [Pure]
    public readonly Vector3 Cross(in Vector3 b)
    {
        Vector3 v;
        v.X = (Y * b.Z - Z * b.Y);
        v.Y = (Z * b.X - X * b.Z);
        v.Z = (X * b.Y - Y * b.X);
        return v;
    }

    [Pure]
    public readonly Vector3 Lerp(in Vector3 to, float amount)
    {
        Vector3 v;
        v.X = X + (to.X - X) * amount;
        v.Y = Y + (to.Y - Y) * amount;
        v.Z = Z + (to.Z - Z) * amount;
        return v;
    }

    public static Vector3 operator -(in Vector3 a)
    {
        return new Vector3(-a.X, -a.Y, -a.Z);
    }

    public static Vector3 operator +(in Vector3 a, in Vector3 b)
    {
        return new Vector3(a.X + b.X, a.Y + b.Y, a.Z + b.Z);
    }
    public static Vector3 operator -(in Vector3 a, in Vector3 b)
    {
        return new Vector3(a.X - b.X, a.Y - b.Y, a.Z - b.Z);
    }
    public static Vector3 operator *(in Vector3 a, in Vector3 b)
    {
        return new Vector3(a.X * b.X, a.Y * b.Y, a.Z * b.Z);
    }
    public static Vector3 operator /(in Vector3 a, in Vector3 b)
    {
        return new Vector3(a.X / b.X, a.Y / b.Y, a.Z / b.Z);
    }

    public static Vector3 operator +(in Vector3 a, float f)
    {
        return new Vector3(a.X + f, a.Y + f, a.Z + f);
    }
    public static Vector3 operator -(in Vector3 a, float f)
    {
        return new Vector3(a.X - f, a.Y - f, a.Z - f);
    }
    public static Vector3 operator *(in Vector3 a, float f)
    {
        return new Vector3(a.X * f, a.Y * f, a.Z * f);
    }
    public static Vector3 operator /(in Vector3 a, float f)
    {
        return new Vector3(a.X / f, a.Y / f, a.Z / f);
    }

    public static Vector3 operator +(float f, in Vector3 b)
    {
        return new Vector3(f + b.X, f + b.Y, f + b.Z);
    }
    public static Vector3 operator -(float f, in Vector3 b)
    {
        return new Vector3(f - b.X, f - b.Y, f - b.Z);
    }
    public static Vector3 operator *(float f, in Vector3 b)
    {
        return new Vector3(f * b.X, f * b.Y, f * b.Z);
    }
    public static Vector3 operator /(float f, in Vector3 b)
    {
        return new Vector3(f / b.X, f / b.Y, f / b.Z);
    }

    
    public static bool operator==(in Vector3 a, in Vector3 b)
    {
        return a.X == b.X && a.Y == b.Y && a.Z == b.Z;
    }

    public static bool operator!=(in Vector3 a, in Vector3 b)
    {
        return a.X != b.X || a.Y != b.Y || a.Z != b.Z;
    }

    public bool Equals(Vector3 other)
    {
        return X == other.X && Y == other.Y && Z == other.Z;
    }

    public override bool Equals(object obj)
    {
        return obj is Vector3 other && Equals(other);
    }

    public override int GetHashCode()
    {
        unchecked
        {
            return X.GetHashCode() + Y.GetHashCode() + Z.GetHashCode();
        }
    }
}