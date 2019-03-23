// Decompiled with JetBrains decompiler
// Type: SynapseGaming.LightingSystem.Shadows.BaseShadowMapManager
// Assembly: SynapseGaming-SunBurn-Pro, Version=1.3.2.8, Culture=neutral, PublicKeyToken=c23c60523565dbfd
// MVID: A5F03349-72AC-4BAA-AEEE-9AB9B77E0A39
// Assembly location: C:\Projects\BlackBox\StarDrive\SynapseGaming-SunBurn-Pro.dll

using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using ns11;
using ns3;
using SynapseGaming.LightingSystem.Core;
using SynapseGaming.LightingSystem.Lights;

namespace SynapseGaming.LightingSystem.Shadows
{
    /// <summary>
    /// Base class that provides shadow map management.  Used by the forward rendering
    /// ShadowMapManager and deferred rendering DeferredShadowMapManager classes.
    /// </summary>
    public abstract class BaseShadowMapManager : BaseShadowManager, IUnloadable, IManager, IRenderableManager, IManagerService, IShadowMapVisibility, IShadowMapManager
    {
        private static readonly List<ShadowGroup> ShadowGroups = new List<ShadowGroup>(128);
        private static readonly List<Rectangle> list_1 = new List<Rectangle>();
        private static readonly Dictionary<RenderTarget, ShadowRenderTargetGroup> RenderTargetCache = new Dictionary<RenderTarget, ShadowRenderTargetGroup>();
        private bool shadowsEnabled = true;
        private float shadowQuality = 1f;
        private Enum9 enum9_0 = Enum9.const_1;
        private float float_2 = 1f;
        private readonly DisposablePool<ShadowRenderTargetGroup> ShadowRtgPool = new DisposablePool<ShadowRenderTargetGroup>();
        private readonly GraphicsDeviceMonitor GraphicsDeviceMonitor0;
        private readonly ShadowMapCache ShadowCache;
        private RenderTarget2D DefaultRenderTarget;

        /// <summary>
        /// Gets the manager specific Type used as a unique key for storing and
        /// requesting the manager from the IManagerServiceProvider.
        /// </summary>
        public Type ManagerType => SceneInterface.ShadowMapManagerType;

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
        public int ManagerProcessOrder { get; set; } = 40;

        /// <summary>
        /// Determines the transition range of each shadow level-of-detail. The range is normalized relative
        /// to the environment ShadowFadeEndDistance, for instance a value of 1.0 transitions at the
        /// ShadowFadeEndDistance whereas a value of 0.25 transitions at (ShadowFadeEndDistance * 0.25).
        /// Index 0 is the highest level of detail.
        /// </summary>
        public float[] ShadowLODRangeHints { get; } = new float[3] { 0.2f, 0.53f, 1f };

        /// <summary>
        /// True when smaller half-float format render targets are preferred. These
        /// formats consume less memory and generally perform better, but have lower
        /// accuracy on directional lights.
        /// </summary>
        public bool PreferHalfFloatTextureFormat
        {
            get => ShadowCache.PreferHalfFloatTextureFormat;
            set
            {
                if (ShadowCache.PreferHalfFloatTextureFormat == value)
                    return;
                ShadowCache.Resize(ShadowCache.PageSize, ShadowCache.MaxMemoryUsage, value);
            }
        }

        /// <summary>
        /// Maximum amount of memory the shadow map cache is allowed to consume. This is an
        /// approximate value and the cache may use more memory in certain instances.
        /// </summary>
        public int MaxMemoryUsage
        {
            get => ShadowCache.MaxMemoryUsage;
            set
            {
                if (ShadowCache.MaxMemoryUsage == value)
                    return;
                ShadowCache.Resize(ShadowCache.PageSize, value, ShadowCache.PreferHalfFloatTextureFormat);
            }
        }

        /// <summary>
        /// Size in pixels of each render target (page) in the cache. For a size of 1024
        /// the actual page dimensions are 1024x1024. Small sizes can reduce performance by
        /// fragmenting the shadow maps, and reduce shadow quality by lowering the maximum
        /// resolution of each shadow map section.
        /// </summary>
        public int PageSize
        {
            get => ShadowCache.PageSize;
            set
            {
                if (ShadowCache.PageSize == value)
                    return;
                ShadowCache.Resize(value, ShadowCache.MaxMemoryUsage, ShadowCache.PreferHalfFloatTextureFormat);
            }
        }

        private int _MaxShadowLOD => ShadowCache.PageSize >> 1;

