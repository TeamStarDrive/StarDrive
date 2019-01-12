using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace Ship_Game
{

	public sealed class BackgroundItem : IDisposable
	{
		public SubTexture Texture;

		public Vector3 UpperLeft;
		public Vector3 LowerLeft;
		public Vector3 UpperRight;
		public Vector3 LowerRight;
		public VertexDeclaration LayoutDescriptor;
		public VertexPositionNormalTexture[] Vertices = new VertexPositionNormalTexture[4];
		public int[] Indices = new int[6];
		public static BasicEffect QuadEffect;

		public BackgroundItem()
		{
		}

        public BackgroundItem(SubTexture texture)
        {
            Texture = texture;
        }

		public void Draw(ScreenManager manager, in Matrix view, in Matrix projection, float alpha)
		{
			QuadEffect.World      = Matrix.Identity;
			QuadEffect.View       = view;
			QuadEffect.Projection = projection;
			QuadEffect.Texture    = Texture.Texture;
			QuadEffect.Alpha      = alpha;
		    manager.GraphicsDevice.VertexDeclaration = LayoutDescriptor;
			QuadEffect.Begin();
			foreach (EffectPass pass in QuadEffect.CurrentTechnique.Passes)
			{
				pass.Begin();
			    manager.GraphicsDevice.DrawUserIndexedPrimitives(PrimitiveType.TriangleList, 
                    Vertices, 0, 4, Indices, 0, 2);
				pass.End();
			}
			QuadEffect.End();
		    manager.GraphicsDevice.RenderState.DepthBufferWriteEnable = true;
		}

		public void FillVertices()
		{
			Vertices[0].Position = LowerLeft;
		    Vertices[1].Position = UpperLeft;
		    Vertices[2].Position = LowerRight;
		    Vertices[3].Position = UpperRight;
			Vertices[0].TextureCoordinate = Texture.CoordLowerLeft;
			Vertices[1].TextureCoordinate = Texture.CoordUpperLeft;
			Vertices[2].TextureCoordinate = Texture.CoordLowerRight;;
			Vertices[3].TextureCoordinate = Texture.CoordUpperRight;
			Indices[0] = 0;
			Indices[1] = 1;
			Indices[2] = 2;
			Indices[3] = 2;
			Indices[4] = 1;
			Indices[5] = 3;
		}

		public void LoadContent(ScreenManager manager, Matrix view, Matrix projection)
		{
			LayoutDescriptor = new VertexDeclaration(manager.GraphicsDevice, VertexPositionNormalTexture.VertexElements);
		}

        public void Dispose()
        {
            Destroy();
            GC.SuppressFinalize(this);
        }

        ~BackgroundItem() { Destroy(); }

        void Destroy()
        {
            LayoutDescriptor?.Dispose(ref LayoutDescriptor);
        }
    }
}