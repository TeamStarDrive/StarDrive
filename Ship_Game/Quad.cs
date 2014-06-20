using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;

namespace Ship_Game
{
	public struct Quad
	{
		public Vector3 Origin;

		public Vector3 UpperLeft;

		public Vector3 LowerLeft;

		public Vector3 UpperRight;

		public Vector3 LowerRight;

		public Vector3 Normal;

		public Vector3 Up;

		public Vector3 Left;

		public VertexPositionNormalTexture[] Vertices;

		public int[] Indexes;

		public Quad(Vector3 origin, Vector3 normal, Vector3 up, float width, float height)
		{
			this.Vertices = new VertexPositionNormalTexture[4];
			this.Indexes = new int[6];
			this.Origin = origin;
			this.Normal = normal;
			this.Up = up;
			this.Left = Vector3.Cross(normal, this.Up);
			Vector3 uppercenter = ((this.Up * height) / 2f) + origin;
			this.UpperLeft = uppercenter + ((this.Left * width) / 2f);
			this.UpperRight = uppercenter - ((this.Left * width) / 2f);
			this.LowerLeft = this.UpperLeft - (this.Up * height);
			this.LowerRight = this.UpperRight - (this.Up * height);
			this.FillVertices();
		}

		private void FillVertices()
		{
			Vector2 textureUpperLeft = new Vector2(0f, 0f);
			Vector2 textureUpperRight = new Vector2(1f, 0f);
			Vector2 textureLowerLeft = new Vector2(0f, 1f);
			Vector2 textureLowerRight = new Vector2(1f, 1f);
			for (int i = 0; i < (int)this.Vertices.Length; i++)
			{
				this.Vertices[i].Normal = this.Normal;
			}
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
	}
}