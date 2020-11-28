using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace Ship_Game
{
	public sealed class Header
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
			leftRect = new Rectangle(r.X, r.Y, 42, 41);
			FillRect = new Rectangle(r.X + 42, r.Y, r.Width - 42 - 4, 41);
			RightRect = new Rectangle(r.X + r.Width - 4, r.Y, 4, 41);
			TextPos = new Vector2(leftRect.X + leftRect.Width + 4, leftRect.Y + leftRect.Height / 2 - Fonts.Arial20Bold.LineSpacing / 2 + 1);
			overheader = new Rectangle(r.X, r.Y - 11, r.Width, 15);
		}

		public void Draw(SpriteBatch batch, DrawTimes elapsed)
		{
			batch.Draw(ResourceManager.Texture("OliveUI/header_left"), leftRect, Color.White);
			batch.Draw(ResourceManager.Texture("OliveUI/header_fill"), FillRect, Color.White);
			batch.Draw(ResourceManager.Texture("OliveUI/header_right"), RightRect, Color.White);
			batch.Draw(ResourceManager.Texture("OliveUI/over_header"), overheader, Color.White);
			if (Fonts.Arial20Bold.MeasureString(text).X > FillRect.Width - 150)
			{
				batch.DrawString(Fonts.Arial12Bold, text, TextPos, new Color(198, 189, 180));
				return;
			}
			batch.DrawString(Fonts.Arial20Bold, text, TextPos, new Color(198, 189, 180));
		}
	}
}