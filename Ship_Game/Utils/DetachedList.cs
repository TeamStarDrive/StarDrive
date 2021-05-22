using System.Collections.Generic;

namespace Ship_Game.Utils
{
    /// <summary>
    /// Used to provide a list of items that will not change by adding or removing items from the buffered list.
    /// </summary>
    public class DetachedList<T> where T : class
    {
        Array<T> PublicList;
        Array<T> VolatileList;
        bool VolatileListChanged;

        /// <summary>
        /// Gets a snapshot reference of the items in the buffered list.
        /// The list will never change unless changed outside of the buffered list.
        /// When iterating this list make sure to use the reference list by using:
        /// var refList = instance.GetRef()
        /// foreach(var item in refList)
        /// It is thread safe with above conditions. 
        /// </summary>
        public IReadOnlyList<T> GetRef() =>  PublicList;

        public DetachedList()
        {
            PublicList   = new Array<T>();
            VolatileList = new Array<T>();
        }

        /// <summary>
        /// Apply pending modifications and create a new list reference.
        /// the update is atomic 
        /// </summary>
        public void Update()
        {
            if (VolatileListChanged)
            {
                PublicList   = VolatileList;
                VolatileList = new Array<T>(PublicList);
                VolatileListChanged  = false;
            }
        }

        /// <summary>
        /// Not thread safe. all adds must be done on same thread.
        /// </summary>
        public bool Add(T item)
        {
            bool added   = VolatileList.AddUniqueRef(item);
            VolatileListChanged |= added;
            return added;
        }

        /// <summary>
        /// Not thread safe. all removes must be done on same thread.
        /// </summary>
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