using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;

namespace Ship_Game
{
	public class HeaderRightSlant
	{
		public Rectangle leftRect;

		public Rectangle FillRect;

		public Rectangle RightRect;

		private string text;

		private Vector2 TextPos;

		public HeaderRightSlant(Rectangle r, string text)
		{
			this.text = text;
			this.leftRect = new Rectangle(r.X, r.Y, 42, 36);
			this.FillRect = new Rectangle(r.X + 42, r.Y, r.Width - 42 - 30, 36);
			this.RightRect = new Rectangle(r.X + r.Width - 30, r.Y, 30, 36);
			this.TextPos = new Vector2((float)(this.leftRect.X + this.leftRect.Width + 4), (float)(this.leftRect.Y + this.leftRect.Height / 2 - Fonts.Arial20Bold.LineSpacing / 2 + 1));
		}

		public void Draw(Ship_Game.ScreenManager ScreenManager)
		{
			ScreenManager.SpriteBatch.Draw(ResourceManager.TextureDict["OliveUI/header_left"], this.leftRect, Color.White);
			ScreenManager.SpriteBatch.Draw(ResourceManager.TextureDict["OliveUI/header_fill"], this.FillRect, Color.White);
			ScreenManager.SpriteBatch.Draw(ResourceManager.TextureDict["OliveUI/header_right_slant"], this.RightRect, Color.White);
			ScreenManager.SpriteBatch.DrawString(Fonts.Arial20Bold, this.text, this.TextPos, new Color(198, 189, 180));
		}
	}
}