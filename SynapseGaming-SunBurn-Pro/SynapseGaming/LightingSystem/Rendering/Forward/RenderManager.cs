// Decompiled with JetBrains decompiler
// Type: SynapseGaming.LightingSystem.Rendering.Forward.RenderManager
// Assembly: SynapseGaming-SunBurn-Pro, Version=1.3.2.8, Culture=neutral, PublicKeyToken=c23c60523565dbfd
// MVID: A5F03349-72AC-4BAA-AEEE-9AB9B77E0A39
// Assembly location: C:\Projects\BlackBox\StarDrive\SynapseGaming-SunBurn-Pro.dll

using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using ns3;
using ns9;
using SynapseGaming.LightingSystem.Core;
using SynapseGaming.LightingSystem.Effects;
using SynapseGaming.LightingSystem.Effects.Forward;
using SynapseGaming.LightingSystem.Lights;
using SynapseGaming.LightingSystem.Shadows;
using SynapseGaming.LightingSystem.Shadows.Deferred;

namespace SynapseGaming.LightingSystem.Rendering.Forward
{
  /// <summary>Provides a complete forward renderer.</summary>
  public class RenderManager : BaseRenderManager
  {
    private static Class61 class61_0 = new Class61();
    private static Class64 class64_0 = new Class64();
      private int int_4 = 1;
    private List<ISceneObject> list_2 = new List<ISceneObject>();
    private List<RenderableMesh> list_3 = new List<RenderableMesh>();
    private List<Class63> list_4 = new List<Class63>();
    private List<Class63> list_5 = new List<Class63>();
    private List<Class63> list_6 = new List<Class63>();
      private Class65 class65_0 = new Class65();
    private Class67 class67_0 = new Class67();
    private List<ILight> list_8 = new List<ILight>();
    private List<RenderableMesh> list_9 = new List<RenderableMesh>();
    private List<Class63> list_10 = new List<Class63>();
    private List<Class63> list_11 = new List<Class63>();
    private List<Class63> list_12 = new List<Class63>();
    private List<RenderableMesh> list_13 = new List<RenderableMesh>();
    private Dictionary<ISceneObject, bool> dictionary_0 = new Dictionary<ISceneObject, bool>();
    private Vector3[] vector3_0 = new Vector3[8];
    private bool[] bool_5 = new bool[6];
    private bool bool_3;
    private const bool bool_4 = false;
    private FogEffect fogEffect_0;

    /// <summary>
    /// Cleans up shimmering effects on object edges. Requires a
    /// depth buffer format that supports stencil tests. Improper
    /// depth buffer formats will disable the feature.
    /// </summary>
    public bool MultiPassEdgeCleanupEnabled { get; set; } = true;

      /// <summary>
    /// Current scene shadow maps provided by the ShadowManager and
    /// filled by this render manager (only valid between calls to
    /// BeginFrameRendering and EndFrameRendering).
    /// 
    /// See the Custom Renderer project template for an example of
    /// how to use ShadowRenderTargetGroup and the contained
    /// shadow maps to render shadows onto the scene.
    /// </summary>
    public List<ShadowRenderTargetGroup> FrameShadowRenderTargetGroups { get; } = new List<ShadowRenderTargetGroup>();

      /// <summary>Creates a new RenderManager instance.</summary>
    /// <param name="graphicsdevicemanager"></param>
    /// <param name="sceneinterface">Service provider used to access all other manager services in this scene.</param>
    public RenderManager(IGraphicsDeviceService graphicsdevicemanager, IManagerServiceProvider sceneinterface)
      : base(graphicsdevicemanager, sceneinterface)
    {
    }

