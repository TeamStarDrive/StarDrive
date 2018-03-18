// Decompiled with JetBrains decompiler
// Type: SynapseGaming.LightingSystem.Core.LineRenderHelper
// Assembly: SynapseGaming-SunBurn-Pro, Version=1.3.2.8, Culture=neutral, PublicKeyToken=c23c60523565dbfd
// MVID: A5F03349-72AC-4BAA-AEEE-9AB9B77E0A39
// Assembly location: C:\Projects\BlackBox\StarDrive\SynapseGaming-SunBurn-Pro.dll

using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace SynapseGaming.LightingSystem.Core
{
  /// <summary>
  /// Helper class that provides quick and easy line rendering.
  /// </summary>
  public class LineRenderHelper : BasePrimitiveRenderHelper
  {
    /// <summary>Number of primitives submitted to the render helper.</summary>
    public override int PrimitiveCount => this.VertexCount / 2;

      /// <summary>Primitive type used by the render helper.</summary>
    protected override PrimitiveType PrimitiveType => PrimitiveType.LineList;

      /// <summary>Creates a new LineRenderHelper instance.</summary>
    /// <param name="vertexcapacity">Maximum number of vertices the render helper can contain.</param>
    public LineRenderHelper(int vertexcapacity)
      : base(vertexcapacity)
    {
    }

    /// <summary>
    /// Submits a single line to the render helper.
    /// 
    /// Please note: all vertices and primitives contained in the render helper
    /// *must* be in the same space (ie: object space, world space, ...), as they
    /// are rendered all at once in the Render method using the same effect
    /// property values (including world, view, and projection transforms).
    /// </summary>
    /// <param name="start"></param>
    /// <param name="end"></param>
    /// <param name="startcolor"></param>
    /// <param name="endcolor"></param>
    public void Submit(Vector3 start, Vector3 end, Color startcolor, Color endcolor)
    {
      this.SubmitVertex(start, startcolor);
      this.SubmitVertex(end, endcolor);
    }

    /// <summary>
    /// Submits a single line to the render helper.
    /// 
    /// Please note: all vertices and primitives contained in the render helper
    /// *must* be in the same space (ie: object space, world space, ...), as they
    /// are rendered all at once in the Render method using the same effect
    /// property values (including world, view, and projection transforms).
    /// </summary>
    /// <param name="start"></param>
    /// <param name="end"></param>
    /// <param name="color"></param>
    public void Submit(Vector3 start, Vector3 end, Color color)
    {
      this.Submit(start, end, color, color);
    }
  }
}
