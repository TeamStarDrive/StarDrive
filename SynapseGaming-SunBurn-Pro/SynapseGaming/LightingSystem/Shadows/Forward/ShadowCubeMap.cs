// Decompiled with JetBrains decompiler
// Type: SynapseGaming.LightingSystem.Shadows.Forward.ShadowCubeMap
// Assembly: SynapseGaming-SunBurn-Pro, Version=1.3.2.8, Culture=neutral, PublicKeyToken=c23c60523565dbfd
// MVID: A5F03349-72AC-4BAA-AEEE-9AB9B77E0A39
// Assembly location: C:\Projects\BlackBox\StarDrive\SynapseGaming-SunBurn-Pro.dll

using Microsoft.Xna.Framework.Graphics;

namespace SynapseGaming.LightingSystem.Shadows.Forward
{
  /// <summary>
  /// Shadow map class that implements cube-mapped shadows with
  /// per surface level-of-detail. Used for point based lights.
  /// </summary>
  public class ShadowCubeMap : BaseShadowCubeMap
  {
    /// <summary>
    /// Creates a new effect that performs rendering specific to the shadow
    /// mapping implementation used by this object.
    /// </summary>
    /// <returns></returns>
    protected override Effect CreateEffect()
    {
      return new ShadowEffect(this.Device);
    }
  }
}
