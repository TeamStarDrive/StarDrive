using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;

namespace Ship_Game
{
	public class Body
	{
		public Rectangle BodyRect;

		private Rectangle tabRect;

		private Rectangle underheader;

		public Body(Rectangle r)
		{
			this.BodyRect = r;
			this.tabRect = new Rectangle(r.X - 53, r.Y + r.Height / 2 - 6, 11, 53);
			this.underheader = new Rectangle(r.X, r.Y + r.Height, r.Width, 10);
		}

		public void Draw(Ship_Game.ScreenManager ScreenManager)
		{
			ScreenManager.SpriteBatch.End();
			ScreenManager.SpriteBatch.Begin(SpriteBlendMode.Additive);
			ScreenManager.SpriteBatch.Draw(ResourceManager.TextureDict["OliveUI/body_grade"], this.BodyRect, Color.White);
			ScreenManager.SpriteBatch.End();
			ScreenManager.SpriteBatch.Begin();
			ScreenManager.SpriteBatch.Draw(ResourceManager.TextureDict["OliveUI/under_header"], this.underheader, Color.White);
		}

		public void DrawRightTab(Ship_Game.ScreenManager ScreenManager)
		{
			this.tabRect = new Rectangle(this.BodyRect.X + this.BodyRect.Width, this.BodyRect.Y + this.BodyRect.Height / 2 - 6, 11, 53);
			ScreenManager.SpriteBatch.Draw(ResourceManager.TextureDict["OliveUI/body_righttab"], this.tabRect, Color.White);
		}
	}
}