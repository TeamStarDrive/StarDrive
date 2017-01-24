// Decompiled with JetBrains decompiler
// Type: SynapseGaming.LightingSystem.Rendering.IObjectManager
// Assembly: SynapseGaming-SunBurn-Pro, Version=1.3.2.8, Culture=neutral, PublicKeyToken=c23c60523565dbfd
// MVID: A5F03349-72AC-4BAA-AEEE-9AB9B77E0A39
// Assembly location: C:\Projects\BlackBox\StarDrive\SynapseGaming-SunBurn-Pro.dll

using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using SynapseGaming.LightingSystem.Core;

namespace SynapseGaming.LightingSystem.Rendering
{
  /// <summary>
  /// Interface that provides access to the scene's object manager. The object manager
  /// provides methods for storing and querying scene objects.
  /// </summary>
  public interface IObjectManager : IQuery<ISceneObject>, ISubmit<ISceneObject>, IQuery<RenderableMesh>, IUnloadable, IManager, IRenderableManager, IWorldRenderableManager, IManagerService
  {
    /// <summary>
    /// Helper method that creates and submits a static scene
    /// object using a method layout similar to SunBurn 1.2.x.
    /// 
    /// NOTE: This method creates a single scene object for an
    /// entire model.  This is ideal for small models such as
    /// props, however large models that represent entire rooms
    /// or scenes need to be split into separate objects per
    /// model mesh using SubmitStaticSceneObjectPerMesh.
    /// </summary>
    /// <param name="model">Source model.</param>
    /// <param name="world">Scene object world transform.</param>
    /// <param name="visibility">Defines how the object is rendered.
    /// This enumeration is a Flag, which allows combining multiple values using the
    /// Logical OR operator (example: "ObjectVisibility.Rendered | ObjectVisibility.CastShadows",
    /// both renders objects and casts shadows from them).</param>
    void SubmitStaticSceneObject(Model model, Matrix world, ObjectVisibility visibility);

    /// <summary>
    /// Helper method that creates and submits a static scene
    /// object using a method layout similar to SunBurn 1.2.x.
    /// 
    /// NOTE: This method creates a scene object for each mesh
    /// contained in the model.  This is ideal for large models
    /// that represent entire rooms or scenes, however small
    /// models such as props should be contained in a single scene
    /// object using SubmitStaticSceneObject.
    /// </summary>
    /// <param name="model">Source model.</param>
    /// <param name="world">Scene object world transform.</param>
    /// <param name="visibility">Defines how the object is rendered.
    /// This enumeration is a Flag, which allows combining multiple values using the
    /// Logical OR operator (example: "ObjectVisibility.Rendered | ObjectVisibility.CastShadows",
    /// both renders objects and casts shadows from them).</param>
    void SubmitStaticSceneObjectPerMesh(Model model, Matrix world, ObjectVisibility visibility);

    /// <summary>
    /// Helper method that creates and submits a static scene
    /// object using a method layout similar to SunBurn 1.2.x.
    /// </summary>
    /// <param name="mesh">Source model mesh.</param>
    /// <param name="world">Scene object world transform.</param>
    /// <param name="visibility">Defines how the object is rendered.
    /// This enumeration is a Flag, which allows combining multiple values using the
    /// Logical OR operator (example: "ObjectVisibility.Rendered | ObjectVisibility.CastShadows",
    /// both renders objects and casts shadows from them).</param>
    void SubmitStaticSceneObject(ModelMesh mesh, Matrix world, ObjectVisibility visibility);

    /// <summary>
    /// Helper method that creates and submits a static scene
    /// object using a method layout similar to SunBurn 1.2.x.
    /// 
    /// NOTE: This method creates a single scene object for an
    /// entire model.  This is ideal for small models such as
    /// props, however large models that represent entire rooms
    /// or scenes need to be split into separate objects per
    /// model mesh using SubmitStaticSceneObjectPerMesh.
    /// </summary>
    /// <param name="model">Source model.</param>
    /// <param name="overrideeffect">User defined effect used to render the object.</param>
    /// <param name="world">Scene object world transform.</param>
    /// <param name="visibility">Defines how the object is rendered.
    /// This enumeration is a Flag, which allows combining multiple values using the
    /// Logical OR operator (example: "ObjectVisibility.Rendered | ObjectVisibility.CastShadows",
    /// both renders objects and casts shadows from them).</param>
    void SubmitStaticSceneObject(Model model, Effect overrideeffect, Matrix world, ObjectVisibility visibility);

    /// <summary>
    /// Helper method that creates and submits a static scene
    /// object using a method layout similar to SunBurn 1.2.x.
    /// 
    /// NOTE: This method creates a scene object for each mesh
    /// contained in the model.  This is ideal for large models
    /// that represent entire rooms or scenes, however small
    /// models such as props should be contained in a single scene
    /// object using SubmitStaticSceneObject.
    /// </summary>
    /// <param name="model">Source model.</param>
    /// <param name="overrideeffect">User defined effect used to render the object.</param>
    /// <param name="world">Scene object world transform.</param>
    /// <param name="visibility">Defines how the object is rendered.
    /// This enumeration is a Flag, which allows combining multiple values using the
    /// Logical OR operator (example: "ObjectVisibility.Rendered | ObjectVisibility.CastShadows",
    /// both renders objects and casts shadows from them).</param>
    void SubmitStaticSceneObjectPerMesh(Model model, Effect overrideeffect, Matrix world, ObjectVisibility visibility);

    /// <summary>
    /// Helper method that creates and submits a static scene
    /// object using a method layout similar to SunBurn 1.2.x.
    /// </summary>
    /// <param name="mesh">Source model mesh.</param>
    /// <param name="overrideeffect">User defined effect used to render the object.</param>
    /// <param name="world">Scene object world transform.</param>
    /// <param name="visibility">Defines how the object is rendered.
    /// This enumeration is a Flag, which allows combining multiple values using the
    /// Logical OR operator (example: "ObjectVisibility.Rendered | ObjectVisibility.CastShadows",
    /// both renders objects and casts shadows from them).</param>
    void SubmitStaticSceneObject(ModelMesh mesh, Effect overrideeffect, Matrix world, ObjectVisibility visibility);

    /// <summary>
    /// Removes all objects from the container. Commonly used while clearing the scene.
    /// </summary>
    new void Clear();
  }
}
