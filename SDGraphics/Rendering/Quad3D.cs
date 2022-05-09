using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using SDGraphics;
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

    static Vector3 RotateAroundPoint(in Vector3 p, in Vector2 center, float radians)
    {
        var pos = new Vector2(p.X, p.Y);
        pos = pos.RotateAroundPoint(center, radians);
        return new Vector3(pos, p.Z);
    }

    public void RotateAroundTL(float radians)
    {
        if (radians != 0f)
        {
            var center = A.ToVec2();
            A = RotateAroundPoint(A, center, radians);
            B = RotateAroundPoint(B, center, radians);
            C = RotateAroundPoint(C, center, radians);
            D = RotateAroundPoint(D, center, radians);
        }
    }

    public void RotateAroundCenter(float radians)
    {
        if (radians != 0f)
        {
            var center = ((A + C) / 2f).ToVec2();
            A = RotateAroundPoint(A, center, radians);
            B = RotateAroundPoint(B, center, radians);
            C = RotateAroundPoint(C, center, radians);
            D = RotateAroundPoint(D, center, radians);
        }
    }
}
