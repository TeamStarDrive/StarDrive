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
    // Visual style of the ScrollList
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
        Rectangle ScrollUp;
        Rectangle ScrollDown;
        Rectangle ScrollBarHousing;
        Rectangle ScrollBar;

        int ScrollBarHover;
        int StartDragPos;
        bool DraggingScrollBar;
        float ScrollBarStartDragPos;
        float ClickTimer;
        const float TimerDelay = 0.05f;

        readonly int EntryHeight;
        readonly int MaxVisibleEntries;
        readonly Array<T> Entries         = new Array<T>();
        readonly Array<T> ExpandedEntries = new Array<T>(); // Flattened entries

        // this controls the visual style of the ScrollList
        // can be freely changed at any point
        public ListStyle Style;

        // Automatic Selection highlight
        public Selector SelectionBox;

        // If TRUE, automatically draws Selection highlight around each ScrollList Item
        public bool IsSelectable = false;

        // If set to a valid UIElement instance, then this element will be drawn in the background
        public UIElementV2 Background;

        // EVENT: Called when a new item is focused with mouse
        //        @note This is called again with <null> when mouse leaves focus
        public Action<T> OnHovered;

        // EVENT: Called when an item is clicked on
        public Action<T> OnClick;

        // EVENT: Called when an item is double-clicked
        public Action<T> OnDoubleClick;

        // EVENT: Called when an item drag starts or item drag ends
        public Action<T, DragEvent> OnDrag;

        // If TRUE, allows automatic dragging of ScrollList Items
        public bool IsDraggable = false;

        T DraggedEntry;
        Vector2 DraggedOffset;

        public ScrollList(UIElementV2 background, ListStyle style = ListStyle.Default) : this(background, 40, style)
        {
        }
        
        public ScrollList(UIElementV2 background, int entryHeight, ListStyle style = ListStyle.Default) : this(background.Rect, entryHeight, style)
        {
            Background = background;
        }

        public ScrollList(float x, float y, float w, float h, int entryHeight, ListStyle style = ListStyle.Default)
            : this(new Rectangle((int)x, (int)y, (int)w, (int)h), entryHeight, style)
        {
        }

        public ScrollList(in Rectangle rect, int entryHeight, ListStyle style = ListStyle.Default) : base(null, rect)
        {
            Style = style;
            EntryHeight = entryHeight;
            MaxVisibleEntries = (rect.Height - 25) / entryHeight;
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
        public T LastItem => ExpandedEntries[Math.Min(ExpandedEntries.Count, FirstVisibleIndex + MaxVisibleEntries) - 1];

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
                RequiresLayout = true;
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
                RequiresLayout = true;
        }

        public void Sort<TValue>(Func<T, TValue> predicate)
        {
            T[] sorted = Entries.OrderBy(predicate).ToArray();
            Entries.Clear();
            Entries.AddRange(sorted);
            RequiresLayout = true;
        }

        public void SortDescending<TValue>(Func<T, TValue> predicate)
        {
            T[] sorted = Entries.OrderByDescending(predicate).ToArray();
            Entries.Clear();
            Entries.AddRange(sorted);
            RequiresLayout = true;
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
            if (!Rect.HitTest(input.CursorPosition))
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
                    if (!item.HasSubEntries && IsSelectable)
                    {
                        SelectionBox = new Selector(item.Rect);
                    }
                    return true;
                }
            }

            // No longer hovering over the scroll list? Clear the SelectionBox
            if (IsSelectable && !Rect.HitTest(input.CursorPosition))
            {
                SelectionBox = null;
            }
            return false;
        }

        public override bool HandleInput(InputState input)
        {
            if (!Visible || !Enabled)
                return false;

            bool hit = HandleScrollBar(input);
            hit |= HandleMouseScrollUpDown(input);
            HandleDraggable(input);
            HandleElementDragging(input);

            if (Background != null && Background.HandleInput(input))
                return true;

            if (!hit)
                return HandleInputChildElements(input);
            return hit;
        }

        #endregion

        #region ScrollList Update / PerformLayout
        
        ScrollListStyleTextures GetStyle() => ScrollListStyleTextures.Get(Style);

        public override void PerformLayout()
        {
            base.PerformLayout();

            ScrollListStyleTextures s = GetStyle();
            ScrollUp   = new Rectangle((int)(Right - 20), (int)Y + 30,      s.ScrollBarArrowUp.Normal.Width,   s.ScrollBarArrowUp.Normal.Height);
            ScrollDown = new Rectangle((int)(Right - 20), (int)Bottom - 14, s.ScrollBarArrowDown.Normal.Width, s.ScrollBarArrowDown.Normal.Height);
            ScrollBarHousing = new Rectangle(ScrollUp.X + 1, ScrollUp.Y + ScrollUp.Height + 3, s.ScrollBarMid.Normal.Width, ScrollDown.Y - ScrollUp.Y - ScrollUp.Height - 6);

            if (Background != null)
            {
                Background.Rect = Rect;
                Background.PerformLayout();
            }

            if (Entries.Count > 0)
            {
                ExpandedEntries.Clear();
                foreach (T e in Entries)
                    e.GetFlattenedVisibleExpandedEntries(ExpandedEntries);

                FirstVisibleIndex = FirstVisibleIndex.Clamped(0,
                    Math.Max(0, ExpandedEntries.Count - MaxVisibleEntries));

                UpdateItemPositions(Pos);
            }

            UpdateScrollBar();
        }

        void UpdateScrollBar()
        {
            ScrollListStyleTextures s = GetStyle();
            int scrollStart = (int)(StartingPercent * ScrollBarHousing.Height);
            int scrollEnd = (int)(ScrollBarHousing.Height * PercentViewed);
            int width = s.ScrollBarMid.Normal.Width;

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
                                           (int)Width - 40, EntryHeight);
                    e.PerformLayout();
                }
                else
                {
                    e.ClearItemLayout();
                }
            }
        }

        public override void Update(float deltaTime)
        {
            Background?.Update(deltaTime);

            base.Update(deltaTime);

            if (RequiresLayout)
                PerformLayout();
        }

        #endregion

        #region ScrollList Draw

        void DrawScrollBar(SpriteBatch batch)
        {
            ScrollListStyleTextures s = GetStyle();
            int upDownSize = (ScrollBar.Height - s.ScrollBarMid.Normal.Height) / 2;
            var up  = new Rectangle(ScrollBar.X, ScrollBar.Y, ScrollBar.Width, upDownSize);
            var mid = new Rectangle(ScrollBar.X, ScrollBar.Y + upDownSize, ScrollBar.Width, s.ScrollBarMid.Normal.Height);
            var bot = new Rectangle(ScrollBar.X, mid.Y + mid.Height, ScrollBar.Width, upDownSize);

            bool parentHovered      = (ScrollBarHover == 1);
            bool controlItemHovered = (ScrollBarHover == 2);

            s.ScrollBarUpDown.Draw(batch, up, parentHovered, controlItemHovered);
            s.ScrollBarMid   .Draw(batch, mid, parentHovered, controlItemHovered);
            s.ScrollBarUpDown.Draw(batch, bot, parentHovered, controlItemHovered);

            Vector2 cursor = GameBase.ScreenManager.input.CursorPosition;
            s.ScrollBarArrowUp.Draw(batch, ScrollUp, parentHovered, ScrollUp.HitTest(cursor));
            s.ScrollBarArrowDown.Draw(batch, ScrollDown, parentHovered, ScrollDown.HitTest(cursor));
        }

        public override void Draw(SpriteBatch batch)
        {
            Background?.Draw(batch);

            if (ExpandedEntries.Count > MaxVisibleEntries)
                DrawScrollBar(batch);

            DrawChildElements(batch);

            if (IsSelectable)
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

            // The current visible index of this Entry
            public int VisibleIndex;

            readonly Array<T> SubEntries = new Array<T>();
            public bool HasSubEntries => SubEntries.NotEmpty;
            
            // Lightweight dynamic elements
            // @note This provides customization options for each ScrollList Item
            //       use methods EnablePlus() / EnableUpDown() / etc to enable these elements
            readonly Array<Element> DynamicElements = new Array<Element>();

            public Entry()
            {
            }

            protected Entry(UIElementV2 parent, Vector2 pos) : base(parent, pos)
            {
            }

            protected Entry(UIElementV2 parent, in Rectangle rect) : base(parent, rect)
            {
            }

            public class Element
            {
                public Entry Parent;
                public Vector2 RelPos;
                public ToolTipText Tooltip;
                public Action OnClick;
                public Func<ScrollListStyleTextures.Hoverable> GetHoverable;
                Rectangle AbsRect;
                bool IsHovered;
                
                public bool HandleInput(InputState input)
                {
                    IsHovered = AbsRect.HitTest(input.CursorPosition);
                    if (IsHovered && input.LeftMouseClick)
                    {
                        OnClick?.Invoke();
                        return true;
                    }
                    return false;
                }
                public void UpdateLayout()
                {
                    SubTexture icon = GetHoverable().Normal;
                    // For negative RelPos, start from opposite edge
                    float x = (RelPos.X >= 0 ? Parent.X : Parent.Right) + RelPos.X;
                    float y = (RelPos.Y >= 0 ? Parent.Y : Parent.Bottom) + RelPos.Y;
                    AbsRect = new Rectangle((int)x, (int)(y + 15f - icon.Height / 2f), icon.Width, icon.Height);
                }
                public void Draw(SpriteBatch batch)
                {
                    GetHoverable().Draw(batch, AbsRect, Parent.Hovered, IsHovered);
                    if (IsHovered && Tooltip.IsValid)
                        ToolTip.CreateTooltip(Tooltip);
                }
            }

            void AddElement(Vector2 relPos, ToolTipText tooltip, Action onClick, Func<ScrollListStyleTextures.Hoverable> getHoverable)
            {
                var e = new Element{ Parent = this, RelPos = relPos, Tooltip = tooltip, OnClick = onClick, GetHoverable = getHoverable };
                DynamicElements.Add(e);
            }

            public void AddPlus(Vector2 relPos, ToolTipText tooltip, Action onClick = null) => AddElement(relPos, tooltip, onClick, () => List.GetStyle().BuildAdd);
            public void AddEdit(Vector2 relPos, ToolTipText tooltip, Action onClick = null) => AddElement(relPos, tooltip, onClick, () => List.GetStyle().BuildEdit);
            public void AddUp(Vector2 relPos, ToolTipText tooltip, Action onClick = null) => AddElement(relPos, tooltip, onClick, () => List.GetStyle().QueueArrowUp);
            public void AddDown(Vector2 relPos, ToolTipText tooltip, Action onClick = null) => AddElement(relPos, tooltip, onClick, () => List.GetStyle().QueueArrowDown);
            public void AddApply(Vector2 relPos, ToolTipText tooltip, Action onClick = null) => AddElement(relPos, tooltip, onClick, () => List.GetStyle().QueueRush);
            public void AddCancel(Vector2 relPos, ToolTipText tooltip, Action onClick) => AddElement(relPos, tooltip, onClick, () => List.GetStyle().QueueDelete);

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

            // Resets the list item in such a way that it cannot be accidentally interacted with
            public void ClearItemLayout()
            {
                Hovered = false;
                Rect = new Rectangle(-500, -500, 0, 0);
            }

            public override void PerformLayout()
            {
                for (int i = 0; i < DynamicElements.Count; ++i)
                    DynamicElements[i].UpdateLayout();

                base.PerformLayout();
            }

            public override bool HandleInput(InputState input)
            {
                bool wasHovered = Hovered;
                Hovered = Rect.HitTest(input.CursorPosition);

                // Mouse entered this Entry
                if (!wasHovered && Hovered)
                {
                    GameAudio.ButtonMouseOver();
                    List.OnItemHovered(this as T);
                }

                for (int i = 0; i < DynamicElements.Count; ++i)
                    if (DynamicElements[i].HandleInput(input))
                        return true;

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
                    // @note Always capture input if hovered?
                    return true;
                }

                return base.HandleInput(input);
            }

            public override void Draw(SpriteBatch batch)
            {
                base.Draw(batch);
                for (int i = 0; i < DynamicElements.Count; ++i)
                {
                    DynamicElements[i].Draw(batch);
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
        public T[] Items
        {
            get
            {
                IReadOnlyList<T> allEntries = List.AllEntries;
                var items = new T[allEntries.Count];
                for (int i = 0; i < items.Length; ++i)
                    items[i] = allEntries[i];
                return items;
            }
        }
    }
}