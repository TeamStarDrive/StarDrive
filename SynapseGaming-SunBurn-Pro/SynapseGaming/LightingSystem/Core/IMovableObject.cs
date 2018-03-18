// Decompiled with JetBrains decompiler
// Type: SynapseGaming.LightingSystem.Core.IMovableObject
// Assembly: SynapseGaming-SunBurn-Pro, Version=1.3.2.8, Culture=neutral, PublicKeyToken=c23c60523565dbfd
// MVID: A5F03349-72AC-4BAA-AEEE-9AB9B77E0A39
// Assembly location: C:\Projects\BlackBox\StarDrive\SynapseGaming-SunBurn-Pro.dll

using Microsoft.Xna.Framework;

namespace SynapseGaming.LightingSystem.Core
{
  /// <summary>
  /// Interface used by game objects that implement moving while contained
  /// within a manager / container object.
  /// </summary>
  public interface IMovableObject
  {
    /// <summary>
    /// Indicates the object bounding area spans the entire world and
    /// the object is always visible.
    /// </summary>
    bool InfiniteBounds { get; }

    /// <summary>
    /// Indicates the current move. This value increments each time the object
    /// is moved (when the World transform changes).
    /// </summary>
    int MoveId { get; }

    /// <summary>
    /// Defines how movement is applied. Updates to Dynamic objects
    /// are automatically applied, where Static objects must be moved
    /// manually using [manager].Move().
    /// 
    /// Important note: ObjectType can be changed at any time, HOWEVER managers
    /// will only see the change after removing and resubmitting the object.
    /// </summary>
    ObjectType ObjectType { get; set; }

    /// <summary>World space transform of the object.</summary>
    Matrix World { get; set; }

    /// <summary>World space bounding area of the object.</summary>
    BoundingBox WorldBoundingBox { get; }
  }
}
