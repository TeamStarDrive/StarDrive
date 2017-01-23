﻿// Decompiled with JetBrains decompiler
// Type: SynapseGaming.LightingSystem.Shadows.Deferred.DeferredShadowDirectionalMap
// Assembly: SynapseGaming-SunBurn-Pro, Version=1.3.2.8, Culture=neutral, PublicKeyToken=c23c60523565dbfd
// MVID: A5F03349-72AC-4BAA-AEEE-9AB9B77E0A39
// Assembly location: C:\Projects\BlackBox\StarDrive\SynapseGaming-SunBurn-Pro.dll

using Microsoft.Xna.Framework.Graphics;
using SynapseGaming.LightingSystem.Effects.Deferred;

namespace SynapseGaming.LightingSystem.Shadows.Deferred
{
  /// <summary>
  /// Shadow map class that implements cascading level-of-detail
  /// directional shadows. Used for directional lights.
  /// </summary>
  public class DeferredShadowDirectionalMap : BaseShadowDirectionalMap
  {
    /// <summary>
    /// Creates a new effect that performs rendering specific to the shadow
    /// mapping implementation used by this object.
    /// </summary>
    /// <returns></returns>
    protected override Effect CreateEffect()
    {
      return (Effect) new DeferredObjectEffect(this.Device);
    }
  }
}
