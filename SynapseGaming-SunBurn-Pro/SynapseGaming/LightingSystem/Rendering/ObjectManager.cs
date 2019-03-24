// Decompiled with JetBrains decompiler
// Type: SynapseGaming.LightingSystem.Rendering.ObjectManager
// Assembly: SynapseGaming-SunBurn-Pro, Version=1.3.2.8, Culture=neutral, PublicKeyToken=c23c60523565dbfd
// MVID: A5F03349-72AC-4BAA-AEEE-9AB9B77E0A39
// Assembly location: C:\Projects\BlackBox\StarDrive\SynapseGaming-SunBurn-Pro.dll

using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using SynapseGaming.LightingSystem.Core;
using SynapseGaming.LightingSystem.Editor;

namespace SynapseGaming.LightingSystem.Rendering
{
    /// <summary>Manages all scene objects in a mini scenegraph.</summary>
    public class ObjectManager : ObjectGraph<ISceneObject>, IQuery<ISceneObject>, ISubmit<ISceneObject>, IQuery<RenderableMesh>, IUnloadable, IManager, IRenderableManager, IWorldRenderableManager, IManagerService, IObjectManager
    {
        private List<ISceneObject> list_1 = new List<ISceneObject>();

        /// <summary>
        /// Gets the manager specific Type used as a unique key for storing and
        /// requesting the manager from the IManagerServiceProvider.
        /// </summary>
        public Type ManagerType => SceneInterface.ObjectManagerType;

        /// <summary>
        /// Sets the order this manager is processed relative to other managers
        /// in the IManagerServiceProvider. Managers with lower processing order
        /// values are processed first.
        /// 
        /// In the case of BeginFrameRendering and EndFrameRendering, BeginFrameRendering
        /// is processed in the normal order (lowest order value to highest), however
        /// EndFrameRendering is processed in reverse order (highest to lowest) to ensure
        /// the first manager begun is the last one ended (FILO).
        /// </summary>
        public int ManagerProcessOrder { get; set; } = 50;

        /// <summary>
        /// The current GraphicsDeviceManager used by this object.
        /// </summary>
        public IGraphicsDeviceService GraphicsDeviceManager { get; }

        /// <summary>Creates a new ObjectManager instance.</summary>
        /// <param name="graphicsdevicemanager"></param>
        /// <param name="worldboundingbox">The smallest bounding area that completely
        /// contains the scene.  Helps the RenderManager build an optimal scene tree.</param>
        /// <param name="worldtreemaxdepth"></param>
        public ObjectManager(IGraphicsDeviceService graphicsdevicemanager, BoundingBox worldboundingbox, int worldtreemaxdepth)
          : base(worldboundingbox, worldtreemaxdepth)
        {
            this.GraphicsDeviceManager = graphicsdevicemanager;
            LightingSystemEditor.RegisterOnReplaceEffect(this.method_0);
        }

        /// <summary>Creates a new ObjectManager instance.</summary>
        /// <param name="graphicsdevicemanager"></param>
        public ObjectManager(IGraphicsDeviceService graphicsdevicemanager)
        {
            this.GraphicsDeviceManager = graphicsdevicemanager;
            LightingSystemEditor.RegisterOnReplaceEffect(this.method_0);
        }

        /// <summary />
        ~ObjectManager()
        {
            LightingSystemEditor.UnregisterOnReplaceEffect(this.method_0);
        }

        /// <summary>
        /// Use to apply user quality and performance preferences to the resources managed by this object.
        /// </summary>
        /// <param name="preferences"></param>
        public virtual void ApplyPreferences(ILightingSystemPreferences preferences)
        {
        }

        /// <summary>Sets up the object prior to rendering.</summary>
        /// <param name="state"></param>
        public virtual void BeginFrameRendering(ISceneState state)
        {
            this.MoveDynamicObjects();
        }

        /// <summary>Finalizes rendering.</summary>
        public virtual void EndFrameRendering()
        {
            //SplashScreen.CheckProductActivation();
        }

        private void method_0(Effect effect_0, Effect effect_1)
        {
            this.list_1.Clear();
            this.Find(this.list_1, ObjectFilter.All);
            foreach (ISceneObject sceneObject in this.list_1)
            {
                for (int index = 0; index < sceneObject.RenderableMeshes.Count; ++index)
                    sceneObject.RenderableMeshes[index].RemapEffect();
            }
        }

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
        public void SubmitStaticSceneObject(Model model, Matrix world, ObjectVisibility visibility)
        {
            this.Submit(new SceneObject(model)
            {
                ObjectType = ObjectType.Static,
                Visibility = visibility,
                World = world
            });
        }

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
        public void SubmitStaticSceneObjectPerMesh(Model model, Matrix world, ObjectVisibility visibility)
        {
            int count = model.Meshes.Count;
            for (int index = 0; index < count; ++index)
                this.SubmitStaticSceneObject(model.Meshes[index], world, visibility);
        }

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
        public void SubmitStaticSceneObject(ModelMesh mesh, Matrix world, ObjectVisibility visibility)
        {
            this.Submit(new SceneObject(mesh)
            {
                ObjectType = ObjectType.Static,
                Visibility = visibility,
                World = world
            });
        }

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
        public void SubmitStaticSceneObject(Model model, Effect overrideeffect, Matrix world, ObjectVisibility visibility)
        {
            this.Submit(new SceneObject(model, overrideeffect, model.Root.Name)
            {
                ObjectType = ObjectType.Static,
                Visibility = visibility,
                World = world
            });
        }

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
        public void SubmitStaticSceneObjectPerMesh(Model model, Effect overrideeffect, Matrix world, ObjectVisibility visibility)
        {
            int count = model.Meshes.Count;
            for (int index = 0; index < count; ++index)
                this.SubmitStaticSceneObject(model.Meshes[index], overrideeffect, world, visibility);
        }

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
        public void SubmitStaticSceneObject(ModelMesh mesh, Effect overrideeffect, Matrix world, ObjectVisibility visibility)
        {
            this.Submit(new SceneObject(mesh, overrideeffect, mesh.ParentBone.Name)
            {
                ObjectType = ObjectType.Static,
                Visibility = visibility,
                World = world
            });
        }

