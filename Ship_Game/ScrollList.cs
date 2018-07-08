using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using System;
using System.Collections.Generic;

namespace Ship_Game
{
    public class ScrollList : IDisposable
    {
        private Submenu Parent;
        public Rectangle ScrollUp;
        public Rectangle ScrollDown;
        public Rectangle ScrollBarHousing;
        public Rectangle ScrollBar;

        private int entryHeight = 40;
        private int ScrollBarHover;
        private int startDragPos;
        private bool dragging;
        private float ScrollBarStartDragPos;
        private float ClickTimer;
        private float TimerDelay = 0.05f;

        public int entriesToDisplay;
        public int indexAtTop;
        public BatchRemovalCollection<Entry> Entries = new BatchRemovalCollection<Entry>();
        public bool IsDraggable;
        public Entry DraggedEntry;
        public BatchRemovalCollection<Entry> Copied = new BatchRemovalCollection<Entry>();
        private Vector2 DraggedOffset;

        // Added by EVWeb to not waste space when a list won't use certain buttons
        private bool CancelCol = true;
        private bool UpCol = true;
        private bool DownCol = true;
        private bool ApplyCol = true;
        private readonly Texture2D ArrowUpIcon  = ResourceManager.Texture("NewUI/icon_queue_arrow_up");
        private readonly Texture2D BuildAddIcon = ResourceManager.Texture("NewUI/icon_build_add");

        private readonly Texture2D ScrollBarArrowUp = ResourceManager.Texture("NewUI/scrollbar_arrow_up");
        private readonly Texture2D ScrollBarArrorDown = ResourceManager.Texture("NewUI/scrollbar_arrow_down");
        private readonly Texture2D ScrollBarMidMarker = ResourceManager.Texture("NewUI/scrollbar_bar_mid");
        

        public ScrollList(Submenu p)
        {
            Parent = p;
            entriesToDisplay = (p.Menu.Height - 25) / 40;            
            ScrollUp = new Rectangle(p.Menu.X + p.Menu.Width - 20, p.Menu.Y + 30, ScrollBarArrowUp.Width, ScrollBarArrowUp.Height);            
            ScrollDown = new Rectangle(p.Menu.X + p.Menu.Width - 20, p.Menu.Y + p.Menu.Height - 14, ScrollBarArrorDown.Width, ScrollBarArrorDown.Height);
            
            ScrollBarHousing = new Rectangle(ScrollUp.X + 1, ScrollUp.Y + ScrollUp.Height + 3, ScrollBarMidMarker.Width, ScrollDown.Y - ScrollUp.Y - ScrollUp.Height - 6);
            ScrollBar = new Rectangle(ScrollBarHousing.X, ScrollBarHousing.Y, ScrollBarMidMarker.Width, 0);
        }

        public ScrollList(Submenu p, bool cc, bool uc, bool dc, bool ac) : this(p)
        {
            CancelCol = cc;
            UpCol = uc;
            DownCol = dc;
            ApplyCol = ac;
        }

        public ScrollList(Submenu p, int eHeight)
        {
            entryHeight = eHeight;
            Parent = p;
            entriesToDisplay = (p.Menu.Height - 25) / entryHeight;
            ScrollUp = new Rectangle(p.Menu.X + p.Menu.Width - 20, p.Menu.Y + 30, ScrollBarArrowUp.Width, ScrollBarArrowUp.Height);
            ScrollDown = new Rectangle(p.Menu.X + p.Menu.Width - 20, p.Menu.Y + p.Menu.Height - 14, ScrollBarArrorDown.Width, ScrollBarArrorDown.Height);
            ScrollBarHousing = new Rectangle(ScrollUp.X + 1, ScrollUp.Y + ScrollUp.Height + 3, ScrollBarMidMarker.Width, ScrollDown.Y - ScrollUp.Y - ScrollUp.Height - 6);
            ScrollBar = new Rectangle(ScrollBarHousing.X, ScrollBarHousing.Y, ScrollBarMidMarker.Width, 0);
        }

        public ScrollList(Submenu p, int eHeight, bool cc, bool uc, bool dc, bool ac) : this(p, eHeight)
        {
            CancelCol = cc;
            UpCol = uc;
            DownCol = dc;
            ApplyCol = ac;
        }

        public ScrollList(Submenu p, int eHeight, bool realRect)
        {
            entryHeight = eHeight;
            Parent = p;
            entriesToDisplay = p.Menu.Height / entryHeight;
            ScrollUp = new Rectangle(p.Menu.X + p.Menu.Width - 20, p.Menu.Y + 5, ScrollBarArrowUp.Width, ScrollBarArrowUp.Height);
            ScrollDown = new Rectangle(p.Menu.X + p.Menu.Width - 20, p.Menu.Y + p.Menu.Height - 14, ScrollBarArrorDown.Width, ScrollBarArrorDown.Height);
            ScrollBarHousing = new Rectangle(ScrollUp.X + 1, ScrollUp.Y + ScrollUp.Height + 3, ScrollBarMidMarker.Width, ScrollDown.Y - ScrollUp.Y - ScrollUp.Height - 6);
            ScrollBar = new Rectangle(ScrollBarHousing.X, ScrollBarHousing.Y, ScrollBarMidMarker.Width, 0);
        }

