// Decompiled with JetBrains decompiler
// Type: SynapseGaming.LightingSystem.Rendering.IShadowRenderer
// Assembly: SynapseGaming-SunBurn-Pro, Version=1.3.2.8, Culture=neutral, PublicKeyToken=c23c60523565dbfd
// MVID: A5F03349-72AC-4BAA-AEEE-9AB9B77E0A39
// Assembly location: C:\Projects\BlackBox\StarDrive\SynapseGaming-SunBurn-Pro.dll

using Microsoft.Xna.Framework.Graphics;
using SynapseGaming.LightingSystem.Shadows;

namespace SynapseGaming.LightingSystem.Rendering
{
  /// <summary>
  /// Interface used by objects that perform custom rendering during shadow map generation.
  /// </summary>
  public interface IShadowRenderer
  {
    /// <summary>Prepares for shadow map rendering.</summary>
    /// <param name="shadowgroup"></param>
    void BeginShadowGroupRendering(ShadowGroup shadowgroup);

    /// <summary>Performs shadow map rendering.</summary>
    /// <param name="shadowgroup"></param>
    /// <param name="surface"></param>
    /// <param name="shadoweffect"></param>
    void RenderToShadowMapSurface(ShadowGroup shadowgroup, ShadowMapSurface surface, Effect shadoweffect);

    /// <summary>Finalizes shadow map rendering.</summary>
    /// <param name="shadowgroup"></param>
    void EndShadowGroupRendering(ShadowGroup shadowgroup);
  }
}
