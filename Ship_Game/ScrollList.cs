using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using System;
using System.Linq;
using System.Collections.Generic;

namespace Ship_Game
{
    public class ScrollList
    {
        private readonly Submenu Parent;
        public Rectangle ScrollUp;
        public Rectangle ScrollDown;
        public Rectangle ScrollBarHousing;
        public Rectangle ScrollBar;

        private readonly int EntryHeight = 40;
        private int ScrollBarHover;
        private int StartDragPos;
        private bool DraggingScrollBar;
        private float ScrollBarStartDragPos;
        private float ClickTimer;
        private float TimerDelay = 0.05f;

        public int entriesToDisplay;
        public int indexAtTop;
        private readonly Array<Entry> Entries = new Array<Entry>();
        private readonly Array<Entry> Copied = new Array<Entry>(); // Flattened entries
        public bool IsDraggable;
        public Entry DraggedEntry;
        private Vector2 DraggedOffset;

        // Added by EVWeb to not waste space when a list won't use certain buttons
        private readonly bool CancelCol = true;
        private readonly bool UpCol = true;
        private readonly bool DownCol = true;
        private readonly bool ApplyCol = true;
        private readonly Texture2D ArrowUpIcon  = ResourceManager.Texture("NewUI/icon_queue_arrow_up");
        private readonly Texture2D BuildAddIcon = ResourceManager.Texture("NewUI/icon_build_add");

        private readonly Texture2D ScrollBarArrowUp   = ResourceManager.Texture("NewUI/scrollbar_arrow_up");
        private readonly Texture2D ScrollBarArrorDown = ResourceManager.Texture("NewUI/scrollbar_arrow_down");
        private readonly Texture2D ScrollBarMidMarker = ResourceManager.Texture("NewUI/scrollbar_bar_mid");
        

        public ScrollList(Submenu p)
        {
            Parent = p;
            entriesToDisplay = (p.Menu.Height - 25) / 40;            
            InitializeRects(p, 30);
        }

        public ScrollList(Submenu p, int eHeight)
        {
            Parent = p;
            EntryHeight = eHeight;
            entriesToDisplay = (p.Menu.Height - 25) / EntryHeight;
            InitializeRects(p, 30);
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
            Parent = p;
            EntryHeight = eHeight;
            entriesToDisplay = p.Menu.Height / EntryHeight;
            InitializeRects(p, 5);
        }

        private void InitializeRects(Submenu p, int yOffset)
        {
            int x = p.Menu.X + p.Menu.Width - 20;
            ScrollUp   = new Rectangle(x, p.Menu.Y + yOffset,            ScrollBarArrowUp.Width, ScrollBarArrowUp.Height);
            ScrollDown = new Rectangle(x, p.Menu.Y + p.Menu.Height - 14, ScrollBarArrorDown.Width, ScrollBarArrorDown.Height);
            ScrollBarHousing = new Rectangle(ScrollUp.X + 1, ScrollUp.Y + ScrollUp.Height + 3, ScrollBarMidMarker.Width, ScrollDown.Y - ScrollUp.Y - ScrollUp.Height - 6);
            ScrollBar        = new Rectangle(ScrollBarHousing.X, ScrollBarHousing.Y, ScrollBarMidMarker.Width, 0);
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
            if (addrect   > 0) e.Plus = 1;
            if (addpencil > 0) e.Edit = 1;
            Entries.Add(e);
            Update();
            e.ParentList = this;
        }

        public void AddQItem(object o)
        {
            var e = new Entry(o)
            {
                Plus  = 0,
                QItem = 1
            };
            Entries.Add(e);
            Update();
            e.ParentList = this;
        }

        public void Remove(Entry e)
        {
            Entries.Remove(e);
            Copied.Remove(e);
        }

        public void RemoveItem(object o)
        {
            Entries.RemoveFirstIf(e => e.item == o);
            Copied.RemoveFirstIf(e => e.item == o);
        }

        public void RemoveIf<T>(Func<T, bool> predicate) where T : class
        {
            Entries.RemoveAllIf(e => e.item is T item && predicate(item));
            Copied.RemoveAllIf(e => e.item is T item && predicate(item));
        }

        public void RemoveFirst()
        {
            if (Entries.Count > 0)
            {
                Entries.RemoveAt(0);
                Copied.RemoveAt(0);
            }
        }

        public Entry EntryAt(int index) => Entries[index];

        public T FirstItem<T>() where T : class
        {
            return (T)Copied[0].item;
        }

        public T ItemAt<T>(int index) where T : class
        {
            return (T)Copied[index].item;
        }

