using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;

namespace Ship_Game
{
    public enum ListControls
    {
        All,    // show all list controls
        Cancel // only show Cancel control
    }

    public enum ListOptions
    {
        None,
        Draggable
    }

    [DebuggerTypeProxy(typeof(ScrollListDebugView))]
    [DebuggerDisplay("Entries = {Entries.Count}  Expanded = {ExpandedEntries.Count}")]
    public class ScrollList
    {
        private readonly Submenu Parent;
        private Rectangle ScrollUp;
        private Rectangle ScrollDown;
        private Rectangle ScrollBarHousing;
        private Rectangle ScrollBar;

        private readonly int EntryHeight = 40;
        private int ScrollBarHover;
        private int StartDragPos;
        private bool DraggingScrollBar;
        private float ScrollBarStartDragPos;
        private float ClickTimer;
        private float TimerDelay = 0.05f;

        private readonly int MaxVisibleEntries;
        public int FirstVisibleIndex;
        private readonly Array<Entry> Entries = new Array<Entry>();
        private readonly Array<Entry> ExpandedEntries = new Array<Entry>(); // Flattened entries
        private readonly bool IsDraggable;
        public Entry DraggedEntry;
        private Vector2 DraggedOffset;

        // Added by EVWeb to not waste space when a list won't use certain buttons
        private readonly ListControls Controls = ListControls.All;
        private readonly Texture2D ArrowUpIcon  = ResourceManager.Texture("NewUI/icon_queue_arrow_up");
        private readonly Texture2D BuildAddIcon = ResourceManager.Texture("NewUI/icon_build_add");

        private readonly Texture2D ScrollBarArrowUp   = ResourceManager.Texture("NewUI/scrollbar_arrow_up");
        private readonly Texture2D ScrollBarArrorDown = ResourceManager.Texture("NewUI/scrollbar_arrow_down");
        private readonly Texture2D ScrollBarMidMarker = ResourceManager.Texture("NewUI/scrollbar_bar_mid");
        

        public ScrollList(Submenu p, ListOptions options = ListOptions.None)
        {
            Parent = p;
            MaxVisibleEntries = (p.Menu.Height - 25) / 40;         
            IsDraggable = options == ListOptions.Draggable;
            InitializeRects(p, 30);
        }

        public ScrollList(Submenu p, int eHeight, ListControls controls = ListControls.All)
        {
            Parent = p;
            EntryHeight = eHeight;
            Controls = controls;
            MaxVisibleEntries = (p.Menu.Height - 25) / EntryHeight;
            InitializeRects(p, 30);
        }

        public ScrollList(Submenu p, int eHeight, bool realRect)
        {
            Parent = p;
            EntryHeight = eHeight;
            MaxVisibleEntries = p.Menu.Height / EntryHeight;
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
            var e = new Entry(this, o, false, false);
            Entries.Add(e);
            UpdateListElements();
            return e;
        }

        public void AddItem(object o, bool plus, bool edit)
        {
            Entries.Add(new Entry(this, o, plus, edit));
            UpdateListElements();
        }

        public void SetItems<T>(IEnumerable<T> newItems) where T : class
        {
            Entries.Clear();
            ExpandedEntries.Clear();
            foreach (T item in newItems)
                Entries.Add(new Entry(this, item, false, false));
            UpdateListElements();
        }

        private bool RemoveSub(Entry e)
        {
            foreach (Entry entry in Entries)
                if (entry.RemoveSub(e)) return true;
            return false;
        }

        public void Remove(Entry e)
        {
            if (!RemoveSub(e))
                Entries.Remove(e);

            if (ExpandedEntries.Remove(e))
                UpdateListElements();
        }

        private bool RemoveSubItem(Predicate<Entry> predicate)
        {
            foreach (Entry entry in Entries)
                if (entry.RemoveFirstSubIf(predicate)) return true;
            return false;
        }

