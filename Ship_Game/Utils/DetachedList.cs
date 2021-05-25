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
        object ChangeLocker = new object();

        /// <summary>
        /// Gives a reference to the PublicList. On update this will no longer be the PublicList.
        /// To add or remove items from the public list use DetachedList add/remove methods. 
        /// When iterating this list make sure to use a reference to the PublicList by using:
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
                PublicList = VolatileList;
                lock (ChangeLocker)
                {
                    VolatileList = new Array<T>(PublicList);
                    VolatileListChanged = false;
                }
            }
        }

        /// <summary>
        /// Not thread safe. All adds must be done on same thread.
        /// </summary>
        public bool Add(T item)
        {
            lock (ChangeLocker)
            {
                bool added = VolatileList.AddUniqueRef(item);
                VolatileListChanged |= added;
                return added;
            }
        }

        /// <summary>
        /// Not thread safe. All removes must be done on same thread.
        /// </summary>
        public bool Remove(T item)
        {
            lock (ChangeLocker)
            {
                bool removed = VolatileList.RemoveRef(item);
                VolatileListChanged |= removed;
                return removed;
            }
        }

        public void CleanOut()
        {
            PublicList   = new Array<T>();
            VolatileList = new Array<T>();
        }

        public void ClearAndDispose()
        {
            PublicList.ClearAndDispose();
            VolatileList.ClearAndDispose();
        }
    }
}