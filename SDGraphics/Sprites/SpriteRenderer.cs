using System;
using System.Collections.Generic;
using System.Diagnostics;
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
        EffectParameter UseTextureParam;

        public SpriteRenderer(GraphicsDevice device)
        {
            Device = device;
            VD = new VertexDeclaration(device, Vertex.VertexElements);

            Simple = Shader.FromFile(device, "Content/Effects/Simple.fx");
            ViewProjectionParam = Simple["ViewProjection"];
            TextureParam = Simple["Texture"];
            UseTextureParam = Simple["UseTexture"];
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

        public void Begin(in Matrix view, in Matrix projection)
        {
            view.Multiply(projection, out Matrix viewProjection);
            //Matrix.Invert(viewProjection, out Matrix invViewProj);
            SetViewProjection(viewProjection);
        }

        void FillVertexData(Vertex[] vertices, short[] indices, int index,
            float left, float right, float top, float bot, float z,
            in RectF coords, Color color)
        {
            int vertexOffset = index * 4;
            int indexOffset = index * 6;

            var tc_tl = new Vector2(coords.X, coords.Y); // TexCoord TopLeft
            var tc_br = new Vector2(coords.X + coords.W, coords.Y + coords.H); // TexCoord TopRight
            var tc_tr = new Vector2(tc_br.X, tc_tl.Y); // TexCoord BotRight
            var tc_bl = new Vector2(tc_tl.X, tc_br.Y); // TexCoord BotLeft

            vertices[vertexOffset + 0] = new Vertex(new Vector3(left, top, z), color, tc_tl); // TopLeft
            vertices[vertexOffset + 1] = new Vertex(new Vector3(right, top, z), color, tc_tr); // TopRight
            vertices[vertexOffset + 2] = new Vertex(new Vector3(right, bot, z), color, tc_br); // BotRight
            vertices[vertexOffset + 3] = new Vertex(new Vector3(left, bot, z), color, tc_bl); // BotLeft

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

        void DrawTriangles(Texture2D texture, Vertex[] vertices, short[] indices)
        {
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
            foreach (EffectPass pass in Simple.CurrentTechnique.Passes)
            {
                pass.Begin();
                Device.DrawUserIndexedPrimitives(PrimitiveType.TriangleList,
                    vertices, 0, vertices.Length, indices, 0, indices.Length / 3);
                pass.End();
            }
            Simple.End();
        }

        static readonly RectF DefaultCoords = new(0, 0, 1, 1);

        public void Draw(Texture2D texture, in Vector3 center, in Vector2 size, Color color)
        {
            var vertices = new Vertex[4];
            var indices = new short[6];

            // calculate these with double precision to improve accuracy
            double sx2 = size.X / 2.0;
            double sy2 = size.Y / 2.0;
            float left  = (float)(center.X - sx2);
            float right = (float)(center.X + sx2);
            float top = (float)(center.Y - sy2);
            float bot = (float)(center.Y + sy2);
            FillVertexData(vertices, indices, 0, left, right, top, bot, center.Z, DefaultCoords, color);
            DrawTriangles(texture, vertices, indices);
        }

        public void Draw(Texture2D texture, in RectF rect, Color color)
        {
            var vertices = new Vertex[4];
            var indices = new short[6];
            float left = rect.X;
            float right = left + rect.W;
            float top = rect.Y;
            float bot = top + rect.H;
            FillVertexData(vertices, indices, 0, left, right, top, bot, 0f, DefaultCoords, color);
            DrawTriangles(texture, vertices, indices);
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
            Draw(null, rect, color);
        }
    }
}
