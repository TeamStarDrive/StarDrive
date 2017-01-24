// Decompiled with JetBrains decompiler
// Type: SynapseGaming.LightingSystem.Core.IUnloadable
// Assembly: SynapseGaming-SunBurn-Pro, Version=1.3.2.8, Culture=neutral, PublicKeyToken=c23c60523565dbfd
// MVID: A5F03349-72AC-4BAA-AEEE-9AB9B77E0A39
// Assembly location: C:\Projects\BlackBox\StarDrive\SynapseGaming-SunBurn-Pro.dll

namespace SynapseGaming.LightingSystem.Core
{
  /// <summary>
  /// Interface used by objects that support Unloading. Unlike disposed objects, unloaded objects
  /// can continue to be used and any required internal resources are recreated as needed.
  /// </summary>
  public interface IUnloadable
  {
    /// <summary>
    /// Disposes any graphics resource used internally by this object, and removes
    /// scene resources managed by this object. Commonly used during Game.UnloadContent.
    /// </summary>
    void Unload();
  }
}
