using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using System;
using System.Collections.Generic;

namespace Ship_Game
{
	public sealed class FloatSlider : IInputHandler
	{
		public Rectangle rect;
		private Rectangle ContainerRect;
		public bool Hover;
		public string Text = "";
		public Rectangle Cursor;
		public int Tip_ID;
		private bool dragging;
        private float bottom;
        private float top ;
        public float AmountRange;

        private float Value = 0.5f;
	    public float Amount
	    {
	        get => Value;
	        set
	        {
	            Value  = value.Clamp(bottom, top);
	            Cursor = new Rectangle(rect.X + (int)(rect.Width * Value), 
	                rect.Y + rect.Height / 2 - SliderKnob.Height / 2,
	                SliderKnob.Width, SliderKnob.Height);
	        }
	    }


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
            Cursor = new Rectangle(rect.X + (int)(rect.Width * Amount), rect.Y + rect.Height / 2 - SliderKnob.Height / 2, SliderKnob.Width, SliderKnob.Height);
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
            AmountRange = defaultValue;
            if (AmountRange > 0 && top > 0)
            {
                if (AmountRange < bottom)
                    AmountRange = bottom;
                if (AmountRange > top + bottom)
                    AmountRange = top + bottom;
                Amount = (AmountRange - bottom) / top;
            }
            else Amount = 0;
            Cursor = new Rectangle(rect.X + (int)(rect.Width * Amount), rect.Y + rect.Height / 2 - SliderKnob.Height / 2, SliderKnob.Width, SliderKnob.Height);
        }

        public bool HitTest(Vector2 pos) => ContainerRect.HitTest(pos);

        private void Draw(ScreenManager screenManager, string valueIndicator)
        {
            SpriteBatch sb = screenManager.SpriteBatch;
            sb.DrawString(Fonts.Arial12Bold, Text, new Vector2(ContainerRect.X + 10, ContainerRect.Y), TextColor);

            var gradient = new Rectangle(rect.X, rect.Y, (int)(Amount * rect.Width), 6);
            sb.Draw(SliderGradient, gradient, gradient, Color.White);
            sb.DrawRectangle(rect, Hover ? HoverColor : NormalColor);

            for (int i = 0; i < 11; i++)
            {
                Vector2 tickCursor = new Vector2(rect.X + rect.Width / 10 * i, rect.Y + rect.Height + 2);
                sb.Draw(Hover ? SliderMinuteHover : SliderMinute, tickCursor, Color.White);
            }

            Rectangle drawRect = Cursor;
            drawRect.X = drawRect.X - drawRect.Width / 2;
            sb.Draw(Hover ? SliderKnobHover : SliderKnob, drawRect, Color.White);

            Vector2 textPos = new Vector2(rect.X + rect.Width + 8, rect.Y + rect.Height / 2 - Fonts.Arial12Bold.LineSpacing / 2);
            sb.DrawString(Fonts.Arial12Bold, valueIndicator, textPos, new Color(255, 239, 208));

            if (Hover && Tip_ID != 0)
            {
                ToolTip.CreateTooltip(Localizer.Token(Tip_ID));
            }
        }

	    public void DrawPercent(ScreenManager screenManager)
		{
            int num = (int)(Amount * 100f);
            Draw(screenManager, num.ToString("00") + "%");
		}

		public void DrawDecimal(ScreenManager screenManager)
		{
            int num = (int)(Amount * top + bottom);
            Draw(screenManager, num.ToString());
		}

		public bool HandleInput(InputState input)
		{
            Hover = rect.HitTest(input.CursorPosition);

			Rectangle clickCursor = Cursor;
			clickCursor.X -= Cursor.Width / 2;

			if (clickCursor.HitTest(input.CursorPosition) && input.LeftMouseHeldDown)
				dragging = true;

			if (dragging)
			{
				Cursor.X = (int)input.CursorPosition.X;
				if (Cursor.X > rect.X + rect.Width) Cursor.X = rect.X + rect.Width;
				else if (Cursor.X < rect.X)         Cursor.X = rect.X;

				if (input.LeftMouseReleased)
					dragging = false;
				Amount = 1f - (rect.X + rect.Width - Cursor.X) / (float)rect.Width;
			}
            AmountRange = bottom + Amount * top;
            return dragging;
		}

	}
}