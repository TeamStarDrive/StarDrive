using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace Ship_Game
{
	public sealed class Body
	{
		public Rectangle BodyRect;

		private Rectangle tabRect;

		private Rectangle underheader;

		public Body(Rectangle r)
		{
			BodyRect = r;
			tabRect = new Rectangle(r.X - 53, r.Y + r.Height / 2 - 6, 11, 53);
			underheader = new Rectangle(r.X, r.Y + r.Height, r.Width, 10);
		}

		public void Draw(ScreenManager ScreenManager)
		{
			ScreenManager.SpriteBatch.End();
			ScreenManager.SpriteBatch.Begin(SpriteBlendMode.Additive);
			ScreenManager.SpriteBatch.Draw(ResourceManager.Texture("OliveUI/body_grade"), BodyRect, Color.White);
			ScreenManager.SpriteBatch.End();
			ScreenManager.SpriteBatch.Begin();
			ScreenManager.SpriteBatch.Draw(ResourceManager.Texture("OliveUI/under_header"), underheader, Color.White);
		}

		public void DrawRightTab(ScreenManager ScreenManager)
		{
			tabRect = new Rectangle(BodyRect.X + BodyRect.Width, BodyRect.Y + BodyRect.Height / 2 - 6, 11, 53);
			ScreenManager.SpriteBatch.Draw(ResourceManager.Texture("OliveUI/body_righttab"), tabRect, Color.White);
		}
	}
}