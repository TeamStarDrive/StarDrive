// Decompiled with JetBrains decompiler
// Type: SynapseGaming.LightingSystem.Rendering.RenderableMesh
// Assembly: SynapseGaming-SunBurn-Pro, Version=1.3.2.8, Culture=neutral, PublicKeyToken=c23c60523565dbfd
// MVID: A5F03349-72AC-4BAA-AEEE-9AB9B77E0A39
// Assembly location: C:\Projects\BlackBox\StarDrive\SynapseGaming-SunBurn-Pro.dll

using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using ns3;
using SynapseGaming.LightingSystem.Core;
using SynapseGaming.LightingSystem.Effects;
using System;

namespace SynapseGaming.LightingSystem.Rendering
{
  /// <summary>
  /// Mesh class used by the built-in renderers that provides
  /// properties common to all rendering in XNA / DirectX.
  /// </summary>
  public class RenderableMesh
  {
    internal CullMode cullMode_0 = CullMode.CullCounterClockwiseFace;
    internal bool bool_0 = true;
    internal ISceneObject isceneObject_0;
    internal Matrix matrix_0;
    internal Matrix matrix_1;
    internal BoundingSphere boundingSphere_0;
    internal Matrix matrix_2;
    internal Matrix matrix_3;
    internal Effect effect_0;
    internal Matrix matrix_4;
    internal Matrix matrix_5;
    internal Matrix matrix_6;
    internal Matrix matrix_7;
    internal IndexBuffer indexBuffer_0;
    internal VertexBuffer vertexBuffer_0;
    internal VertexDeclaration vertexDeclaration_0;
    internal int int_0;
    internal int int_1;
    internal int int_2;
    internal int int_3;
    internal int int_4;
    internal PrimitiveType primitiveType_0;
    internal int int_5;
    internal int int_6;
    internal int int_7;
    internal TransparencyMode transparencyMode_0;
    internal bool bool_1;
    internal bool bool_2;
    internal bool bool_3;
    internal bool bool_4;
    internal bool bool_5;

    /// <summary>Parent scene object this mesh is contained in.</summary>
    public ISceneObject SceneObject
    {
      get
      {
        return this.isceneObject_0;
      }
    }

    /// <summary>Effect applied to the mesh during rendering.</summary>
    public Effect Effect
    {
      get
      {
        return this.effect_0;
      }
      set
      {
        this.effect_0 = value;
        this.RemapEffect();
        this.CalculateMaterialInfo();
      }
    }

    /// <summary>
    /// Complete world space transform of the mesh (from mesh-space to
    /// world-space, ie: includes the mesh's object-space transform).
    /// </summary>
    public Matrix World
    {
      get
      {
        return this.matrix_4;
      }
    }

    /// <summary>
    /// Transposed complete world space transform of the mesh (from
    /// mesh-space to world-space, ie: includes the mesh's object-space transform).
    /// </summary>
    public Matrix WorldTranspose
    {
      get
      {
        return this.matrix_5;
      }
    }

    /// <summary>
    /// Inverse complete world space transform of the mesh (from world-space
    /// to mesh-space, ie: includes the mesh's object-space transform).
    /// </summary>
    public Matrix WorldToMesh
    {
      get
      {
        return this.matrix_6;
      }
    }

    /// <summary>
    /// Transposed inverse complete world space transform of the mesh
    /// (from world-space to mesh-space, ie: includes the mesh's
    /// object-space transform).
    /// </summary>
    public Matrix WorldToMeshTranspose
    {
      get
      {
        return this.matrix_7;
      }
    }

    /// <summary>Object space transform of the mesh.</summary>
    public Matrix MeshToObject
    {
      get
      {
        return this.matrix_0;
      }
      set
      {
        this.matrix_0 = value;
        Matrix.Invert(ref this.matrix_0, out this.matrix_1);
        this.method_0();
      }
    }

    /// <summary>IndexBuffer that contains the mesh geometry.</summary>
    public IndexBuffer IndexBuffer
    {
      get
      {
        return this.indexBuffer_0;
      }
    }

    /// <summary>VertexBuffer that contains the mesh geometry.</summary>
    public VertexBuffer VertexBuffer
    {
      get
      {
        return this.vertexBuffer_0;
      }
    }

    /// <summary>Describes the mesh vertex buffer contents.</summary>
    public VertexDeclaration VertexDeclaration
    {
      get
      {
        return this.vertexDeclaration_0;
      }
    }

    /// <summary>
    /// Offset in bytes from the beginning of the vertex buffer to start reading data.
    /// </summary>
    public int VertexStreamOffset
    {
      get
      {
        return this.int_0;
      }
    }

