// Decompiled with JetBrains decompiler
// Type: SynapseGaming.LightingSystem.Core.ObjectVisibility
// Assembly: SynapseGaming-SunBurn-Pro, Version=1.3.2.8, Culture=neutral, PublicKeyToken=c23c60523565dbfd
// MVID: A5F03349-72AC-4BAA-AEEE-9AB9B77E0A39
// Assembly location: C:\Projects\BlackBox\StarDrive\SynapseGaming-SunBurn-Pro.dll

using System;

namespace SynapseGaming.LightingSystem.Core
{
  /// <summary>
  /// Defines how objects are rendered.
  /// 
  /// This enumeration is a Flag, which allows combining multiple values using the
  /// Logical OR operator (example: "ObjectVisibility.Rendered | ObjectVisibility.CastShadows",
  /// both renders objects and casts shadows from them).
  /// </summary>
  [Flags]
  public enum ObjectVisibility
  {
    None = 0,
    Rendered = 1,
    CastShadows = 2,
    RenderedAndCastShadows = CastShadows | Rendered,
  }
}
