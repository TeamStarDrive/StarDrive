using System;
using Microsoft.Xna.Framework.Graphics;

namespace Ship_Game.Graphics.Particles;

/// <summary>
/// Shared data between ParticleVertexBuffer instances
/// </summary>
public class ParticleVertexBufferShared : IDisposable
{
    // Index buffer turns sets of four vertices into particle quads (pairs of triangles).
    public IndexBuffer IndexBuffer;
    public VertexDeclaration VertexDeclaration;
    public GraphicsDevice Device;

    public ParticleVertexBufferShared(GraphicsDevice device)
    {
        Device = device;
        VertexDeclaration = new(device, ParticleVertex.VertexElements);

        // Create and populate the index buffer.
        ushort[] indices = new ushort[ParticleVertexBuffer.Size * 6];

        for (int i = 0; i < ParticleVertexBuffer.Size; i++)
        {
            indices[i * 6 + 0] = (ushort)(i * 4 + 0);
            indices[i * 6 + 1] = (ushort)(i * 4 + 1);
            indices[i * 6 + 2] = (ushort)(i * 4 + 2);

            indices[i * 6 + 3] = (ushort)(i * 4 + 0);
            indices[i * 6 + 4] = (ushort)(i * 4 + 2);
            indices[i * 6 + 5] = (ushort)(i * 4 + 3);
        }

        IndexBuffer = new(device, typeof(ushort), indices.Length, BufferUsage.WriteOnly);
        IndexBuffer.SetData(indices);
    }

    ~ParticleVertexBufferShared()
    {
        IndexBuffer?.Dispose(ref IndexBuffer);
        VertexDeclaration?.Dispose(ref VertexDeclaration);
    }

    public void Dispose()
    {
        IndexBuffer?.Dispose(ref IndexBuffer);
        VertexDeclaration?.Dispose(ref VertexDeclaration);
        GC.SuppressFinalize(this);
    }
}
