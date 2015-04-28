using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;

namespace Ship_Game
{
	internal struct RoundLineVertex
	{
		public Vector3 pos;

		public Vector2 rhoTheta;

		public Vector2 scaleTrans;

		public float index;

        public static int SizeInBytes = 32;

		public static VertexElement[] VertexElements = new VertexElement[] { new VertexElement(0, 0, VertexElementFormat.Vector3, VertexElementMethod.Default, VertexElementUsage.Position, 0), new VertexElement(0, 12, VertexElementFormat.Vector2, VertexElementMethod.Default, VertexElementUsage.Normal, 0), new VertexElement(0, 20, VertexElementFormat.Vector2, VertexElementMethod.Default, VertexElementUsage.TextureCoordinate, 0), new VertexElement(0, 28, VertexElementFormat.Single, VertexElementMethod.Default, VertexElementUsage.TextureCoordinate, 1) };

		public RoundLineVertex(Vector3 pos, Vector2 norm, Vector2 tex, float index)
		{
			this.pos = pos;
			this.rhoTheta = norm;
			this.scaleTrans = tex;
			this.index = index;
		}
	}
}