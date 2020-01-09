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
        public ButtonTextAlign TextAlign = ButtonTextAlign.Center;
        public LocalizedText Text;
        public ToolTipText Tooltip;
        public string ClickSfx = "echo_affirm";

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
            StyleTextures styling = GetStyle(Style);
            switch (State)
            {
                default:                 return styling.Normal;
                case PressState.Hover:   return styling.Hover;
                case PressState.Pressed: return styling.Pressed;
            }
        }

        Color TextColor()
        {
            StyleTextures styling = GetStyle(Style);
            switch (State)
            {
                default:                 return styling.DefaultColor;
                case PressState.Hover:   return styling.HoverColor;
                case PressState.Pressed: return styling.PressColor;
            }
        }

        public override void Draw(SpriteBatch batch)
        {
            if (!Visible)
                return;

            Rectangle r = Rect;
            batch.Draw(ButtonTexture(), r, Color.White);

            if (Text.NotEmpty)
            {
                SpriteFont font = Fonts.Arial12Bold;
                string text = Text.Text;

                Vector2 textCursor;
                if (TextAlign == ButtonTextAlign.Center)
                    textCursor.X = r.X + r.Width / 2 - font.MeasureString(text).X / 2f;
                else
                    textCursor.X = r.X + 25f;

                textCursor.Y = r.Y + r.Height / 2 - font.LineSpacing / 2;
                if (State == PressState.Pressed)
                    textCursor.Y += 1f; // pressed down effect

                batch.DrawString(font, text, textCursor, Enabled ? TextColor() : Color.Gray);
            }
        }

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

            if (State == PressState.Pressed && input.LeftMouseReleased)
            {
                State = PressState.Hover;
                OnButtonClicked();
                if (ClickSfx.NotEmpty())
                    GameAudio.PlaySfxAsync(ClickSfx);
                return true;
            }

            if (State != PressState.Pressed && input.LeftMouseClick)
            {
                State = PressState.Pressed;
                return true;
            }
            if (State == PressState.Pressed && input.LeftMouseHeldDown)
            {
                State = PressState.Pressed;
                return true;
            }

            // only trigger tooltip if we were hovering last frame as well as this one
            if (State == PressState.Hover)
            {
                if (Tooltip.IsValid)
                {
                    ToolTip.CreateTooltip(Tooltip);
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
