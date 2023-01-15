// Decompiled with JetBrains decompiler
// Type: SynapseGaming.LightingSystem.Lights.LightManager
// Assembly: SynapseGaming-SunBurn-Pro, Version=1.3.2.8, Culture=neutral, PublicKeyToken=c23c60523565dbfd
// MVID: A5F03349-72AC-4BAA-AEEE-9AB9B77E0A39
// Assembly location: C:\Projects\BlackBox\StarDrive\SynapseGaming-SunBurn-Pro.dll

using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using ns11;
using EmbeddedResources;
using ns7;
using SynapseGaming.LightingSystem.Core;
using SynapseGaming.LightingSystem.Rendering.Deferred;
using SynapseGaming.LightingSystem.Shadows;

namespace SynapseGaming.LightingSystem.Lights
{
  /// <summary>
  /// Manages all scene lights and allows querying the scene with
  /// a view or bounding box for lights that affect the area
  /// (acts as a light scenegraph).
  /// </summary>
  public class LightManager : BaseLightManager, ILightManager
  {
      private int int_3 = 8;
    private List<ILightRig> LightRigs = new List<ILightRig>(16);
    private List<ILight> Lights = new List<ILight>(64);
    private Class45 class45_0;
    private Class54 class54_0;

      /// <summary>
    /// Gets the manager specific Type used as a unique key for storing and
    /// requesting the manager from the IManagerServiceProvider.
    /// </summary>
    public Type ManagerType => SceneInterface.LightManagerType;

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
    public int ManagerProcessOrder { get; set; } = 30;

      /// <summary>
    /// The current GraphicsDeviceManager used by this object.
    /// </summary>
    public IGraphicsDeviceService GraphicsDeviceManager { get; }

    /// <summary>
    /// Number of geometry layers (slices) used to construct volume lights. Increasing
    /// the number of slices improves volume lighting quality, decreasing the number of
    /// slices improves performance.
    /// </summary>
    public int VolumeLightSliceCount
    {
      get => this.int_3;
          set
      {
        this.int_3 = value;
        ClearAndDispose();
      }
    }

    /// <summary>Creates a new LightManager instance.</summary>
    /// <param name="graphicsdevicemanager"></param>
    /// <param name="worldboundingbox">The smallest bounding area that completely
    /// contains the scene. Helps the LightManager build an optimal scene tree.</param>
    /// <param name="worldtreemaxdepth">Maximum depth for entries in the scene tree. Small
    /// scenes with few objects see better performance with shallow trees. Large complex
    /// scenes often need deeper trees.</param>
    public LightManager(IGraphicsDeviceService graphicsdevicemanager, BoundingBox worldboundingbox, int worldtreemaxdepth)
      : base(worldboundingbox, worldtreemaxdepth)
    {
      this.GraphicsDeviceManager = graphicsdevicemanager;
    }

    /// <summary>Creates a new LightManager instance.</summary>
    /// <param name="graphicsdevicemanager"></param>
    public LightManager(IGraphicsDeviceService graphicsdevicemanager)
    {
      this.GraphicsDeviceManager = graphicsdevicemanager;
    }

    /// <summary>
    /// Use to apply user quality and performance preferences to the resources managed by this object.
    /// </summary>
    /// <param name="preferences"></param>
    public override void ApplyPreferences(ILightingSystemPreferences preferences)
    {
      base.ApplyPreferences(preferences);
    }

