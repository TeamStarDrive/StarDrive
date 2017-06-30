using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using System;
using System.Collections.Generic;

namespace Ship_Game
{
    public class Submenu
    {
        public Rectangle Menu;
        private readonly ScreenManager ScreenManager;
        public Array<Tab> Tabs = new Array<Tab>();
        public bool LowRes;

        private Rectangle UpperLeft;
        private readonly Rectangle TR;
        private Rectangle topHoriz;
        private Rectangle botHoriz;
        private Rectangle BL;
        private Rectangle BR;
        private Rectangle VL;
        private Rectangle VR;
        private Rectangle TL;

        private SpriteFont toUse;
        private bool Blue;

        private InputState Input;

        public Submenu(Rectangle theMenu)
        {
            ScreenManager = Game1.Instance.ScreenManager;
            Input = ScreenManager.input;
            if (Game1.Instance.RenderWidth <= 1280)
            {
                this.LowRes = true;
            }
            if (!this.LowRes)
            {
                this.toUse = Fonts.Arial12Bold;
            }
            else
            {
                this.toUse = Fonts.Arial12Bold;
            }
            this.toUse = Fonts.Pirulen12;
            this.Menu = theMenu;
            this.UpperLeft = new Rectangle(theMenu.X, theMenu.Y, ResourceManager.TextureDict["NewUI/submenu_header_left"].Width, ResourceManager.TextureDict["NewUI/submenu_header_left"].Height);
            this.TL = new Rectangle(theMenu.X, theMenu.Y + 25 - 2, ResourceManager.TextureDict["NewUI/submenu_corner_TL"].Width, ResourceManager.TextureDict["NewUI/submenu_corner_TL"].Height);
            this.TR = new Rectangle(theMenu.X + theMenu.Width - ResourceManager.TextureDict["NewUI/submenu_corner_TR"].Width, theMenu.Y + 25 - 2, ResourceManager.TextureDict["NewUI/submenu_corner_TR"].Width, ResourceManager.TextureDict["NewUI/submenu_corner_TR"].Height);
            this.BL = new Rectangle(theMenu.X, theMenu.Y + theMenu.Height - ResourceManager.TextureDict["NewUI/submenu_corner_BL"].Height + 2, ResourceManager.TextureDict["NewUI/submenu_corner_BL"].Width, ResourceManager.TextureDict["NewUI/submenu_corner_BL"].Height);
            this.BR = new Rectangle(theMenu.X + theMenu.Width - ResourceManager.TextureDict["NewUI/submenu_corner_BR"].Width, theMenu.Y + theMenu.Height + 2 - ResourceManager.TextureDict["NewUI/submenu_corner_BR"].Height, ResourceManager.TextureDict["NewUI/submenu_corner_BR"].Width, ResourceManager.TextureDict["NewUI/submenu_corner_BR"].Height);
            this.topHoriz = new Rectangle(theMenu.X + ResourceManager.TextureDict["NewUI/submenu_corner_TL"].Width, theMenu.Y + 25 - 2, theMenu.Width - this.TR.Width - ResourceManager.TextureDict["NewUI/submenu_corner_TL"].Width, 2);
            this.botHoriz = new Rectangle(theMenu.X + this.BL.Width, theMenu.Y + theMenu.Height, theMenu.Width - this.BL.Width - this.BR.Width, 2);
            this.VL = new Rectangle(theMenu.X, theMenu.Y + 25 + this.TR.Height - 2, 2, theMenu.Height - 25 - this.BL.Height - 2);
            this.VR = new Rectangle(theMenu.X + theMenu.Width - 2, theMenu.Y + 25 + this.TR.Height - 2, 2, theMenu.Height - 25 - this.BR.Height - 2);
        }

