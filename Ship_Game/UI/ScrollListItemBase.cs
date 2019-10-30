using System;
using System.Collections.Generic;
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

        // The current visible index of this Entry
        public int VisibleIndex { get; set; }

        // If TRUE, this entry acts as a special ScrollList Item Header
        // Which can be expanded and collapsed
        public readonly bool IsHeader;
        public readonly string HeaderText;

        protected Array<ScrollListItemBase> SubEntries;

        // Lightweight dynamic elements
        // @note This provides customization options for each ScrollList Item
        //       use methods EnablePlus() / EnableUpDown() / etc to enable these elements
        protected Array<Element> DynamicElements;


        public ScrollListItemBase()
        {
        }

        public ScrollListItemBase(string headerText)
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

        public int NumSubItems => SubEntries.Count;

        public void AddSubItem(ScrollListItemBase entry)
        {
            if (!IsHeader)
                Log.Error($"SubItems can only be added if {TypeName}.IsHeader = true");

            entry.List = List;
            if (SubEntries == null)
                SubEntries = new Array<ScrollListItemBase>();
            SubEntries.Add(entry);
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

        public override bool HandleInput(InputState input)
        {
            bool wasHovered = Hovered;
            Hovered = Rect.HitTest(input.CursorPosition);

            // Mouse entered this Entry
            if (!wasHovered && Hovered && List.EnableItemEvents)
            {
                GameAudio.ButtonMouseOver();
                List.OnItemHovered(this);
            }

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
                    return true;
                }
            }

            return false;
        }

        public override void Draw(SpriteBatch batch)
        {
            base.Draw(batch);

            if (IsHeader)
            {
                int width = Math.Min(350, (int)Width - 40);
                var r = new Rectangle((int)X, (int)Y, width, (int)Height - 10);
                new Selector(r, (Hovered ? new Color(95, 82, 47) : new Color(32, 30, 18))).Draw(batch);

                var textPos = new Vector2(r.X + 10, r.CenterY() - Fonts.Pirulen12.LineSpacing / 2);
                batch.DrawString(Fonts.Pirulen12, HeaderText, textPos, Color.White);

                if (SubEntries != null && SubEntries.NotEmpty)
                {
                    string open = Expanded ? "-" : "+";
                    textPos = new Vector2(r.Right - 15 - Fonts.Arial20Bold.MeasureString(open).X / 2f,
                                          r.Y + 10 + 6 - Fonts.Arial20Bold.LineSpacing / 2);
                    batch.DrawString(Fonts.Arial20Bold, open, textPos, Color.White);
                }
            }

            if (DynamicElements != null && Hovered)
            {
                for (int i = 0; i < DynamicElements.Count; ++i)
                {
                    DynamicElements[i].Draw(batch);
                }
            }
        }
    }
}