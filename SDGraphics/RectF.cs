using System;
using System.Diagnostics.Contracts;
using System.Runtime.CompilerServices;

using XnaVector2 = Microsoft.Xna.Framework.Vector2;
using XnaRect = Microsoft.Xna.Framework.Rectangle;

namespace SDGraphics;

public struct RectF
{
    public float X;
    public float Y;
    public float W;
    public float H;

    public float Left => X;
    public float Top => Y;
    public float Right => X + W;
    public float Bottom => Y + H;
    
    public Vector2 Center => new(X + W*0.5f, Y + H*0.5f);
    public Vector2 Size => new(W, H);

    public Vector2 TopLeft => new(X, Y);
    public Vector2 BotRight => new(X + W, Y + H);

    public override string ToString() => $"{{X:{X} Y:{Y} W:{W} H:{H}}}";

    public RectF(float x, float y, float w, float h)
    {
        X = x;
        Y = y;
        W = w;
        H = h;
    }

    public RectF(double x, double y, double w, double h)
    {
        X = (float)x;
        Y = (float)y;
        W = (float)w;
        H = (float)h;
    }

    public RectF(Vector2 pos, float w, float h)
    {
        X = pos.X;
        Y = pos.Y;
        W = w;
        H = h;
    }

    public RectF(XnaVector2 pos, float w, float h)
    {
        X = pos.X;
        Y = pos.Y;
        W = w;
        H = h;
    }

    public RectF(Vector2 pos, Vector2 size)
    {
        X = pos.X;
        Y = pos.Y;
        W = size.X;
        H = size.Y;
    }

    public RectF(XnaVector2 pos, XnaVector2 size)
    {
        X = pos.X;
        Y = pos.Y;
        W = size.X;
        H = size.Y;
    }

    public RectF(in Vector2d pos, in Vector2d size)
    {
        X = (float)pos.X;
        Y = (float)pos.Y;
        W = (float)size.X;
        H = (float)size.Y;
    }

    public RectF(in XnaRect r)
    {
        X = r.X;
        Y = r.Y;
        W = r.Width;
        H = r.Height;
    }

    public static implicit operator XnaRect(in RectF r)
    {
        return new XnaRect((int)r.X, (int)r.Y, (int)r.W, (int)r.H);
    }

    public static implicit operator Rectangle(in RectF r)
    {
        return new Rectangle((int)r.X, (int)r.Y, (int)r.W, (int)r.H);
    }

    /// <summary>
    /// This creates a rectangle from points X1Y1 X2Y2, instead of TopLeftXY+WidthHeight
    /// o------o- y1
    /// |      |
    /// |      |
    /// o------o- y2
    /// '      '
    /// x1     x2
    /// </summary>
    public static RectF FromPoints(float x1, float x2, float y1, float y2)
    {
        float w = x2 - x1;
        float h = y2 - y1;
        return new RectF(x1, y1, w, h);
    }

    /// <summary>
    /// This creates a rectangle from center point and radius, the rectangle encloses this circle
    /// </summary>
    public static RectF FromPointRadius(Vector2 center, float radius)
    {
        return new RectF(center.X - radius, center.Y - radius, radius*2, radius*2);
    }

    /// <summary>
    /// This creates a rectangle from center point and radius, the rectangle encloses this circle
    /// </summary>
    public static RectF FromPointRadius(XnaVector2 center, float radius)
    {
        return new RectF(center.X - radius, center.Y - radius, radius*2, radius*2);
    }

    /// <summary>
    /// This creates a rectangle from center and width/height
    /// </summary>
    public static RectF FromCenter(Vector2 center, float width, float height)
    {
        return new RectF(center.X - width*0.5f, center.Y - height*0.5f, width, height);
    }

    public static RectF FromCenter(in Vector2d center, in Vector2d size)
    {
        return new RectF(center.X - size.X * 0.5, center.Y - size.Y * 0.5, size.X, size.Y);
    }

    public static RectF FromCenter(in Vector2d center, double width, double height)
    {
        return new RectF(center.X - width * 0.5, center.Y - height * 0.5, width, height);
    }

    [Pure][MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool Overlaps(in RectF r)
    {
        return Left <= r.Right && Right > r.Left
            && Top <= r.Bottom && Bottom > r.Top;
    }

    [Pure] public RectF ScaledBy(float scale)
    {
        if (scale.AlmostEqual(1f))
            return this;
        float extrude = scale - 1f;
        float extrudeX = (W*extrude);
        float extrudeY = (H*extrude);
        return new RectF(X - extrudeX, Y - extrudeY, W + extrudeX*2, H + extrudeY*2);
    }
}