        /// <summary>Creates a new BaseShadowMapManager instance.</summary>
        /// <param name="graphicsdevicemanager"></param>
        /// <param name="pagesize">Size in pixels of each render target (page) in the cache.
        /// For a size of 1024 the actual page dimensions are 1024x1024. Small sizes can reduce
        /// performance by fragmenting the shadow maps, and reduce shadow quality by lowering
        /// the maximum resolution of each shadow map section.</param>
        /// <param name="maxmemoryusage">Maximum amount of memory the cache is allowed to consume.
        /// This is an approximate value and the cache may use more memory in certain instances.</param>
        /// <param name="preferhalffloat">True when smaller half-float format render targets are
        /// preferred. These formats consume less memory and generally perform better, but have
        /// lower accuracy on directional lights.</param>
        public BaseShadowMapManager(IGraphicsDeviceService graphicsdevicemanager, int pagesize, int maxmemoryusage, bool preferhalffloat)
          : base(graphicsdevicemanager)
        {
            GraphicsDeviceMonitor0 = new GraphicsDeviceMonitor(graphicsdevicemanager);
            ShadowCache = new ShadowMapCache(graphicsdevicemanager, pagesize, maxmemoryusage, preferhalffloat);
        }

        /// <summary>Creates a new BaseShadowMapManager instance.</summary>
        /// <param name="graphicsdevicemanager"></param>
        /// <param name="shadowmapcache"></param>
        public BaseShadowMapManager(IGraphicsDeviceService graphicsdevicemanager, ShadowMapCache shadowmapcache)
          : base(graphicsdevicemanager)
        {
            GraphicsDeviceMonitor0 = new GraphicsDeviceMonitor(graphicsdevicemanager);
            ShadowCache = shadowmapcache;
        }

        /// <summary>Creates a new BaseShadowMapManager instance.</summary>
        /// <param name="graphicsdevicemanager"></param>
        public BaseShadowMapManager(IGraphicsDeviceService graphicsdevicemanager)
          : base(graphicsdevicemanager)
        {
            GraphicsDeviceMonitor0 = new GraphicsDeviceMonitor(graphicsdevicemanager);
            ShadowCache = new ShadowMapCache(graphicsdevicemanager, 2048, 67108864, false);
        }

        /// <summary>
        /// Creates a new or cached shadow map object for this light type.
        /// </summary>
        /// <param name="shadowsource">Shadow source which uses the newly created or cached shadow map object.
        /// Provides information about how the shadow is used, such as location and the type of objects rendered
        /// to the shadow map.</param>
        /// <returns></returns>
        protected abstract IShadowMap CreateDirectionalShadowMap(IShadowSource shadowsource);

        /// <summary>
        /// Creates a new or cached shadow map object for this light type.
        /// </summary>
        /// <param name="shadowsource">Shadow source which uses the newly created or cached shadow map object.
        /// Provides information about how the shadow is used, such as location and the type of objects rendered
        /// to the shadow map.</param>
        /// <returns></returns>
        protected abstract IShadowMap CreatePointShadowMap(IShadowSource shadowsource);

        /// <summary>
        /// Creates a new or cached shadow map object for this light type.
        /// </summary>
        /// <param name="shadowsource">Shadow source which uses the newly created or cached shadow map object.
        /// Provides information about how the shadow is used, such as location and the type of objects rendered
        /// to the shadow map.</param>
        /// <returns></returns>
        protected abstract IShadowMap CreateSpotShadowMap(IShadowSource shadowsource);

        /// <summary>
        /// Use to apply user quality and performance preferences to the resources managed by this object.
        /// </summary>
        /// <param name="preferences"></param>
        public override void ApplyPreferences(ILightingSystemPreferences preferences)
        {
            shadowsEnabled = preferences.ShadowDetail != DetailPreference.Off;
            shadowQuality = MathHelper.Clamp(preferences.ShadowQuality, 0.05f, 1f);
        }

