﻿using System;
namespace SDGraphics.Rendering;

/// <summary>
/// A single 3D Quad that MAY be rotated
///
/// A---B
/// | + |
/// D---C
/// </summary>
public struct Quad3D
{
    public Vector3 A; // TopLeft
    public Vector3 B; // TopRight
    public Vector3 C; // BotRight
    public Vector3 D; // BotLeft

    public Quad3D(in RectF rect, float z)
    {
        float left = rect.X;
        float right = left + rect.W;
        float top = rect.Y;
        float bot = top + rect.H;
        A = new Vector3(left, top, z);
        B = new Vector3(right, top, z);
        C = new Vector3(right, bot, z);
        D = new Vector3(left, bot, z);
    }

    public Quad3D(in Vector3 center, in Vector2 size)
    {
        // calculate these with double precision to improve accuracy
        double sx2 = size.X / 2.0;
        double sy2 = size.Y / 2.0;
        float left = (float)(center.X - sx2);
        float right = (float)(center.X + sx2);
        float top = (float)(center.Y - sy2);
        float bot = (float)(center.Y + sy2);
        float z = center.Z;
        A = new Vector3(left, top, z);
        B = new Vector3(right, top, z);
        C = new Vector3(right, bot, z);
        D = new Vector3(left, bot, z);
    }

    /// <summary>
    /// Creates a Quad from point and radius, with an additional Z value
    /// </summary>
    public Quad3D(in Vector2 center, float radius, float zValue)
    {
        float left = (center.X - radius);
        float right = (center.X + radius);
        float top = (center.Y - radius);
        float bot = (center.Y + radius);
        A = new Vector3(left, top, zValue);
        B = new Vector3(right, top, zValue);
        C = new Vector3(right, bot, zValue);
        D = new Vector3(left, bot, zValue);
    }

    /// <summary>
    /// Creates a Quad from a 3D point and radius
    /// </summary>
    public Quad3D(in Vector3 center, float radius)
    {
        float left = (center.X - radius);
        float right = (center.X + radius);
        float top = (center.Y - radius);
        float bot = (center.Y + radius);
        A = new Vector3(left, top, center.Z);
        B = new Vector3(right, top, center.Z);
        C = new Vector3(right, bot, center.Z);
        D = new Vector3(left, bot, center.Z);
    }

    /// <summary>
    /// A quad from a 2D line
    /// RedFox - This is from my 3D utility library, https://github.com/RedFox20/AlphaGL
    /// </summary>
    /// <param name="p1">Start point</param>
    /// <param name="p2">End point</param>
    /// <param name="width">Width of the line</param>
    /// <param name="zValue">Z position</param>
    public Quad3D(in Vector2 p1, in Vector2 p2, float width, float zValue)
    {
        //  0---3   0\``3
        //  | + |   | \ |
        //  1---2   1--\2
        float x1 = p1.X, x2 = p2.X;
        float y1 = p1.Y, y2 = p2.Y;

        // construct a left vector (Y, -X)
        float dx = p2.Y - p1.Y;
        float dy = p1.X - p2.X;
        double invLen = (width * 0.5) / Math.Sqrt(dx*dx + dy*dy);
        float cx = (float)(dx * invLen); // center xy offsets
        float cy = (float)(dy * invLen);

        A.X = x1 - cx; A.Y = y1 - cy; A.Z = zValue; // left-top
        B.X = x2 - cx; B.Y = y2 - cy; B.Z = zValue; // left-bottom
        C.X = x2 + cx; C.Y = y2 + cy; C.Z = zValue; // right-bottom
        D.X = x1 + cx; D.Y = y1 + cy; D.Z = zValue; // right-bottom
    }

    /// <summary>
    /// A quad from a 3D line
    /// RedFox - This is from my 3D utility library, https://github.com/RedFox20/AlphaGL
    /// </summary>
    /// <param name="p1">Start point</param>
    /// <param name="p2">End point</param>
    /// <param name="width">Width of the line</param>
    public Quad3D(in Vector3 p1, in Vector3 p2, float width)
    {
        //  0---3   0\``3
        //  | + |   | \ |
        //  1---2   1--\2
        float x1 = p1.X, x2 = p2.X;
        float y1 = p1.Y, y2 = p2.Y;
        float z1 = p1.Z, z2 = p2.Z;

        // construct a left vector (Y, -X)
        float dx = y2 - y1;
        float dy = x1 - x2;
        double invLen = (width * 0.5) / Math.Sqrt(dx*dx + dy*dy);
        float cx = (float)(dx * invLen); // center xy offsets
        float cy = (float)(dy * invLen);

        A.X = x1 - cx; A.Y = y1 - cy; A.Z = z1; // left-top
        B.X = x2 - cx; B.Y = y2 - cy; B.Z = z2; // left-bottom
        C.X = x2 + cx; C.Y = y2 + cy; C.Z = z2; // right-bottom
        D.X = x1 + cx; D.Y = y1 + cy; D.Z = z1; // right-bottom
    }

    static Vector3 RotateAroundPoint(in Vector3 p, in Vector2 center, float radians)
    {
        Vector2 pos = new(p.X, p.Y);
        pos = pos.RotateAroundPoint(center, radians);
        return new Vector3(pos, p.Z);
    }

    public readonly Quad3D RotatedAroundTL(float radians)
    {
        Quad3D quad = this;
        quad.RotateAroundTL(radians);
        return quad;
    }

    public void RotateAroundTL(float radians)
    {
        if (radians != 0f)
        {
            Vector2 center = A.ToVec2();
            A = RotateAroundPoint(A, center, radians);
            B = RotateAroundPoint(B, center, radians);
            C = RotateAroundPoint(C, center, radians);
            D = RotateAroundPoint(D, center, radians);
        }
    }
    
    public readonly Quad3D RotatedAroundCenter(float radians)
    {
        Quad3D quad = this;
        quad.RotateAroundCenter(radians);
        return quad;
    }

    public void RotateAroundCenter(float radians)
    {
        if (radians != 0f)
        {
            Vector2 center = ((A + C) / 2f).ToVec2();
            A = RotateAroundPoint(A, center, radians);
            B = RotateAroundPoint(B, center, radians);
            C = RotateAroundPoint(C, center, radians);
            D = RotateAroundPoint(D, center, radians);
        }
    }
}
