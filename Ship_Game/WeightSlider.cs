using Microsoft.Xna.Framework.Graphics;
using SDGraphics;
using Vector2 = SDGraphics.Vector2;
using Rectangle = SDGraphics.Rectangle;

namespace Ship_Game
{
	public sealed class WeightSlider
	{
		public readonly RectF Rect;
		public RectF ContainerRect;
		public string Text;

		public bool Hover;
		public float Amount = 0.5f;

		public RectF Cursor;
		RectF RedRect;
		RectF GreenRect;

		public LocalizedText Tooltip;

		bool dragging;

		public WeightSlider(RectF r, string text, LocalizedText tooltip)
		{
			Text = text;
			Tooltip = tooltip;
			Rect = new(r.X + 9, r.CenterY + 3, 120, 6);
			ContainerRect = r;

            var tex = ResourceManager.Texture("NewUI/slider_crosshair");
			Cursor = new(Rect.X + Rect.W * Amount, Rect.Y + Rect.H / 2 - tex.CenterY, tex.SizeF);
			RedRect = new(Rect.X, Rect.Y, Rect.W / 2, 6);
			GreenRect = new(Rect.X + Rect.W / 2, Rect.Y, Rect.W / 2, 6);
		}

		public void Draw(SpriteBatch batch)
		{
			Vector2 Cursor = new Vector2(ContainerRect.X + 10, ContainerRect.Y);
			batch.DrawString(Fonts.Arial12Bold, Text, Cursor, Colors.Cream);
			if (Amount > 0.5f)
			{
				float greenamount = 2f * (Amount - 0.5f);
				batch.Draw(ResourceManager.Texture("NewUI/slider_grd_green"), 
                           new RectF(GreenRect.X, Rect.Y, (int)(greenamount * GreenRect.W), 6), Color.White);
			}
			else if (Amount < 0.5f)
			{
				float blackAmount = 2f * Amount;
				batch.FillRectangle(RedRect, Color.Maroon);
				batch.FillRectangle(new(RedRect.X, Rect.Y, (int)(blackAmount * RedRect.W), 6), Color.Black);
			}
			batch.DrawRectangle(Rect, (Hover ? new(164, 154, 133) : new(72, 61, 38)));

			for (int i = 0; i < 11; i++)
			{
				Vector2 tickCursor = new(Rect.X + Rect.W / 10 * i, Rect.Bottom + 2);
				if (Hover)
				{
					batch.Draw(ResourceManager.Texture("NewUI/slider_minute_hover"), tickCursor, Color.White);
				}
				else
				{
					batch.Draw(ResourceManager.Texture("NewUI/slider_minute"), tickCursor, Color.White);
				}
			}
            Rectangle drawRect = this.Cursor;
			drawRect.X = drawRect.X - drawRect.Width / 2;
			if (Hover)
			{
				batch.Draw(ResourceManager.Texture("NewUI/slider_crosshair_hover"), drawRect, Color.White);
			}
			else
			{
				batch.Draw(ResourceManager.Texture("NewUI/slider_crosshair"), drawRect, Color.White);
			}
			if (Hover && Tooltip.IsValid)
			{
				ToolTip.CreateTooltip(Tooltip);
			}
			Vector2 textPos = new(Rect.X + Rect.W + 8, Rect.Y + Rect.H / 2 - Fonts.Arial12Bold.LineSpacing / 2);
			float single = 2f * (Amount - 0.5f);
			batch.DrawString(Fonts.Arial12Bold, single.ToString("0.#"), textPos, Colors.Cream);
		}

	    public bool HandleInput(InputState input, ref float currentvalue)
	    {
	        if (!Rect.HitTest(input.CursorPosition) || !input.LeftMouseHeld())
	        {
	            SetAmount(currentvalue);
	            return false;
	        }
	        currentvalue = HandleInput(input);
	        return true;

	    }

        public float HandleInput(InputState input)
		{
			if (!Rect.HitTest(input.CursorPosition))
			{
				Hover = false;
			}
			else
			{
				Hover = true;
			}

			RectF clickCursor = Cursor;
			clickCursor.X -= Cursor.W / 2;
			if (clickCursor.HitTest(input.CursorPosition) && input.LeftMouseHeldDown)
			{
				dragging = true;
			}
			if (dragging)
			{
				Cursor.X = (int)input.CursorPosition.X;
				if (Cursor.X > Rect.Right)
				{
					Cursor.X = Rect.Right;
				}
				else if (Cursor.X < Rect.X)
				{
					Cursor.X = Rect.X;
				}
				if (input.LeftMouseUp)
				{
					dragging = false;
				}
				Amount = 1f - (Rect.Right - Cursor.X) / Rect.W;
			}
			return Amount;
		}

		public void SetAmount(float amt)
		{
			Amount = amt;
            var tex = ResourceManager.Texture("NewUI/slider_crosshair");
			Cursor = new(Rect.X + (int)(Rect.W * Amount), Rect.Y + Rect.H / 2 - tex.CenterY, tex.SizeF);
		}
	}
}