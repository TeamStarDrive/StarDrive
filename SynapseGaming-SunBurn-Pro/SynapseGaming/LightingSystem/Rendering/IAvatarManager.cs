// Decompiled with JetBrains decompiler
// Type: SynapseGaming.LightingSystem.Rendering.IAvatarManager
// Assembly: SynapseGaming-SunBurn-Pro, Version=1.3.2.8, Culture=neutral, PublicKeyToken=c23c60523565dbfd
// MVID: A5F03349-72AC-4BAA-AEEE-9AB9B77E0A39
// Assembly location: C:\Projects\BlackBox\StarDrive\SynapseGaming-SunBurn-Pro.dll

using SynapseGaming.LightingSystem.Core;

namespace SynapseGaming.LightingSystem.Rendering
{
  /// <summary>
  /// Interface that provides access to the scene's avatar manager. The avatar manager
  /// provides methods for storing, querying, and rendering scene avatars.
  /// </summary>
  public interface IAvatarManager : IQuery<IAvatar>, ISubmit<IAvatar>, IUnloadable, IManager, IRenderableManager, IManagerService, IShadowRenderer
  {
    /// <summary>
    /// Controls avatar lighting by blending between approximate directional
    /// and ambient lighting.  A blending value of 0.0f makes avatar lighting
    /// highly directional, while a value of 1.0f makes avatar lighting highly
    /// ambient.
    /// </summary>
    float AmbientBlend { get; set; }

    /// <summary>
    /// Controls avatar lighting intensity, providing a means to tune avatar
    /// lighting to the rest of the scene. An intensity of 1.0f keeps
    /// avatar lighting the same, a value of 0.5f halves the lighting
    /// intensity, while 2.0f doubles it.
    /// </summary>
    float LightingIntensity { get; set; }

    /// <summary>
    /// Removes all objects from the container. Commonly used while clearing the scene.
    /// </summary>
    void Clear();
  }
}
