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
        private Matrix worldToSurfaceView = Matrix.Identity;
        private Matrix projection = Matrix.Identity;
        private bool bool_1 = true;
        private BoundingFrustum frustum = new BoundingFrustum(Matrix.Identity);
        private Viewport viewport;
        private Rectangle rectangle_0;

        /// <summary>
        /// View transform used to project the scene into the
        /// surface and the surface onto the scene.
        /// </summary>
        public Matrix WorldToSurfaceView
        {
            get => worldToSurfaceView;
            set
            {
                worldToSurfaceView = value;
                bool_1 = true;
            }
        }

        /// <summary>
        /// Projection transform used to project the scene into
        /// the surface and the surface onto the scene.
        /// </summary>
        public Matrix Projection
        {
            get => projection;
            set
            {
                projection = value;
                bool_1 = true;
            }
        }

        /// <summary>The surface projection frustum.</summary>
        public BoundingFrustum Frustum
        {
            get
            {
                if (bool_1)
                {
                    frustum.Matrix = worldToSurfaceView * projection;
                    bool_1 = false;
                }
                return frustum;
            }
        }

        /// <summary>
        /// Viewport used when rendering to the surface render target location.
        /// </summary>
        public Viewport Viewport => viewport;

        /// <summary>Level-of-detail applied to the surface.</summary>
        public float LevelOfDetail { get; set; } = 1f;

        /// <summary>The surface location in the render target.</summary>
        public Rectangle RenderTargetLocation
        {
            get => rectangle_0;
            set
            {
                rectangle_0 = value;
                viewport.X = rectangle_0.X;
                viewport.Y = rectangle_0.Y;
                viewport.Width = rectangle_0.Width;
                viewport.Height = rectangle_0.Height;
                viewport.MinDepth = 0.0f;
                viewport.MaxDepth = 1f;
            }
        }

        internal bool Enabled { get; set; } = true;

        internal Rectangle method_0(int int_0)
        {
            return new Rectangle(rectangle_0.X + int_0, rectangle_0.Y + int_0, rectangle_0.Width - int_0 * 2, rectangle_0.Height - int_0 * 2);
        }

        internal void method_1(Vector3 vector3_0)
        {
            worldToSurfaceView.Translation = vector3_0;
        }
    }
}
