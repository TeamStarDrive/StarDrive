// Decompiled with JetBrains decompiler
// Type: SynapseGaming.LightingSystem.Rendering.IAvatar
// Assembly: SynapseGaming-SunBurn-Pro, Version=1.3.2.8, Culture=neutral, PublicKeyToken=c23c60523565dbfd
// MVID: A5F03349-72AC-4BAA-AEEE-9AB9B77E0A39
// Assembly location: C:\Projects\BlackBox\StarDrive\SynapseGaming-SunBurn-Pro.dll

using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.GamerServices;
using SynapseGaming.LightingSystem.Core;

namespace SynapseGaming.LightingSystem.Rendering
{
  /// <summary>
  /// Interface used by objects that provide properties necessary for avatar
  /// rendering and are stored in the IAvatarManager manager service.
  /// </summary>
  public interface IAvatar : IMovableObject
  {
    /// <summary>World space bounding area of the object.</summary>
    BoundingSphere WorldBoundingSphere { get; }

    /// <summary>
    /// Extended world space bounding area of the object. This area is roughly twice the size
    /// to accommodate avatar animations that fall outside the normal bounds.
    /// </summary>
    BoundingBox WorldBoundingBoxProxy { get; }

    /// <summary>
    /// Array of bone transforms for the skeleton's current pose. The matrix index is the
    /// same as the bone order used by the avatar.
    /// </summary>
    IList<Matrix> SkinBones { get; set; }

    /// <summary>The current avatar facial expression.</summary>
    AvatarExpression Expression { get; set; }

    /// <summary>AvatarRenderer used to render the avatar.</summary>
    AvatarRenderer Renderer { get; }

    /// <summary>
    /// Description of the avatar size, clothing, features, and more.
    /// </summary>
    AvatarDescription Description { get; }

    /// <summary>
    /// Defines how the avatar is rendered.
    /// 
    /// This enumeration is a Flag, which allows combining multiple values using the
    /// Logical OR operator (example: "ObjectVisibility.Rendered | ObjectVisibility.CastShadows",
    /// both renders the avatar and casts shadows from it).
    /// </summary>
    ObjectVisibility Visibility { get; set; }

    /// <summary>
    /// Determines if the avatar casts shadows base on the current ObjectVisibility options.
    /// </summary>
    bool CastShadows { get; }

    /// <summary>
    /// Determines if the avatar is visible base on the current ObjectVisibility options.
    /// </summary>
    bool Visible { get; }

    /// <summary>
    /// Sets both the avatar bone transforms and expression using an AvatarAnimation object.
    /// </summary>
    /// <param name="animation"></param>
    void ApplyAnimation(AvatarAnimation animation);
  }
}