        public ScrollList(Submenu p, int eHeight, bool realRect, bool cc, bool uc, bool dc, bool ac) : this(p, eHeight, realRect)
        {
            CancelCol = cc;
            UpCol = uc;
            DownCol = dc;
            ApplyCol = ac;
        }

        public Entry AddItem(object o)
        {
            var e = new Entry(o);
            Entries.Add(e);
            Update();
            e.ParentList = this;
            return e;
        }

        public void AddItem(object o, int addrect, int addpencil)
        {
            var e = new Entry(o);
            if (addrect > 0)
            {
                e.Plus = 1;
            }
            if (addpencil > 0)
            {
                e.editRect = new Rectangle();
                e.Edit = 1;
            }
            e.addRect = new Rectangle();
            Entries.Add(e);
            Update();
            e.ParentList = this;
        }

        public void AddQItem(object o)
        {
            var e = new Entry(o)
            {
                Plus = 0,
                up = new Rectangle(),
                down = new Rectangle(),
                apply = new Rectangle(),
                cancel = new Rectangle(),
                clickRect = new Rectangle(),
                QItem = 1
            };
            Entries.Add(e);
            Update();
            e.ParentList = this;
        }

        public virtual void Draw(SpriteBatch spriteBatch)
        {
            if (Copied.Count > entriesToDisplay)
            {
                int updownsize = (ScrollBar.Height - ScrollBarMidMarker.Height) / 2;
                var up = new Rectangle(ScrollBar.X, ScrollBar.Y, ScrollBar.Width, updownsize);
                var mid = new Rectangle(ScrollBar.X, ScrollBar.Y + updownsize, ScrollBar.Width, ScrollBarMidMarker.Height);
                var bot = new Rectangle(ScrollBar.X, mid.Y + mid.Height, ScrollBar.Width, updownsize);
                if (ScrollBarHover == 0)
                {
                    spriteBatch.Draw(ResourceManager.TextureDict["NewUI/scrollbar_bar_updown"], up, Color.White);
                    spriteBatch.Draw(ScrollBarMidMarker, mid, Color.White);
                    spriteBatch.Draw(ResourceManager.TextureDict["NewUI/scrollbar_bar_updown"], bot, Color.White);
                }
                else if (ScrollBarHover == 1)
                {
                    spriteBatch.Draw(ResourceManager.TextureDict["NewUI/scrollbar_bar_updown_hover1"], up, Color.White);
                    spriteBatch.Draw(ResourceManager.TextureDict["NewUI/scrollbar_bar_mid_hover1"], mid, Color.White);
                    spriteBatch.Draw(ResourceManager.TextureDict["NewUI/scrollbar_bar_updown_hover1"], bot, Color.White);
                }
                else if (ScrollBarHover == 2)
                {
                    spriteBatch.Draw(ResourceManager.TextureDict["NewUI/scrollbar_bar_updown_hover2"], up, Color.White);
                    spriteBatch.Draw(ResourceManager.TextureDict["NewUI/scrollbar_bar_mid_hover2"], mid, Color.White);
                    spriteBatch.Draw(ResourceManager.TextureDict["NewUI/scrollbar_bar_updown_hover2"], bot, Color.White);
                }
                Vector2 mousepos = Mouse.GetState().Pos();
                spriteBatch.Draw((ScrollUp.HitTest(mousepos) ? ResourceManager.TextureDict["NewUI/scrollbar_arrow_up_hover1"] : ScrollBarArrowUp), ScrollUp, Color.White);
                spriteBatch.Draw((ScrollDown.HitTest(mousepos) ? ResourceManager.TextureDict["NewUI/scrollbar_arrow_down_hover1"] : ScrollBarArrorDown), ScrollDown, Color.White);
            }
            if (DraggedEntry != null && DraggedEntry.item is QueueItem)
            {
                Vector2 mousepos = Mouse.GetState().Pos();

                var queueItem = DraggedEntry.item as QueueItem;
                if (queueItem.isBuilding)
                {
                    Vector2 bCursor = mousepos + DraggedOffset;
                    spriteBatch.Draw(ResourceManager.TextureDict[string.Concat("Buildings/icon_", queueItem.Building.Icon, "_48x48")], new Rectangle((int)bCursor.X, (int)bCursor.Y, 29, 30), Color.White);
                    var tCursor = new Vector2(bCursor.X + 40f, bCursor.Y);
                    spriteBatch.DrawString(Fonts.Arial12Bold, queueItem.Building.Name, tCursor, Color.White);
                    tCursor.Y = tCursor.Y + (float)Fonts.Arial12Bold.LineSpacing;
                    var pbRect = new Rectangle((int)tCursor.X, (int)tCursor.Y, 150, 18);
                    var pb = new ProgressBar(pbRect)
                    {
                        Max = queueItem.Cost,
                        Progress = queueItem.productionTowards
                    };
                    pb.Draw(spriteBatch);
                }
                else if (queueItem.isShip)
                {
                    Vector2 bCursor = mousepos + DraggedOffset;

                    spriteBatch.Draw(ResourceManager.HullsDict[queueItem.sData.Hull].Icon, new Rectangle((int)bCursor.X, (int)bCursor.Y, 29, 30), Color.White);
                    var tCursor = new Vector2(bCursor.X + 40f, bCursor.Y);
                    spriteBatch.DrawString(Fonts.Arial12Bold, queueItem.sData.Name, tCursor, Color.White);
                    tCursor.Y = tCursor.Y + (float)Fonts.Arial12Bold.LineSpacing;
                    var pbRect = new Rectangle((int)tCursor.X, (int)tCursor.Y, 150, 18);
                    var pb = new ProgressBar(pbRect)
                    {
                        Max = queueItem.Cost,
                        Progress = queueItem.productionTowards
                    };
                    pb.Draw(spriteBatch);
                }
                else if (queueItem.isTroop)
                {
                    Vector2 bCursor = mousepos + DraggedOffset;

                    Troop template = ResourceManager.GetTroopTemplate(queueItem.troopType);
                    template.Draw(spriteBatch, new Rectangle((int)bCursor.X, (int)bCursor.Y, 29, 30));

                    var tCursor = new Vector2(bCursor.X + 40f, bCursor.Y);
                    spriteBatch.DrawString(Fonts.Arial12Bold, queueItem.troopType, tCursor, Color.White);
                    tCursor.Y = tCursor.Y + Fonts.Arial12Bold.LineSpacing;
                    var pbRect = new Rectangle((int)tCursor.X, (int)tCursor.Y, 150, 18);
                    var pb = new ProgressBar(pbRect)
                    {
                        Max = queueItem.Cost,
                        Progress = queueItem.productionTowards
                    };
                    pb.Draw(spriteBatch);
                }
            }
        }

