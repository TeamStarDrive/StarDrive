using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Ship_Game.Audio;

namespace Ship_Game
{
    public interface IListScreen
    {
        void ResetLists();
    }

    public enum SubmenuStyle
    {
        Brown,
        Blue,
    }

    public class Submenu : UIElementV2
    {
        public Array<Tab> Tabs = new Array<Tab>();

        Rectangle UpperLeft;
        Rectangle TR;
        Rectangle topHoriz;
        Rectangle botHoriz;
        Rectangle BL;
        Rectangle BR;
        Rectangle VL;
        Rectangle VR;
        Rectangle TL;
        readonly SpriteFont Font = Fonts.Pirulen12;
        readonly SubmenuStyle Style;

        public Submenu(in Rectangle theMenu, SubmenuStyle style = SubmenuStyle.Brown) : base(null, theMenu)
        {
            Style = style;
            this.PerformLayout();
        }

        public Submenu(int x, int y, int width, int height, SubmenuStyle style = SubmenuStyle.Brown)
            : this(new Rectangle(x, y, width, height), style)
        {
        }

        public Submenu(float x, float y, float width, float height, SubmenuStyle style = SubmenuStyle.Brown)
            : this(new Rectangle((int)x, (int)y, (int)width, (int)height), style)
        {
        }

        public class StyleTextures
        {
            public SubTexture HorizVert;
            public SubTexture CornerTL;
            public SubTexture CornerTR;
            public SubTexture CornerBR;
            public SubTexture CornerBL;

            public SubTexture Left;
            public SubTexture LeftUnsel;
            public SubTexture HoverLeftEdge;
            public SubTexture HoverLeft;
            public SubTexture HoverMid;
            public SubTexture HoverRight;
            public SubTexture Middle;
            public SubTexture MiddleUnsel;
            public SubTexture Right;
            public SubTexture RightUnsel;
            public SubTexture RightExt;
            public SubTexture RightExtUnsel;

            public StyleTextures(SubmenuStyle style)
            {
                switch (style)
                {
                    case SubmenuStyle.Brown:
                        HorizVert       = ResourceManager.Texture("NewUI/submenu_horiz_vert");
                        CornerTL        = ResourceManager.Texture("NewUI/submenu_corner_TL");
                        CornerTR        = ResourceManager.Texture("NewUI/submenu_corner_TR");
                        CornerBR        = ResourceManager.Texture("NewUI/submenu_corner_BR");
                        CornerBL        = ResourceManager.Texture("NewUI/submenu_corner_BL");

                        HoverLeftEdge = ResourceManager.Texture("NewUI/submenu_header_hover_leftedge");
                        HoverLeft     = ResourceManager.Texture("NewUI/submenu_header_hover_left");
                        HoverMid      = ResourceManager.Texture("NewUI/submenu_header_hover_mid");
                        HoverRight    = ResourceManager.Texture("NewUI/submenu_header_hover_right");

                        Left          = ResourceManager.Texture("NewUI/submenu_header_left");
                        LeftUnsel     = ResourceManager.Texture("NewUI/submenu_header_left_unsel");
                        Middle        = ResourceManager.Texture("NewUI/submenu_header_middle");
                        MiddleUnsel   = ResourceManager.Texture("NewUI/submenu_header_middle_unsel");
                        Right         = ResourceManager.Texture("NewUI/submenu_header_right");
                        RightUnsel    = ResourceManager.Texture("NewUI/submenu_header_right_unsel");
                        RightExt      = ResourceManager.Texture("NewUI/submenu_header_rightextend");
                        RightExtUnsel = ResourceManager.Texture("NewUI/submenu_header_rightextend_unsel");
                        break;
                    
                    case SubmenuStyle.Blue:
                        HorizVert     = ResourceManager.Texture("ResearchMenu/submenu_horiz_vert");
                        CornerTL      = ResourceManager.Texture("ResearchMenu/submenu_corner_TL");
                        CornerTR      = ResourceManager.Texture("ResearchMenu/submenu_corner_TR");
                        CornerBR      = ResourceManager.Texture("ResearchMenu/submenu_corner_BR");
                        CornerBL      = ResourceManager.Texture("ResearchMenu/submenu_corner_BL");

                        // research menu doesn't have any hovers, so just reuse left/middle/right
                        HoverLeftEdge = ResourceManager.Texture("ResearchMenu/submenu_header_left");
                        HoverLeft     = ResourceManager.Texture("ResearchMenu/submenu_header_left");
                        HoverMid      = ResourceManager.Texture("ResearchMenu/submenu_header_middle");
                        HoverRight    = ResourceManager.Texture("ResearchMenu/submenu_header_right");

                        Left          = ResourceManager.Texture("ResearchMenu/submenu_header_left");
                        LeftUnsel     = ResourceManager.Texture("ResearchMenu/submenu_header_left");
                        Middle        = ResourceManager.Texture("ResearchMenu/submenu_header_middle");
                        MiddleUnsel   = ResourceManager.Texture("ResearchMenu/submenu_header_middle");
                        Right         = ResourceManager.Texture("ResearchMenu/submenu_header_right");
                        RightUnsel    = ResourceManager.Texture("ResearchMenu/submenu_header_right");
                        RightExt      = ResourceManager.Texture("ResearchMenu/submenu_transition_right");
                        RightExtUnsel = ResourceManager.Texture("ResearchMenu/submenu_transition_right");
                        break;
                }
            }
        }

