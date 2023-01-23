using System;
using System.Diagnostics;
using System.Reflection;
using Microsoft.Xna.Framework.Graphics;
using SDGraphics.Shaders;
using SDUtils;
using SDGraphics.Rendering;

namespace SDGraphics.Sprites;

using XnaMatrix = Microsoft.Xna.Framework.Matrix;

/// <summary>
/// An interface for drawing 2D or 3D sprites
/// </summary>
public sealed class SpriteRenderer : IDisposable
{
    public readonly GraphicsDevice Device;
    public VertexDeclaration VertexDeclaration;

    internal Shader Simple;
    internal readonly EffectPass SimplePass;
    readonly EffectParameter ViewProjectionParam;
    readonly EffectParameter TextureParam;
    readonly EffectParameter UseTextureParam;

    unsafe delegate void DrawUserIndexedPrimitivesD(
        GraphicsDevice device,
        PrimitiveType primitiveType,
        int numVertices,
        int primitiveCount,
        void* pIndexData,
        int indexFormat,
        void* pVertexData,
        int vertexStride
    );
    readonly DrawUserIndexedPrimitivesD DrawUserIndexedPrimitives;

    public SpriteRenderer(GraphicsDevice device)
    {
        Device = device ?? throw new NullReferenceException(nameof(device));
        VertexDeclaration = new VertexDeclaration(device, VertexCoordColor.VertexElements);

        Simple = Shader.FromFile(device, "Content/Effects/Simple.fx");
        ViewProjectionParam = Simple["ViewProjection"];
        TextureParam = Simple["Texture"];
        UseTextureParam = Simple["UseTexture"];
        SimplePass = Simple.CurrentTechnique.Passes[0];

        const BindingFlags anyMethod = BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public;
        MethodInfo method = typeof(GraphicsDevice).GetMethod("RawDrawUserIndexedPrimitives", anyMethod);
        if (method == null)
            throw new InvalidOperationException("Missing RawDrawUserIndexedPrimitives from XNA.GraphicsDevice");
        DrawUserIndexedPrimitives = (DrawUserIndexedPrimitivesD)Delegate.CreateDelegate(typeof(DrawUserIndexedPrimitivesD), null, method);
    }

    public void Dispose()
    {
        Mem.Dispose(ref VertexDeclaration);
        Mem.Dispose(ref Simple);
    }

    unsafe void SetViewProjection(in Matrix viewProjection)
    {
        fixed (Matrix* pViewProjection = &viewProjection)
        {
            ViewProjectionParam.SetValue(*(XnaMatrix*)pViewProjection);
        }
    }

    public bool IsBegin { get; private set; }

    public void Begin(in Matrix viewProjection)
    {
        if (IsBegin)
        {
            End();
        }

        SetViewProjection(viewProjection);

        IsBegin = true;
    }

    public void Begin(in Matrix view, in Matrix projection)
    {
        view.Multiply(projection, out Matrix viewProjection);
        //Matrix.Invert(viewProjection, out Matrix invViewProj);
        Begin(viewProjection);
    }

    public void End()
    {
        if (IsBegin)
        {
            IsBegin = false;
        }
    }

    public static unsafe void FillVertexData(VertexCoordColor* vertices, ushort* indices, int index,
                                             in Quad3D quad, in Quad2D coords, Color color)
    {
        int vertexOffset = index * 4;
        int indexOffset = index * 6;

        vertices[vertexOffset + 0] = new VertexCoordColor(quad.A, color, coords.A); // TopLeft
        vertices[vertexOffset + 1] = new VertexCoordColor(quad.B, color, coords.B); // TopRight
        vertices[vertexOffset + 2] = new VertexCoordColor(quad.C, color, coords.C); // BotRight
        vertices[vertexOffset + 3] = new VertexCoordColor(quad.D, color, coords.D); // BotLeft

        indices[indexOffset + 0] = (ushort)(vertexOffset + 0);
        indices[indexOffset + 1] = (ushort)(vertexOffset + 1);
        indices[indexOffset + 2] = (ushort)(vertexOffset + 2);
        indices[indexOffset + 3] = (ushort)(vertexOffset + 0);
        indices[indexOffset + 4] = (ushort)(vertexOffset + 2);
        indices[indexOffset + 5] = (ushort)(vertexOffset + 3);
    }

    [Conditional("DEBUG")]
    static void CheckTextureDisposed(Texture2D texture)
    {
        if (texture.IsDisposed)
            throw new ObjectDisposedException($"Texture2D '{texture.Name}'");
    }

    internal void ShaderBegin(Texture2D texture)
    {
        bool useTexture = texture != null;
        if (useTexture)
        {
            // only set Texture sampler if texture is used
            CheckTextureDisposed(texture);
            TextureParam.SetValue(texture);
        }
        UseTextureParam.SetValue(useTexture);

        Simple.Begin();
        SimplePass.Begin();
    }

    internal void ShaderEnd()
    {
        SimplePass.End();
        Simple.End();
    }

    /// <summary>
    /// Enables direct draw to the GPU. This is quite inefficient, so consider
    /// using BatchedSprites where possible.
    /// </summary>
    public unsafe void Draw(Texture2D texture, in Quad3D quad, in Quad2D coords, Color color)
    {
        // stack allocating these is extremely important to reduce memory pressure
        VertexCoordColor* vertices = stackalloc VertexCoordColor[4];
        ushort* indices = stackalloc ushort[6];
        FillVertexData(vertices, indices, 0, quad, coords, color);

        Device.VertexDeclaration = VertexDeclaration;

        ShaderBegin(texture);
        DrawUserIndexedPrimitives(Device, PrimitiveType.TriangleList,
            numVertices: 4,
            primitiveCount: 2,
            pIndexData: indices,
            indexFormat: 101, // 101: ushort indices, 102: uint indices
            pVertexData: vertices,
            vertexStride: sizeof(VertexCoordColor)
        );
        ShaderEnd();
    }

    public void Draw(BatchedSprites sprites)
    {
        sprites.Draw(this);
    }

    static readonly Quad2D DefaultCoords = new(new RectF(0, 0, 1, 1));

    public void Draw(Texture2D texture, in Vector3 center, in Vector2 size, Color color)
    {
        Draw(texture, new Quad3D(center, size), DefaultCoords, color);
    }

    public void Draw(Texture2D texture, in RectF rect, Color color)
    {
        Draw(texture, new Quad3D(rect, 0f), DefaultCoords, color);
    }

    public void Draw(Texture2D texture, in RectF rect, float z, in RectF sourceCoords, Color color)
    {
        Draw(texture, new Quad3D(rect, z), new Quad2D(sourceCoords), color);
    }

    public void Draw(Texture2D texture, in Vector3d center, in Vector2d size, Color color)
    {
        Draw(texture, center.ToVec3f(), size.ToVec2f(), color);
    }

    public void FillRect(in Vector3 center, in Vector2 size, Color color)
    {
        Draw(null, center, size, color);
    }

    public void FillRect(in Vector3d center, in Vector2d size, Color color)
    {
        Draw(null, center.ToVec3f(), size.ToVec2f(), color);
    }

    public void FillRect(in RectF rect, Color color)
    {
        Draw(null, new Quad3D(rect, 0f), DefaultCoords, color);
    }
}