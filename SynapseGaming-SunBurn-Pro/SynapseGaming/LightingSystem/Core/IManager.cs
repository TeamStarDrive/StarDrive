// Decompiled with JetBrains decompiler
// Type: SynapseGaming.LightingSystem.Core.IManager
// Assembly: SynapseGaming-SunBurn-Pro, Version=1.3.2.8, Culture=neutral, PublicKeyToken=c23c60523565dbfd
// MVID: A5F03349-72AC-4BAA-AEEE-9AB9B77E0A39
// Assembly location: C:\Projects\BlackBox\StarDrive\SynapseGaming-SunBurn-Pro.dll

namespace SynapseGaming.LightingSystem.Core
{
  /// <summary>Base interface for objects managing scene resources.</summary>
  public interface IManager : IUnloadable
  {
    /// <summary>
    /// Use to apply user quality and performance preferences to the resources managed by this object.
    /// </summary>
    /// <param name="preferences"></param>
    void ApplyPreferences(ILightingSystemPreferences preferences);

    /// <summary>
    /// Removes resources managed by this object. Commonly used while clearing the scene.
    /// </summary>
    void Clear();
  }
}
