using System;
using SDUtils;

namespace Ship_Game.Utils
{
    public class ChangePendingList<T>  where T : class
    {
        public Array<T> Items { get; private set; }
        Array<PendingItem<T>> Pending;

        public ChangePendingList()
        {
            Items = new Array<T>();
            Pending = new Array<PendingItem<T>>();
        }

        public void Update()
        {
            while (Pending.TryPopLast(out PendingItem<T> pending))
            {
                if (pending.Add)
                {
                    Items.Add(pending.Item);
                }
                else
                {
                    Items.RemoveRef(pending.Item);
                }
            }
        }

        public void Add(T item)
        {
            Pending.Add(new PendingItem<T>{ Item = item, Add = true });
        }

        public bool RemoveItemImmediate(T item)
        {
            if (Items.RemoveRef(item))
                return true;
            return Pending.RemoveFirst(s => s.Item == item);
        }

        public bool Contains(T item)
        {
            for (int i = 0; i < Pending.Count; ++i)
            {
                if (Pending[i].Item == item)
                    return Pending[i].Add;
            }
            return Items.ContainsRef(item);
        }

        public void Clear()
        {
            // Thread-safety: Just replace the lists
            Pending = new Array<PendingItem<T>>();
            Items = new Array<T>();
        }
    }
}