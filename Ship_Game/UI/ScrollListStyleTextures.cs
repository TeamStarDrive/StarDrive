using System;
using System.Diagnostics.Contracts;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace Ship_Game
{
    public class ScrollListStyleTextures
    {
        public class Hoverable
        {
            public SubTexture Normal;
            public SubTexture Hover1;
            public SubTexture Hover2;
            public Hoverable(string defaultName)
            {
                Normal = ResourceManager.Texture(defaultName);
                Hover1 = ResourceManager.Texture(defaultName+"_hover1");
                Hover2 = ResourceManager.Texture(defaultName+"_hover2");
            }
            public void Draw(SpriteBatch batch, in Rectangle rect, bool parentHovered, bool controlItemHovered)
            {
                SubTexture texture = Normal;
                if     (controlItemHovered) texture = Hover2; // control[eg cancel button] is being hovered
                else if (parentHovered)     texture = Hover1; // parent container is being hovered
                batch.Draw(texture, rect, Color.White);
            }
        }

        public readonly Hoverable ScrollBarArrowUp;
        public readonly Hoverable ScrollBarArrowDown;
        public readonly Hoverable ScrollBarUpDown;
        public readonly Hoverable ScrollBarMid;

        public readonly Hoverable BuildAdd;
        public readonly Hoverable BuildEdit;
        public readonly Hoverable QueueArrowUp;
        public readonly Hoverable QueueArrowDown;
        public readonly Hoverable QueueRush;
        public readonly Hoverable QueueDelete;

        public ScrollListStyleTextures(string folder)
        {
            ScrollBarArrowUp   = new Hoverable(folder+"/scrollbar_arrow_up");
            ScrollBarArrowDown = new Hoverable(folder+"/scrollbar_arrow_down");
            ScrollBarMid       = new Hoverable(folder+"/scrollbar_bar_mid");
            ScrollBarUpDown    = new Hoverable(folder+"/scrollbar_bar_updown");
            BuildAdd       = new Hoverable("NewUI/icon_build_add");
            BuildEdit      = new Hoverable("NewUI/icon_build_edit");
            QueueArrowUp   = new Hoverable("NewUI/icon_queue_arrow_up");
            QueueArrowDown = new Hoverable("NewUI/icon_queue_arrow_down");
            QueueRush      = new Hoverable("NewUI/icon_queue_rushconstruction");
            QueueDelete    = new Hoverable("NewUI/icon_queue_delete");
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
