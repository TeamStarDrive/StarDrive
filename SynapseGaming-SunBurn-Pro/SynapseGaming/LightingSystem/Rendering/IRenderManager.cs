// Decompiled with JetBrains decompiler
// Type: SynapseGaming.LightingSystem.Rendering.IRenderManager
// Assembly: SynapseGaming-SunBurn-Pro, Version=1.3.2.8, Culture=neutral, PublicKeyToken=c23c60523565dbfd
// MVID: A5F03349-72AC-4BAA-AEEE-9AB9B77E0A39
// Assembly location: C:\Projects\BlackBox\StarDrive\SynapseGaming-SunBurn-Pro.dll

using SynapseGaming.LightingSystem.Core;

namespace SynapseGaming.LightingSystem.Rendering
{
  /// <summary>
  /// Interface that provides access to the scene's render manager. The render manager
  /// provides methods for controlling scene rendering.
  /// </summary>
  public interface IRenderManager : IUnloadable, IManager, IRenderableManager, IManagerService
  {
    /// <summary>Renders the scene.</summary>
    void Render();
  }
}
