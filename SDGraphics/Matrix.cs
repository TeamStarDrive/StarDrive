using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SDGraphics;

// 4x4 Affine transformation matrix, originally based on XNA for compatibility
public struct Matrix
{
    public float M11;
    public float M12;
    public float M13;
    public float M14;
    public float M21;
    public float M22;
    public float M23;
    public float M24;
    public float M31;
    public float M32;
    public float M33;
    public float M34;
    public float M41;
    public float M42;
    public float M43;
    public float M44;

    public static readonly Matrix Identity = new Matrix(1f, 0.0f, 0.0f, 0.0f, 0.0f, 1f, 0.0f, 0.0f, 0.0f, 0.0f, 1f, 0.0f, 0.0f, 0.0f, 0.0f, 1f);

    public Vector3 Up
    {
        get
        {
            Vector3 up;
            up.X = M21;
            up.Y = M22;
            up.Z = M23;
            return up;
        }
        set
        {
            M21 = value.X;
            M22 = value.Y;
            M23 = value.Z;
        }
    }

    public Vector3 Down
    {
        get
        {
            Vector3 down;
            down.X = -M21;
            down.Y = -M22;
            down.Z = -M23;
            return down;
        }
        set
        {
            M21 = -value.X;
            M22 = -value.Y;
            M23 = -value.Z;
        }
    }

    public Vector3 Right
    {
        get
        {
            Vector3 right;
            right.X = M11;
            right.Y = M12;
            right.Z = M13;
            return right;
        }
        set
        {
            M11 = value.X;
            M12 = value.Y;
            M13 = value.Z;
        }
    }

    public Vector3 Left
    {
        get
        {
            Vector3 left;
            left.X = -M11;
            left.Y = -M12;
            left.Z = -M13;
            return left;
        }
        set
        {
            M11 = -value.X;
            M12 = -value.Y;
            M13 = -value.Z;
        }
    }

    public Vector3 Forward
    {
        get
        {
            Vector3 forward;
            forward.X = -M31;
            forward.Y = -M32;
            forward.Z = -M33;
            return forward;
        }
        set
        {
            M31 = -value.X;
            M32 = -value.Y;
            M33 = -value.Z;
        }
    }

    public Vector3 Backward
    {
        get
        {
            Vector3 backward;
            backward.X = M31;
            backward.Y = M32;
            backward.Z = M33;
            return backward;
        }
        set
        {
            M31 = value.X;
            M32 = value.Y;
            M33 = value.Z;
        }
    }

    public Vector3 Translation
    {
        get
        {
            Vector3 translation;
            translation.X = M41;
            translation.Y = M42;
            translation.Z = M43;
            return translation;
        }
        set
        {
            M41 = value.X;
            M42 = value.Y;
            M43 = value.Z;
        }
    }

    public Matrix(
        float m11,
        float m12,
        float m13,
        float m14,
        float m21,
        float m22,
        float m23,
        float m24,
        float m31,
        float m32,
        float m33,
        float m34,
        float m41,
        float m42,
        float m43,
        float m44)
    {
        M11 = m11;
        M12 = m12;
        M13 = m13;
        M14 = m14;
        M21 = m21;
        M22 = m22;
        M23 = m23;
        M24 = m24;
        M31 = m31;
        M32 = m32;
        M33 = m33;
        M34 = m34;
        M41 = m41;
        M42 = m42;
        M43 = m43;
        M44 = m44;
    }

    public Matrix(in Microsoft.Xna.Framework.Matrix m)
    {
        M11 = m.M11;
        M12 = m.M12;
        M13 = m.M13;
        M14 = m.M14;
        M21 = m.M21;
        M22 = m.M22;
        M23 = m.M23;
        M24 = m.M24;
        M31 = m.M31;
        M32 = m.M32;
        M33 = m.M33;
        M34 = m.M34;
        M41 = m.M41;
        M42 = m.M42;
        M43 = m.M43;
        M44 = m.M44;
    }

    public void Multiply(in Matrix b, out Matrix result)
    {
        result.M11 = (M11 * b.M11 + M12 * b.M21 + M13 * b.M31 + M14 * b.M41);
        result.M12 = (M11 * b.M12 + M12 * b.M22 + M13 * b.M32 + M14 * b.M42);
        result.M13 = (M11 * b.M13 + M12 * b.M23 + M13 * b.M33 + M14 * b.M43);
        result.M14 = (M11 * b.M14 + M12 * b.M24 + M13 * b.M34 + M14 * b.M44);
        result.M21 = (M21 * b.M11 + M22 * b.M21 + M23 * b.M31 + M24 * b.M41);
        result.M22 = (M21 * b.M12 + M22 * b.M22 + M23 * b.M32 + M24 * b.M42);
        result.M23 = (M21 * b.M13 + M22 * b.M23 + M23 * b.M33 + M24 * b.M43);
        result.M24 = (M21 * b.M14 + M22 * b.M24 + M23 * b.M34 + M24 * b.M44);
        result.M31 = (M31 * b.M11 + M32 * b.M21 + M33 * b.M31 + M34 * b.M41);
        result.M32 = (M31 * b.M12 + M32 * b.M22 + M33 * b.M32 + M34 * b.M42);
        result.M33 = (M31 * b.M13 + M32 * b.M23 + M33 * b.M33 + M34 * b.M43);
        result.M34 = (M31 * b.M14 + M32 * b.M24 + M33 * b.M34 + M34 * b.M44);
        result.M41 = (M41 * b.M11 + M42 * b.M21 + M43 * b.M31 + M44 * b.M41);
        result.M42 = (M41 * b.M12 + M42 * b.M22 + M43 * b.M32 + M44 * b.M42);
        result.M43 = (M41 * b.M13 + M42 * b.M23 + M43 * b.M33 + M44 * b.M43);
        result.M44 = (M41 * b.M14 + M42 * b.M24 + M43 * b.M34 + M44 * b.M44);
    }
}
