using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;

namespace Ship_Game
{
	public sealed class DanProgressBar
	{
		public Rectangle rect;

		public float Max;

		public Rectangle progressRect;

		public DanProgressBar(Vector2 Position, float Max)
		{
			this.Max = Max;
			this.rect = new Rectangle((int)Position.X, (int)Position.Y, 170, 9);
			this.progressRect = new Rectangle(this.rect.X + 12, this.rect.Y + 2, 156, 5);
		}

		public void Draw(Ship_Game.ScreenManager ScreenManager, float Percent)
		{
			ScreenManager.SpriteBatch.Draw(ResourceManager.Texture("NewUI/Dan_Progress_Housing"), this.rect, Color.White);
			ScreenManager.SpriteBatch.FillRectangle(this.progressRect, new Color(24, 81, 91));
			int x = this.progressRect.X + (int)(Percent * (float)this.progressRect.Width);
			Rectangle Mask = new Rectangle(x, this.progressRect.Y, (int)((1f - Percent) * (float)this.progressRect.Width), this.progressRect.Height);
			ScreenManager.SpriteBatch.FillRectangle(Mask, Color.Black);
		}
	}
}