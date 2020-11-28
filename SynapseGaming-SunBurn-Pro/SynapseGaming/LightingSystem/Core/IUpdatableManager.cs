// Decompiled with JetBrains decompiler
// Type: SynapseGaming.LightingSystem.Core.IUpdatableManager
// Assembly: SynapseGaming-SunBurn-Pro, Version=1.3.2.8, Culture=neutral, PublicKeyToken=c23c60523565dbfd
// MVID: A5F03349-72AC-4BAA-AEEE-9AB9B77E0A39
// Assembly location: C:\Projects\BlackBox\StarDrive\SynapseGaming-SunBurn-Pro.dll

using Microsoft.Xna.Framework;

namespace SynapseGaming.LightingSystem.Core
{
  /// <summary>
  /// Interface used by objects managing resources that are updated based on real or game time.
  /// </summary>
  public interface IUpdatableManager : IUnloadable, IManager
  {
    /// <summary>Updates the object and its contained resources.</summary>
    /// <param name="deltaTime"></param>
    void Update(float deltaTime);
  }
}
