// Decompiled with JetBrains decompiler
// Type: SynapseGaming.LightingSystem.Core.SceneInterface
// Assembly: SynapseGaming-SunBurn-Pro, Version=1.3.2.8, Culture=neutral, PublicKeyToken=c23c60523565dbfd
// MVID: A5F03349-72AC-4BAA-AEEE-9AB9B77E0A39
// Assembly location: C:\Projects\BlackBox\StarDrive\SynapseGaming-SunBurn-Pro.dll

using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using SynapseGaming.LightingSystem.Editor;
using SynapseGaming.LightingSystem.Lights;
using SynapseGaming.LightingSystem.Rendering;
using SynapseGaming.LightingSystem.Rendering.Deferred;
using SynapseGaming.LightingSystem.Rendering.Forward;
using SynapseGaming.LightingSystem.Shadows;
using SynapseGaming.LightingSystem.Shadows.Deferred;
using SynapseGaming.LightingSystem.Shadows.Forward;

namespace SynapseGaming.LightingSystem.Core
{
  /// <summary>
  /// Acts as both a service provider and component manager for a scene.
  /// 
  /// As a service provider its contained manager services can be requested by type and
  /// accessed through common interfaces, making both using the built-in managers and
  /// writing custom replacement managers easy.
  /// 
  /// As a component manager all contained manager services automatically receive calls
  /// to BeginFrameRendering, EndFrameRendering, Update, and more, allowing custom managers
  /// to be plugged in and run with out writing any additional code to specifically handle them.
  /// </summary>
  public class SceneInterface : IRenderableManager, IUpdatableManager, IManagerServiceProvider
  {
    private static ManagerServiceComparer ManagerServiceComparer0 = new ManagerServiceComparer();
    private static RenderableComparer RenderableComparer0 = new RenderableComparer();
    private static UpdatableComparer UpdatableComparer0 = new UpdatableComparer();
    private Dictionary<Type, IManagerService> dictionary_0 = new Dictionary<Type, IManagerService>(16);
    private List<IManagerService> Services = new List<IManagerService>(16);
    private List<IUpdatableManager> Updateables = new List<IUpdatableManager>(16);
    private List<IRenderableManager> Renderables = new List<IRenderableManager>(16);

      /// <summary>
    /// The current GraphicsDeviceManager used by this object.
    /// </summary>
    public IGraphicsDeviceService GraphicsDeviceManager { get; }

      /// <summary>
    /// Provides convenient access to the ResourceManager manager service contained in the provider.
    /// 
    /// Note: this property will be null if no manager service of this type is contained in the provider.
    /// </summary>
    public IResourceManager ResourceManager { get; private set; }

    /// <summary>
    /// Provides convenient access to the ObjectManager manager service contained in the provider.
    /// 
    /// Note: this property will be null if no manager service of this type is contained in the provider.
    /// </summary>
    public IObjectManager ObjectManager { get; private set; }

    /// <summary>
    /// Provides convenient access to the RenderManager manager service contained in the provider.
    /// 
    /// Note: this property will be null if no manager service of this type is contained in the provider.
    /// </summary>
    public IRenderManager RenderManager { get; private set; }

    /// <summary>
    /// Provides convenient access to the LightManager manager service contained in the provider.
    /// 
    /// Note: this property will be null if no manager service of this type is contained in the provider.
    /// </summary>
    public ILightManager LightManager { get; private set; }

    /// <summary>
    /// Provides convenient access to the ShadowMapManager manager service contained in the provider.
    /// 
    /// Note: this property will be null if no manager service of this type is contained in the provider.
    /// </summary>
    public IShadowMapManager ShadowMapManager { get; private set; }

    /// <summary>
    /// Provides convenient access to the PostProcessManager manager service contained in the provider.
    /// 
    /// Note: this property will be null if no manager service of this type is contained in the provider.
    /// </summary>
    public IPostProcessManager PostProcessManager { get; private set; }

