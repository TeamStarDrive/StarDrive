// Decompiled with JetBrains decompiler
// Type: SynapseGaming.LightingSystem.Lights.BaseLightManager
// Assembly: SynapseGaming-SunBurn-Pro, Version=1.3.2.8, Culture=neutral, PublicKeyToken=c23c60523565dbfd
// MVID: A5F03349-72AC-4BAA-AEEE-9AB9B77E0A39
// Assembly location: C:\Projects\BlackBox\StarDrive\SynapseGaming-SunBurn-Pro.dll

using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using SynapseGaming.LightingSystem.Core;

namespace SynapseGaming.LightingSystem.Lights
{
  /// <summary>
  /// Manages all scene lights and allows querying the scene with
  /// a view or bounding box for lights that affect the area
  /// (acts as a light scenegraph).
  /// </summary>
  public class BaseLightManager : ObjectGraph<ILight>, IQuery<ILight>, IUnloadable, IManager, ILightQuery, IDisposable
  {
    private static int int_1 = 10000;
    private static float float_0 = 0.004f;
    private List<ILight> list_1 = new List<ILight>(16);
    private Class52 class52_0 = new Class52();
    /// <summary>
    /// Current scene state information provided to BeginFrameRendering (only valid between calls to BeginFrameRendering and EndFrameRendering).
    /// </summary>
    protected ISceneState SceneState;

    internal static int MaxLightsPerGroup
    {
      get => int_1;
        set => int_1 = value;
    }

    /// <summary>Creates a new BaseLightManager instance.</summary>
    /// <param name="worldboundingbox">The smallest bounding area that completely
    /// contains the scene. Helps the LightManager build an optimal scene tree.</param>
    /// <param name="worldtreemaxdepth">Maximum depth for entries in the scene tree. Small
    /// scenes with few objects see better performance with shallow trees. Large complex
    /// scenes often need deeper trees.</param>
    public BaseLightManager(BoundingBox worldboundingbox, int worldtreemaxdepth)
      : base(worldboundingbox, worldtreemaxdepth)
    {
    }

    /// <summary>Creates a new BaseLightManager instance.</summary>
    public BaseLightManager()
    {
    }

    /// <summary>
    /// Use to apply user quality and performance preferences to the resources managed by this object.
    /// </summary>
    /// <param name="preferences"></param>
    public virtual void ApplyPreferences(ILightingSystemPreferences preferences)
    {
    }

    /// <summary>Adds a light to the LightManager.</summary>
    /// <param name="light"></param>
    public override void Submit(ILight light)
    {
      base.Submit(light);
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
      this.list_1.Clear();
      base.Find(this.list_1, worldbounds, objectfilter);
      bool bool_0 = (objectfilter & ObjectFilter.Enabled) != 0;
      bool bool_1 = (objectfilter & ObjectFilter.Disabled) != 0;
      int count = this.list_1.Count;
      for (int index = 0; index < count; ++index)
      {
        ILight ilight_0 = this.list_1[index];
        if (this.method_0(bool_0, bool_1, ilight_0))
          foundobjects.Add(ilight_0);
      }
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
      this.list_1.Clear();
      base.Find(this.list_1, worldbounds, objectfilter);
      bool bool_0 = (objectfilter & ObjectFilter.Enabled) != 0;
      bool bool_1 = (objectfilter & ObjectFilter.Disabled) != 0;
      int count = this.list_1.Count;
      for (int index = 0; index < count; ++index)
      {
        ILight ilight_0 = this.list_1[index];
        if (this.method_0(bool_0, bool_1, ilight_0))
          foundobjects.Add(ilight_0);
      }
    }

    /// <summary>
    /// Finds all contained objects that match a set of filter attributes.
    /// </summary>
    /// <param name="foundobjects">List used to store found objects during the query.</param>
    /// <param name="objectfilter">Filter used to limit query results to objects with specific attributes.</param>
    public override void Find(List<ILight> foundobjects, ObjectFilter objectfilter)
    {
      this.list_1.Clear();
      base.Find(this.list_1, objectfilter);
      bool bool_0 = (objectfilter & ObjectFilter.Enabled) != 0;
      bool bool_1 = (objectfilter & ObjectFilter.Disabled) != 0;
      int count = this.list_1.Count;
      for (int index = 0; index < count; ++index)
      {
        ILight ilight_0 = this.list_1[index];
        if (this.method_0(bool_0, bool_1, ilight_0))
          foundobjects.Add(ilight_0);
      }
    }

    private bool method_0(bool bool_0, bool bool_1, ILight ilight_0)
    {
      if (!bool_0 && !bool_1)
        return false;
      if (bool_0 && bool_1)
        return true;
      if (!ilight_0.Enabled)
        return bool_1;
      IPointSource pointSource = ilight_0 as IPointSource;
      if (pointSource != null && pointSource.Radius <= 0.0)
        return bool_1;
      Vector3 colorAndIntensity = ilight_0.CompositeColorAndIntensity;
      if (colorAndIntensity.X + (double) colorAndIntensity.Y + colorAndIntensity.Z > float_0)
        return bool_0;
      return bool_1;
    }

