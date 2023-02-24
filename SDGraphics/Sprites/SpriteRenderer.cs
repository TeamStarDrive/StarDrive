using System;
using Microsoft.Xna.Framework.Graphics;
using SDGraphics.Shaders;
using SDUtils;
using SDGraphics.Rendering;

namespace SDGraphics.Sprites;

/// <summary>
/// An interface for drawing 2D or 3D sprites
/// </summary>
public sealed class SpriteRenderer : IDisposable
{
    internal const int MaxBatchSize = ushort.MaxValue;

    public readonly GraphicsDevice Device;
    public VertexDeclaration VertexDeclaration;

    // Since we are always drawing Quads, the index buffer can be pre-calculated and shared
    internal IndexBuffer IndexBuf;

    SpriteShader DefaultEffect;
    SpriteShader CurrentEffect;

    // Helper utility for batching draw calls together 
    readonly DynamicSpriteBatcher Batcher;

    public SpriteRenderer(GraphicsDevice device)
    {
        Device = device ?? throw new NullReferenceException(nameof(device));
        VertexDeclaration = new(device, VertexCoordColor.VertexElements);
        Batcher = new(device);

        // load the shader with parameters
        Shader simple = Shader.FromFile(device, "Content/Effects/Simple.fx");
        DefaultEffect = new(simple);
        CurrentEffect = DefaultEffect;

        // lastly, create buffers
        IndexBuf = CreateIndexBuffer(device);
    }

    public void Dispose()
    {
        Mem.Dispose(ref VertexDeclaration);
        Mem.Dispose(ref IndexBuf);
        Mem.Dispose(ref DefaultEffect);
        CurrentEffect = null;
        Batcher.Dispose();
    }

    /// <summary>
    /// TRUE if Begin() has been called
    /// </summary>
    public bool IsBegin { get; private set; }

    /// <summary>
    /// Statistics: average size of a Begin() / End() pair
    /// </summary>
    public int AverageBatchSize { get; private set; }

    /// <summary>
    /// Initializes the view projection matrix for rendering,
    /// and if `IsBegin` was true, End()'s the previous state.
    /// </summary>
    public void Begin(in Matrix viewProjection, SpriteShader effect = null)
    {
        if (IsBegin)
        {
            End();
        }

        CurrentEffect = effect ?? DefaultEffect;
        CurrentEffect.SetViewProjection(viewProjection);

        IsBegin = true;
    }
    public void Begin(in Matrix view, in Matrix projection)
    {
        view.Multiply(projection, out Matrix viewProjection);
        Begin(viewProjection);
    }

    /// <summary>
    /// Ends the current rendering task and flushes any buffers if needed
    /// </summary>
    public void End()
    {
        if (IsBegin)
        {
            IsBegin = false;
        }

        // some debug stuff
        AverageBatchSize = (AverageBatchSize + Batcher.Count) / 2;

        // flush and draw all the quads
        Batcher.DrawBatches(this);
        Batcher.Reset();
    }

    public void RecycleBuffers()
    {
        Batcher.RecycleBuffers();
    }

    // creates a completely reusable index buffer
    static IndexBuffer CreateIndexBuffer(GraphicsDevice device)
    {
        const int numQuads = MaxBatchSize / 6;

        ushort[] indices = new ushort[MaxBatchSize];
        for (int index = 0; index < numQuads; ++index)
        {
            int vertexOffset = index * 4;
            int indexOffset = index * 6;
            indices[indexOffset + 0] = (ushort)(vertexOffset + 0);
            indices[indexOffset + 1] = (ushort)(vertexOffset + 1);
            indices[indexOffset + 2] = (ushort)(vertexOffset + 2);
            indices[indexOffset + 3] = (ushort)(vertexOffset + 0);
            indices[indexOffset + 4] = (ushort)(vertexOffset + 2);
            indices[indexOffset + 5] = (ushort)(vertexOffset + 3);
        }

        IndexBuffer indexBuf = new(device, typeof(ushort), indices.Length, BufferUsage.WriteOnly);
        indexBuf.SetData(indices);
        return indexBuf;
    }

