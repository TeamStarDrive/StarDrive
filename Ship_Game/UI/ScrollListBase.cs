﻿using System;
using Microsoft.Xna.Framework.Graphics;
using SDGraphics;
using SDUtils;
using Ship_Game.Graphics;
using Vector2 = SDGraphics.Vector2;
using Rectangle = SDGraphics.Rectangle;
using Ship_Game.UI;

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

    public abstract class ScrollListBase : UIElementContainer
    {
        // Top and Bottom padding for list items
        const int PaddingTop   = 15;
        const int PaddingBot   = 15;
        const int PaddingLeft  = 8;
        const int PaddingRight = 24; // leave extra room for scrollbar

        // inner housing rect for scroll list items
        // available for reading, BUT it will be recalculated in every PerformLayout()
        public RectF ItemsHousing;
        protected RectF ScrollUp, ScrollUpClickArea;
        protected RectF ScrollDown, ScrollDownClickArea;
        protected RectF ScrollBar, ScrollBarClickArea;
        protected RectF ScrollHousing;
        protected int ScrollBarHover;
        protected bool ScrollUpHover, ScrollDownHover;
        
        protected int DragStartMousePos;
        protected float DragStartScrollPos;
        protected bool DraggingScrollBar;
        protected bool ScrollBarPosChanged;
        protected bool ShouldDrawScrollBar;
        protected bool FirstUpdateDone; // If true, ScrollList.Update() has been called

        // if not null, this Rect will be updated during every PerformLayout()
        // feel free to set this, but you will need to trigger PerformLayout()
        public IClientArea ClientAreaSource;

        // Minimum time that must be elapsed before we start dragging
        protected const float DragBeginDelay = 0.075f;

        // By default, 4px padding between items, 0px from edges
        public Vector2 ItemPadding = new(0f, 4f);
        
        // this controls the visual style of the ScrollList
        // can be freely changed at any point
        public ListStyle Style;

        // Automatic Selection highlight
        public Selector Highlight;

        // Index of the currently highlighted ScrollList Item, -1 if no highlighted item
        public int HighlightedIndex = -1;

        // If TRUE, automatically draws Selection highlight around each ScrollList Item
        public bool EnableItemHighlight;

        // If TRUE, items will trigger click events
        // NOTE: This is automatically enabled by setting
        //       ScrollList<T>.OnHovered/OnClick/OnDoubleClick event
        public bool EnableItemEvents = true;

        // If TRUE, allows automatic dragging of ScrollList Items OUTSIDE of the list
        // NOTE: This is automatically enabled by setting ScrollList<T>.OnDrag event
        public bool EnableDragOutEvents = false;

        // If TRUE, allows to drag and reordering of ScrollList Items INSIDE the list
        // NOTE: This is automatically enabled by setting ScrollList<T>.OnDragReorder event
        public bool EnableDragReorderEvents = false;

        // DEBUG: Enable special debug features for Scroll List Debugging
        public bool DebugDrawScrollList;

        public abstract void OnItemHovered(ScrollListItemBase item);
        public abstract void OnItemClicked(ScrollListItemBase item);
        public abstract void OnItemDoubleClicked(ScrollListItemBase item);
        public abstract void OnItemDragged(ScrollListItemBase item, DragEvent evt, bool outside);
        public abstract void OnItemDragReordered(ScrollListItemBase dragged, ScrollListItemBase destination);

        // If set to a valid UIElement instance, then this element will be drawn in the background
        UIElementV2 TheBackground;

        public void SetBackground(UIElementV2 background)
        {
            if (TheBackground != background)
            {
                TheBackground?.RemoveFromParent();
                TheBackground = background;
                if (background != null)
                {
                    // attach with relative position, so it moves along with us
                    Add(background).SetLocalPos(Pos - background.Pos);
                }
            }
        }

        public virtual void OnItemExpanded(ScrollListItemBase item, bool expanded)
        {
            // NOTE: Modify the index when opening/closing headers, to make list usage more convenient
            if (expanded)
            {
                float relClickPos = (float)item.VisibleIndex / MaxVisibleItems;
                if (relClickPos >= 0.5f)
                {
                    VisibleItemsBegin += Math.Min(MaxVisibleItems / 2, MaxVisibleItems);
                }
            }
            else
            {
                //VisibleItemsBegin = Math.Max(0, VisibleItemsBegin - item.NumSubItems);
            }

            RequiresLayout = true;
            HighlightedIndex = -1; // remove highlight when closing/expanding
            Highlight = null;
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

        protected ScrollListItemBase DraggedEntry;
        Vector2 DraggedOffset;
        public bool IsDragging => DraggedEntry != null;

        void HandleDraggable(InputState input)
        {
            if (!EnableDragOutEvents && !EnableDragReorderEvents)
                return;

            // try to start item drag, but only if we're not dragging scrollbar
            if (DraggedEntry == null && !DraggingScrollBar)
            {
                if (input.LeftMouseHeld(DragBeginDelay) && Rect.HitTest(input.StartLeftHold))
                {
                    Vector2 cursor = input.CursorPosition;
                    for (int i = VisibleItemsBegin; i < VisibleItemsEnd; i++)
                    {
                        ScrollListItemBase e = FlatEntries[i];
                        if (e.Rect.HitTest(cursor))
                        {
                            DraggedEntry = e;
                            DraggedOffset = e.TopLeft - cursor;
                            OnItemDragged(e, DragEvent.Begin, false);
                            return;
                        }
                    }
                }
            }
            else // already dragging
            {
                if (input.LeftMouseUp)
                    CancelDragging();
            }
        }

        public void CancelDragging()
        {
            if (IsDragging)
            {
                bool outside = !Rect.HitTest(GetInput().CursorPosition);
                OnItemDragged(DraggedEntry, DragEvent.End, outside);
                DraggedEntry = null;
                RequiresLayout = true; // refresh the items
            }
        }

        protected void HandleElementDragging(InputState input)
        {
            if (!EnableDragReorderEvents || DraggedEntry == null || !input.LeftMouseDown)
                return;

            Vector2 cursor = input.CursorPosition;
            int oldIndex = FlatEntries.IndexOf(DraggedEntry);
            if (oldIndex == -1)
                return;

            for (int newIndex = VisibleItemsBegin; newIndex < VisibleItemsEnd; newIndex++)
            {
                ScrollListItemBase newItem = FlatEntries[newIndex];
                if (newItem != DraggedEntry && newIndex != oldIndex &&
                    newItem.Rect.HitTest(cursor))
                {
                    // reorder flat entries for this frame
                    FlatEntries.Reorder(oldIndex, newIndex);

                    // but queue recalculation just in case
                    RequiresLayout = true;
                    DraggedEntry.RequiresLayout = true;
                    newItem.RequiresLayout = true;

                    OnItemDragReordered(DraggedEntry, newItem);
                    break;
                }
            }
        }

        public override bool HandleInput(InputState input)
        {
            if (!Visible || !Enabled)
                return false;

            HandleDraggable(input);
            HandleElementDragging(input);
            
            // NOTE: We do not early return, because we want to update hover state for all ScrollList items
            bool captured = base.HandleInput(input) || HandleInputScrollBar(input);
            bool createdSelector = false;

            if (!captured && Rect.HitTest(input.CursorPosition))
            {
                // input wasn't captured by other items or scroll bar
                // so update hover state of visible items
                for (int i = VisibleItemsBegin; i < VisibleItemsEnd; i++)
                {
                    ScrollListItemBase item = FlatEntries[i];
                    if (item.UpdateHoverState(input))
                    {
                        if (!item.IsHeader && EnableItemHighlight)
                        {
                            createdSelector = true;
                            HighlightedIndex = i;
                            Highlight = new Selector(item.Rect.Bevel(4, 2));
                        }
                    }
                }

                // now capture input
                for (int i = VisibleItemsBegin; i < VisibleItemsEnd; i++)
                {
                    ScrollListItemBase item = FlatEntries[i];
                    if (item.HandleInput(input))
                    {
                        captured = true;
                        break; // it's safe to early break here
                    }
                }
            }

            // Not hovering over any items? Clear the SelectionBox
            if (!createdSelector && EnableItemHighlight && HighlightedIndex != -1 )
            {
                HighlightedIndex = -1;
                Highlight = null;
                OnItemHovered(null); // no longer hovering on anything
            }

            if (DraggedEntry != null)
            {
                // item is allowed to be dragged outside of the ScrollList?
                if (EnableDragOutEvents)
                {
                    DraggedEntry.Pos = input.CursorPosition + DraggedOffset;
                }
                else // item must stay inside the ScrollList
                {
                    DraggedEntry.Y = (input.CursorPosition + DraggedOffset).Y;
                }

                DraggedEntry.RequiresLayout = true;
            }

            return captured;
        }

        #endregion

        #region ScrollList Update / PerformLayout
        
        public ScrollListStyleTextures GetStyle() => ScrollListStyleTextures.Get(Style);
        
        // flattened entries
        protected readonly Array<ScrollListItemBase> FlatEntries = new Array<ScrollListItemBase>();
        
        // visible range is [begin, end)
        protected int VisibleItemsBegin, VisibleItemsEnd;

        // this is the default height for scroll list items
        // usually set to 40px at construction, but if ScrollListItem defines
        // override int ItemHeight, then this is overwritten
        protected int EntryHeight;

        protected int MaxVisibleItems;
        
        protected abstract void FlattenEntries();

        // Updates the visible index range
        // @return TRUE if this caused FirstVisibleIndex or LastVisibleIndex to change
        void UpdateVisibleIndex(float indexFraction, int maxVisibleItems)
        {
            float fraction = indexFraction.Clamped(0, FlatEntries.Count);
            int begin = (int)Math.Floor(fraction);
            int end   = (int)Math.Ceiling(fraction + 0.5f + maxVisibleItems);
            VisibleItemsBegin = begin.Clamped(0, Math.Max(0, FlatEntries.Count - maxVisibleItems));
            VisibleItemsEnd   = end.Clamped(0, FlatEntries.Count);
        }

        protected void SetItemsHousing()
        {
            ItemsHousing = new(X + PaddingLeft, Y + PaddingTop,
                               Width - (PaddingLeft + PaddingRight),
                               Height - (PaddingTop + PaddingBot));
        }

        public override void PerformLayout()
        {
            // fetch ClientArea if source is set
            if (ClientAreaSource != null)
                RectF = ClientAreaSource.ClientArea;

            base.PerformLayout();

            SetItemsHousing();
            ScrollListStyleTextures s = GetStyle();
            SubTexture up = s.ScrollBarArrowUp.Normal;
            SubTexture dn = s.ScrollBarArrowDown.Normal;
            ScrollUp   = new(Right - (up.Width + 5), Y + PaddingTop, up.Width, up.Height);
            ScrollDown = new(Right - (dn.Width + 5), Bottom - PaddingBot - dn.Height, dn.Width, dn.Height);
            ScrollHousing = new(ScrollUp.X + 1, ScrollUp.Bottom + 3, s.ScrollBarMid.Normal.Width, ScrollDown.Y - ScrollUp.Bottom - 6);
            ScrollBar.X = ScrollHousing.X;
            ScrollUpClickArea   = ScrollUp.Bevel(5);
            ScrollDownClickArea = ScrollDown.Bevel(5);

            FlattenEntries();

            if (FlatEntries.Count > 0)
            {
                int height = FlatEntries.First.ItemHeight;
                if (height != 0) EntryHeight = height;
            }

            // PaddingBot gives padding bottom of the items when scrolling
            float maxVisibleItemsF = (ItemsHousing.H) / (EntryHeight + ItemPadding.Y);
            MaxVisibleItems = (int)Math.Ceiling(maxVisibleItemsF);
            ShouldDrawScrollBar = FlatEntries.Count > (int)Math.Floor(maxVisibleItemsF);

            int itemX = (int)Math.Round(ItemsHousing.X + ItemPadding.X);
            int itemY = (int)Math.Round(ItemsHousing.Y + ItemPadding.Y);
            int itemW = (int)Math.Round(ItemsHousing.W - ItemPadding.X*2);

            if (ScrollBarPosChanged)
            {
                ScrollBarPosChanged = false;

                // when scrollbar was moved being dragged by input, use it to update the visible index
                float relScrollPos = GetRelativeScrollPosFromScrollBar();
                float scrolledIndexF = Math.Max(0, FlatEntries.Count - maxVisibleItemsF) * relScrollPos;
                UpdateVisibleIndex(scrolledIndexF, MaxVisibleItems);

                float remainder = (scrolledIndexF - VisibleItemsBegin) % 1f;
                int scrollOffset = (int)Math.Floor(remainder * EntryHeight);
                itemY -= scrollOffset;
                //Log.Info($"pos={relScrollPos} idxF={scrolledIndexF} rem={remainder} off={scrollOffset}");
            }
            else // otherwise, update/clamp visible indices and recalculate scrollbar
            {
                int begin = VisibleItemsBegin;
                UpdateVisibleIndex(begin, MaxVisibleItems);
                UpdateScrollBarToCurrentIndex(maxVisibleItemsF);
                //Log.Info($"Before=[{begin},{VisibleItemsEnd}) After=[{VisibleItemsBegin},{VisibleItemsEnd})");
            }

            int visibleIndex = 0;
            for (int i = 0; i < FlatEntries.Count; i++)
            {
                ScrollListItemBase e = FlatEntries[i];
                if (VisibleItemsBegin <= i && i < VisibleItemsEnd)
                {
                    e.ItemIndex = i;
                    e.VisibleIndex = visibleIndex++;
                    e.Rect = new Rectangle(itemX, itemY, itemW, EntryHeight);
                    e.PerformLayout();
                    itemY += (int)Math.Round(EntryHeight + ItemPadding.Y);
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
            float scrollSpan   = ScrollHousing.H - ScrollBar.H;
            return (scrollBarPos / scrollSpan);
        }

        void UpdateScrollBarToCurrentIndex(float maxVisibleItemsF)
        {
            int count = FlatEntries.Count;
            int startOffset = count == 0 ? 0 : (int)(ScrollHousing.H * (VisibleItemsBegin / (float)count));
            int barHeight = count == 0 ? (int)ScrollHousing.H : (int)(ScrollHousing.H * (maxVisibleItemsF / count));

            ScrollBar = new(ScrollHousing.X, ScrollHousing.Y + startOffset,
                            GetStyle().ScrollBarMid.Normal.Width, barHeight);
            ScrollBarClickArea = ScrollBar.Widen(5);
        }

        // set scrollbar to requested position
        void SetScrollBarPosition(float newScrollY)
        {
            if (newScrollY < ScrollHousing.Y)
                newScrollY = ScrollHousing.Y;
            else if ((newScrollY + ScrollBar.H) > ScrollHousing.Bottom)
                newScrollY = ScrollHousing.Bottom - ScrollBar.H;
            
            // This enables smooth scrollbar dragging: with every pixel changed we request layout
            if (ScrollBar.Y != newScrollY)
            {
                ScrollBar.Y = ScrollBarClickArea.Y = newScrollY;
                RequiresLayout = true;
                ScrollBarPosChanged = true;
            }
        }

        // move the scrollbar by requested amount of pixels up (-) or down (+)
        void ScrollByScrollBar(int deltaScroll) => SetScrollBarPosition(ScrollBar.Y + deltaScroll);

        public override void Update(float fixedDeltaTime)
        {
            if (!Visible)
                return;
            
            FirstUpdateDone = true;
            base.Update(fixedDeltaTime);

            for (int i = VisibleItemsBegin; i < VisibleItemsEnd; i++)
            {
                FlatEntries[i].Update(fixedDeltaTime);
            }
        }

        #endregion


        #region ScrollList Draw

        void DrawScrollBar(SpriteBatch batch)
        {
            ScrollListStyleTextures s = GetStyle();
            float upDownSize = (ScrollBar.H - s.ScrollBarMid.Normal.Height) / 2;
            var up  = new RectF(ScrollBar.X, ScrollBar.Y, ScrollBar.W, upDownSize);
            var mid = new RectF(ScrollBar.X, ScrollBar.Y + upDownSize, ScrollBar.W, s.ScrollBarMid.Normal.Height);
            var bot = new RectF(ScrollBar.X, mid.Y + mid.H, ScrollBar.W, upDownSize);

            bool parentHovered      = (ScrollBarHover == 1);
            bool controlItemHovered = (ScrollBarHover == 2);

            s.ScrollBarUpDown.Draw(batch, up, parentHovered, controlItemHovered);
            s.ScrollBarMid   .Draw(batch, mid, parentHovered, controlItemHovered);
            s.ScrollBarUpDown.Draw(batch, bot, parentHovered, controlItemHovered);
            s.ScrollBarArrowUp.Draw(batch, ScrollUp, parentHovered, ScrollUpHover);
            s.ScrollBarArrowDown.Draw(batch, ScrollDown, parentHovered, ScrollDownHover);
        }

        public override void Draw(SpriteBatch batch, DrawTimes elapsed)
        {
            if (!FirstUpdateDone)
                Log.Error($"{TypeName}.Update() Name={Name} has not been called. This is a bug!"
                          + " Make sure the ScrollList is being updated in GameScreen.Update() or screen.Add(list) for automatic update.");
            base.Draw(batch, elapsed);

            if (ShouldDrawScrollBar)
                DrawScrollBar(batch);
            
            // use a scissor to clip smooth scroll items
            batch.SafeEnd();
            batch.SafeBegin(SpriteBlendMode.AlphaBlend);

            for (int i = VisibleItemsBegin; i < VisibleItemsEnd; ++i)
            {
                var e = FlatEntries[i];
                if (e != DraggedEntry)
                {
                    e.Draw(batch, elapsed);
                }
                if (DebugDrawScrollList)
                {
                    batch.DrawRectangle(e.Rect, i % 2 == 0 ? Color.Green.Alpha(0.5f) : Color.Blue.Alpha(0.5f));
                }
            }
            
            if (DebugDrawScrollList) // scissored debug
            {
                batch.DrawRectangle(ScrollHousing, Color.Red);
                batch.DrawRectangle(ItemsHousing, Color.Magenta);
            }

            if (EnableItemHighlight)
                Highlight?.Draw(batch, elapsed);

            // widen the scissor for Item highlights to be visible
            RectF scissor = ItemsHousing.Bevel(1).Widen(8);
            RenderStates.EnableScissorTest(batch.GraphicsDevice, scissor);
            batch.SafeEnd();
            batch.SafeBegin();
            RenderStates.DisableScissorTest(batch.GraphicsDevice);
            
            if (DebugDrawScrollList) // non-scissored debug
            {
                batch.DrawRectangle(Rect, Color.AliceBlue);
            }

            // Draw the currently dragged entry
            if (DraggedEntry != null)
            {
                batch.FillRectangle(DraggedEntry.Rect, new Color(0, 0, 0, 150));
                DraggedEntry.Draw(batch, elapsed);
            }
        }

        #endregion
    }
}
