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
    public class ScrollList<T> : UIElementContainer where T : ScrollListItem<T>
    {
        Rectangle ScrollUp, ScrollUpClickArea;
        Rectangle ScrollDown, ScrollDownClickArea;
        Rectangle ScrollBar, ScrollBarClickArea;
        Rectangle ScrollHousing;
        int ScrollBarHover;
        bool ScrollUpHover, ScrollDownHover;
        
        int DragStartMousePos;
        int DragStartScrollPos;
        bool DraggingScrollBar;
        bool WasScrolled;

        // The current scroll position of the scrollbar, clamped to [0, 1]
        float RelScrollPos;

        float ClickTimer;
        const float TimerDelay = 0.05f;

        // Top and Bottom padding for list items
        readonly int PaddingTop = 30;
        readonly int PaddingBot = 14;
        int EntryHeight;
        int MaxVisibleEntries;
        readonly Array<T> Entries         = new Array<T>();
        readonly Array<T> ExpandedEntries = new Array<T>(); // Flattened entries

        // this controls the visual style of the ScrollList
        // can be freely changed at any point
        public ListStyle Style;

        // Automatic Selection highlight
        public Selector SelectionBox;

        // If TRUE, automatically draws Selection highlight around each ScrollList Item
        public bool EnableItemHighlight;

        // If set to a valid UIElement instance, then this element will be drawn in the background
        UIElementV2 TheBackground;
        public UIElementV2 Background
        {
            set
            {
                if (TheBackground != value)
                {
                    TheBackground?.RemoveFromParent();
                    TheBackground = value;
                    if (value != null) Add(value);
                }
            }
        }

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

        static Rectangle GetOurRectFromBackground(UIElementV2 background)
        {
            Rectangle r = background.Rect;
            if (background is Menu1)
                r.Width -= 5;
            return r;
        }

        public ScrollList(UIElementV2 background, ListStyle style = ListStyle.Default)
            : this(background, 40, style)
        {
        }
        
        public ScrollList(UIElementV2 background, int entryHeight, ListStyle style = ListStyle.Default)
            : this(GetOurRectFromBackground(background), entryHeight, style)
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
            ItemHeight = entryHeight;
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

        public int ItemHeight
        {
            get => EntryHeight;
            set
            {
                if (EntryHeight != value)
                {
                    EntryHeight = value;
                    MaxVisibleEntries = (int)Math.Floor((Height - (PaddingTop + PaddingBot)) / value);
                    RequiresLayout = true;
                }
            }
        }

        public int NumEntries => Entries.Count;
        public IReadOnlyList<T> AllEntries => Entries;

        // Item at non-flattened index: doesn't index hierarchical elements
        public T this[int index] => Entries[index];

        // @return The first visible item
        public T FirstItem => ExpandedEntries[VisibleItemsBegin];
        public T LastItem  => ExpandedEntries[VisibleItemsEnd - 1];

        // visible range is [begin, end)
        int VisibleItemsBegin, VisibleItemsEnd;
        
        // Updates the visible index range
        // @return TRUE if this caused FirstVisibleIndex or LastVisibleIndex to change
        void UpdateVisibleIndex(float indexFraction)
        {
            //ItemHeight = EntryHeight;
            int begin = (int)Math.Floor(indexFraction);
            int end   = (int)Math.Ceiling(indexFraction + MaxVisibleEntries);
            VisibleItemsBegin = begin.Clamped(0, Math.Max(0, ExpandedEntries.Count - MaxVisibleEntries));
            VisibleItemsEnd   = end.Clamped(0, Math.Max(0, ExpandedEntries.Count));
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

        int ScrollButtonAmount(bool held) => !held ? EntryHeight / 2 : EntryHeight / 6;
        int ScrollWheelAmount => EntryHeight / 3;

        bool HandleInputScrollButtons(InputState input)
        {
            ScrollUpHover   = ScrollUpClickArea.HitTest(input.CursorPosition);
            ScrollDownHover = ScrollDownClickArea.HitTest(input.CursorPosition);
            bool held = input.LeftMouseHeld(0.1f);
            if ((held || input.LeftMouseClick) && (ScrollUpHover || ScrollDownHover))
            {
                ScrollByScrollBar(ScrollButtonAmount(held) * (ScrollUpHover ? -1 : +1));
                return true;
            }
            return false;
        }

        bool HandleInputScrollDrag(InputState input)
        {
            ScrollBarHover = 0; // no highlight

            if (ScrollBarClickArea.HitTest(input.CursorPosition))
            {
                ScrollBarHover = 1; // gentle focus
                if (!DraggingScrollBar && input.LeftMouseHeld(0.1f))
                {
                    DraggingScrollBar = true;
                    DragStartMousePos = (int)input.CursorPosition.Y;
                    DragStartScrollPos = ScrollBar.Y;
                }
            }

            if (DraggingScrollBar && input.LeftMouseDown)
            {
                ScrollBarHover = 2; // full active highlight

                float difference = input.CursorPosition.Y - DragStartMousePos;
                if (Math.Abs(difference) > 0f)
                    SetScrollBarPosition((int)Math.Round(DragStartScrollPos + difference));
                return true;
            }

            DraggingScrollBar = false;
            return false;
        }

        bool HandleInputMouseWheel(InputState input)
        {
            if (Rect.HitTest(input.CursorPosition))
            {
                int amount = 0;
                if (input.ScrollIn) amount = -ScrollWheelAmount;
                if (input.ScrollOut) amount = ScrollWheelAmount;
                if (amount != 0)
                {
                    ScrollByScrollBar(amount);
                    return true;
                }
            }
            return false;
        }

        bool HandleInputScrollBar(InputState input)
        {
            bool hit = HandleInputScrollButtons(input);
            hit |= HandleInputScrollDrag(input);
            hit |= HandleInputMouseWheel(input);
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
                        for (int i = VisibleItemsBegin; i < VisibleItemsEnd; i++)
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

            for (int i = VisibleItemsBegin; i < VisibleItemsEnd; i++)
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

        bool HandleInputVisibleItems(InputState input)
        {
            // NOTE: We do not early return, because we want to update hover state for all ScrollList items
            bool anyCaptured = false;
            bool anyHovered = false;
            if (Rect.HitTest(input.CursorPosition))
            {
                for (int i = VisibleItemsBegin; i < VisibleItemsEnd; i++)
                {
                    T item = ExpandedEntries[i];
                    anyCaptured |= item.HandleInput(input);
                    bool thisHovered = item.Hovered;
                    anyHovered |= thisHovered;

                    // only show selector if item is not a Header element
                    if (thisHovered && !item.IsHeader && EnableItemHighlight)
                    {
                        SelectionBox = new Selector(item.Rect);
                    }
                }
            }

            // Not hovering over any items? Clear the SelectionBox
            if (!anyHovered && EnableItemHighlight)
            {
                SelectionBox = null;
            }
            return anyCaptured;
        }

        public override bool HandleInput(InputState input)
        {
            if (!Visible || !Enabled)
                return false;

            HandleDraggable(input);
            HandleElementDragging(input);

            return base.HandleInput(input) || HandleInputScrollBar(input) || HandleInputVisibleItems(input);
        }

        #endregion

        #region ScrollList Update / PerformLayout
        
        public ScrollListStyleTextures GetStyle() => ScrollListStyleTextures.Get(Style);

        public override void PerformLayout()
        {
            if (TheBackground != null)
                TheBackground.Pos = Pos;

            base.PerformLayout();

            ScrollListStyleTextures s = GetStyle();
            ScrollUp   = new Rectangle((int)(Right - 20), (int)Y + PaddingTop,      s.ScrollBarArrowUp.Normal.Width,   s.ScrollBarArrowUp.Normal.Height);
            ScrollDown = new Rectangle((int)(Right - 20), (int)Bottom - PaddingBot, s.ScrollBarArrowDown.Normal.Width, s.ScrollBarArrowDown.Normal.Height);
            ScrollHousing = new Rectangle(ScrollUp.X + 1, ScrollUp.Bottom + 3, s.ScrollBarMid.Normal.Width, ScrollDown.Y - ScrollUp.Y - ScrollUp.Height - 6);
            ScrollUpClickArea   = ScrollUp.Bevel(5);
            ScrollDownClickArea = ScrollDown.Bevel(5);

            ExpandedEntries.Clear();
            for (int i = 0; i < Entries.Count; ++i)
                Entries[i].GetFlattenedVisibleExpandedEntries(ExpandedEntries);

            int scrollOffset = 0;
            if (WasScrolled)
            {
                WasScrolled = false;
                // when scrollbar was moved being dragged by input, use it to update the visible index
                float scrollPos = GetRelativeScrollPosFromScrollBar();
                float newIndexFraction = Math.Max(0, ExpandedEntries.Count - MaxVisibleEntries) * scrollPos;
                UpdateVisibleIndex(newIndexFraction);

                float remainder = (newIndexFraction - VisibleItemsBegin) % 1f;
                scrollOffset = (int)Math.Floor(remainder * EntryHeight);
                //Log.Info($"rpos={scrollPos} fidx={newIndexFraction} rem={remainder} offset={scrollOffset}");
            }
            else // otherwise, update/clamp visible indices and recalculate scrollbar
            {
                UpdateVisibleIndex(VisibleItemsBegin);
                UpdateScrollBar();
            }

            int visibleIndex = 0;
            for (int i = 0; i < ExpandedEntries.Count; i++)
            {
                T e = ExpandedEntries[i];
                if (VisibleItemsBegin <= i && i < VisibleItemsEnd)
                {
                    e.VisibleIndex = visibleIndex++;
                    int y = (int)Y + PaddingTop + (e.VisibleIndex * EntryHeight) - scrollOffset;
                    e.Rect = new Rectangle((int)X + 20, y, (int)Width - 40, EntryHeight);
                    e.PerformLayout();
                }
                else
                {
                    e.ClearItemLayout();
                }
            }
        }

        // this is the relative position of the scrollbar [0, 1] inside the scrollbar housing
        float GetRelativeScrollPosFromScrollBar()
        {
            float scrollBarPos = (ScrollBar.Y - ScrollHousing.Y);
            float scrollSpan   = ScrollHousing.Height - ScrollBar.Height;
            return (scrollBarPos / scrollSpan);
        }

        void UpdateScrollBar()
        {
            int startOffset = (int)(ScrollHousing.Height * (VisibleItemsBegin / (float)ExpandedEntries.Count));
            int barHeight   = (int)(ScrollHousing.Height * (MaxVisibleEntries / (float)ExpandedEntries.Count));

            ScrollBar = new Rectangle(ScrollHousing.X, ScrollHousing.Y + startOffset,
                                      GetStyle().ScrollBarMid.Normal.Width, barHeight);
            ScrollBarClickArea = ScrollBar.Widen(5);
        }

        // set scrollbar to requested position
        void SetScrollBarPosition(int newScrollY)
        {
            if (newScrollY < ScrollHousing.Y)
                newScrollY = ScrollHousing.Y;
            else if ((newScrollY + ScrollBar.Height) > ScrollHousing.Bottom)
                newScrollY = ScrollHousing.Bottom - ScrollBar.Height;
            
            // This enables smooth scrollbar dragging: with every pixel changed we request layout
            if (ScrollBar.Y != newScrollY)
            {
                ScrollBar.Y = ScrollBarClickArea.Y = newScrollY;
                RequiresLayout = true;
                WasScrolled = true;
            }
        }

        // move the scrollbar by requested amount of pixels up (-) or down (+)
        void ScrollByScrollBar(int deltaScroll) => SetScrollBarPosition(ScrollBar.Y + deltaScroll);

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
            s.ScrollBarArrowUp.Draw(batch, ScrollUp, parentHovered, ScrollUpHover);
            s.ScrollBarArrowDown.Draw(batch, ScrollDown, parentHovered, ScrollDownHover);
        }

        public override void Draw(SpriteBatch batch)
        {
            //if (!FirstUpdateDone)
            //    Log.Error($"{TypeName}.Update() has not been called. This is a bug!"
            //              +" Make sure the ScrollList is being updated in GameScreen.Update() or screen.Add(list) for automatic update.");
            base.Draw(batch);

            if (ExpandedEntries.Count > MaxVisibleEntries)
                DrawScrollBar(batch);
            
            // use a scissor to clip smooth scroll items
            batch.End();
            batch.Begin(SpriteBlendMode.AlphaBlend, SpriteSortMode.Deferred, SaveStateMode.None);

            for (int i = VisibleItemsBegin; i < VisibleItemsEnd; ++i)
            {
                ExpandedEntries[i].Draw(batch);
            }

            if (EnableItemHighlight)
                SelectionBox?.Draw(batch);

            batch.GraphicsDevice.RenderState.ScissorTestEnable = true;
            batch.GraphicsDevice.ScissorRectangle = new Rectangle((int)X, ScrollUp.Y, (int)Width, ScrollDown.Bottom - ScrollUp.Y);
            batch.End();
            batch.Begin();
            batch.GraphicsDevice.RenderState.ScissorTestEnable = false;

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