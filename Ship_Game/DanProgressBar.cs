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
			rect = new Rectangle((int)Position.X, (int)Position.Y, 170, 9);
			progressRect = new Rectangle(rect.X + 12, rect.Y + 2, 156, 5);
		}

		public void Draw(ScreenManager ScreenManager, float Percent)
		{
			ScreenManager.SpriteBatch.Draw(ResourceManager.Texture("NewUI/Dan_Progress_Housing"), rect, Color.White);
			ScreenManager.SpriteBatch.FillRectangle(progressRect, new Color(24, 81, 91));
			int x = progressRect.X + (int)(Percent * (float)progressRect.Width);
			Rectangle Mask = new Rectangle(x, progressRect.Y, (int)((1f - Percent) * (float)progressRect.Width), progressRect.Height);
			ScreenManager.SpriteBatch.FillRectangle(Mask, Color.Black);
		}
	}
}