using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Ship_Game.Audio;

namespace Ship_Game
{
    public class ScrollListItem<T> : ScrollListItemBase where T : ScrollListItem<T>
    {
        public override string ToString() => IsHeader ? $"{TypeName} Header={HeaderText}" : base.ToString();

        protected ScrollListItem() { }

        // Creates a ScrollList Item Header which can be expanded
        protected ScrollListItem(string headerText) : base(headerText) { }

        void AddElement(Vector2 relPos, in LocalizedText tooltip, Action onClick, Func<ScrollListStyleTextures.Hoverable> getHoverable)
        {
            var e = new Element{ Parent = this, RelPos = relPos, Tooltip = tooltip, OnClick = onClick, GetHoverable = getHoverable };
            if (DynamicElements == null) DynamicElements = new Array<Element>();
            DynamicElements.Add(e);
        }

        public void AddPlus(Vector2 relPos, in LocalizedText tooltip, Action onClick = null)  => AddElement(relPos, tooltip, onClick, () => List.GetStyle().BuildAdd);
        public void AddEdit(Vector2 relPos, in LocalizedText tooltip, Action onClick = null)  => AddElement(relPos, tooltip, onClick, () => List.GetStyle().BuildEdit);
        public void AddUp(Vector2 relPos, in LocalizedText tooltip, Action onClick = null)    => AddElement(relPos, tooltip, onClick, () => List.GetStyle().QueueArrowUp);
        public void AddDown(Vector2 relPos, in LocalizedText tooltip, Action onClick = null)  => AddElement(relPos, tooltip, onClick, () => List.GetStyle().QueueArrowDown);
        public void AddApply(Vector2 relPos, in LocalizedText tooltip, Action onClick = null) => AddElement(relPos, tooltip, onClick, () => List.GetStyle().QueueRush);
        public void AddCancel(Vector2 relPos, in LocalizedText tooltip, Action onClick)       => AddElement(relPos, tooltip, onClick, () => List.GetStyle().QueueDelete);

        public bool RemoveFirstSubIf(Predicate<T> predicate)
        {
            if (SubEntries == null || SubEntries.IsEmpty)
                return false;

            foreach (ScrollListItemBase sub in SubEntries)
                if (((T)sub).RemoveFirstSubIf(predicate))
                    return true;

            bool removed =  SubEntries.RemoveFirst(e => predicate((T)e));
            if (removed) List.RequiresLayout = true;
            return removed;
        }
    }
}