        public void DrawBlue(SpriteBatch spriteBatch)
        {
            if (Copied.Count > entriesToDisplay)
            {
                float percentViewed = entriesToDisplay / (float)Copied.Count;
                float startingPercent = indexAtTop / (float)Copied.Count;
                ScrollBar = new Rectangle(ScrollBarHousing.X, ScrollBarHousing.Y + (int)(startingPercent * ScrollBarHousing.Height), ResourceManager.TextureDict["ResearchMenu/scrollbar_bar_mid"].Width, (int)(ScrollBarHousing.Height * percentViewed));
                int updownsize = (ScrollBar.Height - ResourceManager.TextureDict["ResearchMenu/scrollbar_bar_mid"].Height) / 2;
                var up = new Rectangle(ScrollBar.X, ScrollBar.Y, ScrollBar.Width, updownsize);
                var mid = new Rectangle(ScrollBar.X, ScrollBar.Y + updownsize, ScrollBar.Width, ResourceManager.TextureDict["ResearchMenu/scrollbar_bar_mid"].Height);
                var bot = new Rectangle(ScrollBar.X, mid.Y + mid.Height, ScrollBar.Width, updownsize);
                if (ScrollBarHover == 0)
                {
                    spriteBatch.Draw(ResourceManager.TextureDict["ResearchMenu/scrollbar_bar_updown"], up, Color.White);
                    spriteBatch.Draw(ResourceManager.TextureDict["ResearchMenu/scrollbar_bar_mid"], mid, Color.White);
                    spriteBatch.Draw(ResourceManager.TextureDict["ResearchMenu/scrollbar_bar_updown"], bot, Color.White);
                }
                else if (ScrollBarHover == 1)
                {
                    spriteBatch.Draw(ResourceManager.TextureDict["ResearchMenu/scrollbar_bar_updown_hover1"], up, Color.White);
                    spriteBatch.Draw(ResourceManager.TextureDict["ResearchMenu/scrollbar_bar_mid_hover1"], mid, Color.White);
                    spriteBatch.Draw(ResourceManager.TextureDict["ResearchMenu/scrollbar_bar_updown_hover1"], bot, Color.White);
                }
                else if (ScrollBarHover == 2)
                {
                    spriteBatch.Draw(ResourceManager.TextureDict["ResearchMenu/scrollbar_bar_updown_hover2"], up, Color.White);
                    spriteBatch.Draw(ResourceManager.TextureDict["ResearchMenu/scrollbar_bar_mid_hover2"], mid, Color.White);
                    spriteBatch.Draw(ResourceManager.TextureDict["ResearchMenu/scrollbar_bar_updown_hover2"], bot, Color.White);
                }
                spriteBatch.Draw(ResourceManager.TextureDict["ResearchMenu/scrollbar_arrow_up"], ScrollUp, Color.White);
                spriteBatch.Draw(ResourceManager.TextureDict["ResearchMenu/scrollbar_arrow_down"], ScrollDown, Color.White);
            }
        }

