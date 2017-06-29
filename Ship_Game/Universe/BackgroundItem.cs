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
		public void Draw(ScreenManager manager, Matrix view, Matrix projection, float Alpha, Array<BackgroundItem> bgiList)
		{
			QuadEffect.View = view;
			QuadEffect.Projection = projection;
			QuadEffect.Texture = Texture;
			QuadEffect.Alpha = Alpha;
		    manager.GraphicsDevice.VertexDeclaration = quadVertexDecl;
			QuadEffect.Begin();
			foreach (EffectPass pass in QuadEffect.CurrentTechnique.Passes)
			{
				pass.Begin();
				foreach (BackgroundItem bgi in bgiList)
				{
				    manager.GraphicsDevice.DrawUserIndexedPrimitives(PrimitiveType.TriangleList, bgi.Vertices, 0, 4, bgi.Indexes, 0, 2);
				}
				pass.End();
			}
			QuadEffect.End();
		    manager.GraphicsDevice.RenderState.DepthBufferWriteEnable = true;
		}

		public void Draw(ScreenManager manager, Matrix view, Matrix projection, float Alpha)
		{
			QuadEffect.World      = Matrix.Identity;
			QuadEffect.View       = view;
			QuadEffect.Projection = projection;
			QuadEffect.Texture    = Texture;
			QuadEffect.Alpha      = Alpha;
		    manager.GraphicsDevice.VertexDeclaration = quadVertexDecl;
			QuadEffect.Begin();
			foreach (EffectPass pass in QuadEffect.CurrentTechnique.Passes)
			{
				pass.Begin();
			    manager.GraphicsDevice.DrawUserIndexedPrimitives(PrimitiveType.TriangleList, Vertices, 0, 4, Indexes, 0, 2);
				pass.End();
			}
			QuadEffect.End();
		    manager.GraphicsDevice.RenderState.DepthBufferWriteEnable = true;
		}

		public void FillVertices()
		{
			var textureUpperLeft  = new Vector2(0f, 0f);
		    var textureUpperRight = new Vector2(1f, 0f);
		    var textureLowerLeft  = new Vector2(0f, 1f);
		    var textureLowerRight = new Vector2(1f, 1f);
			Vertices[0].Position = LowerLeft;
		    Vertices[1].Position = UpperLeft;
		    Vertices[2].Position = LowerRight;
		    Vertices[3].Position = UpperRight;
			Vertices[0].TextureCoordinate = textureLowerLeft;
			Vertices[1].TextureCoordinate = textureUpperLeft;
			Vertices[2].TextureCoordinate = textureLowerRight;
			Vertices[3].TextureCoordinate = textureUpperRight;
			Indexes[0] = 0;
			Indexes[1] = 1;
			Indexes[2] = 2;
			Indexes[3] = 2;
			Indexes[4] = 1;
			Indexes[5] = 3;
		}

		public void LoadContent(ScreenManager manager, Matrix view, Matrix projection)
		{
			quadVertexDecl = new VertexDeclaration(manager.GraphicsDevice, 
                VertexPositionNormalTexture.VertexElements);
		}

        public void Dispose()
        {
            Destroy();
            GC.SuppressFinalize(this);
        }

        ~BackgroundItem() { Destroy(); }

        private void Destroy()
        {
            quadVertexDecl?.Dispose(ref quadVertexDecl);
        }
    }
}