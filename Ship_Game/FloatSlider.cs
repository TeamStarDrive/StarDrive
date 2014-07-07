using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using System;
using System.Collections.Generic;

namespace Ship_Game
{
	public class FloatSlider
	{
		public Rectangle rect;

		public Rectangle ContainerRect;

		public bool Hover;

		public string Text = "";

		public float amount = 0.5f;

		public Rectangle cursor;

		public int Tip_ID;

		private Rectangle redRect;

		private Rectangle greenRect;

		private bool dragging;

        private float bottom;
        private float top ;
        public float amountRange;

		public FloatSlider(Rectangle r, string Text)
		{
			this.Text = Text;
			this.ContainerRect = r;
			this.rect = new Rectangle(r.X + 9, r.Y + r.Height / 2 + 3, r.Width - 30, 6);
			this.cursor = new Rectangle(this.rect.X + (int)((float)this.rect.Width * this.amount), this.rect.Y + this.rect.Height / 2 - ResourceManager.TextureDict["NewUI/slider_crosshair"].Height / 2, ResourceManager.TextureDict["NewUI/slider_crosshair"].Width, ResourceManager.TextureDict["NewUI/slider_crosshair"].Height);
			this.redRect = new Rectangle(this.rect.X, this.rect.Y, this.rect.Width / 2, 6);
			this.greenRect = new Rectangle(this.rect.X + this.rect.Width / 2, this.rect.Y, this.rect.Width / 2, 6);
            this.bottom = 0;
            this.top = 10000f;
		}
        //added by gremlin trying to simplify the use of this slider.
        public FloatSlider(Rectangle r, string Text, float bottomRange, float topRange, float defaultValue)
        {
            this.Text = Text;
            this.ContainerRect = r;
            this.rect = new Rectangle(r.X + 9, r.Y + r.Height / 2 + 3, r.Width - 30, 6);
            this.cursor = new Rectangle(this.rect.X + (int)((float)this.rect.Width * this.amount), this.rect.Y + this.rect.Height / 2 - ResourceManager.TextureDict["NewUI/slider_crosshair"].Height / 2, ResourceManager.TextureDict["NewUI/slider_crosshair"].Width, ResourceManager.TextureDict["NewUI/slider_crosshair"].Height);
            this.redRect = new Rectangle(this.rect.X, this.rect.Y, this.rect.Width / 2, 6);
            this.greenRect = new Rectangle(this.rect.X + this.rect.Width / 2, this.rect.Y, this.rect.Width / 2, 6);
            this.bottom = bottomRange;
            this.top = topRange;
            this.amountRange = defaultValue;
            if (this.amountRange > 0 && this.top > 0)
            {
                if (this.amountRange < this.bottom)
                    this.amountRange = this.bottom;
                if (this.amountRange > this.top)
                    this.amountRange = this.top;
                this.amount = (this.amountRange ) / (this.top + this.bottom);
                this.amount = this.amount < 0 ? 0: this.amount;
            }
            else
                this.amount = 0;
            this.cursor = new Rectangle(this.rect.X + (int)((float)this.rect.Width * this.amount), this.rect.Y + this.rect.Height / 2 - ResourceManager.TextureDict["NewUI/slider_crosshair"].Height / 2, ResourceManager.TextureDict["NewUI/slider_crosshair"].Width, ResourceManager.TextureDict["NewUI/slider_crosshair"].Height);

        }

		public void Draw(Ship_Game.ScreenManager ScreenManager)
		{


            Microsoft.Xna.Framework.Graphics.SpriteBatch SpriteBatch = ScreenManager.SpriteBatch;
			Vector2 Cursor = new Vector2((float)(this.ContainerRect.X + 10), (float)this.ContainerRect.Y);
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
			int num = (int)(this.amount * this.top+this.bottom);
			SpriteBatch.DrawString(arial12Bold, num.ToString(), textPos, new Color(255, 239, 208));
			if (this.Hover && this.Tip_ID != 0)
			{
				ToolTip.CreateTooltip(this.Tip_ID, ScreenManager);
			}
		}

		public void DrawDecimal(Ship_Game.ScreenManager ScreenManager)
		{
			Microsoft.Xna.Framework.Graphics.SpriteBatch SpriteBatch = ScreenManager.SpriteBatch;
			Vector2 Cursor = new Vector2((float)(this.ContainerRect.X + 10), (float)this.ContainerRect.Y);
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
			int num = (int)((float)(this.amount * 100f));
			string amt = string.Concat(num.ToString("00"), "%");
			SpriteBatch.DrawString(Fonts.Arial12Bold, amt, textPos, new Color(255, 239, 208));
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
            this.amountRange = this.amount * (this.top + this.bottom);
			return this.amount;

		}

		public void SetAmount(float amt)
		{
            //if (this.amountRange != (this.amount *this.top +this.bottom))//this.amountRange > 0 && this.top > 0)
            //{

            //    this.amount = this.amountRange / this.top - this.bottom;
            //    this.amount=this.amount < 0 ? this.amount = 0 : this.amount;
            //}
            //else
            //{
                this.amount = amt;
            //}
			this.cursor = new Rectangle(this.rect.X + (int)((float)this.rect.Width * this.amount), this.rect.Y + this.rect.Height / 2 - ResourceManager.TextureDict["NewUI/slider_crosshair"].Height / 2, ResourceManager.TextureDict["NewUI/slider_crosshair"].Width, ResourceManager.TextureDict["NewUI/slider_crosshair"].Height);
		}


	}
}