        public virtual bool HandleInput(InputState input)
        {
            bool hit = false;
            if (ScrollUp.HitTest(input.CursorPosition) && input.InGameSelect)
            {
                if (indexAtTop > 0)
                {
                    ScrollList scrollList = this;
                    scrollList.indexAtTop = scrollList.indexAtTop - 1;
                }
                hit = true;
                float percentViewed = entriesToDisplay / (float)Copied.Count;
                float startingPercent = indexAtTop / (float)Copied.Count;
                ScrollBar = new Rectangle(ScrollBarHousing.X, ScrollBarHousing.Y + (int)(startingPercent * ScrollBarHousing.Height), ScrollBarMidMarker.Width, (int)(ScrollBarHousing.Height * percentViewed));
            }
            if (ScrollDown.HitTest(input.CursorPosition) && input.InGameSelect)
            {
                if (indexAtTop + entriesToDisplay < Copied.Count)
                {
                    ScrollList scrollList1 = this;
                    scrollList1.indexAtTop = scrollList1.indexAtTop + 1;
                }
                float percentViewed = entriesToDisplay / (float)Copied.Count;
                float startingPercent = indexAtTop / (float)Copied.Count;
                ScrollBar = new Rectangle(ScrollBarHousing.X, ScrollBarHousing.Y + (int)(startingPercent * ScrollBarHousing.Height), ScrollBarMidMarker.Width, (int)(ScrollBarHousing.Height * percentViewed));
                hit = true;
            }
            if (ScrollBarHousing.HitTest(input.CursorPosition))
            {
                ScrollBarHover = 1;
                //upScrollHover = 1;
                //downScrollHover = 1;
                if (ScrollBar.HitTest(input.CursorPosition))
                {
                    ScrollBarHover = 2;
                    if (input.MouseCurr.LeftButton == ButtonState.Pressed && input.MousePrev.LeftButton == ButtonState.Released)
                    {
                        startDragPos = (int)input.CursorPosition.Y;
                        ScrollBarStartDragPos = (float)ScrollBar.Y;
                        dragging = true;
                        hit = true;
                    }
                }
            }
            else if (!dragging)
            {
                ScrollBarHover = 0;
                //upScrollHover = 0;
                //downScrollHover = 0;
            }
            if (input.MouseCurr.LeftButton == ButtonState.Pressed && input.MousePrev.LeftButton == ButtonState.Pressed && dragging)
            {
                float difference = input.CursorPosition.Y - startDragPos;
                if (Math.Abs(difference) > 0f)
                {
                    ScrollBar.Y = (int)(ScrollBarStartDragPos + difference);
                    if (ScrollBar.Y < ScrollBarHousing.Y)
                    {
                        ScrollBar.Y = ScrollBarHousing.Y;
                    }
                    else if (ScrollBar.Y + ScrollBar.Height > ScrollBarHousing.Y + ScrollBarHousing.Height)
                    {
                        ScrollBar.Y = ScrollBarHousing.Y + ScrollBarHousing.Height - ScrollBar.Height;
                    }
                }
                float mousePosAsPct = (input.CursorPosition.Y - ScrollBarHousing.Y) / ScrollBarHousing.Height;
                mousePosAsPct = mousePosAsPct.Clamped(0f, 1f);
                indexAtTop = (int)(Copied.Count * mousePosAsPct);
                if (indexAtTop + entriesToDisplay >= Copied.Count)
                {
                    indexAtTop = Copied.Count - entriesToDisplay;
                }
                hit = true;
            }
            if (Parent.Menu.HitTest(input.CursorPosition))
            {
                if (input.MouseCurr.ScrollWheelValue > input.MousePrev.ScrollWheelValue)
                {
                    if (indexAtTop > 0)
                    {
                        --indexAtTop;
                    }
                    hit = true;
                    float percentViewed = entriesToDisplay / (float)Copied.Count;
                    float startingPercent = indexAtTop / (float)Copied.Count;
                    ScrollBar = new Rectangle(ScrollBarHousing.X, ScrollBarHousing.Y + (int)(startingPercent * ScrollBarHousing.Height), ScrollBarMidMarker.Width, (int)(ScrollBarHousing.Height * percentViewed));
                }
                if (input.MouseCurr.ScrollWheelValue < input.MousePrev.ScrollWheelValue)
                {
                    if (indexAtTop + entriesToDisplay < Copied.Count)
                    {
                        ++indexAtTop;
                    }
                    hit = true;
                    float percentViewed = entriesToDisplay / (float)Copied.Count;
                    float startingPercent = indexAtTop / (float)Copied.Count;
                    ScrollBar = new Rectangle(ScrollBarHousing.X, ScrollBarHousing.Y + (int)(startingPercent * ScrollBarHousing.Height), ScrollBarMidMarker.Width, (int)(ScrollBarHousing.Height * percentViewed));
                }
            }
            if (input.MouseCurr.LeftButton == ButtonState.Released && input.MousePrev.LeftButton == ButtonState.Pressed && dragging)
            {
                dragging = false;
            }
            if (IsDraggable && DraggedEntry == null)
            {
                for (int i = indexAtTop; i < Copied.Count && i < indexAtTop + entriesToDisplay; i++)
                {
                    Entry e = null;
                    try
                    {
                        e = Copied[i];
                    }
                    catch
                    {
                        continue;
                    }
                    if (e.clickRect.HitTest(input.CursorPosition))
                    {
                        if (input.MouseCurr.LeftButton != ButtonState.Pressed)
                        {
                            ClickTimer = 0f;
                        }
                        else
                        {
                            ScrollList clickTimer = this;
                            clickTimer.ClickTimer = clickTimer.ClickTimer + 0.0166666675f;
                            if (ClickTimer > TimerDelay)
                            {
                                DraggedEntry = e;
                                DraggedOffset = new Vector2(e.clickRect.X, e.clickRect.Y) - input.CursorPosition;
                                break;
                            }
                        }
                    }
                }
            }
            if (input.MouseCurr.LeftButton == ButtonState.Released)
            {
                ClickTimer = 0f;
                DraggedEntry = null;
            }
            if (DraggedEntry != null && input.MouseCurr.LeftButton == ButtonState.Pressed)
            {
                int dragged = 0;
                for (int i = indexAtTop; i < Entries.Count && i < indexAtTop + entriesToDisplay; i++)
                {
                    Entry e = null;
                    try
                    {
                        e = Entries[i];
                    }
                    catch
                    {
                        continue;
                    }
                    if (e.clickRect == DraggedEntry.clickRect)
                    {
                        dragged = Entries.IndexOf(e);
                    }
                }
                for (int i = indexAtTop; i < Entries.Count && i < indexAtTop + entriesToDisplay; i++)
                {
                    Entry e = Entries[i];
                    if (e.clickRect.HitTest(input.CursorPosition))
                    {
                        int newIndex = 0;
                        try
                        {
                            newIndex = Entries.IndexOf(e);
                        }
                        catch
                        {
                            continue;
                        }
                        if (newIndex < dragged)
                        {
                            ScrollList.Entry toReplace = e;
                            Entries[newIndex] = Entries[dragged];
                            Entries[newIndex].clickRect = toReplace.clickRect;
                            Entries[dragged] = toReplace;
                            Entries[dragged].clickRect = DraggedEntry.clickRect;
                            DraggedEntry = Entries[newIndex];
                            break;
                        }
                    }
                }
            }
            if (indexAtTop < 0)
            {
                indexAtTop = 0;
            }
            Update();
            return hit;
        }