        public T ItemAtTop<T>() where T : class
        {
            return (T)Copied[indexAtTop].item;
        }

        public int IndexOf<T>(Func<T, bool> predicate) where T : class
        {
            for (int i = 0; i < Entries.Count; ++i)
            {
                Entry e = Entries[i];
                if (e.item is T item && predicate(item))
                    return i;
            }
            return -1;
        }

        public void Sort<T, TValue>(Func<T, TValue> predicate) where T : class
        {
            Entry[] sorted = Entries.OrderBy(e => predicate(e.item as T)).ToArray();
            Entries.Clear();
            Entries.AddRange(sorted);
            Update();
        }

        public void SortDescending<T, TValue>(Func<T, TValue> predicate) where T : class
        {
            Entry[] sorted = Entries.OrderByDescending(e => predicate(e.item as T)).ToArray();
            Entries.Clear();
            Entries.AddRange(sorted);
            Update();
        }

        public IReadOnlyList<Entry> AllEntries => Entries;
        public IReadOnlyList<Entry> AllFlattenedEntries => Copied;
        public int NumEntries => Entries.Count;
        public int NumFlattenedEntries => Copied.Count;

        public IEnumerable<Entry> VisibleEntries
        {
            get
            {
                for (int i = indexAtTop; i < Entries.Count && i < indexAtTop + entriesToDisplay; ++i)
                    yield return Entries[i];
            }
        }

        public IEnumerable<Entry> FlattenedEntries
        {
            get
            {
                for (int i = indexAtTop; i < Copied.Count && i < indexAtTop + entriesToDisplay; ++i)
                    yield return Copied[i];
            }
        }

        public IEnumerable<T> VisibleItems<T>() where T : class
        {
            for (int i = indexAtTop; i < Entries.Count && i < indexAtTop + entriesToDisplay; ++i)
                if (Entries[i].item is T item)
                    yield return item;
        }

        public IEnumerable<T> FlattenedItems<T>() where T : class
        {
            for (int i = indexAtTop; i < Copied.Count && i < indexAtTop + entriesToDisplay; ++i)
                if (Copied[i].item is T item)
                    yield return item;
        }

        public IEnumerable<T> AllItems<T>() where T : class
        {
            for (int i = 0; i < Entries.Count; ++i)
                if (Entries[i].item is T item)
                    yield return item;
        }

        public IEnumerable<T> AllFlattenedItems<T>() where T : class
        {
            for (int i = 0; i < Copied.Count; ++i)
                if (Copied[i].item is T item)
                    yield return item;
        }

