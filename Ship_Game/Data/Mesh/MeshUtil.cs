using System;
using XnaBoundingBox = Microsoft.Xna.Framework.BoundingBox;
namespace Ship_Game.Data.Mesh;

public static class MeshUtil
{
    public static float Radius(this XnaBoundingBox bounds)
    {
        // get all diameters of the BB
        float dx = bounds.Max.X - bounds.Min.X;
        float dy = bounds.Max.Y - bounds.Min.Y;
        float dz = bounds.Max.Z - bounds.Min.Z;

        // and pick the largest diameter
        float maxDiameter = Math.Max(dx, Math.Max(dy, dz));
        return maxDiameter * 0.5f;
    }

    // Joins two bounding boxes into a single bigger bb
    public static XnaBoundingBox Join(this XnaBoundingBox a, in XnaBoundingBox b)
    {
        var bb = new XnaBoundingBox();
        bb.Min.X = Math.Min(a.Min.X, b.Min.X);
        bb.Min.Y = Math.Min(a.Min.Y, b.Min.Y);
        bb.Min.Z = Math.Min(a.Min.Z, b.Min.Z);

        bb.Max.X = Math.Max(a.Max.X, b.Max.X);
        bb.Max.Y = Math.Max(a.Max.Y, b.Max.Y);
        bb.Max.Z = Math.Max(a.Max.Z, b.Max.Z);
        return bb;
    }
}
