// Decompiled with JetBrains decompiler
// Type: SynapseGaming.LightingSystem.Core.INamedObject
// Assembly: SynapseGaming-SunBurn-Pro, Version=1.3.2.8, Culture=neutral, PublicKeyToken=c23c60523565dbfd
// MVID: A5F03349-72AC-4BAA-AEEE-9AB9B77E0A39
// Assembly location: C:\Projects\BlackBox\StarDrive\SynapseGaming-SunBurn-Pro.dll

namespace SynapseGaming.LightingSystem.Core
{
  /// <summary>
  /// Interface used by game objects that expose a string name.
  /// </summary>
  public interface INamedObject
  {
    /// <summary>The object's current name.</summary>
    string Name { get; set; }
  }
}