        public bool HandleInput(InputState input, Planet p)
        {
            bool hit = false;
            if (ScrollUp.HitTest(input.CursorPosition) && input.InGameSelect)
            {
                if (indexAtTop > 0)
                {
                    ScrollList scrollList = this;
                    scrollList.indexAtTop = scrollList.indexAtTop - 1;
                }
                hit = true;
                float percentViewed = entriesToDisplay / (float)Copied.Count;
                float startingPercent = indexAtTop / (float)Copied.Count;
                ScrollBar = new Rectangle(ScrollBarHousing.X, ScrollBarHousing.Y + (int)(startingPercent * ScrollBarHousing.Height), ScrollBarMidMarker.Width, (int)(ScrollBarHousing.Height * percentViewed));
            }
            if (ScrollDown.HitTest(input.CursorPosition) && input.InGameSelect)
            {
                if (indexAtTop + entriesToDisplay < Copied.Count)
                {
                    ScrollList scrollList1 = this;
                    scrollList1.indexAtTop = scrollList1.indexAtTop + 1;
                }
                hit = true;
                float percentViewed = entriesToDisplay / (float)Copied.Count;
                float startingPercent = indexAtTop / (float)Copied.Count;
                ScrollBar = new Rectangle(ScrollBarHousing.X, ScrollBarHousing.Y + (int)(startingPercent * ScrollBarHousing.Height), ScrollBarMidMarker.Width, (int)(ScrollBarHousing.Height * percentViewed));
            }
            if (ScrollBarHousing.HitTest(input.CursorPosition))
            {
                ScrollBarHover = 1;
                //upScrollHover = 1;
                //downScrollHover = 1;
                if (ScrollBar.HitTest(input.CursorPosition))
                {
                    ScrollBarHover = 2;
                    if (input.MouseCurr.LeftButton == ButtonState.Pressed && input.MousePrev.LeftButton == ButtonState.Released)
                    {
                        startDragPos = (int)input.CursorPosition.Y;
                        ScrollBarStartDragPos = ScrollBar.Y;
                        dragging = true;
                        hit = true;
                    }
                }
            }
            else if (!dragging)
            {
                ScrollBarHover = 0;
                //upScrollHover = 0;
                //downScrollHover = 0;
            }
            if (input.MouseCurr.LeftButton == ButtonState.Pressed && input.MousePrev.LeftButton == ButtonState.Pressed && dragging)
            {
                float difference = input.CursorPosition.Y - startDragPos;
                if (Math.Abs(difference) > 0f)
                {
                    ScrollBar.Y = (int)(ScrollBarStartDragPos + difference);
                    if (ScrollBar.Y < ScrollBarHousing.Y)
                    {
                        ScrollBar.Y = ScrollBarHousing.Y;
                    }
                    else if (ScrollBar.Y + ScrollBar.Height > ScrollBarHousing.Y + ScrollBarHousing.Height)
                    {
                        ScrollBar.Y = ScrollBarHousing.Y + ScrollBarHousing.Height - ScrollBar.Height;
                    }
                }
                float mousePosAsPct = (input.CursorPosition.Y - ScrollBarHousing.Y) / ScrollBarHousing.Height;
                if (mousePosAsPct < 0f)
                {
                    mousePosAsPct = 0f;
                }
                if (mousePosAsPct > 1f)
                {
                    mousePosAsPct = 1f;
                }
                indexAtTop = (int)(Copied.Count * mousePosAsPct);
                if (indexAtTop + entriesToDisplay >= Copied.Count)
                {
                    indexAtTop = Copied.Count - entriesToDisplay;
                }
                hit = true;
            }
            if (Parent.Menu.HitTest(input.CursorPosition))
            {
                if (input.MouseCurr.ScrollWheelValue > input.MousePrev.ScrollWheelValue)
                {
                    if (indexAtTop > 0)
                    {
                        --indexAtTop;
                    }
                    hit = true;
                    float percentViewed = entriesToDisplay / (float)Copied.Count;
                    float startingPercent = indexAtTop / (float)Copied.Count;
                    ScrollBar = new Rectangle(ScrollBarHousing.X, ScrollBarHousing.Y + (int)(startingPercent * ScrollBarHousing.Height), ScrollBarMidMarker.Width, (int)(ScrollBarHousing.Height * percentViewed));
                }
                if (input.MouseCurr.ScrollWheelValue < input.MousePrev.ScrollWheelValue)
                {
                    if (indexAtTop + entriesToDisplay < Copied.Count)
                    {
                        ++indexAtTop;
                    }
                    hit = true;
                    float percentViewed = entriesToDisplay / (float)Copied.Count;
                    float startingPercent = indexAtTop / (float)Copied.Count;
                    ScrollBar = new Rectangle(ScrollBarHousing.X, ScrollBarHousing.Y + (int)(startingPercent * ScrollBarHousing.Height), ScrollBarMidMarker.Width, (int)(ScrollBarHousing.Height * percentViewed));
                }
            }
            if (input.MouseCurr.LeftButton == ButtonState.Released && input.MousePrev.LeftButton == ButtonState.Pressed && dragging)
            {
                dragging = false;
            }
            if (IsDraggable && DraggedEntry == null)
            {
                for (int i = indexAtTop; i < Entries.Count && i < indexAtTop + entriesToDisplay; i++)
                {
                    try
                    {
                        Entry e = Copied[i];
                        if (e.clickRect.HitTest(input.CursorPosition))
                        {
                            if (input.MouseCurr.LeftButton != ButtonState.Pressed)
                            {
                                ClickTimer = 0f;
                            }
                            else
                            {
                                ClickTimer += 0.0166666675f;
                                if (ClickTimer > TimerDelay)
                                {
                                    DraggedEntry = e;
                                    DraggedOffset = new Vector2(e.clickRect.X, e.clickRect.Y) - input.CursorPosition;
                                    break;
                                }
                            }
                        }
                    }
                    catch
                    {
                    }
                }
            }
            if (input.MouseCurr.LeftButton == ButtonState.Released)
            {
                ClickTimer = 0f;
                DraggedEntry = null;
            }
            if (DraggedEntry != null && input.MouseCurr.LeftButton == ButtonState.Pressed)
            {
                int Dragged = 0;
                for (int i = indexAtTop; i < Entries.Count && i < indexAtTop + entriesToDisplay; i++)
                {
                    Entry e = Entries[i];
                    if (e.clickRect == DraggedEntry.clickRect)
                    {
                        Dragged = Entries.IndexOf(e);
                    }
                }
                for (int i = indexAtTop; i < Entries.Count && i < indexAtTop + entriesToDisplay; i++)
                {
                    try
                    {
                        Entry e = Entries[i];
                        if (e.clickRect.HitTest(input.CursorPosition))
                        {
                            int NewIndex = Entries.IndexOf(e);
                            if (NewIndex < Dragged)
                            {
                                Entry toReplace = e;
                                Entries[NewIndex] = Entries[Dragged];
                                Entries[NewIndex].clickRect = toReplace.clickRect;
                                p.ConstructionQueue[NewIndex] = p.ConstructionQueue[Dragged];
                                Entries[Dragged] = toReplace;
                                Entries[Dragged].clickRect = DraggedEntry.clickRect;
                                p.ConstructionQueue[Dragged] = toReplace.item as QueueItem;
                                DraggedEntry = Entries[NewIndex];
                                break;
                            }
                            else if (NewIndex > Dragged)
                            {
                                Entry toRemove = Entries[Dragged];
                                for (int j = Dragged + 1; j <= NewIndex; j++)
                                {
                                    Entries[j].clickRect = Entries[j - 1].clickRect;
                                    Entries[j - 1] = Entries[j];
                                    p.ConstructionQueue[j - 1] = p.ConstructionQueue[j];
                                }
                                toRemove.clickRect = Entries[NewIndex].clickRect;
                                Entries[NewIndex] = toRemove;
                                p.ConstructionQueue[NewIndex] = toRemove.item as QueueItem;
                                DraggedEntry = Entries[NewIndex];
                            }
                        }
                    }
                    catch
                    {
                    }
                }
            }
            Update();
            return hit;
        }

