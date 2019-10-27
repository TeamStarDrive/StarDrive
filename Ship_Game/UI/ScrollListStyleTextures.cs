using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Ship_Game
{
    class ScrollListStyleTextures
    {
        public readonly SubTexture ScrollBarArrowUp;
        public readonly SubTexture ScrollBarArrowUpHover1;
        public readonly SubTexture ScrollBarArrowDown;
        public readonly SubTexture ScrollBarArrowDownHover1;
        public readonly SubTexture ScrollBarUpDown;
        public readonly SubTexture ScrollBarUpDownHover1;
        public readonly SubTexture ScrollBarUpDownHover2;
        public readonly SubTexture ScrollBarMid;
        public readonly SubTexture ScrollBarMidHover1;
        public readonly SubTexture ScrollBarMidHover2;

        public readonly SubTexture BuildAdd;
        public readonly SubTexture BuildAddHover1;
        public readonly SubTexture BuildAddHover2;
        public readonly SubTexture BuildEdit;
        public readonly SubTexture BuildEditHover1;
        public readonly SubTexture BuildEditHover2;

        public readonly SubTexture QueueArrowUp;
        public readonly SubTexture QueueArrowUpHover1;
        public readonly SubTexture QueueArrowUpHover2;
        public readonly SubTexture QueueArrowDown;
        public readonly SubTexture QueueArrowDownHover1;
        public readonly SubTexture QueueArrowDownHover2;
        public readonly SubTexture QueueRush;
        public readonly SubTexture QueueRushHover1;
        public readonly SubTexture QueueRushHover2;
        public readonly SubTexture QueueDelete;
        public readonly SubTexture QueueDeleteHover1;
        public readonly SubTexture QueueDeleteHover2;

        public ScrollListStyleTextures(string folder)
        {
            ScrollBarArrowUp         = ResourceManager.Texture(folder+"/scrollbar_arrow_up");
            ScrollBarArrowUpHover1   = ResourceManager.Texture(folder+"/scrollbar_arrow_up_hover1");
            ScrollBarArrowDown       = ResourceManager.Texture(folder+"/scrollbar_arrow_down");
            ScrollBarArrowDownHover1 = ResourceManager.Texture(folder+"/scrollbar_arrow_down_hover1");
            ScrollBarMid             = ResourceManager.Texture(folder+"/scrollbar_bar_mid");
            ScrollBarMidHover1       = ResourceManager.Texture(folder+"/scrollbar_bar_mid_hover1");
            ScrollBarMidHover2       = ResourceManager.Texture(folder+"/scrollbar_bar_mid_hover2");
            ScrollBarUpDown          = ResourceManager.Texture(folder+"/scrollbar_bar_updown");
            ScrollBarUpDownHover1    = ResourceManager.Texture(folder+"/scrollbar_bar_updown_hover1");
            ScrollBarUpDownHover2    = ResourceManager.Texture(folder+"/scrollbar_bar_updown_hover2");

            BuildAdd          = ResourceManager.Texture("NewUI/icon_build_add");
            BuildAddHover1    = ResourceManager.Texture("NewUI/icon_build_add_hover1");
            BuildAddHover2    = ResourceManager.Texture("NewUI/icon_build_add_hover2");
            BuildEdit         = ResourceManager.Texture("NewUI/icon_build_edit");
            BuildEditHover1   = ResourceManager.Texture("NewUI/icon_build_edit_hover1");
            BuildEditHover2   = ResourceManager.Texture("NewUI/icon_build_edit_hover2");

            QueueArrowUp         = ResourceManager.Texture("NewUI/icon_queue_arrow_up");
            QueueArrowUpHover1   = ResourceManager.Texture("NewUI/icon_queue_arrow_up_hover1");
            QueueArrowUpHover2   = ResourceManager.Texture("NewUI/icon_queue_arrow_up_hover2");
            QueueArrowDown       = ResourceManager.Texture("NewUI/icon_queue_arrow_down");
            QueueArrowDownHover1 = ResourceManager.Texture("NewUI/icon_queue_arrow_down_hover1");
            QueueArrowDownHover2 = ResourceManager.Texture("NewUI/icon_queue_arrow_down_hover2");
            QueueRush            = ResourceManager.Texture("NewUI/icon_queue_rushconstruction");
            QueueRushHover1      = ResourceManager.Texture("NewUI/icon_queue_rushconstruction_hover1");
            QueueRushHover2      = ResourceManager.Texture("NewUI/icon_queue_rushconstruction_hover2");
            QueueDelete          = ResourceManager.Texture("NewUI/icon_queue_delete");
            QueueDeleteHover1    = ResourceManager.Texture("NewUI/icon_queue_delete_hover1");
            QueueDeleteHover2    = ResourceManager.Texture("NewUI/icon_queue_delete_hover2");
        }

        static int ContentId;
        static ScrollListStyleTextures[] Styling;

        public static ScrollListStyleTextures Get(ListStyle style)
        {
            if (Styling == null || ContentId != ResourceManager.ContentId)
            {
                ContentId = ResourceManager.ContentId;
                Styling = new[]
                {
                    new ScrollListStyleTextures("NewUI"),
                    new ScrollListStyleTextures("ResearchMenu"),
                };
            }
            return Styling[(int)style];
        }
    }
}