    /// <summary>
    /// Builds all object batches, shadow maps, and cached information before rendering.
    /// Any object added to the RenderManager after this call will not be visible during the frame.
    /// </summary>
    /// <param name="scenestate"></param>
    public override void BeginFrameRendering(ISceneState scenestate)
    {
      LightingSystemPerformance.Begin("RenderManager.BeginFrameRendering");
      this.class65_0.method_0();
      this.class67_0.method_0();
      base.BeginFrameRendering(scenestate);
      Matrix viewToWorld = scenestate.ViewToWorld;
      GraphicsDevice graphicsDevice = this.GraphicsDeviceManager.GraphicsDevice;
      FillMode fillMode = graphicsDevice.RenderState.FillMode;
      graphicsDevice.RenderState.FillMode = FillMode.Solid;
      DepthStencilBuffer depthStencilBuffer = graphicsDevice.DepthStencilBuffer;
      this.bool_3 = depthStencilBuffer != null && depthStencilBuffer.Format == DepthFormat.Depth24Stencil8;
      if (this.fogEffect_0 == null)
        this.fogEffect_0 = new FogEffect(graphicsDevice);
      this.list_2.Clear();
      this.list_3.Clear();
      IObjectManager manager1 = (IObjectManager) this.ServiceProvider.GetManager(SceneInterface.ObjectManagerType, false);
      if (manager1 != null)
        manager1.Find(this.list_2, this.SceneState.ViewFrustum, ObjectFilter.DynamicAndStatic);
      for (int index1 = 0; index1 < this.list_2.Count; ++index1)
      {
        ISceneObject sceneObject = this.list_2[index1];
        if (sceneObject != null && sceneObject.Visible)
        {
          float num1 = Vector3.DistanceSquared(viewToWorld.Translation, sceneObject.WorldBoundingSphere.Center);
          float num2 = this.SceneState.Environment.VisibleDistance + sceneObject.WorldBoundingSphere.Radius;
          if (num1 <= num2 * (double) num2)
          {
            ++this.class57_0.lightingSystemStatistic_1.AccumulationValue;
            for (int index2 = 0; index2 < sceneObject.RenderableMeshes.Count; ++index2)
            {
              RenderableMesh renderableMesh = sceneObject.RenderableMeshes[index2];
              if (renderableMesh.effect_0 != null)
              {
                if (renderableMesh.effect_0 is BaseSasBindEffect)
                  (renderableMesh.effect_0 as BaseSasBindEffect).GameTime = scenestate.GameTime;
                if (this.MaxLoadedMipLevelEnabled)
                {
                  if (renderableMesh.effect_0 is BasicEffect)
                    this.SetTextureLOD((renderableMesh.effect_0 as BasicEffect).Texture);
                  else if (renderableMesh.effect_0 is ITextureAccessEffect)
                  {
                    ITextureAccessEffect effect0 = renderableMesh.effect_0 as ITextureAccessEffect;
                    for (int index3 = 0; index3 < effect0.TextureCount; ++index3)
                      this.SetTextureLOD(effect0.GetTexture(index3));
                  }
                }
                this.list_3.Add(renderableMesh);
              }
            }
          }
        }
      }
      this.list_3.Sort(class61_0);
      class64_0.method_0(this.SceneState.View, this.SceneState.ViewToWorld, this.SceneState.Projection, this.SceneState.ProjectionToView, this.list_4, this.list_3, Enum7.flag_0 | Enum7.flag_1 | Enum7.flag_2 | Enum7.flag_3);
      class64_0.method_0(this.SceneState.View, this.SceneState.ViewToWorld, this.SceneState.Projection, this.SceneState.ProjectionToView, this.list_5, this.list_3, Enum7.flag_0);
      class64_0.method_0(this.SceneState.View, this.SceneState.ViewToWorld, this.SceneState.Projection, this.SceneState.ProjectionToView, this.list_6, this.list_3, Enum7.flag_0 | Enum7.flag_1);
      this.FrameShadowRenderTargetGroups.Clear();
      IShadowMapManager manager2 = (IShadowMapManager) this.ServiceProvider.GetManager(SceneInterface.ShadowMapManagerType, false);
      if (manager2 == null)
      {
        this.GetDefaultShadows(this.FrameShadowRenderTargetGroups, this.FrameLights);
      }
      else
      {
        if (manager2 is DeferredShadowMapManager)
          throw new Exception("Cannot use a deferred shadow map manager with a forward render manager. Please switch to a forward shadow map manager.");
        manager2.BuildShadows(this.FrameShadowRenderTargetGroups, this.FrameLights, false);
      }
      if (this.ShadowDetail != DetailPreference.Off && manager1 != null)
      {
        graphicsDevice.RenderState.AlphaTestEnable = false;
        graphicsDevice.RenderState.AlphaBlendEnable = false;
        graphicsDevice.RenderState.SourceBlend = Blend.One;
        graphicsDevice.RenderState.DestinationBlend = Blend.Zero;
        graphicsDevice.RenderState.CullMode = CullMode.CullCounterClockwiseFace;
        graphicsDevice.RenderState.DepthBufferEnable = true;
        for (int index = 0; index < 6; ++index)
        {
          ClipPlane clipPlane = graphicsDevice.ClipPlanes[index];
          this.bool_5[index] = clipPlane.IsEnabled;
          clipPlane.IsEnabled = false;
        }
        this.BuildShadowMaps(this.FrameShadowRenderTargetGroups);
        for (int index = 0; index < 6; ++index)
          graphicsDevice.ClipPlanes[index].IsEnabled = this.bool_5[index];
        graphicsDevice.RenderState.CullMode = CullMode.CullCounterClockwiseFace;
      }
      if (this.ClearBackBufferEnabled)
      {
        Color color = new Color(scenestate.Environment.FogColor);
        ClearOptions options = ClearOptions.Target | ClearOptions.DepthBuffer;
        if (this.MultiPassEdgeCleanupEnabled && this.bool_3)
          options |= ClearOptions.Stencil;
        graphicsDevice.Clear(options, color, 1f, 0);
      }
      graphicsDevice.RenderState.FillMode = fillMode;
    }