        public void Reset()
        {
            Entries.Clear();
            Copied.Clear();
            indexAtTop = 0;
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
                    spriteBatch.Draw(ResourceManager.Texture("NewUI/scrollbar_bar_updown"), up, Color.White);
                    spriteBatch.Draw(ScrollBarMidMarker, mid, Color.White);
                    spriteBatch.Draw(ResourceManager.Texture("NewUI/scrollbar_bar_updown"), bot, Color.White);
                }
                else if (ScrollBarHover == 1)
                {
                    spriteBatch.Draw(ResourceManager.Texture("NewUI/scrollbar_bar_updown_hover1"), up, Color.White);
                    spriteBatch.Draw(ResourceManager.Texture("NewUI/scrollbar_bar_mid_hover1"), mid, Color.White);
                    spriteBatch.Draw(ResourceManager.Texture("NewUI/scrollbar_bar_updown_hover1"), bot, Color.White);
                }
                else if (ScrollBarHover == 2)
                {
                    spriteBatch.Draw(ResourceManager.Texture("NewUI/scrollbar_bar_updown_hover2"), up, Color.White);
                    spriteBatch.Draw(ResourceManager.Texture("NewUI/scrollbar_bar_mid_hover2"), mid, Color.White);
                    spriteBatch.Draw(ResourceManager.Texture("NewUI/scrollbar_bar_updown_hover2"), bot, Color.White);
                }
                Vector2 mousepos = Mouse.GetState().Pos();
                spriteBatch.Draw(ScrollUp.HitTest(mousepos) ? ResourceManager.Texture("NewUI/scrollbar_arrow_up_hover1") : ScrollBarArrowUp, ScrollUp, Color.White);
                spriteBatch.Draw(ScrollDown.HitTest(mousepos) ? ResourceManager.Texture("NewUI/scrollbar_arrow_down_hover1") : ScrollBarArrorDown, ScrollDown, Color.White);
            }
            if (DraggedEntry != null && DraggedEntry.item is QueueItem)
            {
                Vector2 mousepos = Mouse.GetState().Pos();

                var queueItem = DraggedEntry.item as QueueItem;
                if (queueItem.isBuilding)
                {
                    Vector2 bCursor = mousepos + DraggedOffset;
                    spriteBatch.Draw(ResourceManager.Texture($"Buildings/icon_{queueItem.Building.Icon}_48x48"), new Rectangle((int)bCursor.X, (int)bCursor.Y, 29, 30), Color.White);
                    var tCursor = new Vector2(bCursor.X + 40f, bCursor.Y);
                    spriteBatch.DrawString(Fonts.Arial12Bold, queueItem.Building.Name, tCursor, Color.White);
                    tCursor.Y += Fonts.Arial12Bold.LineSpacing;
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
                    tCursor.Y += Fonts.Arial12Bold.LineSpacing;
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
                    tCursor.Y += Fonts.Arial12Bold.LineSpacing;
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

        private float PercentViewed => entriesToDisplay / (float)Copied.Count;
        private float StartingPercent => indexAtTop / (float)Copied.Count;

        private void UpdateScrollBar(bool blue = false)
        {
            int scrollStart = (int)(StartingPercent * ScrollBarHousing.Height);
            int scrollEnd = (int)(ScrollBarHousing.Height * PercentViewed);
            int width = blue ? ResourceManager.Texture("ResearchMenu/scrollbar_bar_mid").Width : ScrollBarMidMarker.Width;

            ScrollBar = new Rectangle(ScrollBarHousing.X, ScrollBarHousing.Y + scrollStart, width, scrollEnd);
        }

        public void DrawBlue(SpriteBatch spriteBatch)
        {
            if (Copied.Count > entriesToDisplay)
            {
                UpdateScrollBar(blue: true);
                int updownsize = (ScrollBar.Height - ResourceManager.Texture("ResearchMenu/scrollbar_bar_mid").Height) / 2;
                var up = new Rectangle(ScrollBar.X, ScrollBar.Y, ScrollBar.Width, updownsize);
                var mid = new Rectangle(ScrollBar.X, ScrollBar.Y + updownsize, ScrollBar.Width, ResourceManager.Texture("ResearchMenu/scrollbar_bar_mid").Height);
                var bot = new Rectangle(ScrollBar.X, mid.Y + mid.Height, ScrollBar.Width, updownsize);
                if (ScrollBarHover == 0)
                {
                    spriteBatch.Draw(ResourceManager.Texture("ResearchMenu/scrollbar_bar_updown"), up, Color.White);
                    spriteBatch.Draw(ResourceManager.Texture("ResearchMenu/scrollbar_bar_mid"), mid, Color.White);
                    spriteBatch.Draw(ResourceManager.Texture("ResearchMenu/scrollbar_bar_updown"), bot, Color.White);
                }
                else if (ScrollBarHover == 1)
                {
                    spriteBatch.Draw(ResourceManager.Texture("ResearchMenu/scrollbar_bar_updown_hover1"), up, Color.White);
                    spriteBatch.Draw(ResourceManager.Texture("ResearchMenu/scrollbar_bar_mid_hover1"), mid, Color.White);
                    spriteBatch.Draw(ResourceManager.Texture("ResearchMenu/scrollbar_bar_updown_hover1"), bot, Color.White);
                }
                else if (ScrollBarHover == 2)
                {
                    spriteBatch.Draw(ResourceManager.Texture("ResearchMenu/scrollbar_bar_updown_hover2"), up, Color.White);
                    spriteBatch.Draw(ResourceManager.Texture("ResearchMenu/scrollbar_bar_mid_hover2"), mid, Color.White);
                    spriteBatch.Draw(ResourceManager.Texture("ResearchMenu/scrollbar_bar_updown_hover2"), bot, Color.White);
                }
                spriteBatch.Draw(ResourceManager.Texture("ResearchMenu/scrollbar_arrow_up"), ScrollUp, Color.White);
                spriteBatch.Draw(ResourceManager.Texture("ResearchMenu/scrollbar_arrow_down"), ScrollDown, Color.White);
            }
        }


        private bool HandleScrollUpDownButtons(InputState input)
        {
            if (!input.InGameSelect)
                return false;
            if (ScrollUp.HitTest(input.CursorPosition))
            {
                if (indexAtTop > 0)
                    --indexAtTop;
                UpdateScrollBar();
                return true;
            }
            if (ScrollDown.HitTest(input.CursorPosition))
            {
                if (indexAtTop + entriesToDisplay < Copied.Count)
                    ++indexAtTop;
                UpdateScrollBar();
                return true;
            }
            return false;
        }

        private bool HandleScrollDragInput(InputState input)
        {
            ScrollBarHover = 0;
            if (!ScrollBarHousing.HitTest(input.CursorPosition))
                return false;

            if (!ScrollBar.HitTest(input.CursorPosition))
                return false;
            
            ScrollBarHover = 1;
            if (!input.LeftMouseClick)
                return false;
            
            ScrollBarHover = 2;
            StartDragPos = (int)input.CursorPosition.Y;
            ScrollBarStartDragPos = ScrollBar.Y;
            DraggingScrollBar = true;
            return true;
        }

        private bool HandleScrollBarDragging(InputState input)
        {
            if (!input.LeftMouseDown || !DraggingScrollBar)
                return false;

            float difference = input.CursorPosition.Y - StartDragPos;
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
            return true;
        }

        private bool HandleScrollBar(InputState input)
        {
            bool hit = HandleScrollUpDownButtons(input);
            hit |= HandleScrollDragInput(input);
            hit |= HandleScrollBarDragging(input);
            
            if (DraggingScrollBar && input.LeftMouseUp)
                DraggingScrollBar = false;
            return hit;
        }

        private bool HandleParentMenuOpen(InputState input)
        {
            if (!Parent.Menu.HitTest(input.CursorPosition))
                return false;
            if (input.ScrollIn)
            {
                if (indexAtTop > 0)
                    --indexAtTop;
                UpdateScrollBar();
                return true;
            }
            if (input.ScrollOut)
            {
                if (indexAtTop + entriesToDisplay < Copied.Count)
                    ++indexAtTop;
                UpdateScrollBar();
                return true;
            }
            return false;
        }

        private void HandleDraggable(InputState input)
        {
            if (IsDraggable && DraggedEntry == null)
            {
                if (input.LeftMouseUp)
                {
                    ClickTimer = 0f;
                }
                else
                {
                    ClickTimer += 0.0166666675f;
                    if (ClickTimer > TimerDelay)
                    {
                        for (int i = indexAtTop; i < Copied.Count && i < indexAtTop + entriesToDisplay; i++)
                        {
                            Entry e = Copied[i];
                            if (!e.clickRect.HitTest(input.CursorPosition))
                                continue;
                            DraggedEntry = e;
                            DraggedOffset = new Vector2(e.clickRect.X, e.clickRect.Y) - input.CursorPosition;
                            break;
                        }
                    }
                }
            }
            if (input.LeftMouseUp)
            {
                ClickTimer = 0f;
                DraggedEntry = null;
            }
        }

        private void HandleElementDragging(InputState input, Func<int, int, bool> onDragElement)
        {
            if (DraggedEntry == null || !input.LeftMouseDown)
                return;

            int dragged = 0;
            // WTF?
            for (int i = indexAtTop; i < Entries.Count && i < indexAtTop + entriesToDisplay; i++)
            {
                Entry e = Entries[i];
                if (e.clickRect == DraggedEntry.clickRect)
                {
                    dragged = i;
                    break;
                }
            }
            for (int i = indexAtTop; i < Entries.Count && i < indexAtTop + entriesToDisplay; i++)
            {
                Entry e = Entries[i];
                if (!e.clickRect.HitTest(input.CursorPosition))
                    continue;
                if (onDragElement(i, dragged))
                    break;
            }
        }

        public virtual bool HandleInput(InputState input)
        {
            bool hit = HandleScrollBar(input);
            hit |= HandleParentMenuOpen(input);
            HandleDraggable(input);

            HandleElementDragging(input, (newIndex, dragged) =>
            {
                if (newIndex < dragged)
                {
                    Entry toReplace = Entries[newIndex];
                    Entries[newIndex] = Entries[dragged];
                    Entries[newIndex].clickRect = toReplace.clickRect;
                    Entries[dragged] = toReplace;
                    Entries[dragged].clickRect = DraggedEntry.clickRect;
                    DraggedEntry = Entries[newIndex];
                    return true;
                }
                return false;
            });
            Update();
            return hit;
        }

        public bool HandleInput(InputState input, Planet p)
        {
            bool hit = HandleScrollBar(input);
            hit |= HandleParentMenuOpen(input);
            HandleDraggable(input);

            HandleElementDragging(input, (newIndex, dragged) =>
            {
                if (newIndex < dragged)
                {
                    Entry toReplace = Entries[newIndex];
                    Entries[newIndex] = Entries[dragged];
                    Entries[newIndex].clickRect = toReplace.clickRect;
                    p.ConstructionQueue[newIndex] = p.ConstructionQueue[dragged];
                    Entries[dragged] = toReplace;
                    Entries[dragged].clickRect = DraggedEntry.clickRect;
                    p.ConstructionQueue[dragged] = toReplace.item as QueueItem;
                    DraggedEntry = Entries[newIndex];
                    return true;
                }
                if (newIndex > dragged)
                {
                    Entry toRemove = Entries[dragged];
                    for (int j = dragged + 1; j <= newIndex; j++)
                    {
                        Entries[j].clickRect = Entries[j - 1].clickRect;
                        Entries[j - 1] = Entries[j];
                        p.ConstructionQueue[j - 1] = p.ConstructionQueue[j];
                    }
                    toRemove.clickRect = Entries[newIndex].clickRect;
                    Entries[newIndex] = toRemove;
                    p.ConstructionQueue[newIndex] = toRemove.item as QueueItem;
                    DraggedEntry = Entries[newIndex];
                }
                return false;
            });
            Update();
            return hit;
        }

        private void UpdateClickRect(Entry e, Point topLeft, int j)
        {
            var r = new Rectangle(topLeft.X + 20, topLeft.Y + 35 + j * EntryHeight, Parent.Menu.Width - 40, EntryHeight);
            e.clickRect = r;
            if (e.Plus != 0) e.addRect  = new Rectangle(r.X + r.Width - 30, r.Y + 15 - BuildAddIcon.Height / 2, BuildAddIcon.Width, BuildAddIcon.Height);
            if (e.Edit != 0) e.editRect = new Rectangle(r.X + r.Width - 60, r.Y + 15 - BuildAddIcon.Height / 2, BuildAddIcon.Width, BuildAddIcon.Height);
            if (UpCol)     e.up     = new Rectangle(r.X + r.Width -  30, r.Y + 15 - ArrowUpIcon.Height / 2, ArrowUpIcon.Width, ArrowUpIcon.Height);
            if (DownCol)   e.down   = new Rectangle(r.X + r.Width -  60, r.Y + 15 - ArrowUpIcon.Height / 2, ArrowUpIcon.Width, ArrowUpIcon.Height);
            if (ApplyCol)  e.apply  = new Rectangle(r.X + r.Width -  90, r.Y + 15 - ArrowUpIcon.Height / 2, ArrowUpIcon.Width, ArrowUpIcon.Height);
            if (CancelCol) e.cancel = new Rectangle(r.X + r.Width - 120, r.Y + 15 - ArrowUpIcon.Height / 2, ArrowUpIcon.Width, ArrowUpIcon.Height);
        }

        public void TransitionUpdate(Rectangle r)
        {
            ScrollUp = new Rectangle(r.X + r.Width - 20, r.Y + 30, ScrollBarArrowUp.Width, ScrollBarArrowUp.Height);
            ScrollDown = new Rectangle(r.X + r.Width - 20, r.Y + r.Height - 14, ScrollBarArrorDown.Width, ScrollBarArrorDown.Height);
            ScrollBarHousing = new Rectangle(ScrollUp.X + 1, ScrollUp.Y + ScrollUp.Height + 3, ScrollBarMidMarker.Width, ScrollDown.Y - ScrollUp.Y - ScrollUp.Height - 6);
            int j = 0;
            for (int i = 0; i < Entries.Count; i++)
            {
                Entry e = Entries[i];
                if (i < indexAtTop || i > indexAtTop + entriesToDisplay - 1)
                {
                    e.SetUnclickable();
                }
                else
                {
                    UpdateClickRect(e, r.Pos(), j++);
                }
            }
        }

        public void Update()
        {
            Copied.Clear();

            foreach (Entry e in Entries)
            {
                Copied.Add(e);
                if (!e.ShowingSub)
                    continue;
                foreach (Entry sub in e.SubEntries)
                {
                    Copied.Add(sub);
                    foreach (Entry subsub in sub.SubEntries)
                        Copied.Add(subsub);
                }
            }
            
            int j = 0;
            for (int i = 0; i < Copied.Count; i++)
            {
                Entry e = Copied[i];
                if (i >= indexAtTop)
                {
                    UpdateClickRect(e, Parent.Menu.Pos(), j++);
                }
                else
                {
                    e.SetUnclickable();
                }
            }
            ScrollBar.Height = (int)(ScrollBarHousing.Height * PercentViewed);
            if (indexAtTop < 0)
                indexAtTop = 0;
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

            public void SetUnclickable()
            {
                clickRect = new Rectangle(-500, -500, 0, 0);
            }
        }
    }
}