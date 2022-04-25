using System;
using System.Diagnostics.Contracts;
using SDUtils;

namespace SDGraphics;

public struct Vector3
{
    public float X;
    public float Y;
    public float Z;

    public static readonly Vector3 Zero = default;

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

    public override string ToString()
    {
        return $"[{X:0.###}, {Y:0.###}, {Z:0.###}]";
    }

    public string ToString(int precision)
    {
        return $"[{X.String(precision)}, {Y.String(precision)}, {Z.String(precision)}]";
    }

    public static implicit operator Microsoft.Xna.Framework.Vector3(in Vector3 v)
    {
        return new Microsoft.Xna.Framework.Vector3(v.X, v.Y, v.Z);
    }

    [Pure] public float Length() => (float)Math.Sqrt(X * X + Y * Y + Z * Z);

    [Pure]
    public Vector3 Normalized(float newMagnitude)
    {
        float len = (float)Math.Sqrt(X * X + Y * Y + Z * Z) / newMagnitude;
        return len > 0.0000001
            ? new Vector3((X / len), (Y / len), (Z / len))
            : default;
    }

    [Pure]
    public Vector3 Normalized()
    {
        float len = (float)Math.Sqrt(X * X + Y * Y + Z * Z);
        return len > 0.0000001
            ? new Vector3((X / len), (Y / len), (Z / len))
            : default;
    }

    [Pure]
    public float Distance(in Vector3 b)
    {
        float dx = X - b.X;
        float dy = Y - b.Y;
        float dz = Z - b.Z;
        return (float)Math.Sqrt(dx * dx + dy * dy + dz * dz);
    }

    [Pure]
    public Vector3 Transform(in Matrix matrix)
    {
        float x = (X * matrix.M11 + Y * matrix.M21 + Z * matrix.M31) + matrix.M41;
        float y = (X * matrix.M12 + Y * matrix.M22 + Z * matrix.M32) + matrix.M42;
        float z = (X * matrix.M13 + Y * matrix.M23 + Z * matrix.M33) + matrix.M43;
        return new Vector3(x, y, z);
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
}