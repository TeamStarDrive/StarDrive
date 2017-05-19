// Decompiled with JetBrains decompiler
// Type: SynapseGaming.LightingSystem.Lights.ILightRig
// Assembly: SynapseGaming-SunBurn-Pro, Version=1.3.2.8, Culture=neutral, PublicKeyToken=c23c60523565dbfd
// MVID: A5F03349-72AC-4BAA-AEEE-9AB9B77E0A39
// Assembly location: C:\Projects\BlackBox\StarDrive\SynapseGaming-SunBurn-Pro.dll

using System.Collections.Generic;
using SynapseGaming.LightingSystem.Core;

namespace SynapseGaming.LightingSystem.Lights
{
  /// <summary>
  /// Interface used for light rigs, which act as containers for storing, sharing, and organizing scene lights.
  /// </summary>
  public interface ILightRig : IQuery<ILight>, INamedObject, IEditorObject, ILightQuery
  {
    /// <summary>Light groups contained by the light rig.</summary>
    List<ILightGroup> LightGroups { get; }

    /// <summary>
    /// Applies changes made to contained lights and groups to the light rig's
    /// internal scenegraph.  This must be called after making changes and before
    /// rendering the light rig.
    /// </summary>
    void CommitChanges();

    /// <summary>Removes all lights and light groups.</summary>
    void Clear();

    /// <summary />
    void Save();
  }
}
