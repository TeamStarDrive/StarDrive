// Decompiled with JetBrains decompiler
// Type: SynapseGaming.LightingSystem.Core.ObjectType
// Assembly: SynapseGaming-SunBurn-Pro, Version=1.3.2.8, Culture=neutral, PublicKeyToken=c23c60523565dbfd
// MVID: A5F03349-72AC-4BAA-AEEE-9AB9B77E0A39
// Assembly location: C:\Projects\BlackBox\StarDrive\SynapseGaming-SunBurn-Pro.dll

namespace SynapseGaming.LightingSystem.Core
{
  /// <summary>
  /// Defines how object movement is applied. Updates to Dynamic objects
  /// are automatically applied, where Static objects must be moved
  /// manually using [manager].Move().
  /// </summary>
  public enum ObjectType
  {
    Static,
    Dynamic
  }
}
