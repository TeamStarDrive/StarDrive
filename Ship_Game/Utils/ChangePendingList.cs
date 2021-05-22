namespace Ship_Game.Utils
{
    public class ChangePendingList<T>  where T : class
    {
        Array<T> PendingAdds;
        Array<T> PendingRemoves;
        Array<T> List;

        public ChangePendingList()
        {
            PendingAdds    = new Array<T>();
            PendingRemoves = new Array<T>();
            List = new Array<T>();
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
                List.AddUnique(item);
            }
        }

        public bool AddItemPending(T item) => PendingAdds.AddUniqueRef(item);

        public bool RemoveItemPending(T item) => PendingRemoves.AddUniqueRef(item);
        public bool RemoveItemImmediate(T item)
        {
            bool removed = List.RemoveRef(item);
            removed |= PendingRemoves.RemoveRef(item);
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