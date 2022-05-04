using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Xna.Framework.Graphics;
using SDGraphics.Shaders;
using SDUtils;

namespace SDGraphics.Sprites
{
    using XnaMatrix = Microsoft.Xna.Framework.Matrix;

    /// <summary>
    /// An interface for drawing 2D or 3D sprites
    /// </summary>
    public class SpriteRenderer : IDisposable
    {
        public struct Vertex
        {
            public Vector3 Position;
            public Color Color;
            public Vector2 Coords;
            public Vertex(in Vector3 pos, Color color, in Vector2 coords)
            {
                Position = pos;
                Color = color;
                Coords = coords;
            }
            public static readonly VertexElement[] VertexElements = new VertexElement[3]
            {
                new (0, 0, VertexElementFormat.Vector3, VertexElementMethod.Default, VertexElementUsage.Position, 0),
                new (0, 12, VertexElementFormat.Color, VertexElementMethod.Default, VertexElementUsage.Color, 0),
                new (0, 16, VertexElementFormat.Vector2, VertexElementMethod.Default, VertexElementUsage.TextureCoordinate, 0)
            };
            public const int SizeInBytes = 24;
        }

        readonly GraphicsDevice Device;
        VertexDeclaration VD;
        Shader Simple;
        EffectParameter ViewProjectionParam;
        EffectParameter TextureParam;

        public SpriteRenderer(GraphicsDevice device)
        {
            Device = device;
            VD = new VertexDeclaration(device, Vertex.VertexElements);

            Simple = Shader.FromFile(device, "Content/Effects/Simple.fx");
            ViewProjectionParam = Simple["ViewProjection"];
            TextureParam = Simple["Texture"];
        }

        public void Dispose()
        {
            Memory.Dispose(ref VD);
            Memory.Dispose(ref Simple);
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

        public void Begin(in XnaMatrix view, in XnaMatrix projection)
        {
            XnaMatrix viewProjection = view * projection;
            ViewProjectionParam.SetValue(viewProjection);
        }

        static readonly RectF DefaultCoords = new(0, 0, 1, 1);

        public void Draw(Texture2D texture, in Vector3 center, in Vector2 size, Color color)
        {
            var vertices = new Vertex[4];
            var indices = new short[6];
            FillVertexData(vertices, indices, 0, center, size, DefaultCoords, color);
            DrawTriangles(texture, vertices, indices);
        }

        void FillVertexData(Vertex[] vertices, short[] indices, int index, in Vector3 center, Vector2 size, in RectF coords, Color color)
        {
            int vertexOffset = index * 4;
            int indexOffset = index * 6;

            float left = center.X - size.X*0.5f;
            float top = center.Y - size.Y*0.5f;
            float right = left + size.X;
            float bottom = top + size.Y;
            float z = center.Z;

            vertices[vertexOffset + 0] = new Vertex(new Vector3(left, top, z), color, new Vector2(coords.X, coords.Y)); // topleft
            vertices[vertexOffset + 1] = new Vertex(new Vector3(right, top, z), color, new Vector2(coords.X+coords.W, coords.Y)); // topright
            vertices[vertexOffset + 2] = new Vertex(new Vector3(right, bottom, z), color, new Vector2(coords.X+coords.W, coords.Y+coords.H)); // botright
            vertices[vertexOffset + 3] = new Vertex(new Vector3(left, bottom, z), color, new Vector2(coords.X, coords.Y+coords.H)); // botleft

            indices[indexOffset + 0] = (short)(vertexOffset + 0);
            indices[indexOffset + 1] = (short)(vertexOffset + 1);
            indices[indexOffset + 2] = (short)(vertexOffset + 2);
            indices[indexOffset + 3] = (short)(vertexOffset + 0);
            indices[indexOffset + 4] = (short)(vertexOffset + 2);
            indices[indexOffset + 5] = (short)(vertexOffset + 3);
        }

        void DrawTriangles(Texture2D texture, Vertex[] vertices, short[] indices)
        {
            TextureParam.SetValue(texture);

            Device.VertexDeclaration = VD;
            Simple.Begin();
            foreach (EffectPass pass in Simple.CurrentTechnique.Passes)
            {
                pass.Begin();
                Device.DrawUserIndexedPrimitives(PrimitiveType.TriangleList, 
                    vertices, 0, vertices.Length, indices, 0, indices.Length / 3);
                pass.End();
            }
            Simple.End();
        }
    }
}
