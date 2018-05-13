// Decompiled with JetBrains decompiler
// Type: SynapseGaming.LightingSystem.Core.LightingSystemStatisticCategory
// Assembly: SynapseGaming-SunBurn-Pro, Version=1.3.2.8, Culture=neutral, PublicKeyToken=c23c60523565dbfd
// MVID: A5F03349-72AC-4BAA-AEEE-9AB9B77E0A39
// Assembly location: C:\Projects\BlackBox\StarDrive\SynapseGaming-SunBurn-Pro.dll

using System;

namespace SynapseGaming.LightingSystem.Core
{
  /// <summary>
  /// Statistic categories used when rendering. Data is always captured even when not rendered.
  /// 
  /// This enumeration is a Flag, which allows combining multiple values using the
  /// Logical OR operator (example: "RenderCategories.Rendering | RenderCategories.Lighting",
  /// renders both rendering and lighting statistics).
  /// </summary>
  [Flags]
  public enum LightingSystemStatisticCategory
  {
    None = 0,
    Rendering = 1,
    Lighting = 2,
    Shadowing = 4,
    SceneGraph = 8,
    UserDefined1 = 65536,
    UserDefined2 = 131072,
    UserDefined3 = 262144,
    UserDefined4 = 524288,
    UserDefined5 = 1048576,
    UserDefined6 = 2097152,
    UserDefined7 = 4194304,
    UserDefined8 = 8388608,
    All = 1073741823
  }
}
