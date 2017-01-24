// Decompiled with JetBrains decompiler
// Type: SynapseGaming.LightingSystem.Core.BasePrimitiveRenderHelper
// Assembly: SynapseGaming-SunBurn-Pro, Version=1.3.2.8, Culture=neutral, PublicKeyToken=c23c60523565dbfd
// MVID: A5F03349-72AC-4BAA-AEEE-9AB9B77E0A39
// Assembly location: C:\Projects\BlackBox\StarDrive\SynapseGaming-SunBurn-Pro.dll

using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace SynapseGaming.LightingSystem.Core
{
  /// <summary>
  /// Base class used in primitive rendering helper classes.
  /// </summary>
  public abstract class BasePrimitiveRenderHelper
  {
    private int int_0;
    private VertexPositionColor[] vertexPositionColor_0;

    /// <summary>Number of vertices submitted to the render helper.</summary>
    public int VertexCount
    {
      get
      {
        return this.int_0;
      }
    }

    /// <summary>
    /// Maximum number of vertices the render helper can contain.
    /// </summary>
    public int VertexCapacity
    {
      get
      {
        return this.vertexPositionColor_0.Length;
      }
    }

    /// <summary>
    /// Number of primitives submitted to the render helper. This is implemented by
    /// descendant classes and derived from the VertexCount and number of vertices
    /// per-primitive.
    /// </summary>
    public abstract int PrimitiveCount { get; }

    /// <summary>
    /// Primitive type. This is implemented by descendant classes.
    /// </summary>
    protected abstract PrimitiveType PrimitiveType { get; }

    /// <summary>Creates a new BasePrimitiveRenderHelper instance.</summary>
    /// <param name="vertexcapacity">Maximum number of vertices the render helper can contain.</param>
    public BasePrimitiveRenderHelper(int vertexcapacity)
    {
      this.vertexPositionColor_0 = new VertexPositionColor[vertexcapacity];
    }

    /// <summary>
    /// Submits a single vertex to the render helper.
    /// 
    /// Please note: all vertices and primitives contained in the render helper
    /// *must* be in the same space (ie: object space, world space, ...), as they
    /// are rendered all at once in the Render method using the same effect
    /// property values (including world, view, and projection transforms).
    /// </summary>
    /// <param name="position"></param>
    /// <param name="color"></param>
    protected void SubmitVertex(Vector3 position, Color color)
    {
      this.vertexPositionColor_0[this.int_0].Position = position;
      this.vertexPositionColor_0[this.int_0++].Color = color;
    }

    /// <summary>Clears all submitted vertices from the render helper.</summary>
    public virtual void Clear()
    {
      this.int_0 = 0;
    }

    /// <summary>
    /// Renders all contained vertices at once using the currently active effect.
    /// 
    /// Please note: this method expects an effect to be active (between the
    /// EffectPass.Begin and EffectPass.End calls) and effect property values
    /// including transforms to be set correctly.  If using BasicEffect remember
    /// to enable VertexColorEnabled for vertex colors to be visible.
    /// </summary>
    /// <param name="device"></param>
    public void Render(GraphicsDevice device)
    {
      if (this.int_0 < 2)
        return;
      device.VertexDeclaration = LightingSystemManager.Instance.method_10(device);
      device.DrawUserPrimitives<VertexPositionColor>(this.PrimitiveType, this.vertexPositionColor_0, 0, this.PrimitiveCount);
    }

    /// <summary>
    /// Renders all contained vertices at once using the supplied effect.
    /// 
    /// Please note: this method expects all effect property values
    /// including transforms to be set correctly.  If using BasicEffect remember
    /// to enable VertexColorEnabled for vertex colors to be visible.
    /// </summary>
    /// <param name="device"></param>
    /// <param name="effect"></param>
    public void Render(GraphicsDevice device, Effect effect)
    {
      if (this.int_0 < 2)
        return;
      effect.Begin();
      for (int index = 0; index < effect.CurrentTechnique.Passes.Count; ++index)
      {
        EffectPass pass = effect.CurrentTechnique.Passes[0];
        pass.Begin();
        this.Render(device);
        pass.End();
      }
      effect.End();
    }
  }
}
