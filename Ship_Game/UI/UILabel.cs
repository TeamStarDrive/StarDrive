using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Ship_Game.Audio;

namespace Ship_Game
{
    public class UILabel : UIElementV2, IColorElement
    {
        string LabelText; // Simple Text
        Array<string> Lines; // Multi-Line Text
        Func<UILabel, string> GetText; // Dynamic Text Binding
        SpriteFont LabelFont;

        public Action<UILabel> OnClick;

        public Color Color { get; set; } = Color.White;
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

        public Func<UILabel, string> DynamicText
        {
            set
            {
                GetText = value;
                Size = LabelFont.MeasureString(GetText(this));
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

        public override string ToString() => $"{TypeName} {ElementDescr} Text=\"{Text}\"";
        
        public UILabel(Vector2 pos, LocalizedText text) : this(pos, text, Fonts.Arial12Bold)
        {
        }
        public UILabel(Vector2 pos, LocalizedText text, Color color) : this(pos, text, Fonts.Arial12Bold)
        {
            Color = color;
        }
        public UILabel(Vector2 pos, LocalizedText text, SpriteFont font) : base(pos, font.MeasureString(text.Text))
        {
            LabelText = text.Text;
            LabelFont = font;
        }
        public UILabel(Vector2 pos, LocalizedText text, SpriteFont font, Color color) : this(pos, text, font)
        {
            Color = color;
        }

        /////////////////////////////////////////////////////////////////////////////////////////////////

        public UILabel(LocalizedText text) : this(text, Fonts.Arial12Bold)
        {
        }
        public UILabel(LocalizedText text, Color color) : this(text, Fonts.Arial12Bold)
        {
            Color = color;
        }
        public UILabel(LocalizedText text, SpriteFont font)
        {
            LabelFont = font;
            Text = text.Text;
        }

        public UILabel(Func<UILabel, string> getText) : this(getText, Fonts.Arial12Bold)
        {
        }
        public UILabel(Func<UILabel, string> getText, SpriteFont font)
        {
            LabelFont = font;
            DynamicText = getText;
        }
        
        public UILabel(Func<UILabel, string> getText, Action<UILabel> onClick) : this(getText, Fonts.Arial12Bold)
        {
            OnClick = onClick;
        }

        /////////////////////////////////////////////////////////////////////////////////////////////////

        void DrawLine(SpriteBatch batch, string text, Vector2 pos, Color color)
        {
            if (AlignRight) pos.X -= Size.X;
            if (DropShadow)
                batch.DrawDropShadowText(text, pos, LabelFont, color);
            else
                batch.DrawString(LabelFont, text, pos, color);
        }

        Color CurrentColor => IsMouseOver ? Highlight : Color;

        public override void Draw(SpriteBatch batch)
        {
            if (Lines != null && Lines.NotEmpty)
            {
                Color color = CurrentColor;
                Vector2 cursor = Pos;
                for (int i = 0; i < Lines.Count; ++i)
                {
                    string line = Lines[i];
                    if (line.NotEmpty())
                        DrawLine(batch, line, cursor, color);
                    cursor.Y += LabelFont.LineSpacing + 2;
                }
            }
            else if (GetText != null)
            {
                string text = GetText(this); // GetText is allowed to modify [this]
                if (text.NotEmpty())
                    DrawLine(batch, text, Pos, CurrentColor);
            }
            else if (LabelText.NotEmpty())
            {
                DrawLine(batch, LabelText, Pos, CurrentColor);
            }
        }

        public override bool HandleInput(InputState input)
        {
            if (HitTest(input.CursorPosition))
            {
                if (OnClick != null)
                {
                    if (!IsMouseOver)
                        GameAudio.ButtonMouseOver();

                    IsMouseOver = true;

                    if (input.InGameSelect)
                    {
                        GameAudio.BlipClick();
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
