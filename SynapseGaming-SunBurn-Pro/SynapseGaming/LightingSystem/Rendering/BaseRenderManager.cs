// Decompiled with JetBrains decompiler
// Type: SynapseGaming.LightingSystem.Rendering.BaseRenderManager
// Assembly: SynapseGaming-SunBurn-Pro, Version=1.3.2.8, Culture=neutral, PublicKeyToken=c23c60523565dbfd
// MVID: A5F03349-72AC-4BAA-AEEE-9AB9B77E0A39
// Assembly location: C:\Projects\BlackBox\StarDrive\SynapseGaming-SunBurn-Pro.dll

using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using ns10;
using SynapseGaming.LightingSystem.Core;
using SynapseGaming.LightingSystem.Lights;
using SynapseGaming.LightingSystem.Shadows;

namespace SynapseGaming.LightingSystem.Rendering
{
    /// <summary>
    /// Base class that provides basic render management.  Used by the forward rendering
    /// RenderManager and deferred rendering DeferredRenderManager classes.
    /// </summary>
    public abstract class BaseRenderManager : IUnloadable, IManager, IRenderableManager, IManagerService, IRenderManager
    {
        private ShadowRenderTargetGroup shadowRenderTargetGroup_0 = new ShadowRenderTargetGroup();
        internal Class57 class57_0 = new Class57();
        private Class71 class71_0;

        /// <summary>
        /// Gets the manager specific Type used as a unique key for storing and
        /// requesting the manager from the IManagerServiceProvider.
        /// </summary>
        public Type ManagerType => SceneInterface.RenderManagerType;

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
        public int ManagerProcessOrder { get; set; } = 60;

        /// <summary>
        /// The current GraphicsDeviceManager used by this object.
        /// </summary>
        public IGraphicsDeviceService GraphicsDeviceManager { get; }

        /// <summary>
        /// Enables clearing the back buffer during rendering.
        /// Disabling allows custom rendering (such as skybox)
        /// prior to calling RenderManager.Render().
        /// </summary>
        public bool ClearBackBufferEnabled { get; set; } = true;

        /// <summary>
        /// Changes the render fill mode allowing solid and wireframe rendering.
        /// </summary>
        public FillMode RenderFillMode { get; set; } = FillMode.Solid;

        /// <summary>Maximum mipmap level used during rendering.</summary>
        public int MaxMipLevel { get; set; }

        /// <summary>Mipmap level of detail bias applied during rendering.</summary>
        public float MipMapLevelOfDetailBias { get; set; }

        /// <summary>
        /// Current composite ambient light (combination of all scene ambient lights)
        /// provided by the LightManager (only valid between calls to
        /// BeginFrameRendering and EndFrameRendering).
        /// </summary>
        public AmbientLight FrameAmbientLight { get; } = new AmbientLight();

        /// <summary>
        /// Current scene lights provided by the LightManager (only valid between
        /// calls to BeginFrameRendering and EndFrameRendering).
        /// </summary>
        public List<ILight> FrameLights { get; } = new List<ILight>();

        /// <summary>
        /// Determines the current rendering quality based on the user preferences provided to ApplyPreferences.
        /// </summary>
        protected int MaxAnisotropy { get; private set; } = 4;

        /// <summary>
        /// Determines the current rendering quality based on the user preferences provided to ApplyPreferences.
        /// </summary>
        protected int MaxLoadedMipLevel { get; private set; }

        /// <summary>
        /// Determines the current rendering quality based on the user preferences provided to ApplyPreferences.
        /// </summary>
        protected bool MaxLoadedMipLevelEnabled { get; private set; }

        /// <summary>
        /// Current scene state information provided to BeginFrameRendering (only valid between calls to BeginFrameRendering and EndFrameRendering).
        /// </summary>
        protected ISceneState SceneState { get; private set; }

        /// <summary>
        /// Determines the current rendering quality based on the user preferences provided to ApplyPreferences.
        /// </summary>
        protected DetailPreference ShadowDetail { get; private set; } = DetailPreference.Medium;

        /// <summary>
        /// Determines the current rendering quality based on the user preferences provided to ApplyPreferences.
        /// </summary>
        protected DetailPreference EffectDetail { get; private set; }

        /// <summary>
        /// Determines the current rendering quality based on the user preferences provided to ApplyPreferences.
        /// </summary>
        protected TextureFilter MipFilter { get; private set; } = TextureFilter.Linear;

        /// <summary>
        /// Determines the current rendering quality based on the user preferences provided to ApplyPreferences.
        /// </summary>
        protected TextureFilter MinFilter { get; private set; } = TextureFilter.Anisotropic;

        /// <summary>
        /// Determines the current rendering quality based on the user preferences provided to ApplyPreferences.
        /// </summary>
        protected TextureFilter MagFilter { get; private set; } = TextureFilter.Anisotropic;

        /// <summary>
        /// Current ambient lights provided by the LightManager (only valid between calls to BeginFrameRendering and EndFrameRendering).
        /// </summary>
        protected List<ILight> FrameAmbientLights { get; } = new List<ILight>();