    /// <summary>
    /// Generates shadow maps for the provided shadow render groups. Override this
    /// method to customize shadow map generation.
    /// </summary>
    /// <param name="shadowrendertargetgroups">Shadow render groups to generate shadow maps for.</param>
    protected override void BuildShadowMaps(List<ShadowRenderTargetGroup> shadowrendertargetgroups)
    {
      IObjectManager manager1 = (IObjectManager) this.ServiceProvider.GetManager(SceneInterface.ObjectManagerType, false);
      IAvatarManager manager2 = (IAvatarManager) this.ServiceProvider.GetManager(SceneInterface.AvatarManagerType, false);
      foreach (ShadowRenderTargetGroup renderTargetGroup in this.FrameShadowRenderTargetGroups)
      {
        if (renderTargetGroup.HasShadows() && !renderTargetGroup.ContentsAreValid)
        {
          ++this.class57_0.lightingSystemStatistic_10.AccumulationValue;
          renderTargetGroup.Begin();
          foreach (ShadowGroup shadowGroup in renderTargetGroup.ShadowGroups)
          {
            if (shadowGroup.Shadow is IShadowMap)
            {
              ++this.class57_0.lightingSystemStatistic_11.AccumulationValue;
              IShadowMap shadow = shadowGroup.Shadow as IShadowMap;
              this.list_9.Clear();
              ObjectFilter objectfilter = shadowGroup.ShadowSource.ShadowType != ShadowType.AllObjects ? ObjectFilter.Static : ObjectFilter.DynamicAndStatic;
              manager1.Find(this.list_9, shadowGroup.BoundingBox, objectfilter);
              this.list_9.Sort(class61_0);
              class64_0.method_1(this.list_10, this.list_9, false, bool_4);
              if (manager2 != null)
                manager2.BeginShadowGroupRendering(shadowGroup);
              Vector3 shadowPosition = shadowGroup.ShadowSource.ShadowPosition;
              for (int surface1 = 0; surface1 < shadow.Surfaces.Length; ++surface1)
              {
                ShadowMapSurface surface2 = shadow.Surfaces[surface1];
                ++this.class57_0.lightingSystemStatistic_12.AccumulationValue;
                if (shadow.IsSurfaceVisible(surface1, this.SceneState.ViewFrustum))
                {
                  shadow.BeginSurfaceRendering(surface1);
                  this.dictionary_0.Clear();
                  foreach (RenderableMesh renderableMesh in this.list_9)
                  {
                    if (renderableMesh != null)
                    {
                      ISceneObject isceneObject0 = renderableMesh.sceneObject;
                      bool flag;
                      if (this.dictionary_0.TryGetValue(isceneObject0, out flag))
                      {
                        renderableMesh.bool_0 = flag;
                      }
                      else
                      {
                        renderableMesh.bool_0 = isceneObject0.CastShadows && surface2.Frustum.Contains(isceneObject0.WorldBoundingSphere) != ContainmentType.Disjoint;
                        this.dictionary_0.Add(isceneObject0, renderableMesh.bool_0);
                      }
                    }
                  }
                  foreach (Class63 class63 in this.list_10)
                  {
                    if (class63.HasRenderableObjects)
                    {
                      EffectHelper.SyncObjectAndShadowEffects(class63.Effect, shadow.ShadowEffect);
                      if (shadow.ShadowEffect is ISkinnedEffect && class63.Objects.Skinned.Count > 0)
                      {
                        ISkinnedEffect shadowEffect = shadow.ShadowEffect as ISkinnedEffect;
                        shadowEffect.Skinned = true;
                        this.method_4(class63.Objects.Skinned, shadow.ShadowEffect, true, Enum6.const_0, class63.Transparent, true);
                        shadowEffect.Skinned = false;
                      }
                      if (class63.Objects.NonSkinned.Count > 0)
                        this.method_4(class63.Objects.NonSkinned, shadow.ShadowEffect, true, Enum6.const_0, class63.Transparent, true);
                    }
                  }
                  if (manager2 != null)
                  {
                    manager2.RenderToShadowMapSurface(shadowGroup, surface2, shadow.ShadowEffect);
                    this.class67_0.method_0();
                    this.class65_0.method_0();
                  }
                  shadow.EndSurfaceRendering();
                  ++this.class57_0.lightingSystemStatistic_13.AccumulationValue;
                }
              }
              if (manager2 != null)
                manager2.EndShadowGroupRendering(shadowGroup);
              shadow.ContentsAreValid = true;
            }
          }
          renderTargetGroup.End();
        }
        else
          renderTargetGroup.UpdateRenderTargetTexture();
      }
    }

