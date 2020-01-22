using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Ship_Game.Audio;

namespace Ship_Game
{
    public enum TextAlign
    {
        // normal text alignment:
        //    x
        //      TEXT
        Default,

        // Text will be flipped on the X axis:
        //       x
        // TEXT
        Right,

        // Text will be drawn to the center of pos:
        //  TE x XT
        Center,
    }

    public class UILabel : UIElementV2, IColorElement
    {
        LocalizedText LabelText; // Localized Simple Text
        Array<string> Lines; // Multi-Line Text
        Func<UILabel, string> GetText; // Dynamic Text Binding
        SpriteFont LabelFont;

        public Action<UILabel> OnClick;

        public Color Color { get; set; } = Color.White;
        public Color Highlight = UIColors.LightBeige;

        public bool DropShadow = false;
        public bool IsMouseOver { get; private set; }

        // Text Alignment will change the alignment axis along which the text is drawn
        public TextAlign Align;

        public LocalizedText Text
        {
            get => LabelText;
            set
            {
                if (LabelText != value)
                {
                    LabelText = value;
                    Size = LabelFont.MeasureString(LabelText.Text); // @todo: Size is not updated when language changes
                }
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
                Size = LabelFont.MeasureString(GetText(this)); // @todo: Size is not updated when language changes
            }
        }

        public SpriteFont Font
        {
            get => LabelFont;
            set
            {
                if (LabelFont != value)
                {
                    LabelFont = value;
                    Size = value.MeasureString(LabelText.Text); // @todo: Size is not updated when language changes
                }
            }
        }

        public override string ToString() => $"{TypeName} {ElementDescr} Text={Text}";
        
        public UILabel(SpriteFont font)
        {
            LabelFont = font;
            Size = new Vector2(font.LineSpacing); // give it a mock size to ease debugging
        }
        public UILabel(float x, float y, in LocalizedText text) : this(new Vector2(x,y), text, Fonts.Arial12Bold)
        {
        }
        public UILabel(Vector2 pos, in LocalizedText text) : this(pos, text, Fonts.Arial12Bold)
        {
        }
        public UILabel(Vector2 pos, in LocalizedText text, Color color) : this(pos, text, Fonts.Arial12Bold)
        {
            Color = color;
        }
        public UILabel(Vector2 pos, in LocalizedText text, SpriteFont font) : base(pos)
        {
            LabelFont = font;
            LabelText = text;
            Size = font.MeasureString(text.Text); // @todo: Size is not updated when language changes
        }
        public UILabel(Vector2 pos, in LocalizedText text, SpriteFont font, Color color) : this(pos, text, font)
        {
            Color = color;
        }

        /////////////////////////////////////////////////////////////////////////////////////////////////

        public UILabel(in LocalizedText text) : this(text, Fonts.Arial12Bold)
        {
        }
        public UILabel(in LocalizedText text, Color color) : this(text, Fonts.Arial12Bold)
        {
            Color = color;
        }
        public UILabel(in LocalizedText text, SpriteFont font)
        {
            LabelFont = font;
            Text = text;
        }

        public UILabel(Func<UILabel, string> getText) : this(getText, Fonts.Arial12Bold)
        {
        }
        public UILabel(Func<UILabel, string> getText, Action<UILabel> onClick) : this(getText, Fonts.Arial12Bold)
        {
            OnClick = onClick;
        }
        public UILabel(Func<UILabel, string> getText, SpriteFont font)
        {
            LabelFont = font;
            DynamicText = getText;
        }
        
        /////////////////////////////////////////////////////////////////////////////////////////////////

        void DrawTextLine(SpriteBatch batch, string text, Vector2 pos, Color color)
        {
            switch (Align)
            {
                default:
                case TextAlign.Default:
                    pos.X = (int)Math.Round(pos.X);
                    pos.Y = (int)Math.Round(pos.Y);
                    break;
                case TextAlign.Right:
                    pos.X = (int)Math.Round(pos.X - Size.X);
                    pos.Y = (int)Math.Round(pos.Y);
                    break;
                case TextAlign.Center:
                    pos.X = (int)Math.Round(pos.X - Size.X*0.5f); // NOTE: Text pos MUST be rounded to pixel boundaries
                    pos.Y = (int)Math.Round(pos.Y - Size.Y*0.5f);
                    break;
            }

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
                        DrawTextLine(batch, line, cursor, color);
                    cursor.Y += LabelFont.LineSpacing + 2;
                }
            }
            else if (GetText != null)
            {
                string text = GetText(this); // GetText is allowed to modify [this]
                if (text.NotEmpty())
                    DrawTextLine(batch, text, Pos, CurrentColor);
            }
            else if (LabelText.NotEmpty)
            {
                DrawTextLine(batch, LabelText.Text, Pos, CurrentColor);
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