    internal void ShaderBegin(Texture2D texture, Color color)
    {
        CurrentEffect.SetTexture(texture); // also set null
        CurrentEffect.SetUseTexture(useTexture: texture != null);
        CurrentEffect.SetColor(color);

        CurrentEffect.Shader.Begin();
        CurrentEffect.ShaderPass.Begin();
    }

    internal void ShaderEnd()
    {
        CurrentEffect.ShaderPass.End();
        CurrentEffect.Shader.End();
    }

    /// <summary>
    /// Draw a precompiled batch of sprites
    /// </summary>
    public void Draw(BatchedSprites sprites)
    {
        sprites.Draw(this, Color.White);
    }

    /// <summary>
    /// Draw a precompiled batch of sprites with a color multiplier
    /// </summary>
    public void Draw(BatchedSprites sprites, Color color)
    {
        sprites.Draw(this, color);
    }

    /// <summary>
    /// Enables direct draw to the GPU. This is quite inefficient, so consider
    /// using BatchedSprites where possible.
    /// </summary>
    public void Draw(Texture2D texture, in Quad3D quad, in Quad2D coords, Color color)
    {
        Batcher.Add(texture, in quad, in coords, color);
    }
    public void Draw(SubTexture texture, in Quad3D quad, Color color)
    {
        Batcher.Add(texture.Texture, in quad, in texture.UVCoords, color);
    }

    /// <summary>
    /// Default UV Coordinates to draw the full texture
    /// </summary>
    public static readonly Quad2D DefaultCoords = new(new RectF(0, 0, 1, 1));

    public void Draw(Texture2D texture, in Vector3 center, in Vector2 size, Color color)
    {
        Batcher.Add(texture, new Quad3D(center, size), in DefaultCoords, color);
    }
    public void Draw(SubTexture texture, in Vector3 center, in Vector2 size, Color color)
    {
        Batcher.Add(texture.Texture, new Quad3D(center, size), in texture.UVCoords, color);
    }

    /// <summary>
    /// Draw a texture quad at 2D position `rect`, facing upwards
    /// </summary>
    public void Draw(Texture2D texture, in RectF rect, Color color)
    {
        Batcher.Add(texture, new Quad3D(rect, 0f), DefaultCoords, color);
    }
    public void Draw(SubTexture texture, in RectF rect, Color color)
    {
        Batcher.Add(texture.Texture, new Quad3D(rect, 0f), texture.UVCoords, color);
    }

    /// <summary>
    /// Double precision overload for drawing a texture quad at 3D position `center`
    /// </summary>
    public void Draw(Texture2D texture, in Vector3d center, in Vector2d size, Color color)
    {
        Batcher.Add(texture, new Quad3D(center.ToVec3f(), size.ToVec2f()), DefaultCoords, color);
    }
    public void Draw(SubTexture texture, in Vector3d center, in Vector2d size, Color color)
    {
        Batcher.Add(texture.Texture, new Quad3D(center.ToVec3f(), size.ToVec2f()), texture.UVCoords, color);
    }

    /// <summary>
    /// Fills a rectangle at 3D position `center`, facing upwards
    /// </summary>
    public void FillRect(in Vector3 center, in Vector2 size, Color color)
    {
        Batcher.Add(null, new Quad3D(center, size), DefaultCoords, color);
    }
    public void FillRect(in Vector3d center, in Vector2d size, Color color)
    {
        Batcher.Add(null, new Quad3D(center.ToVec3f(), size.ToVec2f()), DefaultCoords, color);
    }
    public void FillRect(in RectF rect, Color color)
    {
        Batcher.Add(null, new Quad3D(rect, 0f), DefaultCoords, color);
    }

    /// <summary>
    /// Draws a line based rectangle with no fill
    /// </summary>
    /// <param name="rect">Rectangle quad which can be rotated</param>
    /// <param name="color">Color of the rect</param>
    /// <param name="thickness">Width of the line</param>
    public void DrawRectLine(in Quad3D rect, Color color, float thickness = 1f)
    {
        DrawLine(rect.A, rect.B, color, thickness);
        DrawLine(rect.B, rect.C, color, thickness);
        DrawLine(rect.C, rect.D, color, thickness);
        DrawLine(rect.D, rect.A, color, thickness);
    }

