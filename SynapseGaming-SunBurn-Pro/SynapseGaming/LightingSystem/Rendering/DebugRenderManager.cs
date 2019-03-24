// Decompiled with JetBrains decompiler
// Type: SynapseGaming.LightingSystem.Rendering.DebugRenderManager
// Assembly: SynapseGaming-SunBurn-Pro, Version=1.3.2.8, Culture=neutral, PublicKeyToken=c23c60523565dbfd
// MVID: A5F03349-72AC-4BAA-AEEE-9AB9B77E0A39
// Assembly location: C:\Projects\BlackBox\StarDrive\SynapseGaming-SunBurn-Pro.dll

using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using ns11;
using SynapseGaming.LightingSystem.Core;
using SynapseGaming.LightingSystem.Lights;

namespace SynapseGaming.LightingSystem.Rendering
{
  /// <summary>
  /// Helper renderer that displays the bounding boxes of all
  /// rendered scene objects and lights.
  /// 
  /// Can help tune performance and work out bugs by seeing how
  /// objects and lights within the scene overlap and interact
  /// with each other.
  /// </summary>
  public class DebugRenderManager : IUnloadable, IManager, IRenderableManager, IManagerService
  {
      private LineRenderHelper lineRenderHelper_0 = new LineRenderHelper(24);
    private Vector3[] vector3_0 = new Vector3[8];
    private List<ISceneObject> list_0 = new List<ISceneObject>();
    private List<ILight> list_1 = new List<ILight>();
    private ISceneState isceneState_0;
      private IManagerServiceProvider imanagerServiceProvider_0;
    private BasicEffect basicEffect_0;

    /// <summary>
    /// Gets the manager specific Type used as a unique key for storing and
    /// requesting the manager from the IManagerServiceProvider.
    /// </summary>
    public Type ManagerType => typeof (DebugRenderManager);

      /// <summary>
    /// Sets the order this manager is processed relative to other managers
    /// in the IManagerServiceProvider. Managers with lower processing order
    /// values are processed first.
    /// 
    /// In the case of BeginFrameRendering and EndFrameRendering, BeginFrameRendering
    /// is processed in the normal order (lowest order value to highest), however
    /// EndFrameRendering is processed in reverse order (highest to lowest) to ensure
    /// the first manager begun is the last one ended (FILO).
    /// </summary>
    public int ManagerProcessOrder { get; set; } = 100;

      /// <summary>
    /// The current GraphicsDeviceManager used by this object.
    /// </summary>
    public IGraphicsDeviceService GraphicsDeviceManager { get; }

      /// <summary>Creates a new DebugRenderManager instance.</summary>
    /// <param name="graphicsdevicemanager"></param>
    /// <param name="sceneinterface">Service provider used to access all other manager services in this scene.</param>
    public DebugRenderManager(IGraphicsDeviceService graphicsdevicemanager, IManagerServiceProvider sceneinterface)
    {
      this.GraphicsDeviceManager = graphicsdevicemanager;
      this.imanagerServiceProvider_0 = sceneinterface;
    }

    /// <summary>
    /// Use to apply user quality and performance preferences to the resources managed by this object.
    /// </summary>
    /// <param name="preferences"></param>
    public void ApplyPreferences(ILightingSystemPreferences preferences)
    {
    }

    /// <summary>Sets up the object prior to rendering.</summary>
    /// <param name="state"></param>
    public void BeginFrameRendering(ISceneState state)
    {
      this.isceneState_0 = state;
    }

    private void method_0(BoundingBox boundingBox_0, Color color_0)
    {
      boundingBox_0.GetCorners(this.vector3_0);
      this.lineRenderHelper_0.Clear();
      for (int index = 0; index < 3; ++index)
      {
        this.lineRenderHelper_0.Submit(this.vector3_0[index], this.vector3_0[index + 1], color_0);
        this.lineRenderHelper_0.Submit(this.vector3_0[index + 4], this.vector3_0[index + 5], color_0);
        this.lineRenderHelper_0.Submit(this.vector3_0[index], this.vector3_0[index + 4], color_0);
      }
      this.lineRenderHelper_0.Submit(this.vector3_0[0], this.vector3_0[3], color_0);
      this.lineRenderHelper_0.Submit(this.vector3_0[4], this.vector3_0[7], color_0);
      this.lineRenderHelper_0.Submit(this.vector3_0[3], this.vector3_0[7], color_0);
      this.lineRenderHelper_0.Render(this.GraphicsDeviceManager.GraphicsDevice, this.basicEffect_0);
    }

    /// <summary>Finalizes rendering.</summary>
    public void EndFrameRendering()
    {
      GraphicsDevice graphicsDevice = this.GraphicsDeviceManager.GraphicsDevice;
      IObjectManager manager1 = (IObjectManager) this.imanagerServiceProvider_0.GetManager(SceneInterface.ObjectManagerType, false);
      ILightManager manager2 = (ILightManager) this.imanagerServiceProvider_0.GetManager(SceneInterface.LightManagerType, false);
      if (this.basicEffect_0 == null)
      {
        this.basicEffect_0 = new BasicEffect(graphicsDevice, null);
        this.basicEffect_0.TextureEnabled = false;
        this.basicEffect_0.SpecularPower = 0.0f;
        this.basicEffect_0.VertexColorEnabled = true;
      }
      this.basicEffect_0.World = Matrix.Identity;
      this.basicEffect_0.View = this.isceneState_0.View;
      this.basicEffect_0.Projection = this.isceneState_0.Projection;
      if (manager1 != null)
      {
        this.list_0.Clear();
        manager1.Find(this.list_0, this.isceneState_0.ViewFrustum, ObjectFilter.All);
        foreach (ISceneObject sceneObject in this.list_0)
        {
          if (sceneObject != null)
            this.method_0(sceneObject.WorldBoundingBox, Color.LimeGreen);
        }
      }
      if (manager2 == null)
        return;
      this.list_1.Clear();
      manager2.Find(this.list_1, this.isceneState_0.ViewFrustum, ObjectFilter.EnabledDynamicAndStatic);
      foreach (ILight light in this.list_1)
      {
        if (light is IPointSource)
          this.method_0(light.WorldBoundingBox, Color.Yellow);
      }
    }

    /// <summary>
    /// Removes resources managed by this object. Commonly used while clearing the scene.
    /// </summary>
    public void Clear()
    {
    }

    /// <summary>
    /// Disposes any graphics resource used internally by this object, and removes
    /// scene resources managed by this object. Commonly used during Game.UnloadContent.
    /// </summary>
    public void Unload()
    {
      this.Clear();
      Disposable.Free(ref this.basicEffect_0);
    }
  }
}
