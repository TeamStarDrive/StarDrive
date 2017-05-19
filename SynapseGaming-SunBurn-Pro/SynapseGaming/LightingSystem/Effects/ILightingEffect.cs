// Decompiled with JetBrains decompiler
// Type: SynapseGaming.LightingSystem.Effects.ILightingEffect
// Assembly: SynapseGaming-SunBurn-Pro, Version=1.3.2.8, Culture=neutral, PublicKeyToken=c23c60523565dbfd
// MVID: A5F03349-72AC-4BAA-AEEE-9AB9B77E0A39
// Assembly location: C:\Projects\BlackBox\StarDrive\SynapseGaming-SunBurn-Pro.dll

using System.Collections.Generic;
using SynapseGaming.LightingSystem.Lights;

namespace SynapseGaming.LightingSystem.Effects
{
  /// <summary>
  /// Interface that provides custom effects with RenderManager lighting support.
  /// </summary>
  public interface ILightingEffect
  {
    /// <summary>Maximum number of light sources the effect supports.</summary>
    int MaxLightSources { get; }

    /// <summary>
    /// Light sources that apply lighting to the effect during rendering.
    /// </summary>
    List<ILight> LightSources { set; }
  }
}
