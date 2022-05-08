using System;
using System.Diagnostics.Contracts;
using System.Globalization;

using XnaQuat = Microsoft.Xna.Framework.Quaternion;
using XnaPlane = Microsoft.Xna.Framework.Plane;
using XnaMatrix = Microsoft.Xna.Framework.Matrix;

namespace SDGraphics;

// 4x4 Affine transformation matrix, originally based on XNA for compatibility
public struct Matrix : IEquatable<Matrix>
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

    public static readonly XnaMatrix XnaIdentity = XnaMatrix.Identity;

    public static unsafe implicit operator XnaMatrix(in Matrix v)
    {
        fixed (Matrix* pv = &v)
            return *(XnaMatrix*)pv;
    }

    public static unsafe explicit operator Matrix(in XnaMatrix v)
    {
        fixed (XnaMatrix* pv = &v)
            return *(Matrix*)pv;
    }

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

    public Matrix(in XnaMatrix m)
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

    [Pure]
    public static Matrix CreateTranslation(Vector3 position)
    {
        Matrix translation;
        translation.M11 = 1f;
        translation.M12 = 0.0f;
        translation.M13 = 0.0f;
        translation.M14 = 0.0f;
        translation.M21 = 0.0f;
        translation.M22 = 1f;
        translation.M23 = 0.0f;
        translation.M24 = 0.0f;
        translation.M31 = 0.0f;
        translation.M32 = 0.0f;
        translation.M33 = 1f;
        translation.M34 = 0.0f;
        translation.M41 = position.X;
        translation.M42 = position.Y;
        translation.M43 = position.Z;
        translation.M44 = 1f;
        return translation;
    }

    [Pure]
    public static void CreateTranslation(ref Vector3 position, out Matrix result)
    {
        result.M11 = 1f;
        result.M12 = 0.0f;
        result.M13 = 0.0f;
        result.M14 = 0.0f;
        result.M21 = 0.0f;
        result.M22 = 1f;
        result.M23 = 0.0f;
        result.M24 = 0.0f;
        result.M31 = 0.0f;
        result.M32 = 0.0f;
        result.M33 = 1f;
        result.M34 = 0.0f;
        result.M41 = position.X;
        result.M42 = position.Y;
        result.M43 = position.Z;
        result.M44 = 1f;
    }

    [Pure]
    public static Matrix CreateTranslation(float xPosition, float yPosition, float zPosition)
    {
        Matrix translation;
        translation.M11 = 1f;
        translation.M12 = 0.0f;
        translation.M13 = 0.0f;
        translation.M14 = 0.0f;
        translation.M21 = 0.0f;
        translation.M22 = 1f;
        translation.M23 = 0.0f;
        translation.M24 = 0.0f;
        translation.M31 = 0.0f;
        translation.M32 = 0.0f;
        translation.M33 = 1f;
        translation.M34 = 0.0f;
        translation.M41 = xPosition;
        translation.M42 = yPosition;
        translation.M43 = zPosition;
        translation.M44 = 1f;
        return translation;
    }

    [Pure]
    public static void CreateTranslation(
      float xPosition,
      float yPosition,
      float zPosition,
      out Matrix result)
    {
        result.M11 = 1f;
        result.M12 = 0.0f;
        result.M13 = 0.0f;
        result.M14 = 0.0f;
        result.M21 = 0.0f;
        result.M22 = 1f;
        result.M23 = 0.0f;
        result.M24 = 0.0f;
        result.M31 = 0.0f;
        result.M32 = 0.0f;
        result.M33 = 1f;
        result.M34 = 0.0f;
        result.M41 = xPosition;
        result.M42 = yPosition;
        result.M43 = zPosition;
        result.M44 = 1f;
    }

    [Pure]
    public static Matrix CreateScale(float xScale, float yScale, float zScale)
    {
        float num1 = xScale;
        float num2 = yScale;
        float num3 = zScale;
        Matrix scale;
        scale.M11 = num1;
        scale.M12 = 0.0f;
        scale.M13 = 0.0f;
        scale.M14 = 0.0f;
        scale.M21 = 0.0f;
        scale.M22 = num2;
        scale.M23 = 0.0f;
        scale.M24 = 0.0f;
        scale.M31 = 0.0f;
        scale.M32 = 0.0f;
        scale.M33 = num3;
        scale.M34 = 0.0f;
        scale.M41 = 0.0f;
        scale.M42 = 0.0f;
        scale.M43 = 0.0f;
        scale.M44 = 1f;
        return scale;
    }

    [Pure]
    public static void CreateScale(float xScale, float yScale, float zScale, out Matrix result)
    {
        float num1 = xScale;
        float num2 = yScale;
        float num3 = zScale;
        result.M11 = num1;
        result.M12 = 0.0f;
        result.M13 = 0.0f;
        result.M14 = 0.0f;
        result.M21 = 0.0f;
        result.M22 = num2;
        result.M23 = 0.0f;
        result.M24 = 0.0f;
        result.M31 = 0.0f;
        result.M32 = 0.0f;
        result.M33 = num3;
        result.M34 = 0.0f;
        result.M41 = 0.0f;
        result.M42 = 0.0f;
        result.M43 = 0.0f;
        result.M44 = 1f;
    }

    [Pure]
    public static Matrix CreateScale(Vector3 scales)
    {
        float x = scales.X;
        float y = scales.Y;
        float z = scales.Z;
        Matrix scale;
        scale.M11 = x;
        scale.M12 = 0.0f;
        scale.M13 = 0.0f;
        scale.M14 = 0.0f;
        scale.M21 = 0.0f;
        scale.M22 = y;
        scale.M23 = 0.0f;
        scale.M24 = 0.0f;
        scale.M31 = 0.0f;
        scale.M32 = 0.0f;
        scale.M33 = z;
        scale.M34 = 0.0f;
        scale.M41 = 0.0f;
        scale.M42 = 0.0f;
        scale.M43 = 0.0f;
        scale.M44 = 1f;
        return scale;
    }

    [Pure]
    public static void CreateScale(ref Vector3 scales, out Matrix result)
    {
        float x = scales.X;
        float y = scales.Y;
        float z = scales.Z;
        result.M11 = x;
        result.M12 = 0.0f;
        result.M13 = 0.0f;
        result.M14 = 0.0f;
        result.M21 = 0.0f;
        result.M22 = y;
        result.M23 = 0.0f;
        result.M24 = 0.0f;
        result.M31 = 0.0f;
        result.M32 = 0.0f;
        result.M33 = z;
        result.M34 = 0.0f;
        result.M41 = 0.0f;
        result.M42 = 0.0f;
        result.M43 = 0.0f;
        result.M44 = 1f;
    }

    [Pure]
    public static Matrix CreateScale(float scale)
    {
        float num = scale;
        Matrix scale1;
        scale1.M11 = num;
        scale1.M12 = 0.0f;
        scale1.M13 = 0.0f;
        scale1.M14 = 0.0f;
        scale1.M21 = 0.0f;
        scale1.M22 = num;
        scale1.M23 = 0.0f;
        scale1.M24 = 0.0f;
        scale1.M31 = 0.0f;
        scale1.M32 = 0.0f;
        scale1.M33 = num;
        scale1.M34 = 0.0f;
        scale1.M41 = 0.0f;
        scale1.M42 = 0.0f;
        scale1.M43 = 0.0f;
        scale1.M44 = 1f;
        return scale1;
    }

    [Pure]
    public static void CreateScale(float scale, out Matrix result)
    {
        float num = scale;
        result.M11 = num;
        result.M12 = 0.0f;
        result.M13 = 0.0f;
        result.M14 = 0.0f;
        result.M21 = 0.0f;
        result.M22 = num;
        result.M23 = 0.0f;
        result.M24 = 0.0f;
        result.M31 = 0.0f;
        result.M32 = 0.0f;
        result.M33 = num;
        result.M34 = 0.0f;
        result.M41 = 0.0f;
        result.M42 = 0.0f;
        result.M43 = 0.0f;
        result.M44 = 1f;
    }

    [Pure]
    public static Matrix CreateRotationX(float radians)
    {
        float num1 = (float)Math.Cos(radians);
        float num2 = (float)Math.Sin(radians);
        Matrix rotationX;
        rotationX.M11 = 1f;
        rotationX.M12 = 0.0f;
        rotationX.M13 = 0.0f;
        rotationX.M14 = 0.0f;
        rotationX.M21 = 0.0f;
        rotationX.M22 = num1;
        rotationX.M23 = num2;
        rotationX.M24 = 0.0f;
        rotationX.M31 = 0.0f;
        rotationX.M32 = -num2;
        rotationX.M33 = num1;
        rotationX.M34 = 0.0f;
        rotationX.M41 = 0.0f;
        rotationX.M42 = 0.0f;
        rotationX.M43 = 0.0f;
        rotationX.M44 = 1f;
        return rotationX;
    }

    [Pure]
    public static void CreateRotationX(float radians, out Matrix result)
    {
        float num1 = (float)Math.Cos(radians);
        float num2 = (float)Math.Sin(radians);
        result.M11 = 1f;
        result.M12 = 0.0f;
        result.M13 = 0.0f;
        result.M14 = 0.0f;
        result.M21 = 0.0f;
        result.M22 = num1;
        result.M23 = num2;
        result.M24 = 0.0f;
        result.M31 = 0.0f;
        result.M32 = -num2;
        result.M33 = num1;
        result.M34 = 0.0f;
        result.M41 = 0.0f;
        result.M42 = 0.0f;
        result.M43 = 0.0f;
        result.M44 = 1f;
    }

    [Pure]
    public static Matrix CreateRotationY(float radians)
    {
        float num1 = (float)Math.Cos(radians);
        float num2 = (float)Math.Sin(radians);
        Matrix rotationY;
        rotationY.M11 = num1;
        rotationY.M12 = 0.0f;
        rotationY.M13 = -num2;
        rotationY.M14 = 0.0f;
        rotationY.M21 = 0.0f;
        rotationY.M22 = 1f;
        rotationY.M23 = 0.0f;
        rotationY.M24 = 0.0f;
        rotationY.M31 = num2;
        rotationY.M32 = 0.0f;
        rotationY.M33 = num1;
        rotationY.M34 = 0.0f;
        rotationY.M41 = 0.0f;
        rotationY.M42 = 0.0f;
        rotationY.M43 = 0.0f;
        rotationY.M44 = 1f;
        return rotationY;
    }

    [Pure]
    public static void CreateRotationY(float radians, out Matrix result)
    {
        float num1 = (float)Math.Cos(radians);
        float num2 = (float)Math.Sin(radians);
        result.M11 = num1;
        result.M12 = 0.0f;
        result.M13 = -num2;
        result.M14 = 0.0f;
        result.M21 = 0.0f;
        result.M22 = 1f;
        result.M23 = 0.0f;
        result.M24 = 0.0f;
        result.M31 = num2;
        result.M32 = 0.0f;
        result.M33 = num1;
        result.M34 = 0.0f;
        result.M41 = 0.0f;
        result.M42 = 0.0f;
        result.M43 = 0.0f;
        result.M44 = 1f;
    }

    [Pure]
    public static Matrix CreateRotationZ(float radians)
    {
        float num1 = (float)Math.Cos(radians);
        float num2 = (float)Math.Sin(radians);
        Matrix rotationZ;
        rotationZ.M11 = num1;
        rotationZ.M12 = num2;
        rotationZ.M13 = 0.0f;
        rotationZ.M14 = 0.0f;
        rotationZ.M21 = -num2;
        rotationZ.M22 = num1;
        rotationZ.M23 = 0.0f;
        rotationZ.M24 = 0.0f;
        rotationZ.M31 = 0.0f;
        rotationZ.M32 = 0.0f;
        rotationZ.M33 = 1f;
        rotationZ.M34 = 0.0f;
        rotationZ.M41 = 0.0f;
        rotationZ.M42 = 0.0f;
        rotationZ.M43 = 0.0f;
        rotationZ.M44 = 1f;
        return rotationZ;
    }

    [Pure]
    public static void CreateRotationZ(float radians, out Matrix result)
    {
        float num1 = (float)Math.Cos(radians);
        float num2 = (float)Math.Sin(radians);
        result.M11 = num1;
        result.M12 = num2;
        result.M13 = 0.0f;
        result.M14 = 0.0f;
        result.M21 = -num2;
        result.M22 = num1;
        result.M23 = 0.0f;
        result.M24 = 0.0f;
        result.M31 = 0.0f;
        result.M32 = 0.0f;
        result.M33 = 1f;
        result.M34 = 0.0f;
        result.M41 = 0.0f;
        result.M42 = 0.0f;
        result.M43 = 0.0f;
        result.M44 = 1f;
    }

    [Pure]
    public static Matrix CreateFromAxisAngle(Vector3 axis, float angle)
    {
        float x = axis.X;
        float y = axis.Y;
        float z = axis.Z;
        float num1 = (float)Math.Sin(angle);
        float num2 = (float)Math.Cos(angle);
        float num3 = x * x;
        float num4 = y * y;
        float num5 = z * z;
        float num6 = x * y;
        float num7 = x * z;
        float num8 = y * z;
        Matrix fromAxisAngle;
        fromAxisAngle.M11 = num3 + num2 * (1f - num3);
        fromAxisAngle.M12 = (float)(num6 - num2 * (double)num6 + num1 * (double)z);
        fromAxisAngle.M13 = (float)(num7 - num2 * (double)num7 - num1 * (double)y);
        fromAxisAngle.M14 = 0.0f;
        fromAxisAngle.M21 = (float)(num6 - num2 * (double)num6 - num1 * (double)z);
        fromAxisAngle.M22 = num4 + num2 * (1f - num4);
        fromAxisAngle.M23 = (float)(num8 - num2 * (double)num8 + num1 * (double)x);
        fromAxisAngle.M24 = 0.0f;
        fromAxisAngle.M31 = (float)(num7 - num2 * (double)num7 + num1 * (double)y);
        fromAxisAngle.M32 = (float)(num8 - num2 * (double)num8 - num1 * (double)x);
        fromAxisAngle.M33 = num5 + num2 * (1f - num5);
        fromAxisAngle.M34 = 0.0f;
        fromAxisAngle.M41 = 0.0f;
        fromAxisAngle.M42 = 0.0f;
        fromAxisAngle.M43 = 0.0f;
        fromAxisAngle.M44 = 1f;
        return fromAxisAngle;
    }

    [Pure]
    public static void CreateFromAxisAngle(ref Vector3 axis, float angle, out Matrix result)
    {
        float x = axis.X;
        float y = axis.Y;
        float z = axis.Z;
        float num1 = (float)Math.Sin(angle);
        float num2 = (float)Math.Cos(angle);
        float num3 = x * x;
        float num4 = y * y;
        float num5 = z * z;
        float num6 = x * y;
        float num7 = x * z;
        float num8 = y * z;
        result.M11 = num3 + num2 * (1f - num3);
        result.M12 = (float)(num6 - num2 * (double)num6 + num1 * (double)z);
        result.M13 = (float)(num7 - num2 * (double)num7 - num1 * (double)y);
        result.M14 = 0.0f;
        result.M21 = (float)(num6 - num2 * (double)num6 - num1 * (double)z);
        result.M22 = num4 + num2 * (1f - num4);
        result.M23 = (float)(num8 - num2 * (double)num8 + num1 * (double)x);
        result.M24 = 0.0f;
        result.M31 = (float)(num7 - num2 * (double)num7 + num1 * (double)y);
        result.M32 = (float)(num8 - num2 * (double)num8 - num1 * (double)x);
        result.M33 = num5 + num2 * (1f - num5);
        result.M34 = 0.0f;
        result.M41 = 0.0f;
        result.M42 = 0.0f;
        result.M43 = 0.0f;
        result.M44 = 1f;
    }

    [Pure]
    public static Matrix CreatePerspectiveFieldOfView(
      double fieldOfView,
      double aspectRatio,
      double nearPlaneDistance,
      double farPlaneDistance)
    {
        if (fieldOfView <= 0.0 || fieldOfView >= 3.14159274101257)
            throw new ArgumentOutOfRangeException(nameof(fieldOfView), $"Fov out of range: {nameof(fieldOfView)}");
        if (nearPlaneDistance <= 0.0)
            throw new ArgumentOutOfRangeException(nameof(nearPlaneDistance), $"Negative plane distance: {nameof(nearPlaneDistance)}");
        if (farPlaneDistance <= 0.0)
            throw new ArgumentOutOfRangeException(nameof(farPlaneDistance), $"Negative plane distance: {nameof(farPlaneDistance)}");
        if (nearPlaneDistance >= farPlaneDistance)
            throw new ArgumentOutOfRangeException(nameof(nearPlaneDistance), "Opposite planes");
        double num1 = 1.0 / Math.Tan(fieldOfView * 0.5);
        double num2 = num1 / aspectRatio;
        double farPlaneRatio = farPlaneDistance / (nearPlaneDistance - farPlaneDistance);
        Matrix perspectiveFieldOfView;
        perspectiveFieldOfView.M11 = (float)num2;
        perspectiveFieldOfView.M12 = perspectiveFieldOfView.M13 = perspectiveFieldOfView.M14 = 0.0f;
        perspectiveFieldOfView.M22 = (float)num1;
        perspectiveFieldOfView.M21 = perspectiveFieldOfView.M23 = perspectiveFieldOfView.M24 = 0.0f;
        perspectiveFieldOfView.M31 = perspectiveFieldOfView.M32 = 0.0f;
        perspectiveFieldOfView.M33 = (float)farPlaneRatio;
        perspectiveFieldOfView.M34 = -1f;
        perspectiveFieldOfView.M41 = perspectiveFieldOfView.M42 = perspectiveFieldOfView.M44 = 0.0f;
        perspectiveFieldOfView.M43 = (float)(nearPlaneDistance * farPlaneRatio);
        return perspectiveFieldOfView;
    }

    [Pure]
    public static void CreatePerspectiveFieldOfView(
      double fieldOfView,
      double aspectRatio,
      double nearPlaneDistance,
      double farPlaneDistance,
      out Matrix result)
    {
        if (fieldOfView <= 0.0 || fieldOfView >= 3.14159274101257)
            throw new ArgumentOutOfRangeException(nameof(fieldOfView), $"Fov out of range: {nameof(fieldOfView)}");
        if (nearPlaneDistance <= 0.0)
            throw new ArgumentOutOfRangeException(nameof(nearPlaneDistance), $"Negative plane distance: {nameof(nearPlaneDistance)}");
        if (farPlaneDistance <= 0.0)
            throw new ArgumentOutOfRangeException(nameof(farPlaneDistance), $"Negative plane distance: {nameof(farPlaneDistance)}");
        if (nearPlaneDistance >= farPlaneDistance)
            throw new ArgumentOutOfRangeException(nameof(nearPlaneDistance), "Opposite planes");
        double num1 = 1.0 / Math.Tan(fieldOfView * 0.5);
        double num2 = num1 / aspectRatio;
        double farPlaneRatio = farPlaneDistance / (nearPlaneDistance - farPlaneDistance);
        result.M11 = (float)num2;
        result.M12 = result.M13 = result.M14 = 0.0f;
        result.M22 = (float)num1;
        result.M21 = result.M23 = result.M24 = 0.0f;
        result.M31 = result.M32 = 0.0f;
        result.M33 = (float)farPlaneRatio;
        result.M34 = -1f;
        result.M41 = result.M42 = result.M44 = 0.0f;
        result.M43 = (float)(nearPlaneDistance * farPlaneRatio);
    }

    [Pure]
    public static Matrix CreatePerspective(
      float width,
      float height,
      double nearPlaneDistance,
      double farPlaneDistance)
    {
        if (nearPlaneDistance <= 0.0)
            throw new ArgumentOutOfRangeException(nameof(nearPlaneDistance), $"Negative plane distance: {nameof(nearPlaneDistance)}");
        if (farPlaneDistance <= 0.0)
            throw new ArgumentOutOfRangeException(nameof(farPlaneDistance), $"Negative plane distance: {nameof(farPlaneDistance)}");
        if (nearPlaneDistance >= farPlaneDistance)
            throw new ArgumentOutOfRangeException(nameof(nearPlaneDistance), "Opposite planes");
        double farPlaneRatio = farPlaneDistance / (nearPlaneDistance - farPlaneDistance);
        Matrix perspective;
        perspective.M11 = (float)(2.0 * nearPlaneDistance / width);
        perspective.M12 = perspective.M13 = perspective.M14 = 0.0f;
        perspective.M22 = (float)(2.0 * nearPlaneDistance / height);
        perspective.M21 = perspective.M23 = perspective.M24 = 0.0f;
        perspective.M33 = (float)farPlaneRatio;
        perspective.M31 = perspective.M32 = 0.0f;
        perspective.M34 = -1f;
        perspective.M41 = perspective.M42 = perspective.M44 = 0.0f;
        perspective.M43 = (float)(nearPlaneDistance * farPlaneRatio);
        return perspective;
    }

    [Pure]
    public static void CreatePerspective(
      float width,
      float height,
      double nearPlaneDistance,
      double farPlaneDistance,
      out Matrix result)
    {
        if (nearPlaneDistance <= 0.0)
            throw new ArgumentOutOfRangeException(nameof(nearPlaneDistance), $"Negative plane distance: {nameof(nearPlaneDistance)}");
        if (farPlaneDistance <= 0.0)
            throw new ArgumentOutOfRangeException(nameof(farPlaneDistance), $"Negative plane distance: {nameof(farPlaneDistance)}");
        if (nearPlaneDistance >= farPlaneDistance)
            throw new ArgumentOutOfRangeException(nameof(nearPlaneDistance), "Opposite planes");
        double farPlaneRatio = farPlaneDistance / (nearPlaneDistance - farPlaneDistance);
        result.M11 = (float)(2.0 * nearPlaneDistance / width);
        result.M12 = result.M13 = result.M14 = 0.0f;
        result.M22 = (float)(2.0 * nearPlaneDistance / height);
        result.M21 = result.M23 = result.M24 = 0.0f;
        result.M33 = (float)farPlaneRatio;
        result.M31 = result.M32 = 0.0f;
        result.M34 = -1f;
        result.M41 = result.M42 = result.M44 = 0.0f;
        result.M43 = (float)(nearPlaneDistance * farPlaneRatio);
    }

    [Pure]
    public static Matrix CreatePerspectiveOffCenter(
      float left,
      float right,
      float bottom,
      float top,
      double nearPlaneDistance,
      double farPlaneDistance)
    {
        if (nearPlaneDistance <= 0.0)
            throw new ArgumentOutOfRangeException(nameof(nearPlaneDistance), $"Negative plane distance: {nameof(nearPlaneDistance)}");
        if (farPlaneDistance <= 0.0)
            throw new ArgumentOutOfRangeException(nameof(farPlaneDistance), $"Negative plane distance: {nameof(farPlaneDistance)}");
        if (nearPlaneDistance >= farPlaneDistance)
            throw new ArgumentOutOfRangeException(nameof(nearPlaneDistance), "Opposite planes");
        double farPlaneRatio = farPlaneDistance / (nearPlaneDistance - farPlaneDistance);
        Matrix perspectiveOffCenter;
        perspectiveOffCenter.M11 = (float)(2.0 * nearPlaneDistance / (right - (double)left));
        perspectiveOffCenter.M12 = perspectiveOffCenter.M13 = perspectiveOffCenter.M14 = 0.0f;
        perspectiveOffCenter.M22 = (float)(2.0 * nearPlaneDistance / (top - (double)bottom));
        perspectiveOffCenter.M21 = perspectiveOffCenter.M23 = perspectiveOffCenter.M24 = 0.0f;
        perspectiveOffCenter.M31 = (float)((left + (double)right) / (right - (double)left));
        perspectiveOffCenter.M32 = (float)((top + (double)bottom) / (top - (double)bottom));
        perspectiveOffCenter.M33 = (float)farPlaneRatio;
        perspectiveOffCenter.M34 = -1f;
        perspectiveOffCenter.M43 = (float)(nearPlaneDistance * farPlaneRatio);
        perspectiveOffCenter.M41 = perspectiveOffCenter.M42 = perspectiveOffCenter.M44 = 0.0f;
        return perspectiveOffCenter;
    }

    [Pure]
    public static void CreatePerspectiveOffCenter(
      float left,
      float right,
      float bottom,
      float top,
      double nearPlaneDistance,
      double farPlaneDistance,
      out Matrix result)
    {
        if (nearPlaneDistance <= 0.0)
            throw new ArgumentOutOfRangeException(nameof(nearPlaneDistance), $"Negative plane distance: {nameof(nearPlaneDistance)}");
        if (farPlaneDistance <= 0.0)
            throw new ArgumentOutOfRangeException(nameof(farPlaneDistance), $"Negative plane distance: {nameof(farPlaneDistance)}");
        if (nearPlaneDistance >= farPlaneDistance)
            throw new ArgumentOutOfRangeException(nameof(nearPlaneDistance), "Opposite planes");
        double farPlaneRatio = farPlaneDistance / (nearPlaneDistance - farPlaneDistance);
        result.M11 = (float)(2.0 * nearPlaneDistance / (right - (double)left));
        result.M12 = result.M13 = result.M14 = 0.0f;
        result.M22 = (float)(2.0 * nearPlaneDistance / (top - (double)bottom));
        result.M21 = result.M23 = result.M24 = 0.0f;
        result.M31 = (float)((left + (double)right) / (right - (double)left));
        result.M32 = (float)((top + (double)bottom) / (top - (double)bottom));
        result.M33 = (float)farPlaneRatio;
        result.M34 = -1f;
        result.M43 = (float)(nearPlaneDistance * farPlaneRatio);
        result.M41 = result.M42 = result.M44 = 0.0f;
    }

    [Pure]
    public static Matrix CreateOrthographic(
      float width,
      float height,
      float zNearPlane,
      float zFarPlane)
    {
        Matrix orthographic;
        orthographic.M11 = 2f / width;
        orthographic.M12 = orthographic.M13 = orthographic.M14 = 0.0f;
        orthographic.M22 = 2f / height;
        orthographic.M21 = orthographic.M23 = orthographic.M24 = 0.0f;
        orthographic.M33 = (float)(1.0 / (zNearPlane - (double)zFarPlane));
        orthographic.M31 = orthographic.M32 = orthographic.M34 = 0.0f;
        orthographic.M41 = orthographic.M42 = 0.0f;
        orthographic.M43 = zNearPlane / (zNearPlane - zFarPlane);
        orthographic.M44 = 1f;
        return orthographic;
    }

    [Pure]
    public static void CreateOrthographic(
      float width,
      float height,
      float zNearPlane,
      float zFarPlane,
      out Matrix result)
    {
        result.M11 = 2f / width;
        result.M12 = result.M13 = result.M14 = 0.0f;
        result.M22 = 2f / height;
        result.M21 = result.M23 = result.M24 = 0.0f;
        result.M33 = (float)(1.0 / (zNearPlane - (double)zFarPlane));
        result.M31 = result.M32 = result.M34 = 0.0f;
        result.M41 = result.M42 = 0.0f;
        result.M43 = zNearPlane / (zNearPlane - zFarPlane);
        result.M44 = 1f;
    }

    [Pure]
    public static Matrix CreateOrthographicOffCenter(
      float left,
      float right,
      float bottom,
      float top,
      float zNearPlane,
      float zFarPlane)
    {
        Matrix orthographicOffCenter;
        orthographicOffCenter.M11 = (float)(2.0 / (right - (double)left));
        orthographicOffCenter.M12 = orthographicOffCenter.M13 = orthographicOffCenter.M14 = 0.0f;
        orthographicOffCenter.M22 = (float)(2.0 / (top - (double)bottom));
        orthographicOffCenter.M21 = orthographicOffCenter.M23 = orthographicOffCenter.M24 = 0.0f;
        orthographicOffCenter.M33 = (float)(1.0 / (zNearPlane - (double)zFarPlane));
        orthographicOffCenter.M31 = orthographicOffCenter.M32 = orthographicOffCenter.M34 = 0.0f;
        orthographicOffCenter.M41 = (float)((left + (double)right) / (left - (double)right));
        orthographicOffCenter.M42 = (float)((top + (double)bottom) / (bottom - (double)top));
        orthographicOffCenter.M43 = zNearPlane / (zNearPlane - zFarPlane);
        orthographicOffCenter.M44 = 1f;
        return orthographicOffCenter;
    }

    [Pure]
    public static void CreateOrthographicOffCenter(
      float left,
      float right,
      float bottom,
      float top,
      float zNearPlane,
      float zFarPlane,
      out Matrix result)
    {
        result.M11 = (float)(2.0 / (right - (double)left));
        result.M12 = result.M13 = result.M14 = 0.0f;
        result.M22 = (float)(2.0 / (top - (double)bottom));
        result.M21 = result.M23 = result.M24 = 0.0f;
        result.M33 = (float)(1.0 / (zNearPlane - (double)zFarPlane));
        result.M31 = result.M32 = result.M34 = 0.0f;
        result.M41 = (float)((left + (double)right) / (left - (double)right));
        result.M42 = (float)((top + (double)bottom) / (bottom - (double)top));
        result.M43 = zNearPlane / (zNearPlane - zFarPlane);
        result.M44 = 1f;
    }

    [Pure]
    public static Matrix CreateLookAt(
      Vector3 cameraPosition,
      Vector3 cameraTarget,
      Vector3 cameraUpVector)
    {
        Vector3 vector3_1 = (cameraPosition - cameraTarget).Normalized();
        Vector3 vector3_2 = Vector3.Cross(cameraUpVector, vector3_1).Normalized();
        Vector3 vector1 = Vector3.Cross(vector3_1, vector3_2);
        Matrix lookAt;
        lookAt.M11 = vector3_2.X;
        lookAt.M12 = vector1.X;
        lookAt.M13 = vector3_1.X;
        lookAt.M14 = 0.0f;
        lookAt.M21 = vector3_2.Y;
        lookAt.M22 = vector1.Y;
        lookAt.M23 = vector3_1.Y;
        lookAt.M24 = 0.0f;
        lookAt.M31 = vector3_2.Z;
        lookAt.M32 = vector1.Z;
        lookAt.M33 = vector3_1.Z;
        lookAt.M34 = 0.0f;
        lookAt.M41 = -vector3_2.Dot(cameraPosition);
        lookAt.M42 = -vector1.Dot(cameraPosition);
        lookAt.M43 = -vector3_1.Dot(cameraPosition);
        lookAt.M44 = 1f;
        return lookAt;
    }

    [Pure]
    public static void CreateLookAt(
      ref Vector3 cameraPosition,
      ref Vector3 cameraTarget,
      ref Vector3 cameraUpVector,
      out Matrix result)
    {
        Vector3 vector3_1 = (cameraPosition - cameraTarget).Normalized();
        Vector3 vector3_2 = Vector3.Cross(cameraUpVector, vector3_1).Normalized();
        Vector3 vector1 = Vector3.Cross(vector3_1, vector3_2);
        result.M11 = vector3_2.X;
        result.M12 = vector1.X;
        result.M13 = vector3_1.X;
        result.M14 = 0.0f;
        result.M21 = vector3_2.Y;
        result.M22 = vector1.Y;
        result.M23 = vector3_1.Y;
        result.M24 = 0.0f;
        result.M31 = vector3_2.Z;
        result.M32 = vector1.Z;
        result.M33 = vector3_1.Z;
        result.M34 = 0.0f;
        result.M41 = -vector3_2.Dot(cameraPosition);
        result.M42 = -vector1.Dot(cameraPosition);
        result.M43 = -vector3_1.Dot(cameraPosition);
        result.M44 = 1f;
    }

    [Pure]
    public static Matrix CreateWorld(Vector3 position, Vector3 forward, Vector3 up)
    {
        Vector3 vector3_1 = (-forward).Normalized();
        Vector3 vector2 = Vector3.Cross(up, vector3_1).Normalized();
        Vector3 vector3_2 = Vector3.Cross(vector3_1, vector2);
        Matrix world;
        world.M11 = vector2.X;
        world.M12 = vector2.Y;
        world.M13 = vector2.Z;
        world.M14 = 0.0f;
        world.M21 = vector3_2.X;
        world.M22 = vector3_2.Y;
        world.M23 = vector3_2.Z;
        world.M24 = 0.0f;
        world.M31 = vector3_1.X;
        world.M32 = vector3_1.Y;
        world.M33 = vector3_1.Z;
        world.M34 = 0.0f;
        world.M41 = position.X;
        world.M42 = position.Y;
        world.M43 = position.Z;
        world.M44 = 1f;
        return world;
    }

    [Pure]
    public static void CreateWorld(
      ref Vector3 position,
      ref Vector3 forward,
      ref Vector3 up,
      out Matrix result)
    {
        Vector3 vector3_1 = (-forward).Normalized();
        Vector3 vector2 = Vector3.Cross(up, vector3_1).Normalized();
        Vector3 vector3_2 = Vector3.Cross(vector3_1, vector2);
        result.M11 = vector2.X;
        result.M12 = vector2.Y;
        result.M13 = vector2.Z;
        result.M14 = 0.0f;
        result.M21 = vector3_2.X;
        result.M22 = vector3_2.Y;
        result.M23 = vector3_2.Z;
        result.M24 = 0.0f;
        result.M31 = vector3_1.X;
        result.M32 = vector3_1.Y;
        result.M33 = vector3_1.Z;
        result.M34 = 0.0f;
        result.M41 = position.X;
        result.M42 = position.Y;
        result.M43 = position.Z;
        result.M44 = 1f;
    }

    [Pure]
    public static Matrix CreateFromQuaternion(XnaQuat quaternion)
    {
        float num1 = quaternion.X * quaternion.X;
        float num2 = quaternion.Y * quaternion.Y;
        float num3 = quaternion.Z * quaternion.Z;
        float num4 = quaternion.X * quaternion.Y;
        float num5 = quaternion.Z * quaternion.W;
        float num6 = quaternion.Z * quaternion.X;
        float num7 = quaternion.Y * quaternion.W;
        float num8 = quaternion.Y * quaternion.Z;
        float num9 = quaternion.X * quaternion.W;
        Matrix fromQuaternion;
        fromQuaternion.M11 = (float)(1.0 - 2.0 * (num2 + num3));
        fromQuaternion.M12 = (float)(2.0 * (num4 + num5));
        fromQuaternion.M13 = (float)(2.0 * (num6 - num7));
        fromQuaternion.M14 = 0.0f;
        fromQuaternion.M21 = (float)(2.0 * (num4 - num5));
        fromQuaternion.M22 = (float)(1.0 - 2.0 * (num3 + num1));
        fromQuaternion.M23 = (float)(2.0 * (num8 + num9));
        fromQuaternion.M24 = 0.0f;
        fromQuaternion.M31 = (float)(2.0 * (num6 + num7));
        fromQuaternion.M32 = (float)(2.0 * (num8 - num9));
        fromQuaternion.M33 = (float)(1.0 - 2.0 * (num2 + num1));
        fromQuaternion.M34 = 0.0f;
        fromQuaternion.M41 = 0.0f;
        fromQuaternion.M42 = 0.0f;
        fromQuaternion.M43 = 0.0f;
        fromQuaternion.M44 = 1f;
        return fromQuaternion;
    }

    [Pure]
    public static void CreateFromQuaternion(ref XnaQuat quaternion, out Matrix result)
    {
        float num1 = quaternion.X * quaternion.X;
        float num2 = quaternion.Y * quaternion.Y;
        float num3 = quaternion.Z * quaternion.Z;
        float num4 = quaternion.X * quaternion.Y;
        float num5 = quaternion.Z * quaternion.W;
        float num6 = quaternion.Z * quaternion.X;
        float num7 = quaternion.Y * quaternion.W;
        float num8 = quaternion.Y * quaternion.Z;
        float num9 = quaternion.X * quaternion.W;
        result.M11 = (float)(1.0 - 2.0 * (num2 + num3));
        result.M12 = (float)(2.0 * (num4 + num5));
        result.M13 = (float)(2.0 * (num6 - num7));
        result.M14 = 0.0f;
        result.M21 = (float)(2.0 * (num4 - num5));
        result.M22 = (float)(1.0 - 2.0 * (num3 + num1));
        result.M23 = (float)(2.0 * (num8 + num9));
        result.M24 = 0.0f;
        result.M31 = (float)(2.0 * (num6 + num7));
        result.M32 = (float)(2.0 * (num8 - num9));
        result.M33 = (float)(1.0 - 2.0 * (num2 + num1));
        result.M34 = 0.0f;
        result.M41 = 0.0f;
        result.M42 = 0.0f;
        result.M43 = 0.0f;
        result.M44 = 1f;
    }

    [Pure]
    public static Matrix CreateFromYawPitchRoll(float yaw, float pitch, float roll)
    {
        XnaQuat result1;
        XnaQuat.CreateFromYawPitchRoll(yaw, pitch, roll, out result1);
        Matrix result2;
        CreateFromQuaternion(ref result1, out result2);
        return result2;
    }

    [Pure]
    public static void CreateFromYawPitchRoll(
      float yaw,
      float pitch,
      float roll,
      out Matrix result)
    {
        XnaQuat result1;
        XnaQuat.CreateFromYawPitchRoll(yaw, pitch, roll, out result1);
        CreateFromQuaternion(ref result1, out result);
    }

    [Pure]
    public static Matrix CreateShadow(Vector3 lightDirection, XnaPlane plane)
    {
        XnaPlane result;
        XnaPlane.Normalize(ref plane, out result);
        float num1 = result.Normal.X * lightDirection.X + result.Normal.Y * lightDirection.Y + result.Normal.Z * lightDirection.Z;
        float num2 = -result.Normal.X;
        float num3 = -result.Normal.Y;
        float num4 = -result.Normal.Z;
        float num5 = -result.D;
        Matrix shadow;
        shadow.M11 = num2 * lightDirection.X + num1;
        shadow.M21 = num3 * lightDirection.X;
        shadow.M31 = num4 * lightDirection.X;
        shadow.M41 = num5 * lightDirection.X;
        shadow.M12 = num2 * lightDirection.Y;
        shadow.M22 = num3 * lightDirection.Y + num1;
        shadow.M32 = num4 * lightDirection.Y;
        shadow.M42 = num5 * lightDirection.Y;
        shadow.M13 = num2 * lightDirection.Z;
        shadow.M23 = num3 * lightDirection.Z;
        shadow.M33 = num4 * lightDirection.Z + num1;
        shadow.M43 = num5 * lightDirection.Z;
        shadow.M14 = 0.0f;
        shadow.M24 = 0.0f;
        shadow.M34 = 0.0f;
        shadow.M44 = num1;
        return shadow;
    }

    [Pure]
    public static void CreateShadow(ref Vector3 lightDirection, ref XnaPlane plane, out Matrix result)
    {
        XnaPlane result1;
        XnaPlane.Normalize(ref plane, out result1);
        float num1 = result1.Normal.X * lightDirection.X + result1.Normal.Y * lightDirection.Y + result1.Normal.Z * lightDirection.Z;
        float num2 = -result1.Normal.X;
        float num3 = -result1.Normal.Y;
        float num4 = -result1.Normal.Z;
        float num5 = -result1.D;
        result.M11 = num2 * lightDirection.X + num1;
        result.M21 = num3 * lightDirection.X;
        result.M31 = num4 * lightDirection.X;
        result.M41 = num5 * lightDirection.X;
        result.M12 = num2 * lightDirection.Y;
        result.M22 = num3 * lightDirection.Y + num1;
        result.M32 = num4 * lightDirection.Y;
        result.M42 = num5 * lightDirection.Y;
        result.M13 = num2 * lightDirection.Z;
        result.M23 = num3 * lightDirection.Z;
        result.M33 = num4 * lightDirection.Z + num1;
        result.M43 = num5 * lightDirection.Z;
        result.M14 = 0.0f;
        result.M24 = 0.0f;
        result.M34 = 0.0f;
        result.M44 = num1;
    }

    [Pure]
    public static Matrix CreateReflection(XnaPlane value)
    {
        value.Normalize();
        float x = value.Normal.X;
        float y = value.Normal.Y;
        float z = value.Normal.Z;
        float num1 = -2f * x;
        float num2 = -2f * y;
        float num3 = -2f * z;
        Matrix reflection;
        reflection.M11 = (float)(num1 * (double)x + 1.0);
        reflection.M12 = num2 * x;
        reflection.M13 = num3 * x;
        reflection.M14 = 0.0f;
        reflection.M21 = num1 * y;
        reflection.M22 = (float)(num2 * (double)y + 1.0);
        reflection.M23 = num3 * y;
        reflection.M24 = 0.0f;
        reflection.M31 = num1 * z;
        reflection.M32 = num2 * z;
        reflection.M33 = (float)(num3 * (double)z + 1.0);
        reflection.M34 = 0.0f;
        reflection.M41 = num1 * value.D;
        reflection.M42 = num2 * value.D;
        reflection.M43 = num3 * value.D;
        reflection.M44 = 1f;
        return reflection;
    }

    [Pure]
    public static void CreateReflection(ref XnaPlane value, out Matrix result)
    {
        XnaPlane result1;
        XnaPlane.Normalize(ref value, out result1);
        value.Normalize();
        float x = result1.Normal.X;
        float y = result1.Normal.Y;
        float z = result1.Normal.Z;
        float num1 = -2f * x;
        float num2 = -2f * y;
        float num3 = -2f * z;
        result.M11 = (float)(num1 * (double)x + 1.0);
        result.M12 = num2 * x;
        result.M13 = num3 * x;
        result.M14 = 0.0f;
        result.M21 = num1 * y;
        result.M22 = (float)(num2 * (double)y + 1.0);
        result.M23 = num3 * y;
        result.M24 = 0.0f;
        result.M31 = num1 * z;
        result.M32 = num2 * z;
        result.M33 = (float)(num3 * (double)z + 1.0);
        result.M34 = 0.0f;
        result.M41 = num1 * result1.D;
        result.M42 = num2 * result1.D;
        result.M43 = num3 * result1.D;
        result.M44 = 1f;
    }

    [Pure]
    public static Matrix Transform(Matrix value, XnaQuat rotation)
    {
        float num1 = rotation.X + rotation.X;
        float num2 = rotation.Y + rotation.Y;
        float num3 = rotation.Z + rotation.Z;
        float num4 = rotation.W * num1;
        float num5 = rotation.W * num2;
        float num6 = rotation.W * num3;
        float num7 = rotation.X * num1;
        float num8 = rotation.X * num2;
        float num9 = rotation.X * num3;
        float num10 = rotation.Y * num2;
        float num11 = rotation.Y * num3;
        float num12 = rotation.Z * num3;
        float num13 = 1f - num10 - num12;
        float num14 = num8 - num6;
        float num15 = num9 + num5;
        float num16 = num8 + num6;
        float num17 = 1f - num7 - num12;
        float num18 = num11 - num4;
        float num19 = num9 - num5;
        float num20 = num11 + num4;
        float num21 = 1f - num7 - num10;
        Matrix matrix;
        matrix.M11 = value.M11 * num13 + value.M12 * num14 + value.M13 * num15;
        matrix.M12 = value.M11 * num16 + value.M12 * num17 + value.M13 * num18;
        matrix.M13 = value.M11 * num19 + value.M12 * num20 + value.M13 * num21;
        matrix.M14 = value.M14;
        matrix.M21 = value.M21 * num13 + value.M22 * num14 + value.M23 * num15;
        matrix.M22 = value.M21 * num16 + value.M22 * num17 + value.M23 * num18;
        matrix.M23 = value.M21 * num19 + value.M22 * num20 + value.M23 * num21;
        matrix.M24 = value.M24;
        matrix.M31 = value.M31 * num13 + value.M32 * num14 + value.M33 * num15;
        matrix.M32 = value.M31 * num16 + value.M32 * num17 + value.M33 * num18;
        matrix.M33 = value.M31 * num19 + value.M32 * num20 + value.M33 * num21;
        matrix.M34 = value.M34;
        matrix.M41 = value.M41 * num13 + value.M42 * num14 + value.M43 * num15;
        matrix.M42 = value.M41 * num16 + value.M42 * num17 + value.M43 * num18;
        matrix.M43 = value.M41 * num19 + value.M42 * num20 + value.M43 * num21;
        matrix.M44 = value.M44;
        return matrix;
    }

    [Pure]
    public static void Transform(ref Matrix value, ref XnaQuat rotation, out Matrix result)
    {
        float num1 = rotation.X + rotation.X;
        float num2 = rotation.Y + rotation.Y;
        float num3 = rotation.Z + rotation.Z;
        float num4 = rotation.W * num1;
        float num5 = rotation.W * num2;
        float num6 = rotation.W * num3;
        float num7 = rotation.X * num1;
        float num8 = rotation.X * num2;
        float num9 = rotation.X * num3;
        float num10 = rotation.Y * num2;
        float num11 = rotation.Y * num3;
        float num12 = rotation.Z * num3;
        float num13 = 1f - num10 - num12;
        float num14 = num8 - num6;
        float num15 = num9 + num5;
        float num16 = num8 + num6;
        float num17 = 1f - num7 - num12;
        float num18 = num11 - num4;
        float num19 = num9 - num5;
        float num20 = num11 + num4;
        float num21 = 1f - num7 - num10;
        float num22 = value.M11 * num13 + value.M12 * num14 + value.M13 * num15;
        float num23 = value.M11 * num16 + value.M12 * num17 + value.M13 * num18;
        float num24 = value.M11 * num19 + value.M12 * num20 + value.M13 * num21;
        float m14 = value.M14;
        float num25 = value.M21 * num13 + value.M22 * num14 + value.M23 * num15;
        float num26 = value.M21 * num16 + value.M22 * num17 + value.M23 * num18;
        float num27 = value.M21 * num19 + value.M22 * num20 + value.M23 * num21;
        float m24 = value.M24;
        float num28 = value.M31 * num13 + value.M32 * num14 + value.M33 * num15;
        float num29 = value.M31 * num16 + value.M32 * num17 + value.M33 * num18;
        float num30 = value.M31 * num19 + value.M32 * num20 + value.M33 * num21;
        float m34 = value.M34;
        float num31 = value.M41 * num13 + value.M42 * num14 + value.M43 * num15;
        float num32 = value.M41 * num16 + value.M42 * num17 + value.M43 * num18;
        float num33 = value.M41 * num19 + value.M42 * num20 + value.M43 * num21;
        float m44 = value.M44;
        result.M11 = num22;
        result.M12 = num23;
        result.M13 = num24;
        result.M14 = m14;
        result.M21 = num25;
        result.M22 = num26;
        result.M23 = num27;
        result.M24 = m24;
        result.M31 = num28;
        result.M32 = num29;
        result.M33 = num30;
        result.M34 = m34;
        result.M41 = num31;
        result.M42 = num32;
        result.M43 = num33;
        result.M44 = m44;
    }

    public override string ToString()
    {
        CultureInfo c = CultureInfo.CurrentCulture;
        return "{ " + string.Format(c, "{{M11:{0} M12:{1} M13:{2} M14:{3}}} ", M11.ToString(c), M12.ToString(c), M13.ToString(c), M14.ToString(c))
                    + string.Format(c, "{{M21:{0} M22:{1} M23:{2} M24:{3}}} ", M21.ToString(c), M22.ToString(c), M23.ToString(c), M24.ToString(c))
                    + string.Format(c, "{{M31:{0} M32:{1} M33:{2} M34:{3}}} ", M31.ToString(c), M32.ToString(c), M33.ToString(c), M34.ToString(c))
                    + string.Format(c, "{{M41:{0} M42:{1} M43:{2} M44:{3}}} ", M41.ToString(c), M42.ToString(c), M43.ToString(c), M44.ToString(c)) + "}";
    }

    public bool Equals(Matrix other)
    {
        return M11 == other.M11 && M22 == other.M22 && M33 == other.M33 && M44 == other.M44
            && M12 == other.M12 && M13 == other.M13 && M14 == other.M14 && M21 == other.M21
            && M23 == other.M23 && M24 == other.M24 && M31 == other.M31 && M32 == other.M32
            && M34 == other.M34 && M41 == other.M41 && M42 == other.M42 && M43 == other.M43;
    }
    public override bool Equals(object obj)
    {
        bool flag = false;
        if (obj is Matrix other)
            flag = Equals(other);
        return flag;
    }

    public override int GetHashCode() => M11.GetHashCode() + M12.GetHashCode() + M13.GetHashCode() + M14.GetHashCode() + M21.GetHashCode() + M22.GetHashCode() + M23.GetHashCode() + M24.GetHashCode() + M31.GetHashCode() + M32.GetHashCode() + M33.GetHashCode() + M34.GetHashCode() + M41.GetHashCode() + M42.GetHashCode() + M43.GetHashCode() + M44.GetHashCode();

    [Pure]
    public static Matrix Transpose(Matrix matrix)
    {
        Matrix matrix1;
        matrix1.M11 = matrix.M11;
        matrix1.M12 = matrix.M21;
        matrix1.M13 = matrix.M31;
        matrix1.M14 = matrix.M41;
        matrix1.M21 = matrix.M12;
        matrix1.M22 = matrix.M22;
        matrix1.M23 = matrix.M32;
        matrix1.M24 = matrix.M42;
        matrix1.M31 = matrix.M13;
        matrix1.M32 = matrix.M23;
        matrix1.M33 = matrix.M33;
        matrix1.M34 = matrix.M43;
        matrix1.M41 = matrix.M14;
        matrix1.M42 = matrix.M24;
        matrix1.M43 = matrix.M34;
        matrix1.M44 = matrix.M44;
        return matrix1;
    }

    [Pure]
    public static void Transpose(ref Matrix matrix, out Matrix result)
    {
        float m11 = matrix.M11;
        float m12 = matrix.M12;
        float m13 = matrix.M13;
        float m14 = matrix.M14;
        float m21 = matrix.M21;
        float m22 = matrix.M22;
        float m23 = matrix.M23;
        float m24 = matrix.M24;
        float m31 = matrix.M31;
        float m32 = matrix.M32;
        float m33 = matrix.M33;
        float m34 = matrix.M34;
        float m41 = matrix.M41;
        float m42 = matrix.M42;
        float m43 = matrix.M43;
        float m44 = matrix.M44;
        result.M11 = m11;
        result.M12 = m21;
        result.M13 = m31;
        result.M14 = m41;
        result.M21 = m12;
        result.M22 = m22;
        result.M23 = m32;
        result.M24 = m42;
        result.M31 = m13;
        result.M32 = m23;
        result.M33 = m33;
        result.M34 = m43;
        result.M41 = m14;
        result.M42 = m24;
        result.M43 = m34;
        result.M44 = m44;
    }

    [Pure]
    public float Determinant()
    {
        float m11 = M11;
        float m12 = M12;
        float m13 = M13;
        float m14 = M14;
        float m21 = M21;
        float m22 = M22;
        float m23 = M23;
        float m24 = M24;
        float m31 = M31;
        float m32 = M32;
        float m33 = M33;
        float m34 = M34;
        float m41 = M41;
        float m42 = M42;
        float m43 = M43;
        float m44 = M44;
        float num1 = m33 * m44 - m34 * m43;
        float num2 = m32 * m44 - m34 * m42;
        float num3 = m32 * m43 - m33 * m42;
        float num4 = m31 * m44 - m34 * m41;
        float num5 = m31 * m43 - m33 * m41;
        float num6 = m31 * m42 - m32 * m41;
        return m11 * (m22 * num1 - m23 * num2 + m24 * num3) - m12 * (m21 * num1 - m23 * num4 + m24 * num5) + m13 * (m21 * num2 - m22 * num4 + m24 * num6) - m14 * (m21 * num3 - m22 * num5 + m23 * num6);
    }

    [Pure]
    public static Matrix Invert(in Matrix matrix)
    {
        float m11 = matrix.M11;
        float m12 = matrix.M12;
        float m13 = matrix.M13;
        float m14 = matrix.M14;
        float m21 = matrix.M21;
        float m22 = matrix.M22;
        float m23 = matrix.M23;
        float m24 = matrix.M24;
        float m31 = matrix.M31;
        float m32 = matrix.M32;
        float m33 = matrix.M33;
        float m34 = matrix.M34;
        float m41 = matrix.M41;
        float m42 = matrix.M42;
        float m43 = matrix.M43;
        float m44 = matrix.M44;
        float num1 = m33 * m44 - m34 * m43;
        float num2 = m32 * m44 - m34 * m42;
        float num3 = m32 * m43 - m33 * m42;
        float num4 = m31 * m44 - m34 * m41;
        float num5 = m31 * m43 - m33 * m41;
        float num6 = m31 * m42 - m32 * m41;
        float num7 = m22 * num1 - m23 * num2 + m24 * num3;
        float num8 = -(m21 * num1 - m23 * num4 + m24 * num5);
        float num9 = m21 * num2 - m22 * num4 + m24 * num6;
        float num10 = -(m21 * num3 - m22 * num5 + m23 * num6);
        float num11 = (float)(1.0 / (m11 * num7 + m12 * num8 + m13 * num9 + m14 * num10));
        Matrix matrix1;
        matrix1.M11 = num7 * num11;
        matrix1.M21 = num8 * num11;
        matrix1.M31 = num9 * num11;
        matrix1.M41 = num10 * num11;
        matrix1.M12 = -(m12 * num1 - m13 * num2 + m14 * num3) * num11;
        matrix1.M22 = (m11 * num1 - m13 * num4 + m14 * num5) * num11;
        matrix1.M32 = -(m11 * num2 - m12 * num4 + m14 * num6) * num11;
        matrix1.M42 = (m11 * num3 - m12 * num5 + m13 * num6) * num11;
        float num12 = m23 * m44 - m24 * m43;
        float num13 = m22 * m44 - m24 * m42;
        float num14 = m22 * m43 - m23 * m42;
        float num15 = m21 * m44 - m24 * m41;
        float num16 = m21 * m43 - m23 * m41;
        float num17 = m21 * m42 - m22 * m41;
        matrix1.M13 = (m12 * num12 - m13 * num13 + m14 * num14) * num11;
        matrix1.M23 = -(m11 * num12 - m13 * num15 + m14 * num16) * num11;
        matrix1.M33 = (m11 * num13 - m12 * num15 + m14 * num17) * num11;
        matrix1.M43 = -(m11 * num14 - m12 * num16 + m13 * num17) * num11;
        float num18 = m23 * m34 - m24 * m33;
        float num19 = m22 * m34 - m24 * m32;
        float num20 = m22 * m33 - m23 * m32;
        float num21 = m21 * m34 - m24 * m31;
        float num22 = m21 * m33 - m23 * m31;
        float num23 = m21 * m32 - m22 * m31;
        matrix1.M14 = -(m12 * num18 - m13 * num19 + m14 * num20) * num11;
        matrix1.M24 = (m11 * num18 - m13 * num21 + m14 * num22) * num11;
        matrix1.M34 = -(m11 * num19 - m12 * num21 + m14 * num23) * num11;
        matrix1.M44 = (m11 * num20 - m12 * num22 + m13 * num23) * num11;
        return matrix1;
    }

    [Pure]
    public static void Invert(in Matrix matrix, out Matrix result)
    {
        float m11 = matrix.M11;
        float m12 = matrix.M12;
        float m13 = matrix.M13;
        float m14 = matrix.M14;
        float m21 = matrix.M21;
        float m22 = matrix.M22;
        float m23 = matrix.M23;
        float m24 = matrix.M24;
        float m31 = matrix.M31;
        float m32 = matrix.M32;
        float m33 = matrix.M33;
        float m34 = matrix.M34;
        float m41 = matrix.M41;
        float m42 = matrix.M42;
        float m43 = matrix.M43;
        float m44 = matrix.M44;
        float num1 = m33 * m44 - m34 * m43;
        float num2 = m32 * m44 - m34 * m42;
        float num3 = m32 * m43 - m33 * m42;
        float num4 = m31 * m44 - m34 * m41;
        float num5 = m31 * m43 - m33 * m41;
        float num6 = m31 * m42 - m32 * m41;
        float num7 = m22 * num1 - m23 * num2 + m24 * num3;
        float num8 = -(m21 * num1 - m23 * num4 + m24 * num5);
        float num9 = m21 * num2 - m22 * num4 + m24 * num6;
        float num10 = -(m21 * num3 - m22 * num5 + m23 * num6);
        float num11 = (float)(1.0 / (m11 * num7 + m12 * num8 + m13 * num9 + m14 * num10));
        result.M11 = num7 * num11;
        result.M21 = num8 * num11;
        result.M31 = num9 * num11;
        result.M41 = num10 * num11;
        result.M12 = -(m12 * num1 - m13 * num2 + m14 * num3) * num11;
        result.M22 = (m11 * num1 - m13 * num4 + m14 * num5) * num11;
        result.M32 = -(m11 * num2 - m12 * num4 + m14 * num6) * num11;
        result.M42 = (m11 * num3 - m12 * num5 + m13 * num6) * num11;
        float num12 = m23 * m44 - m24 * m43;
        float num13 = m22 * m44 - m24 * m42;
        float num14 = m22 * m43 - m23 * m42;
        float num15 = m21 * m44 - m24 * m41;
        float num16 = m21 * m43 - m23 * m41;
        float num17 = m21 * m42 - m22 * m41;
        result.M13 = (m12 * num12 - m13 * num13 + m14 * num14) * num11;
        result.M23 = -(m11 * num12 - m13 * num15 + m14 * num16) * num11;
        result.M33 = (m11 * num13 - m12 * num15 + m14 * num17) * num11;
        result.M43 = -(m11 * num14 - m12 * num16 + m13 * num17) * num11;
        float num18 = m23 * m34 - m24 * m33;
        float num19 = m22 * m34 - m24 * m32;
        float num20 = m22 * m33 - m23 * m32;
        float num21 = m21 * m34 - m24 * m31;
        float num22 = m21 * m33 - m23 * m31;
        float num23 = m21 * m32 - m22 * m31;
        result.M14 = -(m12 * num18 - m13 * num19 + m14 * num20) * num11;
        result.M24 = (m11 * num18 - m13 * num21 + m14 * num22) * num11;
        result.M34 = -(m11 * num19 - m12 * num21 + m14 * num23) * num11;
        result.M44 = (m11 * num20 - m12 * num22 + m13 * num23) * num11;
    }

    [Pure]
    public static Matrix Lerp(in Matrix a, in Matrix b, float amount)
    {
        Matrix matrix;
        matrix.M11 = a.M11 + (b.M11 - a.M11) * amount;
        matrix.M12 = a.M12 + (b.M12 - a.M12) * amount;
        matrix.M13 = a.M13 + (b.M13 - a.M13) * amount;
        matrix.M14 = a.M14 + (b.M14 - a.M14) * amount;
        matrix.M21 = a.M21 + (b.M21 - a.M21) * amount;
        matrix.M22 = a.M22 + (b.M22 - a.M22) * amount;
        matrix.M23 = a.M23 + (b.M23 - a.M23) * amount;
        matrix.M24 = a.M24 + (b.M24 - a.M24) * amount;
        matrix.M31 = a.M31 + (b.M31 - a.M31) * amount;
        matrix.M32 = a.M32 + (b.M32 - a.M32) * amount;
        matrix.M33 = a.M33 + (b.M33 - a.M33) * amount;
        matrix.M34 = a.M34 + (b.M34 - a.M34) * amount;
        matrix.M41 = a.M41 + (b.M41 - a.M41) * amount;
        matrix.M42 = a.M42 + (b.M42 - a.M42) * amount;
        matrix.M43 = a.M43 + (b.M43 - a.M43) * amount;
        matrix.M44 = a.M44 + (b.M44 - a.M44) * amount;
        return matrix;
    }

    [Pure]
    public static void Lerp(
      ref Matrix a,
      ref Matrix b,
      float amount,
      out Matrix result)
    {
        result.M11 = a.M11 + (b.M11 - a.M11) * amount;
        result.M12 = a.M12 + (b.M12 - a.M12) * amount;
        result.M13 = a.M13 + (b.M13 - a.M13) * amount;
        result.M14 = a.M14 + (b.M14 - a.M14) * amount;
        result.M21 = a.M21 + (b.M21 - a.M21) * amount;
        result.M22 = a.M22 + (b.M22 - a.M22) * amount;
        result.M23 = a.M23 + (b.M23 - a.M23) * amount;
        result.M24 = a.M24 + (b.M24 - a.M24) * amount;
        result.M31 = a.M31 + (b.M31 - a.M31) * amount;
        result.M32 = a.M32 + (b.M32 - a.M32) * amount;
        result.M33 = a.M33 + (b.M33 - a.M33) * amount;
        result.M34 = a.M34 + (b.M34 - a.M34) * amount;
        result.M41 = a.M41 + (b.M41 - a.M41) * amount;
        result.M42 = a.M42 + (b.M42 - a.M42) * amount;
        result.M43 = a.M43 + (b.M43 - a.M43) * amount;
        result.M44 = a.M44 + (b.M44 - a.M44) * amount;
    }

    [Pure]
    public static Matrix Negate(Matrix m)
    {
        Matrix result;
        result.M11 = -m.M11;
        result.M12 = -m.M12;
        result.M13 = -m.M13;
        result.M14 = -m.M14;
        result.M21 = -m.M21;
        result.M22 = -m.M22;
        result.M23 = -m.M23;
        result.M24 = -m.M24;
        result.M31 = -m.M31;
        result.M32 = -m.M32;
        result.M33 = -m.M33;
        result.M34 = -m.M34;
        result.M41 = -m.M41;
        result.M42 = -m.M42;
        result.M43 = -m.M43;
        result.M44 = -m.M44;
        return result;
    }

    [Pure]
    public static void Negate(ref Matrix m, out Matrix result)
    {
        result.M11 = -m.M11;
        result.M12 = -m.M12;
        result.M13 = -m.M13;
        result.M14 = -m.M14;
        result.M21 = -m.M21;
        result.M22 = -m.M22;
        result.M23 = -m.M23;
        result.M24 = -m.M24;
        result.M31 = -m.M31;
        result.M32 = -m.M32;
        result.M33 = -m.M33;
        result.M34 = -m.M34;
        result.M41 = -m.M41;
        result.M42 = -m.M42;
        result.M43 = -m.M43;
        result.M44 = -m.M44;
    }

    [Pure]
    public static Matrix Add(Matrix a, Matrix b)
    {
        Matrix result;
        result.M11 = a.M11 + b.M11;
        result.M12 = a.M12 + b.M12;
        result.M13 = a.M13 + b.M13;
        result.M14 = a.M14 + b.M14;
        result.M21 = a.M21 + b.M21;
        result.M22 = a.M22 + b.M22;
        result.M23 = a.M23 + b.M23;
        result.M24 = a.M24 + b.M24;
        result.M31 = a.M31 + b.M31;
        result.M32 = a.M32 + b.M32;
        result.M33 = a.M33 + b.M33;
        result.M34 = a.M34 + b.M34;
        result.M41 = a.M41 + b.M41;
        result.M42 = a.M42 + b.M42;
        result.M43 = a.M43 + b.M43;
        result.M44 = a.M44 + b.M44;
        return result;
    }

    [Pure]
    public static void Add(ref Matrix a, ref Matrix b, out Matrix result)
    {
        result.M11 = a.M11 + b.M11;
        result.M12 = a.M12 + b.M12;
        result.M13 = a.M13 + b.M13;
        result.M14 = a.M14 + b.M14;
        result.M21 = a.M21 + b.M21;
        result.M22 = a.M22 + b.M22;
        result.M23 = a.M23 + b.M23;
        result.M24 = a.M24 + b.M24;
        result.M31 = a.M31 + b.M31;
        result.M32 = a.M32 + b.M32;
        result.M33 = a.M33 + b.M33;
        result.M34 = a.M34 + b.M34;
        result.M41 = a.M41 + b.M41;
        result.M42 = a.M42 + b.M42;
        result.M43 = a.M43 + b.M43;
        result.M44 = a.M44 + b.M44;
    }

    [Pure]
    public static Matrix Subtract(Matrix a, Matrix b)
    {
        Matrix result;
        result.M11 = a.M11 - b.M11;
        result.M12 = a.M12 - b.M12;
        result.M13 = a.M13 - b.M13;
        result.M14 = a.M14 - b.M14;
        result.M21 = a.M21 - b.M21;
        result.M22 = a.M22 - b.M22;
        result.M23 = a.M23 - b.M23;
        result.M24 = a.M24 - b.M24;
        result.M31 = a.M31 - b.M31;
        result.M32 = a.M32 - b.M32;
        result.M33 = a.M33 - b.M33;
        result.M34 = a.M34 - b.M34;
        result.M41 = a.M41 - b.M41;
        result.M42 = a.M42 - b.M42;
        result.M43 = a.M43 - b.M43;
        result.M44 = a.M44 - b.M44;
        return result;
    }

    [Pure]
    public static void Subtract(ref Matrix a, ref Matrix b, out Matrix result)
    {
        result.M11 = a.M11 - b.M11;
        result.M12 = a.M12 - b.M12;
        result.M13 = a.M13 - b.M13;
        result.M14 = a.M14 - b.M14;
        result.M21 = a.M21 - b.M21;
        result.M22 = a.M22 - b.M22;
        result.M23 = a.M23 - b.M23;
        result.M24 = a.M24 - b.M24;
        result.M31 = a.M31 - b.M31;
        result.M32 = a.M32 - b.M32;
        result.M33 = a.M33 - b.M33;
        result.M34 = a.M34 - b.M34;
        result.M41 = a.M41 - b.M41;
        result.M42 = a.M42 - b.M42;
        result.M43 = a.M43 - b.M43;
        result.M44 = a.M44 - b.M44;
    }

    [Pure]
    public readonly void Multiply(in Matrix b, out Matrix result)
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

    [Pure]
    public static Matrix Multiply(in Matrix a, Matrix b)
    {
        Matrix m;
        m.M11 = a.M11 * b.M11 + a.M12 * b.M21 + a.M13 * b.M31 + a.M14 * b.M41;
        m.M12 = a.M11 * b.M12 + a.M12 * b.M22 + a.M13 * b.M32 + a.M14 * b.M42;
        m.M13 = a.M11 * b.M13 + a.M12 * b.M23 + a.M13 * b.M33 + a.M14 * b.M43;
        m.M14 = a.M11 * b.M14 + a.M12 * b.M24 + a.M13 * b.M34 + a.M14 * b.M44;
        m.M21 = a.M21 * b.M11 + a.M22 * b.M21 + a.M23 * b.M31 + a.M24 * b.M41;
        m.M22 = a.M21 * b.M12 + a.M22 * b.M22 + a.M23 * b.M32 + a.M24 * b.M42;
        m.M23 = a.M21 * b.M13 + a.M22 * b.M23 + a.M23 * b.M33 + a.M24 * b.M43;
        m.M24 = a.M21 * b.M14 + a.M22 * b.M24 + a.M23 * b.M34 + a.M24 * b.M44;
        m.M31 = a.M31 * b.M11 + a.M32 * b.M21 + a.M33 * b.M31 + a.M34 * b.M41;
        m.M32 = a.M31 * b.M12 + a.M32 * b.M22 + a.M33 * b.M32 + a.M34 * b.M42;
        m.M33 = a.M31 * b.M13 + a.M32 * b.M23 + a.M33 * b.M33 + a.M34 * b.M43;
        m.M34 = a.M31 * b.M14 + a.M32 * b.M24 + a.M33 * b.M34 + a.M34 * b.M44;
        m.M41 = a.M41 * b.M11 + a.M42 * b.M21 + a.M43 * b.M31 + a.M44 * b.M41;
        m.M42 = a.M41 * b.M12 + a.M42 * b.M22 + a.M43 * b.M32 + a.M44 * b.M42;
        m.M43 = a.M41 * b.M13 + a.M42 * b.M23 + a.M43 * b.M33 + a.M44 * b.M43;
        m.M44 = a.M41 * b.M14 + a.M42 * b.M24 + a.M43 * b.M34 + a.M44 * b.M44;
        return m;
    }

    [Pure]
    public static void Multiply(ref Matrix a, ref Matrix b, out Matrix m)
    {
        float num1 = a.M11 * b.M11 + a.M12 * b.M21 + a.M13 * b.M31 + a.M14 * b.M41;
        float num2 = a.M11 * b.M12 + a.M12 * b.M22 + a.M13 * b.M32 + a.M14 * b.M42;
        float num3 = a.M11 * b.M13 + a.M12 * b.M23 + a.M13 * b.M33 + a.M14 * b.M43;
        float num4 = a.M11 * b.M14 + a.M12 * b.M24 + a.M13 * b.M34 + a.M14 * b.M44;
        float num5 = a.M21 * b.M11 + a.M22 * b.M21 + a.M23 * b.M31 + a.M24 * b.M41;
        float num6 = a.M21 * b.M12 + a.M22 * b.M22 + a.M23 * b.M32 + a.M24 * b.M42;
        float num7 = a.M21 * b.M13 + a.M22 * b.M23 + a.M23 * b.M33 + a.M24 * b.M43;
        float num8 = a.M21 * b.M14 + a.M22 * b.M24 + a.M23 * b.M34 + a.M24 * b.M44;
        float num9 = a.M31 * b.M11 + a.M32 * b.M21 + a.M33 * b.M31 + a.M34 * b.M41;
        float num10 = a.M31 * b.M12 + a.M32 * b.M22 + a.M33 * b.M32 + a.M34 * b.M42;
        float num11 = a.M31 * b.M13 + a.M32 * b.M23 + a.M33 * b.M33 + a.M34 * b.M43;
        float num12 = a.M31 * b.M14 + a.M32 * b.M24 + a.M33 * b.M34 + a.M34 * b.M44;
        float num13 = a.M41 * b.M11 + a.M42 * b.M21 + a.M43 * b.M31 + a.M44 * b.M41;
        float num14 = a.M41 * b.M12 + a.M42 * b.M22 + a.M43 * b.M32 + a.M44 * b.M42;
        float num15 = a.M41 * b.M13 + a.M42 * b.M23 + a.M43 * b.M33 + a.M44 * b.M43;
        float num16 = a.M41 * b.M14 + a.M42 * b.M24 + a.M43 * b.M34 + a.M44 * b.M44;
        m.M11 = num1;
        m.M12 = num2;
        m.M13 = num3;
        m.M14 = num4;
        m.M21 = num5;
        m.M22 = num6;
        m.M23 = num7;
        m.M24 = num8;
        m.M31 = num9;
        m.M32 = num10;
        m.M33 = num11;
        m.M34 = num12;
        m.M41 = num13;
        m.M42 = num14;
        m.M43 = num15;
        m.M44 = num16;
    }

    [Pure]
    public static Matrix Multiply(Matrix a, float scaleFactor)
    {
        float num = scaleFactor;
        Matrix m;
        m.M11 = a.M11 * num;
        m.M12 = a.M12 * num;
        m.M13 = a.M13 * num;
        m.M14 = a.M14 * num;
        m.M21 = a.M21 * num;
        m.M22 = a.M22 * num;
        m.M23 = a.M23 * num;
        m.M24 = a.M24 * num;
        m.M31 = a.M31 * num;
        m.M32 = a.M32 * num;
        m.M33 = a.M33 * num;
        m.M34 = a.M34 * num;
        m.M41 = a.M41 * num;
        m.M42 = a.M42 * num;
        m.M43 = a.M43 * num;
        m.M44 = a.M44 * num;
        return m;
    }

    [Pure]
    public static void Multiply(ref Matrix a, float scaleFactor, out Matrix m)
    {
        float num = scaleFactor;
        m.M11 = a.M11 * num;
        m.M12 = a.M12 * num;
        m.M13 = a.M13 * num;
        m.M14 = a.M14 * num;
        m.M21 = a.M21 * num;
        m.M22 = a.M22 * num;
        m.M23 = a.M23 * num;
        m.M24 = a.M24 * num;
        m.M31 = a.M31 * num;
        m.M32 = a.M32 * num;
        m.M33 = a.M33 * num;
        m.M34 = a.M34 * num;
        m.M41 = a.M41 * num;
        m.M42 = a.M42 * num;
        m.M43 = a.M43 * num;
        m.M44 = a.M44 * num;
    }

    [Pure]
    public static Matrix Divide(Matrix a, Matrix b)
    {
        Matrix m;
        m.M11 = a.M11 / b.M11;
        m.M12 = a.M12 / b.M12;
        m.M13 = a.M13 / b.M13;
        m.M14 = a.M14 / b.M14;
        m.M21 = a.M21 / b.M21;
        m.M22 = a.M22 / b.M22;
        m.M23 = a.M23 / b.M23;
        m.M24 = a.M24 / b.M24;
        m.M31 = a.M31 / b.M31;
        m.M32 = a.M32 / b.M32;
        m.M33 = a.M33 / b.M33;
        m.M34 = a.M34 / b.M34;
        m.M41 = a.M41 / b.M41;
        m.M42 = a.M42 / b.M42;
        m.M43 = a.M43 / b.M43;
        m.M44 = a.M44 / b.M44;
        return m;
    }

    [Pure]
    public static void Divide(ref Matrix a, ref Matrix b, out Matrix m)
    {
        m.M11 = a.M11 / b.M11;
        m.M12 = a.M12 / b.M12;
        m.M13 = a.M13 / b.M13;
        m.M14 = a.M14 / b.M14;
        m.M21 = a.M21 / b.M21;
        m.M22 = a.M22 / b.M22;
        m.M23 = a.M23 / b.M23;
        m.M24 = a.M24 / b.M24;
        m.M31 = a.M31 / b.M31;
        m.M32 = a.M32 / b.M32;
        m.M33 = a.M33 / b.M33;
        m.M34 = a.M34 / b.M34;
        m.M41 = a.M41 / b.M41;
        m.M42 = a.M42 / b.M42;
        m.M43 = a.M43 / b.M43;
        m.M44 = a.M44 / b.M44;
    }

    [Pure]
    public static Matrix Divide(Matrix a, float divider)
    {
        float num = 1f / divider;
        Matrix m;
        m.M11 = a.M11 * num;
        m.M12 = a.M12 * num;
        m.M13 = a.M13 * num;
        m.M14 = a.M14 * num;
        m.M21 = a.M21 * num;
        m.M22 = a.M22 * num;
        m.M23 = a.M23 * num;
        m.M24 = a.M24 * num;
        m.M31 = a.M31 * num;
        m.M32 = a.M32 * num;
        m.M33 = a.M33 * num;
        m.M34 = a.M34 * num;
        m.M41 = a.M41 * num;
        m.M42 = a.M42 * num;
        m.M43 = a.M43 * num;
        m.M44 = a.M44 * num;
        return m;
    }

    [Pure]
    public static void Divide(ref Matrix a, float divider, out Matrix m)
    {
        float num = 1f / divider;
        m.M11 = a.M11 * num;
        m.M12 = a.M12 * num;
        m.M13 = a.M13 * num;
        m.M14 = a.M14 * num;
        m.M21 = a.M21 * num;
        m.M22 = a.M22 * num;
        m.M23 = a.M23 * num;
        m.M24 = a.M24 * num;
        m.M31 = a.M31 * num;
        m.M32 = a.M32 * num;
        m.M33 = a.M33 * num;
        m.M34 = a.M34 * num;
        m.M41 = a.M41 * num;
        m.M42 = a.M42 * num;
        m.M43 = a.M43 * num;
        m.M44 = a.M44 * num;
    }

    [Pure]
    public static Matrix operator -(Matrix b)
    {
        Matrix m;
        m.M11 = -b.M11;
        m.M12 = -b.M12;
        m.M13 = -b.M13;
        m.M14 = -b.M14;
        m.M21 = -b.M21;
        m.M22 = -b.M22;
        m.M23 = -b.M23;
        m.M24 = -b.M24;
        m.M31 = -b.M31;
        m.M32 = -b.M32;
        m.M33 = -b.M33;
        m.M34 = -b.M34;
        m.M41 = -b.M41;
        m.M42 = -b.M42;
        m.M43 = -b.M43;
        m.M44 = -b.M44;
        return m;
    }

    [Pure]
    public static bool operator ==(in Matrix a, in Matrix b)
    {
        return a.M11 == b.M11 && a.M22 == b.M22 && a.M33 == b.M33 && a.M44 == b.M44
            && a.M12 == b.M12 && a.M13 == b.M13 && a.M14 == b.M14 && a.M21 == b.M21
            && a.M23 == b.M23 && a.M24 == b.M24 && a.M31 == b.M31 && a.M32 == b.M32
            && a.M34 == b.M34 && a.M41 == b.M41 && a.M42 == b.M42 && a.M43 == b.M43;
    }

    [Pure]
    public static bool operator !=(in Matrix a, in Matrix b)
    {
        return a.M11 != b.M11 || a.M12 != b.M12 || a.M13 != b.M13 || a.M14 != b.M14
            || a.M21 != b.M21 || a.M22 != b.M22 || a.M23 != b.M23 || a.M24 != b.M24
            || a.M31 != b.M31 || a.M32 != b.M32 || a.M33 != b.M33 || a.M34 != b.M34
            || a.M41 != b.M41 || a.M42 != b.M42 || a.M43 != b.M43 || a.M44 != b.M44;
    }

    [Pure]
    public static Matrix operator +(in Matrix a, in Matrix b)
    {
        Matrix m;
        m.M11 = a.M11 + b.M11;
        m.M12 = a.M12 + b.M12;
        m.M13 = a.M13 + b.M13;
        m.M14 = a.M14 + b.M14;
        m.M21 = a.M21 + b.M21;
        m.M22 = a.M22 + b.M22;
        m.M23 = a.M23 + b.M23;
        m.M24 = a.M24 + b.M24;
        m.M31 = a.M31 + b.M31;
        m.M32 = a.M32 + b.M32;
        m.M33 = a.M33 + b.M33;
        m.M34 = a.M34 + b.M34;
        m.M41 = a.M41 + b.M41;
        m.M42 = a.M42 + b.M42;
        m.M43 = a.M43 + b.M43;
        m.M44 = a.M44 + b.M44;
        return m;
    }

    [Pure]
    public static Matrix operator -(in Matrix a, in Matrix b)
    {
        Matrix m;
        m.M11 = a.M11 - b.M11;
        m.M12 = a.M12 - b.M12;
        m.M13 = a.M13 - b.M13;
        m.M14 = a.M14 - b.M14;
        m.M21 = a.M21 - b.M21;
        m.M22 = a.M22 - b.M22;
        m.M23 = a.M23 - b.M23;
        m.M24 = a.M24 - b.M24;
        m.M31 = a.M31 - b.M31;
        m.M32 = a.M32 - b.M32;
        m.M33 = a.M33 - b.M33;
        m.M34 = a.M34 - b.M34;
        m.M41 = a.M41 - b.M41;
        m.M42 = a.M42 - b.M42;
        m.M43 = a.M43 - b.M43;
        m.M44 = a.M44 - b.M44;
        return m;
    }

    [Pure]
    public static Matrix operator *(in Matrix a, in Matrix b)
    {
        Matrix m;
        m.M11 = (a.M11 * b.M11 + a.M12 * b.M21 + a.M13 * b.M31 + a.M14 * b.M41);
        m.M12 = (a.M11 * b.M12 + a.M12 * b.M22 + a.M13 * b.M32 + a.M14 * b.M42);
        m.M13 = (a.M11 * b.M13 + a.M12 * b.M23 + a.M13 * b.M33 + a.M14 * b.M43);
        m.M14 = (a.M11 * b.M14 + a.M12 * b.M24 + a.M13 * b.M34 + a.M14 * b.M44);
        m.M21 = (a.M21 * b.M11 + a.M22 * b.M21 + a.M23 * b.M31 + a.M24 * b.M41);
        m.M22 = (a.M21 * b.M12 + a.M22 * b.M22 + a.M23 * b.M32 + a.M24 * b.M42);
        m.M23 = (a.M21 * b.M13 + a.M22 * b.M23 + a.M23 * b.M33 + a.M24 * b.M43);
        m.M24 = (a.M21 * b.M14 + a.M22 * b.M24 + a.M23 * b.M34 + a.M24 * b.M44);
        m.M31 = (a.M31 * b.M11 + a.M32 * b.M21 + a.M33 * b.M31 + a.M34 * b.M41);
        m.M32 = (a.M31 * b.M12 + a.M32 * b.M22 + a.M33 * b.M32 + a.M34 * b.M42);
        m.M33 = (a.M31 * b.M13 + a.M32 * b.M23 + a.M33 * b.M33 + a.M34 * b.M43);
        m.M34 = (a.M31 * b.M14 + a.M32 * b.M24 + a.M33 * b.M34 + a.M34 * b.M44);
        m.M41 = (a.M41 * b.M11 + a.M42 * b.M21 + a.M43 * b.M31 + a.M44 * b.M41);
        m.M42 = (a.M41 * b.M12 + a.M42 * b.M22 + a.M43 * b.M32 + a.M44 * b.M42);
        m.M43 = (a.M41 * b.M13 + a.M42 * b.M23 + a.M43 * b.M33 + a.M44 * b.M43);
        m.M44 = (a.M41 * b.M14 + a.M42 * b.M24 + a.M43 * b.M34 + a.M44 * b.M44);
        return m;
    }

    [Pure]
    public static Matrix operator *(in Matrix a, float scaleFactor)
    {
        float num = scaleFactor;
        Matrix m;
        m.M11 = a.M11 * num;
        m.M12 = a.M12 * num;
        m.M13 = a.M13 * num;
        m.M14 = a.M14 * num;
        m.M21 = a.M21 * num;
        m.M22 = a.M22 * num;
        m.M23 = a.M23 * num;
        m.M24 = a.M24 * num;
        m.M31 = a.M31 * num;
        m.M32 = a.M32 * num;
        m.M33 = a.M33 * num;
        m.M34 = a.M34 * num;
        m.M41 = a.M41 * num;
        m.M42 = a.M42 * num;
        m.M43 = a.M43 * num;
        m.M44 = a.M44 * num;
        return m;
    }

    [Pure]
    public static Matrix operator *(float scaleFactor, in Matrix b)
    {
        float num = scaleFactor;
        Matrix m;
        m.M11 = b.M11 * num;
        m.M12 = b.M12 * num;
        m.M13 = b.M13 * num;
        m.M14 = b.M14 * num;
        m.M21 = b.M21 * num;
        m.M22 = b.M22 * num;
        m.M23 = b.M23 * num;
        m.M24 = b.M24 * num;
        m.M31 = b.M31 * num;
        m.M32 = b.M32 * num;
        m.M33 = b.M33 * num;
        m.M34 = b.M34 * num;
        m.M41 = b.M41 * num;
        m.M42 = b.M42 * num;
        m.M43 = b.M43 * num;
        m.M44 = b.M44 * num;
        return m;
    }

    [Pure]
    public static Matrix operator /(in Matrix a, in Matrix b)
    {
        Matrix m;
        m.M11 = a.M11 / b.M11;
        m.M12 = a.M12 / b.M12;
        m.M13 = a.M13 / b.M13;
        m.M14 = a.M14 / b.M14;
        m.M21 = a.M21 / b.M21;
        m.M22 = a.M22 / b.M22;
        m.M23 = a.M23 / b.M23;
        m.M24 = a.M24 / b.M24;
        m.M31 = a.M31 / b.M31;
        m.M32 = a.M32 / b.M32;
        m.M33 = a.M33 / b.M33;
        m.M34 = a.M34 / b.M34;
        m.M41 = a.M41 / b.M41;
        m.M42 = a.M42 / b.M42;
        m.M43 = a.M43 / b.M43;
        m.M44 = a.M44 / b.M44;
        return m;
    }

    [Pure]
    public static Matrix operator /(in Matrix a, float divider)
    {
        float num = 1f / divider;
        Matrix m;
        m.M11 = a.M11 * num;
        m.M12 = a.M12 * num;
        m.M13 = a.M13 * num;
        m.M14 = a.M14 * num;
        m.M21 = a.M21 * num;
        m.M22 = a.M22 * num;
        m.M23 = a.M23 * num;
        m.M24 = a.M24 * num;
        m.M31 = a.M31 * num;
        m.M32 = a.M32 * num;
        m.M33 = a.M33 * num;
        m.M34 = a.M34 * num;
        m.M41 = a.M41 * num;
        m.M42 = a.M42 * num;
        m.M43 = a.M43 * num;
        m.M44 = a.M44 * num;
        return m;
    }
}