        public Submenu(bool blue, Rectangle theMenu)
        {
            Blue = blue;
            ScreenManager = Game1.Instance.ScreenManager;
            if (Game1.Instance.RenderWidth <= 1280)
            {
                LowRes = true;
            }

            this.toUse = Fonts.Pirulen12;
            this.Menu = theMenu;
            this.UpperLeft = new Rectangle(theMenu.X, theMenu.Y, ResourceManager.TextureDict["ResearchMenu/submenu_header_left"].Width, ResourceManager.TextureDict["ResearchMenu/submenu_header_left"].Height);
            this.TL = new Rectangle(theMenu.X, theMenu.Y + 25 - 2, ResourceManager.TextureDict["ResearchMenu/submenu_corner_TL"].Width, ResourceManager.TextureDict["ResearchMenu/submenu_corner_TL"].Height);
            this.TR = new Rectangle(theMenu.X + theMenu.Width - ResourceManager.TextureDict["ResearchMenu/submenu_corner_TR"].Width, theMenu.Y + 25 - 2, ResourceManager.TextureDict["ResearchMenu/submenu_corner_TR"].Width, ResourceManager.TextureDict["ResearchMenu/submenu_corner_TR"].Height);
            this.BL = new Rectangle(theMenu.X, theMenu.Y + theMenu.Height - ResourceManager.TextureDict["ResearchMenu/submenu_corner_BL"].Height + 2, ResourceManager.TextureDict["ResearchMenu/submenu_corner_BL"].Width, ResourceManager.TextureDict["ResearchMenu/submenu_corner_BL"].Height);
            this.BR = new Rectangle(theMenu.X + theMenu.Width - ResourceManager.TextureDict["ResearchMenu/submenu_corner_BR"].Width, theMenu.Y + theMenu.Height + 2 - ResourceManager.TextureDict["ResearchMenu/submenu_corner_BR"].Height, ResourceManager.TextureDict["ResearchMenu/submenu_corner_BR"].Width, ResourceManager.TextureDict["ResearchMenu/submenu_corner_BR"].Height);
            this.topHoriz = new Rectangle(theMenu.X + ResourceManager.TextureDict["ResearchMenu/submenu_corner_TL"].Width, theMenu.Y + 25 - 2, theMenu.Width - this.TR.Width - ResourceManager.TextureDict["ResearchMenu/submenu_corner_TL"].Width, 2);
            this.botHoriz = new Rectangle(theMenu.X + this.BL.Width, theMenu.Y + theMenu.Height, theMenu.Width - this.BL.Width - this.BR.Width, 2);
            this.VL = new Rectangle(theMenu.X, theMenu.Y + 25 + this.TR.Height - 2, 2, theMenu.Height - 25 - this.BL.Height - 2);
            this.VR = new Rectangle(theMenu.X + theMenu.Width - 2, theMenu.Y + 25 + this.TR.Height - 2, 2, theMenu.Height - 25 - this.BR.Height - 2);
        }

        public Submenu(Rectangle theMenu, bool LowRes)
        {
            ScreenManager = Game1.Instance.ScreenManager;
            this.LowRes = LowRes;
            if (Game1.Instance.RenderWidth <= 1280)
            {
                LowRes = true;
            }
            if (!LowRes)
            {
                this.toUse = Fonts.Arial12Bold;
            }
            else
            {
                this.toUse = Fonts.Arial12Bold;
            }
            this.toUse = Fonts.Pirulen12;
            this.Menu = theMenu;
            this.UpperLeft = new Rectangle(theMenu.X, theMenu.Y, ResourceManager.TextureDict["NewUI/submenu_header_left"].Width, ResourceManager.TextureDict["NewUI/submenu_header_left"].Height);
            this.TL = new Rectangle(theMenu.X, theMenu.Y + 25 - 2, ResourceManager.TextureDict["NewUI/submenu_corner_TL"].Width, ResourceManager.TextureDict["NewUI/submenu_corner_TL"].Height);
            this.TR = new Rectangle(theMenu.X + theMenu.Width - ResourceManager.TextureDict["NewUI/submenu_corner_TR"].Width, theMenu.Y + 25 - 2, ResourceManager.TextureDict["NewUI/submenu_corner_TR"].Width, ResourceManager.TextureDict["NewUI/submenu_corner_TR"].Height);
            this.BL = new Rectangle(theMenu.X, theMenu.Y + theMenu.Height - ResourceManager.TextureDict["NewUI/submenu_corner_BL"].Height + 2, ResourceManager.TextureDict["NewUI/submenu_corner_BL"].Width, ResourceManager.TextureDict["NewUI/submenu_corner_BL"].Height);
            this.BR = new Rectangle(theMenu.X + theMenu.Width - ResourceManager.TextureDict["NewUI/submenu_corner_BR"].Width, theMenu.Y + theMenu.Height + 2 - ResourceManager.TextureDict["NewUI/submenu_corner_BR"].Height, ResourceManager.TextureDict["NewUI/submenu_corner_BR"].Width, ResourceManager.TextureDict["NewUI/submenu_corner_BR"].Height);
            this.topHoriz = new Rectangle(theMenu.X + ResourceManager.TextureDict["NewUI/submenu_corner_TL"].Width, theMenu.Y + 25 - 2, theMenu.Width - this.TR.Width - ResourceManager.TextureDict["NewUI/submenu_corner_TL"].Width, 2);
            this.botHoriz = new Rectangle(theMenu.X + this.BL.Width, theMenu.Y + theMenu.Height, theMenu.Width - this.BL.Width - this.BR.Width, 2);
            this.VL = new Rectangle(theMenu.X, theMenu.Y + 25 + this.TR.Height - 2, 2, theMenu.Height - 25 - this.BL.Height - 2);
            this.VR = new Rectangle(theMenu.X + theMenu.Width - 2, theMenu.Y + 25 + this.TR.Height - 2, 2, theMenu.Height - 25 - this.BR.Height - 2);
        }