    /// <summary>Renders the scene.</summary>
    public override void Render()
    {
      if (this.SceneState == null)
        return;
      LightingSystemPerformance.Begin("RenderManager.Render");
      GraphicsDevice graphicsDevice = this.GraphicsDeviceManager.GraphicsDevice;
      FillMode fillMode = graphicsDevice.RenderState.FillMode;
      this.class65_0.method_0();
      this.class67_0.method_0();
      graphicsDevice.RenderState.AlphaTestEnable = false;
      graphicsDevice.RenderState.AlphaBlendEnable = false;
      graphicsDevice.RenderState.SourceBlend = Blend.One;
      graphicsDevice.RenderState.DestinationBlend = Blend.Zero;
      graphicsDevice.RenderState.CullMode = CullMode.CullCounterClockwiseFace;
      graphicsDevice.RenderState.DepthBufferEnable = true;
      graphicsDevice.RenderState.FillMode = this.RenderFillMode;
      LightingSystemPerformance.Begin("RenderManager.Render (ambient)");
      this.method_2(this.list_4, this.FrameAmbientLights, false, Enum6.const_1);
      graphicsDevice.RenderState.DepthBufferWriteEnable = false;
      LightingSystemPerformance.Begin("RenderManager.Render (lighting loop)");
      foreach (ShadowRenderTargetGroup shadowRenderTargetGroup_1 in this.FrameShadowRenderTargetGroups)
      {
        foreach (ShadowGroup shadowGroup in shadowRenderTargetGroup_1.ShadowGroups)
          this.method_0(shadowRenderTargetGroup_1, shadowGroup);
      }
      if (this.SceneState.Environment.FogEnabled)
      {
        LightingSystemPerformance.Begin("RenderManager.Render (fog)");
        this.fogEffect_0.SetViewAndProjection(this.SceneState.View, this.SceneState.ViewToWorld, this.SceneState.Projection, this.SceneState.ProjectionToView);
        graphicsDevice.RenderState.AlphaBlendEnable = true;
        graphicsDevice.RenderState.SourceBlend = Blend.SourceAlpha;
        graphicsDevice.RenderState.DestinationBlend = Blend.InverseSourceAlpha;
        this.fogEffect_0.StartDistance = this.SceneState.Environment.FogStartDistance;
        this.fogEffect_0.EndDistance = this.SceneState.Environment.FogEndDistance;
        this.fogEffect_0.Color = this.SceneState.Environment.FogColor;
        class64_0.method_1(this.list_12, this.list_3, false, bool_4);
        foreach (Class63 class63 in this.list_12)
        {
          EffectHelper.SyncObjectAndShadowEffects(class63.Effect, this.fogEffect_0);
          this.fogEffect_0.Skinned = false;
          this.method_4(class63.Objects.NonSkinned, this.fogEffect_0, false, Enum6.const_0, class63.Transparent, false);
          this.fogEffect_0.Skinned = true;
          this.method_4(class63.Objects.Skinned, this.fogEffect_0, false, Enum6.const_0, class63.Transparent, false);
        }
      }
      graphicsDevice.RenderState.FillMode = fillMode;
      graphicsDevice.RenderState.AlphaBlendEnable = false;
      graphicsDevice.RenderState.SourceBlend = Blend.One;
      graphicsDevice.RenderState.DestinationBlend = Blend.Zero;
      graphicsDevice.RenderState.DepthBufferWriteEnable = true;
      graphicsDevice.RenderState.CullMode = CullMode.CullCounterClockwiseFace;
    }

