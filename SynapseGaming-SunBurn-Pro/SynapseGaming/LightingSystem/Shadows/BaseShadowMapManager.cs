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
    private static List<ShadowGroup> list_0 = new List<ShadowGroup>(128);
    private static List<Rectangle> list_1 = new List<Rectangle>();
    private static Dictionary<RenderTarget, ShadowRenderTargetGroup> dictionary_1 = new Dictionary<RenderTarget, ShadowRenderTargetGroup>();
      private bool bool_0 = true;
    private float float_1 = 1f;
    private Enum9 enum9_0 = Enum9.const_1;
    private float float_2 = 1f;
    private DisposablePool<ShadowRenderTargetGroup> DisposablePool0 = new DisposablePool<ShadowRenderTargetGroup>();
    private const int int_0 = 67108864;
    private const int int_1 = 2048;
    private const int int_2 = 32;
    private GraphicsDeviceMonitor GraphicsDeviceMonitor0;
    private ShadowMapCache shadowMapCache_0;
    private RenderTarget2D renderTarget2D_0;

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
    public float[] ShadowLODRangeHints { get; } = new float[3]{ 0.2f, 0.53f, 1f };

      /// <summary>
    /// True when smaller half-float format render targets are preferred. These
    /// formats consume less memory and generally perform better, but have lower
    /// accuracy on directional lights.
    /// </summary>
    public bool PreferHalfFloatTextureFormat
    {
      get => this.shadowMapCache_0.PreferHalfFloatTextureFormat;
          set
      {
        if (this.shadowMapCache_0.PreferHalfFloatTextureFormat == value)
          return;
        this.shadowMapCache_0.Resize(this.shadowMapCache_0.PageSize, this.shadowMapCache_0.MaxMemoryUsage, value);
      }
    }

    /// <summary>
    /// Maximum amount of memory the shadow map cache is allowed to consume. This is an
    /// approximate value and the cache may use more memory in certain instances.
    /// </summary>
    public int MaxMemoryUsage
    {
      get => this.shadowMapCache_0.MaxMemoryUsage;
        set
      {
        if (this.shadowMapCache_0.MaxMemoryUsage == value)
          return;
        this.shadowMapCache_0.Resize(this.shadowMapCache_0.PageSize, value, this.shadowMapCache_0.PreferHalfFloatTextureFormat);
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
      get => this.shadowMapCache_0.PageSize;
        set
      {
        if (this.shadowMapCache_0.PageSize == value)
          return;
        this.shadowMapCache_0.Resize(value, this.shadowMapCache_0.MaxMemoryUsage, this.shadowMapCache_0.PreferHalfFloatTextureFormat);
      }
    }

    private int _MaxShadowLOD => this.shadowMapCache_0.PageSize >> 1;

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
      this.GraphicsDeviceMonitor0 = new GraphicsDeviceMonitor(graphicsdevicemanager);
      this.shadowMapCache_0 = new ShadowMapCache(graphicsdevicemanager, pagesize, maxmemoryusage, preferhalffloat);
    }

    /// <summary>Creates a new BaseShadowMapManager instance.</summary>
    /// <param name="graphicsdevicemanager"></param>
    /// <param name="shadowmapcache"></param>
    public BaseShadowMapManager(IGraphicsDeviceService graphicsdevicemanager, ShadowMapCache shadowmapcache)
      : base(graphicsdevicemanager)
    {
      this.GraphicsDeviceMonitor0 = new GraphicsDeviceMonitor(graphicsdevicemanager);
      this.shadowMapCache_0 = shadowmapcache;
    }

    /// <summary>Creates a new BaseShadowMapManager instance.</summary>
    /// <param name="graphicsdevicemanager"></param>
    public BaseShadowMapManager(IGraphicsDeviceService graphicsdevicemanager)
      : base(graphicsdevicemanager)
    {
      this.GraphicsDeviceMonitor0 = new GraphicsDeviceMonitor(graphicsdevicemanager);
      this.shadowMapCache_0 = new ShadowMapCache(graphicsdevicemanager, 2048, 67108864, false);
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
      this.bool_0 = preferences.ShadowDetail != DetailPreference.Off;
      this.float_1 = MathHelper.Clamp(preferences.ShadowQuality, 0.05f, 1f);
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
      GraphicsDevice graphicsDevice = this.GraphicsDeviceManager.GraphicsDevice;
      if (this.renderTarget2D_0 == null)
        this.renderTarget2D_0 = new RenderTarget2D(graphicsDevice, 16, 16, 1, SurfaceFormat.Color);
      rendertargetgroups.Clear();
      list_0.Clear();
      dictionary_1.Clear();
      this.BuildShadowGroups(list_0, lights, usedefaultgrouping);
      if (list_0.Count < 1)
        return;
      float num1 = this._MaxShadowLOD * this.float_1 * this.float_2;
      foreach (ShadowGroup shadowgroup in list_0)
      {
        RenderTarget2D renderTarget2D = this.renderTarget2D_0;
        if (this.bool_0 && shadowgroup.ShadowSource.ShadowType != ShadowType.None)
        {
          IShadowMap shadowMap;
          if (shadowgroup.ShadowSource is ISpotSource)
            shadowMap = this.CreateSpotShadowMap(shadowgroup.ShadowSource);
          else if (shadowgroup.ShadowSource is IPointSource)
            shadowMap = this.CreatePointShadowMap(shadowgroup.ShadowSource);
          else if (shadowgroup.ShadowSource is IDirectionalSource)
            shadowMap = this.CreateDirectionalShadowMap(shadowgroup.ShadowSource);
          else
            continue;
          shadowMap.Build(graphicsDevice, this.SceneState, shadowgroup, this, this.float_1);
          if (shadowMap.CustomRenderTarget is RenderTarget2D)
          {
            renderTarget2D = shadowMap.CustomRenderTarget as RenderTarget2D;
          }
          else
          {
            float num2 = 1f;
            float num3 = num1 * shadowgroup.ShadowSource.ShadowQuality;
            Rectangle rectangle = new Rectangle();
            do
            {
              bool flag = true;
              list_1.Clear();
              foreach (ShadowMapSurface surface in shadowMap.Surfaces)
              {
                int num4 = (int) MathHelper.Clamp((float) Math.Pow(2.0, (float) Math.Floor(CoreUtils.smethod_0(num3 * num2 * surface.LevelOfDetail) * 2.0) * 0.5f), 32f, this._MaxShadowLOD);
                rectangle.Width = num4;
                rectangle.Height = num4;
                list_1.Add(rectangle);
                if (num4 > 32)
                  flag = false;
              }
              renderTarget2D = this.shadowMapCache_0.ReserveSections(list_1);
              if (renderTarget2D == null)
                this.enum9_0 = Enum9.const_0;
              if (!flag)
                num2 *= 0.5f;
              else
                break;
            }
            while (renderTarget2D == null);
            if (renderTarget2D != null)
            {
              for (int surface = 0; surface < shadowMap.Surfaces.Length; ++surface)
                shadowMap.SetSurfaceRenderTargetLocation(surface, list_1[surface]);
            }
            else
              continue;
          }
          shadowgroup.Shadow = shadowMap;
        }
        if (!dictionary_1.ContainsKey(renderTarget2D))
        {
          ShadowRenderTargetGroup renderTargetGroup = this.DisposablePool0.New();
          renderTargetGroup.ShadowGroups.Clear();
          renderTargetGroup.ShadowGroups.Add(shadowgroup);
          dictionary_1.Add(renderTarget2D, renderTargetGroup);
        }
        else
          dictionary_1[renderTarget2D].ShadowGroups.Add(shadowgroup);
      }
      foreach (KeyValuePair<RenderTarget, ShadowRenderTargetGroup> keyValuePair in dictionary_1)
      {
        ShadowRenderTargetGroup renderTargetGroup = keyValuePair.Value;
        RenderTarget key = keyValuePair.Key;
        if (key == this.renderTarget2D_0)
          renderTargetGroup.Build(graphicsDevice, null, this.shadowMapCache_0.DepthBuffer);
        else
          renderTargetGroup.Build(graphicsDevice, key, this.shadowMapCache_0.DepthBuffer);
        rendertargetgroups.Add(renderTargetGroup);
      }
    }

    /// <summary>
    /// Sets up frame information necessary for scene shadowing.
    /// </summary>
    public override void BeginFrameRendering(ISceneState scenestate)
    {
      if (this.GraphicsDeviceMonitor0.Changed)
        this.Unload();
      base.BeginFrameRendering(scenestate);
    }

    /// <summary>
    /// Cleans up frame information including removing all reserved shadow maps.
    /// </summary>
    public override void EndFrameRendering()
    {
      if (this.enum9_0 == Enum9.const_0)
      {
        this.float_2 *= 0.75f;
        this.enum9_0 = Enum9.const_1;
      }
      else if (this.float_2 < 1.0 && (this.enum9_0 == Enum9.const_2 || shadowMapCache_0.method_1() < 0.330000013113022))
      {
        this.float_2 = Math.Min(this.float_2 * 1.33f, 1f);
        this.enum9_0 = Enum9.const_2;
      }
      this.shadowMapCache_0.ClearReserves();
      this.DisposablePool0.RecycleAllTracked();
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
      this.shadowMapCache_0.Unload();
      this.DisposablePool0.Clear();
      Disposable.Free(ref renderTarget2D_0);
    }

    private enum Enum9
    {
      const_0,
      const_1,
      const_2
    }
  }
}
