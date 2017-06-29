// Decompiled with JetBrains decompiler
// Type: SynapseGaming.LightingSystem.Shadows.IShadowMapManager
// Assembly: SynapseGaming-SunBurn-Pro, Version=1.3.2.8, Culture=neutral, PublicKeyToken=c23c60523565dbfd
// MVID: A5F03349-72AC-4BAA-AEEE-9AB9B77E0A39
// Assembly location: C:\Projects\BlackBox\StarDrive\SynapseGaming-SunBurn-Pro.dll

using System.Collections.Generic;
using SynapseGaming.LightingSystem.Core;
using SynapseGaming.LightingSystem.Lights;

namespace SynapseGaming.LightingSystem.Shadows
{
  /// <summary>
  /// Interface that provides access to the scene's shadow map manager. The shadow map manager
  /// provides methods for creating and caching scene shadow maps.
  /// </summary>
  public interface IShadowMapManager : IUnloadable, IManager, IRenderableManager, IManagerService
  {
    /// <summary>
    /// Organizes the provided lights into shadow and render target groups.
    /// </summary>
    /// <param name="rendertargetgroups">Returned render target groups.</param>
    /// <param name="lights">Lights to organize.</param>
    /// <param name="usedefaultgrouping">Determines if ungrouped lights should be placed in a
    /// single default group (recommended: true for deferred rendering and false for forward).</param>
    void BuildShadows(List<ShadowRenderTargetGroup> rendertargetgroups, List<ILight> lights, bool usedefaultgrouping);
  }
}
