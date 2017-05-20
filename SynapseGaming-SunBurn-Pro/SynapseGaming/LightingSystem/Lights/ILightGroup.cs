// Decompiled with JetBrains decompiler
// Type: SynapseGaming.LightingSystem.Lights.ILightGroup
// Assembly: SynapseGaming-SunBurn-Pro, Version=1.3.2.8, Culture=neutral, PublicKeyToken=c23c60523565dbfd
// MVID: A5F03349-72AC-4BAA-AEEE-9AB9B77E0A39
// Assembly location: C:\Projects\BlackBox\StarDrive\SynapseGaming-SunBurn-Pro.dll

using System.Collections.Generic;
using Microsoft.Xna.Framework;
using SynapseGaming.LightingSystem.Core;
using SynapseGaming.LightingSystem.Shadows;

namespace SynapseGaming.LightingSystem.Lights
{
  /// <summary>
  /// Interface used for light groups, which help organizing scene lights within a rig.
  /// </summary>
  public interface ILightGroup : INamedObject, IEditorObject, IShadowSource
  {
    /// <summary>
    /// Determines if the group acts as a shared shadow source for all contained
    /// lights. This allows a considerable performance increase over per-light shadows.
    /// </summary>
    bool ShadowGroup { get; set; }

    /// <summary>Shadow source location when used as a shadow group.</summary>
    Vector3 Position { get; set; }

    /// <summary>Readonly list of the contained lights.</summary>
    IList<ILight> Lights { get; }

    /// <summary>Adds a light to the group.</summary>
    /// <param name="light"></param>
    void Add(ILight light);

    /// <summary>Removes a light to the group.</summary>
    /// <param name="light"></param>
    void Remove(ILight light);

    /// <summary>Removes the light at a specific index.</summary>
    /// <param name="index"></param>
    void RemoveAt(int index);

    /// <summary>Removes all lights from the group.</summary>
    void Clear();
  }
}
