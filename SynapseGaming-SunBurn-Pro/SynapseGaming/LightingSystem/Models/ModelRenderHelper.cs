// Decompiled with JetBrains decompiler
// Type: SynapseGaming.LightingSystem.Models.ModelRenderHelper
// Assembly: SynapseGaming-SunBurn-Pro, Version=1.3.2.8, Culture=neutral, PublicKeyToken=c23c60523565dbfd
// MVID: A5F03349-72AC-4BAA-AEEE-9AB9B77E0A39
// Assembly location: C:\Projects\BlackBox\StarDrive\SynapseGaming-SunBurn-Pro.dll

using Microsoft.Xna.Framework.Graphics;

namespace SynapseGaming.LightingSystem.Models
{
  /// <summary>Helper class for rendering raw XNA Models.</summary>
  public class ModelRenderHelper
  {
    /// <summary>
    /// Renders an XNA ModelMesh using the currently assigned
    /// effect and render states (not the Model's).  Allows
    /// rendering XNA Models as raw geometry.
    /// </summary>
    /// <param name="device">Current graphics device.</param>
    /// <param name="mesh">ModelMesh to render.</param>
    public static void Render(GraphicsDevice device, ModelMesh mesh)
    {
      device.Indices = mesh.IndexBuffer;
      for (int index = 0; index < mesh.MeshParts.Count; ++index)
      {
        ModelMeshPart meshPart = mesh.MeshParts[index];
        device.Vertices[0].SetSource(mesh.VertexBuffer, meshPart.StreamOffset, meshPart.VertexStride);
        device.VertexDeclaration = meshPart.VertexDeclaration;
        device.DrawIndexedPrimitives(PrimitiveType.TriangleList, meshPart.BaseVertex, 0, meshPart.NumVertices, meshPart.StartIndex, meshPart.PrimitiveCount);
      }
    }
  }
}
