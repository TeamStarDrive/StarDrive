using System;

namespace Ship_Game.Utils
{
    public class ChangePendingList<T>  where T : class
    {
        Array<T> PendingAdds;
        Array<T> PendingRemoves;
        Array<T> List;
        readonly Predicate<T> AddItemFilter;

        public ChangePendingList(Predicate<T> addFilter)
        {
            PendingAdds    = new Array<T>();
            PendingRemoves = new Array<T>();
            List           = new Array<T>();
            AddItemFilter  = addFilter;
        }

        public Array<T> Items => List;

        public void Update()
        {
            while(PendingRemoves.NotEmpty)
            {
                var item = PendingRemoves.PopFirst();
                PendingAdds.RemoveRef(item);
                List.RemoveRef(item);
            }

            while(PendingAdds.NotEmpty)
            {
                var item = PendingAdds.PopFirst();
                if (AddItemFilter(item))
                {
                    bool addFail = !List.AddUnique(item);
                    if (addFail)
                        Log.Error("Ship already in force pool");
                }
            }
        }

        public bool AddItemPending(T item)
        {
            if (List.Contains(item)) return false;
            return PendingAdds.AddUniqueRef(item);
        }

        public bool RemoveItemPending(T item) => PendingRemoves.AddUniqueRef(item);
        public bool RemoveItemImmediate(T item)
        {
            bool removed = List.RemoveRef(item);
            removed |= PendingRemoves.RemoveRef(item);
            removed |= PendingAdds.RemoveRef(item);
            return removed;
        }

        public bool Contains(T item)
        {
            if (PendingRemoves.ContainsRef(item)) return false;
            if (PendingAdds.ContainsRef(item))    return true;
            return List.ContainsRef(item);
        }

        public void ClearOut()
        {
            PendingAdds    = new Array<T>();
            PendingRemoves = new Array<T>();
            List           = new Array<T>();
        }

        public void ClearAndDispose()
        {
            PendingRemoves.ClearAndDispose();
            PendingAdds.ClearAndDispose();
            List.ClearAndDispose();
        }
    }
}