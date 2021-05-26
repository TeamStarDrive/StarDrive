using System;
using System.Collections.Generic;
using System.Threading;

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
        Array<Action> PostUpDateActions = new Array<Action>();

        /// <summary>
        /// Gives a reference to the PublicList that is readonly and wont be updated normally.
        /// To add or remove items from the public list use DetachedList add/remove methods. 
        /// This list is static and can be safely accessed from any thread.  
        /// </summary>
        public IReadOnlyList<T> GetRef() =>  PublicList;

        public DetachedList()
        {
            PublicList   = new Array<T>();
            VolatileList = new Array<T>();
        }

        public void QueuePreUpdateAction(Action action) => PostUpDateActions.Add(action);

        /// <summary>
        /// Apply pending modifications and create a new list reference.
        /// the update is atomic but is not thread safe to run the update on different threads. 
        /// </summary>
        public void Update()
        {
            if (VolatileListChanged)
            {
                PublicList = VolatileList;

                VolatileList = new Array<T>(PublicList);
                VolatileListChanged = false;
                while (PostUpDateActions.NotEmpty)
                    PostUpDateActions.PopFirst().Invoke();
            }
        }

        /// <summary>
        /// Not thread safe. All adds must be done on same thread.
        /// </summary>
        public bool Add(T item)
        {
            bool added = VolatileList.AddUniqueRef(item);
            VolatileListChanged |= added;
            return added;
        }

        /// <summary>
        /// Not thread safe. All removes must be done on same thread.
        /// </summary>
        public bool Remove(T item)
        {
            bool removed = VolatileList.RemoveRef(item);
            VolatileListChanged |= removed;
            return removed;
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