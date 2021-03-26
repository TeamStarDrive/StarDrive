// Decompiled with JetBrains decompiler
// Type: SynapseGaming.LightingSystem.Rendering.RenderableMesh
// Assembly: SynapseGaming-SunBurn-Pro, Version=1.3.2.8, Culture=neutral, PublicKeyToken=c23c60523565dbfd
// MVID: A5F03349-72AC-4BAA-AEEE-9AB9B77E0A39
// Assembly location: C:\Projects\BlackBox\StarDrive\SynapseGaming-SunBurn-Pro.dll

using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using ns3;
using SynapseGaming.LightingSystem.Core;
using SynapseGaming.LightingSystem.Effects;

namespace SynapseGaming.LightingSystem.Rendering
{
    /// <summary>
    /// Mesh class used by the built-in renderers that provides
    /// properties common to all rendering in XNA / DirectX.
    /// </summary>
    public class RenderableMesh
    {
        internal bool ShadowInFrustum = true;
        internal ISceneObject sceneObject;
        internal Matrix meshToObject;
        internal Matrix meshToObjectInverse;
        internal BoundingSphere Bounds;
        internal Matrix worldMatrix;
        internal Matrix worldToObjectMatrix;
        internal Effect effect;
        internal Matrix world;
        internal Matrix worldTranspose;
        internal Matrix worldToMesh;
        internal Matrix worldToMeshTranspose;
        internal IndexBuffer indexBuffer;
        internal VertexBuffer vertexBuffer;
        internal VertexDeclaration vertexDeclaration;
        internal int vertexStreamOffset;
        internal int stride;
        internal int vertexBase;
        internal int vertexCount;
        internal int elementStart;
        internal PrimitiveType Type;
        internal int int_5;
        internal int EffectHash;
        internal int int_7;
        internal TransparencyMode Transparency;
        internal bool AddedToEffectGroup;
        internal bool HasTransparency;
        internal bool IsDoubleSided;
        internal bool SupportsShadows;
        internal bool IsTerrain;

        /// <summary>Parent scene object this mesh is contained in.</summary>
        public ISceneObject SceneObject => sceneObject;

        /// <summary>Effect applied to the mesh during rendering.</summary>
        public Effect Effect
        {
            get => effect;
            set
            {
                effect = value;
                RemapEffect();
                CalculateMaterialInfo();
            }
        }

        /// <summary>
        /// Complete world space transform of the mesh (from mesh-space to
        /// world-space, ie: includes the mesh's object-space transform).
        /// </summary>
        public Matrix World => world;

        /// <summary>
        /// Transposed complete world space transform of the mesh (from
        /// mesh-space to world-space, ie: includes the mesh's object-space transform).
        /// </summary>
        public Matrix WorldTranspose => worldTranspose;

        /// <summary>
        /// Inverse complete world space transform of the mesh (from world-space
        /// to mesh-space, ie: includes the mesh's object-space transform).
        /// </summary>
        public Matrix WorldToMesh => worldToMesh;

        /// <summary>
        /// Transposed inverse complete world space transform of the mesh
        /// (from world-space to mesh-space, ie: includes the mesh's
        /// object-space transform).
        /// </summary>
        public Matrix WorldToMeshTranspose => worldToMeshTranspose;

        /// <summary>Object space transform of the mesh.</summary>
        public Matrix MeshToObject
        {
            get => meshToObject;
            set
            {
                meshToObject = value;
                Matrix.Invert(ref meshToObject, out meshToObjectInverse);
                DetectCullMode();
            }
        }

        /// <summary>IndexBuffer that contains the mesh geometry.</summary>
        public IndexBuffer IndexBuffer => indexBuffer;

        /// <summary>VertexBuffer that contains the mesh geometry.</summary>
        public VertexBuffer VertexBuffer => vertexBuffer;

        /// <summary>Describes the mesh vertex buffer contents.</summary>
        public VertexDeclaration VertexDeclaration => vertexDeclaration;

        /// <summary>
        /// Offset in bytes from the beginning of the vertex buffer to start reading data.
        /// </summary>
        public int VertexStreamOffset => vertexStreamOffset;

        /// <summary>Size in bytes of the elements in the vertex buffer.</summary>
        public int VertexStride => stride;

        /// <summary>
        /// Offset added to each index in the index buffer during rendering.
        /// </summary>
        public int VertexBase => vertexBase;

        /// <summary>
        /// Number of vertices in the vertex buffer range required to draw the mesh.
        /// For instance, a quad rendering vertices at indices (2, 5, 6, 9) requires
        /// a vertex buffer range of 8 vertices (vertices 2 – 9 inclusive).
        /// </summary>
        public int VertexCount => vertexCount;