        static int ContentId;
        static StyleTextures[] Styling;

        StyleTextures GetStyle()
        {
            if (Styling == null || ContentId != ResourceManager.ContentId)
            {
                ContentId = ResourceManager.ContentId;
                Styling = new[]
                {
                    new StyleTextures(SubmenuStyle.Brown),
                    new StyleTextures(SubmenuStyle.Blue),
                };
            }
            return Styling[(int)Style];
        }

        public override void PerformLayout()
        {
            StyleTextures s = GetStyle();
            Rectangle r = Rect;
            TL = new Rectangle(r.X, r.Y + 25 - 2, s.CornerTL.Width, s.CornerTL.Height);
            TR = new Rectangle(r.X + r.Width - s.CornerTR.Width, r.Y + 25 - 2, s.CornerTR.Width, s.CornerTR.Height);
            BL = new Rectangle(r.X, r.Y + r.Height - s.CornerBL.Height + 2, s.CornerBL.Width, s.CornerBL.Height);
            BR = new Rectangle(r.X + r.Width - s.CornerBR.Width, r.Y + r.Height + 2 - s.CornerBR.Height, s.CornerBR.Width, s.CornerBR.Height);
            VL = new Rectangle(r.X, r.Y + 25 + TR.Height - 2, 2, r.Height - 25 - BL.Height - 2);
            VR = new Rectangle(r.X + r.Width - 2, r.Y + 25 + TR.Height - 2, 2, r.Height - 25 - BR.Height - 2);
            UpperLeft = new Rectangle(r.X, r.Y, s.Left.Width, s.Left.Height);
            topHoriz = new Rectangle(r.X + TL.Width, r.Y + 25 - 2, r.Width - TR.Width - TL.Width, 2);
            botHoriz = new Rectangle(r.X + BL.Width, r.Y + r.Height, r.Width - BL.Width - BR.Width, 2);
        }


        public void AddTab(string title)
        {
            StyleTextures s = GetStyle();
            float tabX = UpperLeft.X + UpperLeft.Width;
            foreach (Tab ta in Tabs)
                tabX += ta.tabRect.Width + s.Right.Width;

            var tabRect = new Rectangle((int)tabX, UpperLeft.Y, (int)Font.MeasureString(title).X + 2, 25);
            Tabs.Add(new Tab
            {
                tabRect  = tabRect,
                Title    = title,
                Selected = Tabs.Count == 0,
                Hover    = false
            });
        }