        /// <summary>
        /// Service provider used to access all other manager services in this scene. Allows querying
        /// objects through the IObjectManager manager interface, querying lights through the ILightManager manager
        /// interface, and more.
        /// </summary>
        protected IManagerServiceProvider ServiceProvider { get; }

        /// <summary>Creates a new BaseRenderManager instance.</summary>
        /// <param name="graphicsdevicemanager"></param>
        /// <param name="sceneinterface">Service provider used to access all other manager services in this scene.</param>
        public BaseRenderManager(IGraphicsDeviceService graphicsdevicemanager, IManagerServiceProvider sceneinterface)
        {
            GraphicsDeviceManager = graphicsdevicemanager;
            ServiceProvider = sceneinterface;
            class71_0 = new Class71(graphicsdevicemanager);
        }

        /// <summary>
        /// Use to apply user quality and performance preferences to the resources managed by this object.
        /// </summary>
        /// <param name="preferences"></param>
        public virtual void ApplyPreferences(ILightingSystemPreferences preferences)
        {
            switch (preferences.TextureSampling)
            {
                case SamplingPreference.Bilinear:
                    MinFilter = TextureFilter.Linear;
                    MagFilter = TextureFilter.Linear;
                    MipFilter = TextureFilter.Point;
                    break;
                case SamplingPreference.Trilinear:
                    MinFilter = TextureFilter.Linear;
                    MagFilter = TextureFilter.Linear;
                    MipFilter = TextureFilter.Linear;
                    break;
                case SamplingPreference.Anisotropic:
                    MinFilter = TextureFilter.Anisotropic;
                    MagFilter = TextureFilter.Anisotropic;
                    MipFilter = TextureFilter.Linear;
                    break;
            }
            MaxAnisotropy = preferences.MaxAnisotropy;
            MaxLoadedMipLevel = (int)preferences.TextureQuality;
            MaxLoadedMipLevelEnabled = MaxLoadedMipLevel != 0;
            ShadowDetail = preferences.ShadowDetail;
            EffectDetail = preferences.EffectDetail;
            class71_0.ApplyPreferences(preferences);
        }

        /// <summary>
        /// Applies the maximum loaded mipmap level to the provided texture, which reduces texture quality and memory usage by removing the top (n) texture level.
        /// </summary>
        /// <param name="texture"></param>
        protected void SetTextureLOD(Texture texture)
        {
            if (texture == null || texture.IsDisposed || texture.LevelCount < 1)
                return;
            int num = Math.Min(texture.LevelCount - 1, MaxLoadedMipLevel);
            if (texture.LevelOfDetail == num)
                return;
            texture.LevelOfDetail = num;
        }

        /// <summary>
        /// Provides a default set of shadow groups when no IShadowMapManager manager service is available.
        /// </summary>
        /// <param name="rendertargetgroups">Returned render target groups.</param>
        /// <param name="lights">Source lights to create groups for.</param>
        protected void GetDefaultShadows(List<ShadowRenderTargetGroup> rendertargetgroups, List<ILight> lights)
        {
            shadowRenderTargetGroup_0.ShadowGroups.Clear();
            class71_0.method_0(shadowRenderTargetGroup_0.ShadowGroups, lights, true);
            shadowRenderTargetGroup_0.Build(GraphicsDeviceManager.GraphicsDevice, null, null);
            rendertargetgroups.Add(shadowRenderTargetGroup_0);
        }

        /// <summary>
        /// Generates shadow maps for the provided shadow render groups. Override this
        /// method to customize shadow map generation.
        /// </summary>
        /// <param name="shadowrendertargetgroups">Shadow render groups to generate shadow maps for.</param>
        protected abstract void BuildShadowMaps(List<ShadowRenderTargetGroup> shadowrendertargetgroups);

