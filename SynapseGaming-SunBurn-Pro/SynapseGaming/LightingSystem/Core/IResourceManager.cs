// Decompiled with JetBrains decompiler
// Type: SynapseGaming.LightingSystem.Core.IResourceManager
// Assembly: SynapseGaming-SunBurn-Pro, Version=1.3.2.8, Culture=neutral, PublicKeyToken=c23c60523565dbfd
// MVID: A5F03349-72AC-4BAA-AEEE-9AB9B77E0A39
// Assembly location: C:\Projects\BlackBox\StarDrive\SynapseGaming-SunBurn-Pro.dll

using System;

namespace SynapseGaming.LightingSystem.Core
{
  /// <summary>
  /// Interface that provides access to the scene's resource manager. The resource manager
  /// tracks disposable and unloadable resources, freeing them when the scene is unloaded.
  /// </summary>
  public interface IResourceManager : IUnloadable, IManager, IManagerService
  {
    /// <summary>
    /// Assigns ownership of the resource to the resource manager, this means the manager
    /// will handle disposing and removing (IDisposable), or unloading (IUnloadable) the
    /// resource when the scene is unloaded (when [manager].Unload() is called).
    /// </summary>
    /// <param name="resource"></param>
    void AssignOwnership(IDisposable resource);

    /// <summary>
    /// Assigns ownership of the resource to the resource manager, this means the manager
    /// will handle disposing and removing (IDisposable), or unloading (IUnloadable) the
    /// resource when the scene is unloaded (when [manager].Unload() is called).
    /// </summary>
    /// <param name="resource"></param>
    void AssignOwnership(IUnloadable resource);
  }
}
