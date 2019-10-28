using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Ship_Game.Audio;

namespace Ship_Game
{
    public class ScrollListItem<T> : UIElementContainer where T : ScrollListItem<T>
    {
        public ScrollList<T> List;

        // Entries with IsHeader=true can be expanded or collapsed via category title
        public bool Expanded { get; private set; }

        // true if item is currently being hovered over with mouse cursor
        public bool Hovered { get; private set; }

        // The current visible index of this Entry
        public int VisibleIndex;

        // If TRUE, this entry acts as a special ScrollList Item Header
        // Which can be expanded and collapsed
        public bool IsHeader;
        public readonly string HeaderText;

        Array<T> SubEntries;
            
        // Lightweight dynamic elements
        // @note This provides customization options for each ScrollList Item
        //       use methods EnablePlus() / EnableUpDown() / etc to enable these elements
        Array<Element> DynamicElements;

        public override string ToString() => IsHeader ? $"{TypeName} Header={HeaderText}" : base.ToString();

        public ScrollListItem()
        {
        }

        // Creates a ScrollList Item Header which can be expanded
        public ScrollListItem(string headerText)
        {
            HeaderText = headerText;
            IsHeader = true;
        }

        public class Element
        {
            public ScrollListItem<T> Parent;
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
            if (DynamicElements == null) DynamicElements = new Array<Element>();
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
            if (!IsHeader)
                Log.Error($"SubItems can only be added if {TypeName}.IsHeader = true");

            entry.List = List;
            if (SubEntries == null)
                SubEntries = new Array<T>();
            SubEntries.Add(entry);
        }

        public bool RemoveSub(T e)
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

        public bool RemoveFirstSubIf(Predicate<T> predicate)
        {
            if (SubEntries == null || SubEntries.IsEmpty)
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
                if (Expanded && SubEntries != null)
                {
                    foreach (T sub in SubEntries)
                        sub.GetFlattenedVisibleExpandedEntries(entries);
                }
            }
        }

        public void Expand(bool expanded)
        {
            if (!IsHeader || Expanded == expanded || SubEntries == null || SubEntries.IsEmpty)
                return;

            Expanded = expanded;
            if (!expanded && SubEntries != null)
            {
                List.FirstVisibleItemIndex -= SubEntries.Count;
                if (List.FirstVisibleItemIndex < 0)
                    List.FirstVisibleItemIndex = 0;
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
            if (!wasHovered && Hovered)
            {
                GameAudio.ButtonMouseOver();
                List.OnItemHovered(this as T);
            }

            if (DynamicElements != null)
            {
                for (int i = 0; i < DynamicElements.Count; ++i)
                    if (DynamicElements[i].HandleInput(input))
                        return true;
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
                    Expand(!Expanded);
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

            if (IsHeader)
            {
                var r = new Rectangle((int)X, (int)Y, (int)Width - 40, (int)Height - 10);
                new Selector(r, (Hovered ? new Color(95, 82, 47) : new Color(32, 30, 18))).Draw(batch);

                var textPos = new Vector2(r.X + 10, r.CenterY() - Fonts.Pirulen12.LineSpacing / 2);
                batch.DrawString(Fonts.Pirulen12, HeaderText, textPos, Color.White);

                string open = Expanded ? "-" : "+";
                textPos = new Vector2(r.Right - 15 - Fonts.Arial20Bold.MeasureString(open).X / 2f,
                    r.Y + 10 + 6 - Fonts.Arial20Bold.LineSpacing / 2);
                batch.DrawString(Fonts.Arial20Bold, open, textPos, Color.White);
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