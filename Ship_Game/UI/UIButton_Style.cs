using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;

namespace Ship_Game
{
    public enum ButtonStyle
    {
        Default,    // empiretopbar_btn_168px
        Small,      // empiretopbar_btn_68px
        Low80,      // empiretopbar_low_btn_80px
        Low100,     // empiretopbar_low_btn_100px
        Medium,     // empiretopbar_btn_132px       -- GoldenBrown button
        MediumMenu, // empiretopbar_btn_132px_menu  -- Grayed out button
        BigDip,     // empiretopbar_btn_168px_dip
        Military,   // empiretopbar_btn_168px_military
        Close,      // NewUI/Close_Normal
        ResearchQueueUp, // "ResearchMenu/button_queue_up"
        ResearchQueueDown, // "ResearchMenu/button_queue_down"
        ResearchQueueCancel, // "ResearchMenu/button_queue_cancel"
        DanButton,     // UI/dan_button  -- wide brown button
        DanButtonBlue, // UI/dan_button_blue -- blue version of dan_button
        DanButtonRed, // UI/dan_button_red -- red version of dan_button
        EventConfirm, // UI/btn_event_confirm -- a big wide confirm button for Event Popups
        Text,       // only use TEXT as the button 
    }

    public partial class UIButton
    {
        public class StyleTextures
        {
            public SubTexture Normal;
            public SubTexture Hover;
            public SubTexture Pressed;

            // Text Colors
            public Color DefaultTextColor = new Color(255, 240, 189);
            public Color HoverTextColor   = new Color(255, 240, 189);
            public Color PressTextColor   = new Color(255, 240, 189);

            // Fallback background colors if texture is null
            public Color DefaultColor = new Color(96, 81, 49);
            public Color HoverColor   = new Color(106, 91, 59);
            public Color PressColor   = new Color(86, 71, 39);

            public bool DrawBackground = true;

            public StyleTextures()
            {
            }

            public StyleTextures(string normal)
            {
                Normal  = ResourceManager.Texture(normal);
                Hover   = ResourceManager.Texture(normal + "_hover");
                Pressed = ResourceManager.Texture(normal + "_pressed");
            }

            public StyleTextures(string normal, string hover)
            {
                Normal  = ResourceManager.Texture(normal);
                Hover   = ResourceManager.Texture(hover);
                Pressed = Hover;
            }

            public StyleTextures(string normal, ButtonStyle style)
            {
                Normal = Hover = Pressed = ResourceManager.Texture(normal);
                switch (style)
                {
                    default:
                    case ButtonStyle.DanButton:
                        HoverTextColor = new Color(255, 255, 255, 150);
                        PressTextColor = new Color(255, 255, 255, 150);
                        break;
                    case ButtonStyle.DanButtonBlue:
                        DefaultTextColor = new Color(205, 229, 255);
                        HoverTextColor   = new Color(174, 202, 255);
                        PressTextColor   = new Color(174, 202, 255);
                        break;
                    case ButtonStyle.DanButtonRed:
                        DefaultTextColor = Color.Red;
                        HoverTextColor   = Color.White;
                        PressTextColor   = Color.Green;
                        break;
                }
            }
        }

        static int ContentId;
        static StyleTextures[] Styling;

        static StyleTextures GetDefaultStyle(ButtonStyle style)
        {
            if (Styling != null && ContentId == ResourceManager.ContentId)
                return Styling[(int)style];

            ContentId = ResourceManager.ContentId;
            Styling = new[]
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
                new StyleTextures("UI/dan_button", ButtonStyle.DanButton),
                new StyleTextures("UI/dan_button_blue", ButtonStyle.DanButtonBlue),
                new StyleTextures("UI/dan_button_red", ButtonStyle.DanButtonRed),
                new StyleTextures("UI/btn_event_confirm_big"),
                new StyleTextures() { DrawBackground = false },
            };
            return Styling[(int) style];
        }

        void SetStyle(ButtonStyle style)
        {
            StyleTextures defaultStyle = GetDefaultStyle(style);
            SetStyle(defaultStyle);
        }

        void SetStyle(StyleTextures style)
        {
            Normal = style.Normal;
            Hover = style.Hover;
            Pressed = style.Pressed;

            DefaultTextColor = style.DefaultTextColor;
            HoverTextColor = style.HoverTextColor;
            PressTextColor = style.PressTextColor;

            DefaultColor = style.DefaultColor;
            HoverColor = style.HoverColor;
            PressColor = style.PressColor;

            DrawBackground = style.DrawBackground;
        }

        Vector2 GetInitialSize()
        {
            if (Normal != null)
                return Normal.SizeF;
            return RichText.Size;
        }
        
        SubTexture ButtonTexture()
        {
            switch (State)
            {
                default:                 return Normal;
                case PressState.Hover:   return Hover;
                case PressState.Pressed: return Pressed;
            }
        }

        Color BackgroundColor()
        {
            switch (State)
            {
                default:                 return DefaultColor;
                case PressState.Hover:   return HoverColor;
                case PressState.Pressed: return PressColor;
            }
        }

        Color TextColor()
        {
            switch (State)
            {
                default:                 return DefaultTextColor;
                case PressState.Hover:   return HoverTextColor;
                case PressState.Pressed: return PressTextColor;
            }
        }

    }
}