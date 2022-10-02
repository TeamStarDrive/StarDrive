using System;
using Microsoft.Xna.Framework.Graphics;
using SDGraphics;
using SDUtils;
using Ship_Game.Audio;
using Vector2 = SDGraphics.Vector2;
using Rectangle = SDGraphics.Rectangle;
using Ship_Game.UI;
using static System.Net.Mime.MediaTypeNames;

namespace Ship_Game
{
    public enum SubmenuStyle
    {
        Brown,
        Blue,
    }

    public class Submenu : UIPanel
    {
        public class Tab
        {
            public int Index;
            public string Title;
            public RectF Rect;
            public bool Selected;
            public bool Hover;

            internal bool RowStart; // this tab is the first in this row
            internal bool RowEnd; // this tab is the last in this row

            public override string ToString() => $"Tab {Index} {Title} Sel={Selected} Hov={Hover}";
        }

        Rectangle UpperLeft; // inner top left corner
        Rectangle UpperRight; // inner top right corner

        Rectangle TR;
        Rectangle TopHoriz;
        Rectangle BotHoriz;
        Rectangle BL;
        Rectangle BR;
        Rectangle VL;
        Rectangle VR;
        Rectangle TL;
        readonly Graphics.Font Font = Fonts.Pirulen12;
        readonly SubmenuStyle Style;

        // If set, draws a background element before the Submenu itself is drawn
        public UIElementV2 Background;
        
        // EVT: Triggered when a tab is changed
        public Action<int> OnTabChange;
        int CurSelectedIndex = -1;

        public Submenu(in Rectangle theMenu, SubmenuStyle style = SubmenuStyle.Brown)
            : base(theMenu, Color.TransparentBlack)
        {
            Style = style;
            this.PerformLayout();
        }

        public Submenu(int x, int y, int width, int height, SubmenuStyle style = SubmenuStyle.Brown)
            : this(new(x, y, width, height), style)
        {
        }

        public Submenu(float x, float y, float width, float height, SubmenuStyle style = SubmenuStyle.Brown)
            : this(new((int)x, (int)y, (int)width, (int)height), style)
        {
        }

        public Submenu(LocalPos pos, Vector2 size, SubmenuStyle style = SubmenuStyle.Brown)
            : base(pos, size, Color.TransparentBlack)
        {
            Style = style;
        }

        public void SetBackground(Color color)
        {
            Background?.RemoveFromParent();
            Background = new Selector(this, new LocalPos(0,2), new Vector2(Size.X,Size.Y-2), color);
        }

        public override void PerformLayout()
        {
            base.PerformLayout();
            Background?.PerformLayout();

            StyleTextures s = GetStyle();
            Rectangle r = Rect;

            int o = r.Height < TabHeight ? 0 : TabHeight;
            TL = new(r.X, r.Y + o - 2, s.CornerTL.Width, s.CornerTL.Height);
            TR = new(r.Right - s.CornerTR.Width, r.Y + o - 2, s.CornerTR.Width, s.CornerTR.Height);
            BL = new(r.X, r.Bottom - s.CornerBL.Height + 2, s.CornerBL.Width, s.CornerBL.Height);
            BR = new(r.Right - s.CornerBR.Width, r.Bottom - s.CornerBR.Height + 2, s.CornerBR.Width, s.CornerBR.Height);
            VL = new(r.X, r.Y + o + TR.Height - 2, 2, r.Height - o - BL.Height - 2);
            VR = new(r.Right - 2, r.Y + o + TR.Height - 2, 2, r.Height - o - BR.Height - 2);

            UpperLeft = new(r.X, r.Y, s.Left.Width, s.Left.Height);
            UpperRight = new(r.Right-2, r.Y, 2, s.Right.Height);
            TopHoriz = new(r.X + TL.Width, r.Y + o - 2, r.Width - TR.Width - TL.Width, 2);
            BotHoriz = new(r.X + BL.Width, r.Bottom, r.Width - BL.Width - BR.Width, 2);

            RecalculateTabRects();
        }
        
        public Array<Tab> Tabs = new();
        int TabRows;
        Vector2 NextTabPos; // relative position for next tab
        const int TabHeight = 25;

