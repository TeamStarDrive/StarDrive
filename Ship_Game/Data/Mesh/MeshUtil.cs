using System;
using Microsoft.Xna.Framework.Graphics;
using SynapseGaming.LightingSystem.Rendering;

using XnaVector3 = Microsoft.Xna.Framework.Vector3;
using XnaMatrix = Microsoft.Xna.Framework.Matrix;
using BoundingBox = Microsoft.Xna.Framework.BoundingBox;

namespace Ship_Game.Data.Mesh
{
    public static class MeshUtil
    {
        public static float Radius(this BoundingBox bounds)
        {
            // get all diameters of the BB
            float dx = bounds.Max.X - bounds.Min.X;
            float dy = bounds.Max.Y - bounds.Min.Y;
            float dz = bounds.Max.Z - bounds.Min.Z;

            // and pick the largest diameter
            float maxDiameter = Math.Max(dx, Math.Max(dy, dz));
            return maxDiameter * 0.5f;
        }

        // Joins two bounding boxes into a single bigger bb
        public static BoundingBox Join(this BoundingBox a, in BoundingBox b)
        {
            var bb = new BoundingBox();
            bb.Min.X = Math.Min(a.Min.X, b.Min.X);
            bb.Min.Y = Math.Min(a.Min.Y, b.Min.Y);
            bb.Min.Z = Math.Min(a.Min.Z, b.Min.Z);

            bb.Max.X = Math.Max(a.Max.X, b.Max.X);
            bb.Max.Y = Math.Max(a.Max.Y, b.Max.Y);
            bb.Max.Z = Math.Max(a.Max.Z, b.Max.Z);
            return bb;
        }

        public static BoundingBox GetMeshBoundingBox(this SceneObject obj)
        {
            foreach (RenderableMesh mesh in obj.RenderableMeshes)
            {
                return GetMeshBoundingBox(mesh);
            }
            return new BoundingBox();
        }

        public static BoundingBox GetMeshBoundingBox(this RenderableMesh mesh)
        {
            if (mesh.VertexCount == 0)
                return new BoundingBox();

            VertexDeclaration desc = mesh.VertexDeclaration;
            VertexElement position = desc.GetVertexElements()[0];
            int stride = desc.GetVertexStrideSize(position.Stream);
            Log.Assert(position.VertexElementUsage == VertexElementUsage.Position, "Expected Vertex3 Position");

            var vertexData  = new XnaVector3[mesh.VertexCount];
            mesh.VertexBuffer.GetData(0, vertexData, 0, mesh.VertexCount, stride);

            XnaVector3 p = vertexData[0];
            var bb = new BoundingBox(p, p);
            for (int i = 1; i < vertexData.Length; ++i)
            {
                p = vertexData[i];
                if (p.X < bb.Min.X) bb.Min.X = p.X;
                if (p.Y < bb.Min.Y) bb.Min.Y = p.Y;
                if (p.Z < bb.Min.Z) bb.Min.Z = p.Z;

                if (p.X > bb.Max.X) bb.Max.X = p.X;
                if (p.Y > bb.Max.Y) bb.Max.Y = p.Y;
                if (p.Z > bb.Max.Z) bb.Max.Z = p.Z;
            }

            XnaMatrix m = mesh.MeshToObject;
            bb.Min = XnaVector3.Transform(bb.Min, m);
            bb.Max = XnaVector3.Transform(bb.Max, m);
            return bb;
        }

        public static BoundingBox GetMeshBoundingBox(this ModelMesh modelMesh)
        {
            ModelMeshPart mesh = modelMesh.MeshParts[0];
            if (mesh.NumVertices == 0)
                return new BoundingBox();

            VertexDeclaration desc = mesh.VertexDeclaration;
            VertexElement position = desc.GetVertexElements()[0];
            int stride = desc.GetVertexStrideSize(position.Stream);
            Log.Assert(position.VertexElementUsage == VertexElementUsage.Position, "Expected Vertex3 Position");

            var vertexData  = new XnaVector3[mesh.NumVertices];
            modelMesh.VertexBuffer.GetData(0, vertexData, 0, mesh.NumVertices, stride);

            XnaVector3 p = vertexData[0];
            var bb = new BoundingBox(p, p);
            for (int i = 1; i < vertexData.Length; ++i)
            {
                p = vertexData[i];
                if (p.X < bb.Min.X) bb.Min.X = p.X;
                if (p.Y < bb.Min.Y) bb.Min.Y = p.Y;
                if (p.Z < bb.Min.Z) bb.Min.Z = p.Z;

                if (p.X > bb.Max.X) bb.Max.X = p.X;
                if (p.Y > bb.Max.Y) bb.Max.Y = p.Y;
                if (p.Z > bb.Max.Z) bb.Max.Z = p.Z;
            }

            if (modelMesh.ParentBone != null)
            {
                XnaMatrix m = modelMesh.ParentBone.Transform;
                bb.Min = XnaVector3.Transform(bb.Min, m);
                bb.Max = XnaVector3.Transform(bb.Max, m);
            }
            return bb;
        }

        public static BoundingBox GetBoundingBox(this Model model)
        {
            if (model.Meshes.Count == 0)
                return new BoundingBox();

            BoundingBox bb = GetMeshBoundingBox(model.Meshes[0]);
            for (int i = 1; i < model.Meshes.Count; ++i)
            {
                BoundingBox bb2 = GetMeshBoundingBox(model.Meshes[i]);
                bb = bb.Join(bb2);
            }
            return bb;
        }

        public static T[] GetArray<T>(
            this VertexBuffer vbo, ModelMeshPart part, VertexElementUsage usage) where T : struct
        {
            VertexElement[] elements = part.VertexDeclaration.GetVertexElements();
            int count  = part.NumVertices;
            int start  = part.BaseVertex;
            int stride = part.VertexStride;

            for (int i = 0; i < elements.Length; ++i)
            {
                if (elements[i].VertexElementUsage == usage)
                {
                    var data = new T[count];
                    vbo.GetData(elements[i].Offset + start*stride, data, 0, count, stride);
                    return data;
                }
            }
            return null;
        }
    }
}
