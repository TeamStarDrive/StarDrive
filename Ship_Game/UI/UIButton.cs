using System;
using System.Diagnostics;
using Microsoft.Xna.Framework.Graphics;
using Ship_Game.Audio;
using SDGraphics;
using Rectangle = SDGraphics.Rectangle;

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

        public PressState State = PressState.Default;

        public SubTexture Normal;
        public SubTexture Hover;
        public SubTexture Pressed;

        // Text Colors
        public Color DefaultTextColor = new Color(255, 240, 189);
        public Color HoverTextColor   = new Color(255, 240, 189);
        public Color PressTextColor   = new Color(255, 240, 189);

        // Fallback background colors if Normal texture is null
        public bool DrawBackground = true;
        public Color DefaultColor = new Color(96, 81, 49);
        public Color HoverColor   = new Color(106, 91, 59);
        public Color PressColor   = new Color(86, 71, 39);

        ButtonStyle CurrentStyle = ButtonStyle.Default;
        public ButtonTextAlign TextAlign = ButtonTextAlign.Center;
        
        // Rich text element.
        // Can be accessed directly to create multi-font text labels
        public readonly PrettyText RichText;

        /// <summary>
        /// Optional override Function for text. Called Dynamically every frame.
        /// Completely overrides RichText
        /// </summary>
        public Func<string> DynamicText;

        public LocalizedText Tooltip;
        public string ClickSfx = "echo_affirm";

        // If set TRUE, this button will also capture Right Mouse Clicks
        public bool AcceptRightClicks;

        // If set TRUE, text will be drawn with dark shadow
        public bool TextShadows;

        // If set TRUE, will draw UI element bounds
        public bool DebugDraw;

        public Action<UIButton> OnClick;
        public InputBindings.IBinding Hotkey;

        public override string ToString() => $"{TypeName} '{Text}' visible:{Visible} enabled:{Enabled} state:{State}";
        
        public UIButton(ButtonStyle style, in LocalizedText text)
        {
            Style = style;
            Size = GetInitialSize();
            RichText = new PrettyText(elemToUpdateSize: this, text: text);
        }
        
        public UIButton(ButtonStyle style, Vector2 pos, in LocalizedText text) : base(pos)
        {
            Style = style;
            Size = GetInitialSize();
            RichText = new PrettyText(elemToUpdateSize: this, text: text);
        }

        public UIButton(StyleTextures customStyle, Vector2 size, in LocalizedText text)
        {
            SetStyle(customStyle);
            Size = size;
            RichText = new PrettyText(elemToUpdateSize: this, text: text);
        }

        public ButtonStyle Style
        {
            get => CurrentStyle;
            set => SetStyle(value);
        }

        public LocalizedText Text
        {
            get => RichText.Text;
            set => RichText.SetText(value);
        }

        public Graphics.Font Font
        {
            get => RichText.DefaultFont;
            set => RichText.DefaultFont = value;
        }

        protected virtual void OnButtonClicked()
        {
            OnClick?.Invoke(this);
            if (ClickSfx.NotEmpty())
                GameAudio.PlaySfxAsync(ClickSfx);
        }

        public static SubTexture StyleTexture(ButtonStyle style = ButtonStyle.Default)
        {
            return GetDefaultStyle(style).Normal;
        }

        public override void Draw(SpriteBatch batch, DrawTimes elapsed)
        {
            if (!Visible)
                return;

            if (DynamicText != null)
            {
                string text = DynamicText();
                RichText.SetText(new LocalizedText(text, LocalizationMethod.RawText));
            }

            Rectangle r = Rect;
            SubTexture texture = ButtonTexture();
            if (texture != null)
            {
                batch.Draw(texture, r, Color.White);
            }
            else if (DrawBackground)
            {
                Color c = BackgroundColor();
                batch.FillRectangle(r, c.Alpha(0.75f));
                batch.DrawRectangle(r, c.AddRgb(-0.1f), 2);
            }
            // else: we only draw Text, nothing else

            if (RichText.NotEmpty)
            {
                Vector2 textCursor;
                if (TextAlign == ButtonTextAlign.Center)
                    textCursor.X = (r.X + r.Width / 2) - RichText.Size.X * 0.5f;
                else if (TextAlign == ButtonTextAlign.Left)
                    textCursor.X = r.X + 25f;
                else
                    textCursor.X = r.Right - RichText.Size.X;

                textCursor.Y = r.Y + r.Height / 2 - RichText.Size.Y * 0.5f;
                if (State == PressState.Pressed)
                    textCursor.Y += 1f; // pressed down effect

                Color textColor = Enabled ? TextColor() : Color.Gray;
                RichText.Draw(batch, textCursor, textColor, TextShadows);
            }

            if (DebugDraw)
            {
                batch.DrawRectangle(Rect, Color.Red);
                batch.DrawString(Fonts.Arial11Bold, this.ToString(), Pos, Color.Red);
            }
        }

        bool Released(InputState input) => input.LeftMouseReleased || (AcceptRightClicks && input.RightMouseReleased);
        bool Clicked(InputState input)  => input.LeftMouseClick    || (AcceptRightClicks && input.RightMouseClick);
        bool HeldDown(InputState input) => input.LeftMouseHeldDown || (AcceptRightClicks && input.RightMouseHeldDown);

        public override bool HandleInput(InputState input)
        {
            if (!Visible)
                return false;

            // before any of the early returns, check for hotkey match
            if (Hotkey != null && Hotkey.IsTriggered(input))
            {
                OnButtonClicked();
                return true;
            }

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
                    ToolTip.CreateTooltip(Tooltip, Hotkey?.Hotkey, Pos + Size);
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
