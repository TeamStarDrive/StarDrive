using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using System;
using System.Collections.Generic;

namespace Ship_Game
{
	public class SizeSlider
	{
		public Rectangle rect;

		public Rectangle ContainerRect;

		public bool Hover;

		public string Text = "";

		public float amount = 0.5f;

		public Rectangle cursor;

		//private string fmt = "0.#";

		public int Tip_ID;

		private Rectangle redRect;

		private Rectangle greenRect;

		private bool dragging;

		public SizeSlider(Rectangle r, string Text)
		{
			this.Text = Text;
			this.ContainerRect = r;
			this.rect = new Rectangle(r.X + 9, r.Y + r.Height / 2 + 3, r.Width - 30, 6);
			this.cursor = new Rectangle(this.rect.X + (int)((float)this.rect.Width * this.amount), this.rect.Y + this.rect.Height / 2 - ResourceManager.TextureDict["NewUI/slider_crosshair"].Height / 2, ResourceManager.TextureDict["NewUI/slider_crosshair"].Width, ResourceManager.TextureDict["NewUI/slider_crosshair"].Height);
			this.redRect = new Rectangle(this.rect.X, this.rect.Y, this.rect.Width / 2, 6);
			this.greenRect = new Rectangle(this.rect.X + this.rect.Width / 2, this.rect.Y, this.rect.Width / 2, 6);
		}

		public void Draw(Ship_Game.ScreenManager ScreenManager)
		{
			Microsoft.Xna.Framework.Graphics.SpriteBatch SpriteBatch = ScreenManager.SpriteBatch;
			Vector2 Cursor = new Vector2((float)(this.ContainerRect.X + 10), (float)this.ContainerRect.Y);
			SpriteBatch.DrawString(Fonts.Arial12Bold, this.Text, Cursor, new Color(255, 239, 208));
			if (this.amount > 0.5f)
			{
				float greenamount = 2f * (this.amount - 0.5f);
				SpriteBatch.Draw(ResourceManager.TextureDict["NewUI/slider_grd_green"], new Rectangle(this.greenRect.X, this.rect.Y, (int)(greenamount * (float)this.greenRect.Width), 6), new Rectangle?(new Rectangle(this.rect.X, this.rect.Y, (int)(greenamount * (float)this.greenRect.Width), 6)), Color.White);
			}
			else if (this.amount < 0.5f)
			{
				float blackAmount = 2f * this.amount;
				Primitives2D.FillRectangle(SpriteBatch, this.redRect, Color.Maroon);
				Primitives2D.FillRectangle(SpriteBatch, new Rectangle(this.redRect.X, this.rect.Y, (int)(blackAmount * (float)this.redRect.Width), 6), Color.Black);
			}
			Primitives2D.DrawRectangle(SpriteBatch, this.rect, (this.Hover ? new Color(164, 154, 133) : new Color(72, 61, 38)));
			Vector2 tickCursor = new Vector2();
			for (int i = 0; i < 11; i++)
			{
				tickCursor = new Vector2((float)(this.rect.X + this.rect.Width / 10 * i), (float)(this.rect.Y + this.rect.Height + 2));
				if (this.Hover)
				{
					SpriteBatch.Draw(ResourceManager.TextureDict["NewUI/slider_minute_hover"], tickCursor, Color.White);
				}
				else
				{
					SpriteBatch.Draw(ResourceManager.TextureDict["NewUI/slider_minute"], tickCursor, Color.White);
				}
			}
			Rectangle drawRect = this.cursor;
			drawRect.X = drawRect.X - drawRect.Width / 2;
			if (this.Hover)
			{
				SpriteBatch.Draw(ResourceManager.TextureDict["NewUI/slider_crosshair_hover"], drawRect, Color.White);
			}
			else
			{
				SpriteBatch.Draw(ResourceManager.TextureDict["NewUI/slider_crosshair"], drawRect, Color.White);
			}
			Vector2 textPos = new Vector2((float)(this.rect.X + this.rect.Width + 8), (float)(this.rect.Y + this.rect.Height / 2 - Fonts.Arial12Bold.LineSpacing / 2));
			string text = "";
			if (this.amount > 0.5f)
			{
				text = "Bigger";
			}
			if (this.amount < 0.5f)
			{
				text = "Smaller";
			}
			if (this.amount == 0.5f)
			{
				text = "Similar";
			}
			SpriteBatch.DrawString(Fonts.Arial12Bold, text, textPos, new Color(255, 239, 208));
			if (this.Hover && this.Tip_ID != 0)
			{
				ToolTip.CreateTooltip(this.Tip_ID, ScreenManager);
			}
		}

		public float HandleInput(InputState input)
		{
			if (!HelperFunctions.CheckIntersection(this.rect, input.CursorPosition))
			{
				this.Hover = false;
			}
			else
			{
				this.Hover = true;
			}
			Rectangle clickCursor = this.cursor;
			clickCursor.X = clickCursor.X - this.cursor.Width / 2;
			if (HelperFunctions.CheckIntersection(clickCursor, input.CursorPosition) && input.CurrentMouseState.LeftButton == ButtonState.Pressed && input.LastMouseState.LeftButton == ButtonState.Pressed)
			{
				this.dragging = true;
			}
			if (this.dragging)
			{
				this.cursor.X = (int)input.CursorPosition.X;
				if (this.cursor.X > this.rect.X + this.rect.Width)
				{
					this.cursor.X = this.rect.X + this.rect.Width;
				}
				else if (this.cursor.X < this.rect.X)
				{
					this.cursor.X = this.rect.X;
				}
				if (input.CurrentMouseState.LeftButton == ButtonState.Released)
				{
					this.dragging = false;
				}
				this.amount = 1f - (float)((float)this.rect.X + (float)this.rect.Width - (float)this.cursor.X) / (float)this.rect.Width;
			}
			return this.amount;
		}

		public void SetAmount(float amt)
		{
			this.amount = amt;
			this.cursor = new Rectangle(this.rect.X + (int)((float)this.rect.Width * this.amount), this.rect.Y + this.rect.Height / 2 - ResourceManager.TextureDict["NewUI/slider_crosshair"].Height / 2, ResourceManager.TextureDict["NewUI/slider_crosshair"].Width, ResourceManager.TextureDict["NewUI/slider_crosshair"].Height);
		}
	}
}