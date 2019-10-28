using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

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
    [DebuggerDisplay("{TypeName}  Entries = {Entries.Count}  Expanded = {ExpandedEntries.Count}")]
    public class ScrollList<T> : UIElementV2 where T : ScrollListItem<T>
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

        public int NumEntries => Entries.Count;
        public IReadOnlyList<T> AllEntries => Entries;

        float PercentViewed   => MaxVisibleEntries / (float)ExpandedEntries.Count;
        float StartingPercent => FirstVisibleIndex / (float)ExpandedEntries.Count;

        // Item at non-flattened index: doesn't index hierarchical elements
        public T this[int index] => Entries[index];

        // @return The first visible item
        public T FirstItem => ExpandedEntries[FirstVisibleIndex];
        public T LastItem  => ExpandedEntries[VisibleItemsEnd - 1];

        int FirstVisibleIndex;
        int VisibleItemsEnd;
        
        // Updates the visible index range
        // @return TRUE if this caused FirstVisibleIndex or LastVisibleIndex to change
        bool UpdateVisibleIndex(int index)
        {
            int oldFirst = FirstVisibleIndex;
            int oldEnd  = VisibleItemsEnd;
            FirstVisibleIndex = index.Clamped(0, Math.Max(0, ExpandedEntries.Count - MaxVisibleEntries));
            VisibleItemsEnd   = Math.Min(ExpandedEntries.Count, FirstVisibleIndex + MaxVisibleEntries);
            return FirstVisibleIndex != oldFirst || VisibleItemsEnd != oldEnd;
        }

        // Gets or Sets index of the topmost visible item in the list
        // NOTE: This will trigger ScrollList layout if this caused FirstVisibleIndex or LastVisibleIndex to change
        public int FirstVisibleItemIndex
        {
            get => FirstVisibleIndex;
            set => RequiresLayout |= UpdateVisibleIndex(value);
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

        public void Reset()
        {
            Entries.Clear();
            ExpandedEntries.Clear();
            RequiresLayout = true;
        }

        #region ScrollList HandleInput

        bool HandleScrollUpDownButtons(InputState input)
        {
            if (input.LeftMouseClick)
            {
                if (ScrollUp.HitTest(input.CursorPosition))
                {
                    if (FirstVisibleIndex > 0)
                        FirstVisibleItemIndex -= 1;
                    UpdateScrollBar();
                    return true;
                }
                if (ScrollDown.HitTest(input.CursorPosition))
                {
                    if (VisibleItemsEnd < ExpandedEntries.Count)
                        FirstVisibleItemIndex += 1;
                    UpdateScrollBar();
                    return true;
                }
            }
            return false;
        }

        bool HandleScrollDragInput(InputState input)
        {
            ScrollBarHover = 0;
            if (!ScrollBar.HitTest(input.CursorPosition))
                return false;

            ScrollBarHover = 1;
            if (!input.LeftMouseHeld(0.1f))
                return false;

            ScrollBarHover = 2;
            StartDragPos = (int)input.CursorPosition.Y;
            ScrollBarStartDragPos = ScrollBar.Y;
            DraggingScrollBar = true;
            return true;
        }

        bool HandleScrollBarDragging(InputState input)
        {
            if (DraggingScrollBar && input.LeftMouseDown)
            {
                float difference = input.CursorPosition.Y - StartDragPos;
                if (Math.Abs(difference) > 0f)
                {
                    ScrollBar.Y = (int) (ScrollBarStartDragPos + difference);
                    if (ScrollBar.Y < ScrollBarHousing.Y)
                    {
                        ScrollBar.Y = ScrollBarHousing.Y;
                    }
                    else if (ScrollBar.Bottom > ScrollBarHousing.Bottom)
                    {
                        ScrollBar.Y = ScrollBarHousing.Bottom - ScrollBar.Height;
                    }
                }

                float relativeScrollPos = ((ScrollBar.Y - ScrollBarHousing.Y) / (float)ScrollBarHousing.Height).Clamped(0f, 1f);
                FirstVisibleItemIndex = (int)(ExpandedEntries.Count * relativeScrollPos);
                return true;
            }

            DraggingScrollBar = false;
            return false;
        }
        
        bool HandleMouseScrollUpDown(InputState input)
        {
            if (Rect.HitTest(input.CursorPosition))
            {
                if (input.ScrollIn)
                {
                    if (FirstVisibleIndex > 0)
                        FirstVisibleItemIndex -= 1;
                    UpdateScrollBar();
                    return true;
                }
                if (input.ScrollOut)
                {
                    if (VisibleItemsEnd < ExpandedEntries.Count)
                        FirstVisibleItemIndex += 1;
                    UpdateScrollBar();
                    return true;
                }
            }
            return false;
        }

        bool HandleInputScrollBar(InputState input)
        {
            bool hit = HandleScrollUpDownButtons(input);
            hit |= HandleScrollDragInput(input);
            hit |= HandleScrollBarDragging(input);
            hit |= HandleMouseScrollUpDown(input);
            return hit;
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
                        for (int i = FirstVisibleIndex; i < VisibleItemsEnd; i++)
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

            for (int i = FirstVisibleIndex; i < VisibleItemsEnd; i++)
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
            // NOTE: We do not early return, because we want to update hover state for all ScrollList items
            bool captured = false;
            for (int i = FirstVisibleIndex; i < VisibleItemsEnd; i++)
            {
                T item = ExpandedEntries[i];
                if (item.HandleInput(input))
                {
                    // only show selector if item is not a Header element
                    if (!item.IsHeader && IsSelectable)
                    {
                        SelectionBox = new Selector(item.Rect);
                    }
                    captured = true;
                }
            }

            // No longer hovering over the scroll list? Clear the SelectionBox
            if (IsSelectable && !Rect.HitTest(input.CursorPosition))
            {
                SelectionBox = null;
            }
            return captured;
        }

        public override bool HandleInput(InputState input)
        {
            if (!Visible || !Enabled)
                return false;

            HandleDraggable(input);
            HandleElementDragging(input);

            if (HandleInputScrollBar(input))
                return true;

            if (HandleInputChildElements(input))
                return true;

            if (Background != null && Background.HandleInput(input))
                return true;

            return false;
        }

        #endregion

        #region ScrollList Update / PerformLayout
        
        public ScrollListStyleTextures GetStyle() => ScrollListStyleTextures.Get(Style);

        public override void PerformLayout()
        {
            base.PerformLayout();

            ScrollListStyleTextures s = GetStyle();
            ScrollUp   = new Rectangle((int)(Right - 20), (int)Y + 30,      s.ScrollBarArrowUp.Normal.Width,   s.ScrollBarArrowUp.Normal.Height);
            ScrollDown = new Rectangle((int)(Right - 20), (int)Bottom - 14, s.ScrollBarArrowDown.Normal.Width, s.ScrollBarArrowDown.Normal.Height);
            ScrollBarHousing = new Rectangle(ScrollUp.X + 1, ScrollUp.Bottom + 3, s.ScrollBarMid.Normal.Width, ScrollDown.Y - ScrollUp.Y - ScrollUp.Height - 6);

            if (Background != null)
            {
                Background.Rect = Rect;
                Background.PerformLayout();
            }

            ExpandedEntries.Clear();
            for (int i = 0; i < Entries.Count; ++i)
                Entries[i].GetFlattenedVisibleExpandedEntries(ExpandedEntries);

            UpdateVisibleIndex(FirstVisibleIndex);
            // only updated scrollbar if we're not already dragging it
            if (!DraggingScrollBar)
                UpdateScrollBar();

            int visibleIndex = 0;
            for (int i = 0; i < ExpandedEntries.Count; i++)
            {
                T e = ExpandedEntries[i];
                if (FirstVisibleIndex <= i && i < VisibleItemsEnd)
                {
                    e.VisibleIndex = visibleIndex++;
                    e.Rect = new Rectangle((int)X + 20, (int)Y + 35 + e.VisibleIndex * EntryHeight, 
                                           (int)Width - 40, EntryHeight);
                    e.PerformLayout();
                }
                else
                {
                    e.ClearItemLayout();
                }
            }
        }

        void UpdateScrollBar()
        {
            ScrollListStyleTextures s = GetStyle();
            int scrollStart = (int)(ScrollBarHousing.Height * StartingPercent);
            int scrollEnd   = (int)(ScrollBarHousing.Height * PercentViewed);
            int width = s.ScrollBarMid.Normal.Width;
            ScrollBar = new Rectangle(ScrollBarHousing.X, ScrollBarHousing.Y + scrollStart, width, scrollEnd);
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
            //if (!FirstUpdateDone)
            //    Log.Error($"{TypeName}.Update() has not been called. This is a bug!"
            //              +" Make sure the ScrollList is being updated in GameScreen.Update() or screen.Add(list) for automatic update.");

            Background?.Draw(batch);

            if (ExpandedEntries.Count > MaxVisibleEntries)
                DrawScrollBar(batch);

            for (int i = FirstVisibleIndex; i < VisibleItemsEnd; ++i)
                ExpandedEntries[i].Draw(batch);

            if (IsSelectable)
                SelectionBox?.Draw(batch);

            if (DraggedEntry != null)
            {
                DraggedEntry.Pos = GameBase.ScreenManager.input.CursorPosition + DraggedOffset;
                batch.FillRectangle(DraggedEntry.Rect, new Color(0, 0, 0, 150));
                DraggedEntry.Draw(batch);
            }
        }

        #endregion

    }

    internal sealed class ScrollListDebugView<T> where T : ScrollListItem<T>
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