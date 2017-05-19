// Decompiled with JetBrains decompiler
// Type: SynapseGaming.LightingSystem.Shadows.ShadowMapSurface
// Assembly: SynapseGaming-SunBurn-Pro, Version=1.3.2.8, Culture=neutral, PublicKeyToken=c23c60523565dbfd
// MVID: A5F03349-72AC-4BAA-AEEE-9AB9B77E0A39
// Assembly location: C:\Projects\BlackBox\StarDrive\SynapseGaming-SunBurn-Pro.dll

using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace SynapseGaming.LightingSystem.Shadows
{
  /// <summary>
  /// Class that represents one surface in a shadow map, which can be
  /// used for multi-part rendering and level-of-detail. The surface
  /// contains its own section within a render target.
  /// </summary>
  public class ShadowMapSurface
  {
      private Matrix matrix_0 = Matrix.Identity;
    private Matrix matrix_1 = Matrix.Identity;
    private bool bool_1 = true;
    private BoundingFrustum boundingFrustum_0 = new BoundingFrustum(Matrix.Identity);
    private Viewport viewport_0;
      private Rectangle rectangle_0;

    /// <summary>
    /// View transform used to project the scene into the
    /// surface and the surface onto the scene.
    /// </summary>
    public Matrix WorldToSurfaceView
    {
      get => this.matrix_0;
        set
      {
        this.matrix_0 = value;
        this.bool_1 = true;
      }
    }

    /// <summary>
    /// Projection transform used to project the scene into
    /// the surface and the surface onto the scene.
    /// </summary>
    public Matrix Projection
    {
      get => this.matrix_1;
        set
      {
        this.matrix_1 = value;
        this.bool_1 = true;
      }
    }

    /// <summary>The surface projection frustum.</summary>
    public BoundingFrustum Frustum
    {
      get
      {
        if (this.bool_1)
        {
          this.boundingFrustum_0.Matrix = this.matrix_0 * this.matrix_1;
          this.bool_1 = false;
        }
        return this.boundingFrustum_0;
      }
    }

    /// <summary>
    /// Viewport used when rendering to the surface render target location.
    /// </summary>
    public Viewport Viewport => this.viewport_0;

      /// <summary>Level-of-detail applied to the surface.</summary>
    public float LevelOfDetail { get; set; } = 1f;

      /// <summary>The surface location in the render target.</summary>
    public Rectangle RenderTargetLocation
    {
      get => this.rectangle_0;
          set
      {
        this.rectangle_0 = value;
        this.viewport_0.X = this.rectangle_0.X;
        this.viewport_0.Y = this.rectangle_0.Y;
        this.viewport_0.Width = this.rectangle_0.Width;
        this.viewport_0.Height = this.rectangle_0.Height;
        this.viewport_0.MinDepth = 0.0f;
        this.viewport_0.MaxDepth = 1f;
      }
    }

    internal bool Enabled { get; set; } = true;

      internal Rectangle method_0(int int_0)
    {
      return new Rectangle(this.rectangle_0.X + int_0, this.rectangle_0.Y + int_0, this.rectangle_0.Width - int_0 * 2, this.rectangle_0.Height - int_0 * 2);
    }

    internal void method_1(Vector3 vector3_0)
    {
      this.matrix_0.Translation = vector3_0;
    }
  }
}
