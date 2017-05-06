using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace Ship_Game
{

	public sealed class BackgroundItem : IDisposable
	{
		public Texture2D Texture;

		public Vector3 UpperLeft;

		public Vector3 LowerLeft;

		public Vector3 UpperRight;

		public Vector3 LowerRight;

		public VertexDeclaration quadVertexDecl;

		public VertexPositionNormalTexture[] Vertices = new VertexPositionNormalTexture[4];

		public int[] Indexes = new int[6];

		public static BasicEffect QuadEffect;

		public BackgroundItem()
		{
		}
        public BackgroundItem(Texture2D texture)
        {
            Texture = texture;
        }
		public void Draw(Ship_Game.ScreenManager ScreenManager, Matrix view, Matrix projection, float Alpha, Array<BackgroundItem> bgiList)
		{
			QuadEffect.View = view;
			QuadEffect.Projection = projection;
			QuadEffect.Texture = Texture;
			QuadEffect.Alpha = Alpha;
			ScreenManager.GraphicsDevice.VertexDeclaration = quadVertexDecl;
			QuadEffect.Begin();
			foreach (EffectPass pass in QuadEffect.CurrentTechnique.Passes)
			{
				pass.Begin();
				foreach (BackgroundItem bgi in bgiList)
				{
					ScreenManager.GraphicsDevice.DrawUserIndexedPrimitives<VertexPositionNormalTexture>(PrimitiveType.TriangleList, bgi.Vertices, 0, 4, bgi.Indexes, 0, 2);
				}
				pass.End();
			}
			QuadEffect.End();
			ScreenManager.GraphicsDevice.RenderState.DepthBufferWriteEnable = true;
		}

		public void Draw(Ship_Game.ScreenManager ScreenManager, Matrix view, Matrix projection, float Alpha)
		{
			QuadEffect.World = Matrix.Identity;
			QuadEffect.View = view;
			QuadEffect.Projection = projection;
			QuadEffect.Texture = Texture;
			QuadEffect.Alpha = Alpha;
			ScreenManager.GraphicsDevice.VertexDeclaration = quadVertexDecl;
			QuadEffect.Begin();
			foreach (EffectPass pass in QuadEffect.CurrentTechnique.Passes)
			{
				pass.Begin();
				ScreenManager.GraphicsDevice.DrawUserIndexedPrimitives<VertexPositionNormalTexture>(PrimitiveType.TriangleList, Vertices, 0, 4, Indexes, 0, 2);
				pass.End();
			}
			QuadEffect.End();
			ScreenManager.GraphicsDevice.RenderState.DepthBufferWriteEnable = true;
		}

		public void FillVertices()
		{
			Vector2 textureUpperLeft = new Vector2(0f, 0f);
			Vector2 textureUpperRight = new Vector2(1f, 0f);
			Vector2 textureLowerLeft = new Vector2(0f, 1f);
			Vector2 textureLowerRight = new Vector2(1f, 1f);
			Vertices[0].Position = LowerLeft;
			Vertices[0].TextureCoordinate = textureLowerLeft;
			Vertices[1].Position = UpperLeft;
			Vertices[1].TextureCoordinate = textureUpperLeft;
			Vertices[2].Position = LowerRight;
			Vertices[2].TextureCoordinate = textureLowerRight;
			Vertices[3].Position = UpperRight;
			Vertices[3].TextureCoordinate = textureUpperRight;
			Indexes[0] = 0;
			Indexes[1] = 1;
			Indexes[2] = 2;
			Indexes[3] = 2;
			Indexes[4] = 1;
			Indexes[5] = 3;
		}

		public void LoadContent(Ship_Game.ScreenManager ScreenManager, Matrix view, Matrix projection)
		{

			quadVertexDecl = new VertexDeclaration(ScreenManager.GraphicsDevice, VertexPositionNormalTexture.VertexElements);
		}

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        ~BackgroundItem() { Dispose(false); }

        private void Dispose(bool disposing)
        {
            quadVertexDecl?.Dispose(ref quadVertexDecl);
        }
    }
}