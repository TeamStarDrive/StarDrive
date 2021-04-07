using System;
using System.Diagnostics;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Ship_Game.Audio;

namespace Ship_Game
{
    public enum ButtonTextAlign
    {
        Center,
        Left,
        Right,
    }

    // Refactored by RedFox
    public partial class UIButton : UIElementV2
    {
        public enum PressState
        {
            Default, Hover, Pressed
        }
        public PressState  State = PressState.Default;
        public ButtonStyle Style = ButtonStyle.Default;
        public StyleTextures CustomStyle;
        public ButtonTextAlign TextAlign = ButtonTextAlign.Center;

        public LocalizedText Text;

        /// <summary>
        /// Optional override Function for text. Called Dynamically every frame.
        /// </summary>
        public Func<string> DynamicText;

        public LocalizedText Tooltip;
        public string HotKey;
        public string ClickSfx = "echo_affirm";

        public SpriteFont Font = Fonts.Arial12Bold;

        // If set TRUE, this button will also capture Right Mouse Clicks
        public bool AcceptRightClicks;

        // If set TRUE, text will be drawn with dark shadow
        public bool TextShadows;

        public Action<UIButton> OnClick;

        public override string ToString() => $"{TypeName} '{Text}' visible:{Visible} enabled:{Enabled} state:{State}";
        
        public UIButton(ButtonStyle style)
        {
            Style = style;
        }

        public UIButton(in LocalizedText text)
        {
            Text = text;
            Size = ButtonTexture().SizeF;
        }
        
        public UIButton(ButtonStyle style, in LocalizedText text)
        {
            Style = style;
            Text = text;
            Size = ButtonTexture().SizeF;
        }

        public UIButton(Vector2 pos, in LocalizedText text) : base(pos)
        {
            Text = text;
            Size = ButtonTexture().SizeF;
        }

        public UIButton(ButtonStyle style, Vector2 pos, in LocalizedText text) : base(pos)
        {
            Style = style;
            Text  = text;
            Size  = ButtonTexture().SizeF;
        }

        public UIButton(ButtonStyle style, in Rectangle rect) : base(rect)
        {
            Style = style;
        }

        public UIButton(StyleTextures customStyle, Vector2 size, in LocalizedText text)
        {
            CustomStyle = customStyle;
            Text = text;
            Size = size;
        }

        protected virtual void OnButtonClicked()
        {
            OnClick?.Invoke(this);
        }

        public static SubTexture StyleTexture(ButtonStyle style = ButtonStyle.Default)
        {
            return GetStyle(style).Normal;
        }

        SubTexture ButtonTexture()
        {
            StyleTextures styling = CustomStyle ?? GetStyle(Style);
            switch (State)
            {
                default:                 return styling.Normal;
                case PressState.Hover:   return styling.Hover;
                case PressState.Pressed: return styling.Pressed;
            }
        }

        Color BackgroundColor()
        {
            StyleTextures styling = CustomStyle ?? GetStyle(Style);
            switch (State)
            {
                default:                 return styling.DefaultColor;
                case PressState.Hover:   return styling.HoverColor;
                case PressState.Pressed: return styling.PressColor;
            }
        }

        Color TextColor()
        {
            StyleTextures styling = CustomStyle ?? GetStyle(Style);
            switch (State)
            {
                default:                 return styling.DefaultTextColor;
                case PressState.Hover:   return styling.HoverTextColor;
                case PressState.Pressed: return styling.PressTextColor;
            }
        }

        public override void Draw(SpriteBatch batch, DrawTimes elapsed)
        {
            if (!Visible)
                return;

            string text = null;
            if (DynamicText != null)
            {
                text = DynamicText();
                Text = new LocalizedText(text, LocalizationMethod.RawText);
            }
            else if (Text.NotEmpty)
            {
                text = Text.Text;
            }

            Rectangle r = Rect;
            SubTexture texture = ButtonTexture();
            if (texture != null)
            {
                batch.Draw(texture, r, Color.White);
            }
            else
            {
                Color c = BackgroundColor();
                batch.FillRectangle(r, c.Alpha(0.75f));
                batch.DrawRectangle(r, c.AddRgb(-0.1f), 2);
            }

            if (text != null)
            {
                SpriteFont font = Font;
                Vector2 textCursor;
                if (TextAlign == ButtonTextAlign.Center)
                    textCursor.X = r.X + r.Width / 2 - font.MeasureString(text).X / 2f;
                else if (TextAlign == ButtonTextAlign.Left)
                    textCursor.X = r.X + 25f;
                else
                    textCursor.X = r.Right - font.MeasureString(text).X;

                textCursor.Y = r.Y + r.Height / 2 - font.LineSpacing / 2;
                if (State == PressState.Pressed)
                    textCursor.Y += 1f; // pressed down effect

                Color textColor = Enabled ? TextColor() : Color.Gray;
                if (TextShadows)
                    batch.DrawDropShadowText(text, textCursor, font, textColor);
                else
                    batch.DrawString(font, text, textCursor, textColor);
            }
        }

        bool Released(InputState input) => input.LeftMouseReleased || (AcceptRightClicks && input.RightMouseReleased);
        bool Clicked(InputState input)  => input.LeftMouseClick    || (AcceptRightClicks && input.RightMouseClick);
        bool HeldDown(InputState input) => input.LeftMouseHeldDown || (AcceptRightClicks && input.RightMouseHeldDown);

        public override bool HandleInput(InputState input)
        {
            if (!Visible)
                return false;

            if (!Rect.HitTest(input.CursorPosition)) // not hovering?
            {
                State = PressState.Default;
                return false;
            }

            // we are now hovering

            // not hovering last frame? trigger mouseover sfx
            if (State != PressState.Hover && State != PressState.Pressed)
            {
                GameAudio.MouseOver();
            }

            if (State == PressState.Pressed && Released(input))
            {
                State = PressState.Hover;
                OnButtonClicked();
                if (ClickSfx.NotEmpty())
                    GameAudio.PlaySfxAsync(ClickSfx);
                return true;
            }

            if (State != PressState.Pressed && Clicked(input))
            {
                State = PressState.Pressed;
                return true;
            }
            if (State == PressState.Pressed && HeldDown(input))
            {
                State = PressState.Pressed;
                return true;
            }

            // only trigger tooltip if we were hovering last frame as well as this one
            if (State == PressState.Hover)
            {
                if (Tooltip.IsValid)
                {
                    ToolTip.CreateTooltip(Tooltip, HotKey, Pos + Size);
                }
            }

            State = PressState.Hover;
            // @note This should return true to capture the hover input,
            //       however most UI code doesn't use UIElementV2 system yet,
            //       so returning true would falsely trigger a lot of old style buttons
            //       Semantic differences:
            //         old system: true means click/event happened
            //         UIElementV2: true means input was handled/captured and should not propagate to other elements
            return false;
        }
    }
}