        /// <summary>
        /// Finds all contained objects that match a set of filter attributes
        /// and overlap with or are contained in a bounding area.
        /// </summary>
        /// <param name="foundmeshes">List used to store found objects during the query.</param>
        /// <param name="worldbounds">Bounding area used to limit query results.</param>
        /// <param name="objectfilter">Filter used to limit query results to objects with specific attributes.</param>
        public virtual void Find(List<RenderableMesh> foundmeshes, BoundingFrustum worldbounds, ObjectFilter objectfilter)
        {
            this.list_1.Clear();
            this.Find(this.list_1, worldbounds, objectfilter);
            foreach (ISceneObject sceneObject in this.list_1)
            {
                for (int index = 0; index < sceneObject.RenderableMeshes.Count; ++index)
                    foundmeshes.Add(sceneObject.RenderableMeshes[index]);
            }
        }

        /// <summary>
        /// Finds all contained objects that match a set of filter attributes
        /// and overlap with or are contained in a bounding area.
        /// </summary>
        /// <param name="foundmeshes">List used to store found objects during the query.</param>
        /// <param name="worldbounds">Bounding area used to limit query results.</param>
        /// <param name="objectfilter">Filter used to limit query results to objects with specific attributes.</param>
        public virtual void Find(List<RenderableMesh> foundmeshes, BoundingBox worldbounds, ObjectFilter objectfilter)
        {
            this.list_1.Clear();
            this.Find(this.list_1, worldbounds, objectfilter);
            foreach (ISceneObject sceneObject in this.list_1)
            {
                for (int index = 0; index < sceneObject.RenderableMeshes.Count; ++index)
                    foundmeshes.Add(sceneObject.RenderableMeshes[index]);
            }
        }

        /// <summary>
        /// Finds all contained objects that match a set of filter attributes.
        /// </summary>
        /// <param name="foundmeshes">List used to store found objects during the query.</param>
        /// <param name="objectfilter">Filter used to limit query results to objects with specific attributes.</param>
        public virtual void Find(List<RenderableMesh> foundmeshes, ObjectFilter objectfilter)
        {
            this.list_1.Clear();
            this.Find(this.list_1, objectfilter);
            foreach (ISceneObject sceneObject in this.list_1)
            {
                for (int index = 0; index < sceneObject.RenderableMeshes.Count; ++index)
                    foundmeshes.Add(sceneObject.RenderableMeshes[index]);
            }
        }

        /// <summary>
        /// Quickly finds all objects near a bounding area without the overhead of
        /// filtering by object type, checking if objects are enabled, or verifying
        /// containment within the bounds.
        /// </summary>
        /// <param name="foundmeshes">List used to store found objects during the query.</param>
        /// <param name="worldbounds">Bounding area used to limit query results.</param>
        public void FindFast(List<RenderableMesh> foundmeshes, BoundingBox worldbounds)
        {
            this.list_1.Clear();
            this.FindFast(this.list_1, worldbounds);
            foreach (ISceneObject sceneObject in this.list_1)
            {
                for (int index = 0; index < sceneObject.RenderableMeshes.Count; ++index)
                    foundmeshes.Add(sceneObject.RenderableMeshes[index]);
            }
        }

        /// <summary>
        /// Quickly finds all objects without the overhead of filtering by object
        /// type or checking if objects are enabled.
        /// </summary>
        /// <param name="foundmeshes">List used to store found objects during the query.</param>
        public void FindFast(List<RenderableMesh> foundmeshes)
        {
            this.list_1.Clear();
            this.FindFast(this.list_1);
            foreach (ISceneObject sceneObject in this.list_1)
            {
                for (int index = 0; index < sceneObject.RenderableMeshes.Count; ++index)
                    foundmeshes.Add(sceneObject.RenderableMeshes[index]);
            }
        }

        /// <summary>
        /// Disposes any graphics resource used internally by this object, and removes
        /// scene resources managed by this object. Commonly used during Game.UnloadContent.
        /// </summary>
        public virtual void Unload()
        {
            this.Clear();
        }
    }
}
