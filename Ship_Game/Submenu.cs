using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace Ship_Game
{
    public class SubmenuStyle
    {
        public SubTexture HorizVert       { get; private set; }
        public SubTexture CornerTL        { get; private set; }
        public SubTexture CornerTR        { get; private set; }
        public SubTexture CornerBR        { get; private set; }
        public SubTexture CornerBL        { get; private set; }

        public SubTexture Left          { get; private set; }
        public SubTexture LeftUnsel     { get; private set; }
        public SubTexture HoverLeftEdge { get; private set; }
        public SubTexture HoverLeft     { get; private set; }
        public SubTexture HoverMid      { get; private set; }
        public SubTexture HoverRight    { get; private set; }
        public SubTexture Middle        { get; private set; }
        public SubTexture MiddleUnsel   { get; private set; }
        public SubTexture Right         { get; private set; }
        public SubTexture RightUnsel    { get; private set; }
        public SubTexture RightExt      { get; private set; }
        public SubTexture RightExtUnsel { get; private set; }

        // create the style dynamically to allow hotloading
        public static SubmenuStyle CreateBrown() => new SubmenuStyle
        {
            HorizVert       = ResourceManager.Texture("NewUI/submenu_horiz_vert"),
            CornerTL        = ResourceManager.Texture("NewUI/submenu_corner_TL"),
            CornerTR        = ResourceManager.Texture("NewUI/submenu_corner_TR"),
            CornerBR        = ResourceManager.Texture("NewUI/submenu_corner_BR"),
            CornerBL        = ResourceManager.Texture("NewUI/submenu_corner_BL"),

            HoverLeftEdge = ResourceManager.Texture("NewUI/submenu_header_hover_leftedge"),
            HoverLeft     = ResourceManager.Texture("NewUI/submenu_header_hover_left"),
            HoverMid      = ResourceManager.Texture("NewUI/submenu_header_hover_mid"),
            HoverRight    = ResourceManager.Texture("NewUI/submenu_header_hover_right"),

            Left          = ResourceManager.Texture("NewUI/submenu_header_left"),
            LeftUnsel     = ResourceManager.Texture("NewUI/submenu_header_left_unsel"),
            Middle        = ResourceManager.Texture("NewUI/submenu_header_middle"),
            MiddleUnsel   = ResourceManager.Texture("NewUI/submenu_header_middle_unsel"),
            Right         = ResourceManager.Texture("NewUI/submenu_header_right"),
            RightUnsel    = ResourceManager.Texture("NewUI/submenu_header_right_unsel"),
            RightExt      = ResourceManager.Texture("NewUI/submenu_header_rightextend"),
            RightExtUnsel = ResourceManager.Texture("NewUI/submenu_header_rightextend_unsel")
        };

        public static SubmenuStyle CreateBlue() => new SubmenuStyle
        {
            HorizVert       = ResourceManager.Texture("ResearchMenu/submenu_horiz_vert"),
            CornerTL        = ResourceManager.Texture("ResearchMenu/submenu_corner_TL"),
            CornerTR        = ResourceManager.Texture("ResearchMenu/submenu_corner_TR"),
            CornerBR        = ResourceManager.Texture("ResearchMenu/submenu_corner_BR"),
            CornerBL        = ResourceManager.Texture("ResearchMenu/submenu_corner_BL"),

            // research menu doesn't have any hovers, so just reuse left/middle/right
            HoverLeftEdge = ResourceManager.Texture("ResearchMenu/submenu_header_left"),
            HoverLeft     = ResourceManager.Texture("ResearchMenu/submenu_header_left"),
            HoverMid      = ResourceManager.Texture("ResearchMenu/submenu_header_middle"),
            HoverRight    = ResourceManager.Texture("ResearchMenu/submenu_header_right"),

            Left          = ResourceManager.Texture("ResearchMenu/submenu_header_left"),
            LeftUnsel     = ResourceManager.Texture("ResearchMenu/submenu_header_left"),
            Middle        = ResourceManager.Texture("ResearchMenu/submenu_header_middle"),
            MiddleUnsel   = ResourceManager.Texture("ResearchMenu/submenu_header_middle"),
            Right         = ResourceManager.Texture("ResearchMenu/submenu_header_right"),
            RightUnsel    = ResourceManager.Texture("ResearchMenu/submenu_header_right"),
            RightExt      = ResourceManager.Texture("ResearchMenu/submenu_transition_right"),
            RightExtUnsel = ResourceManager.Texture("ResearchMenu/submenu_transition_right")
        };
    }

    public class Submenu
    {
        public Rectangle Menu;
        private readonly ScreenManager ScreenManager;
        public Array<Tab> Tabs = new Array<Tab>();

        private Rectangle UpperLeft;
        private Rectangle TR;
        private Rectangle topHoriz;
        private Rectangle botHoriz;
        private Rectangle BL;
        private Rectangle BR;
        private Rectangle VL;
        private Rectangle VR;
        private Rectangle TL;

        private SpriteFont toUse;

        private readonly SubmenuStyle Style;
        private readonly InputState Input;

        public Submenu(Rectangle theMenu)
        {
            ScreenManager = Game1.Instance.ScreenManager;
            Input = ScreenManager.input;

            Style = SubmenuStyle.CreateBrown();
            toUse = Fonts.Pirulen12;
            InitLayout(theMenu);
        }
        public Submenu(bool blue, Rectangle theMenu)
        {
            ScreenManager = Game1.Instance.ScreenManager;
            Input = ScreenManager.input;
            Style = blue ? SubmenuStyle.CreateBlue() : SubmenuStyle.CreateBrown();
            toUse = Fonts.Pirulen12;
            InitLayout(theMenu);
        }

        private void InitLayout(Rectangle theMenu)
        {
            Menu = theMenu;
            UpperLeft = new Rectangle(theMenu.X, theMenu.Y, Style.Left.Width, Style.Left.Height);
            TL = new Rectangle(theMenu.X, theMenu.Y + 25 - 2, Style.CornerTL.Width, Style.CornerTL.Height);
            TR = new Rectangle(theMenu.X + theMenu.Width - Style.CornerTR.Width, theMenu.Y + 25 - 2, Style.CornerTR.Width, Style.CornerTR.Height);
            BL = new Rectangle(theMenu.X, theMenu.Y + theMenu.Height - Style.CornerBL.Height + 2, Style.CornerBL.Width, Style.CornerBL.Height);
            BR = new Rectangle(theMenu.X + theMenu.Width - Style.CornerBR.Width, theMenu.Y + theMenu.Height + 2 - Style.CornerBR.Height, Style.CornerBR.Width, Style.CornerBR.Height);
            topHoriz = new Rectangle(theMenu.X + TL.Width, theMenu.Y + 25 - 2, theMenu.Width - TR.Width - TL.Width, 2);
            botHoriz = new Rectangle(theMenu.X + BL.Width, theMenu.Y + theMenu.Height, theMenu.Width - BL.Width - BR.Width, 2);
            VL = new Rectangle(theMenu.X, theMenu.Y + 25 + TR.Height - 2, 2, theMenu.Height - 25 - BL.Height - 2);
            VR = new Rectangle(theMenu.X + theMenu.Width - 2, theMenu.Y + 25 + TR.Height - 2, 2, theMenu.Height - 25 - BR.Height - 2);
        }


        public void AddTab(string title)
        {
            float tabX = UpperLeft.X + UpperLeft.Width;
            foreach (Tab ta in Tabs)
                tabX += ta.tabRect.Width + Style.Right.Width;

            var tabRect = new Rectangle((int)tabX, UpperLeft.Y, (int)toUse.MeasureString(title).X + 2, 25);
            Tabs.Add(new Tab
            {
                tabRect  = tabRect,
                Title    = title,
                Selected = Tabs.Count == 0,
                Hover    = false
            });
        }

        public void Draw()
        {
            SpriteBatch spriteBatch = ScreenManager.SpriteBatch;

            spriteBatch.Draw(Style.CornerTL, TL, Color.White);
            if (Tabs.Count > 0)
            {
                SubTexture header = Tabs[0].Selected ? Style.Left :
                                    Tabs[0].Hover    ? Style.HoverLeftEdge :
                                                       Style.LeftUnsel;
                spriteBatch.Draw(header, UpperLeft, Color.White);
            }

            if (Tabs.Count == 1)
            {
                foreach (Tab t in Tabs)
                {
                    var right = new Rectangle(t.tabRect.X + t.tabRect.Width, t.tabRect.Y, Style.Right.Width, 25);
                    var textPos = new Vector2(t.tabRect.X, (t.tabRect.Y + t.tabRect.Height / 2 - toUse.LineSpacing / 2));

                    spriteBatch.Draw(Style.Middle, t.tabRect, Color.White);
                    spriteBatch.Draw(Style.Right, right, Color.White);
                    spriteBatch.DrawString(toUse, t.Title, textPos, new Color(255, 239, 208));
                }
            }
            else if (Tabs.Count > 1)
            {
                for (int i = 0; i < Tabs.Count; i++)
                {
                    Tab t = Tabs[i];
                    if (t.Selected)
                    {
                        var right = new Rectangle(t.tabRect.X + t.tabRect.Width, t.tabRect.Y, Style.Right.Width, 25);

                        spriteBatch.Draw(Style.Middle, t.tabRect, Color.White);
                        spriteBatch.Draw(Style.Right, right, Color.White);

                        if (Tabs.Count - 1 > i && !Tabs[i + 1].Selected)
                        {
                            SubTexture tab = Tabs[i + 1].Hover ? Style.HoverLeft : Style.RightExtUnsel;
                            spriteBatch.Draw(tab, right, Color.White);
                        }
                    }
                    else if (!t.Hover)
                    {
                        var right = new Rectangle(t.tabRect.X + t.tabRect.Width, t.tabRect.Y, Style.RightUnsel.Width, 25);

                        spriteBatch.Draw(Style.MiddleUnsel, t.tabRect, Color.White);
                        spriteBatch.Draw(Style.RightUnsel, right, Color.White);
                        if (Tabs.Count - 1 > i)
                        {
                            SubTexture tex = Tabs[i + 1].Selected ? Style.RightExt :
                                             Tabs[i + 1].Hover    ? Style.HoverLeft :
                                                                    Style.RightExtUnsel;
                            spriteBatch.Draw(tex, right, Color.White);
                        }
                    }
                    else
                    {
                        var right = new Rectangle(t.tabRect.X + t.tabRect.Width, t.tabRect.Y, Style.HoverRight.Width, 25);

                        spriteBatch.Draw(Style.HoverMid, t.tabRect, Color.White);
                        spriteBatch.Draw(Style.HoverRight, right, Color.White);
                        if (Tabs.Count - 1 > i)
                        {
                            SubTexture tex = Tabs[i + 1].Selected ? Style.RightExt :
                                             Tabs[i + 1].Hover    ? Style.HoverLeft :
                                                                    Style.RightExtUnsel;
                            spriteBatch.Draw(tex, right, Color.White);
                        }
                    }
                    var textPos = new Vector2(t.tabRect.X, (t.tabRect.Y + t.tabRect.Height / 2 - toUse.LineSpacing / 2));
                    spriteBatch.DrawString(toUse, t.Title, textPos, new Color(255, 239, 208));
                }
            }
            spriteBatch.Draw(Style.HorizVert, topHoriz, Color.White);
            spriteBatch.Draw(Style.CornerTR,  TR, Color.White);
            spriteBatch.Draw(Style.HorizVert, botHoriz, Color.White);
            spriteBatch.Draw(Style.CornerBR,  BR, Color.White);
            spriteBatch.Draw(Style.CornerBL,  BL, Color.White);
            spriteBatch.Draw(Style.HorizVert, VR, Color.White);
            spriteBatch.Draw(Style.HorizVert, VL, Color.White);
        }

        /// TODO: there are 3 pretty much identical functions here... what the hell??
        public void HandleInput(IListScreen caller)
        {
            for (int i = 0; i < Tabs.Count; i++)
            {
                Tab tab = Tabs[i];
                if (!tab.tabRect.HitTest(Input.CursorPosition))
                {
                    tab.Hover = false;
                    continue;
                }
                tab.Hover = true;
                if (Input.LeftMouseClick)
                {
                    GameAudio.PlaySfxAsync("sd_ui_accept_alt3");
                    tab.Selected = true;
                    foreach (Tab otherTab in Tabs)
                        if (otherTab != tab) otherTab.Selected = false;

                    caller.ResetLists();
                }
            }
        }
        public virtual bool HandleInput(InputState input)
        {
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
                        GameAudio.PlaySfxAsync("sd_ui_accept_alt3");
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
        public void HandleInputNoReset()
        {
            foreach (Tab tab in Tabs)
            {
                if (!tab.tabRect.HitTest(Input.CursorPosition))
                {
                    tab.Hover = false;
                    continue;
                }

                tab.Hover = true;
                if (Input.LeftMouseClick)
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