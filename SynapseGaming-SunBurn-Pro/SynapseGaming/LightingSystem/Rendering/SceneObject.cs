// Decompiled with JetBrains decompiler
// Type: SynapseGaming.LightingSystem.Rendering.SceneObject
// Assembly: SynapseGaming-SunBurn-Pro, Version=1.3.2.8, Culture=neutral, PublicKeyToken=c23c60523565dbfd
// MVID: A5F03349-72AC-4BAA-AEEE-9AB9B77E0A39
// Assembly location: C:\Projects\BlackBox\StarDrive\SynapseGaming-SunBurn-Pro.dll

using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using ns3;
using SynapseGaming.LightingSystem.Core;
using System.Collections.Generic;

namespace SynapseGaming.LightingSystem.Rendering
{
  /// <summary>
  /// Scene object implementation that uses XNA Models as a source.
  /// </summary>
  public class SceneObject : IMovableObject, INamedObject, ISceneObject
  {
    private BoundingBox boundingBox_0 = new BoundingBox();
    private BoundingSphere boundingSphere_0 = new BoundingSphere();
    private bool bool_0 = true;
    private bool bool_1 = true;
    private ObjectVisibility objectVisibility_0 = ObjectVisibility.RenderedAndCastShadows;
    private List<RenderableMesh> list_0 = new List<RenderableMesh>(16);
    private int int_0;
    private ObjectType objectType_0;
    private Matrix matrix_0;
    private Matrix matrix_1;
    private Matrix[] matrix_2;
    private bool bool_2;
    private string string_0;
    private BoundingSphere boundingSphere_1;
    private RenderableMeshCollection renderableMeshCollection_0;

    /// <summary>World space transform of the object.</summary>
    public Matrix World
    {
      get
      {
        return this.matrix_0;
      }
      set
      {
        if (this.matrix_0.Equals(value))
          return;
        this.SetWorldAndWorldToObject(value, Matrix.Invert(value));
      }
    }

    /// <summary>
    /// Indicates the object bounding area spans the entire world and
    /// the object is always visible.
    /// </summary>
    public bool InfiniteBounds
    {
      get
      {
        return this.bool_2;
      }
      protected set
      {
        this.bool_2 = value;
      }
    }

    /// <summary>
    /// Indicates the current move. This value increments each time the object
    /// is moved (when the World transform changes).
    /// </summary>
    public int MoveId
    {
      get
      {
        return this.int_0;
      }
    }

    /// <summary>
    /// Defines how movement is applied. Updates to Dynamic objects
    /// are automatically applied, where Static objects must be moved
    /// manually using [manager].Move().
    /// 
    /// Important note: ObjectType can be changed at any time, HOWEVER managers
    /// will only see the change after removing and resubmitting the object.
    /// </summary>
    public ObjectType ObjectType
    {
      get
      {
        return this.objectType_0;
      }
      set
      {
        this.objectType_0 = value;
      }
    }

    /// <summary>
    /// Array of bone transforms used to form the skeleton's current pose. The array
    /// index of a bone matrix should match the vertex buffer bone index.
    /// </summary>
    public Matrix[] SkinBones
    {
      get
      {
        return this.matrix_2;
      }
      set
      {
        this.matrix_2 = value;
      }
    }

    /// <summary>World space bounding area of the object.</summary>
    public BoundingBox WorldBoundingBox
    {
      get
      {
        return this.boundingBox_0;
      }
    }

    /// <summary>World space bounding area of the object.</summary>
    public BoundingSphere WorldBoundingSphere
    {
      get
      {
        return this.boundingSphere_0;
      }
    }

    /// <summary>Object space bounding area of the object.</summary>
    public BoundingSphere ObjectBoundingSphere
    {
      get
      {
        return this.boundingSphere_1;
      }
    }

    /// <summary>
    /// Defines how the object is rendered.
    /// 
    /// This enumeration is a Flag, which allows combining multiple values using the
    /// Logical OR operator (example: "ObjectVisibility.Rendered | ObjectVisibility.CastShadows",
    /// both renders the object and casts shadows from it).
    /// </summary>
    public ObjectVisibility Visibility
    {
      get
      {
        return this.objectVisibility_0;
      }
      set
      {
        this.objectVisibility_0 = value;
        this.bool_0 = (this.objectVisibility_0 & ObjectVisibility.CastShadows) != ObjectVisibility.None;
        this.bool_1 = (this.objectVisibility_0 & ObjectVisibility.Rendered) != ObjectVisibility.None;
      }
    }

