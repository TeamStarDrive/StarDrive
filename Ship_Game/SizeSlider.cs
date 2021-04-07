using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;

namespace Ship_Game
{
	public sealed class SizeSlider
	{
		public Rectangle rect;

		public Rectangle ContainerRect;

		public bool Hover;

		public string Text = "";

		public float amount = 0.5f;

		public Rectangle cursor;

		//private string fmt = "0.#";

		public LocalizedText Tooltip;

		private Rectangle redRect;

		private Rectangle greenRect;

		private bool dragging;

		public SizeSlider(Rectangle r, string Text)
		{
			this.Text = Text;
			ContainerRect = r;
			rect = new Rectangle(r.X + 9, r.Y + r.Height / 2 + 3, r.Width - 30, 6);
			cursor = new Rectangle(rect.X + (int)(rect.Width * amount), rect.Y + rect.Height / 2 - ResourceManager.Texture("NewUI/slider_crosshair").Height / 2, ResourceManager.Texture("NewUI/slider_crosshair").Width, ResourceManager.Texture("NewUI/slider_crosshair").Height);
			redRect = new Rectangle(rect.X, rect.Y, rect.Width / 2, 6);
			greenRect = new Rectangle(rect.X + rect.Width / 2, rect.Y, rect.Width / 2, 6);
		}

		public void Draw(ScreenManager ScreenManager)
		{
			SpriteBatch SpriteBatch = ScreenManager.SpriteBatch;
			Vector2 Cursor = new Vector2(ContainerRect.X + 10, ContainerRect.Y);
			SpriteBatch.DrawString(Fonts.Arial12Bold, Text, Cursor, Colors.Cream);
			if (amount > 0.5f)
			{
				float greenamount = 2f * (amount - 0.5f);
				SpriteBatch.Draw(ResourceManager.Texture("NewUI/slider_grd_green"), 
                    new Rectangle(greenRect.X, rect.Y, (int)(greenamount * greenRect.Width), 6), Color.White);
			}
			else if (amount < 0.5f)
			{
				float blackAmount = 2f * amount;
				SpriteBatch.FillRectangle(redRect, Color.Maroon);
				SpriteBatch.FillRectangle(new Rectangle(redRect.X, rect.Y, (int)(blackAmount * redRect.Width), 6), Color.Black);
			}
			SpriteBatch.DrawRectangle(rect, (Hover ? new Color(164, 154, 133) : new Color(72, 61, 38)));
			Vector2 tickCursor = new Vector2();
			for (int i = 0; i < 11; i++)
			{
				tickCursor = new Vector2(rect.X + rect.Width / 10 * i, rect.Y + rect.Height + 2);
				if (Hover)
				{
					SpriteBatch.Draw(ResourceManager.Texture("NewUI/slider_minute_hover"), tickCursor, Color.White);
				}
				else
				{
					SpriteBatch.Draw(ResourceManager.Texture("NewUI/slider_minute"), tickCursor, Color.White);
				}
			}
			Rectangle drawRect = cursor;
			drawRect.X = drawRect.X - drawRect.Width / 2;
			if (Hover)
			{
				SpriteBatch.Draw(ResourceManager.Texture("NewUI/slider_crosshair_hover"), drawRect, Color.White);
			}
			else
			{
				SpriteBatch.Draw(ResourceManager.Texture("NewUI/slider_crosshair"), drawRect, Color.White);
			}
			Vector2 textPos = new Vector2(rect.X + rect.Width + 8, rect.Y + rect.Height / 2 - Fonts.Arial12Bold.LineSpacing / 2);
			string text = "";
			if (amount > 0.5f)
			{
				text = "Bigger";
			}
			if (amount < 0.5f)
			{
				text = "Smaller";
			}
			if (amount == 0.5f)
			{
				text = "Similar";
			}
			SpriteBatch.DrawString(Fonts.Arial12Bold, text, textPos, Colors.Cream);
			if (Hover && Tooltip.IsValid)
			{
				ToolTip.CreateTooltip(Tooltip);
			}
		}

        public bool HandleInput(InputState input, ref float  currentvalue)
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
			if (clickCursor.HitTest(input.CursorPosition) && input.LeftMouseHeldDown)
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
			cursor = new Rectangle(rect.X + (int)(rect.Width * amount), rect.Y + rect.Height / 2 - ResourceManager.Texture("NewUI/slider_crosshair").Height / 2, ResourceManager.Texture("NewUI/slider_crosshair").Width, ResourceManager.Texture("NewUI/slider_crosshair").Height);
		}
	}
}