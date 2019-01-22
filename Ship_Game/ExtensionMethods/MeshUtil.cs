using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using SynapseGaming.LightingSystem.Rendering;

namespace Ship_Game
{
    public static class MeshUtil
    {
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
            var bb = new BoundingBox();

            VertexDeclaration desc = mesh.VertexDeclaration;
            VertexElement position = desc.GetVertexElements()[0];
            int stride = desc.GetVertexStrideSize(position.Stream);
            Log.Assert(position.VertexElementUsage == VertexElementUsage.Position, "Expected Vertex3 Position");

            var vertexData  = new Vector3[mesh.VertexCount];
            mesh.VertexBuffer.GetData(0, vertexData, 0, mesh.VertexCount, stride);

            foreach (Vector3 p in vertexData)
            {
                if (p.X < bb.Min.X) bb.Min.X = p.X;
                if (p.Y < bb.Min.Y) bb.Min.Y = p.Y;
                if (p.Z < bb.Min.Z) bb.Min.Z = p.Z;

                if (p.X > bb.Max.X) bb.Max.X = p.X;
                if (p.Y > bb.Max.Y) bb.Max.Y = p.Y;
                if (p.Z > bb.Max.Z) bb.Max.Z = p.Z;
            }

            Matrix m = mesh.MeshToObject;
            bb.Min = Vector3.Transform(bb.Min, m);
            bb.Max = Vector3.Transform(bb.Max, m);
            return bb;
        }

        
        public static BoundingBox GetMeshBoundingBox(this ModelMesh modelMesh)
        {
            var bb = new BoundingBox();

            ModelMeshPart mesh = modelMesh.MeshParts[0];

            VertexDeclaration desc = mesh.VertexDeclaration;
            VertexElement position = desc.GetVertexElements()[0];
            int stride = desc.GetVertexStrideSize(position.Stream);
            Log.Assert(position.VertexElementUsage == VertexElementUsage.Position, "Expected Vertex3 Position");

            var vertexData  = new Vector3[mesh.NumVertices];
            modelMesh.VertexBuffer.GetData(0, vertexData, 0, mesh.NumVertices, stride);

            foreach (Vector3 p in vertexData)
            {
                if (p.X < bb.Min.X) bb.Min.X = p.X;
                if (p.Y < bb.Min.Y) bb.Min.Y = p.Y;
                if (p.Z < bb.Min.Z) bb.Min.Z = p.Z;

                if (p.X > bb.Max.X) bb.Max.X = p.X;
                if (p.Y > bb.Max.Y) bb.Max.Y = p.Y;
                if (p.Z > bb.Max.Z) bb.Max.Z = p.Z;
            }
            return bb;
        }
    }
}