    /// <summary>
    /// Finalizes rendering and cleans up frame information including removing all frame lifespan objects.
    /// </summary>
    public override void EndFrameRendering()
    {
      LightingSystemPerformance.Begin("RenderManager.EndFrameRendering");
      base.EndFrameRendering();
      ILightManager manager = (ILightManager) this.ServiceProvider.GetManager(SceneInterface.LightManagerType, false);
      if (manager != null)
        manager.RenderVolumeLights(null);
      class64_0.method_2();
    }

    /// <summary>
    /// Unloads all scene and device specific data.  Must be called
    /// when the device is reset (during Game.UnloadGraphicsContent()).
    /// </summary>
    public override void Unload()
    {
      if (this.fogEffect_0 != null)
      {
        this.fogEffect_0.Dispose();
        this.fogEffect_0 = null;
      }
      base.Unload();
    }

    private void method_0(ShadowRenderTargetGroup shadowRenderTargetGroup_1, ShadowGroup shadowGroup_0)
    {
      if (shadowGroup_0.Lights.Count < 1 || this.list_3.Count < 1)
        return;
      ++this.class57_0.lightingSystemStatistic_9.AccumulationValue;
      this.class57_0.lightingSystemStatistic_7.AccumulationValue += shadowGroup_0.Lights.Count;
      bool flag1 = shadowGroup_0.ShadowSource is IPointSource;
      GraphicsDevice graphicsDevice = this.GraphicsDeviceManager.GraphicsDevice;
      List<Class63> list_14 = flag1 ? this.list_5 : this.list_6;
      foreach (Class63 class63 in list_14)
      {
        class63.HasRenderableObjects = false;
        foreach (RenderableMesh renderableMesh in class63.Objects.All)
        {
          if (flag1 && (renderableMesh == null || shadowGroup_0.BoundingBox.Contains(renderableMesh.sceneObject.WorldBoundingSphere) == ContainmentType.Disjoint))
          {
            renderableMesh.bool_0 = false;
          }
          else
          {
            renderableMesh.bool_0 = true;
            class63.HasRenderableObjects = true;
          }
        }
      }
      bool flag2 = false;
      bool flag3 = false;
      if (flag1)
      {
        Rectangle rectangle = CoreUtils.smethod_27(shadowGroup_0.BoundingBox, graphicsDevice.Viewport, this.SceneState.ViewProjection, this.SceneState.ViewToWorld);
        if (rectangle.Width <= 0.0 || rectangle.Height <= 0.0)
          return;
        graphicsDevice.RenderState.ScissorTestEnable = true;
        graphicsDevice.ScissorRectangle = rectangle;
        flag2 = true;
      }
      if (this.ShadowDetail != DetailPreference.Off && shadowGroup_0.Shadow is IShadowMap && (shadowGroup_0.Shadow as IShadowMap).ShadowEffect is IRenderableEffect)
      {
        IShadowMap shadow = shadowGroup_0.Shadow as IShadowMap;
        this.list_11.Clear();
        class64_0.method_1(this.list_11, this.list_3, true, bool_4);
        graphicsDevice.RenderState.AlphaBlendEnable = false;
        graphicsDevice.RenderState.SourceBlend = Blend.One;
        graphicsDevice.RenderState.DestinationBlend = Blend.Zero;
        graphicsDevice.RenderState.ColorWriteChannels = ColorWriteChannels.Alpha;
        foreach (Class63 class63_0 in this.list_11)
        {
          if (class63_0.HasRenderableObjects)
          {
            EffectHelper.SyncObjectAndShadowEffects(class63_0.Effect, shadow.ShadowEffect);
            this.method_1(shadowRenderTargetGroup_1, shadowGroup_0, class63_0);
          }
        }
        graphicsDevice.RenderState.AlphaBlendEnable = true;
        graphicsDevice.RenderState.SourceBlend = Blend.DestinationAlpha;
        graphicsDevice.RenderState.DestinationBlend = Blend.One;
        graphicsDevice.RenderState.ColorWriteChannels = ColorWriteChannels.Red | ColorWriteChannels.Green | ColorWriteChannels.Blue;
        flag3 = true;
      }
      else
      {
        graphicsDevice.RenderState.AlphaBlendEnable = true;
        graphicsDevice.RenderState.SourceBlend = Blend.One;
        graphicsDevice.RenderState.DestinationBlend = Blend.One;
      }
      bool flag4;
      if ((flag4 = shadowGroup_0.Lights.Count == 1) && this.MultiPassEdgeCleanupEnabled && this.bool_3)
      {
        graphicsDevice.RenderState.StencilEnable = true;
        graphicsDevice.RenderState.StencilFunction = CompareFunction.NotEqual;
        graphicsDevice.RenderState.StencilPass = StencilOperation.Replace;
        graphicsDevice.RenderState.ReferenceStencil = this.int_4;
        graphicsDevice.RenderState.StencilDepthBufferFail = StencilOperation.Keep;
        graphicsDevice.RenderState.StencilFail = StencilOperation.Keep;
        graphicsDevice.RenderState.StencilMask = int.MaxValue;
        graphicsDevice.RenderState.StencilWriteMask = int.MaxValue;
        graphicsDevice.RenderState.TwoSidedStencilMode = false;
        ++this.int_4;
        if (this.int_4 > 250)
          this.int_4 = 1;
      }
      this.method_2(list_14, shadowGroup_0.Lights, true, Enum6.const_2);
      if (flag4 && this.MultiPassEdgeCleanupEnabled && this.bool_3)
        graphicsDevice.RenderState.StencilEnable = false;
      if (flag2)
        graphicsDevice.RenderState.ScissorTestEnable = false;
      if (!flag3)
        return;
      graphicsDevice.RenderState.ColorWriteChannels = ColorWriteChannels.All;
    }

