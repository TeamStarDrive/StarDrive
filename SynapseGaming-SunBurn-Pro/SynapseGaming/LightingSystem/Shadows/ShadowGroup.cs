// Decompiled with JetBrains decompiler
// Type: SynapseGaming.LightingSystem.Shadows.ShadowGroup
// Assembly: SynapseGaming-SunBurn-Pro, Version=1.3.2.8, Culture=neutral, PublicKeyToken=c23c60523565dbfd
// MVID: A5F03349-72AC-4BAA-AEEE-9AB9B77E0A39
// Assembly location: C:\Projects\BlackBox\StarDrive\SynapseGaming-SunBurn-Pro.dll

using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using SynapseGaming.LightingSystem.Core;
using SynapseGaming.LightingSystem.Lights;

namespace SynapseGaming.LightingSystem.Shadows
{
  /// <summary>
  /// Defines a group of lights that share the same shadow source.
  /// </summary>
  public class ShadowGroup
  {
      /// <summary>
    /// Shared shadow source used to determine shadow casting information.
    /// </summary>
    public IShadowSource ShadowSource { get; private set; }

      /// <summary>Shadow object used to store and render shadows.</summary>
    public IShadow Shadow { get; set; }

      /// <summary>
    /// Shadow bounding sphere originating at the shadow source center.
    /// </summary>
    public BoundingSphere BoundingSphereCentered { get; private set; }

      /// <summary>
    /// Shadow bounding box fitted to the shadow region. For some light types like
    /// spotlights this is not necessarily centered around the shadow source.  For
    /// others like directional lights this is only the shadow bounding area and does
    /// not relate to the illuminated area.
    /// </summary>
    public BoundingBox BoundingBox { get; private set; }

      /// <summary>List of lights that share the shadow source.</summary>
    public List<ILight> Lights { get; } = new List<ILight>(128);

      /// <summary>
    /// Builds the shadow group information based on the shadow source.
    /// </summary>
    /// <param name="shadowsource"></param>
    /// <param name="scenestate">Scene state used to render the current view.</param>
    public void Build(IShadowSource shadowsource, ISceneState scenestate)
    {
      if (this.Lights.Count < 1)
        throw new Exception("Cannot build an empty shadow group.");
      this.ShadowSource = shadowsource;
      if (this.ShadowSource is IPointSource)
      {
        bool flag = true;
        foreach (ILight light in this.Lights)
        {
          if (!flag)
          {
            this.BoundingBox = BoundingBox.CreateMerged(this.BoundingBox, light.WorldBoundingBox);
          }
          else
          {
            this.BoundingBox = light.WorldBoundingBox;
            flag = false;
          }
        }
        BoundingSphere fromBoundingBox = BoundingSphere.CreateFromBoundingBox(this.BoundingBox);
        float radius = Vector3.Distance(fromBoundingBox.Center, shadowsource.ShadowPosition) + fromBoundingBox.Radius;
        this.BoundingSphereCentered = new BoundingSphere(shadowsource.ShadowPosition, radius);
      }
      else
      {
        if (!(this.ShadowSource is IDirectionalSource))
          throw new Exception("Unknown light type - only point, spot, and directional lights are supported at this time.");
        float shadowCasterDistance = scenestate.Environment.ShadowCasterDistance;
        Vector3 translation = scenestate.ViewToWorld.Translation;
        Vector3 vector3 = new Vector3(shadowCasterDistance);
        this.BoundingBox = new BoundingBox(translation - vector3, translation + vector3);
        this.BoundingSphereCentered = new BoundingSphere(translation - (this.ShadowSource as IDirectionalSource).Direction * scenestate.Environment.ShadowCasterDistance, shadowCasterDistance * 2f);
      }
    }
  }
}
