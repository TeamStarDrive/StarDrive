using Microsoft.Xna.Framework;
using SynapseGaming.LightingSystem.Rendering;

namespace Ship_Game
{
    public static class MeshUtil
    {
        public static BoundingBox GetMeshBoundingBox(this SceneObject obj)
        {
            BoundingBox bb = new BoundingBox();

            foreach (RenderableMesh mesh in obj.RenderableMeshes)
            {
                var vertexData = new Vector3[mesh.VertexCount];
                mesh.VertexBuffer.GetData(vertexData, 0, mesh.VertexCount);

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
                break;
            }
            return bb;
        }
    }
}
