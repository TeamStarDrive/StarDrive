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
        bool DraggingScrollBar;
        float ScrollBarStartDragPos;
        float ClickTimer;
        const float TimerDelay = 0.05f;
        readonly int YOffset;

        readonly int MaxVisibleEntries;
        readonly Array<T> Entries         = new Array<T>();
        readonly Array<T> ExpandedEntries = new Array<T>(); // Flattened entries
        readonly bool IsDraggable;
        T DraggedEntry;
        Vector2 DraggedOffset;

        // this controls the visual style of the ScrollList
        // can be freely changed at any point
        public ListStyle Style;
        readonly ListControls Controls = ListControls.All;

        // Automatic Selection highlight
        public Selector SelectionBox;

        // If TRUE, automatically draws Selection highlight around each ScrollList Item
        public bool EnableSelectionBox = true;

        // EVENT: Called when a new item is focused with mouse
        //        @note This is called again with <null> when mouse leaves focus
        public Action<T> OnHovered;

        // EVENT: Called when an item is clicked on
        public Action<T> OnClick;

        // EVENT: Called when an item is double-clicked
        public Action<T> OnDoubleClick;

        // If TRUE, allows automatic dragging of ScrollList Items
        public bool EnableElementDragging = false;

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

        public IReadOnlyList<T> AllEntries => Entries;
        public int NumEntries              => Entries.Count;

        // Item at non-flattened index: doesn't index hierarchical elements
        public T this[int index] => Entries[index];

        // @return The first visible item
        public T FirstItem => ExpandedEntries[FirstVisibleIndex];

        int FirstVisibleIndexValue;
        
        // Gets or Sets index of the topmost visible item in the list
        public int FirstVisibleIndex
        {
            get => FirstVisibleIndexValue;
            set 
            {
                if (FirstVisibleIndexValue != value)
                {
                    FirstVisibleIndexValue = value;
                    RequiresLayout = true;
                }
            }
        }

        public T AddItem(T entry)
        {
            entry.List = this;
            Entries.Add(entry);
            RequiresLayout = true;
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
            RequiresLayout = true;
        }

        bool RemoveSub(T e)
        {
            foreach (T entry in Entries)
                if (entry.RemoveSub(e))
                    return true;
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

        public void RemoveFirstIf(Predicate<T> predicate)
        {
            if (!RemoveSubItem(predicate))
                Entries.RemoveFirst(predicate);

            if (ExpandedEntries.RemoveFirst(predicate))
                UpdateListElements();
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

        public T[] VisibleExpandedEntries => CopyVisibleEntries(ExpandedEntries);
        float PercentViewed   => MaxVisibleEntries / (float)ExpandedEntries.Count;
        float StartingPercent => FirstVisibleIndex / (float)ExpandedEntries.Count;

        public void Reset()
        {
            Entries.Clear();
            ExpandedEntries.Clear();
            FirstVisibleIndex = 0;
            UpdateScrollBar();
        }


        #region ScrollList HandleInput

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

        bool HandleInputChildElements(InputState input)
        {
            int end = Math.Min(ExpandedEntries.Count, FirstVisibleIndex + MaxVisibleEntries);
            for (int i = FirstVisibleIndex; i < end; i++)
            {
                T item = ExpandedEntries[i];
                if (item.HandleInput(input))
                {
                    // only show selector if item doesn't have child elements (it's a header item)
                    if (!item.HasSubEntries && EnableSelectionBox)
                    {
                        SelectionBox = new Selector(item.Rect);
                    }
                    return true;
                }
            }

            // No longer hovering over the scroll list? Clear the SelectionBox
            if (EnableSelectionBox && !ParentMenu.HitTest(input.CursorPosition))
            {
                SelectionBox = null;
            }
            return false;
        }

        public override bool HandleInput(InputState input)
        {
            bool hit = HandleScrollBar(input);
            hit |= HandleMouseScrollUpDown(input);
            HandleDraggable(input);
            HandleElementDragging(input);
            if (!hit)
                return HandleInputChildElements(input);
            return hit;
        }

        #endregion

        #region ScrollList Update / PerformLayout
        
        public override void PerformLayout()
        {
            ScrollListStyleTextures s = ScrollListStyleTextures.Get(Style);
            Submenu p = ParentMenu;
            Rect = p.Rect;
            int x = (int)(p.X + p.Width - 20f);
            ScrollUp   = new Rectangle(x, p.Rect.Y + YOffset,            s.ScrollBarArrowUp.Width,   s.ScrollBarArrowUp.Height);
            ScrollDown = new Rectangle(x, p.Rect.Y + p.Rect.Height - 14, s.ScrollBarArrowDown.Width, s.ScrollBarArrowDown.Height);
            ScrollBarHousing = new Rectangle(ScrollUp.X + 1, ScrollUp.Y + ScrollUp.Height + 3, s.ScrollBarMid.Width, ScrollDown.Y - ScrollUp.Y - ScrollUp.Height - 6);
            ScrollBar        = new Rectangle(ScrollBarHousing.X, ScrollBarHousing.Y, s.ScrollBarMid.Width, 0);
            base.PerformLayout();
        }

        void UpdateScrollBar()
        {
            ScrollListStyleTextures s = ScrollListStyleTextures.Get(Style);
            int scrollStart = (int)(StartingPercent * ScrollBarHousing.Height);
            int scrollEnd = (int)(ScrollBarHousing.Height * PercentViewed);
            int width = s.ScrollBarMid.Width;

            ScrollBar = new Rectangle(ScrollBarHousing.X, ScrollBarHousing.Y + scrollStart, width, scrollEnd);
        }

        void UpdateItemPositions(Vector2 listTopLeft)
        {
            int visibleIndex = 0;
            int visibleEnd = Math.Min(Entries.Count, FirstVisibleIndex + MaxVisibleEntries);
            for (int i = 0; i < ExpandedEntries.Count; i++)
            {
                T e = ExpandedEntries[i];
                if (i >= FirstVisibleIndex && i < visibleEnd)
                {
                    e.VisibleIndex = visibleIndex++;
                    e.Rect = new Rectangle((int)listTopLeft.X + 20, (int)listTopLeft.Y + 35 + e.VisibleIndex * EntryHeight, 
                                           ParentMenu.Rect.Width - 40, EntryHeight);
                    e.PerformLayout();
                }
                else
                {
                    e.ClearItemLayout();
                }
            }
        }

        public void TransitionUpdate(Rectangle r)
        {
            ScrollListStyleTextures s = ScrollListStyleTextures.Get(Style);
            ScrollUp         = new Rectangle(r.X + r.Width - 20, r.Y + 30, s.ScrollBarArrowUp.Width, s.ScrollBarArrowUp.Height);
            ScrollDown       = new Rectangle(r.X + r.Width - 20, r.Y + r.Height - 14, s.ScrollBarArrowDown.Width, s.ScrollBarArrowDown.Height);
            ScrollBarHousing = new Rectangle(ScrollUp.X + 1, ScrollUp.Y + ScrollUp.Height + 3, s.ScrollBarMid.Width, ScrollDown.Y - ScrollUp.Y - ScrollUp.Height - 6);
            UpdateItemPositions(r.PosVec());
        }

        void UpdateListElements()
        {
            ExpandedEntries.Clear();
            foreach (T e in Entries)
                e.GetFlattenedVisibleExpandedEntries(ExpandedEntries);

            FirstVisibleIndex = FirstVisibleIndex.Clamped(0,
                Math.Max(0, ExpandedEntries.Count - MaxVisibleEntries));

            UpdateItemPositions(ParentMenu.Pos);
            UpdateScrollBar();
        }

        #endregion

        #region ScrollList Draw
        
        void DrawScrollBar(SpriteBatch batch)
        {
            ScrollListStyleTextures s = ScrollListStyleTextures.Get(Style);
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

            if (EnableSelectionBox)
                SelectionBox?.Draw(batch);

            if (DraggedEntry != null)
            {
                DraggedEntry.Pos = GameBase.ScreenManager.input.CursorPosition + DraggedOffset;
                batch.FillRectangle(DraggedEntry.Rect, new Color(0, 0, 0, 150));
                DraggedEntry.Draw(batch);
            }
        }


        void DrawChildElements(SpriteBatch batch)
        {
            int end = Math.Min(ExpandedEntries.Count, FirstVisibleIndex + MaxVisibleEntries);
            for (int i = FirstVisibleIndex; i < end; i++)
            {
                ExpandedEntries[i].Draw(batch);
            }
        }

        #endregion

        #region ScrollList Item

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
            public int VisibleIndex;

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

                for (int i = 0; i < SubEntries.Count; ++i)
                    if (SubEntries[i].RemoveSub(e))
                        return true;
                
                bool removed = SubEntries.Remove(e);
                if (removed) List.RequiresLayout = true;
                return removed;
            }

            public bool RemoveFirstSubIf(Predicate<T> predicate)
            {
                if (SubEntries.IsEmpty)
                    return false;

                foreach (T sub in SubEntries)
                    if (sub.RemoveFirstSubIf(predicate)) return true;

                bool removed =  SubEntries.RemoveFirst(predicate);
                if (removed) List.RequiresLayout = true;
                return removed;
            }

            public void GetFlattenedVisibleExpandedEntries(Array<T> entries)
            {
                if (Visible)
                {
                    entries.Add((T)this);
                    if (Expanded)
                    {
                        foreach (T sub in SubEntries)
                            sub.GetFlattenedVisibleExpandedEntries(entries);
                    }
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
                List.RequiresLayout = true;
            }

            public void DrawCancel(SpriteBatch batch, string toolTipText = null)
            {
                ScrollListStyleTextures s = ScrollListStyleTextures.Get(List.Style);
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

            // Resets the list item in such a way that it cannot be accidentally interacted with
            public void ClearItemLayout()
            {
                Hovered = false;
                Rect = new Rectangle(-500, -500, 0, 0);
            }

            public override void PerformLayout()
            {
                ScrollListStyleTextures s = ScrollListStyleTextures.Get(List.Style);
                SubTexture addIcon = s.BuildAdd;
                SubTexture upIcon  = s.QueueArrowUp;

                int right = (int)Right;
                int iconY = (int)Y + 15;
                if (Plus) PlusRect = new Rectangle(right - 30, iconY - addIcon.Height / 2, addIcon.Width, addIcon.Height);
                if (Edit) EditRect = new Rectangle(right - 60, iconY - addIcon.Height / 2, addIcon.Width, addIcon.Height);

                int offset = 0;
                bool all = List.Controls == ListControls.All;
                if (all) Up    = new Rectangle(right - (offset += 30), iconY - upIcon.Height / 2, upIcon.Width, upIcon.Height);
                if (all) Down  = new Rectangle(right - (offset += 30), iconY - upIcon.Height / 2, upIcon.Width, upIcon.Height);
                if (all) Apply = new Rectangle(right - (offset += 30), iconY - upIcon.Height / 2, upIcon.Width, upIcon.Height);
                        Cancel = new Rectangle(right - (offset +  30), iconY - upIcon.Height / 2, upIcon.Width, upIcon.Height);
                
                base.PerformLayout();
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
                ScrollListStyleTextures s = ScrollListStyleTextures.Get(List.Style);

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

        #endregion
    }

    internal sealed class ScrollListDebugView<T> where T : ScrollList<T>.Entry
    {
        readonly ScrollList<T> List;
        // ReSharper disable once UnusedMember.Global
        public ScrollListDebugView(ScrollList<T> list) { List = list; }
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