    /// <summary>
    /// This draws a 2D line at Z=0
    /// </summary>
    /// <param name="p1">Start point</param>
    /// <param name="p2">End point</param>
    /// <param name="color">Color of the line</param>
    /// <param name="thickness">Width of the line</param>
    public void DrawLine(in Vector2 p1, in Vector2 p2, Color color, float thickness = 1f)
    {
        Quad3D line = new(p1, p2, thickness, zValue: 0f);
        Batcher.Add(null, line, DefaultCoords, color);
    }
    public void DrawLine(in Vector2d p1, in Vector2d p2, Color color, float thickness = 1f)
    {
        Quad3D line = new(p1.ToVec2f(), p2.ToVec2f(), thickness, zValue: 0f);
        Batcher.Add(null, line, DefaultCoords, color);
    }

    public void DrawLine(in Vector3 p1, in Vector3 p2, Color color, float thickness = 1f)
    {
        Quad3D line = new(p1, p2, thickness);
        Batcher.Add(null, line, DefaultCoords, color);
    }

    /// <summary>
    /// Draws a circle with an adaptive line count
    /// </summary>
    public void DrawCircle(Vector2 center, float radius, Color color, float thickness = 1f)
    {
        // TODO: there are loads of issues with this, the radius only works for 2D rendering
        // TODO: figure out a better way to draw circles without having to draw 256 lines every time
        int sides = 12 + ((int)radius / 6); // adaptive line count
        DrawCircle(center, radius, sides, color, thickness);
    }
    public void DrawCircle(Vector2d center, double radius, Color color, float thickness = 1f)
    {
        int sides = 12 + ((int)radius / 6); // adaptive line count
        DrawCircle(center, radius, sides, color, thickness);
    }

    /// <summary>
    /// Draws a circle with predefined number of sides
    /// </summary>
    /// <param name="center"></param>
    /// <param name="radius"></param>
    /// <param name="sides">This will always be clamped within [3, 256]</param>
    /// <param name="color"></param>
    /// <param name="thickness"></param>
    public void DrawCircle(Vector2 center, float radius, int sides, Color color, float thickness = 1f)
    {
        sides = sides.Clamped(3, 256);
        float step = 6.28318530717959f / sides;

        Vector2 start = new(center.X + radius, center.Y); // 0 angle is horizontal right
        Vector2 previous = start;

        for (float theta = step; theta < 6.28318530717959f; theta += step)
        {
            Vector2 current = new(center.X + radius * RadMath.Cos(theta), 
                                  center.Y + radius * RadMath.Sin(theta));
            DrawLine(previous, current, color, thickness);
            previous = current;
        }
        DrawLine(previous, start, color, thickness); // connect back to start
    }
    public void DrawCircle(Vector2d center, double radius, int sides, Color color, float thickness = 1f)
    {
        sides = sides.Clamped(3, 256);
        double step = 6.28318530717959 / sides;

        Vector2d start = new(center.X + radius, center.Y); // 0 angle is horizontal right
        Vector2d previous = start;

        for (double theta = step; theta < 6.28318530717959; theta += step)
        {
            Vector2d current = new(center.X + radius * RadMath.Cos(theta), 
                                   center.Y + radius * RadMath.Sin(theta));
            DrawLine(previous, current, color, thickness);
            previous = current;
        }
        DrawLine(previous, start, color, thickness); // connect back to start
    }

    // RedFox - These are salvaged from my 3D utility library, https://github.com/RedFox20/AlphaGL

    //// core radius determines the width of the line core
    //// for very small widths, the core should be very small ~10%
    //// for large width, the core should be very large ~90%
    //static void lineCoreRadii(const float width, float& cr, float& w2)
    //{
    //    switch ((int)width) {
    //        case 0:
    //        case 1:  w2 = (width + 0.5f) * 0.5f; cr = 0.25f; return;
    //        case 2:  w2 = width * 0.5f; cr = 0.75f; return;
    //        case 3:  w2 = width * 0.5f; cr = 1.5f;  return;
    //        // always leave 1 pixel for the edge radius
    //        default: w2 = width * 0.5f; cr = w2 - 1.0f; return;
    //    }
    //}

