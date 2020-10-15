﻿using Microsoft.Xna.Framework;
using System.Diagnostics.Contracts;
using System.Runtime.CompilerServices;

namespace Ship_Game
{
    /// <summary>
    /// Axis-Aligned 2D Bounding Box
    /// Specific to the collision system
    /// </summary>
    public struct AABoundingBox2D
    {
        public float X1, Y1;
        public float X2, Y2;

        public float Width => X2 - X1;
        public float Height => Y2 - Y1;

        public float CenterX => (X1 + X2) * 0.5f;
        public float CenterY => (Y1 + Y2) * 0.5f;

        public Vector2 Center => new Vector2(CenterX, CenterY);
        public Vector2 Size => new Vector2(Width, Height);

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

        public AABoundingBox2D(in Vector2 topLeft, in Vector2 botRight)
        {
            X1 = topLeft.X;
            Y1 = topLeft.Y;
            X2 = botRight.X;
            Y2 = botRight.Y;
        }

        public AABoundingBox2D(GameplayObject go)
        {
            float x = go.Center.X;
            float y = go.Center.Y;
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

            X1 = x - rx;
            X2 = x + rx;
            Y1 = y - ry;
            Y2 = y + ry;
        }

        [Pure][MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool Overlaps(in AABoundingBox2D r)
        {
            return X1 <= r.X2 && X2 > r.X1
                && Y1 <= r.Y2 && Y2 > r.Y1;
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

        [Pure][MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool Overlaps(in AABoundingBox2Di r)
        {
            return X1 <= r.X2 && X2 > r.X1
                && Y1 <= r.Y2 && Y2 > r.Y1;
        }
    }
}