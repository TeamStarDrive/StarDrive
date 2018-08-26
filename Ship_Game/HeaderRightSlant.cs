using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace Ship_Game
{
	public sealed class HeaderRightSlant
	{
		public Rectangle leftRect;

		public Rectangle FillRect;

		public Rectangle RightRect;

		private string text;

		private Vector2 TextPos;

		public HeaderRightSlant(Rectangle r, string text)
		{
			this.text = text;
			leftRect = new Rectangle(r.X, r.Y, 42, 36);
			FillRect = new Rectangle(r.X + 42, r.Y, r.Width - 42 - 30, 36);
			RightRect = new Rectangle(r.X + r.Width - 30, r.Y, 30, 36);
			TextPos = new Vector2(leftRect.X + leftRect.Width + 4, leftRect.Y + leftRect.Height / 2 - Fonts.Arial20Bold.LineSpacing / 2 + 1);
		}

		public void Draw(ScreenManager ScreenManager)
		{
			ScreenManager.SpriteBatch.Draw(ResourceManager.Texture("OliveUI/header_left"), leftRect, Color.White);
			ScreenManager.SpriteBatch.Draw(ResourceManager.Texture("OliveUI/header_fill"), FillRect, Color.White);
			ScreenManager.SpriteBatch.Draw(ResourceManager.Texture("OliveUI/header_right_slant"), RightRect, Color.White);
			ScreenManager.SpriteBatch.DrawString(Fonts.Arial20Bold, text, TextPos, new Color(198, 189, 180));
		}
	}
}