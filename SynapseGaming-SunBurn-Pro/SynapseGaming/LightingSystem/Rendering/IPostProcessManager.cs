// Decompiled with JetBrains decompiler
// Type: SynapseGaming.LightingSystem.Rendering.IPostProcessManager
// Assembly: SynapseGaming-SunBurn-Pro, Version=1.3.2.8, Culture=neutral, PublicKeyToken=c23c60523565dbfd
// MVID: A5F03349-72AC-4BAA-AEEE-9AB9B77E0A39
// Assembly location: C:\Projects\BlackBox\StarDrive\SynapseGaming-SunBurn-Pro.dll

using SynapseGaming.LightingSystem.Core;

namespace SynapseGaming.LightingSystem.Rendering
{
  /// <summary>
  /// Interface that provides access to the scene's post processing manager. The post processing manager
  /// provides methods for rendering full-screen post processing effects.
  /// </summary>
  public interface IPostProcessManager : IUnloadable, IManager, IRenderableManager, IManagerService
  {
    /// <summary>
    /// Adds a new post processor to the processing chain. The last processor added
    /// to the chain is the first to apply its visual effects.
    /// </summary>
    /// <param name="postprocessor"></param>
    void AddPostProcessor(IPostProcessor postprocessor);
  }
}
