using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace Ship_Game
{
    public sealed class Menu1 : UIElementV2
    {
        Rectangle corner_TL;
        Rectangle corner_TR;
        Rectangle corner_BL;
        Rectangle corner_BR;
        Rectangle vertLeft;
        Rectangle vertRight;
        Rectangle horizTop;
        Rectangle horizBot;
        Rectangle fillRect;

        // TODO: make private after scroll-list branch is merged
        public Submenu subMenu;
        readonly bool WithSubMenu;


        public Menu1(int x, int y, int width, int height)
            : this(new Rectangle(x, y, width, height))
        {
        }

        public Menu1(int x, int y, int width, int height, bool withSub)
            : this(new Rectangle(x, y, width, height), withSub)
        {
        }

        public Menu1(in Rectangle theMenu) : base(theMenu)
        {
            this.PerformLayout();
        }

        public Menu1(in Rectangle theMenu, bool withSub) : base(theMenu)
        {
            WithSubMenu = withSub;
            this.PerformLayout();
        }

        public override void PerformLayout()
        {
            base.PerformLayout();

            Rectangle r = Rect;
            StyleTextures s = GetStyle();
            corner_TL = new Rectangle(r.X, r.Y, s.TL.Width, s.TL.Height);
            corner_TR = new Rectangle(r.X + r.Width - s.TR.Width, r.Y, s.TR.Width, s.TR.Height);
            corner_BL = new Rectangle(r.X, r.Y + r.Height - s.BL.Height, s.BL.Width, s.BL.Height);
            corner_BR = new Rectangle(r.X + r.Width - s.BR.Width, r.Y + r.Height - s.BR.Height, s.BR.Width, s.BR.Height);
            horizTop  = new Rectangle(corner_TL.X + corner_TL.Width, corner_TL.Y, r.Width - corner_TL.Width - corner_TR.Width, s.HT.Height);
            horizBot  = new Rectangle(corner_BL.X + corner_BL.Width, r.Y + r.Height - s.HB.Height, r.Width - corner_BL.Width - corner_BR.Width, s.HB.Height);
            vertLeft  = new Rectangle(corner_TL.X + 1, corner_TL.Y + corner_TL.Height, s.VL.Width, r.Height - corner_TL.Height - corner_BL.Height);
            vertRight = new Rectangle(r.X + r.Width - 1 - s.VR.Width, corner_TR.Y + corner_TR.Height, s.VR.Width, r.Height - corner_TR.Height - corner_BR.Height);
            fillRect  = new Rectangle(r.X + 8, r.Y + 8, r.Width - 16, r.Height - 16);

            var subMenuRect = new Rectangle(r.X + 20, r.Y - 5, r.Width - 40, r.Height - 15);

            if (WithSubMenu && subMenu == null)
            {
                subMenu = new Submenu(subMenuRect);
            }
            if (subMenu != null)
            {
                subMenu.Rect = subMenuRect;
                subMenu.PerformLayout();
            }
        }

        public override bool HandleInput(InputState input)
        {
            return false;
        }

        public override void Draw(SpriteBatch batch, DrawTimes elapsed)
        {
            StyleTextures s = GetStyle();
            batch.FillRectangle(fillRect, new Color(0, 0, 0, 220));
            batch.Draw(s.TL, corner_TL, Color.White);
            batch.Draw(s.TR, corner_TR, Color.White);
            batch.Draw(s.BL, corner_BL, Color.White);
            batch.Draw(s.BR, corner_BR, Color.White);
            batch.Draw(s.HB, horizBot, Color.White);
            batch.Draw(s.HT, horizTop, Color.White);
            batch.Draw(s.VL, vertLeft, Color.White);
            batch.Draw(s.VR, vertRight, Color.White);
            subMenu?.Draw(batch, elapsed);
        }

        
        static int ContentId;
        static StyleTextures Style;

        class StyleTextures
        {
            public SubTexture TL, TR, BL, BR;
            public SubTexture HT, HB, VL, VR; // HorizTop, HorizBot, VertLeft, VertRight

            public StyleTextures()
            {
                TL = ResourceManager.Texture("NewUI/menu_1_corner_TL");
                TR = ResourceManager.Texture("NewUI/menu_1_corner_TR");
                BL = ResourceManager.Texture("NewUI/menu_1_corner_BL");
                BR = ResourceManager.Texture("NewUI/menu_1_corner_BR");
                
                HT = ResourceManager.Texture("NewUI/menu_1_horiz_upper");
                HB = ResourceManager.Texture("NewUI/menu_1_horiz_lower");
                VL = ResourceManager.Texture("NewUI/menu_1_vert_left");
                VR = ResourceManager.Texture("NewUI/menu_1_vert_right");
            }
        }

        static StyleTextures GetStyle()
        {
            if (Style == null || ContentId != ResourceManager.ContentId)
            {
                ContentId = ResourceManager.ContentId;
                Style = new StyleTextures();
            }
            return Style;
        }
    }
}