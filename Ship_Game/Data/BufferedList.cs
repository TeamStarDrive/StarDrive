using System.Collections.Generic;

namespace Ship_Game.Data
{
    public class BufferedList<T> where T : class
    {
        Array<T> PublicList;
        Array<T> VolatileList;
        bool VolatileListChanged;

        public IReadOnlyList<T> GetRef() =>  PublicList;

        public BufferedList()
        {
            PublicList   = new Array<T>();
            VolatileList = new Array<T>();
        }

        public void Update()
        {
            if (VolatileListChanged)
            {
                PublicList   = VolatileList;
                VolatileList = new Array<T>(PublicList);
                VolatileListChanged  = false;
            }
        }

        public bool Add(T item)
        {
            bool added   = VolatileList.AddUniqueRef(item);
            VolatileListChanged |= added;
            return added;
        }

        public bool Remove(T item)
        {
            bool removed = VolatileList.RemoveRef(item);
            VolatileListChanged |= removed;
            return removed;
        }

        public void ClearAndDispose()
        {
            PublicList.ClearAndDispose();
            VolatileList.ClearAndDispose();
        }
    }
}