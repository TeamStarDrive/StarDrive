// Decompiled with JetBrains decompiler
// Type: SynapseGaming.LightingSystem.Core.ObjectFilter
// Assembly: SynapseGaming-SunBurn-Pro, Version=1.3.2.8, Culture=neutral, PublicKeyToken=c23c60523565dbfd
// MVID: A5F03349-72AC-4BAA-AEEE-9AB9B77E0A39
// Assembly location: C:\Projects\BlackBox\StarDrive\SynapseGaming-SunBurn-Pro.dll

using System;

namespace SynapseGaming.LightingSystem.Core
{
  /// <summary>
  /// Defines the types of objects that should be returned in a Find() query.
  /// 
  /// This enumeration is a Flag, which allows combining multiple values using the
  /// Logical OR operator (example: "ObjectFilter.Dynamic | ObjectFilter.Enabled",
  /// finds objects that are both dynamic and enabled).
  /// </summary>
  [Flags]
  public enum ObjectFilter
  {
    Dynamic = 1,
    Static = 2,
    Enabled = 4,
    Disabled = 8,
    DynamicAndStatic = Static | Dynamic,
    EnabledAndDisabled = Disabled | Enabled,
    EnabledDynamicAndStatic = DynamicAndStatic | Enabled,
    All = 65535,
  }
}
