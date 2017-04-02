// Decompiled with JetBrains decompiler
// Type: SynapseGaming.LightingSystem.Core.TriangleRenderHelper
// Assembly: SynapseGaming-SunBurn-Pro, Version=1.3.2.8, Culture=neutral, PublicKeyToken=c23c60523565dbfd
// MVID: A5F03349-72AC-4BAA-AEEE-9AB9B77E0A39
// Assembly location: C:\Projects\BlackBox\StarDrive\SynapseGaming-SunBurn-Pro.dll

using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace SynapseGaming.LightingSystem.Core
{
  /// <summary>
  /// Helper class that provides quick and easy triangle rendering.
  /// </summary>
  public class TriangleRenderHelper : BasePrimitiveRenderHelper
  {
    /// <summary>Number of primitives submitted to the render helper.</summary>
    public override int PrimitiveCount
    {
      get
      {
        return this.VertexCount / 3;
      }
    }

    /// <summary>Primitive type used by the render helper.</summary>
    protected override PrimitiveType PrimitiveType
    {
      get
      {
        return PrimitiveType.TriangleList;
      }
    }

    /// <summary>Creates a new TriangleRenderHelper instance.</summary>
    /// <param name="vertexcapacity">Maximum number of vertices the render helper can contain.</param>
    public TriangleRenderHelper(int vertexcapacity)
      : base(vertexcapacity)
    {
    }

    /// <summary>
    /// Submits a single triangle to the render helper.
    /// 
    /// Please note: all vertices and primitives contained in the render helper
    /// *must* be in the same space (ie: object space, world space, ...), as they
    /// are rendered all at once in the Render method using the same effect
    /// property values (including world, view, and projection transforms).
    /// </summary>
    /// <param name="a"></param>
    /// <param name="b"></param>
    /// <param name="c"></param>
    /// <param name="acolor"></param>
    /// <param name="bcolor"></param>
    /// <param name="ccolor"></param>
    public void Submit(Vector3 vector3_0, Vector3 vector3_1, Vector3 vector3_2, Color acolor, Color bcolor, Color ccolor)
    {
      this.SubmitVertex(vector3_0, acolor);
      this.SubmitVertex(vector3_1, bcolor);
      this.SubmitVertex(vector3_2, ccolor);
    }

    /// <summary>
    /// Submits a single triangle to the render helper.
    /// 
    /// Please note: all vertices and primitives contained in the render helper
    /// *must* be in the same space (ie: object space, world space, ...), as they
    /// are rendered all at once in the Render method using the same effect
    /// property values (including world, view, and projection transforms).
    /// </summary>
    /// <param name="a"></param>
    /// <param name="b"></param>
    /// <param name="c"></param>
    /// <param name="color"></param>
    public void Submit(Vector3 vector3_0, Vector3 vector3_1, Vector3 vector3_2, Color color)
    {
      this.Submit(vector3_0, vector3_1, vector3_2, color, color, color);
    }
  }
}
