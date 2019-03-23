// Decompiled with JetBrains decompiler
// Type: SynapseGaming.LightingSystem.Rendering.AvatarManager
// Assembly: SynapseGaming-SunBurn-Pro, Version=1.3.2.8, Culture=neutral, PublicKeyToken=c23c60523565dbfd
// MVID: A5F03349-72AC-4BAA-AEEE-9AB9B77E0A39
// Assembly location: C:\Projects\BlackBox\StarDrive\SynapseGaming-SunBurn-Pro.dll

using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.GamerServices;
using Microsoft.Xna.Framework.Graphics;
using ns11;
using ns3;
using SynapseGaming.LightingSystem.Core;
using SynapseGaming.LightingSystem.Effects;
using SynapseGaming.LightingSystem.Effects.Forward;
using SynapseGaming.LightingSystem.Lights;
using SynapseGaming.LightingSystem.Shadows;

namespace SynapseGaming.LightingSystem.Rendering
{
  /// <summary>
  /// Manages scene avatars and provides support for rendering
  /// and finding avatars by bounding volume.
  /// </summary>
  public class AvatarManager : IQuery<IAvatar>, ISubmit<IAvatar>, IUnloadable, IManager, IRenderableManager, IManagerService, IShadowRenderer, IAvatarManager
  {
      private List<IAvatar> list_0 = new List<IAvatar>(16);
    private List<IAvatar> list_1 = new List<IAvatar>(16);
    private List<IAvatar> list_2 = new List<IAvatar>(16);
    private List<IAvatar> list_3 = new List<IAvatar>(16);
    private Class56 class56_0 = new Class56();
    private ISceneState isceneState_0;
    private IManagerServiceProvider imanagerServiceProvider_0;
      private FogEffect fogEffect_0;
    private Class10 class10_0;

    /// <summary>
    /// Gets the manager specific Type used as a unique key for storing and
    /// requesting the manager from the IManagerServiceProvider.
    /// </summary>
    public Type ManagerType => SceneInterface.AvatarManagerType;

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
    public int ManagerProcessOrder { get; set; } = 70;

      /// <summary>
    /// The current GraphicsDeviceManager used by this object.
    /// </summary>
    public IGraphicsDeviceService GraphicsDeviceManager { get; }

      /// <summary>
    /// Controls avatar lighting by blending between approximate directional
    /// and ambient lighting.  A blending value of 0.0f makes avatar lighting
    /// highly directional, while a value of 1.0f makes avatar lighting highly
    /// ambient.
    /// </summary>
    public float AmbientBlend { get; set; } = 0.55f;

      /// <summary>
    /// Controls avatar lighting intensity, providing a means to tune avatar
    /// lighting to the rest of the scene. An intensity of 1.0f keeps
    /// avatar lighting the same, a value of 0.5f halves the lighting
    /// intensity, while 2.0f doubles it.
    /// </summary>
    public float LightingIntensity { get; set; } = 1f;

      /// <summary>Creates a new AvatarManager instance.</summary>
    /// <param name="graphicsdevicemanager"></param>
    /// <param name="sceneinterface">Service provider used to access all other manager services in this scene.</param>
    public AvatarManager(IGraphicsDeviceService graphicsdevicemanager, IManagerServiceProvider sceneinterface)
    {
      this.GraphicsDeviceManager = graphicsdevicemanager;
      this.imanagerServiceProvider_0 = sceneinterface;
    }

    /// <summary>
    /// Adds an object to the container. This does not transfer ownership, disposable
    /// objects should be maintained and disposed separately.
    /// </summary>
    /// <param name="avatar"></param>
    public void Submit(IAvatar avatar)
    {
      this.list_0.Add(avatar);
      ++this.class56_0.lightingSystemStatistic_0.AccumulationValue;
    }

    /// <summary>
    /// Repositions an object within the container. This method is used when a static object
    /// moves to reposition it in the storage tree / scenegraph.
    /// </summary>
    /// <param name="avatar"></param>
    public virtual void Move(IAvatar avatar)
    {
    }