        public void Reset()
        {
            Entries.Clear();
            Copied.Clear();
            indexAtTop = 0;
        }

        public void TransitionUpdate(Rectangle r)
        {
            ScrollUp = new Rectangle(r.X + r.Width - 20, r.Y + 30, ScrollBarArrowUp.Width, ScrollBarArrowUp.Height);
            ScrollDown = new Rectangle(r.X + r.Width - 20, r.Y + r.Height - 14, ScrollBarArrorDown.Width, ScrollBarArrorDown.Height);
            ScrollBarHousing = new Rectangle(ScrollUp.X + 1, ScrollUp.Y + ScrollUp.Height + 3, ScrollBarMidMarker.Width, ScrollDown.Y - ScrollUp.Y - ScrollUp.Height - 6);
            int j = 0;
            for (int i = 0; i < Entries.Count; i++)
            {
                if (i < indexAtTop)
                {
                    Entries[i].clickRect = new Rectangle(-500, -500, 0, 0);
                }
                else if (i > indexAtTop + entriesToDisplay - 1)
                {
                    Entries[i].clickRect = new Rectangle(-500, -500, 0, 0);
                }
                else
                {
                    Entries[i].clickRect = new Rectangle(r.X + 20, r.Y + 35 + j * entryHeight, Parent.Menu.Width - 40, entryHeight);
                    if (Entries[i].Plus != 0)
                    {
                        Rectangle plusRect = new Rectangle(Entries[i].clickRect.X + Entries[i].clickRect.Width - 30, Entries[i].clickRect.Y + 15 - BuildAddIcon.Height / 2, BuildAddIcon.Width, BuildAddIcon.Height);
                        Entries[i].addRect = plusRect;
                    }
                    if (Entries[i].Edit != 0)
                    {
                        Rectangle plusRect = new Rectangle(Entries[i].clickRect.X + Entries[i].clickRect.Width - 60, Entries[i].clickRect.Y + 15 - BuildAddIcon.Height / 2, BuildAddIcon.Width, BuildAddIcon.Height);
                        Entries[i].editRect = plusRect;
                    }
                    int x = 0;
                    if (UpCol)
                    {
                        Entries[i].up = new Rectangle(Entries[i].clickRect.X + Entries[i].clickRect.Width - (x += 30), Entries[i].clickRect.Y + 15 - ArrowUpIcon.Height / 2, ArrowUpIcon.Width, ArrowUpIcon.Height);
                    }
                    if (DownCol)
                    {
                        Entries[i].down = new Rectangle(Entries[i].clickRect.X + Entries[i].clickRect.Width - (x += 30), Entries[i].clickRect.Y + 15 - ArrowUpIcon.Height / 2, ArrowUpIcon.Width, ArrowUpIcon.Height);
                    }
                    if (ApplyCol)
                    {
                        Entries[i].apply = new Rectangle(Entries[i].clickRect.X + Entries[i].clickRect.Width - (x += 30), Entries[i].clickRect.Y + 15 - ArrowUpIcon.Height / 2, ArrowUpIcon.Width, ArrowUpIcon.Height);
                    }
                    if (CancelCol)
                    {
                        Entries[i].cancel = new Rectangle(Entries[i].clickRect.X + Entries[i].clickRect.Width - (x += 30), Entries[i].clickRect.Y + 15 - ArrowUpIcon.Height / 2, ArrowUpIcon.Width, ArrowUpIcon.Height);
                    }
                    j++;
                }
            }
            Entries.ApplyPendingRemovals();
        }

