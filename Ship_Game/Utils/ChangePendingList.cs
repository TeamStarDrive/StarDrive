using System;

namespace Ship_Game.Utils
{
    public class ChangePendingList<T>  where T : class
    {
        struct PendingItem
        {
            public T Item;
            public bool Add; // true: Add, false: Remove
        }
        
        public Array<T> Items { get; private set; }
        Array<PendingItem> Pending;

        public ChangePendingList()
        {
            Items = new Array<T>();
            Pending = new Array<PendingItem>();
        }

        public void Update()
        {
            while (Pending.TryPopLast(out PendingItem pending))
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
            Pending.Add(new PendingItem{ Item = item, Add = true });
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
            Pending = new Array<PendingItem>();
            Items    = new Array<T>();
        }
    }
}