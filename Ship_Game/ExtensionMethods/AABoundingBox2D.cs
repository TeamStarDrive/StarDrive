﻿using System;
using System.Diagnostics.Contracts;
using System.Runtime.CompilerServices;
using SDGraphics;
using Ship_Game.Spatial;
using Vector2 = SDGraphics.Vector2;

namespace Ship_Game;

/// <summary>
/// Axis-Aligned 2D Bounding Box
/// Specific to the collision system
/// </summary>
public struct AABoundingBox2D
{
    public float X1, Y1;
    public float X2, Y2;

    public readonly float Width => X2 - X1;
    public readonly float Height => Y2 - Y1;

    public readonly float CenterX => (X1 + X2) * 0.5f;
    public readonly float CenterY => (Y1 + Y2) * 0.5f;
        
    public readonly Vector2 Center => new((X1 + X2) * 0.5f, (Y1 + Y2) * 0.5f);
    public readonly Vector2 Size => new(Width, Height);
    public readonly Vector2 TopLeft => new(X1,Y1);
    public readonly Vector2 BotRight => new(X2,Y2);

    public readonly float Area => (X2 - X1) * (Y2 - Y1);

    // Gets the averaged radius of this bounding box (not accurate)
    public readonly float Radius => ((X2 - X1) + (Y2 - Y1)) * 0.25f;

    // Length of the diagonal which crosses this AABB from TopLeft to BottomRight
    public readonly float Diagonal
    {
        get
        {
            float w = X2 - X1;
            float h = Y2 - Y1;
            return (float)Math.Sqrt(w*w + h*h);
        }
    }

    public readonly bool IsEmpty => (X1 == X2) || (Y1 == Y2);

    public override string ToString()
    {
        return $"X1={X1} Y1={Y1} X2={X2} Y2={Y2}";
    }

    public AABoundingBox2D(float x1, float y1, float x2, float y2)
    {
        X1 = x1;
        Y1 = y1;
        X2 = x2;
        Y2 = y2;
    }

    public AABoundingBox2D(Vector2 center, float radius)
    {
        X1 = center.X - radius;
        Y1 = center.Y - radius;
        X2 = center.X + radius;
        Y2 = center.Y + radius;
    }

    // WARNING: The vector order must be correct, there is no validation!
    // Use AABoundingBox2D.FromIrregularPoints() if the points are random
    public AABoundingBox2D(in Vector2 topLeft, in Vector2 botRight)
    {
        X1 = topLeft.X;
        Y1 = topLeft.Y;
        X2 = botRight.X;
        Y2 = botRight.Y;
    }

    public AABoundingBox2D(in RectF rect)
    {
        X1 = rect.X;
        Y1 = rect.Y;
        X2 = rect.X + rect.W;
        Y2 = rect.Y + rect.H;
    }

    public AABoundingBox2D(SpatialObjectBase go)
    {
        float x = go.Position.X;
        float y = go.Position.Y;
        float rx, ry;

        // beam AABB's is a special case
        if (go.Type == GameObjectType.Beam)
        {
            var beam = (Beam)go;
            rx = beam.RadiusX;
            ry = beam.RadiusY;
        }
        else
        {
            rx = ry = go.Radius;
        }

        X1 = (x - rx);
        X2 = (x + rx);
        Y1 = (y - ry);
        Y2 = (y + ry);
    }

    [Pure][MethodImpl(MethodImplOptions.AggressiveInlining)]
    public readonly bool Overlaps(in AABoundingBox2D r)
    {
        // NOTE: >= vs > determines whether there's a match if rectangles touch
        return X1 <= r.X2 && X2 >= r.X1
            && Y1 <= r.Y2 && Y2 >= r.Y1;
    }
        
    [Pure][MethodImpl(MethodImplOptions.AggressiveInlining)]
    public readonly bool Overlaps(float cx, float cy, float radius)
    {
        // find the nearest point on the rectangle to the center of the circle
        float nearestX = Math.Max(X1, Math.Min(cx, X2));
        float nearestY = Math.Max(Y1, Math.Min(cy, Y2));
        float dx = nearestX - cx;
        float dy = nearestY - cy;
        return (dx*dx + dy*dy) <= (radius*radius);
    }

    // expands this bounding box to include the new point
    public void Expand(in Vector2 pos)
    {
        float x = pos.X;
        if      (x < X1) X1 = x;
        else if (X2 < x) X2 = x;
        float y = pos.Y;
        if      (y < Y1) Y1 = y;
        else if (Y2 < y) Y2 = y;
    }

    // merges two bounding box into a new bigger bounding box
    public AABoundingBox2D Merge(in AABoundingBox2D bounds)
    {
        return new(Math.Min(X1, bounds.X1),
            Math.Min(Y1, bounds.Y1),
            Math.Max(X2, bounds.X2),
            Math.Max(Y2, bounds.Y2));
    }

    // Create a rectangle from 2 points that don't need to be sorted in screen space
    // This is ideal for user selection cases where Start and End points can be anywhere on screen
    public static AABoundingBox2D FromIrregularPoints(in Vector2 a, in Vector2 b)
    {
        AABoundingBox2D r;
        r.X1 = Math.Min(a.X, b.X);
        r.X2 = Math.Max(a.X, b.X);
        r.Y1 = Math.Min(a.Y, b.Y);
        r.Y2 = Math.Max(a.Y, b.Y);
        return r;
    }
        