    /// <summary>Renders volume lighting for the contained lights.</summary>
    /// <param name="deferredbuffers">The deferred buffer used during the
    /// rendering pass, or null if forward rendering.</param>
    public void RenderVolumeLights(DeferredBuffers deferredbuffers)
    {
      GraphicsDevice graphicsDevice = this.GraphicsDeviceManager.GraphicsDevice;
      if (this.class45_0 == null)
      {
        this.class45_0 = new Class45(graphicsDevice);
        this.class54_0 = new Class54(graphicsDevice, this.int_3);
      }
      graphicsDevice.RenderState.AlphaBlendEnable = true;
      graphicsDevice.RenderState.DestinationBlend = Blend.One;
      graphicsDevice.RenderState.SourceBlend = Blend.One;
      graphicsDevice.RenderState.CullMode = CullMode.None;
      graphicsDevice.RenderState.DepthBufferEnable = true;
      graphicsDevice.RenderState.DepthBufferWriteEnable = false;
      graphicsDevice.VertexDeclaration = this.class54_0.VertexDeclaration;
      graphicsDevice.Vertices[0].SetSource(this.class54_0.VertexBuffer, 0, this.class54_0.VertexStride);
      this.class45_0.View = this.SceneState.View;
      this.class45_0.Projection = this.SceneState.Projection;
      this.class45_0.method_3(this.SceneState.ProjectionToView);
      RenderTarget2D renderTarget2D = null;
      if (deferredbuffers != null)
        renderTarget2D = deferredbuffers.GetDeferredBuffer(DeferredBufferType.DepthAndSpecularPower);
      this.class45_0.SceneDepthMap = renderTarget2D == null ? null : renderTarget2D.GetTexture();
      this.class45_0.Begin();
      this.class45_0.CurrentTechnique.Passes[0].Begin();
      this.Lights.Clear();
      this.Find(this.Lights, this.SceneState.ViewFrustum, ObjectFilter.EnabledDynamicAndStatic);
      foreach (ILight light in this.Lights)
      {
        if (light is ISpotSource)
        {
          ISpotSource spotSource = light as ISpotSource;
          if (spotSource.Volume > 0.0)
          {
            this.class45_0.World = light.World;
            this.class45_0.Color = light.CompositeColorAndIntensity / this.int_3 * spotSource.Volume;
            this.class45_0.method_2(spotSource.Angle, spotSource.Radius);
            this.class45_0.CommitChanges();
            graphicsDevice.DrawPrimitives(PrimitiveType.TriangleList, 0, this.class54_0.PrimitiveCount);
          }
        }
      }
      this.class45_0.CurrentTechnique.Passes[0].End();
      this.class45_0.End();
      graphicsDevice.RenderState.AlphaBlendEnable = false;
      graphicsDevice.RenderState.DestinationBlend = Blend.Zero;
      graphicsDevice.RenderState.SourceBlend = Blend.One;
      graphicsDevice.RenderState.CullMode = CullMode.CullCounterClockwiseFace;
      graphicsDevice.RenderState.DepthBufferWriteEnable = true;
    }

    /// <summary>
    /// Helper method that creates and submits a static light
    /// using a method layout similar to SunBurn 1.2.x.
    /// </summary>
    /// <param name="diffusecolor">Direct lighting color given off by the light.</param>
    /// <param name="direction">Direction the light is pointing.</param>
    /// <param name="intensity">Intensity of the light.</param>
    /// <param name="shadowtype">Defines the type of objects that cast shadows from the light.
    /// Does not affect an object's ability to receive shadows.</param>
    /// <param name="shadowquality">Visual quality of casts shadows.</param>
    /// <param name="shadowprimarybias">Main property used to eliminate shadow artifacts.</param>
    /// <param name="shadowsecondarybias">Additional fine-tuned property used to eliminate shadow artifacts.</param>
    public void SubmitStaticDirectionalLight(Vector3 diffusecolor, Vector3 direction, float intensity, ShadowType shadowtype, float shadowquality, float shadowprimarybias, float shadowsecondarybias)
    {
      this.Submit(new DirectionalLight
      {
          DiffuseColor = diffusecolor,
          Intensity = intensity,
          Direction = direction,
          ShadowType = shadowtype,
          ShadowQuality = shadowquality,
          ShadowPrimaryBias = shadowprimarybias,
          ShadowSecondaryBias = shadowsecondarybias
      });
    }