    private void method_1(ShadowRenderTargetGroup shadowRenderTargetGroup_1, ShadowGroup shadowGroup_0, Class63 class63_0)
    {
      if (class63_0.Objects.All.Count < 1 || shadowGroup_0.Lights.Count < 1 || !(shadowGroup_0.Shadow is IShadowMap))
        return;
      GraphicsDevice graphicsDevice = this.GraphicsDeviceManager.GraphicsDevice;
      IShadowMap shadow = shadowGroup_0.Shadow as IShadowMap;
      if (!(shadow.ShadowEffect is IRenderableEffect) || !(shadow.ShadowEffect is ISkinnedEffect))
        throw new Exception("RenderShadow requires an IRenderableEffect ShadowEffect.");
      if (shadow.ShadowEffect is Class36)
        (shadow.ShadowEffect as Class36).EffectDetail = this.ShadowDetail;
      shadow.BeginRendering(shadowRenderTargetGroup_1.RenderTargetTexture);
      ISkinnedEffect shadowEffect1 = shadow.ShadowEffect as ISkinnedEffect;
      Effect shadowEffect2 = shadow.ShadowEffect;
      if (class63_0.Objects.Skinned.Count > 0)
      {
        shadowEffect1.Skinned = true;
        this.method_4(class63_0.Objects.Skinned, shadow.ShadowEffect, false, Enum6.const_2, class63_0.Transparent, false);
      }
      if (class63_0.Objects.NonSkinned.Count > 0)
      {
        shadowEffect1.Skinned = false;
        this.method_4(class63_0.Objects.NonSkinned, shadow.ShadowEffect, false, Enum6.const_2, class63_0.Transparent, false);
      }
      shadow.EndRendering();
    }