    /// <summary>
    /// Generates approximate lighting for an area in world space. The returned composite
    /// lighting is packed into a single directional and ambient light for fast single-pass lighting.
    /// 
    /// Note: because this information is approximated smaller world space areas will
    /// result in more accurate lighting. Also the approximation is calculated on the
    /// cpu and cannot take into account shadowing.
    /// </summary>
    /// <param name="worldbounds">Bounding area used to determine approximate lighting.</param>
    /// <param name="ambientblend">Blending value (0.0f - 1.0f) that determines how much approximate lighting
    /// contributes to ambient lighting. Approximate lighting can create highly directional lighting, using
    /// a higher blending value can create softer, more realistic lighting.</param>
    /// <returns>Composite lighting packed into a single directional and ambient light.</returns>
    public CompositeLighting GetCompositeLighting(BoundingBox worldbounds, float ambientblend)
    {
      this.list_1.Clear();
      this.Find(this.list_1, worldbounds, ObjectFilter.EnabledDynamicAndStatic);
      return this.GetCompositeLighting(this.list_1, worldbounds, ambientblend);
    }

    /// <summary>
    /// Generates approximate lighting for an area in world space using a custom set of lights.
    /// The returned composite lighting is packed into a single directional and ambient light for
    /// fast single-pass lighting.
    /// 
    /// Note: because this information is approximated smaller world space areas will
    /// result in more accurate lighting. Also the approximation is calculated on the
    /// cpu and cannot take into account shadowing.
    /// </summary>
    /// <param name="sourcelights">Lights used to generate approximate lighting.</param>
    /// <param name="worldbounds">Bounding area used to determine approximate lighting.</param>
    /// <param name="ambientblend">Blending value (0.0f - 1.0f) that determines how much approximate lighting
    /// contributes to ambient lighting. Approximate lighting can create highly directional lighting, using
    /// a higher blending value can create softer, more realistic lighting.</param>
    /// <returns>Composite lighting packed into a single directional and ambient light.</returns>
    public CompositeLighting GetCompositeLighting(List<ILight> sourcelights, BoundingBox worldbounds, float ambientblend)
    {
      Vector3 zero1 = Vector3.Zero;
      Vector3 zero2 = Vector3.Zero;
      Vector3 vector3_1 = Vector3.Zero;
      Vector3 vector3_2 = (worldbounds.Max + worldbounds.Min) * 0.5f;
      ++this.class52_0.lightingSystemStatistic_0.AccumulationValue;
      foreach (ILight sourcelight in sourcelights)
      {
        ++this.class52_0.lightingSystemStatistic_1.AccumulationValue;
        if (sourcelight is IPointSource)
        {
          IPointSource pointSource = sourcelight as IPointSource;
          Vector3 position = pointSource.Position;
          Vector3 result;
          Vector3.Subtract(ref vector3_2, ref position, out result);
          float num1 = result.Length();
          if (num1 > 0.0)
          {
            result.X /= num1;
            result.Y /= num1;
            result.Z /= num1;
          }
          float num2 = 1f - MathHelper.Clamp(num1 / pointSource.Radius, 0.0f, 1f);
          float num3 = sourcelight.Intensity * num2;
          if (num3 > 0.0 && sourcelight is ISpotSource)
          {
            ISpotSource spotSource = sourcelight as ISpotSource;
            float num4 = (float) Math.Cos(MathHelper.ToRadians(MathHelper.Clamp(spotSource.Angle * 0.5f, 0.01f, 89.99f)));
            float num5 = Vector3.Dot(spotSource.Direction, result);
            num3 *= MathHelper.Clamp((float) ((num5 - (double) num4) / (1.0 - num4)), 0.0f, 1f);
          }
          zero1.X += result.X * num3;
          zero1.Y += result.Y * num3;
          zero1.Z += result.Z * num3;
          Vector3 diffuseColor = sourcelight.DiffuseColor;
          zero2.X += num3 * diffuseColor.X;
          zero2.Y += num3 * diffuseColor.Y;
          zero2.Z += num3 * diffuseColor.Z;
        }
        else if (sourcelight is IDirectionalSource)
        {
          IDirectionalSource directionalSource = sourcelight as IDirectionalSource;
          zero1 += directionalSource.Direction * sourcelight.Intensity;
          zero2 += sourcelight.CompositeColorAndIntensity;
        }
        else if (sourcelight is IAmbientSource)
          vector3_1 = sourcelight.CompositeColorAndIntensity;
      }
      float num = MathHelper.Clamp(ambientblend, 0.0f, 1f);
      Vector3 vector3_3 = vector3_1 + zero2 * num * 0.25f;
      Vector3 vector3_4 = zero2 * (1f - num);
      return new CompositeLighting { Direction = Vector3.Normalize(zero1), DiffuseColor = vector3_4, AmbientColor = vector3_3 };
    }

    /// <summary>Sets up the object prior to rendering.</summary>
    /// <param name="scenestate"></param>
    public virtual void BeginFrameRendering(ISceneState scenestate)
    {
      this.SceneState = scenestate;
      //SplashScreen.CheckProductActivation();
      this.MoveDynamicObjects();
    }

    /// <summary>Finalizes rendering.</summary>
    public virtual void EndFrameRendering()
    {
    }

    public void Dispose()
    {
        Dispose(true);
    }

    protected virtual void Dispose(bool disposing)
    {
        Clear();
    }

    /// <summary>
    /// Disposes any graphics resource used internally by this object, and removes
    /// scene resources managed by this object. Commonly used during Game.UnloadContent.
    /// </summary>
    public virtual void Unload()
    {
      Dispose();
    }

    private class Class52
    {
      public LightingSystemStatistic lightingSystemStatistic_0 = LightingSystemStatistics.GetStatistic("Light_CompositeLights", LightingSystemStatisticCategory.Lighting);
      public LightingSystemStatistic lightingSystemStatistic_1 = LightingSystemStatistics.GetStatistic("Light_CompositeLightSources", LightingSystemStatisticCategory.Lighting);
    }
  }
}
