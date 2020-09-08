// Decompiled with JetBrains decompiler
// Type: SynapseGaming.LightingSystem.Rendering.Deferred.DeferredRenderManager
// Assembly: SynapseGaming-SunBurn-Pro, Version=1.3.2.8, Culture=neutral, PublicKeyToken=c23c60523565dbfd
// MVID: A5F03349-72AC-4BAA-AEEE-9AB9B77E0A39
// Assembly location: C:\Projects\BlackBox\StarDrive\SynapseGaming-SunBurn-Pro.dll

using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using ns11;
using ns3;
using ns5;
using EmbeddedResources;
using ns8;
using Mesh;
using SynapseGaming.LightingSystem.Core;
using SynapseGaming.LightingSystem.Effects;
using SynapseGaming.LightingSystem.Effects.Deferred;
using SynapseGaming.LightingSystem.Lights;
using SynapseGaming.LightingSystem.Shadows;
using SynapseGaming.LightingSystem.Shadows.Forward;

namespace SynapseGaming.LightingSystem.Rendering.Deferred
{
  /// <summary>Provides a complete deferred renderer.</summary>
  public class DeferredRenderManager : BaseRenderManager
  {
    private static Class61 class61_0 = new Class61();
    private static Class64 class64_0 = new Class64();
      private Class59 class59_0 = new Class59();
    private Class59 class59_1 = new Class59();
    private List<ILight> list_2 = new List<ILight>(4);
    private bool[] bool_6 = new bool[6];
    private List<ISceneObject> list_3 = new List<ISceneObject>();
    private List<RenderableMesh> list_4 = new List<RenderableMesh>();
    private List<Class63> list_5 = new List<Class63>();
    private List<Class63> list_6 = new List<Class63>();
      private ShaderSamplerData class65_0 = new ShaderSamplerData();
    private ShaderMeshData class67_0 = new ShaderMeshData();
    private List<RenderableMesh> list_8 = new List<RenderableMesh>();
    private List<Class63> list_9 = new List<Class63>();
    private Dictionary<ISceneObject, bool> dictionary_0 = new Dictionary<ISceneObject, bool>();
    private const float float_1 = 1.2f;
      private const bool bool_5 = false;
    private BasicEffect basicEffect_0;
    private DeferredBuffers deferredBuffers_0;
    private Class38 class38_0;
    private DeferredObjectEffect deferredObjectEffect_0;
    private Class37 class37_0;
    private Texture2D texture2D_0;
    private Class10 class10_0;
    private OcclusionQueryHelper<ShadowGroup> occlusionQueryHelper_0;
    private Model model_0;

    /// <summary>
    /// Determines if the manager should throw an exception when the deferred buffers exceed the
    /// render target size. This helps detect performance issues due to mismatched deferred buffer sizes.
    /// </summary>
    public bool DetectOverSizedDeferredBuffers { get; set; } = true;

      /// <summary>
    /// Enables occlusion querying to reduce light rendering. Helps performance with complex indoor
    /// scenes using lots of lights. May hurt performance in scenes where most tested lights are visible.
    /// </summary>
    public bool OcclusionQueryEnabled { get; set; }

      /// <summary>
    /// Enables z-fill optimization to reduce bandwidth and fill rate consumption in complex
    /// scenes.  May hurt performance in high polygon scenes.
    /// </summary>
    public bool DepthFillOptimizationEnabled { get; set; } = true;

      /// <summary>
    /// Shows light rendering debug information by rendering a constant red color where shadow groups are rendered.
    /// </summary>
    public float LightGroupDebugAmount { get; set; }

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

      /// <summary>
    /// Creates a new DeferredRenderManager instance.  Automatically creates an internal LightManager and StencilShadowManager.
    /// </summary>
    /// <param name="graphicsdevicemanager"></param>
    /// <param name="sceneinterface">Service provider used to access all other manager services in this scene.</param>
    public DeferredRenderManager(IGraphicsDeviceService graphicsdevicemanager, IManagerServiceProvider sceneinterface)
      : base(graphicsdevicemanager, sceneinterface)
    {
      this.method_0();
    }

    private void method_0()
    {
      BaseLightManager.MaxLightsPerGroup = Class38.MaxLightSourcesInternal;
      this.class59_0.Channels[0] = new Class58(DeferredBufferType.LightingDiffuse, SurfaceFormat.Color);
      this.class59_0.Channels[1] = new Class58(DeferredBufferType.LightingSpecular, SurfaceFormat.Color);
      this.class59_1.Channels[0] = new Class58(DeferredBufferType.DepthAndSpecularPower, SurfaceFormat.Color);
      this.class59_1.Channels[1] = new Class58(DeferredBufferType.NormalViewSpaceAndSpecular, SurfaceFormat.Color);
    }

    /// <summary>
    /// Use to apply user quality and performance preferences to the resources managed by this object.
    /// </summary>
    /// <param name="preferences"></param>
    public override void ApplyPreferences(ILightingSystemPreferences preferences)
    {
      base.ApplyPreferences(preferences);
    }

    /// <summary>
    /// Deprecated overload which is no longer used in deferred rendering.  This is only maintained
    /// due to the requirement by IRenderableManager.  Calling this method will throw an exception.
    /// </summary>
    /// <param name="state"></param>
    public override void BeginFrameRendering(ISceneState state)
    {
      throw new Exception("This overload not compatible with deferred rendering. Instead use an overload that accepts a DeferredBuffers object.");
    }