    /// <summary>Size in bytes of the elements in the vertex buffer.</summary>
    public int VertexStride
    {
      get
      {
        return this.int_1;
      }
    }

    /// <summary>
    /// Offset added to each index in the index buffer during rendering.
    /// </summary>
    public int VertexBase
    {
      get
      {
        return this.int_2;
      }
    }

    /// <summary>
    /// Number of vertices in the vertex buffer range required to draw the mesh.
    /// For instance, a quad rendering vertices at indices (2, 5, 6, 9) requires
    /// a vertex buffer range of 8 vertices (vertices 2 – 9 inclusive).
    /// </summary>
    public int VertexCount
    {
      get
      {
        return this.int_3;
      }
    }

    /// <summary>
    /// Index into the buffer that mesh geometry begins. For indexed meshes this
    /// is the first index in the index buffer. For non-indexed meshes this is
    /// the first vertex in the vertex buffer.
    /// </summary>
    public int ElementStart
    {
      get
      {
        return this.int_4;
      }
    }

    /// <summary>Primitive format the mesh geometry is stored in.</summary>
    public PrimitiveType PrimitiveType
    {
      get
      {
        return this.primitiveType_0;
      }
    }

    /// <summary>Number of primitives in the mesh geometry.</summary>
    public int PrimitiveCount
    {
      get
      {
        return this.int_5;
      }
    }

    /// <summary>
    /// Cull mode used to ensure the mesh is rendered correctly.
    /// </summary>
    public CullMode CullMode
    {
      get
      {
        return this.cullMode_0;
      }
    }

    /// <summary>
    /// Object-space bounding area that completely contains the mesh.
    /// </summary>
    public BoundingSphere ObjectSpaceBoundingSphere
    {
      get
      {
        return this.boundingSphere_0;
      }
      set
      {
        this.boundingSphere_0 = value;
      }
    }

    /// <summary>
    /// Creates an empty RenderableMesh instance.
    /// 
    /// Warning: Build must be called to finish constructing the mesh before
    /// attempting to render it.
    /// </summary>
    public RenderableMesh()
    {
    }

    /// <summary>Creates a new RenderableMesh instance.</summary>
    /// <param name="sceneobject">Parent scene object.</param>
    /// <param name="effect">Effect applied to the mesh during rendering.</param>
    /// <param name="indexbuffer">IndexBuffer that contains the mesh geometry.</param>
    /// <param name="vertexbuffer">VertexBuffer that contains the mesh geometry.</param>
    /// <param name="vertexdeclaration">Describes the mesh vertex buffer contents.</param>
    /// <param name="elementstart">Index into the buffer that mesh geometry begins. For indexed meshes this
    /// is the first index in the index buffer. For non-indexed meshes this is
    /// the first vertex in the vertex buffer.</param>
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
    /// <param name="objectspaceboundingsphere">Object-space bounding area that completely contains the mesh.</param>
    public RenderableMesh(ISceneObject sceneobject, Effect effect, Matrix objectspace, BoundingSphere objectspaceboundingsphere, IndexBuffer indexbuffer, VertexBuffer vertexbuffer, VertexDeclaration vertexdeclaration, int elementstart, PrimitiveType primitivetype, int primitivecount, int vertexbase, int vertexcount, int vertexstreamoffset, int vertexstride)
    {
      this.Build(sceneobject, effect, objectspace, objectspaceboundingsphere, indexbuffer, vertexbuffer, vertexdeclaration, elementstart, primitivetype, primitivecount, vertexbase, vertexcount, vertexstreamoffset, vertexstride);
    }

