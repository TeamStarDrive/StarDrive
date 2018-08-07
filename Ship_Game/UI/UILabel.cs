using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace Ship_Game
{
    public class UILabel : UIElementV2
    {
        private string LabelText;
        private Array<string> Lines; // multiline
        private SpriteFont LabelFont;

        public delegate void ClickHandler(UILabel label);
        public event ClickHandler OnClick;

        public Color Color = Color.White;
        public Color Highlight = UIColors.LightBeige;

        public bool DropShadow = false;
        public bool IsMouseOver { get; private set; }

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

        private void DrawLine(SpriteBatch batch, string text, Vector2 pos, Color color)
        {
            if (DropShadow)
                HelperFunctions.DrawDropShadowText(batch, text, pos, LabelFont, color);
            else
                batch.DrawString(LabelFont, text, pos, color);
        }

        public override void Draw(SpriteBatch batch)
        {
            if (LabelText.IsEmpty() && (Lines == null || Lines.IsEmpty))
                return;

            Color color = IsMouseOver ? Highlight : Color;
            if (Lines.NotEmpty)
            {
                Vector2 cursor = Pos;
                for (int i = 0; i < Lines.Count; ++i)
                {
                    string line = Lines[i];
                    DrawLine(batch, line, cursor, color);
                    cursor.Y += LabelFont.LineSpacing + 2;
                }
            }
            else
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