    private void method_2(List<Class63> list_14, List<ILight> list_15, bool bool_6, Enum6 enum6_0)
    {
      LightingSystemPerformance.Begin("RenderManager.RenderObjectBatches");
      foreach (Class63 class63 in list_14)
      {
        if (!bool_6 || class63.HasRenderableObjects)
        {
          if (class63.Effect is BasicEffect)
          {
            BasicEffect effect = class63.Effect as BasicEffect;
            if (effect.LightingEnabled && list_15.Count > 0)
            {
              ILight light = list_15[0];
              if (light is DirectionalLight)
              {
                IDirectionalSource directionalSource = light as IDirectionalSource;
                effect.AmbientLightColor = new Vector3();
                effect.DirectionalLight0.Enabled = true;
                effect.DirectionalLight0.DiffuseColor = light.CompositeColorAndIntensity;
                effect.DirectionalLight0.Direction = directionalSource.Direction;
                effect.DirectionalLight1.Enabled = false;
                effect.DirectionalLight2.Enabled = false;
              }
              else
              {
                if (!(light is AmbientLight))
                  throw new ArgumentException("BasicEffect can only render directional lights.");
                effect.AmbientLightColor = light.CompositeColorAndIntensity;
                effect.DirectionalLight0.Enabled = false;
                effect.DirectionalLight1.Enabled = false;
                effect.DirectionalLight2.Enabled = false;
              }
            }
          }
          else if (class63.Effect is IRenderableEffect)
            (class63.Effect as IRenderableEffect).EffectDetail = this.EffectDetail;
          if (list_15.Count > 0 && class63.Effect is ILightingEffect)
          {
            ILightingEffect effect = class63.Effect as ILightingEffect;
            if (list_15.Count > effect.MaxLightSources)
            {
              this.list_8.Clear();
              for (int index = 0; index < list_15.Count; ++index)
              {
                this.list_8.Add(list_15[index]);
                if (this.list_8.Count >= effect.MaxLightSources || index + 1 >= list_15.Count)
                {
                  effect.LightSources = this.list_8;
                  this.method_4(class63.Objects.All, class63.Effect, bool_6, enum6_0, true, false);
                  this.list_8.Clear();
                }
              }
            }
            else
            {
              effect.LightSources = list_15;
              this.method_4(class63.Objects.All, class63.Effect, bool_6, enum6_0, true, false);
            }
          }
          else
            this.method_4(class63.Objects.All, class63.Effect, bool_6, enum6_0, true, false);
        }
      }
    }

    private bool method_3(Effect effect_0)
    {
      if (effect_0 is IRenderableEffect)
        return (effect_0 as IRenderableEffect).DoubleSided;
      return false;
    }