    /// <summary>
    /// Determines if the object casts shadows base on the current ObjectVisibility options.
    /// </summary>
    public bool CastShadows
    {
      get
      {
        return this.bool_0;
      }
    }

    /// <summary>
    /// Determines if the object is visible base on the current ObjectVisibility options.
    /// </summary>
    public bool Visible
    {
      get
      {
        return this.bool_1;
      }
    }

    /// <summary>The object's current name.</summary>
    public string Name
    {
      get
      {
        return this.string_0;
      }
      set
      {
        this.string_0 = value;
      }
    }

    /// <summary>Collection of the object's internal mesh parts.</summary>
    public RenderableMeshCollection RenderableMeshes
    {
      get
      {
        return this.renderableMeshCollection_0;
      }
      protected set
      {
        this.renderableMeshCollection_0 = value;
      }
    }

    /// <summary>
    /// Default constructor for derived classes that implement their own mesh creation.
    /// </summary>
    public SceneObject()
    {
      this.method_0("");
    }

    /// <summary>Creates a new SceneObject from mesh data.</summary>
    /// <param name="meshdata"></param>
    public SceneObject(MeshData meshdata)
      : this(meshdata, "")
    {
    }

    /// <summary>Creates a new SceneObject from mesh data.</summary>
    /// <param name="meshdata"></param>
    /// <param name="name">Custom name for the object.</param>
    public SceneObject(MeshData meshdata, string name)
    {
      this.method_0(name);
      this.bool_2 = meshdata.InfiniteBounds;
      this.Add(new RenderableMesh((ISceneObject) this, meshdata.Effect, meshdata.MeshToObject, meshdata.ObjectSpaceBoundingSphere, meshdata.IndexBuffer, meshdata.VertexBuffer, meshdata.VertexDeclaration, 0, PrimitiveType.TriangleList, meshdata.PrimitiveCount, 0, meshdata.VertexCount, 0, meshdata.VertexStride));
    }

    /// <summary>
    /// Creates a new SceneObject from a user defined vertex buffer.
    /// </summary>
    /// <param name="effect">Effect applied to the mesh during rendering.</param>
    /// <param name="objectspaceboundingsphere">Smallest object space bounding sphere that
    /// completely encloses the object.</param>
    /// <param name="vertexbuffer">VertexBuffer that contains the mesh geometry.</param>
    /// <param name="vertexdeclaration">Describes the mesh vertex buffer contents.</param>
    /// <param name="vertexstart">Index into the vertex buffer that mesh geometry begins.</param>
    /// <param name="primitivetype">Primitive format the mesh geometry is stored in.</param>
    /// <param name="primitivecount">Number of primitives in the mesh geometry.</param>
    /// <param name="vertexstreamoffset">Offset in bytes from the beginning of the vertex
    /// buffer to start reading data.</param>
    /// <param name="vertexstride">Size in bytes of the elements in the vertex buffer.</param>
    /// <param name="objectspace">Mesh object-space matrix.</param>
    public SceneObject(Effect effect, BoundingSphere objectspaceboundingsphere, Matrix objectspace, VertexBuffer vertexbuffer, VertexDeclaration vertexdeclaration, PrimitiveType primitivetype, int primitivecount, int vertexstart, int vertexstreamoffset, int vertexstride)
      : this("", effect, objectspaceboundingsphere, objectspace, (IndexBuffer) null, vertexbuffer, vertexdeclaration, 0, primitivetype, primitivecount, 0, 0, vertexstreamoffset, vertexstride)
    {
    }