    /// <summary>
    /// Auto-detects moved dynamic objects and repositions them in the storage tree / scenegraph.
    /// </summary>
    public void MoveDynamicObjects()
    {
    }

    /// <summary>Removes an object from the container.</summary>
    /// <param name="avatar"></param>
    public virtual void Remove(IAvatar avatar)
    {
      this.list_0.Remove(avatar);
      ++this.class56_0.lightingSystemStatistic_1.AccumulationValue;
    }

    /// <summary>
    /// Finds all contained objects that match a set of filter attributes
    /// and overlap with or are contained in a bounding area.
    /// </summary>
    /// <param name="foundavatars">List used to store found objects during the query.</param>
    /// <param name="worldbounds">Bounding area used to limit query results.</param>
    /// <param name="objectfilter">Filter used to limit query results to objects with specific attributes.</param>
    public void Find(List<IAvatar> foundavatars, BoundingFrustum worldbounds, ObjectFilter objectfilter)
    {
      int count = foundavatars.Count;
      foreach (IAvatar avatar in this.list_0)
      {
        if (worldbounds.Contains(avatar.WorldBoundingBox) != ContainmentType.Disjoint)
          foundavatars.Add(avatar);
      }
      this.class56_0.lightingSystemStatistic_2.AccumulationValue += foundavatars.Count - count;
    }

    /// <summary>
    /// Finds all contained objects that match a set of filter attributes
    /// and overlap with or are contained in a bounding area.
    /// </summary>
    /// <param name="foundavatars">List used to store found objects during the query.</param>
    /// <param name="worldbounds">Bounding area used to limit query results.</param>
    /// <param name="objectfilter">Filter used to limit query results to objects with specific attributes.</param>
    public void Find(List<IAvatar> foundavatars, BoundingBox worldbounds, ObjectFilter objectfilter)
    {
      int count = foundavatars.Count;
      foreach (IAvatar avatar in this.list_0)
      {
        if (worldbounds.Contains(avatar.WorldBoundingBox) != ContainmentType.Disjoint)
          foundavatars.Add(avatar);
      }
      this.class56_0.lightingSystemStatistic_2.AccumulationValue += foundavatars.Count - count;
    }

    /// <summary>
    /// Finds all contained objects that match a set of filter attributes.
    /// </summary>
    /// <param name="foundavatars">List used to store found objects during the query.</param>
    /// <param name="objectfilter">Filter used to limit query results to objects with specific attributes.</param>
    public void Find(List<IAvatar> foundavatars, ObjectFilter objectfilter)
    {
      int count = foundavatars.Count;
      foreach (IAvatar avatar in this.list_0)
        foundavatars.Add(avatar);
      this.class56_0.lightingSystemStatistic_2.AccumulationValue += foundavatars.Count - count;
    }

    /// <summary>
    /// Quickly finds all objects near a bounding area without the overhead of
    /// filtering by object type, checking if objects are enabled, or verifying
    /// containment within the bounds.
    /// </summary>
    /// <param name="foundavatars">List used to store found objects during the query.</param>
    /// <param name="worldbounds">Bounding area used to limit query results.</param>
    public void FindFast(List<IAvatar> foundavatars, BoundingBox worldbounds)
    {
      this.Find(foundavatars, worldbounds, ObjectFilter.All);
    }

    /// <summary>
    /// Quickly finds all objects without the overhead of filtering by object
    /// type or checking if objects are enabled.
    /// </summary>
    /// <param name="foundavatars">List used to store found objects during the query.</param>
    public void FindFast(List<IAvatar> foundavatars)
    {
      this.Find(foundavatars, ObjectFilter.All);
    }

