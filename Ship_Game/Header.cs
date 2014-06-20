using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;

namespace Ship_Game
{
	public class Header
	{
		public Rectangle leftRect;

		public Rectangle FillRect;

		public Rectangle RightRect;

		public string text;

		private Vector2 TextPos;

		private Rectangle overheader;

		public Header(Rectangle r, string text)
		{
			this.text = text;
			this.leftRect = new Rectangle(r.X, r.Y, 42, 41);
			this.FillRect = new Rectangle(r.X + 42, r.Y, r.Width - 42 - 4, 41);
			this.RightRect = new Rectangle(r.X + r.Width - 4, r.Y, 4, 41);
			this.TextPos = new Vector2((float)(this.leftRect.X + this.leftRect.Width + 4), (float)(this.leftRect.Y + this.leftRect.Height / 2 - Fonts.Arial20Bold.LineSpacing / 2 + 1));
			this.overheader = new Rectangle(r.X, r.Y - 11, r.Width, 15);
		}

		public void Draw(Ship_Game.ScreenManager ScreenManager)
		{
			ScreenManager.SpriteBatch.Draw(ResourceManager.TextureDict["OliveUI/header_left"], this.leftRect, Color.White);
			ScreenManager.SpriteBatch.Draw(ResourceManager.TextureDict["OliveUI/header_fill"], this.FillRect, Color.White);
			ScreenManager.SpriteBatch.Draw(ResourceManager.TextureDict["OliveUI/header_right"], this.RightRect, Color.White);
			ScreenManager.SpriteBatch.Draw(ResourceManager.TextureDict["OliveUI/over_header"], this.overheader, Color.White);
			if (Fonts.Arial20Bold.MeasureString(this.text).X > (float)(this.FillRect.Width - 150))
			{
				ScreenManager.SpriteBatch.DrawString(Fonts.Arial12Bold, this.text, this.TextPos, new Color(198, 189, 180));
				return;
			}
			ScreenManager.SpriteBatch.DrawString(Fonts.Arial20Bold, this.text, this.TextPos, new Color(198, 189, 180));
		}
	}
}