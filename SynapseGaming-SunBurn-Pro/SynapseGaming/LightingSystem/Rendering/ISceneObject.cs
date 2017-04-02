// Decompiled with JetBrains decompiler
// Type: SynapseGaming.LightingSystem.Rendering.ISceneObject
// Assembly: SynapseGaming-SunBurn-Pro, Version=1.3.2.8, Culture=neutral, PublicKeyToken=c23c60523565dbfd
// MVID: A5F03349-72AC-4BAA-AEEE-9AB9B77E0A39
// Assembly location: C:\Projects\BlackBox\StarDrive\SynapseGaming-SunBurn-Pro.dll

using Microsoft.Xna.Framework;
using SynapseGaming.LightingSystem.Core;

namespace SynapseGaming.LightingSystem.Rendering
{
  /// <summary>
  /// Interface used by objects contained within the IObjectManager
  /// manager service.
  /// 
  /// In many cases these object are renderable, however non-renderable
  /// objects can also use this interface and be stored within the
  /// IObjectManager manager service.
  /// </summary>
  public interface ISceneObject : IMovableObject, INamedObject
  {
    /// <summary>
    /// Determines if the object casts shadows base on the current ObjectVisibility options.
    /// </summary>
    bool CastShadows { get; }

    /// <summary>
    /// Determines if the object is visible base on the current ObjectVisibility options.
    /// </summary>
    bool Visible { get; }

    /// <summary>
    /// Defines how the object is rendered.
    /// 
    /// This enumeration is a Flag, which allows combining multiple values using the
    /// Logical OR operator (example: "ObjectVisibility.Rendered | ObjectVisibility.CastShadows",
    /// both renders the object and casts shadows from it).
    /// </summary>
    ObjectVisibility Visibility { get; set; }

    /// <summary>
    /// Array of bone transforms used to form the skeleton's current pose. The array
    /// index of a bone matrix should match the vertex buffer bone index.
    /// </summary>
    Matrix[] SkinBones { get; set; }

    /// <summary>World space bounding area of the object.</summary>
    BoundingSphere WorldBoundingSphere { get; }

    /// <summary>Collection of the object's internal mesh parts.</summary>
    RenderableMeshCollection RenderableMeshes { get; }

    /// <summary>
    /// Sets both the world and inverse world matrices.  Used to improve
    /// performance when the world matrix is set, by providing a cached
    /// or precalculated inverse matrix with the world matrix.
    /// </summary>
    /// <param name="world">World space transform of the object.</param>
    /// <param name="worldtoobj">Inverse world space transform of the object.</param>
    void SetWorldAndWorldToObject(Matrix world, Matrix worldtoobj);
  }
}