    /// <summary>Prepares for shadow map rendering.</summary>
    /// <param name="shadowgroup"></param>
    public void BeginShadowGroupRendering(ShadowGroup shadowgroup)
    {
      this.list_3.Clear();
      this.list_2.Clear();
      ObjectFilter objectfilter = ObjectFilter.Static;
      if (shadowgroup.ShadowSource.ShadowType == ShadowType.AllObjects)
        objectfilter |= ObjectFilter.Dynamic;
      this.Find(this.list_3, shadowgroup.BoundingBox, objectfilter);
      for (int index = 0; index < this.list_3.Count; ++index)
      {
        IAvatar avatar = this.list_3[index];
        if (avatar.CastShadows && avatar.Renderer.IsLoaded)
          this.list_2.Add(avatar);
      }
    }

    /// <summary>Finalizes shadow map rendering.</summary>
    /// <param name="shadowgroup"></param>
    public void EndShadowGroupRendering(ShadowGroup shadowgroup)
    {
    }

    /// <summary>Performs shadow map rendering.</summary>
    /// <param name="shadowgroup"></param>
    /// <param name="surface"></param>
    /// <param name="shadoweffect"></param>
    public void RenderToShadowMapSurface(ShadowGroup shadowgroup, ShadowMapSurface surface, Effect shadoweffect)
    {
      this.list_3.Clear();
      foreach (IAvatar avatar in this.list_2)
      {
        if (surface.Frustum.Contains(avatar.WorldBoundingSphere) != ContainmentType.Disjoint)
          this.list_3.Add(avatar);
      }
      if (this.list_3.Count < 1 || !(shadoweffect is IRenderableEffect) || !(shadoweffect is ISkinnedEffect))
        return;
      GraphicsDevice graphicsDevice = this.GraphicsDeviceManager.GraphicsDevice;
      IRenderableEffect renderableEffect = shadoweffect as IRenderableEffect;
      ISkinnedEffect skinnedEffect = shadoweffect as ISkinnedEffect;
      if (this.class10_0 == null)
        this.class10_0 = new Class10(graphicsDevice);
      skinnedEffect.Skinned = false;
      EffectHelper.SyncObjectAndShadowEffects(this.class10_0.DefaultEffect, shadoweffect);
      graphicsDevice.RenderState.StencilEnable = true;
      graphicsDevice.RenderState.ReferenceStencil = 0;
      graphicsDevice.RenderState.StencilFail = StencilOperation.Keep;
      graphicsDevice.RenderState.StencilDepthBufferFail = StencilOperation.Keep;
      graphicsDevice.RenderState.StencilMask = int.MaxValue;
      graphicsDevice.RenderState.StencilWriteMask = int.MaxValue;
      int num = 1;
      foreach (IAvatar avatar in this.list_3)
      {
        if (avatar.CastShadows && avatar.Renderer.IsLoaded)
        {
          AvatarRenderer renderer = avatar.Renderer;
          graphicsDevice.RenderState.ReferenceStencil = num++;
          graphicsDevice.RenderState.StencilPass = StencilOperation.Replace;
          graphicsDevice.RenderState.StencilFunction = CompareFunction.Always;
          graphicsDevice.RenderState.DepthBufferWriteEnable = false;
          graphicsDevice.RenderState.ColorWriteChannels = ColorWriteChannels.None;
          renderer.World = avatar.World;
          renderer.View = surface.WorldToSurfaceView;
          renderer.Projection = surface.Projection;
          renderer.Draw(avatar.SkinBones, avatar.Expression);
          ++this.class56_0.lightingSystemStatistic_3.AccumulationValue;
          graphicsDevice.RenderState.StencilPass = StencilOperation.Keep;
          graphicsDevice.RenderState.StencilFunction = CompareFunction.Equal;
          graphicsDevice.RenderState.DepthBufferWriteEnable = true;
          graphicsDevice.RenderState.ColorWriteChannels = ColorWriteChannels.All;
          graphicsDevice.RenderState.CullMode = CullMode.None;
          renderableEffect.World = this.class10_0.method_0(avatar.WorldBoundingBoxProxy);
          skinnedEffect.SkinBones = null;
          shadoweffect.Begin();
          shadoweffect.CurrentTechnique.Passes[0].Begin();
          this.class10_0.method_1();
          shadoweffect.CurrentTechnique.Passes[0].End();
          shadoweffect.End();
          ++this.class56_0.lightingSystemStatistic_4.AccumulationValue;
          graphicsDevice.RenderState.CullMode = CullMode.CullCounterClockwiseFace;
        }
      }
      graphicsDevice.RenderState.StencilEnable = false;
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
      this.list_1.Clear();
      this.Find(this.list_1, state.ViewFrustum, ObjectFilter.All);
    }

