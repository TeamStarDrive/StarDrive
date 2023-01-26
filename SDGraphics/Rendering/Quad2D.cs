using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SDGraphics.Rendering;

/// <summary>
/// A single 2D Quad that MAY be rotated
///
/// A---B
/// | + |
/// D---C
/// </summary>
public struct Quad2D
{
    public Vector2 A;
    public Vector2 B;
    public Vector2 C;
    public Vector2 D;

    public Quad2D(in RectF rect)
    {
        float left = rect.X;
        float right = left + rect.W;
        float top = rect.Y;
        float bot = top + rect.H;

        A = new Vector2(left, top);
        B = new Vector2(right, top);
        C = new Vector2(right, bot);
        D = new Vector2(left, bot);
    }

    public Quad2D(float x, float y, float w, float h)
    {
        float right = x + w;
        float bot = y + h;

        A = new Vector2(x, y);
        B = new Vector2(right, y);
        C = new Vector2(right, bot);
        D = new Vector2(x, bot);
    }

    public Quad2D(in Vector2 center, in Vector2 size)
    {
        // calculate these with double precision to improve accuracy
        double sx2 = size.X / 2.0;
        double sy2 = size.Y / 2.0;
        float left = (float)(center.X - sx2);
        float right = (float)(center.X + sx2);
        float top = (float)(center.Y - sy2);
        float bot = (float)(center.Y + sy2);

        A = new Vector2(left, top);
        B = new Vector2(right, top);
        C = new Vector2(right, bot);
        D = new Vector2(left, bot);
    }

    public void RotateAroundTL(float radians)
    {
        if (radians != 0f)
        {
            var center = A;
            A = A.RotateAroundPoint(center, radians);
            B = B.RotateAroundPoint(center, radians);
            C = C.RotateAroundPoint(center, radians);
            D = D.RotateAroundPoint(center, radians);
        }
    }

    public void RotateAroundCenter(float radians)
    {
        if (radians != 0f)
        {
            var center = ((A + C) / 2f);
            A = A.RotateAroundPoint(center, radians);
            B = B.RotateAroundPoint(center, radians);
            C = C.RotateAroundPoint(center, radians);
            D = D.RotateAroundPoint(center, radians);
        }
    }
}