        public void AddTab(string title)
        {
            int w = (int)this.toUse.MeasureString(title).X;
            float tabX = (float)(this.UpperLeft.X + this.UpperLeft.Width);
            foreach (Submenu.Tab ta in this.Tabs)
            {
                tabX = tabX + (float)ta.tabRect.Width;
                tabX = tabX + (float)ResourceManager.TextureDict["NewUI/submenu_header_right"].Width;
            }
            Rectangle tabRect = new Rectangle((int)tabX, this.UpperLeft.Y, w + 2, 25);
            Submenu.Tab t = new Submenu.Tab()
            {
                tabRect = tabRect,
                Title = title
            };
            if (this.Tabs.Count != 0)
            {
                t.Selected = false;
            }
            else
            {
                t.Selected = true;
            }
            t.Hover = false;
            this.Tabs.Add(t);
        }

        public void Draw()
        {
            if (!this.Blue)
            {
                this.ScreenManager.SpriteBatch.Draw(ResourceManager.TextureDict["NewUI/submenu_corner_TL"], this.TL, Color.White);
                if (this.Tabs.Count > 0 && this.Tabs[0].Selected)
                {
                    this.ScreenManager.SpriteBatch.Draw(ResourceManager.TextureDict["NewUI/submenu_header_left"], this.UpperLeft, Color.White);
                }
                else if (this.Tabs.Count > 0 && !this.Tabs[0].Hover)
                {
                    this.ScreenManager.SpriteBatch.Draw(ResourceManager.TextureDict["NewUI/submenu_header_left_unsel"], this.UpperLeft, Color.White);
                }
                else if (this.Tabs.Count > 0 && this.Tabs[0].Hover)
                {
                    this.ScreenManager.SpriteBatch.Draw(ResourceManager.TextureDict["NewUI/submenu_header_hover_leftedge"], this.UpperLeft, Color.White);
                }
                if (this.Tabs.Count == 1)
                {
                    foreach (Submenu.Tab t in this.Tabs)
                    {
                        this.ScreenManager.SpriteBatch.Draw(ResourceManager.TextureDict["NewUI/submenu_header_middle"], t.tabRect, Color.White);
                        Rectangle right = new Rectangle(t.tabRect.X + t.tabRect.Width, t.tabRect.Y, ResourceManager.TextureDict["NewUI/submenu_header_right"].Width, 25);
                        this.ScreenManager.SpriteBatch.Draw(ResourceManager.TextureDict["NewUI/submenu_header_right"], right, Color.White);
                        Vector2 textPos = new Vector2((float)t.tabRect.X, (float)(t.tabRect.Y + t.tabRect.Height / 2 - this.toUse.LineSpacing / 2));
                        this.ScreenManager.SpriteBatch.DrawString(this.toUse, t.Title, textPos, new Color(255, 239, 208));
                    }
                }
                else if (this.Tabs.Count > 1)
                {
                    for (int i = 0; i < this.Tabs.Count; i++)
                    {
                        Submenu.Tab t = this.Tabs[i];
                        if (t.Selected)
                        {
                            this.ScreenManager.SpriteBatch.Draw(ResourceManager.TextureDict["NewUI/submenu_header_middle"], t.tabRect, Color.White);
                            Rectangle right = new Rectangle(t.tabRect.X + t.tabRect.Width, t.tabRect.Y, ResourceManager.TextureDict["NewUI/submenu_header_right"].Width, 25);
                            this.ScreenManager.SpriteBatch.Draw(ResourceManager.TextureDict["NewUI/submenu_header_right"], right, Color.White);
                            if (this.Tabs.Count - 1 > i && !this.Tabs[i + 1].Selected)
                            {
                                if (this.Tabs[i + 1].Hover)
                                {
                                    this.ScreenManager.SpriteBatch.Draw(ResourceManager.TextureDict["NewUI/submenu_header_hover_left"], right, Color.White);
                                }
                                else
                                {
                                    this.ScreenManager.SpriteBatch.Draw(ResourceManager.TextureDict["NewUI/submenu_header_rightextend_unsel"], right, Color.White);
                                }
                            }
                        }
                        else if (!t.Hover)
                        {
                            this.ScreenManager.SpriteBatch.Draw(ResourceManager.TextureDict["NewUI/submenu_header_middle_unsel"], t.tabRect, Color.White);
                            Rectangle right = new Rectangle(t.tabRect.X + t.tabRect.Width, t.tabRect.Y, ResourceManager.TextureDict["NewUI/submenu_header_right_unsel"].Width, 25);
                            this.ScreenManager.SpriteBatch.Draw(ResourceManager.TextureDict["NewUI/submenu_header_right_unsel"], right, Color.White);
                            if (this.Tabs.Count - 1 > i)
                            {
                                if (this.Tabs[i + 1].Selected)
                                {
                                    this.ScreenManager.SpriteBatch.Draw(ResourceManager.TextureDict["NewUI/submenu_header_rightextend"], right, Color.White);
                                }
                                else if (this.Tabs[i + 1].Hover)
                                {
                                    this.ScreenManager.SpriteBatch.Draw(ResourceManager.TextureDict["NewUI/submenu_header_hover_left"], right, Color.White);
                                }
                                else
                                {
                                    this.ScreenManager.SpriteBatch.Draw(ResourceManager.TextureDict["NewUI/submenu_header_rightextend_unsel"], right, Color.White);
                                }
                            }
                        }
                        else
                        {
                            this.ScreenManager.SpriteBatch.Draw(ResourceManager.TextureDict["NewUI/submenu_header_hover_mid"], t.tabRect, Color.White);
                            Rectangle right = new Rectangle(t.tabRect.X + t.tabRect.Width, t.tabRect.Y, ResourceManager.TextureDict["NewUI/submenu_header_hover_right"].Width, 25);
                            this.ScreenManager.SpriteBatch.Draw(ResourceManager.TextureDict["NewUI/submenu_header_hover_right"], right, Color.White);
                            if (this.Tabs.Count - 1 > i)
                            {
                                if (this.Tabs[i + 1].Selected)
                                {
                                    this.ScreenManager.SpriteBatch.Draw(ResourceManager.TextureDict["NewUI/submenu_header_rightextend"], right, Color.White);
                                }
                                else if (this.Tabs[i + 1].Hover)
                                {
                                    this.ScreenManager.SpriteBatch.Draw(ResourceManager.TextureDict["NewUI/submenu_header_hover_left"], right, Color.White);
                                }
                                else
                                {
                                    this.ScreenManager.SpriteBatch.Draw(ResourceManager.TextureDict["NewUI/submenu_header_rightextend_unsel"], right, Color.White);
                                }
                            }
                        }
                        Vector2 textPos = new Vector2((float)t.tabRect.X, (float)(t.tabRect.Y + t.tabRect.Height / 2 - this.toUse.LineSpacing / 2));
                        this.ScreenManager.SpriteBatch.DrawString(this.toUse, t.Title, textPos, new Color(255, 239, 208));
                    }
                }
                this.ScreenManager.SpriteBatch.Draw(ResourceManager.TextureDict["NewUI/submenu_horiz_vert"], this.topHoriz, Color.White);
                this.ScreenManager.SpriteBatch.Draw(ResourceManager.TextureDict["NewUI/submenu_corner_TR"], this.TR, Color.White);
                this.ScreenManager.SpriteBatch.Draw(ResourceManager.TextureDict["NewUI/submenu_horiz_vert"], this.botHoriz, Color.White);
                this.ScreenManager.SpriteBatch.Draw(ResourceManager.TextureDict["NewUI/submenu_corner_BR"], this.BR, Color.White);
                this.ScreenManager.SpriteBatch.Draw(ResourceManager.TextureDict["NewUI/submenu_corner_BL"], this.BL, Color.White);
                this.ScreenManager.SpriteBatch.Draw(ResourceManager.TextureDict["NewUI/submenu_horiz_vert"], this.VR, Color.White);
                this.ScreenManager.SpriteBatch.Draw(ResourceManager.TextureDict["NewUI/submenu_horiz_vert"], this.VL, Color.White);
                return;
            }
            if (this.Blue)
            {
                this.ScreenManager.SpriteBatch.Draw(ResourceManager.TextureDict["ResearchMenu/submenu_corner_TL"], this.TL, Color.White);
                if (this.Tabs.Count > 0 && this.Tabs[0].Selected)
                {
                    this.ScreenManager.SpriteBatch.Draw(ResourceManager.TextureDict["ResearchMenu/submenu_header_left"], this.UpperLeft, Color.White);
                }
                else if (this.Tabs.Count > 0 && !this.Tabs[0].Hover)
                {
                    this.ScreenManager.SpriteBatch.Draw(ResourceManager.TextureDict["ResearchMenu/submenu_header_left_unsel"], this.UpperLeft, Color.White);
                }
                else if (this.Tabs.Count > 0 && this.Tabs[0].Hover)
                {
                    this.ScreenManager.SpriteBatch.Draw(ResourceManager.TextureDict["ResearchMenu/submenu_header_hover_leftedge"], this.UpperLeft, Color.White);
                }
                if (this.Tabs.Count == 1)
                {
                    foreach (Submenu.Tab t in this.Tabs)
                    {
                        this.ScreenManager.SpriteBatch.Draw(ResourceManager.TextureDict["ResearchMenu/submenu_header_middle"], t.tabRect, Color.White);
                        Rectangle right = new Rectangle(t.tabRect.X + t.tabRect.Width, t.tabRect.Y, ResourceManager.TextureDict["ResearchMenu/submenu_header_right"].Width, 25);
                        this.ScreenManager.SpriteBatch.Draw(ResourceManager.TextureDict["ResearchMenu/submenu_header_right"], right, Color.White);
                        Vector2 textPos = new Vector2((float)t.tabRect.X, (float)(t.tabRect.Y + t.tabRect.Height / 2 - this.toUse.LineSpacing / 2));
                        this.ScreenManager.SpriteBatch.DrawString(this.toUse, t.Title, textPos, new Color(255, 239, 208));
                    }
                }
                else if (this.Tabs.Count > 1)
                {
                    for (int i = 0; i < this.Tabs.Count; i++)
                    {
                        Submenu.Tab t = this.Tabs[i];
                        if (t.Selected)
                        {
                            this.ScreenManager.SpriteBatch.Draw(ResourceManager.TextureDict["ResearchMenu/submenu_header_middle"], t.tabRect, Color.White);
                            Rectangle right = new Rectangle(t.tabRect.X + t.tabRect.Width, t.tabRect.Y, ResourceManager.TextureDict["ResearchMenu/submenu_header_right"].Width, 25);
                            this.ScreenManager.SpriteBatch.Draw(ResourceManager.TextureDict["ResearchMenu/submenu_header_right"], right, Color.White);
                            if (this.Tabs.Count - 1 > i && !this.Tabs[i + 1].Selected)
                            {
                                if (this.Tabs[i + 1].Hover)
                                {
                                    this.ScreenManager.SpriteBatch.Draw(ResourceManager.TextureDict["ResearchMenu/submenu_header_hover_left"], right, Color.White);
                                }
                                else
                                {
                                    this.ScreenManager.SpriteBatch.Draw(ResourceManager.TextureDict["ResearchMenu/submenu_header_rightextend_unsel"], right, Color.White);
                                }
                            }
                        }
                        else if (!t.Hover)
                        {
                            this.ScreenManager.SpriteBatch.Draw(ResourceManager.TextureDict["ResearchMenu/submenu_header_middle_unsel"], t.tabRect, Color.White);
                            Rectangle right = new Rectangle(t.tabRect.X + t.tabRect.Width, t.tabRect.Y, ResourceManager.TextureDict["ResearchMenu/submenu_header_right_unsel"].Width, 25);
                            this.ScreenManager.SpriteBatch.Draw(ResourceManager.TextureDict["ResearchMenu/submenu_header_right_unsel"], right, Color.White);
                            if (this.Tabs.Count - 1 > i)
                            {
                                if (this.Tabs[i + 1].Selected)
                                {
                                    this.ScreenManager.SpriteBatch.Draw(ResourceManager.TextureDict["ResearchMenu/submenu_header_rightextend"], right, Color.White);
                                }
                                else if (this.Tabs[i + 1].Hover)
                                {
                                    this.ScreenManager.SpriteBatch.Draw(ResourceManager.TextureDict["ResearchMenu/submenu_header_hover_left"], right, Color.White);
                                }
                                else
                                {
                                    this.ScreenManager.SpriteBatch.Draw(ResourceManager.TextureDict["ResearchMenu/submenu_header_rightextend_unsel"], right, Color.White);
                                }
                            }
                        }
                        else
                        {
                            this.ScreenManager.SpriteBatch.Draw(ResourceManager.TextureDict["ResearchMenu/submenu_header_hover_mid"], t.tabRect, Color.White);
                            Rectangle right = new Rectangle(t.tabRect.X + t.tabRect.Width, t.tabRect.Y, ResourceManager.TextureDict["ResearchMenu/submenu_header_hover_right"].Width, 25);
                            this.ScreenManager.SpriteBatch.Draw(ResourceManager.TextureDict["ResearchMenu/submenu_header_hover_right"], right, Color.White);
                            if (this.Tabs.Count - 1 > i)
                            {
                                if (this.Tabs[i + 1].Selected)
                                {
                                    this.ScreenManager.SpriteBatch.Draw(ResourceManager.TextureDict["ResearchMenu/submenu_header_rightextend"], right, Color.White);
                                }
                                else if (this.Tabs[i + 1].Hover)
                                {
                                    this.ScreenManager.SpriteBatch.Draw(ResourceManager.TextureDict["ResearchMenu/submenu_header_hover_left"], right, Color.White);
                                }
                                else
                                {
                                    this.ScreenManager.SpriteBatch.Draw(ResourceManager.TextureDict["ResearchMenu/submenu_header_rightextend_unsel"], right, Color.White);
                                }
                            }
                        }
                        Vector2 textPos = new Vector2((float)t.tabRect.X, (float)(t.tabRect.Y + t.tabRect.Height / 2 - this.toUse.LineSpacing / 2));
                        this.ScreenManager.SpriteBatch.DrawString(this.toUse, t.Title, textPos, new Color(255, 239, 208));
                    }
                }
                this.ScreenManager.SpriteBatch.Draw(ResourceManager.TextureDict["ResearchMenu/submenu_horiz_vert"], this.topHoriz, Color.White);
                this.ScreenManager.SpriteBatch.Draw(ResourceManager.TextureDict["ResearchMenu/submenu_corner_TR"], this.TR, Color.White);
                this.ScreenManager.SpriteBatch.Draw(ResourceManager.TextureDict["ResearchMenu/submenu_horiz_vert"], this.botHoriz, Color.White);
                this.ScreenManager.SpriteBatch.Draw(ResourceManager.TextureDict["ResearchMenu/submenu_corner_BR"], this.BR, Color.White);
                this.ScreenManager.SpriteBatch.Draw(ResourceManager.TextureDict["ResearchMenu/submenu_corner_BL"], this.BL, Color.White);
                this.ScreenManager.SpriteBatch.Draw(ResourceManager.TextureDict["ResearchMenu/submenu_horiz_vert"], this.VR, Color.White);
                this.ScreenManager.SpriteBatch.Draw(ResourceManager.TextureDict["ResearchMenu/submenu_horiz_vert"], this.VL, Color.White);
            }
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