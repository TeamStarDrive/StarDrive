using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace Ship_Game
{
    public enum ButtonStyle
    {
        Default,    // empiretopbar_btn_168px
        Small,      // empiretopbar_btn_68px
        Low80,      // empiretopbar_low_btn_80px
        Low100,     // empiretopbar_low_btn_100px
        Medium,     // empiretopbar_btn_132px
        MediumMenu, // empiretopbar_btn_132px_menu
        BigDip,     // empiretopbar_btn_168px_dip
    }

    // Refactored by RedFox
    public sealed class UIButton : UIElementV2
    {
        public enum PressState
        {
            Default, Hover, Pressed
        }
        public PressState State  = PressState.Default;
        public ButtonStyle Style = ButtonStyle.Default;
        public string Text     = "";
        public string Launches = "";
        public readonly Color DefaultColor = new Color(255, 240, 189);
        public readonly Color HoverColor   = new Color(255, 240, 189);
        public readonly Color PressColor   = new Color(255, 240, 189);
        public int ToolTip;

        public bool Visible = true;
        public bool Enabled = true; // if false, button will be visible, but gray and not interactive

        public string ClickSfx = "echo_affirm";

        public delegate void ClickHandler(UIButton button);
        public event ClickHandler OnClick;

        public UIButton(Vector2 size)
        {
            Size = size;
        }

        public UIButton(float x = 0f, float y = 0f, string launches = "", string text = "")
        {
            InitializeStyles();
            Text = text;
            Launches = launches;
            Pos  = new Vector2(x, y);
            Size = ButtonTexture().Size();
        }

        public UIButton(ButtonStyle style, float x = 0f, float y = 0f, string launches = "", string text = "")
        {
            InitializeStyles();
            Style = style;
            Text = text;
            Launches = launches;
            Pos  = new Vector2(x, y);
            Size = ButtonTexture().Size();
        }


        private class StyleTextures
        {
            public readonly Texture2D Normal;
            public readonly Texture2D Hover;
            public readonly Texture2D Pressed;
            public StyleTextures(string normal)
            {
                Normal  = ResourceManager.Texture(normal);
                Hover   = ResourceManager.Texture(normal+"_hover");
                Pressed = ResourceManager.Texture(normal+"_pressed");
            }
        }

        private static StyleTextures[] Styling;
        
        private static void InitializeStyles()
        {
            if (Styling != null)
                return;
            Styling = new []
            {
                new StyleTextures("EmpireTopBar/empiretopbar_btn_168px"),
                new StyleTextures("EmpireTopBar/empiretopbar_btn_68px"),
                new StyleTextures("EmpireTopBar/empiretopbar_low_btn_80px"),
                new StyleTextures("EmpireTopBar/empiretopbar_low_btn_100px"),
                new StyleTextures("EmpireTopBar/empiretopbar_btn_132px"),
                new StyleTextures("EmpireTopBar/empiretopbar_btn_132px_menu"),
                new StyleTextures("EmpireTopBar/empiretopbar_btn_168px_dip"),
            };
        }

        private Texture2D ButtonTexture()
        {
            StyleTextures styling = Styling[(int)Style];
            switch (State)
            {
                default:                 return styling.Normal;
                case PressState.Hover:   return styling.Hover;
                case PressState.Pressed: return styling.Pressed;
            }
        }

        private Color TextColor()
        {
            switch (State)
            {
                default:                 return DefaultColor;
                case PressState.Hover:   return HoverColor;
                case PressState.Pressed: return PressColor;
            }
        }

        private void DrawButtonText(SpriteBatch spriteBatch, Rectangle r)
        {
            SpriteFont font = Fonts.Arial12Bold;

            Vector2 textCursor;
            textCursor.X = r.X + r.Width  / 2 - font.MeasureString(Text).X / 2f;
            textCursor.Y = r.Y + r.Height / 2 - font.LineSpacing / 2;
            if (State == PressState.Pressed)
                textCursor.Y += 1f; // pressed down effect

            spriteBatch.DrawString(font, Text, textCursor, Enabled ? TextColor() : Color.Gray);
        }

        public void Draw(SpriteBatch spriteBatch, Rectangle r)
        {
            if (!Visible)
                return;
            spriteBatch.Draw(ButtonTexture(), r, Color.White);
            DrawButtonText(spriteBatch, r);
        }

        public override void Draw(SpriteBatch spriteBatch)
        {
            if (!Visible)
                return;
            spriteBatch.Draw(ButtonTexture(), Rect, Color.White);
            DrawButtonText(spriteBatch, Rect);
        }

        public override bool HandleInput(InputState input)
        {
            if (!Visible)
                return false;

            if (!Rect.HitTest(input.MouseScreenPos))
            {
                State = PressState.Default;
                return false;
            }

            if (State != PressState.Hover && State != PressState.Pressed)
                GameAudio.PlaySfxAsync("mouse_over4");

            if (State == PressState.Pressed && input.LeftMouseReleased)
            {
                State = PressState.Hover;
                OnClick?.Invoke(this);
                if (ClickSfx.NotEmpty())
                    GameAudio.PlaySfxAsync(ClickSfx);
                return true;
            }

            if (input.LeftMouseHeldDown)
            {
                State = PressState.Pressed;
                return true;
            }

            State = PressState.Hover;
            return false;
        }
    }
}
