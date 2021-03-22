using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Ship_Game.Audio;

namespace Ship_Game
{
    public class ScrollListItemBase : UIElementContainer
    {
        public ScrollListBase List;

        // Customization Point: Provide a custom dynamic Height for Scroll List Items
        public virtual int ItemHeight => 0;

        // Entries with IsHeader=true can be expanded or collapsed via category title
        public bool Expanded { get; protected set; }

        // true if item is currently being hovered over with mouse cursor
        public bool Hovered { get; protected set; }

        // Absolute index of this item in the ScrollList
        public int ItemIndex;

        // Visible index of this item, where 0 is the topmost visible item
        public int VisibleIndex;

        // If TRUE, this entry acts as a special ScrollList Item Header
        // Which can be expanded and collapsed
        public bool IsHeader;
        public string HeaderText;
        public int HeaderMaxWidth = 350; // maximum allowed header width limit

        // EVT: Triggered if ScrollList has Click events enabled and this Item is clicked
        public Action OnClick;

        protected Array<ScrollListItemBase> SubEntries;

        // Lightweight dynamic elements
        // @note This provides customization options for each ScrollList Item
        //       use methods EnablePlus() / EnableUpDown() / etc to enable these elements
        protected Array<Element> DynamicElements;


        protected ScrollListItemBase()
        {
        }

        protected ScrollListItemBase(string headerText)
        {
            HeaderText = headerText;
            IsHeader = true;
        }


        // Resets the list item in such a way that it cannot be accidentally interacted with
        public void ClearItemLayout()
        {
            Hovered = false;
            Rect = new Rectangle(-500, -500, 0, 0);
        }

        public void Expand(bool expanded)
        {
            if (Expanded != expanded && IsHeader && SubEntries != null && SubEntries.NotEmpty)
            {
                Expanded = expanded;
                List.OnItemExpanded(this, expanded);
            }
        }

        public int NumSubItems => SubEntries?.Count ?? 0;

        public void AddSubItem(ScrollListItemBase entry)
        {
            if (!IsHeader)
                Log.Error($"SubItems can only be added if {TypeName}.IsHeader = true");

            entry.List = List;
            if (SubEntries == null)
                SubEntries = new Array<ScrollListItemBase>();
            SubEntries.Add(entry);
            if (Expanded)
                List.RequiresLayout = true;
        }

        public bool RemoveSub(ScrollListItemBase e)
        {
            if (SubEntries == null || SubEntries.IsEmpty)
                return false;

            for (int i = 0; i < SubEntries.Count; ++i)
                if (SubEntries[i].RemoveSub(e))
                    return true;
            
            bool removed = SubEntries.Remove(e);
            if (removed) List.RequiresLayout = true;
            return removed;
        }

        public void GetFlattenedVisibleExpandedEntries(Array<ScrollListItemBase> entries)
        {
            if (Visible)
            {
                entries.Add(this);
                if (Expanded && SubEntries != null)
                {
                    for (int i = 0; i < SubEntries.Count; ++i)
                    {
                        SubEntries[i].GetFlattenedVisibleExpandedEntries(entries);
                    }
                }
            }
        }
        
        public class Element
        {
            public ScrollListItemBase Parent;
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

        
        public override void PerformLayout()
        {
            if (DynamicElements != null)
            {
                for (int i = 0; i < DynamicElements.Count; ++i)
                    DynamicElements[i].UpdateLayout();
            }

            base.PerformLayout();
        }

        public bool UpdateHoverState(InputState input)
        {
            bool wasHovered = Hovered;
            bool isHovered = Rect.HitTest(input.CursorPosition);
            Hovered = isHovered;
            // Mouse entered:
            if (!wasHovered && isHovered && List.EnableItemEvents)
            {
                List.OnItemHovered(this);
            }
            // Mouse Leave event is handled in List.HandleInput 
            return isHovered;
        }

        public override bool HandleInput(InputState input)
        {
            if (DynamicElements != null)
            {
                for (int i = 0; i < DynamicElements.Count; ++i)
                    if (DynamicElements[i].HandleInput(input))
                        return true;
            }

            if (base.HandleInput(input))
                return true;

            if (Hovered && List.EnableItemEvents)
            {
                if (input.LeftMouseDoubleClick)
                {
                    GameAudio.AcceptClick();
                    List.OnItemDoubleClicked(this);
                    return true;
                }
                if (input.LeftMouseClick)
                {
                    GameAudio.AcceptClick();
                    Expand(!Expanded);

                    List.OnItemClicked(this);
                    OnClick?.Invoke();
                    return true;
                }
            }

            return false;
        }

        public override void Draw(SpriteBatch batch, DrawTimes elapsed)
        {
            base.Draw(batch, elapsed);

            if (IsHeader)
            {
                int width = Math.Min(HeaderMaxWidth, (int)Width);
                var r = new Rectangle((int)X, (int)Y+4, width, (int)Height - 10);

                if (HeaderText != null)
                {
                    Color bkgColor = !Enabled ? Color.Gray
                                    : Hovered ? new Color(95, 82, 47)
                                    : new Color(32, 30, 18);
                    new Selector(r, bkgColor).Draw(batch, elapsed);

                    var textPos = new Vector2(r.X + 10, r.CenterY() - Fonts.Pirulen12.LineSpacing / 2);
                    batch.DrawString(Fonts.Pirulen12, HeaderText, textPos, Color.White);
                }

                if (SubEntries != null && SubEntries.NotEmpty)
                {
                    string open = Expanded ? "-" : "+";
                    var textPos = new Vector2(r.Right - 26, r.CenterY() - Fonts.Arial20Bold.LineSpacing / 2 - 2);
                    batch.DrawString(Fonts.Arial20Bold, open, textPos, Color.White);
                }
            }

            if (DynamicElements != null)
            {
                for (int i = 0; i < DynamicElements.Count; ++i)
                {
                    DynamicElements[i].Draw(batch);
                }
            }
        }
    }
}