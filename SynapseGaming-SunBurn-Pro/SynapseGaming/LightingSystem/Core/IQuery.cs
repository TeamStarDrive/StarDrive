// Decompiled with JetBrains decompiler
// Type: SynapseGaming.LightingSystem.Core.IQuery`1
// Assembly: SynapseGaming-SunBurn-Pro, Version=1.3.2.8, Culture=neutral, PublicKeyToken=c23c60523565dbfd
// MVID: A5F03349-72AC-4BAA-AEEE-9AB9B77E0A39
// Assembly location: C:\Projects\BlackBox\StarDrive\SynapseGaming-SunBurn-Pro.dll

using System.Collections.Generic;
using Microsoft.Xna.Framework;

namespace SynapseGaming.LightingSystem.Core
{
  /// <summary>
  /// Generic interface used by container objects that implement querying
  /// for contained objects by various object attributes.
  /// </summary>
  /// <typeparam name="T"></typeparam>
  public interface IQuery<T>
  {
    /// <summary>
    /// Finds all contained objects that match a set of filter attributes
    /// and overlap with or are contained in a bounding area.
    /// </summary>
    /// <param name="foundobjects">List used to store found objects during the query.</param>
    /// <param name="worldbounds">Bounding area used to limit query results.</param>
    /// <param name="objectfilter">Filter used to limit query results to objects with specific attributes.</param>
    void Find(List<T> foundobjects, BoundingFrustum worldbounds, ObjectFilter objectfilter);

    /// <summary>
    /// Finds all contained objects that match a set of filter attributes
    /// and overlap with or are contained in a bounding area.
    /// </summary>
    /// <param name="foundobjects">List used to store found objects during the query.</param>
    /// <param name="worldbounds">Bounding area used to limit query results.</param>
    /// <param name="objectfilter">Filter used to limit query results to objects with specific attributes.</param>
    void Find(List<T> foundobjects, BoundingBox worldbounds, ObjectFilter objectfilter);

    /// <summary>
    /// Finds all contained objects that match a set of filter attributes.
    /// </summary>
    /// <param name="foundobjects">List used to store found objects during the query.</param>
    /// <param name="objectfilter">Filter used to limit query results to objects with specific attributes.</param>
    void Find(List<T> foundobjects, ObjectFilter objectfilter);

    /// <summary>
    /// Quickly finds all objects near a bounding area without the overhead of
    /// filtering by object type, checking if objects are enabled, or verifying
    /// containment within the bounds.
    /// </summary>
    /// <param name="foundobjects">List used to store found objects during the query.</param>
    /// <param name="worldbounds">Bounding area used to limit query results.</param>
    void FindFast(List<T> foundobjects, BoundingBox worldbounds);

    /// <summary>
    /// Quickly finds all objects without the overhead of filtering by object
    /// type or checking if objects are enabled.
    /// </summary>
    /// <param name="foundobjects">List used to store found objects during the query.</param>
    void FindFast(List<T> foundobjects);
  }
}
