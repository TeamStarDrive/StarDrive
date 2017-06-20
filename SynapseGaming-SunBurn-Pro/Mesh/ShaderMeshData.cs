using Microsoft.Xna.Framework.Graphics;
using SynapseGaming.LightingSystem.Core;
using SynapseGaming.LightingSystem.Rendering;

namespace Mesh
{
    internal class ShaderMeshData
    {
        protected VBOStats Stats = new VBOStats();
        private RenderableMesh Mesh;

        public void Clear()
        {
            Mesh = null;
        }

        public void SetMeshData(GraphicsDevice device, RenderableMesh mesh)
        {
            if (Mesh == null
                || Mesh.vertexBuffer != mesh.vertexBuffer 
                || Mesh.vertexStreamOffset != mesh.vertexStreamOffset 
                || Mesh.stride != mesh.stride)
            {
                device.Vertices[0].SetSource(mesh.vertexBuffer, mesh.vertexStreamOffset, mesh.stride);
                ++Stats.BatchVBOChanges.AccumulationValue;
            }
            if (Mesh == null || Mesh.indexBuffer != mesh.indexBuffer)
                device.Indices = mesh.indexBuffer;
            if (Mesh == null || Mesh.vertexDeclaration != mesh.vertexDeclaration)
                device.VertexDeclaration = mesh.vertexDeclaration;
            Mesh = mesh;
        }

        protected class VBOStats
        {
            public LightingSystemStatistic BatchVBOChanges = LightingSystemStatistics.GetStatistic("Renderer_BatchVertexBufferChanges", LightingSystemStatisticCategory.Rendering);
        }
    }
}