    /// <summary>
    /// Helper method that creates and submits a static light
    /// using a method layout similar to SunBurn 1.2.x.
    /// </summary>
    /// <param name="diffusecolor">Direct lighting color given off by the light.</param>
    /// <param name="position">Position in world space of the light.</param>
    /// <param name="intensity">Intensity of the light.</param>
    /// <param name="radius">Lighting radius in world space.</param>
    /// <param name="filllight">Provides softer indirect-like illumination without "hot-spots".</param>
    /// <param name="falloffstrength">Controls how quickly lighting falls off over distance (only available in deferred rendering).</param>
    /// <param name="shadowtype">Defines the type of objects that cast shadows from the light.
    /// Does not affect an object's ability to receive shadows.</param>
    /// <param name="shadowquality">Visual quality of casts shadows.</param>
    /// <param name="shadowprimarybias">Main property used to eliminate shadow artifacts.</param>
    /// <param name="shadowsecondarybias">Additional fine-tuned property used to eliminate shadow artifacts.</param>
    /// <param name="shadowsource">Shadow source the light's shadows are generated from.
    /// Allows sharing shadows between point light sources.  Setting the parameter
    /// to null makes the light its own unique shadow source.</param>
    public void SubmitStaticPointLight(Vector3 diffusecolor, Vector3 position, float intensity, float radius, bool filllight, float falloffstrength, ShadowType shadowtype, float shadowquality, float shadowprimarybias, float shadowsecondarybias, IShadowSource shadowsource)
    {
      PointLight pointLight = new PointLight();
      pointLight.DiffuseColor = diffusecolor;
      pointLight.Position = position;
      pointLight.Intensity = intensity;
      pointLight.Radius = radius;
      pointLight.FillLight = filllight;
      pointLight.FalloffStrength = falloffstrength;
      pointLight.ShadowType = shadowtype;
      pointLight.ShadowQuality = shadowquality;
      pointLight.ShadowPrimaryBias = shadowprimarybias;
      pointLight.ShadowSecondaryBias = shadowsecondarybias;
      pointLight.ShadowSource = shadowsource == null ? pointLight : shadowsource;
      this.Submit(pointLight);
    }

    /// <summary>
    /// Helper method that creates and submits a static light
    /// using a method layout similar to SunBurn 1.2.x.
    /// </summary>
    /// <param name="diffusecolor">Direct lighting color given off by the light.</param>
    /// <param name="position">Position in world space of the light.</param>
    /// <param name="intensity">Intensity of the light.</param>
    /// <param name="radius">Lighting radius in world space.</param>
    /// <param name="direction">Direction the light is pointing.</param>
    /// <param name="angle">Angle in degrees of the light's influence.</param>
    /// <param name="filllight">Provides softer indirect-like illumination without "hot-spots".</param>
    /// <param name="falloffstrength">Controls how quickly lighting falls off over distance (only available in deferred rendering).</param>
    /// <param name="shadowtype">Defines the type of objects that cast shadows from the light.
    /// Does not affect an object's ability to receive shadows.</param>
    /// <param name="shadowquality">Visual quality of casts shadows.</param>
    /// <param name="shadowprimarybias">Main property used to eliminate shadow artifacts.</param>
    /// <param name="shadowsecondarybias">Additional fine-tuned property used to eliminate shadow artifacts.</param>
    /// <param name="shadowsource">Shadow source the light's shadows are generated from.
    /// Allows sharing shadows between point light sources.  Setting the parameter
    /// to null makes the light its own unique shadow source.</param>
    public void SubmitStaticSpotLight(Vector3 diffusecolor, Vector3 position, float intensity, float radius, Vector3 direction, float angle, bool filllight, float falloffstrength, ShadowType shadowtype, float shadowquality, float shadowprimarybias, float shadowsecondarybias, IShadowSource shadowsource)
    {
      SpotLight spotLight = new SpotLight();
      spotLight.DiffuseColor = diffusecolor;
      spotLight.Position = position;
      spotLight.Intensity = intensity;
      spotLight.Radius = radius;
      spotLight.Direction = direction;
      spotLight.Angle = angle;
      spotLight.FillLight = filllight;
      spotLight.FalloffStrength = falloffstrength;
      spotLight.ShadowType = shadowtype;
      spotLight.ShadowQuality = shadowquality;
      spotLight.ShadowPrimaryBias = shadowprimarybias;
      spotLight.ShadowSecondaryBias = shadowsecondarybias;
      spotLight.ShadowSource = shadowsource == null ? spotLight : shadowsource;
      this.Submit(spotLight);
    }