    /// <summary>
    /// Provides convenient access to the AvatarManager manager service contained in the provider.
    /// 
    /// Note: this property will be null if no manager service of this type is contained in the provider.
    /// </summary>
    public IAvatarManager AvatarManager { get; private set; }

    /// <summary>
    /// Manager type used to retrieve the IResourceManager manager service.  Use this
    /// type when creating a custom manager that replaces the built-in manager.
    /// </summary>
    public static Type ResourceManagerType => typeof (IResourceManager);

      /// <summary>
    /// Manager type used to retrieve the IObjectManager manager service.  Use this
    /// type when creating a custom manager that replaces the built-in manager.
    /// </summary>
    public static Type ObjectManagerType => typeof (IObjectManager);

      /// <summary>
    /// Manager type used to retrieve the IRenderManager manager service.  Use this
    /// type when creating a custom manager that replaces the built-in manager.
    /// </summary>
    public static Type RenderManagerType => typeof (IRenderManager);

      /// <summary>
    /// Manager type used to retrieve the ILightManager manager service.  Use this
    /// type when creating a custom manager that replaces the built-in manager.
    /// </summary>
    public static Type LightManagerType => typeof (ILightManager);

      /// <summary>
    /// Manager type used to retrieve the IShadowMapManager manager service.  Use this
    /// type when creating a custom manager that replaces the built-in manager.
    /// </summary>
    public static Type ShadowMapManagerType => typeof (IShadowMapManager);

      /// <summary>
    /// Manager type used to retrieve the IPostProcessManager manager service.  Use this
    /// type when creating a custom manager that replaces the built-in manager.
    /// </summary>
    public static Type PostProcessManagerType => typeof (IPostProcessManager);

      /// <summary>
    /// Manager type used to retrieve the LightingSystemEditor manager service.
    /// </summary>
    public static Type EditorType => typeof (LightingSystemEditor);

      /// <summary>
    /// Manager type used to retrieve the IAvatarManager manager service.  Use this
    /// type when creating a custom manager that replaces the built-in manager.
    /// </summary>
    public static Type AvatarManagerType => typeof (IAvatarManager);

      /// <summary>Creates a new SceneInterface instance.</summary>
    /// <param name="graphicsdevicemanager"></param>
    public SceneInterface(IGraphicsDeviceService graphicsdevicemanager)
    {
      this.GraphicsDeviceManager = graphicsdevicemanager;
    }

    /// <summary>
    /// Creates and adds a default set of manager services. This makes
    /// initializing the SceneInterface easier.
    /// 
    /// Depending on the creation options provided the following manager
    /// services will be created:
    /// 
    /// Always
    ///     -ResourceManager
    ///     -ObjectManager
    ///     -LightManager
    /// 
    /// Forward rendering
    ///     -RenderManager
    ///     -LightManager
    /// 
    /// Deferred rednering
    ///     -DeferredRenderManager
    ///     -DeferredShadowMapManager
    /// 
    /// Avatars
    ///     -AvatarManager
    /// 
    /// Post processing
    ///     -PostProcessManager
    /// </summary>
    /// <param name="usedeferredrendering"></param>
    /// <param name="useavatars"></param>
    /// <param name="usepostprocessing"></param>
    public void CreateDefaultManagers(bool usedeferredrendering, bool useavatars, bool usepostprocessing)
    {
      this.Unload();
      this.AddManager(new ResourceManager());
      this.AddManager(new ObjectManager(this.GraphicsDeviceManager));
      this.AddManager(new LightManager(this.GraphicsDeviceManager));
      if (usedeferredrendering)
      {
        this.AddManager(new DeferredRenderManager(this.GraphicsDeviceManager, this));
        this.AddManager(new DeferredShadowMapManager(this.GraphicsDeviceManager));
      }
      else
      {
        this.AddManager(new RenderManager(this.GraphicsDeviceManager, this));
        this.AddManager(new ShadowMapManager(this.GraphicsDeviceManager));
      }
      if (useavatars)
        this.AddManager(new AvatarManager(this.GraphicsDeviceManager, this));
      if (!usepostprocessing)
        return;
      this.AddManager(new PostProcessManager(this.GraphicsDeviceManager));
    }

