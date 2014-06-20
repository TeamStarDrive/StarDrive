using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;

namespace Ship_Game
{
	public class PrimitiveQuad
	{
		public static GraphicsDevice graphicsDevice;

		private Texture2D blankTex;

		private Rectangle[] primitiveQuad = new Rectangle[4];

		public Rectangle enclosingRect;

		public bool isFilled;

		public int X;

		public int Y;

		public int W;

		public int H;

		public PrimitiveQuad(float x, float y, float w, float h)
		{
			this.X = (int)x;
			this.Y = (int)y;
			this.W = (int)w;
			this.H = (int)h;
			this.LoadContent();
			this.enclosingRect = new Rectangle(this.X, this.Y, this.W, this.H);
		}

		public PrimitiveQuad(Rectangle rect)
		{
			this.X = rect.X;
			this.Y = rect.Y;
			this.W = rect.Width;
			this.H = rect.Height;
			this.LoadContent();
			this.enclosingRect = rect;
		}

		public bool Contains(Vector2 pos)
		{
			if (pos.X > (float)this.X && pos.X < (float)(this.X + this.W) && pos.Y > (float)this.Y && pos.Y < (float)(this.Y + this.H))
			{
				return true;
			}
			return false;
		}

		public void Draw(SpriteBatch spriteBatch)
		{
			this.primitiveQuad[0] = new Rectangle(this.X, this.Y, this.W, 1);
			this.primitiveQuad[1] = new Rectangle(this.X, this.Y, 1, this.H);
			this.primitiveQuad[2] = new Rectangle(this.X + this.W, this.Y, 1, this.H);
			this.primitiveQuad[3] = new Rectangle(this.X, this.Y + this.H, this.W, 1);
			Rectangle[] rectangleArray = this.primitiveQuad;
			for (int i = 0; i < (int)rectangleArray.Length; i++)
			{
				Rectangle tmp = rectangleArray[i];
				spriteBatch.Draw(this.blankTex, tmp, Color.White);
			}
		}

		public void Draw(SpriteBatch spriteBatch, Color color)
		{
			this.primitiveQuad[0] = new Rectangle(this.X, this.Y, this.W, 1);
			this.primitiveQuad[1] = new Rectangle(this.X, this.Y, 1, this.H);
			this.primitiveQuad[2] = new Rectangle(this.X + this.W, this.Y, 1, this.H);
			this.primitiveQuad[3] = new Rectangle(this.X, this.Y + this.H, this.W, 1);
			Rectangle[] rectangleArray = this.primitiveQuad;
			for (int i = 0; i < (int)rectangleArray.Length; i++)
			{
				Rectangle tmp = rectangleArray[i];
				Rectangle? nullable = null;
				spriteBatch.Draw(this.blankTex, tmp, nullable, color, 0f, Vector2.Zero, SpriteEffects.None, 0.89f);
			}
		}

		public void Draw(SpriteBatch spriteBatch, Color color, int thickness)
		{
			this.primitiveQuad[0] = new Rectangle(this.X - thickness / 2, this.Y - thickness / 2, this.W, thickness);
			this.primitiveQuad[1] = new Rectangle(this.X - thickness / 2, this.Y - thickness / 2, thickness, this.H);
			this.primitiveQuad[2] = new Rectangle(this.X + this.W - thickness / 2, this.Y - thickness / 2, thickness, this.H + thickness);
			this.primitiveQuad[3] = new Rectangle(this.X - thickness / 2, this.Y + this.H - thickness / 2, this.W, thickness);
			Rectangle[] rectangleArray = this.primitiveQuad;
			for (int i = 0; i < (int)rectangleArray.Length; i++)
			{
				Rectangle tmp = rectangleArray[i];
				Rectangle? nullable = null;
				spriteBatch.Draw(this.blankTex, tmp, nullable, color, 0f, Vector2.Zero, SpriteEffects.None, 0.89f);
			}
		}

		public void LoadContent()
		{
			this.blankTex = new Texture2D(PrimitiveQuad.graphicsDevice, 1, 1);
			Color[] texcol = new Color[] { Color.White };
			this.blankTex.SetData<Color>(texcol);
		}
	}
}