    // this require per-vertex-alpha which is indeed supported by VertexCoordColor
    //void GLDraw2D::LineAA(const Vector2& p1, const Vector2& p2, const float width)
    //{
    //    // 12 vertices
    //    //      x1                A up     
    //    // 0\``2\``4\``6    left  |  right 
    //    // | \ | \ | \ |    <-----o----->  
    //    // 1__\3__\5__\7          |         
    //    //      x2                V down

    //    float cr, w2;
    //    lineCoreRadii(width, cr, w2);

    //    float x1 = p1.x, y1 = p1.y, x2 = p2.x, y2 = p2.y;
    //    Vector2 right(y2 - y1, x1 - x2);
    //    right.normalize();

    //    // extend start and end by a tiny amount (core radius to be exact)
    //    Vector2 dir(x2 - x1, y2 - y1);
    //    dir.normalize(cr);
    //    x1 -= dir.x;
    //    y1 -= dir.y;
    //    x2 += dir.x;
    //    y2 += dir.y;

    //    float ex = right.x * w2, ey = right.y * w2; // edge xy offsets
    //    float cx = right.x * cr, cy = right.y * cr; // center xy offsets
    //    index_t n = (index_t)vertices.size();
    //    vertices.resize(n + 8);
    //    Vertex2Alpha* v = &vertices[n];
    //    v[0].x = x1 - ex, v[0].y = y1 - ey, v[0].a = 0.0f;	// left-top
    //    v[1].x = x2 - ex, v[1].y = y2 - ey, v[1].a = 0.0f;	// left-bottom
    //    v[2].x = x1 - cx, v[2].y = y1 - cy, v[2].a = 1.0f;	// left-middle-top
    //    v[3].x = x2 - cx, v[3].y = y2 - cy, v[3].a = 1.0f;	// left-middle-bottom
    //    v[4].x = x1 + cx, v[4].y = y1 + cy, v[4].a = 1.0f;	// right-middle-top
    //    v[5].x = x2 + cx, v[5].y = y2 + cy, v[5].a = 1.0f;	// right-middle-bottom
    //    v[6].x = x1 + ex, v[6].y = y1 + ey, v[6].a = 0.0f;	// right-top
    //    v[7].x = x2 + ex, v[7].y = y2 + ey, v[7].a = 0.0f;	// right-bottom

    //    size_t numIndices = indices.size();
    //    indices.resize(numIndices + 18);
    //    index_t* i = &indices[numIndices];
    //    i[0]  = n + 0, i[1]  = n + 1, i[2]  = n + 3; // triangle 1
    //    i[3]  = n + 0, i[4]  = n + 3, i[5]  = n + 2; // triangle 2
    //    i[6]  = n + 2, i[7]  = n + 3, i[8]  = n + 5; // triangle 3
    //    i[9]  = n + 2, i[10] = n + 5, i[11] = n + 4; // triangle 4
    //    i[12] = n + 4, i[13] = n + 5, i[14] = n + 7; // triangle 5
    //    i[15] = n + 4, i[16] = n + 7, i[17] = n + 6; // triangle 6
    //}

    //void GLDraw2D::RectAA(const Vector2& origin, const Vector2& size, float lineWidth)
    //{
    //    //  0---3
    //    //  | + |
    //    //  1---2
    //    Vector2 p0(origin.x, origin.y);
    //    Vector2 p1(origin.x, origin.y + size.y);
    //    Vector2 p2(origin.x + size.x, origin.y + size.y);
    //    Vector2 p3(origin.x + size.x, origin.y);
    //    LineAA(p0, p1, lineWidth);
    //    LineAA(p1, p2, lineWidth);
    //    LineAA(p2, p3, lineWidth);
    //    LineAA(p3, p0, lineWidth);
    //}

    //void GLDraw2D::CircleAA(const Vector2& center, float radius, float lineWidth)
    //{
    //    // adaptive line count
    //    const int   segments   = 12 + (int(radius) / 6);
    //    const float segmentArc = (2.0f * rpp::PIf) / segments;
    //    const float x = center.x, y = center.y;

    //    float alpha = segmentArc;
    //    Vector2 A(x, y + radius);
    //    for (int i = 0; i < segments; ++i)
    //    {
    //        Vector2 B(x + sinf(alpha)*radius, y + cosf(alpha)*radius);
    //        LineAA(A, B, lineWidth);
    //        A = B;
    //        alpha += segmentArc;
    //    }
    //}
}
