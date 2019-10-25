using System;
using System.Diagnostics;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Ship_Game.Audio;

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
        Military,   // empiretopbar_btn_168px_military
        Close,      // NewUI/Close_Normal
        ResearchQueueUp, // "ResearchMenu/button_queue_up"
        ResearchQueueDown, // "ResearchMenu/button_queue_down"
        ResearchQueueCancel, // "ResearchMenu/button_queue_cancel"
        DanButton,     // UI/dan_button
        DanButtonBlue, // UI/dan_button_blue
    }

    public enum ButtonTextAlign
    {
        Center,
        Left,
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
        public ButtonTextAlign TextAlign = ButtonTextAlign.Center;
        public string Text;
        public string Tooltip;
        public string ClickSfx = "echo_affirm";

        public Action<UIButton> OnClick;

        public override string ToString() => $"Button '{Text}' visible:{Visible} enabled:{Enabled} state:{State}";
        
        public UIButton(ButtonStyle style)
        {
            Style = style;
        }

        public UIButton(UIElementV2 parent, ButtonStyle style) : base(parent)
        {
            Style = style;
        }

        public UIButton(UIElementV2 parent, Vector2 pos, string text) : base(parent, pos)
        {
            Text = text;
            Size = ButtonTexture().SizeF;
        }

        public UIButton(UIElementV2 parent, ButtonStyle style, Vector2 pos, string text) : base(parent, pos)
        {
            Style = style;
            Text  = text;
            Size  = ButtonTexture().SizeF;
        }

        public UIButton(UIElementV2 parent, ButtonStyle style, in Rectangle rect) : base(parent, rect)
        {
            Style = style;
        }

        class StyleTextures
        {
            public readonly SubTexture Normal;
            public readonly SubTexture Hover;
            public readonly SubTexture Pressed;
            public readonly Color DefaultColor = new Color(255, 240, 189);
            public readonly Color HoverColor   = new Color(255, 240, 189);
            public readonly Color PressColor   = new Color(255, 240, 189);
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
            public StyleTextures(string normal, bool danButtonBlue)
            {
                Normal = Hover = Pressed = ResourceManager.Texture(normal);
                if (danButtonBlue)
                {
                    DefaultColor = new Color(205, 229, 255);
                    HoverColor = new Color(174, 202, 255);
                    PressColor = new Color(174, 202, 255);
                }
                else
                {
                    HoverColor = new Color(255, 255, 255, 150);
                    PressColor = new Color(255, 255, 255, 150);
                }
            }
        }

        static int ContentId;
        static StyleTextures[] Styling;
        
        static StyleTextures GetStyle(ButtonStyle style)
        {
            if (Styling != null && ContentId == ResourceManager.ContentId)
                return Styling[(int)style];

            ContentId = ResourceManager.ContentId;
            Styling = new []
            {
                new StyleTextures("EmpireTopBar/empiretopbar_btn_168px"),
                new StyleTextures("EmpireTopBar/empiretopbar_btn_68px"),
                new StyleTextures("EmpireTopBar/empiretopbar_low_btn_80px"),
                new StyleTextures("EmpireTopBar/empiretopbar_low_btn_100px"),
                new StyleTextures("EmpireTopBar/empiretopbar_btn_132px"),
                new StyleTextures("EmpireTopBar/empiretopbar_btn_132px_menu"),
                new StyleTextures("EmpireTopBar/empiretopbar_btn_168px_dip"),
                new StyleTextures("EmpireTopBar/empiretopbar_btn_168px_military"),
                new StyleTextures("NewUI/Close_Normal", "NewUI/Close_Hover"),
                new StyleTextures("ResearchMenu/button_queue_up", "ResearchMenu/button_queue_up_hover"),
                new StyleTextures("ResearchMenu/button_queue_down", "ResearchMenu/button_queue_down_hover"),
                new StyleTextures("ResearchMenu/button_queue_cancel", "ResearchMenu/button_queue_cancel_hover"),
                new StyleTextures("UI/dan_button", danButtonBlue:false),
                new StyleTextures("UI/dan_button_blue", danButtonBlue:true),
            };
            return Styling[(int)style];
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

            if (Text.NotEmpty())
            {
                SpriteFont font = Fonts.Arial12Bold;

                Vector2 textCursor;
                if (TextAlign == ButtonTextAlign.Center)
                    textCursor.X = r.X + r.Width / 2 - font.MeasureString(Text).X / 2f;
                else
                    textCursor.X = r.X + 25f;

                textCursor.Y = r.Y + r.Height / 2 - font.LineSpacing / 2;
                if (State == PressState.Pressed)
                    textCursor.Y += 1f; // pressed down effect

                batch.DrawString(font, Text, textCursor, Enabled ? TextColor() : Color.Gray);
            }

            if (State == PressState.Hover && Tooltip.NotEmpty())
            {
                ToolTip.CreateTooltip(Tooltip);
            }
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
                GameAudio.MouseOver();

            if (State == PressState.Pressed && input.LeftMouseReleased)
            {
                State = PressState.Hover;
                OnClick?.Invoke(this);
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

            State = PressState.Hover;
            // @note This should return false to capture the hover input,
            //       however most UI code doesn't use UIElementV2 system yet,
            //       so returning true would falsely trigger a lot of old style buttons
            //       Semantic differences:
            //         old system: true means click/event happened
            //         UIElementV2: true means input was handled/captured and should not propagate to other elements
            return false;
        }

    }
}
