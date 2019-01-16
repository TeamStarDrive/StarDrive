using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace Ship_Game
{
    public class UILabel : UIElementV2
    {
        string LabelText; // Simple Text
        Array<string> Lines; // Multi-Line Text
        Func<string> GetText; // Dynamic Text Binding
        SpriteFont LabelFont;

        public delegate void ClickHandler(UILabel label);
        public event ClickHandler OnClick;

        public Color Color = Color.White;
        public Color Highlight = UIColors.LightBeige;

        public bool DropShadow = false;
        public bool IsMouseOver { get; private set; }

        // Text will be flipped on the X axis:  TEXT x
        // Versus normal text alignment:             x TEXT
        public bool AlignRight = false;

        public string Text
        {
            get => LabelText;
            set
            {
                LabelText = value;
                Size = LabelFont.MeasureString(value);
            }
        }

        public Array<string> MultilineText
        {
            get => Lines;
            set
            {
                Lines = value;
                Size = LabelFont.MeasureLines(Lines);
            }
        }

        public Func<string> DynamicText
        {
            set
            {
                GetText = value;
                Size = LabelFont.MeasureString(GetText());
            }
        }

        public SpriteFont Font
        {
            get => LabelFont;
            set
            {
                LabelFont = value;
                Size = value.MeasureString(LabelText);
            }
        }


        public UILabel(UIElementV2 parent, Vector2 pos, string text, SpriteFont font) : base(parent, pos, font.MeasureString(text))
        {
            LabelText = text;
            LabelFont = font;
        }

        public UILabel(UIElementV2 parent, Vector2 pos, int titleId, SpriteFont font) : this(parent, pos, Localizer.Token(titleId), font)
        {
        }
        public UILabel(UIElementV2 parent, Vector2 pos, int titleId, SpriteFont font, Color color) : this(parent, pos, Localizer.Token(titleId), font)
        {
            Color = color;
        }
        public UILabel(UIElementV2 parent, Vector2 pos, string text, SpriteFont font, Color color) : this(parent, pos, text, font)
        {
            Color = color;
        }
        public UILabel(UIElementV2 parent, Vector2 pos, int titleId) : this(parent, pos, Localizer.Token(titleId), Fonts.Arial12Bold)
        {
        }
        public UILabel(UIElementV2 parent, Vector2 pos, string text) : this(parent, pos, text, Fonts.Arial12Bold)
        {
        }
        public UILabel(UIElementV2 parent, Vector2 pos, int titleId, Color color) : this(parent, pos, Localizer.Token(titleId), Fonts.Arial12Bold)
        {
            Color = color;
        }
        public UILabel(UIElementV2 parent, Vector2 pos, string text, Color color) : this(parent, pos, text, Fonts.Arial12Bold)
        {
            Color = color;
        }

        void DrawLine(SpriteBatch batch, string text, Vector2 pos, Color color)
        {
            if (AlignRight) pos.X -= Size.X;
            if (DropShadow)
                HelperFunctions.DrawDropShadowText(batch, text, pos, LabelFont, color);
            else
                batch.DrawString(LabelFont, text, pos, color);
        }

        public override void Draw(SpriteBatch batch)
        {
            Color color = IsMouseOver ? Highlight : Color;
            if (Lines != null && Lines.NotEmpty)
            {
                Vector2 cursor = Pos;
                for (int i = 0; i < Lines.Count; ++i)
                {
                    string line = Lines[i];
                    DrawLine(batch, line, cursor, color);
                    cursor.Y += LabelFont.LineSpacing + 2;
                }
            }
            else if (GetText != null)
            {
                DrawLine(batch, GetText(), Pos, color);
            }
            else if (LabelText.NotEmpty())
            {
                DrawLine(batch, LabelText, Pos, color);
            }
        }

        public override bool HandleInput(InputState input)
        {
            if (HitTest(input.CursorPosition))
            {
                if (OnClick != null)
                {
                    if (!IsMouseOver)
                        GameAudio.PlaySfxAsync("sd_ui_mouseover");

                    IsMouseOver = true;

                    if (input.InGameSelect)
                    {
                        GameAudio.PlaySfxAsync("blip_click");
                        OnClick(this);
                    }
                }
                else IsMouseOver = false;
                return true;
            }
            IsMouseOver = false;
            return false;
        }
    }
}
