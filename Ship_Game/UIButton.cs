using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;

namespace Ship_Game
{
	public class UIButton
	{
		public Rectangle Rect;

		public UIButton.PressState State;

		public Texture2D NormalTexture;

		public Texture2D HoverTexture;

		public Texture2D PressedTexture;

		public string Text = "";

		public string Launches = "";

		public Color HoverColor = new Color(255, 240, 189);

		public Color PressColor = new Color(255, 240, 189);

		public int ToolTip;

		public UIButton()
		{
		}

		public void Draw(SpriteBatch spriteBatch, Rectangle r)
		{
			Vector2 textCursor = new Vector2();
			if (this.Text != "")
			{
				textCursor.X = (float)(r.X + this.Rect.Width / 2) - Fonts.Arial12Bold.MeasureString(this.Text).X / 2f;
				textCursor.Y = (float)(r.Y + this.Rect.Height / 2 - Fonts.Arial12Bold.LineSpacing / 2);
			}
			if (this.State == UIButton.PressState.Normal)
			{
				spriteBatch.Draw(this.NormalTexture, r, Color.White);
				if (this.Text != "")
				{
					spriteBatch.DrawString(Fonts.Arial12Bold, this.Text, textCursor, new Color(255, 240, 189));
					return;
				}
			}
			else if (this.State == UIButton.PressState.Hover)
			{
				spriteBatch.Draw(this.HoverTexture, r, Color.White);
				if (this.Text != "")
				{
					spriteBatch.DrawString(Fonts.Arial12Bold, this.Text, textCursor, this.HoverColor);
					return;
				}
			}
			else if (this.State == UIButton.PressState.Pressed)
			{
				spriteBatch.Draw(this.PressedTexture, r, Color.White);
				if (this.Text != "")
				{
					textCursor.Y = textCursor.Y + 1f;
					spriteBatch.DrawString(Fonts.Arial12Bold, this.Text, textCursor, this.PressColor);
				}
			}
		}

		public void Draw(SpriteBatch spriteBatch)
		{
			Vector2 textCursor = new Vector2();
			if (this.Text != "")
			{
				textCursor.X = (float)(this.Rect.X + this.Rect.Width / 2) - Fonts.Arial12Bold.MeasureString(this.Text).X / 2f;
				textCursor.Y = (float)(this.Rect.Y + this.Rect.Height / 2 - Fonts.Arial12Bold.LineSpacing / 2);
			}
			if (this.State == UIButton.PressState.Normal)
			{
				spriteBatch.Draw(this.NormalTexture, this.Rect, Color.White);
				if (this.Text != "")
				{
					spriteBatch.DrawString(Fonts.Arial12Bold, this.Text, textCursor, new Color(255, 240, 189));
					return;
				}
			}
			else if (this.State == UIButton.PressState.Hover)
			{
				spriteBatch.Draw(this.HoverTexture, this.Rect, Color.White);
				if (this.Text != "")
				{
					spriteBatch.DrawString(Fonts.Arial12Bold, this.Text, textCursor, this.HoverColor);
					return;
				}
			}
			else if (this.State == UIButton.PressState.Pressed)
			{
				spriteBatch.Draw(this.PressedTexture, this.Rect, Color.White);
				if (this.Text != "")
				{
					textCursor.Y = textCursor.Y + 1f;
					spriteBatch.DrawString(Fonts.Arial12Bold, this.Text, textCursor, this.PressColor);
				}
			}
		}

		public void DrawInActive(SpriteBatch spriteBatch)
		{
			Vector2 textCursor = new Vector2();
			if (this.Text != "")
			{
				textCursor.X = (float)(this.Rect.X + this.Rect.Width / 2) - Fonts.Arial12Bold.MeasureString(this.Text).X / 2f;
				textCursor.Y = (float)(this.Rect.Y + this.Rect.Height / 2 - Fonts.Arial12Bold.LineSpacing / 2);
			}
			if (this.State == UIButton.PressState.Normal)
			{
				spriteBatch.Draw(this.NormalTexture, this.Rect, Color.White);
				if (this.Text != "")
				{
					spriteBatch.DrawString(Fonts.Arial12Bold, this.Text, textCursor, Color.Gray);
					return;
				}
			}
			else if (this.State == UIButton.PressState.Hover)
			{
				spriteBatch.Draw(this.HoverTexture, this.Rect, Color.White);
				if (this.Text != "")
				{
					spriteBatch.DrawString(Fonts.Arial12Bold, this.Text, textCursor, Color.Gray);
					return;
				}
			}
			else if (this.State == UIButton.PressState.Pressed)
			{
				spriteBatch.Draw(this.PressedTexture, this.Rect, Color.White);
				if (this.Text != "")
				{
					textCursor.Y = textCursor.Y + 1f;
					spriteBatch.DrawString(Fonts.Arial12Bold, this.Text, textCursor, Color.Gray);
				}
			}
		}

		public void DrawLowRes(SpriteBatch spriteBatch)
		{
			Vector2 textCursor = new Vector2();
			if (this.Text != "")
			{
				textCursor.X = (float)(this.Rect.X + this.Rect.Width / 2) - Fonts.Arial12Bold.MeasureString(this.Text).X / 2f;
				textCursor.Y = (float)(this.Rect.Y + this.Rect.Height / 2 - Fonts.Arial12Bold.LineSpacing / 2 - 1);
			}
			if (this.State == UIButton.PressState.Normal)
			{
				spriteBatch.Draw(this.NormalTexture, this.Rect, Color.White);
				if (this.Text != "")
				{
					spriteBatch.DrawString(Fonts.Arial12, this.Text, textCursor, new Color(255, 240, 189));
					return;
				}
			}
			else if (this.State == UIButton.PressState.Hover)
			{
				spriteBatch.Draw(this.HoverTexture, this.Rect, Color.White);
				if (this.Text != "")
				{
					spriteBatch.DrawString(Fonts.Arial12, this.Text, textCursor, this.HoverColor);
					return;
				}
			}
			else if (this.State == UIButton.PressState.Pressed)
			{
				spriteBatch.Draw(this.PressedTexture, this.Rect, Color.White);
				if (this.Text != "")
				{
					textCursor.Y = textCursor.Y + 1f;
					spriteBatch.DrawString(Fonts.Arial12, this.Text, textCursor, this.PressColor);
				}
			}
		}

		public enum PressState
		{
			Normal,
			Hover,
			Pressed
		}
	}
}