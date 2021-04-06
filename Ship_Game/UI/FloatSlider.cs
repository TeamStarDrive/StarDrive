using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace Ship_Game
{
    public enum SliderStyle
    {
        Decimal, // example: 42000
        Percent, // example: 51%
        Decimal1 // example: 0.5
    }

    public sealed class FloatSlider : UIElementV2
    {
        Rectangle SliderRect; // colored slider
        Rectangle KnobRect;   // knob area used to move the slider value
        public LocalizedText Text;
        public ToolTipText Tip;

        public Action<FloatSlider> OnChange;

        bool Hover, Dragging;
        float Min, Max, Value;
        public SliderStyle Style = SliderStyle.Decimal;

        public float Range => Max-Min;
        public float AbsoluteValue
        {
            get => Min + RelativeValue * Range;
            set
            {
                RelativeValue = (value.Clamped(Min, Max) - Min) / Range;
                RequiresLayout = true;
                UpdateSliderRect();
            }
        }

        public float RelativeValue
        {
            get => Value;
            set
            {
                Value = value.Clamped(0f, 1f);
                RequiresLayout = true;
                UpdateSliderRect();
                OnChange?.Invoke(this);
            }
        }

        public override string ToString() => $"{TypeName} {ElementDescr} r:{Value} a:{AbsoluteValue} [{Min}..{Max}] {Text}";

        static readonly Color TextColor   = Colors.Cream;
        static readonly Color HoverColor  = new Color(164, 154, 133);
        static readonly Color NormalColor = new Color(72, 61, 38);

        static int ContentId;
        static SubTexture SliderKnob;
        static SubTexture SliderKnobHover;
        static SubTexture SliderMinute;
        static SubTexture SliderMinuteHover;
        static SubTexture SliderGradient;   // background gradient for the slider

        public FloatSlider(Rectangle r, LocalizedText text, float min = 0f, float max = 10000f, float value = 5000f)
            : base(r)
        {
            if (SliderKnob == null || ContentId != ResourceManager.ContentId)
            {
                ContentId = ResourceManager.ContentId;
                SliderKnob        = ResourceManager.Texture("NewUI/slider_crosshair");
                SliderKnobHover   = ResourceManager.Texture("NewUI/slider_crosshair_hover");
                SliderMinute      = ResourceManager.Texture("NewUI/slider_minute");
                SliderMinuteHover = ResourceManager.Texture("NewUI/slider_minute_hover");
                SliderGradient    = ResourceManager.Texture("NewUI/slider_grd_green");
            }

            Text  = text;
            Min   = min;
            Max   = max;
            Value = (value.Clamped(Min, Max) - Min) / Range;
            UpdateSliderRect();
        }

        public FloatSlider(SliderStyle style, Rectangle r, LocalizedText text, float min, float max, float value)
            : this(r, text, min, max, value)
        {
            Style = style;
        }

        public FloatSlider(SliderStyle style, Vector2 size, LocalizedText text, float min, float max, float value)
            : this(new Rectangle(0, 0,(int)size.X, (int)size.Y), text, min, max, value)
        {
            Style = style;
        }

        public FloatSlider(SliderStyle style, float w, float h, LocalizedText text, float min, float max, float value)
        {
            if (SliderKnob == null || ContentId != ResourceManager.ContentId)
            {
                ContentId = ResourceManager.ContentId;
                SliderKnob        = ResourceManager.Texture("NewUI/slider_crosshair");
                SliderKnobHover   = ResourceManager.Texture("NewUI/slider_crosshair_hover");
                SliderMinute      = ResourceManager.Texture("NewUI/slider_minute");
                SliderMinuteHover = ResourceManager.Texture("NewUI/slider_minute_hover");
                SliderGradient    = ResourceManager.Texture("NewUI/slider_grd_green");
            }

            Size = new Vector2(w, h);
            Text  = text;
            Min   = min;
            Max   = max;
            Value = (value.Clamped(Min, Max) - Min) / Range;
            Style = style;
            UpdateSliderRect();
        }

        void UpdateSliderRect()
        {
            SliderRect = new Rectangle((int)Pos.X, (int)Pos.Y + (int)Height/2 + 3, (int)Width - 32, 6);
            KnobRect = new Rectangle(SliderRect.X + (int)(SliderRect.Width * Value), 
                                     SliderRect.Y + SliderRect.Height / 2 - SliderKnob.Height / 2, 
                                     SliderKnob.Width, SliderKnob.Height);
        }

        public override void PerformLayout()
        {
            if (!Visible)
                return;

            base.PerformLayout();
            UpdateSliderRect();
        }

        public string StyledValue
        {
            get
            {
                switch (Style)
                {
                    case SliderStyle.Decimal:  return ((int)AbsoluteValue).ToString();
                    case SliderStyle.Decimal1: return (AbsoluteValue).String(1);
                    default:                   return (RelativeValue * 100f).ToString("00") + "%";
                }
            }
        }

        public override void Draw(SpriteBatch batch, DrawTimes elapsed)
        {
            if (!Visible)
                return;

            batch.DrawString(Fonts.Arial12Bold, Text, Pos, TextColor);

            var gradient = new Rectangle(SliderRect.X, SliderRect.Y, (int)(RelativeValue * SliderRect.Width), 6);
            batch.Draw(SliderGradient, gradient, Color.White);
            batch.DrawRectangle(SliderRect, Hover ? HoverColor : NormalColor);

            var tickPos = new Vector2(SliderRect.X, SliderRect.Bottom + 1);
            for (int i = 0; i < 11; i++)
            {
                tickPos.X = SliderRect.X + (int)(((SliderRect.Width-1) / 10f)*i); // @note Yeah, cast is important
                batch.Draw(Hover ? SliderMinuteHover : SliderMinute, tickPos, Color.White);
            }

            Rectangle knobRect = KnobRect;
            knobRect.X -= knobRect.Width / 2;
            batch.Draw(Hover ? SliderKnobHover : SliderKnob, knobRect, Color.White);

            var textPos = new Vector2(SliderRect.X + SliderRect.Width + 8, SliderRect.Y + SliderRect.Height / 2 - Fonts.Arial12Bold.LineSpacing / 2);
            batch.DrawString(Fonts.Arial12Bold, StyledValue, textPos, Colors.Cream);

            if (Hover)
            {
                if (Tip.IsValid)
                {
                    ToolTip.CreateTooltip(Tip, "", new Vector2(Right, CenterY));
                }
            }
        }
        public bool HandleInput(InputState input, ref float currentValue, float dynamicMaxValue)
        {
            Max = Math.Min(500000f, dynamicMaxValue);
           
            if (!Rect.HitTest(input.CursorPosition) || !input.LeftMouseHeld())
            {
                AbsoluteValue = currentValue;
                return false;
            }
            HandleInput(input);
            currentValue = AbsoluteValue;
            return true;

        }
        public override bool HandleInput(InputState input)
        {
            Hover = Rect.HitTest(input.CursorPosition);

            Rectangle clickCursor = KnobRect;
            clickCursor.X -= KnobRect.Width / 2;

            if (clickCursor.HitTest(input.CursorPosition) && input.LeftMouseHeldDown)
                Dragging = true;

            if (Dragging)
            {
                KnobRect.X = (int)input.CursorPosition.X;
                if (KnobRect.X > SliderRect.X + SliderRect.Width) KnobRect.X = SliderRect.X + SliderRect.Width;
                else if (KnobRect.X < SliderRect.X)               KnobRect.X = SliderRect.X;

                if (input.LeftMouseReleased)
                    Dragging = false;

                RelativeValue = 1f - (SliderRect.X + SliderRect.Width - KnobRect.X) / (float)SliderRect.Width;
            }
            return Dragging;
        }

    }
}