    /// <summary>
    /// Creates a new SceneObject from a user defined vertex buffer.
    /// </summary>
    /// <param name="name">Custom name for the object.</param>
    /// <param name="effect">Effect applied to the mesh during rendering.</param>
    /// <param name="objectspaceboundingsphere">Smallest object space bounding sphere that
    /// completely encloses the object.</param>
    /// <param name="vertexbuffer">VertexBuffer that contains the mesh geometry.</param>
    /// <param name="vertexdeclaration">Describes the mesh vertex buffer contents.</param>
    /// <param name="vertexstart">Index into the vertex buffer that mesh geometry begins.</param>
    /// <param name="primitivetype">Primitive format the mesh geometry is stored in.</param>
    /// <param name="primitivecount">Number of primitives in the mesh geometry.</param>
    /// <param name="vertexstreamoffset">Offset in bytes from the beginning of the vertex
    /// buffer to start reading data.</param>
    /// <param name="vertexstride">Size in bytes of the elements in the vertex buffer.</param>
    /// <param name="objectspace">Mesh object-space matrix.</param>
    public SceneObject(string name, Effect effect, BoundingSphere objectspaceboundingsphere, Matrix objectspace, VertexBuffer vertexbuffer, VertexDeclaration vertexdeclaration, PrimitiveType primitivetype, int primitivecount, int vertexstart, int vertexstreamoffset, int vertexstride)
      : this(name, effect, objectspaceboundingsphere, objectspace, (IndexBuffer) null, vertexbuffer, vertexdeclaration, 0, primitivetype, primitivecount, 0, 0, vertexstreamoffset, vertexstride)
    {
    }

    /// <summary>
    /// Creates a new SceneObject from a user defined vertex and index buffer.
    /// </summary>
    /// <param name="effect">Effect applied to the mesh during rendering.</param>
    /// <param name="objectspaceboundingsphere">Smallest object space bounding sphere that
    /// completely encloses the object.</param>
    /// <param name="indexbuffer">IndexBuffer that contains the mesh geometry.</param>
    /// <param name="vertexbuffer">VertexBuffer that contains the mesh geometry.</param>
    /// <param name="vertexdeclaration">Describes the mesh vertex buffer contents.</param>
    /// <param name="indexstart">Index into the index buffer that mesh geometry begins.</param>
    /// <param name="primitivetype">Primitive format the mesh geometry is stored in.</param>
    /// <param name="primitivecount">Number of primitives in the mesh geometry.</param>
    /// <param name="vertexbase">Offset added to each index in the index buffer during rendering.</param>
    /// <param name="vertexcount">Number of vertices in the vertex buffer range required to
    /// draw the mesh.  For instance, a quad rendering vertices at indices (2, 5, 6, 9) requires
    /// a vertex buffer range of 8 vertices (vertices 2 – 9 inclusive).</param>
    /// <param name="vertexstreamoffset">Offset in bytes from the beginning of the vertex
    /// buffer to start reading data.</param>
    /// <param name="vertexstride">Size in bytes of the elements in the vertex buffer.</param>
    /// <param name="objectspace">Mesh object-space matrix.</param>
    public SceneObject(Effect effect, BoundingSphere objectspaceboundingsphere, Matrix objectspace, IndexBuffer indexbuffer, VertexBuffer vertexbuffer, VertexDeclaration vertexdeclaration, int indexstart, PrimitiveType primitivetype, int primitivecount, int vertexbase, int vertexcount, int vertexstreamoffset, int vertexstride)
      : this("", effect, objectspaceboundingsphere, objectspace, indexbuffer, vertexbuffer, vertexdeclaration, indexstart, primitivetype, primitivecount, vertexbase, vertexcount, vertexstreamoffset, vertexstride)
    {
    }

    /// <summary>
    /// Creates a new SceneObject from a user defined vertex and index buffer.
    /// </summary>
    /// <param name="name">Custom name for the object.</param>
    /// <param name="effect">Effect applied to the mesh during rendering.</param>
    /// <param name="objectspaceboundingsphere">Smallest object space bounding sphere that
    /// completely encloses the object.</param>
    /// <param name="indexbuffer">IndexBuffer that contains the mesh geometry.</param>
    /// <param name="vertexbuffer">VertexBuffer that contains the mesh geometry.</param>
    /// <param name="vertexdeclaration">Describes the mesh vertex buffer contents.</param>
    /// <param name="indexstart">Index into the index buffer that mesh geometry begins.</param>
    /// <param name="primitivetype">Primitive format the mesh geometry is stored in.</param>
    /// <param name="primitivecount">Number of primitives in the mesh geometry.</param>
    /// <param name="vertexbase">Offset added to each index in the index buffer during rendering.</param>
    /// <param name="vertexcount">Number of vertices in the vertex buffer range required to
    /// draw the mesh.  For instance, a quad rendering vertices at indices (2, 5, 6, 9) requires
    /// a vertex buffer range of 8 vertices (vertices 2 – 9 inclusive).</param>
    /// <param name="vertexstreamoffset">Offset in bytes from the beginning of the vertex
    /// buffer to start reading data.</param>
    /// <param name="vertexstride">Size in bytes of the elements in the vertex buffer.</param>
    /// <param name="objectspace">Mesh object-space matrix.</param>
    public SceneObject(string name, Effect effect, BoundingSphere objectspaceboundingsphere, Matrix objectspace, IndexBuffer indexbuffer, VertexBuffer vertexbuffer, VertexDeclaration vertexdeclaration, int indexstart, PrimitiveType primitivetype, int primitivecount, int vertexbase, int vertexcount, int vertexstreamoffset, int vertexstride)
    {
      this.method_0(name);
      this.Add(new RenderableMesh((ISceneObject) this, effect, objectspace, objectspaceboundingsphere, indexbuffer, vertexbuffer, vertexdeclaration, indexstart, primitivetype, primitivecount, vertexbase, vertexcount, vertexstreamoffset, vertexstride));
    }

