// Decompiled with JetBrains decompiler
// Type: SynapseGaming.LightingSystem.Core.IPreferences
// Assembly: SynapseGaming-SunBurn-Pro, Version=1.3.2.8, Culture=neutral, PublicKeyToken=c23c60523565dbfd
// MVID: A5F03349-72AC-4BAA-AEEE-9AB9B77E0A39
// Assembly location: C:\Projects\BlackBox\StarDrive\SynapseGaming-SunBurn-Pro.dll

namespace SynapseGaming.LightingSystem.Core
{
  /// <summary>
  /// Interface that provides a base for all objects that load and save user preference.
  /// </summary>
  public interface IPreferences
  {
    /// <summary>Loads user preference from a file.</summary>
    /// <param name="filename"></param>
    void LoadFromFile(string filename);

    /// <summary>Saves user preference to a file.</summary>
    /// <param name="filename"></param>
    void SaveToFile(string filename);
  }
}