    /// <summary>
    /// Helper method that creates and submits a static light
    /// using a method layout similar to SunBurn 1.2.x.
    /// </summary>
    /// <param name="diffusecolor">Direct lighting color given off by the light.</param>
    /// <param name="intensity">Intensity of the light.</param>
    public void SubmitStaticAmbientLight(Vector3 diffusecolor, float intensity)
    {
      this.Submit(new AmbientLight
      {
          DiffuseColor = diffusecolor,
          Intensity = intensity
      });
    }

    /// <summary>
    /// Adds an object to the container. This does not transfer ownership, disposable
    /// objects should be maintained and disposed separately.
    /// </summary>
    /// <param name="lightrig"></param>
    public virtual void Submit(ILightRig lightrig)
    {
      this.LightRigs.Add(lightrig);
    }

    /// <summary>
    /// Moves an object within the container. This method is used when the container
    /// implements a tree or graph, and relocates an object within that structure
    /// often due to a change in object world position.
    /// </summary>
    /// <param name="lightrig"></param>
    public virtual void Move(ILightRig lightrig)
    {
    }

    /// <summary>Removes an object from the container.</summary>
    /// <param name="lightrig"></param>
    public virtual bool Remove(ILightRig lightrig)
    {
      return this.LightRigs.Remove(lightrig);
    }

    /// <summary>
    /// Finds all contained objects that match a set of filter attributes
    /// and overlap with or are contained in a bounding area.
    /// </summary>
    /// <param name="foundobjects">List used to store found objects during the query.</param>
    /// <param name="worldbounds">Bounding area used to limit query results.</param>
    /// <param name="objectfilter">Filter used to limit query results to objects with specific attributes.</param>
    public override void Find(List<ILight> foundobjects, BoundingFrustum worldbounds, ObjectFilter objectfilter)
    {
      base.Find(foundobjects, worldbounds, objectfilter);
      foreach (IQuery<ILight> query in this.LightRigs)
        query.Find(foundobjects, worldbounds, objectfilter);
    }

    /// <summary>
    /// Finds all contained objects that match a set of filter attributes
    /// and overlap with or are contained in a bounding area.
    /// </summary>
    /// <param name="foundobjects">List used to store found objects during the query.</param>
    /// <param name="worldbounds">Bounding area used to limit query results.</param>
    /// <param name="objectfilter">Filter used to limit query results to objects with specific attributes.</param>
    public override void Find(List<ILight> foundobjects, BoundingBox worldbounds, ObjectFilter objectfilter)
    {
      base.Find(foundobjects, worldbounds, objectfilter);
      foreach (IQuery<ILight> query in this.LightRigs)
        query.Find(foundobjects, worldbounds, objectfilter);
    }

    /// <summary>
    /// Finds all contained objects that match a set of filter attributes.
    /// </summary>
    /// <param name="foundobjects">List used to store found objects during the query.</param>
    /// <param name="objectfilter">Filter used to limit query results to objects with specific attributes.</param>
    public override void Find(List<ILight> foundobjects, ObjectFilter objectfilter)
    {
      base.Find(foundobjects, objectfilter);
      foreach (IQuery<ILight> query in this.LightRigs)
        query.Find(foundobjects, objectfilter);
    }

    /// <summary>Removes all lights and cleans up scene information.</summary>
    public override void Clear()
    {
      this.LightRigs.Clear();
      base.Clear();
    }

    void ClearAndDispose()
    {
      Disposable.Dispose(ref this.class45_0);
      Disposable.Dispose(ref this.class54_0);
    }

    protected override void Dispose(bool disposing)
    {
      Disposable.Dispose(ref this.class45_0);
      Disposable.Dispose(ref this.class54_0);
      base.Dispose(disposing);
    }

    /// <summary>
    /// Disposes any graphics resource used internally by this object, and removes
    /// scene resources managed by this object. Commonly used during Game.UnloadContent.
    /// </summary>
    public override void Unload()
    {
      Dispose(true);
    }
  }
}
