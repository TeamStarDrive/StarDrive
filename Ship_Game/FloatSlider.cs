using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using System;
using System.Collections.Generic;

namespace Ship_Game
{
	public sealed class FloatSlider
	{
		public Rectangle rect;
		private Rectangle ContainerRect;
		public bool Hover;
		public string Text = "";
		public float amount = 0.5f;
		public Rectangle cursor;
		public int Tip_ID;
		private bool dragging;
        private float bottom;
        private float top ;
        public float amountRange;

        private static readonly Color TextColor = new Color(255, 239, 208);
        private static readonly Color HoverColor = new Color(164, 154, 133);
        private static readonly Color NormalColor = new Color(72, 61, 38);

        private static Texture2D SliderKnob;
        private static Texture2D SliderKnobHover;
        private static Texture2D SliderMinute;
        private static Texture2D SliderMinuteHover;
        private static Texture2D SliderGradient;   // background gradient for the slider

        private FloatSlider(Rectangle r)
        {
            if (SliderKnob        == null) SliderKnob        = ResourceManager.TextureDict["NewUI/slider_crosshair"];
            if (SliderKnobHover   == null) SliderKnobHover   = ResourceManager.TextureDict["NewUI/slider_crosshair_hover"];
            if (SliderMinute      == null) SliderMinute      = ResourceManager.TextureDict["NewUI/slider_minute"];
            if (SliderMinuteHover == null) SliderMinuteHover = ResourceManager.TextureDict["NewUI/slider_minute_hover"];
            if (SliderGradient    == null) SliderGradient    = ResourceManager.TextureDict["NewUI/slider_grd_green"];

            ContainerRect = r;
            rect   = new Rectangle(r.X + 9, r.Y + r.Height / 2 + 3, r.Width - 30, 6);
        }

        public FloatSlider(Rectangle r, string text) : this(r)
		{
			Text   = text;
            bottom = 0;
            top    = 10000f;
            cursor = new Rectangle(rect.X + (int)(rect.Width * amount), rect.Y + rect.Height / 2 - SliderKnob.Height / 2, SliderKnob.Width, SliderKnob.Height);
        }
        public FloatSlider(Rectangle r, int text) : this(r, Localizer.Token(text))
        {
        }

        //added by gremlin trying to simplify the use of this slider.
        public FloatSlider(Rectangle r, string text, float bottomRange, float topRange, float defaultValue) : this(r)
        {
            Text        = text;
            bottom      = bottomRange;
            top         = topRange;
            amountRange = defaultValue;
            if (amountRange > 0 && top > 0)
            {
                if (amountRange < bottom)
                    amountRange = bottom;
                if (amountRange > top + bottom)
                    amountRange = top + bottom;
                amount = (amountRange - bottom) / top;
            }
            else amount = 0;
            cursor = new Rectangle(rect.X + (int)(rect.Width * amount), rect.Y + rect.Height / 2 - SliderKnob.Height / 2, SliderKnob.Width, SliderKnob.Height);
        }

        public bool HitTest(Vector2 pos) => ContainerRect.HitTest(pos);

        private void Draw(ScreenManager screenManager, string valueIndicator)
        {
            SpriteBatch sb = screenManager.SpriteBatch;
            sb.DrawString(Fonts.Arial12Bold, Text, new Vector2(ContainerRect.X + 10, ContainerRect.Y), TextColor);

            var gradient = new Rectangle(rect.X, rect.Y, (int)(amount * rect.Width), 6);
            sb.Draw(SliderGradient, gradient, gradient, Color.White);
            Primitives2D.DrawRectangle(sb, rect, Hover ? HoverColor : NormalColor);

            for (int i = 0; i < 11; i++)
            {
                Vector2 tickCursor = new Vector2(rect.X + rect.Width / 10 * i, rect.Y + rect.Height + 2);
                sb.Draw(Hover ? SliderMinuteHover : SliderMinute, tickCursor, Color.White);
            }

            Rectangle drawRect = cursor;
            drawRect.X = drawRect.X - drawRect.Width / 2;
            sb.Draw(Hover ? SliderKnobHover : SliderKnob, drawRect, Color.White);

            Vector2 textPos = new Vector2(rect.X + rect.Width + 8, rect.Y + rect.Height / 2 - Fonts.Arial12Bold.LineSpacing / 2);
            sb.DrawString(Fonts.Arial12Bold, valueIndicator, textPos, new Color(255, 239, 208));

            if (Hover && Tip_ID != 0)
            {
                ToolTip.CreateTooltip(Localizer.Token(Tip_ID), screenManager);
            }
        }

	    public void DrawPercent(ScreenManager screenManager)
		{
            int num = (int)(amount * 100f);
            Draw(screenManager, num.ToString("00") + "%");
		}

		public void DrawDecimal(ScreenManager screenManager)
		{
            int num = (int)(amount * top + bottom);
            Draw(screenManager, num.ToString());
		}

		public float HandleInput(InputState input)
		{
            Hover = rect.HitTest(input.CursorPosition);

			Rectangle clickCursor = cursor;
			clickCursor.X -= cursor.Width / 2;
			if (clickCursor.HitTest(input.CursorPosition) && 
                input.CurrentMouseState.LeftButton == ButtonState.Pressed && 
                input.LastMouseState.LeftButton    == ButtonState.Pressed)
			{
				dragging = true;
			}
			if (dragging)
			{
				cursor.X = (int)input.CursorPosition.X;
				if (cursor.X > rect.X + rect.Width) cursor.X = rect.X + rect.Width;
				else if (cursor.X < rect.X)         cursor.X = rect.X;

				if (input.CurrentMouseState.LeftButton == ButtonState.Released)
					dragging = false;
				amount = 1f - (rect.X + rect.Width - cursor.X) / (float)rect.Width;
			}
            amountRange = bottom + amount * top;
			return amount;
		}

		public void SetAmount(float amt)
		{
            amount = amt;
            cursor = new Rectangle(rect.X + (int)(rect.Width * amt), 
                                   rect.Y + rect.Height / 2 - SliderKnob.Height / 2,
                                   SliderKnob.Width, SliderKnob.Height);
		}


	}
}