    /// <summary>Updates the mesh with new effect and geometry data.</summary>
    /// <param name="sceneobject">Parent scene object.</param>
    /// <param name="effect">Effect applied to the mesh during rendering.</param>
    /// <param name="indexbuffer">IndexBuffer that contains the mesh geometry.</param>
    /// <param name="vertexbuffer">VertexBuffer that contains the mesh geometry.</param>
    /// <param name="vertexdeclaration">Describes the mesh vertex buffer contents.</param>
    /// <param name="elementstart">Index into the buffer that mesh geometry begins. For indexed meshes this
    /// is the first index in the index buffer. For non-indexed meshes this is
    /// the first vertex in the vertex buffer.</param>
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
    /// <param name="objectspaceboundingsphere">Object-space bounding area that completely contains the mesh.</param>
    public void Build(ISceneObject sceneobject, Effect effect, Matrix objectspace, BoundingSphere objectspaceboundingsphere, IndexBuffer indexbuffer, VertexBuffer vertexbuffer, VertexDeclaration vertexdeclaration, int elementstart, PrimitiveType primitivetype, int primitivecount, int vertexbase, int vertexcount, int vertexstreamoffset, int vertexstride)
    {
      this.isceneObject_0 = sceneobject;
      this.effect_0 = effect;
      this.matrix_0 = objectspace;
      Matrix.Invert(ref this.matrix_0, out this.matrix_1);
      this.boundingSphere_0 = objectspaceboundingsphere;
      this.indexBuffer_0 = indexbuffer;
      this.int_4 = elementstart;
      this.primitiveType_0 = primitivetype;
      this.int_5 = primitivecount;
      this.int_2 = vertexbase;
      this.vertexBuffer_0 = vertexbuffer;
      this.int_3 = vertexcount;
      this.vertexDeclaration_0 = vertexdeclaration;
      this.int_0 = vertexstreamoffset;
      this.int_1 = vertexstride;
      if (!(this.effect_0 is IRenderableEffect) && !(this.effect_0 is BasicEffect))
        throw new ArgumentException("Only effects derived from IRenderableEffect and BasicEffect are supported by built-in renderers.");
      if (this.effect_0 is ISkinnedEffect && (this.effect_0 as ISkinnedEffect).Skinned)
      {
        VertexElement[] vertexElements = this.vertexDeclaration_0.GetVertexElements();
        bool flag1 = false;
        bool flag2 = false;
        for (int index = 0; index < vertexElements.Length; ++index)
        {
          if (vertexElements[index].VertexElementUsage == VertexElementUsage.BlendWeight)
            flag1 = true;
          if (vertexElements[index].VertexElementUsage == VertexElementUsage.BlendIndices)
            flag2 = true;
        }
        if (!flag1 || !flag2)
          throw new ArgumentException("Effects that implement skinning require object vertex buffers to supply both blending weight and indices in the vertex stream.");
      }
      if (this.effect_0 == null)
        return;
      this.int_7 = this.indexBuffer_0 == null ? Class13.smethod_17(this.vertexBuffer_0.GetHashCode(), this.int_0, this.int_1) : Class13.smethod_18(this.indexBuffer_0.GetHashCode(), this.vertexBuffer_0.GetHashCode(), this.int_0, this.int_1);
      this.int_6 = this.effect_0 == null ? 0 : this.effect_0.GetHashCode();
      this.SetWorldAndWorldToObject(Matrix.Identity, Matrix.Identity);
      this.CalculateMaterialInfo();
    }

    /// <summary>
    /// Recalculates the mesh shadow batching information. This may become necessary
    /// if the mesh effect changes from a non-transparent mode to transparent.
    /// </summary>
    public void CalculateMaterialInfo()
    {
      this.transparencyMode_0 = !(this.effect_0 is ITransparentEffect) ? TransparencyMode.None : (this.effect_0 as ITransparentEffect).TransparencyMode;
      this.bool_2 = this.transparencyMode_0 != TransparencyMode.None;
      this.bool_3 = this.effect_0 is IRenderableEffect && (this.effect_0 as IRenderableEffect).DoubleSided;
      this.bool_4 = this.effect_0 is IShadowGenerateEffect && (this.effect_0 as IShadowGenerateEffect).SupportsShadowGeneration;
      this.bool_5 = this.effect_0 is ITerrainEffect;
    }

    /// <summary>
    /// Should be called by custom renderers when receiving a ReplaceEffect event from
    /// the editor. Replaces the current effect with an editor assigned effect.
    /// </summary>
    public void RemapEffect()
    {
    }

    /// <summary>
    /// Sets both the world and inverse world matrices.  Used to improve
    /// performance when the world matrix is set, by providing a cached
    /// or precalculated inverse matrix with the world matrix.
    /// 
    /// Note: the matrix should only contain the objectToWorld (not the meshToWorld)
    /// transform. The mesh specific meshToObject transform is applied using the
    /// MeshToObject property.
    /// </summary>
    /// <param name="world">World space transform of the object.</param>
    /// <param name="worldtoobject">Inverse world space transform of the object.</param>
    public void SetWorldAndWorldToObject(Matrix world, Matrix worldtoobject)
    {
      this.matrix_2 = world;
      this.matrix_3 = worldtoobject;
      this.method_0();
    }

    private void method_0()
    {
      Matrix.Multiply(ref this.matrix_0, ref this.matrix_2, out this.matrix_4);
      Matrix.Transpose(ref this.matrix_4, out this.matrix_5);
      Matrix.Multiply(ref this.matrix_3, ref this.matrix_1, out this.matrix_6);
      Matrix.Transpose(ref this.matrix_6, out this.matrix_7);
      if ((double) this.matrix_4.Determinant() >= 0.0)
        this.cullMode_0 = CullMode.CullCounterClockwiseFace;
      else
        this.cullMode_0 = CullMode.CullClockwiseFace;
    }
  }
}
