using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;

namespace Ship_Game
{
	public sealed class GenericSlider
	{
		public Rectangle rect;

		public Rectangle ContainerRect;

		public bool Hover;

		public string Text = "";

		public float amount = 0.5f;

		public Rectangle cursor;

		public int Tip_ID;

		//private Rectangle redRect;

		//private Rectangle greenRect;

		private float Min;

		private float Max;

		private bool dragging;

		public GenericSlider(Rectangle r, string Text, float Min, float Max)
		{
			this.Min = Min;
			this.Max = Max;
			this.Text = Text;
			ContainerRect = r;
			rect = new Rectangle(r.X + 9, r.Y + r.Height / 2 + 3, r.Width - 30, 6);
			cursor = new Rectangle(rect.X + (int)(rect.Width * amount), rect.Y + rect.Height / 2 - ResourceManager.Texture("NewUI/slider_crosshair").Height / 2, ResourceManager.Texture("NewUI/slider_crosshair").Width, ResourceManager.Texture("NewUI/slider_crosshair").Height);
		}

		public void Draw(ScreenManager ScreenManager)
		{
			SpriteBatch SpriteBatch = ScreenManager.SpriteBatch;
			Vector2 Cursor = new Vector2(ContainerRect.X, ContainerRect.Y);
			SpriteBatch.DrawString(Fonts.Arial12Bold, Text, Cursor, new Color(255, 239, 208));
			SpriteBatch.Draw(ResourceManager.Texture("NewUI/slider_grd_green"), 
                new Rectangle(rect.X, rect.Y, (int)(amount * rect.Width), 6), Color.White);
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
			SpriteFont arial12Bold = Fonts.Arial12Bold;
			int num = (int)(amount * 100f);
			SpriteBatch.DrawString(arial12Bold, num.ToString(), textPos, new Color(255, 239, 208));
			if (Hover && Tip_ID != 0)
			{
				ToolTip.CreateTooltip(Tip_ID);
			}
		}

		public void DrawPct(SpriteBatch batch)
		{
			Vector2 Cursor = new Vector2(ContainerRect.X, ContainerRect.Y);
            batch.DrawString(Fonts.Arial12Bold, Text, Cursor, new Color(255, 239, 208));
            batch.Draw(ResourceManager.Texture("NewUI/slider_grd_green"), 
                new Rectangle(rect.X, rect.Y, (int)(amount * rect.Width), 6), Color.White);
            batch.DrawRectangle(rect, (Hover ? new Color(164, 154, 133) : new Color(72, 61, 38)));
			Vector2 tickCursor = new Vector2();
			for (int i = 0; i < 11; i++)
			{
				tickCursor = new Vector2(rect.X + rect.Width / 10 * i, rect.Y + rect.Height + 2);
				if (Hover)
				{
                    batch.Draw(ResourceManager.Texture("NewUI/slider_minute_hover"), tickCursor, Color.White);
				}
				else
				{
                    batch.Draw(ResourceManager.Texture("NewUI/slider_minute"), tickCursor, Color.White);
				}
			}
			Rectangle drawRect = cursor;
			drawRect.X = drawRect.X - drawRect.Width / 2;
			if (Hover)
			{
                batch.Draw(ResourceManager.Texture("NewUI/slider_crosshair_hover"), drawRect, Color.White);
			}
			else
			{
                batch.Draw(ResourceManager.Texture("NewUI/slider_crosshair"), drawRect, Color.White);
			}
			Vector2 textPos = new Vector2(rect.X + rect.Width + 8, rect.Y + rect.Height / 2 - Fonts.Arial12Bold.LineSpacing / 2);
			SpriteFont arial12Bold = Fonts.Arial12Bold;
			int num = (int)(amount * 100f);
            batch.DrawString(arial12Bold, string.Concat(num.ToString(), "%"), textPos, new Color(255, 239, 208));
			if (Hover && Tip_ID != 0)
			{
				ToolTip.CreateTooltip(Tip_ID);
			}
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
			amount = amt / Max;
			cursor = new Rectangle(rect.X + (int)(rect.Width * amount), rect.Y + rect.Height / 2 - ResourceManager.Texture("NewUI/slider_crosshair").Height / 2, ResourceManager.Texture("NewUI/slider_crosshair").Width, ResourceManager.Texture("NewUI/slider_crosshair").Height);
		}

		public void UpdatePosition(Vector2 Position, int Width, int Height, string Text)
		{
			this.Text = Text;
			ContainerRect = new Rectangle((int)Position.X, (int)Position.Y, Width, Height);
			rect = new Rectangle(ContainerRect.X, ContainerRect.Y + 20, ContainerRect.Width - 30, 6);
			cursor = new Rectangle(rect.X + (int)(rect.Width * amount), rect.Y + rect.Height / 2 - ResourceManager.Texture("NewUI/slider_crosshair").Height / 2, ResourceManager.Texture("NewUI/slider_crosshair").Width, ResourceManager.Texture("NewUI/slider_crosshair").Height);
		}

        public void Draw(SpriteBatch batch, float x, float y, int width, int height, string text)
        {
            UpdatePosition(new Vector2(x,y), width, height, text);
            DrawPct(batch);
        }
	}
}