        /// <summary>
        /// Index into the buffer that mesh geometry begins. For indexed meshes this
        /// is the first index in the index buffer. For non-indexed meshes this is
        /// the first vertex in the vertex buffer.
        /// </summary>
        public int ElementStart => elementStart;

        /// <summary>Primitive format the mesh geometry is stored in.</summary>
        public PrimitiveType PrimitiveType => Type;

        /// <summary>Number of primitives in the mesh geometry.</summary>
        public int PrimitiveCount => int_5;

        /// <summary>
        /// Cull mode used to ensure the mesh is rendered correctly.
        /// </summary>
        public CullMode CullMode { get; set; } = CullMode.CullCounterClockwiseFace;

        /// <summary>
        /// Object-space bounding area that completely contains the mesh.
        /// </summary>
        public BoundingSphere ObjectSpaceBoundingSphere
        {
            get => Bounds;
            set => Bounds = value;
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
            Build(sceneobject, effect, objectspace, objectspaceboundingsphere, indexbuffer, vertexbuffer, vertexdeclaration, elementstart, primitivetype, primitivecount, vertexbase, vertexcount, vertexstreamoffset, vertexstride);
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
        public void Build(ISceneObject sceneobject, Effect effect, Matrix objectspace, 
            BoundingSphere objectspaceboundingsphere,
            IndexBuffer indexbuffer, 
            VertexBuffer vertexbuffer, 
            VertexDeclaration vertexdeclaration, 
            int elementstart, 
            PrimitiveType primitivetype, 
            int primitivecount, 
            int vertexbase, 
            int vertexcount, 
            int vertexstreamoffset, 
            int vertexstride)
        {
            sceneObject = sceneobject;
            this.effect = effect;
            meshToObject = objectspace;
            Matrix.Invert(ref meshToObject, out meshToObjectInverse);
            Bounds = objectspaceboundingsphere;
            indexBuffer = indexbuffer;
            elementStart = elementstart;
            Type = primitivetype;
            int_5 = primitivecount;
            vertexBase = vertexbase;
            vertexBuffer = vertexbuffer;
            vertexCount = vertexcount;
            vertexDeclaration = vertexdeclaration;
            vertexStreamOffset = vertexstreamoffset;
            stride = vertexstride;
            if (!(this.effect is IRenderableEffect) && !(this.effect is BasicEffect))
                throw new ArgumentException("Only effects derived from IRenderableEffect and BasicEffect are supported by built-in renderers.");
            if (this.effect is ISkinnedEffect && (this.effect as ISkinnedEffect).Skinned)
            {
                VertexElement[] vertexElements = vertexDeclaration.GetVertexElements();
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
            if (this.effect == null)
                return;
            int_7 = indexBuffer == null ? CoreUtils.smethod_17(vertexBuffer.GetHashCode(), vertexStreamOffset, stride) : CoreUtils.smethod_18(indexBuffer.GetHashCode(), vertexBuffer.GetHashCode(), vertexStreamOffset, stride);
            EffectHash = this.effect.GetHashCode();
            SetWorldAndWorldToObject(Matrix.Identity, Matrix.Identity);
            CalculateMaterialInfo();
        }

        /// <summary>
        /// Recalculates the mesh shadow batching information. This may become necessary
        /// if the mesh effect changes from a non-transparent mode to transparent.
        /// </summary>
        public void CalculateMaterialInfo()
        {
            Transparency    = (effect as ITransparentEffect)?.TransparencyMode ?? TransparencyMode.None;
            HasTransparency = Transparency != TransparencyMode.None;
            IsDoubleSided   = effect is IRenderableEffect && (effect as IRenderableEffect).DoubleSided;
            SupportsShadows = effect is IShadowGenerateEffect && (effect as IShadowGenerateEffect).SupportsShadowGeneration;
            IsTerrain       = effect is ITerrainEffect;
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
            worldMatrix = world;
            worldToObjectMatrix = worldtoobject;
            DetectCullMode();
        }

        private void DetectCullMode()
        {
            Matrix.Multiply(ref meshToObject, ref worldMatrix, out world);
            Matrix.Transpose(ref world, out worldTranspose);
            Matrix.Multiply(ref worldToObjectMatrix, ref meshToObjectInverse, out worldToMesh);
            Matrix.Transpose(ref worldToMesh, out worldToMeshTranspose);

            CullMode = world.Determinant() >= 0.0 ? CullMode.CullCounterClockwiseFace : CullMode.CullClockwiseFace;
        }
    }
}