        public void RemoveItem(object o)
        {
            bool ItemPredicate(Entry e) => e.item == o;

            if (!RemoveSubItem(ItemPredicate))
                Entries.RemoveFirstIf(ItemPredicate);

            if (ExpandedEntries.RemoveFirstIf(ItemPredicate))
                UpdateListElements();
        }

        public void RemoveFirstIf<T>(Func<T, bool> predicate) where T : class
        {
            bool ItemPredicate(Entry e) => e.item is T item && predicate(item);

            if (!RemoveSubItem(ItemPredicate))
                Entries.RemoveFirstIf(ItemPredicate);

            if (ExpandedEntries.RemoveFirstIf(ItemPredicate))
                UpdateListElements();
        }

        public void RemoveFirst()
        {
            if (Entries.Count <= 0)
                return;
            Entries.RemoveAt(0);
            ExpandedEntries.RemoveAt(0);
            UpdateListElements();
        }

        public Entry EntryAt(int index) => Entries[index];

        public T FirstItem<T>() where T : class
        {
            return (T)ExpandedEntries[0].item;
        }

        public T ItemAt<T>(int index) where T : class
        {
            return (T)ExpandedEntries[index].item;
        }

        public T ItemAtTop<T>() where T : class
        {
            return (T)ExpandedEntries[FirstVisibleIndex].item;
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
            UpdateListElements();
        }

        public void SortDescending<T, TValue>(Func<T, TValue> predicate) where T : class
        {
            Entry[] sorted = Entries.OrderByDescending(e => predicate(e.item as T)).ToArray();
            Entries.Clear();
            Entries.AddRange(sorted);
            UpdateListElements();
        }

        public IReadOnlyList<Entry> AllEntries => Entries;
        public IReadOnlyList<Entry> AllExpandedEntries => ExpandedEntries;
        public int NumEntries => Entries.Count;
        public int NumExpandedEntries => ExpandedEntries.Count;

        private int EntriesEnd         => Math.Min(Entries.Count,         FirstVisibleIndex + MaxVisibleEntries);
        private int ExpandedEntriesEnd => Math.Min(ExpandedEntries.Count, FirstVisibleIndex + MaxVisibleEntries);

        public IEnumerable<Entry> VisibleEntries
        {
            get
            {
                int end = EntriesEnd;
                for (int i = FirstVisibleIndex; i < end; ++i)
                    yield return Entries[i];
            }
        }

        public IEnumerable<Entry> VisibleExpandedEntries
        {
            get
            {
                int end = ExpandedEntriesEnd;
                for (int i = FirstVisibleIndex; i < end; ++i)
                    yield return ExpandedEntries[i];
            }
        }

        public IEnumerable<T> VisibleItems<T>() where T : class
        {
            int end = EntriesEnd;
            for (int i = FirstVisibleIndex; i < end; ++i)
                if (Entries[i].item is T item)
                    yield return item;
        }

        public IEnumerable<T> VisibleExpandedItems<T>() where T : class
        {
            int end = ExpandedEntriesEnd;
            for (int i = FirstVisibleIndex; i < end; ++i)
                if (ExpandedEntries[i].item is T item)
                    yield return item;
        }

        public IEnumerable<T> AllItems<T>() where T : class
        {
            for (int i = 0; i < Entries.Count; ++i)
                if (Entries[i].item is T item)
                    yield return item;
        }

        public IEnumerable<T> AllExpandedItems<T>() where T : class
        {
            for (int i = 0; i < ExpandedEntries.Count; ++i)
                if (ExpandedEntries[i].item is T item)
                    yield return item;
        }

        public void Reset()
        {
            Entries.Clear();
            ExpandedEntries.Clear();
            FirstVisibleIndex = 0;
            UpdateListElements();
        }