        public override void Draw(SpriteBatch batch)
        {
            if (!Visible)
                return;
            
            StyleTextures s = GetStyle();

            batch.Draw(s.CornerTL, TL, Color.White);
            if (Tabs.Count > 0)
            {
                SubTexture header = Tabs[0].Selected ? s.Left :
                                    Tabs[0].Hover    ? s.HoverLeftEdge :
                                                       s.LeftUnsel;
                batch.Draw(header, UpperLeft, Color.White);
            }

            if (Tabs.Count == 1)
            {
                foreach (Tab t in Tabs)
                {
                    var right = new Rectangle(t.tabRect.X + t.tabRect.Width, t.tabRect.Y, s.Right.Width, 25);
                    var textPos = new Vector2(t.tabRect.X, (t.tabRect.Y + t.tabRect.Height / 2 - Font.LineSpacing / 2));

                    batch.Draw(s.Middle, t.tabRect, Color.White);
                    batch.Draw(s.Right, right, Color.White);
                    batch.DrawString(Font, t.Title, textPos, new Color(255, 239, 208));
                }
            }
            else if (Tabs.Count > 1)
            {
                for (int i = 0; i < Tabs.Count; i++)
                {
                    Tab t = Tabs[i];
                    if (t.Selected)
                    {
                        var right = new Rectangle(t.tabRect.X + t.tabRect.Width, t.tabRect.Y, s.Right.Width, 25);

                        batch.Draw(s.Middle, t.tabRect, Color.White);
                        batch.Draw(s.Right, right, Color.White);

                        if (Tabs.Count - 1 > i && !Tabs[i + 1].Selected)
                        {
                            SubTexture tab = Tabs[i + 1].Hover ? s.HoverLeft : s.RightExtUnsel;
                            batch.Draw(tab, right, Color.White);
                        }
                    }
                    else if (!t.Hover)
                    {
                        var right = new Rectangle(t.tabRect.X + t.tabRect.Width, t.tabRect.Y, s.RightUnsel.Width, 25);

                        batch.Draw(s.MiddleUnsel, t.tabRect, Color.White);
                        batch.Draw(s.RightUnsel, right, Color.White);
                        if (Tabs.Count - 1 > i)
                        {
                            SubTexture tex = Tabs[i + 1].Selected ? s.RightExt :
                                             Tabs[i + 1].Hover    ? s.HoverLeft :
                                                                    s.RightExtUnsel;
                            batch.Draw(tex, right, Color.White);
                        }
                    }
                    else
                    {
                        var right = new Rectangle(t.tabRect.X + t.tabRect.Width, t.tabRect.Y, s.HoverRight.Width, 25);

                        batch.Draw(s.HoverMid, t.tabRect, Color.White);
                        batch.Draw(s.HoverRight, right, Color.White);
                        if (Tabs.Count - 1 > i)
                        {
                            SubTexture tex = Tabs[i + 1].Selected ? s.RightExt :
                                             Tabs[i + 1].Hover    ? s.HoverLeft :
                                                                    s.RightExtUnsel;
                            batch.Draw(tex, right, Color.White);
                        }
                    }
                    var textPos = new Vector2(t.tabRect.X, (t.tabRect.Y + t.tabRect.Height / 2 - Font.LineSpacing / 2));
                    batch.DrawString(Font, t.Title, textPos, new Color(255, 239, 208));
                }
            }
            batch.Draw(s.HorizVert, topHoriz, Color.White);
            batch.Draw(s.CornerTR,  TR, Color.White);
            batch.Draw(s.HorizVert, botHoriz, Color.White);
            batch.Draw(s.CornerBR,  BR, Color.White);
            batch.Draw(s.CornerBL,  BL, Color.White);
            batch.Draw(s.HorizVert, VR, Color.White);
            batch.Draw(s.HorizVert, VL, Color.White);
        }

        /// TODO: there are 3 pretty much identical functions here... what the hell??
        public void HandleInput(InputState input, IListScreen caller)
        {
            if (!Visible || !Enabled)
                return;

            for (int i = 0; i < Tabs.Count; i++)
            {
                Tab tab = Tabs[i];
                if (!tab.tabRect.HitTest(input.CursorPosition))
                {
                    tab.Hover = false;
                    continue;
                }
                tab.Hover = true;
                if (input.LeftMouseClick)
                {
                    GameAudio.AcceptClick();
                    tab.Selected = true;
                    foreach (Tab otherTab in Tabs)
                        if (otherTab != tab) otherTab.Selected = false;

                    caller.ResetLists();
                }
            }
        }

        public override bool HandleInput(InputState input)
        {
            if (!Visible || !Enabled)
                return false;

            Vector2 mousePos = input.CursorPosition;
            for (int i = 0; i < Tabs.Count; i++)
            {
                Tab tab = Tabs[i];
                if (!tab.tabRect.HitTest(mousePos))
                {
                    tab.Hover = false;
                }
                else
                {
                    tab.Hover = true;
                    if (input.LeftMouseClick)
                    {
                        GameAudio.AcceptClick();
                        tab.Selected = true;
                        foreach (Tab t1 in Tabs)
                        {
                            if (t1 == tab)
                            {
                                continue;
                            }
                            t1.Selected = false;
                        }
                        return true;
                    }
                }

            }
            return false;
        }

        public void HandleInputNoReset(InputState input)
        {
            if (!Visible || !Enabled)
                return;

            foreach (Tab tab in Tabs)
            {
                if (!tab.tabRect.HitTest(input.CursorPosition))
                {
                    tab.Hover = false;
                    continue;
                }

                tab.Hover = true;
                if (input.LeftMouseClick)
                    continue;
                tab.Selected = true;
                foreach (Tab otherTab in Tabs)
                {
                    if (otherTab != tab) otherTab.Selected = false;
                }
            }
        }

        public class Tab
        {
            public string Title;
            public Rectangle tabRect;
            public bool Selected;
            public bool Hover;
        }
    }
}