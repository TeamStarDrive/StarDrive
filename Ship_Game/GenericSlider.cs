using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using System;
using System.Collections.Generic;

namespace Ship_Game
{
	public class GenericSlider
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
			this.ContainerRect = r;
			this.rect = new Rectangle(r.X + 9, r.Y + r.Height / 2 + 3, r.Width - 30, 6);
			this.cursor = new Rectangle(this.rect.X + (int)((float)this.rect.Width * this.amount), this.rect.Y + this.rect.Height / 2 - ResourceManager.TextureDict["NewUI/slider_crosshair"].Height / 2, ResourceManager.TextureDict["NewUI/slider_crosshair"].Width, ResourceManager.TextureDict["NewUI/slider_crosshair"].Height);
		}

		public void Draw(Ship_Game.ScreenManager ScreenManager)
		{
			Microsoft.Xna.Framework.Graphics.SpriteBatch SpriteBatch = ScreenManager.SpriteBatch;
			Vector2 Cursor = new Vector2((float)this.ContainerRect.X, (float)this.ContainerRect.Y);
			SpriteBatch.DrawString(Fonts.Arial12Bold, this.Text, Cursor, new Color(255, 239, 208));
			SpriteBatch.Draw(ResourceManager.TextureDict["NewUI/slider_grd_green"], new Rectangle(this.rect.X, this.rect.Y, (int)(this.amount * (float)this.rect.Width), 6), new Rectangle?(new Rectangle(this.rect.X, this.rect.Y, (int)(this.amount * (float)this.rect.Width), 6)), Color.White);
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
			SpriteFont arial12Bold = Fonts.Arial12Bold;
			int num = (int)(this.amount * 100f);
			SpriteBatch.DrawString(arial12Bold, num.ToString(), textPos, new Color(255, 239, 208));
			if (this.Hover && this.Tip_ID != 0)
			{
				ToolTip.CreateTooltip(this.Tip_ID, ScreenManager);
			}
		}

		public void DrawPct(Ship_Game.ScreenManager ScreenManager)
		{
			Microsoft.Xna.Framework.Graphics.SpriteBatch SpriteBatch = ScreenManager.SpriteBatch;
			Vector2 Cursor = new Vector2((float)this.ContainerRect.X, (float)this.ContainerRect.Y);
			SpriteBatch.DrawString(Fonts.Arial12Bold, this.Text, Cursor, new Color(255, 239, 208));
			SpriteBatch.Draw(ResourceManager.TextureDict["NewUI/slider_grd_green"], new Rectangle(this.rect.X, this.rect.Y, (int)(this.amount * (float)this.rect.Width), 6), new Rectangle?(new Rectangle(this.rect.X, this.rect.Y, (int)(this.amount * (float)this.rect.Width), 6)), Color.White);
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
			SpriteFont arial12Bold = Fonts.Arial12Bold;
			int num = (int)(this.amount * 100f);
			SpriteBatch.DrawString(arial12Bold, string.Concat(num.ToString(), "%"), textPos, new Color(255, 239, 208));
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
			this.amount = amt / this.Max;
			this.cursor = new Rectangle(this.rect.X + (int)((float)this.rect.Width * this.amount), this.rect.Y + this.rect.Height / 2 - ResourceManager.TextureDict["NewUI/slider_crosshair"].Height / 2, ResourceManager.TextureDict["NewUI/slider_crosshair"].Width, ResourceManager.TextureDict["NewUI/slider_crosshair"].Height);
		}

		public void UpdatePosition(Vector2 Position, int Width, int Height, string Text)
		{
			this.Text = Text;
			this.ContainerRect = new Rectangle((int)Position.X, (int)Position.Y, Width, Height);
			this.rect = new Rectangle(this.ContainerRect.X, this.ContainerRect.Y + 20, this.ContainerRect.Width - 30, 6);
			this.cursor = new Rectangle(this.rect.X + (int)((float)this.rect.Width * this.amount), this.rect.Y + this.rect.Height / 2 - ResourceManager.TextureDict["NewUI/slider_crosshair"].Height / 2, ResourceManager.TextureDict["NewUI/slider_crosshair"].Width, ResourceManager.TextureDict["NewUI/slider_crosshair"].Height);
		}
	}
}