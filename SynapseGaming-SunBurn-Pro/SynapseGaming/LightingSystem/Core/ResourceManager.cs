// Decompiled with JetBrains decompiler
// Type: SynapseGaming.LightingSystem.Core.ResourceManager
// Assembly: SynapseGaming-SunBurn-Pro, Version=1.3.2.8, Culture=neutral, PublicKeyToken=c23c60523565dbfd
// MVID: A5F03349-72AC-4BAA-AEEE-9AB9B77E0A39
// Assembly location: C:\Projects\BlackBox\StarDrive\SynapseGaming-SunBurn-Pro.dll

using System;
using System.Collections.Generic;

namespace SynapseGaming.LightingSystem.Core
{
  /// <summary>
  /// Can be assigned ownership of disposable and unloadable resources, automatically
  /// freeing them when the scene is unloaded.
  /// </summary>
  public class ResourceManager : IUnloadable, IManager, IManagerService, IResourceManager
  {
    private int int_0 = 500;
    private Dictionary<IDisposable, int> dictionary_0 = new Dictionary<IDisposable, int>(32);
    private Dictionary<IUnloadable, int> dictionary_1 = new Dictionary<IUnloadable, int>(32);

    /// <summary>
    /// Gets the manager specific Type used as a unique key for storing and
    /// requesting the manager from the IManagerServiceProvider.
    /// </summary>
    public Type ManagerType
    {
      get
      {
        return SceneInterface.ResourceManagerType;
      }
    }

    /// <summary>
    /// Sets the order this manager is processed relative to other managers
    /// in the IManagerServiceProvider. Managers with lower processing order
    /// values are processed first.
    /// 
    /// In the case of BeginFrameRendering and EndFrameRendering, BeginFrameRendering
    /// is processed in the normal order (lowest order value to highest), however
    /// EndFrameRendering is processed in reverse order (highest to lowest) to ensure
    /// the first manager begun is the last one ended (FILO).
    /// </summary>
    public int ManagerProcessOrder
    {
      get
      {
        return this.int_0;
      }
      set
      {
        this.int_0 = value;
      }
    }

    /// <summary>Unused.</summary>
    /// <param name="preferences"></param>
    public void ApplyPreferences(ILightingSystemPreferences preferences)
    {
    }

    /// <summary>
    /// Assigns ownership of the resource to the resource manager, this means the manager
    /// will handle disposing and removing (IDisposable), or unloading (IUnloadable) the
    /// resource when the scene is unloaded (when [manager].Unload() is called).
    /// </summary>
    /// <param name="resource"></param>
    public void AssignOwnership(IDisposable resource)
    {
      if (this.dictionary_0.ContainsKey(resource))
        return;
      this.dictionary_0.Add(resource, 0);
    }

    /// <summary>
    /// Assigns ownership of the resource to the resource manager, this means the manager
    /// will handle disposing and removing (IDisposable), or unloading (IUnloadable) the
    /// resource when the scene is unloaded (when [manager].Unload() is called).
    /// </summary>
    /// <param name="resource"></param>
    public void AssignOwnership(IUnloadable resource)
    {
      if (this.dictionary_1.ContainsKey(resource))
        return;
      this.dictionary_1.Add(resource, 0);
    }

    /// <summary>
    /// Unused. Resources assigned to the manager are not removed until
    /// they are disposed (during the Unload method).
    /// </summary>
    public void Clear()
    {
    }

    /// <summary>
    /// Disposes and removes all IDisposable resources. Unloads but
    /// continues tracking IUnloadable resources.
    /// 
    /// Commonly used during Game.UnloadContent.
    /// </summary>
    public void Unload()
    {
      foreach (KeyValuePair<IDisposable, int> keyValuePair in this.dictionary_0)
        keyValuePair.Key.Dispose();
      foreach (KeyValuePair<IUnloadable, int> keyValuePair in this.dictionary_1)
        keyValuePair.Key.Unload();
      this.dictionary_0.Clear();
    }
  }
}
