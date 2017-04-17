using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;

namespace Ship_Game
{
	public sealed class SortButton
	{
		public string Text;

		public Rectangle rect = new Rectangle();

		public bool Ascending = true;

		public bool Hover;

		public bool Selected;
        public bool saved =false;
        private SortButton saveButton;

		public SortButton()
		{
		}
        public SortButton(SortButton sb, string text)
        {
            this.Text = text;
            if(sb!=null && sb.Text == this.Text)
            {
                this.Ascending = sb.Ascending;
                this.saved = true;
            }
            this.saveButton = sb;
            
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
            if (this.saved)
            {
                this.saved = false;

                return true;
            }

            if (!HelperFunctions.CheckIntersection(this.rect, input.CursorPosition))
			{
				this.Hover = false;
			}
			else
			{
				if (!this.Hover)
				{
					GameAudio.PlaySfxAsync("sd_ui_mouseover");
				}
				this.Hover = true;
				if (input.InGameSelect)
				{
					
                    GameAudio.PlaySfxAsync("mouse_over4");
                    if (this.saveButton != null)
                    {
                        this.saveButton.saved = true;
                        this.saveButton.Text = this.Text;
                        this.saveButton.Ascending = this.Ascending;
                    }
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