        private void DrawScrollBar(SpriteBatch spriteBatch)
        {
            int updownsize = (ScrollBar.Height - ScrollBarMidMarker.Height) / 2;
            var up  = new Rectangle(ScrollBar.X, ScrollBar.Y, ScrollBar.Width, updownsize);
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

        private void DrawScrollBarBlue(SpriteBatch spriteBatch)
        {
            int updownsize = (ScrollBar.Height - ResourceManager.Texture("ResearchMenu/scrollbar_bar_mid").Height) / 2;
            var up  = new Rectangle(ScrollBar.X, ScrollBar.Y, ScrollBar.Width, updownsize);
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

        public void DrawBlue(SpriteBatch spriteBatch)
        {
            if (ExpandedEntries.Count > MaxVisibleEntries)
            {
                UpdateScrollBar(blue: true);
                DrawScrollBarBlue(spriteBatch);
            }
        }

        public virtual void Draw(SpriteBatch spriteBatch)
        {
            if (ExpandedEntries.Count > MaxVisibleEntries)
                DrawScrollBar(spriteBatch);

            // @todo Why the hell do we need to know the exact type of item??
            if (DraggedEntry?.item is QueueItem queueItem)
            {
                Vector2 bCursor = Mouse.GetState().Pos() + DraggedOffset;
                var tCursor = new Vector2(bCursor.X + 40f, bCursor.Y);
                var r = new Rectangle((int)bCursor.X, (int)bCursor.Y, 29, 30);
                var pbRect = new Rectangle((int)tCursor.X, (int)tCursor.Y + Fonts.Arial12Bold.LineSpacing, 150, 18);
                var pb = new ProgressBar(pbRect, queueItem.Cost, queueItem.productionTowards);

                if (queueItem.isBuilding)
                {
                    spriteBatch.Draw(ResourceManager.Texture($"Buildings/icon_{queueItem.Building.Icon}_48x48"), r, Color.White);
                    spriteBatch.DrawString(Fonts.Arial12Bold, queueItem.Building.Name, tCursor, Color.White);
                    pb.Draw(spriteBatch);
                }
                else if (queueItem.isShip)
                {
                    spriteBatch.Draw(ResourceManager.HullsDict[queueItem.sData.Hull].Icon, r, Color.White);
                    spriteBatch.DrawString(Fonts.Arial12Bold, queueItem.sData.Name, tCursor, Color.White);
                    pb.Draw(spriteBatch);
                }
                else if (queueItem.isTroop)
                {
                    Troop template = ResourceManager.GetTroopTemplate(queueItem.troopType);
                    template.Draw(spriteBatch, r);
                    spriteBatch.DrawString(Fonts.Arial12Bold, queueItem.troopType, tCursor, Color.White);
                    pb.Draw(spriteBatch);
                }
            }
        }

        public void DrawDraggedEntry(SpriteBatch batch)
        {
            if (DraggedEntry != null)
                batch.FillRectangle(DraggedEntry.Rect, new Color(0, 0, 0, 150));
        }

        private float PercentViewed => MaxVisibleEntries / (float)ExpandedEntries.Count;
        private float StartingPercent => FirstVisibleIndex / (float)ExpandedEntries.Count;

        private void UpdateScrollBar(bool blue = false)
        {
            int scrollStart = (int)(StartingPercent * ScrollBarHousing.Height);
            int scrollEnd = (int)(ScrollBarHousing.Height * PercentViewed);
            int width = blue ? ResourceManager.Texture("ResearchMenu/scrollbar_bar_mid").Width : ScrollBarMidMarker.Width;

            ScrollBar = new Rectangle(ScrollBarHousing.X, ScrollBarHousing.Y + scrollStart, width, scrollEnd);
        }

        private bool HandleScrollUpDownButtons(InputState input)
        {
            if (!input.InGameSelect)
                return false;
            if (ScrollUp.HitTest(input.CursorPosition))
            {
                if (FirstVisibleIndex > 0)
                    --FirstVisibleIndex;
                UpdateScrollBar();
                return true;
            }
            if (ScrollDown.HitTest(input.CursorPosition))
            {
                if (FirstVisibleIndex + MaxVisibleEntries < ExpandedEntries.Count)
                    ++FirstVisibleIndex;
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
            FirstVisibleIndex = (int)(ExpandedEntries.Count * mousePosAsPct);
            if (FirstVisibleIndex + MaxVisibleEntries >= ExpandedEntries.Count)
            {
                FirstVisibleIndex = ExpandedEntries.Count - MaxVisibleEntries;
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

        private bool HandleMouseScrollUpDown(InputState input)
        {
            if (!Parent.Menu.HitTest(input.CursorPosition))
                return false;
            if (input.ScrollIn)
            {
                if (FirstVisibleIndex > 0)
                    --FirstVisibleIndex;
                UpdateScrollBar();
                return true;
            }
            if (input.ScrollOut)
            {
                if (FirstVisibleIndex + MaxVisibleEntries < ExpandedEntries.Count)
                    ++FirstVisibleIndex;
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
                        for (int i = FirstVisibleIndex; i < ExpandedEntries.Count && i < FirstVisibleIndex + MaxVisibleEntries; i++)
                        {
                            Entry e = ExpandedEntries[i];
                            if (!e.CheckHover(input))
                                continue;
                            DraggedEntry = e;
                            DraggedOffset = e.TopLeft - input.CursorPosition;
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

            int dragged = Entries.FirstIndexOf(e => e.Rect == DraggedEntry.Rect);

            for (int i = FirstVisibleIndex; i < Entries.Count && i < FirstVisibleIndex + MaxVisibleEntries; i++)
            {
                if (Entries[i].CheckHover(input) && dragged != -1 && onDragElement(i, dragged))
                    break;
            }
        }

        public virtual bool HandleInput(InputState input)
        {
            bool hit = HandleScrollBar(input);
            hit |= HandleMouseScrollUpDown(input);
            HandleDraggable(input);

            HandleElementDragging(input, (newIndex, dragged) =>
            {
                if (newIndex < dragged)
                {
                    Entry toReplace = Entries[newIndex];
                    Entries[newIndex] = Entries[dragged];
                    Entries[newIndex].Rect = toReplace.Rect;
                    Entries[dragged] = toReplace;
                    Entries[dragged].Rect = DraggedEntry.Rect;
                    DraggedEntry = Entries[newIndex];
                    return true;
                }
                return false;
            });
            UpdateListElements();
            return hit;
        }

        public bool HandleInput(InputState input, Planet p)
        {
            bool hit = HandleScrollBar(input);
            hit |= HandleMouseScrollUpDown(input);
            HandleDraggable(input);

            HandleElementDragging(input, (newIndex, dragged) =>
            {
                if (newIndex < dragged)
                {
                    Entry toReplace = Entries[newIndex];
                    Entries[newIndex] = Entries[dragged];
                    Entries[newIndex].Rect = toReplace.Rect;
                    p.ConstructionQueue[newIndex] = p.ConstructionQueue[dragged];
                    Entries[dragged] = toReplace;
                    Entries[dragged].Rect = DraggedEntry.Rect;
                    p.ConstructionQueue[dragged] = toReplace.item as QueueItem;
                    DraggedEntry = Entries[newIndex];
                    return true;
                }
                if (newIndex > dragged)
                {
                    Entry toRemove = Entries[dragged];
                    for (int j = dragged + 1; j <= newIndex; j++)
                    {
                        Entries[j].Rect = Entries[j - 1].Rect;
                        Entries[j - 1] = Entries[j];
                        p.ConstructionQueue[j - 1] = p.ConstructionQueue[j];
                    }
                    toRemove.Rect = Entries[newIndex].Rect;
                    Entries[newIndex] = toRemove;
                    p.ConstructionQueue[newIndex] = toRemove.item as QueueItem;
                    DraggedEntry = Entries[newIndex];
                }
                return false;
            });
            UpdateListElements();

            // @todo This cannot be implemented before duplicate HandleInput's are removed
            //if (!hit)
            //    hit = HandleItemInput(input);
            return hit;
        }

        public void TransitionUpdate(Rectangle r)
        {
            ScrollUp = new Rectangle(r.X + r.Width - 20, r.Y + 30, ScrollBarArrowUp.Width, ScrollBarArrowUp.Height);
            ScrollDown = new Rectangle(r.X + r.Width - 20, r.Y + r.Height - 14, ScrollBarArrorDown.Width, ScrollBarArrorDown.Height);
            ScrollBarHousing = new Rectangle(ScrollUp.X + 1, ScrollUp.Y + ScrollUp.Height + 3, ScrollBarMidMarker.Width, ScrollDown.Y - ScrollUp.Y - ScrollUp.Height - 6);
            int j = 0;
            int end = EntriesEnd;
            for (int i = 0; i < Entries.Count; i++)
            {
                Entry e = Entries[i];
                if (i >= FirstVisibleIndex && i < end)
                    e.UpdateClickRect(r.Pos(), j++);
                else
                    e.SetUnclickable();
            }
        }

        private void UpdateListElements()
        {
            ExpandedEntries.Clear();
            foreach (Entry e in Entries)
                e.ExpandSubEntries(ExpandedEntries);

            FirstVisibleIndex = FirstVisibleIndex.Clamped(0, 
                Math.Max(0, ExpandedEntries.Count - MaxVisibleEntries));

            int j = 0;
            for (int i = 0; i < ExpandedEntries.Count; i++)
            {
                Entry e = ExpandedEntries[i];
                if (i >= FirstVisibleIndex)
                    e.UpdateClickRect(Parent.Menu.Pos(), j++);
                else
                    e.SetUnclickable();
            }

            UpdateScrollBar();
        }

        private bool HandleItemInput(InputState input)
        {
            int end = ExpandedEntriesEnd;
            for (int i = FirstVisibleIndex; i < end; ++i)
            {
                Entry e = ExpandedEntries[i];
                if (e.item is UIElement element && element.HandleInput(input))
                    return true;
            }
            return false;
        }

        public class Entry
        {
            // entries with subitems can be expanded or collapsed via category title
            public bool Expanded { get; private set; }

            // true if item is currently being hovered over with mouse cursor
            public bool Hovered { get; private set; }

            public Rectangle Rect;
            public object item;

            // Plus and Edit buttons in ColonyScreen build list
            private readonly bool Plus;
            private readonly bool Edit;
            private bool PlusHover;
            private bool EditHover;
            private Rectangle PlusRect;
            private Rectangle EditRect;

            private readonly ScrollList List;

            private Rectangle Up;
            private Rectangle Down;
            private Rectangle Cancel;
            private Rectangle Apply;

            private readonly Array<Entry> SubEntries = new Array<Entry>();

            public override string ToString() => $"Y:{Rect.Y} | {item}";

            public Entry(ScrollList list, object item, bool plus, bool edit)
            {
                List = list;
                this.item = item;
                Plus = plus;
                Edit = edit;
            }

            public T Get<T>() => (T)item;
            public bool Is<T>() => item is T;
            public T As<T>() where T : class => item as T;
            public bool TryGet<T>(out T outItem)
            {
                if (item is T theItem)
                { outItem = theItem; return true; }
                outItem = default(T);
                return false;
            }

            public Selector CreateSelector() => new Selector(Rect);
            public Vector2 TopLeft => new Vector2(Rect.X, Rect.Y);
            public int X => Rect.X;
            public int Y => Rect.Y;
            public int W => Rect.Width;
            public int H => Rect.Height;
            public int Right => Rect.X + Rect.Width;
            public int CenterX => Rect.X + Rect.Width / 2;
            public int CenterY => Rect.Y + Rect.Height / 2;

            public void AddSubItem(object o, bool addAndEdit = false)
            {
                SubEntries.Add(new Entry(List, o, addAndEdit, addAndEdit));
            }

            public bool RemoveSub(Entry e)
            {
                if (SubEntries.IsEmpty)
                    return false;

                foreach (Entry sub in SubEntries)
                    if (sub.RemoveSub(e)) return true;

                return SubEntries.Remove(e);
            }

            public bool RemoveFirstSubIf(Predicate<Entry> predicate)
            {
                if (SubEntries.IsEmpty)
                    return false;

                foreach (Entry sub in SubEntries)
                    if (sub.RemoveFirstSubIf(predicate)) return true;

                return SubEntries.RemoveFirstIf(predicate);
            }

            public void ExpandSubEntries(Array<Entry> entries)
            {
                entries.Add(this);
                if (Expanded)
                    entries.AddRange(SubEntries);
            }

            public void Expand(bool expanded)
            {
                if (Expanded == expanded)
                    return;
                Expanded = expanded;
                if (!expanded)
                {
                    List.FirstVisibleIndex = List.FirstVisibleIndex - SubEntries.Count;
                    if (List.FirstVisibleIndex < 0)
                        List.FirstVisibleIndex = 0;
                }
                List.UpdateListElements();
            }

            public bool WasClicked(InputState input)
            {
                return input.LeftMouseClick && CheckHover(input);
            }

            public void SetUnclickable()
            {
                Rect = new Rectangle(-500, -500, 0, 0);
            }

            public bool WasPlusHovered(InputState input)   => PlusRect.HitTest(input.CursorPosition);
            public bool WasUpHovered(InputState input)     => Up.HitTest(input.CursorPosition);
            public bool WasDownHovered(InputState input)   => Down.HitTest(input.CursorPosition);
            public bool WasCancelHovered(InputState input) => Cancel.HitTest(input.CursorPosition);
            public bool WasApplyHovered(InputState input)  => Apply.HitTest(input.CursorPosition);

            public bool CheckHover(MouseState mouse) => CheckHover(mouse.Pos());
            public bool CheckHover(InputState input) => CheckHover(input.CursorPosition);
            public bool CheckHover(Vector2 mousePos)
            {
                bool wasHovered = Hovered;
                Hovered = Rect.HitTest(mousePos);

                if (!wasHovered && Hovered)
                    GameAudio.PlaySfxAsync("sd_ui_mouseover");

                return Hovered;
            }

            public bool CheckHoverNoSound(Vector2 mousePos)
            {
                Hovered = Rect.HitTest(mousePos);
                return Hovered;
            }

            public bool CheckPlus(InputState input) => (PlusHover = PlusRect.HitTest(input.CursorPosition));
            public bool CheckEdit(InputState input) => (EditHover = EditRect.HitTest(input.CursorPosition));

            public void DrawPlusEdit(SpriteBatch spriteBatch)
            {
                DrawPlus(spriteBatch);
                DrawEdit(spriteBatch);
            }

            public void DrawPlus(SpriteBatch spriteBatch)
            {
                if (Plus)
                {
                    string plus = PlusHover ? "NewUI/icon_build_add_hover2"
                                  : Hovered ? "NewUI/icon_build_add_hover1"
                                            : "NewUI/icon_build_add";
                    spriteBatch.Draw(ResourceManager.Texture(plus), PlusRect, Color.White);
                }
            }

            public void DrawEdit(SpriteBatch spriteBatch)
            {
                if (Edit)
                {
                    string edit = EditHover ? "NewUI/icon_build_edit_hover2"
                                  : Hovered ? "NewUI/icon_build_edit_hover1"
                                            : "NewUI/icon_build_edit";
                    spriteBatch.Draw(ResourceManager.Texture(edit), EditRect, Color.White);
                }
            }

            public void DrawUpDownApplyCancel(SpriteBatch batch, InputState input)
            {
                Vector2 pos = input.CursorPosition;
                if (!Hovered)
                {
                    batch.Draw(ResourceManager.Texture("NewUI/icon_queue_arrow_up"), Up, Color.White);
                    batch.Draw(ResourceManager.Texture("NewUI/icon_queue_arrow_down"), Down, Color.White);
                    batch.Draw(ResourceManager.Texture("NewUI/icon_queue_rushconstruction"), Apply, Color.White);
                    batch.Draw(ResourceManager.Texture("NewUI/icon_queue_delete"), Cancel, Color.White);
                    return;
                }

                batch.Draw(ResourceManager.Texture("NewUI/icon_queue_arrow_up_hover1"), Up, Color.White);
                batch.Draw(ResourceManager.Texture("NewUI/icon_queue_arrow_down_hover1"), Down, Color.White);
                batch.Draw(ResourceManager.Texture("NewUI/icon_queue_rushconstruction_hover1"), Apply, Color.White);
                batch.Draw(ResourceManager.Texture("NewUI/icon_queue_delete_hover1"), Cancel, Color.White);

                if (Up.HitTest(pos))
                {
                    batch.Draw(ResourceManager.Texture("NewUI/icon_queue_arrow_up_hover2"), Up, Color.White);
                }
                if (Empire.Universe.IsActive)
                {
                    if (Down.HitTest(pos))
                    {
                        batch.Draw(ResourceManager.Texture("NewUI/icon_queue_arrow_down_hover2"), Down, Color.White);
                    }
                    if (Apply.HitTest(pos))
                    {
                        batch.Draw(ResourceManager.Texture("NewUI/icon_queue_rushconstruction_hover2"), Apply, Color.White);
                        ToolTip.CreateTooltip(50);
                    }
                    if (Cancel.HitTest(pos))
                    {
                        batch.Draw(ResourceManager.Texture("NewUI/icon_queue_delete_hover2"), Cancel, Color.White);
                        ToolTip.CreateTooltip(53);
                    }
                }
            }

            public void DrawCancel(SpriteBatch batch, InputState input, string toolTipText = null)
            {
                if (Hovered)
                {
                    batch.Draw(ResourceManager.Texture("NewUI/icon_queue_delete_hover1"), Cancel, Color.White);
                    if (WasCancelHovered(input))
                    {
                        batch.Draw(ResourceManager.Texture("NewUI/icon_queue_delete_hover2"), Cancel, Color.White);
                        if (toolTipText.NotEmpty())
                            ToolTip.CreateTooltip(toolTipText);
                        else
                            ToolTip.CreateTooltip(78);
                    }
                }
                else
                {
                    batch.Draw(ResourceManager.Texture("NewUI/icon_queue_delete"), Cancel, Color.White);
                }
            }

            public void UpdateClickRect(Point topLeft, int j)
            {
                int height = List.EntryHeight;
                var r = new Rectangle(topLeft.X + 20, topLeft.Y + 35 + j * height, List.Parent.Menu.Width - 40, height);
                int right = r.X + r.Width;
                int iconY = r.Y + 15;
                Rect = r;

                Texture2D addIcon = List.BuildAddIcon;
                Texture2D upIcon  = List.ArrowUpIcon;

                if (Plus) PlusRect  = new Rectangle(right - 30, iconY - addIcon.Height / 2, addIcon.Width, addIcon.Height);
                if (Edit) EditRect = new Rectangle(right - 60, iconY - addIcon.Height / 2, addIcon.Width, addIcon.Height);

                int offset = 0;
                bool all = List.Controls == ListControls.All;
                if (all) Up    = new Rectangle(right - (offset += 30), iconY - upIcon.Height / 2, upIcon.Width, upIcon.Height);
                if (all) Down  = new Rectangle(right - (offset += 30), iconY - upIcon.Height / 2, upIcon.Width, upIcon.Height);
                if (all) Apply = new Rectangle(right - (offset += 30), iconY - upIcon.Height / 2, upIcon.Width, upIcon.Height);
                        Cancel = new Rectangle(right - (offset += 30), iconY - upIcon.Height / 2, upIcon.Width, upIcon.Height);
            }
        }
    }

    internal sealed class ScrollListDebugView
    {
        private readonly ScrollList List;

        public ScrollListDebugView(ScrollList list)
        {
            List = list;
        }

        [DebuggerBrowsable(DebuggerBrowsableState.RootHidden)]
        public ScrollList.Entry[] Items
        {
            get
            {
                IReadOnlyList<ScrollList.Entry> allEntries = List.AllEntries;
                var items = new ScrollList.Entry[allEntries.Count];
                for (int i = 0; i < items.Length; ++i)
                    items[i] = allEntries[i];
                return items;
            }
        }
    }
}