    /// <summary>
    /// Builds all object batches, shadow maps, and cached information before rendering.
    /// Any object added to the RenderManager after this call will not be visible during the frame.
    /// </summary>
    /// <param name="state"></param>
    /// <param name="deferredbuffers">Manager containing deferred g-buffers properly sized to the current render target or viewport.</param>
    public virtual void BeginFrameRendering(ISceneState state, DeferredBuffers deferredbuffers)
    {
      GraphicsDevice graphicsDevice = this.GraphicsDeviceManager.GraphicsDevice;
      this.deferredBuffers_0 = deferredbuffers;
      if (this.class38_0 == null)
      {
        this.class38_0 = new Class38(graphicsDevice);
        this.class37_0 = new Class37(graphicsDevice);
        this.basicEffect_0 = new BasicEffect(graphicsDevice, null);
        this.texture2D_0 = LightingSystemManager.Instance.EmbeddedTexture("White");
        this.deferredObjectEffect_0 = new DeferredObjectEffect(graphicsDevice);
        this.class10_0 = new Class10(graphicsDevice);
        this.occlusionQueryHelper_0 = new OcclusionQueryHelper<ShadowGroup>(graphicsDevice);
        this.model_0 = LightingSystemManager.Instance.EmbeddedModel("FullSphere");
      }
      this.class65_0.method_0();
      this.class67_0.Clear();
      this.deferredBuffers_0.BeginFrameRendering(state);
      base.BeginFrameRendering(state);
      this.list_3.Clear();
      this.list_4.Clear();
      IObjectManager manager1 = (IObjectManager) this.ServiceProvider.GetManager(SceneInterface.ObjectManagerType, false);
      if (manager1 != null)
        manager1.Find(this.list_3, this.SceneState.ViewFrustum, ObjectFilter.DynamicAndStatic);
      for (int index1 = 0; index1 < this.list_3.Count; ++index1)
      {
        ISceneObject sceneObject = this.list_3[index1];
        if (sceneObject != null && sceneObject.Visible)
        {
          float num1 = Vector3.DistanceSquared(this.SceneState.ViewToWorld.Translation, sceneObject.WorldBoundingSphere.Center);
          float num2 = this.SceneState.Environment.VisibleDistance + sceneObject.WorldBoundingSphere.Radius;
          if (num1 <= num2 * (double) num2)
          {
            ++this.class57_0.lightingSystemStatistic_1.AccumulationValue;
            for (int index2 = 0; index2 < sceneObject.RenderableMeshes.Count; ++index2)
            {
              RenderableMesh renderableMesh = sceneObject.RenderableMeshes[index2];
              if (renderableMesh.effect != null)
              {
                if (renderableMesh.effect is BaseSasBindEffect sasBind)
                    sasBind.UpdateTime(state.ElapsedTime);
                if (this.MaxLoadedMipLevelEnabled)
                {
                  if (renderableMesh.effect is BasicEffect)
                    this.SetTextureLOD((renderableMesh.effect as BasicEffect).Texture);
                  else if (renderableMesh.effect is ITextureAccessEffect)
                  {
                    ITextureAccessEffect effect0 = renderableMesh.effect as ITextureAccessEffect;
                    for (int index3 = 0; index3 < effect0.TextureCount; ++index3)
                      this.SetTextureLOD(effect0.GetTexture(index3));
                  }
                }
                this.list_4.Add(renderableMesh);
              }
            }
          }
        }
      }
      this.list_4.Sort(class61_0);
      class64_0.method_0(this.SceneState.View, this.SceneState.ViewToWorld, this.SceneState.Projection, this.SceneState.ProjectionToView, this.list_5, this.list_4, Enum7.flag_0 | Enum7.flag_1 | Enum7.flag_2 | Enum7.flag_3);
      this.FrameShadowRenderTargetGroups.Clear();
      IShadowMapManager manager2 = (IShadowMapManager) this.ServiceProvider.GetManager(SceneInterface.ShadowMapManagerType, false);
      if (manager2 == null)
      {
        this.GetDefaultShadows(this.FrameShadowRenderTargetGroups, this.FrameLights);
      }
      else
      {
        if (manager2 is ShadowMapManager)
          throw new Exception("Cannot use a forward shadow map manager with a deferred render manager. Please switch to a deferred shadow map manager.");
        manager2.BuildShadows(this.FrameShadowRenderTargetGroups, this.FrameLights, true);
      }
      Viewport viewport = graphicsDevice.Viewport;
      RenderTarget2D renderTarget = (RenderTarget2D) graphicsDevice.GetRenderTarget(0);
      DepthStencilBuffer depthStencilBuffer = graphicsDevice.DepthStencilBuffer;
      FillMode fillMode = graphicsDevice.RenderState.FillMode;
      if (this.DetectOverSizedDeferredBuffers && (viewport.Width < this.deferredBuffers_0.Width || viewport.Height < this.deferredBuffers_0.Height))
        throw new Exception("Supplied deferred buffers are too large for final target image, this will cause performance issues. Supply properly sized buffers or disable DetectOverSizedDeferredBuffers to ignore.");
      this.deferredBuffers_0.method_1(this.class59_1);
      this.deferredBuffers_0.method_1(this.class59_0);
      RenderTarget2D deferredBuffer1 = this.deferredBuffers_0.GetDeferredBuffer(DeferredBufferType.DepthAndSpecularPower);
      RenderTarget2D deferredBuffer2 = this.deferredBuffers_0.GetDeferredBuffer(DeferredBufferType.NormalViewSpaceAndSpecular);
      RenderTarget2D deferredBuffer3 = this.deferredBuffers_0.GetDeferredBuffer(DeferredBufferType.LightingDiffuse);
      RenderTarget2D deferredBuffer4 = this.deferredBuffers_0.GetDeferredBuffer(DeferredBufferType.LightingSpecular);
      this.class65_0.method_0();
      this.class67_0.Clear();
      graphicsDevice.RenderState.AlphaTestEnable = false;
      graphicsDevice.RenderState.AlphaBlendEnable = false;
      graphicsDevice.RenderState.SourceBlend = Blend.One;
      graphicsDevice.RenderState.DestinationBlend = Blend.Zero;
      graphicsDevice.RenderState.CullMode = CullMode.CullCounterClockwiseFace;
      graphicsDevice.RenderState.DepthBufferEnable = true;
      graphicsDevice.RenderState.DepthBufferWriteEnable = true;
      graphicsDevice.RenderState.DepthBufferFunction = CompareFunction.LessEqual;
      graphicsDevice.RenderState.FillMode = this.RenderFillMode;
      graphicsDevice.DepthStencilBuffer = this.deferredBuffers_0.DepthStencilBuffer;
      graphicsDevice.SetRenderTarget(0, deferredBuffer1);
      graphicsDevice.SetRenderTarget(1, null);
      graphicsDevice.SetRenderTarget(2, null);
      graphicsDevice.SetRenderTarget(3, null);
      graphicsDevice.Clear(ClearOptions.Target | ClearOptions.DepthBuffer, Color.TransparentBlack, 1f, 0);
      if (!this.DepthFillOptimizationEnabled && !this.OcclusionQueryEnabled)
      {
        graphicsDevice.SetRenderTarget(1, deferredBuffer2);
      }
      else
      {
        class64_0.method_1(this.list_6, this.list_4, false, bool_5);
        graphicsDevice.RenderState.ColorWriteChannels = ColorWriteChannels.None;
        this.method_5(this.list_6, null, false, DeferredEffectOutput.Depth, false, true, 0);
        graphicsDevice.RenderState.DepthBufferWriteEnable = false;
        if (this.OcclusionQueryEnabled)
        {
          this.occlusionQueryHelper_0.Clear();
          foreach (ShadowRenderTargetGroup renderTargetGroup in this.FrameShadowRenderTargetGroups)
          {
            foreach (ShadowGroup shadowGroup in renderTargetGroup.ShadowGroups)
              this.occlusionQueryHelper_0.SubmitObject(shadowGroup, shadowGroup.BoundingBox);
          }
          this.occlusionQueryHelper_0.RunOcclusionQuery(this.SceneState, 1.2f);
          this.class65_0.method_0();
          this.class67_0.Clear();
        }
        graphicsDevice.RenderState.ColorWriteChannels = ColorWriteChannels.All;
        graphicsDevice.SetRenderTarget(1, deferredBuffer2);
      }
      this.method_5(this.list_5, null, false, DeferredEffectOutput.GBuffer, false, true, 0);
      graphicsDevice.RenderState.FillMode = FillMode.Solid;
      for (int index = 0; index < 6; ++index)
      {
        ClipPlane clipPlane = graphicsDevice.ClipPlanes[index];
        this.bool_6[index] = clipPlane.IsEnabled;
        clipPlane.IsEnabled = false;
      }
      if (this.ShadowDetail != DetailPreference.Off && manager1 != null)
      {
        graphicsDevice.RenderState.AlphaTestEnable = false;
        graphicsDevice.RenderState.AlphaBlendEnable = false;
        graphicsDevice.RenderState.SourceBlend = Blend.One;
        graphicsDevice.RenderState.DestinationBlend = Blend.Zero;
        graphicsDevice.RenderState.CullMode = CullMode.CullCounterClockwiseFace;
        graphicsDevice.RenderState.DepthBufferEnable = true;
        graphicsDevice.RenderState.DepthBufferWriteEnable = true;
        this.BuildShadowMaps(this.FrameShadowRenderTargetGroups);
        graphicsDevice.RenderState.CullMode = CullMode.CullCounterClockwiseFace;
      }
      graphicsDevice.RenderState.AlphaTestEnable = false;
      graphicsDevice.RenderState.AlphaBlendEnable = false;
      graphicsDevice.RenderState.SourceBlend = Blend.One;
      graphicsDevice.RenderState.DestinationBlend = Blend.Zero;
      graphicsDevice.RenderState.CullMode = CullMode.CullCounterClockwiseFace;
      graphicsDevice.RenderState.DepthBufferEnable = true;
      graphicsDevice.RenderState.DepthBufferWriteEnable = false;
      graphicsDevice.SetRenderTarget(0, deferredBuffer3);
      if (this.EffectDetail != DetailPreference.Off)
        graphicsDevice.SetRenderTarget(1, deferredBuffer4);
      else
        graphicsDevice.SetRenderTarget(1, null);
      graphicsDevice.Clear(ClearOptions.Target, Color.TransparentBlack, 1f, 0);
      this.method_1(deferredBuffer1.GetTexture(), deferredBuffer2.GetTexture());
      graphicsDevice.SetRenderTarget(0, renderTarget);
      graphicsDevice.SetRenderTarget(1, null);
      graphicsDevice.DepthStencilBuffer = depthStencilBuffer;
      graphicsDevice.Viewport = viewport;
      graphicsDevice.RenderState.FillMode = fillMode;
      graphicsDevice.RenderState.AlphaBlendEnable = false;
      graphicsDevice.RenderState.SourceBlend = Blend.One;
      graphicsDevice.RenderState.DestinationBlend = Blend.Zero;
      graphicsDevice.RenderState.CullMode = CullMode.CullCounterClockwiseFace;
      graphicsDevice.RenderState.DepthBufferWriteEnable = true;
      graphicsDevice.RenderState.DepthBufferEnable = true;
      for (int index = 0; index < 6; ++index)
        graphicsDevice.ClipPlanes[index].IsEnabled = this.bool_6[index];
      if (!this.ClearBackBufferEnabled)
        return;
      graphicsDevice.Clear(ClearOptions.Target | ClearOptions.DepthBuffer, new Color(this.SceneState.Environment.FogColor), 1f, 0);
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
            if (shadowGroup.Shadow is IShadowMap && (!this.OcclusionQueryEnabled || this.occlusionQueryHelper_0.IsObjectVisible(shadowGroup)))
            {
              IShadowMap shadow = shadowGroup.Shadow as IShadowMap;
              this.method_4(shadowGroup, shadow, manager1, manager2);
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
      GraphicsDevice graphicsDevice = this.GraphicsDeviceManager.GraphicsDevice;
      RenderTarget2D deferredBuffer1 = this.deferredBuffers_0.GetDeferredBuffer(DeferredBufferType.LightingDiffuse);
      RenderTarget2D deferredBuffer2 = this.deferredBuffers_0.GetDeferredBuffer(DeferredBufferType.LightingSpecular);
      graphicsDevice.RenderState.AlphaBlendEnable = false;
      graphicsDevice.RenderState.DepthBufferWriteEnable = true;
      graphicsDevice.RenderState.DepthBufferEnable = true;
      Texture2D texture = deferredBuffer1.GetTexture();
      Texture2D texture2D = null;
      if (this.EffectDetail != DetailPreference.Off)
        texture2D = deferredBuffer2.GetTexture();
      Vector3 down = Vector3.Down;
      ISceneState sceneState = this.SceneState;
      ISceneEnvironment environment = sceneState.Environment;
      foreach (Class63 class63 in this.list_5)
      {
        if (class63.Effect is IDeferredObjectEffect)
        {
          IDeferredObjectEffect effect = class63.Effect as IDeferredObjectEffect;
          effect.SceneLightingDiffuseMap = texture;
          effect.SceneLightingSpecularMap = texture2D;
          effect.SetAmbientLighting(this.FrameAmbientLight, down);
          effect.FogEnabled = environment.FogEnabled;
          if (environment.FogEnabled)
          {
            effect.FogStartDistance = environment.FogStartDistance;
            effect.FogEndDistance = environment.FogEndDistance;
            effect.FogColor = environment.FogColor;
          }
          if (class63.Effect is IRenderableEffect)
            (class63.Effect as IRenderableEffect).SetViewAndProjection(sceneState.View, sceneState.ViewToWorld, sceneState.Projection, sceneState.ProjectionToView);
        }
      }
      this.class65_0.method_0();
      this.class67_0.Clear();
      if (this.DepthFillOptimizationEnabled)
      {
        graphicsDevice.RenderState.ColorWriteChannels = ColorWriteChannels.None;
        this.method_5(this.list_6, null, false, DeferredEffectOutput.Depth, false, true, 0);
        graphicsDevice.RenderState.ColorWriteChannels = ColorWriteChannels.All;
        graphicsDevice.RenderState.DepthBufferWriteEnable = false;
      }
      this.method_5(this.list_5, null, false, DeferredEffectOutput.Final, false, false, 0);
      graphicsDevice.RenderState.AlphaBlendEnable = false;
      graphicsDevice.RenderState.SourceBlend = Blend.One;
      graphicsDevice.RenderState.DestinationBlend = Blend.Zero;
      graphicsDevice.RenderState.CullMode = CullMode.CullCounterClockwiseFace;
      graphicsDevice.RenderState.DepthBufferWriteEnable = true;
      graphicsDevice.RenderState.DepthBufferEnable = true;
    }

    /// <summary>
    /// Finalizes rendering and cleans up frame information including removing all frame lifespan objects.
    /// </summary>
    public override void EndFrameRendering()
    {
      base.EndFrameRendering();
      ILightManager manager = (ILightManager) this.ServiceProvider.GetManager(SceneInterface.LightManagerType, false);
      if (manager != null)
        manager.RenderVolumeLights(this.deferredBuffers_0);
      this.deferredBuffers_0.EndFrameRendering();
      class64_0.method_2();
    }

    /// <summary>
    /// Removes all scene objects and cleans up scene information.
    /// </summary>
    public override void Clear()
    {
      base.Clear();
    }

    /// <summary>
    /// Unloads all scene and device specific data.  Must be called
    /// when the device is reset (during Game.UnloadGraphicsContent()).
    /// </summary>
    public override void Unload()
    {
      base.Unload();
      this.texture2D_0 = null;
      Disposable.Free(ref this.class38_0);
      Disposable.Free(ref this.deferredObjectEffect_0);
      Disposable.Free(ref this.class37_0);
      Disposable.Free(ref this.class10_0);
      Disposable.Free(ref this.occlusionQueryHelper_0);
    }

    private void method_1(Texture2D texture2D_1, Texture2D texture2D_2)
    {
      GraphicsDevice graphicsDevice = this.GraphicsDeviceManager.GraphicsDevice;
      graphicsDevice.RenderState.AlphaBlendEnable = true;
      graphicsDevice.RenderState.SourceBlend = Blend.One;
      graphicsDevice.RenderState.DestinationBlend = Blend.One;
      graphicsDevice.RenderState.CullMode = CullMode.CullClockwiseFace;
      graphicsDevice.RenderState.DepthBufferEnable = true;
      graphicsDevice.RenderState.DepthBufferWriteEnable = false;
      graphicsDevice.RenderState.DepthBufferFunction = CompareFunction.LessEqual;
      this.class38_0.SceneDepthMap = texture2D_1;
      this.class38_0.SceneNormalSpecularMap = texture2D_2;
      this.class38_0.EffectDetail = this.EffectDetail;
      this.class38_0.ShadowDetail = this.ShadowDetail;
      this.class38_0.LightGroupDebugAmount = this.LightGroupDebugAmount;
      this.class38_0.SetViewAndProjection(this.SceneState.View, this.SceneState.ViewToWorld, this.SceneState.Projection, this.SceneState.ProjectionToView);
      this.class38_0.Begin();
      this.class38_0.CurrentTechnique.Passes[0].Begin();
      foreach (ShadowRenderTargetGroup renderTargetGroup in this.FrameShadowRenderTargetGroups)
      {
        foreach (ShadowGroup shadowGroup in renderTargetGroup.ShadowGroups)
        {
          if (shadowGroup.Lights.Count >= 1 && (!this.OcclusionQueryEnabled || this.occlusionQueryHelper_0.IsObjectVisible(shadowGroup)))
          {
            IShadowMap shadow = shadowGroup.Shadow as IShadowMap;
            IShadowSource shadowSource = shadowGroup.ShadowSource;
            bool flag1;
            bool flag2 = !(flag1 = shadowSource is IPointSource) && shadowSource is IDirectionalSource;
            if (shadow != null)
              shadow.BeginRendering(renderTargetGroup.RenderTargetTexture, this.class38_0);
            else if (flag1)
              this.class38_0.SetShadowMapAndType(null, Enum5.const_0);
            else if (flag2)
              this.class38_0.SetShadowMapAndType(null, Enum5.const_1);
            if (flag1)
            {
              if (shadowSource.ShadowRenderLightsTogether)
                this.method_2(shadowGroup, false);
              else
                this.method_3(shadowGroup);
            }
            else if (flag2)
              this.method_2(shadowGroup, true);
            if (shadow != null)
              shadow.EndRendering();
            ++this.class57_0.lightingSystemStatistic_9.AccumulationValue;
          }
        }
      }
      this.class38_0.CurrentTechnique.Passes[0].End();
      this.class38_0.End();
      graphicsDevice.RenderState.DepthBufferEnable = false;
      graphicsDevice.RenderState.DepthBufferWriteEnable = false;
      graphicsDevice.RenderState.DepthBufferFunction = CompareFunction.LessEqual;
    }

    private void method_2(ShadowGroup shadowGroup_0, bool bool_7)
    {
      GraphicsDevice graphicsDevice = this.GraphicsDeviceManager.GraphicsDevice;
      bool invertedWindings = this.SceneState.InvertedWindings;
      if (!bool_7)
      {
        this.class38_0.WorldClippingSphere = BoundingSphere.CreateFromBoundingBox(shadowGroup_0.BoundingBox);
        this.class38_0.SetWorldAndWorldToObject(this.class10_0.method_0(shadowGroup_0.BoundingBox), Matrix.Identity);
        if (CoreUtils.smethod_7(shadowGroup_0.BoundingBox, 1.2f).Intersects(this.SceneState.ViewFrustum.Near) == PlaneIntersectionType.Intersecting)
        {
          graphicsDevice.RenderState.CullMode = !invertedWindings ? CullMode.CullClockwiseFace : CullMode.CullCounterClockwiseFace;
          graphicsDevice.RenderState.DepthBufferFunction = CompareFunction.Greater;
        }
        else
        {
          graphicsDevice.RenderState.CullMode = !invertedWindings ? CullMode.CullCounterClockwiseFace : CullMode.CullClockwiseFace;
          graphicsDevice.RenderState.DepthBufferFunction = CompareFunction.Less;
        }
      }
      else
      {
        this.class38_0.SetWorldAndWorldToObject(Matrix.Identity, Matrix.Identity);
        graphicsDevice.RenderState.CullMode = CullMode.None;
        graphicsDevice.RenderState.DepthBufferFunction = CompareFunction.LessEqual;
      }
      int num1 = 0;
      while (num1 < shadowGroup_0.Lights.Count)
      {
        int num2 = Math.Min(this.class38_0.MaxLightSources, shadowGroup_0.Lights.Count - num1);
        int num3 = num1 + num2;
        this.list_2.Clear();
        for (int index = num1; index < num3; ++index)
        {
          this.list_2.Add(shadowGroup_0.Lights[index]);
          ++this.class57_0.lightingSystemStatistic_7.AccumulationValue;
        }
        num1 = num3;
        this.class38_0.LightSources = this.list_2;
        this.class38_0.CommitChanges();
        if (!bool_7)
          this.class10_0.method_1();
        else
          this.deferredBuffers_0.FullFrameQuad.Render();
        ++this.class57_0.lightingSystemStatistic_8.AccumulationValue;
      }
    }

    private void method_3(ShadowGroup shadowGroup_0)
    {
      GraphicsDevice graphicsDevice = this.GraphicsDeviceManager.GraphicsDevice;
      bool invertedWindings = this.SceneState.InvertedWindings;
      Plane near = this.SceneState.ViewFrustum.Near;
      ModelMesh mesh = this.model_0.Meshes[0];
      ModelMeshPart meshPart = mesh.MeshParts[0];
      bool flag = false;
      graphicsDevice.Indices = mesh.IndexBuffer;
      graphicsDevice.Vertices[0].SetSource(mesh.VertexBuffer, meshPart.StreamOffset, meshPart.VertexStride);
      graphicsDevice.VertexDeclaration = meshPart.VertexDeclaration;
      foreach (ILight light in shadowGroup_0.Lights)
      {
        this.list_2.Clear();
        this.list_2.Add(light);
        this.class38_0.LightSources = this.list_2;
        if (light is ISpotSource)
        {
          BoundingBox worldBoundingBox = light.WorldBoundingBox;
          BoundingSphere fromBoundingBox = BoundingSphere.CreateFromBoundingBox(worldBoundingBox);
          if (CoreUtils.smethod_7(worldBoundingBox, 1.2f).Intersects(near) == PlaneIntersectionType.Intersecting)
          {
            graphicsDevice.RenderState.CullMode = !invertedWindings ? CullMode.CullClockwiseFace : CullMode.CullCounterClockwiseFace;
            graphicsDevice.RenderState.DepthBufferFunction = CompareFunction.Greater;
          }
          else
          {
            graphicsDevice.RenderState.CullMode = !invertedWindings ? CullMode.CullCounterClockwiseFace : CullMode.CullClockwiseFace;
            graphicsDevice.RenderState.DepthBufferFunction = CompareFunction.Less;
          }
          this.class38_0.WorldClippingSphere = fromBoundingBox;
          this.class38_0.CommitChanges();
          Matrix matrix = this.class10_0.method_0(worldBoundingBox);
          Matrix identity = Matrix.Identity;
          Matrix result;
          Matrix.Transpose(ref matrix, out result);
          this.class38_0.SetWorldAndWorldToObject(ref matrix, ref result, ref identity, ref identity);
          this.class10_0.method_1();
          flag = true;
        }
        else
        {
          BoundingSphere worldBoundingSphere = light.WorldBoundingSphere;
          BoundingSphere sphere = CoreUtils.smethod_8(worldBoundingSphere, 1.2f);
          if (near.Intersects(sphere) == PlaneIntersectionType.Intersecting)
          {
            graphicsDevice.RenderState.CullMode = !invertedWindings ? CullMode.CullClockwiseFace : CullMode.CullCounterClockwiseFace;
            graphicsDevice.RenderState.DepthBufferFunction = CompareFunction.Greater;
          }
          else
          {
            graphicsDevice.RenderState.CullMode = !invertedWindings ? CullMode.CullCounterClockwiseFace : CullMode.CullClockwiseFace;
            graphicsDevice.RenderState.DepthBufferFunction = CompareFunction.Less;
          }
          this.class38_0.WorldClippingSphere = worldBoundingSphere;
          this.class38_0.CommitChanges();
          Matrix matrix = Matrix.CreateScale(worldBoundingSphere.Radius) * Matrix.CreateTranslation(worldBoundingSphere.Center);
          Matrix identity = Matrix.Identity;
          Matrix result;
          Matrix.Transpose(ref matrix, out result);
          this.class38_0.SetWorldAndWorldToObject(ref matrix, ref result, ref identity, ref identity);
          if (flag)
          {
            graphicsDevice.Indices = mesh.IndexBuffer;
            graphicsDevice.Vertices[0].SetSource(mesh.VertexBuffer, meshPart.StreamOffset, meshPart.VertexStride);
            graphicsDevice.VertexDeclaration = meshPart.VertexDeclaration;
            flag = false;
          }
          graphicsDevice.DrawIndexedPrimitives(PrimitiveType.TriangleList, meshPart.BaseVertex, 0, meshPart.NumVertices, meshPart.StartIndex, meshPart.PrimitiveCount);
        }
        ++this.class57_0.lightingSystemStatistic_7.AccumulationValue;
        ++this.class57_0.lightingSystemStatistic_8.AccumulationValue;
      }
    }

    private void method_4(ShadowGroup shadowGroup_0, IShadowMap ishadowMap_0, IObjectManager iobjectManager_0, IAvatarManager iavatarManager_0)
    {
      ++this.class57_0.lightingSystemStatistic_11.AccumulationValue;
      this.list_8.Clear();
      ObjectFilter objectfilter = shadowGroup_0.ShadowSource.ShadowType != ShadowType.AllObjects ? ObjectFilter.Static : ObjectFilter.DynamicAndStatic;
      iobjectManager_0.Find(this.list_8, shadowGroup_0.BoundingBox, objectfilter);
      this.list_8.Sort(class61_0);
      class64_0.method_1(this.list_9, this.list_8, false, bool_5);
      if (iavatarManager_0 != null)
        iavatarManager_0.BeginShadowGroupRendering(shadowGroup_0);
      Vector3 shadowPosition = shadowGroup_0.ShadowSource.ShadowPosition;
      this.class65_0.method_0();
      this.deferredObjectEffect_0.EffectDetail = this.EffectDetail;
      this.deferredObjectEffect_0.ShadowArea = shadowGroup_0.BoundingSphereCentered;
      for (int surface1 = 0; surface1 < ishadowMap_0.Surfaces.Length; ++surface1)
      {
        ShadowMapSurface surface2 = ishadowMap_0.Surfaces[surface1];
        ++this.class57_0.lightingSystemStatistic_12.AccumulationValue;
        if (ishadowMap_0.IsSurfaceVisible(surface1, this.SceneState.ViewFrustum))
        {
          ishadowMap_0.BeginSurfaceRendering(surface1, this.deferredObjectEffect_0);
          this.dictionary_0.Clear();
          foreach (RenderableMesh renderableMesh in this.list_8)
          {
            if (renderableMesh != null)
            {
              ISceneObject isceneObject0 = renderableMesh.sceneObject;
              bool flag;
              if (this.dictionary_0.TryGetValue(isceneObject0, out flag))
              {
                renderableMesh.ShadowInFrustum = flag;
              }
              else
              {
                renderableMesh.ShadowInFrustum = isceneObject0.CastShadows && surface2.Frustum.Contains(isceneObject0.WorldBoundingSphere) != ContainmentType.Disjoint;
                this.dictionary_0.Add(isceneObject0, renderableMesh.ShadowInFrustum);
              }
            }
          }
          foreach (Class63 class63 in this.list_9)
          {
            if (class63.HasRenderableObjects)
            {
              ISkinnedEffect skinnedEffect = null;
              if (!class63.CustomShadowGeneration)
              {
                EffectHelper.SyncObjectAndShadowEffects(class63.Effect, this.deferredObjectEffect_0);
                skinnedEffect = this.deferredObjectEffect_0;
              }
              else if (class63.Effect is IRenderableEffect && class63.Effect is IShadowGenerateEffect)
              {
                IRenderableEffect effect1 = class63.Effect as IRenderableEffect;
                effect1.View = surface2.WorldToSurfaceView;
                effect1.Projection = surface2.Projection;
                effect1.EffectDetail = this.EffectDetail;
                IShadowGenerateEffect effect2 = class63.Effect as IShadowGenerateEffect;
                effect2.ShadowPrimaryBias = shadowGroup_0.ShadowSource.ShadowPrimaryBias;
                effect2.ShadowSecondaryBias = shadowGroup_0.ShadowSource.ShadowSecondaryBias;
                effect2.ShadowArea = shadowGroup_0.BoundingSphereCentered;
                effect2.SetCameraView(this.SceneState.View, this.SceneState.ViewToWorld);
                if (class63.Effect is BaseSasEffect sasBind)
                    sasBind.UpdateTime(SceneState.ElapsedTime);
                if (class63.Effect is ISkinnedEffect)
                  skinnedEffect = class63.Effect as ISkinnedEffect;
              }
              else
                continue;
              if (skinnedEffect != null)
              {
                Effect effect_0 = skinnedEffect as Effect;
                if (class63.Objects.Skinned.Count > 0)
                {
                  skinnedEffect.Skinned = true;
                  this.method_7(class63.Objects.Skinned, effect_0, true, DeferredEffectOutput.ShadowDepth, class63.Transparent, true, false, 0);
                  skinnedEffect.Skinned = false;
                }
                if (class63.Objects.NonSkinned.Count > 0)
                  this.method_7(class63.Objects.NonSkinned, effect_0, true, DeferredEffectOutput.ShadowDepth, class63.Transparent, true, false, 0);
              }
              else if (class63.Objects.NonSkinned.Count > 0)
                this.method_7(class63.Objects.NonSkinned, class63.Effect, true, DeferredEffectOutput.ShadowDepth, class63.Transparent, true, false, 0);
            }
          }
          if (iavatarManager_0 != null)
          {
            iavatarManager_0.RenderToShadowMapSurface(shadowGroup_0, surface2, this.deferredObjectEffect_0);
            this.class67_0.Clear();
            this.class65_0.method_0();
          }
          ishadowMap_0.EndSurfaceRendering();
          ++this.class57_0.lightingSystemStatistic_13.AccumulationValue;
        }
      }
      if (iavatarManager_0 == null)
        return;
      iavatarManager_0.EndShadowGroupRendering(shadowGroup_0);
    }

    private void method_5(List<Class63> list_10, ShadowGroup shadowGroup_0, bool bool_7, DeferredEffectOutput deferredEffectOutput_0, bool bool_8, bool bool_9, int int_4)
    {
      foreach (Class63 class63 in list_10)
      {
        if (!bool_7 || class63.HasRenderableObjects)
        {
          if (class63.Effect is IRenderableEffect)
            (class63.Effect as IRenderableEffect).EffectDetail = this.EffectDetail;
          if (shadowGroup_0 != null && class63.Effect is IShadowGenerateEffect)
          {
            IShadowGenerateEffect effect = class63.Effect as IShadowGenerateEffect;
            effect.ShadowPrimaryBias = shadowGroup_0.ShadowSource.ShadowPrimaryBias;
            effect.ShadowSecondaryBias = shadowGroup_0.ShadowSource.ShadowSecondaryBias;
            effect.ShadowArea = shadowGroup_0.BoundingSphereCentered;
          }
          bool bool_8_1 = deferredEffectOutput_0 != DeferredEffectOutput.ShadowDepth || !(class63.Effect is ITransparentEffect) || (class63.Effect as ITransparentEffect).TransparencyMode == TransparencyMode.Clip;
          this.method_7(class63.Objects.All, class63.Effect, bool_7, deferredEffectOutput_0, bool_8_1, bool_8, bool_9, int_4);
        }
      }
    }

    private bool method_6(Effect effect_0)
    {
      if (effect_0 is IRenderableEffect)
        return (effect_0 as IRenderableEffect).DoubleSided;
      return false;
    }

    private void method_7(List<RenderableMesh> list_10, Effect effect_0, bool bool_7, DeferredEffectOutput deferredEffectOutput_0, bool bool_8, bool bool_9, bool bool_10, int int_4)
    {
      if (list_10.Count < 1)
        return;
      if (effect_0 is IDeferredObjectEffect)
      {
        (effect_0 as IDeferredObjectEffect).DeferredEffectOutput = deferredEffectOutput_0;
        EffectPassCollection passes = effect_0.CurrentTechnique.Passes;
        if (bool_10 && passes.Count <= int_4)
          return;
        if (bool_7)
        {
          bool flag = false;
          foreach (RenderableMesh renderableMesh in list_10)
          {
            if (renderableMesh.ShadowInFrustum)
            {
              flag = true;
              break;
            }
          }
          if (!flag)
            return;
        }
        bool flag1 = this.method_6(effect_0);
        GraphicsDevice graphicsDevice = this.GraphicsDeviceManager.GraphicsDevice;
        CullMode cullMode = CullMode.CullCounterClockwiseFace;
        if (this.SceneState.InvertedWindings)
          bool_9 = !bool_9;
        graphicsDevice.RenderState.CullMode = flag1 ? CullMode.None : (!bool_9 ? cullMode : CullMode.CullClockwiseFace);
        if (bool_8)
        {
          if (effect_0 is IAddressableEffect)
          {
            IAddressableEffect addressableEffect = effect_0 as IAddressableEffect;
            this.class65_0.method_1(graphicsDevice, addressableEffect.AddressModeU, addressableEffect.AddressModeV, addressableEffect.AddressModeW, this.MagFilter, this.MinFilter, this.MipFilter, this.MipMapLevelOfDetailBias);
          }
          else
            this.class65_0.method_1(graphicsDevice, TextureAddressMode.Wrap, TextureAddressMode.Wrap, TextureAddressMode.Wrap, this.MagFilter, this.MinFilter, this.MipFilter, this.MipMapLevelOfDetailBias);
        }
        ++this.class57_0.lightingSystemStatistic_3.AccumulationValue;
        this.class57_0.lightingSystemStatistic_4.AccumulationValue += passes.Count;
        ISkinnedEffect skinnedEffect = effect_0 as ISkinnedEffect;
        IRenderableEffect renderableEffect1 = effect_0 as IRenderableEffect;
        BaseRenderableEffect renderableEffect2 = effect_0 as BaseRenderableEffect;
        if (effect_0 is ParameteredEffect && (effect_0 as ParameteredEffect).AffectsRenderStates)
          effect_0.Begin(SaveStateMode.SaveState);
        else
          effect_0.Begin();
        int num = 0;
        if (bool_10)
          num = int_4;
        for (int index = num; index < passes.Count; ++index)
        {
          EffectPass effectPass = passes[index];
          effectPass.Begin();
          foreach (RenderableMesh renderableMesh_1 in list_10)
          {
            if (renderableMesh_1 != null && (!bool_7 || renderableMesh_1.ShadowInFrustum))
            {
              bool flag2 = false;
              if (skinnedEffect != null)
              {
                skinnedEffect.SkinBones = renderableMesh_1.sceneObject.SkinBones;
                flag2 = true;
              }
              if (renderableEffect1 != null)
              {
                renderableEffect1.SetWorldAndWorldToObject(ref renderableMesh_1.world, ref renderableMesh_1.worldTranspose, ref renderableMesh_1.worldToMesh, ref renderableMesh_1.worldToMeshTranspose);
                flag2 = true;
              }
              if (renderableEffect2 != null)
              {
                flag2 = renderableEffect2.UpdatedByBatch;
                renderableEffect2.UpdatedByBatch = false;
              }
              if (flag2)
              {
                effect_0.CommitChanges();
                ++this.class57_0.lightingSystemStatistic_6.AccumulationValue;
              }
              if (!flag1 && cullMode != renderableMesh_1.CullMode)
              {
                graphicsDevice.RenderState.CullMode = !bool_9 ? renderableMesh_1.CullMode : (renderableMesh_1.CullMode != CullMode.CullClockwiseFace ? CullMode.CullClockwiseFace : CullMode.CullCounterClockwiseFace);
                cullMode = renderableMesh_1.CullMode;
                ++this.class57_0.lightingSystemStatistic_5.AccumulationValue;
              }
              this.class67_0.SetMeshData(graphicsDevice, renderableMesh_1);
              if (renderableMesh_1.indexBuffer == null)
                graphicsDevice.DrawPrimitives(renderableMesh_1.Type, renderableMesh_1.elementStart, renderableMesh_1.int_5);
              else
                graphicsDevice.DrawIndexedPrimitives(renderableMesh_1.Type, renderableMesh_1.vertexBase, 0, renderableMesh_1.vertexCount, renderableMesh_1.elementStart, renderableMesh_1.int_5);
              ++this.class57_0.lightingSystemStatistic_2.AccumulationValue;
              this.class57_0.lightingSystemStatistic_0.AccumulationValue += renderableMesh_1.int_5;
            }
          }
          effectPass.End();
          if (bool_10)
            break;
        }
        effect_0.End();
        if (effect_0 is ISamplerEffect && (!(effect_0 is ISamplerEffect) || !(effect_0 as ISamplerEffect).AffectsSamplerStates))
          return;
        this.class65_0.method_0();
      }
      else
      {
        string str = string.Empty;
        if (list_10[0].sceneObject != null)
          str = list_10[0].sceneObject.Name;
        throw new Exception("Deferred rendering does not support non-deferred effects (SceneObject '" + str + "'). Make sure effects derive from IDeferredObjectEffect or model processors are set to a deferred processor.");
      }
    }
  }
}
