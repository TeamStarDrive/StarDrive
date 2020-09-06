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
using Mesh;
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
        private List<RenderableMesh> renderableMeshes = new List<RenderableMesh>();
        private List<Class63> list_4 = new List<Class63>();
        private List<Class63> list_5 = new List<Class63>();
        private List<Class63> list_6 = new List<Class63>();
        private ShaderSamplerData class65_0 = new ShaderSamplerData();
        private ShaderMeshData ShaderMesh = new ShaderMeshData();
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
        /// <param name="state"></param>
        public override void BeginFrameRendering(ISceneState state)
        {
            LightingSystemPerformance.Begin("RenderManager.BeginFrameRendering");
            class65_0.method_0();
            ShaderMesh.Clear();
            base.BeginFrameRendering(state);
            Matrix viewToWorld = state.ViewToWorld;
            GraphicsDevice graphicsDevice = GraphicsDeviceManager.GraphicsDevice;
            FillMode fillMode = graphicsDevice.RenderState.FillMode;
            graphicsDevice.RenderState.FillMode = FillMode.Solid;
            DepthStencilBuffer depthStencilBuffer = graphicsDevice.DepthStencilBuffer;
            bool_3 = depthStencilBuffer != null && depthStencilBuffer.Format == DepthFormat.Depth24Stencil8;
            if (fogEffect_0 == null)
                fogEffect_0 = new FogEffect(graphicsDevice);
            list_2.Clear();
            renderableMeshes.Clear();
            IObjectManager manager1 = (IObjectManager)ServiceProvider.GetManager(SceneInterface.ObjectManagerType, false);
            manager1?.Find(list_2, SceneState.ViewFrustum, ObjectFilter.DynamicAndStatic);
            for (int i = 0; i < list_2.Count; ++i)
            {
                ISceneObject sceneObject = list_2[i];
                if (sceneObject == null || !sceneObject.Visible) continue;
                float num1 = Vector3.DistanceSquared(viewToWorld.Translation, sceneObject.WorldBoundingSphere.Center);
                float num2 = SceneState.Environment.VisibleDistance + sceneObject.WorldBoundingSphere.Radius;
                if (!(num1 <= num2 * num2)) continue;
                ++class57_0.lightingSystemStatistic_1.AccumulationValue;
                for (int j = 0; j < sceneObject.RenderableMeshes.Count; ++j)
                {
                    RenderableMesh renderableMesh = sceneObject.RenderableMeshes[j];
                    switch (renderableMesh.effect) {
                        case null:
                            continue;
                        case BaseSasBindEffect sasBind:
                            sasBind.UpdateTime(state.ElapsedTime);
                            break;
                    }

                    if (MaxLoadedMipLevelEnabled)
                    {
                        switch (renderableMesh.effect) {
                            case BasicEffect basicEffect:
                                SetTextureLOD(basicEffect.Texture);
                                break;
                            case ITextureAccessEffect textureEffect:
                                for (int k = 0; k < textureEffect.TextureCount; ++k)
                                    SetTextureLOD(textureEffect.GetTexture(k));
                                break;
                        }
                    }
                    renderableMeshes.Add(renderableMesh);
                }
            }
            renderableMeshes.Sort(class61_0);
            class64_0.method_0(SceneState.View, SceneState.ViewToWorld, SceneState.Projection, SceneState.ProjectionToView, list_4, renderableMeshes, Enum7.flag_0 | Enum7.flag_1 | Enum7.flag_2 | Enum7.flag_3);
            class64_0.method_0(SceneState.View, SceneState.ViewToWorld, SceneState.Projection, SceneState.ProjectionToView, list_5, renderableMeshes, Enum7.flag_0);
            class64_0.method_0(SceneState.View, SceneState.ViewToWorld, SceneState.Projection, SceneState.ProjectionToView, list_6, renderableMeshes, Enum7.flag_0 | Enum7.flag_1);
            FrameShadowRenderTargetGroups.Clear();
            var manager2 = (IShadowMapManager)ServiceProvider.GetManager(SceneInterface.ShadowMapManagerType, false);
            if (manager2 == null)
            {
                GetDefaultShadows(FrameShadowRenderTargetGroups, FrameLights);
            }
            else
            {
                if (manager2 is DeferredShadowMapManager)
                    throw new Exception("Cannot use a deferred shadow map manager with a forward render manager. Please switch to a forward shadow map manager.");
                manager2.BuildShadows(FrameShadowRenderTargetGroups, FrameLights, false);
            }
            if (ShadowDetail != DetailPreference.Off && manager1 != null)
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
                    bool_5[index] = clipPlane.IsEnabled;
                    clipPlane.IsEnabled = false;
                }
                BuildShadowMaps(FrameShadowRenderTargetGroups);
                for (int index = 0; index < 6; ++index)
                    graphicsDevice.ClipPlanes[index].IsEnabled = bool_5[index];
                graphicsDevice.RenderState.CullMode = CullMode.CullCounterClockwiseFace;
            }
            if (ClearBackBufferEnabled)
            {
                Color color = new Color(state.Environment.FogColor);
                ClearOptions options = ClearOptions.Target | ClearOptions.DepthBuffer;
                if (MultiPassEdgeCleanupEnabled && bool_3)
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
            IObjectManager manager1 = (IObjectManager)ServiceProvider.GetManager(SceneInterface.ObjectManagerType, false);
            IAvatarManager manager2 = (IAvatarManager)ServiceProvider.GetManager(SceneInterface.AvatarManagerType, false);
            foreach (ShadowRenderTargetGroup renderTargetGroup in FrameShadowRenderTargetGroups)
            {
                if (renderTargetGroup.HasShadows() && !renderTargetGroup.ContentsAreValid)
                {
                    ++class57_0.lightingSystemStatistic_10.AccumulationValue;
                    renderTargetGroup.Begin();
                    foreach (ShadowGroup shadowGroup in renderTargetGroup.ShadowGroups)
                    {
                        if (shadowGroup.Shadow is IShadowMap)
                        {
                            ++class57_0.lightingSystemStatistic_11.AccumulationValue;
                            IShadowMap shadow = shadowGroup.Shadow as IShadowMap;
                            list_9.Clear();
                            ObjectFilter objectfilter = shadowGroup.ShadowSource.ShadowType != ShadowType.AllObjects ? ObjectFilter.Static : ObjectFilter.DynamicAndStatic;
                            manager1.Find(list_9, shadowGroup.BoundingBox, objectfilter);
                            list_9.Sort(class61_0);
                            class64_0.method_1(list_10, list_9, false, bool_4);
                            if (manager2 != null)
                                manager2.BeginShadowGroupRendering(shadowGroup);
                            Vector3 shadowPosition = shadowGroup.ShadowSource.ShadowPosition;
                            for (int surface1 = 0; surface1 < shadow.Surfaces.Length; ++surface1)
                            {
                                ShadowMapSurface surface2 = shadow.Surfaces[surface1];
                                ++class57_0.lightingSystemStatistic_12.AccumulationValue;
                                if (shadow.IsSurfaceVisible(surface1, SceneState.ViewFrustum))
                                {
                                    shadow.BeginSurfaceRendering(surface1);
                                    dictionary_0.Clear();
                                    foreach (RenderableMesh renderableMesh in list_9)
                                    {
                                        if (renderableMesh != null)
                                        {
                                            ISceneObject isceneObject0 = renderableMesh.sceneObject;
                                            bool flag;
                                            if (dictionary_0.TryGetValue(isceneObject0, out flag))
                                            {
                                                renderableMesh.ShadowInFrustum = flag;
                                            }
                                            else
                                            {
                                                renderableMesh.ShadowInFrustum = isceneObject0.CastShadows && surface2.Frustum.Contains(isceneObject0.WorldBoundingSphere) != ContainmentType.Disjoint;
                                                dictionary_0.Add(isceneObject0, renderableMesh.ShadowInFrustum);
                                            }
                                        }
                                    }
                                    foreach (Class63 class63 in list_10)
                                    {
                                        if (class63.HasRenderableObjects)
                                        {
                                            EffectHelper.SyncObjectAndShadowEffects(class63.Effect, shadow.ShadowEffect);
                                            if (shadow.ShadowEffect is ISkinnedEffect && class63.Objects.Skinned.Count > 0)
                                            {
                                                ISkinnedEffect shadowEffect = shadow.ShadowEffect as ISkinnedEffect;
                                                shadowEffect.Skinned = true;
                                                RenderObjectBatch(class63.Objects.Skinned, shadow.ShadowEffect, true, TransparencyMode.const_0, class63.Transparent, true);
                                                shadowEffect.Skinned = false;
                                            }
                                            if (class63.Objects.NonSkinned.Count > 0)
                                                RenderObjectBatch(class63.Objects.NonSkinned, shadow.ShadowEffect, true, TransparencyMode.const_0, class63.Transparent, true);
                                        }
                                    }
                                    if (manager2 != null)
                                    {
                                        manager2.RenderToShadowMapSurface(shadowGroup, surface2, shadow.ShadowEffect);
                                        ShaderMesh.Clear();
                                        class65_0.method_0();
                                    }
                                    shadow.EndSurfaceRendering();
                                    ++class57_0.lightingSystemStatistic_13.AccumulationValue;
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
            if (SceneState == null)
                return;
            LightingSystemPerformance.Begin("RenderManager.Render");
            GraphicsDevice graphicsDevice = GraphicsDeviceManager.GraphicsDevice;
            FillMode fillMode = graphicsDevice.RenderState.FillMode;
            class65_0.method_0();
            ShaderMesh.Clear();
            graphicsDevice.RenderState.AlphaTestEnable = false;
            graphicsDevice.RenderState.AlphaBlendEnable = false;
            graphicsDevice.RenderState.SourceBlend = Blend.One;
            graphicsDevice.RenderState.DestinationBlend = Blend.Zero;
            graphicsDevice.RenderState.CullMode = CullMode.CullCounterClockwiseFace;
            graphicsDevice.RenderState.DepthBufferEnable = true;
            graphicsDevice.RenderState.FillMode = RenderFillMode;
            LightingSystemPerformance.Begin("RenderManager.Render (ambient)");
            method_2(list_4, FrameAmbientLights, false, TransparencyMode.const_1);
            graphicsDevice.RenderState.DepthBufferWriteEnable = false;
            LightingSystemPerformance.Begin("RenderManager.Render (lighting loop)");
            foreach (ShadowRenderTargetGroup shadowRenderTargetGroup_1 in FrameShadowRenderTargetGroups)
            {
                foreach (ShadowGroup shadowGroup in shadowRenderTargetGroup_1.ShadowGroups)
                    RenderShadows(shadowRenderTargetGroup_1, shadowGroup);
            }
            if (SceneState.Environment.FogEnabled)
            {
                LightingSystemPerformance.Begin("RenderManager.Render (fog)");
                fogEffect_0.SetViewAndProjection(SceneState.View, SceneState.ViewToWorld, SceneState.Projection, SceneState.ProjectionToView);
                graphicsDevice.RenderState.AlphaBlendEnable = true;
                graphicsDevice.RenderState.SourceBlend = Blend.SourceAlpha;
                graphicsDevice.RenderState.DestinationBlend = Blend.InverseSourceAlpha;
                fogEffect_0.StartDistance = SceneState.Environment.FogStartDistance;
                fogEffect_0.EndDistance = SceneState.Environment.FogEndDistance;
                fogEffect_0.Color = SceneState.Environment.FogColor;
                class64_0.method_1(list_12, renderableMeshes, false, bool_4);
                foreach (Class63 class63 in list_12)
                {
                    EffectHelper.SyncObjectAndShadowEffects(class63.Effect, fogEffect_0);
                    fogEffect_0.Skinned = false;
                    RenderObjectBatch(class63.Objects.NonSkinned, fogEffect_0, false, TransparencyMode.const_0, class63.Transparent, false);
                    fogEffect_0.Skinned = true;
                    RenderObjectBatch(class63.Objects.Skinned, fogEffect_0, false, TransparencyMode.const_0, class63.Transparent, false);
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
            ILightManager manager = (ILightManager)ServiceProvider.GetManager(SceneInterface.LightManagerType, false);
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
            if (fogEffect_0 != null)
            {
                fogEffect_0.Dispose();
                fogEffect_0 = null;
            }
            base.Unload();
        }

        private void RenderShadows(ShadowRenderTargetGroup targetGroup, ShadowGroup shadowGroup)
        {
            if (shadowGroup.Lights.Count < 1 || renderableMeshes.Count < 1)
                return;
            ++class57_0.lightingSystemStatistic_9.AccumulationValue;
            class57_0.lightingSystemStatistic_7.AccumulationValue += shadowGroup.Lights.Count;
            bool flag1 = shadowGroup.ShadowSource is IPointSource;
            GraphicsDevice graphicsDevice = GraphicsDeviceManager.GraphicsDevice;
            List<Class63> list_14 = flag1 ? list_5 : list_6;
            foreach (Class63 class63 in list_14)
            {
                class63.HasRenderableObjects = false;
                foreach (RenderableMesh renderableMesh in class63.Objects.All)
                {
                    if (flag1 && (renderableMesh == null || shadowGroup.BoundingBox.Contains(renderableMesh.sceneObject.WorldBoundingSphere) == ContainmentType.Disjoint))
                    {
                        renderableMesh.ShadowInFrustum = false;
                    }
                    else
                    {
                        renderableMesh.ShadowInFrustum = true;
                        class63.HasRenderableObjects = true;
                    }
                }
            }
            bool flag2 = false;
            bool flag3 = false;
            if (flag1)
            {
                Rectangle rectangle = CoreUtils.smethod_27(shadowGroup.BoundingBox, graphicsDevice.Viewport, SceneState.ViewProjection, SceneState.ViewToWorld);
                if (rectangle.Width <= 0.0 || rectangle.Height <= 0.0)
                    return;
                graphicsDevice.RenderState.ScissorTestEnable = true;
                graphicsDevice.ScissorRectangle = rectangle;
                flag2 = true;
            }
            if (ShadowDetail != DetailPreference.Off && shadowGroup.Shadow is IShadowMap && (shadowGroup.Shadow as IShadowMap).ShadowEffect is IRenderableEffect)
            {
                IShadowMap shadow = shadowGroup.Shadow as IShadowMap;
                list_11.Clear();
                class64_0.method_1(list_11, renderableMeshes, true, bool_4);
                graphicsDevice.RenderState.AlphaBlendEnable = false;
                graphicsDevice.RenderState.SourceBlend = Blend.One;
                graphicsDevice.RenderState.DestinationBlend = Blend.Zero;
                graphicsDevice.RenderState.ColorWriteChannels = ColorWriteChannels.Alpha;
                foreach (Class63 class63_0 in list_11)
                {
                    if (class63_0.HasRenderableObjects)
                    {
                        EffectHelper.SyncObjectAndShadowEffects(class63_0.Effect, shadow.ShadowEffect);
                        method_1(targetGroup, shadowGroup, class63_0);
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
            if ((flag4 = shadowGroup.Lights.Count == 1) && MultiPassEdgeCleanupEnabled && bool_3)
            {
                graphicsDevice.RenderState.StencilEnable = true;
                graphicsDevice.RenderState.StencilFunction = CompareFunction.NotEqual;
                graphicsDevice.RenderState.StencilPass = StencilOperation.Replace;
                graphicsDevice.RenderState.ReferenceStencil = int_4;
                graphicsDevice.RenderState.StencilDepthBufferFail = StencilOperation.Keep;
                graphicsDevice.RenderState.StencilFail = StencilOperation.Keep;
                graphicsDevice.RenderState.StencilMask = int.MaxValue;
                graphicsDevice.RenderState.StencilWriteMask = int.MaxValue;
                graphicsDevice.RenderState.TwoSidedStencilMode = false;
                ++int_4;
                if (int_4 > 250)
                    int_4 = 1;
            }
            method_2(list_14, shadowGroup.Lights, true, TransparencyMode.const_2);
            if (flag4 && MultiPassEdgeCleanupEnabled && bool_3)
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
            GraphicsDevice graphicsDevice = GraphicsDeviceManager.GraphicsDevice;
            IShadowMap shadow = shadowGroup_0.Shadow as IShadowMap;
            if (!(shadow.ShadowEffect is IRenderableEffect) || !(shadow.ShadowEffect is ISkinnedEffect))
                throw new Exception("RenderShadow requires an IRenderableEffect ShadowEffect.");
            if (shadow.ShadowEffect is ShadowEffect effect)
                effect.EffectDetail = ShadowDetail;
            shadow.BeginRendering(shadowRenderTargetGroup_1.RenderTargetTexture);
            ISkinnedEffect shadowEffect1 = shadow.ShadowEffect as ISkinnedEffect;
            Effect shadowEffect2 = shadow.ShadowEffect;
            if (class63_0.Objects.Skinned.Count > 0)
            {
                shadowEffect1.Skinned = true;
                RenderObjectBatch(class63_0.Objects.Skinned, shadow.ShadowEffect, false, TransparencyMode.const_2, class63_0.Transparent, false);
            }
            if (class63_0.Objects.NonSkinned.Count > 0)
            {
                shadowEffect1.Skinned = false;
                RenderObjectBatch(class63_0.Objects.NonSkinned, shadow.ShadowEffect, false, TransparencyMode.const_2, class63_0.Transparent, false);
            }
            shadow.EndRendering();
        }

        private void method_2(List<Class63> list_14, List<ILight> list_15, bool bool_6, TransparencyMode enum6_0)
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
                        (class63.Effect as IRenderableEffect).EffectDetail = EffectDetail;
                    if (list_15.Count > 0 && class63.Effect is ILightingEffect)
                    {
                        ILightingEffect effect = class63.Effect as ILightingEffect;
                        if (list_15.Count > effect.MaxLightSources)
                        {
                            list_8.Clear();
                            for (int index = 0; index < list_15.Count; ++index)
                            {
                                list_8.Add(list_15[index]);
                                if (list_8.Count >= effect.MaxLightSources || index + 1 >= list_15.Count)
                                {
                                    effect.LightSources = list_8;
                                    RenderObjectBatch(class63.Objects.All, class63.Effect, bool_6, enum6_0, true, false);
                                    list_8.Clear();
                                }
                            }
                        }
                        else
                        {
                            effect.LightSources = list_15;
                            RenderObjectBatch(class63.Objects.All, class63.Effect, bool_6, enum6_0, true, false);
                        }
                    }
                    else
                        RenderObjectBatch(class63.Objects.All, class63.Effect, bool_6, enum6_0, true, false);
                }
            }
        }

        private static bool IsDoubleSided(Effect effect)
        {
            if (effect is IRenderableEffect)
                return (effect as IRenderableEffect).DoubleSided;
            return false;
        }

        private void RenderObjectBatch(List<RenderableMesh> batch, Effect effect, bool shadowsEnabled,
            TransparencyMode transparencyMode, bool transparent, bool cullCCW)
        {
            if (batch.Count < 1)
                return;
            LightingSystemPerformance.Begin("RenderManager.RenderObjectBatch");
            if (effect is IDeferredObjectEffect)
            {
                string str = string.Empty;
                if (batch[0].sceneObject != null)
                    str = batch[0].sceneObject.Name;
                throw new Exception("Forward rendering does not support deferred effects (SceneObject '" + str + "'). Make sure model processors are set to a non-deferred processor.");
            }
            if (shadowsEnabled)
            {
                bool flag = false;
                foreach (RenderableMesh renderableMesh in batch)
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
            bool doubleSided = IsDoubleSided(effect);
            bool transparencyClip = false;
            bool transparencyEqual = false;
            GraphicsDevice device = GraphicsDeviceManager.GraphicsDevice;
            CullMode cullMode = CullMode.CullCounterClockwiseFace;
            if (SceneState.InvertedWindings)
                cullCCW = !cullCCW;
            device.RenderState.CullMode = doubleSided ? CullMode.None : (!cullCCW ? cullMode : CullMode.CullClockwiseFace);
            if (effect is ITransparentEffect && transparencyMode != TransparencyMode.const_0)
            {
                if (transparencyMode == TransparencyMode.const_1)
                {
                    ITransparentEffect transparentEffect = effect as ITransparentEffect;
                    if (transparentEffect.TransparencyMode == Core.TransparencyMode.Clip)
                    {
                        transparencyClip = true;
                        device.RenderState.AlphaTestEnable = true;
                        device.RenderState.ReferenceAlpha = (int)(transparentEffect.Transparency * (double)byte.MaxValue);
                        device.RenderState.AlphaFunction = CompareFunction.GreaterEqual;
                    }
                }
                else if (transparencyMode == TransparencyMode.const_2)
                {
                    transparencyEqual = true;
                    device.RenderState.DepthBufferFunction = CompareFunction.Equal;
                }
            }
            if (transparent)
            {
                if (effect is IAddressableEffect)
                {
                    IAddressableEffect addressableEffect = effect as IAddressableEffect;
                    class65_0.method_1(device, addressableEffect.AddressModeU, addressableEffect.AddressModeV, addressableEffect.AddressModeW, MagFilter, MinFilter, MipFilter, MipMapLevelOfDetailBias);
                }
                else
                    class65_0.method_1(device, TextureAddressMode.Wrap, TextureAddressMode.Wrap, TextureAddressMode.Wrap, MagFilter, MinFilter, MipFilter, MipMapLevelOfDetailBias);
            }
            EffectPassCollection passes = effect.CurrentTechnique.Passes;
            ++class57_0.lightingSystemStatistic_3.AccumulationValue;
            class57_0.lightingSystemStatistic_4.AccumulationValue += passes.Count;
            var basicFX = effect as BasicEffect;
            var skinnedFX = effect as ISkinnedEffect;
            var renderableFX = effect as IRenderableEffect;
            var baseFX = effect as BaseRenderableEffect;
            if (effect is ParameteredEffect && (effect as ParameteredEffect).AffectsRenderStates)
                effect.Begin(SaveStateMode.SaveState);
            else
                effect.Begin();
            for (int index = 0; index < passes.Count; ++index)
            {
                EffectPass pass = effect.CurrentTechnique.Passes[index];
                pass.Begin();
                foreach (RenderableMesh mesh in batch)
                {
                    if (mesh != null && (!shadowsEnabled || mesh.ShadowInFrustum))
                    {
                        bool flag4 = false;
                        if (basicFX != null)
                        {
                            basicFX.World = mesh.world;
                            flag4 = true;
                        }
                        else
                        {
                            if (skinnedFX != null)
                            {
                                skinnedFX.SkinBones = mesh.sceneObject.SkinBones;
                                flag4 = true;
                            }
                            if (renderableFX != null)
                            {
                                renderableFX.SetWorldAndWorldToObject(ref mesh.world, ref mesh.worldTranspose, ref mesh.worldToMesh, ref mesh.worldToMeshTranspose);
                                flag4 = true;
                            }
                            if (baseFX != null)
                            {
                                flag4 = baseFX.UpdatedByBatch;
                                baseFX.UpdatedByBatch = false;
                            }
                        }
                        if (flag4)
                        {
                            effect.CommitChanges();
                            ++class57_0.lightingSystemStatistic_6.AccumulationValue;
                        }
                        if (!doubleSided && cullMode != mesh.CullMode)
                        {
                            device.RenderState.CullMode = !cullCCW 
                                ? mesh.CullMode : (mesh.CullMode != CullMode.CullClockwiseFace 
                                ? CullMode.CullClockwiseFace : CullMode.CullCounterClockwiseFace);
                            cullMode = mesh.CullMode;
                            ++class57_0.lightingSystemStatistic_5.AccumulationValue;
                        }
                        ShaderMesh.SetMeshData(device, mesh);
                        if (mesh.indexBuffer == null)
                            device.DrawPrimitives(mesh.Type, mesh.elementStart, mesh.int_5);
                        else
                            device.DrawIndexedPrimitives(mesh.Type, mesh.vertexBase, 0, mesh.vertexCount, mesh.elementStart, mesh.int_5);
                        ++class57_0.lightingSystemStatistic_2.AccumulationValue;
                        class57_0.lightingSystemStatistic_0.AccumulationValue += mesh.int_5;
                    }
                }
                pass.End();
            }
            effect.End();
            if (transparencyClip)
                device.RenderState.AlphaTestEnable = false;
            if (transparencyEqual)
                device.RenderState.DepthBufferFunction = CompareFunction.LessEqual;
            if (effect is ISamplerEffect && (!(effect is ISamplerEffect) || !(effect as ISamplerEffect).AffectsSamplerStates))
                return;
            class65_0.method_0();
        }

        private enum TransparencyMode
        {
            const_0,
            const_1,
            const_2
        }
    }
}
