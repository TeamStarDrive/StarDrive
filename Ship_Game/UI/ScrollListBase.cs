using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
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

    public abstract class ScrollListBase : UIElementContainer
    {
        protected Rectangle ScrollUp, ScrollUpClickArea;
        protected Rectangle ScrollDown, ScrollDownClickArea;
        protected Rectangle ScrollBar, ScrollBarClickArea;
        protected Rectangle ScrollHousing;
        protected int ScrollBarHover;
        protected bool ScrollUpHover, ScrollDownHover;
        
        protected int DragStartMousePos;
        protected int DragStartScrollPos;
        protected bool DraggingScrollBar;
        protected bool WasScrolled;

        // The current scroll position of the scrollbar, clamped to [0, 1]
        protected float RelScrollPos;

        protected float ClickTimer;
        protected const float TimerDelay = 0.05f;

        // Top and Bottom padding for list items
        protected readonly int PaddingTop = 30;
        protected readonly int PaddingBot = 14;
        protected int EntryHeight;
        protected int MaxVisibleEntries;
        
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

        // If TRUE, allows automatic dragging of ScrollList Items
        public bool IsDraggable = false;

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

        public abstract void OnItemHovered(ScrollListItemBase item);
        public abstract void OnItemClicked(ScrollListItemBase item);
        public abstract void OnItemDoubleClicked(ScrollListItemBase item);
        public abstract void OnItemDragged(ScrollListItemBase item, DragEvent evt);

        // flattened entries
        protected readonly Array<ScrollListItemBase> FlatEntries = new Array<ScrollListItemBase>();

        // visible range is [begin, end)
        protected int VisibleItemsBegin, VisibleItemsEnd;

        // Updates the visible index range
        // @return TRUE if this caused FirstVisibleIndex or LastVisibleIndex to change
        void UpdateVisibleIndex(float indexFraction)
        {
            //ItemHeight = EntryHeight;
            int begin = (int)Math.Floor(indexFraction);
            int end   = (int)Math.Ceiling(indexFraction + MaxVisibleEntries);
            VisibleItemsBegin = begin.Clamped(0, Math.Max(0, FlatEntries.Count - MaxVisibleEntries));
            VisibleItemsEnd   = end.Clamped(0, Math.Max(0, FlatEntries.Count));
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

        bool HandleInputVisibleItems(InputState input)
        {
            // NOTE: We do not early return, because we want to update hover state for all ScrollList items
            bool anyCaptured = false;
            bool anyHovered = false;
            if (Rect.HitTest(input.CursorPosition))
            {
                for (int i = VisibleItemsBegin; i < VisibleItemsEnd; i++)
                {
                    ScrollListItemBase item = FlatEntries[i];
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

        protected ScrollListItemBase DraggedEntry;
        protected Vector2 DraggedOffset;

        protected abstract void HandleDraggable(InputState input);
        protected abstract void HandleElementDragging(InputState input);

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

        protected abstract void FlattenEntries();

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

            FlattenEntries();

            int scrollOffset = 0;
            if (WasScrolled)
            {
                WasScrolled = false;
                // when scrollbar was moved being dragged by input, use it to update the visible index
                float scrollPos = GetRelativeScrollPosFromScrollBar();
                float newIndexFraction = Math.Max(0, FlatEntries.Count - MaxVisibleEntries) * scrollPos;
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
            for (int i = 0; i < FlatEntries.Count; i++)
            {
                ScrollListItemBase e = FlatEntries[i];
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
            int startOffset = (int)(ScrollHousing.Height * (VisibleItemsBegin / (float)FlatEntries.Count));
            int barHeight   = (int)(ScrollHousing.Height * (MaxVisibleEntries / (float)FlatEntries.Count));

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

            if (FlatEntries.Count > MaxVisibleEntries)
                DrawScrollBar(batch);
            
            // use a scissor to clip smooth scroll items
            batch.End();
            batch.Begin(SpriteBlendMode.AlphaBlend, SpriteSortMode.Deferred, SaveStateMode.None);

            for (int i = VisibleItemsBegin; i < VisibleItemsEnd; ++i)
            {
                FlatEntries[i].Draw(batch);
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
}
