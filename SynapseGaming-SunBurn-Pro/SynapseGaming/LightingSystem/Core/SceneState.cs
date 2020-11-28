// Decompiled with JetBrains decompiler
// Type: SynapseGaming.LightingSystem.Core.SceneState
// Assembly: SynapseGaming-SunBurn-Pro, Version=1.3.2.8, Culture=neutral, PublicKeyToken=c23c60523565dbfd
// MVID: A5F03349-72AC-4BAA-AEEE-9AB9B77E0A39
// Assembly location: C:\Projects\BlackBox\StarDrive\SynapseGaming-SunBurn-Pro.dll

using Microsoft.Xna.Framework;

namespace SynapseGaming.LightingSystem.Core
{
    /// <summary>
    /// Provides scene, frame, and view specific data to the the lighting system.
    /// </summary>
    public class SceneState : ISceneState
    {
        private static ISceneEnvironment isceneEnvironment_1 = new SceneEnvironment();
        private static ISceneEnvironment isceneEnvironment_2 = new SceneEnvironment();
        private Matrix ViewProjectionMat = Matrix.Identity;

        /// <summary>The scene's current view matrix.</summary>
        public Matrix View { get; private set; } = Matrix.Identity;

        /// <summary>The scene's inverse view matrix.</summary>
        public Matrix ViewToWorld { get; private set; } = Matrix.Identity;

        /// <summary>The scene's current projection matrix.</summary>
        public Matrix Projection { get; private set; } = Matrix.Identity;

        /// <summary>The scene's inverse projection matrix.</summary>
        public Matrix ProjectionToView { get; private set; } = Matrix.Identity;

        /// <summary>The scene's combined view and projection matrix.</summary>
        public Matrix ViewProjection => this.ViewProjectionMat;

        /// <summary>
        /// The scene's combined inverse view and inverse projection matrix.
        /// </summary>
        public Matrix ProjectionToWorld { get; private set; } = Matrix.Identity;

        /// <summary>The scene's current view frustum.</summary>
        public BoundingFrustum ViewFrustum { get; } = new BoundingFrustum(Matrix.Identity);

        /// <summary>
        /// Indicates the rendering pass is drawing to the screen (or to a
        /// target copied to the screen).
        /// </summary>
        public bool RenderingToScreen { get; private set; } = true;

        /// <summary>
        /// Determines if primitive culling mode should be flipped to accommodate
        /// inverted windings caused by mirrored view or projection transforms.
        /// </summary>
        public bool InvertedWindings { get; private set; }

        /// <summary>Indicates the projection is 2D.</summary>
        public bool OrthographicProjection { get; private set; }

        /// <summary>Time elapsed since last frame.</summary>
        public float ElapsedTime { get; private set; }

        /// <summary>The current frame id.</summary>
        public int FrameId { get; private set; }

        /// <summary>The scene's current environment.</summary>
        public ISceneEnvironment Environment { get; private set; } = isceneEnvironment_1;

        /// <summary>Sets up the scene state prior to 2D rendering.</summary>
        /// <param name="viewposition">World space position of the 2D camera.</param>
        /// <param name="viewwidth">Number of world space units visible across the
        /// width of the viewport.</param>
        /// <param name="aspectratio">Aspect ratio of the viewport.</param>
        /// <param name="elapsedTime">Time since last frame.</param>
        /// <param name="environment">Environment object used while rendering.</param>
        /// <param name="renderingtoscreen">Indicates the rendering pass is drawing
        /// to the screen (or to a target copied to the screen).</param>
        public void BeginFrameRendering(Vector2 viewposition, float viewwidth, float aspectratio,
            float elapsedTime, ISceneEnvironment environment, bool renderingtoscreen)
        {
            float nearPlaneDistance = viewwidth * 2.5f;
            float farPlaneDistance = nearPlaneDistance * 10f;
            Matrix lookAt = Matrix.CreateLookAt(new Vector3(viewposition, -nearPlaneDistance), new Vector3(viewposition, 0.0f), Vector3.Up);
            Matrix perspective = Matrix.CreatePerspective(viewwidth, viewwidth / aspectratio, nearPlaneDistance, farPlaneDistance);
            ISceneEnvironment environment1 = environment == null ? isceneEnvironment_2 : environment;
            environment1.ShadowCasterDistance = farPlaneDistance;
            environment1.ShadowFadeEndDistance = farPlaneDistance;
            environment1.ShadowFadeStartDistance = farPlaneDistance;
            environment1.VisibleDistance = farPlaneDistance;
            this.BeginFrameRendering(ref lookAt, ref perspective, elapsedTime,
                                     environment1, renderingtoscreen);
            this.OrthographicProjection = true;
        }

        /// <summary>Sets up the scene state prior to 3D rendering.</summary>
        /// <param name="view">Camera view matrix.</param>
        /// <param name="projection">Camera projection matrix.</param>
        /// <param name="elapsedTime">Time since last frame.</param>
        /// <param name="environment">Environment object used while rendering.</param>
        /// <param name="renderingtoscreen">Indicates the rendering pass is drawing
        /// to the screen (or to a target copied to the screen).</param>
        public void BeginFrameRendering(ref Matrix view, ref Matrix projection,
            float elapsedTime, ISceneEnvironment environment, bool renderingtoscreen)
        {
            //SplashScreen.CheckProductActivation();
            this.View = view;
            this.ViewToWorld = Matrix.Invert(view);
            this.Projection = projection;
            this.ProjectionToView = Matrix.Invert(projection);
            this.ViewProjectionMat = view * projection;
            this.ProjectionToWorld = Matrix.Invert(this.ViewProjectionMat);
            this.ElapsedTime = elapsedTime;
            this.RenderingToScreen = renderingtoscreen;
            this.InvertedWindings = this.ViewProjectionMat.Determinant() >= 0.0;
            this.OrthographicProjection = false;
            this.Environment = environment == null ? isceneEnvironment_1 : environment;
            this.ViewFrustum.Matrix = view * projection;
            ++this.FrameId;
        }

        /// <summary>Finalizes rendering.</summary>
        public void EndFrameRendering()
        {
        }

        /// <summary />
        public void ApplyEditorUpdate(ref Matrix view, ref Matrix viewtoworld, ref Matrix projection)
        {
            this.BeginFrameRendering(ref view, ref projection, ElapsedTime, this.Environment, this.RenderingToScreen);
        }
    }
}
