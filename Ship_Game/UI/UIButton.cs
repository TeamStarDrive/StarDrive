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
        Close,      // NewUI/Close_Normal
    }

    // Refactored by RedFox
    public class UIButton : UIElementV2
    {
        public enum PressState
        {
            Default, Hover, Pressed
        }
        public PressState  State = PressState.Default;
        public ButtonStyle Style = ButtonStyle.Default;
        public string Text;
        public string Launches;
        public readonly Color DefaultColor = new Color(255, 240, 189);
        public readonly Color HoverColor   = new Color(255, 240, 189);
        public readonly Color PressColor   = new Color(255, 240, 189);
        public string Tooltip;

        public string ClickSfx = "echo_affirm";

        public delegate void ClickHandler(UIButton button);
        public event ClickHandler OnClick;

        
        public UIButton(UIElementV2 parent, Vector2 pos, string launches = "", string text = "") : base(parent, pos)
        {
            InitializeStyles();
            Text     = text;
            Launches = launches;
            Size     = ButtonTexture().Size();
        }

        public UIButton(UIElementV2 parent, Vector2 pos, string text = "") : base(parent, pos)
        {
            InitializeStyles();
            Text     = text;
            Size     = ButtonTexture().Size();
        }

        public UIButton(UIElementV2 parent, ButtonStyle style, Vector2 pos, string launches = "", string text = "") : base(parent, pos)
        {
            InitializeStyles();
            Style    = style;
            Text     = text;
            Launches = launches;
            Size     = ButtonTexture().Size();
        }

        public UIButton(UIElementV2 parent, float x = 0f, float y = 0f, string launches = "", string text = "")
            : this(parent, new Vector2(x, y), launches, text)
        {
        }

        public UIButton(UIElementV2 parent, ButtonStyle style, float x = 0f, float y = 0f, string launches = "", string text = "")
            : this(parent, style, new Vector2(x, y), launches, text)
        {
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
            public StyleTextures(string normal, string hover)
            {
                Normal  = ResourceManager.Texture(normal);
                Hover   = ResourceManager.Texture(hover);
                Pressed = Hover;
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
                new StyleTextures("NewUI/Close_Normal", "NewUI/Close_Hover"),
            };
        }

        public static Vector2 StyleSize(ButtonStyle style = ButtonStyle.Default)
        {
            InitializeStyles();
            return Styling[(int)style].Normal.Size();
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

        public void Draw(SpriteBatch batch, Rectangle r)
        {
            if (!Visible)
                return;

            batch.Draw(ButtonTexture(), r, Color.White);

            SpriteFont font = Fonts.Arial12Bold;

            Vector2 textCursor;
            textCursor.X = r.X + r.Width  / 2 - font.MeasureString(Text).X / 2f;
            textCursor.Y = r.Y + r.Height / 2 - font.LineSpacing / 2;
            if (State == PressState.Pressed)
                textCursor.Y += 1f; // pressed down effect

            batch.DrawString(font, Text, textCursor, Enabled ? TextColor() : Color.Gray);

            if (State == PressState.Hover && Tooltip.NotEmpty())
            {
                ToolTip.CreateTooltip(Tooltip);
            }
        }

        public override void Draw(SpriteBatch batch)
        {
            Draw(batch, Rect);
        }

        public override bool HandleInput(InputState input)
        {
            if (!Visible)
                return false;

            if (!Rect.HitTest(input.CursorPosition))
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
            // @note This should return false to capture the hover input,
            //       however most UI code doesn't use UIElementV2 system yet,
            //       so returning true would falsely trigger a lot of old style buttons
            //       Semantic differences:
            //         old system: true means click/event happened
            //         UIElementV2: true means input was handled/captured and should not propagate to other elements
            return false;
        }

        public override string ToString() => $"Button '{Launches}' visible:{Visible} enabled:{Enabled} state:{State}";
    }
}
