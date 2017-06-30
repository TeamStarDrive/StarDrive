using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace Ship_Game
{
    public class UILabel : UIElementV2
    {
        private string LabelText;
        private SpriteFont LabelFont;

        public delegate void ClickHandler(UILabel button);
        public event ClickHandler OnClick;

        public Color Color = Color.White;
        public Color Highlight = UIColors.LightBeige;

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

        public SpriteFont Font
        {
            get => LabelFont;
            set
            {
                LabelFont = value;
                Size = value.MeasureString(LabelText);
            }
        }


        public UILabel(Vector2 pos, string text, SpriteFont font) : base(pos, font.MeasureString(text))
        {
            LabelText = text;
            LabelFont = font;
        }
        public UILabel(Vector2 pos, int localization, SpriteFont font) : this(pos, Localizer.Token(localization), font)
        {
        }
        public UILabel(Vector2 pos, int localization) : this(pos, Localizer.Token(localization), Fonts.Arial12Bold)
        {
        }
        public UILabel(Vector2 pos, string text) : this(pos, text, Fonts.Arial12Bold)
        {
        }



        public override void Draw(SpriteBatch spriteBatch)
        {
            if (LabelText.NotEmpty())
            {
                spriteBatch.DrawString(LabelFont, LabelText, Pos, IsMouseOver ? Highlight : Color);
            }
        }

        public override bool HandleInput(InputState input)
        {
            if (HitTest(input.MouseScreenPos))
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
