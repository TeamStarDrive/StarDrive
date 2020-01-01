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
        Military,   // empiretopbar_btn_168px_military
        Close,      // NewUI/Close_Normal
        ResearchQueueUp, // "ResearchMenu/button_queue_up"
        ResearchQueueDown, // "ResearchMenu/button_queue_down"
        ResearchQueueCancel, // "ResearchMenu/button_queue_cancel"
        DanButton,     // UI/dan_button
        DanButtonBlue, // UI/dan_button_blue
        Formation, // 
    }

    public partial class UIButton
    {
        class StyleTextures
        {
            public readonly SubTexture Normal;
            public readonly SubTexture Hover;
            public readonly SubTexture Pressed;
            public readonly Color DefaultColor = new Color(255, 240, 189);
            public readonly Color HoverColor = new Color(255, 240, 189);
            public readonly Color PressColor = new Color(255, 240, 189);

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

            public StyleTextures(string normal, bool danButtonBlue)
            {
                Normal = Hover = Pressed = ResourceManager.Texture(normal);
                if (danButtonBlue)
                {
                    DefaultColor = new Color(205, 229, 255);
                    HoverColor   = new Color(174, 202, 255);
                    PressColor   = new Color(174, 202, 255);
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
                return Styling[(int) style];

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
                new StyleTextures("UI/dan_button", danButtonBlue: false),
                new StyleTextures("UI/dan_button_blue", danButtonBlue: true),
            };
            return Styling[(int) style];
        }
    }
}