        public void Clear()
        {
            Tabs.Clear();
            CurSelectedIndex = -1; // don't trigger event during Clear()
            RecalculateTabRects();
        }

        public int NumTabs => Tabs.Count;

        int IndexOf(string title) => Tabs.FirstIndexOf(tab => tab.Title == title);
        public bool IsSelected(string title) => SelectedIndex != -1 && IndexOf(title) == SelectedIndex;
        public bool ContainsTab(string title) => IndexOf(title) != -1;

        public void AddTab(LocalizedText title)
        {
            Tab tab = new() { Index = Tabs.Count, Title = title.Text };
            UpdateTabRect(tab);
            Tabs.Add(tab);
        }

        void RecalculateTabRects()
        {
            NextTabPos = Vector2.Zero;
            TabRows = 0;
            foreach (Tab tab in Tabs)
                UpdateTabRect(tab);
        }
        
        void UpdateTabRect(Tab tab)
        {
            float w = Font.TextWidth(tab.Title) + 2 + GetStyle().Right.Width;

            // new rect will go over upper right edge? line change
            if (UpperRight.X < (UpperLeft.Right + NextTabPos.X + w))
            {
                NextTabPos.X = 0f;
                NextTabPos.Y += TabHeight - 2;
                ++TabRows;
            }
            else
            {
                // if line didn't change, always clear the last tabs end of row flag
                if (tab.Index > 0) Tabs[tab.Index - 1].RowEnd = false;
                if (TabRows == 0) ++TabRows;
            }

            tab.RowEnd = true; // always mark the last element as end of row
            tab.RowStart = NextTabPos.X == 0f;
            Vector2 newPos = new(UpperLeft.Right + NextTabPos.X, UpperLeft.Y + NextTabPos.Y);
            NextTabPos.X += w;

            tab.Rect = new(newPos, new(w, TabHeight));
        }

        protected virtual void OnTabChangedEvt(int newIndex)
        {
            if (CurSelectedIndex != newIndex)
            {
                CurSelectedIndex = newIndex;
                OnTabChange?.Invoke(newIndex);
            }
        }

        public int SelectedIndex
        {
            get => CurSelectedIndex;
            set
            {
                int newIndex = -1;
                for (int i = 0; i < Tabs.Count; ++i)
                {
                    Tab tab = Tabs[i];
                    tab.Selected = (i == value);
                    if (tab.Selected) newIndex = i;
                }
                OnTabChangedEvt(newIndex);
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
                tab.Hover = tab.Rect.HitTest(mousePos);
                if (tab.Hover && input.LeftMouseClick)
                {
                    GameAudio.AcceptClick();
                    SelectedIndex = i;
                    return true;
                }
            }

            return base.HandleInput(input);
        }

        public override void Update(float fixedDeltaTime)
        {
            if (SelectedIndex == -1 && Tabs.NotEmpty)
                SelectedIndex = 0;

            Background?.Update(fixedDeltaTime);
            base.Update(fixedDeltaTime);
        }

        void DrawMenuBarTopLeft(SpriteBatch batch, Tab t, StyleTextures s)
        {
            bool selected = Tabs.Count == 1 || t.Selected;
            SubTexture header = s.LeftUnsel;
            if     (selected) header = s.Left;
            else if (t.Hover) header = s.HoverLeftEdge;

            batch.Draw(header, new RectF(UpperLeft.X, t.Rect.Y, UpperLeft.Size()), Color.White);
        }

