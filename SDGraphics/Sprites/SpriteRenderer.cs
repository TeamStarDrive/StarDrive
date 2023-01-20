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
public class SpriteRenderer : IDisposable
{
    public readonly GraphicsDevice Device;
    VertexDeclaration VD;
    Shader Simple;
    EffectParameter ViewProjectionParam;
    EffectParameter TextureParam;
    EffectParameter UseTextureParam;
    
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
        VD = new VertexDeclaration(device, VertexCoordColor.VertexElements);

        Simple = Shader.FromFile(device, "Content/Effects/Simple.fx");
        ViewProjectionParam = Simple["ViewProjection"];
        TextureParam = Simple["Texture"];
        UseTextureParam = Simple["UseTexture"];

        const BindingFlags anyMethod = BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public;
        MethodInfo method = typeof(GraphicsDevice).GetMethod("RawDrawUserIndexedPrimitives", anyMethod);
        if (method == null)
            throw new InvalidOperationException("Missing RawDrawUserIndexedPrimitives from XNA.GraphicsDevice");
        DrawUserIndexedPrimitives = (DrawUserIndexedPrimitivesD)Delegate.CreateDelegate(typeof(DrawUserIndexedPrimitivesD), null, method);
    }

    public void Dispose()
    {
        Mem.Dispose(ref VD);
        Mem.Dispose(ref Simple);
    }

    unsafe void SetViewProjection(in Matrix viewProjection)
    {
        fixed (Matrix* pViewProjection = &viewProjection)
        {
            ViewProjectionParam.SetValue(*(XnaMatrix*)pViewProjection);
        }
    }

    public void Begin(in Matrix viewProjection)
    {
        SetViewProjection(viewProjection);
    }

    public void Begin(in Matrix view, in Matrix projection)
    {
        view.Multiply(projection, out Matrix viewProjection);
        //Matrix.Invert(viewProjection, out Matrix invViewProj);
        SetViewProjection(viewProjection);
    }

    static unsafe void FillVertexData(VertexCoordColor* vertices, short* indices, int index,
                                      in Quad3D quad, in Quad2D coords, Color color)
    {
        int vertexOffset = index * 4;
        int indexOffset = index * 6;

        vertices[vertexOffset + 0] = new VertexCoordColor(quad.A, color, coords.A); // TopLeft
        vertices[vertexOffset + 1] = new VertexCoordColor(quad.B, color, coords.B); // TopRight
        vertices[vertexOffset + 2] = new VertexCoordColor(quad.C, color, coords.C); // BotRight
        vertices[vertexOffset + 3] = new VertexCoordColor(quad.D, color, coords.D); // BotLeft

        indices[indexOffset + 0] = (short)(vertexOffset + 0);
        indices[indexOffset + 1] = (short)(vertexOffset + 1);
        indices[indexOffset + 2] = (short)(vertexOffset + 2);
        indices[indexOffset + 3] = (short)(vertexOffset + 0);
        indices[indexOffset + 4] = (short)(vertexOffset + 2);
        indices[indexOffset + 5] = (short)(vertexOffset + 3);
    }

    [Conditional("DEBUG")]
    static void CheckTextureDisposed(Texture2D texture)
    {
        if (texture.IsDisposed)
            throw new ObjectDisposedException($"Texture2D '{texture.Name}'");
    }

    static readonly Quad2D DefaultCoords = new(new RectF(0, 0, 1, 1));

    public unsafe void Draw(Texture2D texture, in Quad3D quad, in Quad2D coords, Color color)
    {
        // stack allocating these is extremely important to reduce memory pressure
        VertexCoordColor* vertices = stackalloc VertexCoordColor[4];
        short* indices = stackalloc short[6];
        FillVertexData(vertices, indices, 0, quad, coords, color);

        bool useTexture = texture != null;
        if (useTexture)
        {
            // only set Texture sampler if texture is used
            CheckTextureDisposed(texture);
            TextureParam.SetValue(texture);
        }
        UseTextureParam.SetValue(useTexture);

        Device.VertexDeclaration = VD;
        Simple.Begin();
        EffectPassCollection passes = Simple.CurrentTechnique.Passes;
        int numPasses = passes.Count;
        for (int i = 0; i < numPasses; ++i)
        {
            EffectPass pass = passes[i];
            pass.Begin();
            DrawUserIndexedPrimitives(Device, PrimitiveType.TriangleList,
                numVertices: 4,
                primitiveCount: 2,
                pIndexData: indices,
                indexFormat: 101, // 101: ushort indices, 102: uint indices
                pVertexData: vertices,
                vertexStride: sizeof(VertexCoordColor)
            );
            pass.End();
        }
        Simple.End();
    }

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