        /// <summary>
        /// Builds all object batches, shadow maps, and cached information before rendering.
        /// Any object added to the RenderManager after this call will not be visible during the frame.
        /// </summary>
        /// <param name="state"></param>
        public virtual void BeginFrameRendering(ISceneState state)
        {
            //SplashScreen.CheckProductActivation();
            SceneState = state;
            GraphicsDevice graphicsDevice = GraphicsDeviceManager.GraphicsDevice;
            int num1 = Math.Max(Math.Min(MaxAnisotropy, LightingSystemManager.Instance.GetGraphicsDeviceSupport(graphicsDevice).MaxAnisotropy), 1);
            for (int index = 0; index < 8; ++index)
            {
                SamplerState samplerState = graphicsDevice.SamplerStates[index];
                samplerState.MaxAnisotropy = num1;
                samplerState.MaxMipLevel = MaxMipLevel;
            }
            class71_0.BeginFrameRendering(state);
            FrameLights.Clear();
            FrameAmbientLights.Clear();
            ILightManager manager = (ILightManager)ServiceProvider.GetManager(SceneInterface.LightManagerType, false);
            if (manager == null)
            {
                FrameAmbientLight.DiffuseColor = new Vector3(1f, 0.9f, 0.8f);
                FrameAmbientLight.Intensity = 0.25f;
            }
            else
            {
                manager.Find(FrameLights, SceneState.ViewFrustum, ObjectFilter.EnabledDynamicAndStatic);
                FrameAmbientLight.DiffuseColor = Vector3.Zero;
                FrameAmbientLight.Intensity = 1f;
                int num2 = 0;
                float num3 = 0.0f;
                Matrix viewToWorld = state.ViewToWorld;
                for (int index = 0; index < FrameLights.Count; ++index)
                {
                    ILight light = FrameLights[index];
                    if (light is IAmbientSource)
                    {
                        num3 += (light as IAmbientSource).Depth;
                        ++num2;
                        FrameAmbientLight.DiffuseColor = FrameAmbientLight.DiffuseColor + light.CompositeColorAndIntensity;
                        FrameLights.RemoveAt(index);
                        --index;
                    }
                    if (light is IPointSource)
                    {
                        IPointSource pointSource = light as IPointSource;
                        float num4 = Vector3.DistanceSquared(viewToWorld.Translation, pointSource.Position);
                        float num5 = SceneState.Environment.VisibleDistance + pointSource.Radius;
                        if (num4 > num5 * (double)num5)
                        {
                            FrameLights.RemoveAt(index);
                            --index;
                        }
                    }
                }
                FrameAmbientLight.Depth = num2 <= 0 ? 0.1f : num3 / num2;
            }
            FrameAmbientLights.Add(FrameAmbientLight);
        }

        /// <summary>Renders the scene.</summary>
        public abstract void Render();

        /// <summary>
        /// Finalizes rendering and cleans up frame information including removing all frame lifespan objects.
        /// </summary>
        public virtual void EndFrameRendering()
        {
            class71_0.EndFrameRendering();
        }

        /// <summary>
        /// Removes all scene objects and cleans up scene information.
        /// </summary>
        public virtual void Clear()
        {
            class71_0.Clear();
        }

        /// <summary>
        /// Unloads all scene and device specific data.  Must be called
        /// when the device is reset (during Game.UnloadGraphicsContent()).
        /// </summary>
        public virtual void Unload()
        {
            Clear();
            class71_0.Unload();
        }

        internal class Class57
        {
            public LightingSystemStatistic lightingSystemStatistic_0 = LightingSystemStatistics.GetStatistic("Renderer_PolysRendered", LightingSystemStatisticCategory.Rendering);
            public LightingSystemStatistic lightingSystemStatistic_1 = LightingSystemStatistics.GetStatistic("Renderer_SceneObjectsRendered", LightingSystemStatisticCategory.Rendering);
            public LightingSystemStatistic lightingSystemStatistic_2 = LightingSystemStatistics.GetStatistic("Renderer_MeshesRendered", LightingSystemStatisticCategory.Rendering);
            public LightingSystemStatistic lightingSystemStatistic_3 = LightingSystemStatistics.GetStatistic("Renderer_Batches", LightingSystemStatisticCategory.Rendering);
            public LightingSystemStatistic lightingSystemStatistic_4 = LightingSystemStatistics.GetStatistic("Renderer_BatchPasses", LightingSystemStatisticCategory.Rendering);
            public LightingSystemStatistic lightingSystemStatistic_5 = LightingSystemStatistics.GetStatistic("Renderer_BatchCullModeChanges", LightingSystemStatisticCategory.Rendering);
            public LightingSystemStatistic lightingSystemStatistic_6 = LightingSystemStatistics.GetStatistic("Renderer_BatchEffectCommitChanges", LightingSystemStatisticCategory.Rendering);
            public LightingSystemStatistic lightingSystemStatistic_7 = LightingSystemStatistics.GetStatistic("Light_LightsRendered", LightingSystemStatisticCategory.Lighting);
            public LightingSystemStatistic lightingSystemStatistic_8 = LightingSystemStatistics.GetStatistic("Light_LightsRenderedAsGroup", LightingSystemStatisticCategory.Lighting);
            public LightingSystemStatistic lightingSystemStatistic_9 = LightingSystemStatistics.GetStatistic("Shadow_ShadowGroupsRendered", LightingSystemStatisticCategory.Shadowing);
            public LightingSystemStatistic lightingSystemStatistic_10 = LightingSystemStatistics.GetStatistic("Shadow_ShadowMapPagesProcessed", LightingSystemStatisticCategory.Shadowing);
            public LightingSystemStatistic lightingSystemStatistic_11 = LightingSystemStatistics.GetStatistic("Shadow_ShadowMapsProcessed", LightingSystemStatisticCategory.Shadowing);
            public LightingSystemStatistic lightingSystemStatistic_12 = LightingSystemStatistics.GetStatistic("Shadow_ShadowMapFacesProcessed", LightingSystemStatisticCategory.Shadowing);
            public LightingSystemStatistic lightingSystemStatistic_13 = LightingSystemStatistics.GetStatistic("Shadow_ShadowMapFacesFilled", LightingSystemStatisticCategory.Shadowing);
        }
    }
}
