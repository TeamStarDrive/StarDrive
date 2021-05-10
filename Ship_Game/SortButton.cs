using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Ship_Game.Audio;

namespace Ship_Game
{
	public sealed class SortButton
	{
		public string Text;
		public Rectangle rect;

		public bool Ascending = true;
		public bool Hover;
		public bool Selected;
		public bool saved;
		SortButton saveButton;

		public SortButton()
		{
		}
		public SortButton(SortButton sb, string text)
		{
			Text = text;
			if (sb != null && sb.Text == Text)
			{
				Ascending = sb.Ascending;
				saved = true;
			}
			saveButton = sb;
		}
		public void Draw(ScreenManager ScreenManager)
		{
			Color orange;
			SpriteBatch spriteBatch = ScreenManager.SpriteBatch;
			Graphics.Font arial20Bold = Fonts.Arial20Bold;
			string text = Text;
			var vector2 = new Vector2(rect.X, rect.Y);
			if (Selected)
			{
				orange = Color.Orange;
			}
			else
			{
				orange = (Hover ? Color.White : Colors.Cream);
			}
			spriteBatch.DrawString(arial20Bold, text, vector2, orange);
		}

		public void Draw(ScreenManager ScreenManager, Graphics.Font font)
		{
			Color orange;
			SpriteBatch spriteBatch = ScreenManager.SpriteBatch;
			Graphics.Font spriteFont = font;
			string text = Text;
			Vector2 vector2 = new Vector2(rect.X, rect.Y);
			if (Selected)
			{
				orange = Color.Orange;
			}
			else
			{
				orange = (Hover ? Color.White : Colors.Cream);
			}
			spriteBatch.DrawString(spriteFont, text, vector2, orange);
		}

		public bool HandleInput(InputState input)
		{
			if (saved)
			{
				saved = false;
				return true;
			}

			if (!rect.HitTest(input.CursorPosition))
			{
				Hover = false;
			}
			else
			{
				if (!Hover)
				{
					GameAudio.ButtonMouseOver();
				}
				Hover = true;
				if (input.InGameSelect)
				{
					
					GameAudio.MouseOver();
					if (saveButton != null)
					{
						saveButton.saved = true;
						saveButton.Text = Text;
						saveButton.Ascending = Ascending;
					}
					return true;
				}
			}
			return false;
		}

		public void Update(Vector2 Position)
		{
			rect = new Rectangle((int)Position.X, (int)Position.Y, (int)Fonts.Arial20Bold.MeasureString(Text).X, Fonts.Arial20Bold.LineSpacing);
		}
	}
}