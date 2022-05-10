using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using SDUtils;

namespace Ship_Game.Utils
{
    public struct PendingItem<T>
    {
        public T Item;
        public bool Add; // true: Add, false: Remove
    }

    /// <summary>
    /// Yet another thread-safe utility
    /// It's similar to `ChangePendingList`, but it's thread-safe
    /// </summary>
    public class ChangePendingListSafe<T>
    {
        Array<PendingItem<T>> Items = new Array<PendingItem<T>>();
        readonly object Sync = new object();

        public void Add(T item)
        {
            lock (Sync)
                Items.Add(new PendingItem<T>{ Item = item, Add = true });
        }

        public void Remove(T item)
        {
            lock (Sync)
                Items.Add(new PendingItem<T>{ Item = item, Add = false });
        }

        public void Clear()
        {
            lock (Sync)
                Items.Clear();
        }

        /// <summary>
        /// Gets pending items and CLEARS this list
        /// </summary>
        public Array<PendingItem<T>> MovePendingItems()
        {
            var newList = new Array<PendingItem<T>>();
            lock (Sync)
            {
                Array<PendingItem<T>> items = Items; // just swap
                Items = newList;
                return items;
            }
        }
    }
}