    private void method_4(List<RenderableMesh> list_14, Effect effect_0, bool bool_6, Enum6 enum6_0, bool bool_7, bool bool_8)
    {
      if (list_14.Count < 1)
        return;
      LightingSystemPerformance.Begin("RenderManager.RenderObjectBatch");
      if (effect_0 is IDeferredObjectEffect)
      {
        string str = string.Empty;
        if (list_14[0].sceneObject != null)
          str = list_14[0].sceneObject.Name;
        throw new Exception("Forward rendering does not support deferred effects (SceneObject '" + str + "'). Make sure model processors are set to a non-deferred processor.");
      }
      if (bool_6)
      {
        bool flag = false;
        foreach (RenderableMesh renderableMesh in list_14)
        {
          if (renderableMesh.bool_0)
          {
            flag = true;
            break;
          }
        }
        if (!flag)
          return;
      }
      bool flag1 = this.method_3(effect_0);
      bool flag2 = false;
      bool flag3 = false;
      GraphicsDevice graphicsDevice = this.GraphicsDeviceManager.GraphicsDevice;
      CullMode cullMode = CullMode.CullCounterClockwiseFace;
      if (this.SceneState.InvertedWindings)
        bool_8 = !bool_8;
      graphicsDevice.RenderState.CullMode = flag1 ? CullMode.None : (!bool_8 ? cullMode : CullMode.CullClockwiseFace);
      if (effect_0 is ITransparentEffect && enum6_0 != Enum6.const_0)
      {
        if (enum6_0 == Enum6.const_1)
        {
          ITransparentEffect transparentEffect = effect_0 as ITransparentEffect;
          if (transparentEffect.TransparencyMode == TransparencyMode.Clip)
          {
            flag2 = true;
            graphicsDevice.RenderState.AlphaTestEnable = true;
            graphicsDevice.RenderState.ReferenceAlpha = (int) (transparentEffect.Transparency * (double) byte.MaxValue);
            graphicsDevice.RenderState.AlphaFunction = CompareFunction.GreaterEqual;
          }
        }
        else if (enum6_0 == Enum6.const_2)
        {
          flag3 = true;
          graphicsDevice.RenderState.DepthBufferFunction = CompareFunction.Equal;
        }
      }
      if (bool_7)
      {
        if (effect_0 is IAddressableEffect)
        {
          IAddressableEffect addressableEffect = effect_0 as IAddressableEffect;
          this.class65_0.method_1(graphicsDevice, addressableEffect.AddressModeU, addressableEffect.AddressModeV, addressableEffect.AddressModeW, this.MagFilter, this.MinFilter, this.MipFilter, this.MipMapLevelOfDetailBias);
        }
        else
          this.class65_0.method_1(graphicsDevice, TextureAddressMode.Wrap, TextureAddressMode.Wrap, TextureAddressMode.Wrap, this.MagFilter, this.MinFilter, this.MipFilter, this.MipMapLevelOfDetailBias);
      }
      EffectPassCollection passes = effect_0.CurrentTechnique.Passes;
      ++this.class57_0.lightingSystemStatistic_3.AccumulationValue;
      this.class57_0.lightingSystemStatistic_4.AccumulationValue += passes.Count;
      BasicEffect basicEffect = effect_0 as BasicEffect;
      ISkinnedEffect skinnedEffect = effect_0 as ISkinnedEffect;
      IRenderableEffect renderableEffect1 = effect_0 as IRenderableEffect;
      BaseRenderableEffect renderableEffect2 = effect_0 as BaseRenderableEffect;
      if (effect_0 is ParameteredEffect && (effect_0 as ParameteredEffect).AffectsRenderStates)
        effect_0.Begin(SaveStateMode.SaveState);
      else
        effect_0.Begin();
      for (int index = 0; index < passes.Count; ++index)
      {
        EffectPass pass = effect_0.CurrentTechnique.Passes[index];
        pass.Begin();
        foreach (RenderableMesh renderableMesh_1 in list_14)
        {
          if (renderableMesh_1 != null && (!bool_6 || renderableMesh_1.bool_0))
          {
            bool flag4 = false;
            if (basicEffect != null)
            {
              basicEffect.World = renderableMesh_1.world;
              flag4 = true;
            }
            else
            {
              if (skinnedEffect != null)
              {
                skinnedEffect.SkinBones = renderableMesh_1.sceneObject.SkinBones;
                flag4 = true;
              }
              if (renderableEffect1 != null)
              {
                renderableEffect1.SetWorldAndWorldToObject(ref renderableMesh_1.world, ref renderableMesh_1.worldTranspose, ref renderableMesh_1.worldToMesh, ref renderableMesh_1.matrix_7);
                flag4 = true;
              }
              if (renderableEffect2 != null)
              {
                flag4 = renderableEffect2.UpdatedByBatch;
                renderableEffect2.UpdatedByBatch = false;
              }
            }
            if (flag4)
            {
              effect_0.CommitChanges();
              ++this.class57_0.lightingSystemStatistic_6.AccumulationValue;
            }
            if (!flag1 && cullMode != renderableMesh_1.cullMode_0)
            {
              graphicsDevice.RenderState.CullMode = !bool_8 ? renderableMesh_1.cullMode_0 : (renderableMesh_1.cullMode_0 != CullMode.CullClockwiseFace ? CullMode.CullClockwiseFace : CullMode.CullCounterClockwiseFace);
              cullMode = renderableMesh_1.cullMode_0;
              ++this.class57_0.lightingSystemStatistic_5.AccumulationValue;
            }
            this.class67_0.method_1(graphicsDevice, renderableMesh_1);
            if (renderableMesh_1.indexBuffer == null)
              graphicsDevice.DrawPrimitives(renderableMesh_1.primitiveType_0, renderableMesh_1.elementStart, renderableMesh_1.int_5);
            else
              graphicsDevice.DrawIndexedPrimitives(renderableMesh_1.primitiveType_0, renderableMesh_1.vertexBase, 0, renderableMesh_1.vertexCount, renderableMesh_1.elementStart, renderableMesh_1.int_5);
            ++this.class57_0.lightingSystemStatistic_2.AccumulationValue;
            this.class57_0.lightingSystemStatistic_0.AccumulationValue += renderableMesh_1.int_5;
          }
        }
        pass.End();
      }
      effect_0.End();
      if (flag2)
        graphicsDevice.RenderState.AlphaTestEnable = false;
      if (flag3)
        graphicsDevice.RenderState.DepthBufferFunction = CompareFunction.LessEqual;
      if (effect_0 is ISamplerEffect && (!(effect_0 is ISamplerEffect) || !(effect_0 as ISamplerEffect).AffectsSamplerStates))
        return;
      this.class65_0.method_0();
    }

    private enum Enum6
    {
      const_0,
      const_1,
      const_2
    }
  }
}