    /// <summary>
    /// Creates a new SceneObject constructing RenderableMeshes
    /// from all ModelMeshes within the provided Model.
    /// </summary>
    /// <param name="model"></param>
    public SceneObject(Model model)
      : this(model, model.Root.Name)
    {
    }

    /// <summary>
    /// Creates a new SceneObject constructing RenderableMeshes
    /// from the provided ModelMesh.
    /// </summary>
    /// <param name="mesh"></param>
    public SceneObject(ModelMesh mesh)
      : this(mesh, mesh.ParentBone.Name)
    {
    }

    /// <summary>
    /// Creates a new SceneObject constructing RenderableMeshes
    /// from all ModelMeshes within the provided Model.
    /// </summary>
    /// <param name="model"></param>
    /// <param name="name">Custom name for the object.</param>
    public SceneObject(Model model, string name)
    {
      this.method_0(name);
      for (int index = 0; index < model.Meshes.Count; ++index)
        this.method_3(model.Meshes[index], (Effect) null);
    }

    /// <summary>
    /// Creates a new SceneObject constructing RenderableMeshes
    /// from the provided ModelMesh.
    /// </summary>
    /// <param name="mesh"></param>
    /// <param name="name">Custom name for the object.</param>
    public SceneObject(ModelMesh mesh, string name)
    {
      this.method_0(name);
      this.method_3(mesh, (Effect) null);
    }

    /// <summary>
    /// Creates a new SceneObject constructing RenderableMeshes
    /// from all ModelMeshes within the provided Model.
    /// </summary>
    /// <param name="model"></param>
    /// <param name="overrideeffect">User defined effect used to render the object.</param>
    /// <param name="name">Custom name for the object.</param>
    public SceneObject(Model model, Effect overrideeffect, string name)
    {
      this.method_0(name);
      for (int index = 0; index < model.Meshes.Count; ++index)
        this.method_3(model.Meshes[index], overrideeffect);
    }

    /// <summary>
    /// Creates a new SceneObject constructing RenderableMeshes
    /// from the provided ModelMesh.
    /// </summary>
    /// <param name="mesh"></param>
    /// <param name="overrideeffect">User defined effect used to render the object.</param>
    /// <param name="name">Custom name for the object.</param>
    public SceneObject(ModelMesh mesh, Effect overrideeffect, string name)
    {
      this.method_0(name);
      this.method_3(mesh, overrideeffect);
    }

    private void method_0(string string_1)
    {
      this.renderableMeshCollection_0 = new RenderableMeshCollection((IList<RenderableMesh>) this.list_0);
      this.string_0 = string.IsNullOrEmpty(string_1) ? string.Empty : string_1;
      this.World = Matrix.Identity;
      this.Visibility = ObjectVisibility.RenderedAndCastShadows;
    }

    /// <summary>
    /// Adds a mesh to this object. Automatically recalculates the object bounds.
    /// </summary>
    /// <param name="mesh"></param>
    public void Add(RenderableMesh mesh)
    {
      this.list_0.Add(mesh);
      this.method_1();
    }

    /// <summary>
    /// Removes a mesh from this object. Automatically recalculates the object bounds.
    /// </summary>
    /// <param name="mesh"></param>
    public void Remove(RenderableMesh mesh)
    {
      this.list_0.Remove(mesh);
      this.method_1();
    }

