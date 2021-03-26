using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Ship_Game.Audio;

namespace Ship_Game
{
    public enum TextAlign
    {
        // normal text alignment: TopLeft
        //    x
        //      TEXT
        Default,

        // Text will be flipped on the X axis:
        //       x
        // TEXT
        Right,

        // Text will be drawn to horizontal center of label rect
        //  TE + XT
        //     x
        HorizontalCenter,

        // Text will be drawn to vertical center of label rect
        //   TE x XT
        VerticalCenter,

        // Text will be drawn in the center of its label rect
        // equivalent to HorizontalCenter + VerticalCenter
        Center,
    }

    public class UILabel : UIElementV2, IColorElement
    {
        LocalizedText LabelText; // Localized Simple Text
        Array<string> Lines; // Multi-Line Text
        Func<UILabel, string> GetText; // Dynamic Text Binding
        SpriteFont LabelFont;
        string CachedText;
        Vector2 ActualLineSize;

        public Action<UILabel> OnClick;

        public Color Color { get; set; } = Color.White;

        Color TextHoverColor = UIColors.LightBeige;
        bool EnableHighlights;

        // If set to a custom value, will display
        // text with a different highlight when hovered
        public Color Highlight
        {
            get => TextHoverColor;
            set
            {
                TextHoverColor = value;
                EnableHighlights = (value != Color.TransparentBlack);
            }
        }

        public bool DropShadow = false;
        public bool IsMouseOver { get; private set; }

        // Text Alignment will change the alignment axis along which the text is drawn
        public TextAlign TextAlign = TextAlign.Default;

        public LocalizedText Text
        {
            get => LabelText;
            set
            {
                if (LabelText != value)
                {
                    LabelText = value;
                    UpdateSizeFromText(LabelText.Text);
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
                ActualLineSize = new Vector2(Size.X, LabelFont.LineSpacing + 2); // @hack
                RequiresLayout = true;
            }
        }

        // Allows to set a dynamic text callback, which will determine
        // the contents of the label
        public Func<UILabel, string> DynamicText
        {
            set
            {
                GetText = value;
                if (value != null)
                {
                    UpdateSizeFromText(GetText(this));
                }
            }
        }

        // Allows to override the default font of the UI Label after creation
        public SpriteFont Font
        {
            get => LabelFont;
            set
            {
                if (LabelFont != value)
                {
                    LabelFont = value;
                    UpdateSizeFromText(LabelText.Text);
                }
            }
        }

        void UpdateSizeFromText(string text)
        {
            // @todo: Size is not updated when language changes
            ActualLineSize = LabelFont.MeasureString(text);
            // @todo Should we always overwrite the size?
            Size = ActualLineSize;
            RequiresLayout = true;
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
            UpdateSizeFromText(text.Text);
        }
        public UILabel(Vector2 pos, in LocalizedText text, SpriteFont font, Color color)
            : this(pos, text, font)
        {
            Color = color;
        }
        public UILabel(in Rectangle rect, in LocalizedText text, SpriteFont font, Color color)
        {
            LabelFont = font;
            LabelText = text;
            UpdateSizeFromText(text.Text);
            Color = color;
            Rect = rect;
        }

        /////////////////////////////////////////////////////////////////////////////////////////////////

        public UILabel(in LocalizedText text) : this(text, Fonts.Arial12Bold)
        {
        }
        public UILabel(in LocalizedText text, Color color) : this(text, Fonts.Arial12Bold, color)
        {
        }
        public UILabel(in LocalizedText text, SpriteFont font) : this(text, font, Color.White)
        {
        }
        public UILabel(in LocalizedText text, SpriteFont font, Color color)
        {
            LabelFont = font;
            Text = text; // NOTE: triggers UpdateSizeFromText 
            Color = color;
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
            switch (TextAlign)
            {
                // NOTE: Text pos MUST be rounded to pixel boundaries
                default:
                case TextAlign.Default:
                    pos.X = (int)Math.Round(pos.X);
                    pos.Y = (int)Math.Round(pos.Y);
                    break;
                case TextAlign.Right:
                    pos.X = (int)Math.Round(pos.X - Size.X);
                    pos.Y = (int)Math.Round(pos.Y);
                    break;
                case TextAlign.HorizontalCenter:
                    pos.X = (int)Math.Round((pos.X + Size.X*0.5f) - ActualLineSize.X*0.5f);
                    pos.Y = (int)Math.Round(pos.Y);
                    break;
                case TextAlign.VerticalCenter:
                    pos.X = (int)Math.Round(pos.X);
                    pos.Y = (int)Math.Round((pos.Y + Size.Y*0.5f) - ActualLineSize.Y*0.5f);
                    break;
                case TextAlign.Center:
                    pos.X = (int)Math.Round((pos.X + Size.X*0.5f) - ActualLineSize.X*0.5f);
                    pos.Y = (int)Math.Round((pos.Y + Size.Y*0.5f) - ActualLineSize.Y*0.5f);
                    break;
            }

            if (DropShadow)
                batch.DrawDropShadowText(text, pos, LabelFont, color);
            else
                batch.DrawString(LabelFont, text, pos, color);
        }

        Color CurrentColor => IsMouseOver ? Highlight : Color;

        public override void Draw(SpriteBatch batch, DrawTimes elapsed)
        {
            if (Lines != null && Lines.NotEmpty)
            {
                Color color = CurrentColor;
                Vector2 cursor = Pos;
                float lineHeight = LabelFont.LineSpacing + 2;
                for (int i = 0; i < Lines.Count; ++i)
                {
                    string line = Lines[i];
                    if (line.NotEmpty())
                        DrawTextLine(batch, line, cursor, color);
                    cursor.Y += lineHeight;
                }
            }
            else if (GetText != null)
            {
                string text = GetText(this); // GetText is allowed to modify [this]
                RequiresLayout |= CachedText != text;
                CachedText = text;
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
            bool hit = HitTest(input.CursorPosition);
            if (hit && OnClick != null)
            {
                if (!IsMouseOver)
                    GameAudio.ButtonMouseOver();

                IsMouseOver = true;

                if (input.InGameSelect)
                {
                    GameAudio.BlipClick();
                    OnClick(this);
                }
                return true;
            }
            
            IsMouseOver = hit && EnableHighlights;
            return false;
        }
    }
}