        public void Update()
        {
            Copied.Clear();
            foreach (Entry e in Entries)
            {
                Copied.Add(e);
                if (!e.ShowingSub)
                {
                    continue;
                }
                foreach (Entry sub in e.SubEntries)
                {
                    Copied.Add(sub);
                    foreach (Entry subsub in sub.SubEntries)
                    {
                        Copied.Add(subsub);
                    }
                }
            }
            int j = 0;
            Texture2D buildAddIcon = BuildAddIcon;
            for (int i = 0; i < Copied.Count; i++)
            {
                if (Copied[i] != null)
                {
                    if (i >= indexAtTop)
                    {
                        Copied[i].clickRect = new Rectangle(Parent.Menu.X + 20, Parent.Menu.Y + 35 + j * entryHeight, Parent.Menu.Width - 40, entryHeight);
                        
                        if (Copied[i].Plus != 0)
                        {
                            var plusRect = new Rectangle(Copied[i].clickRect.X + Copied[i].clickRect.Width - 30, Copied[i].clickRect.Y + 15 - buildAddIcon.Height / 2, buildAddIcon.Width, buildAddIcon.Height);
                            Copied[i].addRect = plusRect;
                        }
                        if (Copied[i].Edit != 0)
                        {
                            var plusRect = new Rectangle(Copied[i].clickRect.X + Copied[i].clickRect.Width - 60, Copied[i].clickRect.Y + 15 - buildAddIcon.Height / 2, buildAddIcon.Width, buildAddIcon.Height);
                            Copied[i].editRect = plusRect;
                        }
                        int x = 0;
                        if (UpCol)
                        {
                            Copied[i].up = new Rectangle(Copied[i].clickRect.X + Copied[i].clickRect.Width - (x += 30), Copied[i].clickRect.Y + 15 - ArrowUpIcon.Height / 2, ArrowUpIcon.Width, ArrowUpIcon.Height);
                        }
                        if (DownCol)
                        {
                            Copied[i].down = new Rectangle(Copied[i].clickRect.X + Copied[i].clickRect.Width - (x += 30), Copied[i].clickRect.Y + 15 - ArrowUpIcon.Height / 2, ArrowUpIcon.Width, ArrowUpIcon.Height);
                        }
                        if (ApplyCol)
                        {
                            Copied[i].apply = new Rectangle(Copied[i].clickRect.X + Copied[i].clickRect.Width - (x += 30), Copied[i].clickRect.Y + 15 - ArrowUpIcon.Height / 2, ArrowUpIcon.Width, ArrowUpIcon.Height);
                        }
                        if (CancelCol)
                        {
                            Copied[i].cancel = new Rectangle(Copied[i].clickRect.X + Copied[i].clickRect.Width - (x += 30), Copied[i].clickRect.Y + 15 - ArrowUpIcon.Height / 2, ArrowUpIcon.Width, ArrowUpIcon.Height);
                        }
                        j++;
                    }
                    else
                    {
                        Copied[i].clickRect = new Rectangle(-500, -500, 0, 0);
                    }
                }
            }
            float percentViewed = entriesToDisplay / (float)Copied.Count;
            ScrollBar.Height = (int)(ScrollBarHousing.Height * percentViewed);
            Entries.ApplyPendingRemovals();
            Copied.ApplyPendingRemovals();
            if (indexAtTop < 0)
            {
                indexAtTop = 0;
            }
        }

