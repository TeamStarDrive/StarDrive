// Decompiled with JetBrains decompiler
// Type: SynapseGaming.LightingSystem.Shadows.ShadowGroup
// Assembly: SynapseGaming-SunBurn-Pro, Version=1.3.2.8, Culture=neutral, PublicKeyToken=c23c60523565dbfd
// MVID: A5F03349-72AC-4BAA-AEEE-9AB9B77E0A39
// Assembly location: C:\Projects\BlackBox\StarDrive\SynapseGaming-SunBurn-Pro.dll

using Microsoft.Xna.Framework;
using SynapseGaming.LightingSystem.Core;
using SynapseGaming.LightingSystem.Lights;
using System;
using System.Collections.Generic;

namespace SynapseGaming.LightingSystem.Shadows
{
  /// <summary>
  /// Defines a group of lights that share the same shadow source.
  /// </summary>
  public class ShadowGroup
  {
    private List<ILight> list_0 = new List<ILight>(128);
    private IShadowSource ishadowSource_0;
    private IShadow ishadow_0;
    private BoundingSphere boundingSphere_0;
    private BoundingBox boundingBox_0;

    /// <summary>
    /// Shared shadow source used to determine shadow casting information.
    /// </summary>
    public IShadowSource ShadowSource
    {
      get
      {
        return this.ishadowSource_0;
      }
    }

    /// <summary>Shadow object used to store and render shadows.</summary>
    public IShadow Shadow
    {
      get
      {
        return this.ishadow_0;
      }
      set
      {
        this.ishadow_0 = value;
      }
    }

    /// <summary>
    /// Shadow bounding sphere originating at the shadow source center.
    /// </summary>
    public BoundingSphere BoundingSphereCentered
    {
      get
      {
        return this.boundingSphere_0;
      }
    }

    /// <summary>
    /// Shadow bounding box fitted to the shadow region. For some light types like
    /// spotlights this is not necessarily centered around the shadow source.  For
    /// others like directional lights this is only the shadow bounding area and does
    /// not relate to the illuminated area.
    /// </summary>
    public BoundingBox BoundingBox
    {
      get
      {
        return this.boundingBox_0;
      }
    }

    /// <summary>List of lights that share the shadow source.</summary>
    public List<ILight> Lights
    {
      get
      {
        return this.list_0;
      }
    }

    /// <summary>
    /// Builds the shadow group information based on the shadow source.
    /// </summary>
    /// <param name="shadowsource"></param>
    /// <param name="scenestate">Scene state used to render the current view.</param>
    public void Build(IShadowSource shadowsource, ISceneState scenestate)
    {
      if (this.list_0.Count < 1)
        throw new Exception("Cannot build an empty shadow group.");
      this.ishadowSource_0 = shadowsource;
      if (this.ishadowSource_0 is IPointSource)
      {
        bool flag = true;
        foreach (ILight light in this.list_0)
        {
          if (!flag)
          {
            this.boundingBox_0 = BoundingBox.CreateMerged(this.boundingBox_0, light.WorldBoundingBox);
          }
          else
          {
            this.boundingBox_0 = light.WorldBoundingBox;
            flag = false;
          }
        }
        BoundingSphere fromBoundingBox = BoundingSphere.CreateFromBoundingBox(this.boundingBox_0);
        float radius = Vector3.Distance(fromBoundingBox.Center, shadowsource.ShadowPosition) + fromBoundingBox.Radius;
        this.boundingSphere_0 = new BoundingSphere(shadowsource.ShadowPosition, radius);
      }
      else
      {
        if (!(this.ishadowSource_0 is IDirectionalSource))
          throw new Exception("Unknown light type - only point, spot, and directional lights are supported at this time.");
        float shadowCasterDistance = scenestate.Environment.ShadowCasterDistance;
        Vector3 translation = scenestate.ViewToWorld.Translation;
        Vector3 vector3 = new Vector3(shadowCasterDistance);
        this.boundingBox_0 = new BoundingBox(translation - vector3, translation + vector3);
        this.boundingSphere_0 = new BoundingSphere(translation - (this.ishadowSource_0 as IDirectionalSource).Direction * scenestate.Environment.ShadowCasterDistance, shadowCasterDistance * 2f);
      }
    }
  }
}