    /// <summary>Finalizes rendering.</summary>
    public void EndFrameRendering()
    {
      if (this.list_1.Count < 1)
        return;
      GraphicsDevice graphicsDevice = this.GraphicsDeviceManager.GraphicsDevice;
      ILightManager manager = (ILightManager) this.imanagerServiceProvider_0.GetManager(SceneInterface.LightManagerType, false);
      bool fogEnabled;
      if (fogEnabled = this.isceneState_0.Environment.FogEnabled)
      {
        DepthStencilBuffer depthStencilBuffer = graphicsDevice.DepthStencilBuffer;
        if (depthStencilBuffer == null || depthStencilBuffer.Format == DepthFormat.Depth16 || (depthStencilBuffer.Format == DepthFormat.Depth24 || depthStencilBuffer.Format == DepthFormat.Depth32) || depthStencilBuffer.Format == DepthFormat.Unknown)
          throw new Exception("Invalid depth buffer format. Stencil buffer required for advanced avatar rendering.");
        if (this.fogEffect_0 == null)
          this.fogEffect_0 = new FogEffect(graphicsDevice);
        if (this.class10_0 == null)
          this.class10_0 = new Class10(graphicsDevice);
        graphicsDevice.Clear(ClearOptions.Stencil, Color.Black, 0.0f, 0);
        graphicsDevice.RenderState.StencilEnable = true;
        graphicsDevice.RenderState.ReferenceStencil = 0;
        graphicsDevice.RenderState.StencilFail = StencilOperation.Keep;
        graphicsDevice.RenderState.StencilDepthBufferFail = StencilOperation.Keep;
        graphicsDevice.RenderState.StencilMask = int.MaxValue;
        graphicsDevice.RenderState.StencilWriteMask = int.MaxValue;
        this.fogEffect_0.Color = this.isceneState_0.Environment.FogColor;
        this.fogEffect_0.StartDistance = this.isceneState_0.Environment.FogStartDistance;
        this.fogEffect_0.EndDistance = this.isceneState_0.Environment.FogEndDistance;
      }
      int num1 = 1;
      float visibleDistance = this.isceneState_0.Environment.VisibleDistance;
      float fogStartDistance = this.isceneState_0.Environment.FogStartDistance;
      Vector3 translation = this.isceneState_0.ViewToWorld.Translation;
      foreach (IAvatar avatar in this.list_1)
      {
        if (avatar.Visible)
        {
          BoundingSphere worldBoundingSphere = avatar.WorldBoundingSphere;
          float num2 = visibleDistance + worldBoundingSphere.Radius;
          float result;
          Vector3.DistanceSquared(ref translation, ref worldBoundingSphere.Center, out result);
          if (result <= num2 * (double) num2)
          {
            bool flag = false;
            if (fogEnabled)
            {
              float num3 = fogStartDistance - worldBoundingSphere.Radius;
              flag = result > num3 * (double) num3;
            }
            if (fogEnabled)
            {
              graphicsDevice.RenderState.ReferenceStencil = num1++;
              graphicsDevice.RenderState.StencilPass = StencilOperation.Replace;
              graphicsDevice.RenderState.StencilFunction = CompareFunction.Always;
            }
            AvatarRenderer renderer = avatar.Renderer;
            renderer.World = avatar.World;
            renderer.View = this.isceneState_0.View;
            renderer.Projection = this.isceneState_0.Projection;
            if (manager != null)
            {
              CompositeLighting compositeLighting = manager.GetCompositeLighting(avatar.WorldBoundingBox, this.AmbientBlend);
              renderer.LightColor = compositeLighting.DiffuseColor * this.LightingIntensity;
              renderer.LightDirection = compositeLighting.Direction;
              renderer.AmbientLightColor = compositeLighting.AmbientColor * this.LightingIntensity;
            }
            else
            {
              renderer.LightColor = new Vector3(0.0f);
              renderer.AmbientLightColor = new Vector3(0.25f);
            }
            renderer.Draw(avatar.SkinBones, avatar.Expression);
            ++this.class56_0.lightingSystemStatistic_3.AccumulationValue;
            if (flag)
            {
              graphicsDevice.RenderState.StencilPass = StencilOperation.Keep;
              graphicsDevice.RenderState.StencilFunction = CompareFunction.Equal;
              graphicsDevice.RenderState.ColorWriteChannels = ColorWriteChannels.All;
              graphicsDevice.RenderState.DepthBufferEnable = false;
              graphicsDevice.RenderState.CullMode = CullMode.None;
              graphicsDevice.RenderState.AlphaBlendEnable = true;
              graphicsDevice.RenderState.SourceBlend = Blend.SourceAlpha;
              graphicsDevice.RenderState.DestinationBlend = Blend.InverseSourceAlpha;
              this.fogEffect_0.World = this.class10_0.method_0(avatar.WorldBoundingBoxProxy);
              this.fogEffect_0.View = this.isceneState_0.View;
              this.fogEffect_0.Projection = this.isceneState_0.Projection;
              this.fogEffect_0.Begin();
              this.fogEffect_0.CurrentTechnique.Passes[0].Begin();
              this.class10_0.method_1();
              this.fogEffect_0.CurrentTechnique.Passes[0].End();
              this.fogEffect_0.End();
              ++this.class56_0.lightingSystemStatistic_4.AccumulationValue;
              graphicsDevice.RenderState.CullMode = CullMode.CullCounterClockwiseFace;
              graphicsDevice.RenderState.DepthBufferEnable = true;
            }
          }
        }
      }
      if (!fogEnabled)
        return;
      graphicsDevice.RenderState.StencilEnable = false;
    }

