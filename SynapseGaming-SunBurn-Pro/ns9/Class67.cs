// Decompiled with JetBrains decompiler
// Type: ns9.Class67
// Assembly: SynapseGaming-SunBurn-Pro, Version=1.3.2.8, Culture=neutral, PublicKeyToken=c23c60523565dbfd
// MVID: A5F03349-72AC-4BAA-AEEE-9AB9B77E0A39
// Assembly location: C:\Projects\BlackBox\StarDrive\SynapseGaming-SunBurn-Pro.dll

using Microsoft.Xna.Framework.Graphics;
using SynapseGaming.LightingSystem.Core;
using SynapseGaming.LightingSystem.Rendering;

namespace ns9
{
  internal class Class67
  {
    protected Class67.Class68 Statistics = new Class67.Class68();
    private RenderableMesh renderableMesh_0;

    public void method_0()
    {
      this.renderableMesh_0 = (RenderableMesh) null;
    }

    public void method_1(GraphicsDevice graphicsDevice_0, RenderableMesh renderableMesh_1)
    {
      if (this.renderableMesh_0 == null || this.renderableMesh_0.vertexBuffer != renderableMesh_1.vertexBuffer || (this.renderableMesh_0.vertexStreamOffset != renderableMesh_1.vertexStreamOffset || this.renderableMesh_0.stride != renderableMesh_1.stride))
      {
        graphicsDevice_0.Vertices[0].SetSource(renderableMesh_1.vertexBuffer, renderableMesh_1.vertexStreamOffset, renderableMesh_1.stride);
        ++this.Statistics.lightingSystemStatistic_0.AccumulationValue;
      }
      if (this.renderableMesh_0 == null || this.renderableMesh_0.indexBuffer != renderableMesh_1.indexBuffer)
        graphicsDevice_0.Indices = renderableMesh_1.indexBuffer;
      if (this.renderableMesh_0 == null || this.renderableMesh_0.vertexDeclaration != renderableMesh_1.vertexDeclaration)
        graphicsDevice_0.VertexDeclaration = renderableMesh_1.vertexDeclaration;
      this.renderableMesh_0 = renderableMesh_1;
    }

    protected class Class68
    {
      public LightingSystemStatistic lightingSystemStatistic_0 = LightingSystemStatistics.GetStatistic("Renderer_BatchVertexBufferChanges", LightingSystemStatisticCategory.Rendering);
    }
  }
}
