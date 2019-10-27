using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using Ship_Game.Audio;

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

    public enum ListStyle
    {
        Default,
        Blue, // used in research screen
    }

    public enum DragEvent
    {
        Begin,
        End,
    }

    [DebuggerTypeProxy(typeof(ScrollListDebugView<>))]
    [DebuggerDisplay("Entries = {Entries.Count}  Expanded = {ExpandedEntries.Count}")]
    public class ScrollList<T> : UIElementV2
        where T : ScrollList<T>.Entry
    {
        public readonly Submenu ParentMenu;
        Rectangle ScrollUp;
        Rectangle ScrollDown;
        Rectangle ScrollBarHousing;
        Rectangle ScrollBar;

        readonly int EntryHeight = 40;
        int ScrollBarHover;
        int StartDragPos;
        public bool DraggingScrollBar { get; private set; }
        float ScrollBarStartDragPos;
        float ClickTimer;
        float TimerDelay = 0.05f;
        readonly int YOffset;

        readonly int MaxVisibleEntries;
        public int FirstVisibleIndex;
        readonly Array<T> Entries         = new Array<T>();
        readonly Array<T> ExpandedEntries = new Array<T>(); // Flattened entries
        readonly bool IsDraggable;
        public T DraggedEntry;
        public Vector2 DraggedOffset;

        public ListStyle Style;
        readonly ListControls Controls = ListControls.All;

        // Automatic Selection highlight
        public Selector SelectionBox;

        // EVENT: Called when a new item is focused with mouse
        //        @note This is called again with <null> when mouse leaves focus
        public Action<T> OnHovered;

        // EVENT: Called when an item is clicked on
        public Action<T> OnClick;

        // EVENT: Called when an item is double-clicked
        public Action<T> OnDoubleClick;

        // EVENT: Called when an item drag starts or item drag ends
        public Action<T, DragEvent> OnDrag;

        public ScrollList(Submenu p, ListOptions options = ListOptions.None, ListStyle style = ListStyle.Default)
        {
            ParentMenu = p;
            Style = style;
            MaxVisibleEntries = (p.Rect.Height - 25) / 40;
            IsDraggable = options == ListOptions.Draggable;
            YOffset = 30;
            this.PerformLayout();
        }

        public ScrollList(Submenu p, int eHeight, ListControls controls = ListControls.All, ListStyle style = ListStyle.Default)
        {
            ParentMenu = p;
            Style = style;
            EntryHeight = eHeight;
            Controls = controls;
            MaxVisibleEntries = (p.Rect.Height - 25) / EntryHeight;
            YOffset = 30;
            this.PerformLayout();
        }

        public ScrollList(Submenu p, int eHeight, bool realRect, ListStyle style = ListStyle.Default)
        {
            ParentMenu = p;
            Style = style;
            EntryHeight = eHeight;
            MaxVisibleEntries = p.Rect.Height / EntryHeight;
            YOffset = 5;
            this.PerformLayout();
        }

        public virtual void OnItemHovered(T item)
        {
            OnHovered?.Invoke(item);
        }

        public virtual void OnItemClicked(T item)
        {
            OnClick?.Invoke(item);
        }

        public virtual void OnItemDoubleClicked(T item)
        {
            OnDoubleClick?.Invoke(item);
        }

        public virtual void OnItemDragged(T item, DragEvent evt)
        {
            OnDrag?.Invoke(item, evt);
        }

        class StyleTextures
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

            public StyleTextures(string folder)
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
        }

        static int ContentId;
        static StyleTextures[] Styling;

        StyleTextures GetStyle()
        {
            if (Styling != null && ContentId == ResourceManager.ContentId)
                return Styling[(int)Style];

            ContentId = ResourceManager.ContentId;
            Styling = new []
            {
                new StyleTextures("NewUI"),
                new StyleTextures("ResearchMenu"),
            };
            return Styling[(int)Style];
        }

        public override void PerformLayout()
        {
            Submenu p = ParentMenu;
            StyleTextures s = GetStyle();
            Rect = p.Rect;
            int x = (int)(p.X + p.Width - 20f);
            ScrollUp   = new Rectangle(x, p.Rect.Y + YOffset,            s.ScrollBarArrowUp.Width,   s.ScrollBarArrowUp.Height);
            ScrollDown = new Rectangle(x, p.Rect.Y + p.Rect.Height - 14, s.ScrollBarArrowDown.Width, s.ScrollBarArrowDown.Height);
            ScrollBarHousing = new Rectangle(ScrollUp.X + 1, ScrollUp.Y + ScrollUp.Height + 3, s.ScrollBarMid.Width, ScrollDown.Y - ScrollUp.Y - ScrollUp.Height - 6);
            ScrollBar        = new Rectangle(ScrollBarHousing.X, ScrollBarHousing.Y, s.ScrollBarMid.Width, 0);
            base.PerformLayout();
        }

        public T AddItem(T entry)
        {
            entry.List = this;
            Entries.Add(entry);
            UpdateListElements();
            return entry;
        }

        public void SetItems(IEnumerable<T> newItems)
        {
            Entries.Clear();
            ExpandedEntries.Clear();
            foreach (T item in newItems)
            {
                item.List = this;
                Entries.Add(item);
            }
            UpdateListElements();
        }

        bool RemoveSub(T e)
        {
            foreach (T entry in Entries)
                if (entry.RemoveSub(e)) return true;
            return false;
        }

        public void Remove(T e)
        {
            if (!RemoveSub(e))
                Entries.Remove(e);

            if (ExpandedEntries.Remove(e))
                UpdateListElements();
        }

        bool RemoveSubItem(Predicate<T> predicate)
        {
            foreach (T entry in Entries)
                if (entry.RemoveFirstSubIf(predicate)) return true;
            return false;
        }

        public void RemoveItem(T toRemove)
        {
            bool ItemPredicate(T e) => e == toRemove;

            if (!RemoveSubItem(ItemPredicate))
                Entries.RemoveFirst(ItemPredicate);

            if (ExpandedEntries.RemoveFirst(ItemPredicate))
                UpdateListElements();
        }

        public void RemoveFirstIf(Predicate<T> predicate)
        {
            if (!RemoveSubItem(predicate))
                Entries.RemoveFirst(predicate);

            if (ExpandedEntries.RemoveFirst(predicate))
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

        public T EntryAt(int index) => Entries[index];

        public T FirstItem()
        {
            return ExpandedEntries[0];
        }

        public T ItemAt(int index)
        {
            return ExpandedEntries[index];
        }

        public T ItemAtTop()
        {
            return ExpandedEntries[FirstVisibleIndex];
        }

        public int IndexOf(Func<T, bool> predicate)
        {
            for (int i = 0; i < Entries.Count; ++i)
                if (predicate(Entries[i]))
                    return i;
            return -1;
        }

        public void Sort<TValue>(Func<T, TValue> predicate)
        {
            T[] sorted = Entries.OrderBy(predicate).ToArray();
            Entries.Clear();
            Entries.AddRange(sorted);
            UpdateListElements();
        }

        public void SortDescending<TValue>(Func<T, TValue> predicate)
        {
            T[] sorted = Entries.OrderByDescending(predicate).ToArray();
            Entries.Clear();
            Entries.AddRange(sorted);
            UpdateListElements();
        }

        public IReadOnlyList<T> AllEntries         => Entries;
        public IReadOnlyList<T> AllExpandedEntries => ExpandedEntries;
        public int NumEntries         => Entries.Count;
        public int NumExpandedEntries => ExpandedEntries.Count;

        // @note Optimized for speed
        T[] CopyVisibleEntries(Array<T> entries)
        {
            int start = FirstVisibleIndex;
            int end = Math.Min(entries.Count, FirstVisibleIndex + MaxVisibleEntries);
            int count = end - start;

            T[] items = count <= 0 ? Empty<T>.Array : new T[count];
            for (int i = 0; i < items.Length; ++i)
                items[i] = entries[start++];
            return items;
        }

        public T[] VisibleEntries         => CopyVisibleEntries(Entries);
        public T[] VisibleExpandedEntries => CopyVisibleEntries(ExpandedEntries);
        public Array<T> AllItems()         => new Array<T>(Entries);


        public void Reset()
        {
            Entries.Clear();
            ExpandedEntries.Clear();
            FirstVisibleIndex = 0;
            UpdateListElements();
        }

        void DrawScrollBar(SpriteBatch batch)
        {
            StyleTextures s = GetStyle();
            int updownsize = (ScrollBar.Height - s.ScrollBarMid.Height) / 2;
            var up  = new Rectangle(ScrollBar.X, ScrollBar.Y, ScrollBar.Width, updownsize);
            var mid = new Rectangle(ScrollBar.X, ScrollBar.Y + updownsize, ScrollBar.Width, s.ScrollBarMid.Height);
            var bot = new Rectangle(ScrollBar.X, mid.Y + mid.Height, ScrollBar.Width, updownsize);
            if (ScrollBarHover == 0)
            {
                batch.Draw(s.ScrollBarUpDown,  up, Color.White);
                batch.Draw(s.ScrollBarMid,    mid, Color.White);
                batch.Draw(s.ScrollBarUpDown, bot, Color.White);
            }
            else if (ScrollBarHover == 1)
            {
                batch.Draw(s.ScrollBarUpDownHover1,  up, Color.White);
                batch.Draw(s.ScrollBarMidHover1,    mid, Color.White);
                batch.Draw(s.ScrollBarUpDownHover1, bot, Color.White);
            }
            else if (ScrollBarHover == 2)
            {
                batch.Draw(s.ScrollBarUpDownHover2,  up, Color.White);
                batch.Draw(s.ScrollBarMidHover2,    mid, Color.White);
                batch.Draw(s.ScrollBarUpDownHover2, bot, Color.White);
            }
            Vector2 mousepos = Mouse.GetState().Pos();
            batch.Draw(ScrollUp.HitTest(mousepos)   ? s.ScrollBarArrowUpHover1   : s.ScrollBarArrowUp, ScrollUp, Color.White);
            batch.Draw(ScrollDown.HitTest(mousepos) ? s.ScrollBarArrowDownHover1 : s.ScrollBarArrowDown, ScrollDown, Color.White);
        }

        public override void Draw(SpriteBatch batch)
        {
            if (ExpandedEntries.Count > MaxVisibleEntries)
                DrawScrollBar(batch);

            DrawChildElements(batch);
            SelectionBox?.Draw(batch);

            if (DraggedEntry != null)
            {
                DraggedEntry.Pos = GameBase.ScreenManager.input.CursorPosition + DraggedOffset;
                batch.FillRectangle(DraggedEntry.Rect, new Color(0, 0, 0, 150));
                DraggedEntry.Draw(batch);
            }
        }

        float PercentViewed   => MaxVisibleEntries / (float)ExpandedEntries.Count;
        float StartingPercent => FirstVisibleIndex / (float)ExpandedEntries.Count;

        void UpdateScrollBar()
        {
            StyleTextures s = GetStyle();
            int scrollStart = (int)(StartingPercent * ScrollBarHousing.Height);
            int scrollEnd = (int)(ScrollBarHousing.Height * PercentViewed);
            int width = s.ScrollBarMid.Width;

            ScrollBar = new Rectangle(ScrollBarHousing.X, ScrollBarHousing.Y + scrollStart, width, scrollEnd);
        }

        bool HandleScrollUpDownButtons(InputState input)
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

        bool HandleScrollDragInput(InputState input)
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

        bool HandleScrollBarDragging(InputState input)
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

        bool HandleScrollBar(InputState input)
        {
            bool hit = HandleScrollUpDownButtons(input);
            hit |= HandleScrollDragInput(input);
            hit |= HandleScrollBarDragging(input);

            if (DraggingScrollBar && input.LeftMouseUp)
                DraggingScrollBar = false;
            return hit;
        }

        bool HandleMouseScrollUpDown(InputState input)
        {
            if (!ParentMenu.HitTest(input.CursorPosition))
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

        void HandleDraggable(InputState input)
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
                        Vector2 cursor = input.CursorPosition;
                        for (int i = FirstVisibleIndex; i < ExpandedEntries.Count && i < FirstVisibleIndex + MaxVisibleEntries; i++)
                        {
                            T e = ExpandedEntries[i];
                            if (e.Rect.HitTest(cursor))
                            {
                                DraggedEntry = e;
                                DraggedOffset = e.TopLeft - input.CursorPosition;
                                OnItemDragged(DraggedEntry, DragEvent.Begin);
                                break;
                            }
                        }
                    }
                }
            }
            if (input.LeftMouseUp)
            {
                OnItemDragged(DraggedEntry, DragEvent.End);
                ClickTimer = 0f;
                DraggedEntry = null;
            }
        }

        void HandleElementDragging(InputState input)
        {
            if (DraggedEntry == null || !input.LeftMouseDown)
                return;

            Vector2 cursor = input.CursorPosition;
            int dragged = Entries.FirstIndexOf(e => e.Rect == DraggedEntry.Rect);

            for (int i = FirstVisibleIndex; i < Entries.Count && i < FirstVisibleIndex + MaxVisibleEntries; i++)
            {
                if (Entries[i].Rect.HitTest(cursor) && dragged != -1)
                {
                    if (i < dragged)
                    {
                        T toReplace = Entries[i];
                        Entries[i] = Entries[dragged];
                        Entries[dragged] = toReplace;
                        DraggedEntry = Entries[i];
                        break;
                    }
                    if (i > dragged)
                    {
                        T toRemove = Entries[dragged];
                        for (int j = dragged + 1; j <= i; j++)
                        {
                            Entries[j - 1] = Entries[j];
                        }
                        Entries[i] = toRemove;
                        DraggedEntry = Entries[i];
                        break;
                    }
                }
            }
        }

        public override bool HandleInput(InputState input)
        {
            bool hit = HandleScrollBar(input);
            hit |= HandleMouseScrollUpDown(input);
            HandleDraggable(input);
            HandleElementDragging(input);
            UpdateListElements();
            if (!hit)
                return HandleInputChildElements(input);
            return hit;
        }

        public void TransitionUpdate(Rectangle r)
        {
            StyleTextures s = GetStyle();
            ScrollUp = new Rectangle(r.X + r.Width - 20, r.Y + 30, s.ScrollBarArrowUp.Width, s.ScrollBarArrowUp.Height);
            ScrollDown = new Rectangle(r.X + r.Width - 20, r.Y + r.Height - 14, s.ScrollBarArrowDown.Width, s.ScrollBarArrowDown.Height);
            ScrollBarHousing = new Rectangle(ScrollUp.X + 1, ScrollUp.Y + ScrollUp.Height + 3, s.ScrollBarMid.Width, ScrollDown.Y - ScrollUp.Y - ScrollUp.Height - 6);
            int j = 0;
            int end = Math.Min(Entries.Count, FirstVisibleIndex + MaxVisibleEntries);
            for (int i = 0; i < Entries.Count; i++)
            {
                T e = Entries[i];
                if (i >= FirstVisibleIndex && i < end)
                    e.UpdateClickRect(r.Pos(), j++);
                else
                    e.SetUnclickable();
            }
        }

        public void UpdateListElements()
        {
            ExpandedEntries.Clear();
            foreach (T e in Entries)
                e.GetFlattenedVisibleExpandedEntries(ExpandedEntries);

            FirstVisibleIndex = FirstVisibleIndex.Clamped(0,
                Math.Max(0, ExpandedEntries.Count - MaxVisibleEntries));

            int j = 0;
            for (int i = 0; i < ExpandedEntries.Count; i++)
            {
                T e = ExpandedEntries[i];
                if (i >= FirstVisibleIndex)
                    e.UpdateClickRect(ParentMenu.Rect.Pos(), j++);
                else
                    e.SetUnclickable();
            }

            UpdateScrollBar();
        }

        
        bool HandleInputChildElements(InputState input)
        {
            for (int i = FirstVisibleIndex; i < ExpandedEntries.Count && i < FirstVisibleIndex + MaxVisibleEntries; i++)
            {
                T item = ExpandedEntries[i];
                if (item.HandleInput(input))
                {
                    // only show selector if item doesn't have child elements (it's a header item)
                    if (!item.HasSubEntries)
                    {
                        SelectionBox = new Selector(item.Rect);
                    }
                    return true;
                }
            }

            // No longer hovering over the scroll list? Clear the SelectionBox
            if (!ParentMenu.HitTest(input.CursorPosition))
            {
                SelectionBox = null;
            }
            return false;
        }

        void DrawChildElements(SpriteBatch batch)
        {
            for (int i = FirstVisibleIndex; i < ExpandedEntries.Count && i < FirstVisibleIndex + MaxVisibleEntries; i++)
            {
                ExpandedEntries[i].Draw(batch);
            }
        }

        public class Entry : UIElementContainer
        {
            public ScrollList<T> List;

            // entries with subitems can be expanded or collapsed via category title
            public bool Expanded { get; private set; }

            // true if item is currently being hovered over with mouse cursor
            public bool Hovered { get; private set; }

            // Plus and Edit buttons in ColonyScreen build list
            readonly bool Plus;
            readonly bool Edit;
            public bool PlusHover { get; private set; }
            public bool EditHover { get; private set; }
            Rectangle PlusRect;
            Rectangle EditRect;

            public bool UpHover     { get; private set; }
            public bool DownHover   { get; private set; }
            public bool CancelHover { get; private set; }
            public bool ApplyHover  { get; private set; }
            Rectangle Up;
            Rectangle Down;
            Rectangle Cancel;
            Rectangle Apply;

            // The current visible index of this Entry
            public int VisibleIndex { get; protected set; }

            readonly Array<T> SubEntries = new Array<T>();
            public bool HasSubEntries => SubEntries.NotEmpty;

            public Entry()
            {
            }
            public Entry(bool plus, bool edit)
            {
                Plus = plus;
                Edit = edit;
            }
            protected Entry(UIElementV2 parent, Vector2 pos) : base(parent, pos)
            {
            }
            protected Entry(UIElementV2 parent, in Rectangle rect) : base(parent, rect)
            {
            }

            public void AddSubItem(T entry)
            {
                entry.List = List;
                SubEntries.Add(entry);
            }

            public bool RemoveSub(T e)
            {
                if (SubEntries.IsEmpty)
                    return false;

                foreach (T sub in SubEntries)
                    if (sub.RemoveSub(e)) return true;

                return SubEntries.Remove(e);
            }

            public bool RemoveFirstSubIf(Predicate<T> predicate)
            {
                if (SubEntries.IsEmpty)
                    return false;

                foreach (T sub in SubEntries)
                    if (sub.RemoveFirstSubIf(predicate)) return true;

                return SubEntries.RemoveFirst(predicate);
            }

            public void GetFlattenedVisibleExpandedEntries(Array<T> entries)
            {
                if (Visible)
                {
                    entries.Add((T)this);
                    if (Expanded)
                        foreach (T sub in SubEntries)
                            sub.GetFlattenedVisibleExpandedEntries(entries);
                }
            }

            public void Expand(bool expanded)
            {
                if (Expanded == expanded)
                    return;

                Expanded = expanded;
                if (!expanded)
                {
                    List.FirstVisibleIndex -= SubEntries.Count;
                    if (List.FirstVisibleIndex < 0)
                        List.FirstVisibleIndex = 0;
                }
                List.UpdateListElements();
            }

            public void SetUnclickable()
            {
                Rect = new Rectangle(-500, -500, 0, 0);
            }

            public void DrawCancel(SpriteBatch batch, string toolTipText = null)
            {
                StyleTextures s = List.GetStyle();
                if (Hovered)
                {
                    batch.Draw(s.QueueDeleteHover1, Cancel, Color.White);
                    if (CancelHover)
                    {
                        batch.Draw(s.QueueDeleteHover2, Cancel, Color.White);
                        if (toolTipText.NotEmpty())
                            ToolTip.CreateTooltip(toolTipText);
                        else
                            ToolTip.CreateTooltip(78);
                    }
                }
                else
                {
                    batch.Draw(s.QueueDelete, Cancel, Color.White);
                }
            }

            public void UpdateClickRect(Point topLeft, int ourIndex)
            {
                VisibleIndex = ourIndex;
                int height = List.EntryHeight;
                var r = new Rectangle(topLeft.X + 20, topLeft.Y + 35 + ourIndex * height, List.ParentMenu.Rect.Width - 40, height);
                int right = r.X + r.Width;
                int iconY = r.Y + 15;
                Rect = r;
                PerformLayout();

                StyleTextures s = List.GetStyle();
                SubTexture addIcon = s.BuildAdd;
                SubTexture upIcon  = s.QueueArrowUp;

                if (Plus) PlusRect = new Rectangle(right - 30, iconY - addIcon.Height / 2, addIcon.Width, addIcon.Height);
                if (Edit) EditRect = new Rectangle(right - 60, iconY - addIcon.Height / 2, addIcon.Width, addIcon.Height);

                int offset = 0;
                bool all = List.Controls == ListControls.All;
                if (all) Up    = new Rectangle(right - (offset += 30), iconY - upIcon.Height / 2, upIcon.Width, upIcon.Height);
                if (all) Down  = new Rectangle(right - (offset += 30), iconY - upIcon.Height / 2, upIcon.Width, upIcon.Height);
                if (all) Apply = new Rectangle(right - (offset += 30), iconY - upIcon.Height / 2, upIcon.Width, upIcon.Height);
                        Cancel = new Rectangle(right - (offset +  30), iconY - upIcon.Height / 2, upIcon.Width, upIcon.Height);
            }

            public override bool HandleInput(InputState input)
            {
                Vector2 pos = input.CursorPosition;
                if (Plus) PlusHover = PlusRect.HitTest(pos);
                if (Edit) EditHover = EditRect.HitTest(pos);

                UpHover     = Up.HitTest(input.CursorPosition);
                DownHover   = Down.HitTest(input.CursorPosition);
                CancelHover = Cancel.HitTest(input.CursorPosition);
                ApplyHover  = Apply.HitTest(input.CursorPosition);

                bool wasHovered = Hovered;
                Hovered = Rect.HitTest(input.CursorPosition);

                // Mouse entered this Entry
                if (!wasHovered && Hovered)
                {
                    GameAudio.ButtonMouseOver();
                    List.OnItemHovered(this as T);
                }

                if (Hovered)
                {
                    if (input.LeftMouseDoubleClick)
                    {
                        GameAudio.AcceptClick();
                        List.OnItemDoubleClicked(this as T);
                        return true;
                    }
                    if (input.LeftMouseClick)
                    {
                        GameAudio.AcceptClick();
                        if (SubEntries.NotEmpty)
                        {
                            Expand(!Expanded);
                        }
                        List.OnItemClicked(this as T);
                        return true;
                    }
                }

                return Hovered;
            }

            public override void Draw(SpriteBatch batch)
            {
                StyleTextures s = List.GetStyle();

                if (Plus)
                {
                    SubTexture plus = PlusHover ? s.BuildAddHover2
                                      : Hovered ? s.BuildAddHover1
                                                : s.BuildAdd;
                    batch.Draw(plus, PlusRect, Color.White);
                }

                if (Edit)
                {
                    SubTexture edit = EditHover ? s.BuildEditHover2
                                      : Hovered ? s.BuildEditHover1
                                                : s.BuildEdit;
                    batch.Draw(edit, EditRect, Color.White);
                }

                if (!Hovered)
                {
                    batch.Draw(s.QueueArrowUp,   Up,     Color.White);
                    batch.Draw(s.QueueArrowDown, Down,   Color.White);
                    batch.Draw(s.QueueRush,      Apply,  Color.White);
                    batch.Draw(s.QueueDelete,    Cancel, Color.White);
                    return;
                }

                batch.Draw(s.QueueArrowUpHover1,   Up,     Color.White);
                batch.Draw(s.QueueArrowDownHover1, Down,   Color.White);
                batch.Draw(s.QueueRushHover1,      Apply,  Color.White);
                batch.Draw(s.QueueDeleteHover1,    Cancel, Color.White);

                if (UpHover)
                {
                    batch.Draw(s.QueueArrowUpHover2, Up, Color.White);
                }
                if (DownHover)
                {
                    batch.Draw(s.QueueArrowDownHover2, Down, Color.White);
                }
                if (ApplyHover)
                {
                    batch.Draw(s.QueueRushHover2, Apply, Color.White);
                    ToolTip.CreateTooltip(50);
                }
                if (CancelHover)
                {
                    batch.Draw(s.QueueDeleteHover2, Cancel, Color.White);
                    ToolTip.CreateTooltip(53);
                }
            }
        }
    }

    internal sealed class ScrollListDebugView<T> where T : ScrollList<T>.Entry
    {
        readonly ScrollList<T> List;

        // ReSharper disable once UnusedMember.Global
        public ScrollListDebugView(ScrollList<T> list)
        {
            List = list;
        }

        [DebuggerBrowsable(DebuggerBrowsableState.RootHidden)]
        public ScrollList<T>.Entry[] Items
        {
            get
            {
                IReadOnlyList<ScrollList<T>.Entry> allEntries = List.AllEntries;
                var items = new ScrollList<T>.Entry[allEntries.Count];
                for (int i = 0; i < items.Length; ++i)
                    items[i] = allEntries[i];
                return items;
            }
        }
    }
}