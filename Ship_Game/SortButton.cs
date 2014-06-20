using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;

namespace Ship_Game
{
	public class SortButton
	{
		public string Text;

		public Rectangle rect = new Rectangle();

		public bool Ascending = true;

		public bool Hover;

		public bool Selected;

		public SortButton()
		{
		}

		public void Draw(Ship_Game.ScreenManager ScreenManager)
		{
			Color orange;
			SpriteBatch spriteBatch = ScreenManager.SpriteBatch;
			SpriteFont arial20Bold = Fonts.Arial20Bold;
			string text = this.Text;
			Vector2 vector2 = new Vector2((float)this.rect.X, (float)this.rect.Y);
			if (this.Selected)
			{
				orange = Color.Orange;
			}
			else
			{
				orange = (this.Hover ? Color.White : new Color(255, 239, 208));
			}
			spriteBatch.DrawString(arial20Bold, text, vector2, orange);
		}

		public void Draw(Ship_Game.ScreenManager ScreenManager, SpriteFont font)
		{
			Color orange;
			SpriteBatch spriteBatch = ScreenManager.SpriteBatch;
			SpriteFont spriteFont = font;
			string text = this.Text;
			Vector2 vector2 = new Vector2((float)this.rect.X, (float)this.rect.Y);
			if (this.Selected)
			{
				orange = Color.Orange;
			}
			else
			{
				orange = (this.Hover ? Color.White : new Color(255, 239, 208));
			}
			spriteBatch.DrawString(spriteFont, text, vector2, orange);
		}

		public bool HandleInput(InputState input)
		{
			if (!HelperFunctions.CheckIntersection(this.rect, input.CursorPosition))
			{
				this.Hover = false;
			}
			else
			{
				if (!this.Hover)
				{
					AudioManager.PlayCue("sd_ui_mouseover");
				}
				this.Hover = true;
				if (input.InGameSelect)
				{
					AudioManager.PlayCue("mouse_over4");
					return true;
				}
			}
			return false;
		}

		public void Update(Vector2 Position)
		{
			this.rect = new Rectangle((int)Position.X, (int)Position.Y, (int)Fonts.Arial20Bold.MeasureString(this.Text).X, Fonts.Arial20Bold.LineSpacing);
		}
	}
}