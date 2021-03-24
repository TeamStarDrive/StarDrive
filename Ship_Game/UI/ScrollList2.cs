using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using Microsoft.Xna.Framework;
using Ship_Game.Audio;

namespace Ship_Game
{

    // EVENT: Called when an item drag starts or item drag ends
    // @param T item that was dragged
    // @param DragEvent evt Description of the event
    // @param bool outside If TRUE, mouse cursor is outside of ScrollList Rect
    public delegate void ScrollListDragOutEvt<T>(T item, DragEvent type, bool outside) where T : ScrollListItem<T>;

    // EVENT: Called when EnableDragReorderItems and an item changes its index
    // @param T item that was dragged
    // @param int oldIndex The old index that the dragged item had
    // @param int newIndex The new index of the dragged item
    public delegate void ScrollListDragReorderEvt<T>(T item, int oldIndex, int newIndex) where T : ScrollListItem<T>;


    [DebuggerTypeProxy(typeof(ScrollListDebugView<>))]
    [DebuggerDisplay("{TypeName}  Entries = {Entries.Count}  Expanded = {FlatEntries.Count}")]
    public class ScrollList2<T> : ScrollListBase where T : ScrollListItem<T>
    {
        readonly Array<T> Entries = new Array<T>();

        Action<T> EvtHovered;
        Action<T> EvtClick;
        Action<T> EvtDoubleClick;
        ScrollListDragOutEvt<T> EvtDragOut;
        ScrollListDragReorderEvt<T> EvtDragReorder;

        // EVENT: Called when a new item is focused with mouse
        //        @note This is called again with <null> when mouse leaves focus
        public Action<T> OnHovered
        {
            set
            {
                EvtHovered = value;
                EnableItemEvents = EvtHovered != null || EvtClick != null || EvtDoubleClick != null;
            }
        }

        // EVENT: Called when an item is clicked on
        public Action<T> OnClick
        {
            set
            {
                EvtClick = value;
                EnableItemEvents = EvtHovered != null || EvtClick != null || EvtDoubleClick != null;
            }
        }

        // EVENT: Called when an item is double-clicked
        public Action<T> OnDoubleClick
        {
            set
            {
                EvtDoubleClick = value;
                EnableItemEvents = EvtHovered != null || EvtClick != null || EvtDoubleClick != null;
            }
        }

        // EVENT: Called when an item drag starts or item drag ends
        // @see ScrollListDragOutEvt<T> for more documentation
        public ScrollListDragOutEvt<T> OnDragOut
        {
            set
            {
                EvtDragOut = value;
                EnableDragOutEvents = EvtDragOut != null;
            }
        }

        // EVENT: Called when EnableDragReorderItems and an item changes its index
        // @see ScrollListDragReorderEvt<T> for more documentation
        public ScrollListDragReorderEvt<T> OnDragReorder
        {
            set
            {
                EvtDragReorder = value;
                EnableDragReorderEvents = EvtDragReorder != null;
            }
        }

        static Rectangle GetOurRectFromBackground(UIElementV2 background)
        {
            Rectangle r = background.Rect;
            if (background is Menu1)
            {
                r.Width -= 5;
            }
            else if (background is Submenu)
            {
                r.Y += 10;
            }
            return r;
        }

        // WARNING: ScrollList2 will take ownership of `background`
        public ScrollList2(UIElementV2 background, ListStyle style = ListStyle.Default)
            : this(background, 40, style)
        {
        }
        
        // WARNING: ScrollList2 will take ownership of `background`
        public ScrollList2(UIElementV2 background, int entryHeight, ListStyle style = ListStyle.Default)
            : this(GetOurRectFromBackground(background), entryHeight, style)
        {
            TakeOwnershipOfBackground(background);
        }

        public ScrollList2(float x, float y, float w, float h, int entryHeight, ListStyle style = ListStyle.Default)
            : this(new Rectangle((int)x, (int)y, (int)w, (int)h), entryHeight, style)
        {
        }

        public ScrollList2(in Rectangle rect, int entryHeight = 40, ListStyle style = ListStyle.Default)
        {
            Rect = rect;
            Style = style;
            EntryHeight = entryHeight;
        }

        public override void OnItemHovered(ScrollListItemBase item)
        {
            GameAudio.ButtonMouseOver();
            EvtHovered?.Invoke((T)item);
        }

        public override void OnItemClicked(ScrollListItemBase item)
        {
            EvtClick?.Invoke((T)item);
        }

        public override void OnItemDoubleClicked(ScrollListItemBase item)
        {
            EvtDoubleClick?.Invoke((T)item);
        }

        public override void OnItemDragged(ScrollListItemBase item, DragEvent evt, bool outside)
        {
            EvtDragOut?.Invoke((T)item, evt, outside);
        }

        public override void OnItemDragReordered(ScrollListItemBase dragged, ScrollListItemBase destination)
        {
            int oldIndex = Entries.IndexOf(dragged);
            int newIndex = Entries.IndexOf(destination);
            if (oldIndex == -1 || newIndex == -1)
            {
                Log.Error($"ItemDragReorder failed oldIndex:{oldIndex} newIndex:{newIndex}, child-item moving is not implemented");
                return;
            }

            Entries.Reorder(oldIndex, newIndex);
            EvtDragReorder?.Invoke((T)dragged, oldIndex, newIndex);
        }

        // Number of non-flattened entries
        public int NumEntries => Entries.Count;
        public IReadOnlyList<T> AllEntries => Entries;

        // Item at non-flattened index: doesn't index hierarchical elements
        public T this[int index] => Entries[index];

        // @return The first currently visible item
        public T ItemAtTop => (T)FlatEntries[VisibleItemsBegin];

        // @return The last currently visible item
        public T ItemAtBottom  => (T)FlatEntries[VisibleItemsEnd - 1];

        public T AddItem(T entry)
        {
            entry.List = this;
            Entries.Add(entry);
            RequiresLayout = true;
            return entry;
        }

        public void SetItems(IEnumerable<T> newItems)
        {
            Reset();
            foreach (T item in newItems)
            {
                item.List = this;
                Entries.Add(item);
            }
        }

        public void Reset()
        {
            Entries.Clear();
            FlatEntries.Clear();
            // we reset the End to prevent index out of bounds,
            // but we keep Begin to give stability to the queue
            VisibleItemsEnd = 0; 
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

            if (FlatEntries.Remove(e))
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

            if (FlatEntries.RemoveFirst((e) => predicate((T)e)))
                RequiresLayout = true;
        }

        public bool Any(Predicate<T> predicate)
        {
            return Entries.Any(predicate)
                || FlatEntries.Any((e) => predicate((T)e));
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
        
        protected override void FlattenEntries()
        {
            FlatEntries.Clear();
            for (int i = 0; i < Entries.Count; ++i)
                Entries[i].GetFlattenedVisibleExpandedEntries(FlatEntries);
        }
    }

    internal sealed class ScrollListDebugView<T> where T : ScrollListItem<T>
    {
        readonly ScrollList2<T> List;
        // ReSharper disable once UnusedMember.Global
        public ScrollListDebugView(ScrollList2<T> list) { List = list; }
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