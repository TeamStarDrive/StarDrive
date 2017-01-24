// Decompiled with JetBrains decompiler
// Type: SynapseGaming.LightingSystem.Core.FullFrameQuad
// Assembly: SynapseGaming-SunBurn-Pro, Version=1.3.2.8, Culture=neutral, PublicKeyToken=c23c60523565dbfd
// MVID: A5F03349-72AC-4BAA-AEEE-9AB9B77E0A39
// Assembly location: C:\Projects\BlackBox\StarDrive\SynapseGaming-SunBurn-Pro.dll

using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;

namespace SynapseGaming.LightingSystem.Core
{
  /// <summary>
  /// Helper class that renders a full viewport quad using the user
  /// effect provided to the Render method.
  /// </summary>
  public class FullFrameQuad : IDisposable
  {
    private VertexPositionTexture[] vertexPositionTexture_0 = new VertexPositionTexture[4];
    private int int_0;
    private int int_1;
    private GraphicsDevice graphicsDevice_0;
    private VertexBuffer vertexBuffer_0;
    private VertexDeclaration vertexDeclaration_0;

    /// <summary>The quad's VertexBuffer (used in custom rendering).</summary>
    public VertexBuffer VertexBuffer
    {
      get
      {
        return this.vertexBuffer_0;
      }
    }

    /// <summary>
    /// The quad's VertexDeclaration (used in custom rendering).
    /// </summary>
    public VertexDeclaration VertexDeclaration
    {
      get
      {
        return this.vertexDeclaration_0;
      }
    }

    /// <summary>The quad's VertexSize (used in custom rendering).</summary>
    public int VertexSize
    {
      get
      {
        return VertexPositionTexture.SizeInBytes;
      }
    }

    /// <summary>Creates a new FullFrameQuad instance.</summary>
    /// <param name="device"></param>
    /// <param name="width">Target wiewport width.</param>
    /// <param name="height">Target wiewport height.</param>
    public FullFrameQuad(GraphicsDevice device, int width, int height)
      : this(device, width, height, -Vector2.One, Vector2.One)
    {
    }

    /// <summary>
    /// Creates a new FullFrameQuad instance with min/max screen space
    /// rendering bounds for partial screen coverage.
    /// </summary>
    /// <param name="device"></param>
    /// <param name="width">Target wiewport width.</param>
    /// <param name="height">Target wiewport height.</param>
    /// <param name="screenmin">Screen space min render area.</param>
    /// <param name="screenmax">Screen space max render area.</param>
    public FullFrameQuad(GraphicsDevice device, int width, int height, Vector2 screenmin, Vector2 screenmax)
    {
      this.graphicsDevice_0 = device;
      this.int_0 = width;
      this.int_1 = height;
      Vector3 vector3 = new Vector3(-0.5f / (float) width, 0.5f / (float) height, 0.0f);
      this.vertexPositionTexture_0[0].Position = new Vector3(screenmin.X, screenmax.Y, 0.0f) + vector3;
      this.vertexPositionTexture_0[0].TextureCoordinate = new Vector2(0.0f, 0.0f);
      this.vertexPositionTexture_0[1].Position = new Vector3(screenmax.X, screenmax.Y, 0.0f) + vector3;
      this.vertexPositionTexture_0[1].TextureCoordinate = new Vector2(1f, 0.0f);
      this.vertexPositionTexture_0[2].Position = new Vector3(screenmin.X, screenmin.Y, 0.0f) + vector3;
      this.vertexPositionTexture_0[2].TextureCoordinate = new Vector2(0.0f, 1f);
      this.vertexPositionTexture_0[3].Position = new Vector3(screenmax.X, screenmin.Y, 0.0f) + vector3;
      this.vertexPositionTexture_0[3].TextureCoordinate = new Vector2(1f, 1f);
      this.vertexBuffer_0 = new VertexBuffer(device, VertexPositionTexture.SizeInBytes * 4, BufferUsage.WriteOnly);
      this.vertexBuffer_0.SetData<VertexPositionTexture>(this.vertexPositionTexture_0);
      this.vertexDeclaration_0 = new VertexDeclaration(device, VertexPositionTexture.VertexElements);
    }

    /// <summary>Renders the quad using the supplied effect.</summary>
    /// <param name="effect"></param>
    public void Render(Effect effect)
    {
      this.graphicsDevice_0.VertexDeclaration = this.vertexDeclaration_0;
      this.graphicsDevice_0.Vertices[0].SetSource(this.vertexBuffer_0, 0, this.VertexSize);
      effect.Begin();
      for (int index = 0; index < effect.CurrentTechnique.Passes.Count; ++index)
      {
        EffectPass pass = effect.CurrentTechnique.Passes[index];
        pass.Begin();
        this.graphicsDevice_0.DrawPrimitives(PrimitiveType.TriangleStrip, 0, 2);
        pass.End();
      }
      effect.End();
    }

    /// <summary>Renders the quad.</summary>
    public void Render()
    {
      this.graphicsDevice_0.VertexDeclaration = this.vertexDeclaration_0;
      this.graphicsDevice_0.Vertices[0].SetSource(this.vertexBuffer_0, 0, this.VertexSize);
      this.graphicsDevice_0.DrawPrimitives(PrimitiveType.TriangleStrip, 0, 2);
    }

    /// <summary>Disposes all related graphics objects.</summary>
    public void Dispose()
    {
      this.graphicsDevice_0 = (GraphicsDevice) null;
      if (this.vertexBuffer_0 != null)
      {
        this.vertexBuffer_0.Dispose();
        this.vertexBuffer_0 = (VertexBuffer) null;
      }
      if (this.vertexDeclaration_0 == null)
        return;
      this.vertexDeclaration_0.Dispose();
      this.vertexDeclaration_0 = (VertexDeclaration) null;
    }
  }
}