        public void Dispose()
        {
            Destroy();
            GC.SuppressFinalize(this);
        }

        ~ScrollList() { Destroy(); }

        private void Destroy()
        {
            Entries?.Dispose(ref Entries);
            Copied?.Dispose(ref Copied);
        }

        public class Entry
        {
            public bool ShowingSub;
            public Rectangle clickRect;
            public object item;
            public int Hover;
            public int Plus;
            public int PlusHover;
            public int Edit;
            public int EditHover;

            public Rectangle editRect;
            public Rectangle addRect;
            public ScrollList ParentList;

            public int clickRectHover;
            public int QItem;

            public Rectangle up;
            public Rectangle down;
            public Rectangle cancel;
            public Rectangle apply;

            public int uh;
            public int ch;
            public int dh;
            public int ah;

            // moved this here for consistency
            public Array<Entry> SubEntries = new Array<Entry>();

            public Entry(object item)
            {
                this.item = item;
            }

            public void AddItem(object o)
            {
                SubEntries.Add(new Entry(o));
            }

            public void AddItem(object o, int addrect, int addpencil)
            {
                var e = new Entry(o);
                if (addrect > 0)
                {
                    e.Plus = 1;
                }
                if (addpencil > 0)
                {
                    e.editRect = new Rectangle();
                    e.Edit = 1;
                }
                e.addRect = new Rectangle();
                SubEntries.Add(e);
            }

            public void AddItemWithCancel(object o)
            {
                var e = new Entry(o);
                SubEntries.Add(e);
                e.cancel = new Rectangle();
            }

            public bool WasClicked(InputState input)
            {
                return input.LeftMouseClick && clickRect.HitTest(input.CursorPosition);
            }
        }
    }


}