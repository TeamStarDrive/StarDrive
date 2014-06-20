using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;

namespace Ship_Game
{
	public class BackgroundItem
	{
		public Texture2D Texture;

		public Vector3 UpperLeft;

		public Vector3 LowerLeft;

		public Vector3 UpperRight;

		public Vector3 LowerRight;

		public VertexDeclaration quadVertexDecl;

		public VertexPositionNormalTexture[] Vertices = new VertexPositionNormalTexture[4];

		public int[] Indexes = new int[6];

		public BasicEffect quadEffect;

		public BackgroundItem()
		{
		}

		public void Draw(Ship_Game.ScreenManager ScreenManager, Matrix view, Matrix projection, float Alpha, List<BackgroundItem> bgiList)
		{
			this.quadEffect.View = view;
			this.quadEffect.Projection = projection;
			this.quadEffect.Texture = this.Texture;
			this.quadEffect.Alpha = Alpha;
			ScreenManager.GraphicsDevice.VertexDeclaration = this.quadVertexDecl;
			this.quadEffect.Begin();
			foreach (EffectPass pass in this.quadEffect.CurrentTechnique.Passes)
			{
				pass.Begin();
				foreach (BackgroundItem bgi in bgiList)
				{
					ScreenManager.GraphicsDevice.DrawUserIndexedPrimitives<VertexPositionNormalTexture>(PrimitiveType.TriangleList, bgi.Vertices, 0, 4, bgi.Indexes, 0, 2);
				}
				pass.End();
			}
			this.quadEffect.End();
			ScreenManager.GraphicsDevice.RenderState.DepthBufferWriteEnable = true;
		}

		public void Draw(Ship_Game.ScreenManager ScreenManager, Matrix view, Matrix projection, float Alpha)
		{
			this.quadEffect.World = Matrix.Identity;
			this.quadEffect.View = view;
			this.quadEffect.Projection = projection;
			this.quadEffect.Texture = this.Texture;
			this.quadEffect.Alpha = Alpha;
			ScreenManager.GraphicsDevice.VertexDeclaration = this.quadVertexDecl;
			this.quadEffect.Begin();
			foreach (EffectPass pass in this.quadEffect.CurrentTechnique.Passes)
			{
				pass.Begin();
				ScreenManager.GraphicsDevice.DrawUserIndexedPrimitives<VertexPositionNormalTexture>(PrimitiveType.TriangleList, this.Vertices, 0, 4, this.Indexes, 0, 2);
				pass.End();
			}
			this.quadEffect.End();
			ScreenManager.GraphicsDevice.RenderState.DepthBufferWriteEnable = true;
		}

		public void FillVertices()
		{
			Vector2 textureUpperLeft = new Vector2(0f, 0f);
			Vector2 textureUpperRight = new Vector2(1f, 0f);
			Vector2 textureLowerLeft = new Vector2(0f, 1f);
			Vector2 textureLowerRight = new Vector2(1f, 1f);
			this.Vertices[0].Position = this.LowerLeft;
			this.Vertices[0].TextureCoordinate = textureLowerLeft;
			this.Vertices[1].Position = this.UpperLeft;
			this.Vertices[1].TextureCoordinate = textureUpperLeft;
			this.Vertices[2].Position = this.LowerRight;
			this.Vertices[2].TextureCoordinate = textureLowerRight;
			this.Vertices[3].Position = this.UpperRight;
			this.Vertices[3].TextureCoordinate = textureUpperRight;
			this.Indexes[0] = 0;
			this.Indexes[1] = 1;
			this.Indexes[2] = 2;
			this.Indexes[3] = 2;
			this.Indexes[4] = 1;
			this.Indexes[5] = 3;
		}

		public void LoadContent(Ship_Game.ScreenManager ScreenManager, Matrix view, Matrix projection)
		{
			this.quadEffect = new BasicEffect(ScreenManager.GraphicsDevice, (EffectPool)null)
			{
				World = Matrix.Identity,
				View = view,
				Projection = projection,
				TextureEnabled = true
			};
			this.quadVertexDecl = new VertexDeclaration(ScreenManager.GraphicsDevice, VertexPositionNormalTexture.VertexElements);
		}
	}
}