        /// <summary>
        /// Organizes the provided lights into shadow and render target groups.
        /// </summary>
        /// <param name="rendertargetgroups">Returned render target groups.</param>
        /// <param name="lights">Lights to organize.</param>
        /// <param name="usedefaultgrouping">Determines if ungrouped lights should be placed in a
        /// single default group (recommended: true for deferred rendering and false for forward).</param>
        public void BuildShadows(List<ShadowRenderTargetGroup> rendertargetgroups, List<ILight> lights, bool usedefaultgrouping)
        {
            GraphicsDevice device = GraphicsDeviceManager.GraphicsDevice;
            if (DefaultRenderTarget == null)
                DefaultRenderTarget = new RenderTarget2D(device, 16, 16, 1, SurfaceFormat.Color);
            rendertargetgroups.Clear();
            ShadowGroups.Clear();
            RenderTargetCache.Clear();
            BuildShadowGroups(ShadowGroups, lights, usedefaultgrouping);
            if (ShadowGroups.Count < 1)
                return;
            float num1 = _MaxShadowLOD * shadowQuality * float_2;
            foreach (ShadowGroup shadowgroup in ShadowGroups)
            {
                RenderTarget2D rt = DefaultRenderTarget;
                if (shadowsEnabled && shadowgroup.ShadowSource.ShadowType != ShadowType.None)
                {
                    IShadowMap shadowMap;
                    if (shadowgroup.ShadowSource is ISpotSource)
                        shadowMap = CreateSpotShadowMap(shadowgroup.ShadowSource);
                    else if (shadowgroup.ShadowSource is IPointSource)
                        shadowMap = CreatePointShadowMap(shadowgroup.ShadowSource);
                    else if (shadowgroup.ShadowSource is IDirectionalSource)
                        shadowMap = CreateDirectionalShadowMap(shadowgroup.ShadowSource);
                    else
                        continue;

                    shadowMap.Build(device, SceneState, shadowgroup, this, shadowQuality);
                    if (shadowMap.CustomRenderTarget is RenderTarget2D rt2)
                    {
                        rt = rt2;
                    }
                    else
                    {
                        rt = RenderTarget2D(num1, shadowgroup, shadowMap);
                        if (rt == null)
                            continue;

                        for (int surface = 0; surface < shadowMap.Surfaces.Length; ++surface)
                            shadowMap.SetSurfaceRenderTargetLocation(surface, list_1[surface]);
                    }
                    shadowgroup.Shadow = shadowMap;
                }
                if (!RenderTargetCache.ContainsKey(rt))
                {
                    ShadowRenderTargetGroup rtg = ShadowRtgPool.New();
                    rtg.ShadowGroups.Clear();
                    rtg.ShadowGroups.Add(shadowgroup);
                    RenderTargetCache.Add(rt, rtg);
                }
                else
                    RenderTargetCache[rt].ShadowGroups.Add(shadowgroup);
            }
            foreach (KeyValuePair<RenderTarget, ShadowRenderTargetGroup> keyValuePair in RenderTargetCache)
            {
                ShadowRenderTargetGroup renderTargetGroup = keyValuePair.Value;
                RenderTarget rt = keyValuePair.Key;
                renderTargetGroup.Build(device, rt == DefaultRenderTarget ? null : rt, ShadowCache.DepthBuffer);
                rendertargetgroups.Add(renderTargetGroup);
            }
        }

        private RenderTarget2D RenderTarget2D(float num1, ShadowGroup shadowgroup, IShadowMap shadowMap)
        {
            RenderTarget2D renderTarget2D;
            float num2 = 1f;
            float num3 = num1 * shadowgroup.ShadowSource.ShadowQuality;
            Rectangle rectangle = new Rectangle();
            do
            {
                bool flag = true;
                list_1.Clear();
                foreach (ShadowMapSurface surface in shadowMap.Surfaces)
                {
                    int num4 = (int)MathHelper.Clamp(
                        (float)Math.Pow(2.0,
                            (float)Math.Floor(CoreUtils.smethod_0(num3 * num2 * surface.LevelOfDetail) * 2.0) * 0.5f), 32f,
                        _MaxShadowLOD);
                    rectangle.Width = num4;
                    rectangle.Height = num4;
                    list_1.Add(rectangle);
                    if (num4 > 32)
                        flag = false;
                }

                renderTarget2D = ShadowCache.ReserveSections(list_1);
                if (renderTarget2D == null)
                    enum9_0 = Enum9.const_0;
                if (!flag)
                    num2 *= 0.5f;
                else
                    break;
            } while (renderTarget2D == null);

            return renderTarget2D;
        }

        /// <summary>
        /// Sets up frame information necessary for scene shadowing.
        /// </summary>
        public override void BeginFrameRendering(ISceneState state)
        {
            if (GraphicsDeviceMonitor0.Changed)
                Unload();
            base.BeginFrameRendering(state);
        }

        /// <summary>
        /// Cleans up frame information including removing all reserved shadow maps.
        /// </summary>
        public override void EndFrameRendering()
        {
            if (enum9_0 == Enum9.const_0)
            {
                float_2 *= 0.75f;
                enum9_0 = Enum9.const_1;
            }
            else if (float_2 < 1.0 && (enum9_0 == Enum9.const_2 || ShadowCache.method_1() < 0.330000013113022))
            {
                float_2 = Math.Min(float_2 * 1.33f, 1f);
                enum9_0 = Enum9.const_2;
            }
            ShadowCache.ClearReserves();
            ShadowRtgPool.RecycleAllTracked();
            base.EndFrameRendering();
        }

        /// <summary>Cleans up scene information.</summary>
        public override void Clear()
        {
        }

        /// <summary>
        /// Disposes any graphics resource used internally by this object, and removes
        /// scene resources managed by this object. Commonly used during Game.UnloadContent.
        /// </summary>
        public override void Unload()
        {
            ShadowCache.Unload();
            ShadowRtgPool.Clear();
            Disposable.Free(ref DefaultRenderTarget);
        }

        private enum Enum9
        {
            const_0,
            const_1,
            const_2
        }
    }
}