    /// <summary>
    /// Removes resources managed by this object. Commonly used while clearing the scene.
    /// </summary>
    public void Clear()
    {
      this.list_0.Clear();
    }

    /// <summary>
    /// Disposes any graphics resource used internally by this object, and removes
    /// scene resources managed by this object. Commonly used during Game.UnloadContent.
    /// </summary>
    public void Unload()
    {
      this.Clear();
      Disposable.Free(ref this.class10_0);
      Disposable.Free(ref this.fogEffect_0);
    }

    private class Class56
    {
      public LightingSystemStatistic lightingSystemStatistic_0 = LightingSystemStatistics.GetStatistic("SceneGraph_ObjectsSubmitted", LightingSystemStatisticCategory.SceneGraph);
      public LightingSystemStatistic lightingSystemStatistic_1 = LightingSystemStatistics.GetStatistic("SceneGraph_ObjectsRemoved", LightingSystemStatisticCategory.SceneGraph);
      public LightingSystemStatistic lightingSystemStatistic_2 = LightingSystemStatistics.GetStatistic("SceneGraph_ObjectsRetrieved", LightingSystemStatisticCategory.SceneGraph);
      public LightingSystemStatistic lightingSystemStatistic_3 = LightingSystemStatistics.GetStatistic("Renderer_AvatarsRendered", LightingSystemStatisticCategory.Rendering);
      public LightingSystemStatistic lightingSystemStatistic_4 = LightingSystemStatistics.GetStatistic("Renderer_AvatarProxiesRendered", LightingSystemStatisticCategory.Rendering);
    }
  }
}
