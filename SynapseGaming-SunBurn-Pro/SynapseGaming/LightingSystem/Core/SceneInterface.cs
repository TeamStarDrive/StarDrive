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
        static readonly ManagerServiceComparer ManagerComp = new ManagerServiceComparer();
        static readonly RenderableComparer RenderableComp = new RenderableComparer();
        static readonly UpdatableComparer UpdatableComp = new UpdatableComparer();
        readonly Dictionary<Type, IManagerService> Managers = new Dictionary<Type, IManagerService>(16);
        readonly List<IManagerService> Services = new List<IManagerService>(16);
        readonly List<IUpdatableManager> Updateables = new List<IUpdatableManager>(16);
        readonly List<IRenderableManager> Renderables = new List<IRenderableManager>(16);

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
        public static Type ResourceManagerType => typeof(IResourceManager);

        /// <summary>
        /// Manager type used to retrieve the IObjectManager manager service.  Use this
        /// type when creating a custom manager that replaces the built-in manager.
        /// </summary>
        public static Type ObjectManagerType => typeof(IObjectManager);

        /// <summary>
        /// Manager type used to retrieve the IRenderManager manager service.  Use this
        /// type when creating a custom manager that replaces the built-in manager.
        /// </summary>
        public static Type RenderManagerType => typeof(IRenderManager);

        /// <summary>
        /// Manager type used to retrieve the ILightManager manager service.  Use this
        /// type when creating a custom manager that replaces the built-in manager.
        /// </summary>
        public static Type LightManagerType => typeof(ILightManager);

        /// <summary>
        /// Manager type used to retrieve the IShadowMapManager manager service.  Use this
        /// type when creating a custom manager that replaces the built-in manager.
        /// </summary>
        public static Type ShadowMapManagerType => typeof(IShadowMapManager);

        /// <summary>
        /// Manager type used to retrieve the IPostProcessManager manager service.  Use this
        /// type when creating a custom manager that replaces the built-in manager.
        /// </summary>
        public static Type PostProcessManagerType => typeof(IPostProcessManager);

        /// <summary>
        /// Manager type used to retrieve the LightingSystemEditor manager service.
        /// </summary>
        public static Type EditorType => typeof(LightingSystemEditor);

        /// <summary>
        /// Manager type used to retrieve the IAvatarManager manager service.  Use this
        /// type when creating a custom manager that replaces the built-in manager.
        /// </summary>
        public static Type AvatarManagerType => typeof(IAvatarManager);

        /// <summary>Creates a new SceneInterface instance.</summary>
        /// <param name="device"></param>
        public SceneInterface(IGraphicsDeviceService device)
        {
            GraphicsDeviceManager = device;
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
        /// <param name="useDeferredRendering"></param>
        /// <param name="useAvatars"></param>
        /// <param name="usePostProcessing"></param>
        public void CreateDefaultManagers(bool useDeferredRendering, bool useAvatars, bool usePostProcessing)
        {
            Unload();
            AddManager(new ResourceManager());
            AddManager(new ObjectManager(GraphicsDeviceManager));
            AddManager(new LightManager(GraphicsDeviceManager));
            if (useDeferredRendering)
            {
                AddManager(new DeferredRenderManager(GraphicsDeviceManager, this));
                AddManager(new DeferredShadowMapManager(GraphicsDeviceManager));
            }
            else
            {
                AddManager(new RenderManager(GraphicsDeviceManager, this));
                AddManager(new ShadowMapManager(GraphicsDeviceManager));
            }
            if (useAvatars)
                AddManager(new AvatarManager(GraphicsDeviceManager, this));
            if (usePostProcessing)
                AddManager(new PostProcessManager(GraphicsDeviceManager));
        }

        /// <summary>Adds a manager service to the provider.</summary>
        /// <param name="manager"></param>
        public virtual void AddManager(IManagerService manager)
        {
            RemoveManager(manager);
            if (Managers.ContainsKey(manager.ManagerType))
                return;
            Managers.Add(manager.ManagerType, manager);
            ResortServices();
            UpdateManagers();
        }

        /// <summary>Removes a manager service from the provider.</summary>
        /// <param name="manager"></param>
        public virtual void RemoveManager(IManagerService manager)
        {
            if (!Managers.ContainsKey(manager.ManagerType))
                return;
            Managers.Remove(manager.ManagerType);
            ResortServices();
            UpdateManagers();
        }

        void UpdateManagers()
        {
            ResourceManager = (IResourceManager)GetManager(ResourceManagerType, false);
            ObjectManager = (IObjectManager)GetManager(ObjectManagerType, false);
            RenderManager = (IRenderManager)GetManager(RenderManagerType, false);
            LightManager = (ILightManager)GetManager(LightManagerType, false);
            ShadowMapManager = (IShadowMapManager)GetManager(ShadowMapManagerType, false);
            PostProcessManager = (IPostProcessManager)GetManager(PostProcessManagerType, false);
            AvatarManager = (IAvatarManager)GetManager(AvatarManagerType, false);
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
            Services.Clear();
            Renderables.Clear();
            Updateables.Clear();
            foreach (KeyValuePair<Type, IManagerService> keyValuePair in Managers)
            {
                IManagerService managerService = keyValuePair.Value;
                Services.Add(managerService);
                if (managerService is IRenderableManager renderable)
                    Renderables.Add(renderable);
                if (managerService is IUpdatableManager updatable)
                    Updateables.Add(updatable);
            }
            Services.Sort(ManagerComp);
            Renderables.Sort(RenderableComp);
            Updateables.Sort(UpdatableComp);
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
            Type managertype = typeof(T);
            T manager = (T)GetManager(managertype, required);
            if (manager == null && required)
                throw new Exception("Service manager does not contain a service assigned to the '" + managertype.Name + "' type.");
            return manager;
        }

        /// <summary>
        /// Retrieves a manager service by type from the provider.
        /// </summary>
        /// <param name="type">Type used by the manager as a unique
        /// identifying key (IManagerService.ManagerType).</param>
        /// <param name="required">Determines whether an exception should
        /// be thrown if the manager is not found.</param>
        /// <returns></returns>
        public IManagerService GetManager(Type type, bool required)
        {
            Managers.TryGetValue(type, out IManagerService service);
            if (service == null && required)
                throw new Exception("Service manager does not contain a service assigned to the '" + type.Name + "' type.");
            return service;
        }

        /// <summary>Retrieves all manager services from the provider.</summary>
        /// <param name="managers">List used to store manager services.</param>
        public void GetManagers(List<IManagerService> managers)
        {
            foreach (KeyValuePair<Type, IManagerService> keyValuePair in Managers)
                managers.Add(keyValuePair.Value);
        }

        /// <summary>
        /// Use to apply user quality and performance preferences to the
        /// contained manager services.
        /// </summary>
        /// <param name="preferences"></param>
        public virtual void ApplyPreferences(ILightingSystemPreferences preferences)
        {
            foreach (IManagerService manager in Services)
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
            foreach (IManagerService manager in Services)
                manager.Clear();
        }

        /// <summary>
        /// Disposes any graphics resources used internally by the
        /// contained manager services, and removes scene resources
        /// managed by them. Commonly used during Game.UnloadContent.
        /// </summary>
        public virtual void Unload()
        {
            foreach (IManagerService unloadable in Services)
                unloadable.Unload();
        }

        /// <summary>
        /// Updates the contained manager services and their managed resources.
        /// </summary>
        /// <param name="deltaTime"></param>
        public virtual void Update(float deltaTime)
        {
            foreach (IUpdatableManager updatableManager in Updateables)
                updatableManager.Update(deltaTime);
        }

        /// <summary>
        /// Sets up the contained manager services prior to rendering (used for forward rendering).
        /// </summary>
        /// <param name="state"></param>
        public virtual void BeginFrameRendering(ISceneState state)
        {
            for (int i = 0; i < Renderables.Count; i++)
            {
                IRenderableManager renderer = Renderables[i];
                renderer.BeginFrameRendering(state);
            }
        }

        /// <summary>
        /// Sets up the contained manager services prior to rendering (used for deferred rendering).
        /// </summary>
        /// <param name="state"></param>
        /// <param name="deferredbuffers"></param>
        public virtual void BeginFrameRendering(ISceneState state, DeferredBuffers deferredbuffers)
        {
            foreach (IRenderableManager renderer in Renderables)
            {
                if (renderer is DeferredRenderManager deferred)
                    deferred.BeginFrameRendering(state, deferredbuffers);
                else
                    renderer.BeginFrameRendering(state);
            }
        }

        /// <summary>
        /// Finalizes rendering on the contained manager services.
        /// </summary>
        public virtual void EndFrameRendering()
        {
            for (int index = Renderables.Count - 1; index >= 0; --index)
                Renderables[index].EndFrameRendering();
            LightingSystemStatistics.CommitChanges();
        }

        internal class ManagerServiceComparer : IComparer<IManagerService>
        {
            public int Compare(IManagerService a, IManagerService b)
            {
                if (a == null)
                    return -1;
                if (b == null)
                    return 1;
                return a.ManagerProcessOrder - b.ManagerProcessOrder;
            }
        }

        internal class RenderableComparer : IComparer<IRenderableManager>
        {
            public int Compare(IRenderableManager a, IRenderableManager b)
            {
                if (a == null)
                    return -1;
                if (b == null)
                    return 1;
                return ((IManagerService) a).ManagerProcessOrder - ((IManagerService) b).ManagerProcessOrder;
            }
        }

        internal class UpdatableComparer : IComparer<IUpdatableManager>
        {
            public int Compare(IUpdatableManager a, IUpdatableManager b)
            {
                if (a == null)
                    return -1;
                if (b == null)
                    return 1;
                return ((IManagerService) a).ManagerProcessOrder - ((IManagerService) b).ManagerProcessOrder;
            }
        }
    }
}
