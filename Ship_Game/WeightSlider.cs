using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using System;
using System.Collections.Generic;

namespace Ship_Game
{
	public sealed class WeightSlider
	{
		public Rectangle rect;

		public Rectangle ContainerRect;

		public bool Hover;

		public string Text = "";

		public float amount = 0.5f;

		public Rectangle cursor;

		private string fmt = "0.#";

		public int Tip_ID;

		private Rectangle redRect;

		private Rectangle greenRect;

		private bool dragging;

		public WeightSlider(Rectangle r, string Text)
		{
			this.Text = Text;
			this.ContainerRect = r;
			this.rect = new Rectangle(r.X + 9, r.Y + r.Height / 2 + 3, 120, 6);
            var tex = ResourceManager.Texture("NewUI/slider_crosshair");
			this.cursor = new Rectangle(this.rect.X + (int)((float)this.rect.Width * this.amount), this.rect.Y + this.rect.Height / 2 - tex.Height / 2, tex.Width, tex.Height);
			this.redRect = new Rectangle(this.rect.X, this.rect.Y, this.rect.Width / 2, 6);
			this.greenRect = new Rectangle(this.rect.X + this.rect.Width / 2, this.rect.Y, this.rect.Width / 2, 6);
		}

		public void Draw(Ship_Game.ScreenManager ScreenManager)
		{
			Vector2 Cursor = new Vector2((float)(this.ContainerRect.X + 10), (float)this.ContainerRect.Y);
			ScreenManager.SpriteBatch.DrawString(Fonts.Arial12Bold, this.Text, Cursor, new Color(255, 239, 208));
			if (this.amount > 0.5f)
			{
				float greenamount = 2f * (this.amount - 0.5f);
				ScreenManager.SpriteBatch.Draw(ResourceManager.Texture("NewUI/slider_grd_green"), new Rectangle(this.greenRect.X, this.rect.Y, (int)(greenamount * (float)this.greenRect.Width), 6), new Rectangle?(new Rectangle(this.rect.X, this.rect.Y, (int)(greenamount * (float)this.greenRect.Width), 6)), Color.White);
			}
			else if (this.amount < 0.5f)
			{
				float blackAmount = 2f * this.amount;
				ScreenManager.SpriteBatch.FillRectangle(this.redRect, Color.Maroon);
				ScreenManager.SpriteBatch.FillRectangle(new Rectangle(this.redRect.X, this.rect.Y, (int)(blackAmount * (float)this.redRect.Width), 6), Color.Black);
			}
			ScreenManager.SpriteBatch.DrawRectangle(this.rect, (this.Hover ? new Color(164, 154, 133) : new Color(72, 61, 38)));
			Vector2 tickCursor = new Vector2();
			for (int i = 0; i < 11; i++)
			{
				tickCursor = new Vector2((float)(this.rect.X + this.rect.Width / 10 * i), (float)(this.rect.Y + this.rect.Height + 2));
				if (this.Hover)
				{
					ScreenManager.SpriteBatch.Draw(ResourceManager.Texture("NewUI/slider_minute_hover"), tickCursor, Color.White);
				}
				else
				{
					ScreenManager.SpriteBatch.Draw(ResourceManager.Texture("NewUI/slider_minute"), tickCursor, Color.White);
				}
			}
			Rectangle drawRect = this.cursor;
			drawRect.X = drawRect.X - drawRect.Width / 2;
			if (this.Hover)
			{
				ScreenManager.SpriteBatch.Draw(ResourceManager.Texture("NewUI/slider_crosshair_hover"), drawRect, Color.White);
			}
			else
			{
				ScreenManager.SpriteBatch.Draw(ResourceManager.Texture("NewUI/slider_crosshair"), drawRect, Color.White);
			}
			if (this.Hover && this.Tip_ID != 0)
			{
				ToolTip.CreateTooltip(this.Tip_ID);
			}
			Vector2 textPos = new Vector2((float)(this.rect.X + this.rect.Width + 8), (float)(this.rect.Y + this.rect.Height / 2 - Fonts.Arial12Bold.LineSpacing / 2));
			float single = 2f * (this.amount - 0.5f);
			ScreenManager.SpriteBatch.DrawString(Fonts.Arial12Bold, single.ToString(this.fmt), textPos, new Color(255, 239, 208));
		}

	    public bool HandleInput(InputState input, ref float currentvalue)
	    {
	        if (!rect.HitTest(input.CursorPosition) || !input.LeftMouseHeld())
	        {
	            SetAmount(currentvalue);
	            return false;
	        }
	        currentvalue = HandleInput(input);
	        return true;

	    }

        public float HandleInput(InputState input)
		{
			if (!this.rect.HitTest(input.CursorPosition))
			{
				this.Hover = false;
			}
			else
			{
				this.Hover = true;
			}
			Rectangle clickCursor = this.cursor;
			clickCursor.X = clickCursor.X - this.cursor.Width / 2;
			if (clickCursor.HitTest(input.CursorPosition) && input.MouseCurr.LeftButton == ButtonState.Pressed && input.MousePrev.LeftButton == ButtonState.Pressed)
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
				if (input.LeftMouseUp)
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
            var tex = ResourceManager.Texture("NewUI/slider_crosshair");
			this.cursor = new Rectangle(this.rect.X + (int)((float)this.rect.Width * this.amount), this.rect.Y + this.rect.Height / 2 - tex.Height / 2, tex.Width, tex.Height);
		}
	}
}