        public override void Draw(SpriteBatch batch, DrawTimes elapsed)
        {
            if (!Visible)
                return;
            
            Background?.Draw(batch, elapsed);
            base.Draw(batch, elapsed);

            StyleTextures s = GetStyle();
            
            if (Tabs.Count == 1)
            {
                DrawMenuBarTopLeft(batch, Tabs[0], s);
                foreach (Tab t in Tabs)
                {
                    RectF r = t.Rect;
                    var right = new RectF(r.X + r.W, r.Y, s.Right.Width, 25);
                    var textPos = new Vector2(r.X, (r.Y + r.H / 2 - Font.LineSpacing / 2));

                    batch.Draw(s.Middle, r, Color.White);
                    batch.Draw(s.Right, right, Color.White);
                    batch.DrawString(Font, t.Title, textPos, Colors.Cream);
                }
            }
            else if (Tabs.NotEmpty)
            {
                for (int i = 0; i < Tabs.Count; i++)
                {
                    Tab t = Tabs[i];
                    Tab nextTab = i < (Tabs.Count - 1) ? Tabs[i + 1] : null;
                    RectF r = t.Rect;

                    if (t.RowStart)
                        DrawMenuBarTopLeft(batch, t, s);

                    // middle part of a tab
                    var middle = new RectF(r.X, r.Y, r.W - s.Right.Width, TabHeight);
                    // right side of a tab
                    var right = new RectF(r.X + r.W - s.Right.Width, r.Y, s.Right.Width, TabHeight);

                    if (t.Selected)
                    {
                        batch.Draw(s.Middle, middle, Color.White);
                        batch.Draw(s.Right, right, Color.White);

                        if (!t.RowEnd && nextTab != null)
                        {
                            SubTexture tab = nextTab.Hover ? s.HoverLeft : s.RightExtUnsel;
                            batch.Draw(tab, right, Color.White);
                        }
                    }
                    else if (t.Hover)
                    {
                        batch.Draw(s.HoverMid, middle, Color.White);
                        batch.Draw(s.HoverRight, right, Color.White);
                        if (!t.RowEnd && nextTab != null) // we have nextTab, draw |_\
                        {
                            SubTexture tex = nextTab.Selected ? s.RightExt : s.RightExtUnsel;
                            batch.Draw(tex, right, Color.White);
                        }
                    }
                    else // unselected
                    {
                        batch.Draw(s.MiddleUnsel, middle, Color.White);
                        batch.Draw(s.RightUnsel, right, Color.White);
                        if (!t.RowEnd && nextTab != null)
                        {
                            SubTexture tex = nextTab.Selected ? s.RightExt :
                                             nextTab.Hover    ? s.HoverLeft :
                                                                s.RightExtUnsel;
                            batch.Draw(tex, right, Color.White);
                        }
                    }

                    var textPos = new Vector2(r.X, (r.Y + r.H / 2 - Font.LineSpacing / 2));
                    batch.DrawString(Font, t.Title, textPos, Colors.Cream);

                    if (DebugDraw) // debugging:
                    {
                        batch.DrawRectangle(r, Color.Red);
                        batch.DrawRectangle(right, t.RowEnd ? Color.Yellow : Color.LightBlue);
                        batch.DrawRectangle(UpperLeft, Color.Green);
                        batch.DrawRectangle(UpperRight, Color.Green);
                    }
                }
            }


            // if we have tabs, draw horizontal bars for every row
            if (Tabs.NotEmpty)
            {
                float topY = TopHoriz.Y - (TabHeight - 2);
                for (int i = 0; i < (TabRows+1); ++i)
                {
                    DrawTopBar(topY + i*(TabHeight-2));
                }
                DrawVerticalBars(topY);
            }
            else // only draw the border
            {
                DrawTopBar(TopHoriz.Y);
                DrawVerticalBars(TopHoriz.Y);
            }

            // bottom corners L
            batch.Draw(s.CornerBR,  BR, Color.White);
            batch.Draw(s.CornerBL,  BL, Color.White);

            // bottom horizontal --- 
            batch.Draw(s.HorizVert, BotHoriz, Color.White);

            // debugging
            //batch.DrawRectangle(Rect, Color.Red);
            
            void DrawTopBar(float barY)
            {
                batch.Draw(s.HorizVert, new RectF(TopHoriz.X, barY, TopHoriz.Width, 2), Color.White);
                batch.Draw(s.CornerTL, new RectF(TL.X, barY, TL.Width, TL.Height), Color.White);
                batch.Draw(s.CornerTR, new RectF(TR.X, barY, TR.Width, TR.Height), Color.White);
            }

            void DrawVerticalBars(float topY)
            {
                // vertical left & right |
                float height = (BL.Y - topY) - TL.Height;
                batch.Draw(s.HorizVert, new RectF(VR.X, topY+TL.Height, VR.Width, height), Color.White);
                batch.Draw(s.HorizVert, new RectF(VL.X, topY+TL.Height, VL.Width, height), Color.White);
            }
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

    }
}