    /// <summary>Adds a manager service to the provider.</summary>
    /// <param name="manager"></param>
    public virtual void AddManager(IManagerService manager)
    {
      this.RemoveManager(manager);
      if (this.dictionary_0.ContainsKey(manager.ManagerType))
        return;
      this.dictionary_0.Add(manager.ManagerType, manager);
      this.ResortServices();
      this.method_0();
    }

    /// <summary>Removes a manager service from the provider.</summary>
    /// <param name="manager"></param>
    public virtual void RemoveManager(IManagerService manager)
    {
      if (!this.dictionary_0.ContainsKey(manager.ManagerType))
        return;
      this.dictionary_0.Remove(manager.ManagerType);
      this.ResortServices();
      this.method_0();
    }

    private void method_0()
    {
      this.ResourceManager = (IResourceManager) this.GetManager(ResourceManagerType, false);
      this.ObjectManager = (IObjectManager) this.GetManager(ObjectManagerType, false);
      this.RenderManager = (IRenderManager) this.GetManager(RenderManagerType, false);
      this.LightManager = (ILightManager) this.GetManager(LightManagerType, false);
      this.ShadowMapManager = (IShadowMapManager) this.GetManager(ShadowMapManagerType, false);
      this.PostProcessManager = (IPostProcessManager) this.GetManager(PostProcessManagerType, false);
      this.AvatarManager = (IAvatarManager) this.GetManager(AvatarManagerType, false);
    }

    /// <summary>
    /// Resorts the contained manager services.
    /// 
    /// Providers should automatically resort when manager services
    /// are added and removed, however manual resorting is necessary
    /// if a manager service's ManagerProcessOrder property changes
    /// after being added to the provider.
    /// </summary>
    public virtual void ResortServices()
    {
      this.Services.Clear();
      this.Renderables.Clear();
      this.Updateables.Clear();
      foreach (KeyValuePair<Type, IManagerService> keyValuePair in this.dictionary_0)
      {
        IManagerService managerService = keyValuePair.Value;
        this.Services.Add(managerService);
        if (managerService is IRenderableManager)
          this.Renderables.Add(managerService as IRenderableManager);
        if (managerService is IUpdatableManager)
          this.Updateables.Add(managerService as IUpdatableManager);
      }
      this.Services.Sort(ManagerServiceComparer0);
      this.Renderables.Sort(RenderableComparer0);
      this.Updateables.Sort(UpdatableComparer0);
    }

    /// <summary>
    /// Retrieves a manager service by type from the provider.
    /// </summary>
    /// <typeparam name="T">Type used by the manager as a unique
    /// identifying key (IManagerService.ManagerType).</typeparam>
    /// <param name="required">Determines whether an exception should
    /// be thrown if the manager is not found.</param>
    /// <returns></returns>
    public virtual T GetManager<T>(bool required) where T : class
    {
      Type managertype = typeof (T);
      T manager = (T) this.GetManager(managertype, required);
      if (manager == null && required)
        throw new Exception("Service manager does not contain a service assigned to the '" + managertype.Name + "' type.");
      return manager;
    }

    /// <summary>
    /// Retrieves a manager service by type from the provider.
    /// </summary>
    /// <param name="managertype">Type used by the manager as a unique
    /// identifying key (IManagerService.ManagerType).</param>
    /// <param name="required">Determines whether an exception should
    /// be thrown if the manager is not found.</param>
    /// <returns></returns>
    public IManagerService GetManager(Type managertype, bool required)
    {
      IManagerService managerService;
      this.dictionary_0.TryGetValue(managertype, out managerService);
      if (managerService == null && required)
        throw new Exception("Service manager does not contain a service assigned to the '" + managertype.Name + "' type.");
      return managerService;
    }

    /// <summary>Retrieves all manager services from the provider.</summary>
    /// <param name="managers">List used to store manager services.</param>
    public void GetManagers(List<IManagerService> managers)
    {
      foreach (KeyValuePair<Type, IManagerService> keyValuePair in this.dictionary_0)
        managers.Add(keyValuePair.Value);
    }

