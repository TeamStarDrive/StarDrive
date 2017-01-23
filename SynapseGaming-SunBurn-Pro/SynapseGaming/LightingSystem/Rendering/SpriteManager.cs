// Decompiled with JetBrains decompiler
// Type: SynapseGaming.LightingSystem.Rendering.SpriteManager
// Assembly: SynapseGaming-SunBurn-Pro, Version=1.3.2.8, Culture=neutral, PublicKeyToken=c23c60523565dbfd
// MVID: A5F03349-72AC-4BAA-AEEE-9AB9B77E0A39
// Assembly location: C:\Projects\BlackBox\StarDrive\SynapseGaming-SunBurn-Pro.dll

using Microsoft.Xna.Framework.Graphics;
using ns3;
using ns9;
using SynapseGaming.LightingSystem.Core;
using System;

namespace SynapseGaming.LightingSystem.Rendering
{
  /// <summary>
  /// Acts as a resource manager for arrays and buffers used during sprite creation.
  /// </summary>
  public class SpriteManager : IUnloadable, IManager, IManagerService
  {
    private int int_0 = 100;
    private Class21<RenderableMesh> class21_0 = new Class21<RenderableMesh>();
    private Class22<Class69> class22_0 = new Class22<Class69>();
    private IGraphicsDeviceService igraphicsDeviceService_0;

    /// <summary>
    /// Gets the manager specific Type used as a unique key for storing and
    /// requesting the manager from the IManagerServiceProvider.
    /// </summary>
    public Type ManagerType
    {
      get
      {
        return typeof (SpriteManager);
      }
    }

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
    public int ManagerProcessOrder
    {
      get
      {
        return this.int_0;
      }
      set
      {
        this.int_0 = value;
      }
    }

    /// <summary>Creates a new SpriteManager instance.</summary>
    /// <param name="graphicsdevicemanager"></param>
    public SpriteManager(IGraphicsDeviceService graphicsdevicemanager)
    {
      this.igraphicsDeviceService_0 = graphicsdevicemanager;
    }

    /// <summary>
    /// Creates a new SpriteContainer instance for storing and rendering 2D sprites.
    /// </summary>
    /// <returns></returns>
    public SpriteContainer CreateSpriteContainer()
    {
      SpriteContainer spriteContainer = new SpriteContainer(this.igraphicsDeviceService_0.GraphicsDevice, this.class21_0, this.class22_0);
      spriteContainer.ObjectType = ObjectType.Dynamic;
      return spriteContainer;
    }

    /// <summary>
    /// Use to apply user quality and performance preferences to the resources managed by this object.
    /// </summary>
    /// <param name="preferences"></param>
    public void ApplyPreferences(ILightingSystemPreferences preferences)
    {
    }

    /// <summary>
    /// Removes all objects from the container. Commonly used while clearing the scene.
    /// </summary>
    public void Clear()
    {
      this.class21_0.method_0();
      this.class22_0.method_0();
    }

    /// <summary>
    /// Disposes any graphics resource used internally by this object, and removes
    /// scene resources managed by this object. Commonly used during Game.UnloadContent.
    /// </summary>
    public void Unload()
    {
      this.class21_0.Clear();
      this.class22_0.method_1();
    }
  }
}
