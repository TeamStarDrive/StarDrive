// Decompiled with JetBrains decompiler
// Type: SynapseGaming.LightingSystem.Core.ISceneEnvironment
// Assembly: SynapseGaming-SunBurn-Pro, Version=1.3.2.8, Culture=neutral, PublicKeyToken=c23c60523565dbfd
// MVID: A5F03349-72AC-4BAA-AEEE-9AB9B77E0A39
// Assembly location: C:\Projects\BlackBox\StarDrive\SynapseGaming-SunBurn-Pro.dll

using Microsoft.Xna.Framework;
using ns4;
using SynapseGaming.LightingSystem.Editor;

namespace SynapseGaming.LightingSystem.Core
{
  /// <summary>
  /// Interface that provides a base for all scene environment objects.
  /// </summary>
  [Attribute0(true)]
  public interface ISceneEnvironment : INamedObject, IEditorObject
  {
    /// <summary>Maximum world space distance objects are visible.</summary>
    [Attribute5(2, 0.0, 2147483647.0, 2.0)]
    [Attribute1(true, Description = "Viewable Distance", HorizontalAlignment = true, MajorGrouping = 2, MinorGrouping = 3, ToolTipText = "")]
    float VisibleDistance { get; set; }

    /// <summary>Enables scene fog.</summary>
    [Attribute1(true, Description = "Fog Enabled", HorizontalAlignment = true, MajorGrouping = 1, MinorGrouping = 1, ToolTipText = "")]
    bool FogEnabled { get; set; }

    /// <summary>World space distance that fog begins.</summary>
    [Attribute5(2, 0.0, 2147483647.0, 2.0)]
    [Attribute1(true, Description = "Fog Start Distance", HorizontalAlignment = true, MajorGrouping = 2, MinorGrouping = 1, ToolTipText = "")]
    float FogStartDistance { get; set; }

    /// <summary>World space distance that fog fully obscures objects.</summary>
    [Attribute5(2, 0.0, 2147483647.0, 2.0)]
    [Attribute1(true, Description = "Fog End Distance", HorizontalAlignment = true, MajorGrouping = 2, MinorGrouping = 2, ToolTipText = "")]
    float FogEndDistance { get; set; }

    /// <summary>Color applied to scene fog.</summary>
    [Attribute1(true, ControlType = ControlType.ColorSelection, Description = "Fog Color", HorizontalAlignment = false, MajorGrouping = 1, MinorGrouping = 2, ToolTipText = "")]
    Vector3 FogColor { get; set; }

    /// <summary>
    /// World space distance that directional shadows begin fading.
    /// </summary>
    [Attribute5(2, 0.0, 2147483647.0, 2.0)]
    [Attribute1(true, Description = "Shadow Fade Start", HorizontalAlignment = true, MajorGrouping = 3, MinorGrouping = 1, ToolTipText = "")]
    float ShadowFadeStartDistance { get; set; }

    /// <summary>
    /// World space distance that directional shadows completely disappear.
    /// </summary>
    [Attribute5(2, 0.0, 2147483647.0, 2.0)]
    [Attribute1(true, Description = "Shadow Fade End", HorizontalAlignment = true, MajorGrouping = 3, MinorGrouping = 2, ToolTipText = "")]
    float ShadowFadeEndDistance { get; set; }

    /// <summary>
    /// World space distance used to include shadow casters. This allows including shadows
    /// from objects further away than the shadow fade area, for instance shadows from
    /// distant mountains.
    /// </summary>
    [Attribute1(true, Description = "Max Cast Distance", HorizontalAlignment = true, MajorGrouping = 3, MinorGrouping = 3, ToolTipText = "")]
    [Attribute5(2, 0.0, 2147483647.0, 2.0)]
    float ShadowCasterDistance { get; set; }

    /// <summary>Strength of bloom applied to the scene.</summary>
    [Attribute5(2, 0.0, 10000.0, 2.0)]
    [Attribute1(true, Description = "Bloom Amount", HorizontalAlignment = true, MajorGrouping = 4, MinorGrouping = 1, ToolTipText = "")]
    float BloomAmount { get; set; }

    /// <summary>Minimum pixel intensity required for bloom to occur.</summary>
    [Attribute1(true, Description = "Bloom Threshold", HorizontalAlignment = true, MajorGrouping = 4, MinorGrouping = 2, ToolTipText = "")]
    [Attribute5(2, 0.0, 1.0, 0.01)]
    float BloomThreshold { get; set; }

    /// <summary>Intensity of the scene exposure.</summary>
    [Attribute5(2, 0.0, 100.0, 0.1)]
    [Attribute1(true, Description = "Exposure Amount", HorizontalAlignment = true, MajorGrouping = 4, MinorGrouping = 3, ToolTipText = "")]
    float ExposureAmount { get; set; }

    /// <summary>
    /// Time required to fully adjust High Dynamic Range to lighting changes.
    /// </summary>
    [Attribute1(true, Description = "Transition Time", HorizontalAlignment = true, MajorGrouping = 5, MinorGrouping = 3, ToolTipText = "")]
    [Attribute5(2, 0.0, 10000.0, 0.1)]
    float DynamicRangeTransitionTime { get; set; }

    /// <summary>
    /// Maximum intensity increase allowed for High Dynamic Range. Limits intensity
    /// increases, which sets the darkness-level where the scene will remain dark.
    /// </summary>
    [Attribute1(true, Description = "HDR Transition Max", HorizontalAlignment = true, MajorGrouping = 5, MinorGrouping = 1, ToolTipText = "")]
    [Attribute5(2, 0.0, 10000.0, 0.5)]
    float DynamicRangeTransitionMaxScale { get; set; }

    /// <summary>
    /// Maximum intensity decrease allowed for High Dynamic Range. Limits intensity
    /// decreases, which sets the brightness-level where the scene will remain overly bright.
    /// </summary>
    [Attribute5(2, 0.0, 10000.0, 0.5)]
    [Attribute1(true, Description = "HDR Transition Min", HorizontalAlignment = true, MajorGrouping = 5, MinorGrouping = 2, ToolTipText = "")]
    float DynamicRangeTransitionMinScale { get; set; }

    /// <summary>Saves the object back to its originating file.</summary>
    void Save();
  }
}
