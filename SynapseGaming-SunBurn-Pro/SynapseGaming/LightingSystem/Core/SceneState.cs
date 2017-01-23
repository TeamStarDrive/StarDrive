// Decompiled with JetBrains decompiler
// Type: SynapseGaming.LightingSystem.Core.SceneState
// Assembly: SynapseGaming-SunBurn-Pro, Version=1.3.2.8, Culture=neutral, PublicKeyToken=c23c60523565dbfd
// MVID: A5F03349-72AC-4BAA-AEEE-9AB9B77E0A39
// Assembly location: C:\Projects\BlackBox\StarDrive\SynapseGaming-SunBurn-Pro.dll

using Microsoft.Xna.Framework;
using SynapseGaming.LightingSystem.Rendering;

namespace SynapseGaming.LightingSystem.Core
{
  /// <summary>
  /// Provides scene, frame, and view specific data to the the lighting system.
  /// </summary>
  public class SceneState : ISceneState
  {
    private static ISceneEnvironment isceneEnvironment_1 = (ISceneEnvironment) new SceneEnvironment();
    private static ISceneEnvironment isceneEnvironment_2 = (ISceneEnvironment) new SceneEnvironment();
    private Matrix matrix_0 = Matrix.Identity;
    private Matrix matrix_1 = Matrix.Identity;
    private Matrix matrix_2 = Matrix.Identity;
    private Matrix matrix_3 = Matrix.Identity;
    private Matrix matrix_4 = Matrix.Identity;
    private Matrix matrix_5 = Matrix.Identity;
    private BoundingFrustum boundingFrustum_0 = new BoundingFrustum(Matrix.Identity);
    private bool bool_0 = true;
    private GameTime gameTime_0 = new GameTime();
    private ISceneEnvironment isceneEnvironment_0 = SceneState.isceneEnvironment_1;
    private bool bool_1;
    private bool bool_2;
    private int int_0;

    /// <summary>The scene's current view matrix.</summary>
    public Matrix View
    {
      get
      {
        return this.matrix_0;
      }
    }

    /// <summary>The scene's inverse view matrix.</summary>
    public Matrix ViewToWorld
    {
      get
      {
        return this.matrix_1;
      }
    }

    /// <summary>The scene's current projection matrix.</summary>
    public Matrix Projection
    {
      get
      {
        return this.matrix_2;
      }
    }

    /// <summary>The scene's inverse projection matrix.</summary>
    public Matrix ProjectionToView
    {
      get
      {
        return this.matrix_3;
      }
    }

    /// <summary>The scene's combined view and projection matrix.</summary>
    public Matrix ViewProjection
    {
      get
      {
        return this.matrix_4;
      }
    }

    /// <summary>
    /// The scene's combined inverse view and inverse projection matrix.
    /// </summary>
    public Matrix ProjectionToWorld
    {
      get
      {
        return this.matrix_5;
      }
    }

    /// <summary>The scene's current view frustum.</summary>
    public BoundingFrustum ViewFrustum
    {
      get
      {
        return this.boundingFrustum_0;
      }
    }

    /// <summary>
    /// Indicates the rendering pass is drawing to the screen (or to a
    /// target copied to the screen).
    /// </summary>
    public bool RenderingToScreen
    {
      get
      {
        return this.bool_0;
      }
    }

    /// <summary>
    /// Determines if primitive culling mode should be flipped to accommodate
    /// inverted windings caused by mirrored view or projection transforms.
    /// </summary>
    public bool InvertedWindings
    {
      get
      {
        return this.bool_1;
      }
    }

    /// <summary>Indicates the projection is 2D.</summary>
    public bool OrthographicProjection
    {
      get
      {
        return this.bool_2;
      }
    }

    /// <summary>The scene's current game time.</summary>
    public GameTime GameTime
    {
      get
      {
        return this.gameTime_0;
      }
    }

    /// <summary>The current frame id.</summary>
    public int FrameId
    {
      get
      {
        return this.int_0;
      }
    }

    /// <summary>The scene's current environment.</summary>
    public ISceneEnvironment Environment
    {
      get
      {
        return this.isceneEnvironment_0;
      }
    }

    /// <summary>Sets up the scene state prior to 2D rendering.</summary>
    /// <param name="viewposition">World space position of the 2D camera.</param>
    /// <param name="viewwidth">Number of world space units visible across the
    /// width of the viewport.</param>
    /// <param name="aspectratio">Aspect ratio of the viewport.</param>
    /// <param name="gametime">Current game time.</param>
    /// <param name="environment">Environment object used while rendering.</param>
    /// <param name="renderingtoscreen">Indicates the rendering pass is drawing
    /// to the screen (or to a target copied to the screen).</param>
    public void BeginFrameRendering(Vector2 viewposition, float viewwidth, float aspectratio, GameTime gametime, ISceneEnvironment environment, bool renderingtoscreen)
    {
      float nearPlaneDistance = viewwidth * 2.5f;
      float farPlaneDistance = nearPlaneDistance * 10f;
      Matrix lookAt = Matrix.CreateLookAt(new Vector3(viewposition, -nearPlaneDistance), new Vector3(viewposition, 0.0f), Vector3.Up);
      Matrix perspective = Matrix.CreatePerspective(viewwidth, viewwidth / aspectratio, nearPlaneDistance, farPlaneDistance);
      ISceneEnvironment environment1 = environment == null ? SceneState.isceneEnvironment_2 : environment;
      environment1.ShadowCasterDistance = farPlaneDistance;
      environment1.ShadowFadeEndDistance = farPlaneDistance;
      environment1.ShadowFadeStartDistance = farPlaneDistance;
      environment1.VisibleDistance = farPlaneDistance;
      this.BeginFrameRendering(lookAt, perspective, gametime, environment1, renderingtoscreen);
      this.bool_2 = true;
    }

    /// <summary>Sets up the scene state prior to 3D rendering.</summary>
    /// <param name="view">Camera view matrix.</param>
    /// <param name="projection">Camera projection matrix.</param>
    /// <param name="gametime">Current game time.</param>
    /// <param name="environment">Environment object used while rendering.</param>
    /// <param name="renderingtoscreen">Indicates the rendering pass is drawing
    /// to the screen (or to a target copied to the screen).</param>
    public void BeginFrameRendering(Matrix view, Matrix projection, GameTime gametime, ISceneEnvironment environment, bool renderingtoscreen)
    {
      //SplashScreen.CheckProductActivation();
      this.matrix_0 = view;
      this.matrix_1 = Matrix.Invert(view);
      this.matrix_2 = projection;
      this.matrix_3 = Matrix.Invert(projection);
      this.matrix_4 = view * projection;
      this.matrix_5 = Matrix.Invert(this.matrix_4);
      this.gameTime_0 = gametime;
      this.bool_0 = renderingtoscreen;
      this.bool_1 = (double) this.matrix_4.Determinant() >= 0.0;
      this.bool_2 = false;
      this.isceneEnvironment_0 = environment == null ? SceneState.isceneEnvironment_1 : environment;
      this.boundingFrustum_0.Matrix = view * projection;
      ++this.int_0;
    }

    /// <summary>Finalizes rendering.</summary>
    public void EndFrameRendering()
    {
    }

    /// <summary />
    public void ApplyEditorUpdate(Matrix view, Matrix viewtoworld, Matrix projection)
    {
      this.BeginFrameRendering(view, projection, this.gameTime_0, this.isceneEnvironment_0, this.bool_0);
    }
  }
}
