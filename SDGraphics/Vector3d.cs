using System;
using System.Diagnostics.Contracts;
using SDUtils;

namespace SDGraphics;
using XnaVector2 = Microsoft.Xna.Framework.Vector2;
using XnaVector3 = Microsoft.Xna.Framework.Vector3;
using XnaMatrix = Microsoft.Xna.Framework.Matrix;

// Double precision Vector3
public struct Vector3d
{
    public double X;
    public double Y;
    public double Z;

    public static readonly Vector3d Zero = default;

    public Vector3d(double xyz)
    {
        X = xyz;
        Y = xyz;
        Z = xyz;
    }

    public Vector3d(double x, double y, double z)
    {
        X = x;
        Y = y;
        Z = z;
    }

    public Vector3d(in Vector3 v)
    {
        X = v.X;
        Y = v.Y;
        Z = v.Z;
    }

    public Vector3d(in XnaVector3 v)
    {
        X = v.X;
        Y = v.Y;
        Z = v.Z;
    }

    public Vector3d(in Vector2 v, double z)
    {
        X = v.X;
        Y = v.Y;
        Z = z;
    }

    public Vector3d(in XnaVector2 v, double z)
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

    [Pure] public Vector2 ToVec2f() => new Vector2((float)X, (float)Y);
    [Pure] public Vector3 ToVec3f() => new Vector3((float)X, (float)Y, (float)Z);
    [Pure] public Vector2d ToVec2d() => new Vector2d(X, Y);
    [Pure] public double Length() => Math.Sqrt(X*X + Y*Y + Z*Z);

    [Pure] public Vector3d Normalized(double newMagnitude)
    {
        double len = Math.Sqrt(X*X + Y*Y + Z*Z) / newMagnitude;
        return len > 0.0000001
            ? new Vector3d(X / len, Y / len, Z / len)
            : default;
    }

    [Pure] public Vector3d Normalized()
    {
        double len = Math.Sqrt(X*X + Y*Y + Z*Z);
        return len > 0.0000001
            ? new Vector3d(X / len, Y / len, Z / len)
            : default;
    }

    [Pure] public double Distance(in Vector3d b)
    {
        double dx = X - b.X;
        double dy = Y - b.Y;
        double dz = Z - b.Z;
        return Math.Sqrt(dx*dx + dy*dy + dz*dz);
    }

    [Pure] public Vector3d Transform(in Matrix matrix)
    {
        double x = (X*matrix.M11 + Y*matrix.M21 + Z*matrix.M31) + matrix.M41;
        double y = (X*matrix.M12 + Y*matrix.M22 + Z*matrix.M32) + matrix.M42;
        double z = (X*matrix.M13 + Y*matrix.M23 + Z*matrix.M33) + matrix.M43;
        return new Vector3d(x, y, z);
    }

    [Pure] public Vector3d Transform(in XnaMatrix matrix)
    {
        double x = (X*matrix.M11 + Y*matrix.M21 + Z*matrix.M31) + matrix.M41;
        double y = (X*matrix.M12 + Y*matrix.M22 + Z*matrix.M32) + matrix.M42;
        double z = (X*matrix.M13 + Y*matrix.M23 + Z*matrix.M33) + matrix.M43;
        return new Vector3d(x, y, z);
    }

    public static Vector3d operator+(in Vector3d a, in Vector3d b)
    {
        return new Vector3d(a.X + b.X, a.Y + b.Y, a.Z + b.Z);
    }
    public static Vector3d operator-(in Vector3d a, in Vector3d b)
    {
        return new Vector3d(a.X - b.X, a.Y - b.Y, a.Z - b.Z);
    }
    public static Vector3d operator*(in Vector3d a, in Vector3d b)
    {
        return new Vector3d(a.X * b.X, a.Y * b.Y, a.Z * b.Z);
    }
    public static Vector3d operator/(in Vector3d a, in Vector3d b)
    {
        return new Vector3d(a.X / b.X, a.Y / b.Y, a.Z / b.Z);
    }

    public static Vector3d operator+(in Vector3d a, double f)
    {
        return new Vector3d(a.X + f, a.Y + f, a.Z + f);
    }
    public static Vector3d operator-(in Vector3d a, double f)
    {
        return new Vector3d(a.X - f, a.Y - f, a.Z - f);
    }
    public static Vector3d operator*(in Vector3d a, double f)
    {
        return new Vector3d(a.X * f, a.Y * f, a.Z * f);
    }
    public static Vector3d operator/(in Vector3d a, double f)
    {
        return new Vector3d(a.X / f, a.Y / f, a.Z / f);
    }

    public static Vector3d operator +(double f, in Vector3d b)
    {
        return new Vector3d(f + b.X, f + b.Y, f + b.Z);
    }
    public static Vector3d operator -(double f, in Vector3d b)
    {
        return new Vector3d(f - b.X, f - b.Y, f - b.Z);
    }
    public static Vector3d operator *(double f, in Vector3d b)
    {
        return new Vector3d(f * b.X, f * b.Y, f * b.Z);
    }
    public static Vector3d operator /(double f, in Vector3d b)
    {
        return new Vector3d(f / b.X, f / b.Y, f / b.Z);
    }
}