    /// <summary>Removes all meshes from this object.</summary>
    public void Clear()
    {
      this.list_0.Clear();
      this.method_1();
    }

    /// <summary>
    /// Recalculates the object bounding area based on all contained meshes.
    /// 
    /// Calling this method may become necessary if a mesh bounding area is
    /// altered after being added to the object.
    /// </summary>
    public void CalculateBounds()
    {
      this.method_1();
    }

    private void method_1()
    {
      if (this.bool_2)
        this.boundingSphere_1 = new BoundingSphere(Vector3.Zero, 3.402823E+37f);
      else if (this.renderableMeshCollection_0.Count > 0)
      {
        this.boundingSphere_1 = this.renderableMeshCollection_0[0].boundingSphere_0;
        for (int index = 1; index < this.renderableMeshCollection_0.Count; ++index)
          this.boundingSphere_1 = BoundingSphere.CreateMerged(this.boundingSphere_1, this.renderableMeshCollection_0[index].boundingSphere_0);
      }
      this.method_2();
    }

    private void method_2()
    {
      this.boundingSphere_0 = !this.bool_2 ? Class13.smethod_6(this.boundingSphere_1, this.matrix_0) : new BoundingSphere(Vector3.Zero, 3.402823E+37f);
      BoundingBox fromSphere = BoundingBox.CreateFromSphere(this.boundingSphere_0);
      if (this.objectType_0 == ObjectType.Static)
      {
        this.boundingBox_0 = fromSphere;
      }
      else
      {
        if (!(fromSphere != this.boundingBox_0))
          return;
        this.boundingBox_0 = fromSphere;
        ++this.int_0;
      }
    }

    private void method_3(ModelMesh modelMesh_0, Effect effect_0)
    {
      Matrix identity = Matrix.Identity;
      for (ModelBone modelBone = modelMesh_0.ParentBone; modelBone != null; modelBone = modelBone.Parent)
        identity *= modelBone.Transform;
      BoundingSphere objectspaceboundingsphere = Class13.smethod_6(modelMesh_0.BoundingSphere, identity);
      for (int index = 0; index < modelMesh_0.MeshParts.Count; ++index)
      {
        ModelMeshPart meshPart = modelMesh_0.MeshParts[index];
        Effect effect = meshPart.Effect;
        if (effect_0 != null)
          effect = effect_0;
        this.Add(new RenderableMesh((ISceneObject) this, effect, identity, objectspaceboundingsphere, modelMesh_0.IndexBuffer, modelMesh_0.VertexBuffer, meshPart.VertexDeclaration, meshPart.StartIndex, PrimitiveType.TriangleList, meshPart.PrimitiveCount, meshPart.BaseVertex, meshPart.NumVertices, meshPart.StreamOffset, meshPart.VertexStride));
      }
    }

    /// <summary>
    /// Sets both the world and inverse world matrices.  Used to improve
    /// performance when the world matrix is set, by providing a cached
    /// or precalculated inverse matrix with the world matrix.
    /// </summary>
    /// <param name="world">World space transform of the object.</param>
    /// <param name="worldtoobj">Inverse world space transform of the object.</param>
    public void SetWorldAndWorldToObject(Matrix world, Matrix worldtoobj)
    {
      if (this.matrix_0.Equals(world))
        return;
      this.matrix_0 = world;
      this.matrix_1 = worldtoobj;
      ++this.int_0;
      this.method_2();
      for (int index = 0; index < this.renderableMeshCollection_0.Count; ++index)
        this.renderableMeshCollection_0[index].SetWorldAndWorldToObject(world, worldtoobj);
    }

    /// <summary>Returns a String that represents the current Object.</summary>
    /// <returns></returns>
    public override string ToString()
    {
      return Class13.smethod_2((INamedObject) this);
    }

    /// <summary>
    /// Helper method that creates a new SceneObject for each
    /// ModelMesh in the provided Model.
    /// </summary>
    /// <param name="model">Source Model object.</param>
    /// <param name="returnobjects">List used to store the created SceneObject objects.</param>
    public static void CreateMeshBasedObjectsFromModel(Model model, IList<SceneObject> returnobjects)
    {
      for (int index = 0; index < model.Meshes.Count; ++index)
        returnobjects.Add(new SceneObject(model.Meshes[index]));
    }
  }
}
