using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;

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
			ContainerRect = r;
			rect = new Rectangle(r.X + 9, r.Y + r.Height / 2 + 3, 120, 6);
            var tex = ResourceManager.Texture("NewUI/slider_crosshair");
			cursor = new Rectangle(rect.X + (int)(rect.Width * amount), rect.Y + rect.Height / 2 - tex.Height / 2, tex.Width, tex.Height);
			redRect = new Rectangle(rect.X, rect.Y, rect.Width / 2, 6);
			greenRect = new Rectangle(rect.X + rect.Width / 2, rect.Y, rect.Width / 2, 6);
		}

		public void Draw(ScreenManager ScreenManager)
		{
			Vector2 Cursor = new Vector2(ContainerRect.X + 10, ContainerRect.Y);
			ScreenManager.SpriteBatch.DrawString(Fonts.Arial12Bold, Text, Cursor, new Color(255, 239, 208));
			if (amount > 0.5f)
			{
				float greenamount = 2f * (amount - 0.5f);
				ScreenManager.SpriteBatch.Draw(ResourceManager.Texture("NewUI/slider_grd_green"), 
                    new Rectangle(greenRect.X, rect.Y, (int)(greenamount * greenRect.Width), 6), Color.White);
			}
			else if (amount < 0.5f)
			{
				float blackAmount = 2f * amount;
				ScreenManager.SpriteBatch.FillRectangle(redRect, Color.Maroon);
				ScreenManager.SpriteBatch.FillRectangle(new Rectangle(redRect.X, rect.Y, (int)(blackAmount * redRect.Width), 6), Color.Black);
			}
			ScreenManager.SpriteBatch.DrawRectangle(rect, (Hover ? new Color(164, 154, 133) : new Color(72, 61, 38)));
			Vector2 tickCursor = new Vector2();
			for (int i = 0; i < 11; i++)
			{
				tickCursor = new Vector2(rect.X + rect.Width / 10 * i, rect.Y + rect.Height + 2);
				if (Hover)
				{
					ScreenManager.SpriteBatch.Draw(ResourceManager.Texture("NewUI/slider_minute_hover"), tickCursor, Color.White);
				}
				else
				{
					ScreenManager.SpriteBatch.Draw(ResourceManager.Texture("NewUI/slider_minute"), tickCursor, Color.White);
				}
			}
			Rectangle drawRect = cursor;
			drawRect.X = drawRect.X - drawRect.Width / 2;
			if (Hover)
			{
				ScreenManager.SpriteBatch.Draw(ResourceManager.Texture("NewUI/slider_crosshair_hover"), drawRect, Color.White);
			}
			else
			{
				ScreenManager.SpriteBatch.Draw(ResourceManager.Texture("NewUI/slider_crosshair"), drawRect, Color.White);
			}
			if (Hover && Tip_ID != 0)
			{
				ToolTip.CreateTooltip(Tip_ID);
			}
			Vector2 textPos = new Vector2(rect.X + rect.Width + 8, rect.Y + rect.Height / 2 - Fonts.Arial12Bold.LineSpacing / 2);
			float single = 2f * (amount - 0.5f);
			ScreenManager.SpriteBatch.DrawString(Fonts.Arial12Bold, single.ToString(fmt), textPos, new Color(255, 239, 208));
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
			if (!rect.HitTest(input.CursorPosition))
			{
				Hover = false;
			}
			else
			{
				Hover = true;
			}
			Rectangle clickCursor = cursor;
			clickCursor.X = clickCursor.X - cursor.Width / 2;
			if (clickCursor.HitTest(input.CursorPosition) && input.MouseCurr.LeftButton == ButtonState.Pressed && input.MousePrev.LeftButton == ButtonState.Pressed)
			{
				dragging = true;
			}
			if (dragging)
			{
				cursor.X = (int)input.CursorPosition.X;
				if (cursor.X > rect.X + rect.Width)
				{
					cursor.X = rect.X + rect.Width;
				}
				else if (cursor.X < rect.X)
				{
					cursor.X = rect.X;
				}
				if (input.LeftMouseUp)
				{
					dragging = false;
				}
				amount = 1f - (rect.X + (float)rect.Width - cursor.X) / rect.Width;
			}
			return amount;
		}

		public void SetAmount(float amt)
		{
			amount = amt;
            var tex = ResourceManager.Texture("NewUI/slider_crosshair");
			cursor = new Rectangle(rect.X + (int)(rect.Width * amount), rect.Y + rect.Height / 2 - tex.Height / 2, tex.Width, tex.Height);
		}
	}
}