    /// <summary>
    /// Use to apply user quality and performance preferences to the
    /// contained manager services.
    /// </summary>
    /// <param name="preferences"></param>
    public virtual void ApplyPreferences(ILightingSystemPreferences preferences)
    {
      foreach (IManagerService manager in this.Services)
        manager.ApplyPreferences(preferences);
    }

    /// <summary>
    /// Removes resources managed by the contained manager services.
    /// Commonly used while clearing the scene.
    /// 
    /// Note: this does not remove the contained manager services.
    /// </summary>
    public virtual void Clear()
    {
      foreach (IManagerService manager in this.Services)
        manager.Clear();
    }

    /// <summary>
    /// Disposes any graphics resources used internally by the
    /// contained manager services, and removes scene resources
    /// managed by them. Commonly used during Game.UnloadContent.
    /// </summary>
    public virtual void Unload()
    {
      foreach (IManagerService unloadable in this.Services)
        unloadable.Unload();
    }

    /// <summary>
    /// Updates the contained manager services and their managed resources.
    /// </summary>
    /// <param name="gameTime"></param>
    public virtual void Update(GameTime gameTime)
    {
      foreach (IUpdatableManager updatableManager in this.Updateables)
        updatableManager.Update(gameTime);
    }

    /// <summary>
    /// Sets up the contained manager services prior to rendering (used for forward rendering).
    /// </summary>
    /// <param name="scenestate"></param>
    public virtual void BeginFrameRendering(ISceneState scenestate)
    {
      foreach (IRenderableManager renderableManager in this.Renderables)
        renderableManager.BeginFrameRendering(scenestate);
    }

    /// <summary>
    /// Sets up the contained manager services prior to rendering (used for deferred rendering).
    /// </summary>
    /// <param name="scenestate"></param>
    /// <param name="deferredbuffers"></param>
    public virtual void BeginFrameRendering(ISceneState scenestate, DeferredBuffers deferredbuffers)
    {
      foreach (IRenderableManager renderableManager in this.Renderables)
      {
        if (renderableManager is DeferredRenderManager)
          (renderableManager as DeferredRenderManager).BeginFrameRendering(scenestate, deferredbuffers);
        else
          renderableManager.BeginFrameRendering(scenestate);
      }
    }

    /// <summary>
    /// Finalizes rendering on the contained manager services.
    /// </summary>
    public virtual void EndFrameRendering()
    {
      for (int index = this.Renderables.Count - 1; index >= 0; --index)
        this.Renderables[index].EndFrameRendering();
      LightingSystemStatistics.CommitChanges();
    }

    internal class ManagerServiceComparer : IComparer<IManagerService>
    {
      public int Compare(IManagerService imanagerService_0, IManagerService imanagerService_1)
      {
        if (imanagerService_0 == null)
          return -1;
        if (imanagerService_1 == null)
          return 1;
        return imanagerService_0.ManagerProcessOrder - imanagerService_1.ManagerProcessOrder;
      }
    }

    internal class RenderableComparer : IComparer<IRenderableManager>
    {
      public int Compare(IRenderableManager irenderableManager_0, IRenderableManager irenderableManager_1)
      {
        if (irenderableManager_0 == null)
          return -1;
        if (irenderableManager_1 == null)
          return 1;
        return (irenderableManager_0 as IManagerService).ManagerProcessOrder - (irenderableManager_1 as IManagerService).ManagerProcessOrder;
      }
    }

    internal class UpdatableComparer : IComparer<IUpdatableManager>
    {
      public int Compare(IUpdatableManager iupdatableManager_0, IUpdatableManager iupdatableManager_1)
      {
        if (iupdatableManager_0 == null)
          return -1;
        if (iupdatableManager_1 == null)
          return 1;
        return (iupdatableManager_0 as IManagerService).ManagerProcessOrder - (iupdatableManager_1 as IManagerService).ManagerProcessOrder;
      }
    }
  }
}