    public static bool operator==(in AABoundingBox2D a, in AABoundingBox2D b)
    {
        return a.X1 == b.X1 && a.X2 == b.X2 && a.Y1 == b.Y1 && a.Y2 == b.Y2;
    }

    public static bool operator!=(in AABoundingBox2D a, in AABoundingBox2D b)
    {
        return a.X1 != b.X1 || a.X2 != b.X2 || a.Y1 != b.Y1 || a.Y2 != b.Y2;
    }

    public override bool Equals(object obj)
    {
        return obj is AABoundingBox2D d && X1 == d.X1 && Y1 == d.Y1 && X2 == d.X2 && Y2 == d.Y2;
    }

    public override int GetHashCode()
    {
        int hashCode = 268039418;
        hashCode = hashCode * -1521134295 + X1.GetHashCode();
        hashCode = hashCode * -1521134295 + Y1.GetHashCode();
        hashCode = hashCode * -1521134295 + X2.GetHashCode();
        hashCode = hashCode * -1521134295 + Y2.GetHashCode();
        return hashCode;
    }
}

/// <summary>
/// Axis-Aligned 2D Bounding Box with INTEGER precision
/// Specific to the collision system
/// </summary>
public struct AABoundingBox2Di
{
    public int X1, Y1;
    public int X2, Y2;

    public int Width => X2 - X1;
    public int Height => Y2 - Y1;

    public readonly Vector2 Center => new((X1+X2)*0.5f, (Y1+Y2)*0.5f);
    public readonly bool IsEmpty => (X1 == X2) || (Y1 == Y2);

    public AABoundingBox2Di(int x1, int y1, int x2, int y2)
    {
        X1 = x1;
        Y1 = y1;
        X2 = x2;
        Y2 = y2;
    }

    public AABoundingBox2Di(in AABoundingBox2D r)
    {
        X1 = (int)r.X1;
        Y1 = (int)r.Y1;
        X2 = (int)r.X2;
        Y2 = (int)r.Y2;
    }

    public AABoundingBox2Di(in RectF r)
    {
        X1 = (int)r.Left;
        Y1 = (int)r.Top;
        X2 = (int)r.Right;
        Y2 = (int)r.Bottom;
    }

    public AABoundingBox2Di(SpatialObjectBase go)
    {
        int x = (int)go.Position.X;
        int y = (int)go.Position.Y;
        int rx, ry;

        // beam AABB's is a special case
        if (go.Type == GameObjectType.Beam)
        {
            var beam = (Beam)go;
            rx = beam.RadiusX;
            ry = beam.RadiusY;
        }
        else
        {
            rx = ry = (int)(go.Radius + 0.5f); // ceil
        }

        X1 = (x - rx);
        X2 = (x + rx);
        Y1 = (y - ry);
        Y2 = (y + ry);
    }

    [Pure][MethodImpl(MethodImplOptions.AggressiveInlining)]
    public readonly bool Overlaps(in AABoundingBox2Di r)
    {
        // NOTE: >= vs > determines whether there's a match if rectangles touch
        return X1 <= r.X2 && X2 >= r.X1
            && Y1 <= r.Y2 && Y2 >= r.Y1;
    }
}

public struct AABoundingBox2Dd
{
    public double X1, Y1;
    public double X2, Y2;

    public readonly double Width => X2 - X1;
    public readonly double Height => Y2 - Y1;
        
    public override string ToString()
    {
        return $"X1={X1} Y1={Y1} X2={X2} Y2={Y2}";
    }

    public AABoundingBox2Dd(double x1, double y1, double x2, double y2)
    {
        X1 = x1;
        Y1 = y1;
        X2 = x2;
        Y2 = y2;
    }

    // WARNING: The vector order must be correct, there is no validation!
    // Use AABoundingBox2D.FromIrregularPoints() if the points are random
    public AABoundingBox2Dd(in Vector2d topLeft, in Vector2d botRight)
    {
        X1 = topLeft.X;
        Y1 = topLeft.Y;
        X2 = botRight.X;
        Y2 = botRight.Y;
    }

    // WARNING: The vector order must be correct, there is no validation!
    // Use AABoundingBox2D.FromIrregularPoints() if the points are random
    public AABoundingBox2Dd(in Vector3d topLeft, in Vector3d botRight)
    {
        X1 = topLeft.X;
        Y1 = topLeft.Y;
        X2 = botRight.X;
        Y2 = botRight.Y;
    }

    public static implicit operator AABoundingBox2D(in AABoundingBox2Dd bb)
    {
        return new AABoundingBox2D((float)bb.X1, (float)bb.Y1, (float)bb.X2, (float)bb.Y2);
    }

    [Pure][MethodImpl(MethodImplOptions.AggressiveInlining)]
    public readonly bool Overlaps(Vector2 pos, float radius)
    {
        // NOTE: >= vs > determines whether there's a match if rectangles touch
        return X1 <= (pos.X + radius) && X2 >= (pos.X - radius)
            && Y1 <= (pos.Y + radius) && Y2 >= (pos.Y - radius);
    }

    [Pure][MethodImpl(MethodImplOptions.AggressiveInlining)]
    public readonly bool Overlaps(in RectF r)
    {
        // NOTE: >= vs > determines whether there's a match if rectangles touch
        return X1 <= (r.X+r.W) && X2 >= r.X
            && Y1 <= (r.Y+r.H) && Y2 >= r.Y;
    }
}
